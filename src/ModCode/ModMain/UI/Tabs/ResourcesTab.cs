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

        // 검색어가 바뀌면 탭을 다시 그리지 않고(검색 칸의 포커스를 지킨다) 이 글들만 고친다.
        private sealed class CountLabel
        {
            public Text Text;
            public string Name;
            public string Key;      // 그룹 버튼이면 그룹 키, 종류 칩이면 null
            public int Index;       // 종류 칩의 순번(0이 전체)
            public void Set(int count)
            {
                Text.text = Key != null ? Name + " " + CountText(count) : Name + " (" + CountText(count) + ")";
            }
        }

        // 수가 음수(-1)면 아직 세지 않음: "…"(기술 서적 그룹을 열기 전, 성능 1 S3).
        private static string CountText(int count)
        {
            return count < 0 ? "…" : count.ToString();
        }

        private readonly ListView<Row> list = new ListView<Row>("ItemTable", RowH);
        private readonly DetailPane detail = new DetailPane("ItemDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");
        private readonly ItemCatalog.Filter filter = new ItemCatalog.Filter();
        private readonly List<CountLabel> labels = new List<CountLabel>();
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
            list.Build(content, RightX, y, RightW, ContentH - DetailH - 8 - y, Rows(owned), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
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
            GroupButton(CurrencyKey, "화폐", ItemCatalog.Currencies.Count, y);
        }

        private void GroupButton(string key, string name, int count, float y)
        {
            Button button = Ui.Button("ResGroup" + key, content, name + " " + CountText(count), 0, y, LeftW, GroupStep - 4, new Action(() => SetGroup(key)), true, groupKey == key);
            labels.Add(new CountLabel { Text = button.GetComponentInChildren<Text>(), Name = name, Key = key });
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
            // 칩의 폭은 거르기 전의 수로 잰다. 거르기가 비어 보유·가방과 무관하므로 그룹마다 한 번만 센다(묶음 3)
            int[] widest;
            if (!widestByGroup.TryGetValue(group.Key, out widest))
            {
                widest = ItemCatalog.CountByKind(group, new ItemCatalog.Filter(), owned);
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

        // 화폐 그룹의 명성 줄. 명성은 아이템이 아니라 인물의 속성(저장값 reputation)이다. 낭야각 같은 상점의 "명성 N 도달 필요"에 쓰이는 값과 같아
        // (실측: 화면의 80 = 저장값 80) 재화와 함께 올릴 수 있게 여기 둔다. 쓰기는 능력치 탭과 같은 경로다.
        private float DrawFameLine(float y)
        {
            AttributeDef fame = AttributeCatalog.All.Find(d => d.Label == "명성");
            if (fame == null)
            {
                return y;
            }
            int amount = AmountBar.Get("item");
            int have = fame.Get(Player.data.unitData.propertyData);
            Ui.Label(content, "속성 명성", RightX, y, 110, 38, 18);
            Ui.Label(content, "보유", RightX + 110, y, 44, 38, 16).color = Ui.Dim;
            Ui.Label(content, have.ToString(), RightX + 154, y, 100, 38, 18).gameObject.name = "CurrencyOwnedFame";
            Ui.Button("CurrencyAddFame", content, "+ " + amount, RightX + 260, y + 2, 120, 34, new Action(() =>
                Say(EditService.SetAttribute(Player, fame, fame.Get(Player.data.unitData.propertyData) + amount))));
            // 안내는 두 줄로 줄바꿈한다(한 줄에는 들어가지 않는다. 회차 ws-1017f의 캡처).
            Ui.Wrapped(content, "인물의 속성입니다(능력치 탭 > 개인 속성의 명성과 같은 값). 상점의 \"명성 N 도달 필요\" 조건에 쓰입니다. 아래 재화는 가방 칸을 차지하지 않습니다.",
                RightX + 392, y, RightW - 392, 40, 14, TextAnchor.UpperLeft).color = Ui.Dim;
            return y + 44;
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
            list.SetItems(Rows(owned));
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
                else if (label.Key == CurrencyKey)
                {
                    label.Set(ItemCatalog.Currencies.Count);
                }
                else if (label.Key != null)
                {
                    label.Set(counts[label.Key]);
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
                    label.Set(label.Index == 0 ? sum : kinds[label.Index - 1]);
                }
            }
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
                    (setPending ? pendingOwnedValue : held).ToString(), null, new Action<int, bool>((v, enter) =>
                    {
                        int now = Player.data.unitData.propData.GetPropsNum(id);
                        bool pending = Confirm.IsPending(setKey) && pendingOwnedId == id;
                        switch (NumberFieldRules.DecideOwned(v, now, enter, pending, pendingOwnedValue))
                        {
                            case NumberFieldRules.OwnedAction.Set:
                                Say(BagService.SetCount(Player, id, v));
                                return;
                            case NumberFieldRules.OwnedAction.Ask:
                                pendingOwnedId = id;
                                pendingOwnedValue = v;
                                Confirm.Ask(setKey, BagService.LowerQuestion(Player, id, v));
                                return;
                            case NumberFieldRules.OwnedAction.Confirm:
                                Confirm.Clear();
                                Say(BagService.SetCount(Player, id, v));
                                return;
                            default:
                                // 같은 값이거나 떠나면서 줄이기: 아무것도 하지 않고 칸을 지금 수로 되돌린다.
                                if (owned != null && owned.text != now.ToString())
                                {
                                    owned.text = now.ToString();
                                }
                                return;
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

        // 화폐 줄: ☆ 없음(즐겨찾기는 가방 아이템의 것), 종류 "화폐", 품질 없음, 보유는 영석이면 EditService.Money. 지급은 예전 화폐 탭의 Add와 같다.
        private void DrawCurrencyRow(RectTransform row, ItemCatalog.Entry entry, float h)
        {
            int id = entry.Id;
            WrapLabel(Ui.Button("ItemName" + id, row, entry.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(Detail(entry)))));
            Ui.Wrapped(row, "화폐", ColKind, 0, KindW, h, 16);
            Ui.Label(row, "-", ColLevel, 0, 44, h, 16).color = Ui.Dim;
            int have = id == PropsIDType.Money ? EditService.Money(Player) : Player.data.unitData.propData.GetPropsNum(id);
            Ui.Label(row, have.ToString(), ColOwned, 0, 60, h, 16).gameObject.name = "CurrencyOwned" + id;
            int amount = AmountBar.Get("item");
            Ui.Button("CurrencyAdd" + id, row, "+ " + amount, ColGive, 2, GiveW, RowH - 4, new Action(() => Say(AddCurrency(entry, amount))));
        }

        private static string AddCurrency(ItemCatalog.Entry entry, int amount)
        {
            if (entry.Id == PropsIDType.Money)
            {
                return EditService.AddMoney(Player, amount);
            }
            string result = EditService.GiveItem(Player, entry.Id, amount);
            // 이름의 번역이 없는 재화는 결과 문장에 번역 열쇠가 나온다. 목록에 보인 이름으로 바꾼다.
            string raw = EditService.ItemName(entry.Id);
            return string.IsNullOrEmpty(raw) || raw == entry.Name ? result : result.Replace(raw, entry.Name);
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

        // 저장했으면 참. 같은 수량이면 아무것도 하지 않는다(칸을 떠날 때마다 오므로 결과 칸을 채우지 않는다).
        private bool SetAmount(int id, int amount)
        {
            if (amount == FavoriteStore.AmountOf(id))
            {
                return false;
            }
            string text;
            bool ok = FavoriteStore.SetAmount(id, amount, out text);
            Say(text);
            return ok;
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
            if (entry.Group == null)
            {
                // 화폐 목록의 항목은 용도 그룹이 없다.
                return "[" + entry.Id + "] " + entry.Name + "  (화폐, 품질 " + entry.Level + ")\n" + entry.Desc;
            }
            return "[" + entry.Id + "] " + entry.Name + "  (" + entry.Group.Name + " / " + entry.Kind.Name + ", 품질 " + entry.Level + ")\n" + entry.Desc;
        }
    }
}
