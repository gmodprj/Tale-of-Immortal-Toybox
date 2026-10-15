using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 자원 탭의 검증용 명령(tools/research/ui-check.ps1의 groups·favstore·favorites 부분).
    //   rgroups : total=<전체> other=<기타 수> dupes=<표의 중복 번호 수> sum=<그룹 합> otherClasses=<번호,…> | <그룹 키>=<수> …
    internal static class DevCommandsResources
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "rgroups":
                    return Groups();
                default:
                    return null;
            }
        }

        private static string Groups()
        {
            Dictionary<string, int> counts = ItemCatalog.CountByGroup(new ItemCatalog.Filter(), null);
            int sum = 0;
            var sb = new StringBuilder();
            foreach (ItemGroups.Group group in ItemGroups.Groups)
            {
                sum += counts[group.Key];
                sb.Append(' ').Append(group.Key).Append('=').Append(counts[group.Key]);
            }
            var others = new List<string>();
            foreach (ItemGroups.Kind kind in ItemGroups.ByKey(ItemGroups.OtherKey).Kinds)
            {
                foreach (int c in kind.Classes)
                {
                    others.Add(c.ToString());
                }
            }
            return "total=" + counts[ItemGroups.AllKey] + " other=" + counts[ItemGroups.OtherKey] + " dupes=" + ItemGroups.Duplicates
                + " sum=" + sum + " otherClasses=" + string.Join(",", others.ToArray()) + " |" + sb;
        }
    }
}
