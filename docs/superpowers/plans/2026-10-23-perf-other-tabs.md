# 묶음 3 구현 계획 (성능 2: 다른 탭과 검색)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 프리셋·기예·도호 탭을 여는 시간과 자원 탭 검색의 글자마다 비용을 줄이고, `Init`의 모드 행 덮기를 줄일 길을 실험한다. 보이는 것과 결과는 그대로.

**Architecture:**
- 파일 표지(경로·크기·수정 시각)를 만드는 순수 함수 `Services/FileStamp.cs`를 단위 시험하고, 프리셋 목록과 기본 프리셋 비교가 표지가 같을 때 지난 결과를 쓴다.
- 기예·도호·자원 탭은 그리기 한 번 동안만 쓰는 지역 집합·사전으로 중복 계산을 없앤다(게임 상태는 그리기를 넘어 두지 않는다).
- `Init` 실험은 개발 명령으로 먼저 재고, 조건을 넘을 때만 넣는다.

**Tech Stack:** C# 7.3, MelonLoader 0.5.4(Il2Cpp), PowerShell 7 검사, `tests/ModMain.Tests`.

**Spec:** `docs/superpowers/specs/2026-10-23-perf-other-tabs-design.md`(승인 2026-10-23).

## Global Constraints

- 브랜치 `feat/perf-other-tabs`(`develop`에서). 끝나면 사용자 확인 뒤 `--no-ff`. 커밋 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 두 구성 경고 0(`-c Release`, `-c Release -p:DevTools=false -o <임시>`). 배포 코드는 `Dev\`를 참조하지 않는다.
- 회차 `session.ps1 -Begin <이름> -Fixture base`, 이름 `po-1023a`부터 다시 쓰지 않음, `-End`는 `SAVE RESTORED AND IDENTICAL`. 재시작 뒤 화면을 누르는 검사는 하지 않는다.
- 캐시는 비우는 때를 반드시 정한다. 게임 상태(배운 서적, 도호 보유자, `BlockReason`, `Snapshot`)는 그리기를 넘어 두지 않는다.
- 목표: 프리셋 탭 다시 들어가기 0.1초 안쪽(폴더가 같을 때), 기예 탭 0.1초 안쪽(아이템 카탈로그가 만들어진 뒤), 도호 탭 절반 이하, 자원 탭 검색은 전보다 줄임, `Init`은 0.5초 넘게 줄 때만 넣음.
- 목록·번역 결과는 그대로(`rperf dump`, `rloc`, `rzhstat`. 아이템 설명의 `@w_XXXXXX|`는 지우고 비교).

## Review Focus

- **프리셋 파일을 탭 밖에서 고쳤는데 크기가 같은 경우**: 수정 시각이 표지에 있으므로 다시 읽어야 한다. Task 2 Step 5에서 같은 크기로 고쳐 확인.
- **프리셋 파일을 지운 경우**: 줄이 줄어야 한다. Task 2 Step 5.
- **기예 서적을 배운 직후 같은 화면**: 분류 단추의 배운 수가 하나 늘어야 한다. Task 3 Step 3.
- **도호를 다른 인물에게 준 직후**: 그 줄의 다른 보유자가 바뀌어야 한다. Task 4 Step 3.
- **자원 탭에서 검색어를 지웠을 때**: 그룹 수·칩 수·줄 수가 처음 열 때와 같아야 한다. Task 5 Step 3.

---

### Task 1: 측정 명령과 기준선

**Files:**
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPerfProbes.cs`(`rperf query`), `src/ModCode/ModMain/Dev/DevCommandsPerf.cs`(분기)

**Interfaces:**
- Produces: `rperf query <검색어|->`: 자원 탭을 열고 지금 그룹에서 `ResourcesTab.OnQuery`와 같은 일을 하게 한 뒤 걸린 ms. 탭의 private 메서드는 반사로 부른다(`OnQuery(string)`). `-`는 빈 검색어. 돌려주는 줄 `query='<글>' ms=<n> rows=<줄 수>`.

