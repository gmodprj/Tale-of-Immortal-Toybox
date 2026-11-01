using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. 2026-10-29 리뷰의 미확인 항목(06 문서 8.0절 3번)을 재는 탐침. 배포 코드는 건드리지 않는다.
    //   tswatch start|read|stop   매 프레임 Time.timeScale과 전투 장면 상태(battle·battleMap·playerUnitCtrl·map)를 보고, 바뀐 프레임만 적는다.
    //                             전투 장면을 싣는 동안(battle은 있고 playerUnitCtrl은 없음)에 지도 속도 배율이 걸려 있는지 본다(05 문서 86.4절).
    //   iwatch <i>|read|stop      선법 i의 data(cdTime·coolTime·cool·cast·isUse·energy·useCount)와 IsCreate(false)를 매 프레임 보고 바뀐 프레임만 적는다.
    //                             쿨타임 없음에서도 남는 약 2초가 어느 값의 몫인지 본다(87.4절).
    //   npcart                    전투의 플레이어 아닌 단위마다 법보(allProps의 PropsArtifact) sole·내구. 내구 접두의 비소유 쓰기 수(87.4절).
    internal static class DevCommandsProbe1029
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "tswatch":
                    return DevTimeScaleWatch.Command(p.Length > 1 ? p[1] : "read");
                case "iwatch":
                    return DevImmortalWatch.Command(p.Length > 1 ? p[1] : "read");
                case "npcart":
                    return NpcArtifacts();
                default:
                    return null;
            }
        }

        private static string NpcArtifacts()
        {
            if (!RateService.InBattle)
            {
                return "not in battle";
            }
            var sb = new StringBuilder();
            var all = SceneType.battle.unit.allUnitIncludeDie;
            int units = 0, arts = 0;
            for (int i = 0; i < all.Count; i++)
            {
                UnitCtrlBase u = all[i];
                if (u == null || u.data == null || u.data.unitType == UnitType.Player)
                {
                    continue;
                }
                units++;
                UnitDataHuman h = u.data.TryCast<UnitDataHuman>();
                if (h == null || h.worldUnitData == null || h.worldUnitData.unitData == null || h.worldUnitData.unitData.propData == null)
                {
                    sb.Append('[').Append(i).Append(' ').Append(u.data.unitType).Append(" no-worldUnitData] ");
                    continue;
                }
                var props = h.worldUnitData.unitData.propData.allProps;
                sb.Append('[').Append(i).Append(' ').Append(u.data.unitType).Append(" hp=").Append(u.data.hp).Append(' ');
                for (int j = 0; j < props.Count; j++)
                {
                    DataProps.PropsData pd = props[j];
                    if (pd == null || !SpriteService.IsArtifact(pd.propsID))
                    {
                        continue;
                    }
                    DataProps.PropsArtifact art = pd.To<DataProps.PropsArtifact>();
                    arts++;
                    sb.Append(pd.soleID).Append(':').Append(pd.propsID).Append(":dur=").Append(art == null ? -1 : art.durable).Append(' ');
                }
                sb.Append("] ");
            }
            sb.Append("units=").Append(units).Append(" artifacts=").Append(arts)
                .Append(" calls=").Append(RateGuards.DurableCalls).Append(" blocked=").Append(RateGuards.DurableBlockedCount)
                .Append(" otherDecrease=").Append(RateGuards.DevDurableOtherCalls).Append(" otherLast=").Append(RateGuards.DevDurableOtherLast ?? "-");
            return sb.ToString();
        }
    }

    // 프레임별 timeScale·전투 장면 상태. 상태가 바뀐 프레임만 적는다(최대 300줄).
    internal static class DevTimeScaleWatch
    {
        private static bool on;
        private static int frames;
        private static string last;
        private static readonly List<string> lines = new List<string>();
        private static int loadingScaledFrames;   // battle은 있는데 playerUnitCtrl이 없는 동안 timeScale != 1인 프레임
        private static int loadingFrames;

        public static string Command(string what)
        {
            if (what == "start")
            {
                on = true;
                frames = 0; last = null; lines.Clear(); loadingScaledFrames = 0; loadingFrames = 0;
                return "watching";
            }
            var sb = new StringBuilder();
            sb.Append("frames=").Append(frames).Append(" loadingFrames=").Append(loadingFrames).Append(" loadingScaledFrames=").Append(loadingScaledFrames).Append('\n');
            for (int i = 0; i < lines.Count; i++)
            {
                sb.Append(lines[i]).Append('\n');
            }
            if (what == "stop")
            {
                on = false;
            }
            return sb.ToString();
        }

        public static void Tick()
        {
            if (!on)
            {
                return;
            }
            try
            {
                frames++;
                bool battle = SceneType.battle != null;
                bool map = battle && SceneType.battle.battleMap != null;
                bool player = map && SceneType.battle.battleMap.playerUnitCtrl != null;
                bool scenemap = SceneType.map != null;
                float ts = Time.timeScale;
                if (battle && !player)
                {
                    loadingFrames++;
                    if (ts != 1f)
                    {
                        loadingScaledFrames++;
                    }
                }
                string now = "ts=" + ts.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                    + " battle=" + (battle ? 1 : 0) + " battleMap=" + (map ? 1 : 0) + " player=" + (player ? 1 : 0) + " map=" + (scenemap ? 1 : 0)
                    + " world=" + (g.world != null ? 1 : 0);
                if (now != last)
                {
                    last = now;
                    if (lines.Count < 300)
                    {
                        lines.Add("f" + frames + " t=" + Time.realtimeSinceStartup.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " " + now);
                    }
                }
            }
            catch (Exception e)
            {
                on = false;
                lines.Add("stopped: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }

    // 선법 하나의 값을 프레임마다 보고 바뀐 프레임만 적는다(최대 400줄).
    internal static class DevImmortalWatch
    {
        private static bool on;
        private static int index;
        private static int frames;
        private static string last;
        private static readonly List<string> lines = new List<string>();

        public static string Command(string what)
        {
            if (what == "read" || what == "stop")
            {
                var sb = new StringBuilder();
                sb.Append("index=").Append(index).Append(" frames=").Append(frames).Append(" on=").Append(on).Append('\n');
                for (int i = 0; i < lines.Count; i++)
                {
                    sb.Append(lines[i]).Append('\n');
                }
                if (what == "stop")
                {
                    on = false;
                }
                return sb.ToString();
            }
            index = int.Parse(what);
            on = true;
            frames = 0; last = null; lines.Clear();
            return "watching immortal " + index;
        }

        private static string F(float v)
        {
            return v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static void Tick()
        {
            if (!on)
            {
                return;
            }
            try
            {
                frames++;
                if (!RateService.InBattle)
                {
                    return;
                }
                UnitCtrlPlayer u = SceneType.battle.battleMap.playerUnitCtrl;
                if (u.immortalSkill == null || index >= u.immortalSkill.Count)
                {
                    return;
                }
                ImmortalSkillBase s = u.immortalSkill[index];
                ImmortalSkillDataBase d = s.data;
                string now = "cdTime=" + F(d.cdTime) + " coolTime=" + F(d.coolTime) + " cool=" + F(d.cool) + " cast=" + F(d.cast)
                    + " isUse=" + d.isUse + " energy=" + F(d.energy) + " useCount=" + d.useCount + " stopTime=" + d.stopTime.value
                    + " delay=" + d.delaySkillTime.value + " canCreate=" + s.IsCreate(false);
                if (now != last)
                {
                    last = now;
                    if (lines.Count < 400)
                    {
                        lines.Add("f" + frames + " t=" + Time.realtimeSinceStartup.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " " + now);
                    }
                }
            }
            catch (Exception e)
            {
                on = false;
                lines.Add("stopped: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
