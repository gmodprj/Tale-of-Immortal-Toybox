# 트레이너 비교 1묶음 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 편집기에 넷을 더한다. 성격 편집, 아이템 소모 없음, 가방 수량 지정·버리기, 영력·집중력 소모 없음(전투 중 모든 감소)이다.

**Architecture:** 순수 계산(가방 줄이기 순서, 성격 검사)은 게임 형식 없는 파일에 두고 `tests/ModMain.Tests`로 시험한다. 게임 쓰기는 `EditService`·`BagService`에 둔다. 소모 막기 패치는 새 파일 `Services/RateGuards.cs`에 두고, `RateService`의 줄과 `EnsurePatched`에서 부른다. 화면은 기존 능력치·자원·배율 탭에 더한다. 각 기능은 회차 안의 `ui-check.ps1` 부분으로 게임에서 확인한다.

**Tech Stack:** C# 7.3(net472, MelonLoader 0.5.4, 언홀로어 IL2CPP 프록시), HarmonyX, PowerShell 7 검사, `tests/ModMain.Tests`(net8.0 콘솔).

**Spec:** `docs/superpowers/specs/2026-10-20-trainer-batch1-design.md`. 실험 기록은 `docs/05-editor-v0.1.md` 65절이다.

## Global Constraints

- 브랜치 `feat/trainer-batch1`(이미 있음)에서 작업한다. 끝나면 `develop`에 `git merge --no-ff`. `main`·`develop`에 직접 커밋하지 않는다.
- 게임 검증은 `pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서 한다. 회차 이름은 다시 쓰지 않는다(`tb1-1020d`부터 차례로). `-End`는 `SAVE RESTORED AND IDENTICAL`이어야 한다. 사용자의 게임이 켜져 있으면 멈추고 알린다.
- 빌드: `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release`(배포 구성, 경고 0)와 `-c Release -p:DevTools=true`(개발 구성은 회차가 스스로 만든다).
- 게임 쓰기는 모두 `SaveBackup.EnsureOnce()` → 쓰기 → 다시 읽어 판정 → `ChangeLog.Add(…)` 순서로 한다.
- 화면 글과 결과 문장은 한국어로 쓴다. 문구는 스펙 5절의 것을 그대로 쓴다.
- Vector2 등 값 형식 인자를 받는 게임 메서드에는 Harmony 패치를 걸지 않는다(05 문서 65절: 게임이 꺼진다).
- Harmony 접두에서 원래 인자는 자리 이름(`__0`, `__1`)으로 받는다.
- 설정은 저장하지 않는다. 배율 줄은 켤 때마다 x1에서 시작한다.
- 새 `[TlToybox] ` 로그 줄을 내면 `check-lib.ps1`의 `ModWarnings` 제외 목록에 넣는다.
- 검사는 실험 명령 `Dev/DevCommandsTrainerBatch1.cs`(`tbuse`·`tbbuse`·`tbforge`·`tbtraits`·`tbequips`·`tbmp`)를 쓴다. 이 파일은 남긴다. 다만 `tbtrace`·`tbblock`은 서비스 패치와 겹치므로 검사에서 부르지 않는다.

## Review Focus

- **장착품을 지우는 줄이기**: 법보를 장착한 채 같은 번호를 0으로 하거나 버리면, 장착 법보가 남고 장착 칸이 그대로여야 한다(`DelProps(id, n)`은 장착품부터 지운다). 시험은 Task 1(순서)과 Task 2(게임)에 있다.
- **전투 아이템 칸의 단약 묶음**: 칸에 넣은 단약(고정본 1011052 ×7)을 3으로 줄이면 묶음은 칸에 남은 채 3이 되고, 0으로 하면 1이 남고 "장착 중인 1개는 그대로" 문장이 나와야 한다. 시험은 Task 1과 Task 2에 있다.
- **아이템 소모 없음이 영석까지 막는 경우**: 비급 공부에서 영석 900은 나가야 한다(쓰는 sole만 막는다). 시험은 Task 4에 있다.
- **막기 표시가 남는 경우**: 쓰기 행동이 예외로 끝나 기억한 sole이 남으면, 다음 제작 재료가 막힐 수 있다. 다음 `OnCreate` 앞에서 비우고, 지우는 것은 뒤 패치에서 한다. 연기 재료가 줄어드는지 Task 4에서 시험한다.
- **NPC 성격 칸에 목록 밖 번호**: `목록에 없는 번호 n`이 보이고, 칩으로만 바꿀 수 있어야 한다. 순수 검사는 Task 3에 있다.

---

### Task 1: 가방 줄이기 순서(순수 계산)

**Files:**
- Create: `src/ModCode/ModMain/Services/BagPlan.cs`
- Modify: `tests/ModMain.Tests/ModMain.Tests.csproj`(`<Compile Include="..\..\src\ModCode\ModMain\Services\BagPlan.cs" Link="Linked\BagPlan.cs" />`)
- Modify: `tests/ModMain.Tests/Program.cs`(`BagPlanTests()`를 `Main`에서 부름)

**Interfaces:**
- Produces:
  - `internal static class BagPlan`
  - `public sealed class Stack { public string Sole; public int Count; public bool Equipped; public int Strength; }`. `Strength`는 법보의 `level+grade+옵션 수`, 그 밖은 0이다.
  - `public sealed class Cut { public string Sole; public int Count; public bool All; }`. `All`이면 `DelProps(sole)`, 아니면 `DelProps(sole, Count)`로 지운다.
  - `public static List<Cut> Reduce(List<Stack> stacks, bool overlay, int remove, out int kept)`
    - 뜻: `remove`개를 줄이는 계획이다. `kept`는 장착품 때문에 줄이지 못한 수다.
    - 순서:
      1. 장착하지 않은 줄부터 줄인다.
      2. 겹치지 않는 아이템은 `Strength` 오름차순으로 지우고, 같으면 입력 순서를 따른다.
      3. 겹치는 아이템은 입력 순서로 줄인다. 묶음을 다 비우면 `All`이다.
      4. 그래도 남으면 장착한 겹치는 묶음을 1개까지만 줄인다.
      5. 장착한 겹치지 않는 아이템은 줄이지 않는다.
  - `public static int StrongCount(List<Stack> stacks)`: 장착하지 않은 줄 가운데 `Strength > 기본` 인 수. 기본은 `Strength`의 최솟값이다. 버리기 확인 문구의 "강화·옵션이 붙은 것 n개"에 쓴다.

- [ ] **Step 1: 실패하는 시험을 쓴다** (`Program.cs`의 `BagPlanTests`, 기존 `Check(name, ok, detail)`를 쓴다)
  - `bagplan overlay plain`: 묶음 하나 `{A,10,false}`, remove 4 → `[A x4]`, kept 0.
  - `bagplan overlay all`: `{A,10,false}`, remove 10 → `[A All]`.
  - `bagplan overlay equipped keeps one`: `{A,7,true}`, remove 7 → `[A x6]`, kept 1.
  - `bagplan overlay free first`: `{E,5,true},{F,3,false}`, remove 5 → `[F All, E x2]`, kept 0.
  - `bagplan artifact weak first`: 겹치지 않음 `{X,1,false,S=9},{Y,1,false,S=3},{Z,1,true,S=1}`, remove 2 → `[Y All, X All]`, kept 0.
  - `bagplan artifact equipped kept`: 같은 세 줄, remove 3 → `[Y All, X All]`, kept 1.
  - `bagplan strong count`: `{X,S=9},{Y,S=3},{W,S=3}` → 1.
- [ ] **Step 2: 실패를 확인한다.** Run: `dotnet run --project tests/ModMain.Tests -c Release`. Expected: 빌드 실패(BagPlan 없음).
- [ ] **Step 3: `BagPlan.Reduce`와 `StrongCount`를 구현한다.** 게임 형식을 쓰지 않는다. `CraftPlan.cs`와 같은 머리 주석을 단다(스펙 6절, 05 문서 65절).
- [ ] **Step 4: 통과를 확인한다.** Run: 같은 명령. Expected: 새 줄 일곱 개 PASS, `FAILS=0`.
- [ ] **Step 5: 커밋한다.** `git add src/ModCode/ModMain/Services/BagPlan.cs tests/ModMain.Tests` → `git commit -m "feat: 가방 줄이기 순서 계산(BagPlan)과 시험"`

### Task 2: 가방 수량 지정·버리기(서비스·자원 탭·검사)

**Files:**
- Modify: `src/ModCode/ModMain/Services/BagService.cs`(가방 칸 코드 옆에 수량 지정 둘을 더함)
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs:551-581`(`DrawItemRow`)
- Modify: `tools/research/ui-check.ps1`(부분 `bagcount`)

