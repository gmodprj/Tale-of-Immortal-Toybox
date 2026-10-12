using System;
using System.Text;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 개발 전용. docs/04-ingame-state-analysis.md 15절 3단계(연쇄가 있는 쓰기)의 검증 명령.
    internal static class DevCommandsStage3
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "npcs": return Npcs(int.Parse(p[1]));
                case "findprefix": return FindPrefix();
                case "intim": return Intim(p[1]);
                case "setintim":
                    Me().data.unitData.relationData.SetIntim(p[1], float.Parse(p[2]));
                    return "SetIntim called; " + Intim(p[1]);
                case "setintim2":
                    Unit(p[1]).data.unitData.relationData.SetIntim(Me().data.unitData.unitID, float.Parse(p[2]));
                    return "npc-side SetIntim called; " + Intim(p[1]);
                case "updaterel":
                    Me().data.UpdateAllUnitRelation();
                    Unit(p[1]).data.UpdateAllUnitRelation();
                    return "UpdateAllUnitRelation called on both; " + Intim(p[1]);
                case "stats": return Stats(Unit(p.Length > 1 ? p[1] : "me"));
                case "addexp":
                    int ret = Me().data.unitData.propertyData.AddExp(int.Parse(p[1]));
                    return "AddExp returned " + ret + "; " + Stats(Me());
                case "setgrade":
                    Me().data.unitData.propertyData.SetGradeID(int.Parse(p[1]));
                    return "SetGradeID called; " + Stats(Me());
                case "upgrade":
                    int code = Me().CreateAction(new UnitActionRoleUpGrade(int.Parse(p[1]), new Il2CppStringArray(0), new Il2CppStringArray(0)), false);
                    return "UnitActionRoleUpGrade returned " + code + "; " + Stats(Me());
                case "martial": return Martial(Unit(p.Length > 1 ? p[1] : "me"));
                case "mexp":
                    Unit(p[1]).data.unitData.AddMartialExp(p[2], int.Parse(p[3]));
                    return "AddMartialExp called; " + Martial(Unit(p[1]));
                case "plevel":
                    MartialOf(p[1], p[2]).SetPrefixLevel(int.Parse(p[3]), int.Parse(p[4]));
                    return "SetPrefixLevel called; " + Martial(Unit(p[1]));
                case "pid":
                    MartialOf(p[1], p[2]).SetPrefixID(int.Parse(p[3]), int.Parse(p[4]));
                    return "SetPrefixID called; " + Martial(Unit(p[1]));
                case "luck": return Luck(Unit(p.Length > 1 ? p[1] : "me"));
                case "luckadd":
                    int addCode = Me().CreateAction(new UnitActionLuckAdd(int.Parse(p[1])), false);
                    return "UnitActionLuckAdd returned " + addCode + "; " + Luck(Me()) + " | " + Stats(Me());
                case "luckdel":
                    int delCode = Me().CreateAction(new UnitActionLuckDel(int.Parse(p[1])), false);
                    return "UnitActionLuckDel returned " + delCode + "; " + Luck(Me()) + " | " + Stats(Me());
                default:
                    return null;
            }
        }

        private static WorldUnitBase Me()
        {
            return g.world.playerUnit;
        }

        private static WorldUnitBase Unit(string id)
        {
            if (id == "me")
            {
                return Me();
            }
            WorldUnitBase unit = g.world.unit.GetUnit(id, true);
            if (unit == null)
            {
                throw new ArgumentException("no unit " + id);
            }
            return unit;
        }

        private static DataProps.MartialData MartialOf(string unitId, string soleId)
        {
            return Unit(unitId).data.unitData.GetActionMartial(soleId).data.To<DataProps.MartialData>();
        }

        private static string Npcs(int max)
        {
            var sb = new StringBuilder();
            var units = g.world.unit.GetUnits(false);
            string myId = Me().data.unitData.unitID;
            int shown = 0;
            for (int i = 0; i < units.Count && shown < max; i++)
            {
                var data = units[i].data.unitData;
                if (data.unitID == myId)
                {
                    continue;
                }
                shown++;
                sb.Append("\n  ").Append(data.unitID).Append(" grade=").Append(data.propertyData.gradeID)
                    .Append(" martial=").Append(data.allActionMartial.Count)
                    .Append(" point=").Append(data.pointX).Append(',').Append(data.pointY)
                    .Append(" name=").Append(data.propertyData.GetName());
            }
            return "total=" + units.Count + sb;
        }

        private static string FindPrefix()
        {
            var units = g.world.unit.GetUnits(false);
            for (int i = 0; i < units.Count; i++)
            {
                var data = units[i].data.unitData;
                foreach (var pair in data.allActionMartial)
                {
                    var martial = pair.Value.data.To<DataProps.MartialData>();
                    if (martial.GetPrefixs().Count >= 2)
                    {
                        return "unit=" + data.unitID + " soleID=" + pair.Key + " prefixes=" + martial.GetPrefixs().Count
                            + " grade=" + data.propertyData.gradeID;
                    }
                }
            }
            return "none";
        }

        private static string Intim(string id)
        {
            WorldUnitBase me = Me();
            WorldUnitBase other = Unit(id);
            string myId = me.data.unitData.unitID;
            var mine = me.data.unitData.relationData;
            var theirs = other.data.unitData.relationData;
            return "me->npc=" + mine.GetIntim(id) + " npc->me=" + theirs.GetIntim(myId)
                + " npc.intimToPlayerUnit=" + theirs.intimToPlayerUnit
                + " me.isFriend=" + mine.IsFriend(other) + " npc.isFriend=" + theirs.IsFriend(me)
                + " bothType(me)=" + me.data.GetRelationType(other) + " bothType(npc)=" + other.data.GetRelationType(me);
        }

        private static string Stats(WorldUnitBase unit)
        {
            var saved = unit.data.unitData.propertyData;
            var dyn = unit.data.dynUnitData;
            ConfRoleGradeItem grade = g.conf.roleGrade.GetItem(saved.gradeID);
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(saved.gradeID);
            return "gradeID=" + saved.gradeID + "(grade=" + grade.grade + " phase=" + grade.phase + " quality=" + grade.quality + ")"
                + " exp=" + saved.exp + " next=" + (next == null ? "none" : next.id + "@exp" + next.exp)
                + " life=" + saved.life + " abilityPoint=" + saved.abilityPoint
                + " attack=" + saved.attack + "/" + dyn.attack.value
                + " defense=" + saved.defense + "/" + dyn.defense.value
                + " hpMax=" + saved.hpMax + "/" + dyn.hpMax.value
                + " mpMax=" + saved.mpMax + "/" + dyn.mpMax.value
                + " luck=" + saved.luck + "/" + dyn.luck.value
                + " isUpGrade=" + g.conf.roleGrade.IsUpGrade(unit);
        }

        private static string Martial(WorldUnitBase unit)
        {
            var sb = new StringBuilder("count=" + unit.data.unitData.allActionMartial.Count);
            foreach (var pair in unit.data.unitData.allActionMartial)
            {
                var action = pair.Value;
                var martial = action.data.To<DataProps.MartialData>();
                sb.Append("\n  ").Append(pair.Key).Append(" type=").Append(martial.martialType)
                    .Append(" baseID=").Append(martial.baseID).Append(" grade=").Append(martial.grade)
                    .Append(" exp=").Append(action.exp).Append(" prefixes=[");
                var prefixes = martial.GetPrefixs();
                for (int i = 0; i < prefixes.Count; i++)
                {
                    var prefix = prefixes[i];
                    sb.Append(i == 0 ? "" : " ").Append(prefix.index).Append(":id")
                        .Append(prefix.prefixValueItem == null ? "null" : prefix.prefixValueItem.id.ToString())
                        .Append(":num").Append(prefix.prefixValueItem == null ? "null" : prefix.prefixValueItem.number.ToString())
                        .Append(":lv").Append(prefix.prefixLevel);
                }
                sb.Append("] values=").Append(Join(action.data.values));
            }
            return sb.ToString();
        }

        private static string Luck(WorldUnitBase unit)
        {
            var saved = unit.data.unitData.propertyData;
            var sb = new StringBuilder("born=[");
            for (int i = 0; i < saved.bornLuck.Length; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(saved.bornLuck[i].id);
            }
            sb.Append("] add=[");
            for (int i = 0; i < saved.addLuck.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(saved.addLuck[i].id).Append("(dur").Append(saved.addLuck[i].duration).Append(')');
            }
            sb.Append("] runtime=").Append(unit.allLuck.Count);
            return sb.ToString();
        }

        private static string Join(Il2CppStructArray<int> values)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length && i < 24; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(values[i]);
            }
            return sb.ToString();
        }
    }
}
