# 선법 탭 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 공법 다음에 "선법" 탭을 두어 본원 선력을 보이고 바꾸며, 게임의 모든 선법을 선인·종류·이름으로 걸러 하나씩 또는 모두 익히게 한다.

**Architecture:** 선법의 목록·보유·익히기는 새 서비스 `ImmortalService`가 맡는다(선인 표에서 카드 목록을 만들고, 게임 함수 `ImmortalCard.AddImmortalCard`로 더한다). 화면은 새 탭 `ImmortalTab`(첫 화면 + `Push`한 선법 추가 화면). 선력은 능력치 탭의 정의(`AttributeCatalog`의 `immortalPoint`)와 `EditService.SetAttribute`를 그대로 쓴다.

**Tech Stack:** C# 7.3 / .NET 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4, PowerShell 7 검증 스크립트, `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-14-immortal-tab-design.md`

## Global Constraints

- 브랜치 `feat/immortal-tab`(develop에서). 끝에서 `git merge --no-ff`로 `develop`에 병합하고 `tools/install.ps1`로 설치. `main`에는 병합하지 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 두 구성(개발, `-p:DevTools=false`) 모두 경고·오류 0, `bin\Release`에 DLL 하나.
- 게임 검증은 `session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서. 회차 이름은 `immtab1`부터, 다시 쓰지 않는다. `-End`가 "SAVE RESTORED AND IDENTICAL".
- 서비스는 화면을 모르고 결과를 문장으로 돌려준다. 쓰기 전 `SaveBackup.EnsureOnce()`, 쓴 뒤 다시 읽어 판정, `ChangeLog.Add`.
- 장착·지우기는 하지 않는다(스펙 3절).
- 문서에는 확인한 이름만 쓰고 확인하지 못한 것은 "미확인"과 사유.

## Review Focus

1. 편집 대상이 NPC이고 `immortalCard`가 없거나 비어 있음 → 탭이 예외 없이 "선법 자료가 없습니다"를 보이고 버튼이 꺼진다. (Task 2 `ui`의 NPC 줄)
2. 한 카드 번호가 표에 없음(선인 표에만 있음) → 목록에서 빠지고 예외가 없다. (Task 1 `catalog`의 "every entry has a name")
3. 이미 가진 카드를 다시 익히기 → 건너뜀으로 세고 카드 수가 늘지 않는다. (Task 1 `learn`)
4. 모두 익히기를 거른 목록에 대해 → 거른 것만 익힌다(다른 선인의 카드는 그대로). (Task 1 `learn-all`)
5. 탭이 하나 늘어 기존 검사의 탭 이름이 밀림 → 기존 검사가 그대로 통과. (Task 2 Step 6)

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `src/ModCode/ModMain/Services/ImmortalService.cs` | 새로 | 카드 목록, 거르기, 보유, 익히기, 쓴 선력 |
| `src/ModCode/ModMain/UI/Tabs/ImmortalTab.cs` | 새로 | 첫 화면, 선법 추가 화면 |
| `src/ModCode/ModMain/UI/EditorWindow.cs` | 수정 | 탭 등록(공법 다음), 탭 줄 높이 |
| `src/ModCode/ModMain/Dev/DevCommandsImmortal.cs` | 새로 | `ilist`, `icount`, `ilearn`, `ilearnall`, `ihas` |
| `src/ModCode/ModMain/Dev/DevCommands.cs` | 수정 | 위 명령 연결 |
| `tools/research/immortal-check.ps1` | 새로 | 검사 부분 `catalog`, `learn`, `learn-all`, `restart`, `ui`, `game` |
| `tools/research/ui-check.ps1`, `datebag-check.ps1`, `preset-check.ps1` | 수정 | 탭 이름 `Tab6` 이상을 하나씩 민다, 탭 수 |

---

### Task 1: 선법 서비스와 검사

**Files:**
- Create: `src/ModCode/ModMain/Services/ImmortalService.cs`, `src/ModCode/ModMain/Dev/DevCommandsImmortal.cs`, `tools/research/immortal-check.ps1`
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs`(Execute의 맨 앞)

**Interfaces:**
- Produces:
  - `ImmortalService.Entry { int CardId; int HumanId; string HumanName; bool Active; string Name; int Cost; string Info; string GetText; }`
  - `ImmortalService.All() : List<Entry>` — 선인 표 순서, 선인마다 주동(있으면) 다음 피동. 표에 없는 카드와 두 번째로 나오는 같은 카드는 뺀다.
  - `ImmortalService.Humans() : List<KeyValuePair<int, string>>` — 목록에 카드가 있는 선인(번호, 이름).
  - `ImmortalService.Find(int humanId, int kind, string query) : List<Entry>` — humanId 0은 전체, kind 0 전체·1 주동·2 피동, query는 이름의 일부(공백 무시).
  - `ImmortalService.HasData(WorldUnitBase) : bool`, `Has(WorldUnitBase, int cardId) : bool`, `Owned(WorldUnitBase) : List<Entry>`, `Equipped(WorldUnitBase, int cardId) : bool`, `UsedPoint(WorldUnitBase) : int`, `TotalPoint(WorldUnitBase) : int`
  - `ImmortalService.Learn(WorldUnitBase, Entry) : string`, `LearnAll(WorldUnitBase, List<Entry>) : string`(문장 "선법: 익힘 n, 건너뜀 m(이미 보유).")
  - 개발 명령 `ilist` → `owned=<수> total=<선력 총량> used=<쓴 선력> cards=<카드,…>`, `icount <선인> <종류> [검색어]` → `count=<수> first=<카드 번호>`, `ilearn <카드 번호>` → Learn의 문장, `ilearnall <선인> <종류>` → LearnAll의 문장, `ihas <카드 번호>` → `has=True|False`.

