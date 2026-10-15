# 도호(칭호) 관리 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 편집기에 플레이어 전용 "도호" 탭(16번째)을 더해, 게임의 도호 100개를 보고 주기·빼기·장착·해제하게 한다. '제일' 도호는 가진 NPC에게서 옮기고, 편집기가 준 도호의 "도호 획득" 창은 편집기가 확인을 눌러 닫는다.

**Architecture:** `AppellationService`가 게임 표(`g.conf.appellationTitle`)를 한 번 읽어 목록을 두고, 게임 함수(`WorldUnitAppellationMgr.AddAppellationID`·`DelAppellationID`·`EquipAppellationID`·`UnequipAppellationID`)로 바꾼 뒤 저장 자료를 다시 읽어 판정한다. 매 프레임 `Tick`이 열린 획득 창이 편집기가 준 번호면 그 창의 확인 버튼을 누른다. `AppellationTab`은 그리기만 한다.

**Tech Stack:** C# 7.3, .NET Framework 4.7.2, Unity IL2CPP(Unhollower), MelonLoader 0.5.4. 검증은 `tools/DevRemote/session.ps1` 회차 안의 `tools/research/ui-check.ps1`.

**Spec:** `docs/superpowers/specs/2026-10-15-appellation-design.md`

## Global Constraints

- 브랜치: `feat/appellation`(develop에서 분기, 스펙 커밋이 있다). `main`·`develop`에 직접 커밋하지 않는다. 끝나면 `git merge --no-ff`로 develop에 병합한다.
- 게임 폴더는 읽기 전용. 설치는 `pwsh tools/install.ps1`만(사용자의 게임이 꺼져 있을 때). 게임 DLL을 저장소에 복사하지 않는다.
- 빌드: `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo`와 `... -p:DevTools=false -o "$TEMP/tltoybox-release-check"` 두 구성 모두 경고 0·오류 0, `src/ModCode/ModMain/bin/Release`에는 `MOD_sWP6UL.dll` 하나.
- 게임 검증은 회차 안에서만(`session.ps1 -Begin <이름> -Fixture base` … `-End <이름>`). 회차 이름은 다시 쓰지 않는다. 이 계획의 회차: `ttl-1015a`부터 차례로.
- 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다(임의로 끄지 않는다).
- 쓰기 전에 `SaveBackup.EnsureOnce()`. 바꾼 뒤 저장 자료를 다시 읽어 `ChangeLog.Add(대상, 항목, 이전, 이후, 확인됨)`.
- 게임이 붙인 것을 지우는 동작(빼기, '제일' 도호 가져오기)은 두 번 눌러 확인한다(`Confirm.Button`).
- 화면 글은 한국어. 요소 이름(스펙 3.3): `Tab15`, `AplEquipped`, `AplUnequip`, `AplClass<All|Owned|First|Romance|Champion|Spender|Dress|Free|Alchemy|Forge|Supreme|Ending|Other>`, `AplQuery`(지우기 `AplQueryClear`), `AplOwnedOnly`, `AplClear`, `AplTable*`(줄 수 글 `AplTableRange`), `AplState<번호>`, `AplName<번호>`, `AplLoss<번호>`, `AplHolder<번호>`, `AplGive<번호>`, `AplRemove<번호>`, `AplEquip<번호>`, `AplDetail`.
- 문장(검사가 본다): 주기 `도호 주기: <이름>` + 옮겼으면 ` (<이름들>에게서 옮김)` + 장착됐으면 `, 장착됨` + 경지 `. <경지> 이상이라 다음 돌파 때 잃습니다` + 이야기 `. 이야기와 묶인 효과라 확인하지 못했습니다` + `.`. 거부: `이미 가졌습니다`, `게임에 없는 번호입니다`, `갖고 있지 않습니다`, `장착한 도호가 없습니다`. 빼기 `도호 빼기: <이름>` + 장착이었으면 `, 장착이 비었습니다` + `.`. 변경 이력 항목 `도호 <이름>(<번호>)`, 이전·이후 `없음`·`가짐`·`장착`. 장착 바꾸기 항목 `도호 장착`.

## Review Focus

1. 이미 가진 도호를 다시 주면 거부되고 아무것도 바뀌지 않는다. (Task 1 `appellation`의 "giving a title already held is refused")
2. '제일' 도호를 두 NPC가 이미 함께 가진 상태(예전 편집이나 게임의 어긋남)에서 주면 둘 모두에게서 빼고 플레이어 하나만 남는다. (Task 1 `appellation`의 "two NPCs hold 1404 …", "a first title moves from every NPC holder")
3. 장착한 것이 없을 때 주면 게임이 그것을 장착하고, 결과에 "장착됨"이 붙고 그 효과가 든다. (Task 1 `appellation`의 "a gift with nothing equipped …", "the equipped effect applies")
4. 게임 표에 없는 번호(모드를 뺀 도호 등)를 주면 거부된다. (Task 1 `appellation`의 "a number the game does not have is refused")
5. 검색 칸에 번호를 치면 그 도호 한 줄이 나온다. (Task 2 `appellation-ui`의 "a number finds that title")

---

### Task 1: 도호 서비스, 획득 창 닫기, 개발 명령

**Files:**
- Create: `src/ModCode/ModMain/Services/AppellationService.cs`, `src/ModCode/ModMain/Dev/DevCommandsAppellation.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs`(명령 넘기기), `src/ModCode/ModMain/UI/EditorHost.cs`(`Tick`), `tools/research/ui-check.ps1`(부분 `appellation`, `appellation-restart`)

**Interfaces:**
- Produces:
  - `AppellationService.Entry`(`int Id; string Name; string ClassKey; string Effect; string Gain; string Desc; int DropGrade; bool Unique; bool Story; string Search`)
  - `AppellationService.ClassGroup`(`string Key; string Name; int[] Classes`), `Groups : ClassGroup[]`(First, Romance, Champion, Spender, Dress, Free, Alchemy, Forge, Supreme, Ending, Other), `AllKey = "All"`, `OwnedKey = "Owned"`, `OtherKey = "Other"`, `StoryNotice`
  - `All() : List<Entry>`, `Get(int id) : Entry`, `GroupName(string key) : string`, `HasOther : bool`
  - `Owned(WorldUnitBase unit) : HashSet<int>`, `Equipped(WorldUnitBase unit) : int`(없으면 0), `OtherHolders(WorldUnitBase unit, int id) : List<WorldUnitBase>`, `HolderText(Entry e, List<WorldUnitBase> holders) : string`
  - `CurrentGrade(WorldUnitBase unit) : int`, `DropsSoon(Entry e, int grade) : bool`, `LossText(Entry e, int grade) : string`, `DropNotice(Entry e) : string`
  - `Give`·`Remove`·`Equip(WorldUnitBase unit, int id, out string text) : bool`, `Unequip(WorldUnitBase unit, out string text) : bool`
  - `Tick()`, `Silenced : int`
  - 개발 명령: `ttl`, `ttlgive <번호>`, `ttldel <번호>`, `ttlequip <번호>`, `ttlunequip`, `ttlholders <번호>`, `ttlnpcgive <unitID|any> <번호>`

- [ ] **Step 1: 검사를 먼저 쓴다**

`tools/research/ui-check.ps1`의 `'favstore' {` 바로 앞에 넣는다:

