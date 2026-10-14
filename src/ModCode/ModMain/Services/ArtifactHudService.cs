using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 지도 화면 초상 왼쪽 아래의 신기 버튼(쌍어패·호천안·연요호)을 나란히 놓는다.
    // 게임은 신기를 하나만 가진다고 보고 셋을 같은 자리(-240,-93)에 둔다. 둘 이상 켜져 있으면 쌍어패 → 호천안 → 연요호 순서로 오른쪽으로 46씩 놓는다.
    // 아래로는 화면 끝에 잘리고 위에는 운명 표시가 있어 같은 높이에서 오른쪽으로 놓는다(05 문서 37절).
    // 전투를 다녀오면 지도 화면이 새로 만들어져 게임의 자리로 돌아가므로, 지도 화면이나 켜진 버튼이 바뀌면 다시 맞춘다. 세이브와 게임 자료는 바꾸지 않는다.
    internal static class ArtifactHudService
    {
        private const float Step = 46f;

        private static IntPtr lastMap = IntPtr.Zero;
        private static int lastMask = -1;
        private static IntPtr failedMap = IntPtr.Zero;
        private static Vector2 home;

        public static void Tick()
        {
            UIMapMain map = null;
            try
            {
                if (g.ui == null)
                {
                    return;
                }
                map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
                if (map == null || map.playerInfo == null || map.Pointer == failedMap)
                {
                    return;
                }
                var pi = map.playerInfo;
                Button[] buttons = { pi.btnPiscesPendant, pi.btnGodEye, pi.btnDevilDemon };
                int mask = 0;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i].gameObject.activeSelf)
                    {
                        mask |= 1 << i;
                    }
                }
                if (map.Pointer == lastMap && mask == lastMask)
                {
                    return;
                }
                if (map.Pointer != lastMap)
                {
                    // 새 지도 화면: 게임이 둔 자리(셋 모두 같다)를 기준으로 삼는다.
                    home = buttons[0] != null ? buttons[0].GetComponent<RectTransform>().anchoredPosition : new Vector2(-240f, -93f);
                }
                lastMap = map.Pointer;
                lastMask = mask;
                Place(buttons, mask);
            }
            catch (Exception e)
            {
                failedMap = map == null ? IntPtr.Zero : map.Pointer;
                MelonLogger.Msg("[TlToybox] 신기 버튼 배치 실패(이 지도 화면에서는 다시 하지 않음): " + e.Message);
            }
        }

        private static void Place(Button[] buttons, int mask)
        {
            int shown = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    shown++;
                }
            }
            int slot = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                {
                    continue;
                }
                var rt = buttons[i].GetComponent<RectTransform>();
                bool on = (mask & (1 << i)) != 0;
                // 하나뿐이거나 꺼진 버튼은 게임의 자리로 둔다.
                rt.anchoredPosition = on && shown > 1 ? new Vector2(home.x + Step * slot, home.y) : home;
                if (on)
                {
                    slot++;
                }
            }
        }
    }
}