- [ ] **Step 1: 검사를 먼저 쓴다**

`tools/research/immortal-check.ps1`:

```powershell
# 선법 탭의 게임 내 검증(docs/superpowers/specs/2026-10-14-immortal-tab-design.md 6절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

function Fields([string]$text) {
  $body = ($text -replace '^[^>]*-> ', '') -replace ' \| .*$', ''
  $map = @{}
  foreach ($pair in $body -split ' ') { if ($pair -match '^([^=]+)=(.*)$') { $map[$Matches[1]] = $Matches[2] } }
  $map
}
# 설정 표(게임 폴더의 읽기 전용 JSON)에서 센 카드 수: 선인마다 주동(0이 아니면)과 피동, 같은 번호는 한 번, 표에 있는 것만.
function ExpectedCount {
  $dir = 'E:\Games\Tale of Immortal\Mod\modFQA\配置修改教程\配置（只读）Json格式'
  $active = @{}; foreach ($r in (Get-Content -LiteralPath (Join-Path $dir 'ImmortalMethod.json') -Raw | ConvertFrom-Json)) { $active[[string]$r.id] = 1 }
  $passive = @{}; foreach ($r in (Get-Content -LiteralPath (Join-Path $dir 'PassiveMethod.json') -Raw | ConvertFrom-Json)) { $passive[[string]$r.passiveId] = 1 }
  $seen = @{}
  foreach ($h in (Get-Content -LiteralPath (Join-Path $dir 'ImmortalHuman.json') -Raw | ConvertFrom-Json)) {
    if ([string]$h.activeSkills -ne '0' -and $active.ContainsKey([string]$h.activeSkills)) { $seen["a$($h.activeSkills)"] = 1 }
    foreach ($p in ([string]$h.passiveSkills -split '\|')) { if ($p -ne '0' -and $passive.ContainsKey($p)) { $seen["p$p"] = 1 } }
  }
  $seen.Count
}

switch ($Part) {
  'catalog' {
    $all = Fields (Cmd 'icount 0 0' 300)
    Check 'the list has every card of the immortal table' ([int]$all.count -eq (ExpectedCount)) "count=$($all.count) want=$(ExpectedCount)"
    Check 'one immortal (1004) has six cards' ((Fields (Cmd 'icount 1004 0' 300)).count -eq '6') (Cmd 'icount 1004 0' 300)
    Check 'active only' ([int](Fields (Cmd 'icount 0 1' 300)).count -ge 7 -and [int](Fields (Cmd 'icount 0 1' 300)).count -le 10) (Cmd 'icount 0 1' 300)
    Check 'a name search finds the card' ((Fields (Cmd 'icount 0 0 천무절향' 300)).first -eq '5000') (Cmd 'icount 0 0 천무절향' 300)
    Check 'every entry has a name' ((Cmd 'icount 0 0 ?noname' 300) -match 'count=0 ')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'learn' {
    Check 'the fixture has no immortal card' ((Fields (Cmd 'ilist' 200)).owned -eq '0') (Cmd 'ilist' 200)
    $r = Cmd 'ilearn 5000' 800
    Check 'one card is learned' ((Cmd 'ihas 5000' 200) -match 'has=True' -and (Fields (Cmd 'ilist' 200)).owned -eq '1') $r
    $r = Cmd 'ilearn 5000' 800
    Check 'learning it again adds nothing' ((Fields (Cmd 'ilist' 200)).owned -eq '1') $r
    $r = Cmd 'ilearn 10010' 800
    Check 'a passive card is learned' ((Cmd 'ihas 10010' 200) -match 'has=True') $r
    Check 'the change log has the line' ((Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 5 -Encoding UTF8) -match '선법')
    Cmd 'save' 2500 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'restart' {
    # learn(끝에서 저장한다) 뒤에 session.ps1 -Restart를 하고 돌린다.
    Check 'the learned cards are kept after a restart' ((Cmd 'ihas 5000' 200) -match 'has=True' -and (Cmd 'ihas 10010' 200) -match 'has=True') (Cmd 'ilist' 200)
  }
  'learn-all' {
    $before = [int](Fields (Cmd 'ilist' 200)).owned
    $r = Cmd 'ilearnall 1004 0' 2000
    Check 'every card of the filtered list is learned' ([int](Fields (Cmd 'ilist' 200)).owned -eq $before + 6 -and $r -match '6') $r
    Check 'cards of other immortals are not learned' ((Cmd 'ihas 6000' 200) -match 'has=False')
    $r = Cmd 'ilearnall 1004 0' 2000
    Check 'doing it again skips all' ([int](Fields (Cmd 'ilist' 200)).owned -eq $before + 6) $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  default { throw "알 수 없는 Part: $Part" }
}
"FAILS=$fails"
```

