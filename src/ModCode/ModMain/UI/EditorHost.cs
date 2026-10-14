using System;
using System.IO;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 단축키를 감시해 편집기 창을 여닫는 컴포넌트를 게임에 붙인다.
    internal static class EditorHost
    {
        private static bool registered;
        private static GameObject host;

        public static KeyCode Hotkey = KeyCode.Insert;

        public static void Ensure()
        {
            Hotkey = ReadHotkey();
            if (!registered)
            {
                ClassInjector.RegisterTypeInIl2Cpp<EditorHostBehaviour>();
                registered = true;
            }
            if (host == null)
            {
                host = new GameObject("TlToyboxHost");
                UnityEngine.Object.DontDestroyOnLoad(host);
                host.AddComponent<EditorHostBehaviour>();
            }
            MelonLogger.Msg("[TlToybox] 편집기 단축키: " + Hotkey);
        }

        // <세이브 루트>\TlToybox\config.txt 의 "hotkey=Insert|F2|F3". 파일이 없거나 값이 다르면 Insert.
        private static KeyCode ReadHotkey()
        {
            try
            {
                string path = Path.Combine(ChangeLog.DataDir, "config.txt");
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(ChangeLog.DataDir);
                    File.WriteAllText(path, "# 편집기를 여닫는 키. Insert, F2, F3 중 하나." + Environment.NewLine + "hotkey=Insert" + Environment.NewLine);
                    return KeyCode.Insert;
                }
                foreach (string line in File.ReadAllLines(path))
                {
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("hotkey=", StringComparison.OrdinalIgnoreCase))
                    {
                        string value = trimmed.Substring(7).Trim().ToLowerInvariant();
                        if (value == "f2") { return KeyCode.F2; }
                        if (value == "f3") { return KeyCode.F3; }
                    }
                }
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] 설정 파일을 읽지 못했습니다: " + e.Message);
            }
            return KeyCode.Insert;
        }
    }

    public class EditorHostBehaviour : MonoBehaviour
    {
        public EditorHostBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            // 편집기를 열지 않아도 가방 추가 칸이 적용되게 한다. 예외를 던지지 않는다.
            BagService.Tick();
            EditorWindow.Tick();
            StatusOverlay.Tick();
            RateService.Tick();
            ArtifactHudService.Tick();
            if (Input.GetKeyDown(EditorHost.Hotkey))
            {
                try
                {
                    EditorWindow.Toggle();
                }
                catch (Exception e)
                {
                    MelonLogger.Msg("[TlToybox] 편집기 창 오류: " + e);
                }
            }
        }
    }
}