**Interfaces:**
- Consumes:
  - `BagPlan.Reduce`, `BagPlan.StrongCount`(Task 1)
  - `EditService.ItemName(int)`, `EditService.NameOf(unit)`
- Produces:
  - `public static string BagService.SetCount(WorldUnitBase unit, int propsId, int n)`: 결과 문장을 돌려준다.
  - `public static string BagService.Drop(WorldUnitBase unit, int propsId)`: 결과 문장을 돌려준다.
  - `public static string BagService.DropQuestion(WorldUnitBase unit, int propsId)`: 확인 문구를 돌려준다.

- [ ] **Step 1: 검사 부분 `bagcount`를 먼저 쓴다.** 회차 안에서만 돈다. 확인 항목:
  - 자원 탭 `Tab1`에서 단약 1011011 줄을 찾는다(검색 `ui set` 사용). `ResOwned1011011`이 숫자 칸이고 지금 수 `10`인지 본다.
  - 칸에 25를 쓰고 Enter를 누르면 `count 1011011`이 25다. 7을 쓰면 7이다. 0을 쓰면 0이다.
  - 이력에 `아이템 6품 보원단(1011011)` 줄이 생긴다.
  - 전투 칸 단약 1011052(×7, 장착)에 3을 쓰면 3이 되고 `tbequips`에 그 sole이 남는다. 0을 쓰면 1이 남고, 결과 문장에 `장착 중인 1개는 그대로`가 들어 있다.
  - 법보 3031101: `trgive` 세 번, `trequip <첫 sole>`. `ResDrop3031101`을 한 번 누르면 확인 문구가 나오고(`uistate`), 수는 그대로다. 한 번 더 누르면 수가 1이고, `tr`에 장착 법보가 남는다.
  - 예외 0(`Traps`).
