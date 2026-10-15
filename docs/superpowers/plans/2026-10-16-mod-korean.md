# 창작마당 모드 한글화(G2c) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 창작마당 모드가 더한 LocalText 행(`isModExtend`, 지금 62개)을 번역 파일로 한글로 덮어 게임 화면·편집기·비급 이름에 "0" 대신 한국어가 보이게 하고, 번역이 없는 키는 중국어로 보이게 한다.

**Architecture:** 번역 파일 `Translations/mods.txt`(키<TAB>한국어)를 DLL에 내장하고 처음 실행 때 모드 데이터 폴더 `translations\mods.txt`로 풀어 둔다(기본 프리셋과 같은 틀). `TranslationService.Apply()`가 `ModMain.Init`마다 파일을 읽어 `g.conf.localText.allConfList`의 모드 행 `kr`을 덮고(없으면 `ch`) `SkillCatalog.Reset()`을 부른다. 개발 명령 `rloc`(결과·다시 적용·미번역 내보내기·키 읽기)과 검사 `ui-check.ps1 -Part translation`이 검증이다. 회차 스크립트가 `translations` 폴더를 프리셋처럼 보관·복원한다.

**Tech Stack:** C# 7.3 / .NET 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4, PowerShell 7 검증 스크립트, `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-16-mod-korean-design.md`

## Global Constraints

- 브랜치 `feat/mod-korean`(develop에서. 스펙 커밋 `e0f67e7`·`54368aa`). 끝에서 `git merge --no-ff`로 `develop`에 병합한다. `main`에는 병합하지 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`. 커밋에 `-c core.autocrlf=false`를 쓰지 않는다(`.gitattributes`의 `text=auto`가 LF로 저장한다).
- 실험 브랜치 `feat/korean-research`의 `wsloc`·`wslocdump`는 가져오지 않는다.
- 두 빌드 구성(개발 `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo`, 배포 `-p:DevTools=false -o <임시 폴더>`) 모두 오류 0·경고 0, `bin\Release`에 `MOD_sWP6UL.dll` 하나.
- 게임 검증은 `pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서. 회차 이름은 `ws-1016p`부터 차례로(`ws-1016a`~`o`는 썼다), 다시 쓰지 않는다. `-End`가 `SAVE RESTORED AND IDENTICAL`·`WORKSHOP FOLDERS REMOVED: 0`·`TRANSLATIONS RESTORED…`. `-Restart`는 DLL을 다시 설치하지 않으므로 코드를 고치면 `-End` 뒤 새 이름으로 `-Begin`. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 번역 파일: UTF-8(BOM 없음), `키<TAB>한국어`, `#` 줄과 빈 줄 무시, 한국어 안의 줄바꿈은 `\n` 두 글자, 같은 키가 두 번이면 뒤의 것, 자리 표시(`$…$`, `&…&`, `{…}`, `<…>`)는 원문 그대로. 내용은 스펙 부록 A의 59행(중국어가 빈 2개와 `exists`는 넣지 않는다). 스펙 표의 `\|`는 파일에서 `|`다.
- 적용 규칙: `isModExtend` 행만. 사전에 키가 있으면 `kr` = 번역(앞뒤 공백 제거, `\n` → 줄바꿈), 없고 `kr`이 "0"이거나 빈 글이고 `ch`가 비어 있지 않으면 `kr = ch`. 기본 행은 사전에 키가 있어도 건드리지 않는다. 끝에 `SkillCatalog.Reset()`.
- 결과 문장(`rloc`, `Latest.log`): `file=<있음|풀어 둠|없음|읽기 실패 …> rows=<사전 수> applied=<번역으로 덮은 수> fallback=<ch로 덮은 수> unused=<사전에 있으나 모드 표에 없는 키 수> broken=<탭 없는 줄 수>`. 첫 적용의 기대값은 `rows=59 applied=59 fallback=1 unused=0 broken=0`(`fallback`은 `exists`).
- 데이터 폴더의 파일이 있으면 내장본으로 덮지 않는다(사용자 수정 보호).
- 문서에는 확인한 이름만, 확인하지 못한 것은 "미확인"과 사유. 사용자에게 보이는 글은 한국어.

## Review Focus

1. 번역 파일을 메모장으로 고쳐 BOM과 CRLF가 생겼다 → 그대로 읽혀 59행이 적용된다. (Task 2 검사 "a file with BOM and CRLF still applies")
2. 사전에 게임 기본 키(`s_liuxue`)를 적었다 → 기본 행은 바뀌지 않고 `unused`로 센다. (Task 2 검사 "a base key in the file is ignored and counted as unused")
3. 키 앞뒤에 공백이 있는 줄 → 공백을 지우고 적용한다. (Task 2 검사 "spaces around a key are trimmed")
4. 번역 안의 `\n` → 게임 글처럼 줄바꿈이 된다. (Task 2 검사 "a backslash-n becomes a line break")
5. 파일을 지웠다 → 다음 적용(재시작)에서 내장본이 다시 풀린다. (Task 2 검사 "a missing file is extracted again")

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `tools/DevRemote/session.ps1`, `session.tests.ps1` | 수정 | `translations` 폴더 보관·복원(프리셋과 같은 틀). 시험 26·27 |
| `src/ModCode/ModMain/Translations/mods.txt` | 새로 | 번역 59행(스펙 부록 A에서 생성) |
| `src/ModCode/ModMain/ModMain.csproj` | 수정 | `Translations\*.txt`를 내장 리소스로 |
| `src/ModCode/ModMain/Services/TranslationService.cs` | 새로 | 파일 풀기·읽기·적용·미번역 내보내기·키 읽기 |
| `src/ModCode/ModMain/ModMain.cs` | 수정 | `Init`에서 `TranslationService.Apply()` |
| `src/ModCode/ModMain/Dev/DevCommandsTranslation.cs`, `Dev/DevCommands.cs` | 새로·수정 | `rloc`, `rloc apply`, `rloc dump`, `rloc key <키>` |
| `tools/research/ui-check.ps1` | 수정 | 부분 `translation`; `modbooks`의 중국어 이름 기대값을 한글로 |
| `docs/05-editor-v0.1.md`(43절), `docs/06-handoff.md` | 수정 | 6절 |

