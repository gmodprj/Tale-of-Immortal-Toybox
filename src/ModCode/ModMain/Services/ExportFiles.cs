using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 내보내기 파일의 공통 처리. 가방(bag)과 캐릭터(character)가 함께 쓴다.
    // 파일은 세이브 폴더 밖의 <TlToybox>\exports\<종류>\<이름>.json 이다.
    internal static class ExportFiles
    {
        public static string Dir(string kind)
        {
            return Path.Combine(Path.Combine(ChangeLog.DataDir, "exports"), kind);
        }

        // 화면에서 적은 이름을 파일 이름으로. 쓸 수 없는 글자는 _로 바꾼다. 빈 글이면 이름이 없는 것이다.
        public static string FileNameFor(string name)
        {
            var sb = new StringBuilder();
            char[] bad = Path.GetInvalidFileNameChars();
            foreach (char c in (name ?? "").Trim())
            {
                sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
            }
            return sb.ToString().Trim().TrimEnd('.');
        }

        // 같은 이름이 있으면 "이름 (2)", "이름 (3)"으로 쓴다. 쓴 파일의 경로를 돌려준다.
        public static string Write(string kind, string stem, string json)
        {
            string dir = Dir(kind);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, stem + ".json");
            for (int n = 2; File.Exists(path); n++)
            {
                path = Path.Combine(dir, stem + " (" + n + ").json");
            }
            File.WriteAllText(path, json, new UTF8Encoding(false));
            return path;
        }

        // 그 종류의 내보내기 파일들. 최신이 앞. 폴더가 없거나 읽지 못하면 빈 목록.
        public static List<string> Files(string kind)
        {
            var files = new List<string>();
            try
            {
                string dir = Dir(kind);
                if (Directory.Exists(dir))
                {
                    files.AddRange(Directory.GetFiles(dir, "*.json"));
                    files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return files;
        }

        // 내보내기 파일을 지운다. 그 종류의 폴더 안에 있는 .json만 지운다.
        public static string Delete(string kind, string file)
        {
            try
            {
                string full = Path.GetFullPath(file);
                if (!full.StartsWith(Path.GetFullPath(Dir(kind)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !full.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return "삭제: 내보내기 폴더의 파일이 아니라 지우지 않았습니다.";
                }
                if (!File.Exists(full))
                {
                    return "삭제: 파일이 이미 없습니다(" + Path.GetFileName(full) + ").";
                }
                File.Delete(full);
                return "지웠습니다: " + Path.GetFileName(full);
            }
            catch (IOException e)
            {
                return "삭제: 지우지 못했습니다(" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "삭제: 지우지 못했습니다(" + e.Message + ").";
            }
        }

        // ---- JSON에서 값 꺼내기 (Json.Parse의 결과) ----

        public static string Text(Dictionary<string, object> o, string key)
        {
            object v;
            return o.TryGetValue(key, out v) && v is string ? (string)v : "";
        }

        // 정수로 읽는다. 없거나 정수가 아니거나 int 범위를 넘으면 false.
        public static bool Whole(Dictionary<string, object> o, string key, out int value)
        {
            value = 0;
            object v;
            if (!o.TryGetValue(key, out v) || !(v is long))
            {
                return false;
            }
            long n = (long)v;
            if (n < int.MinValue || n > int.MaxValue)
            {
                return false;
            }
            value = (int)n;
            return true;
        }

        // 정수 배열로 읽는다. 없으면 빈 배열과 true, 있는데 정수가 아닌 것이 섞였으면 false.
        public static bool Ints(Dictionary<string, object> o, string key, out int[] values)
        {
            values = new int[0];
            object v;
            if (!o.TryGetValue(key, out v))
            {
                return true;
            }
            var list = v as List<object>;
            if (list == null)
            {
                return false;
            }
            var result = new int[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                if (!(list[i] is long) || (long)list[i] < int.MinValue || (long)list[i] > int.MaxValue)
                {
                    return false;
                }
                result[i] = (int)(long)list[i];
            }
            values = result;
            return true;
        }

        // 객체의 배열로 읽는다. 없거나 배열이 아니면 빈 목록. 객체가 아닌 요소는 null로 들어간다.
        public static List<Dictionary<string, object>> Items(Dictionary<string, object> o, string key)
        {
            var result = new List<Dictionary<string, object>>();
            object v;
            if (o.TryGetValue(key, out v) && v is List<object>)
            {
                foreach (object each in (List<object>)v)
                {
                    result.Add(each as Dictionary<string, object>);
                }
            }
            return result;
        }
    }
}
