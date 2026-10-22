using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // LocalText를 Init에서 한 번만 훑는다(성능 1 스펙 4.1, 계획 Task 5).
    //   Scan: 모드 행의 kr 덮기(TranslationService)와 ch→kr 전체 일치 사전(TextTranslationService)을 한 번의 훑기로 한다.
    //   ChOf: 기술 카탈로그의 중국어 원문 찾기. 게임의 키 사전 allText를 ContainsKey·인덱서로 바로 찾는다
    //         (TryGetValue는 MissingMethodException. 회차 perf1-1023b에서 761개 키가 지금까지의 훑기와 모두 같았다, 4 ms).
    internal static class LocalTextIndex
    {
        // 마지막 Scan의 ch→kr. Scan이 끝나지 못했으면 null(TextTranslationService가 자기 훑기로 되돌아간다).
        public static Dictionary<string, string> ChToKr { get; private set; }

#if DEVTOOLS
        // 개발 명령 rperf ltfail: 다음 Scan이 첫 행에서 예외를 던진다(되돌아가기 길 시험).
        internal static bool DevFailNext;
#endif

        public static void Scan(Dictionary<string, string> modTranslations, out int applied, out int fallback, out HashSet<string> seen)
        {
            ChToKr = null;
            applied = 0;
            fallback = 0;
            seen = new HashSet<string>();
            var chToKr = new Dictionary<string, string>(StringComparer.Ordinal);
            var all = g.conf.localText.allConfList;
            for (int i = 0; i < all.Count; i++)
            {
#if DEVTOOLS
                if (DevFailNext)
                {
                    DevFailNext = false;
                    throw new InvalidOperationException("rperf ltfail");
                }
#endif
                ConfLocalTextItem item = all[i];
                if (item == null)
                {
                    continue;
                }
                string ch = item.ch;
                string kr = item.kr;
                if (item.isModExtend)
                {
                    string key = item.key;
                    if (key != null)
                    {
                        string text;
                        bool found = modTranslations.TryGetValue(key, out text);
                        string next = LocalTextRules.ModKr(found ? null : kr, found ? null : ch, found ? text : null);
                        if (next != null)
                        {
                            item.kr = next;
                            kr = next;
                            if (found)
                            {
                                seen.Add(key);
                                applied++;
                            }
                            else
                            {
                                fallback++;
                            }
                        }
                    }
                }
                if (LocalTextRules.KeepsWhole(ch, kr) && !chToKr.ContainsKey(ch))
                {
                    chToKr[ch] = kr;
                }
            }
            ChToKr = chToKr;
        }

        // LocalText 행의 ch. 없으면 "".
        public static string ChOf(string key)
        {
            if (key == null)
            {
                return "";
            }
            var dict = g.conf.localText.allText;
            if (!dict.ContainsKey(key))
            {
                return "";
            }
            ConfLocalTextItem item = dict[key];
            return item == null ? "" : item.ch ?? "";
        }
    }
}