- [ ] **Step 2: 회차를 열고 검사가 실패하는지 본다.** Run: `session.ps1 -Begin tb1-1020d -Fixture base` → `ui-check.ps1 -Part bagcount`. Expected: `ResOwned1011011` 없음으로 FAIL. 그 뒤 `-End`.
- [ ] **Step 3: `BagService.SetCount`·`Drop`·`DropQuestion`을 구현한다.**
  - 입력 범위는 0~99999다. 밖이면 `"<이름>: 수량은 0~99999만 쓸 수 있습니다."`를 돌려준다.
  - 같은 수면 `"<이름>: 이미 n개입니다."`를 돌려준다.
  - 늘리기: 기존 `EditService.TryGiveItem`을 쓴다. 문장도 그대로다.
  - 줄이기:
    - 입력: `propData.allProps`에서 그 번호의 줄을 고르고, `GetAllEquipPropsSoleID()`로 장착을 표시한다. 겹침은 `g.conf.itemProps.GetItem(id).isOverlay == 1`로 본다. 법보는 `SpriteService.IsArtifact`이면 `To<DataProps.PropsArtifact>()`의 `level+grade+GetPrefixCount()`를 `Strength`로 넣는다.
    - `Cut`마다 `All`이면 `DelProps(sole)`, 아니면 `DelProps(sole, Count)`를 부른다.
    - 다시 센 수 = 목표 + kept 이면 성공이다.
    - 이력은 `ChangeLog.Add(NameOf(unit), "아이템 <이름>(<번호>)", 전, 후, 성공)`이다.
    - 문장: `<이름>: n개 -> m개`. kept>0이면 끝에 ` 장착 중인 k개는 그대로 두었습니다.`를 붙인다.
  - `Drop`은 `SetCount(unit, id, 0)`이다.
  - `DropQuestion` 문구: `"<이름> n개를 모두 버립니다(강화·옵션이 붙은 것 s개 포함). 한 번 더 누르면 버립니다."`. s가 0이면 괄호를 뺀다.
