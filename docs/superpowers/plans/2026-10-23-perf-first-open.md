# 성능 1 구현 계획 (자원 탭 첫 열기와 `Init` 줄이기)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 자원 탭 첫 열기를 4.0초에서 0.5초 안팎으로 줄이고, `Init`의 LocalText 전체 훑기를 셋에서 많아야 하나로 줄인다. 목록·번역 결과는 바이트까지 그대로 둔다.

**Architecture:**
- 측정 도구 `rperf`를 `develop`으로 옮기고, 실험 명령 셋(`dump`·`lt`·`confid`)을 더한다. 고치기 전 기준선 회차에서 두 갈림(4.1 (가)/(나), 4.5의 2 건너뛰기 여부)을 먼저 정한다.
- 새 서비스 `LocalTextIndex`가 `Init`에서 LocalText를 한 번 훑는다. 모드 행 `kr` 덮기, ch→kr 전체 일치 사전, 기술 카탈로그의 `ChOf`가 이것을 같이 쓴다.
- 판정 규칙(`LocalTextRules`, `TranslationStamp`)은 게임 형식 없이 따로 두어 `tests/ModMain.Tests`가 시험한다. 그 밖은 회차의 실측과 목록 덤프 비교로 검증한다.

**Tech Stack:** C# 7.3, MelonLoader 0.5.4(Il2Cpp 언홀로어 형식), PowerShell 7 검사 스크립트, 시험 실행기 `tests/ModMain.Tests`.

**Spec:** `docs/superpowers/specs/2026-10-22-perf-first-open-design.md`(승인 2026-10-23). 측정 근거는 `docs/12-review-2026-10-22.md` 2절.

## Global Constraints

- 브랜치 `feat/perf-first-open`(`develop`에서 딴다). 끝나면 `develop`에 `git merge --no-ff`. `main`·`develop`에 직접 커밋하지 않는다. 커밋 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 두 구성 모두 경고 0으로 컴파일: `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release`와 `… -c Release -p:DevTools=false -o <임시 폴더>`. 배포 코드는 `Dev\`를 참조하지 않는다.
- 게임 검증은 `pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base` … `-End <이름>`. 회차 이름은 `perf1-1023a`부터 차례로, 다시 쓰지 않는다. `-End`는 `SAVE RESTORED AND IDENTICAL`이어야 한다. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 측정은 창작마당 모드 40개를 모두 실은 상태(`-Exclude` 없음), 사용자의 `favorites.txt` 그대로(비급 줄 0). 즐겨찾기를 지우는 `books-ui`보다 먼저 잰다.
- 결과 같음의 기준: 고치기 전·뒤 `rperf dump`의 두 파일이 바이트까지 같고, `rloc`·`rzhstat`의 요약이 같다.
- Il2Cpp 제네릭 호출은 회차에서 시험한 뒤에만 쓴다(`allText.TryGetValue`는 `MissingMethodException`).
- 문서에는 어셈블리·게임에서 확인한 이름만 적는다. 기능 규칙은 05 문서 새 절(77절)에.

## Review Focus

- **번역 적용 중 예외**: `LocalTextIndex.Scan`이 중간에 예외를 내면 `TextTranslationService`는 자기 `LocalTextMap()`으로 되돌아가 지금처럼 사전을 만들어야 한다(번역이 통째로 빠지면 안 됨). Task 5에 시험 단계가 있다.
- **같은 키가 LocalText에 둘 이상**: 지금 `ChOf`는 앞의 행의 ch를 쓴다. (가)를 고르면 `allText`가 같은 행을 가리키는지 Task 3에서 센 결과로 보장한다. 덤프 비교(`Search` 칸)가 잡는다.
- **사용자가 게임 실행 중에 번역 파일을 고친 뒤 주 화면으로 돌아감**: 건너뛰기를 넣으면 파일 표지가 달라져 다시 적용해야 한다. Task 8에 시험이 있다.
- **기술 서적 그룹을 열지 않은 채 다른 탭이 카탈로그를 만든 경우**: 자원 탭으로 돌아오면 "…" 대신 수가 보여야 한다. Task 7에 확인 단계가 있다.
- **배포 빌드의 `Init` 로그**: `ms=`가 배포 빌드에도 찍혀야 한다(개발 전용 코드에 넣지 않는다). Task 2에서 `-ReleaseBuild` 회차로 본다.

---

### Task 1: `rperf`를 `develop`으로 옮기고 실험 명령 셋을 더한다

**Files:**
- Create: `src/ModCode/ModMain/Dev/DevCommandsPerf.cs` — `git show feat/perf-research:src/ModCode/ModMain/Dev/DevCommandsPerf.cs`의 내용을 가져온다.
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs` — `DevCommandsPerf.Execute(p)` 호출을 `DevCodeScan.Execute` 앞에 넣는다(실험 브랜치의 자리와 같다).

