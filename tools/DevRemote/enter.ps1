# 게임을 실행한 뒤 모드 에디터의 "게임 디버깅 시작"을 거쳐 첫 번째 세이브로 세계에 진입한다.
# 각 단계는 클릭 전에 그 좌표의 UI 요소 이름을 확인하고, 기대한 버튼일 때만 누른다.
param([string]$GameDir = 'E:\Games\Tale of Immortal', [switch]$Launch)
$dir = 'E:\TlToybox\.devremote'
$log = Join-Path $GameDir 'MelonLoader\Latest.log'

function Send([string]$c) {
  $out = Join-Path $dir 'out.txt'
  if (Test-Path $out) { [IO.File]::Delete($out) }
  $cmd = Join-Path $dir 'cmd.txt'; if (Test-Path $cmd) { [IO.File]::Delete($cmd) }
  [IO.File]::WriteAllText((Join-Path $dir 'cmd.tmp'), $c)
  [IO.File]::Move((Join-Path $dir 'cmd.tmp'), (Join-Path $dir 'cmd.txt'))
  for ($n = 0; $n -lt 50 -and -not (Test-Path $out); $n++) { [Threading.Thread]::Sleep(100) }
  if (Test-Path $out) { ([IO.File]::ReadAllText($out)).Trim() } else { 'NO RESPONSE' }
}

function ReadLog { $fs = [IO.File]::Open($log, 'Open', 'Read', 'ReadWrite'); $sr = New-Object IO.StreamReader($fs); $t = $sr.ReadToEnd(); $sr.Close(); $fs.Close(); $t }

# 좌표의 요소 이름이 $expect와 일치할 때까지 기다렸다가 클릭한다.
function ClickWhen([int]$x, [int]$y, [string]$expect, [int]$timeoutSec = 60) {
  for ($t = 0; $t -lt $timeoutSec; $t++) {
    $hit = Send "hit $x $y"
    if ($hit -match $expect) { $r = Send "click $x $y"; "OK   $expect : $r"; [Threading.Thread]::Sleep(1500); return $true }
    [Threading.Thread]::Sleep(1000)
  }
  "FAIL $expect : last hit = $hit"; return $false
}

$started = Get-Date
if ($Launch) {
  if (Get-Process guigubahuang -ErrorAction SilentlyContinue) { 'game already running'; exit 1 }
  Start-Process -FilePath (Join-Path $GameDir 'guigubahuang.exe') -WorkingDirectory $GameDir
}
for ($t = 0; $t -lt 90 -and -not ((Test-Path $log) -and ((-not $Launch) -or (Get-Item $log).LastWriteTime -gt $started) -and (ReadLog) -match 'DevRemote] watching'); $t++) { [Threading.Thread]::Sleep(1000) }

$steps = @(
  @(960, 1002, 'StartGameTip/.*btnOK'),      # 공지: 확인
  @(170, 800, 'Login/.*btnMod'),             # 메인: 모드
  @(958, 560, 'ModMain/.*btnProject'),       # 모드 제작
  @(1506, 988, 'ModMainProject/.*btnInfoOpen'), # 편집
  @(1768, 762, 'ModMainFunction/.*btnDebug'),   # 게임 디버깅 시작
  @(958, 541, 'CheckPopup/.*btn2'),          # 알림: 예
  @(170, 636, 'Login/.*btnInto'),            # 팔황 진입하기
  @(520, 390, 'CacheItem.*btnRoot1')         # 첫 번째 세이브
)
foreach ($s in $steps) {
  $ok = ClickWhen $s[0] $s[1] $s[2]
  $ok | Where-Object { $_ -is [string] }
  if (($ok | Select-Object -Last 1) -ne $true) { exit 1 }
}
for ($t = 0; $t -lt 180 -and -not ((ReadLog) -match 'TlToybox\] state: ready'); $t++) { [Threading.Thread]::Sleep(1000) }
if ((ReadLog) -match 'TlToybox\] state: ready') { 'WORLD READY' } else { 'WORLD NOT READY'; exit 1 }
