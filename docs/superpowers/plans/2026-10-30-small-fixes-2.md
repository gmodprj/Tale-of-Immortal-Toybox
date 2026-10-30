# 작은 수정 2차(묶음 1) 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 13 문서 7절 묶음 1 — 사용자가 플레이에서 바로 느끼는 결함(자원 탭 `보유` 칸, 성공·드롭 상태 유지, 하위 화면 잔존, 안내 글 넘침)과 문구를 고친다.

**Architecture:** 순수 판정 둘(`NumberFieldRules`, `RateGroups.OneHitApplies`)을 떼어 단위 시험으로 덮고, 위젯·탭·`ModMain`은 그 판정을 부르기만 한다. 게임 동작은 기존 검사(`ui-check bagcount`·`favorites`·`books-ui`, `treasure-check forge-ui`, `trainer4-check onehit`)를 고친 규칙에 맞춰 회차에서 돌린다.

**Tech Stack:** C# (.NET Framework 4.7.2, MelonLoader IL2CPP 언홀로어), 단위 시험 `tests/ModMain.Tests`(Check 함수), 게임 검사 PowerShell.

**Spec:** `docs/13-review-2026-10-29.md` 3·4절(A1·A3·B1~B5·B8·B9·B11·B13·B16·B20), 2.5·2.6절. 사용자 결정(2026-10-30): B2는 "보유 칸의 줄이기는 Enter + 확인".

## Global Constraints

- 브랜치 `fix/small-fixes-2`(`develop`에서 분기). 병합은 `develop`에 `--no-ff`. 게임 폴더에 설치하지 않는다.
- 되돌리기 어려운 동작은 두 번 누르기(05 문서 47절). 확인 대기는 `Confirm`(창 전체에 하나).
- 사용자 글은 한국어. 내부 문서 번호를 화면 글에 쓰지 않는다.
- 검증은 `session.ps1 -Begin <이름> -Fixture base -Exclude 2814696167` 회차에서. 회차 이름은 `sf2-a-1030`부터.

## Review Focus

1. 보유 칸에서 더 작은 수를 쓰고 **다른 곳을 누르면**(blur) 아무 일도 없고 칸이 원래 수로 돌아와야 한다 — Task 3의 `DecideOwned(… enter:false …)` 시험과 `bagcount`의 새 줄.
2. 보유 칸에서 같은 수를 쓰고 떠나면 결과 칸에 "이미 n개입니다"가 **나오지 않아야** 한다(다시 그리기 없음) — Task 1 `Decide` 시험, Task 3 `bagcount` 줄.
3. 줄이기 확인 대기 중 다른 단추를 누르면 대기가 풀려야 한다(`Confirm.OnRender`) — Task 3 `bagcount` 줄("ask then press another button → lowering not applied").
4. 일격이 켜진 전투에서 플레이어가 맞아도 일격을 받지 않아야 하고(기존 `onehit` 검사), 동료(`PlayerNPC`)도 — Task 2 단위 시험(게임에서는 동료 전투를 열 수 없어 코드만).
5. `경지 10까지`가 첫 누름에 묻기만 하고 경지가 그대로여야 한다 — Task 5 `forge-ui`의 새 줄.

---

### Task 1: `NumberFieldRules` — 숫자 칸 제출 판정(순수)

**Files:**
- Create: `src/ModCode/ModMain/Services/NumberFieldRules.cs`
- Modify: `tests/ModMain.Tests/ModMain.Tests.csproj`(Compile Include 한 줄), `tests/ModMain.Tests/Program.cs`(`NumberFieldRulesTests()` 추가, `Main`에서 호출)

