# 창작마당 모드를 찾고 받아 게임의 로컬 모드 폴더에 설치하는 도구
# (docs/superpowers/specs/2026-10-15-workshop-install-design.md, 2026-10-16-workshop-expand-design.md).
#   -Status                       목록의 줄마다 상태·받음·설치·갱신·DLL을 보인다
#   -Download  [-Id a,b]          설치 줄 가운데 받지 않은 것(또는 -Id로 고른 것. 보류는 되고 제외는 거부)을 SteamCMD의 캐시 로그인으로 받는다(최대 세 번)
#   -Install   [-Id a,b] [-Yes]   설치 줄의 받은 것을 <GameDir>\ModExportData\<ID>\에 복사한다. -Yes 없이는 계획만 보인다
#   -Uninstall [-Id a,b] [-Yes]   목록 ID의 설치 폴더를 지운다(상태와 무관). -Yes 없이는 계획만 보인다
#   -Search    [-Kind …] [-Top n] 창작마당에서 종류마다 구독자 상위 후보를 모아 후보표를 쓴다(2026-10-16 설계 3.3절)
#   -Doc                          등록부 문서 docs/09-workshop-mods.md와 컬렉션 설명문을 쓴다(3.4절)
# 목록 tools/workshop/mods.txt: 창작마당ID | 상태(설치·보류·제외) | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 비고. 줄 순서가 설치 순서.
# 받음·설치·갱신·지움은 tools/workshop/history.txt에 한 줄씩 남는다(-NoHistory면 남기지 않는다. 회차 안에서 쓴다).
# 게임 폴더에 쓰는 것은 ModExportData\<창작마당ID>\뿐이다(CLAUDE.md). 비밀번호는 받지 않는다. 네트워크는 읽기만 한다.
# 설치 원본: <ID>\ModExportData.cache가 있으면 <ID>\ 전체, 없으면 <ID>\debug\Mod_*\ 가운데 cache가 있는 폴더가 하나일 때 그것.
param([switch]$Status, [switch]$Download, [switch]$Install, [switch]$Uninstall, [switch]$Search, [switch]$Doc, [switch]$Yes,
  [string]$Id = '',
  [string]$GameDir = 'E:\Games\Tale of Immortal', [string]$SteamCmd = 'E:\SteamCmd\steamcmd.exe', [string]$Account = 'deepestlight',
  [string]$List = '', [int]$AppId = 1468810,
  [string]$History = '', [switch]$NoHistory,
  # -Search (3.3절)
  [string]$Kind = '외형,공법,세계관,음악', [int]$Top = 20, [double]$Delay = 1.5, [string]$Out = '',
  # -Doc (3.4절)
  [string]$DocOut = '', [string]$CollectionFile = '', [string]$TranslationFile = '',
  # 아래는 workshop.tests.ps1이 쓰는 시험용 매개변수다. 평소에는 주지 않는다. -WebRoot는 네트워크 대신 그 폴더의 파일을 읽는다.
  [string]$ContentDir = '', [string]$ProcessName = 'guigubahuang', [int]$TimeoutMinutes = 30, [string]$WebRoot = '')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $List) { $List = Join-Path $repo 'tools\workshop\mods.txt' }
$listDir = Split-Path -Parent $List
if (-not $History) { $History = Join-Path $listDir 'history.txt' }
if (-not $CollectionFile) { $CollectionFile = Join-Path $listDir 'collection.txt' }
if (-not $TranslationFile) { $TranslationFile = Join-Path $repo 'src\ModCode\ModMain\Translations\mods.txt' }
if (-not $Out) { $Out = Join-Path $listDir ('candidates-' + (Get-Date -Format 'yyyyMMdd') + '.md') }
if (-not $DocOut) { $DocOut = Join-Path $repo 'docs\09-workshop-mods.md' }
$steamRoot = Split-Path -Parent $SteamCmd
if (-not $ContentDir) { $ContentDir = Join-Path $steamRoot "steamapps\workshop\content\$AppId" }
$acf = Join-Path $steamRoot "steamapps\workshop\appworkshop_$AppId.acf"
$modsDir = Join-Path $GameDir 'ModExportData'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$States = @('설치', '보류', '제외')
$Kinds = @('틀', '외형', '공법', '세계관', '음악')

function Running { [bool](Get-Process $ProcessName -ErrorAction SilentlyContinue) }

