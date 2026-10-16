# 창작마당 모드 확장(W: 검색·등록부·컬렉션) 설계 (2026-10-16)

## 1. 목적

사용자 요청(2026-10-16) 여섯 항목 가운데 셋을 한 묶음(W)으로 다룬다.

- 2 "스팀 창작마당에서 모드 추가 검색 및 `E:\SteamCmd`를 통한 추가 다운로드 관리"
- 3 "앞 세션 및 이번 세션 다운로드받은 모드는 문서로 확실히 관리할 것"
- 5 "가능하면 스팀 귀곡팔황에 다운받은 모드 모음집 만들어 추가 및 관리"

나머지는 따로 간다: 1 전체 리뷰(R. 별도 세션 `editor-review`, 보고서 `docs/08-review-2026-10-16.md`), 4 추가 모드 한글화(K. 이 묶음의 설치가 끝난 뒤 별도 스펙), 6 기타 검토(X. R의 결과와 인계 8.3a·8.5·8.6절을 모아 고른다).

사용자가 고른 것(2026-10-16):

- 순서: W를 먼저 설계·구현하고 R은 병렬로 돌린다. K는 설치 뒤, X는 R 뒤.
- 더 찾을 종류: 외형·공법(새 공법을 더하는 것), 음악·연출, 세계관·NPC·이야기. 편의·시스템 모드는 찾지 않는다.
- 선정: 도구가 종류마다 구독자 상위 20개 후보표를 만들고, 제가 설명을 읽어 한국어 요약·추천을 붙이고, 사용자가 고른다.
- 컬렉션: 사용자가 Chrome에서 Steam에 로그인해 두면 제가 Claude in Chrome으로 만들고 항목을 넣는다. 가시성은 비공개 링크(Unlisted).
- 접근 방식: 지금 도구 `tools/workshop.ps1`을 확장한다(검색·문서 생성·컬렉션 비교). `mods.txt`를 상태 열이 있는 단일 원본으로 두고 등록부 문서는 도구가 만든다. Python 이식이나 컬렉션을 단일 원본으로 두는 안은 버렸다.

## 2. 실측 (2026-10-16, 이 PC에서 읽기만. 로그인·구독·내려받기 없음)

| 항목 | 실측 |
|---|---|
| 검색 페이지 | `https://steamcommunity.com/workshop/browse/?appid=1468810&browsesort=totaluniquesubscribers&actualsort=totaluniquesubscribers&section=readytouseitems&numperpage=30&p=<쪽>`에 `searchtext=<낱말>` 또는 `requiredtags[]=<태그>`. HTTP 200, 한 쪽에 ID 30개(`sharedfiles/filedetails/?id=<ID>` 링크를 순서대로 모으면 된다). 제목은 이 페이지에서 읽지 않는다(상세 API가 준다) |
| 게임 고유 태그 | 페이지 안 JSON에서 읽은 이름과 중국어 표시: `Luck` 气运, `Drama` 剧情, `NPC`, `Battle` 战斗, `Function` 功能, `LargeIntegrated` 大型综合, `Item` 道具, `Beautify` 美化, `OpenSource` 开源, `English`. 나머지는 Steam 공통 태그(언어, Characters 등) |
| 상세 API | `POST https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/` (`itemcount`, `publishedfileids[n]`). 키 없이 된다. 돌려주는 것: `publishedfileid`, `title`(서식 태그 `<color=…>`가 섞일 수 있다), `file_size`, `time_updated`, `time_created`, `subscriptions`, `lifetime_subscriptions`, `favorited`, `views`, `tags[].tag`, `description`(전문), `visibility`(0이 공개), `banned`, `result`(1이 정상), `creator`, `preview_url` |
| 필수 항목 | 항목 페이지 `https://steamcommunity.com/sharedfiles/filedetails/?id=<ID>`의 `<div … id="RequiredItems">` 블록 안에 `filedetails/?id=<ID>` 링크와 `<div class="requiredItem">이름</div>`. 2815562168에서 `2814703549 Fatury`를 읽었다. 02a 조사표 9절의 "필수 아이템 미확인"은 이 방법으로 풀린다 |
| 컬렉션 조회 | `POST …/ISteamRemoteStorage/GetCollectionDetails/v1/` (`collectioncount`, `publishedfileids[0]`). 키 없이 된다. `collectiondetails[0].children[].publishedfileid`. 3341173894(공개, 76개)로 확인 |
| 컬렉션 만들기 | `https://steamcommunity.com/sharedfiles/managecollection/?appid=1468810`은 로그인 없이는 "Steam Community :: Error" 페이지. 만들기·편집은 로그인한 브라우저에서만 된다. 컬렉션을 만들거나 고치는 공개 Web API 메서드는 찾지 못했다(미확인. 이 설계는 브라우저로만 쓴다) |
| 요청 제한 | 02a 조사(2026-10-12)에서 연속 요청이 "您最近作出的请求太多了"로 막힌 적이 있다. 오늘의 요청 수(검색 1, 상세 1, 항목 1, 컬렉션 1, 관리 1)로는 막히지 않았다 |
| 이 PC의 Python | `python`은 Windows 스토어 스텁이라 실행되지 않는다. `py -3 -I -X utf8`로 돈다(인계 6절). 도구는 PowerShell로만 쓴다 |
| 받아 둔 것 | `E:\SteamCmd\steamapps\workshop\content\1468810\`에 17개(목록 15 + 제외 2945768219 + 시험용 3681025074). `appworkshop_1468810.acf`의 `WorkshopItemsInstalled`에 `size`·`timeupdated`·`manifest` |
| 앞 세션의 자동 목록 | `D:\Download\output\mod_list.md`(2026-10-15, 외형 15·공법 14, 점수 = 누적 구독 + 5×즐겨찾기). 저장소에 없다 |

## 3. 결정

### 3.1 목록 파일 `tools/workshop/mods.txt` (단일 원본)

한 줄에 일곱 칸. `#` 줄은 주석. 줄 순서가 설치 순서(Fatury 첫 줄).