```powershell
  'appellation' {
    # 도호 관리 스펙 5절: 서비스. 고정본 인물은 결정(대경지 3)이고 영단도인(2001)을 장착했다. 끝에서 저장한다(appellation-restart가 본다).
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "(?:^|\s)$key=([^\s|]*)") { $Matches[1] } else { '' } }
    function St { Cmd 'ttl' 300 }
    function Owns([string]$s, [int]$id) { ",$(Field $s 'owned')," -match ",$id," }
    function TitleLines([string]$pattern) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Encoding UTF8 | Where-Object { $_ -match $pattern }).Count }
    $s = St
    $me = Field $s 'me'
    Check 'the fixture has 2001 equipped' ((Field $s 'equipped') -eq '2001') $s
    $talent = [int](Field $s 'talent')
    $silenced = [int](Field $s 'silenced')
    # 주기: 장착은 그대로, 획득 창은 편집기가 확인을 눌러 닫는다
    $logged = TitleLines '도호 풍류도인\(1502\)'
    $o = Cmd 'ttlgive 1502' 1500
    Start-Sleep -Seconds 3
    $s = St
    Check 'giving adds the title and keeps the equipped one' ($o -match 'ok=True' -and (Owns $s 1502) -and (Field $s 'equipped') -eq '2001') "$o | $s"
    Check 'the editor closes its own title popup' ((Field $s 'popup') -eq 'False' -and [int](Field $s 'silenced') -gt $silenced) $s
    Check 'the change log has the gift' ((TitleLines '도호 풍류도인\(1502\)') -eq $logged + 1)
    # Review Focus 1: 이미 가진 도호
    $o = Cmd 'ttlgive 1502' 600
    Check 'giving a title already held is refused' ($o -match 'ok=False' -and $o -match '이미 가졌습니다') $o
    # Review Focus 4: 게임 표에 없는 번호
    $o = Cmd 'ttlgive 999999' 600
    Check 'a number the game does not have is refused' ($o -match 'ok=False' -and $o -match '게임에 없는 번호') $o
    # '제일' 도호. Review Focus 2: 두 NPC가 함께 가졌으면 둘 다에게서 뺀다
    $h = Cmd 'ttlholders 1404' 300
    Check 'an NPC holds 1404 in the fixture' ((Field $h 'holders') -eq '1' -and $h -notmatch "\($me\)") $h
    $o = Cmd 'ttlnpcgive any 1404' 600
    $h = Cmd 'ttlholders 1404' 300
    Check 'two NPCs hold 1404 before the gift' ($o -match 'ok=True' -and (Field $h 'holders') -eq '2') "$o | $h"
    $logged = TitleLines '도호 팔황 제일권수\(1404\)'
    $o = Cmd 'ttlgive 1404' 1500
    $h = Cmd 'ttlholders 1404' 300
    Check 'a first title moves from every NPC holder' ($o -match 'ok=True' -and $o -match '에게서 옮김' -and (Field $h 'holders') -eq '1' -and $h -match "\($me\)") "$o | $h"
    Check 'the change log has the gift and both removals' ((TitleLines '도호 팔황 제일권수\(1404\)') -eq $logged + 3)
    # 경지 밖: 결정경 인물에게 영녕 제일도수(결정경에 이르면 잃는다)
    $o = Cmd 'ttlgive 1001' 1500
    Check 'a title lost at this grade is given with a notice' ($o -match 'ok=True' -and $o -match '다음 돌파 때 잃습니다') $o
    # 장착·해제
    $o = Cmd 'ttlequip 1502' 600
    Check 'equip switches the title' ($o -match 'ok=True' -and (Field (St) 'equipped') -eq '1502') $o
    $o = Cmd 'ttlunequip' 600
    Check 'unequip leaves none' ($o -match 'ok=True' -and (Field (St) 'equipped') -eq '0') $o
    $o = Cmd 'ttlunequip' 600
    Check 'unequip with none is refused' ($o -match 'ok=False' -and $o -match '장착한 도호가 없습니다') $o
    # Review Focus 3: 장착한 것이 없을 때 주면 게임이 장착하고 효과가 든다(어계천존 talent_1_40)
    $o = Cmd 'ttlgive 2111' 1500
    $s = St
    Check 'a gift with nothing equipped is equipped and says so' ($o -match '장착됨' -and (Field $s 'equipped') -eq '2111') "$o | $s"
    Check 'the equipped effect applies (talent +40)' ([int](Field $s 'talent') -eq $talent + 40) "before=$talent | $s"
    # 장착한 것을 빼면 장착이 빈다
    $o = Cmd 'ttldel 2111' 600
    $s = St
    Check 'removing the equipped title empties the slot' ($o -match 'ok=True' -and $o -match '장착이 비었습니다' -and -not (Owns $s 2111) -and (Field $s 'equipped') -eq '0') "$o | $s"
    Check 'the effect goes with it' ([int](Field $s 'talent') -eq $talent) $s
    $o = Cmd 'ttldel 1601' 600
    Check 'removing a title not held is refused' ($o -match 'ok=False' -and $o -match '갖고 있지 않습니다') $o
    $o = Cmd 'ttlequip 1601' 600
    Check 'equipping a title not held is refused' ($o -match 'ok=False' -and $o -match '갖고 있지 않습니다') $o
    $o = Cmd 'ttlgive 2601' 1500
    Check 'a story title is given with its notice' ($o -match 'ok=True' -and $o -match '이야기와 묶인 효과라 확인하지 못했습니다') $o
    $o = Cmd 'ttlequip 2001' 600
    Check 'equip back to 2001' ($o -match 'ok=True' -and (Field (St) 'equipped') -eq '2001') $o
    Start-Sleep -Seconds 3
    Check 'no title popup is left open' ((Field (St) 'popup') -eq 'False') (St)
    Check 'save is called' ((Cmd 'save' 4000) -match 'SaveData called')
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  'appellation-restart' {
    # 도호 관리 스펙 5절: appellation 부분이 저장한 뒤 -Restart 하고 돌린다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "(?:^|\s)$key=([^\s|]*)") { $Matches[1] } else { '' } }
    $s = Cmd 'ttl' 300
    $me = Field $s 'me'
    $owned = ",$(Field $s 'owned'),"
    Check 'the given titles are still held' ($owned -match ',1001,' -and $owned -match ',1404,' -and $owned -match ',1502,' -and $owned -match ',2001,' -and $owned -notmatch ',2111,') $s
    Check 'the equipped title is still 2001' ((Field $s 'equipped') -eq '2001') $s
    $h = Cmd 'ttlholders 1404' 300
    Check 'the moved first title has one holder' ((Field $h 'holders') -eq '1' -and $h -match "\($me\)") $h
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ttl-1015a -Fixture base
pwsh tools/research/ui-check.ps1 -Part appellation
pwsh tools/DevRemote/session.ps1 -End ttl-1015a
```

Expected: `ttl`이 `UNKNOWN`이라 처음 줄부터 FAIL이 여럿, 마지막 줄 `FAILS=`가 0보다 크다.

- [ ] **Step 3: 서비스를 쓴다**

