using System.Collections.Generic;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 배울 수 있는 기술의 목록과 추가. 분류는 게임 인물 화면의 기술 탭을 따른다(docs/05 22·23절).
    internal static class SkillCatalog
    {
        public class Kind
        {
            public string Tab;          // 게임 기술 화면의 탭 이름
            public MartialType Type;
            public int Combine;         // 이 분류 전체에서 뽑는 묶음 번호
        }

        public class Entry
        {
            public int BaseId;
            public string Name;
            public string Desc;
            public int GradeMin;
            public int GradeMax;
            public string Search;
        }

        public static readonly Kind[] Kinds =
        {
            new Kind { Tab = "무예/영술", Type = MartialType.SkillLeft, Combine = 101 },
            new Kind { Tab = "절기", Type = MartialType.SkillRight, Combine = 102 },
            new Kind { Tab = "신법", Type = MartialType.Step, Combine = 101 },
            new Kind { Tab = "심법", Type = MartialType.Ability, Combine = 101 },
            new Kind { Tab = "신통", Type = MartialType.Ultimate, Combine = 103 },
        };

        private const int RollMax = 3000;
        private static readonly Dictionary<int, List<Entry>> cache = new Dictionary<int, List<Entry>>();

        private static void Add(List<Entry> list, int id, string nameKey, string descKey, int gradeMin, int gradeMax)
        {
            // 경지 범위가 0~0인 항목은 플레이어가 배우는 기술이 아니다(괴물용·부품).
            string name = EditService.Compact(GameTool.LS(nameKey));
            if (gradeMax < 1 || name.Length == 0 || name == EditService.Compact(nameKey))
            {
                return;
            }
            list.Add(new Entry
            {
                BaseId = id,
                Name = name,
                Desc = MartialService.CleanDesc(GameTool.LS(descKey)).Replace("\r", "").Replace('\n', ' '),
                GradeMin = gradeMin,
                GradeMax = gradeMax,
                Search = name,
            });
        }

        private static List<Entry> All(int kindIndex)
        {
            List<Entry> list;
            if (cache.TryGetValue(kindIndex, out list))
            {
                return list;
            }
            list = new List<Entry>();
            Kind kind = Kinds[kindIndex];
            if (kind.Type == MartialType.Step)
            {
                var all = g.conf.battleStepBase._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    Add(list, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax);
                }
            }
            else if (kind.Type == MartialType.Ability)
            {
                var all = g.conf.battleAbilityBase._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    Add(list, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax);
                }
            }
            else
            {
                // 설정의 type: 1·2는 무예·영술, 3은 절기, 4는 신통.
                var all = g.conf.battleSkillAttack._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    int type = all[i].type;
                    bool match = kind.Type == MartialType.SkillLeft ? (type == 1 || type == 2)
                        : kind.Type == MartialType.SkillRight ? type == 3 : type == 4;
                    if (match)
                    {
                        Add(list, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax);
                    }
                }
            }
            cache[kindIndex] = list;
            return list;
        }

        public static List<Entry> Find(int kindIndex, string query)
        {
            string needle = EditService.Compact(query ?? "");
            int id;
            bool isId = int.TryParse(needle, out id);
            var result = new List<Entry>();
            foreach (Entry entry in All(kindIndex))
            {
                if (needle.Length == 0 || entry.Search.Contains(needle) || (isId && entry.BaseId == id))
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        private static bool Contains(Il2CppStructArray<int> values, int id)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == id)
                {
                    return true;
                }
            }
            return false;
        }

        private static Il2CppStructArray<int> Roll(Kind kind, int grade)
        {
            switch (kind.Type)
            {
                case MartialType.Step: return RewardTool.GetStepReward(kind.Combine, grade, null);
                case MartialType.Ability: return RewardTool.GetAbilityReward(kind.Combine, grade, null);
                default: return RewardTool.GetSkillReward(kind.Combine, grade, null);
            }
        }

        // 기술을 배운 상태로 넣는다. 옵션은 게임의 뽑기가 정한다: 원하는 기술이 나올 때까지 묶음에서 다시 뽑고,
        // 끝내 나오지 않으면 옵션 없는 기술로 만든다.
        public static string Learn(WorldUnitBase unit, int kindIndex, Entry entry, int grade)
        {
            Kind kind = Kinds[kindIndex];
            if (grade < entry.GradeMin || grade > entry.GradeMax)
            {
                return entry.Name + ": 이 기술은 경지 " + entry.GradeMin + "~" + entry.GradeMax + "에서만 만들 수 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            Il2CppStructArray<int> values = null;
            int tries = 0;
            for (; tries < RollMax; tries++)
            {
                Il2CppStructArray<int> rolled = Roll(kind, grade);
                if (rolled != null && Contains(rolled, entry.BaseId)
                    && DataProps.PropsData.NewMartial(kind.Type, rolled).To<DataProps.MartialData>().baseID == entry.BaseId)
                {
                    values = rolled;
                    break;
                }
            }
            bool plain = values == null;
            if (plain)
            {
                // 값의 자리: 3번째가 종류, 4번째가 기술 번호, 5번째가 경지(실측한 옵션 없는 공법의 모양).
                int[] raw = { 2, 0, 1, (int)kind.Type, entry.BaseId, grade };
                values = new Il2CppStructArray<int>(raw.Length);
                for (int i = 0; i < raw.Length; i++)
                {
                    values[i] = raw[i];
                }
            }
            var data = unit.data.unitData;
            int before = data.allActionMartial.Count;
            DataProps.PropsData props = DataProps.PropsData.NewMartial(kind.Type, values);
            // 게임이 알아보지 못하는 조합이면 깨진 기술이 들어가므로, 배우게 하기 전에 확인한다.
            DataProps.MartialData made = props.To<DataProps.MartialData>();
            if (made == null || made.martialInfo == null || made.baseID != entry.BaseId || string.IsNullOrEmpty(made.martialInfo.name))
            {
                return entry.Name + ": 게임이 이 기술을 만들지 못했습니다. 추가하지 않았습니다.";
            }
            data.StudyActionMartialProp(props);
            bool ok = data.allActionMartial.Count == before + 1;
            ChangeLog.Add(EditService.NameOf(unit), kind.Tab + " " + entry.Name + "(" + entry.BaseId + ", 경지 " + grade + ")", "없음", ok ? "익힘" : "없음", ok);
            if (!ok)
            {
                return entry.Name + ": 게임이 추가하지 않았습니다.";
            }
            return kind.Tab + " 추가: " + entry.Name + " (경지 " + grade + ")" + (plain ? " — 뽑기에서 나오지 않아 옵션 없이 만들었습니다." : "");
        }
    }
}
