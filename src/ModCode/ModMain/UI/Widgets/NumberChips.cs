using System;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 번호 칩 한 줄(경지 1~10, 단계 1~6. 13 문서 S5). 이름은 prefix + 번호, 간격은 step(폭이 달라도 자리는 step마다).
    internal static class NumberChips
    {
        public static void Draw(Transform parent, string prefix, int from, int to, float x, float y, float w, float step, float h,
            int selected, Action<int> onPick, Func<int, string> label = null, Func<int, float> widthOf = null)
        {
            float cx = x;
            for (int n = from; n <= to; n++)
            {
                int value = n;
                Ui.Button(prefix + n, parent, label == null ? n.ToString() : label(n), cx, y, widthOf == null ? w : widthOf(n), h,
                    new Action(() => onPick(value)), true, selected == n);
                cx += step;
            }
        }
    }
}
