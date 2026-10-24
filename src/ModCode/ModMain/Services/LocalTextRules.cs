namespace MOD_sWP6UL
{
    // 번역 적용의 판정 규칙(게임 형식 없이 시험한다. 성능 1 계획 Task 4).
    internal static class LocalTextRules
    {
        // 모드 행(LocalText isModExtend)의 새 kr. 바꾸지 않으면 null.
        // 번역표에 있으면 그 번역, 없고 kr이 비었거나 "0"이면 ch(중국어), 그 밖은 그대로(43절).
        public static string ModKr(string kr, string ch, string translated)
        {
            if (translated != null)
            {
                return translated;
            }
            if ((string.IsNullOrEmpty(kr) || kr == "0") && !string.IsNullOrEmpty(ch))
            {
                return ch;
            }
            return null;
        }

        // ch→kr 전체 일치 사전에 넣을 줄인지: ch에 한자가 있고 kr이 한국어(비지 않음, "0" 아님, ch와 다름, 한자 없음). 54절.
        // 같은 ch의 중복 거르기(앞의 행)는 부르는 쪽이 한다.
        public static bool KeepsWhole(string ch, string kr)
        {
            return !string.IsNullOrEmpty(ch) && !string.IsNullOrEmpty(kr) && kr != ch && kr != "0"
                && TextReplacer.HasChinese(ch) && !TextReplacer.HasChinese(kr);
        }
    }
}
