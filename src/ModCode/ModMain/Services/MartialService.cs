using MelonLoader;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnhollowerBaseLib;

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
                case MartialType.SkillLeft: return "무예/영술";
                case MartialType.SkillRight: return "절기";
                case MartialType.Step: return "신법";
                case MartialType.Ability: return "심법";
                case MartialType.Ultimate: return "신통";
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

        // 번역 키 → 다듬은 설명 한 줄(줄바꿈을 공백으로). 여러 탭이 같은 꼴로 쓰던 것을 모았다(10 문서 R3, 묶음 5).
        public static string CleanLine(string key)
        {
            return CleanDesc(GameTool.LS(key)).Replace('\n', ' ');
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
        internal static int TopRawOfLevel(DataProps.MartialData martial, int index, int level)
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

        // 옵션 행의 자리별 가중치(weight1~weight11). 0이면 그 자리에 나올 수 없다. SkillCatalog.RemapPrefixNumbers도 쓴다.
        internal static int Weight(ConfBattleSkillPrefixValueItem item, int index)
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
            return SlotBlocked(item, index) ? SlotReason : RuleBlockReason(martial, index, item);
        }

        public const string SlotReason = MartialRules.SlotReason;

        // 그 자리의 가중치가 0이라 게임이 그 자리에 만들지 않는 옵션인지(스펙 2026-10-20-trainer-batch2-design.md 5.1절: 강제로 넣을 수 있다).
        public static bool SlotBlocked(ConfBattleSkillPrefixValueItem item, int index)
        {
            return Weight(item, index) <= 0;
        }

        // 자리 판정을 뺀 규칙: 중복 한도와 선행·배타 조건. 지킬 수 없으면 이유, 지키면 null.
        public static string RuleBlockReason(DataProps.MartialData martial, int index, ConfBattleSkillPrefixValueItem item)
        {
            List<PrefixView> current = Prefixes(martial);
            var numbers = new List<int>();
            foreach (PrefixView view in current)
            {
                numbers.Add(view.Index == index ? item.number : view.Number);
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

        // force: 자리 판정만 건너뛴다(05 문서 67절 E1: 넣은 옵션은 저장 후에도 남고 전투에서 예외가 없다. 효과는 재지 않았다).
        public static string ReplacePrefix(WorldUnitBase unit, string soleId, int index, ConfBattleSkillPrefixValueItem item, bool force = false)
        {
            DataProps.MartialData martial = Martial(unit, soleId);
            PrefixView before = martial == null ? null : Find(martial, index);
            if (before == null)
            {
                return "옵션을 찾지 못했습니다.";
            }
            string blocked = force ? RuleBlockReason(martial, index, item) : ReplaceBlockReason(martial, index, item);
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
            bool forced = force && SlotBlocked(item, index);
            ChangeLog.Add(EditService.NameOf(unit), Name(martial) + " 옵션" + (index + 1) + (forced ? " (자리 강제)" : ""),
                before.Number + "번(품질 " + before.Level + ")", after.Number + "번(품질 " + after.Level + ")", verified);
            return "옵션" + (index + 1) + " 교체: " + before.Number + "번 -> " + after.Number + "번, 품질 " + after.Level;
        }

        // 장착 칸에 들어 있는지.
        public static bool IsEquipped(WorldUnitBase unit, string soleId)
        {
            var data = unit.data.unitData;
            if (data.skillLeft == soleId || data.skillRight == soleId || data.ultimate == soleId || data.step == soleId)
            {
                return true;
            }
            for (int i = 0; data.abilitys != null && i < data.abilitys.Length; i++)
            {
                if (data.abilitys[i] == soleId)
                {
                    return true;
                }
            }
            return false;
        }

        // 장착한다. 심법은 빈 칸 가운데 첫 칸에, 나머지는 하나뿐인 칸에 넣는다.
        public static string Equip(WorldUnitBase unit, string soleId)
        {
            var data = unit.data.unitData;
            DataUnit.ActionMartialData action = data.GetActionMartial(soleId);
            if (action == null)
            {
                return "공법을 찾지 못했습니다.";
            }
            DataProps.MartialData martial = action.data.To<DataProps.MartialData>();
            int index = 0;
            if (martial.martialType == MartialType.Ability)
            {
                index = -1;
                for (int i = 0; data.abilitys != null && i < data.abilitys.Length; i++)
                {
                    if (string.IsNullOrEmpty(data.abilitys[i]))
                    {
                        index = i;
                        break;
                    }
                }
                if (index < 0)
                {
                    return Name(martial) + ": 심법 칸이 모두 차 있습니다. 먼저 하나를 해제하세요.";
                }
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int code = unit.CreateAction(new UnitActionMartialEquip(action, index), false);
            bool ok = IsEquipped(unit, soleId);
            ChangeLog.Add(EditService.NameOf(unit), Name(martial) + " 장착", "해제", ok ? "장착" : "해제", ok);
            return ok ? "장착: " + Name(martial) : Name(martial) + ": 게임이 장착하지 않았습니다(코드 " + code + "). 경지나 칸 수 조건일 수 있습니다.";
        }

        public static string Unequip(WorldUnitBase unit, string soleId)
        {
            DataUnit.ActionMartialData action = unit.data.unitData.GetActionMartial(soleId);
            if (action == null)
            {
                return "공법을 찾지 못했습니다.";
            }
            DataProps.MartialData martial = action.data.To<DataProps.MartialData>();
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int code = unit.CreateAction(new UnitActionMartialUnequip(action), false);
            bool ok = !IsEquipped(unit, soleId);
            ChangeLog.Add(EditService.NameOf(unit), Name(martial) + " 해제", "장착", ok ? "해제" : "장착", ok);
            return ok ? "해제: " + Name(martial) : Name(martial) + ": 게임이 해제하지 않았습니다(코드 " + code + ").";
        }

        // 값 배열(머리 세 칸 뒤: 종류, 기술 번호, 경지, 옵션 번호들, -1, 옵션 품질들)로 공법을 만들어 배운 상태로 넣는다.
        // 실측: 이렇게 만든 공법은 값·옵션·품질이 원본과 같다(docs/superpowers/specs/2026-10-13-character-export-design.md 3절).
        // 실패하면 이유를, 넣었으면 null을 돌려준다. 게임이 알아보지 못해 깨진 것은 바로 지운다(읽는 것만으로 예외가 난다).
        public static string AddFromValues(WorldUnitBase unit, int[] tail, int exp, int expectBaseId)
        {
            string soleId;
            return AddFromValues(unit, tail, exp, expectBaseId, out soleId);
        }

        // 위와 같고, 넣은 공법의 고유 번호를 돌려준다. 넣지 못했으면 soleId는 null이다.
        public static string AddFromValues(WorldUnitBase unit, int[] tail, int exp, int expectBaseId, out string soleId)
        {
            soleId = null;
            if (tail == null || tail.Length < 3)
            {
                return "값이 모자랍니다";
            }
            var data = unit.data.unitData;
            int before = data.allActionMartial.Count;
            string made = null;
            string error;
            try
            {
                var values = new Il2CppStructArray<int>(tail.Length);
                for (int i = 0; i < tail.Length; i++)
                {
                    values[i] = tail[i];
                }
                DataProps.PropsData props = DataProps.PropsData.New(0, 1, DataProps.PropsDataType.Martial, values);
                made = props.soleID;
                data.StudyActionMartialProp(props);
                if (data.allActionMartial.Count != before + 1)
                {
                    error = "게임이 배우게 하지 않았습니다";
                }
                else
                {
                    DataProps.MartialData martial = data.GetActionMartial(made).data.To<DataProps.MartialData>();
                    if (martial.baseID == expectBaseId && !string.IsNullOrEmpty(martial.martialInfo.name))
                    {
                        if (exp > 0)
                        {
                            data.GetActionMartial(made).exp = exp;
                        }
                        soleId = made;
                        return null;
                    }
                    error = "게임이 알아보지 못하는 공법이라 넣지 않았습니다";
                }
            }
            catch (Exception e)
            {
                error = "게임이 받지 않았습니다(" + e.GetType().Name + ")";
            }
            // 실패한 경로는 어느 것이든, 만들던 공법이 목록에 들어가 있으면 지운다(게임이 넣은 뒤에 예외를 던졌을 수도 있다).
            try
            {
                if (made != null && data.allActionMartial.Count != before)
                {
                    data.DelActionMartial(made);
                }
            }
            catch (Exception)
            {
            }
            return data.allActionMartial.Count == before ? error : error + ". 깨진 공법이 남았을 수 있습니다. 저장하지 말고 공법 탭을 확인하세요";
        }

        // 익힌 공법의 옵션 번호를 자리 순서대로.
        public static int[] OptionNumbers(DataProps.MartialData martial)
        {
            List<PrefixView> views = Prefixes(martial);
            views.Sort((a, b) => a.Index.CompareTo(b.Index));
            var numbers = new int[views.Count];
            for (int i = 0; i < views.Count; i++)
            {
                numbers[i] = views[i].Number;
            }
            return numbers;
        }

        // 옵션 번호 목록(자리 순서)을 그 기술에 쓸 수 없는 이유. 쓸 수 있으면 null.
        // 규칙은 옵션 교체(ReplaceBlockReason)와 같다: 그 기술의 번호, 자리의 가중치, 중복 한도, 선행(양수)·배타(음수) 조건.
        // 실측: 규칙 없이도 게임은 12개까지 만들어 준다. 설정 표에 자리의 가중치가 11번째까지만 있어 11개로 막는다.
        // anySlot: 자리 판정만 건너뛴다(프리셋 적용. 편집기가 자리 밖에 넣은 옵션을 내보낸 파일이 다시 적용되게. 스펙 7.1절).
        // slotFrom: 자리 판정을 이 자리부터만 한다(대도까지 채우기: 옛 옵션은 자리 밖이어도 인정, 05 문서 83절).
        public static string OptionProblem(MartialType type, int baseId, int[] options, bool anySlot = false, int slotFrom = 0)
        {
            // 게임 행을 PrefixRow로 바꾸고 판정은 MartialRules(순수. 문구는 그대로. 05 문서 93절).
            var rows = new List<PrefixRow>();
            var items = g.conf.battleSkillPrefixValue.GetItems((int)type, baseId);
            for (int i = 0; items != null && i < items.Count; i++)
            {
                rows.Add(RowOf(items[i]));
            }
            return MartialRules.OptionProblem(options, rows, anySlot, slotFrom);
        }

        // 게임의 옵션 행을 순수 판정이 쓰는 레코드로.
        internal static PrefixRow RowOf(ConfBattleSkillPrefixValueItem item)
        {
            var weights = new int[11];
            for (int i = 0; i < 11; i++)
            {
                weights[i] = Weight(item, i);
            }
            return new PrefixRow { Number = item.number, Multi = item.multi, Weights = weights, Requires = Requires(item).ToArray() };
        }

        // 옵션을 지정해 공법을 만들어 배운 상태로 넣고, 옵션마다 게임이 허용하는 최고 품질로 맞춘다.
        // 넣었으면 null과 고유 번호를, 아니면 이유를 돌려준다. 규칙을 어긴 옵션으로는 만들지 않는다.
        public static string Make(WorldUnitBase unit, MartialType type, int baseId, int grade, int[] options, out string soleId, bool anySlot = false)
        {
            soleId = null;
            string problem = OptionProblem(type, baseId, options, anySlot);
            if (problem != null)
            {
                return problem;
            }
            var tail = new List<int> { (int)type, baseId, grade };
            if (options.Length > 0)
            {
                tail.AddRange(options);
                tail.Add(-1);
                for (int i = 0; i < options.Length; i++)
                {
                    tail.Add(1);
                }
            }
            string made;
            string error = AddFromValues(unit, tail.ToArray(), 0, baseId, out made);
            if (error != null)
            {
                return error;
            }
            try
            {
                DataProps.MartialData martial = Martial(unit, made);
                int[] read = OptionNumbers(martial);
                bool same = martial.grade == grade && read.Length == options.Length;
                for (int i = 0; same && i < read.Length; i++)
                {
                    same = read[i] == options[i];
                }
                if (same)
                {
                    foreach (PrefixView view in Prefixes(martial))
                    {
                        int raw = TopRawOfLevel(martial, view.Index, view.MaxLevel);
                        if (raw >= 0)
                        {
                            martial.SetPrefixLevel(view.Index, raw);
                        }
                    }
                    soleId = made;
                    return null;
                }
            }
            catch (Exception)
            {
            }
            // 게임이 다르게 읽었거나 읽다가 예외가 났다. 남기지 않는다.
            try
            {
                unit.data.unitData.DelActionMartial(made);
            }
            catch (Exception)
            {
            }
            return "게임이 옵션이나 경지를 다르게 읽어 넣지 않았습니다";
        }

        // 심법 비급의 세트 번호. 세트가 없거나 심법이 아니면 0.
        public static int SuitOf(DataProps.PropsData props)
        {
            try
            {
                DataProps.PropsAbilityData ability = props.To<DataProps.PropsAbilityData>();
                return ability == null ? 0 : ability.suitID;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // 비급 값 문자열이 세트 번호만 든 기록({"suitID":번호})인지(13 문서 R1: 세 곳이 같은 정규식을 쓰던 것).
        private static bool IsPlainSuitRecord(string values)
        {
            return Regex.IsMatch(values, "^\\{\"suitID\":\\d+\\}$");
        }

        // 심법 비급에 세트 번호를 쓴다. 실측: 세트 번호는 값 배열이 아니라 valuesStr의 JSON {"suitID":번호}에 있고,
        // 장착한 심법의 종류가 아니라 이 번호로 세트가 켜진다(프리셋 스펙 3절). 다른 글이 적혀 있으면 건드리지 않는다.
        public static bool SetSuit(DataProps.PropsData props, int suitId)
        {
            try
            {
                if (props.To<DataProps.MartialData>().martialType != MartialType.Ability)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            string now = props.valuesStr ?? "";
            if (now.Length > 0 && !IsPlainSuitRecord(now))
            {
                return false;
            }
            props.valuesStr = "{\"suitID\":" + suitId + "}";
            return SuitOf(props) == suitId;
        }

        // 그 종류의 장착 칸(무예·절기·신법·신통)에 든 공법의 고유 번호. 비어 있으면 null.
        public static string EquippedIn(WorldUnitBase unit, MartialType type)
        {
            var data = unit.data.unitData;
            string sole = type == MartialType.SkillLeft ? data.skillLeft
                : type == MartialType.SkillRight ? data.skillRight
                : type == MartialType.Step ? data.step
                : type == MartialType.Ultimate ? data.ultimate : null;
            return string.IsNullOrEmpty(sole) ? null : sole;
        }

        // 심법의 유형(battleAbilityBase.type, 1~8). 심법이 아니면 0. 유형마다 하나만 장착할 수 있다.
        public static int AbilityType(int baseId)
        {
            ConfBattleAbilityBaseItem conf = g.conf.battleAbilityBase.GetItem(baseId);
            return conf == null ? 0 : conf.type;
        }

        // 장착한 심법 가운데 그 유형의 것. 없으면 null.
        public static string EquippedAbility(WorldUnitBase unit, int abilityType)
        {
            var data = unit.data.unitData;
            for (int i = 0; data.abilitys != null && i < data.abilitys.Length; i++)
            {
                if (string.IsNullOrEmpty(data.abilitys[i]))
                {
                    continue;
                }
                DataProps.MartialData martial = Martial(unit, data.abilitys[i]);
                if (martial != null && AbilityType(martial.baseID) == abilityType)
                {
                    return data.abilitys[i];
                }
            }
            return null;
        }

        // 익힌 공법을 지운다(게임의 기술 망각 행동).
        public static string Forget(WorldUnitBase unit, string soleId)
        {
            var data = unit.data.unitData;
            DataUnit.ActionMartialData action = data.GetActionMartial(soleId);
            if (action == null)
            {
                return "공법을 찾지 못했습니다.";
            }
            string name;
            try
            {
                name = Name(action.data.To<DataProps.MartialData>());
            }
            catch (Exception e)
            {
                name = "(읽을 수 없음 " + e.GetType().Name + ")";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            try
            {
                unit.CreateAction(new UnitActionMartialDel(action), false);
            }
            catch (Exception e)
            {
                // 깨진 공법은 게임의 삭제 행동이 읽다가 예외를 낼 수 있다. 장착 칸이 없어진 번호를 가리키지 않게 먼저 비우고 목록에서 직접 뺀다.
                MelonLogger.Msg("[TlToybox] 공법 삭제 행동 오류(fallback) " + soleId + ": " + e.GetType().Name + ": " + e.Message);
                if (IsEquipped(unit, soleId))
                {
                    try
                    {
                        unit.CreateAction(new UnitActionMartialUnequip(action), false);
                    }
                    catch (Exception)
                    {
                    }
                    if (IsEquipped(unit, soleId))
                    {
                        return name + ": 장착 칸을 비우지 못해 지우지 않았습니다. 게임 화면에서 장착을 해제한 뒤 다시 하세요.";
                    }
                }
                data.DelActionMartial(soleId);
            }
            bool ok = !data.allActionMartial.ContainsKey(soleId);
            ChangeLog.Add(EditService.NameOf(unit), "공법 " + name, "익힘", ok ? "없음" : "익힘", ok);
            return ok ? "공법 망각: " + name : name + ": 게임이 지우지 않았습니다.";
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

        // ---- 전승(심법 세트). 트레이너 3차, 스펙 2026-10-23-trainer3-design.md 5.4·6절 ----

        public sealed class SuitView
        {
            public int Id;
            public string Name;
            public List<string> Members = new List<string>();      // 구성원 심법 이름(표 순서)
            public List<string> MembersHeld = new List<string>();  // 대상 인물의 심법 가운데 이 세트 번호를 가진 것
            public int Claim1, Claim2;
            public string Desc1, Desc2;
            public bool HasCondition;
            public bool Current;                                   // 고른 심법의 지금 세트
        }

        private static string AbilityName(int baseId)
        {
            ConfBattleAbilityBaseItem conf = g.conf.battleAbilityBase.GetItem(baseId);
            return conf == null ? baseId.ToString() : EditService.Compact(GameTool.LS(conf.name));
        }

        private static string SuitText(string key)
        {
            return string.IsNullOrEmpty(key) || key == "0" ? "" : CleanLine(key);
        }

        // 깨진 공법을 건너뛴 첫 번만 로그(그 뒤는 조용히).
        private static bool brokenWarned;
        internal static void BrokenSkipped(string where, Exception e)
        {
            if (brokenWarned)
            {
                return;
            }
            brokenWarned = true;
            MelonLogger.Msg("[TlToybox] 깨진 공법 건너뜀(" + where + "): " + e.GetType().Name + ": " + e.Message);
        }

        // 그 심법의 기본 번호가 구성원(suitMember)에 든 세트. 심법이 아니면 빈 목록.
        public static List<SuitView> SuitsFor(WorldUnitBase unit, string soleId)
        {
            var result = new List<SuitView>();
            DataUnit.ActionMartialData action = unit.data.unitData.GetActionMartial(soleId);
            if (action == null)
            {
                return result;
            }
            DataProps.MartialData martial = action.data.To<DataProps.MartialData>();
            if (martial.martialType != MartialType.Ability)
            {
                return result;
            }
            int current = SuitOf(action.data);
            string baseId = martial.baseID.ToString();
            var held = new Dictionary<int, List<string>>();
            foreach (var pair in unit.data.unitData.allActionMartial)
            {
                // 깨진 공법(값이 비어 읽기만으로 예외)이 하나 있어도 전승 화면이 통째로 실패하지 않게 줄 단위로 건너뛴다(13 문서 A11).
                int suit;
                string name;
                try
                {
                    DataProps.MartialData m = pair.Value.data.To<DataProps.MartialData>();
                    if (m == null || m.martialType != MartialType.Ability)
                    {
                        continue;
                    }
                    suit = SuitOf(pair.Value.data);
                    if (suit == 0)
                    {
                        continue;
                    }
                    // 깨진 공법에서 실제로 던지는 곳은 martialInfo 조회(Name 안)다(회차 fs-b-1030의 Player.log). 그것까지 try 안에서 읽는다.
                    name = Name(m);
                }
                catch (Exception e)
                {
                    BrokenSkipped("전승", e);
                    continue;
                }
                List<string> list;
                if (!held.TryGetValue(suit, out list))
                {
                    list = new List<string>();
                    held[suit] = list;
                }
                list.Add(name);
            }
            var all = g.conf.battleAbilitySuitBase._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfBattleAbilitySuitBaseItem conf = all[i];
                string[] members = (conf.suitMember ?? "").Split('|');
                if (Array.IndexOf(members, baseId) < 0)
                {
                    continue;
                }
                var view = new SuitView
                {
                    Id = conf.id,
                    Name = PresetService.SuitName(conf.id),
                    Claim1 = conf.activationClaim1,
                    Claim2 = conf.activationClaim2,
                    Desc1 = SuitText(conf.suitDesc1),
                    Desc2 = SuitText(conf.suitDesc2),
                    HasCondition = !string.IsNullOrEmpty(conf.suitCondition) && conf.suitCondition != "0",
                    Current = conf.id == current,
                };
                foreach (string member in members)
                {
                    int id;
                    if (int.TryParse(member, out id))
                    {
                        view.Members.Add(AbilityName(id));
                    }
                }
                List<string> mine;
                if (held.TryGetValue(conf.id, out mine))
                {
                    view.MembersHeld.AddRange(mine);
                }
                result.Add(view);
            }
            return result;
        }

        public static string ApplySuit(WorldUnitBase unit, string soleId, int suitId)
        {
            DataUnit.ActionMartialData action = unit.data.unitData.GetActionMartial(soleId);
            if (action == null)
            {
                return "공법을 찾지 못했습니다.";
            }
            DataProps.MartialData martial = action.data.To<DataProps.MartialData>();
            string name = Name(martial);
            if (martial.martialType != MartialType.Ability)
            {
                return name + ": 심법에만 전승을 붙입니다.";
            }
            ConfBattleAbilitySuitBaseItem conf = g.conf.battleAbilitySuitBase.GetItem(suitId);
            if (conf == null || Array.IndexOf((conf.suitMember ?? "").Split('|'), martial.baseID.ToString()) < 0)
            {
                return name + ": 이 심법은 그 세트(" + PresetService.SuitName(suitId) + ")의 구성원이 아닙니다.";
            }
            int before = SuitOf(action.data);
            if (before == suitId)
            {
                return name + ": 이미 " + PresetService.SuitName(suitId) + " 전승입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string oldValues = action.data.valuesStr ?? "";
            if (oldValues.Length > 0 && !IsPlainSuitRecord(oldValues))
            {
                return name + ": 다른 정보가 적힌 비급이라 전승을 쓰지 않았습니다.";
            }
            bool written = SetSuit(action.data, suitId);
            RefreshSuits(unit);
            bool ok = written && SuitOf(action.data) == suitId;
            if (!ok)
            {
                // 확인이 어긋나면 쓴 값을 옛 것으로 되돌린다(05 문서 85절. 프리셋 길은 그대로).
                action.data.valuesStr = oldValues;
                RefreshSuits(unit);
                bool back = (action.data.valuesStr ?? "") == oldValues;
                ChangeLog.Add(EditService.NameOf(unit), name + " 전승", before == 0 ? "없음" : PresetService.SuitName(before), "쓰기 실패" + (back ? "(되돌림)" : ""), false);
                return back ? name + ": 전승을 쓰지 못해 옛 값으로 되돌렸습니다." : name + ": 전승을 쓰지 못했고 옛 값으로도 되돌리지 못했습니다(공법 탭에서 확인하세요).";
            }
            ChangeLog.Add(EditService.NameOf(unit), name + " 전승", before == 0 ? "없음" : PresetService.SuitName(before), PresetService.SuitName(suitId), true);
            return name + ": 전승 " + (before == 0 ? "없음" : PresetService.SuitName(before)) + " → " + PresetService.SuitName(suitId) + ".";
        }

        public static string RemoveSuit(WorldUnitBase unit, string soleId)
        {
            DataUnit.ActionMartialData action = unit.data.unitData.GetActionMartial(soleId);
            if (action == null)
            {
                return "공법을 찾지 못했습니다.";
            }
            string name = Name(action.data.To<DataProps.MartialData>());
            string now = action.data.valuesStr ?? "";
            int before = SuitOf(action.data);
            if (before == 0 && now.Length == 0)
            {
                return name + ": 전승이 없습니다.";
            }
            if (!IsPlainSuitRecord(now))
            {
                return name + ": 다른 정보가 적힌 비급이라 전승을 떼지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            action.data.valuesStr = "";
            RefreshSuits(unit);
            bool ok = SuitOf(action.data) == 0;
            ChangeLog.Add(EditService.NameOf(unit), name + " 전승", before == 0 ? "없음" : PresetService.SuitName(before), "없음", ok);
            return !ok ? name + ": 전승을 떼지 못했습니다."
                : before == 0 ? name + ": 빈 전승 기록을 지웠습니다."
                : name + ": 전승 " + KoreanText.Object(PresetService.SuitName(before)) + " 뗐습니다.";
        }

        private static void RefreshSuits(WorldUnitBase unit)
        {
            try
            {
                g.world.suitAbility.UpdateSuitAbilityFeature(unit);
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Msg("[TlToybox] suit: UpdateSuitAbilityFeature failed: " + e.Message);
            }
        }
    }
}
