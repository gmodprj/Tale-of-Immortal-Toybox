using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;

namespace MOD_sWP6UL
{
    // config.txt 파일 입출력. 읽기·쓰기 실패는 로그만 남기고 동작은 이어 간다(설정은 없어도 기본값으로 돈다).
    // 쓰기는 AtomicFile(임시 파일 뒤 바꿔치기. 13 문서 A7), 예외는 종류를 가리지 않고 받는다(A15: 경로 오류 같은 다른 예외가 EditorHost까지 올라가던 것).
    internal static partial class ConfigFile
    {
        public static string FilePath
        {
            get { return Path.Combine(ChangeLog.DataDir, "config.txt"); }
        }

        // 파일의 키=값. 없거나 읽지 못하면 빈 사전. what은 로그에 쓸 이름(예: "창 위치").
        public static Dictionary<string, string> Read(string what)
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return Parse(File.ReadAllLines(FilePath));
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] " + KoreanText.Object(what) + " 읽지 못했습니다: " + e.GetType().Name + ": " + e.Message);
            }
            return Parse(new string[0]);
        }

        // 주어진 키의 줄만 바꿔 쓴다(다른 줄은 그대로).
        public static void Write(IDictionary<string, string> values, string what)
        {
            try
            {
                string[] lines = File.Exists(FilePath) ? File.ReadAllLines(FilePath) : new string[0];
                // File.WriteAllLines와 같은 모양(줄마다 줄 끝, BOM 없는 UTF-8)으로 AtomicFile에 쓴다.
                var sb = new System.Text.StringBuilder();
                foreach (string line in Replace(lines, values))
                {
                    sb.Append(line).Append(Environment.NewLine);
                }
                AtomicFile.WriteAllText(FilePath, sb.ToString(), new System.Text.UTF8Encoding(false));
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] " + KoreanText.Object(what) + " 저장하지 못했습니다: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // 파일이 없으면 text로 만든다(처음 실행 때 안내 주석과 기본 줄). 있으면 건드리지 않는다.
        public static void EnsureFile(string text, string what)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    AtomicFile.WriteAllText(FilePath, text, new System.Text.UTF8Encoding(false));
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] " + KoreanText.Object(what) + " 만들지 못했습니다: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
