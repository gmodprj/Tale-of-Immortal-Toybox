using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MOD_sWP6UL
{
    // 작은 JSON 읽기. 내보내기 파일을 읽는 데 필요한 만큼만 한다(객체, 배열, 문자열, 수, true/false/null).
    // 외부 라이브러리를 쓰지 않는 이유: 모드의 산출물은 DLL 하나여야 한다.
    // 값의 형식: 객체는 Dictionary<string, object>, 배열은 List<object>, 문자열은 string, 정수는 long, 소수는 double, 불은 bool.
    // 문법이 틀리면 FormatException을 던진다.
    internal static class Json
    {
        public static object Parse(string text)
        {
            if (text == null)
            {
                throw new FormatException("빈 내용");
            }
            int i = 0;
            object value = Value(text, ref i, 0);
            SkipSpace(text, ref i);
            if (i != text.Length)
            {
                throw new FormatException(i + "번째 글자: 값 뒤에 다른 글자가 있습니다");
            }
            return value;
        }

        // 문자열을 JSON의 따옴표 글로 만든다.
        public static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        private static void SkipSpace(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r' || s[i] == '\uFEFF'))
            {
                i++;
            }
        }

        private static FormatException Bad(int i, string what)
        {
            return new FormatException(i + "번째 글자: " + what);
        }

        // 겹침의 한도. 내보내기 파일은 세 겹이면 된다. 한도가 없으면 깊게 겹친 파일 하나가 스택을 넘쳐 게임을 죽인다(잡을 수 없는 예외).
        private const int MaxDepth = 64;

        private static object Value(string s, ref int i, int depth)
        {
            if (depth > MaxDepth)
            {
                throw Bad(i, "너무 깊게 겹쳐 있습니다");
            }
            SkipSpace(s, ref i);
            if (i >= s.Length)
            {
                throw Bad(i, "값이 있어야 할 자리에서 끝났습니다");
            }
            char c = s[i];
            if (c == '{')
            {
                return Object(s, ref i, depth);
            }
            if (c == '[')
            {
                return Array(s, ref i, depth);
            }
            if (c == '"')
            {
                return String(s, ref i);
            }
            if (Word(s, ref i, "true"))
            {
                return true;
            }
            if (Word(s, ref i, "false"))
            {
                return false;
            }
            if (Word(s, ref i, "null"))
            {
                return null;
            }
            return Number(s, ref i);
        }

        private static bool Word(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0)
            {
                return false;
            }
            i += word.Length;
            return true;
        }

        private static object Object(string s, ref int i, int depth)
        {
            var result = new Dictionary<string, object>();
            i++;
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == '}')
            {
                i++;
                return result;
            }
            while (true)
            {
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != '"')
                {
                    throw Bad(i, "이름(따옴표 글)이 있어야 합니다");
                }
                string key = String(s, ref i);
                SkipSpace(s, ref i);
                if (i >= s.Length || s[i] != ':')
                {
                    throw Bad(i, "':'이 있어야 합니다");
                }
                i++;
                result[key] = Value(s, ref i, depth + 1);
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }
                if (i < s.Length && s[i] == '}')
                {
                    i++;
                    return result;
                }
                throw Bad(i, "',' 또는 '}'가 있어야 합니다");
            }
        }

        private static object Array(string s, ref int i, int depth)
        {
            var result = new List<object>();
            i++;
            SkipSpace(s, ref i);
            if (i < s.Length && s[i] == ']')
            {
                i++;
                return result;
            }
            while (true)
            {
                result.Add(Value(s, ref i, depth + 1));
                SkipSpace(s, ref i);
                if (i < s.Length && s[i] == ',')
                {
                    i++;
                    continue;
                }
                if (i < s.Length && s[i] == ']')
                {
                    i++;
                    return result;
                }
                throw Bad(i, "',' 또는 ']'가 있어야 합니다");
            }
        }

        private static string String(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;
            while (true)
            {
                if (i >= s.Length)
                {
                    throw Bad(i, "따옴표가 닫히지 않았습니다");
                }
                char c = s[i++];
                if (c == '"')
                {
                    return sb.ToString();
                }
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= s.Length)
                {
                    throw Bad(i, "역슬래시 뒤에서 끝났습니다");
                }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        int code;
                        if (i + 4 > s.Length || !int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                        {
                            throw Bad(i, "\\u 뒤에 16진수 네 자리가 있어야 합니다");
                        }
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default:
                        // \" \\ \/ 는 그 글자 그대로
                        sb.Append(e);
                        break;
                }
            }
        }

        private static object Number(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E'))
            {
                i++;
            }
            string text = s.Substring(start, i - start);
            long whole;
            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out whole))
            {
                return whole;
            }
            double real;
            if (text.Length > 0 && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out real))
            {
                return real;
            }
            throw Bad(start, "값을 읽을 수 없습니다");
        }
    }
}
