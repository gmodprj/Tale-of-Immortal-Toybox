# 리뷰 수정 묶음(X) 설계: 탭 줄 재배치와 작은 수정 (2026-10-16)

## 1. 목적

사용자 요청(2026-10-16) 6번 "기타 추가 검토 확인". 리뷰 보고서 `docs/08-review-2026-10-16.md`(49건)에서 사용자가 고른 묶음은 "작은 수정 한 묶음(1·3·4·5·7·13·14·16·20) + 탭 줄 재배치(2·24)"다. 여기에 검수 회차 `ws-1017d`(05 문서 46절)에서 본 "자원 탭 `모드만` 목록의 빈 이름 줄"과 결과 칸 잘림(8)을 더한다. 결과 칸(8)은 예외 표시(4)와 함께 해야 "버튼을 눌렀는데 아무 일도 없다"가 사라진다(보고서 7.1절).

## 2. 범위

| 항목 | 보고서 번호 | 한 줄 |
|---|---|---|
| 탭 줄 재배치 | 2(U1)·24(F5/U10) | 화폐 탭을 자원 탭의 그룹으로, 안전 탭을 제목 줄 단추로. 탭 간격 52→46. 자원 탭의 영석 줄을 뺀다 |
| C1 | 1 | 공법 탭의 줄 그리기를 보호한다. 깨진 공법은 "(읽을 수 없음)" 줄 |
| U2 | 3 | 선천운명 교체·역천개명 정하기·기술 모두 배우기·기예 모두 배우기를 두 번 누르기로 |
| C2 | 4 | 버튼 콜백과 탭 그리기의 예외를 결과 칸에 보인다 |
| C3+D5 | 5·20 | `Destroy`에서 날짜 고정·배율·무적을 끈다. 인계 2절의 문장을 맞춘다 |
| C4 | 6 | 캐릭터 가져오기의 경지 구역 뒤에 `GradeService.Settle` |
| C5 | 7 | 백업 없이 쓰는 세 곳에 `SaveBackup.EnsureOnce()` |
| 빈 이름 줄 | 46절 | 이름이 비거나 "0"인 모드 기술은 "이름 없음(번호)"로, 목록 끝에 |
| U3 | 8 | 결과 칸이 넘치면 "…"과 `더 보기`. 누르면 스크롤되는 칸으로 펼친다 |
| 문서 | 13·14·16 | 05 문서 3절(수명 상한)·34절(immortalPoint), 인계 3절(절 수·스펙 목록) |

미루는 것(이번에 하지 않는다): 12(내보내기 format 2), 9(`.devremote` 경로), 17(시험 프로젝트), 19(커밋 표지), 10(번역 적용 비용), 11(비급 뽑기 시간 상한), 15(05 문서 1~4절 다시 쓰기), 낮음 21~49, 8.3a(사용자 캡처 필요), 8.6절.

## 3. 탭 줄 재배치

### 3.1 지금

`EditorWindow.Build`가 탭 16개를 `tabs` 배열 순서로 `Tab<i>`(i = 배열 순번) 단추로 그린다. 간격 52, 단추 150x44, 탭 줄 `Ui.Rect("Tabs", window, 16, ContentY, 150, 16 * 52)`. 16번째의 아래 끝 888, 결과 칸의 위 끝 908. 다음 탭은 들어갈 자리가 없다(보고서 4.1절).

### 3.2 결정: 탭 번호를 고정하고 두 자리를 비운다

