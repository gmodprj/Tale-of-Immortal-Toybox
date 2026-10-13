# 가방 내보내기·가져오기 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 현재 가방(아이템, 장착 중인 것, 영석과 화폐)을 이름을 붙여 JSON 파일로 내보내고, 새 탭 "보관함"에서 골라 전부 또는 하나씩 지금 가방에 더한다.

**Architecture:** 파일 읽기·쓰기와 가져오기는 화면을 모르는 `Services/BagExportService.cs`가 맡고(작은 JSON 읽기·쓰기는 `Services/Json.cs`), 화면은 `UI/Tabs/VaultTab.cs`가 묶음 B의 부품(`ListView`, `SearchBox`, `Confirm`, `Push`)으로 그린다. 아이템은 읽어 둔 값으로 `PropsData.New`로 다시 만들어 넣고, 넣은 뒤 실제로 늘었는지 확인한다.

**Tech Stack:** C# 7.3 / .NET Framework 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4(Unhollower), uGUI. 검증은 PowerShell 7 스크립트와 `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-13-bag-export-design.md`

## Global Constraints

- 대상: 귀곡팔황 v1.2.113.259, 게임 폴더 `E:\Games\Tale of Immortal`, 모드 ID `sWP6UL`, 네임스페이스 `MOD_sWP6UL`.
- 참조를 추가하지 않는다(JSON 라이브러리 포함). `bin\Release`에는 `MOD_sWP6UL.dll` 하나만.
- 개발 구성과 배포 구성(`-p:DevTools=false`) 모두 경고 0, 오류 0.
- 가져오기는 항상 지금 가방에 더한다. 가방을 비우는 동작은 없다.
- 문자열 배열이 비어 있지 않은 아이템은 내보내지 않는다.
- 가방 추가 칸은 사용자가 `가방을 늘리고 모두`를 골랐을 때만 바꾼다.
- 모드가 지우는 파일은 `<TlToybox 폴더>\exports\bag` 안의 `.json`뿐이다.
- 쓰기 전에 `SaveBackup.EnsureOnce()`, 쓰기 뒤에 `ChangeLog.Add`.
- 보관함 탭은 화폐 다음(네 번째, `Tab3`)이다. 경지부터 안전까지는 `Tab4`~`Tab10`이 된다.
- 브랜치: `develop`에서 `feat/bag-export`. `git merge --no-ff`. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 게임 검증은 `tools/DevRemote/session.ps1`의 회차 안에서만. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 검증이 만드는 내보내기 파일은 이름이 `uicheck-`로 시작한다. 검증이 끝나거나 멈추면 그 파일들을 지운다(이 폴더는 세이브 밖이라 회차가 되돌리지 않는다). 사용자가 만든 파일은 건드리지 않는다.
- 코드 모양은 주변과 같게: 중괄호는 항상 쓰고 줄을 바꾼다, 들여쓰기 4칸, 주석은 한국어로 "왜"를 적는다.

## Review Focus

1. 사람이 고친 JSON: 쉼표가 빠졌거나, 숫자 자리에 글자가 있거나, `items`가 없거나, 값이 `int` 범위를 넘는다 → "읽을 수 없음"으로 보이거나 그 줄만 건너뛰고, 예외가 화면 밖으로 나가지 않는다. (Task 1 `vault`)
2. 이름이 비었거나 공백뿐이거나 파일에 쓸 수 없는 글자만 있다 → 내보내지 않고 이유를 알리거나 `_`로 바뀐 이름으로 저장한다. (Task 1 `vault`)
3. 가져오는 도중 가방이 찬다 → 들어간 것과 못 넣은 것이 정확히 보고되고, 못 넣은 것이 "가져옴"으로 세어지지 않는다. (Task 1 `vault`)
4. 내보내기 폴더가 없거나, 목록을 본 뒤 파일이 밖에서 지워졌다 → 빈 목록이 나오고, 가져오기·삭제가 이유를 알리며 예외가 없다. (Task 1 `vault`)
5. 수량이 0이거나 음수인 줄, 같은 아이템이 여러 줄 → 0 이하는 건너뛰고, 여러 줄은 각각 더한다. (Task 1 `vault`)

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `src/ModCode/ModMain/Services/Json.cs` | 새 파일 | 작은 JSON 읽기와 문자열 따옴표 처리 |
| `src/ModCode/ModMain/Services/BagExportService.cs` | 새 파일 | 내보내기, 목록, 가져오기, 필요한 칸, 삭제 |
| `src/ModCode/ModMain/UI/Tabs/VaultTab.cs` | 새 파일 | 보관함 탭 |
| `src/ModCode/ModMain/UI/EditorWindow.cs` | 수정 | 탭 목록에 보관함, 탭 줄의 높이 |
| `tools/research/ui-check.ps1`, `datebag-check.ps1` | 수정 | 탭 번호 갱신, `vault`·`vault-restart` 부분 |
| `docs/05-editor-v0.1.md`, `docs/06-handoff.md` | 수정 | 28절, 인계 문서 |

---

### Task 1: 서비스, 보관함 탭, 검증

