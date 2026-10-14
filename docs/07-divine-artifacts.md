# 07. 신기(시작 신기 셋)와 신안 조사 (2026-10-14)

사용자 요청: "게임 시작시 얻는 3가지 신기와 신기 정보를 확인하고 신기 기능 등에 대한 모드 관리 데이터 리서치".

근거는 세 가지다. 어셈블리 목록(`tools/research/types.txt`), 게임의 설정 표 사본(`Mod\modFQA\配置修改教程\配置（只读）Json格式\*.json`, 읽기 전용), 앞서 게임에서 내보낸 표(`.devremote\dump-roleCreateFeature.txt`, `dump-itemProps.txt`, 한국어 이름). 게임을 띄워 확인하지 못한 것은 4절에 "미확인"으로 모았다(조사하던 때 사용자의 게임이 켜져 있어 회차를 열지 않았다).

## 1. 신기 셋은 캐릭터 생성 때 고르는 선천운명이다

| 신기 | 선천운명(번호) | 게임의 설명(한국어) | 관리 형식(`g.world`의 필드) | 설정 표 |
|---|---|---|---|---|
| 식혼검 | 식혼검(2804) | "게임 시작 시, 식혼마검을 보유한 상태로 진행됩니다. 마도 수치 +200, 행운 -10." | `SoulDevourSwordMgr soulDevourSword` | `SoulDevourSwordParamer`(70), `SoulDevourSwordLevel`(3), `SoulDevourSwordDevour`(10), `SoulDevourSwordEffects`(5), `ItemSoulDevourSword`(3) |
| 음양안 | 음양안(2805) | "행운 +20, 오성 +10, 시야 +1. 신비로운 두 눈을 지니고 있어 각종 기이한 영체를 볼 수 있다." | `YinyangEyeMgr yinyangEye` | `yinyangEyeParamer`(45), `yinyangEyeSoul`(38) |
| 연화검 | 연화검의(2806) | "검법 자질 +30, 공격력 +2. 반생 광련을 보유하고 있어 천지의 영기를 자동으로 흡수하여 …" | `LotusSwordMgr lotusSword` | `lotusSwordParamer`(33), `lotusSwordEffects`(4), `lotusSwordSeedGrow`(8), `ItemLotusSeed`(11) |

- 세 관리 형식은 모두 `WorldMgr`(= `g.world`)의 필드다. 신기마다 `Init`, `ReadData`, `OnSaveData`, 달 처리(`OnRunEnd` 등)를 가진다.
- 고정본 세이브의 인물(신환)은 선천운명에 식혼검(2804)을 가지고 있다(회차 `grade-1014d`의 개발 명령 출력 `born=[2804,1526,2010,…]`). 연화검의(2806)는 프리셋 02(수검)가 붙이는 선천운명이다.
- 식혼검과 연화검은 실물 아이템이 따로 있다. 식혼검은 단계마다 아이템이 바뀌고(식혼검·응봉 8104130 → 유영 8104131 → 혼멸 8104132), 연화검의 반생 광련은 8104001이다(`lotusLevelPropId` 8104001|8104002|8104003).

## 2. 신기마다의 구조

### 식혼검 (`SoulDevourSwordMgr`)

저장 자료 `SoulDevourSwordMgr.Data`(세계 하나에 하나): `state`, `unitId`(가진 인물), `pointX`·`pointY`(떨어져 있을 때의 위치), `exp`, `level`, `atk`(쌓인 공격력), `getMonth`, `lastDevourMonth`, `nextDevourMonth`, `exposureUnitIds`·`exposureState`(정체가 드러난 상태), `effectsUnlockProcess`(효과 해금 진행), `swordWakeUp`, `bornFeatureExist`, `featureExist`, `logs` 등.

