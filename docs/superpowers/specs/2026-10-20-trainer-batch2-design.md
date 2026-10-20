# 트레이너 비교 2묶음(성공률 여섯)과 공법 옵션 자리 강제 교체 — 설계 (2026-10-20)

## 1. 의도

사용자 요청(2026-10-20): "모드창의 공법 참오 속성의 변경에서 불가: 이자리에 나올수 없는 옵션도 강제 추가 가능하게 수정하는 기능도 다음 작업에 포함하여 진행". "다음 작업"은 `docs/11-trainer-comparison.md` 4절 2번(성공률 묶음)이다. 두 기능을 한 묶음으로 설계·구현한다.

- **A. 공법 옵션 자리 강제 교체**: 공법 탭의 옵션 교체 화면은 그 자리의 가중치(`weight<자리>`)가 0인 옵션을 "불가: 이 자리에는 나올 수 없는 옵션"으로 막는다(05 문서 21절 규칙, `MartialService.ReplaceBlockReason`). 이 사유 하나만 두 번 누르기로 넘을 수 있게 한다.
- **B. 성공률 여섯**: 도주, 훔치기, 포획(연요호 요수 포획·호요 기술 습득), 드롭, 종문 초빙, 관계 발전. 켜 두면 플레이어의 시도가 성공한다.

트레이너 코드는 옮기지 않는다. 트레이너가 부르는 게임 API 이름만 실마리로 쓰고(비교 문서 2.3절), 동작은 회차에서 실험해 확정한다(4절). 구현 규칙은 1묶음과 같다: 백업·재조회·이력, 회차 검증, Vector2 같은 값 형식 인자 메서드 패치 금지(05 문서 65절).

## 2. 목표 / 비목표 / 실패로 볼 것

목표
- A: 자리 불가 옵션 줄에도 단추가 켜지고, 첫 누름에 위험을 알리고 둘째 누름에 바꾼다. 바뀐 옵션이 게임의 공법 화면에 나오고, 저장 후에도 남는다.
- B: 켜고 끌 수 있는 줄 여섯. 켠 줄은 플레이어의 그 시도를 성공시킨다. NPC끼리의 시도는 그대로다.

비목표
- A: 중복 한도(`multi`) 초과, 선행·배타 조건(`require`) 위반은 지금처럼 막는다(요청 범위 밖. 내 판단). 다른 공법에만 있는 옵션 번호(`GetItems`에 없는 번호)는 넣지 않는다.
- B: 확률을 배율로 조절(x2 등)하지 않는다. 켬/끔만. 다른 성공률(돌파 성공, 참오, 연단 품질, 상점 흥정)은 이번에 하지 않는다. 설정 저장 안 함(켤 때마다 꺼짐).

실패로 볼 것
- A: 강제로 넣은 옵션 때문에 공법 화면·전투가 예외를 내거나, 세이브를 불러올 때 공법이 깨진다(실험 E1로 먼저 본다. 깨지면 이 기능은 막고 이유를 기록한다).
- B: NPC의 도주·훔치기·포획이 같이 성공한다. 드롭이 상점·임무 보상 같은 드롭 아닌 곳까지 바꾼다. 종문 초빙 점수 함수가 초빙 밖(종문 내 평가·관계 판정)에도 쓰여 게임이 이상해진다.

## 3. 게임 자료 (어셈블리에서 확인. 동작은 4절에서 실측)

| 줄 | 이름 | 비고(트레이너가 한 것) |
|---|---|---|
| A 옵션 | `MartialData.SetPrefixID(index, number)`, `SetPrefixLevel`, `ConfBattleSkillPrefixValueItem.weight1~11` | 기존 교체와 같은 쓰기. 자리 판정만 건너뜀 |
| 도주 | `UnitActionRoleEscape.CompleteRate(int luck, int footSpeed, int toUnitFootSpeed)`(static float), `isSureComplete`(get/set), `OnCreate` | 트레이너: CompleteRate 접미 100, isSureComplete true |
| 훔치기 | `UnitActionRoleStealItem.CompleteRate(WorldUnitBase unit, WorldUnitBase toUnit)`(static float), `DiscoverRate(WorldUnitBase unit)`(float), `OnCreate` | 트레이너: 100 / 0 |
| 포획 | `ConfDevilDemonParamerBase.get_collectMonstRate`(float), `ConfPotmonSkillStudyItem.get_studyRate`·`get_battleStudyRate`(int) | 트레이너: 접미로 올림 |
| 드롭 | `ConfGameDifficultyValueItem.get_dropRate`(int), `PlayerInfoExtendPropsEx.CanDrop(PropsData, WorldUnitBase)`(static bool) | 트레이너: 10000 / true |
| 종문 초빙 | `FormulaTool.Relation.UnitSchooldScore(WorldUnitBase unit, WorldUnitBase toUnit, int paraA)`(static int), `UnitActionFeedback115`(초빙 응답 행동) | 트레이너: 점수 ≥ 99999 |
| 관계 발전 | `UnitActionRoleRelation.addIntim`(get/set), `isNPCReduceIntim`(get/set) | 트레이너: addIntim ≥ 60, isNPCReduceIntim false |

