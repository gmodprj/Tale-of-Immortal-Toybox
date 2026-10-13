# 06. 작업 인계 (2026-10-13 기준)

다음 세션이 이 문서만 읽고 이어서 일할 수 있도록 현재 상태, 일하는 방법, 남은 일을 정리한다.
세부 실험 기록은 `docs/05-editor-v0.1.md`(1~25절)에 있고, 이 문서는 그 길잡이다.

**이어받는 순서**: 1절(상태)을 훑고 → 8절(다음에 할 일)을 읽고 → 일을 시작할 때 5절(검증 방법)과 6절(함정)을 본다.

## 1. 한눈에 보는 상태

- 대상 게임: 귀곡팔황(Tale of Immortal) v1.2.113.259, `E:\Games\Tale of Immortal`. Unity 2020.3 IL2CPP, MelonLoader 0.5.4.
- 결과물: 게임 안에서 `Insert`로 여닫는 편집기(코드로 만든 uGUI 창, 1500x1000). 공식 모드 경로(`ModMain.Init/Destroy`)로 로드된다.
- 모드 ID `sWP6UL`, 어셈블리 `MOD_sWP6UL.dll`.
- 저장소 `E:\TlToybox`, 로컬 git만 사용(원격 없음). 통합 브랜치 `develop`이 최신이고 작업 트리는 깨끗하다. `main`은 초기 커밋 그대로다(릴리스한 적 없음).
- 게임에 설치된 것: 배포 빌드(개발 도구 제외)가 `ModExportData\Mod_sWP6UL\ModCode\dll\`에 있다. `develop`의 마지막 코드와 같다(2026-10-12 20:30 설치).
- 사용자의 세이브: 1번 슬롯(인물 "신환"). 시험 뒤에는 매번 시험 전 복사본으로 되돌렸다. 2026-10-12 밤(20:37~21:14)에 사용자가 설치본으로 직접 플레이하며 편집기를 썼다(영근·기예 +100, 가방 추가 칸 +90, 날짜 직접 지정, 선천운명 교체. `changes.log`에 남아 있다). 그래서 모드의 가방 칸 기록(`bag-slots.txt`)에 `kh3o73=90`이 있다. 검증 회차는 세이브와 이 파일을 시험 전 상태로 되돌린다.
- 직전 작업: 묶음 B(화면 구조 정리와 탭별 UI/UX 개선)의 1차(검증 도구, 창의 틀, 탭 분리)를 끝냈다(05 문서 26절). 다음은 2차 계획이다(8.3절).

## 2. 편집기 기능 지도

탭 순서: 능력치 · 자원 · 경지 · 공법 · 운명 · 기예 · NPC · 세계 · 안전.

| 탭 | 기능 | 코드 | 실험 기록(05 문서) |
|---|---|---|---|
| 능력치 | 게임 인물 화면의 다섯 분류(개인·전투·공법 자질·영근 자질·기예)로 저장값 편집, 증감 단위 1/10/100/1000/직접 입력, 자원 회복 | `AttributeCatalog`, `EditService.SetAttribute` | 5, 14, 15절 |
| 자원 | 영석 추가, 아이템 표(번호·카테고리·이름·품질·보유·설명), 이름·카테고리 필터, 페이지당 10/20/50, 지급 수량, 가방 칸 ±10 | `ItemCatalog`, `EditService.GiveItem`, `BagService` | 14, 15, 25절 |
| 경지 | 수련치 채우기, 소경지 돌파, 대경지는 재료·천재지보 지급 후 게임의 돌파 화면 열기 | `EditService`(Breakthrough, GiveBreakthroughMaterials) | 8, 9, 20, 21절 |
| 공법 | 익힌 공법 목록, 옵션 품질·교체, 숙련 경험치, 장착·해제·망각, 기술 추가(다섯 분류·경지·이름, 모두 배우기, 강제 추가) | `MartialService`, `SkillCatalog` | 7, 22, 23, 24절 |
| 운명 | 선천운명 추가·교체·삭제, 후천운명 추가·삭제, 역천개명(경지별) 지정 | `LuckService`, `FateService` | 10, 15~19절 |
| 기예 | 연단·연기·부적·풍수·약재·광재의 서적 배우기·잊기·모두 배우기·강제 추가 | `ArtService` | 22, 23, 24절 |
| NPC | 가까운 NPC 목록·이름 검색, 호감도, 편집 대상으로 지정 | `EditService`(SetIntimBoth) | 5절 |
| 세계 | 순간 이동, 날짜(고정, +1·+5·+10일, 다음 달 1일로, 연·월·일 직접 지정), 전투 무적·피해 배율 | `WorldService`, `TimeService`, `BattleService`(`WorldService.cs` 안) | 13, 25절 |
| 안전 | 세이브 백업, 변경 이력 | `Safety/SaveBackup`, `Safety/ChangeLog` | 3절 |

공통 규칙:

- 쓰기 전에 한 실행에 한 번 세이브 폴더를 백업한다(`SaveBackup.EnsureOnce`). 백업과 이력은 `%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\TlToybox\`(`backups\`, `changes.log`, `config.txt`, `editor-lucks.txt`, `bag-slots.txt`).
- 쓰기 뒤에 값을 다시 읽어 이력에 남긴다. 다르면 "(재조회 불일치)"가 붙는다.
- 되돌리기 어렵거나 게임이 붙인 것을 지우는 동작은 버튼을 두 번 눌러야 한다("정말 삭제", "정말 강제", "정말 변경" 등).
- 능력치·공법·운명은 편집 대상(플레이어 또는 NPC)에, 자원·경지·기예·세계는 플레이어에 적용된다.
- 켜고 끄는 기능(무적, 피해 배율, 날짜 고정)은 저장하지 않는다. 게임을 켤 때마다 꺼진 상태로 시작한다.
- 편집기는 게임 화면 사이에 끼어 그려진다. 편집기를 연 뒤에 뜨는 게임 창은 편집기 위에 나오고, `Insert`는 가려진 편집기를 맨 앞으로 가져온다(맨 앞이면 닫는다). 켜 둔 기능은 화면 위쪽 가운데에 표시된다.
- 게임 코드에 끼어드는 방법은 두 가지를 쓴다: 이벤트 구독(전투 보조)과 Harmony Prefix(날짜 고정, 처음 켤 때만 설치).

## 3. 저장소 구조

| 경로 | 내용 |
|---|---|
| `docs/00-design-brief.md` | 기획서(기준 문서) |
| `docs/01-game-structure.md`, `02-mod-dev-research.md`, `02a-workshop-survey.md`, `04-ingame-state-analysis.md` | 사전 조사 |
| `docs/05-editor-v0.1.md` | 편집기의 범위·규칙·실험 기록(1~26절) |
| `docs/06-handoff.md` | 이 문서 |
| `docs/superpowers/specs/`, `plans/` | 작업 단위의 설계 스펙과 구현 계획. 사전 조사, 묶음 A(`2026-10-12-date-bag*`), 묶음 B(`2026-10-13-ui-rework*`) |
| `src/ModCode/ModMain/ModMain.cs` | 진입점(`Init`, `Destroy`) |
| `src/ModCode/ModMain/Services/` | 게임 값의 읽기·쓰기. 화면을 모르고 결과를 문장으로 돌려준다 |
| `src/ModCode/ModMain/UI/` | `Ui.cs`(uGUI 도우미), `EditorHost.cs`(단축키, 매 프레임 처리), `EditorWindow.cs`(창의 틀: 제목 줄·탭 줄·결과 영역·앞뒤 순서·이동·접기), `StatusOverlay.cs`(켜 둔 기능 표시), `Tabs/`(탭마다 한 파일, `EditorTab` 상속), `Widgets/`(공통 부품. 지금은 `AmountBar`) |
| `src/ModCode/ModMain/Safety/` | 백업, 변경 이력 |
| `src/ModCode/ModMain/Dev/` | 개발 빌드 전용 명령(배포 빌드에서 제외) |
| `tools/install.ps1` | 배포 빌드를 만들어 게임에 설치 |
| `tools/DevRemote/` | 게임을 화면 밖에서 조작하는 도구(MelonMod + 스크립트). `session.ps1`이 검증 한 회차를 묶고, `session.tests.ps1`이 그 안전 규칙을 시험한다 |
| `tools/research/` | 게임 어셈블리 조사 스크립트, 게임 내 자동 검증(`major-breakthrough-check.ps1`, `datebag-check.ps1`, `ui-check.ps1`. 공통 함수는 `check-lib.ps1`) |
| `ModProject_sWP6UL/`, `.devremote/`, `.superpowers/` | git 제외. 모드 에디터 프로젝트, 조작 도구의 작업 폴더와 세이브 복사본, 계획 실행 중의 임시 기록 |

`src/ModCode/ModMain/Adapters`, `Features`, `Resolvers`와 `tests/`는 처음 뼈대만 만들고 쓰지 않았다(`.gitkeep`만 있음).

## 4. 빌드와 설치

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release                # 개발 빌드(Dev 명령 포함)
    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release -p:DevTools=false -o <임시폴더>   # 배포 구성 컴파일 확인
    pwsh tools/install.ps1                                                     # 배포 빌드를 게임에 설치

- 게임이 켜져 있으면 DLL이 잠겨 설치·교체가 안 된다. 한 번 로드된 DLL은 게임을 끄기 전에는 바뀌지 않는다.
- 두 구성 모두 컴파일되는지 매번 확인한다(`Dev\`의 코드를 배포 코드가 참조하면 배포 구성에서 깨진다. `#if DEVTOOLS`로 감싼 것은 된다).
- 스크립트는 PowerShell 7(`pwsh`)로 실행한다.

