# 화면 구조 정리 2차 — 공통 부품과 탭별 개선 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 목록을 스크롤로 통일하고, 검색을 즉시 걸리게 하고, 숫자를 쓰고 Enter로 적용하게 하고, 확인 대기를 한 방식으로 맞춘다. 가방 칸 한 번에 지정, 변경 이력 전체 보기, 의복 아이템의 구분 표시를 넣는다.

**Architecture:** 1차에서 만든 틀(`EditorWindow`, `EditorTab`) 위에 공통 부품 다섯 개(`ListView`, `SearchBox`, `DetailPane`, `Confirm`, `NumberField`)를 `UI/Widgets/`에 만들고, 탭 여덟 개의 그리는 코드를 부품으로 다시 쓴다. 탭이 버튼 동작 뒤에 내용을 다시 그리는 방식은 그대로이고, 검색어 입력과 스크롤 때는 목록이 자기 줄만 다시 그린다. 탭 안의 하위 화면은 `EditorTab.Push`/`Pop`으로 쌓는다.

**Tech Stack:** C# 7.3 / .NET Framework 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4(Unhollower), uGUI. 검증은 PowerShell 7 스크립트와 `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-13-ui-rework-design.md` (작업 순서 3~7번, 5.4의 `Push`/`Pop`, 5.5~5.8, 6절의 7~9, 11~14, 17, 18번)

**스펙에 없는 추가(사용자 지시, 2026-10-13):** 의복 아이템 295개는 게임의 이름과 설명이 모두 "의복"이다. 자원 탭에서 구분되도록 성별·모양·색을 이름과 설명에 보인다. 이 때문에 `Services/ItemCatalog.cs` 한 곳을 고친다(스펙은 `Services/`를 건드리지 않는다고 했다. 이 한 가지만 예외다).

## Global Constraints

- 대상: 귀곡팔황 v1.2.113.259, 게임 폴더 `E:\Games\Tale of Immortal`, 모드 ID `sWP6UL`, 네임스페이스 `MOD_sWP6UL`.
- `src/ModCode/ModMain/Services/`는 `ItemCatalog.cs`의 의복 표시 말고는 고치지 않는다. 서비스가 돌려주는 결과 문장과 변경 이력의 모양은 그대로다.
- 참조를 추가하지 않는다. `bin\Release`에는 `MOD_sWP6UL.dll` 하나만 있어야 한다.
- 개발 구성과 배포 구성(`-p:DevTools=false`) 모두 경고 0, 오류 0.
- 기능은 줄이지 않는다. 없어지는 것은 `검색` 버튼, 쪽 넘김 버튼(`이전`·`다음`은 목록의 이동 줄로 대체), "페이지당" 선택, 능력치의 "직접 입력" 줄, `10쪽 뒤로`뿐이다.
- 이름을 유지하는 버튼과 입력 칸: `Day1`·`Day5`·`Day10`, `SkipMonth`, `Freeze`, `DateApply`, `DateYear`·`DateMonth`·`DateDay`, `BagMinus`·`BagPlus`, `God`, `Multiplier`, `Close`, `Collapse`, `ToPlayer`, `Tab0`~`Tab8`, 탭마다의 대표 버튼(`Restore`, `MoneyAdd`, `FillExp`, `SkillAddOpen`, `LuckAddOpen`, `ArtLearnAll`, `NpcRefresh`, `BackupNow`).
- 목록 안의 버튼은 줄 번호가 아니라 항목의 번호를 이름에 붙인다(스크롤해도 이름이 바뀌지 않게).
- 브랜치: `develop`에서 `feat/ui-rework-2`. `git merge --no-ff`. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 게임 검증은 `tools/DevRemote/session.ps1`의 회차 안에서만. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 코드 모양은 주변과 같게: 중괄호는 항상 쓰고 줄을 바꾼다, 들여쓰기 4칸, 주석은 한국어로 "왜"를 적는다.
- 검증 스크립트: 게임 화면을 닫은 뒤 고정 시간으로 기다리지 않는다. 화면 밖의 게임에 잇달아 보낸 키는 빠질 수 있으므로 키 사이는 300ms 이상 띄우고, 키가 들어갔는지를 먼저 확인한 뒤 판정한다.

## Review Focus

1. 검색 결과가 0개일 때, 그리고 목록이 보이는 줄 수보다 짧을 때 → 빈 목록 문구가 나오고 이동 버튼이 숨으며 예외가 없다. (Task 2 `items`)
2. 목록을 끝까지 내린 상태에서 검색어를 넣어 목록이 짧아진다 → 맨 위에서 다시 시작하고 빈 화면이 되지 않는다. (Task 2 `items`)
3. 숫자 칸에 빈 값, 음수, 아주 큰 수를 쓰고 Enter → 적용하지 않고 이유를 알리며 값이 그대로다. (Task 3 `numbers`)
4. 확인 대기 중에 다른 탭·다른 대상·하위 화면으로 갔다가 돌아온다 → 확인이 풀려 있고 한 번의 클릭으로 실행되지 않는다. (Task 4 `confirm`)
5. 하위 화면이 열린 채 편집 대상을 바꾸거나 탭을 떠난다 → 하위 화면이 닫히고 목록이 새 대상의 것이다. (Task 4 `confirm`)

## File Structure

| 파일 (`src/ModCode/ModMain/`) | 변경 | 맡는 일 |
|---|---|---|
| `UI/Widgets/ListView.cs` | 새 파일 | 보이는 줄만 만드는 스크롤 목록과 이동 줄 |
| `UI/Widgets/SearchBox.cs` | 새 파일 | 즉시 검색 입력 칸 |
| `UI/Widgets/DetailPane.cs` | 새 파일 | 고른 줄의 전체 글 |
| `UI/Widgets/Confirm.cs` | 새 파일 | 두 번 눌러 확인(창 전체에 하나) |
| `UI/Widgets/NumberField.cs` | 새 파일 | 숫자 입력 + Enter 적용 |
| `UI/Widgets/AmountBar.cs` | 수정 | 직접 입력 칸에서 Enter로 적용 |
| `UI/Tabs/EditorTab.cs` | 수정 | 하위 화면 쌓기(`Push`/`Pop`) |
| `UI/EditorWindow.cs` | 수정 | 그릴 때 확인 대기 정리, 탭을 떠날 때와 대상이 바뀔 때 하위 화면 닫기 |
| `UI/Tabs/ResourcesTab.cs`, `AttributesTab.cs`, `NpcTab.cs`, `SafetyTab.cs`, `MartialTab.cs`, `LuckTab.cs`, `ArtTab.cs`, `WorldTab.cs` | 다시 씀 | 부품으로 다시 그린다 |
| `Services/ItemCatalog.cs` | 수정 | 의복의 이름과 설명에 성별·모양·색 |
| `Safety/ChangeLog.cs` | 수정 | 지난 기록 읽기 |
| `tools/research/ui-check.ps1` | 수정 | 새 부분 `items`, `numbers`, `people`, `history`, `confirm`. 기존 `base`·`tabs`의 이름 갱신 |
| `docs/05-editor-v0.1.md`, `docs/06-handoff.md` | 수정 | 27절, 인계 문서 |

`GradeTab.cs`는 고치지 않는다(목록·검색·숫자 입력이 없다).

화면 좌표의 기준: 내용 영역은 가로 1304(`ContentW`), 세로 844(`ContentH`). 하위 화면에서는 위쪽 44가 제목과 `돌아가기`다(`SubTop`).

## 검증 회차의 공통 절차

1차 계획과 같다. 저장소 뿌리에서:

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin <이름>     # 마지막 줄이 WORLD READY
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part <부분>       # 마지막 줄 FAILS=0
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End <이름>       # SAVE RESTORED AND IDENTICAL
```

검사가 FAIL이어도 반드시 `-End`로 끝낸 뒤 고친다. `battle`·`status-battle`은 전투에 들어간 채 끝나므로 회차의 마지막에만 돌린다.

---

### Task 1: 공통 부품, 하위 화면, 검사 부분 미리 넣기

부품과 틀의 연결을 만든다. 이 Task에서 부품을 쓰는 곳은 `AmountBar` 하나다. 새 검사 부분을 모두 먼저 넣고 한 회차에서 실패하는 것을 본 뒤(빨간 불), 이후 Task가 한 부분씩 통과시킨다.

**Files:**
- Create: `src/ModCode/ModMain/UI/Widgets/ListView.cs`, `SearchBox.cs`, `DetailPane.cs`, `Confirm.cs`, `NumberField.cs`
- Modify: `src/ModCode/ModMain/UI/Widgets/AmountBar.cs`
- Modify: `src/ModCode/ModMain/UI/Tabs/EditorTab.cs`
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs` (`SetTarget`, `SwitchTab`, `Render`)
- Modify: `tools/research/ui-check.ps1`

**Interfaces:**
- Produces:
  - `ListView<T>`: `new ListView<T>(string name, float rowHeight)`; `Action<T> OnSelect`; `string EmptyText`; `int Count`; `void Reset()`(맨 위로); `void Build(Transform parent, float x, float y, float w, float h, IList<T> items, Action<RectTransform, T, int> drawRow)`; `void SetItems(IList<T> items)`(바꾸고 맨 위로, 줄만 다시 그림); `void Refresh()`. 요소 이름: 스크롤 영역 `<name>`, 줄 `Row<번호>`, 이동 줄 `<name>First`·`Prev`·`Next`·`Last`, 범위 글 `<name>Range`("전체 N개 중 a~b번째" / "0개").
  - `SearchBox.Build(Transform parent, string name, float x, float y, float w, string value, string placeholder, Action<string> onChanged)` → `InputField`. 지움 버튼 `<name>Clear`.
  - `DetailPane`: `new DetailPane(string name, string hint)`; `void Build(Transform parent, float x, float y, float w, float h)`; `void Show(string text)`; `void Clear()`.
  - `Confirm`: `bool IsPending(string key)`; `void Ask(string key, string message)`; `void Clear()`; `void OnRender()`; `Button Button(string name, Transform parent, string label, string confirmLabel, float x, float y, float w, float h, string key, string message, Action action, bool enabled = true)`.
  - `NumberField.Build(Transform parent, string name, float x, float y, float w, float h, string text, Action<string> onChanged, Action<int> onSubmit)` → `InputField`.
  - `EditorTab`: `protected const float SubTop = 44`; `protected void Push(Func<string> title, Action draw)`; `protected void Pop()`; `protected void CloseViews()`; `public void Leave()`; `public void TargetChanged()`. 하위 화면의 돌아가기 버튼 이름은 `Back`.

- [ ] **Step 1: 작업 브랜치를 만든다**

```powershell
git -C E:\TlToybox switch -c feat/ui-rework-2 develop
```

- [ ] **Step 2: `ui-check.ps1`에 새 부분 다섯 개를 넣는다**

`'order' {` 줄 앞에 넣는다.