- [ ] **Step 1:** `develop`에서 `feat/perf-other-tabs`. 명령을 더하고 두 구성 빌드. 커밋 `dev: rperf query`.
- [ ] **Step 2:** 회차 `po-1023a`: `rperf tabs`를 두 번(첫·다시), 자원 탭에서 `ResGroupDrug`(첫 그룹 이름은 회차에서 `uidescs ResGroup`으로 확인)를 연 뒤 `rperf query -`, `rperf query 단`, `rperf query 단약`, `rperf dump base`, 그리고 화면 글 기준값: `editor` → `Tab9` → `uidescs ArtKind`(기예 단추 여섯의 글), `Tab15` → `uidescs AplClass`(도호 칩 글). `.devremote\po-baseline.txt`로. `-End`.

### Task 2: 프리셋 탭 — 폴더 표지

**Files:**
- Create: `src/ModCode/ModMain/Services/FileStamp.cs`
- Modify: `src/ModCode/ModMain/Services/PresetService.cs`(`List` 앞, `DefaultsChanged`, `RestoreDefaults`), `src/ModCode/ModMain/UI/Tabs/PresetTab.cs:28-60`, `tests/ModMain.Tests/ModMain.Tests.csproj`, `tests/ModMain.Tests/Program.cs`

**Interfaces:**
- Produces:
  - `internal static class FileStamp { static string Of(string dir, string pattern) }` — `dir` 안 `pattern` 파일을 이름순으로 `이름|크기|LastWriteTimeUtc.Ticks`, `;`로 잇는다. 폴더가 없으면 `"-"`.
  - `PresetService.ListCached()` — 표지가 지난번과 같으면 지난 `List()` 결과, 아니면 `List()`를 부르고 표지를 새로 적는다. `PresetService.DefaultsChangedCached()` — 같은 규칙으로 `DefaultsChanged()`. 둘 다 `PresetService.Forget()`이 비운다.

