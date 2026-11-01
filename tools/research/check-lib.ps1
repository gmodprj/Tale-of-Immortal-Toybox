# 게임 내 검증 스크립트가 함께 쓰는 함수. tools/research의 검사 아홉(major-breakthrough-check.ps1 빼고)이 점(.)으로 불러 쓴다.
# 줄마다 PASS/FAIL이 나오고 Finish가 마지막 줄 FAILS=<수>를 낸다. "SHOT <파일>"은 눈으로 확인할 캡처다.
$ErrorActionPreference = 'Stop'
$tools = Join-Path (Split-Path -Parent $PSScriptRoot) 'DevRemote'
$work = & (Join-Path (Split-Path -Parent $PSScriptRoot) 'DevRemote\devremote-dir.ps1')   # .devremote 경로는 한 곳(05 문서 49절)
$data = Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\TlToybox'
$log = 'E:\Games\Tale of Immortal\MelonLoader\Latest.log'
$script:fails = 0
if (-not (Test-Path (Join-Path $work 'session.name'))) { throw 'session.ps1 -Begin으로 연 회차 안에서만 실행합니다(세이브와 모드 파일을 바꾸는 시험이다).' }

# $reply: 답을 기다리는 최대 시간(mod.ps1 -ReplyMs). $wait: 답을 받은 뒤 쉬는 시간.
function Cmd([string]$c, [int]$wait = 600, [int]$reply = 8000) { (& "$tools\mod.ps1" -Cmd $c -WaitMs $wait -ReplyMs $reply) -join "`n" }
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
  # 검사 errors가 일부러 내는 시험 예외(test-exception)의 줄과, 깨진 공법 망각의 대체 경로가 남기는 "(fallback)" 줄(검사 actions가 따로 센다)은 경고로 세지 않는다.
  Get-Content -LiteralPath $log | Where-Object { $_ -match '\[TlToybox\] ' -and $_ -notmatch '\[TlToybox\] (Init|Destroy|state: |translations: |texts: )' -and $_ -notmatch ': (Insert|F2|F3)\s*$' -and $_ -notmatch 'InvalidOperationException: test-exception' -and $_ -notmatch '\(fallback\) ' }
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
# ---- 검사 아홉에 복붙돼 있던 도우미(05 문서 91절, 13 문서 C1·C8). 뜻이 다른 사본은 각 검사에 남겼다. ----

# 전투에 들어간다: fight → 전투 진입 확인(960,684) → 전투 개시(956,749. -NoStart면 누르지 않는다). 고정본 인물은 수준 20까지 안전.
function EnterBattleScene([int]$level = 20, [switch]$NoStart) {
  Cmd "fight 1011 $level" 6000 | Out-Null
  Rc 'click 960 684' 12000 | Out-Null
  if (-not $NoStart) { Rc 'click 956 749' 4000 | Out-Null }
}
# 전투를 끝내고 지도로 나온다(06 문서 8.0절의 규칙): 전투 개시를 안 눌렀으면 누르고, bend를 최대 3번, 결과 창의 확인 단추가
# 실제로 맞을 때까지 기다렸다 누르고, MapMain과 inBattle=False까지 기다린다. 어기면 다음 전투가 빈 화면으로 뜬다(회차 rx-g·rx-n-1027).
function LeaveBattleScene([string]$shotName = 'leave-fail') {
  Start-Sleep -Seconds 3
  if ((Rc 'hit 956 749' 200) -match 'btnStart') { Rc 'click 956 749' 4000 | Out-Null }
  for ($t = 0; $t -lt 3 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $t++) {
    "INFO bend: $(Cmd 'bend' 3000)"
    for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
  }
  if ((Cmd 'uilist' 100) -notmatch 'BattleEnd') { Shot $shotName }
  for ($i = 0; $i -lt 20 -and (Rc 'hit 959 831' 200) -notmatch 'btnOK'; $i++) { Start-Sleep -Milliseconds 500 }
  # 확인 단추가 맞을 때만 누른다(이미 지도라면 지도의 그 자리를 누르지 않는다. 최종 리뷰).
  if ((Rc 'hit 959 831' 200) -match 'btnOK') { Rc 'click 959 831' 2000 | Out-Null }
  # 지도가 돌아올 때까지 최대 60번(0.5초 + 명령 답 시간. 첫 전투 뒤의 장면 적재는 15초를 넘길 때가 있다. 회차 ctc-d-1030). 결과 창이 아직 떠 있으면 확인을 다시 누른다.
  for ($i = 0; $i -lt 60 -and ((Cmd 'uilist' 100) -notmatch 'MapMain' -or (Cmd 'rstate' 200) -match 'inBattle=True'); $i++) {
    if ($i % 10 -eq 9 -and (Cmd 'uilist' 100) -match 'BattleEnd' -and (Rc 'hit 959 831' 200) -match 'btnOK') { Rc 'click 959 831' 1000 | Out-Null }
    Start-Sleep -Milliseconds 500
  }
  Start-Sleep -Seconds 2
}
# 변경 이력(changes.log)의 마지막 40줄에 패턴이 있는지(검사마다 20·30·40으로 달랐다. 가장 큰 40).
function LogHasLine([string]$pattern) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 40 -Encoding UTF8 | Where-Object { $_ -match $pattern }).Count -gt 0 }
# 그 이름의 단추가 꺼져 있는지(누르면 "not interactable"). 켜져 있으면 실제로 눌린다.
function ButtonIsOff([string]$name) { $p = Pos $name; (Rc "click $($p.x) $($p.y)" 300) -match 'not interactable' }
# 편집기 글 요소의 글(text='…'의 안쪽). 없으면 빈 글.
function UiText([string]$name) { if ((Cmd "uitext $name" 200) -match "text='([^']*)'") { $Matches[1] } else { '' } }
function UiHas([string]$name) { (Cmd "uitext $name" 200) -notmatch 'no element' }
# 글에서 패턴의 첫 그룹을 수로. 없으면 기본값.
function NumIn([string]$text, [string]$pattern, [double]$default = -1) { if ($text -match $pattern) { [double]$Matches[1] } else { $default } }
# "이름=값 이름=값 …" 한 줄을 표로. 명령의 머리("명령 -> ")와 " | " 뒤의 문장은 뗀다. 값에 '='가 들어도 된다.
function FieldsOf([string]$text) {
  $body = ($text -replace '^[^>]*-> ', '') -replace ' \| .*$', ''
  $map = @{}
  foreach ($pair in ($body -split ' ')) {
    $at = $pair.IndexOf('=')
    if ($at -gt 0) { $map[$pair.Substring(0, $at)] = $pair.Substring($at + 1) }
  }
  $map
}
# 편집기를 열고(이미 열려 있어도 됨) 그 번호의 탭을 누른다.
function OpenEditorTab([int]$tab) { Cmd 'editor' | Out-Null; Press "Tab$tab" | Out-Null }

function Finish {
  "FAILS=$script:fails"
  if ($script:fails -gt 0) { exit 1 }
  exit 0
}
