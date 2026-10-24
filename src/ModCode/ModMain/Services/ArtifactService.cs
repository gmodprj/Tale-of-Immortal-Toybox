using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 시작 신기 셋(1 쌍어패, 2 호천안, 3 연요호)의 목록, 쌍어옥패 점수, 호천안·연요호의 손상. 세계 자료(g.data.world)에 쓴다.
    // 목록을 비우면 게임이 시작 연출과 신기 선택을 다시 열므로 마지막 하나는 빼지 않는다(docs/07 7.2절, 스펙 3절).
    // 지도 화면의 신기 버튼은 다시 불러와야 바뀐다(UpdateUI로는 켜지지 않았다).
    internal static class ArtifactService
    {
        public static readonly int[] Ids = { 1, 2, 3 };
        public const int PointMax = 9999;
        public const string ButtonNote = "지도 화면의 신기 버튼은 저장 후 다시 불러오면 바뀝니다.";
        public const string LastReason = "신기는 하나 이상 있어야 합니다. 비우면 게임이 시작 연출과 신기 선택을 다시 엽니다.";
        private const string Who = "신기";

        public static bool Ready
        {
            get { return g.data != null && g.data.world != null && g.data.world.animaWeapons != null; }
        }

        public static bool Has(int id)
        {
            return Ready && g.data.world.animaWeapons.Contains((GameAnimaWeapon)id);
        }

        public static int Count
        {
            get { return Ready ? g.data.world.animaWeapons.Count : 0; }
        }

        public static string Listed()
        {
            var parts = new List<string>();
            for (int i = 0; i < Count; i++)
            {
                parts.Add(((int)g.data.world.animaWeapons[i]).ToString());
            }
            return string.Join(",", parts.ToArray());
        }

        internal static string Text(string key)
        {
            return key == null || key == "0" ? "" : MartialService.CleanLine(key);
        }

        public static string Name(int id)
        {
            ConfAnimaWeaponItem item = g.conf.animaWeapon.GetItem(id);
            string name = item == null ? "" : Text(item.name);
            return name.Length > 0 ? name : "신기 " + id;
        }

        public static string Desc(int id)
        {
            ConfAnimaWeaponItem item = g.conf.animaWeapon.GetItem(id);
            return item == null ? "" : Text(item.desc);
        }

        public static string Enable(int id)
        {
            string label = Name(id);
            if (!Ready)
            {
                return label + ": 세계 자료가 없습니다.";
            }
            if (Has(id))
            {
                return label + ": 이미 가지고 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = Listed();
            g.data.world.animaWeapons.Add((GameAnimaWeapon)id);
            bool ok = Has(id);
            ChangeLog.Add(Who, "신기 목록", before, Listed(), ok);
            return ok ? label + ": 켰습니다. " + ButtonNote : label + ": 목록에 들어가지 않았습니다.";
        }

        public static string Disable(int id)
        {
            string label = Name(id);
            if (!Has(id))
            {
                return label + ": 가지고 있지 않습니다.";
            }
            if (Count <= 1)
            {
                return label + ": " + LastReason;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = Listed();
            g.data.world.animaWeapons.Remove((GameAnimaWeapon)id);
            bool ok = !Has(id);
            ChangeLog.Add(Who, "신기 목록", before, Listed(), ok);
            return ok ? label + ": 껐습니다(그 신기의 자료는 남습니다). " + ButtonNote : label + ": 목록에서 빠지지 않았습니다.";
        }

        public static int Point
        {
            get { return Ready ? g.data.world.resurgeCount : 0; }
        }

        public static string SetPoint(int value)
        {
            if (!Ready)
            {
                return "쌍어옥패 점수: 세계 자료가 없습니다.";
            }
            int v = Math.Max(0, Math.Min(PointMax, value));
            int before = Point;
            if (before == v)
            {
                return "쌍어옥패 점수: 이미 " + v + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            g.data.world.resurgeCount = v;
            bool ok = Point == v;
            ChangeLog.Add(Who, "쌍어옥패 점수", before.ToString(), Point.ToString(), ok);
            return ok ? "쌍어옥패 점수: " + before + " → " + v + (v != value ? "(0~" + PointMax + "로 맞춤)" : "") + "." : "쌍어옥패 점수: 바뀌지 않았습니다.";
        }

        private static DataWorld.World.GodEyeData GodEye
        {
            get { return Ready ? g.data.world.godEyeData : null; }
        }

        public static bool GodEyeDamaged
        {
            get
            {
                var e = GodEye;
                return e != null && (e.isDamage || e.IsRepair());
            }
        }

        public static string GodEyeState()
        {
            var e = GodEye;
            if (e == null)
            {
                return "자료 없음";
            }
            return e.isDamage ? "손상" : e.IsRepair() ? "수리 중" : "사용 가능";
        }

        // Repair()는 수리를 맡기는 함수라(3개월 뒤에 끝난다) 쓰지 않고 값을 직접 지운다(스펙 2절).
        public static string RepairGodEye()
        {
            var e = GodEye;
            if (e == null)
            {
                return "호천안: 자료가 없습니다.";
            }
            if (!GodEyeDamaged)
            {
                return "호천안: 손상되지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = GodEyeState();
            e.isDamage = false;
            e.repairMonth = 0;
            bool ok = e.IsCanUse();
            ChangeLog.Add("호천안", "손상", before, GodEyeState(), ok);
            return ok ? "호천안: 손상을 해제했습니다(바로 쓸 수 있습니다)." : "호천안: 값은 지웠지만 게임이 아직 쓸 수 없다고 합니다.";
        }

        private static DevilDemonMgr Devil
        {
            get { return g.world == null ? null : g.world.devilDemonMgr; }
        }

        // 연요호의 손상은 관리자(실행 중의 값)와 세계 자료(저장되는 값) 두 곳에 있다. 둘 다 본다.
        // repairMonth가 지금 달보다 뒤면 수리 중이고 게임도 쓸 수 없다고 한다(IsCanUse=False, 회차 atab10). 지난 달이면 끝난 수리다.
        private static bool DevilBroken
        {
            get
            {
                DevilDemonMgr mgr = Devil;
                return (mgr != null && mgr.devilDemonData != null && mgr.devilDemonData.isDamage)
                    || (Ready && g.data.world.devilDemonData != null && g.data.world.devilDemonData.isDamage);
            }
        }

        private static bool DevilRepairing
        {
            get
            {
                DevilDemonMgr mgr = Devil;
                int month = g.world == null || g.world.run == null ? 0 : g.world.run.roundMonth;
                return (mgr != null && mgr.devilDemonData != null && mgr.devilDemonData.repairMonth > month)
                    || (Ready && g.data.world.devilDemonData != null && g.data.world.devilDemonData.repairMonth > month);
            }
        }

        public static bool DevilDamaged
        {
            get { return DevilBroken || DevilRepairing; }
        }

        public static string DevilState()
        {
            return Devil == null ? "자료 없음" : DevilBroken ? "손상" : DevilRepairing ? "수리 중" : "사용 가능";
        }

        public static string RepairDevil()
        {
            DevilDemonMgr mgr = Devil;
            if (mgr == null)
            {
                return "연요호: 자료가 없습니다.";
            }
            if (!DevilDamaged)
            {
                return "연요호: 손상되지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            if (mgr.devilDemonData != null)
            {
                mgr.devilDemonData.isDamage = false;
                mgr.devilDemonData.repairMonth = 0;
            }
            if (Ready && g.data.world.devilDemonData != null)
            {
                g.data.world.devilDemonData.isDamage = false;
                g.data.world.devilDemonData.repairMonth = 0;
            }
            // 연요호를 가졌으면 게임의 판정(IsCanUse)까지 본다. 없으면 IsCanUse는 늘 거짓이다.
            bool ok = !DevilDamaged && (!Has(3) || mgr.IsCanUse());
            ChangeLog.Add("연요호", "손상", "손상", DevilState(), ok);
            return ok ? "연요호: 손상을 해제했습니다." : "연요호: 손상이 남아 있습니다.";
        }

        // 플레이어의 대경지(1 연기, 2 축기, 3 결정 …). 호천안 요수의 단계와 호요의 단계가 이 번호를 쓴다(스펙 2절).
        public static int PlayerMajor()
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            ConfRoleGradeItem item = me == null ? null : g.conf.roleGrade.GetItem(me.data.unitData.propertyData.gradeID);
            return item == null ? 1 : Math.Max(1, item.grade);
        }

        public static string MajorName(int major)
        {
            ConfRoleGradeItem row = GradeService.FirstRowOf(major);
            string name = row == null ? "" : Text(row.gradeName);
            return name.Length > 0 ? name : major + "단계";
        }
    }
}
