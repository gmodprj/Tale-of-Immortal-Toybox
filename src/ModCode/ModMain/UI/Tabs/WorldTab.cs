using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 세계 탭: 순간 이동, 날짜(고정·실제 진행·직접 지정). 전투 보조와 배율은 배율 탭에 있다.
    internal sealed class WorldTab : EditorTab
    {
        private const string DateKey = "date";

        private readonly string[] dateInput = { "", "", "" };  // 직접 지정: 연, 월, 일
        private bool dateEdited;    // 입력 칸을 사용자가 고쳤는지. 고치지 않았으면 현재 날짜를 보여 준다

        private static int Step
        {
            get { return AmountBar.Get("move"); }
        }

        public override string Title
        {
            get { return "세계"; }
        }

        public override void OnOpen()
        {
            ResetDateInput();
        }

        public override void OnShow()
        {
            ResetDateInput();
        }

        // 직접 지정의 입력 칸을 현재 날짜로 되돌린다. 편집기를 열 때, 이 탭으로 들어올 때, 날짜가 바뀌는 동작 뒤에 부른다.
        // 그러지 않으면 예전 날짜가 입력 칸에 남아, 일부만 고쳐 적용했을 때 엉뚱한 날짜로 간다.
        private void ResetDateInput()
        {
            dateEdited = false;
        }

        protected override void Draw()
        {
            Vector2Int here = WorldService.PlayerPoint;
            Ui.Panel("MoveBox", content, 0, 0, ContentW, 250, Ui.Section);
            Ui.Label(content, "순간 이동", 16, 6, 300, 36, 21);
            Ui.Label(content, "현재 위치: (" + here.x + ", " + here.y + ")", 16, 44, 400, 34);
            Ui.Label(content, "칸 수", 640, 8, 60, 38, 18).color = Ui.Dim;
            AmountBar.Build(content, "move", 700, 8);
            // 방향 버튼: 단위 값만큼의 칸을 이동한다. 대지도는 y가 커질수록 아래쪽이다(실측 전: 방향은 버튼 이름 기준).
            Ui.Button("MoveUp", content, "y - " + Step, 150, 88, 120, 40, new Action(() => Say(WorldService.TeleportBy(0, -Step))));
            Ui.Button("MoveLeft", content, "x - " + Step, 16, 136, 120, 40, new Action(() => Say(WorldService.TeleportBy(-Step, 0))));
            Ui.Button("MoveRight", content, "x + " + Step, 284, 136, 120, 40, new Action(() => Say(WorldService.TeleportBy(Step, 0))));
            Ui.Button("MoveDown", content, "y + " + Step, 150, 184, 120, 40, new Action(() => Say(WorldService.TeleportBy(0, Step))));
            WorldUnitBase npc = EditorWindow.SelectedNpcId == null ? null : g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true);
            Ui.Button("MoveToNpc", content, npc == null ? "선택한 NPC에게 이동" : EditService.NameOf(npc) + "에게 이동", 450, 136, 300, 40,
                new Action(() => Say(WorldService.TeleportToUnit(g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true)))), npc != null);
            Ui.Label(content, npc == null ? "NPC 탭에서 NPC를 선택하면 그 위치로 갈 수 있습니다." : "", 450, 184, 460, 34, 15).color = Ui.Dim;

            DrawTimeBox(262);
        }

        // 날짜 상자: 표기, 고정, 실제 진행, 직접 지정. 아래쪽 끝의 y를 돌려준다.
        private float DrawTimeBox(float top)
        {
            const float height = 196;
            Ui.Panel("TimeBox", content, 0, top, ContentW, height, Ui.Section);
            Ui.Label(content, "날짜", 16, top + 6, 80, 36, 21);
            Ui.Label(content, TimeService.Describe(), 100, top + 6, 1000, 36);

            // 날짜 고정
            bool frozen = TimeService.Frozen;
            Ui.Button("Freeze", content, "날짜 고정: " + (frozen ? "켜짐" : "꺼짐"), 16, top + 48, 220, 40, new Action(() => { ResetDateInput(); Say(TimeService.SetFrozen(!TimeService.Frozen)); }), true, frozen);
            Ui.Label(content, "켜 두면 행동을 해도 날짜가 흐르지 않고, 게임의 달 넘기기도 막습니다. 게임을 다시 켜면 꺼진 상태로 시작합니다.", 250, top + 48, 1040, 40, 15).color = Ui.Dim;

            // 실제 진행: 게임의 날짜 진행을 그대로 실행한다. 고정 중에는 쓸 수 없다.
            float y = top + 96;
            bool canAdvance = !frozen && TimeService.ResidueDay > 0;
            Ui.Label(content, "실제 진행", 16, y, 110, 40, 18).color = Ui.Dim;
            int[] days = { 1, 5, 10 };
            for (int i = 0; i < days.Length; i++)
            {
                int amount = days[i];
                Ui.Button("Day" + amount, content, "+ " + amount + "일", 130 + i * 96, y, 90, 40, new Action(() => { ResetDateInput(); Say(TimeService.Advance(amount)); }), canAdvance);
            }
            Ui.Button("SkipMonth", content, "다음 달 1일로", 424, y, 180, 40, new Action(() =>
            {
                ResetDateInput();
                string error = TimeService.OpenSkipMonth();
                if (error != null)
                {
                    Say(error);
                    return;
                }
                EditorWindow.Close();
            }), !frozen);
            Text advanceNote = Ui.Label(content, frozen ? "날짜 고정 중에는 진행할 수 없습니다. 달력의 직접 지정은 됩니다."
                : "게임의 날짜 진행을 그대로 실행합니다. +N일은 이번 달 남은 날까지만 진행하고, 달은 게임의 확인 창에서 넘깁니다.", 620, y, 680, 40, 14);
            advanceNote.color = Ui.Dim;
            advanceNote.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 직접 지정: 달력의 날짜만 바꾼다. 두 번 눌러 확인한다. 칸에서 Enter를 눌러도 적용 버튼과 같다.
            y = top + 144;
            if (!dateEdited)
            {
                int[] now = TimeService.ToDate(TimeService.WorldDay);
                for (int i = 0; i < 3; i++)
                {
                    dateInput[i] = now[i].ToString();
                }
            }
            Ui.Label(content, "직접 지정", 16, y, 110, 40, 18).color = Ui.Dim;
            string[] names = { "DateYear", "DateMonth", "DateDay" };
            string[] units = { "년", "월", "일" };
            float[] xs = { 130, 270, 380 };
            float[] widths = { 100, 70, 70 };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                // 칸을 고치면 앞서 물은 확인은 다른 날짜에 대한 것이므로 푼다.
                NumberField.Build(content, names[i], xs[i], y, widths[i], 40, dateInput[i],
                    new Action<string>(v => { dateInput[index] = v; dateEdited = true; Confirm.Clear(); }),
                    new Action<int>(v => ApplyDate()));
                Ui.Label(content, units[i], xs[i] + widths[i] + 4, y, 30, 40, 18);
            }
            bool waiting = Confirm.IsPending(DateKey);
            Ui.Button("DateApply", content, waiting ? "정말 변경" : "적용", 494, y, 130, 40, new Action(ApplyDate), true, waiting);
            Text dateNote = Ui.Label(content, "달력의 날짜만 바꿉니다(앞·뒤 모두). 그 사이의 월말 처리는 일어나지 않고, 임무 기한 같은 날짜 기준 기록은 그대로입니다.", 640, y, 660, 40, 14);
            dateNote.color = Ui.Warn;
            dateNote.horizontalOverflow = HorizontalWrapMode.Wrap;
            return top + height;
        }

        private void ApplyDate()
        {
            int year, month, day, target;
            string error;
            if (!int.TryParse(dateInput[0], out year) || !int.TryParse(dateInput[1], out month) || !int.TryParse(dateInput[2], out day))
            {
                Say("날짜: 연·월·일을 모두 숫자로 입력하세요.");
                return;
            }
            if (!TimeService.TryToWorldDay(year, month, day, out target, out error))
            {
                Say(error);
                return;
            }
            if (target == TimeService.WorldDay)
            {
                Say("날짜: 이미 " + TimeService.DateText(target) + "입니다.");
                return;
            }
            if (!Confirm.IsPending(DateKey))
            {
                Confirm.Ask(DateKey, TimeService.DateText(TimeService.WorldDay) + " -> " + TimeService.DateText(target)
                    + ": 달력만 바뀌고 월말 처리와 날짜 기준 기록은 그대로입니다. \"정말 변경\"을 한 번 더 누르세요.");
                return;
            }
            Confirm.Clear();
            string result = TimeService.SetDate(year, month, day);
            ResetDateInput();
            Say(result);
        }

#if DEVTOOLS
        // 개발 전용: 직접 지정 입력 칸과 확인 상태.
        public string DevDateState
        {
            get { return "input=" + dateInput[0] + "-" + dateInput[1] + "-" + dateInput[2] + " pending=" + Confirm.IsPending(DateKey); }
        }
#endif
    }
}
