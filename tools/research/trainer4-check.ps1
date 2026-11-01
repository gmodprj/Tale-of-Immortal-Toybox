# 트레이너 4차 전투 묶음(스펙 docs/superpowers/specs/2026-10-28-trainer4-battle-design.md, 05 문서 87절)의 게임 내 검증.
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base -Exclude 2814696167로 게임이 떠서 세계에 들어가 있어야 한다. ui·items는 한 회차, 나머지는 부분마다 깨끗한 회차(06 문서 5절).
#   -Part ui       배율 탭: 칩별 줄(전투 이동 속도는 시간·속도, 전투 칩 8줄, 법보 내구는 관계·소모), 새 줄 설명, 주는 피해 일격 단계, 켜 둔 것과 화면 위 표시.
#   -Part items    전투 아이템 쿨타임: x1이면 단약 두 번째 거부, 없음이면 단약·법보 연달아, x1로 돌리면 itemCDRate 100.
#   -Part skills   기술 쿨타임의 선법·영역: 등선(40)·선법 장착·영역 넣기 뒤 전투, 없음이면 cd 100과 연달아 발동, x1·x0.5에서 원래 값과 겹치지 않음.
#   -Part onehit   일격: 수준 60 적(무적)이 한 방에 쓰러짐, 무적 없이 수준 20 적에게 맞아도 플레이어는 일격을 받지 않음.
#   -Part durable  법보 내구 소모: 없음이면 처음 쓰기 뒤와 trdur로도 내구 그대로, x1이면 2 줄어듦, 없음에서도 수리는 됨.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

# 그 줄을 n번 누른다(편집기를 열고 닫는다).
function Step([string]$row, [int]$n) { OpenEditorTab 12; 1..$n | ForEach-Object { Press $row | Out-Null }; $v = UiText $row; Press 'Close' | Out-Null; Start-Sleep -Milliseconds 500; $v }

