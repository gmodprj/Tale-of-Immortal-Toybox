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

        private const int ClothClass = 304;   // ItemProps.className: 의복

        private static List<Entry> all;
        private static List<Entry> currencies;
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
            currencies = new List<Entry>();
            var counts = new Dictionary<int, int>();
            var items = g.conf.itemProps._allConfList;
            for (int i = 0; i < items.Count; i++)
            {
                ConfItemPropsItem item = items[i];
                string name = GameTool.LS(item.name);
                // 재화(hide)는 아이템 표에 넣지 않는다. 그 가운데 일반 재화(type 0)는 화폐 목록으로 따로 모은다.
                // 천원산 안에서만 쓰는 재화(type 5: 신석, 현옥)와 임무용 물건은 넣지 않는다.
                if (item.hide != 0)
                {
                    if (item.type == 0)
                    {
                        string label = MartialService.CleanDesc(GameTool.LS(item.desc)).Replace("\r", "").Replace('\n', ' ');
                        // 이름의 번역이 없는 재화(등선폐)는 설명이 곧 이름이다.
                        bool named = !string.IsNullOrEmpty(name) && name != item.name;
                        currencies.Add(new Entry { Id = item.id, Name = named ? name : label, Desc = label, Level = item.level, Type = item.type, SubType = item.className });
                    }
                    continue;
                }
                if (string.IsNullOrEmpty(name) || name == item.name)
                {
                    continue;
                }
                string desc = MartialService.CleanDesc(GameTool.LS(item.desc)).Replace("\r", "").Replace('\n', ' ');
                // 의복은 게임의 이름과 설명이 모두 "의복"이다. 성별·모양·색으로 구분되게 한다(의복 표 ClothItem).
                ConfClothItemItem cloth = item.className == ClothClass ? g.conf.clothItem.GetItem(item.id) : null;
                if (cloth != null)
                {
                    string sex = cloth.sex == 1 ? "남" : "여";
                    desc = name + " — " + sex + "성용, 모양 " + cloth.style + ", 색 " + cloth.color;
                    name = name + "(" + sex + ") " + cloth.style + "-" + cloth.color;
                }
                all.Add(new Entry
                {
                    Id = item.id,
                    Name = name,
                    Desc = desc,
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

        // 영석을 포함한 일반 재화(종문 공헌, 성주령, 전공 등). 가방 칸을 차지하지 않고 수량만 있다.
        public static List<Entry> Currencies
        {
            get
            {
                if (currencies == null)
                {
                    Build();
                }
                return currencies;
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
            // 게임의 분류표에는 이름이 같은 세부 분류가 여럿 있다. 번호를 붙여 구분한다.
            var seen = new Dictionary<string, int>();
            foreach (Category category in result)
            {
                int n;
                seen.TryGetValue(category.Name, out n);
                seen[category.Name] = n + 1;
            }
            foreach (Category category in result)
            {
                if (seen[category.Name] > 1)
                {
                    category.Name += "#" + category.Id;
                }
            }
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
