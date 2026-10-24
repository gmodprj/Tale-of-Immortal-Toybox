# 신기 탭 설계 (2026-10-14)

## 1. 목적

사용자 요청(2026-10-14): 시작 신기 셋 조사(`docs/07-divine-artifacts.md`)를 바탕으로 신기를 편집기 기능으로 만들기. 사용자가 고른 것:

- 범위: 신기 목록, 쌍어옥패 점수, 호천안, 연요호 호요(네 가지 모두).
- 위치: 새 "신기" 탭.
- 호천안: 요수 단위로 해금(그 요수의 기술을 모두 익힘), 기술마다 장착, "모든 요수 해금".
- 호요 추가: 단계(대경지)를 고른다. 기본값은 플레이어의 지금 대경지.

범위 밖: 쌍어패 반쪽 주기·회수(`GivePisces`는 기록만 바꿔 정식 흐름과 다르다), 호요 승급(조건 미확인), 호천안 단계(`godEyeGrade`) 쓰기, 지우기(호요 방생, 기술 잊기).

## 2. 실측 (회차 `art-1014b`·`c`·`d`, 실험 브랜치 `feat/artifact-research`, 07 문서 7절)

| 항목 | 실측 |
|---|---|
| 신기 목록 | `g.data.world.animaWeapons`(`List<GameAnimaWeapon>`: 1 쌍어패, 2 호천안, 3 연요호). 넣으면 그 신기의 게임 화면이 바로 열려 동작하고 저장 후 유지된다. 지도 화면의 버튼(`UIMapMain.playerInfo.btnPiscesPendant`·`btnGodEye`·`btnDevilDemon`)은 `UpdateUI()`로는 켜지지 않고 다시 불러오면 켜진다. 빼면 다시 불러온 뒤 버튼이 꺼지고 그 신기의 자료는 남는다. 세이브 칸 메타(`g.cache.curCache.data.animaWeapons`)는 저장할 때 따라 바뀐다 |
| 빈 목록 | 빈 목록으로 불러오면 게임이 시작 연출(`IntoGameInfo` → `IntoGameDrama`) → 신기 선택 → 튜토리얼 전투를 다시 튼다 |
| 쌍어옥패 점수 | `g.data.world.resurgeCount`. 부활 정보 화면(`ResurgeInfo`)의 "쌍어옥패 N점"과 지도 화면 초상 옆 숫자. 쓰면 바로 반영, 저장 후 유지 |
| 호천안 손상 | `godEyeData.isDamage`, `repairMonth`. `Repair()`는 수리 맡기기(`repairMonth`가 지금 달+3, `IsCanUse()=false`). `isDamage=false`, `repairMonth=0`을 쓰면 `IsCanUse()=true` |
| 호천안 요수 | `godEyeData.bossGrade`(`Dictionary<int,int>`: 요수 번호 → 단계). 값이 요수 그림 위의 경지 글이 된다(1 연기, 2 축기, 3 결정, 대경지 번호와 같다). 쓰면 "요수 기능" 화면에서 그 요수가 열린다. 화면 아래 "축기경의 요수 격파 0/5"(호천안 돌파 진행)는 `bossGrade`만 쓸 때 바뀌지 않았다. (구현 중 바로잡음: 기술까지 익히면 1/5로 올랐다. 회차 `atab4c`, 05 문서 37절) |
| 호천안 기술 | `godEyeData.skillsID`(익힌 기술 번호들), `equipSkillID`(장착 하나). 요수가 열려 있어야 화면에 나온다. 화면에 "요수 기술 터득(1/3)", 기술에 "장비 중". 저장 후 유지 |
| 요수·기술 표 | `g.conf.godEyeSkills`(95줄): `id`(기술 번호), `name`, `desc`, `bossID`. 요수 이름은 `g.conf.battleUnitAttr.GetItem(bossID).name`(7010 수요, 7091 무라, 340271 약령) |
| 호요 추가 | `g.world.devilDemonMgr.potMonMgr.AddPotMon(호요 번호, 단계, true)`. 단계는 대경지 번호(1을 주면 연기경, 3을 주면 결정경). 경험치는 그 단계의 시작값. 연요호 화면 "모든 호요"에 바로 나오고 저장 후 유지 |
| 호요 수 | 화면 "호요 수량: N/28". 28을 넘겨도 들어가고 "31/28"로 보인다. 28의 출처는 찾지 못했다(`DevilDemonBase.collectBossCount`를 메모리에서 바꿔도 그대로) |
| 호요 경험치 | `PotmonAddExp(호요, 개수, false)`: 개수당 1000(`PotmonParamer.potmonPropAddExp`), 다음 단계 경험치(`PotmonGradeLevel`)에서 멈춘다. 반환값은 더한 양 |
| 호요 표 | `g.conf.potmonBase`(45줄): `id`, `name`(예: 1001 수요의 영), `race` |
| 연요호 손상 | `devilDemonData.isDamage`, `repairMonth`, `brokenCount`. `DevilDemonMgr.IsCanUse()` |
| 대경지 | `g.conf.roleGrade.GetItem(gradeID).grade`(고정본: 경지 9 → 3 결정) |