# 목록 파일. '#' 줄은 주석. ID | 상태 | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 비고
function ReadList {
  if (-not (Test-Path -LiteralPath $List)) { throw "목록 파일이 없습니다: $List" }
  $rows = New-Object System.Collections.Generic.List[object]
  foreach ($line in [IO.File]::ReadAllLines($List)) {
    $t = $line.Trim()
    if (-not $t -or $t.StartsWith('#')) { continue }
    $cells = @($t.Split('|') | ForEach-Object { $_.Trim() })
    if ($cells.Count -lt 7 -or $cells[0] -notmatch '^\d+$') { throw "목록의 줄을 읽지 못했습니다(일곱 칸이어야 합니다): $line" }
    if ($States -notcontains $cells[1]) { throw "모르는 상태입니다(설치·보류·제외): $line" }
    $held = $cells[1] -eq '보류'
    if (-not ($Kinds -contains $cells[2] -or ($held -and $cells[2] -eq '-'))) { throw "모르는 종류입니다(틀·외형·공법·세계관·음악): $line" }
    if (-not $held -and ($cells[3] -in @('', '-') -or $cells[4] -in @('', '-'))) { throw "설치·제외 줄은 이름을 비울 수 없습니다: $line" }
    $rows.Add([pscustomobject]@{ Id = $cells[0]; State = $cells[1]; Kind = $cells[2]; Title = $cells[3]; Korean = $cells[4]; ModId = $cells[5]; Note = $cells[6] })
  }
  $rows.ToArray()
}
# -Id로 고른 것만 남긴다. 목록에 없는 ID는 거부한다.
function Chosen($rows) {
  if (-not $Id) { return @($rows) }
  $wanted = @($Id.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
  foreach ($w in $wanted) { if (-not @($rows | Where-Object { $_.Id -eq $w })) { throw "목록에 없는 ID입니다: $w" } }
  @($rows | Where-Object { $wanted -contains $_.Id })
}
# 받을 줄: 설치 줄. -Id로 지정하면 보류도 받되 제외는 거부한다.
function ForDownload($rows) {
  if ($Id) { foreach ($r in $rows) { if ($r.State -eq '제외') { throw "제외한 ID입니다: $($r.Id) ($($r.Note))" } }; return @($rows) }
  @($rows | Where-Object { $_.State -eq '설치' })
}
# 설치할 줄: 설치 줄만. -Id로 보류·제외를 지정하면 거부한다.
function ForInstall($rows) {
  if ($Id) { foreach ($r in $rows) { if ($r.State -ne '설치') { throw "$($r.State) 상태의 ID는 설치하지 않습니다: $($r.Id)" } } }
  @($rows | Where-Object { $_.State -eq '설치' })
}
# 이력 한 줄을 덧붙인다. 동작은 받음·설치·갱신·지움. -NoHistory면 쓰지 않는다.
function WriteHistory([string]$action, $row) {
  if ($NoHistory) { return }
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $History))
  $line = '{0} | {1} | {2} | {3}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm'), $action, $row.Id, $row.Korean
  [IO.File]::AppendAllText($History, $line + "`n", $utf8)
}
# 받은 폴더. ModExportData.cache가 어디에든 하나 이상 있어야 받은 것이다(받다 끊긴 빈 폴더는 아니다).
function Downloaded([string]$id) {
  $root = Join-Path $ContentDir $id
  if (-not (Test-Path -LiteralPath $root -PathType Container)) { return $null }
  if (@(Get-ChildItem -LiteralPath $root -Recurse -File -Filter ModExportData.cache).Count -ge 1) { $root } else { $null }
}
# 설치 원본. 평평하면 <ID>\, debug 모양이면 <ID>\debug\Mod_*\ 하나. 못 고르면 Path가 $null이고 Reason에 이유.
function Source([string]$id) {
  $root = Join-Path $ContentDir $id
  if (-not (Downloaded $id)) { return [pscustomobject]@{ Path = $null; Shape = '-'; Reason = $(if (Test-Path -LiteralPath $root) { 'ModExportData.cache가 없음' } else { '받지 않음' }) } }
  if (Test-Path -LiteralPath (Join-Path $root 'ModExportData.cache')) { return [pscustomobject]@{ Path = $root; Shape = '평평함'; Reason = '' } }
  $debug = Join-Path $root 'debug'
  $inner = @(if (Test-Path -LiteralPath $debug) { Get-ChildItem -LiteralPath $debug -Directory | Where-Object { $_.Name -like 'Mod_*' -and (Test-Path -LiteralPath (Join-Path $_.FullName 'ModExportData.cache')) } })
  if ($inner.Count -eq 1) { return [pscustomobject]@{ Path = $inner[0].FullName; Shape = 'debug'; Reason = '' } }
  if ($inner.Count -gt 1) { return [pscustomobject]@{ Path = $null; Shape = 'debug'; Reason = "debug 안에 Mod_* 폴더가 $($inner.Count)개" } }
  [pscustomobject]@{ Path = $null; Shape = '?'; Reason = '설치 원본을 고르지 못함' }
}
function SizeMb([string]$path) {
  $sum = (Get-ChildItem -LiteralPath $path -Recurse -File | Measure-Object Length -Sum).Sum
  [math]::Round([double]$sum / 1MB, 1)
}
function Dlls([string]$path) { @(Get-ChildItem -LiteralPath $path -Recurse -File -Filter *.dll | ForEach-Object Name) }
# 받은 원본과 설치본이 다른 파일 수. robocopy /L은 복사하지 않고 할 일만 센다(끝 코드 0이면 같다).
function DiffCount([string]$src, [string]$dst) {
  $lines = @(robocopy $src $dst /L /MIR /NJH /NJS /NP /NDL /NC /NS | Where-Object { $_.Trim() })
  if ($LASTEXITCODE -ge 8) { throw "robocopy 비교에 실패했습니다($LASTEXITCODE): $src" }
  if ($LASTEXITCODE -eq 0) { 0 } else { [math]::Max(1, $lines.Count) }
}
# appworkshop_<AppId>.acf의 timeupdated(창작마당에서 마지막으로 갱신된 날).
function Updated([string]$id) {
  if (-not (Test-Path -LiteralPath $acf)) { return '-' }
  $m = [regex]::Match([IO.File]::ReadAllText($acf), '"' + $id + '"\s*\{[^}]*"timeupdated"\s*"(\d+)"')
  if ($m.Success) { [DateTimeOffset]::FromUnixTimeSeconds([long]$m.Groups[1].Value).LocalDateTime.ToString('yyyy-MM-dd') } else { '-' }
}
function Installed([string]$id) { Join-Path $modsDir $id }
function IsInstalled([string]$id) { Test-Path -LiteralPath (Join-Path (Installed $id) 'ModExportData.cache') }

