using System;
using System.Collections.Generic;
using System.IO;

namespace MOD_sWP6UL
{
    // 게임 없이 도는 시험 실행기. 줄마다 PASS/FAIL, 끝에 FAILS=<수>, 실패가 있으면 종료 코드 1(저장소의 ps1 시험과 같은 꼴. 05 문서 49절).
    // 대상: Json(파서·Quote), ExportFiles(값 꺼내기·파일 이름·쓰기·목록·삭제), TranslationTable(번역표 규칙).
    internal static class Program
    {
        private static int fails;

        private static void Check(string name, bool ok, string detail = "")
        {
            if (ok)
            {
                Console.WriteLine("PASS " + name);
            }
            else
            {
                fails++;
                Console.WriteLine("FAIL " + name + "  " + detail);
            }
        }

        // FormatException이 나면 true와 그 글, 아니면 false.
        private static bool Fails(Func<object> action, out string message)
        {
            message = "";
            try
            {
                object v = action();
                message = "no exception: " + (v == null ? "null" : v.ToString());
                return false;
            }
            catch (FormatException e)
            {
                message = e.Message;
                return true;
            }
        }

        private static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            JsonTests();
            ExportFilesTests();
            TranslationTableTests();
            TextReplacerTests();
            CraftPlanTests();
            BagPlanTests();
            TraitRulesTests();
            PresetDefaultsTests();
            Console.WriteLine("FAILS=" + fails);
            return fails > 0 ? 1 : 0;
        }

        private static void JsonTests()
        {
            var o = Json.Parse("{\"a\": 1, \"b\": [true, false, null], \"c\": \"x\", \"d\": {\"e\": -2}}") as Dictionary<string, object>;
            Check("json: an object parses to a dictionary", o != null && o.Count == 4);
            Check("json: an integer is a long", o != null && o["a"] is long && (long)o["a"] == 1);
            var b = o == null ? null : o["b"] as List<object>;
            Check("json: an array keeps true, false and null", b != null && b.Count == 3 && (bool)b[0] && !(bool)b[1] && b[2] == null);
            Check("json: a string is a string", o != null && (string)o["c"] == "x");
            var d = o == null ? null : o["d"] as Dictionary<string, object>;
            Check("json: a nested object and a negative integer", d != null && (long)d["e"] == -2);
            Check("json: a decimal is a double", Json.Parse("1.5") is double && (double)Json.Parse("1.5") == 1.5);
            Check("json: an exponent is a double", Json.Parse("1e3") is double && (double)Json.Parse("1e3") == 1000);
            Check("json: a large integer stays a long", Json.Parse("12345678901234") is long && (long)Json.Parse("12345678901234") == 12345678901234L);
            Check("json: empty object and array", ((Dictionary<string, object>)Json.Parse("{}")).Count == 0 && ((List<object>)Json.Parse("[ ]")).Count == 0);
            Check("json: escapes in a string", (string)Json.Parse("\"a\\nb\\\"c\\\\d\\u0041\\/\"") == "a\nb\"c\\dA/");
            Check("json: a BOM and whitespace are skipped", Json.Parse("\uFEFF \n {\"k\": [1]} \r\n") is Dictionary<string, object>);
            string why;
            Check("json: \\u needs four hex digits", Fails(() => Json.Parse("\"\\u12\""), out why) && why.Contains("16진수"), why);
            Check("json: text after the value is refused", Fails(() => Json.Parse("1 2"), out why) && why.Contains("다른 글자"), why);
            Check("json: an unclosed string is refused", Fails(() => Json.Parse("\"abc"), out why) && why.Contains("따옴표"), why);
            Check("json: a missing colon is refused", Fails(() => Json.Parse("{\"a\" 1}"), out why) && why.Contains("':'"), why);
            Check("json: an unknown word is refused", Fails(() => Json.Parse("nope"), out why) && why.Contains("읽을 수 없습니다"), why);
            Check("json: null text is refused", Fails(() => Json.Parse(null), out why) && why.Contains("빈 내용"), why);
            Check("json: 65 nested arrays are allowed", Json.Parse(new string('[', 65) + new string(']', 65)) is List<object>);
            Check("json: 66 nested arrays hit the depth limit", Fails(() => Json.Parse(new string('[', 66) + new string(']', 66)), out why) && why.Contains("깊게"), why);
            string raw = "a\"b\n\tc\\d\u0001";
            Check("json: Quote escapes and parses back", (string)Json.Parse(Json.Quote(raw)) == raw, Json.Quote(raw));
            Check("json: Quote writes control characters as \\u", Json.Quote("\u0001") == "\"\\u0001\"", Json.Quote("\u0001"));
            Check("json: Quote of null is an empty string", Json.Quote(null) == "\"\"");
        }

