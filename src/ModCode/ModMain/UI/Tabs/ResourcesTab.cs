using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭: 영석, 지급 수량과 가방 칸, 용도 그룹으로 나눈 아이템 표와 즐겨찾기.
    // 설계: docs/superpowers/specs/2026-10-15-resources-tab-design.md 3·4절. 판단은 ItemCatalog·FavoriteStore가 하고 여기서는 그린다.
    internal sealed class ResourcesTab : EditorTab
    {
        private const float RowH = 32;
        private const float DetailH = 92;
        private const float TopH = 92;                  // 1줄 영석, 2줄 지급 수량과 가방 칸
        private const float LeftW = 170;                // 그룹 목록
        private const float RightX = 180;
        private const float RightW = ContentW - RightX; // 1124
        private const float ChipH = 34, ChipGap = 6;
        private const int ChipRowsShown = 3;
        // 표의 열(목록 안의 x). 지급 열은 목록 안쪽 폭(RightW - 스크롤 막대)의 오른쪽 끝에 붙인다.
        private const float ColStar = 4, ColName = 50, ColKind = 256, ColLevel = 372, ColOwned = 420, ColDesc = 486;
        private const float NameW = 200, KindW = 110, GiveW = 116, AmountW = 90;
        private const float ColGive = RightW - Ui.ScrollBarWidth - GiveW - 4;   // 990
        private const float ColAmount = ColGive - AmountW - 6;                  // 894
        private const float DescW = ColGive - ColDesc - 8;                      // 496
        private const float FavDescW = ColAmount - ColDesc - 8;                 // 400

        // 검색어가 바뀌면 탭을 다시 그리지 않고(검색 칸의 포커스를 지킨다) 이 글들만 고친다.
        private sealed class CountLabel
        {
            public Text Text;
            public string Name;
            public string Key;      // 그룹 버튼이면 그룹 키, 종류 칩이면 null
            public int Index;       // 종류 칩의 순번(0이 전체)
            public void Set(int count)
            {
                Text.text = Key != null ? Name + " " + count : Name + " (" + count + ")";
            }
        }

        private readonly ListView<ItemCatalog.Entry> list = new ListView<ItemCatalog.Entry>("ItemTable", RowH);
        private readonly DetailPane detail = new DetailPane("ItemDetail", "아이템명을 누르면 여기에 전체 설명이 나옵니다.");
        private readonly ItemCatalog.Filter filter = new ItemCatalog.Filter();
        private readonly List<CountLabel> labels = new List<CountLabel>();
        private string groupKey = ItemGroups.AllKey;
        private float chipScrollY;
        private string bagInput = "";

        public override string Title
        {
            get { return "자원"; }
        }

        private bool IsFav
        {
            get { return groupKey == ItemGroups.FavKey; }
        }

        public override void OnShow()
        {
            string notice = FavoriteStore.TakeNotice();
            if (notice != null)
            {
                Say(notice);
            }
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

        // 그룹·종류·품질·보유 중만을 바꾼다: 목록을 맨 위로, 설명 칸을 비우고 다시 그린다.
        private void Changed()
        {
            list.Reset();
            detail.Clear();
            Redraw();
        }

        private void SetGroup(string key)
        {
            groupKey = key;
            filter.Kind = null;
            chipScrollY = 0;
            Changed();
        }

        private void ClearFilters()
        {
            filter.Query = "";
            filter.Kind = null;
            filter.Quality = 0;
            filter.OwnedOnly = false;
            Changed();
        }

        private static ItemCatalog.Entry Missing(int id)
        {
            return new ItemCatalog.Entry
            {
                Id = id,
                Name = "게임에 없는 아이템 (" + id + ")",
                Desc = "즐겨찾기에 남아 있지만 지금 게임의 아이템 표에 없습니다(그 아이템을 넣은 모드를 뺐을 수 있습니다). ★를 누르면 뺍니다.",
                Search = "",
                Missing = true,
            };
        }

        // 지금 보일 줄. 즐겨찾기 보기는 넣은 순서, 나머지는 그룹·종류·게임 표 순서.
        private List<ItemCatalog.Entry> Rows(HashSet<int> owned)
        {
            if (!IsFav)
            {
                filter.Group = groupKey;
                return ItemCatalog.Select(filter, owned);
            }
            var rows = new List<ItemCatalog.Entry>();
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                ItemCatalog.Entry entry = ItemCatalog.Get(favorite.Id) ?? Missing(favorite.Id);
                if (ItemCatalog.Passes(entry, filter, owned))
                {
                    rows.Add(entry);
                }
            }
            return rows;
        }

        private int FavCount(HashSet<int> owned)
        {
            int count = 0;
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                ItemCatalog.Entry entry = ItemCatalog.Get(favorite.Id) ?? Missing(favorite.Id);
                if (ItemCatalog.Passes(entry, filter, owned))
                {
                    count++;
                }
            }
            return count;
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            HashSet<int> owned = ItemCatalog.OwnedIds(player);
            labels.Clear();
            DrawTop(player);
            DrawGroups(owned);
            float y = TopH + 8;
            y = DrawKinds(y, owned);
            y = DrawFilters(y);
            DrawHead(y);
            y += 32;
            list.EmptyText = IsFav ? "즐겨찾기가 비어 있습니다. 표의 ☆를 눌러 넣습니다." : "조건에 맞는 아이템이 없습니다.";
            float descW = IsFav ? FavDescW : DescW;
            list.HeightOf = entry => RowFor(RowH, Ui.TextHeight(entry.Desc, descW, 15), Ui.TextHeight(entry.Name, NameW, 19),
                Ui.TextHeight(entry.Kind == null ? "-" : entry.Kind.Name, KindW, 16));
            list.Build(content, RightX, y, RightW, ContentH - DetailH - 8 - y, Rows(owned), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawTop(WorldUnitBase player)
        {
            int moneyAmount = AmountBar.Get("money");
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, TopH, Ui.Section);
            // 1줄: 영석
            Ui.Label(content, "영석: " + EditService.Money(player), 12, 4, 250, 40, 20);
            Ui.Label(content, "추가량", 270, 4, 70, 40, 18).color = Ui.Dim;
            float moneyRight = AmountBar.Build(content, "money", 340, 5);
            Ui.Button("MoneyAdd", content, "영석 + " + moneyAmount, moneyRight + 16, 5, 200, 38, new Action(() => Say(EditService.AddMoney(Player, moneyAmount))));
            // 2줄: 아이템 지급 수량과 가방 칸. 가방 칸은 편집기가 늘린 만큼만 되돌릴 수 있다.
            Ui.Label(content, "지급 수량", 12, 48, 110, 40, 18);
            float itemRight = AmountBar.Build(content, "item", 122, 49);
            int bagExtra = BagService.Extra;
            bagInput = bagExtra.ToString();
            float bagX = itemRight + 16;
            Ui.Label(content, BagService.Describe(), bagX, 48, 296, 40, 17);
            float bx = bagX + 300;
            NumberField.Build(content, "BagExtra", bx, 49, 76, 38, bagInput, new Action<string>(v => bagInput = v), new Action<int>(SetBagExtra));
            Ui.Button("BagApply", content, "적용", bx + 80, 49, 60, 38, new Action(() =>
            {
                int value;
                if (!int.TryParse(bagInput, out value))
                {
                    Say("가방: 추가 칸을 숫자로 입력하세요.");
                    return;
                }
                SetBagExtra(value);
            }));
            Ui.Button("BagMinus", content, "-" + BagService.Step, bx + 144, 49, 60, 38, new Action(() => Say(BagService.Change(-BagService.Step))), bagExtra > 0);
            Ui.Button("BagPlus", content, "+" + BagService.Step, bx + 208, 49, 60, 38, new Action(() => Say(BagService.Change(BagService.Step))), bagExtra < BagService.MaxExtra);
        }

        private void DrawGroups(HashSet<int> owned)
        {
            Dictionary<string, int> counts = ItemCatalog.CountByGroup(filter, owned);
            float y = TopH + 8;
            GroupButton(ItemGroups.FavKey, "★ 즐겨찾기", FavCount(owned), y);
            y += 44;
            GroupButton(ItemGroups.AllKey, "전체", counts[ItemGroups.AllKey], y);
            y += 44;
            foreach (ItemGroups.Group group in ItemGroups.Groups)
            {
                GroupButton(group.Key, group.Name, counts[group.Key], y);
                y += 44;
            }
        }

        private void GroupButton(string key, string name, int count, float y)
        {
            Button button = Ui.Button("ResGroup" + key, content, name + " " + count, 0, y, LeftW, 40, new Action(() => SetGroup(key)), true, groupKey == key);
            labels.Add(new CountLabel { Text = button.GetComponentInChildren<Text>(), Name = name, Key = key });
        }

        // 종류 칩(그룹 보기), 모두 지급(즐겨찾기 보기) 또는 안내(전체 보기). 다음 줄의 y를 돌려준다.
        private float DrawKinds(float y, HashSet<int> owned)
        {
            if (IsFav)
            {
                Confirm.Button("ResAllGive", content, "모두 지급", "정말 모두 지급", RightX, y, 170, 38, "res-all-give",
                    "보이는 즐겨찾기를 저장된 수량으로 모두 지급합니다. 한 번 더 누르면 실행합니다.", new Action(GiveAll));
                Ui.Label(content, "수량 칸에 쓰고 Enter를 누르면 저장됩니다. ☆로 넣고 ★로 뺍니다.", RightX + 182, y, RightW - 182, 38, 16).color = Ui.Dim;
                return y + 44;
            }
            if (groupKey == ItemGroups.AllKey)
            {
                Text hint = Ui.Label(content, "왼쪽에서 그룹을 고르면 종류로 나눠 볼 수 있습니다.", RightX, y, RightW, 38, 17);
                hint.color = Ui.Dim;
                hint.gameObject.name = "ResAllHint";
                return y + 44;
            }
            ItemGroups.Group group = ItemGroups.ByKey(groupKey);
            int[] counts = ItemCatalog.CountByKind(group, filter, owned);
            int[] widest = ItemCatalog.CountByKind(group, new ItemCatalog.Filter(), owned);   // 칩의 폭은 거르기 전의 수로 잰다
            var names = new List<string> { "전체" };
            var shown = new List<int>();
            var sized = new List<int>();
            int shownAll = 0, sizedAll = 0;
            for (int i = 0; i < group.Kinds.Count; i++)
            {
                names.Add(group.Kinds[i].Name);
                shown.Add(counts[i]);
                sized.Add(widest[i]);
                shownAll += counts[i];
                sizedAll += widest[i];
            }
            shown.Insert(0, shownAll);
            sized.Insert(0, sizedAll);
            // 칩을 줄바꿈해 모두 놓는다. 세 줄을 넘으면 그 영역 안에서 스크롤한다.
            float areaW = RightW - Ui.ScrollBarWidth;
            var xs = new float[names.Count];
            var ys = new float[names.Count];
            var ws = new float[names.Count];
            float cx = 0, cy = 0;
            for (int i = 0; i < names.Count; i++)
            {
                float w = Math.Max(70f, Ui.TextWidth(names[i] + " (" + sized[i] + ")", 19) + 28f);
                if (cx > 0 && cx + w > areaW)
                {
                    cx = 0;
                    cy += ChipH + ChipGap;
                }
                xs[i] = cx;
                ys[i] = cy;
                ws[i] = w;
                cx += w + ChipGap;
            }
            float allH = cy + ChipH;
            float shownH = Math.Min(allH, ChipRowsShown * (ChipH + ChipGap) - ChipGap);
            Transform area = allH > shownH
                ? Ui.Scroll("ResKinds", content, RightX, y, RightW, shownH, allH, chipScrollY, new Action<float>(v => chipScrollY = v))
                : Ui.Rect("ResKinds", content, RightX, y, RightW, shownH);
            for (int i = 0; i < names.Count; i++)
            {
                ItemGroups.Kind kind = i == 0 ? null : group.Kinds[i - 1];
                Button chip = Ui.Button("ResKind" + i, area, names[i] + " (" + shown[i] + ")", xs[i], ys[i], ws[i], ChipH,
                    new Action(() => { filter.Kind = kind; Changed(); }), true, filter.Kind == kind);
                labels.Add(new CountLabel { Text = chip.GetComponentInChildren<Text>(), Name = names[i], Index = i });
            }
            return y + shownH + 6;
        }

        private float DrawFilters(float y)
        {
            Ui.Label(content, "아이템명", RightX, y, 76, 38, 18);
            SearchBox.Build(content, "ItemQuery", RightX + 76, y, 290, filter.Query, "이름의 일부 또는 번호", new Action<string>(OnQuery));
            Ui.Label(content, "품질(높을수록 귀함)", RightX + 376, y, 150, 38, 16).color = Ui.Dim;
            Ui.Button("ResQuality0", content, "전체", RightX + 526, y, 56, 38, new Action(() => { filter.Quality = 0; Changed(); }), true, filter.Quality == 0);
            for (int q = 1; q <= 6; q++)
            {
                int quality = q;
                Ui.Button("ResQuality" + q, content, q.ToString(), RightX + 586 + (q - 1) * 44, y, 40, 38,
                    new Action(() => { filter.Quality = quality; Changed(); }), true, filter.Quality == q);
            }
            Ui.Button("ResOwnedOnly", content, "보유 중만", RightX + 856, y, 120, 38, new Action(() => { filter.OwnedOnly = !filter.OwnedOnly; Changed(); }), true, filter.OwnedOnly);
            Ui.Button("ResClear", content, "초기화", RightX + 982, y, 100, 38, new Action(ClearFilters));
            return y + 44;
        }

        private void DrawHead(float y)
        {
            Ui.Panel("ResHead", content, RightX, y, RightW, 30, Ui.Section);
            Ui.Label(content, "☆", RightX + ColStar, y, 40, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            Ui.Label(content, "아이템명", RightX + ColName, y, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "종류", RightX + ColKind, y, KindW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", RightX + ColLevel, y, 44, 30, 16).color = Ui.Dim;
            Ui.Label(content, "보유", RightX + ColOwned, y, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", RightX + ColDesc, y, IsFav ? FavDescW : DescW, 30, 16).color = Ui.Dim;
            if (IsFav)
            {
                Ui.Label(content, "수량", RightX + ColAmount, y, AmountW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            }
            Ui.Label(content, "지급", RightX + ColGive, y, GiveW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
        }

        // 검색어가 바뀌었을 때: 목록과 개수 글만 고친다.
        private void OnQuery(string value)
        {
            filter.Query = value;
            detail.Clear();
            HashSet<int> owned = ItemCatalog.OwnedIds(Player);
            list.SetItems(Rows(owned));
            Dictionary<string, int> counts = ItemCatalog.CountByGroup(filter, owned);
            int[] kinds = IsFav || groupKey == ItemGroups.AllKey ? null : ItemCatalog.CountByKind(ItemGroups.ByKey(groupKey), filter, owned);
            int favCount = FavCount(owned);
            foreach (CountLabel label in labels)
            {
                if (label.Key == ItemGroups.FavKey)
                {
                    label.Set(favCount);
                }
                else if (label.Key != null)
                {
                    label.Set(counts[label.Key]);
                }
                else if (kinds != null)
                {
                    int sum = 0;
                    foreach (int n in kinds)
                    {
                        sum += n;
                    }
                    label.Set(label.Index == 0 ? sum : kinds[label.Index - 1]);
                }
            }
        }

        private void DrawRow(RectTransform row, ItemCatalog.Entry entry, int index)
        {
            float h = row.sizeDelta.y;
            int id = entry.Id;
            bool fav = FavoriteStore.Has(id);
            Ui.Button("ResStar" + id, row, fav ? "★" : "☆", ColStar, 2, 40, RowH - 4, new Action(() => ToggleFavorite(id)), true, fav);
            WrapLabel(Ui.Button("ItemName" + id, row, entry.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(Detail(entry)))));
            Ui.Wrapped(row, entry.Kind == null ? "-" : entry.Kind.Name, ColKind, 0, KindW, h, 16);
            Ui.Label(row, entry.Missing ? "-" : entry.Level.ToString(), ColLevel, 0, 44, h, 16);
            Ui.Label(row, entry.Missing ? "-" : Player.data.unitData.propData.GetPropsNum(id).ToString(), ColOwned, 0, 60, h, 16);
            Text desc = Ui.Wrapped(row, entry.Desc, ColDesc, 0, IsFav ? FavDescW : DescW, h, 15);
            desc.color = Ui.Dim;
            desc.gameObject.name = "ItemDesc" + id;
            if (IsFav)
            {
                int saved = FavoriteStore.AmountOf(id);
                NumberField.Build(row, "ResAmount" + id, ColAmount, (h - (RowH - 4)) / 2, AmountW, RowH - 4, saved.ToString(), null,
                    new Action<int>(v => SetAmount(id, v)));
                Ui.Button("ItemGive" + id, row, "지급 x" + saved, ColGive, 2, GiveW, RowH - 4,
                    new Action(() => Say(EditService.GiveItem(Player, id, FavoriteStore.AmountOf(id)))), !entry.Missing);
            }
            else
            {
                int amount = AmountBar.Get("item");
                Ui.Button("ItemGive" + id, row, "지급 x" + amount, ColGive, 2, GiveW, RowH - 4, new Action(() => Say(EditService.GiveItem(Player, id, amount))));
            }
        }

        private void ToggleFavorite(int id)
        {
            string text;
            if (FavoriteStore.Has(id))
            {
                FavoriteStore.Remove(id, out text);
            }
            else
            {
                FavoriteStore.Add(id, AmountBar.Get("item"), out text);
            }
            Say(text);
        }

        private void SetAmount(int id, int amount)
        {
            string text;
            FavoriteStore.SetAmount(id, amount, out text);
            Say(text);
        }

        // 보이는(걸러진) 즐겨찾기 가운데 게임에 있는 것을 저장된 수량으로 차례로 준다.
        private void GiveAll()
        {
            int given = 0;
            var failed = new List<string>();
            foreach (ItemCatalog.Entry entry in Rows(ItemCatalog.OwnedIds(Player)))
            {
                if (entry.Missing)
                {
                    continue;
                }
                string text;
                if (EditService.TryGiveItem(Player, entry.Id, FavoriteStore.AmountOf(entry.Id), out text))
                {
                    given++;
                }
                else
                {
                    failed.Add(entry.Name);
                }
            }
            Say(given + failed.Count == 0 ? "즐겨찾기 모두 지급: 줄 수 있는 아이템이 없습니다."
                : "즐겨찾기 모두 지급: 넣음 " + given + ", 못 넣음 " + failed.Count + (failed.Count > 0 ? "(" + string.Join(", ", failed.ToArray()) + ")" : "") + ".");
        }

        private static string Detail(ItemCatalog.Entry entry)
        {
            if (entry.Missing)
            {
                return "[" + entry.Id + "] " + entry.Name + "\n" + entry.Desc;
            }
            return "[" + entry.Id + "] " + entry.Name + "  (" + entry.Group.Name + " / " + entry.Kind.Name + ", 품질 " + entry.Level + ")\n" + entry.Desc;
        }
    }
}