- 탭마다 **번호**를 둔다(배열 순번이 아니다). 번호는 지금의 `Tab<i>` 이름을 그대로 쓴다: 능력치 0, 자원 1, 보관함 3, 경지 4, 공법 5, 선법 6, 신기 7, 운명 8, 기예 9, NPC 10, 세계 11, 배율 12, 프리셋 13, 안전 14, 도호 15. 2(화폐)는 빈 번호로 남긴다. 검증 스크립트의 `Press "Tab<번호>"`가 그대로 맞는다. 새 탭은 16부터 받는다.
- 탭 줄에는 안전(14)을 뺀 14개를 그린다. 간격 46, 단추 150x40(글 19px는 그대로 보인다. 보고서 4.1절 1번). 탭 줄의 높이는 `18 * 46`으로 두어 18개까지 들어간다(18번째의 아래 끝 64 + 17·46 + 40 = 886 < 908).
- 편집 대상 표시(`TargetMark`)는 단추 높이에 맞춘다(높이 40).
- `uistate`(개발 명령)의 `tab=`은 **번호**를 찍는다(배열 순번이 아니라). 검사의 `tab=$i`가 그대로 맞는다.

### 3.3 안전 탭은 제목 줄의 단추 `Tab14`

- 제목 줄의 `접기` 왼쪽에 단추 `Tab14`(글 "안전", 90x36)를 둔다. 누르면 내용 영역에 안전 탭(백업·변경 이력)이 그려진다. 그동안 왼쪽 탭 줄에는 켜진 탭이 없고 `Tab14`가 켜진 색이며 글이 **"돌아가기"**로 바뀐다. 다시 누르면 안전을 열기 전의 탭으로 돌아간다. 탭 줄의 다른 탭을 누르면 그 탭으로 간다(안전은 닫힌다).
- 안전 탭의 내용(`SafetyTab`)은 바꾸지 않는다. 탭을 떠나고 들어올 때의 `Leave`·`OnShow` 규칙도 같다(안전으로 가면 앞 탭의 하위 화면이 닫힌다. 지금 탭 전환과 같다).
- 상태(제 결정): 안전도 `tabs` 배열의 한 원소이고 `tab`(보이는 탭의 배열 순번)으로 선택된다. 제목 줄 단추로 열 때 그때의 `tab`을 `tabBefore`에 적어 두고, 돌아가기는 `SwitchTab(tabBefore)`다(한 번도 연 적이 없으면 0). 접기·펼침과 닫기·열기는 지금처럼 `tab`을 바꾸지 않으므로 안전을 보던 채 닫았다 열면 안전이 그대로 보이고 `Tab14`는 돌아가기다.
- 검사: `history`·`wrap`·`preset-check`의 `Press 'Tab14'`는 그대로 맞는다. `tabs`에 "안전을 열면 `tab=14`, `Tab14`의 글이 돌아가기, 다시 누르면 앞 탭으로"를 더한다.

### 3.4 화폐는 자원 탭의 그룹 "화폐"

