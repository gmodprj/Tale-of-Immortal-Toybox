using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 연요호의 호요. 종류는 설정 표 potmonBase, 가진 호요는 실행 중의 관리자(g.world.devilDemonMgr.potMonMgr.potMonDatas)에서 읽는다.
    // 추가는 PotMonMgr.AddPotMon(종류, 단계, true), 경험치는 PotmonAddExp(개수당 1000, 다음 단계 경험치에서 멈춘다). 승급은 하지 않는다(스펙 2·3절).
    internal static class PotmonService
    {
        public sealed class Kind
        {
            public int Id;
            public string Name = "";
            public string Race = "";
            public string Search = "";
        }

        public sealed class Pet
        {
            public string SoleId = "";
            public int Id;
            public string Name = "";
            public int Grade;
            public int Exp;
            public int Next;    // 다음 단계 경험치. 가장 높은 단계면 -1
        }

        public const int Cap = 28;          // 게임 화면 "호요 수량: N/28". 출처는 찾지 못했다(스펙 2절)
        public const int ExpPerItem = 1000; // PotmonParamer.potmonPropAddExp
        public const int MaxGrade = 10;

        private static List<Kind> kinds;

        private static PotMonMgr Mgr
        {
            get { return g.world == null || g.world.devilDemonMgr == null ? null : g.world.devilDemonMgr.potMonMgr; }
        }

        public static List<Kind> Kinds()
        {
            if (kinds != null)
            {
                return kinds;
            }
            var result = new List<Kind>();
            var rows = g.conf.potmonBase._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                string name = ArtifactService.Text(rows[i].name);
                var k = new Kind { Id = rows[i].id, Name = name.Length > 0 ? name : "호요 " + rows[i].id, Race = ArtifactService.Text(rows[i].race) };
                k.Search = TextTags.SearchKey(k.Name + k.Race);
                result.Add(k);
            }
            kinds = result;
            return kinds;
        }

        public static List<Kind> Find(string query)
        {
            string q = TextTags.SearchKey(query);
            return q.Length == 0 ? new List<Kind>(Kinds()) : Kinds().FindAll(k => k.Search.Contains(q));
        }

        public static Kind FindKind(int id)
        {
            return Kinds().Find(k => k.Id == id);
        }

        public static int Count
        {
            get
            {
                PotMonMgr m = Mgr;
                return m == null || m.potMonDatas == null ? 0 : m.potMonDatas.Count;
            }
        }

        public static int NextExp(int grade)
        {
            var rows = g.conf.potmonGradeLevel._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].grade == grade + 1)
                {
                    return rows[i].exp;
                }
            }
            return -1;
        }

        public static List<Pet> Owned()
        {
            var result = new List<Pet>();
            PotMonMgr m = Mgr;
            for (int i = 0; m != null && m.potMonDatas != null && i < m.potMonDatas.Count; i++)
            {
                PotMonUnitData u = m.potMonDatas[i].potMonUnitData;
                if (u == null)
                {
                    continue;
                }
                Kind k = FindKind(u.id);
                result.Add(new Pet { SoleId = u.soleId, Id = u.id, Name = k == null ? "호요 " + u.id : k.Name, Grade = u.grade, Exp = u.exp, Next = NextExp(u.grade) });
            }
            return result;
        }

        public static string Add(Kind k, int grade)
        {
            if (k == null)
            {
                return "호요: 그런 종류가 없습니다.";
            }
            string label = "호요 " + k.Name;
            if (!ArtifactService.Has(3))
            {
                return label + ": 연요호가 없습니다.";
            }
            PotMonMgr m = Mgr;
            if (m == null || m.potMonDatas == null)
            {
                return label + ": 연요호 자료가 없습니다.";
            }
            if (grade < 1 || grade > MaxGrade)
            {
                return label + ": 단계는 1~" + MaxGrade + "입니다.";
            }
            int before = Count;
            if (before >= Cap)
            {
                return label + ": 호요는 게임 화면의 상한 " + Cap + "까지 넣습니다(지금 " + before + ").";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            m.AddPotMon(k.Id, grade, true);
            bool ok = Count == before + 1;
            ChangeLog.Add("연요호", "호요 추가", before.ToString(), Count + "(" + k.Name + ", " + ArtifactService.MajorName(grade) + ")", ok);
            return ok ? label + ": 넣었습니다(" + ArtifactService.MajorName(grade) + ", 호요 " + Count + "/" + Cap + ")." : label + ": 게임이 넣지 않았습니다.";
        }

        public static string FillExp(string soleId)
        {
            PotMonMgr m = Mgr;
            PotMonMgr.PotMonData data = m == null ? null : m.GetPotmonFromSoleID(soleId);
            if (data == null || data.potMonUnitData == null)
            {
                return "호요: 찾지 못했습니다.";
            }
            PotMonUnitData u = data.potMonUnitData;
            Kind k = FindKind(u.id);
            string label = "호요 " + (k == null ? u.id.ToString() : k.Name);
            int next = NextExp(u.grade);
            if (next < 0)
            {
                return label + ": 가장 높은 단계입니다.";
            }
            if (u.exp >= next)
            {
                return label + ": 이미 다음 단계 경험치입니다. 승급은 게임 화면에서 합니다(승급 아이템 필요).";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int before = u.exp;
            int count = (next - before + ExpPerItem - 1) / ExpPerItem;
            m.PotmonAddExp(data, count, false);
            int after = data.potMonUnitData.exp;
            bool ok = after >= next;
            ChangeLog.Add("연요호", "호요 " + (k == null ? u.id.ToString() : k.Name) + " 경험치", before.ToString(), after.ToString(), ok);
            return ok ? label + ": 경험치 " + before + " → " + after + "(다음 단계 경계). 승급은 게임 화면에서 합니다."
                : label + ": 경험치가 " + before + " → " + after + "에 그쳤습니다.";
        }
    }
}