**Interfaces:**
- Produces:
  ```csharp
  internal static class NumberFieldRules
  {
      public enum Action { Ignore, Revert, Reject, Submit }
      // text: 칸의 글, committed: 마지막으로 넘긴 글(처음은 그린 값), enter: Enter로 끝났는지, commitOnEndEdit: 떠날 때도 넘기는 칸인지
      public static Action Decide(string text, string committed, bool enter, bool commitOnEndEdit, out int value);
      public enum OwnedAction { Ignore, Set, Ask, Confirm }
      // 보유 칸: value가 held보다 크면 Set(Enter·blur 모두), 같으면 Ignore, 작으면 Enter일 때만 — 대기 중이고 같은 값이면 Confirm, 아니면 Ask. blur면 Ignore
      public static OwnedAction DecideOwned(int value, int held, bool enter, bool pending, int pendingValue);
  }
  ```

- [ ] **Step 1: 시험 쓰기** — `Program.cs`에 `NumberFieldRulesTests()`:
  ```csharp
  int v;
  Check("numberfield: blur on an enter-only field is ignored", NumberFieldRules.Decide("5", "3", false, false, out v) == NumberFieldRules.Action.Ignore);
  Check("numberfield: enter with a number submits", NumberFieldRules.Decide("5", "3", true, false, out v) == NumberFieldRules.Action.Submit && v == 5);
  Check("numberfield: enter with text rejects", NumberFieldRules.Decide("abc", "3", true, true, out v) == NumberFieldRules.Action.Reject);
  Check("numberfield: blur with text reverts", NumberFieldRules.Decide("abc", "3", false, true, out v) == NumberFieldRules.Action.Revert);
  Check("numberfield: blur with the same value is ignored", NumberFieldRules.Decide("3", "3", false, true, out v) == NumberFieldRules.Action.Ignore);
  Check("numberfield: blur with the same value in other digits is ignored", NumberFieldRules.Decide("03", "3", false, true, out v) == NumberFieldRules.Action.Ignore);
  Check("numberfield: blur with a new value submits", NumberFieldRules.Decide("4", "3", false, true, out v) == NumberFieldRules.Action.Submit && v == 4);
  Check("numberfield: enter with the same value still submits", NumberFieldRules.Decide("3", "3", true, true, out v) == NumberFieldRules.Action.Submit);
  Check("owned: same value is ignored", NumberFieldRules.DecideOwned(10, 10, true, false, 0) == NumberFieldRules.OwnedAction.Ignore);
  Check("owned: raising sets on blur", NumberFieldRules.DecideOwned(25, 10, false, false, 0) == NumberFieldRules.OwnedAction.Set);
  Check("owned: raising sets on enter", NumberFieldRules.DecideOwned(25, 10, true, false, 0) == NumberFieldRules.OwnedAction.Set);
  Check("owned: lowering on blur is ignored", NumberFieldRules.DecideOwned(7, 10, false, false, 0) == NumberFieldRules.OwnedAction.Ignore);
  Check("owned: lowering on enter asks", NumberFieldRules.DecideOwned(7, 10, true, false, 0) == NumberFieldRules.OwnedAction.Ask);
  Check("owned: lowering on enter with the same pending value confirms", NumberFieldRules.DecideOwned(7, 10, true, true, 7) == NumberFieldRules.OwnedAction.Confirm);
  Check("owned: lowering on enter with another pending value asks again", NumberFieldRules.DecideOwned(5, 10, true, true, 7) == NumberFieldRules.OwnedAction.Ask);
  Check("owned: zero on enter asks", NumberFieldRules.DecideOwned(0, 10, true, false, 0) == NumberFieldRules.OwnedAction.Ask);
  ```
- [ ] **Step 2: 실패 확인** — `dotnet run --project tests/ModMain.Tests -c Release` → 컴파일 오류(형식 없음).
- [ ] **Step 3: 구현** — 위 서명대로. `Decide`의 같은 값 비교는 `int.TryParse(committed)`한 수와 `value`의 비교(글 비교가 아님).
- [ ] **Step 4: 통과 확인** — 같은 명령, `FAILS=0`.
- [ ] **Step 5: 커밋** — `feat: NumberFieldRules — 숫자 칸 제출 판정과 보유 칸 줄이기 규칙(순수, 시험 16줄)`

