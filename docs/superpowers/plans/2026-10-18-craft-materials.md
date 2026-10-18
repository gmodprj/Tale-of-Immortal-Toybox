# 기예 제작 재료 지급 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기예 탭의 하위 화면에서 연단·연기·부적 처방을 고르면 그 처방 한 벌(× 묶음 수)의 재료를 가방에 넣는다.

**Architecture:** 한 벌의 계산은 게임 형식을 쓰지 않는 순수 클래스 `CraftPlan`(단위 시험)이 하고, 게임 표를 읽어 그 입력을 만들고 지급하는 것은 `CraftMaterialService`가 한다. 화면은 기예 탭의 하위 화면(`Push`) 하나다.

**Tech Stack:** C# (net472, MelonLoader 0.5.4, Il2Cpp 언홀로어 형식), 단위 시험 `tests/ModMain.Tests`(링크한 순수 파일), 게임 검사 PowerShell(`tools/research/ui-check.ps1`, 회차 `tools/DevRemote/session.ps1`).

**Spec:** `docs/superpowers/specs/2026-10-18-craft-materials-design.md` (실험 결과 05 문서 61절)

## Global Constraints

- 분류 셋: 연단(단방 className 605, 표 `g.conf.makePillFormula`, 배운 기록 `g.data.world.pillFormulas[i].id`), 연기(연기보 618, `g.conf.artifactShapeMaterial`, `g.data.world.allArtistrySkillID`), 부적(도보 658, `g.conf.runeFormula`의 `formulaId`+`grade`, `allArtistrySkillID`).
- 한 벌: 연단 = 주약 `herbalRequier[i]` × `herbalRequierCount[i]` + 오행 부족분(처방 `huo,shui,lei,feng,tu,mu,yin,yan` − 주약이 주는 값, 양수만)마다 그 오행만 가진 약재 중 가장 낮은 단계(`ItemCatalog.Entry.Level`, 같으면 값이 큰 것, 그래도 같으면 번호가 작은 것) × ⌈부족분 / 값⌉ + 쓸 단로가 없을 때만 단로 1. 연기 = `coreMaterialID` × 1 + 보조 재료 정확히 `materialNum`개(종류 `materialType`의 아이템 중 `artifactShapeMixed.mixed`가 가장 작은 것, 같으면 번호가 작은 것) + 불씨(className 522 중 단계가 가장 낮은 것) × 1. 부적 = `requier1`의 첫 번호 × `requier1Count`. 희귀 재료는 주지 않는다.
- 약재 오행: `g.conf.itemHerbal.GetItem(id).effectValue`(`|`) → `g.conf.roleEffect.GetItem(n)`에서 `effectType == 102`인 `value`의 끝 여덟 수(`element_elementNames_a_..._h`)를 더한다. 순서 화·수·뇌·풍·토·목·음·양.
- 단로: `g.conf.makePillFurnace` 아이템. 쓸 수 있음 = `basRequire` ≤ 인물 연단 자질(`unit.data.unitData.propertyData.refineElixir`). 가방에 쓸 수 있는 단로가 없으면 쓸 수 있는 것 중 `basRequire`가 가장 낮은 것(같으면 번호가 작은 것) 1개(묶음 수와 무관).
- 연기의 인물 조건: `basEqpRequire`를 인물 연기 자질(`propertyData.refineWeapon`)과 비교해 모자라면 줄·문장에 `연기 자질 n 이상이 필요합니다(지금 m). 능력치 탭에서 바꿀 수 있습니다.`를 붙이고 재료는 준다.
- 가방: 같은 재료는 한 칸에 겹친다. 새 칸 = 한 벌의 재료 가운데 가방에 아직 없는 번호의 수(묶음 수와 무관). 남은 칸 = `propData.GetGridMaxNum() - propData.GetGridCurNum()`. 새 칸 > 남은 칸이면 아무것도 주지 않고 `가방 칸 n개가 더 있어야 합니다(자원 탭의 가방 칸 단추).`
- 지급은 `EditService.TryGiveItem(unit, id, count, out text)`, 플레이어에게만. 첫 지급 전 백업은 그 함수가 한다. 끝에 이력 한 줄 `ChangeLog.Add(name, "기예 재료", <처방 이름>, "<n종 m개 × k>", ok)`.
- 결과 문장: `<분류> <처방>: 재료 n종 m개를 넣었습니다(묶음 k).` 배우지 않았으면 끝에 ` 이 처방은 아직 배우지 않았습니다(기예 탭에서 배울 수 있습니다).` 못 넣은 것이 있으면 ` 넣지 못함: <이름 수, …>.`
- 요소 이름(검사가 쓴다): `ArtMaterialsOpen`, `MatKind0`·`MatKind1`·`MatKind2`, `MatLearnedOnly`, `MatQuery`, `MatCount1`·`MatCount5`·`MatCount10`, 목록 `MatList`, 줄 `MatGive<키>`·`MatSummary<키>`(키: 연단·연기 = 처방 번호, 부적 = `<도보>_<단계>`), 아래 칸 `MatDetail`.
- 개발 명령은 `Dev\`(배포 제외). 실험 명령 `cm*`은 그대로 둔다.

## Review Focus

- 처방 표의 주약 칸이 비었거나(`herbalRequier` 빈 문자열) 자릿수가 어긋난 행: 그 주약은 빼고 계산하며 예외 없이 줄을 그린다 → Task 1 `pill: empty main list`.
- 부족분을 채울 약재가 없는 오행(예: 음·양만 50 단계뿐인 경우가 아니라 아예 없음): 그 오행은 줄에 `(채울 약재 없음)`으로 남기고 나머지는 준다 → Task 1 `pill: a missing element herb is reported`.
- 쓸 단로가 하나도 없는 낮은 연단 자질(0): 단로는 주지 않고 `연단 자질이 모자라 쓸 단로가 없습니다` → Task 1 `furnace: none usable`.
- 연기 재료 종류의 아이템이 혼합 표에 없음: 그 처방은 보조 재료 없이 `(보조 재료를 찾지 못함)` → Task 1 `forge: no candidate`.
- 묶음 10에서 큰 수(예 주약 300 × 10): 정수 넘침 없이 3000 → Task 1 `pill: k multiplies counts`.

---

### Task 1: 한 벌 계산 `CraftPlan` (순수, 단위 시험)

**Files:**
- Create: `src/ModCode/ModMain/Services/CraftPlan.cs`
- Modify: `tests/ModMain.Tests/ModMain.Tests.csproj` (링크 한 줄), `tests/ModMain.Tests/Program.cs` (시험)

**Interfaces:**
- Produces (namespace `MOD_sWP6UL`, `internal static class CraftPlan`):
  - `sealed class Line { public int Id; public int Count; public string Note; }` (Id 0 + Note만 있는 줄 = 안내)
  - `sealed class Herb { public int Id; public int Level; public int[] Elements; }` (길이 8)
  - `sealed class Candidate { public int Id; public int Class; public int Level; public int Mixed; }`
  - `static List<Line> PillSet(int[] need, IList<int> mainIds, IList<int> mainCounts, Func<int, int[]> elementsOf, IList<Herb> herbs)` — 주약 줄들 + 부족분 줄들(전역 제약의 규칙). 채울 약재가 없는 오행은 `Note = "<오행 이름> n (채울 약재 없음)"`인 Id 0 줄.
  - `static int PickFurnace(IList<Candidate> furnaces, int skill)` — `Mixed` 칸을 `basRequire`로 쓴다. 없으면 0.
  - `static List<Line> ForgeSet(int core, int num, IList<int> types, IList<Candidate> materials, IList<Candidate> fires)`
  - `static List<Line> RuneSet(string requier1, int count)`
  - `static List<Line> Times(List<Line> set, int k)` — Count에 k를 곱한 새 목록(Id 0 줄은 그대로)
  - `static int NewSlots(IEnumerable<Line> set, Func<int, bool> inBag)`
  - `static readonly string[] ElementNames = { "화", "수", "뇌", "풍", "토", "목", "음", "양" }`

- [ ] **Step 1: 실패하는 시험을 쓴다** (`Program.cs`, 기존 `Check` 꼴)
  - `pill: main herbs and the lei deficit` — need `{0,0,150,0,0,250,0,0}`, main `[5031061]`×`[50]`, elementsOf(5031061)=`{0,0,0,0,0,5,0,0}`, herbs: 5031102(lv2,뇌5), 5031112(lv2,뇌10), 5031324(lv4,뇌20) → 줄 `5031061x50`, `5031112x15`.
  - `pill: two mains and three deficits` — 6051021 값(need 뇌300·풍100·토200·목600, main 5031233×40(목10)·5031122×20(뇌5·풍5)), herbs 목 5031061(lv2,5)·뇌 5031112(lv2,10)·토 5031041(lv2,5) → `5031233x40, 5031122x20, 5031112x20, 5031041x40, 5031061x40`(뇌 200/10, 토 200/5, 목 200/5).
  - `pill: empty main list` — mainIds 빈 목록 → 부족분 줄만, 예외 없음.
  - `pill: a missing element herb is reported` — need 음 50, herbs에 음 없음 → Id 0 줄 Note에 `음 50 (채울 약재 없음)`.
  - `pill: k multiplies counts` — `Times`(5031061x300, k=10) → 3000.
  - `furnace: lowest usable` / `furnace: none usable` — (6061011 bas10, 6061021 bas20), skill 102 → 6061011; skill 0 → 0.
  - `forge: exact count of the purest` — num 20, types [525,523], candidates (5231101 cls523 mixed15), (5231105 cls523 mixed5), (5251104 cls525 mixed6), (9999 cls600 mixed1) → 줄 `core x1`, `5231105x20`, fire 최저 단계.
  - `forge: no candidate` → Id 0 줄 `(보조 재료를 찾지 못함)`.
  - `rune: first of the pair` — `RuneSet("5021164|5021674", 2)` → `5021164x2`.
  - `slots: only new ids count` — set {A,B,C}, inBag A → 2.

- [ ] **Step 2: 실패를 확인한다** — Run: `dotnet run --project tests/ModMain.Tests -c Release` / Expected: 빌드 실패(`CraftPlan` 없음)
- [ ] **Step 3: `CraftPlan.cs`를 쓴다** (게임 형식·`g` 참조 없음. 링크를 csproj에 더한다)
- [ ] **Step 4: 통과를 확인한다** — 같은 명령, Expected: `FAILS=0`
- [ ] **Step 5: 커밋** — `feat: 기예 재료 한 벌 계산(CraftPlan)과 단위 시험`

### Task 2: 게임 연결 `CraftMaterialService`와 개발 명령

**Files:**
- Create: `src/ModCode/ModMain/Services/CraftMaterialService.cs`, `src/ModCode/ModMain/Dev/DevCommandsMaterials.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs` (모듈 등록, `DevCommandsCraft` 앞)

**Interfaces:**
- Consumes: Task 1 전부.
- Produces:
  - `sealed class Recipe { public int Kind; public string Key; public int BookId; public int Grade; public string Name; public bool Learned; public List<CraftPlan.Line> Set; public string Warning; }` (Kind 0 연단·1 연기·2 부적, Key는 요소 이름 꼬리, Warning은 인물 조건·단로 없음 안내나 null)
  - `static readonly string[] KindNames = { "연단", "연기", "부적" }`
  - `static List<Recipe> Recipes(int kind, bool learnedOnly, string query)` — 처방 이름은 `ItemCatalog.Get(BookId).Name`, 부적은 이름 뒤에 ` <단계>단계`. 검색은 이름·번호 부분 일치(`EditService.Compact` 기준).
  - `static string Summary(Recipe r)` — `<이름> x<수>, …`(Id 0 줄은 Note)
  - `static string Give(Recipe r, int k)` — 전역 제약의 가방·지급·이력·문장.
  - 개발 명령 `matlist <분류> [all]` → 줄마다 `key|learned|summary`, `matgive <분류> <키> <k>` → `Give` 결과.

- [ ] **Step 1: 서비스를 쓴다** — 단로 줄은 `Recipes`가 아니라 `Give` 때 가방을 보고 정한다(`Summary`에는 `단로 1(없을 때)`로 보인다). 연기의 `Warning`은 전역 제약 문구.
- [ ] **Step 2: 두 구성 빌드** — Run: `dotnet build src/ModCode/ModMain/ModMain.csproj -c Debug` 과 `-c Release -p:DevTools=false` / Expected: 경고 0·오류 0
- [ ] **Step 3: 회차에서 손으로 확인** — `session.ps1 -Begin mat-1018c -Fixture base` 뒤 `matlist 0`(6051011 줄이 `주약 x50, 뇌정화 x15` 꼴), `matgive 0 6051011 1`, `cmpillui 6051011` 캡처에 ✓, `matlist 1 all`, `matgive 1 6181110 1` 뒤 `cmforge 6181110 <보조 재료 번호>`가 `isCreate=0`, `matgive 2 6092020_1 1` 뒤 `cmrune 6092020 1`이 `filled=1`. `-End`.
- [ ] **Step 4: 커밋** — `feat: 기예 재료 지급 서비스와 개발 명령 matlist·matgive`

### Task 3: 기예 탭의 하위 화면

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/ArtTab.cs`

