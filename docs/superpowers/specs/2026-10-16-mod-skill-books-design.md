# 모드 공법의 비급(G2b) 설계 (2026-10-16)

## 1. 목적

사용자 요청(2026-10-15, 요청 4의 "공법 모드의 경우 이 모드에서 해당 공법 문서 획득 여부")의 두 번째 묶음. 창작마당 공법 모드가 더한 공법을 자원 탭의 기술 서적 그룹(05 문서 39절)에서 보이고 비급으로 지급한다.

사용자가 고른 것(2026-10-16):

- 범위: 지급(바로 배우기는 하지 않는다. 표시만도 아니다).
- 화면: 같은 목록에 섞여 나오고 분류 열에 `[모드]` 꼬리표, 종류 칩에 `모드만`.
- 경지: 모드 공법은 경지 범위가 0~0이므로 1~10을 자유로 고르고 처음은 지금 경지(기존 경지 단추 그대로).
- 신통: 이 모드의 신통은 게임이 값을 읽지 못하므로(2절) 지급을 막고 이유를 보인다.

## 2. 실측 (회차 `ws-1016a`~`g`, 고정본, 실험 브랜치 `feat/modskill-research`. 인계 8.3e절)

| 항목 | 실측 |
|---|---|
| 모드 공법의 자리 | 공법 모드 `3390738990`의 공법 다섯이 `g.conf.battleSkillAttack._allConfList`에 행으로 있다(기본 1568 + 5). `name`은 LocalText 키(`skill_name_middle<번호>`), `gradeMin`·`gradeMax`는 0, `skillID`는 `id`와 같다. 번호·종류: 59898952(4 신통, 千杀神枪), 984555698(3 절기, 烈炎掌), 353535(3 절기, 藏气狂刀), 3535351(1 무예, 赤炎剑, `basType` basSword), 844854565(3 절기, 风旋龙卷) |
| 모드 행의 표시 | `ConfBaseItem.isModExtend`가 다섯 모두 `True`, 기본 행(11412, 23116)은 `False`(회차 `ws-1016g`) |
| 왜 목록에 없나 | `SkillCatalog.Add`가 `gradeMax < 1`이거나 이름 키가 풀리지 않으면 뺀다 |
| 이름 | 모드 LocalText 행은 `ch`만 있고 `kr`은 글자 "0". `GameTool.LS(키)`가 "0"을 돌려주고 게임 화면도 "0". `g.conf.localText.allConfList`의 그 키 행에서 `ch`를 읽을 수 있다. 실행 중 그 행의 `kr`을 덮으면 `LS`가 바로 바뀐다(G2c의 길) |
| 비급 값의 모양 | 게임이 뽑은 비급 값은 `[기술 번호, 경지, …옵션…, -1, …]`(0번이 기술 번호). 0번만 모드 번호로 바꿔 `PropsData.NewMartial`로 만들면 절기 셋은 6/6 가방에 들어가고 이름·품질이 읽히며 학습이 받아들인다(`IsCreateAction` 0). 무예는 뽑힌 옵션 조합에 따라 4번 중 1번 읽혔다(읽힌 것은 정상. `can=5`는 이 고정본의 기본 무예 비급도 같다). 신통은 원본을 바꿔도 3/3 값을 읽지 못한다(NullReferenceException) |
| 읽기의 때 | `NewMartial(...).To<MartialData>()`는 가방에 넣기 전에도 된다(`RollValues`의 `Accepts`가 그렇게 쓴다). 실패해도 세이브에 흔적이 없다 |
| 평범한 값 | `[2,0,1,종류,번호,경지]`는 비급 값의 모양이 아니다(기본 기술에서도 깨진다). 배우기 전용 |
| DLL 모드 | `2839261049`(传承心法)는 기술 표에 행을 더하지 않는다. 이 설계의 대상이 아니다 |

## 3. 결정

### 3.1 모드 공법 수집 (`SkillCatalog`)

