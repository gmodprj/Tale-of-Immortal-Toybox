using System;
using UnityEngine;

namespace MOD_sWP6UL
{
    // RateService의 매 프레임 부분(13 문서 R7로 나눔): 전투 속도·지도 속도·전투 이동 속도·정력 돌려주기·쿨타임 비율·기술 값.
    // 전투 플레이어 단위 하나에 떠 둔 값의 전환 규칙은 RateUnitRules(스펙 2026-10-30-rate-service-cleanup-design.md 2절).
    internal static partial class RateService
    {
        private static bool speedApplied;

        // 전투 플레이어 단위가 보이는지(전투 장면·전투 지도·플레이어 컨트롤러가 모두 있음).
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
                if (data == null)
                {
                    // 전투 밖: 켜져 있으면 기억을 남긴다(같은 단위가 다시 보이면 붙어 있는지로 판정한다). 꺼져 있으면 잊는다(처리기는 단위와 함께 사라진다).
                    if (!BattleMove.On)
                    {
                        ForgetMove();
                    }
                    return;
                }
                // 붙어 있는지는 포인터가 아니라 DynInt의 처리기 목록(태그)으로 본다(13 문서 A2: 두 번 붙어 배율이 겹치지 않게. 스펙 2절).
                bool attached = IsMoveAttached(data.moveSpeed);
                if (RateUnitRules.MoveDetachNeeded(BattleMove.On, attached))
                {
                    data.moveSpeed.DelValue(moveHandler);
                    ForgetMove();
                    return;
                }
                if (!BattleMove.On)
                {
                    ForgetMove();
                    return;
                }
                int want = (int)Math.Round(data.moveSpeed.baseValue * (BattleMove.Value - 1f));
                if (RateUnitRules.MoveAttachNeeded(BattleMove.On, attached))
                {
                    if (moveHandler == null)
                    {
                        moveHandler = UnhollowerRuntimeLib.DelegateSupport.ConvertDelegate<DynInt.DynObjectAddHandler>(new Func<int>(() => moveAdd));
                    }
                    moveAdd = want;
                    data.moveSpeed.AddValue(moveHandler, MoveTag);
#if DEVTOOLS
                    BattleMove.Last = data.moveSpeed.baseValue + "->" + data.moveSpeed.value;
#endif
                }
                else if (moveAdd != want)
                {
                    moveAdd = want;
                }
                moveUnit = data.Pointer;
                moveSpeedOf = data.moveSpeed;
            }
            catch (Exception e)
            {
                Warn(BattleMove, e);
            }
        }

        // 그 DynInt에 이 모드의 처리기(태그 MoveTag)가 붙어 있는지.
        private static bool IsMoveAttached(DynInt speed)
        {
            var calls = speed == null ? null : speed.allAddCall;
            for (int i = 0; calls != null && i < calls.Count; i++)
            {
                if (calls[i].t2 == MoveTag)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ForgetMove()
        {
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
        private static bool skillApplied;       // 기술 쿨타임 줄이 이 단위의 skill·stepCDRate에 걸려 있다
        private static bool itemApplied;        // 전투 아이템 쿨타임 줄이 itemCDRate에 걸려 있다
        private static int skillWritten, stepWritten, itemWritten;   // 마지막으로 쓴 값(같은 단위의 값이 바뀌었는지 보는 데 쓴다. 스펙 2절)

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
                bool inBattle = InBattle;
                UnitCtrlPlayer unit = inBattle ? SceneType.battle.battleMap.playerUnitCtrl : null;
                DynInt skill = unit == null ? null : unit.data.skillCDRate;
                DynInt step = unit == null ? null : unit.data.stepCDRate;
                DynInt item = unit == null ? null : unit.data.itemCDRate;
                bool held = cooldownUnit != IntPtr.Zero;
                bool applied = skillApplied || itemApplied;
                bool same = unit != null && unit.Pointer == cooldownUnit;
                // 쓴 줄의 값만 비교한다. 안 쓴 줄(그 Written은 낡은 값)까지 비교하면 매 프레임 "바뀜"으로 보여 곱한 값을 원래 값으로 다시 뜬다(회차 rsc-a-1030에서 1e8까지 겹침).
                bool intact = !applied || (same
                    && (!skillApplied || (skill.baseValue == skillWritten && step.baseValue == stepWritten))
                    && (!itemApplied || item.baseValue == itemWritten));
                switch (RateUnitRules.Decide(inBattle, any, held, applied, same, intact))
                {
                    case RateUnitRules.Step.Skip:
                        return;
                    case RateUnitRules.Step.Forget:
                        ForgetCooldown();
                        return;
                    case RateUnitRules.Step.Capture:
                        cooldownUnit = unit.Pointer;
                        cooldownSkill = skill.baseValue;
                        cooldownStep = step.baseValue;
                        cooldownItem = item.baseValue;
                        skillApplied = false;
                        itemApplied = false;
                        break;
                }
                // 줄마다 켜져 있을 때와 되돌릴 때만 쓴다. 다른 줄만 켰을 때 꺼진 줄의 비율(게임 값)을 덮지 않는다(최종 리뷰).
                if (RateGroups.WritesRate(Cooldown.On, skillApplied))
                {
                    skillWritten = ApplyCooldown(skill, cooldownSkill, Cooldown);
                    stepWritten = ApplyCooldown(step, cooldownStep, Cooldown);
                    skillApplied = Cooldown.On;
                }
                if (RateGroups.WritesRate(ItemCooldown.On, itemApplied))
                {
                    itemWritten = ApplyCooldown(item, cooldownItem, ItemCooldown);
                    itemApplied = ItemCooldown.On;
                }
                if (!any)
                {
                    // 모두 원래 값으로 되돌렸다.
                    ForgetCooldown();
                }
            }
            catch (Exception e)
            {
                Warn(ItemCooldown.On && !Cooldown.On ? ItemCooldown : Cooldown, e);
            }
        }

        private static void ForgetCooldown()
        {
            cooldownUnit = IntPtr.Zero;
            skillApplied = false;
            itemApplied = false;
        }

        // 비율 하나를 그 줄의 배율에 맞춘다(꺼져 있으면 원래 값). 원래 값이 0 이하이면 건드리지 않는다.
        // 돌려주는 값은 지금 그 비율의 baseValue(쓴 값 또는 건드리지 않은 값).
        private static int ApplyCooldown(DynInt value, int original, Rate rate)
        {
            if (original <= 0)
            {
                return value.baseValue;
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
            return want;
        }

        // 기술마다의 값(DynInt.baseValue) 하나와 그 원래 값, 마지막으로 쓴 값.
        private sealed class SkillValue
        {
            public DynInt Value;
            public int Original;
            public int Written;
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
                bool inBattle = InBattle;
                UnitCtrlPlayer unit = inBattle ? SceneType.battle.battleMap.playerUnitCtrl : null;
                // 되돌린 항목은 지워지므로 항목이 남아 있다 = 쓴 값이 있다. 항목마다의 "그대로인지"는 아래에서 따로 본다.
                switch (RateUnitRules.Decide(inBattle, any, skillUnit != IntPtr.Zero, skillValues.Count > 0, unit != null && unit.Pointer == skillUnit, true))
                {
                    case RateUnitRules.Step.Skip:
                        return;
                    case RateUnitRules.Step.Forget:
                        skillValues.Clear();
                        skillUnit = IntPtr.Zero;
                        return;
                    case RateUnitRules.Step.Capture:
                        skillValues.Clear();
                        skillUnit = unit.Pointer;
                        break;
                }
                if (any)
                {
                    Collect(unit);
                }
                var done = new System.Collections.Generic.List<IntPtr>();
                foreach (var pair in skillValues)
                {
                    SkillValue v = pair.Value;
                    if (v.Value.baseValue != v.Written)
                    {
                        // 게임이 값을 바꿨거나 같은 주소에 새 값이 잡혔다(스펙 2절). 옛 원래 값을 쓰지 않고 이 항목을 잊는다.
                        done.Add(pair.Key);
                        continue;
                    }
                    // 기술 쿨타임 줄(선법·영역의 cd)은 없음도 0이 아니라 하한(RateGroups.SkillCooldown)
                    int want = !v.Rate.On ? v.Original
                        : v.Rate == Cooldown ? RateGroups.SkillCooldown(v.Original, v.Rate.Value) : Scaled(v.Original, v.Rate.Value);
                    if (v.Value.baseValue != want)
                    {
                        v.Value.baseValue = want;
                    }
                    v.Written = want;
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
            skillValues[value.Pointer] = new SkillValue { Value = value, Original = value.baseValue, Written = value.baseValue, Rate = rate };
        }

        // 0 이하는 곱하지 않는다. 반올림은 0.5를 올린다(25의 절반은 13).
        private static int Scaled(int original, float factor)
        {
            return RateGroups.ScaleSkillValue(original, factor);
        }

        // 원래 값이 0 이하이면(게임이 쿨타임을 멈춘 상태) 건드리지 않는다.
        private static int CooldownTarget(int original, float factor)
        {
            return RateGroups.CooldownRateValue(original, factor, CooldownMax);
        }
    }
}
