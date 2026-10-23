# 묶음 2 구현 계획 (모드 공법 목록 정리)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 자원 탭 기술 서적과 공법 탭 기술 추가의 모드 공법 줄을 "누르면 비급이 만들어지는 것"만 남기고, 이름이 읽히는 것은 이름으로 보이며, 살릴 수 있는 것은 살린다.

**Architecture:**
- 판정 규칙은 게임 형식 없는 `Services/ModSkillRules.cs`에 두고 `tests/ModMain.Tests`가 시험한다(M1 이름, M2 빈 행, M4 비급 경지 범위).
- `SkillCatalog.Entry`에 숨김 표시와 비급 경지 범위를 두고, 두 목록은 새 `SkillCatalog.Listed`로 거른다. `Find`·`Get`(프리셋·즐겨찾기·개발 명령)은 숨긴 행도 그대로 돌려준다.
- 비급을 살리는 길은 실험(Task 2)으로 고른 뒤 `RollValuesForMod`에 넣는다(Task 3).

**Tech Stack:** C# 7.3, MelonLoader 0.5.4(Il2Cpp), PowerShell 7 검사, 시험 실행기 `tests/ModMain.Tests`.

**Spec:** `docs/superpowers/specs/2026-10-23-mod-skill-list-design.md`(승인 2026-10-23). 측정 근거 `docs/12-review-2026-10-22.md` 3절, 같은 번호 되풀이의 근거 05 문서 76절.

## Global Constraints

