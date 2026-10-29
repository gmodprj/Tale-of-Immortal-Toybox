# 트레이너 4차 전투 묶음 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 이 저장소에서는 executing-plans(이 세션에서 직접)로 하고, 진행 기록은 `.superpowers/sdd/2026-10-28-trainer4-battle/progress.md`(끝나면 지움).

**Goal:** 배율 탭에서 기술 쿨타임이 선법·영역에도 걸리고, 전투 아이템 쿨타임·법보 내구 소모 줄이 새로 생기고, 주는 피해에 일격 단계가 생긴다.

**Architecture:** 기존 `RateService`의 두 길(전투 단위 비율 `TickCooldown`, 기술마다의 값 `TickSkillValues`/`Track`)에 얹고, 일격은 `BattleService.OnHitCore`, 법보 내구는 `RateGuards`의 새 접두(`PropsArtifact.set_durable`). 게임 형식을 쓰지 않는 계산은 `RateGroups`에 두어 단위 시험에 링크한다.

**Tech Stack:** C# (net472), MelonLoader 0.5.4, HarmonyX, Unhollower(IL2CPP). 단위 시험 `tests/ModMain.Tests`(자체 `Check`).

**Spec:** `docs/superpowers/specs/2026-10-28-trainer4-battle-design.md`

## Global Constraints

- 브랜치 `feat/trainer4-battle`(develop에서). 병합은 사용자 확인 뒤 `--no-ff`.
- 모두 저장하지 않는다. 게임을 켤 때·세이브를 떠날 때 끈다. `모두 1배로`가 새 줄과 일격도 끈다.
- 플레이어 것에만 건다(일격: 공격자가 플레이어인 타격 / 내구: soleID가 플레이어 `allProps`에 있는 법보).
- 단계: 기술 쿨타임·전투 아이템 쿨타임 x1·0.5·0.25·0.1·없음, 법보 내구 소모 x1·없음, 주는 피해 x1·2·5·10·100·일격.
- 기술마다의 쿨타임 "없음" = 하한 100(밀리초). 원래 값 0 이하는 건드리지 않는다.
- 일격 값 = 대상 `hp + maxHP.value`, int 최댓값으로 자름. `ClearCall`을 부르지 않는다.
- 전투 칩 8줄: 기술 쿨타임, 전투 아이템 쿨타임, 신기 쿨타임, 영력 소모, 집중력 소모, 주는 피해, 받는 피해, 무적. 전투 이동 속도는 시간·속도 칩(지도 속도, 전투 속도 다음)으로.
- 새 단추 이름 `RateItemCooldown`, `RateArtifactDurable`. 키 `itemCooldown`, `artifactDurable`. Short `아이템 쿨타임`, `법보 내구`. 일격 표시 `일격`, 화면 위 `피해 일격`.
- 줄 설명은 스펙 3절 문구.
- 사용자에게 보이는 글은 한국어. 두 빌드 구성(개발, `-p:DevTools=false`) 모두 컴파일.
- 게임 검증은 `session.ps1` 회차(`-Fixture base -Exclude 2814696167`), 회차 이름은 새로.

## Review Focus

1. 1배로 돌렸을 때 선법·영역 `cd`와 `itemCDRate`가 원래 값으로 돌아온다 → Task 3·4의 검사 줄.
2. 같은 전투에서 쿨타임 줄을 켰다 껐다 다시 켜도 배율이 겹치지 않는다(원래 값을 곱한 값으로 다시 뜨지 않음) → Task 4 검사에 "켬→끔→켬 뒤 cd = 원래×배율".
3. 일격은 플레이어가 맞는 타격에 걸리지 않는다(무적을 끈 채 일격만 켜도 플레이어 hp가 한 방에 사라지지 않음) → Task 2 단위 시험(대상이 플레이어면 값 그대로를 판정하는 함수)과 Task 5 검사.
4. 내구 접두가 플레이어 것이 아닌 법보(soleID가 allProps에 없음)는 막지 않는다 → Task 6 단위 시험(판정 함수)과 개발 명령.
5. 수리(늘리는 쓰기)는 없음에서도 된다 → Task 6 검사(편집기 법보 탭 수리가 통과).

---

### Task 1: 영역에 단위 비율이 듣는지 실측

스펙 4절: 영역을 기술마다의 길에 넣을지 이 결과로 정한다.

**Files:** 없음(실험 브랜치 `feat/trainer4-research`의 DLL로 회차만).

- [ ] **Step 1:** `git checkout feat/trainer4-research`, 개발 빌드, 회차 `t4-d-1028` 열기.
- [ ] **Step 2:** `t4field 1 300`, 전투(수준 5), `t4cd`로 영역 `cdTime` 190 확인 → 편집기 배율 탭 `RateCooldown`을 네 번(없음) → `t4cd`.
  Expected: 영역 `cdTime`이 190 그대로면 "듣지 않음", 0에 가까우면 "들음".
