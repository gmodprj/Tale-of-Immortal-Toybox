using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 수량을 고르는 줄: 1/10/100/1000 버튼과 직접 입력 칸.
    // 값은 쓰는 곳(열쇠)마다 따로 기억한다. 저장하지 않으므로 게임을 다시 켜면 기본값이다.
    //   열쇠: attr(능력치 증감), martialExp(숙련 경험치), move(순간 이동 칸 수), intim(호감도 증감), money(영석), item(아이템 지급 수량)
    internal static class AmountBar
    {
        public const int Max = 999999;

        private static readonly int[] Amounts = { 1, 10, 100, 1000 };
        private static readonly Dictionary<string, int> values = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> typed = new Dictionary<string, string>();

        public static int Get(string key)
        {
            int value;
            return values.TryGetValue(key, out value) ? value : (key == "item" ? 1 : 10);
        }

        // 수량 줄을 그리고 오른쪽 끝의 x를 돌려준다. 요소의 이름은 열쇠로 시작한다(attr10, attrInput, attrApply).
        public static float Build(Transform parent, string key, float x, float y)
        {
            int current = Get(key);
            for (int i = 0; i < Amounts.Length; i++)
            {
                int amount = Amounts[i];
                Ui.Button(key + amount, parent, amount.ToString(), x + i * 66, y, 62, 38,
                    new Action(() => { typed.Remove(key); values[key] = amount; EditorWindow.ClearMessage(); EditorWindow.Refresh(); }), true, current == amount);
            }
            float inputX = x + Amounts.Length * 66 + 6;
            string text;
            if (!typed.TryGetValue(key, out text))
            {
                text = Array.IndexOf(Amounts, current) < 0 ? current.ToString() : "";
            }
            Action<int> apply = value =>
            {
                if (value < 1 || value > Max)
                {
                    EditorWindow.Say("수량은 1~" + Max + " 사이의 숫자로 입력하세요.");
                    return;
                }
                values[key] = value;
                EditorWindow.ClearMessage();
                EditorWindow.Refresh();
            };
            NumberField.Build(parent, key + "Input", inputX, y, 130, 38, text, new Action<string>(v => typed[key] = v), apply);
            Ui.Button(key + "Apply", parent, "적용", inputX + 136, y, 70, 38, new Action(() =>
            {
                string entered;
                int value;
                typed.TryGetValue(key, out entered);
                if (!int.TryParse(entered, out value))
                {
                    value = 0;
                }
                apply(value);
            }));
            return inputX + 136 + 70;
        }
    }
}
