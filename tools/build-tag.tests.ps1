# 배포 빌드에 커밋 표지가 들어가는지(InformationalVersion = <짧은 커밋>[-dirty]). 게임을 띄우지 않는다. 05 문서 49절.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다.
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$script:fails = 0
function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
$out = Join-Path $env:TEMP ('tltoybox-build-tag-' + [guid]::NewGuid().ToString('N'))
try {
  $commit = (& git -C $repo rev-parse --short HEAD).Trim()
  $dirty = if ((& git -C $repo status --porcelain) -join '') { '-dirty' } else { '' }
  dotnet build (Join-Path $repo 'src\ModCode\ModMain\ModMain.csproj') -c Release --nologo -p:DevTools=false -o $out | Out-Null
  $dll = Join-Path $out 'MOD_sWP6UL.dll'
  Check 'the release build exists' (Test-Path $dll) $dll
  $ver = (Get-Item $dll).VersionInfo.ProductVersion
  Check 'the release build carries the commit' ($ver -eq "$commit$dirty") "ProductVersion=$ver expected=$commit$dirty"
  Check 'the tag is a short hash' ($ver -match '^[0-9a-f]{7,}(-dirty)?$') $ver
} finally {
  if (Test-Path $out) { Remove-Item -LiteralPath $out -Recurse -Force }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
