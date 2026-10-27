using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 배율 탭의 분류 칩과 정력 돌려주기 계산(05 문서 86절, 스펙 2026-10-27-rate-tab-expand-design.md). 게임 형식을 쓰지 않아 단위 시험에 링크한다.
    internal static class RateGroups
    {
        public sealed class Group
        {
            public string Name;
            public string[] Keys;   // 줄 키. 배율은 RateService.Rate.Key, 주는 피해는 multiplier, 무적은 god
        }

        public const string OnName = "켜 둔 것";

        public static readonly Group[] Groups =
        {
            new Group { Name = "성장", Keys = new[] { "exp", "martial", "studyGain" } },
            new Group { Name = "시간·속도", Keys = new[] { "mapSpeed", "speed", "move", "actionDay" } },
            new Group { Name = "전투", Keys = new[] { "battleMove", "cooldown", "artifactCooldown", "mpCost", "spCost", "multiplier", "taken", "god" } },
            new Group { Name = "관계·소모", Keys = new[] { "intimGain", "energyCost", "itemUse" } },
        };

        // 칩 수: 분류 넷 + 켜 둔 것
        public static int Count
        {
            get { return Groups.Length + 1; }
        }

        public static string NameOf(int index)
        {
            return index < Groups.Length ? Groups[index].Name : OnName;
        }

        // 그 칩에 보일 줄 키. 켜 둔 것은 분류 순서대로 켜진 줄만.
        public static List<string> KeysOf(int index, Func<string, bool> isOn)
        {
            var result = new List<string>();
            if (index >= 0 && index < Groups.Length)
            {
                result.AddRange(Groups[index].Keys);
                return result;
            }
            foreach (Group group in Groups)
            {
                foreach (string key in group.Keys)
                {
                    if (isOn(key))
                    {
                        result.Add(key);
                    }
                }
            }
            return result;
        }

        // 그 칩에서 켜진 줄 수(칩 글 뒤에 붙인다)
        public static int OnCount(int index, Func<string, bool> isOn)
        {
            int n = 0;
            foreach (string key in KeysOf(index, isOn))
            {
                if (isOn(key))
                {
                    n++;
                }
            }
            return n;
        }

        // 그 줄 키가 든 분류 칩 번호. 없으면 -1.
        public static int GroupOf(string key)
        {
            for (int i = 0; i < Groups.Length; i++)
            {
                if (Array.IndexOf(Groups[i].Keys, key) >= 0)
                {
                    return i;
                }
            }
            return -1;
        }

        // 정력 소모 배율: 지난 값 before에서 now로 줄었으면, 남길 소모(줄어든 양 × rate, 올림)를 뺀 나머지를 돌려준다. 늘었거나 같으면 0.
        // 올림이라 1짜리 소모도 x0.5·x0.25에서 1로 남는다(반올림이면 공짜가 됐다, 최종 리뷰). 없음(0)이면 전부 돌려준다.
        public static int EnergyRefund(int before, int now, float rate)
        {
            if (now >= before || rate >= 1f)
            {
                return 0;
            }
            int drop = before - now;
            int keep = (int)Math.Ceiling(drop * (double)Math.Max(0f, rate) - 1e-9);
            return Math.Max(0, drop - keep);
        }
    }
}