---

### Task 1: 회차 스크립트가 `translations` 폴더를 보관·복원한다

**Files:**
- Modify: `tools/DevRemote/session.ps1` (변수 셋, `-Begin`의 보관본 검사·보관, `-End`의 복원)
- Modify: `tools/DevRemote/session.tests.ps1` (끝의 정리 루프 앞에 시험 둘)

**Interfaces:**
- Produces: `-Begin`이 `<데이터 폴더>\translations`를 `.devremote\session-translations`에 복사(없으면 `session-translations.absent`)하고, `-End`가 되돌린 뒤 `TRANSLATIONS RESTORED` 또는 `TRANSLATIONS RESTORED (there was no folder)`를 보인다. 남은 보관본이 있으면 `-Begin`이 멈춘다. Task 2의 회차가 이것에 기댄다(검사가 파일을 지우고 바꾼다).

- [ ] **Step 1: 시험 둘을 쓴다**

`tools/DevRemote/session.tests.ps1`의 `# 26.` 블록(모드 폴더 잠김 시험) 뒤, 정리 `foreach ($root in $boxes) {` 앞에:

```powershell
# 27. -End는 번역 폴더(translations)를 회차 전의 내용으로 되돌린다. 검사가 파일을 지우고 바꿔도 사용자가 고친 번역이 남는다
$b = NewBox
[void][IO.Directory]::CreateDirectory("$($b.data)\translations")
[IO.File]::WriteAllText("$($b.data)\translations\mods.txt", "k`tedited by the user")
[void](Run $b $none @{ Begin = 'x' })
[IO.File]::WriteAllText("$($b.data)\translations\mods.txt", "k`toverwritten by a check")
[IO.File]::WriteAllText("$($b.data)\translations\untranslated.txt", 'left by a check')
$o = Run $b $none @{ End = 'x' }
Check 'End restores the translations folder' ($o -match 'TRANSLATIONS RESTORED' -and (Tree "$($b.data)\translations") -eq "\mods.txt=k`tedited by the user" -and -not (Test-Path "$($b.work)\session-translations")) $o

# 28. 회차 전에 번역 폴더가 없었으면 -End 뒤에도 없다(다음 Init 때 내장본이 풀린다). 남은 보관본이 있으면 -Begin이 멈춘다
$b = NewBox
[void](Run $b $none @{ Begin = 'x' })
[void][IO.Directory]::CreateDirectory("$($b.data)\translations")
[IO.File]::WriteAllText("$($b.data)\translations\mods.txt", 'extracted during the session')
$o = Run $b $none @{ End = 'x' }
Check 'End removes a translations folder the session made' ($o -match 'TRANSLATIONS RESTORED \(there was no folder\)' -and -not (Test-Path "$($b.data)\translations") -and -not (Test-Path "$($b.work)\session-translations.absent")) $o
$b = NewBox
[void][IO.Directory]::CreateDirectory("$($b.work)\session-translations")
[IO.File]::WriteAllText("$($b.work)\session-translations\mods.txt", 'the only copy')
$o = Run $b $none @{ Begin = 'x' }
Check 'Begin refuses to overwrite a leftover translations copy' ($o -match '^THROWN' -and (Test-Path "$($b.work)\session-translations\mods.txt") -and -not (Test-Path "$($b.work)\session.name")) $o
```

- [ ] **Step 2: 시험을 돌려 실패를 본다**

Run: `pwsh tools/DevRemote/session.tests.ps1`
Expected: 새 검사 셋이 `FAIL`(`TRANSLATIONS RESTORED`가 없고, 보관본이 있어도 `-Begin`이 선다), 나머지는 `PASS`, `FAILS=3`.

- [ ] **Step 3: `session.ps1`을 고친다**

변수 선언에서 `$noPresets = …` 줄 뒤에:

```powershell
$translationsDir = Join-Path $data 'translations'
$keptTranslations = Join-Path $dir 'session-translations'        # 회차 전의 translations 폴더(번역 파일)
$noTranslations = Join-Path $dir 'session-translations.absent'   # 회차 전에 translations 폴더가 없었다는 표시
```

`-Begin`의 프리셋 보관본 검사 줄(`if ((Test-Path -LiteralPath $keptPresets) -or (Test-Path $noPresets)) { throw …}`) 뒤에:

```powershell
  if ((Test-Path -LiteralPath $keptTranslations) -or (Test-Path $noTranslations)) { throw "끝내지 않은 회차의 번역 보관본이 있습니다($keptTranslations 또는 $noTranslations). translations 폴더와 비교해 되돌린 뒤 지우고 다시 시작합니다." }
