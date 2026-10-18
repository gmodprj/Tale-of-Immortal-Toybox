using System;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 세계(위치)와 전투 보조. 날짜는 TimeService에 있다. 쓰기 경로는 docs/04-ingame-state-analysis.md 15절 4단계에서 실측한 것이다.
    internal static class WorldService
    {
        public static Vector2Int PlayerPoint
        {
            get { return g.world.playerUnit.data.unitData.GetPoint(); }
        }

        // 플레이어를 대지도의 다른 칸으로 옮긴다. 게임의 위치 설정 행동을 쓴다.
        public static string Teleport(Vector2Int target, string reason)
        {
            WorldUnitBase player = g.world.playerUnit;
            Vector2Int before = player.data.unitData.GetPoint();
            if (target.x == before.x && target.y == before.y)
            {
                return "이동: 이미 그 칸에 있습니다.";
            }
            var action = new UnitActionSetPoint(target);
            if (player.IsCreateAction(action, false) != 0)
            {
                return "이동: 게임이 (" + target.x + ", " + target.y + ") 칸으로의 이동을 허용하지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            player.CreateAction(new UnitActionSetPoint(target), false);
            Vector2Int after = player.data.unitData.GetPoint();
            bool verified = after.x == target.x && after.y == target.y;
            ChangeLog.Add(EditService.NameOf(player), "위치" + (reason == null ? "" : "(" + reason + ")"),
                before.x + "," + before.y, after.x + "," + after.y, verified);
            return verified ? "이동: (" + before.x + ", " + before.y + ") -> (" + after.x + ", " + after.y + ")"
                : "이동: 행동을 실행했지만 위치가 (" + after.x + ", " + after.y + ")입니다.";
        }

        public static string TeleportBy(int dx, int dy)
        {
            Vector2Int here = PlayerPoint;
            return Teleport(new Vector2Int(here.x + dx, here.y + dy), null);
        }

        public static string TeleportToUnit(WorldUnitBase unit)
        {
            return Teleport(unit.data.unitData.GetPoint(), EditService.NameOf(unit) + "에게");
        }

    }

    // 전투 보조: 플레이어가 받는 피해를 0으로 하거나, 플레이어가 주는 피해에 배율을 건다.
    // 게임의 피해 계산 이벤트(EBattleType.UnitHitDynIntHandler)를 구독해 값을 바꾼다.
    // 대상은 플레이어 단위(UnitType.Player)뿐이다. 동료·NPC·몬스터끼리의 피해는 건드리지 않는다.
    // 설정은 저장하지 않으며 게임을 켤 때마다 꺼진 상태로 시작한다.
    internal static class BattleService
    {
        private static Il2CppSystem.Action<ETypeData> handler;
        private static bool subscribed;

        public static bool God;
        public static int Multiplier = 1;
        public static readonly int[] Multipliers = { 1, 2, 5, 10, 100 };

        private static bool Wanted
        {
            get { return God || Multiplier > 1 || RateService.Taken.On; }
        }

        // 받는 피해 배율이 바뀌었을 때 구독 상태를 맞춘다.
        public static void Refresh()
        {
            Sync();
        }

        // 이벤트 관리자는 세이브에 진입할 때 새로 만들어질 수 있으므로, ModMain.Init마다 구독 상태를 다시 맞춘다.
        public static void OnInit()
        {
            subscribed = false;
            Sync();
        }

        public static void OnDestroy()
        {
            Unsubscribe();
        }

        public static string SetGod(bool on)
        {
            God = on;
            Sync();
            ChangeLog.Add("전투", "무적(플레이어가 받는 피해 0)", on ? "꺼짐" : "켜짐", on ? "켜짐" : "꺼짐", true);
            return "무적: " + (on ? "켜짐" : "꺼짐");
        }

        public static string NextMultiplier()
        {
            int before = Multiplier;
            int index = Array.IndexOf(Multipliers, Multiplier);
            Multiplier = Multipliers[(index + 1) % Multipliers.Length];
            Sync();
            ChangeLog.Add("전투", "피해 배율(플레이어가 주는 피해)", "x" + before, "x" + Multiplier, true);
            return "피해 배율: x" + Multiplier;
        }

        // 무적을 끄고 피해 배율을 1로 되돌린다(배율 탭의 "모두 1배로").
        public static void Reset()
        {
            if (God)
            {
                SetGod(false);
            }
            if (Multiplier != 1)
            {
                int before = Multiplier;
                Multiplier = 1;
                Sync();
                ChangeLog.Add("전투", "피해 배율(플레이어가 주는 피해)", "x" + before, "x1", true);
            }
        }

        private static void Sync()
        {
            if (Wanted && !subscribed)
            {
                if (handler == null)
                {
                    handler = new Action<ETypeData>(OnHit);
                }
                g.events.On(EBattleType.UnitHitDynIntHandler, handler);
                subscribed = true;
            }
            else if (!Wanted && subscribed)
            {
                Unsubscribe();
            }
        }

        private static void Unsubscribe()
        {
            if (subscribed && handler != null)
            {
                g.events.Off(EBattleType.UnitHitDynIntHandler, handler);
            }
            subscribed = false;
        }

#if DEVTOOLS
        // 개발 전용: 구독 여부와, 이벤트가 불린 횟수·배율을 건 횟수·마지막으로 건 값.
        public static int DevHits;
        public static int DevScaled;
        public static string DevLast = "-";
        public static string DevDrop = "-";
        public static int DevOnPlayer;
        public static string DevTaken = "-";
        private static System.IntPtr devTarget;
        private static int devTargetHp;
        private static int devTargetValue;

        public static string DevState
        {
            get { return "subscribed=" + subscribed + " hits=" + DevHits + " scaled=" + DevScaled + " onPlayer=" + DevOnPlayer + " last=" + DevLast + " drop=" + DevDrop + " taken=" + DevTaken; }
        }
#endif

        // 타격마다 게임이 부른다. 여기서 난 예외가 전투로 번지지 않게 막고, 같은 예외는 한 번만 로그에 남긴다(리뷰 08의 38번).
        private static void OnHit(ETypeData e)
        {
            try
            {
                OnHitCore(e);
            }
            catch (Exception ex)
            {
                string why = ex.GetType().Name + ": " + ex.Message;
                if (why != hitWarned)
                {
                    hitWarned = why;
                    MelonLoader.MelonLogger.Msg("[TlToybox] 타격 처리 오류(배율·무적을 이번 타격에 걸지 못함): " + why);
                }
            }
        }

        private static string hitWarned;

        private static void OnHitCore(ETypeData e)
        {
#if DEVTOOLS
            DevHits++;
#endif
            var data = e.Cast<EBattleTypeData.UnitHitDynIntHandler>();
            if (data.hitUnit != null && data.hitUnit.data != null && data.hitUnit.data.unitType == UnitType.Player)
            {
#if DEVTOOLS
                DevOnPlayer++;
#endif
                if (God)
                {
                    data.dynV.baseValue = 0;
                    data.dynV.ClearCall();
                }
                else if (RateService.Taken.On && data.dynV.baseValue > 0)
                {
                    int before = data.dynV.baseValue;
                    // 배율로는 0이 되지 않게 한다(피해 1에 x0.5를 걸면 0이 되어 무적과 같아진다. 0은 무적의 몫이다).
                    data.dynV.baseValue = Math.Max(1, (int)Math.Round(before * (double)RateService.Taken.Value));
#if DEVTOOLS
                    DevTaken = "base " + before + "->" + data.dynV.baseValue;
#endif
                }
                return;
            }
            UnitCtrlBase attacker = data.hitData == null ? null : data.hitData.attackUnit;
            if (Multiplier > 1 && attacker != null && attacker.data != null && attacker.data.unitType == UnitType.Player)
            {
#if DEVTOOLS
                int devBase = data.dynV.baseValue;
                int devValue = data.dynV.value;
#endif
                long scaled = (long)data.dynV.baseValue * Multiplier;
                data.dynV.baseValue = scaled > int.MaxValue ? int.MaxValue : (int)scaled;
#if DEVTOOLS
                DevScaled++;
                DevLast = "base " + devBase + "->" + data.dynV.baseValue + " value " + devValue + "->" + data.dynV.value;
                // 같은 적을 다시 때렸을 때, 앞선 타격이 실제로 깎은 체력을 적는다(화면의 숫자가 남은 체력으로 잘리는지 가린다).
                if (data.hitUnit != null)
                {
                    int hpNow = data.hitUnit.data.hp;
                    if (data.hitUnit.Pointer == devTarget)
                    {
                        DevDrop = "prevValue=" + devTargetValue + " hp " + devTargetHp + "->" + hpNow + " max=" + data.hitUnit.data.maxHP.value;
                    }
                    devTarget = data.hitUnit.Pointer;
                    devTargetHp = hpNow;
                    devTargetValue = data.dynV.value;
                    DevLast += " targetHp=" + hpNow + "/" + data.hitUnit.data.maxHP.value;
                }
#endif
            }
        }
    }
}
