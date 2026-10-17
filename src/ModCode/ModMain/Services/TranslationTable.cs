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
            if (t.Trim().Length == 0 || t.TrimStart().StartsWith("#", System.StringComparison.Ordinal))
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

        // 줄들 → 사전. '#' 줄과 빈 줄은 무시, 탭이 없는 줄과 키가 빈 줄은 broken. 키·글의 앞뒤 공백을 지우고 "\n"을 줄바꿈으로(키도. texts.txt의 라벨 원문에 줄바꿈이 든다). 같은 키는 뒤의 것.
        public static Dictionary<string, string> Parse(IEnumerable<string> lines, out int broken)
        {
            var map = new Dictionary<string, string>();
            broken = 0;
            foreach (string raw in lines)
            {
                string line = (raw ?? "").TrimEnd('\r');
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#", System.StringComparison.Ordinal))
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
                map[Unescape(key)] = Unescape(line.Substring(tab + 1).Trim());
            }
            return map;
        }

        // "\n"은 줄바꿈, "\_"는 공백(앞뒤 공백은 지우므로, 조각 번역의 끝 공백을 적는 길. texts.txt).
        private static string Unescape(string text)
        {
            return text.Replace("\\n", "\n").Replace("\\_", " ");
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
