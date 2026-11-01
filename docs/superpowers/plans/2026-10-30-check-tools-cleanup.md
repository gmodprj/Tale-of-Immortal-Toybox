# 검사 도구 정리(묶음 3) 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 검사 스크립트의 복붙 도우미를 `check-lib.ps1`로 모으고(특히 전투 종료 규칙을 어긴 옛 `Leave` 사본), `session.ps1`의 복원 빈틈 셋을 고치고, `session.tests.ps1`이 덮지 않던 갈래 넷을 시험으로 덮는다. 검사의 판정은 바뀌지 않는다.

**Architecture:** 공통 함수는 `check-lib.ps1` 한 곳. 검사 파일은 자기만의 도우미(`St`·`Tr`·`Art` 등)만 남긴다. `session.ps1`은 `KeepFile/RestoreFile` 꼴로 `config.txt`를 다룬다.

**Spec:** `docs/13-review-2026-10-29.md` 5.1절 C1~C11(C7·C12~C17은 범위 밖). 사용자 지시 "묶음 3 시작".

## Global Constraints

- 브랜치 `chore/check-tools-cleanup`. PowerShell 함수 이름은 두 단어 이상(별칭 충돌. 06 8.0절). 줄 끝 CRLF, `session*.ps1`은 BOM.
- 검사의 판정(Check 줄의 뜻)은 바꾸지 않는다. 바뀐 검사는 회차에서 한 부분씩 돌려 FAIL 0을 본다.
- 회차 이름 `ctc-a-1030`부터.

## Review Focus

1. `Leave`를 `LeaveBattle`로 바꾼 검사(trainer4·preset·ui-check combat·artifact)가 전투 뒤 다음 전투를 열 때 빈 화면이 되지 않아야 한다 — 회차에서 trainer4 `onehit`(전투 둘), `combat`(전투 셋).
2. `IsOff`는 단추를 **눌러서** 판정하므로 꺼지지 않은 단추를 누르면 실제로 실행된다 — 기존 동작 그대로(바꾸지 않음), 통합만.
3. `LogHas`의 `-Tail`을 하나로 맞추면(40) 기존 검사가 덜 보던 줄이 들어와 판정이 바뀔 수 있다 — 가장 큰 40을 쓰고 회차에서 확인.
4. `config.txt`가 회차 전에 없었던 경우 `-End`가 지워야 한다 — `session.tests` 새 시험.
5. `-End`가 복원 도중 던지면 `session.mode`가 남아 있어야 한다(C5) — 시험(복원 실패를 흉내내기 어렵다면 순서만 코드로 확인하고 적는다).

---

### Task 1: `session.ps1` — C4·C5·C6 + `session.tests.ps1` 네 시험(C3)

**Files:** `tools/DevRemote/session.ps1`, `tools/DevRemote/session.tests.ps1`.

- [ ] Step 1 시험(RED): `session.tests.ps1` 끝(FAILS 출력 앞)에:
  - 46 `config.txt` 없던 회차: `-Begin` 뒤 `data\config.txt`를 만들고 `-End` → 파일이 없어야 함(`CONFIG RESTORED (there was no file)`).
  - 47 `martial-grid.txt`: `-Begin` 전 `grid=3` → 회차 중 `grid=9`로 바꿈 → `-End` 뒤 `grid=3`, `MARTIAL GRID RESTORED`.
  - 48 `-MakeFixture -From` 끝 역슬래시: `Run $b $none @{ MakeFixture='f1'; From="$($b.save)\" }` → `FIXTURE MADE`, 던지지 않음.
  - 49 `$keptDll` 잔존: `work\session-installed.dll`을 만들어 두고 `-Begin` → `THROWN`, 세이브 복사본 없음.
  - 50 `-End`의 표식 순서(C5): `-Begin` 뒤 `work\session.mode`를 읽고, `-End`를 **복원이 실패하도록**(복사본 폴더 `save-backup-x`를 지운 뒤) 돌리면 던지고 `session.mode`가 아직 남아 있어야 함(복원 전에 지우지 않음).
- [ ] Step 2 `pwsh tools/DevRemote/session.tests.ps1` → 46·48·50 FAIL(47·49는 이미 되는 동작이면 PASS — 그 경우 "덮지 않던 갈래를 덮음"으로 적는다).
- [ ] Step 3 구현: `KeepFile $configFile $keptConfig $noConfig` / `RestoreFile` + `'CONFIG RESTORED'`·`'(there was no file)'`; `DeleteFiles $flag, $modeFile`를 복원 끝(표식 지우는 자리) 뒤로; `$From = $From.TrimEnd('\')`.
- [ ] Step 4 50/50 PASS → 커밋 `fix: session.ps1 — config.txt 없던 회차 복원, 표식은 복원 뒤에 지움, MakeFixture 끝 역슬래시; 시험 다섯(13 문서 C3~C6)`.

### Task 2: `check-lib.ps1` 공통 도우미 + 사본 제거(C1·C8·C2·C10·C11·C9)

**Files:** `tools/research/check-lib.ps1`, `ui-check.ps1`, `trainer4-check.ps1`, `trainer3-check.ps1`, `treasure-check.ps1`, `mastery-check.ps1`, `preset-check.ps1`, `artifact-check.ps1`, `immortal-check.ps1`.