**Interfaces:**
- Produces: 개발 명령 `rperf tabs|parts|skillbuild|modroll|modrows`(그대로), 새로 `rperf dump <꼬리>`, `rperf lt`, `rperf confid`.

- [ ] **Step 1:** `develop`에서 `feat/perf-first-open`을 만들고 위 두 파일을 옮긴다. 개발·배포 두 구성을 빌드해 경고 0을 본다.
- [ ] **Step 2: `rperf dump <꼬리>`** — 기술 카탈로그 다섯 분류(`SkillCatalog.Find(k, "")`)의 줄마다 `분류\tBaseId\tName\tDesc\tGradeMin\tGradeMax\tIsMod\tNameless\tNameFromCh\tBasType\tSearch`, 아이템 카탈로그(`ItemCatalog.Select(new ItemCatalog.Filter(), null)` 순서 그대로)의 줄마다 `Id\tName\tDesc\tLevel\tType\tSubType\tTypeName\tSubTypeName\tGroup.Key\tKind.Order\tSearch`와 `ItemCatalog.Currencies`를 `.devremote\perf-dump-<꼬리>-skills.txt`·`-items.txt`로 쓴다(UTF-8, BOM 없음, `\n`). 돌려주는 줄은 두 파일의 줄 수.
- [ ] **Step 3: `rperf lt`** — 기술 카탈로그 항목의 이름 키 761개(`battleSkillAttack`·`battleStepBase`·`battleAbilityBase`의 `name` 칸, 카탈로그에 든 행만)에 대해 `g.conf.localText.allText.ContainsKey(key)`와 `allText[key].ch`를 부르고, 지금 `SkillCatalog.ChOf(key)`와 비교한다. 돌려주는 줄: `keys=<수> ok=<같음> diff=<다름> missing=<없음> ex=<예외 이름 또는 none> ms=<761개 찾는 시간>`. 다른 키는 `.devremote\perf-lt-diff.txt`에. 예외가 나면 잡아서 이름을 적는다(게임을 죽이지 않게 한 번만 시도).
- [ ] **Step 4: `rperf confid`** — `g.conf.localText`와 `TextTranslationService`의 `Targets` 표 16개 각각의 `Pointer`(Il2Cpp 객체 주소, 16진), 모드 행(`isModExtend`) 첫 행의 `key`·`kr`, 표마다 지금 한자가 없는(바꿔 끼운) 칸 첫 하나의 값을 한 줄씩 `.devremote\perf-confid-<n>.txt`(n은 1부터 늘림)에 쓴다. `Targets`는 지금 `private`이므로 `internal`로 바꾼다(배포 코드의 접근 한정자만 바뀜).
- [ ] **Step 5:** 두 구성 빌드 경고 0. 커밋 `dev: rperf를 develop으로, dump·lt·confid 추가`.

### Task 2: `Init` 단계별 시간을 로그에 남긴다

**Files:**
- Modify: `src/ModCode/ModMain/ModMain.cs` (`Init`), `Services/TranslationService.cs` (`Apply`), `Services/TextTranslationService.cs` (`Apply`·`ApplyTable` 호출부)

