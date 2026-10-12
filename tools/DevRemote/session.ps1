# 게임 검증 한 회차의 시작과 끝을 묶는다(docs/06-handoff.md 5절의 절차).
#   -Begin <이름>   게임이 꺼져 있는지 확인 → 빌드 → 세이브를 .devremote\save-backup-<이름>으로 복사해 검증
#                   → bag-slots.txt 보관 → 게임에 설치 → 화면 밖에서 실행해 1번 세이브 진입
#   -Restart        이 스크립트가 띄운 게임을 닫고 다시 띄운다(세이브와 설치된 DLL은 그대로). 저장 후 재시작 확인용
#   -End <이름>     이 스크립트가 띄운 게임을 닫고 → 세이브를 복사본으로 되돌려 일치를 확인
#                   → bag-slots.txt와 설치돼 있던 DLL을 되돌린다
#   -ReleaseBuild   -Begin과 함께 쓴다. 개발 도구를 뺀 배포 구성으로 빌드해 설치한다. 준비 로그가 없으므로 30초를 기다린다
# 게임은 달이 넘어갈 때 자동 저장하므로, 저장 명령을 쓰지 않은 회차도 -End로 되돌린다.
#
# 안전 규칙(session.tests.ps1이 확인한다):
#   - 회차는 한 번에 하나다. -Begin이 표식(session.name)을 남기고, -End와 -Restart는 그 표식이 있을 때만 동작한다.
#     -End의 이름이 열린 회차와 다르거나 회차가 없으면 세이브를 건드리지 않는다.
#   - 이 스크립트가 띄운 게임만 닫는다. 다른 게임이 실행 중이면 띄우지도, 닫지도, 세이브를 되돌리지도 않는다.
#   - 세이브를 되돌리기 직전의 상태를 save-before-restore-<이름>으로 한 벌 남긴다.
param([string]$Begin = '', [string]$End = '', [switch]$Restart, [switch]$ReleaseBuild,
  [string]$GameDir = 'E:\Games\Tale of Immortal', [string]$ModId = 'sWP6UL',
  # 아래는 session.tests.ps1이 쓰는 시험용 매개변수다. 평소에는 주지 않는다.
  [string]$SaveDir = '', [string]$WorkDir = '', [string]$DataDir = '', [string]$ProcessName = 'guigubahuang', [switch]$DryRun)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dir = if ($WorkDir) { $WorkDir } else { Join-Path $repo '.devremote' }