- `CurrencyTab`을 지운다. 자원 탭의 그룹 목록 끝(기타 다음)에 `ResGroupCurrency`("화폐 n")를 둔다. n은 `ItemCatalog.Currencies`의 수(명성은 세지 않는다).
- 화폐 그룹의 표는 아이템 표(`ItemTable`)를 그대로 쓴다. `ItemCatalog.Currencies`의 원소는 아이템 표의 줄과 같은 `ItemCatalog.Entry`다(`Id`·`Name`·`Desc`·`Level`·`Type`·`SubType`. `Kind`는 null). 그래서 표(`ListView<Row>`)는 바꾸지 않고 `ResourcesTab`의 `Rows()`(화폐 그룹이면 `Currencies`로 줄을 만든다)와 `DrawRow`(화폐 그룹이면 화폐 줄 그리기로 간다)만 바꾼다. 줄은 `Row { Item = 재화 항목 }`이고 열은 ☆ 없음(즐겨찾기는 가방 아이템의 것), 이름(`ItemName<id>` 단추. 누르면 아래 설명 칸에 설명 전체), 종류 "화폐", 품질 "-", 보유(`CurrencyOwned<id>`. 영석은 `EditService.Money`, 나머지는 `propData.GetPropsNum`), 지급 `CurrencyAdd<id>`("+ 수량". 수량은 윗줄의 지급 수량 `AmountBar "item"`). 지급은 지금 `CurrencyTab.Add`와 같다(영석은 `AddMoney`, 나머지는 `GiveItem`에 이름 치환).
- 화폐 그룹에서는 종류 칩·거르기 줄 대신 **명성 줄** 하나를 둔다: "속성 명성 보유 N(`CurrencyOwnedFame`) [+ 수량](`CurrencyAddFame`)"과 안내문(지금 `CurrencyTab`의 명성 줄 글 그대로). 검색·품질·보유 중만 거르기는 화폐 그룹에 없다(재화 9종이라 필요 없다).
- 설명은 표에 두지 않고(다른 그룹과 같다) 설명 칸에서 본다. 재화 설명은 `ItemCatalog.Currencies`의 `Desc`.
- `AmountBar "money"`(화폐 탭의 추가량)는 더 쓰지 않는다. 수량 기억은 "item" 하나다.
- 검사: `items`·`wrap`의 `Press 'Tab2'` 세 곳을 `Press 'Tab1'; Press 'ResGroupCurrency'`로. `CurrencyDesc10031`(설명이 잘리지 않음) 검사는 `ItemName10031`을 눌러 `ItemDetail`에 설명 전체가 나오는지로. `CurrencyRow10031/10032`(겹치지 않음) 검사는 뺀다(아이템 표의 줄 높이는 `ListView`가 정하고 다른 그룹에서 이미 검사한다). `$tabProbe`의 2번(`CurrencyAdd10001`)은 사라지고 1번은 `MoneyAdd`→`BagApply`.

### 3.5 자원 탭의 윗줄(24)

- 영석 줄(`MoneyBox`의 1줄: "영석: n", 추가량, `MoneyAdd`)을 뺀다. 영석은 화폐 그룹의 첫 줄이다. `TopH`는 92→48이고 지급 수량·가방 줄이 그 한 줄이다(x 배치는 지금 2줄과 같다).
- 비는 44px로 그룹 단추 15개(즐겨찾기·전체·그룹 10·기술 서적·화폐)가 `GroupStep` 42 그대로 설명 칸 위에 들어간다(56 + 14·42 + 38 = 682 < 686).

### 3.6 문서·검사의 좌표

- 인계 2절의 탭 순서 문장과 5절의 "탭 줄은 x=151, `Tab0`이 y=146이고 탭마다 52씩"을 고친다(`Tab0` y=144, 46씩. 안전은 제목 줄. 실측해서 적는다).
- 05 문서 47절(탭 줄 재배치)에 규칙·실측·검사.

## 4. 작은 수정

### 4.1 C1 공법 탭의 줄 보호

- `MartialTab.DrawMartialRow`에서 `learned.Data.data.To<DataProps.MartialData>()`와 이름·종류·경지 읽기를 try/catch로 감싼다. 예외가 나면 그 줄에 "(읽을 수 없음) <예외 종류>"(`MartialBroken<index>`)와 `선택` 단추만 그린다. 선택하면 아래 세부·옵션 칸은 "읽을 수 없는 공법입니다. 기술 망각으로 지울 수 있습니다."이고 `기술 망각`(지금 단추)은 켜져 있다. 망각은 지금 경로(`MartialService.Remove` 계열)로 soleId만 쓰므로 읽지 않고도 지워진다(확인: 플랜에서 코드로).
- 개발 명령 `mbreak`(DEVTOOLS): 편집 대상의 배운 공법 하나의 기본 번호 값을 어느 표에도 없는 수(999999999)로 바꿔 읽을 수 없게 만든다. 검사 `actions`: `mbreak` → `Tab5` → `MartialBroken*`이 있고 다른 줄은 그대로 그려지며 → 선택 → 망각 → 목록에서 사라진다. 기본 번호로 예외가 나지 않으면 다른 값(`martialType`을 없는 수로, 옵션 목록을 비우기 등)을 바꿔 봐도 된다(플랜에서 실험으로 정한다). 그래도 만들 수 없으면 그 사실을 05 문서에 적고 코드 읽기로만 확인한 것으로 보고한다.

