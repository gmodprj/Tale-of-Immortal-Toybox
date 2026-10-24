# 프리셋 탭 (1) 엔진과 화면 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 편집기에 `프리셋` 탭을 더해, 외부 JSON 파일로 적은 캐릭터 빌드를 지금 캐릭터에 단계별로 또는 한 번에 적용한다(공법·심법 세트·선천운명·역천개명·법보와 기령·신혼·명우, 경지 올리기).

**Architecture:** 화면을 모르는 서비스들이 게임 값을 쓴다. `GradeService`(경지 올리기), `MartialService`의 확장(옵션을 지정해 만들기, 세트 번호), `SpriteService`·`ElderService`·`WingmanService`(직접 쓰기)가 낱개 동작이고, `PresetService`가 파일을 읽어 줄마다 검사한 뒤 이 동작들을 순서대로 부르며 넣음·건너뜀·못 넣음을 센다. 화면은 `UI/Tabs/PresetTab.cs` 하나다. 단위 시험이 없으므로 게임 안 검증 스크립트 `tools/research/preset-check.ps1`이 시험이고, 서비스는 개발 명령으로 직접 불러 화면보다 먼저 검증한다.

**Tech Stack:** C# 7.3 / .NET Framework 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4(Unhollower), uGUI. 검증은 PowerShell 7 스크립트와 `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-14-preset-tab-design.md`

이 계획은 엔진과 화면까지다. 기본 프리셋 10개의 내용, 05 문서 34절, 인계 문서, 병합과 설치는 둘째 계획(`2026-10-14-preset-tab-2-builds.md`)에서 한다. 둘째 계획은 이 계획의 실행이 끝난 뒤에 쓴다(프리셋의 번호를 대조하고 검사하는 데 이 계획이 만드는 명령 `poptpick`·`pcheck`·`papply`가 필요하다). 이 계획이 끝나면 기본 프리셋은 초안 하나(`07-창-과룡문.json`)뿐이다.

## Global Constraints

- 대상: 귀곡팔황 v1.2.113.259, 게임 폴더 `E:\Games\Tale of Immortal`, 모드 ID `sWP6UL`, 네임스페이스 `MOD_sWP6UL`.
- 참조를 추가하지 않는다. `bin\Release`에는 `MOD_sWP6UL.dll` 하나만. 기본 프리셋은 DLL 안에 넣는다(EmbeddedResource).
- 개발 구성과 배포 구성(`-p:DevTools=false`) 모두 경고 0, 오류 0. `Dev\`의 코드를 배포 코드가 참조하지 않는다.
- 대상은 항상 플레이어(`g.world.playerUnit`)다.
- 적용은 더하기다. 익힌 공법·운명을 지우지 않는다. 장착만 바꾼다.
- 쓰기 전에 `SaveBackup.EnsureOnce()`, 쓰기 뒤에 다시 읽어 판정하고 `ChangeLog.Add`. 줄 하나가 실패해도 나머지는 계속한다.
- 신혼은 대경지 7 이상, 명우는 대경지 9 이상에서만 쓴다. 법보와 기령은 대경지 2 이상의 단계에 든다.
- 이미 어느 법보에 들어 있는 기령은 옮기지 않는다.
- 수도 점수는 장착에 모자랄 때만, 모자라지 않을 만큼만 올린다. 자질과 오성은 올리지 않는다.
- 옵션은 11개까지이고, 파일을 읽을 때 자리 규칙(그 기술의 번호, 자리의 가중치, 중복 한도, 선행·배타)으로 거른다.
- 경지 올리기는 한 줄씩 `SetGradeID`를 부르며 `UnitActionRoleUpGrade.GetAddAttr`의 값과 줄의 `growAddLife`·`abilityPoint`를 더한다. `immortalPoint`는 더하지 않는다.
- 파일 위치: `%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\TlToybox\presets\<이름>.json`(UTF-8). `format` 1, `kind` `preset`.
- 탭 순서: 능력치 · 자원 · 화폐 · 보관함 · 경지 · 공법 · 운명 · 기예 · NPC · 세계 · 배율 · 프리셋 · 안전(`Tab0`~`Tab12`). 프리셋은 `Tab11`, 안전은 `Tab12`.
- 버튼 이름: `PresetRestore`, `PresetDetail<순번>`, `PresetApply<순번>`, `PresetFull<순번>`, `StageApply<대경지>`, `StageRaise<대경지>`. 순번은 파일 이름 순(대소문자 무시)의 0부터의 번호다.
- 두 번 눌러 확인: `PresetRestore`, `PresetFull*`, `StageRaise*`는 항상. `PresetApply*`·`StageApply*`는 지금의 역천개명·신혼·명우를 바꾸게 될 때만.
- 브랜치: `feat/preset-tab`(이미 있다, 스펙이 커밋돼 있다). 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. `develop`에 병합하지 않는다(둘째 계획의 끝에서 한다).
- 게임 검증은 `tools/DevRemote/session.ps1`의 회차 안에서만, `-Fixture base`로. 회차는 한 번에 하나, 한 회차는 10분 안에 끝낸다. 회차 이름은 다시 쓰지 않는다(이미 쓴 이름: `preset1`, `preset2`, `preset-1014a`~`preset-1014d`). 이 계획의 이름은 `ptab1`부터이고, 같은 단계를 다시 돌릴 때는 `ptab1b`처럼 글자를 붙인다. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 스크립트는 `pwsh`로 돌린다. `mod.ps1`·`rc.ps1`은 `&`로 부른다. 검사의 판정은 영문 표지와 숫자로 한다(한글 문구로 찾지 않는다).
- 코드 모양은 주변과 같게: 중괄호는 항상 쓰고 줄을 바꾼다, 들여쓰기 4칸, 주석은 한국어로 "왜"를 적는다. 식 본문 멤버와 문자열 보간은 쓰지 않는다.
- 고정본 `base`의 값(검사가 기대는 것): 경지 번호 9(대경지 3), 수도 점수 62 중 54 사용, 공법 28개, 장착 `left=14111 right=22111 step=510021 ult=34111`, 심법 `85001, 86001, 84001, 81001, 83001, 82001`(세트 번호 없음), 역천개명 기록 `2:700016, 3:700040`, 법보·기령·신혼·명우 없음.

## Review Focus

1. 사용자가 고친 파일의 틀린 값(없는 번호, 숫자 자리의 글자, 옵션 12개, 같은 칸 두 줄) → 그 줄만 빠지고 이유가 보이며 나머지는 적용된다. 예외 없음. (Task 5 `files`의 "thirteen bad lines", Task 6 `apply`의 "a bad line does not stop the rest")
2. 이미 역천개명을 가진 캐릭터에 적용 → 바꾸게 되는 것을 미리 알리고(두 번 확인), 다른 대경지의 기록은 그대로다. (Task 6 `apply`의 "stage 3 would replace a fate", "fate 2 is untouched", Task 7 `ui`의 "apply asks first")
3. 같은 적용을 두 번 → 공법이 늘지 않고 수도 점수도 다시 오르지 않는다. (Task 6 "applying again adds nothing")
4. 프리셋에 없는 유형의 심법이 장착된 채 남아 수도 점수를 차지 → 쓰고 있는 점수가 수도 점수를 넘지 않는다. (Task 6 "ability points cover what is in use")
5. 가방이 가득 차 법보를 줄 수 없음 → 기령을 만들지 않고 그 줄만 못 넣음이다. (Task 4 `extras`의 "a full bag fails cleanly")

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `tools/DevRemote/session.ps1` | 수정 | `presets` 폴더를 회차 전 상태로 되돌린다 |
| `tools/DevRemote/session.tests.ps1` | 수정 | 위의 시험 둘(20 → 22개) |
| `tools/research/preset-check.ps1` | 새로 | 프리셋의 게임 내 검증. 부분: `state`, `grade`, `martial`, `extras`, `extras-battle`, `files`, `apply`, `apply-full`, `apply-battle`, `apply-restart`, `ui` |
| `tools/research/ui-check.ps1` | 수정 | 탭 수와 안전 탭의 이름(`Tab11` → `Tab12`) |
| `src/ModCode/ModMain/Dev/DevCommandsPreset.cs` | 새로 | 검증용 명령 |
| `src/ModCode/ModMain/Dev/DevCommands.cs` | 수정 | 위 명령 연결 |
| `src/ModCode/ModMain/Services/GradeService.cs` | 새로 | 경지 올리기 |
| `src/ModCode/ModMain/Services/MartialService.cs` | 수정 | 옵션 검사, 옵션을 지정해 만들기, 세트 번호, 장착 칸 읽기 |
| `src/ModCode/ModMain/Services/SpriteService.cs` | 새로 | 법보와 기령 |
| `src/ModCode/ModMain/Services/ElderService.cs` | 새로 | 신혼 |
| `src/ModCode/ModMain/Services/WingmanService.cs` | 새로 | 명우 |
| `src/ModCode/ModMain/Services/PresetService.cs` | 새로 | 형식, 읽기와 줄 검사, 기본 프리셋, 단계, 상태 |
| `src/ModCode/ModMain/Services/PresetService.Apply.cs` | 새로 | 적용(단계, 지금 경지까지, 최종 단계까지) |
| `src/ModCode/ModMain/Presets/07-창-과룡문.json` | 새로 | 기본 프리셋의 초안 하나 |
| `src/ModCode/ModMain/ModMain.csproj` | 수정 | `Presets\*.json`을 EmbeddedResource로 |
| `src/ModCode/ModMain/UI/Tabs/PresetTab.cs` | 새로 | 목록과 상세보기 |
| `src/ModCode/ModMain/UI/EditorWindow.cs` | 수정 | 탭 등록, 탭 줄의 높이 |

스펙 5.4절은 `ExportFiles`를 넓힌다고 했지만 `PresetService`가 폴더(`ChangeLog.DataDir\presets`)를 직접 다루고 `ExportFiles`의 JSON 값 꺼내기(`Text`, `Whole`, `Ints`, `Items`)만 쓴다. `ExportFiles`는 고치지 않는다.

## 공통 절차

빌드(두 구성):

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release -p:DevTools=false -o "$TEMP/tltoybox-release-check"
```

Expected: 둘 다 "오류 0개", 경고 줄 없음.

회차:

```bash
pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base     # "WORLD READY"
pwsh tools/research/preset-check.ps1 -Part <부분>                # 줄마다 PASS/FAIL, 마지막 줄 FAILS=<수>
pwsh tools/DevRemote/session.ps1 -End <이름>                     # "SAVE RESTORED AND IDENTICAL"
```

검사가 이상하게 실패하면 `pwsh -Command "& tools/DevRemote/mod.ps1 -Cmd uilist"`와 캡처로 무엇이 떠 있는지부터 본다(인계 문서 6절).

---

### Task 1: 검증의 틀 — 프리셋 폴더의 보관, 상태를 읽는 개발 명령, 검사 스크립트

**Files:**
- Modify: `tools/DevRemote/session.ps1:41-43,139-141,197-203`
- Modify: `tools/DevRemote/session.tests.ps1:155-158`
- Create: `tools/research/preset-check.ps1`
- Create: `src/ModCode/ModMain/Dev/DevCommandsPreset.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs:56-62`

**Interfaces:**
- Consumes: `FateService.Records(WorldUnitBase)`(기존), `tools/research/check-lib.ps1`의 `Cmd`, `Rc`, `Check`, `Press`, `IsAt`, `Pos`, `Traps`, `Finish`, `$data`, `$work`.
- Produces:
  - 개발 명령 `pstate` — 한 줄에 `이름=값`: `grade major ap used attack attackv defense hpMax life martials left right step ult abilities artifact sprite sprites dupes talents talentsall elder elderlv wing effects prefixes fates born`. `abilities`는 `기술 번호:세트 번호`를 쉼표로(칸 순서), `talents`는 `켠 수/보이는 수`, `fates`는 `대경지:번호`를 쉼표로.
  - 개발 명령 `psoles` — `soles=<익힌 공법의 고유 번호를 정렬해 쉼표로>`.
  - 개발 명령 `pmartial <기술 번호>` — `n=<수>` 뒤에 ` | sole=… g=<경지> eq=<True|False> suit=<번호> lv=<희귀도> cost=<수도 점수> nums=<옵션 번호,…> atmax=<True|False>`.
  - 개발 명령 `pbattle` — `suits=<번호:수,…> wing=<번호> wingobjs=<수> props=<수> skills=<수>` 또는 `not in battle`.
  - `preset-check.ps1`의 함수 `Fields`, `State`, `Soles`, `Into`, `Leave`, 변수 `$presetDir`.
  - `session.ps1 -End`가 `PRESETS RESTORED`를 낸다.

- [ ] **Step 1: `session.tests.ps1`에 시험 둘을 더한다**

`# 17.` 시험의 `Check 'End restores config.txt' …` 줄 바로 아래, `foreach ($root in $boxes) {` 앞에 넣는다.

```powershell
# 18. -End는 프리셋 폴더(presets)를 회차 전의 내용으로 되돌린다. 검사가 폴더를 지우고 다시 만들어도 사용자가 고친 프리셋이 남는다
$b = NewBox
[void][IO.Directory]::CreateDirectory("$($b.data)\presets")
[IO.File]::WriteAllText("$($b.data)\presets\mine.json", 'edited by the user')
[IO.File]::WriteAllText("$($b.data)\presets\01-default.json", 'default')
[void](Run $b $none @{ Begin = 'x' })
[IO.File]::WriteAllText("$($b.data)\presets\mine.json", 'overwritten by a check')
[IO.File]::Delete("$($b.data)\presets\01-default.json")
[IO.File]::WriteAllText("$($b.data)\presets\uicheck-preset.json", 'left by a check')
$o = Run $b $none @{ End = 'x' }
Check 'End restores the presets folder' ($o -match 'PRESETS RESTORED' -and (Tree "$($b.data)\presets") -eq '\01-default.json=default;\mine.json=edited by the user' -and -not (Test-Path "$($b.work)\session-presets")) "$o | $(Tree "$($b.data)\presets")"
# 19. 회차 전에 프리셋 폴더가 없었으면 -End 뒤에도 없다(다음에 탭을 열 때 기본 프리셋이 풀린다)
$b = NewBox
[void](Run $b $none @{ Begin = 'x' })
[void][IO.Directory]::CreateDirectory("$($b.data)\presets")
[IO.File]::WriteAllText("$($b.data)\presets\uicheck-preset.json", 'left by a check')
$o = Run $b $none @{ End = 'x' }
Check 'End removes a presets folder the session made' ($o -match 'PRESETS RESTORED' -and -not (Test-Path "$($b.data)\presets") -and -not (Test-Path "$($b.work)\session-presets.absent")) $o
```

- [ ] **Step 2: 시험이 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.tests.ps1
```

Expected: 새 두 줄이 FAIL, 나머지 20줄 PASS, `FAILS=2`.

- [ ] **Step 3: `session.ps1`이 `presets` 폴더를 보관하고 되돌리게 한다**

`$keptConfig = …` 줄(42행) 아래에 세 줄을 더한다.

```powershell
$presetsDir = Join-Path $data 'presets'
$keptPresets = Join-Path $dir 'session-presets'          # 회차 전의 presets 폴더(프리셋 파일)
$noPresets = Join-Path $dir 'session-presets.absent'     # 회차 전에 presets 폴더가 없었다는 표시
```

`-Begin`의 `if (Test-Path -LiteralPath $configFile) { Copy-Item -LiteralPath $configFile -Destination $keptConfig }` 줄(140행) 아래에 넣는다.

```powershell
  # 프리셋 폴더는 세이브 밖에 있다. 검사(preset-check.ps1)가 폴더를 지우고 다시 만들므로 회차 전의 것을 떠 둔다.
  if (Test-Path -LiteralPath $keptPresets) { [IO.Directory]::Delete($keptPresets, $true) }
  DeleteFiles $noPresets
  if (Test-Path -LiteralPath $presetsDir) { Copy-Item -LiteralPath $presetsDir -Destination $keptPresets -Recurse } else { [IO.File]::WriteAllText($noPresets, '') }
