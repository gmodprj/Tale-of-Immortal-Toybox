# tools/workshop.ps1의 시험. 게임·SteamCMD·실제 세이브·네트워크를 건드리지 않고 임시 폴더에서 한다.
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
  [IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false)))
}
# 받은 폴더·게임 폴더·목록·가짜 SteamCMD·저장된 웹 응답(web)을 흉내 낸 임시 폴더 한 벌.
#   111: 평평한 것(DLL 하나, 설치)  222: debug 모양(설치)  333: debug 안에 Mod_*가 둘(설치)  444: 받지 않음(설치)  555: 폴더만 있고 cache 없음(설치)
#   666: 보류(받지 않음)  777: 제외(받지 않았지만 게임 폴더에는 설치돼 있음)
function NewBox {
  $root = Join-Path $env:TEMP ('tltoybox-workshop-tests-' + [guid]::NewGuid().ToString('N'))
  $box = [pscustomobject]@{
    root = $root; content = "$root\steam\steamapps\workshop\content\1468810"; steamcmd = "$root\steam\steamcmd.ps1"
    game = "$root\game"; list = "$root\mods.txt"; acf = "$root\steam\steamapps\workshop\appworkshop_1468810.acf"
    history = "$root\history.txt"; web = "$root\web" }
  WriteFile "$($box.game)\ModExportData\Mod_sWP6UL\ModExportData.cache" 'ours'
  WriteFile "$($box.game)\ModExportData\unknown\ModExportData.cache" 'not in the list'
  WriteFile "$($box.game)\ModExportData\777\ModExportData.cache" 'c777'
  WriteFile "$($box.content)\111\ModExportData.cache" 'c111'
  WriteFile "$($box.content)\111\ModAssets\a.png" 'png'
  WriteFile "$($box.content)\111\ModCode\dll\MOD_x.dll" 'dll'
  WriteFile "$($box.content)\222\debug\Mod_abc\ModExportData.cache" 'c222'
  WriteFile "$($box.content)\222\debug\Mod_abc\ModAssets\b.png" 'png'
  WriteFile "$($box.content)\222\ModProject\ModData.cache" 'project'
  WriteFile "$($box.content)\333\debug\Mod_a\ModExportData.cache" 'c333a'
  WriteFile "$($box.content)\333\debug\Mod_b\ModExportData.cache" 'c333b'
  [void][IO.Directory]::CreateDirectory("$($box.content)\555")
  [void][IO.Directory]::CreateDirectory($box.web)
  WriteFile $box.list "# 시험용`n111 | 설치 | 외형 | one | 하나 | - | DLL 1`n222 | 설치 | 외형 | two | 둘 | - | debug`n333 | 설치 | 외형 | three | 셋 | - |`n444 | 설치 | 공법 | four | 넷 | - |`n555 | 설치 | 공법 | five | 다섯 | - |`n666 | 보류 | - | - | - | - | 시험용 보류`n777 | 제외 | 외형 | seven | 일곱 | - | 제외 사유`n"
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
    $out = & $tool -GameDir $box.game -SteamCmd $box.steamcmd -ContentDir $box.content -List $box.list -History $box.history -WebRoot $box.web -ProcessName $process -TimeoutMinutes 1 @more
    ($out -join "`n")
  } catch { 'THROWN: ' + $_.Exception.Message }
}
function Tree([string]$root) {
  (Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($root.Length) + '=' + [IO.File]::ReadAllText($_.FullName) }) -join ';'
}
function HistoryLines($box) { if (Test-Path -LiteralPath $box.history) { @([IO.File]::ReadAllLines($box.history)) } else { @() } }
function ReadIf([string]$p) { if (Test-Path -LiteralPath $p) { [IO.File]::ReadAllText($p) } else { '' } }   # 구현 전(파일 없음)에도 시험이 멈추지 않게
$when = '\d{4}-\d\d-\d\d \d\d:\d\d'

