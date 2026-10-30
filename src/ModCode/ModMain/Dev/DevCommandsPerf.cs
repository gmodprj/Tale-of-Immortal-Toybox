using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 개발 전용 실험(브랜치 feat/perf-research, docs/12-review-2026-10-22.md). 자원 탭이 느린 까닭과 모드 공법 비급이 만들어지지 않는 까닭을 잰다.
    // 배포 코드는 고치지 않는다. 자원 탭 Draw가 부르는 것과 같은 함수를 같은 인자로 불러 시간을 잰다.
    //   rperf tabs        편집기를 열고(닫혀 있으면) 탭마다 SwitchTab의 시간. open=<ms> <번호>:<제목>:<ms> …
    //   rperf parts       자원 탭 Draw의 조각별 시간(처음 부르면 카탈로그 만들기가 들어간다). 이름=<ms>(<수>) …
    //   rperf modroll [경지]   모드 공법마다 비급 값 뽑기(RollValuesForMod, 가방에 넣지 않는다). 요약을 돌려주고 전체는 .devremote\perf-modroll-<경지>.txt
    //   rperf modrows     기술 표 셋의 모드 행 덤프(이름 키·경지 범위·옵션 행 수·카탈로그 상태). 전체는 .devremote\perf-modrows.txt
    internal static class DevCommandsPerf
    {
        // 자원 탭의 표 열(ResourcesTab의 NameW·KindW와 글 크기). 바뀌면 같이 고친다.
        private const float NameW = 380, KindW = 160;
        private const float SkillDescW = 1050;   // 공법 탭 기술 추가 목록의 설명 칸(MartialTab)

        public static string Execute(string[] p)
        {
            if (p[0] != "rperf")
            {
                return null;
            }
            string what = p.Length > 1 ? p[1] : "";
            switch (what)
            {
                case "tabs":
                    return Tabs();
                case "parts":
                    return Parts();
                case "modroll":
                    return ModRoll(p.Length > 2 ? int.Parse(p[2]) : 0, p.Length > 3 && p[3] == "listed");
                case "modrows":
                    return ModRows();
                case "skillbuild":
                    return SkillBuild();
                case "dump":
                case "lt":
                case "confid":
                case "main":
                case "ltfail":
                case "query":
                case "preset":
                case "apl":
                case "ltscan":
                case "press":
                    return DevCommandsPerfProbes.Execute(p);
                default:
                    return "usage: rperf tabs | parts | modroll [grade] | modrows | skillbuild | dump <tag> | lt | confid";
            }
        }

        private static WorldUnitBase PlayerOrNull()
        {
            return g.world == null ? null : g.world.playerUnit;
        }

        private static string Tabs()
        {
            if (PlayerOrNull() == null)
            {
                return "not ready";
            }
            var sb = new StringBuilder();
            var watch = Stopwatch.StartNew();
            if (!EditorWindow.IsOpen)
            {
                EditorWindow.Toggle();
                sb.Append("open=").Append(watch.ElapsedMilliseconds).Append(' ');
            }
            Type type = typeof(EditorWindow);
            var tabs = (EditorTab[])type.GetField("tabs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var numbers = (int[])type.GetField("tabNumbers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            int before = (int)type.GetField("tab", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            for (int i = 0; i < tabs.Length; i++)
            {
                watch.Restart();
                EditorWindow.SwitchTab(i);
                sb.Append(numbers[i]).Append(':').Append(tabs[i].Title).Append(':').Append(watch.ElapsedMilliseconds).Append(' ');
            }
            EditorWindow.SwitchTab(before);
            return sb.ToString().TrimEnd();
        }

        private sealed class Timer
        {
            private readonly StringBuilder sb = new StringBuilder();
            private readonly Stopwatch watch = new Stopwatch();

            public void Start()
            {
                watch.Restart();
            }

            public void Stop(string name, int count)
            {
                sb.Append(name).Append('=').Append(watch.ElapsedMilliseconds).Append("ms(").Append(count).Append(") ");
            }

            public override string ToString()
            {
                return sb.ToString().TrimEnd();
            }
        }

        private static string Parts()
        {
            WorldUnitBase player = PlayerOrNull();
            if (player == null)
            {
                return "not ready";
            }
            var t = new Timer();

            t.Start();
            List<ItemCatalog.Entry> all = ItemCatalog.Select(new ItemCatalog.Filter(), null);
            t.Stop("items", all.Count);

            t.Start();
            int skills = 0, mods = 0, nameless = 0;
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    skills++;
                    if (e.IsMod)
                    {
                        mods++;
                    }
                    if (e.Nameless)
                    {
                        nameless++;
                    }
                }
            }
            t.Stop("skills", skills);
            t.Stop("mods", mods);
            t.Stop("nameless", nameless);

            t.Start();
            HashSet<int> owned = ItemCatalog.OwnedIds(player);
            t.Stop("owned", owned.Count);

            t.Start();
            Dictionary<int, int> books = MartialBookService.CountAllInBag(player);
            t.Stop("bagbooks", books.Count);

            t.Start();
            int grade = Math.Max(1, FateService.CurrentGrade(player));
            t.Stop("grade", grade);

            t.Start();
            Dictionary<string, int> groups;
            int[] unusedKinds;
            ItemCatalog.Counts(new ItemCatalog.Filter(), owned, null, out groups, out unusedKinds);
            t.Stop("groups", groups[ItemGroups.AllKey]);

            // 자원 탭 "전체" 그룹의 Measure: 줄마다 이름(380, 19)과 종류(160, 16)의 높이.
            for (int round = 1; round <= 2; round++)
            {
                t.Start();
                foreach (ItemCatalog.Entry e in all)
                {
                    Ui.TextHeight(e.Name, NameW, 19);
                    Ui.TextHeight(e.Kind == null ? "-" : e.Kind.Name, KindW, 16);
                }
                t.Stop("heights" + round, all.Count);
            }

            // 자원 탭이 그릴 때마다 세는 기술 서적 수(BookCount: 다섯 분류 × Find, 이 경지만 켜짐).
            t.Start();
            int bookCount = 0;
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    if (e.IsMod || SkillCatalog.BlockReason(e, grade) == null)
                    {
                        bookCount++;
                    }
                }
            }
            t.Stop("bookcount", bookCount);

            // 기술 서적 그룹을 열었을 때 모드 줄마다 부르는 BlockReason(SourceFor가 분류 전체를 훑는다).
            t.Start();
            int modBlocked = 0;
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    if (e.IsMod && SkillCatalog.BlockReason(e, grade) != null)
                    {
                        modBlocked++;
                    }
                }
            }
            t.Stop("modblock", modBlocked);

            t.Start();
            int favs = FavoriteStore.Entries.Count;
            t.Stop("favs", favs);

            // 공법 탭 기술 추가 목록의 Measure: 분류마다 설명(1050, 15)의 높이.
            for (int round = 1; round <= 2; round++)
            {
                t.Start();
                int n = 0;
                for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
                {
                    foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                    {
                        Ui.TextHeight(e.Desc, SkillDescW, 15);
                        n++;
                    }
                }
                t.Stop("skilldesc" + round, n);
            }
            return t.ToString();
        }

        // 모드 공법마다 RollValuesForMod를 부르고, 실패한 것은 한 번 더 같은 길을 밟아 어디서 막히는지 적는다.
        private static string ModRoll(int gradeArg, bool listed)
        {
            WorldUnitBase player = PlayerOrNull();
            if (player == null)
            {
                return "not ready";
            }
            int grade = gradeArg > 0 ? gradeArg : Math.Max(1, FateService.CurrentGrade(player));
            var lines = new List<string>();
            var causes = new Dictionary<string, int>();
            var summary = new StringBuilder();
            var total = Stopwatch.StartNew();
            long slowest = 0;
            int slowestId = 0;
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                int ok = 0, fail = 0;
                // listed: 목록에 남는 줄(숨기지 않았고 그 경지에서 만들어지는 것)만(묶음 2)
                foreach (SkillCatalog.Entry e in listed ? SkillCatalog.Listed(k, "") : SkillCatalog.Find(k, ""))
                {
                    if (!e.IsMod || (listed && !SkillCatalog.ModBookFits(e, grade)))
                    {
                        continue;
                    }
                    var watch = Stopwatch.StartNew();
                    string reason;
                    Il2CppStructArray<int> values = SkillCatalog.RollValuesForMod(k, e, grade, out reason);
                    long ms = watch.ElapsedMilliseconds;
                    if (ms > slowest)
                    {
                        slowest = ms;
                        slowestId = e.BaseId;
                    }
                    if (values != null)
                    {
                        ok++;
                        lines.Add("OK\t" + k + "\t" + e.BaseId + "\t" + e.Name + "\t" + ms + "ms");
                        continue;
                    }
                    fail++;
                    string cause = Diagnose(k, e, grade);
                    string key = cause.Split(' ')[0];
                    int c;
                    causes.TryGetValue(key, out c);
                    causes[key] = c + 1;
                    lines.Add("FAIL\t" + k + "\t" + e.BaseId + "\t" + e.Name + "\tnameless=" + e.Nameless + "\tch=" + e.NameFromCh
                        + "\tbas=" + e.BasType + "\t" + ms + "ms\t" + reason + "\t" + cause);
                }
                summary.Append(SkillCatalog.Kinds[k].Tab).Append(" ok=").Append(ok).Append(" fail=").Append(fail).Append(" | ");
            }
            string file = Path.Combine(DevCommands.Dir, "perf-modroll-" + grade + ".txt");
            File.WriteAllLines(file, lines.ToArray(), new UTF8Encoding(false));
            summary.Append("grade=").Append(grade).Append(" ms=").Append(total.ElapsedMilliseconds)
                .Append(" slowest=").Append(slowestId).Append(':').Append(slowest).Append("ms causes=");
            foreach (var pair in causes)
            {
                summary.Append(pair.Key).Append(':').Append(pair.Value).Append(',');
            }
            return summary.ToString().TrimEnd(',') + " file=" + file;
        }

        // RollValuesForMod와 같은 길을 한 번 밟아 처음 막힌 곳. RollValuesForMod의 순서가 바뀌면 같이 고친다.
        private static string Diagnose(int k, SkillCatalog.Entry mod, int grade)
        {
            SkillCatalog.Kind kind = SkillCatalog.Kinds[k];
            SkillCatalog.Entry source = SkillCatalog.SourceFor(k, mod, grade);
            if (source == null)
            {
                return "nosource";
            }
            Il2CppStructArray<int> rolled = SkillCatalog.RollValues(kind, source.BaseId, grade, 3000, null);
            if (rolled == null)
            {
                return "rollnull source=" + source.BaseId;
            }
            var values = new Il2CppStructArray<int>(rolled.Length);
            for (int i = 0; i < rolled.Length; i++)
            {
                values[i] = i == 0 ? mod.BaseId : rolled[i];
            }
            try
            {
                SkillCatalog.RemapPrefixNumbers(kind, rolled, values, mod.BaseId);
            }
            catch (Exception e)
            {
                return "remap " + e.GetType().Name + ": " + Short(e.Message);
            }
            DataProps.MartialData martial;
            try
            {
                martial = DataProps.PropsData.NewMartial(kind.Type, values).To<DataProps.MartialData>();
            }
            catch (Exception e)
            {
                return "newmartial " + e.GetType().Name + ": " + Short(e.Message) + " values=" + Join(values);
            }
            try
            {
                if (martial.baseID != mod.BaseId)
                {
                    return "baseid " + martial.baseID;
                }
                string name = martial.martialInfo.name;
                return string.IsNullOrEmpty(name) ? "emptyname values=" + Join(values) : "passed-on-retry name=" + name;
            }
            catch (Exception e)
            {
                return "info " + e.GetType().Name + ": " + Short(e.Message) + " values=" + Join(values);
            }
        }

        // 기술 카탈로그 만들기(3.4초, 회차 perf-1022b)를 나눈다: LocalText ch 사전, 분류마다 All, 표 행 수, 행마다 쓰는 함수의 합.
        // SkillCatalog.Reset()으로 비운 뒤 다시 만든다(TranslationService.Apply 뒤에 하는 것과 같다).
        private static string SkillBuild()
        {
            var t = new Timer();
            t.Start();
            int lt = g.conf.localText.allConfList.Count;
            t.Stop("ltcount", lt);
            SkillCatalog.Reset();
            t.Start();
            SkillCatalog.ChOf("");
            t.Stop("chdict", lt);
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                t.Start();
                int n = SkillCatalog.Find(k, "").Count;
                t.Stop("kind" + k, n);
            }
            t.Start();
            int attack = g.conf.battleSkillAttack._allConfList.Count;
            int step = g.conf.battleStepBase._allConfList.Count;
            int ability = g.conf.battleAbilityBase._allConfList.Count;
            t.Stop("rows", attack + step + ability);
            t.Stop("attackRows", attack);
            t.Stop("stepRows", step);
            t.Stop("abilityRows", ability);
            // 행마다의 비용: 기술 표의 이름·설명을 LS로 읽기, 설명 정리(CleanDesc).
            var all = g.conf.battleSkillAttack._allConfList;
            var names = new List<string>(all.Count);
            var descs = new List<string>(all.Count);
            t.Start();
            for (int i = 0; i < all.Count; i++)
            {
                names.Add(all[i].name);
                descs.Add(all[i].desc);
            }
            t.Stop("readKeys", all.Count);
            t.Start();
            var texts = new List<string>(all.Count);
            for (int i = 0; i < names.Count; i++)
            {
                GameTool.LS(names[i]);
                texts.Add(GameTool.LS(descs[i]));
            }
            t.Stop("ls2", names.Count);
            t.Start();
            foreach (string d in texts)
            {
                MartialService.CleanDesc(d);
            }
            t.Stop("cleanDesc", texts.Count);
            return t.ToString();
        }

        // 기술 표 셋의 모드 행(isModExtend)을 그대로 적는다: 이름 키와 풀린 글, 경지 범위, 옵션 행 수, 카탈로그의 이름 없음 여부.
        // 요약: 이름 없음 × 경지 범위 0~0, 옵션 행 수의 분포, 번호 앞자리별 수(어느 모드의 것인지 가늠). 전체는 .devremote\perf-modrows.txt
        private static string ModRows()
        {
            var lines = new List<string> { "kind\tid\ttype\tnameKey\tname\tch\tgradeMin\tgradeMax\tclassName\tbasType\tprefixRows\tcatalog\tdesc" };
            var buckets = new SortedDictionary<string, int>();
            var heads = new SortedDictionary<string, int>();
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                SkillCatalog.Kind kind = SkillCatalog.Kinds[k];
                var catalog = new Dictionary<int, SkillCatalog.Entry>();
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    catalog[e.BaseId] = e;
                }
                if (kind.Type == MartialType.Step)
                {
                    var all = g.conf.battleStepBase._allConfList;
                    for (int i = 0; i < all.Count; i++)
                    {
                        if (all[i].isModExtend)
                        {
                            Row(lines, buckets, heads, k, kind, catalog, all[i].id, 0, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, "", "");
                        }
                    }
                }
                else if (kind.Type == MartialType.Ability)
                {
                    var all = g.conf.battleAbilityBase._allConfList;
                    for (int i = 0; i < all.Count; i++)
                    {
                        if (all[i].isModExtend)
                        {
                            Row(lines, buckets, heads, k, kind, catalog, all[i].id, 0, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, "", "");
                        }
                    }
                }
                else
                {
                    var all = g.conf.battleSkillAttack._allConfList;
                    for (int i = 0; i < all.Count; i++)
                    {
                        int type = all[i].type;
                        bool match = kind.Type == MartialType.SkillLeft ? (type == 1 || type == 2)
                            : kind.Type == MartialType.SkillRight ? type == 3 : type == 4;
                        if (match && all[i].isModExtend)
                        {
                            Row(lines, buckets, heads, k, kind, catalog, all[i].id, type, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, all[i].className, all[i].basType);
                        }
                    }
                }
            }
            string file = Path.Combine(DevCommands.Dir, "perf-modrows.txt");
            File.WriteAllLines(file, lines.ToArray(), new UTF8Encoding(false));
            var sb = new StringBuilder("rows=" + (lines.Count - 1) + " | ");
            foreach (var pair in buckets)
            {
                sb.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
            }
            sb.Append("| heads ");
            foreach (var pair in heads)
            {
                sb.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
            }
            return sb.Append("file=").Append(file).ToString();
        }

        private static void Row(List<string> lines, SortedDictionary<string, int> buckets, SortedDictionary<string, int> heads, int k, SkillCatalog.Kind kind,
            Dictionary<int, SkillCatalog.Entry> catalog, int id, int type, string nameKey, string descKey, int gradeMin, int gradeMax, string className, string basType)
        {
            string name = GameTool.LS(nameKey);
            string ch = SkillCatalog.ChOf(nameKey);
            int prefixRows = -1;
            try
            {
                var items = g.conf.battleSkillPrefixValue.GetItems((int)kind.Type, id);
                prefixRows = items == null ? 0 : items.Count;
            }
            catch (Exception)
            {
                prefixRows = -2;
            }
            SkillCatalog.Entry entry;
            string state = !catalog.TryGetValue(id, out entry) ? "absent" : entry.Nameless ? "nameless" : "named";
            string desc = GameTool.LS(descKey) ?? "";
            desc = desc.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
            if (desc.Length > 40)
            {
                desc = desc.Substring(0, 40);
            }
            lines.Add(k + "\t" + id + "\t" + type + "\t" + nameKey + "\t" + Clean(name) + "\t" + Clean(ch) + "\t" + gradeMin + "\t" + gradeMax + "\t" + className + "\t" + basType
                + "\t" + prefixRows + "\t" + state + "\t" + desc);
            string bucket = "k" + k + "/" + state + "/g" + (gradeMax < 1 ? "0" : "n") + "/p" + (prefixRows <= 0 ? "0" : prefixRows < 9 ? "1-8" : "9+");
            int c;
            buckets.TryGetValue(bucket, out c);
            buckets[bucket] = c + 1;
            string head = id.ToString();
            head = head.Length > 4 ? head.Substring(0, head.Length - 4) + "xxxx" : head;
            heads.TryGetValue(head, out c);
            heads[head] = c + 1;
        }

        private static string Clean(string s)
        {
            return (s ?? "").Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
        }

        private static string Short(string s)
        {
            s = (s ?? "").Replace('\n', ' ').Replace('\r', ' ');
            return s.Length > 120 ? s.Substring(0, 120) : s;
        }

        private static string Join(Il2CppStructArray<int> values)
        {
            var parts = new List<string>();
            for (int i = 0; i < values.Length; i++)
            {
                parts.Add(values[i].ToString());
            }
            return string.Join(",", parts.ToArray());
        }
    }
}
