namespace MOD_sWP6UL
{
    // 비급 지급의 시간 상한(08 문서 C7·10 문서의 11번, 묶음 4). 한 번 지급에 2초를 넘기면 거기서 멈춘다(주 스레드라 화면이 멈추지 않게).
    internal static class GiveBudget
    {
        public const long LimitMs = 2000;

        // 지금까지 걸린 ms와 만든 권수로, 다음 권을 만들기 전에 멈출지. 첫 권은 늘 시도하고, 다 만들었으면 멈출 일이 없다.
        public static bool ShouldStop(long elapsedMs, int made, int wanted)
        {
            return made > 0 && made < wanted && elapsedMs >= LimitMs;
        }

        public static string StopText(int made, int wanted)
        {
            return "시간 상한(" + (LimitMs / 1000) + "초)에 걸려 " + made + "권만 만들었습니다(요청 " + wanted + "권). 다시 누르세요";
        }
    }
}
