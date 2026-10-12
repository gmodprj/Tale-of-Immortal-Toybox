using System;
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
                button.onClick.AddListener(onClick);
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

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
