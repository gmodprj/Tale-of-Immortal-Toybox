# 11. 외부 트레이너(ggbh-trainer 0.8.30)와의 기능 비교 (2026-10-19)

사용자 요청(2026-10-19): "트레이너 기능을 우리 편집기와 비교해서, 우리에게 없는 기능과 구현 방법을 문서로 정리해 줘". 대상은 `E:\ggbh-trainer-0.8.30`(카페 배포판. 트레이너 UI 0.8.30, 게임 안 브리지 0.8.25).

**조사 방법.** 실행하지 않고 파일만 읽었다.
- 브리지 `Mods\GGBHTrainerBridge.dll`(MelonLoader 모드, .NET): `ilspycmd`로 디컴파일했다(약 24,000줄. 사본은 세션 scratchpad에 있고 저장소에는 넣지 않는다).
- UI `Trainer\GGBHTrainer.exe`(PyInstaller, Python 3.12 + PySide6): 앱 모듈 67개의 상수 문자열(탭·줄 이름·설명)만 `marshal`로 읽었다.
- 아래의 게임 API 이름은 트레이너 코드에서 읽은 것이다. **우리 회차에서는 아직 실측하지 않았다**(구현할 때 실험으로 확인한다).
- 트레이너 코드는 남이 배포한 것이므로 옮겨 쓰지 않는다. 쓰는 게임 API와 동작(사실)만 참고하고 구현은 우리 규칙(백업 → 검사 → 쓰기 → 재조회 → 이력, 회차 검증)으로 새로 한다.

## 1. 트레이너의 구조

- **두 부분 구조.** 게임 안 브리지가 `http://127.0.0.1:17865/`에 HTTP를 열고, 바깥 창(관리자 권한 실행)이 GET 요청을 보낸다. 경로는 62개다(`/bag/*`, `/skills/*`, `/destiny/*`, `/cheats/*`, `/school/*`, `/breakthrough/*`, `/relations`, `/favor/set`, `/units/search`, `/storage/*`, `/player/*`).
- **요청 처리.** 수신 스레드가 요청을 받아 넘기면 주 스레드(`OnUpdate`)가 처리한다. 12초 안에 끝나지 않으면 시간 초과를 돌려준다. 우리 편집기처럼 게임 안에 창을 그리지 않는다.
- **게임 코드 접근.** 게임 코드는 리플렉션으로 부른다.
  - 쓰기는 `propertyData` 칸 직접 쓰기, `UnitAction*` + `CreateAction`, `DataProps` 메서드를 섞어 쓴다.
  - 치트는 Harmony 패치 약 45개다. 처음 켤 때 붙이고 떼지 않는다(우리 `RateService`와 같은 방식).
  - 이 밖에 프레임마다 값을 되돌리는 방식(틱)도 쓴다.
- **안전장치.**
  - 세이브 백업이 없다.
  - 변경 이력이 없다.
  - 재조회는 일부만 한다.
- **보안(사용자 참고).** 브리지 HTTP에는 인증이 없고, 상태를 바꾸는 요청도 모두 GET이다.
  - 같은 PC의 어떤 프로그램이든 아이템·운명·치트를 바꿀 수 있다.
  - 브라우저에서 연 웹 페이지도 `<img src="http://127.0.0.1:17865/bag/add?...">` 같은 요청으로 세이브를 바꿀 수 있다(CSRF. 코드로 본 추론, 미실측).
  - 트레이너를 쓸 때는 게임 중에 모르는 웹 페이지를 열지 않는 편이 안전하다.

## 2. 대조표 — 우리에게 없거나 일부만 있는 것

표기: **없음**(우리에게 없다), **일부**(비슷한 것은 있으나 범위가 다르다), **있음**(같은 효과). 우선순위는 사용 빈도·노력·위험으로 제안한 것이다(높음 / 중간 / 낮음).

### 2.1 전투·자원 치트

