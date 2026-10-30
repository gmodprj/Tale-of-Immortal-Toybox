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
                // 되돌린 값 배열이 옛 것과 같은지 본다(05 문서 85절).
                bool back = Action(unit, sole) != null && string.Join(",", Array.ConvertAll(Values(action), x => x.ToString())) == string.Join(",", Array.ConvertAll(old, x => x.ToString()));
                ChangeLog.Add(EditService.NameOf(unit), name + " 수련 단계", before, "끼워 넣기 실패", false);
                return name + ": " + error + (back ? ". 되돌렸습니다." : ". 되돌리지 못했을 수 있습니다(공법 탭에서 확인하세요).");
            }
            string after = Stage(unit, sole);
            string newName = MartialService.Name(action.data.To<DataProps.MartialData>());
            int unusedFrom;
            string points = martial.martialType == MartialType.Ability && MartialService.IsEquipped(unit, sole) ? RaisePoints(unit, 0, out unusedFrom) : "";
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
            int pointsFrom = -1;   // 수도 점수를 올렸으면 올리기 전 값(실패 때 되돌림)
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
                    // 옛 수치가 실제로 옮겨졌는지 본다(05 문서 85절).
                    Dictionary<int, int> copied = Raws(fresh.data.To<DataProps.MartialData>());
                    foreach (var pair in oldRaws)
                    {
                        int got;
                        if (error == null && (!copied.TryGetValue(pair.Key, out got) || got != pair.Value))
                        {
                            error = "옛 옵션 수치를 옮기지 못했습니다";
                        }
                    }
                    fresh.exp = exp;
                    if (valuesStr.Length > 0)
                    {
                        fresh.data.valuesStr = valuesStr;
                    }
                    if (error == null && slot >= 0)
                    {
                        unit.CreateAction(new UnitActionMartialUnequip(action), false);
                        if (type == MartialType.Ability)
                        {
                            points = RaisePoints(unit, m.martialInfo.abilityPoint, out pointsFrom);
                        }
                        unit.CreateAction(new UnitActionMartialEquip(fresh, slot), false);
                        if (!MartialService.IsEquipped(unit, made))
                        {
                            error = "새 공법을 장착하지 못했습니다";
                        }
                        else if (EquipSlot(unit, made) != slot)
                        {
                            error = "새 공법이 옛 칸(" + slot + ")이 아닌 칸(" + EquipSlot(unit, made) + ")에 장착됐습니다";
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
                    RemakeRollback(unit, action, made, slot);
                    tried = made;
                }
            }
            if (error != null)
            {
                if (pointsFrom >= 0)
                {
                    points = RestorePoints(unit, pointsFrom);
                }
                return RemakeFailText(unit, name, before, error, tried, sole, slot, points);
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

        // 다시 만들기 실패: 새 것을 지우고 옛 것을 원래 칸에 되돌린다. 예외는 삼킨다(이미 실패한 길이다. RemakeFailText가 결과를 확인한다).
        private static void RemakeRollback(WorldUnitBase unit, DataUnit.ActionMartialData oldAction, string made, int slot)
        {
            try
            {
                var data = unit.data.unitData;
                if (MartialService.IsEquipped(unit, made))
                {
                    unit.CreateAction(new UnitActionMartialUnequip(data.GetActionMartial(made)), false);
                }
                data.DelActionMartial(made);
                if (slot >= 0 && !MartialService.IsEquipped(unit, oldAction.data.soleID))
                {
                    unit.CreateAction(new UnitActionMartialEquip(oldAction, slot), false);
                }
            }
            catch (Exception)
            {
            }
        }

        // 다시 만들기 실패의 결과 문장. 되돌리기가 실제로 됐는지 본다: 새 것이 없고, 옛 것이 원래 칸에 있어야 "그대로"다. 이력에도 남긴다.
        private static string RemakeFailText(WorldUnitBase unit, string name, string before, string error, string tried, string sole, int slot, string points)
        {
            var data = unit.data.unitData;
            bool newGone = tried == null || data.GetActionMartial(tried) == null;
            bool slotBack = slot < 0 || EquipSlot(unit, sole) == slot;
            ChangeLog.Add(EditService.NameOf(unit), name + " 수련 단계", before, "다시 만들기 실패", false);
            string tail = newGone && slotBack ? " 옛 공법은 그대로입니다."
                : (newGone ? "" : " 새 공법(" + tried + ")이 남았을 수 있습니다.") + (slotBack ? "" : " 옛 공법이 원래 칸에 다시 장착되지 않았습니다.") + " 공법 탭에서 확인하세요.";
            return name + ": 다시 만들지 못했습니다(" + error + ")." + tail + points;
        }

        // 장착한(또는 장착할) 심법의 수도 점수가 모자라면 모자란 만큼 올린다(프리셋과 같은 규칙, 게임의 장착 거부 코드 61).
        // extra: 아직 장착하지 않은 심법의 비용. 올렸으면 문구, 아니면 "".
        // raisedFrom: 올렸으면 올리기 전 값, 아니면 -1. 게임이 값을 다르게 남기면 SetAttribute의 문구를 붙인다(05 문서 85절).
        private static string RaisePoints(WorldUnitBase unit, int extra, out int raisedFrom)
        {
            raisedFrom = -1;
            try
            {
                int before, want, now;
                string reply;
                if (!RaiseAbilityPoints(unit, extra, out before, out want, out now, out reply))
                {
                    return "";
                }
                if (now != before)
                {
                    raisedFrom = before;
                }
                return " 수도 점수 " + before + " → " + now + "." + (now == want ? "" : " (" + reply + ")");
            }
            catch (Exception e)
            {
                return " 수도 점수를 맞추지 못했습니다(" + e.GetType().Name + ").";
            }
        }

        // 수도 점수가 모자라면(쓰는 것 + extra > 총량) 모자란 만큼 올린다. 프리셋 적용과 대도까지 채우기가 함께 쓴다(13 문서 R5).
        // 올릴 필요가 없으면 거짓. before: 올리기 전 저장값, want: 올리려던 값, now: 올린 뒤 저장값(게임이 다르게 남길 수 있다), reply: SetAttribute의 문구.
        internal static bool RaiseAbilityPoints(WorldUnitBase unit, int extra, out int before, out int want, out int now, out string reply)
        {
            AttributeDef def = AttributeCatalog.All.Find(d => d.Key == "abilityPoint");
            before = want = now = 0;
            reply = null;
            int used = unit.data.dynUnitData.GetCurUseAbilityPoint() + extra;
            int total = def.DynValue(unit.data.dynUnitData);
            if (used <= total)
            {
                return false;
            }
            before = def.Get(unit.data.unitData.propertyData);
            want = before + (used - total);
            reply = EditService.SetAttribute(unit, def, want);
            now = def.Get(unit.data.unitData.propertyData);
            return true;
        }

        // 다시 만들기가 실패했을 때 올린 수도 점수를 원래 값으로 되돌린다.
        private static string RestorePoints(WorldUnitBase unit, int value)
        {
            try
            {
                AttributeDef def = AttributeCatalog.All.Find(d => d.Key == "abilityPoint");
                EditService.SetAttribute(unit, def, value);
                int now = def.Get(unit.data.unitData.propertyData);
                return now == value ? " 수도 점수는 " + value + "(으)로 되돌렸습니다." : " 수도 점수를 되돌리지 못했습니다(지금 " + now + ").";
            }
            catch (Exception e)
            {
                return " 수도 점수를 되돌리지 못했습니다(" + e.GetType().Name + ").";
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
