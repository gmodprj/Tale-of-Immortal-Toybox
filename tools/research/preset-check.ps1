# 프리셋 탭의 게임 내 검증(docs/superpowers/specs/2026-10-14-preset-tab-design.md 8절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다.
# 프리셋 폴더(presets)는 세이브 밖에 있지만 session.ps1 -End가 회차 전 상태로 되돌린다. 이 검사는 폴더를 마음대로 지우고 만든다.
# -Range 'a-b': preset-all이 적용할 기본 프리셋 번호 범위(기본 전부). 한 회차의 10분 안에 들게 나눠 돌린다.
param([Parameter(Mandatory)][string]$Part, [string]$Range = '')
. (Join-Path $PSScriptRoot 'check-lib.ps1')

$presetDir = Join-Path $data 'presets'

# "이름=값 이름=값 …" 한 줄을 표로 바꾼다. 명령의 머리("명령 -> ")와 " | " 뒤의 문장은 뗀다.
function Fields([string]$text) {
  $body = ($text -replace '^[^>]*-> ', '') -replace ' \| .*$', ''
  $map = @{}
  foreach ($pair in ($body -split ' ')) {
    $at = $pair.IndexOf('=')
    if ($at -gt 0) { $map[$pair.Substring(0, $at)] = $pair.Substring($at + 1) }
  }
  $map
}
function State { Fields (Cmd 'pstate' 150) }
function Soles { @((Fields (Cmd 'psoles' 150)).soles -split ',' | Where-Object { $_ }) }
# 전투에 들어간다(ui-check.ps1의 combat 부분과 같은 순서: 진입 확인 → 전투 개시).
function Into([int]$level) {
  Cmd "fight 1011 $level" 6000 | Out-Null; Rc 'click 960 684' 12000 | Out-Null; Rc 'click 956 749' 4000 | Out-Null
}
# 전투를 끝내고 지도로 나온다. 결과 창이 뜬 뒤 확인을 누르고, 지도가 돌아온 뒤 몇 초 기다린다(서두르면 화면이 빈다).
function Leave {
  Cmd 'bend' 500 | Out-Null
  for ($i = 0; $i -lt 30 -and (Cmd 'uilist' 100) -notmatch 'BattleEnd'; $i++) { Start-Sleep -Milliseconds 500 }
  Start-Sleep -Seconds 2
  for ($i = 0; $i -lt 10 -and (Cmd 'uilist' 100) -notmatch 'MapMain'; $i++) { Rc 'click 959 831' 2500 | Out-Null }
  Start-Sleep -Seconds 3
}