**Files:**
- Create: `src/ModCode/ModMain/Services/Json.cs`, `src/ModCode/ModMain/Services/BagExportService.cs`, `src/ModCode/ModMain/UI/Tabs/VaultTab.cs`
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs`
- Modify: `tools/research/ui-check.ps1`, `tools/research/datebag-check.ps1`

**Interfaces:**
- Produces:
  - `Json.Parse(string)` → `object`(`Dictionary<string, object>`, `List<object>`, `string`, `long`, `bool`, `null`), 틀리면 `FormatException`. `Json.Quote(string)`.
  - `BagExportService`: `Dir`; `class Item { Id, Name, Count, Martial, Values, Equipped }`; `class Money { Id, Name, Count }`; `class Pack { File, Name, Date, Character, Skipped, Items, Currencies, Error }`; `Export(string name)`, `List()`, `NeededSlots(Pack)`, `FreeSlots()`, `ImportItem(Pack, int)`, `ImportCurrency(Pack, int)`, `ImportAll(Pack, bool expandBag)`, `Delete(Pack)`.
  - 요소 이름: `VaultName`(이름 입력), `VaultExport`, `VaultQuery`, `VaultList`, `VaultOpen<줄>`, `VaultAll<줄>`, `VaultExpand<줄>`, `VaultFit<줄>`, `VaultDelete<줄>`, 세부의 `VaultItemQuery`, `VaultItems`, `VaultGet<아이템 순번>`, `VaultGetC<화폐 순번>`.

- [ ] **Step 1: 작업 브랜치, 탭 번호 갱신, 검사 부분 넣기(빨간 불)**

```powershell
git -C E:\TlToybox switch -c feat/bag-export develop
```

보관함이 `Tab3`으로 들어가므로 검증 스크립트의 `Tab9`~`Tab3`을 큰 수부터 하나씩 올린다(`Tab9`→`Tab10`, …, `Tab3`→`Tab4`). `ui-check.ps1`의 `$tabProbe`에는 `'CurrencyAdd10001'` 다음에 `'VaultExport'`를 넣고, 탭 수 반복(`-lt 10`)을 `-lt 11`로 바꾼다. `datebag-check.ps1`의 `Tab8`은 `Tab9`로 바꾼다(`Tab1`은 그대로).

`ui-check.ps1`의 `'order' {` 줄 앞에 다음 두 부분을 넣는다.

```powershell
  'vault' {
    # 묶음 C(docs/superpowers/specs/2026-10-13-bag-export-design.md 6절). 내보내기 폴더는 세이브 밖이라 회차가 되돌리지 않는다.
    # 이 검사가 만든 파일(uicheck-로 시작)은 끝날 때와 멈췄을 때 지운다.
    $traps = Traps
    $dir = Join-Path $data 'exports\bag'
    function CleanVault { if (Test-Path -LiteralPath $dir) { Get-ChildItem -LiteralPath $dir -Filter 'uicheck-*' | ForEach-Object { [IO.File]::Delete($_.FullName) } } }
    function Num([string]$cmd, [string]$pattern) { if ((Cmd $cmd 100) -match $pattern) { [int]$Matches[1] } else { -1 } }
    function Msg { if ((Cmd 'uistate' 100) -match "message='(.*)'\s*$") { $Matches[1] } else { '' } }
    CleanVault
    try {
      Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null

      # 6절 1~3번, Review Focus 2: 이름과 파일
      Press 'VaultExport' | Out-Null
      Check 'an empty name is refused' ((Msg) -match '이름을' -and -not (Test-Path -LiteralPath (Join-Path $dir '.json')))
      Cmd 'ui set VaultName uicheck-A' | Out-Null; Press 'VaultExport' | Out-Null
      $fileA = Join-Path $dir 'uicheck-A.json'
      Check 'export writes a file' ((Test-Path -LiteralPath $fileA) -and (Msg) -match '내보냈습니다') (Msg)
      $pack = Get-Content -LiteralPath $fileA -Raw -Encoding UTF8 | ConvertFrom-Json
      $all = Num 'read' 'props=(\d+)'
      Check 'the file is valid JSON with every plain item' ($pack.format -eq 1 -and $pack.kind -eq 'bag' -and $pack.items.Count -eq $all - $pack.skipped -and $pack.items.Count -gt 0) "items=$($pack.items.Count) skipped=$($pack.skipped) bag=$all"
      Check 'currencies are in the file' (@($pack.currencies | Where-Object { $_.id -eq 10001 -and $_.count -gt 0 }).Count -eq 1)
      Check 'equipped items are marked' (@($pack.items | Where-Object { $_.equipped }).Count -gt 0)
      Cmd 'ui set VaultName uicheck-A' | Out-Null; Press 'VaultExport' | Out-Null
      Check 'the same name gets a number' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-A (2).json'))
      Cmd 'ui set VaultName uicheck-b:c*d?' | Out-Null; Press 'VaultExport' | Out-Null
      Check 'characters a file name cannot hold become underscores' (Test-Path -LiteralPath (Join-Path $dir 'uicheck-b_c_d_.json'))
      Check 'the list shows three exports' ((Cmd 'uitext VaultListRange') -match '전체 3개' -or (Num 'uitext VaultListRange' '전체 (\d+)개') -ge 3)

      # 가져오기의 기준이 되는 묶음을 손으로 만든다: 겹쳐지는 것 1, 겹쳐지지 않는 것 2(같은 번호 두 줄), 비급 1, 화폐 1, 그리고 걸러져야 할 줄들
      $martial = @($pack.items | Where-Object { $_.type -eq 'Martial' })[0]
      $stack = @($pack.items | Where-Object { $_.type -eq 'Props' -and $_.count -gt 1 })[0]
      $single = @($pack.items | Where-Object { $_.type -eq 'Props' -and $_.count -eq 1 -and -not $_.equipped })[0]
      $made = [ordered]@{ format = 1; kind = 'bag'; name = 'uicheck-M'; exportedAt = '2026-10-13 00:00:00'; character = 'test'; skipped = 0
        items = @(
          [ordered]@{ id = $stack.id; name = $stack.name; count = 3; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $single.id; name = $single.name; count = 1; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $single.id; name = $single.name; count = 1; type = 'Props'; values = @(); equipped = $true },
          [ordered]@{ id = 0; name = $martial.name; count = 1; type = 'Martial'; values = $martial.values; equipped = $false },
          [ordered]@{ id = 999999999; name = 'no such item'; count = 1; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $stack.id; name = 'zero'; count = 0; type = 'Props'; values = @(); equipped = $false },
          [ordered]@{ id = $stack.id; name = 'negative'; count = -5; type = 'Props'; values = @(); equipped = $false })
        currencies = @([ordered]@{ id = 10041; name = '성주령'; count = 7 }) }
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-M.json'), ($made | ConvertTo-Json -Depth 6))
      Press 'Tab3' | Out-Null   # 탭으로 다시 들어오면 목록을 새로 읽는다. 방금 쓴 파일이 맨 위(0번 줄)다
      Check 'the hand-made export is on top' ((Cmd 'uitext Row0') -match 'uicheck-M')

      # 6절 4~8번: 세부 화면에서 하나씩
      Press 'VaultOpen0' | Out-Null
      Check 'the detail screen lists items and currencies' ((IsAt 'Back') -and (Cmd 'uitext VaultItemsRange') -match '전체 8개')
      $n0 = Num "count $($stack.id)" 'count=(\d+)'; $g0 = Num 'grid' 'cur=(\d+)'
      Press 'VaultGet0' | Out-Null
      Check 'a stackable item adds to the stack without a slot' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n0 + 3 -and (Num 'grid' 'cur=(\d+)') -eq $g0) (Msg)
      $n1 = Num "count $($single.id)" 'count=(\d+)'
      Press 'VaultGet1' | Out-Null
      Check 'a single item takes one slot' ((Num "count $($single.id)" 'count=(\d+)') -eq $n1 + 1 -and (Num 'grid' 'cur=(\d+)') -eq $g0 + 1) (Msg)
      $p0 = Num 'read' 'props=(\d+)'
      Press 'VaultGet3' | Out-Null
      Check 'a manual is recreated' ((Num 'read' 'props=(\d+)') -eq $p0 + 1) (Msg)
      $n2 = Num "count $($stack.id)" 'count=(\d+)'
      Press 'VaultGet4' | Out-Null
      Check 'an unknown item is skipped with a reason' ((Msg) -match '게임에 없는') (Msg)
      Press 'VaultGet5' | Out-Null
      Check 'a zero count is skipped' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n2 -and (Msg) -match '수량') (Msg)
      $c0 = Num 'count 10041' 'count=(\d+)'
      Press 'VaultGetC0' | Out-Null
      Check 'a currency adds its amount' ((Num 'count 10041' 'count=(\d+)') -eq $c0 + 7) (Msg)
      Cmd 'ui set VaultItemQuery 성주령' | Out-Null
      Check 'the detail search filters' ((Cmd 'uitext VaultItemsRange') -match '전체 1개')
      Press 'VaultItemQueryClear' | Out-Null
      Press 'Back' | Out-Null

      # 6절 9번: 빈 칸이 넉넉할 때 전부
      $n3 = Num "count $($stack.id)" 'count=(\d+)'; $n4 = Num "count $($single.id)" 'count=(\d+)'; $c1 = Num 'count 10041' 'count=(\d+)'; $p1 = Num 'read' 'props=(\d+)'
      Press 'VaultAll0' | Out-Null
      $m = Msg
      Check 'import-all adds everything valid' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n3 + 3 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n4 + 2 -and (Num 'count 10041' 'count=(\d+)') -eq $c1 + 7 -and (Num 'read' 'props=(\d+)') -eq $p1 + 3) $m
      Check 'the result counts what went in and what was skipped' ($m -match '아이템 4줄' -and $m -match '화폐 1종' -and $m -match '건너뜀 3줄') $m

      # 6절 10~11번, Review Focus 3: 가방을 가득 채운 뒤
      Cmd 'gridfill 600 5' 4000 | Out-Null
      $extra0 = Num 'bagstat' 'extra=(\d+)'; $n5 = Num "count $($stack.id)" 'count=(\d+)'; $n6 = Num "count $($single.id)" 'count=(\d+)'
      Press 'VaultAll0' | Out-Null
      Check 'a full bag asks instead of importing' ((IsAt 'VaultExpand0') -and (IsAt 'VaultFit0') -and (Msg) -match '필요' -and (Num "count $($stack.id)" 'count=(\d+)') -eq $n5) (Msg)
      Press 'VaultFit0' | Out-Null
      $m = Msg
      Check 'fit-only adds the stackable and reports the rest' ((Num "count $($stack.id)" 'count=(\d+)') -eq $n5 + 3 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n6 -and $m -match '못 넣음 3줄' -and (Num 'bagstat' 'extra=(\d+)') -eq $extra0) $m
      Press 'VaultAll0' | Out-Null; Press 'VaultExpand0' | Out-Null
      $m = Msg
      Check 'expand-and-import grows the bag by tens and adds everything' ((Num 'bagstat' 'extra=(\d+)') -eq $extra0 + 10 -and (Num "count $($single.id)" 'count=(\d+)') -eq $n6 + 2 -and $m -notmatch '못 넣음') $m

      # 6절 12번, Review Focus 1·4: 읽을 수 없는 파일, 밖에서 지워진 파일
      Start-Sleep -Milliseconds 1100
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-broken.json'), '{ "format": 1, "kind": "bag" "items": [ }')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-empty.json'), '')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-v2.json'), '{ "format": 2, "kind": "bag", "items": [], "currencies": [] }')
      [IO.File]::WriteAllText((Join-Path $dir 'uicheck-big.json'), '{ "format": 1, "kind": "bag", "name": "big", "items": [ { "id": 99999999999999, "count": "many", "type": "Props" } ] }')
      Press 'Tab3' | Out-Null
      Cmd 'ui set VaultQuery uicheck-' | Out-Null
      $unreadable = 0; for ($i = 0; $i -lt 8; $i++) { if ((Cmd "uitext Row$i" 100) -match '읽을 수 없음') { $unreadable++ } }
      Check 'broken, empty and other-format files are listed as unreadable' ($unreadable -eq 3) "unreadable=$unreadable"
      Cmd 'ui set VaultQuery uicheck-big' | Out-Null
      Press 'VaultAll0' | Out-Null
      Check 'bad values in a readable file are skipped, not thrown' ((Msg) -match '건너뜀 1줄') (Msg)
      Cmd 'ui set VaultQuery uicheck-M' | Out-Null
      [IO.File]::Delete((Join-Path $dir 'uicheck-M.json'))
      Press 'VaultAll0' | Out-Null
      Check 'a file removed behind the list is reported' ((Msg) -match '파일이 없습니다') (Msg)

      # 6절 14번: 삭제는 두 번 눌러야 한다
      Cmd 'ui set VaultQuery uicheck-A' | Out-Null
      Press 'VaultDelete0' | Out-Null
      Check 'delete asks first' ((Cmd 'uitext VaultDelete0') -match "text='정말 삭제'")
      Press 'Tab1' | Out-Null; Press 'Tab3' | Out-Null
      Check 'another action keeps the files' ((Test-Path -LiteralPath $fileA) -and (Test-Path -LiteralPath (Join-Path $dir 'uicheck-A (2).json')))
      Cmd 'ui set VaultQuery uicheck-A' | Out-Null
      $before = @(Get-ChildItem -LiteralPath $dir -Filter 'uicheck-A*').Count
      Press 'VaultDelete0' | Out-Null; Press 'VaultDelete0' | Out-Null
      Check 'the second press deletes exactly one file' (@(Get-ChildItem -LiteralPath $dir -Filter 'uicheck-A*').Count -eq $before - 1) (Msg)
      Press 'VaultQueryClear' | Out-Null

      # 6절 16번: 변경 이력
      $hist = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Select-Object -Last 40) -join "`n"
      Check 'imports are in the change log' ($hist -match '가방 가져오기')

      # 6절 15번의 준비: 저장하고 다시 시작한 뒤에도 남는지
      [IO.File]::WriteAllText((Join-Path $work 'check-expect.txt'), "$($single.id) $(Num "count $($single.id)" 'count=(\d+)')")
      Press 'Close' | Out-Null
      Cmd 'save' 5000 | Out-Null
      Check 'no exception in button callbacks' ((Traps) -eq $traps)
      Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    } finally { CleanVault }
  }
  'vault-restart' {
    $want = ([IO.File]::ReadAllText((Join-Path $work 'check-expect.txt'))) -split ' '
    Check 'imported items survive save and restart' ((Cmd "count $($want[0])") -match "count=$($want[1])$")
    Cmd 'editor' | Out-Null; Press 'Tab3' | Out-Null
    Check 'the vault opens with no test files left' ((Cmd 'ui set VaultQuery uicheck-') -ne $null -and (Cmd 'uitext VaultListRange') -match "text='0개'")
    Press 'VaultQueryClear' | Out-Null; Press 'Close' | Out-Null
  }