- [ ] **Step 4: `DrawItemRow`를 고친다.**
  - `보유` 라벨 대신 `NumberField.Build(row, "ResOwned" + id, ColOwned, …, 70 폭, …, v => Say(BagService.SetCount(Player, id, v)), true)`를 둔다. 마지막 `true`는 칸을 떠날 때도 저장한다는 뜻으로, 즐겨찾기 수량 칸과 같다.
  - 그 오른쪽 `ColOwned + 76`에 `Confirm.Button("ResDrop" + id, row, "버리기", "정말 버리기", …, 90 폭, "res-drop-" + id, BagService.DropQuestion(Player, id), () => Say(BagService.Drop(Player, id)), 보유 > 0)`를 둔다.
  - 게임에 없는 줄(`entry.Missing`)은 지금처럼 `-` 라벨이고 단추를 두지 않는다.
  - 저장한 뒤 그 줄의 수를 다시 그리려면 기존 `Changed()`를 부른다.
- [ ] **Step 5: 배포 빌드 경고 0을 확인한다.** Run: `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release`. Expected: `경고 0개 오류 0개`.
- [ ] **Step 6: 회차에서 통과를 확인한다.** Run: `-Begin tb1-1020e -Fixture base` → `ui-check.ps1 -Part bagcount`, `-Part items`, `-Part favorites`(기존 자원 탭이 깨지지 않는지) → `-End`. Expected: 셋 다 `FAILS=0`, `SAVE RESTORED AND IDENTICAL`.
- [ ] **Step 7: 커밋한다.** `git commit -m "feat: 자원 탭 보유 수량 지정·버리기(장착품 보호)와 검사 bagcount"`

### Task 3: 성격(능력치 탭 여섯째 분류)

**Files:**
- Create: `src/ModCode/ModMain/Services/TraitRules.cs`(순수 검사)
- Modify: `src/ModCode/ModMain/Services/EditService.cs`(`SetTrait`)
- Modify: `src/ModCode/ModMain/Services/AttributeCatalog.cs`(`Categories`에 `"성격"`)
- Modify: `src/ModCode/ModMain/UI/Tabs/AttributesTab.cs`(분류 5일 때 표 대신 성격 화면)
- Modify: `tests/ModMain.Tests/ModMain.Tests.csproj`, `Program.cs`(`TraitRulesTests`)
- Modify: `tools/research/ui-check.ps1`(부분 `traits`)

**Interfaces:**
- Produces:
  - `TraitRules.Slot`(enum `In, Out1, Out2`)
  - `public static string TraitRules.Check(Slot slot, int id, int type, int other)`
    - 인자: `type`은 그 번호의 표 `type`이다. 0이면 표에 없다는 뜻이다. `other`는 다른 외재 칸의 값이다.
    - 맞으면 null을 돌려준다. 아니면 이유를 돌려준다: `"표에 없는 성격입니다"`, `"내재 칸에는 내재 성격만 넣을 수 있습니다"`, `"외재 칸에는 외재 성격만 넣을 수 있습니다"`, `"다른 외재 칸과 같은 성격입니다"`.
  - `public static string EditService.SetTrait(WorldUnitBase unit, TraitRules.Slot slot, int id)`
  - `public static string EditService.TraitName(int id)`: 표의 `sc5asd_sd34`를 `GameTool.LS`로 바꾼 값이다. 표에 없으면 `"목록에 없는 번호 " + id`다.