**Interfaces:**
- Produces: `Latest.log` 줄 `[TlToybox] translations: … ms=<전체> ensure=<ms> scan=<ms>`, `[TlToybox] texts: … ms=<전체> ensure=<ms> ltmap=<ms>` 그리고 표마다 기존 `<표>=<칸 수>` 뒤에 `@<ms>`(예: `dramaDialogue=12@840`), `[TlToybox] init ms=<전체> dress=<ms> grid=<ms> patch=<ms>`.

- [ ] **Step 1:** `System.Diagnostics.Stopwatch`로 위 조각을 잰다. 요약 글(`LastSummary`)에도 같은 글을 넣는다(`rloc`·`rzhstat`이 보인다). 결과 같음 비교에 쓰는 앞부분(`file=… applied=…`)의 모양은 바꾸지 않고 끝에 덧붙인다.
- [ ] **Step 2:** 두 구성 빌드 경고 0. 커밋 `perf: Init 단계별 시간을 로그에 남김`.
- [ ] **Step 3:** 회차 `perf1-1023a -ReleaseBuild`를 열어 `Latest.log`에 세 줄이 `ms=`와 함께 나오는지 본 뒤 `-End`.

### Task 3: 기준선 회차와 두 갈림 정하기

**Files:** 코드 변경 없음. 결과는 `.devremote`와 Task 9의 문서에.

- [ ] **Step 1:** 회차 `perf1-1023b -Fixture base`(개발 빌드, 이 브랜치의 지금 상태 = 고치기 전 + 측정 도구). 순서: `rperf tabs`(자원 탭 첫 값이 기준선) → `rperf parts` → `rperf skillbuild` → `rperf dump before` → `rperf lt` → `rperf confid`(1번 파일). `Latest.log`의 `Init` 줄을 `.devremote\perf1-init-before.txt`로 복사.
- [ ] **Step 2: 주 화면 복귀.** 게임 메뉴의 돌아가기로 주 화면에 나간 뒤 같은 세이브로 다시 들어간다. 길: 먼저 `uibuttons <게임 메뉴 화면 이름>`과 `xrefl`로 돌아가기 단추가 부르는 메서드를 찾아 부르는 개발 명령 `rperf main`을 더한다. 찾지 못하면 `rc.ps1 -Cmd "click 959 391"`(게임 메뉴가 떠 있을 때)로 한다. 다시 들어가기는 `session.ps1`이 쓰는 이름 클릭(`Login/.*btnInto`, `CacheItem.*btnRoot1`)을 `rc.ps1`로 같은 순서로. 두 번째 `Init`의 로그 줄을 `perf1-init-second-before.txt`에, `rperf confid`(2번 파일)를 남긴다. 그 뒤 `-End`.
- [ ] **Step 3: 갈림 (가)/(나).** `rperf lt`가 `ex=none`이고 `diff=0`이면 (가). 아니면 (나). 결과를 `perf1-decisions.txt`에 한 줄로.
- [ ] **Step 4: 갈림 건너뛰기.** confid 1·2번 파일에서 `localText`와 표 16개의 주소가 **모두** 같고 모드 행 `kr`·표 칸 값이 **모두** 같으면 "건너뛰기 가능". 하나라도 다르면 "건너뛰기 없음"(Task 8의 Step 1~3을 하지 않는다). 같은 파일에 한 줄로.
- [ ] **Step 5:** 주 화면 복귀를 끝내 하지 못했으면 "건너뛰기 없음"으로 정하고 이유를 적는다. 멈추지 않는다.

### Task 4: 판정 규칙을 게임 밖으로 빼고 시험한다

**Files:**
- Create: `src/ModCode/ModMain/Services/LocalTextRules.cs`, `src/ModCode/ModMain/Services/TranslationStamp.cs`
- Modify: `tests/ModMain.Tests/ModMain.Tests.csproj`(두 파일 `Compile Include`), `tests/ModMain.Tests/Program.cs`(`LocalTextRulesTests()`·`TranslationStampTests()`를 `Main`에)

