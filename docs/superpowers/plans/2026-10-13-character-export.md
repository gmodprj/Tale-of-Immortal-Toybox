# 캐릭터 내보내기·가져오기 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 플레이어의 능력치·경지·공법·기예·운명·역천개명을 이름을 붙여 JSON 파일로 내보내고, 보관함 탭의 `캐릭터` 쪽에서 골라 전부·구역별·줄 하나씩 지금 캐릭터로 가져온다.

**Architecture:** 화면을 모르는 `Services/CharacterExportService.cs`가 내보내기·읽기·가져오기를 맡고, 가방과 함께 쓰는 파일 처리는 `Services/ExportFiles.cs`로 모은다. 화면은 `VaultTab`을 partial 클래스로 나눠 `UI/Tabs/VaultTab.Character.cs`에 캐릭터의 목록과 세부를 그린다. 가져오기는 기존 서비스(`EditService.SetAttribute`, `LuckService`, `FateService`, `ArtService`)를 부르고, 부르기 전후의 상태를 읽어 넣음·건너뜀·못 넣음을 센다.

**Tech Stack:** C# 7.3 / .NET Framework 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4(Unhollower), uGUI. 검증은 PowerShell 7 스크립트와 `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-13-character-export-design.md`

## Global Constraints

- 대상: 귀곡팔황 v1.2.113.259, 게임 폴더 `E:\Games\Tale of Immortal`, 모드 ID `sWP6UL`, 네임스페이스 `MOD_sWP6UL`.
- 참조를 추가하지 않는다. `bin\Release`에는 `MOD_sWP6UL.dll` 하나만.
- 개발 구성과 배포 구성(`-p:DevTools=false`) 모두 경고 0, 오류 0. `Dev\`의 코드를 배포 코드가 참조하지 않는다.
- 내보내기도 가져오기도 플레이어(`g.world.playerUnit`)에 한다. 편집 대상이 NPC여도 같다.
- 공법·기예·선천운명·후천운명은 더하기다. 지금 가진 것을 지우는 동작은 없다.
- 경지와 `모두 가져오기`는 두 번 눌러 확인한다. 경지의 확인 문구와 결과에 "능력치는 따라오지 않는다"를 적는다.
- 공법은 `PropsData.New(0, 1, Martial, 값의 넷째 칸부터)` + `StudyActionMartialProp` + `exp`로 넣고, 넣은 뒤 이름과 기술 번호를 읽어 확인한다. 읽히지 않으면 바로 지운다.
- 쓰기 전에 `SaveBackup.EnsureOnce()`, 쓰기 뒤에 `ChangeLog.Add`.
- 파일은 `<TlToybox 폴더>\exports\character` 안의 `.json`. 모드가 지우는 것은 그 폴더와 `exports\bag` 안의 `.json`뿐이다.
- 목록의 글은 자르지 않는다. `Ui.Wrapped`와 `ListView.HeightOf`(`RowFor`)를 쓴다(05 문서 29절).
- 브랜치: `develop`에서 `feat/character-export`. `git merge --no-ff`. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 게임 검증은 `tools/DevRemote/session.ps1`의 회차 안에서만. 회차 이름은 한 번 쓴 것을 다시 쓰지 않는다(같은 이름의 복사본이 있으면 열리지 않는다). 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 검증이 만드는 내보내기 파일은 이름이 `uicheck-`로 시작한다. 검증이 끝나거나 멈추면 그 파일들을 지운다. 사용자가 만든 파일은 건드리지 않는다.
- 스크립트의 줄바꿈은 CRLF, 코드 모양은 주변과 같게(중괄호는 항상 쓰고 줄을 바꾼다, 들여쓰기 4칸, 주석은 한국어로 "왜"를 적는다).

## Review Focus

1. 파일의 수련치가 그 경지의 다음 단계 수련치보다 크다(손으로 고침) → 다음 단계의 수련치까지만 올린다. 게임이 돌파를 일으키지 않는다. (Task 3 `charvault`의 "exp is capped")
2. 같은 파일을 두 번 `모두 가져오기` → 두 번째에는 공법·기예·운명이 늘지 않는다. (Task 3 "importing twice adds nothing")
3. 값이 모자라거나 게임이 알아보지 못하는 공법 줄 → 깨진 공법이 남지 않고(공법 수 그대로) 그 줄만 못 넣음으로 나온다. (Task 3 "a broken martial line leaves nothing behind")
4. 캐릭터 폴더에 가방 파일, 가방 폴더에 캐릭터 파일 → "읽을 수 없음"으로 나오고 가져오기 버튼이 없다. 가방 목록과 캐릭터 목록이 서로의 파일을 섞지 않는다. (Task 2 "lists do not mix", Task 3 "a bag file in the character folder is unreadable")
5. 세부 화면을 연 채 `가방`/`캐릭터`를 바꾸거나 탭을 떠났다 돌아옴 → 하위 화면이 닫히고 확인 대기가 풀린다. 다른 묶음의 줄이 눌리지 않는다. (Task 3 "leaving closes the detail screen")

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `src/ModCode/ModMain/Services/ExportFiles.cs` | 새 파일 | 내보내기 폴더, 파일 이름, 쓰기, 목록, 삭제, JSON에서 값 꺼내기 |
| `src/ModCode/ModMain/Services/BagExportService.cs` | 수정 | 위의 것을 쓰도록 |
| `src/ModCode/ModMain/Services/AttributeCatalog.cs` | 수정 | `AttributeDef.Key` |
| `src/ModCode/ModMain/Services/MartialService.cs` | 수정 | `AddFromValues` |
| `src/ModCode/ModMain/Services/ArtService.cs` | 수정 | `ForceLearn`에 단방의 `exp`·`maxExp` |
| `src/ModCode/ModMain/Services/CharacterExportService.cs` | 새 파일 | 내보내기, 읽기, 가져오기 |
| `src/ModCode/ModMain/UI/Tabs/VaultTab.cs` | 수정 | partial, `가방`/`캐릭터` 전환 |
| `src/ModCode/ModMain/UI/Tabs/VaultTab.Character.cs` | 새 파일 | 캐릭터 목록과 세부 화면 |
| `src/ModCode/ModMain/Dev/DevCommandsChar.cs` | 새 파일 | 검증용 명령(`cstate`, `cmartial`, `mdel`, `cattr`, `cartdel`, `cborndel`, `cfatealt`) |
| `tools/research/ui-check.ps1` | 수정 | `charvault`, `charvault-restart` |
| `docs/05-editor-v0.1.md`, `docs/06-handoff.md` | 수정 | 30절, 3절의 예외, 인계 문서 |

스펙 5.4절의 결과 문장은 예시다. 구현은 구역마다 같은 모양("공법: 추가 3, 건너뜀 20, 못 넣음 1(이름…).")으로 낸다. 스펙 5.2절은 화면 파일을 `VaultCharacterView.cs`라고 했다. `Push`·`Say`가 `EditorTab`의 protected 멤버라 별도 클래스에서는 부를 수 없으므로 `VaultTab`의 partial 파일로 둔다.

---

### Task 1: 공통 파일 처리(`ExportFiles`)로 가방을 옮긴다

**Files:**
- Create: `src/ModCode/ModMain/Services/ExportFiles.cs`
- Modify: `src/ModCode/ModMain/Services/BagExportService.cs`

**Interfaces:**
- Produces: `ExportFiles.Dir(string kind)`, `FileNameFor(string name)`, `Write(string kind, string stem, string json)`(쓴 파일의 전체 경로, 실패는 `IOException`/`UnauthorizedAccessException`), `Files(string kind)`(최신이 앞, 폴더가 없으면 빈 목록), `Delete(string kind, string file)`(결과 문장), `Text(o, key)`, `Whole(o, key, out int)`, `Ints(o, key, out int[])`, `Items(o, key)`.

- [ ] **Step 1: 작업 브랜치**

```powershell
git -C E:\TlToybox switch -c feat/character-export develop
```

- [ ] **Step 2: `Services/ExportFiles.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 내보내기 파일의 공통 처리. 가방(bag)과 캐릭터(character)가 함께 쓴다.
    // 파일은 세이브 폴더 밖의 <TlToybox>\exports\<종류>\<이름>.json 이다.
    internal static class ExportFiles
    {
        public static string Dir(string kind)
        {
            return Path.Combine(Path.Combine(ChangeLog.DataDir, "exports"), kind);
        }

        // 화면에서 적은 이름을 파일 이름으로. 쓸 수 없는 글자는 _로 바꾼다. 빈 글이면 이름이 없는 것이다.
        public static string FileNameFor(string name)
        {
            var sb = new StringBuilder();
            char[] bad = Path.GetInvalidFileNameChars();
            foreach (char c in (name ?? "").Trim())
            {
                sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
            }
            return sb.ToString().Trim().TrimEnd('.');
        }

        // 같은 이름이 있으면 "이름 (2)", "이름 (3)"으로 쓴다. 쓴 파일의 경로를 돌려준다.
        public static string Write(string kind, string stem, string json)
        {
            string dir = Dir(kind);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, stem + ".json");
            for (int n = 2; File.Exists(path); n++)
            {
                path = Path.Combine(dir, stem + " (" + n + ").json");
            }
            File.WriteAllText(path, json, new UTF8Encoding(false));
            return path;
        }

        // 그 종류의 내보내기 파일들. 최신이 앞. 폴더가 없거나 읽지 못하면 빈 목록.
        public static List<string> Files(string kind)
        {
            var files = new List<string>();
            try
            {
                string dir = Dir(kind);
                if (Directory.Exists(dir))
                {
                    files.AddRange(Directory.GetFiles(dir, "*.json"));
                    files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return files;
        }

        // 내보내기 파일을 지운다. 그 종류의 폴더 안에 있는 .json만 지운다.
        public static string Delete(string kind, string file)
        {
            try
            {
                string full = Path.GetFullPath(file);
                if (!full.StartsWith(Path.GetFullPath(Dir(kind)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !full.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return "삭제: 내보내기 폴더의 파일이 아니라 지우지 않았습니다.";
                }
                if (!File.Exists(full))
                {
                    return "삭제: 파일이 이미 없습니다(" + Path.GetFileName(full) + ").";
                }
                File.Delete(full);
                return "지웠습니다: " + Path.GetFileName(full);
            }
            catch (IOException e)
            {
                return "삭제: 지우지 못했습니다(" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "삭제: 지우지 못했습니다(" + e.Message + ").";
            }
        }

        // ---- JSON에서 값 꺼내기 (Json.Parse의 결과) ----

        public static string Text(Dictionary<string, object> o, string key)
        {
            object v;
            return o.TryGetValue(key, out v) && v is string ? (string)v : "";
        }

        // 정수로 읽는다. 없거나 정수가 아니거나 int 범위를 넘으면 false.
        public static bool Whole(Dictionary<string, object> o, string key, out int value)
        {
            value = 0;
            object v;
            if (!o.TryGetValue(key, out v) || !(v is long))
            {
                return false;
            }
            long n = (long)v;
            if (n < int.MinValue || n > int.MaxValue)
            {
                return false;
            }
            value = (int)n;
            return true;
        }

        // 정수 배열로 읽는다. 없으면 빈 배열과 true, 있는데 정수가 아닌 것이 섞였으면 false.
        public static bool Ints(Dictionary<string, object> o, string key, out int[] values)
        {
            values = new int[0];
            object v;
            if (!o.TryGetValue(key, out v))
            {
                return true;
            }
            var list = v as List<object>;
            if (list == null)
            {
                return false;
            }
            var result = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                if (!(list[i] is long) || (long)list[i] < int.MinValue || (long)list[i] > int.MaxValue)
                {
                    return false;
                }
                result[i] = (int)(long)list[i];
            }
            values = result;
            return true;
        }

        // 객체의 배열로 읽는다. 없거나 배열이 아니면 빈 목록. 객체가 아닌 요소는 null로 들어간다.
        public static List<Dictionary<string, object>> Items(Dictionary<string, object> o, string key)
        {
            var result = new List<Dictionary<string, object>>();
            object v;
            if (o.TryGetValue(key, out v) && v is List<object>)
            {
                foreach (object each in (List<object>)v)
                {
                    result.Add(each as Dictionary<string, object>);
                }
            }
            return result;
        }
    }
}
```

- [ ] **Step 3: `BagExportService.cs`가 이것을 쓰게 한다**

동작은 바꾸지 않는다. 다음 다섯 곳을 고친다.

1. `Dir`의 내용을 `get { return ExportFiles.Dir("bag"); }`로.
2. `private static string FileNameFor(string name) { … }` 메서드를 지우고, `Export` 안의 `string stem = FileNameFor((name ?? "").Trim());`을 `string stem = ExportFiles.FileNameFor(name);`으로.
3. `Export`의 쓰기 블록
   ```csharp
                Directory.CreateDirectory(Dir);
                string path = Path.Combine(Dir, stem + ".json");
                for (int n = 2; File.Exists(path); n++)
                {
                    path = Path.Combine(Dir, stem + " (" + n + ").json");
                }
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
   ```
   을 `string path = ExportFiles.Write("bag", stem, sb.ToString());`으로.
4. `private static string Text(…)`와 `private static bool Whole(…)` 두 메서드를 지우고, 파일 안의 `Text(`·`Whole(` 호출을 `ExportFiles.Text(`·`ExportFiles.Whole(`으로 바꾼다(`ReadItem`, `ReadMoney`, `Read`).
5. `List()`의 본문을
   ```csharp
            var result = new List<Pack>();
            foreach (string file in ExportFiles.Files("bag"))
            {
                result.Add(Read(file));
            }
            return result;
   ```
   로, `Delete(Pack pack)`의 본문을 `return ExportFiles.Delete("bag", pack.File);`으로.

- [ ] **Step 4: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 4
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 5: 회차에서 가방 검사가 그대로 통과하는지 본다**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin charx-t1
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part vault 2>&1 | Select-Object -Last 2
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End charx-t1
```

Expected: `FAILS=0`(PASS 34), `SAVE RESTORED AND IDENTICAL`.

- [ ] **Step 6: 커밋한다**

```powershell
git -C E:\TlToybox add src
git -C E:\TlToybox commit -m @'
refactor: 내보내기 파일의 공통 처리를 ExportFiles로

가방과 캐릭터 내보내기가 함께 쓸 폴더·파일 이름·쓰기·목록·삭제와 JSON 값
꺼내기를 한 곳에 둔다. 가방의 동작은 그대로다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 2: 내보내기, 읽기, 캐릭터 목록 화면

**Files:**
- Modify: `src/ModCode/ModMain/Services/AttributeCatalog.cs`, `Services/MartialService.cs`, `Services/ArtService.cs`
- Create: `src/ModCode/ModMain/Services/CharacterExportService.cs`, `UI/Tabs/VaultTab.Character.cs`, `Dev/DevCommandsChar.cs`
- Modify: `src/ModCode/ModMain/UI/Tabs/VaultTab.cs`, `Dev/DevCommands.cs`, `tools/research/ui-check.ps1`

**Interfaces:**
- Consumes: Task 1의 `ExportFiles`.
- Produces:
  - `AttributeDef.Key`(string). `MartialService.AddFromValues(WorldUnitBase unit, int[] tail, int exp, int expectBaseId)` → 실패 이유 또는 `null`. `ArtService.ForceLearn(Kind kind, ItemCatalog.Entry book, int exp = 0, int maxExp = 100)`.
  - `CharacterExportService`: `Kind`("character"); `enum Section { Grade, Attributes, Martials, Arts, Lucks, Fates }`; `class Attr { Key, Name, Value, Problem }`, `MartialLine { Type, BaseId, Name, Grade, Exp, Equipped, Values, Problem }`, `ArtLine { KindName, Id, Name, Exp, MaxExp, Problem }`, `LuckLine { Id, Name, Problem }`, `FateLine { Grade, Id, Name, Problem }`, `Pack { File, Name, Date, Character, HasGrade, GradeId, GradeName, GradeExp, Attributes, Martials, Arts, BornLucks, AddLucks, Fates, Error }`; `Export(string name)`, `List()`, `Delete(Pack)`. 가져오기는 Task 3.
  - 요소 이름: `VaultModeBag`, `VaultModeChar`, `CharName`, `CharExport`, `CharQuery`, `CharList`, `CharOpen<줄>`, `CharAll<줄>`, `CharDelete<줄>`.
  - 개발 명령: `cstate`, `cmartial <기술 번호>`, `mdel <고유 번호>`, `cattr <key> <값>`, `cartdel <번호>`, `cborndel <번호>`, `cfatealt <대경지>`.

- [ ] **Step 1: 검사를 먼저 넣는다(빨간 불)**

`tools/research/ui-check.ps1`의 `'order' {` 줄 앞에 넣는다.

```powershell
  'charvault' {
    # 묶음 D(docs/superpowers/specs/2026-10-13-character-export-design.md 7절). 내보내기 폴더는 세이브 밖이라 회차가 되돌리지 않는다.
    # 이 검사가 만든 파일(uicheck-로 시작)은 끝날 때와 멈췄을 때 지운다.
    $traps = Traps
    $dir = Join-Path $data 'exports\character'
    $bagDir = Join-Path $data 'exports\bag'
    function CleanChar { foreach ($d in $dir, $bagDir) { if (Test-Path -LiteralPath $d) { Get-ChildItem -LiteralPath $d -Filter 'uicheck-*' | ForEach-Object { [IO.File]::Delete($_.FullName) } } } }
    function Msg { if ((Cmd 'uistate' 100) -match "message='(.*)'\s*$") { $Matches[1] } else { '' } }
    function State { $o = Cmd 'cstate' 200; $h = @{ raw = $o }; foreach ($m in [regex]::Matches($o, '(\w+)=(\S+)')) { $h[$m.Groups[1].Value] = $m.Groups[2].Value }; $h }
    function Has([string]$cmd, [string]$pattern) { (Cmd $cmd 200) -match $pattern }
    CleanChar
    try {
      Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null; Press 'VaultModeChar' | Out-Null
      $s0 = State

      # 7절 1~2번: 내보내기와 파일
      Press 'CharExport' | Out-Null
      Check 'an empty name is refused' ((Msg) -match '이름을')
      Cmd 'ui set CharName uicheck-C' | Out-Null; Press 'CharExport' | Out-Null
      $fileC = Join-Path $dir 'uicheck-C.json'
      Check 'export writes a file' ((Test-Path -LiteralPath $fileC) -and (Msg) -match '내보냈습니다') (Msg)
      $pack = Get-Content -LiteralPath $fileC -Raw -Encoding UTF8 | ConvertFrom-Json
      Check 'the file names its kind' ($pack.format -eq 1 -and $pack.kind -eq 'character' -and $pack.character.Length -gt 0)
      Check 'the grade is in the file' ("$($pack.grade.id)" -eq $s0.grade -and "$($pack.grade.exp)" -eq $s0.exp) "file=$($pack.grade.id)/$($pack.grade.exp) game=$($s0.grade)/$($s0.exp)"
      Check 'every attribute is in the file' ("$(@($pack.attributes).Count)" -eq $s0.attrs -and @($pack.attributes | Where-Object { $_.key -eq 'attack' -and "$($_.value)" -eq $s0.attack }).Count -eq 1) "file=$(@($pack.attributes).Count) game=$($s0.attrs)"
      Check 'attribute keys are unique' (@($pack.attributes | ForEach-Object { $_.key } | Sort-Object -Unique).Count -eq @($pack.attributes).Count)
      Check 'every learned martial is in the file' ("$(@($pack.martials).Count)" -eq $s0.martials -and @($pack.martials | Where-Object { @($_.values).Count -lt 3 }).Count -eq 0) "file=$(@($pack.martials).Count) game=$($s0.martials)"
      Check 'learned arts are in the file' ("$(@($pack.arts).Count)" -eq $s0.arts) "file=$(@($pack.arts).Count) game=$($s0.arts)"
      Check 'born lucks are in the file' ("$(@($pack.bornLucks).Count)" -eq $s0.born)
      Check 'progress markers and timed lucks are left out' (@($pack.addLucks).Count -le [int]$s0.add)
      Cmd 'ui set CharName uicheck-C' | Out-Null; Press 'CharExport' | Out-Null
      Check 'the same name gets a number' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-C (2).json'))
      Cmd 'ui set CharName uicheck-b:c*d?' | Out-Null; Press 'CharExport' | Out-Null
      Check 'characters a file name cannot hold become underscores' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))
      Cmd 'ui set CharQuery uicheck-' | Out-Null
      Check 'the list shows three exports' ((Cmd 'uitext CharListRange') -match '전체 3개')

      # 7절 3번, Review Focus 4: 가방 목록과 섞이지 않는다
      Press 'VaultModeBag' | Out-Null
      Cmd 'ui set VaultName uicheck-bagside' | Out-Null; Press 'VaultExport' | Out-Null
      Cmd 'ui set VaultQuery uicheck-' | Out-Null
      Check 'the bag list shows only the bag export' ((Cmd 'uitext VaultListRange') -match '전체 1개' -and (Cmd 'uitext Row0') -match 'uicheck-bagside')
      Press 'VaultQueryClear' | Out-Null
      Press 'VaultModeChar' | Out-Null
      Check 'lists do not mix' ((Cmd 'uitext CharListRange') -match '전체 3개' -and (IsAt 'CharExport') -and -not (IsAt 'VaultExport'))
      Check 'the mode is kept when the tab is left' ((Press 'Tab1') -and (Press 'Tab3') -and (IsAt 'CharExport'))

      # 7절 16번의 삭제
      Cmd 'ui set CharQuery uicheck-b' | Out-Null
      Press 'CharDelete0' | Out-Null
      Check 'delete asks first' ((Cmd 'uitext CharDelete0') -match "text='정말 삭제'" -and (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json')))
      Press 'CharDelete0' | Out-Null
      Check 'the second press deletes the file' (-not (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))) (Msg)
      Press 'CharQueryClear' | Out-Null
      #CHARVAULT-IMPORT#

      Press 'Close' | Out-Null
      Check 'no exception in button callbacks' ((Traps) -eq $traps)
      Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    } finally { CleanChar }
  }
```

`#CHARVAULT-IMPORT#` 줄은 Task 3이 가져오기 검사로 바꾼다(지금은 주석으로 남는다).

빨간 불: 지금 빌드로 회차를 열어 실패를 본다.

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin charx-red1
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part charvault 2>&1 | Select-Object -Last 3
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End charx-red1
```

Expected: `VaultModeChar`를 찾지 못해 멈춘다. `exports\character`에 `uicheck-` 파일이 남지 않는다.

- [ ] **Step 2: `AttributeDef.Key`를 넣는다**

`AttributeCatalog.cs`의 `AttributeDef`에 `public string Key;   // 파일에 적는 고정 이름. 화면의 이름(Label)이 바뀌어도 같은 줄을 가리킨다`를 `Label` 위에 넣는다.

`Def` 도우미에 `key` 매개변수를 둘째 자리에 넣고(`Def(int category, string key, string label, …)`, 본문에 `Key = key,`), 호출을 고친다. 호출은 모두 `Def(분류, "이름", p => p.<필드>, …` 모양이므로 필드 이름을 key로 쓴다.

```powershell
$p = 'E:\TlToybox\src\ModCode\ModMain\Services\AttributeCatalog.cs'
$s = [IO.File]::ReadAllText($p)
$s2 = [regex]::Replace($s, 'Def\((\d), ("[^"]+"), p => p\.(\w+),', 'Def($1, "$3", $2, p => p.$3,')
"changed: $(([regex]::Matches($s, 'Def\(\d, "[^"]+", p => p\.\w+,')).Count)"
[IO.File]::WriteAllText($p, $s2, (New-Object System.Text.UTF8Encoding($false)))
```

Expected: `changed: 39`. `Def`를 쓰지 않고 `new AttributeDef { … }`로 적은 줄(수명 등)에는 손으로 `Key = "<저장 필드 이름>",`을 넣는다(수명은 `Key = "life"`). 끝나면 `Key`가 없는 정의가 없어야 한다: `AttributeCatalog.All`의 정의 수와 `Key = `·`Def(` 호출 수의 합이 같은지 눈으로 센다.

- [ ] **Step 3: `MartialService.AddFromValues`를 넣는다**

`MartialService.cs`의 맨 위에 `using UnhollowerBaseLib;`가 없으면 넣고, `Forget` 메서드 앞에 넣는다.

```csharp
        // 값 배열(머리 세 칸 뒤: 종류, 기술 번호, 경지, 옵션 번호들, -1, 옵션 품질들)로 공법을 만들어 배운 상태로 넣는다.
        // 실측: 이렇게 만든 공법은 값·옵션·품질이 원본과 같다(docs/superpowers/specs/2026-10-13-character-export-design.md 3절).
        // 실패하면 이유를, 넣었으면 null을 돌려준다. 게임이 알아보지 못해 깨진 것은 바로 지운다(읽는 것만으로 예외가 난다).
        public static string AddFromValues(WorldUnitBase unit, int[] tail, int exp, int expectBaseId)
        {
            if (tail == null || tail.Length < 3)
            {
                return "값이 모자랍니다";
            }
            try
            {
                var data = unit.data.unitData;
                int before = data.allActionMartial.Count;
                var values = new Il2CppStructArray<int>(tail.Length);
                for (int i = 0; i < tail.Length; i++)
                {
                    values[i] = tail[i];
                }
                DataProps.PropsData made = DataProps.PropsData.New(0, 1, DataProps.PropsDataType.Martial, values);
                data.StudyActionMartialProp(made);
                if (data.allActionMartial.Count != before + 1)
                {
                    return "게임이 배우게 하지 않았습니다";
                }
                bool valid;
                try
                {
                    DataProps.MartialData martial = data.GetActionMartial(made.soleID).data.To<DataProps.MartialData>();
                    valid = martial.baseID == expectBaseId && !string.IsNullOrEmpty(martial.martialInfo.name);
                }
                catch (Exception)
                {
                    valid = false;
                }
                if (!valid)
                {
                    data.DelActionMartial(made.soleID);
                    return "게임이 알아보지 못하는 공법이라 넣지 않았습니다";
                }
                if (exp > 0)
                {
                    data.GetActionMartial(made.soleID).exp = exp;
                }
                return null;
            }
            catch (Exception e)
            {
                return "게임이 받지 않았습니다(" + e.GetType().Name + ")";
            }
        }
```

- [ ] **Step 4: `ArtService.ForceLearn`이 단방의 값을 받게 한다**

시그니처를 `public static string ForceLearn(Kind kind, ItemCatalog.Entry book, int exp = 0, int maxExp = 100)`으로 바꾸고, 본문의

```csharp
                formula.exp = 0;
                formula.maxExp = 100; // 게임이 새로 배운 단방에 넣는 값(실측)
```

을

```csharp
                // 새로 배운 단방은 exp 0, maxExp 100이다(실측). 내보낸 값을 받으면 그 값으로 넣는다.
                formula.maxExp = Math.Max(1, maxExp);
                formula.exp = Math.Max(0, Math.Min(exp, formula.maxExp));
```

로 바꾼다. `using System;`이 없으면 넣는다.

- [ ] **Step 5: `Services/CharacterExportService.cs`를 만든다(내보내기와 읽기)**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 캐릭터 내보내기·가져오기. 화면을 모르고 결과를 문장으로 돌려준다. 대상은 항상 플레이어다.
    // 근거: docs/superpowers/specs/2026-10-13-character-export-design.md.
    internal static partial class CharacterExportService
    {
        public const string Kind = "character";
        private const int HeadLength = 3;   // 공법 값 배열의 머리: 2, 0, 수량

        public enum Section { Grade, Attributes, Martials, Arts, Lucks, Fates }

        public class Attr
        {
            public string Key = "";
            public string Name = "";
            public int Value;
            public string Problem;      // 이 줄을 쓸 수 없는 이유. null이면 쓸 수 있다
        }

        public class MartialLine
        {
            public int Type;
            public int BaseId;
            public string Name = "";
            public int Grade;
            public int Exp;
            public bool Equipped;
            public int[] Values = new int[0];   // 머리 세 칸 뒤의 값
            public string Problem;
        }

        public class ArtLine
        {
            public string KindName = "";    // ArtService.Kind.Tab
            public int Id;
            public string Name = "";
            public int Exp;
            public int MaxExp = 100;
            public string Problem;
        }

        public class LuckLine
        {
            public int Id;
            public string Name = "";
            public string Problem;
        }

        public class FateLine
        {
            public int Grade;
            public int Id;
            public string Name = "";
            public string Problem;
        }

        public class Pack
        {
            public string File;
            public string Name = "";
            public string Date = "";
            public string Character = "";
            public bool HasGrade;
            public int GradeId;
            public string GradeName = "";
            public int GradeExp;
            public List<Attr> Attributes = new List<Attr>();
            public List<MartialLine> Martials = new List<MartialLine>();
            public List<ArtLine> Arts = new List<ArtLine>();
            public List<LuckLine> BornLucks = new List<LuckLine>();
            public List<LuckLine> AddLucks = new List<LuckLine>();
            public List<FateLine> Fates = new List<FateLine>();
            public string Error;        // 읽을 수 없는 이유. null이면 읽혔다
        }

        private static WorldUnitBase Me
        {
            get { return g.world.playerUnit; }
        }

        // ---- 내보내기 ----

        public static string Export(string name)
        {
            if (!EditService.WorldReady)
            {
                return "내보내기: 세이브에 진입한 뒤에 사용할 수 있습니다.";
            }
            string stem = ExportFiles.FileNameFor(name);
            if (stem.Length == 0)
            {
                return "내보내기: 이름을 입력하세요.";
            }
            WorldUnitBase me = Me;
            var saved = me.data.unitData.propertyData;

            var attributes = new List<string>();
            foreach (AttributeDef def in AttributeCatalog.All)
            {
                attributes.Add("    { \"key\": " + Json.Quote(def.Key) + ", \"name\": " + Json.Quote(def.Label) + ", \"value\": " + def.Get(saved) + " }");
            }

            var martials = new List<string>();
            int broken = 0;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                // 깨진 공법은 속성을 읽는 것만으로 예외가 난다. 그런 것은 담지 않는다.
                try
                {
                    DataProps.PropsData props = pair.Value.data;
                    DataProps.MartialData martial = props.To<DataProps.MartialData>();
                    var tail = new List<string>();
                    for (int i = HeadLength; i < props.values.Length; i++)
                    {
                        tail.Add(props.values[i].ToString());
                    }
                    martials.Add("    { \"type\": " + (int)martial.martialType + ", \"baseId\": " + martial.baseID + ", \"name\": " + Json.Quote(MartialService.Name(martial))
                        + ", \"grade\": " + martial.grade + ", \"exp\": " + pair.Value.exp + ", \"equipped\": " + (MartialService.IsEquipped(me, pair.Key) ? "true" : "false")
                        + ",\n      \"values\": [" + string.Join(", ", tail.ToArray()) + "] }");
                }
                catch (Exception)
                {
                    broken++;
                }
            }

            var arts = new List<string>();
            var world = g.data.world;
            foreach (ArtService.Kind kind in ArtService.Kinds)
            {
                foreach (ItemCatalog.Entry book in ArtService.Books(kind, ""))
                {
                    if (!ArtService.Learned(kind, book.Id))
                    {
                        continue;
                    }
                    int exp = 0, maxExp = 100;
                    for (int i = 0; i < world.pillFormulas.Count; i++)
                    {
                        if (world.pillFormulas[i].id == book.Id)
                        {
                            exp = world.pillFormulas[i].exp;
                            maxExp = world.pillFormulas[i].maxExp;
                        }
                    }
                    arts.Add("    { \"kind\": " + Json.Quote(kind.Tab) + ", \"id\": " + book.Id + ", \"name\": " + Json.Quote(book.Name)
                        + ", \"exp\": " + exp + ", \"maxExp\": " + maxExp + " }");
                }
            }

            var born = new List<string>();
            var added = new List<string>();
            foreach (LuckService.LuckView view in LuckService.Owned(me))
            {
                string line = "    { \"id\": " + view.Id + ", \"name\": " + Json.Quote(view.Name) + " }";
                if (view.Born)
                {
                    born.Add(line);
                }
                // 진행 표식, 숨김, 기간이 있는 것은 이 세이브의 진행 상태다. 역천개명 분류는 아래에서 경지와 함께 담는다.
                else if (!view.IsSystem && !view.Hidden && view.Duration == -1 && view.Type != 3)
                {
                    added.Add(line);
                }
            }

            var fates = new List<string>();
            foreach (FateService.Slot slot in FateService.Slots(me))
            {
                if (slot.LuckId != 0)
                {
                    fates.Add("    { \"grade\": " + slot.Grade + ", \"id\": " + slot.LuckId + ", \"name\": " + Json.Quote(slot.LuckName) + " }");
                }
            }

            var sb = new StringBuilder();
            sb.Append("{\n  \"format\": 1,\n  \"kind\": \"").Append(Kind).Append("\",\n  \"name\": ").Append(Json.Quote((name ?? "").Trim()))
                .Append(",\n  \"exportedAt\": \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append("\",\n  \"character\": ").Append(Json.Quote(EditService.NameOf(me)))
                .Append(",\n  \"grade\": { \"id\": ").Append(saved.gradeID).Append(", \"name\": ").Append(Json.Quote(EditService.GradeName(saved.gradeID)))
                .Append(", \"exp\": ").Append(saved.exp).Append(" }");
            Block(sb, "attributes", attributes);
            Block(sb, "martials", martials);
            Block(sb, "arts", arts);
            Block(sb, "bornLucks", born);
            Block(sb, "addLucks", added);
            Block(sb, "fates", fates);
            sb.Append("\n}\n");
            try
            {
                string path = ExportFiles.Write(Kind, stem, sb.ToString());
                return "내보냈습니다: " + Path.GetFileName(path) + " (능력치 " + attributes.Count + ", 공법 " + martials.Count + ", 기예 " + arts.Count
                    + ", 운명 " + (born.Count + added.Count) + ", 역천개명 " + fates.Count + (broken > 0 ? ", 읽지 못한 공법 " + broken + "개는 뺐습니다" : "") + ")";
            }
            catch (IOException e)
            {
                return "내보내기: 파일을 쓰지 못했습니다(" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "내보내기: 파일을 쓰지 못했습니다(" + e.Message + ").";
            }
        }

        private static void Block(StringBuilder sb, string name, List<string> lines)
        {
            sb.Append(",\n  \"").Append(name).Append("\": [\n").Append(string.Join(",\n", lines.ToArray())).Append("\n  ]");
        }

        // ---- 읽기 ----

        private const string BadNumber = "번호나 값이 숫자가 아닙니다";
        private const string BadShape = "줄의 모양이 틀립니다";

        private static Pack Read(string file)
        {
            var pack = new Pack { File = file, Name = Path.GetFileNameWithoutExtension(file) };
            try
            {
                var root = Json.Parse(System.IO.File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>;
                int format;
                if (root == null || !ExportFiles.Whole(root, "format", out format) || format != 1 || ExportFiles.Text(root, "kind") != Kind)
                {
                    pack.Error = "이 편집기의 캐릭터 내보내기 파일(format 1)이 아닙니다";
                    return pack;
                }
                string name = ExportFiles.Text(root, "name");
                if (name.Length > 0)
                {
                    pack.Name = name;
                }
                pack.Date = ExportFiles.Text(root, "exportedAt");
                pack.Character = ExportFiles.Text(root, "character");
                object grade;
                if (root.TryGetValue("grade", out grade) && grade is Dictionary<string, object>)
                {
                    var o = (Dictionary<string, object>)grade;
                    pack.HasGrade = ExportFiles.Whole(o, "id", out pack.GradeId);
                    ExportFiles.Whole(o, "exp", out pack.GradeExp);
                    pack.GradeName = ExportFiles.Text(o, "name");
                }
                foreach (var o in ExportFiles.Items(root, "attributes"))
                {
                    var line = new Attr();
                    if (o == null)
                    {
                        line.Problem = BadShape;
                    }
                    else
                    {
                        line.Key = ExportFiles.Text(o, "key");
                        line.Name = ExportFiles.Text(o, "name");
                        if (!ExportFiles.Whole(o, "value", out line.Value))
                        {
                            line.Problem = BadNumber;
                        }
                    }
                    pack.Attributes.Add(line);
                }
                foreach (var o in ExportFiles.Items(root, "martials"))
                {
                    var line = new MartialLine();
                    if (o == null)
                    {
                        line.Problem = BadShape;
                    }
                    else
                    {
                        line.Name = ExportFiles.Text(o, "name");
                        ExportFiles.Whole(o, "type", out line.Type);
                        ExportFiles.Whole(o, "grade", out line.Grade);
                        ExportFiles.Whole(o, "exp", out line.Exp);
                        object flag;
                        line.Equipped = o.TryGetValue("equipped", out flag) && flag is bool && (bool)flag;
                        if (!ExportFiles.Whole(o, "baseId", out line.BaseId) || !ExportFiles.Ints(o, "values", out line.Values))
                        {
                            line.Problem = BadNumber;
                        }
                        // 값의 앞 세 칸은 종류, 기술 번호, 경지다. 그보다 짧거나 기술 번호가 다르면 이 줄로는 공법을 만들 수 없다.
                        else if (line.Values.Length < 3 || line.Values[1] != line.BaseId)
                        {
                            line.Problem = "공법의 값(values)이 모자라거나 기술 번호와 맞지 않습니다";
                        }
                    }
                    pack.Martials.Add(line);
                }
                foreach (var o in ExportFiles.Items(root, "arts"))
                {
                    var line = new ArtLine();
                    if (o == null)
                    {
                        line.Problem = BadShape;
                    }
                    else
                    {
                        line.KindName = ExportFiles.Text(o, "kind");
                        line.Name = ExportFiles.Text(o, "name");
                        ExportFiles.Whole(o, "exp", out line.Exp);
                        if (!ExportFiles.Whole(o, "maxExp", out line.MaxExp))
                        {
                            line.MaxExp = 100;
                        }
                        if (!ExportFiles.Whole(o, "id", out line.Id))
                        {
                            line.Problem = BadNumber;
                        }
                    }
                    pack.Arts.Add(line);
                }
                ReadLucks(root, "bornLucks", pack.BornLucks);
                ReadLucks(root, "addLucks", pack.AddLucks);
                foreach (var o in ExportFiles.Items(root, "fates"))
                {
                    var line = new FateLine();
                    if (o == null)
                    {
                        line.Problem = BadShape;
                    }
                    else
                    {
                        line.Name = ExportFiles.Text(o, "name");
                        if (!ExportFiles.Whole(o, "grade", out line.Grade) || !ExportFiles.Whole(o, "id", out line.Id))
                        {
                            line.Problem = BadNumber;
                        }
                    }
                    pack.Fates.Add(line);
                }
            }
            catch (FormatException e)
            {
                pack.Error = "JSON 문법 오류(" + e.Message + ")";
            }
            catch (IOException e)
            {
                pack.Error = e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                pack.Error = e.Message;
            }
            return pack;
        }

        private static void ReadLucks(Dictionary<string, object> root, string key, List<LuckLine> into)
        {
            foreach (var o in ExportFiles.Items(root, key))
            {
                var line = new LuckLine();
                if (o == null)
                {
                    line.Problem = BadShape;
                }
                else
                {
                    line.Name = ExportFiles.Text(o, "name");
                    if (!ExportFiles.Whole(o, "id", out line.Id))
                    {
                        line.Problem = BadNumber;
                    }
                }
                into.Add(line);
            }
        }

        // 내보낸 것들. 최신이 앞.
        public static List<Pack> List()
        {
            var result = new List<Pack>();
            foreach (string file in ExportFiles.Files(Kind))
            {
                result.Add(Read(file));
            }
            return result;
        }

        public static string Delete(Pack pack)
        {
            return ExportFiles.Delete(Kind, pack.File);
        }
    }
}
```


- [ ] **Step 6: 개발 명령 `Dev/DevCommandsChar.cs`를 만들고 연결한다**

```csharp
using System;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 캐릭터 내보내기·가져오기의 검증(tools/research/ui-check.ps1의 charvault)이 상태를 읽고 일부러 바꾸는 데 쓴다.
    internal static class DevCommandsChar
    {
        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            switch (p[0])
            {
                case "cstate": return State(me);
                case "cmartial": return Martials(me, int.Parse(p[1]));
                case "mdel":
                    me.data.unitData.DelActionMartial(p[1]);
                    return "DelActionMartial called; martials=" + me.data.unitData.allActionMartial.Count;
                case "cattr":
                    {
                        AttributeDef def = AttributeCatalog.All.Find(d => d.Key == p[1]);
                        return def == null ? "no attribute " + p[1] : EditService.SetAttribute(me, def, int.Parse(p[2]));
                    }
                case "cartdel":
                    {
                        int id = int.Parse(p[1]);
                        foreach (ArtService.Kind kind in ArtService.Kinds)
                        {
                            ItemCatalog.Entry book = ArtService.Books(kind, "").Find(b => b.Id == id);
                            if (book != null && ArtService.Learned(kind, id))
                            {
                                return ArtService.Forget(kind, book);
                            }
                        }
                        return "no learned art " + id;
                    }
                case "cborndel":
                    {
                        int id = int.Parse(p[1]);
                        var born = me.data.unitData.propertyData.bornLuck;
                        for (int i = 0; i < born.Length; i++)
                        {
                            if (born[i].id == id)
                            {
                                return LuckService.RemoveBorn(me, i);
                            }
                        }
                        return "no born luck " + id;
                    }
                case "cfatealt":
                    {
                        // 그 경지의 역천개명을 지금 것과 다른 첫 후보로 바꾼다.
                        int grade = int.Parse(p[1]);
                        foreach (LuckService.LuckView view in FateService.Candidates(grade, ""))
                        {
                            if (!LuckService.Has(me, view.Id))
                            {
                                return "alt=" + view.Id + " " + FateService.Set(me, grade, view.Id);
                            }
                        }
                        return "no other candidate";
                    }
                default:
                    return null;
            }
        }

        // 검사가 비교하는 값들을 이름=값으로 한 줄에.
        private static string State(WorldUnitBase me)
        {
            var saved = me.data.unitData.propertyData;
            var world = g.data.world;
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(saved.gradeID);
            int arts = 0;
            foreach (ArtService.Kind kind in ArtService.Kinds)
            {
                arts += ArtService.LearnedCount(kind);
            }
            var fates = new StringBuilder();
            foreach (FateService.Slot slot in FateService.Slots(me))
            {
                fates.Append(fates.Length == 0 ? "" : ",").Append(slot.Grade).Append(':').Append(slot.LuckId);
            }
            return "grade=" + saved.gradeID + " exp=" + saved.exp + " nextexp=" + (next == null ? -1 : next.exp)
                + " attack=" + saved.attack + " defense=" + saved.defense + " attrs=" + AttributeCatalog.All.Count
                + " martials=" + me.data.unitData.allActionMartial.Count + " arts=" + arts
                + " born=" + saved.bornLuck.Length + " add=" + saved.addLuck.Count + " fates=" + (fates.Length == 0 ? "none" : fates.ToString());
        }

        // 그 기술 번호의 익힌 공법들: 수, 그리고 하나마다 고유 번호·숙련 경험치·값 배열(머리 세 칸 뒤).
        private static string Martials(WorldUnitBase me, int baseId)
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                try
                {
                    DataProps.PropsData props = pair.Value.data;
                    if (props.To<DataProps.MartialData>().baseID != baseId)
                    {
                        continue;
                    }
                    count++;
                    sb.Append(" sole=").Append(pair.Key).Append(" mexp=").Append(pair.Value.exp).Append(" values=[");
                    for (int i = 3; i < props.values.Length; i++)
                    {
                        sb.Append(i == 3 ? "" : ",").Append(props.values[i]);
                    }
                    sb.Append("]");
                }
                catch (Exception)
                {
                }
            }
            return "n=" + count + sb;
        }
    }
}
```

`Dev/DevCommands.cs`의 `Execute` 맨 앞(`string ui = UiSpike.Execute(p);` 앞)에 넣는다.

```csharp
            string character = DevCommandsChar.Execute(p);
            if (character != null)
            {
                return character;
            }
