using System;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. docs/04-ingame-state-analysis.md 15절 4단계(세계·전투)의 검증 명령.
    internal static class DevCommandsStage4
    {
        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world.playerUnit;
            switch (p[0])
            {
                case "where":
                    return Where(me);
                case "tp":
                    {
                        Vector2Int before = me.data.unitData.GetPoint();
                        var target = new Vector2Int(before.x + int.Parse(p[1]), before.y + int.Parse(p[2]));
                        int code = me.CreateAction(new UnitActionSetPoint(target), false);
                        return "UnitActionSetPoint returned " + code + "; " + Where(me);
                    }
                case "day":
                    {
                        int before = g.data.world.worldDay;
                        bool ok = g.world.run.AddDay(int.Parse(p[1]), false);
                        return "AddDay returned " + ok + "; worldDay " + before + " -> " + g.data.world.worldDay
                            + " roundDay=" + g.world.run.roundDay + " roundMonth=" + g.world.run.roundMonth;
                    }
                case "fight":
                    g.world.battle.IntoBattle(new DataMap.MonstData { id = int.Parse(p[1]), level = int.Parse(p[2]) }, null, null);
                    return "IntoBattle called";
                case "battle":
                    return BattleSpike.Execute(p);
                default:
                    return null;
            }
        }

        private static string Where(WorldUnitBase me)
        {
            Vector2Int point = me.data.unitData.GetPoint();
            return "point=" + point.x + "," + point.y + " saved=" + me.data.unitData.pointX + "," + me.data.unitData.pointY
                + " worldDay=" + g.data.world.worldDay;
        }
    }

    internal static class BattleSpike
    {
        private static Il2CppSystem.Action<ETypeData> handler;
        private static bool subscribed;
        public static bool God;
        public static int Multiplier = 1;
        public static int HitsOnPlayer;
        public static int HitsByPlayer;
        public static int Zeroed;
        public static int Multiplied;
        public static string Last = "";

        public static string Execute(string[] p)
        {
            switch (p[1])
            {
                case "on":
                    if (!subscribed)
                    {
                        handler = new Action<ETypeData>(OnHit);
                        g.events.On(EBattleType.UnitHitDynIntHandler, handler);
                        subscribed = true;
                    }
                    return "subscribed";
                case "off":
                    if (subscribed)
                    {
                        g.events.Off(EBattleType.UnitHitDynIntHandler, handler);
                        subscribed = false;
                    }
                    return "unsubscribed";
                case "god":
                    God = p[2] == "on";
                    return "god=" + God;
                case "mul":
                    Multiplier = int.Parse(p[2]);
                    return "multiplier=" + Multiplier;
                case "stat":
                    {
                        string hp = "n/a";
                        if (SceneType.battle != null && SceneType.battle.battleMap != null && SceneType.battle.battleMap.playerUnitCtrl != null)
                        {
                            var data = SceneType.battle.battleMap.playerUnitCtrl.data;
                            hp = data.hp + "/" + data.maxHP.value;
                        }
                        return "subscribed=" + subscribed + " god=" + God + " mul=" + Multiplier + " hitsOnPlayer=" + HitsOnPlayer
                            + " zeroed=" + Zeroed + " hitsByPlayer=" + HitsByPlayer + " multiplied=" + Multiplied
                            + " playerHp=" + hp + " last=" + Last;
                    }
                default:
                    return "UNKNOWN battle command";
            }
        }

        private static void OnHit(ETypeData e)
        {
            var data = e.Cast<EBattleTypeData.UnitHitDynIntHandler>();
            bool onPlayer = data.hitUnit != null && data.hitUnit.data.unitType == UnitType.Player;
            UnitCtrlBase attacker = data.hitData == null ? null : data.hitData.attackUnit;
            bool byPlayer = attacker != null && attacker.data.unitType == UnitType.Player;
            int before = data.dynV.value;
            if (onPlayer)
            {
                HitsOnPlayer++;
                if (God)
                {
                    data.dynV.baseValue = 0;
                    data.dynV.ClearCall();
                    Zeroed++;
                }
            }
            else if (byPlayer)
            {
                HitsByPlayer++;
                if (Multiplier > 1)
                {
                    data.dynV.baseValue = data.dynV.baseValue * Multiplier;
                    Multiplied++;
                }
            }
            Last = (onPlayer ? "onPlayer" : byPlayer ? "byPlayer" : "other") + " " + before + "->" + data.dynV.value;
        }
    }
}