- [ ] **Step 3:** 회차 닫기(`SAVE RESTORED AND IDENTICAL`), `git checkout feat/trainer4-battle`로 돌아오기. 결과를 스펙 2절 E4에 한 줄 적고 커밋. "들음"이면 Task 4에서 영역을 뺀다.

### Task 2: 순수 계산 — 칩, 기술 쿨타임 하한, 일격 값, 내구 판정

**Files:**
- Modify: `src/ModCode/ModMain/Services/RateGroups.cs`
- Test: `tests/ModMain.Tests/Program.cs` (`RateGroupsTests`)

**Interfaces — Produces:**
- `RateGroups.Groups`: 성장 `{exp, martial, studyGain}`, 시간·속도 `{mapSpeed, speed, battleMove, move, actionDay}`, 전투 `{cooldown, itemCooldown, artifactCooldown, mpCost, spCost, multiplier, taken, god}`, 관계·소모 `{intimGain, energyCost, itemUse, artifactDurable}`.
- `public const int SkillCooldownFloor = 100;`
- `public static int SkillCooldown(int original, float rate)` — original ≤ 0이면 original. rate ≥ 1이면 original. rate == 0이면 `Math.Min(original, SkillCooldownFloor)`. 그 밖은 `max(SkillCooldownFloor와 original 중 작은 것, round(original×rate, AwayFromZero))`.
- `public static int OneHitDamage(int hp, int maxHp)` — `(long)max(0,hp) + max(0,maxHp)`를 int 최댓값으로 자름, 최소 1.
- `public static bool DurableBlocked(int now, int next, bool owned, bool on)` — `on && owned && next < now`.

- [ ] **Step 1: 실패하는 시험** — `RateGroupsTests`의 `all` 배열에 `"itemCooldown", "artifactDurable"`을 더하고(20줄), 아래를 더한다:

```csharp
Check("rate groups: battle move is in time and speed", RateGroups.GroupOf("battleMove") == 1);
Check("rate groups: battle chip keeps eight rows", RateGroups.Groups[2].Keys.Length == 8 && RateGroups.GroupOf("itemCooldown") == 2);
Check("rate groups: artifact durable is in life", RateGroups.GroupOf("artifactDurable") == 3);
Check("skill cooldown: half rounds away", RateGroups.SkillCooldown(60000, 0.5f) == 30000 && RateGroups.SkillCooldown(250, 0.5f) == 125 && RateGroups.SkillCooldown(150, 0.5f) == 100);
Check("skill cooldown: none is the floor", RateGroups.SkillCooldown(190000, 0f) == 100);
Check("skill cooldown: a short one is not lengthened", RateGroups.SkillCooldown(50, 0f) == 50 && RateGroups.SkillCooldown(50, 0.5f) == 50);
Check("skill cooldown: zero or less is kept", RateGroups.SkillCooldown(0, 0f) == 0 && RateGroups.SkillCooldown(-10000, 0.5f) == -10000);
Check("skill cooldown: x1 keeps", RateGroups.SkillCooldown(365, 1f) == 365);
Check("one hit: hp plus max", RateGroups.OneHitDamage(29877, 29877) == 59754);
Check("one hit: overflow is cut", RateGroups.OneHitDamage(int.MaxValue, 5) == int.MaxValue);
Check("one hit: never zero", RateGroups.OneHitDamage(0, 0) == 1);
Check("durable: only a drop on an owned artifact is blocked", RateGroups.DurableBlocked(100, 98, true, true) && !RateGroups.DurableBlocked(100, 98, false, true) && !RateGroups.DurableBlocked(98, 100, true, true) && !RateGroups.DurableBlocked(100, 98, true, false));
```
  기존 줄 `"rate groups: new rows are in their chips"`의 `battleMove == 2`를 `== 1`로 고친다.- [ ] **Step 2:** `dotnet run --project tests/ModMain.Tests -c Release` → 새 줄 FAIL(컴파일 오류 포함).
- [ ] **Step 3:** 위 Interfaces대로 `RateGroups.cs`를 고친다.
- [ ] **Step 4:** 시험 다시 → 모두 PASS.
- [ ] **Step 5:** 커밋 `feat: 배율 칩 재배치와 쿨타임 하한·일격·내구 판정 계산`.

### Task 3: 전투 아이템 쿨타임 줄

**Files:**
- Modify: `src/ModCode/ModMain/Services/RateService.cs`

**Interfaces:**
- Produces: `public static readonly Rate ItemCooldown` (Key `itemCooldown`, Label `전투 아이템 쿨타임`, Short `아이템 쿨타임`, Steps `{1, 0.5, 0.25, 0.1, 0}`, ZeroText `없음`, Note 스펙 3절), `All`에 `Cooldown` 다음으로.

