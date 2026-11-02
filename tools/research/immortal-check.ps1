# 선법 탭의 게임 내 검증(docs/superpowers/specs/2026-10-14-immortal-tab-design.md 6절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

# 설정 표(게임 폴더의 읽기 전용 JSON)에서 센 카드 수: 선인마다 주동(0이 아니면)과 피동, 같은 번호는 한 번, 표에 있는 것만.
function ExpectedCount {
  $dir = 'E:\SteamLibrary\steamapps\common\鬼谷八荒\Mod\modFQA\配置修改教程\配置（只读）Json格式'
  $active = @{}; foreach ($r in (Get-Content -LiteralPath (Join-Path $dir 'ImmortalMethod.json') -Raw | ConvertFrom-Json)) { $active[[string]$r.id] = 1 }
  $passive = @{}; foreach ($r in (Get-Content -LiteralPath (Join-Path $dir 'PassiveMethod.json') -Raw | ConvertFrom-Json)) { $passive[[string]$r.passiveId] = 1 }
  $seen = @{}
  foreach ($h in (Get-Content -LiteralPath (Join-Path $dir 'ImmortalHuman.json') -Raw | ConvertFrom-Json)) {
    if ([string]$h.activeSkills -ne '0' -and $active.ContainsKey([string]$h.activeSkills)) { $seen["a$($h.activeSkills)"] = 1 }
    foreach ($p in ([string]$h.passiveSkills -split '\|')) { if ($p -ne '0' -and $passive.ContainsKey($p)) { $seen["p$p"] = 1 } }
  }
  $seen.Count
}

