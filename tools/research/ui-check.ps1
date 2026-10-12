# 편집기 화면의 게임 내 검증(docs/superpowers/specs/2026-10-13-ui-rework-design.md 6절).
# tools\DevRemote\session.ps1 -Begin으로 게임이 떠서 세계에 들어가 있어야 한다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

# 탭마다 그 탭에만 있는 버튼 하나. 탭이 제대로 그려졌는지 보는 데 쓴다.
$tabProbe = @('Restore', 'MoneyAdd', 'FillExp', 'SkillAddOpen', 'LuckAddOpen', 'ArtLearnAll', 'NpcRefresh', 'Freeze', 'BackupNow')

switch ($Part) {
  'base' {
    $traps = Traps
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    for ($i = 0; $i -lt 9; $i++) {
      Press "Tab$i" | Out-Null
      Check "tab $i shows $($tabProbe[$i])" (IsAt $tabProbe[$i])
    }
    Press 'Tab0' | Out-Null
    Check 'uitext reads a label' ((Cmd 'uitext Restore') -match "text='자원 회복'")
    Check 'a name prefix finds an element' ((Cmd 'uipos Plus*') -match '-> Plus\d+ at ')
    Check 'unknown name is reported' ((Cmd 'uipos NoSuchThing') -match 'no element named')
    Press 'Close' | Out-Null
    Check 'editor closed' ((Cmd 'uipos Tab0') -match 'no element named')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'tabs' {
    $traps = Traps
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    $s = Cmd 'uistate'
    Check 'state is readable' ($s -match 'open=True tab=\d target=player ') $s
    Check 'title shows the target' ((Cmd 'uitext Title') -match '편집 대상: .+\(플레이어\)')
    $p = Pos 'ToPlayer'
    Check 'ToPlayer is off while the target is the player' ((Rc "click $($p.x) $($p.y)" 300) -match 'not interactable')
    for ($i = 0; $i -lt 9; $i++) {
      Press "Tab$i" | Out-Null
      Check "tab $i is selected" ((Cmd 'uistate') -match "tab=$i ")
      Check "tab $i shows $($tabProbe[$i])" (IsAt $tabProbe[$i])
    }

    # 탭 안의 하위 화면
    Press 'Tab3' | Out-Null; Press 'SkillAddOpen' | Out-Null
    Check 'skill add screen opens' (IsAt 'SkillBack')
    Press 'SkillBack' | Out-Null
    Check 'skill add screen closes' (IsAt 'SkillAddOpen')
    Press 'Tab4' | Out-Null; Press 'LuckAddOpen' | Out-Null
    Check 'luck picker opens' (IsAt 'LuckBack')
    Press 'LuckBack' | Out-Null; Press 'FateOpen' | Out-Null
    Check 'fate screen opens' (IsAt 'FateBack')
    Press 'FateBack' | Out-Null
    Check 'luck list is back' (IsAt 'LuckAddOpen')

    # 수량은 쓰는 곳마다 따로 기억한다 (스펙 6절 10번)
    Press 'Tab0' | Out-Null; Press 'attr100' | Out-Null
    Check 'attribute step is 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'Tab7' | Out-Null
    Check 'move step is still 10' ((Cmd 'uitext MoveUp') -match "text='y - 10'")
    Press 'move1000' | Out-Null
    Check 'move step is 1000' ((Cmd 'uitext MoveUp') -match "text='y - 1000'")
    Press 'Tab6' | Out-Null; Press 'Pick0' | Out-Null
    Check 'intim step is still 10' ((Cmd 'uitext IntimPlus') -match "text='\+ 10'")
    Press 'Tab0' | Out-Null
    Check 'attribute step is still 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'attr10' | Out-Null; Press 'Tab7' | Out-Null; Press 'move10' | Out-Null

    # 결과 영역 (스펙 6절 20번은 캡처로 본다)
    Press 'Tab0' | Out-Null; Press 'Restore' | Out-Null
    Check 'result line shows the outcome' ((Cmd 'uistate') -match "message='자원 회복: ")
    Press 'Tab1' | Out-Null
    Check 'tab change clears the result line' ((Cmd 'uistate') -match "message=''")
    Check 'a long result is accepted' ((Cmd 'uisay 30') -match 'said 360 chars')
    Shot 'long-message'
    Press 'Tab1' | Out-Null

    # 편집 대상: NPC로 바꿨다가 제목 줄의 버튼으로 되돌린다 (스펙 6절 15번)
    Press 'Tab6' | Out-Null; Press 'EditNpc' | Out-Null
    $s = Cmd 'uistate'
    Check 'target is the NPC and the first tab is shown' ($s -match 'tab=0 target=npc ') $s
    Check 'title shows the NPC' ((Cmd 'uitext Title') -match '\(NPC\)')
    Press 'ToPlayer' | Out-Null
    Check 'back to the player' ((Cmd 'uistate') -match 'target=player ' -and (Cmd 'uitext Title') -match '\(플레이어\)')

    # Review Focus 2: 대상으로 잡은 NPC가 없어지면 플레이어로 돌아간다
    $s = Cmd 'uitarget no-such-unit'
    Check 'a missing target falls back to the player' ($s -match 'target=player ' -and (Cmd 'uitext Title') -match '\(플레이어\)') $s
    Press 'Tab3' | Out-Null
    Check 'target tabs still draw after the fallback' (IsAt 'SkillAddOpen')

    Shot 'tabs'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
