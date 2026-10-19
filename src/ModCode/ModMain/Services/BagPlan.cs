using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 가방에서 한 아이템의 수를 줄이는 순서(docs/superpowers/specs/2026-10-20-trainer-batch1-design.md 6절, 05 문서 65절).
    // 게임의 DelProps(번호, 수)는 가방 순서대로 지워 장착품부터 지울 수 있다. 그래서 줄(sole)마다 지울 수를 정한다.
    // 게임 형식을 쓰지 않는다(단위 시험 tests/ModMain.Tests). 가방을 읽어 입력을 만드는 것은 BagService.
    internal static class BagPlan
    {
        // 가방의 한 줄. Strength는 법보의 level+grade+옵션 수, 그 밖은 0.
        public sealed class Stack
        {
            public string Sole;
            public int Count;
            public bool Equipped;
            public int Strength;
        }

        // 지울 것 하나. All이면 그 줄을 통째로(DelProps(sole)), 아니면 Count개만(DelProps(sole, Count)).
        public sealed class Cut
        {
            public string Sole;
            public int Count;
            public bool All;
        }

        // remove개를 줄이는 계획. 장착하지 않은 줄부터 줄인다. 겹치지 않는 아이템은 약한 것부터(같으면 입력 순서),
        // 겹치는 아이템은 입력 순서로. 그래도 남으면 장착한 겹치는 묶음을 1개까지만 줄인다(전투 아이템 칸에서 빠지지 않게).
        // 장착한 겹치지 않는 아이템은 줄이지 않는다. kept는 장착품 때문에 줄이지 못한 수.
        public static List<Cut> Reduce(List<Stack> stacks, bool overlay, int remove, out int kept)
        {
            var cuts = new List<Cut>();
            var free = new List<Stack>();
            var equipped = new List<Stack>();
            foreach (Stack s in stacks)
            {
                (s.Equipped ? equipped : free).Add(s);
            }
            if (!overlay)
            {
                // List.Sort는 안정 정렬이 아니므로 입력 순서를 두 번째 열쇠로 쓴다.
                var order = new Dictionary<Stack, int>();
                for (int i = 0; i < free.Count; i++)
                {
                    order[free[i]] = i;
                }
                free.Sort((x, y) => x.Strength != y.Strength ? x.Strength.CompareTo(y.Strength) : order[x].CompareTo(order[y]));
            }
            int left = remove;
            foreach (Stack s in free)
            {
                if (left <= 0)
                {
                    break;
                }
                if (s.Count <= left)
                {
                    cuts.Add(new Cut { Sole = s.Sole, Count = s.Count, All = true });
                    left -= s.Count;
                }
                else
                {
                    cuts.Add(new Cut { Sole = s.Sole, Count = left });
                    left = 0;
                }
            }
            if (overlay)
            {
                foreach (Stack s in equipped)
                {
                    int take = System.Math.Min(left, s.Count - 1);
                    if (take > 0)
                    {
                        cuts.Add(new Cut { Sole = s.Sole, Count = take });
                        left -= take;
                    }
                }
            }
            kept = left;
            return cuts;
        }

        // 장착하지 않은 줄 가운데 가장 약한 것보다 센 것의 수(버리기 확인 문구의 "강화·옵션이 붙은 것 n개").
        public static int StrongCount(List<Stack> stacks)
        {
            int min = int.MaxValue;
            foreach (Stack s in stacks)
            {
                if (!s.Equipped && s.Strength < min)
                {
                    min = s.Strength;
                }
            }
            int n = 0;
            foreach (Stack s in stacks)
            {
                if (!s.Equipped && s.Strength > min)
                {
                    n++;
                }
            }
            return n;
        }
    }
}
