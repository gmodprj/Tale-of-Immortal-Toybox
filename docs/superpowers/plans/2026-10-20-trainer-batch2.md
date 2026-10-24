# 트레이너 비교 2묶음 구현 계획 (옵션 자리 강제·훔치기·관계 발전·드롭 배수)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 공법 옵션을 자리 밖에도 두 번 누르기로 넣고, 새 탭에서 훔치기·관계 발전 성공과 전투 드롭 배수를 켠다.

**Architecture:**
- **옵션 자리 강제:** `MartialService`의 옵션 규칙을 자리 판정과 나머지로 나눈다. 교체·프리셋 경로에 `force/anySlot` 인자를 더한다.
- **성공률:** 새 서비스 `SuccessService`가 Harmony 접미·앞뒤 표시를 처음 켤 때 한 번 건다. 1묶음 `RateGuards`와 같은 방식이다.
- **화면:** 새 탭 `SuccessTab`(번호 17)을 둔다. 검증은 회차 안의 `ui-check.ps1` 부분으로 한다.

**Tech Stack:** C# 7.3, MelonLoader 0.5.4, HarmonyX, PowerShell 7 검사.

**Spec:** `docs/superpowers/specs/2026-10-20-trainer-batch2-design.md`(7.2절이 최종 범위). 실험 기록은 `docs/05-editor-v0.1.md` 67절에 있다.

## Global Constraints

- 브랜치 `feat/trainer-batch2`(이미 있음)에서 작업하고, 끝나면 `develop`에 `--no-ff`로 병합한다(사용자 확인 뒤).
- 회차는 `session.ps1 -Begin tb2-1020h …`부터 쓴다. 이름은 다시 쓰지 않는다. `-End`는 `SAVE RESTORED AND IDENTICAL`이어야 한다.
- 배포 빌드 `-c Release`는 경고 0이어야 한다. 게임 쓰기는 백업 → 쓰기 → 다시 읽기 → 이력 순서로 한다.
- Vector2·Vector3 인자 메서드와 칸(필드) 게터에는 패치를 걸지 않는다. 원래 인자는 `__0`으로 받는다.
- 화면 글은 한국어로 쓰고, 문구는 스펙 그대로 쓴다. 설정은 저장하지 않는다.
- 검사는 실험 명령 `tb2steal`·`tb2rel`·`tb2npc`·`tb2mlist`·`tb2slot`를 쓴다. `tb2trace`·`tb2set`은 서비스 패치와 겹치므로 검사에서 쓰지 않는다.

## Review Focus

- **NPC끼리의 관계 응답**: `Feedback104`가 NPC↔NPC로 불릴 때는 필요 점수가 바뀌면 안 된다(표시는 요청자가 플레이어일 때만). `success`에서 직접 볼 수 없으니 코드 리뷰로 본다.
- **표시가 남는 경우**: `Feedback104.OnCreate`가 예외로 끝나면 표시가 남는다. 앞 패치에서 매번 다시 정하므로 다음 호출에서 바로잡힌다.
- **드롭 문자열이 빈 경우**: `GetDropID`가 빈 글이나 null을 돌려주면 그대로 둔다.
- **강제 옵션이 든 공법의 옵션 교체 화면**: 다른 자리를 바꿀 때 기존 강제 옵션 때문에 막히면 안 된다(규칙 검사는 바꾸려는 자리만 자리 판정한다).
- **프리셋 검사**: 개발용 생성기의 결과가 달라지면 안 된다(`anySlot` 기본 false).

---

### Task 1: 옵션 자리 강제

**Files:**
- Modify: `src/ModCode/ModMain/Services/MartialService.cs`
  - 214~258: `ReplaceBlockReason`을 `SlotBlocked` + `RuleBlockReason`으로 나눈다.
  - 260~: `ReplacePrefix(…, bool force = false)`
  - 452: `OptionProblem(…, bool anySlot = false)`
  - 498: `Make(…, bool anySlot = false)`
- Modify: `src/ModCode/ModMain/Services/PresetService.cs:280`, `PresetService.Apply.cs:174` — `anySlot: true`를 넘긴다.
- Modify: `src/ModCode/ModMain/UI/Tabs/MartialTab.cs:353-372` — `DrawPrefixRow`
- Modify: `tools/research/ui-check.ps1` — 부분 `prefixforce`

**Interfaces:**
- Produces:
  - `public static bool MartialService.SlotBlocked(ConfBattleSkillPrefixValueItem item, int index)`
  - `public static string MartialService.RuleBlockReason(DataProps.MartialData martial, int index, ConfBattleSkillPrefixValueItem item)`: 중복 한도·선행·배타만 본다.
  - `ReplaceBlockReason`은 남기고 `SlotBlocked`이면 기존 문구를, 아니면 `RuleBlockReason`을 돌려준다.
  - `ReplacePrefix(WorldUnitBase unit, string soleId, int index, ConfBattleSkillPrefixValueItem item, bool force = false)`: `force`이면 자리 판정만 건너뛴다. 이력 항목 끝에 ` (자리 강제)`를 붙인다.

