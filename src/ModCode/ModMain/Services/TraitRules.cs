namespace MOD_sWP6UL
{
    // 성격 칸에 넣을 수 있는지(docs/superpowers/specs/2026-10-20-trainer-batch1-design.md 6절, 05 문서 65절).
    // 성격 표(roleCreateCharacter)의 type 1이 내재(1~7), 2가 외재(8~19). 게임은 같은 외재 둘도 받지만 편집기가 막는다.
    // 게임 형식을 쓰지 않는다(단위 시험 tests/ModMain.Tests). 표를 읽는 것은 EditService.SetTrait.
    internal static class TraitRules
    {
        public enum Slot { In, Out1, Out2 }

        public const int InnerType = 1;
        public const int OuterType = 2;

        // type: 그 번호의 표 type(표에 없으면 0). other: 다른 외재 칸의 값(내재 칸이면 쓰지 않는다).
        // 넣을 수 있으면 null, 아니면 이유.
        public static string Check(Slot slot, int id, int type, int other)
        {
            if (type != InnerType && type != OuterType)
            {
                return "표에 없는 성격입니다";
            }
            if (slot == Slot.In)
            {
                return type == InnerType ? null : "내재 칸에는 내재 성격만 넣을 수 있습니다";
            }
            if (type != OuterType)
            {
                return "외재 칸에는 외재 성격만 넣을 수 있습니다";
            }
            return id == other ? "다른 외재 칸과 같은 성격입니다" : null;
        }
    }
}