function ShowStatus($rows) {
  $got = 0; $inst = 0
  $target = @($rows | Where-Object { $_.State -eq '설치' })
  'ID | 상태 | 종류 | 이름 | 받음 | 설치 | 갱신 | DLL'
  foreach ($r in $rows) {
    $root = Downloaded $r.Id
    $s = Source $r.Id
    $isInst = IsInstalled $r.Id
    if ($r.State -eq '설치') { if ($root) { $got++ }; if ($isInst) { $inst++ } }
    $have = if ($root) { "$(SizeMb $root)MB $(Updated $r.Id)" } else { '-' }
    $fresh = if ($s.Path -and $isInst) { $n = DiffCount $s.Path (Installed $r.Id); if ($n -eq 0) { '같음' } else { "다름 $n" } } else { '-' }
    $dll = if ($root) { (Dlls $root).Count } else { '-' }
    "$($r.Id) | $($r.State) | $($r.Kind) | $($r.Korean) | $have | $(if ($isInst) { '예' } else { '-' }) | $fresh | $dll"
  }
  $held = @($rows | Where-Object { $_.State -eq '보류' }).Count
  $excl = @($rows | Where-Object { $_.State -eq '제외' }).Count
  "받음 $got/$($target.Count), 설치 $inst/$($target.Count) | 보류 $held | 제외 $excl"
}