```

- [ ] **Step 7: `VaultTab`을 partial로 바꾸고 전환을 넣는다**

`UI/Tabs/VaultTab.cs`:

1. `internal sealed class VaultTab : EditorTab`을 `internal sealed partial class VaultTab : EditorTab`으로.
2. 클래스 주석을 `// 보관함 탭: 가방(이 파일)과 캐릭터(VaultTab.Character.cs)를 파일로 내보내고, 내보낸 것을 골라 가져온다.`로.
3. 필드에 `private const float ModeH = 44;`와 `private bool character;      // 캐릭터 쪽을 보고 있는지`를 넣는다.
4. `OnShow`와 `OnOpen`에 `charPacks = null;`을 더한다.
5. `protected override void Draw()`의 이름을 `private void DrawBag()`으로 바꾸고, 그 안의 `list.Build(content, 0, 128, ContentW, ContentH - 128, Found(), DrawRow);`를 `list.Build(content, 0, 128, ContentW, ContentH - ModeH - 128, Found(), DrawRow);`로 바꾼다.
6. 새 `Draw`를 넣는다.

```csharp
        protected override void Draw()
        {
            Ui.Button("VaultModeBag", content, "가방", 0, 0, 130, 36, new Action(() => { character = false; Redraw(); }), true, !character);
            Ui.Button("VaultModeChar", content, "캐릭터", 136, 0, 130, 36, new Action(() => { character = true; Redraw(); }), true, character);
            // 아래 화면은 전환 줄만큼 내려 그린다. 가방과 캐릭터의 그리기 코드는 자기 영역의 왼쪽 위를 원점으로 쓴다.
            RectTransform whole = content;
            content = Ui.Rect("VaultArea", whole, 0, ModeH, ContentW, ContentH - ModeH);
            if (character)
            {
                DrawCharacters();
            }
            else
            {
                DrawBag();
            }
            content = whole;
        }
```

