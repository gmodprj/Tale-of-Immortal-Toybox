# 극 창의 빠진 배경 그림을 검은 바탕으로 두는지 확인(05 문서 95절). 회차(session.ps1 -Begin … -Fixture base) 안에서 돌린다.
# 음양쌍수의 그림 없는 대화 줄(id -1017852448, 배경 ttGCG8_2. 기연 창으로 열린다)과
# 만고신화의 그림 있는 대화 줄(대조, id 447266701. 큰 그림 대화 창)을 직접 연다. 선택지는 누르지 않고 닫는다. 캡처 check-drama-*.png.
# 고정본에 들어가면 만고신화의 첫 안내 이야기가 떠 있으므로 먼저 열린 극을 닫는다.
. 'E:\TlToybox\tools\research\check-lib.ps1'
$blank = '-1017852448'
$good = '447266701'

function Close-Drama {
  for ($i = 0; $i -lt 6; $i++) {
    $r = Cmd 'rdrama close' 300
    Start-Sleep -Milliseconds 800
    if ($r -match 'open=False') { return }
  }
}
function Open-Drama([string]$id) {
  Close-Drama
  Cmd "rdrama open $id" 300 | Out-Null
  Start-Sleep -Milliseconds 2500
  Cmd 'rdrama ui' 200
}

# 1. 고치기를 끈 원래 모습: 배경 Image에 그림이 없고 흰색이다(사용자가 본 흰 판).
Cmd 'rdrama fix off' 200 | Out-Null
$off = Open-Drama $blank
"OFF $off"
Shot 'drama-off'
Check 'the blank row opens the fortuitous window' ($off -match "fortuitous id=$blank bg=ttGCG8_2") $off
Check 'without the fix the background has no sprite and is white' ($off -match 'fortuitous [^|]*\| imgBG1 sprite=null color=1,1,1,[1-9]') $off

# 2. 고치기를 켠 모습: 같은 Image가 검게(알파는 그대로).
Cmd 'rdrama fix on' 200 | Out-Null
$on = Open-Drama $blank
"ON $on"
Shot 'drama-on'
Check 'with the fix the empty background is black' ($on -match 'fortuitous [^|]*\| imgBG1 sprite=null color=0,0,0,[1-9]') $on

# 3. 대조: 그림이 있는 배경은 색을 바꾸지 않는다.
$ok = Open-Drama $good
"GOOD $ok"
Shot 'drama-good'
Check 'a background with a picture keeps its sprite' ($ok -match 'bigtexture id=447266701[^|]*\| imgBG1 sprite=(?!null)') $ok
Check 'a background with a picture is not darkened' ($ok -match 'bigtexture id=447266701[^|]*\| imgBG1 sprite=[^|]*color=1,1,1') $ok

# 4. 대조: 그림이 있는 기연 창(접두사 없는 게임 배경 하나, 파일이 있는 모드 배경 하나)도 그대로다. rdrama fort가 고른 줄을 연다.
$samples = Cmd 'rdrama fort' 3000 30000
"FORT $samples"
foreach ($kind in @('vanilla', 'mod')) {
  if ($samples -match "$kind id=(-?\d+) bg=(\S+)") {
    $id = $Matches[1]; $bg = $Matches[2]
    $f = Open-Drama $id
    "OPEN $kind $f"
    Shot "drama-fort-$kind"
    Check "a fortuitous window with a picture ($kind $bg) keeps it" ($f -match "fortuitous id=$id[^|]*\| imgBG1 sprite=(?!null)[^|]*color=1,1,1") $f
  } else {
    Check "a fortuitous row with a $kind picture exists" $false $samples
  }
}
Close-Drama

Check 'no exception in button callbacks' ((Traps) -eq 0)
Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
Finish