`check-lib.ps1`이 `$fails`, `Check`, `Cmd`, `Traps`, `$data`를 준다(다른 검사와 같다). 끝 줄의 `FAILS=`는 `check-lib.ps1`이 이미 찍으면 지운다(Step 2에서 본다).

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin immtab1 -Fixture base
pwsh tools/research/immortal-check.ps1 -Part catalog
pwsh tools/research/immortal-check.ps1 -Part learn
pwsh tools/DevRemote/session.ps1 -End immtab1
```

Expected: 명령이 없어(`UNKNOWN`) 모든 줄이 FAIL. (회차를 열면 `session.ps1`이 빌드하므로 서비스를 쓰기 전에 연다.)

- [ ] **Step 3: 서비스를 쓴다**

`src/ModCode/ModMain/Services/ImmortalService.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 선법(등선경의 카드). 목록은 선인 표(immortalHuman)에서 만들고, 카드의 이름·필요 선력·설명은 주동은 immortalMethod, 피동은 passiveMethod에서 읽는다.
    // 익히기는 게임 함수 ImmortalCard.AddImmortalCard(카드, 선인)다. 장착은 하지 않는다(데이터로 장착하면 선법 창이 예외를 냈다. 스펙 2절).
    internal static class ImmortalService
    {
        public sealed class Entry
        {
            public int CardId;
            public int HumanId;
            public string HumanName = "";
            public bool Active;
            public string Name = "";
            public int Cost;
            public string Info = "";
            public string GetText = "";
            public string Search = "";
        }

        private static List<Entry> all;

        private static string Text(string key)
        {
            return key == null || key == "0" ? "" : MartialService.CleanDesc(GameTool.LS(key)).Replace('\n', ' ');
        }

        public static List<Entry> All()
        {
            if (all != null)
            {
                return all;
            }
            var actives = new Dictionary<int, ConfImmortalMethodItem>();
            var methods = g.conf.immortalMethod._allConfList;
            for (int i = 0; i < methods.Count; i++)
            {
                actives[methods[i].id] = methods[i];
            }
            var passives = new Dictionary<int, ConfPassiveMethodItem>();
            var rows = g.conf.passiveMethod._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                passives[rows[i].passiveId] = rows[i];
            }
            var result = new List<Entry>();
            var seen = new HashSet<string>();
            var humans = g.conf.immortalHuman._allConfList;
            for (int i = 0; i < humans.Count; i++)
            {
                ConfImmortalHumanItem human = humans[i];
                string humanName = Text(human.name);
                int active;
                if (int.TryParse(human.activeSkills, out active) && actives.ContainsKey(active) && seen.Add("a" + active))
                {
                    ConfImmortalMethodItem m = actives[active];
                    result.Add(Make(active, human.id, humanName, true, Text(m.name), m.cardCost, Text(m.info), Text(m.getText)));
                }
                foreach (string part in (human.passiveSkills ?? "").Split('|'))
                {
                    int id;
                    if (int.TryParse(part, out id) && passives.ContainsKey(id) && seen.Add("p" + id))
                    {
                        ConfPassiveMethodItem m = passives[id];
                        result.Add(Make(id, human.id, humanName, false, Text(m.name), m.cardCost, Text(m.info), Text(m.getText)));
                    }
                }
            }
            all = result;
            return all;
        }

        private static Entry Make(int cardId, int humanId, string humanName, bool active, string name, int cost, string info, string getText)
        {
            return new Entry
            {
                CardId = cardId, HumanId = humanId, HumanName = humanName, Active = active, Name = name.Length > 0 ? name : cardId.ToString(),
                Cost = cost, Info = info, GetText = getText, Search = EditService.Compact(name + humanName),
            };
        }

        public static List<KeyValuePair<int, string>> Humans()
        {
            var result = new List<KeyValuePair<int, string>>();
            var seen = new HashSet<int>();
            foreach (Entry e in All())
            {
                if (seen.Add(e.HumanId))
                {
                    result.Add(new KeyValuePair<int, string>(e.HumanId, e.HumanName));
                }
            }
            return result;
        }

        // humanId 0은 전체, kind 0 전체·1 주동·2 피동, query는 이름(또는 선인 이름)의 일부.
        public static List<Entry> Find(int humanId, int kind, string query)
        {
            string needle = EditService.Compact(query ?? "");
            var result = new List<Entry>();
            foreach (Entry e in All())
            {
                if ((humanId == 0 || e.HumanId == humanId) && (kind == 0 || (kind == 1) == e.Active) && (needle.Length == 0 || e.Search.Contains(needle)))
                {
                    result.Add(e);
                }
            }
            return result;
        }

        public static bool HasData(WorldUnitBase unit)
        {
            return unit != null && unit.data.unitData.immortalCard != null;
        }

        public static bool Has(WorldUnitBase unit, int cardId)
        {
            return HasData(unit) && unit.data.unitData.immortalCard.IsHadSkill(cardId);
        }

        public static List<Entry> Owned(WorldUnitBase unit)
        {
            var result = new List<Entry>();
            if (!HasData(unit))
            {
                return result;
            }
            foreach (Entry e in All())
            {
                if (Has(unit, e.CardId))
                {
                    result.Add(e);
                }
            }
            return result;
        }

        public static bool Equipped(WorldUnitBase unit, int cardId)
        {
            var equips = HasData(unit) ? unit.data.unitData.immortalCard.equipCardDatas : null;
            for (int i = 0; equips != null && i < equips.Count; i++)
            {
                if (equips[i].equipCardId == cardId)
                {
                    return true;
                }
            }
            return false;
        }

        // 장착한 카드들의 필요 선력의 합(선법 창 "본원 선력: 쓴 양/총량"의 앞 숫자).
        public static int UsedPoint(WorldUnitBase unit)
        {
            int sum = 0;
            foreach (Entry e in All())
            {
                if (Equipped(unit, e.CardId))
                {
                    sum += e.Cost;
                }
            }
            return sum;
        }

        public static int TotalPoint(WorldUnitBase unit)
        {
            return HasData(unit) ? unit.data.unitData.immortalCard.immortalPoint : 0;
        }

        public static string Learn(WorldUnitBase unit, Entry entry)
        {
            string label = "선법 " + entry.Name;
            if (!HasData(unit))
            {
                return label + ": 이 인물은 선법 자료가 없습니다.";
            }
            if (Has(unit, entry.CardId))
            {
                return label + ": 이미 가지고 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            unit.data.unitData.immortalCard.AddImmortalCard(entry.CardId, entry.HumanId);
            bool ok = Has(unit, entry.CardId);
            ChangeLog.Add(EditService.NameOf(unit), "선법 익히기", "-", entry.HumanName + " " + entry.Name + "(" + entry.CardId + ")", ok);
            return ok ? label + ": 익혔습니다(" + entry.HumanName + ", " + (entry.Active ? "주동" : "피동") + ", 필요 선력 " + entry.Cost + ")."
                : label + ": 게임이 더하지 않았습니다.";
        }

        public static string LearnAll(WorldUnitBase unit, List<Entry> entries)
        {
            if (!HasData(unit))
            {
                return "선법: 이 인물은 선법 자료가 없습니다.";
            }
            int learned = 0, skipped = 0;
            var failed = new List<string>();
            foreach (Entry e in entries)
            {
                if (Has(unit, e.CardId))
                {
                    skipped++;
                    continue;
                }
                string text = Learn(unit, e);
                if (Has(unit, e.CardId))
                {
                    learned++;
                }
                else
                {
                    failed.Add(text);
                }
            }
            return "선법: 익힘 " + learned + ", 건너뜀 " + skipped + "(이미 보유)" + (failed.Count > 0 ? ", 못 익힘 " + failed.Count + "(" + string.Join(" / ", failed.ToArray()) + ")" : "") + ".";
        }
    }
}
```

- [ ] **Step 4: 개발 명령**

`src/ModCode/ModMain/Dev/DevCommandsImmortal.cs`:

```csharp
using System.Text;

