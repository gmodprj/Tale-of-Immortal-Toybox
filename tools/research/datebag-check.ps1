# 날짜·가방 기능의 게임 내 검증(docs/superpowers/specs/2026-10-12-date-bag-design.md 6절).
# tools\DevRemote\session.ps1 -Begin으로 게임이 떠서 세계에 들어가 있어야 한다.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다. "SHOT <파일>"은 눈으로 확인할 캡처다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')
function State {
  $o = Cmd 'wdate'
  if ($o -notmatch 'worldDay=(\d+) date=(\d+)-(\d+)-(\d+) roundDay=(\d+)/(\d+) residueDay=(-?\d+)') { throw "wdate 출력을 읽지 못했습니다: $o" }
  $state = [pscustomobject]@{ day = [int]$Matches[1]; y = [int]$Matches[2]; m = [int]$Matches[3]; d = [int]$Matches[4]; round = [int]$Matches[5]; res = [int]$Matches[7]; running = $false }
  $state.running = $o -match 'running=True'
  $state
}
function Grid {
  $o = Cmd 'grid'
  if ($o -notmatch 'cur=(\d+) max=(\d+) base=(\d+) add=\[(.*?)\]') { throw "grid 출력을 읽지 못했습니다: $o" }
  [pscustomobject]@{ cur = [int]$Matches[1]; max = [int]$Matches[2]; add = $Matches[4] }
}
# 갈 수 있는 방향을 찾아 그 칸과 원래 칸을 오가며 $steps번 이동한다. 실제로 이동한 횟수를 돌려준다.
function Walk([int]$steps) {
  $done = 0
  foreach ($dir in @(1, 0), @(-1, 0), @(0, 1), @(0, -1)) {
    if ((Cmd "wmove $($dir[0]) $($dir[1])" 1500) -notmatch 'IsCreateAction=0 CreateAction=0') { continue }
    $done = 1; $sign = -1
    while ($done -lt $steps) {
      if ((Cmd "wmove $($dir[0] * $sign) $($dir[1] * $sign)" 1500) -notmatch 'IsCreateAction=0 CreateAction=0') { break }
      $done++; $sign = -$sign
    }
    break
  }
  $done
}
# 달 넘기기(월말 처리)가 끝나 지도 화면으로 돌아올 때까지 기다린다.
# 처리 도중에 NPC 대화가 떠서 대답을 기다리면 처리가 멈춘 채 있다(실측). 오른쪽 선택지(거절)나 "다음"을 눌러 넘긴다.
function WaitRun {
  $calm = 0
  for ($i = 0; $i -lt 90 -and $calm -lt 2; $i++) {
    if ((Rc 'hit 1170 679' 150) -match 'goOptionItem') { Rc 'click 1170 679' 1200 | Out-Null; $calm = 0; continue }
    if ((Rc 'hit 1042 47' 150) -match 'DramaDialogue') { Rc 'click 1042 47' 800 | Out-Null; $calm = 0; continue }
    if ((Rc 'hit 959 629' 150) -match 'MonthLogBig') { Rc 'click 959 629' 1200 | Out-Null; $calm = 0; continue }
    if ((State).running) { $calm = 0; Start-Sleep -Milliseconds 500; continue }
    $calm++
    Start-Sleep -Milliseconds 400
  }
}
# 변경 이력(changes.log)과 게임 로그는 세이브 폴더 밖이라 회차를 되돌려도 쌓인다. 이 실행에서 새로 생긴 것만 본다.
# 함수 이름을 History로 하면 PowerShell의 별칭(Get-History)이 먼저 걸린다.
function HistoryLines {
  $file = Join-Path $data 'changes.log'
  if (Test-Path -LiteralPath $file) { @(Get-Content -LiteralPath $file -Encoding UTF8) } else { @() }
}
$script:hist0 = @(HistoryLines).Count
function NewHistory { (@(HistoryLines) | Select-Object -Skip $script:hist0) -join "`n" }
function SaveCount { @(Select-String -LiteralPath $log -Pattern 'SaveData callback ok=True').Count }
# 세계 탭의 직접 지정 입력 칸과 확인 상태
function DateInput {
  $o = Cmd 'tin'
  if ($o -notmatch 'input=(\S*) pending=(\w+)') { throw "tin 출력을 읽지 못했습니다: $o" }
  [pscustomobject]@{ text = $Matches[1]; pending = ($Matches[2] -eq 'True'); raw = $o }
}

