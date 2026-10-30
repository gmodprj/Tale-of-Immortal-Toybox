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
            new Group { Name = "시간·속도", Keys = new[] { "mapSpeed", "speed", "battleMove", "move", "actionDay" } },
            // 전투 칩은 8줄을 넘기지 않는다(줄 높이 64). 전투 이동 속도는 트레이너 4차에서 시간·속도로 옮겼다(05 문서 87절).
            new Group { Name = "전투", Keys = new[] { "cooldown", "itemCooldown", "artifactCooldown", "mpCost", "spCost", "multiplier", "taken", "god" } },
            new Group { Name = "관계·소모", Keys = new[] { "intimGain", "energyCost", "itemUse", "artifactDurable" } },
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

        // ---- 트레이너 4차(05 문서 87절, 스펙 2026-10-28-trainer4-battle-design.md) ----

        // 기술마다의 쿨타임(선법·영역의 cd, 밀리초)의 하한. 없음(0)도 0이 아니라 이 값이다(선법은 에너지를 cdTime으로 나눠 채우는 꼴).
        public const int SkillCooldownFloor = 100;

        // 기술마다의 쿨타임 값. 0 이하(게임이 멈춘 값)와 x1은 그대로. 원래 값이 하한보다 짧으면 늘리지 않는다.
        public static int SkillCooldown(int original, float rate)
        {
            if (original <= 0 || rate >= 1f)
            {
                return original;
            }
            int floor = Math.Min(SkillCooldownFloor, original);
            if (rate <= 0f)
            {
                return floor;
            }
            int scaled = (int)Math.Round(original * (double)rate, MidpointRounding.AwayFromZero);
            return Math.Max(floor, scaled);
        }

        // 전투 단위 비율(skill·step·item CDRate)을 이 프레임에 쓸지: 줄이 켜져 있거나, 걸어 둔 것을 되돌릴 때 한 번.
        // 꺼진 채 걸지 않은 줄의 비율은 건드리지 않는다(다른 줄만 켰을 때 게임 값을 덮지 않게, 최종 리뷰).
        public static bool WritesRate(bool on, bool applied)
        {
            return on || applied;
        }

        // 일격의 피해: 대상의 남은 체력 + 최대 체력(넘치면 int 최댓값). 0은 피해가 아니므로 최소 1.
        public static int OneHitDamage(int hp, int maxHp)
        {
            long sum = (long)Math.Max(0, hp) + Math.Max(0, maxHp);
            return sum > int.MaxValue ? int.MaxValue : (int)Math.Max(1L, sum);
        }

        // 게임 UnitType의 값(열거형은 게임 어셈블리에 있어 int로 받는다): Player=1, PlayerNPC=2(동료).
        public const int UnitPlayer = 1;
        public const int UnitPlayerNpc = 2;

        // 일격을 걸 타격인지: 플레이어의 타격이고, 맞은 쪽이 플레이어·동료가 아닐 때만(13 문서 2.6절).
        public static bool OneHitApplies(int attackerType, int targetType)
        {
            return attackerType == UnitPlayer && targetType != UnitPlayer && targetType != UnitPlayerNpc;
        }

        // 법보 내구 소모 없음: 켜져 있고 플레이어 법보이며 줄어드는 쓰기일 때만 막는다(수리로 늘리는 것은 그대로).
        public static bool DurableBlocked(int now, int next, bool owned, bool on)
        {
            return on && owned && next < now;
        }
    }
}