- [ ] **Step 1: 검사 `prefixforce`를 쓴다.**
  - `tb2mlist`에서 `fY5GR9`(14111)를 쓴다. `tb2slot fY5GR9 1`에서 15번이 `w0`이고 조건 `r0`인지 확인한다.
  - 공법 탭 `Tab5`에서 그 공법의 옵션 2 교체 화면을 연다(기존 `actions` 검사의 열기 방식).
  - `ReplaceForce15` 단추가 있고 글이 `강제`인지 본다. 줄 사유 글에 `자리 밖`이 있는지 본다.
  - 한 번 누르면 `uistate`에 `효과가 나지 않을 수 있습니다`가 나오고 옵션은 그대로다.
  - 한 번 더 누르면 `tb2mlist`에서 `1:15:`이고, 이력에 `(자리 강제)`가 있다.
  - 중복 한도 초과 줄(이미 있는 번호 `14`, multi 1)은 `ReplacePick14` 단추가 꺼져 있고 강제 단추가 없다.
  - 예외는 0이어야 한다.
- [ ] **Step 2: 회차 `tb2-1020h`에서 실패를 확인한다.** Expected: `ReplaceForce15` 없음.
- [ ] **Step 3: 서비스를 나누고 `force/anySlot` 인자를 더한다.** `Make`는 `OptionProblem(type, baseId, options, anySlot)`를 부른다. 프리셋 두 곳은 `true`를 넘긴다.
- [ ] **Step 4: `DrawPrefixRow`를 고친다.**
  - 자리만 막힌 줄(`SlotBlocked && RuleBlockReason == null`): 경고 글 `"자리 밖: 이 자리에는 나오지 않는 옵션(강제로 넣을 수 있음)"`을 보이고, `Confirm.Button("ReplaceForce" + number, row, "강제", "정말 강제", …, "prefix-force-" + number, 문구, () => ReplacePrefix(…, true))`을 둔다.
  - 문구: `"옵션 n번은 이 자리에 나오지 않는 옵션입니다. 게임이 원래 만들지 않는 조합이라 효과가 나지 않을 수 있습니다. 한 번 더 누르면 넣습니다."`
  - 그 밖의 막힌 줄은 지금처럼 둔다.
- [ ] **Step 5: 빌드(경고 0)하고 회차 `tb2-1020i`에서 `prefixforce`, `actions`를 돌린다.** `preset-check.ps1 -Part ui`는 회차 `tb2-1020j`에서 돌린다. Expected: 모두 `FAILS=0`.
- [ ] **Step 6: 커밋한다.** `feat: 공법 옵션 자리 강제 교체(두 번 누르기)와 프리셋의 자리 판정 완화, 검사 prefixforce`

### Task 2: 성공·드롭 탭

**Files:**
- Create: `src/ModCode/ModMain/Services/SuccessService.cs`
- Create: `src/ModCode/ModMain/UI/Tabs/SuccessTab.cs`
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs:481-487`(탭 목록에 `new SuccessTab()`, `tabNumbers`에 17)
- Modify: `src/ModCode/ModMain/UI/StatusOverlay.cs:44` 근처(켠 줄 표시)
- Modify: `src/ModCode/ModMain/Dev/DevCommandsTrainerBatch2.cs`
  - `tb2relof <npc 번호>`: 플레이어와의 관계 종류(`relationData.GetRelation`)
  - `sstate`: `SuccessService` 상태와 `DropIds`(바뀐 뒤 보상 번호 수 합)·`DropCalls`
- Modify: `tools/research/ui-check.ps1`
  - 부분 `success`
  - `$tabProbe`에 `@(17, 'SuccessReset')`
  - `tabs`가 탭 수를 세면 그 값

**Interfaces:**
- Produces:
  - `internal static class SuccessService`
  - `public static bool Steal, Relation`
  - `public static int DropTimes`(1·2·3·5)
  - `public static readonly int[] DropSteps = { 1, 2, 3, 5 }`
  - `public static string SetSteal(bool on)`, `SetRelation(bool on)`, `NextDrop()`, `Reset()`: 결과 문장을 돌려주고 `ChangeLog.Add("성공·드롭", …)`로 이력을 남긴다.
  - 개발 계수 `#if DEVTOOLS public static int DropCalls, DropIds; #endif`
  - 패치는 처음 켤 때 줄별로 한 번 건다.