- [ ] **Step 8: `UI/Tabs/VaultTab.Character.cs`를 만든다(목록 화면)**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 보관함 탭의 캐릭터 쪽: 플레이어를 파일로 내보내고, 내보낸 것을 골라 지금 캐릭터로 가져온다.
    internal sealed partial class VaultTab
    {
        private readonly ListView<CharacterExportService.Pack> charList = new ListView<CharacterExportService.Pack>("CharList", 40);
        private List<CharacterExportService.Pack> charPacks;    // null이면 다시 읽는다
        private string charName = "";
        private string charQuery = "";

        private List<CharacterExportService.Pack> FoundCharacters()
        {
            if (charQuery.Length == 0)
            {
                return charPacks;
            }
            return charPacks.FindAll(p => p.Name.Contains(charQuery) || System.IO.Path.GetFileName(p.File).Contains(charQuery));
        }

        private static string UnreadableChar(CharacterExportService.Pack pack)
        {
            return "(읽을 수 없음) " + System.IO.Path.GetFileName(pack.File) + " — " + pack.Error;
        }

        private void DrawCharacters()
        {
            if (charPacks == null)
            {
                charPacks = CharacterExportService.List();
            }
            charList.EmptyText = "내보낸 것이 없습니다. 위에서 이름을 적고 내보내세요.";
            Ui.Label(content, "이름", 0, 0, 60, 38, 18).color = Ui.Dim;
            Ui.Input("CharName", content, charName, "내보낼 이름", 60, 0, 330, 38, new Action<string>(v => charName = v));
            Ui.Button("CharExport", content, "현재 캐릭터 내보내기", 400, 0, 240, 38, new Action(() =>
            {
                string result = CharacterExportService.Export(charName);
                charPacks = null;
                charList.Reset();
                Say(result);
            }));
            Ui.Wrapped(content, "플레이어의 능력치·경지·공법·기예·운명을 담습니다. 내보내기도 가져오기도 항상 플레이어에 합니다(편집 대상과 무관).", 656, 0, ContentW - 656, 38, 14).color = Ui.Dim;

            Ui.Label(content, "찾기", 0, 48, 60, 38, 18).color = Ui.Dim;
            SearchBox.Build(content, "CharQuery", 60, 48, 330, charQuery, "이름의 일부", new Action<string>(v =>
            {
                charQuery = v;
                charList.SetItems(FoundCharacters());
            }));
            Ui.Wrapped(content, "공법·기예·운명은 지금 것에 더하고, 능력치와 경지는 파일의 값으로 바뀝니다. 파일은 " + ExportFiles.Dir(CharacterExportService.Kind) + " 에 있습니다.", 400, 48, ContentW - 400, 38, 14).color = Ui.Dim;

            Ui.Panel("CharHead", content, 0, 96, ContentW, 30, Ui.Section);
            Ui.Label(content, "이름", 8, 96, 250, 30, 16).color = Ui.Dim;
            Ui.Label(content, "날짜", 265, 96, 180, 30, 16).color = Ui.Dim;
            Ui.Label(content, "인물", 450, 96, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "경지", 555, 96, 150, 30, 16).color = Ui.Dim;
            Ui.Label(content, "공법", 710, 96, 45, 30, 16).color = Ui.Dim;
            Ui.Label(content, "기예", 758, 96, 45, 30, 16).color = Ui.Dim;
            Ui.Label(content, "운명", 806, 96, 45, 30, 16).color = Ui.Dim;
            charList.HeightOf = pack => pack.Error != null
                ? RowFor(40, Ui.TextHeight(UnreadableChar(pack), ContentW - Ui.ScrollBarWidth - 158, 16))
                : RowFor(40, Ui.TextHeight(pack.Name, 250, 20), Ui.TextHeight(pack.Character, 100, 16), Ui.TextHeight(pack.GradeName, 150, 16));
            charList.Build(content, 0, 128, ContentW, ContentH - ModeH - 128, FoundCharacters(), DrawCharacterRow);
        }

        private void DrawCharacterRow(RectTransform row, CharacterExportService.Pack pack, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            float h = row.sizeDelta.y;
            if (pack.Error != null)
            {
                Ui.Wrapped(row, UnreadableChar(pack), 8, 0, right - 158, h, 16).color = Ui.Warn;
            }
            else
            {
                Ui.Wrapped(row, pack.Name, 8, 0, 250, h);
                Ui.Label(row, pack.Date, 265, 0, 180, h, 16);
                Ui.Wrapped(row, pack.Character, 450, 0, 100, h, 16);
                Ui.Wrapped(row, pack.HasGrade ? pack.GradeName : "-", 555, 0, 150, h, 16);
                Ui.Label(row, pack.Martials.Count.ToString(), 710, 0, 45, h, 16);
                Ui.Label(row, pack.Arts.Count.ToString(), 758, 0, 45, h, 16);
                Ui.Label(row, (pack.BornLucks.Count + pack.AddLucks.Count).ToString(), 806, 0, 45, h, 16);
                DrawCharacterActions(row, pack, index, right);
            }
            Confirm.Button("CharDelete" + index, row, "삭제", "정말 삭제", right - 108, 3, 100, 34, "char-del:" + pack.File,
                System.IO.Path.GetFileName(pack.File) + ": 파일을 지웁니다. 되돌릴 수 없습니다. \"정말 삭제\"를 한 번 더 누르세요.", new Action(() =>
                {
                    string result = CharacterExportService.Delete(pack);
                    charPacks = null;
                    Say(result);
                }));
        }

        // 읽힌 묶음의 줄에 붙는 버튼(세부, 모두 가져오기). 가져오기가 생기기 전에는 비어 있다.
        private void DrawCharacterActions(RectTransform row, CharacterExportService.Pack pack, int index, float right)
        {
        }
    }
}
```

- [ ] **Step 9: 두 구성으로 빌드하고 회차에서 검사한다(초록 불)**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 4
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin charx-t2
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part charvault
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part vault 2>&1 | Select-Object -Last 1
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End charx-t2
```