## 5. 게임에서 검증하는 방법

사용자의 화면·마우스·키보드를 건드리지 않고 한다. 사용자가 게임을 켜 둔 동안에는 시험할 수 없고, 사용자의 게임을 임의로 끄지 않는다(끄도록 알리고 기다린다).

### 회차 스크립트 (기본)

    pwsh tools/DevRemote/session.ps1 -Begin <이름>     # 확인 → 개발 빌드 → 세이브 복사·검증 → 설치 → 화면 밖에서 실행해 1번 세이브 진입
    pwsh tools/DevRemote/session.ps1 -Restart          # 게임만 껐다 켠다(저장 후 재시작 확인)
    pwsh tools/DevRemote/session.ps1 -End <이름>       # 종료 → 세이브 복원·일치 확인 → bag-slots.txt와 설치돼 있던 DLL 되돌리기

- `-Begin`은 "WORLD READY", `-End`는 "SAVE RESTORED AND IDENTICAL"이 나와야 성공이다. `-Begin <이름> -ReleaseBuild`는 배포 구성으로 설치해 띄운다(개발 명령이 없으므로 30초를 기다리기만 한다).
- 회차는 한 번에 하나다. `-Begin`이 표식(`.devremote\session.name`)을 남기고, `-End`는 열린 회차와 이름이 같을 때만 세이브를 되돌린다. 되돌리기 직전의 세이브는 `.devremote\save-before-restore-<이름>`에, 시험 전 복사본은 `save-backup-<이름>`에 남는다.
- 자기가 띄우지 않은 게임은 닫지 않고, 그런 게임이 실행 중이면 띄우지도 되돌리지도 않는다. 게임은 창 닫기 요청으로 끈다(`Stop-Process`는 권한 검사에 막힌다).
- 게임은 달이 넘어갈 때 자동 저장한다. 저장 명령을 쓰지 않은 회차도 반드시 `-End`로 끝낸다.
- 이 규칙의 시험은 `pwsh tools/DevRemote/session.tests.ps1`(게임을 띄우지 않고 임시 폴더에서, 14개)이다. 스크립트를 고치면 다시 돌린다.

