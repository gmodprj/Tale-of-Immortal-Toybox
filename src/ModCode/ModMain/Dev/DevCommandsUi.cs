using System;
using System.Reflection;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 개발 전용. 편집기 화면의 검증 명령. tools/research/check-lib.ps1이 이 명령들의 출력 모양에 의존한다.
    //   uipos <이름>   : 편집기 안에서 그 이름의 요소를 찾아 가운데의 게임 좌표를 돌려준다. 이름 끝의 *는 "이 글자로 시작"이다
    //   uitext <이름>  : 그 요소의 글(입력 칸이면 입력된 글, 아니면 그 아래 첫 Text)
    //   uifocus <이름> : 그 이름의 입력 칸을 고르고 입력 상태로 만든다
    //   uidrag x1 y1 x2 y2 [steps] : 끌기 이벤트를 보낸다(실제 마우스가 만드는 것과 같은 이벤트 경로)
    //   uicanvas / uilist / uiopen <화면> / uiclose <화면> : 게임 화면의 정렬 값을 읽고, 화면을 열고 닫는다
    internal static class DevCommandsUi
    {
        private const string EditorName = "TlToyboxEditor";

        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "uipos":
                    return Pos(p[1]);
                case "uitext":
                    return TextOf(p[1]);
                case "uifocus":
                    return Focus(p[1]);
                case "uidrag":
                    return Drag(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), p.Length > 5 ? int.Parse(p[5]) : 4);
                case "uicanvas":
                    return CanvasInfo();
                case "uilist":
                    return OpenList();
                case "uiopen":
                    return Open(p[1]);
                case "uiclose":
                    g.ui.CloseUI(TypeOf(p[1]), true);
                    return "closed " + p[1];
                default:
                    return null;
            }
        }

        private static bool Matches(string actual, string wanted)
        {
            return wanted.EndsWith("*") ? actual.StartsWith(wanted.Substring(0, wanted.Length - 1)) : actual == wanted;
        }

        // 켜져 있는 요소만 깊이 우선으로 찾는다. 같은 이름이 여럿이면 먼저 만든 것이 나온다.
        private static Transform FindDeep(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }
                if (Matches(child.name, name))
                {
                    return child;
                }
                Transform found = FindDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static Transform Find(string name)
        {
            GameObject root = GameObject.Find(EditorName);
            return root == null ? null : FindDeep(root.transform, name);
        }

        private static string Pos(string name)
        {
            Transform found = Find(name);
            if (found == null)
            {
                return "no element named " + name;
            }
            RectTransform rect = found.GetComponent<RectTransform>();
            Canvas canvas = GameObject.Find(EditorName).GetComponent<Canvas>();
            // 덧그리기 방식에서는 세계 좌표가 곧 화면 좌표다. 카메라 방식에서는 그 카메라로 바꾼다.
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Rect r = rect.rect;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(new Vector3(r.center.x, r.center.y, 0f)));
            return found.name + " at " + Mathf.RoundToInt(screen.x) + "," + Mathf.RoundToInt(Screen.height - screen.y)
                + " size=" + Mathf.RoundToInt(r.width) + "," + Mathf.RoundToInt(r.height);
        }

        private static string TextOf(string name)
        {
            Transform found = Find(name);
            if (found == null)
            {
                return "no element named " + name;
            }
            InputField input = found.GetComponent<InputField>();
            if (input != null)
            {
                return found.name + " text='" + input.text + "'";
            }
            Text text = found.GetComponentInChildren<Text>();
            return found.name + " text='" + (text == null ? "" : text.text) + "'";
        }

        private static string Focus(string name)
        {
            Transform found = Find(name);
            if (found == null)
            {
                return "no element named " + name;
            }
            InputField input = found.GetComponent<InputField>();
            if (input == null)
            {
                return found.name + " is not an input";
            }
            EventSystem.current.SetSelectedGameObject(found.gameObject);
            input.ActivateInputField();
            return found.name + " focused";
        }

        // 게임 좌표(왼쪽 위 기준) (x1,y1)에서 (x2,y2)까지 끄는 이벤트를 그 자리의 끌기 처리기에 보낸다.
        private static string Drag(int x1, int y1, int x2, int y2, int steps)
        {
            EventSystem es = EventSystem.current;
            if (es == null)
            {
                return "no EventSystem";
            }
            var pointer = new PointerEventData(es);
            var from = new Vector2(x1, Screen.height - y1);
            var to = new Vector2(x2, Screen.height - y2);
            pointer.position = from;
            pointer.pressPosition = from;
            pointer.button = PointerEventData.InputButton.Left;
            var results = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            es.RaycastAll(pointer, results);
            if (results.Count == 0)
            {
                return "no hit at " + x1 + "," + y1;
            }
            GameObject hit = results[0].gameObject;
            GameObject handler = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
            if (handler == null)
            {
                return "hit=" + hit.name + " -> no drag handler";
            }
            pointer.pointerDrag = handler;
            pointer.dragging = true;
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.beginDragHandler);
            Vector2 step = (to - from) / steps;
            for (int i = 0; i < steps; i++)
            {
                pointer.delta = step;
                pointer.position = pointer.position + step;
                ExecuteEvents.Execute(handler, pointer, ExecuteEvents.dragHandler);
            }
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.endDragHandler);
            return "hit=" + hit.name + " handler=" + handler.name + " dragged " + (x2 - x1) + "," + (y2 - y1) + " in " + steps + " steps";
        }

        private static string Describe(Canvas c)
        {
            if (c == null)
            {
                return "null";
            }
            return c.gameObject.name + " mode=" + c.renderMode + " override=" + c.overrideSorting + " order=" + c.sortingOrder
                + " plane=" + c.planeDistance + " cam=" + (c.worldCamera == null ? "none" : c.worldCamera.name) + " goLayer=" + c.gameObject.layer;
        }

        private static string CanvasInfo()
        {
            var sb = new StringBuilder();
            sb.Append("screen=").Append(Screen.width).Append('x').Append(Screen.height);
            sb.Append("\n  game canvas: ").Append(Describe(g.ui.canvas));
            sb.Append("\n  UI layer topSort=").Append(g.ui.GetLayerTopSort(UILayer.UI)).Append(" interval=").Append(UIMgr.UIIntervalSorting);
            GameObject editor = GameObject.Find(EditorName);
            sb.Append("\n  editor: ").Append(editor == null ? "not open" : Describe(editor.GetComponent<Canvas>()));
            return sb.ToString();
        }

        private static string OpenList()
        {
            var sb = new StringBuilder("open UI:");
            foreach (var pair in g.ui.allUI)
            {
                var list = pair.Value;
                for (int i = 0; list != null && i < list.Count; i++)
                {
                    UIBase ui = list[i];
                    if (ui == null)
                    {
                        continue;
                    }
                    sb.Append("\n  ").Append(pair.Key).Append(" layer=").Append(ui.uiType == null ? "?" : ui.uiType.uiLayer.ToString())
                        .Append(" sort=").Append(ui.sortingOrder).Append(" active=").Append(ui.gameObject.activeInHierarchy);
                }
            }
            return sb.ToString();
        }

        private static UIType.UITypeBase TypeOf(string name)
        {
            PropertyInfo prop = typeof(UIType).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            if (prop == null)
            {
                throw new ArgumentException("no UIType." + name);
            }
            return (UIType.UITypeBase)prop.GetValue(null, null);
        }

        private static string Open(string name)
        {
            if (name == "CheckPopup")
            {
                g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("시험", "편집기와의 앞뒤 순서를 보는 시험 창입니다.", 2,
                    new Action(() => MelonLogger.Msg("[TlToybox][cmd] check popup yes")), new Action(() => MelonLogger.Msg("[TlToybox][cmd] check popup no")));
                return "opened CheckPopup";
            }
            g.ui.OpenUI(TypeOf(name));
            return "opened " + name;
        }
    }
}
