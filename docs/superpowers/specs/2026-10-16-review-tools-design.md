# 리뷰 남은 도구 묶음(9·19·17) 설계: `.devremote` 경로 한 곳, 커밋 표지, 시험 프로젝트 (2026-10-16)

## 1. 목적

리뷰 보고서 `docs/08-review-2026-10-16.md` 7.1절의 "도구(9·17·18·19)" 묶음 가운데 남은 셋. 18(검사의 빈틈)은 47·48절에서 끝났다.

| 항목 | 보고서 | 한 줄 |
|---|---|---|
| 9 | T1 | `.devremote`의 기준이 둘(절대 경로 박힘 / 저장소 뿌리에서 계산)이라 워크트리에서 어긋난다. 한 곳으로 모은다 |
| 19 | T8 | 설치본과 코드를 잇는 표지가 없다. 커밋 표지를 어셈블리에 넣고 `Init`에서 한 줄 찍는다 |
| 17 | T6 | 게임 없이 도는 시험이 없다. 게임 형식을 쓰지 않는 파일을 링크한 작은 시험 프로젝트를 `tests/`에 만든다 |

## 2. 결정

### 2.1 `.devremote` 경로 (9)

- 기준은 하나다: 환경 변수 `TLTOYBOX_DEVREMOTE`가 있으면 그 폴더, 없으면 `E:\TlToybox\.devremote`(고정). 저장소 위치에서 계산하지 않는다. 그래서 워크트리에서 돌려도 회차 표식·세이브 사본·캡처·명령 채널이 한 곳에 모이고, 두 워크트리가 동시에 회차를 열면 표식이 막는다(리뷰 T1).
- PowerShell: `tools/DevRemote/devremote-dir.ps1` 하나가 경로를 돌려준다(`& devremote-dir.ps1`). `session.ps1`(`-WorkDir`가 없을 때의 기본), `mod.ps1`, `rc.ps1`, `enter.ps1`, `tools/research/check-lib.ps1`(`$work`), `major-breakthrough-check.ps1`(`$S`), `preset-build.ps1`(`$dump`)이 그것을 쓴다. `-WorkDir`(시험용)은 그대로 우선한다.
- C#: `DevCommands.Dir`를 상수에서 속성으로(같은 규칙. 게임은 `session.ps1`이 `Start-Process`로 띄우므로 환경 변수를 물려받는다). `DevCommandsPreset`(`pdump`)·`DevCommandsSnap`(`snap`)의 박힌 경로는 `DevCommands.Dir`로. `tools/DevRemote/DevRemote.cs`(별도 어셈블리)는 같은 규칙을 세 줄로 따로 둔다.
- `Init` 로그에 `devremote=<경로> (env|default)`를 함께 찍는다(개발 빌드만. 어느 경로를 읽었는지 `Latest.log`로 안다).
- 인계 5절에 "회차는 어느 체크아웃에서 열어도 `.devremote`는 한 곳"을 적는다.

### 2.2 커밋 표지 (19)

- `ModMain.csproj`에 빌드 전 타깃 `StampCommit`: `git rev-parse --short HEAD`와 `git status --porcelain`으로 `InformationalVersion`을 `<짧은 커밋>` 또는 `<짧은 커밋>-dirty`로 둔다. git이 없거나 실패하면 `unknown`. `GenerateAssemblyInfo`가 속성 값으로 캐시를 만들므로 커밋이 바뀌면 다시 컴파일된다.
- `BuildInfo.Commit`(`AssemblyInformationalVersionAttribute`를 읽는다)로 `Init`이 `[TlToybox] Init build=<표지>`를 찍는다. 검사의 `ModWarnings`는 `Init` 머리를 이미 정상 줄로 본다.
- `tools/install.ps1`은 설치한 DLL의 `ProductVersion`(= InformationalVersion)을 끝에 찍는다. 인계 1절의 설치 줄에 그 표지를 적는다.

### 2.3 시험 프로젝트 (17)

