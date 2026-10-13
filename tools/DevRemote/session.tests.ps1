# session.ps1의 안전 규칙 시험. 게임을 띄우지 않고(-DryRun) 임시 폴더에서 한다. 실제 세이브와 게임은 건드리지 않는다.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다.
$ErrorActionPreference = 'Stop'
$session = Join-Path $PSScriptRoot 'session.ps1'
$script:fails = 0
$none = 'no-such-process-tltoybox'            # 실행 중인 게임이 없는 상황
$busy = (Get-Process -Id $PID).ProcessName    # 실행 중인 게임이 있는 상황(이 시험을 돌리는 프로세스의 이름을 빌린다)
$boxes = New-Object System.Collections.Generic.List[string]

function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
# 세이브·작업·모드 데이터 폴더를 흉내 낸 임시 폴더 한 벌
function NewBox {
  $root = Join-Path $env:TEMP ('tltoybox-session-tests-' + [guid]::NewGuid().ToString('N'))
  $box = [pscustomobject]@{ root = $root; save = "$root\save\CacheData"; work = "$root\work"; data = "$root\data" }
  foreach ($folder in "$($box.save)\slot1", $box.work, $box.data) { [void][IO.Directory]::CreateDirectory($folder) }
  [IO.File]::WriteAllText("$($box.save)\DataGloble.cache", 'global-1')
  [IO.File]::WriteAllText("$($box.save)\slot1\a.cache", 'a-1')
  $boxes.Add($root)
  $box
}
# session.ps1을 모의 실행한다. 던진 오류는 "THROWN: ..." 문자열로 돌려준다.
function Run($box, [string]$process, [hashtable]$more) {
  try {
    $out = & $session -DryRun -SaveDir $box.save -WorkDir $box.work -DataDir $box.data -ProcessName $process @more
    ($out -join "`n")
  } catch { 'THROWN: ' + $_.Exception.Message }
}
function Tree([string]$root) {
  (Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($root.Length) + '=' + [IO.File]::ReadAllText($_.FullName) }) -join ';'
}

# 1. 열린 회차 없이 -End: 거부하고 세이브를 건드리지 않는다
$b = NewBox
Copy-Item -LiteralPath $b.save -Destination "$($b.work)\save-backup-x" -Recurse
[IO.File]::WriteAllText("$($b.save)\slot1\a.cache", 'a-2-played-later')
$before = Tree $b.save
$o = Run $b $none @{ End = 'x' }
Check 'End without an open session is refused' ($o -match '^THROWN' -and (Tree $b.save) -eq $before) $o

# 2. 다른 이름의 회차가 열려 있을 때 -End: 거부
[IO.File]::WriteAllText("$($b.work)\session.name", 'y')
$o = Run $b $none @{ End = 'x' }
Check 'End with another name than the open session is refused' ($o -match '^THROWN' -and (Tree $b.save) -eq $before) $o

# 3. -Begin: 세이브 복사본, 회차 표식, bag-slots 보관
$b = NewBox
[IO.File]::WriteAllText("$($b.data)\bag-slots.txt", "p1=20`n")
$o = Run $b $none @{ Begin = 'x' }
$marked = (Test-Path "$($b.work)\session.name") -and [IO.File]::ReadAllText("$($b.work)\session.name") -eq 'x'
Check 'Begin copies the save, marks the session and keeps bag-slots' ($o -match 'DRY LAUNCH' -and (Tree "$($b.work)\save-backup-x") -eq (Tree $b.save) -and $marked -and (Test-Path "$($b.work)\session-bag-slots.txt")) $o

# 4. 열린 회차가 있는데 -Begin: 거부
$o = Run $b $none @{ Begin = 'z' }
Check 'Begin while a session is open is refused' ($o -match '^THROWN' -and -not (Test-Path "$($b.work)\save-backup-z")) $o