## 3. 결정

- 탭 "신기"를 선법 다음에 둔다: 능력치 · 자원 · 화폐 · 보관함 · 경지 · 공법 · 선법 · **신기** · 운명 · 기예 · NPC · 세계 · 배율 · 프리셋 · 안전(15개). 검증 스크립트의 탭 이름은 `Tab7`이 신기가 되고 운명부터 하나씩 밀린다(운명 `Tab8`, NPC `Tab10`, 세계 `Tab11`, 배율 `Tab12`, 프리셋 `Tab13`, 안전 `Tab14`). 탭 줄 높이를 `15 * 52`로.
- 편집 대상과 무관하게 플레이어 세계 자료에 쓴다(`UsesTarget=false`).
- 모든 쓰기는 `SaveBackup.EnsureOnce()` 뒤에 하고, 쓴 뒤 다시 읽어 판정하고 `ChangeLog.Add`로 남긴다.
- 신기 목록:
  - 켜기는 목록에 그 번호를 더한다(이미 있으면 "이미 가짐").
  - 끄기는 목록에서 뺀다. **목록에 하나만 남았으면 서비스가 거부한다**("신기는 하나 이상 있어야 합니다. 비우면 게임이 시작 연출과 신기 선택을 다시 엽니다"). 화면은 그 줄의 끄기 버튼을 끄고 이유를 보인다.
  - 끄기는 두 번 눌러 확인한다(`Confirm`. 그 신기의 버튼이 사라진다).
  - 켜고 끈 뒤의 결과 문장에 "지도 화면의 신기 버튼은 저장 후 다시 불러오면 바뀝니다"를 붙인다.
- 쌍어옥패 점수: `NumberField`(Enter)와 −10·−1·+1·+10. 게임의 상한은 미확인이라 편집기는 0~9999로 제한한다(벗어나면 끝값으로).
- 호천안·연요호 하위 화면은 그 신기를 가졌을 때만 들어갈 수 있다(없으면 "자세히" 버튼을 끄고 "신기를 먼저 켜세요").
- 손상 해제: `isDamage=false`, `repairMonth=0`. 손상이 없으면 버튼을 끈다. `Repair()`는 쓰지 않는다.
- 호천안 요수 해금: 요수의 `bossGrade`가 없으면 플레이어의 지금 대경지로 쓰고, 그 요수의 기술(`godEyeSkills`에서 `bossID`가 같은 줄)을 `skillsID`에 없으면 더한다. 이미 해금된 요수는 단계를 바꾸지 않고 빠진 기술만 더한다. 결과 문장: "수요: 해금, 기술 3개 익힘".
- "모든 요수 해금"은 두 번 눌러 확인한다. 결과: "요수 해금 N, 기술 M개 익힘, 건너뜀 K(이미 모두 익힘)".
- 기술 장착: `equipSkillID=기술 번호`. 익히지 않은 기술은 버튼을 끈다. 장착 중이면 "장착 중"으로 보이고 버튼을 끈다. 장착 해제는 없다(게임 화면에서 바꾼다).
- 호요 추가: `AddPotMon(번호, 단계, true)`. **호요가 28 이상이면 거부한다**("호요는 게임 화면의 상한 28까지 넣습니다"). 단계 버튼 1~10, 기본은 플레이어의 대경지. 같은 호요를 여럿 넣을 수 있다(게임도 허용).
- 호요 경험치 채우기: 다음 단계 경험치까지 `PotmonAddExp`를 부른다(개수는 (다음 단계 경험치 − 지금 경험치)/1000을 올림). 이미 경계면 버튼을 끄고 "승급은 게임 화면에서(승급 아이템 필요)". 10단계면 끈다.

