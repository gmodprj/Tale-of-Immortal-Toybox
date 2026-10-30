using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 기예 탭: 게임 기예 화면의 여섯 분류. 서적을 골라 배우게 하거나 배운 기록을 지운다.
    internal sealed partial class ArtTab : EditorTab
    {
        private const float RowH = 32;
        private const float DetailH = 70;
        private const float ColId = 8, ColName = 110, ColLevel = 420, ColState = 490, ColDesc = 600, ColAct = 1166;

        private readonly ListView<ItemCatalog.Entry> list = new ListView<ItemCatalog.Entry>("ArtTable", RowH);
        private readonly DetailPane detail = new DetailPane("ArtDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");
        private int artKind;
        private string query = "";
        private HashSet<int> learnedNow = new HashSet<int>();   // 지금 분류의 배운 번호(그리기마다 새로. 묶음 3)
        private readonly Dictionary<int, string> blockReasons = new Dictionary<int, string>();   // 서적마다 "배울 수 없는 까닭". Draw 시작에 비우고 다음 Draw까지 쓴다(늦게 그리는 줄도 같은 값. 13 문서 P3)

        public override string Title
        {
            get { return "기예"; }
        }

        public ArtTab()
        {
            list.OnSelect = book => detail.Show("[" + book.Id + "] " + book.Name + "  (품질 " + book.Level + ")\n" + book.Desc);
        }

        protected override void Draw()
        {
            blockReasons.Clear();
            for (int i = 0; i < ArtService.Kinds.Length; i++)
            {
                int index = i;
                ArtService.Kind each = ArtService.Kinds[i];
                HashSet<int> learnedIds = ArtService.LearnedIds(each);
                if (i == artKind)
                {
                    learnedNow = learnedIds;
                }
                Ui.Button("ArtKind" + i, content, each.Tab + " (" + ArtService.LearnedCount(each, learnedIds) + "/" + ArtService.BookCount(each) + ")", i * 176, 0, 170, 40,
                    new Action(() => { artKind = index; list.Reset(); detail.Clear(); Redraw(); }), true, artKind == i);
            }
            // 기예 제작 재료(05 문서 62절): 연단·연기·부적 처방의 재료를 가방에 넣는 하위 화면.
            Ui.Button("ArtMaterialsOpen", content, "제작 재료…", ContentW - 180, 0, 180, 40, new Action(OpenMaterials));
            ArtService.Kind kind = ArtService.Kinds[artKind];
            list.EmptyText = "조건에 맞는 " + kind.Book + "이(가) 없습니다.";

            Ui.Label(content, kind.Book, 0, 50, 90, 38);
            SearchBox.Build(content, "ArtQuery", 90, 50, 330, query, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                query = v;
                detail.Clear();
                list.SetItems(ArtService.Books(ArtService.Kinds[artKind], query));
            }));
            // 하나씩 잊어야 되돌린다. 두 번 눌러 확인한다(리뷰 U2).
            Confirm.Button("ArtLearnAll", content, kind.Tab + " 모두 배우기", "정말 모두 배우기", 600, 50, 220, 38, "art-learn-all",
                kind.Tab + "의 기예를 모두 배웁니다(하나씩 잊어야 되돌립니다). 한 번 더 누르면 실행합니다.",
                new Action(() => Say(ArtService.LearnAll(ArtService.Kinds[artKind]))));
            Ui.Label(content, "기예는 플레이어의 기록입니다(편집 대상과 무관).", 840, 50, 460, 38, 15).color = Ui.Dim;

            Ui.Panel("ArtHead", content, 0, 98, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", ColId, 98, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, kind.Book, ColName, 98, 300, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", ColLevel, 98, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", ColState, 98, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", ColDesc, 98, 500, 30, 16).color = Ui.Dim;

            list.HeightOf = book => RowFor(RowH, Ui.TextHeight(book.Desc, 560, 15), Ui.TextHeight(book.Name, 305, 17));
            list.Build(content, 0, 130, ContentW, ContentH - 130 - DetailH - 8, ArtService.Books(kind, query), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawRow(RectTransform row, ItemCatalog.Entry book, int index)
        {
            ArtService.Kind kind = ArtService.Kinds[artKind];
            bool learned = learnedNow.Contains(book.Id);
            // 배우지 않은 것은 지금 배울 수 있는지 게임에 물어 본다.
            string blocked = null;
            if (!learned && !blockReasons.TryGetValue(book.Id, out blocked))
            {
                blocked = ArtService.BlockReason(kind, book.Id);
                blockReasons[book.Id] = blocked;
            }
            float h = row.sizeDelta.y;
            Ui.Label(row, book.Id.ToString(), ColId, 0, 100, h, 16);
            Ui.Wrapped(row, book.Name, ColName, 0, 305, h, 17);
            Ui.Label(row, book.Level.ToString(), ColLevel, 0, 60, h, 16);
            Text state = Ui.Label(row, learned ? "배움" : blocked != null ? "배울 수 없음" : "-", ColState, 0, 110, h, 15);
            state.color = learned ? Ui.Warn : Ui.Dim;
            Text desc = Ui.Wrapped(row, book.Desc, ColDesc, 0, 560, h, 15);
            desc.color = Ui.Dim;
            desc.gameObject.name = "ArtDesc" + book.Id;
            string name = "ArtAct" + book.Id;
            if (learned)
            {
                Confirm.Button(name, row, "잊기", "정말 잊기", ColAct, 2, 116, RowH - 4, "art:" + book.Id,
                    book.Name + ": 배운 기록을 지웁니다. 계속하려면 \"정말 잊기\"를 한 번 더 누르세요.",
                    new Action(() => Say(ArtService.Forget(ArtService.Kinds[artKind], book))));
            }
            else if (blocked == null)
            {
                Ui.Button(name, row, "배우기", ColAct, 2, 116, RowH - 4, new Action(() => Say(ArtService.Learn(ArtService.Kinds[artKind], book))));
            }
            else
            {
                // 지금 배울 수 없는 것: 이유를 보여 주고, 한 번 더 누르면 조건을 건너뛰고 넣는다.
                Confirm.Button(name, row, "강제 추가", "정말 강제", ColAct, 2, 116, RowH - 4, "art:" + book.Id,
                    book.Name + ": " + blocked + ". 그래도 넣으려면 \"정말 강제\"를 한 번 더 누르세요.",
                    new Action(() => Say(ArtService.ForceLearn(ArtService.Kinds[artKind], book))));
            }
        }
    }
}
