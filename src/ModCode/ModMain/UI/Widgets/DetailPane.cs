using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 목록 아래의 상자. 줄에서는 다 보이지 않는 이름과 설명을, 고른 줄에 한해 전부 보인다.
    // 글이 상자보다 길면 상자 안에서 스크롤한다(잘리지 않는다). 글은 이 객체가 기억하므로 탭이 내용을 다시 그려도 남는다.
    // 글의 요소 이름은 name + "Text", 스크롤 영역은 name + "Scroll"이다.
    internal sealed class DetailPane
    {
        private const int Size = 16;
        private readonly string name;
        private readonly string hint;
        private string text = "";
        private RectTransform box;
        private float width;
        private float height;

        public DetailPane(string name, string hint)
        {
            this.name = name;
            this.hint = hint;
        }

        public void Build(Transform parent, float x, float y, float w, float h)
        {
            width = w;
            height = h;
            box = Ui.Panel(name, parent, x, y, w, h, Ui.Section);
            box.GetComponent<Image>().raycastTarget = false;
            Apply();
        }

        public void Show(string value)
        {
            text = value ?? "";
            Apply();
        }

        public void Clear()
        {
            Show("");
        }

        // 글을 다시 놓는다. 스크롤 막대의 폭을 늘 비워 두고 재므로, 스크롤이 생겨도 줄바꿈 자리가 바뀌지 않는다.
        private void Apply()
        {
            if (box == null)
            {
                return;
            }
            Ui.Clear(box);
            string shown = text.Length > 0 ? text : hint;
            float textW = width - 24 - Ui.ScrollBarWidth;
            float textH = Ui.TextHeight(shown, textW, Size) + 4;
            float needed = textH + 8;
            Transform area = needed > height
                ? Ui.Scroll(name + "Scroll", box, 0, 0, width, height, needed)
                : (Transform)box;
            Text label = Ui.Wrapped(area, shown, 12, 4, textW, textH, Size, TextAnchor.UpperLeft);
            label.gameObject.name = name + "Text";
            label.color = text.Length > 0 ? Color.white : Ui.Dim;
        }
    }
}
