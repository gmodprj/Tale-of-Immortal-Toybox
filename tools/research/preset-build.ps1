# 기본 프리셋의 JSON(src/ModCode/ModMain/Presets)과 대조표(Markdown)를 한 표에서 만든다. 01~10은 2026-10-14, 11~16은 2026-10-20(17~20은 만들지 않음, 05 문서 70절).
# 이름은 게임에서 내보낸 표(.devremote\dump-skills.txt, dump-fates.txt: 개발 명령 pdump skills, pdump fates)에서 읽는다. 번호를 손으로 옮겨 적지 않기 위해서다.
# 메타 파일(*.meta.json)은 이 스크립트가 만들지 않는다.
# 옵션은 "특수 옵션"의 번호만 적고, 자리는 기술 종류의 규칙으로 채운다(05 문서 34절):
#   무예/영술·심법: 1 1 S 1 S 1 S 1 S 1 S      절기·신통: 1 E S E S E S E S E S (E의 기본은 7 1 1 1 1, 7은 재사용 대기시간)
#   신법: 1 S 1 S 2 S 2 S 1 S S
param([string]$TablesTo = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot)
$dump = & (Join-Path $PSScriptRoot '..\DevRemote\devremote-dir.ps1')   # .devremote 경로는 한 곳(05 문서 49절)
$out = Join-Path $root 'src\ModCode\ModMain\Presets'

$skillName = @{}
foreach ($line in [IO.File]::ReadAllLines((Join-Path $dump 'dump-skills.txt'))) {
  $f = $line.TrimStart([char]0xFEFF) -split "`t"
  if ($f.Count -ge 3) { $skillName[$f[1]] = $f[2] }
}
$fateName = @{}
foreach ($line in [IO.File]::ReadAllLines((Join-Path $dump 'dump-fates.txt'))) {
  $f = $line.TrimStart([char]0xFEFF) -split "`t"
  if ($f.Count -ge 2) { $fateName[$f[0]] = $f[1] }
}
# pdump fates의 이름은 띄어쓰기가 없다. 화면의 이름(GameTool.LS)과 다른 것은 화면 이름으로 쓴다(05 문서 69·70절).
$fateFix = @{ '700020' = '미끄러운 발'; '700089' = '영능의 비'; '700090' = '영능의 샘'; '700091' = '영능의 바다'; '700097' = '영력의 나무'; '700100' = '체력의 나무' }
foreach ($k in $fateFix.Keys) { $fateName[$k] = $fateFix[$k] }
$kindOf = @{ '81' = '경'; '82' = '공'; '83' = '식'; '84' = '결'; '85' = '록'; '86' = '비권'; '87' = '신공'; '88' = '대법' }
$lineOf = @{ '01' = '도'; '02' = '창'; '03' = '검'; '04' = '권'; '05' = '장'; '06' = '지'; '07' = '화'; '08' = '수'; '09' = '뇌'; '10' = '풍'; '11' = '토'; '12' = '목' }
$suitName = @{ 10006 = '도강운'; 10012 = '옥룡음'; 10013 = '단관하'; 10016 = '과룡문'; 10022 = '경중인'; 10024 = '난방화'; 10026 = '망해조'; 10027 = '연화령'; 10030 = '풍릉소'; 10032 = '황령연' }
$spriteName = @{ 1001 = '운몽의'; 1002 = '창애'; 1003 = '주진' }
$elderName = @{ 1 = '교룡'; 2 = '구진' }
$wingName = @{ 1004 = '봉황'; 1005 = '현조'; 1007 = '청난'; 1009 = '촉조' }

function AbilityName([int]$id) {
  $text = "$id"
  $kind = $kindOf[$text.Substring(0, 2)]
  if ($id -ge 86000) { "$kind($($lineOf[$text.Substring(3, 2)]))" } else { $kind }
}
function Options([string]$slot, [int[]]$special, [int[]]$even) {
  if ($slot -eq 'step') {
    if ($special.Count -ne 6) { throw "신법의 특수 옵션은 여섯이어야 합니다" }
    return @(1, $special[0], 1, $special[1], 2, $special[2], 2, $special[3], 1, $special[4], $special[5])
  }
  if ($special.Count -ne 5) { throw "특수 옵션은 다섯이어야 합니다($slot)" }
  if ($slot -eq 'right' -or $slot -eq 'ultimate') {
    if (-not $even) { $even = 7, 1, 1, 1, 1 }
    return @(1, $even[0], $special[0], $even[1], $special[1], $even[2], $special[2], $even[3], $special[3], $even[4], $special[4])
  }
  @(1, 1, $special[0], 1, $special[1], 1, $special[2], 1, $special[3], 1, $special[4])
}
# 줄을 만드는 함수들. said는 공략의 표기이고, 비어 있으면 채운 줄이다.
function Skill([string]$slot, [int]$id, [int[]]$special, [string]$said, [int[]]$even) {
  @{ slot = $slot; id = $id; options = (Options $slot $special $even); said = $said }
}
function Ability([int]$id, [int[]]$special, [int]$suit, [string]$said) {
  @{ id = $id; options = (Options 'ability' $special $null); suit = $suit; said = $said }
}
function Fate([int]$grade, [int]$id, [string]$said) { @{ grade = $grade; id = $id; said = $said } }

