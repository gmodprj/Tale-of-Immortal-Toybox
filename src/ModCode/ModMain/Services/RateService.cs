using System;
using System.Globalization;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 획득·소모의 배율. 플레이어의 호출에만 건다. 설정은 저장하지 않으며 게임을 켤 때마다 1배로 시작한다.
    // 거는 곳은 docs/superpowers/specs/2026-10-13-rate-tab-design.md 3절에서 실측했다.
    //   수련치: DataUnit.PropertyData.AddExp. 수련과 전투의 처치가 모두 지난다.
    //   공법 숙련: DataUnit.UnitInfoData.AddMartialExpInBattle. 이 메서드가 안에서 AddMartialExp를 부르므로 한 곳에만 건다.
    //   이동: UnitActionMovePlayer.OnCreate가 쓰기 전의 costDay(한 칸의 날짜 비용).
    //   전투 속도: 패치 없이 전투 장면에서 매 프레임 Time.timeScale을 맞춘다.
    //   수련·학습의 날짜: UnitActionBase.Create 안의 unit.AddResidueDay(day)의 인자. 행동의 day는 바꾸지 않는다(숙련·진도는 생성 때 이미 정해져 있다).
    //     달 끝의 날짜 판정 IsDayCost(day)에도 같은 배율(05 문서 63절).
    internal static class RateService
    {
        public sealed class Rate
        {
            public string Key;
            public string Label;    // 탭의 줄 이름
            public string Short;    // 화면 위쪽 표시의 이름
            public string Note;
            public float[] Steps;   // 첫 칸은 항상 1
            public int Index;
            public bool Patched;
            public bool Warned;
            public MethodInfo Target;   // 걸 메서드. 전투 속도는 없다
            public string Hook;

            public float Value
            {
                get { return Steps[Index]; }
            }

            public bool On
            {
                get { return Index != 0; }
            }

            public string ZeroText;     // 값이 0인 단계를 부르는 말(없으면 x0)

            public string Text
            {
                get { return Value == 0f && ZeroText != null ? ZeroText : "x" + Value.ToString("0.##", CultureInfo.InvariantCulture); }
            }

#if DEVTOOLS
            public string Last = "-";   // 개발 전용: 마지막으로 바꾼 값(전 -> 후)
#endif
        }

        public static readonly Rate Exp = new Rate
        {
            Key = "exp", Label = "수련치 획득", Short = "수련", Steps = new[] { 1f, 2f, 5f, 10f, 100f },
            Note = "수련과 전투로 얻는 수련치를 곱합니다. 수련치가 다음 단계의 필요량까지 찬 인물은 오르지 않습니다.",
            Hook = "ExpPrefix",
        };

        public static readonly Rate Martial = new Rate
        {
            Key = "martial", Label = "전투 중 공법 숙련", Short = "숙련", Steps = new[] { 1f, 2f, 5f, 10f, 100f },
            Note = "전투에서 기술을 써서 얻는 숙련 경험치를 곱합니다.",
            Hook = "MartialPrefix",
        };

        public static readonly Rate Move = new Rate
        {
            Key = "move", Label = "이동의 날짜 소모", Short = "이동", Steps = new[] { 1f, 0.5f, 0.25f, 0.1f, 0f },
            Note = "대지도에서 한 칸을 갈 때 쓰는 날짜를 곱합니다. x0이면 걸어도 날짜가 흐르지 않습니다.",
            Hook = "MovePrefix",
        };

        // 기술 수련(UnitActionMartialTrain)과 비급 학습(UnitActionPropMartialStudy)이 쓰는 날짜. 이동과 같은 단계(05 문서 63절 실측):
        //   수련의 숙련(addExp)과 학습의 진도(exp → World.allMartialStudy[비급].exp, 필요량 ConfBattleSkillCost.GetExp에 닿으면 배움)는
        //   행동을 만들 때 이미 정해져 날짜와 무관하다. 그래서 흐르는 날짜만 줄이면 같은 결과를 더 짧은 시간에 얻는다.
        //   참오(UnitActionMartialRealize)·망각(UnitActionMartialDel)은 게임이 날짜를 쓰지 않는다(day 0. 참오는 아이템·영석).
        public static readonly Rate ActionDay = new Rate
        {
            Key = "actionDay", Label = "수련·학습의 날짜 소모", Short = "수련·학습 시간", Steps = new[] { 1f, 0.5f, 0.25f, 0.1f, 0f },
            Note = "기술 수련과 비급 학습에 쓰는 날짜를 곱합니다(반올림. 얻는 숙련·학습 진도는 그대로). 참오·망각은 게임이 날짜를 쓰지 않습니다.",
            Hook = "ActionDayPrefix",
        };

        // 배율을 거는 행동의 Il2Cpp 형식 이름.
        private static readonly string[] ActionDayTypes = { "UnitActionMartialTrain", "UnitActionPropMartialStudy" };

        public static readonly Rate Speed = new Rate
        {
            Key = "speed", Label = "전투 속도", Short = "속도", Steps = new[] { 1f, 1.5f, 2f, 3f },
            Note = "전투 장면의 진행 속도를 곱합니다. 전투 밖에서는 걸리지 않습니다.",
        };

        // 전투 중인 플레이어의 쿨타임 속도(skillCDRate, stepCDRate. 기본 100)를 올려 쿨타임을 줄인다(실측: 05 문서 33절).
        // skillCDRate는 무예·절기·신통에, stepCDRate는 신법에 걸린다.
        public static readonly Rate Cooldown = new Rate
        {
            Key = "cooldown", Label = "기술 쿨타임", Short = "쿨타임", Steps = new[] { 1f, 0.5f, 0.25f, 0.1f, 0f }, ZeroText = "없음",
            Note = "전투에서 무예·절기·신법·신통과 명우 기술, 선법·영역 기술의 재사용 대기시간을 곱합니다. 없음이면 바로 다시 쓸 수 있습니다(영력·도력 소모와 발동 조건, 선법의 발동 시간은 그대로).",
        };

        // 전투 아이템 칸(단약 등)과 장착 법보 발동의 쿨타임: 전투 단위 비율 itemCDRate(기본 100)를 올린다.
        // 신기 쪽 artifactCDRate와 달리 전투 중에 바꿔도 듣는다(05 문서 87절, 회차 t4-a-1028).
        public static readonly Rate ItemCooldown = new Rate
        {
            Key = "itemCooldown", Label = "전투 아이템 쿨타임", Short = "아이템 쿨타임", Steps = new[] { 1f, 0.5f, 0.25f, 0.1f, 0f }, ZeroText = "없음",
            Note = "전투 아이템 칸(단약·부적 등)과 장착 법보를 쓴 뒤의 재사용 대기시간을 곱합니다. 아이템 수는 아이템 소모 줄이 맡습니다.",
        };

        // 법보 내구 소모 없음: PropsArtifact.set_durable 접두에서 플레이어 법보의 줄어드는 쓰기를 막는다(패치는 RateGuards, 05 문서 87절).
        public static readonly Rate ArtifactDurable = new Rate
        {
            Key = "artifactDurable", Label = "법보 내구 소모", Short = "법보 내구", Steps = new[] { 1f, 0f }, ZeroText = "없음",
            Note = "없음이면 플레이어 법보의 내구가 줄지 않습니다(전투에서 법보를 처음 쓸 때 줄어드는 것 등). 수리로 늘리는 것은 그대로입니다.",
        };

        // 플레이어가 받는 피해. 주는 피해와 같은 이벤트에서 BattleService가 곱한다.
        public static readonly Rate Taken = new Rate
        {
            Key = "taken", Label = "받는 피해", Short = "받는 피해", Steps = new[] { 1f, 0.5f, 0.25f, 0.1f },
            Note = "플레이어가 받는 피해를 곱합니다. 무적이 켜져 있으면 무적이 우선입니다.",
        };

        // 전투 중 기술마다의 값에 곱한다(전투 단위의 비율 artifactCDRate·mpCostRate·spCostRate는 전투 중에 바꿔도 듣지 않았다. 05 문서 37절).
        // 신기 쿨타임: 호천안 기술의 cd(밀리초), 연요호의 cd(초). 쌍어패 소환의 대기는 게임 날짜라 다루지 않는다.
        public static readonly Rate ArtifactCooldown = new Rate
        {
            Key = "artifactCooldown", Label = "신기 쿨타임", Short = "신기 쿨타임", Steps = new[] { 1f, 0.5f, 0.25f, 0.1f, 0f }, ZeroText = "없음",
            Note = "전투에서 호천안 기술과 연요호의 재사용 대기시간을 곱합니다(쌍어패 소환의 대기는 게임 날짜라 그대로). 소모와 발동 조건은 그대로입니다.",
        };

        // 영력 소모: 무예·절기(skills), 신통(ultimate), 신법(step), 호천안 기술의 mpCost.
        public static readonly Rate MpCost = new Rate
        {
            Key = "mpCost", Label = "영력 소모", Short = "영력 소모", Steps = new[] { 1f, 0.5f, 0.25f, 0.1f, 0f }, ZeroText = "없음",
            Note = "전투에서 무예·절기·신법·신통과 호천안 기술이 쓰는 영력을 곱합니다. 없음이면 전투 중 영력이 줄지 않습니다.",
        };

        // 집중력 소모: 연요호의 spCost.
        public static readonly Rate SpCost = new Rate
        {
            Key = "spCost", Label = "집중력 소모", Short = "집중력 소모", Steps = new[] { 1f, 0.5f, 0.25f, 0.1f, 0f }, ZeroText = "없음",
            Note = "전투에서 연요호가 쓰는 집중력을 곱합니다. 없음이면 전투 중 집중력이 줄지 않습니다(전투 단약 포함).",
        };

        // 비급 학습의 진도(05 문서 64절): UnitActionPropMartialStudy.OnCreate가 World.allMartialStudy에 더하기 전의 행동 exp를 곱한다.
        // 필요량(ConfBattleSkillCost.GetExp, 경지 3 무예 740)에 닿으면 배운다. 플레이어만.
        public static readonly Rate StudyGain = new Rate
        {
            Key = "studyGain", Label = "비급 학습 진도", Short = "학습 진도", Steps = new[] { 1f, 2f, 5f, 10f, 100f },
            Note = "비급을 한 번 공부할 때 쌓이는 진도를 곱합니다(필요량에 닿으면 배웁니다). x100이면 대개 한 번에 배웁니다.",
            Hook = "StudyGainPrefix",
        };

        // 아이템 소모 없음(05 문서 65절): 쓰기 행동이 쓰는 아이템과 전투 아이템 칸의 아이템만 막는다. 패치는 RateGuards.
        public static readonly Rate ItemUse = new Rate
        {
            Key = "itemUse", Label = "아이템 소모", Short = "아이템 소모", Steps = new[] { 1f, 0f }, ZeroText = "없음",
            Note = "플레이어가 아이템을 써도(단약·전투 아이템·비급 공부·돌파 재료·상자 열기·부적) 수가 줄지 않습니다. 제작 재료·판매·선물·퀘스트 제출은 그대로 줄어듭니다.",
        };

        // ---- 배율 탭 확장(05 문서 86절, 스펙 2026-10-27-rate-tab-expand-design.md) ----

        // 지도 속도: 세계에 있고 전투가 아닐 때 매 프레임 Time.timeScale을 맞춘다(실측 x3에서 지도 이동 3배, 회차 r2-c-1027).
        public static readonly Rate MapSpeed = new Rate
        {
            Key = "mapSpeed", Label = "지도 속도", Short = "지도 속도", Steps = new[] { 1f, 1.5f, 2f, 3f },
            Note = "전투 밖(지도)의 진행 속도를 곱합니다. 이동과 연출이 빨라지고, 날짜 소모는 그대로입니다. 전투 안은 전투 속도 줄이 맡습니다.",
        };

        // 전투 이동 속도: 전투 플레이어 data.moveSpeed(DynInt)에 AddValue로 base×(배수−1)을 더한다(실측 411 → 822, 회차 r2-a-1027).
        public static readonly Rate BattleMove = new Rate
        {
            Key = "battleMove", Label = "전투 이동 속도", Short = "이동 속도", Steps = new[] { 1f, 1.5f, 2f, 3f },
            Note = "전투 안에서 플레이어가 걷는 빠르기(이동속도)를 곱합니다. 전투마다 다시 걸고, 전투 밖의 이동속도 값은 바꾸지 않습니다.",
        };

        // 호감 변화: RelationData.AddIntim 접두에서 플레이어가 주체·대상인 양수 value를 곱한다(게임 대화는 양쪽으로 부른다, 회차 r2-f-1027).
        public static readonly Rate IntimGain = new Rate
        {
            Key = "intimGain", Label = "호감 변화", Short = "호감", Steps = new[] { 1f, 2f, 5f, 10f },
            Note = "대화 등으로 플레이어와 NPC 사이의 호감이 오를 때 오르는 양을 곱합니다(양쪽 모두). 내려가는 것과 편집기 NPC 탭의 값은 그대로입니다.",
            Hook = "IntimPrefix",
        };

        // 정력 소모: 정력을 깎는 곳이 행동마다 달라(costEnergy 열넷) 매 프레임 줄어든 만큼을 돌려준다(실측 훔치기 −10, 대화 −5).
        public static readonly Rate EnergyCost = new Rate
        {
            Key = "energyCost", Label = "정력 소모", Short = "정력 소모", Steps = new[] { 1f, 0.5f, 0.25f, 0f }, ZeroText = "없음",
            Note = "정력이 줄어드는 양을 곱합니다(대화·훔치기 같은 행동, 사건 등 줄어드는 모든 경우. 없음이면 줄지 않음). 회복은 그대로입니다.",
        };

        // All은 위의 줄들보다 뒤에 둔다(정적 필드는 적힌 순서로 초기화된다).
        public static readonly Rate[] All = { Exp, Martial, Move, ActionDay, Speed, Cooldown, ItemCooldown, ArtifactCooldown, MpCost, SpCost, StudyGain, ItemUse, Taken,
            MapSpeed, BattleMove, IntimGain, EnergyCost, ArtifactDurable };

        private static HarmonyLib.Harmony harmony;
        private static bool speedApplied;

        // 다음 단계로 넘긴다. 마지막 다음은 1배다. 결과 문장을 돌려준다.
        public static string Next(Rate rate)
        {
            int next = (rate.Index + 1) % rate.Steps.Length;
            if (next != 0)
            {
                string error = EnsurePatched(rate);
                if (error != null)
                {
                    return error;
                }
            }
            if ((rate == MpCost || rate == SpCost) && rate.Steps[next] == 0f)
            {
                string error = EnsureDrain();
                if (error != null)
                {
                    return error;
                }
            }
            string before = rate.Text;
            rate.Index = next;
            ChangeLog.Add("배율", rate.Label, before, rate.Text, true);
            if (rate == Taken)
            {
                BattleService.Refresh();
            }
            return rate.Label + ": " + rate.Text;
        }

        // 배율과 전투 보조를 모두 끈다.
        public static string Reset()
        {
            foreach (Rate rate in All)
            {
                if (rate.On)
                {
                    string before = rate.Text;
                    rate.Index = 0;
                    ChangeLog.Add("배율", rate.Label, before, rate.Text, true);
                }
            }
            BattleService.Reset();
            BattleService.Refresh();
            RestoreSpeed();
            RestoreMapSpeed();
            return "배율: 모두 1배로 되돌리고 무적을 껐습니다.";
        }

        public static void OnDestroy()
        {
            RestoreSpeed();
            RestoreMapSpeed();
            moveUnit = IntPtr.Zero;
            energyLast = int.MinValue;
            cooldownUnit = IntPtr.Zero;
            skillUnit = IntPtr.Zero;
            skillValues.Clear();
        }

        // 그 배율을 처음 켤 때 한 번 건다. 건 뒤에는 떼지 않고 값만 본다.
        private static string EnsurePatched(Rate rate)
        {
            if (rate.Patched)
            {
                return null;
            }
            if (rate == ItemUse)
            {
                return PatchWith(rate, RateGuards.PatchItemUse);
            }
            if (rate == ArtifactDurable)
            {
                return PatchWith(rate, RateGuards.PatchDurable);
            }
            if (rate.Hook == null)
            {
                return null;
            }
            try
            {
                if (rate.Target == null)
                {
                    rate.Target = FindTarget(rate);
                }
                if (rate.Target == null)
                {
                    throw new MissingMethodException(rate.Key);
                }
                if (harmony == null)
                {
                    harmony = new HarmonyLib.Harmony("TlToybox.rate");
                }
                harmony.Patch(rate.Target, new HarmonyLib.HarmonyMethod(
                    typeof(RateService).GetMethod(rate.Hook, BindingFlags.Static | BindingFlags.Public)));
                if (rate == ActionDay)
                {
                    // Create의 앞뒤로 "지금 플레이어의 기술 행동 안"을 표시하고, 그 안의 AddResidueDay 인자만 곱한다.
                    harmony.Patch(rate.Target, postfix: new HarmonyLib.HarmonyMethod(
                        typeof(RateService).GetMethod("ActionDayPostfix", BindingFlags.Static | BindingFlags.Public)));
                    harmony.Patch(typeof(WorldUnitBase).GetMethod("AddResidueDay"), new HarmonyLib.HarmonyMethod(
                        typeof(RateService).GetMethod("ResidueDayPrefix", BindingFlags.Static | BindingFlags.Public)));
                    // 달 끝에 남은 날짜가 원래 값보다 적으면 행동이 거부된다. 판정도 곱한 값으로 한다.
                    harmony.Patch(typeof(UnitActionBase).GetMethod("IsDayCost"), new HarmonyLib.HarmonyMethod(
                        typeof(RateService).GetMethod("ActionDayCheckPrefix", BindingFlags.Static | BindingFlags.Public)));
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 배율 패치 실패(" + rate.Key + "): " + e);
                return "배율: 게임에 연결하지 못해 켜지 않았습니다(" + e.GetType().Name + ").";
            }
            rate.Patched = true;
            return null;
        }

        // 영력·집중력 "없음"이 처음 켜질 때 한 번: 전투 중 플레이어의 AddMP·AddSP 음수를 막는다(05 문서 65절).
        // 다른 단계는 기술값 곱하기(TickSkillValues)만 쓰므로 걸지 않는다.
        private static bool drainPatched;

        private static string EnsureDrain()
        {
            if (drainPatched)
            {
                return null;
            }
            try
            {
                if (harmony == null)
                {
                    harmony = new HarmonyLib.Harmony("TlToybox.rate");
                }
                RateGuards.PatchDrain(harmony);
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 배율 패치 실패(drain): " + e);
                return "배율: 게임에 연결하지 못해 켜지 않았습니다(" + e.GetType().Name + ").";
            }
            drainPatched = true;
            return null;
        }

        // 게임 메서드 여럿에 거는 줄(RateGuards). 실패하면 켜지 않는다.
        private static string PatchWith(Rate rate, Action<HarmonyLib.Harmony> patch)
        {
            try
            {
                if (harmony == null)
                {
                    harmony = new HarmonyLib.Harmony("TlToybox.rate");
                }
                patch(harmony);
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 배율 패치 실패(" + rate.Key + "): " + e);
                return "배율: 게임에 연결하지 못해 켜지 않았습니다(" + e.GetType().Name + ").";
            }
            rate.Patched = true;
            return null;
        }

        private static MethodInfo FindTarget(Rate rate)
        {
            if (rate == Exp)
            {
                return typeof(DataUnit.PropertyData).GetMethod("AddExp");
            }
            if (rate == Martial)
            {
                return typeof(DataUnit.UnitInfoData).GetMethod("AddMartialExpInBattle");
            }
            if (rate == Move)
            {
                return typeof(UnitActionMovePlayer).GetMethod("OnCreate");
            }
            if (rate == ActionDay)
            {
                return typeof(UnitActionBase).GetMethod("Create");
            }
            if (rate == StudyGain)
            {
                return typeof(UnitActionPropMartialStudy).GetMethod("OnCreate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (rate == IntimGain)
            {
                return typeof(DataUnit.RelationData).GetMethod("AddIntim");
            }
            return null;
        }

        private static WorldUnitBase Player
        {
            get { return g.world == null ? null : g.world.playerUnit; }
        }

        // 0 이하는 곱하지 않는다(줄어드는 호출을 키우지 않는다). 넘치면 최댓값으로 자른다.
        private static int Scale(Rate rate, int value)
        {
            if (value <= 0)
            {
                return value;
            }
            double scaled = Math.Round(value * (double)rate.Value);
            int result = scaled > int.MaxValue ? int.MaxValue : (int)scaled;
#if DEVTOOLS
            rate.Last = value + "->" + result;
#endif
            return result;
        }

        private static void Warn(Rate rate, Exception e)
        {
            if (!rate.Warned)
            {
                rate.Warned = true;
                MelonLogger.Msg("[TlToybox] 배율 처리 오류(" + rate.Key + "): " + e.Message);
            }
        }

        // 호감 변화: 플레이어가 주체이거나 대상인 호출의 양수만 곱한다. 예외를 던지지 않는다.
        public static void IntimPrefix(DataUnit.RelationData __instance, string __0, ref float __1)
        {
            if (!IntimGain.On || __1 <= 0f)
            {
                return;
            }
            try
            {
                WorldUnitBase player = Player;
                if (player == null)
                {
                    return;
                }
                string me = player.data.unitData.unitID;
                if (__instance.unitID != me && __0 != me)
                {
                    return;
                }
                float before = __1;
                __1 = before * IntimGain.Value;
#if DEVTOOLS
                IntimGain.Last = before + "->" + __1;
#endif
            }
            catch (Exception e)
            {
                Warn(IntimGain, e);
            }
        }

        public static void ExpPrefix(DataUnit.PropertyData __instance, ref int v)
        {
            if (!Exp.On)
            {
                return;
            }
            try
            {
                WorldUnitBase me = Player;
                if (me != null && __instance != null && __instance.Pointer == me.data.unitData.propertyData.Pointer)
                {
                    v = Scale(Exp, v);
                }
            }
            catch (Exception e)
            {
                Warn(Exp, e);
            }
        }

        public static void MartialPrefix(DataUnit.UnitInfoData __instance, ref int exp)
        {
            if (!Martial.On)
            {
                return;
            }
            try
            {
                WorldUnitBase me = Player;
                if (me != null && __instance != null && __instance.Pointer == me.data.unitData.Pointer)
                {
                    exp = Scale(Martial, exp);
                }
            }
            catch (Exception e)
            {
                Warn(Martial, e);
            }
        }

        public static void MovePrefix(UnitActionMovePlayer __instance)
        {
            if (!Move.On)
            {
                return;
            }
            try
            {
                WorldUnitBase me = Player;
                WorldUnitBase unit = __instance == null ? null : __instance.unit;
                if (me == null || unit == null || unit.data.unitData.unitID != me.data.unitData.unitID)
                {
                    return;
                }
                float before = __instance.costDay;
                if (before <= 0f)
                {
                    return;
                }
                __instance.costDay = before * Move.Value;
#if DEVTOOLS
                Move.Last = before.ToString(CultureInfo.InvariantCulture) + "->" + __instance.costDay.ToString(CultureInfo.InvariantCulture);
#endif
            }
            catch (Exception e)
            {
                Warn(Move, e);
            }
        }

        // 플레이어의 기술 행동이면 참.
        private static bool IsPlayerSkillAction(UnitActionBase action)
        {
            WorldUnitBase me = Player;
            WorldUnitBase unit = action == null ? null : action.unit;
            if (me == null || unit == null || unit.data.unitData.unitID != me.data.unitData.unitID)
            {
                return false;
            }
            return Array.IndexOf(ActionDayTypes, action.GetIl2CppType().Name) >= 0;
        }

        private static int ScaleDay(int day)
        {
            return day <= 0 ? day : (int)Math.Round(day * (double)ActionDay.Value, MidpointRounding.AwayFromZero);
        }

        // 지금 플레이어의 기술 행동이 Create 중인지. 그 안의 AddResidueDay만 곱한다.
        private static bool inSkillAction;

        public static void ActionDayPrefix(UnitActionBase __instance)
        {
            inSkillAction = false;
            if (!ActionDay.On)
            {
                return;
            }
            try
            {
                inSkillAction = IsPlayerSkillAction(__instance);
            }
            catch (Exception e)
            {
                Warn(ActionDay, e);
            }
        }

        public static void ActionDayPostfix()
        {
            inSkillAction = false;
        }

        public static void StudyGainPrefix(UnitActionPropMartialStudy __instance)
        {
            if (!StudyGain.On)
            {
                return;
            }
            try
            {
                WorldUnitBase me = Player;
                WorldUnitBase unit = __instance == null ? null : __instance.unit;
                if (me == null || unit == null || unit.data.unitData.unitID != me.data.unitData.unitID)
                {
                    return;
                }
                __instance.exp = Scale(StudyGain, __instance.exp);
            }
            catch (Exception e)
            {
                Warn(StudyGain, e);
            }
        }

        // 매개변수 이름이 다를 수 있어 자리(__0)로 받는다.
        public static void ResidueDayPrefix(ref int __0)
        {
            if (!inSkillAction || !ActionDay.On || __0 <= 0)
            {
                return;
            }
            try
            {
                int before = __0;
                __0 = ScaleDay(before);
#if DEVTOOLS
                ActionDay.Last = before + "->" + __0;
#endif
            }
            catch (Exception e)
            {
                Warn(ActionDay, e);
            }
        }

        public static void ActionDayCheckPrefix(UnitActionBase __instance, ref int day)
        {
            if (!ActionDay.On)
            {
                return;
            }
            try
            {
                if (IsPlayerSkillAction(__instance))
                {
                    day = ScaleDay(day);
                }
            }
            catch (Exception e)
            {
                Warn(ActionDay, e);
            }
        }

        public static bool InBattle
        {
            get
            {
                return SceneType.battle != null && SceneType.battle.battleMap != null
                    && SceneType.battle.battleMap.playerUnitCtrl != null;
            }
        }

        // 매 프레임 불린다. 전투 장면에 있는 동안만 속도를 맞추고, 벗어나거나 1배로 돌리면 되돌린다. 예외를 던지지 않는다.
        public static void Tick()
        {
            TickCooldown();
            TickSkillValues();
            TickMapSpeed();
            TickBattleMove();
            TickEnergy();
#if DEVTOOLS
            DevCommandsRate.TrackTick();
#endif
            if (!Speed.On && !speedApplied)
            {
                return;
            }
            try
            {
                if (!Speed.On || !InBattle)
                {
                    RestoreSpeed();
                    return;
                }
                // 0은 게임의 일시 정지다. 풀지 않는다.
                float now = Time.timeScale;
                if (now != 0f && now != Speed.Value)
                {
                    Time.timeScale = Speed.Value;
                }
                speedApplied = true;
            }
            catch (Exception e)
            {
                Warn(Speed, e);
                // 전투 장면을 판정하지 못하면 걸지 않는다.
                try
                {
                    RestoreSpeed();
                }
                catch (Exception)
                {
                }
            }
        }

        // 걸어 둔 속도를 1로 되돌린다. 게임이 정지(0)한 동안에는 쓰지 않고 걸었다는 기억을 남겨,
        // 정지가 풀린 프레임에 Tick이 되돌리게 한다(게임이 정지 전의 값을 되살려도 남지 않는다).
        private static void RestoreSpeed()
        {
            if (!speedApplied || Time.timeScale == 0f)
            {
                return;
            }
            // 지도 속도가 켜져 있으면 1로 덮지 않는다(전투를 나온 첫 프레임에 지도 속도를 지우지 않게, 최종 리뷰).
            if (!(MapSpeed.On && mapApplied))
            {
                Time.timeScale = 1f;
            }
            speedApplied = false;
        }

        // ---- 지도 속도 ----
        private static bool mapApplied;

        private static void TickMapSpeed()
        {
            if (!MapSpeed.On && !mapApplied)
            {
                return;
            }
            try
            {
                if (!MapSpeed.On || g.world == null || InBattle)
                {
                    RestoreMapSpeed();
                    return;
                }
                float now = Time.timeScale;
                if (now != 0f && now != MapSpeed.Value)
                {
                    Time.timeScale = MapSpeed.Value;
                }
                mapApplied = true;
            }
            catch (Exception e)
            {
                Warn(MapSpeed, e);
                try
                {
                    RestoreMapSpeed();
                }
                catch (Exception)
                {
                }
            }
        }

        // 지도 속도가 걸어 둔 것만 1로 되돌린다. 전투 속도가 건 것은 건드리지 않는다(전투 속도는 speedApplied로 따로 되돌린다).
        private static void RestoreMapSpeed()
        {
            if (!mapApplied || Time.timeScale == 0f)
            {
                return;
            }
            if (!speedApplied)
            {
                Time.timeScale = 1f;
            }
            mapApplied = false;
        }

        // ---- 전투 이동 속도 ----
        private static IntPtr moveUnit;             // 처리기를 건 전투 단위(전투마다 새로 만들어진다)
        private static DynInt moveSpeedOf;
        private static int moveAdd;
        private static DynInt.DynObjectAddHandler moveHandler;
        private const string MoveTag = "TlToybox.battleMove";

        private static void TickBattleMove()
        {
            if (!BattleMove.On && moveUnit == IntPtr.Zero)
            {
                return;
            }
            try
            {
                UnitCtrlPlayer ctrl = InBattle ? SceneType.battle.battleMap.playerUnitCtrl : null;
                UnitDataBase data = ctrl == null ? null : ctrl.data;
                if (!BattleMove.On || data == null)
                {
                    DetachMove(data);
                    return;
                }
                if (moveUnit != data.Pointer)
                {
                    // 새 전투: 지난 단위는 사라졌으므로 떼지 않고 잊는다.
                    moveUnit = IntPtr.Zero;
                    moveSpeedOf = null;
                }
                int want = (int)Math.Round(data.moveSpeed.baseValue * (BattleMove.Value - 1f));
                if (moveUnit == IntPtr.Zero)
                {
                    if (moveHandler == null)
                    {
                        moveHandler = UnhollowerRuntimeLib.DelegateSupport.ConvertDelegate<DynInt.DynObjectAddHandler>(new Func<int>(() => moveAdd));
                    }
                    moveAdd = want;
                    data.moveSpeed.AddValue(moveHandler, MoveTag);
                    moveUnit = data.Pointer;
                    moveSpeedOf = data.moveSpeed;
#if DEVTOOLS
                    BattleMove.Last = data.moveSpeed.baseValue + "->" + data.moveSpeed.value;
#endif
                }
                else if (moveAdd != want)
                {
                    moveAdd = want;
                }
            }
            catch (Exception e)
            {
                Warn(BattleMove, e);
            }
        }

        // 걸어 둔 처리기를 뗀다. 지금 전투의 단위가 건 단위와 같을 때만 DelValue를 부른다.
        private static void DetachMove(UnitDataBase data)
        {
            if (moveUnit == IntPtr.Zero)
            {
                return;
            }
            if (data != null && data.Pointer == moveUnit && moveSpeedOf != null)
            {
                moveSpeedOf.DelValue(moveHandler);
            }
            moveUnit = IntPtr.Zero;
            moveSpeedOf = null;
            moveAdd = 0;
        }

#if DEVTOOLS
        public static string MoveState()
        {
            if (!InBattle)
            {
                return "not in battle";
            }
            DynInt ms = SceneType.battle.battleMap.playerUnitCtrl.data.moveSpeed;
            return "moveSpeed value=" + ms.value + " base=" + ms.baseValue + " add=" + moveAdd + " attached=" + (moveUnit != IntPtr.Zero);
        }
#endif

        // ---- 정력 소모 ----
        private static int energyLast = int.MinValue;
        public static int EnergyRefunded;   // 이번 실행에서 돌려준 합(개발 명령이 읽는다)

        private static void TickEnergy()
        {
            if (!EnergyCost.On)
            {
                energyLast = int.MinValue;
                return;
            }
            try
            {
                WorldUnitBase player = Player;
                if (player == null)
                {
                    energyLast = int.MinValue;
                    return;
                }
                var saved = player.data.unitData.propertyData;
                int now = saved.energy;
                if (energyLast != int.MinValue)
                {
                    int back = RateGroups.EnergyRefund(energyLast, now, EnergyCost.Value);
                    if (back > 0)
                    {
                        int max = player.data.dynUnitData.energy.maxValue;
                        int next = Math.Min(max, now + back);
                        if (next > now)
                        {
                            saved.energy = next;
                            EnergyRefunded += next - now;
#if DEVTOOLS
                            EnergyCost.Last = energyLast + "->" + now + "->" + next;
#endif
                            now = next;
                        }
                    }
                }
                energyLast = now;
            }
            catch (Exception e)
            {
                Warn(EnergyCost, e);
            }
        }

        // 쿨타임 속도에 쓰는 값의 상한. "없음"은 원래 값의 만 배다(13초가 0.0013초).
        private const long CooldownMax = 100000000;
        private const float CooldownNone = 10000f;

        private static IntPtr cooldownUnit;     // 값을 떠 둔 전투 단위. 전투마다 새로 만들어진다
        private static int cooldownSkill;       // 그 단위의 원래 skillCDRate
        private static int cooldownStep;        // 원래 stepCDRate
        private static int cooldownItem;        // 원래 itemCDRate(전투 아이템 쿨타임 줄)

        // 전투 중인 플레이어의 쿨타임 속도를 맞춘다. 기술 쿨타임은 skillCDRate·stepCDRate, 전투 아이템 쿨타임은 itemCDRate.
        // 전투마다 원래 값을 떠 두고, 줄을 1배로 돌리면 그 비율을 원래 값으로 되돌린다.
        private static void TickCooldown()
        {
            bool any = Cooldown.On || ItemCooldown.On;
            if (!any && cooldownUnit == IntPtr.Zero)
            {
                return;
            }
            try
            {
                if (!InBattle)
                {
                    // 전투 단위가 보이지 않는다. 꺼져 있으면 되돌릴 것이 없으니 잊는다.
                    // 켜져 있으면 떠 둔 값을 남긴다: 같은 단위가 다시 보였을 때(연출 등으로 잠깐 사라진 경우)
                    // 이미 곱한 값을 원래 값으로 다시 뜨면 배율이 겹친다. 새 전투의 단위는 포인터가 달라 새로 뜬다.
                    if (!any)
                    {
                        cooldownUnit = IntPtr.Zero;
                    }
                    return;
                }
                UnitCtrlPlayer unit = SceneType.battle.battleMap.playerUnitCtrl;
                DynInt skill = unit.data.skillCDRate;
                DynInt step = unit.data.stepCDRate;
                DynInt item = unit.data.itemCDRate;
                if (unit.Pointer != cooldownUnit)
                {
                    if (!any)
                    {
                        cooldownUnit = IntPtr.Zero;
                        return;
                    }
                    cooldownUnit = unit.Pointer;
                    cooldownSkill = skill.baseValue;
                    cooldownStep = step.baseValue;
                    cooldownItem = item.baseValue;
                }
                ApplyCooldown(skill, cooldownSkill, Cooldown);
                ApplyCooldown(step, cooldownStep, Cooldown);
                ApplyCooldown(item, cooldownItem, ItemCooldown);
                if (!any)
                {
                    // 모두 원래 값으로 되돌렸다.
                    cooldownUnit = IntPtr.Zero;
                }
            }
            catch (Exception e)
            {
                Warn(ItemCooldown.On && !Cooldown.On ? ItemCooldown : Cooldown, e);
            }
        }

        // 비율 하나를 그 줄의 배율에 맞춘다(꺼져 있으면 원래 값). 원래 값이 0 이하이면 건드리지 않는다.
        private static void ApplyCooldown(DynInt value, int original, Rate rate)
        {
            if (original <= 0)
            {
                return;
            }
            int want = original;
            if (rate.On)
            {
                float factor = rate.Value == 0f ? CooldownNone : 1f / rate.Value;
                want = CooldownTarget(original, factor);
            }
            if (value.baseValue != want)
            {
                value.baseValue = want;
            }
        }

        // 기술마다의 값(DynInt.baseValue) 하나와 그 원래 값.
        private sealed class SkillValue
        {
            public DynInt Value;
            public int Original;
            public Rate Rate;
        }

        private static IntPtr skillUnit;   // 원래 값을 떠 둔 전투 단위. 전투마다 새로 만들어진다
        private static readonly System.Collections.Generic.Dictionary<IntPtr, SkillValue> skillValues = new System.Collections.Generic.Dictionary<IntPtr, SkillValue>();

        // 전투 중인 플레이어의 기술마다의 영력 소모·집중력 소모·신기 대기에 배율을 건다. 처음 본 값은 원래 값으로 떠 두고,
        // 1배로 돌리면 그 값으로 되돌린다. 전투 중에 새로 생긴 기술(호천안 기술을 바꾼 경우 등)은 보이는 대로 떠 둔다.
        private static void TickSkillValues()
        {
            bool any = ArtifactCooldown.On || MpCost.On || SpCost.On || Cooldown.On;
            if (!any && skillValues.Count == 0)
            {
                return;
            }
            try
            {
                if (!InBattle)
                {
                    // 같은 단위가 다시 보일 때 곱한 값을 원래 값으로 뜨지 않도록, 켜져 있으면 떠 둔 것을 남긴다.
                    if (!any)
                    {
                        skillValues.Clear();
                        skillUnit = IntPtr.Zero;
                    }
                    return;
                }
                UnitCtrlPlayer unit = SceneType.battle.battleMap.playerUnitCtrl;
                if (unit.Pointer != skillUnit)
                {
                    skillValues.Clear();
                    skillUnit = unit.Pointer;
                    if (!any)
                    {
                        return;
                    }
                }
                if (any)
                {
                    Collect(unit);
                }
                var done = new System.Collections.Generic.List<IntPtr>();
                foreach (var pair in skillValues)
                {
                    SkillValue v = pair.Value;
                    // 기술 쿨타임 줄(선법·영역의 cd)은 없음도 0이 아니라 하한(RateGroups.SkillCooldown)
                    int want = !v.Rate.On ? v.Original
                        : v.Rate == Cooldown ? RateGroups.SkillCooldown(v.Original, v.Rate.Value) : Scaled(v.Original, v.Rate.Value);
                    if (v.Value.baseValue != want)
                    {
                        v.Value.baseValue = want;
                    }
                    if (!v.Rate.On)
                    {
                        done.Add(pair.Key);
                    }
                }
                // 1배로 돌린 값은 되돌렸으니 잊는다(다시 켜면 그때의 값을 원래 값으로 뜬다).
                foreach (IntPtr key in done)
                {
                    skillValues.Remove(key);
                }
            }
            catch (Exception e)
            {
                Warn(MpCost.On ? MpCost : SpCost.On ? SpCost : ArtifactCooldown.On ? ArtifactCooldown : Cooldown, e);
            }
        }

        private static void Collect(UnitCtrlPlayer unit)
        {
            if (MpCost.On)
            {
                for (int i = 0; unit.skills != null && i < unit.skills.Count; i++)
                {
                    Track(Attack(unit.skills[i]) == null ? null : Attack(unit.skills[i]).mpCost, MpCost);
                }
                SkillDataAttack ult = Attack(unit.ultimate);
                Track(ult == null ? null : ult.mpCost, MpCost);
                Track(unit.step == null || unit.step.data == null ? null : unit.step.data.mpCost, MpCost);
            }
            SkillDataAttack eye = unit.eyeSkillBase == null ? null : Attack(unit.eyeSkillBase.skillAttack);
            if (MpCost.On)
            {
                Track(eye == null ? null : eye.mpCost, MpCost);
            }
            if (ArtifactCooldown.On)
            {
                Track(eye == null ? null : eye.cd, ArtifactCooldown);
                Track(unit.devilDemonSkill == null ? null : unit.devilDemonSkill.cd, ArtifactCooldown);
            }
            if (SpCost.On)
            {
                Track(unit.devilDemonSkill == null ? null : unit.devilDemonSkill.spCost, SpCost);
            }
            if (Cooldown.On)
            {
                // 선법·영역은 단위 비율 skillCDRate가 듣지 않아 기술마다의 cd를 곱한다(05 문서 87절, 회차 t4-b·t4-d).
                for (int i = 0; unit.immortalSkill != null && i < unit.immortalSkill.Count; i++)
                {
                    ImmortalSkillBase s = unit.immortalSkill[i];
                    Track(s == null || s.data == null ? null : s.data.cd, Cooldown);
                }
                Track(unit.fieldSkill == null || unit.fieldSkill.data == null ? null : unit.fieldSkill.data.cd, Cooldown);
            }
        }

        private static SkillDataAttack Attack(SkillAttack skill)
        {
            return skill == null || skill.data == null ? null : skill.data.TryCast<SkillDataAttack>();
        }

        private static void Track(DynInt value, Rate rate)
        {
            if (value == null || skillValues.ContainsKey(value.Pointer))
            {
                return;
            }
            skillValues[value.Pointer] = new SkillValue { Value = value, Original = value.baseValue, Rate = rate };
        }

        // 0 이하는 곱하지 않는다. 반올림은 0.5를 올린다(25의 절반은 13).
        private static int Scaled(int original, float factor)
        {
            if (original <= 0)
            {
                return original;
            }
            return (int)Math.Round(original * (double)factor, MidpointRounding.AwayFromZero);
        }

        // 원래 값이 0 이하이면(게임이 쿨타임을 멈춘 상태) 건드리지 않는다.
        private static int CooldownTarget(int original, float factor)
        {
            if (original <= 0)
            {
                return original;
            }
            double scaled = Math.Round(original * (double)factor);
            return scaled > CooldownMax ? (int)CooldownMax : (int)scaled;
        }
    }
}