**Interfaces:**
- Consumes: Task 2의 `Recipes`·`Summary`·`Give`·`KindNames`.
- Produces: 전역 제약의 요소 이름들.

- [ ] **Step 1: 하위 화면을 쓴다** — 분류 단추 줄 오른쪽 끝(`ContentW - 180, 0, 180, 40`)에 `제작 재료…`(`ArtMaterialsOpen`) → `Push(() => "제작 재료", DrawMaterials)`. 위 줄: 분류 칩 셋(120폭), `배운 것만`(켜진 색 토글), 검색 칸, `묶음 1/5/10`. `ListView<Recipe>`(줄: 이름(배우지 않았으면 ` (배우지 않음)`)·경지/단계·요약(`MatSummary<키>`, 줄바꿈)·`재료 지급`(`MatGive<키>`, 170×36)), 아래 칸 `MatDetail`(줄을 누르면 재료 전체와 `Warning`, 연단은 오행 요구와 주약이 채우는 값). 연기 줄에 `Warning`이 있으면 요약 끝에 붙인다.
- [ ] **Step 2: 빌드** — Task 2 Step 2와 같다
- [ ] **Step 3: 커밋** — `feat: 기예 탭 제작 재료 하위 화면`

### Task 4: 게임 검사와 문서

**Files:**
- Modify: `tools/research/ui-check.ps1` (`'materials'` 부분, `default` 앞), `docs/05-editor-v0.1.md`(62절), `docs/06-handoff.md`(1·2·8.5절)

