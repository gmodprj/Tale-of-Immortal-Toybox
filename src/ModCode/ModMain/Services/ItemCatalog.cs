using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 지급할 수 있는 아이템의 목록. 게임의 아이템 표(ItemProps)와 분류표(ItemType)를 한 번 읽어 둔다.
    // 자원 탭은 편집기의 용도 그룹(ItemGroups)으로 거른다(Select). 기예 탭과 개발 명령은 게임의 분류 번호로 거른다(Find).
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
            public ItemGroups.Group Group;  // 자원 탭의 용도 그룹(화폐 목록에서는 null)
            public ItemGroups.Kind Kind;
            public int TableIndex;  // 게임 표에서의 순번
            public bool Missing;    // 즐겨찾기에 있지만 게임에 없는 번호(자원 탭이 만드는 자리표시)
        }

        // 자원 탭의 거르기. Group은 그룹 키(ItemGroups.AllKey면 전체), Kind가 null이면 그룹 전체.
        public sealed class Filter
        {
            private string query = "";

            public string Group = ItemGroups.AllKey;
            public ItemGroups.Kind Kind;
            public int Quality;         // 0이면 전체, 1~6(게임의 level)
            public bool OwnedOnly;

            public string Needle { get; private set; } = "";
            public bool IsId { get; private set; }
            public int Id { get; private set; }

            // 이름의 일부 또는 번호. 넣을 때 공백과 기호를 뺀 검색어를 한 번 만들어 둔다.
            public string Query
            {
                get { return query; }
                set
                {
                    query = value ?? "";
                    Needle = EditService.Compact(query);
                    int id;
                    IsId = int.TryParse(Needle, out id);
                    Id = id;
                }
            }
        }

        private const int ClothClass = 304;   // ItemProps.className: 의복

        private static List<Entry> all;
        private static List<Entry> currencies;
        private static Dictionary<int, Entry> byId;

        internal static string CategoryName(int id)
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
            byId = new Dictionary<int, Entry>();
            // 분류 이름은 분류 번호마다 한 번만 구한다(성능 1 S4. 3,401행마다 두 번 부르던 것).
            var categoryNames = new Dictionary<int, string>();
            Func<int, string> category = id =>
            {
                string cached;
                if (!categoryNames.TryGetValue(id, out cached))
                {
                    cached = CategoryName(id);
                    categoryNames[id] = cached;
                }
                return cached;
            };
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
                var entry = new Entry
                {
                    Id = item.id,
                    Name = name,
                    Desc = desc,
                    Level = item.level,
                    Type = item.type,
                    SubType = item.className,
                    TypeName = category(item.type),
                    SubTypeName = category(item.className),
                    Search = EditService.Compact(name),
                    TableIndex = all.Count,
                };
                ItemGroups.Group group;
                ItemGroups.Kind kind;
                ItemGroups.Place(item.className, item.id, out group, out kind);
                entry.Group = group;
                entry.Kind = kind;
                all.Add(entry);
                byId[entry.Id] = entry;
            }
            ItemGroups.SortOther();
            // 그룹 순서, 그룹 안의 종류 순서, 게임 표 순서. List.Sort는 같은 값의 순서를 지키지 않으므로 표의 순번까지 비교한다.
            all.Sort((a, b) =>
            {
                int c = a.Group.Order.CompareTo(b.Group.Order);
                if (c == 0)
                {
                    c = a.Kind.Order.CompareTo(b.Kind.Order);
                }
                return c != 0 ? c : a.TableIndex.CompareTo(b.TableIndex);
            });
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

        // 조건에 맞는 아이템을 게임 표 순서로. query는 이름의 일부 또는 번호. mainType과 subType은 음수면 전체.
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
            result.Sort((a, b) => a.TableIndex.CompareTo(b.TableIndex));
            return result;
        }

        // 그 번호의 아이템. 게임에 없거나 목록에 넣지 않은 번호면 null.
        public static Entry Get(int id)
        {
            if (all == null)
            {
                Build();
            }
            Entry entry;
            return byId.TryGetValue(id, out entry) ? entry : null;
        }

        // 그 인물의 가방에 있는 아이템 번호들(보유 중만 거르기에 쓴다). 가방을 한 번 훑어 모은다.
        public static HashSet<int> OwnedIds(WorldUnitBase unit)
        {
            var result = new HashSet<int>();
            var props = unit.data.unitData.propData.allProps;
            for (int i = 0; i < props.Count; i++)
            {
                if (props[i].propsCount > 0)
                {
                    result.Add(props[i].propsID);
                }
            }
            return result;
        }

        // 그룹·종류를 빼고 본 거르기(검색어, 품질, 보유 중만). 게임에 없는 번호의 자리표시는 늘 통과한다(뺄 수 있게).
        public static bool Passes(Entry entry, Filter filter, HashSet<int> owned)
        {
            if (entry.Missing)
            {
                return true;
            }
            if (filter.Quality > 0 && entry.Level != filter.Quality)
            {
                return false;
            }
            if (filter.OwnedOnly && (owned == null || !owned.Contains(entry.Id)))
            {
                return false;
            }
            return filter.Needle.Length == 0 || entry.Search.Contains(filter.Needle) || (filter.IsId && entry.Id == filter.Id);
        }

        // 자원 탭의 목록: 그룹(전체면 모두)과 종류 안에서 거르기를 통과한 것. 순서는 그룹·종류·게임 표 순서.
        public static List<Entry> Select(Filter filter, HashSet<int> owned)
        {
            var result = new List<Entry>();
            foreach (Entry entry in All)
            {
                if (filter.Group != ItemGroups.AllKey && entry.Group.Key != filter.Group)
                {
                    continue;
                }
                if (filter.Kind != null && entry.Kind != filter.Kind)
                {
                    continue;
                }
                if (Passes(entry, filter, owned))
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        // 그룹 키마다 거르기(검색어, 품질, 보유 중만)를 통과한 수. AllKey에는 전체의 수.
        // 그룹마다의 수와(group이 있으면) 그 그룹의 종류마다의 수를 한 번 훑어 함께 센다(묶음 3. 13 문서 R6: 셋이던 세기를 하나로). group이 null이면 byKind도 null.
        public static void Counts(Filter filter, HashSet<int> owned, ItemGroups.Group group, out Dictionary<string, int> byGroup, out int[] byKind)
        {
            List<Entry> entries = All;
            byGroup = new Dictionary<string, int>();
            byGroup[ItemGroups.AllKey] = 0;
            foreach (ItemGroups.Group each in ItemGroups.Groups)
            {
                byGroup[each.Key] = 0;
            }
            byKind = group == null ? null : new int[group.Kinds.Count];
            foreach (Entry entry in entries)
            {
                if (!Passes(entry, filter, owned))
                {
                    continue;
                }
                byGroup[ItemGroups.AllKey]++;
                byGroup[entry.Group.Key]++;
                if (byKind != null && entry.Group == group)
                {
                    byKind[entry.Kind.Order]++;
                }
            }
        }

    }
}