### 회차 안에서 쓰는 도구

- `mod.ps1 -Cmd "<명령>"` — 모드의 개발 명령. `editor`가 단축키 없이 편집기를 연다. 목록은 `Dev\DevCommands*.cs`. 날짜·가방은 `wdate`, `grid`, `wmove`, `wexp`, `tdesc`, `tadv`, `tset`, `tskip`, `tfreeze`, `tin`, `tmsg`, `thud`, `bag`, `bagstat`, `gridfill`.
- `rc.ps1 -Cmd "click x y" -NoShot` / `"hit x y"` / `"scroll x y delta"` — 게임 좌표(1920x1080)의 UI 요소를 누르거나 이름을 확인한다.
- `win.ps1 -Do shot -Out <파일> -Scale 0.6` — 창 캡처. 캡처 좌표에서 게임 좌표로: `(x / Scale - 8, y / Scale - 31)`.
- `win.ps1 -Do key -Vk 0x2D` — Insert 키. 모드의 단축키는 전달되지만 게임 자체의 단축키(ESC, 달 넘기기 `Z`)는 전달되지 않았다. 입력 칸을 골라 입력 상태로 만든 뒤에는 글자 키(`0x41` 등)와 Enter(`0x0D`)가 그 칸에 들어간다(2026-10-13 실측, 묶음 B 스펙 3.4절).
- `tools/research/datebag-check.ps1 -Part <이름>` — 날짜·가방의 자동 검증(PASS/FAIL). 부분은 `base`, `time`, `time-restart`, `freeze`, `bag`, `bag-restart`, `bag-corrupt`. `freeze`와 `bag`은 깨끗한 회차에서 시작하고, `time-restart`·`bag-restart`·`bag-corrupt`는 앞 부분 뒤에 `-Restart`를 하고 돌린다.
- `tools/research/ui-check.ps1 -Part <이름>` — 편집기 화면의 자동 검증. 부분은 `base`, `tabs`, `order`, `move`, `move-restart`, `move-corrupt`, `status`, `status-battle`, `battle`. `move-restart`와 `move-corrupt`는 앞 부분 뒤에 `-Restart`를 하고 돌린다. `battle`과 `status-battle`은 전투에 들어간 채로 끝나므로 각각 회차의 마지막에 돌리고 바로 `-End` 한다.

편집기의 버튼은 좌표가 아니라 이름으로 찾는다. `mod.ps1 -Cmd "uipos <이름>"`이 가운데의 게임 좌표를 준다(이름 끝의 `*`는 "이 글자로 시작"). 검증 스크립트에서는 `check-lib.ps1`의 `Press <이름>`(찾아서 누름), `IsAt <이름>`(그 자리의 클릭이 그 요소로 가는지)을 쓴다. `uistate`가 창의 상태(켜 둔 기능 표시, 열림, 탭, 대상, 방식, 정렬, 가려짐, 위치, 접힘, 결과 문구)를 주고, `uitoggle`이 단축키와 같은 동작을 한다. 그 밖에 `uitext`, `uifocus`, `uidrag`, `uiopen`·`uiclose <게임 화면>`, `uilist`, `uicanvas`, `uitarget`, `uisay`, `uiraise`, `uicollapse`(`Dev/DevCommandsUi.cs`).

