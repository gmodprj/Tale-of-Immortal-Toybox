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
