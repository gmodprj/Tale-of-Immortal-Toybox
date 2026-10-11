# TlToybox

귀곡팔황(Tale of Immortal) 통합 치트·샌드박스 모드 `GGBH Cheat Sandbox`.

현재 단계: 사전 조사 완료, 기능 구현 전.

## 구조

| 경로 | 내용 |
|---|---|
| `docs/00-design-brief.md` | 기획서 |
| `docs/01-game-structure.md` | 설치본 구조 |
| `docs/02-mod-dev-research.md` | 모드 개발 구성과 경로 판정 |
| `docs/02a-workshop-survey.md` | 창작마당·모드 사이트 조사표 |
| `docs/04-ingame-state-analysis.md` | 인게임 상태 구성 분석 |
| `src/ModCode/ModMain/` | 코드 모드 프로젝트(현재 빈 진입점만 있음) |
| `src/ModExcel/` | 설정 테이블 |
| `src/ModAssets/` | 리소스 |

`src/`는 게임 모드 프로젝트의 `ModProject` 폴더에 대응한다.

## 빌드

요구 사항: .NET SDK 8, MelonLoader가 포함된 게임 설치본.

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release

산출물은 `src/ModCode/ModMain/bin/Release/MOD_TlTbx0.dll`이다. 모드 ID `TlTbx0`는 임시값이며, 게임 폴더로의 배포는 아직 하지 않는다.