`src/ModCode/ModMain/Services/AppellationService.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 도호(칭호) 관리: 게임 표 appellationTitle을 읽고 플레이어에게 주기·빼기·장착·해제를 한다. 화면을 모른다.
    // 근거: docs/superpowers/specs/2026-10-15-appellation-design.md 2·3.2절(인계 문서 8.3d절의 실측).
    internal static class AppellationService
    {
        public sealed class Entry
        {
            public int Id;
            public string Name;
            public string ClassKey;     // ClassGroup.Key
            public string Effect;       // 장착 효과(tips)
            public string Gain;         // 얻는 법(gainDesc)
            public string Desc;
            public int DropGrade;       // 이 대경지에 이르면 돌파 때 잃는다(dropType 1). 0이면 잃지 않는다
            public bool Unique;         // 한 사람만 가진다(lossType 1, '제일' 도호)
            public bool Story;          // 이야기와 묶인 효과(결말 다섯, 단금쉬옥)
            public string Search;
        }

        public sealed class ClassGroup
        {
            public string Key;
            public string Name;
            public int[] Classes;       // className의 첫 값
        }

        public const string AllKey = "All";
        public const string OwnedKey = "Owned";
        public const string OtherKey = "Other";
        public const string StoryNotice = "이야기와 묶인 효과라 확인하지 못했습니다";
        private const int StoryOnlyId = 2500;   // 단금쉬옥(분류 110)

        public static readonly ClassGroup[] Groups =
        {
            new ClassGroup { Key = "First", Name = "제일", Classes = new[] { 101 } },
            new ClassGroup { Key = "Romance", Name = "풍류", Classes = new[] { 102 } },
            new ClassGroup { Key = "Champion", Name = "괴수", Classes = new[] { 103 } },
            new ClassGroup { Key = "Spender", Name = "산재", Classes = new[] { 104 } },
            new ClassGroup { Key = "Dress", Name = "예상", Classes = new[] { 105 } },
            new ClassGroup { Key = "Free", Name = "소요", Classes = new[] { 106 } },
            new ClassGroup { Key = "Alchemy", Name = "영단", Classes = new[] { 107 } },
            new ClassGroup { Key = "Forge", Name = "영보", Classes = new[] { 108 } },
            new ClassGroup { Key = "Supreme", Name = "천존", Classes = new[] { 109, 110, 112 } },
            new ClassGroup { Key = "Ending", Name = "결말", Classes = new[] { 2601, 2602, 2603, 2604, 2605 } },
            new ClassGroup { Key = OtherKey, Name = "기타", Classes = new int[0] },
        };

        private static List<Entry> entries;
        private static readonly HashSet<int> quiet = new HashSet<int>();   // 편집기가 준 번호: 그 획득 창은 편집기가 닫는다

        // 편집기가 대신 확인을 누른 "도호 획득" 창의 수(검사가 본다).
        public static int Silenced { get; private set; }

        public static List<Entry> All()
        {
            if (entries != null)
            {
                return entries;
            }
            var list = new List<Entry>();
            var all = g.conf.appellationTitle._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfAppellationTitleItem c = all[i];
                int cls = c.className != null && c.className.Length > 0 ? c.className[0] : 0;
                string key = KeyOf(cls);
                string name = Text(c.name);
                int drop = c.dropType == 1 && c.dropValue != null && c.dropValue.Length > 0 ? c.dropValue[0] : 0;
                list.Add(new Entry
                {
                    Id = c.id,
                    Name = name.Length > 0 ? name : "도호 " + c.id,
                    ClassKey = key,
                    Effect = Clean(c.tips),
                    Gain = Clean(c.gainDesc),
                    Desc = Clean(c.desc),
                    DropGrade = drop >= 1 && drop <= 10 ? drop : 0,
                    Unique = c.lossType == 1,
                    Story = key == "Ending" || c.id == StoryOnlyId,
                    Search = EditService.Compact(name),
                });
            }
            entries = list;
            return entries;
        }

        private static string KeyOf(int cls)
        {
            foreach (ClassGroup group in Groups)
            {
                if (Array.IndexOf(group.Classes, cls) >= 0)
                {
                    return group.Key;
                }
            }
            return OtherKey;
        }

        private static string Text(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }
            try
            {
                return GameTool.LS(key) ?? "";
            }
            catch (Exception)
            {
                return key;
            }
        }

        // 자리 표시($s_quan$ → 권법)를 풀고 색 태그를 지운다(공법 탭과 같은 함수).
        private static string Clean(string key)
        {
            return MartialService.CleanDesc(Text(key)).Replace("\r", "").Replace('\n', ' ');
        }

        public static Entry Get(int id)
        {
            return All().Find(e => e.Id == id);
        }

        public static string GroupName(string key)
        {
            foreach (ClassGroup group in Groups)
            {
                if (group.Key == key)
                {
                    return group.Name;
                }
            }
            return key;
        }

        // 게임 표에 묶음 밖의 분류가 있는지(있을 때만 기타 칩을 보인다).
        public static bool HasOther
        {
            get { return All().Exists(e => e.ClassKey == OtherKey); }
        }

        private static string TitleName(int id)
        {
            Entry e = Get(id);
            return e == null ? "도호 " + id : e.Name;
        }

        public static HashSet<int> Owned(WorldUnitBase unit)
        {
            var ids = new HashSet<int>();
            var title = unit.data.unitData.appellationTitle;
            if (title == null)
            {
                return ids;
            }
            for (int i = 0; i < title.appellationItems.Count; i++)
            {
                ids.Add(title.appellationItems[i].id);
            }
            return ids;
        }

        public static int Equipped(WorldUnitBase unit)
        {
            var title = unit.data.unitData.appellationTitle;
            return title == null || title.equipAppellationID.Count == 0 ? 0 : title.equipAppellationID[0];
        }

        // 그 도호를 가진 다른 인물(게임의 보유자 목록 WorldUnitAppellationMgr.allAppellationUnits).
        public static List<WorldUnitBase> OtherHolders(WorldUnitBase unit, int id)
        {
            var result = new List<WorldUnitBase>();
            var map = WorldUnitAppellationMgr.allAppellationUnits;
            if (map == null || !map.ContainsKey(id))
            {
                return result;
            }
            var list = map[id];
            string me = unit.data.unitData.unitID;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].data.unitData.unitID != me)
                {
                    result.Add(list[i]);
                }
            }
            return result;
        }

        // 표의 "다른 보유자" 열: '제일' 도호는 이름, 그 밖은 "NPC n명", 없으면 "-".
        public static string HolderText(Entry e, List<WorldUnitBase> holders)
        {
            if (holders.Count == 0)
            {
                return "-";
            }
            if (!e.Unique)
            {
                return "NPC " + holders.Count + "명";
            }
            var names = new List<string>();
            foreach (WorldUnitBase unit in holders)
            {
                names.Add(EditService.NameOf(unit));
            }
            return string.Join(", ", names.ToArray());
        }

        public static int CurrentGrade(WorldUnitBase unit)
        {
            return Math.Max(1, FateService.CurrentGrade(unit));
        }

        public static bool DropsSoon(Entry e, int grade)
        {
            return e.DropGrade > 0 && grade >= e.DropGrade;
        }

        // 표의 "잃는 경지" 열.
        public static string LossText(Entry e, int grade)
        {
            if (e.DropGrade == 0)
            {
                return "-";
            }
            string name = ArtifactService.MajorName(e.DropGrade);
            return DropsSoon(e, grade) ? name + " · 다음 돌파" : name;
        }

        public static string DropNotice(Entry e)
        {
            return ArtifactService.MajorName(e.DropGrade) + " 이상이라 다음 돌파 때 잃습니다";
        }

        public static bool Give(WorldUnitBase unit, int id, out string text)
        {
            Entry e = Get(id);
            if (e == null)
            {
                text = "도호: 게임에 없는 번호입니다(" + id + ").";
                return false;
            }
            if (Owned(unit).Contains(id))
            {
                text = "도호: " + e.Name + "은(는) 이미 가졌습니다.";
                return false;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            try
            {
                string field = "도호 " + e.Name + "(" + id + ")";
                var moved = new List<string>();
                if (e.Unique)
                {
                    // 한 사람만 가지는 도호: 가진 인물들에게서 먼저 뺀다(게임은 정리하지 않는다).
                    foreach (WorldUnitBase other in OtherHolders(unit, id))
                    {
                        string who = EditService.NameOf(other);
                        other.appellation.DelAppellationID(id, false);
                        ChangeLog.Add(who, field, "가짐", "없음", !Owned(other).Contains(id));
                        moved.Add(who);
                    }
                }
                bool emptyBefore = Equipped(unit) == 0;
                quiet.Add(id);
                unit.appellation.AddAppellationID(id);
                bool ok = Owned(unit).Contains(id);
                if (!ok)
                {
                    quiet.Remove(id);
                }
                bool equippedNow = emptyBefore && Equipped(unit) == id;
                ChangeLog.Add(EditService.NameOf(unit), field, "없음", equippedNow ? "장착" : "가짐", ok);
                text = "도호 주기: " + e.Name
                    + (moved.Count > 0 ? " (" + string.Join(", ", moved.ToArray()) + "에게서 옮김)" : "")
                    + (equippedNow ? ", 장착됨" : "")
                    + (DropsSoon(e, CurrentGrade(unit)) ? ". " + DropNotice(e) : "")
                    + (e.Story ? ". " + StoryNotice : "")
                    + (ok ? "." : ". 게임이 넣지 않았습니다.");
                return ok;
            }
            catch (Exception ex)
            {
                text = "도호: 게임이 받지 않았습니다(" + ex.GetType().Name + ").";
                return false;
            }
        }

        public static bool Remove(WorldUnitBase unit, int id, out string text)
        {
            string name = TitleName(id);
            if (!Owned(unit).Contains(id))
            {
                text = "도호: " + name + "은(는) 갖고 있지 않습니다.";
                return false;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            try
            {
                bool wasEquipped = Equipped(unit) == id;
                unit.appellation.DelAppellationID(id, false);
                bool ok = !Owned(unit).Contains(id);
                ChangeLog.Add(EditService.NameOf(unit), "도호 " + name + "(" + id + ")", wasEquipped ? "장착" : "가짐", "없음", ok);
                text = "도호 빼기: " + name + (wasEquipped && ok ? ", 장착이 비었습니다" : "") + (ok ? "." : ". 게임이 빼지 않았습니다.");
                return ok;
            }
            catch (Exception ex)
            {
                text = "도호: 게임이 받지 않았습니다(" + ex.GetType().Name + ").";
                return false;
            }
        }

        public static bool Equip(WorldUnitBase unit, int id, out string text)
        {
            string name = TitleName(id);
            if (!Owned(unit).Contains(id))
            {
                text = "도호: " + name + "은(는) 갖고 있지 않습니다.";
                return false;
            }
            int before = Equipped(unit);
            if (before == id)
            {
                text = "도호: " + name + "은(는) 이미 장착했습니다.";
                return false;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            try
            {
                unit.appellation.EquipAppellationID(id);
                bool ok = Equipped(unit) == id;
                ChangeLog.Add(EditService.NameOf(unit), "도호 장착", before == 0 ? "없음" : TitleName(before), name, ok);
                text = "도호 장착: " + name + (ok ? "." : ". 게임이 장착하지 않았습니다.");
                return ok;
            }
            catch (Exception ex)
            {
                text = "도호: 게임이 받지 않았습니다(" + ex.GetType().Name + ").";
                return false;
            }
        }

        public static bool Unequip(WorldUnitBase unit, out string text)
        {
            int current = Equipped(unit);
            if (current == 0)
            {
                text = "도호: 장착한 도호가 없습니다.";
                return false;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            try
            {
                unit.appellation.UnequipAppellationID(current);
                bool ok = Equipped(unit) == 0;
                ChangeLog.Add(EditService.NameOf(unit), "도호 장착", TitleName(current), "없음", ok);
                text = "도호 장착 해제: " + TitleName(current) + (ok ? "." : ". 게임이 해제하지 않았습니다.");
                return ok;
            }
            catch (Exception ex)
            {
                text = "도호: 게임이 받지 않았습니다(" + ex.GetType().Name + ").";
                return false;
            }
        }

        // 매 프레임. 편집기가 준 번호의 "도호 획득" 창이 열려 있으면 그 창의 확인 버튼을 누른다.
        // CloseUI로 닫으면 창 대기열이 멈춰 그 뒤의 창이 뜨지 않는다(회차 apl-1015d). 확인을 누르면 대기열이 이어진다(apl-1015e).
        public static void Tick()
        {
            if (quiet.Count == 0)
            {
                return;
            }
            try
            {
                UIGetTaoist ui = g.ui.GetUI<UIGetTaoist>(UIType.GetTaoist);
                if (ui == null || ui.conf == null || !quiet.Remove(ui.conf.id))
                {
                    return;
                }
                Silenced++;
                ui.btnOK.onClick.Invoke();
            }
            catch (Exception)
            {
                // 매 프레임 불리므로 예외를 던지지 않는다.
            }
        }
    }
}
```

