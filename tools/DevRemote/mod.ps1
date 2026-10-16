# 모드의 개발용 명령 채널(DevCommands)에 명령을 보내고 결과를 출력한다.
param([Parameter(Mandatory)][string[]]$Cmd, [int]$WaitMs = 600)
$dir = & (Join-Path $PSScriptRoot 'devremote-dir.ps1')   # .devremote 경로는 한 곳(05 문서 49절)
foreach ($c in $Cmd) {
  $out = Join-Path $dir 'mod-out.txt'
  if (Test-Path $out) { [IO.File]::Delete($out) }
  [IO.File]::WriteAllText((Join-Path $dir 'mod-cmd.tmp'), $c)
  [IO.File]::Move((Join-Path $dir 'mod-cmd.tmp'), (Join-Path $dir 'mod-cmd.txt'))
  for ($n = 0; $n -lt 80 -and -not (Test-Path $out); $n++) { [Threading.Thread]::Sleep(100) }
  if (Test-Path $out) {
    # 게임이 결과 파일을 아직 쓰는 중이면 읽기가 막힌다. 잠깐 뒤에 다시 읽는다.
    $text = $null
    for ($r = 0; $r -lt 10 -and $null -eq $text; $r++) { try { $text = [IO.File]::ReadAllText($out) } catch [IO.IOException] { [Threading.Thread]::Sleep(50) } }
    if ($null -eq $text) { "$c -> NO RESPONSE (result file stayed locked)" } else { $text.Trim() }
  } else { "$c -> NO RESPONSE" }
  [Threading.Thread]::Sleep($WaitMs)
}
