# tools/workshop.ps1의 시험. 게임·SteamCMD·실제 세이브를 건드리지 않고 임시 폴더에서 한다.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다.
$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot 'workshop.ps1'
$script:fails = 0
$none = 'no-such-process-tltoybox'            # 실행 중인 게임이 없는 상황
$busy = (Get-Process -Id $PID).ProcessName    # 실행 중인 게임이 있는 상황(이 시험을 돌리는 프로세스의 이름을 빌린다)
$boxes = New-Object System.Collections.Generic.List[string]

function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
function WriteFile([string]$path, [string]$text) {
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
  [IO.File]::WriteAllText($path, $text)
}
# 받은 폴더·게임 폴더·목록·가짜 SteamCMD를 흉내 낸 임시 폴더 한 벌.
#   111: 평평한 것(DLL 하나)  222: debug 모양  333: debug 안에 Mod_*가 둘  444: 받지 않음  555: 폴더만 있고 cache 없음
function NewBox {
  $root = Join-Path $env:TEMP ('tltoybox-workshop-tests-' + [guid]::NewGuid().ToString('N'))
  $box = [pscustomobject]@{
    root = $root; content = "$root\steam\steamapps\workshop\content\1468810"; steamcmd = "$root\steam\steamcmd.ps1"
    game = "$root\game"; list = "$root\mods.txt"; acf = "$root\steam\steamapps\workshop\appworkshop_1468810.acf" }
  WriteFile "$($box.game)\ModExportData\Mod_sWP6UL\ModExportData.cache" 'ours'
  WriteFile "$($box.game)\ModExportData\unknown\ModExportData.cache" 'not in the list'
  WriteFile "$($box.content)\111\ModExportData.cache" 'c111'
  WriteFile "$($box.content)\111\ModAssets\a.png" 'png'
  WriteFile "$($box.content)\111\ModCode\dll\MOD_x.dll" 'dll'
  WriteFile "$($box.content)\222\debug\Mod_abc\ModExportData.cache" 'c222'
  WriteFile "$($box.content)\222\debug\Mod_abc\ModAssets\b.png" 'png'
  WriteFile "$($box.content)\222\ModProject\ModData.cache" 'project'
  WriteFile "$($box.content)\333\debug\Mod_a\ModExportData.cache" 'c333a'
  WriteFile "$($box.content)\333\debug\Mod_b\ModExportData.cache" 'c333b'
  [void][IO.Directory]::CreateDirectory("$($box.content)\555")
  WriteFile $box.list "# 시험용`n111 | 외형 | one | 하나 | DLL 1`n222 | 외형 | two | 둘 | debug`n333 | 외형 | three | 셋 |`n444 | 공법 | four | 넷 |`n555 | 공법 | five | 다섯 |`n"
  WriteFile $box.acf "`"AppWorkshop`"`n{`n`t`"WorkshopItemDetails`"`n`t{`n`t`t`"111`"`n`t`t{`n`t`t`t`"manifest`"`t`t`"1`"`n`t`t`t`"timeupdated`"`t`t`"1735353779`"`n`t`t}`n`t}`n}`n"
  # 가짜 SteamCMD. +runscript 파일의 workshop_download_item 줄마다 폴더를 만들고 Success 줄을 찍는다.
  # 첫 호출에서는 마지막 항목을 failed로 찍는다(재시도 확인). expired.flag가 있으면 로그인이 풀린 출력만 찍는다.
  WriteFile $box.steamcmd @'
$content = Join-Path $PSScriptRoot 'steamapps\workshop\content\1468810'
$marker = Join-Path $PSScriptRoot 'called.txt'
'Steam Console Client (fake)'
if (Test-Path (Join-Path $PSScriptRoot 'expired.flag')) { 'Cached credentials not found'; 'FAILED (Invalid Password)'; exit 0 }
$i = [array]::IndexOf($args, '+runscript')
$lines = @(Get-Content -LiteralPath $args[$i + 1] | Where-Object { $_ -match '^workshop_download_item' })
$first = -not (Test-Path $marker)
[IO.File]::WriteAllText($marker, '')
for ($k = 0; $k -lt $lines.Count; $k++) {
  $id = ($lines[$k] -split ' ')[2]
  if ($first -and $k -eq $lines.Count - 1) { "ERROR! Download item $id failed (Failure)."; continue }
  [void][IO.Directory]::CreateDirectory("$content\$id")
  [IO.File]::WriteAllText("$content\$id\ModExportData.cache", "c$id")
  "Success. Downloaded item $id to `"$content\$id`" (10 bytes)"
}
'@
  $boxes.Add($root)
  $box
}
# workshop.ps1을 상자 안에서 돌린다. 던진 오류는 "THROWN: ..." 문자열로 돌려준다.
function Run($box, [string]$process, [hashtable]$more) {
  try {
    $out = & $tool -GameDir $box.game -SteamCmd $box.steamcmd -ContentDir $box.content -List $box.list -ProcessName $process -TimeoutMinutes 1 @more
    ($out -join "`n")
  } catch { 'THROWN: ' + $_.Exception.Message }
}
function Tree([string]$root) {
  (Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($root.Length) + '=' + [IO.File]::ReadAllText($_.FullName) }) -join ';'
}

# 1. -Status: 줄마다 받음·설치·갱신·DLL, 마지막 줄의 요약
$b = NewBox
$o = Run $b $none @{ Status = $true }
Check 'Status shows a downloaded flat item with size, date and dll count' ($o -match '111 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| - \| - \| 1') $o
Check 'Status shows a debug item without a date' ($o -match '222 \| 외형 \| 둘 \| 0(\.0)?MB - \| - \| - \| 0') $o
Check 'Status shows a missing item with dashes' ($o -match '444 \| 공법 \| 넷 \| - \| - \| - \| -') $o
Check 'Status counts a folder without a cache as not downloaded' ($o -match '555 \| 공법 \| 다섯 \| - \| - \| - \| -') $o
Check 'Status ends with the summary' ($o -match '받음 3/5, 설치 0/5') $o

# 2. -Id는 목록의 ID만 받는다
$o = Run $b $none @{ Status = $true; Id = '111,222' }
Check 'Id narrows the rows' ($o -match '받음 2/2, 설치 0/2' -and -not ($o -match '(?m)^444')) $o
$o = Run $b $none @{ Status = $true; Id = '999' }
Check 'an unknown Id is refused' ($o -match '^THROWN.*목록에 없는 ID') $o

# 3. 깨진 목록 줄은 시작 전에 멈춘다
$b = NewBox
[IO.File]::AppendAllText($b.list, "abc | 외형 | bad`n")
$o = Run $b $none @{ Status = $true }
Check 'a broken list line stops the tool before it does anything' ($o -match '^THROWN.*목록의 줄을 읽지 못했습니다') $o

# 4. 동작은 하나만 고른다
$b = NewBox
$o = Run $b $none @{ }
Check 'no action is refused' ($o -match '^THROWN.*하나를 고릅니다') $o
$o = Run $b $none @{ Status = $true; Install = $true }
Check 'two actions are refused' ($o -match '^THROWN.*하나를 고릅니다') $o

# 5. -Install(계획만): 원본을 고르고 이유를 보이되 아무것도 쓰지 않는다
$b = NewBox
$before = Tree $b.game
$o = Run $b $none @{ Install = $true }
Check 'install plan lists the flat item with its dll' ($o -match '111 \| 하나 \| 평평함 \| 0(\.0)?MB \| DLL MOD_x\.dll \| 새로 설치') $o
Check 'install plan lists the debug item' ($o -match '222 \| 둘 \| debug \| 0(\.0)?MB \| DLL - \| 새로 설치') $o
Check 'install plan skips an item with two Mod_ folders' ($o -match '333 \| 셋 \| 건너뜀: debug 안에 Mod_\* 폴더가 2개') $o
Check 'install plan skips an item that is not downloaded' ($o -match '444 \| 넷 \| 건너뜀: 받지 않음') $o
Check 'a folder without a cache is skipped with the reason' ($o -match '555 \| 다섯 \| 건너뜀: ModExportData.cache가 없음') $o
Check 'install without -Yes writes nothing' ($o -match 'PLAN ONLY' -and (Tree $b.game) -eq $before) $o

# 6. -Install -Yes: 평평한 것은 그대로, debug 모양은 Mod_* 안의 내용만
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'install -Yes copies the flat item as it is' ($o -match 'INSTALLED 111 하나' -and [IO.File]::ReadAllText("$($b.game)\ModExportData\111\ModExportData.cache") -eq 'c111' -and (Test-Path "$($b.game)\ModExportData\111\ModCode\dll\MOD_x.dll")) $o
Check 'install -Yes flattens the debug item' ($o -match 'INSTALLED 222 둘' -and [IO.File]::ReadAllText("$($b.game)\ModExportData\222\ModExportData.cache") -eq 'c222' -and (Test-Path "$($b.game)\ModExportData\222\ModAssets\b.png") -and -not (Test-Path "$($b.game)\ModExportData\222\debug") -and -not (Test-Path "$($b.game)\ModExportData\222\ModProject")) $o
Check 'install -Yes ends with the count' ($o -match '설치 2개') $o
$o = Run $b $none @{ Status = $true }
Check 'Status shows installed items as identical' ($o -match '111 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| 예 \| 같음 \| 1' -and $o -match '받음 3/5, 설치 2/5') $o

# 7. 같은 것을 다시 넣으면 UNCHANGED, 원본이 바뀌면 UPDATED(게임이 켜져 있으면 그 줄만 거부)
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'a second install -Yes changes nothing' ($o -match 'UNCHANGED 111 하나' -and $o -match 'UNCHANGED 222 둘' -and $o -match '설치 0개') $o
WriteFile "$($b.content)\111\ModAssets\new.png" 'new'
$o = Run $b $none @{ Status = $true }
Check 'Status notices a changed source' ($o -match '111 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| 예 \| 다름 \d+ \| 1') $o
WriteFile "$($b.content)\444\ModExportData.cache" 'c444'
$o = Run $b $busy @{ Install = $true; Yes = $true }
Check 'a running game blocks updating an installed folder' ($o -match '111 \| 하나 .*거부: 게임이 실행 중' -and -not (Test-Path "$($b.game)\ModExportData\111\ModAssets\new.png")) $o
Check 'a running game still lets a new folder in' ($o -match 'INSTALLED 444 넷' -and (Test-Path "$($b.game)\ModExportData\444\ModExportData.cache") -and $o -match '다음 게임 시작부터') $o
$o = Run $b $none @{ Install = $true; Yes = $true; Id = '111' }
Check 'with the game closed the changed item is updated' ($o -match 'UPDATED 111 하나' -and (Test-Path "$($b.game)\ModExportData\111\ModAssets\new.png")) $o

# 8. -Uninstall: 계획만, 게임이 켜져 있으면 거부, -Yes는 목록 ID 폴더만 지운다
$before = Tree $b.game
$o = Run $b $none @{ Uninstall = $true }
Check 'uninstall plan lists installed items and writes nothing' ($o -match '111 \| 하나 \| 지움 0(\.0)?MB' -and $o -match '333 \| 셋 \| 설치돼 있지 않음' -and $o -match 'PLAN ONLY' -and (Tree $b.game) -eq $before) $o
$o = Run $b $busy @{ Uninstall = $true; Yes = $true }
Check 'uninstall is refused while the game runs' ($o -match '^THROWN.*게임이 실행 중' -and (Tree $b.game) -eq $before) $o
$o = Run $b $none @{ Uninstall = $true; Yes = $true }
Check 'uninstall -Yes removes the listed folders' ($o -match 'REMOVED 111 하나' -and $o -match 'REMOVED 222 둘' -and $o -match 'REMOVED 444 넷' -and $o -match '지움 3개' -and -not (Test-Path "$($b.game)\ModExportData\111") -and -not (Test-Path "$($b.game)\ModExportData\222")) $o
Check 'uninstall leaves the editor folder and a folder that is not in the list' ((Test-Path "$($b.game)\ModExportData\Mod_sWP6UL\ModExportData.cache") -and (Test-Path "$($b.game)\ModExportData\unknown\ModExportData.cache")) $o

# 9. 게임 폴더가 아니면 설치하지 않는다
$b = NewBox
[IO.Directory]::Delete("$($b.game)\ModExportData", $true)
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'install refuses a folder without ModExportData' ($o -match '^THROWN.*ModExportData') $o

# 10. -Download: 받지 않은 것만 받고, 실패한 것은 다시 시도한다(가짜 SteamCMD는 첫 호출에서 마지막 항목을 실패시킨다)
$b = NewBox
$o = Run $b $none @{ Download = $true }
Check 'download asks only for the missing items' ($o -match 'pass 1: 2개') $o
Check 'download reports the failed item and retries' ($o -match 'FAIL 555 다섯' -and $o -match 'pass 2: 1개' -and $o -match 'OK   555 다섯') $o
Check 'download picks up a folder without a cache' ((Test-Path "$($b.content)\555\ModExportData.cache") -and [IO.File]::ReadAllText("$($b.content)\555\ModExportData.cache") -eq 'c555') $o
Check 'download creates the missing folder' ((Test-Path "$($b.content)\444\ModExportData.cache") -and $o -match 'OK   444 넷') $o
Check 'download ends with the count' ($o -match '받음 5/5') $o
$o = Run $b $none @{ Download = $true }
Check 'download with nothing missing does not run steamcmd' ($o -match '받음 5/5 \(받을 것이 없습니다\)') $o
$o = Run $b $none @{ Download = $true; Id = '111' }
Check 'download with -Id re-downloads that item' ($o -match 'pass 1: 1개' -and $o -match 'OK   111 하나') $o

# 11. 로그인이 풀려 있으면 멈추고 사용자가 칠 명령을 보인다
$b = NewBox
WriteFile "$($b.root)\steam\expired.flag" ''
$o = Run $b $none @{ Download = $true }
Check 'an expired login stops the download and shows the login command' ($o -match '로그인이 풀려 있습니다' -and $o -match '\+login deepestlight \+quit' -and -not (Test-Path "$($b.content)\444")) $o

foreach ($root in $boxes) {
  if ($root.StartsWith((Join-Path $env:TEMP 'tltoybox-workshop-tests-')) -and (Test-Path $root)) { [IO.Directory]::Delete($root, $true) }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
exit 0