| 트레이너 기능 | 우리 | 구현 방법(트레이너가 쓰는 게임 API) | 우리 쪽 설계 제안 | 우선 |
|---|---|---|---|---|
| 무한 체력 | 있음(배율 탭 무적) | `MartialTool.HitDanage` 계열·`FormulaTool+Battle.BattleDamageDC` 접두에서 피격자가 플레이어면 `hitValue=0`, `UnitDataBase.AddHP` 음수 건너뜀 | 지금의 `OnHit` 방식 유지. 독·지속 피해가 새면 `AddHP` 접두를 더한다 | — |
| 무한 영력·집중력 | 있음(05 문서 66절: `없음`이 전투 중 모든 감소를 막음) | `UnitDataBase.AddMP`·`AddSP` 접두에서 플레이어이고 값 < 0이면 건너뜀 | 배율 탭 `영력 소모`·`집중력 소모`의 `없음` 단계에 `AddMP/AddSP` 접두를 더해 모든 소모를 막는다 | 중간 |
| 일격필살 | 일부(주는 피해 배율) | `FormulaTool+Battle.BattleDamage` 접두 `powA=2e9`, `HitDanage`에서 `hp=0` 후 `Die` | 주는 피해 배율에 `일격` 단계 추가(값 대신 `hitValue`를 대상 체력으로) | 낮음 |
| 스킬 즉시 사용 | 있음(기술·신기 쿨타임 없음) | `SkillDataAttack/StepDataBase/FieldSkillDataBase/PropItemDataBase`의 `get_cdUseTime`·`lastShotTime`, `ImmortalSkillBase.IsCool`, 4프레임마다 CD 지움 | 우리 것이 다루지 않는 **선법 기술(`ImmortalSkillBase`)·영역 기술(`FieldSkillDataBase`)·명우 기술·전투 아이템**의 쿨타임을 실측 후 추가 | 중간 |
| 무한 아이템(사용해도 수량 유지) | 있음(05 문서 66절: 배율 탭 `아이템 소모`. 트레이너가 막던 `UnitActionPropUse.CostPropItem`은 실제로 불리지 않았다, 65절) | `UnitActionPropUse.CostPropItem`·`SetUse` 건너뜀, `WorldUnitData.CostPropItem`(세 오버로드)·`CostItem`, `DataProps.DelProps` 건너뜀, 단약 CD `GetPropsPillCD`→0 | 배율 탭 줄 `아이템 소모`(켬/끔). **소모 지점 하나**(`WorldUnitData.CostPropItem`)부터 실측하고, 연단·연기 재료 소모와 섞이지 않게 플레이어의 "사용" 행동에만 건다. 트레이너식 "15프레임마다 수량 되돌리기"는 쓰지 않는다 | 높음 |
| 무한 정력 | **없음**(능력치 탭에서 값만 쓴다) | 패치 없이 매 프레임 `propertyData.energy = max(현재, energyMax)` | 배율 탭 줄 `정력 소모` x1·x0.5·x0 — 정력을 깎는 곳(행동의 `costEnergy`)을 찾아 곱한다. 못 찾으면 틱 방식 | 중간 |
| 법보 내구 유지 | 일부(법보 탭 수리, 60절) | 30프레임마다 `PropsArtifact.durable`을 모양 설정 내구로 | 법보 탭에 `내구 고정` 켬/끔(틱 없이 내구를 깎는 곳에 패치하는 것이 낫다. 실측 필요) | 낮음 |

### 2.2 속도·시간

