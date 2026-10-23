# 트레이너 3차(스펙 docs/superpowers/specs/2026-10-23-trainer3-design.md, 05 문서 82절)의 게임 내 검증. 법보·기령 쪽은 treasure-check.ps1의 forge·give·forge-ui.
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base -Exclude 2814696167로 게임이 떠서 세계에 들어가 있어야 한다.
# 고정본 플레이어의 심법: 81001~86001 여섯, 세트 없음.
#   -Part suit      전승 서비스(msole·msuits·msuit·msuitstate·mvalues): 붙이기·떼기·구성원 아님 거부·다른 글 거부·NPC 대상.
#   -Part suit-ui   공법 탭의 전승 글·전승… 하위 화면(붙이기·떼기), 무예에는 단추 없음. 캡처 check-suit.png.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

function LogHas([string]$word) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 30 -Encoding UTF8 | Where-Object { $_ -match $word }).Count -gt 0 }
function SoleOf([int]$base, [string]$who = 'me') { (Cmd "msole $base $who" 300) -replace '^msole .*? -> ', '' }
function State([string]$sole, [string]$who = 'me') { Cmd "msuitstate $sole $who" 300 }
# 그 이름의 버튼이 꺼져 있는지(누르면 "not interactable").
function IsOffBtn([string]$name) { $p = Pos $name; (Rc "click $($p.x) $($p.y)" 300) -match 'not interactable' }

switch ($Part) {
  'suit' {
    $traps = Traps
    $s85 = SoleOf 85001; $s84 = SoleOf 84001; $s82 = SoleOf 82001; $s81 = SoleOf 81001; $s83 = SoleOf 83001
    Check 'the fixture has the six abilities' (@(@($s85, $s84, $s82, $s81, $s83) | Where-Object { $_ -match '^\w{6}$' }).Count -eq 5) "$s85 $s84 $s82 $s81 $s83"
    $list = Cmd "msuits $s85" 300
    Check 'the suit list of 85001 has 10025' ($list -match '10025:[^:]+:members=6:held=0:cur=0') $list
    $r = Cmd "msuit $s85 10025" 800
    Check 'applying a suit turns it on' ((State $s85) -match 'suit=10025 isSuit=true') "$r | $(State $s85)"
    Cmd "msuit $s84 10025" 800 | Out-Null; Cmd "msuit $s82 10025" 800 | Out-Null
    Check 'three abilities carry 10025' ((Cmd 'pstate' 500) -match '85001:10025' -and (Cmd 'pstate' 500) -match '84001:10025' -and (Cmd 'pstate' 500) -match '82001:10025') (Cmd 'pstate' 500)
    Check 'the list marks the current suit and the held members' ((Cmd "msuits $s85" 300) -match '10025:[^:]+:members=6:held=3:cur=1') (Cmd "msuits $s85" 300)
    $r = Cmd "msuit $s85 10025" 500
    Check 'the same suit again says so' ($r -match '이미') $r
    $r = Cmd "msuit $s82 0" 800
    Check 'removing a suit empties the text' ((State $s82) -match 'suit=0 isSuit=false values=$') "$r | $(State $s82)"
    $r = Cmd "msuit $s81 10025" 500
    Check 'a suit the ability is not a member of is refused' ($r -match '구성원이 아닙니다' -and (State $s81) -match 'suit=0 ') "$r | $(State $s81)"
    Cmd "mvalues $s83 {`"x`":1}" 300 | Out-Null
    $r = Cmd "msuit $s83 10026" 500
    Check 'foreign valuesStr is refused' ($r -match '다른 정보' -and (State $s83) -match 'values=\{"x":1\}') "$r | $(State $s83)"
    $r = Cmd "msuit $s83 0" 500
    Check 'foreign valuesStr is not removed either' ($r -match '다른 정보' -and (State $s83) -match 'values=\{"x":1\}') "$r | $(State $s83)"
    Cmd "mvalues $s83 -" 300 | Out-Null
    # NPC 대상
    $npc = if ((Cmd 'trnpc any' 300) -match 'target=(\S+)') { $Matches[1] } else { '' }
    Cmd 'trnpc me' 200 | Out-Null
    $abil = Cmd "msole 0 $npc" 300
    $pick = $null
    foreach ($m in [regex]::Matches($abil, '(\w{6}):(\d+)')) {
      $ns = $m.Groups[1].Value
      $first = [regex]::Match((Cmd "msuits $ns $npc" 300), '^msuits .*? -> (\d+):')
      if ($first.Success) { $pick = @($ns, $first.Groups[1].Value); break }
    }
    if ($pick) {
      $mine = Cmd 'pstate' 500
      $r = Cmd "msuit $($pick[0]) $($pick[1]) $npc" 800
      Check 'npc target: the npc ability gets the suit' ((State $pick[0] $npc) -match "suit=$($pick[1]) ") "$r | $(State $pick[0] $npc)"
      Check 'npc target: the player is unchanged' ((Cmd 'pstate' 500) -eq $mine)
    } else {
      "INFO npc $npc has no ability with a suit: $abil"
    }
    Check 'the change log has suit lines' (LogHas '전승')
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'suit-ui' {
    # 공법 탭 5.4: 심법을 고르면 전승 글과 전승… 단추, 하위 화면에서 붙이기·떼기. 무예는 단추가 없다.
    $traps = Traps
    $s85 = SoleOf 85001
    $left = (Cmd 'psoles' 300)
    Cmd 'uitarget player' | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab5' | Out-Null
    Cmd "tb2pick $s85" 1500 | Out-Null
    Check 'an ability shows its suit text' ((Cmd 'uitext MartialSuitText') -match '전승: 없음') (Cmd 'uitext MartialSuitText')
    Check 'an ability has the suit button' (IsAt 'MartialSuitOpen')
    $id = if ((Cmd "msuits $s85" 300) -match '-> (\d+):') { $Matches[1] } else { '0' }
    Press 'MartialSuitOpen' | Out-Null
    Check 'the suit screen lists the first suit' ((IsAt "SuitPick$id") -and (Cmd "uitext SuitRow$id") -match "\[$id\]") (Cmd "uitext SuitRow$id")
    Check 'remove is off without a suit' ((Cmd 'uitext SuitRemove') -match "text=" -and (IsOffBtn 'SuitRemove'))
    Press "SuitPick$id" | Out-Null
    Check 'pick from the screen applies the suit' ((State $s85) -match "suit=$id isSuit=true") (State $s85)
    Check 'the current suit is off' (IsOffBtn "SuitPick$id")
    Shot 'suit'
    Press 'SuitRemove' | Out-Null
    Check 'remove from the screen' ((State $s85) -match 'suit=0 ') (State $s85)
    Press 'Back' | Out-Null
    $skill = if ((Cmd 'pstate' 300) -match 'left=(\d+)') { SoleOf ([int]$Matches[1]) } else { '' }
    Cmd "tb2pick $skill" 1500 | Out-Null
    Check 'a skill has no suit button' ((Cmd 'uitext MartialSuitOpen') -match 'no element') (Cmd 'uitext MartialSuitOpen')
    Check 'no callback exception in the log' ((Traps) -eq $traps)
    Cmd 'uitoggle' | Out-Null
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
