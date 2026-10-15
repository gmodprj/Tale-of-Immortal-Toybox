# 기술 서적(비급) 지급 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 자원 탭에 `기술 서적` 그룹을 두어 무예/영술·절기·신법·심법·신통의 비급을 경지와 최소 품질을 정해 가방에 지급하고, 비급도 즐겨찾기에 넣게 한다.

**Architecture:** 비급 만들기는 새 서비스 `MartialBookService`가 맡는다. 공법 탭 기술 추가의 뽑기를 `SkillCatalog.RollValues`로 떼어 함께 쓴다. 즐겨찾기 줄은 아이템과 비급 두 종류가 되고(`FavoriteStore`), 자원 탭의 표는 아이템·기술·비급 즐겨찾기를 담는 공통 줄(`Row`)로 바뀐다. 기술 서적 그룹은 partial 파일 `ResourcesTab.Books.cs`에 둔다.

**Tech Stack:** C# 7.3 / .NET 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4, PowerShell 7 검증 스크립트, `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-15-martial-books-design.md`

## Global Constraints

- 브랜치 `feat/martial-books`(develop에서, 스펙 커밋 `e234a92`). 끝에서 `git merge --no-ff`로 `develop`에 병합하고 `tools/install.ps1`로 설치한다. `main`에는 병합하지 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 두 구성(개발, `-p:DevTools=false -o <임시 폴더>`) 모두 오류 0·경고 0, `bin\Release`에 `MOD_sWP6UL.dll` 하나.
- 게임 검증은 `pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서. 회차 이름은 `book-1015a`부터 차례로, 다시 쓰지 않는다. `-End`가 "SAVE RESTORED AND IDENTICAL". 회차와 검사는 한 번에 하나. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 고정본 인물은 결정(대경지 3)이고 가방에 비급 넷이 있다: `kEWxCN`(절기 23116, 경지 3, 품질 1), `fBfinb`(무예 44112, 경지 3), `Be94lN`(신법 510021, 경지 1), `iFyzgI`(무예 17111, 경지 1).
- `SkillCatalog.Kinds`의 순번: 0 무예/영술(`SkillLeft`), 1 절기(`SkillRight`), 2 신법(`Step`), 3 심법(`Ability`), 4 신통(`Ultimate`).
- 비급: 한 번에 최대 20권(`MartialBookService.MaxPerGive`), 한 권에 뽑기 최대 20000번(`RollMax`). 품질은 `MartialData.martialInfo.level`(1~6). 뽑기에서 끝내 나오지 않으면 옵션 없는 비급을 만들지 않고 멈춘다.
- 즐겨찾기 비급 줄: `book <분류 0~4> <기술 번호> <경지 1~10> <최소 품질 1~6> <수량 1~20>`. 같은 비급은 (기술 번호, 경지, 최소 품질). 아이템 줄과 따로 센다.
- 요소 이름: `ResGroupManual`, `ResKind0`~`5`, `ResGrade1`~`10`, `ResMinLevel1`~`6`, `ResBookFit`, `BookStar|BookName|BookGive|BookBag<기술 번호>`(`BookBag`은 가방 열의 글), `FavBookStar|FavBookName|FavBookAmount|FavBookGive<기술 번호>_<경지>_<최소 품질>`. 그대로 쓰는 이름: `ItemQuery`, `ItemTable*`, `ItemDetail`, `ResClear`, `ResAllGive`, `ResStar`·`ItemName`·`ItemGive`·`ResAmount<아이템 번호>`.
- 서비스는 화면을 모르고 결과를 문장으로 돌려준다. 쓰기 전 `SaveBackup.EnsureOnce()`, 넣은 뒤 다시 읽어 판정, 넣은 비급마다 `ChangeLog.Add`.
- 공법 탭 기술 추가의 동작(최대 3000번, 끝내 나오지 않으면 옵션 없는 기술)은 바꾸지 않는다.
- 문서에는 확인한 이름만, 확인하지 못한 것은 "미확인"과 사유. `g`는 게임의 전역 클래스이므로 지역 변수 이름으로 쓰지 않는다.

## Review Focus

1. 즐겨찾기에 있는 비급의 기술이 게임에 없다(모드를 뺀 경우) → 필터와 상관없이 보이고, 지급이 꺼지며, ★로 뺄 수 있다. (Task 3 `books-ui`의 "a missing skill book favorite…")
2. 여러 권을 주는 도중 가방이 찬다 → 멈추고 넣은 권수를 알리며 가방에는 그 권수만 늘어난다. (Task 1 `books`의 "a full bag stops…")
3. 지급 수량이 20보다 크다 → 20권만 만들고 그 안내를 붙인다. (Task 1 `books`의 "more than twenty…")
4. 같은 기술을 다른 경지·최소 품질로 즐겨찾기 → 서로 다른 줄로 남고, 같은 조합은 거부된다. (Task 2 `favstore`의 "the same book…", "another minimum quality…")
5. 기술 서적 그룹에서 경지를 바꾼 뒤 `초기화` → 경지가 지금 경지(3)로, 최소 품질 1, `이 경지만` 켬으로 돌아간다. (Task 3 `books-ui`의 "clear brings back…")

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `src/ModCode/ModMain/Services/SkillCatalog.cs` | 수정 | 뽑기를 `RollValues`로 떼고 `LearnNow`가 쓴다. `Get(kindIndex, baseId)` |
| `src/ModCode/ModMain/Services/MartialBookService.cs` | 새로 | 비급 만들기(`Make`), 가방의 기술별 비급 수(`CountAllInBag`, `CountInBag`), 아래 칸의 안내(`Describe`) |
| `src/ModCode/ModMain/Services/FavoriteStore.cs` | 전체 교체 | 비급 줄 읽기·쓰기, 비급 즐겨찾기의 넣기·빼기·수량 |
| `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs` | 전체 교체 | 공통 줄 `Row`, 즐겨찾기 보기·모두 지급의 비급 줄, 그룹 목록에 기술 서적 |
| `src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs` | 새로 | 기술 서적 그룹(종류 칩, 필터 두 줄, 표, 지급, 별), 비급 즐겨찾기 줄 |
| `src/ModCode/ModMain/Dev/DevCommandsResources.cs` | 수정 | `rbook`, `rbooks`, `rbookstudy`, `rskill`, `rfav addbook|setbook|delbook`, `rfav`의 비급 줄 |
| `tools/research/ui-check.ps1` | 수정 | 부분 `books`, `books-ui`(새), `favstore`(더함) |
| `docs/05-editor-v0.1.md`, `docs/06-handoff.md` | 수정 | 39절, 인계 문서 |

---

### Task 1: 비급 만들기 서비스

**Files:**
- Create: `src/ModCode/ModMain/Services/MartialBookService.cs`
- Modify: `src/ModCode/ModMain/Services/SkillCatalog.cs`, `src/ModCode/ModMain/Dev/DevCommandsResources.cs`, `tools/research/ui-check.ps1`(부분 `books`)

**Interfaces:**
- Produces:
  - `SkillCatalog.RollValues(Kind kind, int baseId, int grade, int maxTries, Func<DataProps.MartialData, bool> accept) : Il2CppStructArray<int>`(없으면 null), `SkillCatalog.Get(int kindIndex, int baseId) : Entry`(없으면 null)
  - `MartialBookService.MaxPerGive = 20`, `RollMax = 20000`, `Make(WorldUnitBase unit, int kindIndex, SkillCatalog.Entry entry, int grade, int minLevel, int count, out string text) : int`(넣은 권수), `CountAllInBag(WorldUnitBase unit) : Dictionary<int,int>`(기술 번호 → 가방의 비급 수), `CountInBag(WorldUnitBase unit, int baseId) : int`, `Describe(SkillCatalog.Entry entry, int grade, int minLevel, int count) : string`
  - `Make`의 문장: `<기술명> 비급 n권: <이름>(품질 l), …` + 20권을 넘겨 청했으면 ` (한 번에 20권까지 줍니다)` + 멈췄으면 `. 멈춤: <이유>` + `.`. 이유: `경지 g에서 품질 m 이상이 나오지 않았습니다`, `가방에 들어가지 않았습니다(빈 칸을 확인하세요)`, `게임이 받지 않았습니다(<예외 형식>)`. 경지 범위 밖: `<기술명> 비급: <SkillCatalog.BlockReason의 문장>.`
  - `Describe`: `지급하면: 경지 g, 품질 m 이상의 비급 n권(한 권마다 옵션을 따로 뽑습니다)` + 20권 초과면 `. 한 번에 20권까지 줍니다` + `.`, 범위 밖이면 `지급할 수 없습니다: <BlockReason>.`
  - 변경 이력 줄: 항목 `비급 <이름>(<기술 번호>)`, 이전 `-`, 이후 `경지 g 품질 l`
  - 개발 명령: `rbook <분류> <기술 번호> <경지> <최소 품질> <권수>` → `made=<수> | <Make의 문장>`, `rbooks [기술 번호]` → `count=<수> items=<soleID>:<기술 번호>:<경지>:<품질>:<이름>,…`, `rbookstudy <soleID>` → `can=<IsCreateAction>`, `rskill <분류> <기술 번호>` → `min=<경지> max=<경지> name=<이름>`

- [ ] **Step 1: 검사를 먼저 쓴다**

`tools/research/ui-check.ps1`의 부분 `'favstore' {` 바로 앞에 넣는다:

```powershell
  'books' {
    # 기술 서적(비급) 지급 스펙 5절: 서비스. 가방을 채우는 검사가 끝에 있으므로 회차의 마지막에 돌린다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "$key=([^\s|]+)") { $Matches[1] } else { '' } }
    function BookCount { [int](Field (Cmd 'rbooks 23116' 300) 'count') }
    function BookLines { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ -match '비급 ' }).Count }
    $skill = Cmd 'rskill 1 23116' 300
    Check 'the skill table has 23116' ($skill -match 'min=\d+ max=\d+') $skill
    $min = [int](Field $skill 'min'); $max = [int](Field $skill 'max')
    $grade = [Math]::Max($min, [Math]::Min($max, 3))
    $before = BookCount
    $logged = BookLines
    $o = Cmd "rbook 1 23116 $grade 1 1" 3000
    $after = Cmd 'rbooks 23116' 300
    Check 'one book goes into the bag' ($o -match 'made=1 ' -and (BookCount) -eq $before + 1) "$o | $after"
    Check 'the new book is that skill and grade' ($after -match ":23116:$($grade):[1-6]:") $after
    $o = Cmd "rbook 1 23116 $grade 6 3" 20000
    $books = Cmd 'rbooks 23116' 300
    $six = @([regex]::Matches($books, ":23116:$($grade):6:([^,\s]+)") | ForEach-Object { $_.Groups[1].Value })
    Check 'three books of quality 6' ($o -match 'made=3 ' -and $six.Count -ge 3) "$o | $books"
    Check 'their options are not all the same' (@($six | Select-Object -Unique).Count -ge 2) ($six -join ' ')
    $out = if ($max -lt 10) { $max + 1 } else { $min - 1 }
    $count = BookCount
    $o = Cmd "rbook 1 23116 $out 1 1" 1000
    Check 'a grade outside the range is refused' ($o -match 'made=0 ' -and (BookCount) -eq $count) $o
    # Review Focus 3: 20권을 넘겨 청하면 20권과 안내
    $count = BookCount
    $o = Cmd "rbook 1 23116 $grade 1 25" 20000
    Check 'more than twenty gives twenty with a notice' ($o -match 'made=20 ' -and $o -match '20권까지' -and (BookCount) -eq $count + 20) $o
    Check 'the change log has a line per book' ((BookLines) -eq $logged + 24) "before=$logged after=$(BookLines)"
    $mine = @([regex]::Matches((Cmd 'rbooks 23116' 300), '(?:items=|,)([^:,]+):23116:') | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -ne 'kEWxCN' })
    $study = if ($mine.Count -gt 0) { Cmd "rbookstudy $($mine[0])" 300 } else { '' }
    Check 'the game study accepts a made book' ($study -match 'can=0') "$($mine -join ',') | $study"
    # Review Focus 2: 가방이 찬 채로 청하면 멈추고 넣은 권수만 늘어난다
    Cmd 'gridfill 600 5' 6000 | Out-Null
    Check 'the bag is full' ((Cmd 'gridfill 1 5' 1500) -match 'given=0 ')
    $count = BookCount
    $o = Cmd "rbook 1 23116 $grade 1 5" 6000
    $made = [int](Field $o 'made')
    Check 'a full bag stops with the number put in' ($o -match '가방에 들어가지 않았습니다' -and (BookCount) -eq $count + $made) $o
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin book-1015a -Fixture base
pwsh tools/research/ui-check.ps1 -Part books
pwsh tools/DevRemote/session.ps1 -End book-1015a
```

Expected: `rskill`이 `UNKNOWN`이라 처음 줄부터 FAIL이 여럿, 마지막 줄 `FAILS=`가 0보다 크다.

- [ ] **Step 3: 뽑기를 떼어 낸다**

`src/ModCode/ModMain/Services/SkillCatalog.cs`:

`using` 줄 맨 앞에 `using System;`을 더한다.

`LearnNow` 안의 다음 블록을:

```csharp
            Il2CppStructArray<int> values = null;
            // 그 기술 하나만 나오는 묶음이 있으면(입문 무예·영술 등) 그것을 먼저 쓴다.
            int own = roll ? OwnCombine(kind, entry.BaseId) : 0;
            if (own != 0)
            {
                Il2CppStructArray<int> fixedRoll = RewardTool.GetSkillReward(own, grade, null);
                if (fixedRoll != null && Contains(fixedRoll, entry.BaseId))
                {
                    values = fixedRoll;
                }
            }
            for (int tries = 0; roll && values == null && tries < RollMax; tries++)
            {
                Il2CppStructArray<int> rolled = Roll(kind, grade);
                if (rolled != null && Contains(rolled, entry.BaseId)
                    && DataProps.PropsData.NewMartial(kind.Type, rolled).To<DataProps.MartialData>().baseID == entry.BaseId)
                {
                    values = rolled;
                    break;
                }
            }
