using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // NPC 탭: 가까운 NPC 목록, 이름 검색, 호감도, 편집 대상 지정.
    internal sealed class NpcTab : EditorTab
    {
        private const float RowH = 40;

        private sealed class Row
        {
            public string Id;
            public string Name;
            public int Distance;
        }

        private readonly ListView<Row> list = new ListView<Row>("NpcList", RowH);
        private string query = "";
        private List<Row> cache;   // 가까운 순서. null이면 다시 만든다

        private static int Step
        {
            get { return AmountBar.Get("intim"); }
        }

        public override string Title
        {
            get { return "NPC"; }
        }

        public NpcTab()
        {
            list.EmptyText = "조건에 맞는 NPC가 없습니다.";
        }

        // 편집기를 열 때마다 목록을 새로 만든다(그 사이에 NPC가 움직였을 수 있다).
        public override void OnOpen()
        {
            cache = null;
        }

        // 이름은 여기서 한 번만 읽어 둔다. 치는 대로 거르려면 수천 명의 이름을 글자마다 게임에서 다시 읽을 수 없다.
        private void BuildCache()
        {
            cache = new List<Row>();
            WorldUnitBase player = Player;
            string playerId = player.data.unitData.unitID;
            int px = player.data.unitData.pointX;
            int py = player.data.unitData.pointY;
            var units = g.world.unit.GetUnits(false);
            for (int i = 0; i < units.Count; i++)
            {
                var data = units[i].data.unitData;
                if (data.unitID == playerId)
                {
                    continue;
                }
                cache.Add(new Row
                {
                    Id = data.unitID,
                    Name = EditService.NameOf(units[i]),
                    Distance = Math.Abs(data.pointX - px) + Math.Abs(data.pointY - py),
                });
            }
            cache.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        }

        private List<Row> Found()
        {
            if (query.Length == 0)
            {
                return cache;
            }
            string wanted = EditService.Compact(query);
            return cache.FindAll(row => row.Name.Contains(wanted));
        }

        protected override void Draw()
        {
            if (cache == null)
            {
                BuildCache();
            }
            WorldUnitBase player = Player;
            WorldUnitBase selected = EditorWindow.SelectedNpcId == null ? null : g.world.unit.GetUnit(EditorWindow.SelectedNpcId, true);

            Ui.Panel("SelectedBox", content, 0, 0, ContentW, 104, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "아래 목록에서 NPC를 선택하세요. 가까운 순서입니다.", 16, 12, 880, 36);
            }
            else
            {
                RectTransform nameHolder = Ui.Rect("SelectedName", content, 16, 8, 600, 36);
                Ui.Label(nameHolder, "선택: " + EditService.NameOf(selected) + "   " + EditService.GradeName(selected.data.unitData.propertyData.gradeID), 0, 0, 600, 36, 21);
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

            Ui.Label(content, "이름 검색", 0, 114, 110, 38);
            SearchBox.Build(content, "NpcQuery", 110, 114, 330, query, "이름의 일부", new Action<string>(v =>
            {
                query = v;
                list.SetItems(Found());
            }));
            Ui.Button("NpcRefresh", content, "목록 새로 고침", 460, 114, 180, 38, new Action(() => { cache = null; list.Reset(); Redraw(); }));

            Ui.Label(content, "이름", 8, 158, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "경지", 268, 158, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "거리(칸)", 568, 158, 120, 30, 18).color = Ui.Dim;
            list.Build(content, 0, 190, ContentW, ContentH - 190, Found(), DrawRow);
        }

        private void DrawRow(RectTransform row, Row npc, int index)
        {
            WorldUnitBase unit = g.world.unit.GetUnit(npc.Id, true);
            bool picked = npc.Id == EditorWindow.SelectedNpcId;
            Ui.Label(row, npc.Name, 8, 0, 250, RowH);
            Ui.Label(row, unit == null ? "(없음)" : EditService.GradeName(unit.data.unitData.propertyData.gradeID), 268, 0, 290, RowH);
            Ui.Label(row, npc.Distance.ToString(), 568, 0, 120, RowH);
            Ui.Button("Pick" + npc.Id, row, picked ? "선택됨" : "선택", 708, 3, 110, RowH - 6, new Action(() => { EditorWindow.SelectedNpcId = npc.Id; Redraw(); }), unit != null, picked);
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