- 브랜치 `feat/mod-skill-list`(`develop`에서). 끝나면 사용자 확인 뒤 `develop`에 `--no-ff`. 커밋 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 두 구성 경고 0: `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release`, `… -p:DevTools=false -o <임시>`. 배포 코드는 `Dev\`를 참조하지 않는다.
- 회차 `session.ps1 -Begin <이름> -Fixture base`, 이름은 `msl-1023a`부터, 다시 쓰지 않음, `-End`는 `SAVE RESTORED AND IDENTICAL`. 회차 안에서 `-Restart` 뒤 화면을 누르는 검사는 하지 않는다(만고신화 안내 화면, 05 문서 77절).
- 실험·측정은 만고신화를 실은 채(사용자 환경). `modbooks`·`books-ui` 검사만 `-Exclude 2814696167`.
- 학습 수용 기준: `rbookstudy <sole>`의 `can=`이 같은 경지에서 만든 바닐라 비급(고성검 14112, `rbook 1 14112 <경지> 1 1`)의 `can=`과 같다. 고정값 0으로 보지 않는다.
- 숨김 판정은 설정 표만 읽는다(뽑기 없음). 카탈로그를 만들 때 한 번. 자원 탭 첫 열기(`rperf tabs`)는 0.4~0.6초 안팎을 지킨다.
- 숨김은 목록에서만: `SkillCatalog.Find`·`Get`의 결과는 바꾸지 않는다. 개발 명령 `rmods`·`rperf modroll`·`modrows`는 숨긴 행도 본다.

## Review Focus

- **즐겨찾기에 숨긴 모드 비급 줄**: 줄이 사라지지 않고 보이며(뺄 수 있게), 지급은 이유를 적고 실패해야 한다. Task 5 Step 5.
- **검색어로 숨긴 공법의 이름·번호를 쳤을 때**: 두 목록 모두 나오지 않아야 한다(번호 검색 포함). Task 5 Step 5.
- **"모드만" 칩과 분류 칩의 수**: 숨긴 줄을 세지 않아야 한다(칩 수와 줄 수가 같음). Task 5 Step 5.
- **경지를 바꿨을 때**: 고른 경지에서 못 만드는 모드 줄이 사라지고, 경지를 돌리면 다시 나온다. Task 5 Step 5.
- **프리셋**: 숨긴 모드 공법이 든 기본 프리셋 21번(만고검결)이 지금처럼 쓸 수 있는 줄로 나온다(`pcheck`). Task 6 Step 2.

---

### Task 1: 기준선과 실험 명령

**Files:**
- Create: `src/ModCode/ModMain/Dev/DevCommandsModBook.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs`(새 `Execute` 연결), `src/ModCode/ModMain/Services/SkillCatalog.cs`(실험이 쓰는 `RollValues`·`OwnCombine`·`SourceFor`·`RemapPrefixNumbers`가 `internal`인지 확인, 아니면 `internal`로)

**Interfaces:**
- Produces: 개발 명령
  - `rmodtry <분류> <번호> <경지> <길>`: 길 `now`(지금 `RollValuesForMod`)·`repeat`(4.3 (나))·`own`(가)·`trim`(다). 값을 만들고(가방에 넣음), 읽기 결과와 이름, 그 비급의 `rbookstudy` `can=`, 같은 경지 바닐라 14112의 `can=`을 한 줄로: `path=<길> made=<0|1> name=<이름> can=<n> base=<n> values=<…>`.
  - `rmodsweep <경지> <길> [반복=10]`: 이름 있는(Nameless 아님) 모드 공법 전부에 그 길을 `반복`번 따로 시도(가방에 넣지 않고 읽기까지만, `can=`은 줄마다 첫 성공 하나만 실제로 넣어 잼)해 `.devremote\modsweep-<경지>-<길>.txt`(분류·번호·이름·옵션 행 수·`multi` 합·성공 수/반복·`can` 일치)와 요약 줄 `ok=<10/10 수> partial=<수> fail=<수>`.

- [ ] **Step 1:** `develop`에서 `feat/mod-skill-list`. 두 명령을 만든다. (나) `repeat`: `RemapPrefixNumbers` 뒤에도 모드에 없는 번호가 남은 자리를, 그 자리 가중치가 0보다 크고 중복 한도(`multi`) 안인 모드 번호로 채운다(같은 번호 되풀이 허용). (가) `own`: `OwnCombine(kind, modId)`가 0이 아니면 그 묶음으로 뽑는다. (다) `trim`: 첫 `-1` 앞의 옵션 칸을 모드 행 수만큼으로 줄이고 뒤의 수준 칸도 같은 수로 줄인다.
- [ ] **Step 2:** 두 구성 빌드 경고 0. 커밋 `dev: 모드 비급 실험 명령 rmodtry·rmodsweep`.
- [ ] **Step 3:** 회차 `msl-1023a`: `rperf modroll 2`(기준선 성공·실패), `rperf dump base`, `rperf tabs`. 결과를 `.devremote\msl-baseline.txt`로. `-End`.

### Task 2: 살리기 실험과 판정 모양 정하기

**Files:** 코드 변경 없음(필요하면 Task 1 명령의 버그만 고친다). 결과 `.devremote\msl-decisions.txt`.

- [ ] **Step 1:** 회차 `msl-1023b`: `rmodsweep 2 now`, `rmodsweep 2 repeat`, `rmodsweep 2 own`, `rmodsweep 2 trim`. 각 요약 줄을 기록.
- [ ] **Step 2:** 가장 많이 10/10을 내는 길 하나를 고른다(같으면 `repeat` → `own` → `trim` 순). 고른 길로 `rmodsweep 1 <길>`, `rmodsweep 5 <길>`, `rmodsweep 10 <길>`. `-End`.
- [ ] **Step 3: 판정 모양.** 고른 길의 결과에서 10/10과 그 밖을 가르는 설정 표의 값(옵션 행 수, `multi` 합, 경지)을 찾는다. 판정 `ModSkillRules.BookGrades`(Task 4)의 규칙과, 그 규칙이 틀리지 않음을 보이는 대표 사례 열 줄 안팎(입력 → 기대 경지 범위)을 `msl-decisions.txt`에 적는다. "규칙이 만들어짐이라 했는데 10/10이 아님" 0개가 조건이다.
- [ ] **Step 4 (M5):** 옵션 행 0개인 신법 6개는 `g.conf.battleSkillPrefixValue.GetItems(t, 번호)`를 `t` 1~5(그리고 `MartialType` 값 전부)로 불러 다른 `skillType`에 행이 있는지 본다. 있으면 Task 3에서 그 `skillType`을 쓰게 하고, 없으면 판정에서 "못 만듦".

### Task 3: 고른 길을 `RollValuesForMod`에 넣는다

**Files:**
- Modify: `src/ModCode/ModMain/Services/SkillCatalog.cs` (`RollValuesForMod`, `RemapPrefixNumbers`)

**Interfaces:**
- Consumes: Task 2의 고른 길.
- Produces: `RollValuesForMod(int kindIndex, Entry mod, int grade, out string reason)` 서명 그대로.

- [ ] **Step 1:** Task 2에서 고른 길을 넣는다(실험 명령의 코드를 옮기고, 실험 명령은 이 함수를 부르게 바꾼다). M5에서 다른 `skillType`을 찾았으면 반영.
- [ ] **Step 2:** 두 구성 빌드. 커밋 `fix: 옵션 행이 모자란 모드 공법의 비급 값(<길>)`.
- [ ] **Step 3:** 회차 `msl-1023c`: `rmodsweep 2 now` 요약이 Task 2의 고른 길 결과와 같다. `rperf modroll 2`의 성공 수가 기준선보다 늘었다. `-End`.

### Task 4: 판정 규칙(순수 함수)과 단위 시험

**Files:**
- Create: `src/ModCode/ModMain/Services/ModSkillRules.cs`
- Modify: `tests/ModMain.Tests/ModMain.Tests.csproj`(링크), `tests/ModMain.Tests/Program.cs`(`ModSkillRulesTests()`)

**Interfaces:**
- Produces: `internal static class ModSkillRules`
  - `static bool IsLiteralName(string nameKey)` — M1: 이름 키가 LocalText에 없을 때 그 글을 이름으로 쓸지. 참 조건: 비지 않음, `"0"` 아님, `{`를 품지 않음, 영문·숫자·밑줄만으로 된 글이 아님.
  - `static bool IsBlankRow(string nameKey, string descText)` — M2: 이름 키가 null·빈 글·`"0"`이고 정리한 설명 글이 null·빈 글·`"0"`이면 참.
  - `static void BookGrades(<Task 2 Step 3이 정한 입력>, out int min, out int max)` — M4: 비급이 만들어지는 경지 범위. 못 만들면 `min = 0, max = 0`.

- [ ] **Step 1: 실패하는 시험.**
  - `IsLiteralName`: `"개자비"` 참, `"<COLOR=#FFAA00>용유사방</COLOR>"` 참, `"skill_name_123"` 거짓, `"0"` 거짓, `""` 거짓, `null` 거짓, `"{#jlname}"` 거짓, `"abc{x}"` 거짓.
  - `IsBlankRow`: `("0", "")` 참, `("", null)` 참, `("0", "0")` 참, `("0", "설명")` 거짓, `("skill_1", "")` 거짓.
  - `BookGrades`: `msl-decisions.txt`의 대표 사례마다 한 줄(입력 → `min`·`max`). 옵션 행 0개는 `(0,0)`.
- [ ] **Step 2:** `dotnet run --project tests/ModMain.Tests -c Release` → 컴파일 실패(형식 없음).
- [ ] **Step 3:** 구현. `IsLiteralName`의 "영문·숫자·밑줄만"은 서식 태그를 벗긴 뒤가 아니라 원래 글로 본다(태그가 든 이름은 키 꼴이 아니다).
- [ ] **Step 4:** 시험 `FAILS=0`. 커밋 `feat: 모드 공법 판정 규칙 ModSkillRules`.

### Task 5: 카탈로그 숨김 표시와 두 목록의 거르기

**Files:**
- Modify: `src/ModCode/ModMain/Services/SkillCatalog.cs` (`Entry`, `Add`, 새 `Listed`), `src/ModCode/ModMain/UI/Tabs/MartialTab.cs:271,294`, `src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs:52-95`(`BookShown`, `BookRows`, `BookKindCounts`)

**Interfaces:**
- Consumes: `ModSkillRules`(Task 4).
- Produces:
  - `Entry`의 새 칸 `bool Hidden`, `string HiddenReason`(개발 명령이 보임), `int BookGradeMin`, `int BookGradeMax`(모드 행만. 바닐라는 0).
  - `public static List<Entry> Listed(int kindIndex, string query)` — `Find`와 같되 `Hidden` 제외.
  - `public static bool ModBookFits(Entry entry, int grade)` — 모드 행이고 `BookGradeMin <= grade <= BookGradeMax`.

- [ ] **Step 1:** `Add`에서 모드 행에: 이름 판정에 `IsLiteralName`을 더해 `Nameless`를 정한다(이름 키가 LocalText에 없을 때만 — `ResolveText`가 키를 그대로 돌려준 경우). `IsBlankRow`면 `Hidden = true, HiddenReason = "이름·설명 없음"`. 그 밖은 `BookGrades`로 범위를 정하고 `(0,0)`이면 `Hidden = true, HiddenReason = "비급을 만들 수 없음"`.
- [ ] **Step 2:** 공법 탭 기술 추가(271·294행)와 자원 탭 기술 서적(`BookRows`·`BookKindCounts`)이 `Listed`를 쓴다. `BookShown`은 모드 행이면 `ModBookFits(entry, BookGrade)`(사용자 결정: 못 만드는 줄은 경지 칩과 무관하게 숨김), 바닐라는 지금 그대로.
- [ ] **Step 3:** `rmods`가 줄마다 `Hidden`·`HiddenReason`·`BookGradeMin~Max`를 더 적게 한다.
- [ ] **Step 4:** 두 구성 빌드. 커밋 `feat: 모드 공법 목록에서 이름·설명 없는 행과 비급을 못 만드는 행을 숨김`.
- [ ] **Step 5:** 회차 `msl-1023d`(재시작 없이):
  - `rmods 2`: 숨김 수(이름·설명 없음 459 + 못 만듦), 남은 모드 줄 수.
  - `rperf modroll 2`의 대상은 숨기지 않은 줄만(이 단계에서 `modroll`에 `listed` 인자를 더해 `Listed`만 돌게 한다): 실패 0.
  - 자원 탭: `editor` → `Tab1` → `ResGroupManual` → `ResKind6`(모드만): 칩 글의 수와 `Row` 줄 수가 같고 `rmods`의 남은 수와 같다. 검색 칸에 숨긴 공법 이름 하나와 번호 하나를 넣으면 줄 0. 경지 칩을 1→10으로 바꿀 때 범위 밖 줄이 사라지는지(`BookGradeMin~Max`가 갈리는 공법이 있을 때만).
  - 즐겨찾기: `rfav addbook <분류> <숨긴 번호> 2 1 1` 뒤 즐겨찾기 화면에 그 줄이 보이고, 지급은 실패 문장(지금 `MartialBookService`의 이유)을 낸다.
  - 공법 탭 기술 추가의 모드 줄 수가 `Listed` 수와 같다.
  - 캡처 `msl-mods-books.png`, `msl-mods-martial.png`. `-End`.

### Task 6: 검사·측정·문서·병합

**Files:**
- Modify: `tools/research/ui-check.ps1`(`modbooks`의 대상 번호가 숨겨졌을 때만), `docs/05-editor-v0.1.md`(78절), `docs/12-review-2026-10-22.md`(7절 묶음 2), `docs/06-handoff.md`

- [ ] **Step 1:** `modbooks`가 쓰는 모드 번호(984555698·353535·844854565·59898952 등)가 숨김이면 남은 공법으로 바꾼다.
- [ ] **Step 2:** 회차 `msl-1023e`(만고신화 포함): `ui-check` `items`·`groups`·`tabs`·`translation` FAIL 0, `rperf tabs`의 자원 탭 첫 값, `pcheck 21`(프리셋 21번에 쓸 수 없는 줄 없음), `rperf dump after`와 `base`의 바닐라 줄 비교(모드 줄 말고는 같음). `-End`. 회차 `msl-1023f`(`-Exclude 2814696167`): `modbooks`·`books-ui` FAIL 0. 단위 시험 FAIL 0.
- [ ] **Step 3:** 05 문서 78절: 실험 결과표(길마다, 경지마다), 고른 길, 판정 규칙, 숨긴 수와 이유별 수, 끝내 못 살린 이름 있는 공법의 이름과 수, 전후 `modroll`. 12 문서 7절 묶음 2에 결과. 06 문서(1절 작업 줄, 8.0절, 8.7절 회차 이름, 3절 `ModSkillRules.cs`). 커밋 `docs: 묶음 2 결과`.
- [ ] **Step 4:** 사용자 확인 뒤 `develop`에 `--no-ff` 병합. 설치는 따로 지시가 있을 때만.