function DoInstall($rows) {
  if (-not (Test-Path -LiteralPath $modsDir -PathType Container)) { throw "게임의 로컬 모드 폴더가 없습니다: $modsDir (ModExportData가 있는 게임 폴더여야 합니다)" }
  $running = Running
  $plans = New-Object System.Collections.Generic.List[object]
  foreach ($r in $rows) {
    $s = Source $r.Id
    if (-not $s.Path) { "$($r.Id) | $($r.Korean) | 건너뜀: $($s.Reason)"; continue }
    $target = Installed $r.Id
    $exists = Test-Path -LiteralPath $target -PathType Container
    $what = if (-not $exists) { '새로 설치' } else { $n = DiffCount $s.Path $target; if ($n -eq 0) { '같음' } else { "갱신(다른 파일 $n)" } }
    $dlls = Dlls $s.Path
    $line = "$($r.Id) | $($r.Korean) | $($s.Shape) | $(SizeMb $s.Path)MB | DLL $(if ($dlls.Count -gt 0) { $dlls -join ',' } else { '-' }) | $what"
    if ($exists -and $running -and $what -ne '같음') { "$line | 거부: 게임이 실행 중입니다(설치된 폴더를 덮지 않습니다)"; continue }
    $line
    $plans.Add([pscustomobject]@{ Row = $r; Src = $s.Path; Dst = $target; What = $what })
  }
  if (-not $Yes) { 'PLAN ONLY (아무것도 쓰지 않았습니다. 실제로 쓰려면 -Yes)'; return }
  $done = 0
  foreach ($p in $plans) {
    if ($p.What -eq '같음') { "UNCHANGED $($p.Row.Id) $($p.Row.Korean)"; continue }
    robocopy $p.Src $p.Dst /MIR /NJH /NJS /NP /NDL /NFL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "복사에 실패했습니다(robocopy $LASTEXITCODE): $($p.Dst)" }
    if (-not (Test-Path -LiteralPath (Join-Path $p.Dst 'ModExportData.cache'))) { throw "설치본에 ModExportData.cache가 없습니다: $($p.Dst)" }
    "$(if ($p.What -eq '새로 설치') { 'INSTALLED' } else { 'UPDATED' }) $($p.Row.Id) $($p.Row.Korean)"
    WriteHistory $(if ($p.What -eq '새로 설치') { '설치' } else { '갱신' }) $p.Row
    $done++
  }
  if ($running -and $done -gt 0) { '게임이 실행 중입니다. 새로 넣은 모드는 다음 게임 시작부터 읽힙니다.' }
  "설치 ${done}개"
}

