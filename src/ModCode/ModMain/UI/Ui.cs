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
                    var texts = Resources.FindObjectsOfTypeAll<Text>();
                    for (int i = 0; i < texts.Length && font == null; i++)
                    {
                        font = texts[i].font;
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

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }
    }
}
