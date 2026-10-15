using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭의 기술 서적(비급) 그룹과 즐겨찾기의 비급 줄. 설계: docs/superpowers/specs/2026-10-15-martial-books-design.md 3.2·3.3절.
    internal sealed partial class ResourcesTab
    {
        private const string ManualKey = "Manual";

        private int bookKind = -1;          // -1이면 다섯 분류 전부, 아니면 SkillCatalog.Kinds의 순번
        private int bookGrade;              // 0이면 플레이어의 지금 대경지로 정한다
        private int bookMinLevel = 1;
        private bool bookFit = true;        // 고른 경지로 만들 수 있는 기술만
        private readonly Dictionary<int, int> learned = new Dictionary<int, int>();   // 기술 번호 → 배운 수(기술 서적 그룹을 그릴 때 센다)

        private int BookGrade
        {
            get
            {
                if (bookGrade <= 0)
                {
                    bookGrade = Math.Max(1, FateService.CurrentGrade(Player));
                }
                return bookGrade;
            }
        }

        private void ClearBookFilters()
        {
            bookKind = -1;
            bookGrade = 0;
            bookMinLevel = 1;
            bookFit = true;
        }

        private void PrepareBooks(WorldUnitBase unit)
        {
            learned.Clear();
            if (groupKey != ManualKey)
            {
                return;
            }
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (var pair in SkillCatalog.LearnedCounts(unit, k))
                {
                    learned[pair.Key] = pair.Value;
                }
            }
        }

        private bool BookShown(SkillCatalog.Entry entry)
        {
            return !bookFit || SkillCatalog.BlockReason(entry, BookGrade) == null;
        }

        private List<Row> BookRows()
        {
            var rows = new List<Row>();
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                if (bookKind >= 0 && bookKind != k)
                {
                    continue;
                }
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(k, filter.Query))
                {
                    if (BookShown(entry))
                    {
                        rows.Add(new Row { Skill = entry, SkillKind = k });
                    }
                }
            }
            return rows;
        }

        // 분류마다 검색어와 "이 경지만"을 통과한 기술의 수(SkillCatalog.Kinds 순서).
        private int[] BookKindCounts()
        {
            var counts = new int[SkillCatalog.Kinds.Length];
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(k, filter.Query))
                {
                    if (BookShown(entry))
                    {
                        counts[k]++;
                    }
                }
            }
            return counts;
        }

        private int BookCount()
        {
            int total = 0;
            foreach (int n in BookKindCounts())
            {
                total += n;
            }
            return total;
        }

        // 종류 칩 여섯(전체와 다섯 분류). 한 줄에 들어간다.
        private float DrawBookKinds(float y)
        {
            int[] counts = BookKindCounts();
            int total = 0;
            foreach (int n in counts)
            {
                total += n;
            }
            float x = 0;
            for (int i = 0; i <= SkillCatalog.Kinds.Length; i++)
            {
                string name = i == 0 ? "전체" : SkillCatalog.Kinds[i - 1].Tab;
                int count = i == 0 ? total : counts[i - 1];
                float w = Math.Max(70f, Ui.TextWidth(name + " (00000)", 19) + 28f);
                int kindValue = i - 1;
                Button chip = Ui.Button("ResKind" + i, content, name + " (" + count + ")", RightX + x, y, w, ChipH,
                    new Action(() => { bookKind = kindValue; Changed(); }), true, bookKind == kindValue);
                labels.Add(new CountLabel { Text = chip.GetComponentInChildren<Text>(), Name = name, Index = i });
                x += w + ChipGap;
            }
            return y + ChipH + 6;
        }

        // 필터 두 줄: 검색·이 경지만·초기화 / 경지 1~10·최소 품질 1~6.
        private float DrawBookFilters(float y)
        {
            Ui.Label(content, "기술명", RightX, y, 76, 38, 18);
            SearchBox.Build(content, "ItemQuery", RightX + 76, y, 290, filter.Query, "이름의 일부 또는 번호", new Action<string>(OnQuery));
            Ui.Button("ResBookFit", content, "이 경지만", RightX + 376, y, 120, 38, new Action(() => { bookFit = !bookFit; Changed(); }), true, bookFit);
            Ui.Button("ResClear", content, "초기화", RightX + 502, y, 100, 38, new Action(ClearFilters));
            float y2 = y + 44;
            Ui.Label(content, "경지", RightX, y2, 50, 38, 18);
            for (int gr = 1; gr <= 10; gr++)
            {
                int value = gr;
                Ui.Button("ResGrade" + gr, content, gr.ToString(), RightX + 50 + (gr - 1) * 42, y2, 38, 38, new Action(() => { bookGrade = value; Changed(); }), true, BookGrade == gr);
            }
            Ui.Label(content, "최소 품질", RightX + 486, y2, 90, 38, 18);
            for (int q = 1; q <= 6; q++)
            {
                int value = q;
                Ui.Button("ResMinLevel" + q, content, q.ToString(), RightX + 576 + (q - 1) * 42, y2, 38, 38, new Action(() => { bookMinLevel = value; Changed(); }), true, bookMinLevel == q);
            }
            return y2 + 44;
        }

        private void DrawBookHead(float y)
        {
            Ui.Panel("ResHead", content, RightX, y, RightW, 30, Ui.Section);
            Ui.Label(content, "☆", RightX + ColStar, y, 40, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            Ui.Label(content, "기술명", RightX + ColName, y, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "분류", RightX + ColKind, y, KindW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "경지", RightX + ColLevel, y, 50, 30, 16).color = Ui.Dim;
            Ui.Label(content, "배움", RightX + ColOwned, y, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "가방", RightX + ColOwned + 70, y, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "지급", RightX + ColGive, y, GiveW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
        }

        private void DrawSkillRow(RectTransform row, Row r, float h)
        {
            SkillCatalog.Entry skill = r.Skill;
            int id = skill.BaseId;
            int kindIndex = r.SkillKind;
            bool fav = FavoriteStore.HasBook(id, BookGrade, bookMinLevel);
            Ui.Button("BookStar" + id, row, fav ? "★" : "☆", ColStar, 2, 40, RowH - 4, new Action(() => ToggleBook(kindIndex, skill)), true, fav);
            WrapLabel(Ui.Button("BookName" + id, row, skill.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(SkillDetail(r)))));
            Ui.Wrapped(row, SkillCatalog.Kinds[kindIndex].Tab, ColKind, 0, KindW, h, 16);
            Ui.Label(row, skill.GradeMin + "~" + skill.GradeMax, ColLevel, 0, 50, h, 16);
            Ui.Label(row, learned.ContainsKey(id) ? "예" : "-", ColOwned, 0, 60, h, 16);
            Ui.Label(row, BagBooks(id).ToString(), ColOwned + 70, 0, 60, h, 16).gameObject.name = "BookBag" + id;
            int amount = Math.Min(AmountBar.Get("item"), MartialBookService.MaxPerGive);
            Ui.Button("BookGive" + id, row, "지급 x" + amount, ColGive, 2, GiveW, RowH - 4,
                new Action(() => GiveBook(kindIndex, skill, BookGrade, bookMinLevel, AmountBar.Get("item"))), SkillCatalog.BlockReason(skill, BookGrade) == null);
        }

        private void DrawBookFavRow(RectTransform row, Row r, float h)
        {
            FavoriteStore.Favorite f = r.Book;
            int baseId = f.Id, grade = f.Grade, minLevel = f.MinLevel, kindIndex = f.BookKind;
            SkillCatalog.Entry skill = r.Skill;
            string tail = baseId + "_" + grade + "_" + minLevel;
            Ui.Button("FavBookStar" + tail, row, "★", ColStar, 2, 40, RowH - 4, new Action(() =>
            {
                string text;
                FavoriteStore.RemoveBook(baseId, grade, minLevel, out text);
                Say(text);
            }), true, true);
            WrapLabel(Ui.Button("FavBookName" + tail, row, BookFavName(r), ColName, 2, NameW, h - 4, new Action(() => detail.Show(BookFavDetail(r)))));
            Ui.Wrapped(row, "비급·" + SkillCatalog.Kinds[kindIndex].Tab, ColKind, 0, KindW, h, 16);
            Ui.Label(row, minLevel + "+", ColLevel, 0, 44, h, 16);
            Ui.Label(row, skill == null ? "-" : BagBooks(baseId).ToString(), ColOwned, 0, 60, h, 16);
            NumberField.Build(row, "FavBookAmount" + tail, ColAmount, (h - (RowH - 4)) / 2, AmountW, RowH - 4, f.Amount.ToString(), null, new Action<int>(v =>
            {
                string text;
                FavoriteStore.SetBookAmount(baseId, grade, minLevel, v, out text);
                Say(text);
            }));
            Ui.Button("FavBookGive" + tail, row, "지급 x" + f.Amount, ColGive, 2, GiveW, RowH - 4,
                new Action(() => GiveBook(kindIndex, skill, grade, minLevel, FavoriteStore.BookAmountOf(baseId, grade, minLevel))), skill != null);
        }

        private string BookFavName(Row r)
        {
            return FavoriteStore.BookLabel(r.Book.BookKind, r.Book.Id, r.Book.Grade, r.Book.MinLevel);
        }

        private string SkillDetail(Row r)
        {
            SkillCatalog.Entry skill = r.Skill;
            return skill.Name + "  [" + skill.BaseId + "]  " + SkillCatalog.Kinds[r.SkillKind].Tab + ", 경지 " + skill.GradeMin + "~" + skill.GradeMax + "\n"
                + MartialBookService.Describe(skill, BookGrade, bookMinLevel, AmountBar.Get("item")) + "\n" + skill.Desc;
        }

        private string BookFavDetail(Row r)
        {
            if (r.Skill == null)
            {
                return "[" + r.Book.Id + "] " + BookFavName(r) + "\n즐겨찾기에 남아 있지만 지금 게임의 기술 표에 없습니다(그 기술을 넣은 모드를 뺐을 수 있습니다). ★를 누르면 뺍니다.";
            }
            return BookFavName(r) + "  [" + r.Skill.BaseId + "]  " + SkillCatalog.Kinds[r.SkillKind].Tab + "\n"
                + MartialBookService.Describe(r.Skill, r.Book.Grade, r.Book.MinLevel, r.Book.Amount) + "\n" + r.Skill.Desc;
        }

        private void ToggleBook(int kindIndex, SkillCatalog.Entry skill)
        {
            string text;
            if (FavoriteStore.HasBook(skill.BaseId, BookGrade, bookMinLevel))
            {
                FavoriteStore.RemoveBook(skill.BaseId, BookGrade, bookMinLevel, out text);
            }
            else
            {
                FavoriteStore.AddBook(kindIndex, skill.BaseId, BookGrade, bookMinLevel, Math.Min(AmountBar.Get("item"), MartialBookService.MaxPerGive), out text);
            }
            Say(text);
        }

        private void GiveBook(int kindIndex, SkillCatalog.Entry skill, int grade, int minLevel, int amount)
        {
            if (skill == null)
            {
                return;
            }
            string text;
            MartialBookService.Make(Player, kindIndex, skill, grade, minLevel, amount, out text);
            Say(text);
        }
    }
}
