namespace MOD_sWP6UL
{
    // 숫자 입력 칸의 제출 판정(순수. 05 문서 89절, 13 문서 B1·B2). NumberField와 자원 탭의 보유 칸이 쓴다.
    //   Decide: 칸이 끝맺을 때(Enter 또는 포커스 잃음) 무엇을 할지. 떠날 때는 마지막으로 넘긴 값과 같은 수면 아무것도 하지 않는다.
    //   DecideOwned: 보유 칸. 늘리기는 Enter·떠나기 모두 적용, 줄이기는 Enter로만 묻고 같은 값의 Enter로 확인한다.
    internal static class NumberFieldRules
    {
        public enum Action { Ignore, Revert, Reject, Submit }

        // text: 칸의 글, committed: 마지막으로 넘긴 글(처음은 그린 값), enter: Enter로 끝났는지, commitOnEndEdit: 떠날 때도 넘기는 칸인지.
        public static Action Decide(string text, string committed, bool enter, bool commitOnEndEdit, out int value)
        {
            value = 0;
            if (!enter && !commitOnEndEdit)
            {
                return Action.Ignore;
            }
            if (!int.TryParse(text, out value))
            {
                return enter ? Action.Reject : Action.Revert;
            }
            int last;
            if (!enter && int.TryParse(committed, out last) && last == value)
            {
                return Action.Ignore;
            }
            return Action.Submit;
        }

        public enum OwnedAction { Ignore, Set, Ask, Confirm }

        // value: 쓴 수, held: 지금 가진 수, pending: 이 칸의 줄이기 확인이 대기 중인지, pendingValue: 그때 물은 수.
        public static OwnedAction DecideOwned(int value, int held, bool enter, bool pending, int pendingValue)
        {
            if (value == held)
            {
                return OwnedAction.Ignore;
            }
            if (value > held)
            {
                return OwnedAction.Set;
            }
            if (!enter)
            {
                return OwnedAction.Ignore;
            }
            return pending && pendingValue == value ? OwnedAction.Confirm : OwnedAction.Ask;
        }
    }
}