- [ ] **Step 1: 순수 시험을 쓴다.**
  - `traits in ok`: (In, 2, 1, 0) → null.
  - `traits in rejects outer`: (In, 9, 2, 0) → 내재 문구.
  - `traits out ok`: (Out1, 10, 2, 15) → null.
  - `traits out same`: (Out1, 15, 2, 15) → 같은 성격 문구.
  - `traits out rejects inner`: (Out2, 3, 1, 8) → 외재 문구.
  - `traits unknown`: (In, 99, 0, 0) → 표에 없음.
- [ ] **Step 2: 실패를 확인하고 `TraitRules`를 구현한 뒤 통과를 확인한다.** Run: `dotnet run --project tests/ModMain.Tests -c Release`. Expected: 처음에는 빌드 실패, 구현 후 `FAILS=0`.
- [ ] **Step 3: 검사 부분 `traits`를 쓴다.** 확인 항목:
  - `AttrCat5`가 있다.
  - 누르면 칩 `Trait_in_1`~`Trait_in_7`, `Trait_out1_8`~`Trait_out1_19`, `Trait_out2_8`~`Trait_out2_19`가 그려진다.
  - 고정본 플레이어(in 4, o1 8, o2 9)에서 `Trait_out1_9`가 꺼져 있다. 누르면 반응이 없다(Rc로 누른 결과가 그 단추가 아님).
  - `Trait_in_2`를 누르면 `tbtraits 0`이 `in=2`이고 이력에 `성격` 줄이 생긴다.
  - `Trait_out2_15`를 누르면 `o2=15`다.
  - NPC 탭에서 첫 NPC를 대상으로 고른 뒤(기존 `people` 검사의 방식) `Trait_in_7`을 누르면 그 NPC의 `in=7`이다.
  - 예외 0.
- [ ] **Step 4: 회차에서 실패를 확인한다.** Run: `-Begin tb1-1020f -Fixture base` → `-Part traits` → `-End`. Expected: `AttrCat5` 없음으로 FAIL.
- [ ] **Step 5: `EditService.SetTrait`·`TraitName`을 구현한다.**
  - 순서: `TraitRules.Check` → 백업 → `propertyData.inTrait/outTrait1/outTrait2`에 쓰기 → 다시 읽기 → `ChangeLog.Add(NameOf(unit), "성격(" + 칸 이름 + ")", 전 이름, 후 이름, ok)`.
  - 칸 이름은 `내재 성격`·`외재 성격 1`·`외재 성격 2`다.
  - 문장: `성격(<인물>): 외재 성격 1 <전> -> <후>`.
  - 표는 `g.conf.roleCreateCharacter.GetItem(id)`로 읽는다. `type`도 여기서 꺼낸다.
- [ ] **Step 6: `AttributesTab`을 고친다.**
  - `attributeCategory == 5`면 세 줄을 그린다(y = 52 + 줄×(칩 높이 합 + 20)).
  - 줄마다 머리 라벨 `내재 성격 — <지금 이름>`을 단다.
  - 칩은 `Ui.Button("Trait_" + 칸 + "_" + id, …, 켜짐 = 지금 값, 쓸 수 있음 = Check(...) == null || 지금 값)` 꼴로, 140×38에 간격 6을 두고 한 줄에 7개씩 놓는다.
  - 아래 안내 `"성격은 대화·사건 선택지와 NPC의 행동에 영향을 줍니다."`를 둔다(스펙 5.1).
  - 증감 단위·자원 회복 줄은 다른 분류에서만 그린다.
  - 칩 목록은 `_allConfList`를 번호로 돈다.
- [ ] **Step 7: 빌드(경고 0)하고 회차에서 통과를 확인한다.** Run: `-Begin tb1-1020g -Fixture base` → `-Part traits`, `-Part tabs` → `-End`. Expected: `FAILS=0` 둘, `SAVE RESTORED AND IDENTICAL`.
- [ ] **Step 8: 커밋한다.** `git commit -m "feat: 능력치 탭 성격 분류(내재·외재 둘)와 검사 traits"`

