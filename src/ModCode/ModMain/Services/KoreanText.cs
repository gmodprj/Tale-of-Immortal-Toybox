namespace MOD_sWP6UL
{
    // 한국어 조사 고르기(미룬 것 묶음, 05 문서 85절). 게임 형식을 쓰지 않아 단위 시험에 링크한다.
    internal static class KoreanText
    {
        // word 뒤에 목적격 조사를 붙인다. 마지막 글자가 한글 음절이면 받침으로 을/를을 고르고, 아니면(영문·숫자·빈 글) "을(를)".
        public static string Object(string word)
        {
            word = word ?? "";
            if (word.Length == 0)
            {
                return "을(를)";
            }
            char last = word[word.Length - 1];
            if (last < '가' || last > '힣')
            {
                return word + "을(를)";
            }
            return word + ((last - '가') % 28 == 0 ? "를" : "을");
        }
    }
}
