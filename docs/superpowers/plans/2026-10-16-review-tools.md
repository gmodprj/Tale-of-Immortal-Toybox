# 리뷰 남은 도구 묶음(9·19·17) 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `.devremote` 경로를 한 곳으로, 커밋 표지를 어셈블리와 `Init` 로그에, 게임 없이 도는 시험 프로젝트를 `tests/`에.

**Architecture:** 경로는 PowerShell 쪽 `devremote-dir.ps1` 하나와 C# 쪽 `DevCommands.Dir` 하나가 같은 규칙(환경 변수 `TLTOYBOX_DEVREMOTE`, 기본 `E:\TlToybox\.devremote`)을 갖는다. 커밋 표지는 csproj 타깃이 `InformationalVersion`에 넣고 `BuildInfo`가 읽는다. 시험 프로젝트는 순수 소스 셋을 링크하고 패키지 없이 PASS/FAIL 실행기로 돈다. `TranslationService`의 순수 부분은 `TranslationTable`로 뗀다.

**Tech Stack:** PowerShell 7, C# 7.3(net472 모드 / net8.0 시험), MSBuild Exec, git.

**Spec:** `docs/superpowers/specs/2026-10-16-review-tools-design.md`

## Global Constraints

- 기본 경로 `E:\TlToybox\.devremote`, 환경 변수 `TLTOYBOX_DEVREMOTE`. `-WorkDir`(시험용)은 그대로 우선.
- `Init` 로그: `[TlToybox] Init build=<표지> devremote=<경로> (env|default)`(devremote는 DEVTOOLS에만).
- 시험은 줄마다 `PASS/FAIL`, 끝에 `FAILS=<수>`, 실패면 종료 코드 1.
- 두 구성 빌드 경고 0. 게임 안 검사 `translation`은 그대로 27 PASS.
- 회차 이름 `ws-1017o`. 커밋 끝 `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.

## Review Focus

1. 환경 변수에 끝 역슬래시나 따옴표가 섞여 있어도 `Join-Path`·`Path.Combine`이 맞게 붙는다. → Task 1 시험에 끝 역슬래시 경우를 넣는다.
2. git이 없는 셸에서 빌드하면 `unknown`으로 빌드가 멈추지 않는다. → Task 2에서 `IgnoreExitCode`·`ContinueOnError`로. 시험은 하지 않는다(코드로).
3. 커밋만 바뀌고 소스가 그대로인 빌드가 새 표지를 받는다. → Task 2 시험이 빈 커밋을 만들지 않고 "dirty" 전환(파일 하나 건드림)으로 본다.
4. `TranslationTable.Parse`가 `Load`와 같은 결과(기존 `translation` 검사의 824행·broken 1·unused 1 수치). → Task 4 회차.
5. 시험 프로젝트가 모드 소스를 링크하므로 모드 소스의 `internal`에 닿는다. 모드 소스에 Unity 형식을 쓰는 줄이 섞이면 시험 빌드가 깨진다. → Task 3에서 링크 대상을 셋으로 못박고 `ChangeLog`만 스텁.

---

### Task 1: `.devremote` 경로 한 곳 (9)

**Files:**
- Create: `tools/DevRemote/devremote-dir.ps1`, `tools/DevRemote/devremote-dir.tests.ps1`
- Modify: `tools/DevRemote/session.ps1:28`, `mod.ps1:3`, `rc.ps1:2`, `enter.ps1:5`, `tools/research/check-lib.ps1:5`, `major-breakthrough-check.ps1:4`, `preset-build.ps1:9`
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs:14`, `DevCommandsPreset.cs:811`, `DevCommandsSnap.cs:29`, `tools/DevRemote/DevRemote.cs:20`, `src/ModCode/ModMain/ModMain.cs`(Init 로그)

- [ ] **Step 1: 시험을 쓴다(RED)**

