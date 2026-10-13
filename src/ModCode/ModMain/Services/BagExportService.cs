using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 가방 내보내기·가져오기. 화면을 모르고 결과를 문장으로 돌려준다.
    // 아이템은 읽어 둔 값으로 PropsData.New로 다시 만들어 넣는다(실측: 번호·수량·종류·값이 원본과 같다).
    // 근거: docs/superpowers/specs/2026-10-13-bag-export-design.md 3절.
    internal static class BagExportService
    {
        public class Item
        {
            public int Id;
            public string Name = "";
            public int Count;
            public bool Martial;        // 비급. 번호가 0이고 내용이 Values에 있다
            public int[] Values = new int[0];   // 머리 세 칸(종류, 번호, 수량) 뒤의 값
            public bool Equipped;
            public string Problem;      // 파일의 이 줄을 쓸 수 없는 이유. null이면 쓸 수 있다
        }

        public class Money
        {
            public int Id;
            public string Name = "";
            public int Count;
            public string Problem;
        }

        public class Pack
        {
            public string File;
            public string Name = "";
            public string Date = "";
            public string Character = "";
            public int Skipped;
            public List<Item> Items = new List<Item>();
            public List<Money> Currencies = new List<Money>();
            public string Error;        // 읽을 수 없는 이유. null이면 읽혔다
        }

        private const int HeadLength = 3;   // values의 머리: 종류, 번호, 수량

        public static string Dir
        {
            get { return Path.Combine(Path.Combine(ChangeLog.DataDir, "exports"), "bag"); }
        }

        private static DataProps Bag
        {
            get { return g.world.playerUnit.data.unitData.propData; }
        }

        // 지금 가방에 비어 있는 칸. 넘쳐 있으면 0.
        public static int FreeSlots()
        {
            DataProps bag = Bag;
            return Math.Max(0, bag.GetGridMaxNum() - bag.GetGridCurNum());
        }

        private static bool Stacks(int propsId)
        {
            ConfItemPropsItem conf = g.conf.itemProps.GetItem(propsId);
            return conf != null && conf.isOverlay != 0;
        }

        // 이 묶음을 전부 넣을 때 필요한 칸. 겹쳐지는 것은 가방에 이미 있으면 칸을 쓰지 않고, 없으면 종류마다 한 칸이다.
        public static int NeededSlots(Pack pack)
        {
            DataProps bag = Bag;
            int need = 0;
            var newStacks = new HashSet<int>();
            foreach (Item item in pack.Items)
            {
                if (Usable(item) != null)
                {
                    continue;
                }
                if (!item.Martial && Stacks(item.Id))
                {
                    if (bag.GetPropsNum(item.Id) == 0 && newStacks.Add(item.Id))
                    {
                        need++;
                    }
                }
                else
                {
                    need++;
                }
            }
            return need;
        }

        // ---- 내보내기 ----

        private static string FileNameFor(string name)
        {
            var sb = new StringBuilder();
            char[] bad = Path.GetInvalidFileNameChars();
            foreach (char c in name)
            {
                sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
            }
            return sb.ToString().Trim().TrimEnd('.');
        }

        public static string Export(string name)
        {
            if (!EditService.WorldReady)
            {
                return "내보내기: 세이브에 진입한 뒤에 사용할 수 있습니다.";
            }
            string stem = FileNameFor((name ?? "").Trim());
            if (stem.Length == 0)
            {
                return "내보내기: 이름을 입력하세요.";
            }
            WorldUnitBase me = g.world.playerUnit;
            DataProps bag = Bag;
            var sb = new StringBuilder();
            int items = 0, skipped = 0, currencies = 0;
            var itemLines = new List<string>();
            for (int i = 0; i < bag.allProps.Count; i++)
            {
                DataProps.PropsData d = bag.allProps[i];
                Il2CppStringArray strs = d.GetValuesStrArray();
                // 문자열을 가진 아이템은 이 세이브의 NPC 등을 가리킨다(종문 추천서). 다른 세이브에서는 뜻이 없으므로 담지 않는다.
                if (strs != null && strs.Length > 0)
                {
                    skipped++;
                    continue;
                }
                bool martial = d.propsType == DataProps.PropsDataType.Martial;
                string label;
                try
                {
                    label = martial ? MartialService.Name(d.To<DataProps.MartialData>()) : (EditService.ItemName(d.propsID) ?? "");
                }
                catch (Exception)
                {
                    label = martial ? "(비급)" : "";
                }
                var values = new StringBuilder();
                for (int v = HeadLength; d.values != null && v < d.values.Length; v++)
                {
                    values.Append(v == HeadLength ? "" : ", ").Append(d.values[v]);
                }
                itemLines.Add("    { \"id\": " + d.propsID + ", \"name\": " + Json.Quote(label) + ", \"count\": " + d.propsCount
                    + ", \"type\": \"" + (martial ? "Martial" : "Props") + "\", \"values\": [" + values + "], \"equipped\": "
                    + (me.data.unitData.IsEquipProps(d.soleID) ? "true" : "false") + " }");
                items++;
            }
            var moneyLines = new List<string>();
            foreach (ItemCatalog.Entry entry in ItemCatalog.Currencies)
            {
                int count = bag.GetPropsNum(entry.Id);
                if (count > 0)
                {
                    moneyLines.Add("    { \"id\": " + entry.Id + ", \"name\": " + Json.Quote(entry.Name) + ", \"count\": " + count + " }");
                    currencies++;
                }
            }
            sb.Append("{\n  \"format\": 1,\n  \"kind\": \"bag\",\n  \"name\": ").Append(Json.Quote(name.Trim()))
                .Append(",\n  \"exportedAt\": \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append("\",\n  \"character\": ").Append(Json.Quote(EditService.NameOf(me)))
                .Append(",\n  \"skipped\": ").Append(skipped)
                .Append(",\n  \"items\": [\n").Append(string.Join(",\n", itemLines.ToArray())).Append("\n  ],\n  \"currencies\": [\n")
                .Append(string.Join(",\n", moneyLines.ToArray())).Append("\n  ]\n}\n");
            try
            {
                Directory.CreateDirectory(Dir);
                string path = Path.Combine(Dir, stem + ".json");
                for (int n = 2; File.Exists(path); n++)
                {
                    path = Path.Combine(Dir, stem + " (" + n + ").json");
                }
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                return "내보냈습니다: " + Path.GetFileName(path) + " (아이템 " + items + "줄, 화폐 " + currencies + "종"
                    + (skipped > 0 ? ", 이 세이브에 묶인 아이템 " + skipped + "개는 뺐습니다" : "") + ")";
            }
            catch (IOException e)
            {
                return "내보내기: 파일을 쓰지 못했습니다(" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "내보내기: 파일을 쓰지 못했습니다(" + e.Message + ").";
            }
        }

        // ---- 읽기 ----

        private static string Text(Dictionary<string, object> o, string key)
        {
            object v;
            return o.TryGetValue(key, out v) && v is string ? (string)v : "";
        }

        // 정수로 읽는다. 없거나 정수가 아니거나 int 범위를 넘으면 false.
        private static bool Whole(Dictionary<string, object> o, string key, out int value)
        {
            value = 0;
            object v;
            if (!o.TryGetValue(key, out v) || !(v is long))
            {
                return false;
            }
            long n = (long)v;
            if (n < int.MinValue || n > int.MaxValue)
            {
                return false;
            }
            value = (int)n;
            return true;
        }

        private static Pack Read(string file)
        {
            var pack = new Pack { File = file, Name = Path.GetFileNameWithoutExtension(file) };
            try
            {
                var root = Json.Parse(File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>;
                int format;
                if (root == null || !Whole(root, "format", out format) || format != 1 || Text(root, "kind") != "bag")
                {
                    pack.Error = "이 편집기의 가방 내보내기 파일(format 1)이 아닙니다";
                    return pack;
                }
                string name = Text(root, "name");
                if (name.Length > 0)
                {
                    pack.Name = name;
                }
                pack.Date = Text(root, "exportedAt");
                pack.Character = Text(root, "character");
                Whole(root, "skipped", out pack.Skipped);
                object list;
                if (root.TryGetValue("items", out list) && list is List<object>)
                {
                    foreach (object each in (List<object>)list)
                    {
                        pack.Items.Add(ReadItem(each as Dictionary<string, object>));
                    }
                }
                if (root.TryGetValue("currencies", out list) && list is List<object>)
                {
                    foreach (object each in (List<object>)list)
                    {
                        pack.Currencies.Add(ReadMoney(each as Dictionary<string, object>));
                    }
                }
            }
            catch (FormatException e)
            {
                pack.Error = "JSON 문법 오류(" + e.Message + ")";
            }
            catch (IOException e)
            {
                pack.Error = e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                pack.Error = e.Message;
            }
            return pack;
        }

        private static Item ReadItem(Dictionary<string, object> o)
        {
            var item = new Item();
            if (o == null)
            {
                item.Problem = "줄의 모양이 틀립니다";
                return item;
            }
            item.Name = Text(o, "name");
            item.Martial = Text(o, "type") == "Martial";
            object flag;
            item.Equipped = o.TryGetValue("equipped", out flag) && flag is bool && (bool)flag;
            if (!Whole(o, "id", out item.Id) || !Whole(o, "count", out item.Count))
            {
                item.Problem = "번호나 수량이 숫자가 아닙니다";
                return item;
            }
            object values;
            if (o.TryGetValue("values", out values) && values is List<object>)
            {
                var list = (List<object>)values;
                item.Values = new int[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    if (!(list[i] is long) || (long)list[i] < int.MinValue || (long)list[i] > int.MaxValue)
                    {
                        item.Problem = "값 배열에 숫자가 아닌 것이 있습니다";
                        return item;
                    }
                    item.Values[i] = (int)(long)list[i];
                }
            }
            return item;
        }

        private static Money ReadMoney(Dictionary<string, object> o)
        {
            var money = new Money();
            if (o == null)
            {
                money.Problem = "줄의 모양이 틀립니다";
                return money;
            }
            money.Name = Text(o, "name");
            if (!Whole(o, "id", out money.Id) || !Whole(o, "count", out money.Count))
            {
                money.Problem = "번호나 수량이 숫자가 아닙니다";
            }
            return money;
        }

        // 내보낸 것들. 최신이 앞. 폴더가 없으면 빈 목록.
        public static List<Pack> List()
        {
            var result = new List<Pack>();
            try
            {
                if (!Directory.Exists(Dir))
                {
                    return result;
                }
                var files = new List<string>(Directory.GetFiles(Dir, "*.json"));
                files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                foreach (string file in files)
                {
                    result.Add(Read(file));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return result;
        }

        // ---- 가져오기 ----

        // 이 줄을 넣을 수 없는 이유. null이면 넣을 수 있다.
        private static string Usable(Item item)
        {
            if (item.Problem != null)
            {
                return item.Problem;
            }
            if (item.Count <= 0)
            {
                return "수량이 1보다 작습니다";
            }
            if (!item.Martial && g.conf.itemProps.GetItem(item.Id) == null)
            {
                return "게임에 없는 아이템 번호입니다";
            }
            return null;
        }

        private static string Usable(Money money)
        {
            if (money.Problem != null)
            {
                return money.Problem;
            }
            if (money.Count <= 0)
            {
                return "수량이 1보다 작습니다";
            }
            foreach (ItemCatalog.Entry entry in ItemCatalog.Currencies)
            {
                if (entry.Id == money.Id)
                {
                    return null;
                }
            }
            return "화폐 목록에 없는 번호입니다";
        }

        // 아이템 한 줄을 가방에 넣는다. 넣은 뒤 실제로 늘었는지 본다(가방이 가득 차면 게임이 예외 없이 넣지 않는다).
        private static bool Put(Item item)
        {
            DataProps bag = Bag;
            int before = item.Martial ? bag.allProps.Count : bag.GetPropsNum(item.Id);
            var tail = new Il2CppStructArray<int>(item.Values.Length);
            for (int i = 0; i < item.Values.Length; i++)
            {
                tail[i] = item.Values[i];
            }
            DataProps.PropsData made = DataProps.PropsData.New(item.Id, item.Count,
                item.Martial ? DataProps.PropsDataType.Martial : DataProps.PropsDataType.Props, tail);
            bag.AddProps(made);
            int after = item.Martial ? bag.allProps.Count : bag.GetPropsNum(item.Id);
            return after == before + (item.Martial ? 1 : item.Count);
        }

        private static bool PutMoney(Money money)
        {
            WorldUnitBase me = g.world.playerUnit;
            int before = Bag.GetPropsNum(money.Id);
            if ((long)before + money.Count > int.MaxValue)
            {
                return false;
            }
            if (money.Id == PropsIDType.Money)
            {
                me.data.RewardPropMoney(money.Count);
            }
            else
            {
                me.data.RewardPropItem(money.Id, money.Count, false);
            }
            return Bag.GetPropsNum(money.Id) == before + money.Count;
        }

        private static string Label(Item item)
        {
            return (item.Name.Length > 0 ? item.Name : item.Martial ? "(비급)" : "아이템") + (item.Martial ? "" : "(" + item.Id + ")");
        }

        // 묶음의 파일이 아직 있는지. 목록을 본 뒤 밖에서 지워졌을 수 있다.
        private static string Gone(Pack pack)
        {
            return File.Exists(pack.File) ? null : "가져오기: 파일이 없습니다(" + Path.GetFileName(pack.File) + "). 목록을 다시 여세요.";
        }

        public static string ImportItem(Pack pack, int index)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            Item item = pack.Items[index];
            string problem = Usable(item);
            if (problem != null)
            {
                return Label(item) + ": " + problem + ". 가져오지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            bool ok = Put(item);
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), "가방 가져오기 " + Label(item), "+0", ok ? "+" + item.Count : "+0", ok);
            return ok ? Label(item) + " x" + item.Count + "을(를) 가져왔습니다."
                : Label(item) + ": 가방에 들어가지 않았습니다(빈 칸을 확인하세요).";
        }

        public static string ImportCurrency(Pack pack, int index)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            Money money = pack.Currencies[index];
            string problem = Usable(money);
            if (problem != null)
            {
                return money.Name + "(" + money.Id + "): " + problem + ". 가져오지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            bool ok = PutMoney(money);
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), "가방 가져오기 " + money.Name + "(" + money.Id + ")", "+0", ok ? "+" + money.Count : "+0", ok);
            return ok ? money.Name + " +" + money.Count + "을(를) 가져왔습니다." : money.Name + ": 수량이 기대한 만큼 오르지 않았습니다.";
        }

        // 전부 가져온다. expandBag이면 모자란 칸만큼 가방의 추가 칸을 먼저 늘린다(10칸 단위로 올림, 상한까지).
        public static string ImportAll(Pack pack, bool expandBag)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string grown = "";
            if (expandBag)
            {
                int missing = NeededSlots(pack) - FreeSlots();
                int room = BagService.MaxExtra - BagService.Extra;
                int add = Math.Min(room, (missing + BagService.Step - 1) / BagService.Step * BagService.Step);
                if (missing > 0 && add > 0)
                {
                    BagService.Change(add);
                    grown = " 가방 추가 칸 +" + add + ".";
                }
            }
            int put = 0, skipped = 0;
            var failed = new List<string>();
            // 겹쳐지는 것부터 넣는다. 칸을 쓰지 않거나 한 칸만 쓰므로, 뒤의 것 때문에 자리를 잃지 않는다.
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (Item item in pack.Items)
                {
                    bool stacks = !item.Martial && item.Problem == null && Stacks(item.Id);
                    if (stacks != (pass == 0))
                    {
                        continue;
                    }
                    if (Usable(item) != null)
                    {
                        skipped++;
                        continue;
                    }
                    if (Put(item))
                    {
                        put++;
                    }
                    else
                    {
                        failed.Add(Label(item));
                    }
                }
            }
            int money = 0;
            foreach (Money each in pack.Currencies)
            {
                if (Usable(each) != null)
                {
                    skipped++;
                }
                else if (PutMoney(each))
                {
                    money++;
                }
                else
                {
                    failed.Add(each.Name);
                }
            }
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), "가방 가져오기 전부(" + pack.Name + ")", "-",
                "아이템 " + put + "줄, 화폐 " + money + "종, 못 넣음 " + failed.Count + "줄", failed.Count == 0);
            var sb = new StringBuilder(pack.Name + ": 아이템 " + put + "줄, 화폐 " + money + "종을 가져왔습니다." + grown);
            if (skipped > 0)
            {
                sb.Append(" 건너뜀 ").Append(skipped).Append("줄(쓸 수 없는 줄).");
            }
            if (failed.Count > 0)
            {
                sb.Append(" 못 넣음 ").Append(failed.Count).Append("줄(가방이 가득 참): ");
                for (int i = 0; i < failed.Count && i < 5; i++)
                {
                    sb.Append(i == 0 ? "" : ", ").Append(failed[i]);
                }
                if (failed.Count > 5)
                {
                    sb.Append(" 외 ").Append(failed.Count - 5).Append("줄");
                }
            }
            return sb.ToString();
        }

        // 내보내기 파일을 지운다. 내보내기 폴더 안의 .json만 지운다.
        public static string Delete(Pack pack)
        {
            try
            {
                string full = Path.GetFullPath(pack.File);
                if (!full.StartsWith(Path.GetFullPath(Dir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || !full.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return "삭제: 내보내기 폴더의 파일이 아니라 지우지 않았습니다.";
                }
                if (!File.Exists(full))
                {
                    return "삭제: 파일이 이미 없습니다(" + Path.GetFileName(full) + ").";
                }
                File.Delete(full);
                return "지웠습니다: " + Path.GetFileName(full);
            }
            catch (IOException e)
            {
                return "삭제: 지우지 못했습니다(" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "삭제: 지우지 못했습니다(" + e.Message + ").";
            }
        }
    }
}
