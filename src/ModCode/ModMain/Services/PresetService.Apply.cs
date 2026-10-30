using System;
using MelonLoader;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Outcome = MOD_sWP6UL.ApplyOutcome;
using Put = MOD_sWP6UL.ApplyPut;
using Tally = MOD_sWP6UL.ApplyTally;

namespace MOD_sWP6UL
{
    // 프리셋의 적용. 줄 하나를 넣는 함수가 넣음·건너뜀·못 넣음 가운데 하나를 돌려주고 구역마다 그것을 센다(CharacterExportService와 같은 방식).
    // 판정은 기존 서비스가 돌려준 문장이 아니라 넣은 뒤의 상태로 한다.
    internal static partial class PresetService
    {

        // 한 번의 적용에서 수도 점수를 올렸으면 처음 값과 마지막 값.
        private sealed class Points
        {
            public int Before = -1;
            public int After;
        }

        // 마지막 적용의 넣음·건너뜀·못 넣음의 합("done= skipped= failed="). 거부됐으면 빈 글. 검증이 읽는다.
        public static string LastRun = "";

        // ---- 줄 하나 넣기 ----

        private static bool IsBorn(WorldUnitBase me, int id)
        {
            var born = me.data.unitData.propertyData.bornLuck;
            for (int i = 0; i < born.Length; i++)
            {
                if (born[i].id == id)
                {
                    return true;
                }
            }
            return false;
        }

