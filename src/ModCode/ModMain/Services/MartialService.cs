using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MOD_sWP6UL
{
    // 공법(무기·영기, 신법, 심법, 절기)의 조회와 편집.
    // 옵션 규칙(위치 가중치, 중복 한도, 선행·배타 조건)은 설정 표 BattleSkillPrefixValue에서 읽는다.
    internal static class MartialService
    {
        public class PrefixView
        {
            public int Index;
            public int Number;
            public int Raw;      // 저장되는 수치(prefixLevel)
            public int Level;    // 게임이 수치에서 계산하는 품질 단계
            public int MaxLevel;
            public string Desc;
        }

        private const int RawSearchMax = 4096;

        public static string TypeLabel(MartialType type)
        {
            switch (type)
            {
                case MartialType.SkillLeft: return "기술(좌)";
                case MartialType.SkillRight: return "기술(우)";
                case MartialType.Step: return "신법";
                case MartialType.Ability: return "심법";
                case MartialType.Ultimate: return "절기";
                default: return type.ToString();
            }
        }

        public static DataProps.MartialData Martial(WorldUnitBase unit, string soleId)
        {
            DataUnit.ActionMartialData action = unit.data.unitData.GetActionMartial(soleId);
            return action == null ? null : action.data.To<DataProps.MartialData>();
        }

        public static string Name(DataProps.MartialData martial)
        {
            return EditService.Compact(martial.martialInfo.name);
        }

        // 옵션 설명의 자리 표시자를 읽을 수 있게 다듬는다. 수치 자리(&...&)는 N으로, 용어 자리($...$)는 번역문으로 바꾼다.
        public static string CleanDesc(string desc)
        {
            if (string.IsNullOrEmpty(desc))
            {
                return "";
            }
            string text = Regex.Replace(desc, @"&[^&]+&", "N");
            text = Regex.Replace(text, @"\$([^$]+)\$", m => GameTool.LS(m.Groups[1].Value));
            return Regex.Replace(text, @"<[^>]+>", "");
        }

        public static List<PrefixView> Prefixes(DataProps.MartialData martial)
        {
            var result = new List<PrefixView>();
            ConfBattleSkillPrefixValue conf = g.conf.battleSkillPrefixValue;
            var shown = conf.GetPrefix(martial);
            for (int i = 0; i < shown.Count; i++)
            {
                SkillPrefixData data = shown[i];
                result.Add(new PrefixView
                {
                    Index = data.index,
                    Number = data.prefixValueItem.number,
                    Raw = data.prefixLevel,
                    Level = data.level,
                    MaxLevel = conf.GetPrefixMaxLevel(martial, data.prefixValueItem),
                    Desc = CleanDesc(data.prefixValueItemDesc),
                });
            }
            return result;
        }

        private static PrefixView Find(DataProps.MartialData martial, int index)
        {
            foreach (PrefixView view in Prefixes(martial))
            {
                if (view.Index == index)
                {
                    return view;
                }
            }
            return null;
        }

        // raw 수치를 넣었을 때 게임이 계산하는 품질 단계. 계산식이 옵션마다 달라 게임에 직접 물어본다.
        private static int LevelAt(DataProps.MartialData martial, int index, int raw)
        {
            martial.SetPrefixLevel(index, raw);
            return Find(martial, index).Level;
        }

        // 품질 단계가 level 이상이 되는 가장 작은 수치. 없으면 -1.
        private static int FirstRawAtLeast(DataProps.MartialData martial, int index, int level)
        {
            if (LevelAt(martial, index, RawSearchMax) < level)
            {
                return -1;
            }
            int lo = 0;
            int hi = RawSearchMax;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (LevelAt(martial, index, mid) >= level)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid + 1;
                }
            }
            return lo;
        }

        // 품질 단계 level에 해당하는 수치 구간의 가장 높은 값. 탐색 중 바꾼 수치는 호출한 쪽이 최종값을 다시 쓴다.
        private static int TopRawOfLevel(DataProps.MartialData martial, int index, int level)
        {
            int first = FirstRawAtLeast(martial, index, level);
            if (first < 0)
            {
                return -1;
            }
            int next = FirstRawAtLeast(martial, index, level + 1);
            return next < 0 ? first : next - 1;
        }

        public static string SetQuality(WorldUnitBase unit, string soleId, int index, int level)
        {
            DataProps.MartialData martial = Martial(unit, soleId);
            PrefixView before = martial == null ? null : Find(martial, index);
            if (before == null)
            {
                return "옵션을 찾지 못했습니다.";
            }
            if (level < 1 || level > before.MaxLevel)
            {
                return "옵션 품질: " + level + "은(는) 허용 범위 1~" + before.MaxLevel + " 밖이라 적용하지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int raw = TopRawOfLevel(martial, index, level);
            if (raw < 0)
            {
                martial.SetPrefixLevel(index, before.Raw);
                return "옵션 품질: 품질 " + level + "에 해당하는 수치를 찾지 못해 되돌렸습니다.";
            }
            martial.SetPrefixLevel(index, raw);
            PrefixView after = Find(martial, index);
            bool verified = after.Level == level;
            ChangeLog.Add(EditService.NameOf(unit), Name(martial) + " 옵션" + (index + 1) + " 품질",
                before.Level + "(수치 " + before.Raw + ")", after.Level + "(수치 " + after.Raw + ")", verified);
            return "옵션" + (index + 1) + " 품질: " + before.Level + " -> " + after.Level;
        }

        public static List<ConfBattleSkillPrefixValueItem> ValidItems(DataProps.MartialData martial)
        {
            var result = new List<ConfBattleSkillPrefixValueItem>();
            var items = g.conf.battleSkillPrefixValue.GetItems((int)martial.martialType, martial.baseID);
            for (int i = 0; items != null && i < items.Count; i++)
            {
                result.Add(items[i]);
            }
            return result;
        }

        private static int Weight(ConfBattleSkillPrefixValueItem item, int index)
        {
            switch (index)
            {
                case 0: return item.weight1;
                case 1: return item.weight2;
                case 2: return item.weight3;
                case 3: return item.weight4;
                case 4: return item.weight5;
                case 5: return item.weight6;
                case 6: return item.weight7;
                case 7: return item.weight8;
                case 8: return item.weight9;
                case 9: return item.weight10;
                case 10: return item.weight11;
                default: return 0;
            }
        }

        private static List<int> Requires(ConfBattleSkillPrefixValueItem item)
        {
            var result = new List<int>();
            foreach (string part in (item.require ?? "").Split('|', '_', ','))
            {
                int value;
                if (int.TryParse(part, out value) && value != 0)
                {
                    result.Add(value);
                }
            }
            return result;
        }

        // 옵션 index 자리를 item으로 바꿀 수 없는 이유. 바꿀 수 있으면 null.
        public static string ReplaceBlockReason(DataProps.MartialData martial, int index, ConfBattleSkillPrefixValueItem item)
        {
            List<PrefixView> current = Prefixes(martial);
            var numbers = new List<int>();
            foreach (PrefixView view in current)
            {
                numbers.Add(view.Index == index ? item.number : view.Number);
            }
            if (Weight(item, index) <= 0)
            {
                return "이 자리에는 나올 수 없는 옵션";
            }
            int same = 0;
            foreach (int number in numbers)
            {
                if (number == item.number)
                {
                    same++;
                }
            }
            if (same > item.multi)
            {
                return "중복 한도 " + item.multi + "개 초과";
            }
            // 바꾼 뒤의 조합 전체가 선행(양수)·배타(음수) 조건을 지키는지 본다.
            List<ConfBattleSkillPrefixValueItem> valid = ValidItems(martial);
            foreach (int number in numbers)
            {
                ConfBattleSkillPrefixValueItem conf = valid.Find(v => v.number == number);
                if (conf == null)
                {
                    continue;
                }
                foreach (int require in Requires(conf))
                {
                    if (require > 0 && !numbers.Contains(require))
                    {
                        return "옵션 " + number + "번에 필요한 " + require + "번 옵션이 없음";
                    }
                    if (require < 0 && numbers.Contains(-require))
                    {
                        return "옵션 " + number + "번과 " + (-require) + "번은 함께 둘 수 없음";
                    }
                }
            }
            return null;
        }

        public static string ReplacePrefix(WorldUnitBase unit, string soleId, int index, ConfBattleSkillPrefixValueItem item)
        {
            DataProps.MartialData martial = Martial(unit, soleId);
            PrefixView before = martial == null ? null : Find(martial, index);
            if (before == null)
            {
                return "옵션을 찾지 못했습니다.";
            }
            string blocked = ReplaceBlockReason(martial, index, item);
            if (blocked != null)
            {
                return "옵션 교체: " + blocked + " — 적용하지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            martial.SetPrefixID(index, item.number);
            // 교체하면 게임이 수치를 무작위로 다시 정하므로, 이전 품질(새 옵션의 한도 안에서)로 맞춘다.
            PrefixView replaced = Find(martial, index);
            int level = Math.Min(before.Level, replaced.MaxLevel);
            int raw = TopRawOfLevel(martial, index, Math.Max(1, level));
            martial.SetPrefixLevel(index, raw < 0 ? replaced.Raw : raw);
            PrefixView after = Find(martial, index);
            bool verified = after.Number == item.number;
            ChangeLog.Add(EditService.NameOf(unit), Name(martial) + " 옵션" + (index + 1),
                before.Number + "번(품질 " + before.Level + ")", after.Number + "번(품질 " + after.Level + ")", verified);
            return "옵션" + (index + 1) + " 교체: " + before.Number + "번 -> " + after.Number + "번, 품질 " + after.Level;
        }

        public static string AddExp(WorldUnitBase unit, string soleId, int amount)
        {
            DataUnit.ActionMartialData action = unit.data.unitData.GetActionMartial(soleId);
            if (action == null)
            {
                return "공법을 찾지 못했습니다.";
            }
            if (amount <= 0)
            {
                return "숙련 경험치: 0보다 큰 수만 추가할 수 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int before = action.exp;
            unit.data.unitData.AddMartialExp(soleId, amount);
            int after = action.exp;
            string name = Name(action.data.To<DataProps.MartialData>());
            if (after == before)
            {
                return name + " 숙련 경험치: 게임이 올리지 않았습니다(" + before + " 그대로). 이미 상한이거나 오르지 않는 공법입니다.";
            }
            ChangeLog.Add(EditService.NameOf(unit), name + " 숙련 경험치", before.ToString(), after.ToString(), true);
            return name + " 숙련 경험치: " + before + " -> " + after;
        }
    }
}
