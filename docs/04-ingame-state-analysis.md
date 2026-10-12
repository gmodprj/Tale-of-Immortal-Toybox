# 04. 인게임 상태 구성 분석

> 분석일: 2026-10-12 · 방법: 정적 분석(프록시 어셈블리 메타데이터, GGBH_API.chm, 설정 테이블)
> 한계: 런타임 검증을 하지 않았다. 쓰기·저장 반영·연쇄 처리는 확정하지 않는다.

## 1. 요약

- 게임 상태는 **저장 데이터 계층(`g.data`)** 과 **런타임 계층(`g.world`)** 두 겹이다. 저장 데이터 클래스(`DataUnit/UnitInfoData` 등)가 세이브에 직렬화되고, 런타임 클래스(`WorldUnitBase` 등)가 그것을 감싸 로직과 파생값을 제공한다.
- 플레이어와 NPC는 **같은 클래스**다. `g.world.playerUnit`도 `g.world.unit.GetUnit(id)`도 `WorldUnitBase`를 돌려주며, 모든 수치는 `unit.data.unitData`(`DataUnit/UnitInfoData`) 아래에 있다. 기준 문서의 플레이어 모듈과 NPC 모듈은 대상만 다른 하나의 편집기로 만들 수 있다.
- 능력치는 **저장값(`DataUnit/PropertyData`의 `Int32`)** 과 **파생값(`WorldUnitDynData`의 `DynInt`)** 이 같은 이름으로 따로 있다. `DynInt`는 기본값에 가산·승산 콜백과 상하한을 적용해 최종값을 낸다. 화면에 보이는 수치는 파생값이므로, 저장값을 바꿔도 기운·장비 보정이 다시 얹힌다.
- 영석은 별도 필드가 아니라 **아이템**이다(`PropsIDType.Money`). 공법도 아이템과 같은 `DataProps/PropsData`에 담기며 `propsType`이 `Martial`이다.
- 값을 바꾸는 공식 통로가 세 가지 있다. ① 데이터 클래스의 메서드(`AddExp`, `SetGradeID`, `AddProps`, `SetIntim` 등) ② 단위 행동(`unit.CreateAction(new UnitAction...)`) ③ 명령 문자열(`DramaFunctionTool.OptionsFunction`). 필드 직접 쓰기보다 이 통로를 우선 검증 대상으로 삼는다.
- 설치본의 API 문서(chm)는 설치된 게임보다 오래됐다. 프록시에 있는 `lotusSword`, `SoulDevourSwordMgr`, `YinyangEyeMgr`가 chm에 없다.

## 2. 분석 대상과 방법

