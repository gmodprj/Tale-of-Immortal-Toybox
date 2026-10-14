using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 가져오기. 줄 하나를 넣는 함수(Put…)가 넣음·건너뜀·못 넣음 가운데 하나와 이유를 돌려주고,
    // 줄·구역·전부 가져오기가 그것을 세어 문장으로 만든다. 게임이 던지는 예외는 줄 단위로 잡는다.
    internal static partial class CharacterExportService
    {
        private enum Outcome { Done, Skipped, Failed }

        // ---- 화면이 상태를 보이는 데 쓰는 것 ----

        public static bool AttrNow(Attr line, out int value)
        {
            value = 0;
            AttributeDef def = AttributeCatalog.All.Find(d => d.Key == line.Key);
            if (def == null)
            {
                return false;
            }
            value = def.Get(Me.data.unitData.propertyData);
            return true;
        }

        public static bool HasMartial(int baseId)
        {
            foreach (var pair in Me.data.unitData.allActionMartial)
            {
                try
                {
                    if (pair.Value.data.To<DataProps.MartialData>().baseID == baseId)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        // 옵션의 수: 값에서 종류·기술 번호·경지 뒤, -1 앞까지.
        public static int OptionCount(MartialLine line)
        {
            int count = 0;
            for (int i = 3; i < line.Values.Length && line.Values[i] != -1; i++)
            {
                count++;
            }
            return count;
        }

        private static ArtService.Kind ArtKind(ArtLine line)
        {
            foreach (ArtService.Kind kind in ArtService.Kinds)
            {
                if (kind.Tab == line.KindName)
                {
                    return kind;
                }
            }
            return null;
        }

        public static bool ArtLearned(ArtLine line)
        {
            ArtService.Kind kind = ArtKind(line);
            return kind != null && ArtService.Learned(kind, line.Id);
        }

        public static string FateState(FateLine line)
        {
            if (line.Grade > FateService.CurrentGrade(Me))
            {
                return "경지 미달";
            }
            foreach (FateService.Slot slot in FateService.Slots(Me))
            {
                if (slot.Grade == line.Grade)
                {
                    // 기록에는 있는데 운명의 효과가 없으면(다른 경로로 지워진 것) 다시 정해야 한다.
                    return slot.LuckId == line.Id && LuckService.Has(Me, line.Id) ? "같음" : "다름";
                }
            }
            return "다름";
        }

        // 줄을 가져올 수 없는 이유(읽기에서 걸린 것과, 게임에 없는 번호). 화면이 줄에 이유를 보이고 버튼을 끄는 데 쓴다. 가져올 수 있으면 null.
        public static string Why(ArtLine line)
        {
            if (line.Problem != null)
            {
                return line.Problem;
            }
            ArtService.Kind kind = ArtKind(line);
            if (kind == null)
            {
                return "모르는 기예 분류입니다";
            }
            return ArtService.Books(kind, "").Find(b => b.Id == line.Id) == null ? "게임에 없는 " + kind.Book + "입니다" : null;
        }

        public static string Why(LuckLine line)
        {
            if (line.Problem != null)
            {
                return line.Problem;
            }
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(line.Id);
            if (conf == null)
            {
                return "게임에 없는 운명입니다";
            }
            if (conf.type == 3)
            {
                return "역천개명 항목에서 가져옵니다";
            }
            return LuckService.IsSystem(line.Id) ? "경지의 진행 표식입니다" : null;
        }

        public static string Why(FateLine line)
        {
            if (line.Problem != null)
            {
                return line.Problem;
            }
            return g.conf.roleCreateFeature.GetItem(line.Id) == null || g.conf.fateFeature.GetItem(line.Id) == null ? "게임의 역천개명 목록에 없습니다" : null;
        }

        // ---- 줄 하나 넣기 ----

        private static string Gone(Pack pack)
        {
            return System.IO.File.Exists(pack.File) ? null : "가져오기: 파일이 없습니다(" + Path.GetFileName(pack.File) + "). 목록을 다시 여세요.";
        }

        private static Outcome PutGrade(Pack pack, out string text)
        {
            if (!pack.HasGrade)
            {
                text = "경지: 파일에 경지가 없습니다";
                return Outcome.Skipped;
            }
            ConfRoleGradeItem conf = g.conf.roleGrade.GetItem(pack.GradeId);
            if (conf == null)
            {
                text = "경지: 게임에 없는 경지 번호(" + pack.GradeId + ")입니다";
                return Outcome.Failed;
            }
            WorldUnitBase me = Me;
            var saved = me.data.unitData.propertyData;
            // 수련치는 그 경지의 다음 단계 수련치를 넘기지 않는다(넘기면 게임이 돌파를 기다리는 상태를 넘어선다).
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(pack.GradeId);
            int wantExp = next == null ? pack.GradeExp : Math.Min(pack.GradeExp, next.exp);
            if (saved.gradeID == pack.GradeId && saved.exp >= wantExp)
            {
                text = "경지: 이미 " + EditService.GradeName(pack.GradeId) + "입니다";
                return Outcome.Skipped;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return Outcome.Failed;
            }
            string before = EditService.GradeName(saved.gradeID) + "(" + saved.gradeID + ", 수련치 " + saved.exp + ")";
            // 경지 번호를 직접 쓴다. 실측: 번호와 수련치만 바뀌고 능력치는 따라오지 않는다(스펙 3절).
            // "경지 ID를 직접 쓰지 않는다"는 규칙의 예외이며 가져오기에서만 쓴다.
            if (saved.gradeID != pack.GradeId)
            {
                saved.SetGradeID(pack.GradeId);
            }
            if (wantExp > saved.exp)
            {
                // 이전 경지의 "난관 봉착" 기운이 남아 있으면 수련치가 오르지 않는다(05 문서 8절). 수련치 채우기와 같이 먼저 지운다.
                EditService.RemoveStaleBottleneck(me);
                saved.AddExp(wantExp - saved.exp);
            }
            bool gradeOk = saved.gradeID == pack.GradeId;
            bool expOk = saved.exp >= wantExp;
            string after = EditService.GradeName(saved.gradeID) + "(" + saved.gradeID + ", 수련치 " + saved.exp + ")";
            ChangeLog.Add(EditService.NameOf(me), "캐릭터 가져오기 경지", before, after, gradeOk && expOk);
            if (!gradeOk)
            {
                text = "경지: 썼지만 번호가 예상과 다릅니다(" + saved.gradeID + ")";
                return Outcome.Failed;
            }
            if (!expOk)
            {
                text = "경지: " + EditService.GradeName(saved.gradeID) + "(으)로 바꿨지만 수련치는 " + saved.exp + "까지만 올랐습니다(파일 " + wantExp + ")";
                return Outcome.Failed;
            }
            text = "경지: " + EditService.GradeName(saved.gradeID) + "(수련치 " + saved.exp + ")로 바꿨습니다. 능력치는 따라오지 않으니 능력치도 가져오세요";
            return Outcome.Done;
        }

        private static Outcome PutAttr(Attr line, out string text)
        {
            string label = line.Name.Length > 0 ? line.Name : line.Key;
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            AttributeDef def = AttributeCatalog.All.Find(d => d.Key == line.Key);
            if (def == null)
            {
                text = label + ": 모르는 능력치(" + line.Key + ")입니다";
                return Outcome.Failed;
            }
            WorldUnitBase me = Me;
            if (def.Get(me.data.unitData.propertyData) == line.Value)
            {
                text = def.Label + ": 이미 " + line.Value + "입니다";
                return Outcome.Skipped;
            }
            // 범위 검사·백업·변경 이력은 능력치 탭과 같은 경로가 한다.
            text = EditService.SetAttribute(me, def, line.Value);
            // 능력치 탭과 같은 기준으로 판정한다: 저장값과 게임의 기준값이 모두 파일의 값이어야 한다.
            return def.Get(me.data.unitData.propertyData) == line.Value && def.DynBase(me.data.dynUnitData) == line.Value ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutMartial(MartialLine line, bool evenIfKnown, out string text)
        {
            string label = (line.Name.Length > 0 ? line.Name : "공법") + "(" + line.BaseId + ")";
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (!evenIfKnown && HasMartial(line.BaseId))
            {
                text = label + ": 이미 익혔습니다";
                return Outcome.Skipped;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return Outcome.Failed;
            }
            WorldUnitBase me = Me;
            string error = MartialService.AddFromValues(me, line.Values, line.Exp, line.BaseId);
            ChangeLog.Add(EditService.NameOf(me), "캐릭터 가져오기 공법 " + label, "없음", error == null ? "익힘" : "없음", error == null);
            text = error == null ? label + ": 익혔습니다(숙련 경험치 " + line.Exp + ")" : label + ": " + error;
            return error == null ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutArt(ArtLine line, out string text)
        {
            string label = (line.Name.Length > 0 ? line.Name : "서적") + "(" + line.Id + ")";
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            ArtService.Kind kind = ArtKind(line);
            if (kind == null)
            {
                text = label + ": 모르는 기예 분류(" + line.KindName + ")입니다";
                return Outcome.Failed;
            }
            ItemCatalog.Entry book = ArtService.Books(kind, "").Find(b => b.Id == line.Id);
            if (book == null)
            {
                text = label + ": 게임에 없는 " + kind.Book + "입니다";
                return Outcome.Failed;
            }
            if (ArtService.Learned(kind, line.Id))
            {
                text = book.Name + ": 이미 배웠습니다";
                return Outcome.Skipped;
            }
            text = ArtService.ForceLearn(kind, book, line.Exp, line.MaxExp);
            return ArtService.Learned(kind, line.Id) ? Outcome.Done : Outcome.Failed;
        }

        private static Outcome PutLuck(LuckLine line, bool born, out string text)
        {
            string label = (line.Name.Length > 0 ? line.Name : "운명") + "(" + line.Id + ")";
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            WorldUnitBase me = Me;
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(line.Id);
            if (conf == null)
            {
                text = label + ": 게임에 없는 운명입니다";
                return Outcome.Failed;
            }
            // 내보내기가 빼는 것은 가져오기도 받지 않는다(손으로 적은 줄). 역천개명 분류를 여기서 붙이면 게임이 지금 경지의 역천개명 기록을 덮어쓴다.
            if (conf.type == 3)
            {
                text = label + ": 역천개명은 역천개명 항목에서 가져옵니다";
                return Outcome.Failed;
            }
            if (LuckService.IsSystem(line.Id))
            {
                text = label + ": 경지의 진행 표식이라 가져오지 않습니다";
                return Outcome.Failed;
            }
            // 가진 운명은 건드리지 않는다(후천 목록에 있는 것을 선천으로 옮기지도 않는다).
            if (LuckService.Has(me, line.Id))
            {
                text = label + ": 이미 가지고 있습니다";
                return Outcome.Skipped;
            }
            text = born ? LuckService.AddBorn(me, line.Id) : LuckService.Add(me, line.Id);
            if (!LuckService.Has(me, line.Id))
            {
                return Outcome.Failed;
            }
            if (!born)
            {
                return Outcome.Done;
            }
            // 선천은 효과뿐 아니라 선천운명 배열에 들어갔는지도 본다(AddBorn이 "상태가 예상과 다릅니다"를 돌려주는 경우).
            var bornLuck = me.data.unitData.propertyData.bornLuck;
            for (int i = 0; i < bornLuck.Length; i++)
            {
                if (bornLuck[i].id == line.Id)
                {
                    return Outcome.Done;
                }
            }
            return Outcome.Failed;
        }

        // pending: 역천개명 구역을 한 번에 가져올 때, 아직 처리하지 않은 파일의 대경지들(FateService.SetAmong). 줄 하나를 가져올 때는 null이다.
        private static Outcome PutFate(FateLine line, List<int> pending, out string text)
        {
            string label = (line.Name.Length > 0 ? line.Name : "역천개명") + "(" + line.Id + ")";
            if (pending != null)
            {
                pending.Remove(line.Grade);
            }
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (g.conf.roleCreateFeature.GetItem(line.Id) == null || g.conf.fateFeature.GetItem(line.Id) == null)
            {
                text = label + ": 게임의 역천개명 목록에 없습니다";
                return Outcome.Failed;
            }
            string state = FateState(line);
            if (state != "다름")
            {
                text = label + ": " + (state == "같음" ? "이미 그 경지의 역천개명입니다" : "아직 도달하지 않은 경지(대경지 " + line.Grade + ")라 건너뜁니다");
                return Outcome.Skipped;
            }
            text = FateService.SetAmong(Me, line.Grade, line.Id, pending);
            return FateState(line) == "같음" ? Outcome.Done : Outcome.Failed;
        }

        // ---- 줄 하나의 가져오기(화면의 줄 버튼) ----

        private delegate Outcome Put(out string text);

        private static string One(Pack pack, Put put)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            try
            {
                string text;
                put(out text);
                return text + ".";
            }
            catch (Exception e)
            {
                return "가져오기: 게임이 받지 않았습니다(" + e.GetType().Name + ").";
            }
        }

        public static string ImportGrade(Pack pack)
        {
            return One(pack, (out string text) => PutGrade(pack, out text));
        }

        public static string ImportAttribute(Pack pack, int index)
        {
            return One(pack, (out string text) => PutAttr(pack.Attributes[index], out text));
        }

        // 줄의 버튼은 이미 익힌 기술이어도 하나 더 넣는다.
        public static string ImportMartial(Pack pack, int index)
        {
            return One(pack, (out string text) => PutMartial(pack.Martials[index], true, out text));
        }

        public static string ImportArt(Pack pack, int index)
        {
            return One(pack, (out string text) => PutArt(pack.Arts[index], out text));
        }

        public static string ImportBornLuck(Pack pack, int index)
        {
            return One(pack, (out string text) => PutLuck(pack.BornLucks[index], true, out text));
        }

        public static string ImportAddLuck(Pack pack, int index)
        {
            return One(pack, (out string text) => PutLuck(pack.AddLucks[index], false, out text));
        }

        public static string ImportFate(Pack pack, int index)
        {
            return One(pack, (out string text) => PutFate(pack.Fates[index], null, out text));
        }

        // ---- 구역과 전부 ----

        // 한 구역의 집계. 끝나면 "공법: 추가 3, 건너뜀 20, 못 넣음 1(이름…)" 같은 문장이 된다.
        private sealed class Tally
        {
            public int Done, Skipped;
            public readonly List<string> Failed = new List<string>();

            public void Count(Put put, string label)
            {
                Outcome outcome;
                string text;
                try
                {
                    outcome = put(out text);
                }
                catch (Exception e)
                {
                    outcome = Outcome.Failed;
                    text = label + ": 게임이 받지 않았습니다(" + e.GetType().Name + ")";
                }
                if (outcome == Outcome.Done)
                {
                    Done++;
                }
                else if (outcome == Outcome.Skipped)
                {
                    Skipped++;
                }
                else
                {
                    // 줄이 돌려준 문장에 이유가 들어 있다("이름: 이유").
                    Failed.Add(string.IsNullOrEmpty(text) ? label : text.TrimEnd('.'));
                }
            }

            public string Sentence(string title, string verb)
            {
                var sb = new StringBuilder(title + ": " + verb + " " + Done + ", 건너뜀 " + Skipped);
                if (Failed.Count > 0)
                {
                    sb.Append(", 못 넣음 ").Append(Failed.Count).Append("(");
                    for (int i = 0; i < Failed.Count && i < 5; i++)
                    {
                        sb.Append(i == 0 ? "" : " / ").Append(Failed[i]);
                    }
                    sb.Append(Failed.Count > 5 ? " 외 " + (Failed.Count - 5) + "개)" : ")");
                }
                return sb.Append(".").ToString();
            }
        }

        private static string Name(string name, string fallback)
        {
            return name.Length > 0 ? name : fallback;
        }

        // 한 구역을 가져온다. failed는 못 넣은 줄이 하나라도 있었는지다(변경 이력의 성공 표시에 쓴다).
        private static string Run(Pack pack, Section section, out bool failed)
        {
            var tally = new Tally();
            string sentence = RunInto(pack, section, tally, out failed);
            failed = failed || tally.Failed.Count > 0;
            return sentence;
        }

        private static string RunInto(Pack pack, Section section, Tally tally, out bool failed)
        {
            failed = false;
            switch (section)
            {
                case Section.Grade:
                    {
                        string text;
                        try
                        {
                            failed = PutGrade(pack, out text) == Outcome.Failed;
                        }
                        catch (Exception e)
                        {
                            failed = true;
                            text = "경지: 게임이 받지 않았습니다(" + e.GetType().Name + ")";
                        }
                        return text + ".";
                    }
                case Section.Attributes:
                    foreach (Attr line in pack.Attributes)
                    {
                        Attr each = line;
                        tally.Count((out string text) => PutAttr(each, out text), Name(each.Name, each.Key));
                    }
                    return tally.Sentence("능력치", "변경");
                case Section.Martials:
                    foreach (MartialLine line in pack.Martials)
                    {
                        MartialLine each = line;
                        tally.Count((out string text) => PutMartial(each, false, out text), Name(each.Name, "공법 " + each.BaseId));
                    }
                    return tally.Sentence("공법", "추가");
                case Section.Arts:
                    foreach (ArtLine line in pack.Arts)
                    {
                        ArtLine each = line;
                        tally.Count((out string text) => PutArt(each, out text), Name(each.Name, "서적 " + each.Id));
                    }
                    return tally.Sentence("기예", "추가");
                case Section.Lucks:
                    foreach (LuckLine line in pack.BornLucks)
                    {
                        LuckLine each = line;
                        tally.Count((out string text) => PutLuck(each, true, out text), Name(each.Name, "운명 " + each.Id));
                    }
                    foreach (LuckLine line in pack.AddLucks)
                    {
                        LuckLine each = line;
                        tally.Count((out string text) => PutLuck(each, false, out text), Name(each.Name, "운명 " + each.Id));
                    }
                    return tally.Sentence("운명", "추가");
                default:
                    var pending = new List<int>();
                    foreach (FateLine line in pack.Fates)
                    {
                        if (line.Problem == null)
                        {
                            pending.Add(line.Grade);
                        }
                    }
                    foreach (FateLine line in pack.Fates)
                    {
                        FateLine each = line;
                        tally.Count((out string text) => PutFate(each, pending, out text), Name(each.Name, "역천개명 " + each.Id));
                    }
                    return tally.Sentence("역천개명", "지정");
            }
        }

        public static string ImportSection(Pack pack, Section section)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            bool failed;
            string result = Run(pack, section, out failed);
            // 경지와 능력치는 줄마다 이력이 남는다(PutGrade, SetAttribute). 나머지 구역은 구역의 결과를 한 줄 남긴다.
            if (section != Section.Grade && section != Section.Attributes)
            {
                ChangeLog.Add(EditService.NameOf(Me), "캐릭터 가져오기(" + pack.Name + ") 구역", "-", result, !failed);
            }
            return result;
        }

        // 전부. 경지를 먼저 바꿔야 역천개명이 파일의 경지까지 적용된다.
        public static string ImportAll(Pack pack)
        {
            string gone = Gone(pack);
            if (gone != null)
            {
                return gone;
            }
            var sb = new StringBuilder(pack.Name + " — ");
            Section[] order = { Section.Grade, Section.Attributes, Section.Lucks, Section.Fates, Section.Martials, Section.Arts };
            bool anyFailed = false;
            foreach (Section section in order)
            {
                bool failed;
                sb.Append(Run(pack, section, out failed)).Append(" ");
                anyFailed = anyFailed || failed;
            }
            string result = sb.ToString().TrimEnd();
            ChangeLog.Add(EditService.NameOf(Me), "캐릭터 가져오기 전부(" + pack.Name + ")", "-", result, !anyFailed);
            return result;
        }
    }
}
