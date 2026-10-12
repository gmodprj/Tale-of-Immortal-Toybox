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
                case "mdetail": return MartialDetail(p[1], p[2]);
                case "openui":
                    if (p[1] == "UpGrade") { g.ui.OpenUI(UIType.UpGrade); return "opened UpGrade"; }
                    if (p[1] == "FateFeature") { g.ui.OpenUI(UIType.FateFeature); return "opened FateFeature"; }
                    return "unknown ui";
                case "upclick":
                    {
                        // 게임의 돌파 화면이 열려 있고 카드와 재료가 선택된 상태에서, 화면의 돌파 처리 메서드를 직접 부른다.
                        UIUpGrade ui = g.ui.GetUI<UIUpGrade>(UIType.UpGrade);
                        if (ui == null)
                        {
                            return "UpGrade UI is not open";
                        }
                        UIUpGradeGradeList list = ui.uiUpGrade;
                        UnityEngine.Transform rootT = list.goGradeRoot.transform;
                        UnityEngine.GameObject card = null;
                        for (int i = 0; i < rootT.childCount; i++)
                        {
                            var toggle = rootT.GetChild(i).gameObject.GetComponentInChildren<UnityEngine.UI.Toggle>();
                            if (toggle != null && toggle.isOn)
                            {
                                card = rootT.GetChild(i).gameObject;
                            }
                        }
                        string info = "cards=" + rootT.childCount + " selectedCard=" + (card == null ? "none" : card.name)
                            + " require=" + (list.selectRequirePropsSoleID == null ? -1 : list.selectRequirePropsSoleID.Length)
                            + " cur=" + (list.curGradeItem == null ? 0 : list.curGradeItem.id);
                        if (card == null)
                        {
                            return info + " (no card selected)";
                        }
                        list.OnUpGradeClick(card, g.conf.roleGrade.GetItem(int.Parse(p[1])));
                        return info + " OnUpGradeClick called; " + Stats(Me());
                    }
                case "upcomplete":
                    UnitActionRoleUpGrade.ActionUpGradeComplete(Me(), g.conf.roleGrade.GetItem(int.Parse(p[1])), true);
                    return "ActionUpGradeComplete called; " + Stats(Me()) + " | " + Luck(Me());
                case "upcheck":
                case "upgo":
                    return UpGrade(p[0] == "upgo", int.Parse(p[1]), p.Length > 2 ? int.Parse(p[2]) : 0, p.Length > 3 ? int.Parse(p[3]) : 0);
                case "luckfind": return LuckFind(p[1]);
                case "bornset":
                    Me().data.unitData.propertyData.bornLuck[int.Parse(p[1])].id = int.Parse(p[2]);
                    return "bornLuck[" + p[1] + "].id written; " + Luck(Me()) + " | " + Stats(Me());
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

        // 대경지 돌파 검증. requireId의 아이템을 count개 재료로 넣어 돌파 행동의 판정값을 보거나(upcheck) 실행한다(upgo).
        private static string UpGrade(bool go, int gradeId, int requireId, int count)
        {
            WorldUnitBase me = Me();
            var soles = new System.Collections.Generic.List<string>();
            if (requireId != 0)
            {
                var owned = me.data.unitData.propData.GetProps(requireId);
                for (int i = 0; owned != null && i < owned.Count && soles.Count < count; i++)
                {
                    soles.Add(owned[i].soleID);
                }
            }
            var require = new Il2CppStringArray(soles.ToArray());
            var rarity = new Il2CppStringArray(0);
            var action = new UnitActionRoleUpGrade(gradeId, require, rarity);
            string head = "materials=" + soles.Count + " completeRate=" + UnitActionRoleUpGrade.GetCompleteRate(me, require);
            if (!go)
            {
                return head + " IsCreateAction=" + me.IsCreateAction(action, false) + " | " + Stats(me);
            }
            int code = me.CreateAction(action, false);
            return head + " CreateAction=" + code + " state=" + action.state + " addLife=" + action.addLife
                + " addAbilityPoint=" + action.addAbilityPoint + " | " + Stats(me) + " | " + Luck(me);
        }

        private static string MartialDetail(string unitId, string soleId)
        {
            DataProps.MartialData martial = MartialOf(unitId, soleId);
            var conf = g.conf.battleSkillPrefixValue;
            var sb = new StringBuilder("name=" + martial.martialInfo.name + " level=" + martial.martialInfo.level
                + " type=" + (int)martial.martialType + " baseID=" + martial.baseID + " grade=" + martial.grade);
            var shown = conf.GetPrefix(martial);
            for (int i = 0; i < shown.Count; i++)
            {
                var d = shown[i];
                sb.Append("\n  [").Append(d.index).Append("] num=").Append(d.prefixValueItem.number)
                    .Append(" prefixLevel=").Append(d.prefixLevel).Append(" level=").Append(d.level)
                    .Append(" max=").Append(conf.GetPrefixMaxLevel(martial, d.prefixValueItem))
                    .Append(" multi=").Append(d.prefixValueItem.multi).Append(" require=").Append(d.prefixValueItem.require)
                    .Append(" select=").Append(d.prefixValueItem.selectType)
                    .Append(" desc=").Append(d.prefixValueItemDesc);
            }
            var valid = conf.GetItems((int)martial.martialType, martial.baseID);
            sb.Append("\n  validItems=").Append(valid == null ? -1 : valid.Count);
            for (int i = 0; valid != null && i < valid.Count; i++)
            {
                var item = valid[i];
                sb.Append("\n    num=").Append(item.number).Append(" id=").Append(item.id).Append(" multi=").Append(item.multi)
                    .Append(" require=").Append(item.require).Append(" select=").Append(item.selectType)
                    .Append(" w=").Append(item.weight1).Append(',').Append(item.weight2).Append(',').Append(item.weight3)
                    .Append(',').Append(item.weight4).Append(',').Append(item.weight5)
                    .Append(" max=").Append(conf.GetPrefixMaxLevel(martial, item))
                    .Append(" desc=").Append(GameTool.LS(item.desc));
            }
            return sb.ToString();
        }

        private static string LuckFind(string text)
        {
            var sb = new StringBuilder();
            var all = g.conf.roleCreateFeature._allConfList;
            for (int i = 0; i < all.Count && sb.Length < 1500; i++)
            {
                string name = GameTool.LS(all[i].name);
                if (name.Contains(text))
                {
                    sb.Append(all[i].id).Append(":").Append(name).Append("(type").Append(all[i].type).Append(" lv").Append(all[i].level).Append(") ");
                }
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