# 1. -Status: 줄마다 상태·받음·설치·갱신·DLL, 마지막 줄의 요약
$b = NewBox
$o = Run $b $none @{ Status = $true }
Check 'Status shows a downloaded flat item with state, size, date and dll count' ($o -match '111 \| 설치 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| - \| - \| 1') $o
Check 'Status shows a debug item without a date' ($o -match '222 \| 설치 \| 외형 \| 둘 \| 0(\.0)?MB - \| - \| - \| 0') $o
Check 'Status shows a missing item with dashes' ($o -match '444 \| 설치 \| 공법 \| 넷 \| - \| - \| - \| -') $o
Check 'Status counts a folder without a cache as not downloaded' ($o -match '555 \| 설치 \| 공법 \| 다섯 \| - \| - \| - \| -') $o
Check 'Status shows a held row with dashes for kind and name' ($o -match '666 \| 보류 \| - \| - \| - \| - \| - \| -') $o
Check 'Status shows an excluded row that is installed' ($o -match '777 \| 제외 \| 외형 \| 일곱 \| - \| 예 \| - \| -') $o
Check 'Status ends with the summary over install rows and counts held and excluded rows' ($o -match '받음 3/5, 설치 0/5 \| 보류 1 \| 제외 1') $o

# 2. -Id는 목록의 ID만 받는다
$o = Run $b $none @{ Status = $true; Id = '111,222' }
Check 'Id narrows the rows' ($o -match '받음 2/2, 설치 0/2' -and -not ($o -match '(?m)^444')) $o
$o = Run $b $none @{ Status = $true; Id = '999' }
Check 'an unknown Id is refused' ($o -match '^THROWN.*목록에 없는 ID') $o

# 3. 깨진 목록 줄은 시작 전에 멈춘다
$b = NewBox
[IO.File]::AppendAllText($b.list, "abc | 설치 | 외형 | bad | 나쁨 | - |`n")
$o = Run $b $none @{ Status = $true }
Check 'a broken list line stops the tool before it does anything' ($o -match '^THROWN.*목록의 줄을 읽지 못했습니다') $o
$b = NewBox
[IO.File]::AppendAllText($b.list, "888 | 외형 | old | 옛 | 다섯 칸`n")
$o = Run $b $none @{ Status = $true }
Check 'a five-cell line is refused' ($o -match '^THROWN.*목록의 줄을 읽지 못했습니다') $o
$b = NewBox
[IO.File]::AppendAllText($b.list, "888 | 대기 | 외형 | x | 엑스 | - |`n")
$o = Run $b $none @{ Status = $true }
Check 'an unknown state is refused' ($o -match '^THROWN.*모르는 상태') $o
$b = NewBox
[IO.File]::AppendAllText($b.list, "888 | 설치 | 기타 | x | 엑스 | - |`n")
$o = Run $b $none @{ Status = $true }
Check 'an unknown kind is refused' ($o -match '^THROWN.*모르는 종류') $o
$b = NewBox
[IO.File]::AppendAllText($b.list, "888 | 설치 | 외형 | - | - | - |`n")
$o = Run $b $none @{ Status = $true }
Check 'a dash name on an install row is refused' ($o -match '^THROWN.*이름을 비울 수 없습니다') $o

# 4. 동작은 하나만 고른다
$b = NewBox
$o = Run $b $none @{ }
Check 'no action is refused' ($o -match '^THROWN.*하나를 고릅니다') $o
$o = Run $b $none @{ Status = $true; Install = $true }
Check 'two actions are refused' ($o -match '^THROWN.*하나를 고릅니다') $o