### Task 2: 일격의 대상 판정(순수) + `BattleService`

**Files:**
- Modify: `src/ModCode/ModMain/Services/RateGroups.cs`(함수 추가), `src/ModCode/ModMain/Services/WorldService.cs:236-238`, `tests/ModMain.Tests/Program.cs`(`RateGroupsTests` 안에 4줄)

**Interfaces:**
- Produces: `RateGroups.OneHitApplies(int attackerType, int targetType)` — `UnitType` 값(int): `Player=1`, `PlayerNPC=2`. 공격자가 1이고 대상이 1·2가 아닐 때만 참. 상수 `RateGroups.UnitPlayer = 1`, `RateGroups.UnitPlayerNpc = 2`.

- [ ] **Step 1: 시험** —
  ```csharp
  Check("onehit: player hitting a monster applies", RateGroups.OneHitApplies(1, 3));
  Check("onehit: player hitting an ally does not", !RateGroups.OneHitApplies(1, 2));
  Check("onehit: player hitting the player does not", !RateGroups.OneHitApplies(1, 1));
  Check("onehit: a monster hitting anything does not", !RateGroups.OneHitApplies(3, 3) && !RateGroups.OneHitApplies(4, 1));
  ```
- [ ] **Step 2: 실패 확인** → 컴파일 오류.
- [ ] **Step 3: 구현** — `RateGroups`에 상수 둘과 함수. `WorldService.cs`의 일격 분기 조건 `attacker.data.unitType == UnitType.Player && data.hitUnit != null && data.hitUnit.data != null`을 `data.hitUnit != null && data.hitUnit.data != null && RateGroups.OneHitApplies((int)attacker.data.unitType, (int)data.hitUnit.data.unitType)`로. 배수 분기(`Multiplier > 1`)는 그대로.
- [ ] **Step 4: 통과 확인** `FAILS=0`; `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release` 오류 0.
- [ ] **Step 5: 커밋** — `fix: 일격은 플레이어·동료(PlayerNPC)를 맞힌 타격에 걸지 않는다(13 문서 2.6절)`

### Task 3: `NumberField`와 자원 탭 `보유` 칸(B1·B2) + `bagcount` 검사

**Files:**
- Modify: `src/ModCode/ModMain/UI/Widgets/NumberField.cs`, `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs:582-586`, `tools/research/ui-check.ps1`(`bagcount` 부분)

**Interfaces:**
- Produces: `NumberField.Build(Transform parent, string name, float x, float y, float w, float h, string text, Action<string> onChanged, Action<int, bool> onSubmit, bool commitOnEndEdit)` — 둘째 인자 `enter`는 Enter로 끝났는지. 기존 `Action<int>` 두 오버로드는 이것을 감싼다. 끝맺음 처리는 `NumberFieldRules.Decide`로: `Ignore`면 아무것도 하지 않음(글도 그대로), `Revert`면 `input.text = committed`, `Reject`면 `Say("숫자를 입력하세요.")`, `Submit`이면 `committed = v; onSubmit(value, enter)`.
- `ResourcesTab`: 필드 `private int pendingOwnedId; private int pendingOwnedValue;` 확인 키 `"res-set-" + id`. 그리기: 칸의 처음 글은 `Confirm.IsPending(key) && pendingOwnedId == id ? pendingOwnedValue.ToString() : held.ToString()`. 제출 콜백은 `NumberFieldRules.DecideOwned(v, held, enter, Confirm.IsPending(key) && pendingOwnedId == id, pendingOwnedValue)`:
  - `Set` → `Say(BagService.SetCount(Player, id, v))`
  - `Ask` → `pendingOwnedId = id; pendingOwnedValue = v; Confirm.Ask(key, BagService.LowerQuestion(Player, id, v))` — `BagService.LowerQuestion(WorldUnitBase unit, int propsId, int n)`를 `BagService.Count.cs`에 추가: `"<이름>: <held>개에서 <n>개로 줄입니다(<held-n>개 버림). 같은 값에서 Enter를 한 번 더 누르세요."` (`DropQuestion` 옆, 이름은 `DropQuestion`이 쓰는 같은 도우미로)
  - `Confirm` → `Confirm.Clear(); Say(BagService.SetCount(Player, id, v))`
  - `Ignore` → 아무것도 하지 않음(blur로 줄이기·같은 값). 칸의 글이 `held`와 다르면 `input.text = held.ToString()`으로 되돌린다(콜백이 `InputField`를 받도록 `Build`의 반환값을 지역 변수에 둔다).
  - `held`는 그릴 때 읽은 값이 아니라 콜백 시점에 `Player.data.unitData.propData.GetPropsNum(id)`로 다시 읽는다.

