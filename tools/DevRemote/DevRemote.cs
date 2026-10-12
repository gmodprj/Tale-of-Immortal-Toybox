using System;
using System.IO;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(TlToyboxDevRemote.DevRemote), "TlToyboxDevRemote", "0.1.0", "TlToybox")]
[assembly: MelonGame]

namespace TlToyboxDevRemote
{
    // 개발 전용. 명령 파일을 읽어 게임 안에서 UI 클릭을 수행한다.
    // 실제 마우스와 키보드를 쓰지 않으므로 게임 창이 뒤에 있어도 조작할 수 있다.
    //   명령 파일: <CommandDir>\cmd.txt   한 줄에 하나: "click <x> <y>" (화면 왼쪽 위 기준 픽셀), "hit <x> <y>"
    //   결과 파일: <CommandDir>\out.txt
    public class DevRemote : MelonMod
    {
        private const string CommandDir = @"E:\TlToybox\.devremote";
        private const int PollFrames = 10;

        private int frame;

        public override void OnApplicationStart()
        {
            Directory.CreateDirectory(CommandDir);
            MelonLogger.Msg("[DevRemote] watching " + CommandDir);
        }

        public override void OnUpdate()
        {
            frame++;
            if (frame % PollFrames != 0)
            {
                return;
            }
            Park();
            string cmdPath = Path.Combine(CommandDir, "cmd.txt");
            if (!File.Exists(cmdPath))
            {
                return;
            }
            string[] lines;
            try
            {
                lines = File.ReadAllLines(cmdPath);
                File.Delete(cmdPath);
            }
            catch (IOException)
            {
                return;
            }
            var output = new StringBuilder();
            foreach (string line in lines)
            {
                string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }
                try
                {
                    output.AppendLine(Handle(parts));
                }
                catch (Exception e)
                {
                    output.AppendLine("ERROR " + line + " : " + e.GetType().Name + ": " + e.Message);
                }
            }
            File.WriteAllText(Path.Combine(CommandDir, "out.txt"), output.ToString(), Encoding.UTF8);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out WinRect rect);

        private struct WinRect { public int Left, Top, Right, Bottom; }

        private const int ParkX = 30000;
        private const int ParkY = 0;

        // <CommandDir>\park.flag 파일이 있는 동안 게임을 창 모드로 바꾸고 화면 밖에 세워 둔다.
        // 사용자의 화면을 가리지 않으면서 창 내용 캡처와 조작이 가능하게 하기 위한 것이다.
        // 파일이 없으면 아무것도 하지 않으므로 평소 플레이에는 영향이 없다.
        private static void Park()
        {
            if (!File.Exists(Path.Combine(CommandDir, "park.flag")))
            {
                return;
            }
            if (Screen.fullScreen || Screen.width != 1920 || Screen.height != 1080)
            {
                Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
                return;
            }
            IntPtr window = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (window == IntPtr.Zero)
            {
                return;
            }
            if (IsIconic(window))
            {
                ShowWindow(window, 4); // SW_SHOWNOACTIVATE
                return;
            }
            WinRect rect;
            if (GetWindowRect(window, out rect) && (rect.Left != ParkX || rect.Top != ParkY))
            {
                SetWindowPos(window, IntPtr.Zero, ParkX, ParkY, 0, 0, 0x0001 | 0x0004 | 0x0010); // NOSIZE | NOZORDER | NOACTIVATE
            }
        }

        private static string Handle(string[] parts)
        {
            if ((parts[0] == "click" || parts[0] == "hit") && parts.Length == 3)
            {
                return Pointer(int.Parse(parts[1]), int.Parse(parts[2]), parts[0] == "click");
            }
            if (parts[0] == "scroll" && parts.Length == 4)
            {
                return Scroll(int.Parse(parts[1]), int.Parse(parts[2]), float.Parse(parts[3]));
            }
            return "UNKNOWN " + string.Join(" ", parts);
        }

        // 좌표 아래의 스크롤 영역에 마우스 휠 입력을 보낸다. delta가 음수면 아래로 내린다.
        private static string Scroll(int x, int y, float delta)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return "no EventSystem";
            }
            var pointer = new PointerEventData(eventSystem);
            pointer.position = new Vector2(x, Screen.height - y);
            pointer.scrollDelta = new Vector2(0, delta);
            var results = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointer, results);
            if (results.Count == 0)
            {
                return "no hit at " + x + "," + y;
            }
            GameObject hit = results[0].gameObject;
            GameObject handler = ExecuteEvents.GetEventHandler<IScrollHandler>(hit);
            if (handler == null)
            {
                return "hit=" + PathOf(hit.transform) + " -> no scroll handler";
            }
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.scrollHandler);
            return "hit=" + PathOf(hit.transform) + " -> scroll " + delta + ": " + handler.name;
        }

        private static string Pointer(int x, int y, bool click)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return "no EventSystem";
            }
            var pointer = new PointerEventData(eventSystem);
            pointer.position = new Vector2(x, Screen.height - y);
            pointer.button = PointerEventData.InputButton.Left;
            var results = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointer, results);
            if (results.Count == 0)
            {
                return "screen=" + Screen.width + "x" + Screen.height + " no hit at " + x + "," + y;
            }
            RaycastResult first = results[0];
            GameObject hit = first.gameObject;
            string head = "screen=" + Screen.width + "x" + Screen.height + " hit=" + PathOf(hit.transform);
            if (!click)
            {
                return head;
            }
            pointer.pointerCurrentRaycast = first;
            pointer.pointerPressRaycast = first;

            for (Transform t = hit.transform; t != null; t = t.parent)
            {
                Button button = t.gameObject.GetComponent<Button>();
                if (button != null)
                {
                    if (!button.interactable)
                    {
                        return head + " -> Button not interactable: " + t.name;
                    }
                    button.onClick.Invoke();
                    return head + " -> Button.onClick: " + t.name;
                }
                Toggle toggle = t.gameObject.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.isOn = !toggle.isOn;
                    return head + " -> Toggle: " + t.name + " isOn=" + toggle.isOn;
                }
            }

            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            if (handler == null)
            {
                return head + " -> no click handler";
            }
            pointer.pointerPress = handler;
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.pointerClickHandler);
            return head + " -> pointer events: " + handler.name;
        }

        private static string PathOf(Transform t)
        {
            var sb = new StringBuilder(t.name);
            int depth = 0;
            for (Transform p = t.parent; p != null && depth < 6; p = p.parent, depth++)
            {
                sb.Insert(0, p.name + "/");
            }
            return sb.ToString();
        }
    }
}
