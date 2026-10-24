using System;
using MelonLoader;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 개발 전용 탐침. 편집기 UI를 무엇으로 만들지 정하기 위해 IMGUI, 코드로 만든 uGUI, 키 입력을 시험한다.
    internal static class UiSpike
    {
        private static bool registered;
        private static GameObject root;
        private static GameObject canvasRoot;
        private static int clicks;
        private static Text counterText;

        public static string Execute(string[] p)
        {
            if (p[0] != "ui")
            {
                return null;
            }
            switch (p[1])
            {
                case "imgui":
                    EnsureBehaviour();
                    UiSpikeBehaviour.ShowImgui = p[2] == "on";
                    return "imgui=" + UiSpikeBehaviour.ShowImgui + " onGuiCalls=" + UiSpikeBehaviour.OnGuiCalls;
                case "keys":
                    EnsureBehaviour();
                    return "f8Downs=" + UiSpikeBehaviour.F8Downs + " anyKeyFrames=" + UiSpikeBehaviour.AnyKeyFrames
                        + " updates=" + UiSpikeBehaviour.Updates + " onGuiCalls=" + UiSpikeBehaviour.OnGuiCalls
                        + " imguiButtonClicks=" + UiSpikeBehaviour.ImguiClicks;
                case "fonts":
                    return Fonts();
                case "ugui":
                    if (p[2] == "on")
                    {
                        BuildCanvas();
                        return "ugui built; clicks=" + clicks;
                    }
                    if (canvasRoot != null)
                    {
                        UnityEngine.Object.Destroy(canvasRoot);
                        canvasRoot = null;
                    }
                    return "ugui destroyed; clicks=" + clicks;
                case "codes":
                    {
                        string raw = g.world.unit.GetUnits(false)[1].data.unitData.propertyData.GetName();
                        var sb = new System.Text.StringBuilder();
                        foreach (char c in raw) { sb.Append(((int)c).ToString("X4")).Append(' '); }
                        var sq = new System.Text.StringBuilder();
                        foreach (char c in (p.Length > 2 ? p[2] : "")) { sq.Append(((int)c).ToString("X4")).Append(' '); }
                        return "name=" + sb + "| arg=" + sq;
                    }
                case "set":
                    return EditorWindow.DevSetInput(p[2], p.Length > 3 ? p[3] : "");
                case "clicks":
                    return "clicks=" + clicks;
                default:
                    return "UNKNOWN ui command";
            }
        }

        private static void EnsureBehaviour()
        {
            if (!registered)
            {
                ClassInjector.RegisterTypeInIl2Cpp<UiSpikeBehaviour>();
                registered = true;
            }
            if (root == null)
            {
                root = new GameObject("TlToyboxUiSpike");
                UnityEngine.Object.DontDestroyOnLoad(root);
                root.AddComponent<UiSpikeBehaviour>();
            }
        }

        private static string Fonts()
        {
            var texts = Resources.FindObjectsOfTypeAll<Text>();
            var tmps = Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>();
            string textFont = "none";
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].font != null)
                {
                    textFont = texts[i].font.name;
                    break;
                }
            }
            string tmpFont = "none";
            for (int i = 0; i < tmps.Length; i++)
            {
                if (tmps[i].font != null)
                {
                    tmpFont = tmps[i].font.name;
                    break;
                }
            }
            return "uiText=" + texts.Length + " firstFont=" + textFont + " tmp=" + tmps.Length + " firstTmpFont=" + tmpFont;
        }

        private static Font GameFont()
        {
            var texts = Resources.FindObjectsOfTypeAll<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].font != null)
                {
                    return texts[i].font;
                }
            }
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void BuildCanvas()
        {
            if (canvasRoot != null)
            {
                return;
            }
            Font font = GameFont();

            canvasRoot = new GameObject("TlToyboxSpikeCanvas");
            UnityEngine.Object.DontDestroyOnLoad(canvasRoot);
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            canvasRoot.AddComponent<GraphicRaycaster>();

            var panel = NewRect("Panel", canvasRoot.transform, new Vector2(40, -40), new Vector2(520, 220));
            panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

            var title = NewRect("Title", panel, new Vector2(16, -12), new Vector2(480, 40));
            AddText(title, font, "TlToybox UI 탐침 — 한글 표시 시험", 24);

            var counter = NewRect("Counter", panel, new Vector2(16, -64), new Vector2(480, 36));
            counterText = AddText(counter, font, "클릭 횟수: 0", 22);

            var buttonRect = NewRect("Button", panel, new Vector2(16, -120), new Vector2(220, 56));
            var image = buttonRect.gameObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.45f, 0.75f, 1f);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(new Action(OnButtonClick));
            var label = NewRect("Label", buttonRect, new Vector2(0, 0), new Vector2(220, 56));
            AddText(label, font, "눌러 보기", 22).alignment = TextAnchor.MiddleCenter;
        }

        private static void OnButtonClick()
        {
            clicks++;
            if (counterText != null)
            {
                counterText.text = "클릭 횟수: " + clicks;
            }
            MelonLogger.Msg("[TlToybox][ui] uGUI button clicked " + clicks);
        }

        // 왼쪽 위 기준 위치와 크기로 RectTransform을 만든다.
        private static RectTransform NewRect(string name, Transform parent, Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            return rect;
        }

        private static Text AddText(RectTransform rect, Font font, string value, int size)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }
    }

    public class UiSpikeBehaviour : MonoBehaviour
    {
        public static bool ShowImgui;
        public static int OnGuiCalls;
        public static int Updates;
        public static int F8Downs;
        public static int AnyKeyFrames;
        public static int ImguiClicks;

        public UiSpikeBehaviour(IntPtr ptr) : base(ptr) { }

        private void Update()
        {
            Updates++;
            if (Input.GetKeyDown(KeyCode.F8))
            {
                F8Downs++;
                MelonLogger.Msg("[TlToybox][ui] F8 down " + F8Downs);
            }
            if (Input.anyKey)
            {
                AnyKeyFrames++;
            }
        }

        private void OnGUI()
        {
            OnGuiCalls++;
            if (!ShowImgui)
            {
                return;
            }
            GUI.Box(new Rect(600, 40, 420, 180), "TlToybox IMGUI 탐침 — 한글 표시 시험");
            GUI.Label(new Rect(620, 80, 380, 30), "IMGUI 버튼 클릭 횟수: " + ImguiClicks);
            if (GUI.Button(new Rect(620, 120, 200, 50), "눌러 보기"))
            {
                ImguiClicks++;
            }
        }
    }
}