Expected: 빌드는 둘 다 경고 0·오류 0. `charvault`와 `vault` 모두 `FAILS=0`. `exports\character`와 `exports\bag`에 `uicheck-` 파일이 남지 않는다. 캐릭터 목록 화면을 캡처해 배치가 겹치지 않는지 눈으로 본다.

- [ ] **Step 10: 커밋한다**

```powershell
git -C E:\TlToybox add src tools
git -C E:\TlToybox commit -m @'
feat: 캐릭터 내보내기 — 보관함 탭의 캐릭터 목록

플레이어의 능력치·경지·공법·기예·운명·역천개명을 이름을 붙여 JSON 파일로
내보내고, 보관함 탭의 캐릭터 쪽에 목록으로 보인다. 가져오기는 다음 커밋.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 3: 가져오기와 세부 화면

**Files:**
- Create: `src/ModCode/ModMain/Services/CharacterExportService.Import.cs`
- Modify: `src/ModCode/ModMain/UI/Tabs/VaultTab.Character.cs`, `tools/research/ui-check.ps1`

**Interfaces:**
- Consumes: Task 2의 `CharacterExportService`(형식과 `Read`), `MartialService.AddFromValues`, `ArtService.ForceLearn(kind, book, exp, maxExp)`, `AttributeDef.Key`, 개발 명령.
- Produces:
  - `CharacterExportService.ImportAll(Pack)`, `ImportSection(Pack, Section)`, `ImportGrade(Pack)`, `ImportAttribute(Pack, int)`, `ImportMartial(Pack, int)`, `ImportArt(Pack, int)`, `ImportBornLuck(Pack, int)`, `ImportAddLuck(Pack, int)`, `ImportFate(Pack, int)` → 결과 문장.
  - 화면이 상태를 보이는 데 쓰는 것: `AttrNow(Attr, out int)`, `HasMartial(int baseId)`, `ArtLearned(ArtLine)`, `FateState(FateLine)`("같음"/"다름"/"경지 미달"), `OptionCount(MartialLine)`.
  - 요소 이름: `CharSecGrade`, `CharSecAttributes`, `CharSecMartials`, `CharSecArts`, `CharSecLucks`, `CharSecFates`, `CharImportSection`, `CharItemQuery`, `CharItems`, `CharGetGrade`, `CharGetAttr_<key>`, `CharGetMartial<순번>`, `CharGetArt<순번>`, `CharGetBorn<순번>`, `CharGetAdd<순번>`, `CharGetFate<순번>`.

- [ ] **Step 1: 가져오기 검사를 넣는다(빨간 불)**

`ui-check.ps1`의 `      #CHARVAULT-IMPORT#` 줄을 다음으로 바꾼다.