```

빨간 불: 지금 빌드로 회차를 열어 `vault`가 실패하는 것을 본다(보관함 탭이 없어 `Tab3`이 경지 탭이고 `VaultExport`를 찾지 못한다).

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin bagx-red
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part vault 2>&1 | Select-Object -Last 3
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End bagx-red
```

Expected: `VaultExport`를 찾지 못해 멈춘다. `exports\bag`에 `uicheck-` 파일이 남지 않는다.

- [ ] **Step 2: `Services/Json.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MOD_sWP6UL
{
    // 작은 JSON 읽기. 내보내기 파일을 읽는 데 필요한 만큼만 한다(객체, 배열, 문자열, 수, true/false/null).
    // 외부 라이브러리를 쓰지 않는 이유: 모드의 산출물은 DLL 하나여야 한다.
    // 값의 형식: 객체는 Dictionary<string, object>, 배열은 List<object>, 문자열은 string, 정수는 long, 소수는 double, 불은 bool.
    // 문법이 틀리면 FormatException을 던진다.
    internal static class Json
    {
        public static object Parse(string text)
        {
            if (text == null)
            {
                throw new FormatException("빈 내용");
            }
            int i = 0;
            object value = Value(text, ref i);
            SkipSpace(text, ref i);
            if (i != text.Length)
            {
                throw new FormatException(i + "번째 글자: 값 뒤에 다른 글자가 있습니다");
            }
            return value;
        }

        // 문자열을 JSON의 따옴표 글로 만든다.
        public static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        private static void SkipSpace(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r' || s[i] == '\uFEFF'))
            {
                i++;
            }
        }

        private static FormatException Bad(int i, string what)
        {
            return new FormatException(i + "번째 글자: " + what);
        }

        private static object Value(string s, ref int i)
        {
            SkipSpace(s, ref i);
            if (i >= s.Length)
            {
                throw Bad(i, "값이 있어야 할 자리에서 끝났습니다");
            }
            char c = s[i];
            if (c == '{')
            {
                return Object(s, ref i);
            }
            if (c == '[')
            {
                return Array(s, ref i);
            }
            if (c == '"')
            {
                return String(s, ref i);
            }
            if (Word(s, ref i, "true"))
            {
                return true;
            }
            if (Word(s, ref i, "false"))
            {
                return false;
            }
            if (Word(s, ref i, "null"))
            {
                return null;
            }
            return Number(s, ref i);
        }

        private static bool Word(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0)
            {
                return false;
            }
            i += word.Length;
            return true;
        }

        private static object Object(string s, ref int i)
        {
            var result = new Dictionary<string, object>();
            i++;
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == '}')
            {
                i++;
                return result;
            }
            while (true)
            {
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != '"')
                {
                    throw Bad(i, "이름(따옴표 글)이 있어야 합니다");
                }
                string key = String(s, ref i);
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != ':')
                {
                    throw Bad(i, "':'이 있어야 합니다");
                }
                i++;
                result[key] = Value(s, ref i);
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }
                if (i < s.Length && s[i] == '}')
                {
                    i++;
                    return result;
                }
                throw Bad(i, "',' 또는 '}'가 있어야 합니다");
            }
        }

        private static object Array(string s, ref int i)
        {
            var result = new List<object>();
            i++;
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == ']')
            {
                i++;
                return result;
            }
            while (true)
            {
                result.Add(Value(s, ref i));
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }
                if (i < s.Length && s[i] == ']')
                {
                    i++;
                    return result;
                }
                throw Bad(i, "',' 또는 ']'가 있어야 합니다");
            }
        }

        private static string String(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;
            while (true)
            {
                if (i >= s.Length)
                {
                    throw Bad(i, "따옴표가 닫히지 않았습니다");
                }
                char c = s[i++];
                if (c == '"')
                {
                    return sb.ToString();
                }
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= s.Length)
                {
                    throw Bad(i, "역슬래시 뒤에서 끝났습니다");
                }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        int code;
                        if (i + 4 > s.Length || !int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                        {
                            throw Bad(i, "\\u 뒤에 16진수 네 자리가 있어야 합니다");
                        }
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default:
                        // \" \\ \/ 는 그 글자 그대로
                        sb.Append(e);
                        break;
                }
            }
        }

        private static object Number(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E'))
            {
                i++;
            }
            string text = s.Substring(start, i - start);
            long whole;
            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out whole))
            {
                return whole;
            }
            double real;
            if (text.Length > 0 && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out real))
            {
                return real;
            }
            throw Bad(start, "값을 읽을 수 없습니다");
        }
    }
}
```

