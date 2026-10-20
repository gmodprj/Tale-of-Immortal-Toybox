using System.Text.RegularExpressions;

namespace MOD_sWP6UL
{
    // 모드 글의 서식 태그(<COLOR=#…>·</color>·<b>·<size=…>)를 지운다(05 문서 72절). 게임 형식을 쓰지 않는다(단위 시험 tests/ModMain.Tests).
    // 태그는 '<' 바로 뒤가 영문자이거나 '/'인 것만 본다. "a < b"처럼 띄운 부등호는 남긴다.
    internal static class TextTags
    {
        private static readonly Regex Tag = new Regex(@"</?[A-Za-z][^<>]*>");

        public static string Strip(string text)
        {
            return string.IsNullOrEmpty(text) ? "" : Tag.Replace(text, "");
        }
    }
}
