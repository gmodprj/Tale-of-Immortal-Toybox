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

        private static string Handle(string[] parts)
        {
            if ((parts[0] == "click" || parts[0] == "hit") && parts.Length == 3)
            {
                return Pointer(int.Parse(parts[1]), int.Parse(parts[2]), parts[0] == "click");
            }
            return "UNKNOWN " + string.Join(" ", parts);
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
