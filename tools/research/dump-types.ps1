param([string]$Game, [string]$Out)
Add-Type -Path "$Game\MelonLoader\Mono.Cecil.dll"
$rp = New-Object Mono.Cecil.ReaderParameters
$rp.ReadWrite = $false
$sw = [IO.StreamWriter]::new($Out, $false, [Text.Encoding]::UTF8)
foreach ($name in 'Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','GameConfPack2.dll','GameConfPack3.dll') {
  $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$Game\MelonLoader\Managed\$name", $rp)
  $sw.WriteLine("ASSEMBLY $name")
  foreach ($t in $asm.MainModule.GetTypes()) {
    $base = if ($t.BaseType) { $t.BaseType.FullName } else { '' }
    $kind = if ($t.IsEnum) { 'ENUM' } else { 'TYPE' }
    $sw.WriteLine("$kind $($t.FullName) : $base")
    if ($t.IsEnum) {
      foreach ($f in $t.Fields) { if ($f.HasConstant) { $sw.WriteLine("  E $($f.Name) = $($f.Constant)") } }
      continue
    }
    foreach ($p in $t.Properties) { $sw.WriteLine("  P $($p.PropertyType.FullName) $($p.Name)") }
    foreach ($m in $t.Methods) {
      if ($m.IsGetter -or $m.IsSetter -or $m.IsConstructor) { continue }
      $ps = ($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', '
      $st = if ($m.IsStatic) { 'static ' } else { '' }
      $sw.WriteLine("  M $st$($m.ReturnType.FullName) $($m.Name)($ps)")
    }
  }
  $asm.Dispose()
}
$sw.Dispose()