각 값의 뜻(확률 단위가 0~1인지 0~100인지, 판정이 어디서 쓰이는지)은 미확인이다.

## 4. 설계 전 실험

회차 이름 `tb2-1020a`부터, 고정본(`-Fixture base`). 실험 명령은 `Dev/DevCommandsTrainerBatch2.cs`(배포 제외). 결과는 05 문서 67절에 남기고 이 문서를 고친다.

| 실험 | 방법 | 확인할 것 |
|---|---|---|
| E1 옵션 자리 강제 | 고정본 공법 하나의 자리 하나에 가중치 0 옵션을 `SetPrefixID`로 넣고, 게임 공법 화면(`PlayerInfo` 공법 쪽) 캡처, 전투에서 그 공법 발동(`bcast`), 저장·재시작 뒤 값 | 화면에 그 옵션 글이 나오는지, 예외가 없는지, 효과가 붙는지(가능하면), 저장 후 남는지 |
| E2 도주 | 기록용 접두로 `CompleteRate` 반환값과 호출 시점, `isSureComplete`를 본다. 전투 밖 NPC에게서 도주 행동(`UnitActionRoleEscape`)을 만들어 성공 여부 | 반환값 단위(0~1 / 0~100), 접미로 바꾸면 성공하는지 |
| E3 훔치기 | `UnitActionRoleStealItem`을 가까운 NPC의 아이템에 만들어 `CompleteRate`·`DiscoverRate` 기록, 바꾼 뒤 아이템이 넘어오는지 | 단위, 발각 판정 |
| E4 포획 | 연요호 포획 판정(`collectMonstRate`)이 언제 읽히는지, 호요 기술 습득 확률 칸 | 플레이어 판정만인지(설정 표 값이라 NPC와 공유) |
| E5 드롭 | 전투 처치 뒤 드롭 판정에서 `dropRate`·`CanDrop` 호출 기록 | 상점·보상 등 다른 곳에서도 읽히는지 |
| E6 초빙·관계 | `UnitSchooldScore` 호출 위치 기록(초빙 행동 안팎), `UnitActionRoleRelation`의 `addIntim` 뜻 | 초빙 행동 안에서만 바꿀 수 있는지(1묶음의 앞뒤 표시 방식), 관계 발전 "성공"이 무엇인지 |

## 5. 화면

### 5.1 A — 공법 탭 옵션 교체 화면

- 가중치 0이 유일한 사유인 줄: "불가: 이 자리에는 나올 수 없는 옵션" 대신 "자리 밖: 이 자리에는 나오지 않는 옵션(강제로 넣을 수 있음)"을 경고색으로 보이고, 단추를 `강제`(`ReplaceForce<번호>`)로 켠다. 두 번 누르기(`Confirm`): 첫 누름 문구 "옵션 n번은 이 자리에 나오지 않는 옵션입니다. 게임이 원래 만들지 않는 조합이라 효과가 나지 않을 수 있습니다. 한 번 더 누르면 넣습니다."(E1 결과로 문구를 고친다).
- 중복 한도·선행·배타 사유가 있으면 지금처럼 막는다(강제 단추 없음).

### 5.2 B — 새 탭 `성공률`

- 탭 줄 열일곱째(`Tab17`, 번호 고정. 탭 줄은 18개까지 들어간다, 47절). 배율 탭은 이미 12줄이라 더하지 않는다(내 판단).
- 줄 여섯, 각 줄은 이름·켬/끔 단추(`Success<키>`, 글 `켜짐`/`꺼짐`)·설명. 키: `Escape`(도주), `Steal`(훔치기), `Capture`(포획), `Drop`(드롭), `Recruit`(종문 초빙), `Relation`(관계 발전). 위에 `모두 끄기`(`SuccessReset`).
- 켠 줄은 화면 위쪽 가운데 표시(배율과 같은 자리)에 짧은 이름으로 나온다.
- 각 줄의 설명은 E2~E6 결과로 적는다(무엇이 성공하는지, NPC는 그대로인지).

