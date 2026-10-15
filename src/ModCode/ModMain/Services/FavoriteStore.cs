using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 자원 탭의 즐겨찾기: 아이템 번호와 지급 수량. 모든 세이브에 공통이고 TlToybox\favorites.txt에 둔다.
    // 쓰기는 임시 파일에 쓴 뒤 바꿔치기한다. 깨진 줄이 있던 파일은 처음 덮어쓰기 전에 .bak으로 남긴다.
    // 읽지 못한 파일은 덮어쓰지 않는다. 근거: docs/superpowers/specs/2026-10-15-resources-tab-design.md 3.2·3.3절, 6절.
    internal static class FavoriteStore
    {
        public sealed class Favorite
        {
            public int Id;
            public int Amount;
        }

        public const int MaxAmount = AmountBar.Max;
        private const string FileName = "favorites.txt";
        private static readonly string AmountRule = "즐겨찾기: 수량은 1~" + MaxAmount + " 사이의 숫자로 입력하세요.";

        private static List<Favorite> list;     // null이면 아직 읽지 않았다
        private static string readError;        // 파일을 읽지 못한 이유. 있으면 덮어쓰지 않는다
        private static bool keepOriginal;       // 깨진 줄이 있었다: 처음 덮어쓰기 전에 .bak으로 남긴다
        private static bool noticePending;      // 자원 탭에 들어올 때 한 번 알릴 것이 있다

        public static int SkippedLines { get; private set; }

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
                    var seen = new HashSet<int>();
                    foreach (string line in File.ReadAllLines(FilePath, Encoding.UTF8))
                    {
                        string trimmed = line.Trim();
                        if (trimmed.Length == 0 || trimmed[0] == '#')
                        {
                            continue;
                        }
                        string[] parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        int id, amount;
                        if (parts.Length != 2 || !int.TryParse(parts[0], out id) || !int.TryParse(parts[1], out amount) || !seen.Add(id))
                        {
                            SkippedLines++;
                            continue;
                        }
                        list.Add(new Favorite { Id = id, Amount = Math.Max(1, Math.Min(MaxAmount, amount)) });
                    }
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

        // 넣은 순서의 즐겨찾기. 고치지 말고 읽기만 한다.
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
            return list.Find(f => f.Id == id);
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

        private static string Label(int id)
        {
            ItemCatalog.Entry entry = ItemCatalog.Get(id);
            return entry != null ? entry.Name : "게임에 없는 아이템 (" + id + ")";
        }

        public static bool Add(int id, int amount, out string text)
        {
            if (amount < 1 || amount > MaxAmount)
            {
                text = AmountRule;
                return false;
            }
            if (Has(id))
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 이미 있습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.Add(new Favorite { Id = id, Amount = amount }), out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기에 넣었습니다: " + Label(id) + " x" + amount;
            return true;
        }

        public static bool Remove(int id, out string text)
        {
            if (!Has(id))
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 즐겨찾기에 없습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.RemoveAll(f => f.Id == id), out error))
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
            int old = AmountOf(id);
            if (old == 0)
            {
                text = "즐겨찾기: " + Label(id) + "은(는) 즐겨찾기에 없습니다.";
                return false;
            }
            string error;
            if (!Change(next => next.Find(f => f.Id == id).Amount = amount, out error))
            {
                text = "즐겨찾기: " + error;
                return false;
            }
            text = "즐겨찾기 수량: " + Label(id) + " " + old + " -> " + amount;
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
                next.Add(new Favorite { Id = f.Id, Amount = f.Amount });
            }
            edit(next);
            try
            {
                Directory.CreateDirectory(ChangeLog.DataDir);
                if (keepOriginal && File.Exists(FilePath))
                {
                    File.Copy(FilePath, FilePath + ".bak", true);
                }
                var sb = new StringBuilder("# TlToybox 자원 탭 즐겨찾기: 한 줄에 <아이템 번호> <수량>\n");
                foreach (Favorite f in next)
                {
                    sb.Append(f.Id).Append(' ').Append(f.Amount).Append('\n');
                }
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                if (File.Exists(FilePath))
                {
                    File.Replace(tmp, FilePath, null);
                }
                else
                {
                    File.Move(tmp, FilePath);
                }
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
