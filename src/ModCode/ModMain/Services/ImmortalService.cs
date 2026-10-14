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