Cmd 'uistate' 300 | Out-Null
switch ($Part) {
  'ui' {
    $traps = Traps
    OpenEditorTab 12
    Cmd 'ratechip 0' | Out-Null
    Press 'RateGroup1' | Out-Null
    Check 'time chip has battle move' ((UiHas 'RateBattleMove') -and (UiHas 'RateMapSpeed') -and (UiHas 'RateSpeed') -and (UiHas 'RateMove') -and (UiHas 'RateActionDay'))
    Press 'RateGroup2' | Out-Null
    Check 'battle chip has the eight rows' ((UiHas 'RateCooldown') -and (UiHas 'RateItemCooldown') -and (UiHas 'RateArtifactCooldown') -and (UiHas 'RateMpCost') -and (UiHas 'RateSpCost') -and (UiHas 'Multiplier') -and (UiHas 'RateTaken') -and (UiHas 'God') -and -not (UiHas 'RateBattleMove'))
    Check 'item cooldown row starts at x1 with its note' ((UiText 'RateItemCooldown') -eq 'x1' -and (UiText 'RateNoteItemCooldown') -match '전투 아이템 칸') "$(UiText 'RateItemCooldown') | $(UiText 'RateNoteItemCooldown')"
    Check 'skill cooldown note names immortal and field skills' ((UiText 'RateNoteCooldown') -match '선법·영역' -and (UiText 'RateNoteCooldown') -match '명우') (UiText 'RateNoteCooldown')
    Check 'damage note names one hit' ((UiText 'MultiplierNote') -match '일격') (UiText 'MultiplierNote')
    1..5 | ForEach-Object { Press 'Multiplier' | Out-Null }
    Check 'damage reaches one hit' ((UiText 'Multiplier') -eq '일격') (UiText 'Multiplier')
    Check 'the battle chip counts one hit as on' ((UiText 'RateGroup2') -eq '전투 1') (UiText 'RateGroup2')
    1..4 | ForEach-Object { Press 'RateItemCooldown' | Out-Null }
    Check 'item cooldown reaches none' ((UiText 'RateItemCooldown') -eq '없음') (UiText 'RateItemCooldown')
    Press 'RateGroup3' | Out-Null
    Check 'life chip has artifact durable' ((UiHas 'RateArtifactDurable') -and (UiHas 'RateItemUse') -and (UiText 'RateNoteArtifactDurable') -match '법보의 내구') (UiText 'RateNoteArtifactDurable')
    Press 'RateArtifactDurable' | Out-Null
    Check 'artifact durable reaches none' ((UiText 'RateArtifactDurable') -eq '없음') (UiText 'RateArtifactDurable')
    Press 'RateGroup4' | Out-Null
    Check 'the on chip lists the three' ((UiText 'RateGroup4') -eq '켜 둔 것 3' -and (UiHas 'Multiplier') -and (UiHas 'RateItemCooldown') -and (UiHas 'RateArtifactDurable')) (UiText 'RateGroup4')
    Start-Sleep -Milliseconds 800
    $st = Cmd 'uistate'
    Check 'the status line shows them' ($st -match '피해 일격' -and $st -match '아이템 쿨타임 없음' -and $st -match '법보 내구 없음') $st
    # 켜 둔 것 칩에서는 x1이 되는 순간 줄이 사라지므로 전투 칩에서 돌린다.
    Press 'RateGroup2' | Out-Null
    Press 'Multiplier' | Out-Null
    Check 'damage wraps from one hit to x1' ((UiText 'Multiplier') -eq 'x1') (UiText 'Multiplier')
    Press 'RateReset' | Out-Null
    Press 'RateGroup4' | Out-Null
    Check 'reset turns the new rows off' ((UiText 'RateGroup4') -eq '켜 둔 것' -and (UiHas 'RateEmpty')) (UiText 'RateGroup4')
    Press 'Tab14' | Out-Null
    $lines = Cmd 'uidescs HistoryLine' 300
    Check 'one hit is in the history' ($lines -match '일격') $lines
    Cmd 'ratechip all' | Out-Null
    Press 'Close' | Out-Null
    Shot 'trainer4-ui'
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'items' {
    $traps = Traps
    # 법보 칸도 보려고 법보를 지급·장착한다(고정본에는 장착 법보가 없다).
    Cmd 'trgive 3031101' 800 | Out-Null
    $sole = if ((Cmd 'tr' 200) -match '\[(\w+):3031101') { $Matches[1] } else { '' }
    Check 'an artifact is equipped for the slot' ($sole -ne '' -and (Cmd "trequip $sole" 800) -match '장착') $sole
    EnterBattleScene 5
    $c = Cmd 'cdall' 200
    Check 'the battle has pills and the artifact slot' ($c -match 'prop3 PropItemPill' -and $c -match 'prop4 PropItemPill') $c
    # 칸 3(1011101, 20초)을 쓴다. 칸 4(1011161)는 다른 단약과 쿨타임을 나누지 않는다(회차 t4-a).
    Check 'a pill works once at x1' ((Cmd 'tbbuse 3' 400) -match 'IsCreate=True') ''
    Check 'the same pill is refused at x1' ((Cmd 'tbbuse 3' 400) -match 'IsCreate=False') ''
    Check 'item cooldown is none' ((Step 'RateItemCooldown' 4) -eq '없음')
    $c = Cmd 'cdall' 200
    Check 'item rate is raised' ((NumIn $c 'item=(\d+)/') -ge 1000000) $c
    $ok = @(1..2 | ForEach-Object { Cmd 'tbbuse 3' 400 } | Where-Object { $_ -match 'IsCreate=True' }).Count
    Check 'the pill is used twice in a row' ($ok -eq 2) "ok=$ok"
    $art = if ($c -match 'prop(\d) PropItemArtifact') { $Matches[1] } else { '' }
    if ($art -ne '') {
      $ok = @(1..2 | ForEach-Object { Cmd "tbbuse $art" 400 } | Where-Object { $_ -match 'IsCreate=True' }).Count
      Check 'the artifact is used twice in a row' ($ok -eq 2) "ok=$ok"
    } else { 'INFO no artifact equipped in the fixture; artifact slot not checked' }
    Check 'item cooldown wraps to x1' ((Step 'RateItemCooldown' 1) -eq 'x1')
    $c = Cmd 'cdall' 200
    Check 'item rate is back to 100' ($c -match 'item=100/100') $c
    LeaveBattleScene
    Check 'back on the map' ((Cmd 'uilist' 100) -match 'MapMain')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'skills' {
    $traps = Traps
    Cmd 'setgrade 40' 500 | Out-Null
    Check 'the immortal card is equipped' ((Cmd 'iequip 5000 0' 500) -match 'EquipCard=True') ''
    Check 'the field skill is set' ((Cmd 'fieldset 1 300' 300) -match 'IsFieldSkill=True') ''
    EnterBattleScene 5
    $c = Cmd 'cdall' 200
    Check 'the battle has the immortal and field skill' ($c -match 'imm0 id=5000 cd=60000/' -and $c -match 'field cd=190000/') $c
    Check 'skill cooldown is none' ((Step 'RateCooldown' 4) -eq '없음')
    $c = Cmd 'cdall' 200
    Check 'immortal and field cd are at the floor' ($c -match 'imm0 id=5000 cd=100/' -and $c -match 'field cd=100/') $c
    $first = $false
    for ($i = 0; $i -lt 10 -and -not $first; $i++) { $first = (Cmd 'icast 0' 500) -match 'IsCreate=True' }
    Check 'the immortal skill is cast' $first
    $again = $false
    for ($i = 0; $i -lt 12 -and -not $again; $i++) { Start-Sleep -Milliseconds 500; $again = (Cmd 'icast 0' 100) -match 'IsCreate=True' }
    Check 'the immortal skill is cast again within seconds' $again
    $f1 = $false
    for ($i = 0; $i -lt 20 -and -not $f1; $i++) { $f1 = (Cmd 'fcast' 300) -match 'IsCreate=True'; if (-not $f1) { Start-Sleep -Seconds 2 } }
    Check 'the field skill is cast after its charge' $f1
    Start-Sleep -Seconds 1
    Check 'the field skill is cast again' ((Cmd 'fcast' 300) -match 'IsCreate=True') (Cmd 'cdall' 200)
    Check 'skill cooldown wraps to x1' ((Step 'RateCooldown' 1) -eq 'x1')
    $c = Cmd 'cdall' 200
    Check 'immortal and field cd are back' ($c -match 'imm0 id=5000 cd=60000/' -and $c -match 'field cd=190000/') $c
    Check 'skill cooldown is x0.5' ((Step 'RateCooldown' 1) -eq 'x0.5')
    $c = Cmd 'cdall' 200
    Check 'x0.5 halves the original, not the old value' ($c -match 'imm0 id=5000 cd=30000/' -and $c -match 'field cd=95000/') $c
    OpenEditorTab 12; Press 'RateReset' | Out-Null; Press 'Close' | Out-Null; Start-Sleep -Milliseconds 500
    $c = Cmd 'cdall' 200
    Check 'reset puts the cd back' ($c -match 'imm0 id=5000 cd=60000/' -and $c -match 'field cd=190000/' -and $c -match 'rates skill=100/100') $c
    LeaveBattleScene
    Check 'back on the map' ((Cmd 'uilist' 100) -match 'MapMain')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'onehit' {
    $traps = Traps
    OpenEditorTab 12; Press 'God' | Out-Null; 1..5 | ForEach-Object { Press 'Multiplier' | Out-Null }
    Check 'damage is one hit' ((UiText 'Multiplier') -eq '일격') (UiText 'Multiplier')
    Press 'Close' | Out-Null
    EnterBattleScene 60
    for ($i = 0; $i -lt 60 -and (Cmd 'bdmg' 100) -notmatch 'last=onehit'; $i++) { Cmd 'bcast 1' 350 | Out-Null }
    $d = Cmd 'bdmg' 100
    Check 'a hit gets the one-hit value' ($d -match 'onehit base->(\d+) ' -and [long]$Matches[1] -ge 10000) $d
    Start-Sleep -Milliseconds 1500
    $h = Cmd 'bhp' 200
    Check 'the enemy that was hit is down' ($h -match 'target=hp=0 die=True') $h
    LeaveBattleScene
    # 무적 없이 수준 20(한 방이 수십): 플레이어가 맞아도 일격 값을 받지 않는다.
    OpenEditorTab 12; Press 'God' | Out-Null; Press 'Close' | Out-Null
    EnterBattleScene 20
    for ($i = 0; $i -lt 40 -and (Cmd 'bdmg' 100) -notmatch 'onPlayer=[1-9]'; $i++) { Start-Sleep -Milliseconds 500 }
    $d = Cmd 'bdmg' 100; $h = Cmd 'bhp' 200
    Check 'the player was hit' ($d -match 'onPlayer=[1-9]') $d
    Check 'the player is alive after being hit' ((NumIn $h 'playerHp=(-?\d+)') -gt 0) $h
    LeaveBattleScene
    OpenEditorTab 12; Press 'RateReset' | Out-Null; Press 'Close' | Out-Null
    Check 'back on the map' ((Cmd 'uilist' 100) -match 'MapMain')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'durable' {
    $traps = Traps
    Cmd 'trgive 3031101' 800 | Out-Null
    $sole = if ((Cmd 'tr' 200) -match '\[(\w+):3031101') { $Matches[1] } else { '' }
    Check 'an artifact is given and equipped' ($sole -ne '' -and (Cmd "trequip $sole" 800) -match '장착') $sole
    function Dur { NumIn (Cmd 'artdur' 200) "$([regex]::Escape($sole)):dur=(\d+)" }
    function UseArt { $c = Cmd 'cdall' 200; if ($c -match 'prop(\d) PropItemArtifact') { Cmd "tbbuse $($Matches[1])" 1500 } else { "no artifact slot | $c" } }
    Check 'durable cost is none' ((Step 'RateArtifactDurable' 1) -eq '없음')
    EnterBattleScene 5
    $u = UseArt
    Check 'the artifact is used in battle' ($u -match 'IsCreate=True') $u
    Check 'none keeps the durability after the first use' ((Dur) -eq 100) (Cmd 'artdur' 200)
    Check 'the guard blocked one write' ((Cmd 'artdur' 200) -match 'blocked=1') (Cmd 'artdur' 200)
    LeaveBattleScene
    Check 'the durability is still full after the battle' ((Dur) -eq 100) (Cmd 'artdur' 200)
    Cmd "trdur $sole 50" 300 | Out-Null
    Check 'none also keeps a direct lower write' ((Dur) -eq 100) (Cmd 'artdur' 200)
    Check 'durable cost wraps to x1' ((Step 'RateArtifactDurable' 1) -eq 'x1')
    EnterBattleScene 5
    UseArt | Out-Null
    Check 'x1 costs two on the first use' ((Dur) -eq 98) (Cmd 'artdur' 200)
    LeaveBattleScene
    Cmd "trdur $sole 50" 300 | Out-Null
    Check 'x1 lets a lower write through' ((Dur) -eq 50) (Cmd 'artdur' 200)
    Step 'RateArtifactDurable' 1 | Out-Null
    $r = Cmd "trrepair $sole" 500
    Check 'repair still works under none' ((Dur) -eq 100) "$r | $(Cmd 'artdur' 200)"
    OpenEditorTab 12; Press 'RateReset' | Out-Null; Press 'Close' | Out-Null
    Check 'back on the map' ((Cmd 'uilist' 100) -match 'MapMain')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
