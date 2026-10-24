# 07. 시작 신기 셋(쌍어패·호천안·연요호) 조사 (2026-10-14)

사용자 요청: "게임 시작시 얻는 3가지 신기와 신기 정보를 확인하고 신기 기능 등에 대한 모드 관리 데이터 리서치". 사용자가 게임의 신기 선택 화면(호천안, 쌍어패, 연요호) 캡처를 주었다.

근거: 어셈블리 목록(`tools/research/types.txt`), 게임 설정 표 사본(`Mod\modFQA\配置修改教程\配置（只读）Json格式\*.json`, 읽기 전용, 영어 번역은 `LocalText.json`), 게임에서 내보낸 한국어 표(`.devremote\dump-*.txt`). 1~6절은 설정 표와 어셈블리로 한 조사이고, 7절은 2026-10-14 고정본 회차(`art-1014b`, `art-1014c`)에서 게임으로 확인한 것이다. 실험 명령은 브랜치 `feat/artifact-research`의 `Dev/DevCommandsArtifact.cs`에 있다.

처음에는 식혼검·음양안·연화검의(특수 선천운명)를 시작 신기로 잘못 보았다. 그것들은 6절에 따로 적는다.

## 1. 신기의 종류와 저장 위치

게임은 신기를 열거형 `GameAnimaWeapon`으로 나눈다.

| 값 | 이름(어셈블리) | 게임 화면 | 게임 화면의 설명(요약) |
|---|---|---|---|
| 1 | `PiscesPendant` | 쌍어패 | 점수를 보유했다면 다시 태어날 수 있다. 둘로 나누어 다른 사람에게 증여하면 서로를 보호한다 |
| 2 | `HootinEye` | 호천안 | 세계를 더 넓게 보고 다른 수선자의 가방을 꿰뚫어 본다. 요수의 약점을 간파하고 그 기술을 쓴다. 위험할 때 탈출. 모두 영석으로 충전한다 |
| 3 | `DevilDemon` | 연요호 | 요수를 집어넣어 연화하고 지성을 얻게 해 부릴 수 있게 한다 |

- 가진 신기: 세계 자료 `DataWorld.World.animaWeapons`(`List<GameAnimaWeapon>`, 코드에서는 `g.data.world.animaWeapons`). 셋을 모두 넣으면 셋 다 동작한다(7.2절). 게임 안에서 여럿이 되는 경로는 미확인.
- 이름·설명·배경 글: 설정 표 `AnimaWeapon`(`g.conf.animaWeapon`, `ConfAnimaWeaponItem`: `name`, `icon`, `desc`, `backstory`). 이 표의 JSON 사본은 게임 폴더에 없다. 게임에서 읽은 한국어 이름은 1 쌍어패, 2 호천안, 3 연요호(7.1절).
- 새 게임 화면의 선택: `curAnimaWeapon`, `openAnimaWeapons`(게임 시작 연출 `UIIntoGameDrama`). 세이브 칸의 메타(`g.cache.curCache.data.animaWeapons`, 정수 목록)는 저장할 때 세계 자료의 목록을 그대로 옮겨 적는다(7.2절). 불러오기 화면의 신기 그림이 이것을 쓰는 것으로 보인다(`UpdateAnimaWeaponIcon(CacheData)`, 화면은 미확인).
- 공식 모드의 조건 `checkAnimaWeapon`("플레이어가 어떤 신기를 가졌는가"), `CheckAnimaWeaponSkill`이 있다. 모드 설정 도구가 `AnimaWeapon` 표의 줄을 모드 항목으로 만들 수 있다(`CreateModItem(ConfAnimaWeaponItem)`).

## 2. 쌍어패 (`PiscesPendant`)

| 항목 | 내용 |
|---|---|
| 세계 자료 | `DataWorld.World.piscesData`(`PiscesData`): `npcUnitId`(반쪽을 준 NPC), `IsGive`, `lastGiveMonth`, `lastRecallMonth`, `lastTransToNpcMonth`, `lastBattleSummonMonth`, `IsTransCD`. 함수 `GivePisces(unit)`, `RecallPisces()` |
| 부활 | 세계 자료의 `resurgeCount`가 **쌍어옥패 점수**다(7.4절, 게임 화면으로 확인). 전투에서 빈사가 되면 점수를 써서 되살아난다(`battle_fuhuotishi1` "Cost: {0} Bagua Jade Points"). `resurgePower1`·`resurgePower2`·`resurgePowerCount`는 문구로 보아 생기·사기(사람을 구하면 생기, 죽이면 사기가 모인다는 `resurgeInfo_shengqitishi`·`siqitishi`)와 그 환산으로 보이나 화면에서 보지 못했다(미확인) |
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
| 요수 설정 | `PotmonBase`(45종, 게임 화면의 이름은 "호요". 예: 1001 수요의 영, 1002 약령의 영, 1003 시왕의 영, 1004 대해의 영), `PotmonGradeLevel`(10단계, 경험치 0~203000), `PotmonFurryGradeLevel`(30), `PotmonSkill`(241), `PotmonBattleSkill`(163), `PotmonSkillLife`(492), `PotmonSkillStudy`(1717), `PotmonSkillAid`(113), `PotmonPropertyScale`(48), `PotmonParamer`(27: 처음 요수 1001, 처음 기술 2개, 계약 3, 경험치 약 2823001·승급 약 2823002·치료 약 2823003·전환 약 2823004) |

