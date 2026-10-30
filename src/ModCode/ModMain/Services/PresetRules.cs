using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 프리셋 적용의 순수 판정(13 문서 T5, 05 문서 93절): 익힌 공법 사본 가운데 그 줄과 같은 것 고르기.
    internal static class PresetRules
    {
        // 익힌 공법의 사본. 상태를 여러 번 물을 때 게임 객체를 다시 읽지 않도록 한 번 떠 둔다(PresetService.Snapshot이 만든다).
        public sealed class Learned
        {
            public string Sole;
            public int BaseId;
            public int Grade;
            public int[] Options;
            public int Suit;
            public bool Equipped;
        }

        public static bool SameOptions(int[] a, int[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        // 그 줄과 같은 공법: 같은 기술과 옵션, 그 경지 이상, matchSuit(심법)이면 세트 번호도 같다. 장착한 것을 먼저 고른다. 없으면 null.
        public static Learned Find(List<Learned> learned, int baseId, int[] options, int grade, bool matchSuit, int suit)
        {
            Learned found = null;
            foreach (Learned each in learned)
            {
                if (each.BaseId != baseId || each.Grade < grade || !SameOptions(each.Options, options))
                {
                    continue;
                }
                if (matchSuit && each.Suit != suit)
                {
                    continue;
                }
                if (found == null || (each.Equipped && !found.Equipped))
                {
                    found = each;
                }
            }
            return found;
        }
    }
}
