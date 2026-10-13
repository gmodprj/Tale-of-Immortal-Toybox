using System;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 배율 탭: 플레이어의 전투 보조(무적, 주는 피해)와 획득·소모의 배율.
    // 모두 저장하지 않으며 게임을 켤 때마다 꺼진 상태로 시작한다.
    internal sealed class RateTab : EditorTab
    {
        private const float RowH = 64;

        public override string Title
        {
            get { return "배율"; }
        }

        protected override void Draw()
        {
            Ui.Panel("RateBox", content, 0, 0, ContentW, 60 + 8 * RowH + 16, Ui.Section);
            Ui.Label(content, "배율", 16, 6, 200, 36, 21);
            Ui.Button("RateReset", content, "모두 1배로", 100, 6, 150, 38, new Action(() => Say(RateService.Reset())));
            Ui.Label(content, "게임을 다시 켜면 모두 꺼진 상태로 시작합니다. 켜 둔 것은 화면 위쪽 가운데에 표시됩니다.", 270, 10, 1010, 30, 15).color = Ui.Dim;
            float y = 60;
            foreach (RateService.Rate each in RateService.All)
            {
                RateService.Rate rate = each;
                if (rate == RateService.Taken)
                {
                    continue;
                }
                string note = rate == RateService.Move && TimeService.Frozen
                    ? "날짜 고정 중에는 날짜가 흐르지 않습니다. 이 배율은 고정을 끈 뒤에 적용됩니다."
                    : rate.Note;
                y = Row(y, "Rate" + char.ToUpperInvariant(rate.Key[0]) + rate.Key.Substring(1), rate.Label, rate.Text, rate.On, note,
                    new Action(() => Say(RateService.Next(rate))));
            }
            y = Row(y, "Multiplier", "주는 피해", "x" + BattleService.Multiplier, BattleService.Multiplier > 1,
                "플레이어가 주는 피해를 곱합니다. 동료와 적끼리의 피해는 그대로입니다.",
                new Action(() => Say(BattleService.NextMultiplier())));
            RateService.Rate taken = RateService.Taken;
            y = Row(y, "RateTaken", taken.Label, taken.Text, taken.On, taken.Note, new Action(() => Say(RateService.Next(taken))));
            Row(y, "God", "무적", BattleService.God ? "켜짐" : "꺼짐", BattleService.God,
                "플레이어가 받는 피해를 0으로 합니다.",
                new Action(() => Say(BattleService.SetGod(!BattleService.God))));
        }

        // 줄 하나: 이름, 값 버튼, 설명. 다음 줄의 y를 돌려준다.
        // 설명 글에는 이름을 붙인다(검증이 날짜 고정 중의 안내를 읽는다).
        private float Row(float y, string name, string label, string value, bool on, string note, Action onClick)
        {
            Ui.Label(content, label, 16, y + 10, 220, 40, 19);
            Ui.Button(name, content, value, 240, y + 8, 150, 44, onClick, true, on);
            UnityEngine.UI.Text text = Ui.Wrapped(content, note, 410, y + 8, ContentW - 430, 44, 15);
            text.color = Ui.Dim;
            text.gameObject.name = name.StartsWith("Rate") ? "RateNote" + name.Substring(4) : name + "Note";
            return y + RowH;
        }
    }
}
