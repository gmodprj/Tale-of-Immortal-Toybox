namespace MOD_sWP6UL
{
    // 모드 공법 목록 정리의 판정 규칙(게임 형식 없이 시험한다. 묶음 2, 05 문서 78절).
    internal static class ModSkillRules
    {
        // M1: 이름 키가 LocalText에 없을 때 그 글을 이름으로 쓸지. 모드가 이름 칸에 번역 키 대신 이름 글자를 그대로 넣은 경우(52개).
        // 비지 않고, "0"이 아니고, 자리 표시({…})를 품지 않고, 영문·숫자·밑줄만으로 된 키 꼴이 아니면 이름이다.
        // 키 꼴은 원래 글로 본다(서식 태그가 든 이름은 키 꼴이 아니다).
        public static bool IsLiteralName(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey) || nameKey == "0" || nameKey.IndexOf('{') >= 0)
            {
                return false;
            }
            foreach (char c in nameKey)
            {
                bool keyChar = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
                if (!keyChar)
                {
                    return true;
                }
            }
            return false;
        }

        // M2: 이름 키가 비었거나 "0"이고 설명도 없는 모드 행(459개). 무엇인지 알 수 없어 목록에서 숨긴다(사용자 결정 2026-10-23).
        public static bool IsBlankRow(string nameKey, string descText)
        {
            bool noName = string.IsNullOrEmpty(nameKey) || nameKey == "0";
            bool noDesc = string.IsNullOrEmpty(descText) || descText == "0";
            return noName && noDesc;
        }

        // M4: 모드 공법의 비급이 만들어지는 경지 범위. 못 만들면 0~0.
        // 실측(회차 msl-1023c, 경지 1·2·5·10, 10번 따로): 옵션 행이 있으면 절기·신통은 1~10, 무예/영술은 경지 1에서만 실패(2~10),
        // 신법은 어느 경지·어느 길로도 읽히지 않았다. 옵션 행이 0개면(이름 없는 심법 435 등) 만들 수 없다.
        // kindIndex는 SkillCatalog.Kinds의 순번(0 무예/영술, 1 절기, 2 신법, 3 심법, 4 신통).
        // multiSum은 옵션 행마다 min(multi, 11)의 합: 남는 칸을 같은 번호 되풀이로 채울 수 있는 칸 수. 비급의 옵션 칸은 많아야 11이므로
        // 11보다 작으면 칸을 다 못 채울 수 있어 못 만듦으로 본다(실측된 성공 97행은 모두 11 이상, 최종 리뷰 Important 1).
        public const int MaxSlots = 11;

        public static void BookGrades(int kindIndex, int optionRows, int multiSum, out int min, out int max)
        {
            min = 0;
            max = 0;
            if (optionRows <= 0 || kindIndex == 2 || multiSum < MaxSlots)
            {
                return;
            }
            min = kindIndex == 0 ? 2 : 1;
            max = 10;
        }
    }
}
