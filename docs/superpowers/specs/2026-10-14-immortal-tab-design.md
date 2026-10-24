# 선법 탭 설계 (2026-10-14)

## 1. 목적

사용자 요청(2026-10-14): "선법탭을 두거나 별도의 화면에서 하위페이지로 구성후 공법과 같은 방식의 전체 선법 습득 페이지 구성". 사용자가 고른 것: 새 "선법" 탭, 하나씩 익히기, 모두 익히기, 선력 함께 보이기. 지우기는 범위 밖이다.

## 2. 실측 (회차 `imm-1014a`, `grade-1014a`~`f`, 실험 브랜치 `feat/immortal-research`)

| 항목 | 실측 |
|---|---|
| 저장 위치 | `unitData.immortalCard`(`DataUnit.ImmortalCard`): `immortalPoint`(선력 총량), `cardDatas`(가진 카드: `immortalId`, `cardId`, `isPassive`, `activeMonth`), `equipCardDatas`(장착: `slot`, `equipCardId`, `isPassive`) |
| 익히기 | `ImmortalCard.AddImmortalCard(cardId, immortalId)`가 카드를 더한다. 선법 창에 바로 나오고 선인 옆 수집 수가 오른다(동방명설 0/6 → 2/6, 계고혼 0/1 → 1/1). `AddImmortalCard(cardId)`(인수 하나)도 더하고 `True`를 돌려준다. 같은 카드를 다시 더하면 늘지 않는다 |
| 저장 | 저장 후 재시작해도 카드가 남는다 |
| 경지 | 결정 경지(대경지 3)에서도 데이터는 들어간다. 장착 칸은 `immortalSlot`: 등선 초기 2, 중기 4, 후기 6 |
| 선인 | `g.conf.immortalHuman`(20명): `name`, `activeSkills`(주동 카드 번호, 0이면 없음), `passiveSkills`(피동 카드 번호들, `|`로 이음), `collectID`(선법 창의 수집 수에 세는 카드들. 0이면 목록에 나오지 않는 선인: 금홍파효, 부서진 제단, 식혼탑, 장삼) |
| 주동 카드 | `g.conf.immortalMethod`(10줄): 번호가 `id`. `name`, `cardCost`(필요 선력), `info`(설명), `getText`(획득 경로). 8000·9000은 이름이 번역되지 않은 키 그대로다 |
| 피동 카드 | `g.conf.passiveMethod`(61줄): 카드 번호가 `passiveId`(줄 번호 `id`와 다르다). `name`, `cardCost`, `info`, `getText`는 번역 키다. `exclusiveImmortal`의 뜻은 미확인(선인 번호와 맞지 않는 값이 있다) |
| 선력 | 선법 창 "본원 선력: 쓴 양/총량". 총량 상한은 `roleAttributeLimit.immortalPointMax` 40. 능력치 탭의 "본원 선력"(05 문서 35절)과 같은 값 |
| 데이터로 장착 | `ImmortalCard.EquipCard(4000, false, 0)`는 `True`였지만 그 뒤 선법 창을 열 때 예외(`ArgumentOutOfRangeException`)가 났다. 장착은 하지 않는다 |

## 3. 결정

- 탭 "선법"을 공법 다음에 둔다: 능력치 · 자원 · 화폐 · 보관함 · 경지 · 공법 · **선법** · 운명 · 기예 · NPC · 세계 · 배율 · 프리셋 · 안전(14개). 검증 스크립트의 이름은 `Tab6`이 선법이 되고 그 뒤가 하나씩 밀린다(프리셋 `Tab12`, 안전 `Tab13`).
- 편집 대상은 공법 탭과 같다(플레이어 또는 고른 NPC). 대상의 `immortalCard`가 없으면 탭에 "이 인물은 선법 자료가 없습니다"를 보이고 버튼을 끈다.
- 선법의 목록은 선인 표에서 만든다: 선인마다 주동 카드(있으면) 하나와 피동 카드들. 한 카드가 두 선인에 들어 있으면 첫 선인의 것으로 한 줄만 보인다. 이름·필요 선력·설명·획득 경로는 주동은 `immortalMethod`, 피동은 `passiveMethod`에서 읽는다. 표에 없는 카드 번호는 목록에 넣지 않는다.
- 익히기는 `AddImmortalCard(카드 번호, 선인 번호)`다. 쓰기 전에 `SaveBackup.EnsureOnce()`, 쓴 뒤 `IsHadSkill`로 다시 읽어 판정하고 `ChangeLog.Add`.
- 장착과 지우기는 하지 않는다. 장착은 게임의 선법 창에서 한다.
- 등선 전에도 익힐 수 있다. 화면에 "선법 창과 장착은 등선경부터입니다(장착 칸: 초기 2, 중기 4, 후기 6)"를 보인다.
- 선력은 능력치 탭의 정의(`AttributeCatalog`의 `immortalPoint`)와 `EditService.SetAttribute`를 그대로 쓴다.

## 4. 화면