| 항목 | 설정 |
|---|---|
| 흡혼 | 인물을 죽이면 원혼을 흡수한다(`SoulDevour(killer, dieUnit)`). 대경지마다 경험치와 공격력이 오른다(`SoulDevourSwordDevour`: 대경지 1은 경험치 +2·공격력 +2 … 대경지 10은 +203·+300). 흡수 간격 3개월 |
| 주기 | 24개월(`swordDevourInterval`)마다 흡수해야 하고 아니면 경고(6·3개월 전) 뒤 반격(던전 193)한다. 운명 "식혼검"(9197, 후천)의 설명: "일정 주기마다 수선자의 원혼을 흡수해야 하며, 그렇지 않으면 반격을 당하게 된다" |
| 단계 | 3단계. 필요 경험치 0·50·550(`SoulDevourSwordLevel`; `ItemSoulDevourSword`는 0·50·250으로 다르다). 단계마다 던전 28041~28043 |
| 공격력 | 상한 1200(`soulDevourSwordAtkLimit`) |
| 효과 해금 | 5개(`SoulDevourSwordEffects`): ① 흡혼으로 공격력 증가 ② 1위 검수 원혼 3개 → "삼절검기"(효과 2804001) ③ 마도 150 이상 천교 원혼 6개 → "명계의 빛"(2804002) ④ 종주 원혼 1개 → 절기 때 검혼의 기술(2804044) ⑤ 신통 때 식혼진, 치명상 1회 막기(2804004·2804005). 영어 설명 기준. 한국어 표기는 미확인 |
| 노출 | 정체가 드러나면 관계가 끊기거나 호감 -2000, 체포 던전(194) |
| 잃음·떨어짐 | 죽으면 칼이 떨어져 다른 인물이 주울 수 있다(`Drop`, `UnitLoseSword`, `UnitGetSword`). 떨어져도 경험치·공격력의 67%, 진행의 50%를 남긴다 |
| NPC | NPC도 칼을 가질 수 있다(`NpcSwordDoKill`, 8개월 간격) |

### 음양안 (`YinyangEyeMgr`)

저장 자료 `YinyangEyeData`: `soulsDic`(지도에 있는 영체, `YinyangEyeDataSoul`: `soulId`, `unitId`, `point`, `isActive`, `taskSoleId`, `activeMonth`), `dieUnitSouls`, `randomUnitSoul`, `specailSoul`, `monthAddSoulMax`, `soulLastCreateMonth`.

| 항목 | 설정 |
|---|---|
| 영체 | 38종(`yinyangEyeSoul`). 1xxx 잔혼, 2001, 3001~3028(선혼, 악혼, 상혼, 겁혼, 용혼, … 신통혼). 죽은 인물 근처(5칸)와 무작위로 생기고 지도에 20개까지 |
| 상호작용 | 영체를 누르면 효과(`SoulAct`). 예: 3018은 대경지마다 공격력(1~304, 상한 4~1216), 3028은 신통 기술(32111 마살도, 36112 마인장, 37111 충허지, 37112 신맥지, 62112 복령우, 64112 역공계 가운데 하나)과 운명 9219 |
| 임무 | 영체 임무 132001, 완료 보상 1410006 |
| 시야 | `additionalVision` 1 |

### 연화검 (`LotusSwordMgr`)

저장 자료: 세계 쪽 `LotusSwordData.seedDatas`(지도의 연자들), 인물 쪽 `DataUnit.LotusSwordData`(`seedExp`, `seedAddWeight`, `lastSeed1008Month`).

| 항목 | 설정 |
|---|---|
| 효과 해금 | 대경지로 4개(`lotusSwordEffects`): 축기(2) 검 신법 동안 체력 회복(280631·280642), 결정(3) 검 절기 때 연꽃 발사(280611), 금단(4) 검 신법 동안 주변 지속 피해(280641), 구령(5) 검 절기 때 연꽃 여럿(280621) |
| 연자 | 달마다 경험치 10(`monthAddExp`), 120이 차면 연자가 맺힌다. 격자의 속성(`lotusSwordSeedGrow`)이 성장과 연자 종류의 가중치를 바꾼다 |
| 연자 8종 | 빨간색 8104201(공법 복제), 주황색 8104202(무작위 도구), 노란색 8104203(몰래 주기), 초록색 8104204, 청색 8104205(다음 운유곡 기령이 신급), 파란색 8104206, 자주색 8104207(쌍수 때 심득), 채색 8104208(36개월에 한 번) |
| 광련 단계 | 대경지 1~2 → 1, 3~4 → 2, 5 이상 → 3(`untiGradeToLotusLevel`) |

