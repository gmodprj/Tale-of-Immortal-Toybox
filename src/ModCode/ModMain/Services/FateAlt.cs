namespace MOD_sWP6UL
{
    // 프리셋 역천개명 줄의 대체 번호 고르기(05 문서 73절). 게임 형식을 쓰지 않는다(단위 시험 tests/ModMain.Tests).
    // 만고신화(2814696167)는 혈염대법·무예정통 같은 역천개명을 경지 1~1로 막고 같은 계열의 새 것(탄천마공·무법어도…)을 연다.
    // 프리셋은 줄마다 "alt"를 둘 수 있고, 원래 번호를 그 경지에서 쓸 수 없을 때만 대체 번호를 쓴다. 바닐라에서는 원래 번호 그대로다.
    internal static class FateAlt
    {
        public static int Pick(int grade, int id, int min, int max, bool exists, int alt, int altMin, int altMax, bool altExists)
        {
            if (exists && min <= grade && grade <= max)
            {
                return id;
            }
            if (alt != 0 && altExists && altMin <= grade && grade <= altMax)
            {
                return alt;
            }
            return id;
        }
    }
}
