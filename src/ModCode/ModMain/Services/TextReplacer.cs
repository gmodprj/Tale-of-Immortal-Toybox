using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 원문 기반 치환의 순수 규칙(스펙 docs/superpowers/specs/2026-10-17-zh-text-korean-design.md 3.3절). 게임을 모르므로 tests/ModMain.Tests가 링크한다.
    // 사전(원문→한국어)으로 (1) 글 전체가 원문과 같으면 그 한국어, (2) 아니면 길이 2 이상의 원문을 긴 것부터 부분 치환한다. 한자가 없는 글은 그대로 돌려준다.
    // 부분 치환은 한자가 하나도 남지 않을 때만 쓴다. 남으면 원문 그대로(반쯤 바뀐 글을 보이지 않기 위해. 남은 글은 TextPatch.Leftover가 모은다).
    // 전체 일치 전용 사전(wholeOnly. LocalText의 ch→kr)은 글 전체가 같을 때만 쓰고 부분 치환에는 쓰지 않는다(5만 줄이라 느리고, 짧은 원문이 엉뚱한 데 끼어들므로).
    internal sealed class TextReplacer
    {
        private readonly Dictionary<string, string> whole;
        private readonly Dictionary<string, string> wholeOnly;
        private readonly KeyValuePair<string, string>[] partial;   // 길이 내림차순, 같은 길이는 서수 오름차순

        public TextReplacer(Dictionary<string, string> map) : this(map, null)
        {
        }

        public TextReplacer(Dictionary<string, string> map, Dictionary<string, string> wholeOnlyMap)
        {
            wholeOnly = wholeOnlyMap ?? new Dictionary<string, string>(StringComparer.Ordinal);
            whole = new Dictionary<string, string>(StringComparer.Ordinal);
            var list = new List<KeyValuePair<string, string>>();
            if (map != null)
            {
                foreach (var pair in map)
                {
                    if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                    {
                        continue;
                    }
                    Add(list, pair.Key, pair.Value);
                    if (pair.Key.IndexOf('\n') >= 0)
                    {
                        // 모드 DLL의 글은 줄바꿈을 두 글자 "\n"(TMP가 줄을 바꾼다. 회차 zh-1017e)이나 CRLF로 넣기도 한다. 그 꼴도 같은 번역으로.
                        Add(list, pair.Key.Replace("\n", "\\n"), pair.Value.Replace("\n", "\\n"));
                        Add(list, pair.Key.Replace("\n", "\r\n"), pair.Value.Replace("\n", "\r\n"));
                    }
                }
            }
            list.Sort((a, b) => a.Key.Length != b.Key.Length ? b.Key.Length.CompareTo(a.Key.Length) : string.CompareOrdinal(a.Key, b.Key));
            partial = list.ToArray();
        }

        private void Add(List<KeyValuePair<string, string>> list, string key, string value)
        {
            if (whole.ContainsKey(key))
            {
                return;
            }
            whole[key] = value;
            if (key.Length >= 2)
            {
                list.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        // 번역표의 원문 수(두 글자 "\n" 꼴로 덧붙인 것 포함).
        public int Count
        {
            get { return whole.Count; }
        }

        public int WholeOnlyCount
        {
            get { return wholeOnly.Count; }
        }

        // 한자(CJK 통합 한자 본구역·확장 A)가 하나라도 있는지. 한글·가나·전각 기호만 있으면 거짓.
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

        // 글 전체가 원문과 같을 때만 한국어를, 아니면 null을 돌려준다(부분 치환 없음). 표 칸 바꿔 끼우기가 쓴다.
        public string Whole(string text)
        {
            string found;
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            return whole.TryGetValue(text, out found) || wholeOnly.TryGetValue(text, out found) ? found : null;
        }

        public string Replace(string text, out bool changed)
        {
            changed = false;
            if (string.IsNullOrEmpty(text) || (whole.Count == 0 && wholeOnly.Count == 0) || !HasChinese(text))
            {
                return text;
            }
            string found = Whole(text);
            if (found != null)
            {
                changed = found != text;
                return found;
            }
            string result = text;
            for (int i = 0; i < partial.Length; i++)
            {
                if (result.IndexOf(partial[i].Key, StringComparison.Ordinal) >= 0)
                {
                    result = result.Replace(partial[i].Key, partial[i].Value);
                }
            }
            changed = !ReferenceEquals(result, text) && result != text && !HasChinese(result);
            return changed ? result : text;
        }
    }
}