# 여러 빌드가 함께 쓰는 줄.
$generic = @{
  rok = { Ability 85001 @(3, 7, 2, 4, 5) 0 '' }
}
$swordRight = { param($said) Skill 'right' 24111 @(10, 11, 9, 12, 8) $said }
$moonLeft = { param($said) Skill 'left' 14113 @(6, 9, 14, 16, 8) $said }
$fireStep = { param($said) Skill 'step' 510111 @(8, 7, 4, 3, 9, 11) $said }
$firePhoenix = { param($said) Skill 'ultimate' 61112 @(11, 8, 9, 12, 10) $said }

$builds = @(
  @{ file = '01-풍검'; name = '풍검'
     summary = '월영검으로 유혈을 쌓고 절영검으로 터뜨려 풍신법(용권풍)의 재사용 대기시간을 없앤다. 용권풍 동안 무적'
     source = '팬덤 위키 Wind Sword, 무협게임 마이너 갤러리 10097'
     skills = @((& $moonLeft 'Moonshadow Sword'), (& $swordRight 'Shadowless Sword'),
       (Skill 'step' 510141 @(4, 3, 10, 7, 8, 11) 'Wind motion skill(Tornado), 풍신법'), (Skill 'ultimate' 64111 @(12, 13, 8, 9, 10) 'Arctic Wind'))
     abilities = @((Ability 81001 @(11, 12, 2, 4, 23) 10006 ''), (Ability 82001 @(6, 13, 5, 3, 25) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(3, 2, 13, 14, 16) 10012 ''), (& $generic.rok), (Ability 86003 @(8, 2, 6, 4, 3) 10012 'Sword Secret Manual'),
       (Ability 87003 @(4, 2, 7, 3, 6) 10012 'Sword Divine Power, 검신공'), (Ability 88003 @(19, 17, 9, 12, 15) 10012 'Sword Mastery, 검 대법'))
     fates = @((Fate 2 700017 'Water Burst, 수력발산'), (Fate 3 700044 '노이미견(갤러리의 0티어 목록)'), (Fate 4 700094 'Blood Energy 1-3'), (Fate 5 700056 'Blood Power, 혈마대법-혈살대법-혈염대법'),
       (Fate 6 700116 'Martial Skill Specialization 1-3, 무예입문-출중-정통'), (Fate 7 700133 'Free Spirit'), (Fate 8 700158 '살심취기(갤러리의 0티어 목록)'),
       (Fate 9 700100 '체력의 나무(갤러리의 0티어 목록)'), (Fate 10 700009 '근비충(갤러리의 0티어 목록)'))
     sprite = @{ id = 1001; artifact = 3031101; said = 'Artifact Spirit Mengyi' }; elder = @{ id = 1; said = 'Jiaolong' }
     wingman = @{ id = 1009; effects = 100101, 100103; prefixes = 100903, 100905; said = 'Nether Feather: Candlelight' } },

  @{ file = '02-수검'; name = '수검'
     summary = '월영검·절영검에 수신법(점멸과 환영)과 취천검. 신법과 절기를 번갈아 써 서로의 재사용 대기시간을 줄인다'
     source = 'taleofimmortal.com/builds/water-sword'
     born = @(@{ id = 2806; said = "Lotus Sword's Will" })
     skills = @((& $moonLeft 'Moonshadow Sword(月影剑)'), (& $swordRight 'Shadowless Sword(绝影剑)'),
       (Skill 'step' 510121 @(4, 6, 7, 8, 10, 11) 'Water Motion Skill(水身法)'), (Skill 'ultimate' 34111 @(11, 12, 13, 8, 9) 'Skyfall Sword(骤天剑)'))
     abilities = @((Ability 81001 @(11, 12, 2, 4, 23) 10006 ''), (Ability 82001 @(5, 3, 25, 19, 2) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(7, 3, 2, 13, 14) 10012 ''), (& $generic.rok), (Ability 86003 @(8, 2, 6, 4, 3) 10012 'Sword Secret Manual'),
       (Ability 87003 @(4, 2, 7, 3, 6) 10012 'Sword Divine Power'), (Ability 88003 @(19, 17, 9, 12, 15) 10012 'Sword Technique / Da Fa'))
     fates = @((Fate 2 700152 '穿云电闪'), (Fate 3 700158 '杀心骤起'), (Fate 4 700100 '体力之树'), (Fate 5 700132 '化物'), (Fate 6 700160 '天外有天'),
       (Fate 7 700156 '凝元寂灭'), (Fate 8 700137 '天地同寿'), (Fate 9 700198 '烟罗幻瘴'), (Fate 10 700056 'Blood Demon'))
     sprite = @{ id = 1001; artifact = 3031101; said = 'Yun Mengyi' }; elder = @{ id = 1; said = 'Jiaolong' } },

  @{ file = '03-화검'; name = '화검'
     summary = '화신법(불여우)의 열염으로 피해를 올리고, 월영검·절영검으로 화신법의 재사용 대기시간을 없애 열염을 계속 쌓는다'
     source = '팬덤 위키 Build, Fire Sword'
     skills = @((& $moonLeft 'Moonshadow Sword'), (& $swordRight 'Shadowless Sword'), (& $fireStep 'Fire motion skill(Illusion)'), (& $firePhoenix 'Fire Phoenix'))
     abilities = @((Ability 81001 @(9, 11, 12, 2, 4) 10006 ''), (Ability 82001 @(10, 5, 3, 25, 2) 10006 ''), (Ability 83001 @(11, 6, 3, 4, 16) 10012 ''),
       (Ability 84001 @(6, 3, 2, 13, 14) 10012 ''), (& $generic.rok), (Ability 86007 @(4, 5, 2, 10, 6) 0 'Fire Secret Manual'),
       (Ability 87003 @(4, 2, 7, 3, 6) 10012 'Sword Divine Power'), (Ability 88003 @(19, 17, 9, 12, 15) 10012 'Sword Mastery'))
     fates = @((Fate 2 700017 'Water Burst'), (Fate 3 700037 'Combat Expertise Earth/Fire'), (Fate 4 700026 "Warlord's Phantom"), (Fate 5 700056 'Blood Power 1'),
       (Fate 6 700094 'Blood Energy 1-3'), (Fate 7 700116 'Martial Skill Specialization 1-3'), (Fate 8 700133 'Free Spirit'), (Fate 9 700137 'The World Together'),
       (Fate 10 700152 'Cloud-Piecing Lightning'))
     sprite = @{ id = 1001; artifact = 3031101; said = 'Artifact Spirit Mengyi' }; elder = @{ id = 1; said = '' } },

  @{ file = '04-화권'; name = '화권'
     summary = '정기권으로 보호막을 쌓고 붕산권으로 보호막을 터뜨린다. 화신법과 열화봉의 열염이 그 피해를 올린다'
     source = '팬덤 위키 Fire Fist'
     skills = @((Skill 'left' 15111 @(7, 14, 16, 12, 13) 'Serene Fist'), (Skill 'right' 25114 @(9, 10, 13, 12, 8) 'Hillbreaker Fist'),
       (& $fireStep 'Fire motion skill(Illusion)'), (& $firePhoenix 'Fire Phoenix'))
     abilities = @((Ability 81001 @(9, 11, 12, 2, 4) 10006 'Wandering Cloud 2-set'), (Ability 82001 @(7, 10, 5, 3, 25) 10006 'Wandering Cloud 2-set'), (Ability 83001 @(8, 11, 6, 3, 4) 0 ''),
       (Ability 84001 @(4, 3, 2, 13, 14) 0 'Guide'), (& $generic.rok), (Ability 86007 @(4, 5, 2, 10, 6) 0 'Fire Secret Manual'),
       (Ability 87003 @(4, 2, 7, 3, 6) 0 'Sword Divine Power'), (Ability 88004 @(19, 9, 12, 17, 2) 0 'Fist Mastery'))
     fates = @((Fate 2 700017 'Water Burst'), (Fate 3 700037 'Combat Expertise Earth/Fire'), (Fate 4 700026 "Warlord's Phantom"), (Fate 5 700056 'Blood Power 1'),
       (Fate 6 700116 'Martial Skill Specialization 1-3'), (Fate 7 700133 'Free Spirit'), (Fate 8 700156 'Concealed Nirvana'), (Fate 9 700137 'The World Together'),
       (Fate 10 700183 'Dipper Tail Footwork'))
     sprite = @{ id = 1002; artifact = 3031101; said = 'Artifact Spirit Cangya' }; elder = @{ id = 2; said = 'Gouchen' }
     wingman = @{ id = 1007; effects = 100103, 100101; prefixes = 100701, 100702; said = 'Nether Feather: Blue Bird' } },

  @{ file = '05-도'; name = '도'
     summary = '열화도로 혈인을 쌓고 광혈참으로 터뜨린다. 무신잔결·살심취기와 도 신공의 치명타 배율'
     source = '무협게임 마이너 갤러리 10097'
     skills = @((Skill 'left' 12112 @(6, 8, 9, 14, 16) '열화도'), (Skill 'right' 22111 @(9, 10, 11, 12, 13) '혈참'),
       (Skill 'step' 510011 @(4, 3, 8, 10, 11, 9) ''), (Skill 'ultimate' 32111 @(8, 9, 10, 11, 13) '마살도'))
     abilities = @((Ability 81001 @(11, 12, 2, 4, 23) 10006 ''), (Ability 82001 @(5, 3, 25, 19, 2) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(3, 2, 13, 14, 16) 10013 ''), (& $generic.rok), (Ability 86001 @(2, 5, 8, 6, 4) 10013 ''),
       (Ability 87001 @(5, 4, 2, 3, 7) 10013 '도신공'), (Ability 88001 @(19, 20, 9, 12, 2) 10013 '도 대법'))
     fates = @((Fate 2 700017 '수력발산'), (Fate 3 700044 '노이미견'), (Fate 4 700026 '무신잔결'), (Fate 5 700056 '혈마대법-혈살대법-혈염대법'), (Fate 6 700158 '살심취기'),
       (Fate 7 700116 '무예입문-출중-정통'), (Fate 8 700100 '체력의 나무'), (Fate 9 700009 '근비충'), (Fate 10 700020 '미끄러운발'))
     elder = @{ id = 1; said = '' } },

  @{ file = '06-수영술'; name = '수영술 — 동결'
     summary = '수령술로 한상을 쌓고 상령폭으로 터뜨려 얼린다. 빙천안과 망해조 세트가 동결을 더한다'
     source = '무협게임 마이너 갤러리 10097'
     skills = @((Skill 'left' 42111 @(6, 9, 10, 14, 16) ''), (Skill 'right' 52115 @(10, 9, 11, 12, 8) ''),
       (Skill 'step' 510121 @(4, 6, 7, 8, 10, 11) ''), (Skill 'ultimate' 62111 @(10, 11, 8, 9, 12) ''))
     abilities = @((Ability 81001 @(5, 11, 12, 2, 4) 10006 ''), (Ability 82001 @(11, 5, 3, 25, 19) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(7, 3, 2, 13, 14) 10026 ''), (& $generic.rok), (Ability 86008 @(8, 2, 3, 12, 9) 10026 ''),
       (Ability 87008 @(3, 6, 7, 5, 2) 10026 ''), (Ability 88008 @(19, 9, 12, 15, 3) 10026 ''))
     fates = @((Fate 2 700017 '수력발산'), (Fate 3 700038 ''), (Fate 4 700100 '체력의 나무'), (Fate 5 700056 '혈마대법-혈살대법-혈염대법'), (Fate 6 700158 '살심취기'),
       (Fate 7 700113 ''), (Fate 8 700044 '노이미견'), (Fate 9 700197 ''), (Fate 10 700009 '근비충'))
     elder = @{ id = 2; said = '' } },

  @{ file = '07-창-과룡문'; name = '창 — 과룡문'
     summary = '천심모로 파세를 쌓고 질룡파로 쓴다. 창 신법을 쓰면 과룡문 세트가 환상을 불러 함께 친다. 신통은 패왕순'
     source = '무협게임 마이너 갤러리 23356'
     skills = @((Skill 'left' 13111 @(7, 11, 14, 12, 16) '천심모'), (Skill 'right' 23116 @(9, 12, 13, 10, 11) '질룡파' @(7, 6, 1, 1, 1)),
       (Skill 'step' 510021 @(4, 3, 8, 9, 10, 11) '신법(이름 없음)'), (Skill 'ultimate' 35111 @(11, 13, 9, 12, 8) '패왕순'))
     abilities = @((Ability 81001 @(11, 12, 2, 4, 23) 10006 '경(도강운)'), (Ability 82001 @(5, 3, 25, 19, 2) 10006 '공(도강운)'), (Ability 83001 @(6, 3, 4, 16, 17) 10016 '식(과룡문)'),
       (Ability 84001 @(3, 2, 13, 14, 16) 10016 '결(과룡문)'), (& $generic.rok), (Ability 86002 @(10, 3, 5, 6, 13) 10016 '비권(창)'),
       (Ability 87004 @(4, 5, 2, 3, 7) 0 '신공(권)'), (Ability 88002 @(19, 12, 8, 14, 17) 10016 '대법(창)'))
     fates = @((Fate 2 700152 '천운전섬'), (Fate 3 700034 '도수전정'), (Fate 4 700144 '답설무흔'), (Fate 5 700056 '혈마대법'), (Fate 6 700158 '살심취기'),
       (Fate 7 700155 '호백용혼'), (Fate 8 700160 '천외유천'), (Fate 9 700198 '연라환장'), (Fate 10 700184 '진천노소'))
     sprite = @{ id = 1003; artifact = 3031101; said = '주진' }; elder = @{ id = 1; said = '교룡' }
     wingman = @{ id = 1005; effects = 100101, 100105; prefixes = 100502, 100501; said = '현조' } },

  @{ file = '08-화영술'; name = '화영술 — 연화령'
     summary = '화환술로 연혼을 쌓고 이화술을 오래 유지한다. 연화령 4세트가 이화술의 재사용 대기시간을 줄인다. 신통은 열화봉'
     source = '무협게임 마이너 갤러리 22976'
     skills = @((Skill 'left' 41111 @(7, 11, 14, 12, 16) '화환술'), (Skill 'right' 51112 @(10, 12, 13, 11, 9) '이화술'), (& $fireStep '신법(그림)'), (& $firePhoenix '열화봉'))
     abilities = @((Ability 81001 @(9, 11, 12, 2, 4) 10006 '경(도강운)'), (Ability 82001 @(10, 5, 3, 25, 2) 10006 '공(도강운)'), (Ability 83001 @(6, 7, 3, 4, 16) 0 '식'),
       (Ability 84001 @(6, 3, 2, 13, 14) 10027 '결(연화령)'), (Ability 85001 @(3, 7, 2, 4, 5) 10027 '록(연화령)'), (Ability 86007 @(4, 5, 2, 10, 6) 10027 '비권(화)'),
       (Ability 87007 @(2, 3, 4, 7, 6) 10027 '신공(화)'), (Ability 88003 @(20, 9, 12, 15, 19) 0 '대법(검)'))
     fates = @((Fate 2 700152 '천운전섬'), (Fate 3 700037 '자화명이'), (Fate 4 700144 '답설무흔'), (Fate 5 700056 '혈마대법'), (Fate 6 700158 '살심취기'),
       (Fate 7 700155 '호백용혼'), (Fate 8 700160 '천외유천'), (Fate 9 700199 '신화천성'), (Fate 10 700184 '진천노소'))
     sprite = @{ id = 1003; artifact = 3031101; said = '주진' }; elder = @{ id = 2; said = '구진' }
     wingman = @{ id = 1004; effects = 100105, 100101; prefixes = 100404, 100401; said = '봉황' } },

  @{ file = '09-목화'; name = '목화 — 난방화'
     summary = '영엽술로 독침을 쌓고 독주술과 화요후가 싸운다. 난방화 4세트가 독주술과 독침을 키운다'
     source = '무협게임 마이너 갤러리 23676'
     skills = @((Skill 'left' 46111 @(6, 8, 14, 9, 16) '영엽술'), (Skill 'right' 56115 @(11, 12, 13, 10, 9) '독주술'), (& $fireStep '신법(화)'),
       (Skill 'ultimate' 66112 @(11, 9, 8, 10, 12) '화요후'))
     abilities = @((Ability 81001 @(14, 15, 11, 12, 9) 10024 '경(난방화)'), (Ability 82001 @(15, 10, 2, 5, 25) 0 '공'), (Ability 83001 @(14, 15, 6, 3, 4) 10024 '식(난방화)'),
       (Ability 84001 @(10, 11, 3, 2, 13) 10024 '결(난방화)'), (& $generic.rok), (Ability 86007 @(4, 5, 2, 10, 6) 0 '비권(화)'),
       (Ability 87004 @(4, 5, 2, 3, 7) 0 '신공(권)'), (Ability 88012 @(19, 9, 12, 17, 20) 10024 '대법(목)'))
     fates = @((Fate 2 700152 '천운전섬'), (Fate 3 700038 '수목장섕'), (Fate 4 700184 '진천노소'), (Fate 5 700056 '혈마대법'), (Fate 6 700158 '살심취기'),
       (Fate 7 700155 '호백용혼'), (Fate 8 700162 '섭신한광'), (Fate 9 700196 '위인위기'), (Fate 10 700198 '연라환장'))
     sprite = @{ id = 1003; artifact = 3031101; said = '주진' }; elder = @{ id = 2; said = '구진' }
     wingman = @{ id = 1004; effects = 100101, 100106, 100103; prefixes = 100405, 100401; said = '봉황' } },

  @{ file = '10-뇌영술'; name = '뇌영술'
     summary = '뇌광술과 영뢰술, 뇌 신법(점멸과 낙뢰), 뇌벌진. 심법은 도 대법·권 비권·검 신공으로 재사용 대기시간을 줄인다'
     source = 'gamersky 1367671, 무협게임 마이너 갤러리 10097'
     skills = @((Skill 'left' 43111 @(6, 8, 9, 14, 16) '雷光术'), (Skill 'right' 53116 @(9, 10, 11, 12, 13) '灵雷术'),
       (Skill 'step' 510131 @(4, 3, 7, 8, 9, 10) '身法：雷'), (Skill 'ultimate' 63112 @(12, 13, 8, 9, 10) '雷罚阵'))
     abilities = @((Ability 81001 @(11, 12, 2, 4, 27) 10006 ''), (Ability 82001 @(12, 5, 3, 25, 19) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(8, 3, 2, 13, 14) 0 ''), (& $generic.rok), (Ability 86004 @(5, 4, 3, 10, 2) 0 '秘卷-拳'),
       (Ability 87003 @(4, 2, 7, 3, 6) 0 '神功-剑'), (Ability 88001 @(9, 12, 15, 17, 3) 0 '大法-刀'))
     fates = @((Fate 2 700017 '수력발산(갤러리의 0티어 목록)'), (Fate 3 700036 '风雷擎天'), (Fate 4 700091 '灵能之潮123'), (Fate 5 700056 '혈마대법-혈살대법-혈염대법(갤러리의 0티어 목록)'),
       (Fate 6 700158 '살심취기(갤러리의 0티어 목록)'), (Fate 7 700061 '双灵共生'), (Fate 8 700100 '체력의 나무(갤러리의 0티어 목록)'), (Fate 9 700113 ''), (Fate 10 700152 ''))
     elder = @{ id = 2; said = '' } },

  # 11~16: 리뷰 번들 초안(2026-10-20). 근거는 docs/superpowers/specs/2026-10-20-preset-drafts-builds.md.
  @{ file = '11-지법'; name = '지법 — 핵탄'
     summary = '윤회지로 내허를 쌓고 영허폭으로 한꺼번에 터뜨린다. 지 비권·신공·대법과 어술(어검 중 무예 재사용 대기시간 감소)로 무예를 몰아 쓴다'
     source = 'gamersky 1361769'
     skills = @((Skill 'left' 17111 @(6, 8, 9, 14, 16) '轮回指'), (Skill 'right' 27113 @(12, 13, 10, 9, 8) '虚灵爆(灵虚爆)'),
       (Skill 'step' 510031 @(4, 10, 6, 9, 8, 7) '御剑 계열 신법'), (Skill 'ultimate' 37111 @(9, 12, 13, 8, 10) ''))
     abilities = @((Ability 81001 @(11, 12, 2, 4, 23) 10006 ''), (Ability 82001 @(5, 3, 25, 19, 2) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(7, 3, 2, 13, 14) 10022 ''), (& $generic.rok), (Ability 86006 @(8, 2, 6, 4, 3) 10022 '秘卷-指'),
       (Ability 87006 @(2, 3, 5, 7, 4) 10022 '神功-指'), (Ability 88006 @(3, 9, 12, 15, 17) 10022 '大法-指'))
     fates = @((Fate 2 700152 ''), (Fate 3 700034 ''), (Fate 4 700116 '普攻 빈도 증가(방향)'), (Fate 5 700056 ''), (Fate 6 700158 ''),
       (Fate 7 700156 ''), (Fate 8 700137 ''), (Fate 9 700198 ''), (Fate 10 700100 ''))
     elder = @{ id = 1; said = '' } },

  @{ file = '12-지검풍'; name = '지·검·풍 — 핵탄 무적'
     summary = '윤회지·유공폭으로 내허를 터뜨리고 풍신법(용권풍)의 무적 동안 충허지를 쓴다. 심법은 지 비권에 검 신공·검 대법'
     source = 'gamersky 1361698'
     skills = @((Skill 'left' 17111 @(6, 8, 9, 14, 16) '轮回指'), (Skill 'right' 27115 @(12, 13, 10, 9, 8) '幽空爆'),
       (Skill 'step' 510141 @(4, 3, 10, 7, 8, 11) '风 身法'), (Skill 'ultimate' 37111 @(9, 12, 13, 8, 10) '冲虚指'))
     abilities = @((Ability 81001 @(11, 12, 2, 4, 23) 10012 ''), (Ability 82001 @(5, 3, 25, 19, 2) 0 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(7, 3, 2, 13, 14) 10012 ''), (& $generic.rok), (Ability 86006 @(8, 2, 6, 4, 3) 0 '指法 心法'),
       (Ability 87003 @(4, 2, 7, 3, 6) 10012 '剑法 抗性'), (Ability 88003 @(19, 17, 9, 12, 15) 10012 '剑法会心'))
     fates = @((Fate 2 700101 '剑灵入门(筑基)'), (Fate 3 700034 ''), (Fate 4 700090 '灵能之雨→灵能之泉(结晶·金丹)'), (Fate 5 700014 '小李→中李(具灵)'), (Fate 6 700056 ''),
       (Fate 7 700158 ''), (Fate 8 700156 ''), (Fate 9 700137 ''), (Fate 10 700100 ''))
     elder = @{ id = 1; said = '' } },

  @{ file = '13-빙화도'; name = '빙화도'
     summary = '열화도에 한상폭의 동결 증폭(동결된 적 피해 증가)을 더하고, 화신법(불여우)과 열화봉으로 열염 수를 쌓아 피해를 키운다. 도 대법의 흡혈'
     source = 'gamersky 1379659'
     skills = @((Skill 'left' 12112 @(6, 8, 16, 14, 15) '烈火刀'), (Skill 'right' 52111 @(11, 13, 9, 10, 8) '寒霜爆'), (& $fireStep '火狐 身法'), (& $firePhoenix '烈火凤'))
     abilities = @((Ability 81001 @(9, 11, 12, 2, 4) 10006 ''), (Ability 82001 @(5, 3, 25, 19, 2) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(7, 3, 2, 13, 14) 10013 ''), (& $generic.rok), (Ability 86001 @(2, 5, 8, 6, 4) 10013 ''),
       (Ability 87001 @(5, 4, 2, 3, 7) 10013 ''), (Ability 88001 @(19, 9, 12, 17, 20) 10013 '刀 大法'))
     fates = @((Fate 2 700016 ''), (Fate 3 700037 '地火明夷'), (Fate 4 700116 ''), (Fate 5 700056 ''), (Fate 6 700158 ''),
       (Fate 7 700156 ''), (Fate 8 700137 ''), (Fate 9 700198 ''), (Fate 10 700100 ''))
     elder = @{ id = 1; said = '' } },

  @{ file = '14-토수'; name = '토수'
     summary = '곤암술로 토괴뢰를 부르고 열산붕을 펼쳐 아군 수를 늘린다. 화신법(불여우)의 무적으로 피하며 버티는 생존형'
     source = 'gamersky 1362955'
     skills = @((Skill 'left' 45111 @(6, 8, 9, 14, 16) ''), (Skill 'right' 55113 @(11, 12, 9, 13, 8) '滚岩术-土傀儡'), (& $fireStep '火狐(无敌身法)'), (Skill 'ultimate' 65111 @(12, 13, 10, 8, 9) '裂山崩'))
     abilities = @((Ability 81001 @(14, 15, 22, 11, 12) 10006 '대체: 经(场上每有一个己方单位回血)'), (Ability 82001 @(5, 3, 25, 19, 2) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(7, 3, 2, 13, 14) 10032 ''), (& $generic.rok), (Ability 86011 @(2, 3, 8, 12, 9) 10032 ''),
       (Ability 87011 @(3, 4, 7, 5, 6) 10032 ''), (Ability 88011 @(19, 20, 9, 12, 15) 10032 ''))
     fates = @((Fate 2 700152 ''), (Fate 3 700037 ''), (Fate 4 700100 ''), (Fate 5 700056 ''), (Fate 6 700158 ''),
       (Fate 7 700156 ''), (Fate 8 700137 ''), (Fate 9 700198 ''), (Fate 10 700160 ''))
     elder = @{ id = 2; said = '' } },

  @{ file = '15-풍수'; name = '풍수 — 오도'
     summary = '풍절술·광풍술의 선풍과 회피, 풍신법(용권풍), 풍 신통으로 거의 늘 무적인 풍 영법. 결정부터 성형되고 오도에서 회피 횟수가 늘어난다'
     source = 'gamersky 1402409'
     skills = @((Skill 'left' 44113 @(7, 11, 12, 14, 16) '风切术'), (Skill 'right' 54114 @(10, 11, 12, 8, 9) '狂风术'),
       (Skill 'step' 510141 @(4, 6, 3, 9, 8, 11) '风 身法'), (Skill 'ultimate' 64111 @(8, 12, 11, 13, 9) '대체: 逆时空'))
     abilities = @((Ability 81001 @(13, 11, 12, 2, 4) 10006 ''), (Ability 82001 @(5, 3, 25, 19, 2) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(7, 3, 2, 13, 14) 10030 ''), (& $generic.rok), (Ability 86010 @(4, 5, 2, 8, 10) 10030 '风密卷'),
       (Ability 87010 @(4, 7, 5, 3, 6) 10030 '风神功'), (Ability 88010 @(19, 9, 12, 17, 3) 10030 '风大法'))
     fates = @((Fate 2 700152 ''), (Fate 3 700036 ''), (Fate 4 700100 '体力'), (Fate 5 700056 '吸血'), (Fate 6 700097 '灵力'),
       (Fate 7 700156 ''), (Fate 8 700137 ''), (Fate 9 700198 ''), (Fate 10 700158 ''))
     elder = @{ id = 2; said = '' } },

  @{ file = '16-만신검'; name = '만신검'
     summary = '고성검으로 유혈을 쌓고 절영검으로 터뜨린다. 유혈 적을 처치해 검혼을 모아 만신검(검성 허상)을 부르고, 신통 동안 무예 재사용 대기시간이 줄고 신법을 쓰면 검기를 더 낸다'
     source = 'gamersky 1361827, 18183 4051798'
     skills = @((Skill 'left' 14112 @(6, 8, 9, 14, 16) '孤星剑'), (& $swordRight '绝影剑'), (Skill 'step' 510121 @(4, 6, 7, 8, 10, 11) '水 身法'),
       (Skill 'ultimate' 34112 @(11, 12, 13, 9, 8) '万神剑'))
     abilities = @((Ability 81001 @(11, 12, 2, 4, 23) 10006 ''), (Ability 82001 @(5, 3, 25, 19, 2) 10006 ''), (Ability 83001 @(6, 3, 4, 16, 17) 0 ''),
       (Ability 84001 @(7, 3, 2, 13, 14) 10012 ''), (& $generic.rok), (Ability 86003 @(8, 2, 6, 4, 3) 10012 '剑系 灵根抗性 心法'),
       (Ability 87003 @(4, 2, 7, 3, 6) 10012 '剑系 功法抗性 心法'), (Ability 88001 @(12, 9, 17, 19, 20) 0 '刀 会心 心法'))
     fates = @((Fate 2 700017 '水力四射'), (Fate 3 700035 '兵刃专精'), (Fate 4 700116 '武法入门-进阶-精通'), (Fate 5 700056 ''), (Fate 6 700091 '灵能之雨-之泉-之潮'),
       (Fate 7 700156 ''), (Fate 8 700137 ''), (Fate 9 700198 ''), (Fate 10 700158 ''))
     elder = @{ id = 1; said = '' } }
)