**Interfaces:**
- Produces:
  - `internal static class LocalTextRules`
    - `static string ModKr(string kr, string ch, string translated)` — 모드 행의 새 `kr`. 바꾸지 않으면 null. 규칙은 지금 `TranslationService.Apply`의 것: `translated != null`이면 `translated`, 아니면 `kr`이 null·빈 글·`"0"`이고 `ch`가 비어 있지 않으면 `ch`, 그 밖은 null.
    - `static bool KeepsWhole(string ch, string kr)` — ch→kr 전체 일치 사전에 넣을 줄인지. 지금 `LocalTextMap`의 조건 그대로(같은 ch의 중복 거르기는 부르는 쪽이 한다).
  - `internal static class TranslationStamp`
    - `static string Of(IList<string> paths)` — 경로마다 `경로|크기|LastWriteTimeUtc.Ticks`, 없으면 `경로|-`를 `;`로 잇는다.

- [ ] **Step 1: 실패하는 시험.** `Program.cs`에 `Check`로:
  - `ModKr("0", "中文", null) == "中文"`, `ModKr("", "中文", null) == "中文"`, `ModKr("한글", "中文", null) == null`, `ModKr("0", "", null) == null`, `ModKr("0", "中文", "번역") == "번역"`, `ModKr("한글", "中文", "번역") == "번역"`.
  - `KeepsWhole("中文", "한국어")` 참; `KeepsWhole("中文", "0")`, `KeepsWhole("中文", "中文")`, `KeepsWhole("abc", "한국어")`(ch에 한자 없음), `KeepsWhole("中文", "한中")`(kr에 한자), `KeepsWhole(null, "한")`, `KeepsWhole("中文", "")` 거짓.
  - 임시 폴더의 파일 둘로 `Of` 두 번이 같고, 한 파일에 한 줄을 덧붙이면 달라지고, 없는 경로는 `|-`로 끝난다.
- [ ] **Step 2:** `dotnet run --project tests/ModMain.Tests -c Release` → 컴파일 실패(형식 없음).
- [ ] **Step 3:** 두 파일을 만든다. `KeepsWhole`은 `TextReplacer.HasChinese`를 쓴다(시험 프로젝트에 이미 연결됨).
- [ ] **Step 4:** 시험 → `FAILS=0`(기존 113 + 새 것).
- [ ] **Step 5:** `TranslationService.Apply`와 `TextTranslationService.LocalTextMap`의 판정을 이 두 함수로 바꾼다(동작 같음). 두 구성 빌드. 커밋 `refactor: 번역 판정 규칙을 LocalTextRules로`.

### Task 5: S1 — LocalText를 한 번만 훑는다

**Files:**
- Create: `src/ModCode/ModMain/Services/LocalTextIndex.cs`
- Modify: `Services/TranslationService.cs`(`Apply`가 `LocalTextIndex.Scan`을 부름), `Services/TextTranslationService.cs`(`Apply`가 `LocalTextIndex.ChToKr`를 씀, 없으면 `LocalTextMap()`), `Services/SkillCatalog.cs`(`ChOf`가 `LocalTextIndex.ChOf`를 부름, 자기 `chByKey` 삭제)

