# 법보·기령 탭 설계 (2026-10-17)

사용자 요청(2026-10-17 오후): "법보와 기령관리도 추가 검토". 질문 없이 진행하라고 해(“그냥 진행”) 범위와 모양은 이 문서의 결정대로 한다. 05 문서 56절에 구현 기록을 쌓는다.

## 1. 뜻과 범위

- 지금 편집기는 법보와 기령을 프리셋 적용(34절, `SpriteService.Apply`)으로만 다룬다: 번호의 법보를 지급·장착하고 번호의 기령을 만들어 넣고 보이는 재능을 켠다. 사용자가 직접 보고 고를 화면이 없다.
- 새 탭 **법보**: 편집 대상(플레이어 또는 NPC)이 가진 법보와 기령을 한 화면에서 보고, 게임이 허용하는 조작을 단추로 한다.
- 목표: (1) 가진 법보 목록과 상태, 장착·해제, 든 기령 빼기. (2) 가진 기령 목록과 상태, 재능 모두 켜기, 레벨 올리기, 장착한 법보에 넣기, 지우기. (3) 기령 만들기(32종), 법보 지급(10종).
- 하지 않는 것: 법보의 경지 올리기·강화·접두 옵션 바꾸기(`ArtifactShapeUpgrade`·`Prefix`. 게임 화면의 연기·승급 경로를 흉내 내지 않는다), 기령의 친밀도·선물·대화·생활 기술, 기령 장비(`ArtifactSpriteEquip`), 기령을 다른 법보로 옮기기(두 법보가 같은 기령을 가리키는 실측이 있어 빼고 다시 넣는 두 단계로만).
- 반(反)목표: 세이브를 깨뜨리는 조합. 같은 번호의 기령 둘(게임이 거부), 든 기령을 지우기(법보가 없는 기령을 가리킴), 장착하지 않은 법보에 넣기(실측한 순서가 장착 → 넣기).

## 2. 게임 자료(확인한 것)

- 법보 = 아이템 분류 401, 열 종 `3031101`~`3031110`(황염호각·참뢰정추·청명현종·혈문가면·치양지치·벽해응주·서영두봉·황극천검·탄천벽새·이진고금. 품질 5~6). 가진 것은 `unit.data.unitData.propData.allProps`의 `PropsData`이고 `To<DataProps.PropsArtifact>()`에 `grade`(법보 경지. `ConfArtifactShapeItem.initGrade` 2~10에서 시작)·`level`·`durable`(최대 `ConfArtifactShapeItem.durable` 100)·`spriteSoleID`(든 기령의 고유 번호, 0이면 없음)가 있다. 장착 여부는 `unitData.IsEquipProps(soleID)`.
- 장착: `UnitActionPropEquip(soleID, 2)`를 `IsCreateAction`으로 물은 뒤 `CreateAction`(34절 실측. 묻지 않으면 장착되지 않는다). 해제: `UnitActionPropUnequip(2)`(같은 순서로 시도. 미확인 → 회차에서 확인).
- 기령 = `unit.data.unitData.artifactSpriteData.sprites`의 `Sprite`(`soleID`, `spriteID`, `level`, `exp`, `talents[number, state]`, `intimacy`, `stamina`). 종류는 `g.conf.artifactSprite` 32종(1001~1008 이름 있는 기령 품질 5, 2001~2024 품질 1·3). 레벨 경험치는 `g.conf.artifactSpriteLevel`(기령마다 level·exp. 1001~1008은 5단계 0·200·500·1000·2000, 2001~은 4단계). 재능은 `g.conf.artifactSpriteTalent`(기령마다 53개, `isShow` 1인 것만 화면에 보인다. 34절).
- 만들기: `artifactSpriteData.AddSprite(spriteID)`(같은 번호가 있으면 null). 지우기: `DelSpriteInSoleID(soleID)`. 재능 켜기: `ActiveTanlet(soleID, number)`. 경험치: `Sprite.AddExp(value)`(레벨이 오르는지는 회차에서 확인). 넣기: `UnitActionEquipSprite(PropsArtifact, spriteSoleID)`. 빼기: 게임 화면 `UIArtifactSprite.RemoveShape()`의 자료 조작은 미확인 — `UnitActionEquipSprite(법보, 0)`을 먼저 시도하고, 안 되면 `PropsArtifact.spriteSoleID = 0`을 쓴다(회차에서 정한다).

## 3. 화면

