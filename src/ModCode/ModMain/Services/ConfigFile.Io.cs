using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;

namespace MOD_sWP6UL
{
    // config.txt 파일 입출력. 읽기·쓰기 실패는 로그만 남기고 동작은 이어 간다(설정은 없어도 기본값으로 돈다).
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
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] " + what + "을(를) 읽지 못했습니다: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                MelonLogger.Msg("[TlToybox] " + what + "을(를) 읽지 못했습니다: " + e.Message);
            }
            return Parse(new string[0]);
        }

        // 주어진 키의 줄만 바꿔 쓴다(다른 줄은 그대로).
        public static void Write(IDictionary<string, string> values, string what)
        {
            try
            {
                Directory.CreateDirectory(ChangeLog.DataDir);
                string[] lines = File.Exists(FilePath) ? File.ReadAllLines(FilePath) : new string[0];
                File.WriteAllLines(FilePath, Replace(lines, values).ToArray());
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] " + what + "을(를) 저장하지 못했습니다: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                MelonLogger.Msg("[TlToybox] " + what + "을(를) 저장하지 못했습니다: " + e.Message);
            }
        }

        // 파일이 없으면 text로 만든다(처음 실행 때 안내 주석과 기본 줄). 있으면 건드리지 않는다.
        public static void EnsureFile(string text, string what)
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    Directory.CreateDirectory(ChangeLog.DataDir);
                    File.WriteAllText(FilePath, text);
                }
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] " + what + "을(를) 만들지 못했습니다: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                MelonLogger.Msg("[TlToybox] " + what + "을(를) 만들지 못했습니다: " + e.Message);
            }
        }
    }
}