```powershell
      # ---- 가져오기 (7절 4~16번) ----
      function Row([string]$needle) { Cmd "ui set CharItemQuery $needle" | Out-Null }
      function IndexOfMartial($list, $one) { for ($i = 0; $i -lt @($list).Count; $i++) { if ([object]::ReferenceEquals(@($list)[$i], $one)) { return $i } }; -1 }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      # 줄 0은 "uicheck-C (2)"(나중에 쓴 것), 줄 1이 uicheck-C다. 내용은 같다.
      Press 'CharOpen0' | Out-Null
      Check 'the detail screen opens on the grade' ((IsAt 'Back') -and (IsAt 'CharGetGrade'))

      # 4~5번: 능력치
      Press 'CharSecAttributes' | Out-Null
      $atk = [int]$s0.attack; $def = [int]$s0.defense
      Cmd "cattr attack $($atk + 5)" | Out-Null; Press 'CharSecAttributes' | Out-Null; Row 'attack'
      Press 'CharGetAttr_attack' | Out-Null
      Check 'one attribute returns to the file value' ((State).attack -eq "$atk") (Msg)
      Check 'an attribute that matches cannot be imported again' (-not (IsAt 'CharGetAttr_attack') -or (Cmd 'uitext CharGetAttr_attack') -match '같음')
      Press 'CharItemQueryClear' | Out-Null
      Cmd "cattr attack $($atk + 5)" | Out-Null; Cmd "cattr defense $($def + 5)" | Out-Null
      Press 'CharSecAttributes' | Out-Null; Press 'CharImportSection' | Out-Null; Press 'CharImportSection' | Out-Null
      $s = State; $m = Msg
      Check 'the attribute section restores every changed value' ($s.attack -eq "$atk" -and $s.defense -eq "$def" -and $m -match '능력치: 변경 2') $m

      # 6번: 경지
      Cmd 'setgrade 5' | Out-Null
      Press 'CharSecGrade' | Out-Null; Press 'CharGetGrade' | Out-Null
      Check 'the grade import asks first' ((State).grade -eq '5' -and (Msg) -match '능력치는 따라오지')
      Press 'CharGetGrade' | Out-Null
      $s = State
      Check 'the second press writes the grade and its exp' ($s.grade -eq $s0.grade -and $s.exp -eq $s0.exp) $s.raw

      # 7~9번: 공법
      $mar = @($pack.martials | Where-Object { -not $_.equipped -and @($_.values).Count -gt 6 })[0]
      $mi = IndexOfMartial $pack.martials $mar
      $want = "values=\[$(@($mar.values) -join ',')\]"
      $o = Cmd "cmartial $($mar.baseId)" 200
      $sole = if ($o -match 'sole=(\S+)') { $Matches[1] } else { '' }
      Cmd "mdel $sole" | Out-Null
      Press 'CharSecMartials' | Out-Null; Row "$($mar.baseId)"
      Press "CharGetMartial$mi" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200
      Check 'a forgotten martial comes back with the same options' ($o -match '^.*n=1 ' -and $o -match $want -and (State).martials -eq $s0.martials) "$o | $(Msg)"
      Press 'CharItemQueryClear' | Out-Null
      Press 'CharImportSection' | Out-Null
      Check 'the martial section adds nothing that is already known' ((State).martials -eq $s0.martials -and (Msg) -match '공법: 추가 0') (Msg)
      Row "$($mar.baseId)"
      Press "CharGetMartial$mi" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200
      Check 'the row button adds one more of a known martial' ($o -match 'n=2 ') $o
      $extra = @([regex]::Matches($o, 'sole=(\S+)') | ForEach-Object { $_.Groups[1].Value })[-1]
      Cmd "mdel $extra" | Out-Null
      Press 'CharItemQueryClear' | Out-Null

      # 10번: 기예
      $art = @($pack.arts)[0]
      if ($art) {
        Cmd "cartdel $($art.id)" | Out-Null
        Check 'the art was forgotten for the test' ((State).arts -eq "$([int]$s0.arts - 1)")
        Press 'CharSecArts' | Out-Null; Row "$($art.id)"
        Press 'CharGetArt0' | Out-Null
        Check 'a forgotten art book comes back' ((State).arts -eq $s0.arts) (Msg)
        Press 'CharItemQueryClear' | Out-Null
      } else { 'INFO no learned art in this save; art import not exercised' }

      # 11번: 운명
      Press 'CharSecLucks' | Out-Null
      $bornId = @($pack.bornLucks)[0].id
      Cmd "cborndel $bornId" | Out-Null
      Check 'the born luck was removed for the test' (-not (Has 'luck' "born=\[[^\]]*\b$bornId\b"))
      Press 'CharSecLucks' | Out-Null; Row "$bornId"
      Press 'CharGetBorn0' | Out-Null
      Check 'a removed born luck comes back as a born luck' (Has 'luck' "born=\[[^\]]*\b$bornId\b") (Msg)
      Press 'CharItemQueryClear' | Out-Null
      $add = @($pack.addLucks)[0]
      if ($add) {
        Cmd "luckdel $($add.id)" | Out-Null
        Press 'CharSecLucks' | Out-Null; Row "$($add.id)"
        Press 'CharGetAdd0' | Out-Null
        Check 'a removed acquired luck comes back' (Has 'luck' "add=\[[^\]]*\b$($add.id)\(") (Msg)
        Press 'CharItemQueryClear' | Out-Null
      } else { 'INFO no exportable acquired luck in this save; not exercised' }

      # 12번: 역천개명
      $fate = @($pack.fates)[0]
      if ($fate) {
        $o = Cmd "cfatealt $($fate.grade)" 300
        Check 'the fate was changed for the test' ((State).fates -notmatch "$($fate.grade):$($fate.id)") $o
        Press 'CharSecFates' | Out-Null
        Press 'CharGetFate0' | Out-Null
        Check 'the fate returns to the one in the file' ((State).fates -match "$($fate.grade):$($fate.id)") (Msg)
        Cmd 'setgrade 1' | Out-Null
        Press 'CharSecFates' | Out-Null
        Check 'a fate above the current grade is shown as out of reach' ((Cmd 'uidescs CharFateState' 200) -match '경지 미달')
        Press 'CharSecGrade' | Out-Null; Press 'CharGetGrade' | Out-Null; Press 'CharGetGrade' | Out-Null
      } else { 'INFO no fate recorded in this save; fate import not exercised' }

      # Review Focus 5: 세부 화면을 연 채 떠나면 닫힌다
      Press 'Tab1' | Out-Null; Press 'Tab3' | Out-Null
      Check 'leaving closes the detail screen' ((IsAt 'CharExport') -and -not (IsAt 'CharGetGrade'))

      # 13번, Review Focus 2: 여러 가지를 바꾼 뒤 모두 가져오기
      Cmd 'setgrade 5' | Out-Null; Cmd "cattr attack $($atk + 5)" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200; if ($o -match 'sole=(\S+)') { Cmd "mdel $($Matches[1])" | Out-Null }
      if ($art) { Cmd "cartdel $($art.id)" | Out-Null }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      Press 'CharAll0' | Out-Null
      Check 'import-all asks first' ((State).grade -eq '5' -and (Cmd 'uitext CharAll0') -match "text='정말 가져오기'")
      Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'import-all restores the character' ($s.grade -eq $s0.grade -and $s.exp -eq $s0.exp -and $s.attack -eq $s0.attack -and $s.martials -eq $s0.martials -and $s.arts -eq $s0.arts -and $s.born -eq $s0.born -and $s.fates -eq $s0.fates) "$($s.raw) | $m"
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'importing twice adds nothing' ($s.martials -eq $s0.martials -and $s.arts -eq $s0.arts -and $s.born -eq $s0.born -and $m -match '공법: 추가 0' -and $m -match '기예: 추가 0') $m

      # 14~15번, Review Focus 1·3·4: 손으로 고친 파일
      $bad = [ordered]@{ format = 1; kind = 'character'; name = 'uicheck-H'; exportedAt = '2026-10-13 00:00:00'; character = 'test'
        grade = [ordered]@{ id = 9999; name = 'no such grade'; exp = 1 }
        attributes = @([ordered]@{ key = 'nope'; name = 'unknown'; value = 1 }, [ordered]@{ key = 'attack'; name = 'too big'; value = 999999999 }, [ordered]@{ key = 'defense'; name = 'text'; value = 'many' })
        martials = @([ordered]@{ type = $mar.type; baseId = $mar.baseId; name = 'short'; grade = 1; exp = 0; equipped = $false; values = @(2) },
          [ordered]@{ type = 2; baseId = 2; name = 'nonsense'; grade = 0; exp = 0; equipped = $false; values = @(2, 2, 0, 1, 2, 3) },
          [ordered]@{ type = $mar.type; baseId = 987654321; name = 'unknown skill'; grade = 1; exp = 0; equipped = $false; values = @($mar.type, 987654321, 1) })
        arts = @([ordered]@{ kind = '연단'; id = 999999999; name = 'no book' }, [ordered]@{ kind = '없는 분류'; id = 1; name = 'no kind' })
        bornLucks = @([ordered]@{ id = 999999999; name = 'no luck' }); addLucks = @([ordered]@{ id = 999999999; name = 'no luck' })
        fates = @([ordered]@{ grade = 2; id = 999999999; name = 'no fate' }) }
      $capped = [ordered]@{ format = 1; kind = 'character'; name = 'uicheck-X'; exportedAt = '2026-10-13 00:00:01'; character = 'test'
        grade = [ordered]@{ id = [int]$s0.grade; name = 'same grade'; exp = 999999999 } }
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-H.json'), ($bad | ConvertTo-Json -Depth 6))
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-X.json'), ($capped | ConvertTo-Json -Depth 6))
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-broken.json'), '{ "format": 1, "kind": "character" "grade": ')
      Copy-Item -LiteralPath (Join-Path $bagDir 'uicheck-bagside.json') -Destination (Join-Path $dir 'uicheck-bagfile.json')
      Press 'Tab3' | Out-Null
      Cmd 'ui set CharQuery uicheck-H' | Out-Null
      $before = State
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State; $m = Msg
      Check 'bad lines are refused one by one' ($s.grade -eq $before.grade -and $s.attack -eq $before.attack -and $s.defense -eq $before.defense -and $m -match '못 넣음') $m
      Check 'a broken martial line leaves nothing behind' ($s.martials -eq $before.martials -and (Cmd 'cmartial 2' 200) -match 'n=0' -and (Cmd 'cmartial 987654321' 200) -match 'n=0') $s.raw
      Cmd 'ui set CharQuery uicheck-X' | Out-Null
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State
      Check 'exp is capped at the next step' ($s.grade -eq $s0.grade -and [int]$s.exp -le [int]$s.nextexp -and [int]$s.exp -ge [int]$s0.exp) $s.raw
      Cmd 'ui set CharQuery uicheck-b' | Out-Null
      $unreadable = 0; for ($i = 0; $i -lt 3; $i++) { if ((Cmd "uitext Row$i" 100) -match '읽을 수 없음') { $unreadable++ } }
      Check 'a bag file in the character folder is unreadable' ($unreadable -eq 2 -and (Cmd 'uipos CharAll0') -match 'no element named') "unreadable=$unreadable"

      # 16번: 목록을 본 뒤 지워진 파일
      Cmd 'ui set CharQuery uicheck-X' | Out-Null
      [IO.File]::Delete((Join-Path $dir 'uicheck-X.json'))
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      Check 'a file removed behind the list is reported' ((Msg) -match '파일이 없습니다') (Msg)
      Press 'CharQueryClear' | Out-Null

      # 18번: 변경 이력
      $hist = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Select-Object -Last 120) -join "`n"
      Check 'imports are in the change log' ($hist -match '캐릭터 가져오기')

      # 17번의 준비: 경지와 능력치와 공법을 바꾼 채 가져오고 저장한다. 다시 시작한 뒤에도 파일의 상태여야 한다
      Cmd 'setgrade 5' | Out-Null; Cmd "cattr attack $($atk + 5)" | Out-Null
      $o = Cmd "cmartial $($mar.baseId)" 200; if ($o -match 'sole=(\S+)') { Cmd "mdel $($Matches[1])" | Out-Null }
      Cmd 'ui set CharQuery uicheck-C' | Out-Null
      Press 'CharAll0' | Out-Null; Press 'CharAll0' | Out-Null
      $s = State
      [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), "$($s.grade) $($s.exp) $($s.attack) $($s.martials) $($mar.baseId) $(@($mar.values) -join ',')")
      Press 'CharQueryClear' | Out-Null
      Shot 'charvault-list'
      Cmd 'ui set CharQuery uicheck-C' | Out-Null; Press 'CharOpen0' | Out-Null; Press 'CharSecMartials' | Out-Null; Shot 'charvault-martials'
      Press 'CharSecAttributes' | Out-Null; Shot 'charvault-attributes'
      Press 'Back' | Out-Null; Press 'CharQueryClear' | Out-Null
      Cmd 'uiopen PlayerInfo' 1500 | Out-Null; Shot 'charvault-playerinfo'; Cmd 'uiclose PlayerInfo' 1000 | Out-Null
      Cmd 'save' 5000 | Out-Null