### 신안 (`GodEye`, 신기 셋과 별개)

- 세계 쪽 자료 `DataWorld.World.godEyeData`(`godEyeGrade`, `skillsID`(익힌 우두머리 기술), `equipSkillID`, `bossGrade`, `isDamage`, `repairMonth`, `viewCompleteUnitID` 등). 전투 단축키(`battleGodEye`)와 지도 화면(`GodEye`, `GodEyeList`, `GodEyeBossSkill`)이 따로 있다.
- 우두머리의 기술을 보고 익혀 쓰는 눈이다(`GodEyeSkills` 95줄, `bossID`로 묶임). 단계 올리기 비용 2000~100000(`GodEyeBaseCost`), 추가 비용(`GodEyeExtraCost`), 달마다 3 소모(`monthCost`), 손상과 수리(`isDamage`, `Repair`).
- 어떻게 얻는지는 미확인이다(시작 신기의 선천운명 셋에는 없다).

## 3. 모드 관리 데이터 — 무엇을 어떻게 바꿀 수 있나

| 방법 | 대상 | 비고 |
|---|---|---|
| 공식 모드의 설정 표(`ModExcel`, 02 문서 2절) | 위 표들의 행을 고치거나 더함. 예: 식혼검 주기·상한·흡혼 수치, 연자 확률, 음양안 영체 가중치, 신안 비용 | 게임 시작 때 읽히는 설정이라 세이브를 바꾸지 않는다. 우리 모드는 지금 `ModCode`(DLL)만 쓴다 |
| 선천운명 붙이기(운명 탭) | 2804·2805·2806을 선천운명으로 | 선천운명만 붙이는 것으로 신기 자료(칼, 영체, 연자)가 생기는지는 미확인. 식혼검은 `UnitGetSword`, 연화검은 광련 아이템이 따로 있다 |
| 아이템 지급(자원 탭) | 식혼검·응봉/유영/혼멸, 광련, 연자 8종 | 아이템만으로 신기가 동작하는지 미확인 |
| 저장 자료 직접 쓰기(편집기 후보) | 식혼검의 `exp`·`level`·`atk`·`nextDevourMonth`·`effectsUnlockProcess`, 음양안의 영체, 연화검의 `seedExp`, 신안의 `godEyeGrade`·`skillsID` | 관리 형식에 `AddExp`, `UpdateSwordLevelLuck`, `SwordUnlockEffect`, `CreateSpecialSoul`, `LotusSeedGrow` 같은 정식 함수가 있다. 직접 쓰기보다 이 함수를 먼저 실험한다 |

## 4. 게임에서 확인할 것 (미확인)

게임이 꺼져 있을 때 고정본 회차에서 한다.

1. 고정본의 신기 자료: `g.world.lotusSword`, `soulDevourSword`, `yinyangEye`의 지금 값과 인물 쪽 `lotusSwordData`(실험 명령 `wsnap`, 브랜치 `feat/artifact-research`).
2. 한국어 표기: 효과 해금의 설명, 영체 38종의 이름, 신안 기술의 이름(`pdump`로 번역된 표를 내보낸다).
3. 선천운명 2804·2805를 다른 인물에게 붙였을 때 신기 자료가 생기는지(식혼검의 `unitId`가 그 인물이 되는지, 영체가 보이는지).
4. 식혼검의 `AddExp`, 연화검의 `LotusSeedGrow`, 음양안의 `CreateSpecialSoul`을 불렀을 때 바뀌는 것(정식 함수로 편집기 기능을 만들 수 있는지).
5. 신안을 얻는 경로.
