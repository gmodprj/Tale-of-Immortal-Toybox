using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 트레이너 3차(스펙 2026-10-23-trainer3-design.md, 05 문서 82절)의 전승·참오를 명령으로 부른다. 검사 trainer3-check.ps1이 쓴다.
    // [unitID]를 주면 그 인물(NPC), 없으면 플레이어.
    //   msole <기본 번호|0> [unitID]            그 기본 번호의 공법 sole(처음 것). 0이면 심법마다 sole:기본 번호
    //   msuits <sole> [unitID]                 SuitsFor: id:이름:members=n:held=n:cur=0|1 …
    //   msuit <sole> <세트 번호|0> [unitID]     ApplySuit, 0이면 RemoveSuit
    //   msuitstate <sole> [unitID]             suit=n isSuit=true|false values=<valuesStr>
    //   mvalues <sole> <글> [unitID]            valuesStr를 그 글로(검사 준비. "-"는 빈 글)
    internal static class DevCommandsTrainer3
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "msole":
                    return Sole(Unit(p, 2), int.Parse(p[1]));
                case "msuits":
                    return Suits(Unit(p, 2), p[1]);
                case "msuit":
                    {
                        WorldUnitBase unit = Unit(p, 3);
                        int id = int.Parse(p[2]);
                        return id == 0 ? MartialService.RemoveSuit(unit, p[1]) : MartialService.ApplySuit(unit, p[1], id);
                    }
                case "msuitstate":
                    return SuitState(Unit(p, 2), p[1]);
                case "mvalues":
                    {
                        var action = Unit(p, 3).data.unitData.GetActionMartial(p[1]);
                        if (action == null)
                        {
                            return "no martial " + p[1];
                        }
                        action.data.valuesStr = p[2] == "-" ? "" : p[2];
                        return "values=" + action.data.valuesStr;
                    }
                default:
                    return null;
            }
        }

        private static WorldUnitBase Unit(string[] p, int index)
        {
            if (p.Length > index && p[index] != "me")
            {
                WorldUnitBase unit = g.world.unit.GetUnit(p[index], true);
                if (unit != null)
                {
                    return unit;
                }
            }
            return g.world.playerUnit;
        }

        private static string Sole(WorldUnitBase unit, int baseId)
        {
            if (baseId == 0)
            {
                // 0: 심법의 sole:기본 번호 목록
                var sb = new StringBuilder();
                foreach (var pair in unit.data.unitData.allActionMartial)
                {
                    var m = pair.Value.data.To<DataProps.MartialData>();
                    if (m.martialType == MartialType.Ability)
                    {
                        sb.Append(pair.Key).Append(':').Append(m.baseID).Append(' ');
                    }
                }
                return sb.Length == 0 ? "none" : sb.ToString();
            }
            foreach (var pair in unit.data.unitData.allActionMartial)
            {
                if (pair.Value.data.To<DataProps.MartialData>().baseID == baseId)
                {
                    return pair.Key;
                }
            }
            return "none";
        }

        private static string Suits(WorldUnitBase unit, string sole)
        {
            var sb = new StringBuilder();
            foreach (MartialService.SuitView v in MartialService.SuitsFor(unit, sole))
            {
                sb.Append(v.Id).Append(':').Append(v.Name).Append(":members=").Append(v.Members.Count).Append(":held=").Append(v.MembersHeld.Count)
                    .Append(":cur=").Append(v.Current ? 1 : 0).Append(' ');
            }
            return sb.Length == 0 ? "no suits" : sb.ToString();
        }

        private static string SuitState(WorldUnitBase unit, string sole)
        {
            var action = unit.data.unitData.GetActionMartial(sole);
            if (action == null)
            {
                return "no martial " + sole;
            }
            bool isSuit = false;
            try
            {
                isSuit = g.world.suitAbility.IsSuitAbility(action.data.To<DataProps.MartialData>());
            }
            catch (System.Exception)
            {
            }
            return "suit=" + MartialService.SuitOf(action.data) + " isSuit=" + (isSuit ? "true" : "false") + " values=" + action.data.valuesStr;
        }
    }
}
