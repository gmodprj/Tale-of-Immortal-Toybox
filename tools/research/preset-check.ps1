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
    $history = (Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 3 -Encoding UTF8) -join ' '
    Check 'the change log has the grade and the attributes' ($history -match '\(9\) -> .*\(27\)' -and $history -match ' 82, .* -> .* 766, ') $history
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
  default { throw "알 수 없는 Part: $Part" }
}
Finish
