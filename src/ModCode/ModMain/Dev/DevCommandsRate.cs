using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 배율 탭의 검증 명령. tools/research/ui-check.ps1의 rates, rates-battle이 이 출력 모양에 의존한다.
    //   rstate   배율의 값과 패치 여부, 마지막으로 바꾼 값, 전투 보조, Time.timeScale.
    //   rnpc     플레이어가 아닌 단위 하나의 AddExp(10)을 부른다(배율이 걸리지 않아야 한다).
    //   rneg     플레이어의 AddExp(-7)을 부른다(곱해지지 않아야 한다).
    internal static class DevCommandsRate
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "rstate":
                    return State();
                case "rnpc":
                    return Npc();
                case "rneg":
                    {
                        var saved = g.world.playerUnit.data.unitData.propertyData;
                        int before = saved.exp;
                        saved.AddExp(-7);
                        return "player AddExp(-7) exp " + before + " -> " + saved.exp;
                    }
                default:
                    return null;
            }
        }

        private static string State()
        {
            var sb = new StringBuilder();
            foreach (RateService.Rate rate in RateService.All)
            {
                sb.Append(rate.Key).Append('=').Append(rate.Text.Substring(1)).Append(rate.Patched ? "(patched)" : "").Append(' ');
            }
            sb.Append("god=").Append(BattleService.God).Append(" damage=").Append(BattleService.Multiplier)
                .Append(" lastExp=").Append(RateService.Exp.Last)
                .Append(" lastMartial=").Append(RateService.Martial.Last)
                .Append(" lastMove=").Append(RateService.Move.Last)
                .Append(" timeScale=").Append(UnityEngine.Time.timeScale.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(" inBattle=").Append(RateService.InBattle);
            return sb.ToString();
        }

        private static string Npc()
        {
            string myId = g.world.playerUnit.data.unitData.unitID;
            var units = g.world.unit.GetUnits(false);
            string last = "no npc";
            for (int i = 0; i < units.Count; i++)
            {
                var info = units[i].data.unitData;
                if (info.unitID == myId)
                {
                    continue;
                }
                int before = info.propertyData.exp;
                info.propertyData.AddExp(10);
                last = "npc exp " + before + " -> " + info.propertyData.exp;
                // 수련치가 상한인 단위는 10이 다 오르지 않는다. 다 오른 단위를 찾을 때까지 넘어간다.
                if (info.propertyData.exp - before == 10)
                {
                    return last;
                }
            }
            return last;
        }
    }
}
