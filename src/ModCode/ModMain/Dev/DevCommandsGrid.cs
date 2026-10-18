using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 기술 학습 상한(05 문서 60절)의 확인.
    //   mgrid            bonus=<더한 칸> grade=<대경지> 그리고 종류마다 <탭>:<배운 수>/<게임의 칸 수>
    //   mgridset <값>    상한 단계를 바꾼다(MartialGridService.Set. 0·10·50·200)
    internal static class DevCommandsGrid
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "mgrid":
                    return State();
                case "mgridset":
                    return MartialGridService.Set(int.Parse(p[1]));
                default:
                    return null;
            }
        }

        private static string State()
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            if (me == null)
            {
                return "not ready";
            }
            int grade = g.conf.roleGrade.GetItem(me.data.unitData.propertyData.gradeID).grade;
            var sb = new StringBuilder();
            sb.Append("bonus=").Append(MartialGridService.Bonus).Append(" grade=").Append(grade);
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                Dictionary<int, int> have = SkillCatalog.LearnedCounts(me, k);
                int n = 0;
                foreach (int c in have.Values)
                {
                    n += c;
                }
                int max = g.conf.roleMartialGradeGrid.GetMaxGrid(grade, SkillCatalog.Kinds[k].Type);
                sb.Append(' ').Append(SkillCatalog.Kinds[k].Tab).Append(':').Append(n).Append('/').Append(max);
            }
            return sb.ToString();
        }
    }
}