| 자료 | 내용 | 규모 |
|---|---|---|
| `MelonLoader\Managed\Assembly-CSharp.dll` 외 3개 프록시 | 타입·프로퍼티·메서드 시그니처를 Mono.Cecil로 덤프 | 클래스 15,312개, 열거형 247개 |
| `Mod\modFQA\代码编写教程\GGBH_API.chm` | Sandcastle로 생성된 API 문서. `hh.exe -decompile`로 추출 | 클래스 8,118개, 열거형 87개, 페이지 110,034개 |
| `Mod\modFQA\配置修改教程\配置（只读）Json格式\` | 설정 테이블 내용 | 761개 |
| `Mod\modFQA\代码编写教程\ModMain\Example\Example.cs` | 공식 사용 예 | — |

읽는 법과 한계:

- Unhollower 프록시에서는 원본 클래스의 **필드가 프로퍼티로** 나타난다. 이 문서에서 "멤버"라고 적은 것은 프록시의 프로퍼티이며, 원본에서 필드였는지 프로퍼티였는지는 chm에서 확인할 수 있다(예: chm은 `DataUnit.PropertyData.guardValue`를 "字段"로 표기).
- 메서드는 이름과 매개변수만 알 수 있다. 메서드 안에서 무엇을 하는지(다른 값을 다시 계산하는지, UI를 갱신하는지)는 알 수 없다.
- "저장값/파생값" 구분은 타입에 근거한다. `Data*` 클래스의 `Int32`·`String` 멤버는 저장값, `DynInt` 멤버는 파생값으로 분류했다.
- 중첩 클래스는 프록시 표기대로 `바깥/안`(예: `DataUnit/UnitInfoData`)으로 적는다. C# 코드에서는 `DataUnit.UnitInfoData`다.

## 3. 전역 접근 구조

전역 클래스 `g`(`MonoBehaviour` 상속)의 정적 멤버:

| 멤버 | 타입 | 역할 |
|---|---|---|
| `g.world` | `WorldMgr` | 런타임 세계. 단위, 건물, 대지도 사건, 시간 진행 |
| `g.data` | `DataMgr` | 저장 데이터 전체 |
| `g.conf` | `ConfMgr` | 설정 테이블(설정 클래스 멤버 972개) |
| `g.ui` | `UIMgr` | UI 열기·닫기 |
| `g.events` | `EventsMgr` | 이벤트 구독·해제 |
| `g.timer` | `TimerMgr` | 지연·프레임 호출 |
| `g.res` | `ResMgr` | 리소스 로드 |
| `g.mod` | `ModMgr` | 모드 관리 |
| `g.cache` | `CacheMgr` | 세이브 파일 입출력으로 보이는 관리자(멤버 미조사) |
| `g.scene` | `SceneMgr` | 장면 전환 |
| `g.sounds`, `g.log`, `g.dlc`, `g.input` | `SoundsMgr`, `LogMgr`, `DLCMgr`, `InputMgr` | 소리, 로그, DLC, 입력 |
| `g.game`, `g.root` | `game`, `GameObject` | 게임 본체 객체, 루트 오브젝트 |

**런타임 계층** `WorldMgr`의 주요 멤버: `playerUnit`(`WorldUnitBase`), `unit`(`WorldUnitMgr`), `build`(`WorldBuildMgr`), `mapEvent`(`WorldMapEventMgr`), `run`(`WorldRunMgr`), `battle`(`WorldBattleMgr`), `unitAI`(`WorldUnitAIMgr`), `fortuitousTrigger`, `dramaTrigger`, `system`, `isIntoWorld`.

**저장 계층** `DataMgr`의 주요 멤버: `unit`(`DataUnit/Unit`), `world`(`DataWorld/World`), `buildSchool`(`DataBuildSchool/School`), `buildTown`, `map`, `grid`, `terr`, `unitLog`, `unitDie`, `globle`(`DataGloble/Globle`), `allData`, `isSaveData`, `SaveData(Action<T>)`.

**장면별 진입점** `SceneType`: `SceneType.map`(`SceneMap`), `SceneType.battle`(`SceneBattle`), `SceneType.login`(`SceneLogin`).

한 단위에 이르는 두 경로:

```text
런타임: g.world.unit.GetUnit(unitID)  → WorldUnitBase → .data (WorldUnitData) → .unitData (DataUnit/UnitInfoData)
저장:   g.data.unit.GetUnit(unitID)                                           → DataUnit/UnitInfoData
플레이어 ID: g.data.world.playerUnitID
```

## 4. 플레이어

### 4.1 상태 소유 구조

`g.world.playerUnit`(`WorldUnitBase`) → `data`(`WorldUnitData`) → `unitData`(`DataUnit/UnitInfoData`). 그 아래:

| 멤버 | 타입 | 내용 |
|---|---|---|
| `propertyData` | `DataUnit/PropertyData` | 능력치 저장값 |
| `initPropertyData` | `DataUnit/InitPropertyData` | 초기 능력치 |
| `propData` | `DataUnit/UnitDataProps`(`DataProps` 상속) | 가방(아이템·영석·비급) |
| `allActionMartial` | `Dictionary<String, DataUnit/ActionMartialData>` | 익힌 공법 |
| `skillLeft`, `skillRight`, `ultimate`, `step`, `abilitys` | `String`, `String[]` | 장착한 공법의 ID |
| `equips`, `props` | `String[]` | 장착 장비·소지품의 ID |
| `relationData` | `DataUnit/RelationData` | 관계 |
| `schoolID` | `String` | 소속 문파 |
| `pointX`, `pointY` | `Int32` | 대지도 위치 |
| `unitID`, `indexNum`, `createDay`, `residueDay` | — | 식별자, 생성일, 남은 일수 |
| `heart`, `elderData`, `artifactSpriteData`, `reborn`, `heresy`, `immortalCard`, `fairyChicken`, `lotusSword` | — | 도심, 장로, 기령, 환생 등 하위 시스템 데이터 |

파생값은 `unit.data.dynUnitData`(`WorldUnitDynData`)에 있다. 저장값과 같은 이름의 `DynInt` 멤버에 더해 `hpRestore`, `expGrowRate`, `stepGrowRate`, `propGridNum`, `propEquipNum`, `intimRate`, `hateRate`, `immortalPoint` 같은 파생 전용 멤버가 있다(멤버 108개).

`DynInt`의 주요 멤버: `baseValue`, `value`, `minValue`, `maxValue`, `lockValue`, `Clamp(Int32, Int32)`, `AddValue(...)`, `AddMul(...)`, `onBaseValueChangeCall`.

### 4.2 주요 필드

| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |
|---|---|---|---|---|---|---|
| 이름 | `DataUnit/PropertyData` | `name` | `String[]` | 저장값 | `GetName()`(읽기) | — |
| 성별, 종족 | `DataUnit/PropertyData` | `sex`, `race` | `UnitSexType`, `UnitRaceType` | 저장값 | — | — |
| 나이, 수명 | `DataUnit/PropertyData` | `age`, `life` | `Int32` | 저장값 | — | `RoleGrade`의 `growAddLife` |
| 체력 | `DataUnit/PropertyData` | `hp`, `hpMax` | `Int32` | 저장값. 파생값은 `dynUnitData.hp`, `hpMax` | — | `RoleGrade`의 `hpUp` |
| 영력, 염력 | `DataUnit/PropertyData` | `mp`, `mpMax`, `sp`, `spMax` | `Int32` | 저장값 | — | `RoleGrade`의 `mpUp`, `spUp` |
| 공격, 방어 | `DataUnit/PropertyData` | `attack`, `defense` | `Int32` | 저장값 | — | `RoleGrade`의 `atkUp`, `defUp` |
| 이동 속도, 각력 | `DataUnit/PropertyData` | `moveSpeed`, `footSpeed` | `Int32` | 저장값 | — | — |
| 회심, 호심 | `DataUnit/PropertyData` | `crit`, `critValue`, `guard`, `guardValue` | `Int32` | 저장값 | — | `RoleGrade`의 `crit`, `guard`, `critV`, `guardV` |
| 물리·마법 저항 | `DataUnit/PropertyData` | `phycicalFree`, `magicFree` | `Int32` | 저장값 | — | — |
| 자질(영근) | `DataUnit/PropertyData` | `basisFire`, `basisFroze`, `basisThunder`, `basisWind`, `basisEarth`, `basisWood` | `Int32` | 저장값 | — | — |
| 자질(무학) | `DataUnit/PropertyData` | `basisSword`, `basisSpear`, `basisBlade`, `basisFist`, `basisPalm`, `basisFinger` | `Int32` | 저장값 | — | — |
| 기예 | `DataUnit/PropertyData` | `refineElixir`, `refineWeapon`, `geomancy`, `symbol`, `herbal`, `mine` | `Int32` | 저장값 | — | — |
| 오성, 행운, 매력, 명성 | `DataUnit/PropertyData` | `talent`, `luck`, `beauty`, `reputation` | `Int32` | 저장값 | — | — |
| 건강, 정력, 심정 | `DataUnit/PropertyData` | `health`, `healthMax`, `energy`, `energyMax`, `mood`, `moodMax` | `Int32` | 저장값 | — | — |
| 정마(입장) | `DataUnit/PropertyData` | `standUp`, `standDown` | `Int32` | 저장값 | — | — |
| 성격 | `DataUnit/PropertyData` | `inTrait`, `outTrait1`, `outTrait2` | `Int32` | 저장값 | — | — |
| 도점 | `DataUnit/PropertyData` | `abilityPoint`, `abilityExp`, `abilityUpLevel` | `Int32` | 저장값 | `AddAbilityExp(Int32)` | `RoleGrade`의 `abilityPoint` |
| 수련 경험치 | `DataUnit/PropertyData` | `exp` | `Int32` | 저장값 | `AddExp(Int32)`, `AddFixExp(Int32)` | `RoleGrade`의 `exp` |
| 경지 | `DataUnit/PropertyData` | `gradeID` | `Int32` | 저장값 | `SetGradeID(Int32)` | `RoleGrade`의 `id` |
| 선천 기운 | `DataUnit/PropertyData` | `bornLuck` | `DataUnit/LuckData[]` | 저장값 | — | `RoleCreateFeature` |
| 후천 기운 | `DataUnit/PropertyData` | `addLuck` | `List<DataUnit/LuckData>` | 저장값 | `AddAddLuck(LuckData)`, `DelAddLuck(Int32)`. 런타임 쪽 `WorldUnitBase.CreateLuck(LuckData)`, `DestroyLuck(...)`, 행동 `UnitActionLuckAdd`, `UnitActionLuckDel` | `RoleCreateFeature` |
| 영석 | `DataProps` | `GetPropsNum(Int32)`에 `PropsIDType.Money` | `Int32` | 저장값(아이템 수량) | `WorldUnitData.RewardPropMoney(Int32)`, `DataProps.AddProps(Int32, Int32, String)`, `DelProps(Int32, Int32)` | `ItemProps` |
| 문파 공헌 | `DataUnit/UnitDataProps` | `totalSchoolMoney`와 아이템 `PropsIDType.SchoolMoney` | `Int32` | 저장값 | 위와 같음 | — |
| 위치 | `DataUnit/UnitInfoData` | `pointX`, `pointY` | `Int32` | 저장값 | `GetPoint()`, `SetPoint(Vector2Int)` | — |
| 능력치 상한 | — | — | — | — | — | `g.conf.roleAttributeLimit`(`ConfRoleAttributeLimit`) |

chm은 저장 필드에 중국어 설명과 `JsonPropertyAttribute`를 함께 적는다. 예: `guardValue` — "暴击防御倍率", `[JsonProperty(PropertyName = "qq")]`. 저장 데이터가 짧은 이름으로 JSON 직렬화된다는 뜻이다.

### 4.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| 능력치 조회 | `DataUnit/PropertyData`의 멤버, `WorldUnitDynData`의 `DynInt.value` | `DataUnit/UnitInfoData` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) |
| 능력치 수정 | `DataUnit/PropertyData`의 멤버 직접 대입 | `DataUnit/UnitInfoData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — `DynInt` 갱신, UI 갱신 | 미확인(런타임 검증 필요) |
| 영석 수정 | `WorldUnitData.RewardPropMoney`, `DataProps.AddProps` | `DataUnit/UnitDataProps` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 수명 증가 | `DataUnit/PropertyData.life` | `DataUnit/UnitInfoData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 자원 회복 | `hp`·`mp`·`sp`·`health`·`energy`·`mood`를 각 `*Max`로 | `DataUnit/UnitInfoData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 기운 추가·삭제 | `UnitActionLuckAdd`, `UnitActionLuckDel`, `WorldUnitBase.CreateLuck` | `DataUnit/PropertyData`, `WorldUnitBase.allLuck` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |

## 5. NPC

### 5.1 상태 소유 구조

- 전체 목록: `g.world.unit`(`WorldUnitMgr`)의 `allUnit`(`Dictionary<String, WorldUnitBase>`), `GetUnits(Boolean isGetHideUnit)`. 저장 쪽은 `g.data.unit.allUnit`(`Dictionary<String, DataUnit/UnitInfoData>`).
- 조회: `GetUnit(String unitID, Boolean)`, `GetUnit(Vector2Int point, Int32 range)`, `GetUnitsInArea(Int32 areaBaseID, Boolean)`.
- 생성·삭제: `WorldUnitMgr.CreateUnit(UnitInfoData)`, `AddUnit(UnitInfoData)`, `DelUnit(WorldUnitBase)`. 저장 쪽 `DataUnit/Unit.AddUnit()`, `DelUnit(String)`.
- 사망: `WorldUnitBase.Die()`, `isDie`. 사망자 데이터는 `g.data.unitDie`(`DataUnitDie/UnitDie`).
- NPC 고유 데이터: `DataUnit/UnitInfoData.aiData`(`DataUnit/AIData`), `npcUpGrade`, `memoryData`. 런타임 쪽 `WorldUnitBase.behavior`(`WorldUnitBehavior`), `allHobby`, `allTask`.
- 능력치·소지품·공법·관계의 구조는 4절과 같다.

### 5.2 주요 필드

| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |
|---|---|---|---|---|---|---|
| 고유 ID | `DataUnit/UnitInfoData` | `unitID` | `String` | 저장값 | — | — |
| 이름으로 검색 | `DataUnit/PropertyData` | `GetName()` | `String` | — | — | — |
| 경지로 검색 | `DataUnit/PropertyData` | `gradeID` | `Int32` | 저장값 | — | `RoleGrade` |
| 문파로 검색 | `DataUnit/UnitInfoData` | `schoolID` | `String` | 저장값 | — | — |
| 지역으로 검색 | `WorldUnitMgr` | `GetUnitsInArea(Int32, Boolean)`, `allUnitArea` | — | — | — | — |
| 관계로 검색 | `WorldUnitData` | `GetRelationType(WorldUnitBase)`, `allUnitRelation` | `UnitBothRelationType` | 파생값(캐시) | `UpdateAllUnitRelation()` | — |
| 성격·취향 | `DataUnit/PropertyData` | `inTrait`, `outTrait1`, `outTrait2`, `hobby`, `looksLove` | `Int32`, `Int32[]` | 저장값 | — | `RoleCreateHobby` 계열(`ConfRoleCreateHobbyItem`) |
| 소지품 | `DataUnit/UnitInfoData` | `propData` | `DataUnit/UnitDataProps` | 저장값 | 6절 | — |
| 위치 이동 | `DataUnit/UnitInfoData` | `SetPoint(Vector2Int)` | — | — | `SetPoint` | — |
| AI | `DataUnit/UnitInfoData` | `aiData` | `DataUnit/AIData` | 저장값 | 미조사 | `Npc*` 표 22개 |

### 5.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| NPC 목록·검색 | `WorldUnitMgr.GetUnits`, `allUnit` | `WorldUnitMgr` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) — 수천 개 순회 시 성능 |
| NPC 상세 조회 | 4절과 같은 멤버 | `DataUnit/UnitInfoData` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) |
| NPC 능력치 수정 | 4.3과 같음 | `DataUnit/UnitInfoData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — AI 전투력 평가 | 미확인(런타임 검증 필요) |
| NPC 이동 | `DataUnit/UnitInfoData.SetPoint` | `DataUnit/UnitInfoData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — `WorldUnitMgr.UpdateAreaUnits`, 대지도 표시 | 미확인(런타임 검증 필요) |
| NPC 생성 | `WorldUnitMgr.CreateUnit`, 명령 문자열 | `WorldUnitMgr`, `DataUnit/Unit` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| NPC 삭제 | `WorldUnitMgr.DelUnit`, `WorldUnitBase.Die` | `WorldUnitMgr` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 관계·문파 직위·임무의 ID 참조 | 미확인(런타임 검증 필요) |

## 6. 아이템

### 6.1 상태 소유 구조

- 가방: `unitData.propData`(`DataUnit/UnitDataProps` → `DataProps`). `allProps`(`List<DataProps/PropsData>`), `allPropsIDTab`(ID별), `allProsSoleIDTab`(고유 ID별), `gridMaxNum`.
- 아이템 한 개: `DataProps/PropsData`. `soleID`(인스턴스 고유 ID), `propsID`(템플릿 ID), `propsCount`(수량), `propsType`(`DataProps/PropsDataType`: `Props` = 1, `Martial` = 2), `values`(`Int32[]`, 인스턴스별 속성), `propsItem`(`ConfItemPropsItem`, 템플릿 참조).
- 종류별 상세는 `values`를 해석한 래퍼 클래스다. `PropsData.To<T>()`로 얻는다: `DataProps/PropsArtifact`(법보), `DataProps/PropsUnitRingData`(반지), `DataProps/PropsElderData`, `DataProps/PropsTreasureMap` 등 `DataProps/PropsDataBase` 파생 클래스.
- 장착 상태는 `unitData.equips`(`String[]`)와 런타임 `WorldUnitBase.allEquips`.
- 문파 창고는 `DataBuildSchool/SchoolData.propData`(`DataProps`), 임시 보관은 `DataWorld/World.tempProps`.

### 6.2 주요 필드

| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |
|---|---|---|---|---|---|---|
| ID로 검색 | `DataProps` | `GetProps(Int32 propsID)`, `GetProps(String soleID)` | — | — | — | `ItemProps` |
| 수량 | `DataProps/PropsData` | `propsCount` | `Int32` | 저장값 | `DataProps.AddProps(Int32, Int32, String)`, `DelProps(String, Int32)`, `DelProps(Int32, Int32)` | — |
| 생성 | `DataProps/PropsData` | `NewProps(Int32 propsID, Int32 propsCount)`(정적) | — | — | `WorldUnitData.RewardPropItem(Int32 id, Int32 count, Boolean showUI)` | `ItemProps` |
| 보상표로 지급 | `WorldUnitData` | `RewardItem(Int32 rewardID, ...)` | — | — | 같은 메서드 | `GameItemReward` 계열 |
| 소모 | `WorldUnitData` | `CostPropItem(Int32, Int32, Boolean)`, `CostItem(Int32, Boolean)` | — | — | 같은 메서드 | — |
| 인스턴스 속성 | `DataProps/PropsData` | `values` | `Int32[]` | 저장값 | `SetValues(Int32 index, Int32 value)` | 종류별 표 |
| 가방 용량 | `DataProps` | `GetGridMaxNum()`, `GetGridCurNum()` | `Int32` | 파생값(`dynUnitData.propGridNum`) | — | — |
| 장착 | `WorldUnitBase` | `CreateEquip(String propsSoleID)`, `DestroyEquip(String)` | — | — | 같은 메서드 | — |
| 복제 | `DataProps/PropsData` | `Clone()` | — | — | — | — |

아이템 관련 설정 표는 `Item*` 24개(`ItemProps`, `ItemBook`, `ItemChest`, `ItemHorse`, `ItemHerbal`, `ItemFate` 등)다.