**Interfaces:**
- Consumes: `LocalTextRules.ModKr`·`KeepsWhole`(Task 4), Task 3의 갈림 (가)/(나).
- Produces: `internal static class LocalTextIndex`
  - `static void Scan(Dictionary<string, string> modTranslations, out int applied, out int fallback, out HashSet<string> seen)` — LocalText `allConfList`를 한 번 훑는다. 행마다 `isModExtend`·`ch`·`kr`를 읽고, 모드 행이면 `key`를 읽어 `ModKr`로 `kr`을 덮은 뒤, 덮은 값으로 `KeepsWhole`이면 `ChToKr[ch] = kr`(같은 ch는 앞의 것). (나)면 모든 행의 `key`도 읽어 `keyToCh`(같은 키는 앞의 것)를 채운다.
  - `static Dictionary<string, string> ChToKr { get; }` — 마지막 `Scan`의 결과. `Scan`이 끝나지 못했으면 null.
  - `static string ChOf(string key)` — (가): `allText.ContainsKey(key)`면 `allText[key].ch ?? ""`, 아니면 `""`. (나): `keyToCh`에서. `key`가 null이면 `""`. (나)인데 `Scan`이 아직 없으면 지금 `SkillCatalog.ChOf`의 훑기로 한 번 채운다.
  - 비우기는 `Scan`이 시작할 때만 한다(새로 채우기 전에). `SkillCatalog.Reset()`은 `LocalTextIndex`를 건드리지 않는다: `TranslationService.Apply`가 `Scan` 바로 뒤에 `SkillCatalog.Reset()`을 부르므로, 거기서 비우면 `TextTranslationService`가 쓸 사전이 사라진다.
  - `rperf skillbuild`의 `chdict` 조각은 `LocalTextIndex.ChOf("")` 시간이 된다. 이름은 그대로 두고 Task 9의 05 문서에 뜻이 바뀐 것을 적는다.

- [ ] **Step 1:** `LocalTextIndex`를 만들고 `TranslationService.Apply`의 훑기 자리에 `Scan`을 넣는다. `seen`·`applied`·`fallback`으로 `unused`·`LastSummary`를 지금과 같게 만든다. 번역 파일이 없거나(`"없음"`) 읽기 실패여도 `Scan`은 빈 사전으로 돈다(ch→kr 사전이 필요하므로).
- [ ] **Step 2:** `TextTranslationService.Apply`: `LocalTextIndex.ChToKr`가 null이 아니면 그것을, null이면 지금의 `LocalTextMap()`을 쓴다. `lt=` 값은 사전 크기로 같게.
- [ ] **Step 3:** 개발 명령 `rperf ltfail`(다음 `Init`의 `Scan`이 처음 행에서 예외를 던지게 하는 정적 표시)을 `DevCommandsPerf`에 더한다. `LocalTextIndex`에 `internal static bool DevFailNext`를 두고 `#if DEVTOOLS`로만 읽는다.
- [ ] **Step 4:** 두 구성 빌드. 커밋 `perf: LocalText를 Init에서 한 번만 훑음(S1)`.
- [ ] **Step 5:** 회차 `perf1-1023c -Fixture base`: `rperf skillbuild`의 `chdict`가 (가)면 20 ms 안쪽·(나)면 0에 가깝고, `rperf dump s1`이 `before`와 바이트까지 같고(`cmp`), `rloc`·`rzhstat` 요약의 `ms=` 앞부분이 Task 3 기준선과 같다. 이어서 `rperf ltfail` → `-Restart` → `rzhstat`의 `lt=`가 기준선과 같은지(되돌아가기 길). `-End`.

### Task 6: S2 — 기술 카탈로그를 한 번에 만든다

**Files:**
- Modify: `src/ModCode/ModMain/Services/SkillCatalog.cs` (`All`, `Add`의 호출부, `OwnCombine` 사전 만들기)

**Interfaces:**
- Produces: `public static bool IsBuilt { get; }` — 다섯 분류가 만들어져 있으면 참. `Find`·`Get`·`All`의 동작과 결과 순서는 그대로.