| 트레이너 기능 | 우리 | 구현 방법 | 설계 제안 | 우선 |
|---|---|---|---|---|
| 게임 속도 빠르게 ×2·×3 / 느리게 ×0.5·×0.25 | 일부(전투 속도만, 전투 밖은 1배) | `UnityEngine.Time.timeScale` 전역 | 배율 탭 `전투 속도`를 `게임 속도`로 넓히거나 줄을 하나 더(지도에서도). 지도의 날짜 진행·애니메이션이 같이 빨라지는지 실측 | 중간 |
| 전투 이동 속도 ×2·×4 | **없음** | 전투 중 `playerUnitCtrl.data.moveSpeed.baseValue`, 밖에서는 `propertyData.footSpeed/moveSpeed`(끄면 원래 값으로) | 배율 탭 줄 `전투 이동 속도`. `BattleService`가 전투 시작 때 플레이어 이동 속도 `DynInt`에 곱셈 호출(`allAddCall`, 우리 가방 칸 방식과 같다)을 걸고 끝날 때 뗀다 | 중간 |
| 세계 시간 정지 | 있음(세계 탭 날짜 고정) | `worldDay`·`gameRunTime` 고정 | — | — |
| 백그라운드 실행 | **없음** | `Application.runInBackground=true`, 모든 `OnApplicationFocus/Pause(bool)` 접두로 건너뜀 | 설정에 `창을 떠나도 계속` 켬/끔. `runInBackground`만으로 되는지 먼저 실측(되면 패치 불필요) | 낮음 |

### 2.3 관계·성공률

| 트레이너 기능 | 우리 | 구현 방법 | 설계 제안 | 우선 |
|---|---|---|---|---|
| 호감 변화 조작(즉시 ±300, 하트 ±1, 변화 ×2·×5·×10) | **없음**(NPC 탭은 값을 바로 쓴다) | `DataUnit+RelationData.AddIntim`·`AddHate` 접두에서 전 값 저장, 접미에서 목표값으로 다시 씀 | 배율 탭 줄 `호감 변화` x1·x2·x5·x10. `AddIntim`의 변화량 인자를 접두에서 곱하는 쪽이 단순(트레이너처럼 사후에 덮어쓰지 않는다) | 중간 |
| 관계 발전 성공 | 있음(05 문서 68절: 성공·드롭 탭. 판정은 NPC 응답 `UnitActionFeedback104`의 필요 점수, 67절) | `UnitActionRoleRelation.get_addIntim`(최소 60)·`get_isNPCReduceIntim`→false | 성공률 묶음(아래)과 함께 | 낮음 |
| 전투 도주·훔치기·포획·드롭·종문 초빙 성공 | 일부(훔치기 성공·드롭 배수는 있음, 68절. 초빙·포획·도주는 판정 위치만, 67절) | 도주 `UnitActionRoleEscape.CompleteRate`→100·`isSureComplete`; 훔치기 `UnitActionRoleStealItem.CompleteRate`→100·`DiscoverRate`→0; 드롭 `ConfGameDifficultyValueItem.get_dropRate`→10000·`PlayerInfoExtendPropsEx.CanDrop`→true; 포획 `ConfDevilDemonParamerBase.get_collectMonstRate`, `ConfPotmonSkillStudyItem.get_studyRate`; 초빙 `FormulaTool+Relation.UnitSchooldScore`≥99999 | 새 탭 `성공률`(켬/끔 여섯 줄) 또는 배율 탭 아래 묶음. 모두 접미로 반환값만 바꾸는 단순 패치라 한 묶음으로 만들 수 있다. 설정표 getter(`get_dropRate`)는 NPC·다른 곳에도 걸리므로 범위를 줄에 적는다 | 중간 |
| 호천안 무소모 | **없음**(손상 해제만) | `ConfGodEyeBaseCost.GetCost`·`ConfGodEyeExtraCost.GetExtraCost`→0 | 신기 탭 호천안 화면에 `사용 비용 없음` 켬/끔 | 낮음 |
| 호계약 무한 | **없음** | `PotMonMgr.GetContractNum`≥999 | 신기 탭 연요호 화면에 `계약 수 무제한` | 낮음 |

### 2.4 성장

