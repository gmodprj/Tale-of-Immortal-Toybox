using System;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 숫자 입력 칸. 숫자를 쓰고 Enter를 누르면 onSubmit으로 넘긴다.
    // 입력 칸의 끝맺음 이벤트는 다른 곳을 눌러 포커스를 잃을 때도 온다. 그때는 적용하지 않도록 Enter 키인지 확인한다(실측: 스펙 3.4절).
    // commitOnEndEdit가 참이면 포커스를 잃을 때도 넘긴다(즐겨찾기의 수량 칸. 05 문서 55절). 그때 숫자가 아니면 마지막으로 넘긴 값으로 되돌린다.
    internal static class NumberField
    {
        public static InputField Build(Transform parent, string name, float x, float y, float w, float h, string text,
            Action<string> onChanged, Action<int> onSubmit)
        {
            return Build(parent, name, x, y, w, h, text, onChanged, onSubmit, false);
        }

        public static InputField Build(Transform parent, string name, float x, float y, float w, float h, string text,
            Action<string> onChanged, Action<int> onSubmit, bool commitOnEndEdit)
        {
            InputField input = Ui.Input(name, parent, text, "", x, y, w, h, onChanged, true);
            string committed = text;
            input.onEndEdit.AddListener(new Action<string>(v =>
            {
                bool enter = UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter);
                if (!enter && !commitOnEndEdit)
                {
                    return;
                }
                int value;
                if (!int.TryParse(v, out value))
                {
                    if (enter)
                    {
                        EditorWindow.Say("숫자를 입력하세요.");
                    }
                    else
                    {
                        input.text = committed;
                    }
                    return;
                }
                committed = v;
                onSubmit(value);
            }));
            return input;
        }
    }
}