```
창작마당ID | 상태 | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 비고
```

- 상태: `설치`(받고 설치한다), `보류`(받아 두되 설치하지 않는다), `제외`(받지도 설치하지도 않는다). 그 밖의 값은 읽기에서 거부한다. 지금 주석으로 둔 두 줄을 정식 줄로 올린다: `2945768219 | 제외 | 외형 | 【执余】女NPC颜值评定优化 | [执余] 여성 NPC 외모 평가 개선 | - | MySql.Data.dll이 들어 있어 제외(2026-10-15)`, `3681025074 | 보류 | - | - | - | - | 사용자 시험용. 목록에 넣지 않기로 함(2026-10-15)`. `보류` 줄은 종류·원문 이름·한국어 이름에 `-`를 허용한다(`설치`·`제외` 줄은 셋 다 비울 수 없다).
- 종류: `틀`·`외형`·`공법`·`세계관`(NPC·이야기 포함)·`음악`(연출 포함)·`-`(보류 줄만). 그 밖의 값은 거부한다.
- 모드ID: 게임이 읽은 `soleID`(05 문서 41절 표. 예: 2814703549 → `q9CYCo`). 새 줄은 검증 회차의 `wsmods` 뒤에 채운다. 모르면 `-`.
- 비고: 필수 항목("Fatury 필요"), DLL 이름, 제외 사유 등 자유 글.
- 도구의 줄 고르기: `-Status`·`-Doc`은 모든 줄. `-Download`는 `설치` 줄 가운데 받지 않은 것(`-Id`로 지정하면 `설치`·`보류` 줄. `제외`는 "제외한 ID입니다"로 거부). `-Install`은 `설치` 줄(`-Id`로 지정해도 `보류`·`제외`는 거부). `-Uninstall`은 상태와 무관하게 줄에 있는 ID. 목록에 없는 ID는 지금처럼 거부.

### 3.2 이력 파일 `tools/workshop/history.txt`

도구가 성공한 동작마다 한 줄을 덧붙인다(UTF-8, BOM 없음). 사람은 고치지 않는다. git에 넣는다.

```
yyyy-MM-dd HH:mm | 동작 | 창작마당ID | 한국어 이름
```

