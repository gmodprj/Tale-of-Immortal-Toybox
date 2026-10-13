# 편집기 화면의 게임 내 검증(docs/superpowers/specs/2026-10-13-ui-rework-design.md 6절).
# tools\DevRemote\session.ps1 -Begin으로 게임이 떠서 세계에 들어가 있어야 한다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

# 탭마다 그 탭에만 있는 버튼 하나. 탭이 제대로 그려졌는지 보는 데 쓴다.
$tabProbe = @('Restore', 'MoneyAdd', 'CurrencyAdd10001', 'VaultModeBag', 'FillExp', 'SkillAddOpen', 'LuckAddOpen', 'ArtLearnAll', 'NpcRefresh', 'Freeze', 'God', 'BackupNow')
# 게임의 인물 화면을 열거나 닫고, UI 층의 맨 위 정렬 값이 그에 맞게 바뀔 때까지 기다린다(닫히는 연출의 길이가 그때그때 다르다).
function GameScreen([string]$do) {
  $want = if ($do -eq 'open') { 20 } else { 0 }
  Cmd "ui$do PlayerInfo" 300 | Out-Null
  for ($i = 0; $i -lt 25 -and (Cmd 'uistate' 100) -notmatch " top=$want "; $i++) { Start-Sleep -Milliseconds 200 }
  Start-Sleep -Milliseconds 300   # 편집기의 주기 처리가 정렬을 맞출 틈
}
# 전투에 들어간다. fight 명령은 "전투 진입" 확인 화면까지만 열므로 그 버튼을 누르고 장면이 바뀔 때까지 기다린다.
# 전투에서 나오는 방법은 자동화하지 않았다. 이 함수를 쓰는 부분은 회차의 마지막에 돌리고 바로 -End 한다.
function EnterBattle {
  Cmd 'fight 1011 1' 6000 | Out-Null
  $o = Rc 'click 960 684' 12000
  "INFO battle entry: $o"
  "INFO open game screens: $((Cmd 'uilist' 100) -replace "`n", ' |')"
}
# 창 위치를 바꾸는 시험(move 계열)은 세이브 폴더 밖의 config.txt를 고친다. 시험 전의 파일을 한 번만 떠 두고,
# 끝났을 때와 중간에 멈췄을 때 모두 되돌린다. 이미 떠 둔 것이 있으면(앞선 실행이 멈춘 것) 덮어쓰지 않는다.
function KeepConfig {
  $copy = Join-Path $work 'check-config.txt'
  if (-not (Test-Path -LiteralPath $copy)) { Copy-Item -LiteralPath (Join-Path $data 'config.txt') -Destination $copy }
}
function RestoreConfig {
  $copy = Join-Path $work 'check-config.txt'
  if (Test-Path -LiteralPath $copy) {
    Copy-Item -LiteralPath $copy -Destination (Join-Path $data 'config.txt') -Force
    [IO.File]::Delete($copy)
    'config.txt restored'
  }
}
trap { if (Get-Command RestoreConfig -ErrorAction SilentlyContinue) { RestoreConfig }; break }
function WinState {
  $o = Cmd 'uistate' 100
  if ($o -notmatch ' pos=(-?\d+),(-?\d+) collapsed=(\w+) ') { throw "uistate 출력을 읽지 못했습니다: $o" }
  [pscustomobject]@{ x = [int]$Matches[1]; y = [int]$Matches[2]; collapsed = ($Matches[3] -eq 'True'); raw = $o }
}
# 제목 줄의 보이는 빈 곳을 잡아 창의 왼쪽 위가 (x,y)에 오도록 끈다.
# 잡는 곳은 닫기 버튼 오른쪽의 틈(창 안 x=1492)이고, 그곳이 화면 밖이면 제목 글자 자리(x=150)다. 글자는 클릭을 받지 않아 바탕이 잡힌다.
function DragWindowTo([int]$x, [int]$y) {
  $w = WinState
  $gx = $w.x + 1492
  if ($gx -gt 1917) { $gx = $w.x + 150 }
  $gy = $w.y + 26
  Cmd "uidrag $gx $gy $($gx + $x - $w.x) $($gy + $y - $w.y) 5" 300
}

switch ($Part) {
  'base' {
    $traps = Traps
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    for ($i = 0; $i -lt 12; $i++) {
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
    Check 'state is readable' ($s -match 'open=True tab=\d+ target=player ') $s
    Check 'title shows the target' ((Cmd 'uitext Title') -match '편집 대상: .+\(플레이어\)')
    $p = Pos 'ToPlayer'
    Check 'ToPlayer is off while the target is the player' ((Rc "click $($p.x) $($p.y)" 300) -match 'not interactable')
    for ($i = 0; $i -lt 12; $i++) {
      Press "Tab$i" | Out-Null
      Check "tab $i is selected" ((Cmd 'uistate') -match "tab=$i ")
      Check "tab $i shows $($tabProbe[$i])" (IsAt $tabProbe[$i])
    }
    Press 'Tab9' | Out-Null
    Check 'world tab no longer has the battle helpers' (-not (IsAt 'God'))

    # 탭 안의 하위 화면
    Press 'Tab5' | Out-Null; Press 'SkillAddOpen' | Out-Null
    Check 'skill add screen opens' (IsAt 'Back')
    Press 'Back' | Out-Null
    Check 'skill add screen closes' (IsAt 'SkillAddOpen')
    Press 'Tab6' | Out-Null; Press 'LuckAddOpen' | Out-Null
    Check 'luck picker opens' (IsAt 'Back')
    Press 'Back' | Out-Null; Press 'FateOpen' | Out-Null
    Check 'fate screen opens' (IsAt 'Back')
    Press 'Back' | Out-Null
    Check 'luck list is back' (IsAt 'LuckAddOpen')

    # 수량은 쓰는 곳마다 따로 기억한다 (스펙 6절 10번)
    Press 'Tab0' | Out-Null; Press 'attr100' | Out-Null
    Check 'attribute step is 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'Tab9' | Out-Null
    Check 'move step is still 10' ((Cmd 'uitext MoveUp') -match "text='y - 10'")
    Press 'move1000' | Out-Null
    Check 'move step is 1000' ((Cmd 'uitext MoveUp') -match "text='y - 1000'")
    Press 'Tab8' | Out-Null; Press 'NpcRefresh' | Out-Null; Press (Pos 'Pick*').name | Out-Null
    Check 'intim step is still 10' ((Cmd 'uitext IntimPlus') -match "text='\+ 10'")
    Press 'Tab0' | Out-Null
    Check 'attribute step is still 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'attr10' | Out-Null; Press 'Tab9' | Out-Null; Press 'move10' | Out-Null

    # 결과 영역 (스펙 6절 20번은 캡처로 본다)
    Press 'Tab0' | Out-Null; Press 'Restore' | Out-Null
    Check 'result line shows the outcome' ((Cmd 'uistate') -match "message='자원 회복: ")
    Press 'Tab1' | Out-Null
    Check 'tab change clears the result line' ((Cmd 'uistate') -match "message=''")
    Check 'a long result is accepted' ((Cmd 'uisay 30') -match 'said 360 chars')
    Shot 'long-message'
    Press 'Tab1' | Out-Null

    # 편집 대상: NPC로 바꿨다가 제목 줄의 버튼으로 되돌린다 (스펙 6절 15번)
    Press 'Tab8' | Out-Null; Press 'EditNpc' | Out-Null
    $s = Cmd 'uistate'
    Check 'target is the NPC and the first tab is shown' ($s -match 'tab=0 target=npc ') $s
    Check 'title shows the NPC' ((Cmd 'uitext Title') -match '\(NPC\)')
    Press 'ToPlayer' | Out-Null
    Check 'back to the player' ((Cmd 'uistate') -match 'target=player ' -and (Cmd 'uitext Title') -match '\(플레이어\)')

    # 삭제 확인 대기는 편집 대상을 바꾸면 풀린다. 제목 줄의 버튼으로 어느 탭에서나 대상을 바꿀 수 있기 때문이다.
    Press 'Tab8' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab6' | Out-Null
    $rm = (Pos 'LuckRemove*').name; Press $rm | Out-Null
    Check 'removing an NPC luck asks first' ((Cmd "uitext $rm") -match "text='정말 삭제'")
    Press 'ToPlayer' | Out-Null
    Press 'Tab8' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab6' | Out-Null
    Check 'the pending confirm is gone after the target changed' ((Cmd "uitext $rm") -match "text='삭제'")
    Press 'ToPlayer' | Out-Null

    # Review Focus 2: 대상으로 잡은 NPC가 없어지면 플레이어로 돌아간다
    $s = Cmd 'uitarget no-such-unit'
    Check 'a missing target falls back to the player' ($s -match 'target=player ' -and (Cmd 'uitext Title') -match '\(플레이어\)') $s
    Press 'Tab5' | Out-Null
    Check 'target tabs still draw after the fallback' (IsAt 'SkillAddOpen')

    Shot 'tabs'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'items' {
    # 스펙 6절 7, 8, 17번. 자원 탭의 목록·검색·상세, 가방 칸 한 번에 지정, 의복의 구분 표시.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    if ($o -notmatch '전체 (\d+)개 중 1~(\d+)번째') { throw "아이템 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]; $screen = [int]$Matches[2]
    Check 'the list holds every item' ($total -gt 3000) $o
    Check 'only the visible rows are built' ((Cmd "uipos Row$($screen + 5)") -match 'no element named')
    Check 'paging buttons are gone' ((Cmd 'uipos ItemPageSize10') -match 'no element named' -and (Cmd 'uipos ItemSearch') -match 'no element named')

    # 즉시 검색: 실제 키로 번호를 친다(이름과 번호 둘 다 찾는다)
    Cmd 'uifocus ItemQuery' | Out-Null
    Key 0x31 350; Key 0x30 350; Key 0x31 350
    $typed = Cmd 'uitext ItemQuery'
    $o = Cmd 'uitext ItemTableRange'
    Check 'the keys reached the search box' ($typed -match "text='101'") $typed
    Check 'typing filters at once' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -lt $total -and [int]$Matches[1] -gt 0) $o
    Check 'the search box is still in place' (IsAt 'ItemQuery')

    # Review Focus 1: 결과가 없을 때
    Cmd 'ui set ItemQuery zzzzzz' | Out-Null
    Check 'no match shows an empty list' ((Cmd 'uitext ItemTableRange') -match "text='0개'" -and (Cmd 'uipos ItemTableNext') -match 'no element named' -and (Cmd 'uipos Row0') -match 'no element named')
    Press 'ItemQueryClear' | Out-Null
    $o = (Cmd 'uitext ItemTableRange') + ' | ' + (Cmd 'uitext ItemQuery')
    Check 'clearing brings everything back' ($o -match "전체 $($total)개 중 1~" -and $o -match "ItemQuery text=''") $o

    # 이동 줄과 스크롤
    # 휠로 조금만 내려도(첫 줄 번호가 그대로여도) 이동 버튼이 켜진다
    $p = Pos 'ItemTable'
    Rc "scroll $($p.x) $($p.y) -0.5" 300 | Out-Null
    $f = Pos 'ItemTableFirst'
    Check 'a small scroll enables the first button' ((Rc "click $($f.x) $($f.y)" 400) -match 'Button.onClick: ItemTableFirst')
    Press 'ItemTableNext' | Out-Null
    Check 'next moves one screen' ((Cmd 'uitext ItemTableRange') -match "중 $($screen + 1)~")
    Press 'ItemTableLast' | Out-Null
    Check 'last reaches the end' ((Cmd 'uitext ItemTableRange') -match "~$($total)번째")
    $p = Pos 'ItemTable'
    Rc "scroll $($p.x) $($p.y) 3" 300 | Out-Null
    Check 'the wheel scrolls back up' ((Cmd 'uitext ItemTableRange') -notmatch "~$($total)번째")
    Press 'ItemTableLast' | Out-Null

    # Review Focus 2: 끝까지 내린 채 검색해 목록이 짧아진다
    Cmd 'ui set ItemQuery 101' | Out-Null
    Check 'a shorter result starts from the top' ((Cmd 'uitext ItemTableRange') -match '중 1~' -and (Cmd 'uipos Row0') -match ' at ')
    Press 'ItemQueryClear' | Out-Null

    # 지급 뒤에도 목록 위치가 그대로이고 보유 수가 바뀐다
    Press 'ItemTableNext' | Out-Null
    $before = Cmd 'uitext ItemTableRange'
    $give = Pos 'ItemGive*'
    $id = $give.name -replace 'ItemGive', ''
    $had = if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
    Press $give.name | Out-Null
    $has = if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
    Check 'giving an item works from the list' ($has -eq $had + 1) "id=$id $had -> $has"
    Check 'the list stays where it was' ((Cmd 'uitext ItemTableRange') -eq $before)
    Press "ItemName$id" | Out-Null
    Check 'pressing a name shows the whole text below' ((Cmd 'uitext ItemDetail') -match "\[$id\]")

    # 의복: 이름만으로 구분된다
    Cmd 'ui set ItemQuery 3041003' | Out-Null
    $o = Cmd 'uitext ItemName3041003'
    Check 'clothes show sex, shape and colour' ($o -match "text='의복\((남|여)\) \d+-\d+'") $o
    Press 'ItemQueryClear' | Out-Null

    # 화폐: 영석 말고도 화폐로 쓰이는 재화(사용자 지시로 추가)
    Press 'Tab2' | Out-Null
    Check 'the currency tab lists the other currencies' ((IsAt 'CurrencyAdd10011') -and (IsAt 'CurrencyAdd10041') -and (IsAt 'CurrencyAdd7000315') -and (IsAt 'CurrencyAdd2821101'))
    Check 'run-only currencies are left out' ((Cmd 'uipos CurrencyAdd5061016') -match 'no element named')
    $grid = if ((Cmd 'grid') -match 'cur=(\d+)') { [int]$Matches[1] } else { -1 }
    foreach ($cid in 10041, 10011) {
      $had = if ((Cmd "count $cid") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
      Press "CurrencyAdd$cid" | Out-Null
      $has = if ((Cmd "count $cid") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
      Check "currency $cid goes up by the chosen amount" ($has -eq $had + 10) "$had -> $has"
    }
    Check 'the result names the currency, not a text key' ((Cmd 'uistate') -notmatch "message='item_name")
    Check 'currencies do not take bag slots' ((Cmd 'grid') -match "cur=$grid ")
    $money = if ((Cmd 'read') -match 'money=(\d+)') { [int]$Matches[1] } else { -1 }
    Press 'CurrencyAdd10001' | Out-Null
    Check 'spirit stones can be added here too' ((Cmd 'read') -match "money=$($money + 10) ")
    Check 'the resources tab no longer has a currency screen' ((Cmd 'uipos CurrencyOpen') -match 'no element named')
    Press 'Tab1' | Out-Null

    # 가방 칸 한 번에 지정 (회차의 -End가 bag-slots.txt를 되돌린다)
    function Extra { if ((Cmd 'bagstat') -match 'extra=(\d+)') { [int]$Matches[1] } else { -1 } }
    $e0 = Extra
    $want = if ($e0 -eq 120) { 135 } else { 125 }
    Cmd 'uifocus BagExtra' | Out-Null; Cmd "ui set BagExtra $want" | Out-Null; Key 0x0D
    Check 'Enter sets the extra slots, rounded down to tens' ((Extra) -eq $want - 5) "extra=$(Extra)"
    # Review Focus 3: 범위 밖의 수는 맞춰서 적용하지 않고 거부한다
    $kept = Extra
    Cmd 'ui set BagExtra 501' | Out-Null; Press 'BagApply' | Out-Null
    Check 'a value above the limit is refused' ((Extra) -eq $kept -and (Cmd 'uistate') -match "message='가방: 추가 칸은 0~500") "extra=$(Extra)"
    Cmd 'ui set BagExtra -10' | Out-Null; Press 'BagApply' | Out-Null
    Check 'a negative value is refused' ((Extra) -eq $kept -and (Cmd 'uistate') -match "message='가방: 추가 칸은 0~500") "extra=$(Extra)"
    Cmd 'ui set BagExtra 500' | Out-Null; Press 'BagApply' | Out-Null
    Check 'the apply button sets the upper limit' ((Extra) -eq 500)
    Cmd 'ui set BagExtra 0' | Out-Null; Press 'BagApply' | Out-Null
    Check 'zero removes them' ((Extra) -eq 0)
    Press 'BagPlus' | Out-Null
    Check 'the +10 button still works' ((Extra) -eq 10)
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'numbers' {
    # 스펙 6절 9번, Review Focus 3. 능력치의 저장값 칸과 수량 줄의 직접 입력.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null
    $cell = (Pos 'AttrValue*').name
    function Val { if ((Cmd "uitext $cell") -match "text='(-?\d+)'") { [int]$Matches[1] } else { throw "값을 읽지 못했습니다: $cell" } }
    $v = Val
    Check 'the direct-input row is gone' ((Cmd 'uipos AttributeApply') -match 'no element named')

    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $($v + 1)" | Out-Null
    Press 'attr10' | Out-Null
    Check 'without Enter nothing is applied' ((Val) -eq $v)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $($v + 1)" | Out-Null; Key 0x0D
    Check 'Enter applies the typed value' ((Val) -eq $v + 1) "value=$(Val)"
    Check 'the change is reported' ((Cmd 'uistate') -match "message='[^']*->")
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell 999999999" | Out-Null; Key 0x0D
    Check 'an out-of-range value is refused' ((Val) -eq $v + 1)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell" | Out-Null; Key 0x0D
    Check 'an empty value is refused with a reason' ((Val) -eq $v + 1 -and (Cmd 'uistate') -match "message='[^']*숫자")
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell -5" | Out-Null; Key 0x0D
    Check 'a negative value is refused' ((Val) -eq $v + 1)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $v" | Out-Null; Key 0x0D
    Check 'back to the original value' ((Val) -eq $v)

    # 수량 줄의 직접 입력도 Enter로 적용된다
    Cmd 'uifocus attrInput' | Out-Null; Cmd 'ui set attrInput 7' | Out-Null; Key 0x0D
    Check 'Enter applies a typed step' ((Cmd 'uitext Plus*') -match "text='\+ 7'")
    Cmd 'uifocus attrInput' | Out-Null; Cmd 'ui set attrInput 0' | Out-Null; Key 0x0D
    Check 'a step of zero is refused' ((Cmd 'uitext Plus*') -match "text='\+ 7'" -and (Cmd 'uistate') -match "message='수량은")
    Press 'attr10' | Out-Null
    # 세계 탭: 날짜 칸에서 Enter를 누르면 적용 버튼과 같다(확인은 그대로 두 번)
    Press 'Tab9' | Out-Null
    if ((Cmd 'tin') -notmatch 'input=(\d+)-(\d+)-(\d+) pending=False') { throw '날짜 입력 상태를 읽지 못했습니다.' }
    $day = [int]$Matches[3]; $other = if ($day -eq 12) { 13 } else { 12 }
    Cmd 'uifocus DateDay' | Out-Null; Cmd "ui set DateDay $other" | Out-Null; Key 0x0D
    Check 'Enter in a date box asks like the apply button' ((Cmd 'tin') -match "-$other pending=True" -and (Cmd 'uitext DateApply') -match "text='정말 변경'")
    Press 'Tab9' | Out-Null
    Check 'coming back to the tab cancels the date change' ((Cmd 'tin') -match "-$day pending=False")
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'people' {
    # NPC 탭의 목록과 즉시 검색.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab8' | Out-Null
    $o = Cmd 'uitext NpcListRange'
    if ($o -notmatch '전체 (\d+)개') { throw "NPC 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]
    Check 'the NPC list is a scrolling list' ($total -gt 8 -and (Cmd 'uipos NpcNext10') -match 'no element named' -and (Cmd 'uipos NpcSearch') -match 'no element named')
    Press 'NpcRefresh' | Out-Null
    $pick = Pos 'Pick*'
    Press $pick.name | Out-Null
    Check 'picking an NPC shows the favour buttons' (IsAt 'IntimPlus')
    $name = if ((Cmd 'uitext SelectedName') -match "text='선택: (\S+)") { $Matches[1] } else { '' }
    Cmd "ui set NpcQuery $name" | Out-Null
    $o = Cmd 'uitext NpcListRange'
    Check 'typing a name narrows the list' ($name.Length -gt 0 -and $o -match '전체 (\d+)개' -and [int]$Matches[1] -ge 1 -and [int]$Matches[1] -lt $total) $o
    Cmd 'ui set NpcQuery zzzzzz' | Out-Null
    Check 'no match shows an empty list' ((Cmd 'uitext NpcListRange') -match "text='0개'")
    Press 'NpcQueryClear' | Out-Null
    Check 'clearing brings everyone back' ((Cmd 'uitext NpcListRange') -match "전체 $($total)개")
    Press 'NpcListLast' | Out-Null
    Check 'the list scrolls to the end' ((Cmd 'uitext NpcListRange') -match "~$($total)번째")
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'history' {
    # 스펙 6절 18번. 안전 탭의 변경 이력 전체 보기. 방금 한 변경이 맨 위에 오는지 보려고 먼저 하나를 만든다.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null; Press 'Restore' | Out-Null
    Press 'Tab11' | Out-Null
    $o = Cmd 'uitext HistoryRange'
    if ($o -notmatch '전체 (\d+)개') { throw "이력 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]
    $lines = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ }).Count
    Check 'past runs are listed too' ($total -eq [Math]::Min(2000, $lines)) "list=$total file=$lines"
    Check 'the newest change is on top' ((Cmd 'uitext Row0') -match '자원 회복')
    Cmd 'ui set HistoryQuery 가방' | Out-Null
    $o = Cmd 'uitext HistoryRange'
    Check 'the search narrows it' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -lt $total) $o
    Press 'Row0' | Out-Null
    Check 'pressing a line shows it whole' ((Cmd 'uitext HistoryDetail') -match '가방')
    Press 'HistoryQueryClear' | Out-Null
    Check 'the backup button is still there' (IsAt 'BackupNow')
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'confirm' {
    # 스펙 6절 11~14번, Review Focus 4·5. 하위 화면과 확인 대기.
    $traps = Traps
    Cmd 'editor' | Out-Null
    Press 'Tab8' | Out-Null; Press 'NpcRefresh' | Out-Null; Press (Pos 'Pick*').name | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab6' | Out-Null
    $rm = (Pos 'LuckRemove*').name
    Press $rm | Out-Null
    Check 'the first press only asks' ((Cmd "uitext $rm") -match "text='정말 삭제'")
    Press 'Tab5' | Out-Null; Press 'Tab6' | Out-Null
    Check 'a tab change cancels it' ((Cmd "uitext $rm") -match "text='삭제'")
    Press $rm | Out-Null
    Press 'LuckAddOpen' | Out-Null
    Check 'a sub screen opens with one back button' ((IsAt 'Back') -and (Cmd 'uipos LuckAddOpen') -match 'no element named')
    Press 'Back' | Out-Null
    Check 'opening a sub screen cancels it' ((Cmd "uitext $rm") -match "text='삭제'")
    Press $rm | Out-Null
    Press 'ToPlayer' | Out-Null
    Press 'Tab8' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab6' | Out-Null
    Check 'a target change cancels it' ((Cmd "uitext $rm") -match "text='삭제'")

    # Review Focus 5: 하위 화면이 열린 채 대상을 바꾸거나 탭을 떠난다
    Press 'FateOpen' | Out-Null
    Check 'the fate screen opens' (IsAt 'Back')
    Press 'ToPlayer' | Out-Null
    Check 'a target change closes the sub screen' ((IsAt 'LuckAddOpen') -and (Cmd 'uistate') -match 'target=player ')
    Press 'LuckAddOpen' | Out-Null
    Cmd 'ui set LuckQuery zzzzzz' | Out-Null
    Check 'the picker search is immediate' ((Cmd 'uitext LuckPickRange') -match "text='0개'")
    Press 'Tab5' | Out-Null; Press 'Tab6' | Out-Null
    Check 'leaving the tab closes the sub screen' (IsAt 'LuckAddOpen')

    # 공법: 기술 추가 화면의 목록과 검색, 옵션 교체 화면
    Press 'Tab5' | Out-Null; Press 'SkillAddOpen' | Out-Null
    $o = Cmd 'uitext SkillListRange'
    Check 'the skill list is a scrolling list' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -gt 12) $o
    Cmd 'ui set SkillQuery zzzzzz' | Out-Null
    Check 'the skill search is immediate' ((Cmd 'uitext SkillListRange') -match "text='0개'")
    Press 'SkillQueryClear' | Out-Null
    Press 'Back' | Out-Null
    Press (Pos 'PickMartial*').name | Out-Null
    Check 'picking a skill shows its options' (IsAt 'MartialEquip')
    Press 'MartialForget' | Out-Null
    Check 'forgetting asks first' ((Cmd 'uitext MartialForget') -match "text='정말 망각'")
    Press 'MartialExp' | Out-Null
    Check 'another action cancels it' ((Cmd 'uitext MartialForget') -match "text='기술 망각'")

    # 기예: 목록과 검색
    Press 'Tab7' | Out-Null
    $o = Cmd 'uitext ArtTableRange'
    Check 'the art list is a scrolling list' ($o -match '전체 (\d+)개') $o
    Cmd 'ui set ArtQuery zzzzzz' | Out-Null
    Check 'the art search is immediate' ((Cmd 'uitext ArtTableRange') -match "text='0개'")
    Press 'ArtQueryClear' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'actions' {
    # 스펙 6절 12~14번. 새 목록에서 실제 동작이 서비스까지 가는지: 기술 배우기, 운명 추가와 삭제, 서적 배우기·잊기.
    $traps = Traps
    # 확인이 필요한 버튼이면 한 번 더 누른다.
    function PressThrough([string]$name) {
      Press $name | Out-Null
      if ((Cmd "uitext $name") -match "text='정말") { Press $name | Out-Null }
      Cmd 'uistate'
    }
    Cmd 'editor' | Out-Null
    Cmd 'uitarget player' | Out-Null

    Press 'Tab5' | Out-Null
    $had = if ((Cmd 'uitext MartialListRange') -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 }
    Press 'SkillAddOpen' | Out-Null
    $s = PressThrough (Pos 'SkillAdd*').name
    Press 'Back' | Out-Null
    $has = if ((Cmd 'uitext MartialListRange') -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 }
    Check 'adding a skill puts one more in the learned list' ($has -eq $had + 1) "$had -> $has; $s"
    "INFO skill: $($s -replace '.*message=', '')"

    Press 'Tab6' | Out-Null; Press 'LuckAddOpen' | Out-Null; Press 'LuckType2' | Out-Null
    Press 'LuckQueryClear' | Out-Null   # 앞선 검사가 검색어를 남겼을 수 있다
    $added = $null
    for ($i = 0; $i -lt 6 -and -not $added; $i++) {
      $p = Pos 'LuckAdd*'
      if ((Rc "click $($p.x) $($p.y)" 600) -match 'Button\.onClick: ') { $added = $p.name -replace 'LuckAdd', '' } else { Press 'LuckPickNext' | Out-Null }
    }
    $s = Cmd 'uistate'
    Check 'adding a luck reports a result' ($added -and $s -match "message='[^']+'") $s
    "INFO luck add: $($s -replace '.*message=', '')"
    Press 'Back' | Out-Null
    # 방금 붙인 운명은 목록의 끝에 온다. 목록이 한 화면보다 길면 끝으로 간다.
    if ((Cmd 'uipos LuckListLast') -match ' at ') { try { Press 'LuckListLast' | Out-Null } catch { } }
    Check 'the added luck is in the list' ((Cmd "uipos LuckRemove$added") -match ' at ')
    $s = PressThrough "LuckRemove$added"
    Check 'removing it reports a result' ($s -match "message='[^']+'" -and (Cmd "uipos LuckRemove$added") -match 'no element named') $s
    "INFO luck remove: $($s -replace '.*message=', '')"

    Press 'Tab7' | Out-Null
    $book = (Pos 'ArtAct*').name
    $label0 = Cmd "uitext $book"
    $s = PressThrough $book
    $label1 = Cmd "uitext $book"
    Check 'the art button changes the book state' ($label1 -ne $label0) "$label0 -> $label1; $s"
    "INFO art 1: $($s -replace '.*message=', '')"
    $s = PressThrough $book
    Check 'and the opposite action brings it back' ((Cmd "uitext $book") -eq $label0) $s
    "INFO art 2: $($s -replace '.*message=', '')"
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'vault' {
    # 묶음 C(docs/superpowers/specs/2026-10-13-bag-export-design.md 6절). 내보내기 폴더는 세이브 밖이라 회차가 되돌리지 않는다.
    # 이 검사가 만든 파일(uicheck-로 시작)은 끝날 때와 멈췄을 때 지운다.
    $traps = Traps
    $dir = Join-Path $data 'exports\bag'
    function CleanVault { if (Test-Path -LiteralPath $dir) { Get-ChildItem -LiteralPath $dir -Filter 'uicheck-*' | ForEach-Object { [IO.File]::Delete($_.FullName) } } }
    function Num([string]$cmd, [string]$pattern) { if ((Cmd $cmd 100) -match $pattern) { [int]$Matches[1] } else { -1 } }
    function Msg { if ((Cmd 'uistate' 100) -match "message='(.*)'\s*$") { $Matches[1] } else { '' } }
    CleanVault
    try {
      Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null; Press 'VaultModeBag' | Out-Null   # 보관함은 마지막에 본 쪽(가방/캐릭터)을 기억한다

      # 6절 1~3번, Review Focus 2: 이름과 파일. 영석과 화폐는 가방의 아이템 목록에도 들어 있어(한 종에 한 줄) 아이템 줄에서는 빠진다
      Cmd 'ui set VaultName' | Out-Null   # 이름 칸은 앞서 적은 것을 기억한다. 비우고 시작한다
      Press 'VaultExport' | Out-Null
      Check 'an empty name is refused' ((Msg) -match '이름을' -and -not (Test-Path -LiteralPath (Join-Path $dir '.json')))
      Cmd 'ui set VaultName uicheck-A' | Out-Null; Press 'VaultExport' | Out-Null
      $fileA = Join-Path $dir 'uicheck-A.json'
      Check 'export writes a file' ((Test-Path -LiteralPath $fileA) -and (Msg) -match '내보냈습니다') (Msg)
      $pack = Get-Content -LiteralPath $fileA -Raw -Encoding UTF8 | ConvertFrom-Json
      $all = Num 'read' 'props=(\d+)'
      Check 'the file is valid JSON with every plain item' ($pack.format -eq 1 -and $pack.kind -eq 'bag' -and $pack.items.Count + $pack.currencies.Count -eq $all - $pack.skipped -and $pack.items.Count -gt 0) "items=$($pack.items.Count) skipped=$($pack.skipped) bag=$all"
      Check 'currencies are in the file' (@($pack.currencies | Where-Object { $_.id -eq 10001 -and $_.count -gt 0 }).Count -eq 1)
      $moneyIds = @($pack.currencies | ForEach-Object { $_.id })
      Check 'currencies are not also listed as items' (@($pack.items | Where-Object { $moneyIds -contains $_.id }).Count -eq 0) "$(@($pack.items | Where-Object { $moneyIds -contains $_.id } | ForEach-Object { $_.id }) -join ',')"
      Check 'equipped items are marked' (@($pack.items | Where-Object { $_.equipped }).Count -gt 0)
      Cmd 'ui set VaultName uicheck-A' | Out-Null; Press 'VaultExport' | Out-Null
      Check 'the same name gets a number' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-A (2).json'))
      Cmd 'ui set VaultName uicheck-b:c*d?' | Out-Null; Press 'VaultExport' | Out-Null
      Check 'characters a file name cannot hold become underscores' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))
      Check 'the header shows the bag slots' ((Cmd 'uitext VaultBagSlots') -match '가방 \d+/\d+') (Cmd 'uitext VaultBagSlots')
      Cmd 'ui set VaultQuery UICHECK-A' | Out-Null
      Check 'the search ignores case' ((Cmd 'uitext VaultListRange') -match '전체 2개') (Cmd 'uitext VaultListRange')
      Press 'VaultQueryClear' | Out-Null
      Check 'the list shows three exports' ((Cmd 'uitext VaultListRange') -match '전체 3개' -or (Num 'uitext VaultListRange' '전체 (\d+)개') -ge 3)

      # 가져오기의 기준이 되는 묶음을 손으로 만든다: 겹쳐지는 것 1, 겹쳐지지 않는 것 2(같은 번호 두 줄), 비급 1, 화폐 1, 그리고 걸러져야 할 줄들
      $martial = @($pack.items | Where-Object { $_.type -eq 'Martial' })[0]
      $stack = @($pack.items | Where-Object { $_.type -eq 'Props' -and $_.count -gt 1 })[0]
      $single = @($pack.items | Where-Object { $_.type -eq 'Props' -and $_.count -eq 1 -and -not $_.equipped })[0]
      $made = [ordered]@{ format = 1; kind = 'bag'; name = 'uicheck-M'; exportedAt = '2026-10-13 00:00:00'; character = 'test'; skipped = 0
        items = @(
          [ordered]@{ id = $stack.id; name = $stack.name; count = 3; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $single.id; name = $single.name; count = 1; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $single.id; name = $single.name; count = 1; type = 'Props'; values = @(); equipped = $true },
          [ordered]@{ id = 0; name = $martial.name; count = 1; type = 'Martial'; values = $martial.values; equipped = $false },
          [ordered]@{ id = 999999999; name = 'no such item'; count = 1; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $stack.id; name = 'zero'; count = 0; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $stack.id; name = 'negative'; count = -5; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = 10001; name = 'money as an item'; count = 5; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = 0; name = 'empty manual'; count = 1; type = 'Martial'; values = @(); equipped = $false })
        currencies = @([ordered]@{ id = 10041; name = '성주령'; count = 7 }) }
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-M.json'), ($made | ConvertTo-Json -Depth 6))
      Press 'Tab3' | Out-Null   # 탭으로 다시 들어오면 목록을 새로 읽는다. 방금 쓴 파일이 맨 위(0번 줄)다
      Check 'the hand-made export is on top' ((Cmd 'uitext Row0') -match 'uicheck-M')

      # 6절 4~8번: 세부 화면에서 하나씩
      Press 'VaultOpen0' | Out-Null
      Check 'the detail screen lists items and currencies' ((IsAt 'Back') -and (Cmd 'uitext VaultItemsRange') -match '전체 10개')
      $n0 = Num "count $($stack.id)" 'count=(\d+)'; $g0 = Num 'grid' 'cur=(\d+)'
      Press 'VaultGet0' | Out-Null
      Check 'a stackable item adds to the stack without a slot' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n0 + 3 -and (Num 'grid' 'cur=(\d+)') -eq $g0) (Msg)
      $n1 = Num "count $($single.id)" 'count=(\d+)'
      Press 'VaultGet1' | Out-Null
      Check 'a single item takes one slot' ((Num "count $($single.id)" 'count=(\d+)') -eq $n1 + 1 -and (Num 'grid' 'cur=(\d+)') -eq $g0 + 1) (Msg)
      $p0 = Num 'read' 'props=(\d+)'
      Press 'VaultGet3' | Out-Null
      Check 'a manual is recreated' ((Num 'read' 'props=(\d+)') -eq $p0 + 1) (Msg)
      $n2 = Num "count $($stack.id)" 'count=(\d+)'
      # 쓸 수 없는 줄은 이유가 줄에 보이고 버튼이 눌리지 않는다
      $why = (Cmd 'uidescs VaultWhy' 200)
      $p4 = Pos 'VaultGet4'; $p5 = Pos 'VaultGet5'
      Check 'an unknown item shows why and cannot be imported' ($why -match "VaultWhy4 text='[^']*게임에 없는" -and (Rc "click $($p4.x) $($p4.y)" 300) -notmatch 'Button\.onClick') $why
      Check 'a zero count shows why and cannot be imported' ($why -match "VaultWhy5 text='[^']*수량" -and (Rc "click $($p5.x) $($p5.y)" 300) -notmatch 'Button\.onClick' -and (Num "count $($stack.id)" 'count=(\d+)') -eq $n2) $why
      $c0 = Num 'count 10041' 'count=(\d+)'
      Press 'VaultGetC0' | Out-Null
      Check 'a currency adds its amount' ((Num 'count 10041' 'count=(\d+)') -eq $c0 + 7) (Msg)
      Cmd 'ui set VaultItemQuery 성주령' | Out-Null
      Check 'the detail search filters' ((Cmd 'uitext VaultItemsRange') -match '전체 1개')
      Press 'VaultItemQueryClear' | Out-Null
      Press 'Back' | Out-Null

      # 6절 9번: 빈 칸이 넉넉할 때 전부
      $n3 = Num "count $($stack.id)" 'count=(\d+)'; $n4 = Num "count $($single.id)" 'count=(\d+)'; $c1 = Num 'count 10041' 'count=(\d+)'; $p1 = Num 'read' 'props=(\d+)'; $m1 = Num 'count 10001' 'count=(\d+)'
      Press 'VaultAll0' | Out-Null
      $m = Msg
      Check 'import-all adds everything valid' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n3 + 3 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n4 + 2 -and (Num 'count 10041' 'count=(\d+)') -eq $c1 + 7 -and (Num 'read' 'props=(\d+)') -eq $p1 + 3) $m
      Check 'the result counts what went in and what was skipped' ($m -match '아이템 4줄' -and $m -match '화폐 1종' -and $m -match '건너뜀 5줄') $m
      Check 'a currency written as an item line is not imported' ((Num 'count 10001' 'count=(\d+)') -eq $m1)

      # 6절 10~11번, Review Focus 3: 가방을 가득 채운 뒤
      Cmd 'gridfill 600 5' 4000 | Out-Null
      $extra0 = Num 'bagstat' 'extra=(\d+)'; $n5 = Num "count $($stack.id)" 'count=(\d+)'; $n6 = Num "count $($single.id)" 'count=(\d+)'
      Press 'VaultAll0' | Out-Null
      Check 'a full bag asks instead of importing' ((IsAt 'VaultExpand0') -and (IsAt 'VaultFit0') -and (Msg) -match '필요' -and (Num "count $($stack.id)" 'count=(\d+)') -eq $n5) (Msg)
      Press 'VaultFit0' | Out-Null
      $m = Msg
      Check 'fit-only adds the stackable and reports the rest' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n5 + 3 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n6 -and $m -match '못 넣음 3줄' -and (Num 'bagstat' 'extra=(\d+)') -eq $extra0) $m
      Press 'VaultAll0' | Out-Null; Press 'VaultExpand0' | Out-Null
      $m = Msg
      Check 'expand-and-import grows the bag by tens and adds everything' ((Num 'bagstat' 'extra=(\d+)') -eq $extra0 + 10 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n6 + 2 -and $m -notmatch '못 넣음') $m

      # 가방이 넘쳐 있을 때(추가 칸을 줄여 쓰는 칸이 전체보다 많다)도 "늘리고 모두"는 넘친 만큼까지 늘려야 한다
      Cmd 'gridfill 600 5' 4000 | Out-Null; Cmd 'bag -10' | Out-Null
      $over = (Num 'grid' 'cur=(\d+)') - (Num 'grid' 'max=(\d+)')
      $extra1 = Num 'bagstat' 'extra=(\d+)'; $n7 = Num "count $($single.id)" 'count=(\d+)'
      Press 'VaultAll0' | Out-Null; Press 'VaultExpand0' | Out-Null
      $m = Msg
      Check 'expanding an overfull bag covers the overflow too' ($over -gt 0 -and (Num 'bagstat' 'extra=(\d+)') -eq $extra1 + [math]::Ceiling(($over + 3) / 10) * 10 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n7 + 2 -and $m -notmatch '못 넣음') "over=$over $m"

      # 6절 12번, Review Focus 1·4: 읽을 수 없는 파일, 밖에서 지워진 파일
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-broken.json'), '{ "format": 1, "kind": "bag" "items": [ }')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-empty.json'), '')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-v2.json'), '{ "format": 2, "kind": "bag", "items": [], "currencies": [] }')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-big.json'), '{ "format": 1, "kind": "bag", "name": "big", "items": [ { "id": 99999999999999, "count": "many", "type": "Props" } ] }')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-deep.json'), '{ "format": 1, "kind": "bag", "x": ' + ('[' * 200) + (']' * 200) + ', "items": [], "currencies": [] }')
      Press 'Tab3' | Out-Null
      Cmd 'ui set VaultQuery uicheck-' | Out-Null
      $unreadable = 0; for ($i = 0; $i -lt 9; $i++) { if ((Cmd "uitext Row$i" 100) -match '읽을 수 없음') { $unreadable++ } }
      Check 'broken, empty, other-format and too-deep files are listed as unreadable' ($unreadable -eq 4) "unreadable=$unreadable"
      Cmd 'ui set VaultQuery uicheck-big' | Out-Null
      Press 'VaultAll0' | Out-Null
      Check 'bad values in a readable file are skipped, not thrown' ((Msg) -match '건너뜀 1줄') (Msg)
      Cmd 'ui set VaultQuery uicheck-M' | Out-Null
      [IO.File]::Delete((Join-Path $dir 'uicheck-M.json'))
      Press 'VaultAll0' | Out-Null
      Check 'a file removed behind the list is reported' ((Msg) -match '파일이 없습니다') (Msg)

      # 6절 14번: 삭제는 두 번 눌러야 한다
      Cmd 'ui set VaultQuery uicheck-A' | Out-Null
      Press 'VaultDelete0' | Out-Null
      Check 'delete asks first' ((Cmd 'uitext VaultDelete0') -match "text='정말 삭제'")
      Press 'Tab1' | Out-Null; Press 'Tab3' | Out-Null
      Check 'another action keeps the files' ((Test-Path -LiteralPath $fileA) -and (Test-Path -LiteralPath (Join-Path $dir 'uicheck-A (2).json')))
      Cmd 'ui set VaultQuery uicheck-A' | Out-Null
      $before = @(Get-ChildItem -LiteralPath $dir -Filter 'uicheck-A*').Count
      Press 'VaultDelete0' | Out-Null; Press 'VaultDelete0' | Out-Null
      Check 'the second press deletes exactly one file' (@(Get-ChildItem -LiteralPath $dir -Filter 'uicheck-A*').Count -eq $before - 1) (Msg)
      Press 'VaultQueryClear' | Out-Null

      # 6절 16번: 변경 이력
      $hist = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Select-Object -Last 40) -join "`n"
      Check 'imports are in the change log' ($hist -match '가방 가져오기')

      # 6절 15번의 준비: 저장하고 다시 시작한 뒤에도 남는지
      [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), "$($single.id) $(Num "count $($single.id)" 'count=(\d+)')")
      Press 'Close' | Out-Null
      Cmd 'save' 5000 | Out-Null
      Check 'no exception in button callbacks' ((Traps) -eq $traps)
      Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    } finally { CleanVault }
  }
  'vault-restart' {
    $want = ([IO.File]::ReadAllText((Join-Path $work 'check-expect.txt'))) -split ' '
    Check 'imported items survive save and restart' ((Cmd "count $($want[0])") -match "count=$($want[1])$")
    Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null
    Check 'the vault opens with no test files left' ((Cmd 'ui set VaultQuery uicheck-') -ne $null -and (Cmd 'uitext VaultListRange') -match "text='0개'")
    Press 'VaultQueryClear' | Out-Null; Press 'Close' | Out-Null
  }
  'wrap' {
    # 05 문서 29절. 목록의 글은 말줄임 없이 줄바꿈되어 전부 보이고, 줄의 높이가 글에 맞춰 늘어난다. 화폐 탭의 명성.
    $traps = Traps
    function TextIn([string]$name) { $o = Cmd "uitext $name" 100; if ($o -match "(?s) text='(.*)'\s*$") { $Matches[1] } else { '' } }
    function Box([string]$name) {
      $o = Cmd "uipos $name" 100
      if ($o -notmatch ' at (-?\d+),(-?\d+) size=(\d+),(\d+)') { return $null }
      [pscustomobject]@{ top = [int]$Matches[2] - [int]$Matches[4] / 2; h = [int]$Matches[4] }
    }
    Cmd 'editor' | Out-Null

    # 화폐 탭: 설명이 잘리지 않는다(정도의 기 10031의 설명은 44자를 넘는다)
    Press 'Tab2' | Out-Null
    $d = TextIn 'CurrencyDesc10031'
    Check 'a currency description is whole' ($d.Length -gt 46 -and $d -notmatch '…') "len=$($d.Length) $d"
    $a = Box 'CurrencyRow10031'; $b = Box 'CurrencyRow10032'
    Check 'currency rows do not overlap' ($a -and $b -and $b.top -ge $a.top + $a.h - 1) "a=$($a.top)+$($a.h) b=$($b.top)"

    # 화폐 탭의 명성: 추가량만큼 오르고 저장값(reputation)과 같다
    $fame0 = if ((Cmd 'fame' 100) -match 'saved=(-?\d+)') { [int]$Matches[1] } else { -999 }
    Check 'the fame row shows the saved value' ((TextIn 'CurrencyOwnedFame') -eq "$fame0") "row=$(TextIn 'CurrencyOwnedFame') saved=$fame0"
    Press 'CurrencyAddFame' | Out-Null
    $o = Cmd 'fame' 100
    Check 'fame rises by the amount' ($o -match "saved=$($fame0 + 10) value=$($fame0 + 10)\b") $o
    Check 'the fame row is redrawn' ((TextIn 'CurrencyOwnedFame') -eq "$($fame0 + 10)")
    $hist = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Select-Object -Last 3) -join "`n"
    Check 'the fame change is in the change log' ($hist -match "명성\s+$fame0 -> $($fame0 + 10)") $hist

    # 자원 탭: 보이는 줄마다 설명이 전부 들어 있고, 긴 설명의 줄은 더 높고, 줄이 서로 겹치지 않는다
    Press 'Tab1' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    $shown = if ($o -match '중 1~(\d+)번째') { [int]$Matches[1] } else { 0 }
    Check 'the item list shows rows' ($shown -ge 3) $o
    $cut = 0; $tall = 0; $gaps = 0; $longest = 0; $prev = $null
    for ($i = 0; $i -lt $shown; $i++) {
      $row = Box "Row$i"
      if ($prev -and [math]::Abs($row.top - ($prev.top + $prev.h)) -gt 1) { $gaps++ }
      if ($row.h -gt 34) { $tall++ }
      $prev = $row
    }
    # 명령의 출력은 "uidescs ItemDesc -> "로 시작한다. 그 머리를 떼어야 첫 줄이 빠지지 않는다
    $descs = @(((Cmd 'uidescs ItemDesc' 300) -replace '^[^\n]*?-> ', '') -split "`n" | Where-Object { $_ -match '^ItemDesc\d+ ' })
    foreach ($line in $descs) { if ($line -match '…') { $cut++ }; if ($line.Length -gt $longest) { $longest = $line.Length } }
    Check 'item descriptions are whole' ($descs.Count -ge 3 -and $cut -eq 0 -and $longest -gt 40) "rows=$($descs.Count) cut=$cut longest=$longest"
    Check 'rows with long text are taller' ($tall -ge 1) "tall=$tall of $shown"
    # 31절: 설명 열을 넓혀 한 화면에 더 많은 줄이 보이고, 높아진 줄의 버튼은 줄의 세로 가운데에 온다
    $firstDesc = if ($descs[0] -match '^(ItemDesc\d+) ') { $Matches[1] } else { '' }
    Check 'the description column is wide' ((Cmd "uipos $firstDesc" 100) -match 'size=605,') (Cmd "uipos $firstDesc" 100)
    Check 'more than ten rows fit on a screen' ($shown -ge 11) "shown=$shown"
    $off = 99
    for ($i = 0; $i -lt $shown; $i++) {
      $row = Box "Row$i"
      if ($row.h -gt 34 -and $descs[$i] -match '^ItemDesc(\d+) ') { $give = Pos "ItemGive$($Matches[1])"; $off = [math]::Abs($give.y - ($row.top + $row.h / 2)); break }
    }
    Check 'the button of a tall row sits in the middle' ($off -le 2) "off=$off"
    Check 'rows sit one under another' ($gaps -eq 0) "gaps=$gaps"
    Check 'the last shown row is inside the list' ((Box "Row$($shown - 1)") -and (IsAt (Pos 'ItemGive*').name))

    # 이동: 다음은 다 보이지 않던 첫 줄에서 시작하고, 끝은 마지막 줄까지 간다
    $total = if ($o -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 }
    Press 'ItemTableNext' | Out-Null
    Check 'next starts at the first row that was not whole' ((Cmd 'uitext ItemTableRange') -match "중 $($shown + 1)~")
    Press 'ItemTableLast' | Out-Null
    Check 'last reaches the end' ((Cmd 'uitext ItemTableRange') -match "~$($total)번째")
    $last = Box "Row$($total - 1)"; $view = Box 'ItemTable'
    Check 'the last row ends at the bottom of the list' ($last -and $view -and [math]::Abs(($last.top + $last.h) - ($view.top + $view.h)) -le 2) "row=$($last.top)+$($last.h) view=$($view.top)+$($view.h)"
    Press 'ItemTablePrev' | Out-Null; Press 'ItemTableFirst' | Out-Null
    Check 'first returns to the top' ((Cmd 'uitext ItemTableRange') -match '중 1~')

    # 다른 탭의 목록도 말줄임이 없다(기예, 운명 고르기, 기술 추가, 변경 이력)
    # 검색 칸은 앞선 검사(confirm)가 적은 글을 기억한다. 비우고 읽는다.
    Press 'Tab7' | Out-Null; Cmd 'ui set ArtQuery' | Out-Null
    $n = @((Cmd 'uidescs ArtDesc' 300) -split "`n" | Where-Object { $_ -match '^ArtDesc' })
    Check 'art book descriptions are whole' ($n.Count -ge 3 -and @($n | Where-Object { $_ -match '…' }).Count -eq 0) "rows=$($n.Count)"
    Press 'Tab6' | Out-Null; Press 'LuckAddOpen' | Out-Null; Cmd 'ui set LuckQuery' | Out-Null
    $n = @((Cmd 'uidescs LuckTips' 300) -split "`n" | Where-Object { $_ -match '^LuckTips' })
    Check 'luck effects are whole' ($n.Count -ge 3 -and @($n | Where-Object { $_ -match '…' }).Count -eq 0) "rows=$($n.Count)"
    Press 'Back' | Out-Null
    # 공법의 옵션: 글이 전부 들어 있고, 줄바꿈으로 높아진 옵션 줄이 아래의 장착 버튼과 겹치지 않는다
    Press 'Tab5' | Out-Null; Press 'PickMartial1' | Out-Null
    $bottom = 0; $cut = 0; $opts = 0
    for ($i = 0; $i -lt 6; $i++) {
      $b = Box "OptionText$i"
      if ($b) { $opts++; if ($b.top + $b.h -gt $bottom) { $bottom = $b.top + $b.h }; if ((TextIn "OptionText$i") -match '…') { $cut++ } }
    }
    $equip = Box 'MartialEquip'
    Check 'martial options are whole and above the equip button' ($opts -ge 1 -and $cut -eq 0 -and $equip -and $bottom -le $equip.top) "options=$opts cut=$cut bottom=$bottom equip=$($equip.top)"
    Shot 'wrap-martial'
    Press 'Tab5' | Out-Null; Press 'SkillAddOpen' | Out-Null; Cmd 'ui set SkillQuery' | Out-Null
    $n = @((Cmd 'uidescs SkillDesc' 300) -split "`n" | Where-Object { $_ -match '^SkillDesc' })
    Check 'skill descriptions are whole' ($n.Count -ge 3 -and @($n | Where-Object { $_ -match '…' }).Count -eq 0) "rows=$($n.Count)"
    Press 'Back' | Out-Null
    Press 'Tab11' | Out-Null
    $n = @((Cmd 'uidescs HistoryLine' 300) -split "`n" | Where-Object { $_ -match '^HistoryLine' })
    Check 'history lines are whole' ($n.Count -ge 1 -and @($n | Where-Object { $_ -match '…' }).Count -eq 0) "rows=$($n.Count)"
    Shot 'wrap-history'
    Press 'Tab1' | Out-Null; Shot 'wrap-items'
    Press 'Tab2' | Out-Null; Shot 'wrap-currency'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'charvault' {
    # 묶음 D(docs/superpowers/specs/2026-10-13-character-export-design.md 7절). 내보내기 폴더는 세이브 밖이라 회차가 되돌리지 않는다.
    # 이 검사가 만든 파일(uicheck-로 시작)은 끝날 때와 멈췄을 때 지운다.
    $traps = Traps
    $dir = Join-Path $data 'exports\character'
    $bagDir = Join-Path $data 'exports\bag'
    function CleanChar { foreach ($d in $dir, $bagDir) { if (Test-Path -LiteralPath $d) { Get-ChildItem -LiteralPath $d -Filter 'uicheck-*' | ForEach-Object { [IO.File]::Delete($_.FullName) } } } }
    function Msg { if ((Cmd 'uistate' 100) -match "message='(.*)'\s*$") { $Matches[1] } else { '' } }
    function State { $o = Cmd 'cstate' 200; $h = @{ raw = $o }; foreach ($m in [regex]::Matches($o, '(\w+)=(\S+)')) { $h[$m.Groups[1].Value] = $m.Groups[2].Value }; $h }
    function Has([string]$cmd, [string]$pattern) { (Cmd $cmd 200) -match $pattern }
    CleanChar
    try {
      Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null; Press 'VaultModeChar' | Out-Null
      $s0 = State

      # 7절 1~2번: 내보내기와 파일
      Cmd 'ui set CharName' | Out-Null   # 이름 칸은 앞서 적은 것을 기억한다. 비우고 시작한다
      Press 'CharExport' | Out-Null
      Check 'an empty name is refused' ((Msg) -match '이름을')
      Cmd 'ui set CharName uicheck-C' | Out-Null; Press 'CharExport' | Out-Null
      $fileC = Join-Path $dir 'uicheck-C.json'
      Check 'export writes a file' ((Test-Path -LiteralPath $fileC) -and (Msg) -match '내보냈습니다') (Msg)
      $pack = Get-Content -LiteralPath $fileC -Raw -Encoding UTF8 | ConvertFrom-Json
      Check 'the file names its kind' ($pack.format -eq 1 -and $pack.kind -eq 'character' -and $pack.character.Length -gt 0)
      Check 'the grade is in the file' ("$($pack.grade.id)" -eq $s0.grade -and "$($pack.grade.exp)" -eq $s0.exp) "file=$($pack.grade.id)/$($pack.grade.exp) game=$($s0.grade)/$($s0.exp)"
      Check 'every attribute is in the file' ("$(@($pack.attributes).Count)" -eq $s0.attrs -and @($pack.attributes | Where-Object { $_.key -eq 'attack' -and "$($_.value)" -eq $s0.attack }).Count -eq 1) "file=$(@($pack.attributes).Count) game=$($s0.attrs)"
      Check 'attribute keys are unique' (@($pack.attributes | ForEach-Object { $_.key } | Sort-Object -Unique).Count -eq @($pack.attributes).Count)
      Check 'every learned martial is in the file' ("$(@($pack.martials).Count)" -eq $s0.martials -and @($pack.martials | Where-Object { @($_.values).Count -lt 3 }).Count -eq 0) "file=$(@($pack.martials).Count) game=$($s0.martials)"
      Check 'learned arts are in the file' ("$(@($pack.arts).Count)" -eq $s0.arts) "file=$(@($pack.arts).Count) game=$($s0.arts)"
      Check 'born lucks are in the file' ("$(@($pack.bornLucks).Count)" -eq $s0.born)
      Check 'progress markers and timed lucks are left out' (@($pack.addLucks).Count -le [int]$s0.add)
      Cmd 'ui set CharName uicheck-C' | Out-Null; Press 'CharExport' | Out-Null
      Check 'the same name gets a number' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-C (2).json'))
      Cmd 'ui set CharName uicheck-b:c*d?' | Out-Null; Press 'CharExport' | Out-Null
      Check 'characters a file name cannot hold become underscores' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))
      Cmd 'ui set CharQuery uicheck-' | Out-Null
      Cmd 'ui set CharQuery UICHECK-C' | Out-Null
      Check 'the search ignores case' ((Cmd 'uitext CharListRange') -match '전체 2개') (Cmd 'uitext CharListRange')
      Cmd 'ui set CharQuery uicheck-' | Out-Null
      Check 'the list shows three exports' ((Cmd 'uitext CharListRange') -match '전체 3개')

      # 7절 3번, Review Focus 4: 가방 목록과 섞이지 않는다
      Press 'VaultModeBag' | Out-Null
      Cmd 'ui set VaultName uicheck-bagside' | Out-Null; Press 'VaultExport' | Out-Null
      Cmd 'ui set VaultQuery uicheck-' | Out-Null
      Check 'the bag list shows only the bag export' ((Cmd 'uitext VaultListRange') -match '전체 1개' -and (Cmd 'uitext Row0') -match 'uicheck-bagside')
      Press 'VaultQueryClear' | Out-Null
      Press 'VaultModeChar' | Out-Null
      Check 'lists do not mix' ((Cmd 'uitext CharListRange') -match '전체 3개' -and (IsAt 'CharExport') -and -not (IsAt 'VaultExport'))
      Check 'the mode is kept when the tab is left' ((Press 'Tab1') -and (Press 'Tab3') -and (IsAt 'CharExport'))

      # 7절 16번의 삭제
      Cmd 'ui set CharQuery uicheck-b' | Out-Null
      Press 'CharDelete0' | Out-Null
      Check 'delete asks first' ((Cmd 'uitext CharDelete0') -match "text='정말 삭제'" -and (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json')))
      Press 'CharDelete0' | Out-Null
      Check 'the second press deletes the file' (-not (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))) (Msg)
      Press 'CharQueryClear' | Out-Null
      # ---- 가져오기 (7절 4~16번) ----
      function Row([string]$needle) { Cmd "ui set CharItemQuery $needle" | Out-Null }
      function IndexOfMartial($list, $one) { for ($i = 0; $i -lt @($list).Count; $i++) { if ([object]::ReferenceEquals(@($list)[$i], $one)) { return $i } }; -1 }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      # 줄 0은 "uicheck-C (2)"(나중에 쓴 것), 줄 1이 uicheck-C다. 내용은 같다.
      Press 'CharOpen0' | Out-Null
      Check 'the detail screen opens on the grade' ((IsAt 'Back') -and (IsAt 'CharGetGrade'))

      # 4~5번: 능력치
      Press 'CharSecAttributes' | Out-Null
      $atk = [int]$s0.attack; $def = [int]$s0.defense
      Cmd "cattr attack $($atk + 5)" | Out-Null; Press 'CharSecAttributes' | Out-Null; Row 'attack'
      Press 'CharGetAttr_attack' | Out-Null
      Check 'one attribute returns to the file value' ((State).attack -eq "$atk") (Msg)
      Check 'an attribute that matches cannot be imported again' (-not (IsAt 'CharGetAttr_attack') -or (Cmd 'uitext CharGetAttr_attack') -match '같음')
      Press 'CharItemQueryClear' | Out-Null
      Cmd "cattr attack $($atk + 5)" | Out-Null; Cmd "cattr defense $($def + 5)" | Out-Null
      Press 'CharSecAttributes' | Out-Null; Press 'CharImportSection' | Out-Null; Press 'CharImportSection' | Out-Null
      $s = State; $m = Msg
      Check 'the attribute section restores every changed value' ($s.attack -eq "$atk" -and $s.defense -eq "$def" -and $m -match '능력치: 변경 2') $m

      # 6번: 경지
      Cmd 'setgrade 5' | Out-Null
      Press 'CharSecGrade' | Out-Null; Press 'CharGetGrade' | Out-Null
      Check 'the grade import asks first' ((State).grade -eq '5' -and (Msg) -match '능력치는 따라오지')
      Press 'CharGetGrade' | Out-Null
      $s = State
      Check 'the second press writes the grade and its exp' ($s.grade -eq $s0.grade -and $s.exp -eq $s0.exp) $s.raw

      # 7~9번: 공법
      $mar = @($pack.martials | Where-Object { -not $_.equipped -and @($_.values).Count -gt 6 })[0]
      $mi = IndexOfMartial $pack.martials $mar
      $want = "values=\[$(@($mar.values) -join ',')\]"
      $o = Cmd "cmartial $($mar.baseId)" 200
      $sole = if ($o -match 'sole=(\S+)') { $Matches[1] } else { '' }
      Cmd "mdel $sole" | Out-Null
      Press 'CharSecMartials' | Out-Null; Row "$($mar.baseId)"
      Press "CharGetMartial$mi" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200
      Check 'a forgotten martial comes back with the same options' ($o -match '^.*n=1 ' -and $o -match $want -and (State).martials -eq $s0.martials) "$o | $(Msg)"
      Press 'CharItemQueryClear' | Out-Null
      Press 'CharImportSection' | Out-Null
      Check 'the martial section adds nothing that is already known' ((State).martials -eq $s0.martials -and (Msg) -match '공법: 추가 0') (Msg)
      Row "$($mar.baseId)"
      Press "CharGetMartial$mi" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200
      Check 'the row button adds one more of a known martial' ($o -match 'n=2 ') $o
      $extra = @([regex]::Matches($o, 'sole=(\S+)') | ForEach-Object { $_.Groups[1].Value })[-1]
      Cmd "mdel $extra" | Out-Null
      Press 'CharItemQueryClear' | Out-Null

      # 10번: 기예
      $art = @($pack.arts)[0]
      if ($art) {
        Cmd "cartdel $($art.id)" | Out-Null
        Check 'the art was forgotten for the test' ((State).arts -eq "$([int]$s0.arts - 1)")
        Press 'CharSecArts' | Out-Null; Row "$($art.id)"
        Press 'CharGetArt0' | Out-Null
        Check 'a forgotten art book comes back' ((State).arts -eq $s0.arts) (Msg)
        Press 'CharItemQueryClear' | Out-Null
      } else { 'INFO no learned art in this save; art import not exercised' }

      # 11번: 운명
      Press 'CharSecLucks' | Out-Null
      $bornId = @($pack.bornLucks)[0].id
      Cmd "cborndel $bornId" | Out-Null
      Check 'the born luck was removed for the test' (-not (Has 'luck' "born=\[[^\]]*\b$bornId\b"))
      Press 'CharSecLucks' | Out-Null; Row "$bornId"
      Press 'CharGetBorn0' | Out-Null
      Check 'a removed born luck comes back as a born luck' (Has 'luck' "born=\[[^\]]*\b$bornId\b") (Msg)
      Press 'CharItemQueryClear' | Out-Null
      $add = @($pack.addLucks)[0]
      if ($add) {
        Cmd "luckdel $($add.id)" | Out-Null
        Press 'CharSecLucks' | Out-Null; Row "$($add.id)"
        Press 'CharGetAdd0' | Out-Null
        Check 'a removed acquired luck comes back' (Has 'luck' "add=\[[^\]]*\b$($add.id)\(") (Msg)
        Press 'CharItemQueryClear' | Out-Null
      } else { 'INFO no exportable acquired luck in this save; not exercised' }

      # 12번: 역천개명
      $fate = @($pack.fates)[0]
      if ($fate) {
        $o = Cmd "cfatealt $($fate.grade)" 300
        Check 'the fate was changed for the test' ((State).fates -notmatch "$($fate.grade):$($fate.id)") $o
        # 한 경지의 역천개명을 바꿔도 다른 경지의 기록은 그대로여야 한다(게임의 추가 행동이 지금 경지의 기록을 덮어쓴다)
        $now = (State).fates; $kept = @($s0.fates -split ',' | Where-Object { $_ -notmatch "^$($fate.grade):" } | Where-Object { ",$now," -notmatch ",$_," })
        Check 'changing one grade leaves the other grades alone' ($kept.Count -eq 0) "before=$($s0.fates) now=$now"
        Press 'CharSecFates' | Out-Null
        Press 'CharGetFate0' | Out-Null
        Check 'the fate returns to the one in the file' ((State).fates -match "$($fate.grade):$($fate.id)") (Msg)
        Cmd 'setgrade 1' | Out-Null
        Press 'CharSecFates' | Out-Null
        Check 'a fate above the current grade is shown as out of reach' ((Cmd 'uidescs CharFateState' 200) -match '경지 미달')
        Press 'CharSecGrade' | Out-Null; Press 'CharGetGrade' | Out-Null; Press 'CharGetGrade' | Out-Null
      } else { 'INFO no fate recorded in this save; fate import not exercised' }

      # Review Focus 5: 세부 화면을 연 채 떠나면 닫힌다
      Press 'Tab1' | Out-Null; Press 'Tab3' | Out-Null
      Check 'leaving closes the detail screen' ((IsAt 'CharExport') -and -not (IsAt 'CharGetGrade'))

      # 13번, Review Focus 2: 여러 가지를 바꾼 뒤 모두 가져오기
      Cmd 'setgrade 5' | Out-Null; Cmd "cattr attack $($atk + 5)" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200; if ($o -match 'sole=(\S+)') { Cmd "mdel $($Matches[1])" | Out-Null }
      if ($art) { Cmd "cartdel $($art.id)" | Out-Null }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      Press 'CharAll0' | Out-Null
      Check 'import-all asks first' ((State).grade -eq '5' -and (Cmd 'uitext CharAll0') -match "text='정말 가져오기'")
      Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'import-all restores the character' ($s.grade -eq $s0.grade -and $s.exp -eq $s0.exp -and $s.attack -eq $s0.attack -and $s.martials -eq $s0.martials -and $s.arts -eq $s0.arts -and $s.born -eq $s0.born -and $s.fates -eq $s0.fates) "$($s.raw) | $m"
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'importing twice adds nothing' ($s.martials -eq $s0.martials -and $s.arts -eq $s0.arts -and $s.born -eq $s0.born -and $m -match '공법: 추가 0' -and $m -match '기예: 추가 0') $m

      # 14~15번, Review Focus 1·3·4: 손으로 고친 파일
      $bad = [ordered]@{ format = 1; kind = 'character'; name = 'uicheck-H'; exportedAt = '2026-10-13 00:00:00'; character = 'test'
        grade = [ordered]@{ id = 9999; name = 'no such grade'; exp = 1 }
        attributes = @([ordered]@{ key = 'nope'; name = 'unknown'; value = 1 }, [ordered]@{ key = 'attack'; name = 'too big'; value = 999999999 }, [ordered]@{ key = 'defense'; name = 'text'; value = 'many' })
        martials = @([ordered]@{ type = $mar.type; baseId = $mar.baseId; name = 'short'; grade = 1; exp = 0; equipped = $false; values = @(2) },
          [ordered]@{ type = 2; baseId = 2; name = 'nonsense'; grade = 0; exp = 0; equipped = $false; values = @(2, 2, 0, 1, 2, 3) },
          [ordered]@{ type = $mar.type; baseId = 987654321; name = 'unknown skill'; grade = 1; exp = 0; equipped = $false; values = @($mar.type, 987654321, 1) })
        arts = @([ordered]@{ kind = '연단'; id = 999999999; name = 'no book' }, [ordered]@{ kind = '없는 분류'; id = 1; name = 'no kind' })
        bornLucks = @([ordered]@{ id = 999999999; name = 'no luck' }); addLucks = @([ordered]@{ id = 999999999; name = 'no luck' })
        fates = @([ordered]@{ grade = 2; id = 999999999; name = 'no fate' }) }
      $capped = [ordered]@{ format = 1; kind = 'character'; name = 'uicheck-X'; exportedAt = '2026-10-13 00:00:01'; character = 'test'
        grade = [ordered]@{ id = [int]$s0.grade; name = 'same grade'; exp = 999999999 } }
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-H.json'), ($bad | ConvertTo-Json -Depth 6))
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-X.json'), ($capped | ConvertTo-Json -Depth 6))
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-broken.json'), '{ "format": 1, "kind": "character" "grade": ')
      Copy-Item -LiteralPath (Join-Path $bagDir 'uicheck-bagside.json') -Destination (Join-Path $dir 'uicheck-bagfile.json')
      Press 'Tab3' | Out-Null
      Cmd 'ui set CharQuery uicheck-H' | Out-Null
      $before = State
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'bad lines are refused one by one' ($s.grade -eq $before.grade -and $s.attack -eq $before.attack -and $s.defense -eq $before.defense -and $m -match '못 넣음') $m
      Check 'the result says why lines were refused' ($m -match '모르는 능력치' -and $m -match '게임에 없는') $m
      $last = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ -match '가져오기 전부\(uicheck-H\)' })[-1]
      Check 'a failed import is marked in the change log' ($last -match '재조회 불일치') "$last"
      Check 'a broken martial line leaves nothing behind' ($s.martials -eq $before.martials -and (Cmd 'cmartial 2' 200) -match 'n=0' -and (Cmd 'cmartial 987654321' 200) -match 'n=0') $s.raw
      Cmd 'ui set CharQuery uicheck-X' | Out-Null
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State
      Check 'exp is capped at the next step' ($s.grade -eq $s0.grade -and [int]$s.exp -le [int]$s.nextexp -and [int]$s.exp -ge [int]$s0.exp) $s.raw
      Cmd 'ui set CharQuery uicheck-b' | Out-Null
      $unreadable = 0; for ($i = 0; $i -lt 3; $i++) { if ((Cmd "uitext Row$i" 100) -match '읽을 수 없음') { $unreadable++ } }
      Check 'a bag file in the character folder is unreadable' ($unreadable -eq 2 -and (Cmd 'uipos CharAll0') -match 'no element named') "unreadable=$unreadable"

      # 16번: 목록을 본 뒤 지워진 파일
      Cmd 'ui set CharQuery uicheck-X' | Out-Null
      [IO.File]::Delete((Join-Path $dir 'uicheck-X.json'))
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      Check 'a file removed behind the list is reported' ((Msg) -match '파일이 없습니다') (Msg)
      Press 'CharQueryClear' | Out-Null

      # 18번: 변경 이력
      $hist = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Select-Object -Last 120) -join "`n"
      Check 'imports are in the change log' ($hist -match '캐릭터 가져오기')

      # 17번의 준비: 경지와 능력치와 공법을 바꾼 채 가져오고 저장한다. 다시 시작한 뒤에도 파일의 상태여야 한다
      Cmd 'setgrade 5' | Out-Null; Cmd "cattr attack $($atk + 5)" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200; if ($o -match 'sole=(\S+)') { Cmd "mdel $($Matches[1])" | Out-Null }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State
      Check 'the last import restores the exp too' ($s.exp -eq $s0.exp -and $s.grade -eq $s0.grade) $s.raw
      [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), "$($s.grade) $($s.exp) $($s.attack) $($s.martials) $($mar.baseId) $(@($mar.values) -join ',')")
      Press 'CharQueryClear' | Out-Null
      Shot 'charvault-list'
      Cmd 'ui set CharQuery uicheck-C' | Out-Null; Press 'CharOpen0' | Out-Null; Press 'CharSecMartials' | Out-Null; Shot 'charvault-martials'
      Press 'CharSecAttributes' | Out-Null; Shot 'charvault-attributes'
      Press 'Back' | Out-Null; Press 'CharQueryClear' | Out-Null
      Cmd 'uiopen PlayerInfo' 1500 | Out-Null; Shot 'charvault-playerinfo'; Cmd 'uiclose PlayerInfo' 1000 | Out-Null
      Cmd 'save' 5000 | Out-Null

      Press 'VaultModeBag' | Out-Null   # 다른 검사가 가방 쪽에서 시작하도록 돌려 둔다
      Press 'Close' | Out-Null
      Check 'no exception in button callbacks' ((Traps) -eq $traps)
      Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    } finally { CleanChar }
  }
  'charvault-restart' {
    $want = ([IO.File]::ReadAllText((Join-Path $work 'check-expect.txt'))) -split ' '
    $o = Cmd 'cstate' 200
    Check 'the imported grade, exp and attribute survive save and restart' ($o -match "grade=$($want[0]) exp=$($want[1]) " -and $o -match " attack=$($want[2]) " -and $o -match " martials=$($want[3]) ") $o
    $o = Cmd "cmartial $($want[4])" 200
    Check 'the imported martial survives with its options' ($o -match 'n=1 ' -and $o -match "values=\[$($want[5])\]") $o
    Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null; Press 'VaultModeChar' | Out-Null
    Cmd 'ui set CharQuery uicheck-' | Out-Null
    Check 'no test files are left' ((Cmd 'uitext CharListRange') -match "text='0개'")
    Press 'CharQueryClear' | Out-Null; Press 'Close' | Out-Null
  }
  'fateguard' {
    # 05 문서 31절. 역천개명 분류의 운명을 "운명 추가"로 붙여도 경지별 역천개명 기록은 그대로여야 한다.
    # 게임의 추가 행동이 그 번호를 지금 대경지의 기록에 덮어쓴다(30절의 실측).
    $before = if ((Cmd 'cstate' 200) -match 'fates=(\S+)') { $Matches[1] } else { '' }
    if ($before -eq 'none' -or $before -eq '') { 'INFO no fate record in this save; not exercised' }
    else {
      $o = Cmd 'cluck3' 400
      $after = if ((Cmd 'cstate' 200) -match 'fates=(\S+)') { $Matches[1] } else { '' }
      Check 'the luck was added' ($o -match 'has=True\s*$') $o
      Check 'adding a fate-class luck leaves the fate records alone' ($after -eq $before) "before=$before after=$after"
    }
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'order' {
    $traps = Traps
    Cmd 'editor' | Out-Null
    $s = Cmd 'uistate'
    Check 'opens in camera mode just above the map' ($s -match 'mode=ScreenSpaceCamera order=10 covered=False top=0 ') $s

    # 스펙 6절 2번: 나중에 연 게임 화면이 위에 뜨고, Insert가 편집기를 맨 앞으로 가져온다
    GameScreen open
    $s = Cmd 'uistate'
    Check 'a game screen opened later covers the editor' ($s -match 'covered=True top=20 ' -and -not (IsAt 'Tab0')) $s
    Key 0x2D
    $s = Cmd 'uistate'
    Check 'Insert brings a covered editor to the front' ($s -match 'open=True .*order=30 covered=False ' -and (IsAt 'Tab0')) $s

    # 스펙 6절 3번: 아래의 게임 화면이 닫히면 정렬을 낮춘다
    GameScreen close
    $s = Cmd 'uistate'
    Check 'order drops when the screen below closes' ($s -match 'order=10 covered=False top=0 ') $s
    GameScreen open
    Check 'the reopened screen is above the editor again' ((Cmd 'uistate') -match 'covered=True ' -and -not (IsAt 'Tab0'))
    GameScreen close
    Cmd 'uitoggle' | Out-Null   # 단축키와 같은 동작. 실제 키는 위에서 한 번 보냈다
    Check 'Insert closes an editor that is in front' ((Cmd 'uistate') -match 'open=False ')

    # 스펙 6절 4번: 확인 창은 더 높은 층이라 항상 위다. 가려진 것으로 치지 않는다
    Cmd 'editor' | Out-Null
    Cmd 'uiopen CheckPopup' 1200 | Out-Null
    Check 'confirm popup is above the editor' ((Rc 'hit 1037 531' 300) -match 'CheckPopup' -and -not (IsAt 'Tab0') -and (Cmd 'uistate') -match 'covered=False ')
    Cmd 'uiclose CheckPopup' 1000 | Out-Null
    Check 'editor is usable again' (IsAt 'Tab0')

    # Review Focus 4: Insert를 빠르게 여러 번, 가려진 상태와 섞어서
    1..6 | ForEach-Object { Cmd 'uitoggle' 100 | Out-Null }
    Check 'six quick toggles end where they started' ((Cmd 'uistate') -match 'open=True ')
    GameScreen open
    1..3 | ForEach-Object { Cmd 'uitoggle' 100 | Out-Null }   # 맨 앞으로 → 닫기 → 열기
    $s = Cmd 'uistate'
    Check 'toggles mixed with a covering screen stay consistent' ($s -match 'open=True .*order=30 covered=False ') $s
    GameScreen close
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'move' {
    # 창 위치는 세이브 폴더 밖의 config.txt에 남는다(회차를 되돌려도 남는다). 시험 전의 파일을 떠 두고 move-corrupt에서 되돌린다.
    $traps = Traps
    KeepConfig
    Cmd 'editor' | Out-Null
    $o = DragWindowTo 60 60
    Check 'the title bar takes the drag' ($o -match 'handler=TitleBar ') $o
    $w = WinState
    Check 'window is at the base position' ($w.x -eq 60 -and $w.y -eq 60) $w.raw
    $t = Pos 'Tab0'

    # 스펙 6절 5번: 끌면 따라 움직이고, 옮긴 자리에서 버튼이 눌린다
    DragWindowTo 260 160 | Out-Null
    $w = WinState; $t2 = Pos 'Tab0'
    Check 'window follows the drag' ($w.x -eq 260 -and $w.y -eq 160) $w.raw
    Check 'everything inside moved with it' ($t2.x -eq $t.x + 200 -and $t2.y -eq $t.y + 100 -and (IsAt 'Tab0')) "tab0 $($t.x),$($t.y) -> $($t2.x),$($t2.y)"
    Press 'Tab1' | Out-Null
    Check 'buttons work at the new place' ((Cmd 'uistate') -match 'tab=1 ')
    Check 'position is written to config.txt' ([bool]((Get-Content -LiteralPath (Join-Path $data 'config.txt') -Encoding UTF8) -match '^window=260,160$'))
    Check 'other config lines are kept' ([bool]((Get-Content -LiteralPath (Join-Path $data 'config.txt') -Encoding UTF8) -match '^hotkey='))

    # 화면 밖으로는 나가지 않는다
    DragWindowTo 5000 5000 | Out-Null
    $w = WinState
    Check 'stops at the right and bottom limits' ($w.x -eq 1720 -and $w.y -eq 1028) $w.raw
    DragWindowTo -5000 -5000 | Out-Null
    $w = WinState
    Check 'stops at the left and top limits' ($w.x -eq -1300 -and $w.y -eq 0) $w.raw
    DragWindowTo 260 160 | Out-Null

    # 스펙 6절 6번: 접기와 펼침
    Press 'Tab0' | Out-Null
    $t = Pos 'Tab0'
    Press 'Collapse' | Out-Null
    $w = WinState
    Check 'collapsed' ($w.collapsed -and (Cmd 'uitext Collapse') -match "text='펼침'") $w.raw
    Check 'the folded area lets clicks through to the game' ((Rc "hit $($t.x) $($t.y)" 200) -notmatch 'TlToyboxEditor')
    Check 'the title bar is still there' (IsAt 'Close')

    # Review Focus 3: 접힌 상태에서 끌기, 접은 채 닫았다가 다시 열기
    DragWindowTo 300 200 | Out-Null
    $w = WinState
    Check 'a collapsed window can be dragged' ($w.x -eq 300 -and $w.y -eq 200 -and $w.collapsed) $w.raw
    Press 'Collapse' | Out-Null
    $w = WinState; $t2 = Pos 'Tab0'
    Check 'expands again where it was dragged, on the same tab' (-not $w.collapsed -and $w.raw -match ' tab=0 ' -and $t2.x -eq $t.x + 40 -and $t2.y -eq $t.y + 40 -and (IsAt 'Tab0')) $w.raw
    Press 'Collapse' | Out-Null
    Press 'Close' | Out-Null
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'reopening shows it expanded at the same place' (-not $w.collapsed -and $w.x -eq 300 -and $w.y -eq 200 -and (IsAt 'Tab0')) $w.raw

    DragWindowTo 260 160 | Out-Null   # move-restart가 확인할 위치
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'move-restart' {
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'position survives a restart' ($w.x -eq 260 -and $w.y -eq 160) $w.raw
    Press 'Close' | Out-Null
    # Review Focus 1: 깨진 값과 화면 밖의 값으로 다시 시작할 준비
    $lines = @(Get-Content -LiteralPath (Join-Path $work 'check-config.txt') -Encoding UTF8 | Where-Object { $_ -notmatch '^window=' })
    [IO.File]::WriteAllLines((Join-Path $data 'config.txt'), [string[]]($lines + 'window=abc' + 'window=99999,-99999' + 'window=7'))
    'corrupt config written'
  }
  'move-corrupt' {
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'unreadable lines are skipped and an off-screen value is pulled inside' ($w.x -eq 1720 -and $w.y -eq 0) $w.raw
    # 이 자리에서는 제목 줄의 왼쪽 200만 화면 안에 있다. 그 부분이 잡히는지 본다.
    Check 'the visible part of the title bar can be grabbed' ((Rc 'hit 1870 26' 200) -match '/TitleBar\s*$')
    DragWindowTo 60 60 | Out-Null
    Check 'and dragged back' ((WinState).x -eq 60 -and (IsAt 'Close'))
    Press 'Close' | Out-Null
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    RestoreConfig
  }
  'rates' {
    # 배율 탭: 수련치와 이동. 스펙 2026-10-13-rate-tab-design.md 5절.
    # 깨끗한 회차에서 시작한다(패치가 하나도 걸려 있지 않은 것을 본다).
    $traps = Traps
    function RateState { Cmd 'rstate' 200 }
    function MoveDay { if ((Cmd 'wdate' 200) -match 'moveDay=([\d.]+)') { [double]$Matches[1] } else { throw 'wdate 출력을 읽지 못했습니다' } }
    function WorldDayNow { if ((Cmd 'wdate' 200) -match 'worldDay=(\d+)') { [int]$Matches[1] } else { throw 'wdate 출력을 읽지 못했습니다' } }
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab10' | Out-Null
    foreach ($b in 'RateExp', 'RateMartial', 'RateMove', 'RateSpeed', 'Multiplier', 'God', 'RateReset') { Check "$b is drawn" (IsAt $b) }
    $s = RateState
    Check 'nothing is patched at first' ($s -match 'exp=1 martial=1 move=1 speed=1 ' -and $s -notmatch 'patched') $s

    # 이동: x1의 한 칸을 재고, x0.5에서 절반인지 본다.
    $d0 = MoveDay; Cmd 'wmove 1 0' 900 | Out-Null; $plain = (MoveDay) - $d0
    Check 'a plain step costs something' ($plain -gt 0) "cost=$plain"
    Press 'RateMove' | Out-Null
    Check 'move shows x0.5' ((Cmd 'uitext RateMove') -match "text='x0.5'")
    $s = RateState
    Check 'only move is patched' ($s -match 'move=0.5\(patched\)' -and $s -match 'exp=1 martial=1 ') $s
    $d0 = MoveDay; Cmd 'wmove -1 0' 900 | Out-Null; $half = (MoveDay) - $d0
    $s = RateState
    Check 'a half step is recorded' ($s -match 'lastMove=([\d.]+)->([\d.]+)' -and [math]::Abs([double]$Matches[2] - [double]$Matches[1] * 0.5) -lt 0.0001) $s
    Check 'the half step costs half' ([math]::Abs($half - [double]$Matches[2]) -lt 0.0001) "half=$half"
    Start-Sleep -Milliseconds 900
    Check 'move is on the overlay' ((Cmd 'uistate') -match 'status=\[이동 x0.5\] ')

    # x0: 여러 칸을 걸어도 날짜와 쌓인 값이 그대로다. 걸으면서 수련할 수 있는 칸을 찾는다.
    1..3 | ForEach-Object { Press 'RateMove' | Out-Null }
    Check 'move shows x0' ((Cmd 'uitext RateMove') -match "text='x0'")
    Cmd 'setgrade 8' | Out-Null
    Press 'RateExp' | Out-Null; Press 'RateExp' | Out-Null
    Check 'exp shows x5' ((Cmd 'uitext RateExp') -match "text='x5'")
    $day0 = WorldDayNow; $d0 = MoveDay; $trained = $false
    for ($i = 0; $i -lt 10 -and -not $trained; $i++) {
      $o = Cmd 'wexp' 1500
      if ($o -match 'IsCreateAction=0 day=\d+ CreateAction=0 exp (\d+) -> (\d+)') {
        $trained = $true; $gain = [int]$Matches[2] - [int]$Matches[1]
        $s = RateState
        Check 'training is scaled by 5' ($s -match 'lastExp=(\d+)->(\d+)' -and [int]$Matches[2] -eq [int]$Matches[1] * 5) $s
        Check 'the scaled amount is what was gained' ($gain -eq [int]$Matches[2]) "gain=$gain"
      } elseif ((Cmd 'wmove 1 0' 900) -notmatch 'IsCreateAction=0 CreateAction=0') { break }
      else { Check "free step $i keeps the date" ((WorldDayNow) -eq $day0 -and [math]::Abs((MoveDay) - $d0) -lt 0.0001) }
    }
    Check 'a trainable tile was found' $trained

    # 플레이어가 아닌 단위와 음수.
    $o = Cmd 'rnpc' 300
    Check 'an NPC gains the plain amount' ($o -match 'npc exp (\d+) -> (\d+)' -and [int]$Matches[2] - [int]$Matches[1] -eq 10) $o
    $o = Cmd 'rneg' 300
    Check 'a negative amount is not scaled' ($o -match 'player AddExp\(-7\) exp (\d+) -> (\d+)' -and [int]$Matches[1] - [int]$Matches[2] -eq 7) $o

    # 날짜 고정과 함께.
    Check 'no freeze note while not frozen' ((Cmd 'uitext RateNoteMove') -notmatch '날짜 고정 중')
    Cmd 'tfreeze on' | Out-Null; Press 'Tab10' | Out-Null
    Check 'the freeze note is shown' ((Cmd 'uitext RateNoteMove') -match '날짜 고정 중')
    Cmd 'tfreeze off' | Out-Null; Press 'Tab10' | Out-Null

    # 한 바퀴와 되돌리기, 표시와 이력.
    Start-Sleep -Milliseconds 900
    Check 'both are on the overlay' ((Cmd 'uistate') -match 'status=\[수련 x5 · 이동 x0\] ')
    Press 'RateMove' | Out-Null
    Check 'move wraps to x1' ((Cmd 'uitext RateMove') -match "text='x1'")
    Press 'God' | Out-Null; Press 'Multiplier' | Out-Null
    Press 'RateReset' | Out-Null
    $s = RateState
    Check 'reset puts everything back' ($s -match 'exp=1\(patched\) martial=1 move=1\(patched\) speed=1 ' -and $s -match 'god=False damage=1') $s
    Start-Sleep -Milliseconds 900
    Check 'overlay is hidden after reset' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    $d0 = MoveDay; Cmd 'wmove -1 0' 900 | Out-Null
    Check 'steps cost again after reset' ((MoveDay) -ne $d0 -or (WorldDayNow) -gt $day0)
    Press 'Tab11' | Out-Null
    $lines = (Cmd 'uidescs HistoryLine' 300)
    Check 'changes are in the history' ($lines -match '배율' -and $lines -match '수련치 획득')
    Shot 'rates'
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'rates-battle' {
    # 배율 탭: 전투 중 공법 숙련과 전투 속도. 전투에 들어갔다가 끝내고 지도로 나온다.
    $traps = Traps
    function StepExp { if ((Cmd 'rmart' 200) -match 'step=(-?\d+)') { [int]$Matches[1] } else { throw 'rmart 출력을 읽지 못했습니다' } }
    function Scale { if ((Cmd 'rstate' 200) -match 'timeScale=([\d.]+)') { [double]$Matches[1] } else { throw 'rstate 출력을 읽지 못했습니다' } }
    Cmd 'editor' | Out-Null; Press 'Tab10' | Out-Null
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null
    Check 'speed shows x2' ((Cmd 'uitext RateSpeed') -match "text='x2'")
    Start-Sleep -Milliseconds 500
    Check 'speed is not applied outside battle' ((Scale) -eq 1) (Cmd 'rstate')
    Press 'Close' | Out-Null
    EnterBattle
    Check 'the battle scene is entered' ((Cmd 'uilist' 100) -match 'BattleInfo')
    Start-Sleep -Milliseconds 800
    Check 'speed is applied in battle' ((Scale) -eq 2) (Cmd 'rstate')
    Start-Sleep -Seconds 3
    Check 'speed holds after a few seconds' ((Scale) -eq 2) (Cmd 'rstate')

    # 숙련: 신법을 x1로 한 번, x5로 한 번 써서 오른 양을 견준다. 인자는 rstate의 lastMartial로 본다.
    $e0 = StepExp; Cmd 'bcast 3' 2500 | Out-Null; $plain = (StepExp) - $e0
    Check 'a plain use raises the mastery' ($plain -gt 0) "gain=$plain"
    Cmd 'editor' | Out-Null; Press 'Tab10' | Out-Null
    Check 'rate tab is drawn in battle' (IsAt 'RateMartial')
    Press 'RateMartial' | Out-Null; Press 'RateMartial' | Out-Null
    Check 'martial shows x5' ((Cmd 'uitext RateMartial') -match "text='x5'")
    Press 'Close' | Out-Null
    $e0 = StepExp; Cmd 'bcast 3' 2500 | Out-Null; $gain = (StepExp) - $e0
    $s = Cmd 'rstate' 200
    Check 'the use is scaled by 5' ($s -match 'lastMartial=(\d+)->(\d+)' -and [int]$Matches[2] -eq [int]$Matches[1] * 5) $s
    Check 'the mastery rose by a multiple of 5' ($gain -gt $plain -and $gain % 5 -eq 0) "plain=$plain gain=$gain"
    # AddMartialExpInBattle이 안에서 AddMartialExp를 부른다. 둘 다에 걸리면 오른 양이 제곱(25배)이 된다.
    Check 'the mastery rose by the scaled amount, not its square' ($s -match 'lastMartial=(\d+)->(\d+)' -and $gain -eq [int]$Matches[2]) "gain=$gain $s"

    # 일시 정지와 되돌리기.
    Cmd 'rpause on' 200 | Out-Null; Start-Sleep -Milliseconds 800
    Check 'a paused game stays paused' ((Scale) -eq 0) (Cmd 'rstate')
    Cmd 'rpause off' 200 | Out-Null; Start-Sleep -Milliseconds 800
    Check 'speed comes back after the pause' ((Scale) -eq 2) (Cmd 'rstate')
    Cmd 'editor' | Out-Null; Press 'Tab10' | Out-Null
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null
    Check 'speed shows x1' ((Cmd 'uitext RateSpeed') -match "text='x1'")
    Start-Sleep -Milliseconds 500
    Check 'speed returns to 1 when set back' ((Scale) -eq 1) (Cmd 'rstate')
    # 정지 중에 속도를 끄면, 정지가 풀리며 게임이 정지 전의 값을 되살려도 1이 된다(rpause off가 그 값을 되살린다).
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null
    Start-Sleep -Milliseconds 500
    Cmd 'rpause on' 200 | Out-Null
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null
    Check 'speed is turned off while paused' ((Cmd 'uitext RateSpeed') -match "text='x1'" -and (Scale) -eq 0) (Cmd 'rstate')
    Cmd 'rpause off' 200 | Out-Null; Start-Sleep -Milliseconds 800
    Check 'speed is 1 after resuming with speed off' ((Scale) -eq 1) (Cmd 'rstate')
    Press 'God' | Out-Null
    Check 'god is pressed in the rate tab' ((Cmd 'rstate' 200) -match 'god=True')
    # 전투를 끝내고 지도로 나온다(bend는 게임의 종료 절차를 부르고, 결과 창의 확인은 (959, 831)이다). 켜 둔 속도는 지도에서 걸리지 않는다.
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null; Press 'Close' | Out-Null
    Cmd 'bend' 3000 | Out-Null
    Check 'the result screen opens' ((Cmd 'uilist' 100) -match 'BattleEnd')
    Check 'speed holds on the result screen' ((Scale) -eq 2) (Cmd 'rstate')
    Rc 'click 959 831' 2000 | Out-Null
    for ($i = 0; $i -lt 20 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Start-Sleep -Milliseconds 500 }
    Check 'back on the map' ((Cmd 'uilist' 100) -match 'MapMain')
    Start-Sleep -Milliseconds 500
    Check 'speed is 1 after the battle' ((Scale) -eq 1 -and (Cmd 'rstate' 200) -match 'speed=2 ') (Cmd 'rstate')
    Cmd 'editor' | Out-Null; Press 'Tab10' | Out-Null
    Press 'RateReset' | Out-Null
    Shot 'rates-battle'
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'damage' {
    # 주는 피해의 배율이 실제 타격에 걸리는지. 지도에서 켜고 전투에 들어가는 경우와 전투 안에서 켜는 경우를 본다.
    # 사용자 보고(2026-10-13): 배율을 켜도 피해가 그대로였다.
    $traps = Traps
    function Dmg { Cmd 'bdmg' 200 }
    function Scaled { if ((Dmg) -match 'scaled=(\d+)') { [int]$Matches[1] } else { throw 'bdmg 출력을 읽지 못했습니다' } }
    # 적이 다가와 맞을 때까지 무예와 절기를 쓴다.
    function Fight([int]$want) {
      for ($i = 0; $i -lt 40 -and (Scaled) -lt $want; $i++) { Cmd 'bcast 1' 350 | Out-Null; if ($i % 5 -eq 4) { Cmd 'bcast 2' 350 | Out-Null } }
    }
    Cmd 'editor' | Out-Null; Press 'Tab10' | Out-Null
    1..3 | ForEach-Object { Press 'Multiplier' | Out-Null }
    Check 'damage shows x10' ((Cmd 'uitext Multiplier') -match "text='x10'")
    Press 'God' | Out-Null; Press 'Close' | Out-Null
    EnterBattle
    Rc 'click 956 749' 3000 | Out-Null
    Fight 1
    $s = Dmg
    Check 'set on the map: the hit event reaches the editor in battle' ($s -match 'hits=(\d+)' -and [int]$Matches[1] -gt 0) $s
    Check 'set on the map: a hit is scaled by 10' ($s -match 'last=base (\d+)->(\d+) value (\d+)->(\d+)' -and [int]$Matches[2] -eq [int]$Matches[1] * 10 -and [int]$Matches[4] -ge [int]$Matches[3] * 9) $s
    # 전투 안에서 끄고 다시 켠다.
    Cmd 'editor' | Out-Null; Press 'Tab10' | Out-Null; Press 'RateReset' | Out-Null
    Press 'God' | Out-Null; Press 'Multiplier' | Out-Null; Press 'Close' | Out-Null
    $before = Scaled; Fight ($before + 1)
    $s = Dmg
    Check 'set in battle: a hit is scaled by 2' ((Scaled) -gt $before -and $s -match 'last=base (\d+)->(\d+) ' -and [int]$Matches[2] -eq [int]$Matches[1] * 2) $s
    Cmd 'bend' 3000 | Out-Null; Rc 'click 959 831' 2000 | Out-Null
    for ($i = 0; $i -lt 20 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Start-Sleep -Milliseconds 500 }
    # 두 번째 전투: 켜 둔 채 다시 들어가도 걸린다.
    EnterBattle
    Rc 'click 956 749' 3000 | Out-Null
    $before = Scaled; Fight ($before + 1)
    Check 'kept on: a hit in the next battle is scaled' ((Scaled) -gt $before) (Dmg)
    Cmd 'bend' 3000 | Out-Null; Rc 'click 959 831' 2000 | Out-Null
    for ($i = 0; $i -lt 20 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Start-Sleep -Milliseconds 500 }
    Cmd 'editor' | Out-Null; Press 'Tab10' | Out-Null; Press 'RateReset' | Out-Null; Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'status' {
    # 스펙 6절 19번. 표시는 30프레임마다 맞춰지므로 켜고 끈 뒤 조금 기다린다.
    $traps = Traps
    Check 'hidden when nothing is on' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    Cmd 'tfreeze on' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'shown without ever opening the editor' ((Cmd 'uistate') -match 'status=\[날짜 고정\] ')
    Cmd 'editor' | Out-Null
    Press 'Tab10' | Out-Null; Press 'God' | Out-Null; Press 'Multiplier' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'all three are listed' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] ')
    Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'stays when the editor is closed' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] open=False ')
    Check 'the overlay does not take clicks' ((Rc 'hit 960 23' 200) -notmatch 'TlToybox')
    Shot 'status'
    # 끈다. 배율은 x2 → x5 → x10 → x100 → x1로 돈다.
    Cmd 'editor' | Out-Null
    Press 'Tab10' | Out-Null; Press 'God' | Out-Null
    1..4 | ForEach-Object { Press 'Multiplier' | Out-Null }
    Press 'Tab9' | Out-Null; Press 'Freeze' | Out-Null
    Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'hidden again when everything is off' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'status-battle' {
    # Review Focus 5. 회차의 마지막에 돌리고 바로 -End 한다(전투에서 나오지 않는다).
    Cmd 'editor' | Out-Null
    Press 'Tab10' | Out-Null; Press 'God' | Out-Null
    Press 'Close' | Out-Null
    EnterBattle
    Check 'the battle scene is entered' ((Cmd 'uilist' 100) -match 'BattleInfo')
    Start-Sleep -Milliseconds 900
    Check 'shown in battle' ((Cmd 'uistate') -match 'status=\[무적\] ')
    Shot 'status-battle'
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'battle' {
    # 스펙 6절 21번, Review Focus 5. 전투에서 나오는 방법을 자동화하지 않았으므로 회차의 마지막에 돌리고 바로 -End 한다.
    $traps = Traps
    EnterBattle
    Check 'the battle scene is entered' ((Cmd 'uilist' 100) -match 'BattleInfo')
    Check 'editor opens in battle' ((Cmd 'editor') -match 'open=True')
    "INFO battle state: $(Cmd 'uistate')"
    Check 'tabs are clickable in battle' (IsAt 'Tab10')
    Press 'Tab10' | Out-Null
    Check 'rate tab is drawn in battle' (IsAt 'God')
    Shot 'battle'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
