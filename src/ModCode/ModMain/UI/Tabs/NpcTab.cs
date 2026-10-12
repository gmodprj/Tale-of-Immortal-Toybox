using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // NPC 탭: 가까운 NPC 목록, 이름 검색, 호감도, 편집 대상 지정.
    internal sealed class NpcTab : EditorTab
    {
        private const int NpcRows = 8;

        private int npcPage;
        private string npcQuery = "";
        private string appliedNpcQuery = "";
        private List<WorldUnitBase> npcCache;

        private static int Step
        {
            get { return AmountBar.Get("intim"); }
        }

        public override string Title
        {
            get { return "NPC"; }
        }

        // 편집기를 열 때마다 목록을 새로 만든다(그 사이에 NPC가 움직였을 수 있다).
        public override void OnOpen()
        {
            npcCache = null;
        }

        protected override void Draw()
        {
            if (npcCache == null)
            {
                BuildNpcCache();
            }
            WorldUnitBase player = Player;
            int px = player.data.unitData.pointX;
            int py = player.data.unitData.pointY;
            WorldUnitBase selected = EditorWindow.SelectedNpcId == null ? null : g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true);

            Ui.Panel("SelectedBox", content, 0, 0, ContentW, 104, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "아래 목록에서 NPC를 선택하세요. 가까운 순서입니다.", 16, 12, 880, 36);
            }
            else
            {
                Ui.Label(content, "선택: " + EditService.NameOf(selected) + "   " + EditService.GradeName(selected.data.unitData.propertyData.gradeID), 16, 8, 600, 36, 21);
                Ui.Label(content, "호감도  나→상대 " + EditService.IntimTo(player, selected) + "   상대→나 " + EditService.IntimTo(selected, player), 16, 52, 380, 40);
                Ui.Button("IntimMinus", content, "- " + Step, 400, 54, 100, 38, new Action(() => ChangeIntim(-Step)));
                Ui.Button("IntimPlus", content, "+ " + Step, 506, 54, 100, 38, new Action(() => ChangeIntim(Step)));
                Ui.Button("IntimZero", content, "0으로", 612, 54, 90, 38, new Action(() => Say(EditService.SetIntimBoth(Player, g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true), 0))));
                Ui.Button("EditNpc", content, "능력치 편집", 716, 54, 190, 38, new Action(() =>
                {
                    EditorWindow.SetTarget(EditorWindow.SelectedNpcId);
                    EditorWindow.SwitchTab(0);
                    Say("편집 대상을 NPC로 바꿨습니다.");
                }));
                AmountBar.Build(content, "intim", 716, 8);
            }

            int pages = Math.Max(1, (npcCache.Count + NpcRows - 1) / NpcRows);
            npcPage = Mathf.Clamp(npcPage, 0, pages - 1);
            RenderNpcSearch();
            Ui.Label(content, "이름", 0, 156, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "경지", 260, 156, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "거리(칸)", 560, 156, 120, 30, 18).color = Ui.Dim;
            for (int row = 0; row < NpcRows; row++)
            {
                int index = npcPage * NpcRows + row;
                if (index >= npcCache.Count)
                {
                    break;
                }
                WorldUnitBase npc = npcCache[index];
                string id = npc.data.unitData.unitID;
                float y = 188 + row * 40;
                Ui.Label(content, EditService.NameOf(npc), 0, y, 250, 36);
                Ui.Label(content, EditService.GradeName(npc.data.unitData.propertyData.gradeID), 260, y, 290, 36);
                Ui.Label(content, Distance(npc, px, py).ToString(), 560, y, 120, 36);
                Ui.Button("Pick" + index, content, id == EditorWindow.SelectedNpcId ? "선택됨" : "선택", 700, y, 110, 34, new Action(() => { EditorWindow.SelectedNpcId = id; Redraw(); }), true, id == EditorWindow.SelectedNpcId);
            }
            float footer = 188 + NpcRows * 40 + 8;
            Ui.Button("NpcPrev", content, "이전", 0, footer, 90, 40, new Action(() => { npcPage--; Refresh(); }), npcPage > 0);
            Ui.Label(content, (npcPage + 1) + " / " + pages, 100, footer, 110, 40, 20, TextAnchor.MiddleCenter);
            Ui.Button("NpcNext", content, "다음", 220, footer, 90, 40, new Action(() => { npcPage++; Refresh(); }), npcPage < pages - 1);
            Ui.Button("NpcNext10", content, "10쪽 뒤로", 320, footer, 130, 40, new Action(() => { npcPage += 10; Refresh(); }), npcPage < pages - 1);
            Ui.Button("NpcRefresh", content, "목록 새로 고침", 470, footer, 180, 40, new Action(() => { npcCache = null; npcPage = 0; Refresh(); }));
            Ui.Label(content, "전체 " + npcCache.Count + "명", 670, footer, 200, 40, 18).color = Ui.Dim;
        }

        private void RenderNpcSearch()
        {
            Ui.Label(content, "이름 검색", 0, 110, 110, 40);
            Ui.Input("NpcQuery", content, npcQuery, "이름의 일부", 110, 110, 280, 40, new Action<string>(v => npcQuery = v));
            Ui.Button("NpcSearch", content, "검색", 400, 110, 90, 40, new Action(() => { appliedNpcQuery = npcQuery.Trim(); npcCache = null; npcPage = 0; Redraw(); }));
            Ui.Button("NpcSearchClear", content, "지움", 500, 110, 90, 40, new Action(() => { npcQuery = ""; appliedNpcQuery = ""; npcCache = null; npcPage = 0; Redraw(); }), appliedNpcQuery.Length > 0 || npcQuery.Length > 0);
            if (appliedNpcQuery.Length > 0)
            {
                Ui.Label(content, "'" + appliedNpcQuery + "' 포함", 604, 110, 300, 40, 18).color = Ui.Warn;
            }
        }

        private void BuildNpcCache()
        {
            npcCache = new List<WorldUnitBase>();
            WorldUnitBase player = Player;
            string playerId = player.data.unitData.unitID;
            var units = g.world.unit.GetUnits(false);
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].data.unitData.unitID != playerId
                    && (appliedNpcQuery.Length == 0 || EditService.NameOf(units[i]).Contains(EditService.Compact(appliedNpcQuery))))
                {
                    npcCache.Add(units[i]);
                }
            }
            int px = player.data.unitData.pointX;
            int py = player.data.unitData.pointY;
            npcCache.Sort((a, b) => Distance(a, px, py).CompareTo(Distance(b, px, py)));
        }
        private static int Distance(WorldUnitBase unit, int px, int py)
        {
            return Math.Abs(unit.data.unitData.pointX - px) + Math.Abs(unit.data.unitData.pointY - py);
        }

        private void ChangeIntim(int delta)
        {
            WorldUnitBase npc = g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true);
            if (npc == null)
            {
                Say("선택한 NPC를 찾지 못했습니다.");
                return;
            }
            Say(EditService.SetIntimBoth(Player, npc, EditService.IntimTo(npc, Player) + delta));
        }
    }
}
