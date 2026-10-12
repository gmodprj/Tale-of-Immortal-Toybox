# TlToybox

귀곡팔황(Tale of Immortal) 통합 치트·샌드박스 모드 `GGBH Cheat Sandbox`의 작업 저장소.

## 브랜치 전략

- `main`은 보호 브랜치다. 직접 커밋하지 않는다.
- `develop`이 통합 브랜치다. 직접 커밋하지 않는다.
- 작업은 `develop`에서 분기한 `feat/*`, `fix/*`, `chore/*`, `docs/*` 브랜치에서 한다.
- 현재는 원격 저장소가 없다. 작업 브랜치는 로컬에서 `git merge --no-ff`로 `develop`에 병합한다.
- `main`에는 릴리스 시점에 `develop`만 병합한다.

## 게임 폴더 규칙

- 게임 설치 경로: `E:\Games\Tale of Immortal`
- 게임 폴더는 읽기 전용으로 다룬다. 빌드 산출물을 게임 폴더로 복사하는 일은 사용자 확인을 받은 뒤에만 한다.
- 사용자 승인(2026-10-12)으로 편집기 모드를 게임의 `ModExportDataMod_sWP6UL`에 설치해 두었다. 갱신은 `tools/install.ps1`로 한다.
- 예외: 개발용 조작 도구 `tools/DevRemote`의 DLL이 게임의 `ModsTlToyboxDevRemote.dll`로 설치되어 있다(2026-10-12 사용자 지시). 게임 창을 백그라운드에서 조작하는 용도이며 `tools/DevRemote/rc.ps1`로 쓴다. 지우면 원상태가 된다.
- 게임 DLL을 저장소에 복사하지 않는다. 참조는 `$(GameDir)\MelonLoader`를 가리킨다.
- 세이브 파일을 수정하는 실험은 별도 테스트 세이브에서만 한다.

## 모드 규약

- 어셈블리 이름과 네임스페이스는 `MOD_<모드ID>`, 진입점은 `ModMain` 클래스의 `Init()`·`Destroy()`다(`docs/02-mod-dev-research.md` 3절).
- 모드 ID는 게임 내 모드 에디터가 발급한 `sWP6UL`이다. 에디터 프로젝트는 `ModProject_sWP6UL/`에 있고 git에서 제외한다. Release 빌드가 DLL을 그 프로젝트의 `ModCode/ModMain/bin/Release`로 복사한다.
- `bin\Release`에는 `MOD_<모드ID>.dll` 하나만 있어야 한다. 참조를 추가할 때 `<Private>False</Private>`를 지정한다.

## 문서 규칙

- 클래스명·필드명·메서드명은 어셈블리 또는 API 문서에서 실제로 확인한 것만 적는다.
- 확인하지 못한 항목은 "미확인"과 사유를 적는다.

## 빌드

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Release

게임 경로가 다르면 `-p:GameDir="<경로>"`를 붙인다.