### Task 4: 아이템 소모 없음(배율 줄과 막기 패치)

**Files:**
- Create: `src/ModCode/ModMain/Services/RateGuards.cs`
- Modify: `src/ModCode/ModMain/Services/RateService.cs`(줄 `ItemUse`, `All`, `EnsurePatched`)
- Modify: `src/ModCode/ModMain/Dev/DevCommandsRate.cs`(`rstate`에 `itemUse`가 `All`로 자동 포함되는지만 확인)
- Modify: `tools/research/ui-check.ps1`(부분 `itemuse`. `rates`의 그려짐 목록에 `RateItemUse`)

**Interfaces:**
- Consumes: `RateService.Rate`, `RateService.InBattle`
- Produces:
  - `RateService.ItemUse`(Key `itemUse`, Label `아이템 소모`, Short `아이템 소모`, Steps `{1f, 0f}`, ZeroText `없음`, Note 스펙 5.2 그대로, Hook `null`)
  - `All` 순서: `…, StudyGain, ItemUse, Taken`이다. `rstate` 출력이 `studyGain=1 itemUse=1 taken=1`이 된다.
  - `internal static class RateGuards`
    - `public static void PatchItemUse(HarmonyLib.Harmony h)`
    - 접두·접미: `UseBegin`/`StudyBegin`/`UpGradeBegin`/`ActionEnd`, `CostSoleGuard(WorldUnitData __instance, string __0)`, `CostPdGuard(WorldUnitData __instance, DataProps.PropsData __0)`
  - `RateService.EnsurePatched`: `rate == ItemUse`이면 `RateGuards.PatchItemUse(harmony)`를 부르고 `Patched = true`로 한다(`Hook == null`이라도 이 경로는 탄다).

- [ ] **Step 1: 검사 부분 `itemuse`를 쓴다.** 확인 항목:
  - `RateItemUse`가 그려지고 `x1`이다.
  - 한 번 누르면 `없음`이고 `rstate`에 `itemUse=0(patched)`가 있다.
  - `tbuse 1011011` → 수 그대로. `cmgive 6440014 1` → `tbuse 6440014` → 그대로. `cmgive 6083010 1` → `tbuse 6083010` → 그대로. 사이에 뜨는 `Drama*`는 닫는다.
  - 비급: `mgridset 10`, `rbook 0 <rownskills 첫 번호> <grade> 1 1`, 학습 진도 x100(기존 `studygain`과 같은 방식)으로 `adstudy` 한 번 → 무예 수 +1, 비급 남음, 영석 900 감소.
  - 돌파: `upreal` 세 번 → 경지가 오르고, 대경지 돌파 때의 재료 수가 그대로다. `upreal` 출력의 require/rarity 번호를 `count`로 전후 비교한다.
  - 연기: `cmlearn 6181110`, `matgive 1 6181110 1`, `tbforge 6181110 5251001` → `count 5251001`이 20에서 0으로 준다.
  - 전투: `fight 1011 20` + `click 960 684` → `tbbuse 2`·`tbbuse 4`(체력이 가득이면 거부되는 칸은 건너뜀) → `count 1011111`·`count 1011161` 그대로 → `bend`, 결과 창 확인.
  - 다시 눌러 `x1` → `tbuse 1011011` → 하나 줄어든다.
  - 예외 0, `ModWarnings` 0.
  - 달의 남은 날이 0이면 비급 공부가 거부되므로, 검사는 비급 공부를 먼저 한다.
