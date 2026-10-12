using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnhollowerRuntimeLib;

namespace MOD_sWP6UL
{
    // 가방 칸. 게임의 용량(dynUnitData.propGridNum)에 덧셈을 건다.
    // 건 값은 저장되지 않으므로(실측) 세이브별 추가 칸을 파일에 기억하고, 세계에 들어올 때마다 다시 건다.
    // 줄여서 넘치는 아이템은 사라지지 않고 남는다(실측). 근거: docs/superpowers/specs/2026-10-12-date-bag-design.md 3.4절.
    internal static class BagService
    {
        public const int Step = 10;
        public const int MaxExtra = 500;

        private const string Tag = "TlToybox";
        private const string FileName = "bag-slots.txt";

        private static DynInt.DynObjectAddHandler handler;
        private static int applied;                    // 핸들러가 돌려주는 값
        private static Dictionary<string, int> saved;  // playerUnitID -> 추가 칸. null이면 아직 읽지 않았다
        private static int frames;
        private static bool warned;

        private static string FilePath
        {
            get { return Path.Combine(ChangeLog.DataDir, FileName); }
        }

        private static int Clamp(int value)
        {
            return Math.Max(0, Math.Min(MaxExtra, value));
        }

        // 파일을 읽어 saved를 채운다. 읽지 못하면 false와 이유를 돌려준다. 해석할 수 없는 줄은 건너뛴다.
        private static bool TryLoad(out string error)
        {
            error = null;
            if (saved != null)
            {
                return true;
            }
            try
            {
                var map = new Dictionary<string, int>();
                if (File.Exists(FilePath))
                {
                    foreach (string line in File.ReadAllLines(FilePath))
                    {
                        string trimmed = line.Trim();
                        int eq = trimmed.IndexOf('=');
                        int value;
                        if (trimmed.Length == 0 || trimmed[0] == '#' || eq <= 0 || !int.TryParse(trimmed.Substring(eq + 1).Trim(), out value))
                        {
                            continue;
                        }
                        map[trimmed.Substring(0, eq).Trim()] = Clamp(value);
                    }
                }
                saved = map;
                return true;
            }
            catch (IOException e)
            {
                error = e.Message;
                return false;
            }
            catch (UnauthorizedAccessException e)
            {
                error = e.Message;
                return false;
            }
        }

        private static void Save(Dictionary<string, int> map)
        {
            Directory.CreateDirectory(ChangeLog.DataDir);
            var lines = new List<string> { "# 세이브별 가방 추가 칸. <플레이어 단위 ID>=<칸 수>" };
            foreach (KeyValuePair<string, int> pair in map)
            {
                if (pair.Value > 0)
                {
                    lines.Add(pair.Key + "=" + pair.Value);
                }
            }
            File.WriteAllLines(FilePath, lines.ToArray());
        }

        // 현재 세이브의 추가 칸. 세계가 준비되지 않았거나 파일을 읽지 못하면 0.
        public static int Extra
        {
            get
            {
                string error;
                if (!EditService.WorldReady || !TryLoad(out error))
                {
                    return 0;
                }
                int value;
                saved.TryGetValue(g.data.world.playerUnitID, out value);
                return value;
            }
        }

        public static string Describe()
        {
            var props = g.world.playerUnit.data.unitData.propData;
            return "가방 " + props.GetGridCurNum() + "/" + props.GetGridMaxNum() + " (편집기 추가 +" + Extra + ")";
        }

        private static bool IsAttached(DynInt grid)
        {
            var calls = grid.allAddCall;
            for (int i = 0; calls != null && i < calls.Count; i++)
            {
                if (calls[i].t2 == Tag)
                {
                    return true;
                }
            }
            return false;
        }

        // 현재 플레이어의 용량에 extra를 건다. 이미 걸린 것은 떼고 새 값으로 다시 건다. 0이면 걸지 않는다.
        private static void Apply(WorldUnitBase player, int extra)
        {
            DynInt grid = player.data.dynUnitData.propGridNum;
            if (handler != null && IsAttached(grid))
            {
                grid.DelValue(handler);
            }
            applied = extra;
            if (extra <= 0)
            {
                return;
            }
            if (handler == null)
            {
                handler = DelegateSupport.ConvertDelegate<DynInt.DynObjectAddHandler>(new Func<int>(() => applied));
            }
            grid.AddValue(handler, Tag);
        }

        // 추가 칸을 delta만큼 바꾼다(0~MaxExtra). 파일에 먼저 쓰고, 쓰지 못하면 게임 값도 바꾸지 않는다.
        public static string Change(int delta)
        {
            if (!EditService.WorldReady)
            {
                return "가방: 세이브에 진입한 뒤에 사용할 수 있습니다.";
            }
            string error;
            if (!TryLoad(out error))
            {
                return "가방: 저장 파일을 읽지 못해 바꾸지 않았습니다(" + error + ").";
            }
            string key = g.data.world.playerUnitID;
            int before;
            saved.TryGetValue(key, out before);
            int after = Clamp(before + delta);
            if (after == before)
            {
                return "가방: 추가 칸은 0~" + MaxExtra + " 사이입니다(현재 +" + before + ").";
            }
            var next = new Dictionary<string, int>(saved);
            next[key] = after;
            try
            {
                Save(next);
            }
            catch (IOException e)
            {
                return "가방: 저장 파일을 쓰지 못해 바꾸지 않았습니다(" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "가방: 저장 파일을 쓰지 못해 바꾸지 않았습니다(" + e.Message + ").";
            }
            saved = next;

            WorldUnitBase player = g.world.playerUnit;
            var props = player.data.unitData.propData;
            int maxBefore = props.GetGridMaxNum();
            Apply(player, after);
            int maxAfter = props.GetGridMaxNum();
            bool verified = maxAfter - maxBefore == after - before;
            ChangeLog.Add(EditService.NameOf(player), "가방 추가 칸", "+" + before, "+" + after, verified);
            string text = "가방: 추가 칸 +" + before + " -> +" + after + " (용량 " + maxBefore + " -> " + maxAfter + ")";
            if (!verified)
            {
                text += " 용량이 기대한 만큼 바뀌지 않았습니다.";
            }
            int used = props.GetGridCurNum();
            if (used > maxAfter)
            {
                text += " 가방이 넘칩니다(" + used + "/" + maxAfter + "). 아이템은 남아 있고, 비우기 전에는 새 아이템을 받을 수 없습니다.";
            }
            return text;
        }

        // 걸려 있어야 할 추가 칸이 빠져 있으면 다시 건다. 세이브에 들어온 직후와 인물 데이터가 새로 만들어진 경우를 처리한다.
        public static void EnsureApplied()
        {
            try
            {
                if (!EditService.WorldReady)
                {
                    return;
                }
                WorldUnitBase player = g.world.playerUnit;
                if (player.data == null || player.data.dynUnitData == null || player.data.dynUnitData.propGridNum == null)
                {
                    return;
                }
                int extra = Extra;
                bool attached = IsAttached(player.data.dynUnitData.propGridNum);
                if (extra > 0 && (!attached || applied != extra))
                {
                    Apply(player, extra);
                }
                else if (extra == 0 && attached)
                {
                    Apply(player, 0);
                }
            }
            catch (Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    MelonLogger.Msg("[TlToybox] 가방 추가 칸을 적용하지 못했습니다: " + e.Message);
                }
            }
        }

        // 매 프레임 불린다. 60프레임마다 한 번 EnsureApplied를 실행한다.
        public static void Tick()
        {
            frames++;
            if (frames < 60)
            {
                return;
            }
            frames = 0;
            EnsureApplied();
        }
    }
}