## 5. 모드 관리 데이터 — 무엇을 어떻게 바꿀 수 있나

| 방법 | 대상 | 비고 |
|---|---|---|
| 공식 모드의 설정 표(`ModExcel`, 02 문서 2절) | 위 표들의 줄을 고치거나 더함: 신기의 이름·설명(`AnimaWeapon`), 호천안 비용·확률, 쌍어패 대기·호감 한도, 연요호 단계·요수·기술 | 게임 시작 때 읽는 설정이라 세이브를 바꾸지 않는다. 우리 모드는 지금 코드(DLL)만 쓴다 |
| 신기 목록 바꾸기(편집기 후보) | `World.animaWeapons`에 1·2·3을 더하거나 빼기 | 더하는 것만으로 신기가 동작하는지(세계 처리 `WorldSpecialGodEye` 등이 처음부터 있는지) 미확인 |
| 신기 상태 고치기(편집기 후보, 게임 확인 결과는 8절) | 호천안 `godEyeGrade`·`skillsID`·`isDamage`, 연요호 `isDamage`·요수 추가(`PotMonMgr.AddPotMon`)·경험치(`PotmonAddExp`), 쌍어패 대기 시각·부활 점수 | 정식 함수(`Repair`, `AddPotMon`, `PotmonUpgrade`)를 먼저 실험한다 |

## 6. 특수 선천운명 셋 (신기와 별개)

선천운명 고르기에서 고르는 특별한 운명이다. 각각 세계 관리자(`g.world`)에 관리 형식이 있다.

| 운명 | 번호 | 게임의 설명 | 관리 형식, 설정 표 |
|---|---|---|---|
| 식혼검 | 2804 | 게임 시작 시 식혼마검 보유, 마도 +200, 행운 −10 | `SoulDevourSwordMgr`(자료: `unitId`, `exp`, `level`, `atk` 상한 1200, `effectsUnlockProcess` …). 인물을 죽여 원혼을 흡수, 3단계(응봉 8104130·유영 8104131·혼멸 8104132), 24개월 안에 흡수하지 않으면 반격, 효과 5개 해금. `SoulDevourSword*` 표 |
| 음양안 | 2805 | 행운 +20, 오성 +10, 시야 +1. 기이한 영체를 볼 수 있다 | `YinyangEyeMgr`(자료: `soulsDic` …). 영체 38종(최대 20개)을 눌러 효과. `yinyangEye*` 표 |
| 연화검의 | 2806 | 검법 자질 +30, 공격력 +2. 반생 광련 | `LotusSwordMgr`(세계 `seedDatas`, 인물 `lotusSwordData.seedExp`). 대경지로 검 신법·절기 효과 4개, 8색 연자. `lotusSword*` 표 |

고정본 세이브의 인물(신환)은 선천운명에 식혼검(2804)을 가지고 있다(회차 `grade-1014d`의 개발 명령 출력 `born=[2804,1526,2010,…]`). 연화검의(2806)는 프리셋 02(수검)가 붙이는 선천운명이다.

## 7. 게임에서 확인한 것 (2026-10-14, 고정본 회차 `art-1014b`·`art-1014c`)

고정본 인물(신환, 결정 초기)에서 실험 명령으로 값을 바꾸고 게임 화면 캡처와 값 재조회로 판정했다. 두 회차 모두 `-End`로 세이브를 되돌렸다("SAVE RESTORED AND IDENTICAL").

### 7.1 고정본의 지금 값과 한국어 표기

| 항목 | 값 |
|---|---|
| `animaWeapons` | `1`(쌍어패만) |
| `resurgeCount` / `resurgePower1` / `resurgePower2` / `resurgePowerCount` | 9 / 0 / 0 / 0 |
| `godEyeData` | `godEyeGrade=1`, 기술·요수·장착 없음, 손상 없음 |
| `devilDemonData` | 요수(호요) 0, 손상 없음. `g.world.devilDemonMgr`는 있고, 대경지에 맞는 `DevilDemonBase` 줄(결정이면 3: 흡수 요수 14, 생활 팀 2, 호요 20)을 쓴다 |
| `piscesData` | 반쪽을 준 NPC 없음, `IsTransCD=True` |
| 지도 화면 버튼(`UIMapMain.playerInfo`) | `btnPiscesPendant` 켜짐, `btnGodEye`·`btnDevilDemon` 꺼짐. 왼쪽 아래 인물 초상의 물고기 그림 옆 숫자 9가 쌍어옥패 점수다 |