# 5. -Install(계획만): 설치 줄만, 원본을 고르고 이유를 보이되 아무것도 쓰지 않는다
$b = NewBox
$before = Tree $b.game
$o = Run $b $none @{ Install = $true }
Check 'install plan lists the flat item with its dll' ($o -match '111 \| 하나 \| 평평함 \| 0(\.0)?MB \| DLL MOD_x\.dll \| 새로 설치') $o
Check 'install plan lists the debug item' ($o -match '222 \| 둘 \| debug \| 0(\.0)?MB \| DLL - \| 새로 설치') $o
Check 'install plan skips an item with two Mod_ folders' ($o -match '333 \| 셋 \| 건너뜀: debug 안에 Mod_\* 폴더가 2개') $o
Check 'install plan skips an item that is not downloaded' ($o -match '444 \| 넷 \| 건너뜀: 받지 않음') $o
Check 'a folder without a cache is skipped with the reason' ($o -match '555 \| 다섯 \| 건너뜀: ModExportData.cache가 없음') $o
Check 'install plan leaves out held and excluded rows' (-not ($o -match '(?m)^666') -and -not ($o -match '(?m)^777')) $o
Check 'install without -Yes writes nothing, not even history' ($o -match 'PLAN ONLY' -and (Tree $b.game) -eq $before -and (HistoryLines $b).Count -eq 0) $o
$o = Run $b $none @{ Install = $true; Id = '777' }
Check 'install refuses -Id on a held or excluded row' ($o -match '^THROWN.*제외 상태의 ID는 설치하지 않습니다') $o
$o = Run $b $none @{ Install = $true; Id = '666' }
Check 'install refuses -Id on a held row' ($o -match '^THROWN.*보류 상태의 ID는 설치하지 않습니다') $o

# 6. -Install -Yes: 평평한 것은 그대로, debug 모양은 Mod_* 안의 내용만. 이력에 설치 줄
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'install -Yes copies the flat item as it is' ($o -match 'INSTALLED 111 하나' -and [IO.File]::ReadAllText("$($b.game)\ModExportData\111\ModExportData.cache") -eq 'c111' -and (Test-Path "$($b.game)\ModExportData\111\ModCode\dll\MOD_x.dll")) $o
Check 'install -Yes flattens the debug item' ($o -match 'INSTALLED 222 둘' -and [IO.File]::ReadAllText("$($b.game)\ModExportData\222\ModExportData.cache") -eq 'c222' -and (Test-Path "$($b.game)\ModExportData\222\ModAssets\b.png") -and -not (Test-Path "$($b.game)\ModExportData\222\debug") -and -not (Test-Path "$($b.game)\ModExportData\222\ModProject")) $o
Check 'install -Yes ends with the count' ($o -match '설치 2개') $o
$h = HistoryLines $b
Check 'install -Yes appends a history line per installed item' ($h.Count -eq 2 -and $h[0] -match "^$when \| 설치 \| 111 \| 하나$" -and $h[1] -match "^$when \| 설치 \| 222 \| 둘$") ($h -join ';')
$o = Run $b $none @{ Status = $true }
Check 'Status shows installed items as identical' ($o -match '111 \| 설치 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| 예 \| 같음 \| 1' -and $o -match '받음 3/5, 설치 2/5') $o

# 7. 같은 것을 다시 넣으면 UNCHANGED(이력 없음), 원본이 바뀌면 UPDATED(이력 갱신. 게임이 켜져 있으면 그 줄만 거부). -NoHistory
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'a second install -Yes changes nothing and writes no history' ($o -match 'UNCHANGED 111 하나' -and $o -match 'UNCHANGED 222 둘' -and $o -match '설치 0개' -and (HistoryLines $b).Count -eq 2) $o
WriteFile "$($b.content)\111\ModAssets\new.png" 'new'
$o = Run $b $none @{ Status = $true }
Check 'Status notices a changed source' ($o -match '111 \| 설치 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| 예 \| 다름 \d+ \| 1') $o
WriteFile "$($b.content)\444\ModExportData.cache" 'c444'
$o = Run $b $busy @{ Install = $true; Yes = $true }
Check 'a running game blocks updating an installed folder' ($o -match '111 \| 하나 .*거부: 게임이 실행 중' -and -not (Test-Path "$($b.game)\ModExportData\111\ModAssets\new.png")) $o
Check 'a running game still lets a new folder in' ($o -match 'INSTALLED 444 넷' -and (Test-Path "$($b.game)\ModExportData\444\ModExportData.cache") -and $o -match '다음 게임 시작부터') $o
$o = Run $b $none @{ Install = $true; Yes = $true; Id = '111' }
Check 'with the game closed the changed item is updated and the history says so' ($o -match 'UPDATED 111 하나' -and (Test-Path "$($b.game)\ModExportData\111\ModAssets\new.png") -and (HistoryLines $b)[-1] -match "^$when \| 갱신 \| 111 \| 하나$") $o
WriteFile "$($b.content)\111\ModAssets\new2.png" 'new2'
$count = (HistoryLines $b).Count
$o = Run $b $none @{ Install = $true; Yes = $true; Id = '111'; NoHistory = $true }
Check '-NoHistory updates without a history line' ($o -match 'UPDATED 111 하나' -and (HistoryLines $b).Count -eq $count) $o

