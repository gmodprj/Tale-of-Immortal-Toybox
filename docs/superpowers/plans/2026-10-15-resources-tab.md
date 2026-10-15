# 자원 탭 개편 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 자원 탭의 아이템을 편집기가 정한 용도 그룹 11개로 나눠 보이고, 아이템과 수량을 저장하는 즐겨찾기(모두 지급 포함)를 더한다.

**Architecture:** 분류 표는 새 `ItemGroups`(게임 세부 분류 번호 → 그룹·종류)가 맡고, `ItemCatalog`가 아이템마다 그룹·종류를 붙여 거르기와 개수를 낸다. 즐겨찾기는 새 `FavoriteStore`가 `TlToybox\favorites.txt`에 원자적으로 쓴다. `ResourcesTab`은 그리기만 한다(왼쪽 그룹 목록, 오른쪽 종류 칩·필터·표·설명). 검증은 개발 명령(`rgroups`, `rfav`)과 게임 안 검사(`ui-check.ps1`)로 한다.

**Tech Stack:** C# 7.3 / .NET 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4, PowerShell 7 검증 스크립트, `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-15-resources-tab-design.md`

## Global Constraints

- 브랜치 `feat/resources-tab`(develop에서 만들었고 스펙 커밋 `670d4dd`가 있다). 끝에서 `git merge --no-ff`로 `develop`에 병합한다. `main`에는 병합하지 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 두 구성(개발 `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release`, 배포 `-p:DevTools=false -o <임시 폴더>`) 모두 오류 0·경고 0, `bin\Release`에 `MOD_sWP6UL.dll` 하나.
- 게임 검증은 `pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서 한다. 회차 이름은 `res-1015a`부터 차례로 쓰고 다시 쓰지 않는다. `-Begin`은 "WORLD READY", `-End`는 "SAVE RESTORED AND IDENTICAL"이 나와야 한다. 회차는 한 번에 하나, 검사도 한 번에 하나만 돌린다.
- 사용자의 게임이 켜져 있으면 `-Begin`이 멈춘다. 그때는 사용자에게 게임을 꺼 달라고 알리고 기다린다(임의로 끄지 않는다). 남은 `.devremote\mod-cmd.txt`는 지운다.
- 서비스(`Services/`)는 화면을 모른다. 지급은 지금의 경로(`SaveBackup.EnsureOnce` → `RewardPropItem` → 다시 읽어 판정 → `ChangeLog.Add`)를 그대로 쓴다. 즐겨찾기를 넣고 빼는 것은 변경 이력에 남기지 않는다.
- 즐겨찾기 파일: `ChangeLog.DataDir`의 `favorites.txt`, UTF-8, 한 줄에 `<아이템 번호> <수량>`, 수량 1~999999(`AmountBar.Max`). 쓰기는 `favorites.txt.tmp`에 쓰고 바꿔치기. 깨진 줄이 있던 파일은 처음 덮어쓰기 전에 `favorites.txt.bak`.
- 요소 이름(스펙 3.4절): 그룹 `ResGroup<Fav|All|Pill|Talisman|Growth|Material|Book|Equip|Cloth|Collect|Quest|Other>`, 종류 칩 `ResKind<순번>`(0이 전체), 품질 `ResQuality<0~6>`, `ResOwnedOnly`, `ResClear`, 별 `ResStar<번호>`, 수량 칸 `ResAmount<번호>`, `ResAllGive`, 표 머리 `ResHead`. 그대로 쓰는 이름: `ItemQuery`(`ItemQueryClear`), `ItemTable`(`ItemTableRange`·`ItemTableNext`…), `ItemName<번호>`, `ItemDesc<번호>`, `ItemGive<번호>`, `ItemDetail`, `MoneyAdd`, `BagExtra`, `BagApply`, `BagMinus`, `BagPlus`, 수량 막대 `money*`·`item*`. 없애는 이름: `ItemCat*`, `ItemSub*`, `ItemSubs`, `ItemSearchClear`, `ItemHead`.
- 품질은 게임의 `level`(1~6, 높을수록 귀하다). 6품 보원단 `1011011`이 품질 1, 5품 보원단 `1011012`가 품질 2다.
- 문서에는 어셈블리나 게임에서 확인한 이름만 쓰고, 확인하지 못한 것은 "미확인"과 사유를 적는다.
- PowerShell 함정(인계 6절): 변수 이름은 대소문자를 구분하지 않는다. 큰따옴표 안 변수 뒤 한글은 `$($x)개`. `check-lib.ps1`의 `$log`, `$data`, `$work`와 같은 이름의 변수를 만들지 않는다. 새 함수 이름이 별칭과 겹치지 않는지 `Get-Alias <이름>`으로 본다.

## Review Focus

1. 게임에 없는 번호가 즐겨찾기에 있는데 품질 같은 필터가 걸려 있다 → 그 줄은 늘 보이고 ★로 뺄 수 있어야 한다. (Task 3 `favorites-corrupt`의 "a missing item stays visible under a filter")
2. 즐겨찾기 보기에서 필터로 일부만 보이는 채 `모두 지급` → 보이는 줄만 준다. (Task 3 `favorites`의 "give all gives only the rows shown")
3. 검색 칸에 실제 키로 치는 동안 왼쪽 그룹 개수가 바뀐다 → 탭을 다시 그리지 않아 검색 칸이 포커스를 잃지 않는다. (Task 3 `groups`의 "typing keeps the focus while counts change")
4. 즐겨찾기 파일이 다른 프로그램에 잠겨 읽히지 않는다 → 빈 목록으로 보이고, 그 상태에서 넣어도 파일을 덮어쓰지 않는다. (Task 2 `favstore`의 "a locked file is not overwritten")
5. 수량 0·1000000, 이미 있는 번호를 다시 넣기, 파일의 수량 5000000 → 앞의 셋은 거부, 마지막은 999999로 맞춘다. (Task 2 `favstore`)

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `src/ModCode/ModMain/Services/ItemGroups.cs` | 새로 | 용도 그룹·종류의 표(스펙 4절), 세부 분류 번호 → (그룹, 종류), 의복의 성별, `기타`의 종류 |
| `src/ModCode/ModMain/Services/ItemCatalog.cs` | 수정 | `Entry`에 그룹·종류·표 순번·`Missing`, `Filter`, `Select`, `Passes`, `CountByGroup`, `CountByKind`, `Get`, `OwnedIds`. `Find`는 표 순서를 지킨다. Task 3에서 `MainCategories`·`SubCategories`를 지운다 |
| `src/ModCode/ModMain/Services/FavoriteStore.cs` | 새로 | 즐겨찾기 파일의 읽기·쓰기, 건너뛴 줄 안내 |
| `src/ModCode/ModMain/Services/EditService.cs` | 수정 | `TryGiveItem`(성공 여부를 돌려주는 지급). `GiveItem`이 그것을 부른다 |
| `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs` | 다시 씀 | 스펙 3.4절의 화면 |
| `src/ModCode/ModMain/Dev/DevCommandsResources.cs` | 새로 | `rgroups`, `rfav [reset|reload|add|set|del]` |
| `src/ModCode/ModMain/Dev/DevCommands.cs` | 수정 | 위 명령 연결 |
| `tools/DevRemote/session.ps1`, `session.tests.ps1` | 수정 | `favorites.txt`(와 `.bak`) 보관·되돌리기, 그 시험 |
| `tools/research/ui-check.ps1` | 수정 | 부분 `groups`, `favstore`, `favstore-restart`, `favorites`, `favorites-corrupt`(새), `items`·`wrap`(고침) |
| `docs/05-editor-v0.1.md`, `docs/06-handoff.md` | 수정 | 38절, 인계 문서 |

스펙 7절의 `favorites-restart`는 파일 단의 `favstore-restart`(Task 2)가 맡는다. 재시작 뒤 화면에 나오는지는 Task 3의 `favorites`가 그 회차의 파일로 본다.

---

### Task 1: 용도 그룹 표와 카탈로그

**Files:**
- Create: `src/ModCode/ModMain/Services/ItemGroups.cs`, `src/ModCode/ModMain/Dev/DevCommandsResources.cs`
- Modify: `src/ModCode/ModMain/Services/ItemCatalog.cs`(전체 교체), `src/ModCode/ModMain/Dev/DevCommands.cs`(`Execute`의 맨 앞), `tools/research/ui-check.ps1`(부분 `groups`)

**Interfaces:**
- Produces:
  - `ItemGroups.Group { string Key; string Name; int Order; List<Kind> Kinds; }`, `ItemGroups.Kind { string Name; List<int> Classes; int Order; }`
  - `ItemGroups.FavKey = "Fav"`, `AllKey = "All"`, `OtherKey = "Other"`, `ItemGroups.Groups : List<Group>`(표의 11개, 순서대로), `ItemGroups.ByKey(string) : Group`, `ItemGroups.Duplicates : int`, `ItemGroups.Place(int className, int itemId, out Group, out Kind)`, `ItemGroups.SortOther()`
  - `ItemCatalog.Entry`에 `ItemGroups.Group Group; ItemGroups.Kind Kind; int TableIndex; bool Missing;`
  - `ItemCatalog.Filter { string Query; string Group; ItemGroups.Kind Kind; int Quality; bool OwnedOnly; string Needle; bool IsId; int Id; }`
  - `ItemCatalog.Select(Filter, HashSet<int> owned) : List<Entry>`, `Passes(Entry, Filter, HashSet<int>) : bool`, `CountByGroup(Filter, HashSet<int>) : Dictionary<string,int>`(키는 그룹 키와 `AllKey`), `CountByKind(Group, Filter, HashSet<int>) : int[]`(그룹의 `Kinds` 순서), `Get(int id) : Entry`(없으면 null), `OwnedIds(WorldUnitBase) : HashSet<int>`, `CategoryName(int) : string`(internal)
  - 개발 명령 `rgroups` → `total=<전체> other=<기타 수> dupes=<중복 번호 수> sum=<그룹 합> otherClasses=<번호,…> | Pill=<수> Talisman=<수> …`

- [ ] **Step 1: 검사를 먼저 쓴다**

`tools/research/ui-check.ps1`의 `switch ($Part) {` 바로 아래(부분 `'base'` 앞)에 넣는다:

```powershell
  'groups' {
    # 자원 탭 개편 스펙 7절: 용도 그룹의 분류 표. 화면 쪽 확인은 Task 3에서 이 부분의 끝에 더한다.
    $traps = Traps
    $g = Cmd 'rgroups' 600
    $total = if ($g -match 'total=(\d+) ') { [int]$Matches[1] } else { -1 }
    $sum = if ($g -match ' sum=(\d+) ') { [int]$Matches[1] } else { -2 }
    $pill = if ($g -match ' Pill=(\d+)') { [int]$Matches[1] } else { -1 }
    $cloth = if ($g -match ' Cloth=(\d+)') { [int]$Matches[1] } else { -1 }
    Check 'every item lands in a named group (no Other in the plain game)' ($g -match ' other=0 ') $g
    Check 'the table has no duplicate class numbers' ($g -match ' dupes=0 ') $g
    Check 'the groups add up to the whole list' ($total -gt 3000 -and $sum -eq $total) $g
    Check 'pills are the biggest group' ($pill -gt 1000) $g
    Check 'clothes are their own group' ($cloth -gt 200) $g
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

회차를 연 뒤에 코드를 고친다(`-Begin`이 지금 코드로 빌드한다).

```bash
pwsh tools/DevRemote/session.ps1 -Begin res-1015a -Fixture base
pwsh tools/research/ui-check.ps1 -Part groups
pwsh tools/DevRemote/session.ps1 -End res-1015a
```

Expected: `rgroups`가 `UNKNOWN`이라 앞의 다섯 줄이 FAIL, 마지막 줄 `FAILS=5`. `-End`가 "SAVE RESTORED AND IDENTICAL".

- [ ] **Step 3: 분류 표를 쓴다**

`src/ModCode/ModMain/Services/ItemGroups.cs`:

```csharp
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 자원 탭의 용도 그룹. 게임의 세부 분류 번호(ItemProps.className)를 편집기가 정한 (그룹, 종류)로 옮긴다.
    // 표와 근거: docs/superpowers/specs/2026-10-15-resources-tab-design.md 4절.
    // 표에 없는 번호는 "기타"에 게임 분류 이름으로 들어간다(창작마당 모드가 새 분류로 추가한 아이템 등).
    internal static class ItemGroups
    {
        public sealed class Kind
        {
            public string Name;
            public readonly List<int> Classes = new List<int>();
            public int Order;       // 그룹 안의 순서(0부터)
        }

        public sealed class Group
        {
            public string Key;
            public string Name;
            public int Order;
            public readonly List<Kind> Kinds = new List<Kind>();
        }

        public const string FavKey = "Fav";
        public const string AllKey = "All";
        public const string OtherKey = "Other";
        private const string ClothKey = "Cloth";
        private const int ClothClass = 304;     // 의복. 종류는 의복 표(ClothItem)의 성별로 가른다

        // 그룹 키, 그룹 이름, 종류들("이름:번호,번호"). 의복의 종류는 번호 없이 이름만 둔다(Place가 성별로 고른다).
        private static readonly string[][] Table =
        {
            new[] { "Pill", "단약", "회복:101", "전투:120", "독단:102", "돌파:106", "수명·건강·매력:103", "영약:104", "영과(영근):105", "도심과:629", "심성(독약):110", "대비무:108", "호요:801,802,803,804" },
            new[] { "Talisman", "부적", "전투 부적:111", "지도 부적:112,611", "심령·인요부:633", "그 밖의 부적:608,625", "전투 도구:7" },
            new[] { "Growth", "돌파·성장", "천재지보:508", "영기·영주:505", "조각:507", "도계석:509", "본원지기·선천일기:536,537,653", "명종:535", "선법 습득:6", "체질 개선:613", "비법:631,635", "천명 전용:660,661,662,663,664" },
            new[] { "Material", "제작 재료", "약재:503,107,601", "광재:501,526", "요수 재료:502,512,525", "법보 재료:504,510,513,518,520,521,522,523,524,527,528,530,534", "기령 재료:531,532,533", "의복 재료:511", "선재:612,506" },
            new[] { "Book", "기예 서적", "단방:605", "단로:606", "약보:615", "풍수서:614,657", "도전통론:616", "감식서:617", "연기보:618", "부적 도보:658", "도면·지도:632,630" },
            new[] { "Equip", "장비", "반지:301", "좌기:302", "법보:401", "호신부:627" },
            new[] { ClothKey, "의복", "남", "여", "그 밖" },
            new[] { "Collect", "수집품", "석기:701", "옥기:702", "악기:703,704,705,706", "장신구:707,610", "도기:708", "도감:709", "골기:710", "술·음식:604,634,655", "보물상자:644" },
            new[] { "Quest", "임무·사건", "서신·잔권:602,609,639,649,652", "기억:638", "영물:636,651,659", "비보:637", "선사 물품:656", "그 밖의 임무 물건:2,109,529,603,607,619,624,626,628,640,641,642,643,645,646,647,648,650,654" },
            new[] { OtherKey, "기타" },
        };

        private static List<Group> groups;
        private static Dictionary<int, Kind> byClass;
        private static Dictionary<Kind, Group> owner;
        private static int duplicates;

        private static void Build()
        {
            if (groups != null)
            {
                return;
            }
            var built = new List<Group>();
            byClass = new Dictionary<int, Kind>();
            owner = new Dictionary<Kind, Group>();
            duplicates = 0;
            foreach (string[] row in Table)
            {
                var group = new Group { Key = row[0], Name = row[1], Order = built.Count };
                for (int i = 2; i < row.Length; i++)
                {
                    string[] parts = row[i].Split(':');
                    var kind = new Kind { Name = parts[0], Order = group.Kinds.Count };
                    if (parts.Length > 1)
                    {
                        foreach (string number in parts[1].Split(','))
                        {
                            int id = int.Parse(number);
                            kind.Classes.Add(id);
                            if (byClass.ContainsKey(id))
                            {
                                duplicates++;
                            }
                            byClass[id] = kind;
                        }
                    }
                    group.Kinds.Add(kind);
                    owner[kind] = group;
                }
                built.Add(group);
            }
            groups = built;
        }

        // 표의 그룹들(즐겨찾기·전체는 화면만의 것이라 여기 없다).
        public static List<Group> Groups
        {
            get
            {
                Build();
                return groups;
            }
        }

        // 표에 같은 번호가 두 번 나온 수. 0이어야 한다(검사 rgroups).
        public static int Duplicates
        {
            get
            {
                Build();
                return duplicates;
            }
        }

        public static Group ByKey(string key)
        {
            Build();
            return groups.Find(x => x.Key == key);
        }

        // 아이템 하나의 (그룹, 종류). 의복은 의복 표의 성별로, 표에 없는 번호는 "기타"의 그 분류 이름 종류로.
        public static void Place(int className, int itemId, out Group group, out Kind kind)
        {
            Build();
            if (className == ClothClass)
            {
                group = ByKey(ClothKey);
                ConfClothItemItem cloth = g.conf.clothItem.GetItem(itemId);
                kind = group.Kinds[cloth == null ? 2 : cloth.sex == 1 ? 0 : 1];
                return;
            }
            if (byClass.TryGetValue(className, out kind))
            {
                group = owner[kind];
                return;
            }
            group = ByKey(OtherKey);
            string name = ItemCatalog.CategoryName(className);
            kind = group.Kinds.Find(k => k.Name == name);
            if (kind == null)
            {
                kind = new Kind { Name = name, Order = group.Kinds.Count };
                group.Kinds.Add(kind);
                owner[kind] = group;
            }
            if (!kind.Classes.Contains(className))
            {
                kind.Classes.Add(className);
            }
        }

        // "기타"의 종류를 가장 작은 세부 분류 번호 순으로 놓는다. 카탈로그를 다 만든 뒤 한 번 부른다.
        public static void SortOther()
        {
            Group other = ByKey(OtherKey);
            other.Kinds.Sort((a, b) => Smallest(a).CompareTo(Smallest(b)));
            for (int i = 0; i < other.Kinds.Count; i++)
            {
                other.Kinds[i].Order = i;
            }
        }

        private static int Smallest(Kind kind)
        {
            int min = int.MaxValue;
            foreach (int c in kind.Classes)
            {
                if (c < min)
                {
                    min = c;
                }
            }
            return min;
        }
    }
}
```

`g`는 게임의 전역 클래스다. 지역 변수의 이름으로 `g`를 쓰지 않는다.

- [ ] **Step 4: 카탈로그를 고친다**

`src/ModCode/ModMain/Services/ItemCatalog.cs`를 통째로 바꾼다. `MainCategories`·`SubCategories`는 지금의 자원 탭이 쓰므로 Task 3까지 남긴다. `Find`는 기예 탭(`ArtService.Books`)과 개발 명령 `gridfill`(`DevCommandsTime.Fill`)이 쓰고 게임 표 순서에 기대므로 표 순서로 돌려준다.

```csharp
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 지급할 수 있는 아이템의 목록. 게임의 아이템 표(ItemProps)와 분류표(ItemType)를 한 번 읽어 둔다.
    // 자원 탭은 편집기의 용도 그룹(ItemGroups)으로 거른다(Select). 기예 탭과 개발 명령은 게임의 분류 번호로 거른다(Find).
    internal static class ItemCatalog
    {
        public class Entry
        {
            public int Id;
            public string Name;
            public string Desc;
            public int Level;
            public int Type;        // 대분류 (ItemProps.type)
            public int SubType;     // 세부 분류 (ItemProps.className)
            public string TypeName;
            public string SubTypeName;
            public string Search;   // 공백과 기호를 뺀 이름
            public ItemGroups.Group Group;  // 자원 탭의 용도 그룹(화폐 목록에서는 null)
            public ItemGroups.Kind Kind;
            public int TableIndex;  // 게임 표에서의 순번
            public bool Missing;    // 즐겨찾기에 있지만 게임에 없는 번호(자원 탭이 만드는 자리표시)
        }

        public class Category
        {
            public int Id;
            public string Name;
            public int Count;
        }

        // 자원 탭의 거르기. Group은 그룹 키(ItemGroups.AllKey면 전체), Kind가 null이면 그룹 전체.
        public sealed class Filter
        {
            private string query = "";

            public string Group = ItemGroups.AllKey;
            public ItemGroups.Kind Kind;
            public int Quality;         // 0이면 전체, 1~6(게임의 level)
            public bool OwnedOnly;

            public string Needle { get; private set; } = "";
            public bool IsId { get; private set; }
            public int Id { get; private set; }

            // 이름의 일부 또는 번호. 넣을 때 공백과 기호를 뺀 검색어를 한 번 만들어 둔다.
            public string Query
            {
                get { return query; }
                set
                {
                    query = value ?? "";
                    Needle = EditService.Compact(query);
                    int id;
                    IsId = int.TryParse(Needle, out id);
                    Id = id;
                }
            }
        }

        private const int ClothClass = 304;   // ItemProps.className: 의복

        private static List<Entry> all;
        private static List<Entry> currencies;
        private static List<Category> mainCategories;
        private static Dictionary<int, Entry> byId;

        internal static string CategoryName(int id)
        {
            ConfItemTypeItem item = g.conf.itemType.GetItem(id);
            if (item == null)
            {
                return "분류 " + id;
            }
            string name = GameTool.LS(item.name);
            // 번역이 없는 분류는 키가 그대로 돌아온다.
            return string.IsNullOrEmpty(name) || name == item.name ? "분류 " + id : name;
        }

        private static void Build()
        {
            all = new List<Entry>();
            currencies = new List<Entry>();
            byId = new Dictionary<int, Entry>();
            var counts = new Dictionary<int, int>();
            var items = g.conf.itemProps._allConfList;
            for (int i = 0; i < items.Count; i++)
            {
                ConfItemPropsItem item = items[i];
                string name = GameTool.LS(item.name);
                // 재화(hide)는 아이템 표에 넣지 않는다. 그 가운데 일반 재화(type 0)는 화폐 목록으로 따로 모은다.
                // 천원산 안에서만 쓰는 재화(type 5: 신석, 현옥)와 임무용 물건은 넣지 않는다.
                if (item.hide != 0)
                {
                    if (item.type == 0)
                    {
                        string label = MartialService.CleanDesc(GameTool.LS(item.desc)).Replace("\r", "").Replace('\n', ' ');
                        // 이름의 번역이 없는 재화(등선폐)는 설명이 곧 이름이다.
                        bool named = !string.IsNullOrEmpty(name) && name != item.name;
                        currencies.Add(new Entry { Id = item.id, Name = named ? name : label, Desc = label, Level = item.level, Type = item.type, SubType = item.className });
                    }
                    continue;
                }
                if (string.IsNullOrEmpty(name) || name == item.name)
                {
                    continue;
                }
                string desc = MartialService.CleanDesc(GameTool.LS(item.desc)).Replace("\r", "").Replace('\n', ' ');
                // 의복은 게임의 이름과 설명이 모두 "의복"이다. 성별·모양·색으로 구분되게 한다(의복 표 ClothItem).
                ConfClothItemItem cloth = item.className == ClothClass ? g.conf.clothItem.GetItem(item.id) : null;
                if (cloth != null)
                {
                    string sex = cloth.sex == 1 ? "남" : "여";
                    desc = name + " — " + sex + "성용, 모양 " + cloth.style + ", 색 " + cloth.color;
                    name = name + "(" + sex + ") " + cloth.style + "-" + cloth.color;
                }
                var entry = new Entry
                {
                    Id = item.id,
                    Name = name,
                    Desc = desc,
                    Level = item.level,
                    Type = item.type,
                    SubType = item.className,
                    TypeName = CategoryName(item.type),
                    SubTypeName = CategoryName(item.className),
                    Search = EditService.Compact(name),
                    TableIndex = all.Count,
                };
                ItemGroups.Group group;
                ItemGroups.Kind kind;
                ItemGroups.Place(item.className, item.id, out group, out kind);
                entry.Group = group;
                entry.Kind = kind;
                all.Add(entry);
                byId[entry.Id] = entry;
                int count;
                counts.TryGetValue(item.type, out count);
                counts[item.type] = count + 1;
            }
            ItemGroups.SortOther();
            // 그룹 순서, 그룹 안의 종류 순서, 게임 표 순서. List.Sort는 같은 값의 순서를 지키지 않으므로 표의 순번까지 비교한다.
            all.Sort((a, b) =>
            {
                int c = a.Group.Order.CompareTo(b.Group.Order);
                if (c == 0)
                {
                    c = a.Kind.Order.CompareTo(b.Kind.Order);
                }
                return c != 0 ? c : a.TableIndex.CompareTo(b.TableIndex);
            });
            mainCategories = new List<Category>();
            foreach (var pair in counts)
            {
                mainCategories.Add(new Category { Id = pair.Key, Name = CategoryName(pair.Key), Count = pair.Value });
            }
            mainCategories.Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        private static List<Entry> All
        {
            get
            {
                if (all == null)
                {
                    Build();
                }
                return all;
            }
        }

        // 영석을 포함한 일반 재화(종문 공헌, 성주령, 전공 등). 가방 칸을 차지하지 않고 수량만 있다.
        public static List<Entry> Currencies
        {
            get
            {
                if (currencies == null)
                {
                    Build();
                }
                return currencies;
            }
        }

        // 지금의 자원 탭이 쓴다. Task 3(자원 탭 개편)에서 지운다.
        public static List<Category> MainCategories
        {
            get
            {
                if (mainCategories == null)
                {
                    Build();
                }
                return mainCategories;
            }
        }

        // 대분류 안의 세부 분류 목록(아이템이 있는 것만). 지금의 자원 탭이 쓴다. Task 3에서 지운다.
        public static List<Category> SubCategories(int mainType)
        {
            var counts = new Dictionary<int, Category>();
            foreach (Entry entry in All)
            {
                if (entry.Type != mainType)
                {
                    continue;
                }
                Category category;
                if (!counts.TryGetValue(entry.SubType, out category))
                {
                    category = new Category { Id = entry.SubType, Name = entry.SubTypeName };
                    counts[entry.SubType] = category;
                }
                category.Count++;
            }
            var result = new List<Category>(counts.Values);
            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            // 게임의 분류표에는 이름이 같은 세부 분류가 여럿 있다. 번호를 붙여 구분한다.
            var seen = new Dictionary<string, int>();
            foreach (Category category in result)
            {
                int n;
                seen.TryGetValue(category.Name, out n);
                seen[category.Name] = n + 1;
            }
            foreach (Category category in result)
            {
                if (seen[category.Name] > 1)
                {
                    category.Name += "#" + category.Id;
                }
            }
            return result;
        }

        // 조건에 맞는 아이템을 게임 표 순서로. query는 이름의 일부 또는 번호. mainType과 subType은 음수면 전체.
        public static List<Entry> Find(string query, int mainType, int subType)
        {
            string needle = EditService.Compact(query ?? "");
            int id;
            bool isId = int.TryParse(needle, out id);
            var result = new List<Entry>();
            foreach (Entry entry in All)
            {
                if (mainType >= 0 && entry.Type != mainType)
                {
                    continue;
                }
                if (subType >= 0 && entry.SubType != subType)
                {
                    continue;
                }
                if (needle.Length > 0 && !entry.Search.Contains(needle) && !(isId && entry.Id == id))
                {
                    continue;
                }
                result.Add(entry);
            }
            result.Sort((a, b) => a.TableIndex.CompareTo(b.TableIndex));
            return result;
        }

        // 그 번호의 아이템. 게임에 없거나 목록에 넣지 않은 번호면 null.
        public static Entry Get(int id)
        {
            if (all == null)
            {
                Build();
            }
            Entry entry;
            return byId.TryGetValue(id, out entry) ? entry : null;
        }

        // 그 인물의 가방에 있는 아이템 번호들(보유 중만 거르기에 쓴다). 가방을 한 번 훑어 모은다.
        public static HashSet<int> OwnedIds(WorldUnitBase unit)
        {
            var result = new HashSet<int>();
            var props = unit.data.unitData.propData.allProps;
            for (int i = 0; i < props.Count; i++)
            {
                if (props[i].propsCount > 0)
                {
                    result.Add(props[i].propsID);
                }
            }
            return result;
        }

        // 그룹·종류를 빼고 본 거르기(검색어, 품질, 보유 중만). 게임에 없는 번호의 자리표시는 늘 통과한다(뺄 수 있게).
        public static bool Passes(Entry entry, Filter filter, HashSet<int> owned)
        {
            if (entry.Missing)
            {
                return true;
            }
            if (filter.Quality > 0 && entry.Level != filter.Quality)
            {
                return false;
            }
            if (filter.OwnedOnly && (owned == null || !owned.Contains(entry.Id)))
            {
                return false;
            }
            return filter.Needle.Length == 0 || entry.Search.Contains(filter.Needle) || (filter.IsId && entry.Id == filter.Id);
        }

        // 자원 탭의 목록: 그룹(전체면 모두)과 종류 안에서 거르기를 통과한 것. 순서는 그룹·종류·게임 표 순서.
        public static List<Entry> Select(Filter filter, HashSet<int> owned)
        {
            var result = new List<Entry>();
            foreach (Entry entry in All)
            {
                if (filter.Group != ItemGroups.AllKey && entry.Group.Key != filter.Group)
                {
                    continue;
                }
                if (filter.Kind != null && entry.Kind != filter.Kind)
                {
                    continue;
                }
                if (Passes(entry, filter, owned))
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        // 그룹 키마다 거르기(검색어, 품질, 보유 중만)를 통과한 수. AllKey에는 전체의 수.
        public static Dictionary<string, int> CountByGroup(Filter filter, HashSet<int> owned)
        {
            List<Entry> entries = All;
            var counts = new Dictionary<string, int>();
            counts[ItemGroups.AllKey] = 0;
            foreach (ItemGroups.Group group in ItemGroups.Groups)
            {
                counts[group.Key] = 0;
            }
            foreach (Entry entry in entries)
            {
                if (Passes(entry, filter, owned))
                {
                    counts[ItemGroups.AllKey]++;
                    counts[entry.Group.Key]++;
                }
            }
            return counts;
        }

        // 그 그룹의 종류마다 거르기를 통과한 수(그룹의 Kinds 순서).
        public static int[] CountByKind(ItemGroups.Group group, Filter filter, HashSet<int> owned)
        {
            List<Entry> entries = All;
            var counts = new int[group.Kinds.Count];
            foreach (Entry entry in entries)
            {
                if (entry.Group == group && Passes(entry, filter, owned))
                {
                    counts[entry.Kind.Order]++;
                }
            }
            return counts;
        }
    }
}
```

`CountByGroup`·`CountByKind`가 `All`을 먼저 읽는 것은 카탈로그를 만들면서 `기타`의 종류가 생기기 때문이다.

- [ ] **Step 5: 개발 명령**

`src/ModCode/ModMain/Dev/DevCommandsResources.cs`:

```csharp
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 자원 탭의 검증용 명령(tools/research/ui-check.ps1의 groups·favstore·favorites 부분).
    //   rgroups : total=<전체> other=<기타 수> dupes=<표의 중복 번호 수> sum=<그룹 합> otherClasses=<번호,…> | <그룹 키>=<수> …
    internal static class DevCommandsResources
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "rgroups":
                    return Groups();
                default:
                    return null;
            }
        }

        private static string Groups()
        {
            Dictionary<string, int> counts = ItemCatalog.CountByGroup(new ItemCatalog.Filter(), null);
            int sum = 0;
            var sb = new StringBuilder();
            foreach (ItemGroups.Group group in ItemGroups.Groups)
            {
                sum += counts[group.Key];
                sb.Append(' ').Append(group.Key).Append('=').Append(counts[group.Key]);
            }
            var others = new List<string>();
            foreach (ItemGroups.Kind kind in ItemGroups.ByKey(ItemGroups.OtherKey).Kinds)
            {
                foreach (int c in kind.Classes)
                {
                    others.Add(c.ToString());
                }
            }
            return "total=" + counts[ItemGroups.AllKey] + " other=" + counts[ItemGroups.OtherKey] + " dupes=" + ItemGroups.Duplicates
                + " sum=" + sum + " otherClasses=" + string.Join(",", others.ToArray()) + " |" + sb;
        }
    }
}
```

`src/ModCode/ModMain/Dev/DevCommands.cs`의 `Execute` 맨 앞(`DevCommandsArtifact.Execute` 앞)에 넣는다:

```csharp
            string resources = DevCommandsResources.Execute(p);
            if (resources != null)
            {
                return resources;
            }
```

- [ ] **Step 6: 두 구성으로 빌드한다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo -p:DevTools=false -o "$TEMP/tltoybox-release-check"
ls src/ModCode/ModMain/bin/Release
```

Expected: 두 번 모두 `0 Warning(s)`, `0 Error(s)`. `bin/Release`에 `MOD_sWP6UL.dll` 하나.

- [ ] **Step 7: 검사가 통과하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin res-1015b -Fixture base
pwsh tools/research/ui-check.ps1 -Part groups
pwsh tools/research/ui-check.ps1 -Part items
pwsh tools/DevRemote/session.ps1 -End res-1015b
```

Expected: `groups`가 `FAILS=0`. `items`(지금의 자원 탭)도 `FAILS=0`(카탈로그 순서가 바뀌어도 목록·검색·지급이 그대로인지).

`other=0`이 아니면 출력의 `otherClasses`에 나온 번호를 게임 표 사본(`E:\Games\Tale of Immortal\Mod\modFQA\配置修改教程\配置（只读）Json格式\ItemType.json`의 `name` → `LocalText.json`의 `kr`)에서 찾아, 이름이 맞는 그룹·종류에 넣는다. 맞는 곳이 없으면 `Quest`의 "그 밖의 임무 물건"에 넣는다. 그 번호와 둔 자리를 Task 4의 05 문서 38절에 적고 사용자에게 알린다. 그런 뒤 `res-1015b2`로 다시 돌린다.

- [ ] **Step 8: 커밋**

```bash
git add src/ModCode/ModMain/Services/ItemGroups.cs src/ModCode/ModMain/Services/ItemCatalog.cs src/ModCode/ModMain/Dev/DevCommandsResources.cs src/ModCode/ModMain/Dev/DevCommands.cs tools/research/ui-check.ps1
git commit -m "feat: 자원 탭의 용도 그룹 표와 카탈로그의 거르기·개수

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 즐겨찾기 저장소와 회차의 보관

**Files:**
- Create: `src/ModCode/ModMain/Services/FavoriteStore.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsResources.cs`(`rfav`), `tools/DevRemote/session.ps1`, `tools/DevRemote/session.tests.ps1`, `tools/research/ui-check.ps1`(부분 `favstore`, `favstore-restart`)

**Interfaces:**
- Consumes: Task 1의 `ItemCatalog.Get(int)`.
- Produces:
  - `FavoriteStore.Favorite { int Id; int Amount; }`, `FavoriteStore.MaxAmount = 999999`
  - `FavoriteStore.Entries : List<Favorite>`(넣은 순서), `Has(int) : bool`, `AmountOf(int) : int`(없으면 0), `SkippedLines : int`
  - `FavoriteStore.Add(int id, int amount, out string text) : bool`, `Remove(int id, out string text) : bool`, `SetAmount(int id, int amount, out string text) : bool`
  - `FavoriteStore.TakeNotice() : string`(건너뛴 줄이나 읽기 오류가 있으면 한 번만 문장, 아니면 null), `FavoriteStore.Forget()`(다음에 쓸 때 다시 읽는다. 개발 명령용)
  - 문장: 수량 규칙 `즐겨찾기: 수량은 1~999999 사이의 숫자로 입력하세요.`, 안내 `즐겨찾기 파일에서 읽지 못한 줄 n개를 건너뛰었습니다.`, 잠긴 파일 `즐겨찾기: 파일을 읽지 못해 바꾸지 않았습니다(<이유>).`
  - 로그(영어, `ModWarnings`가 잡는다): `[TlToybox] favorites: skipped <n> lines in favorites.txt`, `[TlToybox] favorites: could not read favorites.txt: <이유>`. 스펙 3.3절은 `MelonLogger.Warning`이라 했지만, 이 저장소의 경고는 모두 `MelonLogger.Msg`에 `[TlToybox] ` 머리를 붙이고 검사의 `ModWarnings`가 그 모양으로 고른다. 그 관례를 따른다. 로그의 한글은 CP949라 검사가 찾을 수 없으므로 영어로 쓴다.
  - 개발 명령 `rfav [reset|reload|add <번호> <수량>|set <번호> <수량>|del <번호>]` → (동작이 있으면 `<문장> | `) + `count=<수> skipped=<수> items=<번호>:<수량>[:missing],…`

- [ ] **Step 1: 회차 스크립트의 시험을 먼저 쓴다**

`tools/DevRemote/session.tests.ps1`의 끝 정리(`foreach ($root in $boxes) {`) 바로 앞에 넣는다:

```powershell
# 21. 즐겨찾기 파일: -Begin이 보관하고 -End가 되돌린다. 회차가 만든 .bak과 .tmp는 지운다
$b = NewBox
[IO.File]::WriteAllText("$($b.data)\favorites.txt", "1011011 10`n")
[void](Run $b $none @{ Begin = 'x' })
[IO.File]::WriteAllText("$($b.data)\favorites.txt", "garbage`n")
[IO.File]::WriteAllText("$($b.data)\favorites.txt.bak", "made by a check`n")
[IO.File]::WriteAllText("$($b.data)\favorites.txt.tmp", "left over`n")
$o = Run $b $none @{ End = 'x' }
Check 'End restores favorites' ($o -match 'FAVORITES RESTORED' -and [IO.File]::ReadAllText("$($b.data)\favorites.txt") -eq "1011011 10`n") $o
Check 'End removes the favorites .bak and .tmp the session made' (-not (Test-Path "$($b.data)\favorites.txt.bak") -and -not (Test-Path "$($b.data)\favorites.txt.tmp")) $o

# 22. 시작할 때 즐겨찾기 파일이 없었으면 끝날 때 없앤다
$b = NewBox
[void](Run $b $none @{ Begin = 'x' })
[IO.File]::WriteAllText("$($b.data)\favorites.txt", "1011011 10`n")
$o = Run $b $none @{ End = 'x' }
Check 'End removes a favorites file the session created' ($o -match 'FAVORITES RESTORED' -and -not (Test-Path "$($b.data)\favorites.txt")) $o

# 23. 회차 전부터 있던 favorites.txt.bak은 회차 전의 내용으로 되돌린다
$b = NewBox
[IO.File]::WriteAllText("$($b.data)\favorites.txt.bak", "old backup`n")
[void](Run $b $none @{ Begin = 'x' })
[IO.File]::WriteAllText("$($b.data)\favorites.txt.bak", "overwritten by a check`n")
$o = Run $b $none @{ End = 'x' }
Check 'End puts back a favorites .bak that was there before' ([IO.File]::ReadAllText("$($b.data)\favorites.txt.bak") -eq "old backup`n") $o
```

- [ ] **Step 2: 시험이 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.tests.ps1
```

Expected: 새 네 줄이 FAIL(`FAVORITES RESTORED`가 없고 파일이 그대로), 나머지 23줄 PASS, `FAILS=4`.

- [ ] **Step 3: 회차 스크립트를 고친다**

`tools/DevRemote/session.ps1`:

머리 주석의 `-Begin` 줄 `→ bag-slots.txt 보관 →`을 `→ bag-slots.txt·favorites.txt 보관 →`으로, `-End` 줄 `→ bag-slots.txt, config.txt, presets 폴더와`를 `→ bag-slots.txt, favorites.txt, config.txt, presets 폴더와`로 바꾼다.

`$noBag = …` 줄 바로 아래에 넣는다:

```powershell
$favFile = Join-Path $data 'favorites.txt'
$keptFav = Join-Path $dir 'session-favorites.txt'          # 회차 전의 favorites.txt(자원 탭 즐겨찾기)
$noFav = Join-Path $dir 'session-favorites.absent'         # 회차 전에 favorites.txt가 없었다는 표시
$keptFavBak = Join-Path $dir 'session-favorites.bak'       # 회차 전의 favorites.txt.bak
$noFavBak = Join-Path $dir 'session-favorites-bak.absent'  # 회차 전에 favorites.txt.bak이 없었다는 표시
```

`function DeleteFiles` 블록 바로 아래에 넣는다:

```powershell
# 세이브 폴더 밖의 모드 파일 하나를 회차 전 상태로 되돌리려고 보관한다. 없었으면 없었다는 표시를 남긴다.
function KeepFile([string]$file, [string]$kept, [string]$absent) {
  DeleteFiles $kept, $absent
  if (Test-Path -LiteralPath $file) { Copy-Item -LiteralPath $file -Destination $kept } else { [IO.File]::WriteAllText($absent, '') }
}
# KeepFile로 보관한 파일을 되돌린다. 회차 전에 없었으면 지운다.
function RestoreFile([string]$file, [string]$kept, [string]$absent) {
  if (Test-Path -LiteralPath $kept) {
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $file))
    Copy-Item -LiteralPath $kept -Destination $file -Force
  } elseif (Test-Path -LiteralPath $absent) {
    DeleteFiles $file
  }
  DeleteFiles $kept, $absent
}
```

`-Begin`에서 `if (Test-Path -LiteralPath $bagFile) { … } else { … }` 줄 바로 아래에 넣는다:

```powershell
  KeepFile $favFile $keptFav $noFav
  KeepFile "$favFile.bak" $keptFavBak $noFavBak
```

`-End`에서 bag-slots를 되돌리는 블록 끝의 `DeleteFiles $keptBag, $noBag` 줄 바로 아래에 넣는다:

```powershell
  # 자원 탭의 즐겨찾기 파일을 회차 전으로 되돌린다. 검사가 쓰다 남긴 임시 파일도 지운다.
  if ((Test-Path $keptFav) -or (Test-Path $noFav)) {
    RestoreFile $favFile $keptFav $noFav
    RestoreFile "$favFile.bak" $keptFavBak $noFavBak
    DeleteFiles "$favFile.tmp"
    'FAVORITES RESTORED'
  }
```

- [ ] **Step 4: 시험이 통과하는 것을 본다**

```bash
pwsh tools/DevRemote/session.tests.ps1
```

Expected: 27줄 모두 PASS, `FAILS=0`.

- [ ] **Step 5: 게임 안 검사를 쓴다**

`tools/research/ui-check.ps1`의 부분 `'groups'` 바로 아래에 넣는다:

```powershell
  'favstore' {
    # 자원 탭 개편 스펙 3.3절: 즐겨찾기 파일의 읽기·쓰기. 경고를 일부러 남기므로 회차에서 이 부분 뒤에는 -Restart한다.
    $traps = Traps
    $fav = Join-Path $data 'favorites.txt'
    function FavText { if (Test-Path -LiteralPath $fav) { [IO.File]::ReadAllText($fav) } else { '' } }
    $o = Cmd 'rfav reset' 300
    Check 'reset starts from no file' ($o -match 'count=0 skipped=0 items=' -and -not (Test-Path -LiteralPath $fav)) $o
    $o = Cmd 'rfav add 1011011 10' 300
    Check 'adding writes the file' ($o -match 'count=1 ' -and (FavText) -match "(?m)^1011011 10\r?$") $o
    Check 'no temp file is left' (-not (Test-Path -LiteralPath "$fav.tmp"))
    # Review Focus 5: 같은 번호, 범위 밖의 수량
    $o = Cmd 'rfav add 1011011 20' 300
    Check 'adding the same item again is refused' ($o -match '이미' -and $o -match 'items=1011011:10(\s|,|$)') $o
    $o = Cmd 'rfav add 1011012 0' 300
    Check 'an amount of zero is refused' ($o -match '수량은 1~999999' -and $o -match 'count=1 ') $o
    $o = Cmd 'rfav set 1011011 1000000' 300
    Check 'an amount above 999999 is refused' ($o -match '수량은 1~999999' -and $o -match 'items=1011011:10(\s|,|$)') $o
    $o = Cmd 'rfav set 1011011 25' 300
    Check 'the amount can be changed' ($o -match 'items=1011011:25' -and (FavText) -match "(?m)^1011011 25\r?$") $o
    $o = Cmd 'rfav add 99999999 3' 300
    Check 'a number the game does not have is kept' ($o -match 'items=1011011:25,99999999:3:missing') $o
    $o = Cmd 'rfav del 99999999' 300
    Check 'removing works' ($o -match 'count=1 ' -and (FavText) -notmatch '99999999') $o

    # 깨진 줄, 같은 번호의 줄, 범위 밖의 수량이 섞인 파일
    [IO.File]::WriteAllText($fav, "# test`n1011011 5`nnot a line`n1011011 7`n1011012 5000000`n1011013 abc`n", [Text.UTF8Encoding]::new($false))
    $o = Cmd 'rfav reload' 300
    Check 'readable lines are kept, the rest counted, big amounts capped' ($o -match 'count=2 skipped=3 items=1011011:5,1011012:999999') $o
    Check 'the log has the warning' (@(ModWarnings | Where-Object { $_ -match 'favorites: skipped 3 lines' }).Count -ge 1)
    Check 'nothing is backed up before a change' (-not (Test-Path -LiteralPath "$fav.bak"))
    Cmd 'rfav set 1011012 7' 300 | Out-Null
    Check 'the original file is kept as .bak before the first write' ((Test-Path -LiteralPath "$fav.bak") -and [IO.File]::ReadAllText("$fav.bak") -match 'not a line')
    Check 'the new file holds only readable lines' ((FavText) -notmatch 'not a line|abc' -and (FavText) -match "(?m)^1011012 7\r?$")
    [IO.File]::WriteAllText("$fav.bak", 'marker')
    Cmd 'rfav set 1011012 8' 300 | Out-Null
    Check 'the .bak is made only once' ([IO.File]::ReadAllText("$fav.bak") -eq 'marker')

    # Review Focus 4: 읽지 못한 파일은 덮어쓰지 않는다
    $before = FavText
    $lock = [IO.File]::Open($fav, 'Open', 'Read', 'None')
    try {
      $o1 = Cmd 'rfav reload' 300
      $o2 = Cmd 'rfav add 1011014 1' 300
    } finally { $lock.Dispose() }
    Check 'a locked file reads as empty' ($o1 -match 'count=0 ') $o1
    Check 'a locked file is not overwritten' ((FavText) -eq $before -and $o2 -match '읽지 못해') $o2
    $o = Cmd 'rfav reload' 300
    Check 'after the lock is gone the list is back' ($o -match 'count=2 skipped=0 items=1011011:5,1011012:8') $o
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'favstore-restart' {
    # favstore 뒤에 session.ps1 -Restart를 하고 돌린다.
    $o = Cmd 'rfav' 200
    Check 'favorites are kept after a restart' ($o -match 'count=2 skipped=0 items=1011011:5,1011012:8') $o
  }
```

- [ ] **Step 6: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin res-1015c -Fixture base
pwsh tools/research/ui-check.ps1 -Part favstore
pwsh tools/DevRemote/session.ps1 -End res-1015c
```

Expected: `rfav`가 `UNKNOWN`이라 FAIL이 여럿, 마지막 줄 `FAILS=`가 0보다 크다. `-End`에 `FAVORITES RESTORED`가 나온다(Step 3이 들어갔으므로).

- [ ] **Step 7: 저장소를 쓴다**

`src/ModCode/ModMain/Services/FavoriteStore.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 자원 탭의 즐겨찾기: 아이템 번호와 지급 수량. 모든 세이브에 공통이고 TlToybox\favorites.txt에 둔다.
    // 쓰기는 임시 파일에 쓴 뒤 바꿔치기한다. 깨진 줄이 있던 파일은 처음 덮어쓰기 전에 .bak으로 남긴다.
    // 읽지 못한 파일은 덮어쓰지 않는다. 근거: docs/superpowers/specs/2026-10-15-resources-tab-design.md 3.2·3.3절, 6절.
    internal static class FavoriteStore
    {
        public sealed class Favorite
        {
            public int Id;
            public int Amount;
        }

        public const int MaxAmount = AmountBar.Max;
        private const string FileName = "favorites.txt";
        private static readonly string AmountRule = "즐겨찾기: 수량은 1~" + MaxAmount + " 사이의 숫자로 입력하세요.";

        private static List<Favorite> list;     // null이면 아직 읽지 않았다
        private static string readError;        // 파일을 읽지 못한 이유. 있으면 덮어쓰지 않는다
        private static bool keepOriginal;       // 깨진 줄이 있었다: 처음 덮어쓰기 전에 .bak으로 남긴다
        private static bool noticePending;      // 자원 탭에 들어올 때 한 번 알릴 것이 있다

        public static int SkippedLines { get; private set; }

        private static string FilePath
        {
            get { return Path.Combine(ChangeLog.DataDir, FileName); }
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
                    var seen = new HashSet<int>();
                    foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                    {
                        string trimmed = line.Trim();
                        if (trimmed.Length == 0 || trimmed[0] == '#')
                        {
                            continue;
                        }
                        string[] parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        int id, amount;
                        if (parts.Length != 2 || !int.TryParse(parts[0], out id) || !int.TryParse(parts[1], out amount) || !seen.Add(id))
                        {
                            SkippedLines++;
                            continue;
                        }
                        list.Add(new Favorite { Id = id, Amount = Math.Max(1, Math.Min(MaxAmount, amount)) });
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

        // 넣은 순서의 즐겨찾기. 고치지 말고 읽기만 한다.
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
            return list.Find(f => f.Id == id);
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

        private static string Label(int id)
        {
            ItemCatalog.Entry entry = ItemCatalog.Get(id);
            return entry != null ? entry.Name : "게임에 없는 아이템 (" + id + ")";
        }

        public static bool Add(int id, int amount, out string text)
        {
            if (amount < 1 || amount > MaxAmount)
            {
                text = AmountRule;
                return false;
            }
            if (Has(id))
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 이미 있습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.Add(new Favorite { Id = id, Amount = amount }), out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에 넣었습니다: " + Label(id) + " x" + amount;
            return true;
        }

        public static bool Remove(int id, out string text)
        {
            if (!Has(id))
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 즐겨찾기에 없습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.RemoveAll(f => f.Id == id), out error))
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
            int old = AmountOf(id);
            if (old == 0)
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 즐겨찾기에 없습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.Find(f => f.Id == id).Amount = amount, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기 수량: " + Label(id) + " " + old + " -> " + amount;
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
                next.Add(new Favorite { Id = f.Id, Amount = f.Amount });
            }
            edit(next);
            try
            {
                Directory.CreateDirectory(ChangeLog.DataDir);
                if (keepOriginal && File.Exists(FilePath))
                {
                    File.Copy(FilePath, FilePath + ".bak", true);
                }
                var sb = new StringBuilder("# TlToybox 자원 탭 즐겨찾기: 한 줄에 <아이템 번호> <수량>\n");
                foreach (Favorite f in next)
                {
                    sb.Append(f.Id).Append(' ').Append(f.Amount).Append('\n');
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

- [ ] **Step 8: 개발 명령 `rfav`**

`src/ModCode/ModMain/Dev/DevCommandsResources.cs`:

머리 주석의 `rgroups` 줄 아래에 넣는다:

```csharp
    //   rfav [reset|reload|add <번호> <수량>|set <번호> <수량>|del <번호>]
    //           : (동작이 있으면 "<문장> | ") + count=<수> skipped=<건너뛴 줄> items=<번호>:<수량>[:missing],…
    //             reset은 favorites.txt(.bak, .tmp 포함)를 지우고 다시 읽게 한다. reload는 파일을 그대로 두고 다시 읽게 한다
```

`using` 줄에 `using System.IO;`를 더한다. `switch`의 `case "rgroups":` 아래에 넣는다:

```csharp
                case "rfav":
                    return Favorites(p);
```

`Groups()` 메서드 아래에 넣는다:

```csharp
        private static string Favorites(string[] p)
        {
            string what = p.Length > 1 ? p[1] : "";
            string text = null;
            string path = Path.Combine(ChangeLog.DataDir, "favorites.txt");
            switch (what)
            {
                case "reset":
                    foreach (string file in new[] { path, path + ".bak", path + ".tmp" })
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                    FavoriteStore.Forget();
                    break;
                case "reload":
                    FavoriteStore.Forget();
                    break;
                case "add":
                    FavoriteStore.Add(int.Parse(p[2]), int.Parse(p[3]), out text);
                    break;
                case "set":
                    FavoriteStore.SetAmount(int.Parse(p[2]), int.Parse(p[3]), out text);
                    break;
                case "del":
                    FavoriteStore.Remove(int.Parse(p[2]), out text);
                    break;
            }
            var items = new List<string>();
            foreach (FavoriteStore.Favorite f in FavoriteStore.Entries)
            {
                items.Add(f.Id + ":" + f.Amount + (ItemCatalog.Get(f.Id) == null ? ":missing" : ""));
            }
            string state = "count=" + items.Count + " skipped=" + FavoriteStore.SkippedLines + " items=" + string.Join(",", items.ToArray());
            return text == null ? state : text + " | " + state;
        }
```

- [ ] **Step 9: 빌드하고 검사가 통과하는 것을 본다**

Task 1 Step 6과 같은 두 빌드를 돌린다(오류·경고 0, DLL 하나).

```bash
pwsh tools/DevRemote/session.ps1 -Begin res-1015d -Fixture base
pwsh tools/research/ui-check.ps1 -Part favstore
pwsh tools/DevRemote/session.ps1 -Restart
pwsh tools/research/ui-check.ps1 -Part favstore-restart
pwsh tools/DevRemote/session.ps1 -End res-1015d
```

Expected: 두 부분 `FAILS=0`. `-End`에 `FAVORITES RESTORED`가 나오고, 회차 뒤 `%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\TlToybox\favorites.txt`가 회차 전과 같다(없었으면 없다).

"no temp file is left"나 "adding writes the file"이 실패하고 메시지에 `File.Replace`의 예외가 보이면, 이 실행 환경이 `File.Replace`를 지원하지 않는 것이다. 그때는 `File.Replace(tmp, FilePath, null);`을 `File.Delete(FilePath); File.Move(tmp, FilePath);`로 바꾸고, 바꾼 사실과 이유를 05 문서 38절에 적는다(원자성이 약해진다).

- [ ] **Step 10: 커밋**

```bash
git add src/ModCode/ModMain/Services/FavoriteStore.cs src/ModCode/ModMain/Dev/DevCommandsResources.cs tools/DevRemote/session.ps1 tools/DevRemote/session.tests.ps1 tools/research/ui-check.ps1
git commit -m "feat: 즐겨찾기 저장소(원자적 쓰기, 깨진 줄 .bak, 잠긴 파일 보호)와 회차의 favorites.txt 되돌리기

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 자원 탭 화면

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs`(전체 교체), `src/ModCode/ModMain/Services/EditService.cs`(`GiveItem`), `src/ModCode/ModMain/Services/ItemCatalog.cs`(`MainCategories`·`SubCategories` 지우기), `tools/research/ui-check.ps1`(부분 `items`, `groups`, `wrap` 고침, `favorites`, `favorites-corrupt` 새로)

**Interfaces:**
- Consumes: Task 1의 `ItemGroups`·`ItemCatalog.Filter/Select/Passes/CountByGroup/CountByKind/Get/OwnedIds`, Task 2의 `FavoriteStore`.
- Produces:
  - `EditService.TryGiveItem(WorldUnitBase unit, int propsId, int count, out string text) : bool`(보유가 수량만큼 늘었으면 true). `GiveItem`은 같은 문장을 돌려준다.
  - 화면의 요소 이름은 Global Constraints의 목록. 그 밖에 `ResAllHint`(전체 그룹의 안내 글), `ResKinds`(종류 칩 영역).
  - 안내 문장: `즐겨찾기 모두 지급: 넣음 n, 못 넣음 m(이름, …).`, 줄 것이 없으면 `즐겨찾기 모두 지급: 줄 수 있는 아이템이 없습니다.`. 확인 버튼 글 `모두 지급` → `정말 모두 지급`.

스펙에서 바꾼 것(05 문서 38절에 적는다):

- 3.4절 맨 위 두 줄의 배치. 1줄에 영석과 가방 칸을 두면 폭이 1304를 넘는다(영석 줄이 1032까지, 가방 글이 약 290). 그래서 **1줄 영석, 2줄 지급 수량과 가방 칸**으로 한다.
- 6절 "수량 칸에 숫자가 아닌 글"의 문장. 수량 칸은 숫자 전용 입력 칸(`NumberField`)이라 글자가 들어가지 않는다. 빈 칸이나 `-`만 넣고 Enter를 누르면 공통 부품의 문장 `숫자를 입력하세요.`가 나온다. 범위 밖의 숫자(0, 1000000)는 스펙의 문장 `즐겨찾기: 수량은 1~999999 사이의 숫자로 입력하세요.`가 나온다.

- [ ] **Step 1: 검사를 고치고 더한다**

`tools/research/ui-check.ps1`:

(a) 부분 `'groups'`의 마지막 줄 `Check 'no callback exception in the log' ((Traps) -eq $traps)`를 아래로 바꾼다:

```powershell
    # 화면: 왼쪽 개수, 검색에 따른 개수, 의복의 종류, 기타, 전체의 안내
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupAll' | Out-Null; Press 'ResClear' | Out-Null
    Check 'the All button shows the total' ((Cmd 'uitext ResGroupAll') -match "text='전체 $total'") (Cmd 'uitext ResGroupAll')
    Check 'the Pill button shows its count' ((Cmd 'uitext ResGroupPill') -match "text='단약 $pill'") (Cmd 'uitext ResGroupPill')
    Check 'All lists everything' ((Cmd 'uitext ItemTableRange') -match "전체 $($total)개")
    Check 'All shows a hint instead of kinds' ((Cmd 'uipos ResKind0') -match 'no element named' -and (Cmd 'uipos ResAllHint') -match ' at ')
    # Review Focus 3: 실제 키로 치는 동안 왼쪽 개수가 바뀌고 검색 칸이 포커스를 잃지 않는다
    Cmd 'uifocus ItemQuery' | Out-Null
    Key 0x31 350; Key 0x30 350
    $typed = Cmd 'uitext ItemQuery'
    $left = Cmd 'uitext ResGroupAll'
    Check 'typing keeps the focus while counts change' ($typed -match "text='10'" -and $left -notmatch "text='전체 $total'") "$typed | $left"
    Press 'ItemQueryClear' | Out-Null
    Check 'clearing the search restores the counts' ((Cmd 'uitext ResGroupAll') -match "text='전체 $total'") (Cmd 'uitext ResGroupAll')
    Press 'ResGroupCloth' | Out-Null
    $k1 = Cmd 'uitext ResKind1'; $k2 = Cmd 'uitext ResKind2'
    Check 'clothes are split by sex' ($k1 -match "text='남 \(\d+\)'" -and $k2 -match "text='여 \(\d+\)'") "$k1 | $k2"
    Press 'ResKind1' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    Check 'a kind narrows the list' ($o -match '전체 (\d+)개' -and [int]$Matches[1] -gt 0 -and [int]$Matches[1] -lt $cloth) $o
    Shot 'groups-cloth'
    Press 'ResGroupOther' | Out-Null
    Check 'Other is empty in the plain game' ((Cmd 'uitext ItemTableRange') -match "text='0개'")
    Press 'ResGroupAll' | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
```

(b) 부분 `'items'`의 첫 줄들 `Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null` 바로 아래에 넣는다:

```powershell
    Press 'ResGroupAll' | Out-Null; Press 'ResClear' | Out-Null
```

같은 부분의 `Check 'paging buttons are gone' …` 줄 아래에 넣는다:

```powershell
    Check 'the old category buttons are gone' ((Cmd 'uipos ItemCat*') -match 'no element named' -and (Cmd 'uipos ItemSub*') -match 'no element named')
```

같은 부분의 `# 의복: 이름만으로 구분된다` 줄 바로 앞에 넣는다:

```powershell
    # 자원 탭 개편(스펙 7절): 그룹 → 종류 → 검색 → 품질 → 보유 중만, 설명 칸의 그룹/종류
    Press 'ResGroupPill' | Out-Null
    Check 'the first kind of pills is recovery' ((Cmd 'uitext ResKind1') -match "text='회복 \(\d+\)'") (Cmd 'uitext ResKind1')
    Press 'ResKind1' | Out-Null
    Cmd 'ui set ItemQuery 보원단' | Out-Null
    Press 'ResQuality1' | Out-Null
    $o = Cmd 'uitext ItemTableRange'
    Check 'group, kind, search and quality narrow to one row' ($o -match "text='전체 1개 중 1~1번째'" -and (Cmd 'uipos ItemName1011011') -match ' at ') $o
    Press 'ItemName1011011' | Out-Null
    Check 'the detail names the group and kind' ((Cmd 'uitext ItemDetail') -match '\[1011011\] 6품 보원단  \(단약 / 회복, 품질 1\)') (Cmd 'uitext ItemDetail')
    $had = if ((Cmd 'count 1011011') -match 'count=(\d+)') { [int]$Matches[1] } else { -1 }
    Press 'ResOwnedOnly' | Out-Null
    $want = if ($had -gt 0) { "text='전체 1개" } else { "text='0개'" }
    Check 'owned only follows the bag' ((Cmd 'uitext ItemTableRange') -match $want) "had=$had $(Cmd 'uitext ItemTableRange')"
    Press 'ResOwnedOnly' | Out-Null
    Press 'ItemGive1011011' | Out-Null
    Press 'ResOwnedOnly' | Out-Null
    Check 'after giving, owned only shows it' ((Cmd 'uitext ItemTableRange') -match "text='전체 1개") (Cmd 'uitext ItemTableRange')
    Press 'ResClear' | Out-Null
    $pillCount = if ((Cmd 'uitext ResGroupPill') -match "단약 (\d+)'") { $Matches[1] } else { 'x' }
    Check 'clear keeps the group and resets the rest' ((Cmd 'uitext ItemQuery') -match "text=''" -and (Cmd 'uitext ItemTableRange') -match "전체 $($pillCount)개") (Cmd 'uitext ItemTableRange')
    Press 'ResGroupAll' | Out-Null
```

(c) 부분 `'wrap'`의 `# 자원 탭: 보이는 줄마다 …` 아래 `Press 'Tab1' | Out-Null` 줄을 아래로 바꾼다:

```powershell
    Press 'Tab1' | Out-Null; Press 'ResGroupAll' | Out-Null; Press 'ResClear' | Out-Null
```

같은 부분의 `Check 'the description column is wide' ((Cmd "uipos $firstDesc" 100) -match 'size=605,') …`에서 `'size=605,'`를 `'size=496,'`로 바꾼다(설명 열이 왼쪽 그룹 목록 때문에 좁아졌다. 스펙 3.4절).

(d) 부분 `'favstore-restart'` 아래에 넣는다:

```powershell
  'favorites' {
    # 자원 탭 개편 스펙 3.2·7절: 화면의 즐겨찾기. 회차의 -End가 favorites.txt를 되돌린다.
    $traps = Traps
    function Held([int]$id) { if ((Cmd "count $id") -match 'count=(\d+)') { [int]$Matches[1] } else { -1 } }
    function FavLines { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ -match '즐겨찾기' }).Count }
    Cmd 'rfav reset' 300 | Out-Null
    $logged = FavLines
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupAll' | Out-Null; Press 'ResClear' | Out-Null
    Check 'the favorites button shows zero' ((Cmd 'uitext ResGroupFav') -match "text='★ 즐겨찾기 0'") (Cmd 'uitext ResGroupFav')
    Press 'item10' | Out-Null
    Cmd 'ui set ItemQuery 1011011' | Out-Null
    Check 'a row starts with an empty star' ((Cmd 'uitext ResStar1011011') -match "text='☆'") (Cmd 'uitext ResStar1011011')
    Press 'ResStar1011011' | Out-Null
    Check 'the star adds it with the current amount' ((Cmd 'rfav' 200) -match 'items=1011011:10(\s|$)') (Cmd 'rfav' 200)
    Check 'the star is filled' ((Cmd 'uitext ResStar1011011') -match "text='★'")
    Cmd 'ui set ItemQuery 1011012' | Out-Null
    Press 'ResStar1011012' | Out-Null
    Press 'ItemQueryClear' | Out-Null
    Press 'ResGroupFav' | Out-Null
    Check 'the favorites view lists them in the order added' ((Pos 'ItemName*').name -eq 'ItemName1011011' -and (Cmd 'uitext ResGroupFav') -match "즐겨찾기 2'") (Cmd 'uitext ResGroupFav')
    Check 'the give button shows the saved amount' ((Cmd 'uitext ItemGive1011011') -match "text='지급 x10'")
    Cmd 'uifocus ResAmount1011011' | Out-Null; Cmd 'ui set ResAmount1011011 25' | Out-Null; Key 0x0D
    Check 'Enter saves the amount' ((Cmd 'rfav' 200) -match 'items=1011011:25,1011012:10') (Cmd 'rfav' 200)
    Press 'item1' | Out-Null
    $a = Held 1011011
    Press 'ItemGive1011011' | Out-Null
    Check 'the row gives the saved amount, not the amount bar' ((Held 1011011) -eq $a + 25) "$a -> $(Held 1011011)"
    Cmd 'uifocus ResAmount1011012' | Out-Null; Cmd 'ui set ResAmount1011012 0' | Out-Null; Key 0x0D
    Check 'an amount of zero is refused' ((Cmd 'rfav' 200) -match '1011012:10' -and (Cmd 'uistate') -match "message='즐겨찾기: 수량은 1~999999") (Cmd 'uistate')
    $a = Held 1011011; $b2 = Held 1011012
    Press 'ResAllGive' | Out-Null
    Check 'give all asks first' ((Held 1011011) -eq $a -and (Cmd 'uitext ResAllGive') -match "text='정말 모두 지급'")
    Press 'ResAllGive' 2000 | Out-Null
    Check 'the second press gives every favorite its saved amount' ((Held 1011011) -eq $a + 25 -and (Held 1011012) -eq $b2 + 10) "$a/$b2 -> $(Held 1011011)/$(Held 1011012)"
    Check 'the result counts what was given' ((Cmd 'uistate') -match "message='즐겨찾기 모두 지급: 넣음 2, 못 넣음 0") (Cmd 'uistate')
    # Review Focus 2: 모두 지급은 걸러져 보이는 줄만 준다(5품 보원단만 품질 2)
    Press 'ResQuality2' | Out-Null
    $a = Held 1011011; $b2 = Held 1011012
    Press 'ResAllGive' | Out-Null; Press 'ResAllGive' 2000 | Out-Null
    Check 'give all gives only the rows shown' ((Held 1011011) -eq $a -and (Held 1011012) -eq $b2 + 10) "$a/$b2 -> $(Held 1011011)/$(Held 1011012)"
    Press 'ResClear' | Out-Null
    Shot 'favorites'
    Press 'ResStar1011012' | Out-Null
    Check 'the star removes it' ((Cmd 'rfav' 200) -match 'count=1 ' -and (Cmd 'uitext ResGroupFav') -match "즐겨찾기 1'") (Cmd 'rfav' 200)
    Check 'favorites do not go to the change log' ((FavLines) -eq $logged)
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'favorites-corrupt' {
    # 깨진 줄과 게임에 없는 번호가 섞인 파일을 화면에서 본다. 경고를 일부러 남기므로 회차의 마지막에 돌린다.
    $traps = Traps
    $fav = Join-Path $data 'favorites.txt'
    Cmd 'rfav reset' 300 | Out-Null
    [IO.File]::WriteAllText($fav, "1011011 5`nnot a line`n99999999 3`n1011013 abc`n", [Text.UTF8Encoding]::new($false))
    Cmd 'rfav reload' 300 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null; Press 'Tab1' | Out-Null
    Check 'entering the tab tells about the skipped lines' ((Cmd 'uistate') -match "message='즐겨찾기 파일에서 읽지 못한 줄 2개를 건너뛰었습니다") (Cmd 'uistate')
    Press 'Tab0' | Out-Null; Press 'Tab1' | Out-Null
    Check 'the notice is shown only once' ((Cmd 'uistate') -notmatch '건너뛰었습니다')
    Press 'ResGroupFav' | Out-Null; Press 'ResClear' | Out-Null
    Check 'a missing item is shown as such' ((Cmd 'uitext ItemName99999999') -match '게임에 없는 아이템 \(99999999\)') (Cmd 'uitext ItemName99999999')
    $p = Pos 'ItemGive99999999'
    Check 'its give button is off' ((Rc "click $($p.x) $($p.y)" 400) -notmatch 'Button\.onClick: ItemGive99999999')
    # Review Focus 1: 필터가 걸려도 게임에 없는 줄은 보이고 뺄 수 있다
    Press 'ResQuality6' | Out-Null
    Check 'a missing item stays visible under a filter' ((Cmd 'uipos ItemName99999999') -match ' at ' -and (Cmd 'uipos ItemName1011011') -match 'no element named')
    Press 'ResStar99999999' | Out-Null
    Check 'a missing item can be removed' ((Cmd 'rfav' 200) -match 'count=1 ') (Cmd 'rfav' 200)
    Press 'ResClear' | Out-Null
    Cmd 'rfav reset' 300 | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
  }
```

`Held`, `FavLines`가 별칭과 겹치지 않는지 본다:

```bash
pwsh -NoProfile -Command "Get-Alias Held, FavLines, FavText -ErrorAction SilentlyContinue"
```

Expected: 아무것도 나오지 않는다.

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin res-1015e -Fixture base
pwsh tools/research/ui-check.ps1 -Part items
pwsh tools/research/ui-check.ps1 -Part favorites
pwsh tools/DevRemote/session.ps1 -End res-1015e
```

Expected: 두 부분 모두 `편집기에서 요소를 찾지 못했습니다: ResGroupAll`로 멈춘다(지금의 화면에는 그룹 버튼이 없다).

- [ ] **Step 3: `TryGiveItem`**

`src/ModCode/ModMain/Services/EditService.cs`의 `public static string GiveItem(WorldUnitBase unit, int propsId, int count)` 메서드 전체를 아래 두 메서드로 바꾼다:

```csharp
        public static string GiveItem(WorldUnitBase unit, int propsId, int count)
        {
            string text;
            TryGiveItem(unit, propsId, count, out text);
            return text;
        }

        // 지급하고 보유 수가 수량만큼 늘었는지 돌려준다(즐겨찾기 모두 지급의 성공 판정). 문장은 GiveItem과 같다.
        public static bool TryGiveItem(WorldUnitBase unit, int propsId, int count, out string text)
        {
            string name = ItemName(propsId);
            if (name == null)
            {
                text = "아이템 " + propsId + ": 게임에 없는 아이템 ID라 지급하지 않았습니다.";
                return false;
            }
            if (count <= 0)
            {
                text = "아이템: 수량은 1 이상이어야 합니다.";
                return false;
            }
            var props = unit.data.unitData.propData;
            int before = props.GetPropsNum(propsId);
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            unit.data.RewardPropItem(propsId, count, false);
            int after = props.GetPropsNum(propsId);
            bool ok = after == before + count;
            ChangeLog.Add(NameOf(unit), "아이템 " + name + "(" + propsId + ")", before.ToString(), after.ToString(), ok);
            text = ok ? name + ": " + before + "개 -> " + after + "개"
                : name + ": 지급을 요청했지만 수량이 " + before + " -> " + after + "입니다(가방 공간을 확인하세요).";
            return ok;
        }
```

- [ ] **Step 4: 자원 탭을 다시 쓴다**

`src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs`를 통째로 바꾼다:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭: 영석, 지급 수량과 가방 칸, 용도 그룹으로 나눈 아이템 표와 즐겨찾기.
    // 설계: docs/superpowers/specs/2026-10-15-resources-tab-design.md 3·4절. 판단은 ItemCatalog·FavoriteStore가 하고 여기서는 그린다.
    internal sealed class ResourcesTab : EditorTab
    {
        private const float RowH = 32;
        private const float DetailH = 92;
        private const float TopH = 92;                  // 1줄 영석, 2줄 지급 수량과 가방 칸
        private const float LeftW = 170;                // 그룹 목록
        private const float RightX = 180;
        private const float RightW = ContentW - RightX; // 1124
        private const float ChipH = 34, ChipGap = 6;
        private const int ChipRowsShown = 3;
        // 표의 열(목록 안의 x). 지급 열은 목록 안쪽 폭(RightW - 스크롤 막대)의 오른쪽 끝에 붙인다.
        private const float ColStar = 4, ColName = 50, ColKind = 256, ColLevel = 372, ColOwned = 420, ColDesc = 486;
        private const float NameW = 200, KindW = 110, GiveW = 116, AmountW = 90;
        private const float ColGive = RightW - Ui.ScrollBarWidth - GiveW - 4;   // 990
        private const float ColAmount = ColGive - AmountW - 6;                  // 894
        private const float DescW = ColGive - ColDesc - 8;                      // 496
        private const float FavDescW = ColAmount - ColDesc - 8;                 // 400

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

        private readonly ListView<ItemCatalog.Entry> list = new ListView<ItemCatalog.Entry>("ItemTable", RowH);
        private readonly DetailPane detail = new DetailPane("ItemDetail", "아이템명을 누르면 여기에 전체 설명이 나옵니다.");
        private readonly ItemCatalog.Filter filter = new ItemCatalog.Filter();
        private readonly List<CountLabel> labels = new List<CountLabel>();
        private string groupKey = ItemGroups.AllKey;
        private float chipScrollY;
        private string bagInput = "";

        public override string Title
        {
            get { return "자원"; }
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

        // 그룹·종류·품질·보유 중만을 바꾼다: 목록을 맨 위로, 설명 칸을 비우고 다시 그린다.
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

        // 지금 보일 줄. 즐겨찾기 보기는 넣은 순서, 나머지는 그룹·종류·게임 표 순서.
        private List<ItemCatalog.Entry> Rows(HashSet<int> owned)
        {
            if (!IsFav)
            {
                filter.Group = groupKey;
                return ItemCatalog.Select(filter, owned);
            }
            var rows = new List<ItemCatalog.Entry>();
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                ItemCatalog.Entry entry = ItemCatalog.Get(favorite.Id) ?? Missing(favorite.Id);
                if (ItemCatalog.Passes(entry, filter, owned))
                {
                    rows.Add(entry);
                }
            }
            return rows;
        }

        private int FavCount(HashSet<int> owned)
        {
            int count = 0;
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                ItemCatalog.Entry entry = ItemCatalog.Get(favorite.Id) ?? Missing(favorite.Id);
                if (ItemCatalog.Passes(entry, filter, owned))
                {
                    count++;
                }
            }
            return count;
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            HashSet<int> owned = ItemCatalog.OwnedIds(player);
            labels.Clear();
            DrawTop(player);
            DrawGroups(owned);
            float y = TopH + 8;
            y = DrawKinds(y, owned);
            y = DrawFilters(y);
            DrawHead(y);
            y += 32;
            list.EmptyText = IsFav ? "즐겨찾기가 비어 있습니다. 표의 ☆를 눌러 넣습니다." : "조건에 맞는 아이템이 없습니다.";
            float descW = IsFav ? FavDescW : DescW;
            list.HeightOf = entry => RowFor(RowH, Ui.TextHeight(entry.Desc, descW, 15), Ui.TextHeight(entry.Name, NameW, 19),
                Ui.TextHeight(entry.Kind == null ? "-" : entry.Kind.Name, KindW, 16));
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
            y += 44;
            GroupButton(ItemGroups.AllKey, "전체", counts[ItemGroups.AllKey], y);
            y += 44;
            foreach (ItemGroups.Group group in ItemGroups.Groups)
            {
                GroupButton(group.Key, group.Name, counts[group.Key], y);
                y += 44;
            }
        }

        private void GroupButton(string key, string name, int count, float y)
        {
            Button button = Ui.Button("ResGroup" + key, content, name + " " + count, 0, y, LeftW, 40, new Action(() => SetGroup(key)), true, groupKey == key);
            labels.Add(new CountLabel { Text = button.GetComponentInChildren<Text>(), Name = name, Key = key });
        }

        // 종류 칩(그룹 보기), 모두 지급(즐겨찾기 보기) 또는 안내(전체 보기). 다음 줄의 y를 돌려준다.
        private float DrawKinds(float y, HashSet<int> owned)
        {
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
            Ui.Label(content, "설명", RightX + ColDesc, y, IsFav ? FavDescW : DescW, 30, 16).color = Ui.Dim;
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
            int[] kinds = IsFav || groupKey == ItemGroups.AllKey ? null : ItemCatalog.CountByKind(ItemGroups.ByKey(groupKey), filter, owned);
            int favCount = FavCount(owned);
            foreach (CountLabel label in labels)
            {
                if (label.Key == ItemGroups.FavKey)
                {
                    label.Set(favCount);
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

        private void DrawRow(RectTransform row, ItemCatalog.Entry entry, int index)
        {
            float h = row.sizeDelta.y;
            int id = entry.Id;
            bool fav = FavoriteStore.Has(id);
            Ui.Button("ResStar" + id, row, fav ? "★" : "☆", ColStar, 2, 40, RowH - 4, new Action(() => ToggleFavorite(id)), true, fav);
            WrapLabel(Ui.Button("ItemName" + id, row, entry.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(Detail(entry)))));
            Ui.Wrapped(row, entry.Kind == null ? "-" : entry.Kind.Name, ColKind, 0, KindW, h, 16);
            Ui.Label(row, entry.Missing ? "-" : entry.Level.ToString(), ColLevel, 0, 44, h, 16);
            Ui.Label(row, entry.Missing ? "-" : Player.data.unitData.propData.GetPropsNum(id).ToString(), ColOwned, 0, 60, h, 16);
            Text desc = Ui.Wrapped(row, entry.Desc, ColDesc, 0, IsFav ? FavDescW : DescW, h, 15);
            desc.color = Ui.Dim;
            desc.gameObject.name = "ItemDesc" + id;
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

        // 보이는(걸러진) 즐겨찾기 가운데 게임에 있는 것을 저장된 수량으로 차례로 준다.
        private void GiveAll()
        {
            int given = 0;
            var failed = new List<string>();
            foreach (ItemCatalog.Entry entry in Rows(ItemCatalog.OwnedIds(Player)))
            {
                if (entry.Missing)
                {
                    continue;
                }
                string text;
                if (EditService.TryGiveItem(Player, entry.Id, FavoriteStore.AmountOf(entry.Id), out text))
                {
                    given++;
                }
                else
                {
                    failed.Add(entry.Name);
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

- [ ] **Step 5: 카탈로그의 옛 분류를 지운다**

`src/ModCode/ModMain/Services/ItemCatalog.cs`에서 다음을 지운다:

- `public class Category { … }` 블록
- 필드 `private static List<Category> mainCategories;`
- `Build()` 안의 `var counts = new Dictionary<int, int>();`, 반복문 끝의 `int count; counts.TryGetValue(item.type, out count); counts[item.type] = count + 1;` 세 줄, 그리고 `ItemGroups.SortOther();` 정렬 뒤의 `mainCategories = new List<Category>(); … mainCategories.Sort(…);` 블록
- 속성 `MainCategories`와 메서드 `SubCategories` 전체(주석 포함)

지운 뒤 아무도 쓰지 않는지 본다:

```bash
grep -rn "MainCategories\|SubCategories\|ItemCatalog.Category" src/ModCode/ModMain --include=*.cs | grep -v "/obj/"
```

Expected: 아무것도 나오지 않는다.

- [ ] **Step 6: 두 구성으로 빌드한다**

Task 1 Step 6과 같다. Expected: 오류·경고 0, DLL 하나.

- [ ] **Step 7: 검사가 통과하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin res-1015f -Fixture base
pwsh tools/research/ui-check.ps1 -Part groups
pwsh tools/research/ui-check.ps1 -Part items
pwsh tools/research/ui-check.ps1 -Part favorites
pwsh tools/research/ui-check.ps1 -Part favorites-corrupt
pwsh tools/DevRemote/session.ps1 -End res-1015f
```

Expected: 네 부분 모두 `FAILS=0`. `-End`에 `FAVORITES RESTORED`.

캡처를 눈으로 본다(Read 도구로 PNG를 연다): `.devremote\check-groups-cloth.png`, `.devremote\check-favorites.png`. 볼 것은 다섯 가지다.
- ★·☆가 글자로 보이는가(네모나 빈칸이 아닌가)
- 왼쪽 그룹 목록과 오른쪽 표가 겹치지 않는가
- 맨 위 두 줄의 글이 서로 겹치지 않는가
- 종류 칩의 글이 칩 안에 들어가는가
- 즐겨찾기 보기의 수량 칸과 지급 버튼이 나란한가

★·☆가 보이지 않으면 버튼 글을 `★`/`☆` 대신 `즐찾`/`+`로 바꾸고, 검사의 `text='☆'`·`text='★'`도 함께 바꾼 뒤 다시 돌린다. 바꾼 사실은 05 문서 38절에 적는다.

- [ ] **Step 8: 회귀 검사**

```bash
pwsh tools/DevRemote/session.ps1 -Begin res-1015g -Fixture base
pwsh tools/research/ui-check.ps1 -Part base
pwsh tools/research/ui-check.ps1 -Part tabs
pwsh tools/research/ui-check.ps1 -Part numbers
pwsh tools/research/ui-check.ps1 -Part history
pwsh tools/research/ui-check.ps1 -Part wrap
pwsh tools/DevRemote/session.ps1 -End res-1015g
pwsh tools/DevRemote/session.ps1 -Begin res-1015h -Fixture base
pwsh tools/research/datebag-check.ps1 -Part bag
pwsh tools/DevRemote/session.ps1 -End res-1015h
pwsh tools/DevRemote/session.ps1 -Begin res-1015i -Fixture base
pwsh tools/research/artifact-check.ps1 -Part typing
pwsh tools/DevRemote/session.ps1 -End res-1015i
```

Expected: 모든 부분 `FAILS=0`. `datebag-check.ps1 -Part bag`은 자원 탭의 `BagPlus`·`BagMinus`와 개발 명령 `gridfill`(카탈로그의 `Find`)을 쓴다. `artifact-check.ps1 -Part typing`은 자원 탭의 `ItemQuery`에 글자를 치며 게임 단축키가 막히는지 본다. 둘 다 깨끗한 회차가 필요해서 회차를 나눴다.

`wrap`의 "more than ten rows fit on a screen"(`$shown -ge 11`)이 실패하면, 설명 열이 605에서 496으로 좁아져 줄이 높아진 것이다. 실측한 `shown`을 05 문서 38절에 적는다. 그 값이 8 이상이면 검사의 기준을 실측값으로 바꾸고 옆에 `# 자원 탭 개편(38절): 설명 열 496, 실측 <값>줄` 주석을 단다. 8 미만이면 멈추고 사용자에게 알린다(화면이 전보다 나빠진 것이다).

- [ ] **Step 9: 커밋**

```bash
git add src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs src/ModCode/ModMain/Services/EditService.cs src/ModCode/ModMain/Services/ItemCatalog.cs tools/research/ui-check.ps1
git commit -m "feat: 자원 탭 개편 — 왼쪽 용도 그룹, 종류 칩, 품질·보유 중만 거르기, 즐겨찾기와 모두 지급

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: 문서, 검토, 병합, 설치

**Files:**
- Modify: `docs/05-editor-v0.1.md`(38절 추가), `docs/06-handoff.md`

**Interfaces:**
- Consumes: Task 1~3의 결과와 검사 출력(회차 `res-1015b`~`i`).

- [ ] **Step 1: 05 문서에 38절을 쓴다**

`docs/05-editor-v0.1.md` 끝에 `## 38. 자원 탭 개편 — 즐겨찾기와 용도별 분류 (2026-10-15 추가)`를 더한다. 소절과 담을 것:

- `### 바꾼 것`: 용도 그룹 11개와 종류(스펙 4절의 표를 링크하고 표 자체는 옮기지 않는다), 화면 배치(스펙 3.4절에서 바꾼 맨 위 두 줄의 배치와 그 이유), 품질 필터(게임의 `level`, 단약 이름의 n품과 반대), 보유 중만, 초기화, 즐겨찾기(넣기·빼기·수량·모두 지급), 즐겨찾기 파일(`favorites.txt`의 형식, 원자적 쓰기, `.bak`, 잠긴 파일).
- `### 게임에서 확인한 것`: 회차 이름과 부분별 결과. `rgroups`의 실측(전체 수, 그룹별 수, `other`). 캡처로 본 것(★·☆ 표시 등). `wrap`에서 실측한 한 화면의 줄 수.
- `### 확인하지 못한 것, 한계`: 실제 마우스로 별 누르기, 한글 입력기로 검색(도구로 할 수 없다), 모두 지급으로 수십 종류를 줄 때의 시간, 작은 종류 이름(대비무·호요·체질 개선·천명 전용)이 게임 화면의 이름과 같은지, 창작마당 모드의 아이템이 `기타`에 들어가는 것(모드가 없어 확인 못 함. G2에서 본다).
- 실행 중에 정한 것(`File.Replace` 대체, ★ 대체, `wrap` 기준 변경, `기타`에 나온 번호)이 있었으면 그 사실과 이유.

- [ ] **Step 2: 인계 문서를 고친다**

`docs/06-handoff.md`:

- 머리 줄 `# 06. 작업 인계 (2026-10-15 기준)`는 그대로 두고, 1절의 "직전 작업"을 이번 일(자원 탭 개편, 38절)로 바꾼다. "열린 일 하나"(8.3a)는 그대로 둔다.
- 2절 표의 자원 줄: 기능을 "영석 추가, 지급 수량, 가방 칸, 용도 그룹 11개(왼쪽 목록)와 종류 칩, 이름·품질·보유 중만 거르기, 즐겨찾기(아이템+수량, 모두 지급)"로, 코드에 `ItemGroups`, `FavoriteStore`를, 실험 기록에 38절을 더한다.
- 2절 공통 규칙의 모드 파일 목록에 `favorites.txt`(자원 탭 즐겨찾기, 모든 세이브 공통)를 더한다.
- 5절 `-End`가 되돌리는 것에 `favorites.txt`(와 `.bak`)를 더한다. 자동 검증 표에 `groups`, `favstore` → `-Restart` → `favstore-restart`, `favorites`, `favorites-corrupt`(회차의 마지막)를 더한다. `items`의 "보는 것"에 그룹·종류·품질·보유 중만을 더한다.
- 5절 "회차 안에서 쓰는 도구"의 개발 명령에 `rgroups`, `rfav [reset|reload|add|set|del]`를 더한다.
- 6절 "PowerShell과 도구"의 "이 PC에는 Python이 없다"를 "`python` 명령은 Windows 스토어 별칭이라 실행되지 않는다. 실제 Python은 `py -3`(3.14)이다. 게임 설정 표를 세는 일 같은 것은 `py -3 -I -X utf8 <스크립트>`로 하고 출력은 파일로 받는다(콘솔은 CP949)"로 바꾼다.
- 8.1절 표에 줄을 더한다: `G1 | 자원 탭 개편: 즐겨찾기(아이템+수량, 모두 지급)와 용도별 분류(요청 2·3, 2026-10-15) | 38절. 스펙·계획 2026-10-15-resources-tab*`.
- 8.2절 끝에 2026-10-15 요청 원문(스펙 1절의 네 줄)을 더한다.
- 8.3a 앞에 `### 8.3b 다음: G2 창작마당 모드 연동 (요청 4, 설계 전)`을 더한다. 담을 것:
  - 요청 1(문서 확인)의 결과: 첨부 도구(`D:\Download\README.md`, `run_download.ps1`, `build_lists.py`)는 아직 실행 전이다.
  - `python`이 아니라 `py -3`로 돌려야 한다.
  - 이 설치본은 Steam을 거치지 않아 구독이 동작하지 않는다(01 문서 11절).
  - 창작마당 항목은 `<창작마당ID>\debug\Mod_<모드ID>\` 모양이다(`E:\SteamLibrary\steamapps\workshop\content\1468810_disabled_claude`의 셋으로 확인). `-GameModDir`의 통째 복사는 한 단계 깊다.
  - 두 항목(2819187866, 2823508121)이 같은 `Mod_TCesd4`를 쓴다.
  - 설정은 암호화된 `ModExportData.cache`에 있어 한글화는 실행 중의 덮어쓰기가 현실적이다(미확인).
  - 비급은 `ItemProps`에 없다. `SkillCatalog`는 실행 중의 표를 읽는다.
  - SteamCMD 첫 로그인은 사용자가 직접 한다.
  - 창작마당 모드의 DLL은 게임 안에서 코드로 실행된다.
  - 설계 전에 게임에서 실험할 셋: 게임이 읽는 폴더 모양, 번역을 덮을 자리, 모드 공법이 목록에 나오는지.
- 8.7절의 회차 이름 목록에 2026-10-15에 쓴 이름(`res-1015a`~`i`와 다시 돌린 것)을 더한다.

- [ ] **Step 3: 문서를 커밋한다**

```bash
git add docs/05-editor-v0.1.md docs/06-handoff.md
git commit -m "docs: 자원 탭 개편(05 문서 38절), 인계 문서 — G2의 출발점과 Python 정정

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 4: 검토자 한 명이 브랜치 전체를 본다**

읽기 전용 서브에이전트 하나에게 맡긴다. 지시문:

```text
E:\TlToybox 저장소의 브랜치 feat/resources-tab을 develop과 비교해 검토하라. 이 작업(브랜치 검토)만 수행하라. 끝나면 보고하고 정지하라. 다른 일로 진행하지 말라.
파일을 고치지 말고, 커밋·브랜치 생성·체크아웃을 하지 말라. 모든 git/파일 명령에 절대경로 또는 `git -C E:\TlToybox`를 쓰라(cd 후 상대경로 금지).
기준 문서: E:\TlToybox\docs\superpowers\specs\2026-10-15-resources-tab-design.md(스펙), E:\TlToybox\docs\superpowers\plans\2026-10-15-resources-tab.md(계획), E:\TlToybox\docs\06-handoff.md 6절(반복한 함정).
볼 것:
(1) 스펙과 다른 동작. 계획의 "스펙 3.4절에서 바꾼 것"은 의도한 것이다.
(2) 즐겨찾기 파일이 깨지거나 사용자의 즐겨찾기를 잃는 경로. 쓰기 실패, 잠긴 파일, 깨진 줄, .bak, .tmp, 회차 스크립트의 보관·되돌리기를 본다.
(3) 지급이 두 번 되거나 거른 줄 밖의 아이템을 주는 경로.
(4) IL2CPP 프록시에서 예외가 날 수 있는 곳. null 의복 표, allProps의 원소, 게임에 없는 번호를 본다.
(5) 검사가 효과가 아니라 버튼·글만 보고 통과하는 곳.
(6) 기예 탭·가방 채우기처럼 ItemCatalog.Find에 기대는 곳의 회귀.
발견마다 파일:줄, 실패 시나리오, 확신도(확실/판단 보류)를 적어라. 명세가 부족해 판단할 수 없으면 NEEDS_CONTEXT로 보고하라.
```

검토자가 보고하면 `git -C E:/TlToybox log --oneline develop..feat/resources-tab`과 `git -C E:/TlToybox status --short`로 검토자가 아무것도 바꾸지 않았는지 본다. 같은 워크스페이스의 다른 저장소에 낯선 브랜치가 생기지 않았는지도 `git -C <그 저장소> branch`로 스팟체크한다.

- [ ] **Step 5: 검토 결과를 반영한다**

"확실"한 결함은 고치고, 그 결함을 드러내는 검사를 먼저 더한 뒤 해당 부분을 새 회차(`res-1015j`부터)에서 다시 돌린다. "판단 보류"는 게임에서 실험해 가리고, 고치지 않기로 한 것은 05 문서 38절의 "확인하지 못한 것, 한계" 끝에 이유와 함께 적는다. 고친 파일만 골라 `git add`하고, 커밋 제목은 `fix: 검토에서 나온 것 — `로 시작해 고친 결함들을 쉼표로 나열한다(묶음 C·D의 커밋 `f8e53c3`과 같은 모양). 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. 검토에서 고칠 것이 없으면 이 단계는 건너뛰고 그렇게 보고한다.

- [ ] **Step 6: develop에 병합한다**

```bash
git -C E:/TlToybox switch develop
git -C E:/TlToybox merge --no-ff feat/resources-tab -m "Merge branch 'feat/resources-tab' into develop"
git -C E:/TlToybox log --oneline -3
```

작업 브랜치를 지우는 것은 사용자에게 묻고 한다(인계 8.4절 8번).

- [ ] **Step 7: 설치한다**

사용자의 게임이 꺼져 있는지 확인하고(`Get-Process guigubahuang`), 켜져 있으면 끄도록 알리고 기다린다.

```bash
pwsh tools/install.ps1
```

Expected: 배포 빌드가 `E:\Games\Tale of Immortal\ModExportData\Mod_sWP6UL\ModCode\dll\MOD_sWP6UL.dll`에 들어간다. 06 문서 1절의 "게임에 설치된 것"에 설치 시각을 적고 커밋한다(`docs: 인계 문서 — 설치 시각`). 이 커밋은 develop에서 바로 하지 않는다. `docs/handoff-1015b` 브랜치를 만들어 커밋하고 `--no-ff`로 병합한다.

- [ ] **Step 8: 사용자에게 보고한다**

보고할 것:
- 바꾼 것과 검사 결과(부분별 `FAILS=0`, 회차 이름)
- 스펙에서 바꾼 것(맨 위 두 줄의 배치), 실행 중에 정한 것
- 직접 써 보고 알려 줄 것: 실제 마우스로 ★ 누르기, 한글 입력기로 검색, 화면이 보기 좋은지, 그룹·종류 이름이 어색한 곳
- 다음 일 G2의 출발점: 인계 8.3b. SteamCMD 첫 로그인은 사용자가 직접 한다.
