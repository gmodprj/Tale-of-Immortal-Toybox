using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 기예 탭: 게임 기예 화면의 여섯 분류. 서적을 골라 배우게 하거나 배운 기록을 지운다.
    internal sealed class ArtTab : EditorTab
    {
        private int artKind;
        private int artPage;
        private float artScrollY;
        private int artPending;   // 잊기·강제 추가 확인 중인 서적 번호
        private string artQuery = "";
        private string appliedArtQuery = "";

        public override string Title
        {
            get { return "기예"; }
        }

        protected override void Draw()
        {
            float contentH = ContentH;
            for (int i = 0; i < ArtService.Kinds.Length; i++)
            {
                int index = i;
                ArtService.Kind each = ArtService.Kinds[i];
                Ui.Button("ArtKind" + i, content, each.Tab + " (" + ArtService.LearnedCount(each) + "/" + ArtService.Books(each, "").Count + ")", i * 176, 0, 170, 40,
                    new Action(() => { artKind = index; artPage = 0; artScrollY = 0; artPending = 0; Redraw(); }), true, artKind == i);
            }
            ArtService.Kind kind = ArtService.Kinds[artKind];

            Ui.Label(content, kind.Book, 0, 50, 90, 38);
            Ui.Input("ArtQuery", content, artQuery, "이름의 일부 또는 번호", 90, 50, 280, 38, new Action<string>(v => artQuery = v));
            Ui.Button("ArtSearch", content, "검색", 378, 50, 84, 38, new Action(() => { appliedArtQuery = artQuery.Trim(); artPage = 0; artScrollY = 0; Redraw(); }));
            Ui.Button("ArtSearchClear", content, "초기화", 468, 50, 96, 38, new Action(() => { artQuery = ""; appliedArtQuery = ""; artPage = 0; artScrollY = 0; Redraw(); }),
                artQuery.Length > 0 || appliedArtQuery.Length > 0);
            Ui.Button("ArtLearnAll", content, kind.Tab + " 모두 배우기", 600, 50, 220, 38, new Action(() => { artPending = 0; Say(ArtService.LearnAll(ArtService.Kinds[artKind])); }));
            Ui.Label(content, "기예는 플레이어의 기록입니다(편집 대상과 무관).", 840, 50, 460, 38, 15).color = Ui.Dim;

            List<ItemCatalog.Entry> books = ArtService.Books(kind, appliedArtQuery);
            const int pageSize = 50;
            int pages = Math.Max(1, (books.Count + pageSize - 1) / pageSize);
            artPage = Mathf.Clamp(artPage, 0, pages - 1);

            const float colId = 8, colName = 110, colLevel = 420, colState = 490, colDesc = 600, colAct = 1166;
            Ui.Panel("ArtHead", content, 0, 98, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", colId, 98, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, kind.Book, colName, 98, 300, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", colLevel, 98, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", colState, 98, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", colDesc, 98, 500, 30, 16).color = Ui.Dim;

            const float rowHeight = 32;
            float footer = contentH - 46;
            int first = artPage * pageSize;
            int shown = Math.Max(0, Math.Min(pageSize, books.Count - first));
            RectTransform rowsRoot = Ui.Scroll("ArtTable", content, 0, 130, ContentW, footer - 8 - 130, shown * rowHeight, artScrollY, new Action<float>(v => artScrollY = v));
            if (books.Count == 0)
            {
                Ui.Label(rowsRoot, "조건에 맞는 " + kind.Book + "이(가) 없습니다.", 16, 8, 800, 34, 18).color = Ui.Warn;
            }
            for (int row = 0; row < shown; row++)
            {
                ItemCatalog.Entry book = books[first + row];
                float y = row * rowHeight;
                bool learned = ArtService.Learned(kind, book.Id);
                bool confirming = artPending == book.Id;
                if (row % 2 == 1)
                {
                    Ui.Panel("Stripe", rowsRoot, 0, y, ContentW - Ui.ScrollBarWidth, rowHeight, new Color(1f, 1f, 1f, 0.04f)).GetComponent<Image>().raycastTarget = false;
                }
                Ui.Label(rowsRoot, book.Id.ToString(), colId, y, 100, rowHeight, 16);
                Ui.Label(rowsRoot, Shorten(book.Name, 15), colName, y, 305, rowHeight, 17);
                Ui.Label(rowsRoot, book.Level.ToString(), colLevel, y, 60, rowHeight, 16);
                // 배우지 않은 것은 지금 배울 수 있는지 게임에 물어 본다.
                string blocked = learned ? null : ArtService.BlockReason(kind, book.Id);
                Text state = Ui.Label(rowsRoot, learned ? "배움" : blocked != null ? "배울 수 없음" : "-", colState, y, 110, rowHeight, 15);
                state.color = learned ? Ui.Warn : Ui.Dim;
                Ui.Label(rowsRoot, Shorten(book.Desc, 32), colDesc, y, 560, rowHeight, 15).color = Ui.Dim;
                string act = learned ? (confirming ? "정말 잊기" : "잊기") : blocked == null ? "배우기" : confirming ? "정말 강제" : "강제 추가";
                Ui.Button("ArtAct" + row, rowsRoot, act, colAct, y + 2, 116, rowHeight - 4, new Action(() =>
                {
                    ArtService.Kind current = ArtService.Kinds[artKind];
                    if (!learned && blocked == null)
                    {
                        artPending = 0;
                        Say(ArtService.Learn(current, book));
                        return;
                    }
                    if (!learned)
                    {
                        // 지금 배울 수 없는 것: 이유를 보여 주고, 한 번 더 누르면 조건을 건너뛰고 넣는다.
                        if (!confirming)
                        {
                            artPending = book.Id;
                            Say(book.Name + ": " + blocked + ". 그래도 넣으려면 \"정말 강제\"를 한 번 더 누르세요.");
                            return;
                        }
                        artPending = 0;
                        Say(ArtService.ForceLearn(current, book));
                        return;
                    }
                    if (!confirming)
                    {
                        artPending = book.Id;
                        Say(book.Name + ": 배운 기록을 지웁니다. 계속하려면 \"정말 잊기\"를 한 번 더 누르세요.");
                        return;
                    }
                    artPending = 0;
                    Say(ArtService.Forget(current, book));
                }), true, confirming);
            }

            Ui.Button("ArtPrev", content, "이전", 0, footer, 80, 38, new Action(() => { artPage--; artScrollY = 0; Refresh(); }), artPage > 0);
            Ui.Label(content, (artPage + 1) + " / " + pages, 86, footer, 110, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("ArtNext", content, "다음", 202, footer, 80, 38, new Action(() => { artPage++; artScrollY = 0; Refresh(); }), artPage < pages - 1);
            Ui.Label(content, kind.Book + " " + books.Count + "개  ·  목록은 마우스 휠이나 오른쪽 막대로 스크롤", 300, footer, 900, 38, 15).color = Ui.Dim;
        }
    }
}
