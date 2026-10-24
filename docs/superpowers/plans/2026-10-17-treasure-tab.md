# 법보·기령 탭 구현 계획 (2026-10-17)

> 스펙: `docs/superpowers/specs/2026-10-17-treasure-tab-design.md`. 브랜치 `feat/treasure-tab`(워크트리 `.claude\worktrees\editor-followups`). 순수 논리가 거의 없어(게임 자료 읽기·게임 행동 호출) 단위 시험 대신 회차의 검사 스크립트로 검증한다. 게임에서 확인하지 않은 것은 끝난 것으로 적지 않는다.

## 전역 제약

- C# 7.3, Il2Cpp 래퍼. 게임 폴더는 읽기 전용(설치는 사용자 확인 뒤 `tools/install.ps1`).
- 모든 조작은 첫 변경 전 `SaveBackup.EnsureOnce()`, 결과는 문장, `ChangeLog.Add(대상, "법보", 전, 후, 확인)`.
- 탭 번호 16(탭 배열의 끝, `tabNumbers`에 16). 검사의 탭 탐침(`ui-check.ps1 $tabProbe`)에 `@(16, 'TreasureGiveOpen')`.

## Task 1: TreasureService

**Files:** Create `src/ModCode/ModMain/Services/TreasureService.cs`. Modify `Services/SpriteService.cs`(`IsArtifact`·`Shown`·`ArtifactSlot`을 internal로).

- [ ] 자료 모델 `Treasure`(법보 한 개: PropsData, PropsArtifact, soleID, 번호, 이름, 품질, 경지, 내구/최대, 장착, 든 기령 sole·글)와 `Spirit`(기령: Sprite, soleID, 번호, 이름, Lv/최대, 경험치/다음, 재능 켬/보임, 든 법보 이름·sole), `Kind`(32종), `ItemKind`(열 종).
- [ ] 읽기 `Ready(unit)`, `Treasures(unit)`, `Spirits(unit)`, `Kinds(query)`, `ItemKinds(unit)`. 레벨 경험치 표는 `g.conf.artifactSpriteLevel._allConfList`를 기령 번호별로 한 번 모은다.
- [ ] 조작 `Equip`·`Unequip`(칸 2. `IsCreateAction` 뒤 `CreateAction`), `PullSpirit`(`UnitActionEquipSprite(법보, 0)` 시도 → 안 되면 `spriteSoleID = 0`), `PutSpirit`(장착한 빈 법보에, 어디에도 없는 기령만), `LightTalents`, `LevelUp`(`AddExp(다음 - 지금)`, 안 오르면 +1 더), `Delete`(어디에도 없는 기령만), `Create`(`AddSprite`), `Give`(`EditService.GiveItem` 1개). 각각 전·후를 다시 읽어 확인하고 문장과 이력.

## Task 2: 탭과 개발 명령

**Files:** Create `UI/Tabs/TreasureTab.cs`, `Dev/DevCommandsTreasure.cs`. Modify `UI/EditorWindow.cs`(탭 배열 끝에 `new TreasureTab()`, `tabNumbers`에 16), `Dev/DevCommands.cs`(dispatch에 `DevCommandsTreasure.Execute`).

- [ ] 탭 "법보"(`UsesTarget` 참): 첫 줄(제목, `TreasureMakeOpen`·`TreasureGiveOpen`, 설명), 가진 법보 목록(`TreasureList`, 줄 44, 최대 5줄), 기령 목록(`SpiritList`, 남은 높이), 하위 화면 둘(`SpiritKindSearch`·`SpiritKindList`·`SpiritMake<번호>`, `TreasureKindList`·`TreasureGive<번호>`). 요소 이름은 스펙 3절.
- [ ] 개발 명령 `tr`, `trgive`, `trequip`, `trunequip`, `trmake`, `trput`, `trpull`, `trlight`, `trlevel`, `trdel`(플레이어 기준).
- [ ] 빌드: `dotnet build … -c Release`와 `-p:DevTools=false` 모두 경고 0. 단위 시험 FAILS=0.

## Task 3: 검사와 회차

**Files:** Create `tools/research/treasure-check.ps1`. Modify `tools/research/ui-check.ps1`($tabProbe).

- [ ] `-Part service`: `tr`(없음) → `trgive 3031101` → `trequip` → `trmake 1001` → `trput` → `trlight`(49) → `trlevel`(레벨 2, 경험치 200) → `trpull`(기령 0) → `trdel` → `trunequip`. 줄마다 PASS/FAIL, `FAILS=`.
- [ ] `-Part ui`: 편집기 `Tab16` → 빈 상태 글 → 하위 화면에서 `TreasureGive3031101`·`SpiritMake1001` → 첫 화면의 줄과 단추(`TreasureEquip`·`SpiritPut`·`SpiritLight`·`SpiritLevel`·`TreasurePull`·`SpiritDel` 두 번) → 캡처 `check-treasure.png`.
- [ ] 회차(고정본 base): `service` → `ui`(같은 회차에서는 `ui`가 이미 만들어진 것 위에서 돈다. 깨끗한 상태가 필요하면 새 회차), `ui-check.ps1 -Part tabs`. 결과는 05 문서 56절에.

## Task 4: 문서와 병합

- [ ] 05 문서 56절(쓰는 법, 규칙과 실측, 구조, 검증, 확인하지 못한 것), 인계 문서 1·2(기능 지도)·5(검사 표·개발 명령)·8절, `tools/research` 목록.
- [ ] `--no-ff`로 develop에 병합. 설치는 사용자 확인 뒤.
