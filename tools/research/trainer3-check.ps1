# 트레이너 3차(스펙 docs/superpowers/specs/2026-10-23-trainer3-design.md, 05 문서 82절)의 게임 내 검증. 법보·기령 쪽은 treasure-check.ps1의 forge·give·forge-ui.
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base -Exclude 2814696167로 게임이 떠서 세계에 들어가 있어야 한다.
# 고정본 플레이어의 심법: 81001~86001 여섯, 세트 없음.
#   -Part suit      전승 서비스(msole·msuits·msuit·msuitstate·mvalues): 붙이기·떼기·구성원 아님 거부·다른 글 거부·NPC 대상.
#   -Part suit-ui   공법 탭의 전승 글·전승… 하위 화면(붙이기·떼기), 무예에는 단추 없음. 캡처 check-suit.png.
#   -Part realize   성공·드롭 탭의 참오 결과 최대: 꺼짐이면 게임 굴림 그대로, 켬이면 최대 품질 3·5·6 기술 모두 맨 위 값으로 확정, 공법 탭 품질 단추는 그대로, 모두 끄기.
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
  'realize' {
    # 성공·드롭 탭 5.5: 참오 결과 최대. 최대 품질 3·5·6인 기술(고정본 86001·85001·신통)에서 게임 참오 창으로 참오 → 확정.
    $traps = Traps
    function Held { if ((Cmd 'count 5021462' 300) -match 'count=(\d+)') { [int]$Matches[1] } else { -1 } }
    function Rolls { [regex]::Matches((Cmd 'realstate' 300), 'rd\d+ old=(-?\d+) new=(-?\d+) max=(-?\d+)') }
    function Realize([string]$sole) {
      Cmd "realui $sole" 1000 | Out-Null
      Cmd 'realclick' 5000 | Out-Null
      $rolls = Rolls
      Cmd 'realok' 2500 | Out-Null
      Cmd 'uiclose MartialRealize' 500 | Out-Null
      ,$rolls
    }
    $ult = if ((Cmd 'pstate' 300) -match 'ult=(\d+)') { SoleOf ([int]$Matches[1]) } else { '' }
    $soles = @((SoleOf 86001), (SoleOf 85001), $ult)
    foreach ($s in $soles) { Cmd "mexp me $s 200000" 500 | Out-Null }
    Cmd 'tbbagset 5021462 1000' 500 | Out-Null
    $h0 = Held
    $rolls = Realize $soles[1]
    $atMax = @($rolls | Where-Object { $_.Groups[2].Value -eq $_.Groups[3].Value }).Count
    Check 'off: the game rolls as usual' ($rolls.Count -gt 0 -and $atMax -lt $rolls.Count) "lines=$($rolls.Count) atMax=$atMax"
    Cmd 'uitarget player' | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab17' | Out-Null
    Press 'SuccessRealize' | Out-Null
    Check 'the row turns on' ((Cmd 'uitext SuccessRealize') -match '켜짐') (Cmd 'uitext SuccessRealize')
    Check 'the status shows it' ((Cmd 'uistate') -match '참오 최대') (Cmd 'uistate')
    Cmd 'uitoggle' | Out-Null
    foreach ($s in $soles) {
      $rolls = Realize $s
      $bad = @($rolls | Where-Object { $_.Groups[2].Value -ne $_.Groups[3].Value })
      Check "on: every rolled line of $s is at its top value" ($rolls.Count -gt 0 -and $bad.Count -eq 0) "lines=$($rolls.Count) bad=$($bad | ForEach-Object { $_.Value })"
      $opts = Cmd "mopts $s" 300
      $notMax = @([regex]::Matches($opts, '\d+:\d+:(\d+)/(\d+)') | Where-Object { $_.Groups[1].Value -ne $_.Groups[2].Value })
      Check "on: after confirm all lines of $s are at max quality" ($notMax.Count -eq 0) $opts
    }
    Check 'the material is spent as the game rules say' ((Held) -lt $h0) "before=$h0 after=$(Held)"
    # 다시 참오(OnRealizeAgain)도 최대(리뷰 지적 3)
    Cmd "realui $($soles[1])" 1000 | Out-Null
    Cmd 'realclick' 5000 | Out-Null
    Cmd 'realagain' 5000 | Out-Null
    $rolls = Rolls
    $bad = @($rolls | Where-Object { $_.Groups[2].Value -ne $_.Groups[3].Value })
    Check 'on: realize again is also at the top value' ($rolls.Count -gt 0 -and $bad.Count -eq 0) "lines=$($rolls.Count) bad=$($bad | ForEach-Object { $_.Value }) | $(Cmd 'realstate' 300)"
    # 참오 단추가 예외로 끝나 표시가 남아도 창 밖의 굴림은 그대로(리뷰 지적 1)
    $r = Cmd 'realstick' 300
    $out = Cmd "realroll $($soles[1])" 500
    $max = if ($out -match 'max=(\d+)') { $Matches[1] } else { '-1' }
    $vals = if ($out -match 'rolls=([\d,]+)') { @($Matches[1].Trim(',').Split(',')) } else { @() }
    Check 'a stuck realize window does not max rolls outside the click' ($vals.Count -eq 6 -and @($vals | Where-Object { $_ -ne $max }).Count -gt 0) "$r | $out"
    Cmd 'realok' 2500 | Out-Null; Cmd 'uiclose MartialRealize' 500 | Out-Null
    # 켠 채 공법 탭의 품질 단추는 그대로 동작한다
    Cmd 'editor' | Out-Null; Press 'Tab5' | Out-Null
    Cmd "tb2pick $($soles[1])" 1500 | Out-Null
    $before = Cmd "mopts $($soles[1])" 300
    Press 'QualityMinus0' | Out-Null
    $after = Cmd "mopts $($soles[1])" 300
    $lv0 = if ($before -match '-> 0:\d+:(\d+)/') { [int]$Matches[1] } else { -1 }
    Check 'quality buttons are untouched while realize-max is on' ($after -match "-> 0:\d+:$($lv0 - 1)/") "$before | $after"
    Press 'Tab17' | Out-Null
    Press 'SuccessReset' | Out-Null
    Check 'reset turns it off' ((Cmd 'uitext SuccessRealize') -match '꺼짐' -and (Cmd 'uistate') -notmatch '참오 최대') (Cmd 'uistate')
    Cmd 'uitoggle' | Out-Null
    Cmd "realui $($soles[1])" 1000 | Out-Null
    Cmd 'realclick' 5000 | Out-Null
    Shot 'realize-off'
    Cmd 'realok' 2500 | Out-Null; Cmd 'uiclose MartialRealize' 500 | Out-Null
    Check 'no game window is left over' ((Cmd 'uilist' 300) -notmatch 'MartialRealize|Mask ') (Cmd 'uilist' 300)
    Check 'the change log has realize lines' (LogHas '참오 결과 최대')
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'persist' {
    # 저장 후 유지(앞): 법보 경지 7·단계 6, 기령 친밀도 최대, 심법 전승 10025, 참오 결과 최대 켬 → 저장. 다음은 session.ps1 -Restart → persist-restart.
    Cmd 'trnpc me' 200 | Out-Null
    Cmd 'trgive 3031101 7 6' 5000 | Out-Null
    $tr = Cmd 'tr' 300
    Check 'a grade 7 stage 6 treasure exists' ($tr -match '3031101:[^:]+:q\d+:g7:') $tr
    Cmd 'trmake 1001' 800 | Out-Null
    $sp = if ((Cmd 'trsp' 300) -match '(-?\d+):1001 intim=') { $Matches[1] } else { '' }
    Cmd "trclose $sp" 500 | Out-Null
    $s85 = SoleOf 85001
    Cmd "msuit $s85 10025" 800 | Out-Null
    Check 'the suit is set' ((State $s85) -match 'suit=10025 ') (State $s85)
    Cmd 'uitarget player' | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab17' | Out-Null; Press 'SuccessRealize' | Out-Null
    Check 'realize-max is on before saving' ((Cmd 'uitext SuccessRealize') -match '켜짐')
    Cmd 'uitoggle' | Out-Null
    $r = Cmd 'save' 3000
    Check 'saved' ($r -notmatch 'ERR|fail') $r
  }
  'persist-restart' {
    $tr = Cmd 'tr' 300
    $sole = if ($tr -match '\[(\w+):3031101:[^:]+:q\d+:g7:') { $Matches[1] } else { '' }
    Check 'the treasure keeps grade 7' ($sole -ne '') $tr
    $a = Cmd "trart $sole" 300
    Check 'the treasure keeps stage 6 and rates 120 (sp 100)' ($a -match 'level=6 ' -and $a -match 'rate=120/120/120/120/100 ') $a
    Check 'the spirit keeps full intimacy' ((Cmd 'trsp' 300) -match ':1001 intim=(\d+)/\1') (Cmd 'trsp' 300)
    $s85 = SoleOf 85001
    Check 'the ability keeps its suit' ((State $s85) -match 'suit=10025 isSuit=true') (State $s85)
    Cmd 'uitarget player' | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab17' | Out-Null
    Check 'realize-max starts off after a restart' ((Cmd 'uitext SuccessRealize') -match '꺼짐') (Cmd 'uitext SuccessRealize')
    Cmd 'uitoggle' | Out-Null
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
