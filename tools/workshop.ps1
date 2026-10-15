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

$chosen = Chosen (ReadList)
$modes = @(@($Status, $Download, $Install, $Uninstall) | Where-Object { $_ }).Count
if ($modes -ne 1) { throw '-Status, -Download, -Install, -Uninstall 가운데 하나를 고릅니다.' }
if ($Status) { ShowStatus $chosen }
$global:LASTEXITCODE = 0
exit 0
