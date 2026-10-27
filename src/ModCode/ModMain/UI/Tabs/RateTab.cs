using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 배율 탭: 플레이어의 전투 보조(무적, 주는 피해)와 획득·소모·속도의 배율.
    // 모두 저장하지 않으며 게임을 켤 때마다 꺼진 상태로 시작한다.
    // 줄이 18개라 분류 칩(성장·시간·속도·전투·관계·소모·켜 둔 것)으로 나눈다(05 문서 86절, 사용자 결정 2026-10-27).
    internal sealed class RateTab : EditorTab
    {
        private const float RowMax = 64;
        private const float ChipY = 54;
        private const float ChipH = 38;
        private const float RowsY = 104;
        private float RowH = RowMax;

        // 고른 칩(세션 동안 기억). 0~3은 분류, 4는 켜 둔 것.
        private static int chip;
#if DEVTOOLS
        // 개발 빌드 전용: 모든 줄을 한 화면에(기존 검사가 줄 이름으로 누른다). 처음부터 켜져 있고 검사 rates2가 끈다.
        public static bool ShowAll = true;
#endif

        public override string Title
        {
            get { return "배율"; }
        }

        public static int Chip
        {
            get { return chip; }
            set { chip = Math.Max(0, Math.Min(RateGroups.Count - 1, value)); }
        }

        protected override void Draw()
        {
            List<string> keys = VisibleKeys();
            bool chips = true;
#if DEVTOOLS
            chips = !ShowAll;
#endif
            float top = chips ? RowsY : 60;
            int rows = Math.Max(1, keys.Count);
            RowH = Math.Min(RowMax, (float)Math.Floor((ContentH - top - 16) / rows));
            Ui.Panel("RateBox", content, 0, 0, ContentW, top + rows * RowH + 16, Ui.Section);
            Ui.Label(content, "배율", 16, 6, 200, 36, 21);
            Ui.Button("RateReset", content, "모두 1배로", 100, 6, 150, 38, new Action(() => Say(RateService.Reset())));
            Ui.Label(content, "게임을 다시 켜면 모두 꺼진 상태로 시작합니다. 켜 둔 것은 화면 위쪽 가운데에 표시됩니다.", 270, 10, 1010, 30, 15).color = Ui.Dim;
            if (chips)
            {
                DrawChips();
            }
            float y = top;
            if (keys.Count == 0)
            {
                Ui.Label(content, "켜 둔 배율이 없습니다.", 16, y + 14, 600, 36, 17).gameObject.name = "RateEmpty";
                return;
            }
            foreach (string key in keys)
            {
                y = DrawRow(key, y);
            }
        }

        private void DrawChips()
        {
            float x = 16;
            for (int i = 0; i < RateGroups.Count; i++)
            {
                int index = i;
                int on = RateGroups.OnCount(i, IsOn);
                string label = RateGroups.NameOf(i) + (on > 0 ? " " + on : "");
                float w = 40 + label.Length * 17;
                Ui.Button("RateGroup" + i, content, label, x, ChipY, w, ChipH, new Action(() => { Chip = index; Redraw(); }), true, chip == i);
                x += w + 10;
            }
        }

        private static List<string> VisibleKeys()
        {
#if DEVTOOLS
            if (ShowAll)
            {
                var all = new List<string>();
                foreach (RateGroups.Group group in RateGroups.Groups)
                {
                    all.AddRange(group.Keys);
                }
                return all;
            }
#endif
            return RateGroups.KeysOf(chip, IsOn);
        }

        // 줄 키가 1배가 아니면(무적은 켜짐) 참.
        public static bool IsOn(string key)
        {
            if (key == "multiplier")
            {
                return BattleService.Multiplier > 1;
            }
            if (key == "god")
            {
                return BattleService.God;
            }
            RateService.Rate rate = Find(key);
            return rate != null && rate.On;
        }

        private static RateService.Rate Find(string key)
        {
            foreach (RateService.Rate rate in RateService.All)
            {
                if (rate.Key == key)
                {
                    return rate;
                }
            }
            return null;
        }

        private float DrawRow(string key, float y)
        {
            if (key == "multiplier")
            {
                return Row(y, "Multiplier", "주는 피해", "x" + BattleService.Multiplier, BattleService.Multiplier > 1,
                    "플레이어가 주는 피해를 곱합니다. 동료와 적끼리의 피해는 그대로입니다.",
                    new Action(() => Say(BattleService.NextMultiplier())));
            }
            if (key == "god")
            {
                return Row(y, "God", "무적", BattleService.God ? "켜짐" : "꺼짐", BattleService.God,
                    "플레이어가 받는 피해를 0으로 합니다.",
                    new Action(() => Say(BattleService.SetGod(!BattleService.God))));
            }
            RateService.Rate rate = Find(key);
            if (rate == null)
            {
                return y;
            }
            string note = (rate == RateService.Move || rate == RateService.ActionDay) && TimeService.Frozen
                ? "날짜 고정 중에는 날짜가 흐르지 않습니다. 이 배율은 고정을 끈 뒤에 적용됩니다."
                : rate.Note;
            return Row(y, "Rate" + char.ToUpperInvariant(rate.Key[0]) + rate.Key.Substring(1), rate.Label, rate.Text, rate.On, note,
                new Action(() => Say(RateService.Next(rate))));
        }

        // 줄 하나: 이름, 값 버튼, 설명. 다음 줄의 y를 돌려준다.
        // 설명 글에는 이름을 붙인다(검증이 날짜 고정 중의 안내를 읽는다).
        private float Row(float y, string name, string label, string value, bool on, string note, Action onClick)
        {
            Ui.Label(content, label, 16, y + 10, 220, 40, 19);
            float h = RowH - 12;
            Ui.Button(name, content, value, 240, y + 6, 150, h, onClick, true, on);
            UnityEngine.UI.Text text = Ui.Wrapped(content, note, 410, y + 6, ContentW - 430, h, 15);
            text.color = Ui.Dim;
            text.gameObject.name = name.StartsWith("Rate") ? "RateNote" + name.Substring(4) : name + "Note";
            return y + RowH;
        }
    }
}