- [ ] **Step 1:** `ItemCooldown`을 정의하고 `All`에 넣는다.
- [ ] **Step 2:** `TickCooldown`이 `itemCDRate`도 맡게 한다: 떠 둔 값 `cooldownItem`(int), 진입 조건 `Cooldown.On || ItemCooldown.On || cooldownUnit != Zero`. 단위가 바뀌면 세 비율을 모두 뜨고, 줄마다 켜져 있으면 `CooldownTarget(원래, 1/배율 또는 CooldownNone)`, 꺼져 있으면 원래 값으로. 둘 다 꺼지면 되돌리고 `cooldownUnit`을 잊는다. 예외는 켜진 줄로 `Warn`.
- [ ] **Step 3:** 개발 빌드 컴파일 → 오류 0.
- [ ] **Step 4:** 커밋 `feat: 배율 탭 전투 아이템 쿨타임(itemCDRate)`.

### Task 4: 기술 쿨타임을 선법·영역으로 넓히기

**Files:**
- Modify: `src/ModCode/ModMain/Services/RateService.cs` (`Cooldown.Note`, `TickSkillValues`, `Collect`)

**Interfaces — Consumes:** `RateGroups.SkillCooldown(int, float)`.

- [ ] **Step 1:** `Cooldown.Note`를 스펙 3절 문구로.
- [ ] **Step 2:** `TickSkillValues`의 `any`에 `Cooldown.On`을 더하고, `Collect`에서 `Cooldown.On`이면 `unit.immortalSkill`마다 `data.cd`, (Task 1이 "듣지 않음"이면) `unit.fieldSkill.data.cd`를 `Track(…, Cooldown)`.
- [ ] **Step 3:** 원래 값 → 목표 값 계산을 줄에 따라 나눈다: `v.Rate == Cooldown`이면 `RateGroups.SkillCooldown(v.Original, v.Rate.Value)`, 그 밖은 지금의 `Scaled`.
- [ ] **Step 4:** 컴파일 → 오류 0. 커밋 `feat: 기술 쿨타임이 선법·영역 기술에도`.

### Task 5: 일격

**Files:**
- Modify: `src/ModCode/ModMain/Services/WorldService.cs` (`BattleService`), `src/ModCode/ModMain/UI/Tabs/RateTab.cs`, `src/ModCode/ModMain/UI/StatusOverlay.cs`, `src/ModCode/ModMain/Dev/DevCommandsRate.cs`(rstate의 `damage=` 값을 `DamageText`로)

**Interfaces — Produces:**
- `BattleService.OneHit` (`public const int OneHit = -1`), `Multipliers = {1, 2, 5, 10, 100, OneHit}`.
- `public static bool DamageOn` (`Multiplier != 1`), `public static string DamageText` (`일격` 또는 `x`+n).
- **Consumes:** `RateGroups.OneHitDamage(int, int)`.

- [ ] **Step 1:** `Multiplier > 1`을 보던 곳을 `DamageOn`으로(`Wanted`, `OnHitCore`의 조건, `RateTab.IsOn`·`DrawRow`, `StatusOverlay` `"피해 " + DamageText`). 변경 이력의 값 글도 `DamageText`.
- [ ] **Step 2:** `OnHitCore`: 공격자가 플레이어이고 `Multiplier == OneHit`이면 `dynV.baseValue = RateGroups.OneHitDamage(hitUnit.data.hp, hitUnit.data.maxHP.value)`, 아니면 지금의 곱하기. 대상이 플레이어인 타격은 지금처럼 그 앞에서 돌아간다.
- [ ] **Step 3:** 주는 피해 줄 설명을 스펙 3절 문구로.
- [ ] **Step 4:** 두 구성 컴파일 → 오류 0. 커밋 `feat: 주는 피해 일격 단계`.

### Task 6: 법보 내구 소모 줄

**Files:**
- Modify: `src/ModCode/ModMain/Services/RateService.cs`, `src/ModCode/ModMain/Services/RateGuards.cs`

**Interfaces:**
- Produces: `RateService.ArtifactDurable` (Key `artifactDurable`, Label `법보 내구 소모`, Short `법보 내구`, Steps `{1, 0}`, ZeroText `없음`, Note 스펙 3절), `All` 끝에. `RateGuards.PatchDurable(Harmony h)`, `RateGuards.DurableGuard(DataProps.PropsArtifact __instance, ref int __0)`, `RateGuards.OwnedByPlayer(DataProps.PropsArtifact art) : bool`.
- Consumes: `RateGroups.DurableBlocked(int, int, bool, bool)`.

