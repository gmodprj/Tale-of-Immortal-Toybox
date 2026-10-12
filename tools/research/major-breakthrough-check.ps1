# 대경지 돌파 재료 지급 검증: 경지를 시험으로 바꾸고, 방식마다 지급 -> 게임 돌파 화면 -> 카드 선택 -> 자동 투입 -> 캡처.
param([int]$Prev, [int]$Rows, [string]$Tag, [int[]]$Only = @())
$T = 'E:\TlToybox\tools\DevRemote'
$S = 'E:TlToybox.devremote'   # 캡처를 두는 곳
Add-Type -AssemblyName System.Drawing
function Rc([string]$c, [int]$wait = 800) { $r = & "$T\rc.ps1" -Cmd $c -NoShot -WaitMs $wait; $r }
function Hit([int]$x, [int]$y) { (& "$T\rc.ps1" -Cmd "hit $x $y" -NoShot -WaitMs 30) -replace '^.*hit=', '' }
function Shot([string]$name) {
  $full = "$S\full-$name.png"
  & "$T\win.ps1" -Do shot -Out $full -Scale 1 | Out-Null
  $bmp = [System.Drawing.Bitmap]::FromFile($full)
  $w = [int]($bmp.Width * 0.45); $h = [int]($bmp.Height * 0.45)
  $small = New-Object System.Drawing.Bitmap $w, $h
  $g = [System.Drawing.Graphics]::FromImage($small); $g.InterpolationMode = 'HighQualityBicubic'
  $g.DrawImage($bmp, 0, 0, $w, $h); $g.Dispose(); $bmp.Dispose()
  $small.Save("$S\$name.png"); $small.Dispose()
  [IO.File]::Delete($full)
}
function EditorOpen { (Hit 150 185) -match 'TlToyboxEditor' }
function CloseMenu { if ((Hit 958 391) -match "GameMemu") { "menu was open: " + (Rc "click 958 391" 800) } }
function OpenEditor { CloseMenu; if (-not (EditorOpen)) { & "$T\mod.ps1" -Cmd "editor" | Out-Null; [Threading.Thread]::Sleep(800) } }

(& "$T\mod.ps1" -Cmd "setgrade $Prev").Substring(0, 80)
OpenEditor
Rc 'click 150 289' | Out-Null
Rc 'click 356 332' | Out-Null   # 수련치 채우기
$openY = 164 + 334 + $Rows * 44 + 6 + 22
for ($i = 0; $i -lt $Rows; $i++) {
  if ($Only.Count -gt 0 -and $Only -notcontains $i) { continue }
  OpenEditor
  Rc 'click 150 289' | Out-Null
  $giveY = 164 + 334 + $i * 44 + 20
  $give = Rc "click 1390 $giveY"
  if ($give -notmatch 'MajorGive') { "row ${i}: give button not hit: $give"; continue }
  if ($i -eq 0 -or $Only.Count -gt 0) { Shot "$Tag-editor" }
  (& "$T\mod.ps1" -Cmd "closeui") | Out-Null; [Threading.Thread]::Sleep(2000); OpenEditor; Rc "click 150 289" | Out-Null
  $open = Rc "click 406 $openY" 12000
  if ($open -notmatch 'MajorOpen') { "row ${i}: open button not hit: $open"; continue }
  # 카드 위치를 찾는다.
  $cards = @(); $inCard = $false; $start = 0
  for ($x = 100; $x -le 1820; $x += 20) {
    $isCard = (Hit $x 369) -match 'goGradeItem'
    if ($isCard -and -not $inCard) { $start = $x; $inCard = $true }
    if (-not $isCard -and $inCard) { $cards += [int](($start + $x - 30) / 2); $inCard = $false }
  }
  "row ${i}: cards at " + ($cards -join ',')
  if ($cards.Count -le $i) { Shot "$Tag-$i-nocard"; continue }
  Rc "click $($cards[$i]) 369" 2500 | Out-Null
  # 자동 투입 버튼 두 개를 찾아 누른다.
  $pressed = @()
  foreach ($y in 700..1000 | Where-Object { $_ % 20 -eq 0 }) {
    foreach ($x in 1200, 1308, 1400) {
      $h = Hit $x $y
      if ($h -match 'btnAddRarity' -and $pressed -notcontains 'rarity') { Rc "click $x $y" 1200 | Out-Null; $pressed += 'rarity' }
      if ($h -match 'btnAddRequire' -and $pressed -notcontains 'require') { Rc "click $x $y" 1200 | Out-Null; $pressed += 'require' }
    }
  }
  CloseMenu
  "row ${i}: auto-insert pressed: " + ($pressed -join ',')
  Shot "$Tag-$i"
  # 돌파 화면을 닫는다.
  CloseMenu
  (& "$T\mod.ps1" -Cmd "closeui") | Out-Null
  [Threading.Thread]::Sleep(1500)
  CloseMenu
}
