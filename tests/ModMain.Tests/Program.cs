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
            Check("zh: placeholders kept", zh.Replace("体力{icon_tili}*<y>100</y>%，灵力", out changed) == "체력{icon_tili}*<y>100</y>%，灵力" && changed);
            string same = "변신기";
            Check("zh: text without CJK returns same instance", object.ReferenceEquals(zh.Replace(same, out changed), same) && !changed);
            Check("zh: null and empty pass through", zh.Replace(null, out changed) == null && !changed && zh.Replace("", out changed) == "" && !changed);
            Check("zh: count and empty map", zh.Count == 6 && new TextReplacer(new Dictionary<string, string>()).Replace("变身", out changed) == "变身" && !changed);
            // Whole: 표 칸 바꿔 끼우기(TextTranslationService.ApplyTable)가 쓴다. 글 전체가 원문과 같을 때만 한국어, 아니면 null(부분 치환 없음).
            Check("zh: Whole returns the value only for an exact key", zh.Whole("无") == "없음" && zh.Whole("幽莹") == "유영" && zh.Whole("变身技-幽莹（未解锁）") == null && zh.Whole("") == null && zh.Whole(null) == null);
        }
    }
}
