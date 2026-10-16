# 게임 검증 한 회차의 시작과 끝을 묶는다(docs/06-handoff.md 5절의 절차).
#   -Begin <이름>   게임이 꺼져 있는지 확인 → 빌드 → 세이브를 .devremote\save-backup-<이름>으로 복사해 검증
#                   → bag-slots.txt·favorites.txt 보관 → 게임에 설치 → 화면 밖에서 실행해 1번 세이브 진입
#   -Restart        이 스크립트가 띄운 게임을 닫고 다시 띄운다(세이브와 설치된 DLL은 그대로). 저장 후 재시작 확인용
#   -End <이름>     이 스크립트가 띄운 게임을 닫고 → 세이브를 복사본으로 되돌려 일치를 확인
#                   → bag-slots.txt, favorites.txt, config.txt, presets 폴더와 설치돼 있던 DLL을 되돌린다
#                   → 회차 동안 ModExportData에 생긴 폴더(창작마당 모드)를 지운다
#                   → translations 폴더(모드 글의 번역 파일)를 회차 전으로 되돌린다
#   -Fixture <이름> -Begin과 함께 쓴다. 사용자의 세이브를 복사해 둔 뒤, 회차 동안만 세이브 폴더를 고정본(.devremote\fixtures\<이름>)으로
#                   바꿔 띄운다. 검사가 사용자의 지금 세이브 상태(떠 있는 창, 진행 중인 사건)에 흔들리지 않게 한다. -End는 똑같이 사용자의 세이브를 되돌린다
#   -MakeFixture <이름> -From <폴더>   세이브 폴더의 복사본(예: .devremote\save-backup-<회차>)으로 고정본을 만든다
#   -ReleaseBuild   -Begin과 함께 쓴다. 개발 도구를 뺀 배포 구성으로 빌드해 설치한다. 준비 로그가 없으므로 30초를 기다린다
# 게임은 달이 넘어갈 때 자동 저장하므로, 저장 명령을 쓰지 않은 회차도 -End로 되돌린다.
#
# 안전 규칙(session.tests.ps1이 확인한다):
#   - 회차는 한 번에 하나다. -Begin이 표식(session.name)을 남기고, -End와 -Restart는 그 표식이 있을 때만 동작한다.
#     -End의 이름이 열린 회차와 다르거나 회차가 없으면 세이브를 건드리지 않는다.
#   - 이 스크립트가 띄운 게임만 닫는다. 다른 게임이 실행 중이면 띄우지도, 닫지도, 세이브를 되돌리지도 않는다.
#   - 세이브를 되돌리기 직전의 상태를 save-before-restore-<이름>으로 한 벌 남긴다.
#   - 고정본을 썼어도 -End 뒤의 세이브는 회차 전의 사용자 것이다. 고정본 자체는 바뀌지 않는다.
#   - -End는 최근 다섯 회차의 복사본만 남기고 오래된 것을 지운다. 회차 동안 모드가 만든 세이브 자동 백업도 지운다(회차 전부터 있던 것은 남긴다).
param([string]$Begin = '', [string]$End = '', [switch]$Restart, [switch]$ReleaseBuild, [string]$Fixture = '', [string]$MakeFixture = '', [string]$From = '',
  [string]$GameDir = 'E:\Games\Tale of Immortal', [string]$ModId = 'sWP6UL',
  # 아래는 session.tests.ps1이 쓰는 시험용 매개변수다. 평소에는 주지 않는다.
  [string]$SaveDir = '', [string]$WorkDir = '', [string]$DataDir = '', [string]$ProcessName = 'guigubahuang', [switch]$DryRun)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dir = if ($WorkDir) { $WorkDir } else { & (Join-Path $PSScriptRoot 'devremote-dir.ps1') }   # .devremote 경로는 한 곳(환경 변수 TLTOYBOX_DEVREMOTE, 기본 E:\TlToybox\.devremote. 05 문서 49절)
