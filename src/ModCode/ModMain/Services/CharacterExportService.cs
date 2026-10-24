using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 캐릭터 내보내기·가져오기. 화면을 모르고 결과를 문장으로 돌려준다. 대상은 항상 플레이어다.
    // 근거: docs/superpowers/specs/2026-10-13-character-export-design.md.
    internal static partial class CharacterExportService
    {
        public const string Kind = "character";
        private const int HeadLength = 3;   // 공법 값 배열의 머리: 2, 0, 수량

        public enum Section { Grade, Attributes, Martials, Arts, Lucks, Fates }

        public class Attr
        {
            public string Key = "";
            public string Name = "";
            public int Value;
            public string Problem;      // 이 줄을 쓸 수 없는 이유. null이면 쓸 수 있다
        }

        public class MartialLine
        {
            public int Type;
            public int BaseId;
            public string Name = "";
            public int Grade;
            public int Exp;
            public bool Equipped;
            public int[] Values = new int[0];   // 머리 세 칸 뒤의 값
            public string Problem;
        }

        public class ArtLine
        {
            public string KindName = "";    // ArtService.Kind.Tab
            public int Id;
            public string Name = "";
            public int Exp;
            public int MaxExp = 100;
            public string Problem;
        }

        public class LuckLine
        {
            public int Id;
            public string Name = "";
            public string Problem;
        }

        public class FateLine
        {
            public int Grade;
            public int Id;
            public string Name = "";
            public string Problem;
        }

        public class Pack
        {
            public string File;
            public string Name = "";
            public string Date = "";
            public string Character = "";
            public bool HasGrade;
            public int GradeId;
            public string GradeName = "";
            public int GradeExp;
            public List<Attr> Attributes = new List<Attr>();
            public List<MartialLine> Martials = new List<MartialLine>();
            public List<ArtLine> Arts = new List<ArtLine>();
            public List<LuckLine> BornLucks = new List<LuckLine>();
            public List<LuckLine> AddLucks = new List<LuckLine>();
            public List<FateLine> Fates = new List<FateLine>();
            public string Error;        // 읽을 수 없는 이유. null이면 읽혔다
        }

        private static WorldUnitBase Me
        {
            get { return g.world.playerUnit; }
        }

        // ---- 내보내기 ----

        public static string Export(string name)
        {
            if (!EditService.WorldReady)
            {
                return "내보내기: 세이브에 진입한 뒤에 사용할 수 있습니다.";
            }
            string stem = ExportFiles.FileNameFor(name);
            if (stem.Length == 0)
            {
                return "내보내기: 이름을 입력하세요.";
            }
            WorldUnitBase me = Me;
            var saved = me.data.unitData.propertyData;

            var attributes = new List<string>();
            foreach (AttributeDef def in AttributeCatalog.All)
            {
                attributes.Add("    { \"key\": " + Json.Quote(def.Key) + ", \"name\": " + Json.Quote(def.Label) + ", \"value\": " + def.Get(saved) + " }");
            }

            var martials = new List<string>();
            int broken = 0;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                // 깨진 공법은 속성을 읽는 것만으로 예외가 난다. 그런 것은 담지 않는다.
                try
                {
                    DataProps.PropsData props = pair.Value.data;
                    DataProps.MartialData martial = props.To<DataProps.MartialData>();
                    var tail = new List<string>();
                    for (int i = HeadLength; i < props.values.Length; i++)
                    {
                        tail.Add(props.values[i].ToString());
                    }
                    martials.Add("    { \"type\": " + (int)martial.martialType + ", \"baseId\": " + martial.baseID + ", \"name\": " + Json.Quote(MartialService.Name(martial))
                        + ", \"grade\": " + martial.grade + ", \"exp\": " + pair.Value.exp + ", \"equipped\": " + (MartialService.IsEquipped(me, pair.Key) ? "true" : "false")
                        + ",\n      \"values\": [" + string.Join(", ", tail.ToArray()) + "] }");
                }
                catch (Exception)
                {
                    broken++;
                }
            }

            var arts = new List<string>();
            var world = g.data.world;
            foreach (ArtService.Kind kind in ArtService.Kinds)
            {
                foreach (ItemCatalog.Entry book in ArtService.Books(kind, ""))
                {
                    if (!ArtService.Learned(kind, book.Id))
                    {
                        continue;
                    }
                    int exp = 0, maxExp = 100;
                    for (int i = 0; i < world.pillFormulas.Count; i++)
                    {
                        if (world.pillFormulas[i].id == book.Id)
                        {
                            exp = world.pillFormulas[i].exp;
                            maxExp = world.pillFormulas[i].maxExp;
                        }
                    }
                    arts.Add("    { \"kind\": " + Json.Quote(kind.Tab) + ", \"id\": " + book.Id + ", \"name\": " + Json.Quote(book.Name)
                        + ", \"exp\": " + exp + ", \"maxExp\": " + maxExp + " }");
                }
            }

            var born = new List<string>();
            var added = new List<string>();
            foreach (LuckService.LuckView view in LuckService.Owned(me))
            {
                string line = "    { \"id\": " + view.Id + ", \"name\": " + Json.Quote(view.Name) + " }";
                if (view.Born)
                {
                    born.Add(line);
                }
                // 진행 표식, 숨김, 기간이 있는 것은 이 세이브의 진행 상태다. 역천개명 분류는 아래에서 경지와 함께 담는다.
                else if (!view.IsSystem && !view.Hidden && view.Duration == -1 && view.Type != 3)
                {
                    added.Add(line);
                }
            }

            var fates = new List<string>();
            foreach (FateService.Slot slot in FateService.Slots(me))
            {
                if (slot.LuckId != 0)
                {
                    fates.Add("    { \"grade\": " + slot.Grade + ", \"id\": " + slot.LuckId + ", \"name\": " + Json.Quote(slot.LuckName) + " }");
                }
            }

            var sb = new StringBuilder();
            sb.Append("{\n  \"format\": 1,\n  \"kind\": \"").Append(Kind).Append("\",\n  \"name\": ").Append(Json.Quote((name ?? "").Trim()))
                .Append(",\n  \"exportedAt\": \"").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append("\",\n  \"character\": ").Append(Json.Quote(EditService.NameOf(me)))
                .Append(",\n  \"grade\": { \"id\": ").Append(saved.gradeID).Append(", \"name\": ").Append(Json.Quote(EditService.GradeName(saved.gradeID)))
                .Append(", \"exp\": ").Append(saved.exp).Append(" }");
            Block(sb, "attributes", attributes);
            Block(sb, "martials", martials);
            Block(sb, "arts", arts);
            Block(sb, "bornLucks", born);
            Block(sb, "addLucks", added);
            Block(sb, "fates", fates);
            sb.Append("\n}\n");
            try
            {
                string path = ExportFiles.Write(Kind, stem, sb.ToString());
                return "내보냈습니다: " + Path.GetFileName(path) + " (능력치 " + attributes.Count + ", 공법 " + martials.Count + ", 기예 " + arts.Count
                    + ", 운명 " + (born.Count + added.Count) + ", 역천개명 " + fates.Count + (broken > 0 ? ", 읽지 못한 공법 " + broken + "개는 뺐습니다" : "") + ")";
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

        private static void Block(StringBuilder sb, string name, List<string> lines)
        {
            sb.Append(",\n  \"").Append(name).Append("\": [\n").Append(string.Join(",\n", lines.ToArray())).Append("\n  ]");
        }

        // ---- 읽기 ----

        private const string BadNumber = "번호나 값이 숫자가 아닙니다";
        private const string BadShape = "줄의 모양이 틀립니다";

        private static Pack Read(string file)
        {
            var pack = new Pack { File = file, Name = Path.GetFileNameWithoutExtension(file) };
            try
            {
                var root = Json.Parse(System.IO.File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>;
                int format;
                if (root == null || !ExportFiles.Whole(root, "format", out format) || format != 1 || ExportFiles.Text(root, "kind") != Kind)
                {
                    pack.Error = "이 편집기의 캐릭터 내보내기 파일(format 1)이 아닙니다";
                    return pack;
                }
                string name = ExportFiles.Text(root, "name");
                if (name.Length > 0)
                {
                    pack.Name = name;
                }
                pack.Date = ExportFiles.Text(root, "exportedAt");
                pack.Character = ExportFiles.Text(root, "character");
                object grade;
                if (root.TryGetValue("grade", out grade) && grade is Dictionary<string, object>)
                {
                    var o = (Dictionary<string, object>)grade;
                    pack.HasGrade = ExportFiles.Whole(o, "id", out pack.GradeId);
                    ExportFiles.Whole(o, "exp", out pack.GradeExp);
                    pack.GradeName = ExportFiles.Text(o, "name");
                }
                foreach (var o in ExportFiles.Items(root, "attributes"))
                {
                    var line = new Attr();
                    if (o == null)
                    {
                        line.Problem = BadShape;
                    }
                    else
                    {
                        line.Key = ExportFiles.Text(o, "key");
                        line.Name = ExportFiles.Text(o, "name");
                        if (!ExportFiles.Whole(o, "value", out line.Value))
                        {
                            line.Problem = BadNumber;
                        }
                    }
                    pack.Attributes.Add(line);
                }
                foreach (var o in ExportFiles.Items(root, "martials"))
                {
                    var line = new MartialLine();
                    if (o == null)
                    {
                        line.Problem = BadShape;
                    }
                    else
                    {
                        line.Name = ExportFiles.Text(o, "name");
                        ExportFiles.Whole(o, "type", out line.Type);
                        ExportFiles.Whole(o, "grade", out line.Grade);
                        ExportFiles.Whole(o, "exp", out line.Exp);
                        object flag;
                        line.Equipped = o.TryGetValue("equipped", out flag) && flag is bool && (bool)flag;
                        if (!ExportFiles.Whole(o, "baseId", out line.BaseId) || !ExportFiles.Ints(o, "values", out line.Values))
                        {
                            line.Problem = BadNumber;
                        }
                        // 값의 앞 세 칸은 종류, 기술 번호, 경지다. 그보다 짧거나 기술 번호가 다르면 이 줄로는 공법을 만들 수 없다.
                        else if (line.Values.Length < 3 || line.Values[1] != line.BaseId)
                        {
                            line.Problem = "공법의 값(values)이 모자라거나 기술 번호와 맞지 않습니다";
                        }
                    }
                    pack.Martials.Add(line);
                }
                foreach (var o in ExportFiles.Items(root, "arts"))
                {
                    var line = new ArtLine();
                    if (o == null)
                    {
                        line.Problem = BadShape;
                    }
                    else
                    {
                        line.KindName = ExportFiles.Text(o, "kind");
                        line.Name = ExportFiles.Text(o, "name");
                        ExportFiles.Whole(o, "exp", out line.Exp);
                        if (!ExportFiles.Whole(o, "maxExp", out line.MaxExp))
                        {
                            line.MaxExp = 100;
                        }
                        if (!ExportFiles.Whole(o, "id", out line.Id))
                        {
                            line.Problem = BadNumber;
                        }
                    }
                    pack.Arts.Add(line);
                }
                ReadLucks(root, "bornLucks", pack.BornLucks);
                ReadLucks(root, "addLucks", pack.AddLucks);
                foreach (var o in ExportFiles.Items(root, "fates"))
                {
                    var line = new FateLine();
                    if (o == null)
                    {
                        line.Problem = BadShape;
                    }
                    else
                    {
                        line.Name = ExportFiles.Text(o, "name");
                        if (!ExportFiles.Whole(o, "grade", out line.Grade) || !ExportFiles.Whole(o, "id", out line.Id))
                        {
                            line.Problem = BadNumber;
                        }
                    }
                    pack.Fates.Add(line);
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

        private static void ReadLucks(Dictionary<string, object> root, string key, List<LuckLine> into)
        {
            foreach (var o in ExportFiles.Items(root, key))
            {
                var line = new LuckLine();
                if (o == null)
                {
                    line.Problem = BadShape;
                }
                else
                {
                    line.Name = ExportFiles.Text(o, "name");
                    if (!ExportFiles.Whole(o, "id", out line.Id))
                    {
                        line.Problem = BadNumber;
                    }
                }
                into.Add(line);
            }
        }

        // 내보낸 것들. 최신이 앞.
        public static List<Pack> List()
        {
            var result = new List<Pack>();
            foreach (string file in ExportFiles.Files(Kind))
            {
                result.Add(Read(file));
            }
            return result;
        }

        public static string Delete(Pack pack)
        {
            return ExportFiles.Delete(Kind, pack.File);
        }
    }
}
