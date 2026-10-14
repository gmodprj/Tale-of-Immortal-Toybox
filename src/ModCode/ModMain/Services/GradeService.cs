using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 경지를 게임의 돌파 절차 없이 올린다. 한 줄씩 올라가며 게임이 돌파 때 더하는 능력치(UnitActionRoleUpGrade.GetAddAttr)를 저장값에 더한다.
    // "경지 번호를 직접 쓰지 않는다"(docs/05 3절)의 두 번째 예외다. 첫 번째는 캐릭터 가져오기이고, 그쪽은 능력치를 더하지 않는다.
    // 근거: docs/superpowers/specs/2026-10-14-preset-tab-design.md 3절(경지)과 5.3절.
    internal static class GradeService
    {
        // GetAddAttr의 키 → AttributeDef.Key. immortalPoint는 저장되는 곳을 확인하지 못해 더하지 않는다.
        private static readonly Dictionary<string, string> GrowKeys = new Dictionary<string, string>
        {
            { "atk", "attack" }, { "def", "defense" }, { "hpMax", "hpMax" }, { "mpMax", "mpMax" }, { "spMax", "spMax" },
            { "crit", "crit" }, { "critV", "critValue" }, { "guard", "guard" }, { "guardV", "guardValue" },
            { "pfr", "phycicalFree" }, { "mfr", "magicFree" }, { "msp", "moveSpeed" },
        };

        // 결과 문장과 변경 이력에 보일 능력치. 수명은 따로 다룬다(저장은 개월, 화면은 년).
        private static readonly string[] Shown = { "attack", "defense", "hpMax", "mpMax", "abilityPoint" };

        private const int MonthsPerYear = 12;
        private const int LifeMaxMonths = 8000 * MonthsPerYear;   // AttributeCatalog의 수명 상한과 같다

        // 대경지와 단계(1 초기, 2 중기, 3 후기)의 줄. 품질이 여럿이면 가장 높은 것, 같으면 번호가 작은 것. 없으면 null.
        private static ConfRoleGradeItem RowOf(int grade, int phase)
        {
            ConfRoleGradeItem best = null;
            var all = g.conf.roleGrade._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].grade == grade && all[i].phase == phase && (best == null || all[i].quality > best.quality))
                {
                    best = all[i];
                }
            }
            return best;
        }

        public static ConfRoleGradeItem FirstRowOf(int grade)
        {
            return RowOf(grade, 1);
        }

        public static ConfRoleGradeItem LastRow()
        {
            var all = g.conf.roleGrade._allConfList;
            return all.Count == 0 ? null : all[all.Count - 1];
        }

        private static int Order(ConfRoleGradeItem row)
        {
            return row.grade * 10 + row.phase;
        }

        // 지금 줄 다음부터 목표 줄까지 지나는 줄들(목표 포함). 목표가 지금과 같은 단계이거나 낮으면 빈 목록.
        public static List<ConfRoleGradeItem> Path(int fromId, int toId)
        {
            var result = new List<ConfRoleGradeItem>();
            ConfRoleGradeItem from = g.conf.roleGrade.GetItem(fromId);
            ConfRoleGradeItem to = g.conf.roleGrade.GetItem(toId);
            if (from == null || to == null || Order(to) <= Order(from))
            {
                return result;
            }
            for (int grade = from.grade; grade <= to.grade; grade++)
            {
                for (int phase = 1; phase <= 3; phase++)
                {
                    int order = grade * 10 + phase;
                    if (order <= Order(from) || order > Order(to))
                    {
                        continue;
                    }
                    ConfRoleGradeItem row = order == Order(to) ? to : RowOf(grade, phase);
                    if (row != null)
                    {
                        result.Add(row);
                    }
                }
            }
            return result;
        }

        private static AttributeDef Def(string key)
        {
            return AttributeCatalog.All.Find(d => d.Key == key);
        }

        // 저장값에 더한다. 게임의 상한을 넘으면 상한으로 쓰고 그 이름을 capped에 적는다.
        private static void Add(DataUnit.PropertyData saved, ConfRoleAttributeLimit limit, string key, int amount, List<string> capped)
        {
            AttributeDef def = Def(key);
            if (def == null || amount == 0)
            {
                return;
            }
            int want = def.Get(saved) + amount;
            int max = def.Max(limit);
            if (want > max)
            {
                want = max;
                if (!capped.Contains(def.Label))
                {
                    capped.Add(def.Label);
                }
            }
            def.Set(saved, want);
        }

        private static string Values(DataUnit.PropertyData saved)
        {
            var sb = new StringBuilder();
            foreach (string key in Shown)
            {
                AttributeDef def = Def(key);
                sb.Append(sb.Length == 0 ? "" : ", ").Append(def.Label).Append(' ').Append(def.Get(saved));
            }
            return sb.Append(", 수명 ").Append(saved.life / MonthsPerYear).Append("년").ToString();
        }

        // 목표 줄까지 올린다. 능력치는 지나는 줄마다 게임의 돌파 증가분만큼 오른다.
        public static string RaiseTo(WorldUnitBase unit, int targetId)
        {
            var saved = unit.data.unitData.propertyData;
            if (g.conf.roleGrade.GetItem(targetId) == null)
            {
                return "경지 올리기: 게임에 없는 경지 번호(" + targetId + ")입니다.";
            }
            List<ConfRoleGradeItem> path = Path(saved.gradeID, targetId);
            if (path.Count == 0)
            {
                return "경지 올리기: 이미 " + EditService.GradeName(targetId) + " 이상입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int fromId = saved.gradeID;
            var before = new Dictionary<string, int>();
            foreach (string key in Shown)
            {
                before[key] = Def(key).Get(saved);
            }
            int lifeBefore = saved.life;
            string valuesBefore = Values(saved);
            ConfRoleAttributeLimit limit = g.conf.roleAttributeLimit;
            var capped = new List<string>();
            // 이전 경지의 "난관 봉착" 기운이 남으면 수련치가 오르지 않는다(docs/05 8절). 캐릭터 가져오기의 경지와 같이 먼저 지운다.
            EditService.RemoveStaleBottleneck(unit);
            foreach (ConfRoleGradeItem row in path)
            {
                var gains = UnitActionRoleUpGrade.GetAddAttr(row);
                foreach (var pair in gains)
                {
                    if (pair.Key == "life")
                    {
                        saved.life = Math.Min(LifeMaxMonths, saved.life + pair.Value);
                        continue;
                    }
                    string key;
                    if (GrowKeys.TryGetValue(pair.Key, out key))
                    {
                        Add(saved, limit, key, pair.Value, capped);
                    }
                }
                saved.life = Math.Min(LifeMaxMonths, saved.life + row.growAddLife);
                Add(saved, limit, "abilityPoint", row.abilityPoint, capped);
                saved.SetGradeID(row.id);
            }
            bool ok = saved.gradeID == targetId;
            string name = EditService.NameOf(unit);
            ChangeLog.Add(name, "경지 올리기", EditService.GradeName(fromId) + "(" + fromId + ")", EditService.GradeName(saved.gradeID) + "(" + saved.gradeID + ")", ok);
            ChangeLog.Add(name, "경지 올리기 능력치", valuesBefore, Values(saved), ok);
            if (!ok)
            {
                return "경지 올리기: 썼지만 경지 번호가 예상과 다릅니다(" + saved.gradeID + "). 저장하지 말고 확인하세요.";
            }
            var sb = new StringBuilder("경지 올리기: " + EditService.GradeName(fromId) + " -> " + EditService.GradeName(saved.gradeID) + ".");
            foreach (string key in Shown)
            {
                AttributeDef def = Def(key);
                sb.Append(' ').Append(def.Label).Append(' ').Append(before[key]).Append(" -> ").Append(def.Get(saved)).Append(',');
            }
            sb.Append(" 수명 ").Append(lifeBefore / MonthsPerYear).Append(" -> ").Append(saved.life / MonthsPerYear).Append("년.");
            if (capped.Count > 0)
            {
                sb.Append(" 게임의 상한에 걸려 상한으로 쓴 것: ").Append(string.Join(", ", capped.ToArray())).Append('.');
            }
            return sb.Append(" 게임의 돌파 절차는 거치지 않았습니다.").ToString();
        }
    }
}