- 동작: `받음`(`-Download`의 `OK`), `설치`(`-Install -Yes`의 `INSTALLED`), `갱신`(`UPDATED`), `지움`(`-Uninstall -Yes`의 `REMOVED`). `UNCHANGED`·계획만 보기·실패는 남기지 않는다.
- `-NoHistory` 스위치가 있으면 쓰지 않는다. 검증 회차 안의 설치(`-End`가 되돌린다)와 시험이 쓴다. 매개변수 `-History <경로>`(기본 `tools\workshop\history.txt`)는 시험용.
- 앞 세션의 것을 소급해 적는다(05 문서 41절): 2026-10-15 `받음` 16줄(목록 15 + 2945768219), 2026-10-16 `설치` 15줄. 시각을 모르므로 `--:--`로 적고(`2026-10-15 --:--`), `-Doc`은 이 모양도 읽는다. 3681025074는 사용자가 받은 것이라 적지 않는다.

### 3.3 검색 `-Search`

```
pwsh tools/workshop.ps1 -Search [-Kind 외형,공법,세계관,음악] [-Top 20] [-Delay 1.5] [-Out <파일>] [-WebRoot <폴더>]
```

- 종류마다 질의(검색 페이지, 구독자순, 한 쪽 30개):

| 종류 | 질의 | 도구가 붙이는 표시 |
|---|---|---|
| 외형 | `requiredtags[]=Beautify` | — |
| 공법 | `requiredtags[]=Battle`, 그리고 `searchtext=功法` | 제목이나 설명에 `升级|升級|合成|分解|融合|经验|經驗|速度|修改器|工具|框架|UI|自动|自動|参悟|參悟|秘籍|秘笈`가 있으면 `시스템?` |
| 세계관 | `requiredtags[]=Drama`, `requiredtags[]=NPC`, `requiredtags[]=LargeIntegrated` 각각 | 태그에 `LargeIntegrated`가 있으면 `대형` |
| 음악 | `searchtext=音乐`, `searchtext=BGM`, `searchtext=特效` 각각 | — |

- 모으기: 질의마다 1쪽부터 읽어 ID를 순서대로 모은다. 종류 안에서 질의가 여럿이면 순서대로 잇고 같은 ID는 한 번만. `mods.txt`에 있는 ID는 후보로 세지 않고 `목록에 있음(상태)`로만 표시한다. 다른 종류에서 이미 후보가 된 ID는 뒤 종류에서 뺀다. 후보가 `-Top`개가 되면 그 종류의 읽기를 멈춘다. 쪽이 비거나 질의마다 5쪽을 넘기면 멈춘다. 요청 사이에 `-Delay`초 쉰다.
- 상세: 모은 ID를 30개씩 상세 API로 조회한다. `result`가 1이 아니거나 `banned`가 참이거나 `visibility`가 0이 아니면 뺀다.
- 필수 항목: 후보마다 항목 페이지를 한 번 읽어 `RequiredItems` 블록의 ID와 이름을 적는다. 블록이 없으면 `-`. 읽지 못하면 `조회 실패`.
- 멈춤: HTTP 429나 응답 본문에 `请求太多`가 있으면 그 자리에서 멈추고, 그때까지 모은 것으로 파일을 쓰고 마지막 줄에 "요청 제한으로 멈춤: <질의> <쪽>"을 적는다. 그 밖의 HTTP 오류는 그 요청만 건너뛰고 표에 `조회 실패`.
- 표시(경고일 뿐 빼지 않는다): `성인향?`(제목에 `双修|雙修|采补|採補|裸|R18|NSFW|涩|澀|绅士|紳士|魅魔|合欢|合歡|处子|處子`), `오래됨`(`time_updated`가 2023-01-01 이전), `큼`(`file_size`가 200MB 초과), `시스템?`, `대형`, `목록에 있음(상태)`.
- 결과 파일: 기본 `tools/workshop/candidates-<yyyyMMdd>.md`(git에 넣는다. 같은 날 다시 돌리면 덮는다). 머리에 생성 시각·질의 조건·`-Top`. 종류마다 표 하나:

```
순위 | ID | 제목 | 구독(누적) | 즐겨찾기 | 갱신일 | 크기 | 태그 | 필수 항목 | 표시 | 한국어 요약 | 추천
```

