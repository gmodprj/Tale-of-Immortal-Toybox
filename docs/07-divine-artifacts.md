# 07. 시작 신기 셋(쌍어패·호천안·연요호) 조사 (2026-10-14)

사용자 요청: "게임 시작시 얻는 3가지 신기와 신기 정보를 확인하고 신기 기능 등에 대한 모드 관리 데이터 리서치". 사용자가 게임의 신기 선택 화면(호천안, 쌍어패, 연요호) 캡처를 주었다.

근거: 어셈블리 목록(`tools/research/types.txt`), 게임 설정 표 사본(`Mod\modFQA\配置修改教程\配置（只读）Json格式\*.json`, 읽기 전용, 영어 번역은 `LocalText.json`), 앞서 게임에서 내보낸 한국어 표(`.devremote\dump-*.txt`). 게임을 띄워 확인하지 못한 것은 5절에 "미확인"으로 모았다(조사하던 때 사용자의 게임이 켜져 있어 회차를 열지 않았다).

처음에는 식혼검·음양안·연화검의(특수 선천운명)를 시작 신기로 잘못 보았다. 그것들은 6절에 따로 적는다.

## 1. 신기의 종류와 저장 위치

게임은 신기를 열거형 `GameAnimaWeapon`으로 나눈다.

| 값 | 이름(어셈블리) | 게임 화면 | 게임 화면의 설명(요약) |
|---|---|---|---|
| 1 | `PiscesPendant` | 쌍어패 | 점수를 보유했다면 다시 태어날 수 있다. 둘로 나누어 다른 사람에게 증여하면 서로를 보호한다 |
| 2 | `HootinEye` | 호천안 | 세계를 더 넓게 보고 다른 수선자의 가방을 꿰뚫어 본다. 요수의 약점을 간파하고 그 기술을 쓴다. 위험할 때 탈출. 모두 영석으로 충전한다 |
| 3 | `DevilDemon` | 연요호 | 요수를 집어넣어 연화하고 지성을 얻게 해 부릴 수 있게 한다 |

- 가진 신기: 세계 자료 `DataWorld.World.animaWeapons`(`List<GameAnimaWeapon>`). 목록이라 여럿을 가질 수 있는 모양이다(게임에서 여럿이 되는 경로는 미확인).
- 이름·설명·배경 글: 설정 표 `AnimaWeapon`(`ConfAnimaWeaponItem`: `name`, `icon`, `desc`, `backstory`). 이 표의 JSON 사본은 게임 폴더에 없다.
- 새 게임 화면의 선택: `curAnimaWeapon`, `openAnimaWeapons`(게임 시작 연출 `UIIntoGameDrama`). 캐시(`CacheMgr.CacheData`)에도 `animaWeapons`(정수 목록)가 있다(열린 신기의 기록으로 보인다, 미확인).
- 공식 모드의 조건 `checkAnimaWeapon`("플레이어가 어떤 신기를 가졌는가"), `CheckAnimaWeaponSkill`이 있다. 모드 설정 도구가 `AnimaWeapon` 표의 줄을 모드 항목으로 만들 수 있다(`CreateModItem(ConfAnimaWeaponItem)`).

## 2. 쌍어패 (`PiscesPendant`)

| 항목 | 내용 |
|---|---|
| 세계 자료 | `DataWorld.World.piscesData`(`PiscesData`): `npcUnitId`(반쪽을 준 NPC), `IsGive`, `lastGiveMonth`, `lastRecallMonth`, `lastTransToNpcMonth`, `lastBattleSummonMonth`, `IsTransCD`. 함수 `GivePisces(unit)`, `RecallPisces()` |
| 부활 | 세계 자료의 `resurgeCount`, `resurgePower1`, `resurgePower2`, `resurgePowerCount`(부활 점수로 보인다, 이름으로 추정). "점수를 보유했다면 다시 태어날 수 있다"와 대응한다. 실제 뜻은 미확인 |
| 전투 | `PiscesPendantSkillBase`(플레이어 전투 단위의 `piscesSkillBase`): 반쪽을 준 NPC(`humanNPC`)를 전투에 부른다. 재사용 대기 |
| 설정 `PiscesParam`(11) | NPC 호감 한도 180, 플레이어 이동 대기 1, NPC 소환 대기 3개월, NPC에게 운명 150(`npcAddLuck`), 주기·회수·관계 끊김·부활·소환 사망의 대화 번호 |
| 세계 처리 | `WorldSpecialPiscesPendant`(행동 이벤트를 듣는다) |

