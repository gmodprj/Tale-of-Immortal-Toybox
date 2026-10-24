using System.Text;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 개발 전용. 기술(무예·영술, 절기, 신법, 심법, 신통)과 기예의 추가·삭제·수련 경로를 조사하는 명령.
    internal static class DevCommandsSkill
    {
        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world.playerUnit;
            var data = me.data.unitData;
            switch (p[0])
            {
                case "skconf": return Conf(int.Parse(p[1]), p.Length > 2 ? int.Parse(p[2]) : 0);
                case "sklist": return List(me);
                case "sknew":
                    {
                        // 조합 번호와 품질로 공법 아이템을 만들어 가방에 넣는다.
                        DataProps.PropsData props = NewMartial(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                        data.propData.AddProps(props);
                        return "created soleID=" + props.soleID + " propsID=" + props.propsID + " values=" + Join(props.values) + " | bag=" + data.propData.allProps.Count;
                    }
                case "sklearn":
                    {
                        DataProps.PropsData props = NewMartial(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                        int before = data.allActionMartial.Count;
                        var action = new UnitActionPropMartialStudy(props);
                        action.isCost = false;
                        action.isNotCheckAttr = true;
                        action.checkGrade = false;
                        action.checkProp = false;
                        action.checkBas = false;
                        int can = me.IsCreateAction(action, false);
                        int code = me.CreateAction(action, false);
                        return "UnitActionPropMartialStudy can=" + can + " code=" + code + " complete=" + action.isStudyComplete
                            + " martial " + before + " -> " + data.allActionMartial.Count + " | " + List(me);
                    }
                case "sklearn2":
                    {
                        DataProps.PropsData props = NewMartial(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                        int before = data.allActionMartial.Count;
                        DataUnit.ActionMartialData learned = data.StudyActionMartialProp(props);
                        return "StudyActionMartialProp returned " + (learned == null ? "null" : "data") + " martial " + before + " -> " + data.allActionMartial.Count + " | " + List(me);
                    }
                case "skdel":
                    {
                        int before = data.allActionMartial.Count;
                        var action = new UnitActionMartialDel(p[1]);
                        action.isRewardItem = false;
                        int code = me.CreateAction(action, false);
                        return "UnitActionMartialDel code=" + code + " martial " + before + " -> " + data.allActionMartial.Count + " | " + List(me);
                    }
                case "skdel2":
                    {
                        int before = data.allActionMartial.Count;
                        var action = new UnitActionMartialDel(data.GetActionMartial(p[1]));
                        int can = me.IsCreateAction(action, false);
                        int code = me.CreateAction(action, false);
                        return "UnitActionMartialDel(data) can=" + can + " code=" + code + " martial " + before + " -> " + data.allActionMartial.Count + " bag=" + data.propData.allProps.Count + " | " + List(me);
                    }
                case "skdel3":
                    {
                        int before = data.allActionMartial.Count;
                        data.DelActionMartial(p[1]);
                        return "DelActionMartial martial " + before + " -> " + data.allActionMartial.Count + " | " + List(me);
                    }
                case "sktrain":
                    {
                        DataUnit.ActionMartialData martial = data.GetActionMartial(p[1]);
                        int before = martial.exp;
                        var action = new UnitActionMartialTrain(martial, int.Parse(p[2]));
                        int can = me.IsCreateAction(action, false);
                        int code = p.Length > 3 && p[3] == "go" ? me.CreateAction(action, false) : -999;
                        return "UnitActionMartialTrain can=" + can + " code=" + code + " addExp=" + action.addExp + " costNum=" + action.costNum
                            + " exp " + before + " -> " + martial.exp + " maxDay=" + UnitActionMartialTrain.GetTrainMaxDay(me, martial) + " day=" + g.world.run.roundDay;
                    }
                case "skrealize":
                    {
                        DataUnit.ActionMartialData martial = data.GetActionMartial(p[1]);
                        var action = new UnitActionMartialRealize(martial);
                        return "UnitActionMartialRealize can=" + me.IsCreateAction(action, false) + " isCostItem=" + action.isCostItem
                            + " realizeDatas=" + (action.realizeDatas == null ? "null" : action.realizeDatas.Count.ToString());
                    }
                case "skequip":
                    {
                        DataUnit.ActionMartialData martial = data.GetActionMartial(p[1]);
                        var action = new UnitActionMartialEquip(martial, int.Parse(p[2]));
                        action.ingonAbilityPoint = true;
                        int code = me.CreateAction(action, false);
                        return "UnitActionMartialEquip code=" + code + " | " + List(me);
                    }
                case "skunequip":
                    {
                        DataUnit.ActionMartialData martial = data.GetActionMartial(p[1]);
                        int code = me.CreateAction(new UnitActionMartialUnequip(martial), false);
                        return "UnitActionMartialUnequip code=" + code + " | " + List(me);
                    }
                case "arts": return Arts();
                case "artstudy":
                    {
                        DataProps.PropsData props = DataProps.PropsData.NewProps(int.Parse(p[1]), 1);
                        int before = g.data.world.allArtistrySkillID.Count;
                        var action = new UnitUnitArtistrySkillStudy(props);
                        int can = me.IsCreateAction(action, false);
                        int code = me.CreateAction(action, false);
                        return "UnitUnitArtistrySkillStudy can=" + can + " code=" + code + " artistry " + before + " -> " + g.data.world.allArtistrySkillID.Count + " | " + Arts();
                    }
                case "pillstudy":
                    {
                        DataProps.PropsData props = DataProps.PropsData.NewProps(int.Parse(p[1]), 1);
                        int before = g.data.world.pillFormulas.Count;
                        var action = new UnitActionPropPillFormulaStudy(props);
                        int can = me.IsCreateAction(action, false);
                        int code = me.CreateAction(action, false);
                        return "UnitActionPropPillFormulaStudy can=" + can + " code=" + code + " formulas " + before + " -> " + g.data.world.pillFormulas.Count + " | " + Arts();
                    }
                default: return null;
            }
        }

        private static DataProps.PropsData NewMartial(int type, int combineId, int grade)
        {
            Il2CppStructArray<int> values;
            switch (type)
            {
                case 3: values = RewardTool.GetStepReward(combineId, grade, null); break;
                case 4: values = RewardTool.GetAbilityReward(combineId, grade, null); break;
                default: values = RewardTool.GetSkillReward(combineId, grade, null); break;
            }
            return DataProps.PropsData.NewMartial((MartialType)type, values);
        }

        private static string Join(Il2CppStructArray<int> values)
        {
            var sb = new StringBuilder();
            for (int i = 0; values != null && i < values.Length && i < 20; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(values[i]);
            }
            return sb.ToString();
        }

        private static string Conf(int type, int skip)
        {
            var sb = new StringBuilder();
            if (type == 3)
            {
                var all = g.conf.battleStepCombine._allConfList;
                sb.Append("step total=").Append(all.Count).Append(" ");
                for (int i = skip; i < all.Count && sb.Length < 1500; i++)
                {
                    sb.Append(all[i].id).Append(":").Append(GameTool.LS(all[i].itemName)).Append("(type=").Append(all[i].type).Append(" lv=").Append(all[i].level).Append(" cls=").Append(all[i].className).Append("); ");
                }
            }
            else if (type == 4)
            {
                var all = g.conf.battleAbilityCombine._allConfList;
                sb.Append("ability total=").Append(all.Count).Append(" ");
                for (int i = skip; i < all.Count && sb.Length < 1500; i++)
                {
                    sb.Append(all[i].id).Append(":").Append(GameTool.LS(all[i].itemName)).Append("(type=").Append(all[i].type).Append(" lv=").Append(all[i].level).Append(" cls=").Append(all[i].className).Append("); ");
                }
            }
            else
            {
                var all = g.conf.battleSkillCombine._allConfList;
                sb.Append("skill total=").Append(all.Count).Append(" ");
                for (int i = skip; i < all.Count && sb.Length < 1500; i++)
                {
                    sb.Append(all[i].id).Append(":").Append(GameTool.LS(all[i].itemName)).Append("(type=").Append(all[i].type).Append(" base=").Append(all[i].baseID).Append(" lv=").Append(all[i].level).Append(" cls=").Append(all[i].className).Append("); ");
                }
            }
            return sb.ToString();
        }

        private static string List(WorldUnitBase unit)
        {
            var data = unit.data.unitData;
            var sb = new StringBuilder("equip L=" + data.skillLeft + " R=" + data.skillRight + " U=" + data.ultimate + " step=" + data.step + " abilitys=[");
            for (int i = 0; data.abilitys != null && i < data.abilitys.Length; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(data.abilitys[i]);
            }
            sb.Append("] learned:");
            var e = data.allActionMartial.GetEnumerator();
            while (e.MoveNext())
            {
                var martial = e.Current.Value.data.To<DataProps.MartialData>();
                sb.Append(" ").Append(e.Current.Key).Append("=").Append(martial.martialInfo.name).Append("(").Append(martial.martialType)
                    .Append(" base").Append(martial.baseID).Append(" g").Append(martial.grade).Append(" exp").Append(e.Current.Value.exp).Append(")");
            }
            return sb.ToString();
        }

        private static string Arts()
        {
            var world = g.data.world;
            var sb = new StringBuilder("pillFormulas=" + world.pillFormulas.Count + "[");
            for (int i = 0; i < world.pillFormulas.Count && i < 12; i++)
            {
                var f = world.pillFormulas[i];
                ConfMakePillFormulaItem conf = g.conf.makePillFormula.GetItem(f.id);
                sb.Append(f.id).Append(":").Append(conf == null ? "?" : GameTool.LS(conf.name)).Append(" ").Append(f.exp).Append("/").Append(f.maxExp).Append("; ");
            }
            sb.Append("] artistry=").Append(world.allArtistrySkillID.Count).Append("[");
            for (int i = 0; i < world.allArtistrySkillID.Count && i < 30; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(world.allArtistrySkillID[i]);
            }
            sb.Append("] martialStudy=").Append(world.allMartialStudy.Count)
                .Append(" counts elixir=").Append(world.playerLog.refineElixirCount).Append(" weapon=").Append(world.playerLog.refineWeaponCount)
                .Append(" symbol=").Append(world.playerLog.symbolCount).Append(" geo=").Append(world.playerLog.geomanticCount)
                .Append(" herb=").Append(world.playerLog.herbCount).Append(" mine=").Append(world.playerLog.mineCount);
            return sb.ToString();
        }
    }
}