게임 화면의 자주 쓰는 좌표: HUD의 날짜 버튼(달 넘기기) `(400, 1000)`, 확인 창의 예 `(884, 531)`·아니오 `(1037, 531)`, 월말 기록 창의 다음 `(959, 629)`, 가방 버튼 `(120, 860)`, 인물 화면 닫기 `(1722, 160)`, 게임 메뉴의 돌아가기 `(959, 391)`.

시험 중에 게임 메뉴(돌아가기·저장…)가 떠 있을 때가 있다. 게임 창이 포커스를 가졌다가 잃은 뒤로 추정한다. `hit`의 결과에 `UI/GameMemu`가 나오면 돌아가기를 눌러 닫는다.

### 조사 도구 (`tools/research/`)

- `dump-types.ps1 -Game <게임폴더> -Out types.txt` — 어셈블리의 형식·속성·메서드 목록을 만든다(`types.txt`는 git 제외, 현재 복사본이 들어 있다).
- `q.ps1 -Show <형식이름>` / `-Find <정규식>` — 그 목록에서 찾는다. 중첩 형식은 `DataProps/PropsData`처럼 `/`로 적는다.
- `methods.ps1 -TypeRegex <정규식> [-MethodRegex <정규식>]` — 생성자와 메서드의 매개변수를 본다(목록에는 생성자가 없다).
- `major-breakthrough-check.ps1 -Prev <경지ID> -Rows <방식 수> -Tag <이름>` — 대경지 재료 지급의 자동 검증(21절).
- 게임 설정 표의 원본은 `E:\Games\Tale of Immortal\Mod\modFQA\配置修改教程\配置（只读）Json格式\*.json`. 이름은 같은 폴더의 `LocalText.json`(중국어·영어, 한국어 열은 대부분 비어 있음).

## 6. 반복해서 겪은 함정

PowerShell과 도구:

- 변수 이름의 대소문자를 구분하지 않는다. `$T`(도구 경로)와 `$t`(본문 문자열)를 한 스크립트에 쓰면 덮어쓴다.
- `mod.ps1`·`rc.ps1`은 배열 매개변수가 있어 `pwsh -File`로 부르면 인수가 어긋난다. `&`로 부른다.
- 창 위치는 세이브 폴더 밖의 `config.txt`에 남는다. 위치를 바꾸는 시험은 시험 전의 파일을 떠 두고 되돌린다(`ui-check.ps1`의 `move` 계열이 그렇게 한다. 중간에 멈췄으면 `.devremoteeck-config.txt`를 손으로 되돌린다).
- 화면 밖의 게임에 잇달아 보낸 키는 빠질 때가 있다. 키 한 번의 전달은 믿을 수 있지만, 여러 번 누르는 판정은 개발 명령(`uitoggle`)으로 한다.
- 게임 화면을 닫는 명령 뒤에 고정 시간으로 기다리지 않는다. 닫히는 연출의 길이가 달라진다. 값이 바뀔 때까지 기다린다(`ui-check.ps1`의 `GameScreen`).
- 함수 이름이 별칭과 겹치면 별칭이 먼저 실행된다(`R`은 `Invoke-History`, `History`는 `Get-History`, `Erase`는 삭제 명령). 새 함수를 만들면 `Get-Alias <이름>`으로 확인한다.
- 명령 문자열에 삭제 명령의 이름이 들어 있으면 안전 검사가 명령 전체를 막는 경우가 있다(문서 문구로 들어 있어도 막혔다). 파일 삭제는 `[IO.File]::Delete`나 Bash의 `rm`을, 문서의 그런 문구는 편집 도구를 쓴다.
- Bash의 here-doc에 작은따옴표가 든 C# 코드를 넣으면 인용이 깨진다. 긴 코드는 Write 도구로 파일을 만들고 끼워 넣는다.
- git이 줄바꿈을 CRLF로 바꾼다. 여러 줄 치환을 하기 전에 `\r`을 지운다.
- `MelonLoader\Latest.log`의 한글은 UTF-8이 아니라 CP949다. 로그에서 한글 문구를 찾는 검사는 아무것도 잡지 못한다. 모드의 경고는 줄의 모양으로 고른다(`datebag-check.ps1`의 `ModWarnings`).
- 세이브 폴더 밖의 파일(`TlToybox\changes.log`, `bag-slots.txt` 등)은 세이브를 되돌려도 남는다. 이력 검사는 그 실행에서 새로 생긴 줄만 봐야 하고, `bag-slots.txt`는 회차 스크립트가 되돌린다.

게임:

- IL2CPP 프록시:
  - 게임이 알아보지 못하는 값으로 만든 객체는 속성을 읽는 것만으로 예외가 난다(공법의 `martialInfo`). try/catch로 감싸고, 깨진 것은 바로 지운다.
  - 버튼 콜백 안의 예외는 화면에 나오지 않고 `MelonLoader\Latest.log`의 "trampoline" 줄에만 남는다. 버튼이 아무 반응이 없으면 그 로그를 본다.
  - 게임 행동(`CreateAction`)의 반환값 0이 "효과가 났다"는 뜻이 아니다. 항상 데이터를 다시 읽어 확인한다.
