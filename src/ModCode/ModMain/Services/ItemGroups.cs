using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 자원 탭의 용도 그룹. 게임의 세부 분류 번호(ItemProps.className)를 편집기가 정한 (그룹, 종류)로 옮긴다.
    // 표와 근거: docs/superpowers/specs/2026-10-15-resources-tab-design.md 4절.
    // 표에 없는 번호는 "기타"에 게임 분류 이름으로 들어간다(창작마당 모드가 새 분류로 추가한 아이템 등).
    internal static class ItemGroups
    {
        public sealed class Kind
        {
            public string Name;
            public readonly List<int> Classes = new List<int>();
            public int Order;       // 그룹 안의 순서(0부터)
        }

        public sealed class Group
        {
            public string Key;
            public string Name;
            public int Order;
            public readonly List<Kind> Kinds = new List<Kind>();
        }

        public const string FavKey = "Fav";
        public const string AllKey = "All";
        public const string OtherKey = "Other";
        private const string ClothKey = "Cloth";
        private const int ClothClass = 304;     // 의복. 종류는 의복 표(ClothItem)의 성별로 가른다

        // 그룹 키, 그룹 이름, 종류들("이름:번호,번호"). 의복의 종류는 번호 없이 이름만 둔다(Place가 성별로 고른다).
        private static readonly string[][] Table =
        {
            new[] { "Pill", "단약", "회복:101", "전투:120", "독단:102", "돌파:106", "수명·건강·매력:103", "영약:104", "영과(영근):105", "도심과:629", "심성(독약):110", "대비무:108", "호요:801,802,803,804" },
            new[] { "Talisman", "부적", "전투 부적:111", "지도 부적:112,611", "심령·인요부:633", "그 밖의 부적:608,625", "전투 도구:7" },
            new[] { "Growth", "돌파·성장", "천재지보:508", "영기·영주:505", "조각:507", "도계석:509", "본원지기·선천일기:536,537,653", "명종:535", "선법 습득:6", "체질 개선:613", "비법:631,635", "천명 전용:660,661,662,663,664" },
            new[] { "Material", "제작 재료", "약재:503,107,601", "광재:501,526", "요수 재료:502,512,525", "법보 재료:504,510,513,518,520,521,522,523,524,527,528,530,534", "기령 재료:531,532,533", "의복 재료:511", "선재:612,506" },
            new[] { "Book", "기예 서적", "단방:605", "단로:606", "약보:615", "풍수서:614,657", "도전통론:616", "감식서:617", "연기보:618", "부적 도보:658", "도면·지도:632,630" },
            new[] { "Equip", "장비", "반지:301", "좌기:302", "법보:401", "호신부:627" },
            new[] { ClothKey, "의복", "남", "여", "그 밖" },
            new[] { "Collect", "수집품", "석기:701", "옥기:702", "악기:703,704,705,706", "장신구:707,610", "도기:708", "도감:709", "골기:710", "술·음식:604,634,655", "보물상자:644" },
            new[] { "Quest", "임무·사건", "서신·잔권:602,609,639,649,652", "기억:638", "영물:636,651,659", "비보:637", "선사 물품:656", "그 밖의 임무 물건:2,109,529,603,607,619,624,626,628,640,641,642,643,645,646,647,648,650,654" },
            new[] { OtherKey, "기타" },
        };

        private static List<Group> groups;
        private static Dictionary<int, Kind> byClass;
        private static Dictionary<Kind, Group> owner;
        private static int duplicates;

        private static void Build()
        {
            if (groups != null)
            {
                return;
            }
            var built = new List<Group>();
            byClass = new Dictionary<int, Kind>();
            owner = new Dictionary<Kind, Group>();
            duplicates = 0;
            foreach (string[] row in Table)
            {
                var group = new Group { Key = row[0], Name = row[1], Order = built.Count };
                for (int i = 2; i < row.Length; i++)
                {
                    string[] parts = row[i].Split(':');
                    var kind = new Kind { Name = parts[0], Order = group.Kinds.Count };
                    if (parts.Length > 1)
                    {
                        foreach (string number in parts[1].Split(','))
                        {
                            int id = int.Parse(number);
                            kind.Classes.Add(id);
                            if (byClass.ContainsKey(id))
                            {
                                duplicates++;
                            }
                            byClass[id] = kind;
                        }
                    }
                    group.Kinds.Add(kind);
                    owner[kind] = group;
                }
                built.Add(group);
            }
            groups = built;
        }

        // 표의 그룹들(즐겨찾기·전체는 화면만의 것이라 여기 없다).
        public static List<Group> Groups
        {
            get
            {
                Build();
                return groups;
            }
        }

        // 표에 같은 번호가 두 번 나온 수. 0이어야 한다(검사 rgroups).
        public static int Duplicates
        {
            get
            {
                Build();
                return duplicates;
            }
        }

        public static Group ByKey(string key)
        {
            Build();
            return groups.Find(x => x.Key == key);
        }

        // 아이템 하나의 (그룹, 종류). 의복은 의복 표의 성별로, 표에 없는 번호는 "기타"의 그 분류 이름 종류로.
        public static void Place(int className, int itemId, out Group group, out Kind kind)
        {
            Build();
            if (className == ClothClass)
            {
                group = ByKey(ClothKey);
                ConfClothItemItem cloth = g.conf.clothItem.GetItem(itemId);
                kind = group.Kinds[cloth == null ? 2 : cloth.sex == 1 ? 0 : 1];
                return;
            }
            if (byClass.TryGetValue(className, out kind))
            {
                group = owner[kind];
                return;
            }
            group = ByKey(OtherKey);
            string name = ItemCatalog.CategoryName(className);
            kind = group.Kinds.Find(k => k.Name == name);
            if (kind == null)
            {
                kind = new Kind { Name = name, Order = group.Kinds.Count };
                group.Kinds.Add(kind);
                owner[kind] = group;
            }
            if (!kind.Classes.Contains(className))
            {
                kind.Classes.Add(className);
            }
        }

        // "기타"의 종류를 가장 작은 세부 분류 번호 순으로 놓는다. 카탈로그를 다 만든 뒤 한 번 부른다.
        public static void SortOther()
        {
            Group other = ByKey(OtherKey);
            other.Kinds.Sort((a, b) => Smallest(a).CompareTo(Smallest(b)));
            for (int i = 0; i < other.Kinds.Count; i++)
            {
                other.Kinds[i].Order = i;
            }
        }

        private static int Smallest(Kind kind)
        {
            int min = int.MaxValue;
            foreach (int c in kind.Classes)
            {
                if (c < min)
                {
                    min = c;
                }
            }
            return min;
        }
    }
}