# said가 '대체:'로 시작하면 원문의 이름이 PC판에 없어 바꾼 줄이다(스펙 2026-10-20-preset-drafts-design.md 6.1절). 채운 줄처럼 filled를 단다.
function Flag($said) { if ($said -and -not $said.StartsWith('대체:')) { '' } else { ', "filled": true' } }
function Mark($said) { if (-not $said) { '채움 | ' } elseif ($said.StartsWith('대체:')) { "대체 | $($said.Substring(3).Trim())" } else { "있음 | $said" } }
$slotName = @{ left = '무예/영술'; right = '절기'; step = '신법'; ultimate = '신통' }
$tables = New-Object Text.StringBuilder
foreach ($b in $builds) {
  $json = New-Object Text.StringBuilder
  [void]$json.AppendLine('{').AppendLine('  "format": 1,').AppendLine('  "kind": "preset",')
  [void]$json.AppendLine(('  "name": "{0}",' -f $b.name)).AppendLine(('  "summary": "{0}",' -f $b.summary)).AppendLine(('  "source": "{0}",' -f $b.source))
  [void]$tables.AppendLine("### $($b.file.Substring(0, 2)). $($b.name)").AppendLine().AppendLine("출처: $($b.source). 파일: ``$($b.file).json``").AppendLine()
  [void]$tables.AppendLine('| 칸 | 게임의 번호와 이름 | 공략 | 공략의 표기 | 옵션 |').AppendLine('|---|---|---|---|---|')
  if ($b.born) {
    $rows = foreach ($l in $b.born) { '    {{ "id": {0}, "name": "연화검의"{1} }}' -f $l.id, (Flag $l.said); [void]$tables.AppendLine("| 선천운명 | $($l.id) 연화검의 | $(Mark $l.said) | |") }
    [void]$json.AppendLine('  "bornLucks": [').AppendLine(($rows -join ",`n")).AppendLine('  ],')
  }
  $rows = foreach ($l in $b.skills) {
    $n = $skillName["$($l.id)"]; if (-not $n) { throw "기술 $($l.id)의 이름을 찾지 못했습니다" }
    [void]$tables.AppendLine("| $($slotName[$l.slot]) | $($l.id) $n | $(Mark $l.said) | $($l.options -join ' ') |")
    '    {{ "slot": "{0}", "baseId": {1}, "name": "{2}", "options": [{3}]{4} }}' -f $l.slot, $l.id, $n, ($l.options -join ', '), (Flag $l.said)
  }
  [void]$json.AppendLine('  "skills": [').AppendLine(($rows -join ",`n")).AppendLine('  ],')
  $rows = foreach ($l in $b.abilities) {
    if (-not $skillName["$($l.id)"]) { throw "심법 $($l.id)를 찾지 못했습니다" }
    $n = AbilityName $l.id
    $suit = if ($l.suit) { ', "suit": {0}' -f $l.suit } else { '' }
    $suitText = if ($l.suit) { " [$($suitName[$l.suit]) $($l.suit)]" } else { '' }
    [void]$tables.AppendLine("| 심법 | $($l.id) $n$suitText | $(Mark $l.said) | $($l.options -join ' ') |")
    '    {{ "baseId": {0}, "name": "{1}", "options": [{2}]{3}{4} }}' -f $l.id, $n, ($l.options -join ', '), $suit, (Flag $l.said)
  }
  [void]$json.AppendLine('  "abilities": [').AppendLine(($rows -join ",`n")).AppendLine('  ],')
  $rows = foreach ($l in $b.fates) {
    $n = $fateName["$($l.id)"]; if (-not $n) { throw "역천개명 $($l.id)의 이름을 찾지 못했습니다" }
    [void]$tables.AppendLine("| 역천개명 $($l.grade) | $($l.id) $n | $(Mark $l.said) | |")
    '    {{ "grade": {0}, "id": {1}, "name": "{2}"{3} }}' -f $l.grade, $l.id, $n, (Flag $l.said)
  }
  [void]$json.Append('  "fates": [').AppendLine().Append(($rows -join ",`n")).AppendLine().Append('  ]')
  if ($b.sprite) {
    [void]$json.AppendLine(',').Append(('  "sprite": {{ "id": {0}, "artifact": {1} }}' -f $b.sprite.id, $b.sprite.artifact))
    [void]$tables.AppendLine("| 기령 | $($b.sprite.id) $($spriteName[$b.sprite.id]) | $(Mark $b.sprite.said) | |").AppendLine("| 법보 | $($b.sprite.artifact) 황염호각 | 채움 | | |")
  }
  if ($b.elder) {
    [void]$json.AppendLine(',').Append(('  "elder": {{ "id": {0}{1} }}' -f $b.elder.id, (Flag $b.elder.said)))
    [void]$tables.AppendLine("| 신혼 | $($b.elder.id) $($elderName[$b.elder.id]) | $(Mark $b.elder.said) | |")
  }
  if ($b.wingman) {
    [void]$json.AppendLine(',').Append(('  "wingman": {{ "id": {0}, "effects": [{1}], "prefixes": [{2}] }}' -f $b.wingman.id, ($b.wingman.effects -join ', '), ($b.wingman.prefixes -join ', ')))
    [void]$tables.AppendLine("| 명우 | $($b.wingman.id) $($wingName[$b.wingman.id]) | $(Mark $b.wingman.said) | 효과 $($b.wingman.effects -join ' '), 옵션 $($b.wingman.prefixes -join ' ') |")
  }
  [void]$json.AppendLine().AppendLine('}')
  [void]$tables.AppendLine()
  [IO.File]::WriteAllText((Join-Path $out "$($b.file).json"), ($json.ToString() -replace "`r", ''), (New-Object Text.UTF8Encoding $false))
}
if ($TablesTo) { [IO.File]::WriteAllText($TablesTo, ($tables.ToString() -replace "`r", ''), (New-Object Text.UTF8Encoding $false)) }
"written=$($builds.Count)"