```

아래 한 줄로 바꾼다:

```csharp
            Il2CppStructArray<int> values = roll ? RollValues(kind, entry.BaseId, grade, RollMax, null) : null;
```

`OwnCombine` 메서드 바로 아래에 넣는다:

```csharp
        // 그 기술이 나오고 accept를 통과하는 뽑기 값. 그 기술 하나만 나오는 묶음이 있으면(입문 무예·영술 등) 먼저 쓰고,
        // 그다음 분류 전체의 묶음에서 maxTries번까지 다시 뽑는다. 없으면 null. 기술 추가(accept null)와 비급 지급이 함께 쓴다.
        internal static Il2CppStructArray<int> RollValues(Kind kind, int baseId, int grade, int maxTries, Func<DataProps.MartialData, bool> accept)
        {
            int own = OwnCombine(kind, baseId);
            if (own != 0)
            {
                Il2CppStructArray<int> fixedRoll = RewardTool.GetSkillReward(own, grade, null);
                if (fixedRoll != null && Contains(fixedRoll, baseId) && (accept == null || Accepts(kind, fixedRoll, baseId, accept)))
                {
                    return fixedRoll;
                }
            }
            for (int tries = 0; tries < maxTries; tries++)
            {
                Il2CppStructArray<int> rolled = Roll(kind, grade);
                if (rolled != null && Contains(rolled, baseId) && Accepts(kind, rolled, baseId, accept))
                {
                    return rolled;
                }
            }
            return null;
        }

        private static bool Accepts(Kind kind, Il2CppStructArray<int> values, int baseId, Func<DataProps.MartialData, bool> accept)
        {
            DataProps.MartialData martial = DataProps.PropsData.NewMartial(kind.Type, values).To<DataProps.MartialData>();
            return martial.baseID == baseId && (accept == null || accept(martial));
        }

        // 그 분류의 그 번호 기술. 없으면 null(즐겨찾기의 비급 줄이 쓴다).
        public static Entry Get(int kindIndex, int baseId)
        {
            if (kindIndex < 0 || kindIndex >= Kinds.Length)
            {
                return null;
            }
            return All(kindIndex).Find(e => e.BaseId == baseId);
        }
```

기술 추가의 뽑기는 바뀌지 않는다: 그 기술만의 묶음은 `Contains`만 보고(accept가 null), 분류 묶음은 `Contains`와 `baseID`를 본다.

- [ ] **Step 4: 서비스를 쓴다**

`src/ModCode/ModMain/Services/MartialBookService.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 기술 서적(비급) 지급. 공법 탭의 기술 추가와 같은 게임 뽑기로 값을 얻어, 배우게 하는 대신 가방에 비급으로 넣는다.
    // 근거: docs/superpowers/specs/2026-10-15-martial-books-design.md 2·3.1절(만든 비급은 게임의 비급과 같은 모양이고 게임의 학습이 받아들인다).
    internal static class MartialBookService
    {
        public const int MaxPerGive = 20;
        public const int RollMax = 20000;

        // 가방의 기술별 비급 수(기술 번호 → 권수).
        public static Dictionary<int, int> CountAllInBag(WorldUnitBase unit)
        {
            var counts = new Dictionary<int, int>();
            var all = unit.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].propsType != DataProps.PropsDataType.Martial)
                {
                    continue;
                }
                try
                {
                    int baseId = all[i].To<DataProps.MartialData>().baseID;
                    int n;
                    counts.TryGetValue(baseId, out n);
                    counts[baseId] = n + 1;
                }
                catch (Exception)
                {
                    // 게임이 읽지 못하는 비급은 세지 않는다.
                }
            }
            return counts;
        }

        public static int CountInBag(WorldUnitBase unit, int baseId)
        {
            int n;
            return CountAllInBag(unit).TryGetValue(baseId, out n) ? n : 0;
        }

        // 아래 칸의 "지급하면 …" 또는 만들 수 없는 이유.
        public static string Describe(SkillCatalog.Entry entry, int grade, int minLevel, int count)
        {
            string blocked = SkillCatalog.BlockReason(entry, grade);
            if (blocked != null)
            {
                return "지급할 수 없습니다: " + blocked + ".";
            }
            int n = Math.Max(1, Math.Min(count, MaxPerGive));
            return "지급하면: 경지 " + grade + ", 품질 " + minLevel + " 이상의 비급 " + n + "권(한 권마다 옵션을 따로 뽑습니다)"
                + (count > MaxPerGive ? ". 한 번에 " + MaxPerGive + "권까지 줍니다" : "") + ".";
        }

        // 비급을 count권(최대 20) 만들어 가방에 넣고 넣은 권수를 돌려준다. 한 권마다 따로 뽑으므로 옵션이 서로 다르다.
        // 게임이 던지는 예외는 여기서 잡는다(버튼 콜백 밖으로 나가면 결과 문장 없이 사라진다).
        public static int Make(WorldUnitBase unit, int kindIndex, SkillCatalog.Entry entry, int grade, int minLevel, int count, out string text)
        {
            string label = entry.Name + " 비급";
            string blocked = SkillCatalog.BlockReason(entry, grade);
            if (blocked != null)
            {
                text = label + ": " + blocked + ".";
                return 0;
            }
            if (count < 1)
            {
                text = label + ": 권수는 1 이상이어야 합니다.";
                return 0;
            }
            if (minLevel < 1 || minLevel > 6)
            {
                text = label + ": 최소 품질은 1~6입니다.";
                return 0;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return 0;
            }
            int wanted = Math.Min(count, MaxPerGive);
            SkillCatalog.Kind kind = SkillCatalog.Kinds[kindIndex];
            DataProps bag = unit.data.unitData.propData;
            var made = new List<string>();
            string stop = null;
            for (int i = 0; i < wanted && stop == null; i++)
            {
                try
                {
                    Il2CppStructArray<int> values = SkillCatalog.RollValues(kind, entry.BaseId, grade, RollMax, m => m.martialInfo.level >= minLevel);
                    if (values == null)
                    {
                        stop = "경지 " + grade + "에서 품질 " + minLevel + " 이상이 나오지 않았습니다";
                        break;
                    }
                    DataProps.PropsData props = DataProps.PropsData.NewMartial(kind.Type, values);
                    int before = bag.allProps.Count;
                    bag.AddProps(props);
                    bool ok = bag.allProps.Count == before + 1;
                    string name = entry.Name;
                    int level = 0;
                    try
                    {
                        DataProps.MartialData martial = props.To<DataProps.MartialData>();
                        name = MartialService.Name(martial);
                        level = martial.martialInfo.level;
                    }
                    catch (Exception)
                    {
                        // 이름과 품질을 읽지 못해도 넣은 것은 센다.
                    }
                    ChangeLog.Add(EditService.NameOf(unit), "비급 " + name + "(" + entry.BaseId + ")", "-", "경지 " + grade + " 품질 " + level, ok);
                    if (!ok)
                    {
                        stop = "가방에 들어가지 않았습니다(빈 칸을 확인하세요)";
                        break;
                    }
                    made.Add(name + "(품질 " + level + ")");
                }
                catch (Exception e)
                {
                    stop = "게임이 받지 않았습니다(" + e.GetType().Name + ")";
                }
            }
            text = label + " " + made.Count + "권" + (made.Count > 0 ? ": " + string.Join(", ", made.ToArray()) : "")
                + (count > MaxPerGive ? " (한 번에 " + MaxPerGive + "권까지 줍니다)" : "")
                + (stop != null ? ". 멈춤: " + stop : "") + ".";
            return made.Count;
        }
    }
}
```

- [ ] **Step 5: 개발 명령**

`src/ModCode/ModMain/Dev/DevCommandsResources.cs`:

머리 주석의 `rfav` 줄들 아래에 넣는다:

```csharp
    //   rbook <분류> <기술 번호> <경지> <최소 품질> <권수> : made=<수> | <MartialBookService.Make의 문장>
    //   rbooks [기술 번호]   : 가방의 비급 count=<수> items=<soleID>:<기술 번호>:<경지>:<품질>:<이름>,…
    //   rbookstudy <soleID>  : 게임의 학습 행동(UnitActionPropMartialStudy)이 그 비급을 받아들이는지 can=<IsCreateAction>
    //   rskill <분류> <기술 번호> : min=<경지> max=<경지> name=<이름>
```

`switch`의 `case "rfav":` 아래에 넣는다:

```csharp
                case "rbook":
                    {
                        int kindIndex = int.Parse(p[1]);
                        SkillCatalog.Entry entry = SkillCatalog.Get(kindIndex, int.Parse(p[2]));
                        if (entry == null)
                        {
                            return "no skill " + p[2];
                        }
                        string text;
                        int made = MartialBookService.Make(g.world.playerUnit, kindIndex, entry, int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]), out text);
                        return "made=" + made + " | " + text;
                    }
                case "rbooks":
                    return Books(p.Length > 1 ? int.Parse(p[1]) : 0);
                case "rbookstudy":
                    {
                        WorldUnitBase me = g.world.playerUnit;
                        var all = me.data.unitData.propData.allProps;
                        for (int i = 0; i < all.Count; i++)
                        {
                            if (all[i].soleID == p[1])
                            {
                                return "can=" + me.IsCreateAction(new UnitActionPropMartialStudy(all[i]), false);
                            }
                        }
                        return "no prop " + p[1];
                    }
                case "rskill":
                    {
                        SkillCatalog.Entry entry = SkillCatalog.Get(int.Parse(p[1]), int.Parse(p[2]));
                        return entry == null ? "no skill " + p[2] : "min=" + entry.GradeMin + " max=" + entry.GradeMax + " name=" + entry.Name;
                    }
```

`Favorites` 메서드 아래에 넣는다:

```csharp
        private static string Books(int baseId)
        {
            var items = new List<string>();
            var all = g.world.playerUnit.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].propsType != DataProps.PropsDataType.Martial)
                {
                    continue;
                }
                try
                {
                    DataProps.MartialData martial = all[i].To<DataProps.MartialData>();
                    if (baseId == 0 || martial.baseID == baseId)
                    {
                        items.Add(all[i].soleID + ":" + martial.baseID + ":" + martial.grade + ":" + martial.martialInfo.level + ":" + MartialService.Name(martial));
                    }
                }
                catch (System.Exception e)
                {
                    items.Add(all[i].soleID + ":ERR " + e.GetType().Name);
                }
            }
            return "count=" + items.Count + " items=" + string.Join(",", items.ToArray());
        }
```

- [ ] **Step 6: 두 구성으로 빌드한다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo -p:DevTools=false -o "$TEMP/tltoybox-release-check"
ls src/ModCode/ModMain/bin/Release
```

Expected: 두 번 모두 경고 0·오류 0, `MOD_sWP6UL.dll` 하나.