제목·설명의 서식 태그(`<color=…>`, `</color>`, `<size=…>`, `<i>` 등)는 지운다. 설명은 표에 넣지 않고(표가 넓어진다) 종류 표 아래 "설명" 소절에 `ID: 앞 200자`로 한 줄씩 둔다. `|`는 `｜`로, 줄바꿈은 공백으로. `한국어 요약`·`추천` 열은 비워 둔다(3.5절에서 사람이 채운다).
- 설명 전문: 같은 실행이 `tools/workshop/candidates-<yyyyMMdd>-desc.txt`에 `ID<TAB>설명 전문`(줄바꿈은 `\n` 두 글자)을 쓴다. 요약을 쓸 때 읽는 작업 파일이고 git에는 넣지 않는다(`.gitignore`).
- `-WebRoot <폴더>`(시험용): 네트워크 대신 폴더의 파일을 읽는다. 검색 페이지는 `browse-<태그 또는 검색어>-p<쪽>.html`, 상세는 `details.json`(배열 하나에 모든 항목. 요청한 ID만 골라 쓴다), 항목 페이지는 `item-<ID>.html`, 컬렉션은 `collection.json`. 없는 파일은 그 요청의 실패로 다룬다.

### 3.4 등록부 문서 `-Doc`

```
pwsh tools/workshop.ps1 -Doc [-WebRoot <폴더>] [-DocOut <파일>]
```

`docs/09-workshop-mods.md`를 통째로 다시 쓴다(git에 넣는다). 머리: "이 문서는 `pwsh tools/workshop.ps1 -Doc`이 만든다. 손으로 고치지 않는다. 고치려면 `tools/workshop/mods.txt`·`history.txt`를 고친 뒤 다시 돌린다", 생성 시각, `-GameDir`, `-SteamCmd`.

| 절 | 내용 | 출처 |
|---|---|---|
| 1 요약 | 상태별 개수(설치·보류·제외), 받음 a/n·설치 b/n(`설치` 줄 기준), 설치본 크기 합(MB), DLL이 든 설치 모드 수 | 목록, 폴더 |
| 2 모드 표 | 목록 순서대로 `ID \| 상태 \| 종류 \| 원문 이름 \| 한국어 이름 \| 모드ID \| 받음 \| 설치 \| 설치본 \| DLL \| 비고 \| 링크`. 받음은 `<MB> <갱신일>` 또는 `-`, 설치는 `예`/`-`, 설치본은 `같음`/`다름 n`/`-`, DLL은 이름을 `,`로, 링크는 창작마당 주소 | 목록, `-Status`와 같은 계산(`Downloaded`·`Source`·`SizeMb`·`Updated`·`DiffCount`·`Dlls`) |
| 3 이력 | `history.txt` 전체를 최근 것부터 표로 | 이력 파일 |
| 4 컬렉션 비교 | `tools/workshop/collection.txt`(ID 한 줄)가 있으면 컬렉션 API로 항목을 읽어 "일치 n", "컬렉션에만: ID…", "설치 줄에만: ID…"를 적는다. 파일이 없으면 "컬렉션 없음". 조회가 실패하면 "조회 실패: <사유>" | 공개 API |
| 5 후보표 | `tools/workshop/candidates-*.md` 파일 이름(링크) | 폴더 |
| 6 한글화 | `src/ModCode/ModMain/Translations/mods.txt`의 키 수. "모드별 상태는 한글화(K)에서 정한다" 한 줄 | 번역 파일 |

- 같은 실행이 `tools/workshop/collection-desc.txt`도 쓴다: 컬렉션 설명문(한국어). 첫 줄 "TlToybox 편집기와 함께 쓰는 창작마당 모드 목록", 종류마다 `설치` 줄의 `원문 이름(한국어 이름)` 목록, 끝에 "이 설치본은 Steam을 거치지 않아 SteamCMD로 받아 `ModExportData\<창작마당ID>\`에 넣는다. 도구: 저장소 `tools/workshop.ps1`". 사람이 컬렉션 설명 칸에 붙여 넣는다.
- 규칙: 목록·이력·설치 상태가 바뀌면 `-Doc`을 돌려 생성본을 같이 커밋한다(인계 문서에 적는다).

### 3.5 선정 절차 (사람이 끼는 단계)

