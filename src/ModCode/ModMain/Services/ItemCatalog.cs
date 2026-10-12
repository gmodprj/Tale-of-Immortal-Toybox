using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 지급할 수 있는 아이템의 목록. 이름으로 찾을 수 있게 게임의 아이템 표를 한 번 읽어 둔다.
    internal static class ItemCatalog
    {
        public class Entry
        {
            public int Id;
            public string Name;
            public string Desc;
            public int Level;
            public string Search; // 공백과 기호를 뺀 이름
        }

        private static List<Entry> all;

        private static List<Entry> All
        {
            get
            {
                if (all == null)
                {
                    all = new List<Entry>();
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
                            Search = EditService.Compact(name),
                        });
                    }
                }
                return all;
            }
        }

        public static int Count
        {
            get { return All.Count; }
        }

        // 이름에 query가 들어간 아이템. query가 숫자면 그 ID의 아이템도 포함한다. 비어 있으면 전체.
        public static List<Entry> Find(string query)
        {
            string needle = EditService.Compact(query ?? "");
            if (needle.Length == 0)
            {
                return All;
            }
            int id;
            bool isId = int.TryParse(needle, out id);
            var result = new List<Entry>();
            foreach (Entry entry in All)
            {
                if (entry.Search.Contains(needle) || (isId && entry.Id == id))
                {
                    result.Add(entry);
                }
            }
            return result;
        }
    }
}