첫 화면:

    본원 선력: 3 / 6   [-1] [+1] [최대]          선법 창과 장착은 등선경부터입니다(장착 칸: 초기 2, 중기 4, 후기 6).
    가진 선법 9개                                                                     [선법 추가]
    선인        이름              종류   필요 선력   상태
    금홍파효    선법·금홍파효      주동   3          장착 중
    금홍파효    참강              피동   2
    …

- "쓴 양"은 장착한 카드들의 필요 선력의 합이다(`equipCardDatas`의 카드 번호로 표에서 읽는다).
- 목록은 `ListView`, 글은 줄바꿈한다.

선법 추가(하위 화면, `Push`):

    선인: [전체] [금홍파효] [검일] … (버튼 줄, 넘치면 줄바꿈)
    종류: [전체] [주동] [피동]     이름: [검색 칸]                    [보이는 선법 모두 익히기]
    이름        선인     종류   필요 선력   설명 / 획득 경로                     상태      
    선법·천무절향 동방명설  주동   4          전장에 풍장을 생성해 … / 천환선사에서 …   미보유    [익히기]

- `익히기`는 가진 카드면 꺼진다(이미 보유).
- `보이는 선법 모두 익히기`는 두 번 눌러 확인한다(`Confirm`). 결과 문장: "선법: 익힘 7, 건너뜀 2(이미 보유)."
- 요소 이름: `ImmortalPointMinus`, `ImmortalPointPlus`, `ImmortalPointMax`, `ImmortalPointText`, `ImmortalAddOpen`, `ImmortalHuman<선인 번호>`(0은 전체), `ImmortalKind<0|1|2>`, `ImmortalSearch`, `ImmortalLearnAll`, `ImmortalLearn<카드 번호>`, `ImmortalState<카드 번호>`, `ImmortalNoData`.

## 5. 코드

| 파일 (`src/ModCode/ModMain/`) | 맡는 일 |
|---|---|
| `Services/ImmortalService.cs` (새) | `Entry`(카드 번호, 선인 번호와 이름, 주동 여부, 이름, 필요 선력, 설명, 획득 경로), `All()`(캐시), `Find(humanId, kind, query)`, `Has(unit, cardId)`, `HasData(unit)`, `Learned(unit)`(가진 카드의 줄들), `UsedPoint(unit)`, `Learn(unit, entry)`, `LearnAll(unit, entries)` |
| `UI/Tabs/ImmortalTab.cs` (새) | 첫 화면과 선법 추가 화면 |
| `UI/EditorWindow.cs` | 탭 등록(공법 다음), 탭 줄 높이 `14 * 52` |
| `Dev/DevCommandsImmortal.cs` (새) | 검증용: `ilist`(가진 카드와 수), `icount [선인] [종류] [검색어]`(목록 수), `ilearn <카드 번호>`, `ihas <카드 번호>` |
| `Dev/DevCommands.cs` | 위 명령 연결 |
| `tools/research/immortal-check.ps1` (새) | 검사(6절) |
| `tools/research/ui-check.ps1`, `datebag-check.ps1`, `preset-check.ps1` | 탭 이름(`Tab6` 이상을 하나씩), 탭 수 |

## 6. 검증 (고정본, `session.ps1` 회차)

`immortal-check.ps1 -Part <이름>`:

| 부분 | 보는 것 |
|---|---|
| `catalog` | 목록 수가 선인 표에서 센 카드 수와 같다, 거르기(선인 1004 → 6줄, 주동만 → 주동 카드 수, 이름 검색) |
| `learn` | 하나 익히기 → 보유, 두 번째는 "이미 보유", 변경 이력 |
| `learn-all` | 거른 목록 모두 익히기(두 번 눌러 확인) → 그 수만큼 보유, 다시 누르면 모두 건너뜀 |
| `ui` | 탭이 그려지고 선력 줄이 능력치 탭과 같은 값, ±1과 최대, 선법 추가 화면의 거르기 버튼과 줄 버튼, 익히기를 누르면 상태가 바뀜, 콜백 예외 없음 |
| `game` | 등선 후기로 올린 뒤 익힌 카드가 게임의 선법 창에 나온다(캡처) |
| `learn` → `-Restart` → `restart` | 저장 후 재시작해도 남는다 |

기존 검사(`base`, `tabs`, `confirm`, `wrap`, `fateguard`, `charvault`, `rates`, 프리셋 `ui`·`files`)가 탭 이름을 고친 뒤에도 통과한다. 배포 구성 경고·오류 0, DLL 하나.

## 7. 확인하지 못한 것

- `passiveMethod.exclusiveImmortal`의 뜻.
- NPC에게 선법이 효과가 있는지(NPC가 선법을 쓰는지).
- 수집 목록(`collectID`)에 없는 카드(부서진 제단 등)를 익혔을 때 게임 화면에서 어디에 나오는지.
- 이름이 번역되지 않은 주동 카드 8000·9000은 키 그대로 보인다.
