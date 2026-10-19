using System;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 기술 행동의 날짜 소모 배율(05 문서 63절)의 실험.
    //   adstudy <비급 sole> [mode]   학습 행동을 만들어 IsCreate 뒤의 칸(day, exp, costItem.days, money)을 보이고 Create한다.
    //                               mode: none(그대로) · cfg<배율>(Create 동안 costItem.days와 day를 곱함)
    //   adtrain <날수>              첫 공법을 그 날수로 수련(UnitActionMartialTrain). 경험치·남은 날 전후
    //   addel                       마지막 공법을 망각(UnitActionMartialDel). 남은 날 전후
    internal static class DevCommandsActionDay
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "adstudy":
                    return Study(p[1], p.Length > 2 ? p[2] : "none");
                case "adtrain":
                    return Train(int.Parse(p[1]));
                case "addel":
                    return Del();
                case "adrealize":
                    return Realize();
                default:
                    return null;
            }
        }

        private static WorldUnitBase Me()
        {
            return g.world == null ? null : g.world.playerUnit;
        }

        // 첫(first) 또는 마지막 공법.
        private static DataUnit.ActionMartialData Pick(WorldUnitBase me, bool first)
        {
            DataUnit.ActionMartialData found = null;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                found = pair.Value;
                if (first)
                {
                    break;
                }
            }
            return found;
        }

        private static int Residue()
        {
            return g.world.run.roundDayResidue;
        }

        private static string Study(string sole, string mode)
        {
            WorldUnitBase me = Me();
            var all = me.data.unitData.propData.allProps;
            DataProps.PropsData book = null;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].soleID == sole)
                {
                    book = all[i];
                }
            }
            if (book == null)
            {
                return "no prop " + sole;
            }
            var sb = new StringBuilder();
            var action = new UnitActionPropMartialStudy(book);
            int can = me.IsCreateAction(action, false);
            ConfBattleSkillCostItem cost = action.skillCostItem;
            sb.Append("can=").Append(can).Append(" day=").Append(action.day).Append(" exp=").Append(action.exp).Append(" money=").Append(action.money)
              .Append(" costDays=").Append(cost == null ? -1 : cost.days).Append(" talentExp=").Append(cost == null ? -1 : cost.talentExp)
              .Append(" complete=").Append(action.isStudyComplete);
            if (can != 0)
            {
                return sb.ToString();
            }
            int r0 = Residue();
            int savedDays = cost == null ? 0 : cost.days;
            if (mode.StartsWith("cfg") && cost != null)
            {
                float rate = float.Parse(mode.Substring(3), System.Globalization.CultureInfo.InvariantCulture);
                cost.days = (int)Math.Round(savedDays * rate, MidpointRounding.AwayFromZero);
                action.day = (int)Math.Round(action.day * rate, MidpointRounding.AwayFromZero);
                sb.Append(" scaled day=").Append(action.day).Append(" costDays=").Append(cost.days);
            }
            try
            {
                me.CreateAction(action, false);
            }
            finally
            {
                if (cost != null)
                {
                    cost.days = savedDays;
                }
            }
            sb.Append(" | after exp=").Append(action.exp).Append(" complete=").Append(action.isStudyComplete).Append(" residue ").Append(r0).Append("->").Append(Residue());
            return sb.ToString();
        }

        private static string Train(int days)
        {
            WorldUnitBase me = Me();
            // 수련할 수 있는 날이 가장 긴 공법
            DataUnit.ActionMartialData a = null;
            int best = 0;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                int max = UnitActionMartialTrain.GetTrainMaxDay(me, pair.Value);
                if (max > best)
                {
                    best = max;
                    a = pair.Value;
                }
            }
            if (a == null)
            {
                return "no trainable martial";
            }
            int e0 = a.exp;
            int r0 = Residue();
            var action = new UnitActionMartialTrain(a, days);
            int can = me.IsCreateAction(action, false);
            string head = "can=" + can + " day=" + action.day + " addExp=" + action.addExp + " maxDay=" + UnitActionMartialTrain.GetTrainMaxDay(me, a);
            if (can != 0)
            {
                return head;
            }
            me.CreateAction(action, false);
            return head + " | exp " + e0 + "->" + a.exp + " residue " + r0 + "->" + Residue() + " martial=" + a.data.soleID;
        }

        // 참오: 행동을 만들어 IsCreate 뒤의 day만 본다(실행하지 않는다).
        private static string Realize()
        {
            WorldUnitBase me = Me();
            var sb = new StringBuilder();
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                var action = new UnitActionMartialRealize(pair.Value);
                int can = me.IsCreateAction(action, false);
                sb.Append(pair.Key).Append(":can=").Append(can).Append(",day=").Append(action.day)
                  .Append(",cost=").Append(action.realizeCostItem == null ? "-" : action.realizeCostItem.id.ToString()).Append(' ');
                if (sb.Length > 600)
                {
                    break;
                }
            }
            return sb.ToString();
        }

        private static string Del()
        {
            WorldUnitBase me = Me();
            var list = me.data.unitData.allActionMartial;
            DataUnit.ActionMartialData a = Pick(me, false);
            if (a == null)
            {
                return "no martial";
            }
            int n0 = list.Count, r0 = Residue();
            var action = new UnitActionMartialDel(a);
            int can = me.IsCreateAction(action, false);
            string head = "can=" + can + " day=" + action.day;
            if (can != 0)
            {
                return head;
            }
            me.CreateAction(action, false);
            return head + " | martials " + n0 + "->" + me.data.unitData.allActionMartial.Count + " residue " + r0 + "->" + Residue();
        }
    }
}
