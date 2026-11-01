namespace MOD_sWP6UL
{
    // 칩(짧은 단추) 줄의 배치(순수. 13 문서 S4, 05 문서 93절). 자원·도호·배율 탭과 기술 서적 칩이 쓴다.
    internal static class ChipBar
    {
        // 칩을 왼쪽부터 놓고 areaW를 넘으면 다음 줄로. xs·ys(원점 0,0)를 채우고 전체 높이를 돌려준다. areaW가 0 이하이면 한 줄에 다 놓는다.
        public static float Layout(float[] widths, float areaW, float chipH, float gap, float[] xs, float[] ys)
        {
            float cx = 0, cy = 0;
            for (int i = 0; i < widths.Length; i++)
            {
                if (areaW > 0 && cx > 0 && cx + widths[i] > areaW)
                {
                    cx = 0;
                    cy += chipH + gap;
                }
                xs[i] = cx;
                ys[i] = cy;
                cx += widths[i] + gap;
            }
            return cy + chipH;
        }
    }
}