switch ($Part) {
  'base' {
    $s = State; $g = Grid
    Check 'date parts are consistent' ($s.round -eq $s.day % 31 -and $s.res -eq 30 - $s.round) "day=$($s.day) round=$($s.round) res=$($s.res)"
    Check 'grid is readable' ($g.max -gt 0) $g.add
    Check 'walking works' ((Walk 2) -eq 2)
  }
  'time' {
    $s0 = State
    Check 'tdesc' ((Cmd 'tdesc') -match "제 $($s0.y)년 $($s0.m)월 $($s0.d)일 \(누적 $($s0.day)일 · 이번 달 남은 날 $($s0.res)\)")

    # 직접 지정: 같은 달 10일로(이후 시험의 기준 상태)
    $o = Cmd "tset $($s0.y) $($s0.m) 10"; $s = State; $base = $s.day
    Check 'tset same month' ($s.y -eq $s0.y -and $s.m -eq $s0.m -and $s.d -eq 10 -and $s.round -eq 9 -and $s.res -eq 21) $o
    Check 'tset same date is refused' ((Cmd "tset $($s0.y) $($s0.m) 10") -match '이미')

    # 실제 진행
    $o = Cmd 'tadv 1'; $s = State
    Check 'tadv 1' ($s.day -eq $base + 1 -and $s.round -eq 10 -and $s.res -eq 20) $o
    $o = Cmd 'tadv 5'; $s = State
    Check 'tadv 5' ($s.day -eq $base + 6 -and $s.res -eq 15) $o
    $o = Cmd 'tadv 10'; $s = State
    Check 'tadv 10' ($s.day -eq $base + 16 -and $s.res -eq 5) $o
    $o = Cmd 'tadv 10'; $s = State
    Check 'tadv beyond the month stops at its end' ($s.day -eq $base + 21 -and $s.round -eq 30 -and $s.res -eq 0 -and $o -match '5일만') $o
    Check 'month-used-up banner is shown' ((Rc 'hit 1042 47' 300) -match 'goSkipMonth')
    $o = Cmd 'tadv 1'
    Check 'tadv refused with no days left' ($o -match '남은 날이 없습니다' -and (State).day -eq $base + 21) $o

    # 남은 날이 0인 달에서 날짜를 되돌리면 띠가 사라지고 다시 행동할 수 있다 (Review Focus 1)
    $o = Cmd "tset $($s0.y) $($s0.m) 10"; $s = State
    Check 'tset out of a used-up month' ($s.day -eq $base -and $s.res -eq 21) $o
    Check 'banner is gone' ((Rc 'hit 1042 47' 300) -match 'no hit')
    Check 'walking works again' ((Walk 2) -eq 2)
    Cmd 'tadv 21' | Out-Null
    Check 'banner comes back at the next month end' ((Rc 'hit 1042 47' 300) -match 'goSkipMonth')
    Cmd "tset $($s0.y) $($s0.m) 10" | Out-Null
    Check 'banner is gone again' ((Rc 'hit 1042 47' 300) -match 'no hit')

    # 범위 밖의 값
    foreach ($bad in '0 1 1', '1 13 1', '1 1 31', '1 0 5', '10000 1 1', '1 1 0') {
      $o = Cmd "tset $bad"
      Check "tset rejects $bad" ($o -match '연 1~9999') $o
    }
    Check 'rejected input changed nothing' ((State).day -eq $base)

    # 다른 해, 그리고 과거
    $o = Cmd "tset $($s0.y + 1) 3 20"; $s = State
    Check 'tset other year' ($s.y -eq $s0.y + 1 -and $s.m -eq 3 -and $s.d -eq 20 -and $s.round -eq 19 -and $s.res -eq 11) $o
    $o = Cmd "tset $($s0.y) 2 5"; $s = State
    Check 'tset into the past' ($s.y -eq $s0.y -and $s.m -eq 2 -and $s.d -eq 5 -and $s.round -eq 4 -and $s.res -eq 26) $o
    Shot 'time-hud'

    # 달 넘기기: 게임의 확인 창을 열고 "예"를 누른다
    $o = Cmd 'tskip'
    Check 'tskip opens the confirm popup' ($o -match 'opened' -and (Rc 'hit 884 531' 300) -match 'CheckPopup') $o
    Rc 'click 884 531' 100 | Out-Null
    $o = Cmd 'tadv 1' 100
    Check 'tadv is refused while the month is turning' ($o -match '넘기는 중' -and $o -match '게임 화면') $o
    WaitRun
    $s = State
    Check 'one clean month after a past date' ($s.y -eq $s0.y -and $s.m -eq 3 -and $s.d -eq 1 -and $s.round -eq 0 -and $s.res -eq 30) "day=$($s.day)"

    # 화면: 세계 탭. 직접 지정의 입력 칸은 고치지 않았으면 늘 현재 날짜를 보여 준다
    Cmd 'editor' | Out-Null
    Press 'Tab9' | Out-Null
    $s = State; $in = DateInput
    Check 'date inputs show the current date' ($in.text -eq "$($s.y)-$($s.m)-$($s.d)" -and -not $in.pending) $in.raw
    Check 'Day1 button is where expected' (IsAt 'Day1')
    $before = $s.day
    Press 'Day1' | Out-Null
    $s = State; $in = DateInput
    Check 'Day1 button advances one day' ($s.day -eq $before + 1)
    Check 'date inputs follow the date' ($in.text -eq "$($s.y)-$($s.m)-$($s.d)") $in.raw
    Cmd 'ui set DateDay' | Out-Null
    Press 'DateApply' | Out-Null
    Check 'empty day is rejected' ((Cmd 'tmsg') -match '숫자로 입력' -and (State).day -eq $before + 1)

    # 확인 상태는 날짜 상자의 다른 버튼을 누르거나 편집기를 닫으면 풀린다
    Cmd 'ui set DateDay 25' | Out-Null
    Press 'DateApply' | Out-Null
    Check 'first click only asks' ((Cmd 'tmsg') -match '정말 변경' -and (DateInput).pending -and (State).day -eq $before + 1)
    Press 'Day1' | Out-Null
    $s = State; $in = DateInput
    Check 'another date button cancels the pending confirm' (-not $in.pending -and $s.day -eq $before + 2 -and $in.text -eq "$($s.y)-$($s.m)-$($s.d)") $in.raw
    Cmd 'ui set DateDay 25' | Out-Null
    Press 'DateApply' | Out-Null
    Press 'Close' | Out-Null
    Cmd 'editor' | Out-Null
    $in = DateInput
    Check 'reopening the editor cancels the pending confirm' (-not $in.pending -and $in.text -eq "$($s.y)-$($s.m)-$($s.d)") $in.raw

    # 지금과 같은 날짜는 묻지 않고 알린다
    Cmd "ui set DateDay $($s.d)" | Out-Null
    Press 'DateApply' | Out-Null
    Check 'same date is reported, not asked' ((Cmd 'tmsg') -match '이미' -and -not (DateInput).pending)

    # 두 번 눌러 적용
    Cmd 'ui set DateDay 25' | Out-Null
    Press 'DateApply' | Out-Null
    Press 'DateApply' | Out-Null
    $s2 = State; $in = DateInput
    Check 'second click applies' ($s2.y -eq $s.y -and $s2.m -eq $s.m -and $s2.d -eq 25 -and $s2.res -eq 6)
    Check 'date inputs show the new date after applying' ($in.text -eq "$($s2.y)-$($s2.m)-25" -and -not $in.pending) $in.raw
    Shot 'time-editor'
    Press 'SkipMonth' | Out-Null
    $o = Rc 'hit 1037 531' 300   # 확인 창의 "아니오" 자리
    Check 'SkipMonth button closes the editor and opens the popup' ($o -match 'CheckPopup' -and $o -notmatch 'btn1') $o
    Rc 'click 1037 531' | Out-Null
    Check 'popup is closed by "no"' ((Rc 'hit 1037 531' 300) -notmatch 'CheckPopup')
    Check 'history has both kinds of entry' ((NewHistory) -match '세계  날짜  ' -and (NewHistory) -match '날짜\(직접 지정\)')

    # 저장 후 재시작 확인의 준비: 알아보기 쉬운 날짜로 바꾸고 저장한다
    Cmd "tset $($s0.y + 2) 5 15" | Out-Null
    [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), [string](State).day)
    $saves = SaveCount
    Cmd 'save' 5000 | Out-Null
    Check 'saved' ((SaveCount) -gt $saves)
  }
  'time-restart' {
    $want = [int][IO.File]::ReadAllText((Join-Path $work 'check-expect.txt'))
    $s = State
    Check 'date survives save and restart' ($s.day -eq $want -and $s.m -eq 5 -and $s.d -eq 15 -and $s.round -eq 14 -and $s.res -eq 16) "day=$($s.day) want=$want"
  }
  'freeze' {
    $s0 = State
    Cmd "tset $($s0.y) $($s0.m) 10" | Out-Null
    $s0 = State
    Check 'starts off' ((Cmd 'tdesc') -match 'frozen=False')
    Check 'freeze on' ((Cmd 'tfreeze on') -match '날짜 고정: 켜짐' -and (Cmd 'tdesc') -match 'frozen=True')

    # 고정 중의 행동: 날짜가 흐르지 않는다
    Check 'walked 6 while frozen' ((Walk 6) -eq 6)
    $s = State
    Check 'frozen moves keep the date' ($s.day -eq $s0.day -and $s.round -eq $s0.round -and $s.res -eq $s0.res) "day=$($s.day) res=$($s.res)"
    # 수련(5일짜리 행동)도 날짜를 쓰지 않는다. 수련할 수 있는 칸을 찾아 동쪽으로 최대 8칸 간다(고정 중이라 이동은 날짜를 쓰지 않는다).
    $trained = $false
    for ($i = 0; $i -lt 9 -and -not $trained; $i++) {
      $o = Cmd 'wexp' 1500
      if ($o -match 'IsCreateAction=0 day=(\d+) CreateAction=0 exp (\d+) -> (\d+)') {
        $gained = [int]$Matches[3] -gt [int]$Matches[2]; $after = State; $trained = $true
        Check 'frozen training does not use days' ($after.day -eq $s0.day -and $after.res -eq $s0.res) $o
        # 수련치가 다음 단계의 필요량까지 찬 인물은 수련해도 오르지 않는다. 그 경우는 알리기만 한다.
        if (-not $gained) { "INFO exp did not rise (already full?): $o" }
      } elseif ((Cmd 'wmove 1 0' 1500) -notmatch 'IsCreateAction=0 CreateAction=0') { break }
    }
    if (-not $trained) { "SKIP frozen training (no tile nearby where the game allows it): $o" }
    # 고정 중의 편집기 기능 (Review Focus 3)
    Check 'tadv refused while frozen' ((Cmd 'tadv 1') -match '고정 중' -and (State).day -eq $s0.day)
    Check 'tskip refused while frozen' ((Cmd 'tskip') -match '고정 중')
    $o = Cmd "tset $($s0.y) $($s0.m) 12"; $s = State
    Check 'tset works while frozen' ($s.d -eq 12 -and $s.round -eq 11 -and $s.res -eq 19) $o

    # 고정 중의 게임 달 넘기기 버튼: 알림만 뜨고 넘어가지 않는다
    Rc 'click 400 1000' 300 | Out-Null
    Shot 'freeze-tip'
    Check 'skip blocked: no confirm popup' ((Rc 'hit 884 531' 300) -notmatch 'CheckPopup')
    Check 'skip blocked: no banner' ((Rc 'hit 1042 47' 300) -match 'no hit')
    Check 'still can walk after the blocked skip' ((Walk 2) -eq 2)
    $s1 = State
    Check 'still the same date' ($s1.day -eq $s.day -and $s1.res -eq $s.res)

    # 화면: 고정 버튼
    Cmd 'editor' | Out-Null
    Press 'Tab9' | Out-Null
    Check 'Freeze button is where expected' (IsAt 'Freeze')
    Shot 'freeze-editor'
    Press 'Freeze' | Out-Null
    Check 'Freeze button turns it off' ((Cmd 'tdesc') -match 'frozen=False')
    Press 'Freeze' | Out-Null
    Check 'Freeze button turns it on' ((Cmd 'tdesc') -match 'frozen=True')
    Press 'Close' | Out-Null

    # 고정을 끄면 다시 흐르고, 달은 한 달만 넘어간다
    Check 'freeze off' ((Cmd 'tfreeze off') -match '날짜 고정: 꺼짐')
    $n = Walk 8; $s2 = State
    Check 'unfrozen moves advance the date' ($n -eq 8 -and $s2.day -gt $s1.day -and ($s2.day - $s1.day) -eq ($s1.res - $s2.res)) "day $($s1.day) -> $($s2.day), res $($s1.res) -> $($s2.res)"
    Rc 'click 400 1000' 1200 | Out-Null
    Check 'confirm popup is shown when not frozen' ((Rc 'hit 884 531' 300) -match 'CheckPopup')
    Rc 'click 884 531' 1500 | Out-Null
    WaitRun
    $s3 = State
    Check 'exactly one month passed' ($s3.d -eq 1 -and $s3.round -eq 0 -and $s3.res -eq 30 -and $s3.day -eq ([math]::Floor($s2.day / 31) + 1) * 31) "day $($s2.day) -> $($s3.day)"
    Check 'history has the toggles' ((NewHistory) -match '날짜 고정')
  }
  'bag' {
    # 주 화면에서부터 60프레임 점검이 돌았다. 경고가 없어야 한다 (Review Focus 5)
    Check 'no warning from the background tick' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    # 사용자의 세이브에 편집기 추가 칸이 이미 있을 수 있다. 0에서 시작하도록 걷어 낸다(회차의 -End가 파일을 되돌린다).
    if ((Cmd 'bagstat') -notmatch 'extra=(\d+)') { throw 'bagstat 출력을 읽지 못했습니다.' }
    if ([int]$Matches[1] -gt 0) { Cmd "bag -$($Matches[1])" | Out-Null }
    Cmd 'gridfill 600 5' 4000 | Out-Null   # 재료로 가방을 가득 채워 기준을 만든다
    $g0 = Grid
    Check 'no editor slots at start' ($g0.add -notmatch 'TlToybox') $g0.add
    Check 'bag is full' ((Cmd 'gridfill 1 5' 1500) -match 'given=0 ')
    Check 'minus at zero is refused' ((Cmd 'bag -10') -match '0~500 사이')

    $o = (Cmd 'bag 10') + ' | ' + (Cmd 'bag 10'); $g = Grid
    Check 'plus 20' ($g.max -eq $g0.max + 20 -and $g.add -match 'TlToybox=20') $o
    Check 'file remembers 20' ([bool]((Get-Content -LiteralPath (Join-Path $data 'bag-slots.txt') -Encoding UTF8) -match '=20$'))
    $o = Cmd 'gridfill 600 5' 4000; $g1 = Grid
    # 가방은 쓰는 칸이 용량보다 2 많아질 때까지 받는다(05 문서 25절). 추가 칸을 걷어 낸 탓에 이미 그보다 많이 들어 있었다면 그만큼 덜 들어간다.
    $fit = [Math]::Max(0, 20 - [Math]::Max(0, $g0.cur - ($g0.max + 2)))
    Check "exactly $fit more items fit" ($o -match "given=$fit " -and $g1.cur -eq $g0.cur + $fit) $o
    Rc 'click 120 860' 2500 | Out-Null
    Shot 'bag-screen'
    Rc 'click 1722 160' 1200 | Out-Null

    # 화면: 자원 탭
    Cmd 'editor' | Out-Null
    Press 'Tab1' | Out-Null
    Check 'BagPlus button is where expected' (IsAt 'BagPlus')
    Press 'BagPlus' | Out-Null
    Check 'BagPlus adds 10' ((Grid).max -eq $g0.max + 30)
    Press 'BagMinus' | Out-Null
    Check 'BagMinus removes 10' ((Grid).max -eq $g0.max + 20)
    Shot 'bag-editor'
    Press 'Close' | Out-Null
    Check 'history has the change' ((NewHistory) -match '가방 추가 칸')

    # 저장 후 재시작 확인의 준비
    [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), [string]($g0.max + 20))
    $saves = SaveCount
    Cmd 'save' 5000 | Out-Null
    Check 'saved' ((SaveCount) -gt $saves)
  }
  'bag-restart' {
    $want = [int][IO.File]::ReadAllText((Join-Path $work 'check-expect.txt'))
    Start-Sleep -Seconds 3
    $g = Grid
    Check 'slots are reapplied without opening the editor' ($g.max -eq $want -and $g.add -match 'TlToybox=20') "max=$($g.max) want=$want add=$($g.add)"
    $o = Cmd 'bag -10'; $g1 = Grid
    Check 'minus 10 keeps the items and warns' ($g1.max -eq $want - 10 -and $g1.cur -eq $g.cur -and $o -match '넘칩니다') $o
    $o = Cmd 'bag -10'; $g2 = Grid
    Check 'back to zero' ($g2.max -eq $want - 20 -and $g2.cur -eq $g.cur -and $g2.add -notmatch 'TlToybox') $o
    Check 'file has no entry' (-not ((Get-Content -LiteralPath (Join-Path $data 'bag-slots.txt') -Encoding UTF8) -match '^[^#].*='))

    # 깨진 파일로 다시 시작할 준비 (Review Focus 4)
    if ((Cmd 'bagstat') -notmatch 'id=(\S+)') { throw 'bagstat 출력을 읽지 못했습니다.' }
    $id = $Matches[1]
    [IO.File]::WriteAllLines((Join-Path $data 'bag-slots.txt'), @('garbage', '=5', 'x=abc', "$id=9999", 'other=-30'))
    'corrupt file written'
  }
  'bag-corrupt' {
    Start-Sleep -Seconds 3
    $g = Grid
    Check 'out-of-range value is clamped to 500' ($g.add -match 'TlToybox=500') $g.add
    Check 'no warning or error from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    Check 'plus at the limit is refused' ((Cmd 'bag 10') -match '0~500 사이')
    $o = Cmd 'bag -500'; $g1 = Grid
    Check 'back to zero from 500' ($g1.add -notmatch 'TlToybox' -and $g1.max -eq $g.max - 500) $o
    [IO.File]::Delete((Join-Path $data 'bag-slots.txt'))
    'bag-slots.txt deleted'
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
