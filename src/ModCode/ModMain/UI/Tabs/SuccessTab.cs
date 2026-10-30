using System;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 성공·드롭 탭(번호 17): 플레이어의 훔치기·관계 발전 성공, 전투 드롭 배수, 참오 결과 최대(트레이너 3차)(docs/superpowers/specs/2026-10-20-trainer-batch2-design.md 7.2절).
    // 저장하지 않으며 게임을 켤 때마다 꺼진 상태로 시작한다.
    internal sealed class SuccessTab : EditorTab
    {
        private const float RowH = 64;

        public override string Title
        {
            get { return "성공·드롭"; }
        }

        protected override void Draw()
        {
            Ui.Panel("SuccessBox", content, 0, 0, ContentW, 60 + 4 * RowH + 16, Ui.Section);
            Ui.Label(content, "성공·드롭", 16, 6, 200, 36, 21);
            Ui.Button("SuccessReset", content, "모두 끄기", 160, 6, 150, 38, new Action(() => Say(SuccessService.Reset())));
            Ui.Label(content, "게임을 다시 켜면 모두 꺼진 상태로 시작합니다. 켜 둔 것은 화면 위쪽 가운데에 표시됩니다.", 330, 10, 950, 30, 15).color = Ui.Dim;
            float y = 60;
            y = Row(y, "SuccessSteal", "훔치기", SuccessService.Steal ? "켜짐" : "꺼짐", SuccessService.Steal,
                "플레이어의 훔치기가 늘 성공하고 들키지 않습니다.",
                new Action(() => Say(SuccessService.SetSteal(!SuccessService.Steal))));
            y = Row(y, "SuccessRelation", "관계 발전", SuccessService.Relation ? "켜짐" : "꺼짐", SuccessService.Relation,
                "플레이어가 청한 관계(의형제·도려·부부·사제 등)를 NPC가 받아들입니다. 친밀도·평판 조건도 넘습니다.",
                new Action(() => Say(SuccessService.SetRelation(!SuccessService.Relation))));
            y = Row(y, "SuccessDrop", "드롭 배수", "x" + SuccessService.DropTimes, SuccessService.DropTimes > 1,
                "전투에서 적이 죽을 때 드롭을 굴리는 횟수를 곱합니다.",
                new Action(() => Say(SuccessService.NextDrop())));
            Row(y, "SuccessRealize", "참오 결과 최대", SuccessService.Realize ? "켜짐" : "꺼짐", SuccessService.Realize,
                "게임의 참오 창에서 참오·다시 참오로 굴린 값이 그 기술의 최대 품질 맨 위가 됩니다. 비용은 그대로 듭니다.",
                new Action(() => Say(SuccessService.SetRealize(!SuccessService.Realize))));
            Ui.Label(content, "종문 초빙·호요 포획·도주는 판정 위치만 찾았고 아직 넣지 않았습니다.", 16, 60 + 4 * RowH + 30, ContentW - 32, 30, 15)
                .gameObject.name = "SuccessLeftOut";
        }

        // 줄 하나: 이름, 값 단추, 설명(배율 탭과 같은 꼴). 다음 줄의 y를 돌려준다.
        private float Row(float y, string name, string label, string value, bool on, string note, Action onClick)
        {
            Ui.Label(content, label, 16, y + 10, 220, 40, 19);
            float h = RowH - 12;
            Ui.Button(name, content, value, 240, y + 6, 150, h, onClick, true, on);
            UnityEngine.UI.Text text = Ui.Wrapped(content, note, 410, y + 6, ContentW - 430, h, 15);
            text.color = Ui.Dim;
            text.gameObject.name = name + "Note";
            return y + RowH;
        }
    }
}
