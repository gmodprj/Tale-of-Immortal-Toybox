using System;
using System.Collections.Generic;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 수련 단계(입문~대도)를 대도까지 채운다: 공법에 옵션을 11개까지 더한다(단계 수 = 옵션 수 + 1, 05 문서 76·83절).
    // 스펙 docs/superpowers/specs/2026-10-23-mastery-fill-design.md 6절.
    //   끼워 넣기: 값 배열에 옵션만 더해 PropsData.SetValues(같은 공법. 장착·숙련·전승 그대로, 실측).
    //   다시 만들기: MartialService.Make로 새로 만들고 장착 칸·숙련·전승·옛 수치를 옮긴 뒤 옛 것을 지운다.
    // 자리 판정은 새 자리에만, 중복 한도·선행·배타는 옛 옵션 + 새 옵션 전체에(OptionProblem의 slotFrom).
    internal static class MasteryService
    {
        public sealed class State
        {
            public int Options;
            public int Stages;
            public string LastStage = "";
            public string Block;   // 더할 수 없는 까닭. 더할 수 있으면 null
        }

#if DEVTOOLS
        public static bool FailNextMake;   // 개발 전용: 다음 다시 만들기의 Make를 실패로 흉내(검사 service)
        public static bool FailNextEquip;  // 개발 전용: 다음 다시 만들기의 새 공법 장착을 실패로 흉내(만든 뒤 되돌리기 검사)
#endif

        private static DataUnit.ActionMartialData Action(WorldUnitBase unit, string sole)
        {
            return unit == null || sole == null ? null : unit.data.unitData.GetActionMartial(sole);
        }

        private static int[] Values(DataUnit.ActionMartialData action)
        {
            var v = action.data.values;
            var result = new int[v == null ? 0 : v.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = v[i];
            }
            return result;
        }

        public static State Read(WorldUnitBase unit, string sole)
        {
            var state = new State();
            DataUnit.ActionMartialData action = Action(unit, sole);
            if (action == null)
            {
                state.Block = "공법을 찾지 못했습니다.";
                return state;
            }
            DataProps.MartialData martial = action.data.To<DataProps.MartialData>();
            state.Options = MartialService.OptionNumbers(martial).Length;
            try
            {
                var items = g.conf.battleSkillMastery.GetItems(unit, martial);
                state.Stages = items == null ? 0 : items.Count;
                if (state.Stages > 0)
                {
                    state.LastStage = GameTool.LS(items[state.Stages - 1].name);
                }
            }
            catch (Exception)
            {
            }
            if (state.Options >= MasteryRules.MaxOptions)
            {
                state.Block = "이미 옵션이 " + MasteryRules.MaxOptions + "개라 대도까지 있습니다.";
            }
            else if (MartialService.ValidItems(martial).Count == 0)
            {
                state.Block = "이 기술은 붙일 수 있는 옵션이 없습니다.";
            }
            else
            {
                string problem = MartialService.OptionProblem(martial.martialType, martial.baseID, MartialService.OptionNumbers(martial), false, state.Options);
                if (problem != null)
                {
                    state.Block = "지금 옵션이 규칙을 어기고 있어 더할 수 없습니다: " + problem;
                }
                else if (Candidates(unit, sole, new List<int>()).Count == 0)
                {
                    state.Block = "다음 자리(" + (state.Options + 1) + ")에 넣을 수 있는 옵션이 없습니다.";
                }
            }
            return state;
        }

        // 다음 자리(옛 옵션 수 + pending 수)에 넣을 수 있는 옵션 행: 그 자리 가중치 > 0이고 전체 조합이 규칙을 지키는 것.
        public static List<ConfBattleSkillPrefixValueItem> Candidates(WorldUnitBase unit, string sole, List<int> pending)
        {
            var result = new List<ConfBattleSkillPrefixValueItem>();
            DataUnit.ActionMartialData action = Action(unit, sole);
            if (action == null)
            {
                return result;
            }
            DataProps.MartialData martial = action.data.To<DataProps.MartialData>();
            var now = new List<int>(MartialService.OptionNumbers(martial));
            int slot = now.Count + pending.Count;
            if (slot >= MasteryRules.MaxOptions)
            {
                return result;
            }
            now.AddRange(pending);
            int oldCount = now.Count - pending.Count;
            foreach (ConfBattleSkillPrefixValueItem item in MartialService.ValidItems(martial))
            {
                if (MartialService.Weight(item, slot) <= 0)
                {
                    continue;
                }
                var trial = new List<int>(now) { item.number };
                if (MartialService.OptionProblem(martial.martialType, martial.baseID, trial.ToArray(), false, oldCount) == null)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        // 자리마다 가중치 비례로 하나씩. 후보가 없는 자리에서 멈춘다.
        public static List<int> Roll(WorldUnitBase unit, string sole, Random rng)
        {
            var picked = new List<int>();
            while (true)
            {
                var cands = Candidates(unit, sole, picked);
                if (cands.Count == 0)
                {
                    return picked;
                }
                int slot = Read(unit, sole).Options + picked.Count;
                var weights = cands.ConvertAll(c => MartialService.Weight(c, slot));
                int index = MasteryRules.PickWeighted(weights, rng.NextDouble());
                if (index < 0)
                {
                    return picked;
                }
                picked.Add(cands[index].number);
            }
        }

        // add가 지금 공법에 규칙대로 더해질 수 있는지. 되면 null.
        private static string AddProblem(DataProps.MartialData martial, List<int> add)
        {
            if (add == null || add.Count == 0)
            {
                return "더할 옵션이 없습니다.";
            }
            var now = new List<int>(MartialService.OptionNumbers(martial));
            if (now.Count + add.Count > MasteryRules.MaxOptions)
            {
                return "옵션은 " + MasteryRules.MaxOptions + "개까지입니다.";
            }
            int oldCount = now.Count;
            now.AddRange(add);
            return MartialService.OptionProblem(martial.martialType, martial.baseID, now.ToArray(), false, oldCount);
        }

        private static void TopNew(DataProps.MartialData martial, int from)
        {
            foreach (MartialService.PrefixView view in MartialService.Prefixes(martial))
            {
                if (view.Index < from)
                {
                    continue;
                }
                int raw = MartialService.TopRawOfLevel(martial, view.Index, view.MaxLevel);
                martial.SetPrefixLevel(view.Index, raw >= 0 ? raw : 1);
            }
        }

        private static Dictionary<int, int> Raws(DataProps.MartialData martial)
        {
            var result = new Dictionary<int, int>();
            foreach (MartialService.PrefixView view in MartialService.Prefixes(martial))
            {
                result[view.Index] = view.Raw;
            }
            return result;
        }

        private static string Stage(WorldUnitBase unit, string sole)
        {
            State s = Read(unit, sole);
            return s.Stages + "/12 " + s.LastStage;
        }

        public static string Insert(WorldUnitBase unit, string sole, List<int> add)
        {
            DataUnit.ActionMartialData action = Action(unit, sole);
            if (action == null)
            {
                return "공법을 찾지 못했습니다.";
            }
            DataProps.MartialData martial = action.data.To<DataProps.MartialData>();
            string name = MartialService.Name(martial);
            string problem = AddProblem(martial, add);
            if (problem != null)
            {
                return name + ": " + problem;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int[] old = Values(action);
            int[] oldOptions = MartialService.OptionNumbers(martial);
            Dictionary<int, int> oldRaws = Raws(martial);
            string before = Stage(unit, sole);
            string error = null;
            try
            {
                int[] next = MasteryRules.InsertOptions(old, add.ToArray());
                action.data.SetValues(ToIl2Cpp(next));
                martial = action.data.To<DataProps.MartialData>();
                TopNew(martial, oldOptions.Length);
                int[] read = MartialService.OptionNumbers(martial);
                var want = new List<int>(oldOptions);
                want.AddRange(add);
                bool same = Action(unit, sole) != null && string.Join(",", Array.ConvertAll(read, x => x.ToString())) == string.Join(",", want.ConvertAll(x => x.ToString()).ToArray());
                foreach (var pair in oldRaws)
                {
                    same = same && Raws(martial).ContainsKey(pair.Key) && Raws(martial)[pair.Key] == pair.Value;
                }
                if (!same)
                {
                    error = "게임이 옵션을 다르게 읽었습니다";
                }
            }
            catch (Exception e)
            {
                error = "게임이 받지 않았습니다(" + e.GetType().Name + ")";
            }
            if (error != null)
            {
                try
                {
                    action.data.SetValues(ToIl2Cpp(old));
                }
                catch (Exception)
                {
                }
                ChangeLog.Add(EditService.NameOf(unit), name + " 수련 단계", before, "끼워 넣기 실패", false);
                return name + ": " + error + ". 되돌렸습니다.";
            }
            string after = Stage(unit, sole);
            string newName = MartialService.Name(action.data.To<DataProps.MartialData>());
            string points = martial.martialType == MartialType.Ability && MartialService.IsEquipped(unit, sole) ? RaisePoints(unit, 0) : "";
            ChangeLog.Add(EditService.NameOf(unit), name + " 수련 단계", before, after + " (끼워 넣기 +" + add.Count + ")", true);
            return newName + ": 옵션 " + oldOptions.Length + " → " + (oldOptions.Length + add.Count) + ", 수련 단계 " + before + " → " + after + "(끼워 넣기)." + points;
        }

        public static string Remake(WorldUnitBase unit, string sole, List<int> add, out string newSole)
        {
            newSole = null;
            DataUnit.ActionMartialData action = Action(unit, sole);
            if (action == null)
            {
                return "공법을 찾지 못했습니다.";
            }
            DataProps.MartialData martial = action.data.To<DataProps.MartialData>();
            string name = MartialService.Name(martial);
            string problem = AddProblem(martial, add);
            if (problem != null)
            {
                return name + ": " + problem;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            var data = unit.data.unitData;
            MartialType type = martial.martialType;
            int baseId = martial.baseID, grade = martial.grade;
            int[] oldOptions = MartialService.OptionNumbers(martial);
            Dictionary<int, int> oldRaws = Raws(martial);
            int exp = action.exp;
            int crazy = action.crazyID;
            string valuesStr = action.data.valuesStr ?? "";
            int slot = EquipSlot(unit, sole);
            string before = Stage(unit, sole);
            var all = new List<int>(oldOptions);
            all.AddRange(add);
            string made = null;
            string tried = null;   // 만들었다가 되돌린 새 공법(되돌리기 확인용)
            string points = "";
            string error;
#if DEVTOOLS
            if (FailNextMake)
            {
                FailNextMake = false;
                error = "개발 흉내: 만들기 실패";
            }
            else
#endif
            {
                error = MartialService.Make(unit, type, baseId, grade, all.ToArray(), out made, true);
            }
            if (error == null)
            {
                try
                {
                    DataUnit.ActionMartialData fresh = data.GetActionMartial(made);
                    DataProps.MartialData m = fresh.data.To<DataProps.MartialData>();
                    foreach (var pair in oldRaws)
                    {
                        m.SetPrefixLevel(pair.Key, pair.Value);
                    }
                    fresh.exp = exp;
                    if (valuesStr.Length > 0)
                    {
                        fresh.data.valuesStr = valuesStr;
                    }
                    if (slot >= 0)
                    {
                        unit.CreateAction(new UnitActionMartialUnequip(action), false);
                        if (type == MartialType.Ability)
                        {
                            points = RaisePoints(unit, m.martialInfo.abilityPoint);
                        }
                        unit.CreateAction(new UnitActionMartialEquip(fresh, slot), false);
                        if (!MartialService.IsEquipped(unit, made))
                        {
                            error = "새 공법을 장착하지 못했습니다";
                        }
#if DEVTOOLS
                        if (FailNextEquip)
                        {
                            FailNextEquip = false;
                            error = "개발 흉내: 장착 실패";
                        }
#endif
                    }
                    if (error == null && fresh.exp != exp)
                    {
                        error = "숙련 경험치를 옮기지 못했습니다";
                    }
                }
                catch (Exception e)
                {
                    error = "옮기다 실패했습니다(" + e.GetType().Name + ")";
                }
                if (error != null)
                {
                    // 새 것을 지우고 옛 것을 원래 칸에 되돌린다.
                    try
                    {
                        if (MartialService.IsEquipped(unit, made))
                        {
                            unit.CreateAction(new UnitActionMartialUnequip(data.GetActionMartial(made)), false);
                        }
                        data.DelActionMartial(made);
                        if (slot >= 0 && !MartialService.IsEquipped(unit, sole))
                        {
                            unit.CreateAction(new UnitActionMartialEquip(action, slot), false);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    tried = made;
                }
            }
            if (error != null)
            {
                // 되돌리기가 실제로 됐는지 본다: 새 것이 없고, 옛 것이 원래 칸에 있어야 "그대로"다.
                bool newGone = tried == null || data.GetActionMartial(tried) == null;
                bool slotBack = slot < 0 || EquipSlot(unit, sole) == slot;
                ChangeLog.Add(EditService.NameOf(unit), name + " 수련 단계", before, "다시 만들기 실패", false);
                string tail = newGone && slotBack ? " 옛 공법은 그대로입니다."
                    : (newGone ? "" : " 새 공법(" + tried + ")이 남았을 수 있습니다.") + (slotBack ? "" : " 옛 공법이 원래 칸에 다시 장착되지 않았습니다.") + " 공법 탭에서 확인하세요.";
                return name + ": 다시 만들지 못했습니다(" + error + ")." + tail + points;
            }
            try
            {
                if (MartialService.IsEquipped(unit, sole))
                {
                    unit.CreateAction(new UnitActionMartialUnequip(action), false);
                }
                data.DelActionMartial(sole);
            }
            catch (Exception)
            {
            }
            newSole = made;
            bool oldGone = data.GetActionMartial(sole) == null;
            RefreshSuits(unit);
            string after = Stage(unit, made);
            string newName = MartialService.Name(data.GetActionMartial(made).data.To<DataProps.MartialData>());
            ChangeLog.Add(EditService.NameOf(unit), name + " 수련 단계", before, after + " (다시 만들기 +" + add.Count + ", 새 " + made + ")", oldGone);
            return newName + ": 옵션 " + oldOptions.Length + " → " + all.Count + ", 수련 단계 " + before + " → " + after + "(다시 만들기"
                + (slot >= 0 ? ", 장착 옮김" : "") + ")." + points + (oldGone ? "" : " 옛 공법을 지우지 못했습니다(공법 탭에서 확인).")
                + (crazy != 0 ? " 주화입마 기록(crazyID " + crazy + ")은 옮기지 않았습니다." : "");
        }

        // 장착한(또는 장착할) 심법의 수도 점수가 모자라면 모자란 만큼 올린다(프리셋과 같은 규칙, 게임의 장착 거부 코드 61).
        // extra: 아직 장착하지 않은 심법의 비용. 올렸으면 문구, 아니면 "".
        private static string RaisePoints(WorldUnitBase unit, int extra)
        {
            try
            {
                AttributeDef def = AttributeCatalog.All.Find(d => d.Key == "abilityPoint");
                int used = unit.data.dynUnitData.GetCurUseAbilityPoint() + extra;
                int total = def.DynValue(unit.data.dynUnitData);
                if (used <= total)
                {
                    return "";
                }
                int before = def.Get(unit.data.unitData.propertyData);
                EditService.SetAttribute(unit, def, before + (used - total));
                return " 수도 점수 " + before + " → " + def.Get(unit.data.unitData.propertyData) + ".";
            }
            catch (Exception e)
            {
                return " 수도 점수를 맞추지 못했습니다(" + e.GetType().Name + ").";
            }
        }

        // 장착 칸: 심법은 abilitys의 자리, 그 밖은 0. 장착하지 않았으면 -1.
        private static int EquipSlot(WorldUnitBase unit, string sole)
        {
            var data = unit.data.unitData;
            if (data.skillLeft == sole || data.skillRight == sole || data.ultimate == sole || data.step == sole)
            {
                return 0;
            }
            for (int i = 0; data.abilitys != null && i < data.abilitys.Length; i++)
            {
                if (data.abilitys[i] == sole)
                {
                    return i;
                }
            }
            return -1;
        }

        private static void RefreshSuits(WorldUnitBase unit)
        {
            try
            {
                g.world.suitAbility.UpdateSuitAbilityFeature(unit);
            }
            catch (Exception)
            {
            }
        }

        private static Il2CppStructArray<int> ToIl2Cpp(int[] values)
        {
            var arr = new Il2CppStructArray<int>(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                arr[i] = values[i];
            }
            return arr;
        }
    }
}