```

그리고 `'charvault' { … }` 블록 뒤에 넣는다.

```powershell
  'charvault-restart' {
    $want = ([IO.File]::ReadAllText((Join-Path $work 'check-expect.txt'))) -split ' '
    $o = Cmd 'cstate' 200
    Check 'the imported grade, exp and attribute survive save and restart' ($o -match "grade=$($want[0]) exp=$($want[1]) " -and $o -match " attack=$($want[2]) " -and $o -match " martials=$($want[3]) ") $o
    $o = Cmd "cmartial $($want[4])" 200
    Check 'the imported martial survives with its options' ($o -match 'n=1 ' -and $o -match "values=\[$($want[5])\]") $o
    Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null; Press 'VaultModeChar' | Out-Null
    Cmd 'ui set CharQuery uicheck-' | Out-Null
    Check 'no test files are left' ((Cmd 'uitext CharListRange') -match "text='0개'")
    Press 'CharQueryClear' | Out-Null; Press 'Close' | Out-Null
  }
```

빨간 불: Task 2의 빌드로 회차를 열어 실패를 본다.

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin charx-red2
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part charvault 2>&1 | Select-Object -Last 3
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End charx-red2
```

Expected: `CharOpen0`을 찾지 못해 멈춘다. 그 앞의 검사는 모두 PASS다. `uicheck-` 파일이 남지 않는다.

- [ ] **Step 2: `Services/CharacterExportService.Import.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 가져오기. 줄 하나를 넣는 함수(Put…)가 넣음·건너뜀·못 넣음 가운데 하나와 이유를 돌려주고,
    // 줄·구역·전부 가져오기가 그것을 세어 문장으로 만든다. 게임이 던지는 예외는 줄 단위로 잡는다.
    internal static partial class CharacterExportService
    {
        private enum Outcome { Done, Skipped, Failed }

        // ---- 화면이 상태를 보이는 데 쓰는 것 ----

        public static bool AttrNow(Attr line, out int value)
        {
            value = 0;
            AttributeDef def = AttributeCatalog.All.Find(d => d.Key == line.Key);
            if (def == null)
            {
                return false;
            }
            value = def.Get(Me.data.unitData.propertyData);
            return true;
        }

        public static bool HasMartial(int baseId)
        {
            foreach (var pair in Me.data.unitData.allActionMartial)
            {
                try
                {
                    if (pair.Value.data.To<DataProps.MartialData>().baseID == baseId)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        // 옵션의 수: 값에서 종류·기술 번호·경지 뒤, -1 앞까지.
        public static int OptionCount(MartialLine line)
        {
            int count = 0;
            for (int i = 3; i < line.Values.Length && line.Values[i] != -1; i++)
            {
                count++;
            }
            return count;
        }

        private static ArtService.Kind ArtKind(ArtLine line)
        {
            foreach (ArtService.Kind kind in ArtService.Kinds)
            {
                if (kind.Tab == line.KindName)
                {
                    return kind;
                }
            }
            return null;
        }

        public static bool ArtLearned(ArtLine line)
        {
            ArtService.Kind kind = ArtKind(line);
            return kind != null && ArtService.Learned(kind, line.Id);
        }

        public static string FateState(FateLine line)
        {
            if (line.Grade > FateService.CurrentGrade(Me))
            {
                return "경지 미달";
            }
            foreach (FateService.Slot slot in FateService.Slots(Me))
            {
                if (slot.Grade == line.Grade)
                {
                    return slot.LuckId == line.Id ? "같음" : "다름";
                }
            }
            return "다름";
        }

        // ---- 줄 하나 넣기 ----

        private static string Gone(Pack pack)
        {
            return System.IO.File.Exists(pack.File) ? null : "가져오기: 파일이 없습니다(" + Path.GetFileName(pack.File) + "). 목록을 다시 여세요.";
        }

        private static Outcome PutGrade(Pack pack, out string text)
        {
            if (!pack.HasGrade)
            {
                text = "경지: 파일에 경지가 없습니다";
                return Outcome.Skipped;
            }
            ConfRoleGradeItem conf = g.conf.roleGrade.GetItem(pack.GradeId);
            if (conf == null)
            {
                text = "경지: 게임에 없는 경지 번호(" + pack.GradeId + ")입니다";
                return Outcome.Failed;
            }
            WorldUnitBase me = Me;
            var saved = me.data.unitData.propertyData;
            // 수련치는 그 경지의 다음 단계 수련치를 넘기지 않는다(넘기면 게임이 돌파를 기다리는 상태를 넘어선다).
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(pack.GradeId);
            int wantExp = next == null ? pack.GradeExp : Math.Min(pack.GradeExp, next.exp);
            if (saved.gradeID == pack.GradeId && saved.exp >= wantExp)
            {
                text = "경지: 이미 " + EditService.GradeName(pack.GradeId) + "입니다";
                return Outcome.Skipped;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return Outcome.Failed;
            }
            string before = EditService.GradeName(saved.gradeID) + "(" + saved.gradeID + ", 수련치 " + saved.exp + ")";
            // 경지 번호를 직접 쓴다. 실측: 번호와 수련치만 바뀌고 능력치는 따라오지 않는다(스펙 3절).
            // "경지 ID를 직접 쓰지 않는다"는 규칙의 예외이며 가져오기에서만 쓴다.
            if (saved.gradeID != pack.GradeId)
            {
                saved.SetGradeID(pack.GradeId);
            }
            if (wantExp > saved.exp)
            {
                saved.AddExp(wantExp - saved.exp);
            }
            bool ok = saved.gradeID == pack.GradeId;
            string after = EditService.GradeName(saved.gradeID) + "(" + saved.gradeID + ", 수련치 " + saved.exp + ")";
            ChangeLog.Add(EditService.NameOf(me), "캐릭터 가져오기 경지", before, after, ok);
            text = ok ? "경지: " + EditService.GradeName(saved.gradeID) + "(수련치 " + saved.exp + ")로 바꿨습니다. 능력치는 따라오지 않으니 능력치도 가져오세요"
                : "경지: 썼지만 번호가 예상과 다릅니다(" + saved.gradeID + ")";
            return ok ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutAttr(Attr line, out string text)
        {
            string label = line.Name.Length > 0 ? line.Name : line.Key;
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            AttributeDef def = AttributeCatalog.All.Find(d => d.Key == line.Key);
            if (def == null)
            {
                text = label + ": 모르는 능력치(" + line.Key + ")입니다";
                return Outcome.Failed;
            }
            WorldUnitBase me = Me;
            if (def.Get(me.data.unitData.propertyData) == line.Value)
            {
                text = def.Label + ": 이미 " + line.Value + "입니다";
                return Outcome.Skipped;
            }
            // 범위 검사·백업·변경 이력은 능력치 탭과 같은 경로가 한다.
            text = EditService.SetAttribute(me, def, line.Value);
            return def.Get(me.data.unitData.propertyData) == line.Value ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutMartial(MartialLine line, bool evenIfKnown, out string text)
        {
            string label = (line.Name.Length > 0 ? line.Name : "공법") + "(" + line.BaseId + ")";
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (!evenIfKnown && HasMartial(line.BaseId))
            {
                text = label + ": 이미 익혔습니다";
                return Outcome.Skipped;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return Outcome.Failed;
            }
            WorldUnitBase me = Me;
            string error = MartialService.AddFromValues(me, line.Values, line.Exp, line.BaseId);
            ChangeLog.Add(EditService.NameOf(me), "캐릭터 가져오기 공법 " + label, "없음", error == null ? "익힘" : "없음", error == null);
            text = error == null ? label + ": 익혔습니다(숙련 경험치 " + line.Exp + ")" : label + ": " + error;
            return error == null ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutArt(ArtLine line, out string text)
        {
            string label = (line.Name.Length > 0 ? line.Name : "서적") + "(" + line.Id + ")";
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            ArtService.Kind kind = ArtKind(line);
            if (kind == null)
            {
                text = label + ": 모르는 기예 분류(" + line.KindName + ")입니다";
                return Outcome.Failed;
            }
            ItemCatalog.Entry book = ArtService.Books(kind, "").Find(b => b.Id == line.Id);
            if (book == null)
            {
                text = label + ": 게임에 없는 " + kind.Book + "입니다";
                return Outcome.Failed;
            }
            if (ArtService.Learned(kind, line.Id))
            {
                text = book.Name + ": 이미 배웠습니다";
                return Outcome.Skipped;
            }
            text = ArtService.ForceLearn(kind, book, line.Exp, line.MaxExp);
            return ArtService.Learned(kind, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutLuck(LuckLine line, bool born, out string text)
        {
            string label = (line.Name.Length > 0 ? line.Name : "운명") + "(" + line.Id + ")";
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            WorldUnitBase me = Me;
            if (g.conf.roleCreateFeature.GetItem(line.Id) == null)
            {
                text = label + ": 게임에 없는 운명입니다";
                return Outcome.Failed;
            }
            // 가진 운명은 건드리지 않는다(후천 목록에 있는 것을 선천으로 옮기지도 않는다).
            if (LuckService.Has(me, line.Id))
            {
                text = label + ": 이미 가지고 있습니다";
                return Outcome.Skipped;
            }
            text = born ? LuckService.AddBorn(me, line.Id) : LuckService.Add(me, line.Id);
            return LuckService.Has(me, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutFate(FateLine line, out string text)
        {
            string label = (line.Name.Length > 0 ? line.Name : "역천개명") + "(" + line.Id + ")";
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (g.conf.roleCreateFeature.GetItem(line.Id) == null || g.conf.fateFeature.GetItem(line.Id) == null)
            {
                text = label + ": 게임의 역천개명 목록에 없습니다";
                return Outcome.Failed;
            }
            string state = FateState(line);
            if (state != "다름")
            {
                text = label + ": " + (state == "같음" ? "이미 그 경지의 역천개명입니다" : "아직 도달하지 않은 경지(대경지 " + line.Grade + ")라 건너뜁니다");
                return Outcome.Skipped;
            }
            text = FateService.Set(Me, line.Grade, line.Id);
            return FateState(line) == "같음" ? Outcome.Done : Outcome.Failed;
        }

        // ---- 줄 하나의 가져오기(화면의 줄 버튼) ----

        private delegate Outcome Put(out string text);

        private static string One(Pack pack, Put put)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            try
            {
                string text;
                put(out text);
                return text + ".";
            }
            catch (Exception e)
            {
                return "가져오기: 게임이 받지 않았습니다(" + e.GetType().Name + ").";
            }
        }

        public static string ImportGrade(Pack pack)
        {
            return One(pack, (out string text) => PutGrade(pack, out text));
        }

        public static string ImportAttribute(Pack pack, int index)
        {
            return One(pack, (out string text) => PutAttr(pack.Attributes[index], out text));
        }

        // 줄의 버튼은 이미 익힌 기술이어도 하나 더 넣는다.
        public static string ImportMartial(Pack pack, int index)
        {
            return One(pack, (out string text) => PutMartial(pack.Martials[index], true, out text));
        }

        public static string ImportArt(Pack pack, int index)
        {
            return One(pack, (out string text) => PutArt(pack.Arts[index], out text));
        }

        public static string ImportBornLuck(Pack pack, int index)
        {
            return One(pack, (out string text) => PutLuck(pack.BornLucks[index], true, out text));
        }

        public static string ImportAddLuck(Pack pack, int index)
        {
            return One(pack, (out string text) => PutLuck(pack.AddLucks[index], false, out text));
        }

        public static string ImportFate(Pack pack, int index)
        {
            return One(pack, (out string text) => PutFate(pack.Fates[index], out text));
        }

        // ---- 구역과 전부 ----

        // 한 구역의 집계. 끝나면 "공법: 추가 3, 건너뜀 20, 못 넣음 1(이름…)" 같은 문장이 된다.
        private sealed class Tally
        {
            public int Done, Skipped;
            public readonly List<string> Failed = new List<string>();

            public void Count(Put put, string label)
            {
                Outcome outcome;
                string text;
                try
                {
                    outcome = put(out text);
                }
                catch (Exception e)
                {
                    outcome = Outcome.Failed;
                    text = label + ": 게임이 받지 않았습니다(" + e.GetType().Name + ")";
                }
                if (outcome == Outcome.Done)
                {
                    Done++;
                }
                else if (outcome == Outcome.Skipped)
                {
                    Skipped++;
                }
                else
                {
                    Failed.Add(label);
                }
            }

            public string Sentence(string title, string verb)
            {
                var sb = new StringBuilder(title + ": " + verb + " " + Done + ", 건너뜀 " + Skipped);
                if (Failed.Count > 0)
                {
                    sb.Append(", 못 넣음 ").Append(Failed.Count).Append("(");
                    for (int i = 0; i < Failed.Count && i < 5; i++)
                    {
                        sb.Append(i == 0 ? "" : ", ").Append(Failed[i]);
                    }
                    sb.Append(Failed.Count > 5 ? " 외 " + (Failed.Count - 5) + "개)" : ")");
                }
                return sb.Append(".").ToString();
            }
        }

        private static string Name(string name, string fallback)
        {
            return name.Length > 0 ? name : fallback;
        }

        private static string Run(Pack pack, Section section)
        {
            var tally = new Tally();
            switch (section)
            {
                case Section.Grade:
                    {
                        string text;
                        try
                        {
                            PutGrade(pack, out text);
                        }
                        catch (Exception e)
                        {
                            text = "경지: 게임이 받지 않았습니다(" + e.GetType().Name + ")";
                        }
                        return text + ".";
                    }
                case Section.Attributes:
                    foreach (Attr line in pack.Attributes)
                    {
                        Attr each = line;
                        tally.Count((out string text) => PutAttr(each, out text), Name(each.Name, each.Key));
                    }
                    return tally.Sentence("능력치", "변경");
                case Section.Martials:
                    foreach (MartialLine line in pack.Martials)
                    {
                        MartialLine each = line;
                        tally.Count((out string text) => PutMartial(each, false, out text), Name(each.Name, "공법 " + each.BaseId));
                    }
                    return tally.Sentence("공법", "추가");
                case Section.Arts:
                    foreach (ArtLine line in pack.Arts)
                    {
                        ArtLine each = line;
                        tally.Count((out string text) => PutArt(each, out text), Name(each.Name, "서적 " + each.Id));
                    }
                    return tally.Sentence("기예", "추가");
                case Section.Lucks:
                    foreach (LuckLine line in pack.BornLucks)
                    {
                        LuckLine each = line;
                        tally.Count((out string text) => PutLuck(each, true, out text), Name(each.Name, "운명 " + each.Id));
                    }
                    foreach (LuckLine line in pack.AddLucks)
                    {
                        LuckLine each = line;
                        tally.Count((out string text) => PutLuck(each, false, out text), Name(each.Name, "운명 " + each.Id));
                    }
                    return tally.Sentence("운명", "추가");
                default:
                    foreach (FateLine line in pack.Fates)
                    {
                        FateLine each = line;
                        tally.Count((out string text) => PutFate(each, out text), Name(each.Name, "역천개명 " + each.Id));
                    }
                    return tally.Sentence("역천개명", "지정");
            }
        }

        public static string ImportSection(Pack pack, Section section)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            string result = Run(pack, section);
            ChangeLog.Add(EditService.NameOf(Me), "캐릭터 가져오기(" + pack.Name + ") 구역", "-", result, !result.Contains("못 넣음"));
            return result;
        }

        // 전부. 경지를 먼저 바꿔야 역천개명이 파일의 경지까지 적용된다.
        public static string ImportAll(Pack pack)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            var sb = new StringBuilder(pack.Name + " — ");
            Section[] order = { Section.Grade, Section.Attributes, Section.Lucks, Section.Fates, Section.Martials, Section.Arts };
            foreach (Section section in order)
            {
                sb.Append(Run(pack, section)).Append(" ");
            }
            string result = sb.ToString().TrimEnd();
            ChangeLog.Add(EditService.NameOf(Me), "캐릭터 가져오기 전부(" + pack.Name + ")", "-", result, !result.Contains("못 넣음") && !result.Contains("없는 경지"));
            return result;
        }
    }
}
```

