using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 즐겨찾기 파일의 줄 읽기·쓰기(게임 없이 단위 시험한다. 10 문서 R7, 묶음 5). 파일 입출력과 게임 한도는 FavoriteStore.cs.
    internal static partial class FavoriteStore
    {
        public sealed class Favorite
        {
            public int Id;          // 아이템 번호, 비급이면 기술 번호
            public int Amount;
            public bool Book;       // 기술 서적(비급) 줄
            public int BookKind;    // SkillCatalog.Kinds의 순번(0~4)
            public int Grade;       // 비급의 경지(1~10)
            public int MinLevel;    // 비급의 최소 품질(1~6)

            public bool Same(Favorite other)
            {
                return Book == other.Book && Id == other.Id && (!Book || (Grade == other.Grade && MinLevel == other.MinLevel));
            }

            public Favorite Copy()
            {
                return new Favorite { Id = Id, Amount = Amount, Book = Book, BookKind = BookKind, Grade = Grade, MinLevel = MinLevel };
            }
        }

        // 줄 해석에 쓰는 한도.
        public sealed class Limits
        {
            public int Kinds;       // 비급 분류 수(SkillCatalog.Kinds.Length)
            public int MaxAmount;   // 아이템 수량 상한
            public int MaxBook;     // 비급 권수 상한
        }

        private static int Clamp(int value, int max)
        {
            return Math.Max(1, Math.Min(max, value));
        }

        // 한 줄: "<번호> <수량>" 또는 "book <분류> <기술 번호> <경지> <최소 품질> <수량>". 읽지 못하면 null.
        private static Favorite Parse(string[] parts, Limits limits)
        {
            int id, amount;
            if (parts.Length == 2 && int.TryParse(parts[0], out id) && int.TryParse(parts[1], out amount))
            {
                return new Favorite { Id = id, Amount = Clamp(amount, limits.MaxAmount) };
            }
            int kind, grade, minLevel;
            if (parts.Length == 6 && parts[0] == "book" && int.TryParse(parts[1], out kind) && int.TryParse(parts[2], out id)
                && int.TryParse(parts[3], out grade) && int.TryParse(parts[4], out minLevel) && int.TryParse(parts[5], out amount)
                && kind >= 0 && kind < limits.Kinds && grade >= 1 && grade <= 10 && minLevel >= 1 && minLevel <= 6)
            {
                return new Favorite { Book = true, BookKind = kind, Id = id, Grade = grade, MinLevel = minLevel, Amount = Clamp(amount, limits.MaxBook) };
            }
            return null;
        }

        // 파일의 줄들 -> 즐겨찾기. 빈 줄과 '#' 줄은 건너뛰고, 읽지 못한 줄과 같은 항목이 앞에 있는 줄은 skipped에 센다.
        public static List<Favorite> ReadLines(IEnumerable<string> lines, Limits limits, out int skipped)
        {
            var result = new List<Favorite>();
            skipped = 0;
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == '#')
                {
                    continue;
                }
                Favorite favorite = Parse(trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries), limits);
                if (favorite == null || result.Exists(f => f.Same(favorite)))
                {
                    skipped++;
                    continue;
                }
                result.Add(favorite);
            }
            return result;
        }

        // 즐겨찾기 -> 파일 글(머리 주석 한 줄과 항목마다 한 줄).
        public static string WriteLines(IEnumerable<Favorite> favorites)
        {
            var sb = new StringBuilder("# TlToybox 자원 탭 즐겨찾기: 한 줄에 <아이템 번호> <수량>, 또는 book <분류> <기술 번호> <경지> <최소 품질> <수량>\n");
            foreach (Favorite f in favorites)
            {
                if (f.Book)
                {
                    sb.Append("book ").Append(f.BookKind).Append(' ').Append(f.Id).Append(' ').Append(f.Grade).Append(' ').Append(f.MinLevel).Append(' ').Append(f.Amount).Append('\n');
                }
                else
                {
                    sb.Append(f.Id).Append(' ').Append(f.Amount).Append('\n');
                }
            }
            return sb.ToString();
        }
    }
}