```powershell
  'items' {
    # 스펙 6절 7, 8, 17번. 자원 탭의 목록·검색·상세, 가방 칸 한 번에 지정, 의복의 구분 표시.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    if ($o -notmatch '전체 (\d+)개 중 1~(\d+)번째') { throw "아이템 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]; $screen = [int]$Matches[2]
    Check 'the list holds every item' ($total -gt 3000) $o
    Check 'only the visible rows are built' ((Cmd "uipos Row$($screen + 5)") -match 'no element named')
    Check 'paging buttons are gone' ((Cmd 'uipos ItemPageSize10') -match 'no element named' -and (Cmd 'uipos ItemSearch') -match 'no element named')

    # 즉시 검색: 실제 키로 번호를 친다(이름과 번호 둘 다 찾는다)
    Cmd 'uifocus ItemQuery' | Out-Null
    Key 0x31 350; Key 0x30 350; Key 0x31 350
    $typed = Cmd 'uitext ItemQuery'
    $o = Cmd 'uitext ItemTableRange'
    Check 'the keys reached the search box' ($typed -match "text='101'") $typed
    Check 'typing filters at once' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -lt $total -and [int]$Matches[1] -gt 0) $o
    Check 'the search box is still in place' (IsAt 'ItemQuery')

    # Review Focus 1: 결과가 없을 때
    Cmd 'ui set ItemQuery zzzzzz' | Out-Null
    Check 'no match shows an empty list' ((Cmd 'uitext ItemTableRange') -match "text='0개'" -and (Cmd 'uipos ItemTableNext') -match 'no element named' -and (Cmd 'uipos Row0') -match 'no element named')
    Press 'ItemQueryClear' | Out-Null
    Check 'clearing brings everything back' ((Cmd 'uitext ItemTableRange') -match "전체 $total개 중 1~" -and (Cmd 'uitext ItemQuery') -match "text=''")

    # 이동 줄과 스크롤
    Press 'ItemTableNext' | Out-Null
    Check 'next moves one screen' ((Cmd 'uitext ItemTableRange') -match "중 $($screen + 1)~")
    Press 'ItemTableLast' | Out-Null
    Check 'last reaches the end' ((Cmd 'uitext ItemTableRange') -match "~$($total)번째")
    $p = Pos 'ItemTable'
    Rc "scroll $($p.x) $($p.y) 3" 300 | Out-Null
    Check 'the wheel scrolls back up' ((Cmd 'uitext ItemTableRange') -notmatch "~$($total)번째")
    Press 'ItemTableLast' | Out-Null

    # Review Focus 2: 끝까지 내린 채 검색해 목록이 짧아진다
    Cmd 'ui set ItemQuery 101' | Out-Null
    Check 'a shorter result starts from the top' ((Cmd 'uitext ItemTableRange') -match '중 1~' -and (Cmd 'uipos Row0') -match ' at ')
    Press 'ItemQueryClear' | Out-Null

    # 지급 뒤에도 목록 위치가 그대로이고 보유 수가 바뀐다
    Press 'ItemTableNext' | Out-Null
    $before = Cmd 'uitext ItemTableRange'
    $give = Pos 'ItemGive*'
    $id = $give.name -replace 'ItemGive', ''
    $had = if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
    Press $give.name | Out-Null
    $has = if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
    Check 'giving an item works from the list' ($has -eq $had + 1) "id=$id $had -> $has"
    Check 'the list stays where it was' ((Cmd 'uitext ItemTableRange') -eq $before)
    Press "ItemName$id" | Out-Null
    Check 'pressing a name shows the whole text below' ((Cmd 'uitext ItemDetail') -match "\[$id\]")

    # 의복: 이름만으로 구분된다
    Cmd 'ui set ItemQuery 3041003' | Out-Null
    $o = Cmd 'uitext ItemName3041003'
    Check 'clothes show sex, shape and colour' ($o -match "text='의복\((남|여)\) \d+-\d+'") $o
    Press 'ItemQueryClear' | Out-Null

    # 가방 칸 한 번에 지정 (회차의 -End가 bag-slots.txt를 되돌린다)
    function Extra { if ((Cmd 'bagstat') -match 'extra=(\d+)') { [int]$Matches[1] } else { -1 } }
    $e0 = Extra
    $want = if ($e0 -eq 120) { 135 } else { 125 }
    Cmd 'uifocus BagExtra' | Out-Null; Cmd "ui set BagExtra $want" | Out-Null; Key 0x0D
    Check 'Enter sets the extra slots, rounded down to tens' ((Extra) -eq $want - 5) "extra=$(Extra)"
    Cmd 'ui set BagExtra 501' | Out-Null; Press 'BagApply' | Out-Null
    Check 'the apply button works and the value is capped' ((Extra) -eq 500)
    Cmd 'ui set BagExtra 0' | Out-Null; Press 'BagApply' | Out-Null
    Check 'zero removes them' ((Extra) -eq 0)
    Press 'BagPlus' | Out-Null
    Check 'the +10 button still works' ((Extra) -eq 10)
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'numbers' {
    # 스펙 6절 9번, Review Focus 3. 능력치의 저장값 칸과 수량 줄의 직접 입력.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null
    $cell = (Pos 'Attr*').name
    function Val { if ((Cmd "uitext $cell") -match "text='(-?\d+)'") { [int]$Matches[1] } else { throw "값을 읽지 못했습니다: $cell" } }
    $v = Val
    Check 'the direct-input row is gone' ((Cmd 'uipos AttributeApply') -match 'no element named')

    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $($v + 1)" | Out-Null
    Press 'attr10' | Out-Null
    Check 'without Enter nothing is applied' ((Val) -eq $v)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $($v + 1)" | Out-Null; Key 0x0D
    Check 'Enter applies the typed value' ((Val) -eq $v + 1) "value=$(Val)"
    Check 'the change is reported' ((Cmd 'uistate') -match "message='[^']*->")
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell 999999999" | Out-Null; Key 0x0D
    Check 'an out-of-range value is refused' ((Val) -eq $v + 1)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell" | Out-Null; Key 0x0D
    Check 'an empty value is refused with a reason' ((Val) -eq $v + 1 -and (Cmd 'uistate') -match "message='[^']*숫자")
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell -5" | Out-Null; Key 0x0D
    Check 'a negative value is refused' ((Val) -eq $v + 1)
    Cmd "uifocus $cell" | Out-Null; Cmd "ui set $cell $v" | Out-Null; Key 0x0D
    Check 'back to the original value' ((Val) -eq $v)

    # 수량 줄의 직접 입력도 Enter로 적용된다
    Cmd 'uifocus attrInput' | Out-Null; Cmd 'ui set attrInput 7' | Out-Null; Key 0x0D
    Check 'Enter applies a typed step' ((Cmd 'uitext Plus*') -match "text='\+ 7'")
    Cmd 'uifocus attrInput' | Out-Null; Cmd 'ui set attrInput 0' | Out-Null; Key 0x0D
    Check 'a step of zero is refused' ((Cmd 'uitext Plus*') -match "text='\+ 7'" -and (Cmd 'uistate') -match "message='수량은")
    Press 'attr10' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'people' {
    # NPC 탭의 목록과 즉시 검색.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab6' | Out-Null
    $o = Cmd 'uitext NpcListRange'
    if ($o -notmatch '전체 (\d+)개') { throw "NPC 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]
    Check 'the NPC list is a scrolling list' ($total -gt 8 -and (Cmd 'uipos NpcNext10') -match 'no element named' -and (Cmd 'uipos NpcSearch') -match 'no element named')
    $pick = Pos 'Pick*'
    Press $pick.name | Out-Null
    Check 'picking an NPC shows the favour buttons' (IsAt 'IntimPlus')
    $name = if ((Cmd 'uitext SelectedName') -match "text='선택: (\S+)") { $Matches[1] } else { '' }
    Cmd "ui set NpcQuery $name" | Out-Null
    $o = Cmd 'uitext NpcListRange'
    Check 'typing a name narrows the list' ($name.Length -gt 0 -and $o -match '전체 (\d+)개' -and [int]$Matches[1] -ge 1 -and [int]$Matches[1] -lt $total) $o
    Cmd 'ui set NpcQuery zzzzzz' | Out-Null
    Check 'no match shows an empty list' ((Cmd 'uitext NpcListRange') -match "text='0개'")
    Press 'NpcQueryClear' | Out-Null
    Check 'clearing brings everyone back' ((Cmd 'uitext NpcListRange') -match "전체 $total개")
    Press 'NpcListLast' | Out-Null
    Check 'the list scrolls to the end' ((Cmd 'uitext NpcListRange') -match "~$($total)번째")
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'history' {
    # 스펙 6절 18번. 안전 탭의 변경 이력 전체 보기. 방금 한 변경이 맨 위에 오는지 보려고 먼저 하나를 만든다.
    $traps = Traps
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null; Press 'Restore' | Out-Null
    Press 'Tab8' | Out-Null
    $o = Cmd 'uitext HistoryRange'
    if ($o -notmatch '전체 (\d+)개') { throw "이력 목록의 범위 글을 읽지 못했습니다: $o" }
    $total = [int]$Matches[1]
    $lines = @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ }).Count
    Check 'past runs are listed too' ($total -eq [Math]::Min(2000, $lines)) "list=$total file=$lines"
    Check 'the newest change is on top' ((Cmd 'uitext Row0') -match '자원 회복')
    Cmd 'ui set HistoryQuery 가방' | Out-Null
    $o = Cmd 'uitext HistoryRange'
    Check 'the search narrows it' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -lt $total) $o
    Press 'Row0' | Out-Null
    Check 'pressing a line shows it whole' ((Cmd 'uitext HistoryDetail') -match '가방')
    Press 'HistoryQueryClear' | Out-Null
    Check 'the backup button is still there' (IsAt 'BackupNow')
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'confirm' {
    # 스펙 6절 11~14번, Review Focus 4·5. 하위 화면과 확인 대기.
    $traps = Traps
    Cmd 'editor' | Out-Null
    Press 'Tab6' | Out-Null; Press (Pos 'Pick*').name | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab4' | Out-Null
    $rm = (Pos 'LuckRemove*').name
    Press $rm | Out-Null
    Check 'the first press only asks' ((Cmd "uitext $rm") -match "text='정말 삭제'")
    Press 'Tab3' | Out-Null; Press 'Tab4' | Out-Null
    Check 'a tab change cancels it' ((Cmd "uitext $rm") -match "text='삭제'")
    Press $rm | Out-Null
    Press 'LuckAddOpen' | Out-Null
    Check 'a sub screen opens with one back button' ((IsAt 'Back') -and (Cmd 'uipos LuckAddOpen') -match 'no element named')
    Press 'Back' | Out-Null
    Check 'opening a sub screen cancels it' ((Cmd "uitext $rm") -match "text='삭제'")
    Press $rm | Out-Null
    Press 'ToPlayer' | Out-Null
    Press 'Tab6' | Out-Null; Press 'EditNpc' | Out-Null; Press 'Tab4' | Out-Null
    Check 'a target change cancels it' ((Cmd "uitext $rm") -match "text='삭제'")

    # Review Focus 5: 하위 화면이 열린 채 대상을 바꾸거나 탭을 떠난다
    Press 'FateOpen' | Out-Null
    Check 'the fate screen opens' (IsAt 'Back')
    Press 'ToPlayer' | Out-Null
    Check 'a target change closes the sub screen' ((IsAt 'LuckAddOpen') -and (Cmd 'uistate') -match 'target=player ')
    Press 'LuckAddOpen' | Out-Null
    Cmd 'ui set LuckQuery zzzzzz' | Out-Null
    Check 'the picker search is immediate' ((Cmd 'uitext LuckPickRange') -match "text='0개'")
    Press 'Tab3' | Out-Null; Press 'Tab4' | Out-Null
    Check 'leaving the tab closes the sub screen' (IsAt 'LuckAddOpen')

    # 공법: 기술 추가 화면의 목록과 검색, 옵션 교체 화면
    Press 'Tab3' | Out-Null; Press 'SkillAddOpen' | Out-Null
    $o = Cmd 'uitext SkillListRange'
    Check 'the skill list is a scrolling list' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -gt 12) $o
    Cmd 'ui set SkillQuery zzzzzz' | Out-Null
    Check 'the skill search is immediate' ((Cmd 'uitext SkillListRange') -match "text='0개'")
    Press 'SkillQueryClear' | Out-Null
    Press 'Back' | Out-Null
    Press (Pos 'PickMartial*').name | Out-Null
    Check 'picking a skill shows its options' (IsAt 'MartialEquip')
    Press 'MartialForget' | Out-Null
    Check 'forgetting asks first' ((Cmd 'uitext MartialForget') -match "text='정말 망각'")
    Press 'MartialExp' | Out-Null
    Check 'another action cancels it' ((Cmd 'uitext MartialForget') -match "text='기술 망각'")

    # 기예: 목록과 검색
    Press 'Tab5' | Out-Null
    $o = Cmd 'uitext ArtTableRange'
    Check 'the art list is a scrolling list' ($o -match '전체 (\d+)개') $o
    Cmd 'ui set ArtQuery zzzzzz' | Out-Null
    Check 'the art search is immediate' ((Cmd 'uitext ArtTableRange') -match "text='0개'")
    Press 'ArtQueryClear' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
```

- [ ] **Step 3: 빨간 불 — 지금 빌드에서 새 부분이 실패하는 것을 본다**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-red
foreach ($part in 'items','numbers','people','history','confirm') { pwsh -NoProfile -File tools\research\ui-check.ps1 -Part $part 2>&1 | Select-Object -Last 3 }
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-red
```

Expected: 다섯 부분 모두 실패한다(요소를 찾지 못해 멈추거나 `FAILS`가 1 이상). `items`는 `ItemTableRange`를, `numbers`는 `Attr*`를, `people`은 `NpcListRange`를, `history`는 `HistoryRange`를 찾지 못한다. `confirm`은 `Back`이 없어 실패한다. 통과하는 부분이 있으면 그 검사는 아무것도 확인하지 못하는 것이므로 고친다.

- [ ] **Step 4: `UI/Widgets/ListView.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 세로로 스크롤되는 목록. 전체 높이만큼의 영역에 화면에 걸친 줄만 만든다(아이템 3천여 개를 줄마다 만들면 너무 느리다).
    // 스크롤해서 첫 줄의 번호가 바뀌면 그때 줄을 다시 만든다. 목록 위치는 이 객체가 기억하므로 탭이 내용을 다시 그려도 유지된다.
    // 아래에 이동 줄이 붙는다: 처음·이전·다음·끝과 "전체 N개 중 a~b번째". 전체가 한 화면에 들어오면 버튼을 숨긴다.
    internal sealed class ListView<T>
    {
        private const float NavH = 38;
        private const float Gap = 6;
        private static readonly Color Stripe = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        private readonly string name;
        private readonly float rowHeight;
        private IList<T> items = new List<T>();
        private Action<RectTransform, T, int> drawRow;
        private RectTransform inner;
        private RectTransform nav;
        private float viewH;
        private float innerW;
        private float scrollY;
        private int first = -1;

        // 줄을 눌렀을 때 부를 것. 있으면 줄 전체가 눌린다(줄 위의 버튼은 따로 눌린다).
        public Action<T> OnSelect;

        public string EmptyText = "조건에 맞는 것이 없습니다.";

        public ListView(string name, float rowHeight)
        {
            this.name = name;
            this.rowHeight = rowHeight;
        }

        public int Count
        {
            get { return items.Count; }
        }

        // 다음에 그릴 때 맨 위에서 시작한다(분류를 바꿨을 때).
        public void Reset()
        {
            scrollY = 0;
        }

        private float Clamp(float y)
        {
            return Mathf.Clamp(y, 0f, Mathf.Max(0f, items.Count * rowHeight - viewH));
        }

        // (x, y, w, h) 안에 목록과 이동 줄을 그린다. h에는 이동 줄의 높이가 들어 있다.
        public void Build(Transform parent, float x, float y, float w, float h, IList<T> list, Action<RectTransform, T, int> draw)
        {
            items = list;
            drawRow = draw;
            viewH = h - NavH - Gap;
            innerW = w - Ui.ScrollBarWidth;
            scrollY = Clamp(scrollY);
            inner = Ui.Scroll(name, parent, x, y, w, viewH, items.Count * rowHeight, scrollY, new Action<float>(OnScroll));
            nav = Ui.Rect(name + "Nav", parent, x, y + viewH + Gap, w, NavH);
            DrawRows();
        }

        // 걸러진 결과로 바꾸고 맨 위에서 시작한다. 줄만 다시 그리므로 검색 칸이 그대로 남는다.
        public void SetItems(IList<T> list)
        {
            items = list;
            scrollY = 0;
            if (inner == null)
            {
                return;
            }
            inner.sizeDelta = new Vector2(innerW, Mathf.Max(viewH, items.Count * rowHeight));
            inner.anchoredPosition = Vector2.zero;
            DrawRows();
        }

        // 보이는 줄을 다시 그린다(줄의 내용이 바뀌었을 때).
        public void Refresh()
        {
            if (inner != null)
            {
                DrawRows();
            }
        }

        private void OnScroll(float y)
        {
            scrollY = y;
            if ((int)(y / rowHeight) != first)
            {
                DrawRows();
            }
        }

        private void ScrollTo(float y)
        {
            scrollY = Clamp(y);
            inner.anchoredPosition = new Vector2(0f, scrollY);
            DrawRows();
        }

        private void DrawRows()
        {
            Ui.Clear(inner);
            Ui.Clear(nav);
            first = Mathf.Max(0, (int)(scrollY / rowHeight));
            int perScreen = Mathf.Max(1, (int)(viewH / rowHeight));
            int last = Mathf.Min(items.Count, first + perScreen + 1);
            if (items.Count == 0)
            {
                Ui.Label(inner, EmptyText, 16, 8, innerW - 32, 34, 18).color = Ui.Warn;
            }
            for (int i = first; i < last; i++)
            {
                RectTransform row = Ui.Rect("Row" + i, inner, 0, i * rowHeight, innerW, rowHeight);
                if (OnSelect != null || i % 2 == 1)
                {
                    var image = row.gameObject.AddComponent<Image>();
                    image.color = i % 2 == 1 ? Stripe : Clear;
                    if (OnSelect != null)
                    {
                        T item = items[i];
                        var button = row.gameObject.AddComponent<Button>();
                        button.targetGraphic = image;
                        button.transition = Selectable.Transition.None;
                        button.onClick.AddListener(new Action(() => OnSelect(item)));
                    }
                    else
                    {
                        image.raycastTarget = false;
                    }
                }
                drawRow(row, items[i], i);
            }

            bool fits = items.Count * rowHeight <= viewH;
            float labelX = 0;
            if (!fits)
            {
                float page = perScreen * rowHeight;
                float max = Clamp(float.MaxValue);
                Ui.Button(name + "First", nav, "처음", 0, 0, 80, NavH, new Action(() => ScrollTo(0f)), scrollY > 0f);
                Ui.Button(name + "Prev", nav, "이전", 86, 0, 80, NavH, new Action(() => ScrollTo(scrollY - page)), scrollY > 0f);
                Ui.Button(name + "Next", nav, "다음", 172, 0, 80, NavH, new Action(() => ScrollTo(scrollY + page)), scrollY < max);
                Ui.Button(name + "Last", nav, "끝", 258, 0, 80, NavH, new Action(() => ScrollTo(max)), scrollY < max);
                labelX = 350;
            }
            string range = items.Count == 0 ? "0개"
                : "전체 " + items.Count + "개 중 " + (first + 1) + "~" + Mathf.Min(items.Count, first + perScreen) + "번째";
            RectTransform holder = Ui.Rect(name + "Range", nav, labelX, 0, 600, NavH);
            Ui.Label(holder, range, 0, 0, 600, NavH, 16).color = Ui.Dim;
        }
    }
}
```

- [ ] **Step 5: `UI/Widgets/SearchBox.cs`를 만든다**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 즉시 검색 입력 칸. 글이 바뀔 때마다 앞뒤 공백을 뗀 검색어를 넘긴다.
    // 넘겨받은 쪽은 탭 전체가 아니라 목록만 다시 그려야 한다(ListView.SetItems). 탭을 다시 그리면 이 칸이 지워져 포커스를 잃는다.
    internal static class SearchBox
    {
        private const float ClearW = 40;

        // 입력 칸의 이름은 name, 지움 버튼의 이름은 name + "Clear".
        public static InputField Build(Transform parent, string name, float x, float y, float w, string value, string placeholder, Action<string> onChanged)
        {
            InputField input = Ui.Input(name, parent, value, placeholder, x, y, w - ClearW - 4, 38, new Action<string>(v => onChanged(v.Trim())));
            // 글을 비우면 위의 콜백이 빈 검색어로 불린다.
            Ui.Button(name + "Clear", parent, "×", x + w - ClearW, y, ClearW, 38, new Action(() => input.text = ""));
            return input;
        }
    }
}
```