```

`-Begin`의 프리셋 보관 줄(`if (Test-Path -LiteralPath $presetsDir) { Copy-Item … } else { [IO.File]::WriteAllText($noPresets, '') }`) 뒤에:

```powershell
  # 번역 폴더도 세이브 밖에 있다. 검사(ui-check.ps1의 translation)가 파일을 지우고 바꾸므로 회차 전의 것을 떠 둔다.
  if (Test-Path -LiteralPath $translationsDir) { Copy-Item -LiteralPath $translationsDir -Destination $keptTranslations -Recurse } else { [IO.File]::WriteAllText($noTranslations, '') }
```

`-End`의 `DeleteFiles $noPresets` 줄 뒤에:

```powershell
  # 번역 폴더를 회차 전으로 되돌린다. 회차 전에 없었으면 지운다.
  if (Test-Path -LiteralPath $keptTranslations) {
    robocopy $keptTranslations $translationsDir /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "번역 폴더 복원에 실패했습니다(robocopy $LASTEXITCODE). 회차 전의 것은 $keptTranslations 에 있습니다." }
    [IO.Directory]::Delete($keptTranslations, $true)
    'TRANSLATIONS RESTORED'
  } elseif (Test-Path $noTranslations) {
    if ((Test-Path -LiteralPath $translationsDir) -and $translationsDir.StartsWith($data + '\')) { [IO.Directory]::Delete($translationsDir, $true) }
    'TRANSLATIONS RESTORED (there was no folder)'
  }
  DeleteFiles $noTranslations
```

머리 주석의 `-End` 설명에 "translations 폴더"를 덧붙인다.

- [ ] **Step 4: 시험을 돌려 통과를 본다**

Run: `pwsh tools/DevRemote/session.tests.ps1`
Expected: 모두 `PASS`, `FAILS=0`.

- [ ] **Step 5: 커밋**

```bash
git add tools/DevRemote/session.ps1 tools/DevRemote/session.tests.ps1
git commit -m "feat: 회차 스크립트 — translations 폴더를 프리셋처럼 보관·복원(시험 27·28)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 2: 번역 파일, 적용 서비스, `rloc`, 검사

**Files:**
- Create: `src/ModCode/ModMain/Translations/mods.txt`
- Modify: `src/ModCode/ModMain/ModMain.csproj` (`EmbeddedResource` 한 줄)
- Create: `src/ModCode/ModMain/Services/TranslationService.cs`
- Modify: `src/ModCode/ModMain/ModMain.cs` (`Init`)
- Create: `src/ModCode/ModMain/Dev/DevCommandsTranslation.cs`, Modify: `src/ModCode/ModMain/Dev/DevCommands.cs` (`Execute` 머리)
- Modify: `tools/research/ui-check.ps1` (부분 `translation` 추가, `modbooks`의 기대값 넷)

**Interfaces:**
- Consumes: Task 1의 `TRANSLATIONS RESTORED`; `ChangeLog.DataDir`(`Safety/ChangeLog.cs`), `SkillCatalog.Reset()`, `g.conf.localText.allConfList`(`ConfLocalTextItem.key/ch/kr/isModExtend`), `GameTool.LS`, `PresetService.Defaults()`의 리소스 읽기 틀.
- Produces: `TranslationService.Dir`, `FilePath`, `UntranslatedPath`, `EnsureFile()`(`"있음"|"풀어 둠"|"없음"`), `Load(out int broken)`(`Dictionary<string,string>`), `Apply()`(결과 문장, `LastSummary`에도), `DumpUntranslated()`(쓴 수), `Describe(string key)`(`ls=… kr=… ch=…`). 개발 명령 `rloc`, `rloc apply`, `rloc dump`, `rloc key <키>`. 검사 `ui-check.ps1 -Part translation`.

- [ ] **Step 1: 검사 `translation`을 쓰고 `modbooks`의 기대값을 한글로 바꾼다**

`tools/research/ui-check.ps1`의 `  'modbooks' {` 줄 바로 앞에:

```powershell
  'translation' {
    # 한글화 스펙 5절. 창작마당 모드 3390738990이 설치돼 있어야 한다. 회차의 -End가 translations 폴더를 되돌린다.
    $traps = Traps
    $tdir = Join-Path $data 'translations'
    $tfile = Join-Path $tdir 'mods.txt'
    function Range { if ((Cmd 'uitext ItemTableRange') -match '전체 (\d+)개') { [int]$Matches[1] } elseif ((Cmd 'uitext ItemTableRange') -match "text='0개'") { 0 } else { -1 } }
    $s = Cmd 'rloc' 300
    # Init은 세이브 진입 전에 두 번 불릴 수 있다(인계 6절). 두 번째면 파일이 "있음"이고 exists 행은 이미 ch라 fallback이 0이다.
    Check 'translations are applied on init' ($s -match 'file=(풀어 둠|있음) rows=59 applied=59 fallback=[01] unused=0 broken=0') $s
    Check 'the translation file was written to the data folder' (Test-Path -LiteralPath $tfile) $tfile
    $k = Cmd 'rloc key skill_name_middle984555698' 300
    Check 'a mod skill name resolves to Korean' ($k -match 'ls=열염장 kr=열염장 ch=烈炎掌') $k
    Check 'a mod term resolves to Korean' ((Cmd 'rloc key s_shenqiang' 300) -match 'ls=신창') (Cmd 'rloc key s_shenqiang' 300)
    Check 'a backslash-n becomes a line break' ((Cmd 'rloc key effect_descshenqiang' 300) -match '초 뒤\s*신창 상태에 들어가며') (Cmd 'rloc key effect_descshenqiang' 300)
    Check 'the English flag row falls back to its Chinese column' ((Cmd 'rloc key 0lkK4v' 300) -match 'ls=exists kr=exists') (Cmd 'rloc key 0lkK4v' 300)
    $mods = Cmd 'rmods' 3000
    Check 'the catalog shows Korean names without the ch fallback' ($mods -match '1:984555698:열염장:basPalm:False:' -and $mods -match '4:59898952:천살신창:') $mods
    $made = Cmd 'rbook 1 984555698 3 1 1' 20000
    Check 'a mod book is named in Korean' ($made -match 'made=1' -and $made -match '열염장\(품질') $made
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupManual' | Out-Null; Press 'ResClear' | Out-Null; Press 'ResKind6' | Out-Null
    Check 'the row shows the Korean name' ((Cmd 'uitext BookName984555698') -match "text='열염장'") (Cmd 'uitext BookName984555698')
    Press 'BookName984555698' | Out-Null
    $d = Cmd 'uitext ItemDetail'
    Check 'the box below no longer mentions a missing Korean name' ($d -notmatch '한글 이름이 없어' -and $d -match '열염장') $d
    Cmd 'ui set ItemQuery 열염' | Out-Null
    Check 'Korean search finds the mod skill' ((Range) -eq 1) (Cmd 'uitext ItemTableRange')
    Cmd 'ui set ItemQuery 烈炎' | Out-Null
    Check 'Chinese search still finds it' ((Range) -eq 1) (Cmd 'uitext ItemTableRange')
    Press 'ItemQueryClear' | Out-Null
    # 파일 수정 → rloc apply: 깨진 줄, 모르는 키, 기본 키, 공백 키, 한 줄 바꾸기
    $backup = [IO.File]::ReadAllText($tfile)
    $text = $backup.Replace("skill_name_middle984555698`t열염장", "skill_name_middle984555698`t열염장2") + "`nbroken line without tab`nno_such_key`t아무개`ns_liuxue`t엉뚱`n skill_name_middle353535 `t 장기광도3 `n"
    [IO.File]::WriteAllText($tfile, $text)
    $s = Cmd 'rloc apply' 3000
    Check 'a broken line and unknown keys are counted, the rest applies' ($s -match 'rows=61 applied=59 fallback=0 unused=2 broken=1') $s
    Check 'an edited line wins' ((Cmd 'rloc key skill_name_middle984555698' 300) -match 'ls=열염장2') (Cmd 'rloc key skill_name_middle984555698' 300)
    Check 'a base key in the file is ignored and counted as unused' ((Cmd 'rloc key s_liuxue' 300) -match 'ls=유혈 kr=유혈') (Cmd 'rloc key s_liuxue' 300)
    Check 'spaces around a key are trimmed' ((Cmd 'rloc key skill_name_middle353535' 300) -match 'ls=장기광도3') (Cmd 'rloc key skill_name_middle353535' 300)
    # BOM + CRLF로 저장해도 읽힌다
    [IO.File]::WriteAllText($tfile, $backup.Replace("`n", "`r`n"), [Text.Encoding]::UTF8)
    $s = Cmd 'rloc apply' 3000
    Check 'a file with BOM and CRLF still applies' ($s -match 'rows=59 applied=59 fallback=0 unused=0 broken=0' -and (Cmd 'rloc key skill_name_middle984555698' 300) -match 'ls=열염장 ') $s
    [IO.File]::WriteAllText($tfile, $backup)
    Cmd 'rloc apply' 3000 | Out-Null
    # 미번역 내보내기
    $dumped = Cmd 'rloc dump' 3000
    Check 'dump writes only the untranslated rows' ($dumped -match 'written=1' -and (Get-Content -LiteralPath (Join-Path $tdir 'untranslated.txt') -Raw) -match "0lkK4v`t`texists") $dumped
    # 파일을 지우면 다시 풀린다
    [IO.File]::Delete($tfile)
    $s = Cmd 'rloc apply' 3000
    Check 'a missing file is extracted again' ($s -match 'file=풀어 둠 rows=59 applied=59' -and (Test-Path -LiteralPath $tfile)) $s
    # 주석만 있는 파일: 사전 0, 번역은 그대로 남는다(이미 덮은 kr은 되돌리지 않는다)
    [IO.File]::WriteAllText($tfile, "# only a comment`n")
    $s = Cmd 'rloc apply' 3000
    Check 'a comment-only file applies nothing and breaks nothing' ($s -match 'rows=0 applied=0 fallback=0 unused=0 broken=0') $s
    [IO.File]::WriteAllText($tfile, $backup)
    Cmd 'rloc apply' 3000 | Out-Null
    Shot 'translation'
    Check 'no new traps' ((Traps) -eq $traps)
  }
```

`modbooks` 블록의 네 검사를 한글 기대값으로 바꾼다(이름이 한글로 풀리면 `NameFromCh`가 거짓이고 아래 칸의 안내가 사라진다):

| 바꾸기 전 | 바꾼 뒤 |
|---|---|
| `$mods -match '984555698:烈炎掌:' -and $mods -match '59898952:千杀神枪:' -and $mods -match '3535351:赤炎剑:basSword:True:'` | `$mods -match '984555698:열염장:' -and $mods -match '59898952:천살신창:' -and $mods -match '3535351:적염검:basSword:False:'` |
| `$mods -match '1:984555698:[^:]*:[^:]*:True:-:\d+' -and $mods -match '0:3535351:[^:]*:basSword:True:-:\d+'` | `$mods -match '1:984555698:[^:]*:[^:]*:False:-:\d+' -and $mods -match '0:3535351:[^:]*:basSword:False:-:\d+'` |
| `$s -match 'min=1 max=10 name=烈炎掌'` | `$s -match 'min=1 max=10 name=열염장'` |
| `-and $d -match '한글 이름이 없어 중국어 이름을' ` (아래 칸 검사의 이 구절만) | (지운다) |

검사 이름 `mod skill names come from the Chinese column`은 `mod skill names are Korean after translation`으로.

- [ ] **Step 2: 회차를 열고 실패를 본다**

사용자의 게임이 꺼져 있는지 본 뒤:

```bash
pwsh tools/DevRemote/session.ps1 -Begin ws-1016p -Fixture base
pwsh tools/research/ui-check.ps1 -Part translation > .superpowers/translation-red.txt 2>&1; grep -c "^PASS" .superpowers/translation-red.txt; grep "^FAIL" .superpowers/translation-red.txt | cut -c1-100
pwsh tools/research/ui-check.ps1 -Part modbooks > .superpowers/modbooks-red.txt 2>&1; grep "^FAIL" .superpowers/modbooks-red.txt | cut -c1-100
```

Expected: `translation`의 첫 줄들이 FAIL(`rloc`가 UNKNOWN, 파일 없음, 이름이 "0"), 파일 수정 검사는 `$tfile`이 없어 예외로 멈출 수 있다(RED로 충분). `modbooks`는 바꾼 네 검사가 FAIL(이름이 아직 중국어, `True`, 안내문이 있음), 나머지 PASS.

- [ ] **Step 3: 번역 파일을 스펙 부록에서 만든다**

```bash
py -3 -I -X utf8 - <<'EOF'
import re
spec = open('docs/superpowers/specs/2026-10-16-mod-korean-design.md', encoding='utf-8').read()
rows = []
seen = False
for line in spec.split('\n'):
    if line.startswith('## 부록 A'):
        seen = True
        continue
    if not seen or not line.startswith('| '):
        continue
    cells = [c.strip() for c in re.split(r'(?<!\\)\|', line.strip().strip('|'))]
    if len(cells) != 3 or cells[0] in ('키', '---'):
        continue
    key, ch, kr = cells
    kr = kr.replace('\\|', '|')
    rows.append(key + '\t' + kr)
assert len(rows) == 59, len(rows)
head = '# 창작마당 모드의 글 한글화. 키<TAB>한국어. # 줄은 주석. 한국어 안의 줄바꿈은 \\n 두 글자. 자리 표시($…$, &…&, {…}, <…>)는 원문 그대로.\n# 출처: docs/superpowers/specs/2026-10-16-mod-korean-design.md 부록 A. 고쳐 쓰려면 모드 데이터 폴더의 translations\\mods.txt를 고친다(이 파일은 처음 한 번만 풀린다).\n'
open('src/ModCode/ModMain/Translations/mods.txt', 'w', encoding='utf-8', newline='\n').write(head + '\n'.join(rows) + '\n')
print('rows', len(rows))
EOF
grep -c "	" src/ModCode/ModMain/Translations/mods.txt
```

Expected: `rows 59`, 탭이 있는 줄 59. (heredoc 안의 `\\n`·`\\|`는 Bash가 한 겹 벗겨 Python에 `\n`·`\|`로 간다. 결과 파일 머리에 `\n` 두 글자가 보이는지 `head -2`로 본다. 다르면 스크립트를 `.superpowers`에 파일로 쓰고 돌린다.)

`ModMain.csproj`의 프리셋 `EmbeddedResource` 줄 뒤에:

```xml
    <!-- 창작마당 모드 글의 번역. DLL 안에 넣어 두고 translations 폴더에 파일이 없을 때 풀어 놓는다. 이름은 "Translations/mods.txt"다. -->
    <EmbeddedResource Include="Translations\*.txt" LogicalName="Translations/%(Filename)%(Extension)" />
```

- [ ] **Step 4: `TranslationService`를 쓴다**

`src/ModCode/ModMain/Services/TranslationService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 창작마당 모드의 글(LocalText의 isModExtend 행) 한글화. 설계: docs/superpowers/specs/2026-10-16-mod-korean-design.md 3절.
    // 번역 파일 <데이터 폴더>\translations\mods.txt(키<TAB>한국어). 없으면 DLL 안의 것을 풀어 둔다(있으면 건드리지 않는다. 사용자가 고친 것을 지킨다).
    // Init마다 Apply()가 모드 행의 kr을 덮는다: 사전에 있으면 번역, 없고 kr이 "0"이거나 비었으면 ch(중국어). 기본 행은 건드리지 않는다.
    internal static class TranslationService
    {
        private const string ResourceName = "Translations/mods.txt";

        public static string LastSummary = "not applied";
        private static HashSet<string> lastKeys = new HashSet<string>();   // 마지막 적용 때 사전에 있던 키(rloc dump가 쓴다)

        public static string Dir
        {
            get { return Path.Combine(ChangeLog.DataDir, "translations"); }
        }

        public static string FilePath
        {
            get { return Path.Combine(Dir, "mods.txt"); }
        }

        public static string UntranslatedPath
        {
            get { return Path.Combine(Dir, "untranslated.txt"); }
        }

        // 파일이 없으면 내장본을 풀어 둔다. "있음"·"풀어 둠"·"없음"(내장본도 없음).
        public static string EnsureFile()
        {
            if (File.Exists(FilePath))
            {
                return "있음";
            }
            string text = Embedded();
            if (text == null)
            {
                return "없음";
            }
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, text, new UTF8Encoding(false));
            return "풀어 둠";
        }

        private static string Embedded()
        {
            Assembly assembly = typeof(TranslationService).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                {
                    return null;
                }
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        // 파일 → 사전. '#' 줄과 빈 줄은 무시, 탭이 없는 줄은 broken으로 센다. 키·글의 앞뒤 공백을 지우고 "\n"을 줄바꿈으로. 같은 키는 뒤의 것.
        public static Dictionary<string, string> Load(out int broken)
        {
            var map = new Dictionary<string, string>();
            broken = 0;
            foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                string line = raw.TrimEnd('\r');
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#"))
                {
                    continue;
                }
                int tab = line.IndexOf('\t');
                string key = tab < 0 ? "" : line.Substring(0, tab).Trim();
                if (key.Length == 0)
                {
                    broken++;
                    continue;
                }
                map[key] = line.Substring(tab + 1).Trim().Replace("\\n", "\n");
            }
            return map;
        }

        public static string Apply()
        {
            string file;
            var map = new Dictionary<string, string>();
            int broken = 0;
            try
            {
                file = EnsureFile();
                if (file != "없음")
                {
                    map = Load(out broken);
                }
            }
            catch (Exception e)
            {
                file = "읽기 실패 " + e.GetType().Name + ": " + e.Message;
                map = new Dictionary<string, string>();
            }
            int applied = 0, fallback = 0;
            var seen = new HashSet<string>();
            var all = g.conf.localText.allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfLocalTextItem item = all[i];
                if (item == null || !item.isModExtend || item.key == null)
                {
                    continue;
                }
                string text;
                if (map.TryGetValue(item.key, out text))
                {
                    item.kr = text;
                    seen.Add(item.key);
                    applied++;
                }
                else if ((string.IsNullOrEmpty(item.kr) || item.kr == "0") && !string.IsNullOrEmpty(item.ch))
                {
                    item.kr = item.ch;
                    fallback++;
                }
            }
            int unused = 0;
            foreach (string key in map.Keys)
            {
                if (!seen.Contains(key))
                {
                    unused++;
                }
            }
            lastKeys = new HashSet<string>(map.Keys);
            SkillCatalog.Reset();
            LastSummary = "file=" + file + " rows=" + map.Count + " applied=" + applied + " fallback=" + fallback + " unused=" + unused + " broken=" + broken;
            MelonLogger.Msg("[TlToybox] translations: " + LastSummary);
            return LastSummary;
        }

        // 사전에 없는 모드 행(ch가 있는 것)을 "키<TAB><TAB>중국어"로 쓴다. 다음 작업이 번역해 mods.txt에 합친다.
        public static int DumpUntranslated()
        {
            var sb = new StringBuilder();
            int written = 0;
            var all = g.conf.localText.allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfLocalTextItem item = all[i];
                if (item == null || !item.isModExtend || item.key == null || lastKeys.Contains(item.key) || string.IsNullOrEmpty(item.ch))
                {
                    continue;
                }
                sb.Append(item.key).Append("\t\t").Append(item.ch.Replace("\r", "").Replace("\n", "\\n")).Append('\n');
                written++;
            }
            Directory.CreateDirectory(Dir);
            File.WriteAllText(UntranslatedPath, sb.ToString(), new UTF8Encoding(false));
            return written;
        }

        public static string Describe(string key)
        {
            var all = g.conf.localText.allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfLocalTextItem item = all[i];
                if (item != null && item.key == key)
                {
                    return "ls=" + GameTool.LS(key) + " kr=" + item.kr + " ch=" + item.ch;
                }
            }
            return "ls=" + GameTool.LS(key) + " row=none";
        }
    }
}
```

`ModMain.cs`의 `Init`에서 `EditorHost.Ensure();` 다음 줄에:

```csharp
            try
            {
                TranslationService.Apply();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] translations failed: " + e.GetType().Name + ": " + e.Message);
            }