namespace MOD_sWP6UL
{
    // 선법 탭의 검증용 명령(tools/research/immortal-check.ps1).
    internal static class DevCommandsImmortal
    {
        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            if (me == null)
            {
                return null;
            }
            switch (p[0])
            {
                case "ilist":
                    {
                        var sb = new StringBuilder();
                        foreach (ImmortalService.Entry e in ImmortalService.Owned(me))
                        {
                            sb.Append(sb.Length == 0 ? "" : ",").Append(e.CardId);
                        }
                        return "owned=" + ImmortalService.Owned(me).Count + " total=" + ImmortalService.TotalPoint(me) + " used=" + ImmortalService.UsedPoint(me) + " cards=" + sb;
                    }
                case "icount":
                    {
                        var list = ImmortalService.Find(int.Parse(p[1]), int.Parse(p[2]), p.Length > 3 ? p[3] : "");
                        return "count=" + list.Count + " first=" + (list.Count == 0 ? 0 : list[0].CardId);
                    }
                case "ilearn":
                    {
                        int id = int.Parse(p[1]);
                        ImmortalService.Entry entry = ImmortalService.All().Find(e => e.CardId == id);
                        return entry == null ? "no card " + id : ImmortalService.Learn(me, entry);
                    }
                case "ilearnall": return ImmortalService.LearnAll(me, ImmortalService.Find(int.Parse(p[1]), int.Parse(p[2]), ""));
                case "ihas": return "has=" + ImmortalService.Has(me, int.Parse(p[1]));
                default: return null;
            }
        }
    }
}
```

`Dev/DevCommands.cs`의 `Execute` 맨 앞(snap 앞)에 넣는다:

```csharp
            string immortal = DevCommandsImmortal.Execute(p);
            if (immortal != null)
            {
                return immortal;
            }
