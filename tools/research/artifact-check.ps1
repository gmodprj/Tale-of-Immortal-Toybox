# 신기 탭의 게임 내 검증(docs/superpowers/specs/2026-10-14-artifact-tab-design.md 6절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다.
# 고정본: 대경지 3(결정), 신기는 쌍어패(1)만, 쌍어옥패 점수 9, 호천안 요수·기술 없음, 호요 0.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

function Fields([string]$text) {
  $body = ($text -replace '^[^>]*-> ', '') -replace ' \| .*$', ''
  $map = @{}
  foreach ($pair in $body -split ' ') { if ($pair -match '^([^=]+)=(.*)$') { $map[$Matches[1]] = $Matches[2] } }
  $map
}
# 그 이름의 버튼이 꺼져 있는지(누르면 "not interactable").
function IsOff([string]$name) { $p = Pos $name; (Rc "click $($p.x) $($p.y)" 300) -match 'not interactable' }
function LogHas([string]$word) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 20 -Encoding UTF8 | Where-Object { $_ -match $word }).Count -gt 0 }
# 점수 칸에 쓰고 Enter. 화면 밖 게임 창으로 가는 키가 한 회차 내내 빠지는 때가 있어(원인 미확인, 회차 atab4·atab13) 반영되지 않았으면 한 번 다시 보낸다.
function EnterPoint([string]$text, [string]$want) {
  for ($try = 1; $try -le 2; $try++) {
    Cmd 'uifocus ArtifactPoint' | Out-Null; Cmd "ui set ArtifactPoint $text" | Out-Null; Key 0x0D 800
    if ((Fields (Cmd 'aw' 200)).point -eq $want) { return }
    "INFO Enter was not applied (try $try)" | Write-Host
  }
}