- [ ] **Step 2: 회차에서 실패를 확인한다.** Run: `-Begin tb1-1020h -Fixture base` → `-Part itemuse` → `-End`. Expected: `RateItemUse` 없음으로 FAIL.
- [ ] **Step 3: `RateGuards`를 구현한다.**
  - 패치 대상(`AccessTools.Method`로 찾는다):
    - `UnitActionPropUse.OnCreate`: 앞 `UseBegin`, 뒤 `ActionEnd`
    - `UnitActionPropMartialStudy.OnCreate`: 앞 `StudyBegin`, 뒤 `ActionEnd`
    - `UnitActionRoleUpGrade.OnCreate`: 앞 `UpGradeBegin`, 뒤 `ActionEnd`
    - `WorldUnitData.CostPropItem(string,int,bool)`: 앞 `CostSoleGuard`
    - `WorldUnitData.CostPropItem(DataProps.PropsData,bool)`: 앞 `CostPdGuard`
  - 기억하는 집합은 `HashSet<string> using`이다. 각 `*Begin`은 먼저 비우고, 행동의 `unit`이 `g.world.playerUnit`과 같은 포인터일 때만 넣는다.
    - `UseBegin`: `propsData.soleID`
    - `StudyBegin`: `martialData.data.soleID`
    - `UpGradeBegin`: `requirePropsSoleID`와 `rarityPropsSoleID`의 모든 칸
  - 가드: `RateService.ItemUse.On`이고 `__instance.unit`이 플레이어이며, sole이 `using`에 있거나 `InBattle`이면서 `playerUnitCtrl.props[i].data.propsData.soleID` 가운데 하나와 같으면 `false`(건너뜀)를 돌려준다. 그 밖은 `true`다.
  - 가드 안의 예외는 잡고 `true`를 돌려준다. 첫 예외 한 번만 `MelonLogger.Msg("[TlToybox] 아이템 소모 가드 실패: " + e)`로 남긴다.
  - 머리 주석에 05 문서 65절의 표를 요약한다.
- [ ] **Step 4: `RateService`에 줄을 더하고 `EnsurePatched`를 잇는다.** 배율 탭(`RateTab`)은 `All`로 줄을 그리므로 따로 고치지 않는다. 다만 줄이 12개일 때 마지막 줄(무적)이 화면 안에 있는지는 검사 `studygain`의 그 항목으로 본다.
- [ ] **Step 5: 검사 `rates`의 그려짐 목록에 `RateItemUse`를 더한다**(`RateStudyGain` 뒤).
- [ ] **Step 6: 빌드(경고 0)하고 회차에서 통과를 확인한다.** Run: `-Begin tb1-1020i -Fixture base` → `-Part rates`(첫 검사로) → `-Part itemuse` → `-End`. Expected: `FAILS=0` 둘. `studygain`은 다음 회차 `tb1-1020j`에서 따로 돌린다(달을 쓴다).
- [ ] **Step 7: 커밋한다.** `git commit -m "feat: 배율 탭 아이템 소모 없음(쓰는 sole·전투 칸만 막음)과 검사 itemuse"`

### Task 5: 영력·집중력 `없음` 넓히기

**Files:**
- Modify: `src/ModCode/ModMain/Services/RateGuards.cs`(`PatchDrain`, `AddMpGuard`, `AddSpGuard`)
- Modify: `src/ModCode/ModMain/Services/RateService.cs`(`MpCost`·`SpCost`의 Note, `Next`에서 0 단계로 갈 때 `EnsureDrain`)
- Modify: `tools/research/ui-check.ps1`(부분 `mpnone`)

**Interfaces:**
- Consumes: `RateService.MpCost`, `RateService.SpCost`, `RateService.InBattle`
- Produces:
  - `public static void RateGuards.PatchDrain(HarmonyLib.Harmony h)`: `UnitDataBase.AddMP(int,bool)`와 `AddSP(int,bool)`에 접두를 건다.
  - `RateService`: `MpCost`나 `SpCost`가 값 0 단계로 처음 갈 때 한 번 `PatchDrain`을 부른다(정적 `drainPatched`).
  - 새 Note:
    - `MpCost`: `"전투에서 무예·절기·신법·신통과 호천안 기술이 쓰는 영력을 곱합니다. 없음이면 전투 중 영력이 줄지 않습니다(적의 영력 깎기 포함)."`
    - `SpCost`: `"전투에서 연요호가 쓰는 집중력을 곱합니다. 없음이면 전투 중 집중력이 줄지 않습니다(전투 단약 포함)."`