# 5. -End: 세이브와 bag-slots를 되돌리고, 되돌리기 직전의 세이브를 남기고, 표식을 지운다
$original = Tree $b.save
[IO.File]::WriteAllText("$($b.save)\slot1\a.cache", 'changed-by-the-test')
[IO.File]::WriteAllText("$($b.save)\slot1\extra.cache", 'new-file')
[IO.File]::WriteAllText("$($b.data)\bag-slots.txt", "garbage`n")
$mutated = Tree $b.save
$o = Run $b $none @{ End = 'x' }
Check 'End restores the save' ($o -match 'SAVE RESTORED AND IDENTICAL' -and (Tree $b.save) -eq $original) $o
Check 'End restores bag-slots' ([IO.File]::ReadAllText("$($b.data)\bag-slots.txt") -eq "p1=20`n") $o
Check 'End keeps the pre-restore save' ((Test-Path "$($b.work)\save-before-restore-x") -and (Tree "$($b.work)\save-before-restore-x") -eq $mutated)
Check 'End clears the session mark' ($marked -and -not (Test-Path "$($b.work)\session.name"))

# 6. -End를 한 번 더: 거부하고 세이브를 건드리지 않는다
[IO.File]::WriteAllText("$($b.save)\slot1\a.cache", 'played-after-the-session')
$before = Tree $b.save
$o = Run $b $none @{ End = 'x' }
Check 'End a second time is refused' ($o -match '^THROWN' -and (Tree $b.save) -eq $before) $o

# 7. 시작할 때 bag-slots가 없었으면 끝날 때 없앤다
$b = NewBox
[void](Run $b $none @{ Begin = 'x' })
[IO.File]::WriteAllText("$($b.data)\bag-slots.txt", "p1=500`n")
$o = Run $b $none @{ End = 'x' }
Check 'End removes a bag-slots file the session created' ($o -match 'SAVE RESTORED' -and -not (Test-Path "$($b.data)\bag-slots.txt")) $o

# 8. 열린 회차 없이 -Restart: 거부
$b = NewBox
$o = Run $b $none @{ Restart = $true }
Check 'Restart without an open session is refused' ($o -match '^THROWN') $o

# 9. 게임이 실행 중이면 -Begin을 거부하고 아무것도 남기지 않는다
$b = NewBox
$o = Run $b $busy @{ Begin = 'x' }
Check 'Begin while a game is running is refused' ($o -match '^THROWN' -and -not (Test-Path "$($b.work)\save-backup-x") -and -not (Test-Path "$($b.work)\session.name")) $o

# 10. 이 스크립트가 띄우지 않은 게임이 실행 중이면 -Restart가 띄우지도, pid를 적지도 않는다
$b = NewBox
[void](Run $b $none @{ Begin = 'x' })
$o = Run $b $busy @{ Restart = $true }
Check 'Restart never adopts a game it did not start' ($o -match '^THROWN' -and $o -notmatch 'DRY LAUNCH' -and -not (Test-Path "$($b.work)\session.pid")) $o

# 11. 이 스크립트가 띄우지 않은 게임이 실행 중이면 -End가 세이브를 되돌리지 않는다
[IO.File]::WriteAllText("$($b.save)\slot1\a.cache", 'the-other-game-is-playing')
$before = Tree $b.save
$o = Run $b $busy @{ End = 'x' }
Check 'End while a foreign game is running is refused' ($o -match '^THROWN' -and (Tree $b.save) -eq $before -and (Test-Path "$($b.work)\session.name")) $o

# 12. 없는 고정본을 주면 -Begin을 거부하고 아무것도 남기지 않는다
$b = NewBox
$o = Run $b $none @{ Begin = 'x'; Fixture = 'nope' }
Check 'Begin with a missing fixture is refused' ($o -match '^THROWN' -and -not (Test-Path "$($b.work)\save-backup-x") -and -not (Test-Path "$($b.work)\session.name")) $o

# 13. 고정본을 주면 회차 동안 세이브가 고정본의 내용이 되고, 사용자의 세이브는 복사본에 남는다
$b = NewBox
[void][IO.Directory]::CreateDirectory("$($b.work)\fixtures\base\slot1")
[IO.File]::WriteAllText("$($b.work)\fixtures\base\DataGloble.cache", 'fixture-global')
[IO.File]::WriteAllText("$($b.work)\fixtures\base\slot1\a.cache", 'fixture-a')
$mine = Tree $b.save
$o = Run $b $none @{ Begin = 'x'; Fixture = 'base' }
Check 'Begin with a fixture plays on the fixture' ($o -match 'FIXTURE LOADED' -and (Tree $b.save) -eq (Tree "$($b.work)\fixtures\base") -and (Tree "$($b.work)\save-backup-x") -eq $mine) $o

