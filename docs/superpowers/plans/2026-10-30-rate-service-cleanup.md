# 배율 서비스 다듬기(묶음 2) 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 배율 서비스의 전투 단위 상태 전환을 규칙(순수 함수)으로 고정해 겹침·누락을 막고, 수치 함수를 떼어 시험으로 덮고, 파일을 나눈다. 동작 불변.

**Architecture:** 판정은 `RateUnitRules`(새 파일)·`RateGroups`(기존)에, 쓰기는 `RateService.Tick.cs`에. `BattleService`·`MartialService`는 함수 호출로 바꾼다.

**Tech Stack:** C#, `tests/ModMain.Tests`, 게임 검사 PowerShell.

**Spec:** `docs/superpowers/specs/2026-10-30-rate-service-cleanup-design.md`

## Global Constraints

- 브랜치 `feat/rate-service-cleanup`. 배율의 수치·반올림·하한은 바꾸지 않는다(검사의 기대값이 그대로여야 한다).
- 회차 이름 `rsc-a-1030`부터.

## Review Focus

1. 쿨타임 줄을 켠 채 전투 밖으로 나갔다가 같은 단위가 다시 보이면 곱한 값을 원래 값으로 다시 뜨지 않아야 한다(`Skip` 뒤 `Apply`) — Task 1의 `Decide` 시험.
2. 같은 포인터인데 값이 마지막으로 쓴 것과 다르면 옛 원래 값을 쓰지 않아야 한다 — Task 1 `!intact` 시험.
3. 전투 이동 속도 처리기는 같은 DynInt에 두 번 붙지 않아야 한다 — Task 1 `MoveAttachNeeded(on, attached=true) == false`.
4. 주는 피해 배수가 음수 baseValue를 건드리지 않아야 한다 — Task 2 `DealtDamage(-5, 100) == -5`.
5. 배율 수치는 전과 같아야 한다 — Task 2의 반올림 시험(13, 12, 25→13 등)과 Task 5의 회차 검사.

---

### Task 1: `RateUnitRules`(순수) + 시험

**Files:** Create `src/ModCode/ModMain/Services/RateUnitRules.cs`; Modify `tests/ModMain.Tests/ModMain.Tests.csproj`, `Program.cs`(`RateUnitRulesTests`).

**Interfaces (Produces):**
```csharp
internal static class RateUnitRules
{
    public enum Step { Skip, Forget, Capture, Apply }
    // inBattle: 전투 단위가 보임, anyOn: 켜진 줄이 있음, held: 떠 둔 단위가 있음, applied: 그 단위에 쓴 값이 있음,
    // same: 지금 단위가 떠 둔 단위와 같은 포인터, intact: 쓴 값이 마지막으로 쓴 것 그대로(applied가 거짓이면 참으로 준다)
    public static Step Decide(bool inBattle, bool anyOn, bool held, bool applied, bool same, bool intact);
    public static bool MoveAttachNeeded(bool on, bool attached);   // on && !attached
    public static bool MoveDetachNeeded(bool on, bool attached);   // !on && attached
}
```
- [ ] Step 1 시험(RED):
  ```csharp
  Check("unit: out of battle with written values holds", RateUnitRules.Decide(false, false, true, true, false, true) == RateUnitRules.Step.Skip);
  Check("unit: out of battle without written values forgets", RateUnitRules.Decide(false, true, true, false, false, true) == RateUnitRules.Step.Forget);
  Check("unit: out of battle with nothing held skips", RateUnitRules.Decide(false, true, false, false, false, true) == RateUnitRules.Step.Skip);
  Check("unit: first sight with a row on captures", RateUnitRules.Decide(true, true, false, false, false, true) == RateUnitRules.Step.Capture);
  Check("unit: first sight with nothing on skips", RateUnitRules.Decide(true, false, false, false, false, true) == RateUnitRules.Step.Skip);
  Check("unit: another pointer with a row on captures", RateUnitRules.Decide(true, true, true, true, false, true) == RateUnitRules.Step.Capture);
  Check("unit: another pointer with nothing on forgets", RateUnitRules.Decide(true, false, true, true, false, true) == RateUnitRules.Step.Forget);
  Check("unit: same pointer but changed values captures again", RateUnitRules.Decide(true, true, true, true, true, false) == RateUnitRules.Step.Capture);
  Check("unit: same pointer but changed values with nothing on forgets", RateUnitRules.Decide(true, false, true, true, true, false) == RateUnitRules.Step.Forget);
  Check("unit: same pointer intact applies", RateUnitRules.Decide(true, true, true, true, true, true) == RateUnitRules.Step.Apply);
  Check("unit: same pointer, nothing on, written values apply (restore)", RateUnitRules.Decide(true, false, true, true, true, true) == RateUnitRules.Step.Apply);
  Check("move: attach only when on and not attached", RateUnitRules.MoveAttachNeeded(true, false) && !RateUnitRules.MoveAttachNeeded(true, true) && !RateUnitRules.MoveAttachNeeded(false, false));
  Check("move: detach only when off and attached", RateUnitRules.MoveDetachNeeded(false, true) && !RateUnitRules.MoveDetachNeeded(true, true) && !RateUnitRules.MoveDetachNeeded(false, false));
  ```
