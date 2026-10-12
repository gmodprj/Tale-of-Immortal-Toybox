# 날짜·가방 기능의 게임 내 검증(docs/superpowers/specs/2026-10-12-date-bag-design.md 6절).
# tools\DevRemote\session.ps1 -Begin으로 게임이 떠서 세계에 들어가 있어야 한다.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다. "SHOT <파일>"은 눈으로 확인할 캡처다.
param([Parameter(Mandatory)][string]$Part)
$ErrorActionPreference = 'Stop'
$tools = Join-Path (Split-Path -Parent $PSScriptRoot) 'DevRemote'
$work = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.devremote'
$data = Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\TlToybox'
$log = 'E:\Games\Tale of Immortal\MelonLoader\Latest.log'
$script:fails = 0

function Cmd([string]$c, [int]$wait = 600) { (& "$tools\mod.ps1" -Cmd $c -WaitMs $wait) -join "`n" }
function Rc([string]$c, [int]$wait = 800) { (& "$tools\rc.ps1" -Cmd $c -NoShot -WaitMs $wait) -join "`n" }
function Shot([string]$name) {
  $file = Join-Path $work "check-$name.png"
  & "$tools\win.ps1" -Do shot -Out $file -Scale 0.6 | Out-Null
  "SHOT $file"
}
function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
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
# 이름을 History로 하면 PowerShell의 별칭(Get-History)이 먼저 걸린다.
function ChangeHistory([int]$tail = 80) { (Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail $tail -Encoding UTF8) -join "`n" }

switch ($Part) {
  'base' {
    $s = State; $g = Grid
    Check 'date parts are consistent' ($s.round -eq $s.day % 31 -and $s.res -eq 30 - $s.round) "day=$($s.day) round=$($s.round) res=$($s.res)"
    Check 'grid is readable' ($g.max -gt 0 -and $g.add -notmatch 'TlToybox') $g.add
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
    Check 'tadv is refused while the month is turning' ($o -match '넘기는 중') $o
    WaitRun
    $s = State
    Check 'one clean month after a past date' ($s.y -eq $s0.y -and $s.m -eq 3 -and $s.d -eq 1 -and $s.round -eq 0 -and $s.res -eq 30) "day=$($s.day)"

    # 화면: 세계 탭
    Cmd 'editor' | Out-Null
    Rc 'click 151 550' | Out-Null
    Check 'Day1 button is where expected' ((Rc 'hit 415 542' 300) -match 'Day1')
    $before = (State).day
    Rc 'click 415 542' | Out-Null
    Check 'Day1 button advances one day' ((State).day -eq $before + 1)
    Cmd 'ui set DateDay' | Out-Null
    Rc 'click 799 590' | Out-Null
    Check 'empty day is rejected' ((Cmd 'tmsg') -match '숫자로 입력' -and (State).day -eq $before + 1)
    $s = State
    Cmd "ui set DateYear $($s.y)" | Out-Null; Cmd "ui set DateMonth $($s.m)" | Out-Null; Cmd 'ui set DateDay 25' | Out-Null
    Rc 'click 799 590' | Out-Null
    Check 'first click only asks' ((Cmd 'tmsg') -match '정말 변경' -and (State).d -eq $s.d)
    Rc 'click 799 590' | Out-Null
    $s2 = State
    Check 'second click applies' ($s2.y -eq $s.y -and $s2.m -eq $s.m -and $s2.d -eq 25 -and $s2.res -eq 6)
    Shot 'time-editor'
    Rc 'click 754 542' | Out-Null
    $o = Rc 'hit 1037 531' 300   # 확인 창의 "아니오" 자리
    Check 'SkipMonth button closes the editor and opens the popup' ($o -match 'CheckPopup' -and $o -notmatch 'btn1') $o
    Rc 'click 1037 531' | Out-Null
    Check 'popup is closed by "no"' ((Rc 'hit 1037 531' 300) -notmatch 'CheckPopup')
    Check 'history has both kinds of entry' ((ChangeHistory) -match '세계  날짜  ' -and (ChangeHistory) -match '날짜\(직접 지정\)')

    # 저장 후 재시작 확인의 준비: 알아보기 쉬운 날짜로 바꾸고 저장한다
    Cmd "tset $($s0.y + 2) 5 15" | Out-Null
    [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), [string](State).day)
    Cmd 'save' 5000 | Out-Null
    Check 'saved' ([bool](Select-String -LiteralPath $log -Pattern 'SaveData callback ok=True'))
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
    Rc 'click 151 550' | Out-Null
    Check 'Freeze button is where expected' ((Rc 'hit 366 494' 300) -match 'Freeze')
    Shot 'freeze-editor'
    Rc 'click 366 494' | Out-Null
    Check 'Freeze button turns it off' ((Cmd 'tdesc') -match 'frozen=False')
    Rc 'click 366 494' | Out-Null
    Check 'Freeze button turns it on' ((Cmd 'tdesc') -match 'frozen=True')
    Rc 'click 1502 90' | Out-Null

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
    Check 'history has the toggles' ((ChangeHistory) -match '날짜 고정')
  }
  default { throw "알 수 없는 Part: $Part" }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
exit 0
