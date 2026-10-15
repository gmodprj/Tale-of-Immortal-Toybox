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
  $rows.ToArray()
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
  "설치 ${done}개"
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

$chosen = Chosen (ReadList)
$modes = @(@($Status, $Download, $Install, $Uninstall) | Where-Object { $_ }).Count
if ($modes -ne 1) { throw '-Status, -Download, -Install, -Uninstall 가운데 하나를 고릅니다.' }
if ($Status) { ShowStatus $chosen }
elseif ($Download) { DoDownload $chosen }
elseif ($Install) { DoInstall $chosen }
elseif ($Uninstall) { DoUninstall $chosen }
$global:LASTEXITCODE = 0
exit 0