- [ ] **Step 3: `Services/BagExportService.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 가방 내보내기·가져오기. 화면을 모르고 결과를 문장으로 돌려준다.
    // 아이템은 읽어 둔 값으로 PropsData.New로 다시 만들어 넣는다(실측: 번호·수량·종류·값이 원본과 같다).
    // 근거: docs/superpowers/specs/2026-10-13-bag-export-design.md 3절.
    internal static class BagExportService
    {
        public class Item
        {
            public int Id;
            public string Name = "";
            public int Count;
            public bool Martial;        // 비급. 번호가 0이고 내용이 Values에 있다
            public int[] Values = new int[0];   // 머리 세 칸(종류, 번호, 수량) 뒤의 값
            public bool Equipped;
            public string Problem;      // 파일의 이 줄을 쓸 수 없는 이유. null이면 쓸 수 있다
        }

        public class Money
        {
            public int Id;
            public string Name = "";
            public int Count;
            public string Problem;
        }

        public class Pack
        {
            public string File;
            public string Name = "";
            public string Date = "";
            public string Character = "";
            public int Skipped;
            public List<Item> Items = new List<Item>();
            public List<Money> Currencies = new List<Money>();
            public string Error;        // 읽을 수 없는 이유. null이면 읽혔다
        }

        private const int HeadLength = 3;   // values의 머리: 종류, 번호, 수량

        public static string Dir
        {
            get { return Path.Combine(Path.Combine(ChangeLog.DataDir, "exports"), "bag"); }
        }

        private static DataProps Bag
        {
            get { return g.world.playerUnit.data.unitData.propData; }
        }

        // 지금 가방에 비어 있는 칸. 넘쳐 있으면 0.
        public static int FreeSlots()
        {
            DataProps bag = Bag;
            return Math.Max(0, bag.GetGridMaxNum() - bag.GetGridCurNum());
        }

        private static bool Stacks(int propsId)
        {
            ConfItemPropsItem conf = g.conf.itemProps.GetItem(propsId);
            return conf != null && conf.isOverlay != 0;
        }

        // 이 묶음을 전부 넣을 때 필요한 칸. 겹쳐지는 것은 가방에 이미 있으면 칸을 쓰지 않고, 없으면 종류마다 한 칸이다.
        public static int NeededSlots(Pack pack)
        {
            DataProps bag = Bag;
            int need = 0;
            var newStacks = new HashSet<int>();
            foreach (Item item in pack.Items)
            {
                if (Usable(item) != null)
                {
                    continue;
                }
                if (!item.Martial && Stacks(item.Id))
                {
                    if (bag.GetPropsNum(item.Id) == 0 && newStacks.Add(item.Id))
                    {
                        need++;
                    }
                }
                else
                {
                    need++;
                }
            }
            return need;
        }

        // ---- 내보내기 ----

        private static string FileNameFor(string name)
        {
            var sb = new StringBuilder();
            char[] bad = Path.GetInvalidFileNameChars();
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
            }
            return sb.ToString().Trim().TrimEnd('.');
        }

        public static string Export(string name)
        {
            if (!EditService.WorldReady)
            {
                return "내보내기: 세이브에 진입한 뒤에 사용할 수 있습니다.";
            }
            string stem = FileNameFor((name ?? "").Trim());
            if (stem.Length == 0)
            {
                return "내보내기: 이름을 입력하세요.";
            }
            WorldUnitBase me = g.world.playerUnit;
            DataProps bag = Bag;
            var sb = new StringBuilder();
            int items = 0, skipped = 0, currencies = 0;
            var itemLines = new List<string>();
            for (int i = 0; i < bag.allProps.Count; i++)
            {
                DataProps.PropsData d = bag.allProps[i];
                Il2CppStringArray strs = d.GetValuesStrArray();
                // 문자열을 가진 아이템은 이 세이브의 NPC 등을 가리킨다(종문 추천서). 다른 세이브에서는 뜻이 없으므로 담지 않는다.
                if (strs != null && strs.Length > 0)
                {
                    skipped++;
                    continue;
                }
                bool martial = d.propsType == DataProps.PropsDataType.Martial;
                string label;
                try
                {
                    label = martial ? MartialService.Name(d.To<DataProps.MartialData>()) : (EditService.ItemName(d.propsID) ?? "");
                }
                catch (Exception)
                {
                    label = martial ? "(비급)" : "";
                }
                var values = new StringBuilder();
                for (int v = HeadLength; d.values != null && v < d.values.Length; v++)
                {
                    values.Append(v == HeadLength ? "" : ", ").Append(d.values[v]);
                }
                itemLines.Add("    { \"id\": " + d.propsID + ", \"name\": " + Json.Quote(label) + ", \"count\": " + d.propsCount
                    + ", \"type\": \"" + (martial ? "Martial" : "Props") + "\", \"values\": [" + values + "], \"equipped\": "
                    + (me.data.unitData.IsEquipProps(d.soleID) ? "true" : "false") + " }");
                items++;
            }
            var moneyLines = new List<string>();
            foreach (ItemCatalog.Entry entry in ItemCatalog.Currencies)
            {
                int count = bag.GetPropsNum(entry.Id);
                if (count > 0)
                {
                    moneyLines.Add("    { \"id\": " + entry.Id + ", \"name\": " + Json.Quote(entry.Name) + ", \"count\": " + count + " }");
                    currencies++;
                }
            }
            sb.Append("{\n  \"format\": 1,\n  \"kind\": \"bag\",\n  \"name\": ").Append(Json.Quote(name.Trim()))
                .Append(",\n  \"exportedAt\": \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append("\",\n  \"character\": ").Append(Json.Quote(EditService.NameOf(me)))
                .Append(",\n  \"skipped\": ").Append(skipped)
                .Append(",\n  \"items\": [\n").Append(string.Join(",\n", itemLines.ToArray())).Append("\n  ],\n  \"currencies\": [\n")
                .Append(string.Join(",\n", moneyLines.ToArray())).Append("\n  ]\n}\n");
            try
            {
                Directory.CreateDirectory(Dir);
                string path = Path.Combine(Dir, stem + ".json");
                for (int n = 2; File.Exists(path); n++)
                {
                    path = Path.Combine(Dir, stem + " (" + n + ").json");
                }
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                return "내보냈습니다: " + Path.GetFileName(path) + " (아이템 " + items + "줄, 화폐 " + currencies + "종"
                    + (skipped > 0 ? ", 이 세이브에 묶인 아이템 " + skipped + "개는 뺐습니다" : "") + ")";
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

        // ---- 읽기 ----

        private static string Text(Dictionary<string, object> o, string key)
        {
            object v;
            return o.TryGetValue(key, out v) && v is string ? (string)v : "";
        }

        // 정수로 읽는다. 없거나 정수가 아니거나 int 범위를 넘으면 false.
        private static bool Whole(Dictionary<string, object> o, string key, out int value)
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

        private static Pack Read(string file)
        {
            var pack = new Pack { File = file, Name = Path.GetFileNameWithoutExtension(file) };
            try
            {
                var root = Json.Parse(File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>;
                int format;
                if (root == null || !Whole(root, "format", out format) || format != 1 || Text(root, "kind") != "bag")
                {
                    pack.Error = "이 편집기의 가방 내보내기 파일(format 1)이 아닙니다";
                    return pack;
                }
                string name = Text(root, "name");
                if (name.Length > 0)
                {
                    pack.Name = name;
                }
                pack.Date = Text(root, "exportedAt");
                pack.Character = Text(root, "character");
                Whole(root, "skipped", out pack.Skipped);
                object list;
                if (root.TryGetValue("items", out list) && list is List<object>)
                {
                    foreach (object each in (List<object>)list)
                    {
                        pack.Items.Add(ReadItem(each as Dictionary<string, object>));
                    }
                }
                if (root.TryGetValue("currencies", out list) && list is List<object>)
                {
                    foreach (object each in (List<object>)list)
                    {
                        pack.Currencies.Add(ReadMoney(each as Dictionary<string, object>));
                    }
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

        private static Item ReadItem(Dictionary<string, object> o)
        {
            var item = new Item();
            if (o == null)
            {
                item.Problem = "줄의 모양이 틀립니다";
                return item;
            }
            item.Name = Text(o, "name");
            item.Martial = Text(o, "type") == "Martial";
            object flag;
            item.Equipped = o.TryGetValue("equipped", out flag) && flag is bool && (bool)flag;
            if (!Whole(o, "id", out item.Id) || !Whole(o, "count", out item.Count))
            {
                item.Problem = "번호나 수량이 숫자가 아닙니다";
                return item;
            }
            object values;
            if (o.TryGetValue("values", out values) && values is List<object>)
            {
                var list = (List<object>)values;
                item.Values = new int[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    if (!(list[i] is long) || (long)list[i] < int.MinValue || (long)list[i] > int.MaxValue)
                    {
                        item.Problem = "값 배열에 숫자가 아닌 것이 있습니다";
                        return item;
                    }
                    item.Values[i] = (int)(long)list[i];
                }
            }
            return item;
        }

        private static Money ReadMoney(Dictionary<string, object> o)
        {
            var money = new Money();
            if (o == null)
            {
                money.Problem = "줄의 모양이 틀립니다";
                return money;
            }
            money.Name = Text(o, "name");
            if (!Whole(o, "id", out money.Id) || !Whole(o, "count", out money.Count))
            {
                money.Problem = "번호나 수량이 숫자가 아닙니다";
            }
            return money;
        }

        // 내보낸 것들. 최신이 앞. 폴더가 없으면 빈 목록.
        public static List<Pack> List()
        {
            var result = new List<Pack>();
            try
            {
                if (!Directory.Exists(Dir))
                {
                    return result;
                }
                var files = new List<string>(Directory.GetFiles(Dir, "*.json"));
                files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                foreach (string file in files)
                {
                    result.Add(Read(file));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return result;
        }

        // ---- 가져오기 ----

        // 이 줄을 넣을 수 없는 이유. null이면 넣을 수 있다.
        private static string Usable(Item item)
        {
            if (item.Problem != null)
            {
                return item.Problem;
            }
            if (item.Count <= 0)
            {
                return "수량이 1보다 작습니다";
            }
            if (!item.Martial && g.conf.itemProps.GetItem(item.Id) == null)
            {
                return "게임에 없는 아이템 번호입니다";
            }
            return null;
        }

        private static string Usable(Money money)
        {
            if (money.Problem != null)
            {
                return money.Problem;
            }
            if (money.Count <= 0)
            {
                return "수량이 1보다 작습니다";
            }
            foreach (ItemCatalog.Entry entry in ItemCatalog.Currencies)
            {
                if (entry.Id == money.Id)
                {
                    return null;
                }
            }
            return "화폐 목록에 없는 번호입니다";
        }

        // 아이템 한 줄을 가방에 넣는다. 넣은 뒤 실제로 늘었는지 본다(가방이 가득 차면 게임이 예외 없이 넣지 않는다).
        private static bool Put(Item item)
        {
            DataProps bag = Bag;
            int before = item.Martial ? bag.allProps.Count : bag.GetPropsNum(item.Id);
            var tail = new Il2CppStructArray<int>(item.Values.Length);
            for (int i = 0; i < item.Values.Length; i++)
            {
                tail[i] = item.Values[i];
            }
            DataProps.PropsData made = DataProps.PropsData.New(item.Id, item.Count,
                item.Martial ? DataProps.PropsDataType.Martial : DataProps.PropsDataType.Props, tail);
            bag.AddProps(made);
            int after = item.Martial ? bag.allProps.Count : bag.GetPropsNum(item.Id);
            return after == before + (item.Martial ? 1 : item.Count);
        }

        private static bool PutMoney(Money money)
        {
            WorldUnitBase me = g.world.playerUnit;
            int before = Bag.GetPropsNum(money.Id);
            if ((long)before + money.Count > int.MaxValue)
            {
                return false;
            }
            if (money.Id == PropsIDType.Money)
            {
                me.data.RewardPropMoney(money.Count);
            }
            else
            {
                me.data.RewardPropItem(money.Id, money.Count, false);
            }
            return Bag.GetPropsNum(money.Id) == before + money.Count;
        }

        private static string Label(Item item)
        {
            return (item.Name.Length > 0 ? item.Name : item.Martial ? "(비급)" : "아이템") + (item.Martial ? "" : "(" + item.Id + ")");
        }

        // 묶음의 파일이 아직 있는지. 목록을 본 뒤 밖에서 지워졌을 수 있다.
        private static string Gone(Pack pack)
        {
            return File.Exists(pack.File) ? null : "가져오기: 파일이 없습니다(" + Path.GetFileName(pack.File) + "). 목록을 다시 여세요.";
        }

        public static string ImportItem(Pack pack, int index)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            Item item = pack.Items[index];
            string problem = Usable(item);
            if (problem != null)
            {
                return Label(item) + ": " + problem + ". 가져오지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            bool ok = Put(item);
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), "가방 가져오기 " + Label(item), "+0", ok ? "+" + item.Count : "+0", ok);
            return ok ? Label(item) + " x" + item.Count + "을(를) 가져왔습니다."
                : Label(item) + ": 가방에 들어가지 않았습니다(빈 칸을 확인하세요).";
        }

        public static string ImportCurrency(Pack pack, int index)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            Money money = pack.Currencies[index];
            string problem = Usable(money);
            if (problem != null)
            {
                return money.Name + "(" + money.Id + "): " + problem + ". 가져오지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            bool ok = PutMoney(money);
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), "가방 가져오기 " + money.Name + "(" + money.Id + ")", "+0", ok ? "+" + money.Count : "+0", ok);
            return ok ? money.Name + " +" + money.Count + "을(를) 가져왔습니다." : money.Name + ": 수량이 기대한 만큼 오르지 않았습니다.";
        }

        // 전부 가져온다. expandBag이면 모자란 칸만큼 가방의 추가 칸을 먼저 늘린다(10칸 단위로 올림, 상한까지).
        public static string ImportAll(Pack pack, bool expandBag)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string grown = "";
            if (expandBag)
            {
                int missing = NeededSlots(pack) - FreeSlots();
                int room = BagService.MaxExtra - BagService.Extra;
                int add = Math.Min(room, (missing + BagService.Step - 1) / BagService.Step * BagService.Step);
                if (missing > 0 && add > 0)
                {
                    BagService.Change(add);
                    grown = " 가방 추가 칸 +" + add + ".";
                }
            }
            int put = 0, skipped = 0;
            var failed = new List<string>();
            // 겹쳐지는 것부터 넣는다. 칸을 쓰지 않거나 한 칸만 쓰므로, 뒤의 것 때문에 자리를 잃지 않는다.
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (Item item in pack.Items)
                {
                    bool stacks = !item.Martial && item.Problem == null && Stacks(item.Id);
                    if (stacks != (pass == 0))
                    {
                        continue;
                    }
                    if (Usable(item) != null)
                    {
                        skipped++;
                        continue;
                    }
                    if (Put(item))
                    {
                        put++;
                    }
                    else
                    {
                        failed.Add(Label(item));
                    }
                }
            }
            int money = 0;
            foreach (Money each in pack.Currencies)
            {
                if (Usable(each) != null)
                {
                    skipped++;
                }
                else if (PutMoney(each))
                {
                    money++;
                }
                else
                {
                    failed.Add(each.Name);
                }
            }
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), "가방 가져오기 전부(" + pack.Name + ")", "-",
                "아이템 " + put + "줄, 화폐 " + money + "종, 못 넣음 " + failed.Count + "줄", failed.Count == 0);
            var sb = new StringBuilder(pack.Name + ": 아이템 " + put + "줄, 화폐 " + money + "종을 가져왔습니다." + grown);
            if (skipped > 0)
            {
                sb.Append(" 건너뜀 ").Append(skipped).Append("줄(쓸 수 없는 줄).");
            }
            if (failed.Count > 0)
            {
                sb.Append(" 못 넣음 ").Append(failed.Count).Append("줄(가방이 가득 참): ");
                for (int i = 0; i < failed.Count && i < 5; i++)
                {
                    sb.Append(i == 0 ? "" : ", ").Append(failed[i]);
                }
                if (failed.Count > 5)
                {
                    sb.Append(" 외 ").Append(failed.Count - 5).Append("줄");
                }
            }
            return sb.ToString();
        }

        // 내보내기 파일을 지운다. 내보내기 폴더 안의 .json만 지운다.
        public static string Delete(Pack pack)
        {
            try
            {
                string full = Path.GetFullPath(pack.File);
                if (!full.StartsWith(Path.GetFullPath(Dir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
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
    }
}
```