- 한국어 표기(게임의 번역을 거친 `g.conf` 표): 신기 셋의 이름·설명·배경 글(`.devremote\dump-awconf.txt`), 호천안 요수 기술 95개(`dump-godEyeSkills.txt`, 예: 100721 유서불풍, 100722 적엽비화, 110076 초목령생), 호요 45종(`dump-potmonBase.txt`, 예: 1001 수요의 영, 1002 약령의 영). 게임 화면의 용어: 연요호의 요수는 "호요", 호천안의 기술 목록은 "요수 기능", 두 신기 공통의 "천도비호"(빈사 때 탈출).
- 설정값도 게임에서 다시 읽었다: `PiscesParam`(NPC 호감 한도 180, 운명 150 …), `GodEyePar`, `DevilDemonBase`(단계 10줄), `PotmonGradeLevel`(1단계 경험치 0, 2단계 8000, 3단계 18000, 4단계 30000. 승급 아이템 100999).

### 7.2 신기 목록(`animaWeapons`)을 바꾸면

| 바꾼 것 | 바로 | 저장 후 다시 불러오면 |
|---|---|---|
| 2·3을 더함 | 데이터는 바로 쓸 수 있다: `DevilDemonMgr.IsOpenDevilDemon()`이 참이 되고 연요호·호천안 화면(`uiopen DevilDemon`, `GodEye`)이 정상으로 열려 동작한다. 지도 화면의 버튼은 `UpdateUI()`를 불러도 켜지지 않는다 | 버튼 셋이 모두 켜진다. 세이브 칸 메타(`g.cache.curCache.data.animaWeapons`)도 저장할 때 `1,2,3`으로 바뀐다 |
| 하나를 뺌 | 버튼은 그대로 | 그 버튼이 꺼진다. 그 신기의 자료(호요, 호천안 기술)는 남는다 |
| **모두 뺌(빈 목록)** | — | **게임이 시작 연출을 다시 튼다**: `IntoGameInfo`(인물 소개) → `IntoGameDrama`(대나무 숲, 바둑판 노인과의 대화) → 신기 셋의 선택 화면 → 확인 창 "호천안 선택을 확정하시겠습니까?" → 고른 하나가 목록에 들어가고(`weapons=2`) 곧바로 시작 튜토리얼 전투(`BattleInfo`)가 열린다. |

- 결론: 신기는 목록에 번호를 넣는 것만으로 열리고 저장 후에도 유지된다. **빈 목록은 만들면 안 된다**(편집기가 목록을 바꾸게 하면 마지막 하나는 빼지 못하게 막아야 한다).
- 첫 회차에서는 `save` 직후 바로 재시작해 바꾼 값이 남지 않았다. 5초 넘게 기다린 뒤 재시작하자 남았다. 저장은 비동기로 끝나는 것으로 보인다(검사에서 `save` 뒤에 기다릴 것).
- 호천안을 가진 세이브는 불러올 때마다 호천안 화면(`GodEye`)이 저절로 열렸다(두 회차, 세 번). 원인 미확인(이 세이브가 호천안을 정식 경로로 얻지 않아서일 수 있다).

### 7.3 신기별 정식 함수와 직접 쓰기

