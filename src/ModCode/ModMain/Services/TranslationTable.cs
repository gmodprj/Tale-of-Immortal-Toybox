using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 번역표(키<TAB>한국어)의 순수 규칙. 파일·게임을 모르므로 tests/ModMain.Tests가 그대로 링크한다(05 문서 49절). TranslationService가 쓴다.
    internal static class TranslationTable
    {
        // 줄의 키. '#' 줄, 빈 줄, 탭이 없는 줄, 빈 키는 null.
        public static string KeyOf(string line)
        {
            string t = (line ?? "").TrimEnd('\r');
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

        // 줄들 → 사전. '#' 줄과 빈 줄은 무시, 탭이 없는 줄과 키가 빈 줄은 broken. 키·글의 앞뒤 공백을 지우고 "\n"을 줄바꿈으로. 같은 키는 뒤의 것.
        public static Dictionary<string, string> Parse(IEnumerable<string> lines, out int broken)
        {
            var map = new Dictionary<string, string>();
            broken = 0;
            foreach (string raw in lines)
            {
                string line = (raw ?? "").TrimEnd('\r');
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

        // 내장본(embedded, 줄바꿈 구분)의 줄 가운데 파일(fileLines)에 없는 키의 줄. 같은 키가 내장본에 두 번이면 앞의 것만.
        public static List<string> MissingLines(IEnumerable<string> fileLines, string embedded)
        {
            var have = new HashSet<string>();
            foreach (string raw in fileLines)
            {
                string key = KeyOf(raw);
                if (key != null)
                {
                    have.Add(key);
                }
            }
            var result = new List<string>();
            foreach (string raw in (embedded ?? "").Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                string key = KeyOf(line);
                if (key == null || have.Contains(key))
                {
                    continue;
                }
                result.Add(line);
                have.Add(key);
            }
            return result;
        }
    }
}