경지를 건너뛴 문장("경지: 파일에 경지가 없습니다.")과 못 넣은 문장("경지: 게임에 없는 경지 번호(9999)입니다.")은 `ImportAll`의 결과에 그대로 들어간다. 검사 14번의 `못 넣음`은 다른 구역에서 나온다.

- [ ] **Step 3: `VaultTab.Character.cs`에 세부 화면을 넣는다**

필드에 더한다.

```csharp
        private sealed class CharLine
        {
            public CharacterExportService.Section Section;
            public int Index;       // 그 구역의 목록 안의 순번
            public bool Born;       // 운명 구역: 선천인지
            public string Search = "";
        }

        private readonly ListView<CharLine> charDetail = new ListView<CharLine>("CharItems", 38);
        private CharacterExportService.Pack charOpened;
        private CharacterExportService.Section charSection = CharacterExportService.Section.Grade;
        private string charItemQuery = "";
```

빈 `DrawCharacterActions`를 바꾼다.

```csharp
        // 읽힌 묶음의 줄에 붙는 버튼.
        private void DrawCharacterActions(RectTransform row, CharacterExportService.Pack pack, int index, float right)
        {
            Ui.Button("CharOpen" + index, row, "세부", right - 380, 3, 90, 34, new Action(() =>
            {
                charOpened = pack;
                charSection = CharacterExportService.Section.Grade;
                charItemQuery = "";
                charDetail.Reset();
                Push(() => charOpened.Name + " — " + charOpened.Character + ", " + charOpened.Date, DrawCharacterDetail);
            }));
            Confirm.Button("CharAll" + index, row, "모두 가져오기", "정말 가져오기", right - 284, 3, 170, 34, "char-all:" + pack.File,
                pack.Name + ": 경지와 능력치가 파일의 값으로 바뀌고, 공법·기예·운명은 없는 것이 더해집니다. \"정말 가져오기\"를 한 번 더 누르세요.",
                new Action(() => Say(CharacterExportService.ImportAll(pack))));
        }
```

메서드들을 더한다.

```csharp
        private static string SectionLabel(CharacterExportService.Section section, CharacterExportService.Pack pack)
        {
            switch (section)
            {
                case CharacterExportService.Section.Grade: return "경지";
                case CharacterExportService.Section.Attributes: return "능력치 " + pack.Attributes.Count;
                case CharacterExportService.Section.Martials: return "공법 " + pack.Martials.Count;
                case CharacterExportService.Section.Arts: return "기예 " + pack.Arts.Count;
                case CharacterExportService.Section.Lucks: return "운명 " + (pack.BornLucks.Count + pack.AddLucks.Count);
                default: return "역천개명 " + pack.Fates.Count;
            }
        }

        private static string LuckTips(int id)
        {
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(id);
            return conf == null ? "" : MartialService.CleanDesc(GameTool.LS(conf.tips)).Replace('\n', ' ');
        }

        private List<CharLine> CharLines()
        {
            CharacterExportService.Pack pack = charOpened;
            var lines = new List<CharLine>();
            switch (charSection)
            {
                case CharacterExportService.Section.Grade:
                    lines.Add(new CharLine { Section = charSection, Search = pack.GradeName + " " + pack.GradeId });
                    break;
                case CharacterExportService.Section.Attributes:
                    for (int i = 0; i < pack.Attributes.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.Attributes[i].Name + " " + pack.Attributes[i].Key });
                    }
                    break;
                case CharacterExportService.Section.Martials:
                    for (int i = 0; i < pack.Martials.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.Martials[i].Name + " " + pack.Martials[i].BaseId });
                    }
                    break;
                case CharacterExportService.Section.Arts:
                    for (int i = 0; i < pack.Arts.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.Arts[i].Name + " " + pack.Arts[i].Id + " " + pack.Arts[i].KindName });
                    }
                    break;
                case CharacterExportService.Section.Lucks:
                    for (int i = 0; i < pack.BornLucks.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Born = true, Search = pack.BornLucks[i].Name + " " + pack.BornLucks[i].Id });
                    }
                    for (int i = 0; i < pack.AddLucks.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.AddLucks[i].Name + " " + pack.AddLucks[i].Id });
                    }
                    break;
                default:
                    for (int i = 0; i < pack.Fates.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.Fates[i].Name + " " + pack.Fates[i].Id });
                    }
                    break;
            }
            return charItemQuery.Length == 0 ? lines : lines.FindAll(l => l.Search.Contains(charItemQuery));
        }

        // 줄에서 가장 긴 글(이름이나 효과)의 높이로 줄 높이를 정한다.
        private float CharLineHeight(CharLine line)
        {
            CharacterExportService.Pack pack = charOpened;
            switch (line.Section)
            {
                case CharacterExportService.Section.Martials:
                    return RowFor(38, Ui.TextHeight(pack.Martials[line.Index].Name, 330, 17));
                case CharacterExportService.Section.Arts:
                    return RowFor(38, Ui.TextHeight(pack.Arts[line.Index].Name, 400, 17));
                case CharacterExportService.Section.Lucks:
                    {
                        CharacterExportService.LuckLine luck = line.Born ? pack.BornLucks[line.Index] : pack.AddLucks[line.Index];
                        return RowFor(38, Ui.TextHeight(luck.Name, 240, 17), Ui.TextHeight(LuckTips(luck.Id), 560, 15));
                    }
                case CharacterExportService.Section.Fates:
                    return RowFor(38, Ui.TextHeight(pack.Fates[line.Index].Name, 240, 17), Ui.TextHeight(LuckTips(pack.Fates[line.Index].Id), 480, 15));
                default:
                    return 38;
            }
        }

        // 하위 화면: 한 묶음의 구역들. 구역을 고르면 그 줄들이 나오고, 줄마다 또는 구역 전체를 가져온다.
        private void DrawCharacterDetail()
        {
            CharacterExportService.Pack pack = charOpened;
            float y = SubTop;
            var sections = (CharacterExportService.Section[])Enum.GetValues(typeof(CharacterExportService.Section));
            for (int i = 0; i < sections.Length; i++)
            {
                CharacterExportService.Section section = sections[i];
                Ui.Button("CharSec" + section, content, SectionLabel(section, pack), i * 156, y, 150, 38, new Action(() =>
                {
                    charSection = section;
                    charItemQuery = "";
                    charDetail.Reset();
                    Redraw();
                }), true, charSection == section);
            }
            SearchBox.Build(content, "CharItemQuery", 0, y + 46, 330, charItemQuery, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                charItemQuery = v;
                charDetail.SetItems(CharLines());
            }));
            bool rewrites = charSection == CharacterExportService.Section.Grade || charSection == CharacterExportService.Section.Attributes;
            CharacterExportService.Section current = charSection;
            Action importSection = new Action(() => Say(CharacterExportService.ImportSection(pack, current)));
            if (rewrites)
            {
                // 값이 파일의 값으로 바뀌는 구역은 한 번 더 확인한다.
                Confirm.Button("CharImportSection", content, "이 항목 모두 가져오기", "정말 가져오기", 350, y + 46, 240, 38, "char-sec:" + current + pack.File,
                    current == CharacterExportService.Section.Grade
                        ? "경지를 파일의 값으로 바꿉니다. 능력치는 따라오지 않습니다(능력치도 가져와야 원래 인물과 같아집니다). \"정말 가져오기\"를 한 번 더 누르세요."
                        : "능력치를 모두 파일의 값으로 바꿉니다. \"정말 가져오기\"를 한 번 더 누르세요.", importSection);
            }
            else
            {
                Ui.Button("CharImportSection", content, "이 항목 모두 가져오기", 350, y + 46, 240, 38, importSection);
            }
            Ui.Wrapped(content, rewrites ? "값은 파일의 값으로 바뀝니다. 대상은 플레이어입니다." : "지금 것에 더합니다(이미 있는 것은 건너뜁니다). 가져온 공법은 자동으로 장착하지 않습니다.",
                606, y + 46, ContentW - 606, 38, 14).color = Ui.Dim;
            float top = y + 94;
            charDetail.EmptyText = "이 항목에는 줄이 없습니다.";
            charDetail.HeightOf = CharLineHeight;
            charDetail.Build(content, 0, top, ContentW, ContentH - top, CharLines(), DrawCharLine);
        }

        // 상태 글과 가져오기 버튼. 줄을 쓸 수 없으면 이유를 보이고 버튼을 끈다.
        private void LineEnd(RectTransform row, string name, string state, string problem, string label, bool enabled, Action action, bool confirm = false, string key = null, string message = null)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            Text text = Ui.Wrapped(row, problem ?? state, right - 330, 0, 190, row.sizeDelta.y, 15);
            text.color = problem != null ? Ui.Warn : Ui.Dim;
            if (confirm)
            {
                Confirm.Button(name, row, label, "정말 가져오기", right - 138, 3, 130, 32, key, message, action, enabled && problem == null);
            }
            else
            {
                Ui.Button(name, row, label, right - 138, 3, 130, 32, action, enabled && problem == null);
            }
        }

        private void DrawCharLine(RectTransform row, CharLine line, int index)
        {
            CharacterExportService.Pack pack = charOpened;
            float h = row.sizeDelta.y;
            int at = line.Index;
            switch (line.Section)
            {
                case CharacterExportService.Section.Grade:
                    {
                        var saved = Player.data.unitData.propertyData;
                        Ui.Label(row, "파일", 8, 0, 60, h, 16).color = Ui.Dim;
                        Ui.Label(row, pack.HasGrade ? EditService.GradeName(pack.GradeId) + " (번호 " + pack.GradeId + ", 수련치 " + pack.GradeExp + ")" : "경지 없음", 70, 0, 420, h, 17);
                        Ui.Label(row, "지금", 500, 0, 60, h, 16).color = Ui.Dim;
                        Ui.Label(row, EditService.GradeName(saved.gradeID) + " (번호 " + saved.gradeID + ", 수련치 " + saved.exp + ")", 562, 0, 380, h, 17);
                        bool same = pack.HasGrade && saved.gradeID == pack.GradeId;
                        LineEnd(row, "CharGetGrade", same ? "같은 경지" : "", pack.HasGrade ? null : "파일에 경지가 없습니다", "가져오기", true,
                            new Action(() => Say(CharacterExportService.ImportGrade(pack))), true, "char-grade:" + pack.File,
                            "경지를 " + EditService.GradeName(pack.GradeId) + "(으)로 바꿉니다. 능력치는 따라오지 않습니다(능력치도 가져와야 원래 인물과 같아집니다). \"정말 가져오기\"를 한 번 더 누르세요.");
                        break;
                    }
                case CharacterExportService.Section.Attributes:
                    {
                        CharacterExportService.Attr attr = pack.Attributes[at];
                        AttributeDef def = AttributeCatalog.All.Find(d => d.Key == attr.Key);
                        int now;
                        bool known = CharacterExportService.AttrNow(attr, out now);
                        Ui.Label(row, def == null ? "-" : AttributeCatalog.Categories[def.Category], 8, 0, 130, h, 16).color = Ui.Dim;
                        Ui.Label(row, def == null ? (attr.Name.Length > 0 ? attr.Name : attr.Key) : def.Label, 145, 0, 240, h, 17);
                        Ui.Label(row, "파일 " + attr.Value, 400, 0, 180, h, 17);
                        Ui.Label(row, known ? "지금 " + now : "", 590, 0, 180, h, 17);
                        bool same = known && now == attr.Value;
                        LineEnd(row, "CharGetAttr_" + attr.Key, same ? "같음" : "", attr.Problem ?? (known ? null : "모르는 능력치입니다"), same ? "같음" : "가져오기", !same,
                            new Action(() => Say(CharacterExportService.ImportAttribute(pack, at))));
                        break;
                    }
                case CharacterExportService.Section.Martials:
                    {
                        CharacterExportService.MartialLine martial = pack.Martials[at];
                        bool known = CharacterExportService.HasMartial(martial.BaseId);
                        Ui.Label(row, MartialService.TypeLabel((MartialType)martial.Type), 8, 0, 110, h, 16).color = Ui.Dim;
                        Ui.Wrapped(row, martial.Name, 125, 0, 330, h, 17);
                        Ui.Label(row, "경지 " + martial.Grade, 465, 0, 70, h, 16);
                        Ui.Label(row, "숙련 " + martial.Exp, 540, 0, 110, h, 16);
                        Ui.Label(row, "옵션 " + CharacterExportService.OptionCount(martial), 655, 0, 80, h, 16);
                        Ui.Label(row, martial.Equipped ? "장착했던 것" : "", 740, 0, 110, h, 15).color = Ui.Warn;
                        LineEnd(row, "CharGetMartial" + at, known ? "있음" : "없음", martial.Problem, known ? "하나 더" : "가져오기", true,
                            new Action(() => Say(CharacterExportService.ImportMartial(pack, at))));
                        break;
                    }
                case CharacterExportService.Section.Arts:
                    {
                        CharacterExportService.ArtLine art = pack.Arts[at];
                        bool learned = CharacterExportService.ArtLearned(art);
                        Ui.Label(row, art.KindName, 8, 0, 110, h, 16).color = Ui.Dim;
                        Ui.Wrapped(row, art.Name, 125, 0, 400, h, 17);
                        Ui.Label(row, art.Id.ToString(), 540, 0, 120, h, 16).color = Ui.Dim;
                        LineEnd(row, "CharGetArt" + at, learned ? "배움" : "-", art.Problem, learned ? "배움" : "가져오기", !learned,
                            new Action(() => Say(CharacterExportService.ImportArt(pack, at))));
                        break;
                    }
                case CharacterExportService.Section.Lucks:
                    {
                        CharacterExportService.LuckLine luck = line.Born ? pack.BornLucks[at] : pack.AddLucks[at];
                        bool has = LuckService.Has(Player, luck.Id);
                        Ui.Label(row, line.Born ? "선천" : "후천", 8, 0, 70, h, 16).color = Ui.Dim;
                        Ui.Wrapped(row, luck.Name, 85, 0, 240, h, 17);
                        Ui.Wrapped(row, LuckTips(luck.Id), 335, 0, 560, h, 15).color = Ui.Dim;
                        bool born = line.Born;
                        LineEnd(row, (born ? "CharGetBorn" : "CharGetAdd") + at, has ? "보유" : "-", luck.Problem, has ? "보유" : "가져오기", !has,
                            new Action(() => Say(born ? CharacterExportService.ImportBornLuck(pack, at) : CharacterExportService.ImportAddLuck(pack, at))));
                        break;
                    }
                default:
                    {
                        CharacterExportService.FateLine fate = pack.Fates[at];
                        string state = CharacterExportService.FateState(fate);
                        Ui.Label(row, "대경지 " + fate.Grade, 8, 0, 150, h, 16).color = Ui.Dim;
                        Ui.Wrapped(row, fate.Name, 165, 0, 240, h, 17);
                        Ui.Wrapped(row, LuckTips(fate.Id), 415, 0, 480, h, 15).color = Ui.Dim;
                        LineEnd(row, "CharGetFate" + at, state, fate.Problem, state == "같음" ? "같음" : "가져오기", state == "다름",
                            new Action(() => Say(CharacterExportService.ImportFate(pack, at))));
                        // 검사가 줄의 상태를 읽을 수 있게 이름을 붙인다.
                        row.GetChild(row.childCount - 2).gameObject.name = "CharFateState" + at;
                        break;
                    }
            }
        }
```