- [ ] **Step 7: 검사가 통과하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin book-1015b -Fixture base
pwsh tools/research/ui-check.ps1 -Part books
pwsh tools/DevRemote/session.ps1 -End book-1015b
pwsh tools/DevRemote/session.ps1 -Begin book-1015c -Fixture base
pwsh tools/research/ui-check.ps1 -Part actions
pwsh tools/DevRemote/session.ps1 -End book-1015c
```

Expected: `books` `FAILS=0`, `actions` `FAILS=0`(공법 탭 기술 추가가 떼어 낸 뽑기로 그대로 동작한다). `books`의 rskill 출력에서 23116의 경지 범위를 05 문서 39절에 적는다.

"three books of quality 6"이 실패하고 결과 문장이 "나오지 않았습니다"이면, 23116의 경지 3에서 품질 6이 20000번 안에 나오지 않은 것이다(실험에서는 3000번에 7번 나왔다). 그때는 멈추고 사용자에게 알린다(스펙 8절의 위험이 실제로 일어난 것).

- [ ] **Step 8: 커밋**

```bash
git add src/ModCode/ModMain/Services/SkillCatalog.cs src/ModCode/ModMain/Services/MartialBookService.cs src/ModCode/ModMain/Dev/DevCommandsResources.cs tools/research/ui-check.ps1
git commit -m "feat: 비급 만들기 서비스 — 최소 품질까지 다시 뽑아 가방에, 기술 추가와 뽑기 함수를 함께 씀

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 즐겨찾기의 비급 줄

**Files:**
- Modify: `src/ModCode/ModMain/Services/FavoriteStore.cs`(전체 교체), `src/ModCode/ModMain/Dev/DevCommandsResources.cs`(`rfav`), `tools/research/ui-check.ps1`(부분 `favstore`)

**Interfaces:**
- Consumes: Task 1의 `SkillCatalog.Get`, `MartialBookService.MaxPerGive`.
- Produces:
  - `FavoriteStore.Favorite`에 `bool Book; int BookKind; int Grade; int MinLevel;`(아이템이면 `Book` false, `Id`는 아이템 번호 또는 기술 번호)
  - `FavoriteStore.HasBook(int baseId, int grade, int minLevel) : bool`, `BookAmountOf(int baseId, int grade, int minLevel) : int`(없으면 0), `AddBook(int kindIndex, int baseId, int grade, int minLevel, int amount, out string text) : bool`, `RemoveBook(int baseId, int grade, int minLevel, out string text) : bool`, `SetBookAmount(int baseId, int grade, int minLevel, int amount, out string text) : bool`
  - 아이템용 `Has`, `AmountOf`, `Add`, `Remove`, `SetAmount`는 아이템 줄만 본다(시그니처 그대로).
  - 비급 수량 규칙 문장: `즐겨찾기: 비급 수량은 1~20 사이의 숫자로 입력하세요.`
  - 비급 이름: `<기술명> 비급 (경지 g, 품질 m 이상)`, 기술이 게임에 없으면 `게임에 없는 기술 (<번호>) (경지 g, 품질 m 이상)`
  - `rfav`의 비급 항목: `book:<분류>:<기술 번호>:<경지>:<최소 품질>:<수량>[:missing]`. `rfav addbook <분류> <기술 번호> <경지> <최소 품질> <수량>`, `rfav setbook <기술 번호> <경지> <최소 품질> <수량>`, `rfav delbook <기술 번호> <경지> <최소 품질>`

- [ ] **Step 1: 검사를 먼저 쓴다**

`tools/research/ui-check.ps1`의 부분 `'favstore'`에서 `Check 'removing works' …` 줄 바로 아래에 넣는다:

```powershell
    # 비급 줄(기술 서적 스펙 3.3절). Review Focus 4: 같은 기술이라도 경지·최소 품질이 다르면 다른 즐겨찾기
    $o = Cmd 'rfav addbook 1 23116 3 6 4' 300
    Check 'a book favorite is written as a book line' ($o -match 'book:1:23116:3:6:4' -and (FavText) -match "(?m)^book 1 23116 3 6 4\r?$") $o
    $o = Cmd 'rfav addbook 1 23116 3 6 2' 300
    Check 'the same book (skill, grade, quality) is refused' ($o -match '이미' -and $o -match 'book:1:23116:3:6:4') $o
    $o = Cmd 'rfav addbook 1 23116 3 5 2' 300
    Check 'another minimum quality is another favorite' ($o -match 'book:1:23116:3:6:4' -and $o -match 'book:1:23116:3:5:2') $o
    $o = Cmd 'rfav setbook 23116 3 6 21' 300
    Check 'a book amount above 20 is refused' ($o -match '1~20' -and $o -match 'book:1:23116:3:6:4') $o
    $o = Cmd 'rfav delbook 23116 3 5' 300
    Check 'a book favorite can be removed' ($o -notmatch 'book:1:23116:3:5' -and (FavText) -notmatch 'book 1 23116 3 5') $o
    Check 'item lines are kept beside book lines' ($o -match 'items=1011011:25,book:1:23116:3:6:4') $o
```

같은 부분의 마지막 `Check 'no callback exception in the log' ((Traps) -eq $traps)` 줄 바로 앞에 넣는다:

```powershell
    # 비급 줄의 읽기: 수량은 20으로 맞추고, 분류가 범위 밖이거나 칸 수가 다른 줄은 건너뛴다. 아이템 줄과 섞여도 순서를 지킨다
    $keep = FavText
    [IO.File]::WriteAllText($fav, "1011011 5`nbook 1 23116 3 6 25`nbook 9 23116 3 6 1`nbook 1 23116`n1011012 8`n", [Text.UTF8Encoding]::new($false))
    $o = Cmd 'rfav reload' 300
    Check 'book lines are read, capped and checked' ($o -match 'count=3 skipped=2 items=1011011:5,book:1:23116:3:6:20,1011012:8') $o
    [IO.File]::WriteAllText($fav, $keep, [Text.UTF8Encoding]::new($false))
    Cmd 'rfav reload' 300 | Out-Null
```

(파일을 원래 내용으로 되돌리므로 `favstore-restart`의 기대값은 그대로다.)

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin book-1015d -Fixture base
pwsh tools/research/ui-check.ps1 -Part favstore
pwsh tools/DevRemote/session.ps1 -End book-1015d
```

Expected: 새 줄이 FAIL(`rfav addbook`이 알 수 없는 동작이라 상태만 나오고, `book` 줄은 깨진 줄로 건너뛰어진다). 기존 줄은 PASS.

- [ ] **Step 3: 저장소를 고친다**

