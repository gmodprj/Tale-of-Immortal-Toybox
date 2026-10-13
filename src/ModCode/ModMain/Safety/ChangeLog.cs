using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MelonLoader;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 편집기가 한 모든 쓰기를 "변경 전 -> 변경 후"로 기록한다. 메모리와 파일 양쪽에 남긴다.
    internal static class ChangeLog
    {
        public class Entry
        {
            public DateTime Time;
            public string Target;
            public string Field;
            public string Before;
            public string After;
            public bool Verified;

            public override string ToString()
            {
                return Time.ToString("HH:mm:ss") + "  " + Target + "  " + Field + "  " + Before + " -> " + After
                    + (Verified ? "" : "  (재조회 불일치)");
            }
        }

        private static readonly List<Entry> entries = new List<Entry>();

        public static IList<Entry> Entries
        {
            get { return entries; }
        }

        public static string DataDir
        {
            get { return Path.Combine(Application.persistentDataPath, "TlToybox"); }
        }

        // changes.log의 마지막 maxLines줄을 최신 순으로 돌려준다. 이번 실행의 기록도 이 파일에 들어 있다.
        // 읽지 못하면 빈 목록과 이유를 돌려준다.
        public static List<string> ReadHistory(int maxLines, out string error)
        {
            error = null;
            var result = new List<string>();
            try
            {
                string path = Path.Combine(DataDir, "changes.log");
                if (!File.Exists(path))
                {
                    return result;
                }
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = lines.Length - 1; i >= 0 && result.Count < maxLines; i--)
                {
                    if (lines[i].Length > 0)
                    {
                        result.Add(lines[i]);
                    }
                }
            }
            catch (IOException e)
            {
                error = e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                error = e.Message;
            }
            return result;
        }
        public static void Add(string target, string field, string before, string after, bool verified)
        {
            var entry = new Entry { Time = DateTime.Now, Target = target, Field = field, Before = before, After = after, Verified = verified };
            entries.Add(entry);
            MelonLogger.Msg("[TlToybox][change] " + entry);
            try
            {
                Directory.CreateDirectory(DataDir);
                File.AppendAllText(Path.Combine(DataDir, "changes.log"),
                    entry.Time.ToString("yyyy-MM-dd ") + entry + Environment.NewLine, Encoding.UTF8);
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] 변경 이력 파일 기록 실패: " + e.Message);
            }
        }
    }
}