1. `-Search`로 후보표를 만든다.
2. 제가 설명 전문(`candidates-<날짜>-desc.txt`. 3.3절)을 읽고 `한국어 요약`(한 줄)과 `추천`(`추천`·`보통`·`비추천`과 이유 한 줄)을 채운다. 기준: 새 내용을 더하는가(공법은 새 공법. 시스템·편의는 비추천), 갱신일(2024년 이후면 좋음. 댓글의 호환 보고는 읽지 않으므로 호환성은 갱신일로만 가늠한다고 표 머리에 적는다), 필수 항목을 함께 넣을 수 있는가, 성인향 아님, 같은 영역의 설치 모드와 겹치지 않는가(예: 여성 외모 개선 둘).
3. 사용자가 후보표의 ID로 고른다. 고른 것을 `mods.txt`에 `설치` 줄로 더한다(원문 이름은 서식 태그를 뺀 제목, 한국어 이름은 제가 짓는다, 모드ID `-`, 비고에 필수 항목). 필수 항목이 목록에 없으면 함께 `설치` 줄로 더하고 앞 줄에 둔다. 사용자가 뺀 후보는 후보표에 남는다(그것이 "검토했으나 넣지 않음"의 기록이다).
4. `-Download` → `-Status`로 DLL 수 → DLL이 있으면 이름을 비고에 적는다. 이름에 `MySql`·`Socket`·`Http`·`Web`·`Net`처럼 바깥과 통신하는 낌새가 있으면 상태를 `제외`로 돌리고 사용자에게 알린다. 받은 뒤 `-Install`(계획만)로 설치 원본 모양(평평함/debug)과 `건너뜀` 줄을 본다. `debug` 안에 `Mod_*`가 둘 이상이면 비고에 적고 사용자에게 묻는다.
5. 검증 회차(5절).
6. 실제 설치(사용자 확인, 게임 꺼짐): `-Install -Yes` → `-Status` → `-Doc` → 05 문서 45절·인계 갱신 → 커밋·`develop` 병합.

### 3.6 컬렉션

- 만들기(한 번): 사용자가 Chrome에서 Steam에 로그인해 둔다(비밀번호·Steam Guard는 사용자). 제가 Claude in Chrome으로 `https://steamcommunity.com/sharedfiles/managecollection/?appid=1468810`을 열어 제목 `귀곡팔황 TlToybox 설치 모드`, 설명(`collection-desc.txt`), 가시성 `비공개 링크(Unlisted)`를 넣고 저장한다. 제출·가시성 변경처럼 되돌리기 어려운 동작은 그때마다 사용자에게 허락을 받는다.
- 항목 넣기: `mods.txt`의 `설치` 줄만. 컬렉션 편집 페이지에서 ID로 찾아 넣거나 항목 페이지의 "컬렉션에 추가"로 하나씩. `보류`·`제외`는 넣지 않는다.
- ID 기록: 만든 컬렉션의 ID를 `tools/workshop/collection.txt`에 한 줄로 적는다(git에 넣는다). `-Doc`을 돌려 4절이 "일치 n"이 되는지 본다. 비공개 링크 컬렉션을 API가 돌려주지 않으면 사용자에게 공개로 바꿀지 묻는다(바꾸지 않으면 4절은 "조회 실패"로 남고 문서의 링크만 둔다).
- 유지: 목록이 바뀔 때마다 `-Doc`의 4절이 차이를 보인다. 차이가 있으면 제가 브라우저로 맞춘다. 도구는 컬렉션을 읽기만 한다.

### 3.7 앞 세션의 자동 목록

`D:\Download\output\mod_list.md`를 `tools/workshop/candidates-20261015.md`로 옮긴다. 머리에 "출처: 앞 세션의 `build_lists.py`(점수 = 누적 구독 + 5×즐겨찾기). 2026-10-15에 사용자가 이 자동 목록 대신 설명을 읽어 고른 16개는 05 문서 41절"을 적고 표는 그대로 둔다. 도구가 만든 것이 아니므로 열 모양이 다르다는 것도 적는다.

## 4. 코드

