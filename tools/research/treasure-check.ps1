# 법보 탭의 게임 내 검증(docs/superpowers/specs/2026-10-17-treasure-tab-design.md 5절, 05 문서 56절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다. 고정본 인물은 법보·기령이 없다.
#   -Part service   개발 명령(tr·trgive·trequip·trmake·trput·trdel·trlight·trlevel·trpull·trunequip)으로 서비스의 흐름. 끝나면 법보 하나(해제)가 남는다.
#   -Part ui        편집기 탭 16의 줄과 단추로 같은 흐름(service 뒤에 같은 회차에서 돌려도 된다. 다른 번호의 법보·기령을 쓴다).
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

function LogHas([string]$word) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 30 -Encoding UTF8 | Where-Object { $_ -match $word }).Count -gt 0 }
# 그 이름의 버튼이 꺼져 있는지(누르면 "not interactable").
function IsOff([string]$name) { $p = Pos $name; (Rc "click $($p.x) $($p.y)" 300) -match 'not interactable' }
function Tr { Cmd 'tr' 300 }

switch ($Part) {
  'service' {
    $s = Tr
    Check 'the fixture has no treasure and no spirit' ($s -match 'treasures=0' -and $s -match 'spirits=0') $s
    $r = Cmd 'trgive 3031101' 800
    $s = Tr
    Check 'giving adds one treasure' ($s -match 'treasures=1 \[(\w+):3031101:') "$r | $s"
    $sole = $Matches[1]
    Check 'the new treasure is unequipped and empty' ($s -match "\[$sole:3031101:[^\]]*:100/100:-:0\]") $s
    $r = Cmd "trequip $sole" 800
    Check 'equip succeeds' ($r -match '장착했습니다' -and (Tr) -match "\[$sole:[^\]]*:eq:0\]") "$r | $(Tr)"
    $r = Cmd 'trmake 1001' 800
    $s = Tr
    Check 'making a spirit adds one at level 1 with dark talents' ($r -match '만들었습니다' -and $s -match 'spirits=1 \[(\d+):1001:[^:]+:Lv1/5:0/200:0/\d+:-\]') "$r | $s"
    $sp = $Matches[1]
    $r = Cmd 'trmake 1001' 800
    Check 'a second spirit of the same kind is refused' ($r -match '하나만' -and (Tr) -match 'spirits=1 ') $r
    $r = Cmd "trput $sole $sp" 800
    Check 'the spirit goes into the equipped treasure' ($r -match '넣었습니다' -and (Tr) -match "\[$sole:[^\]]*:eq:$sp\]" -and (Tr) -match "\[$sp:1001:[^\]]*:$sole\]") "$r | $(Tr)"
    $r = Cmd "trdel $sp" 800
    Check 'a spirit inside a treasure is not deleted' ($r -match '먼저 빼세요' -and (Tr) -match 'spirits=1 ') $r
    $r = Cmd "trlight $sp" 2000
    Check 'all shown talents are lit' ($r -match '재능 \d+개를 켰습니다' -and (Tr) -match "\[$sp:1001:[^\]]*:(\d+)/\1:") "$r | $(Tr)"
    $r = Cmd "trlevel $sp" 800
    Check 'the level goes up by one' ($r -match 'Lv1 → Lv2' -and (Tr) -match "\[$sp:1001:[^:]+:Lv2/5:") "$r | $(Tr)"
    $r = Cmd "trpull $sole" 800
    Check 'pulling empties the treasure and keeps the spirit' ($r -match '뺐습니다' -and (Tr) -match "\[$sole:[^\]]*:eq:0\]" -and (Tr) -match "\[$sp:1001:[^\]]*:-\]") "$r | $(Tr)"
    "INFO pull path: $r"
    $r = Cmd "trdel $sp" 800
    Check 'a free spirit is deleted' ($r -match '지웠습니다' -and (Tr) -match 'spirits=0') "$r | $(Tr)"
    $r = Cmd 'trunequip' 800
    Check 'unequip succeeds' ($r -match '해제했습니다' -and (Tr) -match "\[$sole:[^\]]*:-:0\]") "$r | $(Tr)"
    Check 'the change log has treasure lines' (LogHas '법보')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'ui' {
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
    Check 'the make screen lists the spirits' ((IsAt 'SpiritMake1002') -and (IsAt 'SpiritMake2024'))
    Cmd 'ui set SpiritKindSearch 창애' | Out-Null
    Check 'the search narrows the list' ((Cmd 'uipos SpiritMake1001') -match 'no element named' -and (IsAt 'SpiritMake1002'))
    Press 'SpiritMake1002' | Out-Null
    $s = Tr
    Check 'a spirit is made from the screen' ($s -match '\[(\d+):1002:') $s
    $sp = $Matches[1]
    Check 'the make button turns off for an owned spirit' (IsOff 'SpiritMake1002')
    Press 'Back' | Out-Null
    $s = Tr
    Check 'the treasure is listed' ($s -match '\[(\w+):3031102:') $s
    $sole = $Matches[1]
    Check 'put is off while no treasure is equipped' (IsOff "SpiritPut$sp")
    Press "TreasureEquip$sole" | Out-Null
    Check 'the equip button equips' ((Tr) -match "\[$sole:[^\]]*:eq:0\]" -and (Cmd "uitext TreasureState$sole") -match "text='장착 중'") (Tr)
    Press "SpiritPut$sp" | Out-Null
    Check 'the put button puts the spirit into the equipped treasure' ((Tr) -match "\[$sole:[^\]]*:eq:$sp\]" -and (Cmd "uitext TreasureSpirit$sole") -match 'Lv1') "$(Tr) | $(Cmd "uitext TreasureSpirit$sole")"
    Check 'delete is off while the spirit is inside' (IsOff "SpiritDel$sp")
    Press "SpiritLight$sp" 2000 | Out-Null
    Check 'the light button lights every shown talent' ((Cmd "uitext SpiritTalents$sp") -match "text='재능 (\d+)/\1'") (Cmd "uitext SpiritTalents$sp")
    Check 'the light button turns off afterwards' (IsOff "SpiritLight$sp")
    Press "SpiritLevel$sp" | Out-Null
    Check 'the level button raises the level' ((Cmd "uitext SpiritState$sp") -match "text='Lv 2/") (Cmd "uitext SpiritState$sp")
    Shot 'treasure'
    Press "TreasurePull$sole" | Out-Null
    Check 'the pull button empties the treasure' ((Tr) -match "\[$sole:[^\]]*:eq:0\]" -and (Cmd "uitext TreasureSpirit$sole") -match '없음') (Tr)
    Check 'the pull button turns off when empty' (IsOff "TreasurePull$sole")
    Press "SpiritDel$sp" | Out-Null
    Check 'delete asks first' ((Tr) -match "\[$sp:1002:" -and (Cmd "uitext SpiritDel$sp") -match '정말') (Cmd "uitext SpiritDel$sp")
    Press "SpiritDel$sp" 1500 | Out-Null
    Check 'the second press deletes the spirit' ((Tr) -notmatch "\[$sp:1002:") (Tr)
    Press "TreasureEquip$sole" | Out-Null
    Check 'the unequip button unequips' ((Tr) -match "\[$sole:[^\]]*:-:0\]") (Tr)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
