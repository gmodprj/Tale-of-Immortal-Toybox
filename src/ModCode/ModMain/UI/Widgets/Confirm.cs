using System;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 두 번 눌러 확인. 되돌리기 어려운 동작은 첫 누름에 이유를 보이고, 한 번 더 누르면 실행한다.
    // 확인 대기는 창 전체에 하나만 있고, 다른 동작으로 창이 다시 그려지면 풀린다(탭 전환, 대상 변경, 하위 화면 이동 포함).
    internal static class Confirm
    {
        private static string pending;
        private static bool keep;   // 확인을 물은 바로 그 다시 그리기에서는 풀지 않는다

        public static bool IsPending(string key)
        {
            return pending == key;
        }

        // 확인을 묻는다: 대기 상태로 두고 이유를 결과 영역에 보인다.
        public static void Ask(string key, string message)
        {
            pending = key;
            keep = true;
            EditorWindow.Say(message);
        }

        public static void Clear()
        {
            pending = null;
            keep = false;
        }

        // 창이 다시 그려질 때마다 틀이 부른다.
        public static void OnRender()
        {
            if (keep)
            {
                keep = false;
                return;
            }
            pending = null;
        }

        // 확인이 필요한 버튼. 대기 중이면 confirmLabel을 보이고 강조한다.
        public static Button Button(string name, Transform parent, string label, string confirmLabel, float x, float y, float w, float h,
            string key, string message, Action action, bool enabled = true)
        {
            bool waiting = IsPending(key);
            return Ui.Button(name, parent, waiting ? confirmLabel : label, x, y, w, h, new Action(() =>
            {
                if (!IsPending(key))
                {
                    Ask(key, message);
                    return;
                }
                Clear();
                action();
            }), enabled, waiting);
        }
    }
}
