# 모드의 개발용 명령 채널(DevCommands)에 명령을 보내고 결과를 출력한다.
param([Parameter(Mandatory)][string[]]$Cmd, [int]$WaitMs = 600)
$dir = 'E:\TlToybox\.devremote'
foreach ($c in $Cmd) {
  $out = Join-Path $dir 'mod-out.txt'
  if (Test-Path $out) { [IO.File]::Delete($out) }
  [IO.File]::WriteAllText((Join-Path $dir 'mod-cmd.tmp'), $c)
  [IO.File]::Move((Join-Path $dir 'mod-cmd.tmp'), (Join-Path $dir 'mod-cmd.txt'))
  for ($n = 0; $n -lt 80 -and -not (Test-Path $out); $n++) { [Threading.Thread]::Sleep(100) }
  if (Test-Path $out) { ([IO.File]::ReadAllText($out)).Trim() } else { "$c -> NO RESPONSE" }
  [Threading.Thread]::Sleep($WaitMs)
}