- [ ] **Step 1:** `All(k)`가 캐시가 비었으면 `BuildAll()`을 부르게 한다. `BuildAll`은 `battleSkillAttack._allConfList`를 한 번 훑으며 행의 `type`·`id`·`name`·`desc`·`gradeMin`·`gradeMax`·`basType`·`className`·`isModExtend`를 한 번씩 읽어 분류 0·1·4의 목록에 `Add`, 이어서 신법·심법 표. 분류마다의 "이름 없는 행을 끝으로" 정렬은 그대로.
- [ ] **Step 2:** `OwnCombine`이 쓰는 묶음 사전을 `BuildAll`의 처음에 만든다(지금처럼 처음 부를 때 만들어지면 그대로 둔다).
- [ ] **Step 3:** 두 구성 빌드. 커밋 `perf: 기술 카탈로그를 한 번에 만듦(S2)`.
- [ ] **Step 4:** 회차 `perf1-1023d`: `rperf dump s2`가 `before`와 같고, `rperf skillbuild`의 `kind0`~`kind4` 합이 기준선보다 줄었다. `-End`.

### Task 7: S3·S4 — 기술 서적 수를 늦게 세고, 아이템 분류 이름을 한 번만

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs`(334행 그룹 단추, 495행 수 갱신, `CountLabel.Set`), `UI/Tabs/ResourcesTab.Books.cs`(`BookCount`), `Services/ItemCatalog.cs`(`Build`)

**Interfaces:**
- Consumes: `SkillCatalog.IsBuilt`(Task 6).
- Produces: `BookCount()`가 카탈로그가 없고 지금 그룹이 "기술 서적"(`ManualKey`)이 아니면 `-1`. `CountLabel.Set(int)`과 `GroupButton`은 `-1`을 `"…"`로 쓴다.

- [ ] **Step 1:** 위 동작을 넣는다. "기술 서적" 그룹을 고르면(`SetGroup(ManualKey)`) 카탈로그가 만들어지고 다음 그리기에서 수가 나온다.
- [ ] **Step 2:** `ItemCatalog.Build` 안에 `Dictionary<int, string>`을 두고 `CategoryName`을 분류 번호마다 한 번만 부른다. 결과 글은 그대로. 설명 정리(`CleanDesc`)는 늦추지 않는다(스펙 4.4: 이득이 0.1초를 넘을 때만. Step 4의 `rperf parts`에서 아이템 카탈로그가 여전히 0.3초를 넘으면 숫자를 05 문서에 적고 다음 묶음 후보로 남긴다).
- [ ] **Step 3:** 두 구성 빌드. 커밋 `perf: 기술 서적 수를 늦게 세고 분류 이름을 한 번만(S3·S4)`.
- [ ] **Step 4:** 회차 `perf1-1023e`: 첫 `rperf tabs`의 자원 탭 값(목표 0.5초 안팎), `rperf dump s3`가 `before`와 같음, 첫 열기 캡처(`win.ps1 -Do shot`)에서 "기술 서적 …", `Press 'ResGroupManual'` 뒤 캡처에서 수. 이어서 공법 탭을 먼저 연 뒤 자원 탭으로 돌아오면 "…"가 아니라 수가 보이는지(`uitext ResGroupManual`). 비급 즐겨찾기 줄이 있을 때의 첫 열기 시간: `rfav addbook 0 <기술> 3 1 1` → `-Restart` → `rperf tabs`(숫자만 기록). `-End`.

### Task 8: `Init` — 복귀 뒤 건너뛰기와 표 칸 바꿔 끼우기

**Files:**
- Create: `src/ModCode/ModMain/Services/ReapplyGuard.cs` (Step 1~3을 할 때만)
- Modify: `Services/TranslationService.cs`, `Services/TextTranslationService.cs`

**Interfaces:**
- Consumes: Task 3의 갈림, `TranslationStamp.Of`(Task 4), `rperf confid`(Task 1).
- Produces (건너뛰기 가능일 때): `internal static class ReapplyGuard`
  - `static void Remember()` — 모두 적용한 뒤에 부른다: 번역 파일 셋의 `TranslationStamp.Of`(각 `EnsureFile` **뒤**), `g.conf.localText`와 `Targets` 16개 표의 `Pointer`, 모드 행 하나의 `kr`과 표마다 바꾼 칸 하나의 값.
  - `static bool Unchanged()` — 위 것이 **모두** 같을 때만 참.

- [ ] **Step 1 (건너뛰기 가능일 때만):** `ReapplyGuard`를 만들고, `TranslationService.Apply`의 처음에서 `EnsureFile` 셋을 부른 뒤 `Unchanged()`면 `Scan`·`SkillCatalog.Reset()`·표 칸 바꿔 끼우기를 건너뛰고 `LastSummary` 끝에 ` reapply=skipped`를 붙인다. 아니면 모두 하고 끝에 `Remember()`.
- [ ] **Step 2 (건너뛰기 가능일 때만):** 회차 `perf1-1023f`: 주 화면 복귀(Task 3 Step 2의 길) 뒤 두 번째 `Init` 로그에 `reapply=skipped`와 `ms=`가 줄어든 것, 복귀 뒤 `rperf dump s8`이 `before`와 같은 것. 이어서 데이터 폴더의 `translations\texts.txt` 끝에 주석 줄 `# perf1 test`를 덧붙이고 다시 복귀 → 로그에 `reapply=skipped`가 **없음**. `-End`(번역 폴더는 `-End`가 되돌린다).
- [ ] **Step 3 (건너뛰기 가능일 때만):** 커밋 `perf: 주 화면 복귀 뒤 바뀐 것이 없으면 번역 적용을 건너뜀`.
- [ ] **Step 4:** Task 3의 `perf1-init-before.txt`에서 표마다 `@<ms>`가 큰 표(합의 80%를 차지하는 표들)를 고른다. 그 표들은 `ApplyTable`의 반사 대신 형식을 아는 함수(`g.conf.<표>._allConfList[i].<칸>` 읽기·쓰기)로 바꾼다. 같은 거르기 규칙(한자 있음, `Replacer.Whole`이 다르면 씀). 나머지는 반사 그대로. 고른 표 이름을 커밋 메시지에 적는다.
- [ ] **Step 5:** 두 구성 빌드. 회차 `perf1-1023g`: `rzhstat`의 `tables=`·`cells=`와 표마다 칸 수가 기준선과 같고, `texts:` 줄의 `ms=`가 줄었다. 커밋 `perf: 큰 표의 칸 바꿔 끼우기를 형식 코드로`.

