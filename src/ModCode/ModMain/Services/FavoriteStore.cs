using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 자원 탭의 즐겨찾기: 아이템 번호와 지급 수량, 또는 기술 서적(비급)의 분류·기술·경지·최소 품질과 권수. 모든 세이브에 공통이고 TlToybox\favorites.txt에 둔다.
    // 쓰기는 임시 파일에 쓴 뒤 바꿔치기한다. 깨진 줄이 있던 파일은 처음 덮어쓰기 전에 .bak으로 남긴다.
    // 읽지 못한 파일은 덮어쓰지 않는다. 근거: docs/superpowers/specs/2026-10-15-resources-tab-design.md 3.2·3.3절, 6절,
    // 비급 줄은 2026-10-15-martial-books-design.md 3.3절.
    internal static partial class FavoriteStore
    {
        public const int MaxAmount = AmountBar.Max;
        private const string FileName = "favorites.txt";
        private static readonly string AmountRule = "즐겨찾기: 수량은 1~" + MaxAmount + " 사이의 숫자로 입력하세요.";
        private static readonly string BookAmountRule = "즐겨찾기: 비급 수량은 1~" + MartialBookService.MaxPerGive + " 사이의 숫자로 입력하세요.";

        private static List<Favorite> list;     // null이면 아직 읽지 않았다
        private static string readError;        // 파일을 읽지 못한 이유. 있으면 덮어쓰지 않는다
        private static bool keepOriginal;       // 깨진 줄이 있었다: 처음 덮어쓰기 전에 .bak으로 남긴다
        private static bool noticePending;      // 자원 탭에 들어올 때 한 번 알릴 것이 있다

        public static int SkippedLines { get; private set; }

        // 게임 쪽 한도(기술 분류 수, 수량 상한, 비급 권수 상한). 줄 해석은 FavoriteStore.Lines.cs.
        private static Limits CurrentLimits
        {
            get { return new Limits { Kinds = SkillCatalog.Kinds.Length, MaxAmount = MaxAmount, MaxBook = MartialBookService.MaxPerGive }; }
        }

        private static string FilePath
        {
            get { return Path.Combine(ChangeLog.DataDir, FileName); }
        }

        private static void Load()
        {
            if (list != null)
            {
                return;
            }
            list = new List<Favorite>();
            readError = null;
            keepOriginal = false;
            SkippedLines = 0;
            try
            {
                if (File.Exists(FilePath))
                {
                    int skipped;
                    list = ReadLines(File.ReadAllLines(FilePath, Encoding.UTF8), CurrentLimits, out skipped);
                    SkippedLines = skipped;
                }
                keepOriginal = SkippedLines > 0;
                if (SkippedLines > 0)
                {
                    MelonLogger.Msg("[TlToybox] favorites: skipped " + SkippedLines + " lines in " + FileName);
                }
            }
            catch (Exception e)
            {
                list.Clear();
                SkippedLines = 0;
                readError = e.Message;
                MelonLogger.Msg("[TlToybox] favorites: could not read " + FileName + ": " + e.Message);
            }
            noticePending = SkippedLines > 0 || readError != null;
        }

        // 넣은 순서의 즐겨찾기(아이템 줄과 비급 줄이 섞여 있다). 고치지 말고 읽기만 한다.
        public static List<Favorite> Entries
        {
            get
            {
                Load();
                return list;
            }
        }

        public static bool Has(int id)
        {
            return Find(id) != null;
        }

        public static int AmountOf(int id)
        {
            Favorite favorite = Find(id);
            return favorite == null ? 0 : favorite.Amount;
        }

        private static Favorite Find(int id)
        {
            Load();
            return list.Find(f => !f.Book && f.Id == id);
        }

        public static bool HasBook(int baseId, int grade, int minLevel)
        {
            return FindBook(baseId, grade, minLevel) != null;
        }

        public static int BookAmountOf(int baseId, int grade, int minLevel)
        {
            Favorite favorite = FindBook(baseId, grade, minLevel);
            return favorite == null ? 0 : favorite.Amount;
        }

        private static Favorite FindBook(int baseId, int grade, int minLevel)
        {
            Load();
            return list.Find(f => f.Book && f.Id == baseId && f.Grade == grade && f.MinLevel == minLevel);
        }

        // 건너뛴 줄이나 읽기 오류가 있었으면 한 번만 안내 문장을 돌려준다(자원 탭에 들어올 때).
        public static string TakeNotice()
        {
            Load();
            if (!noticePending)
            {
                return null;
            }
            noticePending = false;
            return readError != null ? "즐겨찾기 파일을 읽지 못했습니다(" + readError + ")."
                : "즐겨찾기 파일에서 읽지 못한 줄 " + SkippedLines + "개를 건너뛰었습니다.";
        }

        // 개발 명령(rfav reset/reload)이 부른다: 다음에 쓸 때 파일을 다시 읽는다.
        internal static void Forget()
        {
            list = null;
            readError = null;
            keepOriginal = false;
            noticePending = false;
            SkippedLines = 0;
        }

        // 읽기 오류가 있었으면 바꾸기 전에 다시 읽는다. 잠금이 풀렸는데 빈 목록으로 판단하면 같은 번호가 두 줄이 된다(검토에서 나온 것).
        private static void Recover()
        {
            Load();
            if (readError != null)
            {
                list = null;
                Load();
            }
        }

        private static string Label(int id)
        {
            ItemCatalog.Entry entry = ItemCatalog.Get(id);
            return entry != null ? entry.Name : "게임에 없는 아이템 (" + id + ")";
        }

        public static string BookLabel(int kindIndex, int baseId, int grade, int minLevel)
        {
            SkillCatalog.Entry entry = SkillCatalog.Get(kindIndex, baseId);
            return (entry != null ? entry.Name + " 비급" : "게임에 없는 기술 (" + baseId + ")") + " (경지 " + grade + ", 품질 " + minLevel + " 이상)";
        }

        public static bool Add(int id, int amount, out string text)
        {
            if (amount < 1 || amount > MaxAmount)
            {
                text = AmountRule;
                return false;
            }
            Recover();
            if (Has(id))
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 이미 있습니다.";
                return false;
            }
            string error;
            if (!Change(next =>
            {
                if (!next.Exists(f => !f.Book && f.Id == id))
                {
                    next.Add(new Favorite { Id = id, Amount = amount });
                }
            }, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에 넣었습니다: " + Label(id) + " x" + amount;
            return true;
        }

        public static bool Remove(int id, out string text)
        {
            Recover();
            if (!Has(id))
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 즐겨찾기에 없습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.RemoveAll(f => !f.Book && f.Id == id), out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에서 뺐습니다: " + Label(id);
            return true;
        }

        public static bool SetAmount(int id, int amount, out string text)
        {
            if (amount < 1 || amount > MaxAmount)
            {
                text = AmountRule;
                return false;
            }
            Recover();
            int old = AmountOf(id);
            if (old == 0)
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 즐겨찾기에 없습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.Find(f => !f.Book && f.Id == id).Amount = amount, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기 수량: " + Label(id) + " " + old + " -> " + amount;
            return true;
        }

        public static bool AddBook(int kindIndex, int baseId, int grade, int minLevel, int amount, out string text)
        {
            string label = BookLabel(kindIndex, baseId, grade, minLevel);
            if (amount < 1 || amount > MartialBookService.MaxPerGive)
            {
                text = BookAmountRule;
                return false;
            }
            if (kindIndex < 0 || kindIndex >= SkillCatalog.Kinds.Length || grade < 1 || grade > 10 || minLevel < 1 || minLevel > 6)
            {
                text = "즐겨찾기: " + label + "의 분류·경지·최소 품질이 범위 밖입니다.";
                return false;
            }
            Recover();
            if (HasBook(baseId, grade, minLevel))
            {
                text = "즐겨찾기: " + label + "은(는) 이미 있습니다.";
                return false;
            }
            string error;
            if (!Change(next =>
            {
                if (!next.Exists(f => f.Book && f.Id == baseId && f.Grade == grade && f.MinLevel == minLevel))
                {
                    next.Add(new Favorite { Book = true, BookKind = kindIndex, Id = baseId, Grade = grade, MinLevel = minLevel, Amount = amount });
                }
            }, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에 넣었습니다: " + label + " x" + amount;
            return true;
        }

        public static bool RemoveBook(int baseId, int grade, int minLevel, out string text)
        {
            Recover();
            Favorite favorite = FindBook(baseId, grade, minLevel);
            if (favorite == null)
            {
                text = "즐겨찾기: 그 비급(" + baseId + ", 경지 " + grade + ", 품질 " + minLevel + " 이상)은 즐겨찾기에 없습니다.";
                return false;
            }
            string label = BookLabel(favorite.BookKind, baseId, grade, minLevel);
            string error;
            if (!Change(next => next.RemoveAll(f => f.Book && f.Id == baseId && f.Grade == grade && f.MinLevel == minLevel), out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에서 뺐습니다: " + label;
            return true;
        }

        public static bool SetBookAmount(int baseId, int grade, int minLevel, int amount, out string text)
        {
            if (amount < 1 || amount > MartialBookService.MaxPerGive)
            {
                text = BookAmountRule;
                return false;
            }
            Recover();
            Favorite favorite = FindBook(baseId, grade, minLevel);
            if (favorite == null)
            {
                text = "즐겨찾기: 그 비급(" + baseId + ", 경지 " + grade + ", 품질 " + minLevel + " 이상)은 즐겨찾기에 없습니다.";
                return false;
            }
            int old = favorite.Amount;
            string label = BookLabel(favorite.BookKind, baseId, grade, minLevel);
            string error;
            if (!Change(next => next.Find(f => f.Book && f.Id == baseId && f.Grade == grade && f.MinLevel == minLevel).Amount = amount, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기 수량: " + label + " " + old + " -> " + amount;
            return true;
        }

        // 목록의 사본을 고쳐 파일에 쓰고, 쓰기에 성공했을 때만 메모리의 목록을 바꾼다.
        private static bool Change(Action<List<Favorite>> edit, out string error)
        {
            Load();
            if (readError != null)
            {
                // 읽지 못한 파일은 덮어쓰지 않는다. 다시 읽어 본다.
                list = null;
                Load();
                if (readError != null)
                {
                    error = "파일을 읽지 못해 바꾸지 않았습니다(" + readError + ").";
                    return false;
                }
            }
            var next = new List<Favorite>();
            foreach (Favorite f in list)
            {
                next.Add(f.Copy());
            }
            edit(next);
            try
            {
                Directory.CreateDirectory(ChangeLog.DataDir);
                if (keepOriginal && File.Exists(FilePath))
                {
                    File.Copy(FilePath, FilePath + ".bak", true);
                }
                AtomicFile.WriteAllText(FilePath, WriteLines(next), new UTF8Encoding(false));
                keepOriginal = false;
            }
            catch (Exception e)
            {
                error = "파일에 쓰지 못했습니다(" + e.Message + ").";
                return false;
            }
            list = next;
            error = null;
            return true;
        }
    }
}