## 3. 호천안 (`HootinEye`, 설정 표 이름은 `GodEye`)

| 항목 | 내용 |
|---|---|
| 세계 자료 | `DataWorld.World.godEyeData`(`GodEyeData`): `godEyeGrade`(단계), `skillsID`(익힌 요수 기술), `equipSkillID`(장착한 기술), `bossGrade`(요수별 단계), `number`, `isDamage`·`repairMonth`(손상과 수리), `viewFailedUnitID`·`viewCompleteUnitID`(가방 엿보기의 기록). 함수 `IsCanUpGrade`, `IsCanUse`, `Repair`, `IsSkillUnlock`, `IsEquipSkill` |
| 전투 | `WorldSpecialGodEye`: 요수의 약점(`Foible`)을 보여 주고, 기술을 익힌다(`StudySkill`). 플레이어 단위의 `eyeSkillBase`, 단축키 `battleGodEye`, 지도 화면 단축키 `mapGodEyeUI` |
| 화면 | `GodEye`, `GodEyeList`, `GodEyeBossSkill` |
| 설정 | `GodEyeSkills`(95: 요수 번호 `bossID`별 기술), `GodEyeBaseCost`(단계 비용 2000 … 100000), `GodEyeExtraCost`(추가 비용 0 … 500000), `GodEyePar`(17: 기술을 볼 확률 50, 기술 시간 30/40/50, 시야 +1, 가방 엿보기 확률 20·실패 대기 3·보이는 아이템 3~5, 달마다 비용 3, 특수 요수 16종과 그 기술의 체력 비율, 회복 10~30, 학습 대기 10000) |

## 4. 연요호 (`DevilDemon`, 요수는 `Potmon`)