| 트레이너 기능 | 우리 | 구현 방법 | 설계 제안 | 우선 |
|---|---|---|---|---|
| 완벽 참오(게임 참오 창에서 굴린 옵션을 최대로) | 일부(공법 탭에서 옵션 품질을 직접 고른다) | `DataProps+MartialData.SetPrefixLevel` 접두(레벨 ≥600), `ConfBattleSkillPrefixValue.RandomPrefixLevel` 접미, `UIMartialRealize` 열림 동안만 | 배율 탭 줄 `참오 결과 최대` 켬/끔. 게임 화면에서 참오할 때만 걸리게 `UnitActionMartialRealize.OnCreate` 안에서만 적용(트레이너의 깊이 추적과 같은 생각) | 중간 |
| 수련 만렙(범례 자물쇠 모두 해제) | 있음(공법 탭 숙련 경험치) | `ActionMartialData.set_exp` | — | — |
| 전투 경험치 ×2·×5·×10 | 있음(전투 중 공법 숙련 배율) | — | — | — |
| 도심 견고 가속·도종 응결치 100% | **없음** | `DataUnit+TaoistHeart.AutoGrow` 접미에서 `SeedUpLevel` 추가 호출, `SeedUpLevel` 인자 ≥5/≥100, `AddHeartEffectHP` ×5, `taoistHeart.seedNeed=0` | 새 기능 `도심`(능력치 탭 하위 또는 경지 탭): 도심 단계·응결치를 읽고 바로 올리는 단추(틱보다 한 번 쓰기). 도심 자료 구조부터 실험 필요 | 중간 |
| 기령 친밀도 만땅 | **없음**(56절 범위 밖) | `Sprite.set_intimacy`/`ArtifactSpriteModelExtensions.SetClose` 접미에서 `artifactSpriteClose.closeMax`까지 | 법보 탭 기령 줄에 `친밀도 최대` 단추(한 번 쓰기, 재조회) | 중간 |
| 탐색(안개 지우기)·시야 상한 | **없음** | 120프레임마다 `BattleFunction.RoleSightWipe()`, `WorldUnitEffectSpecialRoleSight.get_range`≥99 | 세계 탭에 `지도 밝히기`(한 번 실행)와 `시야 넓히기` 켬/끔 | 낮음 |

### 2.5 편집 탭

