using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 경지 탭: 수련치 채우기, 소경지 돌파, 대경지 돌파 재료와 게임의 돌파 화면.
    internal sealed class GradeTab : EditorTab
    {
        public override string Title
        {
            get { return "경지"; }
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            var saved = player.data.unitData.propertyData;
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(saved.gradeID);
            Ui.Panel("GradeBox", content, 0, 0, ContentW, 250, Ui.Section);
            Ui.Label(content, "현재 경지: " + EditService.GradeName(saved.gradeID), 16, 12, 800, 36, 22);
            Ui.Label(content, "다음 단계: " + (next == null ? "없음" : EditService.GradeName(next.id)), 16, 52, 800, 36);
            Ui.Label(content, "수련치: " + saved.exp + (next == null ? "" : " / " + next.exp), 16, 92, 800, 36);
            string blocked = EditService.BreakthroughBlockReason(player);
            Ui.Button("FillExp", content, "수련치 채우기", 16, 144, 200, 48, new Action(() => Say(EditService.FillExp(Player))), next != null && saved.exp < next.exp);
            Ui.Button("Breakthrough", content, "다음 단계로 돌파", 232, 144, 230, 48, new Action(() => Say(EditService.Breakthrough(Player))), blocked == null);
            if (blocked != null)
            {
                Ui.Label(content, blocked, 16, 200, 880, 36, 18).color = Ui.Warn;
            }
            List<ConfRoleGradeItem> major = EditService.MajorBreakthroughRows(player);
            if (major.Count == 0)
            {
                Ui.Label(content, "돌파는 게임의 정식 돌파 행동을 실행합니다. 능력치 보정이 함께 적용됩니다. 대상은 플레이어입니다.", 0, 264, 900, 32, 18).color = Ui.Dim;
                return;
            }
            // 대경지 돌파: 성공 확률 판정과 실패 시의 천겁은 게임의 절차이므로, 재료만 지급하고 게임의 돌파 화면을 연다.
            Ui.Panel("MajorBox", content, 0, 262, ContentW, 70 + major.Count * 44 + 60, Ui.Section);
            Ui.Label(content, "대경지 돌파 — 게임의 돌파 화면에서 진행합니다", 16, 268, 880, 34, 21);
            Ui.Label(content, "방식마다 필요한 재료와 역천개명 선택 수가 다릅니다. 성공 확률은 넣는 재료에 따라 정해지고, 실패하면 천겁을 치릅니다.", 16, 300, 890, 28, 15).color = Ui.Dim;
            for (int i = 0; i < major.Count; i++)
            {
                ConfRoleGradeItem row = major[i];
                float y = 334 + i * 44;
                List<int> needItems = EditService.BreakthroughNeedItems(row);
                List<int> treasures = EditService.BreakthroughTreasures(row);
                string need = needItems.Count == 0 ? "없음"
                    : (EditService.ItemName(needItems[0]) ?? "?") + (needItems.Count > 1 ? " 등 " : " ") + needItems.Count + "개";
                string treasure = treasures.Count == 0 ? "없음"
                    : (EditService.ItemName(treasures[0]) ?? "?") + (treasures.Count > 1 ? " 등 " : " ") + treasures.Count + "개";
                Ui.Label(content, GameTool.LS(row.qualityName) + "  ·  필요 재료 " + need + "  ·  천재지보 " + treasure + "  ·  역천개명 " + row.fateNum + "개", 16, y, 980, 38, 18);
                Ui.Button("MajorGive" + i, content, "재료·천재지보 지급", 1010, y + 2, 280, 36, new Action(() => Say(EditService.GiveBreakthroughMaterials(Player, row))),
                    needItems.Count + treasures.Count > 0);
            }
            float open = 334 + major.Count * 44 + 6;
            Ui.Button("MajorOpen", content, "게임의 돌파 화면 열기", 16, open, 300, 44, new Action(() =>
            {
                string error = EditService.OpenGameBreakthroughScreen(Player);
                if (error != null)
                {
                    Say(error);
                    return;
                }
                EditorWindow.Close();
            }));
            Ui.Label(content, "누르면 편집기를 닫고 게임의 돌파 화면을 엽니다.", 330, open + 4, 560, 36, 16).color = Ui.Dim;
        }
    }
}
