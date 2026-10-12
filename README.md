# TlToybox

귀곡팔황(Tale of Immortal) 통합 치트·샌드박스 모드 `GGBH Cheat Sandbox`.

현재 단계: 편집기 v0.1 — 능력치·자원·경지·공법·운명·기예·NPC·세계·안전의 아홉 탭. 처음 보는 사람은 `docs/06-handoff.md`부터 읽는다(다음에 할 일은 8절). 기능별 범위와 실험 기록은 `docs/05-editor-v0.1.md`.

## 구조

| 경로 | 내용 |
|---|---|
| `docs/00-design-brief.md` | 기획서 |
| `docs/01-game-structure.md` | 설치본 구조 |
| `docs/02-mod-dev-research.md` | 모드 개발 구성과 경로 판정 |
| `docs/02a-workshop-survey.md` | 창작마당·모드 사이트 조사표 |
| `docs/04-ingame-state-analysis.md` | 인게임 상태 구성 분석 |
| `docs/05-editor-v0.1.md` | 편집기 v0.1의 범위, 사용법, 검증 결과 |
| `docs/06-handoff.md` | 작업 인계: 현재 상태, 검증 방법, 남은 일 |
| `docs/superpowers/specs/`, `plans/` | 작업 단위의 설계 스펙과 구현 계획 |
| `tools/research/` | 게임 어셈블리 조사 스크립트 |
| `src/ModCode/ModMain/` | 코드 모드 프로젝트(편집기) |
| `tools/DevRemote/` | 개발용 게임 조작 도구. `session.ps1`이 게임 검증 한 회차(세이브 복사 → 실행 → 복원)를 묶는다 |
| `src/ModExcel/` | 설정 테이블 |
| `src/ModAssets/` | 리소스 |

`src/`는 게임 모드 프로젝트의 `ModProject` 폴더에 대응한다.

## 빌드

요구 사항: .NET SDK 8, MelonLoader가 포함된 게임 설치본.

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release

산출물은 `src/ModCode/ModMain/bin/Release/MOD_sWP6UL.dll`이다. Release 빌드는 DLL을 모드 에디터 프로젝트(`ModProject_sWP6UL/`)의 `bin/Release`로도 복사한다. 평소에는 `tools/install.ps1`이 배포 빌드를 게임의 로컬 모드 폴더에 설치한다.
