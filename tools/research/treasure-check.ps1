# 법보 탭의 게임 내 검증(docs/superpowers/specs/2026-10-17-treasure-tab-design.md 5절, 05 문서 56절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다. 고정본 인물은 법보·기령이 없다.
#   -Part service   개발 명령(tr·trgive·trequip·trmake·trput·trdel·trlight·trlevel·trpull·trunequip)으로 서비스의 흐름. 끝나면 법보 하나(해제)가 남는다.
#   -Part ui        편집기 탭 16의 줄과 단추로 같은 흐름(service 뒤에 같은 회차에서 돌려도 된다. 다른 번호의 법보·기령을 쓴다).
#   -Part repair    수리(05 문서 60절): 개발 명령 trdur로 내구를 낮추고 trrepair·탭의 수리·모두 수리 단추로 채운다. 끝에서 저장한다 → session.ps1 -Restart → -Part repair-restart.
#   -Part repair-restart  저장한 뒤 다시 켜도 수리한 내구가 남는지.
#   -Part forge     트레이너 3차: trart·trgrade·trstage·trsp·trclose·trnpc로 경지 올리기(10에서 멈춤, 비율 유지)·단계 1~6·기령 친밀도 최대·NPC 대상.
#   -Part give      지급의 비율 100·경지·단계, 공용 지급 길, 기존 법보 비율 그대로(05 문서 82절).
#   -Part forge-ui  탭의 강화 화면(경지 +1·경지 10까지 두 번 누르기·단계 칩)·지급 칩·친밀 최대. 캡처 check-treasure-forge.png.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

# 그 이름의 버튼이 꺼져 있는지(누르면 "not interactable").
function Tr { Cmd 'tr' 300 }