```

- [ ] **Step 5: 빌드하고 검사가 통과하는 것을 본다**

두 구성으로 빌드한다. Expected: 오류 0, 경고 0, DLL 1.

```bash
pwsh tools/DevRemote/session.ps1 -Begin immtab2 -Fixture base
pwsh tools/research/immortal-check.ps1 -Part catalog
pwsh tools/research/immortal-check.ps1 -Part learn
pwsh tools/DevRemote/session.ps1 -Restart
pwsh tools/research/immortal-check.ps1 -Part restart
pwsh tools/research/immortal-check.ps1 -Part learn-all
pwsh tools/DevRemote/session.ps1 -End immtab2
```

Expected: 모든 부분 `FAILS=0`. "one immortal (1004) has six cards"가 실패하면 그 선인의 피동이 `passiveMethod`에 몇 개 있는지 보고 기대값을 표에서 센 값으로 고친다(Ruling으로 남긴다).

- [ ] **Step 6: 커밋**

```bash
git add src/ModCode/ModMain/Services/ImmortalService.cs src/ModCode/ModMain/Dev tools/research/immortal-check.ps1
git commit -m "feat: 선법 서비스(목록, 거르기, 익히기)와 검사

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 선법 탭 화면

**Files:**
- Create: `src/ModCode/ModMain/UI/Tabs/ImmortalTab.cs`
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs`(탭 목록과 `13 * 52`)
- Modify: `tools/research/immortal-check.ps1`(부분 `ui`, `game`), `ui-check.ps1`, `datebag-check.ps1`, `preset-check.ps1`(탭 이름)

**Interfaces:**
- Consumes: Task 1의 `ImmortalService`. `AttributeCatalog.All`의 `immortalPoint`, `EditService.SetAttribute`.
- Produces: 요소 이름 `ImmortalPointText`, `ImmortalPointMinus`, `ImmortalPointPlus`, `ImmortalPointMax`, `ImmortalAddOpen`, `ImmortalNoData`, `ImmortalOwnedList`, `ImmortalHuman<번호>`(0 전체), `ImmortalKind<0|1|2>`, `ImmortalSearch`, `ImmortalLearnAll`, `ImmortalLearn<카드>`, `ImmortalState<카드>`. 탭은 `Tab6`.

- [ ] **Step 1: 검사를 먼저 쓴다**

`immortal-check.ps1`의 `default` 위에 넣는다.

```powershell
  'ui' {
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab6' | Out-Null
    Check 'the immortal tab is the seventh' (IsAt 'ImmortalAddOpen')
    Check 'the immortal power is shown as used/total' ((Cmd 'uitext ImmortalPointText' 150) -match "ImmortalPointText text='[^']*0 / 0")
    Press 'ImmortalPointPlus' | Out-Null
    Check 'plus one raises the total' ((Fields (Cmd 'ilist' 200)).total -eq '1') (Cmd 'ilist' 200)
    Press 'ImmortalPointMax' | Out-Null
    Check 'max sets the game limit' ((Fields (Cmd 'ilist' 200)).total -eq '40') (Cmd 'ilist' 200)
    Press 'ImmortalPointMinus' | Out-Null
    Check 'minus one lowers it' ((Fields (Cmd 'ilist' 200)).total -eq '39') (Cmd 'ilist' 200)
    Press 'ImmortalAddOpen' | Out-Null
    Check 'the add page has the filters' ((IsAt 'ImmortalHuman0') -and (IsAt 'ImmortalHuman1004') -and (IsAt 'ImmortalKind2') -and (IsAt 'ImmortalLearnAll'))
    Press 'ImmortalHuman1004' | Out-Null
    Check 'filtering by an immortal shows its cards' ((IsAt 'ImmortalLearn5000') -and -not (IsAt 'ImmortalLearn6000'))
    Press 'ImmortalLearn5000' 1500 | Out-Null
    Check 'learn marks the card as owned' ((Cmd 'ihas 5000' 200) -match 'has=True' -and (Cmd 'uitext ImmortalState5000' 150) -match '보유')
    Press 'ImmortalLearnAll' | Out-Null
    Check 'learn all asks first' ((Fields (Cmd 'ilist' 200)).owned -eq '1')
    Press 'ImmortalLearnAll' 2500 | Out-Null
    Check 'the second press learns the shown cards' ((Fields (Cmd 'ilist' 200)).owned -eq '6') (Cmd 'ilist' 200)
    Press 'Back' | Out-Null
    Check 'the owned list shows the cards' (IsAt 'ImmortalOwnedList')
    Shot 'immortal-tab'
    # NPC: 선법 자료가 없거나 비어 있어도 예외 없이 그려진다.
    Press 'Tab9' | Out-Null; Press 'NpcRefresh' | Out-Null; Press (Pos 'Pick*').name | Out-Null; Press 'EditNpc' | Out-Null
    Press 'Tab6' | Out-Null
    Check 'an NPC target draws without an exception' ((IsAt 'ImmortalAddOpen') -or (IsAt 'ImmortalNoData'))
    Press 'ToPlayer' | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'game' {
    # 등선 후기로 올린 뒤 익힌 카드가 게임의 선법 창에 나오는지(캡처로 본다).
    Cmd 'pgraise 44' 4000 | Out-Null
    for ($n = 0; $n -lt 5 -and (Cmd 'uilist' 200) -match 'DramaDialogue|GetImmortalSkill'; $n++) { Cmd 'uiclose DramaDialogue' 500 | Out-Null; Cmd 'uiclose GetImmortalSkill' 500 | Out-Null }
    Cmd 'ilearnall 1004 0' 2000 | Out-Null
    Cmd 'uiopen ImmortalSkill' 2000 | Out-Null
    Shot 'immortal-game'
    Check 'the game window opened' ((Cmd 'uilist' 200) -match 'ImmortalSkill')
    Cmd 'uiclose ImmortalSkill' 800 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
```

`ToPlayer`(제목 줄의 플레이어로), `EditNpc`, `NpcRefresh`, `Pick*`는 기존 NPC 탭의 요소 이름이다(`ui-check.ps1`이 쓴다). NPC 탭은 탭이 하나 밀려 `Tab9`가 된다.

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin immtab3 -Fixture base
pwsh tools/research/immortal-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End immtab3
```

Expected: "the immortal tab is the seventh"부터 FAIL(지금 `Tab6`은 운명이다).

- [ ] **Step 3: 탭을 쓴다**

`src/ModCode/ModMain/UI/Tabs/ImmortalTab.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 선법 탭: 본원 선력과 가진 선법. 하위 화면: 선법 추가(선인·종류·이름으로 걸러 익히기). 장착은 게임의 선법 창에서 한다.
    internal sealed class ImmortalTab : EditorTab
    {
        private readonly ListView<ImmortalService.Entry> ownedList = new ListView<ImmortalService.Entry>("ImmortalOwnedList", 38);
        private readonly ListView<ImmortalService.Entry> addList = new ListView<ImmortalService.Entry>("ImmortalAddList", 50);
        private int human;      // 0은 전체
        private int kind;       // 0 전체, 1 주동, 2 피동
        private string query = "";

        public override string Title
        {
            get { return "선법"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public ImmortalTab()
        {
            ownedList.EmptyText = "가진 선법이 없습니다.";
            addList.EmptyText = "조건에 맞는 선법이 없습니다.";
        }

        public override void OnTargetChanged()
        {
            ownedList.Reset();
            addList.Reset();
        }

        private static AttributeDef PointDef
        {
            get { return AttributeCatalog.All.Find(d => d.Key == "immortalPoint"); }
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            if (!ImmortalService.HasData(target))
            {
                Ui.Label(content, "이 인물은 선법 자료가 없습니다.", 0, 0, 800, 36).gameObject.name = "ImmortalNoData";
                return;
            }
            int total = ImmortalService.TotalPoint(target);
            Ui.Label(content, "본원 선력: " + ImmortalService.UsedPoint(target) + " / " + total, 0, 0, 260, 36, 20).gameObject.name = "ImmortalPointText";
            int max = PointDef.Max(g.conf.roleAttributeLimit);
            Ui.Button("ImmortalPointMinus", content, "- 1", 270, 0, 80, 34, new Action(() => Say(EditService.SetAttribute(Target, PointDef, ImmortalService.TotalPoint(Target) - 1))), total > 0);
            Ui.Button("ImmortalPointPlus", content, "+ 1", 356, 0, 80, 34, new Action(() => Say(EditService.SetAttribute(Target, PointDef, ImmortalService.TotalPoint(Target) + 1))), total < max);
            Ui.Button("ImmortalPointMax", content, "최대(" + max + ")", 442, 0, 120, 34, new Action(() => Say(EditService.SetAttribute(Target, PointDef, max))), total < max);
            Ui.Wrapped(content, "선법 창과 장착은 등선경부터입니다(장착 칸: 초기 2, 중기 4, 후기 6). 장착은 게임의 선법 창에서 합니다.", 580, 0, ContentW - 760, 40, 15).color = Ui.Dim;
            Ui.Button("ImmortalAddOpen", content, "선법 추가", ContentW - 160, 0, 160, 34, new Action(() =>
            {
                addList.Reset();
                Push(() => "선법 추가 — " + EditService.NameOf(Target), DrawAdd);
            }));

            List<ImmortalService.Entry> owned = ImmortalService.Owned(target);
            const float head = 48;
            Ui.Label(content, "가진 선법 " + owned.Count + "개", 0, head, 300, 30, 18).color = Ui.Dim;
            Ui.Panel("ImmortalHead", content, 0, head + 32, ContentW, 30, Ui.Section);
            Ui.Label(content, "선인", 8, head + 32, 180, 30, 16).color = Ui.Dim;
            Ui.Label(content, "이름", 200, head + 32, 300, 30, 16).color = Ui.Dim;
            Ui.Label(content, "종류", 510, head + 32, 80, 30, 16).color = Ui.Dim;
            Ui.Label(content, "필요 선력", 600, head + 32, 120, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", 730, head + 32, 120, 30, 16).color = Ui.Dim;
            float top = head + 64;
            ownedList.Build(content, 0, top, ContentW, ContentH - top, owned, (row, e, index) =>
            {
                Ui.Label(row, e.HumanName, 8, 0, 180, 38);
                Ui.Label(row, e.Name, 200, 0, 300, 38);
                Ui.Label(row, e.Active ? "주동" : "피동", 510, 0, 80, 38);
                Ui.Label(row, e.Cost.ToString(), 600, 0, 120, 38);
                Ui.Label(row, ImmortalService.Equipped(Target, e.CardId) ? "장착 중" : "", 730, 0, 120, 38, 17).color = Ui.Warn;
            });
        }

        private void DrawAdd()
        {
            float y = SubTop;
            // 선인 버튼: 넘치면 다음 줄로.
            var humans = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, "전체") };
            humans.AddRange(ImmortalService.Humans());
            float x = 0;
            foreach (var pair in humans)
            {
                int id = pair.Key;
                float w = Math.Max(70, (float)Math.Ceiling(Ui.TextWidth(pair.Value, 16)) + 24);
                if (x + w > ContentW)
                {
                    x = 0;
                    y += 40;
                }
                Ui.Button("ImmortalHuman" + id, content, pair.Value, x, y, w, 36, new Action(() => { human = id; addList.Reset(); Redraw(); }), true, human == id);
                x += w + 6;
            }
            y += 46;
            string[] kinds = { "전체", "주동", "피동" };
            for (int i = 0; i < kinds.Length; i++)
            {
                int value = i;
                Ui.Button("ImmortalKind" + i, content, kinds[i], i * 96, y, 90, 36, new Action(() => { kind = value; addList.Reset(); Redraw(); }), true, kind == i);
            }
            SearchBox.Build(content, "ImmortalSearch", 300, y, 330, query, "이름 또는 선인 이름의 일부", new Action<string>(v =>
            {
                query = v;
                addList.SetItems(ImmortalService.Find(human, kind, query));
            }));
            Confirm.Button("ImmortalLearnAll", content, "보이는 선법 모두 익히기", "정말 모두", ContentW - 260, y, 260, 36, "immortal-all",
                "보이는 선법을 모두 익힙니다(이미 가진 것은 건너뜁니다). \"정말 모두\"를 한 번 더 누르세요.",
                new Action(() => Say(ImmortalService.LearnAll(Target, ImmortalService.Find(human, kind, query)))));
            float top = y + 46;
            float descW = ContentW - Ui.ScrollBarWidth - 220;
            addList.HeightOf = e => RowWithSecondLine(50, Ui.TextHeight(Second(e), descW, 15));
            addList.Build(content, 0, top, ContentW, ContentH - top, ImmortalService.Find(human, kind, query), DrawAddRow);
        }

        private static string Second(ImmortalService.Entry e)
        {
            return e.Info + (e.GetText.Length > 0 ? " / 획득: " + e.GetText : "");
        }

        private void DrawAddRow(RectTransform row, ImmortalService.Entry e, int index)
        {
            bool has = ImmortalService.Has(Target, e.CardId);
            float descW = ContentW - Ui.ScrollBarWidth - 220;
            Ui.Label(row, e.Name + "  [" + e.CardId + "]  " + e.HumanName + " · " + (e.Active ? "주동" : "피동") + " · 필요 선력 " + e.Cost, 8, 0, descW, 26, 18);
            Text desc = Ui.Wrapped(row, Second(e), 24, SecondLineY, descW - 16, row.sizeDelta.y - SecondLineY - 4, 15, TextAnchor.UpperLeft);
            desc.color = Ui.Dim;
            float bx = ContentW - Ui.ScrollBarWidth - 200;
            Text state = Ui.Label(row, has ? "보유" : "미보유", bx, 7, 80, 36, 16);
            state.color = has ? Ui.Warn : Ui.Dim;
            state.gameObject.name = "ImmortalState" + e.CardId;
            Ui.Button("ImmortalLearn" + e.CardId, row, "익히기", bx + 86, 7, 110, 36, new Action(() => Say(ImmortalService.Learn(Target, e))), !has);
        }
    }
}
```

`Ui.TextWidth(string, int)`가 없으면 `Ui.TextHeight`처럼 `Ui.cs`에 더한다(같은 방식으로 `TextGenerator`의 `GetPreferredWidth`). 있는지 먼저 `grep -n "TextWidth" src/ModCode/ModMain/UI/Ui.cs`로 본다.

`EditorWindow.cs`: 탭 목록에서 `new MartialTab(),` 뒤에 `new ImmortalTab(),`를 넣고, `13 * 52`를 `14 * 52`로.

- [ ] **Step 4: 기존 검사의 탭 이름을 민다**

탭이 공법(`Tab5`) 뒤에 들어가므로 `Tab6`~`Tab12`가 `Tab7`~`Tab13`이 된다. 세 스크립트에서 `'Tab<N>'`(N ≥ 6)을 큰 번호부터 하나씩 바꾼다(큰 번호부터 해야 겹쳐 바뀌지 않는다). `ui-check.ps1`의 탭 수 검사(`tabs` 부분)가 13을 기대하면 14로. 바꾼 뒤 `grep -n "Tab1[0-3]\|Tab[6-9]" tools/research/*.ps1`로 확인한다.

```powershell
foreach ($f in 'ui-check.ps1', 'datebag-check.ps1', 'preset-check.ps1') {
  $p = Join-Path 'E:\TlToybox\tools\research' $f
  $t = [IO.File]::ReadAllText($p)
  foreach ($n in 12..6) { $t = $t.Replace("'Tab$n'", "'Tab$($n + 1)'").Replace("""Tab$n""", """Tab$($n + 1)""") }
  [IO.File]::WriteAllText($p, $t, (New-Object Text.UTF8Encoding $false))
}
```

`immortal-check.ps1`의 `Tab6`(선법)과 `Tab9`(NPC)는 이미 새 번호다. 이 단계의 바꾸기는 그 파일에 하지 않는다. `ui-check.ps1`의 `$tabProbe`(탭마다 보이는 요소)에 `'SkillAddOpen'` 다음으로 `'ImmortalAddOpen'`을 넣고, `preset-check.ps1`의 "the safety tab is the thirteenth"는 "fourteenth"로 고친다.

- [ ] **Step 5: 빌드하고 검사가 통과하는 것을 본다**

두 구성으로 빌드한다. Expected: 오류 0, 경고 0, DLL 1.

```bash
pwsh tools/DevRemote/session.ps1 -Begin immtab4 -Fixture base
pwsh tools/research/immortal-check.ps1 -Part ui
pwsh tools/research/immortal-check.ps1 -Part game
pwsh tools/DevRemote/session.ps1 -End immtab4
```

Expected: `FAILS=0`. 캡처 `check-immortal-tab.png`(선력 줄, 가진 선법 6줄), `check-immortal-game.png`(게임 선법 창의 동방명설 6/6)를 열어 본다.

- [ ] **Step 6: 기존 검사**

```bash
pwsh tools/DevRemote/session.ps1 -Begin immtab5 -Fixture base
pwsh tools/research/ui-check.ps1 -Part base
pwsh tools/research/ui-check.ps1 -Part tabs
pwsh tools/research/ui-check.ps1 -Part confirm
pwsh tools/research/ui-check.ps1 -Part wrap
pwsh tools/research/ui-check.ps1 -Part fateguard
pwsh tools/DevRemote/session.ps1 -End immtab5
pwsh tools/DevRemote/session.ps1 -Begin immtab6 -Fixture base
pwsh tools/research/ui-check.ps1 -Part rates
pwsh tools/research/ui-check.ps1 -Part vault
pwsh tools/research/ui-check.ps1 -Part charvault
pwsh tools/DevRemote/session.ps1 -End immtab6
pwsh tools/DevRemote/session.ps1 -Begin immtab7 -Fixture base
pwsh tools/research/preset-check.ps1 -Part files
pwsh tools/research/preset-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End immtab7
```

Expected: 모든 부분 `FAILS=0`. 탭 이름 때문에 실패하면 Step 4를 다시 본다.

- [ ] **Step 7: 커밋**

```bash
git add src/ModCode/ModMain/UI tools/research
git commit -m "feat: 선법 탭 — 본원 선력, 가진 선법, 선법 추가(선인·종류·이름, 모두 익히기)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 검토, 문서, 병합, 설치

- [ ] **Step 1: 검토자 한 명이 브랜치 전체를 본다**(가장 능력 있는 모델, 읽기 전용, 범위 고정 문구). Critical·Important는 재현 검사를 먼저 넣고 고친다.

- [ ] **Step 2: 배포 구성으로 띄운다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin immtab8 -Fixture base -ReleaseBuild
pwsh -Command "& tools/DevRemote/win.ps1 -Do key -Vk 0x2D"
pwsh -Command "& tools/DevRemote/rc.ps1 -Cmd 'click 151 458' -NoShot"
pwsh -Command "& tools/DevRemote/win.ps1 -Do shot -Out .devremote/shot-immortal-release.png -Scale 0.6"
pwsh tools/DevRemote/session.ps1 -End immtab8
```

Expected: `hit`이 `Tab6`, 캡처에 선법 탭, `Latest.log`에 "trampoline" 없음(146 + 6 × 52 = 458).

- [ ] **Step 3: 문서**

- `docs/05-editor-v0.1.md`에 `## 36. 선법 탭 (2026-10-14 추가)`: 쓰는 법, 규칙, 실측(스펙 2절), 구조, 검증 표, 확인하지 못한 것.
- `docs/06-handoff.md`: 탭 순서(14개, `Tab0`~`Tab13`, 선법 `Tab6`, 프리셋 `Tab12`, 안전 `Tab13`), 기능 지도에 선법 줄, 3절에 서비스·검사 스크립트, 5절에 `immortal-check.ps1`의 부분과 개발 명령, 8.1절에 묶음, 회차 이름.

- [ ] **Step 4: 커밋, 병합, 설치**

```bash
git add docs
git commit -m "docs: 선법 탭(05 문서 36절), 인계 문서

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git checkout develop
git merge --no-ff feat/immortal-tab -m "Merge branch 'feat/immortal-tab' into develop"
pwsh tools/install.ps1
```

Expected: 병합 커밋, 설치 출력에 `MOD_sWP6UL.dll`. 게임이 켜져 있으면 끄도록 알리고 기다린다.