- [ ] **Step 1: 검사 `success`를 쓴다.**
  - `Tab17`이 있고, `SuccessSteal`·`SuccessRelation`·`SuccessDrop`·`SuccessReset`이 그려진다.
  - **훔치기:** `SuccessSteal`을 켠다(`켜짐`). 가방 칸 수를 잰다. `tb2steal 1`을 하고 결과 창이 뜰 때까지 기다린다(`ActionCourseInfo`). 확인(`click 958 802`)을 누르면 `GetReward`가 열리고 가방 칸이 +1이다. 은폐도 성공이라 실패 대화가 없다.
  - **관계 발전:** `SuccessRelation`을 켠다. `tb2rel 2 3`을 하고 대화를 넘긴다. 결과 창의 확인(`click 958 714`), 알림의 예(`click 958 539`)를 누른다. `tb2relof 2`가 `Brother`를 포함한다.
  - **드롭:** `SuccessDrop`을 x3으로 둔다. 주는 피해를 x100으로 두고 `fight 1011 1`을 연다. 적 몇을 처치한 뒤 `sstate`에서 `DropIds/DropCalls`가 9 근처(정확히는 원래 3의 3배)인지 본다. x1로 되돌려 같은 값이 3인지는 같은 전투의 다음 처치로 본다. 그 뒤 `bend`와 결과 창.
  - `SuccessReset`을 누르면 셋 다 꺼진다. 위쪽 표시는 켜 둔 동안 `훔치기`를 포함한다.
  - 예외 0, 모드 경고 0.
- [ ] **Step 2: 회차 `tb2-1020k`에서 실패를 확인한다.** Expected: `Tab17` 없음.
- [ ] **Step 3: `SuccessService`를 구현한다.**
  - 접미 `StealRate(WorldUnitBase __0, ref float __result)` → 1, `StealDiscover(WorldUnitBase __0, ref float __result)` → 0. 둘 다 `Steal`이고 `__0`이 플레이어일 때만이다.
  - `Fb104Begin(UnitActionFeedback104 __instance)`에서 `inPlayerRequest = Relation && requestUnit이 플레이어`로 정하고, `Fb104End`에서 false로 둔다.
  - 게터 접미 `RequireScore(ref int __result)` → -100000, `RequireReputation(ref int __result)` → 0. 둘 다 `inPlayerRequest`일 때만이다.
  - `DropId(ref string __result)`: `DropTimes>1`이고 결과가 비어 있지 않으면 n번 이어 붙인다. 개발 계수를 센다.
  - 가드 안의 예외는 잡고, 경고는 한 번만 남긴다(`[TlToybox] 성공·드롭 실패: …`).
- [ ] **Step 4: `SuccessTab`(제목 `성공·드롭`)을 쓴다.**
  - 줄 꼴은 `RateTab.Row`와 같게 이름·단추·설명으로 한다.
  - 설명:
    - 훔치기: "플레이어의 훔치기가 늘 성공하고 들키지 않습니다."
    - 관계 발전: "플레이어가 청한 관계(의형제·도려·부부·사제 등)를 NPC가 받아들입니다. 친밀도·평판 조건도 넘습니다."
    - 드롭: "전투에서 적이 죽을 때 드롭을 굴리는 횟수를 곱합니다."
  - 아래 안내는 스펙 7.2절 문구를 쓴다.
  - 탭을 등록하고 `StatusOverlay`에 켠 줄 이름을 더한다.
- [ ] **Step 5: 빌드(경고 0)하고 회차 `tb2-1020l`에서 `success`, `tabs`, `base`를 돌린다.** Expected: `FAILS=0`.
- [ ] **Step 6: 커밋한다.** `feat: 성공·드롭 탭(훔치기·관계 발전 성공, 전투 드롭 배수)과 검사 success`

### Task 3: 문서·마무리

- [ ] **Step 1: 마무리 회차 `tb2-1020m`에서 `items`·`numbers`·`history`·`rates`를 돌린다.** Expected: 모두 `FAILS=0`.
- [ ] **Step 2: 문서를 고친다.**
  - 05 문서 68절: 규칙·실측·검사·한계, 초빙·포획·도주는 67절을 가리킨다.
  - 06 인계:
    - 2절: 공법 탭 강제, 새 탭 행, 탭 줄 16개
    - 5절: 검사 `prefixforce`·`success`
    - 6절: 칸 게터 패치 불가, `AccessTools`가 부모 메서드를 찾는 함정
    - 8.1절
  - 11 비교 문서의 해당 줄을 고친다.
- [ ] **Step 3: 커밋하고, 마지막 리뷰를 맡기고, 병합은 사용자 확인 뒤에 한다.**
