# 06. 작업 인계 (2026-10-29 기준)

다음 세션이 이 문서만 읽고 이어서 일할 수 있도록 현재 상태, 일하는 방법, 남은 일을 정리한다.
세부 실험 기록은 `docs/05-editor-v0.1.md`(1~87절)에 있고, 이 문서는 그 길잡이다.

**이어받는 순서**: 아래 "지금 한 줄 요약"을 읽고 → 8.0절(다음에 할 일)을 읽고 → 일을 시작할 때 5절(검증 방법)과 6절(함정)을 본다. 1절의 날짜별 줄은 필요할 때만.

**지금 한 줄 요약(2026-10-29)**:
- **세 번째 릴리스(2026-10-29)**: 사용자 지시("전 작업 문서 정리 - 인계문서 작성 - develop 커밋 - main 로컬 머지 - 브랜치 정리")로 이 문서 정리까지를 `develop`에 병합하고 `develop`을 `main`에 `--no-ff`로 병합했다. 이제 `main` = `develop` = 설치본(f2d064a)과 같은 코드다(설치본 뒤의 변경은 문서뿐). 병합을 마친 `feat/trainer4-battle`·`docs/install-1028b`·`docs/release-1029`는 지웠다(8.7절). 실험 브랜치는 남겼다.
- **2026-10-28 트레이너 4차 — 전투 묶음(05 문서 87절, f2d064a, 2026-10-28 19:22에 설치)**: 사용자 지시 "트레이너 기능 추가"(질문 없이 "진행")로 12 문서 5절 가운데 셋을 골랐다. 배율 탭의 기술 쿨타임이 선법·영역 기술까지(명우 기술은 원래 걸려 있었음), 새 줄 전투 아이템 쿨타임(단약 칸·법보 발동)과 법보 내구 소모(없음), 주는 피해의 `일격` 단계. 전투 이동 속도는 시간·속도 칩으로 옮겼다(전투 칩 8줄). 검사 `trainer4-check.ps1` 다섯 부분과 기존 배율 검사 모두 FAIL 0.
- **2026-10-27에 끝낸 것(셋, 모두 develop에 병합, 2026-10-28 06:58에 설치)**: 모드를 뺀 뒤의 세이브 동작 확인(05 문서 84절, `session.ps1 -NoEditor`), 미룬 사소한 것 묶음(85절), 배율 탭 확장 — 분류 칩 다섯과 지도 속도·전투 이동 속도·호감 변화·정력 소모(86절).
- **두 번째 릴리스(2026-10-28)**: 사용자 지시로 `develop`을 `main`에 `--no-ff`로 병합했다(그때 설치본 0402190).
- 남은 일은 8.0절: 사용자 확인(트레이너 4차를 실제 플레이에서, 전투 이동 속도의 실제 걷는 빠르기, 배율 탭 칩의 쓰임새, 일격·선법 연사의 체감 등), 12 문서 5절의 남은 트레이너 기능 열셋(사용자가 원할 때).
- **첫 릴리스**: 2026-10-24에 `develop`을 `main`에 `--no-ff`로 병합했다(사용자 지시 "develop to main", dd8be98). 그 전까지 `main`은 초기 커밋 그대로였다.
- 게임 설치본은 f2d064a(2026-10-28 19:22. 1절 "지금 게임에 설치된 것").
- 2026-10-23에 끝낸 것: 12 문서의 묶음 1~5(05 문서 77~81절), 묶음 6 트레이너 3차 (가)(82절), 수련 단계 대도까지 채우기(83절), 사용자 보고 둘(공법 탭 옵션 11줄 표시 83.6절, 모드 기술 이름의 색 태그 83.7절). 12 문서 7절의 계획 1~6이 2026-10-27로 모두 끝났다.
- 일하는 방식(2026-10-23에 굳어진 것): 묶음마다 brainstorming → 설계 전 실험(실험 브랜치 `feat/*-research`, 회차) → 스펙(`docs/superpowers/specs/`) → 검토자 서브에이전트의 질문 → 계획(`docs/superpowers/plans/`) → 이 세션에서 직접 실행(executing-plans, 진행 기록은 `.superpowers/sdd/<계획>/progress.md`, 끝나면 지움) → 브랜치 전체를 opus 검토자 한 번 → Critical·Important만 시험부터 고치고 문서 → 사용자 확인 뒤 `--no-ff` 병합과 브랜치 삭제. 설치 기록은 `docs/install-*` 문서 브랜치로 남긴다. 사용자 보고 같은 작은 고침은 `fix/*` 브랜치에서 검사 줄을 먼저 더하고 고친다.
- 사용자에게 보이는 글은 모두 한국어로. 2026-10-27 세션에서도 네 번 영어로 샜다(영어 검사 출력이나 서브에이전트 보고를 읽은 직후의 한 줄 진행 문장). 도구 호출에 붙이는 한 줄까지 보내기 전에 본다.
- 이 컴퓨터 환경: Node.js 24.22.1을 nvm으로 설치했지만 시스템 PATH 맨 앞에 옛 `nvm\v24.12.0`이 있어 옛 버전이 돈다(관리자 권한 필요, 사용자 몫). 고친 뒤 `nvm uninstall 24.12.0`.

## 1. 한눈에 보는 상태

