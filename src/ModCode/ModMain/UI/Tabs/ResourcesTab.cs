using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭: 영석, 아이템 지급 표, 가방 칸.
    internal sealed class ResourcesTab : EditorTab
    {
        private static readonly int[] ItemPageSizes = { 10, 20, 50 };

        private string itemQuery = "";
        private string appliedItemQuery = "";
        private int itemPage;
        private int itemPageSizeIndex;
        private float itemScrollY;
        private float itemSubScrollY;
        private string itemDetail = "";
        private Text itemDetailLabel;
        private int itemMainType = -1;
        private int itemSubType = -1;

        public override string Title
        {
            get { return "자원"; }
        }

        private void ResetItemList()
        {
            itemPage = 0;
            itemDetail = "";
            itemScrollY = 0;
            EditorWindow.ClearMessage();
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            var props = player.data.unitData.propData;
            float contentH = ContentH;
            int moneyAmount = AmountBar.Get("money");
            int itemAmount = AmountBar.Get("item");

            // 영석 추가량과 아이템 지급 수량은 따로 고른다.
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, 96, Ui.Section);
            Ui.Label(content, "영석: " + EditService.Money(player), 16, 6, 250, 40, 22);
            Ui.Label(content, "추가량", 270, 6, 70, 40, 18).color = Ui.Dim;
            float moneyRight = AmountBar.Build(content, "money", 340, 7);
            Ui.Button("MoneyAdd", content, "영석 + " + moneyAmount, moneyRight + 20, 7, 200, 38, new Action(() => Say(EditService.AddMoney(Player, moneyAmount))));
            Ui.Label(content, "아이템 지급 수량", 16, 50, 250, 40, 20);
            AmountBar.Build(content, "item", 340, 51);
            // 가방 칸. 편집기가 늘린 만큼만 되돌릴 수 있다.
            int bagExtra = BagService.Extra;
            Ui.Label(content, BagService.Describe(), 836, 50, 310, 40, 17);
            Ui.Button("BagMinus", content, "-" + BagService.Step, 1150, 51, 70, 38, new Action(() => Say(BagService.Change(-BagService.Step))), bagExtra > 0);
            Ui.Button("BagPlus", content, "+" + BagService.Step, 1226, 51, 70, 38, new Action(() => Say(BagService.Change(BagService.Step))), bagExtra < BagService.MaxExtra);

            // 필터: 아이템명
            Ui.Label(content, "아이템명", 0, 106, 90, 38);
            Ui.Input("ItemQuery", content, itemQuery, "이름의 일부 또는 번호", 90, 106, 280, 38, new Action<string>(v => itemQuery = v));
            Ui.Button("ItemSearch", content, "검색", 378, 106, 84, 38, new Action(() => { appliedItemQuery = itemQuery.Trim(); ResetItemList(); Refresh(); }));
            Ui.Button("ItemSearchClear", content, "초기화", 468, 106, 96, 38, new Action(() => { itemQuery = ""; appliedItemQuery = ""; itemMainType = -1; itemSubType = -1; ResetItemList(); Refresh(); }),
                itemQuery.Length > 0 || appliedItemQuery.Length > 0 || itemMainType >= 0);

            // 페이지당 개수
            Ui.Label(content, "페이지당", 1010, 106, 90, 38, 18).color = Ui.Dim;
            for (int i = 0; i < ItemPageSizes.Length; i++)
            {
                int index = i;
                Ui.Button("ItemPageSize" + ItemPageSizes[i], content, ItemPageSizes[i] + "개", 1100 + i * 70, 106, 64, 38,
                    new Action(() => { itemPageSizeIndex = index; ResetItemList(); Refresh(); }), true, itemPageSizeIndex == i);
            }
            int pageSize = ItemPageSizes[itemPageSizeIndex];

            // 필터: 카테고리(게임의 분류표). 첫 줄은 대분류, 대분류를 고르면 아래에 세부 분류가 나온다.
            Ui.Label(content, "카테고리", 0, 150, 90, 38);
            float cx = 90;
            Ui.Button("ItemCatAll", content, "전체", cx, 150, 80, 38, new Action(() => { itemMainType = -1; itemSubType = -1; ResetItemList(); Refresh(); }), true, itemMainType < 0);
            cx += 86;
            foreach (ItemCatalog.Category category in ItemCatalog.MainCategories)
            {
                int type = category.Id;
                string label = category.Name + " (" + category.Count + ")";
                float width = 24 + label.Length * 15;
                Ui.Button("ItemCat" + type, content, label, cx, 150, width, 38, new Action(() => { itemMainType = type; itemSubType = -1; itemSubScrollY = 0; ResetItemList(); Refresh(); }), true, itemMainType == type);
                cx += width + 4;
            }
            float top = 194;
            if (itemMainType >= 0)
            {
                // 세부 분류가 많은 대분류(도구는 50종 이상)는 두 줄만 보이게 하고 그 안에서 스크롤한다.
                const float subRow = 38;
                float subW = ContentW - 90;
                List<ItemCatalog.Category> subs = ItemCatalog.SubCategories(itemMainType);
                var places = new List<Vector2>();
                float sx = 76, sy = 0;
                foreach (ItemCatalog.Category category in subs)
                {
                    float width = SubWidth(category);
                    if (sx + width > subW - Ui.ScrollBarWidth)
                    {
                        sx = 0;
                        sy += subRow;
                    }
                    places.Add(new Vector2(sx, sy));
                    sx += width + 6;
                }
                float subsH = sy + subRow;
                float shownH = Math.Min(subsH, subRow * 2);
                Ui.Label(content, "세부", 0, top, 90, 34, 18).color = Ui.Dim;
                Transform subRoot = subsH > shownH
                    ? Ui.Scroll("ItemSubs", content, 90, top, subW, shownH, subsH, itemSubScrollY, new Action<float>(v => itemSubScrollY = v))
                    : Ui.Rect("ItemSubs", content, 90, top, subW, shownH);
                Ui.Button("ItemSubAll", subRoot, "전체", 0, 0, 70, 34, new Action(() => { itemSubType = -1; ResetItemList(); Refresh(); }), true, itemSubType < 0);
                for (int i = 0; i < subs.Count; i++)
                {
                    int sub = subs[i].Id;
                    Ui.Button("ItemSub" + sub, subRoot, subs[i].Name + " (" + subs[i].Count + ")", places[i].x, places[i].y, SubWidth(subs[i]), 34,
                        new Action(() => { itemSubType = sub; ResetItemList(); Refresh(); }), true, itemSubType == sub);
                }
                top += shownH + 4;
            }

            List<ItemCatalog.Entry> found = ItemCatalog.Find(appliedItemQuery, itemMainType, itemSubType);
            int pages = Math.Max(1, (found.Count + pageSize - 1) / pageSize);
            itemPage = Mathf.Clamp(itemPage, 0, pages - 1);

            // 표 머리
            const float colId = 8, colCat = 110, colName = 290, colLevel = 560, colOwned = 630, colDesc = 710, colGive = 1166;
            Ui.Panel("ItemHead", content, 0, top, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", colId, top, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "카테고리", colCat, top, 170, 30, 16).color = Ui.Dim;
            Ui.Label(content, "아이템명", colName, top, 260, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", colLevel, top, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "보유", colOwned, top, 70, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", colDesc, top, 440, 30, 16).color = Ui.Dim;
            Ui.Label(content, "지급", colGive, top, 116, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            top += 32;

            // 아래에서부터: 선택한 아이템의 전체 설명, 그 위에 페이징 줄, 남은 높이가 표 본문이다.
            const float detailH = 92;
            float footer = contentH - detailH - 46;
            const float rowHeight = 32;
            int first = itemPage * pageSize;
            int shown = Math.Max(0, Math.Min(pageSize, found.Count - first));
            RectTransform rowsRoot = Ui.Scroll("ItemTable", content, 0, top, ContentW, footer - 8 - top, shown * rowHeight, itemScrollY,
                new Action<float>(v => itemScrollY = v));
            if (found.Count == 0)
            {
                Ui.Label(rowsRoot, "조건에 맞는 아이템이 없습니다.", 16, 8, 800, 34, 18).color = Ui.Warn;
            }
            for (int row = 0; row < shown; row++)
            {
                ItemCatalog.Entry entry = found[first + row];
                float y = row * rowHeight;
                if (row % 2 == 1)
                {
                    Ui.Panel("Stripe", rowsRoot, 0, y, ContentW - Ui.ScrollBarWidth, rowHeight, new Color(1f, 1f, 1f, 0.04f)).GetComponent<Image>().raycastTarget = false;
                }
                Ui.Label(rowsRoot, entry.Id.ToString(), colId, y, 100, rowHeight, 16);
                Ui.Label(rowsRoot, Shorten(entry.SubTypeName, 8), colCat, y, 175, rowHeight, 16);
                Ui.Button("ItemName" + row, rowsRoot, Shorten(entry.Name, 12), colName, y + 2, 262, rowHeight - 4,
                    new Action(() => { itemDetail = Detail(entry); if (itemDetailLabel != null) { itemDetailLabel.text = itemDetail; } }));
                Ui.Label(rowsRoot, entry.Level.ToString(), colLevel, y, 60, rowHeight, 16);
                Ui.Label(rowsRoot, props.GetPropsNum(entry.Id).ToString(), colOwned, y, 70, rowHeight, 16);
                Ui.Label(rowsRoot, Shorten(entry.Desc, 27), colDesc, y, 440, rowHeight, 15).color = Ui.Dim;
                Ui.Button("ItemGive" + row, rowsRoot, "지급 x" + itemAmount, colGive, y + 2, 116, rowHeight - 4, new Action(() => Say(EditService.GiveItem(Player, entry.Id, itemAmount))));
            }

            // 페이징
            Ui.Button("ItemFirst", content, "처음", 0, footer, 80, 38, new Action(() => { itemPage = 0; itemScrollY = 0; Refresh(); }), itemPage > 0);
            Ui.Button("ItemPrev", content, "이전", 86, footer, 80, 38, new Action(() => { itemPage--; itemScrollY = 0; Refresh(); }), itemPage > 0);
            Ui.Label(content, (itemPage + 1) + " / " + pages, 172, footer, 130, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("ItemNext", content, "다음", 308, footer, 80, 38, new Action(() => { itemPage++; itemScrollY = 0; Refresh(); }), itemPage < pages - 1);
            Ui.Button("ItemLast", content, "끝", 394, footer, 80, 38, new Action(() => { itemPage = pages - 1; itemScrollY = 0; Refresh(); }), itemPage < pages - 1);
            string range = found.Count == 0 ? "0개" : (first + 1) + "~" + (first + shown) + "번째 / 조건에 맞는 아이템 " + found.Count + "개";
            Ui.Label(content, range + "  ·  목록은 마우스 휠이나 오른쪽 막대로 스크롤", 490, footer, 800, 38, 15).color = Ui.Dim;

            // 선택한 아이템의 전체 설명
            Ui.Panel("ItemDetail", content, 0, contentH - detailH, ContentW, detailH, Ui.Section);
            itemDetailLabel = Ui.Label(content, itemDetail.Length > 0 ? itemDetail : "아이템명을 누르면 여기에 전체 설명이 나옵니다.", 12, contentH - detailH + 4, ContentW - 24, detailH - 8, 16, TextAnchor.UpperLeft);
            itemDetailLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemDetailLabel.verticalOverflow = VerticalWrapMode.Truncate;
            if (itemDetail.Length == 0)
            {
                itemDetailLabel.color = Ui.Dim;
            }
        }

        private static float SubWidth(ItemCatalog.Category category)
        {
            return 46 + (category.Name.Length + category.Count.ToString().Length) * 16;
        }

        private static string Detail(ItemCatalog.Entry entry)
        {
            return "[" + entry.Id + "] " + entry.Name + "  (" + entry.TypeName + " / " + entry.SubTypeName + ", 품질 " + entry.Level + ")\n" + entry.Desc;
        }
    }
}