- [ ] **Step 4: 매 프레임 `Tick`을 부른다**

`src/ModCode/ModMain/UI/EditorHost.cs`의 `EditorHostBehaviour.Update`에서 `InputGuard.Tick();` 바로 아래에 넣는다:

```csharp
            AppellationService.Tick();
```

- [ ] **Step 5: 개발 명령을 쓴다**

`src/ModCode/ModMain/Dev/DevCommandsAppellation.cs`:

```csharp
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 도호 탭의 검증용 명령(tools/research/ui-check.ps1의 appellation 부분).
    //   ttl                            : me=<unitID> owned=<번호,…> equipped=<번호|0> talent=<오성> popup=<획득 창이 열렸는지> silenced=<편집기가 닫은 창 수> grade=<대경지>
    //   ttlgive|ttldel|ttlequip <번호> : ok=<성공> | <문장> | <ttl>
    //   ttlunequip                     : ok=<성공> | <문장> | <ttl>
    //   ttlholders <번호>              : holders=<수> names=<이름(unitID)>,…(플레이어 포함)
    //   ttlnpcgive <unitID|any> <번호> : 검사 준비. 그 NPC(any면 그 도호가 없는 첫 NPC)에게 게임 함수로 바로 넣는다. unit=<unitID> ok=<결과>
    internal static class DevCommandsAppellation
    {
        public static string Execute(string[] p)
        {
            string text;
            bool ok;
            switch (p[0])
            {
                case "ttl":
                    return State();
                case "ttlgive":
                    ok = AppellationService.Give(g.world.playerUnit, int.Parse(p[1]), out text);
                    return "ok=" + ok + " | " + text + " | " + State();
                case "ttldel":
                    ok = AppellationService.Remove(g.world.playerUnit, int.Parse(p[1]), out text);
                    return "ok=" + ok + " | " + text + " | " + State();
                case "ttlequip":
                    ok = AppellationService.Equip(g.world.playerUnit, int.Parse(p[1]), out text);
                    return "ok=" + ok + " | " + text + " | " + State();
                case "ttlunequip":
                    ok = AppellationService.Unequip(g.world.playerUnit, out text);
                    return "ok=" + ok + " | " + text + " | " + State();
                case "ttlholders":
                    return Holders(int.Parse(p[1]));
                case "ttlnpcgive":
                    return NpcGive(p[1], int.Parse(p[2]));
                default:
                    return null;
            }
        }

        private static string State()
        {
            WorldUnitBase me = g.world.playerUnit;
            var owned = new List<int>(AppellationService.Owned(me));
            owned.Sort();
            var ids = new List<string>();
            foreach (int id in owned)
            {
                ids.Add(id.ToString());
            }
            return "me=" + me.data.unitData.unitID + " owned=" + string.Join(",", ids.ToArray())
                + " equipped=" + AppellationService.Equipped(me) + " talent=" + me.data.dynUnitData.talent.value
                + " popup=" + (g.ui.GetUI<UIGetTaoist>(UIType.GetTaoist) != null) + " silenced=" + AppellationService.Silenced
                + " grade=" + AppellationService.CurrentGrade(me);
        }

        private static string Holders(int id)
        {
            var names = new List<string>();
            var map = WorldUnitAppellationMgr.allAppellationUnits;
            if (map != null && map.ContainsKey(id))
            {
                var list = map[id];
                for (int i = 0; i < list.Count; i++)
                {
                    names.Add(EditService.NameOf(list[i]) + "(" + list[i].data.unitData.unitID + ")");
                }
            }
            return "holders=" + names.Count + " names=" + string.Join(",", names.ToArray());
        }

        private static string NpcGive(string who, int id)
        {
            string me = g.world.playerUnit.data.unitData.unitID;
            WorldUnitBase unit = null;
            if (who != "any")
            {
                unit = g.world.unit.GetUnit(who, true);
            }
            else
            {
                var units = g.world.unit.GetUnits(false);
                for (int i = 0; i < units.Count && unit == null; i++)
                {
                    if (units[i].data.unitData.unitID != me && !AppellationService.Owned(units[i]).Contains(id))
                    {
                        unit = units[i];
                    }
                }
            }
            if (unit == null)
            {
                return "no unit " + who;
            }
            bool ok = unit.appellation.AddAppellationID(id);
            return "unit=" + unit.data.unitData.unitID + " ok=" + ok;
        }
    }
}
```