- [ ] Step 2 실패 확인(컴파일 오류) → Step 3 구현 → Step 4 `FAILS=0` → Step 5 커밋 `feat: RateUnitRules — 전투 단위 상태 전환 규칙(순수, 시험 13줄)`.

### Task 2: `RateGroups` 수치 함수 여섯 + 시험

**Files:** Modify `Services/RateGroups.cs`, `tests/.../Program.cs`(`RateGroupsTests` 끝에).

**Interfaces (Produces):** 스펙 3절의 서명. `CooldownRateValue(int original, float factor, long max)`.
- [ ] Step 1 시험(RED):
  ```csharp
  Check("scale value: banker's rounding and cap", RateGroups.ScaleValue(5, 0.5f) == 2 && RateGroups.ScaleValue(7, 0.5f) == 4 && RateGroups.ScaleValue(int.MaxValue, 2f) == int.MaxValue && RateGroups.ScaleValue(0, 2f) == 0 && RateGroups.ScaleValue(-3, 2f) == -3);
  Check("scale day: half rounds away from zero", RateGroups.ScaleDay(5, 0.5f) == 3 && RateGroups.ScaleDay(1, 0.25f) == 0 && RateGroups.ScaleDay(0, 0.5f) == 0);
  Check("scale skill value: 25 at half is 13", RateGroups.ScaleSkillValue(25, 0.5f) == 13 && RateGroups.ScaleSkillValue(-1, 0.5f) == -1);
  Check("cooldown rate value: capped at max", RateGroups.CooldownRateValue(100, 10000f, 100000000) == 1000000 && RateGroups.CooldownRateValue(100, 2000000f, 100000000) == 100000000 && RateGroups.CooldownRateValue(0, 2f, 100) == 0);
  Check("taken damage: never below 1", RateGroups.TakenDamage(1, 0.5f) == 1 && RateGroups.TakenDamage(10, 0.25f) == 2 && RateGroups.TakenDamage(0, 0.5f) == 0);
  Check("dealt damage: negative untouched, overflow capped", RateGroups.DealtDamage(-5, 100) == -5 && RateGroups.DealtDamage(0, 100) == 0 && RateGroups.DealtDamage(30, 10) == 300 && RateGroups.DealtDamage(int.MaxValue, 2) == int.MaxValue);
  ```
- [ ] Step 2 RED → Step 3 구현 → Step 4 GREEN → Step 5 커밋 `feat: RateGroups 수치 함수 여섯(ScaleValue·ScaleDay·ScaleSkillValue·CooldownRateValue·TakenDamage·DealtDamage, 시험 6줄)`.

### Task 3: `RateService`·`BattleService`가 규칙을 쓰게(A2·A4·A5·T1·T3)

**Files:** Modify `Services/RateService.cs`(Scale·ScaleDay·Scaled·CooldownTarget 본문을 호출로, `TickCooldown`·`TickSkillValues`·`TickBattleMove`), `Services/WorldService.cs:220-255`.