function DoUninstall($rows) {
  if (Running) { throw '게임이 실행 중입니다. 모드 폴더를 지우지 않습니다. 게임을 끈 뒤 다시 돌립니다.' }
  $targets = New-Object System.Collections.Generic.List[object]
  foreach ($r in $rows) {
    $target = Installed $r.Id
    if (-not (Test-Path -LiteralPath $target -PathType Container)) { "$($r.Id) | $($r.Korean) | 설치돼 있지 않음"; continue }
    "$($r.Id) | $($r.Korean) | 지움 $(SizeMb $target)MB"
    $targets.Add([pscustomobject]@{ Row = $r; Path = $target })
  }
  if (-not $Yes) { 'PLAN ONLY (아무것도 지우지 않았습니다. 실제로 지우려면 -Yes)'; return }
  foreach ($t in $targets) {
    # 지우는 것이 ModExportData 바로 아래의 목록 ID 폴더인지 한 번 더 확인한다. Mod_*와 모르는 폴더는 여기 올 수 없다.
    $full = [IO.Path]::GetFullPath($t.Path)
    $parent = [IO.Path]::GetFullPath($modsDir).TrimEnd('\') + '\'
    if (-not $full.StartsWith($parent) -or (Split-Path -Leaf $full) -ne $t.Row.Id -or $t.Row.Id -notmatch '^\d+$') { throw "지우지 않습니다(예상 밖의 경로): $full" }
    [IO.Directory]::Delete($full, $true)
    "REMOVED $($t.Row.Id) $($t.Row.Korean)"
    WriteHistory '지움' $t.Row
  }
  "지움 $($targets.Count)개"
}

# SteamCMD를 돌리고 출력 줄을 돌려준다. 표준 입력은 빈 파일이라 비밀번호를 물으면 바로 끝난다(멈춰 서지 않는다).
# 시간을 넘기면 끊는다. -SteamCmd가 .ps1이면 pwsh로 돌린다(시험용 가짜).
function RunSteamCmd([string[]]$arguments) {
  $outFile = Join-Path $env:TEMP 'tltoybox-workshop-out.txt'
  $inFile = Join-Path $env:TEMP 'tltoybox-workshop-in.txt'
  [IO.File]::WriteAllText($inFile, '')
  if (Test-Path -LiteralPath $outFile) { [IO.File]::Delete($outFile) }
  $isScript = $SteamCmd.ToLower().EndsWith('.ps1')
  $file = if ($isScript) { 'pwsh' } else { $SteamCmd }
  $argList = if ($isScript) { @('-NoProfile', '-File', "`"$SteamCmd`"") + $arguments } else { $arguments }
  $p = Start-Process -FilePath $file -ArgumentList $argList -WorkingDirectory $steamRoot -NoNewWindow -PassThru -RedirectStandardOutput $outFile -RedirectStandardInput $inFile
  if (-not $p.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    $p.Kill()
    return @{ Ok = $false; Lines = @(); Error = "TIMEOUT: $TimeoutMinutes 분 안에 끝나지 않아 끊었습니다(로그인 입력을 기다리는 것일 수 있습니다)" }
  }
  @{ Ok = $true; Lines = @(Get-Content -LiteralPath $outFile -ErrorAction SilentlyContinue); Error = '' }
}

function DoDownload($rows) {
  $pending = @($rows | Where-Object { $Id -or -not (Downloaded $_.Id) })
  if ($pending.Count -eq 0) { "받음 $($rows.Count)/$($rows.Count) (받을 것이 없습니다)"; return }
  $script = Join-Path $env:TEMP 'tltoybox-workshop-run.txt'
  for ($pass = 1; $pass -le 3 -and $pending.Count -gt 0; $pass++) {
    "pass ${pass}: $($pending.Count)개"
    [IO.File]::WriteAllLines($script, [string[]](@($pending | ForEach-Object { "workshop_download_item $AppId $($_.Id)" }) + @('quit')))
    $res = RunSteamCmd @('+login', $Account, '+runscript', "`"$script`"", '+quit')
    if (-not $res.Ok) { $res.Error; break }
    $joined = $res.Lines -join "`n"
    if ($joined -match 'Cached credentials not found|Login Failure|FAILED \(Invalid Password\)|password:') {
      "로그인이 풀려 있습니다. 사용자가 아래 명령으로 한 번 로그인한 뒤(비밀번호·Steam Guard는 사용자가 입력) 다시 돌립니다:"
      "  $SteamCmd +login $Account +quit"
      return
    }
    if ($joined -match 'No workshop depot defined') { "이 계정($Account)은 게임($AppId)을 갖고 있지 않습니다. 게임을 가진 계정으로 로그인해야 받을 수 있습니다."; return }
    foreach ($r in $pending) {
      if ($joined -match "Success\. Downloaded item $($r.Id)\b") { "OK   $($r.Id) $($r.Korean)"; WriteHistory '받음' $r } else { "FAIL $($r.Id) $($r.Korean)" }
    }
    $pending = @($pending | Where-Object { -not ($joined -match "Success\. Downloaded item $($_.Id)\b") })
  }
  $got = @($rows | Where-Object { Downloaded $_.Id }).Count
  "받음 $got/$($rows.Count)"
  if ($pending.Count -gt 0) { "받지 못함: $(($pending | ForEach-Object Id) -join ', ')" }
}

# ---- -Search: 창작마당 후보표 (2026-10-16 설계 3.3절) ----
$UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) TlToybox-workshop/1.0'
$BrowseUrl = 'https://steamcommunity.com/workshop/browse/'
$DetailsUrl = 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/'
$ItemUrl = 'https://steamcommunity.com/sharedfiles/filedetails/?id='
$KindQueries = [ordered]@{
  '외형'   = @(@{ tag = 'Beautify' })
  '공법'   = @(@{ tag = 'Battle' }, @{ search = '功法' })
  '세계관' = @(@{ tag = 'Drama' }, @{ tag = 'NPC' }, @{ tag = 'LargeIntegrated' })
  '음악'   = @(@{ search = '音乐' }, @{ search = 'BGM' }, @{ search = '特效' })
}
$NsfwRe = '双修|雙修|采补|採補|裸|R18|NSFW|涩|澀|绅士|紳士|魅魔|合欢|合歡|处子|處子'
$SystemRe = '升级|升級|合成|分解|融合|经验|經驗|速度|修改器|工具|框架|UI|自动|自動|参悟|參悟|秘籍|秘笈'
$LimitRe = '请求太多'

function Rest { if (-not $WebRoot) { Start-Sleep -Milliseconds ([int]($Delay * 1000)) } }
# HTTP 한 번. $form이 있으면 폼 POST. -WebRoot가 있으면 그 폴더의 $local 파일을 읽는다(시험용).
# 돌려주는 것: Ok, Status, Body, Limited(요청 제한: HTTP 429 또는 본문의 "请求太多")
function Fetch([string]$url, [string]$local, [hashtable]$form = $null) {
  if ($WebRoot) {
    if (-not $url.StartsWith('https://')) { throw "잘못된 주소입니다: $url" }   # 시험에서도 주소 조립을 검사한다
    $path = Join-Path $WebRoot $local
    if (-not (Test-Path -LiteralPath $path)) { return @{ Ok = $false; Status = 404; Body = ''; Limited = $false } }
    $body = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
    return @{ Ok = $true; Status = 200; Body = $body; Limited = [bool]($body -match $LimitRe) }
  }
  $headers = @{ 'User-Agent' = $UA; 'Accept-Language' = 'zh-CN,ko;q=0.8' }
  try {
    if ($form) {
      $pairs = @($form.GetEnumerator() | ForEach-Object { [Uri]::EscapeDataString([string]$_.Key) + '=' + [Uri]::EscapeDataString([string]$_.Value) })
      $res = Invoke-WebRequest -Uri $url -Method Post -Body ($pairs -join '&') -ContentType 'application/x-www-form-urlencoded' -Headers $headers -TimeoutSec 60
    } else {
      $res = Invoke-WebRequest -Uri $url -Headers $headers -TimeoutSec 60
    }
    $body = [string]$res.Content
    @{ Ok = $true; Status = [int]$res.StatusCode; Body = $body; Limited = [bool]($body -match $LimitRe) }
  } catch {
    $status = 0
    if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
    @{ Ok = $false; Status = $status; Body = ''; Limited = ($status -eq 429) }
  }
}
function QueryName([hashtable]$q) { if ($q.tag) { $q.tag } else { $q.search } }
# 검색 페이지 한 쪽의 ID(나온 순서, 중복 없이). 구독자순, 바로 사용 가능한 항목, 한 쪽 30개
function BrowsePage([hashtable]$q, [int]$page) {
  $qs = "appid=$AppId&browsesort=totaluniquesubscribers&actualsort=totaluniquesubscribers&section=readytouseitems&numperpage=30&p=$page"
  if ($q.tag) { $qs += '&requiredtags[]=' + [Uri]::EscapeDataString($q.tag) } else { $qs += '&searchtext=' + [Uri]::EscapeDataString($q.search) }
  $res = Fetch "${BrowseUrl}?$qs" "browse-$(QueryName $q)-p$page.html"   # ?는 변수 이름에 들어가므로 ${}로 끊는다
  $ids = New-Object System.Collections.Generic.List[string]
  if ($res.Ok) { foreach ($m in [regex]::Matches($res.Body, 'sharedfiles/filedetails/\?id=(\d+)')) { if (-not $ids.Contains($m.Groups[1].Value)) { $ids.Add($m.Groups[1].Value) } } }
  @{ Ok = $res.Ok; Ids = $ids.ToArray(); Limited = $res.Limited }
}
# 상세 API. 30개씩. ID → 상세(JSON 객체). 돌려주지 않은 ID, result가 1이 아닌 것, 차단된 것, 비공개는 뺀다
function Details([string[]]$ids) {
  $map = @{}
  for ($i = 0; $i -lt $ids.Count; $i += 30) {
    $batch = @($ids[$i..([math]::Min($i + 29, $ids.Count - 1))])
    $form = @{ itemcount = $batch.Count }
    for ($k = 0; $k -lt $batch.Count; $k++) { $form["publishedfileids[$k]"] = $batch[$k] }
    $res = Fetch $DetailsUrl 'details.json' $form
    if ($res.Ok) {
      $json = $null
      try { $json = $res.Body | ConvertFrom-Json } catch { $json = $null }
      if ($json) {
        foreach ($d in @($json.response.publishedfiledetails)) {
          $fid = [string]$d.publishedfileid
          if ($batch -notcontains $fid) { continue }
          if ([int]$d.result -ne 1 -or [bool]$d.banned -or [int]$d.visibility -ne 0) { continue }
          $map[$fid] = $d
        }
      }
    }
    Rest
  }
  $map
}
# 항목 페이지의 "필수 아이템". Text: '-'(없음), '조회 실패', 또는 'ID 이름, ID 이름'
function RequiredItems([string]$id) {
  $res = Fetch "$ItemUrl$id" "item-$id.html"
  if (-not $res.Ok) { return @{ Text = '조회 실패'; Limited = $res.Limited } }
  $i = $res.Body.IndexOf('id="RequiredItems"')
  if ($i -lt 0) { return @{ Text = '-'; Limited = $res.Limited } }
  $seg = $res.Body.Substring($i, [math]::Min(6000, $res.Body.Length - $i))
  $items = @(foreach ($m in [regex]::Matches($seg, 'filedetails/\?id=(\d+)[^>]*>\s*<div class="requiredItem">\s*([^<]*)')) { $m.Groups[1].Value + ' ' + $m.Groups[2].Value.Trim() })
  @{ Text = $(if ($items.Count -gt 0) { $items -join ', ' } else { '-' }); Limited = $res.Limited }
}
# 서식 태그를 지우고 한 줄로. 표의 칸 구분자 |는 전각으로
function StripTags([string]$s) {
  if (-not $s) { return '' }
  [regex]::Replace($s, '<[^>]*>', '').Replace("`r", ' ').Replace("`n", ' ').Replace('|', '｜').Trim()
}
# 표시(경고): 목록에 있음(상태), 성인향?, 오래됨(2023-01-01 이전 갱신), 큼(200MB 초과), 시스템?(공법 종류만), 대형
function Flags([string]$kind, $d, [string]$listState) {
  $f = New-Object System.Collections.Generic.List[string]
  if ($listState) { $f.Add("목록에 있음($listState)") }
  if ([string]$d.title -match $NsfwRe) { $f.Add('성인향?') }
  if ([long]$d.time_updated -lt 1672531200) { $f.Add('오래됨') }
  if ([long]$d.file_size -gt 200MB) { $f.Add('큼') }
  if ($kind -eq '공법' -and (([string]$d.title + ' ' + [string]$d.description) -match $SystemRe)) { $f.Add('시스템?') }
  if (@(@($d.tags) | ForEach-Object { [string]$_.tag }) -contains 'LargeIntegrated') { $f.Add('대형') }
  if ($f.Count -eq 0) { '' } else { $f -join ' ' }
}
function Mb([long]$bytes) { [math]::Round([double]$bytes / 1MB, 1) }
function DateOf([long]$unix) { [DateTimeOffset]::FromUnixTimeSeconds($unix).LocalDateTime.ToString('yyyy-MM-dd') }

# 후보표와 설명 전문 파일을 쓴다. $found: 종류 → ID 목록(목록에 있는 것 포함). 종류마다 요약 한 줄을 출력한다
function WriteCandidates([string[]]$kinds, $found, $listed, $details, $req, [string]$stopped) {
  $sb = New-Object System.Text.StringBuilder
  $desc = New-Object System.Text.StringBuilder
  [void]$sb.AppendLine("# 창작마당 후보표 ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))")
  [void]$sb.AppendLine('')
  [void]$sb.AppendLine("도구 pwsh tools/workshop.ps1 -Search -Kind $($kinds -join ',') -Top $Top 이 만들었다(창작마당 검색 페이지의 구독자순). 표시는 경고일 뿐 자동으로 빼지 않는다. 호환성은 댓글을 읽지 않으므로 갱신일로만 가늠한다. 한국어 요약·추천 열은 사람이 채운다. DLL 유무는 받은 뒤 -Status로 본다. 설명 전문은 같은 이름의 -desc.txt(git 제외).")
  foreach ($k in $kinds) {
    [void]$sb.AppendLine('')
    if (-not $found.Contains($k)) { [void]$sb.AppendLine("## $k (요청 제한으로 읽지 못함)"); "${k}: 읽지 못함"; continue }
    $ids = @($found[$k])
    $cands = @($ids | Where-Object { -not $listed.ContainsKey($_) -and $details.ContainsKey($_) })
    $inList = @($ids | Where-Object { $listed.ContainsKey($_) -and $details.ContainsKey($_) })
    "${k}: 후보 $($cands.Count), 목록에 있음 $($inList.Count)"
    [void]$sb.AppendLine("## $k ($($cands.Count)개 후보)")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('| 순위 | ID | 제목 | 구독(누적) | 즐겨찾기 | 갱신일 | 크기 | 태그 | 필수 항목 | 표시 | 한국어 요약 | 추천 |')
    [void]$sb.AppendLine('|---|---|---|---|---|---|---|---|---|---|---|---|')
    $rank = 0
    foreach ($id in $ids) {
      if (-not $details.ContainsKey($id)) { continue }
      $d = $details[$id]
      $isListed = $listed.ContainsKey($id)
      $no = if ($isListed) { '-' } else { $rank++; "$rank" }
      $need = if ($isListed) { '-' } elseif ($req.ContainsKey($id)) { $req[$id] } elseif ($stopped) { '조회 안 함(요청 제한)' } else { '조회 실패' }
      $tags = (@(@($d.tags) | ForEach-Object { [string]$_.tag }) -join ',')
      $flag = Flags $k $d $(if ($isListed) { $listed[$id] } else { '' })
      [void]$sb.AppendLine("| $no | $id | $(StripTags $d.title) | $($d.lifetime_subscriptions) | $($d.favorited) | $(DateOf $d.time_updated) | $(Mb $d.file_size)MB | $tags | $need | $flag |  |  |")
      [void]$desc.AppendLine($id + "`t" + ([string]$d.description).Replace("`r", '').Replace("`n", '\n'))
    }
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('설명(앞 200자):')
    foreach ($id in $cands) {
      $text = StripTags $details[$id].description
      if ($text.Length -gt 200) { $text = $text.Substring(0, 200) + '…' }
      [void]$sb.AppendLine("- ${id}: $text")
    }
  }
  if ($stopped) { [void]$sb.AppendLine(''); [void]$sb.AppendLine("요청 제한으로 멈춤: $stopped") }
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Out))
  [IO.File]::WriteAllText($Out, $sb.ToString().Replace("`r`n", "`n"), $utf8)   # LF(-Doc과 같게)
  $descPath = Join-Path (Split-Path -Parent $Out) ([IO.Path]::GetFileNameWithoutExtension($Out) + '-desc.txt')
  [IO.File]::WriteAllText($descPath, $desc.ToString().Replace("`r`n", "`n"), $utf8)
}