### 4.2 U2 두 번 누르기

`Confirm.Button`으로 바꾼다. 확인 글은 "정말 <동작>"이고 결과 칸의 안내는 "…합니다. 한 번 더 누르면 실행합니다." 꼴(보고서 U6의 통일 문구).

| 단추 | 확인 키 | 첫 누름의 안내 |
|---|---|---|
| `LuckAdd<id>`(교체 모드일 때만) | `luck-replace` | "선천운명 <이전 이름>을(를) 떼고 <새 이름>(으)로 바꿉니다. 한 번 더 누르면 실행합니다." |
| `FatePick<id>` | `fate-pick` | "<대경지>의 역천개명을 <이름>(으)로 정합니다. 이전 것의 효과는 꺼집니다. 한 번 더 누르면 실행합니다." |
| `SkillLearnAll` | `skill-learn-all` | "<분류>의 배울 수 있는 기술을 모두 넣습니다(하나씩 망각해야 되돌립니다). 한 번 더 누르면 실행합니다." |
| `ArtLearnAll` | `art-learn-all` | "<분류>의 기예를 모두 배웁니다(하나씩 잊어야 되돌립니다). 한 번 더 누르면 실행합니다." |

교체 모드가 아닌 `LuckAdd<id>`(추가)는 그대로 한 번이다. 검사: `actions`에 `SkillLearnAll`·`ArtLearnAll`의 "첫 누름은 정말 …, 값은 그대로 / 두 번째에 실행"을, 새 부분 `confirm2`에 교체·정하기의 같은 검사를 더한다(교체는 운명 탭의 교체 단추로 고르기 화면을 열어 `LuckAdd*`를 누른다. 정하기는 `FateOpen` → 칸 → `FatePick*`).

### 4.3 C2 공통 보호

- `Ui.Button`: 콜백을 감싼다. 예외가 나면 `EditorWindow.Say("오류: " + e.GetType().Name + ": " + e.Message)`, 로그 `[TlToybox] 버튼 오류 <단추 이름>: <종류>: <메시지>` 한 줄. 끌기 처리(`AddEntry`)는 지금대로.
- `EditorWindow.Render`: `tabs[tab].Render(content)`를 감싼다. 예외가 나면 내용 영역을 비우고 `DrawError` 글 "이 탭을 그리지 못했습니다: <종류>: <메시지>"를 그린다. 로그는 같은 글이 이어질 때 한 번만(`lastDrawError`).
- 개발 전용: 제목 줄에 단추 `DevThrow`(글 "오류", 40x36. DEVTOOLS 빌드에만) — 누르면 `InvalidOperationException("시험 예외")`. 개발 명령 `uifail`은 다음 `Render`에서 탭 그리기 앞에 같은 예외를 한 번 던지게 한다. 검사(새 부분 `errors`): `DevThrow` → `message='오류: InvalidOperationException: 시험 예외'`, 창은 계속 반응(다음 `Tab0`이 그려진다); `uifail` → `DrawError`가 있고 `Tab1`을 누르면 정상. 이 부분의 "no warning" 검사는 "버튼 오류 줄 한 개와 탭 오류 줄 한 개만"으로 바꾼다.

### 4.4 C3 세이브 전환 때 상태 되돌리기 (+D5)

