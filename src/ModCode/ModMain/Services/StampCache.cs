using System;

namespace MOD_sWP6UL
{
    // 표지(파일 목록·크기·수정 시각 등)가 지난번과 같으면 지난 값을 돌려주는 캐시(묶음 3, 프리셋 탭).
    // 표지는 읽기 **앞에서** 잰다: 읽는 동안 밖에서 파일이 바뀌면 다음 호출의 표지가 달라 다시 읽는다(최종 리뷰).
    // 읽기가 스스로 파일을 쓰는 경우(새 기본 프리셋 풀기)에는 다음 호출에서 한 번 더 읽고 그 뒤로 안정된다.
    internal sealed class StampCache<T> where T : class
    {
        private string stamp;
        private T value;

        public T Get(Func<string> stampOf, Func<T> load)
        {
            string now = stampOf();
            if (value != null && now == stamp)
            {
                return value;
            }
            value = load();
            stamp = now;
            return value;
        }

        public void Forget()
        {
            stamp = null;
            value = null;
        }
    }
}
