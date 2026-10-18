using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 법보 탭: 편집 대상이 가진 법보와 기령. 장착·해제, 수리(60절), 기령 빼기·넣기, 재능 켜기, 레벨 올리기, 지우기, 기령 만들기, 법보 지급.
    // 그리기만 하고 일은 TreasureService가 한다(스펙 docs/superpowers/specs/2026-10-17-treasure-tab-design.md, 05 문서 56절).
    internal sealed class TreasureTab : EditorTab
    {
        private readonly ListView<TreasureService.Treasure> treasureList = new ListView<TreasureService.Treasure>("TreasureList", 44);
        private readonly ListView<TreasureService.Spirit> spiritList = new ListView<TreasureService.Spirit>("SpiritList", 44);
        private readonly ListView<TreasureService.Kind> kindList = new ListView<TreasureService.Kind>("SpiritKindList", 44);
        private readonly ListView<TreasureService.ItemKind> itemList = new ListView<TreasureService.ItemKind>("TreasureKindList", 44);
        private string kindQuery = "";
        private TreasureService.Treasure putTarget;   // 넣기의 대상: 장착한 빈 법보. 없으면 null

        public override string Title
        {
            get { return "법보"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public TreasureTab()
        {
            treasureList.EmptyText = "가진 법보가 없습니다. \"법보 지급…\"으로 받습니다.";
            spiritList.EmptyText = "가진 기령이 없습니다. \"기령 만들기…\"로 만듭니다.";
            kindList.EmptyText = "조건에 맞는 기령이 없습니다.";
            itemList.EmptyText = "법보 표가 비어 있습니다.";
        }

        // 줄의 단추가 끝나는 x(스크롤 막대 왼쪽).
        private static float Right
        {
            get { return ContentW - Ui.ScrollBarWidth - 6; }
        }

        private static string GradeName(int grade)
        {
            return grade >= 1 && grade <= 10 ? ArtifactService.MajorName(grade) : grade.ToString();
        }

        protected override void Draw()
        {
            if (!TreasureService.Ready(Target))
            {
                Ui.Label(content, "세계 자료가 없습니다(세계에 들어간 뒤에 씁니다).", 0, 0, 800, 36).gameObject.name = "TreasureNoData";
                return;
            }
            Ui.Label(content, "법보 — " + EditService.NameOf(Target), 0, 0, 420, 36, 21);
            Ui.Button("TreasureMakeOpen", content, "기령 만들기…", 440, 0, 150, 36, new Action(OpenMake));
            Ui.Button("TreasureGiveOpen", content, "법보 지급…", 600, 0, 150, 36, new Action(OpenGive));
            Ui.Wrapped(content, "장착한 법보에만 기령을 넣습니다. 든 기령은 지우기 전에 빼야 합니다. 법보의 경지·강화·접두 옵션과 기령의 친밀도·장비는 다루지 않습니다.",
                770, 0, ContentW - 770, 40, 15).color = Ui.Dim;
            float y = 46;
            List<TreasureService.Treasure> treasures = TreasureService.Treasures(Target);
            putTarget = null;
            foreach (TreasureService.Treasure t in treasures)
            {
                if (t.Equipped && t.SpiritSole == 0 && putTarget == null)
                {
                    putTarget = t;
                }
            }
            Ui.Label(content, "가진 법보 " + treasures.Count, 0, y, 300, 30, 18).color = Ui.Dim;
            bool anyWorn = treasures.Exists(t => TreasureService.RepairBlock(t) == null);
            Ui.Button("TreasureRepairAll", content, "모두 수리", 310, y - 2, 120, 32, new Action(() => Say(TreasureService.RepairAll(Target))), anyWorn);
            y += 32;
            // ListView의 높이에는 아래 이동 줄(38 + 간격 6)이 들어 있다. 최대 다섯 줄을 보인다.
            float tH = Math.Min(230, Math.Max(48, treasures.Count * 44 + 4)) + 44;
            treasureList.Build(content, 0, y, ContentW, tH, treasures, DrawTreasure);
            y += tH + 10;
            List<TreasureService.Spirit> spirits = TreasureService.Spirits(Target);
            Ui.Label(content, "기령 " + spirits.Count + (putTarget == null ? "   (넣기: 장착한 빈 법보가 없습니다)" : "   (넣기: " + putTarget.Name + ")"), 0, y, 700, 30, 18).color = Ui.Dim;
            y += 32;
            spiritList.Build(content, 0, y, ContentW, ContentH - y, spirits, DrawSpirit);
        }

        private void DrawTreasure(RectTransform row, TreasureService.Treasure t, int index)
        {
            float h = row.sizeDelta.y;
            string sole = t.SoleId;
            Ui.Label(row, t.Name + "  [" + t.ItemId + "]", 8, 0, 300, h, 18);
            Ui.Label(row, "품질 " + t.Quality, 316, 0, 70, h, 16).color = Ui.Dim;
            Ui.Label(row, GradeName(t.Grade), 392, 0, 90, h, 16).color = Ui.Dim;
            Ui.Label(row, "내구 " + t.Durable + "/" + t.DurableMax, 488, 0, 120, h, 16).color = Ui.Dim;
            Text state = Ui.Label(row, t.Equipped ? "장착 중" : "-", 614, 0, 80, h, 16);
            state.color = t.Equipped ? Ui.Warn : Ui.Dim;
            state.gameObject.name = "TreasureState" + sole;
            float bx = Right - 90 - 6 - 112;
            float rx = bx - 6 - 72;
            // 수리: 내구를 모양 설정의 최대까지(05 문서 60절). 가득하거나 최대를 모르면 끈다.
            Ui.Button("TreasureRepair" + sole, row, "수리", rx, (h - 36) / 2, 72, 36, new Action(() => Say(TreasureService.Repair(Target, sole))), TreasureService.RepairBlock(t) == null);
            Text spirit = Ui.Label(row, "기령: " + t.SpiritText, 700, 0, rx - 710, h, 16);
            spirit.color = t.SpiritSole == 0 ? Ui.Dim : Color.white;
            spirit.gameObject.name = "TreasureSpirit" + sole;
            bool equipped = t.Equipped;
            Ui.Button("TreasureEquip" + sole, row, equipped ? "해제" : "장착", bx, (h - 36) / 2, 112, 36,
                new Action(() => Say(equipped ? TreasureService.Unequip(Target, sole) : TreasureService.Equip(Target, sole))));
            Ui.Button("TreasurePull" + sole, row, "기령 빼기", Right - 90, (h - 36) / 2, 90, 36, new Action(() => Say(TreasureService.PullSpirit(Target, sole))), t.SpiritSole != 0);
        }

        private void DrawSpirit(RectTransform row, TreasureService.Spirit s, int index)
        {
            float h = row.sizeDelta.y;
            int sole = s.SoleId;
            float delX = Right - 104;
            float putX = delX - 6 - 72;
            float levelX = putX - 6 - 110;
            float lightX = levelX - 6 - 100;
            Ui.Label(row, s.Name + "  [" + s.SpriteId + "]", 8, 0, 250, h, 18);
            Text state = Ui.Label(row, "Lv " + s.Level + "/" + s.MaxLevel, 262, 0, 80, h, 16);
            state.gameObject.name = "SpiritState" + sole;
            Ui.Label(row, "경험치 " + s.Exp + (s.NextExp < 0 ? " (최고)" : "/" + s.NextExp), 346, 0, 150, h, 16).color = Ui.Dim;
            Text talents = Ui.Label(row, "재능 " + s.Lit + "/" + s.Shown, 500, 0, 96, h, 16);
            talents.color = s.Lit >= s.Shown ? Ui.Warn : Ui.Dim;
            talents.gameObject.name = "SpiritTalents" + sole;
            Ui.Label(row, "법보: " + (s.InName ?? "없음"), 600, 0, lightX - 610, h, 16).color = s.InName == null ? Ui.Dim : Color.white;
            Ui.Button("SpiritLight" + sole, row, "재능 켜기", lightX, (h - 36) / 2, 100, 36, new Action(() => Say(TreasureService.LightTalents(Target, sole))), s.Lit < s.Shown);
            Ui.Button("SpiritLevel" + sole, row, "레벨 올리기", levelX, (h - 36) / 2, 110, 36, new Action(() => Say(TreasureService.LevelUp(Target, sole))), s.NextExp >= 0);
            TreasureService.Treasure target = putTarget;
            Ui.Button("SpiritPut" + sole, row, "넣기", putX, (h - 36) / 2, 72, 36,
                new Action(() => Say(target == null ? "법보: 장착한 빈 법보가 없습니다." : TreasureService.PutSpirit(Target, target.SoleId, sole))), target != null && s.InSole == null);
            WrapLabel(Confirm.Button("SpiritDel" + sole, row, "지우기", "정말 지우기", delX, (h - 36) / 2, 104, 36, "spirit-del-" + sole,
                s.Name + "(Lv" + s.Level + ")을(를) 지웁니다. 되돌릴 수 없습니다. \"정말 지우기\"를 한 번 더 누르세요.",
                new Action(() => Say(TreasureService.Delete(Target, sole))), s.InSole == null), 15);
        }

        private void OpenMake()
        {
            kindList.Reset();
            Push(() => "기령 만들기", DrawMake);
        }

        private void DrawMake()
        {
            float y = SubTop;
            SearchBox.Build(content, "SpiritKindSearch", 0, y, 330, kindQuery, "기령 이름 또는 번호", new Action<string>(v =>
            {
                kindQuery = v;
                kindList.SetItems(TreasureService.Kinds(Target, kindQuery));
            }));
            Ui.Wrapped(content, "같은 기령은 하나만 가질 수 있습니다(게임이 둘째를 만들지 않습니다). 만든 기령은 Lv1, 재능은 꺼진 상태이고, 첫 화면에서 장착한 빈 법보에 넣습니다.",
                350, y, ContentW - 350, 40, 15).color = Ui.Dim;
            float top = y + 46;
            kindList.Build(content, 0, top, ContentW, ContentH - top, TreasureService.Kinds(Target, kindQuery), DrawKind);
        }

        private void DrawKind(RectTransform row, TreasureService.Kind k, int index)
        {
            float h = row.sizeDelta.y;
            Ui.Label(row, k.Name + "  [" + k.SpriteId + "]", 8, 0, 360, h, 18);
            Ui.Label(row, "품질 " + k.Quality, 380, 0, 90, h, 16).color = Ui.Dim;
            Text owned = Ui.Label(row, k.Owned ? "가짐" : "-", 480, 0, 80, h, 16);
            owned.color = k.Owned ? Ui.Warn : Ui.Dim;
            owned.gameObject.name = "SpiritOwned" + k.SpriteId;
            int id = k.SpriteId;
            Ui.Button("SpiritMake" + id, row, "만들기", Right - 140, (h - 36) / 2, 140, 36, new Action(() => Say(TreasureService.Create(Target, id))), !k.Owned);
        }

        private void OpenGive()
        {
            itemList.Reset();
            Push(() => "법보 지급", DrawGive);
        }

        private void DrawGive()
        {
            float y = SubTop;
            Ui.Wrapped(content, "열 종의 법보를 하나씩 가방에 넣습니다(가방이 가득 차면 들어가지 않습니다). 경지는 법보가 시작하는 경지입니다.", 0, y, ContentW, 36, 15).color = Ui.Dim;
            float top = y + 40;
            itemList.Build(content, 0, top, ContentW, ContentH - top, TreasureService.ItemKinds(Target), DrawItemKind);
        }

        private void DrawItemKind(RectTransform row, TreasureService.ItemKind k, int index)
        {
            float h = row.sizeDelta.y;
            Ui.Label(row, k.Name + "  [" + k.ItemId + "]", 8, 0, 360, h, 18);
            Ui.Label(row, "품질 " + k.Quality, 380, 0, 90, h, 16).color = Ui.Dim;
            Ui.Label(row, GradeName(k.InitGrade), 480, 0, 100, h, 16).color = Ui.Dim;
            Ui.Label(row, "가진 수 " + k.Owned, 590, 0, 120, h, 16).color = k.Owned > 0 ? Ui.Warn : Ui.Dim;
            int id = k.ItemId;
            Ui.Button("TreasureGive" + id, row, "지급", Right - 140, (h - 36) / 2, 140, 36, new Action(() => Say(TreasureService.Give(Target, id))));
        }
    }
}
