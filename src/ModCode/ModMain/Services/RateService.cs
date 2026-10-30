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
    internal static partial class RateService
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
            ForgetCooldown();
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
            int result = RateGroups.ScaleValue(value, rate.Value);
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
            return RateGroups.ScaleDay(day, ActionDay.Value);
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
    }
}