$save = if ($SaveDir) { $SaveDir } else { Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\Steam\CacheData' }
$data = if ($DataDir) { $DataDir } else { Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\TlToybox' }
$installed = Join-Path $GameDir "ModExportData\Mod_$ModId\ModCode\dll\MOD_$ModId.dll"
$bagFile = Join-Path $data 'bag-slots.txt'
$marker = Join-Path $dir 'session.name'              # 열려 있는 회차의 이름
$keptDll = Join-Path $dir 'session-installed.dll'    # 회차 전에 게임에 설치돼 있던 DLL
$keptBag = Join-Path $dir 'session-bag-slots.txt'    # 회차 전의 bag-slots.txt
$noBag = Join-Path $dir 'session-bag-slots.absent'   # 회차 전에 bag-slots.txt가 없었다는 표시
$pidFile = Join-Path $dir 'session.pid'
$modeFile = Join-Path $dir 'session.mode'
$flag = Join-Path $dir 'park.flag'

function Running { [bool](Get-Process $ProcessName -ErrorAction SilentlyContinue) }

# 이름을 Erase로 하면 PowerShell의 별칭(Remove-Item)이 먼저 걸린다.
function DeleteFiles([string[]]$files) {
  foreach ($file in $files) { if (Test-Path -LiteralPath $file) { [IO.File]::Delete($file) } }
}

function Launch {
  # 여기서 게임이 보이면 이 스크립트가 띄운 것이 아니다. 띄우지도, pid를 적지도 않는다.
  if (Running) { throw '게임이 실행 중입니다. 이 스크립트가 띄운 게임이 아니므로 건드리지 않습니다. 사용자에게 종료를 요청하고 기다립니다.' }
  if ($DryRun) { 'DRY LAUNCH'; return }
  [IO.File]::WriteAllText($flag, '')
  $blind = (Test-Path $modeFile) -and ([IO.File]::ReadAllText($modeFile).Trim() -eq 'release')
  $enter = Join-Path $PSScriptRoot 'enter.ps1'
  $lines = if ($blind) { & $enter -GameDir $GameDir -Launch -Direct -BlindWaitSeconds 30 } else { & $enter -GameDir $GameDir -Launch -Direct }
  $lines
  $game = Get-Process $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($game -and -not ($lines -match 'game already running')) { [IO.File]::WriteAllText($pidFile, [string]$game.Id) }
  if (-not ($lines -match 'WORLD READY|WAITED')) { throw '세이브 진입에 실패했습니다. -End로 정리한 뒤 원인을 확인합니다.' }
}

# 이 스크립트가 띄운 게임만 닫는다. 창 닫기 요청을 보내고 끝날 때까지 기다린다.
function CloseGame {
  if (-not (Test-Path $pidFile)) { return }
  $id = [int]([IO.File]::ReadAllText($pidFile).Trim())
  $game = Get-Process -Id $id -ErrorAction SilentlyContinue
  if ($game -and $game.ProcessName -eq $ProcessName) {
    [void]$game.CloseMainWindow()
    for ($i = 0; $i -lt 60 -and -not $game.HasExited; $i++) { Start-Sleep -Milliseconds 500; $game.Refresh() }
    if (-not $game.HasExited) { throw "게임이 닫히지 않았습니다(pid $id)." }
    $game.Dispose()
    # 창이 닫힌 뒤에도 같은 이름의 프로세스가 잠깐 목록에 남는다(두 번 관찰). 사라질 때까지 기다린다.
    for ($i = 0; $i -lt 30 -and (Running); $i++) { Start-Sleep -Milliseconds 500 }
  }
  [IO.File]::Delete($pidFile)
}

function Snapshot([string]$root) {
  @(Get-ChildItem -LiteralPath $root -Recurse -File |
    ForEach-Object { $_.FullName.Substring($root.Length) + ' ' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } |
    Sort-Object)
}

function Differences([string]$left, [string]$right) {
  @(Compare-Object (Snapshot $left) (Snapshot $right) | ForEach-Object { "$($_.SideIndicator) $($_.InputObject)" })
}

function OpenSession {
  if (Test-Path $marker) { [IO.File]::ReadAllText($marker).Trim() } else { '' }
}

if ($Begin) {
  if (Running) { throw '게임이 실행 중입니다. 사용자에게 종료를 요청하고 기다립니다(임의로 끄지 않는다).' }
  if (OpenSession) { throw "열려 있는 회차가 있습니다($(OpenSession)). 먼저 -End $(OpenSession) 으로 끝냅니다." }
  $bak = Join-Path $dir "save-backup-$Begin"
  if (Test-Path $bak) { throw "복사본이 이미 있습니다: $bak (다른 이름을 씁니다)" }
  if (Test-Path $keptDll) { throw "끝내지 않은 회차의 흔적이 있습니다($keptDll). 원인을 확인합니다." }
  $proj = Join-Path $repo 'src\ModCode\ModMain\ModMain.csproj'
  if ($DryRun) {
    $dll = $null
  } elseif ($ReleaseBuild) {
    $outDir = Join-Path $env:TEMP "tltoybox-release-$ModId"
    dotnet build $proj -c Release --nologo -p:DevTools=false -p:ModId=$ModId -o $outDir | Select-Object -Last 3
    $dll = Join-Path $outDir "MOD_$ModId.dll"
  } else {
    dotnet build $proj -c Release --nologo | Select-Object -Last 3
    $dll = Join-Path $repo "src\ModCode\ModMain\bin\Release\MOD_$ModId.dll"
  }
  if (-not $DryRun -and $LASTEXITCODE -ne 0) { throw '빌드에 실패했습니다.' }

  # 세이브를 복사하고, 복사본이 원본과 같은지 확인한 뒤에야 회차를 연다.
  Copy-Item -LiteralPath $save -Destination $bak -Recurse
  $diff = Differences $save $bak
  if ($diff.Count -gt 0) { $diff; throw "세이브 복사본이 원본과 다릅니다. 확인한 뒤 지우고 다시 시작합니다: $bak" }
  DeleteFiles $keptBag, $noBag
  if (Test-Path -LiteralPath $bagFile) { Copy-Item -LiteralPath $bagFile -Destination $keptBag } else { [IO.File]::WriteAllText($noBag, '') }
  if (-not $DryRun) {
    Copy-Item -LiteralPath $installed -Destination $keptDll
    Copy-Item -LiteralPath $dll -Destination $installed -Force
    [IO.File]::WriteAllText($modeFile, $(if ($ReleaseBuild) { 'release' } else { 'dev' }))
  }
  [IO.File]::WriteAllText($marker, $Begin)
  Launch
}

if ($Restart) {
  if (-not (OpenSession)) { throw '열려 있는 회차가 없습니다. -Begin으로 시작합니다.' }
  CloseGame
  Launch
}

if ($End) {
  $open = OpenSession
  if (-not $open) { throw '열려 있는 회차가 없습니다. 세이브를 건드리지 않았습니다.' }
  if ($open -ne $End) { throw "열려 있는 회차는 '$open'입니다. 세이브를 건드리지 않았습니다. -End $open 으로 끝냅니다." }
  $bak = Join-Path $dir "save-backup-$End"
  if (-not (Test-Path $bak)) { throw "복사본이 없습니다: $bak" }
  CloseGame
  if (Running) { throw '게임이 아직 실행 중입니다. 이 스크립트가 띄운 게임이 아니므로 닫지 않았고, 세이브도 되돌리지 않았습니다.' }
  DeleteFiles $flag, $modeFile

  # 되돌리기 직전의 세이브를 한 벌 남긴 뒤 복사본으로 되돌린다.
  $pre = Join-Path $dir "save-before-restore-$End"
  robocopy $save $pre /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "되돌리기 전의 세이브를 남기지 못했습니다(robocopy $LASTEXITCODE). 세이브를 건드리지 않았습니다." }
  robocopy $bak $save /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
  if ($LASTEXITCODE -ge 8) { throw "세이브 복원에 실패했습니다(robocopy $LASTEXITCODE). 되돌리기 전의 상태는 $pre 에 있습니다." }
  $diff = Differences $bak $save
  if ($diff.Count -gt 0) { $diff; throw "세이브가 복사본과 다릅니다. 되돌리기 전의 상태는 $pre 에 있습니다." }
  'SAVE RESTORED AND IDENTICAL'

  # 세이브 폴더 밖의 모드 파일(bag-slots.txt)을 회차 전 상태로 되돌린다.
  if (Test-Path $keptBag) {
    [void][IO.Directory]::CreateDirectory($data)
    Copy-Item -LiteralPath $keptBag -Destination $bagFile -Force
    'BAG SLOTS RESTORED'
  } elseif (Test-Path $noBag) {
    DeleteFiles $bagFile
    'BAG SLOTS RESTORED (there was no file)'
  }
  DeleteFiles $keptBag, $noBag
  if (Test-Path $keptDll) {
    Copy-Item -LiteralPath $keptDll -Destination $installed -Force
    [IO.File]::Delete($keptDll)
    'INSTALLED DLL RESTORED'
  }
  [IO.File]::Delete($marker)
}
$global:LASTEXITCODE = 0
exit 0