```

- [ ] **Step 5: 개발 명령 `rloc`을 쓴다**

`src/ModCode/ModMain/Dev/DevCommandsTranslation.cs`:

```csharp
namespace MOD_sWP6UL
{
    // 한글화의 검증용 명령(tools/research/ui-check.ps1의 translation 부분).
    //   rloc           : TranslationService.Apply의 마지막 결과 file=… rows=… applied=… fallback=… unused=… broken=…
    //   rloc apply     : 지금 파일로 다시 적용하고 결과
    //   rloc dump      : 사전에 없는 모드 행을 translations\untranslated.txt에 "키<TAB><TAB>중국어"로. written=<수>
    //   rloc key <키>  : ls=<GameTool.LS> kr=<행의 kr> ch=<행의 ch>
    internal static class DevCommandsTranslation
    {
        public static string Execute(string[] p)
        {
            if (p[0] != "rloc")
            {
                return null;
            }
            if (p.Length == 1)
            {
                return TranslationService.LastSummary;
            }
            switch (p[1])
            {
                case "apply":
                    return TranslationService.Apply();
                case "dump":
                    return "written=" + TranslationService.DumpUntranslated();
                case "key":
                    return TranslationService.Describe(p[2]);
                default:
                    return "unknown rloc " + p[1];
            }
        }
    }
}
```

`Dev/DevCommands.cs`의 `Execute` 머리(`string workshop = DevCommandsWorkshop.Execute(p);` 앞)에:

```csharp
            string translation = DevCommandsTranslation.Execute(p);
            if (translation != null)
            {
                return translation;
            }