`src/ModCode/ModMain/Dev/DevCommands.cs`의 `Execute`에서 `string resources = DevCommandsResources.Execute(p);` 바로 앞에 넣는다:

```csharp
            string titles = DevCommandsAppellation.Execute(p);
            if (titles != null)
            {
                return titles;
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
pwsh tools/DevRemote/session.ps1 -Begin ttl-1015b -Fixture base
pwsh tools/research/ui-check.ps1 -Part appellation
pwsh tools/DevRemote/session.ps1 -Restart
pwsh tools/research/ui-check.ps1 -Part appellation-restart
pwsh tools/DevRemote/session.ps1 -End ttl-1015b
```

Expected: 두 부분 `FAILS=0`.

"the editor closes its own title popup"이 `silenced`가 늘지 않아 실패하면, NPC의 반응 대화가 창 대기열을 막고 있었을 수 있다(인계 8.3d절). 그때는 `uilist`로 `DramaDialogue`가 열려 있는지 보고 기록한다. 대화가 없는데도 늘지 않으면 `Tick`이 불리는지(`EditorHost.cs`)를 본다.

- [ ] **Step 8: 커밋**

```bash
git add src/ModCode/ModMain/Services/AppellationService.cs src/ModCode/ModMain/Dev/DevCommandsAppellation.cs src/ModCode/ModMain/Dev/DevCommands.cs src/ModCode/ModMain/UI/EditorHost.cs tools/research/ui-check.ps1
git commit -m "feat: 도호 서비스 — 주기·빼기·장착, 제일 도호 옮기기, 편집기가 준 도호의 획득 창 닫기

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 도호 탭

**Files:**
- Create: `src/ModCode/ModMain/UI/Tabs/AppellationTab.cs`
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs`(탭 목록, 탭 줄 높이), `tools/research/ui-check.ps1`(`$tabProbe`, 부분 `appellation-ui`)

**Interfaces:**
- Consumes: Task 1의 `AppellationService`(Entry, Groups, AllKey, OwnedKey, OtherKey, StoryNotice, All, Get, GroupName, HasOther, Owned, Equipped, OtherHolders, HolderText, CurrentGrade, DropsSoon, LossText, DropNotice, Give, Remove, Equip, Unequip), 개발 명령 `ttl`, `ttlholders`.
- Produces: 화면 요소 이름(Global Constraints).

- [ ] **Step 1: 검사를 먼저 쓴다**

`tools/research/ui-check.ps1` 위쪽의 `$tabProbe` 배열 끝(`'BackupNow'` 뒤)에 `'AplClassAll'`을 더한다:

```powershell
$tabProbe = @('Restore', 'MoneyAdd', 'CurrencyAdd10001', 'VaultModeBag', 'FillExp', 'SkillAddOpen', 'ImmortalAddOpen', 'ArtifactPoint', 'LuckAddOpen', 'ArtLearnAll', 'NpcRefresh', 'Freeze', 'God', 'PresetRestore', 'BackupNow', 'AplClassAll')
```

같은 파일의 `'favstore' {` 바로 앞에 넣는다:

```powershell
  'appellation-ui' {
    # 도호 관리 스펙 5절: 화면. 고정본 인물은 결정(대경지 3)이고 영단도인(2001)을 장착했다. 관산이 팔황 제일권수(1404)를 가졌다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "(?:^|\s)$key=([^\s|]*)") { $Matches[1] } else { '' } }
    function Num([string]$name) { if ((Cmd "uitext $name") -match "text='[^']*?(\d+)'") { [int]$Matches[1] } else { -1 } }
    function Shown { $t = Cmd 'uitext AplTableRange'; if ($t -match '전체 (\d+)개') { [int]$Matches[1] } elseif ($t -match "text='0개'") { 0 } else { -1 } }
    function Owns([int]$id) { ",$(Field (Cmd 'ttl' 300) 'owned')," -match ",$id," }
    $me = Field (Cmd 'ttl' 300) 'me'
    Cmd 'editor' | Out-Null; Press 'Tab15' | Out-Null
    Check 'the top line shows the equipped title and its effect' ((Cmd 'uitext AplEquipped') -match '영단도인 — .+') (Cmd 'uitext AplEquipped')
    Check 'All counts 100' ((Num 'AplClassAll') -eq 100) (Cmd 'uitext AplClassAll')
    $sum = 0
    foreach ($k in 'First', 'Romance', 'Champion', 'Spender', 'Dress', 'Free', 'Alchemy', 'Forge', 'Supreme', 'Ending', 'Other') { $n = Num "AplClass$k"; if ($n -gt 0) { $sum += $n } }
    Check 'the class chips add up to 100' ($sum -eq 100) "sum=$sum"
    Check 'First has 60 and Owned has 1' ((Num 'AplClassFirst') -eq 60 -and (Num 'AplClassOwned') -eq 1)
    Check 'the table lists all 100' ((Shown) -eq 100) (Cmd 'uitext AplTableRange')
    # 검색과 가진 것만. Review Focus 5: 번호로 찾기
    Cmd 'ui set AplQuery 권수' | Out-Null
    $n = Shown
    Check 'searching narrows the table' ($n -gt 0 -and $n -lt 100) "shown=$n"
    Cmd 'ui set AplQuery 1404' | Out-Null
    Check 'a number finds that title' ((Shown) -eq 1 -and (Cmd 'uipos AplName1404') -match ' at ') (Cmd 'uitext AplTableRange')
    Press 'AplQueryClear' | Out-Null
    Press 'AplOwnedOnly' | Out-Null
    Check 'owned only shows the held title as equipped' ((Shown) -eq 1 -and (Cmd 'uitext AplState2001') -match "text='★'") (Cmd 'uitext AplTableRange')
    Press 'AplClear' | Out-Null
    Check 'clear restores the full table' ((Shown) -eq 100)
    # 잃는 경지 경고
    Cmd 'ui set AplQuery 1001' | Out-Null
    Check 'a title lost at this grade warns' ((Cmd 'uitext AplLoss1001') -match '다음 돌파') (Cmd 'uitext AplLoss1001')
    # '제일' 도호 가져오기: 두 번 눌러야 옮긴다
    $h = Cmd 'ttlholders 1404' 300
    $holder = if ($h -match 'names=([^(]+)\(') { $Matches[1] } else { '?' }
    Cmd 'ui set AplQuery 1404' | Out-Null
    Check 'the holder column and the give button name the holder' ((Cmd 'uitext AplHolder1404') -match $holder -and (Cmd 'uitext AplGive1404') -match "$($holder)에게서 가져오기") "$(Cmd 'uitext AplHolder1404') | $(Cmd 'uitext AplGive1404')"
    $silenced = [int](Field (Cmd 'ttl' 300) 'silenced')
    Press 'AplGive1404' | Out-Null
    Check 'one press does not move it' (-not (Owns 1404))
    Press 'AplGive1404' 1500 | Out-Null
    $h = Cmd 'ttlholders 1404' 300
    Check 'the second press moves it to the player' ((Field $h 'holders') -eq '1' -and $h -match "\($me\)") $h
    Check 'the result names the holder' ((Cmd 'uistate') -match '에게서 옮김') (Cmd 'uistate')
    Start-Sleep -Seconds 3
    $s = Cmd 'ttl' 300
    Check 'its popup was closed by the editor' ((Field $s 'popup') -eq 'False' -and [int](Field $s 'silenced') -gt $silenced) $s
    # 아래 칸
    Press 'AplName1404' | Out-Null
    $d = Cmd 'uitext AplDetail'
    Check 'the detail shows effect, how to get it and the holder' ($d -match '효과: .*권법' -and $d -match '얻는 법: ' -and $d -match '보유자: 나') $d
    Check 'the detail has no raw text keys' ($d -notmatch '\$') $d
    # 빼기도 두 번
    Press 'AplRemove1404' | Out-Null
    Check 'one press does not remove it' (Owns 1404)
    Press 'AplRemove1404' | Out-Null
    Check 'the second press removes it' (-not (Owns 1404))
    # 장착 칸과 줄의 장착
    Press 'AplClear' | Out-Null
    Press 'AplUnequip' | Out-Null
    Check 'unequip empties the top line' ((Cmd 'uitext AplEquipped') -match '장착한 도호가 없습니다') (Cmd 'uitext AplEquipped')
    Cmd 'ui set AplQuery 2001' | Out-Null
    Press 'AplEquip2001' | Out-Null
    Check 'equip from the row' ((Cmd 'uitext AplEquipped') -match '영단도인' -and (Cmd 'uitext AplEquip2001') -match '장착 중') "$(Cmd 'uitext AplEquipped') | $(Cmd 'uitext AplEquip2001')"
    # 이야기 도호 안내
    Cmd 'ui set AplQuery 2601' | Out-Null
    Press 'AplName2601' | Out-Null
    Check 'a story title says its effect is unverified' ((Cmd 'uitext AplDetail') -match '이야기와 묶인 효과라 확인하지 못했습니다') (Cmd 'uitext AplDetail')
    Press 'AplClear' | Out-Null
    Shot 'appellation-ui'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ttl-1015c -Fixture base
pwsh tools/research/ui-check.ps1 -Part appellation-ui
pwsh tools/research/ui-check.ps1 -Part base
pwsh tools/DevRemote/session.ps1 -End ttl-1015c
```

Expected: 둘 다 `편집기에서 요소를 찾지 못했습니다: Tab15`로 멈춘다.

- [ ] **Step 3: 탭을 쓴다**

