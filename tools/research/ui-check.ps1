# 편집기 화면의 게임 내 검증(docs/superpowers/specs/2026-10-13-ui-rework-design.md 6절).
# tools\DevRemote\session.ps1 -Begin으로 게임이 떠서 세계에 들어가 있어야 한다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

# 탭마다 그 탭에만 있는 버튼 하나. 탭이 제대로 그려졌는지 보는 데 쓴다.
$tabProbe = @('Restore', 'MoneyAdd', 'FillExp', 'SkillAddOpen', 'LuckAddOpen', 'ArtLearnAll', 'NpcRefresh', 'Freeze', 'BackupNow')
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
    Check 'skill add screen opens' (IsAt 'Back')
    Press 'Back' | Out-Null
    Check 'skill add screen closes' (IsAt 'SkillAddOpen')
    Press 'Tab4' | Out-Null; Press 'LuckAddOpen' | Out-Null
    Check 'luck picker opens' (IsAt 'Back')
    Press 'Back' | Out-Null; Press 'FateOpen' | Out-Null
    Check 'fate screen opens' (IsAt 'Back')
    Press 'Back' | Out-Null
    Check 'luck list is back' (IsAt 'LuckAddOpen')

    # 수량은 쓰는 곳마다 따로 기억한다 (스펙 6절 10번)
    Press 'Tab0' | Out-Null; Press 'attr100' | Out-Null
    Check 'attribute step is 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'Tab7' | Out-Null
    Check 'move step is still 10' ((Cmd 'uitext MoveUp') -match "text='y - 10'")
    Press 'move1000' | Out-Null
    Check 'move step is 1000' ((Cmd 'uitext MoveUp') -match "text='y - 1000'")
    Press 'Tab6' | Out-Null; Press 'NpcRefresh' | Out-Null; Press (Pos 'Pick*').name | Out-Null
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

    # 삭제 확인 대기는 편집 대상을 바꾸면 풀린다. 제목 줄의 버튼으로 어느 탭에서나 대상을 바꿀 수 있기 때문이다.
    Press 'Tab6' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab4' | Out-Null
    $rm = (Pos 'LuckRemove*').name; Press $rm | Out-Null
    Check 'removing an NPC luck asks first' ((Cmd "uitext $rm") -match "text='정말 삭제'")
    Press 'ToPlayer' | Out-Null
    Press 'Tab6' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab4' | Out-Null
    Check 'the pending confirm is gone after the target changed' ((Cmd "uitext $rm") -match "text='삭제'")
    Press 'ToPlayer' | Out-Null

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
    Press 'CurrencyOpen' | Out-Null
    Check 'the currency screen lists the other currencies' ((IsAt 'Back') -and (IsAt 'CurrencyAdd10011') -and (IsAt 'CurrencyAdd10041') -and (IsAt 'CurrencyAdd7000315') -and (IsAt 'CurrencyAdd2821101'))
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
    Press 'Back' | Out-Null
    Check 'back to the item list' (IsAt 'MoneyAdd')

    # 가방 칸 한 번에 지정 (회차의 -End가 bag-slots.txt를 되돌린다)
    function Extra { if ((Cmd 'bagstat') -match 'extra=(\d+)') { [int]$Matches[1] } else { -1 } }
    $e0 = Extra
    $want = if ($e0 -eq 120) { 135 } else { 125 }
    Cmd 'uifocus BagExtra' | Out-Null; Cmd "ui set BagExtra $want" | Out-Null; Key 0x0D
    Check 'Enter sets the extra slots, rounded down to tens' ((Extra) -eq $want - 5) "extra=$(Extra)"
    Cmd 'ui set BagExtra 501' | Out-Null; Press 'BagApply' | Out-Null
    Check 'the apply button works and the value is capped' ((Extra) -eq 500)
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
    Press 'Tab7' | Out-Null
    if ((Cmd 'tin') -notmatch 'input=(\d+)-(\d+)-(\d+) pending=False') { throw '날짜 입력 상태를 읽지 못했습니다.' }
    $day = [int]$Matches[3]; $other = if ($day -eq 12) { 13 } else { 12 }
    Cmd 'uifocus DateDay' | Out-Null; Cmd "ui set DateDay $other" | Out-Null; Key 0x0D
    Check 'Enter in a date box asks like the apply button' ((Cmd 'tin') -match "-$other pending=True" -and (Cmd 'uitext DateApply') -match "text='정말 변경'")
    Press 'Tab7' | Out-Null
    Check 'coming back to the tab cancels the date change' ((Cmd 'tin') -match "-$day pending=False")
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'people' {
    # NPC 탭의 목록과 즉시 검색.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab6' | Out-Null
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
    Press 'Tab8' | Out-Null
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
    Press 'Tab6' | Out-Null; Press 'NpcRefresh' | Out-Null; Press (Pos 'Pick*').name | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab4' | Out-Null
    $rm = (Pos 'LuckRemove*').name
    Press $rm | Out-Null
    Check 'the first press only asks' ((Cmd "uitext $rm") -match "text='정말 삭제'")
    Press 'Tab3' | Out-Null; Press 'Tab4' | Out-Null
    Check 'a tab change cancels it' ((Cmd "uitext $rm") -match "text='삭제'")
    Press $rm | Out-Null
    Press 'LuckAddOpen' | Out-Null
    Check 'a sub screen opens with one back button' ((IsAt 'Back') -and (Cmd 'uipos LuckAddOpen') -match 'no element named')
    Press 'Back' | Out-Null
    Check 'opening a sub screen cancels it' ((Cmd "uitext $rm") -match "text='삭제'")
    Press $rm | Out-Null
    Press 'ToPlayer' | Out-Null
    Press 'Tab6' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab4' | Out-Null
    Check 'a target change cancels it' ((Cmd "uitext $rm") -match "text='삭제'")

    # Review Focus 5: 하위 화면이 열린 채 대상을 바꾸거나 탭을 떠난다
    Press 'FateOpen' | Out-Null
    Check 'the fate screen opens' (IsAt 'Back')
    Press 'ToPlayer' | Out-Null
    Check 'a target change closes the sub screen' ((IsAt 'LuckAddOpen') -and (Cmd 'uistate') -match 'target=player ')
    Press 'LuckAddOpen' | Out-Null
    Cmd 'ui set LuckQuery zzzzzz' | Out-Null
    Check 'the picker search is immediate' ((Cmd 'uitext LuckPickRange') -match "text='0개'")
    Press 'Tab3' | Out-Null; Press 'Tab4' | Out-Null
    Check 'leaving the tab closes the sub screen' (IsAt 'LuckAddOpen')

    # 공법: 기술 추가 화면의 목록과 검색, 옵션 교체 화면
    Press 'Tab3' | Out-Null; Press 'SkillAddOpen' | Out-Null
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
    Press 'Tab5' | Out-Null
    $o = Cmd 'uitext ArtTableRange'
    Check 'the art list is a scrolling list' ($o -match '전체 (\d+)개') $o
    Cmd 'ui set ArtQuery zzzzzz' | Out-Null
    Check 'the art search is immediate' ((Cmd 'uitext ArtTableRange') -match "text='0개'")
    Press 'ArtQueryClear' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
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
  'status' {
    # 스펙 6절 19번. 표시는 30프레임마다 맞춰지므로 켜고 끈 뒤 조금 기다린다.
    $traps = Traps
    Check 'hidden when nothing is on' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    Cmd 'tfreeze on' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'shown without ever opening the editor' ((Cmd 'uistate') -match 'status=\[날짜 고정\] ')
    Cmd 'editor' | Out-Null
    Press 'Tab7' | Out-Null; Press 'God' | Out-Null; Press 'Multiplier' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'all three are listed' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] ')
    Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'stays when the editor is closed' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] open=False ')
    Check 'the overlay does not take clicks' ((Rc 'hit 960 23' 200) -notmatch 'TlToybox')
    Shot 'status'
    # 끈다. 배율은 x2 → x5 → x10 → x100 → x1로 돈다.
    Cmd 'editor' | Out-Null
    Press 'Tab7' | Out-Null; Press 'God' | Out-Null; Press 'Freeze' | Out-Null
    1..4 | ForEach-Object { Press 'Multiplier' | Out-Null }
    Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'hidden again when everything is off' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'status-battle' {
    # Review Focus 5. 회차의 마지막에 돌리고 바로 -End 한다(전투에서 나오지 않는다).
    Cmd 'editor' | Out-Null
    Press 'Tab7' | Out-Null; Press 'God' | Out-Null
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
    Check 'tabs are clickable in battle' (IsAt 'Tab7')
    Press 'Tab7' | Out-Null
    Check 'world tab is drawn in battle' (IsAt 'God')
    Shot 'battle'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