- [ ] **Step 1: `bagcount` 검사 고치기(RED)** — `SetOwned`(blur)는 늘리기에만 쓴다. 줄이기용 `LowerOwned([int]$id, [int]$n)`: `uifocus ResOwned$id; ui set ResOwned$id $n; Key 0x0D` → `uistate`에 `'개로 줄입니다'`가 없으면 한 번 더(키 유실) → `uifocus ResOwned$id; Key 0x0D`(칸에 대기 값이 미리 들어 있다) → 400 ms. 바꾸는 줄: `SetOwned 1011011 7`·`0`, `SetOwned 1011052 3`·`0`, `SetOwned 3031101 2`를 `LowerOwned`로. 새 줄 넷:
  ```powershell
  SetOwned 1011011 7       # blur로 줄이기
  Check 'lowering by leaving the cell does nothing' ((Held 1011011) -eq 25 -and (Cmd 'uitext ResOwned1011011') -match "text='25'") "held=$(Held 1011011) | $(Cmd 'uitext ResOwned1011011')"
  Cmd 'uistate' | Out-Null; SetOwned 1011011 25   # 같은 값으로 떠나기
  Check 'leaving with the same value says nothing' ((Said) -notmatch '이미 25개') (Said)
  Cmd 'uifocus ResOwned1011011' | Out-Null; Cmd 'ui set ResOwned1011011 7' | Out-Null; Key 0x0D
  Check 'the first enter on a lower value only asks' ((Held 1011011) -eq 25 -and (Said) -match '7개로 줄입니다') (Said)
  Press 'ResClear' | Out-Null; Show '1011011'
  Check 'another button clears the pending lowering' ((Held 1011011) -eq 25 -and (Cmd 'uitext ResOwned1011011') -match "text='25'") (Cmd 'uitext ResOwned1011011')
  ```
  그다음 `LowerOwned 1011011 7` → 기존 `lowering the count removes the difference`.
- [ ] **Step 2: 구현** — 위 Interfaces대로 `NumberField`·`ResourcesTab`·`BagService.LowerQuestion`.
- [ ] **Step 3: 빌드** 오류 0. 단위 시험 `FAILS=0`.
- [ ] **Step 4: 커밋** — `fix: 자원 탭 보유 칸 — 같은 값·blur 줄이기는 무시, 줄이기는 Enter + 확인(13 문서 B1·B2)`. 회차 검증은 Task 6.

### Task 4: `ModMain`·`EditorWindow` — A1·A3·B8

**Files:**
- Modify: `src/ModCode/ModMain/ModMain.cs:78-120`, `src/ModCode/ModMain/UI/EditorWindow.cs`(함수 추가), `src/ModCode/ModMain/UI/Tabs/EditorTab.cs:67-70`

