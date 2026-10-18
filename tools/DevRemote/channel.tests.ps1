# channel.ps1의 시험. 게임 없이 임시 폴더에서 돈다(05 문서 59절).
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'channel.ps1')
$script:fails = 0
function Check([string]$name, [bool]$ok, [string]$detail = '') { if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" } }
$dir = Join-Path ([IO.Path]::GetTempPath()) ('channel-test-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($dir)
try {
  $dst = Join-Path $dir 'mod-cmd.txt'
  $note = Send-ChannelCommand $dir 'first' 'mod-cmd'
  Check 'a command is moved into place' ((Test-Path $dst) -and [IO.File]::ReadAllText($dst) -eq 'first' -and $null -eq $note) "$note"
  Check 'no temp file is left' (-not (Test-Path (Join-Path $dir 'mod-cmd.tmp')))

  # 앞 명령을 "게임"이 0.3초 뒤에 가져가면 기다렸다가 옮긴다
  $job = Start-ThreadJob -ScriptBlock { param($p) Start-Sleep -Milliseconds 300; [IO.File]::Delete($p) } -ArgumentList $dst
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $note = Send-ChannelCommand $dir 'second' 'mod-cmd'
  $sw.Stop(); Wait-Job $job | Out-Null; Remove-Job $job
  Check 'it waits for the game to take the previous command' ($null -eq $note -and [IO.File]::ReadAllText($dst) -eq 'second' -and $sw.ElapsedMilliseconds -ge 250) "$note $($sw.ElapsedMilliseconds)ms"

  # 아무도 가져가지 않으면 5초 뒤 덮고 알린다
  $note = Send-ChannelCommand $dir 'third' 'mod-cmd'
  Check 'a stale command is replaced and reported' ($note -match 'stale' -and [IO.File]::ReadAllText($dst) -eq 'third') "$note"

  # 쓰는 중(배타 잠금)인 결과 파일: 읽기는 풀릴 때까지 다시 시도한다
  $out = Join-Path $dir 'mod-out.txt'
  [IO.File]::WriteAllText($out, 'result')
  $job = Start-ThreadJob -ScriptBlock { param($p) $fs = [IO.File]::Open($p, 'Open', 'ReadWrite', 'None'); Start-Sleep -Milliseconds 400; $fs.Close() } -ArgumentList $out
  Start-Sleep -Milliseconds 100
  $text = Read-ChannelFile $out
  Wait-Job $job | Out-Null; Remove-Job $job
  Check 'a locked result file is read after the lock goes' ($text -eq 'result') "$text"

  $job = Start-ThreadJob -ScriptBlock { param($p) $fs = [IO.File]::Open($p, 'Open', 'ReadWrite', 'None'); Start-Sleep -Milliseconds 400; $fs.Close() } -ArgumentList $out
  Start-Sleep -Milliseconds 100
  Remove-ChannelFile $out
  Wait-Job $job | Out-Null; Remove-Job $job
  Check 'a locked result file is deleted after the lock goes' (-not (Test-Path $out))
  Remove-ChannelFile $out
  Check 'deleting a missing file is fine' (-not (Test-Path $out))
} finally {
  [IO.Directory]::Delete($dir, $true)
}
"FAILS=$script:fails"