### 6.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| 가방 조회 | `DataProps.allProps` | `DataUnit/UnitDataProps` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) |
| 아이템 추가 | `WorldUnitData.RewardPropItem`, `DataProps.AddProps` | `DataUnit/UnitDataProps` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 용량 초과 처리, `UpdateAllShowProps` | 미확인(런타임 검증 필요) |
| 수량 변경 | `DataProps.AddProps`, `DelProps` | `DataUnit/UnitDataProps` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 품질·옵션 변경 | `DataProps/PropsData.SetValues`, `DataProps/PropsArtifact` | `DataProps/PropsData` | 후보 확인(정적) | 미확인(런타임 검증 필요) — `values`의 인덱스별 의미 미해석 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 아이템 템플릿 검색 | `g.conf.itemProps`(`ConfItemProps`) | `ConfMgr` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) |

## 7. 경지·돌파

### 7.1 상태 소유 구조

- 현재 경지는 `DataUnit/PropertyData.gradeID` 하나다. 이 값은 `RoleGrade` 표의 행 `id`이고, 그 행이 대경지(`grade`), 단계(`phase`, 전·중·후기), 돌파 품질(`quality`)을 함께 정한다.
- 수련치는 `DataUnit/PropertyData.exp`이고, 행의 `exp`가 그 단계에 필요한 경험치다.
- 설정 접근: `g.conf.roleGrade`(`ConfRoleGrade`). `GetGradeItem(Int32 grade, Int32 phase)`, `GetGradeItem(grade, phase, quality)`, `GetNextGradeItem(Int32 gradeId)`, `GetGradeItemInExp(Int32 exp)`, `maxGrade`, `GetGradeName(...)`, `IsUpGrade(WorldUnitBase)`, `IsMaxGrade(WorldUnitBase)`, `IsPhaseStop(WorldUnitBase)`, `GetGrowItemRequire(grade, quality)`, `GetMaxLimitCount(WorldUnitBase)`.
- 돌파 행동: `UnitActionRoleUpGrade`, `UnitActionRoleUpGradePackage`, `UnitActionRoleUpGradeRule`, `UnitActionRoleUpGradeElder`.
- NPC의 돌파 기록: `DataUnit/UnitInfoData.npcUpGrade`.

### 7.2 주요 필드

`ConfRoleGradeItem`(`RoleGrade` 표의 한 행)의 멤버와 머리글 설명:

| 멤버 | 설명(머리글 xlsx) | 기획서 항목 |
|---|---|---|
| `grade`, `gradeName` | 境界, 名称 | 대경지 |
| `phase`, `phaseName` | 前中后期 | 세부 단계 |
| `quality`, `qualityName` | 突破品质 | 돌파 품질 |
| `exp` | 需求经验 | 수련치 요구량 |
| `condition`, `drama` | 突破条件, 需求剧情 | 돌파 조건 |
| `lockFeature` | 瓶颈气运ID | 병목 |
| `growItemRequire`, `growItemNum`, `materialsNum` | 突破材料需求, 수량, 可放入天材地宝数量 | 재료 검사 |
| `fateNum` | 突破后可选气运 | 역천개명 선택 수 |
| `growAddLife`, `abilityPoint` | 突破增加寿命, 突破增加道点 | 성공 후 보정 |
| `atkUp`, `defUp`, `hpUp`, `mpUp`, `spUp`, `crit`, `guard`, `critV`, `guardV`, `pfrUp`, `mfrUp` | 标准攻击 등 표준 능력치 | 성공 후 능력치 보정 |
| `immortalPoint` | 本源仙力 | 등선 단계 |
| `maxLimitCount` | 可装备跨境界功法数量 | 공법 경지 제한 |
| `failFeatureProb`, `failFeatureID`, `dungeonID`, `dungeonLevel` | 실패 시 기운 확률·기운·진입 부본 | 실패 처리 |

경지 관련 표: `RoleGrade` 외에 `技能境界配置表.xlsx` 묶음(공법 경지), `Role*` 50개 중 관련 표. 신규 경지 추가의 영향 범위는 이 표들과 `gradeID`를 참조하는 다른 표 전체다(조사 범위 밖).

### 7.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| 현재 경지·단계 조회 | `PropertyData.gradeID` → `g.conf.roleGrade` | `DataUnit/PropertyData` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) |
| 수련치 조작 | `PropertyData.AddExp`, `AddFixExp` | `DataUnit/PropertyData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 단계 자동 상승 여부 | 미확인(런타임 검증 필요) |
| 경지 직접 설정 | `PropertyData.SetGradeID` | `DataUnit/PropertyData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 능력치·수명·도점 보정이 따라오는지가 핵심 | 미확인(런타임 검증 필요) |
| 정식 돌파 실행 | `UnitActionRoleUpGrade` 계열 | `WorldUnitBase` | 후보 확인(정적) | 미확인(런타임 검증 필요) — 생성자 인자 미확인 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 돌파 재료 무시 | `RoleGrade`의 `growItemRequire`를 `ModExcel`로 변경 | 설정 테이블 | 후보 확인(정적) | 미확인(런타임 검증 필요) | 해당 없음 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 다른 경지 수정 모드 |

## 8. 공법·무공·신통

### 8.1 상태 소유 구조

```text
unitData.allActionMartial : Dictionary<String, DataUnit/ActionMartialData>     (키는 soleID)
  └─ ActionMartialData
       ├─ data  : DataProps/PropsData          (propsType = Martial)
       │    └─ To<DataProps/MartialData>()
       │         ├─ martialType : MartialType
       │         ├─ baseID      : Int32         (템플릿)
       │         ├─ grade       : Int32         (공법 경지)
       │         ├─ martialValues : Int32[]
       │         └─ GetPrefixs() : List<DataProps/MartialData/Prefix>
       │              └─ Prefix: prefixValueItem (ConfBattleSkillPrefixValueItem), prefixLevel, index
       ├─ exp   : Int32                        (숙련 경험치)
       └─ masteryItem : ConfBattleSkillMasteryItem
```

- `MartialType`: `SkillLeft` = 1, `SkillRight` = 2, `Step` = 3, `Ability` = 4, `Ultimate` = 5. 기준 문서 6.1의 분류와 대응시키면 무기·영기(좌/우), 신법(`Step`), 심법(`Ability`), 절기·신통(`Ultimate`)이다. 이 대응은 예제 주석과 이름에 근거한 것이며 신통이 어느 값에 속하는지는 확인하지 못했다.
- `DataProps/MartialData`의 파생 클래스: `DataProps/PropsSkillData`, `DataProps/PropsStepData`, `DataProps/PropsAbilityData`.
- 장착 슬롯: `unitData.skillLeft`, `skillRight`, `ultimate`, `step`(각 `String`), `abilitys`(`String[]`).
- 아직 익히지 않은 비급은 가방의 `DataProps/PropsData`(`propsType` = `Martial`)다. `PropsData.NewMartial(MartialType, Int32[])`, `NewMartial(MartialData)`로 만든다.
- 옵션(词条)은 `Prefix`다. 기준 문서 6.2의 `entries[]`에 해당한다.

### 8.2 주요 필드

| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |
|---|---|---|---|---|---|---|
| 인스턴스 ID | `DataProps/PropsData` | `soleID` | `String` | 저장값 | — | — |
| 템플릿 ID | `DataProps/MartialData` | `baseID` | `Int32` | 저장값(`values` 해석) | — | `BattleSkillAttack`, `BattleStepBase`, `BattleAbilityBase` |
| 종류 | `DataProps/MartialData` | `martialType` | `MartialType` | 저장값 | — | — |
| 공법 경지 | `DataProps/MartialData` | `grade` | `Int32` | 저장값 | 미확인 | `技能境界配置表` 묶음 |
| 숙련 경험치 | `DataUnit/ActionMartialData` | `exp` | `Int32` | 저장값 | `UnitInfoData.AddMartialExp(String id, Int32 exp)`, `AddMartialExpInBattle(...)` | `BattleSkillMastery` |
| 경험치 배율 | `DataUnit/UnitInfoData` | `GetMartialAddExpRate(ActionMartialData)` | `Int32` | 파생값 | — | — |
| 옵션 목록 | `DataProps/MartialData` | `GetPrefixs()` | `List<Prefix>` | 저장값(`values` 해석) | `SetPrefixID(Int32 index, Int32 number)`, `SetPrefixLevel(Int32 index, Int32 level)` | `BattleSkillPrefixValue` |
| 옵션 품질 | `DataProps/MartialData/Prefix` | `prefixLevel` | `Int32` | 저장값 | `SetPrefixLevel` | — |
| 해금된 옵션 | `DataUnit/ActionMartialData` | `GetPrefixsUnlock()` | `List<Prefix>` | 파생값 | — | — |
| 학습 | `DataUnit/UnitInfoData` | `StudyActionMartialProp(PropsData)` | — | — | 같은 메서드, 행동 `UnitActionPropMartialStudy` | — |
| 삭제 | `DataUnit/UnitInfoData` | `DelActionMartial(String id)` | — | — | 같은 메서드, 행동 `UnitActionMartialDel` | — |
| 장착·해제 | — | — | — | — | 행동 `UnitActionMartialEquip`, `UnitActionMartialUnequip` | — |
| 수련·참오 | — | — | — | — | 행동 `UnitActionMartialTrain`, `UnitActionMartialRealize` | — |
| 복제 | `DataProps/PropsData` | `Clone()` | — | — | — | — |
| 이름 변경 | `DataProps/PropsData` | `SetValuesStrArray(...)`, `GetValuesStrArray()` | `String[]` | 저장값 | 미확인 — 이름이 여기에 저장되는지 확인 못 함 | — |

기술 관련 설정 표: `BattleSkill*` 18개(`BattleSkillAttack`, `BattleSkillBase`, `BattleSkillEffect`, `BattleSkillCost`, `BattleSkillCombine`, `BattleSkillForget` 등), `BattleEffect*`·`BattleAbility*`·`BattleStep*` 12개, `BattleMissile`, DLC용 `DLCBattle*`. 효과 매개변수 설명은 `效果配置对照表.xlsx`에 있다.

### 8.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| 공법 목록·상세 조회 | `UnitInfoData.allActionMartial`, `GetActionMartial(MartialType)`, `MartialData.GetPrefixs` | `DataUnit/UnitInfoData` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) |
| 숙련 경험치 증가 | `UnitInfoData.AddMartialExp` | `DataUnit/ActionMartialData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 성장형 공법 모드 |
| 옵션 교체·품질 변경 | `MartialData.SetPrefixID`, `SetPrefixLevel` | `DataProps/PropsData.values` | 후보 확인(정적) | 미확인(런타임 검증 필요) — 유효한 옵션 ID 범위, 위치 제한 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 공법 이름 재계산(`02a`의 모드 변경 기록에 "옵션 교체 후 이름이 바뀌는 문제" 언급) | 미확인(런타임 검증 필요) |
| 공법 경지 변경 | `MartialData.grade` | `DataProps/PropsData.values` | 후보 확인(정적) | 미확인(런타임 검증 필요) — 설정 메서드 미확인 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 공법 지급 | `PropsData.NewMartial`, `UnitInfoData.StudyActionMartialProp` | `DataUnit/UnitInfoData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 융합·계승 | 전용 API 미발견. `Clone`, `SetPrefixID`의 조합으로 구현해야 함 | — | 미확인 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |

## 9. 관계

### 9.1 상태 소유 구조

`unitData.relationData`(`DataUnit/RelationData`). 단위마다 자기 쪽 관계를 따로 갖는다. 기준 문서 8장의 "양방향 참조"가 실제 구조다.

- 혈연·인연: `parent`(`String[]`), `children`, `childrenPrivate`, `brother`, `parentBack`, `childrenBack`, `brotherBack`, `married`(`String`), `lover`, `master`, `student`(각 `List<String>`, 값은 단위 ID).
- 호감도: `intimToPlayerUnit`(`Single`), `intimToUnit`(`Dictionary<String, Single>`), `intimToSchool`(`Dictionary<String, Int32>`).
- 기타: `humanValues`(`Dictionary<String, Int32>`), `notRelieveUnitsID`.
- 열거형 `UnitRelationType`: `Parent` = 1, `Children` = 2, `ChildrenPrivate` = 3, `Brother` = 4, `ParentBack` = 5, `ChildrenBack` = 6, `BrotherBack` = 7, `Married` = 8, `Lover` = 9, `Master` = 10, `Student` = 11.
- 열거형 `UnitBothRelationType`(두 단위 사이의 종합 판정): `Parents`, `Children`, `Married`, `Lover`, `Brother`, `Master`, `Student`, `Friend`, `Hater`, `School`, `Stand`, `Doctrine` 등.
- 친구·원수는 별도 목록이 아니라 호감도에서 판정된다(`IsFriend(WorldUnitBase)`, `GetIntimUnitData(...)`가 `friendUnits`, `enemyUnits`를 돌려준다).

### 9.2 주요 필드

| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |
|---|---|---|---|---|---|---|
| 호감도 | `DataUnit/RelationData` | `intimToUnit`, `GetIntim(String unitID)` | `Dictionary<String, Single>`, `Int32` | 저장값 | `AddIntim(String unitID, Single value, Int32 clampNewIntim, String uiTip, Boolean isTip)`, `SetIntim(String unitID, Single value)`, `ClearIntim(String)` | — |
| 원한 | `DataUnit/RelationData` | 호감도의 음수 쪽 | — | 저장값 | `AddHate(...)` | — |
| 친구 | `DataUnit/RelationData` | `IsFriend(WorldUnitBase)` | `Boolean` | 파생값 | 호감도 변경 | — |
| 연인, 배우자 | `DataUnit/RelationData` | `lover`, `married` | `List<String>`, `String` | 저장값 | 행동 `UnitActionRelationSet`, `UnitActionRoleRelation` | — |
| 사제 | `DataUnit/RelationData` | `master`, `student` | `List<String>` | 저장값 | 위와 같음 | — |
| 부모·자식·형제 | `DataUnit/RelationData` | `parent`, `children`, `brother` | — | 저장값 | 행동 `UnitActionRelationSetParent` | — |
| 관계 조회 | `DataUnit/RelationData` | `GetRelation(String toUnitID)`, `IsRelation(String, UnitRelationType)`, `GetAllRelation(UnitRelationType, Boolean)` | — | — | — | — |
| 문파 호감 | `DataUnit/RelationData` | `intimToSchool` | `Dictionary<String, Int32>` | 저장값 | `AddIntimSchool(String buildID, Int32 value)` | — |
| 호감 변화 배율 | `WorldUnitDynData` | `intimRate`, `hateRate` | `DynInt` | 파생값 | — | — |