- [ ] **Step 1: 검사 부분 `mpnone`을 쓴다.** 확인 항목:
  - `RateMpCost`를 눌러 `없음`, `RateSpCost`를 눌러 `없음`으로 둔다.
  - 전투(`fight 1011 20`)에서 `tbmp`의 mp·sp를 읽는다.
  - `bcast 1` 뒤 mp가 그대로다. `tbbuse 2` 뒤 sp가 그대로다(그 칸이 거부되면 다른 칸).
  - `RateMpCost`를 두 번 더 눌러 x0.5로 둔다(단계 순서는 x1 → x0.5 → x0.25 → x0.1 → 없음이고, 없음 다음은 x1이다). `bcast 1` 뒤 mp가 줄어든다(곱하기는 기존 기술값 방식).
  - `bend`, 결과 창 확인. 예외 0.
- [ ] **Step 2: 회차에서 실패를 확인한다.** Run: `-Begin tb1-1020k -Fixture base` → `-Part mpnone` → `-End`. Expected: 없음에서 mp 감소로 FAIL.
- [ ] **Step 3: 가드를 구현한다.** `AddMpGuard(UnitDataBase __instance, int __0)`은 `__0 < 0`, `RateService.MpCost.On && MpCost.Value == 0`, `InBattle`, `__instance.Pointer == playerUnitCtrl.data.Pointer` 가 모두 참이면 `false`다. `AddSpGuard`도 같은 꼴로 `SpCost`를 본다. 예외는 Task 4와 같이 다룬다.
- [ ] **Step 4: 빌드(경고 0)하고 회차에서 통과를 확인한다.** Run: `-Begin tb1-1020l -Fixture base` → `-Part rates`(첫 검사) → `-Part mpnone` → `-Part combat` → `-End`. Expected: `FAILS=0` 셋.
- [ ] **Step 5: 커밋한다.** `git commit -m "feat: 영력·집중력 소모 없음이 전투 중 모든 감소를 막음과 검사 mpnone"`

### Task 6: 문서·마무리 검사·병합

**Files:**
- Modify: `docs/05-editor-v0.1.md`(66절 `트레이너 비교 1묶음`: 규칙·실측·검사·한계·회차 이름)
- Modify: `docs/06-handoff.md`
  - 2절 기능 지도: 능력치 성격, 자원 보유 수량, 배율 아이템 소모·영력 없음
  - 5절 검사 표: `bagcount`·`traits`·`itemuse`·`mpnone`
  - 6절 함정: Vector2 패치 충돌, `DelProps(id, n)`이 장착품부터 지움
  - 8.1 표의 1묶음 줄
- Modify: `docs/11-trainer-comparison.md`(2.1·2.5 표의 해당 줄을 `있음(66절)`으로)

- [ ] **Step 1: 마무리 회차에서 바뀐 탭의 기존 검사를 돌린다.** Run: `-Begin tb1-1020m -Fixture base` → `-Part base`, `tabs`, `items`, `numbers`, `history`, `studygain` → `-End`. Expected: 모두 `FAILS=0`. 실패하면 그 부분만 새 회차에서 다시 돌려 가린다(인계 6절).
- [ ] **Step 2: 게임 없이 도는 시험을 돌린다.** Run: `dotnet run --project tests/ModMain.Tests -c Release`. Expected: `FAILS=0`.
- [ ] **Step 3: 문서를 쓴다.** 회차 이름은 실제로 쓴 것만 적는다.
- [ ] **Step 4: 커밋하고 병합한다.** `git commit -m "docs: 66절 트레이너 비교 1묶음, 인계·비교 문서 갱신"` → `git checkout develop` → `git merge --no-ff feat/trainer-batch1` → `git branch -d feat/trainer-batch1`.
- [ ] **Step 5: 설치는 하지 않는다.** 게임 폴더에 DLL을 넣는 것은 사용자 확인 뒤에만 한다(CLAUDE.md).