$save = if ($SaveDir) { $SaveDir } else { Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\Steam\CacheData' }
$data = if ($DataDir) { $DataDir } else { Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\TlToybox' }
$installed = Join-Path $GameDir "ModExportData\Mod_$ModId\ModCode\dll\MOD_$ModId.dll"
$bagFile = Join-Path $data 'bag-slots.txt'
$marker = Join-Path $dir 'session.name'              # 열려 있는 회차의 이름
$keptDll = Join-Path $dir 'session-installed.dll'    # 회차 전에 게임에 설치돼 있던 DLL
$keptBag = Join-Path $dir 'session-bag-slots.txt'    # 회차 전의 bag-slots.txt
$noBag = Join-Path $dir 'session-bag-slots.absent'   # 회차 전에 bag-slots.txt가 없었다는 표시
$favFile = Join-Path $data 'favorites.txt'
$keptFav = Join-Path $dir 'session-favorites.txt'          # 회차 전의 favorites.txt(자원 탭 즐겨찾기)
$noFav = Join-Path $dir 'session-favorites.absent'         # 회차 전에 favorites.txt가 없었다는 표시
$keptFavBak = Join-Path $dir 'session-favorites.bak'       # 회차 전의 favorites.txt.bak
$noFavBak = Join-Path $dir 'session-favorites-bak.absent'  # 회차 전에 favorites.txt.bak이 없었다는 표시
$pidFile = Join-Path $dir 'session.pid'
$modeFile = Join-Path $dir 'session.mode'
$flag = Join-Path $dir 'park.flag'
$fixtures = Join-Path $dir 'fixtures'
$backups = Join-Path $data 'backups'                 # 모드가 값을 바꾸기 전에 만드는 세이브 자동 백업
$keptBackups = Join-Path $dir 'session-backups.txt'  # 회차 전부터 있던 자동 백업의 이름들
$modsDir = Join-Path $GameDir 'ModExportData'            # 게임의 로컬 모드 폴더(편집기와 창작마당 모드)
$keptModFolders = Join-Path $dir 'session-modfolders.txt'  # 회차 전부터 ModExportData에 있던 폴더 이름들
$configFile = Join-Path $data 'config.txt'
$keptConfig = Join-Path $dir 'session-config.txt'      # 회차 전의 config.txt(창 위치 등)
$presetsDir = Join-Path $data 'presets'
$keptPresets = Join-Path $dir 'session-presets'          # 회차 전의 presets 폴더(프리셋 파일)
$noPresets = Join-Path $dir 'session-presets.absent'     # 회차 전에 presets 폴더가 없었다는 표시
$translationsDir = Join-Path $data 'translations'
$keptTranslations = Join-Path $dir 'session-translations'        # 회차 전의 translations 폴더(번역 파일)
$noTranslations = Join-Path $dir 'session-translations.absent'   # 회차 전에 translations 폴더가 없었다는 표시
$keepSessions = 5                                    # -End가 남기는 회차 복사본의 수

function Running { [bool](Get-Process $ProcessName -ErrorAction SilentlyContinue) }

# 이름을 Erase로 하면 PowerShell의 별칭(Remove-Item)이 먼저 걸린다.
function DeleteFiles([string[]]$files) {
  foreach ($file in $files) { if (Test-Path -LiteralPath $file) { [IO.File]::Delete($file) } }
}
# 세이브 폴더 밖의 모드 파일 하나를 회차 전 상태로 되돌리려고 보관한다. 없었으면 없었다는 표시를 남긴다.
function KeepFile([string]$file, [string]$kept, [string]$absent) {
  DeleteFiles $kept, $absent
  if (Test-Path -LiteralPath $file) { Copy-Item -LiteralPath $file -Destination $kept } else { [IO.File]::WriteAllText($absent, '') }
}
# KeepFile로 보관한 파일을 되돌린다. 회차 전에 없었으면 지운다.
function RestoreFile([string]$file, [string]$kept, [string]$absent) {
  if (Test-Path -LiteralPath $kept) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $file))
    Copy-Item -LiteralPath $kept -Destination $file -Force
  } elseif (Test-Path -LiteralPath $absent) {
    DeleteFiles $file
  }
  DeleteFiles $kept, $absent
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

if ($MakeFixture) {
  $target = Join-Path $fixtures $MakeFixture
  if (Test-Path $target) { throw "고정본이 이미 있습니다: $target" }
  if (-not $From -or -not (Test-Path -LiteralPath $From -PathType Container)) { throw "고정본의 원본 폴더가 없습니다: $From" }
  [void][IO.Directory]::CreateDirectory($fixtures)
  Copy-Item -LiteralPath $From -Destination $target -Recurse
  $diff = Differences $From $target
  if ($diff.Count -gt 0) { $diff; throw "고정본이 원본과 다릅니다: $target" }
  "FIXTURE MADE: $MakeFixture"
}

