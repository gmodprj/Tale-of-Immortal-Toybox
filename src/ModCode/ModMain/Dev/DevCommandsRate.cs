using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 배율 탭의 검증 명령. tools/research/ui-check.ps1의 rates, rates-battle이 이 출력 모양에 의존한다.
    //   rstate   배율의 값과 패치 여부, 마지막으로 바꾼 값, 전투 보조, Time.timeScale.
    //   rnpc     플레이어가 아닌 단위 하나의 AddExp(10)을 부른다(배율이 걸리지 않아야 한다).
    //   rneg     플레이어의 AddExp(-7)을 부른다(곱해지지 않아야 한다).
    //   bcast <종류>   전투 중인 플레이어가 기술을 쓰게 한다. 1 무예/영술, 2 절기, 3 신법.
    //   rmart          장착한 무예·절기·신법·신통의 숙련 경험치.
    //   bdmg           전투 보조의 구독 여부와 피해 배율이 걸린 횟수.
    //   cdstat         전투 중인 플레이어의 쿨타임 속도와 기술별 쿨타임.
    //   rpath <dx> <dy> 게임의 경로 이동으로 그만큼 떨어진 칸까지 간다.
    //   bend [lose]    전투를 승리(또는 패배)로 끝낸다.
    //   rpause on|off  Time.timeScale을 0으로 쓰고(on), 그 전의 값으로 되돌린다(off).
    internal static class DevCommandsRate
    {
        private static float pausedFrom = 1f;

        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "rstate":
                    return State();
                case "rnpc":
                    return Npc();
                case "rneg":
                    {
                        var saved = g.world.playerUnit.data.unitData.propertyData;
                        int before = saved.exp;
                        saved.AddExp(-7);
                        return "player AddExp(-7) exp " + before + " -> " + saved.exp;
                    }
                case "bcast":
                    return Cast(int.Parse(p[1]));
                case "rmart":
                    return Martials();
                case "rpause":
                    // 게임의 일시 정지를 흉내 낸다(0이면 편집기가 건드리지 않아야 한다).
                    // 게임의 정지 해제가 정지 전의 값을 되살리는 경우를 본다: on은 지금 값을 떠 두고, off는 그 값으로 되돌린다.
                    if (p[1] == "on")
                    {
                        pausedFrom = UnityEngine.Time.timeScale;
                        UnityEngine.Time.timeScale = 0f;
                    }
                    else
                    {
                        UnityEngine.Time.timeScale = pausedFrom;
                    }
                    return "timeScale=" + UnityEngine.Time.timeScale;
                case "bdmg":
                    return BattleService.DevState;
                case "rpath":
                    {
                        // 게임의 경로 이동(지도를 눌렀을 때와 같은 절차)으로 여러 칸을 간다.
                        var here = g.world.playerUnit.data.unitData.GetPoint();
                        SceneType.map.world.MovePlayerPosi(new UnityEngine.Vector2Int(here.x + int.Parse(p[1]), here.y + int.Parse(p[2])));
                        return "MovePlayerPosi called from " + here.x + "," + here.y;
                    }
                case "cdstat":
                    return CdStat();
                case "bend":
                    // 전투를 승리로 끝낸다(게임의 종료 절차를 부른다). 결과 창은 따로 닫는다.
                    if (!RateService.InBattle)
                    {
                        return "not in battle";
                    }
                    SceneType.battle.battleEnd.BattleEnd(p.Length < 2 || p[1] != "lose");
                    return "BattleEnd called";
                default:
                    return null;
            }
        }

        private static string State()
        {
            var sb = new StringBuilder();
            foreach (RateService.Rate rate in RateService.All)
            {
                sb.Append(rate.Key).Append('=').Append(rate.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)).Append(rate.Patched ? "(patched)" : "").Append(' ');
            }
            sb.Append("god=").Append(BattleService.God).Append(" damage=").Append(BattleService.Multiplier)
                .Append(" lastExp=").Append(RateService.Exp.Last)
                .Append(" lastMartial=").Append(RateService.Martial.Last)
                .Append(" lastMove=").Append(RateService.Move.Last)
                .Append(" timeScale=").Append(UnityEngine.Time.timeScale.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .Append(" inBattle=").Append(RateService.InBattle);
            return sb.ToString();
        }

        private static string Cast(int type)
        {
            if (!RateService.InBattle)
            {
                return "not in battle";
            }
            UnitCtrlPlayer me = SceneType.battle.battleMap.playerUnitCtrl;
            if (type == 3)
            {
                me.CreateStep();
                return "CreateStep called";
            }
            return "CreateSkillAttack=" + me.CreateSkillAttack((MartialType)type);
        }

        private static string Martials()
        {
            var info = g.world.playerUnit.data.unitData;
            var sb = new StringBuilder();
            string[] names = { "left", "right", "step", "ultimate" };
            string[] ids = { info.skillLeft, info.skillRight, info.step, info.ultimate };
            for (int i = 0; i < ids.Length; i++)
            {
                DataUnit.ActionMartialData data = string.IsNullOrEmpty(ids[i]) ? null : info.GetActionMartial(ids[i]);
                sb.Append(names[i]).Append('=').Append(data == null ? -1 : data.exp).Append(' ');
            }
            return sb.ToString();
        }

        // 실험용: 전투 중인 플레이어의 쿨타임 비율과 기술별 쿨타임 상태.
        private static string CdStat()
        {
            if (!RateService.InBattle)
            {
                return "not in battle";
            }
            UnitCtrlPlayer ctrl = SceneType.battle.battleMap.playerUnitCtrl;
            var data = ctrl.data;
            var sb = new StringBuilder();
            sb.Append("time=").Append(UnityEngine.Time.time.ToString("0.0")).Append(" rates skill=").Append(data.skillCDRate.baseValue).Append('/').Append(data.skillCDRate.value)
                .Append(" step=").Append(data.stepCDRate.baseValue).Append('/').Append(data.stepCDRate.value)
                .Append(" ult=").Append(data.ultimateCDRate.baseValue).Append('/').Append(data.ultimateCDRate.value);
            for (int i = 0; i < ctrl.skills.Count; i++)
            {
                SkillDataAttack d = ctrl.skills[i].data;
                sb.Append(" | skill").Append(i).Append(" cd=").Append(d.cd.baseValue).Append('/').Append(d.cd.value).Append(" cdTime=").Append(d.cdTime.ToString("0.00"))
                    .Append(" use=").Append(d.cdUseTime.ToString("0.00")).Append(" last=").Append(d.lastShotTime.ToString("0.0")).Append(" reduce=").Append(d.reduceCDTime.ToString("0.0"));
            }
            if (ctrl.step != null)
            {
                StepDataBase d = ctrl.step.data;
                sb.Append(" | step cd=").Append(d.cd.baseValue).Append('/').Append(d.cd.value).Append(" cdTime=").Append(d.cdTime.ToString("0.00"))
                    .Append(" use=").Append(d.cdUseTime.ToString("0.00")).Append(" last=").Append(d.lastShotTime.ToString("0.0"));
            }
            if (ctrl.ultimate != null)
            {
                SkillDataAttack d = ctrl.ultimate.data;
                sb.Append(" | ult cd=").Append(d.cd.baseValue).Append('/').Append(d.cd.value).Append(" cdTime=").Append(d.cdTime.ToString("0.00"))
                    .Append(" use=").Append(d.cdUseTime.ToString("0.00")).Append(" last=").Append(d.lastShotTime.ToString("0.0"));
            }
            return sb.ToString();
        }

        private static string Npc()
        {
            string myId = g.world.playerUnit.data.unitData.unitID;
            var units = g.world.unit.GetUnits(false);
            string last = "no npc";
            for (int i = 0; i < units.Count; i++)
            {
                var info = units[i].data.unitData;
                if (info.unitID == myId)
                {
                    continue;
                }
                int before = info.propertyData.exp;
                info.propertyData.AddExp(10);
                last = "npc exp " + before + " -> " + info.propertyData.exp;
                // 수련치가 상한인 단위는 10이 다 오르지 않는다. 다 오른 단위를 찾을 때까지 넘어간다.
                if (info.propertyData.exp - before == 10)
                {
                    return last;
                }
            }
            return last;
        }
    }
}