- [ ] **Step 1:** `PatchDurable`: `typeof(DataProps.PropsArtifact).GetProperty("durable").GetSetMethod()`에 `DurableGuard` 접두(없으면 `MissingMethodException`).
- [ ] **Step 2:** `DurableGuard`: `ArtifactDurable.On`이 아니면 바로 돌아간다. `now = __instance.durable`, 줄어들 때만 `OwnedByPlayer`(art.data.soleID를 `g.world.playerUnit.data.unitData.propData.allProps`의 soleID와 비교)를 보고, `DurableBlocked`이면 `__0 = now`. 예외는 삼키고 한 번만 로그.
- [ ] **Step 3:** `EnsurePatched`에서 `rate == ArtifactDurable`이면 `PatchWith(rate, RateGuards.PatchDurable)`.
- [ ] **Step 4:** 컴파일 → 오류 0. 커밋 `feat: 배율 탭 법보 내구 소모 없음`.

### Task 7: 개발 명령과 검사 스크립트, 게임 검증

**Files:**
- Create: `src/ModCode/ModMain/Dev/DevCommandsTrainer4.cs` (실험 브랜치의 것을 다듬어: `cdall`(쿨타임 전부 읽기), `icast <i>`, `fcast`, `iequip <카드> <칸>`, `fieldset <main1> <dp>`, `bhp`(적 수·일격 마지막 대상의 hp·die), `artdur`(플레이어 법보 soleID·내구, 내구 접두 호출·막은 수)). `DevCommands.cs`에 연결. 실험용 `t4set`·`t4kill`·`t4durtrace`는 넣지 않는다. 일격 마지막 대상은 `BattleService`에 `#if DEVTOOLS`로 `DevKillTarget`.
- Create: `tools/research/trainer4-check.ps1` (부분 `ui`, `items`, `skills`, `onehit`, `durable`)
- Modify: `tools/research/ui-check.ps1:2161-2175` (전투 이동 속도가 `RateGroup1`로: 전투 칩 8줄 판정은 `RateItemCooldown`을 넣고 `RateBattleMove`를 빼고, 켜 둔 것 수 `전투 1` → 시간·속도 쪽으로)

- [ ] **Step 1:** 개발 명령 작성·연결, 컴파일.
- [ ] **Step 2:** 검사 스크립트. 판정:
  - `ui`: 칩 1에 `RateBattleMove`, 칩 2에 8줄(`RateItemCooldown` 포함, `RateBattleMove` 없음), 칩 3에 `RateArtifactDurable`; 설명 글(`RateNoteItemCooldown` 등)이 스펙 문구; 주는 피해를 다섯 번 눌러 `일격`, 한 번 더 `x1`; 켜 둔 것 칩과 `uistate`의 `피해 일격`.
  - `items`: 전투(수준 5)에서 x1이면 단약 두 번째 거부, 아이템 쿨타임 없음에서 같은 단약 연달아 둘, 법보 둘; x1로 돌리면 `cdall`의 `item=100/100`.
  - `skills`: `setgrade 40`, `iequip 5000 0`, `fieldset 1 300`, 전투; 쿨타임 없음에서 선법 cd = 100, 선법 발동 뒤 6초 안에 다시 발동; 영역(충전 기다림) 두 번 발동; 끔→켬(x0.5)에서 선법 cd 30000(겹치지 않음); x1이면 60000·190000.
  - `onehit`: 무적, 수준 60, 일격, `bcast 1`로 맞힌 대상 die; 무적을 끄고 일격만 켠 채 5초 맞은 뒤 플레이어 생존(`battle stat` hp>0).
  - `durable`: 법보 지급·장착, 없음에서 전투 처음 쓰기 뒤 100/100, `artdur`의 막은 수 1; x1에서 새 전투 처음 쓰기 뒤 98; 편집기 법보 탭 수리로 100(없음 켠 채).
- [ ] **Step 3:** 단위 시험 PASS, 두 구성 컴파일.
- [ ] **Step 4:** 회차(새 이름)마다 한 부분씩 또는 묶어 돌려 FAIL 0. 기존 `rates`, `rates2`(ui-check), `combat`, `rates-battle`, `itemuse`, `mpnone`도 FAIL 0(깨끗한 회차로 나눠).
- [ ] **Step 5:** 커밋 `test: 트레이너 4차 개발 명령과 검사(trainer4-check)`.

### Task 8: 문서

**Files:**
- Modify: `docs/05-editor-v0.1.md`(87절 새로: 실험 표, 동작, 검증 결과, 확인하지 못한 것), `docs/12-review-2026-10-22.md` 5절 표(쿨타임·일격·내구 → 있음(87절)), `docs/06-handoff.md`(머리 요약, 2절 기능 지도의 배율 탭, 5절 검사 표에 `trainer4-check`, 8.0절)

- [ ] **Step 1:** 문서 고침. 클래스·필드 이름은 실험에서 확인한 것만.
- [ ] **Step 2:** 커밋 `docs: 05 87절, 12·06 문서 트레이너 4차 반영`.