- `Entry`에 `IsMod`(bool)와 `BasType`(string. `battleSkillAttack` 행의 `basType`, 신법·심법은 "")를 더한다.
- `Add`: `item.isModExtend`가 참이면 경지 범위가 0~0이어도 넣고 `IsMod = true`, `GradeMin = 1`, `GradeMax = 10`으로 둔다(비급의 경지는 1~10 자유). 기본 행은 지금 규칙 그대로(`gradeMax < 1`이거나 이름이 풀리지 않으면 뺀다).
- 이름 풀기 `ResolveText(key)`: `GameTool.LS(key)`가 null·빈 글·"0"·키 그대로이면 `g.conf.localText.allConfList`에서 그 키 행을 찾아 `ch`가 비어 있지 않으면 `ch`, 그것도 없으면 키를 쓴다. 행 찾기는 처음 한 번 `Dictionary<string, string>`(키 → ch)으로 만들어 둔다(`allText.TryGetValue`는 Il2Cpp 제네릭 호출에서 `MissingMethodException`이 났다. 실측). `Name`·`Desc` 모두 이 함수로 푼다. `Search`에는 푼 이름과 `ch`를 함께 넣어 중국어로도 찾아진다.
- `Entry.NameFromCh`(bool): 이름이 `ch`로 대신 채워졌는지. 화면 안내에 쓴다.
- G2c가 나중에 `kr`을 덮으면 `LS`가 한글을 돌려주므로 그대로 따라온다. 목록 캐시(`cache`)는 세션 동안 유지되므로, G2c는 덮은 뒤 캐시를 비우는 함수(`SkillCatalog.Reset()`)를 부른다(이 설계에서 함수만 둔다).
- `BlockReason(entry, grade)`: `IsMod`이고 분류가 신통(순번 4)이면 "이 모드의 신통은 줄 수 없습니다. 게임이 값을 읽지 못합니다(실측)"; `IsMod`이면 경지 1~10 밖만 막는다; 기본 행은 지금 규칙.

### 3.2 치환 뽑기 (`SkillCatalog.RollValuesForMod`)

- 원본 고르기: 같은 분류의 기본 항목(`IsMod`가 아닌 것) 가운데 고른 경지를 범위에 포함하는 것. 무예·영술(순번 0)은 같은 `BasType`을 먼저, 없으면 아무것이나. 둘 이상이면 자기 묶음(`OwnCombine != 0`)이 있는 것을 먼저 쓴다(뽑기가 한 번에 끝난다). 원본이 없으면 null과 이유 "원본으로 쓸 기본 공법이 없습니다".
- 한 번 시도: `RollValues(kind, 원본.BaseId, grade, RollMax, null)`로 뽑은 값을 복사해 0번을 모드 번호로 바꾸고, `DataProps.PropsData.NewMartial(kind.Type, values).To<DataProps.MartialData>()`로 읽어 `martial.baseID == 모드 번호`, 이름이 비어 있지 않음, `accept`(최소 품질)가 참이면 받아들인다. 예외가 나거나 조건이 틀리면 버리고 다시 뽑는다. 최대 50번(무예 실측 성공률 1/4. 원본이 자기 묶음이 있으면 한 번에 한 번의 뽑기라 50번도 1초 안쪽이고, 없으면 한 번에 최대 3000번 뽑기라 길어질 수 있다).
- 돌려주는 것: 값 배열 또는 null과 시도 수. `MartialBookService.Make`는 `entry.IsMod`이면 이것을 쓰고, null이면 "모드 공법의 값을 게임이 읽지 못했습니다(n회 시도)"로 그 권부터 멈춘다(기존 "경지 n에서 품질 m 이상이 나오지 않았습니다"와 같은 자리).
- 그 뒤는 기존과 같다: 가방에 넣고 수를 다시 읽어 판정, 변경 이력(이름에 ` [모드]`를 붙인다), 결과 문장. 결과 문장과 이력의 이름은 카탈로그의 푼 이름(`ch`)을 쓰고, 게임이 보이는 비급 이름(`martialInfo.name`)은 G2c 전까지 "…0"일 수 있다는 것을 아래 칸 안내에 적는다.