`src/ModCode/ModMain/UI/Tabs/AppellationTab.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 도호 탭: 플레이어의 도호(칭호) 보기·주기·빼기·장착. 설계: docs/superpowers/specs/2026-10-15-appellation-design.md 3.1절.
    // 판단은 AppellationService가 하고 여기서는 그린다. 편집 대상을 따르지 않는다(플레이어 전용).
    internal sealed class AppellationTab : EditorTab
    {
        private const float RowH = 34;
        private const float TopH = 44;
        private const float DetailH = 150;
        private const float ChipH = 34, ChipGap = 6;
        private const float ColState = 4, ColName = 52, ColClass = 362, ColLoss = 458, ColHolder = 664;
        private const float StateW = 44, NameW = 300, ClassW = 90, LossW = 200, HolderW = 200;
        private const float ActW = 240;
        private const float ColAct = ContentW - Ui.ScrollBarWidth - ActW - 4;

        // 검색어가 바뀌면 탭을 다시 그리지 않고(검색 칸의 포커스를 지킨다) 칩의 수만 고친다.
        private sealed class CountLabel
        {
            public Text Text;
            public string Name;
            public string Key;
        }

        private delegate bool TitleAction(WorldUnitBase unit, int id, out string text);

        private readonly ListView<AppellationService.Entry> list = new ListView<AppellationService.Entry>("AplTable", RowH);
        private readonly DetailPane detail = new DetailPane("AplDetail", "줄을 누르면 여기에 효과와 얻는 법이 나옵니다.");
        private readonly List<CountLabel> labels = new List<CountLabel>();
        private string classKey = AppellationService.AllKey;
        private string query = "";
        private bool ownedOnly;
        private HashSet<int> owned = new HashSet<int>();   // 그릴 때마다 읽는다
        private int equipped;
        private int grade = 1;

        public override string Title
        {
            get { return "도호"; }
        }

        public AppellationTab()
        {
            list.EmptyText = "조건에 맞는 도호가 없습니다.";
            list.OnSelect = e => detail.Show(DetailOf(e));
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            owned = AppellationService.Owned(player);
            equipped = AppellationService.Equipped(player);
            grade = AppellationService.CurrentGrade(player);
            labels.Clear();
            DrawTop();
            float y = TopH + 8;
            y = DrawChips(y);
            y = DrawFilters(y);
            DrawHead(y);
            y += 32;
            list.HeightOf = e => RowFor(RowH, Ui.TextHeight(e.Name, NameW, 18));
            list.Build(content, 0, y, ContentW, ContentH - DetailH - 8 - y, Rows(), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawTop()
        {
            Ui.Panel("AplTop", content, 0, 0, ContentW, TopH, Ui.Section);
            Ui.Label(content, "장착 중", 12, 2, 80, TopH - 4, 17).color = Ui.Dim;
            AppellationService.Entry e = equipped == 0 ? null : AppellationService.Get(equipped);
            string text = equipped == 0 ? "장착한 도호가 없습니다."
                : e == null ? "도호 " + equipped
                : e.Name + " — " + (e.Effect.Length > 0 ? e.Effect : "효과 글 없음");
            Ui.Label(content, text, 92, 2, ContentW - 92 - 140, TopH - 4, 17).gameObject.name = "AplEquipped";
            Ui.Button("AplUnequip", content, "장착 해제", ContentW - 130, 4, 120, TopH - 8, new Action(() =>
            {
                string result;
                AppellationService.Unequip(Player, out result);
                Say(result);
            }), equipped != 0);
        }

        private bool Passes(AppellationService.Entry e)
        {
            if (ownedOnly && !owned.Contains(e.Id))
            {
                return false;
            }
            string needle = EditService.Compact(query ?? "");
            int id;
            bool isId = int.TryParse(needle, out id);
            return needle.Length == 0 || e.Search.Contains(needle) || (isId && e.Id == id);
        }

        private bool InClass(AppellationService.Entry e, string key)
        {
            if (key == AppellationService.AllKey)
            {
                return true;
            }
            if (key == AppellationService.OwnedKey)
            {
                return owned.Contains(e.Id);
            }
            return e.ClassKey == key;
        }

        private int Count(string key)
        {
            int n = 0;
            foreach (AppellationService.Entry e in AppellationService.All())
            {
                if (Passes(e) && InClass(e, key))
                {
                    n++;
                }
            }
            return n;
        }

        private List<AppellationService.Entry> Rows()
        {
            var rows = new List<AppellationService.Entry>();
            foreach (AppellationService.Entry e in AppellationService.All())
            {
                if (Passes(e) && InClass(e, classKey))
                {
                    rows.Add(e);
                }
            }
            return rows;
        }

        // 분류 칩: 전체, 가진 것, 묶음들(기타는 표에 있을 때만). 줄바꿈해 놓는다. 다음 줄의 y를 돌려준다.
        private float DrawChips(float y)
        {
            var keys = new List<string> { AppellationService.AllKey, AppellationService.OwnedKey };
            var names = new List<string> { "전체", "가진 것" };
            foreach (AppellationService.ClassGroup group in AppellationService.Groups)
            {
                if (group.Key == AppellationService.OtherKey && !AppellationService.HasOther)
                {
                    continue;
                }
                keys.Add(group.Key);
                names.Add(group.Name);
            }
            float x = 0, cy = y;
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                float w = Math.Max(70f, Ui.TextWidth(names[i] + " 000", 18) + 28f);
                if (x > 0 && x + w > ContentW)
                {
                    x = 0;
                    cy += ChipH + ChipGap;
                }
                Button chip = Ui.Button("AplClass" + key, content, names[i] + " " + Count(key), x, cy, w, ChipH,
                    new Action(() => { classKey = key; Changed(); }), true, classKey == key);
                labels.Add(new CountLabel { Text = chip.GetComponentInChildren<Text>(), Name = names[i], Key = key });
                x += w + ChipGap;
            }
            return cy + ChipH + 8;
        }

        private float DrawFilters(float y)
        {
            Ui.Label(content, "도호명", 0, y, 76, 38, 18);
            SearchBox.Build(content, "AplQuery", 76, y, 320, query, "이름의 일부 또는 번호", new Action<string>(OnQuery));
            Ui.Button("AplOwnedOnly", content, "가진 것만", 406, y, 120, 38, new Action(() => { ownedOnly = !ownedOnly; Changed(); }), true, ownedOnly);
            Ui.Button("AplClear", content, "초기화", 532, y, 100, 38, new Action(() =>
            {
                query = "";
                ownedOnly = false;
                classKey = AppellationService.AllKey;
                Changed();
            }));
            return y + 44;
        }

        private void DrawHead(float y)
        {
            Ui.Panel("AplHead", content, 0, y, ContentW, 30, Ui.Section);
            Ui.Label(content, "상태", ColState, y, StateW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            Ui.Label(content, "도호명", ColName, y, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "분류", ColClass, y, ClassW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "잃는 경지", ColLoss, y, LossW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "다른 보유자", ColHolder, y, HolderW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "버튼", ColAct, y, ActW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
        }

        // 분류·가진 것만·초기화: 목록을 맨 위로, 아래 칸을 비우고 다시 그린다.
        private void Changed()
        {
            list.Reset();
            detail.Clear();
            Redraw();
        }

        // 검색어가 바뀌었을 때: 목록과 칩의 수만 고친다.
        private void OnQuery(string value)
        {
            query = value;
            detail.Clear();
            list.SetItems(Rows());
            foreach (CountLabel label in labels)
            {
                label.Text.text = label.Name + " " + Count(label.Key);
            }
        }

        private void DrawRow(RectTransform row, AppellationService.Entry e, int index)
        {
            float h = row.sizeDelta.y;
            int id = e.Id;
            bool has = owned.Contains(id);
            bool on = equipped == id;
            Ui.Label(row, on ? "★" : has ? "●" : "-", ColState, 0, StateW, h, 18, TextAnchor.MiddleCenter).gameObject.name = "AplState" + id;
            WrapLabel(Ui.Button("AplName" + id, row, e.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(DetailOf(e)))));
            Ui.Label(row, AppellationService.GroupName(e.ClassKey), ColClass, 0, ClassW, h, 16);
            Text loss = Ui.Label(row, AppellationService.LossText(e, grade), ColLoss, 0, LossW, h, 16);
            loss.gameObject.name = "AplLoss" + id;
            if (AppellationService.DropsSoon(e, grade))
            {
                loss.color = Ui.Warn;
            }
            List<WorldUnitBase> holders = AppellationService.OtherHolders(Player, id);
            Ui.Label(row, AppellationService.HolderText(e, holders), ColHolder, 0, HolderW, h, 16).gameObject.name = "AplHolder" + id;
            if (has)
            {
                Confirm.Button("AplRemove" + id, row, "빼기", "정말 빼기", ColAct, 2, 110, RowH - 4, "apl-remove-" + id,
                    e.Name + "을(를) 뺍니다. 한 번 더 누르면 실행합니다.", new Action(() => Run(AppellationService.Remove, id)));
                Ui.Button("AplEquip" + id, row, on ? "장착 중" : "장착", ColAct + 120, 2, ActW - 120, RowH - 4,
                    new Action(() => Run(AppellationService.Equip, id)), !on, on);
            }
            else if (e.Unique && holders.Count > 0)
            {
                string who = EditService.NameOf(holders[0]) + (holders.Count > 1 ? " 외 " + (holders.Count - 1) + "명" : "");
                Confirm.Button("AplGive" + id, row, who + "에게서 가져오기", "정말 가져오기", ColAct, 2, ActW, RowH - 4, "apl-take-" + id,
                    who + "에게서 " + e.Name + "을(를) 가져옵니다. 한 번 더 누르면 실행합니다.", new Action(() => Run(AppellationService.Give, id)));
            }
            else
            {
                Ui.Button("AplGive" + id, row, "주기", ColAct, 2, ActW, RowH - 4, new Action(() => Run(AppellationService.Give, id)));
            }
        }

        private static void Run(TitleAction action, int id)
        {
            string text;
            action(Player, id, out text);
            Say(text);
        }

        private string DetailOf(AppellationService.Entry e)
        {
            List<WorldUnitBase> holders = AppellationService.OtherHolders(Player, e.Id);
            var names = new List<string>();
            if (owned.Contains(e.Id))
            {
                names.Add("나");
            }
            for (int i = 0; i < holders.Count && i < 10; i++)
            {
                names.Add(EditService.NameOf(holders[i]));
            }
            if (holders.Count > 10)
            {
                names.Add("외 " + (holders.Count - 10) + "명");
            }
            string text = "[" + e.Id + "] " + e.Name + " · " + AppellationService.GroupName(e.ClassKey)
                + "\n효과: " + (e.Effect.Length > 0 ? e.Effect : "-")
                + "\n얻는 법: " + (e.Gain.Length > 0 ? e.Gain : "-")
                + "\n설명: " + (e.Desc.Length > 0 ? e.Desc : "-")
                + "\n보유자: " + (names.Count > 0 ? string.Join(", ", names.ToArray()) : "없음");
            if (AppellationService.DropsSoon(e, grade))
            {
                text += "\n" + AppellationService.DropNotice(e) + ".";
            }
            if (e.Story)
            {
                text += "\n" + AppellationService.StoryNotice + ".";
            }
            return text;
        }
    }
}
```