- 게임 화면의 표시 조건은 데이터 위치와 다를 수 있다(운명 17절, 역천개명 18절). "넣었다"와 "게임 화면에 나온다"는 따로 확인한다.
- 편집기의 대상 식별: 저장값은 `unit.data.unitData.propertyData`, 화면 값은 `unit.data.dynUnitData`의 `DynInt.value`. 수명·나이는 개월로 저장되고 년으로 표시된다.
- 날짜: 플레이어의 행동은 `WorldUnitBase.AddResidueDay`로 날짜를 쓴다. `WorldRunMgr.AddDay`를 직접 부르거나 막으면 플레이어의 `residueDay`와 어긋나 달을 넘길 때 날짜가 튄다(25절). 달 넘기기(`isAutoRun` = `true`)를 중간에서 막으면 HUD가 어긋난 채 남는다. 예전 개발 명령 `day`(`Dev/DevCommandsStage4.cs`)는 아직 `AddDay`를 직접 부르므로 쓰지 않는다.
- 달이 넘어가는 동안(`g.world.run.isRunning`)에는 NPC 대화가 떠서 처리가 멈춰 있을 수 있다. 자동 시험은 고정 시간만큼 기다리지 말고 `isRunning`이 꺼질 때까지 기다린다(`datebag-check.ps1`의 `WaitRun`).
- 달력을 앞으로 옮기거나 날짜를 진행시키면 날짜 기준 사건(기연, NPC의 제안)이 뜬다. 시험이 예상하지 못한 창에 막히면 화면을 캡처해 무엇이 떠 있는지부터 본다.
- 게임을 닫은 직후에도 프로세스가 목록에 잠깐 남는다. 회차 스크립트가 기다린다.

## 7. 알려진 한계와 미확인 목록

기능별 한계는 05 문서 각 절 끝의 "확인하지 못한 것"에 있다. 그중 다음 작업에 영향이 큰 것:

| 항목 | 상태 | 기록 |
|---|---|---|
| 화신·오도(대경지 7·8)의 천재지보 | 지급은 되지만 게임의 `자동 투입`이 넣지 않는다. 직접 골라 넣는 것은 확인 못 함 | 21절 |
| 기술 수련·참오의 게임 행동 | `IsCreateAction`이 3(불가). 조건 미확인. 편집기는 숙련 경험치·옵션 편집으로 대신 | 22절 |
| 풍수서 강제 추가 | 낮은 단계와 높은 단계가 함께 남는다. 정상 진행에서의 모양 미확인 | 24절 |
| 기술을 수십 개 익힌 상태의 부작용 | 장착 화면·전투 미확인 | 24절 |
| 역천개명 | 기록의 `quality` 뜻 미확인, 실제 돌파로 생긴 기록을 바꾸는 경우 미확인, 계열 규칙(`mutexGroup`) 미검사, 비우기 없음 | 19절 |
| 선천운명 | 개수 상한과 다른 게임 기능과의 관계 미확인 | 17절 |
| 한글 입력기(IME)로 검색 칸 입력 | 미확인(개발 명령으로 값을 넣어 시험) | 14절 |
| 실제 마우스 휠·막대 끌기 | 미확인(도구의 스크롤 이벤트로 시험) | 14절 |
| 이동 불가 칸, 보호막·지속 피해 | 미확인 | 13절 |
| 날짜 고정 | 이동과 수련 밖의 행동(제작·채집 등)의 날짜 소모, 달 넘기기 단축키(`Z`) 미확인. 고정 중 HUD의 날짜 버튼은 막힌다 | 25절 |
| 달력 직접 지정 | 지도 화면 밖(마을·전투·대화 중)에서의 사용 미확인. 달력을 앞으로 옮기면 날짜 기준 사건이 바로 일어날 수 있다(2년 뒤로 옮기자 식혼검 사건 창이 뜸). 임무 기한·재사용 대기는 미확인 | 25절 |
| 가방 칸 | 수백 칸일 때의 가방 화면, 세이브가 여럿일 때 미확인 | 25절 |
| 모드를 뺀 뒤의 세이브 동작 | 미확인 | — |

묶음 A의 최종 검토에서 사소하다고 보고 고치지 않은 것(사용자에게 보고했고 지시는 없었다):

- `TimeService`가 HUD 갱신을 변경 이력 기록보다 먼저 한다. 갱신이 예외를 던지면 값은 썼는데 이력이 남지 않는다.
- 세이브에 들어온 직후 60프레임 안에 가방 칸을 바꾸면 이력에 거짓 "(재조회 불일치)"가 붙는다(핸들러가 아직 걸리지 않아서).
- `bag-slots.txt` 쓰기가 원자적이지 않다.
- Harmony 접두 함수(`ResiduePrefix`, `SkipPrefix`의 판정 부분)에 예외 방어가 없다.
- `ModWarnings`가 "trampoline" 예외 줄을 놓치고, 고정 중 "확인 창 없음" 검사의 대기가 짧다(300ms).

기획서(`00-design-brief.md`)에 있으나 아직 만들지 않은 것: NPC 생성·삭제, 문파 직위 변경, 그 밖의 샌드박스 기능. 범위는 기획서와 05 문서 1절을 대조해 정한다.