## 6. 동작 규칙

- **A**: `MartialService.ReplaceBlockReason`을 둘로 나눈다. 자리 판정(`SlotBlocked`)과 나머지(`RuleBlockReason`). `ReplacePrefix(unit, sole, index, item, bool force)` — `force`이면 자리 판정만 건너뛴다. 이력 항목 끝에 `(자리 강제)`. 프리셋 적용(`PresetService`)의 옵션 검사도 자리 판정만 건너뛴다(7.1절: 내보낸 파일이 다시 적용되게).
- **B**: `Services/SuccessService.cs`. 줄마다 켬/끔 bool과 처음 켤 때 한 번 거는 Harmony 패치(1묶음 `RateGuards`와 같은 방식). 판정 함수에 접미로 반환값을 바꾸되, 플레이어의 시도일 때만이다. 정적 판정 함수(인자에 유닛이 있는 것)는 인자로 판별하고, 인자가 없는 설정 표 값(`dropRate`, `collectMonstRate`)은 플레이어 행동의 앞뒤 표시 안에서만 바꾼다(E4·E5로 표시할 행동을 정한다). 표시할 곳을 찾지 못한 줄은 만들지 않고 05 문서에 이유를 남긴다.
- 상태는 저장하지 않는다. `RateService.Reset`(배율 `모두 1배로`)과는 따로 `SuccessReset`이 끈다.

## 7. 검증

- 검사 `ui-check.ps1`: `prefixforce`(A: 자리 밖 줄의 문구·강제 단추, 두 번 누르기, 공법의 옵션 번호, 중복·조건 위반은 여전히 막힘, 이력), `success`(B: 탭과 줄, 켬/끔, 각 줄의 판정을 개발 명령으로 불러 켜기 전 실패 가능·켠 뒤 성공, NPC 판정은 그대로).
- 기존 검사 `tabs`(탭 수와 `Tab17`), 공법 탭 관련(`actions`), `preset-check`의 옵션 규칙이 깨지지 않는지.
- 배포 구성 경고 0, `-End`의 `SAVE RESTORED AND IDENTICAL`.

## 7.1 검토 뒤 정한 것(내 판단)

- **"플레이어의 시도"의 뜻**:
  - 도주는 세계에서 플레이어가 NPC를 피해 달아나는 행동(`UnitActionRoleEscape`, 행동의 유닛이 플레이어)이다. 줄 이름은 `도주`로 하고 "전투"를 뺀다. 플레이어를 쫓는 NPC를 실패시키는 것은 넣지 않는다.
  - 종문 초빙은 플레이어가 NPC를 자기 종문에 들이는 쪽이다(초빙하는 쪽이 플레이어).
  - 드롭은 플레이어가 끝낸 전투의 적 드롭 판정이다(아군이 처치한 적 포함, 전투 단위). E5에서 그 안으로만 좁힐 수 없으면 줄을 만들지 않는다.
- **한 줄의 판정이 일부만 플레이어로 좁혀질 때**: 되는 부분만 넣고, 설명에 범위를 적는다(예: "요수 포획만. 호요 기술 습득은 그대로"). 줄을 나누지 않는다.
- **강제 옵션과 프리셋**: 편집기가 넣을 수 있는 상태를 내보낸 파일은 다시 적용될 수 있어야 한다. 그래서 프리셋 적용의 옵션 검사도 자리 판정만 건너뛴다(중복·조건은 그대로). 위 6절의 "프리셋 검사는 그대로"를 이것으로 바꾼다.
- **E1의 중간 결과**:
  - 불러온 뒤 게임이 그 옵션을 다시 뽑거나 지우면 A를 만들지 않고, 이유를 05 문서에 남긴다.
  - 글은 나오는데 효과를 확인하지 못하면 A를 만든다. 첫 누름 문구에 "효과가 나지 않을 수 있습니다"를 남긴다.

## 8. 결정한 것(내 판단) / 빌더에게 남긴 것

내 판단: A는 자리 판정만 넘을 수 있고 중복·조건은 그대로 막는다, 프리셋 검사는 그대로, 두 번 누르기. B는 새 탭(배율 탭이 꽉 참), 켬/끔만, 플레이어만, 저장 안 함, 판정 함수 접미 + 표시 방식, 표시할 곳이 없는 줄은 만들지 않는다.
빌더에게: 줄 높이·글 배치, 실험 명령의 출력 꼴.
