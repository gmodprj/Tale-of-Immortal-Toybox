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
            // 이미 걸었으면 그대로. 앞서 실패했으면 다음 Init에서 다시 걸어 본다(리뷰 10의 N2. 실패 로그는 이유가 바뀔 때만).
            if (harmony != null)
            {
                return null;
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
                string failure = "화면 글 치환을 게임에 연결하지 못했습니다(" + e.GetType().Name + ": " + e.Message + ")";
                if (failure != Failure)
                {
                    MelonLogger.Msg("[TlToybox] " + failure);
                }
                Failure = failure;
                return Failure;
            }
            harmony = instance;
            Failure = null;
            return null;
        }

        private static bool prefixWarned;

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
            // 게임의 Text.text 설정자 안에서 돈다. 치환이 던지면 원문을 그대로 두고 첫 번만 로그(13 문서 B22).
            try
            {
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
            catch (Exception e)
            {
                if (!prefixWarned)
                {
                    prefixWarned = true;
                    MelonLogger.Msg("[TlToybox] 화면 글 치환 오류(원문 그대로 둠): " + e.GetType().Name + ": " + e.Message);
                }
            }
        }

        // 세이브를 떠날 때 통계를 비운다(12 문서 N3). 남은 원문 목록(Leftover)은 번역표를 채우는 자료라 그대로 둔다.
        public static void ResetStats()
        {
            Calls = 0;
            Replaced = 0;
            Ticks = 0;
        }

        public static string Stats()
        {
            double ms = Ticks * 1000.0 / Stopwatch.Frequency;
            return "patched=" + (harmony != null) + (Failure != null ? " failure=" + Failure : "") + " calls=" + Calls + " replaced=" + Replaced + " ms=" + ms.ToString("0.00");
        }
    }
}
