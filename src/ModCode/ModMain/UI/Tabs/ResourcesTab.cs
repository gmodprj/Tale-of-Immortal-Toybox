using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭: 영석, 지급 수량과 가방 칸, 용도 그룹으로 나눈 아이템 표, 기술 서적(비급) 지급, 즐겨찾기.
    // 설계: docs/superpowers/specs/2026-10-15-resources-tab-design.md, 2026-10-15-martial-books-design.md.
    // 판단은 ItemCatalog·FavoriteStore·MartialBookService가 하고 여기서는 그린다. 기술 서적 그룹은 ResourcesTab.Books.cs.
    internal sealed partial class ResourcesTab : EditorTab
    {
        private const float RowH = 32;
        private const float DetailH = 150;              // 표에 설명 열이 없어 설명은 이 칸에서 본다. 넘치면 칸 안에서 스크롤한다
        private const float TopH = 92;                  // 1줄 영석, 2줄 지급 수량과 가방 칸
        private const float LeftW = 170;                // 그룹 목록
        private const float GroupStep = 42;             // 그룹 버튼(높이 38)의 간격. 열네 개가 아래 설명 칸 위에 들어간다
        private const float RightX = 180;
        private const float RightW = ContentW - RightX; // 1124
        private const float ChipH = 34, ChipGap = 6;
        private const int ChipRowsShown = 3;
        // 표의 열(목록 안의 x). 설명은 표에 두지 않는다(줄이 높아져 한 화면에 몇 줄 안 보였다. 사용자 지시 2026-10-15).
        // 지급 열은 목록 안쪽 폭(RightW - 스크롤 막대)의 오른쪽 끝에 붙인다.
        private const float ColStar = 4, ColName = 50, ColKind = 436, ColLevel = 602, ColOwned = 652;
        private const float NameW = 380, KindW = 160, GiveW = 116, AmountW = 90;
        private const float ColGive = RightW - Ui.ScrollBarWidth - GiveW - 4;   // 990
        private const float ColAmount = ColGive - AmountW - 6;                  // 894

        // 표의 한 줄: 아이템(Item), 기술 서적 그룹의 기술(Skill), 즐겨찾기의 비급(Book) 가운데 하나.
        private sealed class Row
        {
            public ItemCatalog.Entry Item;
            public SkillCatalog.Entry Skill;            // 기술 줄과 비급 즐겨찾기 줄. 비급 즐겨찾기의 기술이 게임에 없으면 null
            public int SkillKind;
            public FavoriteStore.Favorite Book;
        }

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

        private readonly ListView<Row> list = new ListView<Row>("ItemTable", RowH);
        private readonly DetailPane detail = new DetailPane("ItemDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");
        private readonly ItemCatalog.Filter filter = new ItemCatalog.Filter();
        private readonly List<CountLabel> labels = new List<CountLabel>();
        private string groupKey = ItemGroups.AllKey;
        private float chipScrollY;
        private string bagInput = "";
        private Dictionary<int, int> bagBooks = new Dictionary<int, int>();   // 가방의 기술별 비급 수(그릴 때마다 센다)

        public override string Title
        {
            get { return "자원"; }
        }

        public ResourcesTab()
        {
            // 줄의 빈 곳을 눌러도 아래 칸에 설명이 나온다(이름 버튼과 같다).
            list.OnSelect = row => detail.Show(DetailOf(row));
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

        // 그룹·종류·품질·보유 중만 등을 바꾼다: 목록을 맨 위로, 설명 칸을 비우고 다시 그린다.
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
            ClearBookFilters();
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

        private int BagBooks(int baseId)
        {
            int n;
            return bagBooks.TryGetValue(baseId, out n) ? n : 0;
        }

        private Row FavRow(FavoriteStore.Favorite favorite)
        {
            if (!favorite.Book)
            {
                return new Row { Item = ItemCatalog.Get(favorite.Id) ?? Missing(favorite.Id) };
            }
            return new Row { Book = favorite, SkillKind = favorite.BookKind, Skill = SkillCatalog.Get(favorite.BookKind, favorite.Id) };
        }

        // 즐겨찾기 줄의 거르기. 비급 줄: 검색은 기술 이름, 품질은 최소 품질이 같은 것, 보유 중만은 가방에 그 기술의 비급이 있는 것.
        // 게임에 없는 아이템·기술은 늘 보인다(뺄 수 있게).
        private bool FavPasses(Row row, HashSet<int> owned)
        {
            if (row.Item != null)
            {
                return ItemCatalog.Passes(row.Item, filter, owned);
            }
            if (row.Skill == null)
            {
                return true;
            }
            if (filter.Quality > 0 && row.Book.MinLevel != filter.Quality)
            {
                return false;
            }
            if (filter.OwnedOnly && BagBooks(row.Book.Id) == 0)
            {
                return false;
            }
            return filter.Needle.Length == 0 || row.Skill.Search.Contains(filter.Needle) || (filter.IsId && row.Skill.BaseId == filter.Id);
        }

        // 지금 보일 줄. 즐겨찾기 보기는 넣은 순서, 기술 서적은 분류·기술 표 순서, 나머지는 그룹·종류·게임 표 순서.
        private List<Row> Rows(HashSet<int> owned)
        {
            if (groupKey == ManualKey)
            {
                return BookRows();
            }
            var rows = new List<Row>();
            if (!IsFav)
            {
                filter.Group = groupKey;
                foreach (ItemCatalog.Entry entry in ItemCatalog.Select(filter, owned))
                {
                    rows.Add(new Row { Item = entry });
                }
                return rows;
            }
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                Row row = FavRow(favorite);
                if (FavPasses(row, owned))
                {
                    rows.Add(row);
                }
            }
            return rows;
        }

        private int FavCount(HashSet<int> owned)
        {
            int count = 0;
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                if (FavPasses(FavRow(favorite), owned))
                {
                    count++;
                }
            }
            return count;
        }

        private string NameOf(Row row)
        {
            if (row.Item != null)
            {
                return row.Item.Name;
            }
            return row.Book != null ? BookFavName(row) : row.Skill.Name;
        }

        private static string KindOf(Row row)
        {
            if (row.Item != null)
            {
                return row.Item.Kind == null ? "-" : row.Item.Kind.Name;
            }
            string tab = SkillCatalog.Kinds[row.SkillKind].Tab;
            return row.Book != null ? "비급·" + tab : tab;
        }

        private string DetailOf(Row row)
        {
            if (row.Item != null)
            {
                return Detail(row.Item);
            }
            return row.Book != null ? BookFavDetail(row) : SkillDetail(row);
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            HashSet<int> owned = ItemCatalog.OwnedIds(player);
            bagBooks = MartialBookService.CountAllInBag(player);
            PrepareBooks(player);
            labels.Clear();
            DrawTop(player);
            DrawGroups(owned);
            float y = TopH + 8;
            y = DrawKinds(y, owned);
            y = groupKey == ManualKey ? DrawBookFilters(y) : DrawFilters(y);
            if (groupKey == ManualKey)
            {
                DrawBookHead(y);
            }
            else
            {
                DrawHead(y);
            }
            y += 32;
            list.EmptyText = IsFav ? "즐겨찾기가 비어 있습니다. 표의 ☆를 눌러 넣습니다."
                : groupKey == ManualKey ? "조건에 맞는 기술이 없습니다. '이 경지만'을 끄면 다른 경지의 기술도 보입니다."
                : "조건에 맞는 아이템이 없습니다.";
            list.HeightOf = row => RowFor(RowH, Ui.TextHeight(NameOf(row), NameW, 19), Ui.TextHeight(KindOf(row), KindW, 16));
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
            y += GroupStep;
            GroupButton(ItemGroups.AllKey, "전체", counts[ItemGroups.AllKey], y);
            y += GroupStep;
            foreach (ItemGroups.Group group in ItemGroups.Groups)
            {
                GroupButton(group.Key, group.Name, counts[group.Key], y);
                y += GroupStep;
                if (group.Key == "Book")
                {
                    GroupButton(ManualKey, "기술 서적", BookCount(), y);
                    y += GroupStep;
                }
            }
        }

        private void GroupButton(string key, string name, int count, float y)
        {
            Button button = Ui.Button("ResGroup" + key, content, name + " " + count, 0, y, LeftW, GroupStep - 4, new Action(() => SetGroup(key)), true, groupKey == key);
            labels.Add(new CountLabel { Text = button.GetComponentInChildren<Text>(), Name = name, Key = key });
        }

        // 종류 칩(그룹 보기), 모두 지급(즐겨찾기 보기) 또는 안내(전체 보기). 다음 줄의 y를 돌려준다.
        private float DrawKinds(float y, HashSet<int> owned)
        {
            if (groupKey == ManualKey)
            {
                return DrawBookKinds(y);
            }
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
            int[] kinds = IsFav || groupKey == ItemGroups.AllKey ? null
                : groupKey == ManualKey ? BookKindCounts()
                : ItemCatalog.CountByKind(ItemGroups.ByKey(groupKey), filter, owned);
            int favCount = FavCount(owned);
            int bookCount = BookCount();
            foreach (CountLabel label in labels)
            {
                if (label.Key == ItemGroups.FavKey)
                {
                    label.Set(favCount);
                }
                else if (label.Key == ManualKey)
                {
                    label.Set(bookCount);
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

        private void DrawRow(RectTransform row, Row r, int index)
        {
            float h = row.sizeDelta.y;
            if (r.Item != null)
            {
                DrawItemRow(row, r.Item, h);
            }
            else if (r.Book != null)
            {
                DrawBookFavRow(row, r, h);
            }
            else
            {
                DrawSkillRow(row, r, h);
            }
        }

        private void DrawItemRow(RectTransform row, ItemCatalog.Entry entry, float h)
        {
            int id = entry.Id;
            bool fav = FavoriteStore.Has(id);
            Ui.Button("ResStar" + id, row, fav ? "★" : "☆", ColStar, 2, 40, RowH - 4, new Action(() => ToggleFavorite(id)), true, fav);
            WrapLabel(Ui.Button("ItemName" + id, row, entry.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(Detail(entry)))));
            Ui.Wrapped(row, entry.Kind == null ? "-" : entry.Kind.Name, ColKind, 0, KindW, h, 16);
            Ui.Label(row, entry.Missing ? "-" : entry.Level.ToString(), ColLevel, 0, 44, h, 16);
            Ui.Label(row, entry.Missing ? "-" : Player.data.unitData.propData.GetPropsNum(id).ToString(), ColOwned, 0, 60, h, 16);
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

        // 보이는(걸러진) 즐겨찾기 가운데 게임에 있는 것을 저장된 수량으로 차례로 준다. 비급 줄은 저장된 권수를 모두 넣었을 때 넣음이다.
        // 줄마다 예외를 막는다. 밖으로 나가면 버튼 콜백에서 사라져 결과 문장 없이 일부만 들어가고, 다시 누르면 앞의 것을 또 준다.
        private void GiveAll()
        {
            int given = 0;
            var failed = new List<string>();
            foreach (Row row in Rows(ItemCatalog.OwnedIds(Player)))
            {
                if (row.Item != null ? row.Item.Missing : row.Skill == null)
                {
                    continue;
                }
                string name = row.Item != null ? row.Item.Name : row.Skill.Name + " 비급";
                try
                {
                    string text;
                    bool ok = row.Item != null
                        ? EditService.TryGiveItem(Player, row.Item.Id, FavoriteStore.AmountOf(row.Item.Id), out text)
                        : MartialBookService.Make(Player, row.Book.BookKind, row.Skill, row.Book.Grade, row.Book.MinLevel, row.Book.Amount, out text) == row.Book.Amount;
                    if (ok)
                    {
                        given++;
                    }
                    else
                    {
                        failed.Add(name);
                    }
                }
                catch (Exception e)
                {
                    failed.Add(name + " — " + e.GetType().Name);
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
