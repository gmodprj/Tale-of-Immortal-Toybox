# 모드의 개발용 명령 채널(DevCommands)에 명령을 보내고 결과를 출력한다.
# -ReplyMs: 답을 기다리는 최대 시간(기본 8초). -WaitMs: 답을 받은 뒤 쉬는 시간(화면이 바뀌기를 기다릴 때).
# 프리셋 "최종 단계까지" 적용처럼 8초를 넘는 명령은 -ReplyMs를 늘린다(05 문서 70절: 14·15번이 12~17초).
param([Parameter(Mandatory)][string[]]$Cmd, [int]$WaitMs = 600, [int]$ReplyMs = 8000)
$dir = & (Join-Path $PSScriptRoot 'devremote-dir.ps1')   # .devremote 경로는 한 곳(05 문서 49절)
. (Join-Path $PSScriptRoot 'channel.ps1')                # 파일 교환의 경합 막기(05 문서 59절)
foreach ($c in $Cmd) {
  $out = Join-Path $dir 'mod-out.txt'
  Remove-ChannelFile $out
  $note = Send-ChannelCommand $dir $c 'mod-cmd'
  for ($n = 0; $n -lt [Math]::Max(1, [int]($ReplyMs / 100)) -and -not (Test-Path $out); $n++) { [Threading.Thread]::Sleep(100) }
  $prefix = if ($note) { "($note) " } else { '' }
  if (Test-Path $out) {
    $text = Read-ChannelFile $out
    if ($null -eq $text) { "$prefix$c -> NO RESPONSE (result file stayed locked)" } else { $prefix + $text.Trim() }
  } else { "$prefix$c -> NO RESPONSE" }
  [Threading.Thread]::Sleep($WaitMs)
}