function WritePreset([string]$fileName, [string]$json) {
  [void][IO.Directory]::CreateDirectory($presetDir)
  [IO.File]::WriteAllText((Join-Path $presetDir $fileName), $json, (New-Object Text.UTF8Encoding $false))
}
# plist의 " | " 뒤는 "순번:파일 이름;…"이다. 그 파일의 순번(목록의 버튼 이름에 붙는 번호)을 돌려준다. 없으면 -1.
function PresetIndex([string]$fileName) {
  $o = Cmd 'plist' 300
  $names = (($o -split ' \| ', 2)[1]) -split ';'
  foreach ($entry in $names) { if ($entry -match "^(\d+):$([regex]::Escape($fileName))\s*$") { return [int]$Matches[1] } }
  -1
}
# 실리지 않은 창작마당 모드가 필요한 기본 프리셋(메타 requires의 workshopId, 05 문서 73절: 21번은 만고신화). 그 파일 이름들.
# 회차를 -Exclude로 열면 모드 기술 줄을 쓸 수 없으므로, 기대 문제 수에서 빼고 preset-all에서 건너뛴다.
$repoPresets = Join-Path (Split-Path (Split-Path $PSScriptRoot)) 'src\ModCode\ModMain\Presets'
function MissingModPresets {
  $mods = Cmd 'wsmods' 400
  # 빈 응답(시간 초과)을 "실리지 않음"으로 읽지 않는다. 판정은 mods=의 soleID:name:ver:publishedFileID; 끝 번호로 한다.
  if ($mods -notmatch ' mods=') { throw "wsmods gave no mod list: $mods" }
  $modList = ($mods -split ' mods=', 2)[1]
  foreach ($meta in Get-ChildItem -LiteralPath $repoPresets -Filter '*.meta.json') {
    $req = @((Get-Content -LiteralPath $meta.FullName -Raw -Encoding UTF8 | ConvertFrom-Json).requires)
    foreach ($r in $req) {
      if ($r.workshopId -and $modList -notmatch (':' + $r.workshopId + ';')) { $meta.Name -replace '\.meta\.json$', '.json'; break }
    }
  }
}
function ExpectedProblems {
  $sum = 0
  foreach ($f in @(MissingModPresets)) { $sum += [int](Fields (Cmd "pcheck $f" 600)).problems }
  $sum
}
# 프리셋 목록(ListView)은 화면에 걸친 줄만 만든다. 기본 프리셋이 열 개를 넘어 한 화면을 넘으므로(05 문서 70절),
# 이동 줄의 "전체 N개 중 a~b번째"를 읽어 i번째 줄(0부터)이 보일 때까지 "처음"·"다음"으로 옮긴다. 줄이 보이면 $true.
function ShowRow([int]$i) {
  for ($k = 0; $k -lt 12; $k++) {
    $o = Cmd 'uitext PresetListRange' 150
    if ($o -notmatch '(\d+)~(\d+)') { return $false }
    $from = [int]$Matches[1] - 1; $to = [int]$Matches[2] - 1
    if ($i -ge $from -and $i -le $to) { return $true }
    if ($i -lt $from) { Press 'PresetListFirst' | Out-Null } else { Press 'PresetListNext' | Out-Null }
  }
  $false
}
$pickedFile = Join-Path $work 'check-preset-picked.json'
$stateFile = Join-Path $work 'check-preset-state.txt'
function SavePicked($picked) { [IO.File]::WriteAllText($pickedFile, ($picked | ConvertTo-Json -Depth 4)) }
function LoadPicked { Get-Content -LiteralPath $pickedFile -Raw | ConvertFrom-Json -AsHashtable }
# 검사용 프리셋(uicheck-preset.json)을 만든다. 옵션과 역천개명·선천운명은 게임의 표에서 뽑는다(번호를 추측하지 않는다).
# 창 계열 기술 넷(23116은 경지 3부터), 과룡문(10016) 세트 여섯과 세트 밖의 심법 하나, 대경지 2~10의 역천개명, 법보·기령·신혼·명우.
# 돌려주는 것: opts(기술 번호 → "번호,…"), fates(대경지 → 번호), born(번호). 열쇠는 모두 글자다.
function NewTestPreset {
  $picked = @{ opts = @{}; fates = @{}; born = '0' }
  foreach ($id in 13111, 23116, 510021, 35111, 81001, 83001, 84001, 86002, 87002, 88002, 82001) {
    $picked.opts["$id"] = (Fields (Cmd "poptpick $id 3" 200)).opts
  }
  $taken = @()
  foreach ($major in 2..10) {
    $fate = (Fields (Cmd "pfatepick $major $($taken -join ',')" 200)).id
    if (-not $fate -or $fate -eq '0') { throw "대경지 $major 의 역천개명 후보를 찾지 못했습니다" }
    $picked.fates["$major"] = $fate
    $taken += $fate
  }
  $picked.born = (Fields (Cmd 'pbornpick' 200)).id
  if (-not $picked.born -or $picked.born -eq '0') { throw '선천운명 후보를 찾지 못했습니다' }
  $skills = @(
    ('{{ "slot": "left", "baseId": 13111, "options": [{0}] }}' -f $picked.opts['13111']),
    ('{{ "slot": "right", "baseId": 23116, "options": [{0}] }}' -f $picked.opts['23116']),
    ('{{ "slot": "step", "baseId": 510021, "options": [{0}] }}' -f $picked.opts['510021']),
    ('{{ "slot": "ultimate", "baseId": 35111, "options": [{0}] }}' -f $picked.opts['35111'])) -join ', '
  $abilities = @(foreach ($id in 81001, 83001, 84001, 86002, 87002, 88002) { '{{ "baseId": {0}, "options": [{1}], "suit": 10016 }}' -f $id, $picked.opts["$id"] })
  $abilities += ('{{ "baseId": 82001, "options": [{0}] }}' -f $picked.opts['82001'])
  $fates = @(foreach ($major in 2..10) { '{{ "grade": {0}, "id": {1} }}' -f $major, $picked.fates["$major"] }) -join ', '
  $json = '{ "format": 1, "kind": "preset", "name": "uicheck preset", "summary": "check", "source": "check", ' +
    '"bornLucks": [ { "id": ' + $picked.born + ' } ], "skills": [ ' + $skills + ' ], "abilities": [ ' + ($abilities -join ', ') + ' ], ' +
    '"fates": [ ' + $fates + ' ], "sprite": { "id": 1002, "artifact": 3031101 }, "elder": { "id": 1 }, ' +
    '"wingman": { "id": 1004, "effects": [100101, 100102], "prefixes": [100401, 100402] } }'
  WritePreset 'uicheck-preset.json' $json
  $picked
}
# pmartial의 출력에서 그 기술의 사본들(줄마다 "sole=… g=… …").
function Copies([int]$baseId) { @((Cmd "pmartial $baseId" 200) -split ' \| ' | Where-Object { $_ -match '^sole=' }) }
switch ($Part) {
  'state' {
    $s = State
    Check 'the fixture is at grade 9, major grade 3' ($s.grade -eq '9' -and $s.major -eq '3') "grade=$($s.grade) major=$($s.major)"
    Check 'ability points are 62 with 54 in use' ($s.ap -eq '62' -and $s.used -eq '54') "ap=$($s.ap) used=$($s.used)"
    Check 'saved attack is 82' ($s.attack -eq '82') $s.attack
    Check 'the equipped skills are read' ($s.left -eq '14111' -and $s.right -eq '22111' -and $s.step -eq '510021' -and $s.ult -eq '34111') "$($s.left) $($s.right) $($s.step) $($s.ult)"
    Check 'six abilities without a suit' ($s.abilities -eq '85001:0,86001:0,84001:0,81001:0,83001:0,82001:0') $s.abilities
    Check 'no artifact, sprite, elder or wingman' ($s.artifact -eq '0' -and $s.sprite -eq '0' -and $s.sprites -eq '0' -and $s.elder -eq '0' -and $s.wing -eq '0')
    Check 'fate records are read' ($s.fates -eq '2:700016,3:700040') $s.fates
    Check 'there are 28 learned skills' ($s.martials -eq '28' -and (Soles).Count -eq 28) "martials=$($s.martials) soles=$((Soles).Count)"
    Check 'the equipped left skill is listed with its options' ((Cmd 'pmartial 14111' 150) -match 'n=1 \| sole=\S+ g=1 eq=True suit=0 lv=\d+ cost=0 nums=[\d,]+ atmax=(True|False)')
    Check 'not in battle' ((Cmd 'pbattle' 150) -match 'not in battle')
  }
  'grade' {
    $s0 = State
    Check 'the path from 9 to 27 takes the best quality rows' ((Fields (Cmd 'pgpath 27' 150)).path -eq '10,11,16,17,18,19,20,21,24,25,26,27') (Cmd 'pgpath 27' 150)
    Check 'there is no path to the same or a lower row' ((Fields (Cmd 'pgpath 9' 150)).path -eq '' -and (Fields (Cmd 'pgpath 4' 150)).path -eq '')
    $want = Fields (Cmd 'pgsum 27' 150)
    $r = Cmd 'pgraise 27' 500
    $s1 = State
    Check 'the grade is 27, major grade 7' ($s1.grade -eq '27' -and $s1.major -eq '7') $r
    foreach ($k in 'attack', 'defense', 'hpMax', 'life', 'ap') {
      Check "$k grew by the breakthrough gains" ($s1[$k] -eq $want[$k]) "have $($s1[$k]) want $($want[$k])"
    }
    # 실측(회차 preset-1014b): 9에서 27까지 올리면 공격력 766, 체력 상한 17104, 수명 9600, 수도 점수 312.
    Check 'the values match the measured ones' ($s1.attack -eq '766' -and $s1.hpMax -eq '17104' -and $s1.life -eq '9600' -and $s1.ap -eq '312') "attack=$($s1.attack) hpMax=$($s1.hpMax) life=$($s1.life) ap=$($s1.ap)"
    Check 'the result gives the new attack' ($r -match '82 -> 766')
    $history = (Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 3 -Encoding UTF8) -join ' '
    Check 'the change log has the grade and the attributes' ($history -match '\(9\) -> .*\(27\)' -and $history -match ' 82, .* -> .* 766, ') $history
    $r = Cmd 'pgraise 27' 300
    Check 'raising to the same row again changes nothing' ((State).attack -eq '766' -and (State).grade -eq '27') $r
    $r = Cmd 'pgraise 9999' 300
    Check 'an unknown row is refused' ((State).grade -eq '27') $r
    Cmd 'pgraise 44' 800 | Out-Null
    $s2 = State
    Check 'the last row is reached' ($s2.grade -eq '44' -and $s2.major -eq '10') "grade=$($s2.grade)"
    Check 'attack grew again' ([int]$s2.attack -gt 766) $s2.attack
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'martial' {
    $n0 = [int](State).martials
    $pick = Fields (Cmd 'poptpick 13111 4' 200)
    Check 'four options are picked for 13111' (@($pick.opts -split ',').Count -eq 4 -and $pick.gmin -eq '1' -and $pick.gmax -eq '10') "opts=$($pick.opts)"
    $m = Fields (Cmd "pmake 13111 3 $($pick.opts)" 500)
    Check 'the skill is made' ($m.result -eq 'ok' -and [int](State).martials -eq $n0 + 1) "result=$($m.result)"
    $made = @((Cmd 'pmartial 13111' 200) -split ' \| ' | Where-Object { $_ -match "sole=$($m.sole) " })
    Check 'it has the options asked for, in order' ($made.Count -eq 1 -and $made[0] -match " nums=$([regex]::Escape($pick.opts)) ") ($made -join '')
    Check 'every option is at its max quality' ($made[0] -match 'atmax=True')
    Check 'it is grade 3 and not equipped' ($made[0] -match ' g=3 eq=False ')
    $m10 = Fields (Cmd "pmake 13111 10 $($pick.opts)" 500)
    Check 'a grade 10 copy is made' ($m10.result -eq 'ok' -and (Cmd 'pmartial 13111' 200) -match "sole=$($m10.sole) g=10 ")
    $m0 = Fields (Cmd 'pmake 13111 3 -' 500)
    Check 'a skill without options is made' ($m0.result -eq 'ok' -and (Cmd 'pmartial 13111' 200) -match "sole=$($m0.sole) g=3 eq=False suit=0 lv=\d+ cost=0 nums= ")
    $n1 = [int](State).martials
    Check 'an option the skill does not have is refused' ((Fields (Cmd 'pmake 13111 3 99' 500)).result -eq 'fail' -and [int](State).martials -eq $n1)
    Check 'twelve options are refused' ((Fields (Cmd 'pmake 13111 3 1,2,3,4,5,6,7,8,9,10,11,12' 500)).result -eq 'fail' -and [int](State).martials -eq $n1)
    Check 'an unknown skill is refused' ((Fields (Cmd 'pmake 999999 3 -' 500)).result -eq 'fail' -and [int](State).martials -eq $n1)
    Check 'a grade outside the range of the skill is refused' ((Fields (Cmd 'pmake 23116 1 -' 500)).result -eq 'fail' -and [int](State).martials -eq $n1)
    Check 'the suit number is written and read back' ((Cmd 'psuit 81001 10006' 200) -match 'ok=True suit=10006')
    Check 'the state shows the suit' ((State).abilities -match '81001:10006')
    Check 'the suit number is not written on a skill that is not an ability' ((Cmd 'psuit 14111 10006' 200) -match 'ok=False')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'extras' {
    # 결정 경지(대경지 3): 신혼과 명우는 쓰지 않는다. 결정에서 신혼을 쓰면 게임이 예외를 낸다(스펙 3절).
    $r = Cmd 'pelder 1' 300
    Check 'the elder is not written below major grade 7' ((State).elder -eq '0') $r
    $r = Cmd 'pwing 1004 100101,100102 100401,100402' 300
    Check 'the wingman is not written below major grade 9' ((State).wing -eq '0') $r

    $r = Cmd 'psprite 1002 3031101' 800
    $s = State
    Check 'artifact 3031101 is equipped and holds sprite 1002' ($s.artifact -eq '3031101' -and $s.sprite -eq '1002' -and $s.sprites -eq '1') $r
    Check 'only the shown talents are on' ($s.talents -eq '49/49' -and $s.talentsall -eq '49/53') "talents=$($s.talents) all=$($s.talentsall)"
    Check 'one artifact was given' ((Cmd 'count 3031101' 150) -match 'count=1\b')
    $r = Cmd 'psprite 1002 3031101' 800
    $s = State
    Check 'applying it again changes nothing' ($r -match 'changed=False' -and $s.sprites -eq '1' -and $s.talents -eq '49/49' -and (Cmd 'count 3031101' 150) -match 'count=1\b') $r
    # 같은 번호의 기령을 다른 법보에: 게임은 같은 번호의 기령을 하나 더 만들지 않고, 든 기령을 옮기면 두 법보가 같은 기령을 가리킨다(실측).
    # 그래서 거부하고 아무것도 주거나 바꾸지 않는다.
    $r = Cmd 'psprite 1002 3031102' 800
    $s = State
    Check 'a sprite held by another artifact is not moved or doubled' ($r -match 'changed=False' -and $s.artifact -eq '3031101' -and $s.sprite -eq '1002' -and $s.sprites -eq '1' -and $s.dupes -eq '0' -and (Cmd 'count 3031102' 150) -match 'count=0\b') $r
    Cmd 'psprite 9999 3031102' 300 | Out-Null
    Cmd 'psprite 1002 10001' 300 | Out-Null
    Check 'an unknown sprite and a non-artifact item are refused' ((State).sprites -eq '1' -and (State).artifact -eq '3031101')

    Cmd 'pgraise 27' 500 | Out-Null
    $a0 = [int](State).attackv
    $r = Cmd 'pelder 1' 500
    $s = State
    Check 'elder 1 is at level 3, the top for major grade 7' ($s.elder -eq '1' -and $s.elderlv -eq '3') $r
    Check 'attack rose with the elder' ([int]$s.attackv -gt $a0) "$a0 -> $($s.attackv)"
    $r = Cmd 'pelder 2' 500
    Check 'the elder is switched and keeps its level' ((State).elder -eq '2' -and (State).elderlv -eq '3') $r
    $r = Cmd 'pelder 99' 300
    Check 'an unknown elder is refused' ((State).elder -eq '2') $r
    $r = Cmd 'pwing 1004 100101 100401' 300
    Check 'the wingman is still not written at major grade 7' ((State).wing -eq '0') $r

    Cmd 'pgraise 36' 500 | Out-Null
    $r = Cmd 'pelder 2' 500
    Check 'the elder grows to level 7 at major grade 9' ((State).elderlv -eq '7') $r
    $r = Cmd 'pwing 1004 100101,100102 100401,100402' 500
    $s = State
    Check 'the wingman and its lists are written' ($s.wing -eq '1004' -and $s.effects -eq '100101,100102' -and $s.prefixes -eq '100401,100402') $r
    $r = Cmd 'pwing 1004 100101 100101' 300      # 100101은 명우 1001의 옵션이다
    Check 'an option of another wingman is refused' ((State).prefixes -eq '100401,100402') $r
    $r = Cmd 'pwing 9999 - -' 300
    Check 'an unknown wingman is refused' ((State).wing -eq '1004') $r

    # 가방이 가득 차면 법보를 줄 수 없다. 기령을 만들지 않고 끝나야 한다. 가방을 채우므로 이 부분의 마지막에 둔다.
    Cmd 'gridfill 600 5' 4000 | Out-Null
    $r = Cmd 'psprite 1003 3031103' 800
    $s = State
    Check 'a full bag fails cleanly' ($s.sprites -eq '1' -and $s.artifact -eq '3031101' -and (Cmd 'count 3031103' 150) -match 'count=0\b') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'extras-battle' {
    # extras 바로 뒤에 돌린다. 명우가 전투에 나오는지 본다. 법보는 도구 칸의 아이콘이라 캡처로 본다.
    Into 20
    Start-Sleep -Seconds 2
    $o = Cmd 'pbattle' 200
    $b = Fields $o
    Check 'the battle is entered' ($b.ContainsKey('wing')) $o
    Check 'the wingman is in the battle' ($b.wing -eq '1004' -and [int]$b.wingobjs -ge 1) $o
    Shot 'preset-extras-battle'
    Leave
    Check 'back on the map' ((Cmd 'uilist' 200) -match 'MapMain')
  }
  'files' {
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    $n = [int](Fields (Cmd 'pdefaults' 200)).embedded
    Check 'the mod carries default presets' ($n -ge 1) "embedded=$n"
    $l = Fields (Cmd 'plist' 400)
    $onDisk = @(Get-ChildItem -LiteralPath $presetDir -Filter *.json -ErrorAction SilentlyContinue)
    Check 'the folder is made with the defaults in it' ($onDisk.Count -eq $n) "files=$($onDisk.Count)"
    Check 'the list has one readable line per default' ([int]$l.count -eq $n -and $l.unreadable -eq '0') "count=$($l.count) unreadable=$($l.unreadable)"
    Check 'the defaults have no unusable line' ([int](Fields (Cmd 'pcheck' 600)).problems -eq (ExpectedProblems)) "$(Cmd 'pcheck' 600) | missing-mod presets: $(@(MissingModPresets) -join ',')"
    Check 'the name search finds a skill by its shown name' ((Cmd 'pfind 13111' 300) -match '\[0 13111 \S+ g1-10\]') (Cmd 'pfind 13111' 300)
    Check 'the option list of a skill is printed' ((Cmd 'popts 13111' 300) -match '#1 multi=\d+ require=\S* slots=[\d,]+ ') (Cmd 'popts 13111' 300)
    Check 'an ability is shown with its number' ((Cmd "pstage $(($onDisk | Sort-Object Name | Select-Object -First 1).Name) 3" 400) -match '\(81001\)') (Cmd "pstage $(($onDisk | Sort-Object Name | Select-Object -First 1).Name) 3" 400)
    Check 'nothing is reported as changed' ((Fields (Cmd 'pdefaults' 200)).changed -eq '0')

    # 기본 파일을 고치면 알리고, 되돌리기가 돌려놓는다. 사용자가 넣은 파일은 그대로다.
    $first = $onDisk | Sort-Object Name | Select-Object -First 1
    $original = [IO.File]::ReadAllText($first.FullName)
    WritePreset $first.Name '{ "format": 1, "kind": "preset", "name": "uicheck changed" }'
    WritePreset 'uicheck-user.json' '{ "format": 1, "kind": "preset", "name": "uicheck user", "skills": [ { "slot": "left", "baseId": 13111, "options": [] } ] }'
    Check 'a changed default is counted' ((Fields (Cmd 'pdefaults' 200)).changed -eq '1')
    Check 'the user file is listed' ([int](Fields (Cmd 'plist' 400)).count -eq $n + 1 -and (PresetIndex 'uicheck-user.json') -ge 0)
    Cmd 'prestore' 400 | Out-Null
    Check 'restore writes the default back' ((Fields (Cmd 'pdefaults' 200)).changed -eq '0' -and [IO.File]::ReadAllText($first.FullName).Replace("`r", '').Trim() -eq $original.Replace("`r", '').Trim())
    Check 'restore leaves the user file alone' (Test-Path -LiteralPath (Join-Path $presetDir 'uicheck-user.json'))
    # 폴더가 이미 있으면 지워진 기본 파일을 다시 풀지 않는다(사용자가 지운 것일 수 있다). 대신 알린다.
    [IO.File]::Delete($first.FullName)
    Check 'a deleted default is not written again by listing' ([int](Fields (Cmd 'plist' 400)).count -eq $n -and -not (Test-Path -LiteralPath $first.FullName))
    Check 'a deleted default is counted' ((Fields (Cmd 'pdefaults' 200)).changed -eq '1')
    Cmd 'prestore' 400 | Out-Null
    # .defaults도 폴더 표지에 든다(05 문서 85절): 탭의 목록(ListCached)을 읽어 둔 뒤 밖에서 .defaults를 지우면 다음 읽기가 다시 만든다.
    Cmd 'rperf preset' 300 20000 | Out-Null
    [IO.File]::Delete((Join-Path $presetDir '.defaults'))
    Cmd 'rperf preset' 300 20000 | Out-Null
    Check 'the cached list reads again when .defaults is deleted outside' (Test-Path -LiteralPath (Join-Path $presetDir '.defaults'))

    # 읽을 수 없는 파일
    WritePreset 'uicheck-broken.json' '{ "format": 1, "kind": "preset", '
    WritePreset 'uicheck-bag.json' '{ "format": 1, "kind": "bag", "name": "x", "items": [] }'
    WritePreset 'uicheck-format.json' '{ "format": 2, "kind": "preset", "name": "x" }'
    $l = Fields (Cmd 'plist' 400)
    Check 'three unreadable files are listed as unreadable' ([int]$l.count -eq $n + 4 -and $l.unreadable -eq '3') "count=$($l.count) unreadable=$($l.unreadable)"
    # 프리셋 옆의 메타 파일(*.meta.json, 검증 등급·출처)은 프리셋 목록에 넣지 않는다(05 문서 69절)
    WritePreset 'uicheck-lines.meta.json' '{ "format": 1, "kind": "preset-meta", "preset": "uicheck-lines.json" }'
    $l = Fields (Cmd 'plist' 400)
    Check 'a meta file is not listed as a preset' ([int]$l.count -eq $n + 4 -and $l.unreadable -eq '3') "count=$($l.count) unreadable=$($l.unreadable)"
    [IO.File]::Delete((Join-Path $presetDir 'uicheck-lines.meta.json'))

    # 새 기본값 풀기(05 문서 70절): 폴더의 .defaults에 없는 내장 파일만 목록을 열 때 푼다. 사용자가 지운 기본 파일은 다시 풀지 않는다.
    $record = Join-Path $presetDir '.defaults'
    Check 'the folder keeps a record of unpacked defaults' ((Test-Path -LiteralPath $record) -and @(Get-Content -LiteralPath $record -Encoding UTF8 | Where-Object { $_ }).Count -eq $n) "record=$(Test-Path -LiteralPath $record)"
    $last = (Get-ChildItem -LiteralPath $presetDir -Filter *.json | Where-Object { $_.Name -notlike 'uicheck*' -and $_.Name -notlike '*.meta.json' } | Sort-Object Name | Select-Object -Last 1).Name
    [IO.File]::WriteAllLines($record, @(Get-Content -LiteralPath $record -Encoding UTF8 | Where-Object { $_ -and $_ -ne $last }), (New-Object Text.UTF8Encoding $false))
    [IO.File]::Delete((Join-Path $presetDir $last))
    Cmd 'plist' 400 | Out-Null
    Check 'a default missing from the record is unpacked by listing' ((Test-Path -LiteralPath (Join-Path $presetDir $last)) -and (Get-Content -LiteralPath $record -Encoding UTF8) -contains $last) $last
    # 기록이 없는 옛 폴더: 01~10은 알던 것으로 보고(지웠어도 다시 풀지 않음), 그 밖의 새 기본값만 푼다
    if ($n -gt 10) {
      $legacy = (Get-ChildItem -LiteralPath $presetDir -Filter '01-*.json' | Where-Object { $_.Name -notlike '*.meta.json' } | Select-Object -First 1).Name
      [IO.File]::Delete($record); [IO.File]::Delete((Join-Path $presetDir $legacy)); [IO.File]::Delete((Join-Path $presetDir $last))
      Cmd 'plist' 400 | Out-Null
      Check 'an old folder gets the new defaults but not a deleted old one' ((Test-Path -LiteralPath (Join-Path $presetDir $last)) -and -not (Test-Path -LiteralPath (Join-Path $presetDir $legacy)) -and (Test-Path -LiteralPath $record)) "$last / $legacy"
      Cmd 'prestore' 400 | Out-Null
    } else {
      "INFO only ten defaults are embedded; the old-folder case needs a new default"
    }

    # 틀린 줄: 그 줄만 쓸 수 없고 나머지는 쓸 수 있다. 줄마다의 뜻은 아래 JSON의 순서대로:
    # 없는 기술 / 칸이 다른 기술 / 없는 옵션 / (쓸 수 있음) / 같은 칸 둘째 / 글자가 든 번호,
    # (쓸 수 있음) / 같은 유형 둘째 / 구성원이 아닌 세트 / 없는 세트, 역천개명이 아닌 번호 / 없는 대경지, 없는 기령, 없는 신혼, 남의 명우 옵션
    WritePreset 'uicheck-lines.json' @'
{
  "format": 1, "kind": "preset", "name": "uicheck lines",
  "skills": [
    { "slot": "left", "baseId": 999999, "options": [] },
    { "slot": "left", "baseId": 22111, "options": [] },
    { "slot": "right", "baseId": 22111, "options": [99] },
    { "slot": "step", "baseId": 510021, "options": [] },
    { "slot": "step", "baseId": 510021, "options": [] },
    { "slot": "ultimate", "baseId": "x" }
  ],
  "abilities": [
    { "baseId": 81001, "options": [] },
    { "baseId": 81001, "options": [] },
    { "baseId": 82001, "options": [], "suit": 10016 },
    { "baseId": 83001, "options": [], "suit": 99999 }
  ],
  "fates": [ { "grade": 2, "id": 1 }, { "grade": 11, "id": 700016 } ],
  "sprite": { "id": 9999, "artifact": 3031101 },
  "elder": { "id": 99 },
  "wingman": { "id": 1004, "effects": [100101], "prefixes": [100101] }
}
'@
    $c = Fields (Cmd 'pcheck uicheck-lines.json' 600)
    Check 'thirteen bad lines are found and two lines stay usable' ($c.problems -eq '13' -and $c.usable -eq '2') (Cmd 'pcheck uicheck-lines.json' 600)

    # 단계: 결정 초기(대경지 3)에서 1~3은 열려 있고 4부터는 경지 미달이다. 23116은 경지 3부터의 기술이라 단계 1·2에는 없다.
    WritePreset 'uicheck-stages.json' '{ "format": 1, "kind": "preset", "name": "uicheck stages", "skills": [ { "slot": "left", "baseId": 14111, "options": [] }, { "slot": "right", "baseId": 23116, "options": [] } ], "elder": { "id": 1 }, "wingman": { "id": 1004, "effects": [], "prefixes": [] } }'
    $st1 = Fields (Cmd 'pstage uicheck-stages.json 1' 400)
    $st3 = Fields (Cmd 'pstage uicheck-stages.json 3' 400)
    $st4 = Fields (Cmd 'pstage uicheck-stages.json 4' 400)
    $st9 = Fields (Cmd 'pstage uicheck-stages.json 9' 400)
    Check 'stage 1 has one line, the skill that exists at grade 1' ($st1.total -eq '1' -and $st1.state -eq 'None') "state=$($st1.state) total=$($st1.total)"
    Check 'stage 3 has both skills' ($st3.total -eq '2' -and $st3.state -eq 'None') "state=$($st3.state) total=$($st3.total)"
    Check 'stage 4 is locked' ($st4.state -eq 'Locked')
    Check 'stage 9 adds the elder and the wingman' ($st9.total -eq '4' -and $st9.state -eq 'Locked') "total=$($st9.total)"

    foreach ($file in Get-ChildItem -LiteralPath $presetDir -Filter 'uicheck-*.json') { [IO.File]::Delete($file.FullName) }
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'apply' {
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    $picked = NewTestPreset
    SavePicked $picked
    $file = 'uicheck-preset.json'
    Check 'the test preset has no unusable line' ((Fields (Cmd "pcheck $file" 600)).problems -eq '0') (Cmd "pcheck $file" 600)
    $s0 = State
    $soles0 = Soles
    $money0 = if ((Cmd 'read' 150) -match 'money=(\d+)') { $Matches[1] } else { throw 'read의 출력에서 money를 찾지 못했습니다' }

    # 경지 미달인 단계는 거부한다(고정본은 대경지 3이다).
    $r = Cmd "papply $file 7" 800
    Check 'a locked stage is refused' ($r -match 'refused=True' -and (State).martials -eq $s0.martials) $r

    # 단계 3: 고정본의 역천개명 3(700040)을 바꾸게 되므로 미리 알린다.
    Check 'stage 3 would replace a fate' ((Fields (Cmd "preplaces $file 3" 300)).replaces -eq 'yes') (Cmd "preplaces $file 3" 300)
    Check 'the full apply would replace fates too and says so' ((Fields (Cmd "preplaces $file full" 300)).replaces -eq 'yes') (Cmd "preplaces $file full" 300)
    $r = Cmd "papply $file 3" 4000
    $run = Fields $r
    $s = State
    Check 'nothing failed' ($run.failed -eq '0') $r
    Check 'four skills and seven abilities were made' ([int]$s.martials -eq [int]$s0.martials + 11) "martials $($s0.martials) -> $($s.martials)"
    Check 'the slots hold the preset skills' ($s.left -eq '13111' -and $s.right -eq '23116' -and $s.step -eq '510021' -and $s.ult -eq '35111') "$($s.left) $($s.right) $($s.step) $($s.ult)"
    $abilities = (@($s.abilities -split ',') | Sort-Object) -join ','
    Check 'the preset abilities are equipped with the suit, and the ability of another type stays' ($abilities -eq '81001:10016,82001:0,83001:10016,84001:10016,85001:0,86002:10016,87002:10016,88002:10016') $abilities
    Check 'ability points cover what is in use' ([int]$s.used -le [int]$s.ap -and [int]$s.ap -gt 62) "used=$($s.used) ap=$($s.ap)"
    Check 'fate 3 is the preset one and fate 2 is untouched' ($s.fates -eq "2:700016,3:$($picked.fates['3'])") $s.fates
    $soles1 = Soles
    Check 'everything learned before is still there' (@($soles0 | Where-Object { $soles1 -notcontains $_ }).Count -eq 0)
    $made = @(Copies 13111 | Where-Object { $_ -match '^sole=(\S+) ' -and $soles0 -notcontains $Matches[1] })
    Check 'the made skill is grade 3, equipped, with the options asked for at max quality' ($made.Count -eq 1 -and $made[0] -match " g=3 eq=True .* nums=$([regex]::Escape($picked.opts['13111'])) atmax=True") ($made -join ' ; ')
    Check 'the artifact and sprite are applied from stage 2 up' ($s.artifact -eq '3031101' -and $s.sprite -eq '1002' -and $s.talents -eq '49/49') "artifact=$($s.artifact) sprite=$($s.sprite) talents=$($s.talents)"
    Check 'the elder and wingman of later stages are not applied' ($s.elder -eq '0' -and $s.wing -eq '0')
    Check 'money is unchanged' ((Cmd 'read' 150) -match "money=$money0\b")
    Check 'stage 3 reads as applied' ((Fields (Cmd "pstage $file 3" 400)).state -eq 'Applied') (Cmd "pstage $file 3" 400)
    $history = Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 1 -Encoding UTF8
    Check 'the change log has the preset line' ($history -match 'uicheck preset') $history

    # 같은 단계를 다시: 공법이 늘지 않고 수도 점수도 다시 오르지 않는다.
    $r = Cmd "papply $file 3" 4000
    $run = Fields $r
    $s2 = State
    Check 'applying again adds nothing' ($run.done -eq '0' -and $run.failed -eq '0' -and $s2.martials -eq $s.martials -and $s2.ap -eq $s.ap -and $s2.sprites -eq $s.sprites) $r
    Check 'there is nothing left to replace' ((Fields (Cmd "preplaces $file 3" 300)).replaces -eq 'no')

    # 단계 1: 선천운명. 공법은 이미 있다(경지 3은 1 이상). 23116은 경지 3부터의 기술이라 이 단계에는 없다.
    $r = Cmd "papply $file 1" 3000
    $s3 = State
    Check 'stage 1 adds the born luck and makes no skill' ([int]$s3.born -eq [int]$s2.born + 1 -and $s3.martials -eq $s2.martials) $r
    # 단계 2: 역천개명 2만 바뀐다.
    $r = Cmd "papply $file 2" 3000
    $s4 = State
    Check 'stage 2 sets fate 2 and leaves fate 3' ($s4.fates -eq "2:$($picked.fates['2']),3:$($picked.fates['3'])" -and $s4.martials -eq $s2.martials) $s4.fates

    # 틀린 줄이 섞인 프리셋: 그 줄만 못 넣고 나머지는 적용된다.
    WritePreset 'uicheck-mixed.json' '{ "format": 1, "kind": "preset", "name": "uicheck mixed", "skills": [ { "slot": "left", "baseId": 999999, "options": [] }, { "slot": "ultimate", "baseId": 34111, "options": [] } ] }'
    $r = Cmd 'papply uicheck-mixed.json 3' 3000
    $run = Fields $r
    Check 'a bad line does not stop the rest' ($run.failed -eq '1' -and $run.done -eq '1' -and (State).ult -eq '34111' -and (State).left -eq '13111') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'apply-full' {
    # apply 뒤에 같은 회차에서 돌린다. 경지를 마지막 줄까지 올리고 전부 적용한 뒤 저장한다(apply-restart가 비교한다).
    $picked = LoadPicked
    $file = 'uicheck-preset.json'
    $s0 = State
    $copies0 = (Copies 13111).Count
    $r = Cmd "papply $file full" 9000
    $run = Fields $r
    $s = State
    Check 'nothing failed' ($run.failed -eq '0') $r
    Check 'the grade is the last row' ($s.grade -eq '44' -and $s.major -eq '10') "grade=$($s.grade)"
    Check 'attack grew with the grade' ([int]$s.attack -gt [int]$s0.attack) "$($s0.attack) -> $($s.attack)"
    $wantFates = (2..10 | ForEach-Object { "$($_):$($picked.fates["$_"])" }) -join ','
    Check 'the fates of every major grade are the preset ones' ($s.fates -eq $wantFates) $s.fates
    # 기록만이 아니라 운명의 효과가 실제로 붙어 있는지 본다(경지를 올린 뒤의 역천개명 적용이 앞 대경지의 것을 지우지 않는가).
    $held = Fields (Cmd "phas $((2..10 | ForEach-Object { $picked.fates["$_"] }) -join ',')" 300)
    Check 'every fate luck is actually held' ($held.held -eq '9' -and $held.of -eq '9') "held=$($held.held) of=$($held.of)"
    Check 'the elder is at its top level for major grade 10' ($s.elder -eq '1' -and $s.elderlv -eq '9') "elder=$($s.elder) lv=$($s.elderlv)"
    Check 'the wingman and its lists are the preset ones' ($s.wing -eq '1004' -and $s.effects -eq '100101,100102' -and $s.prefixes -eq '100401,100402') "wing=$($s.wing) $($s.effects) $($s.prefixes)"
    $copies = Copies 13111
    Check 'one grade 10 copy was made, not one per stage' ($copies.Count -eq $copies0 + 1 -and @($copies | Where-Object { $_ -match ' g=10 eq=True ' }).Count -eq 1) ($copies -join ' ; ')
    Check 'the slots hold the preset skills' ($s.left -eq '13111' -and $s.right -eq '23116' -and $s.step -eq '510021' -and $s.ult -eq '35111') "$($s.left) $($s.right) $($s.step) $($s.ult)"
    Check 'ability points cover what is in use' ([int]$s.used -le [int]$s.ap) "used=$($s.used) ap=$($s.ap)"
    Check 'stage 10 reads as applied' ((Fields (Cmd "pstage $file 10" 400)).state -eq 'Applied') (Cmd "pstage $file 10" 400)
    Cmd 'save' 2500 | Out-Null
    [IO.File]::WriteAllText($stateFile, (Cmd 'pstate' 200))
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'apply-battle' {
    # apply-full 뒤에 돌린다. 세트와 명우가 전투에서 실제로 켜지는지 본다. 법보는 도구 칸의 아이콘이라 캡처로 본다.
    Into 20
    Start-Sleep -Seconds 2
    $o = Cmd 'pbattle' 200
    $b = Fields $o
    Check 'the battle is entered' ($b.ContainsKey('suits')) $o
    Check 'the six-piece suit is active' ($b.suits -match '(^|,)10016:6(,|$)') $o
    Check 'the wingman is in the battle' ($b.wing -eq '1004' -and [int]$b.wingobjs -ge 1) $o
    Shot 'preset-apply-battle'
    Leave
    Check 'back on the map' ((Cmd 'uilist' 200) -match 'MapMain')
  }
  'apply-restart' {
    # apply-full(끝에서 저장한다) 뒤에 session.ps1 -Restart를 하고 돌린다.
    $before = Fields ([IO.File]::ReadAllText($stateFile))
    $s = State
    foreach ($k in 'grade', 'attack', 'ap', 'left', 'right', 'step', 'ult', 'artifact', 'sprite', 'talents', 'elder', 'elderlv', 'wing', 'effects', 'prefixes', 'fates') {
      Check "$k is kept after a restart" ($s[$k] -eq $before[$k]) "have $($s[$k]) want $($before[$k])"
    }
    $now = (@($s.abilities -split ',') | Sort-Object) -join ','
    $then = (@($before.abilities -split ',') | Sort-Object) -join ','
    Check 'the abilities and their suit numbers are kept' ($now -eq $then) "have $now want $then"
    $i = Fields (Cmd 'imm' 200)
    Check 'the immortal power and the first immortal art are kept after a restart' ($i.immortalPoint -eq '6' -and $i.cards -eq '5') (Cmd 'imm' 200)
  }
  'ui' {
    $traps = Traps
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    Cmd 'plist' 400 | Out-Null          # 폴더를 만들고 기본 프리셋을 풀게 한다(검사 파일을 먼저 쓰면 풀지 않는다)
    $picked = NewTestPreset
    $n = [int](Fields (Cmd 'pdefaults' 200)).embedded
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab14' | Out-Null
    Check 'the safety view opens from the title bar' (IsAt 'BackupNow')
    Press 'Tab13' | Out-Null
    Check 'the preset tab is drawn' (IsAt 'PresetRestore')
    $shown = $true
    foreach ($i in 0..$n) { $shown = $shown -and (ShowRow $i) -and (IsAt "PresetDetail$i") -and (IsAt "PresetApply$i") -and (IsAt "PresetFull$i") }
    Check 'every preset has its three buttons' $shown
    Check 'the note is empty while the defaults are untouched' ((Cmd 'uitext PresetNote' 150) -match "text=''")

    # 읽을 수 없는 파일은 줄만 있고 버튼이 없다. 탭에 다시 들어오면 폴더를 다시 읽는다.
    WritePreset 'uicheck-zz-broken.json' '{ "format": 1, '
    Press 'Tab14' | Out-Null; Press 'Tab13' | Out-Null
    $bad = PresetIndex 'uicheck-zz-broken.json'
    $good = PresetIndex 'uicheck-preset.json'
    Check 'an unreadable file has no buttons' ($bad -ge 0 -and (ShowRow $bad) -and (ShowRow $good) -and -not (IsAt "PresetApply$bad") -and -not (IsAt "PresetDetail$bad") -and (IsAt "PresetApply$good"))
    [IO.File]::Delete((Join-Path $presetDir 'uicheck-zz-broken.json'))

    # 기본 파일을 고치면 알림이 뜨고, 되돌리기는 두 번 눌러야 한다.
    $first = Get-ChildItem -LiteralPath $presetDir -Filter *.json | Where-Object { $_.Name -notlike 'uicheck-*' } | Sort-Object Name | Select-Object -First 1
    WritePreset $first.Name '{ "format": 1, "kind": "preset", "name": "uicheck changed" }'
    Press 'Tab14' | Out-Null; Press 'Tab13' | Out-Null
    Check 'the note reports one changed default' ((Cmd 'uitext PresetNote' 150) -match "text='[^']*1[^']*'") (Cmd 'uitext PresetNote' 150)
    Press 'PresetRestore' | Out-Null
    Check 'restore asks first' ((Fields (Cmd 'pdefaults' 200)).changed -eq '1')
    Press 'PresetRestore' | Out-Null
    Check 'the second press restores' ((Fields (Cmd 'pdefaults' 200)).changed -eq '0')

    # 상세보기: 결정 초기(대경지 3)에서 단계 1~3은 적용, 4는 경지 올리기.
    $idx = PresetIndex 'uicheck-preset.json'
    ShowRow $idx | Out-Null
    Press "PresetDetail$idx" | Out-Null
    # 머리글: 제목은 프리셋 이름만, 설명과 출처는 표 위의 줄바꿈되는 글(사용자 보고: 긴 제목이 돌아가기 밑으로 넘어갔다).
    Check 'the sub-page title is the preset name only' ((Cmd 'uitext SubTitle' 150) -match "SubTitle text='uicheck preset'") (Cmd 'uitext SubTitle' 150)
    Check 'the summary and the source are shown under the title' ((Cmd 'uitext PresetSummary' 150) -match "PresetSummary text='check .+check") (Cmd 'uitext PresetSummary' 150)
    Check 'the back button is not covered' (IsAt 'Back')
    Check 'stages 1 to 3 can be applied' ((IsAt 'StageApply1') -and (IsAt 'StageApply2') -and (IsAt 'StageApply3'))
    Check 'stage 4 is locked and offers the raise' ((IsAt 'StageRaise4') -and -not (IsAt 'StageApply4'))
    Check 'every shown stage has a state label' ((Cmd 'uitext StageState3' 150) -match "StageState3 text='[^']+'")

    # 단계 3은 고정본의 역천개명 3을 바꾸므로 먼저 묻는다.
    $m0 = (State).martials
    Press 'StageApply3' | Out-Null
    Check 'apply asks first when it would replace a fate' ((State).martials -eq $m0 -and (State).fates -eq '2:700016,3:700040')
    Press 'StageApply3' 4000 | Out-Null
    $s = State
    Check 'the second press applies the stage' ([int]$s.martials -eq [int]$m0 + 11 -and $s.fates -eq "2:700016,3:$($picked.fates['3'])" -and $s.left -eq '13111') "martials=$($s.martials) fates=$($s.fates)"
    Check 'stage 3 reads as applied' ((Fields (Cmd 'pstage uicheck-preset.json 3' 400)).state -eq 'Applied')
    # 단계 1은 바꿀 것이 없으므로 한 번에 적용된다.
    $born0 = [int]$s.born
    Press 'StageApply1' 3000 | Out-Null
    Check 'a stage that replaces nothing applies at once' ([int](State).born -eq $born0 + 1)
    # 경지 올리기는 두 번 눌러야 한다.
    Press 'StageRaise4' | Out-Null
    Check 'raise asks first' ((State).major -eq '3')
    Press 'StageRaise4' 3000 | Out-Null
    Check 'the second press raises to major grade 4' ((State).major -eq '4')
    # 경지 올리기는 정식 돌파처럼 돌파 임무를 진행시켜, 게임의 대화 사건(금단경 돌파 축하 등)이 편집기를 가릴 수 있다. 닫고 본다.
    for ($n = 0; $n -lt 5 -and (Cmd 'uilist' 200) -match 'DramaDialogue'; $n++) { Cmd 'uiclose DramaDialogue' 800 | Out-Null }
    Check 'no game dialogue is left over the editor' ((Cmd 'uilist' 200) -notmatch 'DramaDialogue') (Cmd 'uilist' 200)
    Check 'stage 4 can now be applied' ((IsAt 'StageApply4') -and -not (IsAt 'StageRaise4'))

    # 목록의 적용: 대경지 2의 역천개명(700016)을 바꾸므로 먼저 묻는다. 지금 대경지(4)까지의 역천개명이 모두 정해진다.
    Press 'Back' | Out-Null
    Check 'back on the list' (IsAt 'PresetRestore')
    ShowRow $idx | Out-Null
    Press "PresetApply$idx" | Out-Null
    Check 'apply asks first' ((State).fates -match '^2:700016,')
    Press "PresetApply$idx" 5000 | Out-Null
    $s = State
    Check 'apply sets the fates up to the current major grade' ($s.fates -eq "2:$($picked.fates['2']),3:$($picked.fates['3']),4:$($picked.fates['4'])") $s.fates
    # 최종 단계까지: 두 번 눌러야 한다.
    Press "PresetFull$idx" | Out-Null
    Check 'the full apply asks first' ((State).major -eq '4')
    Press "PresetFull$idx" 9000 | Out-Null
    $s = State
    Check 'the second press raises to the last row and applies everything' ($s.grade -eq '44' -and $s.wing -eq '1004' -and $s.elder -eq '1') "grade=$($s.grade) wing=$($s.wing) elder=$($s.elder)"
    Check 'the message area shows the result' ((Cmd 'uistate' 150) -match "message='[^']*uicheck preset")
    Check 'no callback exception' ((Traps) -eq $traps)
    Check 'no mod warning' (@(ModWarnings).Count -eq 0) ((ModWarnings) -join ' | ')
  }
  'fate-groups' {
    # 게임은 같은 계열(fateFeature.group)의 역천개명을 하나만 둔다. 같은 계열을 또 붙이면 앞의 운명이 사라지고 기록만 남는다(실측).
    # 700001과 700002는 계열 1이다(설정 표 FateFeature). 고정본의 기록은 2:700016(계열 없음), 3:700040(계열 9).
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    WritePreset 'uicheck-group.json' '{ "format": 1, "kind": "preset", "name": "uicheck group", "fates": [ { "grade": 2, "id": 700001 }, { "grade": 3, "id": 700002 } ] }'
    $c = Fields (Cmd 'pcheck uicheck-group.json' 600)
    Check 'two fates of one group in a file: the second is unusable' ($c.problems -eq '1' -and $c.usable -eq '1') (Cmd 'pcheck uicheck-group.json' 600)
    WritePreset 'uicheck-g3.json' '{ "format": 1, "kind": "preset", "name": "uicheck g3", "fates": [ { "grade": 3, "id": 700002 } ] }'
    WritePreset 'uicheck-g2.json' '{ "format": 1, "kind": "preset", "name": "uicheck g2", "fates": [ { "grade": 2, "id": 700001 } ] }'
    $r = Cmd 'papply uicheck-g3.json 3' 2000
    Check 'a fate of group 1 is set at major grade 3' ((State).fates -eq '2:700016,3:700002' -and (Cmd 'phas 700002' 200) -match 'held=1 ') $r
    $r = Cmd 'papply uicheck-g2.json 2' 2000
    Check 'another fate of the same group at another grade is refused' ((Fields $r).failed -eq '1' -and (State).fates -eq '2:700016,3:700002') $r
    Check 'the fate held before is still held' ((Cmd 'phas 700002,700016' 200) -match 'held=2 of=2')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'fate-set' {
    # 운명 탭과 캐릭터 가져오기가 쓰는 FateService.Set, 운명 추가가 쓰는 LuckService.Add의 계열 보호.
    $r = Cmd 'pfateset 3 700002' 800
    Check 'a fate of group 1 is set at major grade 3' ((State).fates -eq '2:700016,3:700002' -and (Cmd 'phas 700002' 200) -match 'held=1 ') $r
    $r = Cmd 'pfateset 2 700001' 800
    Check 'a fate of the same group at another grade is refused' ((State).fates -eq '2:700016,3:700002' -and (Cmd 'phas 700002,700016' 200) -match 'held=2 of=2') $r
    $r = Cmd 'pluckadd 700001' 800
    Check 'adding a fate luck of the same group is refused' ((Cmd 'phas 700001' 200) -match 'held=0 ' -and (Cmd 'phas 700002' 200) -match 'held=1 ') $r
    $r = Cmd 'pfateset 3 700001' 800
    Check 'the same group at the same grade replaces' ((State).fates -eq '2:700016,3:700001' -and (Cmd 'phas 700001' 200) -match 'held=1 ' -and (Cmd 'phas 700002' 200) -match 'held=0 ') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'apply-switch' {
    # 프리셋에서 프리셋으로: A를 최종 단계까지 적용한 뒤, 역천개명 둘이 서로 맞바뀐 B를 적용한다. 못 넣음 없이 B의 것이 모두 실제로 붙어야 한다.
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    $picked = NewTestPreset
    $file = 'uicheck-preset.json'
    $r = Cmd "papply $file full" 9000
    Check 'preset A is applied' ((Fields $r).failed -eq '0') $r
    $a = [IO.File]::ReadAllText((Join-Path $presetDir $file))
    $nine = $picked.fates['9']; $ten = $picked.fates['10']
    $swapped = $a.Replace('"uicheck preset"', '"uicheck swapped"').Replace("{ `"grade`": 9, `"id`": $nine }", "{ `"grade`": 9, `"id`": $ten }").Replace("{ `"grade`": 10, `"id`": $ten }", "{ `"grade`": 10, `"id`": $nine }")
    WritePreset 'uicheck-swapped.json' $swapped
    Check 'the swapped preset reads without unusable lines' ((Fields (Cmd 'pcheck uicheck-swapped.json' 600)).problems -eq '0') (Cmd 'pcheck uicheck-swapped.json' 600)
    $r = Cmd 'papply uicheck-swapped.json all' 6000
    Check 'preset B is applied without a failed line' ((Fields $r).failed -eq '0') $r
    $want = ((2..8 | ForEach-Object { "$($_):$($picked.fates["$_"])" }) + "9:$ten", "10:$nine") -join ','
    Check 'the two fates are swapped in the records' ((State).fates -eq $want) (State).fates
    $ids = (2..10 | ForEach-Object { $picked.fates["$_"] }) -join ','
    Check 'every fate luck is actually held' ((Cmd "phas $ids" 300) -match 'held=9 of=9') (Cmd "phas $ids" 300)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'preset-all' {
    # 기본 프리셋 10개를 차례로 최종 단계까지 적용한다. 프리셋마다 못 넣음이 없고, 마지막 단계가 "적용됨"이어야 한다.
    # 앞 프리셋의 역천개명·신혼·명우를 다음 프리셋이 바꾼다(프리셋에서 프리셋으로 갈아타기).
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    $c = Fields (Cmd 'pcheck' 900)
    $embedded = (Fields (Cmd 'pdefaults' 200)).embedded
    Check 'every default preset is listed and readable' ([int]$embedded -ge 10 -and $c.presets -eq $embedded -and $c.unreadable -eq '0') "embedded=$embedded presets=$($c.presets)"
    $skip = @(MissingModPresets)
    if ($skip.Count -gt 0) { "INFO skipped (mod not loaded): $($skip -join ', ')" }
    Check 'no default preset has an unusable line' ([int]$c.problems -eq (ExpectedProblems)) (Cmd 'pcheck' 900)
    $lo = 0; $hi = 99
    if ($Range -match '^(\d+)-(\d+)$') { $lo = [int]$Matches[1]; $hi = [int]$Matches[2] }
    foreach ($file in Get-ChildItem -LiteralPath $presetDir -Filter *.json | Where-Object { $_.Name -notlike '*.meta.json' -and $skip -notcontains $_.Name -and [int]$_.Name.Substring(0, 2) -ge $lo -and [int]$_.Name.Substring(0, 2) -le $hi } | Sort-Object Name) {
      $r = Cmd "papply $($file.Name) full" 1000 30000   # 적용은 8~17초 걸린다(05 문서 70절)
      $run = Fields $r
      Check "$($file.BaseName.Substring(0, 2)): nothing failed" ($run.failed -eq '0') $r
      # 로더가 고른 번호(줄의 "alt"를 쓴 경우 대체 번호, 05 문서 73절)를 pstrict의 "fate 단계:번호" 줄에서 읽는다.
      $ids = (([regex]::Matches((Cmd "pstrict $($file.Name)" 600), 'fate \d+:(\d+)') | ForEach-Object { $_.Groups[1].Value }) -join ',')
      Check "$($file.BaseName.Substring(0, 2)): every fate luck of the preset is actually held" ((Cmd "phas $ids" 300) -match 'held=9 of=9') (Cmd "phas $ids" 300)
      $st = Fields (Cmd "pstage $($file.Name) 10" 500)
      Check "$($file.BaseName.Substring(0, 2)): the last stage reads as applied" ($st.state -eq 'Applied') (Cmd "pstage $($file.Name) 10" 500)
      Check "$($file.BaseName.Substring(0, 2)): ability points cover what is in use" ([int](State).used -le [int](State).ap) "used=$((State).used) ap=$((State).ap)"
    }
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'fate-dupe' {
    # 한 프리셋에 같은 번호의 역천개명이 두 대경지에 있으면 둘째 줄은 쓸 수 없다(적용하면 두 칸이 서로를 비운다).
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    WritePreset 'uicheck-dupe.json' '{ "format": 1, "kind": "preset", "name": "uicheck dupe", "fates": [ { "grade": 2, "id": 700044 }, { "grade": 3, "id": 700044 } ] }'
    $c = Fields (Cmd 'pcheck uicheck-dupe.json' 600)
    Check 'the same fate at two grades: the second line is unusable' ($c.problems -eq '1' -and $c.usable -eq '1') (Cmd 'pcheck uicheck-dupe.json' 600)
    # 선천운명으로 붙이는 길도 계열을 본다. 고정본의 3:700040(귀일결)과 700058(귀일진결)은 계열 9다.
    $r = Cmd 'pborn 700058' 800
    Check 'a born luck of the same group as a held fate is refused' ((Cmd 'phas 700058' 200) -match 'held=0 ' -and (Cmd 'phas 700040' 200) -match 'held=1 ') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'import-swap' {
    # 캐릭터 가져오기의 역천개명 구역: 파일과 인물의 두 대경지가 서로 맞바뀌어 있어도 한 번에 들어가야 한다.
    $exports = Join-Path $data 'exports\character'
    function CleanExports { if (Test-Path -LiteralPath $exports) { Get-ChildItem -LiteralPath $exports -Filter 'uicheck-*' | ForEach-Object { [IO.File]::Delete($_.FullName) } } }
    CleanExports
    try {
      Cmd 'pfateset 2 700017' 800 | Out-Null
      Cmd 'pfateset 3 700016' 800 | Out-Null
      Check 'the records are set up' ((State).fates -eq '2:700017,3:700016') (State).fates
      Cmd 'cexport uicheck-swap' 1500 | Out-Null
      Cmd 'pfateset 3 700040' 800 | Out-Null
      Cmd 'pfateset 2 700016' 800 | Out-Null
      Cmd 'pfateset 3 700017' 800 | Out-Null
      Check 'the character now has the two swapped' ((State).fates -eq '2:700016,3:700017') (State).fates
      $r = Cmd 'cimpfates uicheck-swap' 3000
      Check 'importing the fates puts both without a failed line' ((State).fates -eq '2:700017,3:700016' -and $r -notmatch '못 넣음 [1-9]') $r
      Check 'both fate lucks are actually held' ((Cmd 'phas 700017,700016' 200) -match 'held=2 of=2') (Cmd 'phas 700017,700016' 200)
      Check 'no callback exception in the log' ((Traps) -eq 0)
    } finally { CleanExports }
  }
  'grade-full' {
    # 경지 올리기가 게임의 정식 돌파와 같은 것을 남기는지(회차 grade-1014a~f에서 정식 돌파와 비교해 찾은 것).
    # 등선 중기·후기의 선력(3+3), 등선 진입의 선법(금홍파효 카드 5장)과 숨은 운명 10001, 돌파 임무(1111007 금단경 돌파)의 정리,
    # 경지에 묶인 칭호(2001 영단도인은 구령경에서 잃는다)의 정리, 현재 체력·영력·집중력을 최대로.
    $r = Cmd 'pgraise 44' 4000
    $i = Fields (Cmd 'imm' 200)
    Check 'the immortal power of the last two steps is given' ($i.immortalPoint -eq '6') "imm: $(Cmd 'imm' 200)"
    Check 'the first immortal art is given' ($i.cards -eq '5') "imm: $(Cmd 'imm' 200)"
    Check 'the hidden luck of the last realm is held' ((Cmd 'phas 10001' 200) -match 'held=1 ') (Cmd 'phas 10001' 200)
    $t = Cmd 'utask' 200
    Check 'the task to break through to golden core is no longer open' ($t -notmatch '\[1111007 ') $t
    Check 'the title lost at the fifth realm is gone' ($t -notmatch 'titles=.*\b2001,') $t
    $h = Cmd 'hpstate' 200
    $ok = $true
    foreach ($m in [regex]::Matches($h, '(\d+)/(\d+)')) { if ($m.Groups[1].Value -ne $m.Groups[2].Value) { $ok = $false } }
    Check 'vitality, energy and focus are full' $ok $h
    Cmd 'uiclose GetImmortalSkill' 500 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'grade-settle' {
    # 예전 판으로 올려 둔 인물(경지 번호만 등선 후기이고 선력·선법이 없다)을 경지 올리기와 최종 단계 적용이 채운다.
    Cmd 'setgrade 44' 500 | Out-Null
    Check 'the character starts without immortal power' ((Fields (Cmd 'imm' 200)).immortalPoint -eq '0') (Cmd 'imm' 200)
    $r = Cmd 'pgraise 44' 3000
    $i = Fields (Cmd 'imm' 200)
    Check 'raising to the same step fills the immortal power and the first art' ($i.immortalPoint -eq '6' -and $i.cards -eq '5') "$r | imm: $(Cmd 'imm' 200)"
    Check 'the hidden luck of the last realm is held' ((Cmd 'phas 10001' 200) -match 'held=1 ') (Cmd 'phas 10001' 200)
    $r = Cmd 'pgraise 44' 3000
    $i = Fields (Cmd 'imm' 200)
    Check 'doing it again adds nothing' ($i.immortalPoint -eq '6' -and $i.cards -eq '5') "imm: $(Cmd 'imm' 200)"
    Cmd 'uiclose GetImmortalSkill' 500 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'immortal-attr' {
    # 능력치 탭의 "본원 선력" 줄: 선법의 선력 총량(unitData.immortalCard.immortalPoint)을 게임의 상한(0~40) 안에서 정한다.
    $r = Cmd 'cattr immortalPoint 12' 800
    Check 'the immortal power total is set' ((Fields (Cmd 'imm' 200)).immortalPoint -eq '12') "$r | $(Cmd 'imm' 200)"
    $r = Cmd 'cattr immortalPoint 41' 800
    Check 'a value over the game limit is refused' ((Fields (Cmd 'imm' 200)).immortalPoint -eq '12') "$r | $(Cmd 'imm' 200)"
    Check 'the editor draws the row in the combat category' ((Cmd 'editor') -match 'open=True')
    Press 'Tab0' | Out-Null
    Press 'AttrCat1' | Out-Null
    Shot 'immortal-attr'
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