        private static Outcome PutBorn(WorldUnitBase me, LuckLine line, out string text)
        {
            string label = Named(line.Name, line.Id);
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            // 없는 것만 더한다. 후천 목록에 있는 것을 선천으로 옮기지도 않는다(캐릭터 가져오기와 같다).
            if (LuckService.Has(me, line.Id))
            {
                text = label;
                return Outcome.Skipped;
            }
            text = LuckService.AddBorn(me, line.Id);
            return IsBorn(me, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        // pending: 이번 적용이 정할 대경지 가운데 아직 처리하지 않은 것들. 함께 둘 수 없는 것을 가진 대경지가 여기 있으면
        // FateService.SetAmong이 그 칸을 먼저 비운다(곧 그 줄이 다시 정한다). 처리한 대경지는 빼서, 정해진 칸이나 실패한 칸을 뒤의 줄이 비우지 않게 한다.
        private static Outcome PutFate(WorldUnitBase me, FateLine line, List<int> pending, out string text)
        {
            pending.Remove(line.Grade);
            string label = Named(line.Name, line.Id);
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (FateNow(me, line.Grade) == line.Id && LuckService.Has(me, line.Id))
            {
                text = label;
                return Outcome.Skipped;
            }
            text = FateService.SetAmong(me, line.Grade, line.Id, pending);
            return FateNow(me, line.Grade) == line.Id && LuckService.Has(me, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        // 이 심법을 장착하기에 수도 점수가 모자라면 모자라지 않을 만큼만 올린다(게임의 거부 코드 61).
        private static void RaisePoints(WorldUnitBase me, string sole, Points points)
        {
            AttributeDef def = AttributeCatalog.All.Find(d => d.Key == "abilityPoint");
            var saved = me.data.unitData.propertyData;
            int cost = MartialService.Martial(me, sole).martialInfo.abilityPoint;
            int used = me.data.dynUnitData.GetCurUseAbilityPoint();
            int total = def.DynValue(me.data.dynUnitData);
            if (used + cost <= total)
            {
                return;
            }
            int before = def.Get(saved);
            EditService.SetAttribute(me, def, before + (used + cost - total));
            if (points.Before < 0)
            {
                points.Before = before;
            }
            points.After = def.Get(saved);
        }

        // 공법·심법 한 줄: 같은 것이 없으면 그 경지로 만들고, 칸을 비운 뒤 장착한다.
        private static Outcome PutSkill(WorldUnitBase me, SkillLine line, int grade, Points points, out string text)
        {
            string label = Named(line.Name, line.BaseId);
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (grade < line.GradeMin || grade > line.GradeMax)
            {
                text = label;
                return Outcome.Skipped;
            }
            var data = me.data.unitData;
            Learned same = Find(Snapshot(), line, grade);
            string sole = same == null ? null : same.Sole;
            bool made = false;
            if (sole == null)
            {
                string error = MartialService.Make(me, line.Type, line.BaseId, grade, line.Options, out sole, true);
                if (error != null)
                {
                    text = label + ": " + error;
                    return Outcome.Failed;
                }
                made = true;
                if (line.Type == MartialType.Ability && line.Suit != 0 && !MartialService.SetSuit(data.GetActionMartial(sole).data, line.Suit))
                {
                    data.DelActionMartial(sole);
                    text = label + ": 세트 번호를 쓰지 못해 넣지 않았습니다";
                    return Outcome.Failed;
                }
            }
            if (MartialService.IsEquipped(me, sole))
            {
                text = label;
                return made ? Outcome.Done : Outcome.Skipped;
            }
            // 칸을 비운다. 게임은 찬 칸(코드 3)과 같은 유형의 심법이 장착된 것(코드 63)을 거부한다(실측).
            string other = line.Type == MartialType.Ability ? MartialService.EquippedAbility(me, line.AbilityType) : MartialService.EquippedIn(me, line.Type);
            string result = label;
            try
            {
                if (other != null)
                {
                    MartialService.Unequip(me, other);
                }
                if (line.Type == MartialType.Ability)
                {
                    RaisePoints(me, sole, points);
                }
                result = MartialService.Equip(me, sole);
            }
            finally
            {
                // 장착하지 못했으면(게임이 거부했거나 중간에 예외가 났으면) 비운 칸을 빈 채로 두지 않는다.
                // 되돌리기가 던지면 원래 예외를 가리지 않게 로그만 남긴다(13 문서 A12). 심법의 원래 칸 번호 보존은 하지 않는다(Equip이 빈 첫 칸을 고른다).
                try
                {
                    if (other != null && !MartialService.IsEquipped(me, sole) && !MartialService.IsEquipped(me, other))
                    {
                        MartialService.Equip(me, other);
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Msg("[TlToybox] 프리셋 되돌리기 실패(" + label + "): " + e.GetType().Name + ": " + e.Message);
                }
            }
            if (!MartialService.IsEquipped(me, sole))
            {
                text = result;
                return Outcome.Failed;
            }
            text = label;
            return Outcome.Done;
        }

        private static Outcome PutSprite(WorldUnitBase me, SpriteLine line, out string text)
        {
            if (line.Problem != null)
            {
                text = "법보와 기령: " + line.Problem;
                return Outcome.Failed;
            }
            if (SpriteService.Has(me, line.Id, line.ArtifactId))
            {
                text = "법보와 기령";
                return Outcome.Skipped;
            }
            bool changed;
            text = SpriteService.Apply(me, line.Id, line.ArtifactId, out changed);
            return SpriteService.Has(me, line.Id, line.ArtifactId) ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutElder(WorldUnitBase me, ElderLine line, out string text)
        {
            if (line.Problem != null)
            {
                text = "신혼: " + line.Problem;
                return Outcome.Failed;
            }
            if (ElderService.Has(me, line.Id))
            {
                text = "신혼";
                return Outcome.Skipped;
            }
            text = ElderService.Set(me, line.Id);
            return ElderService.Has(me, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutWing(WorldUnitBase me, WingLine line, out string text)
        {
            if (line.Problem != null)
            {
                text = "명우: " + line.Problem;
                return Outcome.Failed;
            }
            if (WingmanService.Has(me, line.Id, line.Effects, line.Prefixes))
            {
                text = "명우";
                return Outcome.Skipped;
            }
            text = WingmanService.Set(me, line.Id, line.Effects, line.Prefixes);
            return WingmanService.Has(me, line.Id, line.Effects, line.Prefixes) ? Outcome.Done : Outcome.Failed;
        }

        // ---- 적용 ----

        private static string Refusal(Preset preset)
        {
            if (preset.Error != null)
            {
                return preset.Name + ": 읽을 수 없는 파일이라 적용하지 않았습니다.";
            }
            if (!System.IO.File.Exists(preset.File))
            {
                return "프리셋: 파일이 없습니다(" + Path.GetFileName(preset.File) + "). 탭을 다시 여세요.";
            }
            return null;
        }

        // born: 선천운명을 넣는가. fateFrom~fateTo: 넣을 역천개명의 대경지 범위.
        // grade: 공법·심법을 만드는 경지이자, 법보(2)·신혼(7)·명우(9)를 넣을지 정하는 대경지.
        private static string Apply(Preset preset, string title, bool born, int fateFrom, int fateTo, int grade)
        {
            LastRun = "";
            string refusal = Refusal(preset);
            if (refusal != null)
            {
                return refusal;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            WorldUnitBase me = Me;
            var borns = new Tally();
            var fates = new Tally();
            var skills = new Tally();
            var abilities = new Tally();
            var extras = new Tally();
            var points = new Points();
            if (born)
            {
                foreach (LuckLine each in preset.BornLucks)
                {
                    LuckLine line = each;
                    borns.Count(Named(line.Name, line.Id), (out string text) => PutBorn(me, line, out text));
                }
            }
            var replacing = new List<int>();
            foreach (FateLine each in preset.Fates)
            {
                if (each.Problem == null && each.Grade >= fateFrom && each.Grade <= fateTo)
                {
                    replacing.Add(each.Grade);
                }
            }
            foreach (FateLine each in preset.Fates)
            {
                FateLine line = each;
                if (line.Grade >= fateFrom && line.Grade <= fateTo)
                {
                    fates.Count(Named(line.Name, line.Id), (out string text) => PutFate(me, line, replacing, out text));
                }
            }

            foreach (SkillLine each in preset.Skills)
            {
                SkillLine line = each;
                skills.Count(Named(line.Name, line.BaseId), (out string text) => PutSkill(me, line, grade, points, out text));
            }
            foreach (SkillLine each in preset.Abilities)
            {
                SkillLine line = each;
                abilities.Count(Named(line.Name, line.BaseId), (out string text) => PutSkill(me, line, grade, points, out text));
            }
            if (preset.Sprite != null && grade >= SpriteService.FirstGrade)
            {
                extras.Count("법보와 기령", (out string text) => PutSprite(me, preset.Sprite, out text));
            }
            if (preset.Elder != null && grade >= ElderService.FirstGrade)
            {
                extras.Count("신혼", (out string text) => PutElder(me, preset.Elder, out text));
            }
            if (preset.Wingman != null && grade >= WingmanService.FirstGrade)
            {
                extras.Count("명우", (out string text) => PutWing(me, preset.Wingman, out text));
            }

            var sb = new StringBuilder(preset.Name + " — " + title + ":");
            if (borns.Any)
            {
                sb.Append(' ').Append(borns.Sentence("선천운명", "추가"));
            }
            if (fates.Any)
            {
                sb.Append(' ').Append(fates.Sentence("역천개명", "지정"));
            }
            if (skills.Any)
            {
                sb.Append(' ').Append(skills.Sentence("공법", "장착"));
            }
            if (abilities.Any)
            {
                sb.Append(' ').Append(abilities.Sentence("심법", "장착"));
            }
            if (points.Before >= 0)
            {
                sb.Append(" 수도 점수 ").Append(points.Before).Append(" -> ").Append(points.After).Append('.');
            }
            if (extras.Any)
            {
                sb.Append(' ').Append(extras.Sentence("법보·신혼·명우", "적용"));
            }
            int done = borns.Done + fates.Done + skills.Done + abilities.Done + extras.Done;
            int skipped = borns.Skipped + fates.Skipped + skills.Skipped + abilities.Skipped + extras.Skipped;
            int failed = borns.Failed.Count + fates.Failed.Count + skills.Failed.Count + abilities.Failed.Count + extras.Failed.Count;
            if (done + skipped + failed == 0)
            {
                sb.Append(" 이 범위에 적용할 줄이 없습니다.");
            }
            LastRun = "done=" + done + " skipped=" + skipped + " failed=" + failed;
            string result = sb.ToString();
            ChangeLog.Add(EditService.NameOf(me), "프리셋 " + preset.Name + " (" + title + ")", "-", result, failed == 0);
            return result;
        }

        // 단계 하나. 지금 대경지가 그 단계보다 낮으면 거부한다.
        public static string ApplyStage(Preset preset, int grade)
        {
            LastRun = "";
            if (grade < 1 || grade > StageCount)
            {
                return "프리셋: 없는 단계입니다(" + grade + ").";
            }
            if (FateService.CurrentGrade(Me) < grade)
            {
                return preset.Name + ": " + grade + " " + StageName(grade) + " 단계는 경지 미달입니다. 적용하지 않았습니다.";
            }
            return Apply(preset, grade + " " + StageName(grade), grade == 1, grade, grade, grade);
        }

        // 지금 대경지까지: 선천운명, 대경지 2~지금의 역천개명, 지금 대경지의 공법·심법, 지금 대경지가 허용하는 법보·신혼·명우.
        // 낮은 단계의 공법은 만들지 않는다(경지마다 사본이 쌓이지 않게).
        public static string ApplyAll(Preset preset)
        {
            int current = FateService.CurrentGrade(Me);
            return Apply(preset, "지금 경지(" + StageName(current) + ")까지", true, FateService.FirstGrade, current, current);
        }

        // 경지를 표의 마지막 줄(등선 후기)까지 올리고 전부 적용한다.
        public static string ApplyFull(Preset preset)
        {
            LastRun = "";
            string refusal = Refusal(preset);
            if (refusal != null)
            {
                return refusal;
            }
            ConfRoleGradeItem last = GradeService.LastRow();
            var saved = Me.data.unitData.propertyData;
            string raised = "";
            if (last != null && saved.gradeID != last.id)
            {
                raised = GradeService.RaiseTo(Me, last.id) + " ";
                if (saved.gradeID != last.id)
                {
                    // 올리지 못했으면 적용하지 않는다.
                    return raised.TrimEnd();
                }
            }
            else
            {
                // 이미 등선 후기다. 예전 판의 경지 올리기로 올라가 선력·선법이 빠진 인물이면 채운다.
                string settled = GradeService.Settle(Me);
                raised = settled == null ? "" : settled + " ";
            }
            return raised + ApplyAll(preset);
        }

        // 대경지 grade의 초기까지 경지를 올린다(적용은 하지 않는다).
        public static string RaiseToStage(int grade)
        {
            ConfRoleGradeItem row = GradeService.FirstRowOf(grade);
            return row == null ? "경지 올리기: 게임에 없는 대경지(" + grade + ")입니다." : GradeService.RaiseTo(Me, row.id);
        }

        // ---- 바꾸게 될 것 ----

        // 역천개명·신혼·명우는 하나만 가질 수 있어, 지금 것이 프리셋과 다르면 바꾸게 된다. 그 설명을 돌려준다(없으면 null).
        // 화면은 이것이 있을 때만 두 번 눌러 확인을 묻는다.
        private static string Replaces(Preset preset, int fateFrom, int fateTo, int grade)
        {
            if (preset.Error != null)
            {
                return null;
            }
            WorldUnitBase me = Me;
            var parts = new List<string>();
            Dictionary<int, int> records = FateService.Records(me);
            int fates = 0;
            foreach (FateLine line in preset.Fates)
            {
                int record;
                if (line.Problem == null && line.Grade >= fateFrom && line.Grade <= fateTo
                    && records.TryGetValue(line.Grade, out record) && record != 0 && record != line.Id)
                {
                    fates++;
                }
            }
            if (fates > 0)
            {
                parts.Add("역천개명 " + fates + "개");
            }
            if (preset.Elder != null && preset.Elder.Problem == null && grade >= ElderService.FirstGrade)
            {
                int elder = ElderService.Current(me);
                if (elder != 0 && elder != preset.Elder.Id)
                {
                    parts.Add("신혼(" + ElderService.Name(elder) + ")");
                }
            }
            if (preset.Wingman != null && preset.Wingman.Problem == null && grade >= WingmanService.FirstGrade)
            {
                int wing = WingmanService.Current(me);
                if (wing != 0 && !WingmanService.Has(me, preset.Wingman.Id, preset.Wingman.Effects, preset.Wingman.Prefixes))
                {
                    parts.Add("명우(" + WingmanService.Name(wing) + ")");
                }
            }
            return parts.Count == 0 ? null : string.Join(", ", parts.ToArray());
        }

        public static string ReplacesStage(Preset preset, int grade)
        {
            return Replaces(preset, grade, grade, grade);
        }

        // 최종 단계까지 적용할 때 바꾸게 될 것: 모든 대경지의 역천개명과 신혼·명우.
        public static string ReplacesFull(Preset preset)
        {
            return Replaces(preset, FateService.FirstGrade, StageCount, StageCount);
        }

        public static string ReplacesAll(Preset preset)
        {
            int current = FateService.CurrentGrade(Me);
            return Replaces(preset, FateService.FirstGrade, current, current);
        }
    }
}
