param([string[]]$Cmd = @(), [int]$WaitMs = 1500, [double]$Scale = 0.5, [switch]$NoShot)
$dir = & (Join-Path $PSScriptRoot 'devremote-dir.ps1')   # .devremote 경로는 한 곳(05 문서 49절)
. (Join-Path $PSScriptRoot 'channel.ps1')                # 파일 교환의 경합 막기(05 문서 59절)
foreach ($c in $Cmd) {
  $out = Join-Path $dir 'out.txt'
  Remove-ChannelFile $out
  $note = Send-ChannelCommand $dir $c 'cmd'
  $n = 0
  while (-not (Test-Path $out) -and $n -lt 50) { [Threading.Thread]::Sleep(100); $n++ }
  $prefix = if ($note) { "($note) " } else { '' }
  if (Test-Path $out) {
    # 게임이 결과 파일을 아직 쓰는 중이면 읽기가 막힌다(회차 rv-1018e에서 검사가 멈춤). 잠깐씩 다시 읽는다.
    $text = Read-ChannelFile $out
    if ($null -eq $text) { "$prefix[$c] NO RESPONSE (result file stayed locked)" } else { "$prefix[$c] " + $text.Trim() }
  } else { "$prefix[$c] NO RESPONSE" }
  [Threading.Thread]::Sleep($WaitMs)
}
if (-not $NoShot) { & "$PSScriptRoot\win.ps1" -Do shot -Scale $Scale }
