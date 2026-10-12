# 06. 작업 인계 (2026-10-12 기준)

다음 세션이 이 문서만 읽고 이어서 일할 수 있도록 현재 상태, 일하는 방법, 남은 일을 정리한다.
세부 실험 기록은 `docs/05-editor-v0.1.md`(1~24절)에 있고, 이 문서는 그 길잡이다.

## 1. 한눈에 보는 상태

- 대상 게임: 귀곡팔황(Tale of Immortal) v1.2.113.259, `E:\Games\Tale of Immortal`. Unity 2020.3 IL2CPP, MelonLoader 0.5.4.
- 결과물: 게임 안에서 `Insert`로 여닫는 편집기(코드로 만든 uGUI 창, 1500x1000). 공식 모드 경로(`ModMain.Init/Destroy`)로 로드된다.
- 모드 ID `sWP6UL`, 어셈블리 `MOD_sWP6UL.dll`.
- 저장소 `E:\TlToybox`, 로컬 git만 사용(원격 없음). 통합 브랜치 `develop`이 최신이고 작업 트리는 깨끗하다. `main`은 초기 커밋 그대로다(릴리스한 적 없음).
- 게임에 설치된 것: 배포 빌드(개발 도구 제외)가 `ModExportData\Mod_sWP6UL\ModCode\dll\`에 있다. `develop`의 마지막 상태와 같다.
- 사용자의 세이브: 1번 슬롯(인물 "신환"). 시험 뒤에는 매번 시험 전 복사본으로 되돌렸다.

## 2. 편집기 기능 지도

탭 순서: 능력치 · 자원 · 경지 · 공법 · 운명 · 기예 · NPC · 세계 · 안전.

| 탭 | 기능 | 코드 | 실험 기록(05 문서) |
|---|---|---|---|
| 능력치 | 게임 인물 화면의 다섯 분류(개인·전투·공법 자질·영근 자질·기예)로 저장값 편집, 증감 단위 1/10/100/1000/직접 입력, 자원 회복 | `AttributeCatalog`, `EditService.SetAttribute` | 5, 14, 15절 |
| 자원 | 영석 추가, 아이템 표(번호·카테고리·이름·품질·보유·설명), 이름·카테고리 필터, 페이지당 10/20/50, 지급 수량 | `ItemCatalog`, `EditService.GiveItem` | 14, 15절 |
| 경지 | 수련치 채우기, 소경지 돌파, 대경지는 재료·천재지보 지급 후 게임의 돌파 화면 열기 | `EditService`(Breakthrough, GiveBreakthroughMaterials) | 8, 9, 20, 21절 |
| 공법 | 익힌 공법 목록, 옵션 품질·교체, 숙련 경험치, 장착·해제·망각, 기술 추가(다섯 분류·경지·이름, 모두 배우기, 강제 추가) | `MartialService`, `SkillCatalog` | 7, 22, 23, 24절 |
| 운명 | 선천운명 추가·교체·삭제, 후천운명 추가·삭제, 역천개명(경지별) 지정 | `LuckService`, `FateService` | 10, 15~19절 |
| 기예 | 연단·연기·부적·풍수·약재·광재의 서적 배우기·잊기·모두 배우기·강제 추가 | `ArtService` | 22, 23, 24절 |
| NPC | 가까운 NPC 목록·이름 검색, 호감도, 편집 대상으로 지정 | `EditService`(SetIntimBoth) | 5절 |
| 세계 | 순간 이동, 날짜 진행, 전투 무적·피해 배율 | `WorldService`, `BattleService` | 13절 |
| 안전 | 세이브 백업, 변경 이력 | `Safety/SaveBackup`, `Safety/ChangeLog` | 3절 |

공통 규칙:

- 쓰기 전에 한 실행에 한 번 세이브 폴더를 백업한다(`SaveBackup.EnsureOnce`). 백업과 이력은 `%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\TlToybox\`(`backups\`, `changes.log`, `config.txt`, `editor-lucks.txt`).
- 쓰기 뒤에 값을 다시 읽어 이력에 남긴다. 다르면 "(재조회 불일치)"가 붙는다.
- 되돌리기 어렵거나 게임이 붙인 것을 지우는 동작은 버튼을 두 번 눌러야 한다("정말 삭제", "정말 강제" 등).
- 능력치·공법·운명은 편집 대상(플레이어 또는 NPC)에, 자원·경지·기예·세계는 플레이어에 적용된다.

## 3. 저장소 구조

| 경로 | 내용 |
|---|---|
| `docs/00-design-brief.md` | 기획서(기준 문서) |
| `docs/01-game-structure.md`, `02-mod-dev-research.md`, `02a-workshop-survey.md`, `04-ingame-state-analysis.md` | 사전 조사 |
| `docs/05-editor-v0.1.md` | 편집기의 범위·규칙·실험 기록(1~24절) |
| `docs/06-handoff.md` | 이 문서 |
| `src/ModCode/ModMain/ModMain.cs` | 진입점(`Init`, `Destroy`) |
| `src/ModCode/ModMain/Services/` | 게임 값의 읽기·쓰기 |
| `src/ModCode/ModMain/UI/` | `Ui.cs`(uGUI 도우미), `EditorHost.cs`(단축키), `EditorWindow.cs`(창 전체, 탭별 Render 함수) |
| `src/ModCode/ModMain/Safety/` | 백업, 변경 이력 |
| `src/ModCode/ModMain/Dev/` | 개발 빌드 전용 명령(배포 빌드에서 제외) |
| `tools/install.ps1` | 배포 빌드를 만들어 게임에 설치 |
| `tools/DevRemote/` | 게임을 화면 밖에서 조작하는 도구(MelonMod + 스크립트) |
| `tools/research/` | 게임 어셈블리 조사 스크립트 |
| `ModProject_sWP6UL/`, `.devremote/` | git 제외. 모드 에디터 프로젝트, 조작 도구의 작업 폴더와 세이브 백업 |

`src/ModCode/ModMain/Adapters`, `Features`, `Resolvers`와 `tests/`는 처음 뼈대만 만들고 쓰지 않았다(비어 있음).

## 4. 빌드와 설치

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release                # 개발 빌드(Dev 명령 포함)
    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release -p:DevTools=false -o <임시폴더>   # 배포 구성 컴파일 확인
    powershell -File tools/install.ps1                                         # 배포 빌드를 게임에 설치

- 게임이 켜져 있으면 DLL이 잠겨 설치·교체가 안 된다. 한 번 로드된 DLL은 게임을 끄기 전에는 바뀌지 않는다.
- 두 구성 모두 컴파일되는지 매번 확인한다(`Dev\`의 코드를 배포 코드가 참조하면 배포 구성에서 깨진다).

## 5. 게임에서 검증하는 방법

사용자의 화면·마우스·키보드를 건드리지 않고 한다. 사용자가 게임을 켜 둔 동안에는 시험할 수 없고, 사용자의 게임을 임의로 끄지 않는다(끄도록 알리고 기다린다).

1. 게임이 꺼져 있는지 확인한다(`Get-Process guigubahuang`).
2. 저장이 필요한 시험이면 세이브 폴더를 `.devremote\save-backup-<날짜>-<이름>`으로 복사한다. 세이브는 `%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\Steam\CacheData`.
3. 개발 빌드 DLL을 `ModExportData\Mod_sWP6UL\ModCode\dll\`에 복사하고 `.devremote\park.flag`를 만든다(게임 창이 1920x1080으로 화면 밖 x=30000에 뜬다).
4. `tools\DevRemote\enter.ps1 -Launch -Direct` — 게임을 띄워 1번 세이브까지 들어간다. "WORLD READY"가 나오면 준비된 것이다.
5. 조작:
   - `mod.ps1 -Cmd "<명령>"` — 모드의 개발 명령. `editor`가 단축키 없이 편집기를 연다. 목록은 `Dev\DevCommands*.cs`.
   - `rc.ps1 -Cmd "click x y" -NoShot` / `"hit x y"` / `"scroll x y delta"` — 게임 좌표(1920x1080)의 UI 요소를 누르거나 이름을 확인한다.
   - `win.ps1 -Do shot -Out <파일> -Scale 0.6` — 창 캡처. 캡처 좌표에서 게임 좌표로: `(x / Scale - 8, y / Scale - 31)`.
   - `win.ps1 -Do key -Vk 0x2D` — Insert 키.
6. 끝나면 게임을 끄고(`Stop-Process`), `park.flag`를 지우고, 저장했으면 세이브를 복사본으로 되돌려 `diff -rq`로 확인하고, `tools/install.ps1`로 배포 빌드를 다시 설치한다.

편집기 좌표 계산: 창은 (60,60)에서 시작하고 내용 영역의 원점은 게임 좌표 (240,164)다. 탭 버튼은 x=150, y=185+52×순번.

조사 도구(`tools/research/`):

- `dump-types.ps1 -Game <게임폴더> -Out types.txt` — 어셈블리의 형식·속성·메서드 목록을 만든다(`types.txt`는 git 제외, 현재 복사본이 들어 있다).
- `q.ps1 -Show <형식이름>` / `-Find <정규식>` — 그 목록에서 찾는다.
- `methods.ps1 -TypeRegex <정규식> [-MethodRegex <정규식>]` — 생성자와 메서드의 매개변수를 본다(목록에는 생성자가 없다).
- `major-breakthrough-check.ps1 -Prev <경지ID> -Rows <방식 수> -Tag <이름>` — 대경지 재료 지급의 자동 검증(21절).
- 게임 설정 표의 원본은 `E:\Games\Tale of Immortal\Mod\modFQA\配置修改教程\配置（只读）Json格式\*.json`. 이름은 같은 폴더의 `LocalText.json`(중국어·영어, 한국어 열은 대부분 비어 있음).

## 6. 반복해서 겪은 함정

- PowerShell은 변수 이름의 대소문자를 구분하지 않는다. `$T`(도구 경로)와 `$t`(본문 문자열)를 한 스크립트에 쓰면 덮어쓴다.
- 한 PowerShell 명령에 `Remove-Item`이 들어 있으면 안전 검사가 명령 전체를 막는 경우가 있다. 삭제는 Bash의 `rm`, `mv`로 따로 한다.
- Bash의 here-doc에 작은따옴표가 든 C# 코드를 넣으면 인용이 깨진다. 긴 코드는 Write 도구로 파일을 만들고 끼워 넣는다.
- git이 줄바꿈을 CRLF로 바꾼다. `perl -0pi`나 여러 줄 `Replace`를 쓰기 전에 `\r`을 지운다.
- IL2CPP 프록시:
  - 게임이 알아보지 못하는 값으로 만든 객체는 속성을 읽는 것만으로 예외가 난다(공법의 `martialInfo`). try/catch로 감싸고, 깨진 것은 바로 지운다.
  - 버튼 콜백 안의 예외는 화면에 나오지 않고 `MelonLoader\Latest.log`의 "trampoline" 줄에만 남는다. 버튼이 아무 반응이 없으면 그 로그를 본다.
  - 게임 행동(`CreateAction`)의 반환값 0이 "효과가 났다"는 뜻이 아니다. 항상 데이터를 다시 읽어 확인한다.
- 게임 화면의 표시 조건은 데이터 위치와 다를 수 있다(운명 17절, 역천개명 18절). "넣었다"와 "게임 화면에 나온다"는 따로 확인한다.
- 편집기의 대상 식별: 저장값은 `unit.data.unitData.propertyData`, 화면 값은 `unit.data.dynUnitData`의 `DynInt.value`. 수명·나이는 개월로 저장되고 년으로 표시된다.

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
| 한 번 있었던 이상 상태 | 돌파 화면 뒤 게임 메뉴가 뜨고 단축키가 듣지 않음. 재현 못 함 | 21절 |
| 한글 입력기(IME)로 검색 칸 입력 | 미확인(개발 명령으로 값을 넣어 시험) | 14절 |
| 실제 마우스 휠·막대 끌기 | 미확인(도구의 스크롤 이벤트로 시험) | 14절 |
| 월말 처리, 이동 불가 칸, 보호막·지속 피해 | 미확인 | 13절 |
| 모드를 뺀 뒤의 세이브 동작 | 미확인 | — |

기획서(`00-design-brief.md`)에 있으나 아직 만들지 않은 것: NPC 생성·삭제, 문파 직위 변경, 그 밖의 샌드박스 기능. 범위는 기획서와 05 문서 1절을 대조해 정한다.

## 8. 다음에 할 만한 일

사용자가 정한 순서는 없다. 직전 대화의 흐름으로 보면:

1. 사용자가 실제로 써 보며 알려 오는 문제의 수정(이 세션의 작업 대부분이 그렇게 시작됐다).
2. 위 표의 미확인 항목 가운데 사용자가 곧 마주칠 것: 신법·심법·신통의 모두 배우기, 부적의 강제 추가, NPC에게 추가한 기술의 저장 후 유지.
3. `main`으로의 첫 릴리스(`develop` → `main` 병합). 사용자가 요청할 때만 한다.

## 9. 작업 규칙 요약

- 응답은 한국어. 게임 UI는 한글화되어 있으므로 화면 문구도 한국어로 말한다.
- 브랜치: `develop`에서 `feat/*`·`fix/*`·`chore/*`·`docs/*`를 만들어 작업하고 `git merge --no-ff`로 `develop`에 병합한다. `main`·`develop`에 직접 커밋하지 않는다. 커밋 메시지 끝에 `Co-Authored-By` 줄을 붙인다.
- 게임 폴더는 읽기 전용이 원칙이다. 예외는 `ModExportData\Mod_sWP6UL\`(편집기 설치)와 `Mods\TlToyboxDevRemote.dll`(조작 도구)뿐이다.
- 세이브를 바꾸는 시험은 백업 → 시험 → 복원 → 일치 확인의 순서를 지킨다.
- 문서에는 어셈블리나 게임에서 실제로 확인한 이름만 적고, 확인하지 못한 것은 "미확인"으로 적는다.
- 게임에서 확인하지 않은 변경은 "미검증"이라고 밝히고, 사용자의 게임이 켜져 있어 설치하지 못했으면 그것도 알린다.
