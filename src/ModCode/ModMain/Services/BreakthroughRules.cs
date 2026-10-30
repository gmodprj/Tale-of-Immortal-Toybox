using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 대경지 돌파 재료 고르기의 순수 판정(13 문서 T6, 05 문서 93절). 게임 행(ConfRoleGradeItem)의 칸 값을 받는다.
    internal static class BreakthroughRules
    {
        // "번호_번호_…" 글을 번호 목록으로. 0 이하·숫자 아님·중복은 뺀다.
        public static List<int> ParseIds(string text)
        {
            var result = new List<int>();
            foreach (string part in (text ?? "").Split('_'))
            {
                int id;
                if (int.TryParse(part, out id) && id > 0 && !result.Contains(id))
                {
                    result.Add(id);
                }
            }
            return result;
        }

        // 돌파 방식 하나의 "필요 재료" 칸에 넣을 재료. 종류(itemShowA)에서 고른다.
        // 한 개만 필요하면 가장 좋은 것(표의 마지막), 여러 개면 서로 다른 종류를 앞에서부터 하나씩(모자라면 순환).
        public static List<int> NeedItems(string itemShowA, int growItemNum)
        {
            var kinds = ParseIds(itemShowA);
            var result = new List<int>();
            if (growItemNum <= 0 || kinds.Count == 0)
            {
                return result;
            }
            if (growItemNum == 1)
            {
                result.Add(kinds[kinds.Count - 1]);
                return result;
            }
            for (int i = 0; i < growItemNum; i++)
            {
                result.Add(kinds[i % kinds.Count]);
            }
            return result;
        }

        // "천재지보" 칸에 넣을 것. 후보(itemShowB)에서 서로 다른 종류를 앞에서부터 필요한 수만큼.
        public static List<int> Treasures(string itemShowB, int materialsNum)
        {
            var kinds = ParseIds(itemShowB);
            var result = new List<int>();
            for (int i = 0; i < materialsNum && kinds.Count > 0; i++)
            {
                result.Add(kinds[i % kinds.Count]);
            }
            return result;
        }
    }
}
