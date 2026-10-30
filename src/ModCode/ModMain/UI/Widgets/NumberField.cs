using System;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 숫자 입력 칸. 숫자를 쓰고 Enter를 누르면 onSubmit으로 넘긴다.
    // 입력 칸의 끝맺음 이벤트는 다른 곳을 눌러 포커스를 잃을 때도 온다. 그때는 적용하지 않도록 Enter 키인지 확인한다(실측: 스펙 3.4절).
    // commitOnEndEdit가 참이면 포커스를 잃을 때도 넘긴다(즐겨찾기의 수량 칸. 05 문서 55절). 그때 숫자가 아니면 마지막으로 넘긴 값으로 되돌리고,
    // 마지막으로 넘긴 값과 같은 수면 넘기지 않는다(칸을 눌렀다 떠나기만 해도 다시 그려지던 것. 13 문서 B1). 판정은 NumberFieldRules.Decide.
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
            return Build(parent, name, x, y, w, h, text, onChanged, new Action<int, bool>((v, enter) => onSubmit(v)), commitOnEndEdit);
        }

        // onSubmit의 둘째 인자는 Enter로 끝났는지(거짓이면 포커스를 잃어 끝남).
        public static InputField Build(Transform parent, string name, float x, float y, float w, float h, string text,
            Action<string> onChanged, Action<int, bool> onSubmit, bool commitOnEndEdit)
        {
            InputField input = Ui.Input(name, parent, text, "", x, y, w, h, onChanged, true);
            string committed = text;
            input.onEndEdit.AddListener(new Action<string>(v =>
            {
                bool enter = UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter);
                int value;
                switch (NumberFieldRules.Decide(v, committed, enter, commitOnEndEdit, out value))
                {
                    case NumberFieldRules.Action.Reject:
                        EditorWindow.Say("숫자를 입력하세요.");
                        return;
                    case NumberFieldRules.Action.Revert:
                        input.text = committed;
                        return;
                    case NumberFieldRules.Action.Submit:
                        committed = v;
                        onSubmit(value, enter);
                        return;
                    default:
                        return;
                }
            }));
            return input;
        }
    }
}