## 4. 화면

첫 화면:

    신기                                                        지도 화면의 신기 버튼은 저장 후 다시 불러오면 바뀝니다.
    쌍어패   가짐   점수가 있으면 다시 태어날 수 있다 …                         [끄기]
    호천안   가짐   세계를 더 널리 볼 수 있으며 …            손상 없음          [끄기] [자세히]
    연요호   없음   요수를 집어넣어 …                                          [켜기] [자세히(꺼짐)]
    쌍어옥패 점수  [ 9 ]  [-10] [-1] [+1] [+10]

호천안 화면(`Push`):

    상태: 사용 가능 / 손상(수리 n월)   [손상 해제]     장착: 유서불풍                    [모든 요수 해금]
    이름 검색 [      ]
    요수    단계   기술(익힘/전체)  상태
    수요    연기   1/3            해금      [해금]
       └ 유서불풍  자신을 중심으로 … 피해를 입힌다.   익힘   [장착]
       └ 적엽비화  …                                미익힘  [장착(꺼짐)]
    …

- 요수 줄 아래에 그 요수의 기술 줄을 함께 보인다(요수 95개 기술이 요수 수십 개에 묶여 있어 한 목록으로 충분하다). 검색은 요수 이름과 기술 이름에 건다.

연요호 화면(`Push`):

    상태: 사용 가능 / 손상   [손상 해제]          호요 3 / 28
    가진 호요
    이름        단계   경험치(지금/다음)
    수요의 영    연기   5000 / 8000            [경험치 채우기]
    호요 추가   단계: [1 연기] [2 축기] [3 결정] … [10]      이름 검색 [      ]
    수요의 영   수요   [추가]
    …

- 요소 이름: `ArtifactOn<1|2|3>`, `ArtifactOff<1|2|3>`, `ArtifactState<1|2|3>`, `ArtifactOpen<2|3>`, `ArtifactPoint`(숫자 칸), `ArtifactPointMinus10`·`Minus1`·`Plus1`·`Plus10`, `GodEyeRepair`, `GodEyeUnlockAll`, `GodEyeSearch`, `GodEyeBoss<요수 번호>`(해금), `GodEyeEquip<기술 번호>`, `GodEyeState<요수 번호>`, `DevilRepair`, `DevilCount`, `PotmonExp<soleId>`, `PotmonGrade<1~10>`, `PotmonSearch`, `PotmonAdd<호요 번호>`. 앞머리가 다른 버튼과 겹치지 않게 한다.

## 5. 코드