- [ ] **Step 4: `UI/Tabs/VaultTab.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 보관함 탭: 가방을 파일로 내보내고, 내보낸 것을 골라 지금 가방에 더한다. 하위 화면: 한 묶음의 세부 목록.
    internal sealed class VaultTab : EditorTab
    {
        private sealed class Line
        {
            public bool IsMoney;
            public int Index;       // 묶음의 Items 또는 Currencies 안의 순번
            public string Search;
        }

        private readonly ListView<BagExportService.Pack> list = new ListView<BagExportService.Pack>("VaultList", 40);
        private readonly ListView<Line> detailList = new ListView<Line>("VaultItems", 34);
        private List<BagExportService.Pack> packs;   // null이면 다시 읽는다
        private string exportName = "";
        private string query = "";
        private BagExportService.Pack opened;
        private string detailQuery = "";

        public override string Title
        {
            get { return "보관함"; }
        }

        public VaultTab()
        {
            list.EmptyText = "내보낸 것이 없습니다. 위에서 이름을 적고 내보내세요.";
            detailList.EmptyText = "조건에 맞는 줄이 없습니다.";
        }

        // 탭으로 들어올 때마다 폴더를 다시 읽는다(파일을 밖에서 넣거나 지웠을 수 있다).
        public override void OnShow()
        {
            packs = null;
        }

        public override void OnOpen()
        {
            packs = null;
        }

        private List<BagExportService.Pack> Found()
        {
            if (query.Length == 0)
            {
                return packs;
            }
            return packs.FindAll(p => p.Name.Contains(query) || System.IO.Path.GetFileName(p.File).Contains(query));
        }

        protected override void Draw()
        {
            if (packs == null)
            {
                packs = BagExportService.List();
            }
            Ui.Label(content, "이름", 0, 0, 60, 38, 18).color = Ui.Dim;
            Ui.Input("VaultName", content, exportName, "내보낼 이름", 60, 0, 330, 38, new Action<string>(v => exportName = v));
            Ui.Button("VaultExport", content, "현재 가방 내보내기", 400, 0, 230, 38, new Action(() =>
            {
                string result = BagExportService.Export(exportName);
                packs = null;
                list.Reset();
                Say(result);
            }));
            Text note = Ui.Label(content, "가방의 아이템(장착 중인 것 포함), 영석과 화폐를 담습니다. 이 세이브에 묶인 아이템(종문 추천서 등)은 담지 않습니다.", 646, 0, 650, 38, 14);
            note.color = Ui.Dim;
            note.horizontalOverflow = HorizontalWrapMode.Wrap;

            Ui.Label(content, "찾기", 0, 48, 60, 38, 18).color = Ui.Dim;
            SearchBox.Build(content, "VaultQuery", 60, 48, 330, query, "이름의 일부", new Action<string>(v =>
            {
                query = v;
                list.SetItems(Found());
            }));
            Ui.Label(content, "가져오기는 항상 지금 가방에 더합니다. 파일은 " + BagExportService.Dir + " 에 있습니다.", 400, 48, 900, 38, 14).color = Ui.Dim;

            Ui.Panel("VaultHead", content, 0, 96, ContentW, 30, Ui.Section);
            Ui.Label(content, "이름", 8, 96, 300, 30, 16).color = Ui.Dim;
            Ui.Label(content, "날짜", 330, 96, 200, 30, 16).color = Ui.Dim;
            Ui.Label(content, "인물", 540, 96, 150, 30, 16).color = Ui.Dim;
            Ui.Label(content, "아이템", 700, 96, 80, 30, 16).color = Ui.Dim;
            Ui.Label(content, "화폐", 790, 96, 60, 30, 16).color = Ui.Dim;
            list.Build(content, 0, 128, ContentW, ContentH - 128, Found(), DrawRow);
        }

        private void DrawRow(RectTransform row, BagExportService.Pack pack, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            if (pack.Error != null)
            {
                Text bad = Ui.Label(row, "(읽을 수 없음) " + System.IO.Path.GetFileName(pack.File) + " — " + Shorten(pack.Error, 40), 8, 0, right - 150, 40, 16);
                bad.color = Ui.Warn;
            }
            else
            {
                Ui.Label(row, Shorten(pack.Name, 18), 8, 0, 320, 40);
                Ui.Label(row, pack.Date, 330, 0, 200, 40, 16);
                Ui.Label(row, Shorten(pack.Character, 8), 540, 0, 150, 40, 16);
                Ui.Label(row, pack.Items.Count.ToString(), 700, 0, 80, 40, 16);
                Ui.Label(row, pack.Currencies.Count.ToString(), 790, 0, 60, 40, 16);
                Ui.Button("VaultOpen" + index, row, "세부", right - 566, 3, 90, 34, new Action(() =>
                {
                    opened = pack;
                    detailQuery = "";
                    detailList.Reset();
                    Push(() => opened.Name + " — " + opened.Character + ", " + opened.Date, DrawDetail);
                }));
                string key = "vault-all:" + pack.File;
                if (Confirm.IsPending(key))
                {
                    // 칸이 모자라 물은 상태. 가방을 늘릴지는 사용자가 고른다(추가 칸은 이 세이브에 계속 남는 변경이다).
                    Ui.Button("VaultExpand" + index, row, "가방을 늘리고 모두", right - 470, 3, 190, 34, new Action(() =>
                    {
                        Confirm.Clear();
                        Say(BagExportService.ImportAll(pack, true));
                    }), true, true);
                    Ui.Button("VaultFit" + index, row, "들어가는 만큼만", right - 274, 3, 160, 34, new Action(() =>
                    {
                        Confirm.Clear();
                        Say(BagExportService.ImportAll(pack, false));
                    }), true, true);
                }
                else
                {
                    Ui.Button("VaultAll" + index, row, "모두 가져오기", right - 470, 3, 160, 34, new Action(() =>
                    {
                        int need = BagExportService.NeededSlots(pack);
                        int free = BagExportService.FreeSlots();
                        if (need > free)
                        {
                            Confirm.Ask(key, pack.Name + ": 필요 " + need + "칸, 빈 칸 " + free + "칸. 가방을 늘리고 모두 가져올지, 들어가는 만큼만 가져올지 고르세요.");
                            return;
                        }
                        Say(BagExportService.ImportAll(pack, false));
                    }));
                }
            }
            Confirm.Button("VaultDelete" + index, row, "삭제", "정말 삭제", right - 108, 3, 100, 34, "vault-del:" + pack.File,
                System.IO.Path.GetFileName(pack.File) + ": 파일을 지웁니다. 되돌릴 수 없습니다. \"정말 삭제\"를 한 번 더 누르세요.", new Action(() =>
                {
                    string result = BagExportService.Delete(pack);
                    packs = null;
                    Say(result);
                }));
        }

        private List<Line> DetailLines()
        {
            var lines = new List<Line>();
            for (int i = 0; i < opened.Items.Count; i++)
            {
                BagExportService.Item item = opened.Items[i];
                lines.Add(new Line { IsMoney = false, Index = i, Search = item.Name + " " + item.Id });
            }
            for (int i = 0; i < opened.Currencies.Count; i++)
            {
                BagExportService.Money money = opened.Currencies[i];
                lines.Add(new Line { IsMoney = true, Index = i, Search = money.Name + " " + money.Id });
            }
            return detailQuery.Length == 0 ? lines : lines.FindAll(l => l.Search.Contains(detailQuery));
        }

        // 하위 화면: 한 묶음의 아이템과 화폐. 줄마다 하나씩 가져온다.
        private void DrawDetail()
        {
            float y = SubTop;
            SearchBox.Build(content, "VaultItemQuery", 0, y, 330, detailQuery, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                detailQuery = v;
                detailList.SetItems(DetailLines());
            }));
            Ui.Label(content, "아이템 " + opened.Items.Count + "줄 · 화폐 " + opened.Currencies.Count + "종"
                + (opened.Skipped > 0 ? " · 내보낼 때 뺀 것 " + opened.Skipped + "개" : "") + "    가져온 장비는 가방에 들어가고 자동으로 장착하지 않습니다.",
                350, y, 950, 38, 15).color = Ui.Dim;
            Ui.Panel("VaultItemHead", content, 0, y + 46, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", 8, y + 46, 110, 30, 16).color = Ui.Dim;
            Ui.Label(content, "이름", 130, y + 46, 400, 30, 16).color = Ui.Dim;
            Ui.Label(content, "수량", 540, y + 46, 120, 30, 16).color = Ui.Dim;
            Ui.Label(content, "종류", 670, y + 46, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "장착", 780, y + 46, 80, 30, 16).color = Ui.Dim;
            float top = y + 78;
            detailList.Build(content, 0, top, ContentW, ContentH - top, DetailLines(), DrawDetailRow);
        }

        private void DrawDetailRow(RectTransform row, Line line, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            BagExportService.Pack pack = opened;
            if (line.IsMoney)
            {
                BagExportService.Money money = pack.Currencies[line.Index];
                Ui.Label(row, money.Id.ToString(), 8, 0, 110, 34, 16);
                Ui.Label(row, Shorten(money.Name, 24), 130, 0, 400, 34, 17);
                Ui.Label(row, money.Count.ToString(), 540, 0, 120, 34, 16);
                Ui.Label(row, "화폐", 670, 0, 100, 34, 16).color = Ui.Warn;
                int at = line.Index;
                Ui.Button("VaultGetC" + at, row, "가져오기", right - 128, 2, 120, 30, new Action(() => Say(BagExportService.ImportCurrency(pack, at))));
                return;
            }
            BagExportService.Item item = pack.Items[line.Index];
            Ui.Label(row, item.Martial ? "-" : item.Id.ToString(), 8, 0, 110, 34, 16);
            Ui.Label(row, Shorten(item.Name, 24), 130, 0, 400, 34, 17);
            Ui.Label(row, item.Count.ToString(), 540, 0, 120, 34, 16);
            Ui.Label(row, item.Martial ? "비급" : "아이템", 670, 0, 100, 34, 16);
            Ui.Label(row, item.Equipped ? "장착 중" : "", 780, 0, 80, 34, 16).color = Ui.Warn;
            int itemAt = line.Index;
            Ui.Button("VaultGet" + itemAt, row, "가져오기", right - 128, 2, 120, 30, new Action(() => Say(BagExportService.ImportItem(pack, itemAt))));
        }
    }
}
```