역천개명 줄의 상태 글은 `LineEnd`가 만든 두 요소(상태 글, 버튼) 가운데 앞의 것이다. `row.GetChild(row.childCount - 2)`가 그것이다.

- [ ] **Step 4: 두 구성으로 빌드하고 회차에서 검사한다(초록 불)**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 4
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin charx-t3
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part charvault
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part charvault-restart
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End charx-t3
```

Expected: 빌드는 둘 다 경고 0·오류 0. `charvault`와 `charvault-restart` 모두 `FAILS=0`, `SAVE RESTORED AND IDENTICAL`. 캡처 `check-charvault-list.png`, `-martials.png`, `-attributes.png`, `-playerinfo.png`를 눈으로 본다: 배치가 겹치지 않는지, 그리고 가져온 뒤 게임의 인물 화면이 정상으로 열리고 경지가 파일의 것으로 보이는지(스펙 3절의 미확인 항목). 인물 화면이 열리지 않았으면 캡처로 무엇이 떠 있는지 보고 05 문서에 적는다.

검사가 세이브의 상태에 걸려 실패하면(예: 장착하지 않은 옵션 있는 공법이 없음) 검사를 그 세이브에 맞게 고치지 말고, 무엇이 없는지 `INFO`로 알리고 그 검사만 건너뛰게 고친다.

- [ ] **Step 5: 커밋한다**

```powershell
git -C E:\TlToybox add src tools
git -C E:\TlToybox commit -m @'
feat: 캐릭터 가져오기 — 전부, 구역별, 줄 하나씩

보관함 탭의 캐릭터 쪽에서 내보낸 것을 골라 지금 캐릭터로 가져온다. 능력치와
경지는 파일의 값으로 바뀌고(경지는 번호와 수련치를 직접 쓴다. 능력치는
따라오지 않는다), 공법·기예·운명은 없는 것만 더한다. 공법은 값 배열로 다시
만들어 옵션과 숙련 경험치가 원본과 같다. 줄마다 넣음·건너뜀·못 넣음을 세어
알린다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 4: 전체 검증, 문서, 병합, 설치

**Files:**
- Modify: `docs/05-editor-v0.1.md`(30절, 3절), `docs/06-handoff.md`

**Interfaces:**
- Consumes: Task 1~3의 산출물 전체.

- [ ] **Step 1: 기존 검사와 배포 구성**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin charx-all
foreach ($part in 'vault','wrap','base','tabs','items','numbers','people','history','confirm','actions','status','order') { "== $part"; pwsh -NoProfile -File tools\research\ui-check.ps1 -Part $part 2>&1 | Select-Object -Last 1 }
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End charx-all
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin charx-rel -ReleaseBuild
& tools\DevRemote\win.ps1 -Do key -Vk 0x2D
Start-Sleep -Seconds 1
& tools\DevRemote\rc.ps1 -Cmd 'hit 151 146','click 151 302' -NoShot
Select-String -LiteralPath 'E:\Games\Tale of Immortal\MelonLoader\Latest.log' -Pattern 'trampoline'
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End charx-rel
```

Expected: 모든 부분 `FAILS=0`. 배포 구성에서 `click`이 `Button.onClick: Tab3`, `trampoline` 줄 없음. `history`는 `items` 뒤에 돈다(위 순서가 그렇다).

- [ ] **Step 2: 브랜치 전체 검토(읽기 전용 검토자 한 명)와 수정**

`superpowers:executing-plans`의 Final Review를 병합 전에 한다. 검토에서 나온 것은 검사를 먼저 고쳐 실패를 본 뒤 고치고, `charvault`·`charvault-restart`·`vault`를 다시 돌린다.

- [ ] **Step 3: `docs/05-editor-v0.1.md`**

- 맨 끝에 `## 30. 캐릭터 내보내기·가져오기 (2026-10-13 추가)`를 넣는다: 쓰는 법, 파일 형식(스펙 5.1절), 가져오기 규칙 표(스펙 2·4절), 실측(스펙 3절의 두 표), 검증 결과(부분별 PASS 수), 구현하며 고친 것, 확인하지 못한 것(스펙 3절의 목록에 검증에서 본 것을 반영).
- 3절의 표에서 `| 경지 | 경지 ID를 직접 쓰지 않는다. 정식 돌파 행동만 호출한다 |`를 `| 경지 | 경지 ID를 직접 쓰지 않는다. 정식 돌파 행동만 호출한다. 예외: 캐릭터 가져오기의 경지(두 번 눌러 확인, 30절) |`로 바꾼다.

- [ ] **Step 4: `docs/06-handoff.md`**

- 1절: 직전 작업을 "묶음 D를 끝냈다(05 문서 30절). 요청 1~5가 모두 끝났고, 남은 것은 요청 6(그 밖의 추가·개선 후보)이다"로. 05 문서의 절 범위를 1~30절로.
- 2절: 보관함 줄에 캐릭터 내보내기·가져오기와 코드(`CharacterExportService`, `ExportFiles`, `UI/Tabs/VaultTab.Character.cs`), 30절. 데이터 폴더에 `exports\character\`.
- 3절: `Services/`의 설명에 `ExportFiles.cs`, `CharacterExportService*.cs`. 스펙·계획 목록에 묶음 D(`2026-10-13-character-export*`).
- 5절: `ui-check.ps1`의 부분에 `charvault`, `charvault-restart`(앞 부분 뒤에 `-Restart`). 개발 명령에 `cstate`, `cmartial`, `mdel`, `cattr`, `cartdel`, `cborndel`, `cfatealt`.
- 6절: 회차 이름을 다시 쓰면 `-Begin`이 열리지 않는다는 것(같은 이름의 세이브 복사본이 남아 있다).
- 8.1절: D를 "끝남"으로. 8.5절을 끝난 상태로 고치고 구현에서 알게 된 것을 적는다.
- 8.7절: `feat/char-export-research` 브랜치를 실험 브랜치 목록에 보태고, 검토에서 고치지 않고 남긴 것을 적는다.

- [ ] **Step 5: 문서를 커밋하고 `develop`에 병합, 설치**

```powershell
git -C E:\TlToybox add docs
git -C E:\TlToybox commit -m @'
docs: 캐릭터 내보내기·가져오기의 기록(05 문서 30절), 인계 문서 갱신

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
git -C E:\TlToybox switch develop
git -C E:\TlToybox merge --no-ff feat/character-export -m @'
Merge branch 'feat/character-export' into develop

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
pwsh -NoProfile -File E:\TlToybox\tools\install.ps1
```

Expected: 충돌 없이 병합되고, 설치 스크립트의 마지막 줄에 오늘 날짜의 DLL이 나온다. 게임이 켜져 있으면 사용자에게 알리고 기다린다.

- [ ] **Step 6: 사용자에게 알린다**

바뀐 것(보관함의 캐릭터 쪽 쓰는 법, 파일 위치), 검증 결과, 확인하지 못한 것(경지를 직접 바꾼 뒤의 게임 동작 가운데 보지 못한 것, 다른 세이브·새 인물로 옮기기, 선천운명이 칸 수를 넘을 때)을 알리고, 직접 써 보기를 부탁한다.
