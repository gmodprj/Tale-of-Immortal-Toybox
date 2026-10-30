using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 공법 옵션 목록의 규칙(순수. 13 문서 T4, 05 문서 93절). 문구는 MartialService.OptionProblem이 쓰던 것 그대로(검사가 읽는다).
    internal static class MartialRules
    {
        public const int MaxOptions = 11;
        public const string SlotReason = "이 자리에는 나올 수 없는 옵션";

        // 옵션 번호 목록(자리 순서)을 그 기술에 쓸 수 없는 이유. 쓸 수 있으면 null.
        // 규칙: 그 기술의 번호, 자리의 가중치, 중복 한도, 선행(양수)·배타(음수) 조건.
        // anySlot: 자리 판정만 건너뛴다. slotFrom: 자리 판정을 이 자리부터만 한다.
        public static string OptionProblem(int[] options, List<PrefixRow> rows, bool anySlot, int slotFrom)
        {
            if (options.Length > MaxOptions)
            {
                return "옵션은 " + MaxOptions + "개까지입니다";
            }
            var numbers = new List<int>(options);
            for (int i = 0; i < options.Length; i++)
            {
                int number = options[i];
                PrefixRow row = rows.Find(r => r.Number == number);
                if (row == null)
                {
                    return "이 기술에 없는 옵션입니다(" + number + ")";
                }
                if (!anySlot && i >= slotFrom && row.Weight(i) <= 0)
                {
                    return "옵션 " + (i + 1) + ": " + SlotReason;
                }
                if (numbers.FindAll(n => n == number).Count > row.Multi)
                {
                    return "옵션 " + (i + 1) + ": 중복 한도 " + row.Multi + "개 초과";
                }
                for (int k = 0; row.Requires != null && k < row.Requires.Length; k++)
                {
                    int require = row.Requires[k];
                    if (require > 0 && !numbers.Contains(require))
                    {
                        return "옵션 " + number + "번에 필요한 " + require + "번 옵션이 없음";
                    }
                    if (require < 0 && numbers.Contains(-require))
                    {
                        return "옵션 " + number + "번과 " + (-require) + "번은 함께 둘 수 없음";
                    }
                }
            }
            return null;
        }
    }
}
