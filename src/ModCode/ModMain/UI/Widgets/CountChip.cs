using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 수가 적힌 칩·단추의 글 요소. 검색어가 바뀌면 탭을 다시 그리지 않고(검색 칸의 포커스를 지킨다) 이 글만 고친다(13 문서 S4).
    internal sealed class CountChip
    {
        public Text Text;
        public string Name;
        public string Key;      // 그룹 단추면 그룹 키(도호 탭은 분류 키), 종류 칩이면 null
        public int Index;       // 종류 칩의 순번(0이 전체)

        public void Set(string text)
        {
            Text.text = text;
        }
    }
}