| 신기 | 해 본 것 | 결과 |
|---|---|---|
| 호천안 | `isDamage=true` 뒤 `GodEyeData.Repair()` | 즉시 고쳐지지 않는다. `isDamage=false`, `repairMonth`가 지금 달+3, `IsRepair()=true`, `IsCanUse()=false`, `number`가 1 늘었다. 곧 **"수리를 맡기는" 함수**다. 바로 쓸 수 있게 하려면 `isDamage=false`, `repairMonth=0`을 직접 쓴다(그러면 `IsCanUse()=true`) |
| 호천안 | `skillsID`에 기술 번호만 더함 | `IsSkillUnlock`은 참이지만 "요수 기능" 화면에서 요수가 잠긴 그림자로 남는다 |
| 호천안 | `bossGrade[요수 번호]=단계`(예: 7010 수요, 7091) + `skillsID` + `equipSkillID` | 화면에 그대로 나온다: 요수 그림이 열리고 "요수 기술 터득(1/3)", 기술 "유서불풍"에 "장비 중". 저장 후 유지. 전투에서 그 기술을 쓰는 것은 미확인 |
| 호천안 | 단계(`godEyeGrade`) | 화면 아래 "돌파" 버튼과 "축기경의 요수 격파: 0/5"(그 단계의 요수를 잡은 수)로 오른다. `IsCanUpGrade(false)`는 거짓. 단계를 직접 쓰는 것은 해 보지 않았다 |
| 연요호 | `PotMonMgr.AddPotMon(번호, 대경지, true)` | 호요가 바로 생긴다. 연요호 화면의 "모든 호요"에서 "호요 수량: 3/28", 상세(약령의 영, 결정경, 능력치, 전투 기능 셋)가 나온다. 둘째 인자는 대경지로 보인다(3을 주자 결정경으로 나왔다). 경험치는 그 단계의 시작값(3단계 18000)이 된다. 저장 후 유지(`devilDemonData.potmonData.potMonList`에 들어간다) |
| 연요호 | `PotmonAddExp(호요, 개수, false)` | 개수당 1000을 더하되 **다음 단계 경험치에서 멈춘다**(1단계 호요에 5개 → 5000, 다시 50개 → 8000에서 멈춤, 반환값은 실제로 더한 양) |
| 연요호 | `PotmonUpgrade(호요, false)` | 경험치가 다음 단계에 닿아도 단계가 오르지 않았다. 승급 아이템(100999)이 있어야 하는 것으로 보인다(미확인) |
| 쌍어패 | `PiscesData.GivePisces(NPC)` | 기록만 바뀐다(`npcUnitId`, `IsGive=true`, `lastGiveMonth`). 그 NPC의 운명(설정 `npcAddLuck` 150)·호감은 바뀌지 않았다. 게임 화면의 "쌍어패 증여" 흐름이 운명과 대화를 따로 처리하는 것으로 보인다 |
| 쌍어패 | `RecallPisces()` | `npcUnitId`가 비고 `IsGive=false`, `lastRecallMonth` 기록 |
| 쌍어패 | `resurgeCount` 직접 쓰기 | 부활 정보 화면(`ResurgeInfo`)에 "쌍어옥패 12점"으로 그대로 나온다. 저장 후 유지 |

## 8. 모드 관리 데이터 — 편집기로 만들 수 있는 것 (7절 반영)

| 후보 | 방법 | 위험과 주의 |
|---|---|---|
| 신기 목록 | `g.data.world.animaWeapons`에 1·2·3을 넣고 빼기 | 지도 버튼은 다시 불러와야 켜진다(편집기에서 "저장 후 다시 불러오면 버튼이 나타난다"고 알린다). 마지막 하나는 빼지 못하게 막는다(빈 목록이면 시작 연출과 신기 선택이 다시 나온다) |
| 쌍어옥패 점수 | `resurgeCount` | 쓰는 즉시 화면에 반영. 상한은 미확인 |
| 호천안 손상 | `isDamage=false`, `repairMonth=0` | `Repair()`는 수리 맡기기라 바로 고치는 데 쓰지 않는다 |
| 호천안 요수·기술·장착 | `bossGrade`, `skillsID`, `equipSkillID` | 요수 번호와 기술 번호의 짝은 `GodEyeSkills`(`bossID`). 전투 사용 미확인 |
| 연요호 호요 추가 | `PotMonMgr.AddPotMon` | 호요 수 상한(`DevilDemonBase.devilDemonCount`, 화면의 "/28")을 넘기는지 미확인 |
| 호요 경험치 | `PotmonAddExp` | 단계 경계에서 멈춘다. 승급은 미확인 |
| 쌍어패 반쪽 | `GivePisces`/`RecallPisces` | 기록만 바뀌어 정식 흐름과 다르다. 편집기 후보에서 뺀다 |

쌍어패 반쪽을 뺀 나머지를 신기 탭으로 편집기에 넣었다(05 문서 37절). 만들면서 더 알게 된 것: 호천안 요수의 단계 값은 1 연기·2 축기·3 결정(대경지 번호), 호요 단계도 같다(1을 주면 연기경. 7.3절의 "3을 주자 결정경"과 맞다), 요수 이름은 설정 표 `battleUnitAttr`, 호요 상한 28을 넘겨도 게임이 받아들인다, 요수를 해금하고 기술을 익히면 호천안 돌파 진행("요수 격파")이 오른다, 연요호의 `repairMonth`가 지금 달보다 뒤면 게임도 쓸 수 없다고 한다.

## 9. 아직 확인하지 못한 것

- 호천안 화면이 불러올 때마다 저절로 뜨는 원인.
- 호천안 장착 기술과 호요의 전투 사용, 천도비호(빈사 탈출)의 실제 발동.
- `resurgePower1`·`resurgePower2`·`resurgePowerCount`의 화면 표시.
- 호요 승급(`PotmonUpgrade`)의 조건, 호요 수 상한을 넘겼을 때.
- 호천안 단계(`godEyeGrade`)를 직접 쓴 뒤의 화면과 비용.
- 쌍어패 증여의 정식 흐름(운명 150, 호감 한도 180)과 소환·전송.
