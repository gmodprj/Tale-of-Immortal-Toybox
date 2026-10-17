using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // LocalText 밖에 남은 창작마당 모드의 중국어 글 한글화(스펙 docs/superpowers/specs/2026-10-17-zh-text-korean-design.md).
    // 번역표 <데이터 폴더>\translations\texts.txt(원문<TAB>한국어. 파일 규칙은 mods.txt와 같다: 없으면 내장본을 풀고, 있으면 내장본에만 있는 줄을 덧붙인다).
    // Apply(): 사전으로 TextReplacer를 만들고(화면 글 치환 TextPatch가 쓴다), 다섯 표의 글 칸을 전체 일치로 바꿔 끼운다.
    internal static class TextTranslationService
    {
        private const string ResourceName = "Translations/texts.txt";

        // 표 이름(ConfMgr의 속성)과 바꿔 끼울 글자 속성. 53절의 조사에서 화면에 보이는 칸만.
        // modNpcAddWorld.joinSchoolName은 종문을 이름으로 찾는 값일 수 있어(53절, 표시 여부 미확인) 건드리지 않는다.
        private static readonly string[][] Targets =
        {
            new[] { "battleSkillAttack", "name", "desc" },
            new[] { "battleEffect", "desc" },
            new[] { "roleCreateFeature", "name", "tips" },
            new[] { "modNpcAddWorld", "name1", "name2" },
            new[] { "dramaOptions", "text" },
            new[] { "dramaDialogue", "dialogue" },
        };

        public static string LastSummary = "not applied";
        public static TextReplacer Replacer { get; private set; }

        public static string FilePath
        {
            get { return Path.Combine(TranslationService.Dir, "texts.txt"); }
        }

        public static string EnsureFile()
        {
            string text = Embedded();
            if (File.Exists(FilePath))
            {
                if (text == null)
                {
                    return "있음";
                }
                List<string> missing = TranslationTable.MissingLines(File.ReadAllLines(FilePath, Encoding.UTF8), text);
                if (missing.Count == 0)
                {
                    return "있음";
                }
                string current = File.ReadAllText(FilePath, Encoding.UTF8);
                var all = new StringBuilder(current);
                if (current.Length > 0 && !current.EndsWith("\n"))
                {
                    all.Append('\n');
                }
                all.Append("# 덧붙임 ").Append(DateTime.Now.ToString("yyyy-MM-dd")).Append(": 내장본의 새 원문 ").Append(missing.Count).Append('\n');
                foreach (string line in missing)
                {
                    all.Append(line).Append('\n');
                }
                File.WriteAllText(FilePath, all.ToString(), new UTF8Encoding(false));
                return "덧붙임 " + missing.Count;
            }
            if (text == null)
            {
                return "없음";
            }
            Directory.CreateDirectory(TranslationService.Dir);
            File.WriteAllText(FilePath, text, new UTF8Encoding(false));
            return "풀어 둠";
        }

        private static string Embedded()
        {
            Assembly assembly = typeof(TextTranslationService).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                {
                    return null;
                }
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static string Apply()
        {
            string file;
            var map = new Dictionary<string, string>();
            int broken = 0;
            try
            {
                file = EnsureFile();
                if (file != "없음")
                {
                    map = TranslationTable.Parse(File.ReadAllLines(FilePath, Encoding.UTF8), out broken);
                }
            }
            catch (Exception e)
            {
                file = "읽기 실패 " + e.GetType().Name + ": " + e.Message;
                map = new Dictionary<string, string>();
            }
            // LocalText의 원문(ch)→한국어(kr). 모드가 kr 대신 ch를 그대로 화면에 낼 때(회차 zh-1017c: 불러오기 화면의 팁·"下一条提示") 글 전체가 같으면 게임의 한국어를 쓴다.
            Dictionary<string, string> localText;
            string lt;
            try
            {
                localText = LocalTextMap();
                lt = localText.Count.ToString();
            }
            catch (Exception e)
            {
                localText = null;
                lt = "ERROR(" + e.GetType().Name + ")";
            }
            Replacer = new TextReplacer(map, localText);
            int tables = 0, cells = 0, errors = 0;
            var detail = new StringBuilder();
            if (Replacer.Count > 0)
            {
                foreach (string[] target in Targets)
                {
                    try
                    {
                        int n = ApplyTable(target);
                        if (n >= 0)
                        {
                            tables++;
                            cells += n;
                            detail.Append(' ').Append(target[0]).Append('=').Append(n);
                        }
                    }
                    catch (Exception e)
                    {
                        errors++;
                        detail.Append(' ').Append(target[0]).Append("=ERROR(").Append(e.GetType().Name).Append(": ").Append(e.Message).Append(')');
                    }
                }
            }
            LastSummary = "file=" + file + " rows=" + map.Count + " broken=" + broken + " lt=" + lt + " tables=" + tables + " cells=" + cells + (errors > 0 ? " errors=" + errors : "") + detail;
            MelonLogger.Msg("[TlToybox] texts: " + LastSummary);
            return LastSummary;
        }

        // LocalText 행 가운데 ch에 한자가 있고 kr이 한국어(비어 있지 않고, ch와 다르고, 한자가 없음)인 것의 ch→kr. 같은 ch는 앞의 행.
        // TranslationService.Apply 뒤에 부르므로 모드 행의 kr은 mods.txt의 한국어다.
        private static Dictionary<string, string> LocalTextMap()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var all = g.conf.localText.allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfLocalTextItem item = all[i];
                if (item == null)
                {
                    continue;
                }
                string ch = item.ch;
                string kr = item.kr;
                if (string.IsNullOrEmpty(ch) || string.IsNullOrEmpty(kr) || kr == ch || kr == "0" || !TextReplacer.HasChinese(ch) || TextReplacer.HasChinese(kr) || result.ContainsKey(ch))
                {
                    continue;
                }
                result[ch] = kr;
            }
            return result;
        }

        // 표 하나의 대상 칸을 바꿔 끼운다. 표가 없으면 -1, 있으면 바꾼 칸 수. 글에 한자가 있고 사전에 원문 전체와 같은 줄이 있을 때만(부분 치환 없음).
        private static int ApplyTable(string[] target)
        {
            PropertyInfo table = typeof(ConfMgr).GetProperty(target[0]);
            object conf = table == null ? null : table.GetValue(g.conf, null);
            if (conf == null)
            {
                return -1;
            }
            // _allConfList가 List<T>다. allConfList는 localText 말고는 IReadOnlyList<T> 래퍼라 Count·Item을 반사로 못 찾는다(회차 zh-1017b에서 NullReferenceException).
            PropertyInfo listProperty = conf.GetType().GetProperty("_allConfList") ?? conf.GetType().GetProperty("allConfList");
            object list = listProperty == null ? null : listProperty.GetValue(conf, null);
            if (list == null)
            {
                return -1;
            }
            int count = (int)list.GetType().GetProperty("Count").GetValue(list, null);
            PropertyInfo item = list.GetType().GetProperty("Item");
            PropertyInfo[] fields = null;
            int changed = 0;
            for (int i = 0; i < count; i++)
            {
                object row = item.GetValue(list, new object[] { i });
                if (row == null)
                {
                    continue;
                }
                if (fields == null)
                {
                    fields = new PropertyInfo[target.Length - 1];
                    for (int k = 1; k < target.Length; k++)
                    {
                        fields[k - 1] = row.GetType().GetProperty(target[k]);
                    }
                }
                for (int k = 0; k < fields.Length; k++)
                {
                    if (fields[k] == null || !fields[k].CanWrite)
                    {
                        continue;
                    }
                    string value = fields[k].GetValue(row, null) as string;
                    if (string.IsNullOrEmpty(value) || !TextReplacer.HasChinese(value))
                    {
                        continue;
                    }
                    string korean = Replacer.Whole(value);
                    if (korean != null && korean != value)
                    {
                        fields[k].SetValue(row, korean, null);
                        changed++;
                    }
                }
            }
            return changed;
        }
    }
}
