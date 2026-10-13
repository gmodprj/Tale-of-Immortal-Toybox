using System;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 즉시 검색 입력 칸. 글이 바뀔 때마다 앞뒤 공백을 뗀 검색어를 넘긴다.
    // 넘겨받은 쪽은 탭 전체가 아니라 목록만 다시 그려야 한다(ListView.SetItems). 탭을 다시 그리면 이 칸이 지워져 포커스를 잃는다.
    internal static class SearchBox
    {
        private const float ClearW = 40;

        // 입력 칸의 이름은 name, 지움 버튼의 이름은 name + "Clear".
        public static InputField Build(Transform parent, string name, float x, float y, float w, string value, string placeholder, Action<string> onChanged)
        {
            InputField input = Ui.Input(name, parent, value, placeholder, x, y, w - ClearW - 4, 38, new Action<string>(v => onChanged(v.Trim())));
            // 글을 비우면 위의 콜백이 빈 검색어로 불린다.
            Ui.Button(name + "Clear", parent, "×", x + w - ClearW, y, ClearW, 38, new Action(() => input.text = ""));
            return input;
        }
    }
}