## 8. 다음에 할 일

### 8.1 정해진 순서

사용자가 2026-10-12에 추가 기능과 기존 기능 검토를 요청했고(아래 8.2), 이를 네 묶음으로 나눠 **A → B → C → D** 순서로 하기로 정했다.

| 묶음 | 내용 | 상태 |
|---|---|---|
| A | 날짜 고정·변경, 가방 칸 ±10 (요청 1, 2) | 끝남. 스펙·계획 `docs/superpowers/*/2026-10-12-date-bag*`, 기록 05 문서 25절 |
| B | 화면 구조 정리와 탭별 UI/UX 개선 (요청 3) | **진행 중.** 1차(검증 도구, 창의 틀, 탭 분리) 끝남, 05 문서 26절. 2차(공통 부품, 탭별 개선, 가방 칸 한 번에 지정, 변경 이력 전체 보기)는 계획 작성 전(8.3절) |
| C | 가방 내보내기·가져오기 (요청 4) | B 다음 |
| D | 캐릭터 내보내기·가져오기 (요청 5) | C 다음. C의 저장소와 화면을 재사용 |

요청 6(그 밖에 추가·개선할 기능 검토)은 따로 묶지 않았다. 각 묶음을 설계할 때 그 영역의 후보를 함께 제시하고, 어디에도 들어가지 않는 것은 마지막에 목록으로 낸다. B를 C·D보다 앞에 둔 이유는, C·D에 "목록 → 세부 → 개별 가져오기" 같은 여러 단계의 새 화면이 필요해 지금의 한 파일 구조에 얹으면 다시 손봐야 하기 때문이다.

### 8.2 사용자의 요청 (원문)

    추가기능 + 기존 기능 검토
    1) 날짜고정/변경 기능 추가
    2) 가방 크기  10칸씩 추가/삭제 변경 기능
    3) 기존 화면의 각 탭별 UI/UX 개선
    4) 현재 가방 export/import
     4-1) export 데이터는 json 외부 파일로 관리
     4-2) 인게임 화면에서는 export시 이름을 설정가능
     4-3) export 목록에서 선택적으로 해당 export의 아이템 전부를 인벤토리로 import 할수 있다
     4-4) export 목록에서 특정 export의 세부 목록을 볼수 있으며 세부 목록에서 아이템을 개별적으로 추가가능
    5) 현재 캐릭터 export/import -> 내용이 방대하니 현재 캐릭터의 각 능력치/경지/공법/기예 세부적으로
     5-1) export 데이터는 json 외부 파일로 관리
     5-2) 인게임 화면에서는 export시 이름을 설정가능
     5-3) export 목록에서 선택적으로 해당 export의  모든 정보를 일괄 import 할수 있다
     4-4) export 목록에서 특정 export의 세부 목록을 볼수 있으며 세부 목록에서 캐릭터의 각 능력치/경지/공법/기예 개별적으로 import가능
    6) 위내용 외 추가적으로 모드에 추가되어야 될 기능 및 개선되어야 될 기능 검토

5번 아래의 두 번째 "4-4"는 5-4의 오타로 보았다(사용자에게 그렇게 알렸고 정정은 없었다).

### 8.3 묶음 B — 화면 구조 정리와 탭별 UI/UX 개선

설계 스펙: `docs/superpowers/specs/2026-10-13-ui-rework-design.md`(사용자 승인, 2026-10-13).

구현 계획은 둘로 나눴다. 한 문서에 담기에는 코드가 너무 많고, 2차의 코드는 1차에서 만든 틀에 맞춰 써야 하기 때문이다.

- 1차: `docs/superpowers/plans/2026-10-13-ui-rework-1-shell.md` — 검증 도구(버튼을 이름으로 찾기), 창의 틀(앞뒤 순서·이동·접기·결과 영역·켜 둔 기능 표시), 탭을 파일별로 분리. 스펙 작업 순서의 1~2번. **끝났다**(05 문서 26절). 실행 방식은 사용자가 Native를 골랐다.
- 2차: 아직 쓰지 않았다. 공통 부품(목록·검색·상세·확인·숫자 칸), 탭의 하위 화면 쌓기(`EditorTab`의 `Push`/`Pop`), 탭별 개선, 가방 칸 한 번에 지정, 변경 이력 전체 보기. 스펙 작업 순서의 3~7번과 6절의 7~9, 11~14, 17, 18번. **다음 할 일은 `/superpowers:writing-plans`로 이 계획을 쓰는 것이다.**

사용자와 정한 것(2026-10-13, 스펙 2절):

- 고칠 영역은 넷 모두다: 검색과 목록, 수량·증감 입력, 창 자체, 결과·안내 문구.
- 창은 크기를 유지하고 제목 줄을 끌어 옮기며 `접기`로 제목 줄만 남긴다. 탭 9개는 그대로다.
- 함께 넣는 작은 기능은 가방 칸 한 번에 지정, 변경 이력 전체 보기다. 아이템·영석 줄이기는 요청 6번의 후보로 남겼다.
- 코드는 창의 틀 + 공통 부품(`UI/Widgets/`) + 탭별 클래스(`UI/Tabs/`)로 나눈다. 다시 그리기 방식은 유지하고 목록만 따로 다시 그린다.
- 목록은 스크롤로 통일하고 긴 목록에는 `처음`·`이전`·`다음`·`끝`을 함께 둔다(사용자가 설계 검토에서 고름).

