using System;

namespace MOD_sWP6UL
{
    // 법보의 단계·경지 규칙(게임 형식 없이. 단위 시험이 같은 파일을 쓴다). 실측: 05 문서 82절, 스펙 2026-10-23-trainer3-design.md 4절 E2·E3.
    internal static class TreasureRules
    {
        public const int MaxGrade = 10;
        public const int MinStage = 1;
        public const int MaxStage = 6;

        // 단계마다 비율 다섯에 쓰는 대표 값. 6은 트레이너의 "한계돌파"(120).
        private static readonly int[] StageRates = { 60, 80, 85, 95, 100, 120 };

        public static int StageRate(int stage)
        {
            if (stage < MinStage || stage > MaxStage)
            {
                throw new ArgumentOutOfRangeException("stage", stage, "단계는 1~6입니다");
            }
            return StageRates[stage - 1];
        }

        // 게임이 SetAttrrRate(..., true)로 정하는 단계. 다섯 비율의 평균과 경계(실측): 60 이하 1, 80 이하 2, 90 미만 3, 100 미만 4, 110 미만 5, 그 위 6.
        public static int StageOf(int hp, int atk, int def, int soul, int sp)
        {
            double average = (hp + atk + def + soul + sp) / 5.0;
            if (average <= 60) return 1;
            if (average <= 80) return 2;
            if (average < 90) return 3;
            if (average < 100) return 4;
            if (average < 110) return 5;
            return 6;
        }

        // now에서 target(10을 넘으면 10)까지 경지 올리기를 몇 번 부르는지.
        public static int GradeSteps(int now, int target)
        {
            return Math.Max(0, Math.Min(target, MaxGrade) - now);
        }
    }
}
