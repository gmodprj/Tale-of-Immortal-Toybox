using System.Text;

namespace MOD_sWP6UL
{
    // 선법 탭의 검증용 명령(tools/research/immortal-check.ps1).
    internal static class DevCommandsImmortal
    {
        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            if (me == null)
            {
                return null;
            }
            switch (p[0])
            {
                case "ilist":
                    {
                        var sb = new StringBuilder();
                        foreach (ImmortalService.Entry e in ImmortalService.Owned(me))
                        {
                            sb.Append(sb.Length == 0 ? "" : ",").Append(e.CardId);
                        }
                        return "owned=" + ImmortalService.Owned(me).Count + " total=" + ImmortalService.TotalPoint(me) + " used=" + ImmortalService.UsedPoint(me) + " cards=" + sb;
                    }
                case "icount":
                    {
                        var list = ImmortalService.Find(int.Parse(p[1]), int.Parse(p[2]), p.Length > 3 ? p[3] : "");
                        return "count=" + list.Count + " first=" + (list.Count == 0 ? 0 : list[0].CardId);
                    }
                case "ilearn":
                    {
                        int id = int.Parse(p[1]);
                        ImmortalService.Entry entry = ImmortalService.All().Find(e => e.CardId == id);
                        return entry == null ? "no card " + id : ImmortalService.Learn(me, entry);
                    }
                case "ilearnall": return ImmortalService.LearnAll(me, ImmortalService.Find(int.Parse(p[1]), int.Parse(p[2]), ""));
                case "ihas": return "has=" + ImmortalService.Has(me, int.Parse(p[1]));
                default: return null;
            }
        }
    }
}
