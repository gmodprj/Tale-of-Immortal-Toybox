using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MOD_sWP6UL
{
    // 인물 데이터를 통째로 파일에 뜬다(.devremote\snap-<이름>.txt). 두 시점의 파일을 비교해 게임의 행동이 무엇을 바꾸는지 찾는다.
    // 경지 올리기가 정식 돌파와 다르게 놓친 것을 찾을 때 썼다(docs/05 35절).
    internal static class DevCommandsSnap
    {
        private const int ListLimit = 80;

        public static string Handle(WorldUnitBase me, string[] p)
        {
            switch (p[0])
            {
                case "snap":
                    {
                        var sb = new StringBuilder();
                        var seen = new HashSet<long>();
                        int depth = p.Length > 2 ? int.Parse(p[2]) : 6;
                        Walk(me.data.unitData, "unitData", depth, sb, seen);
                        Walk(me.data.dynUnitData, "dyn", 3, sb, seen);
                        foreach (var pair in FateService.Records(me))
                        {
                            sb.Append("fateLog[").Append(pair.Key).Append("]=").Append(pair.Value).Append('\n');
                        }
                        string path = System.IO.Path.Combine(@"E:\TlToybox\.devremote", "snap-" + p[1] + ".txt");
                        System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                        return "snap=" + p[1] + " chars=" + sb.Length;
                    }
                case "imm":
                    {
                        var card = me.data.unitData.immortalCard;
                        return card == null ? "immortalCard=null" : "immortalPoint=" + card.immortalPoint
                            + " cards=" + (card.cardDatas == null ? -1 : card.cardDatas.Count) + " equips=" + (card.equipCardDatas == null ? -1 : card.equipCardDatas.Count);
                    }
                case "upcomp":
                    UnitActionRoleUpGrade.ActionUpGradeComplete(me, g.conf.roleGrade.GetItem(int.Parse(p[1])), p.Length > 2 && p[2] == "1");
                    return "completed " + p[1];
                case "hpstate":
                    {
                        var s = me.data.unitData.propertyData;
                        var d = me.data.dynUnitData;
                        return "hp=" + s.hp + "/" + d.hpMax.value + " mp=" + s.mp + "/" + d.mpMax.value + " sp=" + s.sp + "/" + d.spMax.value;
                    }
                case "immset":
                    me.data.unitData.immortalCard.immortalPoint = int.Parse(p[1]);
                    return "immortalPoint=" + me.data.unitData.immortalCard.immortalPoint;
                case "immequip":
                    {
                        bool ok = me.data.unitData.immortalCard.EquipCard(int.Parse(p[1]), p[2] == "1", int.Parse(p[3]));
                        return "equip=" + ok + " immortalPoint=" + me.data.unitData.immortalCard.immortalPoint + " equips=" + me.data.unitData.immortalCard.equipCardDatas.Count;
                    }
                case "utask":
                    {
                        var sb = new StringBuilder();
                        var all = me.data.unitData.allTask;
                        for (int i = 0; all != null && i < all.Count; i++)
                        {
                            sb.Append(" [").Append(all[i].id).Append(" state=").Append(all[i].state).Append(" dur=").Append(all[i].duration).Append(" cur=").Append(all[i].curCount).Append(']');
                        }
                        var titles = me.data.unitData.appellationTitle;
                        sb.Append(" titles=");
                        for (int i = 0; titles != null && titles.appellationItems != null && i < titles.appellationItems.Count; i++)
                        {
                            sb.Append(titles.appellationItems[i].id).Append(',');
                        }
                        return "tasks=" + (all == null ? -1 : all.Count) + sb;
                    }
                case "upreal":
                    {
                        // 다음 경지로 게임의 정식 돌파 행동을 부른다. 대경지면 품질이 가장 높은 줄을 고르고 재료와 천재지보를 지급해 넣는다. 수련치는 채운다.
                        var saved = me.data.unitData.propertyData;
                        ConfRoleGradeItem row = g.conf.roleGrade.GetNextGradeItem(saved.gradeID);
                        if (row == null)
                        {
                            return "no next grade";
                        }
                        var majors = EditService.MajorBreakthroughRows(me);
                        if (majors.Count > 0)
                        {
                            row = majors[majors.Count - 1];
                        }
                        EditService.GiveBreakthroughMaterials(me, row);
                        var used = new List<string>();
                        var require = Soles(me, EditService.BreakthroughNeedItems(row), used);
                        var rarity = Soles(me, EditService.BreakthroughTreasures(row), used);
                        if (saved.exp < row.exp)
                        {
                            saved.AddExp(row.exp - saved.exp);
                        }
                        var action = new UnitActionRoleUpGrade(row.id, new UnhollowerBaseLib.Il2CppStringArray(require.ToArray()), new UnhollowerBaseLib.Il2CppStringArray(rarity.ToArray()));
                        action.upgradePhaseForceSuccess = true;
                        int check = me.IsCreateAction(action, false);
                        int code = me.CreateAction(action, false);
                        return "row=" + row.id + " require=" + require.Count + " rarity=" + rarity.Count + " IsCreateAction=" + check + " CreateAction=" + code
                            + " state=" + action.state + " grade=" + saved.gradeID + " immortalPoint=" + me.data.unitData.immortalCard.immortalPoint;
                    }
                default:
                    return null;
            }
        }

        private static List<string> Soles(WorldUnitBase me, List<int> ids, List<string> used)
        {
            var result = new List<string>();
            foreach (int id in ids)
            {
                var owned = me.data.unitData.propData.GetProps(id);
                for (int i = 0; owned != null && i < owned.Count; i++)
                {
                    if (!used.Contains(owned[i].soleID))
                    {
                        used.Add(owned[i].soleID);
                        result.Add(owned[i].soleID);
                        break;
                    }
                }
            }
            return result;
        }

        private static readonly HashSet<string> Skip = new HashSet<string> { "Pointer", "ObjectClass", "WasCollected", "Il2CppType", "isWasCollected" };

        private static void Walk(object o, string path, int depth, StringBuilder sb, HashSet<long> seen)
        {
            if (o == null)
            {
                sb.Append(path).Append("=null\n");
                return;
            }
            Type t = o.GetType();
            if (t.IsPrimitive || t.IsEnum || o is string || o is decimal)
            {
                sb.Append(path).Append('=').Append(o).Append('\n');
                return;
            }
            var il = o as UnhollowerBaseLib.Il2CppObjectBase;
            if (il != null && !seen.Add(il.Pointer.ToInt64()))
            {
                sb.Append(path).Append("=<seen ").Append(t.Name).Append(">\n");
                return;
            }
            if (depth <= 0)
            {
                sb.Append(path).Append("=<").Append(t.Name).Append(">\n");
                return;
            }
            PropertyInfo count = t.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            if (count == null)
            {
                count = t.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance);
            }
            if (count != null && count.GetIndexParameters().Length == 0 && count.PropertyType == typeof(int))
            {
                int n;
                try
                {
                    n = (int)count.GetValue(o, null);
                }
                catch (Exception)
                {
                    return;
                }
                sb.Append(path).Append(".count=").Append(n).Append('\n');
                PropertyInfo item = null;
                foreach (PropertyInfo each in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    ParameterInfo[] index = each.GetIndexParameters();
                    if (each.Name == "Item" && index.Length == 1 && index[0].ParameterType == typeof(int))
                    {
                        item = each;
                    }
                }
                for (int i = 0; item != null && i < n && i < ListLimit; i++)
                {
                    object v;
                    try
                    {
                        v = item.GetValue(o, new object[] { i });
                    }
                    catch (Exception)
                    {
                        break;
                    }
                    Walk(v, path + "[" + i + "]", depth - 1, sb, seen);
                }
                return;
            }
            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0 || Skip.Contains(prop.Name) || !prop.CanRead)
                {
                    continue;
                }
                object v;
                try
                {
                    v = prop.GetValue(o, null);
                }
                catch (Exception)
                {
                    continue;
                }
                Walk(v, path + "." + prop.Name, depth - 1, sb, seen);
            }
        }
    }
}
