# 창작마당 모드 받기·설치(G2a) 설계 (2026-10-15)

## 1. 목적

사용자 요청(2026-10-15, 요청 4): 스팀 창작마당 모드를 이 설치본에 반영하고 한글화하며, 공법 모드는 이 편집기에서 비급을 다룬다. 사용자가 셋으로 나눴다: G2a 받기·설치(이 문서) → G2b 모드 공법의 비급 → G2c 한글화.

사용자가 고른 것:

- 모양: 편집기 화면이 아니라 도구 스크립트 하나(`tools/workshop.ps1`). 모드는 게임을 다시 켜야 읽히므로 게임 안 화면이 줄 것이 없다.
- 목록: 2026-10-15에 설명을 읽어 고른 16개 가운데 `2945768219`(【执余】女NPC颜值评定优化)는 뺀다. DLL 5개에 `MySql.Data.dll`이 있어 바깥 서버와 통신할 수 있는 구성이다. 사용자가 시험으로 받은 `3681025074`도 넣지 않는다. 설치 목록은 15개다.
- 받는 길: Steam 계정 `deepestlight`가 게임을 갖게 되어(2026-10-15 저녁, `app_status 1468810`이 `Subscribed,Permanent`) SteamCMD의 캐시 로그인으로 받는다. 16개 모두 `E:\SteamCmd\steamapps\workshop\content\1468810\<ID>\`에 받아 두었다(약 450MB).

## 2. 실측 (회차 `ws-1015a`, 고정본, 실험 브랜치 `feat/workshop-research`의 `Dev/DevCommandsWorkshop.cs`)

| 항목 | 실측 |
|---|---|
| 게임이 읽는 자리 | `ModMgr.pathModExportData`는 `<게임>/ModExportData`. 그 바로 아래 폴더의 `ModExportData.cache`만 읽는다(`Player.log`의 `加载模组：<경로>/ModExportData.cache` 줄, `g.mod.allModPaths`). 폴더 이름은 아무거나 된다(`2876769227`, `z_2876769227` 모두 로드). `Mod_` 접두는 필요 없다 |
| 한 단계 더 깊은 것 | `<ID>\debug\Mod_dVhgEv\ModExportData.cache`는 읽지 않는다. `Mod_dVhgEv`의 내용을 `<ID>\`로 꺼내 넣자 읽혔다 |
| 받은 항목의 모양 | 평평한 것 12개(`<ID>\ModExportData.cache`, `ModAssets`, `ModExcel`, `ModRes`, `ModCode\dll\*.dll`, `ModProjectPreview.png`), `debug` 모양 4개(`<ID>\debug\Mod_<모드ID>\` + 에디터 프로젝트 `ModProject\`): `2819712935`(Mod_dVhgEv), `2929805068`(Mod_cPugE8), `2945768219`(Mod_ZUQmIr), `3390738990`(Mod_7kyQkw) |
| 모드 ID와 창작마당 번호 | cache 안에 있다. 로드한 뒤 `g.mod.modDatas`의 `soleID`·`name`·`ver`·`publishedFileID`로 읽힌다(`2876769227` → `FASRM7`, `2819712935` → `dVhgEv`, `publishedFileID`가 창작마당 번호와 같다). 모드 이름에 서식 태그(`<color>`, `<size>`, `<i>`)가 섞인 것이 있다 |
| 새 모드의 기본 | 로드 순서 자료(`g.mod.GetLoadOrderData()`)에 없어도 로드된다(`loaded`가 1 → 2 → 3). 순서 자료는 회차 내내 `Mod_sWP6UL=True` 하나였다 |
| 로드 순서 | 이름순도 생성 시각순도 아니다. 2회차 `Mod_sWP6UL, 2876769227`, 3회차 `…, 2876769227, 2819712935`(이름순이면 2819가 앞), 4회차(2876769227을 `z_2876769227`로 바꿈) `…, 2819712935, z_2876769227`. 게임이 처음 발견한 순서를 적어 두고 새 폴더를 뒤에 붙이는 것으로 보인다. 회차 중에 바뀐 파일은 세이브 폴더의 `Steam\CacheData\DataGloble.cache`뿐이라 거기 적히는 것으로 추정한다(암호화돼 내용은 미확인). 한 번에 여러 폴더를 처음 발견할 때의 순서는 미확인 |
| 받기 | `steamcmd +login deepestlight +runscript <파일>`(줄마다 `workshop_download_item 1468810 <ID>`). 익명 로그인은 `failed (Failure)`. 라이선스가 없을 때의 실패 문구는 `No workshop depot defined, skipping non-legacy item`(인계 8.3b절) |
| DLL이 든 것 | `2814703549` Fatury(`Fatury.dll`, `UnityEngine.InputLegacyModule.dll`, `YamlDotNet.dll`), `2832167498`(1), `2839261049`(`MOD_uCKcKw.dll`), `3001496635`(1), `2945768219`(5, 제외) |
| 회차 스크립트 | `session.ps1 -End`는 편집기 DLL만 되돌리고 `ModExportData\`에 새로 생긴 폴더는 지우지 않는다. `-Begin`·`-Restart`가 넘기는 "mod warning" 창은 모드가 둘 늘었을 때도 그대로 넘어갔다 |

## 3. 결정

### 3.1 목록 파일 `tools/workshop/mods.txt`

git에 넣는다. `#`으로 시작하는 줄은 주석. 한 줄에 하나, `|`로 나눈 다섯 칸: `창작마당ID | 종류 | 원문 이름 | 한국어 이름 | 비고`. 종류는 `틀`·`외형`·`공법`. 원문 이름은 창작마당 제목에서 서식 태그를 뺀 것. 줄 순서가 설치 순서이고 Fatury를 첫 줄에 둔다. 스크립트는 이 목록에 있는 ID만 다룬다.