- [ ] **Step 4: 창에 탭을 단다**

`src/ModCode/ModMain/UI/EditorWindow.cs`:

탭 줄을 만드는 줄의 `15 * 52`를 `16 * 52`로 바꾼다(16번째 탭의 아래 끝 64 + 15×52 + 44 = 888이 아래 결과 칸의 위 끝 908보다 위에 있다):

```csharp
            tabBar = Ui.Rect("Tabs", window, 16, ContentY, 150, 16 * 52);
```

탭 목록의 `new SafetyTab(),` 뒤에 `new AppellationTab(),`를 더한다:

```csharp
                new ArtTab(), new NpcTab(), new WorldTab(), new RateTab(), new PresetTab(), new SafetyTab(), new AppellationTab(),
```

- [ ] **Step 5: 두 구성으로 빌드한다**

Task 1 Step 6과 같다. Expected: 경고·오류 0, DLL 하나.

- [ ] **Step 6: 검사가 통과하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin ttl-1015d -Fixture base
pwsh tools/research/ui-check.ps1 -Part appellation-ui
pwsh tools/research/ui-check.ps1 -Part base
pwsh tools/research/ui-check.ps1 -Part tabs
pwsh tools/research/ui-check.ps1 -Part wrap
pwsh tools/research/ui-check.ps1 -Part items
pwsh tools/DevRemote/session.ps1 -End ttl-1015d
pwsh tools/DevRemote/session.ps1 -Begin ttl-1015e -Fixture base
pwsh tools/research/ui-check.ps1 -Part appellation
pwsh tools/DevRemote/session.ps1 -End ttl-1015e
```

Expected: 모든 부분 `FAILS=0`.

캡처 `.devremote\check-appellation-ui.png`를 Read로 열어 본다. 볼 것: 탭 줄 16번째 "도호"가 아래 결과 칸과 겹치지 않는가, 칩 줄·필터 줄·표 머리·줄의 글이 겹치지 않는가, 버튼 열이 표 안에 들어가는가.

- [ ] **Step 7: 커밋**

```bash
git add src/ModCode/ModMain/UI/Tabs/AppellationTab.cs src/ModCode/ModMain/UI/EditorWindow.cs tools/research/ui-check.ps1
git commit -m "feat: 도호 탭 — 장착 칸, 분류 칩, 검색, 표의 주기·빼기·장착·가져오기, 아래 칸

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 문서, 검토, 병합, 설치

**Files:**
- Modify: `docs/05-editor-v0.1.md`(40절), `docs/06-handoff.md`

- [ ] **Step 1: 05 문서에 40절을 쓴다**

`docs/05-editor-v0.1.md` 끝에 `## 40. 도호(칭호) 관리 (2026-10-15 추가)`를 더한다. 담을 것:
- `### 쓰는 법`: 도호 탭(16번째, 플레이어 전용), 장착 칸, 분류 칩, 검색·가진 것만·초기화, 표의 열과 버튼, 두 번 눌러 확인, 아래 칸
- `### 규칙과 실측`: 스펙 2절의 실측 표를 줄여 옮기고, 획득 창 닫기(확인 버튼, `CloseUI`가 대기열을 멈춘 것), '제일' 도호 옮기기, 경지 안내, 이야기 도호, 효과 글의 자리 표시, 경지 올리기·프리셋 적용이 도호를 다시 판정한다는 것
- `### 구조`: 파일, 요소 이름, 개발 명령(`ttl`, `ttlgive`, `ttldel`, `ttlequip`, `ttlunequip`, `ttlholders`, `ttlnpcgive`), 탭 줄 높이
- `### 검증`: 회차 이름과 부분별 결과(고치기 전 실패와 고친 뒤 통과)
- `### 확인하지 못한 것, 한계`: 스펙 8절과 실행 중에 정한 것

- [ ] **Step 2: 인계 문서를 고친다**

`docs/06-handoff.md`:
- 1절 직전 작업에 도호 관리(40절)를 더한다. 다음 일은 G2(8.3b절, 받는 길이 막혀 있음)로 적는다.
- 2절 탭 순서에 "도호"(16개, `Tab15`)를 더하고, 표에 도호 줄(기능, `AppellationService`·`AppellationTab`, 40절)을 더한다. 탭 줄이 가득 찼다는 것을 적는다.
- 5절 검사 표에 `appellation`(→ `-Restart` → `appellation-restart`), `appellation-ui`를 더하고, 개발 명령에 `ttl`… 줄을 더한다.
- 8.3d절의 제목을 "(끝남)"으로 바꾸고 첫 줄에 "05 문서 40절로 만들었다"를 더한다.
- 8.7절 회차 이름에 `apl-1015c`~`f`와 `ttl-1015a`~(쓴 것까지).

- [ ] **Step 3: 문서를 커밋한다**

```bash
git add docs/05-editor-v0.1.md docs/06-handoff.md
git commit -m "docs: 도호(칭호) 관리(05 문서 40절), 인계 문서

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 4: 검토자 한 명이 브랜치 전체를 본다**

읽기 전용 서브에이전트 하나에게 맡긴다(가장 높은 모델). superpowers:requesting-code-review의 양식에, 아래를 넣는다.
- 범위 고정 문구
- 절대경로 지시
- 스펙·계획·진행 기록의 경로
- 이 계획의 Review Focus 다섯 줄 그대로

볼 것:
- 주기·빼기가 두 번 되거나, 거른 줄 밖의 도호를 건드리는 경로
- '제일' 도호 옮기기에서 NPC에게서 빼고 넣기에 실패하는 경우(빼기만 되고 넣기가 안 된 상태)
- `Tick`이 다른 획득 창(게임에서 얻은 것)을 닫는 경로
- IL2CPP 예외가 버튼 콜백 밖으로 나가는 곳
- 검사가 효과 대신 글만 보는 곳

- [ ] **Step 5: 검토 결과를 반영한다**

Critical·Important는 실패하는 검사를 먼저 넣고 고친 뒤, 해당 부분을 새 회차에서 다시 돌린다. Minor는 05 문서 40절의 "확인하지 못한 것, 한계"에 적는다. 고친 파일만 골라 커밋하고, 제목은 `fix: 검토에서 나온 것 — `로 시작한다. 고칠 것이 없으면 건너뛰고 그렇게 보고한다.

- [ ] **Step 6: develop에 병합하고 설치한다**

```bash
git -C E:/TlToybox switch develop
git -C E:/TlToybox merge --no-ff feat/appellation -m "Merge branch 'feat/appellation' into develop"
```

사용자의 게임이 꺼져 있는지 확인하고(켜져 있으면 끄도록 알리고 기다린다) 설치한다:

```bash
pwsh tools/install.ps1
```

인계 문서 1절의 "게임에 설치된 것"에 설치 시각을 적는다. 이 고침은 `docs/handoff-1015d` 브랜치에서 커밋하고 `--no-ff`로 병합한다.

- [ ] **Step 7: 사용자에게 보고한다(한국어)**

보고할 것:
- 바꾼 것과 검사 결과
- 직접 써 볼 것: 실제 마우스로 누르기, 한글 입력기로 검색, 게임의 인물 화면에서 바뀐 도호가 보이는지, 이야기 도호를 줬을 때의 효과
- 다음 일: G2(받는 길이 막혀 있음)