**Interfaces:**
- `EditorWindow.ResetViews()` (public static): `tabs`가 null이 아니면 각 탭의 `Leave()`를 부르고 `message = ""`, `Confirm.Clear()`. `Leave()`는 이미 public(`CloseViews` + `OnHide`).
- `ModMain.ResetState()`에 try 블록 둘 추가: `SuccessService.Reset()`(로그 머리 `"[TlToybox] 성공·드롭 되돌리기 오류: "`), `EditorWindow.ResetViews()`(`"[TlToybox] 하위 화면 닫기 오류: "`).
- `ModMain.Destroy()`·`Init()`의 `BattleService.OnDestroy()`·`BattleService.OnInit()`를 각각 try/catch로 감싸 `MelonLogger.Msg("[TlToybox] battle events failed: " + e.GetType().Name + ": " + e.Message)`.

- [ ] **Step 1: 구현**(단위 시험 대상 아님 — 게임 객체).
- [ ] **Step 2: 빌드** 오류 0.
- [ ] **Step 3: 커밋** — `fix: 세이브를 떠날 때 성공·드롭 상태와 하위 화면을 닫고, 전투 사건 등록을 try로(13 문서 A1·A3·B8)`

### Task 5: 작은 UI 고침(B9·B3·B4·B5·B11·B13·B16·B20) + `forge-ui` 검사

**Files:**
- Modify: `UI/Tabs/EditorTab.cs:58-65`(Pop), `UI/Tabs/MartialTab.cs:205,537`, `UI/Tabs/TreasureTab.cs:174-175`, `UI/Tabs/ResourcesTab.Books.cs:217-222`, `UI/Tabs/ResourcesTab.cs:369`, `UI/Tabs/SuccessTab.cs:36`, `UI/Tabs/AppellationTab.cs:245`, `tools/research/treasure-check.ps1:248`

- [ ] **Step 1: 구현**
  - B9: `Pop()`의 `Redraw()` → `Refresh()`(결과 문구 유지). `Push`는 그대로.
  - B3: `MartialTab.cs:537` `Ui.Label(...)` → `Ui.Wrapped(content, 같은 글, 834, y + 46, 470, 36, 13)`.
  - B4: `TreasureTab.cs:174-175` → `Confirm.Button("ForgeGradeMax", content, "경지 " + Max + "까지", "정말 올리기", 230, y, 150, 38, "forge-grade-max", "경지를 " + TreasureRules.MaxGrade + "까지 올립니다. 내리는 길은 없습니다. 한 번 더 누르면 실행합니다.", new Action(() => Say(TreasureService.GradeUpTo(Target, sole, TreasureRules.MaxGrade))), canUp)`.
  - B5: `ResourcesTab.Books.cs:217` `NumberField.Build(..., new Action<int>(...))` 끝에 `, true` 추가(떠날 때도 저장. 같은 값은 `Decide`가 blur에서 거른다).
  - B11: `ResourcesTab.cs:369` 글 → `"수량 칸에 쓰고 Enter를 누르거나 다른 곳을 누르면 저장됩니다. ☆로 넣고 ★로 뺍니다."`
  - B13: `SuccessTab.cs:36` 글 → `"종문 초빙·호요 포획·도주는 판정 위치만 찾았고 아직 넣지 않았습니다."`
  - B16: `MartialTab.cs:205` `"수련 " + mastery.Stages + "/12 · " + mastery.Block` → `"수련 단계 " + mastery.Stages + "/12 · " + mastery.Block`.
  - B20: `AppellationTab.cs:245` `"버튼"` → `"동작"`.
  - `treasure-check.ps1:248`: `Press 'ForgeGradeMax'` 앞에 `Press 'ForgeGradeMax' | Out-Null; Check 'grade to 10 asks first' ((Art $sole) -match "grade=$($g0 + 1) " -and (Cmd 'uitext ForgeGradeMax') -match '정말 올리기') (Cmd 'uitext ForgeGradeMax')` 추가.