# 8. -Uninstall: 계획만, 게임이 켜져 있으면 거부, -Yes는 목록 ID 폴더만(제외 줄도) 지우고 이력에 지움 줄
$before = Tree $b.game
$o = Run $b $none @{ Uninstall = $true }
Check 'uninstall plan lists installed items of any state and writes nothing' ($o -match '111 \| 하나 \| 지움 0(\.0)?MB' -and $o -match '777 \| 일곱 \| 지움 0(\.0)?MB' -and $o -match '333 \| 셋 \| 설치돼 있지 않음' -and $o -match 'PLAN ONLY' -and (Tree $b.game) -eq $before) $o
$o = Run $b $busy @{ Uninstall = $true; Yes = $true }
Check 'uninstall is refused while the game runs' ($o -match '^THROWN.*게임이 실행 중' -and (Tree $b.game) -eq $before) $o
$o = Run $b $none @{ Uninstall = $true; Yes = $true }
Check 'uninstall -Yes removes the listed folders' ($o -match 'REMOVED 111 하나' -and $o -match 'REMOVED 222 둘' -and $o -match 'REMOVED 444 넷' -and $o -match 'REMOVED 777 일곱' -and $o -match '지움 4개' -and -not (Test-Path "$($b.game)\ModExportData\111") -and -not (Test-Path "$($b.game)\ModExportData\777")) $o
Check 'uninstall leaves the editor folder and a folder that is not in the list' ((Test-Path "$($b.game)\ModExportData\Mod_sWP6UL\ModExportData.cache") -and (Test-Path "$($b.game)\ModExportData\unknown\ModExportData.cache")) $o
Check 'uninstall appends a history line per removed folder' ((HistoryLines $b)[-1] -match "^$when \| 지움 \| 777 \| 일곱$") ((HistoryLines $b) -join ';')

# 9. 게임 폴더가 아니면 설치하지 않는다
$b = NewBox
[IO.Directory]::Delete("$($b.game)\ModExportData", $true)
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'install refuses a folder without ModExportData' ($o -match '^THROWN.*ModExportData') $o

# 10. -Download: 설치 줄 가운데 받지 않은 것만 받고, 실패한 것은 다시 시도한다(가짜 SteamCMD는 첫 호출에서 마지막 항목을 실패시킨다). 이력에 받음 줄
$b = NewBox
$o = Run $b $none @{ Download = $true }
Check 'download asks only for the missing install rows' ($o -match 'pass 1: 2개' -and -not (Test-Path "$($b.content)\666") -and -not (Test-Path "$($b.content)\777")) $o
Check 'download reports the failed item and retries' ($o -match 'FAIL 555 다섯' -and $o -match 'pass 2: 1개' -and $o -match 'OK   555 다섯') $o
Check 'download picks up a folder without a cache' ((Test-Path "$($b.content)\555\ModExportData.cache") -and [IO.File]::ReadAllText("$($b.content)\555\ModExportData.cache") -eq 'c555') $o
Check 'download creates the missing folder' ((Test-Path "$($b.content)\444\ModExportData.cache") -and $o -match 'OK   444 넷') $o
Check 'download ends with the count' ($o -match '받음 5/5') $o
$h = HistoryLines $b
Check 'download appends a history line per downloaded item' ($h.Count -eq 2 -and $h[0] -match "^$when \| 받음 \| 444 \| 넷$" -and $h[1] -match "^$when \| 받음 \| 555 \| 다섯$") ($h -join ';')
$o = Run $b $none @{ Download = $true }
Check 'download with nothing missing does not run steamcmd' ($o -match '받음 5/5 \(받을 것이 없습니다\)') $o
$o = Run $b $none @{ Download = $true; Id = '111' }
Check 'download with -Id re-downloads that item' ($o -match 'pass 1: 1개' -and $o -match 'OK   111 하나') $o
$o = Run $b $none @{ Download = $true; Id = '666' }
Check 'download with -Id downloads a held row' ($o -match 'OK   666 -' -and (Test-Path "$($b.content)\666\ModExportData.cache")) $o
$o = Run $b $none @{ Download = $true; Id = '777' }
Check 'download with -Id refuses an excluded row' ($o -match '^THROWN.*제외한 ID입니다' -and -not (Test-Path "$($b.content)\777")) $o