사전 실험(회차 `uirework-r1`, 스펙 3절)으로 확인한 것: 편집기 캔버스를 게임과 같은 카메라 방식으로 바꾸면 게임 화면 사이에 끼울 수 있다. `EventTrigger`의 끌기가 걸린다. 입력 칸의 Enter를 `onEndEdit` 안에서 구분할 수 있다. 실험 명령은 `feat/ui-rework-research` 브랜치의 `Dev/DevCommandsUi.cs`에 있다(`develop`에 병합하지 않음. 구현 계획에서 필요한 명령을 새로 옮겨 적는다).

설계의 출발점이 된 문제(코드를 읽으며 본 것):

- 창 전체가 `UI/EditorWindow.cs` 한 파일(약 1,370줄)이고 정적 상태 변수가 50개를 넘는다. 버튼을 누를 때마다 화면 전체를 지우고 다시 만든다.
- 증감 단위(`step`) 하나를 능력치, 공법 숙련 경험치, 순간 이동, 호감도가 함께 쓴다. 한 탭에서 바꾸면 다른 탭도 바뀐다.
- 검색은 "검색" 버튼을 눌러야 하고 Enter가 듣지 않는다(`Ui.Input`이 값이 바뀔 때의 콜백만 건다).
- 창을 옮길 수 없고(60,60 고정), 게임의 창(돌파 결과, NPC 대화, 월말 기록)이 편집기 뒤에 뜬다.
- 목록의 방식이 제각각이다(NPC 8줄·기술 12줄의 쪽 넘김, 아이템·기예는 스크롤 표). 긴 글은 잘리고 전체를 볼 방법이 탭마다 다르다.
- 결과 메시지가 아래 한 줄뿐이라 긴 문구는 창 밖으로 나간다.
- 아이템과 영석은 주기만 하고 줄이지 못한다.
- 켜져 있는 기능(무적, 피해 배율, 날짜 고정)이 편집기를 닫으면 보이지 않는다.
- 묶음 A에서 넣은 줄은 임시 배치다. 세계 탭의 날짜 상자, 그리고 자원 탭의 가방 줄(둘째 줄 오른쪽, 그 자리에 있던 안내 문구를 뺐다).

B에서 지킬 것:

- 서비스(`Services/`)는 화면을 모른다. B는 `UI/`만 바꾸는 것이 기본이다.
- 화면 좌표가 바뀌면 `datebag-check.ps1`의 버튼 좌표와 이 문서 5절의 좌표 설명을 함께 고치고, `time`·`freeze`·`bag` 부분을 다시 돌린다. 스펙은 검증 스크립트가 버튼을 이름으로 찾도록 먼저 바꾸는 것을 작업 순서 1번에 두었다. 버튼의 이름(`Day1`, `Freeze`, `BagPlus`, `DateApply`, 입력 칸 `DateYear`·`DateMonth`·`DateDay`)은 유지한다.
- 사용자에게 직접 확인을 부탁할 것(도구로 시험할 수 없음): 실제 마우스로 제목 줄 끌기, 한글 입력기로 검색 칸에 입력.

### 8.4 묶음 C — 가방 내보내기·가져오기

형식 목록(`types.txt`)에서 확인한 것(게임에서는 아직 시험하지 않았다):

- 가방은 `unit.data.unitData.propData`(`DataUnit/UnitDataProps` → `DataProps`)의 `allProps`(`List<DataProps/PropsData>`).
- 아이템 하나(`DataProps/PropsData`)는 `soleID`, `propsID`, `propsCount`, `propsType`, `values`(정수 배열), 문자열 배열(`GetValuesStrArray()`, `GetValuesLocalTextArray()`)로 이뤄진다.
- 만들기는 `PropsData.New(propsID, propsCount, type, values)`, `NewProps(propsID, propsCount)`, `NewMartial(...)`, 넣기는 `DataProps.AddProps(PropsData)`가 있다. 지금 편집기의 지급은 `unit.data.RewardPropItem(propsID, count, false)`를 쓴다.
- 가방이 가득 차면 지급이 거부된다(시험 세이브에서는 표시가 용량보다 2 큰 상태에서 가득 찬다). 가방 칸은 묶음 A의 기능으로 늘릴 수 있다.

설계 전에 실험할 것: 비급·법보 같은 고유 아이템이 `values`만으로 똑같이 복원되는지, 영석(`propsID` 10001인 아이템)과 장착 중인 아이템을 어떻게 다룰지, JSON을 무엇으로 읽고 쓸지(`bin\Release`에는 DLL이 하나만 있어야 하므로 참조를 추가해도 복사하지 않거나 직접 쓴다).

