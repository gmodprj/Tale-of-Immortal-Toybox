using System;
using System.Diagnostics;
using System.Reflection;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 화면 글 치환(스펙 2026-10-17-zh-text-korean-design.md 3.3절). TMP_Text.text와 UnityEngine.UI.Text.text 설정자에 Harmony 앞붙임을 걸어,
    // 들어오는 글에 한자가 있을 때만 TextTranslationService.Replacer로 바꾼다. 한자가 없는 글(거의 전부)은 글자 한 번 훑고 끝난다.
    // 번역표가 비어 있으면 걸지 않는다. 걸지 못하면 로그 한 줄을 남기고 게임은 그대로 돈다(PointerGuard와 같은 처리).
    internal static class TextPatch
    {
        private static HarmonyLib.Harmony harmony;
        public static string Failure { get; private set; }
        public static long Calls;      // 설정자 호출 수
        public static long Replaced;   // 글이 바뀐 수
        public static long Ticks;      // 한자가 든 글의 치환에 쓴 Stopwatch 틱 합
        // 치환 뒤에도 한자가 남은 글(번역표에 없는 원문). 번역표를 채우는 데 쓴다(개발 명령 rzhleft). 같은 글은 한 번, 최대 LeftoverMax개.
        public const int LeftoverMax = 300;
        public static readonly System.Collections.Generic.List<string> Leftover = new System.Collections.Generic.List<string>();
        private static readonly System.Collections.Generic.HashSet<string> leftoverSeen = new System.Collections.Generic.HashSet<string>();

        public static string Ensure()
        {
            if (harmony != null || Failure != null)
            {
                return Failure;
            }
            TextReplacer replacer = TextTranslationService.Replacer;
            if (replacer == null || (replacer.Count == 0 && replacer.WholeOnlyCount == 0))
            {
                return null;
            }
            var instance = new HarmonyLib.Harmony("TlToybox.text");
            try
            {
                MethodInfo prefix = typeof(TextPatch).GetMethod("TextPrefix", BindingFlags.Static | BindingFlags.Public);
                instance.Patch(typeof(TMPro.TMP_Text).GetProperty("text").GetSetMethod(), new HarmonyLib.HarmonyMethod(prefix));
                instance.Patch(typeof(UnityEngine.UI.Text).GetProperty("text").GetSetMethod(), new HarmonyLib.HarmonyMethod(prefix));
            }
            catch (Exception e)
            {
                instance.UnpatchSelf();
                Failure = "화면 글 치환을 게임에 연결하지 못했습니다(" + e.GetType().Name + ": " + e.Message + ")";
                MelonLogger.Msg("[TlToybox] " + Failure);
                return Failure;
            }
            harmony = instance;
            return null;
        }

        public static void TextPrefix(ref string value)
        {
            Calls++;
            if (value == null || value.Length == 0 || !TextReplacer.HasChinese(value))
            {
                return;
            }
            TextReplacer replacer = TextTranslationService.Replacer;
            if (replacer == null)
            {
                return;
            }
            long start = Stopwatch.GetTimestamp();
            bool changed;
            string result = replacer.Replace(value, out changed);
            Ticks += Stopwatch.GetTimestamp() - start;
            if (changed)
            {
                Replaced++;
                value = result;
            }
            if (TextReplacer.HasChinese(result) && Leftover.Count < LeftoverMax && leftoverSeen.Add(result))
            {
                Leftover.Add(result);
            }
        }

        public static string Stats()
        {
            double ms = Ticks * 1000.0 / Stopwatch.Frequency;
            return "patched=" + (harmony != null) + (Failure != null ? " failure=" + Failure : "") + " calls=" + Calls + " replaced=" + Replaced + " ms=" + ms.ToString("0.00");
        }
    }
}