# 11. 로그인이 풀려 있으면 멈추고 사용자가 칠 명령을 보인다
$b = NewBox
WriteFile "$($b.root)\steam\expired.flag" ''
$o = Run $b $none @{ Download = $true }
Check 'an expired login stops the download and shows the login command' ($o -match '로그인이 풀려 있습니다' -and $o -match '\+login deepestlight \+quit' -and -not (Test-Path "$($b.content)\444")) $o

# 12. -Search: 저장해 둔 응답(-WebRoot)으로 후보표를 만든다. 네트워크는 쓰지 않는다
$b = NewBox
function Links([string[]]$ids) { '<html><body>' + (($ids | ForEach-Object { '<a href="https://steamcommunity.com/sharedfiles/filedetails/?id=' + $_ + '&searchtext=">x</a>' }) -join "`n") + '</body></html>' }
function Det([string]$id, [string]$title, [long]$updated, [string]$size, [string[]]$tags, [bool]$banned = $false, [string]$desc = '설명') {
  @{ publishedfileid = $id; result = 1; title = $title; description = $desc; time_updated = $updated; file_size = $size; lifetime_subscriptions = 1000; favorited = 10; visibility = 0; banned = $banned; tags = @($tags | ForEach-Object { @{ tag = $_ } }) }
}
WriteFile "$($b.web)\browse-Beautify-p1.html" (Links @('111', '777', '1001', '1002', '1003', '1004', '1005'))
WriteFile "$($b.web)\browse-Beautify-p2.html" '<html><body>no items</body></html>'
WriteFile "$($b.web)\browse-Battle-p1.html" (Links @('2001', '2002', '1001'))
WriteFile "$($b.web)\browse-Battle-p2.html" '<html></html>'
WriteFile "$($b.web)\browse-功法-p1.html" (Links @('2003'))
WriteFile "$($b.web)\browse-功法-p2.html" '<html></html>'
WriteFile "$($b.web)\browse-Drama-p1.html" (Links @('3001'))
WriteFile "$($b.web)\browse-Drama-p2.html" '<html></html>'
WriteFile "$($b.web)\browse-NPC-p1.html" '<html></html>'
WriteFile "$($b.web)\browse-LargeIntegrated-p1.html" '<html></html>'
$dets = @(
  (Det '111' 'one' 1735000000 '1000' @('Beautify')), (Det '777' 'seven' 1735000000 '1000' @('Beautify')),
  (Det '1001' '双修立绘' 1735000000 '1000' @('Beautify') $false ('가나다' * 100)),
  (Det '1002' 'old one' 1600000000 '1000' @('Beautify')),
  (Det '1003' 'big one' 1735000000 '300000000' @('Beautify')),
  (Det '1004' 'banned one' 1735000000 '1000' @('Beautify') $true),
  (Det '2001' '功法升级' 1735000000 '1000' @('Battle')),
  (Det '2002' 'big world' 1735000000 '1000' @('Battle', 'LargeIntegrated')),
  (Det '2003' '【x】<color=#FF0000>功法</color>' 1735000000 '1000' @('Battle')),
  (Det '3001' 'drama' 1735000000 '1000' @('Drama')))
