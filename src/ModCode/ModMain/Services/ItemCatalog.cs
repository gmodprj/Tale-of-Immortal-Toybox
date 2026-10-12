using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 지급할 수 있는 아이템의 목록. 게임의 아이템 표(ItemProps)와 분류표(ItemType)를 한 번 읽어 둔다.
    internal static class ItemCatalog
    {
        public class Entry
        {
            public int Id;
            public string Name;
            public string Desc;
            public int Level;
            public int Type;        // 대분류 (ItemProps.type)
            public int SubType;     // 세부 분류 (ItemProps.className)
            public string TypeName;
            public string SubTypeName;
            public string Search;   // 공백과 기호를 뺀 이름
        }

        public class Category
        {
            public int Id;
            public string Name;
            public int Count;
        }

        private static List<Entry> all;
        private static List<Category> mainCategories;

        private static string CategoryName(int id)
        {
            ConfItemTypeItem item = g.conf.itemType.GetItem(id);
            if (item == null)
            {
                return "분류 " + id;
            }
            string name = GameTool.LS(item.name);
            // 번역이 없는 분류는 키가 그대로 돌아온다.
            return string.IsNullOrEmpty(name) || name == item.name ? "분류 " + id : name;
        }

        private static void Build()
        {
            all = new List<Entry>();
            var counts = new Dictionary<int, int>();
            var items = g.conf.itemProps._allConfList;
            for (int i = 0; i < items.Count; i++)
            {
                ConfItemPropsItem item = items[i];
                // 영석 같은 재화(hide)는 자원 탭의 다른 기능으로 다룬다.
                if (item.hide != 0)
                {
                    continue;
                }
                string name = GameTool.LS(item.name);
                if (string.IsNullOrEmpty(name) || name == item.name)
                {
                    continue;
                }
                all.Add(new Entry
                {
                    Id = item.id,
                    Name = name,
                    Desc = MartialService.CleanDesc(GameTool.LS(item.desc)),
                    Level = item.level,
                    Type = item.type,
                    SubType = item.className,
                    TypeName = CategoryName(item.type),
                    SubTypeName = CategoryName(item.className),
                    Search = EditService.Compact(name),
                });
                int count;
                counts.TryGetValue(item.type, out count);
                counts[item.type] = count + 1;
            }
            mainCategories = new List<Category>();
            foreach (var pair in counts)
            {
                mainCategories.Add(new Category { Id = pair.Key, Name = CategoryName(pair.Key), Count = pair.Value });
            }
            mainCategories.Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        private static List<Entry> All
        {
            get
            {
                if (all == null)
                {
                    Build();
                }
                return all;
            }
        }

        public static List<Category> MainCategories
        {
            get
            {
                if (mainCategories == null)
                {
                    Build();
                }
                return mainCategories;
            }
        }

        // 대분류 안의 세부 분류 목록(아이템이 있는 것만).
        public static List<Category> SubCategories(int mainType)
        {
            var counts = new Dictionary<int, Category>();
            foreach (Entry entry in All)
            {
                if (entry.Type != mainType)
                {
                    continue;
                }
                Category category;
                if (!counts.TryGetValue(entry.SubType, out category))
                {
                    category = new Category { Id = entry.SubType, Name = entry.SubTypeName };
                    counts[entry.SubType] = category;
                }
                category.Count++;
            }
            var result = new List<Category>(counts.Values);
            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            return result;
        }

        // 조건에 맞는 아이템. query는 이름의 일부 또는 번호. mainType과 subType은 음수면 전체.
        public static List<Entry> Find(string query, int mainType, int subType)
        {
            string needle = EditService.Compact(query ?? "");
            int id;
            bool isId = int.TryParse(needle, out id);
            var result = new List<Entry>();
            foreach (Entry entry in All)
            {
                if (mainType >= 0 && entry.Type != mainType)
                {
                    continue;
                }
                if (subType >= 0 && entry.SubType != subType)
                {
                    continue;
                }
                if (needle.Length > 0 && !entry.Search.Contains(needle) && !(isId && entry.Id == id))
                {
                    continue;
                }
                result.Add(entry);
            }
            return result;
        }
    }
}