```

- [ ] **Step 6: 빌드하고 회차를 바꿔 통과를 본다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
pwsh tools/DevRemote/session.ps1 -End ws-1016p
pwsh tools/DevRemote/session.ps1 -Begin ws-1016q -Fixture base
pwsh tools/research/ui-check.ps1 -Part translation > .superpowers/translation-t2.txt 2>&1; grep "^FAIL" .superpowers/translation-t2.txt | cut -c1-140; tail -1 .superpowers/translation-t2.txt
pwsh tools/research/ui-check.ps1 -Part modbooks > .superpowers/modbooks-t2.txt 2>&1; grep "^FAIL" .superpowers/modbooks-t2.txt | cut -c1-140; tail -1 .superpowers/modbooks-t2.txt
pwsh tools/research/ui-check.ps1 -Part base > .superpowers/base-t2.txt 2>&1; tail -1 .superpowers/base-t2.txt
pwsh tools/research/ui-check.ps1 -Part tabs > .superpowers/tabs-t2.txt 2>&1; tail -1 .superpowers/tabs-t2.txt
grep -a "translations:" "/e/Games/Tale of Immortal/MelonLoader/Latest.log" | head -3
pwsh tools/DevRemote/session.ps1 -End ws-1016q
```

Expected: 빌드 경고 0·오류 0. `-End ws-1016p`에 `TRANSLATIONS RESTORED (there was no folder)`(회차 전에 폴더가 없었으므로. 있었다면 `TRANSLATIONS RESTORED`). `translation` `FAILS=0`, `modbooks` `FAILS=0`, `base`·`tabs` `FAILS=0`. `Latest.log`에 `[TlToybox] translations: file=풀어 둠 rows=59 applied=59 fallback=1 unused=0 broken=0`. `-End ws-1016q`가 `SAVE RESTORED AND IDENTICAL`과 `TRANSLATIONS RESTORED (there was no folder)`. 실패하면 원인을 고치고 새 회차에서 다시.