WriteFile "$($b.web)\details.json" (@{ response = @{ result = 1; resultcount = $dets.Count; publishedfiledetails = $dets } } | ConvertTo-Json -Depth 6)
WriteFile "$($b.web)\item-1001.html" '<div class="requiredItemsContainer" id="RequiredItems"><a href="https://steamcommunity.com/workshop/filedetails/?id=2814703549" target="_blank"><div class="requiredItem">Fatury   </div></a></div>'
WriteFile "$($b.web)\item-1002.html" '<html><body>no required block</body></html>'
foreach ($id in @('2001', '2002', '2003', '3001')) { WriteFile "$($b.web)\item-$id.html" '<html></html>' }
$out = "$($b.root)\cand.md"
$o = Run $b $none @{ Search = $true; Out = $out }
$t = if (Test-Path $out) { [IO.File]::ReadAllText($out) } else { '' }
Check 'search writes a table per kind with the candidate count' ($t -match '(?m)^## 외형 \(3개 후보\)' -and $t -match '(?m)^## 공법 \(3개 후보\)' -and $t -match '(?m)^## 세계관 \(1개 후보\)' -and $t -match '(?m)^## 음악 \(0개 후보\)') $t
Check 'search marks listed ids with their state and gives them no rank' ($t -match '\| - \| 111 \|.*목록에 있음\(설치\)' -and $t -match '\| - \| 777 \|.*목록에 있음\(제외\)') $t
Check 'search drops a banned item and an item the details api did not return' (-not ($t -match '\| 1004 \|') -and -not ($t -match '\| 1005 \|')) $t
Check 'search flags adult, old, big, system and large items' ($t -match '\| 1001 \|.*\| 성인향\? \|' -and $t -match '\| 1002 \|.*\| 오래됨 \|' -and $t -match '\| 1003 \|.*\| 큼 \|' -and $t -match '\| 2001 \|.*\| 시스템\? \|' -and $t -match '\| 2002 \|.*\| 대형 \|') $t
Check 'search reads required items, a page without the block and a missing page' ($t -match '\| 1001 \|(?: [^|]* \|){6} 2814703549 Fatury \|' -and $t -match '\| 1002 \|(?: [^|]* \|){6} - \|' -and $t -match '\| 1003 \|(?: [^|]* \|){6} 조회 실패 \|') $t
$gf = if ($t.Contains('## 공법') -and $t.Contains('## 세계관')) { $t.Substring($t.IndexOf('## 공법'), $t.IndexOf('## 세계관') - $t.IndexOf('## 공법')) } else { '' }
Check 'search does not repeat an id an earlier kind took and joins the queries of a kind in order' (-not ($gf -match '\| 1001 \|') -and $gf -match '\| 1 \| 2001 \|' -and $gf -match '\| 3 \| 2003 \|') $gf
Check 'search strips formatting tags from the title' ($t -match '\| 2003 \| 【x】功法 \|') $t
Check 'search puts the first 200 characters of the description under the table' ($t -match '(?m)^- 1001: (?:가나다){66}가나…$') $t
Check 'search writes the description file next to the table' ((Test-Path "$($b.root)\cand-desc.txt") -and ([IO.File]::ReadAllText("$($b.root)\cand-desc.txt")) -match "(?m)^1001`t") $t
Check 'search prints a line per kind and the file name' ($o -match '외형: 후보 3, 목록에 있음 2' -and $o -match '후보표: ') $o
$o = Run $b $none @{ Search = $true; Out = $out; Top = 2 }
$t = ReadIf $out
Check 'search stops a kind at -Top' ($t -match '(?m)^## 외형 \(2개 후보\)' -and -not ($t -match '\| 1003 \|')) $t
$o = Run $b $none @{ Search = $true; Out = $out; Kind = '외형,기타' }
Check 'an unknown kind is refused' ($o -match '^THROWN.*모르는 종류') $o
WriteFile "$($b.web)\browse-NPC-p1.html" '<html><body>您最近作出的请求太多了</body></html>'
$o = Run $b $none @{ Search = $true; Out = $out }
$t = ReadIf $out
Check 'search stops at the rate limit, keeps what it has, names the place and does not fetch required items' ($t -match '요청 제한으로 멈춤: NPC 1쪽' -and $t -match '\| 3001 \|' -and $t -match '(?m)^## 음악 \(요청 제한으로 읽지 못함\)' -and $t -match '\| 1001 \|(?: [^|]* \|){6} 조회 안 함\(요청 제한\) \|') $t

foreach ($root in $boxes) {
  if ($root.StartsWith((Join-Path $env:TEMP 'tltoybox-workshop-tests-')) -and (Test-Path $root)) { [IO.Directory]::Delete($root, $true) }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
exit 0
