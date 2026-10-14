using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace MOD_sWP6UL
{
    // 프리셋: 빌드 하나의 완성형을 적은 JSON 파일. 단계(대경지 1~10)는 완성형에서 규칙으로 만든다.
    // 화면을 모르고 결과를 문장으로 돌려준다. 대상은 항상 플레이어다. 근거: docs/superpowers/specs/2026-10-14-preset-tab-design.md.
    // 이 파일은 형식·읽기·기본 프리셋·단계·상태를, PresetService.Apply.cs는 적용을 맡는다.
    internal static partial class PresetService
    {
        public const string Kind = "preset";
        public const int StageCount = 10;
        private const string ResourcePrefix = "Presets/";
        private const string BadShape = "줄의 모양이 틀립니다";
        private const string Unknown = "게임에 없는 번호입니다";

        private static readonly string[] SlotNames = { "left", "right", "step", "ultimate" };
        private static readonly MartialType[] SlotTypes = { MartialType.SkillLeft, MartialType.SkillRight, MartialType.Step, MartialType.Ultimate };

        public class SkillLine
        {
            public MartialType Type;
            public int BaseId;
            public string Name = "";
            public int[] Options = new int[0];
            public int Suit;                // 심법에 쓸 세트 번호. 0이면 쓰지 않는다
            public int AbilityType;         // 심법의 유형(1~8). 심법이 아니면 0
            public int GradeMin = 1;
            public int GradeMax = StageCount;
            public bool Filled;             // 공략에 없어 채운 줄
            public string Problem;          // 이 줄을 쓸 수 없는 이유. null이면 쓸 수 있다
        }

        public class LuckLine
        {
            public int Id;
            public string Name = "";
            public bool Filled;
            public string Problem;
        }

        public class FateLine
        {
            public int Grade;
            public int Id;
            public string Name = "";
            public bool Filled;
            public string Problem;
        }

        public class SpriteLine
        {
            public int Id;
            public int ArtifactId;
            public bool Filled;
            public string Problem;
        }

        public class ElderLine
        {
            public int Id;
            public bool Filled;
            public string Problem;
        }

        public class WingLine
        {
            public int Id;
            public int[] Effects = new int[0];
            public int[] Prefixes = new int[0];
            public bool Filled;
            public string Problem;
        }

        public class Preset
        {
            public string File;
            public string Name = "";
            public string Summary = "";
            public string Source = "";
            public string Error;            // 읽을 수 없는 이유. null이면 읽혔다
            public List<LuckLine> BornLucks = new List<LuckLine>();
            public List<SkillLine> Skills = new List<SkillLine>();
            public List<SkillLine> Abilities = new List<SkillLine>();
            public List<FateLine> Fates = new List<FateLine>();
            public SpriteLine Sprite;       // 구역이 없으면 null
            public ElderLine Elder;
            public WingLine Wingman;
        }

        private static WorldUnitBase Me
        {
            get { return g.world.playerUnit; }
        }

        public static string Dir
        {
            get { return Path.Combine(ChangeLog.DataDir, "presets"); }
        }

        // ---- 기본 프리셋 (DLL 안의 파일) ----

        private static Dictionary<string, string> defaults;     // 파일 이름 → 내용

        private static Dictionary<string, string> Defaults()
        {
            if (defaults != null)
            {
                return defaults;
            }
            defaults = new Dictionary<string, string>();
            Assembly assembly = typeof(PresetService).Assembly;
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                {
                    continue;
                }
                using (Stream stream = assembly.GetManifestResourceStream(name))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    defaults[name.Substring(ResourcePrefix.Length)] = reader.ReadToEnd();
                }
            }
            return defaults;
        }

        public static int DefaultCount
        {
            get { return Defaults().Count; }
        }

        private static void WriteDefaults()
        {
            Directory.CreateDirectory(Dir);
            foreach (var pair in Defaults())
            {
                System.IO.File.WriteAllText(Path.Combine(Dir, pair.Key), pair.Value, new UTF8Encoding(false));
            }
        }

        private static string Normal(string text)
        {
            return text.Replace("\r", "").Trim();
        }

        // 폴더에 없거나 DLL 안의 것과 내용이 다른 기본 프리셋의 수. 모드를 갱신한 뒤 새 기본값이 있음을 알리는 데 쓴다.
        public static int DefaultsChanged()
        {
            int changed = 0;
            foreach (var pair in Defaults())
            {
                string path = Path.Combine(Dir, pair.Key);
                try
                {
                    if (!System.IO.File.Exists(path) || Normal(System.IO.File.ReadAllText(path, Encoding.UTF8)) != Normal(pair.Value))
                    {
                        changed++;
                    }
                }
                catch (IOException)
                {
                    changed++;
                }
                catch (UnauthorizedAccessException)
                {
                    changed++;
                }
            }
            return changed;
        }

        // 기본 프리셋을 다시 쓴다. 고친 기본 파일은 덮어쓰고, 다른 파일은 건드리지 않는다.
        public static string RestoreDefaults()
        {
            try
            {
                WriteDefaults();
                return "기본 프리셋 " + DefaultCount + "개를 다시 썼습니다.";
            }
            catch (IOException e)
            {
                return "기본 프리셋: 쓰지 못했습니다(" + e.Message + ").";
            }
            catch (UnauthorizedAccessException e)
            {
                return "기본 프리셋: 쓰지 못했습니다(" + e.Message + ").";
            }
        }

        // 폴더의 프리셋들. 파일 이름 순(대소문자 무시). 폴더가 없으면 만들고 기본 프리셋을 풀어 놓는다.
        // 폴더가 이미 있으면 없는 기본 파일을 다시 풀지 않는다(사용자가 지운 것일 수 있다).
        public static List<Preset> List()
        {
            var result = new List<Preset>();
            try
            {
                if (!Directory.Exists(Dir))
                {
                    WriteDefaults();
                }
                string[] files = Directory.GetFiles(Dir, "*.json");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
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

        // ---- 읽기와 줄 검사 ----

        private static bool Flag(Dictionary<string, object> o, string key)
        {
            object value;
            return o.TryGetValue(key, out value) && value is bool && (bool)value;
        }

        private static string Named(string name, int id)
        {
            return string.IsNullOrEmpty(name) ? id.ToString() : name;
        }

        // 그 종류의 기술 목록에서 번호로 찾는다. 없으면 null.
        private static SkillCatalog.Entry Entry(MartialType type, int baseId)
        {
            for (int kind = 0; kind < SkillCatalog.Kinds.Length; kind++)
            {
                if (SkillCatalog.Kinds[kind].Type != type)
                {
                    continue;
                }
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(kind, baseId.ToString()))
                {
                    if (entry.BaseId == baseId)
                    {
                        return entry;
                    }
                }
            }
            return null;
        }

        private static bool KnownSkill(int baseId)
        {
            for (int kind = 0; kind < SkillCatalog.Kinds.Length; kind++)
            {
                if (Entry(SkillCatalog.Kinds[kind].Type, baseId) != null)
                {
                    return true;
                }
            }
            return false;
        }

        // 기술 번호를 게임에서 찾아 이름과 경지 범위를 채우고, 쓸 수 없으면 이유를 적는다.
        private static void Finish(SkillLine line, bool duplicate)
        {
            SkillCatalog.Entry entry = Entry(line.Type, line.BaseId);
            if (entry == null)
            {
                line.Problem = KnownSkill(line.BaseId) ? "이 칸에 넣을 수 없는 기술입니다" : Unknown;
                return;
            }
            line.Name = entry.Name;
            line.GradeMin = entry.GradeMin;
            line.GradeMax = entry.GradeMax;
            line.Problem = duplicate ? "같은 칸의 줄이 이미 있습니다" : MartialService.OptionProblem(line.Type, line.BaseId, line.Options);
        }

        private static SkillLine ReadSkill(Dictionary<string, object> o, List<MartialType> seen)
        {
            var line = new SkillLine();
            if (o == null || !ExportFiles.Whole(o, "baseId", out line.BaseId) || !ExportFiles.Ints(o, "options", out line.Options))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Name = ExportFiles.Text(o, "name");
            line.Filled = Flag(o, "filled");
            int slot = Array.IndexOf(SlotNames, ExportFiles.Text(o, "slot"));
            if (slot < 0)
            {
                line.Problem = "slot은 left, right, step, ultimate 가운데 하나여야 합니다";
                return line;
            }
            line.Type = SlotTypes[slot];
            Finish(line, seen.Contains(line.Type));
            if (line.Problem == null)
            {
                seen.Add(line.Type);
            }
            return line;
        }

        private static SkillLine ReadAbility(Dictionary<string, object> o, List<int> seen)
        {
            var line = new SkillLine { Type = MartialType.Ability };
            if (o == null || !ExportFiles.Whole(o, "baseId", out line.BaseId) || !ExportFiles.Ints(o, "options", out line.Options)
                || (o.ContainsKey("suit") && !ExportFiles.Whole(o, "suit", out line.Suit)))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Name = ExportFiles.Text(o, "name");
            line.Filled = Flag(o, "filled");
            line.AbilityType = MartialService.AbilityType(line.BaseId);
            Finish(line, line.AbilityType != 0 && seen.Contains(line.AbilityType));
            if (line.Problem == null && line.Suit != 0)
            {
                ConfBattleAbilitySuitBaseItem suit = g.conf.battleAbilitySuitBase.GetItem(line.Suit);
                if (suit == null)
                {
                    line.Problem = "게임에 없는 세트입니다(" + line.Suit + ")";
                }
                else if (Array.IndexOf((suit.suitMember ?? "").Split('|'), line.BaseId.ToString()) < 0)
                {
                    line.Problem = "이 세트의 구성원이 아닙니다(" + SuitName(line.Suit) + ")";
                }
            }
            if (line.Problem == null)
            {
                seen.Add(line.AbilityType);
            }
            return line;
        }

        private static LuckLine ReadLuck(Dictionary<string, object> o)
        {
            var line = new LuckLine();
            if (o == null || !ExportFiles.Whole(o, "id", out line.Id))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Name = ExportFiles.Text(o, "name");
            line.Filled = Flag(o, "filled");
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(line.Id);
            if (conf == null)
            {
                line.Problem = Unknown;
                return line;
            }
            line.Name = GameTool.LS(conf.name);
            // 역천개명 분류를 운명으로 붙이면 지금 대경지의 역천개명 기록이 덮어써진다(docs/05 31절). 진행 표식과 숨김 운명도 받지 않는다.
            if (conf.type == 3 || conf.conceal != 0 || LuckService.IsSystem(line.Id))
            {
                line.Problem = "선천운명으로 붙일 수 없는 운명입니다";
            }
            return line;
        }

        private static FateLine ReadFate(Dictionary<string, object> o, List<int> seen)
        {
            var line = new FateLine();
            if (o == null || !ExportFiles.Whole(o, "grade", out line.Grade) || !ExportFiles.Whole(o, "id", out line.Id))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Name = ExportFiles.Text(o, "name");
            line.Filled = Flag(o, "filled");
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(line.Id);
            ConfFateFeatureItem fate = g.conf.fateFeature.GetItem(line.Id);
            if (conf == null || fate == null)
            {
                line.Problem = "게임의 역천개명 목록에 없습니다";
                return line;
            }
            line.Name = GameTool.LS(conf.name);
            if (line.Grade < FateService.FirstGrade || line.Grade > StageCount || fate.gradeMin > line.Grade || fate.gradeMax < line.Grade)
            {
                line.Problem = "이 경지의 역천개명이 아닙니다";
            }
            else if (seen.Contains(line.Grade))
            {
                line.Problem = "같은 경지의 줄이 이미 있습니다";
            }
            else
            {
                seen.Add(line.Grade);
            }
            return line;
        }

        private static SpriteLine ReadSprite(Dictionary<string, object> o)
        {
            var line = new SpriteLine();
            if (o == null || !ExportFiles.Whole(o, "id", out line.Id) || !ExportFiles.Whole(o, "artifact", out line.ArtifactId))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Filled = Flag(o, "filled");
            if (!SpriteService.Known(line.Id))
            {
                line.Problem = "게임에 없는 기령 번호입니다(" + line.Id + ")";
            }
            else if (!SpriteService.IsArtifact(line.ArtifactId))
            {
                line.Problem = "게임의 법보가 아닙니다(" + line.ArtifactId + ")";
            }
            return line;
        }

        private static ElderLine ReadElder(Dictionary<string, object> o)
        {
            var line = new ElderLine();
            if (o == null || !ExportFiles.Whole(o, "id", out line.Id))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Filled = Flag(o, "filled");
            if (!ElderService.Known(line.Id))
            {
                line.Problem = Unknown;
            }
            return line;
        }

        private static WingLine ReadWing(Dictionary<string, object> o)
        {
            var line = new WingLine();
            if (o == null || !ExportFiles.Whole(o, "id", out line.Id) || !ExportFiles.Ints(o, "effects", out line.Effects)
                || !ExportFiles.Ints(o, "prefixes", out line.Prefixes))
            {
                line.Problem = BadShape;
                return line;
            }
            line.Filled = Flag(o, "filled");
            if (!WingmanService.Known(line.Id))
            {
                line.Problem = Unknown;
                return line;
            }
            foreach (int effect in line.Effects)
            {
                if (!WingmanService.EffectKnown(effect))
                {
                    line.Problem = "게임에 없는 효과 번호입니다(" + effect + ")";
                    return line;
                }
            }
            foreach (int prefix in line.Prefixes)
            {
                if (!WingmanService.PrefixOf(prefix, line.Id))
                {
                    line.Problem = "이 명우의 옵션이 아닙니다(" + prefix + ")";
                    return line;
                }
            }
            return line;
        }

        private static Preset Read(string file)
        {
            var preset = new Preset { File = file, Name = Path.GetFileNameWithoutExtension(file) };
            try
            {
                var root = Json.Parse(System.IO.File.ReadAllText(file, Encoding.UTF8)) as Dictionary<string, object>;
                int format;
                if (root == null || !ExportFiles.Whole(root, "format", out format) || format != 1 || ExportFiles.Text(root, "kind") != Kind)
                {
                    preset.Error = "이 편집기의 프리셋 파일(format 1)이 아닙니다";
                    return preset;
                }
                string name = ExportFiles.Text(root, "name");
                if (name.Length > 0)
                {
                    preset.Name = name;
                }
                preset.Summary = ExportFiles.Text(root, "summary");
                preset.Source = ExportFiles.Text(root, "source");
                foreach (var o in ExportFiles.Items(root, "bornLucks"))
                {
                    preset.BornLucks.Add(ReadLuck(o));
                }
                var slots = new List<MartialType>();
                foreach (var o in ExportFiles.Items(root, "skills"))
                {
                    preset.Skills.Add(ReadSkill(o, slots));
                }
                var types = new List<int>();
                foreach (var o in ExportFiles.Items(root, "abilities"))
                {
                    preset.Abilities.Add(ReadAbility(o, types));
                }
                var grades = new List<int>();
                foreach (var o in ExportFiles.Items(root, "fates"))
                {
                    preset.Fates.Add(ReadFate(o, grades));
                }
                // 구역이 통째로 없으면 그 구역은 건드리지 않는다. 있는데 객체가 아니면 쓸 수 없는 줄로 남긴다.
                object raw;
                if (root.TryGetValue("sprite", out raw) && raw != null)
                {
                    preset.Sprite = ReadSprite(raw as Dictionary<string, object>);
                }
                if (root.TryGetValue("elder", out raw) && raw != null)
                {
                    preset.Elder = ReadElder(raw as Dictionary<string, object>);
                }
                if (root.TryGetValue("wingman", out raw) && raw != null)
                {
                    preset.Wingman = ReadWing(raw as Dictionary<string, object>);
                }
            }
            catch (FormatException e)
            {
                preset.Error = "JSON 문법 오류(" + e.Message + ")";
            }
            catch (IOException e)
            {
                preset.Error = e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                preset.Error = e.Message;
            }
            return preset;
        }

        private static void Note(List<string> into, string section, string name, string problem)
        {
            if (problem != null)
            {
                into.Add(section + " " + name + ": " + problem);
            }
        }

        // 프리셋의 쓸 수 없는 줄들("구역 이름: 이유").
        public static List<string> Problems(Preset preset)
        {
            var result = new List<string>();
            foreach (LuckLine line in preset.BornLucks)
            {
                Note(result, "선천운명", Named(line.Name, line.Id), line.Problem);
            }
            foreach (SkillLine line in preset.Skills)
            {
                Note(result, "공법", Named(line.Name, line.BaseId), line.Problem);
            }
            foreach (SkillLine line in preset.Abilities)
            {
                Note(result, "심법", Named(line.Name, line.BaseId), line.Problem);
            }
            foreach (FateLine line in preset.Fates)
            {
                Note(result, "역천개명", line.Grade + ":" + Named(line.Name, line.Id), line.Problem);
            }
            if (preset.Sprite != null)
            {
                Note(result, "법보와 기령", preset.Sprite.Id.ToString(), preset.Sprite.Problem);
            }
            if (preset.Elder != null)
            {
                Note(result, "신혼", preset.Elder.Id.ToString(), preset.Elder.Problem);
            }
            if (preset.Wingman != null)
            {
                Note(result, "명우", preset.Wingman.Id.ToString(), preset.Wingman.Problem);
            }
            return result;
        }

        // 쓸 수 있는 줄의 수.
        public static int Usable(Preset preset)
        {
            int total = preset.BornLucks.Count + preset.Skills.Count + preset.Abilities.Count + preset.Fates.Count
                + (preset.Sprite == null ? 0 : 1) + (preset.Elder == null ? 0 : 1) + (preset.Wingman == null ? 0 : 1);
            return total - Problems(preset).Count;
        }

        // ---- 단계 ----

        public class Stage
        {
            public int Grade;
            public List<LuckLine> Born = new List<LuckLine>();
            public FateLine Fate;                                           // 없으면 null
            public List<SkillLine> Skills = new List<SkillLine>();
            public List<SkillLine> Abilities = new List<SkillLine>();
            public List<SkillLine> OutOfRange = new List<SkillLine>();      // 이 경지에는 없는 기술. 보이기만 한다
            public SpriteLine Sprite;
            public ElderLine Elder;
            public WingLine Wingman;
        }

        public static string StageName(int grade)
        {
            ConfRoleGradeItem row = GradeService.FirstRowOf(grade);
            return row == null ? "경지 " + grade : GameTool.LS(row.gradeName);
        }

        public static string SuitName(int suitId)
        {
            ConfBattleAbilitySuitBaseItem conf = g.conf.battleAbilitySuitBase.GetItem(suitId);
            return conf == null ? suitId.ToString() : EditService.Compact(GameTool.LS(conf.suitName));
        }

        // 쓸 수 없는 줄은 어느 단계에나 보인다(이유를 알리기 위해서다).
        private static bool InRange(SkillLine line, int grade)
        {
            return line.Problem != null || (grade >= line.GradeMin && grade <= line.GradeMax);
        }

        // 대경지 grade에서 적용하는 것들(스펙 5.2절).
        public static Stage StageOf(Preset preset, int grade)
        {
            var stage = new Stage { Grade = grade };
            if (grade == 1)
            {
                stage.Born.AddRange(preset.BornLucks);
            }
            stage.Fate = preset.Fates.Find(f => f.Grade == grade && f.Problem == null) ?? preset.Fates.Find(f => f.Grade == grade);
            foreach (SkillLine line in preset.Skills)
            {
                (InRange(line, grade) ? stage.Skills : stage.OutOfRange).Add(line);
            }
            foreach (SkillLine line in preset.Abilities)
            {
                (InRange(line, grade) ? stage.Abilities : stage.OutOfRange).Add(line);
            }
            stage.Sprite = grade >= SpriteService.FirstGrade ? preset.Sprite : null;
            stage.Elder = grade >= ElderService.FirstGrade ? preset.Elder : null;
            stage.Wingman = grade >= WingmanService.FirstGrade ? preset.Wingman : null;
            return stage;
        }

        private static string Mark(bool filled, string problem)
        {
            return (filled ? "(공략에 없어 채움)" : "") + (problem == null ? "" : " [쓸 수 없음: " + problem + "]");
        }

        // 단계의 구성을 여러 줄의 글로. 상세보기의 줄과 검증이 쓴다.
        public static string StageText(Stage stage)
        {
            var sb = new StringBuilder();
            if (stage.Born.Count > 0)
            {
                sb.Append("선천운명: ");
                for (int i = 0; i < stage.Born.Count; i++)
                {
                    LuckLine line = stage.Born[i];
                    sb.Append(i == 0 ? "" : ", ").Append(Named(line.Name, line.Id)).Append(Mark(line.Filled, line.Problem));
                }
                sb.Append('\n');
            }
            if (stage.Fate != null)
            {
                sb.Append("역천개명: ").Append(Named(stage.Fate.Name, stage.Fate.Id)).Append(Mark(stage.Fate.Filled, stage.Fate.Problem)).Append('\n');
            }
            if (stage.Skills.Count > 0)
            {
                sb.Append("공법(경지 ").Append(stage.Grade).Append("): ");
                for (int i = 0; i < stage.Skills.Count; i++)
                {
                    SkillLine line = stage.Skills[i];
                    sb.Append(i == 0 ? "" : ", ").Append(MartialService.TypeLabel(line.Type)).Append(' ').Append(Named(line.Name, line.BaseId))
                        .Append("(옵션 ").Append(line.Options.Length).Append(')').Append(Mark(line.Filled, line.Problem));
                }
                sb.Append('\n');
            }
            if (stage.Abilities.Count > 0)
            {
                sb.Append("심법 ").Append(stage.Abilities.Count).Append("(경지 ").Append(stage.Grade).Append("): ");
                for (int i = 0; i < stage.Abilities.Count; i++)
                {
                    SkillLine line = stage.Abilities[i];
                    sb.Append(i == 0 ? "" : ", ").Append(Named(line.Name, line.BaseId))
                        .Append(line.Suit == 0 ? "" : "[" + SuitName(line.Suit) + "]").Append(Mark(line.Filled, line.Problem));
                }
                sb.Append('\n');
            }
            if (stage.OutOfRange.Count > 0)
            {
                sb.Append("이 경지에는 없는 기술: ");
                for (int i = 0; i < stage.OutOfRange.Count; i++)
                {
                    SkillLine line = stage.OutOfRange[i];
                    sb.Append(i == 0 ? "" : ", ").Append(Named(line.Name, line.BaseId)).Append("(경지 ").Append(line.GradeMin).Append('~').Append(line.GradeMax).Append(')');
                }
                sb.Append('\n');
            }
            if (stage.Sprite != null)
            {
                sb.Append("법보와 기령: ").Append(SpriteService.ArtifactName(stage.Sprite.ArtifactId)).Append(" + ").Append(SpriteService.SpriteName(stage.Sprite.Id))
                    .Append(Mark(stage.Sprite.Filled, stage.Sprite.Problem)).Append('\n');
            }
            if (stage.Elder != null)
            {
                sb.Append("신혼: ").Append(ElderService.Name(stage.Elder.Id)).Append(Mark(stage.Elder.Filled, stage.Elder.Problem)).Append('\n');
            }
            if (stage.Wingman != null)
            {
                sb.Append("명우: ").Append(WingmanService.Name(stage.Wingman.Id)).Append("(효과 ").Append(stage.Wingman.Effects.Length)
                    .Append(", 옵션 ").Append(stage.Wingman.Prefixes.Length).Append(')').Append(Mark(stage.Wingman.Filled, stage.Wingman.Problem)).Append('\n');
            }
            return sb.Length == 0 ? "(이 단계에는 적용할 것이 없습니다)" : sb.ToString().TrimEnd('\n');
        }

        // ---- 상태 ----

        public class Learned
        {
            public string Sole;
            public int BaseId;
            public int Grade;
            public int[] Options;
            public int Suit;
            public bool Equipped;
        }

        // 익힌 공법의 사본. 상태를 여러 번 물을 때 게임 객체를 다시 읽지 않도록 한 번 떠 둔다.
        public static List<Learned> Snapshot()
        {
            var result = new List<Learned>();
            WorldUnitBase me = Me;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                // 깨진 공법은 읽는 것만으로 예외가 난다. 없는 것으로 본다.
                try
                {
                    DataProps.PropsData props = pair.Value.data;
                    DataProps.MartialData martial = props.To<DataProps.MartialData>();
                    result.Add(new Learned
                    {
                        Sole = pair.Key,
                        BaseId = martial.baseID,
                        Grade = martial.grade,
                        Options = MartialService.OptionNumbers(martial),
                        Suit = martial.martialType == MartialType.Ability ? MartialService.SuitOf(props) : 0,
                        Equipped = MartialService.IsEquipped(me, pair.Key),
                    });
                }
                catch (Exception)
                {
                }
            }
            return result;
        }

        private static bool SameOptions(int[] a, int[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        // 그 줄과 같은 공법: 같은 기술과 옵션, 그 경지 이상, 심법이면 세트 번호도 같다. 장착한 것을 먼저 고른다. 없으면 null.
        public static Learned Find(List<Learned> learned, SkillLine line, int grade)
        {
            Learned found = null;
            foreach (Learned each in learned)
            {
                if (each.BaseId != line.BaseId || each.Grade < grade || !SameOptions(each.Options, line.Options))
                {
                    continue;
                }
                if (line.Type == MartialType.Ability && each.Suit != line.Suit)
                {
                    continue;
                }
                if (found == null || (each.Equipped && !found.Equipped))
                {
                    found = each;
                }
            }
            return found;
        }

        // 그 대경지의 역천개명 기록. 없으면 0.
        public static int FateNow(WorldUnitBase unit, int grade)
        {
            int id;
            return FateService.Records(unit).TryGetValue(grade, out id) ? id : 0;
        }

        public enum StageState { Locked, Applied, Partial, None }

        private static void Tick(bool usable, bool present, ref int have, ref int total)
        {
            if (!usable)
            {
                return;
            }
            total++;
            have += present ? 1 : 0;
        }

        // 단계의 상태. total은 쓸 수 있는 줄의 수, have는 그 가운데 이미 있는 줄의 수다(스펙 5.2절의 "있음").
        public static StageState StateOf(Stage stage, List<Learned> learned, out int have, out int total)
        {
            WorldUnitBase me = Me;
            have = 0;
            total = 0;
            foreach (LuckLine line in stage.Born)
            {
                Tick(line.Problem == null, line.Problem == null && LuckService.Has(me, line.Id), ref have, ref total);
            }
            if (stage.Fate != null)
            {
                Tick(stage.Fate.Problem == null, FateNow(me, stage.Fate.Grade) == stage.Fate.Id, ref have, ref total);
            }
            foreach (SkillLine line in stage.Skills)
            {
                Learned same = line.Problem == null ? Find(learned, line, stage.Grade) : null;
                Tick(line.Problem == null, same != null && same.Equipped, ref have, ref total);
            }
            foreach (SkillLine line in stage.Abilities)
            {
                Learned same = line.Problem == null ? Find(learned, line, stage.Grade) : null;
                Tick(line.Problem == null, same != null && same.Equipped, ref have, ref total);
            }
            if (stage.Sprite != null)
            {
                Tick(stage.Sprite.Problem == null, stage.Sprite.Problem == null && SpriteService.Has(me, stage.Sprite.Id, stage.Sprite.ArtifactId), ref have, ref total);
            }
            if (stage.Elder != null)
            {
                Tick(stage.Elder.Problem == null, stage.Elder.Problem == null && ElderService.Has(me, stage.Elder.Id), ref have, ref total);
            }
            if (stage.Wingman != null)
            {
                Tick(stage.Wingman.Problem == null,
                    stage.Wingman.Problem == null && WingmanService.Has(me, stage.Wingman.Id, stage.Wingman.Effects, stage.Wingman.Prefixes), ref have, ref total);
            }
            if (FateService.CurrentGrade(me) < stage.Grade)
            {
                return StageState.Locked;
            }
            return have == total ? StageState.Applied : have == 0 ? StageState.None : StageState.Partial;
        }

        // "적용됨"인 단계의 수. 목록의 상태 칸에 쓴다.
        public static int AppliedCount(Preset preset, List<Learned> learned)
        {
            int count = 0;
            for (int grade = 1; grade <= StageCount; grade++)
            {
                int have, total;
                if (StateOf(StageOf(preset, grade), learned, out have, out total) == StageState.Applied && total > 0)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