- [ ] **Step 6: `UI/Widgets/DetailPane.cs`를 만든다**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 목록 아래의 상자. 줄에서는 잘라 보이는 긴 이름과 설명을, 고른 줄에 한해 전부 보인다.
    // 글은 이 객체가 기억하므로 탭이 내용을 다시 그려도 남는다.
    internal sealed class DetailPane
    {
        private readonly string name;
        private readonly string hint;
        private string text = "";
        private Text label;

        public DetailPane(string name, string hint)
        {
            this.name = name;
            this.hint = hint;
        }

        public void Build(Transform parent, float x, float y, float w, float h)
        {
            RectTransform box = Ui.Panel(name, parent, x, y, w, h, Ui.Section);
            box.GetComponent<Image>().raycastTarget = false;
            label = Ui.Label(box, "", 12, 4, w - 24, h - 8, 16, TextAnchor.UpperLeft);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            Apply();
        }

        public void Show(string value)
        {
            text = value ?? "";
            Apply();
        }

        public void Clear()
        {
            Show("");
        }

        private void Apply()
        {
            if (label == null)
            {
                return;
            }
            label.text = text.Length > 0 ? text : hint;
            label.color = text.Length > 0 ? Color.white : Ui.Dim;
        }
    }
}
```

- [ ] **Step 7: `UI/Widgets/Confirm.cs`를 만든다**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 두 번 눌러 확인. 되돌리기 어려운 동작은 첫 누름에 이유를 보이고, 한 번 더 누르면 실행한다.
    // 확인 대기는 창 전체에 하나만 있고, 다른 동작으로 창이 다시 그려지면 풀린다(탭 전환, 대상 변경, 하위 화면 이동 포함).
    internal static class Confirm
    {
        private static string pending;
        private static bool keep;   // 확인을 물은 바로 그 다시 그리기에서는 풀지 않는다

        public static bool IsPending(string key)
        {
            return pending == key;
        }

        // 확인을 묻는다: 대기 상태로 두고 이유를 결과 영역에 보인다.
        public static void Ask(string key, string message)
        {
            pending = key;
            keep = true;
            EditorWindow.Say(message);
        }

        public static void Clear()
        {
            pending = null;
            keep = false;
        }

        // 창이 다시 그려질 때마다 틀이 부른다.
        public static void OnRender()
        {
            if (keep)
            {
                keep = false;
                return;
            }
            pending = null;
        }

        // 확인이 필요한 버튼. 대기 중이면 confirmLabel을 보이고 강조한다.
        public static Button Button(string name, Transform parent, string label, string confirmLabel, float x, float y, float w, float h,
            string key, string message, Action action, bool enabled = true)
        {
            bool waiting = IsPending(key);
            return Ui.Button(name, parent, waiting ? confirmLabel : label, x, y, w, h, new Action(() =>
            {
                if (!IsPending(key))
                {
                    Ask(key, message);
                    return;
                }
                Clear();
                action();
            }), enabled, waiting);
        }
    }
}
```

- [ ] **Step 8: `UI/Widgets/NumberField.cs`를 만든다**

```csharp
using System;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 숫자 입력 칸. 숫자를 쓰고 Enter를 누르면 onSubmit으로 넘긴다.
    // 입력 칸의 끝맺음 이벤트는 다른 곳을 눌러 포커스를 잃을 때도 온다. 그때는 적용하지 않도록 Enter 키인지 확인한다(실측: 스펙 3.4절).
    internal static class NumberField
    {
        public static InputField Build(Transform parent, string name, float x, float y, float w, float h, string text,
            Action<string> onChanged, Action<int> onSubmit)
        {
            InputField input = Ui.Input(name, parent, text, "", x, y, w, h, onChanged, true);
            input.onEndEdit.AddListener(new Action<string>(v =>
            {
                if (!UnityEngine.Input.GetKeyDown(KeyCode.Return) && !UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    return;
                }
                int value;
                if (!int.TryParse(v, out value))
                {
                    EditorWindow.Say("숫자를 입력하세요.");
                    return;
                }
                onSubmit(value);
            }));
            return input;
        }
    }
}
```

- [ ] **Step 9: `UI/Widgets/AmountBar.cs`의 직접 입력을 Enter로도 적용되게 한다**

`Build` 메서드에서 `Ui.Input(key + "Input", …);` 줄부터 메서드의 `return` 앞까지를 다음으로 바꾼다.

```csharp
            Action<int> apply = value =>
            {
                if (value < 1 || value > Max)
                {
                    EditorWindow.Say("수량은 1~" + Max + " 사이의 숫자로 입력하세요.");
                    return;
                }
                values[key] = value;
                EditorWindow.ClearMessage();
                EditorWindow.Refresh();
            };
            NumberField.Build(parent, key + "Input", inputX, y, 130, 38, text, new Action<string>(v => typed[key] = v), apply);
            Ui.Button(key + "Apply", parent, "적용", inputX + 136, y, 70, 38, new Action(() =>
            {
                string entered;
                int value;
                typed.TryGetValue(key, out entered);
                if (!int.TryParse(entered, out value))
                {
                    value = 0;
                }
                apply(value);
            }));
```

- [ ] **Step 10: `UI/Tabs/EditorTab.cs`에 하위 화면을 넣는다**

파일 머리의 `using UnityEngine;`을 다음으로 바꾼다.

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
```

`public void Render(RectTransform area)` 메서드 전체를 다음으로 바꾼다.

```csharp
        // 하위 화면에서 내용이 시작하는 y. 그 위는 제목과 돌아가기다.
        protected const float SubTop = 44;

        private sealed class SubView
        {
            public Func<string> Title;
            public Action Draw;
        }

        private readonly List<SubView> views = new List<SubView>();

        public void Render(RectTransform area)
        {
            content = area;
            if (views.Count == 0)
            {
                Draw();
                return;
            }
            SubView view = views[views.Count - 1];
            Ui.Label(content, view.Title(), 0, 0, ContentW - 150, 36, 21);
            Ui.Button("Back", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(Pop));
            view.Draw();
        }

        // 하위 화면을 쌓는다(목록 → 고르기 같은 흐름). draw는 SubTop 아래에 그린다.
        protected void Push(Func<string> title, Action draw)
        {
            views.Add(new SubView { Title = title, Draw = draw });
            Redraw();
        }

        // 한 단계 돌아간다.
        protected void Pop()
        {
            if (views.Count > 0)
            {
                views.RemoveAt(views.Count - 1);
            }
            Redraw();
        }

        protected void CloseViews()
        {
            views.Clear();
        }

        // 틀이 부른다: 이 탭을 떠날 때. 하위 화면을 닫는다.
        public void Leave()
        {
            CloseViews();
            OnHide();
        }

        // 틀이 부른다: 편집 대상이 바뀌었을 때. 하위 화면은 앞의 대상을 보고 있으므로 닫는다.
        public void TargetChanged()
        {
            CloseViews();
            OnTargetChanged();
        }
```

- [ ] **Step 11: `UI/EditorWindow.cs`를 틀에 연결한다**

세 곳을 바꾼다.

- `SetTarget` 안의 `tabs[i].OnTargetChanged();` → `tabs[i].TargetChanged();`
- `SwitchTab` 안의 `tabs[tab].OnHide();` → `tabs[tab].Leave();`
- `Render`의 첫 줄(`Ui.Clear(titleArea);`) 앞에 `Confirm.OnRender();`를 넣는다.

- [ ] **Step 12: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 13: 회차 — 기존 검사가 그대로 통과하는지**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-t1
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part tabs
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part base
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-t1
```

Expected: 둘 다 `FAILS=0`. (수량 줄의 Enter는 Task 3의 `numbers`가 본다.)

- [ ] **Step 14: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI tools/research/ui-check.ps1
git -C E:\TlToybox commit -m @'
feat: 편집기의 공통 부품 — 목록, 검색, 상세, 확인, 숫자 칸, 하위 화면

보이는 줄만 만드는 스크롤 목록, 즉시 검색 칸, 고른 줄의 전체 글을 보이는 칸,
창 전체에 하나뿐인 확인 대기, Enter로 적용하는 숫자 칸을 만들고 탭이 하위
화면을 쌓을 수 있게 했다. 아직 쓰는 곳은 수량 줄 하나다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 2: 자원 탭 — 목록, 즉시 검색, 가방 칸 한 번에 지정, 의복 표시

**Files:**
- Rewrite: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs`
- Modify: `src/ModCode/ModMain/Services/ItemCatalog.cs` (`Build` 안의 `all.Add(new Entry { … })` 앞)
- Modify: `tools/research/ui-check.ps1` (`$tabProbe`는 그대로)

**Interfaces:**
- Consumes: Task 1의 `ListView<T>`, `SearchBox`, `DetailPane`, `NumberField`; `BagService.Extra`, `Step`, `MaxExtra`, `Change(int)`, `Describe()`; `ItemCatalog.Find(string, int, int)`, `MainCategories`, `SubCategories(int)`; 게임의 `g.conf.clothItem.GetItem(int)`(`ConfClothItemItem`: `sex`, `style`, `color`).
- Produces: 요소 이름 `ItemQuery`, `ItemQueryClear`, `ItemSearchClear`(분류와 검색 초기화), `ItemTable`(목록), `ItemName<아이템 번호>`, `ItemGive<아이템 번호>`, `ItemDetail`, `BagExtra`, `BagApply`.

- [ ] **Step 1: 의복의 성별 값이 무엇을 뜻하는지 게임에서 확인한다**

`ClothItem` 표의 `sex`는 1과 2다. 플레이어 "신환"은 여성이다. 회차 안에서 플레이어의 성별 값을 읽는다(기존 개발 명령이 없으면 `Dev/DevCommandsUi.cs`의 `switch`에 `case "usex": return "sex=" + (int)g.world.playerUnit.data.unitData.propertyData.sex;`를 넣고 빌드한 뒤 읽는다. 이 줄은 확인 뒤 지운다).

Expected: `sex=2`이면 2가 여성, 1이 남성이다(아래 코드의 가정). `sex=1`이면 아래 코드의 `"남"`과 `"여"`를 맞바꾸고, 어느 쪽인지 05 문서 27절에 적는다. 다른 값이면 성별 대신 값 그대로(`성별 1`)를 보이도록 바꾸고 "미확인"으로 적는다.

- [ ] **Step 2: `Services/ItemCatalog.cs` — 의복의 이름과 설명**

`Build` 메서드에서 `all.Add(new Entry` 줄 앞에 넣고, 그 `Entry`의 `Name`, `Desc`, `Search`가 아래의 변수를 쓰게 한다.

```csharp
                string desc = MartialService.CleanDesc(GameTool.LS(item.desc)).Replace("\r", "").Replace('\n', ' ');
                // 의복은 게임의 이름과 설명이 모두 "의복"이다. 성별·모양·색으로 구분되게 한다(의복 표 ClothItem).
                ConfClothItemItem cloth = item.className == ClothClass ? g.conf.clothItem.GetItem(item.id) : null;
                if (cloth != null)
                {
                    string sex = cloth.sex == 1 ? "남" : "여";
                    desc = name + " — " + sex + "성용, 모양 " + cloth.style + ", 색 " + cloth.color;
                    name = name + "(" + sex + ") " + cloth.style + "-" + cloth.color;
                }
```

`Entry`의 세 줄:

```csharp
                    Name = name,
                    Desc = desc,
```

```csharp
                    Search = EditService.Compact(name),
```

(`Desc`에 있던 긴 식은 위의 `desc` 변수로 옮겼으므로 지운다. `Search`는 식이 그대로이고 `name`이 바뀐 값을 쓴다.)

클래스의 필드 자리(`private static List<Entry> all;` 앞)에:

```csharp
        private const int ClothClass = 304;   // ItemProps.className: 의복
```

- [ ] **Step 3: `UI/Tabs/ResourcesTab.cs`를 통째로 바꾼다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭: 영석, 가방 칸, 아이템 지급 표.
    internal sealed class ResourcesTab : EditorTab
    {
        private const float RowH = 32;
        private const float DetailH = 92;
        private const float ColId = 8, ColCat = 110, ColName = 290, ColLevel = 560, ColOwned = 630, ColDesc = 710, ColGive = 1166;

        private readonly ListView<ItemCatalog.Entry> list = new ListView<ItemCatalog.Entry>("ItemTable", RowH);
        private readonly DetailPane detail = new DetailPane("ItemDetail", "아이템명을 누르면 여기에 전체 설명이 나옵니다.");
        private string query = "";
        private float subScrollY;
        private int mainType = -1;
        private int subType = -1;
        private string bagInput = "";

        public override string Title
        {
            get { return "자원"; }
        }

        public ResourcesTab()
        {
            list.EmptyText = "조건에 맞는 아이템이 없습니다.";
        }

        private List<ItemCatalog.Entry> Found()
        {
            return ItemCatalog.Find(query, mainType, subType);
        }

        // 분류를 바꾼다: 목록을 맨 위로, 상세를 비우고 다시 그린다.
        private void SetCategory(int main, int sub)
        {
            mainType = main;
            subType = sub;
            list.Reset();
            detail.Clear();
            Redraw();
        }

        // 가방의 추가 칸을 원하는 수로 맞춘다. 10칸 단위로 내림하고 0~MaxExtra로 맞춘 뒤 지금 값과의 차이만큼 바꾼다.
        private void SetBagExtra(int wanted)
        {
            int target = Math.Max(0, Math.Min(BagService.MaxExtra, wanted / BagService.Step * BagService.Step));
            int delta = target - BagService.Extra;
            if (delta == 0)
            {
                Say("가방: 추가 칸은 이미 +" + target + "입니다.");
                return;
            }
            Say(BagService.Change(delta));
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            int moneyAmount = AmountBar.Get("money");
            int itemAmount = AmountBar.Get("item");

            // 영석, 아이템 지급 수량, 가방 칸
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, 140, Ui.Section);
            Ui.Label(content, "영석: " + EditService.Money(player), 16, 6, 250, 40, 22);
            Ui.Label(content, "추가량", 270, 6, 70, 40, 18).color = Ui.Dim;
            float moneyRight = AmountBar.Build(content, "money", 340, 7);
            Ui.Button("MoneyAdd", content, "영석 + " + moneyAmount, moneyRight + 20, 7, 200, 38, new Action(() => Say(EditService.AddMoney(Player, moneyAmount))));
            Ui.Label(content, "아이템 지급 수량", 16, 50, 250, 40, 20);
            AmountBar.Build(content, "item", 340, 51);
            // 가방 칸. 편집기가 늘린 만큼만 되돌릴 수 있다.
            int bagExtra = BagService.Extra;
            bagInput = bagExtra.ToString();
            Ui.Label(content, BagService.Describe(), 16, 94, 320, 40, 18);
            Ui.Label(content, "추가 칸", 340, 94, 76, 40, 18).color = Ui.Dim;
            NumberField.Build(content, "BagExtra", 416, 95, 100, 38, bagInput, new Action<string>(v => bagInput = v), new Action<int>(SetBagExtra));
            Ui.Button("BagApply", content, "적용", 522, 95, 70, 38, new Action(() =>
            {
                int value;
                if (!int.TryParse(bagInput, out value))
                {
                    Say("가방: 추가 칸을 숫자로 입력하세요.");
                    return;
                }
                SetBagExtra(value);
            }));
            Ui.Button("BagMinus", content, "-" + BagService.Step, 610, 95, 70, 38, new Action(() => Say(BagService.Change(-BagService.Step))), bagExtra > 0);
            Ui.Button("BagPlus", content, "+" + BagService.Step, 686, 95, 70, 38, new Action(() => Say(BagService.Change(BagService.Step))), bagExtra < BagService.MaxExtra);
            Ui.Label(content, "10칸 단위, 0~" + BagService.MaxExtra + ". 숫자를 쓰고 Enter를 눌러도 됩니다.", 770, 94, 520, 40, 15).color = Ui.Dim;

            // 이름 검색(치는 대로 걸러진다)과 초기화
            Ui.Label(content, "아이템명", 0, 150, 90, 38);
            SearchBox.Build(content, "ItemQuery", 90, 150, 330, query, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                query = v;
                detail.Clear();
                list.SetItems(Found());
            }));
            Ui.Button("ItemSearchClear", content, "분류·검색 초기화", 430, 150, 190, 38, new Action(() => { query = ""; SetCategory(-1, -1); }),
                query.Length > 0 || mainType >= 0);

            // 카테고리(게임의 분류표). 첫 줄은 대분류, 대분류를 고르면 아래에 세부 분류가 나온다.
            Ui.Label(content, "카테고리", 0, 194, 90, 38);
            float cx = 90;
            Ui.Button("ItemCatAll", content, "전체", cx, 194, 80, 38, new Action(() => SetCategory(-1, -1)), true, mainType < 0);
            cx += 86;
            foreach (ItemCatalog.Category category in ItemCatalog.MainCategories)
            {
                int type = category.Id;
                string label = category.Name + " (" + category.Count + ")";
                float width = 24 + label.Length * 15;
                Ui.Button("ItemCat" + type, content, label, cx, 194, width, 38, new Action(() => { subScrollY = 0; SetCategory(type, -1); }), true, mainType == type);
                cx += width + 4;
            }
            float top = 238;
            if (mainType >= 0)
            {
                // 세부 분류가 많은 대분류(도구는 50종 이상)는 두 줄만 보이게 하고 그 안에서 스크롤한다.
                const float subRow = 38;
                float subW = ContentW - 90;
                List<ItemCatalog.Category> subs = ItemCatalog.SubCategories(mainType);
                var places = new List<Vector2>();
                float sx = 76, sy = 0;
                foreach (ItemCatalog.Category category in subs)
                {
                    float width = SubWidth(category);
                    if (sx + width > subW - Ui.ScrollBarWidth)
                    {
                        sx = 0;
                        sy += subRow;
                    }
                    places.Add(new Vector2(sx, sy));
                    sx += width + 6;
                }
                float subsH = sy + subRow;
                float shownH = Math.Min(subsH, subRow * 2);
                Ui.Label(content, "세부", 0, top, 90, 34, 18).color = Ui.Dim;
                Transform subRoot = subsH > shownH
                    ? Ui.Scroll("ItemSubs", content, 90, top, subW, shownH, subsH, subScrollY, new Action<float>(v => subScrollY = v))
                    : Ui.Rect("ItemSubs", content, 90, top, subW, shownH);
                int main = mainType;
                Ui.Button("ItemSubAll", subRoot, "전체", 0, 0, 70, 34, new Action(() => SetCategory(main, -1)), true, subType < 0);
                for (int i = 0; i < subs.Count; i++)
                {
                    int sub = subs[i].Id;
                    Ui.Button("ItemSub" + sub, subRoot, subs[i].Name + " (" + subs[i].Count + ")", places[i].x, places[i].y, SubWidth(subs[i]), 34,
                        new Action(() => SetCategory(main, sub)), true, subType == sub);
                }
                top += shownH + 4;
            }

            // 표 머리
            Ui.Panel("ItemHead", content, 0, top, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", ColId, top, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "카테고리", ColCat, top, 170, 30, 16).color = Ui.Dim;
            Ui.Label(content, "아이템명", ColName, top, 260, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", ColLevel, top, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "보유", ColOwned, top, 70, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", ColDesc, top, 440, 30, 16).color = Ui.Dim;
            Ui.Label(content, "지급", ColGive, top, 116, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            top += 32;

            // 표 본문과 이동 줄, 그 아래에 고른 아이템의 전체 설명
            list.Build(content, 0, top, ContentW, ContentH - DetailH - 8 - top, Found(), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawRow(RectTransform row, ItemCatalog.Entry entry, int index)
        {
            Ui.Label(row, entry.Id.ToString(), ColId, 0, 100, RowH, 16);
            Ui.Label(row, Shorten(entry.SubTypeName, 8), ColCat, 0, 175, RowH, 16);
            Ui.Button("ItemName" + entry.Id, row, Shorten(entry.Name, 14), ColName, 2, 262, RowH - 4, new Action(() => detail.Show(Detail(entry))));
            Ui.Label(row, entry.Level.ToString(), ColLevel, 0, 60, RowH, 16);
            Ui.Label(row, Player.data.unitData.propData.GetPropsNum(entry.Id).ToString(), ColOwned, 0, 70, RowH, 16);
            Ui.Label(row, Shorten(entry.Desc, 27), ColDesc, 0, 440, RowH, 15).color = Ui.Dim;
            int amount = AmountBar.Get("item");
            Ui.Button("ItemGive" + entry.Id, row, "지급 x" + amount, ColGive, 2, 116, RowH - 4, new Action(() => Say(EditService.GiveItem(Player, entry.Id, amount))));
        }

        private static float SubWidth(ItemCatalog.Category category)
        {
            return 46 + (category.Name.Length + category.Count.ToString().Length) * 16;
        }

        private static string Detail(ItemCatalog.Entry entry)
        {
            return "[" + entry.Id + "] " + entry.Name + "  (" + entry.TypeName + " / " + entry.SubTypeName + ", 품질 " + entry.Level + ")\n" + entry.Desc;
        }
    }
}
```

- [ ] **Step 4: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개". `g.conf.clothItem.GetItem`이 없다는 오류가 나면 `tools\research\q.ps1 -Show ConfClothItemBase`로 번호로 찾는 메서드의 이름을 확인해 그 이름으로 바꾼다.

- [ ] **Step 5: 회차 — `items`, 가방 검사**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-t2
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part items
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part base
& tools\DevRemote\mod.ps1 -Cmd 'editor'
& tools\DevRemote\rc.ps1 -Cmd 'click 151 198' -NoShot
& tools\DevRemote\win.ps1 -Do shot -Out .devremote\check-items.png -Scale 0.6
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-t2
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-t2b
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part bag
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-t2b
```

Expected: 세 검사 모두 `FAILS=0`. 캡처에서 자원 탭의 배치(가방 줄, 검색 칸의 `×`, 표, 이동 줄, 상세 칸)가 겹치지 않는지 눈으로 본다. 지움 버튼의 `×`가 네모나 빈칸으로 나오면 `SearchBox.cs`의 `"×"`를 `"X"`로 바꾼다.

- [ ] **Step 6: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs src/ModCode/ModMain/Services/ItemCatalog.cs src/ModCode/ModMain/UI/Widgets/SearchBox.cs
git -C E:\TlToybox commit -m @'
feat: 자원 탭 — 스크롤 목록과 즉시 검색, 가방 칸 한 번에 지정, 의복 구분

아이템 표를 끝까지 스크롤되는 목록으로 바꾸고 검색어를 치는 대로 걸러지게 했다.
가방의 추가 칸을 숫자로 적어 한 번에 맞출 수 있다. 이름이 모두 "의복"인 의복
295개는 성별·모양·색을 이름과 설명에 보인다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 3: 능력치, NPC, 안전 탭

**Files:**
- Rewrite: `src/ModCode/ModMain/UI/Tabs/AttributesTab.cs`, `NpcTab.cs`, `SafetyTab.cs`
- Modify: `src/ModCode/ModMain/Safety/ChangeLog.cs`
- Modify: `tools/research/ui-check.ps1` (`tabs` 부분의 `Pick0` → 이름 찾기)

**Interfaces:**
- Consumes: Task 1의 부품; `EditService.SetAttribute(WorldUnitBase, AttributeDef, int)`, `RestoreResources`, `IntimTo`, `SetIntimBoth`, `NameOf`, `Compact`, `GradeName`; `AttributeCatalog.All`, `Categories`; `SaveBackup.LastBackupDir`, `BackupNow()`; `ChangeLog.Entries`, `DataDir`.
- Produces: `ChangeLog.ReadHistory(int maxLines, out string error)` → `List<string>`(최신이 앞). 요소 이름 `Attr<능력치 번호>`(저장값 칸), `NpcQuery`, `NpcList`, `Pick<단위 ID>`, `SelectedName`, `HistoryQuery`, `History`, `HistoryDetail`.

- [ ] **Step 1: `Safety/ChangeLog.cs`에 지난 기록 읽기를 넣는다**

`Add` 메서드 앞에 넣는다.

```csharp
        // changes.log의 마지막 maxLines줄을 최신 순으로 돌려준다. 이번 실행의 기록도 이 파일에 들어 있다.
        // 읽지 못하면 빈 목록과 이유를 돌려준다.
        public static List<string> ReadHistory(int maxLines, out string error)
        {
            error = null;
            var result = new List<string>();
            try
            {
                string path = Path.Combine(DataDir, "changes.log");
                if (!File.Exists(path))
                {
                    return result;
                }
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = lines.Length - 1; i >= 0 && result.Count < maxLines; i--)
                {
                    if (lines[i].Length > 0)
                    {
                        result.Add(lines[i]);
                    }
                }
            }
            catch (IOException e)
            {
                error = e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                error = e.Message;
            }
            return result;
        }