- [ ] **Step 2: 빌드** 오류 0. `check-texts.py`가 있으면 돌리지 않는다(화면 글 번역표와 무관).
- [ ] **Step 3: 커밋** — `fix: 돌아가기가 결과를 지우지 않음, 기술 추가 안내 두 줄, 경지 10까지 두 번 누르기, 비급 수량 칸 떠날 때 저장, 문구 넷(13 문서 B3·B4·B5·B9·B11·B13·B16·B20)`

### Task 6: 회차 검증

**Files:** 없음(검사 실행). 결과를 Task 7의 문서에 적는다.

- [ ] **Step 1: 회차 `sf2-a-1030`**(`-Fixture base -Exclude 2814696167`): `ui-check -Part base`·`tabs`·`confirm`·`favorites`·`books-ui`·`bagcount`(순서대로. `bagcount`는 법보를 지급하므로 뒤에) → 모두 `FAILS=0`. `favorites`에 `FavBookAmount`를 blur로 저장하는 줄이 없으므로 `books-ui` 뒤에 손으로 한 번: `uifocus FavBookAmount<tail>; ui set … 4; uifocus ItemQuery` → `rfav` 또는 `uitext FavBookGive<tail>`가 `지급 x4`. 캡처 `check-skill-add-wrap.png`(기술 추가 화면, B3).
- [ ] **Step 2: 회차 `sf2-b-1030`**: `treasure-check -Part service` → `forge-ui`(새 줄 포함) `FAILS=0`; `mastery-check -Part ui` `FAILS=0`(B16 글은 막힌 공법이 있어야 보임 — 없으면 INFO); `trainer4-check -Part ui`·`onehit` `FAILS=0`; `ui-check -Part success` `FAILS=0`.
- [ ] **Step 3: 회차 `sf2-c-1030`**: `artifact-check -Part hud-battle-slots`(13 문서 2.5절의 `hud drift` 경고 확인). 통과하면 경고는 검사 밖 조합(쌍어패+호천안 칸이 둘 다 켜진 전투)의 것이므로 06 8.0절에 "미확인"으로; 실패하면 13 문서에 결함으로 적고 이 묶음에서는 고치지 않는다.
- [ ] **Step 4: 단위 시험** `dotnet run --project tests/ModMain.Tests -c Release` `FAILS=0`, `session.tests.ps1` 변화 없음(스크립트를 고치지 않았으므로 돌리지 않는다).

### Task 7: 문서와 병합

**Files:**
- Modify: `docs/05-editor-v0.1.md`(89절 추가), `docs/06-handoff.md`(1절 줄, 2절 자원·법보 탭 줄, 8.0절 9번, 5절 `bagcount` 설명), `docs/13-review-2026-10-29.md`(해당 행에 "고침(89절)" 표시)

- [ ] **Step 1: 05 문서 89절** "작은 수정 2차 (2026-10-30)": 고친 것(항목별 한 줄, 13 문서 번호), 보유 칸 규칙(늘리기는 Enter·blur, 줄이기는 Enter + 같은 값 Enter, 다른 단추를 누르면 대기 해제), 검사(회차 이름과 FAIL 수), 확인하지 못한 것(B8은 세이브 전환을 회차에서 만들 수 없어 코드로만, A1 같음, 동료 일격).
- [ ] **Step 2: 06 문서** — 1절에 `2026-10-30` 줄, 2절 자원 탭 줄에 "보유 칸 줄이기는 Enter + 확인", 법보 탭 줄에 "`경지 10까지` 두 번 누르기", 8.0절 9번의 묶음 1을 "끝남(89절)"으로, 5절 `bagcount` 설명에 "줄이기는 Enter 두 번".
- [ ] **Step 3: 13 문서** — A1·A3·B1·B2·B3·B4·B5·B8·B9·B11·B13·B16·B20 행의 "확인" 열 뒤에 `고침(89절)`.
- [ ] **Step 4: 커밋** `docs: 05 89절 작은 수정 2차, 06 1·2·5·8.0절, 13 문서 상태`, `develop` 체크아웃 → `git merge --no-ff fix/small-fixes-2`.
