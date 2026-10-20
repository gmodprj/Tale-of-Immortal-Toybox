using System;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 성공·드롭 탭의 기능(docs/superpowers/specs/2026-10-20-trainer-batch2-design.md 7.2절, 05 문서 67절).
    // 플레이어의 시도에만 건다. 설정은 저장하지 않으며 게임을 켤 때마다 꺼진 상태로 시작한다. 패치는 줄을 처음 켤 때 한 번 걸고 떼지 않는다.
    //   훔치기: UnitActionRoleStealItem.CompleteRate(unit, toUnit) → 1, DiscoverRate(unit) → 0 (0~1 확률).
    //   관계 발전: NPC의 응답 UnitActionFeedback104.OnCreate 안에서, 요청자(requestUnit)가 플레이어일 때만
    //     친밀도 조건(closeRequire·schoolCloseRequire)과 필요 점수(requireScore<종류>·schoolRequireScore<직위>)를 -100000, 필요 명성(requireReputationGrade1~10)을 0으로.
    //   드롭 배수: ConfBattleUnitAttr.GetDropID가 돌려주는 보상 번호 문자열("a|b|c")을 n번 이어 붙인다(전투에서 적이 죽을 때만 쓰인다).
    // 칸(필드)을 감싼 속성과 Vector 인자 메서드에는 걸지 않는다(05 문서 65·67절).
    internal static class SuccessService
    {
        public const int NoRequirement = -100000;
        public static readonly int[] DropSteps = { 1, 2, 3, 5 };

        public static bool Steal { get; private set; }
        public static bool Relation { get; private set; }
        public static int DropTimes { get; private set; } = 1;

        private static HarmonyLib.Harmony harmony;
        private static bool stealPatched, relationPatched, dropPatched, warned;
        private static bool inPlayerRequest;   // 지금 도는 관계 응답이 플레이어의 요청인지

#if DEVTOOLS
        public static int DropCalls, DropIds, DropIdsRaw;   // 개발 전용: GetDropID 호출 수, 돌려준 보상 번호 수(바꾼 뒤), 원래 번호 수
#endif

        public static string SetSteal(bool on)
        {
            string error = on ? Ensure(ref stealPatched, PatchSteal) : null;
            if (error != null)
            {
                return error;
            }
            ChangeLog.Add("성공·드롭", "훔치기", Steal ? "켜짐" : "꺼짐", on ? "켜짐" : "꺼짐", true);
            Steal = on;
            return "훔치기 성공: " + (on ? "켜짐" : "꺼짐");
        }

        public static string SetRelation(bool on)
        {
            string error = on ? Ensure(ref relationPatched, PatchRelation) : null;
            if (error != null)
            {
                return error;
            }
            ChangeLog.Add("성공·드롭", "관계 발전", Relation ? "켜짐" : "꺼짐", on ? "켜짐" : "꺼짐", true);
            Relation = on;
            return "관계 발전 성공: " + (on ? "켜짐" : "꺼짐");
        }

        // 다음 단계로. 마지막 다음은 x1.
        public static string NextDrop()
        {
            int next = DropSteps[(Array.IndexOf(DropSteps, DropTimes) + 1) % DropSteps.Length];
            string error = next > 1 ? Ensure(ref dropPatched, PatchDrop) : null;
            if (error != null)
            {
                return error;
            }
            ChangeLog.Add("성공·드롭", "드롭 배수", "x" + DropTimes, "x" + next, true);
            DropTimes = next;
            return "드롭 배수: x" + next;
        }

        public static string Reset()
        {
            if (Steal)
            {
                SetSteal(false);
            }
            if (Relation)
            {
                SetRelation(false);
            }
            if (DropTimes != 1)
            {
                ChangeLog.Add("성공·드롭", "드롭 배수", "x" + DropTimes, "x1", true);
                DropTimes = 1;
            }
            return "성공·드롭: 모두 껐습니다.";
        }

        private static string Ensure(ref bool patched, Action<HarmonyLib.Harmony> patch)
        {
            if (patched)
            {
                return null;
            }
            try
            {
                if (harmony == null)
                {
                    harmony = new HarmonyLib.Harmony("TlToybox.success");
                }
                patch(harmony);
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 성공·드롭 패치 실패: " + e);
                return "성공·드롭: 게임에 연결하지 못해 켜지 않았습니다(" + e.GetType().Name + ").";
            }
            patched = true;
            return null;
        }

        private static void Hook(HarmonyLib.Harmony h, Type type, string name, Type[] args, string prefix, string postfix)
        {
            System.Reflection.MethodInfo target = HarmonyLib.AccessTools.Method(type, name, args);
            // 부모의 같은 이름을 찾아 걸지 않도록 그 형식에 선언된 것인지 본다(05 문서 67절의 함정).
            if (target == null || target.DeclaringType != type)
            {
                throw new MissingMethodException(type.Name, name);
            }
            var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public;
            h.Patch(target, prefix == null ? null : new HarmonyLib.HarmonyMethod(typeof(SuccessService).GetMethod(prefix, flags)),
                postfix == null ? null : new HarmonyLib.HarmonyMethod(typeof(SuccessService).GetMethod(postfix, flags)));
        }

        private static void PatchSteal(HarmonyLib.Harmony h)
        {
            Type unit = typeof(WorldUnitBase);
            Hook(h, typeof(UnitActionRoleStealItem), "CompleteRate", new[] { unit, unit }, null, "StealRate");
            Hook(h, typeof(UnitActionRoleStealItem), "DiscoverRate", new[] { unit }, null, "StealDiscover");
        }

        private static void PatchRelation(HarmonyLib.Harmony h)
        {
            Type conf = typeof(ConfNpcFeedback104Base);
            Hook(h, typeof(UnitActionFeedback104), "OnCreate", Type.EmptyTypes, "RelationBegin", "RelationEnd");
            Hook(h, conf, "get_closeRequire", Type.EmptyTypes, null, "RequireScore");
            Hook(h, conf, "get_schoolCloseRequire", Type.EmptyTypes, null, "RequireScore");
            foreach (string kind in new[] { "Parents", "Children", "Brother", "Master", "Student", "Marry", "Lover" })
            {
                Hook(h, conf, "get_requireScore" + kind, Type.EmptyTypes, null, "RequireScore");
            }
            Hook(h, conf, "get_schoolRequireScoreMaster", Type.EmptyTypes, null, "RequireScore");
            Hook(h, conf, "get_schoolRequireScoreStudent", Type.EmptyTypes, null, "RequireScore");
            for (int grade = 1; grade <= 10; grade++)
            {
                Hook(h, conf, "get_requireReputationGrade" + grade, Type.EmptyTypes, null, "RequireReputation");
            }
        }

        private static void PatchDrop(HarmonyLib.Harmony h)
        {
            Hook(h, typeof(ConfBattleUnitAttr), "GetDropID", new[] { typeof(int), typeof(bool) }, null, "DropId");
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
                MelonLogger.Msg("[TlToybox] 성공·드롭 실패: " + e);
            }
        }

        public static void StealRate(WorldUnitBase __0, ref float __result)
        {
            try
            {
                if (Steal && IsPlayer(__0))
                {
                    __result = 1f;
                }
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        public static void StealDiscover(WorldUnitBase __0, ref float __result)
        {
            try
            {
                if (Steal && IsPlayer(__0))
                {
                    __result = 0f;
                }
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        // 앞 응답이 예외로 끝나 표시가 남아도 새 응답마다 다시 정한다.
        public static void RelationBegin(UnitActionFeedback104 __instance)
        {
            inPlayerRequest = false;
            try
            {
                inPlayerRequest = Relation && IsPlayer(__instance.requestUnit);
            }
            catch (Exception e)
            {
                Warn(e);
            }
        }

        public static void RelationEnd()
        {
            inPlayerRequest = false;
        }

        public static void RequireScore(ref int __result)
        {
            if (inPlayerRequest)
            {
                __result = NoRequirement;
            }
        }

        public static void RequireReputation(ref int __result)
        {
            if (inPlayerRequest)
            {
                __result = 0;
            }
        }

        public static void DropId(ref string __result)
        {
#if DEVTOOLS
            DropIdsRaw += string.IsNullOrEmpty(__result) ? 0 : __result.Split('|').Length;
#endif
            if (DropTimes > 1 && !string.IsNullOrEmpty(__result))
            {
                string one = __result;
                var parts = new string[DropTimes];
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = one;
                }
                __result = string.Join("|", parts);
            }
#if DEVTOOLS
            DropCalls++;
            DropIds += string.IsNullOrEmpty(__result) ? 0 : __result.Split('|').Length;
#endif
        }
    }
}
