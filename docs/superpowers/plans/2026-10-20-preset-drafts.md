# 리뷰 번들 초안 → 기본 프리셋 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (inline). Steps use checkbox (`- [ ]`) syntax.

**Goal:** 바닐라 빌드 11~20을 기본 프리셋(+`*.meta.json`)으로 넣는다. 이미 프리셋 폴더가 있는 사용자에게도 새 기본값이 풀리게 한다.

**Architecture:**
- 새 기본값 판정은 순수 함수 `PresetDefaults.ToWrite`로 만들고 `tests/ModMain.Tests`로 시험한다. `PresetService.List`·`RestoreDefaults`가 폴더의 `.defaults` 기록을 읽고 쓴다.
- 빌드 내용은 원문을 내장 브라우저로 읽어 작업 기록(`2026-10-20-preset-drafts-builds.md`)에 남긴다. 중국어 이름은 새 개발 명령 `pfindch`로 번호에 대조한다.
- 검증은 회차의 `pcheck`·`pstrict`·`preset-check files/ui/preset-all`이다.

**Spec:** `docs/superpowers/specs/2026-10-20-preset-drafts-design.md`(6.1절 포함). 작성 규칙은 `docs/superpowers/specs/2026-10-14-preset-tab-design.md` 6절을 따른다.

## Global Constraints

- 브랜치 `feat/preset-drafts`에서 하고, `develop`에는 `--no-ff`로 병합한다(사용자 확인 뒤).
- 회차는 `pd-1020a`부터, `-Fixture base`로 연다. `-End`는 `SAVE RESTORED AND IDENTICAL`이어야 한다. 사용자의 게임이 켜져 있으면 멈춘다.
- 프리셋 형식은 `format 1`, `kind "preset"`이다. 공략에 없는 줄에는 `filled: true`를 단다. 메타는 69절 형식이고, 상태는 `공략`·`채움`·`대체`·`프리셋 밖`이다.
- 원문 인용은 한 번에 15단어 미만, 문장 하나 이하로 한다. 웹은 읽기만 한다.
- 배포 빌드 경고 0. 개발 명령은 `Dev/`에 둔다.

## Review Focus

- 사용자가 지운 기본 파일이 `.defaults` 때문에 다시 풀리지 않는지.
- `.defaults`가 없는 옛 폴더에서 사용자가 지운 01~10이 다시 생기지 않는지.
- 같은 빌드를 번호만 바꿔 넣지 않았는지(스펙 6.1절 기준).
- 메타와 프리셋의 출처가 3절 표와 맞는지.

---

### Task 1: 새 기본값 풀기와 검사

**Files:**
- Create: `src/ModCode/ModMain/Services/PresetDefaults.cs`(순수)
- Modify: `src/ModCode/ModMain/Services/PresetService.cs`(`List`·`WriteDefaults`·`RestoreDefaults`)
- Modify: `tests/ModMain.Tests`(링크, `PresetDefaultsTests`)
- Modify: `tools/research/preset-check.ps1`(`files` 셋 추가, `preset-all` 기대값을 `pdefaults embedded`에서 읽기)

**Interfaces:**
- `internal static class PresetDefaults`
  - `public static readonly string[] Legacy`: 01~10의 파일 이름 열 개.
  - `public const string RecordName = ".defaults"`
  - `public static List<string> ToWrite(IList<string> embedded, IList<string> record, ICollection<string> onDisk)`
    - `record`가 null이면 옛 폴더로 보고, 알고 있는 이름을 `Legacy`로 본다.
    - 알고 있는 이름에 없고 폴더에도 없는 내장 이름을 돌려준다(이름순).

- [ ] **Step 1: 시험을 쓴다.**
  - 새 이름만: embedded {01,11}, record {01}, onDisk {} → [11]
  - 지운 것은 다시 안 풂: embedded {01,11}, record {01,11}, onDisk {} → []
  - 옛 폴더: embedded {01..10,11}, record null, onDisk {02} → [11]
  - 이미 있으면 안 씀: embedded {11}, record {}, onDisk {11} → []
- [ ] **Step 2:** 실패 확인 → 구현 → 통과(`dotnet run --project tests/ModMain.Tests -c Release`, `FAILS=0`).
- [ ] **Step 3: `files` 검사에 셋을 더하고 `preset-all` 기대값을 바꾼다.**
  - 기록과 파일을 지운 새 기본값은 목록을 열면 생긴다.
  - 기록은 남기고 파일만 지운 기본값은 다시 생기지 않는다(기존 검사).
  - `.defaults`를 지운 옛 폴더에서 01은 다시 생기지 않고, 11 이후 기본값 하나는 생긴다. 이 검사는 Task 3 뒤 내장이 11개 이상일 때만 돈다. 내장이 10개면 INFO로 건너뛴다.
- [ ] **Step 4:** 회차 `pd-1020a`에서 고치기 전 실패를 보고, 구현 뒤 `pd-1020b`에서 `files` 통과를 본다.
- [ ] **Step 5:** 커밋 `feat: 새 기본 프리셋을 기존 폴더에도 풀기(.defaults 기록)`

### Task 2: `pfindch` 개발 명령

- `pfindch <조각>`: 다섯 분류의 기술(`SkillCatalog` 항목의 원 이름 키), 역천개명(`roleCreateFeature.name`), 세트(심법 세트 이름)에서 LocalText `ch`에 그 조각이 든 것을 찾는다.
  - 출력 꼴: `[분류 번호 한국어이름 ch=중국어]`
  - 구현은 `GameTool.LS`가 아니라 LocalText의 `ch` 칸을 읽는다(`rloc key`가 쓰는 방식, 43절).
- 회차에서 `pfindch 月影`이 14113(월영검)을 내는지 본다. 커밋한다.

### Task 3: 원문 조사와 빌드 작성

빌드 11~20 각각에 다음을 한다.
1. 내장 브라우저로 출처를 읽는다(스펙 3절).
2. `2026-10-20-preset-drafts-builds.md`에 원문 기술·옵션 방향·심법·운명·신혼·명우를 적는다.
3. `pfindch`·`pfind`·`popts`·`pfate`로 번호와 옵션을 정한다.
4. 스펙 6.1절의 "사실상 같음"·"대체" 규칙을 적용한다.
5. `Presets/<번호>-<이름>.json`과 `.meta.json`을 쓴다.

- 회차에서 `prestore`, `pcheck`(0), `pstrict`(옵션·이름 위반 0)를 본다. 커밋은 빌드 몇 개씩 묶어서 한다.

### Task 4: 마무리

- 회차에서 `preset-check files`, `ui`, `preset-all`을 돌린다(10분을 넘으면 회차를 나눈다). 배포 빌드에서 `pdefaults embedded`가 새 수와 같은지 본다.
- 05 문서 70절, 인계 8.1·5절을 고친다.
- 마지막 리뷰(새 리뷰어)를 받고, 병합은 사용자 확인 뒤에 한다.