        private static void ExportFilesTests()
        {
            var o = new Dictionary<string, object>
            {
                { "s", "x" }, { "n", 5L }, { "f", 1.5 }, { "big", 3000000000L },
                { "ints", new List<object> { 1L, 2L } }, { "mixed", new List<object> { 1L, "x" } },
                { "items", new List<object> { new Dictionary<string, object> { { "k", 1L } }, "x" } },
            };
            int n;
            int[] ints;
            Check("export: Text reads a string", ExportFiles.Text(o, "s") == "x");
            Check("export: Text of a missing or non-string key is empty", ExportFiles.Text(o, "none") == "" && ExportFiles.Text(o, "n") == "");
            Check("export: Whole reads an integer", ExportFiles.Whole(o, "n", out n) && n == 5);
            Check("export: Whole refuses a missing key", !ExportFiles.Whole(o, "none", out n));
            Check("export: Whole refuses a decimal", !ExportFiles.Whole(o, "f", out n));
            Check("export: Whole refuses a value outside int", !ExportFiles.Whole(o, "big", out n));
            Check("export: Ints of a missing key is empty and true", ExportFiles.Ints(o, "none", out ints) && ints.Length == 0);
            Check("export: Ints reads integers", ExportFiles.Ints(o, "ints", out ints) && ints.Length == 2 && ints[1] == 2);
            Check("export: Ints refuses a non-integer element", !ExportFiles.Ints(o, "mixed", out ints));
            Check("export: Ints refuses a non-array", !ExportFiles.Ints(o, "s", out ints));
            List<Dictionary<string, object>> items = ExportFiles.Items(o, "items");
            Check("export: Items keeps objects and nulls the rest", items.Count == 2 && items[0] != null && items[1] == null);
            Check("export: Items of a missing key is empty", ExportFiles.Items(o, "none").Count == 0);
            Check("export: FileNameFor replaces bad characters and trims", ExportFiles.FileNameFor(" a:b*c ") == "a_b_c" && ExportFiles.FileNameFor("name.") == "name" && ExportFiles.FileNameFor("  ") == "");

            string kind = "tests-" + Guid.NewGuid().ToString("N");
            string dir = ExportFiles.Dir(kind);
            try
            {
                string first = ExportFiles.Write(kind, "x", "{}");
                string second = ExportFiles.Write(kind, "x", "{}");
                Check("export: Write makes the folder and the file", File.Exists(first) && Path.GetFileName(first) == "x.json", first);
                Check("export: a second write with the same name gets (2)", Path.GetFileName(second) == "x (2).json", second);
                Check("export: Files lists both", ExportFiles.Files(kind).Count == 2);
                Check("export: Delete refuses a file outside the folder", ExportFiles.Delete(kind, Path.Combine(Path.GetTempPath(), "x.json")).StartsWith("삭제: 내보내기 폴더의 파일이 아니라"));
                Check("export: Delete removes a file inside the folder", ExportFiles.Delete(kind, second).StartsWith("지웠습니다") && !File.Exists(second));
                Check("export: Delete reports an already missing file", ExportFiles.Delete(kind, second).StartsWith("삭제: 파일이 이미 없습니다"));
            }
            finally
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        private static void TranslationTableTests()
        {
            int broken;
            Dictionary<string, string> map = TranslationTable.Parse(new[] { "# comment", "", "   ", "k1\tv1", "nokey", "k2\t a \\n b ", "k1\tv9", "\tnovalue-key" }, out broken);
            Check("table: comments and blank lines are skipped, keys are kept", map.Count == 2 && map.ContainsKey("k1") && map.ContainsKey("k2"), map.Count.ToString());
            Check("table: a line without a tab or without a key is broken", broken == 2, broken.ToString());
            Check("table: key and text are trimmed and \\n becomes a line break", map["k2"] == "a \n b", map["k2"]);
            Check("table: the later line wins for the same key", map["k1"] == "v9", map["k1"]);
            Check("table: KeyOf of a comment, blank, no-tab or empty-key line is null",
                TranslationTable.KeyOf("# x") == null && TranslationTable.KeyOf("") == null && TranslationTable.KeyOf("nokey") == null && TranslationTable.KeyOf(" \tv") == null && TranslationTable.KeyOf(null) == null);
            Check("table: KeyOf trims the key and a trailing CR", TranslationTable.KeyOf("k \tv\r") == "k");
            // texts.txt(원문 기반)의 라벨 원문에는 줄바꿈이 든다("秒\n变身效果："). 키의 "\n" 두 글자도 줄바꿈으로 푼다.
            Check("table: key unescapes \\n like the value", TranslationTable.Parse(new[] { "秒\\n变身效果：\t초\\n변신 효과: " }, out broken).ContainsKey("秒\n变身效果："));
            // 앞뒤 공백은 지우므로, 조각 번역의 끝 공백("보호막의 " + 숫자)은 "\_"로 적는다. 키에도 같이 적용한다.
            string spaced;
            Check("table: \\_ keeps a leading or trailing space", TranslationTable.Parse(new[] { "a\\_\t\\_b\\_" }, out broken).TryGetValue("a ", out spaced) && spaced == " b ");
            List<string> missing = TranslationTable.MissingLines(new[] { "a\t1", "# c", "nokey" }, "a\t2\r\nb\t3\n# z\nb\t4\n\nc\t5");
            Check("table: MissingLines keeps only the first embedded line of each new key", missing.Count == 2 && missing[0] == "b\t3" && missing[1] == "c\t5", string.Join("|", missing));
            Check("table: MissingLines of a null embedded text is empty", TranslationTable.MissingLines(new string[0], null).Count == 0);
        }

        // TextReplacer(원문 기반 치환. 스펙 2026-10-17-zh-text-korean-design.md 3.3절).
        private static void TextReplacerTests()
        {
            var zh = new TextReplacer(new Dictionary<string, string> { { "变身技-", "변신기-" }, { "变身技", "변신기" }, { "幽莹", "유영" }, { "无", "없음" }, { "（未解锁）", "(미해금)" }, { "体力{icon_tili}*<y>100</y>%", "체력{icon_tili}*<y>100</y>%" } });
            bool changed;
            Check("zh: HasChinese sees CJK only", TextReplacer.HasChinese("变身") && !TextReplacer.HasChinese("변신 abc") && !TextReplacer.HasChinese("") && !TextReplacer.HasChinese(null) && TextReplacer.HasChinese("a㐀b"));
            Check("zh: whole match wins even for one char", zh.Replace("无", out changed) == "없음" && changed);
            Check("zh: one-char key is not used for partial replace", zh.Replace("无敌", out changed) == "无敌" && !changed);
            Check("zh: longer key first", zh.Replace("变身技-幽莹（未解锁）", out changed) == "변신기-유영(미해금)" && changed, zh.Replace("变身技-幽莹（未解锁）", out changed));
            Check("zh: placeholders kept", zh.Replace("体力{icon_tili}*<y>100</y>%，幽莹", out changed) == "체력{icon_tili}*<y>100</y>%，유영" && changed);
            string same = "변신기";
            Check("zh: text without CJK returns same instance", object.ReferenceEquals(zh.Replace(same, out changed), same) && !changed);
            Check("zh: null and empty pass through", zh.Replace(null, out changed) == null && !changed && zh.Replace("", out changed) == "" && !changed);
            Check("zh: count and empty map", zh.Count == 6 && new TextReplacer(new Dictionary<string, string>()).Replace("变身", out changed) == "变身" && !changed);
            // 부분 치환은 한자가 하나도 남지 않을 때만 쓴다(반쯤 바뀐 글을 보이지 않기 위해. 회차 zh-1017b에서 "苏烬棠"→"苏신당", 게임 패치 노트의 "筑基境"만 바뀜).
            Check("zh: partial result is dropped when CJK remains", zh.Replace("变身技-幽莹（未解锁）未知", out changed) == "变身技-幽莹（未解锁）未知" && !changed);
            // 종족 틀 DLL의 글은 줄바꿈을 두 글자 "\n"으로 넣고 TMP가 줄을 바꾼다(회차 zh-1017e: 변신 설명이 그대로 남음). 줄바꿈이 든 원문은 두 글자 꼴로도 맞춘다.
            var nl = new TextReplacer(new Dictionary<string, string> { { "秒\n变身效果：", "초\n변신 효과：" }, { "幽莹", "유영" } });
            Check("zh: a key with a newline also matches the two-char \\n form", nl.Replace("1秒\\n变身效果：幽莹", out changed) == "1초\\n변신 효과：유영" && changed && nl.Replace("1秒\n变身效果：幽莹", out changed) == "1초\n변신 효과：유영" && changed);
            Check("zh: a key with a newline also matches CRLF", nl.Replace("1秒\r\n变身效果：幽莹", out changed) == "1초\r\n변신 효과：유영" && changed);
            // 전체 일치 전용 사전(LocalText의 ch→kr. 회차 zh-1017c: 불러오기 화면 모드가 원문 ch를 그대로 보여 준다): 글 전체가 같을 때만, 부분 치환에는 쓰지 않는다.
            var lt = new TextReplacer(new Dictionary<string, string> { { "幽莹", "유영" } }, new Dictionary<string, string> { { "修炼技能需要", "기술 수련" } });
            Check("zh: whole-only dictionary matches the whole text only", lt.Replace("修炼技能需要", out changed) == "기술 수련" && changed && lt.Replace("修炼技能需要幽莹", out changed) == "修炼技能需要幽莹" && !changed && lt.Count == 1 && lt.WholeOnlyCount == 1);
            // Whole: 표 칸 바꿔 끼우기(TextTranslationService.ApplyTable)가 쓴다. 글 전체가 원문과 같을 때만 한국어, 아니면 null(부분 치환 없음).
            Check("zh: Whole returns the value only for an exact key", zh.Whole("无") == "없음" && zh.Whole("幽莹") == "유영" && zh.Whole("变身技-幽莹（未解锁）") == null && zh.Whole("") == null && zh.Whole(null) == null);
        }

        private static string Lines(List<CraftPlan.Line> lines)
        {
            var parts = new List<string>();
            foreach (CraftPlan.Line l in lines)
            {
                parts.Add(l.Id == 0 ? "[" + l.Note + "]" : l.Id + "x" + l.Count);
            }
            return string.Join(",", parts);
        }

        private static int[] E(int index, int value)
        {
            var v = new int[8];
            v[index] = value;
            return v;
        }

        private static BagPlan.Stack St(string sole, int count, bool equipped, int strength = 0)
        {
            return new BagPlan.Stack { Sole = sole, Count = count, Equipped = equipped, Strength = strength };
        }

        private static string Cuts(List<BagPlan.Cut> cuts)
        {
            var parts = new List<string>();
            foreach (BagPlan.Cut c in cuts)
            {
                parts.Add(c.Sole + (c.All ? " All" : " x" + c.Count));
            }
            return string.Join(",", parts);
        }

        private static string Names(List<string> list)
        {
            return string.Join(",", list);
        }

        // 새 기본 프리셋 풀기(docs/superpowers/specs/2026-10-20-preset-drafts-design.md 6.1절).
        private static void PresetDefaultsTests()
        {
            var legacy = new List<string>(PresetDefaults.Legacy);
            var all = new List<string>(legacy) { "11-x.json" };
            string a = Names(PresetDefaults.ToWrite(new List<string> { "01-풍검.json", "11-x.json" }, new List<string> { "01-풍검.json" }, new List<string>()));
            Check("defaults new name only", a == "11-x.json", a);
            string b = Names(PresetDefaults.ToWrite(new List<string> { "01-풍검.json", "11-x.json" }, new List<string> { "01-풍검.json", "11-x.json" }, new List<string>()));
            Check("defaults deleted one stays deleted", b == "", b);
            string c = Names(PresetDefaults.ToWrite(all, null, new List<string> { "02-수검.json" }));
            Check("defaults old folder unpacks only new ones", c == "11-x.json", c);
            string d = Names(PresetDefaults.ToWrite(new List<string> { "11-x.json" }, new List<string>(), new List<string> { "11-x.json" }));
            Check("defaults a file on disk is not written", d == "", d);
            Check("defaults legacy has ten names", PresetDefaults.Legacy.Length == 10 && legacy.Contains("07-창-과룡문.json"));
        }

        // 성격 칸 검사(docs/superpowers/specs/2026-10-20-trainer-batch1-design.md 6절, 05 문서 65절: 표 type 1 내재·2 외재).
        private static void TraitRulesTests()
        {
            Check("traits in ok", TraitRules.Check(TraitRules.Slot.In, 2, 1, 0) == null);
            string a = TraitRules.Check(TraitRules.Slot.In, 9, 2, 0);
            Check("traits in rejects outer", a == "내재 칸에는 내재 성격만 넣을 수 있습니다", a);
            Check("traits out ok", TraitRules.Check(TraitRules.Slot.Out1, 10, 2, 15) == null);
            string b = TraitRules.Check(TraitRules.Slot.Out1, 15, 2, 15);
            Check("traits out same", b == "다른 외재 칸과 같은 성격입니다", b);
            string c = TraitRules.Check(TraitRules.Slot.Out2, 3, 1, 8);
            Check("traits out rejects inner", c == "외재 칸에는 외재 성격만 넣을 수 있습니다", c);
            string d = TraitRules.Check(TraitRules.Slot.In, 99, 0, 0);
            Check("traits unknown", d == "표에 없는 성격입니다", d);
        }

        // 가방 줄이기 순서(docs/superpowers/specs/2026-10-20-trainer-batch1-design.md 6절, 05 문서 65절).
        private static void BagPlanTests()
        {
            int kept;
            string a = Cuts(BagPlan.Reduce(new List<BagPlan.Stack> { St("A", 10, false) }, true, 4, out kept));
            Check("bagplan overlay plain", a == "A x4" && kept == 0, a + " kept=" + kept);
            string b = Cuts(BagPlan.Reduce(new List<BagPlan.Stack> { St("A", 10, false) }, true, 10, out kept));
            Check("bagplan overlay all", b == "A All" && kept == 0, b + " kept=" + kept);
            string c = Cuts(BagPlan.Reduce(new List<BagPlan.Stack> { St("A", 7, true) }, true, 7, out kept));
            Check("bagplan overlay equipped keeps one", c == "A x6" && kept == 1, c + " kept=" + kept);
            string d = Cuts(BagPlan.Reduce(new List<BagPlan.Stack> { St("E", 5, true), St("F", 3, false) }, true, 5, out kept));
            Check("bagplan overlay free first", d == "F All,E x2" && kept == 0, d + " kept=" + kept);
            var arts = new List<BagPlan.Stack> { St("X", 1, false, 9), St("Y", 1, false, 3), St("Z", 1, true, 1) };
            string e = Cuts(BagPlan.Reduce(arts, false, 2, out kept));
            Check("bagplan artifact weak first", e == "Y All,X All" && kept == 0, e + " kept=" + kept);
            string f = Cuts(BagPlan.Reduce(arts, false, 3, out kept));
            Check("bagplan artifact equipped kept", f == "Y All,X All" && kept == 1, f + " kept=" + kept);
            int strong = BagPlan.StrongCount(new List<BagPlan.Stack> { St("X", 1, false, 9), St("Y", 1, false, 3), St("W", 1, false, 3) });
            Check("bagplan strong count", strong == 1, "strong=" + strong);
        }

        // 기예 재료 한 벌(docs/superpowers/specs/2026-10-18-craft-materials-design.md 6절, 05 문서 61절의 실측 값).
        private static void CraftPlanTests()
        {
            // 오행 순서 화0 수1 뇌2 풍3 토4 목5 음6 양7
            var elems = new Dictionary<int, int[]>
            {
                { 5031061, E(5, 5) }, { 5031233, E(5, 10) }, { 5031122, new[] { 0, 0, 5, 5, 0, 0, 0, 0 } },
            };
            Func<int, int[]> of = id => elems.ContainsKey(id) ? elems[id] : new int[8];
            var herbs = new List<CraftPlan.Herb>
            {
                new CraftPlan.Herb { Id = 5031324, Level = 4, Elements = E(2, 20) },
                new CraftPlan.Herb { Id = 5031102, Level = 2, Elements = E(2, 5) },
                new CraftPlan.Herb { Id = 5031112, Level = 2, Elements = E(2, 10) },
                new CraftPlan.Herb { Id = 5031041, Level = 2, Elements = E(4, 5) },
                new CraftPlan.Herb { Id = 5031061, Level = 2, Elements = E(5, 5) },
                new CraftPlan.Herb { Id = 5031122, Level = 2, Elements = new[] { 0, 0, 5, 5, 0, 0, 0, 0 } },
            };
            string a = Lines(CraftPlan.PillSet(new[] { 0, 0, 150, 0, 0, 250, 0, 0 }, new List<int> { 5031061 }, new List<int> { 50 }, of, herbs));
            Check("pill: main herbs and the lei deficit", a == "5031061x50,5031112x15", a);
            string b = Lines(CraftPlan.PillSet(new[] { 0, 0, 300, 100, 200, 600, 0, 0 }, new List<int> { 5031233, 5031122 }, new List<int> { 40, 20 }, of, herbs));
            Check("pill: two mains and three deficits", b == "5031233x40,5031122x20,5031112x20,5031041x40,5031061x40", b);
            string c = Lines(CraftPlan.PillSet(new[] { 0, 0, 30, 0, 0, 0, 0, 0 }, new List<int>(), new List<int>(), of, herbs));
            Check("pill: empty main list", c == "5031112x3", c);
            string d = Lines(CraftPlan.PillSet(new[] { 0, 0, 0, 0, 0, 0, 50, 0 }, new List<int>(), new List<int>(), of, herbs));
            Check("pill: a missing element herb is reported", d.Contains("음 50 (채울 약재 없음)"), d);
            string k = Lines(CraftPlan.Times(new List<CraftPlan.Line> { new CraftPlan.Line { Id = 5031061, Count = 300 }, new CraftPlan.Line { Note = "x" } }, 10));
            Check("pill: k multiplies counts", k == "5031061x3000,[x]", k);

            var furnaces = new List<CraftPlan.Candidate>
            {
                new CraftPlan.Candidate { Id = 6061021, Mixed = 20 }, new CraftPlan.Candidate { Id = 6061011, Mixed = 10 },
            };
            Check("furnace: lowest usable", CraftPlan.PickFurnace(furnaces, 102) == 6061011);
            Check("furnace: none usable", CraftPlan.PickFurnace(furnaces, 0) == 0);

            var mats = new List<CraftPlan.Candidate>
            {
                new CraftPlan.Candidate { Id = 5231101, Class = 523, Mixed = 15 }, new CraftPlan.Candidate { Id = 5231105, Class = 523, Mixed = 5 },
                new CraftPlan.Candidate { Id = 5251104, Class = 525, Mixed = 6 }, new CraftPlan.Candidate { Id = 9999, Class = 600, Mixed = 1 },
            };
            var fires = new List<CraftPlan.Candidate>
            {
                new CraftPlan.Candidate { Id = 5221102, Level = 2 }, new CraftPlan.Candidate { Id = 5221101, Level = 1 },
            };
            string f = Lines(CraftPlan.ForgeSet(5121111, 20, new List<int> { 525, 523 }, mats, fires));
            Check("forge: exact count of the purest", f == "5121111x1,5231105x20,5221101x1", f);
            string g = Lines(CraftPlan.ForgeSet(5121111, 20, new List<int> { 700 }, mats, fires));
            Check("forge: no candidate", g.Contains("(보조 재료를 찾지 못함)") && g.StartsWith("5121111x1"), g);

            string r = Lines(CraftPlan.RuneSet("5021164|5021674", 2));
            Check("rune: first of the pair", r == "5021164x2", r);

            var set = new List<CraftPlan.Line> { new CraftPlan.Line { Id = 1, Count = 5 }, new CraftPlan.Line { Id = 2, Count = 1 }, new CraftPlan.Line { Id = 3, Count = 9 }, new CraftPlan.Line { Note = "n" } };
            Check("slots: only new ids count", CraftPlan.NewSlots(set, id => id == 1, id => true) == 2);
            // 겹쳐지지 않는 재료(연기 핵심 재료 className 512, isOverlay 0)는 한 개가 한 칸이고, 이미 가방에 있어도 칸이 더 든다(리뷰 Important 1).
            var forge10 = new List<CraftPlan.Line> { new CraftPlan.Line { Id = 5121111, Count = 10 }, new CraftPlan.Line { Id = 5251001, Count = 200 } };
            Check("slots: a non-stacking item needs one slot per piece", CraftPlan.NewSlots(forge10, id => false, id => id != 5121111) == 11);
            Check("slots: a non-stacking item already in the bag still needs slots", CraftPlan.NewSlots(forge10, id => true, id => id != 5121111) == 10);
        }
    }
}