function DoSearch($rows) {
  $kinds = @($Kind.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
  foreach ($k in $kinds) { if (-not $KindQueries.Contains($k)) { throw "모르는 종류입니다(외형·공법·세계관·음악): $k" } }
  $listed = @{}; foreach ($r in $rows) { $listed[$r.Id] = $r.State }
  $taken = @{}                 # 앞 종류에서 후보가 된 ID
  $found = [ordered]@{}        # 종류 → 표에 넣을 ID(나온 순서. 목록에 있는 것은 표시용으로 함께)
  $stopped = ''                # 요청 제한으로 멈춘 자리
  foreach ($k in $kinds) {
    $ids = New-Object System.Collections.Generic.List[string]
    $count = 0
    :queries foreach ($q in $KindQueries[$k]) {
      for ($p = 1; $p -le 5; $p++) {
        $page = BrowsePage $q $p
        if ($page.Limited) { $stopped = "$(QueryName $q) ${p}쪽"; break queries }
        if (-not $page.Ok -or $page.Ids.Count -eq 0) { break }
        foreach ($id in $page.Ids) {
          if ($ids.Contains($id) -or $taken.ContainsKey($id)) { continue }
          $ids.Add($id)
          if (-not $listed.ContainsKey($id)) { $count++; if ($count -ge $Top) { break queries } }
        }
        Rest
      }
    }
    foreach ($id in $ids) { if (-not $listed.ContainsKey($id)) { $taken[$id] = $true } }
    $found[$k] = $ids.ToArray()
    if ($stopped) { break }
  }
  $all = @(@(foreach ($k in $found.Keys) { $found[$k] }) | Select-Object -Unique)
  $details = if ($all.Count -gt 0) { Details ([string[]]$all) } else { @{} }
  $req = @{}
  if (-not $stopped) {
    foreach ($id in $all) {
      if ($listed.ContainsKey($id) -or -not $details.ContainsKey($id)) { continue }
      $r = RequiredItems $id
      if ($r.Limited) { $stopped = "항목 $id"; break }
      $req[$id] = $r.Text
      Rest
    }
  }
  WriteCandidates $kinds $found $listed $details $req $stopped
  if ($stopped) { "요청 제한으로 멈춤: $stopped (모은 것까지 썼다. 나중에 다시 돌린다)" }
  "후보표: $Out"
}

$all = ReadList
$chosen = Chosen $all
$modes = @(@($Status, $Download, $Install, $Uninstall, $Search, $Doc) | Where-Object { $_ }).Count
if ($modes -ne 1) { throw '-Status, -Download, -Install, -Uninstall, -Search, -Doc 가운데 하나를 고릅니다.' }
if ($Status) { ShowStatus $chosen }
elseif ($Download) { DoDownload (ForDownload $chosen) }
elseif ($Install) { DoInstall (ForInstall $chosen) }
elseif ($Uninstall) { DoUninstall $chosen }
elseif ($Search) { DoSearch $all }
elseif ($Doc) { throw '-Doc은 아직 없습니다(Task 3).' }
$global:LASTEXITCODE = 0
exit 0