- [ ] **Step 1: 검사 `materials`를 쓴다** — 순서: 편집기 → `Tab9` → `ArtMaterialsOpen`. (1) 칩 셋과 `배운 것만` 기본 켜짐, (2) 연단: 검색 `6051011` → `MatSummary6051011`에 주약 50, `MatGive6051011` 누름 → 주약 보유 +50·단로 1(고정본에 단로 없음)·이력 `기예 재료`, `cmpillui 6051011` 캡처(`Shot 'materials-pill'`), (3) 묶음 5로 한 번 더 → 주약 +250, 단로는 더 주지 않음, (4) 연기: `MatKind1`, `배운 것만` 끄기, `MatGive6181110` → `cmforge 6181110 <보조 번호>`가 `isCreate=0`(보조 번호는 `matlist 1 all`의 줄에서 읽는다), 배우지 않음 안내 문장, (5) 부적: `MatKind2` → `MatGive6092020_1` → `cmrune 6092020 1`이 `filled=1`, (6) 가방 모자람: `gridfill`로 칸을 채운 뒤 새 재료가 필요한 처방의 지급이 거부되고 보유가 그대로, 문장 `가방 칸`, (7) `Traps` 0. `gridfill`은 가방을 채우므로 마지막에.
- [ ] **Step 2: 회차에서 돌린다** — `session.ps1 -Begin mat-1018d -Fixture base` → `ui-check.ps1 -Part materials` / Expected: `FAILS=0` → 같은 회차에서 `base`·`tabs` FAIL 0 → `-End`가 `SAVE RESTORED AND IDENTICAL`.
- [ ] **Step 3: 문서** — 05 문서 62절(규칙·화면·실측·한계: 미니게임 성공은 보장하지 않음, 모드 처방 미검사), 인계 1·2절(기예 탭 줄에 `제작 재료`), 8.5절(사용자 확인: 게임 화면에서 실제로 만들어지는지).
- [ ] **Step 4: 커밋** — `test·docs: 기예 재료 검사 materials와 62절`
