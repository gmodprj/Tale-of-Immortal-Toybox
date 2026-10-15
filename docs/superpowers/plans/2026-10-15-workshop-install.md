# 창작마당 모드 받기·설치(G2a) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 확정 목록 15개의 창작마당 모드를 SteamCMD로 받아 게임의 로컬 모드 폴더 `ModExportData\<창작마당ID>\`에 설치·갱신·제거하는 도구 스크립트 `tools/workshop.ps1`을 만들고, 회차 스크립트가 회차 중 설치한 모드를 되돌리게 한다.

**Architecture:** 목록 파일(`tools/workshop/mods.txt`)이 다룰 ID를 정하고, 스크립트는 받은 폴더의 모양(평평함 / `debug\Mod_*`)을 보고 설치 원본을 고른 뒤 `robocopy /MIR`로 복사한다. 게임 폴더에 쓰는 동작은 모두 `-Yes`가 있어야 하고 없으면 계획만 보인다. 설치 상태는 기록하지 않고 폴더 유무로 판정한다. 검증은 게임 없이 도는 `tools/workshop.tests.ps1`(가짜 받은 폴더·가짜 게임 폴더·가짜 SteamCMD)과 고정본 회차 한 번이다.

**Tech Stack:** PowerShell 7(`pwsh`), robocopy, SteamCMD(`E:\SteamCmd\steamcmd.exe`, 캐시 로그인 `deepestlight`), 개발 명령 `wsmods`(C# 7.3, 실험 브랜치 `feat/workshop-research`의 커밋 `f34dec4`).

**Spec:** `docs/superpowers/specs/2026-10-15-workshop-install-design.md`

## Global Constraints

- 브랜치 `feat/workshop-install`(develop에서, 스펙 커밋 `62df431`). 끝에서 `git merge --no-ff`로 `develop`에 병합한다. `main`에는 병합하지 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.
- 게임 폴더에 쓰는 것은 `ModExportData\<창작마당ID>\`뿐이고, `tools/workshop.ps1 -Install -Yes`(또는 `-Uninstall -Yes`)로만 쓴다. 회차 밖에서 `-Yes`를 붙이는 것은 사용자 확인 뒤에만 한다. `-Yes` 없이는 아무것도 쓰지 않는다.
- 비밀번호·Steam Guard는 어디에도 받지 않는다. 로그인이 풀려 있으면 사용자가 칠 명령(`E:\SteamCmd\steamcmd.exe +login deepestlight +quit`)을 보이고 멈춘다.
- 목록은 15개(스펙 3.1의 표). `2945768219`(MySql.Data.dll)와 `3681025074`(사용자 시험용)는 목록에 넣지 않고 주석으로만 남긴다.
- 받은 자리 `E:\SteamCmd\steamapps\workshop\content\1468810\<ID>\`, 설치 자리 `E:\Games\Tale of Immortal\ModExportData\<ID>\`. 폴더 이름은 창작마당 ID 그대로.
- 설치 원본: `<ID>\ModExportData.cache`가 있으면 `<ID>\` 전체(평평함), 없으면 `<ID>\debug\Mod_*\ModExportData.cache`가 정확히 하나일 때 그 `Mod_*` 폴더(debug). 둘 다 아니면 건너뜀.
- `-Install`: 설치된 폴더를 덮을 때는 게임이 꺼져 있어야 한다. 새 폴더는 게임이 켜져 있어도 넣는다. `-Uninstall`: 게임이 켜져 있으면 모두 거부. `Mod_*`와 목록에 없는 폴더는 어떤 경우에도 건드리지 않는다.
- 두 빌드 구성(개발, `-p:DevTools=false -o <임시 폴더>`) 모두 오류 0·경고 0, `bin\Release`에 `MOD_sWP6UL.dll` 하나.
- 게임 검증은 `pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서. 회차 이름은 `ws-1015b`부터 차례로(`ws-1015a`는 썼다), 다시 쓰지 않는다. `-End`가 "SAVE RESTORED AND IDENTICAL"과 "WORKSHOP FOLDERS REMOVED: 15". 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 스크립트의 출력 줄은 스펙 3.2의 모양(`|`로 나눈 칸, `PLAN ONLY`, `INSTALLED`/`UPDATED`/`UNCHANGED`, `REMOVED`, `받음 a/n`, `설치 b/n`)을 지킨다. 시험이 그 글로 판정한다.
- 시험 스크립트는 실제 게임 폴더·SteamCMD·세이브를 건드리지 않는다(임시 폴더와 `-GameDir`·`-ContentDir`·`-SteamCmd`·`-ProcessName`).
- 문서에는 확인한 이름만, 확인하지 못한 것은 "미확인"과 사유. 사용자에게 보이는 글은 한국어.

## Review Focus

1. 목록 줄이 깨졌다(칸이 넷 이하, ID가 숫자가 아님) → 어느 동작이든 시작 전에 그 줄을 보이며 멈추고 아무것도 쓰지 않는다. (Task 1 시험 "a broken list line stops…")
2. 받은 폴더는 있는데 `ModExportData.cache`가 없다(받다 끊김) → `-Install`은 "건너뜀: ModExportData.cache가 없음", `-Download`는 받지 않은 것으로 보고 다시 받는다. (Task 2 시험 "a folder without a cache…", Task 3 시험 "download picks up a folder without a cache")
3. 게임이 켜진 채 `-Install -Yes`로 이미 설치된 폴더를 갱신하려 한다 → 그 줄만 "거부: 게임이 실행 중"이고 새 폴더는 들어간다. (Task 2 시험 "a running game blocks…")
4. `-Uninstall -Yes`의 대상이 `ModExportData\` 바로 아래의 목록 ID 폴더가 아니다(목록 밖 폴더, `Mod_sWP6UL`) → 건드리지 않는다. (Task 2 시험 "uninstall leaves…")
5. SteamCMD가 비밀번호를 묻는다(캐시 만료) → 입력을 빈 파일로 주어 프롬프트에서 바로 끝나게 하고, 출력의 `Cached credentials not found`/`Login Failure`를 보면 사용자가 칠 명령을 보인다. 시간을 넘기면 끊는다. (Task 3 시험 "an expired login…". 실제 SteamCMD의 프롬프트 문구는 미확인이라 05 문서에 적는다)

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `tools/workshop/mods.txt` | 새로 | 다룰 창작마당 ID 15개와 종류·이름·비고. 줄 순서가 설치 순서 |
| `tools/workshop.ps1` | 새로 | 목록 읽기(`ReadList`, `Chosen`), 받은 폴더 판정(`Downloaded`, `Source`), 비교(`DiffCount`), 네 동작(`ShowStatus`, `DoDownload`, `DoInstall`, `DoUninstall`), SteamCMD 호출(`RunSteamCmd`) |
| `tools/workshop.tests.ps1` | 새로 | 가짜 받은 폴더·가짜 게임 폴더·가짜 SteamCMD(`steamcmd.ps1`)로 네 동작을 시험. 줄마다 PASS/FAIL, 마지막 줄 `FAILS=<수>` |
| `tools/DevRemote/session.ps1` | 수정 | `-Begin`이 `ModExportData\`의 폴더 이름을 적고 `-End`가 회차 중 생긴 폴더를 지운다 |
| `tools/DevRemote/session.tests.ps1` | 수정 | 상자에 가짜 게임 폴더를 더하고 모든 시험이 `-GameDir`을 준다. 시험 24·25 추가 |
| `src/ModCode/ModMain/Dev/DevCommandsWorkshop.cs`, `Dev/DevCommands.cs` | 가져옴 | 개발 명령 `wsmods`(실험 브랜치 커밋 `f34dec4`를 cherry-pick) |
| `docs/05-editor-v0.1.md` | 수정 | 41절 "창작마당 모드 받기·설치(G2a)" |
| `docs/06-handoff.md`, `CLAUDE.md` | 수정 | 상태·도구·다음 일, 게임 폴더 규칙의 예외 |

---

### Task 1: 목록 파일과 `-Status`

**Files:**
- Create: `tools/workshop/mods.txt`
- Create: `tools/workshop.ps1`
- Create: `tools/workshop.tests.ps1`

**Interfaces:**
- Produces: `tools/workshop.ps1`의 매개변수 `-Status -Download -Install -Uninstall -Yes -Id <쉼표 목록> -GameDir -SteamCmd -Account -List -AppId -ContentDir -ProcessName -TimeoutMinutes`. 함수 `ReadList()`(줄 객체 `Id, Kind, Title, Korean, Note`의 목록), `Chosen($rows)`, `Downloaded([string]$id)`(받은 폴더 경로 또는 `$null`), `Source([string]$id)`(`Path, Shape, Reason`), `SizeMb`, `Dlls`, `DiffCount($src, $dst)`, `Updated($id)`, `Installed($id)`, `Running`. Task 2·3이 이 함수들을 쓴다.
- Produces: `tools/workshop.tests.ps1`의 `NewBox`(상자: `root, content, steamcmd, game, list, acf`), `Run($box, $process, $more)`, `Check`. Task 2·3이 시험을 이 파일 끝에 더한다.

- [ ] **Step 1: 목록 파일을 쓴다**

`tools/workshop/mods.txt`:

```text
# 창작마당 모드 목록(docs/superpowers/specs/2026-10-15-workshop-install-design.md 3.1절).
# 한 줄에 하나: 창작마당ID | 종류 | 원문 이름 | 한국어 이름 | 비고. '#' 줄은 주석. 줄 순서가 설치 순서다(Fatury 먼저).
# tools/workshop.ps1은 이 목록에 있는 ID만 다룬다.
2814703549 | 틀 | Fatury | Fatury 틀 | 2815562168이 필요로 함. DLL 3
2815562168 | 외형 | 原版17个女性人物动态立绘替换 | 원작 여성 인물 17명 동적 입화 교체 | Fatury 필요. 111MB
3200319699 | 외형 | 【木头】AI动态立绘 | [木头] AI 동적 입화 | 206MB
3001496635 | 외형 | 【软甜废独家】《五官头饰动态立绘》-原创本人手画 | [软甜废] 이목구비·머리 장식 동적 입화 | DLL 1
2993500314 | 외형 | 动态头发美化 | 동적 머리카락 미화 |
2819712935 | 외형 | 女npc颜值优化 | 여성 NPC 외모 개선 | debug 모양(Mod_dVhgEv)
2823930683 | 외형 | 男性NPC颜值优化 | 남성 NPC 외모 개선 |
2945768367 | 외형 | 【执余】大量生成美女 | [执余] 미녀 대량 생성 |
2874128983 | 외형 | 人物美化 .服装解锁 .魅力与权重修改 | 인물 미화·복장 해금·매력과 가중치 수정 |
2876769227 | 외형 | 【只有梦长】特殊NPC服装解锁 | [只有梦长] 특수 NPC 복장 해금 | 모드 ID FASRM7
2929805068 | 외형 | 【大姨夫】仙衣阁、美化 | [大姨夫] 선의각·미화 | debug 모양(Mod_cPugE8)
2832167498 | 외형 | 【含羞草】捏脸 | [含羞草] 얼굴 만들기 | DLL 1
2814806198 | 외형 | 过场图替换示例mod | 전환 화면 그림 교체 예제 모드 |
3390738990 | 공법 | 【只有梦长】五本功法开源 | [只有梦长] 공법 다섯 권(오픈소스) | debug 모양(Mod_7kyQkw). 새 공법 다섯
2839261049 | 공법 | 真.镇宗之宝：传承心法 | 진·진종지보: 전승 심법 | DLL 1(MOD_uCKcKw.dll)
# 2945768219 【执余】女NPC颜值评定优化 — MySql.Data.dll이 들어 있어 제외(2026-10-15)
# 3681025074 — 사용자의 시험용, 목록에 넣지 않음
```

줄마다 칸이 다섯이어야 한다(비고가 비어도 마지막 `|` 뒤를 비워 둔다). 파일은 UTF-8(BOM 없음)로 저장한다.

- [ ] **Step 2: 시험 파일의 틀과 `-Status` 시험을 쓴다**

`tools/workshop.tests.ps1`:

```powershell
# tools/workshop.ps1의 시험. 게임·SteamCMD·실제 세이브를 건드리지 않고 임시 폴더에서 한다.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다.
$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot 'workshop.ps1'
$script:fails = 0
$none = 'no-such-process-tltoybox'            # 실행 중인 게임이 없는 상황
$busy = (Get-Process -Id $PID).ProcessName    # 실행 중인 게임이 있는 상황(이 시험을 돌리는 프로세스의 이름을 빌린다)
$boxes = New-Object System.Collections.Generic.List[string]

function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
function WriteFile([string]$path, [string]$text) {
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
  [IO.File]::WriteAllText($path, $text)
}
# 받은 폴더·게임 폴더·목록·가짜 SteamCMD를 흉내 낸 임시 폴더 한 벌.
#   111: 평평한 것(DLL 하나)  222: debug 모양  333: debug 안에 Mod_*가 둘  444: 받지 않음  555: 폴더만 있고 cache 없음
function NewBox {
  $root = Join-Path $env:TEMP ('tltoybox-workshop-tests-' + [guid]::NewGuid().ToString('N'))
  $box = [pscustomobject]@{
    root = $root; content = "$root\steam\steamapps\workshop\content\1468810"; steamcmd = "$root\steam\steamcmd.ps1"
    game = "$root\game"; list = "$root\mods.txt"; acf = "$root\steam\steamapps\workshop\appworkshop_1468810.acf" }
  WriteFile "$($box.game)\ModExportData\Mod_sWP6UL\ModExportData.cache" 'ours'
  WriteFile "$($box.game)\ModExportData\unknown\ModExportData.cache" 'not in the list'
  WriteFile "$($box.content)\111\ModExportData.cache" 'c111'
  WriteFile "$($box.content)\111\ModAssets\a.png" 'png'
  WriteFile "$($box.content)\111\ModCode\dll\MOD_x.dll" 'dll'
  WriteFile "$($box.content)\222\debug\Mod_abc\ModExportData.cache" 'c222'
  WriteFile "$($box.content)\222\debug\Mod_abc\ModAssets\b.png" 'png'
  WriteFile "$($box.content)\222\ModProject\ModData.cache" 'project'
  WriteFile "$($box.content)\333\debug\Mod_a\ModExportData.cache" 'c333a'
  WriteFile "$($box.content)\333\debug\Mod_b\ModExportData.cache" 'c333b'
  [void][IO.Directory]::CreateDirectory("$($box.content)\555")
  WriteFile $box.list "# 시험용`n111 | 외형 | one | 하나 | DLL 1`n222 | 외형 | two | 둘 | debug`n333 | 외형 | three | 셋 |`n444 | 공법 | four | 넷 |`n555 | 공법 | five | 다섯 |`n"
  WriteFile $box.acf "`"AppWorkshop`"`n{`n`t`"WorkshopItemDetails`"`n`t{`n`t`t`"111`"`n`t`t{`n`t`t`t`"manifest`"`t`t`"1`"`n`t`t`t`"timeupdated`"`t`t`"1735353779`"`n`t`t}`n`t}`n}`n"
  # 가짜 SteamCMD. +runscript 파일의 workshop_download_item 줄마다 폴더를 만들고 Success 줄을 찍는다.
  # 첫 호출에서는 마지막 항목을 failed로 찍는다(재시도 확인). expired.flag가 있으면 로그인이 풀린 출력만 찍는다.
  WriteFile $box.steamcmd @'
$content = Join-Path $PSScriptRoot 'steamapps\workshop\content\1468810'
$marker = Join-Path $PSScriptRoot 'called.txt'
'Steam Console Client (fake)'
if (Test-Path (Join-Path $PSScriptRoot 'expired.flag')) { 'Cached credentials not found'; 'FAILED (Invalid Password)'; exit 0 }
$i = [array]::IndexOf($args, '+runscript')
$lines = @(Get-Content -LiteralPath $args[$i + 1] | Where-Object { $_ -match '^workshop_download_item' })
$first = -not (Test-Path $marker)
[IO.File]::WriteAllText($marker, '')
for ($k = 0; $k -lt $lines.Count; $k++) {
  $id = ($lines[$k] -split ' ')[2]
  if ($first -and $k -eq $lines.Count - 1) { "ERROR! Download item $id failed (Failure)."; continue }
  [void][IO.Directory]::CreateDirectory("$content\$id")
  [IO.File]::WriteAllText("$content\$id\ModExportData.cache", "c$id")
  "Success. Downloaded item $id to `"$content\$id`" (10 bytes)"
}
'@
  $boxes.Add($root)
  $box
}
# workshop.ps1을 상자 안에서 돌린다. 던진 오류는 "THROWN: ..." 문자열로 돌려준다.
function Run($box, [string]$process, [hashtable]$more) {
  try {
    $out = & $tool -GameDir $box.game -SteamCmd $box.steamcmd -ContentDir $box.content -List $box.list -ProcessName $process -TimeoutMinutes 1 @more
    ($out -join "`n")
  } catch { 'THROWN: ' + $_.Exception.Message }
}
function Tree([string]$root) {
  (Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($root.Length) + '=' + [IO.File]::ReadAllText($_.FullName) }) -join ';'
}

# 1. -Status: 줄마다 받음·설치·갱신·DLL, 마지막 줄의 요약
$b = NewBox
$o = Run $b $none @{ Status = $true }
Check 'Status shows a downloaded flat item with size, date and dll count' ($o -match '111 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| - \| - \| 1') $o
Check 'Status shows a debug item without a date' ($o -match '222 \| 외형 \| 둘 \| 0(\.0)?MB - \| - \| - \| 0') $o
Check 'Status shows a missing item with dashes' ($o -match '444 \| 공법 \| 넷 \| - \| - \| - \| -') $o
Check 'Status counts a folder without a cache as not downloaded' ($o -match '555 \| 공법 \| 다섯 \| - \| - \| - \| -') $o
Check 'Status ends with the summary' ($o -match '받음 3/5, 설치 0/5') $o

# 2. -Id는 목록의 ID만 받는다
$o = Run $b $none @{ Status = $true; Id = '111,222' }
Check 'Id narrows the rows' ($o -match '받음 2/2, 설치 0/2' -and -not ($o -match '(?m)^444')) $o
$o = Run $b $none @{ Status = $true; Id = '999' }
Check 'an unknown Id is refused' ($o -match '^THROWN.*목록에 없는 ID') $o

# 3. 깨진 목록 줄은 시작 전에 멈춘다
$b = NewBox
[IO.File]::AppendAllText($b.list, "abc | 외형 | bad`n")
$o = Run $b $none @{ Status = $true }
Check 'a broken list line stops the tool before it does anything' ($o -match '^THROWN.*목록의 줄을 읽지 못했습니다') $o

# 4. 동작은 하나만 고른다
$b = NewBox
$o = Run $b $none @{ }
Check 'no action is refused' ($o -match '^THROWN.*하나를 고릅니다') $o
$o = Run $b $none @{ Status = $true; Install = $true }
Check 'two actions are refused' ($o -match '^THROWN.*하나를 고릅니다') $o

foreach ($root in $boxes) {
  if ($root.StartsWith((Join-Path $env:TEMP 'tltoybox-workshop-tests-')) -and (Test-Path $root)) { [IO.Directory]::Delete($root, $true) }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
exit 0
```

`333`은 cache가 둘이라 "받음"으로 세고(다시 받지 않는다) 설치 원본은 못 고른다. `555`는 cache가 없어 "받지 않음"이다.

- [ ] **Step 3: 시험을 돌려 실패를 본다**

Run: `pwsh tools/workshop.tests.ps1`
Expected: `workshop.ps1`이 없어 모든 줄이 `FAIL`(또는 스크립트 오류), 마지막 줄 `FAILS=`가 0보다 크다.

- [ ] **Step 4: `workshop.ps1`의 공통부와 `-Status`를 쓴다**

`tools/workshop.ps1`:

```powershell
# 창작마당 모드를 받아 게임의 로컬 모드 폴더에 설치하는 도구(docs/superpowers/specs/2026-10-15-workshop-install-design.md).
#   -Status                       목록의 줄마다 받음·설치·갱신·DLL을 보인다
#   -Download  [-Id a,b]          받지 않은 것(또는 -Id로 고른 것)을 SteamCMD의 캐시 로그인으로 받는다(최대 세 번)
#   -Install   [-Id a,b] [-Yes]   받은 것을 <GameDir>\ModExportData\<ID>\에 복사한다. -Yes 없이는 계획만 보인다
#   -Uninstall [-Id a,b] [-Yes]   목록 ID의 설치 폴더를 지운다. -Yes 없이는 계획만 보인다
# 게임 폴더에 쓰는 것은 ModExportData\<창작마당ID>\뿐이다(CLAUDE.md). 비밀번호는 받지 않는다.
# 설치 원본: <ID>\ModExportData.cache가 있으면 <ID>\ 전체, 없으면 <ID>\debug\Mod_*\ 가운데 cache가 있는 폴더가 하나일 때 그것.
param([switch]$Status, [switch]$Download, [switch]$Install, [switch]$Uninstall, [switch]$Yes,
  [string]$Id = '',
  [string]$GameDir = 'E:\Games\Tale of Immortal', [string]$SteamCmd = 'E:\SteamCmd\steamcmd.exe', [string]$Account = 'deepestlight',
  [string]$List = '', [int]$AppId = 1468810,
  # 아래는 workshop.tests.ps1이 쓰는 시험용 매개변수다. 평소에는 주지 않는다.
  [string]$ContentDir = '', [string]$ProcessName = 'guigubahuang', [int]$TimeoutMinutes = 30)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $List) { $List = Join-Path $repo 'tools\workshop\mods.txt' }
$steamRoot = Split-Path -Parent $SteamCmd
if (-not $ContentDir) { $ContentDir = Join-Path $steamRoot "steamapps\workshop\content\$AppId" }
$acf = Join-Path $steamRoot "steamapps\workshop\appworkshop_$AppId.acf"
$modsDir = Join-Path $GameDir 'ModExportData'

function Running { [bool](Get-Process $ProcessName -ErrorAction SilentlyContinue) }

# 목록 파일. '#' 줄은 주석. ID | 종류 | 원문 이름 | 한국어 이름 | 비고
function ReadList {
  if (-not (Test-Path -LiteralPath $List)) { throw "목록 파일이 없습니다: $List" }
  $rows = New-Object System.Collections.Generic.List[object]
  foreach ($line in [IO.File]::ReadAllLines($List)) {
    $t = $line.Trim()
    if (-not $t -or $t.StartsWith('#')) { continue }
    $cells = @($t.Split('|') | ForEach-Object { $_.Trim() })
    if ($cells.Count -lt 5 -or $cells[0] -notmatch '^\d+$') { throw "목록의 줄을 읽지 못했습니다: $line" }
    $rows.Add([pscustomobject]@{ Id = $cells[0]; Kind = $cells[1]; Title = $cells[2]; Korean = $cells[3]; Note = $cells[4] })
  }
  @($rows)
}
# -Id로 고른 것만 남긴다. 목록에 없는 ID는 거부한다.
function Chosen($rows) {
  if (-not $Id) { return @($rows) }
  $wanted = @($Id.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
  foreach ($w in $wanted) { if (-not @($rows | Where-Object { $_.Id -eq $w })) { throw "목록에 없는 ID입니다: $w" } }
  @($rows | Where-Object { $wanted -contains $_.Id })
}
# 받은 폴더. ModExportData.cache가 어디에든 하나 이상 있어야 받은 것이다(받다 끊긴 빈 폴더는 아니다).
function Downloaded([string]$id) {
  $root = Join-Path $ContentDir $id
  if (-not (Test-Path -LiteralPath $root -PathType Container)) { return $null }
  if (@(Get-ChildItem -LiteralPath $root -Recurse -File -Filter ModExportData.cache).Count -ge 1) { $root } else { $null }
}
# 설치 원본. 평평하면 <ID>\, debug 모양이면 <ID>\debug\Mod_*\ 하나. 못 고르면 Path가 $null이고 Reason에 이유.
function Source([string]$id) {
  $root = Join-Path $ContentDir $id
  if (-not (Downloaded $id)) { return [pscustomobject]@{ Path = $null; Shape = '-'; Reason = $(if (Test-Path -LiteralPath $root) { 'ModExportData.cache가 없음' } else { '받지 않음' }) } }
  if (Test-Path -LiteralPath (Join-Path $root 'ModExportData.cache')) { return [pscustomobject]@{ Path = $root; Shape = '평평함'; Reason = '' } }
  $debug = Join-Path $root 'debug'
  $inner = @(if (Test-Path -LiteralPath $debug) { Get-ChildItem -LiteralPath $debug -Directory | Where-Object { $_.Name -like 'Mod_*' -and (Test-Path -LiteralPath (Join-Path $_.FullName 'ModExportData.cache')) } })
  if ($inner.Count -eq 1) { return [pscustomobject]@{ Path = $inner[0].FullName; Shape = 'debug'; Reason = '' } }
  if ($inner.Count -gt 1) { return [pscustomobject]@{ Path = $null; Shape = 'debug'; Reason = "debug 안에 Mod_* 폴더가 $($inner.Count)개" } }
  [pscustomobject]@{ Path = $null; Shape = '?'; Reason = '설치 원본을 고르지 못함' }
}
function SizeMb([string]$path) {
  $sum = (Get-ChildItem -LiteralPath $path -Recurse -File | Measure-Object Length -Sum).Sum
  [math]::Round([double]$sum / 1MB, 1)
}
function Dlls([string]$path) { @(Get-ChildItem -LiteralPath $path -Recurse -File -Filter *.dll | ForEach-Object Name) }
# 받은 원본과 설치본이 다른 파일 수. robocopy /L은 복사하지 않고 할 일만 센다(끝 코드 0이면 같다).
function DiffCount([string]$src, [string]$dst) {
  $lines = @(robocopy $src $dst /L /MIR /NJH /NJS /NP /NDL /NC /NS | Where-Object { $_.Trim() })
  if ($LASTEXITCODE -ge 8) { throw "robocopy 비교에 실패했습니다($LASTEXITCODE): $src" }
  if ($LASTEXITCODE -eq 0) { 0 } else { [math]::Max(1, $lines.Count) }
}
# appworkshop_<AppId>.acf의 timeupdated(창작마당에서 마지막으로 갱신된 날).
function Updated([string]$id) {
  if (-not (Test-Path -LiteralPath $acf)) { return '-' }
  $m = [regex]::Match([IO.File]::ReadAllText($acf), '"' + $id + '"\s*\{[^}]*"timeupdated"\s*"(\d+)"')
  if ($m.Success) { [DateTimeOffset]::FromUnixTimeSeconds([long]$m.Groups[1].Value).LocalDateTime.ToString('yyyy-MM-dd') } else { '-' }
}
function Installed([string]$id) { Join-Path $modsDir $id }
function IsInstalled([string]$id) { Test-Path -LiteralPath (Join-Path (Installed $id) 'ModExportData.cache') }

function ShowStatus($rows) {
  $got = 0; $inst = 0
  'ID | 종류 | 이름 | 받음 | 설치 | 갱신 | DLL'
  foreach ($r in $rows) {
    $root = Downloaded $r.Id
    $s = Source $r.Id
    $isInst = IsInstalled $r.Id
    if ($root) { $got++ }
    if ($isInst) { $inst++ }
    $have = if ($root) { "$(SizeMb $root)MB $(Updated $r.Id)" } else { '-' }
    $fresh = if ($s.Path -and $isInst) { $n = DiffCount $s.Path (Installed $r.Id); if ($n -eq 0) { '같음' } else { "다름 $n" } } else { '-' }
    $dll = if ($root) { (Dlls $root).Count } else { '-' }
    "$($r.Id) | $($r.Kind) | $($r.Korean) | $have | $(if ($isInst) { '예' } else { '-' }) | $fresh | $dll"
  }
  "받음 $got/$($rows.Count), 설치 $inst/$($rows.Count)"
}

$chosen = Chosen (ReadList)
$modes = @(@($Status, $Download, $Install, $Uninstall) | Where-Object { $_ }).Count
if ($modes -ne 1) { throw '-Status, -Download, -Install, -Uninstall 가운데 하나를 고릅니다.' }
if ($Status) { ShowStatus $chosen }
$global:LASTEXITCODE = 0
exit 0
```

- [ ] **Step 5: 시험을 돌려 통과를 본다**

Run: `pwsh tools/workshop.tests.ps1`
Expected: 1~4의 줄이 모두 `PASS`, `FAILS=0`.

실제 환경에서도 한 번: `pwsh tools/workshop.ps1 -Status` → 15줄, 받음 열에 크기와 날짜(예: `2814703549 | 틀 | Fatury 틀 | 2.0MB 2024-12-28 | - | - | 3`), 마지막 줄 `받음 15/15, 설치 0/15`. 게임 폴더에는 아무것도 쓰지 않는다.

- [ ] **Step 6: 커밋**

```bash
git add tools/workshop/mods.txt tools/workshop.ps1 tools/workshop.tests.ps1
git commit -m "feat: 창작마당 모드 도구 — 목록 파일과 -Status(받음·설치·갱신·DLL)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 2: `-Install`과 `-Uninstall`

**Files:**
- Modify: `tools/workshop.ps1` (`ShowStatus` 뒤에 함수 둘, 끝의 분기)
- Modify: `tools/workshop.tests.ps1` (4번 시험 뒤, 정리 루프 앞)

**Interfaces:**
- Consumes: Task 1의 `Source`, `Downloaded`, `DiffCount`, `Installed`, `IsInstalled`, `SizeMb`, `Dlls`, `Running`, `$modsDir`, `$Yes`.
- Produces: `DoInstall($rows)`, `DoUninstall($rows)`. 출력 줄: 계획 `ID | 한국어 이름 | 모양 | 크기MB | DLL <이름들 또는 ->  | 새로 설치|갱신(다른 파일 n)|같음`, `건너뜀: <이유>`, `거부: 게임이 실행 중입니다(설치된 폴더를 덮지 않습니다)`, `PLAN ONLY (아무것도 쓰지 않았습니다. 실제로 쓰려면 -Yes)`; 실행 `INSTALLED|UPDATED|UNCHANGED <ID> <이름>`, `설치 n개`; 제거 `지움 <크기>MB`, `설치돼 있지 않음`, `PLAN ONLY (아무것도 지우지 않았습니다. 실제로 지우려면 -Yes)`, `REMOVED <ID> <이름>`, `지움 n개`.

- [ ] **Step 1: 시험을 더한다**

`tools/workshop.tests.ps1`의 `# 4.` 블록 뒤(정리 `foreach ($root in $boxes)` 앞)에:

```powershell
# 5. -Install(계획만): 원본을 고르고 이유를 보이되 아무것도 쓰지 않는다
$b = NewBox
$before = Tree $b.game
$o = Run $b $none @{ Install = $true }
Check 'install plan lists the flat item with its dll' ($o -match '111 \| 하나 \| 평평함 \| 0(\.0)?MB \| DLL MOD_x\.dll \| 새로 설치') $o
Check 'install plan lists the debug item' ($o -match '222 \| 둘 \| debug \| 0(\.0)?MB \| DLL - \| 새로 설치') $o
Check 'install plan skips an item with two Mod_ folders' ($o -match '333 \| 셋 \| 건너뜀: debug 안에 Mod_\* 폴더가 2개') $o
Check 'install plan skips an item that is not downloaded' ($o -match '444 \| 넷 \| 건너뜀: 받지 않음') $o
Check 'a folder without a cache is skipped with the reason' ($o -match '555 \| 다섯 \| 건너뜀: ModExportData.cache가 없음') $o
Check 'install without -Yes writes nothing' ($o -match 'PLAN ONLY' -and (Tree $b.game) -eq $before) $o

# 6. -Install -Yes: 평평한 것은 그대로, debug 모양은 Mod_* 안의 내용만
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'install -Yes copies the flat item as it is' ($o -match 'INSTALLED 111 하나' -and [IO.File]::ReadAllText("$($b.game)\ModExportData\111\ModExportData.cache") -eq 'c111' -and (Test-Path "$($b.game)\ModExportData\111\ModCode\dll\MOD_x.dll")) $o
Check 'install -Yes flattens the debug item' ($o -match 'INSTALLED 222 둘' -and [IO.File]::ReadAllText("$($b.game)\ModExportData\222\ModExportData.cache") -eq 'c222' -and (Test-Path "$($b.game)\ModExportData\222\ModAssets\b.png") -and -not (Test-Path "$($b.game)\ModExportData\222\debug") -and -not (Test-Path "$($b.game)\ModExportData\222\ModProject")) $o
Check 'install -Yes ends with the count' ($o -match '설치 2개') $o
$o = Run $b $none @{ Status = $true }
Check 'Status shows installed items as identical' ($o -match '111 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| 예 \| 같음 \| 1' -and $o -match '받음 3/5, 설치 2/5') $o

# 7. 같은 것을 다시 넣으면 UNCHANGED, 원본이 바뀌면 UPDATED(게임이 켜져 있으면 그 줄만 거부)
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'a second install -Yes changes nothing' ($o -match 'UNCHANGED 111 하나' -and $o -match 'UNCHANGED 222 둘' -and $o -match '설치 0개') $o
WriteFile "$($b.content)\111\ModAssets\new.png" 'new'
$o = Run $b $none @{ Status = $true }
Check 'Status notices a changed source' ($o -match '111 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| 예 \| 다름 \d+ \| 1') $o
WriteFile "$($b.content)\444\ModExportData.cache" 'c444'
$o = Run $b $busy @{ Install = $true; Yes = $true }
Check 'a running game blocks updating an installed folder' ($o -match '111 \| 하나 .*거부: 게임이 실행 중' -and -not (Test-Path "$($b.game)\ModExportData\111\ModAssets\new.png")) $o
Check 'a running game still lets a new folder in' ($o -match 'INSTALLED 444 넷' -and (Test-Path "$($b.game)\ModExportData\444\ModExportData.cache") -and $o -match '다음 게임 시작부터') $o
$o = Run $b $none @{ Install = $true; Yes = $true; Id = '111' }
Check 'with the game closed the changed item is updated' ($o -match 'UPDATED 111 하나' -and (Test-Path "$($b.game)\ModExportData\111\ModAssets\new.png")) $o

# 8. -Uninstall: 계획만, 게임이 켜져 있으면 거부, -Yes는 목록 ID 폴더만 지운다
$before = Tree $b.game
$o = Run $b $none @{ Uninstall = $true }
Check 'uninstall plan lists installed items and writes nothing' ($o -match '111 \| 하나 \| 지움 0(\.0)?MB' -and $o -match '333 \| 셋 \| 설치돼 있지 않음' -and $o -match 'PLAN ONLY' -and (Tree $b.game) -eq $before) $o
$o = Run $b $busy @{ Uninstall = $true; Yes = $true }
Check 'uninstall is refused while the game runs' ($o -match '^THROWN.*게임이 실행 중' -and (Tree $b.game) -eq $before) $o
$o = Run $b $none @{ Uninstall = $true; Yes = $true }
Check 'uninstall -Yes removes the listed folders' ($o -match 'REMOVED 111 하나' -and $o -match 'REMOVED 222 둘' -and $o -match 'REMOVED 444 넷' -and $o -match '지움 3개' -and -not (Test-Path "$($b.game)\ModExportData\111") -and -not (Test-Path "$($b.game)\ModExportData\222")) $o
Check 'uninstall leaves the editor folder and a folder that is not in the list' ((Test-Path "$($b.game)\ModExportData\Mod_sWP6UL\ModExportData.cache") -and (Test-Path "$($b.game)\ModExportData\unknown\ModExportData.cache")) $o

# 9. 게임 폴더가 아니면 설치하지 않는다
$b = NewBox
[IO.Directory]::Delete("$($b.game)\ModExportData", $true)
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'install refuses a folder without ModExportData' ($o -match '^THROWN.*ModExportData') $o
```

- [ ] **Step 2: 시험을 돌려 실패를 본다**

Run: `pwsh tools/workshop.tests.ps1`
Expected: 5~9의 줄이 `FAIL`(`-Install`·`-Uninstall`이 아직 아무것도 하지 않아 `PLAN ONLY`도 `INSTALLED`도 없다). 1~4는 `PASS`.

- [ ] **Step 3: `DoInstall`과 `DoUninstall`을 쓴다**

`tools/workshop.ps1`의 `ShowStatus` 함수 뒤에:

```powershell
function DoInstall($rows) {
  if (-not (Test-Path -LiteralPath $modsDir -PathType Container)) { throw "게임의 로컬 모드 폴더가 없습니다: $modsDir (ModExportData가 있는 게임 폴더여야 합니다)" }
  $running = Running
  $plans = New-Object System.Collections.Generic.List[object]
  foreach ($r in $rows) {
    $s = Source $r.Id
    if (-not $s.Path) { "$($r.Id) | $($r.Korean) | 건너뜀: $($s.Reason)"; continue }
    $target = Installed $r.Id
    $exists = Test-Path -LiteralPath $target -PathType Container
    $what = if (-not $exists) { '새로 설치' } else { $n = DiffCount $s.Path $target; if ($n -eq 0) { '같음' } else { "갱신(다른 파일 $n)" } }
    $dlls = Dlls $s.Path
    $line = "$($r.Id) | $($r.Korean) | $($s.Shape) | $(SizeMb $s.Path)MB | DLL $(if ($dlls.Count -gt 0) { $dlls -join ',' } else { '-' }) | $what"
    if ($exists -and $running -and $what -ne '같음') { "$line | 거부: 게임이 실행 중입니다(설치된 폴더를 덮지 않습니다)"; continue }
    $line
    $plans.Add([pscustomobject]@{ Row = $r; Src = $s.Path; Dst = $target; What = $what })
  }
  if (-not $Yes) { 'PLAN ONLY (아무것도 쓰지 않았습니다. 실제로 쓰려면 -Yes)'; return }
  $done = 0
  foreach ($p in $plans) {
    if ($p.What -eq '같음') { "UNCHANGED $($p.Row.Id) $($p.Row.Korean)"; continue }
    robocopy $p.Src $p.Dst /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "복사에 실패했습니다(robocopy $LASTEXITCODE): $($p.Dst)" }
    if (-not (Test-Path -LiteralPath (Join-Path $p.Dst 'ModExportData.cache'))) { throw "설치본에 ModExportData.cache가 없습니다: $($p.Dst)" }
    "$(if ($p.What -eq '새로 설치') { 'INSTALLED' } else { 'UPDATED' }) $($p.Row.Id) $($p.Row.Korean)"
    $done++
  }
  if ($running -and $done -gt 0) { '게임이 실행 중입니다. 새로 넣은 모드는 다음 게임 시작부터 읽힙니다.' }
  "설치 $done개"
}

function DoUninstall($rows) {
  if (Running) { throw '게임이 실행 중입니다. 모드 폴더를 지우지 않습니다. 게임을 끈 뒤 다시 돌립니다.' }
  $targets = New-Object System.Collections.Generic.List[object]
  foreach ($r in $rows) {
    $target = Installed $r.Id
    if (-not (Test-Path -LiteralPath $target -PathType Container)) { "$($r.Id) | $($r.Korean) | 설치돼 있지 않음"; continue }
    "$($r.Id) | $($r.Korean) | 지움 $(SizeMb $target)MB"
    $targets.Add([pscustomobject]@{ Row = $r; Path = $target })
  }
  if (-not $Yes) { 'PLAN ONLY (아무것도 지우지 않았습니다. 실제로 지우려면 -Yes)'; return }
  foreach ($t in $targets) {
    # 지우는 것이 ModExportData 바로 아래의 목록 ID 폴더인지 한 번 더 확인한다. Mod_*와 모르는 폴더는 여기 올 수 없다.
    $full = [IO.Path]::GetFullPath($t.Path)
    $parent = [IO.Path]::GetFullPath($modsDir).TrimEnd('\') + '\'
    if (-not $full.StartsWith($parent) -or (Split-Path -Leaf $full) -ne $t.Row.Id -or $t.Row.Id -notmatch '^\d+$') { throw "지우지 않습니다(예상 밖의 경로): $full" }
    [IO.Directory]::Delete($full, $true)
    "REMOVED $($t.Row.Id) $($t.Row.Korean)"
  }
  "지움 $($targets.Count)개"
}
```

끝의 분기를 바꾼다:

```powershell
if ($Status) { ShowStatus $chosen }
elseif ($Install) { DoInstall $chosen }
elseif ($Uninstall) { DoUninstall $chosen }
```

- [ ] **Step 4: 시험을 돌려 통과를 본다**

Run: `pwsh tools/workshop.tests.ps1`
Expected: 1~9 모두 `PASS`, `FAILS=0`.

실제 환경에서 계획만: `pwsh tools/workshop.ps1 -Install` → 15줄(4개가 `debug`, DLL 줄 4개: `2814703549`에 `Fatury.dll,UnityEngine.InputLegacyModule.dll,YamlDotNet.dll`), 모두 `새로 설치`, 마지막 줄 `PLAN ONLY`. 게임 폴더의 `ModExportData\`에 `Mod_sWP6UL`만 있는지 다시 본다(`-Yes`를 붙이지 않는다).

- [ ] **Step 5: 커밋**

```bash
git add tools/workshop.ps1 tools/workshop.tests.ps1
git commit -m "feat: 창작마당 모드 도구 — -Install(계획만/-Yes, 평평함·debug 모양, 갱신)과 -Uninstall(목록 ID 폴더만)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 3: `-Download`

**Files:**
- Modify: `tools/workshop.ps1` (`DoUninstall` 뒤에 함수 둘, 끝의 분기)
- Modify: `tools/workshop.tests.ps1` (9번 시험 뒤)

**Interfaces:**
- Consumes: Task 1의 `Downloaded`, `$SteamCmd`, `$steamRoot`, `$Account`, `$AppId`, `$TimeoutMinutes`; 가짜 SteamCMD(`NewBox`의 `steamcmd.ps1`).
- Produces: `RunSteamCmd([string[]]$arguments)`(해시 `Ok`, `Lines`), `DoDownload($rows)`. 출력 줄: `pass n: m개`, `OK   <ID> <이름>`, `FAIL <ID> <이름>`, `받음 a/n`, `받지 못함: <ID, …>`, 로그인이 풀렸을 때 `로그인이 풀려 있습니다…`와 `  <SteamCmd> +login <Account> +quit`, 라이선스가 없을 때 `이 계정(…)은 게임(…)을 갖고 있지 않습니다…`, 시간 초과 `TIMEOUT: …`.

- [ ] **Step 1: 시험을 더한다**

`tools/workshop.tests.ps1`의 `# 9.` 블록 뒤에:

```powershell
# 10. -Download: 받지 않은 것만 받고, 실패한 것은 다시 시도한다(가짜 SteamCMD는 첫 호출에서 마지막 항목을 실패시킨다)
$b = NewBox
$o = Run $b $none @{ Download = $true }
Check 'download asks only for the missing items' ($o -match 'pass 1: 2개') $o
Check 'download reports the failed item and retries' ($o -match 'FAIL 555 다섯' -and $o -match 'pass 2: 1개' -and $o -match 'OK   555 다섯') $o
Check 'download picks up a folder without a cache' ([IO.File]::ReadAllText("$($b.content)\555\ModExportData.cache") -eq 'c555') $o
Check 'download creates the missing folder' ((Test-Path "$($b.content)\444\ModExportData.cache") -and $o -match 'OK   444 넷') $o
Check 'download ends with the count' ($o -match '받음 5/5') $o
$o = Run $b $none @{ Download = $true }
Check 'download with nothing missing does not run steamcmd' ($o -match '받음 5/5 \(받을 것이 없습니다\)') $o
$o = Run $b $none @{ Download = $true; Id = '111' }
Check 'download with -Id re-downloads that item' ($o -match 'pass 1: 1개' -and $o -match 'OK   111 하나') $o

# 11. 로그인이 풀려 있으면 멈추고 사용자가 칠 명령을 보인다
$b = NewBox
WriteFile "$($b.root)\steam\expired.flag" ''
$o = Run $b $none @{ Download = $true }
Check 'an expired login stops the download and shows the login command' ($o -match '로그인이 풀려 있습니다' -and $o -match '\+login deepestlight \+quit' -and -not (Test-Path "$($b.content)\444")) $o
```

- [ ] **Step 2: 시험을 돌려 실패를 본다**

Run: `pwsh tools/workshop.tests.ps1`
Expected: 10·11의 줄이 `FAIL`. 1~9는 `PASS`.

- [ ] **Step 3: `RunSteamCmd`와 `DoDownload`를 쓴다**

`tools/workshop.ps1`의 `DoUninstall` 뒤에:

```powershell
# SteamCMD를 돌리고 출력 줄을 돌려준다. 표준 입력은 빈 파일이라 비밀번호를 물으면 바로 끝난다(멈춰 서지 않는다).
# 시간을 넘기면 끊는다. -SteamCmd가 .ps1이면 pwsh로 돌린다(시험용 가짜).
function RunSteamCmd([string[]]$arguments) {
  $outFile = Join-Path $env:TEMP 'tltoybox-workshop-out.txt'
  $inFile = Join-Path $env:TEMP 'tltoybox-workshop-in.txt'
  [IO.File]::WriteAllText($inFile, '')
  if (Test-Path -LiteralPath $outFile) { [IO.File]::Delete($outFile) }
  $isScript = $SteamCmd.ToLower().EndsWith('.ps1')
  $file = if ($isScript) { 'pwsh' } else { $SteamCmd }
  $argList = if ($isScript) { @('-NoProfile', '-File', "`"$SteamCmd`"") + $arguments } else { $arguments }
  $p = Start-Process -FilePath $file -ArgumentList $argList -WorkingDirectory $steamRoot -NoNewWindow -PassThru -RedirectStandardOutput $outFile -RedirectStandardInput $inFile
  if (-not $p.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    $p.Kill()
    return @{ Ok = $false; Lines = @(); Error = "TIMEOUT: $TimeoutMinutes 분 안에 끝나지 않아 끊었습니다(로그인 입력을 기다리는 것일 수 있습니다)" }
  }
  @{ Ok = $true; Lines = @(Get-Content -LiteralPath $outFile -ErrorAction SilentlyContinue); Error = '' }
}

function DoDownload($rows) {
  $pending = @($rows | Where-Object { $Id -or -not (Downloaded $_.Id) })
  if ($pending.Count -eq 0) { "받음 $($rows.Count)/$($rows.Count) (받을 것이 없습니다)"; return }
  $script = Join-Path $env:TEMP 'tltoybox-workshop-run.txt'
  for ($pass = 1; $pass -le 3 -and $pending.Count -gt 0; $pass++) {
    "pass ${pass}: $($pending.Count)개"
    [IO.File]::WriteAllLines($script, [string[]](@($pending | ForEach-Object { "workshop_download_item $AppId $($_.Id)" }) + @('quit')))
    $res = RunSteamCmd @('+login', $Account, '+runscript', "`"$script`"", '+quit')
    if (-not $res.Ok) { $res.Error; break }
    $joined = $res.Lines -join "`n"
    if ($joined -match 'Cached credentials not found|Login Failure|FAILED \(Invalid Password\)|password:') {
      "로그인이 풀려 있습니다. 사용자가 아래 명령으로 한 번 로그인한 뒤(비밀번호·Steam Guard는 사용자가 입력) 다시 돌립니다:"
      "  $SteamCmd +login $Account +quit"
      return
    }
    if ($joined -match 'No workshop depot defined') { "이 계정($Account)은 게임($AppId)을 갖고 있지 않습니다. 게임을 가진 계정으로 로그인해야 받을 수 있습니다."; return }
    foreach ($r in $pending) {
      if ($joined -match "Success\. Downloaded item $($r.Id)\b") { "OK   $($r.Id) $($r.Korean)" } else { "FAIL $($r.Id) $($r.Korean)" }
    }
    $pending = @($pending | Where-Object { -not ($joined -match "Success\. Downloaded item $($_.Id)\b") })
  }
  $got = @($rows | Where-Object { Downloaded $_.Id }).Count
  "받음 $got/$($rows.Count)"
  if ($pending.Count -gt 0) { "받지 못함: $(($pending | ForEach-Object Id) -join ', ')" }
}
```

끝의 분기에 `elseif ($Download) { DoDownload $chosen }`를 더한다(`-Status` 다음).

- [ ] **Step 4: 시험을 돌려 통과를 본다**

Run: `pwsh tools/workshop.tests.ps1`
Expected: 1~11 모두 `PASS`, `FAILS=0`.

실제 SteamCMD로: `pwsh tools/workshop.ps1 -Download` → `받음 15/15 (받을 것이 없습니다)`(모두 받아져 있다). `pwsh tools/workshop.ps1 -Download -Id 2839261049` → `pass 1: 1개`, `OK   2839261049 진·진종지보: 전승 심법`, `받음 1/1`(SteamCMD가 같은 판을 다시 확인한다. 1분쯤). 받은 폴더의 `ModExportData.cache`가 그대로 있는지 본다.

- [ ] **Step 5: 커밋**

```bash
git add tools/workshop.ps1 tools/workshop.tests.ps1
git commit -m "feat: 창작마당 모드 도구 — -Download(SteamCMD 캐시 로그인, 세 번 재시도, 로그인 풀림·라이선스 없음 안내)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 4: 회차 스크립트가 회차 중 설치한 모드 폴더를 되돌린다

**Files:**
- Modify: `tools/DevRemote/session.ps1` (변수 둘, `-Begin`의 `$keptBackups` 쓰기 뒤, `-End`의 자동 백업 정리 뒤)
- Modify: `tools/DevRemote/session.tests.ps1` (`NewBox`, `Run`, 끝에 시험 24·25)

**Interfaces:**
- Produces: `-Begin`이 `.devremote\session-modfolders.txt`에 `<GameDir>\ModExportData\` 바로 아래 폴더 이름을 줄마다 적는다. `-End`가 그 파일에 없는 폴더를 지우고 `WORKSHOP FOLDERS REMOVED: n`을 보인 뒤 파일을 없앤다. `Mod_<ModId>`는 지우지 않는다. Task 6의 회차가 이것에 기댄다.

- [ ] **Step 1: 시험 상자에 게임 폴더를 더하고 시험 둘을 쓴다**

`tools/DevRemote/session.tests.ps1`의 `NewBox`를 바꾼다(`game` 칸과 폴더 둘):

```powershell
function NewBox {
  $root = Join-Path $env:TEMP ('tltoybox-session-tests-' + [guid]::NewGuid().ToString('N'))
  $box = [pscustomobject]@{ root = $root; save = "$root\save\CacheData"; work = "$root\work"; data = "$root\data"; game = "$root\game" }
  foreach ($folder in "$($box.save)\slot1", $box.work, $box.data, "$($box.game)\ModExportData\Mod_sWP6UL") { [void][IO.Directory]::CreateDirectory($folder) }
  [IO.File]::WriteAllText("$($box.save)\DataGloble.cache", 'global-1')
  [IO.File]::WriteAllText("$($box.save)\slot1\a.cache", 'a-1')
  [IO.File]::WriteAllText("$($box.game)\ModExportData\Mod_sWP6UL\ModExportData.cache", 'ours')
  $boxes.Add($root)
  $box
}
```

`Run`이 `-GameDir`을 준다(모든 시험이 실제 게임 폴더를 보지 않게):

```powershell
function Run($box, [string]$process, [hashtable]$more) {
  try {
    $out = & $session -DryRun -SaveDir $box.save -WorkDir $box.work -DataDir $box.data -GameDir $box.game -ProcessName $process @more
    ($out -join "`n")
  } catch { 'THROWN: ' + $_.Exception.Message }
}
```

`# 23.` 블록 뒤(정리 `foreach ($root in $boxes)` 앞)에:

```powershell
# 24. 회차 동안 ModExportData에 생긴 폴더(창작마당 모드)는 -End가 지운다
$b = NewBox
[void](Run $b $none @{ Begin = 'x' })
[void][IO.Directory]::CreateDirectory("$($b.game)\ModExportData\2876769227")
[IO.File]::WriteAllText("$($b.game)\ModExportData\2876769227\ModExportData.cache", 'ws')
$o = Run $b $none @{ End = 'x' }
Check 'End removes a workshop folder the session installed' ($o -match 'WORKSHOP FOLDERS REMOVED: 1' -and -not (Test-Path "$($b.game)\ModExportData\2876769227") -and -not (Test-Path "$($b.work)\session-modfolders.txt")) $o

# 25. 회차 전부터 있던 폴더와 편집기 폴더(Mod_sWP6UL)는 남긴다
$b = NewBox
[void][IO.Directory]::CreateDirectory("$($b.game)\ModExportData\2819712935")
[void](Run $b $none @{ Begin = 'x' })
$o = Run $b $none @{ End = 'x' }
Check 'End keeps folders that were there before and the editor folder' ($o -match 'WORKSHOP FOLDERS REMOVED: 0' -and (Test-Path "$($b.game)\ModExportData\2819712935") -and (Test-Path "$($b.game)\ModExportData\Mod_sWP6UL\ModExportData.cache")) $o
```

- [ ] **Step 2: 시험을 돌려 실패를 본다**

Run: `pwsh tools/DevRemote/session.tests.ps1`
Expected: 24·25가 `FAIL`(`WORKSHOP FOLDERS REMOVED`가 나오지 않는다), 1~23은 `PASS`, `FAILS=2`.

- [ ] **Step 3: `session.ps1`을 고친다**

변수 선언(`$keptBackups` 줄 뒤)에:

```powershell
$modsDir = Join-Path $GameDir 'ModExportData'            # 게임의 로컬 모드 폴더(편집기와 창작마당 모드)
$keptModFolders = Join-Path $dir 'session-modfolders.txt'  # 회차 전부터 ModExportData에 있던 폴더 이름들
```

`-Begin`에서 `[IO.File]::WriteAllText($keptBackups, ($names -join "`n"))` 줄 뒤에:

```powershell
  # 회차 전부터 게임의 로컬 모드 폴더에 있던 폴더 이름을 적어 둔다. -End가 그 밖의 것(회차 동안 설치한 창작마당 모드)만 지운다.
  $modNames = @(if (Test-Path -LiteralPath $modsDir) { Get-ChildItem -LiteralPath $modsDir -Directory | ForEach-Object Name })
  [IO.File]::WriteAllText($keptModFolders, ($modNames -join "`n"))
```

`-End`에서 자동 백업 정리 블록(`[IO.File]::Delete($keptBackups)`로 끝나는 `if (Test-Path $keptBackups) { … }`) 뒤, "오래된 회차의 복사본" 주석 앞에:

```powershell
  # 회차 동안 ModExportData에 생긴 폴더(회차 안에서 설치한 창작마당 모드)를 지운다. 회차 전부터 있던 것과 편집기(Mod_<ModId>)는 남긴다.
  if (Test-Path $keptModFolders) {
    $keepMods = @([IO.File]::ReadAllText($keptModFolders) -split "`n" | Where-Object { $_ })
    $removedMods = 0
    if (Test-Path -LiteralPath $modsDir) {
      foreach ($entry in Get-ChildItem -LiteralPath $modsDir -Directory) {
        if ($keepMods -notcontains $entry.Name -and $entry.Name -ne "Mod_$ModId" -and $entry.FullName.StartsWith($modsDir + '\')) {
          [IO.Directory]::Delete($entry.FullName, $true)
          $removedMods++
        }
      }
    }
    [IO.File]::Delete($keptModFolders)
    "WORKSHOP FOLDERS REMOVED: $removedMods"
  }
```

파일 머리의 주석(`-End <이름>` 줄)에 "회차 동안 ModExportData에 생긴 폴더를 지운다"를 덧붙인다.

- [ ] **Step 4: 시험을 돌려 통과를 본다**

Run: `pwsh tools/DevRemote/session.tests.ps1`
Expected: 1~25 모두 `PASS`, `FAILS=0`.

- [ ] **Step 5: 커밋**

```bash
git add tools/DevRemote/session.ps1 tools/DevRemote/session.tests.ps1
git commit -m "feat: 회차 스크립트 — -Begin이 ModExportData의 폴더를 적고 -End가 회차 중 생긴 폴더(창작마당 모드)를 지운다

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 5: 개발 명령 `wsmods`를 가져오고 두 구성으로 빌드한다

**Files:**
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs`, Create: `src/ModCode/ModMain/Dev/DevCommandsWorkshop.cs` (실험 브랜치 커밋 `f34dec4`를 cherry-pick)

**Interfaces:**
- Produces: 회차 안 `mod.ps1 -Cmd "wsmods"` → `wsmods -> path=… loaded=<수> cache=… paths=<soleID|경로;…> mods=<soleID:이름:판:창작마당 번호;…> ns=<DLL 경로;…> order=<폴더 이름=isLoad;…>`(결과 파일 `.devremote\mod-out.txt`, UTF-8). Task 6이 쓴다.

- [ ] **Step 1: 실험 커밋을 가져온다**

```bash
git cherry-pick f34dec4
git log --oneline -1
git show --stat HEAD | tail -3
```

Expected: 충돌 없이 커밋이 하나 생기고 `Dev/DevCommandsWorkshop.cs`(75줄)와 `Dev/DevCommands.cs`(+5)가 들어 있다. 커밋 메시지는 그대로 둔다(`dev: 창작마당 실험 명령 wsmods …`).

- [ ] **Step 2: 두 구성으로 빌드한다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check"
ls src/ModCode/ModMain/bin/Release/*.dll
```

Expected: 둘 다 `경고 0개`, `오류 0개`. `bin\Release`에 `MOD_sWP6UL.dll` 하나. 배포 구성은 `Dev\`를 빼므로 `wsmods`가 없는 것이 맞다.

- [ ] **Step 3: 커밋할 것이 없는지 본다**

```bash
git status --short
```

Expected: 비어 있다(cherry-pick이 커밋을 만들었다).

---

### Task 6: 고정본 회차에서 15개 설치를 검증하고 05 문서 41절을 쓴다

**Files:**
- Modify: `docs/05-editor-v0.1.md` (40절 뒤에 41절)

**Interfaces:**
- Consumes: Task 2의 `-Install`/`-Yes`, Task 4의 `WORKSHOP FOLDERS REMOVED`, Task 5의 `wsmods`, `tools/DevRemote/session.ps1`, `tools/DevRemote/mod.ps1`, `tools/research/ui-check.ps1`, `tools/DevRemote/win.ps1`.

- [ ] **Step 1: 사용자의 게임이 꺼져 있는지 보고 회차를 연다**

```bash
tasklist | grep -i guigubahuang || echo "not running"
pwsh tools/DevRemote/session.ps1 -Begin ws-1015b -Fixture base
```

Expected: 켜져 있으면 사용자에게 끄도록 알리고 기다린다(임의로 끄지 않는다). 꺼져 있으면 `FIXTURE LOADED: base` … `WORLD READY`.

- [ ] **Step 2: 기준값을 읽는다**

```bash
pwsh tools/DevRemote/mod.ps1 -Cmd "wsmods" > /dev/null; cat .devremote/mod-out.txt
```

Expected: `loaded=1`, `paths=sWP6UL|…\Mod_sWP6UL;`, `order=Mod_sWP6UL=True;`.

- [ ] **Step 3: 계획을 보고 15개를 넣는다**

```bash
pwsh tools/workshop.ps1 -Install
pwsh tools/workshop.ps1 -Install -Yes
ls "/e/Games/Tale of Immortal/ModExportData"
```

Expected: 계획 15줄 모두 `새로 설치`(건너뜀 0)와 `PLAN ONLY`. `-Yes`에서 `INSTALLED` 15줄, `게임이 실행 중입니다. 새로 넣은 모드는 다음 게임 시작부터 읽힙니다.`, `설치 15개`. 폴더 16개(`Mod_sWP6UL` + 15). 회차 안이므로 `-End`가 되돌린다(사용자 확인은 회차 밖 설치 때 받는다).

- [ ] **Step 4: 재시작해 로드를 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Restart
grep -a "加载模组" "$USERPROFILE/AppData/LocalLow/guigugame/guigubahuang/Player.log"
pwsh tools/DevRemote/mod.ps1 -Cmd "wsmods" > /dev/null; cat .devremote/mod-out.txt
grep -a -i "exception\|error" "/e/Games/Tale of Immortal/MelonLoader/Latest.log" | head -20
```

Expected: `WORLD READY`(mod warning 창이 넘어간다). `加载模组` 줄 16개. `wsmods`의 `loaded=16`, `mods`에 창작마당 번호 15개가 모두 있고 각 모드 ID(soleID)가 보인다(05 문서에 적는다), `ns`에 DLL 경로 4개(`Fatury.dll`을 포함한 `2814703549`, `2832167498`, `2839261049`, `3001496635`). `Latest.log`의 예외는 적기만 한다(다른 모드의 것은 고치지 않는다). 예외로 편집기가 열리지 않으면 그 모드를 `-Uninstall -Yes`가 아니라 폴더 이름을 적어 두고 `-End` 뒤 사용자에게 보고한다(회차 안에서는 게임이 켜져 있어 제거가 거부된다).

`加载模组` 줄의 순서(발견 순서)를 적어 둔다. Fatury(`2814703549`)가 `2815562168`보다 앞인지 본다.

- [ ] **Step 5: 편집기가 여전히 동작하는지 본다**

```bash
pwsh tools/research/ui-check.ps1 -Part base
pwsh tools/research/ui-check.ps1 -Part tabs
```

Expected: 둘 다 `FAILS=0`. 실패하면 어떤 줄인지 적고(다른 모드와의 충돌이면 그 모드 이름), 고치지 않은 채 05 문서에 적는다.

- [ ] **Step 6: 게임 모드 화면의 순서를 캡처해 본다(되면)**

세계 안에서는 모드 화면을 열 수 없다. `uilist`로 열린 화면을 보고, 게임 메뉴(ESC는 전달되지 않으므로 `rc.ps1 -Cmd "click 1870 40"` 같은 오른쪽 위 메뉴 버튼의 좌표를 `rc.ps1 -Cmd "hit x y"`로 찾는다)에서 "돌아가기" `(959, 391)`로 주 화면에 가서 모드 버튼을 찾아 누르고 `win.ps1 -Do shot -Out .devremote/ws-modlist.png -Scale 0.6`으로 캡처한다. 10분 안에 되지 않으면 그만두고 "미확인(사용자 확인 항목)"으로 적는다. 주 화면으로 나간 뒤에는 `-Restart`로 다시 세계에 들어가지 않아도 된다(다음 단계가 `-End`).

- [ ] **Step 7: 회차를 닫는다**

```bash
pwsh tools/DevRemote/session.ps1 -End ws-1015b
ls "/e/Games/Tale of Immortal/ModExportData"
```

Expected: `SAVE RESTORED AND IDENTICAL`, `WORKSHOP FOLDERS REMOVED: 15`, 폴더는 `Mod_sWP6UL`뿐.

- [ ] **Step 8: 05 문서 41절을 쓴다**

`docs/05-editor-v0.1.md`의 40절 끝에 이어 붙인다:

```markdown
## 41. 창작마당 모드 받기·설치(G2a) (2026-10-15 추가)

스펙 `docs/superpowers/specs/2026-10-15-workshop-install-design.md`, 계획 `plans/2026-10-15-workshop-install.md`. 사용자 요청 4(스팀 모드 반영)의 첫 묶음. 받는 길은 Steam 계정 `deepestlight`가 게임을 갖게 되면서 열렸다(2026-10-15 저녁).

### 규칙

- 게임은 `ModExportData\<폴더>\ModExportData.cache`만 읽는다(한 단계). 폴더 이름은 아무거나 되고 `Mod_` 접두는 필요 없다. 창작마당 항목의 `<ID>\debug\Mod_x\`는 꺼내 평평하게 넣어야 한다.
- 도구는 `tools/workshop.ps1`(목록 `tools/workshop/mods.txt`, 15개). `-Status`, `-Download`, `-Install [-Yes]`, `-Uninstall [-Yes]`. `-Yes` 없이는 게임 폴더에 아무것도 쓰지 않는다. 설치 자리는 `ModExportData\<창작마당ID>\`.
- 회차 스크립트의 `-End`가 회차 중 `ModExportData\`에 생긴 폴더를 지운다(`WORKSHOP FOLDERS REMOVED: n`).
- 제외: `2945768219`(MySql.Data.dll 포함), `3681025074`(사용자 시험용).

### 실측 (회차 `ws-1015a`)

(스펙 2절의 표를 옮겨 적는다: 읽는 자리, 한 단계 더 깊은 것, 받은 항목의 모양, 모드 ID와 창작마당 번호, 새 모드의 기본, 로드 순서, 받기, DLL이 든 것.)

### 검증 (회차 `ws-1015b`)

(4~7단계의 결과를 적는다: `加载模组` 줄 수와 순서, `wsmods`의 `loaded`와 모드 ID 15개의 표(창작마당 번호 | 모드 ID | 이름 | 판), `ns`의 DLL 경로, `Latest.log`의 예외, `ui-check base`·`tabs`의 결과, 모드 화면 캡처의 결과 또는 "미확인", `-End`의 줄.)

### 확인하지 못한 것, 한계

- 한 번에 여러 폴더를 처음 발견할 때의 로드 순서(회차 `ws-1015b`의 `加载模组` 순서를 적되 규칙으로 단정하지 않는다). 순서 바꾸기는 게임의 모드 화면이 한다.
- 로드 순서가 세이브 폴더의 `DataGloble.cache`에 남는다는 것은 추정이다.
- 실제 SteamCMD가 캐시가 풀렸을 때 내는 문구(스크립트는 `Cached credentials not found`, `Login Failure`, `FAILED (Invalid Password)`, `password:`를 본다). 표준 입력을 빈 파일로 주어 프롬프트에서 멈추지 않게 했지만 실제로는 미확인.
- `robocopy /MIR` 갱신은 설치본에만 있던 파일을 지운다. 모드가 자기 폴더에 파일을 쓰는지 미확인.
- DLL 모드 4개와 편집기의 충돌은 회차 한 번(세계 진입, `base`·`tabs`)으로만 봤다.
- 회차 이름: `ws-1015a`(실험), `ws-1015b`(검증).
```

괄호 안의 "적는다"는 4~7단계에서 실제로 나온 값으로 채운다. 값을 얻지 못한 것은 "미확인"과 사유.

- [ ] **Step 9: 커밋**

```bash
git add docs/05-editor-v0.1.md
git commit -m "docs: 05 문서 41절 — 창작마당 모드 받기·설치(G2a)의 규칙, 실측, 회차 ws-1015b 검증

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 7: 인계·CLAUDE.md를 고치고 검토·병합한다

**Files:**
- Modify: `docs/06-handoff.md` (1절, 4절, 5절 "회차 안에서 쓰는 도구", 8.3b절, 8.7절)
- Modify: `CLAUDE.md` (게임 폴더 규칙)

- [ ] **Step 1: CLAUDE.md의 게임 폴더 규칙에 한 줄을 더한다**

"게임 폴더는 읽기 전용으로 다룬다…" 줄 뒤에:

```markdown
- 창작마당 모드는 `tools/workshop.ps1 -Install -Yes`로 `ModExportData\<창작마당ID>\`에 설치한다. `-Yes`는 사용자 확인 뒤에만 붙인다(`-Yes` 없이는 계획만 보인다). 목록은 `tools/workshop/mods.txt`.
```

- [ ] **Step 2: 인계 문서를 고친다**

- 1절 "저장소" 줄: 작업 브랜치 `feat/workshop-install`을 `develop`에 병합했다고 적고, 실험 브랜치 열에 `feat/workshop-research`를 더한다.
- 1절 "직전 작업": "G2a 창작마당 모드 받기·설치(05 문서 41절): 받는 길이 열려(`deepestlight`가 게임 보유) 16개를 받았고, 도구 `tools/workshop.ps1`과 목록 15개를 만들었다. 회차 `ws-1015b`에서 15개 설치를 검증했다. 사용자의 게임에는 아직 설치하지 않았다(회차 밖 `-Install -Yes`는 사용자 확인 뒤)."
- 1절 "다음에 할 일": (1) 사용자 확인 뒤 `pwsh tools/workshop.ps1 -Install -Yes`로 실제 설치(게임을 끈 상태에서). (2) G2b 모드 공법의 비급(3390738990·2839261049의 공법이 공법 탭 `SkillCatalog`에 나오는지, 39절의 기술 서적 지급이 되는지 실험부터). (3) G2c 한글화(번역을 덮을 자리 실험: `LocalText` 표 또는 `GameTool.LS`. 모드 이름의 서식 태그). 그 밖의 후보(8.3a절 월말 보고, 8.5절 확인 대기)는 그대로.
- 4절 "빌드와 설치"에 `pwsh tools/workshop.ps1 -Status | -Download | -Install [-Yes] | -Uninstall [-Yes]   # 창작마당 모드(목록 tools/workshop/mods.txt)` 줄과 "게임이 켜져 있으면 설치된 폴더의 갱신과 제거가 거부된다" 줄.
- 5절 "회차 안에서 쓰는 도구"의 목록에 `- 창작마당: wsmods(ModMgr의 경로·로드 목록·모드 자료·순서. 05 문서 41절)`. 회차 스크립트 설명의 "`-End`가 되돌리는 것"에 "회차 중 `ModExportData\`에 생긴 폴더(창작마당 모드)를 지운다".
- 8.3b절 제목을 "G2 창작마당 모드 연동 — G2a 끝남(2026-10-15), G2b·G2c 남음"으로 바꾸고 끝에 "2026-10-15 저녁: 받는 길이 열렸다(`deepestlight`가 게임 보유, `app_status`가 `Subscribed,Permanent`). G2a는 05 문서 41절. 남은 것은 G2b(모드 공법의 비급)와 G2c(한글화)이고, 설계 전 실험 둘(모드 공법이 기술 목록에 나오는지, 번역을 덮을 자리)이 남아 있다. 받은 16개와 설치 목록 15개의 차이(2945768219 제외)는 스펙 1절." 문단을 더한다.
- 8.5절(확인 대기)에: "창작마당 모드 15개를 실제로 설치한 뒤 플레이했을 때의 이상 여부, 게임 모드 화면에서 Fatury가 맨 위인지(회차에서 캡처하지 못했으면)".
- 8.7절 회차 이름에 `ws-1015a`, `ws-1015b`.

- [ ] **Step 3: 커밋**

```bash
git add CLAUDE.md docs/06-handoff.md
git commit -m "docs: 인계·CLAUDE.md — 창작마당 모드 도구(workshop.ps1), G2a 끝남, 다음 일(실제 설치, G2b, G2c)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

- [ ] **Step 4: 검토자 한 명이 브랜치 전체를 본다**

읽기 전용 서브에이전트에게 `git diff develop...feat/workshop-install`과 스펙을 주고 본다. 범위: 이 브랜치의 변경만. 지시에 "이 검토만 하고 보고 뒤 정지. 파일을 고치지 말 것. 모든 git 명령에 `-C E:\TlToybox`를 쓸 것"을 넣는다. 보는 것: `-Yes` 없이 게임 폴더에 쓰는 길이 있는지, `-Uninstall`이 목록 밖 폴더를 지울 수 있는 길이 있는지, `DiffCount`가 같은데 다르다고(또는 반대로) 할 수 있는지, `RunSteamCmd`가 멈춰 설 수 있는지, 시험이 실제 게임 폴더를 볼 수 있는지, `session.ps1`의 `-End`가 `Mod_sWP6UL`을 지울 수 있는지. 실제 결함은 고치고(시험을 먼저 더한 뒤) 다시 시험. "판단 보류"는 05 문서 41절의 "확인하지 못한 것"에 적는다.

- [ ] **Step 5: 시험을 모두 다시 돌리고 병합한다**

```bash
pwsh tools/workshop.tests.ps1
pwsh tools/DevRemote/session.tests.ps1
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
git checkout develop
git merge --no-ff feat/workshop-install -m "Merge branch 'feat/workshop-install' into develop"
git log --oneline -3
```

Expected: 두 시험 `FAILS=0`, 빌드 오류 0·경고 0, 병합 커밋 하나. 편집기 DLL은 바뀌지 않았으므로(`wsmods`는 개발 구성에만) `tools/install.ps1`은 돌리지 않아도 된다. 다만 인계 1절의 "게임에 설치된 것"이 `develop`의 마지막 코드와 같다고 적으려면 돌린다(사용자의 게임이 꺼져 있을 때). 작업 브랜치 삭제와 실제 설치(`-Install -Yes`)는 사용자에게 묻는다.