- [ ] **Step 5: `UI/EditorWindow.cs`에 탭을 넣는다**

- `new CurrencyTab(), new GradeTab()` → `new CurrencyTab(), new VaultTab(), new GradeTab()`
- `150, 10 * 52)` → `150, 11 * 52)`

- [ ] **Step 6: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 7: 회차 — `vault`, `vault-restart`, 그리고 기존 검사**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin bagx-t1
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part vault
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part vault-restart
foreach ($part in 'base','tabs','items','numbers','people','history','confirm','actions','status','order') { pwsh -NoProfile -File tools\research\ui-check.ps1 -Part $part 2>&1 | Select-Object -Last 1 }
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End bagx-t1
Get-ChildItem "$env:USERPROFILE\AppData\LocalLow\guigugame\guigubahuang\TlToybox\exports\bag" -Filter 'uicheck-*'
```

Expected: 모두 `FAILS=0`. 마지막 명령은 아무것도 내지 않는다(검사 파일이 남지 않았다). 보관함 탭과 세부 화면을 캡처해 배치가 겹치지 않는지 눈으로 본다.

- [ ] **Step 8: 커밋한다**

```powershell
git -C E:\TlToybox add src tools
git -C E:\TlToybox commit -m @'
feat: 가방 내보내기·가져오기 — 보관함 탭