| ID | 종류 | 원문 이름 | 한국어 이름 | 비고 |
|---|---|---|---|---|
| 2814703549 | 틀 | Fatury | Fatury 틀 | 2815562168이 필요로 함. DLL 3 |
| 2815562168 | 외형 | 原版17个女性人物动态立绘替换 | 원작 여성 인물 17명 동적 입화 교체 | Fatury 필요. 111MB |
| 3200319699 | 외형 | 【木头】AI动态立绘 | [木头] AI 동적 입화 | 206MB |
| 3001496635 | 외형 | 【软甜废独家】《五官头饰动态立绘》-原创本人手画 | [软甜废] 이목구비·머리 장식 동적 입화 | DLL 1 |
| 2993500314 | 외형 | 动态头发美化 | 동적 머리카락 미화 | |
| 2819712935 | 외형 | 女npc颜值优化 | 여성 NPC 외모 개선 | debug 모양(Mod_dVhgEv) |
| 2823930683 | 외형 | 男性NPC颜值优化 | 남성 NPC 외모 개선 | |
| 2945768367 | 외형 | 【执余】大量生成美女 | [执余] 미녀 대량 생성 | |
| 2874128983 | 외형 | 人物美化 .服装解锁 .魅力与权重修改 | 인물 미화·복장 해금·매력과 가중치 수정 | |
| 2876769227 | 외형 | 【只有梦长】特殊NPC服装解锁 | [只有梦长] 특수 NPC 복장 해금 | 모드 ID FASRM7 |
| 2929805068 | 외형 | 【大姨夫】仙衣阁、美化 | [大姨夫] 선의각·미화 | debug 모양(Mod_cPugE8) |
| 2832167498 | 외형 | 【含羞草】捏脸 | [含羞草] 얼굴 만들기 | DLL 1 |
| 2814806198 | 외형 | 过场图替换示例mod | 전환 화면 그림 교체 예제 모드 | |
| 3390738990 | 공법 | 【只有梦长】五本功法开源 | [只有梦长] 공법 다섯 권(오픈소스) | debug 모양(Mod_7kyQkw). 새 공법 다섯 |
| 2839261049 | 공법 | 真.镇宗之宝：传承心法 | 진·진종지보: 전승 심법 | DLL 1(MOD_uCKcKw.dll) |

제외한 것은 주석 줄로 남긴다: `# 2945768219 【执余】女NPC颜值评定优化 — MySql.Data.dll이 들어 있어 제외(2026-10-15)`, `# 3681025074 — 사용자의 시험용, 목록에 넣지 않음`.

### 3.2 스크립트 `tools/workshop.ps1`

