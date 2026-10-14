using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 호천안의 요수와 기술. 목록은 설정 표 godEyeSkills(기술 줄마다 bossID)를 요수로 묶어 만든다. 요수 이름은 battleUnitAttr의 그 번호 줄.
    // 해금은 godEyeData.bossGrade[요수]=단계(그림 위의 경지 글이 된다), 익힘은 skillsID, 장착은 equipSkillID(스펙 2절).
    internal static class GodEyeService
    {
        public sealed class Skill
        {
            public int Id;
            public int BossId;
            public string Name = "";
            public string Desc = "";
        }

        public sealed class Boss
        {
            public int Id;
            public string Name = "";
            public readonly List<Skill> Skills = new List<Skill>();
            public string Search = "";
        }

        private static List<Boss> bosses;

        private static string Normalize(string text)
        {
            return (text ?? "").Replace(" ", "").ToLowerInvariant();
        }

        public static List<Boss> Bosses()
        {
            if (bosses != null)
            {
                return bosses;
            }
            var result = new List<Boss>();
            var byId = new Dictionary<int, Boss>();
            var rows = g.conf.godEyeSkills._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                ConfGodEyeSkillsItem row = rows[i];
                Boss boss;
                if (!byId.TryGetValue(row.bossID, out boss))
                {
                    ConfBattleUnitAttrItem attr = g.conf.battleUnitAttr.GetItem(row.bossID);
                    string name = attr == null ? "" : ArtifactService.Text(attr.name);
                    boss = new Boss { Id = row.bossID, Name = name.Length > 0 ? name : "요수 " + row.bossID };
                    byId[row.bossID] = boss;
                    result.Add(boss);
                }
                int id = row.id;
                if (boss.Skills.Exists(s => s.Id == id))
                {
                    continue;
                }
                string skillName = ArtifactService.Text(row.name);
                boss.Skills.Add(new Skill { Id = id, BossId = row.bossID, Name = skillName.Length > 0 ? skillName : id.ToString(), Desc = ArtifactService.Text(row.desc) });
            }
            foreach (Boss b in result)
            {
                b.Search = Normalize(b.Name + string.Join("", b.Skills.ConvertAll(s => s.Name).ToArray()));
            }
            bosses = result;
            return bosses;
        }

        public static List<Boss> Find(string query)
        {
            string q = Normalize(query);
            return q.Length == 0 ? new List<Boss>(Bosses()) : Bosses().FindAll(b => b.Search.Contains(q));
        }

        public static Boss FindBoss(int id)
        {
            return Bosses().Find(b => b.Id == id);
        }

        public static Skill FindSkill(int id)
        {
            foreach (Boss b in Bosses())
            {
                Skill s = b.Skills.Find(x => x.Id == id);
                if (s != null)
                {
                    return s;
                }
            }
            return null;
        }

        // 서로 다른 기술 번호의 수.
        public static int SkillCount()
        {
            var ids = new HashSet<int>();
            foreach (Boss b in Bosses())
            {
                foreach (Skill s in b.Skills)
                {
                    ids.Add(s.Id);
                }
            }
            return ids.Count;
        }

        private static DataWorld.World.GodEyeData Data
        {
            get { return ArtifactService.Ready ? g.data.world.godEyeData : null; }
        }

        public static bool Unlocked(Boss b)
        {
            var d = Data;
            return b != null && d != null && d.bossGrade != null && d.bossGrade.ContainsKey(b.Id);
        }

        public static int Grade(Boss b)
        {
            return Unlocked(b) ? Data.bossGrade[b.Id] : 0;
        }

        public static bool Learned(int skillId)
        {
            var d = Data;
            return d != null && d.skillsID != null && d.skillsID.Contains(skillId);
        }

        public static int LearnedCount(Boss b)
        {
            int n = 0;
            foreach (Skill s in b.Skills)
            {
                n += Learned(s.Id) ? 1 : 0;
            }
            return n;
        }

        public static int Equipped
        {
            get
            {
                var d = Data;
                return d == null ? 0 : d.equipSkillID;
            }
        }

        private static bool Complete(Boss b)
        {
            return Unlocked(b) && LearnedCount(b) == b.Skills.Count;
        }

        public static string Unlock(Boss b)
        {
            if (b == null)
            {
                return "호천안: 그런 요수가 없습니다.";
            }
            if (!ArtifactService.Has(2))
            {
                return b.Name + ": 호천안이 없습니다.";
            }
            var d = Data;
            if (d == null || d.bossGrade == null || d.skillsID == null)
            {
                return b.Name + ": 호천안 자료가 없습니다.";
            }
            if (Complete(b))
            {
                return b.Name + ": 이미 해금했고 기술을 모두 익혔습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            bool was = Unlocked(b);
            int before = LearnedCount(b);
            int grade = was ? Grade(b) : ArtifactService.PlayerMajor();
            if (!was)
            {
                d.bossGrade[b.Id] = grade;
            }
            foreach (Skill s in b.Skills)
            {
                if (!Learned(s.Id))
                {
                    d.skillsID.Add(s.Id);
                }
            }
            bool ok = Complete(b);
            int added = LearnedCount(b) - before;
            ChangeLog.Add("호천안", "요수 " + b.Name + "(" + b.Id + ")", was ? "해금, 기술 " + before + "/" + b.Skills.Count : "잠김",
                "해금(" + ArtifactService.MajorName(grade) + "), 기술 " + LearnedCount(b) + "/" + b.Skills.Count, ok);
            return ok ? b.Name + ": " + (was ? "이미 해금" : "해금(" + ArtifactService.MajorName(grade) + ")") + ", 기술 " + added + "개 익힘."
                : b.Name + ": 게임 자료에 들어가지 않았습니다.";
        }

        public static string UnlockAll(List<Boss> list)
        {
            if (!ArtifactService.Has(2))
            {
                return "호천안: 호천안이 없습니다.";
            }
            int unlocked = 0, learned = 0, skipped = 0;
            var failed = new List<string>();
            foreach (Boss b in list)
            {
                if (Complete(b))
                {
                    skipped++;
                    continue;
                }
                bool was = Unlocked(b);
                int before = LearnedCount(b);
                string text = Unlock(b);
                if (Complete(b))
                {
                    unlocked += was ? 0 : 1;
                    learned += LearnedCount(b) - before;
                }
                else
                {
                    failed.Add(text);
                }
            }
            return "호천안: 요수 해금 " + unlocked + ", 기술 " + learned + "개 익힘, 건너뜀 " + skipped + "(이미 모두 익힘)"
                + (failed.Count > 0 ? ", 못 함 " + failed.Count + "(" + string.Join(" / ", failed.ToArray()) + ")" : "") + ".";
        }

        public static string Equip(Skill s)
        {
            if (s == null)
            {
                return "호천안: 그런 기술이 없습니다.";
            }
            if (!ArtifactService.Has(2))
            {
                return s.Name + ": 호천안이 없습니다.";
            }
            if (!Learned(s.Id))
            {
                return s.Name + ": 익히지 않은 기술은 장착할 수 없습니다.";
            }
            if (Equipped == s.Id)
            {
                return s.Name + ": 이미 장착 중입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            Skill old = FindSkill(Equipped);
            string before = Equipped == 0 ? "없음" : old == null ? Equipped.ToString() : old.Name;
            Data.equipSkillID = s.Id;
            bool ok = Equipped == s.Id;
            ChangeLog.Add("호천안", "장착 기술", before, s.Name + "(" + s.Id + ")", ok);
            return ok ? s.Name + ": 장착했습니다." : s.Name + ": 장착되지 않았습니다.";
        }
    }
}
