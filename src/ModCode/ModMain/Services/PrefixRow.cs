namespace MOD_sWP6UL
{
    // 옵션 행(게임의 ConfBattleSkillPrefixValueItem) 가운데 판정에 쓰는 것만 떼어 낸 레코드. 순수 판정(ModSkillRules·MartialRules)이
    // 게임 형식 없이 돌게 한다(05 문서 93절). 게임 행에서 만드는 것은 MartialService.RowOf.
    internal sealed class PrefixRow
    {
        public int Number;        // 옵션 번호
        public int Multi;         // 같은 번호를 둘 수 있는 수(중복 한도)
        public int[] Weights;     // 자리 1~11의 가중치(weight1~weight11). 0이면 그 자리에 나올 수 없다
        public int[] Requires;    // 선행(양수)·배타(음수) 조건

        // 그 자리의 가중치. 자리가 범위 밖이면 0.
        public int Weight(int index)
        {
            return Weights != null && index >= 0 && index < Weights.Length ? Weights[index] : 0;
        }
    }
}
