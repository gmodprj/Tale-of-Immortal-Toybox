using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 경지를 게임의 돌파 절차 없이 올린다. 한 줄씩 올라가며 게임이 돌파 때 더하는 능력치(UnitActionRoleUpGrade.GetAddAttr)를 저장값에 더한다.
    // 정식 돌파와 비교해 찾은 나머지도 한다(docs/05 35절): 줄의 선력(immortalPoint)을 선법의 총량에 더하고, 줄마다 게임의 돌파 마무리
    // (ActionUpGradeComplete: 돌파 임무, 등선 진입의 선법과 숨은 운명, 역천개명 기록 칸)를 부르고, 끝나면 경지에 묶인 칭호를 다시 판정하고
    // 현재 체력·영력·집중력을 최대로 채운다.
    // "경지 번호를 직접 쓰지 않는다"(docs/05 3절)의 두 번째 예외다. 첫 번째는 캐릭터 가져오기이고, 그쪽은 능력치를 더하지 않는다.
    // 근거: docs/superpowers/specs/2026-10-14-preset-tab-design.md 3절(경지)과 5.3절.
    internal static class GradeService
    {
        // GetAddAttr의 키 → AttributeDef.Key. immortalPoint는 능력치가 아니라 선법의 선력 총량(unitData.immortalCard.immortalPoint)에 더한다.
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

        private static void AddImmortalPoint(WorldUnitBase unit, int amount)
        {
            var card = unit.data.unitData.immortalCard;
            if (card != null && amount > 0)
            {
                // 게임의 상한(roleAttributeLimit.immortalPointMax, 40)을 넘기지 않는다.
                card.immortalPoint = Math.Min(g.conf.roleAttributeLimit.immortalPointMax, card.immortalPoint + amount);
            }
        }

        // 게임의 돌파 마무리. 기록은 남기지 않는다(addLog를 참으로 주면 대화 사건이 뜬다. 회차 grade-1014e).
        // 등선 진입에서는 게임의 "선법 획득" 창이 뜬다. 정식 돌파에서도 뜨는 창이다.
        private static void Complete(WorldUnitBase unit, ConfRoleGradeItem row)
        {
            try
            {
                UnitActionRoleUpGrade.ActionUpGradeComplete(unit, row, false);
            }
            catch (Exception)
            {
            }
        }

        // 경지에 묶인 칭호를 게임의 규칙으로 다시 판정한다(정식 돌파가 하는 일. 예: 영단도인은 구령경에서 잃는다).
        private static void DropTitles(WorldUnitBase unit)
        {
            var titles = unit.appellation == null ? null : unit.appellation.allAppellations;
            if (titles == null)
            {
                return;
            }
            // 판정이 칭호를 지우면 목록이 바뀌므로 먼저 떠 둔다.
            var drops = new List<WorldUnitAppellationDrop1>();
            for (int i = 0; i < titles.Count; i++)
            {
                WorldUnitAppellationDrop1 drop = titles[i] == null || titles[i].dropBase == null ? null : titles[i].dropBase.TryCast<WorldUnitAppellationDrop1>();
                if (drop != null)
                {
                    drops.Add(drop);
                }
            }
            foreach (WorldUnitAppellationDrop1 drop in drops)
            {
                try
                {
                    drop.UpdateGrade();
                }
                catch (Exception)
                {
                }
            }
        }

        // 정식 돌파는 현재 체력·영력·집중력을 최대로 채운다.
        private static void Refill(WorldUnitBase unit)
        {
            try
            {
                unit.UpdateConf();
            }
            catch (Exception)
            {
            }
            var saved = unit.data.unitData.propertyData;
            var dyn = unit.data.dynUnitData;
            saved.hp = dyn.hpMax.value;
            saved.mp = dyn.mpMax.value;
            saved.sp = dyn.spMax.value;
        }

        // 지금 경지까지 지나온 단계의 선력 합. 단계마다 품질이 가장 높은 줄의 값이다(선력이 있는 줄은 등선 중기·후기뿐이고 품질이 하나다).
        private static int ImmortalPointUpTo(ConfRoleGradeItem current)
        {
            int sum = 0;
            for (int grade = 1; grade <= current.grade; grade++)
            {
                for (int phase = 1; phase <= 3; phase++)
                {
                    if (grade == current.grade && phase > current.phase)
                    {
                        break;
                    }
                    ConfRoleGradeItem row = RowOf(grade, phase);
                    sum += row == null ? 0 : row.immortalPoint;
                }
            }
            return sum;
        }

        // 예전 판의 경지 올리기나 캐릭터 가져오기로 경지만 올라간 인물에게 빠진 것을 채운다: 선력 총량이 지나온 단계의 합보다 적으면 그 합으로,
        // 등선인데 선법이 하나도 없으면 등선 진입의 마무리를 부른다. 경지에 묶인 칭호도 다시 판정한다. 채운 것이 없으면 null.
        public static string Settle(WorldUnitBase unit)
        {
            var saved = unit.data.unitData.propertyData;
            ConfRoleGradeItem current = g.conf.roleGrade.GetItem(saved.gradeID);
            var card = unit.data.unitData.immortalCard;
            if (current == null || card == null)
            {
                return null;
            }
            var done = new List<string>();
            int want = ImmortalPointUpTo(current);
            ConfRoleGradeItem first = FirstRowOf(LastRow().grade);
            bool noArts = current.grade >= first.grade && (card.cardDatas == null || card.cardDatas.Count == 0);
            if (card.immortalPoint < want || noArts)
            {
                string backupError = SaveBackup.EnsureOnce();
                if (backupError != null)
                {
                    return backupError + " 채우지 않았습니다.";
                }
            }
            if (card.immortalPoint < want)
            {
                done.Add("선력 " + card.immortalPoint + " -> " + want);
                card.immortalPoint = want;
            }
            if (noArts)
            {
                Complete(unit, first);
                done.Add("등선 진입의 선법 " + (card.cardDatas == null ? 0 : card.cardDatas.Count) + "장");
            }
            // 칭호 재판정은 칭호를 지울 수 있다. 채울 것이 없어도 그 앞에 백업한다(리뷰 C5).
            string titleBackup = SaveBackup.EnsureOnce();
            if (titleBackup == null)
            {
                DropTitles(unit);
            }
            string skipped = titleBackup == null ? "" : " " + titleBackup + " 칭호를 재판정하지 않았습니다.";
            if (done.Count == 0)
            {
                return skipped.Length == 0 ? null : skipped.Trim();
            }
            string text = "경지에 따라 빠져 있던 것을 채웠습니다: " + string.Join(", ", done.ToArray()) + "." + skipped;
            ChangeLog.Add(EditService.NameOf(unit), "경지에 따른 보충", "-", string.Join(", ", done.ToArray()), true);
            return text;
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
                string settled = Settle(unit);
                return "경지 올리기: 이미 " + EditService.GradeName(targetId) + " 이상입니다." + (settled == null ? "" : " " + settled);
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
                AddImmortalPoint(unit, row.immortalPoint);
                Complete(unit, row);
            }
            DropTitles(unit);
            Refill(unit);
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