### Task 9: 마지막 측정·검사·문서·병합

**Files:**
- Modify: `docs/05-editor-v0.1.md`(새 77절), `docs/12-review-2026-10-22.md`(7절 묶음 1 줄에 결과), `docs/06-handoff.md`(1절 작업 줄, 8.0절, 8.7절 회차 이름, 3절의 `Services` 새 파일 셋)

- [ ] **Step 1:** 회차 `perf1-1023h`(측정, 기준선과 같은 조건): `rperf tabs`·`parts`·`skillbuild`, `Init` 로그 두 번(시작, 주 화면 복귀 뒤), `rperf dump after`가 `before`와 바이트까지 같음. `-End`.
- [ ] **Step 2:** 검사 회차 `perf1-1023i`(`-Exclude` 없음): `ui-check.ps1 -Part tabs`, `items`, `groups`, `numbers`, `translation`, `modbooks`, 마지막에 `books-ui`. 모두 `FAILS=0`. 단위 시험 `FAILS=0`. 하나라도 실패하면 고치고 그 검사를 다시 돌린다.
- [ ] **Step 3:** 05 문서 77절: 전후 표(자원 탭 첫 열기, 기술 카탈로그 조각, `Init` 단계별, 복귀 뒤 `Init`), 갈림 결과((가)/(나), 건너뛰기), 서적 단추 "…" 규칙, 비급 즐겨찾기가 있을 때의 숫자, 회차 이름. 12 문서 7절 묶음 1에 "끝남, 결과는 05 문서 77절". 06 문서 갱신. 커밋 `docs: 성능 1 결과`.
- [ ] **Step 4:** 사용자 확인 뒤 `develop`에 `--no-ff` 병합. 게임 설치(`tools/install.ps1`)는 사용자가 따로 지시할 때만.
