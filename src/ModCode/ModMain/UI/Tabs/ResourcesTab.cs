using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭: 지급 수량과 가방 칸, 용도 그룹으로 나눈 아이템 표, 기술 서적(비급) 지급, 화폐 그룹(영석·재화·명성), 즐겨찾기.
    // 설계: docs/superpowers/specs/2026-10-15-resources-tab-design.md, 2026-10-15-martial-books-design.md.
    // 판단은 ItemCatalog·FavoriteStore·MartialBookService가 하고 여기서는 그린다. 기술 서적 그룹은 ResourcesTab.Books.cs.
    internal sealed partial class ResourcesTab : EditorTab
    {
        private const string CurrencyKey = "Currency";  // 화폐 그룹: 영석과 재화(ItemCatalog.Currencies), 명성 줄. 05 문서 47절
        // 보유 칸의 줄이기 확인(05 문서 89절): 어느 아이템의 몇 개로 줄이려는지. 확인 키는 "res-set-<번호>"(Confirm이 창 전체에 하나만 둔다).
        private int pendingOwnedId;
        private int pendingOwnedValue;
        private const float RowH = 32;
        private const float DetailH = 150;              // 표에 설명 열이 없어 설명은 이 칸에서 본다. 넘치면 칸 안에서 스크롤한다
        private const float TopH = 48;                  // 지급 수량과 가방 칸 한 줄(영석 줄은 화폐 그룹으로 갔다. 05 문서 47절)
        private const float LeftW = 170;                // 그룹 목록
        private const float GroupStep = 42;             // 그룹 버튼(높이 38)의 간격. 열다섯 개가 아래 설명 칸 위에 들어간다(56 + 14·42 + 38 = 682 < 686)
        private const float RightX = 180;
        private const float RightW = ContentW - RightX; // 1124
        private const float ChipH = 34, ChipGap = 6;
        private const int ChipRowsShown = 3;
        // 표의 열(목록 안의 x). 설명은 표에 두지 않는다(줄이 높아져 한 화면에 몇 줄 안 보였다. 사용자 지시 2026-10-15).
        // 지급 열은 목록 안쪽 폭(RightW - 스크롤 막대)의 오른쪽 끝에 붙인다.
        private const float ColStar = 4, ColName = 50, ColKind = 436, ColLevel = 602, ColOwned = 652;
        private const float NameW = 380, KindW = 160, GiveW = 116, AmountW = 90, OwnedW = 70, DropW = 90;
        private const float ColDrop = ColOwned + OwnedW + 6;                    // 728
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

        // 수가 적힌 글(CountChip)에 수를 써넣는다: 그룹 단추는 "이름 수", 종류 칩은 "이름 (수)".
        private static void SetCount(CountChip label, int count)
        {
            label.Set(label.Key != null ? label.Name + " " + CountText(count) : label.Name + " (" + CountText(count) + ")");
        }

        // 수가 음수(-1)면 아직 세지 않음: "…"(기술 서적 그룹을 열기 전, 성능 1 S3).
        private static string CountText(int count)
        {
            return count < 0 ? "…" : count.ToString();
        }

        private readonly ListView<Row> list = new ListView<Row>("ItemTable", RowH);
        private readonly DetailPane detail = new DetailPane("ItemDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");
        private readonly ItemCatalog.Filter filter = new ItemCatalog.Filter();
        private readonly List<CountChip> labels = new List<CountChip>();
        private string groupKey = ItemGroups.AllKey;
        private static readonly Dictionary<string, int[]> widestByGroup = new Dictionary<string, int[]>();   // 칩 폭용 거르기 전 종류 수
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

        private bool IsCurrency
        {
            get { return groupKey == CurrencyKey; }
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

        private int BagBooks(int baseId)
        {
            int n;
            return bagBooks.TryGetValue(baseId, out n) ? n : 0;
        }

        // 지금 보일 줄. 즐겨찾기 보기는 넣은 순서, 기술 서적은 분류·기술 표 순서, 나머지는 그룹·종류·게임 표 순서.
        private List<Row> Rows(HashSet<int> owned)
        {
            if (groupKey == ManualKey)
            {
                return BookRows();
            }
            var rows = new List<Row>();
            if (IsCurrency)
            {
                // 화폐 그룹은 거르기(검색·품질·보유 중만)를 쓰지 않는다. 다른 그룹에서 남긴 검색어가 여기 줄을 숨기지 않게.
                foreach (ItemCatalog.Entry entry in ItemCatalog.Currencies)
                {
                    rows.Add(new Row { Item = entry });
                }
                return rows;
            }
            if (!IsFav)
            {
                filter.Group = groupKey;
                foreach (ItemCatalog.Entry entry in ItemCatalog.Select(filter, owned))
                {
                    rows.Add(new Row { Item = entry });
                }
                return rows;
            }
            return FavRows(owned);
        }

        private static string KindOf(Row row)
        {
            if (row.Item != null)
            {
                return row.Item.Kind == null ? (row.Item.Group == null ? "화폐" : "-") : row.Item.Kind.Name;
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
            bookKindCountsCache = null;
            favRowsNow = FavRows(owned);
            DrawTop(player);
            DrawGroups(owned);
            float y = TopH + 8;
            y = DrawKinds(y, owned);
            y = groupKey == ManualKey ? DrawBookFilters(y) : IsCurrency ? y : DrawFilters(y);
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
            list.Build(content, RightX, y, RightW, ContentH - DetailH - 8 - y, IsFav ? favRowsNow : Rows(owned), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
            favRowsNow = null;
        }

        private void DrawTop(WorldUnitBase player)
        {
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, TopH, Ui.Section);
            // 아이템 지급 수량과 가방 칸. 가방 칸은 편집기가 늘린 만큼만 되돌릴 수 있다. 영석은 화폐 그룹의 첫 줄이다.
            Ui.Label(content, "지급 수량", 12, 4, 110, 40, 18);
            float itemRight = AmountBar.Build(content, "item", 122, 5);
            int bagExtra = BagService.Extra;
            bagInput = bagExtra.ToString();
            float bagX = itemRight + 16;
            Ui.Label(content, BagService.Describe(), bagX, 4, 296, 40, 17);
            float bx = bagX + 300;
            NumberField.Build(content, "BagExtra", bx, 5, 76, 38, bagInput, new Action<string>(v => bagInput = v), new Action<int>(SetBagExtra));
            Ui.Button("BagApply", content, "적용", bx + 80, 5, 60, 38, new Action(() =>
            {
                int value;
                if (!int.TryParse(bagInput, out value))
                {
                    Say("가방: 추가 칸을 숫자로 입력하세요.");
                    return;
                }
                SetBagExtra(value);
            }));
            Ui.Button("BagMinus", content, "-" + BagService.Step, bx + 144, 5, 60, 38, new Action(() => Say(BagService.Change(-BagService.Step))), bagExtra > 0);
            Ui.Button("BagPlus", content, "+" + BagService.Step, bx + 208, 5, 60, 38, new Action(() => Say(BagService.Change(BagService.Step))), bagExtra < BagService.MaxExtra);
        }

        private void DrawGroups(HashSet<int> owned)
        {
            Dictionary<string, int> counts;
            int[] unusedKinds;
            ItemCatalog.Counts(filter, owned, null, out counts, out unusedKinds);
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
            GroupButton(CurrencyKey, "화폐", ItemCatalog.Currencies.Count, y);
        }

        private void GroupButton(string key, string name, int count, float y)
        {
            Button button = Ui.Button("ResGroup" + key, content, name + " " + CountText(count), 0, y, LeftW, GroupStep - 4, new Action(() => SetGroup(key)), true, groupKey == key);
            labels.Add(new CountChip { Text = button.GetComponentInChildren<Text>(), Name = name, Key = key });
        }

        // 종류 칩(그룹 보기), 모두 지급(즐겨찾기 보기) 또는 안내(전체 보기). 다음 줄의 y를 돌려준다.
        private float DrawKinds(float y, HashSet<int> owned)
        {
            if (IsCurrency)
            {
                return DrawFameLine(y);
            }
            if (groupKey == ManualKey)
            {
                return DrawBookKinds(y);
            }
            if (IsFav)
            {
                Confirm.Button("ResAllGive", content, "모두 지급", "정말 모두 지급", RightX, y, 170, 38, "res-all-give",
                    "보이는 즐겨찾기를 저장된 수량으로 모두 지급합니다. 한 번 더 누르면 실행합니다.", new Action(GiveAll));
                Ui.Label(content, "수량 칸에 쓰고 Enter를 누르거나 다른 곳을 누르면 저장됩니다. ☆로 넣고 ★로 뺍니다.", RightX + 182, y, RightW - 182, 38, 16).color = Ui.Dim;
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
            Dictionary<string, int> unusedGroups;
            int[] counts;
            ItemCatalog.Counts(filter, owned, group, out unusedGroups, out counts);
            // 칩의 폭은 거르기 전의 수로 잰다. 거르기가 비어 보유·가방과 무관하므로 그룹마다 한 번만 센다(묶음 3)
            int[] widest;
            if (!widestByGroup.TryGetValue(group.Key, out widest))
            {
                ItemCatalog.Counts(new ItemCatalog.Filter(), owned, group, out unusedGroups, out widest);
                widestByGroup[group.Key] = widest;
            }
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
            for (int i = 0; i < names.Count; i++)
            {
                ws[i] = Math.Max(70f, Ui.TextWidth(names[i] + " (" + sized[i] + ")", 19) + 28f);
            }
            float allH = ChipBar.Layout(ws, areaW, ChipH, ChipGap, xs, ys);
            float shownH = Math.Min(allH, ChipRowsShown * (ChipH + ChipGap) - ChipGap);
            Transform area = allH > shownH
                ? Ui.Scroll("ResKinds", content, RightX, y, RightW, shownH, allH, chipScrollY, new Action<float>(v => chipScrollY = v))
                : Ui.Rect("ResKinds", content, RightX, y, RightW, shownH);
            for (int i = 0; i < names.Count; i++)
            {
                ItemGroups.Kind kind = i == 0 ? null : group.Kinds[i - 1];
                Button chip = Ui.Button("ResKind" + i, area, names[i] + " (" + shown[i] + ")", xs[i], ys[i], ws[i], ChipH,
                    new Action(() => { filter.Kind = kind; Changed(); }), true, filter.Kind == kind);
                labels.Add(new CountChip { Text = chip.GetComponentInChildren<Text>(), Name = names[i], Index = i });
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
            if (!IsCurrency)
            {
                Ui.Label(content, "☆", RightX + ColStar, y, 40, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            }
            Ui.Label(content, IsCurrency ? "화폐" : "아이템명", RightX + ColName, y, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "종류", RightX + ColKind, y, KindW, 30, 16).color = Ui.Dim;
            if (!IsCurrency)
            {
                Ui.Label(content, "품질", RightX + ColLevel, y, 44, 30, 16).color = Ui.Dim;
            }
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
            bookKindCountsCache = null;
            favRowsNow = FavRows(owned);
            list.SetItems(IsFav ? favRowsNow : Rows(owned));
            // 그룹 수와 지금 그룹의 종류 수를 한 번 훑어 함께 센다(묶음 3)
            bool itemGroup = !IsFav && groupKey != ItemGroups.AllKey && groupKey != ManualKey && groupKey != CurrencyKey;
            Dictionary<string, int> counts;
            int[] itemKinds;
            ItemCatalog.Counts(filter, owned, itemGroup ? ItemGroups.ByKey(groupKey) : null, out counts, out itemKinds);
            int[] kinds = IsFav || groupKey == ItemGroups.AllKey ? null
                : groupKey == ManualKey ? BookKindCounts()
                : itemKinds;
            int favCount = FavCount(owned);
            int bookCount = BookCount();
            foreach (CountChip label in labels)
            {
                if (label.Key == ItemGroups.FavKey)
                {
                    SetCount(label, favCount);
                }
                else if (label.Key == ManualKey)
                {
                    SetCount(label, bookCount);
                }
                else if (label.Key == CurrencyKey)
                {
                    SetCount(label, ItemCatalog.Currencies.Count);
                }
                else if (label.Key != null)
                {
                    SetCount(label, counts[label.Key]);
                }
                else if (kinds != null)
                {
                    // 기술 서적 그룹의 kinds는 다섯 분류 뒤에 "모드만"이 하나 더 있다. 전체 수에는 넣지 않는다(분류 수에 이미 들어 있다).
                    int kindCount = groupKey == ManualKey ? SkillCatalog.Kinds.Length : kinds.Length;
                    int sum = 0;
                    for (int i = 0; i < kindCount; i++)
                    {
                        sum += kinds[i];
                    }
                    SetCount(label, label.Index == 0 ? sum : kinds[label.Index - 1]);
                }
            }
            favRowsNow = null;
        }

        private void DrawRow(RectTransform row, Row r, int index)
        {
            float h = row.sizeDelta.y;
            if (r.Item != null)
            {
                if (IsCurrency)
                {
                    DrawCurrencyRow(row, r.Item, h);
                }
                else
                {
                    DrawItemRow(row, r.Item, h);
                }
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
            if (entry.Missing)
            {
                Ui.Label(row, "-", ColOwned, 0, 60, h, 16);
            }
            else
            {
                // 보유 수량 지정·버리기(스펙 2026-10-20-trainer-batch1-design.md 5.3). 늘리기는 칸을 떠날 때도 적용한다.
                // 줄이기는 Enter로만 묻고 같은 값의 Enter로 확인한다(사용자 결정 2026-10-30, 05 문서 89절). 판정은 NumberFieldRules.DecideOwned.
                int held = Player.data.unitData.propData.GetPropsNum(id);
                string setKey = "res-set-" + id;
                bool setPending = Confirm.IsPending(setKey) && pendingOwnedId == id;
                InputField owned = null;
                owned = NumberField.Build(row, "ResOwned" + id, ColOwned, (h - (RowH - 4)) / 2, OwnedW, RowH - 4,
                    (setPending ? pendingOwnedValue : held).ToString(), null, new Func<int, bool, bool>((v, enter) =>
                    {
                        int now = Player.data.unitData.propData.GetPropsNum(id);
                        bool pending = Confirm.IsPending(setKey) && pendingOwnedId == id;
                        switch (NumberFieldRules.DecideOwned(v, now, enter, pending, pendingOwnedValue))
                        {
                            case NumberFieldRules.OwnedAction.Set:
                                Say(BagService.SetCount(Player, id, v));
                                return true;
                            case NumberFieldRules.OwnedAction.Ask:
                                pendingOwnedId = id;
                                pendingOwnedValue = v;
                                Confirm.Ask(setKey, BagService.LowerQuestion(Player, id, v));
                                return true;
                            case NumberFieldRules.OwnedAction.Confirm:
                                Confirm.Clear();
                                Say(BagService.SetCount(Player, id, v));
                                return true;
                            default:
                                // 같은 값이거나 떠나면서 줄이기: 아무것도 하지 않고 칸을 지금 수로 되돌린다. 받아들이지 않았으므로 거짓.
                                if (owned != null && owned.text != now.ToString())
                                {
                                    owned.text = now.ToString();
                                }
                                return false;
                        }
                    }), true);
                // 확인 문구는 누르는 순간의 가방으로 만든다(그린 뒤 가방이 바뀔 수 있다).
                string dropKey = "res-drop-" + id;
                bool waiting = Confirm.IsPending(dropKey);
                Ui.Button("ResDrop" + id, row, waiting ? "정말 버리기" : "버리기", ColDrop, 2, DropW, RowH - 4, new Action(() =>
                {
                    if (!Confirm.IsPending(dropKey))
                    {
                        Confirm.Ask(dropKey, BagService.DropQuestion(Player, id));
                        return;
                    }
                    Confirm.Clear();
                    Say(BagService.Drop(Player, id));
                }), held > 0, waiting);
            }
            if (IsFav)
            {
                int saved = FavoriteStore.AmountOf(id);
                // 수량은 Enter뿐 아니라 칸을 떠날 때도 저장한다(사용자 보고: 고친 수량이 남지 않았다. 05 문서 55절). 저장되면 지급 버튼의 수량 글도 바꾼다.
                Button give = null;
                NumberField.Build(row, "ResAmount" + id, ColAmount, (h - (RowH - 4)) / 2, AmountW, RowH - 4, saved.ToString(), null,
                    new Action<int>(v =>
                    {
                        if (SetAmount(id, v) && give != null)
                        {
                            give.GetComponentInChildren<Text>().text = "지급 x" + FavoriteStore.AmountOf(id);
                        }
                    }), true);
                give = Ui.Button("ItemGive" + id, row, "지급 x" + saved, ColGive, 2, GiveW, RowH - 4,
                    new Action(() => Say(EditService.GiveItem(Player, id, FavoriteStore.AmountOf(id)))), !entry.Missing);
            }
            else
            {
                int amount = AmountBar.Get("item");
                Ui.Button("ItemGive" + id, row, "지급 x" + amount, ColGive, 2, GiveW, RowH - 4, new Action(() => Say(EditService.GiveItem(Player, id, amount))));
            }
        }

        private static string Detail(ItemCatalog.Entry entry)
        {
            if (entry.Missing)
            {
                return "[" + entry.Id + "] " + entry.Name + "\n" + entry.Desc;
            }
            if (entry.Group == null)
            {
                // 화폐 목록의 항목은 용도 그룹이 없다.
                return "[" + entry.Id + "] " + entry.Name + "  (화폐, 품질 " + entry.Level + ")\n" + entry.Desc;
            }
            return "[" + entry.Id + "] " + entry.Name + "  (" + entry.Group.Name + " / " + entry.Kind.Name + ", 품질 " + entry.Level + ")\n" + entry.Desc;
        }
    }
}
