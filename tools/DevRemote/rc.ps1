param([string[]]$Cmd = @(), [int]$WaitMs = 1500, [double]$Scale = 0.5, [switch]$NoShot)
$dir = 'E:\TlToybox\.devremote'
foreach ($c in $Cmd) {
  $out = Join-Path $dir 'out.txt'
  if (Test-Path $out) { [IO.File]::Delete($out) }
  [IO.File]::WriteAllText((Join-Path $dir 'cmd.tmp'), $c)
  [IO.File]::Move((Join-Path $dir 'cmd.tmp'), (Join-Path $dir 'cmd.txt'))
  $n = 0
  while (-not (Test-Path $out) -and $n -lt 50) { [Threading.Thread]::Sleep(100); $n++ }
  if (Test-Path $out) { "[$c] " + ([IO.File]::ReadAllText($out)).Trim() } else { "[$c] NO RESPONSE" }
  [Threading.Thread]::Sleep($WaitMs)
}
if (-not $NoShot) { & "$PSScriptRoot\win.ps1" -Do shot -Scale $Scale }