- `tools/workshop.ps1`: `ReadList`가 일곱 칸과 상태·종류 값을 검사한다. `Chosen`과 동작마다의 줄 고르기(3.1). `WriteHistory`(3.2). `DoSearch`(3.3: `Browse`, `Details`, `RequiredItems`, `Flags`, `WriteCandidates`). `DoDoc`(3.4: `CollectionChildren`, `WriteDoc`, `WriteCollectionDesc`). HTTP는 `Invoke-WebRequest`·`Invoke-RestMethod`에 `User-Agent`와 `Accept-Language: zh-CN,ko;q=0.8`을 주고, `-WebRoot`가 있으면 파일을 읽는 한 함수(`Fetch`)로 모은다. 새 매개변수: `-Search`, `-Doc`, `-Kind`, `-Top`, `-Delay`, `-Out`, `-DocOut`, `-WebRoot`, `-History`, `-NoHistory`. `$modes` 검사에 `-Search`·`-Doc`을 더한다.
- `tools/workshop/mods.txt`: 일곱 칸으로 고쳐 쓴다(15줄 + 제외 1 + 보류 1). 모드ID는 41절 표에서 채운다.
- `tools/workshop/history.txt`: 소급 31줄.
- `tools/workshop/candidates-20261015.md`: 3.7절.
- `tools/workshop/collection.txt`: 컬렉션을 만든 뒤.
- `docs/09-workshop-mods.md`: 생성본.
- `.gitignore`: `tools/workshop/candidates-*-desc.txt`.
- `tools/workshop.tests.ps1`: 5절.
- 편집기 C# 코드와 회차 스크립트는 바꾸지 않는다.

## 5. 검증

- `pwsh tools/workshop.tests.ps1`(게임·SteamCMD·네트워크 없이):
  - 일곱 칸 읽기: 다섯 칸 줄 거부, 모르는 상태·종류 거부, `보류`의 종류 `-` 허용.
  - 상태별 동작: `-Download`가 `설치`만, `-Id`로 `보류`는 받고 `제외`는 거부. `-Install`이 `보류`·`제외`를 거부. `-Uninstall`이 `제외` 줄도 지운다.
  - 이력: `받음`·`설치`·`갱신`·`지움` 줄이 모양대로 붙는다. `UNCHANGED`와 계획만 보기는 붙지 않는다. `-NoHistory`면 파일이 그대로다.
  - `-Search -WebRoot`: 저장해 둔 실제 응답(오늘 받은 `browse.html`·`details.json`·`item.html`을 줄인 것)으로 후보표를 만들어 종류별 줄 수, `목록에 있음(설치)` 표시, 필수 항목 `2814703549 Fatury`, `오래됨`·`큼`·`성인향?`·`시스템?`·`대형` 표시, 서식 태그 제거, `-Top` 상한, 요청 제한 파일(`请求太多`가 든 html)에서 멈춤 줄을 본다.
  - `-Doc -WebRoot`: 가짜 게임 폴더·목록·이력·`collection.json`으로 문서를 만들어 절 제목 여섯, 모드 표 줄 수, 이력 순서(최근 것부터), 컬렉션 "일치 n"·"컬렉션에만"·"설치 줄에만", `collection.txt`가 없을 때 "컬렉션 없음", `collection-desc.txt`의 종류별 목록을 본다.
- `pwsh tools/DevRemote/session.tests.ps1`: 그대로 통과(회차 스크립트 변경 없음).
- 실제 `-Search` 한 번(네트워크): 종류 넷의 후보표가 만들어지고 요청 제한에 걸리지 않는지, 걸리면 멈춤 줄이 남는지.
- 게임 검증(고정본 회차 `ws-1017a`부터. 2026-10-16 회차 이름 `ws-1016a`~`w`는 다시 쓰지 않는다): 게임을 켠 채 `-Install -Yes -NoHistory`(새 폴더는 켜진 채 들어간다) → `-Restart` → `wsmods`의 `loaded`가 16 + 새 모드 수, 새 모드의 모드ID·이름 → `ui-check.ps1 -Part base`·`tabs` `FAILS=0` → `rloc dump`의 `written=<n>`과 `untranslated.txt`를 `.devremote\untranslated-<날짜>.txt`로 복사(K의 입력) → `Latest.log`의 새 예외가 편집기 것인지 → `-End`가 `SAVE RESTORED AND IDENTICAL`·`WORKSHOP FOLDERS REMOVED: <새 모드 수>`·`TRANSLATIONS RESTORED`.
- 실제 설치 뒤: `-Status`가 받음·설치 수를 맞게, `-Doc`의 4절이 "일치 n"(컬렉션을 만든 뒤).

