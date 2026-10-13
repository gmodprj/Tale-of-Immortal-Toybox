using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 운명 탭: 선천·후천운명의 추가·교체·삭제. 하위 화면: 운명 고르기, 역천개명(경지별), 역천개명 고르기.
    internal sealed class LuckTab : EditorTab
    {
        private const float DetailH = 70;

        private readonly ListView<LuckService.LuckView> ownedList = new ListView<LuckService.LuckView>("LuckList", 40);
        private readonly ListView<LuckService.LuckView> pickList = new ListView<LuckService.LuckView>("LuckPick", 50);
        private readonly ListView<LuckService.LuckView> fateList = new ListView<LuckService.LuckView>("FatePick", 50);
        private readonly DetailPane ownedDetail = new DetailPane("LuckDetail", "줄을 누르면 여기에 전체 효과가 나옵니다.");
        private readonly DetailPane pickDetail = new DetailPane("LuckPickDetail", "줄을 누르면 여기에 전체 효과가 나옵니다.");
        private readonly DetailPane fateDetail = new DetailPane("FateDetail", "줄을 누르면 여기에 전체 효과가 나옵니다.");

        private int replaceIndex = -1;  // 교체할 선천운명 자리. -1이면 추가
        private int luckType = 1;
        private string luckQuery = "";
        private int fateGrade;          // 역천개명을 고르는 중인 대경지
        private string fateQuery = "";

        public override string Title
        {
            get { return "운명"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public LuckTab()
        {
            ownedList.EmptyText = "가진 운명이 없습니다.";
            pickList.EmptyText = "조건에 맞는 운명이 없습니다.";
            fateList.EmptyText = "조건에 맞는 역천개명이 없습니다.";
            ownedList.OnSelect = view => ownedDetail.Show(Whole(view));
            pickList.OnSelect = view => pickDetail.Show(Whole(view));
            fateList.OnSelect = view => fateDetail.Show(Whole(view));
        }

        private static string Whole(LuckService.LuckView view)
        {
            return view.Name + "  [" + view.Id + "]  품질 " + view.Level + "\n" + view.Tips;
        }

        public override void OnTargetChanged()
        {
            ownedList.Reset();
            ownedDetail.Clear();
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            List<LuckService.LuckView> owned = LuckService.Owned(target).FindAll(v => !v.Hidden);

            Ui.Button("LuckAddOpen", content, "운명 추가", 0, 0, 160, 36, new Action(() => OpenPicker(-1)));
            Ui.Button("FateOpen", content, "역천개명 (경지별)", 170, 0, 220, 36, new Action(() => Push(() => "역천개명 — " + EditService.NameOf(Target), DrawFate)));
            Ui.Label(content, "운명", 8, 44, 240, 30, 18).color = Ui.Dim;
            Ui.Label(content, "게임 화면의 위치", 258, 44, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "효과", 458, 44, 500, 30, 18).color = Ui.Dim;
            ownedList.HeightOf = view => RowFor(40, Ui.TextHeight(view.Tips, 520, 16), Ui.TextHeight(view.Name, 245, 20));
            ownedList.Build(content, 0, 76, ContentW, ContentH - 76 - DetailH - 44, owned, DrawOwnedRow);
            Ui.Label(content, "게임의 인물 화면은 선천운명 칸과, 후천 분류의 후천운명만 보여 줍니다. \"표시 안 됨\"은 효과는 적용되지만 그 화면에 나오지 않는 운명입니다.", 0, ContentH - DetailH - 38, ContentW, 30, 15).color = Ui.Dim;
            ownedDetail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void OpenPicker(int bornIndex)
        {
            replaceIndex = bornIndex;
            pickList.Reset();
            pickDetail.Clear();
            Push(() => (replaceIndex >= 0 ? "선천운명 " + (replaceIndex + 1) + "번 교체 — " : "운명 추가 — ") + EditService.NameOf(Target), DrawPicker);
        }

        private void DrawOwnedRow(RectTransform row, LuckService.LuckView view, int index)
        {
            WorldUnitBase target = Target;
            // 게임의 인물 화면은 후천 목록 가운데 후천 분류만 후천운명 칸에 보여 준다.
            bool shown = view.Born || view.Type == 2;
            float h = row.sizeDelta.y;
            Ui.Wrapped(row, view.Name, 8, 0, 245, h);
            Text place = Ui.Label(row, view.Born ? "선천운명" : shown ? "후천운명" : "표시 안 됨(효과만)", 258, 0, 200, h, 17);
            if (!shown)
            {
                place.color = Ui.Warn;
            }
            Ui.Wrapped(row, view.Tips, 458, 0, 520, h, 16).gameObject.name = "LuckOwnedTips" + view.Id;
            float right = ContentW - Ui.ScrollBarWidth;
            if (view.Born)
            {
                int bornIndex = view.BornIndex;
                Ui.Button("LuckReplace" + view.Id, row, "교체", right - 254, 3, 120, 34, new Action(() => OpenPicker(bornIndex)));
            }
            else if (view.Type == 1)
            {
                Ui.Button("LuckToBorn" + view.Id, row, "선천으로 옮김", right - 294, 3, 160, 34, new Action(() => Say(LuckService.AddBorn(Target, view.Id))));
            }
            Action remove = new Action(() => Say(view.Born ? LuckService.RemoveBorn(Target, view.BornIndex) : LuckService.Remove(Target, view.Id)));
            // 편집기가 붙인 후천 목록의 운명만 바로 지운다. 선천운명과 게임이 붙인 운명은 한 번 더 확인한다.
            if (view.Born || !LuckService.AddedByEditor(target, view.Id))
            {
                Confirm.Button("LuckRemove" + view.Id, row, "삭제", "정말 삭제", right - 128, 3, 120, 34, "luck:" + view.Id,
                    view.Name + ": " + (view.Born ? "선천운명입니다" : "게임이 붙인 운명입니다") + (view.IsSystem ? "(경지 진행 표식)" : "") + ". 지우려면 \"정말 삭제\"를 한 번 더 누르세요.", remove);
            }
            else
            {
                Ui.Button("LuckRemove" + view.Id, row, "삭제", right - 128, 3, 120, 34, remove);
            }
        }

        // 하위 화면: 운명 고르기(추가 또는 선천운명 교체)
        private void DrawPicker()
        {
            bool replacing = replaceIndex >= 0;
            float y = SubTop;
            int[] types = { 1, 2, 3 };
            for (int i = 0; i < types.Length; i++)
            {
                int type = types[i];
                Ui.Button("LuckType" + type, content, LuckService.TypeLabel(type) + " 분류", i * 146, y, 140, 38, new Action(() =>
                {
                    luckType = type;
                    pickList.Reset();
                    pickDetail.Clear();
                    Redraw();
                }), true, luckType == type);
            }
            SearchBox.Build(content, "LuckQuery", 460, y, 330, luckQuery, "이름·효과의 일부", new Action<string>(v =>
            {
                luckQuery = v;
                pickDetail.Clear();
                pickList.SetItems(LuckService.Candidates(luckType, luckQuery));
            }));
            string note = replacing ? "" : luckType == 1 ? "선천 분류는 선천운명 칸에 추가됩니다."
                : luckType == 2 ? "후천 분류에는 게임이 진행 상태를 나타내려고 쓰는 운명이 섞여 있습니다."
                : "역천개명 분류는 여기서 붙이면 효과만 적용됩니다. 경지 화면에 나오게 하려면 역천개명 (경지별)을 쓰세요.";
            Text noteLabel = Ui.Label(content, note, 800, y, 500, 38, 14);
            noteLabel.color = Ui.Warn;
            noteLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            float top = y + 46;
            pickList.HeightOf = view => RowWithSecondLine(50, Ui.TextHeight(view.Tips, 1050, 15));
            pickList.Build(content, 0, top, ContentW, ContentH - top - DetailH - 8, LuckService.Candidates(luckType, luckQuery), DrawPickRow);
            pickDetail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawPickRow(RectTransform row, LuckService.LuckView view, int index)
        {
            bool replacing = replaceIndex >= 0;
            bool has = LuckService.Has(Target, view.Id);
            Text name = Ui.Label(row, view.Name + "  (품질 " + view.Level + ")" + (view.IsSystem ? "  — 경지 진행 표식" : ""), 8, 0, 1000, 26, 18);
            if (view.IsSystem)
            {
                name.color = Ui.Warn;
            }
            Text tips = Ui.Wrapped(row, view.Tips, 24, SecondLineY, 1050, row.sizeDelta.y - SecondLineY - 4, 15, TextAnchor.UpperLeft);
            tips.color = Ui.Dim;
            tips.gameObject.name = "LuckTips" + view.Id;
            string label = has ? "보유 중" : replacing ? "이것으로 교체" : view.Type == 1 ? "선천운명에 추가" : view.Type == 2 ? "후천운명에 추가" : "추가";
            Ui.Button("LuckAdd" + view.Id, row, label, ContentW - Ui.ScrollBarWidth - 198, 7, 190, 36, new Action(() =>
            {
                if (replacing)
                {
                    string result = LuckService.ReplaceBorn(Target, replaceIndex, view.Id);
                    CloseViews();
                    Say(result);
                    return;
                }
                Say(view.Type == 1 ? LuckService.AddBorn(Target, view.Id) : LuckService.Add(Target, view.Id));
            }), !has);
        }

        // 하위 화면: 도달한 경지마다 한 줄. 변경을 누르면 그 경지의 후보 목록이 열린다.
        private void DrawFate()
        {
            WorldUnitBase target = Target;
            float y = SubTop;
            Ui.Label(content, "경지", 0, y, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "역천개명", 210, y, 240, 30, 18).color = Ui.Dim;
            Ui.Label(content, "효과", 460, y, 600, 30, 18).color = Ui.Dim;
            List<FateService.Slot> slots = FateService.Slots(target);
            float rowH = 40;
            if (slots.Count == 0)
            {
                Ui.Label(content, "역천개명은 축기경부터 경지마다 하나씩 있습니다. 이 인물은 아직 축기경에 이르지 않았습니다.", 0, y + 36, ContentW, 36, 18).color = Ui.Warn;
            }
            float rowY = y + 34;
            for (int i = 0; i < slots.Count; i++)
            {
                FateService.Slot slot = slots[i];
                if (i > 0)
                {
                    rowY += rowH;
                }
                rowH = RowFor(40, Ui.TextHeight(slot.Tips, 700, 16), Ui.TextHeight(slot.LuckName, 245, 20));
                Ui.Label(content, slot.GradeName, 0, rowY, 200, rowH - 4);
                Text name = Ui.Wrapped(content, slot.LuckId == 0 ? "없음" : slot.LuckName, 210, rowY, 245, rowH - 4);
                if (slot.LuckId == 0)
                {
                    name.color = Ui.Dim;
                }
                Ui.Wrapped(content, slot.Tips, 460, rowY, 700, rowH - 4, 16);
                Ui.Button("FateChange" + slot.Grade, content, slot.LuckId == 0 ? "정하기" : "변경", 1180, rowY + 1, 120, 34, new Action(() =>
                {
                    fateGrade = slot.Grade;
                    fateList.Reset();
                    fateDetail.Clear();
                    Push(() => "역천개명 고르기 — " + EditService.NameOf(Target) + ", 대경지 " + fateGrade, DrawFatePicker);
                }));
            }
            Ui.Label(content, "게임의 경지 화면은 경지마다 역천개명 하나를 보여 줍니다. 여기서 정하면 효과와 그 화면의 표시가 함께 바뀝니다. 도달한 경지만 정할 수 있습니다.", 0, (slots.Count == 0 ? y + 34 + 40 : rowY + rowH) + 12, ContentW, 30, 15).color = Ui.Dim;
        }

        // 하위 화면: 그 경지에서 게임이 뽑을 수 있는 역천개명
        private void DrawFatePicker()
        {
            float y = SubTop;
            SearchBox.Build(content, "FateQuery", 0, y, 330, fateQuery, "이름·효과의 일부", new Action<string>(v =>
            {
                fateQuery = v;
                fateDetail.Clear();
                fateList.SetItems(FateService.Candidates(fateGrade, fateQuery));
            }));
            Ui.Label(content, "이 경지에서 게임이 뽑을 수 있는 역천개명만 나옵니다.", 350, y, 700, 38, 15).color = Ui.Dim;
            float top = y + 46;
            fateList.HeightOf = view => RowWithSecondLine(50, Ui.TextHeight(view.Tips, 1050, 15));
            fateList.Build(content, 0, top, ContentW, ContentH - top - DetailH - 8, FateService.Candidates(fateGrade, fateQuery), DrawFateRow);
            fateDetail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawFateRow(RectTransform row, LuckService.LuckView view, int index)
        {
            bool has = LuckService.Has(Target, view.Id);
            Ui.Label(row, view.Name + "  (품질 " + view.Level + ")", 8, 0, 1000, 26, 18);
            Text tips = Ui.Wrapped(row, view.Tips, 24, SecondLineY, 1050, row.sizeDelta.y - SecondLineY - 4, 15, TextAnchor.UpperLeft);
            tips.color = Ui.Dim;
            tips.gameObject.name = "FateTips" + view.Id;
            int grade = fateGrade;
            Ui.Button("FatePick" + view.Id, row, has ? "보유 중" : "이것으로 정하기", ContentW - Ui.ScrollBarWidth - 198, 7, 190, 36, new Action(() =>
            {
                string result = FateService.Set(Target, grade, view.Id);
                Pop();
                Say(result);
            }), !has);
        }
    }
}
