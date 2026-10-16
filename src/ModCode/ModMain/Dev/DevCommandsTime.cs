using System.Text;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. 날짜와 가방의 상태를 읽고, 날짜를 쓰는 게임 행동(이동·수련)을 실행하는 검증 명령.
    // tools/research/datebag-check.ps1이 이 명령들의 출력 모양에 의존한다.
    internal static class DevCommandsTime
    {
        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world.playerUnit;
            switch (p[0])
            {
                case "wdate":
                    return Date(me);
                case "grid":
                    return Grid(me);
                case "wmove":
                    {
                        Vector2Int before = me.data.unitData.GetPoint();
                        var target = new Vector2Int(before.x + int.Parse(p[1]), before.y + int.Parse(p[2]));
                        int can = me.IsCreateAction(new UnitActionMovePlayer(target), false);
                        int code = can == 0 ? me.CreateAction(new UnitActionMovePlayer(target), false) : -1;
                        Vector2Int after = me.data.unitData.GetPoint();
                        return "IsCreateAction=" + can + " CreateAction=" + code + " point " + before.x + "," + before.y
                            + " -> " + after.x + "," + after.y + "; " + Date(me);
                    }
                case "wexp":
                    {
                        Vector2Int point = me.data.unitData.GetPoint();
                        var saved = me.data.unitData.propertyData;
                        var probe = new UnitActionGridGetExp(point);
                        int can = me.IsCreateAction(probe, false);
                        int before = saved.exp;
                        int code = can == 0 ? me.CreateAction(new UnitActionGridGetExp(point), false) : -1;
                        return "IsCreateAction=" + can + " day=" + probe.day + " CreateAction=" + code + " exp " + before
                            + " -> " + saved.exp + "; " + Date(me);
                    }
                case "tdesc":
                    return "frozen=" + TimeService.Frozen + " " + TimeService.Describe();
                case "unload":
                    // Destroy가 부르는 상태 되돌리기만 부른다(창은 두고). 검사 status가 쓴다.
                    ModMain.ResetState();
                    return "reset frozen=" + TimeService.Frozen + " god=" + BattleService.God + " mul=" + BattleService.Multiplier;
                case "tfreeze":
                    return TimeService.SetFrozen(p[1] == "on");
                case "tadv":
                    return TimeService.Advance(int.Parse(p[1]));
                case "tset":
                    return TimeService.SetDate(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]));
                case "tskip":
                    return TimeService.OpenSkipMonth() ?? "opened";
                case "tin":
                    return EditorWindow.DevDateState;
                case "tmsg":
                    return "message=" + EditorWindow.DevMessage;
                case "bag":
                    return BagService.Change(int.Parse(p[1]));
                case "bagstat":
                    return "id=" + g.data.world.playerUnitID + " extra=" + BagService.Extra + " " + BagService.Describe();
                case "thud":
                    {
                        // HUD의 "이번 달의 시간을 전부 사용" 띠의 상태를 보고, 갱신 메서드를 하나씩 불러 본다.
                        UIMapMain map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
                        if (map == null || map.uiPlayerInfo == null)
                        {
                            return "MapMain is not open";
                        }
                        UIMapMainPlayerInfo info = map.uiPlayerInfo;
                        string called = p.Length > 1 ? p[1] : "none";
                        if (called == "skip") { info.UpdateSkipMonth(); }
                        if (called == "info") { info.UpdatePlayerInfo(); }
                        if (called == "ui") { info.UpdateUI(); }
                        return "called=" + called + " goSkipMonth=" + info.goSkipMonth.activeSelf + " goSkipMonth_En=" + info.goSkipMonth_En.activeSelf;
                    }
                case "gridfill":
                    return Fill(me, int.Parse(p[1]), p.Length > 2 ? int.Parse(p[2]) : -1);
                default:
                    return null;
            }
        }

        private static string Date(WorldUnitBase me)
        {
            var run = g.world.run;
            var date = GameTool.DayToDate(g.data.world.worldDay);
            return "worldDay=" + g.data.world.worldDay + " date=" + date[0] + "-" + date[1] + "-" + date[2]
                + " roundDay=" + run.roundDay + "/" + run.roundDayMax
                + " residueDay=" + me.data.unitData.residueDay
                + " age=" + me.data.unitData.propertyData.age
                + " moveDay=" + UnitActionMovePlayer.GetCurDay(me)
                + " running=" + run.isRunning + " canRun=" + run.IsCanRun();
        }

        private static string Grid(WorldUnitBase me)
        {
            var props = me.data.unitData.propData;
            DynInt grid = me.data.dynUnitData.propGridNum;
            var sb = new StringBuilder();
            sb.Append("cur=").Append(props.GetGridCurNum()).Append(" max=").Append(props.GetGridMaxNum())
                .Append(" base=").Append(grid.baseValue).Append(" add=[");
            var adds = grid.allAddCall;
            for (int i = 0; adds != null && i < adds.Count; i++)
            {
                sb.Append(i == 0 ? "" : ", ").Append(adds[i].t2).Append('=').Append(adds[i].t1.Invoke());
            }
            sb.Append("] items=").Append(props.allProps.Count);
            return sb.ToString();
        }

        // 가지고 있지 않은 아이템을 한 개씩 지급해 가방 칸을 채운다. 지급이 안 되면 거기서 멈춘다.
        private static string Fill(WorldUnitBase me, int count, int mainType)
        {
            var props = me.data.unitData.propData;
            int before = props.GetGridCurNum();
            int given = 0;
            string stopped = "";
            foreach (ItemCatalog.Entry entry in ItemCatalog.Find("", mainType, -1))
            {
                if (given >= count)
                {
                    break;
                }
                if (props.GetPropsNum(entry.Id) > 0)
                {
                    continue;
                }
                me.data.RewardPropItem(entry.Id, 1, false);
                if (props.GetPropsNum(entry.Id) == 0)
                {
                    stopped = "stoppedAt=" + entry.Id;
                    break;
                }
                given++;
            }
            return "given=" + given + " " + stopped + " grid " + before + " -> " + props.GetGridCurNum() + "/" + props.GetGridMaxNum();
        }
    }
}
