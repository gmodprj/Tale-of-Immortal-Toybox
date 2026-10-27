using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 자원 탭의 검증용 명령(tools/research/ui-check.ps1의 groups·favstore·favorites 부분).
    //   rgroups : total=<전체> other=<기타 수> dupes=<표의 중복 번호 수> sum=<그룹 합> otherClasses=<번호,…> | <그룹 키>=<수> …
    //   rfav [reset|reload|add <번호> <수량>|set <번호> <수량>|del <번호>
    //         |addbook <분류> <기술> <경지> <최소 품질> <수량>|setbook <기술> <경지> <최소 품질> <수량>|delbook <기술> <경지> <최소 품질>]
    //           : (동작이 있으면 "<문장> | ") + count=<수> skipped=<건너뛴 줄> items=<번호>:<수량>[:missing],…
    //             비급 항목은 book:<분류>:<기술>:<경지>:<최소 품질>:<수량>[:missing]
    //             reset은 favorites.txt(.bak, .tmp 포함)를 지우고 다시 읽게 한다. reload는 파일을 그대로 두고 다시 읽게 한다
    //   rbook <분류> <기술 번호> <경지> <최소 품질> <권수> : made=<수> | <MartialBookService.Make의 문장>
    //   rbooks [기술 번호]   : 가방의 비급 count=<수> items=<soleID>:<기술 번호>:<경지>:<품질>:<이름>,…
    //   rbookstudy <soleID>  : 게임의 학습 행동(UnitActionPropMartialStudy)이 그 비급을 받아들이는지 can=<IsCreateAction>
    //   rskill <분류> <기술 번호> : min=<경지> max=<경지> name=<이름>
    //   rownskills <분류> <경지> : 그 기술 하나만 나오는 묶음이 있고 그 경지로 만들 수 있는 기술 ids=<번호>,…(표 순서, 열 개까지)
    //   rmods [경지] : 카탈로그의 모드 항목 count=<수> items=<분류>:<번호>:<이름>:<basType>:<NameFromCh>:<막힘 이유(그 경지. 기본 3) 또는 ->:<원본 번호(그 경지) 또는 ->,…
    //   rlearn <분류> <번호> <경지> : SkillCatalog.Learn의 문장(모드 항목은 거부 문장)
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
                case "rmods":
                    return Mods(p.Length > 1 ? int.Parse(p[1]) : 3);
                case "rlearn":
                    {
                        SkillCatalog.Entry entry = SkillCatalog.Get(int.Parse(p[1]), int.Parse(p[2]));
                        return entry == null ? "no skill " + p[2] : SkillCatalog.Learn(g.world.playerUnit, int.Parse(p[1]), entry, int.Parse(p[3]), false);
                    }
                case "rbook":
                    {
                        int kindIndex = int.Parse(p[1]);
                        SkillCatalog.Entry entry = SkillCatalog.Get(kindIndex, int.Parse(p[2]));
                        if (entry == null)
                        {
                            return "no skill " + p[2];
                        }
                        string text;
                        int made = MartialBookService.Make(g.world.playerUnit, kindIndex, entry, int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]), out text);
                        return "made=" + made + " | " + text;
                    }
                case "rbooks":
                    return Books(p.Length > 1 ? int.Parse(p[1]) : 0);
                case "rbookstudy":
                    {
                        WorldUnitBase me = g.world.playerUnit;
                        var all = me.data.unitData.propData.allProps;
                        for (int i = 0; i < all.Count; i++)
                        {
                            if (all[i].soleID == p[1])
                            {
                                return "can=" + me.IsCreateAction(new UnitActionPropMartialStudy(all[i]), false);
                            }
                        }
                        return "no prop " + p[1];
                    }
                case "rskill":
                    {
                        SkillCatalog.Entry entry = SkillCatalog.Get(int.Parse(p[1]), int.Parse(p[2]));
                        return entry == null ? "no skill " + p[2] : "min=" + entry.GradeMin + " max=" + entry.GradeMax + " name=" + entry.Name;
                    }
                case "rownskills":
                    {
                        int kindIndex = int.Parse(p[1]);
                        int grade = int.Parse(p[2]);
                        var ids = new List<string>();
                        foreach (SkillCatalog.Entry entry in SkillCatalog.Find(kindIndex, ""))
                        {
                            if (ids.Count < 10 && SkillCatalog.BlockReason(entry, grade) == null
                                && SkillCatalog.OwnCombine(SkillCatalog.Kinds[kindIndex], entry.BaseId) != 0)
                            {
                                ids.Add(entry.BaseId.ToString());
                            }
                        }
                        return "ids=" + string.Join(",", ids.ToArray());
                    }
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
                case "addbook":
                    FavoriteStore.AddBook(int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]), int.Parse(p[6]), out text);
                    break;
                case "setbook":
                    FavoriteStore.SetBookAmount(int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]), out text);
                    break;
                case "delbook":
                    FavoriteStore.RemoveBook(int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), out text);
                    break;
            }
            var items = new List<string>();
            foreach (FavoriteStore.Favorite f in FavoriteStore.Entries)
            {
                items.Add(f.Book
                    ? "book:" + f.BookKind + ":" + f.Id + ":" + f.Grade + ":" + f.MinLevel + ":" + f.Amount + (SkillCatalog.Get(f.BookKind, f.Id) == null ? ":missing" : "")
                    : f.Id + ":" + f.Amount + (ItemCatalog.Get(f.Id) == null ? ":missing" : ""));
            }
            string state = "count=" + items.Count + " skipped=" + FavoriteStore.SkippedLines + " items=" + string.Join(",", items.ToArray());
            return text == null ? state : text + " | " + state;
        }

        private static string Mods(int grade)
        {
            var items = new List<string>();
            int listed = 0, blank = 0, nobook = 0, outOfGrade = 0, noSource = 0;
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    if (!e.IsMod)
                    {
                        continue;
                    }
                    // 묶음 2(05 문서 78절): 목록에 남는 수와 숨긴 까닭별 수
                    if (e.Hidden) { if (e.HiddenReason == "이름·설명 없음") { blank++; } else { nobook++; } }
                    else if (!SkillCatalog.ModBookFits(e, grade)) { outOfGrade++; }
                    else if (SkillCatalog.BlockReason(e, grade) != null) { noSource++; }
                    else { listed++; }
                    string block = SkillCatalog.BlockReason(e, grade) ?? "-";
                    SkillCatalog.Entry source = SkillCatalog.SourceFor(k, e, grade);
                    items.Add(k + ":" + e.BaseId + ":" + e.Name + ":" + e.BasType + ":" + e.NameFromCh + ":" + block + ":" + (source == null ? "-" : source.BaseId.ToString())
                        + ":g" + e.BookGradeMin + "-" + e.BookGradeMax + (e.Hidden ? ":H" + e.HiddenReason : ""));
                }
            }
            return "count=" + items.Count + " listed=" + listed + " hiddenBlank=" + blank + " hiddenNoBook=" + nobook + " outOfGrade=" + outOfGrade + " noSource=" + noSource + " items=" + string.Join(",", items.ToArray());
        }

        private static string Books(int baseId)
        {
            var items = new List<string>();
            var all = g.world.playerUnit.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].propsType != DataProps.PropsDataType.Martial)
                {
                    continue;
                }
                try
                {
                    DataProps.MartialData martial = all[i].To<DataProps.MartialData>();
                    if (baseId == 0 || martial.baseID == baseId)
                    {
                        items.Add(all[i].soleID + ":" + martial.baseID + ":" + martial.grade + ":" + martial.martialInfo.level + ":" + MartialService.Name(martial));
                    }
                }
                catch (System.Exception e)
                {
                    items.Add(all[i].soleID + ":ERR " + e.GetType().Name);
                }
            }
            return "count=" + items.Count + " items=" + string.Join(",", items.ToArray());
        }
    }
}
