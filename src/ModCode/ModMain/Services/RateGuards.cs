using System;
using System.Collections.Generic;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 배율 탭의 "소모를 막는" 줄들이 거는 패치(docs/superpowers/specs/2026-10-20-trainer-batch1-design.md 6절, 05 문서 65절).
    //
    // 아이템 소모 없음: 게임의 아이템 소모는 모두 WorldUnitData.CostPropItem(sole|PropsData) → DataProps.DelProps(sole)를 지난다.
    //   제작·영석도 같은 길이라 전체를 막지 않고, "쓰기 행동이 쓰는 sole"만 막는다.
    //   - UnitActionPropUse.OnCreate(지도 단약·보상자·부적, 일부 전투 단약): propsData.soleID
    //   - UnitActionPropMartialStudy.OnCreate(다 배울 때 비급이 없어진다): martialData.data.soleID. 영석 900은 그대로 낸다
    //   - UnitActionRoleUpGrade.OnCreate(대경지 재료·천재지보): requirePropsSoleID, rarityPropsSoleID
    //   - 전투 중: 행동 없이 바로 줄어드는 전투 단약이 있어 플레이어 전투 아이템 칸(playerUnitCtrl.props)의 sole도 막는다.
    //   전투 아이템의 OnCreate(Vector2 인자)에는 패치를 걸지 않는다(걸면 게임이 꺼졌다).
    // 영력·집중력 없음: 기술의 영력 소모와 전투 단약의 집중력 소모가 UnitDataBase.AddMP·AddSP 음수로 들어온다.
    // 법보 내구 소모 없음(05 문서 87절): 전투에서 법보를 처음 쓸 때 PropItemArtifact.OnCreate가 PropsArtifact.set_durable로 내구를 깎는다
    //   (AddDurable은 거치지 않는다). 설정자에 접두를 걸어 플레이어 법보의 줄어드는 쓰기만 막는다. 플레이어 것인지는 soleID로 가린다
    //   (전투 쪽 PropsArtifact 객체는 가방의 To<PropsArtifact>()와 포인터가 다르다).
    internal static class RateGuards
    {
        private static readonly HashSet<string> usingSoles = new HashSet<string>();   // 지금 도는 쓰기 행동이 쓰는 아이템
        private static bool warned;

        public static void PatchItemUse(HarmonyLib.Harmony h)
        {
            Type pd = typeof(DataProps.PropsData);
            Hook(h, HarmonyLib.AccessTools.Method(typeof(UnitActionPropUse), "OnCreate", Type.EmptyTypes), "UseBegin", "ActionEnd");
            Hook(h, HarmonyLib.AccessTools.Method(typeof(UnitActionPropMartialStudy), "OnCreate", Type.EmptyTypes), "StudyBegin", "ActionEnd");
            Hook(h, HarmonyLib.AccessTools.Method(typeof(UnitActionRoleUpGrade), "OnCreate", Type.EmptyTypes), "UpGradeBegin", "ActionEnd");
            Hook(h, HarmonyLib.AccessTools.Method(typeof(WorldUnitData), "CostPropItem", new[] { typeof(string), typeof(int), typeof(bool) }), "CostSoleGuard", null);
            Hook(h, HarmonyLib.AccessTools.Method(typeof(WorldUnitData), "CostPropItem", new[] { pd, typeof(bool) }), "CostPdGuard", null);
        }

        public static void PatchDrain(HarmonyLib.Harmony h)
        {
            Hook(h, HarmonyLib.AccessTools.Method(typeof(UnitDataBase), "AddMP", new[] { typeof(int), typeof(bool) }), "AddMpGuard", null);
            Hook(h, HarmonyLib.AccessTools.Method(typeof(UnitDataBase), "AddSP", new[] { typeof(int), typeof(bool) }), "AddSpGuard", null);
        }

        private static void Hook(HarmonyLib.Harmony h, System.Reflection.MethodInfo target, string prefix, string postfix)
        {
            if (target == null)
            {
                throw new MissingMethodException(prefix);
            }
            var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public;
            h.Patch(target, new HarmonyLib.HarmonyMethod(typeof(RateGuards).GetMethod(prefix, flags)),
                postfix == null ? null : new HarmonyLib.HarmonyMethod(typeof(RateGuards).GetMethod(postfix, flags)));
        }

        private static bool IsPlayer(WorldUnitBase unit)
        {
            return unit != null && g.world != null && g.world.playerUnit != null && unit.Pointer == g.world.playerUnit.Pointer;
        }

        private static void Warn(Exception e)
        {
            if (!warned)
            {
                warned = true;
                MelonLogger.Msg("[TlToybox] 소모 막기 실패: " + e);
            }
        }

        // 앞 행동이 예외로 끝나 남은 것이 있어도 새 행동마다 먼저 비운다.
        public static void UseBegin(UnitActionPropUse __instance)
        {
            usingSoles.Clear();
            try
            {
                if (RateService.ItemUse.On && IsPlayer(__instance.unit) && __instance.propsData != null)
                {
                    usingSoles.Add(__instance.propsData.soleID);
                }
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        public static void StudyBegin(UnitActionPropMartialStudy __instance)
        {
            usingSoles.Clear();
            try
            {
                if (RateService.ItemUse.On && IsPlayer(__instance.unit) && __instance.martialData != null && __instance.martialData.data != null)
                {
                    usingSoles.Add(__instance.martialData.data.soleID);
                }
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        public static void UpGradeBegin(UnitActionRoleUpGrade __instance)
        {
            usingSoles.Clear();
            try
            {
                if (RateService.ItemUse.On && IsPlayer(__instance.unit))
                {
                    AddAll(__instance.requirePropsSoleID);
                    AddAll(__instance.rarityPropsSoleID);
                }
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        private static void AddAll(UnhollowerBaseLib.Il2CppStringArray soles)
        {
            for (int i = 0; soles != null && i < soles.Length; i++)
            {
                usingSoles.Add(soles[i]);
            }
        }

        public static void ActionEnd()
        {
            usingSoles.Clear();
        }

        public static bool CostSoleGuard(WorldUnitData __instance, string __0)
        {
            return !Blocked(__instance, __0);
        }

        public static bool CostPdGuard(WorldUnitData __instance, DataProps.PropsData __0)
        {
            return __0 == null || !Blocked(__instance, __0.soleID);
        }

        private static bool Blocked(WorldUnitData data, string sole)
        {
            if (!RateService.ItemUse.On || sole == null)
            {
                return false;
            }
            try
            {
                if (!IsPlayer(data.unit))
                {
                    return false;
                }
                return usingSoles.Contains(sole) || InBattleSlot(sole);
            }
            catch (Exception e)
            {
                Warn(e);
                return false;
            }
        }

        private static bool InBattleSlot(string sole)
        {
            if (!RateService.InBattle)
            {
                return false;
            }
            // 전투 장면이 바뀌는 중이면 일부가 비어 있다. 예외로 막기 전체가 풀리지 않게 하나씩 본다(12 문서 A1).
            var battle = SceneType.battle;
            var map = battle == null ? null : battle.battleMap;
            var ctrl = map == null ? null : map.playerUnitCtrl;
            var props = ctrl == null ? null : ctrl.props;
            for (int i = 0; props != null && i < props.Count; i++)
            {
                PropItemBase item = props[i];
                if (item != null && item.data != null && item.data.propsData != null && item.data.propsData.soleID == sole)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool AddMpGuard(UnitDataBase __instance, int __0)
        {
            return !(__0 < 0 && RateService.MpCost.On && RateService.MpCost.Value == 0f && IsPlayerBattleData(__instance));
        }

        public static bool AddSpGuard(UnitDataBase __instance, int __0)
        {
            return !(__0 < 0 && RateService.SpCost.On && RateService.SpCost.Value == 0f && IsPlayerBattleData(__instance));
        }

        private static bool IsPlayerBattleData(UnitDataBase data)
        {
            try
            {
                if (data == null || !RateService.InBattle)
                {
                    return false;
                }
                // 전투 장면이 바뀌는 중이면 일부가 비어 있다. InBattleSlot처럼 하나씩 본다(05 문서 80·85절). 답은 전과 같고 예외 로그만 없다.
                var battle = SceneType.battle;
                var map = battle == null ? null : battle.battleMap;
                var ctrl = map == null ? null : map.playerUnitCtrl;
                var player = ctrl == null ? null : ctrl.data;
                return player != null && data.Pointer == player.Pointer;
            }
            catch (Exception e)
            {
                Warn(e);
                return false;
            }
        }
        public static void PatchDurable(HarmonyLib.Harmony h)
        {
            var prop = typeof(DataProps.PropsArtifact).GetProperty("durable");
            Hook(h, prop == null ? null : prop.GetSetMethod(), "DurableGuard", null);
        }

        // 개발 명령이 읽는다: 접두가 불린 수와 막은 수.
        public static int DurableCalls;
        public static int DurableBlockedCount;

        // 매개변수 이름이 다를 수 있어 자리(__0)로 받는다. 예외를 던지지 않는다.
        public static void DurableGuard(DataProps.PropsArtifact __instance, ref int __0)
        {
            if (!RateService.ArtifactDurable.On)
            {
                return;
            }
            try
            {
                DurableCalls++;
                int now = __instance.durable;
                if (__0 < now && RateGroups.DurableBlocked(now, __0, OwnedByPlayer(__instance), true))
                {
                    __0 = now;
                    DurableBlockedCount++;
                }
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        // 그 법보의 soleID가 플레이어 가방(allProps)에 있으면 참.
        private static bool OwnedByPlayer(DataProps.PropsArtifact art)
        {
            string sole = art == null || art.data == null ? null : art.data.soleID;
            WorldUnitBase player = g.world == null ? null : g.world.playerUnit;
            if (string.IsNullOrEmpty(sole) || player == null)
            {
                return false;
            }
            var all = player.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].soleID == sole)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