- [ ] **Step 1: 실패하는 시험** `FileStampTests()`: 임시 폴더에 `a.json`·`b.json`을 쓰고 `Of(dir, "*.json")` 두 번이 같다; `a.json`에 한 줄 덧붙이면 다르다; 같은 길이로 내용만 바꾸고 `File.SetLastWriteTimeUtc`를 1초 뒤로 하면 다르다; `b.json`을 지우면 다르다; `c.txt`를 더해도 같다; 없는 폴더는 `"-"`.
- [ ] **Step 2:** `dotnet run --project tests/ModMain.Tests -c Release` → 컴파일 실패.
- [ ] **Step 3:** `FileStamp.Of` 구현, 시험 FAILS=0. `PresetService.ListCached`·`DefaultsChangedCached`·`Forget`을 더한다. `List()`는 안에서 `WriteNewDefaults`가 파일을 쓸 수 있으므로 표지는 `List()`가 끝난 **뒤**에 잰다. `RestoreDefaults()` 끝에서 `Forget()`.
- [ ] **Step 4:** `PresetTab`: `OnShow`·`OnOpen`은 `presets = null` 대신 `presets = PresetService.ListCached()`가 같은 목록이면 `list.Reset()`을 하지 않게(목록 객체가 같으면 그대로) 한다. `Draw`의 `DefaultsChanged()`를 `DefaultsChangedCached()`로. 되돌리기 뒤 `presets = null`은 그대로. 두 구성 빌드, 커밋 `perf: 프리셋 탭이 폴더가 같으면 다시 읽지 않음`.
- [ ] **Step 5:** 회차 `po-1023b`: `rperf tabs` 두 번(프리셋 탭 둘째 값 0.1초 안쪽). 프리셋 폴더(`translations` 옆 `presets\`)에 `01-풍검.json`을 `99-test.json`으로 복사 → 프리셋 탭 다시 들어가기 → `PresetRow*` 줄 수 +1. 같은 파일 크기 그대로 한 글자만 바꾸고(예: 공백 한 칸을 다른 공백으로) 수정 시각을 바꾼 뒤 다시 들어가기 → 다시 읽힘(`rperf tabs`의 프리셋 값이 다시 0.3초대로 오름). 지우면 줄 -1. `preset-check.ps1 -Part ui` FAIL 0. `-End`.

### Task 3: 기예 탭 — 배운 수 한 번에

**Files:**
- Modify: `src/ModCode/ModMain/Services/ArtService.cs`(`LearnedCount`, 새 `LearnedIds`), `src/ModCode/ModMain/UI/Tabs/ArtTab.cs:31-37`

**Interfaces:**
- Produces: `ArtService.LearnedIds(Kind kind) -> HashSet<int>`(세계 자료에서 그 분류의 배운 번호를 한 번 훑어 모음. 지금 `Learned`가 보는 목록과 같은 출처), `LearnedCount(Kind)`는 `Books(kind,"")`를 이 집합으로 센다. `ArtService.BookCount(Kind) -> int`(분류의 전체 서적 수. `ItemCatalog.Find("", -1, kind.ItemClass).Count`를 분류마다 한 번 세어 정적 사전에 둔다. 아이템 카탈로그는 바뀌지 않으므로 비우지 않는다).

- [ ] **Step 1:** 위 셋을 넣고 `ArtTab` 단추 글이 `BookCount`를 쓴다. `Learned(kind, id)`는 다른 곳이 쓰므로 그대로 둔다. 두 구성 빌드, 커밋 `perf: 기예 탭 배운 수와 전체 수를 한 번에`.
- [ ] **Step 2:** 회차 `po-1023c`: `rperf tabs`(자원 탭 뒤 기예 탭 0.1초 안쪽). `uidescs ArtKind`의 글 여섯이 기준선(`po-baseline.txt`)과 같다.
- [ ] **Step 3:** 같은 회차에서 배우지 않은 서적 하나를 기존 개발 명령(`artstudy <분류> <번호>`)으로 배운 뒤 `editor` → `Tab9` → `uitext ArtKind<분류>`의 배운 수가 하나 늘었는지. `ui-check -Part actions` FAIL 0. 측정에서 줄마다 `BlockReason`이 목표를 넘기는 원인이면 숫자만 기록. `-End`.

### Task 4: 도호 탭 — 칩 수와 보유자

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/AppellationTab.cs`(`Count`, 칩·수 갱신 125·166-227, 줄 그리기 246·276), `src/ModCode/ModMain/Services/AppellationService.cs`(`OtherHolders` 옆 새 함수)

**Interfaces:**
- Produces: `AppellationTab`의 그리기 지역 값 `Dictionary<string,int> chipCounts`(한 번 훑어 칩 키마다 수), `Dictionary<int, List<WorldUnitBase>> holdersCache`(그리기 동안만). `AppellationService.OtherHolders(string myUnitId, int id)` 겹본(플레이어 `unitID`를 한 번만 읽은 값을 받는다). 출처는 지금처럼 `WorldUnitAppellationMgr.allAppellationUnits[id]`.

- [ ] **Step 1:** 칩 수는 그리기·검색 콜백에서 한 번 훑어 사전으로 센다. 줄의 보유자 글·두 번 누르기 문구·아래 칸이 `holdersCache`를 쓴다. 그리기 시작 때 비운다. 두 구성 빌드, 커밋 `perf: 도호 탭 칩 수와 다른 보유자를 그리기 한 번에`.
- [ ] **Step 2:** 회차 `po-1023d`: `rperf tabs` 두 번(도호 탭 절반 이하). `uidescs AplClass`의 칩 글이 기준선과 같다. 그 뒤 `ui-check -Part appellation-ui` FAIL 0은 깨끗한 회차가 필요하므로 회차 `po-1023d2`를 따로 열어 첫 검사로 돌린다.
- [ ] **Step 3:** 같은 회차 끝에: `ttlnpcgive any <비보유 도호 번호>` 뒤 도호 탭 다시 그리기 → `uitext AplHolder<번호>`가 바뀌었다. `-End`.

### Task 5: 자원 탭 검색

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs`(`OnQuery` 490-530, `DrawKinds` 379-380), `src/ModCode/ModMain/Services/ItemCatalog.cs`(새 `Counts`)

**Interfaces:**
- Produces: `ItemCatalog.Counts(Filter filter, HashSet<int> owned, ItemGroups.Group group, out Dictionary<string,int> byGroup, out int[] byKind)`: 거르기를 한 번 돌며 그룹 수와 (그룹이 null이 아니면) 그 그룹의 종류 수를 함께 센다. 결과는 `CountByGroup`·`CountByKind`와 같다. `ResourcesTab`의 그룹별 칩 폭용 수 `Dictionary<string,int[]> widestByGroup`(처음 셀 때 채우고 비우지 않음: 거르기가 비어 보유·가방과 무관, 스펙 4.4).

- [ ] **Step 1:** `OnQuery`와 `Draw`가 `Counts`를 쓰고, 칩 폭은 `widestByGroup`. `CountByGroup`·`CountByKind`는 남긴다(다른 곳이 쓰면). 두 구성 빌드, 커밋 `perf: 자원 탭 검색의 수 세기를 한 번에`.
- [ ] **Step 2:** 회차 `po-1023e`: `rperf query`(Task 1과 같은 셋, 줄 수 같고 ms 줄음). `rperf dump after5`가 기준선과 같다.
- [ ] **Step 3:** `ui set ItemQuery 단약` 뒤 `ui set ItemQuery `(빈 글)로 지우면 `ResGroup*` 단추 글과 `ResKind*` 칩 글, `ItemTableRange`가 처음 열 때 적어 둔 값과 같다. `ui-check -Part items`·`groups`·`numbers` FAIL 0. `-End`.

### Task 6: `Init` 실험(S9)과 P3·P4 측정

**Files:**
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPerfProbes.cs`(`rperf ltscan`), 결과에 따라 `Services/LocalTextIndex.cs`

**Interfaces:**
- Produces: `rperf ltscan`: LocalText를 훑으며 (1) 모드 행 순번의 처음·끝·개수와 연속인지, (2) `isModExtend`만 읽는 훑기, `isModExtend`+`ch`만 읽는 훑기, 지금 `Scan`과 같은 셋을 읽는 훑기의 ms, (3) 바닐라 행의 ch→kr 사전 크기. 사전에 아무것도 쓰지 않는다.

- [ ] **Step 1:** 명령을 더하고 회차 `po-1023f`에서 돌린다. 같은 회차를 `-Restart` 해 (3)의 크기가 같은지.
- [ ] **Step 2:** 판단(스펙 4.5): (나) — 지금 `Scan`이 이미 `kr`을 늘 읽는다(모드 아닌 행도 `KeepsWhole` 판정에 `kr`이 필요). `ch`에 한자가 없으면 `kr`을 읽지 않도록 순서를 바꾸면 얼마나 주는지가 (2)의 차이다. 0.5초 넘게 줄면 그 순서로 바꾼다(결과 같음: `KeepsWhole`은 `ch`에 한자가 없으면 거짓). (가) — 캐시 파일은 (나)를 넣고도 남은 훑기가 1초를 넘을 때만 한다. 표지는 게임 판(`Application.version`), LocalText 행 수, 실린 모드 목록(`g.mod`의 모드 ID·판, 05 문서 41절의 `wsmods`가 읽는 곳)의 해시. 캐시 파일은 데이터 폴더 `translations\.vanilla-index`. 넣으면 05 문서에 이름과 크기.
- [ ] **Step 3:** 넣은 것이 있으면 두 구성 빌드, 커밋, 회차 `po-1023g`에서 `rloc`·`rzhstat`·`rperf dump after6`이 기준선과 같고 `Init` 로그 `scan=`이 줄었다. 없으면 숫자만 기록.
- [ ] **Step 4:** P3·P4: 같은 회차에서 공법 탭 `SkillAddOpen`과 선법 탭 `ImmortalAddOpen`을 열고 닫는 시간을 `rperf tabs` 방식으로 잰다(필요하면 `rperf open <단추 이름>`을 더한다: 단추를 반사로 눌러 다시 그리기까지 ms). 0.05초를 넘으면 그 탭에서 한 번 부른 목록을 그리기 동안 같이 쓰게 고치고, 아니면 숫자만. `-End`.

### Task 7: 마지막 측정·검사·문서·병합

**Files:**
- Modify: `docs/05-editor-v0.1.md`(79절), `docs/12-review-2026-10-22.md`(7절 묶음 3), `docs/06-handoff.md`

- [ ] **Step 1:** 회차 `po-1023h`: Task 1 Step 2와 같은 측정. `rperf dump final`이 `base`와 같다. `ui-check`의 `tabs`·`items`·`groups`·`translation`, `preset-check -Part ui` FAIL 0. `-End`.
- [ ] **Step 2:** 회차 `po-1023i`(`-Exclude 2814696167`): `modbooks`·`books-ui` FAIL 0. 단위 시험 FAIL 0.
- [ ] **Step 3:** 05 문서 79절(전후 표, 캐시와 비우는 때, `Init` 실험 결과, P3·P4 숫자, 회차 이름), 12 문서 7절 묶음 3 결과, 06 문서. 커밋 `docs: 묶음 3 결과`.
- [ ] **Step 4:** 사용자 확인 뒤 `develop`에 `--no-ff`. 설치는 따로 지시가 있을 때만.
