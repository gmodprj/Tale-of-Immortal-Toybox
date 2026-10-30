namespace MOD_sWP6UL
{
    // 배율 서비스가 전투 플레이어 단위 하나에 떠 둔 값(원래 쿨타임 비율·기술 값·이동 처리기)을 어떻게 다룰지 정하는 규칙(순수).
    // 스펙 docs/superpowers/specs/2026-10-30-rate-service-cleanup-design.md 2절, 13 문서 A2·A4·2.6절. RateService.Tick.cs가 프레임마다 부른다.
    internal static class RateUnitRules
    {
        public enum Step
        {
            Skip,       // 아무것도 하지 않는다(떠 둔 것은 그대로)
            Forget,     // 떠 둔 것을 지운다
            Capture,    // 지금 단위의 원래 값을 새로 뜬 뒤 적용한다
            Apply,      // 떠 둔 단위에 줄마다 쓴다(모두 꺼졌으면 되돌린 뒤 잊는다)
        }

        // inBattle: 전투 단위가 보임, anyOn: 켜진 줄이 있음, held: 떠 둔 단위가 있음, applied: 그 단위에 쓴 값이 있음,
        // same: 지금 단위가 떠 둔 단위와 같은 포인터, intact: 쓴 값이 마지막으로 쓴 것 그대로(applied가 거짓이면 참으로 준다).
        public static Step Decide(bool inBattle, bool anyOn, bool held, bool applied, bool same, bool intact)
        {
            if (!inBattle)
            {
                // 쓴 값이 남아 있으면 잊지 않는다: 같은 단위가 다시 보이면 되돌려야 한다(A4). 아니면 지운다.
                return !held || applied ? Step.Skip : Step.Forget;
            }
            if (!held)
            {
                return anyOn ? Step.Capture : Step.Skip;
            }
            if (!same || (applied && !intact))
            {
                // 앞 단위는 사라졌거나(다른 포인터), 같은 주소에 새 단위가 잡혔거나 게임이 값을 바꿨다(2.6절). 옛 원래 값을 쓰지 않는다.
                return anyOn ? Step.Capture : Step.Forget;
            }
            return Step.Apply;
        }

        // 전투 이동 속도 처리기: 켜져 있고 붙어 있지 않을 때만 붙인다(두 번 붙어 배율이 겹치지 않게, A2).
        public static bool MoveAttachNeeded(bool on, bool attached)
        {
            return on && !attached;
        }

        // 꺼져 있고 붙어 있을 때만 뗀다.
        public static bool MoveDetachNeeded(bool on, bool attached)
        {
            return !on && attached;
        }
    }
}