- `ModMain.Destroy`가 `ResetState()`를 부른다: `TimeService.SetFrozen(false)`(켜져 있을 때만), `RateService.Reset()`(배율 모두 1배, 무적 끔, `BattleService.Reset()` 포함. 이력에 남는다). `EditorWindow.Close()`보다 뒤, `OnDestroy`들보다 앞.
- 개발 명령 `unload`(DEVTOOLS)가 `ResetState()`만 부른다(창은 닫지 않는다). 검사 `status`의 끝에: 날짜 고정·무적·x2를 켜고 `unload` → 900ms 뒤 `status=[hidden]`, `tfreeze`가 off.
- 실제 `Destroy`가 불리는 경로(주 화면으로 나가 다른 세이브 불러오기)는 회차에서 돌리지 않는다. 코드 읽기와 `unload`로 확인한 것으로 적는다.
- 인계 2절의 "게임을 켤 때마다 꺼진 상태로 시작한다"에 "세이브를 바꿀 때(`Destroy`)도 모두 꺼진다"를 더한다.

### 4.5 C4 가져오기의 경지 구역 뒤 보충

- `CharacterExportService.Import.PutGrade`가 수련치를 쓴 뒤 `GradeService.Settle(me)`를 부르고, 돌려준 글이 있으면 결과 문장 끝에 " " + 글을 붙인다. `Settle`이 백업 오류를 돌려주면 그대로 붙인다(이미 `EnsureOnce`가 앞서 돌았으므로 실제로는 나지 않는다).
- 05 문서 30절의 가져오기 규칙 표에 "경지: 쓴 뒤 경지에 따른 보충(35절)" 한 줄.
- 검사 `charvault`: 등선 후기(44)이고 선력·선법이 없는 인물 파일을 경지 구역만 가져오면 `imm`이 `immortalPoint=6 cards=5`(preset-check 611~620과 같은 기준).

### 4.6 C5 백업 빠진 세 곳

| 곳 | 변경 |
|---|---|
| `EditService.OpenGameBreakthroughScreen` | `RemoveStaleBottleneck` 앞에 `SaveBackup.EnsureOnce()`. 오류면 그 글을 돌려주고 화면을 열지 않는다 |
| `GradeService.Settle` | `DropTitles` 앞에 `EnsureOnce`(채울 것이 없을 때도). 오류면 `DropTitles`를 건너뛰고 오류 글을 돌려준다 |
| `FateService.SetAmong` | `Release`를 처음 부르기 전에 `EnsureOnce`. 오류면 비우지 않고 그 글을 돌려준다 |

C5-4(`EditService.RestoreResources`의 `verified: true`)는 낮음이지만 한 줄이므로 함께 한다: 쓴 뒤 읽은 값이 기대값과 같은지로 `verified`를 정한다.

검사(새 부분 `backup`): 백업 폴더 `<data>\backups`의 하위 폴더 수로 본다. `EnsureOnce`는 한 실행에 한 번만 백업하므로(`lastBackupDir`) 항목마다 그 앞에 개발 명령 `bkreset`(DEVTOOLS. `SaveBackup.DevReset()`이 `lastBackupDir`를 지운다)을 부른다(제 결정. 회차를 셋으로 나누는 것보다 싸다). (1) `bkreset` → `wexp`(개발 명령)로 수련치를 채운 뒤 `MajorOpen` → 폴더 +1, 게임 화면은 `uiclose`로 닫는다. (2) `bkreset` → `pgraise <지금 경지>`(이미 그 경지 이상 → `Settle`만) → 폴더 +1. (3) `bkreset` → 역천개명 구역: 플랜에서 `cimpfates`로 "도달하지 않은 대경지에, 도달한 대경지의 역천개명과 계열이 겹치는 것"을 넣는 파일을 만들어 `Release`만 돌고 `Set`이 거절되는 경우를 재현한다 → 폴더 +1이고 결과에 "아직 도달하지 않은 경지". RED에서 (1)·(2)·(3) 모두 폴더 수가 그대로여야 한다. 고정본 세이브의 상태에 따라 RED가 실패하지 않는 항목(예: `Settle`이 채울 것이 있어 어차피 백업)이 나오면 검사를 그 상태에 맞게 바꾸고 05 문서에 적는다.

### 4.7 빈 이름 줄 (46절)

