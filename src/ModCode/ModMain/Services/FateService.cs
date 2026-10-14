using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 역천개명: 경지마다 하나씩 얻는 운명. 게임의 경지 화면은 운명 목록이 아니라 경지별 기록을 읽어 보여 준다(docs/05 18절).
    // 그래서 운명을 붙이는 것과 경지별 기록을 쓰는 것을 함께 한다.
    internal static class FateService
    {
        public const int FirstGrade = 2; // 역천개명은 축기경(대경지 2)부터 있다

        public class Slot
        {
            public int Grade;
            public string GradeName;
            public int LuckId;      // 0이면 기록 없음
            public string LuckName;
            public string Tips;
        }

        private static Il2CppSystem.Collections.Generic.Dictionary<int, DataWorld.World.PlayerLogData.GradeData> Log(WorldUnitBase unit)
        {
            bool isPlayer = unit.data.unitData.unitID == g.world.playerUnit.data.unitData.unitID;
            return isPlayer ? g.data.world.playerLog.upGrade : unit.data.unitData.npcUpGrade;
        }

        public static int CurrentGrade(WorldUnitBase unit)
        {
            ConfRoleGradeItem item = g.conf.roleGrade.GetItem(unit.data.unitData.propertyData.gradeID);
            return item == null ? 0 : item.grade;
        }

        private static string GradeName(int grade)
        {
            var all = g.conf.roleGrade._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].grade == grade)
                {
                    return GameTool.LS(all[i].gradeName);
                }
            }
            return "경지 " + grade;
        }

        // 경지별 역천개명 기록의 사본(대경지 → 운명 번호).
        // 게임의 운명 추가 행동(UnitActionLuckAdd)은 역천개명 분류의 운명을 "지금 대경지"의 기록에도 적는다(실측: 대경지 3인 인물에게
        // 다른 역천개명을 붙이자 3번 기록의 번호가 그것으로 바뀌었고, 3번의 원래 운명은 효과만 남았다). 운명을 붙이는 쪽은
        // 붙이기 전에 이것을 떠 두고, 붙인 뒤 RestoreRecords로 되돌린다.
        public static Dictionary<int, int> Records(WorldUnitBase unit)
        {
            var result = new Dictionary<int, int>();
            var log = Log(unit);
            if (log != null)
            {
                foreach (var pair in log)
                {
                    result[pair.Key] = pair.Value.luck;
                }
            }
            return result;
        }

        public static void RestoreRecords(WorldUnitBase unit, Dictionary<int, int> records)
        {
            var log = Log(unit);
            if (log == null)
            {
                return;
            }
            foreach (var pair in records)
            {
                if (log.ContainsKey(pair.Key) && log[pair.Key].luck != pair.Value)
                {
                    log[pair.Key].luck = pair.Value;
                }
            }
        }

        // 도달한 경지(축기경부터)의 역천개명 칸.
        public static List<Slot> Slots(WorldUnitBase unit)
        {
            var result = new List<Slot>();
            var log = Log(unit);
            int current = CurrentGrade(unit);
            for (int grade = FirstGrade; grade <= current; grade++)
            {
                var slot = new Slot { Grade = grade, GradeName = GradeName(grade), LuckName = "", Tips = "" };
                if (log != null && log.ContainsKey(grade))
                {
                    slot.LuckId = log[grade].luck;
                    ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(slot.LuckId);
                    if (conf != null)
                    {
                        slot.LuckName = GameTool.LS(conf.name);
                        slot.Tips = MartialService.CleanDesc(GameTool.LS(conf.tips)).Replace('\n', ' ');
                    }
                }
                result.Add(slot);
            }
            return result;
        }

        // 그 경지에서 게임이 뽑을 수 있는 역천개명(g.conf.fateFeature의 경지 범위).
        public static List<LuckService.LuckView> Candidates(int grade, string query)
        {
            var result = new List<LuckService.LuckView>();
            string needle = EditService.Compact(query ?? "");
            var all = g.conf.fateFeature._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].gradeMin > grade || all[i].gradeMax < grade)
                {
                    continue;
                }
                ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(all[i].id);
                if (conf == null)
                {
                    continue;
                }
                var view = new LuckService.LuckView
                {
                    Id = conf.id,
                    Name = GameTool.LS(conf.name),
                    Tips = MartialService.CleanDesc(GameTool.LS(conf.tips)).Replace('\n', ' '),
                    Type = conf.type,
                    Level = conf.level,
                };
                if (view.Name.Length == 0)
                {
                    continue;
                }
                if (needle.Length > 0 && !EditService.Compact(view.Name).Contains(needle) && !EditService.Compact(view.Tips).Contains(needle))
                {
                    continue;
                }
                result.Add(view);
            }
            return result;
        }

        // 그 운명과 함께 둘 수 없는 역천개명을 실제로 가진 다른 대경지. 없으면 0.
        // 게임은 같은 계열(fateFeature.group, 0이 아님)의 역천개명을 하나만 둔다. 같은 계열을 또 붙이면 앞의 운명이 사라지고
        // 경지별 기록만 남아 겉으로는 정상으로 보인다(실측, 프리셋 스펙 3절). 같은 번호를 두 대경지에 둘 수도 없다.
        public static int HeldElsewhere(WorldUnitBase unit, int exceptGrade, int newId)
        {
            ConfFateFeatureItem fate = g.conf.fateFeature.GetItem(newId);
            foreach (var pair in Records(unit))
            {
                if (pair.Key == exceptGrade || pair.Value == 0 || !LuckService.Has(unit, pair.Value))
                {
                    continue;
                }
                ConfFateFeatureItem other = g.conf.fateFeature.GetItem(pair.Value);
                if (pair.Value == newId || (fate != null && other != null && fate.group != 0 && other.group == fate.group))
                {
                    return pair.Key;
                }
            }
            return 0;
        }

        // 그 대경지의 역천개명의 효과를 떼고 기록의 번호를 0으로 한다. 곧 다른 것으로 정할 칸을 먼저 비울 때 쓴다.
        // 번호를 남겨 두면, 그 번호를 다른 대경지에 다시 붙인 뒤 이 칸을 정할 때 Set이 "이전 것"으로 보고 지운다.
        public static bool Release(WorldUnitBase unit, int grade)
        {
            var log = Log(unit);
            if (log == null || !log.ContainsKey(grade))
            {
                return false;
            }
            int id = log[grade].luck;
            if (id != 0 && LuckService.Has(unit, id))
            {
                unit.CreateAction(new UnitActionLuckDel(id), false);
            }
            if (id != 0 && LuckService.Has(unit, id))
            {
                return false;
            }
            log[grade].luck = 0;
            return true;
        }
        // 그 경지의 역천개명을 newId로 정한다. 이전 것이 있으면 효과를 끄고 바꾼다.
        public static string Set(WorldUnitBase unit, int grade, int newId)
        {
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(newId);
            ConfFateFeatureItem fate = g.conf.fateFeature.GetItem(newId);
            if (conf == null || fate == null)
            {
                return "역천개명 " + newId + ": 게임의 역천개명 목록에 없습니다.";
            }
            string name = GameTool.LS(conf.name);
            string gradeName = GradeName(grade);
            if (grade < FirstGrade || grade > CurrentGrade(unit))
            {
                return gradeName + ": 아직 도달하지 않은 경지라 정할 수 없습니다.";
            }
            if (fate.gradeMin > grade || fate.gradeMax < grade)
            {
                return name + ": " + gradeName + "에서 얻을 수 없는 역천개명입니다.";
            }
            if (LuckService.Has(unit, newId))
            {
                return name + ": 이미 가지고 있습니다.";
            }
            int clash = HeldElsewhere(unit, grade, newId);
            if (clash != 0)
            {
                ConfRoleCreateFeatureItem held = g.conf.roleCreateFeature.GetItem(Records(unit)[clash]);
                return name + ": 같은 계열의 역천개명(" + (held == null ? "?" : GameTool.LS(held.name)) + ")을 " + GradeName(clash)
                    + "에 가지고 있어 함께 둘 수 없습니다. 그 경지의 역천개명을 먼저 바꾸세요.";
            }
            var log = Log(unit);
            if (log == null)
            {
                return "역천개명: 경지별 기록을 찾지 못했습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int oldId = log.ContainsKey(grade) ? log[grade].luck : 0;
            // 운명 추가 행동이 다른 경지의 기록을 덮어쓰므로 떠 두었다가 되돌린다(Records의 설명).
            Dictionary<int, int> others = Records(unit);
            others.Remove(grade);
            string oldName = "없음";
            if (oldId != 0)
            {
                ConfRoleCreateFeatureItem oldConf = g.conf.roleCreateFeature.GetItem(oldId);
                oldName = oldConf == null ? oldId.ToString() : GameTool.LS(oldConf.name);
                if (LuckService.Has(unit, oldId))
                {
                    unit.CreateAction(new UnitActionLuckDel(oldId), false);
                }
            }
            unit.CreateAction(new UnitActionLuckAdd(newId), false);
            RestoreRecords(unit, others);
            if (!LuckService.Has(unit, newId))
            {
                ChangeLog.Add(EditService.NameOf(unit), gradeName + " 역천개명", oldName, oldName, false);
                return name + ": 게임이 추가하지 않았습니다.";
            }
            if (log.ContainsKey(grade))
            {
                // 게임이 돌파 때 적어 둔 품질 값은 그대로 둔다.
                log[grade].luck = newId;
            }
            else
            {
                var entry = new DataWorld.World.PlayerLogData.GradeData();
                entry.luck = newId;
                entry.quality = 0;
                log[grade] = entry;
            }
            bool ok = log.ContainsKey(grade) && log[grade].luck == newId && (oldId == 0 || !LuckService.Has(unit, oldId));
            ChangeLog.Add(EditService.NameOf(unit), gradeName + " 역천개명", oldName + "(" + oldId + ")", name + "(" + newId + ")", ok);
            return ok ? gradeName + " 역천개명: " + oldName + " -> " + name : gradeName + " 역천개명: 썼지만 상태가 예상과 다릅니다. 저장하지 말고 확인하세요.";
        }
    }
}