```

`-End`의 `'CONFIG RESTORED'`가 든 `if` 블록 아래, `[IO.File]::Delete($marker)` 위에 넣는다.

```powershell
  # 프리셋 폴더를 회차 전으로 되돌린다. 회차 전에 없었으면 지운다.
  if (Test-Path -LiteralPath $keptPresets) {
    robocopy $keptPresets $presetsDir /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "프리셋 폴더 복원에 실패했습니다(robocopy $LASTEXITCODE). 회차 전의 것은 $keptPresets 에 있습니다." }
    [IO.Directory]::Delete($keptPresets, $true)
    'PRESETS RESTORED'
  } elseif (Test-Path $noPresets) {
    if ((Test-Path -LiteralPath $presetsDir) -and $presetsDir.StartsWith($data + '\')) { [IO.Directory]::Delete($presetsDir, $true) }
    'PRESETS RESTORED (there was no folder)'
  }
  DeleteFiles $noPresets
```

머리 주석의 `-End` 설명(6행)을 고친다: `→ bag-slots.txt와 설치돼 있던 DLL을 되돌린다`를 `→ bag-slots.txt, config.txt, presets 폴더와 설치돼 있던 DLL을 되돌린다`로.

- [ ] **Step 4: 시험이 통과하는 것을 본다**

```bash
pwsh tools/DevRemote/session.tests.ps1
```

Expected: 22줄 모두 PASS, `FAILS=0`.

- [ ] **Step 5: `preset-check.ps1`을 만든다(부분 `state`만)**

```powershell
# 프리셋 탭의 게임 내 검증(docs/superpowers/specs/2026-10-14-preset-tab-design.md 8절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다.
# 프리셋 폴더(presets)는 세이브 밖에 있지만 session.ps1 -End가 회차 전 상태로 되돌린다. 이 검사는 폴더를 마음대로 지우고 만든다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

$presetDir = Join-Path $data 'presets'

# "이름=값 이름=값 …" 한 줄을 표로 바꾼다. 명령의 머리("명령 -> ")와 " | " 뒤의 문장은 뗀다.
function Fields([string]$text) {
  $body = ($text -replace '^[^>]*-> ', '') -replace ' \| .*$', ''
  $map = @{}
  foreach ($pair in ($body -split ' ')) {
    $at = $pair.IndexOf('=')
    if ($at -gt 0) { $map[$pair.Substring(0, $at)] = $pair.Substring($at + 1) }
  }
  $map
}
function State { Fields (Cmd 'pstate' 150) }
function Soles { @((Fields (Cmd 'psoles' 150)).soles -split ',' | Where-Object { $_ }) }
# 전투에 들어간다(ui-check.ps1의 combat 부분과 같은 순서: 진입 확인 → 전투 개시).
function Into([int]$level) {
  Cmd "fight 1011 $level" 6000 | Out-Null; Rc 'click 960 684' 12000 | Out-Null; Rc 'click 956 749' 4000 | Out-Null
}
# 전투를 끝내고 지도로 나온다. 결과 창이 뜬 뒤 확인을 누르고, 지도가 돌아온 뒤 몇 초 기다린다(서두르면 화면이 빈다).
function Leave {
  Cmd 'bend' 500 | Out-Null
  for ($i = 0; $i -lt 30 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
  Start-Sleep -Seconds 2
  for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Rc 'click 959 831' 2500 | Out-Null }
  Start-Sleep -Seconds 3
}

switch ($Part) {
  'state' {
    $s = State
    Check 'the fixture is at grade 9, major grade 3' ($s.grade -eq '9' -and $s.major -eq '3') "grade=$($s.grade) major=$($s.major)"
    Check 'ability points are 62 with 54 in use' ($s.ap -eq '62' -and $s.used -eq '54') "ap=$($s.ap) used=$($s.used)"
    Check 'saved attack is 82' ($s.attack -eq '82') $s.attack
    Check 'the equipped skills are read' ($s.left -eq '14111' -and $s.right -eq '22111' -and $s.step -eq '510021' -and $s.ult -eq '34111') "$($s.left) $($s.right) $($s.step) $($s.ult)"
    Check 'six abilities without a suit' ($s.abilities -eq '85001:0,86001:0,84001:0,81001:0,83001:0,82001:0') $s.abilities
    Check 'no artifact, sprite, elder or wingman' ($s.artifact -eq '0' -and $s.sprite -eq '0' -and $s.sprites -eq '0' -and $s.elder -eq '0' -and $s.wing -eq '0')
    Check 'fate records are read' ($s.fates -eq '2:700016,3:700040') $s.fates
    Check 'there are 28 learned skills' ($s.martials -eq '28' -and (Soles).Count -eq 28) "martials=$($s.martials) soles=$((Soles).Count)"
    Check 'the equipped left skill is listed with its options' ((Cmd 'pmartial 14111' 150) -match 'n=1 \| sole=\S+ g=1 eq=True suit=0 lv=\d+ cost=0 nums=[\d,]+ atmax=(True|False)')
    Check 'not in battle' ((Cmd 'pbattle' 150) -match 'not in battle')
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
```

- [ ] **Step 6: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab1 -Fixture base
pwsh tools/research/preset-check.ps1 -Part state
pwsh tools/DevRemote/session.ps1 -End ptab1
```

Expected: `-Begin`이 "WORLD READY". `state`의 줄이 모두 FAIL(`pstate`가 `UNKNOWN`을 돌려준다). `-End`가 "SAVE RESTORED AND IDENTICAL"과 "PRESETS RESTORED (there was no folder)".

- [ ] **Step 7: `Dev/DevCommandsPreset.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 프리셋의 검증(tools/research/preset-check.ps1)이 상태를 읽고 서비스를 직접 부르는 데 쓴다.
    // 상태를 읽는 명령은 서비스를 거치지 않고 게임 값을 바로 읽는다(서비스의 판정을 서비스로 확인하지 않기 위해서다).
    internal static class DevCommandsPreset
    {
        private const int ArtifactClass = 401;
        private static Dictionary<int, bool> shownTalents;

        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            switch (p[0])
            {
                case "pstate": return State(me);
                case "psoles": return Soles(me);
                case "pmartial": return Martials(me, int.Parse(p[1]));
                case "pbattle": return Battle();
                default: return null;
            }
        }

        // 장착 칸의 고유 번호를 기술 번호로. 비었으면 0.
        private static int BaseOf(WorldUnitBase me, string sole)
        {
            if (string.IsNullOrEmpty(sole))
            {
                return 0;
            }
            try
            {
                return me.data.unitData.GetActionMartial(sole).data.To<DataProps.MartialData>().baseID;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private static int SuitOf(DataProps.PropsData props)
        {
            try
            {
                DataProps.PropsAbilityData ability = props.To<DataProps.PropsAbilityData>();
                return ability == null ? 0 : ability.suitID;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // 설정 표의 isShow가 1인 재능인가(게임 화면에 보이는 재능).
        private static bool TalentShown(int spriteId, int number)
        {
            if (shownTalents == null)
            {
                shownTalents = new Dictionary<int, bool>();
                var all = g.conf.artifactSpriteTalent._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    shownTalents[all[i].spriteID * 1000 + all[i].number] = all[i].isShow == 1;
                }
            }
            bool shown;
            return shownTalents.TryGetValue(spriteId * 1000 + number, out shown) && shown;
        }

        private static string Join(Il2CppSystem.Collections.Generic.List<int> list)
        {
            var sb = new StringBuilder();
            for (int i = 0; list != null && i < list.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(list[i]);
            }
            return sb.ToString();
        }

        private static string State(WorldUnitBase me)
        {
            var data = me.data.unitData;
            var saved = data.propertyData;
            var dyn = me.data.dynUnitData;
            ConfRoleGradeItem row = g.conf.roleGrade.GetItem(saved.gradeID);
            var sb = new StringBuilder();
            sb.Append("grade=").Append(saved.gradeID).Append(" major=").Append(row == null ? 0 : row.grade)
                .Append(" ap=").Append(saved.abilityPoint).Append(" used=").Append(dyn.GetCurUseAbilityPoint())
                .Append(" attack=").Append(saved.attack).Append(" attackv=").Append(dyn.attack.value)
                .Append(" defense=").Append(saved.defense).Append(" hpMax=").Append(saved.hpMax).Append(" life=").Append(saved.life)
                .Append(" martials=").Append(data.allActionMartial.Count)
                .Append(" left=").Append(BaseOf(me, data.skillLeft)).Append(" right=").Append(BaseOf(me, data.skillRight))
                .Append(" step=").Append(BaseOf(me, data.step)).Append(" ult=").Append(BaseOf(me, data.ultimate)).Append(" abilities=");
            bool first = true;
            for (int i = 0; data.abilitys != null && i < data.abilitys.Length; i++)
            {
                if (string.IsNullOrEmpty(data.abilitys[i]))
                {
                    continue;
                }
                DataUnit.ActionMartialData action = data.GetActionMartial(data.abilitys[i]);
                sb.Append(first ? "" : ",").Append(BaseOf(me, data.abilitys[i])).Append(':').Append(action == null ? 0 : SuitOf(action.data));
                first = false;
            }

            // 법보와 기령: 장착한 법보, 거기 든 기령, 켠 재능. dupes는 두 법보가 같은 기령을 가리키는 수(0이어야 한다).
            int artifact = 0, sprite = 0, on = 0, shown = 0, allOn = 0, allTalents = 0, dupes = 0;
            var spriteData = data.artifactSpriteData;
            int sprites = spriteData == null || spriteData.sprites == null ? 0 : spriteData.sprites.Count;
            var holders = new List<int>();
            var props = data.propData.allProps;
            for (int i = 0; i < props.Count; i++)
            {
                ConfItemPropsItem conf = g.conf.itemProps.GetItem(props[i].propsID);
                if (conf == null || conf.className != ArtifactClass)
                {
                    continue;
                }
                int inside = props[i].To<DataProps.PropsArtifact>().spriteSoleID;
                if (inside != 0)
                {
                    if (holders.Contains(inside))
                    {
                        dupes++;
                    }
                    holders.Add(inside);
                }
                if (!data.IsEquipProps(props[i].soleID))
                {
                    continue;
                }
                artifact = props[i].propsID;
                var held = inside == 0 || spriteData == null ? null : spriteData.GetSpriteInSoleID(inside);
                if (held == null)
                {
                    continue;
                }
                sprite = held.spriteID;
                for (int t = 0; held.talents != null && t < held.talents.Count; t++)
                {
                    bool lit = held.talents[t].state != 0;
                    allTalents++;
                    allOn += lit ? 1 : 0;
                    if (TalentShown(held.spriteID, held.talents[t].number))
                    {
                        shown++;
                        on += lit ? 1 : 0;
                    }
                }
            }
            sb.Append(" artifact=").Append(artifact).Append(" sprite=").Append(sprite).Append(" sprites=").Append(sprites).Append(" dupes=").Append(dupes)
                .Append(" talents=").Append(on).Append('/').Append(shown).Append(" talentsall=").Append(allOn).Append('/').Append(allTalents);

            // 신혼: 번호와 지금 단계. 대경지 7 아래에서는 단계 표를 읽는 것이 예외를 낼 수 있다.
            var elder = data.elderData;
            int elderId = elder == null ? 0 : elder.id;
            int elderLevel = 0;
            if (elderId != 0)
            {
                try
                {
                    ConfElderLevelItem item = g.conf.elderLevel.GetItem(me);
                    elderLevel = item == null ? 0 : item.level;
                }
                catch (Exception)
                {
                    elderLevel = -1;
                }
            }
            sb.Append(" elder=").Append(elderId).Append(" elderlv=").Append(elderLevel);

            var reborn = data.reborn;
            sb.Append(" wing=").Append(reborn == null ? 0 : reborn.wingManID)
                .Append(" effects=").Append(reborn == null ? "" : Join(reborn.wingmanEffectList))
                .Append(" prefixes=").Append(reborn == null ? "" : Join(reborn.wingmanPrefixList));

            var records = new List<KeyValuePair<int, int>>(FateService.Records(me));
            records.Sort((a, b) => a.Key.CompareTo(b.Key));
            sb.Append(" fates=");
            for (int i = 0; i < records.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(records[i].Key).Append(':').Append(records[i].Value);
            }
            sb.Append(" born=").Append(saved.bornLuck.Length);
            return sb.ToString();
        }

        private static string Soles(WorldUnitBase me)
        {
            var soles = new List<string>();
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                soles.Add(pair.Key);
            }
            soles.Sort(StringComparer.Ordinal);
            return "soles=" + string.Join(",", soles.ToArray());
        }

        // 그 기술 번호의 익힌 공법마다: 고유 번호, 경지, 장착, 세트 번호, 희귀도, 수도 점수 비용, 옵션 번호, 모든 옵션이 품질 상한인지.
        private static string Martials(WorldUnitBase me, int baseId)
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                try
                {
                    DataProps.PropsData props = pair.Value.data;
                    DataProps.MartialData martial = props.To<DataProps.MartialData>();
                    if (martial.baseID != baseId)
                    {
                        continue;
                    }
                    count++;
                    List<MartialService.PrefixView> views = MartialService.Prefixes(martial);
                    views.Sort((a, b) => a.Index.CompareTo(b.Index));
                    bool atMax = true;
                    var numbers = new StringBuilder();
                    for (int i = 0; i < views.Count; i++)
                    {
                        numbers.Append(i == 0 ? "" : ",").Append(views[i].Number);
                        atMax = atMax && views[i].Level >= views[i].MaxLevel;
                    }
                    sb.Append(" | sole=").Append(pair.Key).Append(" g=").Append(martial.grade).Append(" eq=").Append(MartialService.IsEquipped(me, pair.Key))
                        .Append(" suit=").Append(martial.martialType == MartialType.Ability ? SuitOf(props) : 0)
                        .Append(" lv=").Append(martial.martialInfo.level).Append(" cost=").Append(martial.martialInfo.abilityPoint)
                        .Append(" nums=").Append(numbers).Append(" atmax=").Append(atMax);
                }
                catch (Exception)
                {
                }
            }
            return "n=" + count + sb;
        }

        // 전투 중의 플레이어 단위: 켜진 세트(번호:구성원 수), 명우, 도구(법보가 여기 든다), 기술의 수.
        private static string Battle()
        {
            if (SceneType.battle == null || SceneType.battle.battleMap == null || SceneType.battle.battleMap.playerUnitCtrl == null)
            {
                return "not in battle";
            }
            UnitCtrlPlayer ctrl = SceneType.battle.battleMap.playerUnitCtrl;
            var suits = new StringBuilder();
            foreach (var pair in ctrl.activeSuits)
            {
                suits.Append(suits.Length == 0 ? "" : ",").Append(pair.Key).Append(':').Append(pair.Value);
            }
            UnitCtrlHuman human = ctrl.TryCast<UnitCtrlHuman>();
            WingManBase wing = human == null ? null : human.wingman;
            return "suits=" + suits + " wing=" + (wing == null ? 0 : wing.wingManID)
                + " wingobjs=" + (wing == null || wing.wingManList == null ? 0 : wing.wingManList.Count)
                + " props=" + (ctrl.props == null ? 0 : ctrl.props.Count) + " skills=" + (ctrl.skills == null ? 0 : ctrl.skills.Count);
        }
    }
}
```

- [ ] **Step 8: 명령을 연결한다**

`src/ModCode/ModMain/Dev/DevCommands.cs`의 `Execute`에서 `string rate = DevCommandsRate.Execute(p);` 줄 앞에 넣는다.

```csharp
            string preset = DevCommandsPreset.Execute(p);
            if (preset != null)
            {
                return preset;
            }
```

- [ ] **Step 9: 빌드하고 검사가 통과하는 것을 본다**

공통 절차의 빌드 두 줄을 돌린다. Expected: 둘 다 오류 0.

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab2 -Fixture base
pwsh tools/research/preset-check.ps1 -Part state
pwsh tools/DevRemote/session.ps1 -End ptab2
```

Expected: `state` 10줄 모두 PASS, `FAILS=0`. `-End`가 "SAVE RESTORED AND IDENTICAL".

- [ ] **Step 10: 커밋**

```bash
git add tools/DevRemote/session.ps1 tools/DevRemote/session.tests.ps1 tools/research/preset-check.ps1 src/ModCode/ModMain/Dev/DevCommandsPreset.cs src/ModCode/ModMain/Dev/DevCommands.cs
git commit -m "test: 프리셋 검증의 틀 — 프리셋 폴더 보관, 상태를 읽는 개발 명령

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 경지 올리기 — `GradeService`

**Files:**
- Create: `src/ModCode/ModMain/Services/GradeService.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPreset.cs`(명령 `pgpath`, `pgsum`, `pgraise`)
- Modify: `tools/research/preset-check.ps1`(부분 `grade`)

**Interfaces:**
- Consumes: `AttributeCatalog.All`(`AttributeDef.Key`, `Get`, `Set`, `Max`, `Label`), `EditService.GradeName(int)`, `EditService.NameOf`, `EditService.RemoveStaleBottleneck(WorldUnitBase)`, `SaveBackup.EnsureOnce()`, `ChangeLog.Add(target, field, before, after, verified)`.
- Produces:
  - `GradeService.FirstRowOf(int grade) : ConfRoleGradeItem` — 대경지의 초기 줄(품질이 여럿이면 가장 높은 것, 같으면 번호가 작은 것).
  - `GradeService.LastRow() : ConfRoleGradeItem` — 표의 마지막 줄(번호 44).
  - `GradeService.Path(int fromId, int toId) : List<ConfRoleGradeItem>` — 지나는 줄들(목표 포함). 목표가 같거나 낮으면 빈 목록.
  - `GradeService.RaiseTo(WorldUnitBase unit, int targetId) : string` — 결과 문장.
  - 개발 명령 `pgpath <번호>` → `path=10,11,…`, `pgsum <번호>` → `attack= defense= hpMax= life= ap=`(올린 뒤 기대하는 저장값), `pgraise <번호>` → `RaiseTo`의 문장.

- [ ] **Step 1: 검사를 먼저 쓴다**

`preset-check.ps1`의 `switch`에서 `'state' { … }` 아래에 넣는다.

```powershell
  'grade' {
    $s0 = State
    Check 'the path from 9 to 27 takes the best quality rows' ((Fields (Cmd 'pgpath 27' 150)).path -eq '10,11,16,17,18,19,20,21,24,25,26,27') (Cmd 'pgpath 27' 150)
    Check 'there is no path to the same or a lower row' ((Fields (Cmd 'pgpath 9' 150)).path -eq '' -and (Fields (Cmd 'pgpath 4' 150)).path -eq '')
    $want = Fields (Cmd 'pgsum 27' 150)
    $r = Cmd 'pgraise 27' 500
    $s1 = State
    Check 'the grade is 27, major grade 7' ($s1.grade -eq '27' -and $s1.major -eq '7') $r
    foreach ($k in 'attack', 'defense', 'hpMax', 'life', 'ap') {
      Check "$k grew by the breakthrough gains" ($s1[$k] -eq $want[$k]) "have $($s1[$k]) want $($want[$k])"
    }
    # 실측(회차 preset-1014b): 9에서 27까지 올리면 공격력 766, 체력 상한 17104, 수명 9600, 수도 점수 312.
    Check 'the values match the measured ones' ($s1.attack -eq '766' -and $s1.hpMax -eq '17104' -and $s1.life -eq '9600' -and $s1.ap -eq '312') "attack=$($s1.attack) hpMax=$($s1.hpMax) life=$($s1.life) ap=$($s1.ap)"
    Check 'the result gives the new attack' ($r -match '82 -> 766')
    $log = (Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 3 -Encoding UTF8) -join ' '
    Check 'the change log has the grade and the attributes' ($log -match '\(9\) -> .*\(27\)' -and $log -match ' 82, .* -> .* 766, ') $log
    $r = Cmd 'pgraise 27' 300
    Check 'raising to the same row again changes nothing' ((State).attack -eq '766' -and (State).grade -eq '27') $r
    $r = Cmd 'pgraise 9999' 300
    Check 'an unknown row is refused' ((State).grade -eq '27') $r
    Cmd 'pgraise 44' 800 | Out-Null
    $s2 = State
    Check 'the last row is reached' ($s2.grade -eq '44' -and $s2.major -eq '10') "grade=$($s2.grade)"
    Check 'attack grew again' ([int]$s2.attack -gt 766) $s2.attack
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab3 -Fixture base
pwsh tools/research/preset-check.ps1 -Part grade
pwsh tools/DevRemote/session.ps1 -End ptab3
```

Expected: `grade`의 줄이 FAIL(명령이 `UNKNOWN`). "no callback exception"만 PASS.

- [ ] **Step 3: `Services/GradeService.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 경지를 게임의 돌파 절차 없이 올린다. 한 줄씩 올라가며 게임이 돌파 때 더하는 능력치(UnitActionRoleUpGrade.GetAddAttr)를 저장값에 더한다.
    // "경지 번호를 직접 쓰지 않는다"(docs/05 3절)의 두 번째 예외다. 첫 번째는 캐릭터 가져오기이고, 그쪽은 능력치를 더하지 않는다.
    // 근거: docs/superpowers/specs/2026-10-14-preset-tab-design.md 3절(경지)과 5.3절.
    internal static class GradeService
    {
        // GetAddAttr의 키 → AttributeDef.Key. immortalPoint는 저장되는 곳을 확인하지 못해 더하지 않는다.
        private static readonly Dictionary<string, string> GrowKeys = new Dictionary<string, string>
        {
            { "atk", "attack" }, { "def", "defense" }, { "hpMax", "hpMax" }, { "mpMax", "mpMax" }, { "spMax", "spMax" },
            { "crit", "crit" }, { "critV", "critValue" }, { "guard", "guard" }, { "guardV", "guardValue" },
            { "pfr", "phycicalFree" }, { "mfr", "magicFree" }, { "msp", "moveSpeed" },
        };

        // 결과 문장과 변경 이력에 보일 능력치. 수명은 따로 다룬다(저장은 개월, 화면은 년).
        private static readonly string[] Shown = { "attack", "defense", "hpMax", "mpMax", "abilityPoint" };

        private const int MonthsPerYear = 12;
        private const int LifeMaxMonths = 8000 * MonthsPerYear;   // AttributeCatalog의 수명 상한과 같다

        // 대경지와 단계(1 초기, 2 중기, 3 후기)의 줄. 품질이 여럿이면 가장 높은 것, 같으면 번호가 작은 것. 없으면 null.
        private static ConfRoleGradeItem RowOf(int grade, int phase)
        {
            ConfRoleGradeItem best = null;
            var all = g.conf.roleGrade._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].grade == grade && all[i].phase == phase && (best == null || all[i].quality > best.quality))
                {
                    best = all[i];
                }
            }
            return best;
        }

        public static ConfRoleGradeItem FirstRowOf(int grade)
        {
            return RowOf(grade, 1);
        }

        public static ConfRoleGradeItem LastRow()
        {
            var all = g.conf.roleGrade._allConfList;
            return all.Count == 0 ? null : all[all.Count - 1];
        }

        private static int Order(ConfRoleGradeItem row)
        {
            return row.grade * 10 + row.phase;
        }

        // 지금 줄 다음부터 목표 줄까지 지나는 줄들(목표 포함). 목표가 지금과 같은 단계이거나 낮으면 빈 목록.
        public static List<ConfRoleGradeItem> Path(int fromId, int toId)
        {
            var result = new List<ConfRoleGradeItem>();
            ConfRoleGradeItem from = g.conf.roleGrade.GetItem(fromId);
            ConfRoleGradeItem to = g.conf.roleGrade.GetItem(toId);
            if (from == null || to == null || Order(to) <= Order(from))
            {
                return result;
            }
            for (int grade = from.grade; grade <= to.grade; grade++)
            {
                for (int phase = 1; phase <= 3; phase++)
                {
                    int order = grade * 10 + phase;
                    if (order <= Order(from) || order > Order(to))
                    {
                        continue;
                    }
                    ConfRoleGradeItem row = order == Order(to) ? to : RowOf(grade, phase);
                    if (row != null)
                    {
                        result.Add(row);
                    }
                }
            }
            return result;
        }

        private static AttributeDef Def(string key)
        {
            return AttributeCatalog.All.Find(d => d.Key == key);
        }

        // 저장값에 더한다. 게임의 상한을 넘으면 상한으로 쓰고 그 이름을 capped에 적는다.
        private static void Add(DataUnit.PropertyData saved, ConfRoleAttributeLimit limit, string key, int amount, List<string> capped)
        {
            AttributeDef def = Def(key);
            if (def == null || amount == 0)
            {
                return;
            }
            int want = def.Get(saved) + amount;
            int max = def.Max(limit);
            if (want > max)
            {
                want = max;
                if (!capped.Contains(def.Label))
                {
                    capped.Add(def.Label);
                }
            }
            def.Set(saved, want);
        }

        private static string Values(DataUnit.PropertyData saved)
        {
            var sb = new StringBuilder();
            foreach (string key in Shown)
            {
                AttributeDef def = Def(key);
                sb.Append(sb.Length == 0 ? "" : ", ").Append(def.Label).Append(' ').Append(def.Get(saved));
            }
            return sb.Append(", 수명 ").Append(saved.life / MonthsPerYear).Append("년").ToString();
        }

        // 목표 줄까지 올린다. 능력치는 지나는 줄마다 게임의 돌파 증가분만큼 오른다.
        public static string RaiseTo(WorldUnitBase unit, int targetId)
        {
            var saved = unit.data.unitData.propertyData;
            if (g.conf.roleGrade.GetItem(targetId) == null)
            {
                return "경지 올리기: 게임에 없는 경지 번호(" + targetId + ")입니다.";
            }
            List<ConfRoleGradeItem> path = Path(saved.gradeID, targetId);
            if (path.Count == 0)
            {
                return "경지 올리기: 이미 " + EditService.GradeName(targetId) + " 이상입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int fromId = saved.gradeID;
            var before = new Dictionary<string, int>();
            foreach (string key in Shown)
            {
                before[key] = Def(key).Get(saved);
            }
            int lifeBefore = saved.life;
            string valuesBefore = Values(saved);
            ConfRoleAttributeLimit limit = g.conf.roleAttributeLimit;
            var capped = new List<string>();
            // 이전 경지의 "난관 봉착" 기운이 남으면 수련치가 오르지 않는다(docs/05 8절). 캐릭터 가져오기의 경지와 같이 먼저 지운다.
            EditService.RemoveStaleBottleneck(unit);
            foreach (ConfRoleGradeItem row in path)
            {
                var gains = UnitActionRoleUpGrade.GetAddAttr(row);
                foreach (var pair in gains)
                {
                    if (pair.Key == "life")
                    {
                        saved.life = Math.Min(LifeMaxMonths, saved.life + pair.Value);
                        continue;
                    }
                    string key;
                    if (GrowKeys.TryGetValue(pair.Key, out key))
                    {
                        Add(saved, limit, key, pair.Value, capped);
                    }
                }
                saved.life = Math.Min(LifeMaxMonths, saved.life + row.growAddLife);
                Add(saved, limit, "abilityPoint", row.abilityPoint, capped);
                saved.SetGradeID(row.id);
            }
            bool ok = saved.gradeID == targetId;
            string name = EditService.NameOf(unit);
            ChangeLog.Add(name, "경지 올리기", EditService.GradeName(fromId) + "(" + fromId + ")", EditService.GradeName(saved.gradeID) + "(" + saved.gradeID + ")", ok);
            ChangeLog.Add(name, "경지 올리기 능력치", valuesBefore, Values(saved), ok);
            if (!ok)
            {
                return "경지 올리기: 썼지만 경지 번호가 예상과 다릅니다(" + saved.gradeID + "). 저장하지 말고 확인하세요.";
            }
            var sb = new StringBuilder("경지 올리기: " + EditService.GradeName(fromId) + " -> " + EditService.GradeName(saved.gradeID) + ".");
            foreach (string key in Shown)
            {
                AttributeDef def = Def(key);
                sb.Append(' ').Append(def.Label).Append(' ').Append(before[key]).Append(" -> ").Append(def.Get(saved)).Append(',');
            }
            sb.Append(" 수명 ").Append(lifeBefore / MonthsPerYear).Append(" -> ").Append(saved.life / MonthsPerYear).Append("년.");
            if (capped.Count > 0)
            {
                sb.Append(" 게임의 상한에 걸려 상한으로 쓴 것: ").Append(string.Join(", ", capped.ToArray())).Append('.');
            }
            return sb.Append(" 게임의 돌파 절차는 거치지 않았습니다.").ToString();
        }
    }
}
```

- [ ] **Step 4: 개발 명령을 더한다**

`Dev/DevCommandsPreset.cs`의 `switch`에서 `case "pbattle": return Battle();` 아래에 넣는다.

```csharp
                case "pgpath": return GradePath(me, int.Parse(p[1]));
                case "pgsum": return GradeSum(me, int.Parse(p[1]));
                case "pgraise": return GradeService.RaiseTo(me, int.Parse(p[1]));
```

같은 클래스에 메서드 둘을 더한다.

```csharp
        private static string GradePath(WorldUnitBase me, int toId)
        {
            var sb = new StringBuilder("path=");
            List<ConfRoleGradeItem> path = GradeService.Path(me.data.unitData.propertyData.gradeID, toId);
            for (int i = 0; i < path.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(path[i].id);
            }
            return sb.ToString();
        }

        // 그 줄까지 올린 뒤 기대하는 저장값. 서비스의 더하기를 거치지 않고 줄마다의 증가분을 따로 더한다(상한은 보지 않는다).
        private static string GradeSum(WorldUnitBase me, int toId)
        {
            var saved = me.data.unitData.propertyData;
            int attack = saved.attack, defense = saved.defense, hpMax = saved.hpMax, life = saved.life, points = saved.abilityPoint;
            foreach (ConfRoleGradeItem row in GradeService.Path(saved.gradeID, toId))
            {
                foreach (var pair in UnitActionRoleUpGrade.GetAddAttr(row))
                {
                    attack += pair.Key == "atk" ? pair.Value : 0;
                    defense += pair.Key == "def" ? pair.Value : 0;
                    hpMax += pair.Key == "hpMax" ? pair.Value : 0;
                    life += pair.Key == "life" ? pair.Value : 0;
                }
                life += row.growAddLife;
                points += row.abilityPoint;
            }
            return "attack=" + attack + " defense=" + defense + " hpMax=" + hpMax + " life=" + life + " ap=" + points;
        }
```

- [ ] **Step 5: 빌드하고 검사가 통과하는 것을 본다**

공통 절차의 빌드 두 줄. Expected: 오류 0.

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab4 -Fixture base
pwsh tools/research/preset-check.ps1 -Part grade
pwsh tools/DevRemote/session.ps1 -End ptab4
```

Expected: `grade`의 줄 모두 PASS, `FAILS=0`.

"the values match the measured ones"만 실패하면 지나는 줄(품질 선택)이 실험 때와 다른 것이다. `pgpath 27`의 출력과 `10,11,16,17,18,19,20,21,24,25,26,27`을 비교해 `RowOf`를 본다.

- [ ] **Step 6: 커밋**

```bash
git add src/ModCode/ModMain/Services/GradeService.cs src/ModCode/ModMain/Dev/DevCommandsPreset.cs tools/research/preset-check.ps1
git commit -m "feat: 경지 올리기 — 한 줄씩 올리며 게임의 돌파 증가분을 더한다

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 공법 — 옵션 검사, 옵션을 지정해 만들기, 세트 번호, 장착 칸

**Files:**
- Modify: `src/ModCode/ModMain/Services/MartialService.cs:366-423`(`AddFromValues`)과 끝에 새 메서드
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPreset.cs`(명령 `poptpick`, `pmake`, `psuit`)
- Modify: `tools/research/preset-check.ps1`(부분 `martial`)

**Interfaces:**
- Consumes: `MartialService.Prefixes`, `ValidItems`, `Weight`, `Requires`, `TopRawOfLevel`, `Martial`, `IsEquipped`(기존), `SkillCatalog.Kinds`, `SkillCatalog.Find(int kindIndex, string query)`.
- Produces(모두 `MartialService`):
  - `AddFromValues(WorldUnitBase unit, int[] tail, int exp, int expectBaseId, out string soleId) : string` — 기존 것에 고유 번호를 돌려주는 형태를 더한다. 기존 서명은 그대로 남는다.
  - `OptionNumbers(DataProps.MartialData martial) : int[]` — 옵션 번호를 자리 순서대로.
  - `OptionProblem(MartialType type, int baseId, int[] options) : string` — 쓸 수 없는 이유. 쓸 수 있으면 null.
  - `Make(WorldUnitBase unit, MartialType type, int baseId, int grade, int[] options, out string soleId) : string` — 넣었으면 null.
  - `SuitOf(DataProps.PropsData props) : int`, `SetSuit(DataProps.PropsData props, int suitId) : bool`.
  - `EquippedIn(WorldUnitBase unit, MartialType type) : string`, `AbilityType(int baseId) : int`, `EquippedAbility(WorldUnitBase unit, int abilityType) : string`.
  - 개발 명령 `poptpick <기술 번호> <수>` → `type=<수> gmin= gmax= opts=<번호,…>`, `pmake <기술 번호> <경지> <옵션 번호,…|->` → `result=ok sole=<고유 번호>` 또는 `result=fail | <이유>`, `psuit <기술 번호> <세트 번호>` → `ok=<True|False> suit=<번호>`.

- [ ] **Step 1: 검사를 먼저 쓴다**

`preset-check.ps1`의 `'grade' { … }` 아래에 넣는다.

```powershell
  'martial' {
    $n0 = [int](State).martials
    $pick = Fields (Cmd 'poptpick 13111 4' 200)
    Check 'four options are picked for 13111' (@($pick.opts -split ',').Count -eq 4 -and $pick.gmin -eq '1' -and $pick.gmax -eq '10') "opts=$($pick.opts)"
    $m = Fields (Cmd "pmake 13111 3 $($pick.opts)" 500)
    Check 'the skill is made' ($m.result -eq 'ok' -and [int](State).martials -eq $n0 + 1) "result=$($m.result)"
    $made = @((Cmd 'pmartial 13111' 200) -split ' \| ' | Where-Object { $_ -match "sole=$($m.sole) " })
    Check 'it has the options asked for, in order' ($made.Count -eq 1 -and $made[0] -match " nums=$([regex]::Escape($pick.opts)) ") ($made -join '')
    Check 'every option is at its max quality' ($made[0] -match 'atmax=True')
    Check 'it is grade 3 and not equipped' ($made[0] -match ' g=3 eq=False ')
    $m10 = Fields (Cmd "pmake 13111 10 $($pick.opts)" 500)
    Check 'a grade 10 copy is made' ($m10.result -eq 'ok' -and (Cmd 'pmartial 13111' 200) -match "sole=$($m10.sole) g=10 ")
    $m0 = Fields (Cmd 'pmake 13111 3 -' 500)
    Check 'a skill without options is made' ($m0.result -eq 'ok' -and (Cmd 'pmartial 13111' 200) -match "sole=$($m0.sole) g=3 eq=False suit=0 lv=\d+ cost=0 nums= ")
    $n1 = [int](State).martials
    Check 'an option the skill does not have is refused' ((Fields (Cmd 'pmake 13111 3 99' 500)).result -eq 'fail' -and [int](State).martials -eq $n1)
    Check 'twelve options are refused' ((Fields (Cmd 'pmake 13111 3 1,2,3,4,5,6,7,8,9,10,11,12' 500)).result -eq 'fail' -and [int](State).martials -eq $n1)
    Check 'an unknown skill is refused' ((Fields (Cmd 'pmake 999999 3 -' 500)).result -eq 'fail' -and [int](State).martials -eq $n1)
    Check 'a grade outside the range of the skill is refused' ((Fields (Cmd 'pmake 23116 1 -' 500)).result -eq 'fail' -and [int](State).martials -eq $n1)
    Check 'the suit number is written and read back' ((Cmd 'psuit 81001 10006' 200) -match 'ok=True suit=10006')
    Check 'the state shows the suit' ((State).abilities -match '81001:10006')
    Check 'the suit number is not written on a skill that is not an ability' ((Cmd 'psuit 14111 10006' 200) -match 'ok=False')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab5 -Fixture base
pwsh tools/research/preset-check.ps1 -Part martial
pwsh tools/DevRemote/session.ps1 -End ptab5
```

Expected: `martial`의 줄이 FAIL(명령이 `UNKNOWN`).

- [ ] **Step 3: `AddFromValues`가 고유 번호를 돌려주게 한다**

`MartialService.cs`의 `AddFromValues`(366~423행)를 통째로 다음으로 바꾼다. 앞의 것은 기존 호출자(`CharacterExportService`)가 쓰는 서명이다.

```csharp
        // 값 배열(머리 세 칸 뒤: 종류, 기술 번호, 경지, 옵션 번호들, -1, 옵션 품질들)로 공법을 만들어 배운 상태로 넣는다.
        // 실측: 이렇게 만든 공법은 값·옵션·품질이 원본과 같다(docs/superpowers/specs/2026-10-13-character-export-design.md 3절).
        // 실패하면 이유를, 넣었으면 null을 돌려준다. 게임이 알아보지 못해 깨진 것은 바로 지운다(읽는 것만으로 예외가 난다).
        public static string AddFromValues(WorldUnitBase unit, int[] tail, int exp, int expectBaseId)
        {
            string soleId;
            return AddFromValues(unit, tail, exp, expectBaseId, out soleId);
        }

        // 위와 같고, 넣은 공법의 고유 번호를 돌려준다. 넣지 못했으면 soleId는 null이다.
        public static string AddFromValues(WorldUnitBase unit, int[] tail, int exp, int expectBaseId, out string soleId)
        {
            soleId = null;
            if (tail == null || tail.Length < 3)
            {
                return "값이 모자랍니다";
            }
            var data = unit.data.unitData;
            int before = data.allActionMartial.Count;
            string made = null;
            string error;
            try
            {
                var values = new Il2CppStructArray<int>(tail.Length);
                for (int i = 0; i < tail.Length; i++)
                {
                    values[i] = tail[i];
                }
                DataProps.PropsData props = DataProps.PropsData.New(0, 1, DataProps.PropsDataType.Martial, values);
                made = props.soleID;
                data.StudyActionMartialProp(props);
                if (data.allActionMartial.Count != before + 1)
                {
                    error = "게임이 배우게 하지 않았습니다";
                }
                else
                {
                    DataProps.MartialData martial = data.GetActionMartial(made).data.To<DataProps.MartialData>();
                    if (martial.baseID == expectBaseId && !string.IsNullOrEmpty(martial.martialInfo.name))
                    {
                        if (exp > 0)
                        {
                            data.GetActionMartial(made).exp = exp;
                        }
                        soleId = made;
                        return null;
                    }
                    error = "게임이 알아보지 못하는 공법이라 넣지 않았습니다";
                }
            }
            catch (Exception e)
            {
                error = "게임이 받지 않았습니다(" + e.GetType().Name + ")";
            }
            // 실패한 경로는 어느 것이든, 만들던 공법이 목록에 들어가 있으면 지운다(게임이 넣은 뒤에 예외를 던졌을 수도 있다).
            try
            {
                if (made != null && data.allActionMartial.Count != before)
                {
                    data.DelActionMartial(made);
                }
            }
            catch (Exception)
            {
            }
            return data.allActionMartial.Count == before ? error : error + ". 깨진 공법이 남았을 수 있습니다. 저장하지 말고 공법 탭을 확인하세요";
        }
```

- [ ] **Step 4: 새 메서드를 더한다**

같은 파일의 `AddFromValues` 바로 아래(`Forget` 위)에 넣는다.

```csharp
        // 익힌 공법의 옵션 번호를 자리 순서대로.
        public static int[] OptionNumbers(DataProps.MartialData martial)
        {
            List<PrefixView> views = Prefixes(martial);
            views.Sort((a, b) => a.Index.CompareTo(b.Index));
            var numbers = new int[views.Count];
            for (int i = 0; i < views.Count; i++)
            {
                numbers[i] = views[i].Number;
            }
            return numbers;
        }

        // 옵션 번호 목록(자리 순서)을 그 기술에 쓸 수 없는 이유. 쓸 수 있으면 null.
        // 규칙은 옵션 교체(ReplaceBlockReason)와 같다: 그 기술의 번호, 자리의 가중치, 중복 한도, 선행(양수)·배타(음수) 조건.
        // 실측: 규칙 없이도 게임은 12개까지 만들어 준다. 설정 표에 자리의 가중치가 11번째까지만 있어 11개로 막는다.
        public static string OptionProblem(MartialType type, int baseId, int[] options)
        {
            if (options.Length > 11)
            {
                return "옵션은 11개까지입니다";
            }
            var valid = new List<ConfBattleSkillPrefixValueItem>();
            var items = g.conf.battleSkillPrefixValue.GetItems((int)type, baseId);
            for (int i = 0; items != null && i < items.Count; i++)
            {
                valid.Add(items[i]);
            }
            var numbers = new List<int>(options);
            for (int i = 0; i < options.Length; i++)
            {
                int number = options[i];
                ConfBattleSkillPrefixValueItem item = valid.Find(v => v.number == number);
                if (item == null)
                {
                    return "이 기술에 없는 옵션입니다(" + number + ")";
                }
                if (Weight(item, i) <= 0)
                {
                    return "옵션 " + (i + 1) + ": 이 자리에는 나올 수 없는 옵션";
                }
                if (numbers.FindAll(n => n == number).Count > item.multi)
                {
                    return "옵션 " + (i + 1) + ": 중복 한도 " + item.multi + "개 초과";
                }
                foreach (int require in Requires(item))
                {
                    if (require > 0 && !numbers.Contains(require))
                    {
                        return "옵션 " + number + "번에 필요한 " + require + "번 옵션이 없음";
                    }
                    if (require < 0 && numbers.Contains(-require))
                    {
                        return "옵션 " + number + "번과 " + (-require) + "번은 함께 둘 수 없음";
                    }
                }
            }
            return null;
        }

        // 옵션을 지정해 공법을 만들어 배운 상태로 넣고, 옵션마다 게임이 허용하는 최고 품질로 맞춘다.
        // 넣었으면 null과 고유 번호를, 아니면 이유를 돌려준다. 규칙을 어긴 옵션으로는 만들지 않는다.
        public static string Make(WorldUnitBase unit, MartialType type, int baseId, int grade, int[] options, out string soleId)
        {
            soleId = null;
            string problem = OptionProblem(type, baseId, options);
            if (problem != null)
            {
                return problem;
            }
            var tail = new List<int> { (int)type, baseId, grade };
            if (options.Length > 0)
            {
                tail.AddRange(options);
                tail.Add(-1);
                for (int i = 0; i < options.Length; i++)
                {
                    tail.Add(1);
                }
            }
            string made;
            string error = AddFromValues(unit, tail.ToArray(), 0, baseId, out made);
            if (error != null)
            {
                return error;
            }
            try
            {
                DataProps.MartialData martial = Martial(unit, made);
                int[] read = OptionNumbers(martial);
                bool same = martial.grade == grade && read.Length == options.Length;
                for (int i = 0; same && i < read.Length; i++)
                {
                    same = read[i] == options[i];
                }
                if (same)
                {
                    foreach (PrefixView view in Prefixes(martial))
                    {
                        int raw = TopRawOfLevel(martial, view.Index, view.MaxLevel);
                        if (raw >= 0)
                        {
                            martial.SetPrefixLevel(view.Index, raw);
                        }
                    }
                    soleId = made;
                    return null;
                }
            }
            catch (Exception)
            {
            }
            // 게임이 다르게 읽었거나 읽다가 예외가 났다. 남기지 않는다.
            try
            {
                unit.data.unitData.DelActionMartial(made);
            }
            catch (Exception)
            {
            }
            return "게임이 옵션이나 경지를 다르게 읽어 넣지 않았습니다";
        }

        // 심법 비급의 세트 번호. 세트가 없거나 심법이 아니면 0.
        public static int SuitOf(DataProps.PropsData props)
        {
            try
            {
                DataProps.PropsAbilityData ability = props.To<DataProps.PropsAbilityData>();
                return ability == null ? 0 : ability.suitID;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // 심법 비급에 세트 번호를 쓴다. 실측: 세트 번호는 값 배열이 아니라 valuesStr의 JSON {"suitID":번호}에 있고,
        // 장착한 심법의 종류가 아니라 이 번호로 세트가 켜진다(프리셋 스펙 3절). 다른 글이 적혀 있으면 건드리지 않는다.
        public static bool SetSuit(DataProps.PropsData props, int suitId)
        {
            try
            {
                if (props.To<DataProps.MartialData>().martialType != MartialType.Ability)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            string now = props.valuesStr ?? "";
            if (now.Length > 0 && !Regex.IsMatch(now, "^\\{\"suitID\":\\d+\\}$"))
            {
                return false;
            }
            props.valuesStr = "{\"suitID\":" + suitId + "}";
            return SuitOf(props) == suitId;
        }

        // 그 종류의 장착 칸(무예·절기·신법·신통)에 든 공법의 고유 번호. 비어 있으면 null.
        public static string EquippedIn(WorldUnitBase unit, MartialType type)
        {
            var data = unit.data.unitData;
            string sole = type == MartialType.SkillLeft ? data.skillLeft
                : type == MartialType.SkillRight ? data.skillRight
                : type == MartialType.Step ? data.step
                : type == MartialType.Ultimate ? data.ultimate : null;
            return string.IsNullOrEmpty(sole) ? null : sole;
        }

        // 심법의 유형(battleAbilityBase.type, 1~8). 심법이 아니면 0. 유형마다 하나만 장착할 수 있다.
        public static int AbilityType(int baseId)
        {
            ConfBattleAbilityBaseItem conf = g.conf.battleAbilityBase.GetItem(baseId);
            return conf == null ? 0 : conf.type;
        }

        // 장착한 심법 가운데 그 유형의 것. 없으면 null.
        public static string EquippedAbility(WorldUnitBase unit, int abilityType)
        {
            var data = unit.data.unitData;
            for (int i = 0; data.abilitys != null && i < data.abilitys.Length; i++)
            {
                if (string.IsNullOrEmpty(data.abilitys[i]))
                {
                    continue;
                }
                DataProps.MartialData martial = Martial(unit, data.abilitys[i]);
                if (martial != null && AbilityType(martial.baseID) == abilityType)
                {
                    return data.abilitys[i];
                }
            }
            return null;
        }
```

- [ ] **Step 5: 개발 명령을 더한다**

`Dev/DevCommandsPreset.cs`의 `switch`에 넣는다.

```csharp
                case "poptpick": return OptionPick(int.Parse(p[1]), int.Parse(p[2]));
                case "pmake": return MakeSkill(me, int.Parse(p[1]), int.Parse(p[2]), p[3]);
                case "psuit": return WriteSuit(me, int.Parse(p[1]), int.Parse(p[2]));
```

메서드를 더한다.

```csharp
        // 쉼표로 이은 번호들. "-"나 빈 글은 빈 배열.
        private static int[] Ints(string csv)
        {
            var list = new List<int>();
            foreach (string part in (csv ?? "").Split(','))
            {
                int value;
                if (int.TryParse(part, out value))
                {
                    list.Add(value);
                }
            }
            return list.ToArray();
        }

        // 그 기술 번호의 분류와 목록의 항목. 없으면 null.
        private static SkillCatalog.Entry EntryOf(int baseId, out MartialType type)
        {
            type = MartialType.SkillLeft;
            for (int kind = 0; kind < SkillCatalog.Kinds.Length; kind++)
            {
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(kind, baseId.ToString()))
                {
                    if (entry.BaseId == baseId)
                    {
                        type = SkillCatalog.Kinds[kind].Type;
                        return entry;
                    }
                }
            }
            return null;
        }

        // 그 기술에 쓸 수 있는 옵션을 자리마다 하나씩 골라 count개. 검사가 프리셋 파일을 만들 때 쓴다.
        private static string OptionPick(int baseId, int count)
        {
            MartialType type;
            SkillCatalog.Entry entry = EntryOf(baseId, out type);
            if (entry == null)
            {
                return "no skill " + baseId;
            }
            var picked = new List<int>();
            var items = g.conf.battleSkillPrefixValue.GetItems((int)type, baseId);
            for (int slot = 0; slot < count; slot++)
            {
                for (int i = 0; items != null && i < items.Count; i++)
                {
                    var candidate = new List<int>(picked);
                    candidate.Add(items[i].number);
                    if (MartialService.OptionProblem(type, baseId, candidate.ToArray()) == null)
                    {
                        picked = candidate;
                        break;
                    }
                }
            }
            var sb = new StringBuilder();
            for (int i = 0; i < picked.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(picked[i]);
            }
            return "type=" + (int)type + " gmin=" + entry.GradeMin + " gmax=" + entry.GradeMax + " opts=" + sb;
        }

        private static string MakeSkill(WorldUnitBase me, int baseId, int grade, string options)
        {
            MartialType type;
            SkillCatalog.Entry entry = EntryOf(baseId, out type);
            if (entry == null)
            {
                return "result=fail | no skill " + baseId;
            }
            if (grade < entry.GradeMin || grade > entry.GradeMax)
            {
                return "result=fail | grade out of range " + entry.GradeMin + "-" + entry.GradeMax;
            }
            string sole;
            string error = MartialService.Make(me, type, baseId, grade, Ints(options), out sole);
            return error == null ? "result=ok sole=" + sole : "result=fail | " + error;
        }

        private static string WriteSuit(WorldUnitBase me, int baseId, int suitId)
        {
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                try
                {
                    if (pair.Value.data.To<DataProps.MartialData>().baseID == baseId)
                    {
                        bool ok = MartialService.SetSuit(pair.Value.data, suitId);
                        return "ok=" + ok + " suit=" + MartialService.SuitOf(pair.Value.data);
                    }
                }
                catch (Exception)
                {
                }
            }
            return "not learned " + baseId;
        }
```

- [ ] **Step 6: 빌드하고 검사가 통과하는 것을 본다**

공통 절차의 빌드 두 줄. Expected: 오류 0.

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab6 -Fixture base
pwsh tools/research/preset-check.ps1 -Part martial
pwsh tools/research/ui-check.ps1 -Part charvault
pwsh tools/DevRemote/session.ps1 -End ptab6
```

Expected: `martial` 모두 PASS. `charvault`는 전과 같이 PASS(`AddFromValues`의 기존 서명이 그대로 동작한다), 두 번 모두 `FAILS=0`.

"four options are picked"가 실패하고 `opts`가 넷보다 적으면 그 기술의 자리 규칙이 넷을 허용하지 않는 것이다. 수를 줄여(예: 3) 검사를 맞추고, 무엇이 막았는지 05 문서에 적을 것으로 기록해 둔다.

- [ ] **Step 7: 커밋**

```bash
git add src/ModCode/ModMain/Services/MartialService.cs src/ModCode/ModMain/Dev/DevCommandsPreset.cs tools/research/preset-check.ps1
git commit -m "feat: 옵션을 지정해 공법 만들기, 옵션 규칙 검사, 심법의 세트 번호

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: 법보와 기령, 신혼, 명우 — 직접 쓰는 서비스 셋

**Files:**
- Create: `src/ModCode/ModMain/Services/SpriteService.cs`
- Create: `src/ModCode/ModMain/Services/ElderService.cs`
- Create: `src/ModCode/ModMain/Services/WingmanService.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPreset.cs`(명령 `psprite`, `pelder`, `pwing`)
- Modify: `tools/research/preset-check.ps1`(부분 `extras`, `extras-battle`)

**Interfaces:**
- Consumes: `FateService.CurrentGrade(WorldUnitBase) : int`(대경지), `EditService.ItemName(int) : string`, `EditService.Compact(string)`, `EditService.NameOf`, `SaveBackup.EnsureOnce()`, `ChangeLog.Add`, Task 2의 `pgraise`.
- Produces:
  - `SpriteService.FirstGrade`(2), `Known(int spriteId) : bool`, `IsArtifact(int itemId) : bool`, `SpriteName(int) : string`, `ArtifactName(int) : string`, `Has(WorldUnitBase, int spriteId, int artifactId) : bool`, `Apply(WorldUnitBase, int spriteId, int artifactId, out bool changed) : string`.
  - `ElderService.FirstGrade`(7), `Known(int) : bool`, `Name(int) : string`, `Current(WorldUnitBase) : int`, `Level(WorldUnitBase) : int`, `Has(WorldUnitBase, int id) : bool`, `Set(WorldUnitBase, int id) : string`.
  - `WingmanService.FirstGrade`(9), `Known(int) : bool`, `Name(int) : string`, `EffectKnown(int) : bool`, `PrefixOf(int prefixId, int wingmanId) : bool`, `Current(WorldUnitBase) : int`, `Has(WorldUnitBase, int id, int[] effects, int[] prefixes) : bool`, `Set(WorldUnitBase, int id, int[] effects, int[] prefixes) : string`.
  - 개발 명령 `psprite <기령> <법보>` → `changed=<True|False> | <문장>`, `pelder <신혼>`, `pwing <명우> <효과,…|-> <옵션,…|->` → 서비스의 문장.

- [ ] **Step 1: 검사를 먼저 쓴다**

`preset-check.ps1`의 `'martial' { … }` 아래에 넣는다.

```powershell
  'extras' {
    # 결정 경지(대경지 3): 신혼과 명우는 쓰지 않는다. 결정에서 신혼을 쓰면 게임이 예외를 낸다(스펙 3절).
    $r = Cmd 'pelder 1' 300
    Check 'the elder is not written below major grade 7' ((State).elder -eq '0') $r
    $r = Cmd 'pwing 1004 100101,100102 100401,100402' 300
    Check 'the wingman is not written below major grade 9' ((State).wing -eq '0') $r

    $r = Cmd 'psprite 1002 3031101' 800
    $s = State
    Check 'artifact 3031101 is equipped and holds sprite 1002' ($s.artifact -eq '3031101' -and $s.sprite -eq '1002' -and $s.sprites -eq '1') $r
    Check 'only the shown talents are on' ($s.talents -eq '49/49' -and $s.talentsall -eq '49/53') "talents=$($s.talents) all=$($s.talentsall)"
    Check 'one artifact was given' ((Cmd 'count 3031101' 150) -match 'count=1\b')
    $r = Cmd 'psprite 1002 3031101' 800
    $s = State
    Check 'applying it again changes nothing' ($r -match 'changed=False' -and $s.sprites -eq '1' -and $s.talents -eq '49/49' -and (Cmd 'count 3031101' 150) -match 'count=1\b') $r
    # 같은 번호의 기령을 다른 법보에: 이미 법보에 든 기령을 옮기면 두 법보가 같은 기령을 가리킨다(실측). 새 기령을 만들어 넣어야 한다.
    $r = Cmd 'psprite 1002 3031102' 800
    $s = State
    Check 'another artifact gets a sprite of its own' ($s.artifact -eq '3031102' -and $s.sprite -eq '1002' -and $s.sprites -eq '2' -and $s.dupes -eq '0') $r
    Cmd 'psprite 9999 3031102' 300 | Out-Null
    Cmd 'psprite 1002 10001' 300 | Out-Null
    Check 'an unknown sprite and a non-artifact item are refused' ((State).sprites -eq '2' -and (State).artifact -eq '3031102')

    Cmd 'pgraise 27' 500 | Out-Null
    $a0 = [int](State).attackv
    $r = Cmd 'pelder 1' 500
    $s = State
    Check 'elder 1 is at level 3, the top for major grade 7' ($s.elder -eq '1' -and $s.elderlv -eq '3') $r
    Check 'attack rose with the elder' ([int]$s.attackv -gt $a0) "$a0 -> $($s.attackv)"
    $r = Cmd 'pelder 2' 500
    Check 'the elder is switched and keeps its level' ((State).elder -eq '2' -and (State).elderlv -eq '3') $r
    $r = Cmd 'pelder 99' 300
    Check 'an unknown elder is refused' ((State).elder -eq '2') $r
    $r = Cmd 'pwing 1004 100101 100401' 300
    Check 'the wingman is still not written at major grade 7' ((State).wing -eq '0') $r

    Cmd 'pgraise 36' 500 | Out-Null
    $r = Cmd 'pelder 2' 500
    Check 'the elder grows to level 7 at major grade 9' ((State).elderlv -eq '7') $r
    $r = Cmd 'pwing 1004 100101,100102 100401,100402' 500
    $s = State
    Check 'the wingman and its lists are written' ($s.wing -eq '1004' -and $s.effects -eq '100101,100102' -and $s.prefixes -eq '100401,100402') $r
    $r = Cmd 'pwing 1004 100101 100101' 300      # 100101은 명우 1001의 옵션이다
    Check 'an option of another wingman is refused' ((State).prefixes -eq '100401,100402') $r
    $r = Cmd 'pwing 9999 - -' 300
    Check 'an unknown wingman is refused' ((State).wing -eq '1004') $r

    # 가방이 가득 차면 법보를 줄 수 없다. 기령을 만들지 않고 끝나야 한다. 가방을 채우므로 이 부분의 마지막에 둔다.
    Cmd 'gridfill 600 5' 4000 | Out-Null
    $r = Cmd 'psprite 1003 3031103' 800
    $s = State
    Check 'a full bag fails cleanly' ($s.sprites -eq '2' -and $s.artifact -eq '3031102' -and (Cmd 'count 3031103' 150) -match 'count=0\b') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'extras-battle' {
    # extras 바로 뒤에 돌린다. 명우가 전투에 나오는지 본다. 법보는 도구 칸의 아이콘이라 캡처로 본다.
    Into 20
    Start-Sleep -Seconds 2
    $o = Cmd 'pbattle' 200
    $b = Fields $o
    Check 'the battle is entered' ($b.ContainsKey('wing')) $o
    Check 'the wingman is in the battle' ($b.wing -eq '1004' -and [int]$b.wingobjs -ge 1) $o
    Shot 'preset-extras-battle'
    Leave
    Check 'back on the map' ((Cmd 'uilist' 200) -match 'MapMain')
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab7 -Fixture base
pwsh tools/research/preset-check.ps1 -Part extras
pwsh tools/DevRemote/session.ps1 -End ptab7
```

Expected: `psprite`·`pelder`·`pwing`이 `UNKNOWN`이라, 아무것도 바뀌지 않는 것을 보는 줄("is not written", "is refused", "a full bag fails cleanly")만 PASS이고 나머지는 FAIL.

- [ ] **Step 3: `Services/SpriteService.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 법보와 기령. 게임의 획득 경로가 아니라 데이터에 직접 넣는다. 화면을 모르고 결과를 문장으로 돌려준다.
    // 실측(프리셋 스펙 3절): AddSprite로 기령을 만들고, 법보 아이템을 지급해 UnitActionPropEquip으로 장착하고,
    // UnitActionEquipSprite로 법보에 넣는다. 재능은 ActiveTanlet으로 켠다.
    internal static class SpriteService
    {
        public const int FirstGrade = 2;        // 프리셋이 법보와 기령을 넣기 시작하는 대경지(법보 3031101의 최저 경지가 축기다)
        private const int ArtifactClass = 401;  // ItemProps.className: 법보
        private const int ArtifactSlot = 2;     // UnitActionPropEquip의 칸 번호. 실측에서 쓴 값이다

        private static Dictionary<int, bool> shown;   // 기령 번호 * 1000 + 재능 번호 → 게임 화면에 보이는 재능인가

        public static bool Known(int spriteId)
        {
            return g.conf.artifactSprite.GetItem(spriteId) != null;
        }

        public static bool IsArtifact(int itemId)
        {
            ConfItemPropsItem conf = g.conf.itemProps.GetItem(itemId);
            return conf != null && conf.className == ArtifactClass;
        }

        public static string SpriteName(int spriteId)
        {
            ConfArtifactSpriteItem conf = g.conf.artifactSprite.GetItem(spriteId);
            return conf == null ? spriteId.ToString() : EditService.Compact(GameTool.LS(conf.name));
        }

        public static string ArtifactName(int itemId)
        {
            return EditService.ItemName(itemId) ?? itemId.ToString();
        }

        // 설정 표의 isShow가 1인 재능만 게임 화면에 보인다(기령 1002는 53개 가운데 49개). 보이지 않는 것까지 켜면 화면에 "53/49"로 넘친다(실측).
        private static bool Shown(int spriteId, int number)
        {
            if (shown == null)
            {
                shown = new Dictionary<int, bool>();
                var all = g.conf.artifactSpriteTalent._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    shown[all[i].spriteID * 1000 + all[i].number] = all[i].isShow == 1;
                }
            }
            bool value;
            return shown.TryGetValue(spriteId * 1000 + number, out value) && value;
        }

        // 가진 법보들. itemId가 0이면 전부, 아니면 그 번호의 것만.
        private static List<DataProps.PropsData> Artifacts(WorldUnitBase unit, int itemId)
        {
            var result = new List<DataProps.PropsData>();
            var all = unit.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (itemId == 0 ? IsArtifact(all[i].propsID) : all[i].propsID == itemId)
                {
                    result.Add(all[i]);
                }
            }
            return result;
        }

        private static int SpriteSoleIn(DataProps.PropsData artifact)
        {
            return artifact.To<DataProps.PropsArtifact>().spriteSoleID;
        }

        private static DataUnit.ArtifactSpriteData.Sprite SpriteIn(WorldUnitBase unit, DataProps.PropsData artifact)
        {
            int sole = SpriteSoleIn(artifact);
            var sprites = unit.data.unitData.artifactSpriteData;
            return sole == 0 || sprites == null ? null : sprites.GetSpriteInSoleID(sole);
        }

        // 꺼져 있는 보이는 재능의 번호들.
        private static List<int> DarkTalents(DataUnit.ArtifactSpriteData.Sprite sprite)
        {
            var numbers = new List<int>();
            for (int i = 0; sprite.talents != null && i < sprite.talents.Count; i++)
            {
                if (sprite.talents[i].state == 0 && Shown(sprite.spriteID, sprite.talents[i].number))
                {
                    numbers.Add(sprite.talents[i].number);
                }
            }
            return numbers;
        }

        // 장착한 법보가 그 번호이고, 거기에 그 번호의 기령이 들어 있고, 보이는 재능이 모두 켜져 있는가.
        public static bool Has(WorldUnitBase unit, int spriteId, int artifactId)
        {
            foreach (DataProps.PropsData artifact in Artifacts(unit, artifactId))
            {
                if (!unit.data.unitData.IsEquipProps(artifact.soleID))
                {
                    continue;
                }
                DataUnit.ArtifactSpriteData.Sprite sprite = SpriteIn(unit, artifact);
                return sprite != null && sprite.spriteID == spriteId && DarkTalents(sprite).Count == 0;
            }
            return false;
        }

        // 그 번호의 법보에 그 번호의 기령을 넣어 장착하고 보이는 재능을 켠다. changed는 무엇이든 바꿨는지다.
        public static string Apply(WorldUnitBase unit, int spriteId, int artifactId, out bool changed)
        {
            changed = false;
            if (!Known(spriteId))
            {
                return "기령 " + spriteId + ": 게임에 없는 번호입니다.";
            }
            if (!IsArtifact(artifactId))
            {
                return "법보 " + artifactId + ": 게임의 법보가 아닙니다.";
            }
            string label = ArtifactName(artifactId) + " + " + SpriteName(spriteId);
            var data = unit.data.unitData;
            var sprites = data.artifactSpriteData;
            if (sprites == null)
            {
                return "법보와 기령: 이 인물에게 기령 자료가 없습니다.";
            }
            if (Has(unit, spriteId, artifactId))
            {
                return "법보와 기령: 이미 " + label + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string name = EditService.NameOf(unit);

            // 1) 그 번호의 기령이 이미 들어 있는 그 번호의 법보가 있으면 그것을 쓴다.
            DataProps.PropsData target = null;
            DataUnit.ArtifactSpriteData.Sprite sprite = null;
            foreach (DataProps.PropsData each in Artifacts(unit, artifactId))
            {
                DataUnit.ArtifactSpriteData.Sprite inside = SpriteIn(unit, each);
                if (inside != null && inside.spriteID == spriteId)
                {
                    target = each;
                    sprite = inside;
                    break;
                }
            }
            if (target == null)
            {
                // 2) 기령이 비어 있는 그 번호의 법보. 없으면 지급한다.
                var had = new List<string>();
                foreach (DataProps.PropsData each in Artifacts(unit, artifactId))
                {
                    had.Add(each.soleID);
                    if (target == null && SpriteSoleIn(each) == 0)
                    {
                        target = each;
                    }
                }
                if (target == null)
                {
                    unit.data.RewardPropItem(artifactId, 1, false);
                    foreach (DataProps.PropsData each in Artifacts(unit, artifactId))
                    {
                        if (!had.Contains(each.soleID))
                        {
                            target = each;
                        }
                    }
                    if (target == null)
                    {
                        ChangeLog.Add(name, "법보와 기령", "-", label + " 지급 실패", false);
                        return "법보와 기령 " + label + ": 게임이 법보를 주지 않았습니다(가방이 가득 찼을 수 있습니다). 기령은 만들지 않았습니다.";
                    }
                    changed = true;
                }
                // 3) 어느 법보에도 들어 있지 않은 그 번호의 기령. 없으면 만든다.
                //    이미 법보에 든 기령을 다른 법보에 넣으면 두 법보가 같은 기령을 가리킨다(실측). 옮기지 않는다.
                var used = new List<int>();
                foreach (DataProps.PropsData each in Artifacts(unit, 0))
                {
                    int sole = SpriteSoleIn(each);
                    if (sole != 0)
                    {
                        used.Add(sole);
                    }
                }
                for (int i = 0; sprites.sprites != null && i < sprites.sprites.Count; i++)
                {
                    if (sprites.sprites[i].spriteID == spriteId && !used.Contains(sprites.sprites[i].soleID))
                    {
                        sprite = sprites.sprites[i];
                        break;
                    }
                }
                if (sprite == null)
                {
                    sprite = sprites.AddSprite(spriteId);
                }
                if (sprite == null)
                {
                    ChangeLog.Add(name, "법보와 기령", "-", label + " 기령 만들기 실패", false);
                    return "법보와 기령 " + label + ": 게임이 기령을 만들지 않았습니다.";
                }
                changed = true;
                unit.CreateAction(new UnitActionEquipSprite(target.To<DataProps.PropsArtifact>(), sprite.soleID), false);
                if (SpriteSoleIn(target) != sprite.soleID)
                {
                    ChangeLog.Add(name, "법보와 기령", "-", label + " 넣기 실패", false);
                    return "법보와 기령 " + label + ": 게임이 기령을 법보에 넣지 않았습니다. 저장하지 말고 확인하세요.";
                }
            }
            if (!data.IsEquipProps(target.soleID))
            {
                // 다른 법보가 장착돼 있으면 게임이 바꿔 준다(실측). IsCreateAction은 3을 주지만 CreateAction은 장착한다.
                unit.CreateAction(new UnitActionPropEquip(target.soleID, ArtifactSlot), false);
                changed = true;
                if (!data.IsEquipProps(target.soleID))
                {
                    ChangeLog.Add(name, "법보와 기령", "-", label + " 장착 실패", false);
                    return "법보와 기령 " + label + ": 게임이 법보를 장착하지 않았습니다.";
                }
            }
            int lit = 0;
            foreach (int number in DarkTalents(sprite))
            {
                sprites.ActiveTanlet(sprite.soleID, number);
                lit++;
                changed = true;
            }
            try
            {
                unit.UpdateConf();
            }
            catch (Exception)
            {
            }
            bool ok = Has(unit, spriteId, artifactId);
            ChangeLog.Add(name, "법보와 기령", "-", label + (lit > 0 ? ", 재능 " + lit + "개 켬" : ""), ok);
            return ok ? "법보와 기령: " + label + "(재능 " + lit + "개 켬)" : "법보와 기령 " + label + ": 썼지만 상태가 예상과 다릅니다. 저장하지 말고 확인하세요.";
        }
    }
}
```

- [ ] **Step 4: `Services/ElderService.cs`를 만든다**

```csharp
using System;

namespace MOD_sWP6UL
{
    // 신혼. 게임의 정식 행동(UnitActionRoleUpGradeElder)은 쓸 수 없어(예외) 데이터에 직접 쓴다(프리셋 스펙 3절).
    internal static class ElderService
    {
        // 단계 표(elderLevel)의 grade가 7부터다. 그 아래에서 id를 쓰고 UpdateConf를 부르면 예외가 난다(실측: 결정 경지).
        public const int FirstGrade = 7;

        public static bool Known(int id)
        {
            return g.conf.elderBase.GetItem(id) != null;
        }

        public static string Name(int id)
        {
            ConfElderBaseItem conf = g.conf.elderBase.GetItem(id);
            return conf == null ? id.ToString() : EditService.Compact(GameTool.LS(conf.name));
        }

        public static int Current(WorldUnitBase unit)
        {
            var elder = unit.data.unitData.elderData;
            return elder == null ? 0 : elder.id;
        }

        // 지금 신혼의 단계. 신혼이 없거나 읽을 수 없으면 0.
        public static int Level(WorldUnitBase unit)
        {
            if (Current(unit) == 0)
            {
                return 0;
            }
            try
            {
                ConfElderLevelItem item = g.conf.elderLevel.GetItem(unit);
                return item == null ? 0 : item.level;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // 그 대경지가 허용하는 가장 높은 단계의 줄. 없으면 null.
        private static ConfElderLevelItem TopRow(int id, int grade)
        {
            ConfElderLevelItem best = null;
            var rows = g.conf.elderLevel.GetItemInElderID(id);
            for (int i = 0; rows != null && i < rows.Count; i++)
            {
                if (rows[i].grade <= grade && (best == null || rows[i].level > best.level))
                {
                    best = rows[i];
                }
            }
            return best;
        }

        // 그 신혼이고, 지금 대경지가 허용하는 최고 단계인가.
        public static bool Has(WorldUnitBase unit, int id)
        {
            ConfElderLevelItem top = TopRow(id, FateService.CurrentGrade(unit));
            return Current(unit) == id && top != null && Level(unit) >= top.level;
        }

        // 신혼을 그 번호로 정하고, 지금 대경지가 허용하는 최고 단계까지 경험치를 올린다. 지금 신혼이 다른 것이면 바꾼다.
        public static string Set(WorldUnitBase unit, int id)
        {
            if (!Known(id))
            {
                return "신혼 " + id + ": 게임에 없는 번호입니다.";
            }
            string label = Name(id);
            int grade = FateService.CurrentGrade(unit);
            if (grade < FirstGrade)
            {
                return "신혼 " + label + ": 화신(대경지 7)부터 쓸 수 있습니다. 쓰지 않았습니다.";
            }
            var elder = unit.data.unitData.elderData;
            if (elder == null)
            {
                return "신혼: 이 인물에게 신혼 자료가 없습니다.";
            }
            if (Has(unit, id))
            {
                return "신혼: 이미 " + label + " " + Level(unit) + "단계입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int oldId = elder.id;
            string before = oldId == 0 ? "없음" : Name(oldId) + " " + Level(unit) + "단계";
            elder.id = id;
            if (elder.elderID == null)
            {
                elder.elderID = new Il2CppSystem.Collections.Generic.List<int>();
            }
            if (!elder.elderID.Contains(id))
            {
                elder.elderID.Add(id);
            }
            // 단계의 exp는 그 단계에 이르는 누적 값이다(실측: 40000에서 2단계, 대경지 9에서 480000이면 7단계).
            ConfElderLevelItem top = TopRow(id, grade);
            if (top != null && elder.exp < top.exp)
            {
                elder.AddExp(top.exp - elder.exp);
            }
            bool updated = true;
            try
            {
                unit.UpdateConf();
            }
            catch (Exception)
            {
                updated = false;
            }
            string name = EditService.NameOf(unit);
            if (!updated)
            {
                // 게임이 받지 않았다. 번호를 되돌리면 복구된다(실측).
                elder.id = oldId;
                try
                {
                    unit.UpdateConf();
                }
                catch (Exception)
                {
                }
                ChangeLog.Add(name, "신혼", before, before, false);
                return "신혼 " + label + ": 게임이 받지 않아 되돌렸습니다.";
            }
            bool ok = Has(unit, id);
            ChangeLog.Add(name, "신혼", before, label + " " + Level(unit) + "단계", ok);
            return ok ? "신혼: " + before + " -> " + label + " " + Level(unit) + "단계"
                : "신혼 " + label + ": 썼지만 단계가 예상과 다릅니다(" + Level(unit) + "단계). 저장하지 말고 확인하세요.";
        }
    }
}
```

- [ ] **Step 5: `Services/WingmanService.cs`를 만든다**

```csharp
using System;

namespace MOD_sWP6UL
{
    // 명우. 게임은 특수 전투에서 고른 명우·효과·옵션을 unitData.reborn에 옮긴다. 편집기는 그 값을 직접 쓴다(프리셋 스펙 3절).
    internal static class WingmanService
    {
        // 게임에서 명우는 우화(대경지 9)에서 얻는다. 낮은 경지에서 써도 전투에는 나오지만(실측) 게임의 진행을 따른다.
        public const int FirstGrade = 9;

        public static bool Known(int id)
        {
            return g.conf.wingmanBase.GetItem(id) != null;
        }

        public static string Name(int id)
        {
            ConfWingmanBaseItem conf = g.conf.wingmanBase.GetItem(id);
            return conf == null ? id.ToString() : EditService.Compact(GameTool.LS(conf.name));
        }

        public static bool EffectKnown(int effectId)
        {
            return g.conf.wingmanEffect.GetItem(effectId) != null;
        }

        // 그 옵션이 그 명우의 것인가.
        public static bool PrefixOf(int prefixId, int wingmanId)
        {
            ConfWingmanFixValueItem conf = g.conf.wingmanFixValue.GetItem(prefixId);
            return conf != null && conf.wingManID == wingmanId;
        }

        public static int Current(WorldUnitBase unit)
        {
            var reborn = unit.data.unitData.reborn;
            return reborn == null ? 0 : reborn.wingManID;
        }

        private static bool Same(Il2CppSystem.Collections.Generic.List<int> have, int[] want)
        {
            int count = have == null ? 0 : have.Count;
            if (count != want.Length)
            {
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                if (have[i] != want[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static Il2CppSystem.Collections.Generic.List<int> ToList(int[] values)
        {
            var list = new Il2CppSystem.Collections.Generic.List<int>();
            foreach (int value in values)
            {
                list.Add(value);
            }
            return list;
        }

        public static bool Has(WorldUnitBase unit, int id, int[] effects, int[] prefixes)
        {
            var reborn = unit.data.unitData.reborn;
            return reborn != null && reborn.wingManID == id && Same(reborn.wingmanEffectList, effects) && Same(reborn.wingmanPrefixList, prefixes);
        }

        // 명우와 그 효과·옵션 목록을 쓴다. 지금 명우가 다른 것이면 바꾼다.
        public static string Set(WorldUnitBase unit, int id, int[] effects, int[] prefixes)
        {
            if (!Known(id))
            {
                return "명우 " + id + ": 게임에 없는 번호입니다.";
            }
            string label = Name(id);
            foreach (int effect in effects)
            {
                if (!EffectKnown(effect))
                {
                    return "명우 " + label + ": 게임에 없는 효과 번호입니다(" + effect + ").";
                }
            }
            foreach (int prefix in prefixes)
            {
                if (!PrefixOf(prefix, id))
                {
                    return "명우 " + label + ": 이 명우의 옵션이 아닙니다(" + prefix + ").";
                }
            }
            if (FateService.CurrentGrade(unit) < FirstGrade)
            {
                return "명우 " + label + ": 우화(대경지 9)부터 쓸 수 있습니다. 쓰지 않았습니다.";
            }
            var reborn = unit.data.unitData.reborn;
            if (reborn == null)
            {
                return "명우: 이 인물에게 명우 자료가 없습니다.";
            }
            if (Has(unit, id, effects, prefixes))
            {
                return "명우: 이미 " + label + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = reborn.wingManID == 0 ? "없음" : Name(reborn.wingManID);
            reborn.wingManID = id;
            reborn.isPassRebornBattle = true;
            // 명우를 얻은 적이 없는 인물은 두 목록이 null이다(실측). 새 목록을 넣는다.
            reborn.wingmanEffectList = ToList(effects);
            reborn.wingmanPrefixList = ToList(prefixes);
            if (reborn.wingmanPrefixUp == null)
            {
                reborn.wingmanPrefixUp = new Il2CppSystem.Collections.Generic.List<int>();
            }
            try
            {
                unit.UpdateConf();
            }
            catch (Exception)
            {
            }
            bool ok = Has(unit, id, effects, prefixes);
            ChangeLog.Add(EditService.NameOf(unit), "명우", before, label + "(효과 " + effects.Length + ", 옵션 " + prefixes.Length + ")", ok);
            return ok ? "명우: " + before + " -> " + label + "(효과 " + effects.Length + ", 옵션 " + prefixes.Length + ")"
                : "명우 " + label + ": 썼지만 상태가 예상과 다릅니다. 저장하지 말고 확인하세요.";
        }
    }
}
```

- [ ] **Step 6: 개발 명령을 더한다**

`Dev/DevCommandsPreset.cs`의 `switch`에 넣는다.

```csharp
                case "psprite":
                    {
                        bool changed;
                        string result = SpriteService.Apply(me, int.Parse(p[1]), int.Parse(p[2]), out changed);
                        return "changed=" + changed + " | " + result;
                    }
                case "pelder": return ElderService.Set(me, int.Parse(p[1]));
                case "pwing": return WingmanService.Set(me, int.Parse(p[1]), Ints(p[2]), Ints(p[3]));
```

- [ ] **Step 7: 빌드하고 검사가 통과하는 것을 본다**

공통 절차의 빌드 두 줄. Expected: 오류 0.

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab8 -Fixture base
pwsh tools/research/preset-check.ps1 -Part extras
pwsh tools/research/preset-check.ps1 -Part extras-battle
pwsh tools/DevRemote/session.ps1 -End ptab8
```

Expected: `extras`와 `extras-battle` 모두 PASS, 두 번 모두 `FAILS=0`. `extras-battle`이 낸 `SHOT` 파일(`.devremote\check-preset-extras-battle.png`)을 열어 전투 화면 아래 도구 칸에 법보 아이콘이 있는지 본다. 없으면 완료로 치지 않고 원인을 찾는다.

- [ ] **Step 8: 커밋**

```bash
git add src/ModCode/ModMain/Services/SpriteService.cs src/ModCode/ModMain/Services/ElderService.cs src/ModCode/ModMain/Services/WingmanService.cs src/ModCode/ModMain/Dev/DevCommandsPreset.cs tools/research/preset-check.ps1
git commit -m "feat: 법보와 기령, 신혼, 명우를 직접 쓰는 서비스

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: 프리셋 파일 — 형식, 줄 검사, 기본 프리셋, 단계와 상태

**Files:**
- Create: `src/ModCode/ModMain/Services/PresetService.cs`
- Create: `src/ModCode/ModMain/Presets/07-창-과룡문.json`
- Modify: `src/ModCode/ModMain/ModMain.csproj:61-63`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPreset.cs`(명령 `pdefaults`, `plist`, `pcheck`, `prestore`, `pstage`)
- Modify: `tools/research/preset-check.ps1`(부분 `files`)

**Interfaces:**
- Consumes: `Json.Parse(string) : object`, `ExportFiles.Text`, `ExportFiles.Whole`, `ExportFiles.Ints`, `ExportFiles.Items`, `ChangeLog.DataDir`, `SkillCatalog.Kinds`, `SkillCatalog.Find`, `LuckService.Has`, `LuckService.IsSystem`, `FateService.Records`, `FateService.CurrentGrade`, `FateService.FirstGrade`, Task 2의 `GradeService.FirstRowOf`, Task 3의 `MartialService.OptionProblem`·`OptionNumbers`·`SuitOf`·`AbilityType`, Task 4의 `SpriteService`·`ElderService`·`WingmanService`.
- Produces(모두 `PresetService`, `internal static partial class`):
  - 형식: `SkillLine { MartialType Type; int BaseId; string Name; int[] Options; int Suit; int AbilityType; int GradeMin; int GradeMax; bool Filled; string Problem; }`, `LuckLine { int Id; string Name; bool Filled; string Problem; }`, `FateLine { int Grade; int Id; string Name; bool Filled; string Problem; }`, `SpriteLine { int Id; int ArtifactId; bool Filled; string Problem; }`, `ElderLine { int Id; bool Filled; string Problem; }`, `WingLine { int Id; int[] Effects; int[] Prefixes; bool Filled; string Problem; }`, `Preset { string File, Name, Summary, Source, Error; List<LuckLine> BornLucks; List<SkillLine> Skills, Abilities; List<FateLine> Fates; SpriteLine Sprite; ElderLine Elder; WingLine Wingman; }`.
  - `StageCount`(10), `Dir : string`, `DefaultCount : int`, `DefaultsChanged() : int`, `RestoreDefaults() : string`, `List() : List<Preset>`(파일 이름 순).
  - `Stage { int Grade; List<LuckLine> Born; FateLine Fate; List<SkillLine> Skills, Abilities, OutOfRange; SpriteLine Sprite; ElderLine Elder; WingLine Wingman; }`, `StageOf(Preset, int grade) : Stage`, `StageName(int grade) : string`, `StageText(Stage) : string`.
  - `Learned { string Sole; int BaseId; int Grade; int[] Options; int Suit; bool Equipped; }`, `Snapshot() : List<Learned>`, `Find(List<Learned>, SkillLine, int grade) : Learned`.
  - `enum StageState { Locked, Applied, Partial, None }`, `StateOf(Stage, List<Learned>, out int have, out int total) : StageState`, `AppliedCount(Preset, List<Learned>) : int`, `FateNow(WorldUnitBase, int grade) : int`.
  - `Problems(Preset) : List<string>`, `Usable(Preset) : int`.
  - 개발 명령 `pdefaults` → `embedded= changed= dir=<True|False>`, `plist` → `count= unreadable= | <순번>:<파일 이름>;…`, `pcheck [파일 이름]` → `presets= unreadable= problems= usable= | <문제들>`, `prestore`, `pstage <파일 이름> <대경지>` → `state= have= total= | <구성>`.

- [ ] **Step 1: 검사를 먼저 쓴다**

`preset-check.ps1`의 함수들(`Leave` 아래)에 더한다.

```powershell
function WritePreset([string]$fileName, [string]$json) {
  [void][IO.Directory]::CreateDirectory($presetDir)
  [IO.File]::WriteAllText((Join-Path $presetDir $fileName), $json, (New-Object Text.UTF8Encoding $false))
}
# plist의 " | " 뒤는 "순번:파일 이름;…"이다. 그 파일의 순번(목록의 버튼 이름에 붙는 번호)을 돌려준다. 없으면 -1.
function PresetIndex([string]$fileName) {
  $o = Cmd 'plist' 300
  $names = (($o -split ' \| ', 2)[1]) -split ';'
  foreach ($entry in $names) { if ($entry -match "^(\d+):$([regex]::Escape($fileName))\s*$") { return [int]$Matches[1] } }
  -1
}
```

`'extras-battle' { … }` 아래에 부분을 더한다.

```powershell
  'files' {
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    $n = [int](Fields (Cmd 'pdefaults' 200)).embedded
    Check 'the mod carries default presets' ($n -ge 1) "embedded=$n"
    $l = Fields (Cmd 'plist' 400)
    $onDisk = @(Get-ChildItem -LiteralPath $presetDir -Filter *.json -ErrorAction SilentlyContinue)
    Check 'the folder is made with the defaults in it' ($onDisk.Count -eq $n) "files=$($onDisk.Count)"
    Check 'the list has one readable line per default' ([int]$l.count -eq $n -and $l.unreadable -eq '0') "count=$($l.count) unreadable=$($l.unreadable)"
    Check 'the defaults have no unusable line' ((Fields (Cmd 'pcheck' 600)).problems -eq '0') (Cmd 'pcheck' 600)
    Check 'nothing is reported as changed' ((Fields (Cmd 'pdefaults' 200)).changed -eq '0')

    # 기본 파일을 고치면 알리고, 되돌리기가 돌려놓는다. 사용자가 넣은 파일은 그대로다.
    $first = $onDisk | Sort-Object Name | Select-Object -First 1
    $original = [IO.File]::ReadAllText($first.FullName)
    WritePreset $first.Name '{ "format": 1, "kind": "preset", "name": "uicheck changed" }'
    WritePreset 'uicheck-user.json' '{ "format": 1, "kind": "preset", "name": "uicheck user", "skills": [ { "slot": "left", "baseId": 13111, "options": [] } ] }'
    Check 'a changed default is counted' ((Fields (Cmd 'pdefaults' 200)).changed -eq '1')
    Check 'the user file is listed' ([int](Fields (Cmd 'plist' 400)).count -eq $n + 1 -and (PresetIndex 'uicheck-user.json') -ge 0)
    Cmd 'prestore' 400 | Out-Null
    Check 'restore writes the default back' ((Fields (Cmd 'pdefaults' 200)).changed -eq '0' -and [IO.File]::ReadAllText($first.FullName).Replace("`r", '').Trim() -eq $original.Replace("`r", '').Trim())
    Check 'restore leaves the user file alone' (Test-Path -LiteralPath (Join-Path $presetDir 'uicheck-user.json'))
    # 폴더가 이미 있으면 지워진 기본 파일을 다시 풀지 않는다(사용자가 지운 것일 수 있다). 대신 알린다.
    [IO.File]::Delete($first.FullName)
    Check 'a deleted default is not written again by listing' ([int](Fields (Cmd 'plist' 400)).count -eq $n -and -not (Test-Path -LiteralPath $first.FullName))
    Check 'a deleted default is counted' ((Fields (Cmd 'pdefaults' 200)).changed -eq '1')
    Cmd 'prestore' 400 | Out-Null

    # 읽을 수 없는 파일
    WritePreset 'uicheck-broken.json' '{ "format": 1, "kind": "preset", '
    WritePreset 'uicheck-bag.json' '{ "format": 1, "kind": "bag", "name": "x", "items": [] }'
    WritePreset 'uicheck-format.json' '{ "format": 2, "kind": "preset", "name": "x" }'
    $l = Fields (Cmd 'plist' 400)
    Check 'three unreadable files are listed as unreadable' ([int]$l.count -eq $n + 4 -and $l.unreadable -eq '3') "count=$($l.count) unreadable=$($l.unreadable)"

    # 틀린 줄: 그 줄만 쓸 수 없고 나머지는 쓸 수 있다. 줄마다의 뜻은 아래 JSON의 순서대로:
    # 없는 기술 / 칸이 다른 기술 / 없는 옵션 / (쓸 수 있음) / 같은 칸 둘째 / 글자가 든 번호,
    # (쓸 수 있음) / 같은 유형 둘째 / 구성원이 아닌 세트 / 없는 세트, 역천개명이 아닌 번호 / 없는 대경지, 없는 기령, 없는 신혼, 남의 명우 옵션
    WritePreset 'uicheck-lines.json' @'
{
  "format": 1, "kind": "preset", "name": "uicheck lines",
  "skills": [
    { "slot": "left", "baseId": 999999, "options": [] },
    { "slot": "left", "baseId": 22111, "options": [] },
    { "slot": "right", "baseId": 22111, "options": [99] },
    { "slot": "step", "baseId": 510021, "options": [] },
    { "slot": "step", "baseId": 510021, "options": [] },
    { "slot": "ultimate", "baseId": "x" }
  ],
  "abilities": [
    { "baseId": 81001, "options": [] },
    { "baseId": 81001, "options": [] },
    { "baseId": 82001, "options": [], "suit": 10016 },
    { "baseId": 83001, "options": [], "suit": 99999 }
  ],
  "fates": [ { "grade": 2, "id": 1 }, { "grade": 11, "id": 700016 } ],
  "sprite": { "id": 9999, "artifact": 3031101 },
  "elder": { "id": 99 },
  "wingman": { "id": 1004, "effects": [100101], "prefixes": [100101] }
}
'@
    $c = Fields (Cmd 'pcheck uicheck-lines.json' 600)
    Check 'thirteen bad lines are found and two lines stay usable' ($c.problems -eq '13' -and $c.usable -eq '2') (Cmd 'pcheck uicheck-lines.json' 600)

    # 단계: 결정 초기(대경지 3)에서 1~3은 열려 있고 4부터는 경지 미달이다. 23116은 경지 3부터의 기술이라 단계 1·2에는 없다.
    WritePreset 'uicheck-stages.json' '{ "format": 1, "kind": "preset", "name": "uicheck stages", "skills": [ { "slot": "left", "baseId": 14111, "options": [] }, { "slot": "right", "baseId": 23116, "options": [] } ], "elder": { "id": 1 }, "wingman": { "id": 1004, "effects": [], "prefixes": [] } }'
    $st1 = Fields (Cmd 'pstage uicheck-stages.json 1' 400)
    $st3 = Fields (Cmd 'pstage uicheck-stages.json 3' 400)
    $st4 = Fields (Cmd 'pstage uicheck-stages.json 4' 400)
    $st9 = Fields (Cmd 'pstage uicheck-stages.json 9' 400)
    Check 'stage 1 has one line, the skill that exists at grade 1' ($st1.total -eq '1' -and $st1.state -eq 'None') "state=$($st1.state) total=$($st1.total)"
    Check 'stage 3 has both skills' ($st3.total -eq '2' -and $st3.state -eq 'None') "state=$($st3.state) total=$($st3.total)"
    Check 'stage 4 is locked' ($st4.state -eq 'Locked')
    Check 'stage 9 adds the elder and the wingman' ($st9.total -eq '4' -and $st9.state -eq 'Locked') "total=$($st9.total)"

    foreach ($file in Get-ChildItem -LiteralPath $presetDir -Filter 'uicheck-*.json') { [IO.File]::Delete($file.FullName) }
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab9 -Fixture base
pwsh tools/research/preset-check.ps1 -Part files
pwsh tools/DevRemote/session.ps1 -End ptab9
```

Expected: 첫 줄 "the mod carries default presets"부터 FAIL(`pdefaults`가 `UNKNOWN`).

- [ ] **Step 3: 기본 프리셋의 초안과 프로젝트 설정**

`src/ModCode/ModMain/Presets/07-창-과룡문.json`(UTF-8, BOM 없음). 번호는 게임에서 확인한 것이다(스펙 3절). 옵션, 역천개명, 신법, 기령, 신혼, 명우는 둘째 계획에서 채운다.

```json
{
  "format": 1,
  "kind": "preset",
  "name": "창 — 과룡문",
  "summary": "천심모·질룡파·패왕순과 과룡문 세트 (초안)",
  "source": "무협게임 마이너 갤러리 23356",
  "skills": [
    { "slot": "left", "baseId": 13111, "name": "천심모", "options": [] },
    { "slot": "right", "baseId": 23116, "name": "질룡파", "options": [] },
    { "slot": "ultimate", "baseId": 35111, "name": "패왕순", "options": [] }
  ],
  "abilities": [
    { "baseId": 81001, "options": [], "suit": 10016 },
    { "baseId": 83001, "options": [], "suit": 10016 },
    { "baseId": 84001, "options": [], "suit": 10016 },
    { "baseId": 86002, "options": [], "suit": 10016 },
    { "baseId": 87002, "options": [], "suit": 10016 },
    { "baseId": 88002, "options": [], "suit": 10016 }
  ]
}
```

`ModMain.csproj`의 `<ItemGroup Condition="'$(DevTools)' != 'true'">` 블록(61~63행) 아래에 넣는다.

```xml
  <ItemGroup>
    <!-- 기본 프리셋. DLL 안에 넣어 두고 presets 폴더가 없을 때 풀어 놓는다(산출물은 DLL 하나여야 한다). 이름은 "Presets/<파일 이름>"이다. -->
    <EmbeddedResource Include="Presets\*.json" LogicalName="Presets/%(Filename)%(Extension)" />
  </ItemGroup>
```

- [ ] **Step 4: `Services/PresetService.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MOD_sWP6UL
{
    // 프리셋: 빌드 하나의 완성형을 적은 JSON 파일. 단계(대경지 1~10)는 완성형에서 규칙으로 만든다.
    // 화면을 모르고 결과를 문장으로 돌려준다. 대상은 항상 플레이어다. 근거: docs/superpowers/specs/2026-10-14-preset-tab-design.md.
    // 이 파일은 형식·읽기·기본 프리셋·단계·상태를, PresetService.Apply.cs는 적용을 맡는다.
    internal static partial class PresetService
    {
        public const string Kind = "preset";
        public const int StageCount = 10;
        private const string ResourcePrefix = "Presets/";
        private const string BadShape = "줄의 모양이 틀립니다";
        private const string Unknown = "게임에 없는 번호입니다";

        private static readonly string[] SlotNames = { "left", "right", "step", "ultimate" };
        private static readonly MartialType[] SlotTypes = { MartialType.SkillLeft, MartialType.SkillRight, MartialType.Step, MartialType.Ultimate };

        public class SkillLine
        {
            public MartialType Type;
            public int BaseId;
            public string Name = "";
            public int[] Options = new int[0];
            public int Suit;                // 심법에 쓸 세트 번호. 0이면 쓰지 않는다
            public int AbilityType;         // 심법의 유형(1~8). 심법이 아니면 0
            public int GradeMin = 1;
            public int GradeMax = StageCount;
            public bool Filled;             // 공략에 없어 채운 줄
            public string Problem;          // 이 줄을 쓸 수 없는 이유. null이면 쓸 수 있다
        }

        public class LuckLine
        {
            public int Id;
            public string Name = "";
            public bool Filled;
            public string Problem;
        }

        public class FateLine
        {
            public int Grade;
            public int Id;
            public string Name = "";
            public bool Filled;
            public string Problem;
        }

        public class SpriteLine
        {
            public int Id;
            public int ArtifactId;
            public bool Filled;
            public string Problem;
        }

        public class ElderLine
        {
            public int Id;
            public bool Filled;
            public string Problem;
        }

        public class WingLine
        {
            public int Id;
            public int[] Effects = new int[0];
            public int[] Prefixes = new int[0];
            public bool Filled;
            public string Problem;
        }

        public class Preset
        {
            public string File;
            public string Name = "";
            public string Summary = "";
            public string Source = "";
            public string Error;            // 읽을 수 없는 이유. null이면 읽혔다
            public List<LuckLine> BornLucks = new List<LuckLine>();
            public List<SkillLine> Skills = new List<SkillLine>();
            public List<SkillLine> Abilities = new List<SkillLine>();
            public List<FateLine> Fates = new List<FateLine>();
            public SpriteLine Sprite;       // 구역이 없으면 null
            public ElderLine Elder;
            public WingLine Wingman;
        }

        private static WorldUnitBase Me
        {
            get { return g.world.playerUnit; }
        }

        public static string Dir
        {
            get { return Path.Combine(ChangeLog.DataDir, "presets"); }
        }

        // ---- 기본 프리셋 (DLL 안의 파일) ----

        private static Dictionary<string, string> defaults;     // 파일 이름 → 내용

        private static Dictionary<string, string> Defaults()
        {
            if (defaults != null)
            {
                return defaults;
            }
            defaults = new Dictionary<string, string>();
            Assembly assembly = typeof(PresetService).Assembly;
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                {
                    continue;
                }
                using (Stream stream = assembly.GetManifestResourceStream(name))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    defaults[name.Substring(ResourcePrefix.Length)] = reader.ReadToEnd();
                }
            }
            return defaults;
        }

        public static int DefaultCount
        {
            get { return Defaults().Count; }
        }

        private static void WriteDefaults()
        {
            Directory.CreateDirectory(Dir);
            foreach (var pair in Defaults())
            {
                System.IO.File.WriteAllText(Path.Combine(Dir, pair.Key), pair.Value, new UTF8Encoding(false));
            }
        }

        private static string Normal(string text)
        {
            return text.Replace("\r", "").Trim();
        }

        // 폴더에 없거나 DLL 안의 것과 내용이 다른 기본 프리셋의 수. 모드를 갱신한 뒤 새 기본값이 있음을 알리는 데 쓴다.
        public static int DefaultsChanged()
        {
            int changed = 0;
            foreach (var pair in Defaults())
            {
                string path = Path.Combine(Dir, pair.Key);
                try
                {
                    if (!System.IO.File.Exists(path) || Normal(System.IO.File.ReadAllText(path, Encoding.UTF8)) != Normal(pair.Value))
                    {
                        changed++;
                    }
                }
                catch (IOException)
                {
                    changed++;
                }
                catch (UnauthorizedAccessException)
                {
                    changed++;
                }
            }
            return changed;
        }

        // 기본 프리셋을 다시 쓴다. 고친 기본 파일은 덮어쓰고, 다른 파일은 건드리지 않는다.
        public static string RestoreDefaults()
        {
            try
            {
                WriteDefaults();
                return "기본 프리셋 " + DefaultCount + "개를 다시 썼습니다.";
            }
            catch (IOException e)
            {
                return "기본 프리셋: 쓰지 못했습니다(" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "기본 프리셋: 쓰지 못했습니다(" + e.Message + ").";
            }
        }

        // 폴더의 프리셋들. 파일 이름 순(대소문자 무시). 폴더가 없으면 만들고 기본 프리셋을 풀어 놓는다.
        // 폴더가 이미 있으면 없는 기본 파일을 다시 풀지 않는다(사용자가 지운 것일 수 있다).
        public static List<Preset> List()
        {
            var result = new List<Preset>();
            try
            {
                if (!Directory.Exists(Dir))
                {
                    WriteDefaults();
                }
                string[] files = Directory.GetFiles(Dir, "*.json");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    result.Add(Read(file));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return result;
        }

        // ---- 읽기와 줄 검사 ----

        private static bool Flag(Dictionary<string, object> o, string key)
        {
            object value;
            return o.TryGetValue(key, out value) && value is bool && (bool)value;
        }

        private static string Named(string name, int id)
        {
            return string.IsNullOrEmpty(name) ? id.ToString() : name;
        }

        // 그 종류의 기술 목록에서 번호로 찾는다. 없으면 null.
        private static SkillCatalog.Entry Entry(MartialType type, int baseId)
        {
            for (int kind = 0; kind < SkillCatalog.Kinds.Length; kind++)
            {
                if (SkillCatalog.Kinds[kind].Type != type)
                {
                    continue;
                }
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(kind, baseId.ToString()))
                {
                    if (entry.BaseId == baseId)
                    {
                        return entry;
                    }
                }
            }
            return null;
        }

        private static bool KnownSkill(int baseId)
        {
            for (int kind = 0; kind < SkillCatalog.Kinds.Length; kind++)
            {
                if (Entry(SkillCatalog.Kinds[kind].Type, baseId) != null)
                {
                    return true;
                }
            }
            return false;
        }

        // 기술 번호를 게임에서 찾아 이름과 경지 범위를 채우고, 쓸 수 없으면 이유를 적는다.
        private static void Finish(SkillLine line, bool duplicate)
        {
            SkillCatalog.Entry entry = Entry(line.Type, line.BaseId);
            if (entry == null)
            {
                line.Problem = KnownSkill(line.BaseId) ? "이 칸에 넣을 수 없는 기술입니다" : Unknown;
                return;
            }
            line.Name = entry.Name;
            line.GradeMin = entry.GradeMin;
            line.GradeMax = entry.GradeMax;
            line.Problem = duplicate ? "같은 칸의 줄이 이미 있습니다" : MartialService.OptionProblem(line.Type, line.BaseId, line.Options);
        }

        private static SkillLine ReadSkill(Dictionary<string, object> o, List<MartialType> seen)
        {
            var line = new SkillLine();
            if (o == null || !ExportFiles.Whole(o, "baseId", out line.BaseId) || !ExportFiles.Ints(o, "options", out line.Options))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Name = ExportFiles.Text(o, "name");
            line.Filled = Flag(o, "filled");
            int slot = Array.IndexOf(SlotNames, ExportFiles.Text(o, "slot"));
            if (slot < 0)
            {
                line.Problem = "slot은 left, right, step, ultimate 가운데 하나여야 합니다";
                return line;
            }
            line.Type = SlotTypes[slot];
            Finish(line, seen.Contains(line.Type));
            if (line.Problem == null)
            {
                seen.Add(line.Type);
            }
            return line;
        }

        private static SkillLine ReadAbility(Dictionary<string, object> o, List<int> seen)
        {
            var line = new SkillLine { Type = MartialType.Ability };
            if (o == null || !ExportFiles.Whole(o, "baseId", out line.BaseId) || !ExportFiles.Ints(o, "options", out line.Options)
                || (o.ContainsKey("suit") && !ExportFiles.Whole(o, "suit", out line.Suit)))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Name = ExportFiles.Text(o, "name");
            line.Filled = Flag(o, "filled");
            line.AbilityType = MartialService.AbilityType(line.BaseId);
            Finish(line, line.AbilityType != 0 && seen.Contains(line.AbilityType));
            if (line.Problem == null && line.Suit != 0)
            {
                ConfBattleAbilitySuitBaseItem suit = g.conf.battleAbilitySuitBase.GetItem(line.Suit);
                if (suit == null)
                {
                    line.Problem = "게임에 없는 세트입니다(" + line.Suit + ")";
                }
                else if (Array.IndexOf((suit.suitMember ?? "").Split('|'), line.BaseId.ToString()) < 0)
                {
                    line.Problem = "이 세트의 구성원이 아닙니다(" + SuitName(line.Suit) + ")";
                }
            }
            if (line.Problem == null)
            {
                seen.Add(line.AbilityType);
            }
            return line;
        }

        private static LuckLine ReadLuck(Dictionary<string, object> o)
        {
            var line = new LuckLine();
            if (o == null || !ExportFiles.Whole(o, "id", out line.Id))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Name = ExportFiles.Text(o, "name");
            line.Filled = Flag(o, "filled");
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(line.Id);
            if (conf == null)
            {
                line.Problem = Unknown;
                return line;
            }
            line.Name = GameTool.LS(conf.name);
            // 역천개명 분류를 운명으로 붙이면 지금 대경지의 역천개명 기록이 덮어써진다(docs/05 31절). 진행 표식과 숨김 운명도 받지 않는다.
            if (conf.type == 3 || conf.conceal != 0 || LuckService.IsSystem(line.Id))
            {
                line.Problem = "선천운명으로 붙일 수 없는 운명입니다";
            }
            return line;
        }

        private static FateLine ReadFate(Dictionary<string, object> o, List<int> seen)
        {
            var line = new FateLine();
            if (o == null || !ExportFiles.Whole(o, "grade", out line.Grade) || !ExportFiles.Whole(o, "id", out line.Id))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Name = ExportFiles.Text(o, "name");
            line.Filled = Flag(o, "filled");
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(line.Id);
            ConfFateFeatureItem fate = g.conf.fateFeature.GetItem(line.Id);
            if (conf == null || fate == null)
            {
                line.Problem = "게임의 역천개명 목록에 없습니다";
                return line;
            }
            line.Name = GameTool.LS(conf.name);
            if (line.Grade < FateService.FirstGrade || line.Grade > StageCount || fate.gradeMin > line.Grade || fate.gradeMax < line.Grade)
            {
                line.Problem = "이 경지의 역천개명이 아닙니다";
            }
            else if (seen.Contains(line.Grade))
            {
                line.Problem = "같은 경지의 줄이 이미 있습니다";
            }
            else
            {
                seen.Add(line.Grade);
            }
            return line;
        }

        private static SpriteLine ReadSprite(Dictionary<string, object> o)
        {
            var line = new SpriteLine();
            if (o == null || !ExportFiles.Whole(o, "id", out line.Id) || !ExportFiles.Whole(o, "artifact", out line.ArtifactId))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Filled = Flag(o, "filled");
            if (!SpriteService.Known(line.Id))
            {
                line.Problem = "게임에 없는 기령 번호입니다(" + line.Id + ")";
            }
            else if (!SpriteService.IsArtifact(line.ArtifactId))
            {
                line.Problem = "게임의 법보가 아닙니다(" + line.ArtifactId + ")";
            }
            return line;
        }

        private static ElderLine ReadElder(Dictionary<string, object> o)
        {
            var line = new ElderLine();
            if (o == null || !ExportFiles.Whole(o, "id", out line.Id))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Filled = Flag(o, "filled");
            if (!ElderService.Known(line.Id))
            {
                line.Problem = Unknown;
            }
            return line;
        }

        private static WingLine ReadWing(Dictionary<string, object> o)
        {
            var line = new WingLine();
            if (o == null || !ExportFiles.Whole(o, "id", out line.Id) || !ExportFiles.Ints(o, "effects", out line.Effects)
                || !ExportFiles.Ints(o, "prefixes", out line.Prefixes))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Filled = Flag(o, "filled");
            if (!WingmanService.Known(line.Id))
            {
                line.Problem = Unknown;
                return line;
            }
            foreach (int effect in line.Effects)
            {
                if (!WingmanService.EffectKnown(effect))
                {
                    line.Problem = "게임에 없는 효과 번호입니다(" + effect + ")";
                    return line;
                }
            }
            foreach (int prefix in line.Prefixes)
            {
                if (!WingmanService.PrefixOf(prefix, line.Id))
                {
                    line.Problem = "이 명우의 옵션이 아닙니다(" + prefix + ")";
                    return line;
                }
            }
            return line;
        }

        private static Preset Read(string file)
        {
            var preset = new Preset { File = file, Name = Path.GetFileNameWithoutExtension(file) };
            try
            {
                var root = Json.Parse(System.IO.File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>;
                int format;
                if (root == null || !ExportFiles.Whole(root, "format", out format) || format != 1 || ExportFiles.Text(root, "kind") != Kind)
                {
                    preset.Error = "이 편집기의 프리셋 파일(format 1)이 아닙니다";
                    return preset;
                }
                string name = ExportFiles.Text(root, "name");
                if (name.Length > 0)
                {
                    preset.Name = name;
                }
                preset.Summary = ExportFiles.Text(root, "summary");
                preset.Source = ExportFiles.Text(root, "source");
                foreach (var o in ExportFiles.Items(root, "bornLucks"))
                {
                    preset.BornLucks.Add(ReadLuck(o));
                }
                var slots = new List<MartialType>();
                foreach (var o in ExportFiles.Items(root, "skills"))
                {
                    preset.Skills.Add(ReadSkill(o, slots));
                }
                var types = new List<int>();
                foreach (var o in ExportFiles.Items(root, "abilities"))
                {
                    preset.Abilities.Add(ReadAbility(o, types));
                }
                var grades = new List<int>();
                foreach (var o in ExportFiles.Items(root, "fates"))
                {
                    preset.Fates.Add(ReadFate(o, grades));
                }
                // 구역이 통째로 없으면 그 구역은 건드리지 않는다. 있는데 객체가 아니면 쓸 수 없는 줄로 남긴다.
                object raw;
                if (root.TryGetValue("sprite", out raw) && raw != null)
                {
                    preset.Sprite = ReadSprite(raw as Dictionary<string, object>);
                }
                if (root.TryGetValue("elder", out raw) && raw != null)
                {
                    preset.Elder = ReadElder(raw as Dictionary<string, object>);
                }
                if (root.TryGetValue("wingman", out raw) && raw != null)
                {
                    preset.Wingman = ReadWing(raw as Dictionary<string, object>);
                }
            }
            catch (FormatException e)
            {
                preset.Error = "JSON 문법 오류(" + e.Message + ")";
            }
            catch (IOException e)
            {
                preset.Error = e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                preset.Error = e.Message;
            }
            return preset;
        }

        private static void Note(List<string> into, string section, string name, string problem)
        {
            if (problem != null)
            {
                into.Add(section + " " + name + ": " + problem);
            }
        }

        // 프리셋의 쓸 수 없는 줄들("구역 이름: 이유").
        public static List<string> Problems(Preset preset)
        {
            var result = new List<string>();
            foreach (LuckLine line in preset.BornLucks)
            {
                Note(result, "선천운명", Named(line.Name, line.Id), line.Problem);
            }
            foreach (SkillLine line in preset.Skills)
            {
                Note(result, "공법", Named(line.Name, line.BaseId), line.Problem);
            }
            foreach (SkillLine line in preset.Abilities)
            {
                Note(result, "심법", Named(line.Name, line.BaseId), line.Problem);
            }
            foreach (FateLine line in preset.Fates)
            {
                Note(result, "역천개명", line.Grade + ":" + Named(line.Name, line.Id), line.Problem);
            }
            if (preset.Sprite != null)
            {
                Note(result, "법보와 기령", preset.Sprite.Id.ToString(), preset.Sprite.Problem);
            }
            if (preset.Elder != null)
            {
                Note(result, "신혼", preset.Elder.Id.ToString(), preset.Elder.Problem);
            }
            if (preset.Wingman != null)
            {
                Note(result, "명우", preset.Wingman.Id.ToString(), preset.Wingman.Problem);
            }
            return result;
        }

        // 쓸 수 있는 줄의 수.
        public static int Usable(Preset preset)
        {
            int total = preset.BornLucks.Count + preset.Skills.Count + preset.Abilities.Count + preset.Fates.Count
                + (preset.Sprite == null ? 0 : 1) + (preset.Elder == null ? 0 : 1) + (preset.Wingman == null ? 0 : 1);
            return total - Problems(preset).Count;
        }

        // ---- 단계 ----

        public class Stage
        {
            public int Grade;
            public List<LuckLine> Born = new List<LuckLine>();
            public FateLine Fate;                                           // 없으면 null
            public List<SkillLine> Skills = new List<SkillLine>();
            public List<SkillLine> Abilities = new List<SkillLine>();
            public List<SkillLine> OutOfRange = new List<SkillLine>();      // 이 경지에는 없는 기술. 보이기만 한다
            public SpriteLine Sprite;
            public ElderLine Elder;
            public WingLine Wingman;
        }

        public static string StageName(int grade)
        {
            ConfRoleGradeItem row = GradeService.FirstRowOf(grade);
            return row == null ? "경지 " + grade : GameTool.LS(row.gradeName);
        }

        public static string SuitName(int suitId)
        {
            ConfBattleAbilitySuitBaseItem conf = g.conf.battleAbilitySuitBase.GetItem(suitId);
            return conf == null ? suitId.ToString() : EditService.Compact(GameTool.LS(conf.suitName));
        }

        // 쓸 수 없는 줄은 어느 단계에나 보인다(이유를 알리기 위해서다).
        private static bool InRange(SkillLine line, int grade)
        {
            return line.Problem != null || (grade >= line.GradeMin && grade <= line.GradeMax);
        }

        // 대경지 grade에서 적용하는 것들(스펙 5.2절).
        public static Stage StageOf(Preset preset, int grade)
        {
            var stage = new Stage { Grade = grade };
            if (grade == 1)
            {
                stage.Born.AddRange(preset.BornLucks);
            }
            stage.Fate = preset.Fates.Find(f => f.Grade == grade && f.Problem == null) ?? preset.Fates.Find(f => f.Grade == grade);
            foreach (SkillLine line in preset.Skills)
            {
                (InRange(line, grade) ? stage.Skills : stage.OutOfRange).Add(line);
            }
            foreach (SkillLine line in preset.Abilities)
            {
                (InRange(line, grade) ? stage.Abilities : stage.OutOfRange).Add(line);
            }
            stage.Sprite = grade >= SpriteService.FirstGrade ? preset.Sprite : null;
            stage.Elder = grade >= ElderService.FirstGrade ? preset.Elder : null;
            stage.Wingman = grade >= WingmanService.FirstGrade ? preset.Wingman : null;
            return stage;
        }

        private static string Mark(bool filled, string problem)
        {
            return (filled ? "(공략에 없어 채움)" : "") + (problem == null ? "" : " [쓸 수 없음: " + problem + "]");
        }

        // 단계의 구성을 여러 줄의 글로. 상세보기의 줄과 검증이 쓴다.
        public static string StageText(Stage stage)
        {
            var sb = new StringBuilder();
            if (stage.Born.Count > 0)
            {
                sb.Append("선천운명: ");
                for (int i = 0; i < stage.Born.Count; i++)
                {
                    LuckLine line = stage.Born[i];
                    sb.Append(i == 0 ? "" : ", ").Append(Named(line.Name, line.Id)).Append(Mark(line.Filled, line.Problem));
                }
                sb.Append('\n');
            }
            if (stage.Fate != null)
            {
                sb.Append("역천개명: ").Append(Named(stage.Fate.Name, stage.Fate.Id)).Append(Mark(stage.Fate.Filled, stage.Fate.Problem)).Append('\n');
            }
            if (stage.Skills.Count > 0)
            {
                sb.Append("공법(경지 ").Append(stage.Grade).Append("): ");
                for (int i = 0; i < stage.Skills.Count; i++)
                {
                    SkillLine line = stage.Skills[i];
                    sb.Append(i == 0 ? "" : ", ").Append(MartialService.TypeLabel(line.Type)).Append(' ').Append(Named(line.Name, line.BaseId))
                        .Append("(옵션 ").Append(line.Options.Length).Append(')').Append(Mark(line.Filled, line.Problem));
                }
                sb.Append('\n');
            }
            if (stage.Abilities.Count > 0)
            {
                sb.Append("심법 ").Append(stage.Abilities.Count).Append("(경지 ").Append(stage.Grade).Append("): ");
                for (int i = 0; i < stage.Abilities.Count; i++)
                {
                    SkillLine line = stage.Abilities[i];
                    sb.Append(i == 0 ? "" : ", ").Append(Named(line.Name, line.BaseId))
                        .Append(line.Suit == 0 ? "" : "[" + SuitName(line.Suit) + "]").Append(Mark(line.Filled, line.Problem));
                }
                sb.Append('\n');
            }
            if (stage.OutOfRange.Count > 0)
            {
                sb.Append("이 경지에는 없는 기술: ");
                for (int i = 0; i < stage.OutOfRange.Count; i++)
                {
                    SkillLine line = stage.OutOfRange[i];
                    sb.Append(i == 0 ? "" : ", ").Append(Named(line.Name, line.BaseId)).Append("(경지 ").Append(line.GradeMin).Append('~').Append(line.GradeMax).Append(')');
                }
                sb.Append('\n');
            }
            if (stage.Sprite != null)
            {
                sb.Append("법보와 기령: ").Append(SpriteService.ArtifactName(stage.Sprite.ArtifactId)).Append(" + ").Append(SpriteService.SpriteName(stage.Sprite.Id))
                    .Append(Mark(stage.Sprite.Filled, stage.Sprite.Problem)).Append('\n');
            }
            if (stage.Elder != null)
            {
                sb.Append("신혼: ").Append(ElderService.Name(stage.Elder.Id)).Append(Mark(stage.Elder.Filled, stage.Elder.Problem)).Append('\n');
            }
            if (stage.Wingman != null)
            {
                sb.Append("명우: ").Append(WingmanService.Name(stage.Wingman.Id)).Append("(효과 ").Append(stage.Wingman.Effects.Length)
                    .Append(", 옵션 ").Append(stage.Wingman.Prefixes.Length).Append(')').Append(Mark(stage.Wingman.Filled, stage.Wingman.Problem)).Append('\n');
            }
            return sb.Length == 0 ? "(이 단계에는 적용할 것이 없습니다)" : sb.ToString().TrimEnd('\n');
        }

        // ---- 상태 ----

        public class Learned
        {
            public string Sole;
            public int BaseId;
            public int Grade;
            public int[] Options;
            public int Suit;
            public bool Equipped;
        }

        // 익힌 공법의 사본. 상태를 여러 번 물을 때 게임 객체를 다시 읽지 않도록 한 번 떠 둔다.
        public static List<Learned> Snapshot()
        {
            var result = new List<Learned>();
            WorldUnitBase me = Me;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                // 깨진 공법은 읽는 것만으로 예외가 난다. 없는 것으로 본다.
                try
                {
                    DataProps.PropsData props = pair.Value.data;
                    DataProps.MartialData martial = props.To<DataProps.MartialData>();
                    result.Add(new Learned
                    {
                        Sole = pair.Key,
                        BaseId = martial.baseID,
                        Grade = martial.grade,
                        Options = MartialService.OptionNumbers(martial),
                        Suit = martial.martialType == MartialType.Ability ? MartialService.SuitOf(props) : 0,
                        Equipped = MartialService.IsEquipped(me, pair.Key),
                    });
                }
                catch (Exception)
                {
                }
            }
            return result;
        }

        private static bool SameOptions(int[] a, int[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        // 그 줄과 같은 공법: 같은 기술과 옵션, 그 경지 이상, 심법이면 세트 번호도 같다. 장착한 것을 먼저 고른다. 없으면 null.
        public static Learned Find(List<Learned> learned, SkillLine line, int grade)
        {
            Learned found = null;
            foreach (Learned each in learned)
            {
                if (each.BaseId != line.BaseId || each.Grade < grade || !SameOptions(each.Options, line.Options))
                {
                    continue;
                }
                if (line.Type == MartialType.Ability && each.Suit != line.Suit)
                {
                    continue;
                }
                if (found == null || (each.Equipped && !found.Equipped))
                {
                    found = each;
                }
            }
            return found;
        }

        // 그 대경지의 역천개명 기록. 없으면 0.
        public static int FateNow(WorldUnitBase unit, int grade)
        {
            int id;
            return FateService.Records(unit).TryGetValue(grade, out id) ? id : 0;
        }

        public enum StageState { Locked, Applied, Partial, None }

        private static void Tick(bool usable, bool present, ref int have, ref int total)
        {
            if (!usable)
            {
                return;
            }
            total++;
            have += present ? 1 : 0;
        }

        // 단계의 상태. total은 쓸 수 있는 줄의 수, have는 그 가운데 이미 있는 줄의 수다(스펙 5.2절의 "있음").
        public static StageState StateOf(Stage stage, List<Learned> learned, out int have, out int total)
        {
            WorldUnitBase me = Me;
            have = 0;
            total = 0;
            foreach (LuckLine line in stage.Born)
            {
                Tick(line.Problem == null, line.Problem == null && LuckService.Has(me, line.Id), ref have, ref total);
            }
            if (stage.Fate != null)
            {
                Tick(stage.Fate.Problem == null, FateNow(me, stage.Fate.Grade) == stage.Fate.Id, ref have, ref total);
            }
            foreach (SkillLine line in stage.Skills)
            {
                Learned same = line.Problem == null ? Find(learned, line, stage.Grade) : null;
                Tick(line.Problem == null, same != null && same.Equipped, ref have, ref total);
            }
            foreach (SkillLine line in stage.Abilities)
            {
                Learned same = line.Problem == null ? Find(learned, line, stage.Grade) : null;
                Tick(line.Problem == null, same != null && same.Equipped, ref have, ref total);
            }
            if (stage.Sprite != null)
            {
                Tick(stage.Sprite.Problem == null, stage.Sprite.Problem == null && SpriteService.Has(me, stage.Sprite.Id, stage.Sprite.ArtifactId), ref have, ref total);
            }
            if (stage.Elder != null)
            {
                Tick(stage.Elder.Problem == null, stage.Elder.Problem == null && ElderService.Has(me, stage.Elder.Id), ref have, ref total);
            }
            if (stage.Wingman != null)
            {
                Tick(stage.Wingman.Problem == null,
                    stage.Wingman.Problem == null && WingmanService.Has(me, stage.Wingman.Id, stage.Wingman.Effects, stage.Wingman.Prefixes), ref have, ref total);
            }
            if (FateService.CurrentGrade(me) < stage.Grade)
            {
                return StageState.Locked;
            }
            return have == total ? StageState.Applied : have == 0 ? StageState.None : StageState.Partial;
        }

        // "적용됨"인 단계의 수. 목록의 상태 칸에 쓴다.
        public static int AppliedCount(Preset preset, List<Learned> learned)
        {
            int count = 0;
            for (int grade = 1; grade <= StageCount; grade++)
            {
                int have, total;
                if (StateOf(StageOf(preset, grade), learned, out have, out total) == StageState.Applied && total > 0)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
```

- [ ] **Step 5: 개발 명령을 더한다**

`Dev/DevCommandsPreset.cs`의 `switch`에 넣는다.

```csharp
                case "pdefaults":
                    return "embedded=" + PresetService.DefaultCount + " changed=" + PresetService.DefaultsChanged()
                        + " dir=" + System.IO.Directory.Exists(PresetService.Dir);
                case "plist": return ListPresets();
                case "pcheck": return CheckPresets(p.Length > 1 ? p[1] : null);
                case "prestore": return PresetService.RestoreDefaults();
                case "pstage": return StageInfo(p[1], int.Parse(p[2]));
```

메서드를 더한다.

```csharp
        private static PresetService.Preset PresetNamed(string fileName)
        {
            return PresetService.List().Find(x => System.IO.Path.GetFileName(x.File) == fileName);
        }

        private static string ListPresets()
        {
            List<PresetService.Preset> presets = PresetService.List();
            var names = new StringBuilder();
            int unreadable = 0;
            for (int i = 0; i < presets.Count; i++)
            {
                unreadable += presets[i].Error == null ? 0 : 1;
                names.Append(i == 0 ? "" : ";").Append(i).Append(':').Append(System.IO.Path.GetFileName(presets[i].File));
            }
            return "count=" + presets.Count + " unreadable=" + unreadable + " | " + names;
        }

        // 폴더의 프리셋(파일 이름을 주면 그것만)의 쓸 수 없는 줄을 센다. 기본 프리셋은 0이어야 한다.
        private static string CheckPresets(string fileName)
        {
            int count = 0, unreadable = 0, usable = 0;
            var problems = new List<string>();
            foreach (PresetService.Preset preset in PresetService.List())
            {
                if (fileName != null && System.IO.Path.GetFileName(preset.File) != fileName)
                {
                    continue;
                }
                count++;
                if (preset.Error != null)
                {
                    unreadable++;
                    continue;
                }
                usable += PresetService.Usable(preset);
                foreach (string problem in PresetService.Problems(preset))
                {
                    problems.Add(System.IO.Path.GetFileName(preset.File) + " " + problem);
                }
            }
            return "presets=" + count + " unreadable=" + unreadable + " problems=" + problems.Count + " usable=" + usable
                + " | " + string.Join(" / ", problems.ToArray());
        }

        private static string StageInfo(string fileName, int grade)
        {
            PresetService.Preset preset = PresetNamed(fileName);
            if (preset == null || preset.Error != null)
            {
                return "no readable preset " + fileName;
            }
            PresetService.Stage stage = PresetService.StageOf(preset, grade);
            int have, total;
            PresetService.StageState state = PresetService.StateOf(stage, PresetService.Snapshot(), out have, out total);
            return "state=" + state + " have=" + have + " total=" + total + " | " + PresetService.StageText(stage).Replace("\n", " / ");
        }
```

- [ ] **Step 6: 빌드하고 검사가 통과하는 것을 본다**

공통 절차의 빌드 두 줄. Expected: 오류 0. 배포 구성의 산출물 폴더(`$TEMP/tltoybox-release-check`)에 DLL이 하나뿐인지 본다.

```bash
ls "$TEMP/tltoybox-release-check" | grep -c -i "\.dll$"
```

Expected: `1`.

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab10 -Fixture base
pwsh tools/research/preset-check.ps1 -Part files
pwsh tools/DevRemote/session.ps1 -End ptab10
```

Expected: `files`의 줄 모두 PASS, `FAILS=0`. `-End`가 "PRESETS RESTORED".

"the defaults have no unusable line"이 실패하면 `pcheck`의 ` | ` 뒤에 어느 줄이 왜 안 되는지 나온다. 초안의 번호가 그 칸의 기술이 아닌 경우이므로 `pfind`로 쓰던 것과 같이 `SkillCatalog`의 분류를 확인해 초안을 고친다(추측으로 번호를 바꾸지 않는다).

- [ ] **Step 7: 커밋**

```bash
git add src/ModCode/ModMain/Services/PresetService.cs src/ModCode/ModMain/Presets src/ModCode/ModMain/ModMain.csproj src/ModCode/ModMain/Dev/DevCommandsPreset.cs tools/research/preset-check.ps1
git commit -m "feat: 프리셋 파일 읽기와 줄 검사, DLL 안의 기본 프리셋, 단계와 상태

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: 적용 — 단계, 지금 경지까지, 최종 단계까지

**Files:**
- Create: `src/ModCode/ModMain/Services/PresetService.Apply.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPreset.cs`(명령 `papply`, `preplaces`, `pfatepick`, `pbornpick`)
- Modify: `tools/research/preset-check.ps1`(함수 `NewTestPreset`, `SavePicked`, `LoadPicked`, 부분 `apply`, `apply-full`, `apply-battle`, `apply-restart`)

**Interfaces:**
- Consumes: Task 5의 형식과 `Snapshot`·`Find`·`FateNow`·`StageName`·`Named`, Task 3의 `MartialService.Make`·`SetSuit`·`SuitOf`·`EquippedIn`·`EquippedAbility`, 기존 `MartialService.Equip(WorldUnitBase, string) : string`·`Unequip`·`IsEquipped`·`Martial`, Task 4의 서비스 셋, Task 2의 `GradeService.RaiseTo`·`FirstRowOf`·`LastRow`, `LuckService.AddBorn(WorldUnitBase, int) : string`, `LuckService.Has`, `LuckService.Candidates(int type, string query)`, `FateService.Set(WorldUnitBase, int grade, int newId) : string`, `FateService.Candidates(int grade, string query)`, `EditService.SetAttribute(WorldUnitBase, AttributeDef, int) : string`, `AttributeCatalog.All`(`Key`가 `abilityPoint`인 것의 `Get`·`DynValue`).
- Produces(모두 `PresetService`):
  - `ApplyStage(Preset, int grade) : string`, `ApplyAll(Preset) : string`(지금 대경지까지), `ApplyFull(Preset) : string`(경지를 마지막 줄까지 올리고 `ApplyAll`), `RaiseToStage(int grade) : string`.
  - `ReplacesStage(Preset, int grade) : string`, `ReplacesAll(Preset) : string` — 바꾸게 될 것의 설명("역천개명 2개, 명우(주작)"). 없으면 null.
  - `LastRun : string` — 마지막 적용의 `done= skipped= failed=`. 거부됐으면 빈 글.
  - 개발 명령 `papply <파일 이름> <대경지|all|full>` → `done= skipped= failed= | <문장>` 또는 `refused=True | <문장>`, `preplaces <파일 이름> <대경지|all>` → `replaces=<yes|no> | <설명>`, `pfatepick <대경지> [빼낼 번호,…]` → `id=<번호|0>`, `pbornpick` → `id=<번호|0>`.

- [ ] **Step 1: 검사를 먼저 쓴다**

`preset-check.ps1`의 함수들(`PresetIndex` 아래)에 더한다.

```powershell
$pickedFile = Join-Path $work 'check-preset-picked.json'
$stateFile = Join-Path $work 'check-preset-state.txt'
function SavePicked($picked) { [IO.File]::WriteAllText($pickedFile, ($picked | ConvertTo-Json -Depth 4)) }
function LoadPicked { Get-Content -LiteralPath $pickedFile -Raw | ConvertFrom-Json -AsHashtable }
# 검사용 프리셋(uicheck-preset.json)을 만든다. 옵션과 역천개명·선천운명은 게임의 표에서 뽑는다(번호를 추측하지 않는다).
# 창 계열 기술 넷(23116은 경지 3부터), 과룡문(10016) 세트 여섯과 세트 밖의 심법 하나, 대경지 2~10의 역천개명, 법보·기령·신혼·명우.
# 돌려주는 것: opts(기술 번호 → "번호,…"), fates(대경지 → 번호), born(번호). 열쇠는 모두 글자다.
function NewTestPreset {
  $picked = @{ opts = @{}; fates = @{}; born = '0' }
  foreach ($id in 13111, 23116, 510021, 35111, 81001, 83001, 84001, 86002, 87002, 88002, 82001) {
    $picked.opts["$id"] = (Fields (Cmd "poptpick $id 3" 200)).opts
  }
  $taken = @()
  foreach ($major in 2..10) {
    $fate = (Fields (Cmd "pfatepick $major $($taken -join ',')" 200)).id
    if (-not $fate -or $fate -eq '0') { throw "대경지 $major 의 역천개명 후보를 찾지 못했습니다" }
    $picked.fates["$major"] = $fate
    $taken += $fate
  }
  $picked.born = (Fields (Cmd 'pbornpick' 200)).id
  if (-not $picked.born -or $picked.born -eq '0') { throw '선천운명 후보를 찾지 못했습니다' }
  $skills = @(
    ('{{ "slot": "left", "baseId": 13111, "options": [{0}] }}' -f $picked.opts['13111']),
    ('{{ "slot": "right", "baseId": 23116, "options": [{0}] }}' -f $picked.opts['23116']),
    ('{{ "slot": "step", "baseId": 510021, "options": [{0}] }}' -f $picked.opts['510021']),
    ('{{ "slot": "ultimate", "baseId": 35111, "options": [{0}] }}' -f $picked.opts['35111'])) -join ', '
  $abilities = @(foreach ($id in 81001, 83001, 84001, 86002, 87002, 88002) { '{{ "baseId": {0}, "options": [{1}], "suit": 10016 }}' -f $id, $picked.opts["$id"] })
  $abilities += ('{{ "baseId": 82001, "options": [{0}] }}' -f $picked.opts['82001'])
  $fates = @(foreach ($major in 2..10) { '{{ "grade": {0}, "id": {1} }}' -f $major, $picked.fates["$major"] }) -join ', '
  $json = '{ "format": 1, "kind": "preset", "name": "uicheck preset", "summary": "check", "source": "check", ' +
    '"bornLucks": [ { "id": ' + $picked.born + ' } ], "skills": [ ' + $skills + ' ], "abilities": [ ' + ($abilities -join ', ') + ' ], ' +
    '"fates": [ ' + $fates + ' ], "sprite": { "id": 1002, "artifact": 3031101 }, "elder": { "id": 1 }, ' +
    '"wingman": { "id": 1004, "effects": [100101, 100102], "prefixes": [100401, 100402] } }'
  WritePreset 'uicheck-preset.json' $json
  $picked
}
# pmartial의 출력에서 그 기술의 사본들(줄마다 "sole=… g=… …").
function Copies([int]$baseId) { @((Cmd "pmartial $baseId" 200) -split ' \| ' | Where-Object { $_ -match '^sole=' }) }
```

`'files' { … }` 아래에 부분 넷을 더한다.

```powershell
  'apply' {
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    $picked = NewTestPreset
    SavePicked $picked
    $file = 'uicheck-preset.json'
    Check 'the test preset has no unusable line' ((Fields (Cmd "pcheck $file" 600)).problems -eq '0') (Cmd "pcheck $file" 600)
    $s0 = State
    $soles0 = Soles
    $money0 = if ((Cmd 'read' 150) -match 'money=(\d+)') { $Matches[1] } else { throw 'read의 출력에서 money를 찾지 못했습니다' }

    # 경지 미달인 단계는 거부한다(고정본은 대경지 3이다).
    $r = Cmd "papply $file 7" 800
    Check 'a locked stage is refused' ($r -match 'refused=True' -and (State).martials -eq $s0.martials) $r

    # 단계 3: 고정본의 역천개명 3(700040)을 바꾸게 되므로 미리 알린다.
    Check 'stage 3 would replace a fate' ((Fields (Cmd "preplaces $file 3" 300)).replaces -eq 'yes') (Cmd "preplaces $file 3" 300)
    $r = Cmd "papply $file 3" 4000
    $run = Fields $r
    $s = State
    Check 'nothing failed' ($run.failed -eq '0') $r
    Check 'four skills and seven abilities were made' ([int]$s.martials -eq [int]$s0.martials + 11) "martials $($s0.martials) -> $($s.martials)"
    Check 'the slots hold the preset skills' ($s.left -eq '13111' -and $s.right -eq '23116' -and $s.step -eq '510021' -and $s.ult -eq '35111') "$($s.left) $($s.right) $($s.step) $($s.ult)"
    $abilities = (@($s.abilities -split ',') | Sort-Object) -join ','
    Check 'the preset abilities are equipped with the suit, and the ability of another type stays' ($abilities -eq '81001:10016,82001:0,83001:10016,84001:10016,85001:0,86002:10016,87002:10016,88002:10016') $abilities
    Check 'ability points cover what is in use' ([int]$s.used -le [int]$s.ap -and [int]$s.ap -gt 62) "used=$($s.used) ap=$($s.ap)"
    Check 'fate 3 is the preset one and fate 2 is untouched' ($s.fates -eq "2:700016,3:$($picked.fates['3'])") $s.fates
    $soles1 = Soles
    Check 'everything learned before is still there' (@($soles0 | Where-Object { $soles1 -notcontains $_ }).Count -eq 0)
    $made = @(Copies 13111 | Where-Object { $_ -match '^sole=(\S+) ' -and $soles0 -notcontains $Matches[1] })
    Check 'the made skill is grade 3, equipped, with the options asked for at max quality' ($made.Count -eq 1 -and $made[0] -match " g=3 eq=True .* nums=$([regex]::Escape($picked.opts['13111'])) atmax=True") ($made -join ' ; ')
    Check 'the artifact and sprite are applied from stage 2 up' ($s.artifact -eq '3031101' -and $s.sprite -eq '1002' -and $s.talents -eq '49/49') "artifact=$($s.artifact) sprite=$($s.sprite) talents=$($s.talents)"
    Check 'the elder and wingman of later stages are not applied' ($s.elder -eq '0' -and $s.wing -eq '0')
    Check 'money is unchanged' ((Cmd 'read' 150) -match "money=$money0\b")
    Check 'stage 3 reads as applied' ((Fields (Cmd "pstage $file 3" 400)).state -eq 'Applied') (Cmd "pstage $file 3" 400)
    $log = Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 1 -Encoding UTF8
    Check 'the change log has the preset line' ($log -match 'uicheck preset') $log

    # 같은 단계를 다시: 공법이 늘지 않고 수도 점수도 다시 오르지 않는다.
    $r = Cmd "papply $file 3" 4000
    $run = Fields $r
    $s2 = State
    Check 'applying again adds nothing' ($run.done -eq '0' -and $run.failed -eq '0' -and $s2.martials -eq $s.martials -and $s2.ap -eq $s.ap -and $s2.sprites -eq $s.sprites) $r
    Check 'there is nothing left to replace' ((Fields (Cmd "preplaces $file 3" 300)).replaces -eq 'no')

    # 단계 1: 선천운명. 공법은 이미 있다(경지 3은 1 이상). 23116은 경지 3부터의 기술이라 이 단계에는 없다.
    $r = Cmd "papply $file 1" 3000
    $s3 = State
    Check 'stage 1 adds the born luck and makes no skill' ([int]$s3.born -eq [int]$s2.born + 1 -and $s3.martials -eq $s2.martials) $r
    # 단계 2: 역천개명 2만 바뀐다.
    $r = Cmd "papply $file 2" 3000
    $s4 = State
    Check 'stage 2 sets fate 2 and leaves fate 3' ($s4.fates -eq "2:$($picked.fates['2']),3:$($picked.fates['3'])" -and $s4.martials -eq $s2.martials) $s4.fates

    # 틀린 줄이 섞인 프리셋: 그 줄만 못 넣고 나머지는 적용된다.
    WritePreset 'uicheck-mixed.json' '{ "format": 1, "kind": "preset", "name": "uicheck mixed", "skills": [ { "slot": "left", "baseId": 999999, "options": [] }, { "slot": "ultimate", "baseId": 34111, "options": [] } ] }'
    $r = Cmd 'papply uicheck-mixed.json 3' 3000
    $run = Fields $r
    Check 'a bad line does not stop the rest' ($run.failed -eq '1' -and $run.done -eq '1' -and (State).ult -eq '34111' -and (State).left -eq '13111') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'apply-full' {
    # apply 뒤에 같은 회차에서 돌린다. 경지를 마지막 줄까지 올리고 전부 적용한 뒤 저장한다(apply-restart가 비교한다).
    $picked = LoadPicked
    $file = 'uicheck-preset.json'
    $s0 = State
    $copies0 = (Copies 13111).Count
    $r = Cmd "papply $file full" 9000
    $run = Fields $r
    $s = State
    Check 'nothing failed' ($run.failed -eq '0') $r
    Check 'the grade is the last row' ($s.grade -eq '44' -and $s.major -eq '10') "grade=$($s.grade)"
    Check 'attack grew with the grade' ([int]$s.attack -gt [int]$s0.attack) "$($s0.attack) -> $($s.attack)"
    $wantFates = (2..10 | ForEach-Object { "$($_):$($picked.fates["$_"])" }) -join ','
    Check 'the fates of every major grade are the preset ones' ($s.fates -eq $wantFates) $s.fates
    Check 'the elder is at its top level for major grade 10' ($s.elder -eq '1' -and $s.elderlv -eq '9') "elder=$($s.elder) lv=$($s.elderlv)"
    Check 'the wingman and its lists are the preset ones' ($s.wing -eq '1004' -and $s.effects -eq '100101,100102' -and $s.prefixes -eq '100401,100402') "wing=$($s.wing) $($s.effects) $($s.prefixes)"
    $copies = Copies 13111
    Check 'one grade 10 copy was made, not one per stage' ($copies.Count -eq $copies0 + 1 -and @($copies | Where-Object { $_ -match ' g=10 eq=True ' }).Count -eq 1) ($copies -join ' ; ')
    Check 'the slots hold the preset skills' ($s.left -eq '13111' -and $s.right -eq '23116' -and $s.step -eq '510021' -and $s.ult -eq '35111') "$($s.left) $($s.right) $($s.step) $($s.ult)"
    Check 'ability points cover what is in use' ([int]$s.used -le [int]$s.ap) "used=$($s.used) ap=$($s.ap)"
    Check 'stage 10 reads as applied' ((Fields (Cmd "pstage $file 10" 400)).state -eq 'Applied') (Cmd "pstage $file 10" 400)
    Cmd 'save' 2500 | Out-Null
    [IO.File]::WriteAllText($stateFile, (Cmd 'pstate' 200))
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'apply-battle' {
    # apply-full 뒤에 돌린다. 세트와 명우가 전투에서 실제로 켜지는지 본다. 법보는 도구 칸의 아이콘이라 캡처로 본다.
    Into 20
    Start-Sleep -Seconds 2
    $o = Cmd 'pbattle' 200
    $b = Fields $o
    Check 'the battle is entered' ($b.ContainsKey('suits')) $o
    Check 'the six-piece suit is active' ($b.suits -match '(^|,)10016:6(,|$)') $o
    Check 'the wingman is in the battle' ($b.wing -eq '1004' -and [int]$b.wingobjs -ge 1) $o
    Shot 'preset-apply-battle'
    Leave
    Check 'back on the map' ((Cmd 'uilist' 200) -match 'MapMain')
  }
  'apply-restart' {
    # apply-full(끝에서 저장한다) 뒤에 session.ps1 -Restart를 하고 돌린다.
    $before = Fields ([IO.File]::ReadAllText($stateFile))
    $s = State
    foreach ($k in 'grade', 'attack', 'ap', 'left', 'right', 'step', 'ult', 'artifact', 'sprite', 'talents', 'elder', 'elderlv', 'wing', 'effects', 'prefixes', 'fates') {
      Check "$k is kept after a restart" ($s[$k] -eq $before[$k]) "have $($s[$k]) want $($before[$k])"
    }
    $now = (@($s.abilities -split ',') | Sort-Object) -join ','
    $then = (@($before.abilities -split ',') | Sort-Object) -join ','
    Check 'the abilities and their suit numbers are kept' ($now -eq $then) "have $now want $then"
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab11 -Fixture base
pwsh tools/research/preset-check.ps1 -Part apply
pwsh tools/DevRemote/session.ps1 -End ptab11
```

Expected: `NewTestPreset`이 "역천개명 후보를 찾지 못했습니다"로 멈춘다(`pfatepick`이 `UNKNOWN`). 종료 코드가 0이 아니다. `-End`는 정상으로 끝난다.

- [ ] **Step 3: `Services/PresetService.Apply.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 프리셋의 적용. 줄 하나를 넣는 함수가 넣음·건너뜀·못 넣음 가운데 하나를 돌려주고 구역마다 그것을 센다(CharacterExportService와 같은 방식).
    // 판정은 기존 서비스가 돌려준 문장이 아니라 넣은 뒤의 상태로 한다.
    internal static partial class PresetService
    {
        private enum Outcome { Done, Skipped, Failed }

        private delegate Outcome Put(out string text);

        // 한 구역의 집계. "공법: 장착 3, 건너뜀 1, 못 넣음 1(이름: 이유)." 같은 문장이 된다.
        private sealed class Tally
        {
            public int Done, Skipped;
            public readonly List<string> Failed = new List<string>();

            public bool Any
            {
                get { return Done + Skipped + Failed.Count > 0; }
            }

            public void Count(string label, Put put)
            {
                Outcome outcome;
                string text;
                try
                {
                    outcome = put(out text);
                }
                catch (Exception e)
                {
                    outcome = Outcome.Failed;
                    text = label + ": 게임이 받지 않았습니다(" + e.GetType().Name + ")";
                }
                if (outcome == Outcome.Done)
                {
                    Done++;
                }
                else if (outcome == Outcome.Skipped)
                {
                    Skipped++;
                }
                else
                {
                    Failed.Add(string.IsNullOrEmpty(text) ? label : text.TrimEnd('.'));
                }
            }

            public string Sentence(string title, string verb)
            {
                var sb = new StringBuilder(title + ": " + verb + " " + Done + ", 건너뜀 " + Skipped);
                if (Failed.Count > 0)
                {
                    sb.Append(", 못 넣음 ").Append(Failed.Count).Append("(");
                    for (int i = 0; i < Failed.Count && i < 5; i++)
                    {
                        sb.Append(i == 0 ? "" : " / ").Append(Failed[i]);
                    }
                    sb.Append(Failed.Count > 5 ? " 외 " + (Failed.Count - 5) + "개)" : ")");
                }
                return sb.Append(".").ToString();
            }
        }

        // 한 번의 적용에서 수도 점수를 올렸으면 처음 값과 마지막 값.
        private sealed class Points
        {
            public int Before = -1;
            public int After;
        }

        // 마지막 적용의 넣음·건너뜀·못 넣음의 합("done= skipped= failed="). 거부됐으면 빈 글. 검증이 읽는다.
        public static string LastRun = "";

        // ---- 줄 하나 넣기 ----

        private static bool IsBorn(WorldUnitBase me, int id)
        {
            var born = me.data.unitData.propertyData.bornLuck;
            for (int i = 0; i < born.Length; i++)
            {
                if (born[i].id == id)
                {
                    return true;
                }
            }
            return false;
        }

        private static Outcome PutBorn(WorldUnitBase me, LuckLine line, out string text)
        {
            string label = Named(line.Name, line.Id);
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            // 없는 것만 더한다. 후천 목록에 있는 것을 선천으로 옮기지도 않는다(캐릭터 가져오기와 같다).
            if (LuckService.Has(me, line.Id))
            {
                text = label;
                return Outcome.Skipped;
            }
            text = LuckService.AddBorn(me, line.Id);
            return IsBorn(me, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutFate(WorldUnitBase me, FateLine line, out string text)
        {
            string label = Named(line.Name, line.Id);
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (FateNow(me, line.Grade) == line.Id)
            {
                text = label;
                return Outcome.Skipped;
            }
            // 다른 대경지의 기록은 FateService.Set이 지킨다(docs/05 30·31절).
            text = FateService.Set(me, line.Grade, line.Id);
            return FateNow(me, line.Grade) == line.Id && LuckService.Has(me, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        // 이 심법을 장착하기에 수도 점수가 모자라면 모자라지 않을 만큼만 올린다(게임의 거부 코드 61).
        private static void RaisePoints(WorldUnitBase me, string sole, Points points)
        {
            AttributeDef def = AttributeCatalog.All.Find(d => d.Key == "abilityPoint");
            var saved = me.data.unitData.propertyData;
            int cost = MartialService.Martial(me, sole).martialInfo.abilityPoint;
            int used = me.data.dynUnitData.GetCurUseAbilityPoint();
            int total = def.DynValue(me.data.dynUnitData);
            if (used + cost <= total)
            {
                return;
            }
            int before = def.Get(saved);
            EditService.SetAttribute(me, def, before + (used + cost - total));
            if (points.Before < 0)
            {
                points.Before = before;
            }
            points.After = def.Get(saved);
        }

        // 공법·심법 한 줄: 같은 것이 없으면 그 경지로 만들고, 칸을 비운 뒤 장착한다.
        private static Outcome PutSkill(WorldUnitBase me, SkillLine line, int grade, Points points, out string text)
        {
            string label = Named(line.Name, line.BaseId);
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (grade < line.GradeMin || grade > line.GradeMax)
            {
                text = label;
                return Outcome.Skipped;
            }
            var data = me.data.unitData;
            Learned same = Find(Snapshot(), line, grade);
            string sole = same == null ? null : same.Sole;
            bool made = false;
            if (sole == null)
            {
                string error = MartialService.Make(me, line.Type, line.BaseId, grade, line.Options, out sole);
                if (error != null)
                {
                    text = label + ": " + error;
                    return Outcome.Failed;
                }
                made = true;
                if (line.Type == MartialType.Ability && line.Suit != 0 && !MartialService.SetSuit(data.GetActionMartial(sole).data, line.Suit))
                {
                    data.DelActionMartial(sole);
                    text = label + ": 세트 번호를 쓰지 못해 넣지 않았습니다";
                    return Outcome.Failed;
                }
            }
            if (MartialService.IsEquipped(me, sole))
            {
                text = label;
                return made ? Outcome.Done : Outcome.Skipped;
            }
            // 칸을 비운다. 게임은 찬 칸(코드 3)과 같은 유형의 심법이 장착된 것(코드 63)을 거부한다(실측).
            string other = line.Type == MartialType.Ability ? MartialService.EquippedAbility(me, line.AbilityType) : MartialService.EquippedIn(me, line.Type);
            if (other != null)
            {
                MartialService.Unequip(me, other);
            }
            if (line.Type == MartialType.Ability)
            {
                RaisePoints(me, sole, points);
            }
            string result = MartialService.Equip(me, sole);
            if (!MartialService.IsEquipped(me, sole))
            {
                // 장착하지 못했다. 비운 칸을 빈 채로 두지 않는다.
                if (other != null)
                {
                    MartialService.Equip(me, other);
                }
                text = result;
                return Outcome.Failed;
            }
            text = label;
            return Outcome.Done;
        }

        private static Outcome PutSprite(WorldUnitBase me, SpriteLine line, out string text)
        {
            if (line.Problem != null)
            {
                text = "법보와 기령: " + line.Problem;
                return Outcome.Failed;
            }
            if (SpriteService.Has(me, line.Id, line.ArtifactId))
            {
                text = "법보와 기령";
                return Outcome.Skipped;
            }
            bool changed;
            text = SpriteService.Apply(me, line.Id, line.ArtifactId, out changed);
            return SpriteService.Has(me, line.Id, line.ArtifactId) ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutElder(WorldUnitBase me, ElderLine line, out string text)
        {
            if (line.Problem != null)
            {
                text = "신혼: " + line.Problem;
                return Outcome.Failed;
            }
            if (ElderService.Has(me, line.Id))
            {
                text = "신혼";
                return Outcome.Skipped;
            }
            text = ElderService.Set(me, line.Id);
            return ElderService.Has(me, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutWing(WorldUnitBase me, WingLine line, out string text)
        {
            if (line.Problem != null)
            {
                text = "명우: " + line.Problem;
                return Outcome.Failed;
            }
            if (WingmanService.Has(me, line.Id, line.Effects, line.Prefixes))
            {
                text = "명우";
                return Outcome.Skipped;
            }
            text = WingmanService.Set(me, line.Id, line.Effects, line.Prefixes);
            return WingmanService.Has(me, line.Id, line.Effects, line.Prefixes) ? Outcome.Done : Outcome.Failed;
        }

        // ---- 적용 ----

        private static string Refusal(Preset preset)
        {
            if (preset.Error != null)
            {
                return preset.Name + ": 읽을 수 없는 파일이라 적용하지 않았습니다.";
            }
            if (!System.IO.File.Exists(preset.File))
            {
                return "프리셋: 파일이 없습니다(" + Path.GetFileName(preset.File) + "). 탭을 다시 여세요.";
            }
            return null;
        }

        // born: 선천운명을 넣는가. fateFrom~fateTo: 넣을 역천개명의 대경지 범위.
        // grade: 공법·심법을 만드는 경지이자, 법보(2)·신혼(7)·명우(9)를 넣을지 정하는 대경지.
        private static string Apply(Preset preset, string title, bool born, int fateFrom, int fateTo, int grade)
        {
            LastRun = "";
            string refusal = Refusal(preset);
            if (refusal != null)
            {
                return refusal;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            WorldUnitBase me = Me;
            var borns = new Tally();
            var fates = new Tally();
            var skills = new Tally();
            var abilities = new Tally();
            var extras = new Tally();
            var points = new Points();
            if (born)
            {
                foreach (LuckLine each in preset.BornLucks)
                {
                    LuckLine line = each;
                    borns.Count(Named(line.Name, line.Id), (out string text) => PutBorn(me, line, out text));
                }
            }
            foreach (FateLine each in preset.Fates)
            {
                FateLine line = each;
                if (line.Grade >= fateFrom && line.Grade <= fateTo)
                {
                    fates.Count(Named(line.Name, line.Id), (out string text) => PutFate(me, line, out text));
                }
            }
            foreach (SkillLine each in preset.Skills)
            {
                SkillLine line = each;
                skills.Count(Named(line.Name, line.BaseId), (out string text) => PutSkill(me, line, grade, points, out text));
            }
            foreach (SkillLine each in preset.Abilities)
            {
                SkillLine line = each;
                abilities.Count(Named(line.Name, line.BaseId), (out string text) => PutSkill(me, line, grade, points, out text));
            }
            if (preset.Sprite != null && grade >= SpriteService.FirstGrade)
            {
                extras.Count("법보와 기령", (out string text) => PutSprite(me, preset.Sprite, out text));
            }
            if (preset.Elder != null && grade >= ElderService.FirstGrade)
            {
                extras.Count("신혼", (out string text) => PutElder(me, preset.Elder, out text));
            }
            if (preset.Wingman != null && grade >= WingmanService.FirstGrade)
            {
                extras.Count("명우", (out string text) => PutWing(me, preset.Wingman, out text));
            }

            var sb = new StringBuilder(preset.Name + " — " + title + ":");
            if (borns.Any)
            {
                sb.Append(' ').Append(borns.Sentence("선천운명", "추가"));
            }
            if (fates.Any)
            {
                sb.Append(' ').Append(fates.Sentence("역천개명", "지정"));
            }
            if (skills.Any)
            {
                sb.Append(' ').Append(skills.Sentence("공법", "장착"));
            }
            if (abilities.Any)
            {
                sb.Append(' ').Append(abilities.Sentence("심법", "장착"));
            }
            if (points.Before >= 0)
            {
                sb.Append(" 수도 점수 ").Append(points.Before).Append(" -> ").Append(points.After).Append('.');
            }
            if (extras.Any)
            {
                sb.Append(' ').Append(extras.Sentence("법보·신혼·명우", "적용"));
            }
            int done = borns.Done + fates.Done + skills.Done + abilities.Done + extras.Done;
            int skipped = borns.Skipped + fates.Skipped + skills.Skipped + abilities.Skipped + extras.Skipped;
            int failed = borns.Failed.Count + fates.Failed.Count + skills.Failed.Count + abilities.Failed.Count + extras.Failed.Count;
            if (done + skipped + failed == 0)
            {
                sb.Append(" 이 범위에 적용할 줄이 없습니다.");
            }
            LastRun = "done=" + done + " skipped=" + skipped + " failed=" + failed;
            string result = sb.ToString();
            ChangeLog.Add(EditService.NameOf(me), "프리셋 " + preset.Name + " (" + title + ")", "-", result, failed == 0);
            return result;
        }

        // 단계 하나. 지금 대경지가 그 단계보다 낮으면 거부한다.
        public static string ApplyStage(Preset preset, int grade)
        {
            LastRun = "";
            if (grade < 1 || grade > StageCount)
            {
                return "프리셋: 없는 단계입니다(" + grade + ").";
            }
            if (FateService.CurrentGrade(Me) < grade)
            {
                return preset.Name + ": " + grade + " " + StageName(grade) + " 단계는 경지 미달입니다. 적용하지 않았습니다.";
            }
            return Apply(preset, grade + " " + StageName(grade), grade == 1, grade, grade, grade);
        }

        // 지금 대경지까지: 선천운명, 대경지 2~지금의 역천개명, 지금 대경지의 공법·심법, 지금 대경지가 허용하는 법보·신혼·명우.
        // 낮은 단계의 공법은 만들지 않는다(경지마다 사본이 쌓이지 않게).
        public static string ApplyAll(Preset preset)
        {
            int current = FateService.CurrentGrade(Me);
            return Apply(preset, "지금 경지(" + StageName(current) + ")까지", true, FateService.FirstGrade, current, current);
        }

        // 경지를 표의 마지막 줄(등선 후기)까지 올리고 전부 적용한다.
        public static string ApplyFull(Preset preset)
        {
            LastRun = "";
            string refusal = Refusal(preset);
            if (refusal != null)
            {
                return refusal;
            }
            ConfRoleGradeItem last = GradeService.LastRow();
            var saved = Me.data.unitData.propertyData;
            string raised = "";
            if (last != null && saved.gradeID != last.id)
            {
                raised = GradeService.RaiseTo(Me, last.id) + " ";
                if (saved.gradeID != last.id)
                {
                    // 올리지 못했으면 적용하지 않는다.
                    return raised.TrimEnd();
                }
            }
            return raised + ApplyAll(preset);
        }

        // 대경지 grade의 초기까지 경지를 올린다(적용은 하지 않는다).
        public static string RaiseToStage(int grade)
        {
            ConfRoleGradeItem row = GradeService.FirstRowOf(grade);
            return row == null ? "경지 올리기: 게임에 없는 대경지(" + grade + ")입니다." : GradeService.RaiseTo(Me, row.id);
        }

        // ---- 바꾸게 될 것 ----

        // 역천개명·신혼·명우는 하나만 가질 수 있어, 지금 것이 프리셋과 다르면 바꾸게 된다. 그 설명을 돌려준다(없으면 null).
        // 화면은 이것이 있을 때만 두 번 눌러 확인을 묻는다.
        private static string Replaces(Preset preset, int fateFrom, int fateTo, int grade)
        {
            if (preset.Error != null)
            {
                return null;
            }
            WorldUnitBase me = Me;
            var parts = new List<string>();
            Dictionary<int, int> records = FateService.Records(me);
            int fates = 0;
            foreach (FateLine line in preset.Fates)
            {
                int record;
                if (line.Problem == null && line.Grade >= fateFrom && line.Grade <= fateTo
                    && records.TryGetValue(line.Grade, out record) && record != 0 && record != line.Id)
                {
                    fates++;
                }
            }
            if (fates > 0)
            {
                parts.Add("역천개명 " + fates + "개");
            }
            if (preset.Elder != null && preset.Elder.Problem == null && grade >= ElderService.FirstGrade)
            {
                int elder = ElderService.Current(me);
                if (elder != 0 && elder != preset.Elder.Id)
                {
                    parts.Add("신혼(" + ElderService.Name(elder) + ")");
                }
            }
            if (preset.Wingman != null && preset.Wingman.Problem == null && grade >= WingmanService.FirstGrade)
            {
                int wing = WingmanService.Current(me);
                if (wing != 0 && !WingmanService.Has(me, preset.Wingman.Id, preset.Wingman.Effects, preset.Wingman.Prefixes))
                {
                    parts.Add("명우(" + WingmanService.Name(wing) + ")");
                }
            }
            return parts.Count == 0 ? null : string.Join(", ", parts.ToArray());
        }

        public static string ReplacesStage(Preset preset, int grade)
        {
            return Replaces(preset, grade, grade, grade);
        }

        public static string ReplacesAll(Preset preset)
        {
            int current = FateService.CurrentGrade(Me);
            return Replaces(preset, FateService.FirstGrade, current, current);
        }
    }
}
```

- [ ] **Step 4: 개발 명령을 더한다**

`Dev/DevCommandsPreset.cs`의 `switch`에 넣는다.

```csharp
                case "papply": return ApplyPreset(p[1], p[2]);
                case "preplaces": return ReplacesInfo(p[1], p[2]);
                case "pfatepick": return FatePick(me, int.Parse(p[1]), p.Length > 2 ? p[2] : "");
                case "pbornpick": return BornPick(me);
```

메서드를 더한다.

```csharp
        private static string ApplyPreset(string fileName, string what)
        {
            PresetService.Preset preset = PresetNamed(fileName);
            if (preset == null)
            {
                return "refused=True | no preset " + fileName;
            }
            int grade;
            string result = what == "all" ? PresetService.ApplyAll(preset)
                : what == "full" ? PresetService.ApplyFull(preset)
                : int.TryParse(what, out grade) ? PresetService.ApplyStage(preset, grade) : "unknown " + what;
            return (PresetService.LastRun.Length == 0 ? "refused=True" : PresetService.LastRun) + " | " + result;
        }

        private static string ReplacesInfo(string fileName, string what)
        {
            PresetService.Preset preset = PresetNamed(fileName);
            if (preset == null)
            {
                return "no preset " + fileName;
            }
            string replaces = what == "all" ? PresetService.ReplacesAll(preset) : PresetService.ReplacesStage(preset, int.Parse(what));
            return "replaces=" + (replaces == null ? "no" : "yes") + " | " + replaces;
        }

        // 그 대경지에서 고를 수 있는 역천개명 가운데 가지고 있지 않고 빼낼 목록에도 없는 첫 번째. 검사가 프리셋 파일을 만들 때 쓴다.
        private static string FatePick(WorldUnitBase me, int grade, string exclude)
        {
            var skip = new List<int>(Ints(exclude));
            foreach (LuckService.LuckView view in FateService.Candidates(grade, ""))
            {
                if (!LuckService.Has(me, view.Id) && !skip.Contains(view.Id))
                {
                    return "id=" + view.Id;
                }
            }
            return "id=0";
        }

        // 가지고 있지 않은 선천 분류(1)의 운명 가운데 첫 번째.
        private static string BornPick(WorldUnitBase me)
        {
            foreach (LuckService.LuckView view in LuckService.Candidates(1, ""))
            {
                if (!LuckService.Has(me, view.Id) && !LuckService.IsSystem(view.Id))
                {
                    return "id=" + view.Id;
                }
            }
            return "id=0";
        }
```

- [ ] **Step 5: 빌드하고 검사가 통과하는 것을 본다**

공통 절차의 빌드 두 줄. Expected: 오류 0.

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab12 -Fixture base
pwsh tools/research/preset-check.ps1 -Part apply
pwsh tools/research/preset-check.ps1 -Part apply-full
pwsh tools/research/preset-check.ps1 -Part apply-battle
pwsh tools/DevRemote/session.ps1 -Restart
pwsh tools/research/preset-check.ps1 -Part apply-restart
pwsh tools/DevRemote/session.ps1 -End ptab12
```

Expected: 네 부분 모두 `FAILS=0`. `apply-battle`의 `SHOT` 파일(`.devremote\check-preset-apply-battle.png`)을 열어 도구 칸에 법보 아이콘이 있는지 본다. `-End`가 "SAVE RESTORED AND IDENTICAL"과 "PRESETS RESTORED".

실패했을 때 보는 곳:

- "four skills and seven abilities were made"의 수가 다르면 `papply`의 ` | ` 뒤 문장에서 "못 넣음"의 이유를 읽는다. 장착 거부면 코드가 적혀 있다(3: 칸이 참, 63: 같은 유형의 심법, 61: 수도 점수).
- "the six-piece suit is active"가 실패하면 `pstate`의 `abilities`에서 세트 번호가 `10016`인 것이 여섯인지 본다. 여섯인데 전투에서 켜지지 않으면 스펙 3절의 실측과 다른 것이므로 멈추고 원인을 찾는다(추측으로 고치지 않는다).
- "the elder is at its top level"의 단계가 다르면 `ElderService.TopRow`가 고른 줄의 `exp`와 `elderData.exp`를 비교한다.

- [ ] **Step 6: 커밋**

```bash
git add src/ModCode/ModMain/Services/PresetService.Apply.cs src/ModCode/ModMain/Dev/DevCommandsPreset.cs tools/research/preset-check.ps1
git commit -m "feat: 프리셋 적용 — 단계, 지금 경지까지, 최종 단계까지

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: 화면 — 프리셋 탭

**Files:**
- Create: `src/ModCode/ModMain/UI/Tabs/PresetTab.cs`
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs:436,445-449`
- Modify: `tools/research/ui-check.ps1`(`$tabProbe`, 탭 수, 안전 탭의 이름)
- Modify: `tools/research/preset-check.ps1`(부분 `ui`)

**Interfaces:**
- Consumes: `EditorTab`(`content`, `ContentW`, `ContentH`, `SubTop`, `Push(Func<string>, Action)`, `Say(string)`, `Player`, `RowFor(float, params float[])`, `OnShow`, `OnOpen`), `ListView<T>`(`Build`, `Reset`, `HeightOf`, `EmptyText`), `Confirm.Button(name, parent, label, confirmLabel, x, y, w, h, key, message, action)`, `Ui.Button`·`Label`·`Wrapped`·`Panel`·`TextHeight`·`ScrollBarWidth`·`Dim`·`Warn`·`Section`, `EditService.GradeName(int)`, Task 5·6의 `PresetService`.
- Produces: 탭 `프리셋`(`Tab11`). 요소 이름 `PresetRestore`, `PresetNote`, `PresetProblems`, `PresetDetail<순번>`, `PresetApply<순번>`, `PresetFull<순번>`, `PresetState<순번>`, `StageApply<대경지>`, `StageRaise<대경지>`, `StageState<대경지>`. 안전 탭은 `Tab12`.

- [ ] **Step 1: 기존 검사의 탭 번호를 고친다**

`tools/research/ui-check.ps1`에서:

1. `$tabProbe` 줄을 바꾼다(배율의 `God` 뒤에 `PresetRestore`).

```powershell
$tabProbe = @('Restore', 'MoneyAdd', 'CurrencyAdd10001', 'VaultModeBag', 'FillExp', 'SkillAddOpen', 'LuckAddOpen', 'ArtLearnAll', 'NpcRefresh', 'Freeze', 'God', 'PresetRestore', 'BackupNow')
```

2. 탭 수를 세는 `for ($i = 0; $i -lt 12; $i++)`를 모두 `for ($i = 0; $i -lt 13; $i++)`로 바꾼다.

```bash
grep -n "\-lt 12; \$i++" tools/research/ui-check.ps1
```

3. 안전 탭을 `Tab11`로 여는 줄을 `Tab12`로 바꾼다. 찾은 줄마다 그 뒤에 누르는 요소가 안전 탭의 것(`BackupNow`, `History…`)인지 확인하고 바꾼다.

```bash
grep -n "Tab11" tools/research/ui-check.ps1 tools/research/datebag-check.ps1
```

Expected: `ui-check.ps1`에 세 줄(`history`, `wrap`, `rates` 부분). `datebag-check.ps1`에는 없다.

- [ ] **Step 2: 화면의 검사를 쓴다**

`preset-check.ps1`의 `'apply-restart' { … }` 아래에 넣는다.

```powershell
  'ui' {
    $traps = Traps
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    Cmd 'plist' 400 | Out-Null          # 폴더를 만들고 기본 프리셋을 풀게 한다(검사 파일을 먼저 쓰면 풀지 않는다)
    $picked = NewTestPreset
    $n = [int](Fields (Cmd 'pdefaults' 200)).embedded
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab12' | Out-Null
    Check 'the safety tab is the thirteenth' (IsAt 'BackupNow')
    Press 'Tab11' | Out-Null
    Check 'the preset tab is drawn' (IsAt 'PresetRestore')
    $shown = $true
    foreach ($i in 0..$n) { $shown = $shown -and (IsAt "PresetDetail$i") -and (IsAt "PresetApply$i") -and (IsAt "PresetFull$i") }
    Check 'every preset has its three buttons' $shown
    Check 'the note is empty while the defaults are untouched' ((Cmd 'uitext PresetNote' 150) -match "text=''")

    # 읽을 수 없는 파일은 줄만 있고 버튼이 없다. 탭에 다시 들어오면 폴더를 다시 읽는다.
    WritePreset 'uicheck-zz-broken.json' '{ "format": 1, '
    Press 'Tab12' | Out-Null; Press 'Tab11' | Out-Null
    $bad = PresetIndex 'uicheck-zz-broken.json'
    Check 'an unreadable file has no buttons' ($bad -ge 0 -and -not (IsAt "PresetApply$bad") -and -not (IsAt "PresetDetail$bad") -and (IsAt "PresetApply$(PresetIndex 'uicheck-preset.json')"))
    [IO.File]::Delete((Join-Path $presetDir 'uicheck-zz-broken.json'))

    # 기본 파일을 고치면 알림이 뜨고, 되돌리기는 두 번 눌러야 한다.
    $first = Get-ChildItem -LiteralPath $presetDir -Filter *.json | Where-Object { $_.Name -notlike 'uicheck-*' } | Sort-Object Name | Select-Object -First 1
    WritePreset $first.Name '{ "format": 1, "kind": "preset", "name": "uicheck changed" }'
    Press 'Tab12' | Out-Null; Press 'Tab11' | Out-Null
    Check 'the note reports one changed default' ((Cmd 'uitext PresetNote' 150) -match "text='[^']*1[^']*'") (Cmd 'uitext PresetNote' 150)
    Press 'PresetRestore' | Out-Null
    Check 'restore asks first' ((Fields (Cmd 'pdefaults' 200)).changed -eq '1')
    Press 'PresetRestore' | Out-Null
    Check 'the second press restores' ((Fields (Cmd 'pdefaults' 200)).changed -eq '0')

    # 상세보기: 결정 초기(대경지 3)에서 단계 1~3은 적용, 4는 경지 올리기.
    $idx = PresetIndex 'uicheck-preset.json'
    Press "PresetDetail$idx" | Out-Null
    Check 'stages 1 to 3 can be applied' ((IsAt 'StageApply1') -and (IsAt 'StageApply2') -and (IsAt 'StageApply3'))
    Check 'stage 4 is locked and offers the raise' ((IsAt 'StageRaise4') -and -not (IsAt 'StageApply4'))
    Check 'every shown stage has a state label' ((Cmd 'uitext StageState3' 150) -match "StageState3 text='[^']+'")

    # 단계 3은 고정본의 역천개명 3을 바꾸므로 먼저 묻는다.
    $m0 = (State).martials
    Press 'StageApply3' | Out-Null
    Check 'apply asks first when it would replace a fate' ((State).martials -eq $m0 -and (State).fates -eq '2:700016,3:700040')
    Press 'StageApply3' 4000 | Out-Null
    $s = State
    Check 'the second press applies the stage' ([int]$s.martials -eq [int]$m0 + 11 -and $s.fates -eq "2:700016,3:$($picked.fates['3'])" -and $s.left -eq '13111') "martials=$($s.martials) fates=$($s.fates)"
    Check 'stage 3 reads as applied' ((Fields (Cmd 'pstage uicheck-preset.json 3' 400)).state -eq 'Applied')
    # 단계 1은 바꿀 것이 없으므로 한 번에 적용된다.
    $born0 = [int]$s.born
    Press 'StageApply1' 3000 | Out-Null
    Check 'a stage that replaces nothing applies at once' ([int](State).born -eq $born0 + 1)
    # 경지 올리기는 두 번 눌러야 한다.
    Press 'StageRaise4' | Out-Null
    Check 'raise asks first' ((State).major -eq '3')
    Press 'StageRaise4' 3000 | Out-Null
    Check 'the second press raises to major grade 4' ((State).major -eq '4')
    Check 'stage 4 can now be applied' ((IsAt 'StageApply4') -and -not (IsAt 'StageRaise4'))

    # 목록의 적용: 대경지 2의 역천개명(700016)을 바꾸므로 먼저 묻는다. 지금 대경지(4)까지의 역천개명이 모두 정해진다.
    Press 'Back' | Out-Null
    Check 'back on the list' (IsAt 'PresetRestore')
    Press "PresetApply$idx" | Out-Null
    Check 'apply asks first' ((State).fates -match '^2:700016,')
    Press "PresetApply$idx" 5000 | Out-Null
    $s = State
    Check 'apply sets the fates up to the current major grade' ($s.fates -eq "2:$($picked.fates['2']),3:$($picked.fates['3']),4:$($picked.fates['4'])") $s.fates
    # 최종 단계까지: 두 번 눌러야 한다.
    Press "PresetFull$idx" | Out-Null
    Check 'the full apply asks first' ((State).major -eq '4')
    Press "PresetFull$idx" 9000 | Out-Null
    $s = State
    Check 'the second press raises to the last row and applies everything' ($s.grade -eq '44' -and $s.wing -eq '1004' -and $s.elder -eq '1') "grade=$($s.grade) wing=$($s.wing) elder=$($s.elder)"
    Check 'the message area shows the result' ((Cmd 'uistate' 150) -match "message='[^']*uicheck preset")
    Check 'no callback exception' ((Traps) -eq $traps)
    Check 'no mod warning' (@(ModWarnings).Count -eq 0) ((ModWarnings) -join ' | ')
  }
```

- [ ] **Step 3: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab13 -Fixture base
pwsh tools/research/preset-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End ptab13
```

Expected: `Press 'Tab12'`에서 "편집기에서 요소를 찾지 못했습니다: Tab12"로 멈춘다(탭이 아직 12개다).

- [ ] **Step 4: `UI/Tabs/PresetTab.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 프리셋 탭: 빌드 프리셋의 목록과 상세보기(단계별 적용). 적용은 항상 플레이어에 한다(편집 대상과 무관).
    // 근거: docs/superpowers/specs/2026-10-14-preset-tab-design.md 5.5절.
    internal sealed class PresetTab : EditorTab
    {
        private const float NameW = 230;
        private const float SummaryW = 420;
        private const float SourceW = 170;
        private const float StageTextX = 125;

        private readonly ListView<PresetService.Preset> list = new ListView<PresetService.Preset>("PresetList", 44);
        private readonly ListView<PresetService.Stage> stages = new ListView<PresetService.Stage>("PresetStages", 44);
        private List<PresetService.Preset> presets;         // null이면 폴더를 다시 읽는다
        private List<PresetService.Learned> learned;        // 한 번 그리는 동안 쓰는 익힌 공법의 사본
        private PresetService.Preset opened;

        public override string Title
        {
            get { return "프리셋"; }
        }

        // 탭에 들어올 때와 편집기를 열 때 폴더를 다시 읽는다(밖에서 파일을 고치거나 넣은 것이 반영된다).
        public override void OnShow()
        {
            presets = null;
        }

        public override void OnOpen()
        {
            presets = null;
        }

        private static string Unreadable(PresetService.Preset preset)
        {
            return "(읽을 수 없음) " + System.IO.Path.GetFileName(preset.File) + " — " + preset.Error;
        }

        protected override void Draw()
        {
            if (presets == null)
            {
                presets = PresetService.List();
                list.Reset();
            }
            learned = PresetService.Snapshot();
            Confirm.Button("PresetRestore", content, "기본 프리셋 되돌리기", "정말 되돌리기", 0, 0, 230, 38, "preset-restore",
                "기본 프리셋 " + PresetService.DefaultCount + "개를 다시 씁니다. 고친 기본 파일은 덮어쓰고, 직접 넣은 다른 파일은 그대로 둡니다. \"정말 되돌리기\"를 한 번 더 누르세요.",
                new Action(() =>
                {
                    string result = PresetService.RestoreDefaults();
                    presets = null;
                    Say(result);
                }));
            int changed = PresetService.DefaultsChanged();
            Text note = Ui.Label(content, changed > 0 ? "기본 프리셋 " + changed + "개가 없거나 고쳐졌습니다." : "", 244, 0, 480, 38, 16);
            note.color = Ui.Warn;
            note.gameObject.name = "PresetNote";
            Ui.Label(content, "지금 경지: " + EditService.GradeName(Player.data.unitData.propertyData.gradeID), ContentW - 330, 0, 330, 38, 18, TextAnchor.MiddleRight);
            Ui.Wrapped(content, "파일: " + PresetService.Dir + " — JSON 파일을 고치거나 넣으면 이 탭에 다시 들어올 때 반영됩니다. 적용은 항상 플레이어에 하고, 익힌 것을 지우지 않고 장착만 바꿉니다. "
                + "빌드는 2022년의 공략을 바탕으로 했고 지금 버전에서의 순위는 확인하지 못했습니다.", 0, 44, ContentW, 44, 14).color = Ui.Dim;

            Ui.Panel("PresetHead", content, 0, 94, ContentW, 30, Ui.Section);
            Ui.Label(content, "이름", 8, 94, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", 245, 94, SummaryW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "출처", 672, 94, SourceW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", 848, 94, 84, 30, 16).color = Ui.Dim;
            list.EmptyText = "프리셋 파일이 없습니다. 위의 \"기본 프리셋 되돌리기\"를 누르세요.";
            list.HeightOf = preset => preset.Error != null
                ? RowFor(44, Ui.TextHeight(Unreadable(preset), ContentW - Ui.ScrollBarWidth - 16, 16))
                : RowFor(44, Ui.TextHeight(preset.Name, NameW, 19), Ui.TextHeight(preset.Summary, SummaryW, 16), Ui.TextHeight(preset.Source, SourceW, 14));
            list.Build(content, 0, 126, ContentW, ContentH - 126, presets, DrawRow);
        }

        private void DrawRow(RectTransform row, PresetService.Preset preset, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            float h = row.sizeDelta.y;
            if (preset.Error != null)
            {
                Ui.Wrapped(row, Unreadable(preset), 8, 0, right - 16, h, 16).color = Ui.Warn;
                return;
            }
            Ui.Wrapped(row, preset.Name, 8, 0, NameW, h, 19);
            Ui.Wrapped(row, preset.Summary, 245, 0, SummaryW, h, 16);
            Ui.Wrapped(row, preset.Source, 672, 0, SourceW, h, 14).color = Ui.Dim;
            Text state = Ui.Label(row, PresetService.AppliedCount(preset, learned) + "/" + PresetService.StageCount + " 단계", 848, 0, 84, h, 16);
            state.gameObject.name = "PresetState" + index;
            Ui.Button("PresetDetail" + index, row, "상세보기", right - 356, 3, 100, 34, new Action(() =>
            {
                opened = preset;
                stages.Reset();
                Push(() => opened.Name + (opened.Summary.Length > 0 ? " — " + opened.Summary : "") + (opened.Source.Length > 0 ? " (출처: " + opened.Source + ")" : ""), DrawDetail);
            }));
            // 지금의 역천개명·신혼·명우를 바꾸게 될 때만 두 번 눌러 확인한다(스펙 4절).
            string replaces = PresetService.ReplacesAll(preset);
            if (replaces == null)
            {
                Ui.Button("PresetApply" + index, row, "적용", right - 250, 3, 90, 34, new Action(() => Say(PresetService.ApplyAll(preset))));
            }
            else
            {
                Confirm.Button("PresetApply" + index, row, "적용", "정말 적용", right - 250, 3, 90, 34, "preset-apply:" + preset.File,
                    preset.Name + ": 지금의 " + replaces + "을(를) 프리셋의 것으로 바꿉니다. \"정말 적용\"을 한 번 더 누르세요.",
                    new Action(() => Say(PresetService.ApplyAll(preset))));
            }
            Confirm.Button("PresetFull" + index, row, "최종 단계까지", "정말 적용", right - 154, 3, 150, 34, "preset-full:" + preset.File,
                preset.Name + ": 경지를 등선 후기까지 올린 뒤 모든 단계를 적용합니다. 게임의 돌파 절차를 거치지 않으며 능력치는 돌파 증가분만큼 오릅니다. \"정말 적용\"을 한 번 더 누르세요.",
                new Action(() => Say(PresetService.ApplyFull(preset))));
        }

        private void DrawDetail()
        {
            learned = PresetService.Snapshot();
            var all = new List<PresetService.Stage>();
            for (int grade = 1; grade <= PresetService.StageCount; grade++)
            {
                all.Add(PresetService.StageOf(opened, grade));
            }
            float right = ContentW - Ui.ScrollBarWidth;
            float textW = right - StageTextX - 400;
            // 쓸 수 없는 줄을 위에 모아 알린다. 대경지가 틀린 역천개명처럼 어느 단계에도 들지 않는 줄은 여기에만 나온다.
            float top = SubTop;
            List<string> problems = PresetService.Problems(opened);
            if (problems.Count > 0)
            {
                string warning = "쓸 수 없는 줄 " + problems.Count + "개(적용에서 빠집니다): " + string.Join(" / ", problems.ToArray());
                float warningH = Math.Max(30, (float)Math.Ceiling(Ui.TextHeight(warning, ContentW, 15)) + 6);
                Text shown = Ui.Wrapped(content, warning, 0, top, ContentW, warningH, 15);
                shown.color = Ui.Warn;
                shown.gameObject.name = "PresetProblems";
                top += warningH + 4;
            }
            Ui.Panel("StageHead", content, 0, top, ContentW, 30, Ui.Section);
            Ui.Label(content, "단계", 8, top, 110, 30, 16).color = Ui.Dim;
            Ui.Label(content, "구성", StageTextX, top, textW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", right - 390, top, 120, 30, 16).color = Ui.Dim;
            stages.HeightOf = stage => RowFor(44, Ui.TextHeight(PresetService.StageText(stage), textW, 16));
            stages.Build(content, 0, top + 32, ContentW, ContentH - top - 32, all, (row, stage, index) => DrawStage(row, stage, textW));
        }

        private void DrawStage(RectTransform row, PresetService.Stage stage, float textW)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            float h = row.sizeDelta.y;
            int grade = stage.Grade;
            Ui.Label(row, grade + " " + PresetService.StageName(grade), 8, 0, 110, h, 18);
            Ui.Wrapped(row, PresetService.StageText(stage), StageTextX, 0, textW, h, 16);
            int have, total;
            PresetService.StageState state = PresetService.StateOf(stage, learned, out have, out total);
            string label = state == PresetService.StageState.Locked ? "경지 미달"
                : state == PresetService.StageState.Applied ? "적용됨"
                : state == PresetService.StageState.Partial ? "일부(" + have + "/" + total + ")" : "적용 전";
            Text text = Ui.Label(row, label, right - 390, 0, 120, h, 16);
            text.gameObject.name = "StageState" + grade;
            if (state == PresetService.StageState.Locked)
            {
                text.color = Ui.Dim;
                Confirm.Button("StageRaise" + grade, row, "경지를 이 단계까지 올리기", "정말 올리기", right - 264, 3, 260, 34, "stage-raise:" + grade,
                    PresetService.StageName(grade) + " 초기까지 경지를 올립니다. 게임의 돌파 절차를 거치지 않으며 능력치는 돌파 증가분만큼 오릅니다. \"정말 올리기\"를 한 번 더 누르세요.",
                    new Action(() => Say(PresetService.RaiseToStage(grade))));
                return;
            }
            string replaces = PresetService.ReplacesStage(opened, grade);
            if (replaces == null)
            {
                Ui.Button("StageApply" + grade, row, "적용", right - 104, 3, 100, 34, new Action(() => Say(PresetService.ApplyStage(opened, grade))));
            }
            else
            {
                Confirm.Button("StageApply" + grade, row, "적용", "정말 적용", right - 104, 3, 100, 34, "stage-apply:" + opened.File + ":" + grade,
                    grade + " " + PresetService.StageName(grade) + ": 지금의 " + replaces + "을(를) 프리셋의 것으로 바꿉니다. \"정말 적용\"을 한 번 더 누르세요.",
                    new Action(() => Say(PresetService.ApplyStage(opened, grade))));
            }
        }
    }
}
```

- [ ] **Step 5: 탭을 등록한다**

`UI/EditorWindow.cs`에서 두 곳을 고친다.

탭 줄의 높이(436행 근처):

```csharp
            tabBar = Ui.Rect("Tabs", window, 16, ContentY, 150, 13 * 52);
```

탭 목록(445~449행):

```csharp
            tabs = new EditorTab[]
            {
                new AttributesTab(), new ResourcesTab(), new CurrencyTab(), new VaultTab(), new GradeTab(), new MartialTab(), new LuckTab(),
                new ArtTab(), new NpcTab(), new WorldTab(), new RateTab(), new PresetTab(), new SafetyTab(),
            };
```

탭 수 12를 전제로 한 곳이 더 있는지 찾는다.

```bash
grep -n "12 \* 52\|< 12\|== 11\|tabs.Length" src/ModCode/ModMain/UI/EditorWindow.cs
```

Expected: `12 * 52`는 남지 않는다. `tabs.Length`로 도는 곳은 고칠 것이 없다.

- [ ] **Step 6: 빌드하고 검사가 통과하는 것을 본다**

공통 절차의 빌드 두 줄. Expected: 오류 0.

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab14 -Fixture base
pwsh tools/research/preset-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End ptab14
```

Expected: `ui`의 줄 모두 PASS, `FAILS=0`.

`IsAt 'StageRaise4'`가 거짓이면 단계 4의 줄이 목록의 보이는 범위 밖일 수 있다(목록은 화면에 걸친 줄만 만든다). 캡처로 확인하고, 그렇다면 그 검사 앞에 `Press 'PresetStagesNext' | Out-Null`을 넣는다.

화면을 눈으로 한 번 본다: 같은 회차에서 `ui` 뒤에 캡처해 목록과 상세보기의 글이 잘리지 않고 버튼이 줄의 가운데에 있는지 확인한다.

```bash
pwsh -Command "& tools/DevRemote/win.ps1 -Do shot -Out .devremote/shot-preset-list.png -Scale 0.6"
```

- [ ] **Step 7: 커밋**

```bash
git add src/ModCode/ModMain/UI/Tabs/PresetTab.cs src/ModCode/ModMain/UI/EditorWindow.cs tools/research/ui-check.ps1 tools/research/preset-check.ps1
git commit -m "feat: 프리셋 탭 — 목록, 상세보기, 단계별 적용과 경지 올리기

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: 전체 검증과 검토

**Files:**
- Modify: 검증과 검토에서 나온 것만.

**Interfaces:**
- Consumes: Task 1~7의 모든 것.
- Produces: 스펙 8절의 시험 가운데 엔진과 화면에 해당하는 것(1~15, 17, 18)이 통과한 기록. 16(`preset-all`: 기본 10개)은 둘째 계획에서 한다.

- [ ] **Step 1: 회차 스크립트의 시험과 두 구성의 빌드**

```bash
pwsh tools/DevRemote/session.tests.ps1
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release -p:DevTools=false -o "$TEMP/tltoybox-release-check"
ls "$TEMP/tltoybox-release-check" | grep -c -i "\.dll$"
```

Expected: `FAILS=0`(22개), 두 빌드 모두 오류 0·경고 0, DLL 수 `1`.

- [ ] **Step 2: 프리셋의 검사를 깨끗한 회차들에서 처음부터 다시 돌린다**

한 회차가 10분을 넘지 않게 셋으로 나눈다. 회차와 검사는 한 번에 하나만 돌린다.

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab15 -Fixture base
pwsh tools/research/preset-check.ps1 -Part state
pwsh tools/research/preset-check.ps1 -Part martial
pwsh tools/research/preset-check.ps1 -Part files
pwsh tools/research/preset-check.ps1 -Part grade
pwsh tools/DevRemote/session.ps1 -End ptab15
```

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab16 -Fixture base
pwsh tools/research/preset-check.ps1 -Part extras
pwsh tools/research/preset-check.ps1 -Part extras-battle
pwsh tools/DevRemote/session.ps1 -End ptab16
```

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab17 -Fixture base
pwsh tools/research/preset-check.ps1 -Part apply
pwsh tools/research/preset-check.ps1 -Part apply-full
pwsh tools/research/preset-check.ps1 -Part apply-battle
pwsh tools/DevRemote/session.ps1 -Restart
pwsh tools/research/preset-check.ps1 -Part apply-restart
pwsh tools/DevRemote/session.ps1 -End ptab17
```

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab18 -Fixture base
pwsh tools/research/preset-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End ptab18
```

Expected: 모든 부분 `FAILS=0`, 모든 `-End`가 "SAVE RESTORED AND IDENTICAL". 순서에 주의: `state`는 회차의 첫 검사, `grade`는 경지를 올리므로 그 회차의 마지막.

- [ ] **Step 3: 기존 검사가 그대로인지 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab19 -Fixture base
pwsh tools/research/ui-check.ps1 -Part base
pwsh tools/research/ui-check.ps1 -Part tabs
pwsh tools/research/ui-check.ps1 -Part confirm
pwsh tools/research/ui-check.ps1 -Part wrap
pwsh tools/research/ui-check.ps1 -Part fateguard
pwsh tools/DevRemote/session.ps1 -End ptab19
```

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab20 -Fixture base
pwsh tools/research/ui-check.ps1 -Part rates
pwsh tools/research/ui-check.ps1 -Part vault
pwsh tools/research/ui-check.ps1 -Part charvault
pwsh tools/DevRemote/session.ps1 -End ptab20
```

Expected: 모두 `FAILS=0`. `rates`는 깨끗한 회차의 첫 검사로 돌린다(패치가 걸려 있지 않은 것을 본다). `wrap`의 "the last shown row is inside the list"가 한 번 실패하고 다시 돌려 통과하는 것은 전부터 있던 일이다(인계 문서 8.6절). 그 밖의 실패는 이번 변경의 탓으로 보고 원인을 찾는다.

- [ ] **Step 4: 배포 구성으로 게임에 띄운다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ptab21 -Fixture base -ReleaseBuild
pwsh -Command "& tools/DevRemote/win.ps1 -Do key -Vk 0x2D"
pwsh -Command "& tools/DevRemote/rc.ps1 -Cmd 'click 151 718' -NoShot"
pwsh -Command "& tools/DevRemote/win.ps1 -Do shot -Out .devremote/shot-preset-release.png -Scale 0.6"
pwsh tools/DevRemote/session.ps1 -End ptab21
```

Expected: `-Begin`이 "WAITED"로 끝난다(배포 구성에는 준비 로그가 없다). `click 151 718`은 탭 줄의 열두 번째 탭이다(x=151, `Tab0`이 y=146, 탭마다 52씩: 146 + 11 × 52 = 718). 출력의 `hit`이 `Tab11`로 끝나고 `Button.onClick`이 찍힌다. 캡처에 프리셋 탭의 목록이 그려져 있다. `MelonLoader\Latest.log`에 "trampoline" 줄이 없다.

```bash
grep -c "trampoline" "/e/Games/Tale of Immortal/MelonLoader/Latest.log"
```

Expected: `0`.

- [ ] **Step 5: 검토자 한 명이 브랜치 전체를 본다**

읽기 전용 서브에이전트(`oh-my-claudecode:code-reviewer`)에게 맡긴다. 지시문에는 다음을 그대로 넣는다.

```
레포: E:\TlToybox (절대경로만 쓴다. 모든 git 명령에 -C E:\TlToybox 를 붙인다. cd 뒤에 상대경로로 명령하지 않는다.)
이 작업(브랜치 feat/preset-tab의 코드 검토)만 수행하라. 끝나면 보고하고 정지하라. 다른 일로 진행하지 말라.
파일을 고치지 말고, 커밋하지 말고, 게임을 실행하지 말라. 명세에 없는 것을 추측해 제안하지 말라. 판단에 필요한 사실이 없으면 NEEDS_CONTEXT로 보고하라.

볼 것: git -C E:\TlToybox diff develop...feat/preset-tab -- src tools
기준 문서: docs/superpowers/specs/2026-10-14-preset-tab-design.md (3절이 게임에서 실측한 사실이다), docs/superpowers/plans/2026-10-14-preset-tab-1-engine.md의 Global Constraints와 Review Focus.

찾을 것(심각한 순서로, 파일:줄과 실패하는 입력을 구체적으로):
1. 사용자의 캐릭터를 되돌릴 수 없게 망가뜨리는 경로 — 익힌 공법이 지워짐, 장착 칸이 빈 채로 남음, 두 법보가 같은 기령을 가리킴, 대경지 7 미만에서 신혼을 씀, 깨진 공법이 남음.
2. 실패를 성공으로 세는 곳 — 넣은 뒤 상태를 다시 읽지 않고 문장만 믿는 판정.
3. 사용자가 고친 JSON의 어떤 값이 예외를 던지게 하는가(널, 음수, 아주 큰 수, 빈 배열, 객체 자리의 배열).
4. 배포 구성에서 Dev\ 코드를 참조하는 곳.
5. 스펙과 다른 동작.
확신이 없는 것은 "판단 보류"로 따로 적어라.
```

- [ ] **Step 6: 검토에서 나온 것을 처리한다**

- 결함으로 확인되는 것: 그 경우를 `preset-check.ps1`에 검사로 먼저 넣어 실패를 보고, 고치고, 통과를 본 뒤 커밋한다.
- "판단 보류": 게임에서 실험해 가린다(추측으로 고치지 않는다). 가리지 못한 것은 둘째 계획의 05 문서 34절 "확인하지 못한 것"에 적을 목록으로 남긴다.
- 사소해서 고치지 않기로 한 것: 목록으로 남겨 사용자에게 보고한다.

고친 것이 있으면 Step 2의 해당 회차를 다시 돌린다.

- [ ] **Step 7: 커밋**

```bash
git add -A src tools docs
git commit -m "fix: 프리셋 탭 검토에서 나온 것

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

고친 것이 없으면 이 단계는 건너뛴다. `develop`에 병합하지 않는다. 병합과 설치는 둘째 계획의 끝에서 한다.

---

## 스펙 8절과의 대응

| 스펙의 시험 | 이 계획의 검사 |
|---|---|
| 1, 2, 3 (기본 프리셋, 되돌리기, 읽을 수 없는 파일) | Task 5 `files`, Task 7 `ui` |
| 4 (틀린 줄) | Task 5 `files`의 "thirteen bad lines", Task 6 `apply`의 "a bad line does not stop the rest" |
| 5 (상세보기의 단계와 잠금) | Task 5 `files`의 단계 넷, Task 7 `ui` |
| 6, 7, 8 (단계 적용, 그대로여야 할 것, 다시 적용) | Task 6 `apply` |
| 8a (바꾸게 될 때의 확인) | Task 6 `apply`의 `preplaces`, Task 7 `ui`의 "asks first" |
| 9 (법보와 기령) | Task 4 `extras`, Task 6 `apply` |
| 10 (경지 올리기) | Task 2 `grade`, Task 7 `ui`의 `StageRaise4` |
| 11 (신혼) | Task 4 `extras`, Task 6 `apply-full` |
| 12 (목록의 `적용`) | Task 7 `ui`의 "apply sets the fates up to the current major grade", Task 6 `apply-full`의 "one grade 10 copy" |
| 13 (`최종 단계까지`) | Task 6 `apply-full`, Task 7 `ui` |
| 14 (전투) | Task 4 `extras-battle`, Task 6 `apply-battle` |
| 15 (저장 후 재시작) | Task 6 `apply-restart` |
| 16 (기본 10개) | 둘째 계획 |
| 17 (이력, 예외, 경고, 배포 구성) | 각 부분의 "no callback exception", Task 7 `ui`의 "no mod warning", Task 8 Step 4 |
| 18 (기존 검사) | Task 8 Step 3 |
