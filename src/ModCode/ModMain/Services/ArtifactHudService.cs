using System;
using MelonLoader;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 게임 화면의 신기 표시(쌍어패·호천안·연요호)를 나란히 놓는다. 게임은 신기를 하나만 가진다고 보고 셋을 같은 자리에 두어, 둘 이상이면 겹친다(05 문서 37절).
    // - 지도 화면 초상 왼쪽 아래의 버튼: 게임의 자리 -240,-93. 오른쪽으로 46씩(아래로는 화면 끝에 잘리고 위에는 운명 표시가 있다).
    // - 전투 화면 오른쪽 아래의 칸: 게임의 자리 쌍어패·호천안 493,58, 연요호 490,65. 오른쪽으로 60씩. 칸은 조건이 맞을 때만 켜진다
    //   (연요호: 가지면, 호천안: 기술을 장착하면, 쌍어패: 반쪽을 준 NPC가 있으면).
    // 켜진 것이 둘 이상이면 쌍어패 → 호천안 → 연요호 순서로 놓고, 하나뿐이거나 꺼진 것은 게임의 자리에 둔다.
    // 화면이 새로 만들어지거나(전투를 다녀온 지도, 새 전투) 켜진 것이 바뀔 때만 옮긴다. 세이브와 게임 자료는 바꾸지 않는다.
    internal static class ArtifactHudService
    {
        private sealed class Row
        {
            private readonly string what;
            private readonly float step;
            private IntPtr lastScreen = IntPtr.Zero;
            private IntPtr failedScreen = IntPtr.Zero;
            private int lastMask = -1;
            private Vector2[] homes = new Vector2[0];

            public Row(string what, float step)
            {
                this.what = what;
                this.step = step;
            }

            // screen은 그 화면의 포인터(새로 만들어졌는지 가린다), items는 쌍어패·호천안·연요호 순서의 표시들.
            public void Update(IntPtr screen, GameObject[] items)
            {
                if (screen == failedScreen)
                {
                    return;
                }
                try
                {
                    int mask = 0;
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items[i] != null && items[i].activeSelf)
                        {
                            mask |= 1 << i;
                        }
                    }
                    if (screen == lastScreen && mask == lastMask)
                    {
                        return;
                    }
                    if (screen != lastScreen)
                    {
                        // 새 화면: 게임이 둔 자리를 기억한다.
                        homes = new Vector2[items.Length];
                        for (int i = 0; i < items.Length; i++)
                        {
                            homes[i] = items[i] == null ? Vector2.zero : items[i].GetComponent<RectTransform>().anchoredPosition;
                        }
                    }
                    lastScreen = screen;
                    lastMask = mask;
                    Place(items, mask);
                }
                catch (Exception e)
                {
                    failedScreen = screen;
                    MelonLogger.Msg("[TlToybox] " + what + " 신기 표시 배치 실패(이 화면에서는 다시 하지 않음): " + e.Message);
                }
            }

            private void Place(GameObject[] items, int mask)
            {
                int shown = 0;
                for (int i = 0; i < items.Length; i++)
                {
                    shown += (mask & (1 << i)) != 0 ? 1 : 0;
                }
                int slot = 0;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] == null)
                    {
                        continue;
                    }
                    var rt = items[i].GetComponent<RectTransform>();
                    bool on = (mask & (1 << i)) != 0;
                    // 칸마다 게임이 둔 높이와 첫 칸과의 가로 차이는 지킨다(연요호 칸은 첫 칸보다 3 왼쪽, 7 위).
                    rt.anchoredPosition = on && shown > 1 ? new Vector2(homes[i].x + step * slot, homes[i].y) : homes[i];
                    if (on)
                    {
                        slot++;
                    }
                }
            }
        }

        private static readonly Row MapRow = new Row("지도 화면", 46f);
        private static readonly Row BattleRow = new Row("전투 화면", 60f);

        public static void Tick()
        {
            if (g.ui == null)
            {
                return;
            }
            UIMapMain map;
            UIBattleInfo battle;
            try
            {
                map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
                battle = g.ui.GetUI<UIBattleInfo>(UIType.BattleInfo);
            }
            catch (Exception)
            {
                return;
            }
            if (map != null && map.playerInfo != null)
            {
                var pi = map.playerInfo;
                MapRow.Update(map.Pointer, new[] { Go(pi.btnPiscesPendant), Go(pi.btnGodEye), Go(pi.btnDevilDemon) });
            }
            if (battle != null && battle.info != null)
            {
                var inf = battle.info;
                BattleRow.Update(battle.Pointer, new[] { inf.goPiscesPendant, inf.goGodEye, inf.goDevilDemon });
            }
        }

        private static GameObject Go(Component c)
        {
            return c == null ? null : c.gameObject;
        }
    }
}