**Interfaces (check-lib에 추가):**
- `EnterBattleScene([int]$level = 20, [switch]$NoStart)`: `fight 1011 <level>` → 전투 진입(960,684) 12초 → `-NoStart`가 아니면 `전투 개시`(956,749)가 `btnStart`로 맞을 때 누름(4초). (ui-check의 기존 `EnterBattle`은 따로 있으므로 이름을 달리한다.)
- `LeaveBattleScene([string]$shotName = 'leave-fail')`: ui-check 2187의 `LeaveBattle` 본문 그대로(btnStart 확인 → `bend` 최대 3번 → btnOK 대기 → `inBattle=False`·`MapMain` 대기 → 2초).
- `LogHasLine([string]$pattern)`: `changes.log`의 마지막 40줄에 패턴이 있는지.
- `ButtonIsOff([string]$name)`: 누르면 `not interactable`.
- `UiText([string]$name)`: `text='…'`의 안쪽(없으면 빈 글). `UiHas([string]$name)`: 요소가 있는지.
- `NumIn([string]$text, [string]$pattern, [double]$default = -1)`: 첫 그룹을 double로.
- `FieldsOf([string]$text)`: `k=v` 쌍을 해시로(preset-check의 `IndexOf('=')` 판: 값에 `=`가 들어도 됨).
- `OpenEditorTab([int]$tab)`: `editor` → `Press "Tab<tab>"`.
- [ ] Step 1 `check-lib.ps1`에 위 함수 추가(머리 주석도 "검사 9개가 쓴다"로).
- [ ] Step 2 사본 치환(이름만 바꿔 호출부 수정):
  - trainer4: `T`→`UiText`, `Has`→`UiHas`, `Tab`→`OpenEditorTab 12`, `Into`→`EnterBattleScene`, `Leave`→`LeaveBattleScene`, `Num`→`NumIn`, 예열 `uistate`(이미 됨). 머리 주석(C9).
  - preset-check: `Fields`→`FieldsOf`(`State`·`Soles`는 그대로 두되 `FieldsOf` 호출), `Into`·`Leave`→공통.
  - ui-check: 2151·2152 `Has`/`T`, 2187 `LeaveBattle` 본문 → `LeaveBattleScene 'rates2-leave-fail'`, 2270 `LeaveBattle`, 2473~2483 combat의 `Into`/`Leave`/`Tab`.
  - artifact-check: `Fields`→`FieldsOf`, `IsOff`→`ButtonIsOff`, `LogHas`→`LogHasLine`, 272·302·351·413의 `IntoBattle`/`LeaveBattle` → `EnterBattleScene 20 -NoStart` 또는 `EnterBattleScene 20`(+ `bgod on`은 호출부에 남김), `LeaveBattleScene`. 421 `Num`은 `-999` 기본과 `\b$key=` 꼴이라 **그대로 둔다**(뜻이 다름).
  - immortal-check: `Fields`→`FieldsOf`.
  - treasure·trainer3·mastery: `LogHas`→`LogHasLine`, `IsOff`/`IsOffBtn`→`ButtonIsOff`, mastery `Text`/`Has`→`UiText`/`UiHas`.
  - C2 treasure-check: 부분마다 시작에 `$traps = Traps`, 검사 `((Traps) -eq $traps)`.
  - C10 mastery-check: `$env:TEMP\mastery-persist.txt` → `Join-Path $work 'mastery-persist.txt'`, `persist` 시작 때 지움.
  - C9 머리 주석: mastery(부분 여섯), trainer3(persist·persist-restart), treasure(give·forge-ui), check-lib.
- [ ] Step 3 `pwsh -NoProfile -Command "[scriptblock]::Create((Get-Content -Raw <파일>)) | Out-Null"`로 아홉 파일 파싱 확인. 남은 사본이 없는지 `grep -n "^function \(Fields\|LogHas\|IsOff\|Into\|Leave\|Num\|Has\|T\|Tab\)\b"`.
- [ ] Step 4 커밋 `chore: 검사 도우미를 check-lib로(전투 진입·종료·LogHasLine·ButtonIsOff·UiText·FieldsOf·OpenEditorTab), 사본 제거, treasure Traps 차이, mastery persist 파일 위치, 머리 주석(13 문서 C1·C2·C8~C11)`.

### Task 3: 회차 검증

- [ ] `ctc-a-1030`: `trainer4-check ui`·`items` → `ui-check combat`. `ctc-b-1030`: `trainer4-check onehit`(전투 둘) → `treasure-check service` → `forge-ui`. `ctc-c-1030`: `mastery-check persist` → `-Restart` → `persist-restart`; `trainer3-check suit-ui`. `ctc-d-1030`: `artifact-check hud-battle` → `hud-battle-slots`; `immortal-check catalog`; `preset-check extras` → `extras-battle`. 모두 FAIL 0. `session.tests.ps1` 50 PASS.

### Task 4: 문서·병합

- [ ] 05 문서 91절(공통 함수 표, 바뀐 검사, 시험 다섯, 회차 결과), 06 5절(`check-lib` 함수 목록 줄, `session.tests` 수 45→50, 함정 목록의 "확인 단추가 맞을 때까지" 규칙이 공통 함수에 들어갔음), 06 1절·8.0절 9번, 13 문서 C1~C6·C8~C11 행 `고침(91절)`. 커밋, `develop` 병합.