switch ($Part) {
  'catalog' {
    $all = FieldsOf (Cmd 'icount 0 0' 300)
    Check 'the list has every card of the immortal table' ([int]$all.count -eq (ExpectedCount)) "count=$($all.count) want=$(ExpectedCount)"
    Check 'one immortal (1004) has six cards' ((FieldsOf (Cmd 'icount 1004 0' 300)).count -eq '6') (Cmd 'icount 1004 0' 300)
    Check 'active only' ([int](FieldsOf (Cmd 'icount 0 1' 300)).count -ge 7 -and [int](FieldsOf (Cmd 'icount 0 1' 300)).count -le 10) (Cmd 'icount 0 1' 300)
    Check 'a name search finds the card' ((FieldsOf (Cmd 'icount 0 0 천무절향' 300)).first -eq '5000') (Cmd 'icount 0 0 천무절향' 300)
    Check 'every entry has a translated name' ((FieldsOf (Cmd 'inoname' 300)).count -eq '0') (Cmd 'inoname' 300)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'learn' {
    Check 'the fixture has no immortal card' ((FieldsOf (Cmd 'ilist' 200)).owned -eq '0') (Cmd 'ilist' 200)
    $r = Cmd 'ilearn 5000' 800
    Check 'one card is learned' ((Cmd 'ihas 5000' 200) -match 'has=True' -and (FieldsOf (Cmd 'ilist' 200)).owned -eq '1') $r
    $r = Cmd 'ilearn 5000' 800
    Check 'learning it again adds nothing' ((FieldsOf (Cmd 'ilist' 200)).owned -eq '1') $r
    $r = Cmd 'ilearn 10010' 800
    Check 'a passive card is learned' ((Cmd 'ihas 10010' 200) -match 'has=True') $r
    Check 'the change log has the line' (@(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 5 -Encoding UTF8 | Where-Object { $_ -match '선법' }).Count -gt 0)
    Cmd 'save' 2500 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'restart' {
    # learn(끝에서 저장한다) 뒤에 session.ps1 -Restart를 하고 돌린다.
    Check 'the learned cards are kept after a restart' ((Cmd 'ihas 5000' 200) -match 'has=True' -and (Cmd 'ihas 10010' 200) -match 'has=True') (Cmd 'ilist' 200)
  }
  'learn-all' {
    $before = [int](FieldsOf (Cmd 'ilist' 200)).owned
    $r = Cmd 'ilearnall 1004 0' 2000
    # 앞 부분이 익혀 둔 카드는 건너뛴다. 익힘과 건너뜀의 합이 그 선인의 카드 수(6)이고, 익힌 만큼 늘어야 한다.
    $done = if ($r -match '익힘 (\d+), 건너뜀 (\d+)') { [int]$Matches[1] } else { -1 }
    $skip = if ($r -match '익힘 (\d+), 건너뜀 (\d+)') { [int]$Matches[2] } else { -1 }
    Check 'every card of the filtered list is learned' ($done + $skip -eq 6 -and [int](FieldsOf (Cmd 'ilist' 200)).owned -eq $before + $done) $r
    Check 'cards of other immortals are not learned' ((Cmd 'ihas 6000' 200) -match 'has=False')
    $r = Cmd 'ilearnall 1004 0' 2000
    Check 'doing it again skips all' ($r -match '익힘 0, 건너뜀 6' -and [int](FieldsOf (Cmd 'ilist' 200)).owned -eq $before + $done) $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'ui' {
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab6' | Out-Null
    Check 'the immortal tab is the seventh' (IsAt 'ImmortalAddOpen')
    Check 'the immortal power is shown as used/total' ((Cmd 'uitext ImmortalPointText' 150) -match "ImmortalPointText text='[^']*0 / 0")
    Press 'ImmortalPointPlus' | Out-Null
    Check 'plus one raises the total' ((FieldsOf (Cmd 'ilist' 200)).total -eq '1') (Cmd 'ilist' 200)
    Press 'ImmortalPointMax' | Out-Null
    Check 'max sets the game limit' ((FieldsOf (Cmd 'ilist' 200)).total -eq '40') (Cmd 'ilist' 200)
    Press 'ImmortalPointMinus' | Out-Null
    Check 'minus one lowers it' ((FieldsOf (Cmd 'ilist' 200)).total -eq '39') (Cmd 'ilist' 200)
    # 상한(40)을 넘은 세이브(예전 판의 경지 올리기로 45가 될 수 있었다)에서도 최대로 40에 맞출 수 있어야 한다.
    Cmd 'immset 45' 300 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab5' | Out-Null; Press 'Tab6' | Out-Null
    try { Press 'ImmortalPointMax' | Out-Null } catch { }
    Check 'max brings a total over the limit back to the limit' ((FieldsOf (Cmd 'ilist' 200)).total -eq '40') (Cmd 'ilist' 200)
    Cmd 'immset 39' 300 | Out-Null
    Press 'Tab5' | Out-Null; Press 'Tab6' | Out-Null
    Press 'ImmortalAddOpen' | Out-Null
    $fit = FieldsOf (Cmd 'ifit ImmortalDesc' 300)
    Check 'every description in the add list is whole' ([int]$fit.texts -ge 5 -and $fit.overflow -eq '0') (Cmd 'ifit ImmortalDesc' 300)
    Check 'the add page has the filters' ((IsAt 'ImmortalHuman0') -and (IsAt 'ImmortalHuman1004') -and (IsAt 'ImmortalKind2') -and (IsAt 'ImmortalLearnAll'))
    Press 'ImmortalHuman1004' | Out-Null
    Check 'filtering by an immortal shows its cards' ((IsAt 'ImmortalLearn5000') -and -not (IsAt 'ImmortalLearn6000'))
    Press 'ImmortalLearn5000' 1500 | Out-Null
    Check 'learn marks the card as owned' ((Cmd 'ihas 5000' 200) -match 'has=True' -and (Cmd 'uitext ImmortalState5000' 150) -match '보유')
    Press 'ImmortalLearnAll' | Out-Null
    Check 'learn all asks first' ((FieldsOf (Cmd 'ilist' 200)).owned -eq '1')
    Press 'ImmortalLearnAll' 2500 | Out-Null
    Check 'the second press learns the shown cards' ((FieldsOf (Cmd 'ilist' 200)).owned -eq '6') (Cmd 'ilist' 200)
    Press 'Back' | Out-Null
    Check 'the owned list shows the cards' (IsAt 'ImmortalOwnedList')
    Shot 'immortal-tab'
    # NPC: 선법 자료가 없거나 비어 있어도 예외 없이 그려진다.
    Press 'Tab10' | Out-Null; Press 'NpcRefresh' | Out-Null; Press (Pos 'Pick*').name | Out-Null; Press 'EditNpc' | Out-Null
    Press 'Tab6' | Out-Null
    Check 'an NPC target draws without an exception' ((IsAt 'ImmortalAddOpen') -or (IsAt 'ImmortalNoData'))
    Press 'ToPlayer' | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'game' {
    # 등선 후기로 올린 뒤 익힌 카드가 게임의 선법 창에 나오는지(캡처로 본다).
    Cmd 'pgraise 44' 4000 | Out-Null
    for ($n = 0; $n -lt 5 -and (Cmd 'uilist' 200) -match 'DramaDialogue|GetImmortalSkill'; $n++) { Cmd 'uiclose DramaDialogue' 500 | Out-Null; Cmd 'uiclose GetImmortalSkill' 500 | Out-Null }
    $i = Cmd 'ilist' 200
    Check 'raising the grade keeps the immortal power within the game limit' ([int](FieldsOf $i).total -le 40) $i
    Cmd 'ilearnall 1004 0' 2000 | Out-Null
    Cmd 'uiopen ImmortalSkill' 2000 | Out-Null
    Shot 'immortal-game'
    Check 'the game window opened' ((Cmd 'uilist' 200) -match 'ImmortalSkill')
    Cmd 'uiclose ImmortalSkill' 800 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