## 6. 문서

- 05 문서 45절 "창작마당 모드 확장(W): 검색·등록부·컬렉션": 규칙(3절 요약), 실측(2절), 검증(회차 이름·수치), 확인하지 못한 것.
- 인계 1절(설치 현황의 모드 수·컬렉션), 3절(새 파일), 4절(`-Search`·`-Doc`·`-NoHistory` 명령), 5절(회차에서 창작마당 모드를 넣을 때 `-NoHistory`), 8절(W 끝남, K·X 남음, 회차 이름).
- 02a 조사표 9절 "필수 아이템" 행에 각주: 2026-10-16에 `RequiredItems` 블록으로 읽는 방법을 찾았다(이 스펙 2절).
- `docs/09-workshop-mods.md`는 생성본이므로 손으로 쓰지 않는다.

## 7. 범위 밖

- 한글화(K): 새 모드의 글을 번역해 번역표에 합치는 것. 이 묶음의 회차가 `rloc dump`로 입력만 만든다.
- 로드 순서 바꾸기·켜고 끄기(게임의 모드 화면이 한다), 창작마당 갱신의 자동 감지(`-Download`를 다시 돌리면 받는다), 컬렉션 쓰기 자동화(API가 없다), 편의·시스템 종류의 검색, Nexus 등 다른 배포처, 댓글 읽기.
- 편집기 화면: 이 묶음은 도구와 문서만 바꾼다.

## 8. 확인하지 못한 것, 위험

- Steam의 요청 제한: 한 번의 `-Search`가 검색 페이지 약 10~20회, 상세 3~4회, 항목 페이지 최대 80회를 낸다. `-Delay 1.5`면 3분 안팎. 막히면 멈춤 줄을 남기고 나중에 다시 돌린다(이미 모은 후보는 파일에 있다).
- 비공개 링크(Unlisted) 컬렉션을 `GetCollectionDetails`가 돌려주는지 미확인. 안 되면 3.6절의 대안.
- 새 DLL 모드와 편집기의 충돌은 회차 한 번(세계 진입·`base`·`tabs`)으로만 본다. 실제 플레이의 이상은 사용자 확인 항목.
- 대형 세계관 모드는 새 게임에서만 보이는 것이 많다. 회차는 로드까지만 확인한다. 실제 설치 뒤 사용자 세이브에 주는 영향(새 종문·NPC가 기존 세이브에 들어가는지, 세이브가 깨지는지)은 미확인이다. 설치 전에 사용자의 세이브 백업을 권한다(편집기의 자동 백업과 별개로 `tools/DevRemote/session.ps1`의 복사본 규칙은 회차에만 적용된다).
- `robocopy /MIR` 갱신이 설치본에만 있던 파일을 지운다(G2a 때부터의 미확인).
- 캐시 로그인이 풀리면 사용자가 한 번 로그인한다(G2a 3.2절). Chrome의 Steam 로그인도 사용자 몫이고 Steam Guard가 끼면 그때마다 사용자가 푼다.
- 제목·설명이 중국어라 제가 읽어 요약한다. 오역·오분류는 사용자가 후보표에서 잡는다.
- `candidates-<날짜>-desc.txt`에는 설명 전문이 들어가므로 git에 넣지 않는다(크기와 저작권).

## 9. 구현 계획의 뼈대 (writing-plans가 Task로 펼친다)

1. 목록 일곱 칸·상태별 동작·이력 파일·`-NoHistory`·시험. `mods.txt` 변환과 이력 소급.
2. `-Search`(`-WebRoot` 포함)·시험 자료·시험. 실제 한 번 돌려 `candidates-<날짜>.md`.
3. `-Doc`·`collection-desc.txt`·컬렉션 비교·시험. `candidates-20261015.md` 옮기기. `docs/09` 첫 생성본.
4. (사람) 요약·추천 채우기 → 사용자 선정 → `mods.txt`에 더하기.
5. 받기·DLL 거르기·검증 회차·모드ID 채우기.
6. (사람·브라우저) 실제 설치(사용자 확인) → 컬렉션 만들기·항목 넣기 → `collection.txt` → `-Doc`.
7. 05 문서 45절·인계·02a 각주, 검토, `develop` 병합.