| 트레이너 기능 | 우리 | 구현 방법 | 설계 제안 | 우선 |
|---|---|---|---|---|
| 성격(내재 1·외재 2) | 있음(05 문서 66절) | `propertyData.inTrait`(1~7)·`outTrait1`·`outTrait2`(8~19, 서로 달라야 함), 목록 `g.conf.roleCreateCharacter` | 능력치 탭 여섯째 분류 `성격`: 세 칸 고르기, 편집 대상(플레이어·NPC). 단순 칸 쓰기라 노력 작음 | 높음 |
| 마을 창고(건목) 읽기·수량·삭제 | **없음** | `MapBuildTownStorage.GetPlayerStorage().data.propData`·`propDataBook`, `set_propsCount`, `DelProps` | 자원 탭 그룹 `창고` 또는 보관함 탭 셋째 화면. 창고에 넣기도(`AddProps`) | 중간 |
| 가방 아이템 수량 지정·삭제 | 있음(05 문서 66절. 장착품은 지우지 않는다) | `PropsData.set_propsCount`, `propData.DelProps` | 자원 탭의 `보유 중만` 줄에 `수량 지정`·`버리기(두 번)` | 중간 |
| NPC 가방 편집 | **없음**(자원은 플레이어) | 같은 API를 대상 유닛의 `propData`에 | 자원 탭을 편집 대상 기준으로(가방 칸·화폐는 플레이어 전용 유지) | 낮음 |
| 법보 추가의 단계(1~6)·한계돌파(rate ≤120) | 일부(법보 지급은 기본값) | `PropsArtifact.SetLevel`/`grade`/`durable`, 비율 `rate` | 법보 탭 `법보 지급` 화면에 단계·강화 고르기, 가진 법보 줄에 `단계 올리기`(56절 후속 후보와 같다) | 중간 |
| 전승(심법 세트) 부여·해제 | 일부(프리셋 적용 때만) | `suitID` 칸, `g.conf.battleAbilitySuitBase`(`activationClaim`), `suitAbility.AddSuitAbilityFeature`/`UpdateSuitAbilityFeature` | 공법 탭 심법 줄에 `전승` 하위 화면(프리셋의 세트 코드 재사용) | 중간 |
| 경지 직접 지정(gradeID만) | 있음(경지 탭·프리셋은 정식 돌파로) | `propertyData.SetGradeID` | 트레이너 스스로 "능력치가 오르지 않는다"고 경고하는 방식. 우리 정식 경로가 낫다 | — |
| 관계: 전체 인물 검색·관계 필터(도려·사제·가족·부부·의형제·종문)·양방향 따로(상대→나 / 나→상대) | 일부(가까운 NPC, 양쪽 같은 값) | `relationData.GetIntim`, `intimToPlayerUnit`·`intimToUnit`, `SetIntimToUnit`; 전체 유닛 `g.world.unit` 색인 | NPC 탭에 `전체 검색`(세계 모든 인물, 페이지)·관계 칩·호감 두 칸 | 중간 |
| 종문: 자원(영석·약재·광재·위신·안정도·번영도·충성도), 기명제자·상한, 청린지(용문 효과 칸) | **없음** | `schoolData`·`GetSchoolAttr`/`AddSchoolAttr`, `g.conf.schoolInitScale`, 청린지 `buildSchool.dragonDoor.data.unitsBuffs`, `g.conf.schoolDragonDoorBuff` | 새 탭 `종문`(탭 줄 17번째). 종주가 아니면 읽기만. 종문 자료가 커서 스펙과 실험이 먼저 | 중간 |
| 돌파: 규칙 영역(주·보조 속성) 교체, 도혼 아이템 만들기 | 일부(프리셋이 신혼·명우만) | 영역 `fieldSkillData.mainType1..3`·`viceType1A..3B`; 도혼 `PropsData.NewProps` + `mainType/viceType1/viceType2/purity` | 경지 탭 하위 화면 `화신 이후`: 신혼·명우(프리셋 코드 재사용)·규칙 영역·도혼 지급 | 중간 |

## 3. 우리에게만 있는 것(참고)

신기 탭(쌍어패·호천안·연요호, 전투 단축키), 선법 탭, 도호 탭, 프리셋(빌드를 단계별로), 보관함(가방·캐릭터 내보내기·가져오기), 기예 배우기와 제작 재료, 학습 상한, 법보 탭의 기령 관리, 세계 순간 이동, 날짜 직접 지정, 배율의 날짜 소모·학습 진도, 세이브 백업과 변경 이력, 창작마당 모드 연동과 한글화, 회차 기반 자동 검증.

## 4. 제안 순서

1. **작고 자주 쓰는 것**: 성격(2.5), 무한 아이템(2.1), 가방 수량 지정·버리기(2.5), 무한 영력·집중력 넓히기(2.1). 모두 단순 칸 쓰기나 접두 하나다.
2. **성공률 묶음**(2.3): 도주·훔치기·포획·드롭·초빙·관계 발전. 반환값만 바꾸는 패치 여섯이라 한 스펙으로.
3. **배율 탭 확장**: 게임 속도(전투 밖), 전투 이동 속도, 호감 변화, 참오 결과 최대, 정력 소모. 배율 탭이 14줄이라 묶음(탭 안 하위 화면) 설계가 먼저 필요하다(탭 줄 상한은 18개, 47절).
4. **새 탭·큰 자료**: 종문(2.5), 창고(2.5), 돌파 화신 이후(2.5), 도심(2.4), 전승(2.5), NPC 전체 검색(2.5).
5. **낮음**: 일격, 법보 내구 고정, 백그라운드 실행, 지도 밝히기·시야, 호천안 비용·계약 수.

각 항목은 기존 방식대로 설계 전 실험(트레이너가 쓴 API가 우리 회차에서도 그렇게 동작하는지) → 스펙 → 구현 → 회차 검증으로 한다.
