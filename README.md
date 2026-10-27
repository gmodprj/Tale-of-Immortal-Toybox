# TlToybox

귀곡팔황(Tale of Immortal) 통합 치트·샌드박스 모드 `GGBH Cheat Sandbox`.

현재 단계: 편집기 v0.1 — 게임 안에서 `Insert`로 여닫는 창. 탭 16개(능력치·자원·보관함·경지·공법·선법·신기·운명·기예·NPC·세계·배율·프리셋·도호·법보·성공·드롭)와 제목 줄의 안전 단추가 있다. 공법 탭은 수련 단계를 대도까지 채우고, 법보 탭은 경지·단계(한계돌파)를 바꾼다. 배율 탭은 분류 칩 다섯에 18줄(지도 속도·전투 이동 속도·호감 변화·정력 소모 포함)이다. 창작마당 모드를 받아 설치하는 도구와, 설치한 모드 글의 한글화(번역표 셋)도 함께 있다. 처음 보는 사람은 `docs/06-handoff.md`부터 읽는다(다음에 할 일은 8.0절). 2026-10-24에 `develop`을 `main`에 처음 병합했고(첫 릴리스), 2026-10-28에 두 번째로 병합했다. 기능별 범위와 실험 기록은 `docs/05-editor-v0.1.md`.

## 구조

| 경로 | 내용 |
|---|---|
| `docs/00-design-brief.md` | 기획서 |
| `docs/01-game-structure.md` | 설치본 구조 |
| `docs/02-mod-dev-research.md` | 모드 개발 구성과 경로 판정 |
| `docs/02a-workshop-survey.md` | 창작마당·모드 사이트 조사표 |
| `docs/04-ingame-state-analysis.md` | 인게임 상태 구성 분석 |
| `docs/05-editor-v0.1.md` | 편집기 v0.1의 범위, 사용법, 검증 결과(절 단위 기록) |
| `docs/06-handoff.md` | 작업 인계: 현재 상태, 검증 방법, 남은 일 |
| `docs/07-divine-artifacts.md` | 시작 신기 셋(쌍어패·호천안·연요호) 조사 |
| `docs/08-review-2026-10-16.md`, `docs/10-review-2026-10-18.md`, `docs/12-review-2026-10-22.md` | 편집기 전체 리뷰, 코드 리뷰와 리팩토링 검토, 코드 리뷰·성능 측정과 개선 계획 |
| `docs/09-workshop-mods.md` | 창작마당 모드 등록부(`tools/workshop.ps1 -Doc`이 만든다) |
| `docs/11-trainer-comparison.md` | 외부 트레이너(ggbh-trainer 0.8.30)와의 기능 비교 |
| `docs/superpowers/specs/`, `plans/` | 작업 단위의 설계 스펙과 구현 계획 |
| `src/ModCode/ModMain/` | 코드 모드 프로젝트(편집기). 번역표(`Translations/`)와 기본 프리셋(`Presets/`)을 DLL 안에 넣는다 |
| `src/ModExcel/`, `src/ModAssets/` | 비어 있다(`.gitkeep`만). 모드 에디터 프로젝트의 설정 테이블·리소스 자리 |
| `tests/ModMain.Tests/` | 게임 없이 도는 단위 시험 |
| `tools/install.ps1` | 배포 빌드를 만들어 게임에 설치 |
| `tools/workshop.ps1`, `tools/workshop/` | 창작마당 모드 받기·설치·검색·등록부 도구와 모드 목록 |
| `tools/DevRemote/` | 개발용 게임 조작 도구. `session.ps1`이 게임 검증 한 회차(세이브 복사 → 실행 → 복원)를 묶는다 |
| `tools/research/` | 게임 어셈블리 조사 스크립트, 게임 안 자동 검사(`ui-check.ps1` 등), 번역 묶음 합치기 |

`src/`는 게임 모드 프로젝트의 `ModProject` 폴더에 대응한다.

## 빌드

요구 사항: .NET SDK 8, MelonLoader가 포함된 게임 설치본.

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release                       # 개발 빌드(개발 명령 포함)
    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release -p:DevTools=false     # 배포 구성
    dotnet run --project tests/ModMain.Tests -c Release                              # 단위 시험

산출물은 `src/ModCode/ModMain/bin/Release/MOD_sWP6UL.dll` 하나다. Release 빌드는 DLL을 모드 에디터 프로젝트(`ModProject_sWP6UL/`)의 `bin/Release`로도 복사한다. 평소에는 `tools/install.ps1`이 배포 빌드를 게임의 `ModExportData\Mod_sWP6UL`에 설치한다.