현재 가방의 아이템(장착 중인 것 포함)과 영석·화폐를 이름을 붙여 JSON 파일로
내보내고, 보관함 탭에서 골라 전부 또는 하나씩 지금 가방에 더한다. 이 세이브에
묶인 아이템은 담지 않는다. 칸이 모자라면 가방을 늘릴지 들어가는 만큼만 넣을지
묻고, 넣은 뒤 실제로 들어갔는지 확인해 못 넣은 것을 알린다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 2: 전체 검증, 문서, 병합, 설치

**Files:**
- Modify: `docs/05-editor-v0.1.md` (28절), `docs/06-handoff.md`

- [ ] **Step 1: 남은 검사와 배포 구성**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin bagx-final
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part freeze
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part battle
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End bagx-final
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin bagx-final-b
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part bag
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End bagx-final-b
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin bagx-rel -ReleaseBuild
& tools\DevRemote\win.ps1 -Do key -Vk 0x2D
Start-Sleep -Seconds 1
& tools\DevRemote\rc.ps1 -Cmd 'hit 151 146','click 151 302' -NoShot
Select-String -LiteralPath 'E:\Games\Tale of Immortal\MelonLoader\Latest.log' -Pattern 'trampoline'
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End bagx-rel
```

Expected: `freeze`·`battle`·`bag` 모두 `FAILS=0`. 배포 구성에서 `click`이 `Button.onClick: Tab3`(보관함), `trampoline` 줄 없음.

- [ ] **Step 2: `docs/05-editor-v0.1.md`의 맨 끝에 28절을 넣는다**

스펙 3절의 실측 표와 "확인하지 못한 것", 5.1절의 파일 형식을 옮기고, 검증 결과(부분별 PASS 수), 구현하며 고친 것, 한계를 적는다. 제목은 `## 28. 가방 내보내기·가져오기 — 보관함 탭 (2026-10-13 추가)`. 세이브가 하나뿐이라 다른 세이브로 옮기는 것은 "미확인"으로 적는다.