- 대상 게임: 귀곡팔황(Tale of Immortal) v1.2.113.259, `E:\Games\Tale of Immortal`. Unity 2020.3 IL2CPP, MelonLoader 0.5.4.
- 결과물: 게임 안에서 `Insert`로 여닫는 편집기(코드로 만든 uGUI 창, 1500x1000). 공식 모드 경로(`ModMain.Init/Destroy`)로 로드된다. 모드 ID `sWP6UL`, 어셈블리 `MOD_sWP6UL.dll`.
- 저장소 `E:\TlToybox`, 로컬 git만 사용(원격 없음). 통합 브랜치 `develop`이 최신이다. `main`은 2026-10-24에 `develop`을 처음 병합했고(첫 릴리스), 2026-10-28에 두 번째, 2026-10-29에 세 번째로 병합했다(요약 참고). 브랜치는 `main`, `develop`, 병합하지 않는 실험 브랜치 스물(`feat/*-research`. 목록은 8.7절)뿐이다(2026-10-21 정리, 2026-10-22에 `feat/perf-research`·`feat/mastery-research`, 2026-10-23에 `feat/trainer3-research`·`feat/mastery2-research`, 2026-10-27에 `feat/rate2-research`, 2026-10-28에 `feat/trainer4-research` 추가. 8.7절). 메인 체크아웃 `E:\TlToybox`는 `develop`에 있고 워크트리는 없다. 작업은 `develop`에서 딴 브랜치로 하고, 다른 세션과 겹치면 `.claude\worktrees\<이름>`에 워크트리를 만든다. 병합은 `develop`을 체크아웃해 `--no-ff`로 한다. 병합을 마친 작업·문서 브랜치는 사용자 지시로 지운다.
- **지금 게임에 설치된 것**(2026-10-30 21:34 기준. 앞 설치본의 기록은 8.8절):
  - **2026-10-30 21:34 갱신(90~93절)**: 편집기 DLL을 `develop` bb122e1로 바꿨다 — 사용자 지시("설치")·게임 꺼짐 확인 뒤 `tools/install.ps1`, `installed build: bb122e1`(3,035,136바이트). 바로 앞 설치본은 dd355aa(10:56, 아래 줄). 들어간 것: 13 문서 7절의 묶음 2·4·5 — 배율 서비스 다듬기(90절), 파일 안전과 예외 경로(92절: 번역표·설정·학습 상한·즐겨찾기 파일을 임시 파일 뒤 바꿔치기로 씀, 예외 경로 로그), 리팩토링 2차(93절). 모두 동작 불변이 목표라 사용자가 보기에 달라지는 것은 없어야 한다. 설치 뒤 배포 구성 회차 `rf2-j-1030`(`-ReleaseBuild`, 고정본)에서 `Init build=bb122e1`로 뜨는 것만 확인했다(회차는 설치본을 보관했다가 되돌리므로 설치본 그대로).
  - **2026-10-30 10:56 갱신(89절)**: 편집기 DLL을 `develop` dd355aa로 바꿨다 — 사용자 지시("설치")·게임 꺼짐 확인 뒤 `tools/install.ps1`, `installed build: dd355aa`(3,031,040바이트). 바로 앞 설치본은 f2d064a(2026-10-28 19:22, 아래 줄). 들어간 것: 작은 수정 2차(89절: 자원 탭 보유 칸 규칙 — 같은 값·떠나며 줄이기 무시, 줄이기는 Enter + 같은 값 Enter; 세이브를 떠날 때 성공·드롭 상태와 하위 화면 닫기; 돌아가기가 결과 유지; 기술 추가 안내 두 줄; 법보 `경지 10까지` 두 번 누르기; 비급 수량 칸 떠날 때 저장; 일격의 동료 제외; 문구 넷). 확인은 회차(고정본)에서만 했다. 사용자가 봐 줄 것은 89.3절(8.0절 0번).
  - **2026-10-28 19:22 갱신(87절)**: 편집기 DLL을 `develop` f2d064a로 바꿨다 — 사용자 지시("병합하고 설치")·게임 꺼짐 확인 뒤 `tools/install.ps1`, `installed build: f2d064a`(3,028,480바이트). 바로 앞 설치본은 0402190(2026-10-28 06:58, 아래 줄). 들어간 것: 트레이너 4차 전투 묶음(선법·영역까지 넓힌 기술 쿨타임, 전투 아이템 쿨타임, 주는 피해 일격, 법보 내구 소모 없음, 전투 이동 속도의 칩 이동). 확인은 회차(고정본)에서만 했다.
  - 조작 도구: `Mods\TlToyboxDevRemote.dll`(개발용. 회차와 검사가 쓴다. 지우면 원상태).
  - 창작마당 모드 40개: `ModExportData\<창작마당ID>\`. 목록 `tools/workshop/mods.txt`(설치 40·보류 4·제외 1), 이력 `history.txt`, 등록부 `docs/09-workshop-mods.md`, Steam 컬렉션 3820620999(40개, 설명 `collection-desc`). 마지막에 넣은 것은 만고신화 2814696167(`dH6k0J`, 71절. 새 게임용으로 설치한 채 둔다, 73절). 만고신화는 경지 표(`RoleGrade`)를 바꿔, 바닐라 세이브의 경지가 한 단계 낮게 읽힌다(72절).
  - 게임의 모드 순서(레지스트리 PlayerPrefs `HKCU\Software\guigugame\guigubahuang`의 `Mod_LoadData`): 41개(편집기 + 창작마당 40) 모두 `isLoad:true`. 만고신화를 41번째로 켰다(앞 값 `.devremote\playerprefs-Mod_LoadData-before-wangu-1020.bin`). **새 창작마당 모드를 설치하면 모드 창에서 저장하거나 이 값을 다시 써야 Fatury가 그 모드의 그림을 싣는다**(52절). 동적 입화 설정 `GameSetting_dynPortraitEnabled2_h171193457`=1(74절에서 잠시 0으로 시험하고 되돌림).
  - 모드 데이터 폴더 `%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\TlToybox`: 설정·즐겨찾기·프리셋·자동 백업과 번역 파일 셋 `translations\mods.txt`(LocalText 키별, 16,184행. 43·46절)·`texts.txt`(화면 글 원문, 245줄. 54절)·`tables.txt`(표 칸 원문, 3,271줄. 74절). DLL 안의 것이 첫 실행 때 풀리고, 파일이 이미 있으면 내장본에만 있는 줄이 끝에 덧붙는다. 사용자가 고친 줄은 그대로. 한글화와 패치는 모두 토이박스가 실행 중에 메모리의 표에 하고, 각 모드 폴더의 파일은 고치지 않는다.
  - 사용자는 설치본으로 직접 플레이한다. 2026-10-16 이후의 사용자 확인을 기다리는 것은 8.5절과 8.0절.
- **직전 작업(2026-10-16, 하루에 다섯 단계)**: G2a 창작마당 모드 받기·설치(05 문서 41절, `tools/workshop.ps1`. 스펙 없이 도구 스크립트로) → G2b·G2c 설계 전 실험(8.3e절, `ws-1016a`~`g`) → G2b 모드 공법의 비급(42절, 스펙·계획 `2026-10-16-mod-skill-books*`, `ws-1016h`~`m`) → G2c 모드 글 한글화(43절, 스펙·계획 `2026-10-16-mod-korean*`, 번역 59행, `ws-1016n`~`r`) → 모드 신통 비급(44절, 사용자 요청 "다시 확인": 옵션 번호 치환으로 42절의 차단을 풂, 브랜치 `fix/mod-ultimate-books`, `ws-1016s`~`w`). 2026-10-15에는 G1(38절), 비급 지급(39절), 도호 관리(40절). → 2026-10-16 오후: W 창작마당 확장(45절. 스펙·계획 `2026-10-16-workshop-expand*`. 목록 일곱 칸·이력, `-Search` 후보표(80개에 요약·추천), `-Doc` 등록부, 사용자가 고른 8개 받기·회차 `ws-1017a` 검증·설치, Steam 컬렉션). 리뷰 R은 별도 세션(`docs/08-review-2026-10-16.md`, 브랜치 `docs-review-1016`, 병합 전). → 같은 날 K 새 모드 8개 한글화(46절. 스펙 `2026-10-16-mod-korean-2-design.md`, 번역 765행 추가로 824행, `EnsureFile` 덧붙이기, 회차 `ws-1017b`(RED)·`ws-1017c`(GREEN)). → 같은 날 X 리뷰 수정 묶음(47·48절. 스펙 `2026-10-16-review-fixes-design.md`, 계획 `plans/2026-10-16-review-fixes.md`, 회차 `ws-1017e`(RED)·`f`·`g`(GREEN)·`h`~`k`(최종)·`l`~`n`(리뷰 후속). 탭 줄 재배치(화폐 그룹·안전 단추·고정 번호)와 작은 수정 열 가지). → 같은 날 Y 리뷰 남은 도구 묶음(49절. 스펙 `2026-10-16-review-tools-design.md`, 회차 `ws-1017p`·`q`). → 같은 날 밤~17일 새벽: 창작마당 3차(50절. 브랜치 `feat/workshop-batch3`, 회차 `ws-1017r`·`s`).
- **2026-10-17의 작업(시간순. 모두 develop에 병합, 6c12d31에 설치됨)**:
  - 오전 — 창 위의 휠·포인터가 뒤의 지도에 새는 버그 수정(51절, 브랜치 `fix/editor-input-leak`, 회차 `fix-1017a`~`e`). 게임 메서드의 네이티브 코드 해독(`xrefmap`·`xrefl`·`xrefd`)으로 `MapWorldMgr.OnUpdate`가 UI 조건 없이 입력을 읽는 것을 찾아 `UI/PointerGuard.cs`(Harmony 패치 넷 + 카메라 eventMask)로 막았다. 검사는 가상 입력(`ui-check.ps1 -Part input`). 실제 마우스는 사용자 확인(8.5절).
  - 오전 — NPC 몸(입화)이 비어 보이는 보고의 조사(52절, 회차 `body-1017a`~`i`·`create-1017a`~`f`·`rel-1017a`). Fatury 틀은 PlayerPrefs `Mod_LoadData`의 모드만 싣는다 → 사용자 승인으로 35개 모두 켬(위). 2815562168의 `Body\149`는 깨진 PNG. 도구 `fatury-scan.py`·`fatury-log.py`, 개발 명령 `rdress`·`rbodyq`·`rbodyr`·`rnpcinfo`·`ruigrid`, `session.ps1 -Login`.
  - 낮 — 남은 중국어 글 조사(53절, 회차 `zh-1017a`, 개발 명령 `rzh`): LocalText 밖의 글 네 층(표 칸 129, 종족 JSON 30, 종족 틀 DLL 라벨 25, 다른 DLL 버튼 8).
  - 오후 — LocalText 밖 중국어 글 한글화(54절, 스펙·계획 `2026-10-17-zh-text-korean*`, 회차 `zh-1017b`~`h`): 원문 기반 번역표 `Translations/texts.txt`(245줄 → 데이터 폴더 `translations\texts.txt`), 시작 때 여섯 표의 글 칸 131개 바꿔 끼움(`TextTranslationService`), `TMP_Text.text`·`Text.text` 설정자의 Harmony 앞붙임(`TextPatch`. 한자가 남으면 원문 그대로), LocalText ch→kr 전체 일치 사전. 캐릭터 만들기 화면의 종족 기술·변신 설명이 한국어(캡처 `.devremote\zh-1017g-peacock-transf.png`). 개발 명령 `rzhtext`·`rzhstat`·`rzhleft`·`rzhapply`.
  - 늦은 오후 — 사용자 보고 셋(55절, 회차 `fu-1017a`·`b`): 즐겨찾기 수량 칸은 칸을 떠나도 저장(`NumberField.commitOnEndEdit`), 전투 HUD의 신기 칸은 오른쪽 대신 **위로** 60씩(종족 틀의 기술 단추와 겹침), 게임이 못 만드는 기본 기술 셋(멸신참·마인참·극풍인)은 기술 추가 목록에서 뺌. `check-lib.ps1`의 경고 판정에 `texts:` 줄 제외. 전투 불러오기가 한 번 멈췄다(원인 미확인, 55절 끝).
  - 저녁 — 법보 탭(56절, 스펙·계획 `2026-10-17-treasure-tab*`, 회차 `tr-1017a`·`b`): 탭 16에 가진 법보(장착·해제·기령 빼기), 기령(재능 켜기·레벨 올리기·장착한 빈 법보에 넣기·지우기), 하위 화면 기령 만들기(32종)·법보 지급(열 종). `TreasureService`, 개발 명령 `tr`·`tr*`, 검사 `treasure-check.ps1 -Part service|ui` FAIL 0(캡처 `.devremote\check-treasure.png`).
- **2026-10-18의 작업**: 브랜치 정리(8.7절) → 창작마당 4차(57절, 브랜치 `feat/workshop-batch4`, 회차 `ws-1018a`·`b`): 설명 본문의 선택·권장 모드를 찾아 DLL까지 검토, 다섯 개 설치·글 243행 한글화, 보류 둘(세이브 자동 복구 2831402551: 월드 진입마다 세이브를 지움 / NPC AI 2991543623: 플레이어 공법 자동 성장 60%). Steam 컬렉션 3820620999에 다섯 개를 더해 39개로, 설명도 39개 기준으로 바꿨다(사용자가 내장 브라우저에서 다시 로그인한 뒤. 57절).
- **2026-10-18 오후의 작업(모두 develop에 병합, 게임 설치 전)**: 명령 채널 경합 셋 수정(59절), 법보 수리·기술 학습 상한(60절, 회차 `cr-1018a`~`e`), 기예 제작 재료 지급(61절 실험·62절, 스펙·계획 `2026-10-18-craft-materials*`, 회차 `mat-1018a`~`f`, 단위 시험 `CraftPlan` 13, 검사 `materials` 12, 최종 리뷰의 Important 둘 고침). 16:50에 65fa68c로 설치함. 리뷰가 남긴 사소한 것은 62절 끝과 커밋 3e05f0d 메시지.
- **2026-10-19**: 배율 탭에 수련·학습의 날짜 소모(63절, 2026-10-19 15:53 설치). 비급 학습은 진도가 쌓이는 구조(`World.allMartialStudy`, 필요량 수백)라 날짜를 줄여도 진도는 그대로다. 참오·망각은 게임이 날짜를 쓰지 않는다. 이어서 비급 학습 진도 배율(64절, x1~x100. x100이면 대개 한 번에 배움)도 더했다.
- **2026-10-20의 작업(모두 develop에 병합, 20:07에 134197d로 설치)**: 성격·아이템 소모·가방 수량·옵션 자리 강제·성공·드롭 탭(65~69절) → 기본 프리셋 11~16(70절) → 만고신화 검토·설치·한글화 9,666행(71·72절) → 역천개명 대체 번호 `alt`와 21번 만고검결 프리셋, `session.ps1 -Exclude`(73절) → 캐릭터 만들기 의복 빈 썸네일 채우기(`DressThumbService`)와 만고신화 하위 폴더 표 원문 3,271개 한글화(`Translations/tables.txt`, 74절).
- **2026-10-21 아침의 작업**: 의복 목록이 299번에서 끝나는 까닭 조사(75절, 회차 `wg-1021a`). 뒤의 57줄(dressID 10044~10100)은 [执余] 2956998877이 그림 없이 `active=0`으로 꺼 둔 빈 자리라 고를 새 의상은 없다. e6902ea로 설치.
- **2026-10-22의 작업**: 문서 정리(README와 이 문서 2·3·5절을 지금 구조에 맞춤) → 코드 리뷰·성능 측정과 개선 계획(`docs/12-review-2026-10-22.md`, 회차 `perf-1022a`~`c`). 자원 탭 첫 열기 4.0초 = 기술 카탈로그 3.4초(그 가운데 LocalText ch 사전 2.6초) + 아이템 카탈로그 0.55초. `Init` 5.5~5.7초. 모드 공법 563개 가운데 비급 실패 522개(이름·설명 없는 행 459, 옵션 행 5~6개 68, 옵션 행 0개 신법 6. 이름 없음 514개 가운데 52개는 판정만 고치면 이름이 나옴). 트레이너 미반영 24개. 배포 코드와 설치본은 그대로다. 측정 명령 `rperf`는 실험 브랜치 `feat/perf-research`에만 있다. → 같은 날 기술 수련 단계 조사(05 문서 76절, 회차 `ms-1022a`~`c`, 실험 브랜치 `feat/mastery-research`): 기술마다 보이는 단계 수는 옵션 수 + 1이고, 옵션을 규칙 안에서 11개로 채워 다시 만들면 입문~대도가 모두 생긴다(게임 기술 창에서 확인). 옵션 행이 없는 신법 어술·모드 신법 6·이름 없는 모드 심법 435는 안 된다. 사용자가 정한 순서: 12 문서 7절 제안대로(1→2→3→4→5→6), `rperf`는 `develop`의 개발 명령으로 옮김, 성능 1 묶음 스펙부터.
- **2026-10-23의 작업**: 성능 1 묶음(05 문서 77절, 스펙·계획 `2026-10-22-perf-first-open-design.md`·`2026-10-23-perf-first-open.md`, 브랜치 `feat/perf-first-open`, 회차 `perf1-1023a`~`i`). 자원 탭 첫 열기 3.5초 → 0.4~0.6초, `Init` 5.5초 → 3.7초(주 화면 복귀 뒤 3.7초 → 2.5초). 목록·번역 결과는 그대로(덤프 비교). `rperf`는 이제 `develop`의 개발 명령이다. 게임 설치는 하지 않았다.
- **2026-10-23 묶음 2**: 모드 공법 목록 정리(05 문서 78절, 브랜치 `feat/mod-skill-list`, 회차 `msl-1023a`~`j`). 남는 옵션 칸을 모드 번호로 되풀이해 채워 비급이 만들어지는 모드 공법 37 → 122, 이름·설명 없는 459와 비급을 못 만드는 신법 6은 목록에서 숨김(프리셋·즐겨찾기는 그대로), 51개가 이름을 얻음. 게임 설치는 하지 않았다.
- **2026-10-23 묶음 3**: 성능 2(05 문서 79절, 브랜치 `feat/perf-other-tabs`, 회차 `po-1023a`~`q`). 프리셋 탭 다시 열기 0.34 → 0.08~0.11초(폴더 표지와 그리기 한 번 동안의 게임 답 기억), 기예 0.18 → 0.04초, 도호 0.09 → 0.06초. 자원 탭 검색과 `Init`(LocalText 훑기 2.3초는 행마다 객체 꺼내기 비용이라 줄일 수 없음)은 측정만. 게임 설치는 하지 않았다.
- **2026-10-23 묶음 4**: 작은 수정(05 문서 80절, 브랜치 `fix/small-fixes-1023`, 회차 `sf-1023a`~`c`). 비급 지급 2초 시간 상한, 전투 중 아이템 소모 막기의 null 검사(A1. 답은 전과 같고 로그만 사라짐 — 80절), 확인값·통계 초기화(A2·N3), 첫 예외 로그(A4), 매 프레임 배열 재사용(F1). 게임 설치는 하지 않았다.
- **2026-10-23 묶음 5**: 리팩토링 1차(05 문서 81절, 브랜치 `chore/refactor-1023`, 회차 `rf-1023a`~`f`). 결과 집계(`ApplyTally`), `config.txt`(`ConfigFile`), 즐겨찾기 줄(`FavoriteStore.Lines`), 글 정리 도우미, 탭 배열. 동작은 그대로(같은 키 여러 줄은 뒤의 줄). `base` 검사가 묶음 1부터 깨져 있던 것을 고침. 게임 설치는 하지 않았다.
- **2026-10-23 묶음 6 (가)**: 트레이너 3차(05 문서 82절, 스펙·계획 `2026-10-23-trainer3*`, 브랜치 `feat/trainer3`, 실험 `feat/trainer3-research`·회차 `t3-1023a`~`h`, 검증 회차 `tr3-1023a`~`q`). 법보 경지 올리기·단계 1~6(한계돌파)과 지급의 경지·단계, 편집기 지급의 비율 0 결함 고침(지급 법보가 체력 1이던 것), 기령 친밀도 최대, 공법 탭 전승 화면, 성공·드롭 탭 참오 결과 최대. 게임 설치는 하지 않았다.
- **2026-10-23 저녁**: 704a567 설치(1절) 뒤 수련 단계 대도까지 채우기(05 문서 83절, 스펙·계획 `2026-10-23-mastery-fill*`, 브랜치 `feat/mastery-fill`, 실험 `feat/mastery2-research`·회차 `ms2-1023a`, 검증 회차 `mf-1023a`~`k`, 21:11에 9c144ef로 설치). 제자리 끼워 넣기(`PropsData.SetValues`)가 고유 번호·장착·숙련·전승을 지키고, 다시 만들기도 옮겨 준다. 장착 심법은 수도 점수가 모자라면 올린다.
- **2026-10-24**: 문서 정리(이 문서 머리·8.0절, README), `develop`을 `main`에 병합(첫 릴리스), 브랜치 정리(병합된 작업 브랜치는 없음. 실험 브랜치 열여덟은 남김, 8.7절).
- **2026-10-27 (3)**: 배율 탭 확장(05 문서 86절, 브랜치 `feat/rate-tab-expand`, 실험 `feat/rate2-research`·회차 `r2-a`~`f-1027`, 검증 `rx-a`~`p-1027`). 게임 설치는 하지 않았다.
- **2026-10-27 (2)**: 미룬 사소한 것 묶음(05 문서 85절, 브랜치 `fix/deferred-1027`, 회차 `dfx-0`·`a`~`g-1027`). 게임 설치는 하지 않았다.
- **2026-10-27**: 모드를 뺀 뒤의 세이브 동작 확인(05 문서 84절, 브랜치 `feat/session-noeditor`, 회차 `noed-a-1027`~`d`). `session.ps1 -NoEditor`. 게임 설치는 하지 않았다(도구와 문서뿐).
- **2026-10-28**: 0402190 설치(1절), 문서 정리와 두 번째 릴리스(`develop` → `main`), 브랜치 정리(병합된 작업 브랜치는 없음. 실험 브랜치 열아홉은 남김, 8.7절).
- **2026-10-28 (2)**: 트레이너 4차 전투 묶음(05 문서 87절, 스펙·계획 `2026-10-28-trainer4-battle*`, 브랜치 `feat/trainer4-battle`, 실험 `feat/trainer4-research`·회차 `t4-a`~`d-1028`, 검증 `t4-i`~`q-1028`). 병합·설치 전.
- **2026-10-29 (2)**: 리뷰와 미확인 항목 확인(13 문서 `docs/13-review-2026-10-29.md`, 05 문서 88절, 브랜치 `chore/review-1029`, 회차 `rv-a-1029`). 8.0절 3번 가운데 전투 장면 적재 중 지도 속도·선법 `coolTime`·전투 불러오기 멈춤·괴수 전투의 NPC 법보를 닫았고, 읽기 전용 리뷰 세 갈래(서비스·UI·도구와 문서)의 지적과 개선 묶음 1~7을 13 문서에 정리했다. 탐침 명령 `tswatch`·`iwatch`·`npcart`(개발 빌드만). 06 문서 5절의 검사 부분 표 27줄과 README 빌드 명령을 코드에 맞췄다. 배포 코드는 바뀌지 않았고 게임 설치도 하지 않았다.
- **2026-10-30**: 작은 수정 2차(05 문서 89절, 13 문서 7절 묶음 1. 계획 `2026-10-30-small-fixes-2.md`, 브랜치 `fix/small-fixes-2`, 회차 `sf2-a`~`c-1030`): 자원 탭 보유 칸(같은 값·떠나며 줄이기 무시, 줄이기는 Enter + 확인), 세이브를 떠날 때 성공·드롭 상태와 하위 화면 닫기, 돌아가기가 결과 유지, 기술 추가 안내 두 줄, `경지 10까지` 두 번 누르기, 비급 수량 칸 떠날 때 저장, 일격의 동료 제외, 문구 넷. 게임 설치는 하지 않았다.
- **2026-10-30 (2)**: 배율 서비스 다듬기(05 문서 90절, 13 문서 7절 묶음 2. 스펙·계획 `2026-10-30-rate-service-cleanup*`, 브랜치 `feat/rate-service-cleanup`, 회차 `rsc-a`~`d-1030`): 전투 단위 상태 전환을 `RateUnitRules`로, 이동 처리기는 붙어 있는지로, 수치 함수 여섯을 `RateGroups`로, `RateService`를 두 파일로. 동작 불변. 2026-10-30 21:34 bb122e1로 설치됨(1절).
- **2026-10-30 (3)**: 검사 도구 정리(05 문서 91절, 13 문서 7절 묶음 3. 계획 `2026-10-30-check-tools-cleanup.md`, 브랜치 `chore/check-tools-cleanup`, 회차 `ctc-a`~`d-1030`): 검사 아홉의 복붙 도우미를 `check-lib.ps1`로(전투 진입·종료는 8.0절 규칙판 하나), `session.ps1` 복원 빈틈 셋, `session.tests` 45 → 51. 배포 코드는 바뀌지 않았다.
- **2026-10-30 (4)**: 파일 안전과 예외 경로(05 문서 92절, 13 문서 7절 묶음 4. 계획 `2026-10-30-file-safety.md`, 브랜치 `fix/file-safety`, 회차 `fs-a`·`b-1030`): `AtomicFile`(번역표 셋·`config.txt`·`martial-grid.txt`·즐겨찾기를 임시 파일 뒤 바꿔치기로), `ConfigFile` 모든 예외 로그, 프리셋 표지·전승/배운 수의 깨진 공법·프리셋 되돌리기·화면 글 치환의 예외 경로, 도호 조용 표시를 세이브 떠날 때 비움. 2026-10-30 21:34 bb122e1로 설치됨(1절).
- **2026-10-30 (5)**: 리팩토링 2차(05 문서 93절, 13 문서 7절 묶음 5. 계획 `2026-10-30-refactor-2.md`, 브랜치 `chore/refactor-2`, 회차 `rf2-a`~`-1030`): 순수 판정 넷(`PrefixRow`·`ModSkillRules.RemapPrefixNumbers`·`MartialRules`·`PresetRules`·`BreakthroughRules`, 시험 22줄), 서비스 중복(R2·R5·R6·R8), UI 위젯(`ChipBar`·`CountChip`·`NumberChips`·`ToggleRow`·`MissingNote`), 자원·공법 탭 partial, 그리기 되풀이 정리. 동작 불변. 2026-10-30 21:34 bb122e1로 설치됨(1절).
- **2026-10-31**: 극 대화 창의 흰 배경 조사(05 문서 94절, 브랜치 `chore/drama-bg-probe`, 회차 `drama-a`~`c-1031`): [蛋]음양쌍수(3426669627)의 패키지에 대화 배경 그림 62가지(대화 96줄)가 없어 흰 판이 나온다(원본 패키지부터 없음). 개발 명령 `rdrama` 추가. 편집기 동작은 그대로(설치 불필요). 흰 판을 검게 두는 손질은 사용자 결정 전.
- **다음에 할 일(2026-10-30)**: 13 문서 7절의 묶음은 모두 끝났고 설치됐다(bb122e1, 21:34). 남은 것은 8.0절(사용자 확인·결정)과 13 문서 7절의 6(문서 남은 것)·7(새 기능. 사용자가 고를 때). 그 아래 줄은 2026-10-18까지의 목록이다.
- (2026-10-18까지의 목록) **다음에 할 일**: (000) 코드 리뷰 `docs/10-review-2026-10-18.md`(2026-10-18) 5절의 묶음 가운데 사용자가 고른 것 — 작은 수정 묶음은 끝남(58절, 12:18 설치. 그 검증에서 나온 명령 채널 경합은 59절에서 고침, 목록 휠 실패는 재현 안 됨) / 리팩토링 1차(R1~R5·R7) / 측정 뒤 결정(08의 10·11). (00) 4차 모드의 실제 동작 확인(8.5절 마지막 줄). (0) 설치는 끝났다(6c12d31, 2026-10-17 16:40. 2026-10-18에 8500271로 갱신). 사용자 확인 목록은 8.5절의 마지막 묶음("2026-10-17 설치본"). (1) 사용자 확인(8.5절): 새 모드 8개의 실제 효과와 한국어 글(종족 선택·요도희·절색방·BGM), 새 탭 줄·화폐 그룹·안전 단추의 쓰임새. (2) 리뷰에서 남긴 것(48·49절 끝): 12(내보내기 format 2)·10·11·15·T2~T5·낮음 21~49. (3) 열린 일: 이동 배율 x0.25의 이른 월말 보고(8.3a절). (4) 게임의 `Mods\TlToyboxDevRemote.dll`(2026-10-12 빌드)도 환경 변수를 읽는 새 빌드로 바꾸려면 사용자 확인(기본 경로는 같아 회차에는 영향 없음). (5) X 후보: 이름 키가 없는 모드 기술 168개의 "이름 없음(번호)" 표시(50절 검토). (6) 입화(52절, 해결): 2815562168 `Body\149` 폴더를 뺄지(사용자 결정)와 `workshop.ps1 -Install` 뒤 모드 순서(`Mod_LoadData`) 갱신 단계를 41절 절차에 넣을지. 10:04 세션이 24개를 읽은 까닭은 미확인으로 둔다. (7) 중국어가 또 보이면 회차에서 `rzhleft`로 그 글의 원문을 받아 `Translations/texts.txt`에 줄을 더한다(54절. 부분 치환은 조각이 하나라도 빠지면 글 전체가 중국어로 남는다. 시작 화면 패치 노트와 모드 목록 창은 일부러 둔다). (8) 전투 불러오기 멈춤(55절, 한 번): 다시 나면 그 순서(편집기 열린 채 `numbers` 검사 뒤 `awset 1,3` → `fight 1011 20` → 전투 확인)로 재현한다. (9) 법보 탭 후속 후보(56절, 사용자가 원하면): 법보의 경지·강화·접두 옵션, 기령의 친밀도·장비.
- 사용자의 세이브: 1번 슬롯(인물 "신환", 결정 초기). 사용자는 설치본으로 직접 플레이하며 편집기를 쓴다(2026-10-12 밤, 2026-10-13 13:50~14:02와 21:04~21:06. `changes.log`에 남아 있다). 사용자가 계속 플레이하므로 세이브의 날짜는 수시로 바뀐다(2026-10-16 오전 캡처: 2년 7월 8일, 결정 초기). 고정본(5절)은 영향을 받지 않는다.
- **검증은 사용자의 세이브가 아니라 고정본 세이브에서 한다**(5절). 회차가 끝나면 사용자의 세이브는 회차 전과 같게 되돌려진다.

## 2. 편집기 기능 지도

탭 줄(16개, 간격 46): 능력치 · 자원 · 보관함 · 경지 · 공법 · 선법 · 신기 · 운명 · 기예 · NPC · 세계 · 배율 · 프리셋 · 도호 · 법보 · 성공·드롭. 안전은 제목 줄의 단추(`Tab14`. 누르면 안전 화면, 다시 누르면 앞 탭). 검증 스크립트의 이름은 고정 번호 `Tab<번호>`다(능력치 0, 자원 1, 보관함 3, 경지 4, 공법 5, 선법 6, 신기 7, 운명 8, 기예 9, NPC 10, 세계 11, 배율 12, 프리셋 13, 안전 14, 도호 15, 법보 16, 성공·드롭 17. 2는 비었다. 새 탭은 18부터. `uistate`의 `tab=`도 이 번호). 화폐는 자원 탭의 그룹이다. 탭 줄은 18개까지 들어간다(05 문서 47절).

| 탭 | 기능 | 코드 | 실험 기록(05 문서) |
|---|---|---|---|
| 능력치 | 게임 인물 화면의 다섯 분류(개인·전투·공법 자질·영근 자질·기예)로 저장값 편집(값 칸에 쓰고 Enter), 증감 단위, 자원 회복. 전투 속성 끝에 선법의 본원 선력(0~40). 여섯째 분류 `성격`(내재 1·외재 2를 칩으로, 편집 대상 기준, 66절) | `AttributeCatalog`, `EditService.SetAttribute`·`SetTrait`, `TraitRules` | 5, 14, 15, 27, 35, 66절 |
| 자원 | 지급 수량, 가방 칸(±10, 직접 지정), 화폐 그룹(영석과 재화 8종: 종문 공헌, 생기, 사기, 정도의 기, 마도의 기, 성주령, 전공, 선우. 명성 줄. 47절), 용도 그룹 10개(9개와 기타. 왼쪽 목록, 개수는 거르기를 반영)와 종류 칩, 이름·품질·보유 중만 거르기, 아이템 표(☆·이름·종류·품질·보유·지급. 설명은 아래 칸. 보유는 숫자 칸(늘리기는 Enter·떠나기 모두, 줄이기는 Enter로 묻고 같은 값의 Enter로 확인. 89절), `버리기`는 두 번 누르기. 장착품은 지우지 않는다, 66절), 즐겨찾기(아이템+수량, 비급+권수, 모두 지급), 기술 서적(비급) 지급: 경지·최소 품질, 한 번에 20권 | `ItemGroups`, `ItemCatalog`, `FavoriteStore`, `MartialBookService`, `EditService.GiveItem`·`TryGiveItem`, `BagService`(`BagService.Count.cs`의 수량 지정), `BagPlan` | 14, 15, 25, 27, 31, 38, 39, 66절 |
| 보관함 | `가방`: 현재 가방(아이템, 장착 중인 것, 영석과 화폐)을 이름을 붙여 JSON 파일로 내보내기, 목록·세부, 하나씩 또는 모두 가져오기(칸이 모자라면 늘릴지 묻는다), 삭제. `캐릭터`: 플레이어의 능력치·경지·공법·기예·운명·역천개명을 내보내고 전부·구역별·줄 하나씩 가져오기 | `BagExportService`, `CharacterExportService`(+`.Import`), `ExportFiles`, `Json`, `UI/Tabs/VaultTab`(+`.Character`) | 28, 30, 31절 |
| 경지 | 수련치 채우기, 소경지 돌파, 대경지는 재료·천재지보 지급 후 게임의 돌파 화면 열기 | `EditService`(Breakthrough, GiveBreakthroughMaterials) | 8, 9, 20, 21절 |
| 공법 | 익힌 공법 목록, 옵션 품질·교체(자리 밖 옵션은 `강제` 두 번 누르기, 68절), 숙련 경험치, 장착·해제·망각, 심법 전승(`전승…`: 그 심법이 구성원인 세트 붙이기·떼기, 82절), 수련 단계 대도까지 채우기(`대도까지…`: 옵션을 11개까지 무작위 또는 직접 골라 끼워 넣기·다시 만들기, 83절), 기술 추가(다섯 분류·경지·이름, 모두 배우기, 강제 추가), 학습 상한(게임의 기술 칸 수에 더함. NPC에게도 걸림, 데이터 폴더 `martial-grid.txt`에 남음, 60절) | `MartialService`, `MasteryService`, `MasteryRules`, `SkillCatalog`, `MartialGridService` | 7, 22, 23, 24, 60, 68, 82, 83절 |
| 선법 | 본원 선력(쓴 양/총량, ±1·최대), 가진 선법, 선법 추가(선인·종류·이름으로 걸러 하나씩·모두 익히기). 장착은 게임의 선법 창에서 | `ImmortalService`, `UI/Tabs/ImmortalTab` | 36절 |
| 신기 | 신기 셋(쌍어패·호천안·연요호) 켜고 끄기(마지막 하나는 못 끔), 쌍어옥패 점수, 호천안(손상 해제, 요수 해금·모두 해금, 기술 장착), 연요호(손상 해제, 호요 추가(단계 고르기, 28까지), 경험치 채우기). 플레이어 세계 자료. 탭 밖에서도: 신기 표시가 둘 이상이면 지도 화면의 버튼과 전투 화면의 칸을 나란히 놓는다. 전투 단축키(신기마다 따로, 게임의 신기 키 끄기)와 전투 칸 클릭 | `ArtifactService`, `GodEyeService`, `PotmonService`, `ArtifactHudService`, `ArtifactKeyService`, `UI/Tabs/ArtifactTab` | 37절, `docs/07-divine-artifacts.md` |
| 운명 | 선천운명 추가·교체·삭제, 후천운명 추가·삭제, 역천개명(경지별) 지정. 같은 계열의 역천개명을 다른 대경지에 가지고 있으면 거부한다 | `LuckService`, `FateService` | 10, 15~19, 30, 31, 34절 |
| 기예 | 연단·연기·부적·풍수·약재·광재의 서적 배우기·잊기·모두 배우기·강제 추가. 하위 화면 `제작 재료…`: 연단·연기·부적 처방을 고르면 한 벌(묶음 1·5·10)의 재료를 가방에 넣는다(62절) | `ArtService`, `CraftMaterialService`, `CraftPlan`, `UI/Tabs/ArtTab.Materials` | 22, 23, 24, 61, 62절 |
| NPC | 가까운 NPC 목록·이름 검색, 호감도, 편집 대상으로 지정 | `EditService`(SetIntimBoth) | 5, 27절 |
| 세계 | 순간 이동, 날짜(고정, +1·+5·+10일, 다음 달 1일로, 연·월·일 직접 지정) | `WorldService`, `TimeService` | 13, 25절 |
| 배율 | 분류 칩 다섯(성장·시간·속도·전투·관계·소모·켜 둔 것, 칩마다 켜 둔 줄 수. 86절)으로 20줄: 수련치 획득, 전투 중 공법 숙련, 비급 학습 진도(64절) / 지도 속도(전투 밖 게임 속도, 86절), 전투 속도, 이동의 날짜 소모, 수련·학습의 날짜 소모(63절) / 기술 쿨타임(없음까지. 선법·영역 cd도, 87절. 전투 단위 상태의 전환 규칙은 90절), 전투 아이템 쿨타임(단약 칸·법보 발동, 87절), 신기 쿨타임, 영력 소모, 집중력 소모(없음이면 전투 중 모든 감소를 막음, 66절), 주는 피해(x100 다음 `일격`, 87절), 받는 피해, 무적 / 호감 변화(오를 때 양쪽, 86절), 정력 소모(없음까지, 86절), 아이템 소모(없음이면 쓰는 아이템이 줄지 않음, 66절), 법보 내구 소모(없음이면 플레이어 법보 내구가 줄지 않음, 87절). 전투 이동 속도(86절)는 시간·속도 칩(87절에서 옮김). 모두 1배로. 개발 빌드는 처음에 모든 줄을 한 화면에(`ratechip`) | `RateService`, `RateGuards`, `RateGroups`, `BattleService`(`WorldService.cs` 안), `UI/Tabs/RateTab` | 13, 32, 33, 37, 66, 86, 87절 |
| 프리셋 | 빌드 프리셋(JSON 파일. 기본 17개: 01~16과 만고신화용 21번 만고검결) 목록: `적용`(지금 대경지까지), `최종 단계까지`(경지를 등선 후기까지 올리고 전부), `상세보기`(대경지 단계마다의 구성·상태·적용, 경지 올리기), `기본 프리셋 되돌리기`. 공법 넷·심법과 세트·역천개명·선천운명·법보와 기령·신혼·명우를 넣는다. 이미 있는 프리셋 폴더에는 새로 생긴 기본값만 푼다(`.defaults` 기록, 70절). 역천개명 줄의 대체 번호 `alt`는 원래 번호를 그 경지에서 쓸 수 없을 때만 쓴다(73절) | `PresetService`(+`.Apply`), `PresetDefaults`, `FateAlt`, `SpriteService`, `ElderService`, `WingmanService`, `GradeService`, `Presets/*.json`, `UI/Tabs/PresetTab` | 34, 69, 70, 73절 |
| 안전(제목 줄 단추) | 세이브 백업, 변경 이력(지난 실행 포함, 검색) | `Safety/SaveBackup`, `Safety/ChangeLog` | 3, 27절 |
| 도호 | 플레이어의 도호(칭호) 100개: 장착 칸, 분류 칩, 검색·가진 것만, 주기·빼기(두 번)·장착, 다른 인물이 가진 '제일' 도호 가져오기(두 번), 잃는 경지 경고, 이야기 도호 안내. 편집기가 준 도호의 "도호 획득" 창과 편집기가 뺀 도호의 "도호 상실" 창은 편집기가 확인을 누른다 | `AppellationService`, `UI/Tabs/AppellationTab` | 40절 |
| 법보 | 편집 대상이 가진 법보(장착·해제, 수리·모두 수리, 든 기령 빼기)와 기령(재능 모두 켜기, 레벨 올리기, 장착한 빈 법보에 넣기, 지우기(두 번)), 기령 친밀도 최대, 법보 줄의 `강화…`(경지 올리기 10까지 — `경지 10까지`는 두 번 누르기(89절)·단계 1~6, 6은 한계돌파 비율 120, 집중력 비율은 100까지), 하위 화면 기령 만들기(32종, 검색)·법보 지급(열 종, 경지·단계 칩. 기본 단계 5) | `TreasureService`, `TreasureRules`, `SpriteService`(보이는 재능 표), `UI/Tabs/TreasureTab` | 56, 60, 82절 |
| 성공·드롭 | 훔치기 성공, 관계 발전 성공(플레이어가 청한 관계를 NPC가 받아들임), 전투 드롭 배수(x1·2·3·5), 참오 결과 최대(게임 참오 창의 굴림을 최대 품질 맨 위로, 82절), 모두 끄기. 저장하지 않음 | `SuccessService`, `SuccessTab` | 67, 68, 82절 |

공통 규칙:

- 쓰기 전에 한 실행에 한 번 세이브 폴더를 백업한다(`SaveBackup.EnsureOnce`). 모드의 파일은 `%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\TlToybox\`에 있다: `backups\`, `changes.log`, `config.txt`(단축키, 창 위치), `editor-lucks.txt`, `bag-slots.txt`, `favorites.txt`(자원 탭 즐겨찾기, 모든 세이브 공통. 아이템 줄 `<번호> <수량>`과 비급 줄 `book <분류> <기술> <경지> <최소 품질> <권수>`), 내보내기 파일의 `exports\bag\`·`exports\character\`, 프리셋의 `presets\`.
- 쓰기 뒤에 값을 다시 읽어 이력에 남긴다. 다르면 "(재조회 불일치)"가 붙는다.
- 되돌리기 어렵거나 게임이 붙인 것을 지우는 동작은 버튼을 두 번 눌러야 한다(`Confirm`). 확인 대기는 창 전체에 하나이고 다른 동작을 하면 풀린다.
- 능력치·공법·선법·운명은 편집 대상(플레이어 또는 NPC)에, 자원·화폐·보관함·경지·기예·세계·프리셋은 플레이어에 적용된다. 신기는 세계 자료(`g.data.world`)라 편집 대상과 무관하다.
- 경지 번호는 직접 쓰지 않는다. 예외는 둘이다: 캐릭터 가져오기의 경지(30절)와 프리셋의 경지 올리기(34절). 둘 다 두 번 눌러 확인한다.
- 켜고 끄는 기능(무적, 피해 배율, 날짜 고정, 배율)은 저장하지 않는다. 게임을 켤 때마다 꺼진 상태로 시작하고, 세이브를 떠날 때(`Destroy`)도 모두 꺼진다(48절. 이력에 남는다).
- 편집기는 게임 화면 사이에 끼어 그려진다. 편집기를 연 뒤에 뜨는 게임 창은 편집기 위에 나오고, `Insert`는 가려진 편집기를 맨 앞으로 가져온다(맨 앞이면 닫는다). 켜 둔 기능은 화면 위쪽 가운데에 표시된다.
- 목록의 글은 자르지 않고 줄바꿈해 전부 보인다(사용자 지시, 29절).
- 게임 코드에 끼어드는 방법은 두 가지를 쓴다: 이벤트 구독(전투 보조)과 Harmony Prefix(날짜 고정과 배율, 처음 켤 때만 설치). 전투 속도는 전투 장면에서 매 프레임 `Time.timeScale`을 맞춘다.

## 3. 저장소 구조

| 경로 | 내용 |
|---|---|
| `docs/00-design-brief.md` | 기획서(기준 문서) |
| `docs/01-game-structure.md`, `02-mod-dev-research.md`, `02a-workshop-survey.md`, `04-ingame-state-analysis.md` | 사전 조사 |
| `docs/05-editor-v0.1.md` | 편집기의 범위·규칙·실험 기록(1~88절). 기능을 바꾸면 절을 추가한다 |
| `docs/06-handoff.md` | 이 문서 |
| `docs/07-divine-artifacts.md` | 시작 신기 셋(쌍어패·호천안·연요호)과 특수 선천운명 셋(식혼검·음양안·연화검의)의 조사. 게임 확인(7절), 편집기 후보(8절), 미확인(9절) |
| `docs/08-review-2026-10-16.md`, `docs/10-review-2026-10-18.md` | 편집기 전체 리뷰(2026-10-16), 코드 리뷰와 리팩토링 검토(2026-10-18). 남은 항목은 1절 끝 "2026-10-18까지의 목록"과 8.6절 |
| `docs/11-trainer-comparison.md` | 외부 트레이너(ggbh-trainer 0.8.30)와의 기능 비교(2026-10-19). 4절의 제안 순서로 65~68절을 만들었다 |
| `docs/12-review-2026-10-22.md` | 코드 리뷰·성능 측정과 개선 계획(2026-10-22): 탭 여는 시간, 자원 탭 첫 열기 4초의 원인, 모드 공법 비급 실패 522개의 분류, 트레이너 미반영 24개, 묶음 순서(7절) |
| `docs/superpowers/specs/`, `plans/` | 작업 단위의 설계 스펙과 구현 계획: 사전 조사, 묶음 A(`2026-10-12-date-bag*`), B(`2026-10-13-ui-rework*`), C(`2026-10-13-bag-export*`), D(`2026-10-13-character-export*`), F1(`2026-10-13-rate-tab*`), F2(`2026-10-14-preset-tab*`, 프리셋 열 개의 근거는 `specs/2026-10-14-preset-builds.md`), 선법 탭(`2026-10-14-immortal-tab*`), 신기 탭(`2026-10-14-artifact-tab*`), 도호(`2026-10-15-appellation*`), 비급(`2026-10-15-martial-books*`), 자원 탭(`2026-10-15-resources-tab*`), 창작마당 설치(`2026-10-15-workshop-install*`), 한글화(`2026-10-16-mod-korean*`), 모드 공법의 비급(`2026-10-16-mod-skill-books*`), 창작마당 확장(`2026-10-16-workshop-expand*`), 새 모드 한글화(`2026-10-16-mod-korean-2*`), 리뷰 수정(`2026-10-16-review-fixes*`), 리뷰 도구 묶음(`2026-10-16-review-tools*`), LocalText 밖 글 한글화(`2026-10-17-zh-text-korean*`), 법보 탭(`2026-10-17-treasure-tab*`), 기예 제작 재료(`2026-10-18-craft-materials*`), 트레이너 비교 1·2묶음(`2026-10-20-trainer-batch1*`, `2026-10-20-trainer-batch2*`), 기본 프리셋 11~16(`2026-10-20-preset-drafts*`, 작업 기록 `specs/2026-10-20-preset-drafts-builds.md`) |
| `src/ModCode/ModMain/ModMain.cs` | 진입점(`Init`, `Destroy`) |
| `src/ModCode/ModMain/Services/` | 게임 값의 읽기·쓰기. 화면을 모르고 결과를 문장으로 돌려준다. `AtomicFile.cs`(사용자 파일의 임시 파일 뒤 바꿔치기 쓰기, 92절), 순수 판정 `PrefixRow`·`MartialRules`·`PresetRules`·`BreakthroughRules`·`RateUnitRules`·`NumberFieldRules`(93·90·89절), `Json.cs`(작은 JSON 읽기), `ExportFiles.cs`(내보내기 파일의 공통 처리), 한글화의 `TranslationService.cs`·`TranslationTable.cs`(LocalText 키 번역표)·`TextTranslationService.cs`·`TextReplacer.cs`(원문 기반 번역표, 54절), 법보·기령의 `SpriteService.cs`(프리셋)·`TreasureService.cs`(법보 탭)도 여기 있다. 2026-10-18 뒤에 생긴 것: `CraftMaterialService.cs`·`CraftPlan.cs`(기예 제작 재료, 62절), `MartialGridService.cs`(기술 학습 상한, 60절), `BagPlan.cs`·`BagService.Count.cs`(가방 수량 지정, 65·66절), `TraitRules.cs`(성격, 66절), `SuccessService.cs`(성공·드롭 탭, 67·68절), `PresetDefaults.cs`(새 기본 프리셋만 풀기, 70절), `TextTags.cs`(모드 글의 서식 태그 지우기, 72절), `FateAlt.cs`(역천개명 대체 번호, 73절), `DressThumbService.cs`(캐릭터 만들기 의복 빈 썸네일, 74절), `LocalTextIndex.cs`·`LocalTextRules.cs`(LocalText 한 번 훑기와 번역 판정, 77절), `ModSkillRules.cs`(모드 공법 이름·숨김·비급 경지 판정, 78절), `FileStamp.cs`·`StampCache.cs`(폴더 표지와 표지 캐시, 79절), `GiveBudget.cs`(비급 지급 시간 상한, 80절), `ApplyTally.cs`·`ConfigFile.cs`(+`.Io.cs`)·`FavoriteStore.Lines.cs`(81절). 게임 형식을 쓰지 않는 `CraftPlan`·`BagPlan`·`TraitRules`·`PresetDefaults`·`TextTags`·`FateAlt`·`LocalTextRules`·`ModSkillRules`·`FileStamp`·`StampCache`·`GiveBudget`·`ApplyTally`·`ConfigFile`·`FavoriteStore.Lines`는 `tests/ModMain.Tests`가 시험한다 |
| `src/ModCode/ModMain/Translations/` | DLL에 내장되는 번역표 셋: `mods.txt`(LocalText 키<TAB>한국어, 16,184행. 43·46·50·57·72절), `texts.txt`(원문<TAB>한국어, 245줄. 화면 글 치환, 54절), `tables.txt`(표 칸 전용 원문<TAB>한국어, 3,271줄. 글 전체가 같을 때만 바꿈, 74절). 설치 뒤 첫 실행 때 데이터 폴더 `translations\`로 풀린다. 묶음을 합치는 스크립트는 `tools/research/merge-mods-translations.py`(mods)·`merge-table-texts.py`(tables) |
| `src/ModCode/ModMain/Presets/` | 기본 프리셋 17개(01~16, 21. JSON)와 파일마다의 검증 기록 `*.meta.json`(검증 등급·출처·모드 의존성. DLL에 넣지 않는다, 69절). JSON만 DLL에 EmbeddedResource로 들어간다. 손으로 고치지 않고 `tools/research/preset-build.ps1`의 표를 고쳐 다시 만든다(만고신화 설치본의 덤프로 전체를 다시 만들면 역천개명 이름이 바뀌므로 `-Only <번호>`로 고칠 것만, 73절) |
| `src/ModCode/ModMain/UI/` | `Ui.cs`(uGUI 도우미, 글 높이 재기), `EditorHost.cs`(단축키, 매 프레임 처리), `EditorWindow.cs`(창의 틀), `StatusOverlay.cs`(켜 둔 기능 표시), `InputGuard.cs`(편집기 입력 칸에 글을 치는 동안 게임 단축키를 막음), `PointerGuard.cs`(창 위 입력이 지도에 새지 않게 하는 Harmony 패치, 51절), `TextPatch.cs`(화면 글의 한자 치환 Harmony 패치, 54절), `Tabs/`(탭마다 한 파일, `EditorTab` 상속), `Widgets/`(`ListView`, `SearchBox`, `DetailPane`, `Confirm`, `NumberField`, `AmountBar`, `ChipBar`·`CountChip`·`NumberChips`(93절)) |
| `src/ModCode/ModMain/Safety/` | 백업, 변경 이력 |
| `src/ModCode/ModMain/Dev/` | 개발 빌드 전용 명령(배포 빌드에서 제외) |
| `tools/install.ps1` | 배포 빌드를 만들어 게임에 설치 |
| `tools/workshop.ps1`, `tools/workshop/` | 창작마당 모드 도구(`-Status`·`-Download`·`-Install`·`-Uninstall`·`-Search`·`-Doc`)와 목록 `mods.txt`(일곱 칸)·이력 `history.txt`·후보표 `candidates-*.md`·컬렉션 ID `collection.txt`·설명문 `collection-desc.txt`. 시험 `tools/workshop.tests.ps1`(81개). 41·45절 |
| `docs/09-workshop-mods.md` | 창작마당 모드 등록부. `tools/workshop.ps1 -Doc`이 만든다(손으로 고치지 않는다) |
| `tools/DevRemote/` | 게임을 화면 밖에서 조작하는 도구(MelonMod + 스크립트). `session.ps1`이 검증 한 회차를 묶고, `session.tests.ps1`이 그 안전 규칙을 시험한다(45개). 명령 채널의 공용 함수 `channel.ps1`(경합 셋을 막음, 59절. 시험 `channel.tests.ps1` 7개), `.devremote` 경로의 기준 `devremote-dir.ps1`(시험 12개, 49절) |
| `tools/build-tag.tests.ps1` | 배포 빌드에 커밋 표지(`<짧은 커밋>[-dirty]`)가 들어가는지 시험한다(4개, 49절) |
| `tools/research/` | 게임 어셈블리 조사 스크립트, 게임 내 자동 검증(`ui-check.ps1`, `datebag-check.ps1`, `preset-check.ps1`, `immortal-check.ps1`, `artifact-check.ps1`, `treasure-check.ps1`, `trainer3-check.ps1`, `trainer4-check.ps1`, `mastery-check.ps1`, `major-breakthrough-check.ps1`. 공통 함수는 `check-lib.ps1`), 기본 프리셋을 만드는 `preset-build.ps1`, Python 조사 스크립트 `fatury-scan.py`·`fatury-log.py`(52절)·`check-texts.py`(54절), 번역 묶음을 번역표에 합치는 `merge-mods-translations.py`(43절의 절차)·`merge-table-texts.py`(74절). Python은 `py -3 -I -X utf8`로 돌린다 |
| `ModProject_sWP6UL/`, `.devremote/`, `.superpowers/` | git 제외. 모드 에디터 프로젝트, 조작 도구의 작업 폴더(세이브 복사본, 고정본 `fixtures\`), 계획 실행 중의 임시 기록 |

`src/ModCode/ModMain/Adapters`, `Features`, `Resolvers`는 처음 뼈대만 만들고 쓰지 않았다(`.gitkeep`만 있음). `tests/ModMain.Tests`는 게임 없이 도는 시험 프로젝트다(49절. `dotnet run --project tests/ModMain.Tests -c Release`).

## 4. 빌드와 설치

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release                # 개발 빌드(Dev 명령 포함)
    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release -p:DevTools=false -o <임시폴더>   # 배포 구성 컴파일 확인
    pwsh tools/install.ps1                                                     # 배포 빌드를 게임에 설치
    pwsh tools/workshop.ps1 -Status | -Download | -Install [-Yes] | -Uninstall [-Yes] | -Search [-Kind …] [-Top n] | -Doc   # 창작마당 모드(목록 tools/workshop/mods.txt, 05 문서 41·45절). -Yes 없이는 계획만. 회차 안에서는 -NoHistory

- 게임이 켜져 있으면 DLL이 잠겨 설치·교체가 안 된다. 한 번 로드된 DLL은 게임을 끄기 전에는 바뀌지 않는다.
- 두 구성 모두 컴파일되는지 매번 확인한다(`Dev\`의 코드를 배포 코드가 참조하면 배포 구성에서 깨진다. `#if DEVTOOLS`로 감싼 것은 된다).
- 스크립트는 PowerShell 7(`pwsh`)로 실행한다.
- 창작마당 모드의 설치된 폴더를 덮거나 지우는 것은 게임이 켜져 있으면 거부된다. 새 폴더는 켜져 있어도 들어가고 다음 게임 시작부터 읽힌다. 시험은 `pwsh tools/workshop.tests.ps1`(게임·SteamCMD 없이 임시 폴더에서).
- 창작마당 모드의 받음·설치·갱신·지움은 `tools/workshop/history.txt`에 남는다. 회차 안에서 넣을 때는 `-NoHistory`(`-End`가 되돌리므로). 목록·이력·설치가 바뀌면 `pwsh tools/workshop.ps1 -Doc`을 돌려 `docs/09-workshop-mods.md`와 `tools/workshop/collection-desc.txt`를 같이 커밋한다(45절).
- 번역 파일은 DLL 안에 있어 따로 설치하지 않는다. 설치 뒤 첫 실행 때 모드 데이터 폴더 `translations\mods.txt`로 풀리고, 이미 있으면 내장본에만 있는 키의 줄이 끝에 덧붙는다(46절. 사용자가 고친 줄은 그대로. 줄을 지워 번역을 끄면 다음 실행에 다시 들어오므로 끄려면 글을 비운다). 새 모드의 글을 더하는 절차는 43절(`rloc dump` → 번역 → `Translations/mods.txt`).

## 5. 게임에서 검증하는 방법

사용자의 화면·마우스·키보드를 건드리지 않고 한다. 사용자가 게임을 켜 둔 동안에는 시험할 수 없고, 사용자의 게임을 임의로 끄지 않는다(끄도록 알리고 기다린다).

### 회차 스크립트 (기본)

    pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base   # 확인 → 개발 빌드 → 세이브 복사·검증 → 세이브를 고정본으로 바꿈 → 설치 → 화면 밖에서 실행해 1번 세이브 진입
    pwsh tools/DevRemote/session.ps1 -Restart                      # 게임만 껐다 켠다(저장 후 재시작 확인)
    pwsh tools/DevRemote/session.ps1 -End <이름>                   # 종료 → 사용자의 세이브 복원·일치 확인 → 그 밖의 것 되돌리기와 정리

- **만고신화(2814696167)가 설치된 뒤로(2026-10-20) 기존 검사는 `-Exclude 2814696167`을 붙여 돌린다.** 만고신화는 경지 표를 바꿔 고정본 인물(경지 9)이 축기로 읽히고, 바닐라 경지 번호를 전제한 검사(preset `state`·`files`·`ui`, `grade`, `modbooks` 등)가 어긋난다. `-Exclude`는 회차 동안 그 모드 폴더를 `.devremote\session-parked-mods`로 옮겼다가 `-End`가 되돌린다(`Mod_LoadData`는 바이트까지 그대로). 만고신화가 있어야 맞는 검사(`translation`, 프리셋 `preset-all`의 21번과 대체 역천개명)는 `-Exclude` 없이 돌린다. 73절.
- **`-Fixture base`를 붙여 고정본에서 검증한다.** 고정본은 `.devremote\fixtures\base`(git 제외)다. 인물 신환, 결정 초기(경지 9), 2년 2월 24일, 가방 89/154(추가 칸 +90), 공법 28, 역천개명 기록 `2:700016, 3:700040`, 떠 있는 게임 창 없음. 없어졌으면 다시 만든다:

      pwsh tools/DevRemote/session.ps1 -MakeFixture base -From "$env:USERPROFILE\AppData\LocalLow\guigugame\guigubahuang\TlToybox\backups\20261013-125814\CacheData"

  `-Fixture` 없이 열면 사용자의 지금 세이브에서 돈다(지금은 월말 기록 창 때문에 `order`·`items`·`numbers`가 실패한다). 고정본을 새로 고를 때의 함정은 05 문서 31절에 있다.
- `-Begin`은 "WORLD READY", `-End`는 "SAVE RESTORED AND IDENTICAL"이 나와야 성공이다. `-Begin <이름> -ReleaseBuild`는 배포 구성으로 설치해 띄운다(개발 명령이 없으므로 30초를 기다리기만 한다).
- **회차 이름은 한 번 쓴 것을 다시 쓰지 않는다.** 같은 이름의 복사본(`.devremote\save-backup-<이름>`)이 남아 있으면 `-Begin`이 열리지 않는다.
- 회차는 한 번에 하나다. `-Begin`이 표식(`.devremote\session.name`)을 남기고, `-End`는 열린 회차와 이름이 같을 때만 세이브를 되돌린다. 시험 전 복사본은 `save-backup-<이름>`에, 되돌리기 직전의 세이브는 `save-before-restore-<이름>`에 남는다.
- `-End`가 되돌리는 것: 세이브, `bag-slots.txt`, `martial-grid.txt`(기술 학습 상한, 60절), `favorites.txt`와 `.bak`(`.tmp`는 지운다), `config.txt`(창 위치. 회차 전에 없었으면 지우지 않는다 — 13 문서 C4), 프리셋 폴더(`presets\`. 없었으면 없는 상태로), 번역 폴더(`translations\`. 프리셋과 같은 규칙. 검사 `translation`이 파일을 바꾸고 지운다), 설치돼 있던 DLL. 끝내지 못한 회차의 프리셋·번역 보관본(`.devremote\session-presets`, `session-translations`)이 남아 있으면 `-Begin`이 멈춘다(사용자의 파일의 유일한 사본일 수 있다). 정리하는 것: 회차 동안 모드가 만든 자동 백업(회차 전부터 있던 것은 남긴다), 최근 다섯 회차보다 오래된 세이브 복사본. 회차 중 `ModExportData\`에 생긴 폴더(창작마당 모드)는 지운다(`WORKSHOP FOLDERS REMOVED: n`. `Mod_sWP6UL`은 지우지 않는다).
- `-End`가 되돌리지 않는 것: `changes.log`(검사의 기록이 남는다), 내보내기 폴더(검사가 자기 파일 `uicheck-*`를 지운다).
- 자기가 띄우지 않은 게임은 닫지 않고, 그런 게임이 실행 중이면 띄우지도 되돌리지도 않는다. 게임은 창 닫기 요청으로 끈다(`Stop-Process`는 권한 검사에 막힌다).
- 게임은 달이 넘어갈 때 자동 저장한다. 저장 명령을 쓰지 않은 회차도 반드시 `-End`로 끝낸다.
- `-Begin <이름> -NoEditor`는 편집기 폴더를 회차 동안 빼고 띄운다(모드를 뺀 뒤의 세이브 동작 확인용, 30초 기다림, 개발 명령 없음. 05 문서 84절). 고정본 `edited`는 편집기로 바꾼 값(대도 11줄·법보 경지 7 단계 6·전승)이 든 것이다.
- 스크립트를 고치면 `pwsh tools/DevRemote/session.tests.ps1`(게임을 띄우지 않고 임시 폴더에서, 45개)을 다시 돌린다.

### 자동 검증

`tools/research/ui-check.ps1 -Part <이름>` — 편집기 화면의 검증. 줄마다 PASS/FAIL, 마지막 줄 `FAILS=<수>`.

| 부분 | 보는 것 | 돌리는 조건 |
|---|---|---|
| `base`, `tabs`, `people`, `confirm`, `status`, `wrap`, `fateguard` | 창의 기본, 탭마다 그려지는지, NPC 목록, 확인 대기, 켜 둔 기능 표시, 글 줄바꿈과 화폐 탭의 명성, 역천개명 분류의 보호 | 아무 때나 |
| `items`, `numbers` | 자원·화폐 탭(자원 탭의 그룹·종류·검색·품질·보유 중만 포함), 숫자 칸의 Enter(실제 키 입력) | 게임 창이 떠 있지 않을 때 |
| `groups` | 자원 탭의 분류 표(`rgroups`: 기타 0, 중복 0, 합)와 왼쪽 개수, 검색 중의 개수와 포커스, 의복의 남·여 | 아무 때나 |
| `favstore` → `-Restart` → `favstore-restart` | 즐겨찾기 파일(넣기·거부·원자적 쓰기·깨진 줄·.bak·잠긴 파일)과 재시작 뒤 유지 | `favstore`는 경고를 일부러 남기므로 바로 뒤에 `-Restart` |
| `favorites`, `favorites-corrupt` | 화면의 즐겨찾기(☆·수량 칸·지급·모두 지급·거른 줄만), 깨진 파일과 게임에 없는 번호 | `favorites-corrupt`는 경고를 남기므로 회차의 마지막에 |
| `books` | 비급 만들기 서비스(한 권, 최소 품질 6 세 권, 경지 범위 밖 거부, 20권 한도, 변경 이력, 게임 학습이 받아들임, 가방이 찼을 때 멈춤) | 가방을 채우므로 회차의 마지막에 |
| `books-ui` | 자원 탭의 기술 서적 그룹(종류 칩, 경지·최소 품질·이 경지만, 지급과 가방 열, ☆, 즐겨찾기의 비급 줄과 모두 지급, 게임에 없는 기술) | 아무 때나(즐겨찾기를 지우고 시작한다) |
| `modbooks` | 모드 공법의 비급(42절): 카탈로그의 모드 항목 다섯(43절 뒤로 한글 이름), 절기·무예 비급과 학습 수용, 신통 비급(44절. 옵션 번호 치환), `모드만` 칩·꼬리표·아래 칸·즐겨찾기 | 창작마당 모드 `3390738990`이 설치돼 있어야 한다. 가방에 비급을 넣으므로 회차 뒤쪽에 |
| `translation` | 한글화(43절): 적용 결과, 키 읽기(`rloc key`), 카탈로그·비급·줄의 한글 이름, 한글·중국어 검색, 파일 수정·BOM·삭제·주석만, 미번역 내보내기 | 창작마당 모드 `3390738990`이 설치돼 있어야 한다. 회차 안에서 번역 파일을 바꾸므로 `-End`가 되돌린다 |
| `appellation` → `-Restart` → `appellation-restart` | 도호 서비스(주기·거부·'제일' 옮기기·경지 안내·장착·해제·빼기·효과·획득 창 닫기)와 저장 후 유지 | `appellation`이 끝에서 저장한다. 바로 뒤에 `-Restart` |
| `appellation-ui` | 도호 탭(칩·검색·가진 것만·경고·가져오기 두 번·빼기 두 번·장착 칸·아래 칸). NPC 반응 대화와 "도호 상실" 창을 넘긴다 | 깨끗한 회차에서(1404를 관산 혼자 가진 고정본 상태가 필요하다) |
| `history` | 변경 이력 | `items` 뒤 |
| `actions` | 기술·운명·기예의 실제 동작 | 아무 때나(세이브를 바꾼다) |
| `order` | 편집기와 게임 화면의 앞뒤 순서 | 게임 창이 떠 있지 않을 때 |
| `input` | 창 위의 휠·오른쪽 단추 끌기가 뒤의 지도에 새지 않는지, 창 위에서 카메라 eventMask 0, 창 안 목록의 휠 스크롤(51절. 가상 입력으로) | 지도 화면에서, 게임 창이 떠 있지 않을 때 |
| `move` → `-Restart` → `move-restart` → `-Restart` → `move-corrupt` | 창 이동·접기와 위치 기억 | 이 순서로 |
| `vault` → `-Restart` → `vault-restart` | 보관함의 가방 쪽 | 이 순서로 |
| `charvault` → `-Restart` → `charvault-restart` | 보관함의 캐릭터 쪽 | 이 순서로 |
| `rates` | 배율 탭: 수련치와 이동, 되돌리기, 표시와 이력 | 깨끗한 회차의 첫 검사로(패치가 걸려 있지 않은 것을 본다) |
| `bagcount` | 자원 탭의 보유 수량 지정·버리기: 늘리기·줄이기(Enter 두 번. 떠나며 줄이기 무시, 같은 값 조용, 첫 Enter는 묻기만, 다른 단추로 대기 해제. 89절)·0, 전투 아이템 칸 묶음(1개 남김), 장착 법보 보호, 두 번 누르기, 이력(66절) | **깨끗한 회차의 첫 검사로**(고정본 1011011 x10 전제. `favorites`·`items` 뒤면 실패). 법보를 지급한다 |
| `traits` | 능력치 탭 성격: 칩 31개, 같은 외재 칩 꺼짐, 쓰기·이력, NPC 대상(66절) | 아무 때나 |
| `itemuse` | 배율 탭 아이템 소모 없음: 단약·보상자·지도 부적·비급 완료·대경지 돌파 재료·전투 단약은 남고, 영석·연기 재료는 줄고, 끄면 다시 준다(66절) | 달과 전투를 쓴다. 회차의 뒤쪽에 |
| `prefixforce` | 공법 탭 옵션 자리 강제: 자리 밖 줄의 강제 단추, 두 번 누르기 문구, 중복 한도 줄은 그대로 막힘, 이력(68절) | 아무 때나 |
| `success` | 성공·드롭 탭: 훔치기 성공과 보상, 관계 발전(의형제 결연까지 넘기고 `tb2relof`로 확인), 드롭 x3에서 보상 번호 수 세 배, 모두 끄기(68절) | 전투와 관계를 바꾼다. 회차의 뒤쪽에 |
| `mpnone` | 배율 탭 영력·집중력 없음: 전투에서 기술·전투 단약 뒤 영력·집중력 그대로, x0.5에서는 다시 준다(66절) | 전투를 연다 |
| `rates-battle` | 배율 탭: 전투 중 공법 숙련과 전투 속도. 전투에 들어갔다가 끝내고 지도로 나온다 | 게임 창이 떠 있지 않을 때 |
| `combat` | 배율 탭의 전투 줄: 받는 피해, 기술 쿨타임, 주는 피해와 적의 체력 감소. 전투 셋을 차례로 연다 | 게임 창이 떠 있지 않을 때. 5분쯤 걸린다 |
| `confirm2`, `errors`, `backup`, `import-settle` | 48절의 두 번 누르기 넷(선천운명 교체·역천개명 정하기·기술/기예 모두 배우기), 단추·탭 예외가 결과 칸에 보임(`DevThrow`·`uifail`), 백업 없이 쓰던 세 곳의 쓰기 전 백업(`bkreset`), 캐릭터 가져오기 뒤 경지에 따른 보충 | 아무 때나(`backup`은 백업 폴더를 만든다) |
| `grid`, `materials`, `actionday`, `studygain` | 기술 학습 상한(60절. 고정본은 무예 8/8), 기예 제작 재료 지급(62절), 수련·학습의 날짜 소모 배율(63절. `adtrain`·`adstudy`), 비급 학습 진도 배율(64절) | `studygain`은 달을 쓰고 이야기 창이 떠 회차의 마지막에 |
| `rates2` → `rates2-map` → `rates2-battle` → `rates2-life` | 배율 탭 확장(86절): 칩 다섯과 칩별 줄·새 줄 넷·모두 1배로 / 지도 속도 x3과 끄기 / 전투 이동 속도(새 전투에서 다시 걸림, 끄면 원래 값) / 호감 변화·정력 소모(가까운 NPC와 대화) | `rates2-map`·`rates2-battle`은 게임 창이 떠 있지 않을 때. 걷기·대화 뒤 보상 창이 편집기를 가릴 수 있다(8.0절) |
| (트레이너 4차) `trainer4-check.ps1`의 `ui`, `items`, `skills`, `onehit`, `durable` | 칩별 줄·새 줄·일격 단계·켜 둔 것 / 전투 아이템 쿨타임(같은 단약 거부 → 없음에서 단약·법보 연달아, x1이면 `itemCDRate` 100) / 등선 40·선법 장착·영역 넣기 뒤 선법·영역 cd 하한 100과 연달아 발동, x1·x0.5 / 수준 60 적 일격, 무적 없이 맞아도 생존 / 법보 내구 그대로·x1이면 98·수리 | `ui`·`items`는 한 회차, 나머지는 부분마다 깨끗한 회차. 05 문서 87절 |
| `battle`, `status-battle` | 전투 중의 편집기 | 전투에 들어간 채로 끝난다. 회차의 마지막에 돌리고 바로 `-End`(이어서 다른 검사를 하려면 `bend`와 결과 창의 확인으로 나온다). `datebag-check.ps1`의 `freeze`와 같은 회차에 두지 않는다 |

`tools/research/datebag-check.ps1 -Part <이름>` — 날짜·가방의 검증. 부분은 `base`, `time`, `time-restart`, `freeze`, `bag`, `bag-restart`, `bag-corrupt`. `freeze`와 `bag`은 깨끗한 회차에서 시작하고, `-restart`·`-corrupt`는 앞 부분 뒤에 `-Restart`를 하고 돌린다. `time`은 달을 넘길 때 사건이 일어나면 막힌다.

`tools/research/preset-check.ps1 -Part <이름>` — 프리셋의 검증.

| 부분 | 보는 것 | 돌리는 조건 |
|---|---|---|
| `state`, `martial`, `files`, `grade` | 상태 읽기, 옵션을 지정한 공법 만들기와 세트 번호, 프리셋 파일 읽기와 기본 프리셋, 경지 올리기 | 깨끗한 회차에서 이 순서로 |
| `grade-full`, `grade-settle`, `immortal-attr` | 경지 올리기가 정식 돌파와 같은 것을 남기는지(등선 선력·금홍파효 카드·숨은 운명·돌파 임무 정리), 예전 판으로 올린 인물의 보충, 능력치 탭 "본원 선력"(0~40) | `grade` 뒤 같은 회차 |
| `extras` → `extras-battle` | 법보·기령·신혼·명우 쓰기, 전투에서 명우 | 깨끗한 회차에서. `extras-battle`은 명우가 있어야 통과한다 |
| `fate-set`, `fate-groups`, `apply-switch` | 역천개명의 계열 보호(운명 탭의 길, 프리셋의 길), 프리셋에서 프리셋으로 갈아타기 | `fate-set`은 깨끗한 회차의 첫 검사로(고정본의 기록에서 시작한다) |
| (선법) `immortal-check.ps1`의 `catalog`, `learn` → `-Restart` → `restart`, `learn-all`, `ui`, `game` | 선법 목록·익히기·모두 익히기·저장 후 유지, 선법 탭 화면, 게임 선법 창 | 깨끗한 회차에서 이 순서로. `learn`은 고정본에 선법이 없는 것에서 시작한다 |
| (법보) `treasure-check.ps1`의 `service`, `ui` | 법보 탭 서비스(지급·장착·기령 만들기·넣기·재능·레벨·빼기·지우기·해제)와 탭 화면(단추로 같은 흐름, 캡처 `check-treasure.png`) | 깨끗한 회차에서 `service` 뒤에 `ui`(다른 번호의 법보·기령을 쓴다). 개발 명령은 05 문서 56절 |
| (법보 3차) `treasure-check.ps1`의 `forge`, `give`, `forge-ui` | 경지 올리기·10에서 멈춤·비율 유지, 단계 1~6, 친밀도 최대, NPC 대상 / 지급의 비율 100·경지·단계, 공용 지급 길, 기존 법보 비율 그대로 / 탭의 강화 화면·지급 칩·친밀 최대(캡처 `check-treasure-forge.png`) | `service` 뒤 아무 때나(새 sole을 번호로 찾는다). 05 문서 82절 |
| (법보 수리) `treasure-check.ps1`의 `repair` → `-Restart` → `repair-restart` | `trdur`로 내구를 낮추고 `trrepair`·탭의 수리·모두 수리로 채움(60절), 저장 후 유지 | `repair`가 끝에서 저장한다 |
| (대도까지 채우기) `mastery-check.ps1`의 `service`, `ui`, `persist` → `-Restart` → `persist-restart` → `battle` | 끼워 넣기·다시 만들기·후보·거부·자리 밖 옛 옵션·만들기와 장착 실패 되돌리기·장착 심법 수도 점수·NPC / 공법 탭 화면 / 저장 후 유지 / 전투에서 쓰기와 정보 창 11/11 캡처 | 부분마다 깨끗한 회차(`persist`·`battle`은 한 회차). 05 문서 83절 |
| (모드 기술 이름) `mastery-check.ps1`의 `modname` | 모드 기술 이름의 색 태그 제거(83.7절): 원문에는 태그가 있고 편집기 이름에는 `COLOR`가 없음, 캡처 `check-modname.png` | 만고신화가 실린 회차(`-Exclude` 없이) |
| (트레이너 3차) `trainer3-check.ps1`의 `suit`, `suit-ui`, `realize`, `persist` → `-Restart` → `persist-restart` | 전승 서비스·NPC·다른 글 거부 / 공법 탭 전승 화면 / 참오 결과 최대(최대 품질 3·5·6 기술, 끄면 원래 굴림, 품질 단추 영향 없음) / 저장 후 유지 | `realize`는 숙련·재료를 채우고 게임 참오 창을 연다. 05 문서 82절 |
| (신기) `artifact-check.ps1`의 `list`·`godeye`·`potmon` / `ui`·`game` / `persist` → `-Restart` → `restart` | 서비스(목록, 점수, 손상, 해금·장착, 호요), 신기 탭 화면, 게임 화면 캡처, 저장 후 유지와 지도 버튼 | `/`마다 깨끗한 회차. 부분마다 고정본 상태(쌍어패만, 호요 0)를 가정한다. 개발 명령은 05 문서 37절 |
| (신기 화면·키) `artifact-check.ps1`의 `hud-one`·`hud`·`hud-battle`·`hud-battle-slots` / `keys`·`keys-ui` → `-Restart` → `keys-restart` / `slot-clicks`·`typing` | 지도 HUD의 신기 단추 자리(쌍어패만 −240,−93), 호천안 화면 닫기, 전투를 다녀온 뒤 다시 나란히, 전투 화면의 신기 칸(둘 이상이면 위로 60씩, 55절) / 신기별 단축키와 게임 신기 키 끄기, 저장 후 유지 / 전투 칸의 실제 클릭 경로(`brclick`), 입력 칸에 글자를 치는 동안 게임 단축키(B)가 반응하지 않음 | 부분마다 고정본 상태. 키는 화면 밖 게임에 전달될 때만 판정(안 되면 INFO). `keys-restart`는 `keys` 바로 뒤(8.0절) |
| `fate-dupe`, `import-swap` | 같은 역천개명 두 줄, 선천운명의 계열, 캐릭터 가져오기의 역천개명 맞바꿈 | `fate-dupe`는 깨끗한 회차의 첫 검사로. `import-swap`은 기록을 바꾸므로 그 회차의 마지막에 |
| `apply` → `apply-full` → `apply-battle` → `-Restart` → `apply-restart` | 단계 적용, 최종 단계까지, 전투에서 세트와 명우, 저장 후 유지 | 깨끗한 회차에서 이 순서로 |
| `ui` | 프리셋 탭의 화면(목록, 상세보기, 확인 문구, 쓸 수 없는 줄) | 깨끗한 회차에서, **그 회차의 마지막에**(4단계 잠김은 고정본 경지를 전제하고, 단계를 실제로 적용해 장착·게임 창이 바뀐다. 93절) |
| `preset-all` | 기본 프리셋(2026-10-20부터 17개: 01~16과 21)을 차례로 최종 단계까지 적용(못 넣음 0, 마지막 단계가 적용됨). 메타 `requires`의 모드가 실리지 않았으면 그 프리셋은 건너뛴다(INFO) | 깨끗한 회차에서. 하나에 8~17초라 `-Range 1-9`·`-Range 10-21`로 나눠 돌린다(각 4분쯤, 70·73절). 끝에 인물이 등선이라 선술 얻기 창이 뜰 수 있어 그 회차에서 다른 화면 검사를 잇지 않는다 |

한 번에 다 돌리면 10분을 넘는다. 회차를 나눈다(예: 보관함 계열 / 그 밖의 화면 / 날짜·전투 / 프리셋은 위 표의 줄마다 한 회차).

### 회차 안에서 쓰는 도구

- `mod.ps1 -Cmd "<명령>"` — 모드의 개발 명령. `editor`가 단축키 없이 편집기를 연다. 목록은 `Dev\DevCommands*.cs`.
  - 답은 `-ReplyMs`(기본 8000)까지만 기다리고, `-WaitMs`는 답을 받은 **뒤에** 쉬는 시간이다. 8초를 넘는 명령(프리셋 `papply … full`)은 `-ReplyMs`를 늘린다. 안 늘리면 모드는 끝까지 실행하고 로그에 답을 남기는데 결과는 `NO RESPONSE`로 나온다(70절). 검사 스크립트에서는 `Cmd <명령> <쉼> <답 기다림>`.
  - 화면: `uipos <이름>`(가운데의 게임 좌표. 이름 끝의 `*`는 "이 글자로 시작"), `uitext <이름>`, `uidescs <이름의 앞머리>`(그 이름으로 시작하는 글들을 한 번에), `uistate`(창의 상태와 결과 문구), `uitoggle`, `ui set <입력 칸> <값>`, `uifocus`, `uidrag`, `uiopen`·`uiclose <게임 화면>`, `uilist`(열려 있는 게임 화면), `uitarget`, `uiraise`, `uicollapse`.
  - 창 위 입력(51절): `mapcam`(지도 카메라 배율·위치·eventMask, 격자 호버 UI, 마우스 위치, EventSystem 판정, 패치 상태), `colliders`(카메라와 충돌체·`ColliderHandler`), 가상 입력 `uipointer <x> <y>|off`·`uiwheel <delta> [프레임]`·`uikey <KeyCode 번호> [프레임]`(0이면 풂), `inwatch start|read|stop`(프레임별 휠·위치·판정 모음). 게임 메서드의 네이티브 코드 해독 `xrefmap`·`xrefl <형식> <메서드> [깊이]`·`xrefd <형식> <메서드> <이름 조각> [앞] [뒤]`(`Dev/DevCodeScan.cs`, Iced). MelonLoader의 `XrefScanner`는 쓰지 않는다(초기화 실패, `UsedBy`는 게임 종료).
  - 값: `read`, `count <아이템 번호>`, `stats`, `luck`, `cstate`(경지·수련치·능력치·공법 수 등을 한 줄에), `cmartial <기술 번호>`, `cfatelog`.
  - 일부러 바꾸기: `setgrade <번호>`, `addexp`, `cattr <key> <값>`, `mdel <공법 고유 번호>`, `cartdel <서적 번호>`, `cborndel <운명 번호>`, `luckdel`, `cfatealt <대경지>`, `cluck3`, `gridfill`, `bag <증감>`, `save`.
  - 배율·전투: `rstate`(배율의 값과 패치 여부, `Time.timeScale`), `rnpc`, `rneg`, `bcast <1|2|3>`(전투에서 무예·절기·신법 발동), `rmart`(장착 기술의 숙련), `bend [lose]`(전투를 승리나 패배로 끝냄. 결과 창은 따로 닫는다), `rpath <dx> <dy>`(게임의 경로 이동으로 여러 칸), `rpause on|off`, `bdmg`(피해 배율이 실제 타격에 걸렸는지, 적의 체력 감소, 받는 피해), `cdstat`(쿨타임 속도와 기술별 쿨타임). 트레이너 4차(87절): `cdall`(선법·영역·전투 아이템 칸·명우까지), `icast <i>`·`fcast`(선법·영역 발동), `iequip <카드> <칸>`·`fieldset <main1> <dp>`(검사 준비), `bhp`(일격 대상의 죽음, 적 수, 플레이어 hp), `artdur`(법보 내구와 막은 수). `fight <부본> <수준>`의 수준은 고정본 인물에게 20까지가 안전하다(30부터는 쓰러진다).
  - 리뷰 탐침(13 문서 2절, 2026-10-29. 개발 빌드만): `tswatch start|read|stop`(매 프레임 `Time.timeScale`과 `SceneType.battle`·`battleMap`·`playerUnitCtrl`·`SceneType.map`의 유무를 보고 바뀐 프레임만 적는다. `loadingFrames`는 battle은 있고 playerUnitCtrl은 없는 프레임 수), `iwatch <i>|read|stop`(선법 i의 `cdTime`·`coolTime`·`cool`·`cast`·`isUse`·`energy`·`useCount`와 `IsCreate(false)`를 매 프레임 보고 바뀐 프레임만), `npcart`(전투의 플레이어 아닌 단위마다 종류·hp·법보 sole·내구. 내구 접두가 플레이어 것이 아닌 법보의 줄어드는 쓰기를 본 수 `otherDecrease`와 마지막 값). `Dev/DevCommandsProbe1029.cs`.
  - 선법: `ilist`, `icount <선인> <종류> [검색어]`, `ilearn <카드>`, `ilearnall <선인> <종류>`, `ihas <카드>`, `inoname`, `ifit <요소 이름 앞머리>`(칸보다 높아 잘리는 글의 수). 실험용 `snap <이름>`(인물 데이터를 통째로 파일에), `imm`, `immset <값>`, `upreal`(재료를 넣은 정식 돌파).
  - 법보(56절): `tr`(법보·기령 상태 한 줄), `trgive <아이템>`, `trequip <sole>`, `trunequip [sole]`, `trmake <기령 번호>`, `trput <법보 sole> <기령 sole>`, `trpull <법보 sole>`, `trlight`·`trlevel`·`trdel <기령 sole>`. 기령의 `sole`은 음수일 수 있다.
  - 신기: `aw`, `awset <번호,…>`, `awbtn`, `aon`·`aoff <번호>`, `apoint <값>`, `gedmg`, `gestate`, `gerepair`, `gecat`, `geunlock <요수>`, `geunlockall`, `geboss <요수>`, `geequip <기술>`, `ddmg <0|1|2|3>`, `ddstate`, `ddrepair`, `potlist`, `potadd <종류> <단계>`, `potfill <soleId>`(05 문서 37절). 설정 표를 번호로 찾는 `confname <번호>`는 실험 브랜치 `feat/artifact-research`에만 있다.
  - 프리셋: `pstate`(경지·수도 점수·장착·세트·기령·신혼·명우·역천개명을 한 줄에), `phas <운명 번호,…>`(실제로 가진 수), `pcheck [파일]`(쓸 수 없는 줄), `papply <파일> <단계|all|full>`, `pstage <파일> <단계>`, `pfateset <대경지> <번호>`, `pluckadd <번호>`, 찾기 `pfind`·`pfate`·`pluck`·`psuits`·`pconf`·`popts <기술 번호>`, 표 내보내기 `pdump <fates|skills|opts|g.conf의 표 이름>`(`.devremote\dump-<이름>.txt`). 전체 목록은 05 문서 34절.
  - 날짜·가방: `wdate`, `grid`, `wmove`, `wexp`, `tdesc`, `tadv`, `tset`, `tskip`, `tfreeze`, `tin`, `tmsg`, `thud`, `bagstat`.
  - 자원 탭: `rgroups`(전체·기타·중복·합과 그룹별 수, 표에 없는 분류 번호), `rfav [reset|reload|add <번호> <수량>|set <번호> <수량>|del <번호>|addbook <분류> <기술> <경지> <최소 품질> <수량>|setbook <기술> <경지> <최소 품질> <수량>|delbook <기술> <경지> <최소 품질>]`(즐겨찾기 상태. `reset`은 파일을 지우고 다시 읽게 한다), `rbook <분류> <기술> <경지> <최소 품질> <권수>`(비급 만들기), `rbooks [기술]`(가방의 비급), `rbookstudy <soleID>`(게임 학습이 받아들이는지), `rskill <분류> <기술>`(경지 범위), `rownskills <분류> <경지>`(그 기술 하나만 나오는 묶음이 있는 기술). 분류는 0 무예/영술 ~ 4 신통. 모드 공법: `rmods [경지]`(카탈로그의 모드 항목: 분류:번호:이름:basType:NameFromCh:막힘 이유:원본. 그 경지 기준, 기본 3), `rlearn <분류> <번호> <경지>`(공법 탭 배우기의 문장. 05 문서 42절).
  - 도호 탭: `ttl`(가진 것·장착·오성·획득 창·편집기가 닫은 창 수), `ttlgive|ttldel|ttlequip <번호>`, `ttlunequip`, `ttlholders <번호>`, `ttlnpcgive <unitID|any> <번호>`(검사 준비), `ttlrewardopen`·`ttlrewardclose`(일반 보상 창 열고 닫기).
  - 창작마당: `wsmods`(`g.mod`의 경로, 로드 목록 `paths`(모드 ID|폴더), 모드 자료 `mods`(모드 ID:이름:판:창작마당 번호), DLL 경로 `ns`, 순서 자료 `order`. 05 문서 41절). 회차 안에서 모드를 넣을 때는 `pwsh tools/workshop.ps1 -Install -Yes -NoHistory`(이력에 남기지 않는다. 45절).
  - 한글화: `rloc`(마지막 적용 결과 `file=… rows=… applied=… fallback=… unused=… broken=…`), `rloc apply`(지금 파일로 다시), `rloc dump`(사전에 없는 모드 행을 `translations\untranslated.txt`에 키<TAB><TAB>중국어로), `rloc key <키>`(`ls=… kr=… ch=…`). 05 문서 43절. LocalText 밖의 글(54절): `rzhtext <원문…>`(번역표로 바꿔 봄. `\n` 두 글자는 줄바꿈), `rzhstat`(표 적용 요약 `file= rows= lt= tables= cells=`과 화면 글 치환 통계 `patched= calls= replaced= ms=`), `rzhleft`(치환 뒤에도 한자가 남은 화면 글을 `.devremote\zh-left.txt`에. 번역표에 더할 원문을 받는 길), `rzhapply`(번역표를 다시 읽어 표 칸 다시 적용), `rzh [표 이름|all] [최대 줄]`(설정 표의 한자 칸을 `.devremote\zh-<표>.txt`로. 53절).
  - 입화(52절): `rdress <dressID>`, `rbody`·`rbodyq`·`rbodyr`(몸 그림 자산과 Fatury의 비동기 적재 탐침), `rbodyof <unitID>`, `rprops`, `rsetbody <번호>`, `ropenui`·`rcloseui <UIType 이름>`, `rnpcinfo`, `ruigrid <스크롤 이름> [all]`(목록 칸의 이름과 그림. 캐릭터 만들기 의복 목록 확인).
- `rc.ps1 -Cmd "click x y" -NoShot` / `"hit x y"` / `"scroll x y delta"` — 게임 좌표(1920x1080)의 UI 요소를 누르거나 이름을 확인한다.
- `win.ps1 -Do shot -Out <파일> -Scale 0.6` — 창 캡처. 캡처 좌표에서 게임 좌표로: `(x / Scale - 8, y / Scale - 31)`.
- `win.ps1 -Do key -Vk 0x2D` — Insert 키. `-Do move -X -Y`·`-Do wheel -X -Y -Delta`(WM_MOUSEMOVE·WM_MOUSEWHEEL)도 있으나 Unity의 `Input`에는 닿지 않는다(51절 실측). 마우스는 개발 명령의 가상 입력(`uipointer`·`uiwheel`·`uikey`)으로 한다. 모드의 단축키는 전달되지만 게임 자체의 단축키(ESC, 달 넘기기 `Z`)는 전달되지 않았다. 입력 칸을 골라 입력 상태로 만든 뒤에는 글자 키(`0x41` 등)와 Enter(`0x0D`)가 그 칸에 들어간다.
- 검증 스크립트 안에서는 `check-lib.ps1`의 `Cmd`, `Rc`, `Press <이름>`(찾아서 누름), `IsAt <이름>`(그 자리의 클릭이 그 요소로 가는지), `Pos`, `Check`, `Shot`, `Traps`, `ModWarnings`를 쓴다. 2026-10-30(05 문서 91절)부터 공통 도우미가 더 있다: `EnterBattleScene [수준] [-NoStart]`·`LeaveBattleScene [캡처 이름]`(전투 진입·종료. 아래 8.0절의 규칙이 들어 있다 — 검사에 전투 코드를 새로 복붙하지 않는다), `LogHasLine <패턴>`(변경 이력 마지막 40줄), `ButtonIsOff <이름>`, `UiText`·`UiHas <이름>`, `NumIn <글> <패턴>`, `FieldsOf <글>`(`k=v` 표), `OpenEditorTab <번호>`. 편집기의 버튼은 좌표가 아니라 이름으로 찾는다.
- `.devremote`(회차 표식·세이브 사본·캡처·명령 채널)는 어느 체크아웃에서 열어도 한 곳이다: 환경 변수 `TLTOYBOX_DEVREMOTE`(절대 경로)가 있으면 그 폴더, 없으면 `E:\TlToybox\.devremote`(49절, `tools/DevRemote/devremote-dir.ps1`). 워크트리에서 회차를 열어도 같은 곳을 쓰고, 두 체크아웃이 동시에 열면 표식이 막는다. 설치본의 `Init` 줄 `build=<커밋>`으로 어느 코드가 설치됐는지 안다.
- 게임 없이 도는 시험(2026-10-21에 센 수): `pwsh tools/DevRemote/session.tests.ps1`(51, 2026-10-30), `pwsh tools/workshop.tests.ps1`(81), `pwsh tools/DevRemote/channel.tests.ps1`(7), `pwsh tools/DevRemote/devremote-dir.tests.ps1`(12), `pwsh tools/build-tag.tests.ps1`(4, 배포 빌드 두 번), `dotnet run --project tests/ModMain.Tests -c Release`(2026-10-27에 `KoreanText` 6줄 더함). 스크립트나 순수 논리를 고치면 해당 시험을 다시 돌린다.

게임 화면의 자주 쓰는 좌표: HUD의 날짜 버튼(달 넘기기) `(400, 1000)`, 확인 창의 예 `(884, 531)`·아니오 `(1037, 531)`, 월말 기록 창의 다음 `(959, 629)`, 가방 버튼 `(120, 860)`, 인물 화면 닫기 `(1722, 160)`, 게임 메뉴의 돌아가기 `(959, 391)`, 전투 개시 `(956, 749)`, 전투 결과 창의 확인 `(959, 831)`. 편집기가 기본 위치(60,60)일 때 탭 줄은 x=151, `Tab0`이 y=144이고 탭마다 46씩 내려간다(`Tab15` y=742. 2는 비었다). 안전 단추 `Tab14`는 제목 줄 (1312,86). 편집기의 단추는 좌표가 아니라 이름으로 찾는다(47절).

### 조사 도구 (`tools/research/`)

- `dump-types.ps1 -Game <게임폴더> -Out types.txt` — 어셈블리의 형식·속성·메서드 목록을 만든다(`types.txt`는 git 제외, 현재 복사본이 들어 있다).
- `q.ps1 -Show <형식이름>` / `-Find <정규식>` — 그 목록에서 찾는다. 중첩 형식은 `DataProps/PropsData`처럼 `/`로 적는다. `-Find`는 형식 이름만 찾는다. 속성·필드 이름은 `types.txt`를 직접 검색한다.
- `methods.ps1 -TypeRegex <정규식> [-MethodRegex <정규식>]` — 생성자와 메서드의 매개변수를 본다(목록에는 생성자가 없다).
- `major-breakthrough-check.ps1 -Prev <경지ID> -Rows <방식 수> -Tag <이름>` — 대경지 재료 지급의 자동 검증(21절).
- 실험 브랜치의 개발 명령(병합하지 않음. `git checkout`해 빌드해야 쓸 수 있다): `feat/ultimate-research`의 `ulprefix <skillType> <기술>`·`ulroll <분류> <기술> <경지>`·`ulread <분류> <원본> <모드> <경지>`·`ulfix <분류> <원본> <모드> <경지> [add|learn]`(44절 조사: 비급 값의 모양, 옵션 번호 조회, 단계별 읽기와 예외 스택), `feat/modskill-research`의 `wsconf`·`wsname`·`wsbook`·`wsbookas`·`wslearn`·`wslearnas`·`wssetkr`(8.3e절), `feat/korean-research`의 `wsloc`·`wslocdump`(43절 설계 전 실험), `feat/perf-research`의 `rperf tabs`(탭마다 `SwitchTab` 시간)·`rperf parts`(자원 탭 그리기의 조각별 시간. 탭을 열기 전에 부르면 카탈로그 만들기가 들어간다)·`rperf skillbuild`(기술 카탈로그 만들기 나누기)·`rperf modroll [경지]`(모드 공법마다 비급 뽑기 시험, `.devremote\perf-modroll-<경지>.txt`. 563개에 98초)·`rperf modrows`(모드 기술 행 덤프, `.devremote\perf-modrows.txt`)(12 문서).
- 게임 설정 표의 원본은 `E:\Games\Tale of Immortal\Mod\modFQA\配置修改教程\配置（只读）Json格式\*.json`. 이름은 같은 폴더의 `LocalText.json`(중국어·영어, 한국어 열은 대부분 비어 있음).

## 6. 반복해서 겪은 함정

검증:

- 검사가 이상하게 실패하면 `mod.ps1 -Cmd uilist`와 캡처로 **무엇이 떠 있는지부터** 본다. 게임 창(월말 기록, 대화 사건, 게임 메뉴)이 떠 있으면 편집기를 가리고 키 입력을 가져간다. 그 창을 닫으면 게임이 다음 사건(대화, 전투)으로 넘어갈 수 있다.
- 실패의 원인을 세이브 탓으로 돌리기 전에 고정본에서 다시 돌려 본다. 고정본에서도 실패하면 코드나 검사의 문제다.
- 검사는 앞 검사가 남긴 화면 상태를 물려받는다. 보관함은 마지막에 본 쪽(가방/캐릭터)과 이름 칸의 글을 기억하고, 창 위치는 `config.txt`에 남는다. 검사는 자기가 필요한 상태를 직접 만들고 시작한다(`vault`는 `가방`을 누르고 이름 칸을 비운다).
- **회차와 검사는 한 번에 하나만 돌린다.** 오래 걸리는 검사를 백그라운드로 돌려 놓고 끊긴 줄 알고 다른 회차를 열어 두 실행이 같은 게임을 건드렸고, 실패 여럿이 그 때문이었다(32절). 도구의 한 번 실행은 10분이 한계이므로 회차를 그 안에 끝나게 나눈다(`-Begin` + 검사 서너 부분 + `-End`).
- **사용자가 말한 대상을 게임 화면이나 사용자의 캡처로 먼저 확정한다.** "시작 신기 셋"을 설정 표 이름만 보고 특수 선천운명 셋(식혼검·음양안·연화검의)으로 잘못 보고했다가, 사용자가 보낸 신기 선택 화면(호천안·쌍어패·연요호)으로 바로잡았다. 게임의 열거형(`GameAnimaWeapon`)처럼 게임이 직접 묶어 둔 목록을 먼저 찾는다.
- 사용자의 게임이 켜져 있으면 `session.ps1 -Begin`이 시작 전에 멈춘다(세이브는 건드리지 않는다). 그 뒤 `mod.ps1`을 부르면 `.devremote\mod-cmd.txt`가 남으니 지운다(개발 빌드 회차가 열리면 그 명령이 실행된다).
- **기록만 보는 검사는 실제 보유를 놓친다.** 프리셋의 역천개명 검사가 경지별 기록만 봐서, 아홉을 적용했는데 운명은 넷만 붙어 있는 것을 놓쳤다(같은 계열을 게임이 지웠다. 34절). 검토자가 찾았다. 게임이 여러 곳에 적는 값은 효과가 나는 쪽(운명의 보유 `phas`)을 본다.
- 검사 스크립트 안에서 `check-lib.ps1`의 변수(`$log`, `$data`, `$work`)와 같은 이름의 변수를 만들지 않는다. `$log`를 덮어써 `Traps`가 깨진 적이 있다.
- 모드가 `[TlToybox] ` 로그 줄을 새로 내면 `check-lib.ps1`의 `ModWarnings` 제외 목록에 넣는다. 54절의 `texts:` 줄이 모든 검사의 `no warning from the mod`를 실패시켰다(55절).
- 검사는 앞 실행이 남긴 자료를 물려받는다. `Press`는 꺼진 단추를 누르면 예외로 멈추므로(법보 탭 `ui`: 앞 실행이 만든 기령의 `만들기`), 자료를 만드는 검사는 새 회차에서 돌리거나 다른 번호를 쓴다. 하위 화면의 검색어(`kindQuery` 등)는 탭이 기억하므로 검사가 먼저 맞춘다(56절).
- 개발 명령이 `NO RESPONSE`면 `.devremote\mod-cmd.txt`가 남아 다음 명령이 "파일이 이미 있으므로"로 막힌다. 지우고 다시 보낸다. 전투 불러오기 화면에서 게임이 멈추면(55절, 한 번) 폴링도 멈춘다. 게임 창이 응답하면 `-End`의 창 닫기로 끝난다.
- `session.ps1 -End`의 출력을 `tail`로 자르면 `SAVE RESTORED AND IDENTICAL` 줄이 잘려 성공을 확인하지 못한다(회차 `perf-1022a`. 세이브 파일 48개를 복사본과 해시로 대조해 같음을 확인했다). `grep SAVE`로 그 줄을 골라 본다. 회차 도중에는 설치된 DLL을 바꿀 수 없으므로, 측정 명령을 더하려면 `-End` 뒤 새 이름으로 다시 연다.
- Bash에서 받은 개발 명령의 한글 출력(탭 제목 등)은 깨져 보인다(콘솔 코드 페이지). 번호로 읽거나 `.devremote`에 UTF-8 파일로 쓰게 한다. 오래 걸리는 개발 명령(모드 공법 563개 뽑기 98초)은 그동안 게임 프레임을 멈추므로 `-ReplyMs`를 넉넉히 준다.
- 화면 밖 게임에 보낸 Enter 키가 한 회차 내내 하나도 닿지 않은 적이 있다(`fu-1017a`: `favorites`의 Enter 검사 넷과 `numbers` 둘 실패, 같은 검사가 `fu-1017b`에서는 모두 통과). 키에 기대는 검사의 실패는 새 회차에서 다시 본 뒤 판정한다.
- `session.ps1 -Begin`은 스스로 개발 빌드를 만든다. "고치기 전의 DLL로 검사가 실패하는 것을 본다"를 하려면 코드를 고치기 전에 회차를 열어야 한다(프리셋 열 개의 검사는 이 때문에 실패를 보지 못했다).
- 게임 행동을 흉내 내는 기능(경지 올리기 등)은 같은 구간을 정식 행동으로 지난 결과와 통째로 비교한다(개발 명령 `snap`. 35절). 능력치만 맞춰 놓고 선력·선법·임무·칭호를 놓친 적이 있다.
- 검사가 통과해도 **버튼과 표시만 본 것인지 실제 효과를 본 것인지** 구별한다. 피해 배율은 버튼과 화면 위쪽 표시만 검사하고 있었고, 사용자가 "적용되지 않는다"고 알린 뒤에야 적의 체력 감소를 보는 검사를 만들었다(33절). 새 기능의 검사는 게임의 값이 바뀌는 것까지 본다.
- 전투를 연달아 열 때는 결과 창(`BattleEnd`)이 뜨고 확인을 누른 뒤 `MapMain`이 돌아오고 몇 초 지난 다음에 다음 전투를 연다(`ui-check.ps1`의 `combat` 부분의 `Leave`). 서두르면 화면이 비고 열린 게임 화면이 하나도 없는 상태가 된다.
- 고정본 인물은 `fight 1011 <수준>`의 수준 30부터 한 방에 쓰러지고, 쓰러지면 대화 사건(`DramaDialogue`)이 떠 뒤의 검사가 꼬인다. 맞아야 하는 검사는 수준 20, 오래 때려야 하는 검사는 수준 60에 무적을 켜고 한다.
- 화면 밖의 게임에서는 클릭으로 공격·이동이 되지 않는다. 기술은 `bcast`, 경로 이동은 `rpath`, 전투의 끝은 `bend`로 한다. 화면에 뜨는 피해 숫자는 캡처에 찍히지 않는다.
- `datebag-check.ps1 -Part freeze` 뒤 같은 회차에서는 전투 진입 검사가 실패한다(세 번 재현, 원인 미확인. 날짜 고정을 켠 채 깨끗한 상태에서 전투에 들어가는 것은 된다).
- 숫자 칸의 Enter가 한 회차 내내 반영되지 않은 적이 있다(신기 탭 `ui`, 새 회차 여섯 번 중 두 번. 진단 회차에서는 9/9 반영, 원인 미확인). `artifact-check.ps1`의 `EnterPoint`처럼 반영됐는지 읽어 보고 한 번 다시 보내며 INFO를 남긴다.
- 검사 부분은 고정본의 처음 상태를 가정하는 것이 많다. 같은 회차에서 다른 부분이 자료를 바꿔 두면(예: `ui`가 넣은 호요를 `potmon`이 물려받음) 실패한다. 실패하면 그 부분만 새 회차에서 다시 돌려 본다.
- 화면 밖의 게임에 잇달아 보낸 키는 빠질 때가 있다. 키 한 번의 전달은 믿을 수 있지만, 여러 번 누르는 판정은 개발 명령(`uitoggle`)으로 한다.
- 시험 중에 게임 메뉴(`GameMemu`: 돌아가기·저장…)가 떠서 편집기를 가릴 때가 있다(게임 창의 포커스 변화로 추정). `check-lib.ps1`의 `Press`가 닫고 다시 누른다.
- 도호를 주거나 빼면 근처 NPC의 반응 대화(`DramaDialogue`)가 편집기 위에 떠 다음 누르기를 가져갈 수 있다. 검사는 `SkipDialogue`로 넘긴다(40절). 게임 메뉴가 거듭 열린 회차(`game menu was open`)의 실패는 새 회차에서 다시 돌려 가린다(`ttl-1015i`→`k`).
- 문서를 스크립트로 고칠 때: Bash 히어독 안의 Python 글에 역슬래시 두 개를 쓰면 하나로 풀려, Windows 경로의 역슬래시 뒤 글자가 이스케이프(백스페이스, 8진수)로 바뀌어 문서가 깨진다. 경로의 역슬래시는 `chr(92)`로 만들거나 Edit 도구로 쓰고, 고친 뒤 제어 문자와 경로를 grep으로 확인한다.
- 개발 명령 `save` 직후 바로 `-Restart`하면 바꾼 값이 남지 않을 수 있다(신기 조사에서 한 번). 저장이 끝나도록 몇 초 기다린 뒤 재시작한다(07 문서 7.2절).
- 게임 화면을 닫는 명령 뒤에 고정 시간으로 기다리지 않는다. 값이 바뀔 때까지 기다린다(`ui-check.ps1`의 `GameScreen`, `datebag-check.ps1`의 `WaitRun`).
- 세이브 폴더 밖의 파일(`changes.log`, `bag-slots.txt`, `config.txt`, `exports\`)은 세이브를 되돌려도 남는다. 이력 검사는 그 실행에서 새로 생긴 줄만 본다.
- 개발 명령의 출력은 `<명령> -> `로 시작한다. 줄 단위로 나눠 `^이름`으로 고르면 첫 줄이 빠진다. 머리를 떼고 나눈다.
- 검사에 같은 문구가 두 번 있을 때 문자열 치환으로 고치면 둘 다 바뀐다(`vault`와 `charvault`가 닮았다).
- `session.ps1 -Restart`는 빌드하지 않는다(설치된 개발 DLL 그대로 다시 띄움). 코드를 고친 뒤에는 `-End` → 새 이름으로 `-Begin`(74절에서 두 번 헛돌았다). `-Restart`에 `-Login`을 안 붙이면 세계까지 들어간다.
- 개발 명령은 `mod.ps1 '<명령>'`으로 보낸다. `rc.ps1 -Cmd`는 조작 도구(DevRemote)의 `hit`·`click`·`scroll`만 알고 나머지에는 `UNKNOWN <명령>`을 돌려준다(74절).
- 캐릭터 만들기 의복 목록의 칸 번호("211", "의복211")는 dressID가 아니라 목록의 순서다. 사용자가 말한 번호는 `ruigrid srItems all`의 칸 순서로 dressID로 바꿔 본다(74절).
- "골라도 같은 그림"이면 코드를 의심하기 전에 모드의 그림 파일이 서로 같은지(해시) 본다. [剑来] 2818763591은 14장이 같은 파일이었다(74절). Fatury `asset.yml`의 `template`·`expireTime`은 원인이 아니었다.
- 목록 스크롤(`scroll 460 914 ±n`)은 관성으로 몇 초 더 움직인다. `hit`으로 칸을 찾을 때는 3초 넘게 기다린 뒤 읽는다. 같은 dressID가 두 칸인 번호가 있어 칸 이름만으로 위치를 셈하면 엇나간다(74절).
- `MelonLoader\Latest.log`의 한글은 UTF-8이 아니라 CP949다. 로그에서 한글 문구를 찾는 검사는 아무것도 잡지 못한다. 모드의 경고는 줄의 모양으로 고른다(`ModWarnings`). 도구의 출력에서 한글이 깨져 보이는 것도 같은 이유다(판정은 영문 표지와 숫자로 한다).

PowerShell과 도구:

- 변수 이름의 대소문자를 구분하지 않는다. `$T`와 `$t`를 한 스크립트에 쓰면 덮어쓴다. 비교 `-eq`도 글자의 대소문자를 구분하지 않는다(`'restart' -eq 'RESTART'`가 참). 구분하려면 `-ceq`.
- 큰따옴표 글 안의 `"$p: …"`는 드라이브(범위) 표기로 읽혀 구문 오류가 난다. `"$($p): …"`나 `"${p}: …"`로 쓴다(`treasure-check.ps1`의 `"\[${sole}:"`, 56절).
- 정규식 치환을 `sed`로 하면 역슬래시·괄호 이스케이프가 꼬인다. PowerShell의 `[IO.File]::ReadAllText` → `-replace`(작은따옴표 패턴) → `WriteAllText(UTF8 BOM 없음)`이 안전하다.
- `mod.ps1`·`rc.ps1`은 배열 매개변수가 있어 `pwsh -File`로 부르면 인수가 어긋난다. `&`로 부른다.
- 큰따옴표 글 안에서 변수 바로 뒤에 한글이 오면 변수 이름의 일부로 읽힌다(`"$total개"`). `"$($total)개"`로 쓴다.
- `if` 식이 빈 배열을 돌려주면 `$null`이 된다. `@(if (...) { ... })`로 감싼다.
- 함수 이름이 별칭과 겹치면 별칭이 먼저 실행된다(`R`, `History`, `Erase` 등). 새 함수를 만들면 `Get-Alias <이름>`으로 확인한다. 이번에도 `R`로 문서 치환 함수를 만들었다가 아무것도 바뀌지 않았다.
- 여러 줄의 코드·문서 치환은 Bash의 `sed`나 `pwsh -Command '…'` 한 줄에 넣지 않는다(따옴표와 한글이 깨진다). 치환 스크립트를 Write 도구로 파일로 만들어 `pwsh -File`로 돌린다. 찾는 글이 없으면 멈추게 한다.
- 명령 문자열에 삭제 명령의 이름이 들어 있으면 안전 검사가 명령 전체를 막는 경우가 있다. 파일 삭제는 `[IO.File]::Delete`나 Bash의 `rm`을 쓴다.
- Bash의 here-doc에 긴 한글 문서나 작은따옴표가 든 C# 코드를 넣으면 인용이 깨진다. 긴 내용은 Write 도구로 파일을 만든다.
- git이 줄바꿈을 CRLF로 바꾼다. 여러 줄 치환을 하기 전에 `\r`을 지우고, 쓸 때 되돌린다.
- 히어독 안 Python 글의 `\\t`·`\\n`도 하나로 풀려 탭·줄바꿈이 된다(74절 설치 기록의 `translations\tables.txt`가 탭으로 들어갔다). Windows 경로가 든 글은 Write 도구로 스크립트 파일을 만들어 돌린다.
- `Get-ItemProperty`로 게임 레지스트리 키 전체를 읽으면 Int64 값에서 "Unable to cast" 오류가 줄줄이 난다. 한 값만 `reg query <키> /v <이름>`으로 읽고 `reg add … /t REG_DWORD`로 쓴다(74절).
- `python` 명령은 Windows 스토어 별칭이라 실행되지 않는다. 실제 Python은 `py -3`(3.14)이다. 게임 설정 표를 세는 일 같은 것은 `py -3 -I -X utf8 <스크립트>`로 하고 출력은 파일로 받는다(콘솔은 CP949라 한자에서 멈춘다). 저장소의 스크립트는 지금처럼 PowerShell로 쓴다.
- PowerShell의 `-match`는 대소문자를 가리지 않는다. `brother=False`가 `Brother`에 맞아 검사가 거짓으로 통과한 적이 있다(68절). 대소문자가 뜻을 가르면 `-cmatch`.
- 함수 이름 `Mp`도 별칭(Move-ItemProperty)이다. 검사 `mpnone`이 이것으로 멈췄다(66절).
- 정적 필드는 적힌 순서로 초기화된다. `RateService.All`보다 뒤에 새 줄을 선언하면 `All`에 null이 들어가 배율 탭이 통째로 그려지지 않는다(66절).

게임:

- 언홀로어가 칸(필드)을 감싼 속성(`get_dropRate`, `get_addIntim` 등)에는 Harmony 패치가 걸리지 않는다. 계산 속성(`ConfNpcFeedback104Base.get_requireScoreBrother` 같은 설정 표의 값)은 걸린다(67절). `AccessTools.Method(형식, 이름)`은 그 형식에 없으면 부모의 것을 찾아 걸므로 `DeclaringType`을 확인한다.
- **Vector2 같은 값 형식 인자를 받는 게임 메서드(`PropItemPill.OnCreate` 등)에 Harmony 패치를 걸면 그 순간 게임이 꺼진다**(il2cpp 내부, `Player.log`의 `Crash!!!`. 65절).
- `DataProps.DelProps(번호, 수)`는 가방 순서대로 지워 **장착한 법보부터** 지우고 장착 칸에서도 뺀다. 수를 줄일 때는 sole 단위로 지운다(`BagPlan`, 65·66절). `GetPropsNum`과 `GetAllEquipPropsSoleID`는 장착품(장비와 전투 아이템 칸의 단약 묶음)을 포함한다.
- 창 밖의 게임에 보낸 마우스 메시지(WM_MOUSEMOVE·WM_MOUSEWHEEL)는 Unity의 `Input`에 닿지 않는다(키 메시지는 닿는다). 마우스는 개발 명령의 가상 입력으로 흉내 낸다(51절).
- 게임 메서드의 호출 추적에 MelonLoader의 `XrefScanner`를 쓰지 않는다: `XrefScan`은 캐시 DB 초기화에 실패하고, `UsedBy`는 게임이 종료됐다(회차 fix-1017a). `xrefl`·`xrefd`(Iced 해독)를 쓴다. 같은 코드가 접힌 메서드는 다른 이름으로 나올 수 있다.
- Harmony 접두에서 원래 메서드의 매개변수를 이름으로 받으면 언홀로어가 붙인 이름과 다를 때 패치 전체가 실패한다. 자리 이름(`__0`)으로 받는다. 게임의 프레임 처리 안에서만 입력을 가로채려면 그 메서드의 접두·접미로 표시(`InMapUpdate`)를 두고 `Input` 패치가 그 표시를 본다(51절).
- IL2CPP 프록시:
  - 게임이 알아보지 못하는 값으로 만든 객체는 속성을 읽는 것만으로 예외가 난다(공법의 `martialInfo`). try/catch로 감싸고, 깨진 것은 바로 지운다.
  - 버튼 콜백 안의 예외는 화면에 나오지 않고 `MelonLoader\Latest.log`의 "trampoline" 줄에만 남는다. 버튼이 아무 반응이 없으면 그 로그를 본다.
  - 게임 행동(`CreateAction`)의 반환값 0이 "효과가 났다"는 뜻이 아니다. 항상 데이터를 다시 읽어 확인한다.
  - 게임 행동 가운데 `IsCreateAction`으로 먼저 물어야 동작하는 것이 있다. 법보 장착(`UnitActionPropEquip`)은 묻지 않고 `CreateAction`만 부르면 장착되지 않았다(34절).
- 신기 목록(`g.data.world.animaWeapons`)이 비면 게임이 불러올 때 시작 연출·신기 선택·튜토리얼 전투를 다시 튼다. 신기를 바꾸는 코드와 검사는 목록을 비우지 않는다(07 문서 7.2절).
- 실행 중의 관리자와 저장되는 세계 자료가 같은 값을 따로 가질 때가 있다(연요호의 손상: `g.world.devilDemonMgr.devilDemonData`와 `g.data.world.devilDemonData`, 호요: 추가하면 관리자의 `potMonDatas`에 바로 들어가고, 세계 자료의 `potMonList`는 그때 0이었다가 저장 후 다시 불러온 뒤에 채워져 있었다). 둘 다 쓰거나 실행 중의 쪽을 쓴다(37절).
- 역천개명은 계열(`FateFeature.group`, 0이 아님)마다 하나다. 같은 계열을 다른 대경지에 붙이면 게임이 앞의 운명을 지우고 기록만 남긴다. `FateService.HeldElsewhere`로 먼저 본다(34절).
- 같은 번호의 기령은 하나만 가질 수 있다(`AddSprite`가 null). 기령의 `soleID`는 음수일 수 있다(56절). 심법 세트는 장착한 종류가 아니라 비급마다의 세트 번호로 켜진다(34절).
- 설정 표의 `allConfList`는 `localText` 말고는 `IReadOnlyList<T>` 래퍼라 반사로 `Count`·`Item`을 못 찾는다(NullReferenceException). 표를 반사로 돌 때는 `_allConfList`를 쓴다(54절).
- Fatury 틀은 PlayerPrefs `Mod_LoadData`에 `isLoad:true`인 모드의 자산만 싣는다. 새 창작마당 모드를 넣으면 모드 창에서 저장하거나 이 값을 다시 써야 그림이 보인다(52절).
- 모드의 글: 종족 틀 DLL은 줄바꿈을 CRLF로 넣고 TMP가 두 글자 `\n`도 줄바꿈으로 그린다. 설명의 `{icon_…}`·`<y>…</y>`·`&…&`는 TMP 태그와 값으로 바뀐 뒤 `text` 설정자에 온다. 불러오기 화면 모드는 LocalText의 `ch`를 그대로 화면에 낸다(54절. 원문 기반 번역표는 이 셋을 모두 받게 되어 있다).
- 기본 공법 가운데 `battleSkillAttack.className`이 `"0"`이고 자기 묶음(`battleSkillCombine.baseID`)도 없는 것(멸신참·마인참·극풍인)은 NPC 전용이라 옵션 없이 만들어도 게임이 받지 않는다(55절).
- 게임의 행동은 요청한 것 말고 다른 데이터도 바꾼다. 운명 추가 행동(`UnitActionLuckAdd`)은 역천개명 분류의 운명을 지금 대경지의 역천개명 기록에도 적는다(30·31절). 가방의 `allProps`에는 영석과 화폐도 들어 있다(28절). 새 기능을 만들면 "바꾼 것"만이 아니라 "그대로여야 할 것"도 검사한다.
- 게임 화면의 표시 조건은 데이터 위치와 다를 수 있다(운명 17절, 역천개명 18절). "넣었다"와 "게임 화면에 나온다"는 따로 확인한다.
- 저장값은 `unit.data.unitData.propertyData`, 화면 값은 `unit.data.dynUnitData`의 `DynInt.value`. 수명·나이는 개월로 저장되고 년으로 표시된다.
- 경지: `SetGradeID`는 경지 번호와 수련치만 바꾼다. 능력치·수명은 따라오지 않는다(30절).
- 날짜: 플레이어의 행동은 `WorldUnitBase.AddResidueDay`로 날짜를 쓴다. `WorldRunMgr.AddDay`를 직접 부르거나 막으면 `residueDay`와 어긋나 달을 넘길 때 날짜가 튄다(25절). 예전 개발 명령 `day`(`Dev/DevCommandsStage4.cs`)는 아직 `AddDay`를 직접 부르므로 쓰지 않는다.
- 달력을 앞으로 옮기거나 날짜를 진행시키면 날짜 기준 사건(기연, NPC의 제안)이 뜬다.
- 게임을 닫은 직후에도 프로세스가 목록에 잠깐 남는다. 회차 스크립트가 기다린다.

## 7. 알려진 한계와 미확인 목록

기능별 한계는 05 문서 각 절 끝의 "확인하지 못한 것"에 있다. 그중 다음 작업에 영향이 큰 것:

| 항목 | 상태 | 기록 |
|---|---|---|
| 화신·오도(대경지 7·8)의 천재지보 | 지급은 되지만 게임의 `자동 투입`이 넣지 않는다. 직접 골라 넣는 것은 확인 못 함 | 21절 |
| 기술 수련·참오의 게임 행동 | `IsCreateAction`이 3(불가). 조건 미확인. 편집기는 숙련 경험치·옵션 편집으로 대신 | 22절 |
| 풍수서 강제 추가 | 낮은 단계와 높은 단계가 함께 남는다 | 24절 |
| 기술을 수십 개 익힌 상태의 부작용 | 장착 화면·전투 미확인 | 24절 |
| 역천개명 | 기록의 `quality` 뜻 미확인. 계열 규칙은 `FateFeature.group`으로 확인해 넣었다(34절). `mutexGroup`은 미검사. 화면에서 비우는 버튼은 없다. 운명 추가(분류 3)로 붙여 기록에 없는 역천개명과의 계열 충돌은 보지 않는다 | 19, 31, 34절 |
| 선천운명 | 개수 상한과 다른 게임 기능과의 관계 미확인. 캐릭터 가져오기는 "없는 것만 더하기"라 칸 수를 넘길 수 있다 | 17, 30절 |
| 경지를 직접 바꾼 인물 | 인물 화면의 표시와 저장 후 유지는 확인. 돌파가 따로 여는 것(해금, 기록)이 있는지, 23번 이상의 경지 번호는 미확인 | 30절 |
| 내보내기 파일을 다른 세이브·새 인물로 | 세이브가 하나뿐이라 같은 세이브 안에서만 확인 | 28, 30절 |
| 한글 입력기(IME), 실제 마우스 휠·막대 끌기, 제목 줄 끌기 | 도구로 시험할 수 없다(휠·오른쪽 끌기는 가상 입력으로만, 51절). 사용자에게 확인을 부탁했다 | 14, 26, 27, 51절 |
| 화면 크기가 1920x1080이 아닐 때 | 글 줄바꿈의 높이, 창 위치 미확인 | 29, 31절 |
| 날짜 고정 | 이동과 수련 밖의 행동(제작·채집 등)의 날짜 소모, 달 넘기기 단축키(`Z`) 미확인 | 25절 |
| 달력 직접 지정 | 지도 화면 밖에서의 사용 미확인. 앞으로 옮기면 날짜 기준 사건이 바로 일어날 수 있다 | 25절 |
| 가방 칸 | 수백 칸일 때의 가방 화면, 세이브가 여럿일 때 미확인 | 25절 |
| 배율 | 전투 속도의 체감과 타격 연출과의 충돌(값만 확인), 숙련이 오르지 않는 기술의 이유, 달을 넘길 때의 부하, 지형·탈것에 따른 이동 비용 | 32절 |
| 경지 올리기 | 대경지 9로의 정식 돌파(특수 전투)와는 비교하지 못했다. 돌파 임무의 대화 사건이 편집기를 가릴 수 있다 | 35절 |
| 프리셋 | 적용한 세이브로 오래 플레이했을 때, 신혼의 게임 화면 위치와 교체 때의 경험치, 명우의 효과·옵션 개수의 한도. 프리셋에 없는 기령·명우는 앞의 것이 남는다. 공략은 2022년 것이고 채운 줄은 작성자의 판단 | 34절, `specs/2026-10-14-preset-builds.md` |
| 신기 | 호천안 기술과 호요의 전투 사용, 천도비호의 발동, 요수 해금으로 오른 "요수 격파"가 호천안 돌파를 여는지, 호요 상한 28의 출처, 불러올 때마다 호천안 화면이 뜨는 원인, 쌍어옥패 점수의 게임 상한, 쌍어패 반쪽 주기의 정식 흐름 | 37절, 07 문서 9절 |
| 모드를 뺀 뒤의 세이브 동작 | 2026-10-27 확인: 불러오기·바꾼 값 유지는 된다. 가방 추가 칸은 편집기가 없으면 사라진다(아이템은 남아 90/64). 편집기 없이 한 NPC 대화의 선택지가 비어 멈춘 일 한 번(원인 미확인) | 84절 |
| 창 위 입력 막기 | 가상 입력으로만 검사. 실제 마우스의 휠·오른쪽 끌기·호버는 사용자 확인 | 51절 |
| LocalText 밖 글 한글화 | 새 모드의 글에 번역표에 없는 조각이 하나라도 있으면 그 글 전체가 중국어로 남는다(`rzhleft`로 원문을 받아 줄을 더한다). 시작 화면 패치 노트·모드 목록 창은 번역하지 않는다. 사용자 세이브에서는 미확인 | 54절 |
| 전투 HUD 신기 칸 | 위로 쌓은 칸이 종족 틀의 기술 단추·다른 모드의 단추와 겹치지 않는지는 사용자 세이브에서 미확인(고정본 인물은 종족 단추가 없다). 전투 불러오기 멈춤 한 번(원인 미확인) | 55절 |
| 만고신화 | 경지 표를 바꿔 바닐라 세이브의 경지가 한 단계 낮게 읽힌다. 기본 프리셋은 `alt`로 대체한다. 모드 기술 563개 가운데 522개는 이름이 없어 "이름 없음(번호)" | 72·73절 |
| 표 칸 바꿔 끼우기(16표 3,815칸) | 다른 모드 DLL이 `itemProps.name`·`dramaNpc.name` 같은 칸을 중국어 글과 비교한다면 맞지 않게 된다(근거 없음, 추측). 모드 업데이트로 원문이 바뀐 칸은 중국어로 돌아간다(`rzh all`로 다시 모은다) | 74절 |
| 의복 썸네일 채우기 | 창을 닫은 뒤 캐시를 비우는 길, 두 번째 캐릭터 만들기, 남성 목록은 화면으로 보지 않았다. 목록 209~220번(dressID 311~322)은 모드 그림이 같은 파일이라 모두 같은 옷 | 74절 |
| 법보 탭 | 켠 재능·올린 레벨·넣은 기령이 전투와 게임의 기령 화면에 반영되는지 미확인. 법보의 경지·강화·접두 옵션, 기령의 친밀도·장비는 범위 밖. NPC를 편집 대상으로는 시험하지 않았다 | 56절 |

## 8. 다음에 할 일

### 8.0 2026-10-29에 남은 일

12 문서 7절의 계획(묶음 1~6)과 2026-10-24의 남은 일 셋(배율 탭 확장, 미룬 사소한 것, 모드를 뺀 뒤의 세이브 동작)은 모두 끝났다(05 문서 77~86절). 2026-10-28에 트레이너 4차 전투 묶음(87절)을 끝내 설치했고, 2026-10-29에 세 번째 릴리스로 `main`에 넣었다. 같은 날 오후에 코드·UI·도구 리뷰와 미확인 항목 확인을 했다(13 문서, 9번). 남은 것은 사용자가 고르거나 확인할 것과 13 문서 7절의 개선 묶음이다.

0. **사용자 확인(실제 플레이에서, 설치본 bb122e1)**: 묶음 2·4·5(90·92·93절)는 동작 불변이 목표 — 배율 탭·프리셋·파일 저장(번역표·설정·즐겨찾기)·공법 탭 대도까지가 전처럼 되는지. 그 앞: 작은 수정 2차(89.3절: 성공·드롭 탭을 켠 채 다른 세이브를 열면 꺼져 있는지, 하위 화면을 연 채 세이브를 바꾸면 탭 첫 화면이 보이는지, 보유 칸 줄이기의 Enter 확인 체감). 그 앞: 트레이너 4차 — 일격(특히 보스·피해 상한이 있는 적), 전투 아이템 쿨타임, 법보 내구 없음, 선법·영역을 실제 단축키로 연달아 썼을 때(87.4절).
1. **그 앞의 사용자 확인(0402190부터 들어 있는 것)**: 배율 탭의 칩 짜임과 86절의 새 줄 넷, 특히 전투 이동 속도의 실제 걷는 빠르기(도구로는 값만 확인, 86.5절). 그 앞의 것: 법보 강화·지급 경지/단계·친밀 최대·전승·참오 결과 최대(82절), 대도까지 채우기(83절), 설치본 134197d 확인 목록(캐릭터 만들기 의복 썸네일, 만고신화 선천 이름·대사 한국어, 프리셋 21번. 73·74절)과 8.5절.
2. **새 기능 후보(사용자가 원할 때)**: 12 문서 5절의 남은 트레이너 기능 열셋 — 백그라운드 실행, 도주·호요 포획·종문 초빙 성공(67절에 판정 위치만), 호천안 비용·호계약 무제한, 도심 견고·응결치, 지도 밝히기, 마을 창고, NPC 가방, 전체 인물 검색·양방향 호감, 종문 탭, 화신 이후 돌파. 배율 탭은 칩마다 여유가 있다(줄 높이 64, 칩당 최대 8줄).
3. **남은 확인거리(미확인으로 적어 둔 것)**: 2026-10-29에 13 문서 2절(05 문서 88절)에서 넷을 닫았다 — 전투 장면 적재 중 지도 속도는 걸리지 않음(장면 객체 셋이 같은 프레임에 생기고 그 프레임에 이미 `timeScale=1`), 선법 `coolTime`(5.00)은 줄지 않는 고정값이고 쿨타임 없음에서 남는 시간은 사용 중(`isUse` 약 4초)과 기력 1 채우기, 전투 불러오기 멈춤은 같은 순서로 재현 안 됨(두 번째), 괴수 전투의 단위에는 법보가 없음. 남은 것: 편집기를 뺀 회차의 빈 선택지 대화(84.4절. 다시 보이면 편집기 있는 회차에서 그 사건의 선택지 글을 비교), 넘친 가방(90/64)에서 아이템을 얻을 때(84.4절. `-NoEditor` 회차에는 개발 명령이 없어 사용자 플레이에서), 인간 NPC 전투의 법보 내구(87.4절. 지도에서 NPC를 공격하는 도구가 필요), 일격의 동료 대상(87.5절. `UnitType.PlayerNPC` 가드 한 줄은 13 문서 7절 묶음 1), 신기 둘 이상의 전투 HUD 자리 경고 `artifact hud drift (전투 화면, mask=5)`(13 문서 2.5절. `artifact-check hud-battle`로). 옵션 행 없는 기술의 `대도까지…` 화면(83.5절)과 83.6절의 고치기 전 실패 확인은 하지 않기로 했다(13 문서 2.7절).
4. **게임 설치**: bb122e1(2026-10-30 21:34)이 설치돼 있다(1절). 다음 설치는 게임을 끈 뒤 `tools/install.ps1`, 기록은 1절 "지금 게임에 설치된 것"과 8.8절.
5. 의복 목록(75절): 299번 뒤에 고를 새 의상은 없다(빈 자리 57줄). 남은 것은 [剑来] 235~237번이 화집 해금 줄(`flag=xianlingbiyu`) 때문에 숨는지 확인하는 일(사용자가 원하면)과, 남성 목록 대조(미확인).
6. 사용자 결정: 같은 dressID 두 칸(217, 221~234, 238~243)과 [剑来]의 같은 그림 14장(267·310~322)을 그대로 둘지(74절). 03묶음 2304~2393의 비하 욕설 표현을 유지할지, `E:\c` 폴더를 지울지. 실험 브랜치 스물을 계속 남길지(8.7절).
7. 만고신화로 새 게임을 시작하면: 기본 프리셋의 역천개명 `alt`가 실제로 고르는지(`pstrict`), 경지 표가 바뀐 상태의 경지 탭·경지 올리기(73절).
8. 리뷰가 남긴 사소한 것: 13 문서 6절에 현황을 모았다(73·74·85·86·87절 끝과 1절 끝 "2026-10-18까지의 목록"의 남은 줄).
9. **다음 작업 후보(2026-10-29 리뷰, 13 문서 7절)**: 묶음 1은 끝남(2026-10-30, 05 문서 89절. 남은 확인은 89.3절 — 세이브 전환 뒤 성공·드롭 꺼짐과 하위 화면 닫힘은 사용자 플레이에서). 묶음 2도 끝남(2026-10-30, 90절. 설치 전). 묶음 3도 끝남(2026-10-30, 91절. 도구와 검사뿐). 묶음 4도 끝남(2026-10-30, 92절. 설치 전 — 묶음 2와 함께 설치할 것). 묶음 5도 끝남(2026-10-30, 93절. 설치 전). **묶음 1~5 모두 끝남.** 원래 목록: 묶음 1 작은 수정 2차(성공·드롭 상태가 다음 세이브로 넘어감 A1, 자원 탭 `보유` 칸의 떠날 때 쓰기와 확인 없는 버리기 B1·B2, 하위 화면이 세이브를 넘어 남음 B8, 기술 추가 안내 글 넘침 B3, 일격 동료 가드) → 2 배율 서비스 다듬기(이동 처리기 중복 A2, 쿨타임 단위 A4, 순수 판정 떼어 시험) → 3 검사 도구 정리(전투 종료 `Leave` 사본 셋 C1, 도우미 통합 C8, `session.tests` 보강 C3) → 4 파일 안전(번역·설정 파일 원자적 쓰기 A7) → 5 리팩토링 2차. 사용자가 정할 것 넷은 13 문서 7절 끝.

검사를 돌릴 때의 함정(2026-10-23에 겪음. 6절에도 해당):
- 측정 명령(`rperf tabs`·`query`·`press`) 뒤 같은 회차에서 `ui-check`를 돌리면 편집기 상태가 남아 실패하거나 요약 없이 끝난다. 검사는 측정 없는 회차에서.
- 경지에 기대는 검사(`modbooks`·`books-ui`·`appellation-ui`·`preset-check ui`)와 새 검사 대부분은 `-Exclude 2814696167`로. 만고신화가 있어야 하는 것(`translation`, `mastery-check modname`)은 빼지 않는다. `bagcount`는 `items` 뒤에 돌리면 실패(고정본 수량 전제). `keys-restart`는 `keys` 바로 뒤에.
- `treasure-check service`·`ui-check bagcount`·`treasure-check forge-ui`·`mastery-check` 부분들은 고정본의 빈 상태나 수를 전제하니 깨끗한 회차에서(82.5·83.5절).
- 회차의 첫 개발 명령은 낡은 명령 파일을 치우느라 빈 답일 수 있다. 검사 맨 앞에 예열 명령을 둔다.
- 게임 참오 창을 연출 중에 닫으면 게임 `Mask`가 남아 클릭을 막는다(82.5절). 전투 결과 창은 `bend` 뒤 몇 초 지나서 뜬다(83.5절). 전투 전 화면의 `전투 진입`은 (960, 684).
- 만고신화를 실은 회차에서 `-Restart`하면 첫 안내 이야기 화면이 편집기를 가린다. 화면 검사는 재시작 없는 회차나 `-Exclude` 회차에서.
- `preset-check ui`는 게임 대화창(`DramaSprite`)이 끼어 가끔 실패한다(develop 기준에서도). 다시 돌려 통과를 본다.
- (2026-10-27) 검사 안 함수 이름이 PowerShell 별칭과 겹치면(`Mv` = Move-Item, `R`) 별칭이 먼저 잡혀 "Path 매개 변수 없음" 같은 엉뚱한 오류가 난다. 함수 이름은 두 단어 이상으로.
- (2026-10-27) 전투를 끝내는 검사(2026-10-30부터 `check-lib.ps1`의 `LeaveBattleScene`이 이 규칙대로 한다. 91절): 전투 전 화면의 `전투 개시`(btnStart, (956,749))를 먼저 누르고, `bend` 뒤 결과 창의 확인 단추(959,831)가 실제로 맞을 때까지 기다렸다 누르고, `inBattle=False`까지 기다린다. 어기면 다음 전투가 빈 화면으로 뜬다. 고정본에 들어가면 편지 창(`DramaSutraLetter`), 걷기·대화 뒤에는 보상 창(`GetReward`)·우연 사건(`DramaFortuitous`)이 편집기를 가릴 수 있다(05 문서 86.3절의 `OpenRates`).
- (2026-10-27) 회차 스크립트를 PowerShell `-replace`로 고치면 `$_`가 바뀌어 거르기 줄이 깨질 수 있다. 회차 스크립트는 Write로 통째로 쓴다. PowerShell 문자열의 `"$i:"`는 변수 이름 오류다(`${i}:`).
- Bash heredoc 안에서 `\\`·`\\t`·`\\n`이 바뀐다. 파이썬 수정 스크립트는 파일로 써서 돌린다(메모리의 함정). PowerShell에서 여러 줄 `Replace`는 파일의 줄 끝(CRLF/LF)에 따라 조용히 빠진다 — 바꾼 수를 세어 확인한다. 함수 이름 `R`은 내장 별칭과 겹친다.

### 8.1 지금까지

사용자가 2026-10-12에 여섯 가지, 2026-10-13에 둘을 요청했다(8.2절). 모두 끝냈다. 2026-10-15의 넷 가운데 1~3은 그날, 4(G2 창작마당 모드)는 2026-10-16에 끝냈다(41~44절). 같은 날 추가로 요청받은 비급 지급과 도호 관리도 끝냈다.

| 묶음 | 내용 | 기록 |
|---|---|---|
| A | 날짜 고정·변경, 가방 칸 ±10 (요청 1, 2) | 05 문서 25절. 스펙·계획 `2026-10-12-date-bag*` |
| B | 화면 구조 정리와 탭별 UI/UX 개선 (요청 3) | 26·27절. 스펙 `2026-10-13-ui-rework-design.md`, 계획 `-1-shell.md`·`-2-widgets.md` |
| C | 가방 내보내기·가져오기 (요청 4) | 28절. 스펙·계획 `2026-10-13-bag-export*` |
| — | 사용자가 써 보고 알린 것: 목록의 글을 말줄임 대신 줄바꿈으로, 화폐 탭에 명성 | 29절(스펙 없음) |
| D | 캐릭터 내보내기·가져오기 (요청 5) | 30절. 스펙·계획 `2026-10-13-character-export*` |
| E | 안정화 (요청 6의 검토에서 고른 것): 역천개명 분류의 보호, 검증용 고정본 세이브, 화면의 작은 개선, 브랜치·복사본 정리 | 31절(스펙 없음) |
| F1 | 배율 탭 (2026-10-13의 새 요청 2): 수련치·전투 중 공법 숙련·이동의 날짜 소모·전투 속도의 배율, 무적과 피해 배율을 세계 탭에서 옮김 | 32절. 스펙·계획 `2026-10-13-rate-tab*` |
| — | 사용자가 써 보고 알린 것: 피해 배율이 걸리지 않는다(결함을 찾지 못함. 실제 체력 감소로 걸리는 것을 확인), 기술 쿨타임의 배율·제거, 받는 피해의 배율 | 33절(스펙 없음) |

| F2 | 프리셋 탭 (2026-10-13의 새 요청 1): 빌드 프리셋 열 개, 단계별 적용과 경지 올리기, 법보·기령·신혼·명우. 역천개명의 계열 보호를 운명 탭·캐릭터 가져오기에도 | 34절. 스펙 `2026-10-14-preset-tab-design.md`, 계획 `-1-engine.md`·`-2-builds.md`, 내용의 근거 `2026-10-14-preset-builds.md` |

| — | 사용자가 써 보고 알린 것: 프리셋 적용 뒤 선력 0/0(경지 올리기가 정식 돌파와 다르게 놓친 것 다섯을 찾아 고침), 능력치 탭의 본원 선력, 프리셋 상세보기의 머리글 넘침 | 35절 |
| — | 선법 탭(사용자 요청 2026-10-14): 본원 선력, 가진 선법, 전체 선법을 걸러 익히기 | 36절. 스펙·계획 `2026-10-14-immortal-tab*` |
| — | 시작 신기 조사(사용자 요청 2026-10-14)와 신기 탭: 신기 목록, 쌍어옥패 점수, 호천안 요수·장착, 연요호 호요 | 07 문서, 05 문서 37절. 스펙·계획 `2026-10-14-artifact-tab*` |
| G1 | 자원 탭 개편: 즐겨찾기(아이템+수량, 모두 지급)와 용도별 분류(사용자 요청 2·3, 2026-10-15) | 38절. 스펙·계획 `2026-10-15-resources-tab*` |
| — | 사용자가 써 보고 알린 것: 자원 탭 표의 설명 열 빼기, 아래 설명 칸 스크롤 | 38절 끝(스펙 없음) |
| — | 기술 서적(비급) 지급(사용자 요청 2026-10-15): 자원 탭의 기술 서적 그룹, 경지·최소 품질, 한 번에 20권, 즐겨찾기의 비급 줄 | 39절. 스펙·계획 `2026-10-15-martial-books*` |
| — | 도호(칭호) 관리(사용자 요청 2026-10-15): 16번째 도호 탭, 주기·빼기·장착, '제일' 도호 옮기기, 편집기가 주고 뺀 도호의 획득·상실 창 닫기 | 40절. 스펙·계획 `2026-10-15-appellation*` |
| G2a | 창작마당 모드 받기·설치(2026-10-15 요청 4의 첫 단계): SteamCMD로 받아 `ModExportData`에 넣는 도구 `tools/workshop.ps1`, 15개 설치 | 41절(스펙 없음. 도구 스크립트) |
| G2b | 모드 공법의 비급: 모드 행(`isModExtend`)을 카탈로그에, 치환 뽑기로 비급 지급, `모드만` 칩·`[모드]` 꼬리표 | 42절. 스펙·계획 `2026-10-16-mod-skill-books*` |
| G2c | 모드 글 한글화: 번역 파일 59행을 DLL에 내장해 데이터 폴더에 풀고 `Init`마다 모드 행의 `kr` 덮기, `rloc` | 43절. 스펙·계획 `2026-10-16-mod-korean*` |
| — | 모드 신통 비급(사용자 요청 2026-10-16 "읽을 수 없어 추가할 수 없는 공법 다시 확인"): 옵션 번호를 모드 것으로 바꿔 신통 차단을 풂 | 44절(스펙 없음. 브랜치 `fix/mod-ultimate-books`) |
| W | 창작마당 확장: 검색·등록부·컬렉션 (2026-10-16 요청 2·3·5). 목록 일곱 칸·이력, `-Search` 후보표, `-Doc` 등록부, 새 모드 8개(23개), Steam 컬렉션 3820620999 | 45절. 스펙·계획 `2026-10-16-workshop-expand*` |
| K | 새 모드 8개 한글화 (2026-10-16 요청 4). 번역 765행(824행), 번역 파일 덧붙이기 | 46절. 스펙 `2026-10-16-mod-korean-2-design.md` |
| Y | 리뷰 남은 도구 묶음(9·19·17): `.devremote` 경로 한 곳(`TLTOYBOX_DEVREMOTE`), 커밋 표지(`Init build=`), 시험 프로젝트 `tests/ModMain.Tests` | 49절. 스펙 `2026-10-16-review-tools-design.md` |
| X | 리뷰 수정 묶음 (2026-10-16 요청 6). 탭 줄 재배치(화폐 그룹·안전 단추·46 간격), 작은 수정 열 가지(깨진 공법 줄, 두 번 누르기 넷, 예외 표시, 세이브 전환 때 끄기, 가져오기 보충, 백업 세 곳, 이름 없음 줄, 결과 칸 더 보기, 문서) | 47·48절. 스펙 `2026-10-16-review-fixes-design.md` |
| Z | LocalText 밖 중국어 글 한글화 (2026-10-17 요청 "캡처 밖 미번역 전부 확인"의 후속). 원문 기반 번역표 `texts.txt` 245줄, 표 칸 131개 바꿔 끼움, 화면 글 치환(Harmony), LocalText ch→kr 사전 | 53·54절. 스펙·계획 `2026-10-17-zh-text-korean*` |
| — | 사용자 보고 셋 (2026-10-17 오후): 즐겨찾기 수량이 남지 않음(칸을 떠나도 저장), 전투 HUD 신기 칸이 종족 기술 단추와 겹침(위로 쌓기), "게임이 이 기술을 만들지 못했습니다"(NPC 전용 기술 셋 제외) | 55절(스펙 없음. 브랜치 `fix/editor-followups`) |
| — | 법보 탭 (2026-10-17 요청 "법보와 기령관리도 추가 검토"): 가진 법보·기령, 장착·해제, 기령 넣기·빼기·재능·레벨·지우기, 기령 만들기, 법보 지급 | 56절. 스펙·계획 `2026-10-17-treasure-tab*` |
| — | 외부 트레이너(ggbh-trainer 0.8.30)와 기능 비교 (2026-10-19 요청): 없는 기능·일부만 있는 기능, 트레이너가 쓰는 게임 API, 제안 순서(성격·무한 아이템·가방 수량·영력/집중력 → 성공률 묶음 → 배율 확장 → 종문·창고 등 새 탭) | `docs/11-trainer-comparison.md` 4절 |
| — | 트레이너 비교 1묶음 (2026-10-20 요청): 성격, 아이템 소모 없음, 가방 수량 지정·버리기, 영력·집중력 없음 넓히기 | 65·66절. 스펙·계획 `2026-10-20-trainer-batch1*` |
| — | 트레이너 비교 2묶음 (2026-10-20 요청): 공법 옵션 자리 강제, 성공·드롭 탭(훔치기·관계 발전 성공, 드롭 배수). 종문 초빙·호요 포획·도주는 판정 위치만 기록(67절), 사용자 결정으로 미구현 | 67·68절. 스펙·계획 `2026-10-20-trainer-batch2*` |
| — | 기본 프리셋 10개 검증과 `*.meta.json` 분리 (2026-10-20 요청): 옵션·이름·운명 경지 모두 게임 표와 대조, 운명 이름 띄어쓰기 고침, 질룡파 6 등은 유효로 확인. 리뷰 번들 초안 12~21번은 다음 일 | 69절. 개발 명령 `pstrict` |
| — | 리뷰 번들 초안을 기본 프리셋으로 (2026-10-20 요청): 11~16 여섯 개(지법·지검풍·빙화도·토수·풍수·만신검)와 메타. 17·18·20(출처가 요약뿐이라 플레이어 줄이 모두 채움)·19(02 수검과 같음)·21(모드 미설치)은 만들지 않음. 이미 폴더가 있는 사용자에게도 새 기본값을 푸는 `.defaults` 기록 | 70절. 스펙·계획 `2026-10-20-preset-drafts*`, 작업 기록 `2026-10-20-preset-drafts-builds.md`, 개발 명령 `pfindch` |

### 8.2 사용자의 요청 (원문, 2026-10-12)

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

사용자의 새 요청 (원문, 2026-10-13 저녁):

    추가 기능 검토
    1 프리셋 탭 추가
    1-1) 캐릭터 빌드를 인터넷등에서 리서치후 최상위 빌드 10가지를 프리셋으로 구성후 해당 프리셋을 선택시 해당 빌드를 풀셋으로 적용 할 수 있어야 한다 프리셋 목록에서는 적용 버튼과 해당 프리셋이 가지고 있는 빌드의 세부 구성 방식을 볼수 있는 상세보기가 존재하고 상세보기시 해당 빌드의 구성 단계와 특정 단계별로 진행을 버튼으로 적용할수 있어야 한다
    2 배율탭 추가
    2-1) 전투중 공법등의 경험치나 이동중 시간 흐름등 배율을 설정할수 있는 상황과 환경을 리서치하고 배율을 적용할수 있는지

사용자의 새 요청 (원문, 2026-10-15. 첨부: `D:\Download\README.md`, `run_download.ps1`, `build_lists.py`):

    1 문서확인
    2 자원탭에 즐겨찾기기능 추가
    3 자원탭 카테고리 분류 재조정 및 개선 (분류/UI/UX)
    4 스팀 모드 반영 및  및 모드 한글화, 공법 모드의 경우 이 모드에서 해당 공법 문서 획득 여부

요청 2·3은 G1(38절)으로 끝냈다. 요청 1의 결과와 요청 4는 8.3b절. 같은 날 추가로 비급 지급(39절)과 도호 관리(40절)를 요청받아 끝냈다.

### 8.3b G2 창작마당 모드 연동 — G2a·G2b·G2c 모두 끝남(2026-10-16)

사용자가 G1 다음에 하기로 골랐다. `/superpowers:brainstorming`으로 시작한다. 지금까지 확인한 것(요청 1, 문서 확인의 결과):

- 첨부 도구(`D:\Download\README.md`, `run_download.ps1`, `build_lists.py`)는 외형·공법 모드의 구독자·즐겨찾기 상위 N개를 골라 SteamCMD로 받는다. 2026-10-15에는 아직 실행 전이었다(`D:\Download\output\`, `C:\steamcmd\` 없음).
- README의 `python build_lists.py`는 이 PC에서 돌지 않는다. `py -3 build_lists.py`로 돌린다(6절).
- 이 설치본은 Steam을 거치지 않아 창작마당 구독이 동작하지 않는다(01 문서 11절). 그래서 SteamCMD로 받아 로컬 모드 폴더(`ModExportData\`)에 넣는 길이 필요하다.
- 창작마당 항목은 `<창작마당ID>\debug\Mod_<모드ID>\` 모양이다(`E:\SteamLibrary\steamapps\workshop\content\1468810_disabled_claude`에 남은 셋으로 확인). 로컬 모드는 `ModExportData\Mod_<모드ID>\`에서 읽힌다(12절). `run_download.ps1 -GameModDir`는 `<창작마당ID>` 폴더를 통째로 복사하므로 한 단계 깊다. 게임이 그 모양을 읽는지는 미확인.
- 서로 다른 창작마당 항목 2819187866과 2823508121이 같은 `Mod_TCesd4`를 쓴다. 펼쳐 넣으면 서로 덮어쓴다.
- 모드의 설정 표는 암호화된 `ModExportData.cache`에 있어 파일을 고쳐 한글화할 수 없다. 게임이 모드를 불러온 뒤 편집기 모드가 번역을 덮는 방식이 현실적이다(미확인).
- 공법의 비급은 아이템 표(`ItemProps`)에 없고, 기술 번호를 담은 비급 아이템(`DataProps.PropsDataType.Martial`)으로 만들어진다. 공법 탭의 기술 목록(`SkillCatalog`)은 실행 중의 설정 표를 읽으므로 이름이 있는 모드 공법은 목록에 나올 것이다(미확인). 자원 탭은 표에 없는 분류 번호를 `기타`에 모은다(38절).
- SteamCMD의 첫 로그인(비밀번호·Steam Guard)은 사용자가 직접 한다. 그 뒤 캐시된 로그인으로 받기·설치는 편집기 쪽이 할 수 있다. SteamCMD 설치 파일을 받는 것은 사용자에게 허락을 받는다.
- 창작마당 모드의 DLL(`ModCode`)은 게임 안에서 코드로 실행된다. 설치할 목록을 사용자가 확인한다. 게임 폴더에 넣는 것도 사용자 확인 뒤에 한다(CLAUDE.md).
- 요청 4의 "공법 문서 획득 여부"가 "모드 공법의 비급을 이 편집기로 얻을 수 있는지(지급)"인지 "얻었는지 보여 주기(표시)"인지는 사용자에게 묻는다.
- 설계 전에 게임에서 실험할 셋: 게임이 읽는 폴더 모양(`ModMgr.pathModExportData`, `allModPaths`), 번역을 덮을 자리(`LocalText` 표 또는 `GameTool.LS`), 모드 공법이 기술 목록에 나오는지.

2026-10-15 G2 시작에서 한 것과 막힌 것:

- 사용자가 고른 나누기: G2a 받기·설치 → G2b 모드 공법의 비급(39절의 기술 서적 지급이 모드 공법에도 되는지) → G2c 한글화. 각각 스펙·계획·구현.
- `build_lists.py`(`py -3 -I -X utf8 D:\Download\build_lists.py --out D:\Download\output`)의 자동 목록은 공법 14개 대부분이 공법을 추가하지 않는 편의·시스템 모드였고, 외형 15개에도 세계관·종문·BGM 모드가 섞였다. 사용자가 "내용 기준으로 다시 고르기"를 골랐다. 설명을 읽어 고른 목록(사용자 확인): 외형 `2814703549`(Fatury, 선행), `2815562168`, `3200319699`, `3001496635`, `2993500314`, `2819712935`, `2823930683`, `2945768367`, `2945768219`, `2874128983`, `2876769227`, `2929805068`, `2832167498`, `2814806198` / 공법 `3390738990`(【只有梦长】五本功法开源: 새 공법 다섯, 게임 안의 산출 없음), `2839261049`(真.镇宗之宝：传承心法). 인기 모드 가운데 새 공법을 더하는 것은 드물다. 합계 약 455MB, 페이지의 필요 항목은 `2815562168` → Fatury뿐(설명에 Fatury를 쓴다는 것이 둘 더).
- SteamCMD는 `E:\SteamCmd\steamcmd.exe`, 계정 `deepestlight`의 로그인이 캐시돼 있다(2026-10-15 15:53, 비밀번호 없이 로그인된다).
- **막힌 것**: 모든 항목이 `failed (Failure)`, 로그는 `No workshop depot defined, skipping non-legacy item`. `app_status 1468810`이 `License packageID 0 … State : No License`다. **이 계정은 귀곡팔황을 갖고 있지 않다**(다른 라이선스 1528개). SteamCMD의 창작마당 받기는 게임을 가진 계정이어야 한다. 이 PC의 Steam에 로그인한 계정도 `deepestlight` 하나다(`loginusers.vdf`).
- 받는 길(사용자 몫): 게임을 가진 다른 계정으로 한 번 로그인(`E:\SteamCmd\steamcmd.exe +login <계정> +quit`, 비밀번호·Steam Guard는 사용자), 또는 `deepestlight`에 게임 구매, 또는 Steam 클라이언트(가족 공유 등)로 구독. 그 뒤 받기: `+login <계정> +runscript <목록 파일> +quit`(목록 파일은 `workshop_download_item 1468810 <ID>` 줄). 받은 위치는 `E:\SteamCmd\steamapps\workshop\content\1468810\<ID>\debug\Mod_<모드ID>\`이고, 우리 모드 폴더(`ModExportData\Mod_sWP6UL\`)와 같은 모양이다.
- 이 PC에 남은 창작마당 항목 셋(`E:\SteamLibrary\...\1468810_disabled_claude`)은 성인향 하나, 돌파 재료 하나, 내려간 것 하나라 개발 재료로 맞지 않는다.

2026-10-15 저녁: 받는 길이 열렸다(`deepestlight`가 게임 보유, `app_status 1468810`이 `Subscribed,Permanent`). 16개를 모두 받았고 G2a(받기·설치 도구, 회차 스크립트 확장, 검증)는 05 문서 41절에 있다. 받은 16개와 설치 목록 15개의 차이(`2945768219` 제외)는 스펙 `2026-10-15-workshop-install-design.md` 1절. 남은 것은 G2b(모드 공법의 비급)와 G2c(한글화)이고, 설계 전 실험 둘(모드 공법이 기술 목록에 나오는지, 번역을 덮을 자리)이 남아 있다. 실측으로 알게 된 것: 게임은 `ModExportData\<아무 이름>\ModExportData.cache`를 한 단계만 읽고, 모드 ID와 창작마당 번호는 cache 안에 있으며, 새 모드는 순서 자료 없이도 로드되고 처음 발견한 순서(한 번에 여럿이면 이름순)를 게임이 적어 둔다.

### 8.3c 기술 서적(비급) 지급 — 실험 결과 (2026-10-15, 끝남)

05 문서 39절로 만들었다. 아래는 그 전의 실험 기록이다.

사용자 요청(2026-10-15): 자원 탭에서 기예 서적처럼 기술 서적(무예/영술·절기·신법·심법·신통)도 지급하고 싶다. G2와 별도로 먼저 하고, 게임 실험부터. 기술 서적은 게임 아이템 표(`ItemProps`)에 없고 얻을 때마다 내용(기술 번호·경지·품질·옵션)을 붙여 만드는 아이템이라 자원 탭 분류(38절)에 들어가지 않았다.

실험(회차 `res-1015o`, `res-1015p`, 고정본, 실험 브랜치 `feat/martialbook-research`의 `Dev/DevCommandsMartialBook.cs`, 병합하지 않는다):

| 질문 | 결과 |
|---|---|
| 만들 수 있는가 | 된다. 공법 탭의 기술 추가와 같은 게임 보상 뽑기(`RewardTool.GetSkillReward`·`GetStepReward`·`GetAbilityReward(묶음, 경지, null)`)를 원하는 기술이 나올 때까지 돌리고(절기 23116 경지 3에서 2~144번, 무예 44112에서 33번), `DataProps.PropsData.NewMartial(종류, 값)`으로 만든 것을 가방(`propData.AddProps`)에 넣었다. 가방 칸을 하나씩 차지한다(89 → 91/154) |
| 게임의 비급과 같은가 | 값의 모양이 고정본에 원래 있던 비급과 같다: `[2, 0, 1, 종류, 기술 번호, 경지, 품질, 옵션…, -1, …]`(예: 만든 것 `[2,0,1,2,23116,3,1,3,13,6,-1,22,90,23,271]`, 원래 것 `[2,0,1,2,23116,3,5,4,-1,45,1]`). 이름에 옵션의 접두어가 붙고(정전질룡파 / 금궁질룡파 / 우연질룡파), 품질(`martialInfo.level`)이 읽힌다. 게임 가방의 `서적` 탭에 비급 아이콘으로 나온다(캡처 `.devremote\mb-bag-books.png`) |
| 게임의 학습으로 배워지는가 | 된다. 게임의 학습 행동 `UnitActionPropMartialStudy(가방의 비급)`가 허용되고(`IsCreateAction` 0) 학습 진행도가 세계 자료 `g.data.world.allMartialStudy[비급 soleID]`(`exp`, `lastMonst`)에 쌓인다(한 번에 8~12). 진행도를 크게 두고 다음 달에 학습하자 `isStudyComplete`가 참이 되고, 배운 기술이 28 → 29, 비급은 가방에서 사라지고, 학습 기록이 지워졌다. 배운 기술은 비급의 고유 번호와 옵션을 그대로 가진다(`661DjS = 우연질룡파`) |
| 학습의 제한 | 한 인물이 한 달에 한 번만 학습한다(같은 달에는 다른 비급도 `IsCreateAction` 1). 비급 쪽의 `lastMonst`를 바꿔도 풀리지 않았고 달력이 바뀌면 풀렸다. 완료에 필요한 진행도는 103으로는 모자랐다(정확한 값 미확인, 9999로 완료). 학습 비용은 설정 표 `BattleSkillCost`(경지·품질·종류별 `money`, `days`, `talentExp`)로 보인다(미확인). 게임 화면의 학습은 `MartialLearnGame`(학습 미니게임) 화면으로 하는 것으로 보인다(열어 보지 않았다) |
| 보지 못한 것 | 가방에서 비급을 눌러 뜨는 정보 창(클릭은 아이콘에 닿았으나 창이 뜨지 않았다), 게임 화면의 학습 미니게임으로 끝까지 배우기, 학습 비용이 실제로 빠지는지, 완료 기준 값, `GetStudyTip`(IL2CPP 값 형식 반환이라 값이 깨진다) |

추천: 자원 탭에 `기술 서적` 그룹(종류 다섯)을 두고, 공법 탭의 기술 추가와 같은 뽑기로 비급을 만들어 가방에 넣는 지급을 만든다. 만들어진 비급은 게임의 비급과 같은 것이므로 학습·판매·NPC에게 주기는 게임이 맡는다. 설계에서 정할 것: 경지 고르기(지금 경지 / 직접), 품질·옵션(뽑기에 맡김 / 고름), 기술 목록의 출처(`SkillCatalog`), 수량(한 권씩 / n권), 즐겨찾기와의 관계(번호가 아니라 기술·경지로 저장해야 한다). 이 일은 G2의 "공법 모드의 문서 획득 여부"와 같은 장치다.

실험 중 본 것: `tadv 7`(달 마지막 날)이 거부되는 동안 기연 사건 화면(`DramaFortuitous`)이 떴다. 회차의 `-End`가 세이브를 되돌려 남지 않았다.

### 8.3d 도호(칭호) 관리 — 실험 결과 (2026-10-15, 끝남)

05 문서 40절로 만들었다. 아래는 그 전의 실험 기록이다.

사용자 요청(2026-10-15): G2를 미루고 "도호관리 기능부터 먼저 기능 검토". 사용자가 도호(道号)·칭호라고 답했다. 게임의 한국어 글은 칭호(appellation)를 "도호"라고 부른다(예: 고정본의 "영단도인").

실험(회차 `apl-1015a`, `apl-1015b`, 고정본, 실험 브랜치 `feat/appellation-research`의 `Dev/DevCommandsAppellation.cs`, 병합하지 않는다):

| 질문 | 결과 |
|---|---|
| 어디에 있나 | 설정 표 `g.conf.appellationTitle`(100줄: `className`, `appellationLevel`, `priority`, `name`, `desc`, `tips`, `gainDesc`, `roleEffect`, `lossType`/`lossValue`, `dropType`/`dropValue`, `condition`, `symbolID`). 인물의 저장 자료는 `unitData.appellationTitle`(`appellationItems`: `id`와 얻은 `month`, `equipAppellationID`). 실행 중의 관리자는 `unit.appellation`(`WorldUnitAppellationMgr`: `allAppellations`, `allEquipAppellations`, 정적 `allAppellationUnits`(번호 → 가진 인물)) |
| 무엇이 있나 | 분류(`className` 첫 값): 101 지역 제일 X수 60개(영녕~팔황, 무기·영근별. 한 사람만 가진다), 102 풍류(도려 수), 103 괴수(종문 대비무), 104 산재(소비), 105 예상선군·선자(의복), 106 소요(무소속), 107 영단(단방), 108 영보(기보), 109 만법천존, 110 어계천존·단금쉬옥, 112 참요천존, 2601~2605 이야기 결말. 낮은 단계는 경지가 오르면 떨어진다(`dropType` 1, 값이 경지) |
| 이름을 바꿀 수 있나 | **저장 자료에 인물별 이름이 없다**(번호와 달만). 이름은 설정 표의 고정 글이다. 바꾸려면 설정 표를 실행 중에 덮어써야 하고 모든 인물에게 같이 바뀐다(실험하지 않았다). `appellationSymbolNamed`(19줄)는 이름이 비어 있다 |
| 주기·빼기 | `AddAppellationID(id)`가 아무 번호나 받는다(경지 밖, 이미 다른 인물이 가진 "제일" 도호도 `True`). 게임의 "도호 획득" 창(`GetTaoist`)이 하나씩 뜬다. `DelAppellationID(id, false)`가 뺀다. NPC에게도 된다. 저장 후 재시작해도 남는다 |
| 한 사람만 가지는 도호 | 플레이어에게 주면 두 사람이 가진다(`팔황 제일권수` 1404: 관산과 플레이어). 달을 넘겨도 게임이 정리하지 않았다. 잃는 판정(`WorldUnitAppellationLoss1`)은 도전에 졌을 때·공법을 지웠을 때이고, 떨어지는 판정(`WorldUnitAppellationDrop1`)은 돌파 때다 |
| 장착 | 한 번에 하나(`equipAppellationID`가 바뀐다). 장착한 것을 빼면 장착이 비고, 비어 있을 때 넣은 도호가 장착된다(재시작 때도 하나를 장착한다). 게임의 인물 화면에서 초상 아래 도호를 누르면 `TaoistSelect`(획득한 도호 / 도호 획득 가능, `도호 교체`)가 열려 플레이어가 직접 바꿀 수 있다 |
| 효과 | 장착했을 때만. `roleEffect` → `g.conf.roleEffect`(`effectType`, `value`). 예: 어계천존(2111) `talent_1_40` → 인물 화면의 오성 90 → 130. 영단도인 `giveCloseStable`(단약 선물), 풍류·예상·만법천존 `npcCloseStable`(호감), 괴수 `schoolMoneyAdd`·`schoolBuyDecline`(종문), 단금쉬옥 `hpMax_0_15`, 설예도군 `fiveFlowerSummon`(소환). 지역 제일 X수는 효과 표가 없고 같은·낮은 경지에게서 받는 그 무기 피해를 줄인다(`tips`) |
| 그 밖 | 도호를 넣은 뒤 NPC가 그 도호로 말을 거는 대화가 떴다("과연 풍류도인, …"). 고정본 NPC 1449명 가운데 48명이 도호를 가졌다 |

추천: 도호 관리를 편집기에 넣는다. 보기(대상의 도호·장착·효과), 주기(분류별 100개, 효과와 얻는 법을 보여 줌), 빼기, 장착 바꾸기, NPC에게도. "제일" 도호를 줄 때는 지금 가진 인물에게서 빼고 옮기는 것을 기본으로 한다(게임이 정리하지 않으므로). 경지 밖 도호는 다음 돌파에 떨어질 수 있다고 알린다. 이름 바꾸기는 저장 구조에 없어 넣지 않는 것을 추천한다(하려면 표시 덮어쓰기로 따로 설계). 설계에서 정할 것: 자리(새 탭 / 운명·능력치 탭 안), 대상(플레이어만 / NPC도), "제일" 도호의 처리, 도호 획득 창을 띄울지.

2026-10-16: G2b는 05 문서 42절로 끝났다. 42절에서 막았던 모드 신통 지급은 같은 날 44절에서 열었다(옵션 번호 치환). G2c(한글화)는 05 문서 43절로 끝났다(번역표는 스펙 `2026-10-16-mod-korean-design.md` 부록 A). 요청 4는 끝났다.

### 8.3e G2b·G2c 설계 전 실험 결과 (2026-10-16, 끝남)

회차 `ws-1016a`~`f`(고정본. 설치된 창작마당 모드 15개가 회차 전부터 있던 것이라 그대로 로드됨), 실험 브랜치 `feat/modskill-research`의 `Dev/DevCommandsWorkshop.cs`(`wsconf`, `wsname`, `wsbook`, `wsbookas`, `wslearn`, `wslearnas`, `wssetkr`). 개발 브랜치에는 병합하지 않는다.

**모드 공법이 어디 있는가.** 공법 모드 `3390738990`(【只有梦长】五本功法开源)의 공법 다섯은 실행 중 설정 표에 들어 있다(`g.conf.battleSkillAttack._allConfList` 1573행 = 기본 1568 + 5, `battleSkillBase` 2062 = 2044 + 18). 행은 `name`이 LocalText 키(`skill_name_middle<번호>`), `gradeMin`·`gradeMax`가 0, `skillID`가 `id`와 같다.

| 번호 | 종류(`type`) | 이름(`ch`) | 모드 JSON의 표 |
|---|---|---|---|
| 59898952 | 4 신통 | 千杀神枪 | BattleSkillAttack·Base·Sub·Value·PrefixValue·Condition·Effect, BattleEffect, BattleMissile, BattleShot99, LocalText |
| 984555698 | 3 절기 | 烈炎掌 | (같은 모양) |
| 353535 | 3 절기 | 藏气狂刀 | |
| 3535351 | 1 무예 | 赤炎剑 | |
| 844854565 | 3 절기 | 风旋龙卷 | |

`BattleSkillCombine`(보상 묶음)은 없다. 공법 탭의 `SkillCatalog`는 `gradeMax < 1`이거나 이름 키가 풀리지 않으면 빼므로(`Add`), 다섯 모두 목록에 없고 `rskill`도 `no skill`이다. DLL 모드 `2839261049`(真.镇宗之宝：传承心法)는 기술 표에 행을 더하지 않는다(코드로 동작. 표로는 줄 수 없다).

**이름.** 모드의 LocalText 행은 `ch`만 있고 `kr`은 글자 "0"이다. 그래서 `GameTool.LS(키)`가 "0"을 돌려주고 게임 화면에도 "0"으로 보인다(`wsconf`: `ls=0`, `wsname`: `ch=千杀神枪 kr=0`). 모드 JSON 원본은 주석(`//`)과 뒤 쉼표가 있는 느슨한 형식이라 보통 JSON 파서가 거부한다(내보낸 `debug` 쪽 JSON은 암호화돼 읽을 수 없다).

**비급·배우기.** 평범한 값 `[2,0,1,종류,번호,경지]`로 만든 비급은 기본 기술(11412)에서도 깨진다(비급 값의 모양이 아니다. 배우기 전용). 게임이 뽑은 진짜 비급 값(`SkillCatalog.RollValues`로 같은 종류의 기본 기술을 뽑은 것. 모양은 `[기술 번호, 경지, …옵션…, -1, …]`로 0번이 기술 번호)에서 0번만 모드 번호로 바꿔 `PropsData.NewMartial`로 만들면:

| 종류 | 비급(가방에 넣고 읽기·`rbookstudy`) | 바로 배우기(`StudyActionMartialProp`·`cmartial`) |
|---|---|---|
| 절기 셋(984555698, 353535, 844854565) | 6/6 성공. 이름 `<옵션 접두>0`(예: 묵운0), 품질 3, `can=0`(39절의 수용 값) | 성공. `cmartial`로 읽힘(이름 우무0, 품질 3) |
| 무예 3535351 | 뽑힌 옵션 조합에 따라 다름: 4번 중 1번만 읽힘(적하0, 품질 4, `can=5`). `can=5`는 이 고정본의 진짜 무예 비급(`fBfinb`, 새로 만든 44112 비급)도 같으므로 정상 | 2번 중 1번 읽힘(이연0·혼명0 품질 4). 읽히지 않는 것은 깨진 기술(이름 없음)로 남는다 |
| 신통 59898952 | 원본을 바꿔도(32112·33112) 3/3 모두 값을 읽지 못함(NullReferenceException) | 배워지지만 읽지 못함(깨진 기술). 원인·해결은 05 문서 44절(옵션 번호가 모드에 없어서) |

무예가 조합에 따라 갈리는 이유와 신통이 늘 깨지는 이유는 2026-10-16 05 문서 44절에서 규명했다: 비급의 옵션 번호(number)가 모드 공법의 PrefixValue 행에 없어서다. 처음의 추정은 절반만 맞았다. 표가 아니라 옵션 번호가 없었다(당시 추정: 모드가 `BattleSkillCombine`·`BattleSkillLevel` 등 일부 표를 주지 않아 옵션 접두가 그 기술에 없는 값을 가리키는 것으로 추정). 깨진 비급·기술은 회차가 되돌려 사용자 세이브에 남지 않았다.

**한글화의 자리(G2c).** 실행 중에 `g.conf.localText.allConfList`의 그 행 `kr`을 덮어쓰면 `GameTool.LS`가 바로 바뀌고(`wssetkr skill_name_middle59898952 천살신창` → `before=0 after=천살신창`), 그 뒤 만든 비급의 이름도 "원명열염장"처럼 한글로 나온다. 모드 자체의 이름(`g.mod.modDatas[].name`)은 LocalText 키가 아니라 글 그대로다(서식 태그 포함).

**설계에 주는 뜻.** (1) 모드 공법 목록은 `SkillCatalog`의 필터를 지나지 못하므로 "기본 표에 없는 번호" 또는 "이름 키가 모드 LocalText에 있는 행"을 따로 모아 보이고, 이름은 `kr`이 "0"이면 `ch`로 대신한다. (2) 비급은 "같은 종류의 기본 기술을 뽑아 0번만 바꾸고 `To<MartialData>()`로 읽어 본 뒤 받아들이기"를 되풀이하면 절기·무예는 된다. 신통은 이 모드에서는 안 된다. (3) 한글화는 번역 파일 → 실행 중 `kr` 덮기로 가능하다.

회차 이름: `ws-1016a`(표 덤프), `ws-1016b`(평범한 값), `ws-1016c`(치환 4번), `ws-1016d`(대조군·`wsname`), `ws-1016e`(치환 0번·배우기), `ws-1016f`(`wssetkr`). 비교에 쓴 기본 표 번호 집합은 `.devremote\base-skill-ids.txt`(git 제외), 덤프는 `.devremote\dump-*.txt`.

### 8.3a 다음: 이동 배율과 이른 월말 안내 (재현 못 함, 열림)

2026-10-16 사용자 관찰(화면 캡처): 배율 줄에 `이동 x0.25`가 켜진 채 달이 바뀐 직후("팔황세계가 변천하는 중" 뒤) "이번 달의 시간을 전부 사용했습니다"가 떴다. 재현 조건의 단서로 적어 둔다(아직 조사하지 않음).

사용자 보고(2026-10-14): 배율 탭의 `이동의 날짜 소모` x0.25에서 한 칸씩 가도, 여러 칸을 한 번에 가도, 화면 날짜가 대략 12~15일인데 평소의 달 종료 문구가 뜬다.

지금까지 한 것(회차 `month-1014a`, 05 문서 37절 끝 "이동 배율과 이른 월말 안내"):

- 직접 이동(`wmove`, `UnitActionMovePlayer`)으로 150번 오가는 동안 한 칸 0.046875일씩 쌓여 20칸에 하루, `worldDay`·`roundDay`·`residueDay`가 맞게 줄었다(2일 → 9일). 월말 안내 없음.
- 게임의 경로 이동(`rpath`)은 한 칸에 쌓이는 값이 걸린 비용의 두 배쯤이었다(0.359 → 0.484, 비용 0.078). 마을 옆이라 두 걸음 만에 마을 화면이 열려 길게 재지 못했다.

다음 세션이 할 것:

1. 사용자에게 받을 것: 달 종료 문구가 뜬 화면의 캡처(날짜, 왼쪽 아래 달 넘기기 버튼의 남은 날 표시), 날짜 고정을 함께 켰는지, 새 인물(1년 1월)이었는지. 사용자의 세이브에서만 나면 `-Fixture` 없이(사용자의 세이브로) 회차를 연다(인계 5절. 그 세이브는 월말 기록 창이 떠 있을 수 있다).
2. 마을에서 떨어진 넓은 자리로 순간 이동(세계 탭 또는 개발 명령)한 뒤 경로 이동(`rpath`)으로 여러 칸을 가며 `wdate`(`worldDay`, `roundDay`, `residueDay`, `moveDay`)와 `uilist`를 매 걸음 뜬다. 스크립트 `.devremote\month-probe2.ps1`(git 제외)이 그 틀이다.
3. 의심할 곳: 경로를 정할 때 게임이 배율 없는 비용으로 남은 날짜를 따로 계산하는지(`UnitActionMovePlayer`의 `moveDay`·`GetCurDay`, 경로 이동 쪽 함수), 경로 이동이 한 칸에 비용을 두 번 쓰는지, 날짜 고정(`TimeService`)과 겹칠 때.
4. 재현되면 실패하는 검사(`ui-check.ps1 -Part rates`나 새 부분)를 먼저 넣고 고친다.

### 8.3 신기 (2026-10-14, 끝남)

사용자 요청 "시작 신기 셋과 모드 관리 데이터 조사"는 07 문서(1~6절 조사, 7절 게임 확인, 8절 편집기 후보, 9절 미확인)로, 이어진 기능은 신기 탭(05 문서 37절)으로 끝냈다. 사용자에게 알릴 것과 남은 일:

- 요수 해금은 게임 호천안의 "요수 격파 n/5"(호천안 돌파 진행)도 올린다. 돌파가 실제로 열리는지는 미확인(37절).
- 남은 미확인: 호천안 기술과 호요의 전투 사용, 호요 상한 28의 출처, 불러올 때마다 호천안 화면이 뜨는 원인, 쌍어옥패 점수의 게임 상한(37절 끝, 07 문서 9절).
- 실험 명령은 `feat/artifact-research`의 `Dev/DevCommandsArtifactResearch.cs`(`confname <번호>`: 그 번호의 줄이 있는 설정 표와 이름, `confset`, `gecount`, `bwui`·`bwmove`, `bdev`·`buse`, `brates`·`brate`, `bskills`·`bskillset`, `bclickinfo`·`bcomps` 등). 그 브랜치는 병합하지 않는다.
- 사용자가 직접 정해 둔 신기 단축키가 `config.txt`에 있다(쌍어패 C, 호천안 Z, 게임 신기 키 끔). 검사 `keys`는 비우고 시작하며 회차 `-End`가 되돌린다.


그 밖의 다음 후보:
프리셋 탭에서 이어질 수 있는 것:

| 후보 | 내용 |
|---|---|
| 프리셋 내용의 손질 | 사용자가 써 보고 알려 오는 것. `tools/research/preset-build.ps1`의 표를 고치고 돌린 뒤 `preset-check.ps1 -Part preset-all`. 찾지 못한 이름(Spirit Sight, Rolling World)과 그림이라 읽지 못한 옵션(화영술·목화) |
| 프리셋 만들기 | 지금 캐릭터를 프리셋 파일로 내보내기(지금은 손으로 JSON을 쓴다) |
| 지우기 | 프리셋에 없는 기령·명우 떼기, 역천개명 비우기 버튼 |

그 밖의 후보(요청 6의 검토 결과, 2026-10-13):

| 후보 | 내용 | 비고 |
|---|---|---|
| 배율의 남은 조사 | 호감도, 영석·아이템 획득, 기예 경험치, 드롭률, 적 능력치, 전투 쿨타임 | 32절의 "걸리는 것을 확인하지 못한 지점" |
| 편의 묶음 F | 아이템·재화 줄이기, 현재값(체력·영력·기분·건강·나이) 직접 편집, 전투 쿨타임 없음·적 능력치 배율(전투 속도는 배율 탭에 들어갔다) | 지금 구조에 그대로 얹힌다. 게임에서 조사한 뒤 설계한다 |
| NPC·관계·문파 G | NPC의 가방 보기·주기, 관계 유형(친구·연인·배우자·사제·원한), 문파 직위·자원 | 양방향 참조가 걸려 범위와 위험이 크다. 조사부터 한다 |
| 첫 릴리스 | `develop` → `main` 병합, 태그, `README.md`의 설치·제거·사용법, 모드를 뺀 뒤의 세이브 동작 확인 | 사용자가 "나중에"로 미뤘다. 요청할 때 한다 |
| 그 밖 | 공법의 경지 승급·융합, 지역 해금, 즐겨찾기·단축키 변경·글자 크기, NPC를 대상으로 한 내보내기, "파일과 같게 맞추기", 대경지 7·8의 천재지보, 화면 크기 대응 | 기획서(`00-design-brief.md`)의 표와 대조한 것 |

사용자가 직접 써 보며 알려 오는 문제의 수정이 그보다 먼저다(지금까지의 작업 대부분이 그렇게 시작됐다).

### 8.4 진행 방식 (묶음 A~F2에서 쓴 것)

1. `/superpowers:brainstorming`으로 시작한다. 요청의 크기를 먼저 가른다: 작은 수정은 대화에서 짧은 설계를 보이고 승인받아 바로 하고(29·31절), 새 기능 묶음은 스펙 → 계획 → 실행을 거친다(A~D).
2. 질문은 선택지로 묻는다. 사용자는 짧게 답한다("진행", 선택지 고르기). 추천안을 첫 선택지에 둔다.
3. 만드는 방법이 게임의 실제 동작에 달려 있으면 명세를 쓰기 전에 게임에서 실험한다(사용자가 그렇게 골랐다). 실험 코드는 `Dev\`에 두고 별도 브랜치(`feat/*-research`)에 커밋만 한다.
4. 스펙(`docs/superpowers/specs/`) → 사용자 검토 → 계획(`plans/`) → 사용자 검토 → 실행. 실행 방식은 사용자가 매번 "Native"(이 세션에서 직접 구현하고 마지막에 검토자 한 번)를 골랐다.
5. 단위 시험이 없으므로 게임 안 검증 스크립트가 시험이다. 검사를 먼저 넣어 실패를 보고, 구현한 뒤 통과를 본다.
6. 병합 전에 검토자(서브에이전트, 읽기 전용) 한 명이 브랜치 전체를 본다. 묶음 C·D 모두 검토에서 실제 결함이 나왔다(재화가 두 번 담김, 경고를 성공으로 셈). 검토자가 "판단 보류"로 넘긴 것은 게임에서 실험해 가린다.
7. 끝나면 05 문서에 절을 추가하고 이 문서를 갱신한 뒤 `develop`에 병합하고 `tools/install.ps1`로 설치한다. 병합한 작업 브랜치는 지운다.
8. 삭제(브랜치, 복사본, 백업)와 `main` 병합은 사용자에게 하나씩 확인받는다.

화면을 새로 만들 때 지킬 것:

- 목록은 `ListView`, 검색은 `SearchBox`(검색어가 바뀌면 목록만 다시 그린다), 확인은 `Confirm`, 숫자는 `NumberField`, 여러 단계의 화면은 `EditorTab.Push`/`Pop`.
- `ListView.Build`에 주는 높이에는 아래 이동 줄(38 + 간격 6)이 들어간다. 줄 몇 개짜리 짧은 목록은 그만큼 더해야 줄이 보인다(37절).
- 목록의 글은 자르지 않는다. `Ui.Wrapped`로 줄바꿈하고 `ListView.HeightOf`에 `RowFor(최소, Ui.TextHeight(...))`를 준다. 줄에 바로 붙인 버튼은 `ListView`가 세로 가운데로 옮긴다.
- 버튼 이름에는 줄 번호가 아니라 항목의 번호를 붙인다. 이름의 앞머리가 다른 버튼과 겹치지 않게 한다(검증이 이름으로 찾는다).
- 서비스(`Services/`)는 화면을 모른다. 넣은 뒤에는 실제로 들어갔는지 다시 읽어 판정하고, 가져올 수 없는 줄은 이유를 줄에 보이고 버튼을 끈다.

### 8.5 사용자에게 확인을 부탁했고 아직 답을 받지 못한 것

도구로 시험할 수 없는 것들이다. 사용자가 써 본 이야기를 하면 05 문서의 해당 절 "확인하지 못한 것"에 결과를 적는다.

- 실제 마우스로 제목 줄 끌기, `Insert`를 빠르게 여러 번, 한글 입력기로 검색 칸에 입력, 마우스 휠과 스크롤 막대로 긴 목록 넘기기(26·27절. 휠은 가상 입력으로 통과, 51절). 창 위에서 실제 휠·오른쪽 끌기·호버가 뒤의 지도에 새지 않는지(51절 수정본을 설치한 뒤. 사용자가 본 "호버 효과"가 어느 경로였는지는 못 좁혔으니 남아 있으면 캡처).
- 강화·옵션이 붙은 장비의 내보내기·가져오기, 가져온 비급을 게임에서 배우기, 다른 세이브·새 인물로 옮기기(28·30절).
- 선력 0/0으로 적용해 둔 세이브: 프리셋의 `최종 단계까지`를 한 번 더 누르면 선력 6과 선법 "금홍파효"가 채워진다(35절). 결과를 들으면 35절에 적는다.
- 선법 탭: 익힌 선법을 게임의 선법 창에서 장착해 전투에서 썼을 때, NPC에게 익힌 선법이 효과가 있는지(36절).
- 신기 탭: 켠 신기가 다시 불러온 뒤 지도 화면에 나오고 쓸 수 있는지, 호천안에서 장착한 요수 기술과 호요를 전투에서 썼을 때, 요수를 모두 해금한 뒤 호천안의 "돌파"가 되는지, 신기별 전투 단축키와 쌍어패·연요호 칸 클릭을 실제로 써 봤을 때, "게임의 신기 키: 전투 중 끔"을 켠 채 게임 설정 창을 열었을 때 키 설정이 F로 남는지, 전투 뒤 지도 버튼이 다시 겹치면 `Latest.log`의 `artifact hud drift` 줄(37절).
- 프리셋 탭을 사용자가 실제로 써 본 이야기: 프리셋을 적용한 세이브로 플레이했을 때의 이상 여부, 신혼이 게임 화면의 어디에 나오는지, 기령 재능과 법보를 게임 화면에서 다뤘을 때, 한글 이름의 프리셋 파일을 직접 넣었을 때(34절).
- 배율 탭 전체를 사용자가 실제로 써 본 이야기(2026-10-13 22:15 설치본. 쿨타임과 받는 피해는 설치 뒤 아직 써 보지 않았다).
- 주는 피해 배율: 한 방에 죽지 않는 적을 x1과 x10으로 때렸을 때 화면의 피해 숫자가 달라지는지(33절. 도구로는 적의 체력이 곱한 만큼 깎이는 것까지 확인했다). 전투 속도의 체감, 기술 쿨타임 `없음`에서 신통이 연달아 나가는지(32·33절).
- 화폐 탭에서 명성을 100 이상으로 올린 뒤 낭야각의 "명성 100 도달 필요"가 풀리는지(29절. 저장값이 화면의 명성과 같다는 것까지만 도구로 확인했다).
- 경지를 가져온 인물로 계속 플레이했을 때의 이상 여부(30절).
- 자원 탭(38·39절): 사용자가 써 보고 알리기로 했다(2026-10-15). 비급은 게임 화면에서 눌러 학습(미니게임)하기, 상점에 팔기, NPC에게 주기, 품질 6을 여러 권 줄 때 걸리는 시간.
- 도호 탭(40절): 실제 마우스·한글 입력기, 게임 인물 화면의 도호 표시, 이야기 도호(결말 다섯, 단금쉬옥)를 줬을 때의 효과.
- 창작마당 모드(41절): 15개를 실제로 설치한 뒤 플레이했을 때의 이상 여부(DLL 모드 4개: Fatury, `2832167498`, `2839261049`, `3001496635`), 게임 주 화면의 모드 화면에서 Fatury가 맨 위인지(회차에서는 열지 못했다), 모드 공법·외형이 실제로 보이는지.
- 모드 공법 비급(42절): 게임에서 학습해 전투에서 썼을 때, 자원 탭 `모드만` 칩과 `[모드]` 줄을 실제 마우스로 써 봤을 때, 품질이 늘 3(절기)·5(무예)인 것이 게임 안에서도 그렇게 보이는지.
- 창작마당 모드 확장(45절): 새 모드 8개(도감 틀, 요도희, 극 NPC 실체화, 종족 틀, 종족 확장, 용족, 팔황 절색방, BGM 교체)를 설치한 뒤 플레이했을 때의 이상 여부, 캐릭터 생성의 종족 선택·요도희 획득(화봉주 종문)·절색방 극(주성 진입)·BGM이 실제로 보이고 들리는지, 기존 세이브에 어떻게 들어가는지. 모드 글은 46절의 번역으로 한국어로 보여야 한다. 캐릭터 생성의 종족 설명, 절색방 화면의 긴 안내문, 요도희의 대사에서 자리 표시가 풀린 문장 전체와 어색한 번역(데이터 폴더의 `translations\mods.txt`를 고치면 다음 실행부터 바뀐다).
- 창작마당 3차(50절): 11개(음양쌍수·환희연 강화판·운상요지·설정·농앵 동행 등)를 설치하고 플레이했을 때의 이상 여부, 극 대사의 긴 글과 `{name|B}`·`[A0]` 같은 자리 표시가 풀린 문장, 아이템·운명 이름이 한국어로 자연스러운지, 원문으로 둔 3행이 어디서 보이는지. 번역표가 든 DLL(95e2038)은 2026-10-17 07:06에 설치했다.
- 입화 몸 그림(52절): 캐릭터 생성창 의복은 11:1x에 정상 확인(해결). 아직 들을 것: NPC 대화(호청의)의 몸도 정상인지, 2815562168의 `Body\149`(깨진 PNG. 지금은 원작 101번 옷으로 보임)를 게임 폴더에서 뺄지 — 빼면 원작 149번이 쓰이지만 `workshop.ps1 -Install`이 되살린다.
- **2026-10-17 설치본(6c12d31, 16:40)의 확인 묶음**: 창 위에서 실제 휠·오른쪽 끌기·호버가 뒤의 지도에 새지 않는지(51절) / 캐릭터 만들기와 인물 화면의 종족 기술·변신 설명, 극 NPC 전용 운명 이름, 얼굴 만들기 확인 창, 영석 모드 단추가 한국어인지와 남은 중국어(54절. 남으면 캡처 → `rzhleft`) / 전투에서 신기 셋이 위로 쌓여 종족 기술 단추와 겹치지 않는지(55절) / 즐겨찾기 수량을 고치고 다른 곳을 눌러도 남는지(55절) / 기술 추가 목록에서 멸신참이 사라졌는지(55절) / 법보 탭에서 켠 재능·올린 레벨·넣은 기령이 전투와 게임의 기령 화면에 반영되는지, NPC를 대상으로 했을 때(56절).
- **2026-10-18 기예·공법·법보 추가(60·62절, 16:50 설치)의 확인 묶음**: 기예 탭 `제작 재료…`로 받은 재료로 게임에서 실제로 연단·연기·부적이 만들어지는지 / 공법 탭 학습 상한을 올린 뒤 비급으로 상한을 넘어 배우는지 / 법보 탭 수리 / 인물 화면을 게임에서 직접 열 때 동료 모드(TeamBest)의 예외가 나는지(60절).
- **2026-10-18 설치본(8500271)의 확인 묶음(57절)**: NPC 대화의 지점(자질·경지)과 파티 청하기, 플레이어 정보 화면의 종족 드롭다운, 마을 선거루에서 동부 사기·건물·곁채의 얼굴 만들기·동행, 일월천 이야기가 동작하는지와 한국어인지. DLL 안에 박힌 중국어(종족 드롭다운·동료 관계 글·동천복지 메뉴 일부)가 보이면 캡처 → `rzhleft` → `Translations/texts.txt`.

### 8.6 검토에서 사소하다고 보고 고치지 않은 것

사용자에게 보고했고 지시는 없었다.

- 묶음 A: `TimeService`가 HUD 갱신을 이력 기록보다 먼저 함, 세이브 진입 직후 60프레임 안에 가방 칸을 바꾸면 거짓 "(재조회 불일치)", `bag-slots.txt` 쓰기가 원자적이지 않음, Harmony 접두 함수에 예외 방어가 없음, `ModWarnings`가 "trampoline" 줄을 놓침.
- 묶음 B: 16:9보다 넓은 화면에서 창을 아래로 끌면 제목 줄이 화면 밖으로 나갈 수 있음, UI 카메라가 한 번 사라지면 다시 열 때까지 덧그리기 방식으로 남음, 삭제·교체 뒤 상세 칸에 이전 글이 남음, NPC 목록의 거리가 목록을 만든 시점의 값임, 숫자 칸의 안내 글이 빠짐, 변경 이력 파일을 탭에 들어올 때마다 통째로 읽음.
- 묶음 C: 모두 가져오기의 결과가 못 넣은 것의 이름만 보이고 수량은 안 보임, 화폐가 오르지 않은 경우도 "못 넣음"에 묶임, 아이템 수량의 `int` 넘침 검사 없음, 겹쳐지지 않는 아이템에 수량 2 이상을 적은 줄, Windows 예약 파일 이름.
- 묶음 D: 최고 경지는 수련치 상한이 없음, 검색 중에도 "이 항목 모두 가져오기"는 구역 전체를 가져옴, 원본이 같은 기술을 둘 가졌으면 모두 가져오기는 하나만 넣음, 기예 분류가 화면 이름으로 파일에 적힘, 서적 목록을 줄마다 다시 만듦, 수명은 년 단위로 옮겨져 개월 나머지가 버려짐, 편집기를 닫았다 열면 세부 화면이 남음.
- 묶음 F1(배율 탭): 전투 속도를 켠 채 지도에 있으면 매 프레임 전투 여부를 읽음, 속도의 복귀 값이 1로 고정(게임의 자체 속도 값을 덮어쓸 수 있음), 날짜 고정 중 이동 줄의 안내 문구("고정을 끈 뒤에 적용"이라 하지만 쌓이는 값에는 지금도 걸린다), `rates`의 x0 이동 검사의 걸음 수가 지형에 달림, 개발 명령의 인자 검사 없음, 게임 안 덧셈의 넘침은 방어하지 않음.
- 쿨타임·받는 피해: 받는 피해의 반올림이 짝수 반올림, 쿨타임이 켜진 전투에서 매 프레임 Il2Cpp 읽기와 null 검사 없음, 배포 구성으로 게임에 띄워 보지 않음(컴파일만 확인).
- 묶음 F2(프리셋 탭): 05 문서 34절의 "확인하지 못한 것, 한계" 끝에 모아 두었다(기본 프리셋을 되돌린 뒤 일치 확인 없음, 파일 읽기가 세 종류의 예외만 잡음, 모양이 틀린 줄의 이름이 "0", 상한을 넘은 능력치가 경지 올리기로 줄어듦, 품질 탐색의 4096회, "62 -> 62" 표기, 선천운명 칸 수 알림 없음, 상세보기의 낡은 사본). `preset-all` 검사는 실패하는 것을 보지 못한 채 넣었다(6절).
- 신기 탭: 05 문서 37절의 "확인하지 못한 것, 한계" 끝에 모아 두었다(경험치 채우기·손상 해제가 신기 보유를 보지 않음, `int`를 넘는 점수, 글 칸과 버튼 이름의 앞머리, 모두 해금의 긴 실패 문장, 검사의 `Traps`와 덜 본 버튼 상태, 호요 자료의 null 원소).
- 검토자의 "판단 보류"로 남은 것: 게임의 자체 속도 연출과 전투 속도의 충돌, 배율을 켠 채 달을 넘길 때의 부하, 다른 형식의 전투(`DLCUnitCtrlPlayer`), 한 전투 안에서 플레이어 단위가 바뀌는 경우의 쿨타임(32·33절의 "확인하지 못한 것").
- 검증: `ui-check.ps1 -Part wrap`의 "the last shown row is inside the list"가 한 번 실패했다가 다시 돌리자 통과했다(원인 미확인). `freeze` 뒤의 전투 진입 실패(6절).
- 자원 탭·비급·도호(38~40절): 각 절의 검토 문단과 "확인하지 못한 것, 한계"에 모아 두었다(즐겨찾기 보관본 덮어쓰기, 비급 모두 지급의 일부 지급 표시, 초기화의 범위, '제일' 도호를 옮기다 게임 예외가 나면 알림이 없음, 획득 창 번호 기억이 세이브를 바꿔도 남음, 가져오기 버튼 글 넘침 등).
- 한글화(43절): 게임의 기술 창·비급 설명에서 모드 공법의 이름·설명이 한국어로 보이는지(자리 표시가 풀린 전체 문장), 외형 모드의 안내문("용모를 바꾸시겠습니까?")이 어디서 뜨는지. 어색한 문장은 데이터 폴더의 `translations\mods.txt`를 고치면 다음 실행부터 바뀐다.
- 모드 신통(44절): 자원 탭에서 지급한 천살신창 비급을 읽어 배우고 전투에서 신통이 발동하는지. 옵션 번호를 모드 것으로 바꿔 만든 비급이라 옵션 글이 모드 공법 자신의 것으로 보여야 한다.

예전부터 남은 미확인: 신법·심법·신통의 모두 배우기, 부적의 강제 추가, NPC에게 추가한 기술의 저장 후 유지.
- 창작마당 확장(45절): 항목 페이지의 요청 제한 뒤 "이어 받기"가 없다(다시 돌리면 처음부터), 음악 질의(`音乐`·`BGM`·`特效`)가 설명 본문까지 맞춰 음악이 아닌 모드가 섞인다, 미등록 컬렉션은 API 비교가 안 된다(`-Doc` 4절 `조회 실패: result=9`), `-Status`의 DLL 수가 debug·ModProject 복사본까지 센다(2945768219가 15).
- 창작마당 3차(50절): 이름 키가 LocalText에 없는 모드 기술 168개가 공법·자원 탭에 "이름 없음(번호)"로 남는다(번역 밖. 모드 이름을 붙이거나 접는 옵션은 X 후보). 번역표의 절 머리 주석에 창작마당 번호를 더 적는 일은 하지 않았다(`# 3426669627 음양쌍수 …`).
- 2026-10-17 작업(51~56절): 52절의 `Body\149`(깨진 PNG)는 그대로 둠, 10:04 세션이 모드 24개를 읽은 까닭 미확인 / 54절의 `是的是的发` 한 줄(출처 미확인), `joinSchoolName` 4칸(종문 찾기 값 가능성으로 제외), 세이브 안 NPC 이름은 붙인 이름 줄이 있어야 바뀜 / 55절의 전투 불러오기 멈춤 한 번(재현 안 됨) / 56절의 NPC 대상 미시험.

### 8.7 남겨 둔 것

- 실험 브랜치 열넷(`feat/date-bag-research`, `feat/ui-rework-research`, `feat/bag-export-research`, `feat/char-export-research`, `feat/multiplier-research`, `feat/preset-research`, `feat/immortal-research`, `feat/artifact-research`, `feat/martialbook-research`, `feat/appellation-research`, `feat/workshop-research`, `feat/modskill-research`, `feat/korean-research`, `feat/ultimate-research`): 사전 실험 명령이 들어 있다. 사용자가 남기기로 했다. `develop`에 병합하지 않는다. 2026-10-22에 열다섯째 `feat/perf-research`(측정 명령 `rperf`, 12 문서), 열여섯째 `feat/mastery-research`(수련 단계 조사 명령 `ms*`, 05 문서 76절)를 더했다. 2026-10-23에 열일곱째 `feat/trainer3-research`(트레이너 3차 설계 전 실험 명령 `t3*`, 생성자도 읽는 `xrefl`, 05 문서 82절), 열여덟째 `feat/mastery2-research`(76절의 `ms*`에 제자리 끼워 넣기 실험 `msvals`·`msinsert`, 83절)를 더했다. 2026-10-27에 열아홉째 `feat/rate2-research`(배율 탭 확장 설계 전 실험 명령 `r2*`, 86절)를 더했다. 2026-10-28에 스물째 `feat/trainer4-research`(트레이너 4차 설계 전 실험 명령 `t4*`, 87절)를 더했다. 2026-10-28·29 브랜치 정리 때도 사용자의 앞 결정(2026-10-24)대로 남겼다.
- 모드의 자동 백업(`TlToybox\backups`, 약 100개, 약 300MB): 사용자가 지우지 않기로 했다. 고정본의 원본(`20261013-125814`)이 여기 있다.
- `.devremote`의 세이브 복사본: `-End`가 최근 다섯 회차의 것만 남긴다. 조사 산출물도 여기 있다: `zh-all.txt`·`zh-display-20261017.txt`·`zh-left.txt`(53·54절), `playerprefs-Mod_LoadData-*.txt`(52절), 캡처 `zh-1017*.png`·`check-treasure.png`·`fu-1017b-*.png`.
- 2026-10-17 저녁에 사용자 지시로 지운 브랜치·워크트리: `feat/zh-korean`·`docs/install-1017b`(워크트리 `zh-korean`), `fix/editor-followups`·`feat/treasure-tab`·`docs/install-1017c`(워크트리 `editor-followups`). 모두 develop에 병합된 뒤였다.
- 2026-10-18에 사용자 지시("브랜치 정리")로 지운 브랜치: `docs/mod-prereqs`(자기 커밋 없음. 메인 체크아웃을 `develop`으로 옮김), `feat/login-round`, `fix/editor-input-leak`. 모두 develop에 병합된 뒤였다. 남은 워크트리는 없다. 실험 브랜치 열넷은 병합하지 않은 커밋(1~19개)이 있어 그대로 둔다.
- 2026-10-18 오전 사용자 지시로 지운 브랜치: `feat/workshop-batch4`, `docs/handoff-1018`, `docs/collection-1018`(57절. 모두 develop에 병합된 뒤). 이 기록의 `docs/branches-1018b`도 병합 뒤 지웠다. 오후에는 병합 뒤 `feat/craft-cap-repair`는 남겨 두고(지울지 묻지 않음), `feat/craft-materials-exp`·`docs/craft-materials-spec`는 사용자의 "develop에 병합" 지시로 지웠다.
- 2026-10-13에 쓴 회차 이름(다시 쓰지 않는다): `rate1`~`rate2`, `preset1`~`preset2`, `ratetab1`~`ratetab11`(`5b`, `6b`~`6d` 포함), `dmg1`~`dmg3`, `cd1`, `cmb1`~`cmb9`. 
- 2026-10-14에 쓴 회차 이름: `grade-1014a`~`s`, `immo-1014a`~`b`, `hdr-1014a`~`c`, `imm-1014a`, `immtab1`~`immtab8`(`2b`~`2d`, `4b`·`4c` 포함), `immfix1`~`2`, `art-1014a`(게임이 켜져 있어 열리지 않았다. 다시 쓰지 않는다), `art-1014b`~`d`, `atab1`~`atab18`(`2b`~`2d`, `4b`~`4d`, `5b` 포함), `preset-1014a`~`d`, `ptab1`~`ptab25`(`8b`~`8e`, `21b` 포함), `pbuild1`~`pbuild27`(`2b` 포함), `awui-1014a`, `hud-1014a`~`d`, `bwui-1014a`~`b`, `bhud-1014a`~`d`, `bdev-1014a`(게임이 켜져 있어 열리지 않았다), `hudr-1014a`, `key-1014a`~`d`, `drift-1014a`~`b`, `akey-1014a`~`j`, `rate-1014a`~`b`, `cost-1014a`~`d`, `click-1014a`~`l`, `eyeclick-1014a`~`c`(`a`는 게임이 켜져 있어 열리지 않았다), `typing-1014a`~`k`, `month-1014a`. 다음은 날짜를 붙여 짓는 편이 안전하다(예: `fix-1015a`).
- 2026-10-15에 쓴 회차 이름: `res-1015a`~`q`, `book-1015a`~`o`(`e2`, `e3` 포함), `apl-1015a`~`f`, `ttl-1015a`~`o`, `apl-1015g`, `ws-1015a`~`b`.
- 2026-10-16에 쓴 회차 이름: `ws-1016a`~`w`, `ws-1017a`~`q`. 2026-10-16 밤~17일 새벽: `ws-1017r`~`s`.
- 2026-10-17 아침에 쓴 회차 이름: `fix-1017a`~`e`(입력 누출 수정 세션), `body-1017a`~`i`(입화 몸 그림 조사, 52절), `create-1017a`~`f`(캐릭터 만들기 의복 목록 확인, 52절. `-Login`으로 로그인 화면에서 멈춤), `rel-1017a`(배포 구성 확인, 52절). 2026-10-17 낮~오후: `zh-1017a`(53절 조사), `zh-1017b`~`h`(54절. `d`~`g`는 `-Login`), `fu-1017a`~`b`(55절. `a`는 전투 불러오기에서 멈춰 `-End`로 끝냈다), `tr-1017a`~`b`(56절).

- 2026-10-20 저녁 사용자 지시("브랜치 정리")로 지운 브랜치(모두 develop에 병합된 뒤): `docs/install-1018b`, `docs/install-1020`, `docs/review-1018`, `feat/action-day-rate`, `feat/craft-cap-repair`, `feat/preset-drafts`, `feat/study-gain`, `feat/study-time`, `feat/workshop-wangu`, `fix/channel-wheel`, `fix/review-1018-small`, `fix/wangu-table-texts`, 이 정리의 `docs/handoff-1020`.
- 2026-10-20에 쓴 회차 이름: `wg-1020a`~`w`(만고신화 71~74절. `p`~`w`는 `-Login`). 그 앞 65~70절의 회차 이름은 각 절에 있다.

- 2026-10-22에 쓴 회차 이름: `perf-1022a`~`c`(12 문서. 실험 브랜치 `feat/perf-research`의 개발 빌드), `ms-1022a`~`c`(05 문서 76절. 실험 브랜치 `feat/mastery-research`). 2026-10-23: `perf1-1023a`~`j`(05 문서 77절), `msl-1023a`~`k`(78절), `po-1023a`~`x`(`a2` 포함, 79절), `sf-1023a`~`c`(80절), `rf-1023a`~`f`(81절), `t3-1023a`~`h`·`tr3-1023a`~`v`(82절), `ms2-1023a`·`mf-1023a`~`q`(83절). 문서 브랜치 `docs/review-1022`는 병합 뒤 사용자 지시("브랜치 정리")로 지웠다. 실험 브랜치 `feat/perf-research`는 남긴다.
- 2026-10-21에 쓴 회차 이름: `wg-1021a`(75절, `-Login`). 병합을 마친 `fix/dress-list-missing`은 사용자 지시("병합을 끝낸 fix/dress-list-missing 브랜치를 지워 줘")로 지웠다. 이 정리의 `docs/handoff-1021`은 병합 뒤 지웠다. README와 이 문서 2·3·5절을 지금 구조에 맞춘 `docs/structure-1021`·`docs/tests-1021`도 병합 뒤 사용자 지시로 지웠다.

- 2026-10-28에 쓴 회차 이름: `t4-a`~`d-1028`(87절 설계 전 실험, 실험 브랜치 `feat/trainer4-research`), `t4-e`~`t-1028`(87절 검증. `e`는 실행 스크립트 문제로 일부만 돌았다). 2026-10-29에 사용자 지시("브랜치 정리")로 지운 브랜치(모두 develop에 병합된 뒤): `feat/trainer4-battle`, `docs/install-1028b`, 이 정리의 `docs/release-1029`.

### 8.8 설치 이력(2026-10-17~28, 최신이 위)

  - **2026-10-28 06:58 갱신(84~86절)**: 편집기 DLL을 `develop` 0402190으로 바꿨다 — 사용자 지시("설치")·게임 꺼짐 확인 뒤 `tools/install.ps1`, `installed build: 0402190`(3,025,408바이트). 바로 앞 설치본은 022d832(2026-10-23 22:18). 들어간 것: 미룬 사소한 것 묶음(85절: 모드 비급 경지 표기, 공법 탭 `대도까지 있음`, 법보·전승·대도 채우기의 되돌리기와 문구 등), 배율 탭 확장(86절: 분류 칩 다섯, 지도 속도·전투 이동 속도·호감 변화·정력 소모). 84절은 도구(`session.ps1 -NoEditor`)라 설치본과 상관없다. 게임에서 사용자가 쓰기 전의 확인은 회차(고정본)에서만 했다.
  - **2026-10-23 22:18 갱신(83.7절)**: 편집기 DLL을 `develop` 022d832로 바꿨다 — 사용자 지시("모드 기술 이름에 남는 COLORFFAA00…COLOR 색 표시도 고쳐 줘. 검증하고 develop에 병합한 뒤 설치까지")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 022d832`(3,017,216바이트). 바로 앞 설치본은 e0be677(21:53). 고친 것: 모드 기술 이름의 색 태그 글자(이름 다듬기 전에 태그 지움).
  - **2026-10-23 21:53 갱신(83.6절)**: 편집기 DLL을 `develop` e0be677로 바꿨다 — 사용자 지시("게임을 껐어. 검증하고 끝나면 develop에 병합하고 설치해 줘")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: e0be677`(3,017,216바이트). 바로 앞 설치본은 9c144ef(21:11). 고친 것: 공법 탭이 옵션을 6줄까지만 그리던 것(11줄까지).
  - **2026-10-23 21:11 갱신(83절)**: 편집기 DLL을 `develop` 9c144ef로 바꿨다 — 사용자 지시("feat/mastery-fill을 develop에 --no-ff로 병합하고 브랜치를 지운 뒤 … 게임에 설치해 줘")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 9c144ef`(3,017,216바이트). 바로 앞 설치본은 704a567(같은 날 19:54, 77~82절). 더해진 것: 공법 탭의 수련 단계 대도까지 채우기(83절).
  - **2026-10-23 19:54 갱신(77~82절)**: 편집기 DLL을 `develop` 704a567로 바꿨다 — 사용자 지시("설치 후 다음 단계 진행")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 704a567`(3,005,440바이트). 바로 앞 설치본은 e6902ea(2026-10-21 07:00). 들어간 것: 자원 탭 첫 열기 등 성능 개선(77·79절), 모드 공법 목록 정리(78절), 작은 수정(80절), 리팩토링 1차(81절. 같은 키 여러 줄의 `config.txt`는 뒤의 줄), 트레이너 3차 (가)(82절: 법보 강화 화면·지급의 경지·단계, 편집기 지급 법보의 비율 0 고침, 기령 친밀도, 전승 화면, 참오 결과 최대). 게임에서 사용자가 쓰기 전의 확인은 회차(고정본)에서만 했다.
  - **2026-10-21 07:00 갱신(75절)**: 편집기 DLL을 `develop` e6902ea로 바꿨다 — 사용자 지시("설치까지 진행 - 문서정리 - 핸드오프 - 커밋")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: e6902ea`(2,981,888바이트). 바로 앞 설치본 134197d(2026-10-20 20:07)와 배포 구성의 기능은 같다(그 뒤의 변경은 문서와 개발 명령 `rdress list`의 출력뿐이고, 개발 명령은 배포 빌드에서 빠진다). 134197d의 내용: 캐릭터 만들기 의복 빈 썸네일 채우기, 만고신화 하위 폴더 표 원문 3,271개 한글화(`tables.txt`, 첫 실행에 데이터 폴더에 풀림), 서식 태그를 벗겨 찾는 전체 일치(74절).
  - **2026-10-20 20:07 갱신(74절)**: 편집기 DLL을 `develop` 134197d(캐릭터 만들기 의복 빈 썸네일 채우기, 만고신화 하위 폴더 표 원문 3,271개 한글화 `tables.txt`, 서식 태그를 벗겨 찾는 전체 일치)로 바꿨다 — 사용자 지시("fix/wangu-table-texts를 develop에 --no-ff로 병합하고 게임 편집기 DLL을 갱신 설치해 줘")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 134197d`(2,981,888바이트). 바로 앞 설치본은 3d685b6(18:24). 첫 실행에 데이터 폴더 `translations\tables.txt`가 풀린다. 의복 목록 209~220번이 모두 같은 털옷인 것은 [剑来](2818763591)의 그림 14장이 같은 파일이라서다(74절).
  - **2026-10-20 18:24 갱신(72·73절)**: 편집기 DLL을 `develop` 3d685b6(만고신화 글 한글화 16,184행, 모드 기술 이름의 색 태그 지우기, 역천개명 대체 번호 `alt`, 기본 프리셋 17개에 21번 만고검결)로 바꿨다 — 사용자 지시("feat/workshop-wangu를 develop에 --no-ff로 병합하고 게임 편집기 DLL을 갱신 설치해 줘")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 3d685b6`(2,315,264바이트). 바로 앞 설치본은 555912a(14:36). 만고신화(2814696167)는 설치한 채 둔다(새 게임용, 73절). 다음 실행에서 프리셋 탭을 열면 기존 프리셋 폴더에 21번이 풀린다.
  - **2026-10-20 14:36 갱신(65~70절)**: 편집기 DLL을 `develop` 555912a(성격·아이템 소모 없음·가방 수량, 옵션 자리 강제·성공·드롭 탭, 기본 프리셋 16개와 `.defaults` 기록 포함)로 바꿨다 — 사용자 지시("게임 설치")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 555912a`(1,219,072바이트). 바로 앞 설치본은 708ec32(2026-10-19 15:53). 다음 실행에서 프리셋 탭을 열면 기존 프리셋 폴더에 11~16이 풀리고 `.defaults`가 생긴다(70절).
  - **2026-10-19 15:53 갱신(63·64절)**: 편집기 DLL을 `develop` 708ec32(배율 탭의 수련·학습 날짜 소모, 비급 학습 진도 포함)로 바꿨다 — 사용자 확인("게임은 꺼져 있어. develop의 DLL을 게임에 설치해 줘")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 708ec32`(1,185,792바이트). 바로 앞 설치본은 65fa68c(2026-10-18 16:50). 배율은 저장하지 않아 켤 때마다 1배.
  - **2026-10-18 16:50 갱신(59·60·62절)**: 편집기 DLL을 `develop` 65fa68c(법보 수리, 기술 학습 상한, 기예 제작 재료 지급 포함)로 바꿨다 — 사용자 확인("게임은 꺼져 있어. develop의 DLL을 게임에 설치해 줘")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 65fa68c`(1,183,744바이트). 바로 앞 설치본은 5a03fbe(12:18). 학습 상한은 기본(0)으로 시작한다(데이터 폴더에 `martial-grid.txt`가 없으므로). 창작마당 모드·`Mod_LoadData`는 아래 10:11 줄 그대로.
  - **2026-10-18 12:18 갱신(58·59절)**: 편집기 DLL을 `develop` 5a03fbe(리뷰 10의 작은 수정 묶음: 공법 탭 모드 기술 줄, 이력·설정 파일의 접근 거부, 돌파 마무리 로그, 타격 처리 보호, 표 칸 치환 조건, 화면 글 패치 재시도, 입력 차단 첫 예외 로그)로 바꿨다 — 사용자 확인("게임은 꺼져 있어. develop의 DLL을 게임에 설치해 줘")·게임 꺼짐 뒤 `tools/install.ps1`, `installed build: 5a03fbe-dirty`(1,163,264바이트. `-dirty`는 추적하지 않는 `tools/workshop/descriptions-20261017.txt`). 바로 앞 설치본은 8500271(10:11). 창작마당 모드·`Mod_LoadData`는 아래 10:11 줄 그대로.
  - **2026-10-18 갱신(57절)**: 편집기 DLL을 `develop` 8500271(4차 번역표 6,518행 포함)로 바꿨다 — 사용자 확인("다섯 개를 진행해 줘. 게임은 꺼 둘게")·게임 꺼짐 뒤 메인 체크아웃에서 `tools/install.ps1`, `installed build: 8500271-dirty`(1,162,240바이트. `-dirty`는 추적하지 않는 `tools/workshop/descriptions-20261017.txt` 때문). 게임을 켜면 `Init build=8500271-dirty`, 첫 실행에 데이터 폴더의 번역 파일에 243행이 덧붙는다. 창작마당 모드는 39개(아래 34개 + 4차 다섯: 2822252336 지점 `LE2lAt`, 3269125854 종족 수정 도구 `EUnXsf`, 2814673099 동료 `ahlMBV`, 2814686803 [大鬼] UI 틀 `GV2cem`, 2814717778 [大鬼] 동천복지 `8s4Ze4`), `Mod_LoadData`는 40개 모두 켜짐(UI 틀이 동천복지 앞. 앞 값 `.devremote\playerprefs-Mod_LoadData-before-ws-1018a.bin`). 목록 설치 39·보류 5·제외 1. 아래 줄의 6c12d31·34개·35개는 그 전 상태다.
  - 편집기 모드(2026-10-17 설치본): 배포 빌드(개발 도구 제외) `ModExportData\Mod_sWP6UL\ModCode\dll\MOD_sWP6UL.dll`. `develop` 6c12d31(56절 법보 탭, 55절 후속 셋, 54절 LocalText 밖 중국어 글 한글화, 51절 입력 누수 수정, 3차 번역표 6,275행 포함. 조사·검사용 개발 명령은 배포 빌드에서 빠짐)와 같다 — 2026-10-17 16:40에 사용자 지시·게임 꺼짐 뒤 워크트리 `.claude\worktrees\editor-followups`에서 develop을 체크아웃해 `tools/install.ps1`로 설치, `installed build: 6c12d31`(1,139,712바이트). 게임을 켜면 `Latest.log`의 `[TlToybox] Init build=6c12d31`과 `[TlToybox] texts: … tables=6 cells=131`로 확인할 수 있다(49·54절). 처음 켤 때 데이터 폴더에 `translations\texts.txt`(245줄)가 풀린다(앞 설치본에서 이미 풀렸으면 그대로). 바로 앞 설치본은 8f26b36(2026-10-17 14:38), 그 앞은 9cf06ae(11:42). 51절의 수정은 회차의 가상 입력으로만 검사했고 실제 마우스 확인은 사용자 몫이다(8.5절). 54절은 고정본 회차(캐릭터 만들기 화면의 종족 기술·변신 설명)에서만 확인했고 사용자 세이브에서의 확인은 사용자 몫이다.
  - 게임의 모드 순서(레지스트리 PlayerPrefs `HKCU\Software\guigugame\guigubahuang`의 `Mod_LoadData`): 2026-10-17 09시에 사용자 승인으로 **설치 모드 35개 모두 `isLoad:true`**로 바꿔 두었다(그 전에는 편집기 모드 하나뿐이라 Fatury 틀이 창작마당 입화를 싣지 않았다. 05 문서 52절). 원래 값은 `.devremote\playerprefs-Mod_LoadData-before-body-1017a.txt`. 사용자가 11:1x에 모드 창(11개 켜짐)과 캐릭터 생성창 의복 정상 출력을 확인했다(52절 결론). **새 창작마당 모드를 설치하면 모드 창에서 저장하거나 이 값을 다시 써야 Fatury가 그 모드의 그림을 싣는다.**
  - 창작마당 모드 34개: `ModExportData\<창작마당ID>\`(15개는 2026-10-16 오전, 8개는 같은 날 13:20, 11개(3차)는 같은 날 저녁에 사용자 확인을 받고 게임을 끈 상태에서 `pwsh tools/workshop.ps1 -Install -Yes`. 목록 `tools/workshop/mods.txt`(일곱 칸, 상태 설치 34·보류 3·제외 1), 이력 `history.txt`, 등록부 `docs/09-workshop-mods.md`(`-Doc`이 만든다), Steam 컬렉션 3820620999(미등록, `tools/workshop/collection.txt`). 05 문서 41·45·50절). DLL 모드 8개(Fatury·도감 틀·종족 틀·절색방·`MOD_hdUSic`(2833549873, 종류 `기능`) 등. 로드 중 `Latest.log`에 남는 `Fatury.ResLateLoadPatch`의 예외 한 줄은 그 모드의 것). 3차 모드의 글 5,287행은 50절에서 한글화했고, 그 번역표가 든 DLL(95e2038)을 2026-10-17 07:06에 설치했다. 데이터 폴더의 `translations\mods.txt`(824행)에는 다음 실행 때 새 키 5,451행이 덧붙는다(46절의 규칙).

## 9. 작업 규칙 요약

- 응답은 한국어. 게임 UI는 한글화되어 있으므로 화면 문구도 한국어로 말한다.
- 사용자에게 보이는 글(진행 보고, 설계 절, 질문 앞의 설명, 마지막 보고)은 모두 한국어로 쓴다. 화면 설계는 글로 그린 그림이 아니라 그림(위젯)으로 보인다(2026-10-15 사용자 지적 두 번).
- 브랜치: `develop`에서 `feat/*`·`fix/*`·`chore/*`·`docs/*`를 만들어 작업하고 `git merge --no-ff`로 `develop`에 병합한다. `main`·`develop`에 직접 커밋하지 않는다. 커밋 메시지 끝에 `Co-Authored-By` 줄을 붙인다.
- 게임 폴더는 읽기 전용이 원칙이다. 예외는 `ModExportData\Mod_sWP6UL\`(편집기 설치), `ModExportData\<창작마당ID>\`(창작마당 모드. `tools/workshop.ps1 -Install -Yes`, 사용자 확인 뒤), `Mods\TlToyboxDevRemote.dll`(조작 도구)뿐이다.
- 세이브를 바꾸는 시험은 `session.ps1`의 회차 안에서, 고정본으로 한다. 회차는 반드시 `-End`로 끝내고 "SAVE RESTORED AND IDENTICAL"을 확인한다.
- 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다. 임의로 끄지 않는다.
- 문서에는 어셈블리나 게임에서 실제로 확인한 이름만 적고, 확인하지 못한 것은 "미확인"으로 적는다.
- 게임에서 확인하지 않은 변경은 "미검증"이라고 밝히고, 돌리지 않은 검사와 통과시키지 못한 검사를 보고에 적는다.
