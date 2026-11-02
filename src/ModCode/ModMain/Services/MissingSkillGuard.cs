using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 기술 본표에 없는 기술의 수준 계산을 0으로 넘긴다(05 문서 97.8절).
    // 이 계정에 없는 DLC의 기술(예: 절기 24117·24118, 신통 34114, 신법 510162)은 옵션 행만 남고 본표 행이 없다.
    // 에뮬레이터 시절 그 기술을 배운 인물이 있으면 FormulaTool.UnitPower.GetMartialLevel이 null 예외를 내고,
    // 세계 진입 중의 도호 수여(WorldSpecialAppellationGrant.OnGrant)가 끊겨 불러오기 화면에서 멈춘다.
    // 앞붙임이 본표를 보고 없는 번호면 원래 코드를 건너뛰고 0을 돌려준다. 세이브의 기술은 건드리지 않는다.
    internal static class MissingSkillGuard
    {
        private static HarmonyLib.Harmony harmony;
        private static readonly KnownSkillIds known = new KnownSkillIds();
        private static readonly HashSet<string> seen = new HashSet<string>();

        public static int Guarded;
        public static string LastNote = "skill guard: not hooked";

        public static void Ensure()
        {
            if (harmony != null)
            {
                return;
            }
            var instance = new HarmonyLib.Harmony("TlToybox.skillguard");
            try
            {
                MethodInfo target = typeof(FormulaTool.UnitPower).GetMethod("GetMartialLevel", BindingFlags.Static | BindingFlags.Public);
                MethodInfo prefix = typeof(MissingSkillGuard).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public);
                instance.Patch(target, prefix: new HarmonyLib.HarmonyMethod(prefix));
                // SkillPower도 기술 본표 행을 읽어 같은 null 예외를 낸다(97.8절). 예외로 끝나면 그 인물의 기술 전투력을 0으로.
                MethodInfo skillPower = typeof(FormulaTool.UnitPower).GetMethod("SkillPower", BindingFlags.Static | BindingFlags.Public);
                MethodInfo finalizer = typeof(MissingSkillGuard).GetMethod(nameof(SkillPowerFinalizer), BindingFlags.Static | BindingFlags.Public);
                instance.Patch(skillPower, finalizer: new HarmonyLib.HarmonyMethod(finalizer));
            }
            catch (Exception e)
            {
                instance.UnpatchSelf();
                LastNote = "skill guard: hook failed " + e.GetType().Name;
                MelonLogger.Msg("[TlToybox] skill guard hook failed: " + e.GetType().Name + ": " + e.Message);
                return;
            }
            harmony = instance;
            LastNote = "skill guard: hooked";
        }

        public static bool Prefix(MartialType martialType, int baseID, ref int __result)
        {
            try
            {
                int table = martialType == MartialType.Step ? KnownSkillIds.Step : martialType == MartialType.Ability ? KnownSkillIds.Ability : KnownSkillIds.Attack;
                Refresh(table);
                if (!known.Missing(table, baseID))
                {
                    return true;
                }
                Guarded++;
                string key = martialType + " " + baseID;
                if (seen.Count < 20 && seen.Add(key))
                {
                    MelonLogger.Msg("[TlToybox] skill guard: no base row for " + key + " (level 0)");
                }
                __result = 0;
                return false;
            }
            catch (Exception)
            {
                return true;   // 판정이 실패하면 게임의 원래 계산을 막지 않는다.
            }
        }

        public static int PowerZeroed;

        public static Exception SkillPowerFinalizer(Exception __exception, ref int __result)
        {
            if (__exception == null)
            {
                return null;
            }
            PowerZeroed++;
            if (PowerZeroed == 1)
            {
                MelonLogger.Msg("[TlToybox] skill guard: SkillPower failed (" + __exception.GetType().Name + "), using 0");
            }
            __result = 0;
            return null;
        }

        private static void Refresh(int table)
        {
            if (table == KnownSkillIds.Step)
            {
                var rows = g.conf.battleStepBase._allConfList;
                if (known.Stale(table, rows.Count))
                {
                    var list = new List<int>(rows.Count);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        list.Add(rows[i].id);
                    }
                    known.Fill(table, list, rows.Count);
                }
            }
            else if (table == KnownSkillIds.Ability)
            {
                var rows = g.conf.battleAbilityBase._allConfList;
                if (known.Stale(table, rows.Count))
                {
                    var list = new List<int>(rows.Count);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        list.Add(rows[i].id);
                    }
                    known.Fill(table, list, rows.Count);
                }
            }
            else
            {
                var rows = g.conf.battleSkillAttack._allConfList;
                if (known.Stale(table, rows.Count))
                {
                    var list = new List<int>(rows.Count);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        list.Add(rows[i].id);
                    }
                    known.Fill(table, list, rows.Count);
                }
            }
        }
    }
}
