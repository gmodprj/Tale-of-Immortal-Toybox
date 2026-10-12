param([string]$Game = 'E:\Games\Tale of Immortal', [string]$TypeRegex, [string]$MethodRegex = '^\.ctor$', [int]$Max = 80)
Add-Type -Path "$Game\MelonLoader\Mono.Cecil.dll"
$rp = New-Object Mono.Cecil.ReaderParameters
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$Game\MelonLoader\Managed\Assembly-CSharp.dll", $rp)
$n = 0
foreach ($t in $asm.MainModule.GetTypes()) {
  if ($t.FullName -notmatch $TypeRegex) { continue }
  foreach ($m in $t.Methods) {
    if ($m.Name -notmatch $MethodRegex) { continue }
    $ps = ($m.Parameters | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '
    if ($ps -match '^IntPtr ') { continue }
    $st = if ($m.IsStatic) { 'static ' } else { '' }
    "$($t.FullName).$($m.Name)($ps) : $st$($m.ReturnType.Name)"
    if (++$n -ge $Max) { return }
  }
}
