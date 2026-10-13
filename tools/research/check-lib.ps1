# 게임 내 검증 스크립트가 함께 쓰는 함수. datebag-check.ps1과 ui-check.ps1이 점(.)으로 불러 쓴다.
# 줄마다 PASS/FAIL이 나오고 Finish가 마지막 줄 FAILS=<수>를 낸다. "SHOT <파일>"은 눈으로 확인할 캡처다.
$ErrorActionPreference = 'Stop'
$tools = Join-Path (Split-Path -Parent $PSScriptRoot) 'DevRemote'
$work = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.devremote'
$data = Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\TlToybox'
$log = 'E:\Games\Tale of Immortal\MelonLoader\Latest.log'
$script:fails = 0
if (-not (Test-Path (Join-Path $work 'session.name'))) { throw 'session.ps1 -Begin으로 연 회차 안에서만 실행합니다(세이브와 모드 파일을 바꾸는 시험이다).' }

function Cmd([string]$c, [int]$wait = 600) { (& "$tools\mod.ps1" -Cmd $c -WaitMs $wait) -join "`n" }
function Rc([string]$c, [int]$wait = 800) { (& "$tools\rc.ps1" -Cmd $c -NoShot -WaitMs $wait) -join "`n" }
# 게임 창에 키 하나를 보낸다. 0x2D는 Insert, 0x0D는 Enter.
function Key([int]$vk, [int]$wait = 500) {
  & "$tools\win.ps1" -Do key -Vk $vk | Out-Null
  Start-Sleep -Milliseconds $wait
}
function Shot([string]$name) {
  $file = Join-Path $work "check-$name.png"
  & "$tools\win.ps1" -Do shot -Out $file -Scale 0.6 | Out-Null
  "SHOT $file"
}
# 모드가 로그에 남긴 경고·오류 줄. 로그의 한글은 UTF-8이 아니어서(CP949) 문구로 찾을 수 없다.
# 정상 줄(Init, Destroy, state, 단축키 안내)이 아닌 "[TlToybox] " 줄을 고른다. 명령과 변경 이력은 "[TlToybox][...]"이라 걸리지 않는다.
function ModWarnings {
  Get-Content -LiteralPath $log | Where-Object { $_ -match '\[TlToybox\] ' -and $_ -notmatch '\[TlToybox\] (Init|Destroy|state: )' -and $_ -notmatch ': (Insert|F2|F3)\s*$' }
}
# 버튼 콜백 안의 예외는 화면에 나오지 않고 로그의 "trampoline" 줄에만 남는다. 그 줄의 수.
function Traps { @(Get-Content -LiteralPath $log | Where-Object { $_ -match 'trampoline' }).Count }
function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
# 편집기에서 그 이름의 요소를 찾아 가운데의 게임 좌표를 돌려준다. 이름 끝의 *는 "이 글자로 시작"이다.
function Pos([string]$name) {
  $o = Cmd "uipos $name" 100
  if ($o -notmatch '-> (\S+) at (-?\d+),(-?\d+) size=') { throw "편집기에서 요소를 찾지 못했습니다: $name ($o)" }
  [pscustomobject]@{ name = $Matches[1]; x = [int]$Matches[2]; y = [int]$Matches[3] }
}
# 그 이름의 요소가 있고, 그 자리의 클릭이 그 요소로 가는지(가려지지 않았는지).
function IsAt([string]$name) {
  $o = Cmd "uipos $name" 100
  if ($o -notmatch '-> (\S+) at (-?\d+),(-?\d+) size=') { return $false }
  $real = $Matches[1]; $x = $Matches[2]; $y = $Matches[3]
  (Rc "hit $x $y" 150) -match "/$([regex]::Escape($real))\s*$"
}
# 그 이름의 버튼을 찾아 그 자리를 누른다. 다른 것이 눌렸거나 버튼이 꺼져 있으면 멈춘다.
function Press([string]$name, [int]$wait = 800) {
  $p = Pos $name
  $o = Rc "click $($p.x) $($p.y)" $wait
  # 시험 중에 게임 메뉴(돌아가기·저장…)가 떠서 편집기를 가릴 때가 있다(게임 창의 포커스 변화로 추정). 돌아가기를 눌러 닫고 한 번 더 누른다.
  if ($o -match '/GameMemu/') {
    "INFO game menu was open; closed it" | Write-Host
    Rc 'click 959 391' 600 | Out-Null
    $o = Rc "click $($p.x) $($p.y)" $wait
  }
  if ($o -notmatch "Button\.onClick: $([regex]::Escape($p.name))\s*$") { throw "버튼이 눌리지 않았습니다: $name ($o)" }
  $o
}
function Finish {
  "FAILS=$script:fails"
  if ($script:fails -gt 0) { exit 1 }
  exit 0
}