```

파일 머리에 `using System;`이 이미 있다(`UnauthorizedAccessException`).

- [ ] **Step 2: `UI/Tabs/AttributesTab.cs`를 통째로 바꾼다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 능력치 탭: 게임 인물 화면의 다섯 분류로 저장값을 편집한다. 저장값 칸에 숫자를 쓰고 Enter를 누르면 그 값이 된다.
    internal sealed class AttributesTab : EditorTab
    {
        private int attributeCategory;

        public override string Title
        {
            get { return "능력치"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public override void OnTargetChanged()
        {
            attributeCategory = 0;
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            int step = AmountBar.Get("attr");
            var saved = target.data.unitData.propertyData;
            var dyn = target.data.dynUnitData;
            ConfRoleAttributeLimit limit = g.conf.roleAttributeLimit;
            List<AttributeDef> all = AttributeCatalog.All;

            // 게임의 인물 화면과 같은 분류
            for (int i = 0; i < AttributeCatalog.Categories.Length; i++)
            {
                int category = i;
                Ui.Button("AttrCat" + i, content, AttributeCatalog.Categories[i], i * 156, 0, 150, 40,
                    new Action(() => { attributeCategory = category; Redraw(); }), true, attributeCategory == i);
            }

            const float head = 52;
            Ui.Label(content, "항목", 0, head, 190, 30, 18).color = Ui.Dim;
            Ui.Label(content, "저장값", 206, head, 110, 30, 18).color = Ui.Dim;
            Ui.Label(content, "게임 화면 값(보정 포함)", 330, head, 220, 30, 18).color = Ui.Dim;
            Ui.Label(content, "허용 범위", 560, head, 220, 30, 18).color = Ui.Dim;

            int row = 0;
            for (int i = 0; i < all.Count; i++)
            {
                AttributeDef def = all[i];
                if (def.Category != attributeCategory)
                {
                    continue;
                }
                int index = i;
                float y = head + 34 + row * 40;
                row++;
                int value = def.Get(saved);
                int min = def.Min(limit);
                int max = def.Max(limit);
                Ui.Label(content, def.Label, 0, y, 200, 36);
                NumberField.Build(content, "Attr" + index, 206, y, 110, 34, value.ToString(), null,
                    new Action<int>(v => Say(EditService.SetAttribute(Target, def, v))));
                Ui.Label(content, def.DynValue(dyn).ToString(), 330, y, 220, 36);
                Ui.Label(content, min + " ~ " + max + (def.OwnLimit ? " (자체 상한)" : ""), 560, y, 260, 36, 18).color = Ui.Dim;
                Ui.Button("Minus" + index, content, "- " + step, 830, y, 100, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Get(Target.data.unitData.propertyData) - step))));
                Ui.Button("Plus" + index, content, "+ " + step, 936, y, 100, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Get(Target.data.unitData.propertyData) + step))));
                Ui.Button("Max" + index, content, "최대", 1042, y, 84, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Max(g.conf.roleAttributeLimit)))));
            }

            // 분류마다 줄 수가 달라도 아래쪽 줄의 위치는 고정한다(가장 긴 분류가 12줄).
            float footer = head + 34 + 12 * 40 + 12;
            Ui.Label(content, "증감 단위", 0, footer, 110, 38, 18).color = Ui.Dim;
            float right = AmountBar.Build(content, "attr", 110, footer);
            Ui.Button("Restore", content, "자원 회복", right + 30, footer, 140, 38, new Action(() => Say(EditService.RestoreResources(Target))));
            Ui.Label(content, "저장값 칸에 숫자를 쓰고 Enter를 누르면 그 값으로 바뀝니다.", 0, footer + 52, ContentW, 30, 18).color = Ui.Dim;
            Ui.Label(content, "저장값은 인물 고유의 값이고, 게임 화면에는 운명·장비 등의 보정이 더해진 값이 나옵니다. 편집기는 저장값을 바꿉니다.", 0, footer + 86, ContentW, 30, 15).color = Ui.Dim;
        }
    }
}
```

- [ ] **Step 3: `UI/Tabs/NpcTab.cs`를 통째로 바꾼다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // NPC 탭: 가까운 NPC 목록, 이름 검색, 호감도, 편집 대상 지정.
    internal sealed class NpcTab : EditorTab
    {
        private const float RowH = 40;

        private sealed class Row
        {
            public string Id;
            public string Name;
            public int Distance;
        }

        private readonly ListView<Row> list = new ListView<Row>("NpcList", RowH);
        private string query = "";
        private List<Row> cache;   // 가까운 순서. null이면 다시 만든다

        private static int Step
        {
            get { return AmountBar.Get("intim"); }
        }

        public override string Title
        {
            get { return "NPC"; }
        }

        public NpcTab()
        {
            list.EmptyText = "조건에 맞는 NPC가 없습니다.";
        }

        // 편집기를 열 때마다 목록을 새로 만든다(그 사이에 NPC가 움직였을 수 있다).
        public override void OnOpen()
        {
            cache = null;
        }

        // 이름은 여기서 한 번만 읽어 둔다. 치는 대로 거르려면 수천 명의 이름을 글자마다 게임에서 다시 읽을 수 없다.
        private void BuildCache()
        {
            cache = new List<Row>();
            WorldUnitBase player = Player;
            string playerId = player.data.unitData.unitID;
            int px = player.data.unitData.pointX;
            int py = player.data.unitData.pointY;
            var units = g.world.unit.GetUnits(false);
            for (int i = 0; i < units.Count; i++)
            {
                var data = units[i].data.unitData;
                if (data.unitID == playerId)
                {
                    continue;
                }
                cache.Add(new Row
                {
                    Id = data.unitID,
                    Name = EditService.NameOf(units[i]),
                    Distance = Math.Abs(data.pointX - px) + Math.Abs(data.pointY - py),
                });
            }
            cache.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        }

        private List<Row> Found()
        {
            if (query.Length == 0)
            {
                return cache;
            }
            string wanted = EditService.Compact(query);
            return cache.FindAll(row => row.Name.Contains(wanted));
        }

        protected override void Draw()
        {
            if (cache == null)
            {
                BuildCache();
            }
            WorldUnitBase player = Player;
            WorldUnitBase selected = EditorWindow.SelectedNpcId == null ? null : g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true);

            Ui.Panel("SelectedBox", content, 0, 0, ContentW, 104, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "아래 목록에서 NPC를 선택하세요. 가까운 순서입니다.", 16, 12, 880, 36);
            }
            else
            {
                RectTransform nameHolder = Ui.Rect("SelectedName", content, 16, 8, 600, 36);
                Ui.Label(nameHolder, "선택: " + EditService.NameOf(selected) + "   " + EditService.GradeName(selected.data.unitData.propertyData.gradeID), 0, 0, 600, 36, 21);
                Ui.Label(content, "호감도  나→상대 " + EditService.IntimTo(player, selected) + "   상대→나 " + EditService.IntimTo(selected, player), 16, 52, 380, 40);
                Ui.Button("IntimMinus", content, "- " + Step, 400, 54, 100, 38, new Action(() => ChangeIntim(-Step)));
                Ui.Button("IntimPlus", content, "+ " + Step, 506, 54, 100, 38, new Action(() => ChangeIntim(Step)));
                Ui.Button("IntimZero", content, "0으로", 612, 54, 90, 38, new Action(() => Say(EditService.SetIntimBoth(Player, g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true), 0))));
                Ui.Button("EditNpc", content, "능력치 편집", 716, 54, 190, 38, new Action(() =>
                {
                    EditorWindow.SetTarget(EditorWindow.SelectedNpcId);
                    EditorWindow.SwitchTab(0);
                    Say("편집 대상을 NPC로 바꿨습니다.");
                }));
                AmountBar.Build(content, "intim", 716, 8);
            }

            Ui.Label(content, "이름 검색", 0, 114, 110, 38);
            SearchBox.Build(content, "NpcQuery", 110, 114, 330, query, "이름의 일부", new Action<string>(v =>
            {
                query = v;
                list.SetItems(Found());
            }));
            Ui.Button("NpcRefresh", content, "목록 새로 고침", 460, 114, 180, 38, new Action(() => { cache = null; list.Reset(); Redraw(); }));

            Ui.Label(content, "이름", 8, 158, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "경지", 268, 158, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "거리(칸)", 568, 158, 120, 30, 18).color = Ui.Dim;
            list.Build(content, 0, 190, ContentW, ContentH - 190, Found(), DrawRow);
        }

        private void DrawRow(RectTransform row, Row npc, int index)
        {
            WorldUnitBase unit = g.world.unit.GetUnit(npc.Id, true);
            bool picked = npc.Id == EditorWindow.SelectedNpcId;
            Ui.Label(row, npc.Name, 8, 0, 250, RowH);
            Ui.Label(row, unit == null ? "(없음)" : EditService.GradeName(unit.data.unitData.propertyData.gradeID), 268, 0, 290, RowH);
            Ui.Label(row, npc.Distance.ToString(), 568, 0, 120, RowH);
            Ui.Button("Pick" + npc.Id, row, picked ? "선택됨" : "선택", 708, 3, 110, RowH - 6, new Action(() => { EditorWindow.SelectedNpcId = npc.Id; Redraw(); }), unit != null, picked);
        }

        private void ChangeIntim(int delta)
        {
            WorldUnitBase npc = g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true);
            if (npc == null)
            {
                Say("선택한 NPC를 찾지 못했습니다.");
                return;
            }
            Say(EditService.SetIntimBoth(Player, npc, EditService.IntimTo(npc, Player) + delta));
        }
    }
}
```

- [ ] **Step 4: `UI/Tabs/SafetyTab.cs`를 통째로 바꾼다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 안전 탭: 세이브 백업, 변경 이력(지난 실행의 기록 포함).
    internal sealed class SafetyTab : EditorTab
    {
        private const int MaxLines = 2000;
        private const float RowH = 30;
        private const float DetailH = 70;

        private readonly ListView<string> list = new ListView<string>("History", RowH);
        private readonly DetailPane detail = new DetailPane("HistoryDetail", "줄을 누르면 여기에 전체가 나옵니다.");
        private string query = "";
        private List<string> lines;   // 최신이 앞. null이면 다시 읽는다
        private string readError;

        public override string Title
        {
            get { return "안전"; }
        }

        public SafetyTab()
        {
            list.EmptyText = "조건에 맞는 기록이 없습니다.";
            list.OnSelect = line => detail.Show(line);
        }

        // 탭으로 들어올 때마다 파일을 다시 읽는다(다른 탭에서 한 변경이 들어와야 한다).
        public override void OnShow()
        {
            lines = null;
            list.Reset();
            detail.Clear();
        }

        public override void OnOpen()
        {
            lines = null;
        }

        private void Load()
        {
            lines = ChangeLog.ReadHistory(MaxLines, out readError);
            if (readError == null)
            {
                return;
            }
            // 파일을 읽지 못하면 이번 실행의 기록(메모리)을 보인다.
            IList<ChangeLog.Entry> entries = ChangeLog.Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                lines.Add(entries[i].ToString());
            }
        }

        private List<string> Found()
        {
            return query.Length == 0 ? lines : lines.FindAll(line => line.Contains(query));
        }

        protected override void Draw()
        {
            if (lines == null)
            {
                Load();
            }
            Ui.Panel("BackupBox", content, 0, 0, ContentW, 104, Ui.Section);
            Ui.Label(content, "세이브 백업: " + (SaveBackup.LastBackupDir == null ? "이번 실행에서 아직 백업하지 않았습니다." : "완료"), 16, 8, 880, 34);
            if (SaveBackup.LastBackupDir != null)
            {
                Ui.Label(content, SaveBackup.LastBackupDir, 16, 40, 880, 28, 15).color = Ui.Dim;
            }
            Ui.Button("BackupNow", content, "지금 백업", 16, 62, 160, 36, new Action(() =>
            {
                string error = SaveBackup.BackupNow();
                Say(error ?? "세이브를 백업했습니다.");
            }));
            Ui.Label(content, "값을 처음 바꾸기 직전에 자동으로 한 번 백업합니다. 백업은 디스크에 저장된 세이브의 사본입니다.", 190, 64, 720, 32, 16).color = Ui.Dim;

            Ui.Label(content, "변경 이력", 0, 114, 110, 38, 20);
            SearchBox.Build(content, "HistoryQuery", 110, 114, 330, query, "대상이나 항목의 일부", new Action<string>(v =>
            {
                query = v;
                detail.Clear();
                list.SetItems(Found());
            }));
            Text note = Ui.Label(content, readError == null ? "지난 실행의 기록을 포함해 최근 " + MaxLines + "줄까지, 최신이 위입니다."
                : "기록 파일을 읽지 못해 이번 실행의 기록만 보입니다(" + readError + ").", 460, 114, 840, 38, 15);
            note.color = readError == null ? Ui.Dim : Ui.Warn;

            list.Build(content, 0, 158, ContentW, ContentH - 158 - DetailH - 8, Found(), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawRow(RectTransform row, string line, int index)
        {
            Text text = Ui.Label(row, Shorten(line, 92), 8, 0, ContentW - 40, RowH, 16);
            // 쓴 뒤 다시 읽은 값이 달랐던 기록
            if (line.Contains("(재조회 불일치)"))
            {
                text.color = Ui.Warn;
            }
        }
    }
}
```

