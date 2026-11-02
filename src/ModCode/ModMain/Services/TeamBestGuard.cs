using System;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 동료(파티) 모드(2814673099, TeamBest.dll)가 캐릭터 정보창에 "结伴关系" 탭을 끼우다 중간에 터지는 것을 막는다(05 문서 99절).
    // TeamBest.SMoonUITools.CreatePlayerInfoNewPatch는 탭 단추를 복제한 뒤 창의 Root/G:goRelation/Group:Relation2/ScrollRect를 찾는데,
    // 이 게임판에는 그 경로가 없어 null 예외가 나고 UIPlayerInfo.Init이 예외로 끝난다(반쯤 만든 탭이 남는다). 그 경로가 없으면 이 함수를 건너뛴다.
    internal static class TeamBestGuard
    {
        private const string NeededPath = "Root/G:goRelation/Group:Relation2/ScrollRect";
        private static HarmonyLib.Harmony harmony;

        public static int Skipped;
        public static string LastNote = "teambest guard: not hooked";

        public static void Ensure()
        {
            if (harmony != null)
            {
                return;
            }
            Assembly team = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "TeamBest");
            if (team == null)
            {
                LastNote = "teambest guard: TeamBest not loaded";
                return;
            }
            MethodInfo target = team.GetType("TeamBest.SMoonUITools")?.GetMethod("CreatePlayerInfoNewPatch", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (target == null)
            {
                LastNote = "teambest guard: CreatePlayerInfoNewPatch not found";
                return;
            }
            var instance = new HarmonyLib.Harmony("TlToybox.teambest");
            try
            {
                instance.Patch(target, prefix: new HarmonyLib.HarmonyMethod(typeof(TeamBestGuard).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                instance.UnpatchSelf();
                LastNote = "teambest guard: hook failed " + e.GetType().Name;
                MelonLogger.Msg("[TlToybox] teambest guard hook failed: " + e.GetType().Name + ": " + e.Message);
                return;
            }
            harmony = instance;
            LastNote = "teambest guard: hooked";
        }

        // 원래 함수의 첫 인자 이름이 __instance라 Harmony의 주입 이름과 겹친다. 인자는 __args로 받는다.
        public static bool Prefix(object[] __args)
        {
            try
            {
                var ui = __args != null && __args.Length > 0 ? __args[0] as UIPlayerInfo : null;
                if (ui == null || ui.transform.Find(NeededPath) != null)
                {
                    return true;
                }
                Skipped++;
                if (Skipped == 1)
                {
                    MelonLogger.Msg("[TlToybox] teambest guard: " + NeededPath + " missing, party tab skipped");
                }
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