| 파일 (`src/ModCode/ModMain/`) | 맡는 일 |
|---|---|
| `Services/ArtifactService.cs` (새) | 신기 목록(`Has`, `Enable`, `Disable` — 마지막 하나 거부), 점수(`Point`, `SetPoint`), 손상(`GodEyeDamaged`, `RepairGodEye`, `DevilDamaged`, `RepairDevil`), 플레이어 대경지(`PlayerMajor`), 설정 표의 이름·설명(`Name(n)`, `Desc(n)`) |
| `Services/GodEyeService.cs` (새) | `Boss`(요수 번호, 이름, 기술 줄들), `Skill`(번호, 이름, 설명), `Bosses()`(캐시, `godEyeSkills`를 `bossID`로 묶음, 표 순서), `Find(query)`, `Unlocked`, `Grade`, `Learned`, `Equipped`, `Unlock(boss)`, `UnlockAll(bosses)`, `Equip(skill)` |
| `Services/PotmonService.cs` (새) | `Kind`(번호, 이름, 종족), `Kinds()`(캐시), `Find(query)`, `Owned()`(지금 호요: soleId, 번호, 이름, 단계, 경험치, 다음 단계 경험치), `Count`, `Cap`(28), `Add(kind, grade)`, `FillExp(soleId)` |
| `UI/Tabs/ArtifactTab.cs` (새) | 첫 화면과 두 하위 화면 |
| `UI/EditorWindow.cs` | 탭 등록(선법 다음), 탭 줄 높이 `15 * 52` |
| `Dev/DevCommandsArtifact.cs` (새, `DEVTOOLS`) | 검증용: `aw`(목록·점수·버튼), `awset <번호들>`(검사 준비로 목록을 바로 쓰기), `gestate`, `potlist`, `potcount` |
| `Dev/DevCommands.cs` | 위 명령 연결 |
| `tools/research/artifact-check.ps1` (새) | 검사(6절) |
| `tools/research/ui-check.ps1`, `preset-check.ps1`, `immortal-check.ps1`, `datebag-check.ps1` | 탭 이름(`Tab7` 이상을 하나씩), 탭 수 |

## 6. 검증 (고정본, `session.ps1` 회차)

`artifact-check.ps1 -Part <이름>`:

| 부분 | 보는 것 |
|---|---|
| `list` | 고정본(쌍어패만)에서 쌍어패 끄기가 거부된다(버튼 꺼짐, 서비스 문장). 호천안·연요호 켜기 → 목록 1,2,3. 하나 끄기(두 번 눌러) → 빠짐. 변경 이력 |
| `point` | 숫자 칸 Enter, ±1·±10, 0 아래로 내려가지 않음, `resurgeCount`가 같다 |
| `godeye` | 손상을 만든 뒤(`gedmg`) 해제 → `IsCanUse`. 요수 하나 해금 → `bossGrade`가 플레이어 대경지, 그 요수의 기술 수만큼 `skillsID`. 장착 → `equipSkillID`. 모두 해금(두 번) → 표의 요수 수와 표의 서로 다른 기술 수. 다시 누르면 모두 건너뜀 |
| `potmon` | 단계 3으로 추가 → 호요 하나, 단계 3, 경험치 18000. 경험치 채우기 → 30000(4단계 시작)에서 멈춤. 28개에서 추가 거부 |
| `ui` | 탭이 그려지고 줄과 버튼 이름이 있다, 없는 신기의 "자세히"가 꺼짐, 하위 화면 둘이 그려짐, 콜백 예외 없음 |
| `game` | 게임 화면 캡처: 연요호 "모든 호요"의 "호요 수량", 호천안 "요수 기능"의 해금 요수와 "장비 중", 부활 정보의 점수 |
| `list` → `save` → `-Restart` → `restart` | 저장 후 다시 불러오면 지도 버튼 셋이 켜지고, 점수·요수·호요가 남는다(저장 뒤 몇 초 기다린다) |

기존 검사(`ui-check`의 `base`, `tabs`, `confirm`, `wrap`, `fateguard`, `immortal-check`의 `ui`, 프리셋 `ui`)가 탭 이름을 고친 뒤에도 통과한다. 배포 구성 경고·오류 0, DLL 하나.

## 7. 확인하지 못한 것

- 호천안 장착 기술과 호요의 전투 사용, 천도비호의 발동.
- 호천안 요수의 단계(`bossGrade`)가 기술의 위력에 주는 영향.
- 호요 상한 28의 출처. 대경지나 다른 설정에 따라 바뀌는지.
- 호천안을 가진 세이브를 불러올 때마다 호천안 화면이 저절로 뜨는 원인(07 문서 7.2절).
- 쌍어옥패 점수의 게임 상한, `resurgePower1/2`.
- 호요를 여럿 같은 종류로 넣었을 때의 생활·파견 팀.
