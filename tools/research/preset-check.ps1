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
