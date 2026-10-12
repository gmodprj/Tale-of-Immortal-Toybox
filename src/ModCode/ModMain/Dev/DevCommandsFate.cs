using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 역천개명 운명이 저장되는 모양을 조사하는 명령.
    internal static class DevCommandsFate
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "luckdump": return Dump(p.Length > 1 && p[1] != "me" ? g.world.unit.GetUnit(p[1], true) : g.world.playerUnit);
                case "gradelog":
                    {
                        var log = g.data.world.playerLog.upGrade;
                        if (p.Length > 3)
                        {
                            var entry = new DataWorld.World.PlayerLogData.GradeData();
                            entry.luck = int.Parse(p[2]);
                            entry.quality = int.Parse(p[3]);
                            log[int.Parse(p[1])] = entry;
                        }
                        return "player upGrade " + GradeText(log);
                    }
                case "editor":
                    // 단축키 입력 없이 편집기 창을 연다(자동 검증용).
                    if (!EditorWindow.IsOpen)
                    {
                        EditorWindow.Toggle();
                    }
                    return "editor open=" + EditorWindow.IsOpen;
                case "closeui":
                    g.ui.CloseUI(UIType.UpGrade, true);
                    return "closed UpGrade";
                case "findfated": return FindFated(p.Length > 1 ? int.Parse(p[1]) : 0);
                default: return null;
            }
        }

        private static string ObjText(DataObjectData obj)
        {
            if (obj == null)
            {
                return "obj=null";
            }
            var sb = new StringBuilder("obj{");
            var groups = obj.allObject.GetEnumerator();
            while (groups.MoveNext())
            {
                sb.Append(groups.Current.Key).Append(":[");
                var pairs = groups.Current.Value.GetEnumerator();
                while (pairs.MoveNext())
                {
                    sb.Append(pairs.Current.Key).Append('=').Append(pairs.Current.Value).Append(',');
                }
                sb.Append("] ");
            }
            return sb.Append('}').ToString();
        }

        private static string GradeText(Il2CppSystem.Collections.Generic.Dictionary<int, DataWorld.World.PlayerLogData.GradeData> log)
        {
            if (log == null)
            {
                return "null";
            }
            var sb = new StringBuilder("{");
            var e = log.GetEnumerator();
            while (e.MoveNext())
            {
                sb.Append(e.Current.Key).Append(":quality=").Append(e.Current.Value.quality).Append(",luck=").Append(e.Current.Value.luck).Append(" ");
            }
            return sb.Append("}").ToString();
        }

        private static string One(DataUnit.LuckData luck)
        {
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(luck.id);
            return luck.id + ":" + (conf == null ? "?" : GameTool.LS(conf.name) + "(type" + conf.type + ")")
                + " dur=" + luck.duration + " time=" + luck.createTime + " " + ObjText(luck.objData) + "; ";
        }

        private static string Dump(WorldUnitBase unit)
        {
            var saved = unit.data.unitData.propertyData;
            var sb = new StringBuilder(unit.data.unitData.unitID + " grade=" + saved.gradeID + " BORN ");
            for (int i = 0; i < saved.bornLuck.Length; i++)
            {
                sb.Append(One(saved.bornLuck[i]));
            }
            sb.Append(" ADD ");
            for (int i = 0; i < saved.addLuck.Count; i++)
            {
                sb.Append(One(saved.addLuck[i]));
            }
            sb.Append(" NPCUPGRADE ").Append(GradeText(unit.data.unitData.npcUpGrade));
            sb.Append(" UNITOBJ ").Append(ObjText(unit.data.unitData.objData));
            return sb.ToString();
        }

        // 역천개명 분류(type 3)의 운명을 가진 NPC를 찾아 그 운명 목록을 보여 준다.
        private static string FindFated(int skip)
        {
            var units = g.world.unit.GetUnits(false);
            string me = g.world.playerUnit.data.unitData.unitID;
            for (int i = 0; i < units.Count; i++)
            {
                var saved = units[i].data.unitData.propertyData;
                if (units[i].data.unitData.unitID == me)
                {
                    continue;
                }
                for (int k = 0; k < saved.addLuck.Count; k++)
                {
                    ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(saved.addLuck[k].id);
                    if (conf != null && conf.type == 3)
                    {
                        if (skip-- > 0)
                        {
                            break;
                        }
                        return Dump(units[i]);
                    }
                }
            }
            return "no unit with a type-3 luck in addLuck (units=" + units.Count + ")";
        }
    }
}
