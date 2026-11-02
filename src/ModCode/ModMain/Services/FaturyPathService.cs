using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MelonLoader;

namespace MOD_sWP6UL
{
    // Fatury의 모드 그림 폴더(Fatury.BwY.GetHome)에 뒤붙임을 걸어, 없는 경로면 debug\Mod_*\ModAssets\Fatury로 바꾼다(05 문서 98절).
    // Fatury의 형식 이름(BwY)은 난독화된 것이라 Fatury가 갱신되면 못 찾을 수 있다. 그때는 걸지 않고 로그만 남긴다.
    // 편집기의 Init이 Fatury의 Init보다 먼저 불려(모드 순서 맨 앞) Fatury가 폴더를 훑기 전에 걸린다.
    internal static class FaturyPathService
    {
        private static HarmonyLib.Harmony harmony;
        private static readonly Dictionary<string, string> cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static int Redirected;
        public static string LastNote = "fatury path: not hooked";

        public static void Ensure()
        {
            if (harmony != null)
            {
                return;
            }
            Assembly fatury = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Fatury");
            if (fatury == null)
            {
                LastNote = "fatury path: Fatury not loaded";
                return;
            }
            MethodInfo target = fatury.GetType("Fatury.BwY")?.GetMethod("GetHome", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (target == null || target.ReturnType != typeof(string))
            {
                LastNote = "fatury path: GetHome not found";
                MelonLogger.Msg("[TlToybox] fatury path: Fatury.BwY.GetHome not found (Fatury changed?)");
                return;
            }
            var instance = new HarmonyLib.Harmony("TlToybox.faturypath");
            try
            {
                instance.Patch(target, postfix: new HarmonyLib.HarmonyMethod(typeof(FaturyPathService).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                instance.UnpatchSelf();
                LastNote = "fatury path: hook failed " + e.GetType().Name;
                MelonLogger.Msg("[TlToybox] fatury path hook failed: " + e.GetType().Name + ": " + e.Message);
                return;
            }
            harmony = instance;
            LastNote = "fatury path: hooked";
        }

        public static void Postfix(ref string __result)
        {
            try
            {
                if (string.IsNullOrEmpty(__result))
                {
                    return;
                }
                if (!cache.TryGetValue(__result, out string alt))
                {
                    alt = FaturyPathRules.Alternative(__result, Directory.Exists, Directory.GetDirectories);
                    cache[__result] = alt;
                    if (alt != null)
                    {
                        MelonLogger.Msg("[TlToybox] fatury path: " + __result + " -> " + alt);
                    }
                }
                if (alt != null)
                {
                    Redirected++;
                    __result = alt;
                }
            }
            catch (Exception)
            {
                // Fatury의 동작을 막지 않는다.
            }
        }
    }
}