switch ($Part) {
  'service' {
    $traps = Traps
    $s = Tr
    Check 'the fixture has no treasure and no spirit' ($s -match 'treasures=0' -and $s -match 'spirits=0') $s
    $r = Cmd 'trgive 3031101' 800
    $s = Tr
    Check 'giving adds one treasure' ($s -match 'treasures=1 \[(\w+):3031101:') "$r | $s"
    $sole = $Matches[1]
    Check 'the new treasure is unequipped and empty' ($s -match "\[${sole}:3031101:[^\]]*:100/100:-:0\]") $s
    $r = Cmd "trequip $sole" 800
    Check 'equip succeeds' ($r -match '장착했습니다' -and (Tr) -match "\[${sole}:[^\]]*:eq:0\]") "$r | $(Tr)"
    $r = Cmd 'trmake 1001' 800
    $s = Tr
    Check 'making a spirit adds one at level 1 with dark talents' ($r -match '만들었습니다' -and $s -match 'spirits=1 \[(-?\d+):1001:[^:]+:Lv1/5:0/200:0/\d+:-\]') "$r | $s"
    $sp = $Matches[1]
    $r = Cmd 'trmake 1001' 800
    Check 'a second spirit of the same kind is refused' ($r -match '하나만' -and (Tr) -match 'spirits=1 ') $r
    $r = Cmd "trput $sole $sp" 800
    Check 'the spirit goes into the equipped treasure' ($r -match '넣었습니다' -and (Tr) -match "\[${sole}:[^\]]*:eq:$sp\]" -and (Tr) -match "\[${sp}:1001:[^\]]*:$sole\]") "$r | $(Tr)"
    $r = Cmd "trdel $sp" 800
    Check 'a spirit inside a treasure is not deleted' ($r -match '먼저 빼세요' -and (Tr) -match 'spirits=1 ') $r
    $r = Cmd "trlight $sp" 2000
    Check 'all shown talents are lit' ($r -match '재능 \d+개를 켰습니다' -and (Tr) -match "\[${sp}:1001:[^\]]*:(\d+)/\1:") "$r | $(Tr)"
    $r = Cmd "trlevel $sp" 800
    Check 'the level goes up by one' ($r -match 'Lv1 → Lv2' -and (Tr) -match "\[${sp}:1001:[^:]+:Lv2/5:") "$r | $(Tr)"
    $r = Cmd "trpull $sole" 800
    Check 'pulling empties the treasure and keeps the spirit' ($r -match '뺐습니다' -and (Tr) -match "\[${sole}:[^\]]*:eq:0\]" -and (Tr) -match "\[${sp}:1001:[^\]]*:-\]") "$r | $(Tr)"
    "INFO pull path: $r"
    $r = Cmd "trdel $sp" 800
    Check 'a free spirit is deleted' ($r -match '지웠습니다' -and (Tr) -match 'spirits=0') "$r | $(Tr)"
    $r = Cmd 'trunequip' 800
    Check 'unequip succeeds' ($r -match '해제했습니다' -and (Tr) -match "\[${sole}:[^\]]*:-:0\]") "$r | $(Tr)"
    Check 'the change log has treasure lines' (LogHasLine '법보')
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'ui' {
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab16' | Out-Null
    Check 'the treasure tab is selected' ((Cmd 'uistate') -match 'tab=16 ') (Cmd 'uistate')
    Check 'the tab shows its buttons' ((IsAt 'TreasureMakeOpen') -and (IsAt 'TreasureGiveOpen'))
    $before = Tr
    Press 'TreasureGiveOpen' | Out-Null
    Check 'the give screen lists the ten treasures' ((IsAt 'TreasureGive3031101') -and (IsAt 'TreasureGive3031110'))
    Press 'TreasureGive3031102' | Out-Null
    Check 'a treasure is given from the screen' ((Tr) -match '3031102') (Tr)
    Press 'Back' | Out-Null
    Press 'TreasureMakeOpen' | Out-Null
    Cmd 'ui set SpiritKindSearch 100' | Out-Null
    Check 'the make screen lists the spirits' ((IsAt 'SpiritMake1002') -and (IsAt 'SpiritMake1001'))
    Cmd 'ui set SpiritKindSearch 창애' | Out-Null
    Check 'the search narrows the list' ((Cmd 'uipos SpiritMake1001') -match 'no element named' -and (IsAt 'SpiritMake1002'))
    Press 'SpiritMake1002' | Out-Null
    $s = Tr
    Check 'a spirit is made from the screen' ($s -match '\[(-?\d+):1002:') $s
    $sp = $Matches[1]
    Check 'the make button turns off for an owned spirit' (ButtonIsOff 'SpiritMake1002')
    Press 'Back' | Out-Null
    $s = Tr
    Check 'the treasure is listed' ($s -match '\[(\w+):3031102:') $s
    $sole = $Matches[1]
    Check 'put is off while no treasure is equipped' (ButtonIsOff "SpiritPut$sp")
    Press "TreasureEquip$sole" | Out-Null
    Check 'the equip button equips' ((Tr) -match "\[${sole}:[^\]]*:eq:0\]" -and (Cmd "uitext TreasureState$sole") -match "text='장착 중'") (Tr)
    Press "SpiritPut$sp" | Out-Null
    Check 'the put button puts the spirit into the equipped treasure' ((Tr) -match "\[${sole}:[^\]]*:eq:$sp\]" -and (Cmd "uitext TreasureSpirit$sole") -match 'Lv1') "$(Tr) | $(Cmd "uitext TreasureSpirit$sole")"
    Check 'delete is off while the spirit is inside' (ButtonIsOff "SpiritDel$sp")
    Press "SpiritLight$sp" 2000 | Out-Null
    Check 'the light button lights every shown talent' ((Cmd "uitext SpiritTalents$sp") -match "text='재능 (\d+)/\1'") (Cmd "uitext SpiritTalents$sp")
    Check 'the light button turns off afterwards' (ButtonIsOff "SpiritLight$sp")
    Press "SpiritLevel$sp" | Out-Null
    Check 'the level button raises the level' ((Cmd "uitext SpiritState$sp") -match "text='Lv 2/") (Cmd "uitext SpiritState$sp")
    Shot 'treasure'
    Press "TreasurePull$sole" | Out-Null
    Check 'the pull button empties the treasure' ((Tr) -match "\[${sole}:[^\]]*:eq:0\]" -and (Cmd "uitext TreasureSpirit$sole") -match '없음') (Tr)
    Check 'the pull button turns off when empty' (ButtonIsOff "TreasurePull$sole")
    Press "SpiritDel$sp" | Out-Null
    Check 'delete asks first' ((Tr) -match "\[${sp}:1002:" -and (Cmd "uitext SpiritDel$sp") -match '정말') (Cmd "uitext SpiritDel$sp")
    Press "SpiritDel$sp" 1500 | Out-Null
    Check 'the second press deletes the spirit' ((Tr) -notmatch "\[${sp}:1002:") (Tr)
    Press "TreasureEquip$sole" | Out-Null
    Check 'the unequip button unequips' ((Tr) -match "\[${sole}:[^\]]*:-:0\]") (Tr)
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'repair' {
    $traps = Traps
    $soleFile = Join-Path $work 'treasure-repair-soles.txt'
    Cmd 'trgive 3031103' 800 | Out-Null; Cmd 'trgive 3031104' 800 | Out-Null
    $s = Tr
    $a = if ($s -match '\[(\w+):3031103:[^\]]*:(\d+)/(\d+):') { $Matches[1] } else { '' }
    $max = [int]$Matches[3]
    $b = if ($s -match '\[(\w+):3031104:') { $Matches[1] } else { '' }
    Check 'two treasures were given with a known maximum durability' ($a -ne '' -and $b -ne '' -and $max -gt 0) $s
    Check 'repair is refused while the durability is full' ((Cmd "trrepair $a" 800) -match '이미 가득') (Cmd "trrepair $a" 800)
    Cmd "trdur $a 10" 300 | Out-Null
    Check 'the durability can be lowered for the test' ((Tr) -match "\[${a}:3031103:[^\]]*:10/${max}:") (Tr)
    $r = Cmd "trrepair $a" 800
    Check 'repair fills the durability' ($r -match '수리했습니다' -and (Tr) -match "\[${a}:3031103:[^\]]*:${max}/${max}:") "$r | $(Tr)"
    Check 'repair is in the change log' (LogHasLine '내구 10')
    Cmd "trdur $a 5" 300 | Out-Null; Cmd "trdur $b 7" 300 | Out-Null
    $r = Cmd 'trrepair all' 1500
    Check 'repair-all fills every worn treasure' ($r -match '2개를 수리했습니다' -and (Tr) -match "\[${a}:[^\]]*:${max}/${max}:" -and (Tr) -match "\[${b}:3031104:[^\]]*:(\d+)/\1:") "$r | $(Tr)"
    $r = Cmd 'trrepair all' 800
    Check 'repair-all says when nothing is worn' ($r -match '수리할 법보가 없습니다') $r
    # 탭의 단추
    Cmd "trdur $a 20" 300 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab16' | Out-Null
    Check 'the row repair button is off for a full treasure' (ButtonIsOff "TreasureRepair$b")
    Press "TreasureRepair$a" | Out-Null
    Check 'the row repair button repairs' ((Tr) -match "\[${a}:3031103:[^\]]*:${max}/${max}:" -and (Cmd 'uistate') -match '수리했습니다') "$(Tr) | $(Cmd 'uistate')"
    Check 'the row repair button turns off afterwards' (ButtonIsOff "TreasureRepair$a")
    Check 'repair-all is off when nothing is worn' (ButtonIsOff 'TreasureRepairAll')
    Cmd "trdur $a 3" 300 | Out-Null; Cmd "trdur $b 4" 300 | Out-Null
    Press 'Tab1' | Out-Null; Press 'Tab16' | Out-Null   # 다시 그려 단추 상태를 새로 읽는다
    Press 'TreasureRepairAll' 1500 | Out-Null
    Check 'the repair-all button repairs both' ((Cmd 'uistate') -match '2개를 수리했습니다' -and (Tr) -match "\[${a}:[^\]]*:${max}/${max}:" -and (Tr) -match "\[${b}:[^\]]*:(\d+)/\1:") "$(Tr) | $(Cmd 'uistate')"
    Shot 'treasure-repair'
    # 하나는 닳은 채로 저장해 다시 켠 뒤 둘 다 확인한다
    Cmd "trdur $b 9" 300 | Out-Null
    [IO.File]::WriteAllText($soleFile, "$a $b $max")
    Check 'the game saves' ((Cmd 'save' 5000) -notmatch 'ERROR') 'save'
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'repair-restart' {
    $parts = ([IO.File]::ReadAllText((Join-Path $work 'treasure-repair-soles.txt'))).Split(' ')
    $a = $parts[0]; $b = $parts[1]; $max = $parts[2]
    $s = Tr
    Check 'the repaired durability survives a restart' ($s -match "\[${a}:3031103:[^\]]*:${max}/${max}:") $s
    Check 'the lowered durability survives a restart too (the write is real save data)' ($s -match "\[${b}:3031104:[^\]]*:9/") $s
  }
  'forge' {
    $traps = Traps
    # 트레이너 3차(스펙 2026-10-23-trainer3-design.md 6절): 경지 올리기·10에서 멈춤·비율 유지, 단계 1~6, 기령 친밀도 최대, NPC 대상.
    function Art([string]$s) { Cmd "trart $s" 300 }
    Cmd 'trnpc me' 200 | Out-Null
    $r = Cmd 'trgive 3031108' 800
    if ((Tr) -match '\[(\w+):3031108:') { $sole = $Matches[1] } else { $sole = '' }
    $a = Art $sole
    Check 'a given treasure starts at grade 3' ($a -match 'grade=3 ') "$r | $a"
    $null = $a -match 'rate=([\d/]+)'; $rate0 = $Matches[1]
    $null = $a -match 'pre=(\d+)/'; $pre0 = [int]$Matches[1]
    $r = Cmd "trgrade $sole" 800
    $a = Art $sole
    Check 'grade up adds one grade and one option' ($a -match 'grade=4 ' -and $a -match "pre=$($pre0 + 1)/") "$r | $a"
    Check 'grade up keeps the rates' ($a -match "rate=$rate0 ") "$rate0 | $a"
    $r = Cmd "trgrade $sole 10" 3000
    $a = Art $sole
    Check 'grade up to 10 stops at 10' ($a -match 'grade=10 ' -and $a -match "rate=$rate0 ") "$r | $a"
    $r = Cmd "trgrade $sole" 800
    $a = Art $sole
    Check 'grade at 10 is refused' ($r -match '이미 경지 10' -and $a -match 'grade=10 ') "$r | $a"
    $want = @{ 1 = 60; 2 = 80; 3 = 85; 4 = 95; 5 = 100; 6 = 120 }
    foreach ($n in 1..6) {
      $r = Cmd "trstage $sole $n" 500
      $a = Art $sole
      $v = $want[$n]
      Check "stage $n sets level $n and rates $v" ($a -match "level=$n " -and $a -match "rate=$v/$v/$v/$v/$([Math]::Min($v, 100)) ") "$r | $a"
    }
    $r = Cmd 'trmake 1001' 800
    $s = Cmd 'trsp' 300
    if ($s -match '(-?\d+):1001 intim=(\d+)/(\d+)') { $sp = $Matches[1]; $i0 = [int]$Matches[2]; $max = [int]$Matches[3] } else { $sp = ''; $i0 = -1; $max = -1 }
    Check 'a new spirit starts below the maximum' ($i0 -ge 0 -and $i0 -lt $max -and $max -gt 0) $s
    $r = Cmd "trclose $sp" 500
    Check 'intimacy goes to the maximum' ((Cmd 'trsp' 300) -match "${sp}:1001 intim=$max/$max") "$r | $(Cmd 'trsp' 300)"
    $r = Cmd "trclose $sp" 500
    Check 'a second press says it is already full' ($r -match '이미') $r
    # NPC 대상: 그 NPC의 법보·기령이 바뀌고 플레이어 쪽은 그대로
    $mine = Art $sole
    $r = Cmd 'trnpc any' 300
    Check 'an npc is targeted' ($r -match 'target=') $r
    $r = Cmd 'trgive 3031101' 800
    if ((Tr) -match '\[(\w+):3031101:') { $ns = $Matches[1] } else { $ns = '' }
    $r = Cmd "trgrade $ns" 800
    Check 'npc target: grade up on the npc treasure' ((Art $ns) -match 'grade=3 ') "$r | $(Art $ns)"
    $r = Cmd 'trmake 1002' 800
    if ((Cmd 'trsp' 300) -match '(-?\d+):1002 intim=') { $nsp = $Matches[1] } else { $nsp = '' }
    $r = Cmd "trclose $nsp" 500
    Check 'npc target: intimacy on the npc spirit' ((Cmd 'trsp' 300) -match "${nsp}:1002 intim=$max/$max") "$r | $(Cmd 'trsp' 300)"
    Cmd 'trnpc me' 200 | Out-Null
    Check 'npc target: the player treasure is unchanged' ((Art $sole) -eq $mine) "$mine | $(Art $sole)"
    Check 'npc target: the player has no 1002 spirit' ((Cmd 'trsp' 300) -notmatch ':1002 ') (Cmd 'trsp' 300)
    Check 'the change log has forge lines' (LogHasLine '경지' -and (LogHasLine '친밀도'))
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'give' {
    $traps = Traps
    # 트레이너 3차 6절: 편집기 지급의 비율 0 고침(공용 길)과 법보 지급의 경지·단계. 다른 부분 뒤에 돌려도 된다(번호로 새 sole을 찾는다).
    function Art([string]$s) { Cmd "trart $s" 300 }
    function Soles([int]$id) { $all = @([regex]::Matches((Tr), "\[(\w+):${id}:") | ForEach-Object { $_.Groups[1].Value }); ,$all }
    function NewSole([int]$id, $before) { @((Soles $id) | Where-Object { $before -notcontains $_ })[0] }
    Cmd 'trnpc me' 200 | Out-Null
    $b = Soles 3031103
    $r = Cmd 'trgive 3031103' 800
    $s = NewSole 3031103 $b
    $a = Art $s
    Check 'a plain give has rates 100 and stage 5' ($a -match 'level=5 ' -and $a -match 'rate=100/100/100/100/100 ') "$r | $a"
    $b = Soles 3031102
    $r = Cmd 'trgive 3031102 6 6' 3000
    $s2 = NewSole 3031102 $b
    $a = Art $s2
    Check 'give with grade 6 and stage 6' ($a -match 'grade=6 ' -and $a -match 'level=6 ' -and $a -match 'rate=120/120/120/120/100 ') "$r | $a"
    $b = Soles 3031104
    $r = Cmd 'trgive 3031104 12 3' 5000
    $s3 = NewSole 3031104 $b
    $a = Art $s3
    Check 'give with grade 12 stops at 10, stage 3' ($a -match 'grade=10 ' -and $a -match 'level=3 ' -and $a -match 'rate=85/85/85/85/85 ') "$r | $a"
    Cmd "trstage $s 2" 500 | Out-Null
    $b = Soles 3031103
    $r = Cmd 'cmgive 3031103 2' 1000
    $news = @((Soles 3031103) | Where-Object { $b -notcontains $_ })
    Check 'the common give path fixes every new treasure' ($news.Count -eq 2 -and @($news | Where-Object { (Art $_) -match 'rate=100/100/100/100/100 ' }).Count -eq 2) "$r | $($news -join ',')"
    Check 'existing treasure keeps its rates' ((Art $s) -match 'level=2 ' -and (Art $s) -match 'rate=80/80/80/80/80 ') (Art $s)
    # 프리셋의 법보와 기령 줄(SpriteService.Apply)도 편집기 지급이다(리뷰 지적 2)
    $b = Soles 3031109
    $r = Cmd 'psprite 1004 3031109' 1500
    $s4 = NewSole 3031109 $b
    Check 'the preset sprite line gives a treasure with rates 100' ($s4 -and (Art $s4) -match 'rate=100/100/100/100/100 ') "$r | $(Art $s4)"
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'forge-ui' {
    $traps = Traps
    # 트레이너 3차 5.1~5.3: 줄의 강화… → 경지 +1·단계 6·경지 10까지, 지급 화면의 경지·단계 칩, 기령 줄의 친밀도 최대.
    function Art([string]$s) { Cmd "trart $s" 300 }
    Cmd 'trnpc me' 200 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab16' | Out-Null
    $b = @([regex]::Matches((Tr), '\[(\w+):3031107:') | ForEach-Object { $_.Groups[1].Value })
    Cmd 'trgive 3031107' 800 | Out-Null
    $sole = @([regex]::Matches((Tr), '\[(\w+):3031107:') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $b -notcontains $_ })[0]
    Press 'Tab15' | Out-Null; Press 'Tab16' | Out-Null
    $g0 = if ((Art $sole) -match 'grade=(\d+) ') { [int]$Matches[1] } else { -1 }
    Check 'the row has a forge button' (IsAt "TreasureForge$sole") $sole
    Press "TreasureForge$sole" | Out-Null
    Check 'the forge screen shows its info' ((Cmd 'uitext ForgeInfo') -match '단계 5') (Cmd 'uitext ForgeInfo')
    Press 'ForgeGradeUp' | Out-Null
    Check 'grade +1 from the screen' ((Art $sole) -match "grade=$($g0 + 1) ") (Art $sole)
    Press 'ForgeStage6' | Out-Null
    Check 'stage 6 from the screen' ((Art $sole) -match 'level=6 ' -and (Art $sole) -match 'rate=120/') (Art $sole)
    Check 'the stage 6 chip says the limit break' ((Cmd 'uitext ForgeStage6') -match '한계돌파') (Cmd 'uitext ForgeStage6')
    # 끝까지 올리기는 두 번 누르기(05 문서 89절): 첫 누름은 묻기만 하고 단추 글이 바뀐다.
    Press 'ForgeGradeMax' | Out-Null
    Check 'grade to 10 asks first' ((Art $sole) -match "grade=$($g0 + 1) " -and (Cmd 'uitext ForgeGradeMax') -match '정말 올리기') "$(Art $sole) | $(Cmd 'uitext ForgeGradeMax')"
    Press 'ForgeGradeMax' | Out-Null
    Check 'grade to 10 from the screen' ((Art $sole) -match 'grade=10 ' -and (Art $sole) -match 'rate=120/') (Art $sole)
    Check 'grade buttons are off at 10' ((ButtonIsOff 'ForgeGradeUp') -and (ButtonIsOff 'ForgeGradeMax'))
    Shot 'treasure-forge'
    Press 'Back' | Out-Null
    Press 'TreasureGiveOpen' | Out-Null
    Check 'the give screen has grade and stage chips' ((IsAt 'GiveGrade0') -and (IsAt 'GiveGrade10') -and (IsAt 'GiveStage1') -and (IsAt 'GiveStage6'))
    Press 'GiveGrade8' | Out-Null; Press 'GiveStage4' | Out-Null
    $b = @([regex]::Matches((Tr), '\[(\w+):3031105:') | ForEach-Object { $_.Groups[1].Value })
    Press 'TreasureGive3031105' | Out-Null
    $s2 = @([regex]::Matches((Tr), '\[(\w+):3031105:') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $b -notcontains $_ })[0]
    Check 'give with grade 8 and stage 4 from the screen' ((Art $s2) -match 'grade=8 ' -and (Art $s2) -match 'level=4 ') (Art $s2)
    Press 'Back' | Out-Null
    $spOut = Cmd 'trmake 1003' 800
    Press 'Tab15' | Out-Null; Press 'Tab16' | Out-Null
    $sp = if ((Cmd 'trsp' 300) -match '(-?\d+):1003 intim=') { $Matches[1] } else { '' }
    Check 'the spirit row shows intimacy' ((Cmd "uitext SpiritCloseText$sp") -match '친밀') (Cmd "uitext SpiritCloseText$sp")
    Press "SpiritClose$sp" | Out-Null
    Check 'intimacy max from the screen' ((Cmd 'trsp' 300) -match "${sp}:1003 intim=(\d+)/\1") (Cmd 'trsp' 300)
    Check 'the intimacy button is off when full' (ButtonIsOff "SpiritClose$sp")
    Check 'no callback exception in the log' ((Traps) -eq $traps)
    Cmd 'uitoggle' | Out-Null
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
