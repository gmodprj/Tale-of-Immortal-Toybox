using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 자원 탭의 검증용 명령(tools/research/ui-check.ps1의 groups·favstore·favorites 부분).
    //   rgroups : total=<전체> other=<기타 수> dupes=<표의 중복 번호 수> sum=<그룹 합> otherClasses=<번호,…> | <그룹 키>=<수> …
    //   rfav [reset|reload|add <번호> <수량>|set <번호> <수량>|del <번호>]
    //           : (동작이 있으면 "<문장> | ") + count=<수> skipped=<건너뛴 줄> items=<번호>:<수량>[:missing],…
    //             reset은 favorites.txt(.bak, .tmp 포함)를 지우고 다시 읽게 한다. reload는 파일을 그대로 두고 다시 읽게 한다
    internal static class DevCommandsResources
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "rgroups":
                    return Groups();
                case "rfav":
                    return Favorites(p);
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

        private static string Favorites(string[] p)
        {
            string what = p.Length > 1 ? p[1] : "";
            string text = null;
            string path = Path.Combine(ChangeLog.DataDir, "favorites.txt");
            switch (what)
            {
                case "reset":
                    foreach (string file in new[] { path, path + ".bak", path + ".tmp" })
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                    FavoriteStore.Forget();
                    break;
                case "reload":
                    FavoriteStore.Forget();
                    break;
                case "add":
                    FavoriteStore.Add(int.Parse(p[2]), int.Parse(p[3]), out text);
                    break;
                case "set":
                    FavoriteStore.SetAmount(int.Parse(p[2]), int.Parse(p[3]), out text);
                    break;
                case "del":
                    FavoriteStore.Remove(int.Parse(p[2]), out text);
                    break;
            }
            var items = new List<string>();
            foreach (FavoriteStore.Favorite f in FavoriteStore.Entries)
            {
                items.Add(f.Id + ":" + f.Amount + (ItemCatalog.Get(f.Id) == null ? ":missing" : ""));
            }
            string state = "count=" + items.Count + " skipped=" + FavoriteStore.SkippedLines + " items=" + string.Join(",", items.ToArray());
            return text == null ? state : text + " | " + state;
        }
    }
}