switch ($Part) {
  'list' {
    Check 'the fixture has only the pisces pendant' ((Fields (Cmd 'aw' 200)).weapons -eq '1') (Cmd 'aw' 200)
    $r = Cmd 'aoff 1' 400
    Check 'the last artifact is not removed' ((Fields (Cmd 'aw' 200)).weapons -eq '1' -and $r -match '하나 이상') $r
    $r = Cmd 'aon 2' 400
    Check 'turning on the god eye adds 2' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2' -and $r -match '켰습니다') $r
    $r = Cmd 'aon 2' 400
    Check 'turning it on again adds nothing' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2' -and $r -match '이미') $r
    Cmd 'aon 3' 400 | Out-Null
    Check 'turning on the gourd adds 3' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2,3') (Cmd 'aw' 200)
    $r = Cmd 'aoff 1' 400
    Check 'one of several can be turned off' ((Fields (Cmd 'aw' 200)).weapons -eq '2,3') $r
    $r = Cmd 'aoff 1' 400
    Check 'turning off a missing one says so' ($r -match '가지고 있지 않') $r
    Check 'the change log has the artifact line' (LogHas '신기 목록')
    $r = Cmd 'apoint 25' 400
    Check 'the point is set' ((Fields (Cmd 'aw' 200)).point -eq '25') $r
    $r = Cmd 'apoint -5' 400
    Check 'a negative point becomes 0' ((Fields (Cmd 'aw' 200)).point -eq '0') $r
    $r = Cmd 'apoint 123456' 400
    Check 'a point over the limit becomes 9999' ((Fields (Cmd 'aw' 200)).point -eq '9999') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'godeye' {
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'geunlock 7010' 400
    Check 'without the god eye nothing is unlocked' ($r -match '호천안이 없습니다' -and (Fields (Cmd 'gestate' 200)).bosses -eq '0') $r
    Cmd 'awset 1,2' 300 | Out-Null
    $cat = Fields (Cmd 'gecat' 300)
    Check 'the catalog groups 95 skills under 42 bosses' ($cat.bosses -eq '42' -and $cat.skills -eq '95') (Cmd 'gecat' 300)
    Check 'the first boss has a translated name' ($cat.first -match '^\D' -and $cat.first -notmatch '^요수') (Cmd 'gecat' 300)
    Cmd 'gedmg 1' 300 | Out-Null
    Check 'damage makes it unusable' ((Fields (Cmd 'gestate' 200)).canUse -eq 'False') (Cmd 'gestate' 200)
    $r = Cmd 'gerepair' 400
    $s = Fields (Cmd 'gestate' 200)
    Check 'repair makes it usable at once' ($s.canUse -eq 'True' -and $s.damaged -eq 'False' -and $s.repairMonth -eq '0') "$r | $(Cmd 'gestate' 200)"
    $r = Cmd 'geequip 100721' 400
    Check 'an unlearned skill is not equipped' ((Fields (Cmd 'gestate' 200)).equip -eq '0') $r
    $r = Cmd 'geunlock 7010' 400
    $b = Fields (Cmd 'geboss 7010' 200)
    Check 'unlock writes the player major grade and learns the boss skills' ($b.grade -eq '3' -and $b.learned -eq '3/3') "$r | $(Cmd 'geboss 7010' 200)"
    $r = Cmd 'geunlock 7010' 400
    Check 'unlocking it again adds nothing' ((Fields (Cmd 'gestate' 200)).skills -eq '3' -and $r -match '이미') $r
    $r = Cmd 'geequip 100721' 400
    Check 'a learned skill is equipped' ((Fields (Cmd 'gestate' 200)).equip -eq '100721') $r
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'geunlockall' 400
    Check 'unlock all needs the god eye too' ((Fields (Cmd 'gestate' 200)).skills -eq '3') $r
    Cmd 'awset 1,2' 300 | Out-Null
    $r = Cmd 'geunlockall' 3000
    $s = Fields (Cmd 'gestate' 200)
    Check 'unlock all opens every boss and learns every skill' ($s.bosses -eq '42' -and $s.skills -eq '95' -and $r -match '요수 해금 41, 기술 92개 익힘, 건너뜀 1') "$r | $(Cmd 'gestate' 200)"
    $r = Cmd 'geunlockall' 3000
    Check 'unlock all again skips every boss' ($r -match '요수 해금 0, 기술 0개 익힘, 건너뜀 42' -and (Fields (Cmd 'gestate' 200)).skills -eq '95') $r
    Check 'the change log has the god eye line' (LogHas '호천안')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'potmon' {
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'potadd 1001 3' 400
    Check 'without the gourd no potmon is added' ($r -match '연요호가 없습니다' -and (Fields (Cmd 'potlist' 200)).count -eq '0') $r
    Cmd 'awset 1,3' 300 | Out-Null
    $r = Cmd 'potadd 1001 3' 600
    $p = Fields (Cmd 'potlist' 200)
    Check 'a potmon is added at the chosen grade' ($p.count -eq '1' -and $p.grade -eq '3' -and $p.exp -eq '18000' -and $p.next -eq '30000') "$r | $(Cmd 'potlist' 200)"
    $r = Cmd "potfill $($p.first)" 600
    $q = Fields (Cmd 'potlist' 200)
    Check 'fill exp stops at the next grade' ($q.exp -eq '30000' -and $q.grade -eq '3') "$r | $(Cmd 'potlist' 200)"
    $r = Cmd "potfill $($p.first)" 600
    Check 'fill exp at the boundary is refused' ((Fields (Cmd 'potlist' 200)).exp -eq '30000' -and $r -match '승급') $r
    $r = Cmd 'potadd 1001 11' 400
    Check 'a grade out of range is refused' ((Fields (Cmd 'potlist' 200)).count -eq '1') $r
    for ($i = 0; $i -lt 27; $i++) { Cmd 'potadd 1002 1' 150 | Out-Null }
    Check 'potmons can be added up to 28' ((Fields (Cmd 'potlist' 200)).count -eq '28') (Cmd 'potlist' 200)
    $r = Cmd 'potadd 1002 1' 400
    Check 'the 29th potmon is refused' ((Fields (Cmd 'potlist' 200)).count -eq '28' -and $r -match '28') $r
    Cmd 'ddmg 1' 300 | Out-Null
    Check 'damage makes the gourd unusable' ((Fields (Cmd 'ddstate' 200)).canUse -eq 'False') (Cmd 'ddstate' 200)
    $r = Cmd 'ddrepair' 400
    Check 'repair makes the gourd usable' ((Fields (Cmd 'ddstate' 200)).canUse -eq 'True' -and (Fields (Cmd 'ddstate' 200)).damaged -eq 'False') "$r | $(Cmd 'ddstate' 200)"
    Cmd 'ddmg 2' 300 | Out-Null
    $d = Fields (Cmd 'ddstate' 200)
    Check 'a gourd under repair counts as damaged' ($d.damaged -eq 'True') (Cmd 'ddstate' 200)
    $r = Cmd 'ddrepair' 400
    $d = Fields (Cmd 'ddstate' 200)
    Check 'repair clears a pending repair too' ($d.damaged -eq 'False' -and $d.canUse -eq 'True' -and $d.repairMonth -eq '0') "$r | $(Cmd 'ddstate' 200)"
    Cmd 'ddmg 3' 300 | Out-Null
    Check 'a finished repair (past month) is not damage' ((Fields (Cmd 'ddstate' 200)).damaged -eq 'False') (Cmd 'ddstate' 200)
    Cmd 'ddmg 0' 300 | Out-Null
    Check 'the change log has the gourd line' (LogHas '연요호')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'ui' {
    Cmd 'awset 1' 300 | Out-Null
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab7' | Out-Null
    Check 'the artifact tab is the eighth' (IsAt 'ArtifactPoint')
    Check 'the last artifact cannot be turned off' ((IsOff 'ArtifactOff1') -and (Cmd 'uitext ArtifactLast' 150) -match '하나 이상')
    Check 'details of a missing artifact are off' (IsOff 'ArtifactOpen2')
    Check 'a missing artifact says to turn it on first' ((Cmd 'uitext ArtifactNeed2' 150) -match '먼저 켜세요') (Cmd 'uitext ArtifactNeed2' 150)
    Press 'ArtifactOn2' 1200 | Out-Null
    Check 'turning on adds it' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2') (Cmd 'aw' 200)
    Press 'ArtifactOn3' 1200 | Out-Null
    Press 'ArtifactOff3' | Out-Null
    Check 'turning off asks first' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2,3')
    Press 'ArtifactOff3' 1200 | Out-Null
    Check 'the second press turns it off' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2') (Cmd 'aw' 200)
    Press 'ArtifactOn3' 1200 | Out-Null
    EnterPoint 25 25
    Check 'the point field sets the point on Enter' ((Fields (Cmd 'aw' 200)).point -eq '25') (Cmd 'aw' 200)
    Press 'ArtifactPointPlus10' | Out-Null
    Press 'ArtifactPointMinus1' | Out-Null
    Check 'plus ten and minus one' ((Fields (Cmd 'aw' 200)).point -eq '34') (Cmd 'aw' 200)
    EnterPoint -5 0
    Check 'a negative point becomes 0' ((Fields (Cmd 'aw' 200)).point -eq '0') (Cmd 'aw' 200)
    EnterPoint 99999 9999
    Check 'a point over the limit becomes 9999' ((Fields (Cmd 'aw' 200)).point -eq '9999') (Cmd 'aw' 200)
    Press 'ArtifactOpen2' | Out-Null
    Check 'the god eye page has its controls' ((IsAt 'GodEyeUnlockAll') -and (IsAt 'GodEyeSearch') -and (IsAt 'GodEyeBoss7010'))
    Press 'GodEyeUnlockAll' | Out-Null
    Check 'unlock all warns that it fills the god eye breakthrough progress' ((Cmd 'uistate' 200) -match '돌파') (Cmd 'uistate' 200)
    Check 'repair is off without damage' (IsOff 'GodEyeRepair')
    Check 'an unlearned skill cannot be equipped' (IsOff 'GodEyeEquip100721')
    Press 'GodEyeBoss7010' 1200 | Out-Null
    Check 'unlock from the page' ((Fields (Cmd 'geboss 7010' 200)).learned -eq '3/3' -and (Cmd 'uitext GodEyeState7010' 150) -match '해금') (Cmd 'geboss 7010' 200)
    Press 'GodEyeEquip100721' 1200 | Out-Null
    Check 'equip from the page' ((Fields (Cmd 'gestate' 200)).equip -eq '100721' -and (Cmd 'uitext GodEyeEquipText' 150) -match '유서불풍') (Cmd 'gestate' 200)
    $fit = Fields (Cmd 'ifit GodEyeDesc' 300)
    Check 'every skill description is whole' ([int]$fit.texts -ge 3 -and $fit.overflow -eq '0') (Cmd 'ifit GodEyeDesc' 300)
    Shot 'artifact-godeye-page'
    Press 'Back' | Out-Null
    Press 'ArtifactOpen3' | Out-Null
    Check 'the gourd page has its controls' ((IsAt 'PotmonGrade10') -and (IsAt 'PotmonSearch') -and (IsAt 'PotmonAdd1001'))
    Check 'the count starts at zero' ((Cmd 'uitext DevilCount' 150) -match '0 / 28')
    Press 'PotmonGrade2' | Out-Null
    Press 'PotmonAdd1001' 1200 | Out-Null
    $p = Fields (Cmd 'potlist' 200)
    Check 'add from the page uses the chosen grade' ($p.count -eq '1' -and $p.grade -eq '2') (Cmd 'potlist' 200)
    Press "PotmonExp$($p.first)" 1200 | Out-Null
    Check 'fill exp from the page' ((Fields (Cmd 'potlist' 200)).exp -eq '18000') (Cmd 'potlist' 200)
    Check 'the fill button is off at the boundary' (IsOff "PotmonExp$($p.first)")
    Check 'the count is shown' ((Cmd 'uitext DevilCount' 150) -match '1 / 28')
    Shot 'artifact-gourd-page'
    Press 'Back' | Out-Null
    Shot 'artifact-tab'
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'game' {
    # 게임 화면에 나오는지(캡처로 본다): 호천안 요수 기능, 연요호 모든 호요, 부활 정보의 점수.
    Cmd 'awset 1,2,3' 300 | Out-Null
    Cmd 'geunlock 7010' 400 | Out-Null; Cmd 'geequip 100721' 400 | Out-Null
    Cmd 'potadd 1001 3' 400 | Out-Null
    Cmd 'apoint 12' 300 | Out-Null
    Cmd 'uiopen GodEye' 1500 | Out-Null
    Rc 'click 1379 851' 1500 | Out-Null
    Check 'the game god eye skill page opened' ((Cmd 'uilist' 200) -match 'GodEyeBossSkill')
    Shot 'artifact-game-godeye'
    Cmd 'uiclose GodEyeBossSkill' 500 | Out-Null; Cmd 'uiclose GodEye' 800 | Out-Null
    Cmd 'uiopen DevilDemon' 1500 | Out-Null
    Rc 'click 1362 897' 1500 | Out-Null
    Check 'the game potmon list opened' ((Cmd 'uilist' 200) -match 'PotmonList')
    Shot 'artifact-game-gourd'
    Cmd 'uiclose PotmonList' 500 | Out-Null; Cmd 'uiclose DevilDemon' 800 | Out-Null
    Cmd 'uiopen ResurgeInfo' 1500 | Out-Null
    Shot 'artifact-game-point'
    Cmd 'uiclose ResurgeInfo' 800 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'persist' {
    # 이 뒤에 session.ps1 -Restart, 그 뒤에 restart 부분을 돌린다.
    Cmd 'awset 1' 300 | Out-Null
    Cmd 'aon 2' 400 | Out-Null; Cmd 'aon 3' 400 | Out-Null
    Cmd 'apoint 12' 300 | Out-Null
    Cmd 'geunlock 7010' 400 | Out-Null; Cmd 'geequip 100721' 400 | Out-Null
    Cmd 'potadd 1001 3' 400 | Out-Null
    Check 'the map buttons do not change before reloading' ((Cmd 'awbtn' 200) -match 'godEye=off' -and (Cmd 'awbtn' 200) -match 'devil=off') (Cmd 'awbtn' 200)
    Cmd 'save' 2500 | Out-Null
    Start-Sleep -Seconds 6   # 저장이 끝나기 전에 재시작하면 남지 않았다(07 문서 7.2절)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'restart' {
    Check 'the artifacts are kept' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2,3' -and (Fields (Cmd 'aw' 200)).point -eq '12') (Cmd 'aw' 200)
    Check 'the map shows all three buttons after reloading' ((Cmd 'awbtn' 200) -match 'pisces=on godEye=on devil=on') (Cmd 'awbtn' 200)
    Check 'the god eye boss and the equipped skill are kept' ((Fields (Cmd 'geboss 7010' 200)).learned -eq '3/3' -and (Fields (Cmd 'gestate' 200)).equip -eq '100721') (Cmd 'gestate' 200)
    $p = Fields (Cmd 'potlist' 200)
    Check 'the potmon is kept' ($p.count -eq '1' -and $p.grade -eq '3') (Cmd 'potlist' 200)
    # 호천안을 가진 세이브는 불러올 때 호천안 화면이 저절로 열린다(원인 미확인, 07 문서 7.2절). 다음 검사를 위해 닫는다.
    if ((Cmd 'uilist' 200) -match 'GodEye') { Cmd 'uiclose GodEye' 800 | Out-Null }
  }
  'hud-one' {
    # 고정본(쌍어패만): 쌍어패는 원래 자리(-240,-93) 그대로.
    $p = Cmd 'awpos' 200
    Check 'with one artifact the pendant stays where the game put it' ($p -match 'pisces=-240,-93@' -and $p -match 'godEye=off' -and $p -match 'devil=off') $p
    # 이 뒤에 셋을 켜고 저장한다. session.ps1 -Restart 뒤에 hud를 돌린다.
    Cmd 'awset 1' 300 | Out-Null
    Cmd 'aon 2' 400 | Out-Null; Cmd 'aon 3' 400 | Out-Null
    Cmd 'save' 2500 | Out-Null
    Start-Sleep -Seconds 6
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'hud' {
    # 호천안을 가진 세이브는 불러올 때 호천안 화면이 열린다(07 문서 7.2절). 먼저 닫는다.
    # 그 화면은 세계에 들어온 뒤 조금 늦게 열린다. 열렸다가 닫힐 때까지(최대 10초) 기다리며 닫는다.
    for ($i = 0; $i -lt 20; $i++) {
      if ((Cmd 'uilist' 200) -match '  GodEye layer') { Cmd 'uiclose GodEye' 800 | Out-Null; if ((Cmd 'uilist' 200) -notmatch '  GodEye layer') { break } }
      Start-Sleep -Milliseconds 300
    }
    Check 'only the map is open before clicking' ((Cmd 'uilist' 200) -notmatch '  (GodEye|DevilDemon|PiscesPendant) layer') (Cmd 'uilist' 200)
    $p = Cmd 'awpos' 200
    Check 'the three buttons sit side by side' ($p -match 'pisces=-240,-93@' -and $p -match 'godEye=-194,-93@' -and $p -match 'devil=-148,-93@') $p
    Shot 'artifact-hud'
    foreach ($pair in @(@('pisces', 'PiscesPendant'), @('godEye', 'GodEye'), @('devil', 'DevilDemon'))) {
      if ($p -match "$($pair[0])=[-\d]+,[-\d]+@(\d+),(\d+)") {
        $o = Rc "click $($Matches[1]) $($Matches[2])" 1500
        $ui = Cmd 'uilist' 200
        Check "the $($pair[0]) button opens its own screen" ($o -match "Button\.onClick: G:btn" -and $ui -match "  $($pair[1]) layer") "$o | $ui"
        Cmd "uiclose $($pair[1])" 800 | Out-Null
      } else { Check "the $($pair[0]) button has a position" $false $p }
    }
    # 같은 지도 화면에서 게임이 자리를 되돌리면(사용자가 실제 전투 뒤에 본 겹침, 경로 미확인) 다시 나란히 놓여야 한다.
    Cmd 'awreset' 200 | Out-Null
    Start-Sleep -Milliseconds 500
    $p = Cmd 'awpos' 200
    Check 'the buttons are placed again after the game moves them back' ($p -match 'pisces=-240,-93@' -and $p -match 'godEye=-194,-93@' -and $p -match 'devil=-148,-93@') $p
    $fs = [IO.File]::Open($log, 'Open', 'Read', 'ReadWrite'); $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::GetEncoding(949)); $logText = $sr.ReadToEnd(); $sr.Close()
    Check 'the mod logs that the game moved them back' ($logText -match '\[TlToybox\] artifact hud drift') ''
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'hud-battle' {
    # 전투를 다녀오면 지도 화면이 새로 만들어진다. 다시 나란히 놓여야 한다.
    # 들어가기와 나오기는 ui-check.ps1 combat의 Into·Leave와 같다(결과 창과 지도가 돌아올 때까지 기다린다).
    Cmd 'fight 1011 20' 6000 | Out-Null; Rc 'click 960 684' 12000 | Out-Null; Rc 'click 956 749' 4000 | Out-Null
    Check 'in battle' ((Cmd 'uilist' 200) -match 'BattleInfo') (Cmd 'uilist' 200)
    Cmd 'bend' 500 | Out-Null
    for ($i = 0; $i -lt 30 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
    Start-Sleep -Seconds 2
    for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Rc 'click 959 831' 2500 | Out-Null }
    Start-Sleep -Seconds 3
    Check 'back on the map' ((Cmd 'uilist' 200) -match '  MapMain layer') (Cmd 'uilist' 200)
    $p = Cmd 'awpos' 200
    Check 'the buttons are side by side again after a battle' ($p -match 'pisces=-240,-93@' -and $p -match 'godEye=-194,-93@' -and $p -match 'devil=-148,-93@') $p
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'hud-battle-slots' {
    # 전투 화면의 신기 칸. 연요호 칸은 연요호를 가지면, 호천안 칸은 기술을 장착하면, 쌍어패 칸은 반쪽을 준 NPC가 있으면 켜진다.
    # 게임은 셋을 거의 같은 자리(쌍어패·호천안 493,58, 연요호 490,65)에 둔다. 둘 이상이면 오른쪽으로 60씩 나란히 놓여야 한다.
    function IntoBattle { Cmd 'fight 1011 20' 6000 | Out-Null; Rc 'click 960 684' 12000 | Out-Null }
    function LeaveBattle {
      Rc 'click 956 749' 1500 | Out-Null
      Cmd 'bend' 500 | Out-Null
      for ($i = 0; $i -lt 30 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
      Start-Sleep -Seconds 2
      for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Rc 'click 959 831' 2500 | Out-Null }
      Start-Sleep -Seconds 3
    }
    Cmd 'awset 1,3' 300 | Out-Null
    IntoBattle
    $p = Cmd 'bwpos' 200
    Check 'with only the gourd slot on it stays where the game put it' ($p -match 'pisces=off' -and $p -match 'godEye=off' -and $p -match 'devil=490,65') $p
    LeaveBattle
    Cmd 'awset 1,2,3' 300 | Out-Null
    Cmd 'geunlock 7010' 400 | Out-Null; Cmd 'geequip 100721' 400 | Out-Null
    Cmd 'pigive' 400 | Out-Null
    IntoBattle
    $p = Cmd 'bwpos' 200
    Check 'the three battle slots sit side by side' ($p -match 'pisces=493,58' -and $p -match 'godEye=553,58' -and $p -match 'devil=610,65') $p
    Shot 'artifact-battle-slots'
    Rc 'click 956 749' 2000 | Out-Null
    $p = Cmd 'bwpos' 200
    Check 'they stay side by side after the battle starts' ($p -match 'pisces=493,58' -and $p -match 'godEye=553,58' -and $p -match 'devil=610,65') $p
    LeaveBattle
    Check 'back on the map' ((Cmd 'uilist' 200) -match '  MapMain layer') (Cmd 'uilist' 200)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'keys' {
    # 신기별 단축키(모드)와 게임의 신기 키 끄기, 전투 칸 클릭. 키는 화면 밖 게임에 전달될 때 판정한다(전달되지 않으면 INFO).
    function IntoBattle { Cmd 'fight 1011 20' 6000 | Out-Null; Rc 'click 960 684' 12000 | Out-Null; Rc 'click 956 749' 2500 | Out-Null; Cmd 'bgod on' 300 | Out-Null }
    function LeaveBattle {
      Cmd 'bend' 500 | Out-Null
      for ($i = 0; $i -lt 30 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
      Start-Sleep -Seconds 2
      for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Rc 'click 959 831' 2500 | Out-Null }
      Start-Sleep -Seconds 3
    }
    Cmd 'awset 1,2,3' 300 | Out-Null
    Cmd 'potadd 1001 1' 500 | Out-Null; Cmd 'potequip' 500 | Out-Null
    Cmd 'geunlock 7010' 400 | Out-Null; Cmd 'geequip 100721' 400 | Out-Null
    # 사용자의 config.txt에 정해 둔 키가 있을 수 있다(회차 -End가 되돌린다). 검사는 비운 상태에서 시작한다.
    Cmd 'akey 1 None' 300 | Out-Null; Cmd 'akey 2 None' 300 | Out-Null; Cmd 'akey 3 None' 300 | Out-Null; Cmd 'akeyoff off' 300 | Out-Null
    $r = Cmd 'akey 3 K' 300
    Check 'a key is set for the gourd' ((Cmd 'akeys' 200) -match 'devil=K' -and (Cmd 'akeys' 200) -match 'godEye=None') "$r | $(Cmd 'akeys' 200)"
    Cmd 'akeyoff on' 300 | Out-Null
    Check 'the game key is kept outside battle' ((Cmd 'akeys' 200) -match 'game=F ') (Cmd 'akeys' 200)
    IntoBattle
    Start-Sleep -Milliseconds 500
    Check 'the game key is off during battle' ((Cmd 'akeys' 200) -match 'game=None ') (Cmd 'akeys' 200)
    # 칸 클릭: 연요호만 발동한다(회차 akey-1014c에서 손으로 확인).
    $p = Cmd 'bslot devil' 200
    if ($p -match '@(\d+),(\d+)') {
      $o = Cmd "brclick $($Matches[1]) $($Matches[2])" 1500
      $s = Cmd 'bfired' 200
      Check 'clicking the gourd slot fires only the gourd' ($o -match 'sent=True' -and $s -match 'devil=True' -and $s -match 'godEye=False') "$o | $s"
    } else { Check 'the gourd slot has a position' $false $p }
    $p = Cmd 'bslot pisces' 200
    Check 'the pendant slot is off without a gifted half' ($p -match 'off') $p
    # 단축키: 호천안 키를 F9로 두고 누르면 호천안이 발동한다. 키가 화면 밖 게임에 전달되지 않으면 INFO.
    Cmd 'akey 2 F9' 300 | Out-Null
    Key 0x78 1500
    $s = Cmd 'bfired' 200
    if ($s -match 'godEye=True') { Check 'the god eye key fires the god eye' $true $s }
    else {
      "INFO the F9 key did not reach the game window ($s)"
      Cmd 'afire 2' 800 | Out-Null
      Check 'firing the god eye from the mod works' ((Cmd 'bfired' 200) -match 'godEye=True') (Cmd 'bfired' 200)
    }
    Cmd 'akey 2 None' 300 | Out-Null
    LeaveBattle
    Check 'the game key is back after battle' ((Cmd 'akeys' 200) -match 'game=F ') (Cmd 'akeys' 200)
    Cmd 'akeyoff off' 300 | Out-Null
    Cmd 'save' 2500 | Out-Null
    Check 'the keys are written to config.txt' (@(Get-Content -LiteralPath (Join-Path $data 'config.txt') -Encoding UTF8 | Where-Object { $_ -match '^artifactkey3=K$' }).Count -eq 1)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'slot-clicks' {
    # 전투 칸을 실제 마우스와 같은 경로로 누른다(brclick). 연요호 칸은 그림(imgPotmonHead)의 게임 처리기가, 쌍어패 칸은 게임의 단추가 클릭을 받는다.
    function IntoBattle { Cmd 'fight 1011 20' 6000 | Out-Null; Rc 'click 960 684' 12000 | Out-Null; Rc 'click 956 749' 2500 | Out-Null; Cmd 'bgod on' 300 | Out-Null }
    Cmd 'awset 1,2,3' 300 | Out-Null
    Cmd 'potadd 1001 1' 500 | Out-Null; Cmd 'potequip' 500 | Out-Null
    Cmd 'pigive' 400 | Out-Null; Cmd 'geunlock 7010' 400 | Out-Null; Cmd 'geequip 100721' 400 | Out-Null
    IntoBattle
    foreach ($pair in @(@('devil', 'devil'), @('pisces', 'pisces'))) {
      $p = Cmd "bslot $($pair[0])" 200
      if ($p -match '@(\d+),(\d+)') {
        $o = Cmd "brclick $($Matches[1]) $($Matches[2])" 2500
        $s = Cmd 'bfired' 200
        Check "a real click on the $($pair[0]) slot fires it" ($s -match "$($pair[1])=True") "$o | $s"
      } else { Check "the $($pair[0]) slot is on" $false $p }
    }
    $s = Cmd 'bfired' 200
    Check 'the god eye is not fired by those clicks' ($s -match 'godEye=False') $s
    # 호천안 칸: 실제 클릭은 게임의 기술 그림 단추(GodEyeSkillItem/btnIcon)에 도착하지만 그 단추는 기술을 쓰지 않는다(click-1014a).
    $p = Cmd 'bslot godEye' 200
    if ($p -match '@(\d+),(\d+)') {
      $o = Cmd "brclick $($Matches[1]) $($Matches[2])" 2500
      $s = Cmd 'bfired' 200
      Check 'a real click on the god eye slot fires it' ($s -match 'godEye=True') "$o | $s"
    } else { Check 'the god eye slot is on' $false $p }
    Cmd 'bend' 500 | Out-Null
    for ($i = 0; $i -lt 30 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
    Start-Sleep -Seconds 2
    for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Rc 'click 959 831' 2500 | Out-Null }
    Start-Sleep -Seconds 3
    Cmd 'bgod off' 300 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'typing' {
    # 편집기의 입력 칸에 글자를 치는 동안 게임의 단축키(B: 가방)가 같이 반응하지 않아야 한다. 칸을 벗어나면 다시 듣는다.
    # 화면 밖 게임에 보낸 키가 회차 내내 빠질 때가 있다(인계 문서 6절). B가 칸에 쳐지지 않으면 판정하지 않고 INFO를 남긴다.
    function Screens { ((Cmd 'uilist' 200) -replace "`n", ' ') }
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab1' | Out-Null
    $typed = $false
    for ($try = 1; $try -le 2 -and -not $typed; $try++) {
      Cmd 'uifocus ItemQuery' 300 | Out-Null
      Key 0x42 1200
      $typed = (Cmd 'uitext ItemQuery' 150) -match "text='[^']*[bB]"
      if (-not $typed) { "INFO the B key did not reach the game window (try $try)" }
    }
    if ($typed) {
      Check 'the game keys are blocked while typing' ((Cmd 'iguard' 200) -match 'blocking=True') (Cmd 'iguard' 200)
      Check 'typing B in the search box does not open the game bag' ((Screens) -notmatch 'PlayerInfo|PlayerBag') (Screens)
      foreach ($u in 'PlayerInfo', 'PlayerBag') { Cmd "uiclose $u" 300 | Out-Null }
      # 편집기를 닫으면(개발 명령, 키가 빠지는 것을 피한다) 키 설정이 돌아오고 B가 다시 듣는다.
      Cmd 'uitoggle' 500 | Out-Null
      Start-Sleep -Milliseconds 500
      Check 'the editor is closed' ((Cmd 'uistate' 200) -match 'open=False') (Cmd 'uistate' 200)
      Check 'the game keys are restored after leaving the box' ((Cmd 'iguard' 200) -match 'blocking=False') (Cmd 'iguard' 200)
      if ((Screens) -match 'GameMemu') { Rc 'click 959 391' 800 | Out-Null }
      Key 0x42 1500
      $after = Screens
      if ($after -match 'PlayerInfo|PlayerBag') { Check 'B opens the game bag again after leaving the box' $true $after }
      else { "INFO B did not open the bag after leaving the box ($after); key delivery or game state" }
      foreach ($u in 'PlayerInfo', 'PlayerBag') { Cmd "uiclose $u" 300 | Out-Null }
    }
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }  'costs' {
    # 배율 탭의 신기 쿨타임·영력 소모·집중력 소모. 기술마다의 값(회차 rate-1014b의 실측)에 곱하고, 1배로 돌리면 되돌린다.
    function IntoBattle { Cmd 'fight 1011 20' 6000 | Out-Null; Rc 'click 960 684' 12000 | Out-Null; Rc 'click 956 749' 2500 | Out-Null; Cmd 'bgod on' 300 | Out-Null }
    function LeaveBattle {
      Cmd 'bend' 500 | Out-Null
      for ($i = 0; $i -lt 30 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
      Start-Sleep -Seconds 2
      for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Rc 'click 959 831' 2500 | Out-Null }
      Start-Sleep -Seconds 3
    }
    function Num([string]$text, [string]$key) { if ($text -match "\b$key=(-?\d+)") { [int]$Matches[1] } else { -999 } }
    Cmd 'awset 1,2,3' 300 | Out-Null
    Cmd 'potadd 1001 1' 500 | Out-Null; Cmd 'potequip' 500 | Out-Null
    Cmd 'geunlock 7010' 400 | Out-Null; Cmd 'geequip 100721' 400 | Out-Null
    Cmd 'rset artifactCooldown 1' 300 | Out-Null   # x0.5
    Cmd 'rset mpCost 1' 300 | Out-Null             # x0.5
    Cmd 'rset spCost 1' 300 | Out-Null             # x0.5
    IntoBattle
    Start-Sleep -Milliseconds 500
    $c = Cmd 'bcost' 200
    Check 'the right-skill mana cost is halved' ((Num $c 's1Mp') -eq 6) $c
    Check 'the god eye skill mana cost and cooldown are halved' ((Num $c 'eyeMp') -eq 15 -and (Num $c 'eyeCd') -eq 12500) $c
    Check 'the gourd focus cost and cooldown are halved' ((Num $c 'devilSp') -eq 6 -and (Num $c 'devilCd') -eq 25) $c
    Check 'the ultimate and step mana costs are halved' ((Num $c 'ultMp') -eq 13 -and (Num $c 'stepMp') -eq 13) $c
    $mp = Num $c 'mp'; $sp = Num $c 'sp'
    Cmd 'bcast 2' 1500 | Out-Null
    $c2 = Cmd 'bcost' 200
    Check 'casting the right skill spends the halved mana' ($mp - (Num $c2 'mp') -eq 6) "$c | $c2"
    Cmd 'afire 3' 1500 | Out-Null
    $c3 = Cmd 'bcost' 200
    Check 'the gourd spends the halved focus' ($sp - (Num $c3 'sp') -eq 6) "$c2 | $c3"
    Cmd 'rset mpCost 0' 300 | Out-Null; Cmd 'rset spCost 0' 300 | Out-Null; Cmd 'rset artifactCooldown 0' 300 | Out-Null
    Start-Sleep -Milliseconds 500
    $c = Cmd 'bcost' 200
    Check 'back to x1 restores every value' ((Num $c 's1Mp') -eq 11 -and (Num $c 'eyeMp') -eq 30 -and (Num $c 'eyeCd') -eq 25000 -and (Num $c 'devilSp') -eq 12 -and (Num $c 'devilCd') -eq 50) $c
    Cmd 'rset mpCost 4' 300 | Out-Null    # 없음
    Start-Sleep -Milliseconds 500
    $c = Cmd 'bcost' 200
    Check 'none makes mana costs 0' ((Num $c 's1Mp') -eq 0 -and (Num $c 'eyeMp') -eq 0) $c
    Cmd 'rset mpCost 0' 300 | Out-Null
    LeaveBattle
    Cmd 'bgod off' 300 | Out-Null
    Check 'the status line lists nothing after reset' ((Cmd 'uistate' 200) -match 'status=\[hidden\]') (Cmd 'uistate' 200)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'keys-ui' {
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Cmd 'akey 2 None' 300 | Out-Null
    Press 'Tab7' | Out-Null
    Check 'the key rows are drawn' ((IsAt 'ArtifactKeySet2') -and (IsAt 'ArtifactKeySet3') -and (IsAt 'ArtifactKeySet1') -and (IsAt 'ArtifactGameKey'))
    Press 'ArtifactKeySet2' | Out-Null
    Check 'change waits for a key' ((Cmd 'uitext ArtifactKeyText2' 150) -match 'Esc') (Cmd 'uitext ArtifactKeyText2' 150)
    Key 0x78 1000
    Check 'the pressed key becomes the god eye key' ((Cmd 'akeys' 200) -match 'godEye=F9') (Cmd 'akeys' 200)
    Press 'ArtifactKeyClear2' | Out-Null
    Check 'clear removes it' ((Cmd 'akeys' 200) -match 'godEye=None') (Cmd 'akeys' 200)
    $before = (Cmd 'akeys' 200) -match 'off=True'
    Press 'ArtifactGameKey' | Out-Null
    Check 'the game key button toggles' (((Cmd 'akeys' 200) -match 'off=True') -ne $before) (Cmd 'akeys' 200)
    Press 'ArtifactGameKey' | Out-Null
    Shot 'artifact-keys'
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'keys-restart' {
    Check 'the keys are kept after a restart' ((Cmd 'akeys' 200) -match 'devil=K' -and (Cmd 'akeys' 200) -match 'off=False') (Cmd 'akeys' 200)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