- `TickCooldown`: 필드 `skillWritten`·`stepWritten`·`itemWritten` 추가. `ApplyCooldown`이 쓴 값을 돌려준다(`int`), 호출부가 `*Written`에 둔다. 판정: `inBattle = InBattle`, `held = cooldownUnit != Zero`, `applied = skillApplied || itemApplied`, `same = unit.Pointer == cooldownUnit`, `intact = !applied || (skill.baseValue == skillWritten && step.baseValue == stepWritten && item.baseValue == itemWritten)`(applied가 아니면 참). `Step`에 따라: `Skip` return; `Forget` 상태 비우기 return; `Capture` 원래 값 뜨기 뒤 Apply로 이어짐; `Apply` 지금 코드(WritesRate 둘, 끝에 `!any`면 잊기).
- `TickSkillValues`: `SkillValue`에 `Written` 추가(`Track`은 `Written = Original`). 판정: `held = skillUnit != Zero`, `applied = skillValues.Count > 0`, `same`, `intact`는 단위 수준에서는 참으로 주고(항목마다 다름), 같은 단위에서 항목별로 `v.Value.baseValue != v.Written`이면 그 항목을 `done`에 넣어 버린다(되돌리지 않음). 쓸 때 `v.Written = want`.
- `TickBattleMove`: `IsMoveAttached(DynInt)`(allAddCall의 t2 == MoveTag) 추가. 전투 밖: `BattleMove.On`이면 return(상태 유지), 아니면 `moveUnit = Zero; moveSpeedOf = null; moveAdd = 0`. 전투 안: `attached = IsMoveAttached(data.moveSpeed)`; `MoveDetachNeeded(On, attached)`면 `DelValue` 후 잊기; `MoveAttachNeeded(On, attached)`면 `AddValue` 후 기억; 붙어 있으면 `moveAdd = want`와 `moveUnit`·`moveSpeedOf` 갱신(잃었던 추적 되찾기).
- `OnHitCore`: 받는 피해 → `RateGroups.TakenDamage(before, Taken.Value)`; 배수 → `RateGroups.DealtDamage(base, Multiplier)`.
- [ ] Step 1 구현 → Step 2 빌드 0 오류, 단위 시험 FAILS=0 → Step 3 커밋 `fix: 배율 서비스 — 전투 단위 상태를 RateUnitRules로, 이동 처리기 붙어 있는지 확인, 수치는 RateGroups로(13 문서 A2·A4·A5·T1·T3)`.

### Task 4: R1·R7

**Files:** Modify `Services/MartialService.cs:609,863,897`(+`IsPlainSuitRecord`), Create `Services/RateService.Tick.cs`, Modify `Services/RateService.cs`(partial, 틱 코드 이동).
- [ ] Step 1 `IsPlainSuitRecord(string s)` → `Regex.IsMatch(s, "^\\{\"suitID\":\\d+\\}$")`, 세 곳 교체. 단위 시험 1줄은 `MartialService`가 게임 형식에 묶여 링크 불가 — 생략(규칙 함수가 한 줄).
- [ ] Step 2 `RateService.cs`를 `partial`로 바꾸고 627줄(`InBattle`)부터 끝까지의 틱 코드와 관련 필드(`speedApplied`·`mapApplied`·`move*`·`energy*`·`cooldown*`·`skill*`·`CooldownMax`·`CooldownNone`)를 `RateService.Tick.cs`로 옮긴다. `Reset`·`OnDestroy`가 부르는 `RestoreSpeed`·`RestoreMapSpeed`·필드는 partial 안에서 그대로 보인다.
- [ ] Step 3 빌드 0 오류, 단위 시험 FAILS=0, `git diff --stat`로 이동만인지(RateService 두 파일 줄 수 합 ≈ 전) → Step 4 커밋 `chore: 전승 기록 판정 한 함수, RateService를 정의/틱 두 파일로(13 문서 R1·R7)`.

### Task 5: 회차 검증

- [ ] `rsc-a-1030`: `ui-check rates`(첫 검사) → `rates-battle` → `combat`(5분).
- [ ] `rsc-b-1030`: `ui-check rates2-battle` → `mpnone`.
- [ ] `rsc-c-1030`: `trainer4-check items` → `skills`. `rsc-d-1030`: `trainer4-check durable`.
- [ ] 모두 FAIL 0. 아니면 순서 문제인지(앞 부분이 남긴 상태) 코드 문제인지 가려 적는다.

### Task 6: 문서·병합

- [ ] 05 문서 90절(규칙 표, 떼어 낸 함수, 검사 결과, 확인하지 못한 것: 실제 사라짐·주소 재사용은 도구로 못 만듦), 06 문서 1절 줄·8.0절 9번(묶음 2 끝남)·2절 배율 줄(규칙 참조), 13 문서 A2·A4·A5·T1·T3·R1·R7 행에 `고침(90절)`.
- [ ] 커밋, `develop`에 `--no-ff` 병합.