- [ ] **Step 5: `ui-check.ps1`의 `tabs` 부분에서 줄 번호로 된 이름을 바꾼다**

| 옛 | 새 |
|---|---|
| `Press 'Pick0' \| Out-Null` | `Press (Pos 'Pick*').name \| Out-Null` |

- [ ] **Step 6: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 7: 회차 — `numbers`, `people`, `history`, `tabs`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-t3
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part numbers
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part people
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part history
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part tabs
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-t3
```

Expected: 네 부분 모두 `FAILS=0`. (`tabs`의 하위 화면 검사 `SkillBack`·`LuckBack`·`FateBack`은 아직 옛 이름으로 통과한다. Task 4에서 바꾼다.)

- [ ] **Step 8: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI/Tabs src/ModCode/ModMain/Safety/ChangeLog.cs tools/research/ui-check.ps1
git -C E:\TlToybox commit -m @'
feat: 능력치·NPC·안전 탭 — 값 칸에 바로 입력, 스크롤 목록, 변경 이력 전체 보기

능력치의 저장값 칸에 숫자를 쓰고 Enter를 누르면 적용된다. NPC 목록은 끝까지
스크롤되고 이름을 치는 대로 걸러진다. 안전 탭은 지난 실행의 기록까지 최근
2,000줄을 보이고 검색할 수 있다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 4: 공법, 운명, 기예 탭 — 하위 화면과 확인

**Files:**
- Rewrite: `src/ModCode/ModMain/UI/Tabs/MartialTab.cs`, `LuckTab.cs`, `ArtTab.cs`
- Modify: `tools/research/ui-check.ps1` (`tabs` 부분의 이름)

**Interfaces:**
- Consumes: Task 1의 부품과 `EditorTab.Push`/`Pop`; `MartialService`, `SkillCatalog`, `LuckService`, `FateService`, `ArtService`의 공개 멤버(지금 탭이 쓰는 것과 같다).
- Produces: 요소 이름 `MartialList`, `PickMartial<순번>`, `SkillAddOpen`, `SkillQuery`, `SkillList`, `SkillAdd<기술 번호>`, `PrefixList`, `ReplacePick<옵션 번호>`, `LuckList`, `LuckRemove<운명 번호>`, `LuckReplace<운명 번호>`, `LuckToBorn<운명 번호>`, `LuckAddOpen`, `FateOpen`, `LuckQuery`, `LuckPick`, `LuckAdd<운명 번호>`, `FateChange<대경지>`, `FateQuery`, `FatePick`, `FatePick<운명 번호>`, `ArtQuery`, `ArtTable`, `ArtAct<서적 번호>`, 하위 화면의 `Back`.

- [ ] **Step 1: `UI/Tabs/MartialTab.cs`를 통째로 바꾼다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 공법 탭: 익힌 공법의 옵션·숙련 경험치·장착·망각. 하위 화면: 기술 추가, 옵션 교체.
    internal sealed class MartialTab : EditorTab
    {
        private sealed class Learned
        {
            public string Id;
            public DataUnit.ActionMartialData Data;
        }

        private const float BoxH = 330;

        private readonly ListView<Learned> martialList = new ListView<Learned>("MartialList", 38);
        private readonly ListView<SkillCatalog.Entry> skillList = new ListView<SkillCatalog.Entry>("SkillList", 50);
        private readonly ListView<ConfBattleSkillPrefixValueItem> prefixList = new ListView<ConfBattleSkillPrefixValueItem>("PrefixList", 50);
        private readonly DetailPane optionDetail = new DetailPane("OptionDetail", "옵션의 글을 누르면 여기에 전체가 나옵니다.");
        private readonly DetailPane skillDetail = new DetailPane("SkillDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");
        private readonly DetailPane prefixDetail = new DetailPane("PrefixDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");

        private string martialId;       // 선택한 공법
        private int replaceIndex = -1;  // 옵션 교체 화면에서 바꾸는 자리
        private int skillKind;
        private int skillGrade;
        private string skillQuery = "";

        private static int Step
        {
            get { return AmountBar.Get("martialExp"); }
        }

        public override string Title
        {
            get { return "공법"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public MartialTab()
        {
            martialList.EmptyText = "익힌 공법이 없습니다.";
            skillList.EmptyText = "조건에 맞는 기술이 없습니다.";
            skillList.OnSelect = entry => skillDetail.Show(entry.Name + "  [" + entry.BaseId + "]  경지 " + entry.GradeMin + "~" + entry.GradeMax + "\n" + entry.Desc);
            prefixList.OnSelect = item => prefixDetail.Show(item.number + "번  " + MartialService.CleanDesc(GameTool.LS(item.desc)));
        }

        public override void OnTargetChanged()
        {
            martialId = null;
            replaceIndex = -1;
            skillGrade = 0;
            martialList.Reset();
            optionDetail.Clear();
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            var all = new List<Learned>();
            foreach (var pair in target.data.unitData.allActionMartial)
            {
                all.Add(new Learned { Id = pair.Key, Data = pair.Value });
            }
            DataProps.MartialData selected = martialId == null ? null : MartialService.Martial(target, martialId);
            if (selected == null)
            {
                martialId = null;
            }

            Ui.Label(content, "공법", 8, 0, 300, 30, 18).color = Ui.Dim;
            Ui.Label(content, "종류", 338, 0, 120, 30, 18).color = Ui.Dim;
            Ui.Label(content, "경지", 468, 0, 80, 30, 18).color = Ui.Dim;
            Ui.Label(content, "숙련 경험치", 558, 0, 150, 30, 18).color = Ui.Dim;
            Ui.Button("SkillAddOpen", content, "기술 추가", ContentW - 160, 0, 160, 30, new Action(() =>
            {
                skillGrade = 0;
                skillList.Reset();
                skillDetail.Clear();
                Push(() => "기술 추가 — " + EditService.NameOf(Target), DrawSkillAdd);
            }));
            float listH = ContentH - 32 - BoxH - 10;
            martialList.Build(content, 0, 32, ContentW, listH, all, DrawMartialRow);

            float box = 32 + listH + 10;
            Ui.Panel("MartialBox", content, 0, box, ContentW, BoxH, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "위에서 공법을 선택하면 옵션을 편집할 수 있습니다.", 16, box + 10, 880, 36);
                return;
            }
            Ui.Label(content, MartialService.Name(selected) + "  (" + MartialService.TypeLabel(selected.martialType) + ")", 16, box + 6, 420, 36, 21);
            Ui.Button("MartialExp", content, "숙련 경험치 + " + Step, 450, box + 8, 230, 34, new Action(() => Say(MartialService.AddExp(Target, martialId, Step))));
            AmountBar.Build(content, "martialExp", 700, box + 6);

            List<MartialService.PrefixView> prefixes = MartialService.Prefixes(selected);
            if (prefixes.Count == 0)
            {
                Ui.Label(content, "이 공법에는 옵션이 없습니다.", 16, box + 52, 880, 34, 18).color = Ui.Dim;
            }
            for (int i = 0; i < prefixes.Count && i < 6; i++)
            {
                MartialService.PrefixView view = prefixes[i];
                int slot = view.Index;
                float y = box + 50 + i * 38;
                Ui.Button("OptionText" + slot, content, (slot + 1) + ". " + Shorten(view.Desc, 28), 16, y, 520, 32, new Action(() => optionDetail.Show((slot + 1) + ". " + view.Desc)));
                Ui.Label(content, "품질 " + view.Level + "/" + view.MaxLevel, 545, y, 110, 34, 18);
                Ui.Button("QualityMinus" + slot, content, "-", 660, y, 44, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.Level - 1))), view.Level > 1);
                Ui.Button("QualityPlus" + slot, content, "+", 708, y, 44, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.Level + 1))), view.Level < view.MaxLevel);
                Ui.Button("QualityMax" + slot, content, "최고", 756, y, 66, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.MaxLevel))), view.Level != view.MaxLevel);
                Ui.Button("Replace" + slot, content, "교체", 826, y, 80, 32, new Action(() =>
                {
                    replaceIndex = slot;
                    prefixList.Reset();
                    prefixDetail.Clear();
                    Push(() =>
                    {
                        DataProps.MartialData martial = MartialService.Martial(Target, martialId);
                        return (martial == null ? "공법" : MartialService.Name(martial)) + " — 옵션 " + (replaceIndex + 1) + " 교체";
                    }, DrawPrefixPicker);
                }));
            }
            optionDetail.Build(content, 920, box + 50, ContentW - 936, 228);

            // 장착·해제와 망각. 망각은 한 번 더 확인한다.
            bool equipped = MartialService.IsEquipped(target, martialId);
            Ui.Button("MartialEquip", content, equipped ? "장착 해제" : "장착", 16, box + 286, 150, 34,
                new Action(() => Say(equipped ? MartialService.Unequip(Target, martialId) : MartialService.Equip(Target, martialId))));
            Confirm.Button("MartialForget", content, "기술 망각", "정말 망각", 176, box + 286, 150, 34, "forget:" + martialId,
                "이 공법을 익힌 목록에서 지웁니다. 계속하려면 \"정말 망각\"을 한 번 더 누르세요.", new Action(() =>
                {
                    string result = MartialService.Forget(Target, martialId);
                    martialId = null;
                    optionDetail.Clear();
                    Say(result);
                }));
            Ui.Label(content, "수련은 위의 숙련 경험치로, 참오는 위 옵션의 품질·교체로 대신합니다.", 340, box + 286, 570, 34, 15).color = Ui.Dim;
        }

        private void DrawMartialRow(RectTransform row, Learned learned, int index)
        {
            DataProps.MartialData martial = learned.Data.data.To<DataProps.MartialData>();
            bool picked = learned.Id == martialId;
            Ui.Label(row, MartialService.Name(martial), 8, 0, 320, 38);
            Ui.Label(row, MartialService.TypeLabel(martial.martialType), 338, 0, 120, 38);
            Ui.Label(row, martial.grade.ToString(), 468, 0, 80, 38);
            Ui.Label(row, learned.Data.exp.ToString(), 558, 0, 150, 38);
            Ui.Button("PickMartial" + index, row, picked ? "선택됨" : "선택", 728, 3, 110, 32, new Action(() =>
            {
                martialId = learned.Id;
                optionDetail.Clear();
                Redraw();
            }), true, picked);
            Ui.Label(row, MartialService.IsEquipped(Target, learned.Id) ? "장착 중" : "", 848, 0, 120, 38, 17).color = Ui.Warn;
        }

        // 하위 화면: 게임 기술 화면의 다섯 분류에서 이름으로 골라 배운 상태로 넣는다.
        private void DrawSkillAdd()
        {
            WorldUnitBase target = Target;
            float y = SubTop;
            for (int i = 0; i < SkillCatalog.Kinds.Length; i++)
            {
                int index = i;
                Ui.Button("SkillKind" + i, content, SkillCatalog.Kinds[i].Tab, i * 156, y, 150, 38, new Action(() =>
                {
                    skillKind = index;
                    skillList.Reset();
                    skillDetail.Clear();
                    Redraw();
                }), true, skillKind == i);
            }
            SearchBox.Build(content, "SkillQuery", 800, y, 330, skillQuery, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                skillQuery = v;
                skillDetail.Clear();
                skillList.SetItems(SkillCatalog.Find(skillKind, skillQuery));
            }));

            // 만들 기술의 경지. 처음에는 편집 대상의 현재 대경지.
            if (skillGrade <= 0)
            {
                skillGrade = Math.Max(1, FateService.CurrentGrade(target));
            }
            Ui.Label(content, "경지", 0, y + 46, 60, 36, 18).color = Ui.Dim;
            for (int grade = 1; grade <= 10; grade++)
            {
                int value = grade;
                Ui.Button("SkillGrade" + grade, content, grade.ToString(), 60 + (grade - 1) * 50, y + 46, 46, 36, new Action(() => { skillGrade = value; Redraw(); }), true, skillGrade == grade);
            }
            Ui.Button("SkillLearnAll", content, SkillCatalog.Kinds[skillKind].Tab + " 모두 배우기", 580, y + 46, 240, 36, new Action(() => Say(SkillCatalog.LearnAll(Target, skillKind, skillGrade))));
            Ui.Label(content, "선택한 경지에서 배울 수 있고 아직 익히지 않은 것만 넣습니다. 옵션은 게임의 뽑기로 정해집니다.", 834, y + 46, 470, 36, 13).color = Ui.Dim;

            learnedCounts = SkillCatalog.LearnedCounts(target, skillKind);
            float top = y + 92;
            skillList.Build(content, 0, top, ContentW, ContentH - top - 78, SkillCatalog.Find(skillKind, skillQuery), DrawSkillRow);
            skillDetail.Build(content, 0, ContentH - 70, ContentW, 70);
        }

        private Dictionary<int, int> learnedCounts = new Dictionary<int, int>();

        private void DrawSkillRow(RectTransform row, SkillCatalog.Entry entry, int index)
        {
            string blocked = SkillCatalog.BlockReason(entry, skillGrade);
            int have;
            learnedCounts.TryGetValue(entry.BaseId, out have);
            Ui.Label(row, Shorten(entry.Name, 16) + "  [" + entry.BaseId + "]  경지 " + entry.GradeMin + "~" + entry.GradeMax, 8, 0, 800, 26, 18);
            Text state = Ui.Label(row, have > 0 ? "배움" + (have > 1 ? " x" + have : "") : blocked != null ? "배울 수 없음" : "-", 820, 0, 200, 26, 16);
            state.color = have > 0 ? Ui.Warn : Ui.Dim;
            Ui.Label(row, Shorten(entry.Desc, 76), 24, 24, 1080, 22, 15).color = Ui.Dim;
            float x = ContentW - Ui.ScrollBarWidth - 178;
            if (blocked == null)
            {
                Ui.Button("SkillAdd" + entry.BaseId, row, have > 0 ? "한 개 더 추가" : "배우기", x, 7, 170, 36,
                    new Action(() => Say(SkillCatalog.Learn(Target, skillKind, entry, skillGrade, false))));
                return;
            }
            // 지금 배울 수 없는 것: 이유를 보여 주고, 한 번 더 누르면 조건을 건너뛰고 넣는다.
            Confirm.Button("SkillAdd" + entry.BaseId, row, "강제 추가", "정말 강제", x, 7, 170, 36, "skill:" + entry.BaseId,
                entry.Name + ": " + blocked + ". \"정말 강제\"를 한 번 더 누르면 이 기술의 경지 범위 안에서 가장 가까운 경지로 만들어 넣습니다.",
                new Action(() => Say(SkillCatalog.Learn(Target, skillKind, entry, skillGrade, true))));
        }

        // 하위 화면: 이 공법에 붙을 수 있는 옵션을 모두 보여 주고, 규칙에 걸리는 것은 사유와 함께 막는다.
        private void DrawPrefixPicker()
        {
            DataProps.MartialData martial = martialId == null ? null : MartialService.Martial(Target, martialId);
            if (martial == null || replaceIndex < 0)
            {
                Ui.Label(content, "공법을 찾지 못했습니다. 돌아가서 다시 선택하세요.", 0, SubTop, 800, 36).color = Ui.Warn;
                return;
            }
            pickerMartial = martial;
            List<ConfBattleSkillPrefixValueItem> items = MartialService.ValidItems(martial);
            Ui.Label(content, "붙을 수 있는 옵션 " + items.Count + "개. 교체하면 품질은 이전 값을 유지합니다(새 옵션의 한도 안에서).", 0, SubTop, 900, 30, 16).color = Ui.Dim;
            float top = SubTop + 36;
            prefixList.Build(content, 0, top, ContentW, ContentH - top - 78, items, DrawPrefixRow);
            prefixDetail.Build(content, 0, ContentH - 70, ContentW, 70);
        }

        private DataProps.MartialData pickerMartial;

        private void DrawPrefixRow(RectTransform row, ConfBattleSkillPrefixValueItem item, int index)
        {
            string blocked = MartialService.ReplaceBlockReason(pickerMartial, replaceIndex, item);
            Ui.Label(row, item.number + "번  " + Shorten(MartialService.CleanDesc(GameTool.LS(item.desc)), 60), 8, 0, 1080, 28, 17);
            if (blocked != null)
            {
                Ui.Label(row, "불가: " + blocked, 38, 24, 1050, 22, 15).color = Ui.Warn;
            }
            int slot = replaceIndex;
            Ui.Button("ReplacePick" + item.number, row, "이걸로", ContentW - Ui.ScrollBarWidth - 118, 7, 110, 36, new Action(() =>
            {
                string result = MartialService.ReplacePrefix(Target, martialId, slot, item);
                CloseViews();
                Say(result);
            }), blocked == null);
        }
    }
}
```