| 항목 | 내용 |
|---|---|
| 세계 자료 | `DataWorld.World.devilDemonData`(`DevilDemonData`): `isDamage`, `brokenCount`, `repairMonth`, `potmonData`(`PotmonData`: `potMonList`(가진 요수), `giveUpPotMonList`, `martialList`(모은 요수 무공), `potMonTeams`, `potMonOutLogs`, `leavePotmons`, `unitRace`, `dataProp`) |
| 요수 하나 | `PotMonUnitData`: `id`(요수 종류), `soleId`, `grade`, `exp`, `health`, `state`, `converState`(인간형 전환), `battleSkills`·`battleSkillMax`, `lifeSkills`, `sex`, `fixName`, `furryGradeId`, `unitId` |
| 관리 | `PotMonMgr`: `AddPotMon(id, grade, isAdd)`, `EquipPotMon`, `UnEquipPotMon`, `CollectMartial`, `Refining`(연화), `PotmonUpgrade`, `PotmonConver`, `PotmonContract`, `PotmonAddExp`, `PotmonGiveUp` |
| 설정 | `DevilDemonBase`(단계 10: 모을 요수 10~28, 생활 팀 1~4, 연요 수 10~55), `DevilDemonParamer`(수리 1개월, 활동 60, 요수 수집 확률 50, 수리 최대 6), `DevilDemonTeam`(11), `DevilDemonWork`(14)·`WorkExp`(120: 파견), `DevilDemonAbsorb`(21) |
| 요수 설정 | `PotmonBase`(45종, 예: 1001(영어 표기 Evergreen's Spirit. 한국어 표기 미확인)), `PotmonGradeLevel`(10단계, 경험치 0~203000), `PotmonFurryGradeLevel`(30), `PotmonSkill`(241), `PotmonBattleSkill`(163), `PotmonSkillLife`(492), `PotmonSkillStudy`(1717), `PotmonSkillAid`(113), `PotmonPropertyScale`(48), `PotmonParamer`(27: 처음 요수 1001, 처음 기술 2개, 계약 3, 경험치 약 2823001·승급 약 2823002·치료 약 2823003·전환 약 2823004) |

## 5. 모드 관리 데이터 — 무엇을 어떻게 바꿀 수 있나

| 방법 | 대상 | 비고 |
|---|---|---|
| 공식 모드의 설정 표(`ModExcel`, 02 문서 2절) | 위 표들의 줄을 고치거나 더함: 신기의 이름·설명(`AnimaWeapon`), 호천안 비용·확률, 쌍어패 대기·호감 한도, 연요호 단계·요수·기술 | 게임 시작 때 읽는 설정이라 세이브를 바꾸지 않는다. 우리 모드는 지금 코드(DLL)만 쓴다 |
| 신기 목록 바꾸기(편집기 후보) | `World.animaWeapons`에 1·2·3을 더하거나 빼기 | 더하는 것만으로 신기가 동작하는지(세계 처리 `WorldSpecialGodEye` 등이 처음부터 있는지) 미확인 |
| 신기 상태 고치기(편집기 후보) | 호천안 `godEyeGrade`·`skillsID`·`isDamage`, 연요호 `isDamage`·요수 추가(`PotMonMgr.AddPotMon`)·경험치(`PotmonAddExp`), 쌍어패 대기 시각·부활 점수 | 정식 함수(`Repair`, `AddPotMon`, `PotmonUpgrade`)를 먼저 실험한다 |

## 6. 특수 선천운명 셋 (신기와 별개)

선천운명 고르기에서 고르는 특별한 운명이다. 각각 세계 관리자(`g.world`)에 관리 형식이 있다.

| 운명 | 번호 | 게임의 설명 | 관리 형식, 설정 표 |
|---|---|---|---|
| 식혼검 | 2804 | 게임 시작 시 식혼마검 보유, 마도 +200, 행운 −10 | `SoulDevourSwordMgr`(자료: `unitId`, `exp`, `level`, `atk` 상한 1200, `effectsUnlockProcess` …). 인물을 죽여 원혼을 흡수, 3단계(응봉 8104130·유영 8104131·혼멸 8104132), 24개월 안에 흡수하지 않으면 반격, 효과 5개 해금. `SoulDevourSword*` 표 |
| 음양안 | 2805 | 행운 +20, 오성 +10, 시야 +1. 기이한 영체를 볼 수 있다 | `YinyangEyeMgr`(자료: `soulsDic` …). 영체 38종(최대 20개)을 눌러 효과. `yinyangEye*` 표 |
| 연화검의 | 2806 | 검법 자질 +30, 공격력 +2. 반생 광련 | `LotusSwordMgr`(세계 `seedDatas`, 인물 `lotusSwordData.seedExp`). 대경지로 검 신법·절기 효과 4개, 8색 연자. `lotusSword*` 표 |

고정본 세이브의 인물(신환)은 선천운명에 식혼검(2804)을 가지고 있다(회차 `grade-1014d`의 개발 명령 출력 `born=[2804,1526,2010,…]`). 연화검의(2806)는 프리셋 02(수검)가 붙이는 선천운명이다.

## 7. 게임에서 확인할 것 (미확인)

게임이 꺼져 있을 때 고정본 회차에서 한다(실험 명령 `wsnap`, 브랜치 `feat/artifact-research`).

1. 고정본의 신기: `World.animaWeapons`의 값, `godEyeData`·`piscesData`·`devilDemonData`의 지금 값.
2. 한국어 표기: `AnimaWeapon` 셋의 이름과 설명, 호천안 기술, 요수 이름.
3. `animaWeapons`에 신기를 더했을 때 그 신기의 화면·전투 기능이 열리는지.
4. 부활 점수(`resurge*`)의 뜻.
5. 정식 함수(`GodEyeData.Repair`, `PotMonMgr.AddPotMon`, `PiscesData.GivePisces`)를 불렀을 때 바뀌는 것.
