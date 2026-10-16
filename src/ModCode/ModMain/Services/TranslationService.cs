using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 창작마당 모드의 글(LocalText의 isModExtend 행) 한글화. 설계: docs/superpowers/specs/2026-10-16-mod-korean-design.md 3절.
    // 번역 파일 <데이터 폴더>\translations\mods.txt(키<TAB>한국어). 없으면 DLL 안의 것을 풀어 둔다(있으면 건드리지 않는다. 사용자가 고친 것을 지킨다).
    // Init마다 Apply()가 모드 행의 kr을 덮는다: 사전에 있으면 번역, 없고 kr이 "0"이거나 비었으면 ch(중국어). 기본 행은 건드리지 않는다.
    internal static class TranslationService
    {
        private const string ResourceName = "Translations/mods.txt";

        public static string LastSummary = "not applied";
        private static HashSet<string> lastKeys = new HashSet<string>();   // 마지막 적용 때 사전에 있던 키(rloc dump가 쓴다)

        public static string Dir
        {
            get { return Path.Combine(ChangeLog.DataDir, "translations"); }
        }

        public static string FilePath
        {
            get { return Path.Combine(Dir, "mods.txt"); }
        }

        public static string UntranslatedPath
        {
            get { return Path.Combine(Dir, "untranslated.txt"); }
        }

        // 파일이 없으면 내장본을 풀어 둔다("풀어 둠"). 있으면 내장본에만 있는 키의 줄을 끝에 덧붙인다("덧붙임 n". 사용자가 고친 줄은 그대로).
        // 덧붙일 것이 없으면 "있음", 내장본도 없으면 "없음". 46절.
        public static string EnsureFile()
        {
            string text = Embedded();
            if (File.Exists(FilePath))
            {
                if (text == null)
                {
                    return "있음";
                }
                int added = AppendMissing(text);
                return added > 0 ? "덧붙임 " + added : "있음";
            }
            if (text == null)
            {
                return "없음";
            }
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, text, new UTF8Encoding(false));
            return "풀어 둠";
        }

        // 내장본의 줄 가운데 파일에 없는 키의 줄을 파일 끝에 덧붙인다(앞에 날짜 주석 한 줄). 돌려주는 것은 덧붙인 줄 수.
        private static int AppendMissing(string embedded)
        {
            var have = new HashSet<string>();
            foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                string key = KeyOf(raw);
                if (key != null)
                {
                    have.Add(key);
                }
            }
            var sb = new StringBuilder();
            int added = 0;
            foreach (string raw in embedded.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                string key = KeyOf(line);
                if (key == null || have.Contains(key))
                {
                    continue;
                }
                sb.Append(line).Append('\n');
                have.Add(key);
                added++;
            }
            if (added == 0)
            {
                return 0;
            }
            string current = File.ReadAllText(FilePath, Encoding.UTF8);
            var all = new StringBuilder(current);
            if (current.Length > 0 && !current.EndsWith("\n"))
            {
                all.Append('\n');
            }
            all.Append("# 덧붙임 ").Append(DateTime.Now.ToString("yyyy-MM-dd")).Append(": 내장본의 새 키 ").Append(added).Append('\n');
            all.Append(sb);
            File.WriteAllText(FilePath, all.ToString(), new UTF8Encoding(false));
            return added;
        }

        // 줄의 키. '#' 줄, 빈 줄, 탭이 없는 줄, 빈 키는 null(Load와 같은 규칙).
        private static string KeyOf(string line)
        {
            string t = line.TrimEnd('\r');
            if (t.Trim().Length == 0 || t.TrimStart().StartsWith("#"))
            {
                return null;
            }
            int tab = t.IndexOf('\t');
            if (tab < 0)
            {
                return null;
            }
            string key = t.Substring(0, tab).Trim();
            return key.Length == 0 ? null : key;
        }

        private static string Embedded()
        {
            Assembly assembly = typeof(TranslationService).Assembly;
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

        // 파일 → 사전. '#' 줄과 빈 줄은 무시, 탭이 없는 줄은 broken으로 센다. 키·글의 앞뒤 공백을 지우고 "\n"을 줄바꿈으로. 같은 키는 뒤의 것.
        public static Dictionary<string, string> Load(out int broken)
        {
            var map = new Dictionary<string, string>();
            broken = 0;
            foreach (string raw in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                string line = raw.TrimEnd('\r');
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#"))
                {
                    continue;
                }
                int tab = line.IndexOf('\t');
                string key = tab < 0 ? "" : line.Substring(0, tab).Trim();
                if (key.Length == 0)
                {
                    broken++;
                    continue;
                }
                map[key] = line.Substring(tab + 1).Trim().Replace("\\n", "\n");
            }
            return map;
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
                    map = Load(out broken);
                }
            }
            catch (Exception e)
            {
                file = "읽기 실패 " + e.GetType().Name + ": " + e.Message;
                map = new Dictionary<string, string>();
            }
            int applied = 0, fallback = 0;
            var seen = new HashSet<string>();
            var all = g.conf.localText.allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfLocalTextItem item = all[i];
                if (item == null || !item.isModExtend || item.key == null)
                {
                    continue;
                }
                string text;
                if (map.TryGetValue(item.key, out text))
                {
                    item.kr = text;
                    seen.Add(item.key);
                    applied++;
                }
                else if ((string.IsNullOrEmpty(item.kr) || item.kr == "0") && !string.IsNullOrEmpty(item.ch))
                {
                    item.kr = item.ch;
                    fallback++;
                }
            }
            int unused = 0;
            foreach (string key in map.Keys)
            {
                if (!seen.Contains(key))
                {
                    unused++;
                }
            }
            lastKeys = new HashSet<string>(map.Keys);
            SkillCatalog.Reset();
            LastSummary = "file=" + file + " rows=" + map.Count + " applied=" + applied + " fallback=" + fallback + " unused=" + unused + " broken=" + broken;
            MelonLogger.Msg("[TlToybox] translations: " + LastSummary);
            return LastSummary;
        }

        // 사전에 없는 모드 행(ch가 있는 것)을 "키<TAB><TAB>중국어"로 쓴다. 다음 작업이 번역해 mods.txt에 합친다.
        public static int DumpUntranslated()
        {
            var sb = new StringBuilder();
            int written = 0;
            var all = g.conf.localText.allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfLocalTextItem item = all[i];
                if (item == null || !item.isModExtend || item.key == null || lastKeys.Contains(item.key) || string.IsNullOrEmpty(item.ch))
                {
                    continue;
                }
                sb.Append(item.key).Append("\t\t").Append(item.ch.Replace("\r", "").Replace("\n", "\\n")).Append('\n');
                written++;
            }
            Directory.CreateDirectory(Dir);
            File.WriteAllText(UntranslatedPath, sb.ToString(), new UTF8Encoding(false));
            return written;
        }

        public static string Describe(string key)
        {
            var all = g.conf.localText.allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfLocalTextItem item = all[i];
                if (item != null && item.key == key)
                {
                    return "ls=" + GameTool.LS(key) + " kr=" + item.kr + " ch=" + item.ch;
                }
            }
            return "ls=" + GameTool.LS(key) + " row=none";
        }
    }
}
