using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 기예 재료 지급(05 문서 62절)의 확인.
    //   matlist <분류 0|1|2> [all] [검색어]   줄마다 key|learned|요약 (all이 없으면 배운 것만)
    //   matgive <분류> <키> <묶음>             CraftMaterialService.Give
    internal static class DevCommandsMaterials
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "matlist":
                    {
                        bool all = p.Length > 2 && p[2] == "all";
                        string query = p.Length > (all ? 3 : 2) ? p[all ? 3 : 2] : "";
                        var sb = new StringBuilder();
                        var list = CraftMaterialService.Recipes(int.Parse(p[1]), !all, query);
                        sb.Append("count=").Append(list.Count);
                        foreach (CraftMaterialService.Recipe r in list)
                        {
                            sb.Append('\n').Append(r.Key).Append('|').Append(r.Learned).Append('|').Append(CraftMaterialService.Summary(r));
                        }
                        return sb.ToString();
                    }
                case "matgive":
                    {
                        int kind = int.Parse(p[1]);
                        var list = CraftMaterialService.Recipes(kind, false, "");
                        CraftMaterialService.Recipe r = list.Find(x => x.Key == p[2]);
                        return r == null ? "no recipe " + p[2] : CraftMaterialService.Give(r, int.Parse(p[3]));
                    }
                default:
                    return null;
            }
        }
    }
}
