using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 성능 1 묶음(docs/superpowers/plans/2026-10-23-perf-first-open.md)의 실험·비교 명령.
    //   rperf dump <꼬리>  기술·아이템 카탈로그 전체를 .devremote\perf-dump-<꼬리>-skills.txt·-items.txt로(고치기 전·뒤 바이트 비교용)
    //   rperf lt           allText.ContainsKey·인덱서로 찾은 ch가 SkillCatalog.ChOf와 같은지(4.1 (가)의 시험)
    //   rperf confid       g.conf.localText와 번역 대상 표 16개의 객체 주소, 모드 행 하나, 표마다 한글 칸 하나(.devremote\perf-confid-<n>.txt)
    internal static class DevCommandsPerfProbes
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public static string Execute(string[] p)
        {
            switch (p[1])
            {
                case "dump": return Dump(p.Length > 2 ? p[2] : "x");
                case "lt": return Lt();
                case "confid": return ConfId();
                case "query":
                    {
                        // rperf query <검색어|-> [그룹 키]: 자원 탭의 OnQuery(검색 칸 글자 하나마다 하는 일)를 불러 ms와 줄 수(묶음 3)
                        if (!EditorWindow.IsOpen)
                        {
                            EditorWindow.Toggle();
                        }
                        var tabs = (EditorTab[])typeof(EditorWindow).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                        int index = Array.FindIndex(tabs, t => t is ResourcesTab);
                        EditorWindow.SwitchTab(index);
                        ResourcesTab tab = (ResourcesTab)tabs[index];
                        if (p.Length > 3)
                        {
                            typeof(ResourcesTab).GetMethod("SetGroup", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tab, new object[] { p[3] });
                        }
                        string q = p.Length > 2 && p[2] != "-" ? p[2] : "";
                        var watch = Stopwatch.StartNew();
                        typeof(ResourcesTab).GetMethod("OnQuery", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tab, new object[] { q });
                        long ms = watch.ElapsedMilliseconds;
                        object list = typeof(ResourcesTab).GetField("list", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tab);
                        object count = list.GetType().GetProperty("Count") != null ? list.GetType().GetProperty("Count").GetValue(list, null) : "?";
                        return "query='" + q + "' ms=" + ms + " rows=" + count;
                    }
                case "preset":
                    {
                        // rperf preset: 프리셋 탭 그리기의 조각별 ms(묶음 3)
                        var sb = new StringBuilder();
                        var w = Stopwatch.StartNew();
                        var list = PresetService.ListCached();
                        sb.Append("list=").Append(w.ElapsedMilliseconds); w.Restart();
                        int changed = PresetService.DefaultsChangedCached();
                        sb.Append(" defaults=").Append(w.ElapsedMilliseconds); w.Restart();
                        var learned = PresetService.Snapshot();
                        sb.Append(" snapshot=").Append(w.ElapsedMilliseconds).Append('(').Append(learned.Count).Append(')'); w.Restart();
                        foreach (var pr in list) { if (pr.Error == null) { PresetService.AppliedCount(pr, learned); } }
                        sb.Append(" applied=").Append(w.ElapsedMilliseconds); w.Restart();
                        foreach (var pr in list) { if (pr.Error == null) { PresetService.ReplacesAll(pr); } }
                        sb.Append(" replacesAll=").Append(w.ElapsedMilliseconds); w.Restart();
                        foreach (var pr in list) { if (pr.Error == null) { PresetService.ReplacesFull(pr); } }
                        sb.Append(" replacesFull=").Append(w.ElapsedMilliseconds).Append(" presets=").Append(list.Count);
                        return sb.ToString();
                    }
                case "ltfail":
                    {
                        // Init의 번역 두 단계를 같은 순서로 다시 부르되 Scan이 첫 행에서 실패하게 한다. texts의 lt=가 그대로면 되돌아가기 길이 산다.
                        LocalTextIndex.DevFailNext = true;
                        string first;
                        try { first = TranslationService.Apply(); } catch (Exception e) { first = "translations failed: " + e.GetType().Name + ": " + e.Message; }
                        string second = TextTranslationService.Apply();
                        return first + " || " + second.Substring(0, Math.Min(200, second.Length));
                    }
                case "main":
                    // 주 화면으로 돌아간다: 게임 이야기 함수 "주 화면으로"(DramaFunction.ReturnMain)를 부른다. 다시 들어가기는 rc.ps1의 이름 클릭으로.
                    new DramaFunction().ReturnMain(new UnhollowerBaseLib.Il2CppStringArray(new[] { "returnMain" }));
                    return "returnMain called";
                default: return null;
            }
        }

        private static string Clean(string s)
        {
            return (s ?? "").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string Dump(string tag)
        {
            var skills = new StringBuilder();
            int skillLines = 0;
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    skills.Append(k).Append('\t').Append(e.BaseId).Append('\t').Append(Clean(e.Name)).Append('\t').Append(Clean(e.Desc)).Append('\t')
                        .Append(e.GradeMin).Append('\t').Append(e.GradeMax).Append('\t').Append(e.IsMod).Append('\t').Append(e.Nameless).Append('\t')
                        .Append(e.NameFromCh).Append('\t').Append(Clean(e.BasType)).Append('\t').Append(Clean(e.Search)).Append('\n');
                    skillLines++;
                }
            }
            var items = new StringBuilder();
            int itemLines = 0;
            foreach (ItemCatalog.Entry e in ItemCatalog.Select(new ItemCatalog.Filter(), null))
            {
                items.Append(e.Id).Append('\t').Append(Clean(e.Name)).Append('\t').Append(Clean(e.Desc)).Append('\t').Append(e.Level).Append('\t')
                    .Append(e.Type).Append('\t').Append(e.SubType).Append('\t').Append(Clean(e.TypeName)).Append('\t').Append(Clean(e.SubTypeName)).Append('\t')
                    .Append(e.Group == null ? "-" : e.Group.Key).Append('\t').Append(e.Kind == null ? -1 : e.Kind.Order).Append('\t').Append(Clean(e.Search)).Append('\n');
                itemLines++;
            }
            foreach (ItemCatalog.Entry e in ItemCatalog.Currencies)
            {
                items.Append("cur\t").Append(e.Id).Append('\t').Append(Clean(e.Name)).Append('\t').Append(Clean(e.Desc)).Append('\t').Append(e.Level).Append('\n');
                itemLines++;
            }
            File.WriteAllText(Path.Combine(DevCommands.Dir, "perf-dump-" + tag + "-skills.txt"), skills.ToString(), Utf8);
            File.WriteAllText(Path.Combine(DevCommands.Dir, "perf-dump-" + tag + "-items.txt"), items.ToString(), Utf8);
            return "dump " + tag + " skills=" + skillLines + " items=" + itemLines;
        }

        // 카탈로그에 든 기술의 이름 키(세 표의 name 칸).
        private static List<string> CatalogNameKeys()
        {
            var ids = new HashSet<long>();
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    ids.Add(((long)SkillCatalog.Kinds[k].Type << 40) + e.BaseId);
                }
            }
            var keys = new List<string>();
            var attack = g.conf.battleSkillAttack._allConfList;
            for (int i = 0; i < attack.Count; i++)
            {
                int type = attack[i].type;
                MartialType kind = type == 1 || type == 2 ? MartialType.SkillLeft : type == 3 ? MartialType.SkillRight : MartialType.Ultimate;
                if (ids.Contains(((long)kind << 40) + attack[i].id)) { keys.Add(attack[i].name); }
            }
            var step = g.conf.battleStepBase._allConfList;
            for (int i = 0; i < step.Count; i++)
            {
                if (ids.Contains(((long)MartialType.Step << 40) + step[i].id)) { keys.Add(step[i].name); }
            }
            var ability = g.conf.battleAbilityBase._allConfList;
            for (int i = 0; i < ability.Count; i++)
            {
                if (ids.Contains(((long)MartialType.Ability << 40) + ability[i].id)) { keys.Add(ability[i].name); }
            }
            return keys;
        }

        private static string Lt()
        {
            List<string> keys = CatalogNameKeys();
            var expected = new List<string>(keys.Count);
            foreach (string key in keys)
            {
                expected.Add(SkillCatalog.ChOf(key));   // 지금의 훑기 사전(먼저 만들어 둔다. 시간 재기에서 뺀다)
            }
            int ok = 0, diff = 0, missing = 0;
            string ex = "none";
            var diffs = new StringBuilder();
            var dict = g.conf.localText.allText;
            var watch = Stopwatch.StartNew();
            try
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    string key = keys[i];
                    string got = "";
                    if (key != null && dict.ContainsKey(key))
                    {
                        ConfLocalTextItem item = dict[key];
                        got = item == null ? "" : item.ch ?? "";
                    }
                    else
                    {
                        missing++;
                    }
                    if (got == expected[i])
                    {
                        ok++;
                    }
                    else
                    {
                        diff++;
                        diffs.Append(key).Append('\t').Append(Clean(expected[i])).Append('\t').Append(Clean(got)).Append('\n');
                    }
                }
            }
            catch (Exception e)
            {
                ex = e.GetType().Name + ": " + e.Message.Split('\n')[0];
            }
            long ms = watch.ElapsedMilliseconds;
            File.WriteAllText(Path.Combine(DevCommands.Dir, "perf-lt-diff.txt"), diffs.ToString(), Utf8);
            return "keys=" + keys.Count + " ok=" + ok + " diff=" + diff + " missing=" + missing + " ex=" + ex + " ms=" + ms;
        }

        private static bool HasHangul(string s)
        {
            foreach (char c in s)
            {
                if (c >= '\uAC00' && c <= '\uD7A3') { return true; }
            }
            return false;
        }

        private static string ConfId()
        {
            var sb = new StringBuilder();
            sb.Append("localText\t").Append(g.conf.localText.Pointer.ToString("X")).Append('\n');
            var lt = g.conf.localText.allConfList;
            for (int i = 0; i < lt.Count; i++)
            {
                if (lt[i] != null && lt[i].isModExtend)
                {
                    sb.Append("modrow\t").Append(lt[i].key).Append('\t').Append(Clean(lt[i].kr)).Append('\n');
                    break;
                }
            }
            foreach (string[] target in TextTranslationService.Targets)
            {
                PropertyInfo table = typeof(ConfMgr).GetProperty(target[0]);
                object conf = table == null ? null : table.GetValue(g.conf, null);
                if (conf == null)
                {
                    sb.Append(target[0]).Append("\tnone\n");
                    continue;
                }
                var baseObj = conf as UnhollowerBaseLib.Il2CppObjectBase;
                sb.Append(target[0]).Append('\t').Append(baseObj == null ? "?" : baseObj.Pointer.ToString("X"));
                PropertyInfo listProperty = conf.GetType().GetProperty("_allConfList") ?? conf.GetType().GetProperty("allConfList");
                object list = listProperty == null ? null : listProperty.GetValue(conf, null);
                string sample = "-";
                if (list != null)
                {
                    int count = (int)list.GetType().GetProperty("Count").GetValue(list, null);
                    PropertyInfo item = list.GetType().GetProperty("Item");
                    for (int i = 0; i < count && sample == "-"; i++)
                    {
                        object row = item.GetValue(list, new object[] { i });
                        for (int k = 1; row != null && k < target.Length && sample == "-"; k++)
                        {
                            PropertyInfo f = row.GetType().GetProperty(target[k]);
                            string v = f == null ? null : f.GetValue(row, null) as string;
                            if (!string.IsNullOrEmpty(v) && HasHangul(v))
                            {
                                sample = i + ":" + target[k] + ":" + Clean(v);
                            }
                        }
                    }
                }
                sb.Append('\t').Append(sample).Append('\n');
            }
            int n = 1;
            while (File.Exists(Path.Combine(DevCommands.Dir, "perf-confid-" + n + ".txt")))
            {
                n++;
            }
            string path = Path.Combine(DevCommands.Dir, "perf-confid-" + n + ".txt");
            File.WriteAllText(path, sb.ToString(), Utf8);
            return "confid " + n + " written";
        }
    }
}