# 14. 고정본으로 놀다 끝내면 세이브는 회차 전의 사용자 것으로 돌아오고, 고정본은 바뀌지 않는다
[IO.File]::WriteAllText("$($b.save)\slot1\a.cache", 'fixture-a-changed-by-the-test')
$fixtureBefore = Tree "$($b.work)\fixtures\base"
$o = Run $b $none @{ End = 'x' }
Check 'End after a fixture restores the user save' ($o -match 'SAVE RESTORED AND IDENTICAL' -and (Tree $b.save) -eq $mine -and (Tree "$($b.work)\fixtures\base") -eq $fixtureBefore) $o

# 15. -End는 오래된 회차의 복사본을 지우고 최근 다섯 회차의 것만 남긴다(고정본은 건드리지 않는다)
$b = NewBox
[void][IO.Directory]::CreateDirectory("$($b.work)\fixtures\base")
[IO.File]::WriteAllText("$($b.work)\fixtures\base\keep.txt", 'fixture')
for ($n = 1; $n -le 7; $n++) {
  foreach ($kind in 'save-backup', 'save-before-restore') {
    $old = "$($b.work)\$kind-old$n"
    [void][IO.Directory]::CreateDirectory($old); [IO.File]::WriteAllText("$old\f.txt", "$n")
    (Get-Item -LiteralPath $old).LastWriteTime = (Get-Date).AddDays(-10).AddMinutes($n)
  }
}
[void](Run $b $none @{ Begin = 'x' })
$o = Run $b $none @{ End = 'x' }
$left = @(Get-ChildItem -LiteralPath $b.work -Directory | Where-Object { $_.Name -match '^save-(backup|before-restore)-' } | ForEach-Object Name | Sort-Object)
Check 'End keeps the copies of the five newest sessions only' ($o -match 'SAVE RESTORED' -and $left.Count -eq 10 -and ($left -contains 'save-backup-x') -and ($left -contains 'save-backup-old7') -and ($left -contains 'save-backup-old4') -and -not ($left -contains 'save-backup-old3') -and (Test-Path "$($b.work)\fixtures\base\keep.txt")) ($left -join ',')

# 16. -End는 회차 동안 모드가 만든 세이브 자동 백업을 지우고, 회차 전부터 있던 백업은 남긴다
$b = NewBox
[void][IO.Directory]::CreateDirectory("$($b.data)\backups\before-session"); [IO.File]::WriteAllText("$($b.data)\backups\before-session\s.txt", 'mine')
[void](Run $b $none @{ Begin = 'x' })
[void][IO.Directory]::CreateDirectory("$($b.data)\backups\made-by-the-test"); [IO.File]::WriteAllText("$($b.data)\backups\made-by-the-test\s.txt", 'test')
$o = Run $b $none @{ End = 'x' }
Check 'End removes the mod backups the session made' ($o -match 'SAVE RESTORED' -and (Test-Path "$($b.data)\backups\before-session\s.txt") -and -not (Test-Path "$($b.data)\backups\made-by-the-test")) $o
# 17. -End는 모드의 설정 파일(config.txt: 창 위치)을 회차 전의 내용으로 되돌린다. 창을 옮기는 검사가 중간에 끝나도 사용자의 창 위치가 남지 않게
$b = NewBox
[IO.File]::WriteAllText("$($b.data)\config.txt", 'hotkey=Insert')
[void](Run $b $none @{ Begin = 'x' })
[IO.File]::WriteAllText("$($b.data)\config.txt", "hotkey=Insert`nwindow=260,160")
[IO.File]::WriteAllText("$($b.work)\check-config.txt", 'left by a stopped check')
$o = Run $b $none @{ End = 'x' }
Check 'End restores config.txt' ($o -match 'SAVE RESTORED' -and [IO.File]::ReadAllText("$($b.data)\config.txt") -eq 'hotkey=Insert' -and -not (Test-Path "$($b.work)\check-config.txt")) $o
foreach ($root in $boxes) {
  if ($root.StartsWith((Join-Path $env:TEMP 'tltoybox-session-tests-')) -and (Test-Path $root)) { [IO.Directory]::Delete($root, $true) }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
exit 0