- [ ] **Step 3: `docs/06-handoff.md`를 갱신한다**

- 1절: 직전 작업을 "묶음 C를 끝냈다(05 문서 28절). 다음은 묶음 D다(8.5절)"로, 설치 시각을 이번 설치로.
- 2절: 탭 순서에 보관함(화폐 다음), 표에 보관함 줄(`BagExportService`, `Json`, 28절). 데이터 폴더 설명에 `exports\bag\`.
- 3절: 05 문서의 절 범위 1~28절, `Services/`에 `Json.cs`·`BagExportService.cs`.
- 5절: `ui-check.ps1`의 부분에 `vault`, `vault-restart`(앞 부분 뒤에 `-Restart`). "검사가 만든 내보내기 파일은 `uicheck-`로 시작하고 검사가 지운다."
- 8.1절: C를 "끝남", D를 "**다음 차례.** 설계 시작 전". 8.4절 첫 줄을 "끝났다"로 바꾸고 D가 다시 쓸 것(보관함 탭, `Json`, 파일 폴더 `exports\`)을 적는다.
- 8.7절: `feat/bag-export-research` 브랜치를 실험 브랜치 목록에 보탠다.

- [ ] **Step 4: 문서를 커밋하고 `develop`에 병합, 설치**

```powershell
git -C E:\TlToybox add docs
git -C E:\TlToybox commit -m @'
docs: 가방 내보내기·가져오기의 기록(05 문서 28절), 인계 문서 갱신

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
git -C E:\TlToybox switch develop
git -C E:\TlToybox merge --no-ff feat/bag-export -m @'
Merge branch 'feat/bag-export' into develop

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
pwsh -NoProfile -File E:\TlToybox\tools\install.ps1
```

Expected: 충돌 없이 병합되고, 설치 스크립트의 마지막 줄에 오늘 날짜의 DLL이 나온다. 게임이 켜져 있으면 사용자에게 알리고 기다린다.

- [ ] **Step 5: 사용자에게 알린다**

바뀐 것(보관함 탭의 쓰는 법, 파일 위치), 검증 결과, 확인하지 못한 것(강화 장비, 가져온 비급 배우기, 다른 세이브로 옮기기)을 알리고, 직접 써 보기를 부탁한다.