동작 넷 가운데 하나를 고른다. 매개변수: `-GameDir`(기본 `E:\Games\Tale of Immortal`), `-SteamCmd`(기본 `E:\SteamCmd\steamcmd.exe`), `-Account`(기본 `deepestlight`), `-List`(기본 `tools\workshop\mods.txt`), `-AppId`(기본 `1468810`), `-Id`(쉼표로 나눈 창작마당 번호. 없으면 목록 전체), `-Yes`. 받은 자리는 `<SteamCmd 폴더>\steamapps\workshop\content\<AppId>\<ID>\`, 설치 자리는 `<GameDir>\ModExportData\<ID>\`다.

- `-Status`: 목록의 줄마다 한 줄. `ID | 종류 | 이름 | 받음 | 설치 | 갱신 | DLL`. 받음은 폴더가 있으면 크기(MB)와 `appworkshop_<AppId>.acf`의 `timeupdated` 날짜, 없으면 `-`. 설치는 `예`/`-`. 갱신은 받은 원본과 설치본을 `robocopy /L /MIR`로 비교해 다른 파일이 없으면 `같음`, 있으면 `다름 n`, 어느 한쪽이 없으면 `-`. DLL은 원본의 `*.dll` 수. 마지막 줄에 `받음 a/n, 설치 b/n`.
- `-Download`: 받지 않은 것(또는 `-Id`로 고른 것)을 `workshop_download_item <AppId> <ID>` 줄로 적은 임시 runscript 파일을 만들어 `steamcmd +login <Account> +runscript <파일> +quit`으로 받는다. 빠진 것이 남으면 같은 방법으로 최대 세 번. 출력의 `Success. Downloaded item`과 `ERROR! Download item <ID> failed`를 읽어 줄마다 `OK`/`FAIL`을 보이고, 끝에 `받음 a/n`. 로그인이 풀려 있으면(출력에 `Cached credentials not found`, `Login Failure`, 비밀번호 입력 요구) 멈추고 사용자가 칠 명령을 그대로 보인다: `E:\SteamCmd\steamcmd.exe +login deepestlight +quit`(비밀번호·Steam Guard는 사용자). `No workshop depot defined`가 나오면 "이 계정은 게임을 갖고 있지 않습니다"라고 보인다. 비밀번호는 어디에도 받지 않는다.
- `-Install`: 받은 것(또는 `-Id`)을 설치한다. 설치 원본 고르기: `<ID>\ModExportData.cache`가 있으면 `<ID>\` 전체, 없으면 `<ID>\debug\Mod_*\ModExportData.cache`가 정확히 하나일 때 그 `Mod_*` 폴더. 둘 다 아니면(받지 않음, `Mod_*`가 둘 이상) 그 줄은 `건너뜀`과 이유. 원본을 `robocopy /MIR`로 `<GameDir>\ModExportData\<ID>\`에 복사한다(갱신도 같은 명령). `-Yes`가 없으면 계획만 보인다: 줄마다 `ID | 이름 | 원본 모양(평평함/debug) | 크기 | DLL 이름들 | 새로 설치/갱신(다른 파일 n)/같음` 그리고 마지막 줄 `PLAN ONLY (아무것도 쓰지 않았습니다. 실제로 쓰려면 -Yes)`. `-Yes`가 있으면 복사하고 줄마다 `INSTALLED`/`UPDATED`/`UNCHANGED`, 끝에 설치본의 `ModExportData.cache`가 있는지 다시 확인한다. 이미 설치돼 있는 폴더를 덮을 때는 게임이 꺼져 있어야 한다(`guigubahuang` 프로세스가 있으면 그 줄을 거부). 새 폴더는 게임이 켜져 있어도 넣는다(게임은 시작할 때만 읽으므로 "다음 게임 시작부터 읽힙니다"를 보인다). 목록에 없는 ID는 거부.
- `-Uninstall`: 목록의 ID(또는 `-Id`)의 `<GameDir>\ModExportData\<ID>\`를 지운다. `-Yes`가 없으면 계획만. 게임이 켜져 있으면 모두 거부. 지우는 경로가 `<GameDir>\ModExportData\` 바로 아래의 목록 ID 폴더인지 확인한 뒤에만 지운다. `Mod_*`와 목록에 없는 폴더는 어떤 경우에도 건드리지 않는다.

설치 상태는 따로 기록하지 않는다. 폴더가 있으면 설치된 것이다. 게임 폴더에 쓰는 것은 `ModExportData\<ID>\`뿐이고, `-Yes` 없이는 아무것도 쓰지 않는다(CLAUDE.md의 "게임 폴더는 사용자 확인 뒤에만"을 스크립트에 박는 것).

### 3.3 회차 스크립트 확장 (`tools/DevRemote/session.ps1`)

- `-Begin`: `<GameDir>\ModExportData\` 바로 아래 폴더 이름들을 `.devremote\session-modfolders.txt`에 적는다(회차 전부터 있던 것).
- `-End`: 그 파일에 없는 폴더를 지우고 `WORKSHOP FOLDERS REMOVED: n`을 보인다. 지우는 것은 `<GameDir>\ModExportData\` 바로 아래, 이름이 `Mod_<ModId>`가 아닌 폴더뿐이다. 지운 뒤 그 파일을 없앤다.
- `-Restart`는 바꾸지 않는다.
- `session.tests.ps1`에 둘: 회차 중 생긴 폴더는 `-End`가 지운다, 회차 전부터 있던 폴더와 `Mod_<ModId>`는 남는다. 시험은 임시 폴더의 `-GameDir`로 돈다(실제 게임 폴더를 건드리지 않는다).

### 3.4 개발 명령 `wsmods`

실험 브랜치의 `Dev/DevCommandsWorkshop.cs`를 그대로 가져온다(검증용). `wsmods` → `path=… loaded=<수> cache=… paths=<soleID|경로;…> mods=<soleID:이름:판:창작마당 번호;…> ns=<DLL 경로;…> order=<폴더 이름=isLoad;…>`.

## 4. 코드

| 파일 | 일 |
|---|---|
| `tools/workshop/mods.txt` | 새 파일. 3.1의 15줄과 제외 주석 둘 |
| `tools/workshop.ps1` | 새 파일. 3.2. 목록 읽기, 원본 고르기, 계획 만들기, robocopy, SteamCMD 호출과 출력 읽기를 함수로 나눈다. 시험을 위해 `-ProcessName`(기본 `guigubahuang`)과 `-ContentDir`(받은 자리를 바꿈)를 둔다 |
| `tools/workshop.tests.ps1` | 새 파일. 5절. 게임도 SteamCMD도 없이 임시 폴더에서 돈다 |
| `tools/DevRemote/session.ps1`, `session.tests.ps1` | 3.3 |
| `src/ModCode/ModMain/Dev/DevCommandsWorkshop.cs`, `Dev/DevCommands.cs` | 3.4(실험 브랜치 `feat/workshop-research`의 커밋을 가져온다) |
| `CLAUDE.md` | 게임 폴더 규칙에 예외 한 줄: `ModExportData\<창작마당ID>\`는 `tools/workshop.ps1 -Install -Yes`로, 사용자 확인 뒤에 |

## 5. 검증

게임 없이:

- `pwsh tools/workshop.tests.ps1`: 임시 폴더에 가짜 받은 항목(평평한 것, `debug` 모양, `Mod_*`가 둘인 것, 받지 않은 것, DLL이 든 것)과 가짜 게임 폴더(`ModExportData\Mod_sWP6UL`, 목록에 없는 폴더 하나)를 만들고 목록 파일을 쓴다. 확인: `-Status`의 줄과 요약, `-Install`(계획만)이 아무것도 쓰지 않음, `-Install -Yes`가 평평한 것은 그대로·`debug` 모양은 꺼내서 복사함, `Mod_*`가 둘인 것은 건너뜀, 받지 않은 것은 건너뜀, 두 번째 `-Install -Yes`는 `UNCHANGED`, 원본을 바꾼 뒤에는 `UPDATED`(게임이 켜져 있으면 거부: `-ProcessName`에 이 시험 프로세스의 이름을 줌), 목록에 없는 `-Id`는 거부, `-Uninstall`(계획만)이 지우지 않음, `-Uninstall -Yes`가 목록 ID 폴더만 지우고 `Mod_sWP6UL`과 모르는 폴더는 남김, 게임이 켜져 있으면 `-Uninstall` 거부. `-Download`는 `-SteamCmd`에 가짜 스크립트(runscript 파일을 읽어 폴더를 만들고 `Success. Downloaded item` 줄을 찍되 첫 번째 호출에서는 하나를 `failed`로 찍는 것)를 주어 재시도와 요약을 본다. 줄마다 PASS/FAIL, 마지막 줄 `FAILS=<수>`.
- `pwsh tools/DevRemote/session.tests.ps1`: 기존 23개 + 둘.
- 실제 SteamCMD로 `-Status`(받음이 보이는지)와 `-Download`(모두 받아져 있어 `받음 15/15`로 끝나는지).

게임에서(회차 `ws-1015b`, 고정본):

1. `-Begin ws-1015b -Fixture base` → `WORLD READY`. `wsmods`로 기준(모드 1).
2. `tools/workshop.ps1 -Install`로 계획을 보고 `-Install -Yes`로 15개를 넣는다(새 폴더라 게임이 켜져 있어도 된다).
3. `-Restart` → `WORLD READY`(mod warning 창이 넘어가는지). `Player.log`의 `加载模组` 줄 16개(15 + `Mod_sWP6UL`), `wsmods`의 `loaded=16`과 `mods`의 창작마당 번호 15개(각 모드 ID를 05 문서에 적는다), `ns`에 DLL 모드 4개의 경로.
4. `ui-check.ps1 -Part base`와 `tabs`: 다른 모드가 있어도 편집기가 열리고 탭이 그려진다. `MelonLoader\Latest.log`에 다른 모드의 예외가 있으면 적는다(고치지는 않는다).
5. 게임 모드 화면의 순서: 주 화면에서 열어야 하므로 회차 안에서 `uiopen`·`rc.ps1`로 열어 캡처를 시도한다. 열지 못하면 사용자 확인 항목(인계 8.5절)으로 남긴다.
6. `-End ws-1015b` → `SAVE RESTORED AND IDENTICAL`, `WORKSHOP FOLDERS REMOVED: 15`, `ModExportData\`에 `Mod_sWP6UL`만.

실제 설치: 회차 밖에서 사용자 확인을 받고 `-Install -Yes`로 한다. 그 뒤 사용자가 플레이한다.

## 6. 문서

- 05 문서 41절 "창작마당 모드 받기·설치(G2a)": 실측(2절), 규칙, 각 모드의 모드 ID, 검증 결과, 확인하지 못한 것.
- 06 인계: 1절 상태, 4절(설치에 `workshop.ps1`), 5절 도구에 `wsmods`와 `workshop.ps1`, 8.3b절을 "G2a 끝남"으로 고치고 G2b·G2c를 다음 일로, 8.7절 회차 이름.
- CLAUDE.md: 게임 폴더 규칙의 예외 한 줄.

## 7. 범위 밖

- 한글화(G2c), 모드 공법의 비급(G2b).
- 로드 순서 바꾸기와 켜고 끄기: 게임의 모드 화면이 한다.
- 창작마당의 갱신 알림(온라인 비교). `-Download`를 다시 돌리면 SteamCMD가 새 판을 받는다.
- `2945768219`와 `3681025074`.
- 창작마당에 올리기, 다른 계정.

## 8. 확인하지 못한 것, 위험

- DLL 모드 4개(Fatury, `2832167498`, `2839261049`, `3001496635`)는 게임 안에서 코드로 실행된다. 편집기와의 충돌은 회차 3~4단계에서 처음 본다.
- 한 번에 15개를 처음 발견할 때의 로드 순서. Fatury(`2814703549`)는 번호가 가장 작아 이름순이면 첫째지만, 게임이 다른 순서를 쓰면 `2815562168`이 Fatury보다 먼저 로드될 수 있다. 5단계에서 본다.
- 로드 순서가 `DataGloble.cache`에 남는다는 것은 추정이다. 회차 `-End`가 세이브 폴더를 되돌리므로 사용자의 순서에는 영향이 없다.
- SteamCMD는 큰 항목(206MB)에서 시간 초과를 낼 수 있다. 세 번 재시도로 받는다(2026-10-15에는 첫 번에 16개 모두 받았다).
- 캐시 로그인은 언젠가 풀린다. 그때는 사용자가 한 번 로그인한다(3.2).
- `robocopy /MIR`로 설치본을 갱신하면 설치본에만 있던 파일(모드가 실행 중 만든 것)이 지워진다. 창작마당 모드가 자기 폴더에 파일을 쓰는지는 미확인이다.