- `SkillCatalog.Add`: 모드 행(isMod)의 이름이 빈 글이거나 "0"이거나 키 그대로이면 이름을 "이름 없음(<번호>)"로 둔다. `Search`에 번호를 넣어 번호로 찾아진다. 분류 목록(`All(kindIndex)`)에서 이름 없음 행은 끝으로 보낸다(안정 정렬. 그 밖의 순서는 그대로).
- 기본 행(게임 본체)의 거르기는 그대로다(이름이 풀린 것만).
- 검사 `modbooks`: `모드만` 첫 줄(`Row0`)의 이름이 "이름 없음"으로 시작하지 않고, `ui set ItemQuery 126954816`으로 "이름 없음(126954816)" 줄이 찾아진다. `rmods`의 `count=103`은 그대로.

### 4.8 U3 결과 칸

- 결과 글의 높이(`Ui.TextHeight(message, 너비, 18)`)가 칸 안쪽(76)을 넘으면: 칸의 오른쪽 아래에 "…"(`MessageEllipsis`)와 단추 `MessageMore`(글 "더 보기", 90x30)를 그린다. 넘지 않으면 둘 다 없다.
- `MessageMore`를 누르면 결과 칸이 위로 커져(높이 `MessageH + 300`) 내용 영역 위에 덮이고, 안은 스크롤(`Ui.Scroll`, 이름 `MessageFull`, 글 `MessageFullText`)로 전체가 보인다. 단추의 글은 "접기"(`MessageMore` 그대로). 탭 전환·새 결과(`Say`)·창 닫기에서 접힌다. 펼친 칸은 내용 영역보다 뒤에 만들어 위에 그려지고, 바탕 `Image`가 클릭을 받으므로 덮인 단추는 눌리지 않는다(접어야 누를 수 있다). 바탕은 창과 같은 `Ui.Background`(불투명) 위에 `Ui.Section`.
- 검사 `tabs`의 "a long result is accepted"(`uisay 30`, 360자) 뒤에: `MessageMore`가 있고 → 누르면 `MessageFullText`의 글 길이가 360 → 다시 누르면 없어진다. `uisay 1` 뒤에는 `MessageMore`가 없다. `Tab1`을 누르면 접힌다.

### 4.9 문서 13·14·16

- 05 문서 3절 "편집기 자체 상한 100000" → "8000년(`AttributeCatalog.OwnLifeMaxYears`)".
- 05 문서 34절 `immortalPoint` 문장에 "(35절에서 고침)".
- 인계 3절 `docs/05-editor-v0.1.md` 줄 "(1~37절)" → "(1~48절)", 스펙 목록에 빠진 것(`2026-10-15-appellation`, `-martial-books`, `-resources-tab`, `-workshop-install`, `2026-10-16-mod-korean`, `-mod-skill-books`, `-workshop-expand`, `-mod-korean-2`, `-review-fixes`)을 더한다. 시험 수 "34개"는 이미 맞다.

## 5. 코드

