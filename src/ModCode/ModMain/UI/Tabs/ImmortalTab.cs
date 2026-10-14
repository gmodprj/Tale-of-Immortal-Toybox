using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 선법 탭: 본원 선력과 가진 선법. 하위 화면: 선법 추가(선인·종류·이름으로 걸러 익히기). 장착은 게임의 선법 창에서 한다.
    internal sealed class ImmortalTab : EditorTab
    {
        private readonly ListView<ImmortalService.Entry> ownedList = new ListView<ImmortalService.Entry>("ImmortalOwnedList", 38);
        private readonly ListView<ImmortalService.Entry> addList = new ListView<ImmortalService.Entry>("ImmortalAddList", 50);
        private int human;      // 0은 전체
        private int kind;       // 0 전체, 1 주동, 2 피동
        private string query = "";

        public override string Title
        {
            get { return "선법"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public ImmortalTab()
        {
            ownedList.EmptyText = "가진 선법이 없습니다.";
            addList.EmptyText = "조건에 맞는 선법이 없습니다.";
        }

        public override void OnTargetChanged()
        {
            ownedList.Reset();
            addList.Reset();
        }

        private static AttributeDef PointDef
        {
            get { return AttributeCatalog.All.Find(d => d.Key == "immortalPoint"); }
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            if (!ImmortalService.HasData(target))
            {
                Ui.Label(content, "이 인물은 선법 자료가 없습니다.", 0, 0, 800, 36).gameObject.name = "ImmortalNoData";
                return;
            }
            int total = ImmortalService.TotalPoint(target);
            Ui.Label(content, "본원 선력: " + ImmortalService.UsedPoint(target) + " / " + total, 0, 0, 260, 36, 20).gameObject.name = "ImmortalPointText";
            int max = PointDef.Max(g.conf.roleAttributeLimit);
            int min = PointDef.Min(g.conf.roleAttributeLimit);
            // 상한을 넘은 값(예전 판의 경지 올리기로 생길 수 있었다)도 -1과 최대로 범위 안에 돌아올 수 있게 한다.
            Ui.Button("ImmortalPointMinus", content, "- 1", 270, 0, 80, 34, new Action(() => Say(EditService.SetAttribute(Target, PointDef, Math.Min(max, ImmortalService.TotalPoint(Target) - 1)))), total > min);
            Ui.Button("ImmortalPointPlus", content, "+ 1", 356, 0, 80, 34, new Action(() => Say(EditService.SetAttribute(Target, PointDef, ImmortalService.TotalPoint(Target) + 1))), total < max);
            Ui.Button("ImmortalPointMax", content, "최대(" + max + ")", 442, 0, 120, 34, new Action(() => Say(EditService.SetAttribute(Target, PointDef, max))), total != max);
            Ui.Wrapped(content, "선법 창과 장착은 등선경부터입니다(장착 칸: 초기 2, 중기 4, 후기 6). 장착은 게임의 선법 창에서 합니다.", 580, 0, ContentW - 760, 40, 15).color = Ui.Dim;
            Ui.Button("ImmortalAddOpen", content, "선법 추가", ContentW - 160, 0, 160, 34, new Action(() =>
            {
                addList.Reset();
                Push(() => "선법 추가 — " + EditService.NameOf(Target), DrawAdd);
            }));

            List<ImmortalService.Entry> owned = ImmortalService.Owned(target);
            const float head = 48;
            Ui.Label(content, "가진 선법 " + owned.Count + "개", 0, head, 300, 30, 18).color = Ui.Dim;
            Ui.Panel("ImmortalHead", content, 0, head + 32, ContentW, 30, Ui.Section);
            Ui.Label(content, "선인", 8, head + 32, 180, 30, 16).color = Ui.Dim;
            Ui.Label(content, "이름", 200, head + 32, 300, 30, 16).color = Ui.Dim;
            Ui.Label(content, "종류", 510, head + 32, 80, 30, 16).color = Ui.Dim;
            Ui.Label(content, "필요 선력", 600, head + 32, 120, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", 730, head + 32, 120, 30, 16).color = Ui.Dim;
            float top = head + 64;
            ownedList.Build(content, 0, top, ContentW, ContentH - top, owned, (row, e, index) =>
            {
                Ui.Label(row, e.HumanName, 8, 0, 180, 38);
                Ui.Label(row, e.Name, 200, 0, 300, 38);
                Ui.Label(row, e.Active ? "주동" : "피동", 510, 0, 80, 38);
                Ui.Label(row, e.Cost.ToString(), 600, 0, 120, 38);
                Ui.Label(row, ImmortalService.Equipped(Target, e.CardId) ? "장착 중" : "", 730, 0, 120, 38, 17).color = Ui.Warn;
            });
        }

        private void DrawAdd()
        {
            float y = SubTop;
            // 선인 버튼: 넘치면 다음 줄로.
            var humans = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, "전체") };
            humans.AddRange(ImmortalService.Humans());
            float x = 0;
            foreach (var pair in humans)
            {
                int id = pair.Key;
                float w = Math.Max(70, (float)Math.Ceiling(Ui.TextWidth(pair.Value, 16)) + 24);
                if (x + w > ContentW)
                {
                    x = 0;
                    y += 40;
                }
                Ui.Button("ImmortalHuman" + id, content, pair.Value, x, y, w, 36, new Action(() => { human = id; addList.Reset(); Redraw(); }), true, human == id);
                x += w + 6;
            }
            y += 46;
            string[] kinds = { "전체", "주동", "피동" };
            for (int i = 0; i < kinds.Length; i++)
            {
                int value = i;
                Ui.Button("ImmortalKind" + i, content, kinds[i], i * 96, y, 90, 36, new Action(() => { kind = value; addList.Reset(); Redraw(); }), true, kind == i);
            }
            SearchBox.Build(content, "ImmortalSearch", 300, y, 330, query, "이름 또는 선인 이름의 일부", new Action<string>(v =>
            {
                query = v;
                addList.SetItems(ImmortalService.Find(human, kind, query));
            }));
            Confirm.Button("ImmortalLearnAll", content, "보이는 선법 모두 익히기", "정말 모두", ContentW - 260, y, 260, 36, "immortal-all",
                "보이는 선법을 모두 익힙니다(이미 가진 것은 건너뜁니다). \"정말 모두\"를 한 번 더 누르세요.",
                new Action(() => Say(ImmortalService.LearnAll(Target, ImmortalService.Find(human, kind, query)))));
            float top = y + 46;
            float descW = ContentW - Ui.ScrollBarWidth - 220;
            addList.HeightOf = e => RowWithSecondLine(50, Ui.TextHeight(Second(e), descW - 16, 15));
            addList.Build(content, 0, top, ContentW, ContentH - top, ImmortalService.Find(human, kind, query), DrawAddRow);
        }

        private static string Second(ImmortalService.Entry e)
        {
            return e.Info + (e.GetText.Length > 0 ? " / 획득: " + e.GetText : "");
        }

        private void DrawAddRow(RectTransform row, ImmortalService.Entry e, int index)
        {
            bool has = ImmortalService.Has(Target, e.CardId);
            float descW = ContentW - Ui.ScrollBarWidth - 220;
            Ui.Label(row, e.Name + "  [" + e.CardId + "]  " + e.HumanName + " · " + (e.Active ? "주동" : "피동") + " · 필요 선력 " + e.Cost, 8, 0, descW, 26, 18);
            Text desc = Ui.Wrapped(row, Second(e), 24, SecondLineY, descW - 16, row.sizeDelta.y - SecondLineY - 4, 15, TextAnchor.UpperLeft);
            desc.color = Ui.Dim;
            desc.gameObject.name = "ImmortalDesc" + e.CardId;
            float bx = ContentW - Ui.ScrollBarWidth - 200;
            Text state = Ui.Label(row, has ? "보유" : "미보유", bx, 7, 80, 36, 16);
            state.color = has ? Ui.Warn : Ui.Dim;
            state.gameObject.name = "ImmortalState" + e.CardId;
            Ui.Button("ImmortalLearn" + e.CardId, row, "익히기", bx + 86, 7, 110, 36, new Action(() => Say(ImmortalService.Learn(Target, e))), !has);
        }
    }
}