### 3.3 화면 (기술 서적 그룹, 05 문서 39절)

- 종류 칩: `전체`, 다섯 분류 뒤에 `모드만 (n)`(`ResKind6`). 고르면 모든 분류의 `IsMod` 항목만 보인다. 개수는 지금 검색어를 반영한다(`이 경지만`은 모드 항목을 거르지 않는다).
- 줄: 분류 열에 `절기 [모드]`처럼 꼬리표, 경지 범위 열은 `모드`(1~10). 이름은 푼 이름. 배움·가방·☆·지급은 그대로. 신통 모드 줄은 지급이 꺼지고 누르면 아래 칸에 이유.
- 아래 칸: 기존 첫 줄 뒤에 `[모드]`를 붙이고, `NameFromCh`이면 "한글 이름이 없어 중국어 이름을 보입니다(게임 화면과 비급 이름에는 0으로 보일 수 있습니다. 한글화는 G2c)."를, 그 다음 설명, 그리고 "모드 공법: 같은 종류의 기본 공법을 뽑아 번호만 바꿔 만듭니다. 옵션은 게임이 뽑은 것입니다." 신통이면 `BlockReason`의 문장.
- `이 경지만`(`ResBookFit`)의 판정 `BookShown`은 `IsMod`이면 참(막힌 신통 줄도 보여 이유를 읽을 수 있게. 기본 항목은 지금처럼 `BlockReason == null`).

### 3.4 즐겨찾기·이력

- 비급 즐겨찾기 줄(`book <분류> <번호> <경지> <최소 품질> <수량>`)은 모드 번호도 같은 형식. `SkillCatalog.Get`이 모드 항목을 돌려주므로 보이고 지급된다. 모드를 뺀 뒤에는 `Get`이 null이라 기존 "게임에 없는 기술" 처리(필터와 상관없이 보이고 지급이 꺼지고 ★로 뺄 수 있다).
- 변경 이력: `비급 <이름> [모드](<번호>)`, 이전 `-`, 이후 `경지 n 품질 m`.

### 3.5 요소 이름

기존 그대로: `ResKind0`~`5`, `ResGrade1`~`10`, `ResMinLevel1`~`6`, `ResBookFit`, `BookStar|BookName|BookGive|BookBag<번호>`, `FavBook*`. 새것: `ResKind6`(모드만). 모드 번호는 크지만 앞머리가 다른 번호와 겹치지 않는다(`BookGive59898952` 등).

## 4. 코드

| 파일 | 일 |
|---|---|
| `src/ModCode/ModMain/Services/SkillCatalog.cs` | `Entry.IsMod`·`BasType`·`NameFromCh`, `Add`의 모드 규칙, `ResolveText`(키 → ch 사전), `Reset()`, `BlockReason`의 모드 규칙, `RollValuesForMod`, 원본 고르기 |
| `src/ModCode/ModMain/Services/MartialBookService.cs` | `Make`가 `IsMod`이면 `RollValuesForMod`를 쓰고 멈춤 문장, 이력 이름에 ` [모드]` |
| `src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs` | `ResKind6` 칩과 필터, 분류·경지 열, `BookShown`, 아래 칸 안내 |
| `src/ModCode/ModMain/Dev/DevCommandsResources.cs` | `rmods`: 카탈로그의 모드 항목(분류:번호:이름:basType:NameFromCh:막힘 이유)과 고른 원본. `rbook`·`rskill`·`rbooks`·`rbookstudy`는 그대로 모드 번호에 쓴다 |
| `tools/research/ui-check.ps1` | 부분 `modbooks` |
| `docs/05-editor-v0.1.md` 42절, `docs/06-handoff.md` | 6절 |

실험 브랜치 `feat/modskill-research`의 명령(`wsconf`, `wsbookas` 등)은 가져오지 않는다.

## 5. 검증

회차 `ws-1016h`부터(고정본. 설치된 창작마당 모드 15개는 회차 전부터 있던 것이라 그대로 로드된다). 인물은 결정 초기(대경지 3).

