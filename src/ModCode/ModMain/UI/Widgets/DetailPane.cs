using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 목록 아래의 상자. 줄에서는 잘라 보이는 긴 이름과 설명을, 고른 줄에 한해 전부 보인다.
    // 글은 이 객체가 기억하므로 탭이 내용을 다시 그려도 남는다.
    internal sealed class DetailPane
    {
        private readonly string name;
        private readonly string hint;
        private string text = "";
        private Text label;

        public DetailPane(string name, string hint)
        {
            this.name = name;
            this.hint = hint;
        }

        public void Build(Transform parent, float x, float y, float w, float h)
        {
            RectTransform box = Ui.Panel(name, parent, x, y, w, h, Ui.Section);
            box.GetComponent<Image>().raycastTarget = false;
            label = Ui.Label(box, "", 12, 4, w - 24, h - 8, 16, TextAnchor.UpperLeft);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
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

        private void Apply()
        {
            if (label == null)
            {
                return;
            }
            label.text = text.Length > 0 ? text : hint;
            label.color = text.Length > 0 ? Color.white : Ui.Dim;
        }
    }
}
