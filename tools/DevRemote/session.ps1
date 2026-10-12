# 게임 검증 한 회차의 시작과 끝을 묶는다(docs/06-handoff.md 5절의 절차).
#   -Begin <이름>   게임이 꺼져 있는지 확인 → 빌드 → 세이브를 .devremote\save-backup-<이름>으로 복사
#                   → 게임에 설치 → 화면 밖에서 실행해 1번 세이브 진입
#   -Restart        이 스크립트가 띄운 게임을 닫고 다시 띄운다(세이브와 설치된 DLL은 그대로). 저장 후 재시작 확인용
#   -End <이름>     이 스크립트가 띄운 게임을 닫고 → 세이브를 복사본으로 되돌려 일치를 확인 → 설치돼 있던 DLL을 되돌린다
#   -ReleaseBuild   -Begin과 함께 쓴다. 개발 도구를 뺀 배포 구성으로 빌드해 설치한다. 준비 로그가 없으므로 30초를 기다린다
# 게임은 달이 넘어갈 때 자동 저장하므로, 저장 명령을 쓰지 않은 회차도 -End로 되돌린다.
param([string]$Begin = '', [string]$End = '', [switch]$Restart, [switch]$ReleaseBuild,
  [string]$GameDir = 'E:\Games\Tale of Immortal', [string]$ModId = 'sWP6UL')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dir = Join-Path $repo '.devremote'
$save = Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\Steam\CacheData'
$installed = Join-Path $GameDir "ModExportData\Mod_$ModId\ModCode\dll\MOD_$ModId.dll"
$keptDll = Join-Path $dir 'session-installed.dll'
$pidFile = Join-Path $dir 'session.pid'
$modeFile = Join-Path $dir 'session.mode'
$flag = Join-Path $dir 'park.flag'

function Launch {
  [IO.File]::WriteAllText($flag, '')
  $blind = (Test-Path $modeFile) -and ([IO.File]::ReadAllText($modeFile).Trim() -eq 'release')
  $enter = Join-Path $PSScriptRoot 'enter.ps1'
  $lines = if ($blind) { & $enter -GameDir $GameDir -Launch -Direct -BlindWaitSeconds 30 } else { & $enter -GameDir $GameDir -Launch -Direct }
  $lines
  $game = Get-Process guigubahuang -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($game) { [IO.File]::WriteAllText($pidFile, [string]$game.Id) }
  if (-not ($lines -match 'WORLD READY|WAITED')) { throw '세이브 진입에 실패했습니다. -End로 정리한 뒤 원인을 확인합니다.' }
}

# 이 스크립트가 띄운 게임만 닫는다. 창 닫기 요청을 보내고 끝날 때까지 기다린다.
function CloseGame {
  if (-not (Test-Path $pidFile)) { return }
  $id = [int]([IO.File]::ReadAllText($pidFile).Trim())
  $game = Get-Process -Id $id -ErrorAction SilentlyContinue
  if ($game -and $game.ProcessName -eq 'guigubahuang') {
    [void]$game.CloseMainWindow()
    for ($i = 0; $i -lt 60 -and -not $game.HasExited; $i++) { Start-Sleep -Milliseconds 500; $game.Refresh() }
    if (-not $game.HasExited) { throw "게임이 닫히지 않았습니다(pid $id)." }
    $game.Dispose()
  }
  [IO.File]::Delete($pidFile)
  # 창이 닫힌 뒤에도 같은 이름의 프로세스가 잠깐 목록에 남는다(두 번 관찰). 사라질 때까지 기다린다.
  for ($i = 0; $i -lt 30 -and (Get-Process guigubahuang -ErrorAction SilentlyContinue); $i++) { Start-Sleep -Milliseconds 500 }
}

function Snapshot([string]$root) {
  Get-ChildItem -LiteralPath $root -Recurse -File |
    ForEach-Object { $_.FullName.Substring($root.Length) + ' ' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } |
    Sort-Object
}

if ($Begin) {
  if (Get-Process guigubahuang -ErrorAction SilentlyContinue) { throw '게임이 실행 중입니다. 사용자에게 종료를 요청하고 기다립니다(임의로 끄지 않는다).' }
  $bak = Join-Path $dir "save-backup-$Begin"
  if (Test-Path $bak) { throw "복사본이 이미 있습니다: $bak" }
  if (Test-Path $keptDll) { throw "끝내지 않은 회차가 있습니다($keptDll). 먼저 -End로 끝냅니다." }
  $proj = Join-Path $repo 'src\ModCode\ModMain\ModMain.csproj'
  if ($ReleaseBuild) {
    $outDir = Join-Path $env:TEMP "tltoybox-release-$ModId"
    dotnet build $proj -c Release --nologo -p:DevTools=false -p:ModId=$ModId -o $outDir | Select-Object -Last 3
    $dll = Join-Path $outDir "MOD_$ModId.dll"
  } else {
    dotnet build $proj -c Release --nologo | Select-Object -Last 3
    $dll = Join-Path $repo "src\ModCode\ModMain\bin\Release\MOD_$ModId.dll"
  }
  if ($LASTEXITCODE -ne 0) { throw '빌드에 실패했습니다.' }
  Copy-Item -LiteralPath $save -Destination $bak -Recurse
  Copy-Item -LiteralPath $installed -Destination $keptDll
  Copy-Item -LiteralPath $dll -Destination $installed -Force
  [IO.File]::WriteAllText($modeFile, $(if ($ReleaseBuild) { 'release' } else { 'dev' }))
  Launch
}

if ($Restart) {
  CloseGame
  Launch
}

if ($End) {
  $bak = Join-Path $dir "save-backup-$End"
  if (-not (Test-Path $bak)) { throw "복사본이 없습니다: $bak" }
  CloseGame
  if (Get-Process guigubahuang -ErrorAction SilentlyContinue) { throw '게임이 아직 실행 중입니다.' }
  foreach ($file in $flag, $modeFile) { if (Test-Path $file) { [IO.File]::Delete($file) } }
  robocopy $bak $save /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "세이브 복원에 실패했습니다(robocopy $LASTEXITCODE)." }
  $diff = Compare-Object (Snapshot $bak) (Snapshot $save)
  if ($diff) { $diff | ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" }; throw '세이브가 복사본과 다릅니다.' }
  'SAVE RESTORED AND IDENTICAL'
  if (Test-Path $keptDll) {
    Copy-Item -LiteralPath $keptDll -Destination $installed -Force
    [IO.File]::Delete($keptDll)
    'INSTALLED DLL RESTORED'
  }
}
exit 0