- `rmods`: 모드 항목 5개(절기 3, 무예 1, 신통 1), 이름이 `ch`(千杀神枪·烈炎掌·藏气狂刀·赤炎剑·风旋龙卷), `NameFromCh` 참, 신통만 막힘 이유, 무예의 원본이 `basSword`인 기본 무예.
- `rskill 1 984555698` → `min=1 max=10`. `rskill 4 59898952` → 있음.
- `rbook 1 984555698 3 1 1`, `rbook 1 353535 3 1 1`, `rbook 1 844854565 3 1 1` → 각각 `made=1`, `rbooks <번호>`에 한 권, `rbookstudy <soleID>`가 `can=0`. `rbook 1 984555698 1 1 1`과 `… 10 1 1`(다른 경지)도 `made=1`. `rbook 1 984555698 3 6 1`(최소 품질 6)도 `made=1`(품질 6).
- `rbook 0 3535351 3 1 1` → `made=1`(재시도 안에서), `rbookstudy`가 기본 무예 비급과 같은 값(5). 세 번 되풀이해 모두 `made=1`.
- `rbook 4 59898952 3 1 1` → `made=0`과 신통 막힘 문장.
- `ui-check.ps1 -Part modbooks`: 기술 서적 그룹에서 `ResKind6`을 누르면 5줄, 칩 글이 `모드만 (5)`, 분류 열에 `[모드]`, 경지 열 `모드`, `BookGive59898952`가 꺼짐, `BookGive984555698` 지급 뒤 가방 열 1, 아래 칸에 중국어 안내 문장, 즐겨찾기 `rfav addbook 1 984555698 3 1 1` 뒤 즐겨찾기 줄과 `모두 지급`으로 한 권 더. 검색 "烈炎"으로 1줄.
- 기존 `books`·`books-ui`·`favorites`·`groups`가 그대로 통과.
- 두 빌드 구성 경고 0·오류 0.

## 6. 문서

- 05 문서 42절 "모드 공법의 비급(G2b)": 규칙, 실측 요약(인계 8.3e절 참조), 검증 결과, 확인하지 못한 것.
- 인계: 1절 상태·다음 일(G2c), 5절 도구에 `rmods`와 `modbooks`, 8.3b절에 G2b 끝남, 8.5절에 "모드 공법 비급을 게임에서 학습해 전투에서 썼을 때", 8.7절 회차 이름.

## 7. 범위 밖

- 바로 배우기(공법 탭). 모드 공법의 옵션을 모드의 `BattleSkillPrefixValue`에서 고르는 것(지금은 원본의 옵션을 그대로 쓴다).
- 한글화(G2c): 이름·설명의 `kr` 덮기. 이 설계는 `ch` 대체와 `Reset()`만 둔다.
- 다른 공법 모드(신통이 되는 모드가 있을 수 있다. 실측은 `3390738990`뿐).
- DLL 모드 `2839261049`.

## 8. 확인하지 못한 것, 위험

- 무예의 성공률 1/4은 뽑기 4번의 결과다. 50번 안에 안 되는 날이 있을 수 있다(그때는 멈춤 문장).
- 경지 3 밖의 경지와 최소 품질 6은 5절에서 처음 본다.
- 만든 비급을 게임에서 학습해 전투에서 쓸 때의 동작은 사용자 확인 항목(모드 공법의 옵션이 원본 기술의 것이라 어색할 수 있다).
- 모드를 뺀 세이브에 남은 비급은 게임이 깨진 아이템으로 다룰 수 있다(게임 자체의 문제. 안내 문장만).
- `isModExtend`는 이 모드의 행에서만 봤다. 다른 모드가 기본 행을 덮어쓴 경우(`isVariant`?)의 값은 미확인.
- 50번 시도의 시간: 원본에 자기 묶음이 없으면 한 번에 최대 3000번 뽑기라 수십 초가 될 수 있다. 원본 고르기가 자기 묶음이 있는 것을 먼저 쓰므로 보통은 짧다.
