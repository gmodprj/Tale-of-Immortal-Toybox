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
            TextTagsTests();
            FateAltTests();
            LocalTextRulesTests();
            ModSkillRulesTests();
            FileStampTests();
            StampCacheTests();
            GiveBudgetTests();
            ApplyTallyTests();
            ConfigFileTests();
            FavoriteLinesTests();
            TreasureRulesTests();
            MasteryRulesTests();
            KoreanTextTests();
            RateGroupsTests();
            NumberFieldRulesTests();
            RateUnitRulesTests();
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
            // 앞뒤 서식 태그를 벗겨 안쪽 글로 찾는다(만고신화 선천 이름 <COLOR=#D6A300>万古不朽</COLOR>. 안쪽 万古不朽은 LocalText 사전에 있다).
            var tag = new TextReplacer(new Dictionary<string, string> { { "幽莹", "유영" } }, new Dictionary<string, string> { { "万古不朽", "만고불후" } });
            Check("zh: Whole finds the text inside outer tags", tag.Whole("<COLOR=#D6A300>万古不朽</COLOR>") == "<COLOR=#D6A300>만고불후</COLOR>" && tag.Whole("<B><size=20>幽莹</size></B>") == "<B><size=20>유영</size></B>");
            Check("zh: Whole keeps an exact key with tags first and does not peel inner tags", new TextReplacer(new Dictionary<string, string> { { "<r>幽莹</r>", "<r>유영!</r>" }, { "幽莹", "유영" } }).Whole("<r>幽莹</r>") == "<r>유영!</r>" && tag.Whole("<r>万古</r>不朽") == null && tag.Whole("<COLOR=#D6A300></COLOR>") == null);
            // 만고신화는 꺾쇠 <…>를 지문 표시로도 쓴다(<此人突然出现…>). 영문자로 시작하는 서식 태그만 벗긴다(리뷰 I1).
            var aside = new TextReplacer(new Dictionary<string, string>(), new Dictionary<string, string> { { "你好", "안녕" } });
            Check("zh: Whole peels only formatting tags, not angle-bracket narration", aside.Whole("你好<……必须小心应对>") == null && aside.Whole("<旁白>你好") == null && aside.Whole("<b>你好</b>") == "<b>안녕</b>");
            Check("zh: Replace uses the peeled whole match too", tag.Replace("<COLOR=#D6A300>万古不朽</COLOR>", out changed) == "<COLOR=#D6A300>만고불후</COLOR>" && changed);
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

        // 배율 탭 확장(05 문서 86절): 칩 분류와 정력 돌려주기
        private static void RateGroupsTests()
        {
            string[] all = { "exp", "martial", "move", "actionDay", "speed", "cooldown", "artifactCooldown", "mpCost", "spCost", "studyGain", "itemUse", "taken",
                "mapSpeed", "battleMove", "intimGain", "energyCost", "multiplier", "god", "itemCooldown", "artifactDurable" };
            var seen = new System.Collections.Generic.Dictionary<string, int>();
            foreach (RateGroups.Group group in RateGroups.Groups)
            {
                foreach (string key in group.Keys)
                {
                    int n;
                    seen.TryGetValue(key, out n);
                    seen[key] = n + 1;
                }
            }
            bool once = seen.Count == all.Length;
            foreach (string key in all)
            {
                int n;
                once = once && seen.TryGetValue(key, out n) && n == 1;
            }
            Check("rate groups: every row is in exactly one chip", once, "rows=" + seen.Count);
            Check("rate groups: new rows are in their chips", RateGroups.GroupOf("mapSpeed") == 1 && RateGroups.GroupOf("battleMove") == 1 && RateGroups.GroupOf("intimGain") == 3 && RateGroups.GroupOf("energyCost") == 3);
            Check("rate groups: five chips, the last is on", RateGroups.Count == 5 && RateGroups.NameOf(4) == RateGroups.OnName);
            System.Func<string, bool> on = k => k == "god" || k == "exp";
            var onKeys = RateGroups.KeysOf(4, on);
            Check("rate groups: the on chip lists only rows that are on, in chip order", onKeys.Count == 2 && onKeys[0] == "exp" && onKeys[1] == "god");
            Check("rate groups: on counts per chip", RateGroups.OnCount(0, on) == 1 && RateGroups.OnCount(1, on) == 0 && RateGroups.OnCount(2, on) == 1 && RateGroups.OnCount(4, on) == 2);
            Check("rate groups: battle chip keeps eight rows", RateGroups.Groups[2].Keys.Length == 8 && RateGroups.GroupOf("itemCooldown") == 2);
            Check("rate groups: artifact durable is in life", RateGroups.GroupOf("artifactDurable") == 3);
            Check("skill cooldown: half rounds away", RateGroups.SkillCooldown(60000, 0.5f) == 30000 && RateGroups.SkillCooldown(250, 0.5f) == 125 && RateGroups.SkillCooldown(150, 0.5f) == 100);
            Check("skill cooldown: none is the floor", RateGroups.SkillCooldown(190000, 0f) == 100);
            Check("skill cooldown: a short one is not lengthened", RateGroups.SkillCooldown(50, 0f) == 50 && RateGroups.SkillCooldown(50, 0.5f) == 50);
            Check("skill cooldown: zero or less is kept", RateGroups.SkillCooldown(0, 0f) == 0 && RateGroups.SkillCooldown(-10000, 0.5f) == -10000);
            Check("skill cooldown: x1 keeps", RateGroups.SkillCooldown(365, 1f) == 365);
            Check("one hit: hp plus max", RateGroups.OneHitDamage(29877, 29877) == 59754);
            Check("one hit: overflow is cut", RateGroups.OneHitDamage(int.MaxValue, 5) == int.MaxValue);
            Check("one hit: never zero", RateGroups.OneHitDamage(0, 0) == 1);
            // 일격의 대상(13 문서 2.6절): 플레이어의 타격만, 플레이어·동료(PlayerNPC)를 맞힌 것은 제외.
            Check("onehit: player hitting a monster applies", RateGroups.OneHitApplies(1, 3));
            Check("onehit: player hitting an ally does not", !RateGroups.OneHitApplies(1, 2));
            Check("onehit: player hitting the player does not", !RateGroups.OneHitApplies(1, 1));
            Check("onehit: a monster hitting anything does not", !RateGroups.OneHitApplies(3, 3) && !RateGroups.OneHitApplies(4, 1));
            // 배율 수치 함수(스펙 2026-10-30-rate-service-cleanup 3절). 반올림 방식이 함수마다 다른 것은 기존 동작 그대로다.
            Check("scale value: banker's rounding and cap", RateGroups.ScaleValue(5, 0.5f) == 2 && RateGroups.ScaleValue(7, 0.5f) == 4 && RateGroups.ScaleValue(int.MaxValue, 2f) == int.MaxValue && RateGroups.ScaleValue(0, 2f) == 0 && RateGroups.ScaleValue(-3, 2f) == -3);
            Check("scale day: half rounds away from zero", RateGroups.ScaleDay(5, 0.5f) == 3 && RateGroups.ScaleDay(1, 0.25f) == 0 && RateGroups.ScaleDay(0, 0.5f) == 0);
            Check("scale skill value: 25 at half is 13", RateGroups.ScaleSkillValue(25, 0.5f) == 13 && RateGroups.ScaleSkillValue(-1, 0.5f) == -1);
            Check("cooldown rate value: capped at max", RateGroups.CooldownRateValue(100, 10000f, 100000000) == 1000000 && RateGroups.CooldownRateValue(100, 2000000f, 100000000) == 100000000 && RateGroups.CooldownRateValue(0, 2f, 100) == 0);
            Check("taken damage: never below 1", RateGroups.TakenDamage(1, 0.5f) == 1 && RateGroups.TakenDamage(10, 0.25f) == 2 && RateGroups.TakenDamage(0, 0.5f) == 0);
            Check("dealt damage: negative untouched, overflow capped", RateGroups.DealtDamage(-5, 100) == -5 && RateGroups.DealtDamage(0, 100) == 0 && RateGroups.DealtDamage(30, 10) == 300 && RateGroups.DealtDamage(int.MaxValue, 2) == int.MaxValue);
            Check("durable: only a drop on an owned artifact is blocked", RateGroups.DurableBlocked(100, 98, true, true) && !RateGroups.DurableBlocked(100, 98, false, true) && !RateGroups.DurableBlocked(98, 100, true, true) && !RateGroups.DurableBlocked(100, 98, true, false));
            Check("cooldown rate: an off row that was never applied is not written", !RateGroups.WritesRate(false, false));
            Check("cooldown rate: an on row is written, and an off row once to restore", RateGroups.WritesRate(true, false) && RateGroups.WritesRate(true, true) && RateGroups.WritesRate(false, true));
            Check("energy refund: half gives half back", RateGroups.EnergyRefund(74, 64, 0.5f) == 5);
            Check("energy refund: none gives all back", RateGroups.EnergyRefund(74, 64, 0f) == 10);
            Check("energy refund: x1 and a rise give nothing", RateGroups.EnergyRefund(74, 64, 1f) == 0 && RateGroups.EnergyRefund(64, 74, 0f) == 0 && RateGroups.EnergyRefund(5, 5, 0f) == 0);
            Check("energy refund: the kept cost rounds up", RateGroups.EnergyRefund(10, 5, 0.5f) == 2 && RateGroups.EnergyRefund(10, 7, 0.25f) == 2);
            Check("energy refund: a cost of one is not free at x0.5 or x0.25", RateGroups.EnergyRefund(5, 4, 0.5f) == 0 && RateGroups.EnergyRefund(5, 4, 0.25f) == 0 && RateGroups.EnergyRefund(5, 4, 0f) == 1);
        }

        // 미룬 것 묶음(05 문서 85절): 로그의 을/를
        private static void KoreanTextTests()
        {
            Check("object after a final consonant", KoreanText.Object("번역 목록") == "번역 목록을");
            Check("object after no final consonant", KoreanText.Object("창 위치") == "창 위치를");
            Check("object after ieung", KoreanText.Object("신기 단축키 설정") == "신기 단축키 설정을");
            Check("object after rieul", KoreanText.Object("물") == "물을" && KoreanText.Object("설정 파일") == "설정 파일을");
            Check("object after latin", KoreanText.Object("config") == "config을(를)");
            Check("object after a digit", KoreanText.Object("목록 3") == "목록 3을(를)");
            Check("object of empty and null", KoreanText.Object("") == "을(를)" && KoreanText.Object(null) == "을(를)");
        }

        // 새 기본 프리셋 풀기(docs/superpowers/specs/2026-10-20-preset-drafts-design.md 6.1절).
        // 모드 글의 서식 태그 지우기(05 문서 72절: 만고신화 기술 이름 <COLOR=#FFAA00>草字剑诀</COLOR>가 COLORFFAA00草字剑诀COLOR로 보였다).
        private static void TextTagsTests()
        {
            Check("search key drops spaces and lowers", TextTags.SearchKey(" Fire Ball 화 염 ") == "fireball화염");
            Check("search key of null", TextTags.SearchKey(null) == "");
            string a = TextTags.Strip("<COLOR=#FFAA00>草字剑诀</COLOR>");
            Check("tags color removed", a == "草字剑诀", a);
            string b = TextTags.Strip("<size=25>[无邪</size>]<b><color=#800000>万古神话</color></b>");
            Check("tags nested removed", b == "[无邪]万古神话", b);
            string c = TextTags.Strip("a < b > c");
            Check("tags spaced angle kept", c == "a < b > c", c);
            Check("tags null is empty", TextTags.Strip(null) == "");
            string d = TextTags.Strip("윤회지");
            Check("tags plain kept", d == "윤회지", d);
            // 이름 다듬기(EditService.Compact가 쓴다): 태그를 먼저 지우고 글자·숫자만(사용자 보고: 공법 이름에 COLORFFAA00…COLOR가 남음)
            string e = TextTags.CompactName("일운<COLOR=#FFAA00>황무검결</COLOR>");
            Check("compact name drops color tags", e == "일운황무검결", e);
            string f = TextTags.CompactName("역 궁 청풍검");
            Check("compact name drops spaces", f == "역궁청풍검", f);
            Check("compact name of null", TextTags.CompactName(null) == "");
            string g = TextTags.CompactName("<size=25>[无邪</size>]<b>万古神话</b>");
            Check("compact name drops brackets and tags", g == "无邪万古神话", g);
        }

        // 프리셋 역천개명 줄의 대체 번호(05 문서 73절: 만고신화는 혈염대법 등을 경지 1~1로 막고 탄천마공 등을 연다).
        // Pick(경지, 원래 번호·범위·있음, 대체 번호·범위·있음) → 쓸 번호. 원래 것을 쓸 수 있으면 원래 것, 아니면 쓸 수 있는 대체, 둘 다 안 되면 원래 것.
        private static void FateAltTests()
        {
            Check("alt original usable", FateAlt.Pick(5, 700056, 5, 10, true, 700028, 2, 10, true) == 700056);
            Check("alt used when original is blocked", FateAlt.Pick(5, 700056, 1, 1, true, 700028, 2, 10, true) == 700028);
            Check("alt not in range keeps original", FateAlt.Pick(2, 700061, 1, 1, true, 700050, 3, 10, true) == 700061);
            Check("alt missing keeps original", FateAlt.Pick(5, 700056, 1, 1, true, 700028, 2, 10, false) == 700056);
            Check("no alt keeps original", FateAlt.Pick(5, 700056, 1, 1, true, 0, 0, 0, false) == 700056);
            Check("alt used when original is missing", FateAlt.Pick(5, 700056, 0, 0, false, 700028, 2, 10, true) == 700028);
            Check("original usable at its lowest grade", FateAlt.Pick(1, 700056, 1, 1, true, 700028, 2, 10, true) == 700056);
            Check("original usable at its highest grade", FateAlt.Pick(10, 700056, 5, 10, true, 700028, 2, 10, true) == 700056);
            Check("alt used at the edges of its range", FateAlt.Pick(11, 700056, 5, 10, true, 700028, 2, 11, true) == 700028 && FateAlt.Pick(2, 700056, 5, 10, true, 700028, 2, 11, true) == 700028);
        }

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
        private static void FavoriteLinesTests()
        {
            var limits = new FavoriteStore.Limits { Kinds = 5, MaxAmount = 9999, MaxBook = 20 };
            var lines = new[] { "# 주석", "", "1011011 10", "book 0 14111 3 1 2", "1011011 5", "bad line", "book 9 1 1 1 1", "book 1 22111 11 1 1", "2000 0", "3000 99999", "book 4 32111 2 6 50", "\t4000\t7 " };
            int skipped;
            List<FavoriteStore.Favorite> read = FavoriteStore.ReadLines(lines, limits, out skipped);
            Check("fav lines: kept rows", read.Count == 6, "count=" + read.Count);
            Check("fav lines: skipped broken and duplicate", skipped == 4, "skipped=" + skipped);
            Check("fav lines: item row", read[0].Id == 1011011 && read[0].Amount == 10 && !read[0].Book);
            Check("fav lines: book row", read[1].Book && read[1].BookKind == 0 && read[1].Id == 14111 && read[1].Grade == 3 && read[1].MinLevel == 1 && read[1].Amount == 2);
            Check("fav lines: amount clamped up to 1", read[2].Id == 2000 && read[2].Amount == 1);
            Check("fav lines: amount clamped down", read[3].Amount == 9999);
            Check("fav lines: book amount clamped", read[4].Amount == 20);
            Check("fav lines: tabs separate", read[5].Id == 4000 && read[5].Amount == 7);
            string text = FavoriteStore.WriteLines(read);
            int skipped2;
            List<FavoriteStore.Favorite> again = FavoriteStore.ReadLines(text.Split('\n'), limits, out skipped2);
            bool same = again.Count == read.Count && skipped2 == 0;
            for (int i = 0; same && i < read.Count; i++) { same = again[i].Same(read[i]) && again[i].Amount == read[i].Amount && again[i].BookKind == read[i].BookKind; }
            Check("fav lines: written text reads back the same", same, text);
            Check("fav lines: header comment first", text.StartsWith("# TlToybox 자원 탭 즐겨찾기"));
        }

        private static void ConfigFileTests()
        {
            var lines = new[] { "# 편집기를 여닫는 키", "hotkey=F2", "  Window = 10, 20 ", "artifactkey1=Alpha1", "broken line", "window=30,40", "" };
            Dictionary<string, string> map = ConfigFile.Parse(lines);
            Check("config: comment and broken lines skipped", !map.ContainsKey("# 편집기를 여닫는 키") && !map.ContainsKey("broken line"));
            Check("config: key is case-insensitive", map.ContainsKey("WINDOW") && map.ContainsKey("hotkey"));
            Check("config: the last line wins", map["window"] == "30,40", map["window"]);
            Check("config: values are trimmed", map["artifactkey1"] == "Alpha1");
            List<string> written = ConfigFile.Replace(lines, new Dictionary<string, string> { { "window", "1,2" }, { "artifactgamekey", "off" } });
            Check("config: other lines stay in order", written[0] == "# 편집기를 여닫는 키" && written[1] == "hotkey=F2" && written[2] == "artifactkey1=Alpha1" && written[3] == "broken line");
            Check("config: replaced keys go to the end once", written.FindAll(l => l.StartsWith("window=")).Count == 1 && written[written.Count - 2] == "window=1,2" && written[written.Count - 1] == "artifactgamekey=off", string.Join("|", written.ToArray()));
            Check("config: replace from nothing", ConfigFile.Replace(new string[0], new Dictionary<string, string> { { "hotkey", "Insert" } })[0] == "hotkey=Insert");
        }

        private static void ApplyTallyTests()
        {
            var tally = new ApplyTally();
            Check("tally: empty has nothing", !tally.Any);
            tally.Count("가", (out string text) => { text = null; return ApplyOutcome.Done; });
            tally.Count("나", (out string text) => { text = null; return ApplyOutcome.Skipped; });
            tally.Count("다", (out string text) => { text = "다: 이유."; return ApplyOutcome.Failed; });
            tally.Count("라", (out string text) => { throw new InvalidOperationException("x"); });
            tally.Count("마", (out string text) => { text = ""; return ApplyOutcome.Failed; });
            Check("tally: any", tally.Any);
            Check("tally: sentence", tally.Sentence("공법", "장착") == "공법: 장착 1, 건너뜀 1, 못 넣음 3(다: 이유 / 라: 게임이 받지 않았습니다(InvalidOperationException) / 마).", tally.Sentence("공법", "장착"));
            var many = new ApplyTally();
            for (int i = 1; i <= 7; i++) { int n = i; many.Count("x" + n, (out string text) => { text = null; return ApplyOutcome.Failed; }); }
            Check("tally: more than five failures", many.Sentence("운명", "넣음") == "운명: 넣음 0, 건너뜀 0, 못 넣음 7(x1 / x2 / x3 / x4 / x5 외 2개).", many.Sentence("운명", "넣음"));
        }

        private static void GiveBudgetTests()
        {
            Check("budget: under 2s keeps going", !GiveBudget.ShouldStop(1999, 3, 20));
            Check("budget: at 2s stops", GiveBudget.ShouldStop(2000, 3, 20));
            Check("budget: all made never stops", !GiveBudget.ShouldStop(5000, 20, 20));
            Check("budget: first book always tried", !GiveBudget.ShouldStop(5000, 0, 20));
            Check("budget: stop text", GiveBudget.StopText(3, 20) == "시간 상한(2초)에 걸려 3권만 만들었습니다(요청 20권). 다시 누르세요");
        }

        private static void StampCacheTests()
        {
            string stamp = "s1";
            int loads = 0;
            var cache = new StampCache<string>();
            Func<string> load = () => { loads++; return "v" + loads; };
            Check("stampcache: first call loads", cache.Get(() => stamp, load) == "v1" && loads == 1);
            Check("stampcache: same stamp reuses", cache.Get(() => stamp, load) == "v1" && loads == 1);
            // 읽는 도중에 밖에서 파일이 바뀌면(표지가 load 안에서 바뀜) 다음 호출은 다시 읽어야 한다(최종 리뷰 Important).
            Func<string> changingLoad = () => { loads++; stamp = "s3"; return "v" + loads; };
            stamp = "s2";
            cache.Get(() => stamp, changingLoad);
            Check("stampcache: a change during load is seen next time", cache.Get(() => stamp, load) == "v3" && loads == 3, "loads=" + loads);
            cache.Forget();
            Check("stampcache: forget reloads", cache.Get(() => stamp, load) == "v4");
        }

        private static void FileStampTests()
        {
            string dir = Path.Combine(Path.GetTempPath(), "tlt-stamp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string a = Path.Combine(dir, "a.json"), b = Path.Combine(dir, "b.json");
                File.WriteAllText(a, "{\"x\":1}");
                File.WriteAllText(b, "{\"y\":2}");
                string first = FileStamp.Of(dir, "*.json");
                Check("stamp: same twice", first == FileStamp.Of(dir, "*.json"));
                File.WriteAllText(Path.Combine(dir, "c.txt"), "other");
                Check("stamp: other pattern ignored", first == FileStamp.Of(dir, "*.json"));
                File.AppendAllText(a, "\n");
                string grown = FileStamp.Of(dir, "*.json");
                Check("stamp: appended line changes it", grown != first);
                DateTime t = File.GetLastWriteTimeUtc(b);
                File.WriteAllText(b, "{\"y\":3}");
                File.SetLastWriteTimeUtc(b, t.AddSeconds(1));
                string edited = FileStamp.Of(dir, "*.json");
                Check("stamp: same size edit with newer time changes it", edited != grown);
                File.Delete(b);
                Check("stamp: deleting a file changes it", FileStamp.Of(dir, "*.json") != edited);
                Check("stamp: missing folder is -", FileStamp.Of(Path.Combine(dir, "nope"), "*.json") == "-");
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        private static void ModSkillRulesTests()
        {
            Check("literal: plain chinese name", ModSkillRules.IsLiteralName("개자비"));
            Check("literal: name with color tag", ModSkillRules.IsLiteralName("<COLOR=#FFAA00>용유사방</COLOR>"));
            Check("literal: key shape is not a name", !ModSkillRules.IsLiteralName("skill_name_123"));
            Check("literal: \"0\"", !ModSkillRules.IsLiteralName("0"));
            Check("literal: empty", !ModSkillRules.IsLiteralName(""));
            Check("literal: null", !ModSkillRules.IsLiteralName(null));
            Check("literal: placeholder", !ModSkillRules.IsLiteralName("{#jlname}"));
            Check("literal: text with placeholder", !ModSkillRules.IsLiteralName("abc{x}"));
            Check("blank: \"0\" and empty desc", ModSkillRules.IsBlankRow("0", ""));
            Check("blank: empty and null desc", ModSkillRules.IsBlankRow("", null));
            Check("blank: \"0\" and \"0\"", ModSkillRules.IsBlankRow("0", "0"));
            Check("blank: has desc", !ModSkillRules.IsBlankRow("0", "설명"));
            Check("blank: has name key", !ModSkillRules.IsBlankRow("skill_1", ""));
            int min, max;
            ModSkillRules.BookGrades(0, 5, 35, out min, out max);
            Check("books: martial art rows 5 -> 2~10", min == 2 && max == 10, min + "~" + max);
            ModSkillRules.BookGrades(0, 14, 98, out min, out max);
            Check("books: martial art rows 14 -> 2~10", min == 2 && max == 10, min + "~" + max);
            ModSkillRules.BookGrades(1, 5, 35, out min, out max);
            Check("books: skill rows 5 -> 1~10", min == 1 && max == 10, min + "~" + max);
            ModSkillRules.BookGrades(4, 5, 35, out min, out max);
            Check("books: ultimate rows 5 -> 1~10", min == 1 && max == 10, min + "~" + max);
            ModSkillRules.BookGrades(2, 5, 35, out min, out max);
            Check("books: step never", min == 0 && max == 0, min + "~" + max);
            ModSkillRules.BookGrades(3, 0, 0, out min, out max);
            Check("books: ability without rows never", min == 0 && max == 0, min + "~" + max);
            ModSkillRules.BookGrades(1, 0, 0, out min, out max);
            Check("books: no option rows never", min == 0 && max == 0, min + "~" + max);
            ModSkillRules.BookGrades(1, 2, 2, out min, out max);
            Check("books: multi too small to fill 11 slots never", min == 0 && max == 0, min + "~" + max);
            ModSkillRules.BookGrades(4, 1, 11, out min, out max);
            Check("books: one row with multi 11 fills", min == 1 && max == 10, min + "~" + max);
        }

        private static void LocalTextRulesTests()
        {
            Check("modkr: \"0\" falls back to ch", LocalTextRules.ModKr("0", "中文", null) == "中文");
            Check("modkr: empty falls back to ch", LocalTextRules.ModKr("", "中文", null) == "中文");
            Check("modkr: korean kr stays", LocalTextRules.ModKr("한글", "中文", null) == null);
            Check("modkr: no ch, no change", LocalTextRules.ModKr("0", "", null) == null);
            Check("modkr: translation wins over \"0\"", LocalTextRules.ModKr("0", "中文", "번역") == "번역");
            Check("modkr: translation wins over korean", LocalTextRules.ModKr("한글", "中文", "번역") == "번역");
            Check("whole: chinese ch with korean kr", LocalTextRules.KeepsWhole("中文", "한국어"));
            Check("whole: kr \"0\"", !LocalTextRules.KeepsWhole("中文", "0"));
            Check("whole: kr same as ch", !LocalTextRules.KeepsWhole("中文", "中文"));
            Check("whole: ch without chinese", !LocalTextRules.KeepsWhole("abc", "한국어"));
            Check("whole: kr with chinese", !LocalTextRules.KeepsWhole("中文", "한中"));
            Check("whole: null ch", !LocalTextRules.KeepsWhole(null, "한"));
            Check("whole: empty kr", !LocalTextRules.KeepsWhole("中文", ""));
        }
        private static string Join(int[] a)
        {
            return string.Join(",", Array.ConvertAll(a, x => x.ToString()));
        }

        // 수련 단계 대도까지 채우기(05 문서 83절). 배열은 회차 ms2-1023a의 실측.
        private static void MasteryRulesTests()
        {
            int[] seven = { 2, 0, 1, 1, 14111, 1, 1, 1, 7, 1, 14, 5, 11, -1, 83, 22, 85, 549, 92, 66, 63 };
            int[] sevenAfter = { 2, 0, 1, 1, 14111, 1, 1, 1, 7, 1, 14, 5, 11, 1, 1, 1, 1, -1, 83, 22, 85, 549, 92, 66, 63, 1, 1, 1, 1 };
            Check("mastery: insert into seven options", Join(MasteryRules.InsertOptions(seven, new[] { 1, 1, 1, 1 })) == Join(sevenAfter), Join(MasteryRules.InsertOptions(seven, new[] { 1, 1, 1, 1 })));
            int[] none = { 2, 0, 1, 1, 11423, 1 };
            int[] noneAfter = { 2, 0, 1, 1, 11423, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            int[] add11 = { 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5 };
            Check("mastery: insert into no options", Join(MasteryRules.InsertOptions(none, add11)) == Join(noneAfter), Join(MasteryRules.InsertOptions(none, add11)));
            Check("mastery: empty add copies", Join(MasteryRules.InsertOptions(seven, new int[0])) == Join(seven) && !ReferenceEquals(MasteryRules.InsertOptions(seven, new int[0]), seven));
            Check("mastery: options of seven", Join(MasteryRules.OptionsOf(seven)) == "1,1,7,1,14,5,11", Join(MasteryRules.OptionsOf(seven)));
            Check("mastery: options of none", MasteryRules.OptionsOf(none).Length == 0);
            Check("mastery: options after insert", Join(MasteryRules.OptionsOf(noneAfter)) == Join(add11));
            Check("mastery: max options", MasteryRules.MaxOptions == 11);
            int[] w = { 0, 3, 1 };
            Check("mastery: pick 0.0", MasteryRules.PickWeighted(w, 0.0) == 1);
            Check("mastery: pick 0.74", MasteryRules.PickWeighted(w, 0.74) == 1);
            Check("mastery: pick 0.76", MasteryRules.PickWeighted(w, 0.76) == 2);
            Check("mastery: pick all zero", MasteryRules.PickWeighted(new[] { 0, 0 }, 0.5) == -1);
            Check("mastery: pick negative ignored", MasteryRules.PickWeighted(new[] { -5, 2 }, 0.1) == 1);
        }

        private static void TreasureRulesTests()
        {
            int[] want = { 60, 80, 85, 95, 100, 120 };
            for (int n = 1; n <= 6; n++)
            {
                Check("treasure: stage " + n + " rate", TreasureRules.StageRate(n) == want[n - 1], TreasureRules.StageRate(n).ToString());
                int r = TreasureRules.StageRate(n);
                Check("treasure: stage " + n + " round trip", TreasureRules.StageOf(r, r, r, r, r) == n);
            }
            bool thrown0 = false, thrown7 = false;
            try { TreasureRules.StageRate(0); } catch (ArgumentOutOfRangeException) { thrown0 = true; }
            try { TreasureRules.StageRate(7); } catch (ArgumentOutOfRangeException) { thrown7 = true; }
            Check("treasure: stage 0 and 7 throw", thrown0 && thrown7);
            int[,] even = { { 0, 1 }, { 60, 1 }, { 61, 2 }, { 80, 2 }, { 81, 3 }, { 89, 3 }, { 90, 4 }, { 99, 4 }, { 100, 5 }, { 109, 5 }, { 110, 6 }, { 200, 6 } };
            for (int i = 0; i < even.GetLength(0); i++)
            {
                int r = even[i, 0];
                Check("treasure: average " + r + " is stage " + even[i, 1], TreasureRules.StageOf(r, r, r, r, r) == even[i, 1], TreasureRules.StageOf(r, r, r, r, r).ToString());
            }
            Check("treasure: mixed 100x4+60 is 4", TreasureRules.StageOf(100, 100, 100, 100, 60) == 4);
            Check("treasure: mixed 120x4+100 is 6", TreasureRules.StageOf(120, 120, 120, 120, 100) == 6);
            Check("treasure: mixed 120+100x4 is 5", TreasureRules.StageOf(120, 100, 100, 100, 100) == 5);
            Check("treasure: mixed 100x4+0 is 2", TreasureRules.StageOf(100, 100, 100, 100, 0) == 2);
            for (int n = 1; n <= 6; n++)
            {
                int r = TreasureRules.StageRate(n), sp = TreasureRules.SpRate(n);
                Check("treasure: stage " + n + " sp rate is at most 100", sp == Math.Min(r, 100), sp.ToString());
                Check("treasure: stage " + n + " with the sp rate stays stage " + n, TreasureRules.StageOf(r, r, r, r, sp) == n);
            }
            Check("treasure: grade steps 2->10", TreasureRules.GradeSteps(2, 10) == 8);
            Check("treasure: grade steps 10->10", TreasureRules.GradeSteps(10, 10) == 0);
            Check("treasure: grade steps 14->10", TreasureRules.GradeSteps(14, 10) == 0);
            Check("treasure: grade steps 3->12 stops at 10", TreasureRules.GradeSteps(3, 12) == 7);
        }

        // 숫자 칸 제출 판정과 자원 탭 보유 칸의 줄이기 규칙(13 문서 B1·B2, 05 문서 89절).
        private static void NumberFieldRulesTests()
        {
            int v;
            Check("numberfield: blur on an enter-only field is ignored", NumberFieldRules.Decide("5", "3", false, false, out v) == NumberFieldRules.Action.Ignore);
            Check("numberfield: enter with a number submits", NumberFieldRules.Decide("5", "3", true, false, out v) == NumberFieldRules.Action.Submit && v == 5);
            Check("numberfield: enter with text rejects", NumberFieldRules.Decide("abc", "3", true, true, out v) == NumberFieldRules.Action.Reject);
            Check("numberfield: blur with text reverts", NumberFieldRules.Decide("abc", "3", false, true, out v) == NumberFieldRules.Action.Revert);
            Check("numberfield: blur with the same value is ignored", NumberFieldRules.Decide("3", "3", false, true, out v) == NumberFieldRules.Action.Ignore);
            Check("numberfield: blur with the same value in other digits is ignored", NumberFieldRules.Decide("03", "3", false, true, out v) == NumberFieldRules.Action.Ignore);
            Check("numberfield: blur with a new value submits", NumberFieldRules.Decide("4", "3", false, true, out v) == NumberFieldRules.Action.Submit && v == 4);
            Check("numberfield: enter with the same value still submits", NumberFieldRules.Decide("3", "3", true, true, out v) == NumberFieldRules.Action.Submit);
            Check("owned: same value is ignored", NumberFieldRules.DecideOwned(10, 10, true, false, 0) == NumberFieldRules.OwnedAction.Ignore);
            Check("owned: raising sets on blur", NumberFieldRules.DecideOwned(25, 10, false, false, 0) == NumberFieldRules.OwnedAction.Set);
            Check("owned: raising sets on enter", NumberFieldRules.DecideOwned(25, 10, true, false, 0) == NumberFieldRules.OwnedAction.Set);
            Check("owned: lowering on blur is ignored", NumberFieldRules.DecideOwned(7, 10, false, false, 0) == NumberFieldRules.OwnedAction.Ignore);
            Check("owned: lowering on enter asks", NumberFieldRules.DecideOwned(7, 10, true, false, 0) == NumberFieldRules.OwnedAction.Ask);
            Check("owned: lowering on enter with the same pending value confirms", NumberFieldRules.DecideOwned(7, 10, true, true, 7) == NumberFieldRules.OwnedAction.Confirm);
            Check("owned: lowering on enter with another pending value asks again", NumberFieldRules.DecideOwned(5, 10, true, true, 7) == NumberFieldRules.OwnedAction.Ask);
            Check("owned: zero on enter asks", NumberFieldRules.DecideOwned(0, 10, true, false, 0) == NumberFieldRules.OwnedAction.Ask);
            // 최종 리뷰 Important 1: 콜백이 받아들이지 않은 값은 committed가 되지 않는다. 그래야 같은 값을 다시 쓰고 떠나도 콜백이 다시 불려 칸을 되돌린다.
            Check("numberfield: a rejected submit keeps the old committed text", NumberFieldRules.NextCommitted("25", "7", false) == "25");
            Check("numberfield: an accepted submit commits the new text", NumberFieldRules.NextCommitted("25", "30", true) == "30");
            Check("numberfield: after a rejected blur the same value still submits", NumberFieldRules.Decide("7", NumberFieldRules.NextCommitted("25", "7", false), false, true, out v) == NumberFieldRules.Action.Submit);
        }

        // 전투 단위 상태의 전환 규칙(스펙 2026-10-30-rate-service-cleanup 2절, 13 문서 A2·A4·2.6절).
        private static void RateUnitRulesTests()
        {
            Check("unit: out of battle with written values holds", RateUnitRules.Decide(false, false, true, true, false, true) == RateUnitRules.Step.Skip);
            Check("unit: out of battle without written values forgets", RateUnitRules.Decide(false, true, true, false, false, true) == RateUnitRules.Step.Forget);
            Check("unit: out of battle with nothing held skips", RateUnitRules.Decide(false, true, false, false, false, true) == RateUnitRules.Step.Skip);
            Check("unit: first sight with a row on captures", RateUnitRules.Decide(true, true, false, false, false, true) == RateUnitRules.Step.Capture);
            Check("unit: first sight with nothing on skips", RateUnitRules.Decide(true, false, false, false, false, true) == RateUnitRules.Step.Skip);
            Check("unit: another pointer with a row on captures", RateUnitRules.Decide(true, true, true, true, false, true) == RateUnitRules.Step.Capture);
            Check("unit: another pointer with nothing on forgets", RateUnitRules.Decide(true, false, true, true, false, true) == RateUnitRules.Step.Forget);
            Check("unit: same pointer but changed values captures again", RateUnitRules.Decide(true, true, true, true, true, false) == RateUnitRules.Step.Capture);
            Check("unit: same pointer but changed values with nothing on forgets", RateUnitRules.Decide(true, false, true, true, true, false) == RateUnitRules.Step.Forget);
            Check("unit: same pointer intact applies", RateUnitRules.Decide(true, true, true, true, true, true) == RateUnitRules.Step.Apply);
            Check("unit: same pointer, nothing on, written values apply (restore)", RateUnitRules.Decide(true, false, true, true, true, true) == RateUnitRules.Step.Apply);
            Check("move: attach only when on and not attached", RateUnitRules.MoveAttachNeeded(true, false) && !RateUnitRules.MoveAttachNeeded(true, true) && !RateUnitRules.MoveAttachNeeded(false, false));
            Check("move: detach only when off and attached", RateUnitRules.MoveDetachNeeded(false, true) && !RateUnitRules.MoveDetachNeeded(true, true) && !RateUnitRules.MoveDetachNeeded(false, false));
        }
    }
}
