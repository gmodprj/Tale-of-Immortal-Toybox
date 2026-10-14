using System;

namespace MOD_sWP6UL
{
    // 신혼. 게임의 정식 행동(UnitActionRoleUpGradeElder)은 쓸 수 없어(예외) 데이터에 직접 쓴다(프리셋 스펙 3절).
    internal static class ElderService
    {
        // 단계 표(elderLevel)의 grade가 7부터다. 그 아래에서 id를 쓰고 UpdateConf를 부르면 예외가 난다(실측: 결정 경지).
        public const int FirstGrade = 7;

        public static bool Known(int id)
        {
            return g.conf.elderBase.GetItem(id) != null;
        }

        public static string Name(int id)
        {
            ConfElderBaseItem conf = g.conf.elderBase.GetItem(id);
            return conf == null ? id.ToString() : EditService.Compact(GameTool.LS(conf.name));
        }

        public static int Current(WorldUnitBase unit)
        {
            var elder = unit.data.unitData.elderData;
            return elder == null ? 0 : elder.id;
        }

        // 지금 신혼의 단계. 신혼이 없거나 읽을 수 없으면 0.
        public static int Level(WorldUnitBase unit)
        {
            if (Current(unit) == 0)
            {
                return 0;
            }
            try
            {
                ConfElderLevelItem item = g.conf.elderLevel.GetItem(unit);
                return item == null ? 0 : item.level;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // 그 대경지가 허용하는 가장 높은 단계의 줄. 없으면 null.
        private static ConfElderLevelItem TopRow(int id, int grade)
        {
            ConfElderLevelItem best = null;
            var rows = g.conf.elderLevel.GetItemInElderID(id);
            for (int i = 0; rows != null && i < rows.Count; i++)
            {
                if (rows[i].grade <= grade && (best == null || rows[i].level > best.level))
                {
                    best = rows[i];
                }
            }
            return best;
        }

        // 그 신혼이고, 지금 대경지가 허용하는 최고 단계인가.
        public static bool Has(WorldUnitBase unit, int id)
        {
            ConfElderLevelItem top = TopRow(id, FateService.CurrentGrade(unit));
            return Current(unit) == id && top != null && Level(unit) >= top.level;
        }

        // 신혼을 그 번호로 정하고, 지금 대경지가 허용하는 최고 단계까지 경험치를 올린다. 지금 신혼이 다른 것이면 바꾼다.
        public static string Set(WorldUnitBase unit, int id)
        {
            if (!Known(id))
            {
                return "신혼 " + id + ": 게임에 없는 번호입니다.";
            }
            string label = Name(id);
            int grade = FateService.CurrentGrade(unit);
            if (grade < FirstGrade)
            {
                return "신혼 " + label + ": 화신(대경지 7)부터 쓸 수 있습니다. 쓰지 않았습니다.";
            }
            var elder = unit.data.unitData.elderData;
            if (elder == null)
            {
                return "신혼: 이 인물에게 신혼 자료가 없습니다.";
            }
            if (Has(unit, id))
            {
                return "신혼: 이미 " + label + " " + Level(unit) + "단계입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int oldId = elder.id;
            string before = oldId == 0 ? "없음" : Name(oldId) + " " + Level(unit) + "단계";
            elder.id = id;
            if (elder.elderID == null)
            {
                elder.elderID = new Il2CppSystem.Collections.Generic.List<int>();
            }
            if (!elder.elderID.Contains(id))
            {
                elder.elderID.Add(id);
            }
            // 단계의 exp는 그 단계에 이르는 누적 값이다(실측: 40000에서 2단계, 대경지 9에서 480000이면 7단계).
            ConfElderLevelItem top = TopRow(id, grade);
            if (top != null && elder.exp < top.exp)
            {
                elder.AddExp(top.exp - elder.exp);
            }
            bool updated = true;
            try
            {
                unit.UpdateConf();
            }
            catch (Exception)
            {
                updated = false;
            }
            string name = EditService.NameOf(unit);
            if (!updated)
            {
                // 게임이 받지 않았다. 번호를 되돌리면 복구된다(실측).
                elder.id = oldId;
                try
                {
                    unit.UpdateConf();
                }
                catch (Exception)
                {
                }
                ChangeLog.Add(name, "신혼", before, before, false);
                return "신혼 " + label + ": 게임이 받지 않아 되돌렸습니다.";
            }
            bool ok = Has(unit, id);
            ChangeLog.Add(name, "신혼", before, label + " " + Level(unit) + "단계", ok);
            return ok ? "신혼: " + before + " -> " + label + " " + Level(unit) + "단계"
                : "신혼 " + label + ": 썼지만 단계가 예상과 다릅니다(" + Level(unit) + "단계). 저장하지 말고 확인하세요.";
        }
    }
}