if ($Begin) {
  if (Running) { throw '게임이 실행 중입니다. 사용자에게 종료를 요청하고 기다립니다(임의로 끄지 않는다).' }
  $fixtureDir = if ($Fixture) { Join-Path $fixtures $Fixture } else { '' }
  if ($Fixture -and -not (Test-Path -LiteralPath $fixtureDir -PathType Container)) { throw "고정본이 없습니다: $fixtureDir" }
  if (OpenSession) { throw "열려 있는 회차가 있습니다($(OpenSession)). 먼저 -End $(OpenSession) 으로 끝냅니다." }
  $bak = Join-Path $dir "save-backup-$Begin"
  if (Test-Path $bak) { throw "복사본이 이미 있습니다: $bak (다른 이름을 씁니다)" }
  if (Test-Path $keptDll) { throw "끝내지 않은 회차의 흔적이 있습니다($keptDll). 원인을 확인합니다." }
  # 남아 있는 프리셋 보관본은 사용자의 프리셋의 유일한 사본일 수 있다(-End가 끝까지 가지 못했거나 다른 판의 스크립트로 끝낸 경우). 덮어쓰지 않는다.
  if ((Test-Path -LiteralPath $keptPresets) -or (Test-Path $noPresets)) { throw "끝내지 않은 회차의 프리셋 보관본이 있습니다($keptPresets 또는 $noPresets). presets 폴더와 비교해 되돌린 뒤 지우고 다시 시작합니다." }
  if ((Test-Path -LiteralPath $keptTranslations) -or (Test-Path $noTranslations)) { throw "끝내지 않은 회차의 번역 보관본이 있습니다($keptTranslations 또는 $noTranslations). translations 폴더와 비교해 되돌린 뒤 지우고 다시 시작합니다." }
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
  KeepFile $favFile $keptFav $noFav
  KeepFile "$favFile.bak" $keptFavBak $noFavBak
  if (-not $DryRun) {
    Copy-Item -LiteralPath $installed -Destination $keptDll
    Copy-Item -LiteralPath $dll -Destination $installed -Force
    [IO.File]::WriteAllText($modeFile, $(if ($ReleaseBuild) { 'release' } else { 'dev' }))
  }
  DeleteFiles $keptConfig
  if (Test-Path -LiteralPath $configFile) { Copy-Item -LiteralPath $configFile -Destination $keptConfig }
  # 프리셋 폴더는 세이브 밖에 있다. 검사(preset-check.ps1)가 폴더를 지우고 다시 만들므로 회차 전의 것을 떠 둔다.
  if (Test-Path -LiteralPath $presetsDir) { Copy-Item -LiteralPath $presetsDir -Destination $keptPresets -Recurse } else { [IO.File]::WriteAllText($noPresets, '') }
  # 번역 폴더도 세이브 밖에 있다. 검사(ui-check.ps1의 translation)가 파일을 지우고 바꾸므로 회차 전의 것을 떠 둔다.
  if (Test-Path -LiteralPath $translationsDir) { Copy-Item -LiteralPath $translationsDir -Destination $keptTranslations -Recurse } else { [IO.File]::WriteAllText($noTranslations, '') }
  # 회차 전부터 있던 자동 백업의 이름을 적어 둔다. -End가 그 밖의 것(회차 동안 생긴 것)만 지운다.
  $names = @(if (Test-Path -LiteralPath $backups) { Get-ChildItem -LiteralPath $backups | ForEach-Object Name })
  [IO.File]::WriteAllText($keptBackups, ($names -join "`n"))
  # 회차 전부터 게임의 로컬 모드 폴더에 있던 폴더 이름을 적어 둔다. -End가 그 밖의 것(회차 동안 설치한 창작마당 모드)만 지운다.
  $modNames = @(if (Test-Path -LiteralPath $modsDir) { Get-ChildItem -LiteralPath $modsDir -Directory | ForEach-Object Name })
  [IO.File]::WriteAllText($keptModFolders, ($modNames -join "`n"))
  [IO.File]::WriteAllText($marker, $Begin)
  if ($Fixture) {
    # 표식을 남긴 뒤에 바꾼다. 여기서 멈춰도 -End가 사용자의 세이브를 복사본으로 되돌린다.
    robocopy $fixtureDir $save /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "고정본을 세이브 폴더에 넣지 못했습니다(robocopy $LASTEXITCODE). -End $Begin 으로 세이브를 되돌립니다." }
    $diff = Differences $fixtureDir $save
    if ($diff.Count -gt 0) { $diff; throw "세이브 폴더가 고정본과 다릅니다. -End $Begin 으로 세이브를 되돌립니다." }
    "FIXTURE LOADED: $Fixture"
  }
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
  # 자원 탭의 즐겨찾기 파일을 회차 전으로 되돌린다. 검사가 쓰다 남긴 임시 파일도 지운다.
  if ((Test-Path $keptFav) -or (Test-Path $noFav)) {
    RestoreFile $favFile $keptFav $noFav
    RestoreFile "$favFile.bak" $keptFavBak $noFavBak
    DeleteFiles "$favFile.tmp"
    'FAVORITES RESTORED'
  }
  if (Test-Path $keptDll) {
    Copy-Item -LiteralPath $keptDll -Destination $installed -Force
    [IO.File]::Delete($keptDll)
    'INSTALLED DLL RESTORED'
  }
  # 창 위치 등 모드의 설정을 회차 전으로 되돌린다. 창을 옮기는 검사(ui-check.ps1의 move)가 중간에 끝나도 사용자의 창 위치가 바뀐 채 남지 않는다.
  if (Test-Path $keptConfig) {
    Copy-Item -LiteralPath $keptConfig -Destination $configFile -Force
    DeleteFiles $keptConfig, (Join-Path $dir 'check-config.txt')
    'CONFIG RESTORED'
  }
  # 프리셋 폴더를 회차 전으로 되돌린다. 회차 전에 없었으면 지운다.
  if (Test-Path -LiteralPath $keptPresets) {
    robocopy $keptPresets $presetsDir /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "프리셋 폴더 복원에 실패했습니다(robocopy $LASTEXITCODE). 회차 전의 것은 $keptPresets 에 있습니다." }
    [IO.Directory]::Delete($keptPresets, $true)
    'PRESETS RESTORED'
  } elseif (Test-Path $noPresets) {
    if ((Test-Path -LiteralPath $presetsDir) -and $presetsDir.StartsWith($data + '\')) { [IO.Directory]::Delete($presetsDir, $true) }
    'PRESETS RESTORED (there was no folder)'
  }
  DeleteFiles $noPresets
  # 번역 폴더를 회차 전으로 되돌린다. 회차 전에 없었으면 지운다.
  if (Test-Path -LiteralPath $keptTranslations) {
    robocopy $keptTranslations $translationsDir /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "번역 폴더 복원에 실패했습니다(robocopy $LASTEXITCODE). 회차 전의 것은 $keptTranslations 에 있습니다." }
    [IO.Directory]::Delete($keptTranslations, $true)
    'TRANSLATIONS RESTORED'
  } elseif (Test-Path $noTranslations) {
    if ((Test-Path -LiteralPath $translationsDir) -and $translationsDir.StartsWith($data + '\')) { [IO.Directory]::Delete($translationsDir, $true) }
    'TRANSLATIONS RESTORED (there was no folder)'
  }
  DeleteFiles $noTranslations
  # 회차 동안 ModExportData에 생긴 폴더(회차 안에서 설치한 창작마당 모드)를 지운다. 회차 전부터 있던 것과 편집기(Mod_<ModId>)는 남긴다.
  if (Test-Path $keptModFolders) {
    $keepMods = @([IO.File]::ReadAllText($keptModFolders) -split "`n" | Where-Object { $_ })
    $removedMods = 0
    if (Test-Path -LiteralPath $modsDir) {
      foreach ($entry in Get-ChildItem -LiteralPath $modsDir -Directory) {
        if ($keepMods -notcontains $entry.Name -and $entry.Name -ne "Mod_$ModId" -and $entry.FullName.StartsWith($modsDir + '\')) {
          [IO.Directory]::Delete($entry.FullName, $true)
          $removedMods++
        }
      }
    }
    [IO.File]::Delete($keptModFolders)
    "WORKSHOP FOLDERS REMOVED: $removedMods"
  }

  [IO.File]::Delete($marker)

  # 회차 동안 모드가 만든 세이브 자동 백업을 지운다(시험 중의 세이브를 뜬 것이라 쓸모가 없다). 회차 전부터 있던 것은 남긴다.
  if (Test-Path $keptBackups) {
    $keep = @([IO.File]::ReadAllText($keptBackups) -split "`n" | Where-Object { $_ })
    if (Test-Path -LiteralPath $backups) {
      foreach ($entry in Get-ChildItem -LiteralPath $backups) {
        if ($keep -notcontains $entry.Name -and $entry.FullName.StartsWith($backups + '\')) {
          if ($entry.PSIsContainer) { [IO.Directory]::Delete($entry.FullName, $true) } else { [IO.File]::Delete($entry.FullName) }
        }
      }
    }
    [IO.File]::Delete($keptBackups)
  }

  # 오래된 회차의 복사본을 지운다. 회차마다 save-backup-<이름>과 save-before-restore-<이름> 한 쌍이고, 최근 것만 남긴다.
  $old = @(Get-ChildItem -LiteralPath $dir -Directory | Where-Object { $_.Name -match '^save-backup-' } |
    Sort-Object LastWriteTime -Descending | Select-Object -Skip $keepSessions)
  foreach ($copy in $old) {
    $name = $copy.Name.Substring('save-backup-'.Length)
    foreach ($path in $copy.FullName, (Join-Path $dir "save-before-restore-$name")) {
      if ((Test-Path -LiteralPath $path) -and $path.StartsWith($dir + '\save-')) { [IO.Directory]::Delete($path, $true) }
    }
  }
  if ($old.Count -gt 0) { "OLD COPIES REMOVED: $($old.Count) sessions" }
}
$global:LASTEXITCODE = 0
exit 0