- `tests/ModMain.Tests/ModMain.Tests.csproj`(net8.0 콘솔, C# 7.3, 패키지 없음). 소스 링크: `Services/Json.cs`, `Services/ExportFiles.cs`, 새 `Services/TranslationTable.cs`. `ExportFiles`가 쓰는 `ChangeLog.DataDir`은 시험 쪽 `Stubs/ChangeLog.cs`(임시 폴더)로 대신한다.
- `TranslationService`의 순수 부분을 `TranslationTable`로 뗀다: `Parse(string[] lines, out int broken)`(지금 `Load`의 규칙: `#`·빈 줄 무시, 탭 없는 줄은 broken, 키·글 trim, `\n` → 줄바꿈, 같은 키는 뒤의 것), `KeyOf(string line)`, `MissingLines(string[] fileLines, string embedded)`(내장본에만 있는 키의 줄). `TranslationService.Load`·`AppendMissing`·`KeyOf`는 그것을 부른다(동작 그대로. 게임 안 검사 `translation` 27개가 그대로 통과해야 한다).
- 실행기: `Program.cs`가 줄마다 `PASS/FAIL`, 끝에 `FAILS=<수>`를 내고 실패가 있으면 종료 코드 1(저장소의 ps1 시험과 같은 꼴). 실행 `dotnet run --project tests/ModMain.Tests -c Release`.
- 시험 내용(각각 한 가지): Json — 객체·배열·문자열·수(정수 long/소수 double/음수/지수)·true/false/null, 이스케이프(`\n`·`\"`·`\\`·`\u`), `\u` 네 자리가 아니면 FormatException, 깊이 64 초과 FormatException(65겹 배열), 값 뒤의 글 FormatException, BOM 허용, `Quote` 왕복. ExportFiles — `Text`(없음·형 다름 → ""), `Whole`(없음·소수·int 범위 밖 → false), `Ints`(없음 → 빈 배열과 true, 정수 아닌 요소 → false), `Items`(객체 아닌 요소 → null), `FileNameFor`(금지 글자 → `_`, 앞뒤 공백·끝 점 제거). TranslationTable — 주석·빈 줄 무시, 탭 없는 줄 broken, trim, `\n` 치환, 같은 키는 뒤의 것, `MissingLines`가 새 키만.

## 3. 검증

- 도구 시험(게임 없음): 새 `tools/DevRemote/devremote-dir.tests.ps1`(기본값, 환경 변수, `mod.ps1`·`rc.ps1`·`session.ps1 -DryRun`·`check-lib.ps1`이 그 폴더를 쓰는지), 새 `tools/build-tag.tests.ps1`(배포 빌드의 `ProductVersion`이 `git rev-parse --short HEAD`[-dirty]), `dotnet run --project tests/ModMain.Tests`. 기존 `session.tests.ps1`(34)·`workshop.tests.ps1`(78)은 그대로 통과.
- 회차 `ws-1017o`(고정본): `base`에 "Init 줄에 build=<7자리 이상 16진수>"와 "Init 줄에 devremote=… (default)"를 더해 통과, `translation` 27 그대로(TranslationTable 분리의 회귀 없음). 환경 변수 경로는 회차 밖에서 도구 시험으로만 본다(게임까지 띄우는 것은 고정본 위치가 달라져 비싸다).

## 4. 문서

05 문서 49절(세 항목의 규칙·실측·검사), 인계 1절(설치 DLL: 2026-10-16 19:30, 48절까지, 표지), 3절(`tests/`가 비어 있다는 문장, `docs/superpowers/specs` 목록), 5절(`.devremote` 한 곳, 도구 시험 셋), 8.1 표(Y 도구 묶음), 8.7 회차 이름.

## 5. 범위 밖

T2(`major-breakthrough-check.ps1`의 구조), T3(시험 도우미 중복), T4(`ui-check.ps1` 분할), T5(`$log` 게임 경로), 게임 형식을 매개변수로 받게 바꿔야 하는 나머지 순수 논리(`ItemGroups`·`GradeService.Path`·`FavoriteStore.Parse`·`MartialService.Requires`).

## 6. 확인하지 못한 것, 위험

- 환경 변수를 다른 경로로 두고 게임까지 띄우는 경우는 도구 시험으로만 본다(명령 채널 파일이 그 폴더에 생기는지). 게임 쪽은 기본값 경로를 `Init` 로그로 본다.
- `git status --porcelain`이 빌드마다 돈다(수십 ms). git이 없는 환경은 `unknown`.
- `TranslationTable` 분리는 동작을 바꾸지 않아야 한다. 게임 안 검사 `translation`으로 본다.