### 9.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| 호감도 조회 | `RelationData.GetIntim` | `DataUnit/RelationData` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) |
| 호감도 설정 | `RelationData.SetIntim`, `AddIntim` | `DataUnit/RelationData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 상대 쪽 값은 따로 바꿔야 하는지, `WorldUnitData.UpdateAllUnitRelation` 필요 여부 | 미확인(런타임 검증 필요) |
| 관계 유형 설정 | `UnitActionRelationSet`, 목록 직접 수정 | `DataUnit/RelationData`(양쪽) | 후보 확인(정적) | 미확인(런타임 검증 필요) — 행동의 생성자 인자 미확인 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 양쪽 목록 동기화 | 미확인(런타임 검증 필요) |
| 일괄 호감도 변경 | `WorldUnitMgr.GetUnits` 순회 + `SetIntim` | 전체 단위 | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |

## 10. 문파

### 10.1 상태 소유 구조

- 저장: `g.data.buildSchool`(`DataBuildSchool/School`) → 문파별 `DataBuildSchool/SchoolData`.
- 런타임: `MapBuildSchool`(`MapBuildBase` 상속). 단위에서는 `unit.data.school`로 얻는다. 건물 관리자는 `g.world.build`(`WorldBuildMgr`).
- 단위 쪽 연결: `unitData.schoolID`(`String`), `WorldUnitData.schoolStand`.

`DataBuildSchool/SchoolData`의 주요 멤버:

| 분류 | 멤버 |
|---|---|
| 재화·명성 | `money`(`Int64`), `faithMoney`(`Int64`), `reputation`, `playerPostMoney` |
| 구성원 | `npcSchoolMain`(문주), `npcBigElders`, `npcElders`, `npcInherit`, `npcIn`(내문), `npcOut`(외문), `totalMember`, `postData`(`DataBuildSchool/PostData`: `postMain`, `postBigElders`, `postElders`), `unitIDToSchoolPostType` |
| 외교 | `enemySchoolID`, `intimToSchool`, `schoolsInimt`, `intimToUnit` |
| 상태 | `propertyData`(`DataBuildSchool/PropertyData`), `manorData`, `fightData`, `honorData`, `tasks`, `logs` |
| 창고 | `propData`(`DataProps`) |
| 처벌·탈퇴 | `punishUnit`, `renegadeUnit`, `exitSchoolUnit`, `unitExitMark` |

`DataBuildSchool/PropertyData`: `level`, `prosperous`, `prosperousMax`, `loyal`, `medicina`, `mine`, `largeArrayHP`, `largeArrayHPMax`, `allySchoolID`, `tactic`.

### 10.2 주요 필드

| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |
|---|---|---|---|---|---|---|
| 가입·탈퇴 | `DataUnit/UnitInfoData` | `schoolID` | `String` | 저장값 | 행동 `UnitActionSchoolJoin`, `UnitActionSchoolExit`, `MapBuildSchool.ExitSchool(WorldUnitBase)` | `School*` 94개 |
| 직급 | `DataBuildSchool/SchoolData` | `postData`, `unitIDToSchoolPostType` | `PostData`, `Dictionary<String, SchoolPostType>` | 저장값, 캐시 | 행동 `UnitActionSchoolSetPostType`, `UnitActionSchoolSwapPostType` | — |
| 개인 공헌 | `DataUnit/UnitDataProps` | `totalSchoolMoney`, 아이템 `PropsIDType.SchoolMoney` | `Int32` | 저장값 | `DataProps.AddProps` | — |
| 문파 재화 | `DataBuildSchool/SchoolData` | `money` | `Int64` | 저장값 | 미확인 | — |
| 구성원 | `DataBuildSchool/SchoolData` | `npcIn`, `npcOut`, `npcElders` 등 | `List<String>` | 저장값 | 위 행동 | — |
| 외교 | `DataBuildSchool/SchoolData` | `enemySchoolID`, `intimToSchool` | `String`, `Dictionary<String, Int32>` | 저장값 | 미확인 | — |
| 번영·충성 | `DataBuildSchool/PropertyData` | `prosperous`, `loyal` | `Int32` | 저장값 | 미확인 | — |

### 10.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| 문파 정보 조회 | `DataBuildSchool/SchoolData` | `DataBuildSchool/School` | 후보 확인(정적) — 문파 목록을 얻는 메서드 미조사 | — | — | — | 미확인(런타임 검증 필요) |
| 공헌·재화 수정 | `DataProps.AddProps`(`SchoolMoney`), `SchoolData.money` | 단위, 문파 | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 직급 변경 | `UnitActionSchoolSetPostType` | `DataBuildSchool/PostData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 구성원 목록과 직위 데이터의 동기화 | 미확인(런타임 검증 필요) — 문파 관리 모드 |
| 가입·탈퇴 | `UnitActionSchoolJoin`, `UnitActionSchoolExit` | 단위, 문파 | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |

## 11. 세계·이벤트

### 11.1 상태 소유 구조

`g.data.world`(`DataWorld/World`)의 주요 멤버:

| 분류 | 멤버 |
|---|---|
| 시간 | `worldDay`(`Int32`), `gameRunTime`(`Int64`) |
| 세계 설정 | `gameLevel`(`GameLevelType`), `npcCountId`, `npcRate`, `customWorldData` |
| 플레이어 | `playerUnitID`, `playerView`, `worldPlayerView`, `playerLog` |
| 부활 | `resurgeCount`, `resurgePower1`, `resurgePower2`, `resurgePowerCount` |
| 해금 | `openAreaID`(`List<OpenAreaData>`), `allArtistrySkillID`, `pillFormulas`, `allMartialStudy` |
| 모드 | `modData`(`List<DataWorld/World/ModData>`), `missingModSoleID` |
| 기타 | `weather`, `allLetter`, `marksUnitID`, `tempProps`, `seedData` |

시간 진행은 `g.world.run`(`WorldRunMgr`): `roundDay`, `roundMonth`, `isRunning`, `AddDay(Int32 day, Boolean isAutoRun)`, `RunWorld(Action)`, `On(WorldRunOrder, Action)`.

대지도 사건은 `g.world.mapEvent`(`WorldMapEventMgr`). 공식 예제: `AddGridEvent(point, 6)`.

기연·극: `g.world.fortuitousTrigger`, `g.world.dramaTrigger`, `DramaTool.OpenDrama(Int32, DramaData)`. 설정 표는 `FortuitousEvent`, `DramaDialogue`, `DramaOptions`, `DramaNpc`, `WorldFortuitousEventBase`.

이벤트 구독: `g.events.On(이름, 콜백)`. 범주 클래스 `EGameType`, `EMapType`, `EBattleType`.

### 11.2 주요 필드

| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |
|---|---|---|---|---|---|---|
| 현재 날짜 | `DataWorld/World` | `worldDay` | `Int32` | 저장값 | `WorldRunMgr.AddDay` | — |
| 시간 진행·정지 | `WorldRunMgr` | `isRunning`, `IsCanRun()` | `Boolean` | 런타임 | `RunWorld`, `AddDay` | — |
| 지역 이동 | `DataUnit/UnitInfoData` | `SetPoint(Vector2Int)` | — | — | 같은 메서드 | `World*` 38개 |
| 지도 해금 | `DataWorld/World` | `openAreaID`, `playerView` | — | 저장값 | 미확인 | — |
| 사건 생성 | `WorldMapEventMgr` | `AddGridEvent` | — | — | 같은 메서드 | `WorldFortuitousEventBase` |
| 기연 강제 발생 | — | 명령 문자열 | — | — | `DramaFunctionTool.OptionsFunction` | `FortuitousEvent`, 대조표 `剧情条件及影响` |
| 난이도 | `DataWorld/World` | `gameLevel` | `GameLevelType` | 저장값 | 미확인 | — |
| 채집·드랍 배율 | — | — | — | — | 설정 표 수정 | `WorldBlock*`, `ItemChestDrops` 등 |

### 11.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| 날짜 조회 | `DataWorld/World.worldDay` | `DataWorld/World` | 후보 확인(정적) | — | — | — | 미확인(런타임 검증 필요) |
| 시간 진행 | `WorldRunMgr.AddDay` | `WorldRunMgr` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — NPC 행동·월말 처리 | 미확인(런타임 검증 필요) |
| 순간 이동 | `UnitInfoData.SetPoint` | `DataUnit/UnitInfoData` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) — 시야·대지도 표시 | 미확인(런타임 검증 필요) — 전송 모드 |
| 사건·부본 생성 | `WorldMapEventMgr.AddGridEvent`, `WorldBattleMgr.IntoBattle` | `WorldMgr` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 명령 실행기 | `DramaFunctionTool.OptionsFunction` | — | 후보 확인(정적) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |

## 12. 전투

### 12.1 상태 소유 구조

전투 중에는 `SceneType.battle`(`SceneBattle`)이 진입점이다: `unit`(`BattleUnitMgr`), `effect`(`BattleEffectMgr`), `battleMap`(`BattleMapMgr`), `room`, `battleEnd`, `martial`, `battleData`.

- 전투 단위: `UnitCtrlBase`(파생: `UnitCtrlHumanNPC` 등). 플레이어는 `SceneType.battle.battleMap.playerUnitCtrl`.
- 전투 단위의 수치: `UnitCtrlBase.data`(`UnitDataBase`). **대지도의 단위 데이터와 별개의 사본**이다. `hp`, `mp`, `sp`, `dp`(`Int32`, 현재값), `maxHP`, `maxMP`, `maxSP`, `attack`, `defense`, `moveSpeed`, `crit`, `guard`(`DynInt`), `skillCDRate`, `stepCDRate`, `ultimateCDRate`, `artifactCDRate`, `itemCDRate`, `hpCostRate`, `mpCostRate`, `skillExpRate`(`DynInt`), `skillsData`, `ultimateData`, `stepData`, `abilitiesData`.
- 전투 이벤트(`EBattleType`의 이름 상수): `BattleStart`, `BattleEnd`, `UnitUseSkill`, `UnitHitSkill`, `UnitHit`, `UnitDie`, `UnitHitDynIntHandler`, `UnitHitDynIntHandlerShield`, `UnitEffectHitDynIntHandler` 등. 단위별 이름을 만드는 정적 메서드도 있다: `UnitHitDynIntHandlerInAttack(UnitCtrlBase)`, `UnitHitDynIntHandlerInHit(UnitCtrlBase)`.

### 12.2 주요 필드

| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |
|---|---|---|---|---|---|---|
| 무적 | `EBattleTypeData.UnitHitDynIntHandler` | `dynV`, `hitUnit` | `DynInt`, 단위 | 이벤트 데이터 | 콜백에서 `dynV.baseValue = 0`(공식 예제) | — |
| 피해 배율 | 위와 같음 | `dynV` | `DynInt` | 이벤트 데이터 | 콜백에서 값 조정. `EBattleType.UnitHitDynIntHandlerInAttack(unit)`로 공격자 한정 | — |
| 쿨타임 배율 | `UnitDataBase` | `skillCDRate`, `stepCDRate`, `ultimateCDRate` | `DynInt` | 파생값 | `DynInt.AddValue`, `AddMul` | — |
| 자원 소모 0 | `UnitDataBase` | `hpCostRate`, `mpCostRate` | `DynInt` | 파생값 | 위와 같음 | `BattleSkillCost` |
| 적 체력 | `UnitDataBase` | `hp`, `maxHP` | `Int32`, `DynInt` | 전투 중 값 | 직접 대입 | `BattleUnitAttr` |
| 효과 부여 | `UnitCtrlBase` | `AddEffect(Int32 id, UnitCtrlBase, SkillCreateData)` | — | — | 같은 메서드 | `BattleEffect` |
| 적·아군 생성 | `BattleUnitMgr` | `CreateUnitMonst`, `CreateUnitHuman` | — | — | 같은 메서드, `BattleFunctionTool.OptionsFunction` | `BattleUnitAttr` |
| 즉사 | `UnitCtrlBase` | `Die(Boolean isEffect, HitData hitData)` | — | — | 같은 메서드 | — |
| 숙련 경험치 배율 | `UnitDataBase` | `skillExpRate`, `ultimateExpRate`, `stepExpRate` | `DynInt` | 파생값 | — | — |

적용 범위 구분(기준 문서 9장): 이벤트 데이터의 `hitUnit`과 `UnitCtrlBase.data`의 `_unitType`(`UnitType`)으로 플레이어·아군·적을 가릴 수 있다. 공식 예제에 `UnitType.PlayerNPC`, `UnitType.Monst`가 쓰인다.

### 12.3 조사 기록

| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |
|---|---|---|---|---|---|---|---|
| 무적·피해 배율 | `g.events.On(EBattleType.UnitHitDynIntHandler, ...)` | 이벤트 | 후보 확인(정적, 공식 예제) | 미확인(런타임 검증 필요) | 해당 없음(전투 중 한정) | 미확인(런타임 검증 필요) — 보호막·효과 피해는 별도 이벤트 | 미확인(런타임 검증 필요) — 다른 모드의 같은 이벤트 구독 |
| 쿨타임·소모 조정 | `UnitDataBase`의 `*CDRate`, `*CostRate` | `UnitDataBase` | 후보 확인(정적) | 미확인(런타임 검증 필요) | 해당 없음 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 적 생성 | `BattleUnitMgr.CreateUnitMonst` | `SceneBattle` | 후보 확인(정적, 공식 예제) | 미확인(런타임 검증 필요) | 해당 없음 | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |
| 전투 결과가 대지도 데이터에 반영되는 경로 | `BattleEndMgr`, `BattleDataMgr` | — | 미확인 | — | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) | 미확인(런타임 검증 필요) |

## 13. 템플릿과 인스턴스의 구분

| 영역 | 템플릿(설정 테이블, `g.conf`) | 인스턴스(세이브, `g.data`) | 연결 |
|---|---|---|---|
| 경지 | `RoleGrade` 행(`ConfRoleGradeItem`) | `PropertyData.gradeID` | 행 `id` |
| 아이템 | `ItemProps` 행(`ConfItemPropsItem`) | `DataProps/PropsData`(`soleID`, `propsCount`, `values`) | `propsID` |
| 공법 | `BattleSkillAttack`·`BattleStepBase`·`BattleAbilityBase` 행 | `DataProps/PropsData`(`propsType` = `Martial`) + `ActionMartialData.exp` | `MartialData.baseID` |
| 공법 옵션 | `BattleSkillPrefixValue` 행(`ConfBattleSkillPrefixValueItem`) | `MartialData/Prefix`(`prefixLevel`, `index`) | `Prefix.prefixValueItem` |
| 기운 | `RoleCreateFeature` 행 | `DataUnit/LuckData`(`id`, `duration`, `createTime`) | `id` |
| 전투 효과 | `BattleEffect` 행(`ConfBattleEffectItem`) | 전투 중 `EffectBase` | 효과 ID |
| NPC | `NpcBase` 등 `Npc*` 행 | `DataUnit/UnitInfoData` | 미확인 |
| 문파 | `School*` 행 | `DataBuildSchool/SchoolData` | 미확인 |

이 구분이 주는 설계상의 결론:

- **템플릿을 바꾸는 일**(모든 개체에 적용, 수치·규칙 조정)은 `ModExcel`로 한다. 게임을 다시 시작해야 적용되고 세이브를 건드리지 않는다.
- **인스턴스를 바꾸는 일**(특정 개체의 값)은 코드로 한다. 세이브에 남는다.
- 기준 문서의 "신규 아이템 정의", "신규 기술", "신규 경지"는 템플릿 추가이고, "아이템 추가", "공법 옵션 편집", "경지 직접 수정"은 인스턴스 변경이다.
- 설정 표의 ID는 모드마다 `excelMID` 오프셋이 붙는다. 인스턴스가 모드의 템플릿 ID를 참조한 채 그 모드를 빼면 세이브가 깨질 수 있다(`DataWorld/World.missingModSoleID`가 이 상황을 추적하는 필드로 보인다. 의미는 확인하지 못했다).

## 14. API 문서와 어셈블리의 불일치