- [ ] **Step 2: `UI/Tabs/LuckTab.cs`를 통째로 바꾼다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 운명 탭: 선천·후천운명의 추가·교체·삭제. 하위 화면: 운명 고르기, 역천개명(경지별), 역천개명 고르기.
    internal sealed class LuckTab : EditorTab
    {
        private const float DetailH = 70;

        private readonly ListView<LuckService.LuckView> ownedList = new ListView<LuckService.LuckView>("LuckList", 40);
        private readonly ListView<LuckService.LuckView> pickList = new ListView<LuckService.LuckView>("LuckPick", 50);
        private readonly ListView<LuckService.LuckView> fateList = new ListView<LuckService.LuckView>("FatePick", 50);
        private readonly DetailPane ownedDetail = new DetailPane("LuckDetail", "줄을 누르면 여기에 전체 효과가 나옵니다.");
        private readonly DetailPane pickDetail = new DetailPane("LuckPickDetail", "줄을 누르면 여기에 전체 효과가 나옵니다.");
        private readonly DetailPane fateDetail = new DetailPane("FateDetail", "줄을 누르면 여기에 전체 효과가 나옵니다.");

        private int replaceIndex = -1;  // 교체할 선천운명 자리. -1이면 추가
        private int luckType = 1;
        private string luckQuery = "";
        private int fateGrade;          // 역천개명을 고르는 중인 대경지
        private string fateQuery = "";

        public override string Title
        {
            get { return "운명"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public LuckTab()
        {
            ownedList.EmptyText = "가진 운명이 없습니다.";
            pickList.EmptyText = "조건에 맞는 운명이 없습니다.";
            fateList.EmptyText = "조건에 맞는 역천개명이 없습니다.";
            ownedList.OnSelect = view => ownedDetail.Show(Whole(view));
            pickList.OnSelect = view => pickDetail.Show(Whole(view));
            fateList.OnSelect = view => fateDetail.Show(Whole(view));
        }

        private static string Whole(LuckService.LuckView view)
        {
            return view.Name + "  [" + view.Id + "]  품질 " + view.Level + "\n" + view.Tips;
        }

        public override void OnTargetChanged()
        {
            ownedList.Reset();
            ownedDetail.Clear();
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            List<LuckService.LuckView> owned = LuckService.Owned(target).FindAll(v => !v.Hidden);

            Ui.Button("LuckAddOpen", content, "운명 추가", 0, 0, 160, 36, new Action(() => OpenPicker(-1)));
            Ui.Button("FateOpen", content, "역천개명 (경지별)", 170, 0, 220, 36, new Action(() => Push(() => "역천개명 — " + EditService.NameOf(Target), DrawFate)));
            Ui.Label(content, "운명", 8, 44, 240, 30, 18).color = Ui.Dim;
            Ui.Label(content, "게임 화면의 위치", 258, 44, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "효과", 458, 44, 500, 30, 18).color = Ui.Dim;
            ownedList.Build(content, 0, 76, ContentW, ContentH - 76 - DetailH - 44, owned, DrawOwnedRow);
            Ui.Label(content, "게임의 인물 화면은 선천운명 칸과, 후천 분류의 후천운명만 보여 줍니다. \"표시 안 됨\"은 효과는 적용되지만 그 화면에 나오지 않는 운명입니다.", 0, ContentH - DetailH - 38, ContentW, 30, 15).color = Ui.Dim;
            ownedDetail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void OpenPicker(int bornIndex)
        {
            replaceIndex = bornIndex;
            pickList.Reset();
            pickDetail.Clear();
            Push(() => (replaceIndex >= 0 ? "선천운명 " + (replaceIndex + 1) + "번 교체 — " : "운명 추가 — ") + EditService.NameOf(Target), DrawPicker);
        }

        private void DrawOwnedRow(RectTransform row, LuckService.LuckView view, int index)
        {
            WorldUnitBase target = Target;
            // 게임의 인물 화면은 후천 목록 가운데 후천 분류만 후천운명 칸에 보여 준다.
            bool shown = view.Born || view.Type == 2;
            Ui.Label(row, Shorten(view.Name, 11), 8, 0, 245, 40);
            Text place = Ui.Label(row, view.Born ? "선천운명" : shown ? "후천운명" : "표시 안 됨(효과만)", 258, 0, 200, 40, 17);
            if (!shown)
            {
                place.color = Ui.Warn;
            }
            Ui.Label(row, Shorten(view.Tips, 34), 458, 0, 560, 40, 16);
            float right = ContentW - Ui.ScrollBarWidth;
            if (view.Born)
            {
                int bornIndex = view.BornIndex;
                Ui.Button("LuckReplace" + view.Id, row, "교체", right - 254, 3, 120, 34, new Action(() => OpenPicker(bornIndex)));
            }
            else if (view.Type == 1)
            {
                Ui.Button("LuckToBorn" + view.Id, row, "선천으로 옮김", right - 294, 3, 160, 34, new Action(() => Say(LuckService.AddBorn(Target, view.Id))));
            }
            Action remove = new Action(() => Say(view.Born ? LuckService.RemoveBorn(Target, view.BornIndex) : LuckService.Remove(Target, view.Id)));
            // 편집기가 붙인 후천 목록의 운명만 바로 지운다. 선천운명과 게임이 붙인 운명은 한 번 더 확인한다.
            if (view.Born || !LuckService.AddedByEditor(target, view.Id))
            {
                Confirm.Button("LuckRemove" + view.Id, row, "삭제", "정말 삭제", right - 128, 3, 120, 34, "luck:" + view.Id,
                    view.Name + ": " + (view.Born ? "선천운명입니다" : "게임이 붙인 운명입니다") + (view.IsSystem ? "(경지 진행 표식)" : "") + ". 지우려면 \"정말 삭제\"를 한 번 더 누르세요.", remove);
            }
            else
            {
                Ui.Button("LuckRemove" + view.Id, row, "삭제", right - 128, 3, 120, 34, remove);
            }
        }

        // 하위 화면: 운명 고르기(추가 또는 선천운명 교체)
        private void DrawPicker()
        {
            bool replacing = replaceIndex >= 0;
            float y = SubTop;
            int[] types = { 1, 2, 3 };
            for (int i = 0; i < types.Length; i++)
            {
                int type = types[i];
                Ui.Button("LuckType" + type, content, LuckService.TypeLabel(type) + " 분류", i * 146, y, 140, 38, new Action(() =>
                {
                    luckType = type;
                    pickList.Reset();
                    pickDetail.Clear();
                    Redraw();
                }), true, luckType == type);
            }
            SearchBox.Build(content, "LuckQuery", 460, y, 330, luckQuery, "이름·효과의 일부", new Action<string>(v =>
            {
                luckQuery = v;
                pickDetail.Clear();
                pickList.SetItems(LuckService.Candidates(luckType, luckQuery));
            }));
            string note = replacing ? "" : luckType == 1 ? "선천 분류는 선천운명 칸에 추가됩니다."
                : luckType == 2 ? "후천 분류에는 게임이 진행 상태를 나타내려고 쓰는 운명이 섞여 있습니다."
                : "역천개명 분류는 여기서 붙이면 효과만 적용됩니다. 경지 화면에 나오게 하려면 역천개명 (경지별)을 쓰세요.";
            Text noteLabel = Ui.Label(content, note, 800, y, 500, 38, 14);
            noteLabel.color = Ui.Warn;
            noteLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            float top = y + 46;
            pickList.Build(content, 0, top, ContentW, ContentH - top - DetailH - 8, LuckService.Candidates(luckType, luckQuery), DrawPickRow);
            pickDetail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawPickRow(RectTransform row, LuckService.LuckView view, int index)
        {
            bool replacing = replaceIndex >= 0;
            bool has = LuckService.Has(Target, view.Id);
            Text name = Ui.Label(row, Shorten(view.Name, 14) + "  (품질 " + view.Level + ")" + (view.IsSystem ? "  — 경지 진행 표식" : ""), 8, 0, 1000, 26, 18);
            if (view.IsSystem)
            {
                name.color = Ui.Warn;
            }
            Ui.Label(row, Shorten(view.Tips, 76), 24, 24, 1080, 22, 15).color = Ui.Dim;
            string label = has ? "보유 중" : replacing ? "이것으로 교체" : view.Type == 1 ? "선천운명에 추가" : view.Type == 2 ? "후천운명에 추가" : "추가";
            Ui.Button("LuckAdd" + view.Id, row, label, ContentW - Ui.ScrollBarWidth - 198, 7, 190, 36, new Action(() =>
            {
                if (replacing)
                {
                    string result = LuckService.ReplaceBorn(Target, replaceIndex, view.Id);
                    CloseViews();
                    Say(result);
                    return;
                }
                Say(view.Type == 1 ? LuckService.AddBorn(Target, view.Id) : LuckService.Add(Target, view.Id));
            }), !has);
        }

        // 하위 화면: 도달한 경지마다 한 줄. 변경을 누르면 그 경지의 후보 목록이 열린다.
        private void DrawFate()
        {
            WorldUnitBase target = Target;
            float y = SubTop;
            Ui.Label(content, "경지", 0, y, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "역천개명", 210, y, 240, 30, 18).color = Ui.Dim;
            Ui.Label(content, "효과", 460, y, 600, 30, 18).color = Ui.Dim;
            List<FateService.Slot> slots = FateService.Slots(target);
            if (slots.Count == 0)
            {
                Ui.Label(content, "역천개명은 축기경부터 경지마다 하나씩 있습니다. 이 인물은 아직 축기경에 이르지 않았습니다.", 0, y + 36, ContentW, 36, 18).color = Ui.Warn;
            }
            for (int i = 0; i < slots.Count; i++)
            {
                FateService.Slot slot = slots[i];
                float rowY = y + 34 + i * 40;
                Ui.Label(content, slot.GradeName, 0, rowY, 200, 36);
                Text name = Ui.Label(content, slot.LuckId == 0 ? "없음" : Shorten(slot.LuckName, 11), 210, rowY, 245, 36);
                if (slot.LuckId == 0)
                {
                    name.color = Ui.Dim;
                }
                Ui.Label(content, Shorten(slot.Tips, 44), 460, rowY, 700, 36, 16);
                Ui.Button("FateChange" + slot.Grade, content, slot.LuckId == 0 ? "정하기" : "변경", 1180, rowY + 1, 120, 34, new Action(() =>
                {
                    fateGrade = slot.Grade;
                    fateList.Reset();
                    fateDetail.Clear();
                    Push(() => "역천개명 고르기 — " + EditService.NameOf(Target) + ", 대경지 " + fateGrade, DrawFatePicker);
                }));
            }
            Ui.Label(content, "게임의 경지 화면은 경지마다 역천개명 하나를 보여 줍니다. 여기서 정하면 효과와 그 화면의 표시가 함께 바뀝니다. 도달한 경지만 정할 수 있습니다.", 0, y + 34 + Math.Max(1, slots.Count) * 40 + 12, ContentW, 30, 15).color = Ui.Dim;
        }

        // 하위 화면: 그 경지에서 게임이 뽑을 수 있는 역천개명
        private void DrawFatePicker()
        {
            float y = SubTop;
            SearchBox.Build(content, "FateQuery", 0, y, 330, fateQuery, "이름·효과의 일부", new Action<string>(v =>
            {
                fateQuery = v;
                fateDetail.Clear();
                fateList.SetItems(FateService.Candidates(fateGrade, fateQuery));
            }));
            Ui.Label(content, "이 경지에서 게임이 뽑을 수 있는 역천개명만 나옵니다.", 350, y, 700, 38, 15).color = Ui.Dim;
            float top = y + 46;
            fateList.Build(content, 0, top, ContentW, ContentH - top - DetailH - 8, FateService.Candidates(fateGrade, fateQuery), DrawFateRow);
            fateDetail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawFateRow(RectTransform row, LuckService.LuckView view, int index)
        {
            bool has = LuckService.Has(Target, view.Id);
            Ui.Label(row, Shorten(view.Name, 14) + "  (품질 " + view.Level + ")", 8, 0, 1000, 26, 18);
            Ui.Label(row, Shorten(view.Tips, 76), 24, 24, 1080, 22, 15).color = Ui.Dim;
            int grade = fateGrade;
            Ui.Button("FatePick" + view.Id, row, has ? "보유 중" : "이것으로 정하기", ContentW - Ui.ScrollBarWidth - 198, 7, 190, 36, new Action(() =>
            {
                string result = FateService.Set(Target, grade, view.Id);
                Pop();
                Say(result);
            }), !has);
        }
    }
}
```

- [ ] **Step 3: `UI/Tabs/ArtTab.cs`를 통째로 바꾼다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 기예 탭: 게임 기예 화면의 여섯 분류. 서적을 골라 배우게 하거나 배운 기록을 지운다.
    internal sealed class ArtTab : EditorTab
    {
        private const float RowH = 32;
        private const float DetailH = 70;
        private const float ColId = 8, ColName = 110, ColLevel = 420, ColState = 490, ColDesc = 600, ColAct = 1166;

        private readonly ListView<ItemCatalog.Entry> list = new ListView<ItemCatalog.Entry>("ArtTable", RowH);
        private readonly DetailPane detail = new DetailPane("ArtDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");
        private int artKind;
        private string query = "";

        public override string Title
        {
            get { return "기예"; }
        }

        public ArtTab()
        {
            list.OnSelect = book => detail.Show("[" + book.Id + "] " + book.Name + "  (품질 " + book.Level + ")\n" + book.Desc);
        }

        protected override void Draw()
        {
            for (int i = 0; i < ArtService.Kinds.Length; i++)
            {
                int index = i;
                ArtService.Kind each = ArtService.Kinds[i];
                Ui.Button("ArtKind" + i, content, each.Tab + " (" + ArtService.LearnedCount(each) + "/" + ArtService.Books(each, "").Count + ")", i * 176, 0, 170, 40,
                    new Action(() => { artKind = index; list.Reset(); detail.Clear(); Redraw(); }), true, artKind == i);
            }
            ArtService.Kind kind = ArtService.Kinds[artKind];
            list.EmptyText = "조건에 맞는 " + kind.Book + "이(가) 없습니다.";

            Ui.Label(content, kind.Book, 0, 50, 90, 38);
            SearchBox.Build(content, "ArtQuery", 90, 50, 330, query, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                query = v;
                detail.Clear();
                list.SetItems(ArtService.Books(ArtService.Kinds[artKind], query));
            }));
            Ui.Button("ArtLearnAll", content, kind.Tab + " 모두 배우기", 600, 50, 220, 38, new Action(() => Say(ArtService.LearnAll(ArtService.Kinds[artKind]))));
            Ui.Label(content, "기예는 플레이어의 기록입니다(편집 대상과 무관).", 840, 50, 460, 38, 15).color = Ui.Dim;

            Ui.Panel("ArtHead", content, 0, 98, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", ColId, 98, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, kind.Book, ColName, 98, 300, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", ColLevel, 98, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", ColState, 98, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", ColDesc, 98, 500, 30, 16).color = Ui.Dim;

            list.Build(content, 0, 130, ContentW, ContentH - 130 - DetailH - 8, ArtService.Books(kind, query), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawRow(RectTransform row, ItemCatalog.Entry book, int index)
        {
            ArtService.Kind kind = ArtService.Kinds[artKind];
            bool learned = ArtService.Learned(kind, book.Id);
            // 배우지 않은 것은 지금 배울 수 있는지 게임에 물어 본다.
            string blocked = learned ? null : ArtService.BlockReason(kind, book.Id);
            Ui.Label(row, book.Id.ToString(), ColId, 0, 100, RowH, 16);
            Ui.Label(row, Shorten(book.Name, 15), ColName, 0, 305, RowH, 17);
            Ui.Label(row, book.Level.ToString(), ColLevel, 0, 60, RowH, 16);
            Text state = Ui.Label(row, learned ? "배움" : blocked != null ? "배울 수 없음" : "-", ColState, 0, 110, RowH, 15);
            state.color = learned ? Ui.Warn : Ui.Dim;
            Ui.Label(row, Shorten(book.Desc, 32), ColDesc, 0, 560, RowH, 15).color = Ui.Dim;
            string name = "ArtAct" + book.Id;
            if (learned)
            {
                Confirm.Button(name, row, "잊기", "정말 잊기", ColAct, 2, 116, RowH - 4, "art:" + book.Id,
                    book.Name + ": 배운 기록을 지웁니다. 계속하려면 \"정말 잊기\"를 한 번 더 누르세요.",
                    new Action(() => Say(ArtService.Forget(ArtService.Kinds[artKind], book))));
            }
            else if (blocked == null)
            {
                Ui.Button(name, row, "배우기", ColAct, 2, 116, RowH - 4, new Action(() => Say(ArtService.Learn(ArtService.Kinds[artKind], book))));
            }
            else
            {
                // 지금 배울 수 없는 것: 이유를 보여 주고, 한 번 더 누르면 조건을 건너뛰고 넣는다.
                Confirm.Button(name, row, "강제 추가", "정말 강제", ColAct, 2, 116, RowH - 4, "art:" + book.Id,
                    book.Name + ": " + blocked + ". 그래도 넣으려면 \"정말 강제\"를 한 번 더 누르세요.",
                    new Action(() => Say(ArtService.ForceLearn(ArtService.Kinds[artKind], book))));
            }
        }
    }
}
```

- [ ] **Step 4: `ui-check.ps1`의 `tabs` 부분에서 이름을 바꾼다**

| 옛 | 새 |
|---|---|
| `IsAt 'SkillBack'`, `Press 'SkillBack' \| Out-Null` | `IsAt 'Back'`, `Press 'Back' \| Out-Null` |
| `IsAt 'LuckBack'`, `Press 'LuckBack' \| Out-Null` | `IsAt 'Back'`, `Press 'Back' \| Out-Null` |
| `IsAt 'FateBack'`, `Press 'FateBack' \| Out-Null` | `IsAt 'Back'`, `Press 'Back' \| Out-Null` |
| `Press 'LuckRemove0' \| Out-Null` | `$rm = (Pos 'LuckRemove*').name; Press $rm \| Out-Null` |
| `(Cmd 'uitext LuckRemove0')` (2곳) | `(Cmd "uitext $rm")` |

- [ ] **Step 5: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 6: 회차 — `confirm`, `tabs`, 그리고 실제 동작 하나씩**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-t4
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part confirm
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part tabs
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-t4
```

Expected: 둘 다 `FAILS=0`.

스펙 6절 12~14번("결과가 지금과 같다")은 같은 회차에서 손으로 한 번씩 눌러 결과 문구와 변경 이력으로 확인한다(`-End` 앞에). 순서와 기대:

```powershell
& tools\DevRemote\mod.ps1 -Cmd 'editor'
# 공법: 기술 추가 화면에서 첫 줄의 버튼(배우기 또는 강제 추가)
# 운명: 운명 추가 화면에서 후천 분류의 첫 "후천운명에 추가", 돌아가서 그 운명의 "삭제"
# 기예: 첫 줄의 버튼, 그 뒤 "잊기" 두 번
```

각각 `uipos <이름>*`으로 버튼을 찾아 `rc.ps1 click`으로 누르고 `mod.ps1 -Cmd 'uistate'`의 `message`가 서비스의 결과 문장(예: "… 배웠습니다", "… 추가", "… 지웠습니다")인지, `MelonLoader\Latest.log`에 `trampoline`이 없는지 본다. 결과 문장을 05 문서 27절의 검증 표에 적는다.

- [ ] **Step 7: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI/Tabs tools/research/ui-check.ps1
git -C E:\TlToybox commit -m @'
feat: 공법·운명·기예 탭 — 스크롤 목록, 즉시 검색, 하위 화면, 확인 대기 통일

쪽 넘김으로 보던 목록을 끝까지 스크롤되는 목록으로 바꾸고 검색어를 치는 대로
걸러지게 했다. 기술 추가·옵션 교체·운명 고르기·역천개명은 같은 모양의 하위
화면이 됐다. 삭제·망각·강제 추가의 확인은 다른 동작을 하면 풀린다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 5: 세계 탭 — 날짜 칸의 Enter, 확인 대기

**Files:**
- Rewrite: `src/ModCode/ModMain/UI/Tabs/WorldTab.cs`
- Modify: `tools/research/ui-check.ps1` (`numbers` 부분에 날짜 검사 추가)

**Interfaces:**
- Consumes: Task 1의 `NumberField`, `Confirm`; `WorldService.PlayerPoint`, `TeleportBy`, `TeleportToUnit`; `TimeService.Describe`, `Frozen`, `SetFrozen`, `ResidueDay`, `Advance`, `OpenSkipMonth`, `ToDate`, `WorldDay`, `TryToWorldDay`, `DateText`, `SetDate`; `BattleService.God`, `SetGod`, `Multiplier`, `NextMultiplier`.
- Produces: 개발 전용 `WorldTab.DevDateState`(모양 그대로: `input=<연>-<월>-<일> pending=<bool>`).

- [ ] **Step 1: `ui-check.ps1`의 `numbers` 부분에 날짜 검사를 넣는다**

`numbers` 부분에서 `Press 'attr10' | Out-Null` 다음, `Press 'Close' | Out-Null` 앞에 넣는다.

```powershell
    # 세계 탭: 날짜 칸에서 Enter를 누르면 적용 버튼과 같다(확인은 그대로 두 번)
    Press 'Tab7' | Out-Null
    if ((Cmd 'tin') -notmatch 'input=(\d+)-(\d+)-(\d+) pending=False') { throw '날짜 입력 상태를 읽지 못했습니다.' }
    $day = [int]$Matches[3]; $other = if ($day -eq 12) { 13 } else { 12 }
    Cmd 'uifocus DateDay' | Out-Null; Cmd "ui set DateDay $other" | Out-Null; Key 0x0D
    Check 'Enter in a date box asks like the apply button' ((Cmd 'tin') -match "-$other pending=True" -and (Cmd 'uitext DateApply') -match "text='정말 변경'")
    Press 'Tab7' | Out-Null
    Check 'coming back to the tab cancels the date change' ((Cmd 'tin') -match "-$day pending=False")
```

이 검사는 지금 빌드에서 실패한다(Enter가 아무 일도 하지 않는다). Task 1의 빨간 불 회차에서 `numbers`가 이미 실패했으므로 따로 돌리지 않는다.

- [ ] **Step 2: `UI/Tabs/WorldTab.cs`를 통째로 바꾼다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 세계 탭: 순간 이동, 날짜(고정·실제 진행·직접 지정), 전투 보조.
    internal sealed class WorldTab : EditorTab
    {
        private const string DateKey = "date";

        private readonly string[] dateInput = { "", "", "" };  // 직접 지정: 연, 월, 일
        private bool dateEdited;    // 입력 칸을 사용자가 고쳤는지. 고치지 않았으면 현재 날짜를 보여 준다

        private static int Step
        {
            get { return AmountBar.Get("move"); }
        }

        public override string Title
        {
            get { return "세계"; }
        }

        public override void OnOpen()
        {
            ResetDateInput();
        }

        public override void OnShow()
        {
            ResetDateInput();
        }

        // 직접 지정의 입력 칸을 현재 날짜로 되돌린다. 편집기를 열 때, 이 탭으로 들어올 때, 날짜가 바뀌는 동작 뒤에 부른다.
        // 그러지 않으면 예전 날짜가 입력 칸에 남아, 일부만 고쳐 적용했을 때 엉뚱한 날짜로 간다.
        private void ResetDateInput()
        {
            dateEdited = false;
        }

        protected override void Draw()
        {
            Vector2Int here = WorldService.PlayerPoint;
            Ui.Panel("MoveBox", content, 0, 0, ContentW, 250, Ui.Section);
            Ui.Label(content, "순간 이동", 16, 6, 300, 36, 21);
            Ui.Label(content, "현재 위치: (" + here.x + ", " + here.y + ")", 16, 44, 400, 34);
            Ui.Label(content, "칸 수", 640, 8, 60, 38, 18).color = Ui.Dim;
            AmountBar.Build(content, "move", 700, 8);
            // 방향 버튼: 단위 값만큼의 칸을 이동한다. 대지도는 y가 커질수록 아래쪽이다(실측 전: 방향은 버튼 이름 기준).
            Ui.Button("MoveUp", content, "y - " + Step, 150, 88, 120, 40, new Action(() => Say(WorldService.TeleportBy(0, -Step))));
            Ui.Button("MoveLeft", content, "x - " + Step, 16, 136, 120, 40, new Action(() => Say(WorldService.TeleportBy(-Step, 0))));
            Ui.Button("MoveRight", content, "x + " + Step, 284, 136, 120, 40, new Action(() => Say(WorldService.TeleportBy(Step, 0))));
            Ui.Button("MoveDown", content, "y + " + Step, 150, 184, 120, 40, new Action(() => Say(WorldService.TeleportBy(0, Step))));
            WorldUnitBase npc = EditorWindow.SelectedNpcId == null ? null : g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true);
            Ui.Button("MoveToNpc", content, npc == null ? "선택한 NPC에게 이동" : EditService.NameOf(npc) + "에게 이동", 450, 136, 300, 40,
                new Action(() => Say(WorldService.TeleportToUnit(g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true)))), npc != null);
            Ui.Label(content, npc == null ? "NPC 탭에서 NPC를 선택하면 그 위치로 갈 수 있습니다." : "", 450, 184, 460, 34, 15).color = Ui.Dim;

            float battle = DrawTimeBox(262) + 12;
            Ui.Panel("BattleBox", content, 0, battle, ContentW, 150, Ui.Section);
            Ui.Label(content, "전투 보조", 16, battle + 6, 200, 36, 21);
            Ui.Button("God", content, "무적: " + (BattleService.God ? "켜짐" : "꺼짐"), 16, battle + 48, 200, 44, new Action(() => Say(BattleService.SetGod(!BattleService.God))), true, BattleService.God);
            Ui.Button("Multiplier", content, "피해 배율: x" + BattleService.Multiplier, 232, battle + 48, 230, 44, new Action(() => Say(BattleService.NextMultiplier())), true, BattleService.Multiplier > 1);
            Ui.Label(content, "무적은 플레이어가 받는 피해를 0으로, 배율은 플레이어가 주는 피해를 곱합니다. 동료와 적끼리의 피해는 그대로입니다.", 16, battle + 98, 890, 24, 15).color = Ui.Dim;
            Ui.Label(content, "게임을 다시 켜면 둘 다 꺼진 상태로 시작합니다. 켜 둔 것은 화면 위쪽 가운데에 표시됩니다.", 16, battle + 120, 890, 24, 15).color = Ui.Dim;
        }

        // 날짜 상자: 표기, 고정, 실제 진행, 직접 지정. 아래쪽 끝의 y를 돌려준다.
        private float DrawTimeBox(float top)
        {
            const float height = 196;
            Ui.Panel("TimeBox", content, 0, top, ContentW, height, Ui.Section);
            Ui.Label(content, "날짜", 16, top + 6, 80, 36, 21);
            Ui.Label(content, TimeService.Describe(), 100, top + 6, 1000, 36);

            // 날짜 고정
            bool frozen = TimeService.Frozen;
            Ui.Button("Freeze", content, "날짜 고정: " + (frozen ? "켜짐" : "꺼짐"), 16, top + 48, 220, 40, new Action(() => { ResetDateInput(); Say(TimeService.SetFrozen(!TimeService.Frozen)); }), true, frozen);
            Ui.Label(content, "켜 두면 행동을 해도 날짜가 흐르지 않고, 게임의 달 넘기기도 막습니다. 게임을 다시 켜면 꺼진 상태로 시작합니다.", 250, top + 48, 1040, 40, 15).color = Ui.Dim;

            // 실제 진행: 게임의 날짜 진행을 그대로 실행한다. 고정 중에는 쓸 수 없다.
            float y = top + 96;
            bool canAdvance = !frozen && TimeService.ResidueDay > 0;
            Ui.Label(content, "실제 진행", 16, y, 110, 40, 18).color = Ui.Dim;
            int[] days = { 1, 5, 10 };
            for (int i = 0; i < days.Length; i++)
            {
                int amount = days[i];
                Ui.Button("Day" + amount, content, "+ " + amount + "일", 130 + i * 96, y, 90, 40, new Action(() => { ResetDateInput(); Say(TimeService.Advance(amount)); }), canAdvance);
            }
            Ui.Button("SkipMonth", content, "다음 달 1일로", 424, y, 180, 40, new Action(() =>
            {
                ResetDateInput();
                string error = TimeService.OpenSkipMonth();
                if (error != null)
                {
                    Say(error);
                    return;
                }
                EditorWindow.Close();
            }), !frozen);
            Text advanceNote = Ui.Label(content, frozen ? "날짜 고정 중에는 진행할 수 없습니다. 달력의 직접 지정은 됩니다."
                : "게임의 날짜 진행을 그대로 실행합니다. +N일은 이번 달 남은 날까지만 진행하고, 달은 게임의 확인 창에서 넘깁니다.", 620, y, 680, 40, 14);
            advanceNote.color = Ui.Dim;
            advanceNote.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 직접 지정: 달력의 날짜만 바꾼다. 두 번 눌러 확인한다. 칸에서 Enter를 눌러도 적용 버튼과 같다.
            y = top + 144;
            if (!dateEdited)
            {
                int[] now = TimeService.ToDate(TimeService.WorldDay);
                for (int i = 0; i < 3; i++)
                {
                    dateInput[i] = now[i].ToString();
                }
            }
            Ui.Label(content, "직접 지정", 16, y, 110, 40, 18).color = Ui.Dim;
            string[] names = { "DateYear", "DateMonth", "DateDay" };
            string[] units = { "년", "월", "일" };
            float[] xs = { 130, 270, 380 };
            float[] widths = { 100, 70, 70 };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                // 칸을 고치면 앞서 물은 확인은 다른 날짜에 대한 것이므로 푼다.
                NumberField.Build(content, names[i], xs[i], y, widths[i], 40, dateInput[i],
                    new Action<string>(v => { dateInput[index] = v; dateEdited = true; Confirm.Clear(); }),
                    new Action<int>(v => ApplyDate()));
                Ui.Label(content, units[i], xs[i] + widths[i] + 4, y, 30, 40, 18);
            }
            bool waiting = Confirm.IsPending(DateKey);
            Ui.Button("DateApply", content, waiting ? "정말 변경" : "적용", 494, y, 130, 40, new Action(ApplyDate), true, waiting);
            Text dateNote = Ui.Label(content, "달력의 날짜만 바꿉니다(앞·뒤 모두). 그 사이의 월말 처리는 일어나지 않고, 임무 기한 같은 날짜 기준 기록은 그대로입니다.", 640, y, 660, 40, 14);
            dateNote.color = Ui.Warn;
            dateNote.horizontalOverflow = HorizontalWrapMode.Wrap;
            return top + height;
        }

        private void ApplyDate()
        {
            int year, month, day, target;
            string error;
            if (!int.TryParse(dateInput[0], out year) || !int.TryParse(dateInput[1], out month) || !int.TryParse(dateInput[2], out day))
            {
                Say("날짜: 연·월·일을 모두 숫자로 입력하세요.");
                return;
            }
            if (!TimeService.TryToWorldDay(year, month, day, out target, out error))
            {
                Say(error);
                return;
            }
            if (target == TimeService.WorldDay)
            {
                Say("날짜: 이미 " + TimeService.DateText(target) + "입니다.");
                return;
            }
            if (!Confirm.IsPending(DateKey))
            {
                Confirm.Ask(DateKey, TimeService.DateText(TimeService.WorldDay) + " -> " + TimeService.DateText(target)
                    + ": 달력만 바뀌고 월말 처리와 날짜 기준 기록은 그대로입니다. \"정말 변경\"을 한 번 더 누르세요.");
                return;
            }
            Confirm.Clear();
            string result = TimeService.SetDate(year, month, day);
            ResetDateInput();
            Say(result);
        }

#if DEVTOOLS
        // 개발 전용: 직접 지정 입력 칸과 확인 상태.
        public string DevDateState
        {
            get { return "input=" + dateInput[0] + "-" + dateInput[1] + "-" + dateInput[2] + " pending=" + Confirm.IsPending(DateKey); }
        }
#endif
    }
}
```

- [ ] **Step 3: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 4: 회차 — `numbers`와 날짜 검사**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-t5
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part numbers
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part time
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-t5
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-t5b
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part freeze
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-t5b
```

Expected: 세 검사 모두 `FAILS=0`. `time`의 "another date button cancels the pending confirm", "reopening the editor cancels the pending confirm"이 통과해야 한다(확인 대기를 `Confirm`이 맡게 된 뒤에도).

- [ ] **Step 5: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI/Tabs/WorldTab.cs tools/research/ui-check.ps1
git -C E:\TlToybox commit -m @'
feat: 세계 탭 — 날짜 칸에서 Enter로 적용, 확인 대기를 공통 방식으로

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 6: 전체 검증, 문서, 병합, 설치

**Files:**
- Modify: `docs/05-editor-v0.1.md` (맨 끝에 27절)
- Modify: `docs/06-handoff.md`

- [ ] **Step 1: 깨끗한 회차에서 전체 검사**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-final
foreach ($part in 'base','tabs','items','numbers','people','history','confirm','order','status') { $o = pwsh -NoProfile -File tools\research\ui-check.ps1 -Part $part 2>&1; "$part : PASS=$(@($o -match '^PASS').Count) $($o | Select-Object -Last 1)"; $o | Where-Object { $_ -match '^FAIL' } }
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-final
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-final-b
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part move
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part move-restart
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part move-corrupt
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part battle
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-final-b
```

Expected: 모든 부분 `FAILS=0`. `items`는 가방 추가 칸을 바꾸므로 `history`보다 앞에 둔다(`history`의 "검색이 줄인다"가 "가방" 기록에 기댄다). 부분별 PASS 수를 적어 둔다.

- [ ] **Step 2: 배포 구성을 게임에서 확인한다**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui2-rel -ReleaseBuild
& tools\DevRemote\win.ps1 -Do key -Vk 0x2D
Start-Sleep -Seconds 1
& tools\DevRemote\rc.ps1 -Cmd 'hit 151 146','click 151 198' -NoShot
& tools\DevRemote\win.ps1 -Do shot -Out .devremote\check-release2.png -Scale 0.6
Select-String -LiteralPath 'E:\Games\Tale of Immortal\MelonLoader\Latest.log' -Pattern 'trampoline'
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui2-rel
```

Expected: `hit`이 `…/Tabs/Tab0`, `click`이 `Button.onClick: Tab1`, `trampoline` 줄 없음. 캡처에서 자원 탭의 목록이 보인다.

- [ ] **Step 3: `docs/05-editor-v0.1.md`의 맨 끝에 27절을 넣는다**

`<…>`는 실제 출력으로 채운다.

```markdown
## 27. 공통 부품과 탭별 개선 (2026-10-13 추가)

묶음 B의 2차분이다. 설계는 `docs/superpowers/specs/2026-10-13-ui-rework-design.md`, 구현 계획은 `docs/superpowers/plans/2026-10-13-ui-rework-2-widgets.md`.

### 바뀐 조작

| 무엇 | 전 | 후 |
|---|---|---|
| 목록(아이템, 서적, 기술, 옵션, 운명, 역천개명, NPC, 변경 이력) | 탭마다 쪽 넘김이거나 쪽 안의 스크롤 | 모두 끝까지 스크롤되는 목록. 아래에 `처음`·`이전`·`다음`·`끝`과 "전체 N개 중 a~b번째" |
| 검색 | 글을 쓰고 `검색` 버튼 | 치는 대로 걸러진다. 칸 오른쪽의 `×`로 지운다 |
| 긴 이름·설명 | 탭마다 다름 | 줄(또는 이름)을 누르면 목록 아래 칸에 전체가 나온다 |
| 능력치 값 지정 | 저장값 칸을 누르고 아래 줄에 입력한 뒤 `적용` | 저장값 칸에 숫자를 쓰고 Enter |
| 수량의 직접 입력, 날짜 | `적용` 버튼 | Enter도 된다 |
| 확인(삭제, 망각, 잊기, 강제 추가, 달력 변경) | 탭마다 따로 기억, 탭을 바꿔도 남음 | 창 전체에 하나. 다른 동작을 하면 풀린다 |
| 하위 화면(기술 추가, 옵션 교체, 운명 고르기, 역천개명) | 화면마다 다른 `돌아가기` | 같은 모양의 제목과 `돌아가기`. 탭을 떠나거나 대상을 바꾸면 닫힌다 |
| 가방 칸 | ±10 | 추가 칸 수를 숫자로 적어 한 번에(10칸 단위, 0~500). ±10도 그대로 |
| 변경 이력 | 이번 실행의 최근 14건 | 지난 실행을 포함해 최근 2,000줄, 검색 |
| 의복 아이템 | 295개가 모두 "의복" | "의복(여) 18101-3"처럼 성별·모양·색 |

### 의복의 구분

- 게임의 아이템 표(`ItemProps`)에서 의복(세부 분류 304) 295개는 이름과 설명이 모두 `衣服`(한국어 "의복")이다.
- 구분 값은 의복 표(`g.conf.clothItem`, `ConfClothItemItem`)의 `sex`, `style`, `color`에 있다. 편집기는 이름을 `의복(<성별>) <style>-<color>`, 설명을 `의복 — <성별>성용, 모양 <style>, 색 <color>`로 보인다.
- 성별 값: <게임에서 확인한 결과. 예: 플레이어(여성)의 `propertyData.sex`가 2였으므로 2를 여성으로 보았다>
- 게임이 의복을 그림으로 어떻게 보여 주는지, `style`이 같은 것끼리 모양이 같은지는 확인하지 않았다.

### 구조

- 부품은 `UI/Widgets/`에 있다: `ListView<T>`(보이는 줄만 만든다), `SearchBox`, `DetailPane`, `Confirm`, `NumberField`, `AmountBar`.
- 검색어가 바뀌면 탭 전체가 아니라 목록만 다시 그린다(`ListView.SetItems`). 탭을 다시 그리면 검색 칸이 지워져 포커스를 잃는다.
- 숫자 칸의 Enter는 입력 칸의 끝맺음 이벤트 안에서 Enter 키인지 확인해 구분한다. 포커스를 잃어서 온 끝맺음은 적용하지 않는다.
- 탭의 하위 화면은 `EditorTab.Push`/`Pop`으로 쌓는다. 묶음 C·D의 "목록 → 세부 → 개별 가져오기"도 이 방식에 얹는다.

### 검증 (게임에서 실행, `tools/research/ui-check.ps1`)

| 부분 | 보는 것 | 결과 |
|---|---|---|
| `items` | 자원 탭의 목록·즉시 검색·이동 줄·상세, 지급 뒤의 위치, 의복 이름, 가방 칸 한 번에 지정 | <PASS 수> |
| `numbers` | 능력치 값 칸의 Enter, Enter 없이는 적용 안 됨, 범위 밖·빈 값·음수, 수량의 Enter, 날짜 칸의 Enter | <PASS 수> |
| `people` | NPC 목록과 이름 검색 | <PASS 수> |
| `history` | 지난 실행의 기록, 최신이 위, 검색, 상세 | <PASS 수> |
| `confirm` | 확인이 탭·하위 화면·대상 변경으로 풀림, 하위 화면이 닫힘, 공법·기예의 목록과 검색 | <PASS 수> |
| `base`, `tabs`, `order`, `move` 계열, `status`, `battle` | 1차의 것이 그대로인지 | <PASS 수> |
| `datebag-check.ps1`의 `time`·`freeze`·`bag` | 날짜와 가방이 그대로인지 | <결과> |
| 공법·운명·기예의 실제 동작 | 배우기, 운명 추가와 삭제, 서적 배우기와 잊기의 결과 문장 | <결과 문장> |
| 배포 구성 | 경고·오류 0, DLL 하나, 편집기가 열린다 | <결과> |

### 구현하며 고친 것

<계획과 달라진 것. 없으면 "없음">

### 확인하지 못한 것, 한계

- 한글 입력기로 검색 칸에 치는 동안의 동작(도구로는 영문·숫자 키와 값 넣기로 시험했다). <사용자가 확인했으면 그 결과>
- 실제 마우스 휠과 스크롤 막대 끌기(도구의 스크롤 이벤트로 시험했다).
- 목록의 줄을 계속 다시 만드는 방식이 스크롤 막대를 빠르게 끌 때 끊기는지.
- 변경 이력 파일이 2,000줄을 넘을 때 그 앞의 기록은 보이지 않는다(파일에는 남아 있다).
```

- [ ] **Step 4: `docs/06-handoff.md`를 갱신한다**

- 1절: "직전 작업"을 "묶음 B를 끝냈다(05 문서 26·27절). 다음은 묶음 C다(8.4절)"로, 설치 시각을 이번 설치로.
- 2절의 표: 능력치(저장값 칸에 직접 입력), 자원(가방 칸 한 번에 지정, 의복 구분), 안전(변경 이력 전체 보기와 검색) 줄의 기능 설명과 "실험 기록" 열에 26·27절을 보탠다. "공통 규칙"의 두 번 누르기 줄에 "확인 대기는 창 전체에 하나이고 다른 동작을 하면 풀린다"를 보탠다.
- 3절: `UI/` 줄의 `Widgets/` 설명을 "공통 부품: `ListView`, `SearchBox`, `DetailPane`, `Confirm`, `NumberField`, `AmountBar`"로, 05 문서의 절 범위를 1~27절로.
- 5절: `ui-check.ps1`의 부분 목록에 `items`, `numbers`, `people`, `history`, `confirm`을 보태고, "`items`는 가방 추가 칸을 바꾼다(회차의 `-End`가 되돌린다). `history`는 `items` 뒤에 돌린다"를 적는다.
- 7절의 "알려진 한계" 표: "한글 입력기(IME)로 검색 칸 입력"과 "실제 마우스 휠·막대 끌기" 줄의 기록 열을 27절로 바꾼다.
- 8.1절의 표: B를 "끝남. 스펙 `2026-10-13-ui-rework-design.md`, 계획 `2026-10-13-ui-rework-1-shell.md`·`-2-widgets.md`, 기록 05 문서 26·27절"로, C를 "**다음 차례.** 설계 시작 전"으로.
- 8.3절: 첫 부분을 "끝났다"로 줄이고, "화면을 새로 만들 때 지킬 것"으로 남긴다: 목록은 `ListView`, 검색은 `SearchBox`(목록만 다시 그린다), 확인은 `Confirm`, 숫자는 `NumberField`, 여러 단계의 화면은 `Push`/`Pop`, 목록 안의 버튼 이름에는 항목의 번호를 붙인다.
- 8.7절: 요청 6번의 후보 목록은 그대로 둔다(아이템·영석 줄이기).

- [ ] **Step 5: 문서를 커밋하고 `develop`에 병합한다**

```powershell
git -C E:\TlToybox add docs/05-editor-v0.1.md docs/06-handoff.md
git -C E:\TlToybox commit -m @'
docs: 공통 부품과 탭별 개선의 기록(05 문서 27절), 인계 문서 갱신

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
git -C E:\TlToybox switch develop
git -C E:\TlToybox merge --no-ff feat/ui-rework-2 -m @'
Merge branch 'feat/ui-rework-2' into develop

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
git -C E:\TlToybox status --short
```

Expected: 충돌 없이 병합되고 작업 트리가 깨끗하다.

- [ ] **Step 6: 배포 빌드를 게임에 설치한다**

게임이 꺼져 있어야 한다. 켜져 있으면 사용자에게 알리고 기다린다.

```powershell
pwsh -NoProfile -File E:\TlToybox\tools\install.ps1
```

Expected: 마지막에 `…\ModExportData\Mod_sWP6UL\ModCode\dll\MOD_sWP6UL.dll`과 오늘 날짜가 나온다.

- [ ] **Step 7: 사용자에게 알리고 직접 확인을 부탁한다**

알릴 것: 27절의 "바뀐 조작" 표, 검증 결과. 부탁할 것(도구로 시험할 수 없었다): **한글로 검색 칸에 입력**(치는 동안 목록이 걸러지는지, 조합 중인 글자가 끊기지 않는지), **마우스 휠과 스크롤 막대**로 긴 목록을 넘기는 느낌.