`tools/DevRemote/devremote-dir.tests.ps1`:
```powershell
# devremote-dir.ps1(.devremote 경로 한 곳)의 시험. 게임을 띄우지 않고 임시 폴더에서 한다. 05 문서 49절.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다.
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$resolver = Join-Path $here 'devremote-dir.ps1'
$script:fails = 0
function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
# 환경 변수를 바꿔 자식 pwsh에서 돌린다(이 셸의 값은 건드리지 않는다).
function InEnv([string]$value, [string]$command) {
  $prefix = if ($null -eq $value) { 'Remove-Item Env:TLTOYBOX_DEVREMOTE -ErrorAction SilentlyContinue; ' } else { "`$env:TLTOYBOX_DEVREMOTE = '$value'; " }
  (& pwsh -NoProfile -Command ($prefix + $command) 2>&1) -join "`n"
}
$box = Join-Path $env:TEMP ('tltoybox-devremote-tests-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($box)
try {
  Check 'resolver exists' (Test-Path $resolver)
  Check 'default is the fixed folder' ((InEnv $null "& '$resolver'") -eq 'E:\TlToybox\.devremote')
  Check 'the environment variable wins' ((InEnv $box "& '$resolver'") -eq $box)
  Check 'a trailing backslash is trimmed' ((InEnv ($box + '\') "& '$resolver'") -eq $box)
  # mod.ps1·rc.ps1은 명령 파일을 그 폴더에 쓴다(게임이 없으니 결과는 오지 않는다. 짧게 기다린다)
  InEnv $box "& '$here\rc.ps1' -Cmd 'hit 1 1' -NoShot -WaitMs 50" | Out-Null
  Check 'rc.ps1 writes its command into the folder' (Test-Path (Join-Path $box 'cmd.txt'))
  InEnv $box "& '$here\mod.ps1' -Cmd 'uistate' -WaitMs 50" | Out-Null
  Check 'mod.ps1 writes its command into the folder' (Test-Path (Join-Path $box 'mod-cmd.txt'))
  # session.ps1은 -WorkDir가 없으면 그 폴더에 회차 표식을 둔다(-DryRun, 세이브·게임은 임시 폴더)
  $save = Join-Path $box 'save\CacheData'; $data = Join-Path $box 'data'; $game = Join-Path $box 'game'
  foreach ($d in "$save\slot1", $data, "$game\ModExportData\Mod_sWP6UL") { [void][IO.Directory]::CreateDirectory($d) }
  [IO.File]::WriteAllText("$save\DataGloble.cache", 'g'); [IO.File]::WriteAllText("$save\slot1\a.cache", 'a')
  [IO.File]::WriteAllText("$game\ModExportData\Mod_sWP6UL\ModExportData.cache", 'ours')
  $o = InEnv $box "& '$here\session.ps1' -DryRun -Begin t1 -SaveDir '$save' -DataDir '$data' -GameDir '$game' -ProcessName no-such-process-tltoybox"
  Check 'session.ps1 keeps its marker in the folder' (Test-Path (Join-Path $box 'session.name')) $o
  # check-lib.ps1은 $work를 그 폴더로 둔다(표식이 있어야 읽힌다)
  $lib = Join-Path (Split-Path -Parent $here) 'research\check-lib.ps1'
  $o = InEnv $box ". '$lib'; `$work"
  Check 'check-lib.ps1 works in the folder' ($o.Trim() -eq $box) $o
} finally {
  if (Test-Path $box) { Remove-Item -LiteralPath $box -Recurse -Force }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
```

- [ ] **Step 2: RED 실행**

Run: `pwsh tools/DevRemote/devremote-dir.tests.ps1`
Expected: 'resolver exists' FAIL부터(파일 없음). 'rc.ps1 writes…'·'mod.ps1 writes…'·'session.ps1 keeps…'·'check-lib…' FAIL(고정 경로·저장소 경로를 쓴다).

- [ ] **Step 3: 해결자와 스크립트**

`tools/DevRemote/devremote-dir.ps1`:
```powershell
# .devremote 폴더(회차 표식·세이브 사본·캡처·명령 채널)의 기준 하나. 환경 변수 TLTOYBOX_DEVREMOTE가 있으면 그 폴더, 없으면 E:\TlToybox\.devremote.
# 저장소 위치에서 계산하지 않는다: 워크트리에서 돌려도 한 곳에 모이고, 두 체크아웃이 동시에 회차를 열면 표식이 막는다(리뷰 T1, 05 문서 49절).
# C# 쪽(DevCommands.Dir, DevRemote.cs)도 같은 규칙이다.
$value = $env:TLTOYBOX_DEVREMOTE
if ($value) { $value.Trim().TrimEnd('\', '/') } else { 'E:\TlToybox\.devremote' }
```
각 스크립트: `$dir = 'E:\TlToybox\.devremote'` → `$dir = & (Join-Path $PSScriptRoot 'devremote-dir.ps1')`(mod·rc·enter). `session.ps1:28`: `$dir = if ($WorkDir) { $WorkDir } else { & (Join-Path $PSScriptRoot 'devremote-dir.ps1') }`. `check-lib.ps1:5`: `$work = & (Join-Path (Split-Path -Parent $PSScriptRoot) 'DevRemote\devremote-dir.ps1')`. `major-breakthrough-check.ps1:4`: `$S = & "$T\devremote-dir.ps1"`. `preset-build.ps1:9`: `$dump = & (Join-Path $PSScriptRoot '..\DevRemote\devremote-dir.ps1')`. 머리 주석의 경로 설명도 "한 곳(devremote-dir.ps1)"으로.

- [ ] **Step 4: C#**

`DevCommands.cs:14`:
```csharp
        // .devremote 폴더(명령 채널·덤프). 환경 변수 TLTOYBOX_DEVREMOTE가 있으면 그 폴더, 없으면 기본. tools/DevRemote/devremote-dir.ps1과 같은 규칙(05 문서 49절).
        private const string DefaultDir = @"E:\TlToybox\.devremote";
        public static string Dir
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("TLTOYBOX_DEVREMOTE");
                return string.IsNullOrEmpty(value) ? DefaultDir : value.Trim().TrimEnd('\\', '/');
            }
        }
        public static bool DirFromEnv
        {
            get { return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TLTOYBOX_DEVREMOTE")); }
        }
```
`DevCommandsPreset.cs:811`·`DevCommandsSnap.cs:29`의 `@"E:\TlToybox\.devremote"` → `DevCommands.Dir`. `DevRemote.cs:20`: `CommandDir`를 같은 규칙의 정적 속성으로(세 줄). `ModMain.Init`의 로그는 Task 2에서 함께 바꾼다(`devremote=` 부분).

- [ ] **Step 5: GREEN**

Run: `pwsh tools/DevRemote/devremote-dir.tests.ps1`; `pwsh tools/DevRemote/session.tests.ps1 | Select-Object -Last 1`; 두 구성 빌드; `dotnet build tools/DevRemote/DevRemote.csproj -c Release`
Expected: FAILS=0 / FAILS=0 / 경고 0 / 경고 0.

- [ ] **Step 6: 커밋** `chore: .devremote 경로를 한 곳으로(devremote-dir.ps1·DevCommands.Dir, 환경 변수 TLTOYBOX_DEVREMOTE)(리뷰 9)`

---

### Task 2: 커밋 표지 (19)

**Files:**
- Create: `src/ModCode/ModMain/BuildInfo.cs`, `tools/build-tag.tests.ps1`
- Modify: `src/ModCode/ModMain/ModMain.csproj`, `ModMain.cs:13`, `tools/install.ps1`, `tools/research/ui-check.ps1`(`base`)

- [ ] **Step 1: 시험(RED)** — `tools/build-tag.tests.ps1`:
```powershell
# 배포 빌드에 커밋 표지가 들어가는지(InformationalVersion = <짧은 커밋>[-dirty]). 게임을 띄우지 않는다. 05 문서 49절.
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$script:fails = 0
function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
$out = Join-Path $env:TEMP ('tltoybox-build-tag-' + [guid]::NewGuid().ToString('N'))
try {
  $commit = (& git -C $repo rev-parse --short HEAD).Trim()
  $dirty = if ((& git -C $repo status --porcelain) -join '') { '-dirty' } else { '' }
  dotnet build (Join-Path $repo 'src\ModCode\ModMain\ModMain.csproj') -c Release --nologo -p:DevTools=false -o $out | Out-Null
  $dll = Join-Path $out 'MOD_sWP6UL.dll'
  $ver = (Get-Item $dll).VersionInfo.ProductVersion
  Check 'the release build carries the commit' ($ver -eq "$commit$dirty") "ProductVersion=$ver expected=$commit$dirty"
  Check 'the tag is a short hash' ($ver -match '^[0-9a-f]{7,}(-dirty)?$') $ver
} finally {
  if (Test-Path $out) { Remove-Item -LiteralPath $out -Recurse -Force }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
```
Run → Expected: FAIL 'the release build carries the commit' (`ProductVersion=1.0.0`).

- [ ] **Step 2: csproj 타깃** — `</Project>` 앞:
```xml
  <!-- 커밋 표지: InformationalVersion = <짧은 커밋>[-dirty]. Init 로그와 설치 스크립트가 읽는다(05 문서 49절). git이 없으면 unknown. -->
  <Target Name="StampCommit" BeforeTargets="GetAssemblyVersion;GenerateAssemblyInfo">
    <Exec Command="git rev-parse --short HEAD" WorkingDirectory="$(MSBuildProjectDirectory)" ConsoleToMSBuild="true" IgnoreExitCode="true" ContinueOnError="true" StandardOutputImportance="low" StandardErrorImportance="low">
      <Output TaskParameter="ConsoleOutput" PropertyName="GitCommit" />
    </Exec>
    <Exec Command="git status --porcelain" WorkingDirectory="$(MSBuildProjectDirectory)" ConsoleToMSBuild="true" IgnoreExitCode="true" ContinueOnError="true" StandardOutputImportance="low" StandardErrorImportance="low">
      <Output TaskParameter="ConsoleOutput" PropertyName="GitDirty" />
    </Exec>
    <PropertyGroup>
      <GitCommit Condition="'$(GitCommit)' == ''">unknown</GitCommit>
      <InformationalVersion>$(GitCommit)</InformationalVersion>
      <InformationalVersion Condition="'$(GitDirty)' != ''">$(GitCommit)-dirty</InformationalVersion>
    </PropertyGroup>
  </Target>
```
`BuildInfo.cs`:
```csharp
using System;
using System.Reflection;

namespace MOD_sWP6UL
{
    // 빌드 표지. ModMain.csproj의 StampCommit가 InformationalVersion에 넣은 <짧은 커밋>[-dirty]. 없으면 "unknown".
    internal static class BuildInfo
    {
        public static string Commit
        {
            get
            {
                var attribute = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(typeof(BuildInfo).Assembly, typeof(AssemblyInformationalVersionAttribute));
                return attribute == null || string.IsNullOrEmpty(attribute.InformationalVersion) ? "unknown" : attribute.InformationalVersion;
            }
        }
    }
}
```
`ModMain.Init`: `MelonLogger.Msg("[TlToybox] Init build=" + BuildInfo.Commit + DevRemoteNote());` 와
```csharp
        private static string DevRemoteNote()
        {
#if DEVTOOLS
            return " devremote=" + DevCommands.Dir + (DevCommands.DirFromEnv ? " (env)" : " (default)");
#else
            return "";
#endif
        }
```
`install.ps1` 끝: `'installed build: ' + (Get-Item (Join-Path $dllDir "MOD_$ModId.dll")).VersionInfo.ProductVersion`.
`ui-check.ps1`의 `base` 끝(`Press 'Close'` 앞)에:
```powershell
    # 05 문서 49절: Init 줄이 빌드 표지와 .devremote 경로를 찍는다
    $init = @(Get-Content -LiteralPath $log | Where-Object { $_ -match '\[TlToybox\] Init ' }) | Select-Object -Last 1
    Check 'the init line names the build' ($init -match ' build=[0-9a-f]{7,}(-dirty)? ') $init
    Check 'the init line names the devremote folder' ($init -match ' devremote=\S+ \((env|default)\)\s*$') $init
```

- [ ] **Step 3: GREEN** — `pwsh tools/build-tag.tests.ps1` → FAILS=0; 두 구성 빌드 경고 0. Expected 그대로.
- [ ] **Step 4: 커밋** `feat: 커밋 표지를 어셈블리(InformationalVersion)와 Init 로그에, install.ps1이 설치본의 표지를 찍는다(리뷰 19)`

---

### Task 3: 시험 프로젝트 (17)

**Files:**
- Create: `tests/ModMain.Tests/ModMain.Tests.csproj`, `tests/ModMain.Tests/Program.cs`, `tests/ModMain.Tests/Stubs/ChangeLog.cs`, `src/ModCode/ModMain/Services/TranslationTable.cs`
- Modify: `src/ModCode/ModMain/Services/TranslationService.cs`(`Load`·`AppendMissing`·`KeyOf`가 `TranslationTable`을 쓴다), `tests/.gitkeep` 삭제

- [ ] **Step 1: 시험 프로젝트와 시험(RED)** — csproj:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- 게임 없이 도는 시험. 게임 형식을 쓰지 않는 모드 소스를 링크한다(05 문서 49절). 패키지 없음. 실행: dotnet run --project tests/ModMain.Tests -c Release -->
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>7.3</LangVersion>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <RootNamespace>MOD_sWP6UL</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="..\..\src\ModCode\ModMain\Services\Json.cs" Link="Linked\Json.cs" />
    <Compile Include="..\..\src\ModCode\ModMain\Services\ExportFiles.cs" Link="Linked\ExportFiles.cs" />
    <Compile Include="..\..\src\ModCode\ModMain\Services\TranslationTable.cs" Link="Linked\TranslationTable.cs" />
  </ItemGroup>
</Project>
```
`Stubs/ChangeLog.cs`: `namespace MOD_sWP6UL { internal static class ChangeLog { public static string DataDir { get { return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tltoybox-tests"); } } } }`
`Program.cs`: `Check(name, ok, detail)` 실행기와 스펙 2.3절의 시험 전부(코드는 Step 3에서 실제 결과에 맞춰 적는다. Json의 숫자 규칙은 `Number` 구현을 읽고 맞춘다).
Run: `dotnet run --project tests/ModMain.Tests -c Release` → Expected: 컴파일 오류(`TranslationTable` 없음) = RED.

- [ ] **Step 2: `TranslationTable`** — `Services/TranslationTable.cs`(순수):
```csharp
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 번역표(키<TAB>한국어)의 순수 규칙. 파일·게임을 모르므로 tests/ModMain.Tests가 그대로 링크한다(05 문서 49절). TranslationService가 쓴다.
    internal static class TranslationTable
    {
        // 줄의 키. '#' 줄, 빈 줄, 탭이 없는 줄, 빈 키는 null.
        public static string KeyOf(string line)
        {
            string t = (line ?? "").TrimEnd('\r');
            if (t.Trim().Length == 0 || t.TrimStart().StartsWith("#"))
            {
                return null;
            }
            int tab = t.IndexOf('\t');
            if (tab < 0)
            {
                return null;
            }
            string key = t.Substring(0, tab).Trim();
            return key.Length == 0 ? null : key;
        }

        // 줄들 → 사전. '#' 줄과 빈 줄은 무시, 탭이 없는 줄(키 없음)은 broken. 키·글의 앞뒤 공백을 지우고 "\n"을 줄바꿈으로. 같은 키는 뒤의 것.
        public static Dictionary<string, string> Parse(IEnumerable<string> lines, out int broken)
        {
            var map = new Dictionary<string, string>();
            broken = 0;
            foreach (string raw in lines)
            {
                string line = (raw ?? "").TrimEnd('\r');
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#"))
                {
                    continue;
                }
                int tab = line.IndexOf('\t');
                string key = tab < 0 ? "" : line.Substring(0, tab).Trim();
                if (key.Length == 0)
                {
                    broken++;
                    continue;
                }
                map[key] = line.Substring(tab + 1).Trim().Replace("\\n", "\n");
            }
            return map;
        }

        // 내장본(embedded, 줄바꿈 구분)의 줄 가운데 파일(fileLines)에 없는 키의 줄. 같은 키가 내장본에 두 번이면 앞의 것만.
        public static List<string> MissingLines(IEnumerable<string> fileLines, string embedded)
        {
            var have = new HashSet<string>();
            foreach (string raw in fileLines)
            {
                string key = KeyOf(raw);
                if (key != null)
                {
                    have.Add(key);
                }
            }
            var result = new List<string>();
            foreach (string raw in (embedded ?? "").Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                string key = KeyOf(line);
                if (key == null || have.Contains(key))
                {
                    continue;
                }
                result.Add(line);
                have.Add(key);
            }
            return result;
        }
    }
}
```
`TranslationService`: `Load` → `return TranslationTable.Parse(File.ReadAllLines(FilePath, Encoding.UTF8), out broken);`, `AppendMissing` → `List<string> missing = TranslationTable.MissingLines(File.ReadAllLines(FilePath, Encoding.UTF8), embedded); if (missing.Count == 0) return 0;` 뒤는 지금처럼 파일에 덧붙임(주석 줄 + `string.Join("\n", missing)` + `\n`), `KeyOf` → `TranslationTable.KeyOf` 호출(또는 삭제).

- [ ] **Step 3: 시험 코드를 완성하고 GREEN** — `dotnet run --project tests/ModMain.Tests -c Release` → FAILS=0(시험 25개 안팎). 두 구성 빌드 경고 0. `.gitignore`의 `bin/`·`obj/`가 `tests/ModMain.Tests/bin|obj`를 가린다(확인).
- [ ] **Step 4: 커밋** `test: 게임 없이 도는 시험 프로젝트 tests/ModMain.Tests(Json·ExportFiles·TranslationTable), 번역표의 순수 규칙을 TranslationTable로(리뷰 17)`

---

### Task 4: 회차 검증, 문서, 병합

- [ ] **Step 1:** 회차 `ws-1017o -Fixture base`: `base`, `translation`, `status`(회차의 첫 부분으로 돌려 `uistate`가 창을 열기 전에도 되는지 한 번 더) → FAILS=0. `-End`.
- [ ] **Step 2:** 05 문서 49절(규칙·실측·검사·미확인), 인계 1절(설치 2026-10-16 19:30, 48절까지, 표지는 다음 설치부터)·3절(`tests/` 문장, 스펙 목록)·5절(`.devremote` 한 곳, 도구 시험 셋: `session.tests`·`workshop.tests`·`devremote-dir.tests`·`build-tag.tests`·`dotnet run tests`)·8.1(Y)·8.7(`ws-1017o`). 커밋.
- [ ] **Step 3:** `git checkout develop && git merge --no-ff feat/review-tools`.