| 파일 | 변경 |
|---|---|
| `UI/EditorWindow.cs` | 탭 번호(`TabNumbers`), 탭 줄 14개·46 간격, 제목 줄 `Tab14`·`DevThrow`, 안전 열기/돌아가기, `Render` 보호·`DrawError`, 결과 칸 `MessageEllipsis`·`MessageMore`·`MessageFull`, `DevState`의 `tab=` 번호 |
| `UI/Ui.cs` | `Button`의 콜백 보호 |
| `UI/Tabs/CurrencyTab.cs` | 삭제 |
| `UI/Tabs/ResourcesTab.cs` | `TopH` 48, 영석 줄 제거, 화폐 그룹(`CurrencyKey`), 명성 줄, 화폐 줄 그리기 |
| `UI/Tabs/MartialTab.cs` | 줄 보호, `SkillLearnAll` 확인 |
| `UI/Tabs/ArtTab.cs`, `UI/Tabs/LuckTab.cs` | `ArtLearnAll`·`LuckAdd`(교체)·`FatePick` 확인 |
| `ModMain.cs` | `ResetState`, `Destroy`에서 호출 |
| `Services/CharacterExportService.Import.cs` | `PutGrade` 뒤 `Settle` |
| `Services/EditService.cs`, `Services/GradeService.cs`, `Services/FateService.cs` | `EnsureOnce` 세 곳, `RestoreResources`의 `verified` |
| `Services/SkillCatalog.cs` | 이름 없음 행 |
| `Dev/DevCommandsUi.cs`, `Dev/DevCommandsSkill.cs`, `Dev/DevCommandsTime.cs`, `Dev/DevCommandsStage3.cs` | `uifail`, `mbreak`, `unload`, `bkreset` |
| `Safety/SaveBackup.cs` | `DevReset()`(DEVTOOLS) |
| `tools/research/ui-check.ps1` | `$tabProbe` 표(번호→단추), `base`·`tabs`·`items`·`wrap`·`actions`·`modbooks`·`status`·`charvault` 수정, 새 부분 `confirm2`·`errors`·`backup` |
| `tools/research/preset-check.ps1` | `Tab14` 검사의 이름만 |

## 6. 검증

- RED: 회차 `ws-1017e`(고정본)에서 바뀐 검사만 돌려 실패를 본다(새 요소가 없어서 실패). `-End`.
- GREEN: 두 구성 빌드(경고 0), 회차 `ws-1017f`: `base`·`tabs`·`items`·`wrap`·`actions`·`confirm2`·`errors`·`backup`·`modbooks`·`status`·`charvault`·`history`와 `preset-check`의 탭 부분이 `FAILS=0`. 캡처 `tabs`·`wrap-currency`·`long-message`(펼친 결과 칸). `-End`가 `SAVE RESTORED AND IDENTICAL`.
- 검사 부분이 많아 한 회차에 다 돌리지 못하면 `ws-1017g`로 나눈다.
- 실제 설치: 사용자 확인, 게임 꺼짐, `pwsh tools/install.ps1`.

## 7. 문서

05 문서 47절(탭 줄 재배치: 번호·좌표·화폐 그룹·안전 단추·검사)·48절(작은 수정 열 가지: 규칙·검사·미확인), 인계 1절(설치 시각·48절까지)·2절(탭 순서, D5 문장)·3절(D4)·5절(탭 좌표)·8절(X 끝남, 8.1 표, 남은 리뷰 항목), 05 문서 3·30·34절.

## 8. 범위 밖

리뷰 항목 가운데 2절 표의 "미루는 것". 안전 탭의 내용 변경, 결과 칸의 이력 연결("안전 탭의 이력에 전체" 문구는 두지 않는다. 더 보기로 충분하다), 즐겨찾기에 화폐 넣기, 화폐 그룹의 거르기.

## 9. 확인하지 못한 것, 위험

- `Destroy` 경로 자체는 회차에서 돌리지 않는다(`unload`로 같은 함수만).
- C5의 RED는 고정본의 상태에 달렸다(4.6절). C1의 `mbreak`가 예외를 만들지 못하면 코드 읽기로만 확인한 것으로 적는다.
- 탭 번호를 고정했으므로 검사·문서의 `Tab<번호>`는 그대로지만, 좌표로 누르는 곳(인계 5절의 y=146·52씩)은 바뀐다. 인계 5절을 실측으로 고친다.
- 화폐 그룹은 아이템 표를 쓰므로 `ItemTable`의 줄 이름이 `Row<i>`다. `CurrencyRow<id>`로 줄을 찾던 검사는 없어진다(3.4절).
- 결과 칸을 펼친 상태는 기억하지 않는다. 새 결과가 오면 접힌다(긴 결과를 보는 중에 다른 단추를 누르면 접힌다. 의도한 것).