- [ ] **Step 7: 배포 구성과 커밋**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check"
ls src/ModCode/ModMain/bin/Release/*.dll
git add src/ModCode/ModMain/Translations/mods.txt src/ModCode/ModMain/ModMain.csproj src/ModCode/ModMain/Services/TranslationService.cs src/ModCode/ModMain/ModMain.cs src/ModCode/ModMain/Dev/DevCommandsTranslation.cs src/ModCode/ModMain/Dev/DevCommands.cs tools/research/ui-check.ps1
git commit -m "feat: 창작마당 모드 한글화 — 번역 파일(DLL 내장·데이터 폴더), Init에서 모드 행의 kr 덮기(없으면 ch), rloc, 검사 translation

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

Expected: 배포 구성 경고 0·오류 0, DLL 하나(번역 파일은 DLL 안에 있다).

---

### Task 3: 문서, 검토, 병합

**Files:**
- Modify: `docs/05-editor-v0.1.md` (42절 뒤에 43절)
- Modify: `docs/06-handoff.md` (1절, 5절, 8.3b절, 8.5절, 8.7절)

- [ ] **Step 1: 05 문서 43절을 쓴다**

42절 끝에 이어 붙인다(회차 결과로 값을 채운다):

```markdown
## 43. 창작마당 모드 한글화(G2c) (2026-10-16 추가)

스펙 `docs/superpowers/specs/2026-10-16-mod-korean-design.md`(부록 A가 번역표), 계획 `plans/2026-10-16-mod-korean.md`. 설계 전 실험은 인계 8.3e절(`wssetkr`)과 회차 `ws-1016o`(모드 글 62행 실측).

### 규칙

- 번역 파일 `src/ModCode/ModMain/Translations/mods.txt`(키<TAB>한국어, `#` 주석, `\n` 두 글자가 줄바꿈, 자리 표시는 원문 그대로)를 DLL에 내장하고, `TranslationService.EnsureFile()`이 모드 데이터 폴더 `TlToybox\translations\mods.txt`에 없을 때만 풀어 둔다. 사용자가 고친 파일은 덮지 않는다. 새 판의 번역을 받으려면 그 파일을 지운다.
- `ModMain.Init`마다 `TranslationService.Apply()`: LocalText의 모드 행(`isModExtend`)만, 사전에 키가 있으면 `kr`을 번역으로, 없고 `kr`이 "0"이거나 비었으면 `ch`로 덮는다. 기본 행은 건드리지 않는다. 끝에 `SkillCatalog.Reset()`. 결과는 `Latest.log`의 `[TlToybox] translations: file=… rows=… applied=… fallback=… unused=… broken=…`.
- 새 모드의 글을 번역하는 절차: 회차에서 `rloc dump` → `TlToybox\translations\untranslated.txt`(키<TAB><TAB>중국어) → 번역해 `src/ModCode/ModMain/Translations/mods.txt`와 데이터 폴더의 `mods.txt`에 합친다 → 스펙 부록 A에도 적는다.
- 개발 명령 `rloc`, `rloc apply`, `rloc dump`, `rloc key <키>`. 검사 `ui-check.ps1 -Part translation`. 회차 스크립트가 `translations` 폴더를 보관·복원한다(`TRANSLATIONS RESTORED`).

### 실측·검증 (회차 `ws-1016o`, `ws-1016p`~`q`)

- 모드 글 62행: 공법 모드 `3390738990`의 이름 5·설명 5·옵션 글 46, 외형 모드 안내문 1(`utg7MubS`), 영어 플래그 1(`0lkK4v`=`exists`), 빈 글 2. 모두 `kr`이 "0"이었다.
- (회차 `ws-1016q`의 결과를 적는다: `rloc`의 첫 적용 문장, `translation` 검사의 PASS 수, `modbooks`·`base`·`tabs` 통과, 비급 이름 "<옵션 접두>열염장", 캡처 `translation`.)

### 확인하지 못한 것, 한계

- 번역 문장은 제가 썼다. 게임 화면(기술 창·비급 설명)에서 자리 표시가 풀린 전체 문장은 사용자 확인 항목.
- `Apply()`는 `Init`마다 LocalText 55,267행을 한 번 돈다(비용 미측정. `rmods`의 같은 훑기가 3초 안쪽).
- `fallback`은 게임을 켠 뒤 첫 적용에서만 1이다(두 번째부터는 `kr`이 이미 `ch`라 0).
- 모드 자체의 이름(모드 화면)은 바꾸지 않는다.
```

- [ ] **Step 2: 인계 문서를 고친다**

- 1절 "직전 작업": "G2c 창작마당 모드 한글화(05 문서 43절): 번역 파일 59행을 DLL에 내장해 데이터 폴더에 풀고 Init마다 모드 행의 kr을 덮는다. 회차 `ws-1016p`~`q`." 앞에 두고 G2b 문장은 뒤로.
- 1절 "다음에 할 일": G2c를 지우고 "(1) 사용자가 설치본으로 플레이하며 창작마당 모드·모드 공법 비급·한글 이름을 확인한다(8.5절). (2) 열린 일: 이동 배율 x0.25의 이른 월말 보고(8.3a절). (3) 8.5절의 확인 대기와 8.6절의 사소한 것. 배포 DLL이 바뀌었으므로 사용자 확인 뒤 게임을 끈 상태에서 `tools/install.ps1`로 설치한다."
- 5절 "회차 안에서 쓰는 도구"에 `- 한글화: rloc, rloc apply, rloc dump, rloc key <키>(05 문서 43절)`. 자동 검증 표에 `translation`(한글화: 적용 결과, 키 읽기, 파일 수정·BOM·삭제, 미번역 내보내기 | 회차 안에서 파일을 바꾸므로 -End가 되돌린다). 회차 스크립트 설명의 "-End가 되돌리는 것"에 `translations` 폴더.
- 8.3b절 제목을 "G2 창작마당 모드 연동 — G2a·G2b·G2c 모두 끝남(2026-10-16)"으로 바꾸고 끝에 "G2c는 05 문서 43절. 요청 4는 끝났다." 문단.
- 8.5절에 "한글화(43절): 게임의 기술 창·비급 설명에서 모드 공법의 이름·설명이 한국어로 보이는지, 외형 모드의 안내문('용모를 바꾸시겠습니까?')이 어디서 뜨는지".
- 8.7절 회차 이름에 `ws-1016o`~`q`.

- [ ] **Step 3: 커밋**

```bash
git add docs/05-editor-v0.1.md docs/06-handoff.md
git commit -m "docs: 05 문서 43절(모드 한글화)과 인계(G2 모두 끝남, rloc·translation)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

- [ ] **Step 4: 검토자 한 명이 브랜치 전체를 본다**

읽기 전용 서브에이전트(가장 높은 모델)에게 `git -C E:\TlToybox diff develop...feat/mod-korean`과 스펙·계획·장부의 판정을 주고 본다. 지시에 "이 검토만 하고 보고 뒤 정지. 파일을 고치지 말 것. 모든 git 명령에 `-C E:\TlToybox`. 빌드·게임 금지"를 넣는다. 보는 것: `Apply()`가 기본 행을 절대 건드리지 않는지, 파일 읽기 예외가 `Init`을 깨뜨리지 않는지, `Load`의 BOM·CRLF·공백 처리, `DumpUntranslated`가 사전 키를 제대로 빼는지, 내장본이 사용자 파일을 덮지 않는지, 회차 스크립트의 보관·복원이 프리셋과 같은 안전 규칙을 지키는지, 번역표의 자리 표시가 원문과 같은지(스크립트로 `$…$`·`&…&`·`{…}`의 집합을 중국어 열과 한국어 열에서 비교). 실제 결함은 검사를 먼저 더한 뒤 고친다. "판단 보류"는 05 문서 43절의 "확인하지 못한 것"에 적는다.

- [ ] **Step 5: 병합**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check"
pwsh tools/DevRemote/session.tests.ps1
git checkout develop
git merge --no-ff feat/mod-korean -m "Merge branch 'feat/mod-korean' into develop"
git log --oneline -3
```

Expected: 두 빌드 경고 0·오류 0, 회차 시험 `FAILS=0`, 병합 커밋 하나. 설치(`tools/install.ps1`)와 작업 브랜치 삭제는 사용자에게 묻는다(게임을 끈 상태에서).
