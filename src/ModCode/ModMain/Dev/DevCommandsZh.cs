using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 게임 설정 표 전체에서 아직 중국어(한자)가 남은 글을 찾아 파일로 적는다(05 문서 53절).
    //   rzh [표 이름|all] [최대 줄]   g.conf의 표마다 _allConfList의 줄을 돌며 글자 속성(string)에 한자가 든 것을 모은다.
    //                                 LocalText는 모드 행(isModExtend)의 kr만 본다(ch는 원문이라 뺀다).
    //                                 결과: .devremote\zh-<표 이름>.txt (표\t속성\tid\t글, 같은 표·속성·글은 한 번), 돌려주는 줄은 표별 수.
    //   rzhtext <원문…>              번역표(TextTranslationService.Replacer)로 그 글을 바꿔 본다. 띄어쓰기는 한 칸으로 이어 붙이고 "\n" 두 글자는 줄바꿈.
    //                                 돌려주는 줄: changed=<참/거짓> <결과>(줄바꿈은 다시 "\n").
    //   rzhstat                       번역표 적용 요약(file= rows= tables= cells=)과 화면 글 치환 통계(patched= calls= replaced= ms=).
    //   rzhleft                       화면 글 치환 뒤에도 한자가 남은 글(TextPatch.Leftover)을 .devremote\zh-left.txt에 한 줄씩(줄바꿈은 "\n") 적는다.
    internal static class DevCommandsZh
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "rzh":
                    return Scan(p.Length > 1 ? p[1] : "all", p.Length > 2 ? int.Parse(p[2]) : 0);
                case "rzhtext":
                    return Translate(p);
                case "rzhstat":
                    return TextTranslationService.LastSummary + " | " + TextPatch.Stats();
                case "rzhleft":
                    return Leftover();
                default:
                    return null;
            }
        }

        private static string Leftover()
        {
            var lines = new StringBuilder();
            foreach (string text in TextPatch.Leftover)
            {
                lines.Append(text.Replace("\r", "").Replace("\n", "\\n")).Append('\n');
            }
            string path = Path.Combine(DevCommands.Dir, "zh-left.txt");
            File.WriteAllText(path, lines.ToString(), Encoding.UTF8);
            return "left=" + TextPatch.Leftover.Count + " written=" + path;
        }

        private static string Translate(string[] p)
        {
            TextReplacer replacer = TextTranslationService.Replacer;
            if (replacer == null)
            {
                return "replacer=null";
            }
            string text = string.Join(" ", p, 1, p.Length - 1).Replace("\\n", "\n");
            bool changed;
            string result = replacer.Replace(text, out changed);
            return "changed=" + changed + " " + result.Replace("\r", "").Replace("\n", "\\n");
        }

        public static bool HasChinese(string text)
        {
            if (text == null)
            {
                return false;
            }
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF))
                {
                    return true;
                }
            }
            return false;
        }

        private static string Scan(string what, int limit)
        {
            var lines = new StringBuilder();
            var summary = new StringBuilder();
            var seen = new HashSet<string>();
            int tables = 0, hits = 0, written = 0;
            foreach (PropertyInfo table in typeof(ConfMgr).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (what != "all" && table.Name != what)
                {
                    continue;
                }
                object conf;
                try
                {
                    conf = table.GetValue(g.conf, null);
                }
                catch (Exception)
                {
                    continue;
                }
                if (conf == null)
                {
                    continue;
                }
                PropertyInfo listProperty = conf.GetType().GetProperty("_allConfList");
                object list = listProperty == null ? null : listProperty.GetValue(conf, null);
                if (list == null)
                {
                    continue;
                }
                int count = (int)list.GetType().GetProperty("Count").GetValue(list, null);
                PropertyInfo item = list.GetType().GetProperty("Item");
                PropertyInfo[] stringProperties = null;
                PropertyInfo idProperty = null, modProperty = null;
                int tableHits = 0;
                var perProperty = new SortedDictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < count; i++)
                {
                    object row;
                    try
                    {
                        row = item.GetValue(list, new object[] { i });
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    if (row == null)
                    {
                        continue;
                    }
                    if (stringProperties == null)
                    {
                        var found = new List<PropertyInfo>();
                        foreach (PropertyInfo property in row.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (property.PropertyType == typeof(string) && property.GetIndexParameters().Length == 0)
                            {
                                found.Add(property);
                            }
                        }
                        stringProperties = found.ToArray();
                        idProperty = row.GetType().GetProperty("id");
                        modProperty = row.GetType().GetProperty("isModExtend");
                    }
                    if (table.Name == "localText")
                    {
                        // 모드 행의 kr만. 원작 행과 ch(원문)는 뺀다.
                        object isMod = modProperty == null ? null : modProperty.GetValue(row, null);
                        if (!(isMod is bool) || !(bool)isMod)
                        {
                            continue;
                        }
                    }
                    string id = "";
                    try
                    {
                        object idValue = idProperty == null ? null : idProperty.GetValue(row, null);
                        id = idValue == null ? "" : idValue.ToString();
                    }
                    catch (Exception)
                    {
                    }
                    for (int k = 0; k < stringProperties.Length; k++)
                    {
                        if (table.Name == "localText" && stringProperties[k].Name != "kr")
                        {
                            continue;
                        }
                        string value;
                        try
                        {
                            value = stringProperties[k].GetValue(row, null) as string;
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        if (string.IsNullOrEmpty(value) || !HasChinese(value))
                        {
                            continue;
                        }
                        tableHits++;
                        hits++;
                        int n;
                        perProperty.TryGetValue(stringProperties[k].Name, out n);
                        perProperty[stringProperties[k].Name] = n + 1;
                        string key = table.Name + "\t" + stringProperties[k].Name + "\t" + value;
                        if (seen.Add(key) && (limit <= 0 || written < limit))
                        {
                            lines.Append(table.Name).Append('\t').Append(stringProperties[k].Name).Append('\t').Append(id).Append('\t')
                                .Append(value.Replace("\r", "").Replace("\n", "\\n")).Append('\n');
                            written++;
                        }
                    }
                }
                if (tableHits > 0)
                {
                    tables++;
                    summary.Append(table.Name).Append('=').Append(tableHits).Append('(');
                    bool first = true;
                    foreach (var pair in perProperty)
                    {
                        if (!first)
                        {
                            summary.Append(',');
                        }
                        first = false;
                        summary.Append(pair.Key).Append(':').Append(pair.Value);
                    }
                    summary.Append(");");
                }
            }
            string path = Path.Combine(DevCommands.Dir, "zh-" + what + ".txt");
            File.WriteAllText(path, lines.ToString(), Encoding.UTF8);
            return "tables=" + tables + " hits=" + hits + " unique=" + seen.Count + " written=" + written + " " + summary;
        }
    }
}