`src/ModCode/ModMain/Services/FavoriteStore.cs`를 통째로 바꾼다:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 자원 탭의 즐겨찾기: 아이템 번호와 지급 수량, 또는 기술 서적(비급)의 분류·기술·경지·최소 품질과 권수. 모든 세이브에 공통이고 TlToybox\favorites.txt에 둔다.
    // 쓰기는 임시 파일에 쓴 뒤 바꿔치기한다. 깨진 줄이 있던 파일은 처음 덮어쓰기 전에 .bak으로 남긴다.
    // 읽지 못한 파일은 덮어쓰지 않는다. 근거: docs/superpowers/specs/2026-10-15-resources-tab-design.md 3.2·3.3절, 6절,
    // 비급 줄은 2026-10-15-martial-books-design.md 3.3절.
    internal static class FavoriteStore
    {
        public sealed class Favorite
        {
            public int Id;          // 아이템 번호, 비급이면 기술 번호
            public int Amount;
            public bool Book;       // 기술 서적(비급) 줄
            public int BookKind;    // SkillCatalog.Kinds의 순번(0~4)
            public int Grade;       // 비급의 경지(1~10)
            public int MinLevel;    // 비급의 최소 품질(1~6)

            public bool Same(Favorite other)
            {
                return Book == other.Book && Id == other.Id && (!Book || (Grade == other.Grade && MinLevel == other.MinLevel));
            }

            public Favorite Copy()
            {
                return new Favorite { Id = Id, Amount = Amount, Book = Book, BookKind = BookKind, Grade = Grade, MinLevel = MinLevel };
            }
        }

        public const int MaxAmount = AmountBar.Max;
        private const string FileName = "favorites.txt";
        private static readonly string AmountRule = "즐겨찾기: 수량은 1~" + MaxAmount + " 사이의 숫자로 입력하세요.";
        private static readonly string BookAmountRule = "즐겨찾기: 비급 수량은 1~" + MartialBookService.MaxPerGive + " 사이의 숫자로 입력하세요.";

        private static List<Favorite> list;     // null이면 아직 읽지 않았다
        private static string readError;        // 파일을 읽지 못한 이유. 있으면 덮어쓰지 않는다
        private static bool keepOriginal;       // 깨진 줄이 있었다: 처음 덮어쓰기 전에 .bak으로 남긴다
        private static bool noticePending;      // 자원 탭에 들어올 때 한 번 알릴 것이 있다

        public static int SkippedLines { get; private set; }

        private static string FilePath
        {
            get { return Path.Combine(ChangeLog.DataDir, FileName); }
        }

        private static int Clamp(int value, int max)
        {
            return Math.Max(1, Math.Min(max, value));
        }

        // 한 줄을 읽는다: "<번호> <수량>" 또는 "book <분류> <기술 번호> <경지> <최소 품질> <수량>". 읽지 못하면 null.
        private static Favorite Parse(string[] parts)
        {
            int id, amount;
            if (parts.Length == 2 && int.TryParse(parts[0], out id) && int.TryParse(parts[1], out amount))
            {
                return new Favorite { Id = id, Amount = Clamp(amount, MaxAmount) };
            }
            int kind, grade, minLevel;
            if (parts.Length == 6 && parts[0] == "book" && int.TryParse(parts[1], out kind) && int.TryParse(parts[2], out id)
                && int.TryParse(parts[3], out grade) && int.TryParse(parts[4], out minLevel) && int.TryParse(parts[5], out amount)
                && kind >= 0 && kind < SkillCatalog.Kinds.Length && grade >= 1 && grade <= 10 && minLevel >= 1 && minLevel <= 6)
            {
                return new Favorite { Book = true, BookKind = kind, Id = id, Grade = grade, MinLevel = minLevel, Amount = Clamp(amount, MartialBookService.MaxPerGive) };
            }
            return null;
        }

        private static void Load()
        {
            if (list != null)
            {
                return;
            }
            list = new List<Favorite>();
            readError = null;
            keepOriginal = false;
            SkippedLines = 0;
            try
            {
                if (File.Exists(FilePath))
                {
                    foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                    {
                        string trimmed = line.Trim();
                        if (trimmed.Length == 0 || trimmed[0] == '#')
                        {
                            continue;
                        }
                        Favorite favorite = Parse(trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                        if (favorite == null || list.Exists(f => f.Same(favorite)))
                        {
                            SkippedLines++;
                            continue;
                        }
                        list.Add(favorite);
                    }
                }
                keepOriginal = SkippedLines > 0;
                if (SkippedLines > 0)
                {
                    MelonLogger.Msg("[TlToybox] favorites: skipped " + SkippedLines + " lines in " + FileName);
                }
            }
            catch (Exception e)
            {
                list.Clear();
                SkippedLines = 0;
                readError = e.Message;
                MelonLogger.Msg("[TlToybox] favorites: could not read " + FileName + ": " + e.Message);
            }
            noticePending = SkippedLines > 0 || readError != null;
        }

        // 넣은 순서의 즐겨찾기(아이템 줄과 비급 줄이 섞여 있다). 고치지 말고 읽기만 한다.
        public static List<Favorite> Entries
        {
            get
            {
                Load();
                return list;
            }
        }

        public static bool Has(int id)
        {
            return Find(id) != null;
        }

        public static int AmountOf(int id)
        {
            Favorite favorite = Find(id);
            return favorite == null ? 0 : favorite.Amount;
        }

        private static Favorite Find(int id)
        {
            Load();
            return list.Find(f => !f.Book && f.Id == id);
        }

        public static bool HasBook(int baseId, int grade, int minLevel)
        {
            return FindBook(baseId, grade, minLevel) != null;
        }

        public static int BookAmountOf(int baseId, int grade, int minLevel)
        {
            Favorite favorite = FindBook(baseId, grade, minLevel);
            return favorite == null ? 0 : favorite.Amount;
        }

        private static Favorite FindBook(int baseId, int grade, int minLevel)
        {
            Load();
            return list.Find(f => f.Book && f.Id == baseId && f.Grade == grade && f.MinLevel == minLevel);
        }

        // 건너뛴 줄이나 읽기 오류가 있었으면 한 번만 안내 문장을 돌려준다(자원 탭에 들어올 때).
        public static string TakeNotice()
        {
            Load();
            if (!noticePending)
            {
                return null;
            }
            noticePending = false;
            return readError != null ? "즐겨찾기 파일을 읽지 못했습니다(" + readError + ")."
                : "즐겨찾기 파일에서 읽지 못한 줄 " + SkippedLines + "개를 건너뛰었습니다.";
        }

        // 개발 명령(rfav reset/reload)이 부른다: 다음에 쓸 때 파일을 다시 읽는다.
        internal static void Forget()
        {
            list = null;
            readError = null;
            keepOriginal = false;
            noticePending = false;
            SkippedLines = 0;
        }

        // 읽기 오류가 있었으면 바꾸기 전에 다시 읽는다. 잠금이 풀렸는데 빈 목록으로 판단하면 같은 번호가 두 줄이 된다(검토에서 나온 것).
        private static void Recover()
        {
            Load();
            if (readError != null)
            {
                list = null;
                Load();
            }
        }

        private static string Label(int id)
        {
            ItemCatalog.Entry entry = ItemCatalog.Get(id);
            return entry != null ? entry.Name : "게임에 없는 아이템 (" + id + ")";
        }

        public static string BookLabel(int kindIndex, int baseId, int grade, int minLevel)
        {
            SkillCatalog.Entry entry = SkillCatalog.Get(kindIndex, baseId);
            return (entry != null ? entry.Name + " 비급" : "게임에 없는 기술 (" + baseId + ")") + " (경지 " + grade + ", 품질 " + minLevel + " 이상)";
        }

        public static bool Add(int id, int amount, out string text)
        {
            if (amount < 1 || amount > MaxAmount)
            {
                text = AmountRule;
                return false;
            }
            Recover();
            if (Has(id))
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 이미 있습니다.";
                return false;
            }
            string error;
            if (!Change(next =>
            {
                if (!next.Exists(f => !f.Book && f.Id == id))
                {
                    next.Add(new Favorite { Id = id, Amount = amount });
                }
            }, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에 넣었습니다: " + Label(id) + " x" + amount;
            return true;
        }

        public static bool Remove(int id, out string text)
        {
            Recover();
            if (!Has(id))
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 즐겨찾기에 없습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.RemoveAll(f => !f.Book && f.Id == id), out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에서 뺐습니다: " + Label(id);
            return true;
        }

        public static bool SetAmount(int id, int amount, out string text)
        {
            if (amount < 1 || amount > MaxAmount)
            {
                text = AmountRule;
                return false;
            }
            Recover();
            int old = AmountOf(id);
            if (old == 0)
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 즐겨찾기에 없습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.Find(f => !f.Book && f.Id == id).Amount = amount, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기 수량: " + Label(id) + " " + old + " -> " + amount;
            return true;
        }

        public static bool AddBook(int kindIndex, int baseId, int grade, int minLevel, int amount, out string text)
        {
            string label = BookLabel(kindIndex, baseId, grade, minLevel);
            if (amount < 1 || amount > MartialBookService.MaxPerGive)
            {
                text = BookAmountRule;
                return false;
            }
            if (kindIndex < 0 || kindIndex >= SkillCatalog.Kinds.Length || grade < 1 || grade > 10 || minLevel < 1 || minLevel > 6)
            {
                text = "즐겨찾기: " + label + "의 분류·경지·최소 품질이 범위 밖입니다.";
                return false;
            }
            Recover();
            if (HasBook(baseId, grade, minLevel))
            {
                text = "즐겨찾기: " + label + "은(는) 이미 있습니다.";
                return false;
            }
            string error;
            if (!Change(next =>
            {
                if (!next.Exists(f => f.Book && f.Id == baseId && f.Grade == grade && f.MinLevel == minLevel))
                {
                    next.Add(new Favorite { Book = true, BookKind = kindIndex, Id = baseId, Grade = grade, MinLevel = minLevel, Amount = amount });
                }
            }, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에 넣었습니다: " + label + " x" + amount;
            return true;
        }

        public static bool RemoveBook(int baseId, int grade, int minLevel, out string text)
        {
            Recover();
            Favorite favorite = FindBook(baseId, grade, minLevel);
            if (favorite == null)
            {
                text = "즐겨찾기: 그 비급(" + baseId + ", 경지 " + grade + ", 품질 " + minLevel + " 이상)은 즐겨찾기에 없습니다.";
                return false;
            }
            string label = BookLabel(favorite.BookKind, baseId, grade, minLevel);
            string error;
            if (!Change(next => next.RemoveAll(f => f.Book && f.Id == baseId && f.Grade == grade && f.MinLevel == minLevel), out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에서 뺐습니다: " + label;
            return true;
        }

        public static bool SetBookAmount(int baseId, int grade, int minLevel, int amount, out string text)
        {
            if (amount < 1 || amount > MartialBookService.MaxPerGive)
            {
                text = BookAmountRule;
                return false;
            }
            Recover();
            Favorite favorite = FindBook(baseId, grade, minLevel);
            if (favorite == null)
            {
                text = "즐겨찾기: 그 비급(" + baseId + ", 경지 " + grade + ", 품질 " + minLevel + " 이상)은 즐겨찾기에 없습니다.";
                return false;
            }
            int old = favorite.Amount;
            string label = BookLabel(favorite.BookKind, baseId, grade, minLevel);
            string error;
            if (!Change(next => next.Find(f => f.Book && f.Id == baseId && f.Grade == grade && f.MinLevel == minLevel).Amount = amount, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기 수량: " + label + " " + old + " -> " + amount;
            return true;
        }

        // 목록의 사본을 고쳐 파일에 쓰고, 쓰기에 성공했을 때만 메모리의 목록을 바꾼다.
        private static bool Change(Action<List<Favorite>> edit, out string error)
        {
            Load();
            if (readError != null)
            {
                // 읽지 못한 파일은 덮어쓰지 않는다. 다시 읽어 본다.
                list = null;
                Load();
                if (readError != null)
                {
                    error = "파일을 읽지 못해 바꾸지 않았습니다(" + readError + ").";
                    return false;
                }
            }
            var next = new List<Favorite>();
            foreach (Favorite f in list)
            {
                next.Add(f.Copy());
            }
            edit(next);
            try
            {
                Directory.CreateDirectory(ChangeLog.DataDir);
                if (keepOriginal && File.Exists(FilePath))
                {
                    File.Copy(FilePath, FilePath + ".bak", true);
                }
                var sb = new StringBuilder("# TlToybox 자원 탭 즐겨찾기: 한 줄에 <아이템 번호> <수량>, 또는 book <분류> <기술 번호> <경지> <최소 품질> <수량>\n");
                foreach (Favorite f in next)
                {
                    if (f.Book)
                    {
                        sb.Append("book ").Append(f.BookKind).Append(' ').Append(f.Id).Append(' ').Append(f.Grade).Append(' ').Append(f.MinLevel).Append(' ').Append(f.Amount).Append('\n');
                    }
                    else
                    {
                        sb.Append(f.Id).Append(' ').Append(f.Amount).Append('\n');
                    }
                }
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                if (File.Exists(FilePath))
                {
                    File.Replace(tmp, FilePath, null);
                }
                else
                {
                    File.Move(tmp, FilePath);
                }
                keepOriginal = false;
            }
            catch (Exception e)
            {
                error = "파일에 쓰지 못했습니다(" + e.Message + ").";
                return false;
            }
            list = next;
            error = null;
            return true;
        }
    }
}
```

- [ ] **Step 4: `rfav`를 고친다**

`src/ModCode/ModMain/Dev/DevCommandsResources.cs`의 `Favorites` 메서드:

`case "del":` 블록 아래에 넣는다:

```csharp
                case "addbook":
                    FavoriteStore.AddBook(int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]), int.Parse(p[6]), out text);
                    break;
                case "setbook":
                    FavoriteStore.SetBookAmount(int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]), out text);
                    break;
                case "delbook":
                    FavoriteStore.RemoveBook(int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), out text);
                    break;
```

항목을 만드는 반복문 안의 `items.Add(f.Id + ":" + f.Amount + (ItemCatalog.Get(f.Id) == null ? ":missing" : ""));`를 아래로 바꾼다:

```csharp
                items.Add(f.Book
                    ? "book:" + f.BookKind + ":" + f.Id + ":" + f.Grade + ":" + f.MinLevel + ":" + f.Amount + (SkillCatalog.Get(f.BookKind, f.Id) == null ? ":missing" : "")
                    : f.Id + ":" + f.Amount + (ItemCatalog.Get(f.Id) == null ? ":missing" : ""));
```

머리 주석의 `rfav` 줄에 `|addbook <분류> <기술> <경지> <최소 품질> <수량>|setbook <기술> <경지> <최소 품질> <수량>|delbook <기술> <경지> <최소 품질>`과 "비급 항목은 book:<분류>:<기술>:<경지>:<최소 품질>:<수량>"을 더한다.

- [ ] **Step 5: 빌드하고 검사가 통과하는 것을 본다**

Task 1 Step 6과 같은 두 빌드(경고·오류 0).

```bash
pwsh tools/DevRemote/session.ps1 -Begin book-1015e -Fixture base
pwsh tools/research/ui-check.ps1 -Part favstore
pwsh tools/DevRemote/session.ps1 -Restart
pwsh tools/research/ui-check.ps1 -Part favstore-restart
pwsh tools/research/ui-check.ps1 -Part favorites
pwsh tools/research/ui-check.ps1 -Part favorites-corrupt
pwsh tools/DevRemote/session.ps1 -End book-1015e
pwsh tools/DevRemote/session.tests.ps1
```

Expected: 네 부분 `FAILS=0`(지금의 자원 탭이 바뀐 저장소로 그대로 동작한다), `session.tests.ps1` 27개 PASS.

- [ ] **Step 6: 커밋**

```bash
git add src/ModCode/ModMain/Services/FavoriteStore.cs src/ModCode/ModMain/Dev/DevCommandsResources.cs tools/research/ui-check.ps1
git commit -m "feat: 즐겨찾기에 비급 줄(분류·기술·경지·최소 품질·권수)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 자원 탭의 기술 서적 그룹

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs`(전체 교체)
- Create: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs`
- Modify: `tools/research/ui-check.ps1`(부분 `books-ui`)

**Interfaces:**
- Consumes: Task 1의 `MartialBookService.Make/CountAllInBag/Describe/MaxPerGive`, `SkillCatalog.Get/Find/BlockReason/LearnedCounts/Kinds`. Task 2의 `FavoriteStore.HasBook/BookAmountOf/AddBook/RemoveBook/SetBookAmount/BookLabel`, `Favorite.Book/BookKind/Grade/MinLevel`.
- Produces: 화면 요소 이름(Global Constraints). 그룹 버튼의 간격을 44에서 42(높이 40에서 38)로 줄인다. 그룹이 14개가 되어 아래 설명 칸(y 694) 위에 들어가게 하기 위해서다.

- [ ] **Step 1: 검사를 먼저 쓴다**

`tools/research/ui-check.ps1`의 부분 `'books'` 아래에 넣는다:

```powershell
  'books-ui' {
    # 기술 서적 스펙 5절: 화면. 고정본 인물은 결정(대경지 3). 회차의 -End가 favorites.txt를 되돌린다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "$key=([^\s|]+)") { $Matches[1] } else { '' } }
    function BookCount { [int](Field (Cmd 'rbooks 23116' 300) 'count') }
    function Held([int]$id) { if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 } }
    function Range { if ((Cmd 'uitext ItemTableRange') -match '전체 (\d+)개') { [int]$Matches[1] } elseif ((Cmd 'uitext ItemTableRange') -match "text='0개'") { 0 } else { -1 } }
    Cmd 'rfav reset' 300 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupManual' | Out-Null; Press 'ResClear' | Out-Null
    Check 'the group button shows a count' ((Cmd 'uitext ResGroupManual') -match "text='기술 서적 [1-9]\d*'") (Cmd 'uitext ResGroupManual')
    Check 'six kind chips' ((Cmd 'uitext ResKind0') -match "text='전체 \(" -and (Cmd 'uitext ResKind5') -match "text='신통 \(" -and (Cmd 'uipos ResKind6') -match 'no element named')
    $r = Pos 'Row0'
    Rc "click $($r.x) $($r.y)" 600 | Out-Null
    Check 'the grade starts at the current grade' ((Cmd 'uitext ItemDetail') -match '지급하면: 경지 3, 품질 1 이상') (Cmd 'uitext ItemDetail')
    $fit = Range
    Press 'ResBookFit' | Out-Null
    $every = Range
    Check 'turning off "this grade only" shows more skills' ($fit -gt 0 -and $every -gt $fit) "fit=$fit all=$every"
    $outside = $null
    foreach ($c in @(@(0, 17111), @(2, 510021), @(0, 11423), @(0, 44112))) {
      $s = Cmd "rskill $($c[0]) $($c[1])" 200
      if ($s -match 'min=(\d+) max=(\d+)' -and (3 -lt [int]$Matches[1] -or 3 -gt [int]$Matches[2])) { $outside = $c[1]; break }
    }
    if ($outside) {
      Cmd "ui set ItemQuery $outside" | Out-Null
      $p = Pos "BookGive$outside"
      Check 'a skill outside the grade cannot be given' ((Rc "click $($p.x) $($p.y)" 400) -notmatch "Button\.onClick: BookGive$outside")
      Press 'ItemQueryClear' | Out-Null
    } else { 'INFO no candidate skill outside grade 3' }
    Press 'ResBookFit' | Out-Null
    # Review Focus 5: 경지를 바꾼 뒤 초기화하면 지금 경지로
    Press 'ResGrade5' | Out-Null; Press 'ResMinLevel4' | Out-Null
    Press 'ResClear' | Out-Null
    $r = Pos 'Row0'
    Rc "click $($r.x) $($r.y)" 600 | Out-Null
    Check 'clear brings back the current grade and quality 1' ((Cmd 'uitext ItemDetail') -match '지급하면: 경지 3, 품질 1 이상') (Cmd 'uitext ItemDetail')
    # 최소 품질 6으로 지급
    Cmd 'ui set ItemQuery 23116' | Out-Null
    Press 'ResMinLevel6' | Out-Null
    Press 'item1' | Out-Null
    $before = BookCount
    Press 'BookGive23116' 3000 | Out-Null
    $books = Cmd 'rbooks 23116' 300
    Check 'giving makes a quality 6 book of that skill' ((BookCount) -eq $before + 1 -and $books -match ':23116:3:6:') $books
    Check 'the bag column counts the books' ((Cmd 'uitext BookBag23116') -match "text='$($before + 1)'") (Cmd 'uitext BookBag23116')
    Press 'BookName23116' | Out-Null
    Check 'the box below tells what will be made' ((Cmd 'uitext ItemDetail') -match '지급하면: 경지 3, 품질 6 이상의 비급 1권') (Cmd 'uitext ItemDetail')
    Press 'BookStar23116' | Out-Null
    Check 'the star saves the book with grade and quality' ((Cmd 'rfav' 200) -match 'book:1:23116:3:6:1') (Cmd 'rfav' 200)
    Check 'the star is filled' ((Cmd 'uitext BookStar23116') -match "text='★'")
    Press 'ItemQueryClear' | Out-Null
    # 아이템 하나를 함께 넣고 즐겨찾기 보기로
    Press 'ResGroupAll' | Out-Null
    Cmd 'ui set ItemQuery 1011011' | Out-Null
    Press 'ResStar1011011' | Out-Null
    Press 'ItemQueryClear' | Out-Null
    Press 'ResGroupFav' | Out-Null
    Check 'the favorites view has the book row' ((Cmd 'uitext FavBookName23116_3_6') -match '비급 \(경지 3, 품질 6 이상\)') (Cmd 'uitext FavBookName23116_3_6')
    Cmd 'uifocus FavBookAmount23116_3_6' | Out-Null; Cmd 'ui set FavBookAmount23116_3_6 21' | Out-Null; Key 0x0D
    Check 'a book amount above 20 is refused' ((Cmd 'rfav' 200) -match 'book:1:23116:3:6:1(\s|,|$)' -and (Cmd 'uistate') -match '비급 수량은 1~20') (Cmd 'uistate')
    Cmd 'uifocus FavBookAmount23116_3_6' | Out-Null; Cmd 'ui set FavBookAmount23116_3_6 5' | Out-Null; Key 0x0D
    Check 'the book amount is saved' ((Cmd 'rfav' 200) -match 'book:1:23116:3:6:5') (Cmd 'rfav' 200)
    $before = BookCount
    Press 'FavBookGive23116_3_6' 5000 | Out-Null
    Check 'the book row gives its saved amount' ((BookCount) -eq $before + 5) "$before -> $(BookCount)"
    $before = BookCount; $had = Held 1011011
    Press 'ResAllGive' | Out-Null; Press 'ResAllGive' 8000 | Out-Null
    Check 'give all gives items and books' ((Held 1011011) -eq $had + 1 -and (BookCount) -eq $before + 5 -and (Cmd 'uistate') -match "message='즐겨찾기 모두 지급: 넣음 2, 못 넣음 0") "$(Cmd 'uistate')"
    Shot 'books-ui'
    Press 'FavBookStar23116_3_6' | Out-Null
    Check 'the book favorite can be removed' ((Cmd 'rfav' 200) -notmatch 'book:') (Cmd 'rfav' 200)
    # Review Focus 1: 게임에 없는 기술의 비급 즐겨찾기
    Cmd 'rfav addbook 1 99999 3 6 1' 300 | Out-Null
    Press 'ResQuality2' | Out-Null
    Check 'a missing skill book favorite is shown under a filter' ((Cmd 'uitext FavBookName99999_3_6') -match '게임에 없는 기술 \(99999\)') (Cmd 'uitext FavBookName99999_3_6')
    $p = Pos 'FavBookGive99999_3_6'
    Check 'its give button is off' ((Rc "click $($p.x) $($p.y)" 400) -notmatch 'Button\.onClick: FavBookGive99999_3_6')
    Press 'FavBookStar99999_3_6' | Out-Null
    Check 'and it can be removed' ((Cmd 'rfav' 200) -notmatch '99999') (Cmd 'rfav' 200)
    Press 'ResClear' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
```

`Range`는 PowerShell의 별칭과 겹치지 않는지 본다:

```bash
pwsh -NoProfile -Command "Get-Alias Range, Held, BookCount, BookLines, Field -ErrorAction SilentlyContinue"
```

Expected: 아무것도 나오지 않는다. 나오면 그 함수 이름을 바꾼다(예: `Range` → `Shown`).

`'the bag column counts the books'`는 기술 줄의 가방 열 글(`BookBag<기술 번호>`)이 가방의 그 기술 비급 수와 같은지 본다.

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin book-1015f -Fixture base
pwsh tools/research/ui-check.ps1 -Part books-ui
pwsh tools/DevRemote/session.ps1 -End book-1015f
```

Expected: `편집기에서 요소를 찾지 못했습니다: ResGroupManual`로 멈춘다.

- [ ] **Step 3: 자원 탭을 바꾼다**

`src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs`를 통째로 바꾼다:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭: 영석, 지급 수량과 가방 칸, 용도 그룹으로 나눈 아이템 표, 기술 서적(비급) 지급, 즐겨찾기.
    // 설계: docs/superpowers/specs/2026-10-15-resources-tab-design.md, 2026-10-15-martial-books-design.md.
    // 판단은 ItemCatalog·FavoriteStore·MartialBookService가 하고 여기서는 그린다. 기술 서적 그룹은 ResourcesTab.Books.cs.
    internal sealed partial class ResourcesTab : EditorTab
    {
        private const float RowH = 32;
        private const float DetailH = 150;              // 표에 설명 열이 없어 설명은 이 칸에서 본다. 넘치면 칸 안에서 스크롤한다
        private const float TopH = 92;                  // 1줄 영석, 2줄 지급 수량과 가방 칸
        private const float LeftW = 170;                // 그룹 목록
        private const float GroupStep = 42;             // 그룹 버튼(높이 38)의 간격. 열네 개가 아래 설명 칸 위에 들어간다
        private const float RightX = 180;
        private const float RightW = ContentW - RightX; // 1124
        private const float ChipH = 34, ChipGap = 6;
        private const int ChipRowsShown = 3;
        // 표의 열(목록 안의 x). 설명은 표에 두지 않는다(줄이 높아져 한 화면에 몇 줄 안 보였다. 사용자 지시 2026-10-15).
        // 지급 열은 목록 안쪽 폭(RightW - 스크롤 막대)의 오른쪽 끝에 붙인다.
        private const float ColStar = 4, ColName = 50, ColKind = 436, ColLevel = 602, ColOwned = 652;
        private const float NameW = 380, KindW = 160, GiveW = 116, AmountW = 90;
        private const float ColGive = RightW - Ui.ScrollBarWidth - GiveW - 4;   // 990
        private const float ColAmount = ColGive - AmountW - 6;                  // 894

        // 표의 한 줄: 아이템(Item), 기술 서적 그룹의 기술(Skill), 즐겨찾기의 비급(Book) 가운데 하나.
        private sealed class Row
        {
            public ItemCatalog.Entry Item;
            public SkillCatalog.Entry Skill;            // 기술 줄과 비급 즐겨찾기 줄. 비급 즐겨찾기의 기술이 게임에 없으면 null
            public int SkillKind;
            public FavoriteStore.Favorite Book;
        }

        // 검색어가 바뀌면 탭을 다시 그리지 않고(검색 칸의 포커스를 지킨다) 이 글들만 고친다.
        private sealed class CountLabel
        {
            public Text Text;
            public string Name;
            public string Key;      // 그룹 버튼이면 그룹 키, 종류 칩이면 null
            public int Index;       // 종류 칩의 순번(0이 전체)
            public void Set(int count)
            {
                Text.text = Key != null ? Name + " " + count : Name + " (" + count + ")";
            }
        }

        private readonly ListView<Row> list = new ListView<Row>("ItemTable", RowH);
        private readonly DetailPane detail = new DetailPane("ItemDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");
        private readonly ItemCatalog.Filter filter = new ItemCatalog.Filter();
        private readonly List<CountLabel> labels = new List<CountLabel>();
        private string groupKey = ItemGroups.AllKey;
        private float chipScrollY;
        private string bagInput = "";
        private Dictionary<int, int> bagBooks = new Dictionary<int, int>();   // 가방의 기술별 비급 수(그릴 때마다 센다)

        public override string Title
        {
            get { return "자원"; }
        }

        public ResourcesTab()
        {
            // 줄의 빈 곳을 눌러도 아래 칸에 설명이 나온다(이름 버튼과 같다).
            list.OnSelect = row => detail.Show(DetailOf(row));
        }

        private bool IsFav
        {
            get { return groupKey == ItemGroups.FavKey; }
        }

        public override void OnShow()
        {
            string notice = FavoriteStore.TakeNotice();
            if (notice != null)
            {
                Say(notice);
            }
        }

        // 가방의 추가 칸을 원하는 수로 맞춘다. 10칸 단위로 내림한 뒤 지금 값과의 차이만큼 바꾼다.
        // 범위 밖의 수는 가까운 값으로 맞추지 않고 거부한다(음수를 잘못 넣었을 때 추가 칸이 모두 사라지지 않게).
        private void SetBagExtra(int wanted)
        {
            if (wanted < 0 || wanted > BagService.MaxExtra)
            {
                Say("가방: 추가 칸은 0~" + BagService.MaxExtra + " 사이의 숫자로 입력하세요.");
                return;
            }
            int target = wanted / BagService.Step * BagService.Step;
            int delta = target - BagService.Extra;
            if (delta == 0)
            {
                Say("가방: 추가 칸은 이미 +" + target + "입니다.");
                return;
            }
            Say(BagService.Change(delta));
        }

        // 그룹·종류·품질·보유 중만 등을 바꾼다: 목록을 맨 위로, 설명 칸을 비우고 다시 그린다.
        private void Changed()
        {
            list.Reset();
            detail.Clear();
            Redraw();
        }

        private void SetGroup(string key)
        {
            groupKey = key;
            filter.Kind = null;
            chipScrollY = 0;
            Changed();
        }

        private void ClearFilters()
        {
            filter.Query = "";
            filter.Kind = null;
            filter.Quality = 0;
            filter.OwnedOnly = false;
            ClearBookFilters();
            Changed();
        }

        private static ItemCatalog.Entry Missing(int id)
        {
            return new ItemCatalog.Entry
            {
                Id = id,
                Name = "게임에 없는 아이템 (" + id + ")",
                Desc = "즐겨찾기에 남아 있지만 지금 게임의 아이템 표에 없습니다(그 아이템을 넣은 모드를 뺐을 수 있습니다). ★를 누르면 뺍니다.",
                Search = "",
                Missing = true,
            };
        }

        private int BagBooks(int baseId)
        {
            int n;
            return bagBooks.TryGetValue(baseId, out n) ? n : 0;
        }

        private Row FavRow(FavoriteStore.Favorite favorite)
        {
            if (!favorite.Book)
            {
                return new Row { Item = ItemCatalog.Get(favorite.Id) ?? Missing(favorite.Id) };
            }
            return new Row { Book = favorite, SkillKind = favorite.BookKind, Skill = SkillCatalog.Get(favorite.BookKind, favorite.Id) };
        }

        // 즐겨찾기 줄의 거르기. 비급 줄: 검색은 기술 이름, 품질은 최소 품질이 같은 것, 보유 중만은 가방에 그 기술의 비급이 있는 것.
        // 게임에 없는 아이템·기술은 늘 보인다(뺄 수 있게).
        private bool FavPasses(Row row, HashSet<int> owned)
        {
            if (row.Item != null)
            {
                return ItemCatalog.Passes(row.Item, filter, owned);
            }
            if (row.Skill == null)
            {
                return true;
            }
            if (filter.Quality > 0 && row.Book.MinLevel != filter.Quality)
            {
                return false;
            }
            if (filter.OwnedOnly && BagBooks(row.Book.Id) == 0)
            {
                return false;
            }
            return filter.Needle.Length == 0 || row.Skill.Search.Contains(filter.Needle) || (filter.IsId && row.Skill.BaseId == filter.Id);
        }

        // 지금 보일 줄. 즐겨찾기 보기는 넣은 순서, 기술 서적은 분류·기술 표 순서, 나머지는 그룹·종류·게임 표 순서.
        private List<Row> Rows(HashSet<int> owned)
        {
            if (groupKey == ManualKey)
            {
                return BookRows();
            }
            var rows = new List<Row>();
            if (!IsFav)
            {
                filter.Group = groupKey;
                foreach (ItemCatalog.Entry entry in ItemCatalog.Select(filter, owned))
                {
                    rows.Add(new Row { Item = entry });
                }
                return rows;
            }
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                Row row = FavRow(favorite);
                if (FavPasses(row, owned))
                {
                    rows.Add(row);
                }
            }
            return rows;
        }

        private int FavCount(HashSet<int> owned)
        {
            int count = 0;
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                if (FavPasses(FavRow(favorite), owned))
                {
                    count++;
                }
            }
            return count;
        }

        private string NameOf(Row row)
        {
            if (row.Item != null)
            {
                return row.Item.Name;
            }
            return row.Book != null ? BookFavName(row) : row.Skill.Name;
        }

        private static string KindOf(Row row)
        {
            if (row.Item != null)
            {
                return row.Item.Kind == null ? "-" : row.Item.Kind.Name;
            }
            string tab = SkillCatalog.Kinds[row.SkillKind].Tab;
            return row.Book != null ? "비급·" + tab : tab;
        }

        private string DetailOf(Row row)
        {
            if (row.Item != null)
            {
                return Detail(row.Item);
            }
            return row.Book != null ? BookFavDetail(row) : SkillDetail(row);
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            HashSet<int> owned = ItemCatalog.OwnedIds(player);
            bagBooks = MartialBookService.CountAllInBag(player);
            PrepareBooks(player);
            labels.Clear();
            DrawTop(player);
            DrawGroups(owned);
            float y = TopH + 8;
            y = DrawKinds(y, owned);
            y = groupKey == ManualKey ? DrawBookFilters(y) : DrawFilters(y);
            if (groupKey == ManualKey)
            {
                DrawBookHead(y);
            }
            else
            {
                DrawHead(y);
            }
            y += 32;
            list.EmptyText = IsFav ? "즐겨찾기가 비어 있습니다. 표의 ☆를 눌러 넣습니다."
                : groupKey == ManualKey ? "조건에 맞는 기술이 없습니다. '이 경지만'을 끄면 다른 경지의 기술도 보입니다."
                : "조건에 맞는 아이템이 없습니다.";
            list.HeightOf = row => RowFor(RowH, Ui.TextHeight(NameOf(row), NameW, 19), Ui.TextHeight(KindOf(row), KindW, 16));
            list.Build(content, RightX, y, RightW, ContentH - DetailH - 8 - y, Rows(owned), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawTop(WorldUnitBase player)
        {
            int moneyAmount = AmountBar.Get("money");
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, TopH, Ui.Section);
            // 1줄: 영석
            Ui.Label(content, "영석: " + EditService.Money(player), 12, 4, 250, 40, 20);
            Ui.Label(content, "추가량", 270, 4, 70, 40, 18).color = Ui.Dim;
            float moneyRight = AmountBar.Build(content, "money", 340, 5);
            Ui.Button("MoneyAdd", content, "영석 + " + moneyAmount, moneyRight + 16, 5, 200, 38, new Action(() => Say(EditService.AddMoney(Player, moneyAmount))));
            // 2줄: 아이템 지급 수량과 가방 칸. 가방 칸은 편집기가 늘린 만큼만 되돌릴 수 있다.
            Ui.Label(content, "지급 수량", 12, 48, 110, 40, 18);
            float itemRight = AmountBar.Build(content, "item", 122, 49);
            int bagExtra = BagService.Extra;
            bagInput = bagExtra.ToString();
            float bagX = itemRight + 16;
            Ui.Label(content, BagService.Describe(), bagX, 48, 296, 40, 17);
            float bx = bagX + 300;
            NumberField.Build(content, "BagExtra", bx, 49, 76, 38, bagInput, new Action<string>(v => bagInput = v), new Action<int>(SetBagExtra));
            Ui.Button("BagApply", content, "적용", bx + 80, 49, 60, 38, new Action(() =>
            {
                int value;
                if (!int.TryParse(bagInput, out value))
                {
                    Say("가방: 추가 칸을 숫자로 입력하세요.");
                    return;
                }
                SetBagExtra(value);
            }));
            Ui.Button("BagMinus", content, "-" + BagService.Step, bx + 144, 49, 60, 38, new Action(() => Say(BagService.Change(-BagService.Step))), bagExtra > 0);
            Ui.Button("BagPlus", content, "+" + BagService.Step, bx + 208, 49, 60, 38, new Action(() => Say(BagService.Change(BagService.Step))), bagExtra < BagService.MaxExtra);
        }

        private void DrawGroups(HashSet<int> owned)
        {
            Dictionary<string, int> counts = ItemCatalog.CountByGroup(filter, owned);
            float y = TopH + 8;
            GroupButton(ItemGroups.FavKey, "★ 즐겨찾기", FavCount(owned), y);
            y += GroupStep;
            GroupButton(ItemGroups.AllKey, "전체", counts[ItemGroups.AllKey], y);
            y += GroupStep;
            foreach (ItemGroups.Group group in ItemGroups.Groups)
            {
                GroupButton(group.Key, group.Name, counts[group.Key], y);
                y += GroupStep;
                if (group.Key == "Book")
                {
                    GroupButton(ManualKey, "기술 서적", BookCount(), y);
                    y += GroupStep;
                }
            }
        }

        private void GroupButton(string key, string name, int count, float y)
        {
            Button button = Ui.Button("ResGroup" + key, content, name + " " + count, 0, y, LeftW, GroupStep - 4, new Action(() => SetGroup(key)), true, groupKey == key);
            labels.Add(new CountLabel { Text = button.GetComponentInChildren<Text>(), Name = name, Key = key });
        }

        // 종류 칩(그룹 보기), 모두 지급(즐겨찾기 보기) 또는 안내(전체 보기). 다음 줄의 y를 돌려준다.
        private float DrawKinds(float y, HashSet<int> owned)
        {
            if (groupKey == ManualKey)
            {
                return DrawBookKinds(y);
            }
            if (IsFav)
            {
                Confirm.Button("ResAllGive", content, "모두 지급", "정말 모두 지급", RightX, y, 170, 38, "res-all-give",
                    "보이는 즐겨찾기를 저장된 수량으로 모두 지급합니다. 한 번 더 누르면 실행합니다.", new Action(GiveAll));
                Ui.Label(content, "수량 칸에 쓰고 Enter를 누르면 저장됩니다. ☆로 넣고 ★로 뺍니다.", RightX + 182, y, RightW - 182, 38, 16).color = Ui.Dim;
                return y + 44;
            }
            if (groupKey == ItemGroups.AllKey)
            {
                Text hint = Ui.Label(content, "왼쪽에서 그룹을 고르면 종류로 나눠 볼 수 있습니다.", RightX, y, RightW, 38, 17);
                hint.color = Ui.Dim;
                hint.gameObject.name = "ResAllHint";
                return y + 44;
            }
            ItemGroups.Group group = ItemGroups.ByKey(groupKey);
            int[] counts = ItemCatalog.CountByKind(group, filter, owned);
            int[] widest = ItemCatalog.CountByKind(group, new ItemCatalog.Filter(), owned);   // 칩의 폭은 거르기 전의 수로 잰다
            var names = new List<string> { "전체" };
            var shown = new List<int>();
            var sized = new List<int>();
            int shownAll = 0, sizedAll = 0;
            for (int i = 0; i < group.Kinds.Count; i++)
            {
                names.Add(group.Kinds[i].Name);
                shown.Add(counts[i]);
                sized.Add(widest[i]);
                shownAll += counts[i];
                sizedAll += widest[i];
            }
            shown.Insert(0, shownAll);
            sized.Insert(0, sizedAll);
            // 칩을 줄바꿈해 모두 놓는다. 세 줄을 넘으면 그 영역 안에서 스크롤한다.
            float areaW = RightW - Ui.ScrollBarWidth;
            var xs = new float[names.Count];
            var ys = new float[names.Count];
            var ws = new float[names.Count];
            float cx = 0, cy = 0;
            for (int i = 0; i < names.Count; i++)
            {
                float w = Math.Max(70f, Ui.TextWidth(names[i] + " (" + sized[i] + ")", 19) + 28f);
                if (cx > 0 && cx + w > areaW)
                {
                    cx = 0;
                    cy += ChipH + ChipGap;
                }
                xs[i] = cx;
                ys[i] = cy;
                ws[i] = w;
                cx += w + ChipGap;
            }
            float allH = cy + ChipH;
            float shownH = Math.Min(allH, ChipRowsShown * (ChipH + ChipGap) - ChipGap);
            Transform area = allH > shownH
                ? Ui.Scroll("ResKinds", content, RightX, y, RightW, shownH, allH, chipScrollY, new Action<float>(v => chipScrollY = v))
                : Ui.Rect("ResKinds", content, RightX, y, RightW, shownH);
            for (int i = 0; i < names.Count; i++)
            {
                ItemGroups.Kind kind = i == 0 ? null : group.Kinds[i - 1];
                Button chip = Ui.Button("ResKind" + i, area, names[i] + " (" + shown[i] + ")", xs[i], ys[i], ws[i], ChipH,
                    new Action(() => { filter.Kind = kind; Changed(); }), true, filter.Kind == kind);
                labels.Add(new CountLabel { Text = chip.GetComponentInChildren<Text>(), Name = names[i], Index = i });
            }
            return y + shownH + 6;
        }

        private float DrawFilters(float y)
        {
            Ui.Label(content, "아이템명", RightX, y, 76, 38, 18);
            SearchBox.Build(content, "ItemQuery", RightX + 76, y, 290, filter.Query, "이름의 일부 또는 번호", new Action<string>(OnQuery));
            Ui.Label(content, "품질(높을수록 귀함)", RightX + 376, y, 150, 38, 16).color = Ui.Dim;
            Ui.Button("ResQuality0", content, "전체", RightX + 526, y, 56, 38, new Action(() => { filter.Quality = 0; Changed(); }), true, filter.Quality == 0);
            for (int q = 1; q <= 6; q++)
            {
                int quality = q;
                Ui.Button("ResQuality" + q, content, q.ToString(), RightX + 586 + (q - 1) * 44, y, 40, 38,
                    new Action(() => { filter.Quality = quality; Changed(); }), true, filter.Quality == q);
            }
            Ui.Button("ResOwnedOnly", content, "보유 중만", RightX + 856, y, 120, 38, new Action(() => { filter.OwnedOnly = !filter.OwnedOnly; Changed(); }), true, filter.OwnedOnly);
            Ui.Button("ResClear", content, "초기화", RightX + 982, y, 100, 38, new Action(ClearFilters));
            return y + 44;
        }

        private void DrawHead(float y)
        {
            Ui.Panel("ResHead", content, RightX, y, RightW, 30, Ui.Section);
            Ui.Label(content, "☆", RightX + ColStar, y, 40, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            Ui.Label(content, "아이템명", RightX + ColName, y, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "종류", RightX + ColKind, y, KindW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", RightX + ColLevel, y, 44, 30, 16).color = Ui.Dim;
            Ui.Label(content, "보유", RightX + ColOwned, y, 60, 30, 16).color = Ui.Dim;
            if (IsFav)
            {
                Ui.Label(content, "수량", RightX + ColAmount, y, AmountW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            }
            Ui.Label(content, "지급", RightX + ColGive, y, GiveW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
        }

        // 검색어가 바뀌었을 때: 목록과 개수 글만 고친다.
        private void OnQuery(string value)
        {
            filter.Query = value;
            detail.Clear();
            HashSet<int> owned = ItemCatalog.OwnedIds(Player);
            list.SetItems(Rows(owned));
            Dictionary<string, int> counts = ItemCatalog.CountByGroup(filter, owned);
            int[] kinds = IsFav || groupKey == ItemGroups.AllKey ? null
                : groupKey == ManualKey ? BookKindCounts()
                : ItemCatalog.CountByKind(ItemGroups.ByKey(groupKey), filter, owned);
            int favCount = FavCount(owned);
            int bookCount = BookCount();
            foreach (CountLabel label in labels)
            {
                if (label.Key == ItemGroups.FavKey)
                {
                    label.Set(favCount);
                }
                else if (label.Key == ManualKey)
                {
                    label.Set(bookCount);
                }
                else if (label.Key != null)
                {
                    label.Set(counts[label.Key]);
                }
                else if (kinds != null)
                {
                    int sum = 0;
                    foreach (int n in kinds)
                    {
                        sum += n;
                    }
                    label.Set(label.Index == 0 ? sum : kinds[label.Index - 1]);
                }
            }
        }

        private void DrawRow(RectTransform row, Row r, int index)
        {
            float h = row.sizeDelta.y;
            if (r.Item != null)
            {
                DrawItemRow(row, r.Item, h);
            }
            else if (r.Book != null)
            {
                DrawBookFavRow(row, r, h);
            }
            else
            {
                DrawSkillRow(row, r, h);
            }
        }

        private void DrawItemRow(RectTransform row, ItemCatalog.Entry entry, float h)
        {
            int id = entry.Id;
            bool fav = FavoriteStore.Has(id);
            Ui.Button("ResStar" + id, row, fav ? "★" : "☆", ColStar, 2, 40, RowH - 4, new Action(() => ToggleFavorite(id)), true, fav);
            WrapLabel(Ui.Button("ItemName" + id, row, entry.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(Detail(entry)))));
            Ui.Wrapped(row, entry.Kind == null ? "-" : entry.Kind.Name, ColKind, 0, KindW, h, 16);
            Ui.Label(row, entry.Missing ? "-" : entry.Level.ToString(), ColLevel, 0, 44, h, 16);
            Ui.Label(row, entry.Missing ? "-" : Player.data.unitData.propData.GetPropsNum(id).ToString(), ColOwned, 0, 60, h, 16);
            if (IsFav)
            {
                int saved = FavoriteStore.AmountOf(id);
                NumberField.Build(row, "ResAmount" + id, ColAmount, (h - (RowH - 4)) / 2, AmountW, RowH - 4, saved.ToString(), null,
                    new Action<int>(v => SetAmount(id, v)));
                Ui.Button("ItemGive" + id, row, "지급 x" + saved, ColGive, 2, GiveW, RowH - 4,
                    new Action(() => Say(EditService.GiveItem(Player, id, FavoriteStore.AmountOf(id)))), !entry.Missing);
            }
            else
            {
                int amount = AmountBar.Get("item");
                Ui.Button("ItemGive" + id, row, "지급 x" + amount, ColGive, 2, GiveW, RowH - 4, new Action(() => Say(EditService.GiveItem(Player, id, amount))));
            }
        }

        private void ToggleFavorite(int id)
        {
            string text;
            if (FavoriteStore.Has(id))
            {
                FavoriteStore.Remove(id, out text);
            }
            else
            {
                FavoriteStore.Add(id, AmountBar.Get("item"), out text);
            }
            Say(text);
        }

        private void SetAmount(int id, int amount)
        {
            string text;
            FavoriteStore.SetAmount(id, amount, out text);
            Say(text);
        }

        // 보이는(걸러진) 즐겨찾기 가운데 게임에 있는 것을 저장된 수량으로 차례로 준다. 비급 줄은 저장된 권수를 모두 넣었을 때 넣음이다.
        // 줄마다 예외를 막는다. 밖으로 나가면 버튼 콜백에서 사라져 결과 문장 없이 일부만 들어가고, 다시 누르면 앞의 것을 또 준다.
        private void GiveAll()
        {
            int given = 0;
            var failed = new List<string>();
            foreach (Row row in Rows(ItemCatalog.OwnedIds(Player)))
            {
                if (row.Item != null ? row.Item.Missing : row.Skill == null)
                {
                    continue;
                }
                string name = row.Item != null ? row.Item.Name : row.Skill.Name + " 비급";
                try
                {
                    string text;
                    bool ok = row.Item != null
                        ? EditService.TryGiveItem(Player, row.Item.Id, FavoriteStore.AmountOf(row.Item.Id), out text)
                        : MartialBookService.Make(Player, row.Book.BookKind, row.Skill, row.Book.Grade, row.Book.MinLevel, row.Book.Amount, out text) == row.Book.Amount;
                    if (ok)
                    {
                        given++;
                    }
                    else
                    {
                        failed.Add(name);
                    }
                }
                catch (Exception e)
                {
                    failed.Add(name + " — " + e.GetType().Name);
                }
            }
            Say(given + failed.Count == 0 ? "즐겨찾기 모두 지급: 줄 수 있는 아이템이 없습니다."
                : "즐겨찾기 모두 지급: 넣음 " + given + ", 못 넣음 " + failed.Count + (failed.Count > 0 ? "(" + string.Join(", ", failed.ToArray()) + ")" : "") + ".");
        }

        private static string Detail(ItemCatalog.Entry entry)
        {
            if (entry.Missing)
            {
                return "[" + entry.Id + "] " + entry.Name + "\n" + entry.Desc;
            }
            return "[" + entry.Id + "] " + entry.Name + "  (" + entry.Group.Name + " / " + entry.Kind.Name + ", 품질 " + entry.Level + ")\n" + entry.Desc;
        }
    }
}
```

- [ ] **Step 4: 기술 서적 그룹을 쓴다**

`src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭의 기술 서적(비급) 그룹과 즐겨찾기의 비급 줄. 설계: docs/superpowers/specs/2026-10-15-martial-books-design.md 3.2·3.3절.
    internal sealed partial class ResourcesTab
    {
        private const string ManualKey = "Manual";

        private int bookKind = -1;          // -1이면 다섯 분류 전부, 아니면 SkillCatalog.Kinds의 순번
        private int bookGrade;              // 0이면 플레이어의 지금 대경지로 정한다
        private int bookMinLevel = 1;
        private bool bookFit = true;        // 고른 경지로 만들 수 있는 기술만
        private readonly Dictionary<int, int> learned = new Dictionary<int, int>();   // 기술 번호 → 배운 수(기술 서적 그룹을 그릴 때 센다)

        private int BookGrade
        {
            get
            {
                if (bookGrade <= 0)
                {
                    bookGrade = Math.Max(1, FateService.CurrentGrade(Player));
                }
                return bookGrade;
            }
        }

        private void ClearBookFilters()
        {
            bookKind = -1;
            bookGrade = 0;
            bookMinLevel = 1;
            bookFit = true;
        }

        private void PrepareBooks(WorldUnitBase unit)
        {
            learned.Clear();
            if (groupKey != ManualKey)
            {
                return;
            }
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (var pair in SkillCatalog.LearnedCounts(unit, k))
                {
                    learned[pair.Key] = pair.Value;
                }
            }
        }

        private bool BookShown(SkillCatalog.Entry entry)
        {
            return !bookFit || SkillCatalog.BlockReason(entry, BookGrade) == null;
        }

        private List<Row> BookRows()
        {
            var rows = new List<Row>();
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                if (bookKind >= 0 && bookKind != k)
                {
                    continue;
                }
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(k, filter.Query))
                {
                    if (BookShown(entry))
                    {
                        rows.Add(new Row { Skill = entry, SkillKind = k });
                    }
                }
            }
            return rows;
        }

        // 분류마다 검색어와 "이 경지만"을 통과한 기술의 수(SkillCatalog.Kinds 순서).
        private int[] BookKindCounts()
        {
            var counts = new int[SkillCatalog.Kinds.Length];
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(k, filter.Query))
                {
                    if (BookShown(entry))
                    {
                        counts[k]++;
                    }
                }
            }
            return counts;
        }

        private int BookCount()
        {
            int total = 0;
            foreach (int n in BookKindCounts())
            {
                total += n;
            }
            return total;
        }

        // 종류 칩 여섯(전체와 다섯 분류). 한 줄에 들어간다.
        private float DrawBookKinds(float y)
        {
            int[] counts = BookKindCounts();
            int total = 0;
            foreach (int n in counts)
            {
                total += n;
            }
            float x = 0;
            for (int i = 0; i <= SkillCatalog.Kinds.Length; i++)
            {
                string name = i == 0 ? "전체" : SkillCatalog.Kinds[i - 1].Tab;
                int count = i == 0 ? total : counts[i - 1];
                float w = Math.Max(70f, Ui.TextWidth(name + " (00000)", 19) + 28f);
                int kindValue = i - 1;
                Button chip = Ui.Button("ResKind" + i, content, name + " (" + count + ")", RightX + x, y, w, ChipH,
                    new Action(() => { bookKind = kindValue; Changed(); }), true, bookKind == kindValue);
                labels.Add(new CountLabel { Text = chip.GetComponentInChildren<Text>(), Name = name, Index = i });
                x += w + ChipGap;
            }
            return y + ChipH + 6;
        }

        // 필터 두 줄: 검색·이 경지만·초기화 / 경지 1~10·최소 품질 1~6.
        private float DrawBookFilters(float y)
        {
            Ui.Label(content, "기술명", RightX, y, 76, 38, 18);
            SearchBox.Build(content, "ItemQuery", RightX + 76, y, 290, filter.Query, "이름의 일부 또는 번호", new Action<string>(OnQuery));
            Ui.Button("ResBookFit", content, "이 경지만", RightX + 376, y, 120, 38, new Action(() => { bookFit = !bookFit; Changed(); }), true, bookFit);
            Ui.Button("ResClear", content, "초기화", RightX + 502, y, 100, 38, new Action(ClearFilters));
            float y2 = y + 44;
            Ui.Label(content, "경지", RightX, y2, 50, 38, 18);
            for (int gr = 1; gr <= 10; gr++)
            {
                int value = gr;
                Ui.Button("ResGrade" + gr, content, gr.ToString(), RightX + 50 + (gr - 1) * 42, y2, 38, 38, new Action(() => { bookGrade = value; Changed(); }), true, BookGrade == gr);
            }
            Ui.Label(content, "최소 품질", RightX + 486, y2, 90, 38, 18);
            for (int q = 1; q <= 6; q++)
            {
                int value = q;
                Ui.Button("ResMinLevel" + q, content, q.ToString(), RightX + 576 + (q - 1) * 42, y2, 38, 38, new Action(() => { bookMinLevel = value; Changed(); }), true, bookMinLevel == q);
            }
            return y2 + 44;
        }

        private void DrawBookHead(float y)
        {
            Ui.Panel("ResHead", content, RightX, y, RightW, 30, Ui.Section);
            Ui.Label(content, "☆", RightX + ColStar, y, 40, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            Ui.Label(content, "기술명", RightX + ColName, y, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "분류", RightX + ColKind, y, KindW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "경지", RightX + ColLevel, y, 50, 30, 16).color = Ui.Dim;
            Ui.Label(content, "배움", RightX + ColOwned, y, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "가방", RightX + ColOwned + 70, y, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "지급", RightX + ColGive, y, GiveW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
        }

        private void DrawSkillRow(RectTransform row, Row r, float h)
        {
            SkillCatalog.Entry skill = r.Skill;
            int id = skill.BaseId;
            int kindIndex = r.SkillKind;
            bool fav = FavoriteStore.HasBook(id, BookGrade, bookMinLevel);
            Ui.Button("BookStar" + id, row, fav ? "★" : "☆", ColStar, 2, 40, RowH - 4, new Action(() => ToggleBook(kindIndex, skill)), true, fav);
            WrapLabel(Ui.Button("BookName" + id, row, skill.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(SkillDetail(r)))));
            Ui.Wrapped(row, SkillCatalog.Kinds[kindIndex].Tab, ColKind, 0, KindW, h, 16);
            Ui.Label(row, skill.GradeMin + "~" + skill.GradeMax, ColLevel, 0, 50, h, 16);
            Ui.Label(row, learned.ContainsKey(id) ? "예" : "-", ColOwned, 0, 60, h, 16);
            Ui.Label(row, BagBooks(id).ToString(), ColOwned + 70, 0, 60, h, 16).gameObject.name = "BookBag" + id;
            int amount = Math.Min(AmountBar.Get("item"), MartialBookService.MaxPerGive);
            Ui.Button("BookGive" + id, row, "지급 x" + amount, ColGive, 2, GiveW, RowH - 4,
                new Action(() => GiveBook(kindIndex, skill, BookGrade, bookMinLevel, AmountBar.Get("item"))), SkillCatalog.BlockReason(skill, BookGrade) == null);
        }

        private void DrawBookFavRow(RectTransform row, Row r, float h)
        {
            FavoriteStore.Favorite f = r.Book;
            int baseId = f.Id, grade = f.Grade, minLevel = f.MinLevel, kindIndex = f.BookKind;
            SkillCatalog.Entry skill = r.Skill;
            string tail = baseId + "_" + grade + "_" + minLevel;
            Ui.Button("FavBookStar" + tail, row, "★", ColStar, 2, 40, RowH - 4, new Action(() =>
            {
                string text;
                FavoriteStore.RemoveBook(baseId, grade, minLevel, out text);
                Say(text);
            }), true, true);
            WrapLabel(Ui.Button("FavBookName" + tail, row, BookFavName(r), ColName, 2, NameW, h - 4, new Action(() => detail.Show(BookFavDetail(r)))));
            Ui.Wrapped(row, "비급·" + SkillCatalog.Kinds[kindIndex].Tab, ColKind, 0, KindW, h, 16);
            Ui.Label(row, minLevel + "+", ColLevel, 0, 44, h, 16);
            Ui.Label(row, skill == null ? "-" : BagBooks(baseId).ToString(), ColOwned, 0, 60, h, 16);
            NumberField.Build(row, "FavBookAmount" + tail, ColAmount, (h - (RowH - 4)) / 2, AmountW, RowH - 4, f.Amount.ToString(), null, new Action<int>(v =>
            {
                string text;
                FavoriteStore.SetBookAmount(baseId, grade, minLevel, v, out text);
                Say(text);
            }));
            Ui.Button("FavBookGive" + tail, row, "지급 x" + f.Amount, ColGive, 2, GiveW, RowH - 4,
                new Action(() => GiveBook(kindIndex, skill, grade, minLevel, FavoriteStore.BookAmountOf(baseId, grade, minLevel))), skill != null);
        }

        private string BookFavName(Row r)
        {
            return FavoriteStore.BookLabel(r.Book.BookKind, r.Book.Id, r.Book.Grade, r.Book.MinLevel);
        }

        private string SkillDetail(Row r)
        {
            SkillCatalog.Entry skill = r.Skill;
            return skill.Name + "  [" + skill.BaseId + "]  " + SkillCatalog.Kinds[r.SkillKind].Tab + ", 경지 " + skill.GradeMin + "~" + skill.GradeMax + "\n"
                + MartialBookService.Describe(skill, BookGrade, bookMinLevel, AmountBar.Get("item")) + "\n" + skill.Desc;
        }

        private string BookFavDetail(Row r)
        {
            if (r.Skill == null)
            {
                return "[" + r.Book.Id + "] " + BookFavName(r) + "\n즐겨찾기에 남아 있지만 지금 게임의 기술 표에 없습니다(그 기술을 넣은 모드를 뺐을 수 있습니다). ★를 누르면 뺍니다.";
            }
            return BookFavName(r) + "  [" + r.Skill.BaseId + "]  " + SkillCatalog.Kinds[r.SkillKind].Tab + "\n"
                + MartialBookService.Describe(r.Skill, r.Book.Grade, r.Book.MinLevel, r.Book.Amount) + "\n" + r.Skill.Desc;
        }

        private void ToggleBook(int kindIndex, SkillCatalog.Entry skill)
        {
            string text;
            if (FavoriteStore.HasBook(skill.BaseId, BookGrade, bookMinLevel))
            {
                FavoriteStore.RemoveBook(skill.BaseId, BookGrade, bookMinLevel, out text);
            }
            else
            {
                FavoriteStore.AddBook(kindIndex, skill.BaseId, BookGrade, bookMinLevel, Math.Min(AmountBar.Get("item"), MartialBookService.MaxPerGive), out text);
            }
            Say(text);
        }

        private void GiveBook(int kindIndex, SkillCatalog.Entry skill, int grade, int minLevel, int amount)
        {
            if (skill == null)
            {
                return;
            }
            string text;
            MartialBookService.Make(Player, kindIndex, skill, grade, minLevel, amount, out text);
            Say(text);
        }
    }
}
```

- [ ] **Step 5: 두 구성으로 빌드한다**

Task 1 Step 6과 같다. Expected: 경고·오류 0, DLL 하나.

- [ ] **Step 6: 검사가 통과하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin book-1015g -Fixture base
pwsh tools/research/ui-check.ps1 -Part books-ui
pwsh tools/research/ui-check.ps1 -Part items
pwsh tools/research/ui-check.ps1 -Part groups
pwsh tools/research/ui-check.ps1 -Part favorites
pwsh tools/research/ui-check.ps1 -Part favorites-corrupt
pwsh tools/DevRemote/session.ps1 -End book-1015g
pwsh tools/DevRemote/session.ps1 -Begin book-1015h -Fixture base
pwsh tools/research/ui-check.ps1 -Part wrap
pwsh tools/research/ui-check.ps1 -Part base
pwsh tools/research/ui-check.ps1 -Part tabs
pwsh tools/research/ui-check.ps1 -Part books
pwsh tools/DevRemote/session.ps1 -End book-1015h
pwsh tools/DevRemote/session.ps1 -Begin book-1015i -Fixture base
pwsh tools/research/datebag-check.ps1 -Part bag
pwsh tools/DevRemote/session.ps1 -End book-1015i
```

Expected: 모든 부분 `FAILS=0`. `books`는 가방을 채우므로 그 회차의 마지막에 둔다.

캡처 `.devremote\check-books-ui.png`를 Read로 열어 본다. 볼 것은 셋이다.
- 왼쪽 그룹 열넷이 아래 설명 칸과 겹치지 않는가
- 필터 두 줄의 글이 겹치지 않는가
- 즐겨찾기 보기에 아이템 줄과 비급 줄이 함께 보이는가

- [ ] **Step 7: 커밋**

```bash
git add src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs tools/research/ui-check.ps1
git commit -m "feat: 자원 탭의 기술 서적 그룹 — 경지·최소 품질로 비급 지급, 즐겨찾기와 모두 지급의 비급 줄

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: 문서, 검토, 병합, 설치

**Files:**
- Modify: `docs/05-editor-v0.1.md`(39절), `docs/06-handoff.md`

- [ ] **Step 1: 05 문서에 39절을 쓴다**

`docs/05-editor-v0.1.md` 끝에 `## 39. 기술 서적(비급) 지급 (2026-10-15 추가)`를 더한다. 담을 것:
- `### 쓰는 법`: 자원 탭 `기술 서적` 그룹(종류 칩, 검색·이 경지만·초기화, 경지 1~10, 최소 품질 1~6, 표의 열), 지급(한 번에 20권, 한 권마다 옵션), ☆와 즐겨찾기 보기의 비급 줄, 모두 지급
- `### 규칙과 실측`: 스펙 2절의 실측 표를 줄여 옮기고(만들기, 같은 모양, 품질, 확률, 게임의 학습), `rskill`로 읽은 23116의 경지 범위, 즐겨찾기 파일의 `book` 줄
- `### 구조`: 파일, 요소 이름, 개발 명령(`rbook`, `rbooks`, `rbookstudy`, `rskill`, `rfav addbook|setbook|delbook`)
- `### 검증`: 회차 이름과 부분별 결과(고치기 전 실패와 고친 뒤 통과)
- `### 확인하지 못한 것, 한계`: 스펙 8절의 넷과 실행 중에 정한 것

- [ ] **Step 2: 인계 문서를 고친다**

`docs/06-handoff.md`:
- 1절 직전 작업에 기술 서적 지급(39절)을 더하고, 다음 일을 G2(8.3b절)로 되돌린다.
- 2절 표의 자원 줄에 "기술 서적(비급) 지급: 경지·최소 품질, 한 번에 20권", 코드에 `MartialBookService`, 실험 기록에 39절.
- 2절 공통 규칙의 `favorites.txt` 설명에 "비급 줄 `book …`".
- 5절 검사 표에 `books`(서비스, 가방을 채우므로 회차의 마지막), `books-ui`(화면)를 더하고, 개발 명령에 `rbook`, `rbooks`, `rbookstudy`, `rskill`, `rfav addbook|setbook|delbook`.
- 8.3c절의 제목을 "(끝남)"으로 바꾸고 첫 줄에 "05 문서 39절로 만들었다"를 더한다.
- 8.7절 회차 이름에 `res-1015q`와 `book-1015a`~(쓴 것까지).

- [ ] **Step 3: 문서를 커밋한다**

```bash
git add docs/05-editor-v0.1.md docs/06-handoff.md
git commit -m "docs: 기술 서적(비급) 지급(05 문서 39절), 인계 문서

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 4: 검토자 한 명이 브랜치 전체를 본다**

읽기 전용 서브에이전트 하나에게 맡긴다(가장 높은 모델). superpowers:requesting-code-review의 양식에, 아래를 넣는다.
- 범위 고정 문구
- 절대경로 지시
- 스펙·계획·진행 기록(`.superpowers/sdd/2026-10-15-martial-books/progress.md`)의 경로
- 이 계획의 Review Focus 다섯 줄 그대로

볼 것:
- 지급이 두 번 되거나 거른 줄 밖을 주는 경로
- 즐겨찾기 파일이 깨지거나 비급 줄과 아이템 줄이 섞이는 경로
- 공법 탭 기술 추가의 회귀(`RollValues`)
- IL2CPP 예외가 버튼 콜백 밖으로 나가는 곳
- 검사가 효과 대신 글만 보는 곳

- [ ] **Step 5: 검토 결과를 반영한다**

Critical·Important는 실패하는 검사를 먼저 넣고 고친 뒤, 해당 부분을 새 회차(`book-1015j`부터)에서 다시 돌린다. Minor는 05 문서 39절의 "확인하지 못한 것, 한계"에 적는다. 고친 파일만 골라 커밋하고, 제목은 `fix: 검토에서 나온 것 — `로 시작해 고친 결함들을 나열한다. 고칠 것이 없으면 건너뛰고 그렇게 보고한다.

- [ ] **Step 6: develop에 병합하고 설치한다**

```bash
git -C E:/TlToybox switch develop
git -C E:/TlToybox merge --no-ff feat/martial-books -m "Merge branch 'feat/martial-books' into develop"
```

사용자의 게임이 꺼져 있는지 확인하고(켜져 있으면 끄도록 알리고 기다린다) 설치한다:

```bash
pwsh tools/install.ps1
```

인계 문서 1절의 "게임에 설치된 것"에 설치 시각을 적는다. 이 고침은 `docs/handoff-1015c` 브랜치에서 커밋하고 `--no-ff`로 병합한다.

- [ ] **Step 7: 사용자에게 보고한다**

보고할 것:
- 바꾼 것과 검사 결과
- 직접 써 볼 것: 게임 화면에서 비급을 눌러 학습하기, 상점에 팔기, NPC에게 주기, 품질 6을 여러 권 줄 때 걸리는 시간
- 다음 일 G2
