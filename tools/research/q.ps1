param([string[]]$Show = @(), [string]$Find = '', [int]$Max = 70, [string]$Member = '', [switch]$NoMethods)
$S = Split-Path -Parent $MyInvocation.MyCommand.Path
$lines = [IO.File]::ReadAllLines("$S\types.txt")
if ($Find) {
  $lines | Where-Object { $_ -match "^(TYPE|ENUM) " -and $_ -match $Find } | Select-Object -First $Max
}
foreach ($name in $Show) {
  $pat = "^(TYPE|ENUM) $([regex]::Escape($name)) : "
  $i = [Array]::FindIndex($lines, [Predicate[string]] { param($l) $l -match $pat })
  if ($i -lt 0) { "NOT FOUND $name"; continue }
  $out = New-Object System.Collections.Generic.List[string]
  for ($k = $i + 1; $k -lt $lines.Count -and $lines[$k] -notmatch '^(TYPE|ENUM|ASSEMBLY) '; $k++) {
    $l = $lines[$k]
    if ($l -match 'NativeFieldInfoPtr|NativeMethodInfoPtr|NativeClassPtr|Il2CppType$|_b__\d|^  M .* (get|set|add|remove)_') { continue }
    if ($NoMethods -and $l -match '^  M ') { continue }
    if ($Member -and $l -notmatch $Member) { continue }
    $l = $l -replace 'Il2CppSystem\.Collections\.Generic\.', '' -replace 'System\.', ''
    $out.Add($l)
  }
  "$($lines[$i])  [$($out.Count) shown-eligible]"
  $out | Select-Object -First $Max
}
