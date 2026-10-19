# 편집기 화면의 게임 내 검증(docs/superpowers/specs/2026-10-13-ui-rework-design.md 6절).
# tools\DevRemote\session.ps1 -Begin으로 게임이 떠서 세계에 들어가 있어야 한다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

# 탭 번호마다 그 탭에만 있는 버튼 하나(05 문서 47절: 번호는 고정이고 2는 비었다. 14는 제목 줄의 안전 단추). 탭이 제대로 그려졌는지 보는 데 쓴다.
$tabProbe = @(
  @(0, 'Restore'), @(1, 'BagApply'), @(3, 'VaultModeBag'), @(4, 'FillExp'), @(5, 'SkillAddOpen'), @(6, 'ImmortalAddOpen'), @(7, 'ArtifactPoint'),
  @(8, 'LuckAddOpen'), @(9, 'ArtLearnAll'), @(10, 'NpcRefresh'), @(11, 'Freeze'), @(12, 'God'), @(13, 'PresetRestore'), @(14, 'BackupNow'), @(15, 'AplClassAll'), @(16, 'TreasureGiveOpen'))
# 게임의 인물 화면을 열거나 닫고, UI 층의 맨 위 정렬 값이 그에 맞게 바뀔 때까지 기다린다(닫히는 연출의 길이가 그때그때 다르다).
function GameScreen([string]$do) {
  $want = if ($do -eq 'open') { 20 } else { 0 }
  Cmd "ui$do PlayerInfo" 300 | Out-Null
  for ($i = 0; $i -lt 25 -and (Cmd 'uistate' 100) -notmatch " top=$want "; $i++) { Start-Sleep -Milliseconds 200 }
  Start-Sleep -Milliseconds 300   # 편집기의 주기 처리가 정렬을 맞출 틈
}
# 전투에 들어간다. fight 명령은 "전투 진입" 확인 화면까지만 열므로 그 버튼을 누르고 장면이 바뀔 때까지 기다린다.
# 전투에서 나오는 방법은 자동화하지 않았다. 이 함수를 쓰는 부분은 회차의 마지막에 돌리고 바로 -End 한다.
function EnterBattle {
  Cmd 'fight 1011 1' 6000 | Out-Null
  $o = Rc 'click 960 684' 12000
  "INFO battle entry: $o"
  "INFO open game screens: $((Cmd 'uilist' 100) -replace "`n", ' |')"
}
# 창 위치를 바꾸는 시험(move 계열)은 세이브 폴더 밖의 config.txt를 고친다. 시험 전의 파일을 한 번만 떠 두고,
# 끝났을 때와 중간에 멈췄을 때 모두 되돌린다. 이미 떠 둔 것이 있으면(앞선 실행이 멈춘 것) 덮어쓰지 않는다.
function KeepConfig {
  $copy = Join-Path $work 'check-config.txt'
  if (-not (Test-Path -LiteralPath $copy)) { Copy-Item -LiteralPath (Join-Path $data 'config.txt') -Destination $copy }
}
function RestoreConfig {
  $copy = Join-Path $work 'check-config.txt'
  if (Test-Path -LiteralPath $copy) {
    Copy-Item -LiteralPath $copy -Destination (Join-Path $data 'config.txt') -Force
    [IO.File]::Delete($copy)
    'config.txt restored'
  }
}
trap { if (Get-Command RestoreConfig -ErrorAction SilentlyContinue) { RestoreConfig }; break }
function WinState {
  $o = Cmd 'uistate' 100
  if ($o -notmatch ' pos=(-?\d+),(-?\d+) collapsed=(\w+) ') { throw "uistate 출력을 읽지 못했습니다: $o" }
  [pscustomobject]@{ x = [int]$Matches[1]; y = [int]$Matches[2]; collapsed = ($Matches[3] -eq 'True'); raw = $o }
}
# 제목 줄의 보이는 빈 곳을 잡아 창의 왼쪽 위가 (x,y)에 오도록 끈다.
# 잡는 곳은 닫기 버튼 오른쪽의 틈(창 안 x=1492)이고, 그곳이 화면 밖이면 제목 글자 자리(x=150)다. 글자는 클릭을 받지 않아 바탕이 잡힌다.
function DragWindowTo([int]$x, [int]$y) {
  $w = WinState
  $gx = $w.x + 1492
  if ($gx -gt 1917) { $gx = $w.x + 150 }
  $gy = $w.y + 26
  Cmd "uidrag $gx $gy $($gx + $x - $w.x) $($gy + $y - $w.y) 5" 300
}

switch ($Part) {
  'groups' {
    # 자원 탭 개편 스펙 7절: 용도 그룹의 분류 표. 화면 쪽 확인은 Task 3에서 이 부분의 끝에 더한다.
    $traps = Traps
    $g = Cmd 'rgroups' 600
    $total = if ($g -match 'total=(\d+) ') { [int]$Matches[1] } else { -1 }
    $sum = if ($g -match ' sum=(\d+) ') { [int]$Matches[1] } else { -2 }
    $pill = if ($g -match ' Pill=(\d+)') { [int]$Matches[1] } else { -1 }
    $cloth = if ($g -match ' Cloth=(\d+)') { [int]$Matches[1] } else { -1 }
    Check 'every item lands in a named group (no Other in the plain game)' ($g -match ' other=0 ') $g
    Check 'the table has no duplicate class numbers' ($g -match ' dupes=0 ') $g
    Check 'the groups add up to the whole list' ($total -gt 3000 -and $sum -eq $total) $g
    Check 'pills are the biggest group' ($pill -gt 1000) $g
    Check 'clothes are their own group' ($cloth -gt 200) $g
    # 화면: 왼쪽 개수, 검색에 따른 개수, 의복의 종류, 기타, 전체의 안내
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupAll' | Out-Null; Press 'ResClear' | Out-Null
    Check 'the All button shows the total' ((Cmd 'uitext ResGroupAll') -match "text='전체 $total'") (Cmd 'uitext ResGroupAll')
    Check 'the Pill button shows its count' ((Cmd 'uitext ResGroupPill') -match "text='단약 $pill'") (Cmd 'uitext ResGroupPill')
    Check 'All lists everything' ((Cmd 'uitext ItemTableRange') -match "전체 $($total)개")
    Check 'All shows a hint instead of kinds' ((Cmd 'uipos ResKind0') -match 'no element named' -and (Cmd 'uipos ResAllHint') -match ' at ')
    # Review Focus 3: 실제 키로 치는 동안 왼쪽 개수가 바뀌고 검색 칸이 포커스를 잃지 않는다
    Cmd 'uifocus ItemQuery' | Out-Null
    Key 0x31 350; Key 0x30 350
    $typed = Cmd 'uitext ItemQuery'
    # 화면 밖 게임에 보낸 키가 통째로 빠질 때가 있다(인계 6절). 하나도 닿지 않았으면 한 번 다시 보낸다.
    if ($typed -match "text=''") {
      "INFO the keys did not reach the search box; sending them once more"
      Cmd 'uifocus ItemQuery' | Out-Null
      Key 0x31 350; Key 0x30 350
      $typed = Cmd 'uitext ItemQuery'
    }
    $left = Cmd 'uitext ResGroupAll'
    Check 'typing keeps the focus while counts change' ($typed -match "text='10'" -and $left -notmatch "text='전체 $total'") "$typed | $left"
    Press 'ItemQueryClear' | Out-Null
    Check 'clearing the search restores the counts' ((Cmd 'uitext ResGroupAll') -match "text='전체 $total'") (Cmd 'uitext ResGroupAll')
    Press 'ResGroupCloth' | Out-Null
    $k1 = Cmd 'uitext ResKind1'; $k2 = Cmd 'uitext ResKind2'
    Check 'clothes are split by sex' ($k1 -match "text='남 \(\d+\)'" -and $k2 -match "text='여 \(\d+\)'") "$k1 | $k2"
    Press 'ResKind1' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    Check 'a kind narrows the list' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -gt 0 -and [int]$Matches[1] -lt $cloth) $o
    Shot 'groups-cloth'
    Press 'ResGroupOther' | Out-Null
    Check 'Other is empty in the plain game' ((Cmd 'uitext ItemTableRange') -match "text='0개'")
    Press 'ResGroupAll' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'books' {
    # 기술 서적(비급) 지급 스펙 5절: 서비스. 가방을 채우는 검사가 끝에 있으므로 회차의 마지막에 돌린다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "$key=([^\s|]+)") { $Matches[1] } else { '' } }
    function BookCount { [int](Field (Cmd 'rbooks 23116' 300) 'count') }
    function BookLines { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ -match '비급 ' }).Count }
    $skill = Cmd 'rskill 1 23116' 300
    Check 'the skill table has 23116' ($skill -match 'min=\d+ max=\d+') $skill
    $min = [int](Field $skill 'min'); $max = [int](Field $skill 'max')
    $grade = [Math]::Max($min, [Math]::Min($max, 3))
    $before = BookCount
    $logged = BookLines
    $o = Cmd "rbook 1 23116 $grade 1 1" 3000
    $after = Cmd 'rbooks 23116' 300
    Check 'one book goes into the bag' ($o -match 'made=1 ' -and (BookCount) -eq $before + 1) "$o | $after"
    Check 'the new book is that skill and grade' ($after -match ":23116:$($grade):[1-6]:") $after
    $o = Cmd "rbook 1 23116 $grade 6 3" 20000
    $books = Cmd 'rbooks 23116' 300
    $six = @([regex]::Matches($books, ":23116:$($grade):6:([^,\s]+)") | ForEach-Object { $_.Groups[1].Value })
    Check 'three books of quality 6' ($o -match 'made=3 ' -and $six.Count -ge 3) "$o | $books"
    Check 'their options are not all the same' (@($six | Select-Object -Unique).Count -ge 2) ($six -join ' ')
    $out = if ($max -lt 10) { $max + 1 } else { $min - 1 }
    $count = BookCount
    $o = Cmd "rbook 1 23116 $out 1 1" 1000
    Check 'a grade outside the range is refused' ($o -match 'made=0 ' -and (BookCount) -eq $count) $o
    # Review Focus 3: 20권을 넘겨 청하면 20권과 안내
    $count = BookCount
    $o = Cmd "rbook 1 23116 $grade 1 25" 20000
    Check 'more than twenty gives twenty with a notice' ($o -match 'made=20 ' -and $o -match '20권까지' -and (BookCount) -eq $count + 20) $o
    Check 'the change log has a line per book' ((BookLines) -eq $logged + 24) "before=$logged after=$(BookLines)"
    $mine = @([regex]::Matches((Cmd 'rbooks 23116' 300), '(?:items=|,)([^:,]+):23116:') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -ne 'kEWxCN' })
    $study = if ($mine.Count -gt 0) { Cmd "rbookstudy $($mine[0])" 300 } else { '' }
    Check 'the game study accepts a made book' ($study -match 'can=0') "$($mine -join ',') | $study"
    # 검토에서 나온 것: 입문 무예·영술(경지 1 전용. 분류 전체의 묶음에서 나오지 않고 그 기술만의 묶음에서 나온다, 05 문서 24절)은
    # 그 묶음에서 품질 1로만 나온다(회차 book-1015l에서 2만 번). 품질 1은 여러 권 되고, 품질 2 이상은 이유와 함께 멈춘다
    $own = Cmd 'rownskills 0 1' 300
    $ownId = (Field $own 'ids').Split(',')[0]
    Check 'there is an entry skill with its own combine' ($ownId -match '^\d+$') $own
    $o = Cmd "rbook 0 $ownId 1 1 3" 6000
    $ownBooks = Cmd "rbooks $ownId" 300
    Check 'an entry skill gives three books of quality 1' ($o -match 'made=3 ' -and [regex]::Matches($ownBooks, ":$($ownId):1:1:").Count -ge 3) "$o | $ownBooks"
    $count = [int](Field $ownBooks 'count')
    $o = Cmd "rbook 0 $ownId 1 2 1" 20000
    Check 'an entry skill above quality 1 stops with the reason' ($o -match 'made=0 ' -and $o -match '품질 2 이상이 나오지 않았습니다' -and [int](Field (Cmd "rbooks $ownId" 300) 'count') -eq $count) $o
    # Review Focus 2: 가방이 찬 채로 청하면 멈추고 넣은 권수만 늘어난다
    Cmd 'gridfill 600 5' 6000 | Out-Null
    Check 'the bag is full' ((Cmd 'gridfill 1 5' 1500) -match 'given=0 ')
    $count = BookCount
    $o = Cmd "rbook 1 23116 $grade 1 5" 6000
    $made = [int](Field $o 'made')
    Check 'a full bag stops with the number put in' ($o -match '가방에 들어가지 않았습니다' -and (BookCount) -eq $count + $made) $o
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'translation' {
    # 한글화 스펙 5절. 창작마당 모드 3390738990이 설치돼 있어야 한다. 회차의 -End가 translations 폴더를 되돌린다.
    $traps = Traps
    $tdir = Join-Path $data 'translations'
    $tfile = Join-Path $tdir 'mods.txt'
    function Range { if ((Cmd 'uitext ItemTableRange') -match '전체 (\d+)개') { [int]$Matches[1] } elseif ((Cmd 'uitext ItemTableRange') -match "text='0개'") { 0 } else { -1 } }
    $s = Cmd 'rloc' 300
    # Init은 세이브 진입 전에 두 번 불릴 수 있다(인계 6절). 두 번째면 파일이 "있음"이고 exists 행은 이미 ch라 fallback이 0이다.
    # 50절: 3차 묶음 뒤 6275행(824 + 번역 5284 + 중국어 없는 행 167). 57절: 4차 243행을 더해 6518행. 6275행 파일이 있으면 243행, 옛 824행 파일이면 5694행이 덧붙는다.
    # fallback은 영어 플래그 1 + 원문으로 둔 3행(미성년 묘사) = 4. 두 번째 Init이면 플래그의 kr이 이미 ch라 3.
    Check 'translations are applied on init' ($s -match 'file=(풀어 둠|있음|덧붙임 243|덧붙임 5694) rows=6518 applied=6518 fallback=[34] unused=0 broken=0') $s
    Check 'the translation file was written to the data folder' (Test-Path -LiteralPath $tfile) $tfile
    $k = Cmd 'rloc key skill_name_middle984555698' 300
    Check 'a mod skill name resolves to Korean' ($k -match 'ls=열염장 kr=열염장 ch=烈炎掌') $k
    Check 'a mod term resolves to Korean' ((Cmd 'rloc key s_shenqiang' 300) -match 'ls=신창') (Cmd 'rloc key s_shenqiang' 300)
    Check 'a backslash-n becomes a line break' ((Cmd 'rloc key effect_descshenqiang' 300) -match '초 뒤\s*신창 상태에 들어가며') (Cmd 'rloc key effect_descshenqiang' 300)
    Check 'the English flag row falls back to its Chinese column' ((Cmd 'rloc key 0lkK4v' 300) -match 'ls=exists kr=exists') (Cmd 'rloc key 0lkK4v' 300)
    $mods = Cmd 'rmods' 3000
    Check 'the catalog shows Korean names without the ch fallback' ($mods -match '1:984555698:열염장:basPalm:False:' -and $mods -match '4:59898952:천살신창:' -and $mods -match '1:353535:장기광도:' -and $mods -match '1:844854565:풍선용권:') $mods
    $made = Cmd 'rbook 1 984555698 3 1 1' 20000
    Check 'a mod book is named in Korean' ($made -match 'made=1' -and $made -match '열염장\(품질') $made
    Check 'the bag list names the mod book in Korean' ((Cmd 'rbooks 984555698' 3000) -match ':984555698:[^,]*열염장') (Cmd 'rbooks 984555698' 3000)
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupManual' | Out-Null; Press 'ResClear' | Out-Null; Press 'ResKind6' | Out-Null
    # 모드 공법이 100개를 넘어(45절 뒤) 줄이 화면 밖에 있을 수 있으므로 먼저 검색해 그 줄만 남긴다
    Cmd 'ui set ItemQuery 열염' | Out-Null
    Check 'Korean search finds the mod skill' ((Range) -eq 1) (Cmd 'uitext ItemTableRange')
    Check 'the row shows the Korean name' ((Cmd 'uitext BookName984555698') -match "text='열염장'") (Cmd 'uitext BookName984555698')
    Press 'BookName984555698' | Out-Null
    $d = Cmd 'uitext ItemDetail'
    Check 'the box below no longer mentions a missing Korean name' ($d -notmatch '한글 이름이 없어' -and $d -match '열염장') $d
    Cmd 'ui set ItemQuery 烈炎' | Out-Null
    Check 'Chinese search still finds it' ((Range) -eq 1) (Cmd 'uitext ItemTableRange')
    Press 'ItemQueryClear' | Out-Null
    # 파일 수정 → rloc apply: 깨진 줄, 모르는 키, 기본 키, 공백 키, 한 줄 바꾸기
    $backup = [IO.File]::ReadAllText($tfile)
    $text = $backup.Replace("skill_name_middle984555698`t열염장", "skill_name_middle984555698`t열염장2") + "`nbroken line without tab`nno_such_key`t아무개`ns_liuxue`t엉뚱`n skill_name_middle353535 `t 장기광도3 `n"
    [IO.File]::WriteAllText($tfile, $text)
    $s = Cmd 'rloc apply' 3000
    Check 'a broken line and unknown keys are counted, the rest applies' ($s -match 'file=있음 rows=6520 applied=6518 fallback=0 unused=2 broken=1') $s
    Check 'an edited line wins' ((Cmd 'rloc key skill_name_middle984555698' 300) -match 'ls=열염장2') (Cmd 'rloc key skill_name_middle984555698' 300)
    Check 'a base key in the file is ignored and counted as unused' ((Cmd 'rloc key s_liuxue' 300) -match 'ls=유혈 kr=유혈') (Cmd 'rloc key s_liuxue' 300)
    Check 'spaces around a key are trimmed' ((Cmd 'rloc key skill_name_middle353535' 300) -match 'ls=장기광도3') (Cmd 'rloc key skill_name_middle353535' 300)
    # BOM + CRLF로 저장해도 읽힌다
    [IO.File]::WriteAllText($tfile, $backup.Replace("`n", "`r`n"), [Text.Encoding]::UTF8)
    $s = Cmd 'rloc apply' 3000
    Check 'a file with BOM and CRLF still applies' ($s -match 'rows=6518 applied=6518 fallback=0 unused=0 broken=0' -and (Cmd 'rloc key skill_name_middle984555698' 300) -match 'ls=열염장 ') $s
    [IO.File]::WriteAllText($tfile, $backup)
    Cmd 'rloc apply' 3000 | Out-Null
    # 미번역 내보내기
    $dumped = Cmd 'rloc dump' 3000
    # 내보내기는 사전에 없는 행: 영어 플래그 1 + 원문으로 둔 3행 = 4(50절)
    Check 'dump writes only the untranslated rows' ($dumped -match 'written=4' -and (Get-Content -LiteralPath (Join-Path $tdir 'untranslated.txt') -Raw) -match "0lkK4v`t`texists") $dumped
    # 파일을 지우면 다시 풀린다
    [IO.File]::Delete($tfile)
    $s = Cmd 'rloc apply' 3000
    Check 'a missing file is extracted again' ($s -match 'file=풀어 둠 rows=6518 applied=6518' -and (Test-Path -LiteralPath $tfile)) $s
    # 주석만 있는 파일: 내장본의 키가 모두 덧붙는다(46절. 43절 때는 사전 0이었다)
    [IO.File]::WriteAllText($tfile, "# only a comment`n")
    $s = Cmd 'rloc apply' 3000
    Check 'a comment-only file gets every embedded row appended' ($s -match 'file=덧붙임 6518 rows=6518 applied=6518 fallback=0 unused=0 broken=0' -and (Get-Content -LiteralPath $tfile -Raw) -match '(?m)^# only a comment') $s
    # 옛 파일(한 줄만, 고친 글): 새 키만 덧붙고 고친 줄은 남는다. 두 번째 적용은 덧붙일 것이 없다
    [IO.File]::WriteAllText($tfile, "# old file`nskill_name_middle984555698`t열염장2`n")
    $s = Cmd 'rloc apply' 3000
    Check 'an old file gets the new embedded keys appended and keeps its edited line' ($s -match 'file=덧붙임 6517 rows=6518 applied=6518' -and (Get-Content -LiteralPath $tfile -Raw) -match "(?m)^role_race_long0`t" -and (Cmd 'rloc key skill_name_middle984555698' 300) -match 'ls=열염장2') $s
    $s = Cmd 'rloc apply' 3000
    Check 'a second apply appends nothing' ($s -match 'file=있음 rows=6518 applied=6518') $s
    Check 'a race name resolves to Korean' ((Cmd 'rloc key role_race_long0' 300) -match 'ls=진룡 kr=진룡 ch=真龙') (Cmd 'rloc key role_race_long0' 300)
    Check 'a ranking hall name resolves to Korean' ((Cmd 'rloc key jsT1150010003' 300) -match 'ls=선자각 ') (Cmd 'rloc key jsT1150010003' 300)
    Check 'the artifact spirit name resolves to Korean' ((Cmd 'rloc key spriteName9963701' 300) -match 'ls=요도희 ') (Cmd 'rloc key spriteName9963701' 300)
    # 57절: 4차 모드(동천복지·지점·동료)의 글
    Check 'a cave mod name resolves to Korean' ((Cmd 'rloc key Cave_ModName' 300) -match 'ls=동천복지 ') (Cmd 'rloc key Cave_ModName' 300)
    Check 'a teaching dialogue resolves to Korean' ((Cmd 'rloc key ewcqZxLU' 300) -match 'ls=\(무엇을 가르칠까\?\)') (Cmd 'rloc key ewcqZxLU' 300)
    Check 'a party task name resolves to Korean' ((Cmd 'rloc key taskName-1979165529' 300) -match 'ls=파티 결성 ') (Cmd 'rloc key taskName-1979165529' 300)
    [IO.File]::WriteAllText($tfile, $backup)
    Cmd 'rloc apply' 3000 | Out-Null
    Shot 'translation'
    Check 'no new traps' ((Traps) -eq $traps)
  }
  'modbooks' {
    # 모드 공법의 비급 스펙 5절. 고정본 인물은 결정(대경지 3). 창작마당 모드 3390738990(공법 다섯)이 설치돼 있어야 한다.
    # 회차의 -End가 세이브와 favorites.txt를 되돌린다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "$key=([^\s|]+)") { $Matches[1] } else { '' } }
    function BookCount([int]$id) { [int](Field (Cmd "rbooks $id" 300) 'count') }
    function Range { if ((Cmd 'uitext ItemTableRange') -match '전체 (\d+)개') { [int]$Matches[1] } elseif ((Cmd 'uitext ItemTableRange') -match "text='0개'") { 0 } else { -1 } }
    # 1. 카탈로그(Task 2)
    $mods = Cmd 'rmods' 3000
    $count = if ($mods -match 'count=(\d+)') { [int]$Matches[1] } else { 0 }   # 45절 뒤 모드 공법이 100개를 넘는다(새 모드 8개)
    Check 'the catalog lists at least the five original mod skills' ($count -ge 5 -and $mods -match '1:984555698:' -and $mods -match '1:353535:' -and $mods -match '1:844854565:' -and $mods -match '0:3535351:' -and $mods -match '4:59898952:') $mods
    Check 'mod skill names are Korean after translation' ($mods -match '984555698:열염장:' -and $mods -match '59898952:천살신창:' -and $mods -match '3535351:적염검:basSword:False:') $mods
    Check 'no mod skill is blocked, the ultimate rolls from 32111' ($mods -match '4:59898952:[^:]*:[^:]*:False:-:32111' -and $mods -match '1:984555698:[^:]*:[^:]*:False:-:\d+' -and $mods -match '0:3535351:[^:]*:basSword:False:-:\d+') $mods
    $s = Cmd 'rskill 1 984555698' 300
    Check 'a mod skill has grade range 1~10' ($s -match 'min=1 max=10 name=열염장') $s
    $l = Cmd 'rlearn 1 984555698 3' 3000
    Check 'learning a mod skill directly is refused with a hint' ($l -match '모드 공법은 자원 탭의 기술 서적에서 비급으로 받습니다' -and (Cmd 'cmartial 984555698' 300) -match 'n=0') $l
    # 2. 비급(Task 3)
    foreach ($id in 984555698, 353535, 844854565) {
      $before = BookCount $id
      $made = Cmd "rbook 1 $id 3 1 1" 20000
      $books = Cmd "rbooks $id" 300
      $sole = if ($books -match "([A-Za-z0-9]{6}):${id}:3:") { $Matches[1] } else { '' }
      $study = if ($sole) { Cmd "rbookstudy $sole" 300 } else { 'no book' }
      Check "a mod skill book is made and accepted ($id)" ($made -match 'made=1' -and (BookCount $id) -eq $before + 1 -and $study -match 'can=0') "$made | $study"
    }
    $made = Cmd 'rbook 1 984555698 1 1 1' 20000
    Check 'a mod book at grade 1' ($made -match 'made=1' -and (Cmd 'rbooks 984555698' 300) -match ':984555698:1:') $made
    $made = Cmd 'rbook 1 984555698 10 1 1' 20000
    Check 'a mod book at grade 10' ($made -match 'made=1' -and (Cmd 'rbooks 984555698' 300) -match ':984555698:10:') $made
    $made = Cmd 'rbook 1 984555698 3 6 1' 60000
    Check 'a mod book with minimum quality 6 is made and says the quality is the game''s' ($made -match 'made=1' -and $made -match '최소 품질을 적용하지 않았습니다') $made
    $ok = 0
    for ($i = 0; $i -lt 3; $i++) { if ((Cmd 'rbook 0 3535351 3 1 1' 60000) -match 'made=1') { $ok++ } }
    Check 'the mod martial art is made three times out of three (retries)' ($ok -eq 3) "ok=$ok"
    $sole = if ((Cmd 'rbooks 3535351' 300) -match '([A-Za-z0-9]{6}):3535351:') { $Matches[1] } else { '' }
    Check 'the mod martial art book reads like a base one' ($sole -ne '' -and (Cmd "rbookstudy $sole" 300) -match 'can=5') "sole=$sole"
    $before = BookCount 59898952
    $made = Cmd 'rbook 4 59898952 3 1 1' 20000
    $sole = if ((Cmd 'rbooks 59898952' 300) -match '([A-Za-z0-9]{6}):59898952:3:') { $Matches[1] } else { '' }
    $study = if ($sole) { Cmd "rbookstudy $sole" 300 } else { 'no book' }
    Check 'the mod ultimate book is made, named and accepted' ($made -match 'made=1' -and $made -match '천살신창\(품질' -and (BookCount 59898952) -eq $before + 1 -and $study -match 'can=0') "$made | $study"
    foreach ($g in 1, 10) {
      $m = Cmd "rmods $g" 3000
      $made = Cmd "rbook 4 59898952 $g 1 1" 20000
      $open = $m -match '4:59898952:[^:]*:[^:]*:[^:]*:-:\d+'
      $closed = $m -match ('4:59898952:[^:]*:[^:]*:[^:]*:경지 ' + $g + '에 원본으로 쓸 기본 공법이 없습니다:-')
      Check "the ultimate at grade $g has a row state that agrees with the result" (($open -and $made -match 'made=1' -and $made -match '천살신창\(품질') -or ($closed -and $made -match 'made=0' -and $made -match '원본으로 쓸 기본 공법이 없습니다')) "$m | $made"
    }
    # 3. 화면(Task 4)
    Cmd 'rfav reset' 300 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupManual' | Out-Null; Press 'ResClear' | Out-Null
    Check 'seven kind chips with the mod-only chip' ((Cmd 'uitext ResKind6') -match "text='모드만 \($count\)'" -and (Cmd 'uipos ResKind7') -match 'no element named') (Cmd 'uitext ResKind6')
    Press 'ResKind6' | Out-Null
    Check 'the mod-only chip shows every mod row' ((Range) -eq $count) (Cmd 'uitext ItemTableRange')
    # 05 문서 48절: 이름이 비거나 "0"인 모드 기술은 "이름 없음(번호)"로 보이고 목록 끝에 온다
    $first = if ((Cmd 'uipos BookName*') -match '-> (BookName\d+) at ') { $Matches[1] } else { '' }
    $t = if ($first) { Cmd "uitext $first" } else { '' }
    Check 'the first mod row has a name' ($t -match "text='([^']*)'" -and $Matches[1] -ne '' -and $Matches[1] -ne '0' -and -not $Matches[1].StartsWith('이름 없음')) $t
    Cmd 'ui set ItemQuery 126954816' | Out-Null
    Check 'a nameless mod skill is shown by number' ((Cmd 'uitext BookName126954816') -match "text='이름 없음\(126954816\)'") (Cmd 'uitext BookName126954816')
    Press 'ItemQueryClear' | Out-Null
    Cmd 'ui set ItemQuery 열염' | Out-Null   # 줄이 화면 밖일 수 있으므로 검색으로 띄운다
    Press 'BookName984555698' | Out-Null
    $d = Cmd 'uitext ItemDetail'
    Check 'the box below explains a mod skill' ($d -match '\[모드\], 경지 1~10\(모드 공법\)' -and $d -match '지급하면: 경지 3의 비급 1권' -and $d -match '최소 품질을 적용하지 않습니다' -and $d -match '옵션 번호를 모드 것으로') $d
    Cmd 'ui set ItemQuery 천살' | Out-Null
    Press 'BookName59898952' | Out-Null
    $d = Cmd 'uitext ItemDetail'
    Check 'the box below no longer refuses the ultimate' ($d -notmatch '지급할 수 없습니다' -and $d -match '\[모드\], 경지 1~10\(모드 공법\)') $d
    $before = BookCount 984555698
    Cmd 'ui set ItemQuery 열염' | Out-Null
    Press 'item1' | Out-Null
    Press 'BookGive984555698' 20000 | Out-Null
    Check 'giving from the row makes one mod book' ((BookCount 984555698) -eq $before + 1 -and (Cmd 'uitext BookBag984555698') -match "text='$($before + 1)'") (Cmd 'uitext BookBag984555698')
    $before = BookCount 59898952
    Cmd 'ui set ItemQuery 천살' | Out-Null
    Press 'BookGive59898952' 20000 | Out-Null
    Check 'giving the ultimate from the row makes one mod book' ((BookCount 59898952) -eq $before + 1 -and (Cmd 'uitext BookBag59898952') -match "text='$($before + 1)'") (Cmd 'uitext BookBag59898952')
    Cmd 'ui set ItemQuery 烈炎' | Out-Null
    Check 'the Chinese name is searchable' ((Range) -eq 1) (Cmd 'uitext ItemTableRange')
    Press 'ItemQueryClear' | Out-Null
    Press 'ResBookFit' | Out-Null; Press 'ResBookFit' | Out-Null
    Check 'this-grade-only keeps mod rows' ((Range) -eq $count) (Cmd 'uitext ItemTableRange')
    Press 'ResClear' | Out-Null
    Check 'clear releases the mod-only chip' ((Range) -gt $count -and (Cmd 'uitext ResKind0') -match "text='전체 \(") (Cmd 'uitext ItemTableRange')
    Press 'ResKind6' | Out-Null
    Cmd 'ui set ItemQuery 열염' | Out-Null
    Press 'BookStar984555698' | Out-Null
    Press 'ItemQueryClear' | Out-Null
    Check 'a mod book favorite is saved' ((Cmd 'rfav' 200) -match 'book:1:984555698:3:1:1') (Cmd 'rfav' 200)
    Press 'ResGroupFav' | Out-Null
    $before = BookCount 984555698
    Press 'ResAllGive' | Out-Null; Press 'ResAllGive' 20000 | Out-Null
    Check 'give all gives the mod book' ((BookCount 984555698) -eq $before + 1) "$before -> $(BookCount 984555698)"
    Shot 'modbooks'
    Cmd 'rfav reset' 300 | Out-Null
    Check 'no new traps' ((Traps) -eq $traps)
  }
  'books-ui' {
    # 기술 서적 스펙 5절: 화면. 고정본 인물은 결정(대경지 3). 회차의 -End가 favorites.txt를 되돌린다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "$key=([^\s|]+)") { $Matches[1] } else { '' } }
    function BookCount { [int](Field (Cmd 'rbooks 23116' 300) 'count') }
    function Held([int]$id) { if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 } }
    function Range { if ((Cmd 'uitext ItemTableRange') -match '전체 (\d+)개') { [int]$Matches[1] } elseif ((Cmd 'uitext ItemTableRange') -match "text='0개'") { 0 } else { -1 } }
    Cmd 'rfav reset' 300 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupManual' | Out-Null; Press 'ResClear' | Out-Null
    Check 'the group button shows a count' ((Cmd 'uitext ResGroupManual') -match "text='기술 서적 [1-9]\d*'") (Cmd 'uitext ResGroupManual')
    Check 'seven kind chips' ((Cmd 'uitext ResKind0') -match "text='전체 \(" -and (Cmd 'uitext ResKind5') -match "text='신통 \(" -and (Cmd 'uitext ResKind6') -match "text='모드만 \(" -and (Cmd 'uipos ResKind7') -match 'no element named')
    $r = Pos 'Row0'
    Rc "click $($r.x) $($r.y)" 600 | Out-Null
    Check 'the grade starts at the current grade' ((Cmd 'uitext ItemDetail') -match '지급하면: 경지 3, 품질 1 이상') (Cmd 'uitext ItemDetail')
    $fit = Range
    Press 'ResBookFit' | Out-Null
    $every = Range
    Check 'turning off "this grade only" shows more skills' ($fit -gt 0 -and $every -gt $fit) "fit=$fit all=$every"
    $outside = $null
    foreach ($c in @(@(0, 17111), @(2, 510021), @(0, 11423), @(0, 44112))) {
      $s = Cmd "rskill $($c[0]) $($c[1])" 200
      if ($s -match 'min=(\d+) max=(\d+)' -and (3 -lt [int]$Matches[1] -or 3 -gt [int]$Matches[2])) { $outside = $c[1]; break }
    }
    if ($outside) {
      Cmd "ui set ItemQuery $outside" | Out-Null
      $p = Pos "BookGive$outside"
      Check 'a skill outside the grade cannot be given' ((Rc "click $($p.x) $($p.y)" 400) -notmatch "Button\.onClick: BookGive$outside")
      Press 'ItemQueryClear' | Out-Null
    } else { 'INFO no candidate skill outside grade 3' }
    Press 'ResBookFit' | Out-Null
    # Review Focus 5: 경지를 바꾼 뒤 초기화하면 지금 경지로
    Press 'ResGrade5' | Out-Null; Press 'ResMinLevel4' | Out-Null
    Press 'ResClear' | Out-Null
    $r = Pos 'Row0'
    Rc "click $($r.x) $($r.y)" 600 | Out-Null
    Check 'clear brings back the current grade and quality 1' ((Cmd 'uitext ItemDetail') -match '지급하면: 경지 3, 품질 1 이상') (Cmd 'uitext ItemDetail')
    # 최소 품질 6으로 지급
    Cmd 'ui set ItemQuery 23116' | Out-Null
    Press 'ResMinLevel6' | Out-Null
    Press 'item1' | Out-Null
    $before = BookCount
    Press 'BookGive23116' 3000 | Out-Null
    $books = Cmd 'rbooks 23116' 300
    Check 'giving makes a quality 6 book of that skill' ((BookCount) -eq $before + 1 -and $books -match ':23116:3:6:') $books
    Check 'the bag column counts the books' ((Cmd 'uitext BookBag23116') -match "text='$($before + 1)'") (Cmd 'uitext BookBag23116')
    Press 'BookName23116' | Out-Null
    Check 'the box below tells what will be made' ((Cmd 'uitext ItemDetail') -match '지급하면: 경지 3, 품질 6 이상의 비급 1권') (Cmd 'uitext ItemDetail')
    Press 'BookStar23116' | Out-Null
    Check 'the star saves the book with grade and quality' ((Cmd 'rfav' 200) -match 'book:1:23116:3:6:1') (Cmd 'rfav' 200)
    Check 'the star is filled' ((Cmd 'uitext BookStar23116') -match "text='★'")
    Press 'ItemQueryClear' | Out-Null
    # 아이템 하나를 함께 넣고 즐겨찾기 보기로
    Press 'ResGroupAll' | Out-Null
    Cmd 'ui set ItemQuery 1011011' | Out-Null
    Press 'ResStar1011011' | Out-Null
    Press 'ItemQueryClear' | Out-Null
    Press 'ResGroupFav' | Out-Null
    Check 'the favorites view has the book row' ((Cmd 'uitext FavBookName23116_3_6') -match '비급 \(경지 3, 품질 6 이상\)') (Cmd 'uitext FavBookName23116_3_6')
    Cmd 'uifocus FavBookAmount23116_3_6' | Out-Null; Cmd 'ui set FavBookAmount23116_3_6 21' | Out-Null; Key 0x0D
    Check 'a book amount above 20 is refused' ((Cmd 'rfav' 200) -match 'book:1:23116:3:6:1(\s|,|$)' -and (Cmd 'uistate') -match '비급 수량은 1~20') (Cmd 'uistate')
    Cmd 'uifocus FavBookAmount23116_3_6' | Out-Null; Cmd 'ui set FavBookAmount23116_3_6 5' | Out-Null; Key 0x0D
    Check 'the book amount is saved' ((Cmd 'rfav' 200) -match 'book:1:23116:3:6:5') (Cmd 'rfav' 200)
    $before = BookCount
    Press 'FavBookGive23116_3_6' 5000 | Out-Null
    Check 'the book row gives its saved amount' ((BookCount) -eq $before + 5) "$before -> $(BookCount)"
    $before = BookCount; $had = Held 1011011
    Press 'ResAllGive' | Out-Null; Press 'ResAllGive' 8000 | Out-Null
    Check 'give all gives items and books' ((Held 1011011) -eq $had + 1 -and (BookCount) -eq $before + 5 -and (Cmd 'uistate') -match "message='즐겨찾기 모두 지급: 넣음 2, 못 넣음 0") "$(Cmd 'uistate')"
    Shot 'books-ui'
    Press 'FavBookStar23116_3_6' | Out-Null
    Check 'the book favorite can be removed' ((Cmd 'rfav' 200) -notmatch 'book:') (Cmd 'rfav' 200)
    # Review Focus 1: 게임에 없는 기술의 비급 즐겨찾기
    Cmd 'rfav addbook 1 99999 3 6 1' 300 | Out-Null
    Press 'ResQuality2' | Out-Null
    Check 'a missing skill book favorite is shown under a filter' ((Cmd 'uitext FavBookName99999_3_6') -match '게임에 없는 기술 \(99999\)') (Cmd 'uitext FavBookName99999_3_6')
    $p = Pos 'FavBookGive99999_3_6'
    Check 'its give button is off' ((Rc "click $($p.x) $($p.y)" 400) -notmatch 'Button\.onClick: FavBookGive99999_3_6')
    Press 'FavBookStar99999_3_6' | Out-Null
    Check 'and it can be removed' ((Cmd 'rfav' 200) -notmatch '99999') (Cmd 'rfav' 200)
    Press 'ResClear' | Out-Null
    # 검토에서 나온 것: 경지를 고르지 않았으면 지금 경지를 따라간다(돌파하거나 다른 세이브를 불러도). 고른 경지는 남는다
    function ManualDetail { Press 'ResGroupManual' | Out-Null; $r = Pos 'Row0'; Rc "click $($r.x) $($r.y)" 600 | Out-Null; Cmd 'uitext ItemDetail' }
    $gid = if ((Cmd 'stats' 300) -match 'gradeID=(\d+)') { [int]$Matches[1] } else { 0 }
    $d3 = ManualDetail
    # 경지 번호는 대경지마다 칸 수가 달라, 대경지가 오를 때까지 하나씩 올린다
    $raised = 0
    for ($id = $gid + 1; $id -le $gid + 8 -and $raised -le 3; $id++) {
      $up = Cmd "setgrade $id" 300
      $raised = if ($up -match '\(grade=(\d+) ') { [int]$Matches[1] } else { 0 }
    }
    $d4 = ManualDetail
    Press 'ResGrade5' | Out-Null
    Cmd "setgrade $gid" 300 | Out-Null
    $d5 = ManualDetail
    Check 'an unchosen grade follows the current grade' ($gid -gt 0 -and $d3 -match '지급하면: 경지 3,' -and $raised -gt 3 -and $d4 -match "지급하면: 경지 $raised,") "gid=$gid | $d3 | $up | $d4"
    Check 'a chosen grade stays when the current grade changes' ($d5 -match '지급하면: 경지 5,') $d5
    Press 'ResClear' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'appellation' {
    # 도호 관리 스펙 5절: 서비스. 고정본 인물은 결정(대경지 3)이고 영단도인(2001)을 장착했다. 끝에서 저장한다(appellation-restart가 본다).
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "(?:^|\s)$key=([^\s|]*)") { $Matches[1] } else { '' } }
    function St { Cmd 'ttl' 300 }
    function Owns([string]$s, [int]$id) { ",$(Field $s 'owned')," -match ",$id," }
    function TitleLines([string]$pattern) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ -match $pattern }).Count }
    $s = St
    $me = Field $s 'me'
    Check 'the fixture has 2001 equipped' ((Field $s 'equipped') -eq '2001') $s
    $talent = [int](Field $s 'talent')
    $silenced = [int](Field $s 'silenced')
    # 주기: 장착은 그대로, 획득 창은 편집기가 확인을 눌러 닫는다
    $logged = TitleLines '도호 풍류도인\(1502\)'
    $o = Cmd 'ttlgive 1502' 1500
    Start-Sleep -Seconds 3
    $s = St
    Check 'giving adds the title and keeps the equipped one' ($o -match 'ok=True' -and (Owns $s 1502) -and (Field $s 'equipped') -eq '2001') "$o | $s"
    Check 'the editor closes its own title popup' ((Field $s 'popup') -eq 'False' -and [int](Field $s 'silenced') -gt $silenced) $s
    Check 'the change log has the gift' ((TitleLines '도호 풍류도인\(1502\)') -eq $logged + 1)
    # Review Focus 1: 이미 가진 도호
    $o = Cmd 'ttlgive 1502' 600
    Check 'giving a title already held is refused' ($o -match 'ok=False' -and $o -match '이미 가졌습니다') $o
    # Review Focus 4: 게임 표에 없는 번호
    $o = Cmd 'ttlgive 999999' 600
    Check 'a number the game does not have is refused' ($o -match 'ok=False' -and $o -match '게임에 없는 번호') $o
    # '제일' 도호. Review Focus 2: 두 NPC가 함께 가졌으면 둘 다에게서 뺀다
    $h = Cmd 'ttlholders 1404' 300
    Check 'an NPC holds 1404 in the fixture' ((Field $h 'holders') -eq '1' -and $h -notmatch "\($me\)") $h
    $o = Cmd 'ttlnpcgive any 1404' 600
    $h = Cmd 'ttlholders 1404' 300
    Check 'two NPCs hold 1404 before the gift' ($o -match 'ok=True' -and (Field $h 'holders') -eq '2') "$o | $h"
    $logged = TitleLines '도호 팔황 제일권수\(1404\)'
    $o = Cmd 'ttlgive 1404' 1500
    $h = Cmd 'ttlholders 1404' 300
    Check 'a first title moves from every NPC holder' ($o -match 'ok=True' -and $o -match '에게서 옮김' -and (Field $h 'holders') -eq '1' -and $h -match "\($me\)") "$o | $h"
    Check 'the change log has the gift and both removals' ((TitleLines '도호 팔황 제일권수\(1404\)') -eq $logged + 3)
    # 경지 밖: 결정경 인물에게 영녕 제일도수(결정경에 이르면 잃는다)
    $o = Cmd 'ttlgive 1001' 1500
    Check 'a title lost at this grade is given with a notice' ($o -match 'ok=True' -and $o -match '다음 돌파 때 잃습니다') $o
    # 장착·해제
    $o = Cmd 'ttlequip 1502' 600
    Check 'equip switches the title' ($o -match 'ok=True' -and (Field (St) 'equipped') -eq '1502') $o
    $o = Cmd 'ttlunequip' 600
    Check 'unequip leaves none' ($o -match 'ok=True' -and (Field (St) 'equipped') -eq '0') $o
    $o = Cmd 'ttlunequip' 600
    Check 'unequip with none is refused' ($o -match 'ok=False' -and $o -match '장착한 도호가 없습니다') $o
    # Review Focus 3: 장착한 것이 없을 때 주면 게임이 장착하고 효과가 든다(어계천존 talent_1_40)
    $o = Cmd 'ttlgive 2111' 1500
    $s = St
    Check 'a gift with nothing equipped is equipped and says so' ($o -match '장착됨' -and (Field $s 'equipped') -eq '2111') "$o | $s"
    Check 'the equipped effect applies (talent +40)' ([int](Field $s 'talent') -eq $talent + 40) "before=$talent | $s"
    # 장착한 것을 빼면 장착이 빈다
    $o = Cmd 'ttldel 2111' 600
    $s = St
    Check 'removing the equipped title empties the slot' ($o -match 'ok=True' -and $o -match '장착이 비었습니다' -and -not (Owns $s 2111) -and (Field $s 'equipped') -eq '0') "$o | $s"
    Check 'the effect goes with it' ([int](Field $s 'talent') -eq $talent) $s
    # 빼기 뒤 게임의 "도호 상실" 창: 편집기가 뺀 도호의 것은 편집기가 확인을 누른다(회차 apl-1015g의 실측)
    Start-Sleep -Seconds 2
    $s = St
    Check 'the editor closes the title-lost window of its own removal' ((Field $s 'reward') -eq 'False') $s
    $o = Cmd 'ttldel 1601' 600
    Check 'removing a title not held is refused' ($o -match 'ok=False' -and $o -match '갖고 있지 않습니다') $o
    $o = Cmd 'ttlequip 1601' 600
    Check 'equipping a title not held is refused' ($o -match 'ok=False' -and $o -match '갖고 있지 않습니다') $o
    $o = Cmd 'ttlgive 2601' 1500
    Check 'a story title is given with its notice' ($o -match 'ok=True' -and $o -match '이야기와 묶인 효과라 확인하지 못했습니다') $o
    $o = Cmd 'ttlequip 2001' 600
    Check 'equip back to 2001' ($o -match 'ok=True' -and (Field (St) 'equipped') -eq '2001') $o
    Start-Sleep -Seconds 3
    Check 'no title popup is left open' ((Field (St) 'popup') -eq 'False') (St)
    # 일반 보상 창(도호가 아닌 것)은 건드리지 않는다
    Cmd 'ttlrewardopen' 600 | Out-Null
    Start-Sleep -Seconds 2
    $s = St
    Check 'an ordinary reward window is left alone' ((Field $s 'rewardAny') -eq 'True' -and (Field $s 'reward') -eq 'False') $s
    Cmd 'ttlrewardclose' 600 | Out-Null
    Check 'save is called' ((Cmd 'save' 4000) -match 'SaveData called')
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'appellation-restart' {
    # 도호 관리 스펙 5절: appellation 부분이 저장한 뒤 -Restart 하고 돌린다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "(?:^|\s)$key=([^\s|]*)") { $Matches[1] } else { '' } }
    $s = Cmd 'ttl' 300
    $me = Field $s 'me'
    $owned = ",$(Field $s 'owned'),"
    Check 'the given titles are still held' ($owned -match ',1001,' -and $owned -match ',1404,' -and $owned -match ',1502,' -and $owned -match ',2001,' -and $owned -notmatch ',2111,') $s
    Check 'the equipped title is still 2001' ((Field $s 'equipped') -eq '2001') $s
    $h = Cmd 'ttlholders 1404' 300
    Check 'the moved first title has one holder' ((Field $h 'holders') -eq '1' -and $h -match "\($me\)") $h
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'appellation-ui' {
    # 도호 관리 스펙 5절: 화면. 고정본 인물은 결정(대경지 3)이고 영단도인(2001)을 장착했다. 관산이 팔황 제일권수(1404)를 가졌다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "(?:^|\s)$key=([^\s|]*)") { $Matches[1] } else { '' } }
    function Num([string]$name) { if ((Cmd "uitext $name") -match "text='[^']*?(\d+)'") { [int]$Matches[1] } else { -1 } }
    function Shown { $t = Cmd 'uitext AplTableRange'; if ($t -match '전체 (\d+)개') { [int]$Matches[1] } elseif ($t -match "text='0개'") { 0 } else { -1 } }
    function Owns([int]$id) { ",$(Field (Cmd 'ttl' 300) 'owned')," -match ",$id," }
    # 도호를 받으면 근처 NPC가 그 도호로 말을 거는 대화가 편집기 위에 뜰 수 있다(게임의 동작, 스펙 3.2). 떠 있으면 넘긴다.
    function SkipDialogue { for ($i = 0; $i -lt 8 -and (Cmd 'uilist' 200) -match 'DramaDialogue'; $i++) { Rc 'click 958 552' 1000 | Out-Null } }
    # 도호를 빼면 게임의 "도호 상실" 창(GetReward)이 뜨고 편집기가 확인을 누른다. 검사가 실패해도 다음 단계가 막히지 않게 남은 창은 확인을 누른다.
    function SkipReward { for ($i = 0; $i -lt 4 -and (Cmd 'uilist' 200) -match 'GetReward'; $i++) { Rc 'click 959 662' 1000 | Out-Null } }
    $me = Field (Cmd 'ttl' 300) 'me'
    Cmd 'editor' | Out-Null; Press 'Tab15' | Out-Null
    Check 'the top line shows the equipped title and its effect' ((Cmd 'uitext AplEquipped') -match '영단도인 — .+') (Cmd 'uitext AplEquipped')
    Check 'All counts 100' ((Num 'AplClassAll') -eq 100) (Cmd 'uitext AplClassAll')
    $sum = 0
    foreach ($k in 'First', 'Romance', 'Champion', 'Spender', 'Dress', 'Free', 'Alchemy', 'Forge', 'Supreme', 'Ending', 'Other') { $n = Num "AplClass$k"; if ($n -gt 0) { $sum += $n } }
    Check 'the class chips add up to 100' ($sum -eq 100) "sum=$sum"
    Check 'First has 60 and Owned has 1' ((Num 'AplClassFirst') -eq 60 -and (Num 'AplClassOwned') -eq 1)
    Check 'the table lists all 100' ((Shown) -eq 100) (Cmd 'uitext AplTableRange')
    # 검색과 가진 것만. Review Focus 5: 번호로 찾기
    Cmd 'ui set AplQuery 권수' | Out-Null
    $n = Shown
    Check 'searching narrows the table' ($n -gt 0 -and $n -lt 100) "shown=$n"
    Cmd 'ui set AplQuery 1404' | Out-Null
    Check 'a number finds that title' ((Shown) -eq 1 -and (Cmd 'uipos AplName1404') -match ' at ') (Cmd 'uitext AplTableRange')
    Press 'AplQueryClear' | Out-Null
    Press 'AplOwnedOnly' | Out-Null
    Check 'owned only shows the held title as equipped' ((Shown) -eq 1 -and (Cmd 'uitext AplState2001') -match "text='★'") (Cmd 'uitext AplTableRange')
    Press 'AplClear' | Out-Null
    Check 'clear restores the full table' ((Shown) -eq 100)
    # 잃는 경지 경고
    Cmd 'ui set AplQuery 1001' | Out-Null
    Check 'a title lost at this grade warns' ((Cmd 'uitext AplLoss1001') -match '다음 돌파') (Cmd 'uitext AplLoss1001')
    # '제일' 도호 가져오기: 두 번 눌러야 옮긴다
    $h = Cmd 'ttlholders 1404' 300
    $holder = if ($h -match 'names=([^(]+)\(') { $Matches[1] } else { '?' }
    Cmd 'ui set AplQuery 1404' | Out-Null
    Check 'the holder column and the give button name the holder' ((Cmd 'uitext AplHolder1404') -match $holder -and (Cmd 'uitext AplGive1404') -match "$($holder)에게서 가져오기") "$(Cmd 'uitext AplHolder1404') | $(Cmd 'uitext AplGive1404')"
    # 검토에서 나온 것: 이름을 먼저 눌러 둔 채 가져오면 아래 칸의 보유자도 바뀐다
    Press 'AplName1404' | Out-Null
    Check 'the detail first shows the NPC holder' ((Cmd 'uitext AplDetail') -match "보유자: $holder") (Cmd 'uitext AplDetail')
    $silenced = [int](Field (Cmd 'ttl' 300) 'silenced')
    Press 'AplGive1404' | Out-Null
    Check 'one press does not move it' (-not (Owns 1404))
    Press 'AplGive1404' 1500 | Out-Null
    $h = Cmd 'ttlholders 1404' 300
    Check 'the second press moves it to the player' ((Field $h 'holders') -eq '1' -and $h -match "\($me\)") $h
    Check 'the result names the holder' ((Cmd 'uistate') -match '에게서 옮김') (Cmd 'uistate')
    Check 'the detail follows the move without another press' ((Cmd 'uitext AplDetail') -match '보유자: 나') (Cmd 'uitext AplDetail')
    Start-Sleep -Seconds 3
    $s = Cmd 'ttl' 300
    Check 'its popup was closed by the editor' ((Field $s 'popup') -eq 'False' -and [int](Field $s 'silenced') -gt $silenced) $s
    SkipDialogue
    # 아래 칸
    Press 'AplName1404' | Out-Null
    $d = Cmd 'uitext AplDetail'
    Check 'the detail shows effect, how to get it and the holder' ($d -match '효과: .*권법' -and $d -match '얻는 법: ' -and $d -match '보유자: 나') $d
    Check 'the detail has no raw text keys' ($d -notmatch '\$') $d
    # 빼기도 두 번
    Press 'AplRemove1404' | Out-Null
    Check 'one press does not remove it' (Owns 1404)
    Press 'AplRemove1404' | Out-Null
    Check 'the second press removes it' (-not (Owns 1404))
    Start-Sleep -Seconds 2
    Check 'the title-lost window of the removal was closed by the editor' ((Cmd 'uilist' 200) -notmatch 'GetReward') ((Cmd 'uilist' 200) -replace "`n", ' |')
    SkipReward
    SkipDialogue
    # 장착 칸과 줄의 장착
    Press 'AplClear' | Out-Null
    Press 'AplUnequip' | Out-Null
    Check 'unequip empties the top line' ((Cmd 'uitext AplEquipped') -match '장착한 도호가 없습니다') (Cmd 'uitext AplEquipped')
    Cmd 'ui set AplQuery 2001' | Out-Null
    Press 'AplEquip2001' | Out-Null
    Check 'equip from the row' ((Cmd 'uitext AplEquipped') -match '영단도인' -and (Cmd 'uitext AplEquip2001') -match '장착 중') "$(Cmd 'uitext AplEquipped') | $(Cmd 'uitext AplEquip2001')"
    # 이야기 도호 안내
    Cmd 'ui set AplQuery 2601' | Out-Null
    Press 'AplName2601' | Out-Null
    Check 'a story title says its effect is unverified' ((Cmd 'uitext AplDetail') -match '이야기와 묶인 효과라 확인하지 못했습니다') (Cmd 'uitext AplDetail')
    Press 'AplClear' | Out-Null
    Shot 'appellation-ui'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'favstore' {
    # 자원 탭 개편 스펙 3.3절: 즐겨찾기 파일의 읽기·쓰기. 경고를 일부러 남기므로 회차에서 이 부분 뒤에는 -Restart한다.
    $traps = Traps
    $fav = Join-Path $data 'favorites.txt'
    function FavText { if (Test-Path -LiteralPath $fav) { [IO.File]::ReadAllText($fav) } else { '' } }
    $o = Cmd 'rfav reset' 300
    Check 'reset starts from no file' ($o -match 'count=0 skipped=0 items=' -and -not (Test-Path -LiteralPath $fav)) $o
    $o = Cmd 'rfav add 1011011 10' 300
    Check 'adding writes the file' ($o -match 'count=1 ' -and (FavText) -match "(?m)^1011011 10\r?$") $o
    Check 'no temp file is left' (-not (Test-Path -LiteralPath "$fav.tmp"))
    # Review Focus 5: 같은 번호, 범위 밖의 수량
    $o = Cmd 'rfav add 1011011 20' 300
    Check 'adding the same item again is refused' ($o -match '이미' -and $o -match 'items=1011011:10(\s|,|$)') $o
    $o = Cmd 'rfav add 1011012 0' 300
    Check 'an amount of zero is refused' ($o -match '수량은 1~999999' -and $o -match 'count=1 ') $o
    $o = Cmd 'rfav set 1011011 1000000' 300
    Check 'an amount above 999999 is refused' ($o -match '수량은 1~999999' -and $o -match 'items=1011011:10(\s|,|$)') $o
    $o = Cmd 'rfav set 1011011 25' 300
    Check 'the amount can be changed' ($o -match 'items=1011011:25' -and (FavText) -match "(?m)^1011011 25\r?$") $o
    $o = Cmd 'rfav add 99999999 3' 300
    Check 'a number the game does not have is kept' ($o -match 'items=1011011:25,99999999:3:missing') $o
    $o = Cmd 'rfav del 99999999' 300
    Check 'removing works' ($o -match 'count=1 ' -and (FavText) -notmatch '99999999') $o
    # 비급 줄(기술 서적 스펙 3.3절). Review Focus 4: 같은 기술이라도 경지·최소 품질이 다르면 다른 즐겨찾기
    $o = Cmd 'rfav addbook 1 23116 3 6 4' 300
    Check 'a book favorite is written as a book line' ($o -match 'book:1:23116:3:6:4' -and (FavText) -match "(?m)^book 1 23116 3 6 4\r?$") $o
    $o = Cmd 'rfav addbook 1 23116 3 6 2' 300
    Check 'the same book (skill, grade, quality) is refused' ($o -match '이미' -and $o -match 'book:1:23116:3:6:4') $o
    $o = Cmd 'rfav addbook 1 23116 3 5 2' 300
    Check 'another minimum quality is another favorite' ($o -match 'book:1:23116:3:6:4' -and $o -match 'book:1:23116:3:5:2') $o
    $o = Cmd 'rfav setbook 23116 3 6 21' 300
    Check 'a book amount above 20 is refused' ($o -match '1~20' -and $o -match 'book:1:23116:3:6:4') $o
    $o = Cmd 'rfav delbook 23116 3 5' 300
    Check 'a book favorite can be removed' ($o -notmatch 'book:1:23116:3:5' -and (FavText) -notmatch 'book 1 23116 3 5') $o
    Check 'item lines are kept beside book lines' ($o -match 'items=1011011:25,book:1:23116:3:6:4') $o

    # 깨진 줄, 같은 번호의 줄, 범위 밖의 수량이 섞인 파일
    [IO.File]::WriteAllText($fav, "# test`n1011011 5`nnot a line`n1011011 7`n1011012 5000000`n1011013 abc`n", [Text.UTF8Encoding]::new($false))
    $o = Cmd 'rfav reload' 300
    Check 'readable lines are kept, the rest counted, big amounts capped' ($o -match 'count=2 skipped=3 items=1011011:5,1011012:999999') $o
    Check 'the log has the warning' (@(ModWarnings | Where-Object { $_ -match 'favorites: skipped 3 lines' }).Count -ge 1)
    Check 'nothing is backed up before a change' (-not (Test-Path -LiteralPath "$fav.bak"))
    Cmd 'rfav set 1011012 7' 300 | Out-Null
    Check 'the original file is kept as .bak before the first write' ((Test-Path -LiteralPath "$fav.bak") -and [IO.File]::ReadAllText("$fav.bak") -match 'not a line')
    Check 'the new file holds only readable lines' ((FavText) -notmatch 'not a line|abc' -and (FavText) -match "(?m)^1011012 7\r?$")
    [IO.File]::WriteAllText("$fav.bak", 'marker')
    Cmd 'rfav set 1011012 8' 300 | Out-Null
    Check 'the .bak is made only once' ([IO.File]::ReadAllText("$fav.bak") -eq 'marker')

    # Review Focus 4: 읽지 못한 파일은 덮어쓰지 않는다
    $before = FavText
    $lock = [IO.File]::Open($fav, 'Open', 'Read', 'None')
    try {
      $o1 = Cmd 'rfav reload' 300
      $o2 = Cmd 'rfav add 1011014 1' 300
    } finally { $lock.Dispose() }
    Check 'a locked file reads as empty' ($o1 -match 'count=0 ') $o1
    Check 'a locked file is not overwritten' ((FavText) -eq $before -and $o2 -match '읽지 못해') $o2
    # 검토에서 나온 것: 잠긴 채 읽어 빈 목록이 된 뒤 잠금이 풀리면, 이미 있는 번호를 넣어도 두 줄이 되지 않아야 한다
    $lock = [IO.File]::Open($fav, 'Open', 'Read', 'None')
    try { Cmd 'rfav reload' 300 | Out-Null } finally { $lock.Dispose() }
    $o = Cmd 'rfav add 1011011 1' 300
    $same = @(((FavText) -split "`n") | Where-Object { $_ -match '^1011011 ' }).Count
    Check 'after a lock, adding an item already in the file is refused' ($o -match '이미' -and $o -match 'count=2 ' -and $same -eq 1) "$o | lines=$same"
    $o = Cmd 'rfav reload' 300
    Check 'after the lock is gone the list is back' ($o -match 'count=2 skipped=0 items=1011011:5,1011012:8') $o
    # 비급 줄의 읽기: 수량은 20으로 맞추고, 분류가 범위 밖이거나 칸 수가 다른 줄은 건너뛴다. 아이템 줄과 섞여도 순서를 지킨다
    $keep = FavText
    [IO.File]::WriteAllText($fav, "1011011 5`nbook 1 23116 3 6 25`nbook 9 23116 3 6 1`nbook 1 23116`n1011012 8`n", [Text.UTF8Encoding]::new($false))
    $o = Cmd 'rfav reload' 300
    Check 'book lines are read, capped and checked' ($o -match 'count=3 skipped=2 items=1011011:5,book:1:23116:3:6:20,1011012:8') $o
    [IO.File]::WriteAllText($fav, $keep, [Text.UTF8Encoding]::new($false))
    Cmd 'rfav reload' 300 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'favstore-restart' {
    # favstore 뒤에 session.ps1 -Restart를 하고 돌린다.
    $o = Cmd 'rfav' 200
    Check 'favorites are kept after a restart' ($o -match 'count=2 skipped=0 items=1011011:5,1011012:8') $o
  }
  'favorites' {
    # 자원 탭 개편 스펙 3.2·7절: 화면의 즐겨찾기. 회차의 -End가 favorites.txt를 되돌린다.
    $traps = Traps
    function Held([int]$id) { if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 } }
    function FavLines { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ -match '즐겨찾기' }).Count }
    Cmd 'rfav reset' 300 | Out-Null
    $logged = FavLines
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupAll' | Out-Null; Press 'ResClear' | Out-Null
    Check 'the favorites button shows zero' ((Cmd 'uitext ResGroupFav') -match "text='★ 즐겨찾기 0'") (Cmd 'uitext ResGroupFav')
    Press 'item10' | Out-Null
    Cmd 'ui set ItemQuery 1011011' | Out-Null
    Check 'a row starts with an empty star' ((Cmd 'uitext ResStar1011011') -match "text='☆'") (Cmd 'uitext ResStar1011011')
    Press 'ResStar1011011' | Out-Null
    Check 'the star adds it with the current amount' ((Cmd 'rfav' 200) -match 'items=1011011:10(\s|$)') (Cmd 'rfav' 200)
    Check 'the star is filled' ((Cmd 'uitext ResStar1011011') -match "text='★'")
    Cmd 'ui set ItemQuery 1011012' | Out-Null
    Press 'ResStar1011012' | Out-Null
    Press 'ItemQueryClear' | Out-Null
    Press 'ResGroupFav' | Out-Null
    Check 'the favorites view lists them in the order added' ((Pos 'ItemName*').name -eq 'ItemName1011011' -and (Cmd 'uitext ResGroupFav') -match "즐겨찾기 2'") (Cmd 'uitext ResGroupFav')
    Check 'the give button shows the saved amount' ((Cmd 'uitext ItemGive1011011') -match "text='지급 x10'")
    Cmd 'uifocus ResAmount1011011' | Out-Null; Cmd 'ui set ResAmount1011011 25' | Out-Null; Key 0x0D
    Check 'Enter saves the amount' ((Cmd 'rfav' 200) -match 'items=1011011:25,1011012:10') (Cmd 'rfav' 200)
    # 55절: Enter 없이 칸을 떠나도(다른 칸에 포커스) 저장되고 지급 버튼의 수량 글이 바뀐다. 숫자가 아니면 마지막 값으로 되돌린다.
    Cmd 'uifocus ResAmount1011011' | Out-Null; Cmd 'ui set ResAmount1011011 30' | Out-Null; Cmd 'uifocus ItemQuery' | Out-Null
    Check 'leaving the field saves the amount too' ((Cmd 'rfav' 200) -match 'items=1011011:30,1011012:10') (Cmd 'rfav' 200)
    Check 'the give button shows the new amount without redrawing' ((Cmd 'uitext ItemGive1011011') -match "text='지급 x30'") (Cmd 'uitext ItemGive1011011')
    Cmd 'uifocus ResAmount1011011' | Out-Null; Cmd 'ui set ResAmount1011011 25' | Out-Null; Cmd 'uifocus ItemQuery' | Out-Null
    Check 'leaving the field again saves the next amount' ((Cmd 'rfav' 200) -match 'items=1011011:25,1011012:10') (Cmd 'rfav' 200)
    Press 'item1' | Out-Null
    $a = Held 1011011
    Press 'ItemGive1011011' | Out-Null
    Check 'the row gives the saved amount, not the amount bar' ((Held 1011011) -eq $a + 25) "$a -> $(Held 1011011)"
    Cmd 'uifocus ResAmount1011012' | Out-Null; Cmd 'ui set ResAmount1011012 0' | Out-Null; Key 0x0D
    Check 'an amount of zero is refused' ((Cmd 'rfav' 200) -match '1011012:10' -and (Cmd 'uistate') -match "message='즐겨찾기: 수량은 1~999999") (Cmd 'uistate')
    $a = Held 1011011; $b2 = Held 1011012
    Press 'ResAllGive' | Out-Null
    Check 'give all asks first' ((Held 1011011) -eq $a -and (Cmd 'uitext ResAllGive') -match "text='정말 모두 지급'")
    Press 'ResAllGive' 2000 | Out-Null
    Check 'the second press gives every favorite its saved amount' ((Held 1011011) -eq $a + 25 -and (Held 1011012) -eq $b2 + 10) "$a/$b2 -> $(Held 1011011)/$(Held 1011012)"
    Check 'the result counts what was given' ((Cmd 'uistate') -match "message='즐겨찾기 모두 지급: 넣음 2, 못 넣음 0") (Cmd 'uistate')
    # Review Focus 2: 모두 지급은 걸러져 보이는 줄만 준다(5품 보원단만 품질 2)
    Press 'ResQuality2' | Out-Null
    $a = Held 1011011; $b2 = Held 1011012
    Press 'ResAllGive' | Out-Null; Press 'ResAllGive' 2000 | Out-Null
    Check 'give all gives only the rows shown' ((Held 1011011) -eq $a -and (Held 1011012) -eq $b2 + 10) "$a/$b2 -> $(Held 1011011)/$(Held 1011012)"
    Press 'ResClear' | Out-Null
    Shot 'favorites'
    Press 'ResStar1011012' | Out-Null
    Check 'the star removes it' ((Cmd 'rfav' 200) -match 'count=1 ' -and (Cmd 'uitext ResGroupFav') -match "즐겨찾기 1'") (Cmd 'rfav' 200)
    Check 'favorites do not go to the change log' ((FavLines) -eq $logged)
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'favorites-corrupt' {
    # 깨진 줄과 게임에 없는 번호가 섞인 파일을 화면에서 본다. 경고를 일부러 남기므로 회차의 마지막에 돌린다.
    $traps = Traps
    $fav = Join-Path $data 'favorites.txt'
    Cmd 'rfav reset' 300 | Out-Null
    [IO.File]::WriteAllText($fav, "1011011 5`nnot a line`n99999999 3`n1011013 abc`n", [Text.UTF8Encoding]::new($false))
    Cmd 'rfav reload' 300 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null; Press 'Tab1' | Out-Null
    Check 'entering the tab tells about the skipped lines' ((Cmd 'uistate') -match "message='즐겨찾기 파일에서 읽지 못한 줄 2개를 건너뛰었습니다") (Cmd 'uistate')
    Press 'Tab0' | Out-Null; Press 'Tab1' | Out-Null
    Check 'the notice is shown only once' ((Cmd 'uistate') -notmatch '건너뛰었습니다')
    Press 'ResGroupFav' | Out-Null; Press 'ResClear' | Out-Null
    Check 'a missing item is shown as such' ((Cmd 'uitext ItemName99999999') -match '게임에 없는 아이템 \(99999999\)') (Cmd 'uitext ItemName99999999')
    $p = Pos 'ItemGive99999999'
    Check 'its give button is off' ((Rc "click $($p.x) $($p.y)" 400) -notmatch 'Button\.onClick: ItemGive99999999')
    # Review Focus 1: 필터가 걸려도 게임에 없는 줄은 보이고 뺄 수 있다
    Press 'ResQuality6' | Out-Null
    Check 'a missing item stays visible under a filter' ((Cmd 'uipos ItemName99999999') -match ' at ' -and (Cmd 'uipos ItemName1011011') -match 'no element named')
    Press 'ResStar99999999' | Out-Null
    Check 'a missing item can be removed' ((Cmd 'rfav' 200) -match 'count=1 ') (Cmd 'rfav' 200)
    Press 'ResClear' | Out-Null
    Cmd 'rfav reset' 300 | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
  }
  'base' {
    $traps = Traps
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    foreach ($t in $tabProbe) {
      Press "Tab$($t[0])" | Out-Null
      Check "tab $($t[0]) shows $($t[1])" (IsAt $t[1])
    }
    Press 'Tab0' | Out-Null
    Check 'uitext reads a label' ((Cmd 'uitext Restore') -match "text='자원 회복'")
    Check 'a name prefix finds an element' ((Cmd 'uipos Plus*') -match '-> Plus\d+ at ')
    Check 'unknown name is reported' ((Cmd 'uipos NoSuchThing') -match 'no element named')
    # 05 문서 49절: Init 줄이 빌드 표지와 .devremote 경로를 찍는다
    $init = @(Get-Content -LiteralPath $log | Where-Object { $_ -match '\[TlToybox\] Init ' }) | Select-Object -Last 1
    Check 'the init line names the build' ($init -match ' build=[0-9a-f]{7,}(-dirty)? ') $init
    Check 'the init line names the devremote folder' ($init -match ' devremote=\S+ \((env|default)\)\s*$') $init
    Press 'Close' | Out-Null
    Check 'editor closed' ((Cmd 'uipos Tab0') -match 'no element named')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'tabs' {
    $traps = Traps
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    $s = Cmd 'uistate'
    Check 'state is readable' ($s -match 'open=True tab=\d+ target=player ') $s
    Check 'title shows the target' ((Cmd 'uitext Title') -match '편집 대상: .+\(플레이어\)')
    $p = Pos 'ToPlayer'
    Check 'ToPlayer is off while the target is the player' ((Rc "click $($p.x) $($p.y)" 300) -match 'not interactable')
    foreach ($t in $tabProbe) {
      Press "Tab$($t[0])" | Out-Null
      Check "tab $($t[0]) is selected" ((Cmd 'uistate') -match "tab=$($t[0]) ")
      Check "tab $($t[0]) shows $($t[1])" (IsAt $t[1])
    }
    # 05 문서 47절: 화폐 탭은 없고(자원 탭의 그룹), 탭 간격 46, 안전은 제목 줄의 단추 Tab14(열면 돌아가기, 다시 누르면 앞 탭).
    Check 'the currency tab is gone' ((Cmd 'uipos Tab2') -match 'no element named')
    $a = Pos 'Tab0'; $b = Pos 'Tab1'; $c = Pos 'Tab3'
    Check 'tab buttons are 46 apart' (($b.y - $a.y) -eq 46 -and ($c.y - $b.y) -eq 46) "y=$($a.y),$($b.y),$($c.y)"
    Check 'the safety button is in the title bar' ((Pos 'Tab14').y -lt $a.y) "safety y=$((Pos 'Tab14').y) tab0 y=$($a.y)"
    Press 'Tab5' | Out-Null; Press 'Tab14' | Out-Null
    Check 'the safety view opens from the title bar' ((Cmd 'uistate') -match 'tab=14 ' -and (IsAt 'BackupNow') -and (Cmd 'uitext Tab14') -match "text='돌아가기'")
    Press 'Tab14' | Out-Null
    Check 'pressing it again returns to the tab before' ((Cmd 'uistate') -match 'tab=5 ' -and (IsAt 'SkillAddOpen') -and (Cmd 'uitext Tab14') -match "text='안전'")
    Press 'Tab14' | Out-Null; Press 'Tab0' | Out-Null
    Check 'a tab button leaves the safety view' ((Cmd 'uistate') -match 'tab=0 ' -and (Cmd 'uitext Tab14') -match "text='안전'")
    Press 'Tab11' | Out-Null
    Check 'world tab no longer has the battle helpers' (-not (IsAt 'God'))

    # 탭 안의 하위 화면
    Press 'Tab5' | Out-Null; Press 'SkillAddOpen' | Out-Null
    Check 'skill add screen opens' (IsAt 'Back')
    Press 'Back' | Out-Null
    Check 'skill add screen closes' (IsAt 'SkillAddOpen')
    Press 'Tab8' | Out-Null; Press 'LuckAddOpen' | Out-Null
    Check 'luck picker opens' (IsAt 'Back')
    Press 'Back' | Out-Null; Press 'FateOpen' | Out-Null
    Check 'fate screen opens' (IsAt 'Back')
    Press 'Back' | Out-Null
    Check 'luck list is back' (IsAt 'LuckAddOpen')

    # 수량은 쓰는 곳마다 따로 기억한다 (스펙 6절 10번)
    Press 'Tab0' | Out-Null; Press 'attr100' | Out-Null
    Check 'attribute step is 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'Tab11' | Out-Null
    Check 'move step is still 10' ((Cmd 'uitext MoveUp') -match "text='y - 10'")
    Press 'move1000' | Out-Null
    Check 'move step is 1000' ((Cmd 'uitext MoveUp') -match "text='y - 1000'")
    Press 'Tab10' | Out-Null; Press 'NpcRefresh' | Out-Null; Press (Pos 'Pick*').name | Out-Null
    Check 'intim step is still 10' ((Cmd 'uitext IntimPlus') -match "text='\+ 10'")
    Press 'Tab0' | Out-Null
    Check 'attribute step is still 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'attr10' | Out-Null; Press 'Tab11' | Out-Null; Press 'move10' | Out-Null

    # 결과 영역 (스펙 6절 20번은 캡처로 본다)
    Press 'Tab0' | Out-Null; Press 'Restore' | Out-Null
    Check 'result line shows the outcome' ((Cmd 'uistate') -match "message='자원 회복: ")
    Press 'Tab1' | Out-Null
    Check 'tab change clears the result line' ((Cmd 'uistate') -match "message=''")
    Check 'a long result is accepted' ((Cmd 'uisay 30') -match 'said 360 chars')
    # 05 문서 48절(U3): 세 줄을 넘는 결과는 "…"과 더 보기. 펼치면 스크롤 칸에 전체, 접기·탭 전환으로 접힌다.
    Check 'a long result offers more' ((IsAt 'MessageMore') -and (Cmd 'uipos MessageEllipsis') -match ' at ' -and (Cmd 'uitext MessageMore') -match "text='더 보기'")
    Press 'MessageMore' | Out-Null
    Check 'more shows the whole text in a scroll box' ((Cmd 'uipos MessageFull') -match ' at ' -and (Cmd 'uitext MessageFullText') -match "(?s)text='.{360}'" -and (Cmd 'uitext MessageMore') -match "text='접기'")
    Check 'the tab column stays clickable while expanded' ((IsAt 'Tab13') -and (IsAt 'Tab15'))
    Shot 'long-message'
    Press 'MessageMore' | Out-Null
    Check 'fold hides the scroll box' ((Cmd 'uipos MessageFull') -match 'no element named' -and (IsAt 'MessageMore'))
    Press 'MessageMore' | Out-Null; Press 'Tab0' | Out-Null
    Check 'a tab change folds it' ((Cmd 'uipos MessageFull') -match 'no element named' -and (Cmd 'uipos MessageMore') -match 'no element named')
    Cmd 'uisay 1' | Out-Null
    Check 'a short result has no more button' ((Cmd 'uipos MessageMore') -match 'no element named' -and (Cmd 'uipos MessageEllipsis') -match 'no element named')
    Press 'Tab1' | Out-Null

    # 편집 대상: NPC로 바꿨다가 제목 줄의 버튼으로 되돌린다 (스펙 6절 15번)
    Press 'Tab10' | Out-Null; Press 'EditNpc' | Out-Null
    $s = Cmd 'uistate'
    Check 'target is the NPC and the first tab is shown' ($s -match 'tab=0 target=npc ') $s
    Check 'title shows the NPC' ((Cmd 'uitext Title') -match '\(NPC\)')
    Press 'ToPlayer' | Out-Null
    Check 'back to the player' ((Cmd 'uistate') -match 'target=player ' -and (Cmd 'uitext Title') -match '\(플레이어\)')

    # 삭제 확인 대기는 편집 대상을 바꾸면 풀린다. 제목 줄의 버튼으로 어느 탭에서나 대상을 바꿀 수 있기 때문이다.
    Press 'Tab10' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab8' | Out-Null
    $rm = (Pos 'LuckRemove*').name; Press $rm | Out-Null
    Check 'removing an NPC luck asks first' ((Cmd "uitext $rm") -match "text='정말 삭제'")
    Press 'ToPlayer' | Out-Null
    Press 'Tab10' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab8' | Out-Null
    Check 'the pending confirm is gone after the target changed' ((Cmd "uitext $rm") -match "text='삭제'")
    Press 'ToPlayer' | Out-Null

    # Review Focus 2: 대상으로 잡은 NPC가 없어지면 플레이어로 돌아간다
    $s = Cmd 'uitarget no-such-unit'
    Check 'a missing target falls back to the player' ($s -match 'target=player ' -and (Cmd 'uitext Title') -match '\(플레이어\)') $s
    Press 'Tab5' | Out-Null
    Check 'target tabs still draw after the fallback' (IsAt 'SkillAddOpen')

    Shot 'tabs'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'items' {
    # 스펙 6절 7, 8, 17번. 자원 탭의 목록·검색·상세, 가방 칸 한 번에 지정, 의복의 구분 표시.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupAll' | Out-Null; Press 'ResClear' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    if ($o -notmatch '전체 (\d+)개 중 1~(\d+)번째') { throw "아이템 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]; $screen = [int]$Matches[2]
    Check 'the list holds every item' ($total -gt 3000) $o
    Check 'only the visible rows are built' ((Cmd "uipos Row$($screen + 5)") -match 'no element named')
    Check 'paging buttons are gone' ((Cmd 'uipos ItemPageSize10') -match 'no element named' -and (Cmd 'uipos ItemSearch') -match 'no element named')
    Check 'the old category buttons are gone' ((Cmd 'uipos ItemCat*') -match 'no element named' -and (Cmd 'uipos ItemSub*') -match 'no element named')

    # 즉시 검색: 실제 키로 번호를 친다(이름과 번호 둘 다 찾는다)
    Cmd 'uifocus ItemQuery' | Out-Null
    Key 0x31 350; Key 0x30 350; Key 0x31 350
    $typed = Cmd 'uitext ItemQuery'
    $o = Cmd 'uitext ItemTableRange'
    Check 'the keys reached the search box' ($typed -match "text='101'") $typed
    Check 'typing filters at once' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -lt $total -and [int]$Matches[1] -gt 0) $o
    Check 'the search box is still in place' (IsAt 'ItemQuery')

    # Review Focus 1: 결과가 없을 때
    Cmd 'ui set ItemQuery zzzzzz' | Out-Null
    Check 'no match shows an empty list' ((Cmd 'uitext ItemTableRange') -match "text='0개'" -and (Cmd 'uipos ItemTableNext') -match 'no element named' -and (Cmd 'uipos Row0') -match 'no element named')
    Press 'ItemQueryClear' | Out-Null
    $o = (Cmd 'uitext ItemTableRange') + ' | ' + (Cmd 'uitext ItemQuery')
    Check 'clearing brings everything back' ($o -match "전체 $($total)개 중 1~" -and $o -match "ItemQuery text=''") $o

    # 이동 줄과 스크롤
    # 휠로 조금만 내려도(첫 줄 번호가 그대로여도) 이동 버튼이 켜진다
    $p = Pos 'ItemTable'
    Rc "scroll $($p.x) $($p.y) -0.5" 300 | Out-Null
    $f = Pos 'ItemTableFirst'
    Check 'a small scroll enables the first button' ((Rc "click $($f.x) $($f.y)" 400) -match 'Button.onClick: ItemTableFirst')
    Press 'ItemTableNext' | Out-Null
    Check 'next moves one screen' ((Cmd 'uitext ItemTableRange') -match "중 $($screen + 1)~")
    Press 'ItemTableLast' | Out-Null
    Check 'last reaches the end' ((Cmd 'uitext ItemTableRange') -match "~$($total)번째")
    $p = Pos 'ItemTable'
    Rc "scroll $($p.x) $($p.y) 3" 300 | Out-Null
    Check 'the wheel scrolls back up' ((Cmd 'uitext ItemTableRange') -notmatch "~$($total)번째")
    Press 'ItemTableLast' | Out-Null

    # Review Focus 2: 끝까지 내린 채 검색해 목록이 짧아진다
    Cmd 'ui set ItemQuery 101' | Out-Null
    Check 'a shorter result starts from the top' ((Cmd 'uitext ItemTableRange') -match '중 1~' -and (Cmd 'uipos Row0') -match ' at ')
    Press 'ItemQueryClear' | Out-Null

    # 지급 뒤에도 목록 위치가 그대로이고 보유 수가 바뀐다
    Press 'ItemTableNext' | Out-Null
    $before = Cmd 'uitext ItemTableRange'
    $give = Pos 'ItemGive*'
    $id = $give.name -replace 'ItemGive', ''
    $had = if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
    Press $give.name | Out-Null
    $has = if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
    Check 'giving an item works from the list' ($has -eq $had + 1) "id=$id $had -> $has"
    Check 'the list stays where it was' ((Cmd 'uitext ItemTableRange') -eq $before)
    Press "ItemName$id" | Out-Null
    Check 'pressing a name shows the whole text below' ((Cmd 'uitext ItemDetail') -match "\[$id\]")

    # 자원 탭 개편(스펙 7절): 그룹 → 종류 → 검색 → 품질 → 보유 중만, 설명 칸의 그룹/종류
    Press 'ResGroupPill' | Out-Null
    Check 'the first kind of pills is recovery' ((Cmd 'uitext ResKind1') -match "text='회복 \(\d+\)'") (Cmd 'uitext ResKind1')
    Press 'ResKind1' | Out-Null
    Cmd 'ui set ItemQuery 보원단' | Out-Null
    Press 'ResQuality1' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    Check 'group, kind, search and quality narrow to one row' ($o -match "text='전체 1개 중 1~1번째'" -and (Cmd 'uipos ItemName1011011') -match ' at ') $o
    Press 'ItemName1011011' | Out-Null
    Check 'the detail names the group and kind' ((Cmd 'uitext ItemDetail') -match '\[1011011\] 6품 보원단  \(단약 / 회복, 품질 1\)') (Cmd 'uitext ItemDetail')
    $had = if ((Cmd 'count 1011011') -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
    Press 'ResOwnedOnly' | Out-Null
    $want = if ($had -gt 0) { "text='전체 1개" } else { "text='0개'" }
    Check 'owned only follows the bag' ((Cmd 'uitext ItemTableRange') -match $want) "had=$had $(Cmd 'uitext ItemTableRange')"
    Press 'ResOwnedOnly' | Out-Null
    Press 'ItemGive1011011' | Out-Null
    Press 'ResOwnedOnly' | Out-Null
    Check 'after giving, owned only shows it' ((Cmd 'uitext ItemTableRange') -match "text='전체 1개") (Cmd 'uitext ItemTableRange')
    Press 'ResClear' | Out-Null
    $pillCount = if ((Cmd 'uitext ResGroupPill') -match "단약 (\d+)'") { $Matches[1] } else { 'x' }
    Check 'clear keeps the group and resets the rest' ((Cmd 'uitext ItemQuery') -match "text=''" -and (Cmd 'uitext ItemTableRange') -match "전체 $($pillCount)개") (Cmd 'uitext ItemTableRange')
    Press 'ResGroupAll' | Out-Null

    # 의복: 이름만으로 구분된다
    Cmd 'ui set ItemQuery 3041003' | Out-Null
    $o = Cmd 'uitext ItemName3041003'
    Check 'clothes show sex, shape and colour' ($o -match "text='의복\((남|여)\) \d+-\d+'") $o
    Press 'ItemQueryClear' | Out-Null

    # 화폐 그룹(05 문서 47절): 영석 말고도 화폐로 쓰이는 재화. 아이템 표의 줄로 보인다(☆ 없음, 보유 CurrencyOwned, 지급 CurrencyAdd).
    Press 'Tab1' | Out-Null; Press 'ResGroupCurrency' | Out-Null
    Check 'the currency group has no star column' ((Cmd 'uipos ResStar10001') -match 'no element named' -and (Cmd 'uitext CurrencyOwned10001') -match "text='\d+'")
    Check 'the currency group lists the other currencies' ((IsAt 'CurrencyAdd10011') -and (IsAt 'CurrencyAdd10041') -and (IsAt 'CurrencyAdd7000315') -and (IsAt 'CurrencyAdd2821101'))
    Check 'run-only currencies are left out' ((Cmd 'uipos CurrencyAdd5061016') -match 'no element named')
    $grid = if ((Cmd 'grid') -match 'cur=(\d+)') { [int]$Matches[1] } else { -1 }
    $step = if ((Cmd 'uitext CurrencyAdd10041') -match "text='\+ (\d+)'") { [int]$Matches[1] } else { -1 }   # 지급 수량(AmountBar item)
    foreach ($cid in 10041, 10011) {
      $had = if ((Cmd "count $cid") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
      Press "CurrencyAdd$cid" | Out-Null
      $has = if ((Cmd "count $cid") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
      Check "currency $cid goes up by the chosen amount" ($step -gt 0 -and $has -eq $had + $step) "$had -> $has step=$step"
    }
    Check 'the result names the currency, not a text key' ((Cmd 'uistate') -notmatch "message='item_name")
    Check 'currencies do not take bag slots' ((Cmd 'grid') -match "cur=$grid ")
    $money = if ((Cmd 'read') -match 'money=(\d+)') { [int]$Matches[1] } else { -1 }
    Press 'CurrencyAdd10001' | Out-Null
    Check 'spirit stones can be added here too' ((Cmd 'read') -match "money=$($money + $step) ")
    Check 'the owned column follows' ((Cmd 'uitext CurrencyOwned10001') -match "text='$($money + $step)'")
    Press 'ResGroupAll' | Out-Null
    Check 'the resources tab no longer has a currency screen' ((Cmd 'uipos CurrencyOpen') -match 'no element named')
    Press 'Tab1' | Out-Null

    # 가방 칸 한 번에 지정 (회차의 -End가 bag-slots.txt를 되돌린다)
    function Extra { if ((Cmd 'bagstat') -match 'extra=(\d+)') { [int]$Matches[1] } else { -1 } }
    $e0 = Extra
    $want = if ($e0 -eq 120) { 135 } else { 125 }
    Cmd 'uifocus BagExtra' | Out-Null; Cmd "ui set BagExtra $want" | Out-Null; Key 0x0D
    Check 'Enter sets the extra slots, rounded down to tens' ((Extra) -eq $want - 5) "extra=$(Extra)"
    # Review Focus 3: 범위 밖의 수는 맞춰서 적용하지 않고 거부한다
    $kept = Extra
    Cmd 'ui set BagExtra 501' | Out-Null; Press 'BagApply' | Out-Null
    Check 'a value above the limit is refused' ((Extra) -eq $kept -and (Cmd 'uistate') -match "message='가방: 추가 칸은 0~500") "extra=$(Extra)"
    Cmd 'ui set BagExtra -10' | Out-Null; Press 'BagApply' | Out-Null
    Check 'a negative value is refused' ((Extra) -eq $kept -and (Cmd 'uistate') -match "message='가방: 추가 칸은 0~500") "extra=$(Extra)"
    Cmd 'ui set BagExtra 500' | Out-Null; Press 'BagApply' | Out-Null
    Check 'the apply button sets the upper limit' ((Extra) -eq 500)
    Cmd 'ui set BagExtra 0' | Out-Null; Press 'BagApply' | Out-Null
    Check 'zero removes them' ((Extra) -eq 0)
    Press 'BagPlus' | Out-Null
    Check 'the +10 button still works' ((Extra) -eq 10)
    Cmd "ui set BagExtra $e0" | Out-Null; Press 'BagApply' | Out-Null   # 처음 값으로 되돌린다(같은 회차의 뒤 검사가 가방이 넘쳐 비급을 못 넣지 않게. 48절)
    Check 'the extra slots are back to the start' ((Extra) -eq $e0) "extra=$(Extra) start=$e0"
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'numbers' {
    # 스펙 6절 9번, Review Focus 3. 능력치의 저장값 칸과 수량 줄의 직접 입력.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null
    $cell = (Pos 'AttrValue*').name
    function Val { if ((Cmd "uitext $cell") -match "text='(-?\d+)'") { [int]$Matches[1] } else { throw "값을 읽지 못했습니다: $cell" } }
    $v = Val
    Check 'the direct-input row is gone' ((Cmd 'uipos AttributeApply') -match 'no element named')

    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $($v + 1)" | Out-Null
    Press 'attr10' | Out-Null
    Check 'without Enter nothing is applied' ((Val) -eq $v)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $($v + 1)" | Out-Null; Key 0x0D
    Check 'Enter applies the typed value' ((Val) -eq $v + 1) "value=$(Val)"
    Check 'the change is reported' ((Cmd 'uistate') -match "message='[^']*->")
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell 999999999" | Out-Null; Key 0x0D
    Check 'an out-of-range value is refused' ((Val) -eq $v + 1)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell" | Out-Null; Key 0x0D
    Check 'an empty value is refused with a reason' ((Val) -eq $v + 1 -and (Cmd 'uistate') -match "message='[^']*숫자")
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell -5" | Out-Null; Key 0x0D
    Check 'a negative value is refused' ((Val) -eq $v + 1)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $v" | Out-Null; Key 0x0D
    Check 'back to the original value' ((Val) -eq $v)

    # 수량 줄의 직접 입력도 Enter로 적용된다
    Cmd 'uifocus attrInput' | Out-Null; Cmd 'ui set attrInput 7' | Out-Null; Key 0x0D
    Check 'Enter applies a typed step' ((Cmd 'uitext Plus*') -match "text='\+ 7'")
    Cmd 'uifocus attrInput' | Out-Null; Cmd 'ui set attrInput 0' | Out-Null; Key 0x0D
    Check 'a step of zero is refused' ((Cmd 'uitext Plus*') -match "text='\+ 7'" -and (Cmd 'uistate') -match "message='수량은")
    Press 'attr10' | Out-Null
    # 세계 탭: 날짜 칸에서 Enter를 누르면 적용 버튼과 같다(확인은 그대로 두 번)
    Press 'Tab11' | Out-Null
    if ((Cmd 'tin') -notmatch 'input=(\d+)-(\d+)-(\d+) pending=False') { throw '날짜 입력 상태를 읽지 못했습니다.' }
    $day = [int]$Matches[3]; $other = if ($day -eq 12) { 13 } else { 12 }
    Cmd 'uifocus DateDay' | Out-Null; Cmd "ui set DateDay $other" | Out-Null; Key 0x0D
    Check 'Enter in a date box asks like the apply button' ((Cmd 'tin') -match "-$other pending=True" -and (Cmd 'uitext DateApply') -match "text='정말 변경'")
    Press 'Tab11' | Out-Null
    Check 'coming back to the tab cancels the date change' ((Cmd 'tin') -match "-$day pending=False")
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'people' {
    # NPC 탭의 목록과 즉시 검색.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab10' | Out-Null
    $o = Cmd 'uitext NpcListRange'
    if ($o -notmatch '전체 (\d+)개') { throw "NPC 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]
    Check 'the NPC list is a scrolling list' ($total -gt 8 -and (Cmd 'uipos NpcNext10') -match 'no element named' -and (Cmd 'uipos NpcSearch') -match 'no element named')
    Press 'NpcRefresh' | Out-Null
    $pick = Pos 'Pick*'
    Press $pick.name | Out-Null
    Check 'picking an NPC shows the favour buttons' (IsAt 'IntimPlus')
    $name = if ((Cmd 'uitext SelectedName') -match "text='선택: (\S+)") { $Matches[1] } else { '' }
    Cmd "ui set NpcQuery $name" | Out-Null
    $o = Cmd 'uitext NpcListRange'
    Check 'typing a name narrows the list' ($name.Length -gt 0 -and $o -match '전체 (\d+)개' -and [int]$Matches[1] -ge 1 -and [int]$Matches[1] -lt $total) $o
    Cmd 'ui set NpcQuery zzzzzz' | Out-Null
    Check 'no match shows an empty list' ((Cmd 'uitext NpcListRange') -match "text='0개'")
    Press 'NpcQueryClear' | Out-Null
    Check 'clearing brings everyone back' ((Cmd 'uitext NpcListRange') -match "전체 $($total)개")
    Press 'NpcListLast' | Out-Null
    Check 'the list scrolls to the end' ((Cmd 'uitext NpcListRange') -match "~$($total)번째")
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'history' {
    # 스펙 6절 18번. 안전 탭의 변경 이력 전체 보기. 방금 한 변경이 맨 위에 오는지 보려고 먼저 하나를 만든다.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null; Press 'Restore' | Out-Null
    Press 'Tab14' | Out-Null
    $o = Cmd 'uitext HistoryRange'
    if ($o -notmatch '전체 (\d+)개') { throw "이력 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]
    $lines = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ }).Count
    Check 'past runs are listed too' ($total -eq [Math]::Min(2000, $lines)) "list=$total file=$lines"
    Check 'the newest change is on top' ((Cmd 'uitext Row0') -match '자원 회복')
    Cmd 'ui set HistoryQuery 가방' | Out-Null
    $o = Cmd 'uitext HistoryRange'
    Check 'the search narrows it' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -lt $total) $o
    Press 'Row0' | Out-Null
    Check 'pressing a line shows it whole' ((Cmd 'uitext HistoryDetail') -match '가방')
    Press 'HistoryQueryClear' | Out-Null
    Check 'the backup button is still there' (IsAt 'BackupNow')
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'confirm' {
    # 스펙 6절 11~14번, Review Focus 4·5. 하위 화면과 확인 대기.
    $traps = Traps
    Cmd 'editor' | Out-Null
    Press 'Tab10' | Out-Null; Press 'NpcRefresh' | Out-Null; Press (Pos 'Pick*').name | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab8' | Out-Null
    $rm = (Pos 'LuckRemove*').name
    Press $rm | Out-Null
    Check 'the first press only asks' ((Cmd "uitext $rm") -match "text='정말 삭제'")
    Press 'Tab5' | Out-Null; Press 'Tab8' | Out-Null
    Check 'a tab change cancels it' ((Cmd "uitext $rm") -match "text='삭제'")
    Press $rm | Out-Null
    Press 'LuckAddOpen' | Out-Null
    Check 'a sub screen opens with one back button' ((IsAt 'Back') -and (Cmd 'uipos LuckAddOpen') -match 'no element named')
    Press 'Back' | Out-Null
    Check 'opening a sub screen cancels it' ((Cmd "uitext $rm") -match "text='삭제'")
    Press $rm | Out-Null
    Press 'ToPlayer' | Out-Null
    Press 'Tab10' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab8' | Out-Null
    Check 'a target change cancels it' ((Cmd "uitext $rm") -match "text='삭제'")

    # Review Focus 5: 하위 화면이 열린 채 대상을 바꾸거나 탭을 떠난다
    Press 'FateOpen' | Out-Null
    Check 'the fate screen opens' (IsAt 'Back')
    Press 'ToPlayer' | Out-Null
    Check 'a target change closes the sub screen' ((IsAt 'LuckAddOpen') -and (Cmd 'uistate') -match 'target=player ')
    Press 'LuckAddOpen' | Out-Null
    Cmd 'ui set LuckQuery zzzzzz' | Out-Null
    Check 'the picker search is immediate' ((Cmd 'uitext LuckPickRange') -match "text='0개'")
    Press 'Tab5' | Out-Null; Press 'Tab8' | Out-Null
    Check 'leaving the tab closes the sub screen' (IsAt 'LuckAddOpen')

    # 공법: 기술 추가 화면의 목록과 검색, 옵션 교체 화면
    Press 'Tab5' | Out-Null; Press 'SkillAddOpen' | Out-Null
    $o = Cmd 'uitext SkillListRange'
    Check 'the skill list is a scrolling list' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -gt 12) $o
    Cmd 'ui set SkillQuery zzzzzz' | Out-Null
    Check 'the skill search is immediate' ((Cmd 'uitext SkillListRange') -match "text='0개'")
    Press 'SkillQueryClear' | Out-Null
    Press 'Back' | Out-Null
    Press (Pos 'PickMartial*').name | Out-Null
    Check 'picking a skill shows its options' (IsAt 'MartialEquip')
    Press 'MartialForget' | Out-Null
    Check 'forgetting asks first' ((Cmd 'uitext MartialForget') -match "text='정말 망각'")
    Press 'MartialExp' | Out-Null
    Check 'another action cancels it' ((Cmd 'uitext MartialForget') -match "text='기술 망각'")

    # 기예: 목록과 검색
    Press 'Tab9' | Out-Null
    $o = Cmd 'uitext ArtTableRange'
    Check 'the art list is a scrolling list' ($o -match '전체 (\d+)개') $o
    Cmd 'ui set ArtQuery zzzzzz' | Out-Null
    Check 'the art search is immediate' ((Cmd 'uitext ArtTableRange') -match "text='0개'")
    Press 'ArtQueryClear' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'actions' {
    # 스펙 6절 12~14번. 새 목록에서 실제 동작이 서비스까지 가는지: 기술 배우기, 운명 추가와 삭제, 서적 배우기·잊기.
    $traps = Traps
    # 확인이 필요한 버튼이면 한 번 더 누른다.
    function PressThrough([string]$name) {
      Press $name | Out-Null
      if ((Cmd "uitext $name") -match "text='정말") { Press $name | Out-Null }
      Cmd 'uistate'
    }
    Cmd 'editor' | Out-Null
    Cmd 'uitarget player' | Out-Null

    Press 'Tab5' | Out-Null
    $had = if ((Cmd 'uitext MartialListRange') -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 }
    Press 'SkillAddOpen' | Out-Null
    $s = PressThrough (Pos 'SkillAdd*').name
    Press 'Back' | Out-Null
    $has = if ((Cmd 'uitext MartialListRange') -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 }
    Check 'adding a skill puts one more in the learned list' ($has -eq $had + 1) "$had -> $has; $s"
    "INFO skill: $($s -replace '.*message=', '')"

    Press 'Tab8' | Out-Null; Press 'LuckAddOpen' | Out-Null; Press 'LuckType2' | Out-Null
    Press 'LuckQueryClear' | Out-Null   # 앞선 검사가 검색어를 남겼을 수 있다
    $added = $null
    for ($i = 0; $i -lt 6 -and -not $added; $i++) {
      $p = Pos 'LuckAdd*'
      if ((Rc "click $($p.x) $($p.y)" 600) -match 'Button\.onClick: ') { $added = $p.name -replace 'LuckAdd', '' } else { Press 'LuckPickNext' | Out-Null }
    }
    $s = Cmd 'uistate'
    Check 'adding a luck reports a result' ($added -and $s -match "message='[^']+'") $s
    "INFO luck add: $($s -replace '.*message=', '')"
    Press 'Back' | Out-Null
    # 방금 붙인 운명은 목록의 끝에 온다. 목록이 한 화면보다 길면 끝으로 간다.
    if ((Cmd 'uipos LuckListLast') -match ' at ') { try { Press 'LuckListLast' | Out-Null } catch { } }
    Check 'the added luck is in the list' ((Cmd "uipos LuckRemove$added") -match ' at ')
    $s = PressThrough "LuckRemove$added"
    Check 'removing it reports a result' ($s -match "message='[^']+'" -and (Cmd "uipos LuckRemove$added") -match 'no element named') $s
    "INFO luck remove: $($s -replace '.*message=', '')"

    Press 'Tab9' | Out-Null
    $book = (Pos 'ArtAct*').name
    $label0 = Cmd "uitext $book"
    $s = PressThrough $book
    $label1 = Cmd "uitext $book"
    Check 'the art button changes the book state' ($label1 -ne $label0) "$label0 -> $label1; $s"
    "INFO art 1: $($s -replace '.*message=', '')"
    $s = PressThrough $book
    Check 'and the opposite action brings it back' ((Cmd "uitext $book") -eq $label0) $s
    "INFO art 2: $($s -replace '.*message=', '')"

    # 05 문서 48절(C1): 깨진 공법 줄은 "(읽을 수 없음)"으로 그려지고 망각으로 지울 수 있다. mbreak가 기술 번호가 없는 공법을 넣는다.
    Press 'Tab5' | Out-Null
    $had = if ((Cmd 'uitext MartialListRange') -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 }
    # 깨진 공법은 게임의 삭제 행동이 예외를 내므로(실측: Il2CppException NullReferenceException) 망각이 대체 경로(장착 해제 → 목록에서 직접 뺌)를 타고 "(fallback)" 로그 한 줄을 남긴다.
    function Fallbacks { @(Get-Content -LiteralPath $log | Where-Object { $_ -match '\[TlToybox\] .*\(fallback\) ' }).Count }
    $fb0 = Fallbacks
    $o = Cmd 'mbreak' 1500
    Press 'Tab0' | Out-Null; Press 'Tab5' | Out-Null
    if ((Cmd 'uipos MartialListLast') -match ' at ') { Press 'MartialListLast' | Out-Null }
    $has = if ((Cmd 'uitext MartialListRange') -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 }
    Check 'the broken martial is in the list' ($has -eq $had + 1 -and (Cmd 'uipos MartialBroken*') -match ' at ') "$had -> $has; $o"
    Check 'the other rows are still drawn' (IsAt 'SkillAddOpen')
    if ((Cmd 'uipos MartialBroken*') -match '-> MartialBroken(\d+) at ') {
      Press "PickMartial$($Matches[1])" | Out-Null
      Check 'picking it shows the notice and the forget button' ((Cmd 'uipos MartialBrokenNote') -match ' at ' -and (IsAt 'MartialForget'))
      $s = PressThrough 'MartialForget'
      Check 'forgetting removes it' ($s -match "message='공법 망각" -and (Cmd 'uipos MartialBroken*') -match 'no element named') $s
      Check 'it went through the delete fallback once' ((Fallbacks) -eq $fb0 + 1) "fallback lines $fb0 -> $(Fallbacks)"
    }
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'confirm2' {
    # 05 문서 48절(U2): 선천운명 교체·역천개명 정하기·기술 모두 배우기·기예 모두 배우기는 두 번 눌러야 한다.
    $traps = Traps
    function Range([string]$name) { if ((Cmd "uitext $name") -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 } }
    # 켜진 첫 단추(uibuttons: 이름 on|off text='…')
    function FirstOn([string]$prefix) { foreach ($line in ((Cmd "uibuttons $prefix" 300) -split "`n")) { if ($line -match "^(\S+) on ") { return $Matches[1] } }; $null }
    Cmd 'editor' | Out-Null; Cmd 'uitarget player' | Out-Null

    # 기술 모두 배우기(심법 분류)
    Press 'Tab5' | Out-Null
    $had = Range 'MartialListRange'
    Press 'SkillAddOpen' | Out-Null; Press 'SkillKind3' | Out-Null
    Press 'SkillLearnAll' | Out-Null
    Check 'learn-all asks first' ((Cmd 'uitext SkillLearnAll') -match "text='정말 모두 배우기'" -and (Cmd 'uistate') -match '한 번 더 누르면')
    Press 'Back' | Out-Null
    Check 'the first press learns nothing' ((Range 'MartialListRange') -eq $had) "$had -> $(Range 'MartialListRange')"
    Press 'SkillAddOpen' | Out-Null; Press 'SkillKind3' | Out-Null
    Press 'SkillLearnAll' | Out-Null; Press 'SkillLearnAll' 4000 | Out-Null
    $s = Cmd 'uistate'
    Press 'Back' | Out-Null
    Check 'the second press learns them' ((Range 'MartialListRange') -gt $had) "$had -> $(Range 'MartialListRange'); $s"
    # 리뷰 08의 22번(10 문서 5절): 모드 공법 줄은 배우기 단추가 꺼져 있고 길을 알린다. 모두 배우기는 모드를 따로 센다
    if ((Cmd 'rmods' 3000) -match '1:984555698:') {
      $had = Range 'MartialListRange'
      Press 'SkillAddOpen' | Out-Null; Press 'SkillKind1' | Out-Null
      Cmd 'ui set SkillQuery 열염' | Out-Null
      $t = Cmd 'uitext SkillAdd984555698'
      Check 'a mod skill row shows a disabled button pointing to the books group' ($t -match "text='자원 탭 기술 서적'") $t
      # 꺼진 단추는 Press가 "not interactable"로 거부한다(누르지 못한다는 것이 곧 판정)
      $refused = try { Press 'SkillAdd984555698' | Out-Null; '' } catch { "$_" }
      $u = Cmd 'uistate'
      Check 'the mod row button cannot be pressed and does not call learn' ($refused -match 'not interactable' -and $u -notmatch '모드 공법은 자원 탭의 기술 서적에서 비급으로') "$refused | $u"
      Cmd 'ui set SkillQuery ' | Out-Null
      Press 'SkillLearnAll' | Out-Null; Press 'SkillLearnAll' 8000 | Out-Null
      $s = Cmd 'uistate'
      Check 'learn-all counts mod skills apart from the out-of-range ones' ($s -match '모드 공법 \d+개는 자원 탭의 기술 서적에서 받습니다') $s
      Press 'Back' | Out-Null
      Check 'learn-all still learns the plain ones' ((Range 'MartialListRange') -ge $had) "$had -> $(Range 'MartialListRange')"
    } else { 'INFO mod skill 984555698 is not loaded; mod row check skipped' }

    # 기예 모두 배우기: 아직 다 배우지 않은 분류를 고른다(단추 글 "<분류> (배움/전체)")
    Press 'Tab9' | Out-Null
    $artKind = -1
    for ($i = 0; $i -lt 6 -and $artKind -lt 0; $i++) { if ((Cmd "uitext ArtKind$i") -match "\((\d+)/(\d+)\)'" -and $Matches[1] -ne $Matches[2]) { $artKind = $i } }
    if ($artKind -lt 0) { 'INFO every art kind is fully learned; not exercised' }
    else {
      Press "ArtKind$artKind" | Out-Null
      $label0 = Cmd "uitext ArtKind$artKind"
      Press 'ArtLearnAll' | Out-Null
      Check 'art learn-all asks first' ((Cmd 'uitext ArtLearnAll') -match "text='정말 모두 배우기'" -and (Cmd "uitext ArtKind$artKind") -eq $label0)
      Press 'ArtLearnAll' 4000 | Out-Null
      # 실행됐는지는 결과 문장(기예: …)으로 본다. 고정본이 그 분류의 남은 책을 배울 조건이 안 되면 수는 그대로다(ws-1017i: 단방 136권 가운데 0권).
      Check 'the second press runs learn-all' ((Cmd 'uistate') -match "message='[^':]+: \d+개를 배웠습니다" -and (Cmd 'uitext ArtLearnAll') -notmatch "text='정말") "$label0 -> $(Cmd "uitext ArtKind$artKind"); $(Cmd 'uistate')"
    }

    # 선천운명 교체
    Press 'Tab8' | Out-Null
    Press (Pos 'LuckReplace*').name | Out-Null
    $pick = FirstOn 'LuckAdd'
    Check 'the replace picker has a candidate' ($null -ne $pick)
    if ($pick) {
      Press $pick | Out-Null
      Check 'replacing asks first' ((Cmd "uitext $pick") -match "text='정말 교체'" -and (Cmd 'uistate') -match '한 번 더 누르면' -and (IsAt 'Back'))
      Press $pick | Out-Null
      Check 'the second press replaces and closes the picker' ((IsAt 'LuckAddOpen') -and (Cmd 'uistate') -match "message='[^']+'") (Cmd 'uistate')
    }

    # 역천개명 정하기
    if (IsAt 'Back') { Press 'Back' | Out-Null }   # 교체 고르기가 열린 채면 닫는다
    Press 'FateOpen' | Out-Null
    Press (Pos 'FateChange*').name | Out-Null
    $pick = FirstOn 'FatePick'
    Check 'the fate picker has a candidate' ($null -ne $pick)
    if ($pick) {
      Press $pick | Out-Null
      Check 'picking a fate asks first' ((Cmd "uitext $pick") -match "text='정말 정하기'" -and (Cmd 'uistate') -match '한 번 더 누르면')
      Press $pick | Out-Null
      Check 'the second press sets it and goes back to the slots' ((Cmd 'uipos FateChange*') -match ' at ' -and (Cmd 'uistate') -match "message='[^']+'") (Cmd 'uistate')
    }
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'errors' {
    # 05 문서 48절(C2): 단추 콜백과 탭 그리기의 예외가 결과 칸·내용 영역에 보인다. DevThrow·uifail은 개발 빌드의 시험 장치.
    $traps = Traps
    function TestErrors { @(Get-Content -LiteralPath $log | Where-Object { $_ -match '\[TlToybox\] .*InvalidOperationException: test-exception' }).Count }   # [cmd] 줄(uistate의 message)은 빼고 센다
    $w0 = @(ModWarnings).Count; $e0 = TestErrors
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null
    Press 'DevThrow' | Out-Null
    Check 'a button exception shows in the result line' ((Cmd 'uistate') -match "message='오류: InvalidOperationException: test-exception'") (Cmd 'uistate')
    Press 'Tab1' | Out-Null; Press 'Tab0' | Out-Null
    Check 'the window still works' (IsAt 'Restore')
    Cmd 'uifail' | Out-Null
    Press 'Tab1' | Out-Null
    Check 'a draw exception shows a notice' ((Cmd 'uitext DrawError') -match "text='이 탭을 그리지 못했습니다: InvalidOperationException: test-exception'") (Cmd 'uitext DrawError')
    Press 'Tab0' | Out-Null
    Check 'the next draw is normal' ((IsAt 'Restore') -and (Cmd 'uipos DrawError') -match 'no element named')
    Press 'Close' | Out-Null
    Check 'no trampoline line in the log' ((Traps) -eq $traps)
    Check 'one button error and one draw error were logged' ((TestErrors) -eq $e0 + 2) "test-exception lines $e0 -> $(TestErrors)"
    Check 'no other warning from the mod' (@(ModWarnings).Count -eq $w0) ((ModWarnings) -join ' | ')
  }
  'backup' {
    # 05 문서 48절(C5): 백업 없이 쓰던 세 곳이 쓰기 전에 백업한다. EnsureOnce는 한 실행에 한 번이라 항목마다 bkreset으로 기록을 지운다.
    # 백업 폴더 이름이 초 단위라 항목 사이에 1초 넘게 쉰다.
    $traps = Traps
    function Backups { $d = Join-Path $data 'backups'; if (Test-Path -LiteralPath $d) { @(Get-ChildItem -LiteralPath $d -Directory).Count } else { 0 } }
    # (1) 게임의 돌파 화면 열기: 난관 기운을 지우기 전에 백업
    Cmd 'bkreset' | Out-Null; $n = Backups
    Cmd 'addexp 100000' 500 | Out-Null
    $r = Cmd 'upopen' 2000
    Check 'opening the breakthrough screen backs up first' ((Backups) -eq $n + 1 -and $r -match 'opened') "$r backups $n -> $(Backups)"
    Start-Sleep -Milliseconds 1200
    # (2) 경지 올리기(이미 그 경지 이상 → 보충만): 칭호 재판정 전에 백업
    Cmd 'bkreset' | Out-Null; $n = Backups
    $grade = if ((Cmd 'cstate' 200) -match 'grade=(\d+)') { [int]$Matches[1] } else { 0 }
    $r = Cmd "pgraise $grade" 3000
    Check 'settling the grade backs up first' ((Backups) -eq $n + 1 -and $r -match '이미') "$r backups $n -> $(Backups)"
    Start-Sleep -Milliseconds 1200
    # (3) 역천개명 여러 칸 정하기: 다른 칸을 비우기 전에 백업(비운 뒤 도달하지 않은 경지라 거절되는 경우)
    $c = Cmd 'fateclash' 500
    if ($c -notmatch 'held=(\d+) heldId=(\d+) target=(\d+)') { "INFO no fate to clash with in this save; not exercised: $c" }
    else {
      $held = $Matches[1]; $heldId = $Matches[2]; $target = $Matches[3]
      Cmd 'bkreset' | Out-Null; $n = Backups
      $r = Cmd "fateamong $target $heldId $held" 2000
      Check 'setting a fate among pending slots backs up before emptying another slot' ((Backups) -eq $n + 1 -and $r -match '아직 도달하지') "$r backups $n -> $(Backups)"
    }
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'import-settle' {
    # 05 문서 48절(C4): 캐릭터 가져오기의 경지 구역 뒤에 경지에 따른 보충(선력·등선 진입 선법)이 따라온다.
    $traps = Traps
    $dir = Join-Path $data 'exports\character'
    $file = Join-Path $dir 'uicheck-S.json'
    if (Test-Path -LiteralPath $file) { [IO.File]::Delete($file) }
    Cmd 'cexport uicheck-S' 800 | Out-Null
    $pack = Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json
    $pack.grade.id = 44; $pack.grade.exp = 0
    [IO.File]::WriteAllText($file, ($pack | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding $false))
    Check 'the character starts without immortal power' ((Cmd 'imm' 200) -match 'immortalPoint=0 ') (Cmd 'imm' 200)
    $r = Cmd 'cimpsec uicheck-S grade' 4000
    Check 'the grade section reports the settle' ($r -match '경지에 따라 빠져 있던 것을 채웠습니다') $r
    $i = Cmd 'imm' 200
    Check 'the immortal power and the first art follow the imported grade' ($i -match 'immortalPoint=6 cards=5 ') $i
    Cmd 'uiclose GetImmortalSkill' 500 | Out-Null
    [IO.File]::Delete($file)
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'vault' {
    # 묶음 C(docs/superpowers/specs/2026-10-13-bag-export-design.md 6절). 내보내기 폴더는 세이브 밖이라 회차가 되돌리지 않는다.
    # 이 검사가 만든 파일(uicheck-로 시작)은 끝날 때와 멈췄을 때 지운다.
    $traps = Traps
    $dir = Join-Path $data 'exports\bag'
    function CleanVault { if (Test-Path -LiteralPath $dir) { Get-ChildItem -LiteralPath $dir -Filter 'uicheck-*' | ForEach-Object { [IO.File]::Delete($_.FullName) } } }
    function Num([string]$cmd, [string]$pattern) { if ((Cmd $cmd 100) -match $pattern) { [int]$Matches[1] } else { -1 } }
    function Msg { if ((Cmd 'uistate' 100) -match "message='(.*)'\s*$") { $Matches[1] } else { '' } }
    CleanVault
    try {
      Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null; Press 'VaultModeBag' | Out-Null   # 보관함은 마지막에 본 쪽(가방/캐릭터)을 기억한다

      # 6절 1~3번, Review Focus 2: 이름과 파일. 영석과 화폐는 가방의 아이템 목록에도 들어 있어(한 종에 한 줄) 아이템 줄에서는 빠진다
      Cmd 'ui set VaultName' | Out-Null   # 이름 칸은 앞서 적은 것을 기억한다. 비우고 시작한다
      Press 'VaultExport' | Out-Null
      Check 'an empty name is refused' ((Msg) -match '이름을' -and -not (Test-Path -LiteralPath (Join-Path $dir '.json')))
      Cmd 'ui set VaultName uicheck-A' | Out-Null; Press 'VaultExport' | Out-Null
      $fileA = Join-Path $dir 'uicheck-A.json'
      Check 'export writes a file' ((Test-Path -LiteralPath $fileA) -and (Msg) -match '내보냈습니다') (Msg)
      $pack = Get-Content -LiteralPath $fileA -Raw -Encoding UTF8 | ConvertFrom-Json
      $all = Num 'read' 'props=(\d+)'
      Check 'the file is valid JSON with every plain item' ($pack.format -eq 1 -and $pack.kind -eq 'bag' -and $pack.items.Count + $pack.currencies.Count -eq $all - $pack.skipped -and $pack.items.Count -gt 0) "items=$($pack.items.Count) skipped=$($pack.skipped) bag=$all"
      Check 'currencies are in the file' (@($pack.currencies | Where-Object { $_.id -eq 10001 -and $_.count -gt 0 }).Count -eq 1)
      $moneyIds = @($pack.currencies | ForEach-Object { $_.id })
      Check 'currencies are not also listed as items' (@($pack.items | Where-Object { $moneyIds -contains $_.id }).Count -eq 0) "$(@($pack.items | Where-Object { $moneyIds -contains $_.id } | ForEach-Object { $_.id }) -join ',')"
      Check 'equipped items are marked' (@($pack.items | Where-Object { $_.equipped }).Count -gt 0)
      Cmd 'ui set VaultName uicheck-A' | Out-Null; Press 'VaultExport' | Out-Null
      Check 'the same name gets a number' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-A (2).json'))
      Cmd 'ui set VaultName uicheck-b:c*d?' | Out-Null; Press 'VaultExport' | Out-Null
      Check 'characters a file name cannot hold become underscores' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))
      Check 'the header shows the bag slots' ((Cmd 'uitext VaultBagSlots') -match '가방 \d+/\d+') (Cmd 'uitext VaultBagSlots')
      Cmd 'ui set VaultQuery UICHECK-A' | Out-Null
      Check 'the search ignores case' ((Cmd 'uitext VaultListRange') -match '전체 2개') (Cmd 'uitext VaultListRange')
      Press 'VaultQueryClear' | Out-Null
      Check 'the list shows three exports' ((Cmd 'uitext VaultListRange') -match '전체 3개' -or (Num 'uitext VaultListRange' '전체 (\d+)개') -ge 3)

      # 가져오기의 기준이 되는 묶음을 손으로 만든다: 겹쳐지는 것 1, 겹쳐지지 않는 것 2(같은 번호 두 줄), 비급 1, 화폐 1, 그리고 걸러져야 할 줄들
      $martial = @($pack.items | Where-Object { $_.type -eq 'Martial' })[0]
      $stack = @($pack.items | Where-Object { $_.type -eq 'Props' -and $_.count -gt 1 })[0]
      $single = @($pack.items | Where-Object { $_.type -eq 'Props' -and $_.count -eq 1 -and -not $_.equipped })[0]
      $made = [ordered]@{ format = 1; kind = 'bag'; name = 'uicheck-M'; exportedAt = '2026-10-13 00:00:00'; character = 'test'; skipped = 0
        items = @(
          [ordered]@{ id = $stack.id; name = $stack.name; count = 3; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $single.id; name = $single.name; count = 1; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $single.id; name = $single.name; count = 1; type = 'Props'; values = @(); equipped = $true },
          [ordered]@{ id = 0; name = $martial.name; count = 1; type = 'Martial'; values = $martial.values; equipped = $false },
          [ordered]@{ id = 999999999; name = 'no such item'; count = 1; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $stack.id; name = 'zero'; count = 0; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $stack.id; name = 'negative'; count = -5; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = 10001; name = 'money as an item'; count = 5; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = 0; name = 'empty manual'; count = 1; type = 'Martial'; values = @(); equipped = $false })
        currencies = @([ordered]@{ id = 10041; name = '성주령'; count = 7 }) }
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-M.json'), ($made | ConvertTo-Json -Depth 6))
      Press 'Tab3' | Out-Null   # 탭으로 다시 들어오면 목록을 새로 읽는다. 방금 쓴 파일이 맨 위(0번 줄)다
      Check 'the hand-made export is on top' ((Cmd 'uitext Row0') -match 'uicheck-M')

      # 6절 4~8번: 세부 화면에서 하나씩
      Press 'VaultOpen0' | Out-Null
      Check 'the detail screen lists items and currencies' ((IsAt 'Back') -and (Cmd 'uitext VaultItemsRange') -match '전체 10개')
      $n0 = Num "count $($stack.id)" 'count=(\d+)'; $g0 = Num 'grid' 'cur=(\d+)'
      Press 'VaultGet0' | Out-Null
      Check 'a stackable item adds to the stack without a slot' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n0 + 3 -and (Num 'grid' 'cur=(\d+)') -eq $g0) (Msg)
      $n1 = Num "count $($single.id)" 'count=(\d+)'
      Press 'VaultGet1' | Out-Null
      Check 'a single item takes one slot' ((Num "count $($single.id)" 'count=(\d+)') -eq $n1 + 1 -and (Num 'grid' 'cur=(\d+)') -eq $g0 + 1) (Msg)
      $p0 = Num 'read' 'props=(\d+)'
      Press 'VaultGet3' | Out-Null
      Check 'a manual is recreated' ((Num 'read' 'props=(\d+)') -eq $p0 + 1) (Msg)
      $n2 = Num "count $($stack.id)" 'count=(\d+)'
      # 쓸 수 없는 줄은 이유가 줄에 보이고 버튼이 눌리지 않는다
      $why = (Cmd 'uidescs VaultWhy' 200)
      $p4 = Pos 'VaultGet4'; $p5 = Pos 'VaultGet5'
      Check 'an unknown item shows why and cannot be imported' ($why -match "VaultWhy4 text='[^']*게임에 없는" -and (Rc "click $($p4.x) $($p4.y)" 300) -notmatch 'Button\.onClick') $why
      Check 'a zero count shows why and cannot be imported' ($why -match "VaultWhy5 text='[^']*수량" -and (Rc "click $($p5.x) $($p5.y)" 300) -notmatch 'Button\.onClick' -and (Num "count $($stack.id)" 'count=(\d+)') -eq $n2) $why
      $c0 = Num 'count 10041' 'count=(\d+)'
      Press 'VaultGetC0' | Out-Null
      Check 'a currency adds its amount' ((Num 'count 10041' 'count=(\d+)') -eq $c0 + 7) (Msg)
      Cmd 'ui set VaultItemQuery 성주령' | Out-Null
      Check 'the detail search filters' ((Cmd 'uitext VaultItemsRange') -match '전체 1개')
      Press 'VaultItemQueryClear' | Out-Null
      Press 'Back' | Out-Null

      # 6절 9번: 빈 칸이 넉넉할 때 전부
      $n3 = Num "count $($stack.id)" 'count=(\d+)'; $n4 = Num "count $($single.id)" 'count=(\d+)'; $c1 = Num 'count 10041' 'count=(\d+)'; $p1 = Num 'read' 'props=(\d+)'; $m1 = Num 'count 10001' 'count=(\d+)'
      Press 'VaultAll0' | Out-Null
      $m = Msg
      Check 'import-all adds everything valid' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n3 + 3 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n4 + 2 -and (Num 'count 10041' 'count=(\d+)') -eq $c1 + 7 -and (Num 'read' 'props=(\d+)') -eq $p1 + 3) $m
      Check 'the result counts what went in and what was skipped' ($m -match '아이템 4줄' -and $m -match '화폐 1종' -and $m -match '건너뜀 5줄') $m
      Check 'a currency written as an item line is not imported' ((Num 'count 10001' 'count=(\d+)') -eq $m1)

      # 6절 10~11번, Review Focus 3: 가방을 가득 채운 뒤
      Cmd 'gridfill 600 5' 4000 | Out-Null
      $extra0 = Num 'bagstat' 'extra=(\d+)'; $n5 = Num "count $($stack.id)" 'count=(\d+)'; $n6 = Num "count $($single.id)" 'count=(\d+)'
      Press 'VaultAll0' | Out-Null
      Check 'a full bag asks instead of importing' ((IsAt 'VaultExpand0') -and (IsAt 'VaultFit0') -and (Msg) -match '필요' -and (Num "count $($stack.id)" 'count=(\d+)') -eq $n5) (Msg)
      Press 'VaultFit0' | Out-Null
      $m = Msg
      Check 'fit-only adds the stackable and reports the rest' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n5 + 3 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n6 -and $m -match '못 넣음 3줄' -and (Num 'bagstat' 'extra=(\d+)') -eq $extra0) $m
      Press 'VaultAll0' | Out-Null; Press 'VaultExpand0' | Out-Null
      $m = Msg
      Check 'expand-and-import grows the bag by tens and adds everything' ((Num 'bagstat' 'extra=(\d+)') -eq $extra0 + 10 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n6 + 2 -and $m -notmatch '못 넣음') $m

      # 가방이 넘쳐 있을 때(추가 칸을 줄여 쓰는 칸이 전체보다 많다)도 "늘리고 모두"는 넘친 만큼까지 늘려야 한다
      Cmd 'gridfill 600 5' 4000 | Out-Null; Cmd 'bag -10' | Out-Null
      $over = (Num 'grid' 'cur=(\d+)') - (Num 'grid' 'max=(\d+)')
      $extra1 = Num 'bagstat' 'extra=(\d+)'; $n7 = Num "count $($single.id)" 'count=(\d+)'
      Press 'VaultAll0' | Out-Null; Press 'VaultExpand0' | Out-Null
      $m = Msg
      Check 'expanding an overfull bag covers the overflow too' ($over -gt 0 -and (Num 'bagstat' 'extra=(\d+)') -eq $extra1 + [math]::Ceiling(($over + 3) / 10) * 10 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n7 + 2 -and $m -notmatch '못 넣음') "over=$over $m"

      # 6절 12번, Review Focus 1·4: 읽을 수 없는 파일, 밖에서 지워진 파일
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-broken.json'), '{ "format": 1, "kind": "bag" "items": [ }')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-empty.json'), '')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-v2.json'), '{ "format": 2, "kind": "bag", "items": [], "currencies": [] }')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-big.json'), '{ "format": 1, "kind": "bag", "name": "big", "items": [ { "id": 99999999999999, "count": "many", "type": "Props" } ] }')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-deep.json'), '{ "format": 1, "kind": "bag", "x": ' + ('[' * 200) + (']' * 200) + ', "items": [], "currencies": [] }')
      Press 'Tab3' | Out-Null
      Cmd 'ui set VaultQuery uicheck-' | Out-Null
      $unreadable = 0; for ($i = 0; $i -lt 9; $i++) { if ((Cmd "uitext Row$i" 100) -match '읽을 수 없음') { $unreadable++ } }
      Check 'broken, empty, other-format and too-deep files are listed as unreadable' ($unreadable -eq 4) "unreadable=$unreadable"
      Cmd 'ui set VaultQuery uicheck-big' | Out-Null
      Press 'VaultAll0' | Out-Null
      Check 'bad values in a readable file are skipped, not thrown' ((Msg) -match '건너뜀 1줄') (Msg)
      Cmd 'ui set VaultQuery uicheck-M' | Out-Null
      [IO.File]::Delete((Join-Path $dir 'uicheck-M.json'))
      Press 'VaultAll0' | Out-Null
      Check 'a file removed behind the list is reported' ((Msg) -match '파일이 없습니다') (Msg)

      # 6절 14번: 삭제는 두 번 눌러야 한다
      Cmd 'ui set VaultQuery uicheck-A' | Out-Null
      Press 'VaultDelete0' | Out-Null
      Check 'delete asks first' ((Cmd 'uitext VaultDelete0') -match "text='정말 삭제'")
      Press 'Tab1' | Out-Null; Press 'Tab3' | Out-Null
      Check 'another action keeps the files' ((Test-Path -LiteralPath $fileA) -and (Test-Path -LiteralPath (Join-Path $dir 'uicheck-A (2).json')))
      Cmd 'ui set VaultQuery uicheck-A' | Out-Null
      $before = @(Get-ChildItem -LiteralPath $dir -Filter 'uicheck-A*').Count
      Press 'VaultDelete0' | Out-Null; Press 'VaultDelete0' | Out-Null
      Check 'the second press deletes exactly one file' (@(Get-ChildItem -LiteralPath $dir -Filter 'uicheck-A*').Count -eq $before - 1) (Msg)
      Press 'VaultQueryClear' | Out-Null

      # 6절 16번: 변경 이력
      $hist = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Select-Object -Last 40) -join "`n"
      Check 'imports are in the change log' ($hist -match '가방 가져오기')

      # 6절 15번의 준비: 저장하고 다시 시작한 뒤에도 남는지
      [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), "$($single.id) $(Num "count $($single.id)" 'count=(\d+)')")
      Press 'Close' | Out-Null
      Cmd 'save' 5000 | Out-Null
      Check 'no exception in button callbacks' ((Traps) -eq $traps)
      Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    } finally { CleanVault }
  }
  'vault-restart' {
    $want = ([IO.File]::ReadAllText((Join-Path $work 'check-expect.txt'))) -split ' '
    Check 'imported items survive save and restart' ((Cmd "count $($want[0])") -match "count=$($want[1])$")
    Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null
    Check 'the vault opens with no test files left' ((Cmd 'ui set VaultQuery uicheck-') -ne $null -and (Cmd 'uitext VaultListRange') -match "text='0개'")
    Press 'VaultQueryClear' | Out-Null; Press 'Close' | Out-Null
  }
  'wrap' {
    # 05 문서 29절. 목록의 글은 말줄임 없이 줄바꿈되어 전부 보이고, 줄의 높이가 글에 맞춰 늘어난다. 화폐 탭의 명성.
    $traps = Traps
    function TextIn([string]$name) { $o = Cmd "uitext $name" 100; if ($o -match "(?s) text='(.*)'\s*$") { $Matches[1] } else { '' } }
    function Box([string]$name) {
      $o = Cmd "uipos $name" 100
      if ($o -notmatch ' at (-?\d+),(-?\d+) size=(\d+),(\d+)') { return $null }
      [pscustomobject]@{ top = [int]$Matches[2] - [int]$Matches[4] / 2; h = [int]$Matches[4] }
    }
    Cmd 'editor' | Out-Null

    # 화폐 그룹(05 문서 47절): 설명은 설명 칸에서 전부 보인다(정도의 기 10031의 설명은 44자를 넘는다)
    Press 'Tab1' | Out-Null; Press 'ResGroupCurrency' | Out-Null
    Press 'ItemName10031' | Out-Null
    $d = TextIn 'ItemDetailText'
    Check 'a currency description is whole' ($d.Length -gt 46 -and $d -notmatch '…') "len=$($d.Length) $d"

    # 화폐 탭의 명성: 추가량만큼 오르고 저장값(reputation)과 같다
    $fame0 = if ((Cmd 'fame' 100) -match 'saved=(-?\d+)') { [int]$Matches[1] } else { -999 }
    $fstep = if ((Cmd 'uitext CurrencyAddFame') -match "text='\+ (\d+)'") { [int]$Matches[1] } else { 10 }   # 지급 수량(AmountBar item)을 따른다
    Check 'the fame row shows the saved value' ((TextIn 'CurrencyOwnedFame') -eq "$fame0") "row=$(TextIn 'CurrencyOwnedFame') saved=$fame0"
    Press 'CurrencyAddFame' | Out-Null
    $o = Cmd 'fame' 100
    Check 'fame rises by the amount' ($o -match "saved=$($fame0 + $fstep) value=$($fame0 + $fstep)\b") $o
    Check 'the fame row is redrawn' ((TextIn 'CurrencyOwnedFame') -eq "$($fame0 + $fstep)")
    $hist = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Select-Object -Last 3) -join "`n"
    Check 'the fame change is in the change log' ($hist -match "명성\s+$fame0 -> $($fame0 + $fstep)") $hist

    # 자원 탭(38절의 덧붙임, 사용자 지시 2026-10-15): 표에는 설명 열이 없고 줄은 한 줄 높이로 쌓인다.
    # 설명은 줄을 누르면 아래 칸에 나오고, 긴 설명도 잘리지 않는다(칸 안에서 스크롤)
    Press 'Tab1' | Out-Null; Press 'ResGroupAll' | Out-Null; Press 'ResClear' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    $shown = if ($o -match '중 1~(\d+)번째') { [int]$Matches[1] } else { 0 }
    Check 'the item list shows rows' ($shown -ge 3) $o
    $tall = 0; $gaps = 0; $prev = $null
    for ($i = 0; $i -lt $shown; $i++) {
      $row = Box "Row$i"
      if ($prev -and [math]::Abs($row.top - ($prev.top + $prev.h)) -gt 1) { $gaps++ }
      if ($row.h -gt 34) { $tall++ }
      $prev = $row
    }
    Check 'the list has no description column' ((Cmd 'uidescs ItemDesc' 300) -match 'no element starting with ItemDesc') (Cmd 'uidescs ItemDesc' 300)
    Check 'rows are one line high' ($tall -eq 0) "tall=$tall of $shown"
    Check 'twelve rows fit on a screen' ($shown -ge 12) "shown=$shown"
    Check 'rows sit one under another' ($gaps -eq 0) "gaps=$gaps"
    Check 'the last shown row is inside the list' ((Box "Row$($shown - 1)") -and (IsAt (Pos 'ItemGive*').name))
    $r = Pos 'Row2'
    $clicked = Rc "click $($r.x) $($r.y)" 600
    Check 'pressing a row shows its text below' ($clicked -match 'Button\.onClick: Row2\s*$' -and (Cmd 'uitext ItemDetail') -match "ItemDetail text='\[\d+\]") "$clicked | $(Cmd 'uitext ItemDetail')"
    Cmd 'ui set ItemQuery 5081075' | Out-Null
    Press 'ItemName5081075' | Out-Null
    $fit = Cmd 'uifit ItemDetail' 200
    Check 'a long text is not cut in the box below' ($fit -match 'texts=[1-9]\d* overflow=0') $fit
    Shot 'wrap-detail'
    Press 'ItemQueryClear' | Out-Null

    # 이동: 다음은 다 보이지 않던 첫 줄에서 시작하고, 끝은 마지막 줄까지 간다
    $total = if ($o -match '전체 (\d+)개') { [int]$Matches[1] } else { 0 }
    Press 'ItemTableNext' | Out-Null
    Check 'next starts at the first row that was not whole' ((Cmd 'uitext ItemTableRange') -match "중 $($shown + 1)~")
    Press 'ItemTableLast' | Out-Null
    Check 'last reaches the end' ((Cmd 'uitext ItemTableRange') -match "~$($total)번째")
    $last = Box "Row$($total - 1)"; $view = Box 'ItemTable'
    Check 'the last row ends at the bottom of the list' ($last -and $view -and [math]::Abs(($last.top + $last.h) - ($view.top + $view.h)) -le 2) "row=$($last.top)+$($last.h) view=$($view.top)+$($view.h)"
    Press 'ItemTablePrev' | Out-Null; Press 'ItemTableFirst' | Out-Null
    Check 'first returns to the top' ((Cmd 'uitext ItemTableRange') -match '중 1~')

    # 다른 탭의 목록도 말줄임이 없다(기예, 운명 고르기, 기술 추가, 변경 이력)
    # 검색 칸은 앞선 검사(confirm)가 적은 글을 기억한다. 비우고 읽는다.
    Press 'Tab9' | Out-Null; Cmd 'ui set ArtQuery' | Out-Null
    $n = @((Cmd 'uidescs ArtDesc' 300) -split "`n" | Where-Object { $_ -match '^ArtDesc' })
    Check 'art book descriptions are whole' ($n.Count -ge 3 -and @($n | Where-Object { $_ -match '…' }).Count -eq 0) "rows=$($n.Count)"
    Press 'Tab8' | Out-Null; Press 'LuckAddOpen' | Out-Null; Cmd 'ui set LuckQuery' | Out-Null
    $n = @((Cmd 'uidescs LuckTips' 300) -split "`n" | Where-Object { $_ -match '^LuckTips' })
    Check 'luck effects are whole' ($n.Count -ge 3 -and @($n | Where-Object { $_ -match '…' }).Count -eq 0) "rows=$($n.Count)"
    Press 'Back' | Out-Null
    # 공법의 옵션: 글이 전부 들어 있고, 줄바꿈으로 높아진 옵션 줄이 아래의 장착 버튼과 겹치지 않는다
    Press 'Tab5' | Out-Null; Press 'PickMartial1' | Out-Null
    $bottom = 0; $cut = 0; $opts = 0
    for ($i = 0; $i -lt 6; $i++) {
      $b = Box "OptionText$i"
      if ($b) { $opts++; if ($b.top + $b.h -gt $bottom) { $bottom = $b.top + $b.h }; if ((TextIn "OptionText$i") -match '…') { $cut++ } }
    }
    $equip = Box 'MartialEquip'
    Check 'martial options are whole and above the equip button' ($opts -ge 1 -and $cut -eq 0 -and $equip -and $bottom -le $equip.top) "options=$opts cut=$cut bottom=$bottom equip=$($equip.top)"
    Shot 'wrap-martial'
    Press 'Tab5' | Out-Null; Press 'SkillAddOpen' | Out-Null; Cmd 'ui set SkillQuery' | Out-Null
    $n = @((Cmd 'uidescs SkillDesc' 300) -split "`n" | Where-Object { $_ -match '^SkillDesc' })
    Check 'skill descriptions are whole' ($n.Count -ge 3 -and @($n | Where-Object { $_ -match '…' }).Count -eq 0) "rows=$($n.Count)"
    Press 'Back' | Out-Null
    Press 'Tab14' | Out-Null
    $n = @((Cmd 'uidescs HistoryLine' 300) -split "`n" | Where-Object { $_ -match '^HistoryLine' })
    Check 'history lines are whole' ($n.Count -ge 1 -and @($n | Where-Object { $_ -match '…' }).Count -eq 0) "rows=$($n.Count)"
    Shot 'wrap-history'
    Press 'Tab1' | Out-Null; Shot 'wrap-items'
    Press 'Tab1' | Out-Null; Press 'ResGroupCurrency' | Out-Null; Shot 'wrap-currency'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'charvault' {
    # 묶음 D(docs/superpowers/specs/2026-10-13-character-export-design.md 7절). 내보내기 폴더는 세이브 밖이라 회차가 되돌리지 않는다.
    # 이 검사가 만든 파일(uicheck-로 시작)은 끝날 때와 멈췄을 때 지운다.
    $traps = Traps
    $dir = Join-Path $data 'exports\character'
    $bagDir = Join-Path $data 'exports\bag'
    function CleanChar { foreach ($d in $dir, $bagDir) { if (Test-Path -LiteralPath $d) { Get-ChildItem -LiteralPath $d -Filter 'uicheck-*' | ForEach-Object { [IO.File]::Delete($_.FullName) } } } }
    function Msg { if ((Cmd 'uistate' 100) -match "message='(.*)'\s*$") { $Matches[1] } else { '' } }
    function State { $o = Cmd 'cstate' 200; $h = @{ raw = $o }; foreach ($m in [regex]::Matches($o, '(\w+)=(\S+)')) { $h[$m.Groups[1].Value] = $m.Groups[2].Value }; $h }
    function Has([string]$cmd, [string]$pattern) { (Cmd $cmd 200) -match $pattern }
    CleanChar
    try {
      Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null; Press 'VaultModeChar' | Out-Null
      $s0 = State

      # 7절 1~2번: 내보내기와 파일
      Cmd 'ui set CharName' | Out-Null   # 이름 칸은 앞서 적은 것을 기억한다. 비우고 시작한다
      Press 'CharExport' | Out-Null
      Check 'an empty name is refused' ((Msg) -match '이름을')
      Cmd 'ui set CharName uicheck-C' | Out-Null; Press 'CharExport' | Out-Null
      $fileC = Join-Path $dir 'uicheck-C.json'
      Check 'export writes a file' ((Test-Path -LiteralPath $fileC) -and (Msg) -match '내보냈습니다') (Msg)
      $pack = Get-Content -LiteralPath $fileC -Raw -Encoding UTF8 | ConvertFrom-Json
      Check 'the file names its kind' ($pack.format -eq 1 -and $pack.kind -eq 'character' -and $pack.character.Length -gt 0)
      Check 'the grade is in the file' ("$($pack.grade.id)" -eq $s0.grade -and "$($pack.grade.exp)" -eq $s0.exp) "file=$($pack.grade.id)/$($pack.grade.exp) game=$($s0.grade)/$($s0.exp)"
      Check 'every attribute is in the file' ("$(@($pack.attributes).Count)" -eq $s0.attrs -and @($pack.attributes | Where-Object { $_.key -eq 'attack' -and "$($_.value)" -eq $s0.attack }).Count -eq 1) "file=$(@($pack.attributes).Count) game=$($s0.attrs)"
      Check 'attribute keys are unique' (@($pack.attributes | ForEach-Object { $_.key } | Sort-Object -Unique).Count -eq @($pack.attributes).Count)
      Check 'every learned martial is in the file' ("$(@($pack.martials).Count)" -eq $s0.martials -and @($pack.martials | Where-Object { @($_.values).Count -lt 3 }).Count -eq 0) "file=$(@($pack.martials).Count) game=$($s0.martials)"
      Check 'learned arts are in the file' ("$(@($pack.arts).Count)" -eq $s0.arts) "file=$(@($pack.arts).Count) game=$($s0.arts)"
      Check 'born lucks are in the file' ("$(@($pack.bornLucks).Count)" -eq $s0.born)
      Check 'progress markers and timed lucks are left out' (@($pack.addLucks).Count -le [int]$s0.add)
      Cmd 'ui set CharName uicheck-C' | Out-Null; Press 'CharExport' | Out-Null
      Check 'the same name gets a number' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-C (2).json'))
      Cmd 'ui set CharName uicheck-b:c*d?' | Out-Null; Press 'CharExport' | Out-Null
      Check 'characters a file name cannot hold become underscores' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))
      Cmd 'ui set CharQuery uicheck-' | Out-Null
      Cmd 'ui set CharQuery UICHECK-C' | Out-Null
      Check 'the search ignores case' ((Cmd 'uitext CharListRange') -match '전체 2개') (Cmd 'uitext CharListRange')
      Cmd 'ui set CharQuery uicheck-' | Out-Null
      Check 'the list shows three exports' ((Cmd 'uitext CharListRange') -match '전체 3개')

      # 7절 3번, Review Focus 4: 가방 목록과 섞이지 않는다
      Press 'VaultModeBag' | Out-Null
      Cmd 'ui set VaultName uicheck-bagside' | Out-Null; Press 'VaultExport' | Out-Null
      Cmd 'ui set VaultQuery uicheck-' | Out-Null
      Check 'the bag list shows only the bag export' ((Cmd 'uitext VaultListRange') -match '전체 1개' -and (Cmd 'uitext Row0') -match 'uicheck-bagside')
      Press 'VaultQueryClear' | Out-Null
      Press 'VaultModeChar' | Out-Null
      Check 'lists do not mix' ((Cmd 'uitext CharListRange') -match '전체 3개' -and (IsAt 'CharExport') -and -not (IsAt 'VaultExport'))
      Check 'the mode is kept when the tab is left' ((Press 'Tab1') -and (Press 'Tab3') -and (IsAt 'CharExport'))

      # 7절 16번의 삭제
      Cmd 'ui set CharQuery uicheck-b' | Out-Null
      Press 'CharDelete0' | Out-Null
      Check 'delete asks first' ((Cmd 'uitext CharDelete0') -match "text='정말 삭제'" -and (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json')))
      Press 'CharDelete0' | Out-Null
      Check 'the second press deletes the file' (-not (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))) (Msg)
      Press 'CharQueryClear' | Out-Null
      # ---- 가져오기 (7절 4~16번) ----
      function Row([string]$needle) { Cmd "ui set CharItemQuery $needle" | Out-Null }
      function IndexOfMartial($list, $one) { for ($i = 0; $i -lt @($list).Count; $i++) { if ([object]::ReferenceEquals(@($list)[$i], $one)) { return $i } }; -1 }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      # 줄 0은 "uicheck-C (2)"(나중에 쓴 것), 줄 1이 uicheck-C다. 내용은 같다.
      Press 'CharOpen0' | Out-Null
      Check 'the detail screen opens on the grade' ((IsAt 'Back') -and (IsAt 'CharGetGrade'))

      # 4~5번: 능력치
      Press 'CharSecAttributes' | Out-Null
      $atk = [int]$s0.attack; $def = [int]$s0.defense
      Cmd "cattr attack $($atk + 5)" | Out-Null; Press 'CharSecAttributes' | Out-Null; Row 'attack'
      Press 'CharGetAttr_attack' | Out-Null
      Check 'one attribute returns to the file value' ((State).attack -eq "$atk") (Msg)
      Check 'an attribute that matches cannot be imported again' (-not (IsAt 'CharGetAttr_attack') -or (Cmd 'uitext CharGetAttr_attack') -match '같음')
      Press 'CharItemQueryClear' | Out-Null
      Cmd "cattr attack $($atk + 5)" | Out-Null; Cmd "cattr defense $($def + 5)" | Out-Null
      Press 'CharSecAttributes' | Out-Null; Press 'CharImportSection' | Out-Null; Press 'CharImportSection' | Out-Null
      $s = State; $m = Msg
      Check 'the attribute section restores every changed value' ($s.attack -eq "$atk" -and $s.defense -eq "$def" -and $m -match '능력치: 변경 2') $m

      # 6번: 경지
      Cmd 'setgrade 5' | Out-Null
      Press 'CharSecGrade' | Out-Null; Press 'CharGetGrade' | Out-Null
      Check 'the grade import asks first' ((State).grade -eq '5' -and (Msg) -match '능력치는 따라오지')
      Press 'CharGetGrade' | Out-Null
      $s = State
      Check 'the second press writes the grade and its exp' ($s.grade -eq $s0.grade -and $s.exp -eq $s0.exp) $s.raw

      # 7~9번: 공법
      $mar = @($pack.martials | Where-Object { -not $_.equipped -and @($_.values).Count -gt 6 })[0]
      $mi = IndexOfMartial $pack.martials $mar
      $want = "values=\[$(@($mar.values) -join ',')\]"
      $o = Cmd "cmartial $($mar.baseId)" 200
      $sole = if ($o -match 'sole=(\S+)') { $Matches[1] } else { '' }
      Cmd "mdel $sole" | Out-Null
      Press 'CharSecMartials' | Out-Null; Row "$($mar.baseId)"
      Press "CharGetMartial$mi" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200
      Check 'a forgotten martial comes back with the same options' ($o -match '^.*n=1 ' -and $o -match $want -and (State).martials -eq $s0.martials) "$o | $(Msg)"
      Press 'CharItemQueryClear' | Out-Null
      Press 'CharImportSection' | Out-Null
      Check 'the martial section adds nothing that is already known' ((State).martials -eq $s0.martials -and (Msg) -match '공법: 추가 0') (Msg)
      Row "$($mar.baseId)"
      Press "CharGetMartial$mi" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200
      Check 'the row button adds one more of a known martial' ($o -match 'n=2 ') $o
      $extra = @([regex]::Matches($o, 'sole=(\S+)') | ForEach-Object { $_.Groups[1].Value })[-1]
      Cmd "mdel $extra" | Out-Null
      Press 'CharItemQueryClear' | Out-Null

      # 10번: 기예
      $art = @($pack.arts)[0]
      if ($art) {
        Cmd "cartdel $($art.id)" | Out-Null
        Check 'the art was forgotten for the test' ((State).arts -eq "$([int]$s0.arts - 1)")
        Press 'CharSecArts' | Out-Null; Row "$($art.id)"
        Press 'CharGetArt0' | Out-Null
        Check 'a forgotten art book comes back' ((State).arts -eq $s0.arts) (Msg)
        Press 'CharItemQueryClear' | Out-Null
      } else { 'INFO no learned art in this save; art import not exercised' }

      # 11번: 운명
      Press 'CharSecLucks' | Out-Null
      $bornId = @($pack.bornLucks)[0].id
      Cmd "cborndel $bornId" | Out-Null
      Check 'the born luck was removed for the test' (-not (Has 'luck' "born=\[[^\]]*\b$bornId\b"))
      Press 'CharSecLucks' | Out-Null; Row "$bornId"
      Press 'CharGetBorn0' | Out-Null
      Check 'a removed born luck comes back as a born luck' (Has 'luck' "born=\[[^\]]*\b$bornId\b") (Msg)
      Press 'CharItemQueryClear' | Out-Null
      $add = @($pack.addLucks)[0]
      if ($add) {
        Cmd "luckdel $($add.id)" | Out-Null
        Press 'CharSecLucks' | Out-Null; Row "$($add.id)"
        Press 'CharGetAdd0' | Out-Null
        Check 'a removed acquired luck comes back' (Has 'luck' "add=\[[^\]]*\b$($add.id)\(") (Msg)
        Press 'CharItemQueryClear' | Out-Null
      } else { 'INFO no exportable acquired luck in this save; not exercised' }

      # 12번: 역천개명
      $fate = @($pack.fates)[0]
      if ($fate) {
        $o = Cmd "cfatealt $($fate.grade)" 300
        Check 'the fate was changed for the test' ((State).fates -notmatch "$($fate.grade):$($fate.id)") $o
        # 한 경지의 역천개명을 바꿔도 다른 경지의 기록은 그대로여야 한다(게임의 추가 행동이 지금 경지의 기록을 덮어쓴다)
        $now = (State).fates; $kept = @($s0.fates -split ',' | Where-Object { $_ -notmatch "^$($fate.grade):" } | Where-Object { ",$now," -notmatch ",$_," })
        Check 'changing one grade leaves the other grades alone' ($kept.Count -eq 0) "before=$($s0.fates) now=$now"
        Press 'CharSecFates' | Out-Null
        Press 'CharGetFate0' | Out-Null
        Check 'the fate returns to the one in the file' ((State).fates -match "$($fate.grade):$($fate.id)") (Msg)
        Cmd 'setgrade 1' | Out-Null
        Press 'CharSecFates' | Out-Null
        Check 'a fate above the current grade is shown as out of reach' ((Cmd 'uidescs CharFateState' 200) -match '경지 미달')
        Press 'CharSecGrade' | Out-Null; Press 'CharGetGrade' | Out-Null; Press 'CharGetGrade' | Out-Null
      } else { 'INFO no fate recorded in this save; fate import not exercised' }

      # Review Focus 5: 세부 화면을 연 채 떠나면 닫힌다
      Press 'Tab1' | Out-Null; Press 'Tab3' | Out-Null
      Check 'leaving closes the detail screen' ((IsAt 'CharExport') -and -not (IsAt 'CharGetGrade'))

      # 13번, Review Focus 2: 여러 가지를 바꾼 뒤 모두 가져오기
      Cmd 'setgrade 5' | Out-Null; Cmd "cattr attack $($atk + 5)" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200; if ($o -match 'sole=(\S+)') { Cmd "mdel $($Matches[1])" | Out-Null }
      if ($art) { Cmd "cartdel $($art.id)" | Out-Null }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      Press 'CharAll0' | Out-Null
      Check 'import-all asks first' ((State).grade -eq '5' -and (Cmd 'uitext CharAll0') -match "text='정말 가져오기'")
      Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'import-all restores the character' ($s.grade -eq $s0.grade -and $s.exp -eq $s0.exp -and $s.attack -eq $s0.attack -and $s.martials -eq $s0.martials -and $s.arts -eq $s0.arts -and $s.born -eq $s0.born -and $s.fates -eq $s0.fates) "$($s.raw) | $m"
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'importing twice adds nothing' ($s.martials -eq $s0.martials -and $s.arts -eq $s0.arts -and $s.born -eq $s0.born -and $m -match '공법: 추가 0' -and $m -match '기예: 추가 0') $m

      # 14~15번, Review Focus 1·3·4: 손으로 고친 파일
      $bad = [ordered]@{ format = 1; kind = 'character'; name = 'uicheck-H'; exportedAt = '2026-10-13 00:00:00'; character = 'test'
        grade = [ordered]@{ id = 9999; name = 'no such grade'; exp = 1 }
        attributes = @([ordered]@{ key = 'nope'; name = 'unknown'; value = 1 }, [ordered]@{ key = 'attack'; name = 'too big'; value = 999999999 }, [ordered]@{ key = 'defense'; name = 'text'; value = 'many' })
        martials = @([ordered]@{ type = $mar.type; baseId = $mar.baseId; name = 'short'; grade = 1; exp = 0; equipped = $false; values = @(2) },
          [ordered]@{ type = 2; baseId = 2; name = 'nonsense'; grade = 0; exp = 0; equipped = $false; values = @(2, 2, 0, 1, 2, 3) },
          [ordered]@{ type = $mar.type; baseId = 987654321; name = 'unknown skill'; grade = 1; exp = 0; equipped = $false; values = @($mar.type, 987654321, 1) })
        arts = @([ordered]@{ kind = '연단'; id = 999999999; name = 'no book' }, [ordered]@{ kind = '없는 분류'; id = 1; name = 'no kind' })
        bornLucks = @([ordered]@{ id = 999999999; name = 'no luck' }); addLucks = @([ordered]@{ id = 999999999; name = 'no luck' })
        fates = @([ordered]@{ grade = 2; id = 999999999; name = 'no fate' }) }
      $capped = [ordered]@{ format = 1; kind = 'character'; name = 'uicheck-X'; exportedAt = '2026-10-13 00:00:01'; character = 'test'
        grade = [ordered]@{ id = [int]$s0.grade; name = 'same grade'; exp = 999999999 } }
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-H.json'), ($bad | ConvertTo-Json -Depth 6))
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-X.json'), ($capped | ConvertTo-Json -Depth 6))
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-broken.json'), '{ "format": 1, "kind": "character" "grade": ')
      Copy-Item -LiteralPath (Join-Path $bagDir 'uicheck-bagside.json') -Destination (Join-Path $dir 'uicheck-bagfile.json')
      Press 'Tab3' | Out-Null
      Cmd 'ui set CharQuery uicheck-H' | Out-Null
      $before = State
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'bad lines are refused one by one' ($s.grade -eq $before.grade -and $s.attack -eq $before.attack -and $s.defense -eq $before.defense -and $m -match '못 넣음') $m
      Check 'the result says why lines were refused' ($m -match '모르는 능력치' -and $m -match '게임에 없는') $m
      $last = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ -match '가져오기 전부\(uicheck-H\)' })[-1]
      Check 'a failed import is marked in the change log' ($last -match '재조회 불일치') "$last"
      Check 'a broken martial line leaves nothing behind' ($s.martials -eq $before.martials -and (Cmd 'cmartial 2' 200) -match 'n=0' -and (Cmd 'cmartial 987654321' 200) -match 'n=0') $s.raw
      Cmd 'ui set CharQuery uicheck-X' | Out-Null
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State
      Check 'exp is capped at the next step' ($s.grade -eq $s0.grade -and [int]$s.exp -le [int]$s.nextexp -and [int]$s.exp -ge [int]$s0.exp) $s.raw
      Cmd 'ui set CharQuery uicheck-b' | Out-Null
      $unreadable = 0; for ($i = 0; $i -lt 3; $i++) { if ((Cmd "uitext Row$i" 100) -match '읽을 수 없음') { $unreadable++ } }
      Check 'a bag file in the character folder is unreadable' ($unreadable -eq 2 -and (Cmd 'uipos CharAll0') -match 'no element named') "unreadable=$unreadable"

      # 16번: 목록을 본 뒤 지워진 파일
      Cmd 'ui set CharQuery uicheck-X' | Out-Null
      [IO.File]::Delete((Join-Path $dir 'uicheck-X.json'))
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      Check 'a file removed behind the list is reported' ((Msg) -match '파일이 없습니다') (Msg)
      Press 'CharQueryClear' | Out-Null

      # 18번: 변경 이력
      $hist = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Select-Object -Last 120) -join "`n"
      Check 'imports are in the change log' ($hist -match '캐릭터 가져오기')

      # 17번의 준비: 경지와 능력치와 공법을 바꾼 채 가져오고 저장한다. 다시 시작한 뒤에도 파일의 상태여야 한다
      Cmd 'setgrade 5' | Out-Null; Cmd "cattr attack $($atk + 5)" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200; if ($o -match 'sole=(\S+)') { Cmd "mdel $($Matches[1])" | Out-Null }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State
      Check 'the last import restores the exp too' ($s.exp -eq $s0.exp -and $s.grade -eq $s0.grade) $s.raw
      [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), "$($s.grade) $($s.exp) $($s.attack) $($s.martials) $($mar.baseId) $(@($mar.values) -join ',')")
      Press 'CharQueryClear' | Out-Null
      Shot 'charvault-list'
      Cmd 'ui set CharQuery uicheck-C' | Out-Null; Press 'CharOpen0' | Out-Null; Press 'CharSecMartials' | Out-Null; Shot 'charvault-martials'
      Press 'CharSecAttributes' | Out-Null; Shot 'charvault-attributes'
      Press 'Back' | Out-Null; Press 'CharQueryClear' | Out-Null
      Cmd 'uiopen PlayerInfo' 1500 | Out-Null; Shot 'charvault-playerinfo'; Cmd 'uiclose PlayerInfo' 1000 | Out-Null
      Cmd 'save' 5000 | Out-Null

      Press 'VaultModeBag' | Out-Null   # 다른 검사가 가방 쪽에서 시작하도록 돌려 둔다
      Press 'Close' | Out-Null
      Check 'no exception in button callbacks' ((Traps) -eq $traps)
      Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    } finally { CleanChar }
  }
  'charvault-restart' {
    $want = ([IO.File]::ReadAllText((Join-Path $work 'check-expect.txt'))) -split ' '
    $o = Cmd 'cstate' 200
    Check 'the imported grade, exp and attribute survive save and restart' ($o -match "grade=$($want[0]) exp=$($want[1]) " -and $o -match " attack=$($want[2]) " -and $o -match " martials=$($want[3]) ") $o
    $o = Cmd "cmartial $($want[4])" 200
    Check 'the imported martial survives with its options' ($o -match 'n=1 ' -and $o -match "values=\[$($want[5])\]") $o
    Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null; Press 'VaultModeChar' | Out-Null
    Cmd 'ui set CharQuery uicheck-' | Out-Null
    Check 'no test files are left' ((Cmd 'uitext CharListRange') -match "text='0개'")
    Press 'CharQueryClear' | Out-Null; Press 'Close' | Out-Null
  }
  'fateguard' {
    # 05 문서 31절. 역천개명 분류의 운명을 "운명 추가"로 붙여도 경지별 역천개명 기록은 그대로여야 한다.
    # 게임의 추가 행동이 그 번호를 지금 대경지의 기록에 덮어쓴다(30절의 실측).
    $before = if ((Cmd 'cstate' 200) -match 'fates=(\S+)') { $Matches[1] } else { '' }
    if ($before -eq 'none' -or $before -eq '') { 'INFO no fate record in this save; not exercised' }
    else {
      $o = Cmd 'cluck3' 400
      $after = if ((Cmd 'cstate' 200) -match 'fates=(\S+)') { $Matches[1] } else { '' }
      Check 'the luck was added' ($o -match 'has=True\s*$') $o
      Check 'adding a fate-class luck leaves the fate records alone' ($after -eq $before) "before=$before after=$after"
    }
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'order' {
    $traps = Traps
    Cmd 'editor' | Out-Null
    $s = Cmd 'uistate'
    Check 'opens in camera mode just above the map' ($s -match 'mode=ScreenSpaceCamera order=10 covered=False top=0 ') $s

    # 스펙 6절 2번: 나중에 연 게임 화면이 위에 뜨고, Insert가 편집기를 맨 앞으로 가져온다
    GameScreen open
    $s = Cmd 'uistate'
    Check 'a game screen opened later covers the editor' ($s -match 'covered=True top=20 ' -and -not (IsAt 'Tab0')) $s
    Key 0x2D
    $s = Cmd 'uistate'
    Check 'Insert brings a covered editor to the front' ($s -match 'open=True .*order=30 covered=False ' -and (IsAt 'Tab0')) $s

    # 스펙 6절 3번: 아래의 게임 화면이 닫히면 정렬을 낮춘다
    GameScreen close
    $s = Cmd 'uistate'
    Check 'order drops when the screen below closes' ($s -match 'order=10 covered=False top=0 ') $s
    GameScreen open
    Check 'the reopened screen is above the editor again' ((Cmd 'uistate') -match 'covered=True ' -and -not (IsAt 'Tab0'))
    GameScreen close
    Cmd 'uitoggle' | Out-Null   # 단축키와 같은 동작. 실제 키는 위에서 한 번 보냈다
    Check 'Insert closes an editor that is in front' ((Cmd 'uistate') -match 'open=False ')

    # 스펙 6절 4번: 확인 창은 더 높은 층이라 항상 위다. 가려진 것으로 치지 않는다
    Cmd 'editor' | Out-Null
    Cmd 'uiopen CheckPopup' 1200 | Out-Null
    Check 'confirm popup is above the editor' ((Rc 'hit 1037 531' 300) -match 'CheckPopup' -and -not (IsAt 'Tab0') -and (Cmd 'uistate') -match 'covered=False ')
    Cmd 'uiclose CheckPopup' 1000 | Out-Null
    Check 'editor is usable again' (IsAt 'Tab0')

    # Review Focus 4: Insert를 빠르게 여러 번, 가려진 상태와 섞어서
    1..6 | ForEach-Object { Cmd 'uitoggle' 100 | Out-Null }
    Check 'six quick toggles end where they started' ((Cmd 'uistate') -match 'open=True ')
    GameScreen open
    1..3 | ForEach-Object { Cmd 'uitoggle' 100 | Out-Null }   # 맨 앞으로 → 닫기 → 열기
    $s = Cmd 'uistate'
    Check 'toggles mixed with a covering screen stay consistent' ($s -match 'open=True .*order=30 covered=False ') $s
    GameScreen close
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'move' {
    # 창 위치는 세이브 폴더 밖의 config.txt에 남는다(회차를 되돌려도 남는다). 시험 전의 파일을 떠 두고 move-corrupt에서 되돌린다.
    $traps = Traps
    KeepConfig
    Cmd 'editor' | Out-Null
    $o = DragWindowTo 60 60
    Check 'the title bar takes the drag' ($o -match 'handler=TitleBar ') $o
    $w = WinState
    Check 'window is at the base position' ($w.x -eq 60 -and $w.y -eq 60) $w.raw
    $t = Pos 'Tab0'

    # 스펙 6절 5번: 끌면 따라 움직이고, 옮긴 자리에서 버튼이 눌린다
    DragWindowTo 260 160 | Out-Null
    $w = WinState; $t2 = Pos 'Tab0'
    Check 'window follows the drag' ($w.x -eq 260 -and $w.y -eq 160) $w.raw
    Check 'everything inside moved with it' ($t2.x -eq $t.x + 200 -and $t2.y -eq $t.y + 100 -and (IsAt 'Tab0')) "tab0 $($t.x),$($t.y) -> $($t2.x),$($t2.y)"
    Press 'Tab1' | Out-Null
    Check 'buttons work at the new place' ((Cmd 'uistate') -match 'tab=1 ')
    Check 'position is written to config.txt' ([bool]((Get-Content -LiteralPath (Join-Path $data 'config.txt') -Encoding UTF8) -match '^window=260,160$'))
    Check 'other config lines are kept' ([bool]((Get-Content -LiteralPath (Join-Path $data 'config.txt') -Encoding UTF8) -match '^hotkey='))

    # 화면 밖으로는 나가지 않는다
    DragWindowTo 5000 5000 | Out-Null
    $w = WinState
    Check 'stops at the right and bottom limits' ($w.x -eq 1720 -and $w.y -eq 1028) $w.raw
    DragWindowTo -5000 -5000 | Out-Null
    $w = WinState
    Check 'stops at the left and top limits' ($w.x -eq -1300 -and $w.y -eq 0) $w.raw
    DragWindowTo 260 160 | Out-Null

    # 스펙 6절 6번: 접기와 펼침
    Press 'Tab0' | Out-Null
    $t = Pos 'Tab0'
    Press 'Collapse' | Out-Null
    $w = WinState
    Check 'collapsed' ($w.collapsed -and (Cmd 'uitext Collapse') -match "text='펼침'") $w.raw
    Check 'the folded area lets clicks through to the game' ((Rc "hit $($t.x) $($t.y)" 200) -notmatch 'TlToyboxEditor')
    Check 'the title bar is still there' (IsAt 'Close')

    # Review Focus 3: 접힌 상태에서 끌기, 접은 채 닫았다가 다시 열기
    DragWindowTo 300 200 | Out-Null
    $w = WinState
    Check 'a collapsed window can be dragged' ($w.x -eq 300 -and $w.y -eq 200 -and $w.collapsed) $w.raw
    Press 'Collapse' | Out-Null
    $w = WinState; $t2 = Pos 'Tab0'
    Check 'expands again where it was dragged, on the same tab' (-not $w.collapsed -and $w.raw -match ' tab=0 ' -and $t2.x -eq $t.x + 40 -and $t2.y -eq $t.y + 40 -and (IsAt 'Tab0')) $w.raw
    Press 'Collapse' | Out-Null
    Press 'Close' | Out-Null
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'reopening shows it expanded at the same place' (-not $w.collapsed -and $w.x -eq 300 -and $w.y -eq 200 -and (IsAt 'Tab0')) $w.raw

    DragWindowTo 260 160 | Out-Null   # move-restart가 확인할 위치
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'move-restart' {
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'position survives a restart' ($w.x -eq 260 -and $w.y -eq 160) $w.raw
    Press 'Close' | Out-Null
    # Review Focus 1: 깨진 값과 화면 밖의 값으로 다시 시작할 준비
    $lines = @(Get-Content -LiteralPath (Join-Path $work 'check-config.txt') -Encoding UTF8 | Where-Object { $_ -notmatch '^window=' })
    [IO.File]::WriteAllLines((Join-Path $data 'config.txt'), [string[]]($lines + 'window=abc' + 'window=99999,-99999' + 'window=7'))
    'corrupt config written'
  }
  'move-corrupt' {
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'unreadable lines are skipped and an off-screen value is pulled inside' ($w.x -eq 1720 -and $w.y -eq 0) $w.raw
    # 이 자리에서는 제목 줄의 왼쪽 200만 화면 안에 있다. 그 부분이 잡히는지 본다.
    Check 'the visible part of the title bar can be grabbed' ((Rc 'hit 1870 26' 200) -match '/TitleBar\s*$')
    DragWindowTo 60 60 | Out-Null
    Check 'and dragged back' ((WinState).x -eq 60 -and (IsAt 'Close'))
    Press 'Close' | Out-Null
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    RestoreConfig
  }
  'rates' {
    # 배율 탭: 수련치와 이동. 스펙 2026-10-13-rate-tab-design.md 5절.
    # 깨끗한 회차에서 시작한다(패치가 하나도 걸려 있지 않은 것을 본다).
    $traps = Traps
    function RateState { Cmd 'rstate' 200 }
    function MoveDay { if ((Cmd 'wdate' 200) -match 'moveDay=([\d.]+)') { [double]$Matches[1] } else { throw 'wdate 출력을 읽지 못했습니다' } }
    function WorldDayNow { if ((Cmd 'wdate' 200) -match 'worldDay=(\d+)') { [int]$Matches[1] } else { throw 'wdate 출력을 읽지 못했습니다' } }
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab12' | Out-Null
    foreach ($b in 'RateExp', 'RateMartial', 'RateMove', 'RateActionDay', 'RateSpeed', 'RateArtifactCooldown', 'RateMpCost', 'RateSpCost', 'Multiplier', 'God', 'RateReset') { Check "$b is drawn" (IsAt $b) }
    $s = RateState
    Check 'nothing is patched at first' ($s -match 'exp=1 martial=1 move=1 actionDay=1 speed=1 ' -and $s -notmatch 'patched') $s

    # 이동: x1의 한 칸을 재고, x0.5에서 절반인지 본다.
    $d0 = MoveDay; Cmd 'wmove 1 0' 900 | Out-Null; $plain = (MoveDay) - $d0
    Check 'a plain step costs something' ($plain -gt 0) "cost=$plain"
    Press 'RateMove' | Out-Null
    Check 'move shows x0.5' ((Cmd 'uitext RateMove') -match "text='x0.5'")
    $s = RateState
    Check 'only move is patched' ($s -match 'move=0.5\(patched\)' -and $s -match 'exp=1 martial=1 ') $s
    $d0 = MoveDay; Cmd 'wmove -1 0' 900 | Out-Null; $half = (MoveDay) - $d0
    $s = RateState
    Check 'a half step is recorded' ($s -match 'lastMove=([\d.]+)->([\d.]+)' -and [math]::Abs([double]$Matches[2] - [double]$Matches[1] * 0.5) -lt 0.0001) $s
    Check 'the half step costs half' ([math]::Abs($half - [double]$Matches[2]) -lt 0.0001) "half=$half"
    Start-Sleep -Milliseconds 900
    Check 'move is on the overlay' ((Cmd 'uistate') -match 'status=\[이동 x0.5\] ')

    # x0: 여러 칸을 걸어도 날짜와 쌓인 값이 그대로다. 걸으면서 수련할 수 있는 칸을 찾는다.
    1..3 | ForEach-Object { Press 'RateMove' | Out-Null }
    Check 'move shows x0' ((Cmd 'uitext RateMove') -match "text='x0'")
    Cmd 'setgrade 8' | Out-Null
    Press 'RateExp' | Out-Null; Press 'RateExp' | Out-Null
    Check 'exp shows x5' ((Cmd 'uitext RateExp') -match "text='x5'")
    $day0 = WorldDayNow; $d0 = MoveDay; $trained = $false
    for ($i = 0; $i -lt 10 -and -not $trained; $i++) {
      $o = Cmd 'wexp' 1500
      if ($o -match 'IsCreateAction=0 day=\d+ CreateAction=0 exp (\d+) -> (\d+)') {
        $trained = $true; $gain = [int]$Matches[2] - [int]$Matches[1]
        $s = RateState
        Check 'training is scaled by 5' ($s -match 'lastExp=(\d+)->(\d+)' -and [int]$Matches[2] -eq [int]$Matches[1] * 5) $s
        Check 'the scaled amount is what was gained' ($gain -eq [int]$Matches[2]) "gain=$gain"
      } elseif ((Cmd 'wmove 1 0' 900) -notmatch 'IsCreateAction=0 CreateAction=0') { break }
      else { Check "free step $i keeps the date" ((WorldDayNow) -eq $day0 -and [math]::Abs((MoveDay) - $d0) -lt 0.0001) }
    }
    Check 'a trainable tile was found' $trained

    # 플레이어가 아닌 단위와 음수.
    $o = Cmd 'rnpc' 300
    Check 'an NPC gains the plain amount' ($o -match 'npc exp (\d+) -> (\d+)' -and [int]$Matches[2] - [int]$Matches[1] -eq 10) $o
    $o = Cmd 'rneg' 300
    Check 'a negative amount is not scaled' ($o -match 'player AddExp\(-7\) exp (\d+) -> (\d+)' -and [int]$Matches[1] - [int]$Matches[2] -eq 7) $o

    # 날짜 고정과 함께.
    Check 'no freeze note while not frozen' ((Cmd 'uitext RateNoteMove') -notmatch '날짜 고정 중')
    Cmd 'tfreeze on' | Out-Null; Press 'Tab12' | Out-Null
    Check 'the freeze note is shown' ((Cmd 'uitext RateNoteMove') -match '날짜 고정 중')
    Cmd 'tfreeze off' | Out-Null; Press 'Tab12' | Out-Null

    # 한 바퀴와 되돌리기, 표시와 이력.
    Start-Sleep -Milliseconds 900
    Check 'both are on the overlay' ((Cmd 'uistate') -match 'status=\[수련 x5 · 이동 x0\] ')
    Press 'RateMove' | Out-Null
    Check 'move wraps to x1' ((Cmd 'uitext RateMove') -match "text='x1'")
    Press 'God' | Out-Null; Press 'Multiplier' | Out-Null
    Press 'RateReset' | Out-Null
    $s = RateState
    Check 'reset puts everything back' ($s -match 'exp=1\(patched\) martial=1 move=1\(patched\) actionDay=1 speed=1 ' -and $s -match 'god=False damage=1') $s
    Start-Sleep -Milliseconds 900
    Check 'overlay is hidden after reset' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    $d0 = MoveDay; Cmd 'wmove -1 0' 900 | Out-Null
    Check 'steps cost again after reset' ((MoveDay) -ne $d0 -or (WorldDayNow) -gt $day0)
    Press 'Tab14' | Out-Null
    $lines = (Cmd 'uidescs HistoryLine' 300)
    Check 'changes are in the history' ($lines -match '배율' -and $lines -match '수련치 획득')
    Shot 'rates'
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'rates-battle' {
    # 배율 탭: 전투 중 공법 숙련과 전투 속도. 전투에 들어갔다가 끝내고 지도로 나온다.
    $traps = Traps
    function StepExp { if ((Cmd 'rmart' 200) -match 'step=(-?\d+)') { [int]$Matches[1] } else { throw 'rmart 출력을 읽지 못했습니다' } }
    function Scale { if ((Cmd 'rstate' 200) -match 'timeScale=([\d.]+)') { [double]$Matches[1] } else { throw 'rstate 출력을 읽지 못했습니다' } }
    Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null
    Check 'speed shows x2' ((Cmd 'uitext RateSpeed') -match "text='x2'")
    Start-Sleep -Milliseconds 500
    Check 'speed is not applied outside battle' ((Scale) -eq 1) (Cmd 'rstate')
    Press 'Close' | Out-Null
    EnterBattle
    Check 'the battle scene is entered' ((Cmd 'uilist' 100) -match 'BattleInfo')
    Start-Sleep -Milliseconds 800
    Check 'speed is applied in battle' ((Scale) -eq 2) (Cmd 'rstate')
    Start-Sleep -Seconds 3
    Check 'speed holds after a few seconds' ((Scale) -eq 2) (Cmd 'rstate')

    # 숙련: 신법을 x1로 한 번, x5로 한 번 써서 오른 양을 견준다. 인자는 rstate의 lastMartial로 본다.
    $e0 = StepExp; Cmd 'bcast 3' 2500 | Out-Null; $plain = (StepExp) - $e0
    Check 'a plain use raises the mastery' ($plain -gt 0) "gain=$plain"
    Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null
    Check 'rate tab is drawn in battle' (IsAt 'RateMartial')
    Press 'RateMartial' | Out-Null; Press 'RateMartial' | Out-Null
    Check 'martial shows x5' ((Cmd 'uitext RateMartial') -match "text='x5'")
    Press 'Close' | Out-Null
    $e0 = StepExp; Cmd 'bcast 3' 2500 | Out-Null; $gain = (StepExp) - $e0
    $s = Cmd 'rstate' 200
    Check 'the use is scaled by 5' ($s -match 'lastMartial=(\d+)->(\d+)' -and [int]$Matches[2] -eq [int]$Matches[1] * 5) $s
    Check 'the mastery rose by a multiple of 5' ($gain -gt $plain -and $gain % 5 -eq 0) "plain=$plain gain=$gain"
    # AddMartialExpInBattle이 안에서 AddMartialExp를 부른다. 둘 다에 걸리면 오른 양이 제곱(25배)이 된다.
    Check 'the mastery rose by the scaled amount, not its square' ($s -match 'lastMartial=(\d+)->(\d+)' -and $gain -eq [int]$Matches[2]) "gain=$gain $s"

    # 일시 정지와 되돌리기.
    Cmd 'rpause on' 200 | Out-Null; Start-Sleep -Milliseconds 800
    Check 'a paused game stays paused' ((Scale) -eq 0) (Cmd 'rstate')
    Cmd 'rpause off' 200 | Out-Null; Start-Sleep -Milliseconds 800
    Check 'speed comes back after the pause' ((Scale) -eq 2) (Cmd 'rstate')
    Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null
    Check 'speed shows x1' ((Cmd 'uitext RateSpeed') -match "text='x1'")
    Start-Sleep -Milliseconds 500
    Check 'speed returns to 1 when set back' ((Scale) -eq 1) (Cmd 'rstate')
    # 정지 중에 속도를 끄면, 정지가 풀리며 게임이 정지 전의 값을 되살려도 1이 된다(rpause off가 그 값을 되살린다).
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null
    Start-Sleep -Milliseconds 500
    Cmd 'rpause on' 200 | Out-Null
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null
    Check 'speed is turned off while paused' ((Cmd 'uitext RateSpeed') -match "text='x1'" -and (Scale) -eq 0) (Cmd 'rstate')
    Cmd 'rpause off' 200 | Out-Null; Start-Sleep -Milliseconds 800
    Check 'speed is 1 after resuming with speed off' ((Scale) -eq 1) (Cmd 'rstate')
    Press 'God' | Out-Null
    Check 'god is pressed in the rate tab' ((Cmd 'rstate' 200) -match 'god=True')
    # 전투를 끝내고 지도로 나온다(bend는 게임의 종료 절차를 부르고, 결과 창의 확인은 (959, 831)이다). 켜 둔 속도는 지도에서 걸리지 않는다.
    Press 'RateSpeed' | Out-Null; Press 'RateSpeed' | Out-Null; Press 'Close' | Out-Null
    Cmd 'bend' 3000 | Out-Null
    Check 'the result screen opens' ((Cmd 'uilist' 100) -match 'BattleEnd')
    Check 'speed holds on the result screen' ((Scale) -eq 2) (Cmd 'rstate')
    Rc 'click 959 831' 2000 | Out-Null
    for ($i = 0; $i -lt 20 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Start-Sleep -Milliseconds 500 }
    Check 'back on the map' ((Cmd 'uilist' 100) -match 'MapMain')
    Start-Sleep -Milliseconds 500
    Check 'speed is 1 after the battle' ((Scale) -eq 1 -and (Cmd 'rstate' 200) -match 'speed=2 ') (Cmd 'rstate')
    Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null
    Press 'RateReset' | Out-Null
    Shot 'rates-battle'
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'combat' {
    # 배율 탭의 전투 줄이 실제 전투에 걸리는지: 주는 피해(적의 체력이 곱한 만큼 깎인다), 받는 피해, 기술 쿨타임.
    # 전투 셋을 차례로 연다. 전투에서 나올 때는 결과 창이 뜨고 지도가 돌아올 때까지 기다린다(서두르면 다음 전투의 화면이 깨진다).
    $traps = Traps
    function Dmg { Cmd 'bdmg' 200 }
    function Num([string]$text, [string]$pattern) { if ($text -match $pattern) { [double]$Matches[1] } else { throw "읽지 못했습니다: $pattern in $text" } }
    function Into([int]$level) {
      Cmd "fight 1011 $level" 6000 | Out-Null; Rc 'click 960 684' 12000 | Out-Null; Rc 'click 956 749' 4000 | Out-Null
    }
    function Leave {
      Cmd 'bend' 500 | Out-Null
      for ($i = 0; $i -lt 30 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
      Start-Sleep -Seconds 2
      for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Rc 'click 959 831' 2500 | Out-Null }
      Start-Sleep -Seconds 3
    }
    function Tab { Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null }
    Tab
    foreach ($b in 'RateCooldown', 'RateTaken') { Check "$b is drawn" (IsAt $b) }
    # 받는 피해만 켰다가 모두 1배로: 이벤트를 듣던 것도 풀린다.
    Press 'RateTaken' | Out-Null
    Check 'listening while taken is on' ((Dmg) -match 'subscribed=True')
    Press 'RateReset' | Out-Null
    Check 'reset stops listening when only taken was on' ((Dmg) -match 'subscribed=False') (Dmg)
    Press 'Close' | Out-Null

    # 전투 1(약한 적): 받는 피해. 공격하지 않고 맞기만 한다.
    Into 1
    Check 'nothing is listening before a row is on' ((Dmg) -match 'subscribed=False')
    Tab; Press 'RateTaken' | Out-Null
    Check 'taken shows x0.5' ((Cmd 'uitext RateTaken') -match "text='x0.5'")
    Press 'Close' | Out-Null
    for ($i = 0; $i -lt 40 -and (Dmg) -match 'taken=-'; $i++) { Start-Sleep -Milliseconds 500 }
    $s = Dmg
    # 약한 적의 피해는 1이다. 배율로는 0이 되지 않는다(0은 무적의 몫).
    Check 'a hit of 1 stays 1' ($s -match 'taken=base 1->1') $s
    Start-Sleep -Milliseconds 900
    Check 'taken is on the overlay' ((Cmd 'uistate') -match 'status=\[받는 피해 x0.5\] ')

    # 쿨타임: 절기의 쿨타임 초가 절반이 되고, 없음에서 연달아 써지며, x1로 돌리면 원래 값이다.
    $plain = Num (Cmd 'cdstat' 200) 'skill1 cd=\d+/\d+ cdTime=([\d.]+)'
    $plainStep = Num (Cmd 'cdstat' 200) 'step cd=\d+/\d+ cdTime=([\d.]+)'
    Check 'the special skill has a cooldown' ($plain -gt 1) "cdTime=$plain"
    Check 'a second use is refused while cooling down' ((Cmd 'bcast 2' 400) -match 'True' -and (Cmd 'bcast 2' 400) -match 'False')
    Tab; Press 'RateCooldown' | Out-Null
    Check 'cooldown shows x0.5' ((Cmd 'uitext RateCooldown') -match "text='x0.5'")
    Start-Sleep -Milliseconds 500
    $c = Cmd 'cdstat' 200
    Check 'the skill cooldown is halved' ([math]::Abs((Num $c 'skill1 cd=\d+/\d+ cdTime=([\d.]+)') - $plain / 2) -lt 0.02) $c
    Check 'the step cooldown is halved' ([math]::Abs((Num $c 'step cd=\d+/\d+ cdTime=([\d.]+)') - $plainStep / 2) -lt 0.02) $c
    1..3 | ForEach-Object { Press 'RateCooldown' | Out-Null }
    Check 'cooldown shows none' ((Cmd 'uitext RateCooldown') -match "text='없음'")
    Press 'Close' | Out-Null; Start-Sleep -Milliseconds 500
    $ok = @(1..4 | ForEach-Object { Cmd 'bcast 2' 400 } | Where-Object { $_ -match 'True' }).Count
    Check 'the special skill is used four times in a row' ($ok -eq 4) "ok=$ok"
    Start-Sleep -Milliseconds 500
    Check 'cooldown is on the overlay' ((Cmd 'uistate') -match '쿨타임 없음')
    Tab; Press 'RateCooldown' | Out-Null
    Check 'cooldown wraps to x1' ((Cmd 'uitext RateCooldown') -match "text='x1'")
    Start-Sleep -Milliseconds 500
    $c = Cmd 'cdstat' 200
    Check 'the cooldown is back' ([math]::Abs((Num $c 'skill1 cd=\d+/\d+ cdTime=([\d.]+)') - $plain) -lt 0.02 -and $c -match 'rates skill=100/100 step=100/100') $c
    Press 'RateCooldown' | Out-Null; Press 'RateTaken' | Out-Null; Press 'RateTaken' | Out-Null
    Check 'taken shows x0.1' ((Cmd 'uitext RateTaken') -match "text='x0.1'")
    1..3 | ForEach-Object { Press 'Multiplier' | Out-Null }
    Check 'damage shows x10' ((Cmd 'uitext Multiplier') -match "text='x10'")
    Press 'Close' | Out-Null
    Leave
    Check 'back on the map' ((Cmd 'uilist' 100) -match 'MapMain')

    # 전투 2(수준 20의 적, 한 방이 수십): 켜 둔 쿨타임이 새 전투에도 걸리고, 받는 피해가 x0.1로 줄어든다.
    # 수준 30부터는 한 방이 수천이라 고정본 인물이 쓰러진다(쓰러지면 대화 사건이 떠 뒤의 검사가 꼬인다).
    Into 20
    Start-Sleep -Milliseconds 500
    $c = Cmd 'cdstat' 200
    Check 'the kept cooldown applies in the next battle' ([math]::Abs((Num $c 'skill1 cd=\d+/\d+ cdTime=([\d.]+)') - $plain / 2) -lt 0.02) $c
    for ($i = 0; $i -lt 40 -and (Dmg) -notmatch 'taken=base \d{2,}->'; $i++) { Start-Sleep -Milliseconds 500 }
    $s = Dmg
    Check 'a hit on the player is cut to a tenth' ($s -match 'taken=base (\d+)->(\d+)' -and [int]$Matches[1] -ge 10 -and [math]::Abs([int]$Matches[2] - [int]$Matches[1] * 0.1) -le 0.51) $s
    Check 'the player is alive' ((Cmd 'battle stat' 100) -match 'playerHp=[1-9]\d*/')
    Leave

    # 전투 3(수준 60의 적, 체력 수만): 주는 피해가 곱해지고 적의 체력이 그만큼 깎인다. 한 방에 쓰러지지 않게 무적을 켠다.
    Tab; Press 'God' | Out-Null; Press 'Close' | Out-Null
    Into 60
    for ($i = 0; $i -lt 60 -and (Dmg) -notmatch 'drop=prevValue'; $i++) { Cmd 'bcast 1' 350 | Out-Null }
    $s = Dmg
    Check 'a hit is scaled by 10' ($s -match 'last=base (\d+)->(\d+) ' -and [int]$Matches[2] -eq [int]$Matches[1] * 10) $s
    Check 'the enemy loses the scaled amount' ($s -match 'drop=prevValue=(\d+) hp (\d+)->(\d+) ' -and [int]$Matches[2] - [int]$Matches[3] -ge [int]$Matches[1]) $s
    Check 'taken x0.1 is still on' ((Cmd 'rstate' 200) -match 'taken=0.1 ')
    Leave
    Tab; Press 'RateReset' | Out-Null
    $s = Cmd 'rstate' 200
    Check 'reset puts the battle rows back' ($s -match 'cooldown=1 taken=1 god=False damage=1') $s
    Press 'Close' | Out-Null
    Shot 'combat'
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'status' {
    # 스펙 6절 19번. 표시는 30프레임마다 맞춰지므로 켜고 끈 뒤 조금 기다린다.
    $traps = Traps
    Check 'hidden when nothing is on' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    Cmd 'tfreeze on' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'shown without ever opening the editor' ((Cmd 'uistate') -match 'status=\[날짜 고정\] ')
    Cmd 'editor' | Out-Null
    Press 'Tab12' | Out-Null; Press 'God' | Out-Null; Press 'Multiplier' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'all three are listed' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] ')
    Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'stays when the editor is closed' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] open=False ')
    Check 'the overlay does not take clicks' ((Rc 'hit 960 23' 200) -notmatch 'TlToybox')
    Shot 'status'
    # 끈다. 배율은 x2 → x5 → x10 → x100 → x1로 돈다.
    Cmd 'editor' | Out-Null
    Press 'Tab12' | Out-Null; Press 'God' | Out-Null
    1..4 | ForEach-Object { Press 'Multiplier' | Out-Null }
    Press 'Tab11' | Out-Null; Press 'Freeze' | Out-Null
    Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'hidden again when everything is off' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    # 05 문서 48절(C3): 세이브를 떠날 때(Destroy) 켜 둔 것을 끈다. unload가 같은 함수를 부른다.
    Cmd 'tfreeze on' | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null; Press 'God' | Out-Null; Press 'Multiplier' | Out-Null; Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'all three are on again' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] ')
    $r = Cmd 'unload' 500
    Start-Sleep -Milliseconds 900
    Check 'leaving the save turns everything off' ((Cmd 'uistate') -match 'status=\[hidden\] ' -and (Cmd 'tdesc') -match 'frozen=False') $r
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'status-battle' {
    # Review Focus 5. 회차의 마지막에 돌리고 바로 -End 한다(전투에서 나오지 않는다).
    Cmd 'editor' | Out-Null
    Press 'Tab12' | Out-Null; Press 'God' | Out-Null
    Press 'Close' | Out-Null
    EnterBattle
    Check 'the battle scene is entered' ((Cmd 'uilist' 100) -match 'BattleInfo')
    Start-Sleep -Milliseconds 900
    Check 'shown in battle' ((Cmd 'uistate') -match 'status=\[무적\] ')
    Shot 'status-battle'
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'battle' {
    # 스펙 6절 21번, Review Focus 5. 전투에서 나오는 방법을 자동화하지 않았으므로 회차의 마지막에 돌리고 바로 -End 한다.
    $traps = Traps
    EnterBattle
    Check 'the battle scene is entered' ((Cmd 'uilist' 100) -match 'BattleInfo')
    Check 'editor opens in battle' ((Cmd 'editor') -match 'open=True')
    "INFO battle state: $(Cmd 'uistate')"
    Check 'tabs are clickable in battle' (IsAt 'Tab12')
    Press 'Tab12' | Out-Null
    Check 'rate tab is drawn in battle' (IsAt 'God')
    Shot 'battle'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'input' {
    # 05 문서 51절: 창 위의 마우스 휠·포인터가 뒤의 게임(지도 확대·축소, 오른쪽 단추 끌기 이동, 충돌체 호버)에 새지 않는지.
    # 게임 창에 보낸 WM_MOUSEMOVE·WM_MOUSEWHEEL은 Unity의 Input에 닿지 않는다(실측, 회차 fix-1017a). 그래서 개발 빌드의 가상 입력(uipointer·uiwheel·uikey)으로
    # Input.mousePosition·mouseScrollDelta·GetKey를 바꿔 게임의 지도 처리(MapWorldMgr.OnUpdate)와 EventSystem이 함께 그 값을 읽게 하고,
    # 지도 카메라 배율(mapcam의 scale)·위치(camPos)·카메라 eventMask(mask)를 읽어 수치로 본다.
    # 편집기는 기본 위치(60,60)·1500x1000이다: (900,560)은 내용 영역, (1750,540)은 창 오른쪽 바깥의 지도.
    $traps = Traps
    function Scale { $o = Cmd 'mapcam' 150; if ($o -match ' scale=([-\d.]+) ') { [double]$Matches[1] } else { throw "mapcam을 읽지 못했습니다: $o" } }
    function Mask { $o = Cmd 'mapcam' 150; if ($o -match ' cam=[^ ]*/mask=(-?\d+)/') { [int]$Matches[1] } else { 999 } }
    function CamPos { $o = Cmd 'mapcam' 150; if ($o -match ' camPos=(\S+) ') { $Matches[1] } else { '?' } }
    function Pointer([int]$x, [int]$y) { Cmd "uipointer $x $y" 400 | Out-Null }
    # 배율은 트윈으로 바뀌므로 두 번 연속 같은 값이 읽힐 때까지 기다린 뒤 읽는다
    function Settled { $last = Scale; for ($i = 0; $i -lt 12; $i++) { Start-Sleep -Milliseconds 350; $now = Scale; if ($now -eq $last) { return $now }; $last = $now }; $last }
    # 확대와 축소를 번갈아 보내 배율이 끝(1이 가장 당긴 상태, max가 가장 민 상태)에 붙어 변화가 안 보이는 일을 피한다(회차 fix-1017c에서 1→1 실패)
    $script:dir = -1
    function Wheel { $script:dir = -$script:dir; Cmd "uiwheel $script:dir 3" 600 | Out-Null; Settled }
    # 변해야 하는 자리: 끝에 붙어 있었을 수 있으니 안 변하면 반대쪽으로 한 번 더
    function WheelChanges { $s0 = Settled; $s1 = Wheel; if ($s1 -eq $s0) { $s1 = Wheel }; @($s0, $s1) }
    # 오른쪽 단추(KeyCode.Mouse1 = 324)를 누른 채 (x1,y1)에서 (x2,y2)로 끈다. 지도 처리가 한 프레임에 한 번 위치를 읽으므로 명령 사이에 틈을 둔다
    function RightDrag([int]$x1, [int]$y1, [int]$x2, [int]$y2) {
      Pointer $x1 $y1
      Cmd 'uikey 324 400' 300 | Out-Null
      Start-Sleep -Milliseconds 400
      Pointer $x2 $y2
      Start-Sleep -Milliseconds 900
      $pos = CamPos
      Cmd 'uikey 0 0' 300 | Out-Null
      $pos
    }
    try {
      if ((Cmd 'uistate') -match 'open=True ') { Cmd 'uitoggle' | Out-Null }
      "INFO $(Cmd 'colliders' 300)"
      Pointer 960 540
      Cmd 'uiwheel -1 3' 600 | Out-Null; Settled | Out-Null
      $r = WheelChanges
      Check 'a wheel over the bare map changes the zoom (the virtual wheel reaches the game)' ($r[1] -ne $r[0]) "$($r[0]) -> $($r[1])"
      $c0 = CamPos; $c1 = RightDrag 960 540 1200 700
      Check 'a right-button drag over the bare map pans the map (the virtual drag reaches the game)' ($c1 -ne $c0) "$c0 -> $c1"

      Cmd 'editor' | Out-Null
      Pointer 900 560
      $m = Cmd 'mapcam'
      Check 'the virtual pointer counts as over the editor' ($m -match ' editorOver=True ') $m
      Check 'the EventSystem sees the pointer over a UI object' ($m -match '/over=True ') $m
      Check 'cameras stop sending OnMouse events while the pointer is over the editor' ((Mask) -eq 0) $m
      $s0 = Settled; $s1 = Wheel
      Check 'a wheel over the editor leaves the zoom alone' ($s1 -eq $s0) "$s0 -> $s1"
      $s0 = Settled; $s1 = Wheel
      Check 'the opposite wheel over the editor leaves the zoom alone too' ($s1 -eq $s0) "$s0 -> $s1"
      $c0 = CamPos; $c1 = RightDrag 900 560 1200 760
      Check 'a right-button drag over the editor does not pan the map' ($c1 -eq $c0) "$c0 -> $c1"

      Pointer 1750 540
      Check 'cameras send OnMouse events again beside the editor' ((Mask) -ne 0) (Cmd 'mapcam')
      $r = WheelChanges
      Check 'a wheel beside the editor still zooms the map' ($r[1] -ne $r[0]) "$($r[0]) -> $($r[1])"
      $c0 = CamPos; $c1 = RightDrag 1750 540 1850 800
      Check 'a right-button drag beside the editor still pans the map' ($c1 -ne $c0) "$c0 -> $c1"

      # 창 안의 목록은 휠로 넘어간다(인계 8.5절의 미확인 항목. EventSystem이 같은 가상 입력을 읽는다)
      Press 'Tab1' | Out-Null; Press 'ResGroupAll' | Out-Null; Press 'ItemQueryClear' | Out-Null
      $p = Pos 'Row0'
      Pointer $p.x $p.y
      $before = Cmd 'uitext ItemTableRange'
      $s0 = Settled
      Cmd 'uiwheel -1 3' 1200 | Out-Null
      $after = Cmd 'uitext ItemTableRange'
      $s1 = Settled
      Check 'a wheel over the list scrolls the list' ($before -match '중 1~' -and $after -notmatch '중 1~') "$before -> $after"
      Check 'scrolling the list does not zoom the map' ($s1 -eq $s0) "$s0 -> $s1"
      Press 'Close' | Out-Null

      Pointer 900 560
      Check 'cameras send OnMouse events again after closing the editor' ((Mask) -ne 0) (Cmd 'mapcam')
      $r = WheelChanges
      Check 'after closing the editor the wheel zooms the map again' ($r[1] -ne $r[0]) "$($r[0]) -> $($r[1])"
    } finally {
      Cmd 'uikey 0 0' | Out-Null
      Cmd 'uipointer off' | Out-Null
    }
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'grid' {
    # 기술 학습 상한(05 문서 60절). 고정본은 결정 초기(대경지 3)에 무예 8/8로 꽉 차 있다.
    # 비급 학습이 달을 쓰고 이야기 창을 띄우므로 회차의 마지막에 돌린다.
    $traps = Traps
    $s = Cmd 'mgrid' 300
    Check 'the cap starts at the game default and the martial slots are full' ($s -match 'bonus=0 ' -and $s -match '무예/영술:(\d+)/(\d+)' -and [int]$Matches[1] -ge [int]$Matches[2]) $s
    $base = [int]$Matches[2]
    $o = Cmd 'rbook 0 12111 3 1 1' 6000
    $sole = if ((Cmd 'rbooks 12111' 3000) -match '(\w+):12111:') { $Matches[1] } else { '' }
    Check 'a martial book was made' ($sole -ne '') "$o | $(Cmd 'rbooks 12111' 3000)"
    $r = Cmd "rbookstudy $sole" 600
    Check 'the game refuses the book while the slots are full' ($r -match 'can=[1-9]') $r
    Cmd 'editor' | Out-Null; Press 'Tab5' | Out-Null
    Check 'the cap buttons are on the martial tab' ((Cmd 'uitext MartialGrid10') -match "text='\+10'" -and (Cmd 'uitext MartialGrid200') -match "text='해제'") (Cmd 'uitext MartialGrid200')
    Press 'MartialGrid10' | Out-Null
    $s = Cmd 'mgrid' 300
    Check 'the +10 button adds ten slots to every kind' ($s -match 'bonus=10 ' -and $s -match "무예/영술:\d+/$($base + 10)" -and (Cmd 'uistate') -match '학습 상한: \+10') "$s | $(Cmd 'uistate')"
    $before = if ($s -match '무예/영술:(\d+)/') { [int]$Matches[1] } else { -1 }
    $r = Cmd "mgridstudy $sole" 3000
    Check 'the game accepts the book past the old cap' ($r -match 'can=0') $r
    # 비급 학습은 시간을 쓰는 행동이라 우연 이야기 창(DramaFortuitous)이 몇 쪽 뜨고, 다 넘긴 뒤에 배운다(cr-1018b 실측).
    # 그 뒤에 대화 창(DramaDialogue, 화면 전체가 '다음' 단추)이 이어질 수 있다(cr-1018d).
    for ($i = 0; $i -lt 20 -and (Cmd 'uilist' 300) -match 'DramaFortuitous|DramaDialogue'; $i++) {
      if ((Rc 'hit 979 821' 300) -match 'goBtnItem') { Rc 'click 979 821' 2500 | Out-Null }
      elseif ((Rc 'hit 960 540' 300) -match 'btnNext') { Rc 'click 960 540' 2000 | Out-Null }
      else { Start-Sleep -Milliseconds 800 }
    }
    $s = Cmd 'mgrid' 300
    Check 'after the study event the book is learned past the old cap' ($s -match "무예/영술:$($before + 1)/") $s
    Check 'the cap change is in the change log' (@(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 20 -Encoding UTF8 | Where-Object { $_ -match '학습 상한' }).Count -gt 0)
    Cmd 'editor' | Out-Null; Press 'Tab5' | Out-Null   # 이야기 창이 지나간 뒤 편집기를 다시 앞으로
    Press 'MartialGrid0' | Out-Null
    $s = Cmd 'mgrid' 300
    Check 'the default button restores the game count and keeps what was learned' ($s -match 'bonus=0 ' -and $s -match "무예/영술:$($before + 1)/$base") $s
    $o = Cmd 'rbook 0 12112 3 1 1' 6000
    $sole2 = if ((Cmd 'rbooks 12112' 3000) -match '(\w+):12112:') { $Matches[1] } else { '' }
    Check 'another book is refused again at the default cap' ((Cmd "rbookstudy $sole2" 600) -match 'can=[1-9]') (Cmd "rbookstudy $sole2" 600)
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'materials' {
    # 기예 제작 재료 지급(05 문서 62절). 고정본: 단방 6051011은 배움, 단로 없음, 주약 5031061 26개, 연기보 6181110은 배우지 않음, 도보 6092020은 배움.
    # gridfill로 가방을 채우므로 회차의 마지막에 돌린다.
    $traps = Traps
    function Have([int]$id) { $n = 0; foreach ($m in [regex]::Matches((Cmd "cmbag $id" 300), ':(\d+)')) { $n += [int]$m.Groups[1].Value }; $n }
    Cmd 'editor' | Out-Null; Press 'Tab9' | Out-Null
    Press 'ArtMaterialsOpen' | Out-Null
    Check 'the materials screen opens with three kinds and learned-only on' ((IsAt 'MatKind0') -and (IsAt 'MatKind2') -and (Cmd 'uitext MatLearnedOnly') -match "text='배운 것만'") (Cmd 'uistate')
    # 연단
    Cmd 'ui set MatQuery 6051011' | Out-Null
    $sum = Cmd 'uitext MatSummary6051011'
    Check 'the pill row lists the main herb x50 and a furnace note' ($sum -match 'x50' -and $sum -match '단로 1') $sum
    $main0 = Have 5031061; $furn0 = Have 6061011; $lei0 = Have 5031112
    Press 'MatGive6051011' 3000 | Out-Null
    $u = Cmd 'uistate'
    Check 'giving puts one pill set into the bag' ((Have 5031061) -eq $main0 + 50 -and (Have 5031112) -eq $lei0 + 15 -and (Have 6061011) -eq $furn0 + 1 -and $u -match '재료 3종 66개를 넣었습니다\(묶음 1\)') "$u | main $main0->$(Have 5031061) lei $lei0->$(Have 5031112) furnace $furn0->$(Have 6061011)"
    Check 'the give is in the change log' (@(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 20 -Encoding UTF8 | Where-Object { $_ -match '기예 재료' }).Count -gt 0)
    Cmd 'cmpillui 6051011' 2500 | Out-Null
    Shot 'materials-pill'
    Cmd 'rcloseui RefineElixirPre' | Out-Null
    Cmd 'editor' | Out-Null
    Press 'MatCount5' | Out-Null
    Press 'MatGive6051011' 3000 | Out-Null
    Check 'a bundle of five multiplies the herbs and adds no second furnace' ((Have 5031061) -eq $main0 + 300 -and (Have 6061011) -eq $furn0 + 1 -and (Cmd 'uistate') -match '묶음 5') "$(Have 5031061) $(Have 6061011) | $(Cmd 'uistate')"
    Press 'MatCount1' | Out-Null
    Cmd 'ui set MatQuery ' | Out-Null
    # 연기(고정본은 연기보를 배우지 않았다): 보조 재료는 불순물이 가장 작은 5251001(불순물 4) 20개
    Press 'MatKind1' | Out-Null
    Check 'learned-only hides an unlearned forge recipe' (-not (IsAt 'MatGive6181110')) (Cmd 'uistate')
    Press 'MatLearnedOnly' | Out-Null
    Press 'MatGive6181110' 3000 | Out-Null
    $u = Cmd 'uistate'
    Check 'the forge give says the recipe is not learned yet' ($u -match '아직 배우지 않았습니다') $u
    $f = Cmd 'cmforge 6181110 5251001' 1000
    Check 'the given forge set passes the game check' ($f -match 'isCreate=0') $f
    # 부적
    Press 'MatKind2' | Out-Null
    Press 'MatGive6092020_1' 3000 | Out-Null
    $r = Cmd 'cmrune 6092020 1' 2500
    Check 'the given rune materials are auto-filled by the game' ($r -match 'filled=1 5021164x2') $r
    Cmd 'rcloseui RuneSelectDraw' | Out-Null
    # 겹쳐지지 않는 연기 핵심 재료는 한 개가 한 칸이다(리뷰 Important 1): 남은 칸을 5로 두면 묶음 10(핵심 10칸)은 거부된다
    $g = Cmd 'gridfill 0' 1000
    if ($g -match '-> (\d+)/(\d+)') { $free = [int]$Matches[2] - [int]$Matches[1] } else { $free = -1 }
    if ($free -gt 5) { Cmd "gridfill $($free - 5)" 8000 | Out-Null }
    Cmd 'editor' | Out-Null; Press 'Tab9' | Out-Null; Press 'ArtMaterialsOpen' | Out-Null; Press 'MatKind1' | Out-Null
    $core0 = Have 5121111
    Press 'MatCount10' | Out-Null
    Press 'MatGive6181110' 3000 | Out-Null
    $u = Cmd 'uistate'
    Check 'a forge bundle of ten needs a slot for every core piece' ($u -match '가방 칸 \d+개가 더 있어야 합니다' -and (Have 5121111) -eq $core0) "free=$free | $u | core $core0 -> $(Have 5121111)"
    Press 'MatCount1' | Out-Null
    # 가방이 모자라면 아무것도 주지 않는다
    Cmd 'gridfill 600 5' 6000 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab9' | Out-Null; Press 'ArtMaterialsOpen' | Out-Null; Press 'MatKind2' | Out-Null
    Cmd 'ui set MatQuery 6092050' | Out-Null
    $before = Have 5021404
    Press 'MatGive6092050_5' 3000 | Out-Null
    $u = Cmd 'uistate'
    Check 'a full bag refuses the whole set' ($u -match '가방 칸 \d+개가 더 있어야 합니다' -and (Have 5021404) -eq $before) "$u | $before -> $(Have 5021404)"
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'actionday' {
    # 기술 수련의 날짜 소모 배율(05 문서 63절). 개발 명령 adtrain(수련할 날이 가장 긴 공법을 10일 수련)으로 본다.
    # 수련은 달을 쓰고 이야기 창이 뜰 수 있다. 회차의 마지막에.
    $traps = Traps
    function Residue { if ((Cmd 'wdate' 300) -match 'residueDay=(-?\d+)') { [int]$Matches[1] } else { -999 } }
    function PassDrama {
      for ($i = 0; $i -lt 20 -and (Cmd 'uilist' 300) -match 'DramaFortuitous|DramaDialogue'; $i++) {
        if ((Rc 'hit 979 821' 300) -match 'goBtnItem') { Rc 'click 979 821' 2500 | Out-Null }
        elseif ((Rc 'hit 960 540' 300) -match 'btnNext') { Rc 'click 960 540' 2000 | Out-Null }
        else { Start-Sleep -Milliseconds 800 }
      }
    }
    function SetRate([string]$want) {
      Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null
      for ($i = 0; $i -lt 6 -and (Cmd 'uitext RateActionDay') -notmatch "text='$([regex]::Escape($want))'"; $i++) { Press 'RateActionDay' | Out-Null }
      $ok = (Cmd 'uitext RateActionDay') -match "text='$([regex]::Escape($want))'"
      Cmd 'uitoggle' | Out-Null   # 편집기를 닫는다(게임의 이야기 창을 가리지 않게)
      $ok
    }
    function Train([string]$month) {
      Cmd "tset 2 $month 1" 2000 | Out-Null   # 달 첫날에서(남은 날 30)
      $r0 = Residue
      $o = Cmd 'adtrain 10' 4000
      $r1 = Residue
      PassDrama
      [pscustomobject]@{ Used = $r0 - $r1; Out = $o; Exp = $(if ($o -match 'addExp=(\d+)') { [int]$Matches[1] } else { -1 }); Got = $(if ($o -match 'exp (\d+)->(\d+)') { [int]$Matches[2] - [int]$Matches[1] } else { -2 }) }
    }
    Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null
    Check 'the training day row is drawn with its note' ((IsAt 'RateActionDay') -and (Cmd 'uitext RateNoteActionDay') -match '기술 수련') (Cmd 'uitext RateNoteActionDay')
    Cmd 'uitoggle' | Out-Null
    $t = Train 3
    Check 'at x1 training ten days uses ten days and gives its exp' ($t.Out -match 'can=0' -and $t.Used -eq 10 -and $t.Got -eq $t.Exp -and $t.Exp -gt 0) "$($t.Used) days | $($t.Out)"
    Check 'the rate goes to x0.5' (SetRate 'x0.5')
    $t = Train 4
    Check 'at x0.5 training ten days uses five days' ($t.Used -eq 5) "$($t.Used) days | $($t.Out)"
    Check 'at x0.5 the exp is not reduced' ($t.Got -eq $t.Exp -and $t.Exp -gt 0) $t.Out
    Check 'the scaled call is recorded' ((Cmd 'rstate' 300) -match 'lastActionDay=10->5') (Cmd 'rstate' 300)
    Check 'the rate goes to x0' (SetRate 'x0')
    $t = Train 5
    Check 'at x0 training uses no days and still gives exp' ($t.Used -eq 0 -and $t.Got -eq $t.Exp -and $t.Exp -gt 0) "$($t.Used) days | $($t.Out)"
    # 다른 행동은 배율과 무관: 망각은 원래 0일
    $r0 = Residue; $d = Cmd 'addel' 3000; $r1 = Residue
    Check 'forgetting a skill uses no days in the game' ($d -match 'can=0 day=0' -and $r1 -eq $r0) "$r0 -> $r1 | $d"
    Check 'the rate goes back to x1' (SetRate 'x1')
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