사용자에게 물을 것: 용도(같은 세이브에서 되돌리기 / 다른 세이브나 새 인물로 옮기기 / 아이템 묶음 보관), 일괄 가져오기가 추가인지 교체인지, 파일을 둘 곳(`TlToybox\` 아래로 가정했다).

### 8.5 묶음 D — 캐릭터 내보내기·가져오기

- 범위는 요청대로 능력치·경지·공법·기예다. 운명(선천·후천·역천개명)과 자원은 요청에 없으므로 넣을지 물어야 한다.
- 능력치는 `AttributeCatalog`에 정의된 항목(다섯 분류), 공법은 `unitData.allActionMartial`과 장착 칸(`skillLeft`, `skillRight`, `ultimate`, `step`, `abilitys`), 기예는 `g.data.world.pillFormulas`와 `allArtistrySkillID`다(22~24절).
- 걸림돌은 경지다. 지금 편집기에는 "경지 ID를 직접 쓰지 않는다"는 규칙이 있다(05 문서 3절). `SetGradeID`는 시험에서만 썼고 능력치 보정이 따라오는지 확인하지 않았다. 가져오기에서 경지를 어떻게 다룰지 설계에서 정해야 한다.
- NPC를 대상으로 할지, 가져올 때 기존 것을 지울지 더할지도 물을 것이다.

### 8.6 진행 방식 (묶음 A에서 쓴 것)

1. `/superpowers:brainstorming`으로 시작한다. 질문은 한 번에 하나씩, 선택지로 묻는다. 사용자는 짧게 답한다("진행", 선택지 고르기).
2. 만드는 방법이 게임의 실제 동작에 달려 있으면 명세를 쓰기 전에 게임에서 실험한다(사용자가 그렇게 골랐다). 실험 코드는 `Dev\`에 두고 별도 브랜치에 커밋만 한다.
3. 명세(`docs/superpowers/specs/`) → 사용자 검토 → 계획(`plans/`) → 사용자 검토 → 실행. 실행 방식은 사용자가 "Native"(이 세션에서 직접 구현하고 마지막에 검토자 한 번)를 골랐다.
4. 단위 시험이 없으므로 Task마다 서비스를 부르는 개발 명령을 먼저 넣어 빌드 실패를 확인하고, 구현한 뒤 게임 안 검증 스크립트로 PASS/FAIL을 본다.
5. 끝나면 05 문서에 절을 추가하고 이 문서를 갱신한 뒤 `develop`에 병합하고 `tools/install.ps1`로 설치한다.

### 8.7 그 밖의 남은 일

- `feat/date-bag-research` 브랜치: 묶음 A의 사전 실험 명령이 들어 있고 `develop`에 병합하지 않았다. 내용은 `Dev/DevCommandsTime.cs`로 대체됐다. 지울지 사용자에게 물었고 답을 받지 못했다(2026-10-13에 다시 알렸다).
- `feat/ui-rework-research` 브랜치: 묶음 B의 사전 실험 명령. `develop`에 병합하지 않는다. 묶음 B가 끝나면 위 브랜치와 함께 지울지 묻는다.
- `.devremote`에 회차별 세이브 복사본(`save-backup-*`, `save-before-restore-*`)이 26개 쌓여 있고, 모드의 자동 백업(`TlToybox\backups\`)도 시험 회차마다 하나씩 늘었다(46개 이상). 정리할지는 정하지 않았다(2026-10-13에 다시 알렸다).
- 요청 6번(그 밖의 추가·개선 후보)으로 모아 둔 것: 아이템·영석 줄이기.
- 7절의 "고치지 않은 것" 다섯 가지.
- 사용자가 직접 써 보며 알려 오는 문제의 수정(지금까지의 작업 대부분이 그렇게 시작됐다).
- 예전부터 남은 미확인: 신법·심법·신통의 모두 배우기, 부적의 강제 추가, NPC에게 추가한 기술의 저장 후 유지.
- `main`으로의 첫 릴리스(`develop` → `main` 병합). 사용자가 요청할 때만 한다.

## 9. 작업 규칙 요약

- 응답은 한국어. 게임 UI는 한글화되어 있으므로 화면 문구도 한국어로 말한다.
- 브랜치: `develop`에서 `feat/*`·`fix/*`·`chore/*`·`docs/*`를 만들어 작업하고 `git merge --no-ff`로 `develop`에 병합한다. `main`·`develop`에 직접 커밋하지 않는다. 커밋 메시지 끝에 `Co-Authored-By` 줄을 붙인다.
- 게임 폴더는 읽기 전용이 원칙이다. 예외는 `ModExportData\Mod_sWP6UL\`(편집기 설치)와 `Mods\TlToyboxDevRemote.dll`(조작 도구)뿐이다.
- 세이브를 바꾸는 시험은 백업 → 시험 → 복원 → 일치 확인의 순서를 지킨다. `session.ps1`의 회차 안에서 한다.
- 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다. 임의로 끄지 않는다.
- 문서에는 어셈블리나 게임에서 실제로 확인한 이름만 적고, 확인하지 못한 것은 "미확인"으로 적는다.
- 게임에서 확인하지 않은 변경은 "미검증"이라고 밝히고, 사용자의 게임이 켜져 있어 설치하지 못했으면 그것도 알린다.
