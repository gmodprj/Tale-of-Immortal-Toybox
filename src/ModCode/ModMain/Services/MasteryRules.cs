using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 수련 단계 대도까지 채우기의 게임 형식 없는 규칙(단위 시험이 같은 파일을 쓴다). 스펙 2026-10-23-mastery-fill-design.md, 05 문서 83절.
    // 공법 값 배열(실측): [2, 0, 1, 종류, 기본 번호, 경지, 옵션 번호…, -1, 옵션 수치…]. 옵션이 없으면 -1과 수치가 없다.
    internal static class MasteryRules
    {
        public const int MaxOptions = 11;
        private const int FirstOption = 6;

        // 옵션 목록 끝(첫 -1, 없으면 배열 끝)에 add를 끼우고, 수치 칸 끝에 1을 add 수만큼 더한 새 배열.
        public static int[] InsertOptions(int[] values, int[] add)
        {
            var list = new List<int>(values);
            if (add.Length == 0)
            {
                return list.ToArray();
            }
            int sep = list.IndexOf(-1, Math.Min(FirstOption, list.Count));
            if (sep < 0)
            {
                list.AddRange(add);
                list.Add(-1);
            }
            else
            {
                list.InsertRange(sep, add);
            }
            for (int i = 0; i < add.Length; i++)
            {
                list.Add(1);
            }
            return list.ToArray();
        }

        // 값 배열의 옵션 번호(자리 순서).
        public static int[] OptionsOf(int[] values)
        {
            var result = new List<int>();
            for (int i = FirstOption; i < values.Length && values[i] != -1; i++)
            {
                result.Add(values[i]);
            }
            return result.ToArray();
        }

        // r(0 이상 1 미만)을 가중치 누적에 비례해 고른 칸. 0 이하는 빼고, 모두 0이면 -1.
        public static int PickWeighted(IList<int> weights, double r)
        {
            long total = 0;
            foreach (int w in weights)
            {
                if (w > 0)
                {
                    total += w;
                }
            }
            if (total == 0)
            {
                return -1;
            }
            double target = r * total;
            long run = 0;
            int last = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0)
                {
                    continue;
                }
                run += weights[i];
                last = i;
                if (target < run)
                {
                    return i;
                }
            }
            return last;
        }
    }
}
