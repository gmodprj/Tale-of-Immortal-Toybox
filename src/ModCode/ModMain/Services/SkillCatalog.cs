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

        // 대상이 이 기술을 몇 개 익혔는지(같은 기술을 옵션만 달리해 여러 개 가질 수 있다).
        public static Dictionary<int, int> LearnedCounts(WorldUnitBase unit, int kindIndex)
        {
            var counts = new Dictionary<int, int>();
            MartialType type = Kinds[kindIndex].Type;
            foreach (var pair in unit.data.unitData.allActionMartial)
            {
                DataProps.MartialData martial = pair.Value.data.To<DataProps.MartialData>();
                if (martial == null || martial.martialType != type)
                {
                    continue;
                }
                int n;
                counts.TryGetValue(martial.baseID, out n);
                counts[martial.baseID] = n + 1;
            }
            return counts;
        }

        // 지금 배울 수 없는 이유. 배울 수 있으면 null.
        public static string BlockReason(Entry entry, int grade)
        {
            return grade < entry.GradeMin || grade > entry.GradeMax
                ? "경지 " + entry.GradeMin + "~" + entry.GradeMax + "의 기술이라 경지 " + grade + "로는 만들 수 없습니다"
                : null;
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

        private static Dictionary<int, int> ownCombines;

        // 무예·영술·절기·신통 가운데 "이 기술 하나만" 나오는 묶음의 번호. 없으면 0.
        private static int OwnCombine(Kind kind, int baseId)
        {
            if (kind.Type == MartialType.Step || kind.Type == MartialType.Ability)
            {
                return 0;
            }
            if (ownCombines == null)
            {
                ownCombines = new Dictionary<int, int>();
                var all = g.conf.battleSkillCombine._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    int id;
                    if (int.TryParse(all[i].baseID, out id) && id > 0 && !ownCombines.ContainsKey(id))
                    {
                        ownCombines[id] = all[i].id;
                    }
                }
            }
            int combine;
            return ownCombines.TryGetValue(baseId, out combine) ? combine : 0;
        }

        private enum Outcome { Rolled, Plain, Refused }

        // 기술 하나를 배운 상태로 넣는다. 옵션은 게임의 뽑기가 정한다: 원하는 기술이 나올 때까지 묶음에서 다시 뽑고,
        // 끝내 나오지 않거나(강제 추가처럼) 뽑기 대상이 아니면 옵션 없는 기술로 만든다.
        private static Outcome LearnNow(WorldUnitBase unit, Kind kind, Entry entry, int grade, bool roll)
        {
            Il2CppStructArray<int> values = null;
            // 그 기술 하나만 나오는 묶음이 있으면(입문 무예·영술 등) 그것을 먼저 쓴다.
            int own = roll ? OwnCombine(kind, entry.BaseId) : 0;
            if (own != 0)
            {
                Il2CppStructArray<int> fixedRoll = RewardTool.GetSkillReward(own, grade, null);
                if (fixedRoll != null && Contains(fixedRoll, entry.BaseId))
                {
                    values = fixedRoll;
                }
            }
            for (int tries = 0; roll && values == null && tries < RollMax; tries++)
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
            // 게임이 알아보지 못하는 조합이면 이름 없는 깨진 기술이 들어간다. 배우게 한 뒤 정보를 읽어 보고,
            // 읽히지 않으면(게임 쪽에서 예외가 난다) 바로 지운다. 배우기 전에는 정상 기술도 정보가 읽히지 않는다.
            data.StudyActionMartialProp(props);
            if (data.allActionMartial.Count != before + 1)
            {
                return Outcome.Refused;
            }
            bool valid;
            try
            {
                DataProps.MartialData made = data.GetActionMartial(props.soleID).data.To<DataProps.MartialData>();
                valid = made.baseID == entry.BaseId && !string.IsNullOrEmpty(made.martialInfo.name);
            }
            catch (System.Exception)
            {
                valid = false;
            }
            if (!valid)
            {
                data.DelActionMartial(props.soleID);
                return Outcome.Refused;
            }            return plain ? Outcome.Plain : Outcome.Rolled;
        }

        // force가 참이면 경지 범위 밖이어도, 그 기술의 범위 안에서 가장 가까운 경지로 만들어 넣는다.
        public static string Learn(WorldUnitBase unit, int kindIndex, Entry entry, int grade, bool force)
        {
            Kind kind = Kinds[kindIndex];
            string blocked = BlockReason(entry, grade);
            if (blocked != null && !force)
            {
                return entry.Name + ": " + blocked + ".";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            // 강제 추가: 게임은 경지 범위 밖의 기술을 만들지 못한다(실측). 범위 안에서 가장 가까운 경지로 만든다.
            if (blocked != null)
            {
                grade = grade < entry.GradeMin ? entry.GradeMin : entry.GradeMax;
            }
            Outcome outcome = LearnNow(unit, kind, entry, grade, true);
            bool ok = outcome != Outcome.Refused;
            ChangeLog.Add(EditService.NameOf(unit), kind.Tab + " " + entry.Name + "(" + entry.BaseId + ", 경지 " + grade + ")" + (blocked != null ? " 강제" : ""), "없음", ok ? "익힘" : "없음", ok);
            if (!ok)
            {
                return entry.Name + ": 게임이 이 기술을 만들지 못했습니다. 추가하지 않았습니다.";
            }
            return kind.Tab + (blocked != null ? " 강제 추가: " : " 추가: ") + entry.Name + " (경지 " + grade + ")"
                + (outcome == Outcome.Plain ? " — 옵션 없이 만들었습니다." : "");
        }

        // 이 분류에서 지금 배울 수 있고 아직 익히지 않은 기술을 모두 넣는다.
        public static string LearnAll(WorldUnitBase unit, int kindIndex, int grade)
        {
            Kind kind = Kinds[kindIndex];
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            Dictionary<int, int> have = LearnedCounts(unit, kindIndex);
            int learned = 0, plain = 0, refused = 0, skipped = 0;
            foreach (Entry entry in All(kindIndex))
            {
                if (have.ContainsKey(entry.BaseId))
                {
                    continue;
                }
                if (BlockReason(entry, grade) != null)
                {
                    skipped++;
                    continue;
                }
                Outcome outcome = LearnNow(unit, kind, entry, grade, true);
                if (outcome == Outcome.Refused)
                {
                    refused++;
                    continue;
                }
                learned++;
                if (outcome == Outcome.Plain)
                {
                    plain++;
                }
            }
            ChangeLog.Add(EditService.NameOf(unit), kind.Tab + " 모두 배우기(경지 " + grade + ")", "", learned + "개 익힘", learned > 0 || refused == 0);
            return kind.Tab + ": " + learned + "개를 익혔습니다(경지 " + grade + ")."
                + (plain > 0 ? " 그중 " + plain + "개는 옵션 없이 만들었습니다." : "")
                + (skipped > 0 ? " 경지 범위 밖 " + skipped + "개는 건너뛰었습니다." : "")
                + (refused > 0 ? " " + refused + "개는 게임이 만들지 못했습니다." : "");
        }
    }
}
