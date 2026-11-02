# 배포 빌드(개발 도구 제외)를 만들어 게임의 로컬 모드 폴더에 설치한다.
# 게임 폴더(ModExportData)에 파일을 쓴다. 게임이 켜져 있으면 DLL이 잠겨 있으므로 먼저 끈다.
# 처음 한 번은 모드 에디터의 "게임 디버깅 시작"으로 만들어진 debug\Mod_<ID> 폴더가 있어야 한다(ModExportData.cache가 거기서 나온다).
param([string]$GameDir = 'E:\SteamLibrary\steamapps\common\鬼谷八荒', [string]$ModId = 'sWP6UL')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (Get-Process guigubahuang -ErrorAction SilentlyContinue) { throw '게임이 실행 중입니다. 종료한 뒤 다시 실행하세요.' }

$out = Join-Path $env:TEMP "tltoybox-release-$ModId"
dotnet build (Join-Path $repo 'src\ModCode\ModMain\ModMain.csproj') -c Release --nologo -p:DevTools=false -p:ModId=$ModId -o $out | Select-Object -Last 4
$dll = Join-Path $out "MOD_$ModId.dll"
if (-not (Test-Path $dll)) { throw "빌드 산출물이 없습니다: $dll" }

$target = Join-Path $GameDir "ModExportData\Mod_$ModId"
if (-not (Test-Path (Join-Path $target 'ModExportData.cache'))) {
  $exported = Join-Path $repo "ModProject_$ModId\debug\Mod_$ModId"
  if (-not (Test-Path (Join-Path $exported 'ModExportData.cache'))) { throw "내보낸 모드 폴더가 없습니다: $exported (모드 에디터에서 '게임 디버깅 시작'을 한 번 실행하세요)" }
  Copy-Item -LiteralPath $exported -Destination $target -Recurse -Force
}
$dllDir = Join-Path $target 'ModCode\dll'
New-Item -ItemType Directory -Force $dllDir | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $dllDir "MOD_$ModId.dll") -Force
Get-ChildItem -LiteralPath $target -Recurse -File | ForEach-Object { '{0}  {1} bytes  {2:yyyy-MM-dd HH:mm}' -f $_.FullName, $_.Length, $_.LastWriteTime }
# 설치본의 커밋 표지(ModMain.csproj의 StampCommit. Init 로그의 build=와 같다). 인계 1절에 적는다.
'installed build: ' + (Get-Item (Join-Path $dllDir "MOD_$ModId.dll")).VersionInfo.ProductVersion
