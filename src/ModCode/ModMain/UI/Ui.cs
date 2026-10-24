using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 코드로 uGUI 요소를 만드는 도우미. 좌표는 부모의 왼쪽 위를 원점으로 하고 y는 아래로 증가한다.
    internal static class Ui
    {
        public static readonly Color Background = new Color(0.07f, 0.08f, 0.10f, 0.95f);
        public static readonly Color Section = new Color(0.14f, 0.15f, 0.18f, 1f);
        public static readonly Color ButtonColor = new Color(0.22f, 0.36f, 0.58f, 1f);
        public static readonly Color ButtonActive = new Color(0.72f, 0.52f, 0.16f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.25f, 0.25f, 0.27f, 1f);
        public static readonly Color Dim = new Color(0.68f, 0.70f, 0.74f, 1f);
        public static readonly Color Warn = new Color(1f, 0.78f, 0.35f, 1f);

        private static Font font;

        // 게임이 쓰는 글꼴을 빌려 쓴다. 한글이 게임 UI와 같은 품질로 표시된다.
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    // 붓글씨체는 작은 글자에서 비슷한 글자를 구분하기 어려우므로 고딕체("heiti")를 우선한다.
                    var texts = Resources.FindObjectsOfTypeAll<Text>();
                    for (int i = 0; i < texts.Length; i++)
                    {
                        Font candidate = texts[i].font;
                        if (candidate == null)
                        {
                            continue;
                        }
                        if (candidate.name == "heiti")
                        {
                            font = candidate;
                            break;
                        }
                        if (font == null)
                        {
                            font = candidate;
                        }
                    }
                    if (font == null)
                    {
                        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                }
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public static RectTransform Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            RectTransform rect = Rect(name, parent, x, y, w, h);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        public static Text Label(Transform parent, string value, float x, float y, float w, float h, int size = 20,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            RectTransform rect = Rect("Label", parent, x, y, w, h);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        // 너비 안에서 줄바꿈되는 글. 높이는 TextHeight로 잰 값에 맞춰 준다.
        public static Text Wrapped(Transform parent, string value, float x, float y, float w, float h, int size = 20,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            Text text = Label(parent, value, x, y, w, h, size, anchor);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }

        private static Text ruler;
        private static readonly Dictionary<string, float> heights = new Dictionary<string, float>();

        // 그 글을 너비 w 안에서 줄바꿈했을 때의 높이. 목록의 줄 높이를 글에 맞추는 데 쓴다.
        // 아이템 3천여 개를 매번 재면 느리므로 결과를 기억한다(글·너비·크기가 같으면 높이도 같다).
        public static float TextHeight(string value, float w, int size)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0f;
            }
            string key = size + "|" + (int)w + "|" + value;
            float height;
            if (heights.TryGetValue(key, out height))
            {
                return height;
            }
            if (ruler == null)
            {
                // 화면에 나오지 않는 자: 캔버스 밖에 두어 그려지지 않고, 장면이 바뀌어도 남는다.
                var go = new GameObject("TlToyboxRuler");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                ruler = go.AddComponent<Text>();
                ruler.font = Font;
                ruler.horizontalOverflow = HorizontalWrapMode.Wrap;
                ruler.verticalOverflow = VerticalWrapMode.Overflow;
            }
            ruler.fontSize = size;
            // 화면의 배율에 따라 줄이 바뀌는 자리가 한두 글자 달라질 수 있어 조금 좁게 잰다.
            TextGenerationSettings settings = ruler.GetGenerationSettings(new Vector2(w - 6f, 0f));
            height = ruler.cachedTextGeneratorForLayout.GetPreferredHeight(value, settings) / ruler.pixelsPerUnit;
            heights[key] = height;
            return height;
        }

        // 그 글을 한 줄로 쓸 때의 너비. 글에 맞춘 버튼의 너비를 정하는 데 쓴다.
        public static float TextWidth(string value, int size)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0f;
            }
            TextHeight("가", 100f, size);   // 자를 만든다
            ruler.fontSize = size;
            TextGenerationSettings settings = ruler.GetGenerationSettings(new Vector2(10000f, 0f));
            return ruler.cachedTextGeneratorForLayout.GetPreferredWidth(value, settings) / ruler.pixelsPerUnit;
        }

        public static Button Button(string name, Transform parent, string label, float x, float y, float w, float h,
            Action onClick, bool enabled = true, bool active = false)
        {
            RectTransform rect = Rect(name, parent, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = !enabled ? ButtonDisabled : active ? ButtonActive : ButtonColor;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = enabled;
            if (enabled && onClick != null)
            {
                // 콜백의 예외는 화면에 아무 반응이 없고 로그의 trampoline 줄에만 남았다(리뷰 C2). 결과 칸에 보이고 로그에 한 줄 남긴다.
                button.onClick.AddListener(new Action(() =>
                {
                    try
                    {
                        onClick();
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Msg("[TlToybox] 버튼 오류 " + name + ": " + e.GetType().Name + ": " + e.Message);
                        EditorWindow.Say("오류: " + e.GetType().Name + ": " + e.Message);
                    }
                }));
            }
            Text text = Label(rect, label, 0, 0, w, h, 19, TextAnchor.MiddleCenter);
            if (!enabled)
            {
                text.color = Dim;
            }
            return button;
        }

        // 글자를 입력하는 칸. 내용이 바뀔 때마다 onChanged가 호출된다.
        public static InputField Input(string name, Transform parent, string value, string placeholder, float x, float y, float w, float h,
            Action<string> onChanged, bool integerOnly = false)
        {
            RectTransform rect = Rect(name, parent, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.02f, 0.02f, 0.03f, 1f);
            Text text = Label(rect, "", 8, 0, w - 16, h);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            Text hint = Label(rect, placeholder, 8, 0, w - 16, h, 18);
            hint.color = Dim;
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = text;
            input.placeholder = hint;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = integerOnly ? InputField.ContentType.IntegerNumber : InputField.ContentType.Standard;
            input.characterLimit = integerOnly ? 10 : 20;
            input.text = value;
            if (onChanged != null)
            {
                input.onValueChanged.AddListener(onChanged);
            }
            return input;
        }

        // 세로로 스크롤되는 영역. 돌려주는 RectTransform에 내용을 넣는다(좌표는 그 영역의 왼쪽 위 기준).
        // offset은 처음에 내려 둘 거리, onScroll은 스크롤할 때마다 현재 내려간 거리를 받는다.
        public const float ScrollBarWidth = 14;

        public static RectTransform Scroll(string name, Transform parent, float x, float y, float w, float h, float contentHeight,
            float offset = 0, Action<float> onScroll = null)
        {
            float innerHeight = Mathf.Max(h, contentHeight);
            RectTransform viewport = Rect(name, parent, x, y, w - ScrollBarWidth, h);
            var image = viewport.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.25f);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform inner = Rect("Rows", viewport, 0, 0, w - ScrollBarWidth, innerHeight);
            inner.anchoredPosition = new Vector2(0, Mathf.Clamp(offset, 0, innerHeight - h));

            // 오른쪽 막대: 목록이 더 있다는 것을 보여 주고, 끌어서 움직일 수도 있다.
            RectTransform bar = Panel(name + "Bar", parent, x + w - ScrollBarWidth, y, ScrollBarWidth, h, new Color(0f, 0f, 0f, 0.45f));
            RectTransform handle = Panel("Handle", bar, 0, 0, ScrollBarWidth, h, innerHeight > h ? ButtonColor : ButtonDisabled);
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = inner;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 40f;
            scroll.verticalScrollbar = scrollbar;
            if (onScroll != null)
            {
                scroll.onValueChanged.AddListener(new Action<Vector2>(v => onScroll(inner.anchoredPosition.y)));
            }
            return inner;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