탭 "법보"는 탭 줄의 맨 끝(`Tab16`, 도호 다음). 편집 대상이 적용되는 탭(`UsesTarget`)이라 왼쪽에 표시가 붙는다. 세계 밖이면 "세계 자료가 없습니다".

- 첫 줄: 제목 "법보 — <편집 대상>", 단추 `기령 만들기…`, `법보 지급…`(하위 화면), 설명(회색): "장착한 법보에만 기령을 넣습니다. 든 기령은 지우기 전에 빼야 합니다."
- **가진 법보** 목록(줄 44, 최대 5줄 보이고 스크롤): 이름 `[번호]` · 품질 · 경지 이름 · 내구 `지금/최대` · `장착 중`(강조) · 기령: `이름 Lv n` 또는 `없음` · 단추 `장착`(장착 중이면 `해제`) · `기령 빼기`(든 기령이 있을 때만).
- **기령** 목록(남은 높이): 이름 `[번호]` · `Lv n/최대` · 경험치 `지금/다음`(최고면 `최고`) · 재능 `켬/보임` · 법보: 든 법보 이름 또는 `없음` · 단추 `재능 켜기`(꺼진 보이는 재능이 있을 때) · `레벨 올리기`(최고가 아닐 때) · `넣기`(장착한 법보가 있고 비어 있으며 이 기령이 어디에도 없을 때) · `지우기`(두 번 눌러 확인. 어디에도 들어 있지 않을 때).
- 하위 화면 **기령 만들기**: 검색(이름·번호), 32종 줄마다 이름 `[번호]` · 품질 · `가짐`/`-` · `만들기`(이미 가지면 꺼짐).
- 하위 화면 **법보 지급**: 열 종 줄마다 이름 `[번호]` · 품질 · 시작 경지 · 가진 수 · `지급`(하나).
- 모든 조작은 결과 문장을 결과 칸에 보이고 변경 이력에 남긴다(`ChangeLog.Add`). 첫 변경 전 자동 백업(`SaveBackup.EnsureOnce`).

요소 이름: `TreasureNoData`, `TreasureMakeOpen`, `TreasureGiveOpen`, `TreasureList`, `TreasureEquip<sole>`, `TreasurePull<sole>`, `TreasureSpirit<sole>`(기령 글), `SpiritList`, `SpiritLight<sole>`, `SpiritLevel<sole>`, `SpiritPut<sole>`, `SpiritDel<sole>`, `SpiritState<sole>`(Lv 글), `SpiritKindSearch`, `SpiritKindList`, `SpiritMake<번호>`, `TreasureKindList`, `TreasureGive<번호>`.

## 4. 구조

- `Services/TreasureService.cs`: 읽기(`Treasures(unit)`, `Spirits(unit)`, `Kinds(query)`, `ItemKinds()`)와 조작(`Equip`, `Unequip`, `PullSpirit`, `PutSpirit`, `LightTalents`, `LevelUp`, `Delete`, `Create`, `Give`). 문장을 돌려준다. `SpriteService`의 보이는 재능 표(`Shown`)와 상수(분류 401, 칸 2)를 함께 쓴다(internal로 연다).
- `UI/Tabs/TreasureTab.cs`: 그리기만. `EditorWindow`의 탭 배열 끝에 넣고 번호 16.
- 개발 명령 `Dev/DevCommandsTreasure.cs`: `tr`(법보·기령 상태 한 줄), `trgive <아이템>`, `trequip <sole>`, `trunequip`, `trmake <기령 번호>`, `trput <법보 sole> <기령 sole>`, `trpull <법보 sole>`, `trlight <기령 sole>`, `trlevel <기령 sole>`, `trdel <기령 sole>`.
- 검사 `tools/research/treasure-check.ps1 -Part service|ui`(고정본 인물은 법보·기령이 없다고 가정한다).

## 5. 검증

- 회차(고정본): `service` — 지급 → 장착 → 만들기 → 넣기 → 재능 켜기(49) → 레벨 올리기(exp가 다음 단계, level이 1 올랐는지) → 빼기(spriteSoleID 0) → 지우기 → 해제. `ui` — 탭 16이 열리고 줄과 단추가 있으며, 단추로 같은 흐름을 밟는다. 캡처 `check-treasure.png`.
- `ui-check.ps1 -Part tabs`의 탭 탐침에 16을 더한다. 배포 구성 빌드와 단위 시험은 그대로 통과해야 한다.
- 확인하지 못하면 적을 것: 전투에서 켠 재능·올린 레벨이 실제 효과를 내는지(사용자 확인 항목), 빼기 경로가 게임 화면과 같은지.
