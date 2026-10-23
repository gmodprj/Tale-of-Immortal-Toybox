using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // config.txt(키=값 줄)의 읽기·쓰기 규칙. 단축키·창 위치·신기 단축키가 각자 갖던 것을 모았다(10 문서 R2, 묶음 5).
    // 이 파일은 게임·로그 없이 단위 시험한다. 파일 입출력은 ConfigFile.Io.cs.
    internal static partial class ConfigFile
    {
        // 줄 → 사전. '#'로 시작하는 줄, 빈 줄, '='가 없는 줄은 건너뛴다. 키는 대소문자를 가리지 않고, 키·값의 앞뒤 공백을 지운다. 같은 키는 뒤의 줄.
        public static Dictionary<string, string> Parse(IEnumerable<string> lines)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in lines)
            {
                string key, value;
                if (Split(line, out key, out value))
                {
                    map[key] = value;
                }
            }
            return map;
        }

        // 바꿀 키의 줄을 지우고(대소문자 무시) 끝에 "키=값"으로 덧붙인다. 다른 줄(주석·깨진 줄 포함)은 순서대로 그대로 둔다.
        public static List<string> Replace(IEnumerable<string> lines, IDictionary<string, string> values)
        {
            var keys = new HashSet<string>(values.Keys, StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (string line in lines)
            {
                string key, value;
                if (Split(line, out key, out value) && keys.Contains(key))
                {
                    continue;
                }
                result.Add(line);
            }
            foreach (var pair in values)
            {
                result.Add(pair.Key + "=" + pair.Value);
            }
            return result;
        }

        private static bool Split(string line, out string key, out string value)
        {
            key = null;
            value = null;
            string t = (line ?? "").Trim();
            if (t.Length == 0 || t.StartsWith("#"))
            {
                return false;
            }
            int eq = t.IndexOf('=');
            if (eq <= 0)
            {
                return false;
            }
            key = t.Substring(0, eq).Trim();
            value = t.Substring(eq + 1).Trim();
            return key.Length > 0;
        }
    }
}