| 항목 | chm | 프록시 어셈블리 | 해석 |
|---|---|---|---|
| 규모 | 클래스 8,118개, 열거형 87개 | 클래스 15,312개, 열거형 247개 | 프록시에는 컴파일러 생성 클래스와 Unhollower 보조 타입이 포함되어 직접 비교는 안 된다 |
| `lotusSword`, `LotusSwordMgr` | 색인에 없음 | 있음 | chm 작성 이후 추가된 콘텐츠 |
| `SoulDevourSwordMgr` | 색인에 없음 | 있음 | 위와 같음 |
| `YinyangEyeMgr` | 색인에 없음 | 있음 | 위와 같음 |
| `fairyChicken`, `immortalCard`, `DevilDemonMgr`, `DLCMgr` | 있음 | 있음 | — |
| `RewardPropMoney`, `SetGradeID`, `SetIntim`, `AddMartialExp`, `SetPrefixLevel`, `GetModPathRoot`, `roleAttributeLimit` | 있음 | 있음 | 이 문서의 핵심 변경 메서드는 양쪽에 모두 있다 |

chm이 프록시보다 나은 점: 멤버마다 중국어 설명이 있고, 필드와 프로퍼티를 구분하며, `JsonProperty` 이름과 메서드 매개변수의 기본값을 보여 준다. 실측 기준은 프록시이고, chm은 의미를 해석하는 보조 자료로 쓴다.

## 15. v0.1 런타임 검증 목록

기준 문서 11.2의 순서(읽기 → 단순한 한 항목 쓰기 → 재시작 후 지속성)에 맞춘 우선순위다.

**1단계 — 읽기 전용(P0)**

1. `Init`에서 `g.world.playerUnit.data.unitData.propertyData.GetName()`을 로그로 출력한다. 로드 경로와 전역 접근을 한 번에 확인한다.
2. `DataUnit/PropertyData`의 저장값과 `WorldUnitDynData`의 `DynInt.value`를 나란히 출력해 화면의 수치가 어느 쪽인지 확인한다.
3. `g.world.unit.GetUnits(false)`의 개수와 순회 시간을 잰다.
4. 가방(`propData.allProps`)과 공법(`allActionMartial`)을 나열한다. `PropsData.To<DataProps/MartialData>()`와 `GetPrefixs()`가 동작하는지 확인한다.
5. `g.data.world.worldDay`, `playerUnitID`를 출력한다.

**2단계 — 단순한 한 항목 쓰기(P1)**

6. 영석: `RewardPropMoney(1)` 호출 후 UI 반영, 세이브 후 재시작 시 지속 여부.
7. 능력치 하나(`attack`): 저장값 직접 대입 후 `DynInt.value` 변화, UI 반영, 지속 여부. 대입만으로 부족하면 `WorldUnitBase.UpdateConf()` 등 갱신 경로를 찾는다.
8. 아이템: `RewardPropItem(id, 1, true)`. 가방이 가득 찼을 때의 동작.
9. 능력치 상한: `g.conf.roleAttributeLimit`의 값과, 상한을 넘겨 썼을 때의 동작(`02a`의 "행운 200 초과 시 드랍 소실" 사례 포함).

**3단계 — 연쇄가 있는 쓰기(P2~P3)**

10. 호감도 `SetIntim`: 한쪽만 바꿨을 때 상대 쪽 값과 관계 판정.
11. 수련치 `AddExp`와 경지 `SetGradeID`: 능력치·수명·도점이 따라오는지. `UnitActionRoleUpGrade`와 결과 비교.
12. 공법 `AddMartialExp`, `SetPrefixLevel`, `SetPrefixID`: 유효 범위, 이름 재계산, 지속 여부.
13. 기운 추가(`UnitActionLuckAdd`)와 삭제.

**4단계 — 고위험(P4 이후)**

14. NPC 생성·삭제와 ID 참조 정리.
15. 문파 직위·구성원 변경.
16. 시간 진행 `AddDay`, 순간 이동 `SetPoint`.
17. 전투 이벤트 구독(`UnitHitDynIntHandler`)의 적용 범위 분리와 `Init` 재호출 시 중복 구독 방지.

### 1단계 결과 (2026-10-12 실측)

읽기 전용 탐침(`src/ModCode/ModMain/Features/ReadOnlyProbe.cs`)을 게임 v1.2.113.259에서 실행했다. 세이브는 연기 초기, 1년 1월의 캐릭터 하나다. 값은 쓰지 않았다. 근거는 `MelonLoader\Latest.log`의 `[TlToybox][probe]` 줄이다.

| # | 항목 | 결과 | 판정 |
|---|---|---|---|
| 1 | `g.world.playerUnit.data.unitData.propertyData.GetName()` | 이름과 `unitID=kh3o73`이 읽혔다 | 읽기 확인됨 |
| 2 | 저장값과 `DynInt.value` 비교 | 공격: 저장 20, `baseValue` 20, `value` 25. 행운: 저장 80, `value` 70. 방어·체력 상한·수명·경지는 세 값이 같다 | 읽기 확인됨. **저장값과 파생값이 실제로 다르다** |
| 3 | `g.world.unit.GetUnits(false)` 순회 | 단위 1,511개, 이름까지 읽는 순회에 63ms | 읽기 확인됨. 매 프레임 전체 순회는 부담이지만 요청 시 한 번 순회는 문제없다 |
| 4a | 가방 `propData.allProps` | 항목 4개, 칸 3/52. 영석은 `propsID=10001`, 수량 500인 아이템이고 `GetPropsNum(PropsIDType.Money)`도 500 | 읽기 확인됨. 영석이 아이템이라는 분석과 일치 |
| 4b | 공법 `allActionMartial`, `To<DataProps.MartialData>()`, `GetPrefixs()` | 1개. `martialType=SkillLeft`, `baseID=11423`, `grade=1`, `exp=0`, 옵션 0개 | 읽기 확인됨. 옵션이 있는 공법은 이 세이브에 없어 `Prefix` 내용은 미확인 |
| 5 | `g.data.world` | `worldDay=1`, `playerUnitID=kh3o73`(1번의 `unitID`와 같음), `gameLevel=Common`, `roundMonth=0` | 읽기 확인됨 |

4~12절의 조사 기록에서 "읽기: 후보 확인(정적)"으로 적은 것 가운데 위 항목에 해당하는 것은 **읽기가 런타임에서 확인됐다.** 쓰기·저장 반영·연쇄 처리는 여전히 미확인이다.

함께 확인된 사실:

- 저장값을 그대로 `baseValue`로 쓰고, 그 위에 보정이 얹혀 `value`가 된다(공격 20 → 25). 화면의 수치는 `value` 쪽이다. 능력치 편집기는 저장값을 바꾸되 결과 확인은 `value`로 해야 한다.
- 파생값이 저장값보다 작을 수도 있다(행운 80 → 70). 보정은 가산만이 아니다.
- 프록시의 제네릭 메서드 `PropsData.To<T>()`와 IL2CPP `Dictionary`의 `foreach` 열거가 이 환경(MelonLoader 0.5.4)에서 동작한다.
- 세계가 준비됐는지는 `g.world != null && g.world.isIntoWorld && g.world.playerUnit != null`로 판정할 수 있다. `Init` 직후에는 `g.world`는 있으나 `isIntoWorld`가 `false`였고, 세이브를 불러와 약 46초 뒤 준비됐다.

**공통 확인 사항**

- 세이브 시점(`DataMgr.SaveData(Action<T>)`, `isSaveData`)과 백업 대상 폴더(`01` 문서 9절의 `Steam\CacheData`).
- `Init`이 다시 불릴 때 이벤트 구독·Harmony 패치·타이머가 중복되지 않는지.
- 모든 쓰기 시험은 별도 테스트 세이브에서 한다.
