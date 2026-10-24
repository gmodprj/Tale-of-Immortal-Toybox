using System;
using System.Reflection;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 날짜: 달력 표기, 게임의 날짜 진행 실행, 달 넘기기, 달력 직접 지정.
    // 구조와 쓰기 경로는 docs/superpowers/specs/2026-10-12-date-bag-design.md 3절에서 실측했다.
    //   누적 일수 g.data.world.worldDay, 달 안의 경과 g.world.run.roundDay, 플레이어의 남은 일수 unitData.residueDay.
    //   플레이어의 행동은 WorldUnitBase.AddResidueDay로 날짜를 쓴다. WorldRunMgr.AddDay를 직접 부르면 남은 일수가 어긋난다.
    //   날짜 고정은 AddResidueDay에서 플레이어 행동분만 건너뛴다. 게임의 달 넘기기 버튼(OnSkipMonth)도 막는다.
    internal static class TimeService
    {
        public const int MaxYear = 9999;

        private static HarmonyLib.Harmony harmony;

        // 날짜 고정. 게임을 켤 때마다 꺼진 상태로 시작한다.
        public static bool Frozen { get; private set; }

        public static string SetFrozen(bool on)
        {
            if (on == Frozen)
            {
                return "날짜 고정: 이미 " + (on ? "켜짐" : "꺼짐") + "입니다.";
            }
            if (on)
            {
                string error = EnsurePatched();
                if (error != null)
                {
                    return error;
                }
            }
            Frozen = on;
            ChangeLog.Add("세계", "날짜 고정(행동해도 날짜가 흐르지 않음)", on ? "꺼짐" : "켜짐", on ? "켜짐" : "꺼짐", true);
            return "날짜 고정: " + (on ? "켜짐" : "꺼짐");
        }

        // 고정을 처음 켤 때 한 번 건다. 한 번도 켜지 않으면 게임에 아무것도 걸지 않는다. 건 뒤에는 떼지 않고 Frozen만 본다.
        private static string EnsurePatched()
        {
            if (harmony != null)
            {
                return null;
            }
            var instance = new HarmonyLib.Harmony("TlToybox.time");
            try
            {
                const BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
                instance.Patch(typeof(WorldUnitBase).GetMethod("AddResidueDay"),
                    new HarmonyLib.HarmonyMethod(typeof(TimeService).GetMethod("ResiduePrefix", flags)));
                instance.Patch(typeof(UIMapMainPlayerInfo).GetMethod("OnSkipMonth"),
                    new HarmonyLib.HarmonyMethod(typeof(TimeService).GetMethod("SkipPrefix", flags)));
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 날짜 고정 패치 실패: " + e);
                instance.UnpatchSelf();
                return "날짜 고정: 게임에 연결하지 못해 켜지 않았습니다(" + e.GetType().Name + ").";
            }
            harmony = instance;
            return null;
        }

        // 플레이어의 행동이 쓰는 날짜만 건너뛴다. NPC와 달 넘기기(isAutoRun)는 통과시킨다.
        // 달 넘기기를 여기서 막으면 화면이 어긋난 채 남는다(스펙 3.2절).
        public static bool ResiduePrefix(WorldUnitBase __instance, int day, bool isAutoRun, ref bool __result)
        {
            if (!Frozen || isAutoRun)
            {
                return true;
            }
            WorldUnitBase player = g.world == null ? null : g.world.playerUnit;
            if (player == null || __instance == null || __instance.data.unitData.unitID != player.data.unitData.unitID)
            {
                return true;
            }
            __result = true;
            return false;
        }

        // 고정 중에는 게임의 달 넘기기 버튼을 막고 이유를 알린다.
        public static bool SkipPrefix()
        {
            if (!Frozen)
            {
                return true;
            }
            try
            {
                UITipItem.AddTip("날짜 고정 중에는 달을 넘길 수 없습니다. 편집기의 세계 탭에서 고정을 끄세요.", 3f);
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 날짜 고정 알림 실패: " + e.Message);
            }
            return false;
        }

        private static WorldUnitBase Player
        {
            get { return g.world.playerUnit; }
        }

        public static int WorldDay
        {
            get { return g.data.world.worldDay; }
        }

        public static int ResidueDay
        {
            get { return Player.data.unitData.residueDay; }
        }

        // 누적 일수의 연·월·일. 게임의 변환을 그대로 쓴다.
        public static int[] ToDate(int worldDay)
        {
            var date = GameTool.DayToDate(worldDay);
            return new[] { date[0], date[1], date[2] };
        }

        public static string DateText(int worldDay)
        {
            int[] date = ToDate(worldDay);
            return date[0] + "년 " + date[1] + "월 " + date[2] + "일";
        }

        public static string Describe()
        {
            return "제 " + DateText(WorldDay) + " (누적 " + WorldDay + "일 · 이번 달 남은 날 " + ResidueDay + ")";
        }

        // 연·월·일을 누적 일수로 바꾼다. 게임의 변환으로 되돌려 같은 날짜가 나올 때만 성공한다(달력 규칙이 바뀌었을 때의 안전장치).
        public static bool TryToWorldDay(int year, int month, int day, out int worldDay, out string error)
        {
            worldDay = 0;
            if (year < 1 || year > MaxYear || month < 1 || month > 12 || day < 1 || day > 30)
            {
                error = "날짜: 연 1~" + MaxYear + ", 월 1~12, 일 1~30으로 입력하세요.";
                return false;
            }
            int ticks = g.world.run.roundDayMax + 1;
            worldDay = (year - 1) * 12 * ticks + (month - 1) * ticks + (day - 1);
            int[] back = ToDate(worldDay);
            if (back[0] != year || back[1] != month || back[2] != day)
            {
                error = "날짜: 게임의 달력 계산과 맞지 않아 적용하지 않았습니다(" + year + "-" + month + "-" + day + " -> "
                    + back[0] + "-" + back[1] + "-" + back[2] + ").";
                return false;
            }
            error = null;
            return true;
        }

        private const string RunningMessage = "날짜: 게임이 달을 넘기는 중입니다. 편집기를 닫고 게임 화면에 뜬 창이 있으면 먼저 처리한 뒤 다시 하세요.";

        // 달이 넘어가는 중(월말 처리)인지. 처리 도중에 NPC 대화가 떠서 대답을 기다리는 동안에도 참이다(실측).
        // 이때 날짜를 쓰면 처리 중인 달과 어긋난다.
        private static bool Running
        {
            get { return g.world.run.isRunning; }
        }

        // 지도 화면의 날짜 표기를 다시 그리고, 남은 날이 생겼으면 "이번 달의 시간을 전부 사용" 띠를 끈다.
        // 띠는 남은 날이 0이 될 때 게임이 켜고 달이 넘어갈 때 끈다. UpdateUI·UpdateSkipMonth·UpdatePlayerInfo로는 꺼지지 않는다(실측).
        private static void RefreshHud()
        {
            UIMapMain map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
            if (map == null || map.uiPlayerInfo == null)
            {
                return;
            }
            UIMapMainPlayerInfo info = map.uiPlayerInfo;
            info.UpdateUI();
            if (Player.data.unitData.residueDay > 0)
            {
                if (info.goSkipMonth != null)
                {
                    info.goSkipMonth.SetActive(false);
                }
                if (info.goSkipMonth_En != null)
                {
                    info.goSkipMonth_En.SetActive(false);
                }
            }
        }

        // 게임의 날짜 진행을 실행한다. 이번 달 남은 날까지만 진행한다.
        public static string Advance(int days)
        {
            if (days < 1)
            {
                return "날짜: 1일 이상이어야 합니다.";
            }
            if (Frozen)
            {
                return "날짜: 날짜 고정 중에는 진행할 수 없습니다. 먼저 고정을 끄세요.";
            }
            if (Running)
            {
                return RunningMessage;
            }
            WorldUnitBase player = Player;
            int residue = player.data.unitData.residueDay;
            if (residue <= 0)
            {
                return "날짜: 이번 달 남은 날이 없습니다. 게임에서 달을 넘기거나 날짜를 직접 지정하세요.";
            }
            int step = Math.Min(days, residue);
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int before = WorldDay;
            player.AddResidueDay(step, false);
            int after = WorldDay;
            int afterResidue = player.data.unitData.residueDay;
            RefreshHud();
            bool verified = after == before + step && afterResidue == residue - step;
            ChangeLog.Add("세계", "날짜", DateText(before), DateText(after), verified);
            if (!verified)
            {
                return "날짜: 진행을 실행했지만 값이 기대와 다릅니다(누적 " + before + " -> " + after + ", 남은 날 " + residue + " -> " + afterResidue + ").";
            }
            string head = step < days ? "이번 달 남은 날이 " + residue + "일이라 " + step + "일만 진행했습니다" : step + "일 진행";
            return "날짜: " + head + " (" + DateText(before) + " -> " + DateText(after) + ", 이번 달 남은 날 " + afterResidue + ")";
        }

        // 게임의 달 넘기기 확인 창을 연다. 진행은 게임의 절차다. 열었으면 null, 못 열면 이유를 돌려준다.
        public static string OpenSkipMonth()
        {
            if (Frozen)
            {
                return "달 넘기기: 날짜 고정 중에는 할 수 없습니다. 먼저 고정을 끄세요.";
            }
            if (Running)
            {
                return RunningMessage;
            }
            UIMapMain map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
            if (map == null || map.uiPlayerInfo == null)
            {
                return "달 넘기기: 지도 화면에서만 할 수 있습니다.";
            }
            // 달이 넘어가면 게임이 자동 저장한다.
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            map.uiPlayerInfo.OnSkipMonth();
            return null;
        }

        // 달력을 직접 지정한다. 누적 일수, 달 안의 경과, 플레이어의 남은 일수를 함께 쓴다.
        // 그 사이의 월말 처리는 일어나지 않고, 날짜 기준 기록(임무 기한 등)은 건드리지 않는다.
        public static string SetDate(int year, int month, int day)
        {
            int target;
            string error;
            if (!TryToWorldDay(year, month, day, out target, out error))
            {
                return error;
            }
            if (Running)
            {
                return RunningMessage;
            }
            int before = WorldDay;
            if (target == before)
            {
                return "날짜: 이미 " + DateText(before) + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            WorldUnitBase player = Player;
            var run = g.world.run;
            int inMonth = day - 1;
            int residue = run.roundDayMax - inMonth;
            g.data.world.worldDay = target;
            run.roundDay = inMonth;
            player.data.unitData.residueDay = residue;
            RefreshHud();
            bool verified = WorldDay == target && run.roundDay == inMonth && player.data.unitData.residueDay == residue;
            ChangeLog.Add("세계", "날짜(직접 지정)", DateText(before), DateText(WorldDay), verified);
            return verified
                ? "날짜: " + DateText(before) + " -> " + DateText(target) + " (누적 " + before + "일 -> " + target + "일)"
                : "날짜: 썼지만 재조회 값이 다릅니다(누적 " + WorldDay + ", 달 안 " + run.roundDay + ", 남은 날 " + player.data.unitData.residueDay + ").";
        }
    }
}
