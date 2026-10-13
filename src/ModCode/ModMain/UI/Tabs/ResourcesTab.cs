using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭: 영석, 가방 칸, 아이템 지급 표.
    internal sealed class ResourcesTab : EditorTab
    {
        private const float RowH = 32;
        private const float DetailH = 92;
        private const float ColId = 8, ColCat = 110, ColName = 290, ColLevel = 560, ColOwned = 630, ColDesc = 710, ColGive = 1166;

        private readonly ListView<ItemCatalog.Entry> list = new ListView<ItemCatalog.Entry>("ItemTable", RowH);
        private readonly DetailPane detail = new DetailPane("ItemDetail", "아이템명을 누르면 여기에 전체 설명이 나옵니다.");
        private string query = "";
        private float subScrollY;
        private int mainType = -1;
        private int subType = -1;
        private string bagInput = "";

        public override string Title
        {
            get { return "자원"; }
        }

        public ResourcesTab()
        {
            list.EmptyText = "조건에 맞는 아이템이 없습니다.";
        }

        private List<ItemCatalog.Entry> Found()
        {
            return ItemCatalog.Find(query, mainType, subType);
        }

        // 분류를 바꾼다: 목록을 맨 위로, 상세를 비우고 다시 그린다.
        private void SetCategory(int main, int sub)
        {
            mainType = main;
            subType = sub;
            list.Reset();
            detail.Clear();
            Redraw();
        }

        // 가방의 추가 칸을 원하는 수로 맞춘다. 10칸 단위로 내림한 뒤 지금 값과의 차이만큼 바꾼다.
        // 범위 밖의 수는 가까운 값으로 맞추지 않고 거부한다(음수를 잘못 넣었을 때 추가 칸이 모두 사라지지 않게).
        private void SetBagExtra(int wanted)
        {
            if (wanted < 0 || wanted > BagService.MaxExtra)
            {
                Say("가방: 추가 칸은 0~" + BagService.MaxExtra + " 사이의 숫자로 입력하세요.");
                return;
            }
            int target = wanted / BagService.Step * BagService.Step;
            int delta = target - BagService.Extra;
            if (delta == 0)
            {
                Say("가방: 추가 칸은 이미 +" + target + "입니다.");
                return;
            }
            Say(BagService.Change(delta));
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            int moneyAmount = AmountBar.Get("money");
            int itemAmount = AmountBar.Get("item");

            // 영석, 아이템 지급 수량, 가방 칸
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, 140, Ui.Section);
            Ui.Label(content, "영석: " + EditService.Money(player), 16, 6, 250, 40, 22);
            Ui.Label(content, "추가량", 270, 6, 70, 40, 18).color = Ui.Dim;
            float moneyRight = AmountBar.Build(content, "money", 340, 7);
            Ui.Button("MoneyAdd", content, "영석 + " + moneyAmount, moneyRight + 20, 7, 200, 38, new Action(() => Say(EditService.AddMoney(Player, moneyAmount))));
            Ui.Label(content, "아이템 지급 수량", 16, 50, 250, 40, 20);
            AmountBar.Build(content, "item", 340, 51);
            // 가방 칸. 편집기가 늘린 만큼만 되돌릴 수 있다.
            int bagExtra = BagService.Extra;
            bagInput = bagExtra.ToString();
            Ui.Label(content, BagService.Describe(), 16, 94, 320, 40, 18);
            Ui.Label(content, "추가 칸", 340, 94, 76, 40, 18).color = Ui.Dim;
            NumberField.Build(content, "BagExtra", 416, 95, 100, 38, bagInput, new Action<string>(v => bagInput = v), new Action<int>(SetBagExtra));
            Ui.Button("BagApply", content, "적용", 522, 95, 70, 38, new Action(() =>
            {
                int value;
                if (!int.TryParse(bagInput, out value))
                {
                    Say("가방: 추가 칸을 숫자로 입력하세요.");
                    return;
                }
                SetBagExtra(value);
            }));
            Ui.Button("BagMinus", content, "-" + BagService.Step, 610, 95, 70, 38, new Action(() => Say(BagService.Change(-BagService.Step))), bagExtra > 0);
            Ui.Button("BagPlus", content, "+" + BagService.Step, 686, 95, 70, 38, new Action(() => Say(BagService.Change(BagService.Step))), bagExtra < BagService.MaxExtra);
            Ui.Label(content, "10칸 단위, 0~" + BagService.MaxExtra + ". 숫자를 쓰고 Enter를 눌러도 됩니다.", 770, 94, 520, 40, 15).color = Ui.Dim;

            // 이름 검색(치는 대로 걸러진다)과 초기화
            Ui.Label(content, "아이템명", 0, 150, 90, 38);
            SearchBox.Build(content, "ItemQuery", 90, 150, 330, query, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                query = v;
                detail.Clear();
                list.SetItems(Found());
            }));
            Ui.Button("ItemSearchClear", content, "분류·검색 초기화", 430, 150, 190, 38, new Action(() => { query = ""; SetCategory(-1, -1); }),
                query.Length > 0 || mainType >= 0);

            // 카테고리(게임의 분류표). 첫 줄은 대분류, 대분류를 고르면 아래에 세부 분류가 나온다.
            Ui.Label(content, "카테고리", 0, 194, 90, 38);
            float cx = 90;
            Ui.Button("ItemCatAll", content, "전체", cx, 194, 80, 38, new Action(() => SetCategory(-1, -1)), true, mainType < 0);
            cx += 86;
            foreach (ItemCatalog.Category category in ItemCatalog.MainCategories)
            {
                int type = category.Id;
                string label = category.Name + " (" + category.Count + ")";
                float width = 24 + label.Length * 15;
                Ui.Button("ItemCat" + type, content, label, cx, 194, width, 38, new Action(() => { subScrollY = 0; SetCategory(type, -1); }), true, mainType == type);
                cx += width + 4;
            }
            float top = 238;
            if (mainType >= 0)
            {
                // 세부 분류가 많은 대분류(도구는 50종 이상)는 두 줄만 보이게 하고 그 안에서 스크롤한다.
                const float subRow = 38;
                float subW = ContentW - 90;
                List<ItemCatalog.Category> subs = ItemCatalog.SubCategories(mainType);
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
                    ? Ui.Scroll("ItemSubs", content, 90, top, subW, shownH, subsH, subScrollY, new Action<float>(v => subScrollY = v))
                    : Ui.Rect("ItemSubs", content, 90, top, subW, shownH);
                int main = mainType;
                Ui.Button("ItemSubAll", subRoot, "전체", 0, 0, 70, 34, new Action(() => SetCategory(main, -1)), true, subType < 0);
                for (int i = 0; i < subs.Count; i++)
                {
                    int sub = subs[i].Id;
                    Ui.Button("ItemSub" + sub, subRoot, subs[i].Name + " (" + subs[i].Count + ")", places[i].x, places[i].y, SubWidth(subs[i]), 34,
                        new Action(() => SetCategory(main, sub)), true, subType == sub);
                }
                top += shownH + 4;
            }

            // 표 머리
            Ui.Panel("ItemHead", content, 0, top, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", ColId, top, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "카테고리", ColCat, top, 170, 30, 16).color = Ui.Dim;
            Ui.Label(content, "아이템명", ColName, top, 260, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", ColLevel, top, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "보유", ColOwned, top, 70, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", ColDesc, top, 440, 30, 16).color = Ui.Dim;
            Ui.Label(content, "지급", ColGive, top, 116, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            top += 32;

            // 표 본문과 이동 줄, 그 아래에 고른 아이템의 전체 설명
            list.HeightOf = entry => RowFor(RowH, Ui.TextHeight(entry.Desc, 440, 15), Ui.TextHeight(entry.Name, 262, 19), Ui.TextHeight(entry.SubTypeName, 175, 16));
            list.Build(content, 0, top, ContentW, ContentH - DetailH - 8 - top, Found(), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawRow(RectTransform row, ItemCatalog.Entry entry, int index)
        {
            float h = row.sizeDelta.y;
            Ui.Label(row, entry.Id.ToString(), ColId, 0, 100, h, 16);
            Ui.Wrapped(row, entry.SubTypeName, ColCat, 0, 175, h, 16);
            WrapLabel(Ui.Button("ItemName" + entry.Id, row, entry.Name, ColName, 2, 262, h - 4, new Action(() => detail.Show(Detail(entry)))));
            Ui.Label(row, entry.Level.ToString(), ColLevel, 0, 60, h, 16);
            Ui.Label(row, Player.data.unitData.propData.GetPropsNum(entry.Id).ToString(), ColOwned, 0, 70, h, 16);
            Text desc = Ui.Wrapped(row, entry.Desc, ColDesc, 0, 440, h, 15);
            desc.color = Ui.Dim;
            desc.gameObject.name = "ItemDesc" + entry.Id;
            int amount = AmountBar.Get("item");
            Ui.Button("ItemGive" + entry.Id, row, "지급 x" + amount, ColGive, 2, 116, RowH - 4, new Action(() => Say(EditService.GiveItem(Player, entry.Id, amount))));
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
