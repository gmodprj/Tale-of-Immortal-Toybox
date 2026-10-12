# 날짜·가방 기능의 게임 내 검증(docs/superpowers/specs/2026-10-12-date-bag-design.md 6절).
# tools\DevRemote\session.ps1 -Begin으로 게임이 떠서 세계에 들어가 있어야 한다.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다. "SHOT <파일>"은 눈으로 확인할 캡처다.
param([Parameter(Mandatory)][string]$Part)
$ErrorActionPreference = 'Stop'
$tools = Join-Path (Split-Path -Parent $PSScriptRoot) 'DevRemote'
$work = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.devremote'
$data = Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\TlToybox'
$log = 'E:\Games\Tale of Immortal\MelonLoader\Latest.log'
$script:fails = 0

function Cmd([string]$c, [int]$wait = 600) { (& "$tools\mod.ps1" -Cmd $c -WaitMs $wait) -join "`n" }
function Rc([string]$c, [int]$wait = 800) { (& "$tools\rc.ps1" -Cmd $c -NoShot -WaitMs $wait) -join "`n" }
function Shot([string]$name) {
  $file = Join-Path $work "check-$name.png"
  & "$tools\win.ps1" -Do shot -Out $file -Scale 0.6 | Out-Null
  "SHOT $file"
}
function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
function State {
  $o = Cmd 'wdate'
  if ($o -notmatch 'worldDay=(\d+) date=(\d+)-(\d+)-(\d+) roundDay=(\d+)/(\d+) residueDay=(-?\d+)') { throw "wdate 출력을 읽지 못했습니다: $o" }
  [pscustomobject]@{ day = [int]$Matches[1]; y = [int]$Matches[2]; m = [int]$Matches[3]; d = [int]$Matches[4]; round = [int]$Matches[5]; res = [int]$Matches[7] }
}
function Grid {
  $o = Cmd 'grid'
  if ($o -notmatch 'cur=(\d+) max=(\d+) base=(\d+) add=\[(.*?)\]') { throw "grid 출력을 읽지 못했습니다: $o" }
  [pscustomobject]@{ cur = [int]$Matches[1]; max = [int]$Matches[2]; add = $Matches[4] }
}
# 갈 수 있는 방향을 찾아 그 칸과 원래 칸을 오가며 $steps번 이동한다. 실제로 이동한 횟수를 돌려준다.
function Walk([int]$steps) {
  $done = 0
  foreach ($dir in @(1, 0), @(-1, 0), @(0, 1), @(0, -1)) {
    if ((Cmd "wmove $($dir[0]) $($dir[1])" 1500) -notmatch 'IsCreateAction=0 CreateAction=0') { continue }
    $done = 1; $sign = -1
    while ($done -lt $steps) {
      if ((Cmd "wmove $($dir[0] * $sign) $($dir[1] * $sign)" 1500) -notmatch 'IsCreateAction=0 CreateAction=0') { break }
      $done++; $sign = -$sign
    }
    break
  }
  $done
}
function CloseMonthLog {
  for ($i = 0; $i -lt 8; $i++) {
    if ((Rc 'hit 959 629' 200) -match 'MonthLogBig') { Rc 'click 959 629' 1500 | Out-Null } else { break }
  }
}
# 이름을 History로 하면 PowerShell의 별칭(Get-History)이 먼저 걸린다.
function ChangeHistory([int]$tail = 80) { (Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail $tail -Encoding UTF8) -join "`n" }

switch ($Part) {
  'base' {
    $s = State; $g = Grid
    Check 'date parts are consistent' ($s.round -eq $s.day % 31 -and $s.res -eq 30 - $s.round) "day=$($s.day) round=$($s.round) res=$($s.res)"
    Check 'grid is readable' ($g.max -gt 0 -and $g.add -notmatch 'TlToybox') $g.add
    Check 'walking works' ((Walk 2) -eq 2)
  }
  default { throw "알 수 없는 Part: $Part" }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
exit 0
