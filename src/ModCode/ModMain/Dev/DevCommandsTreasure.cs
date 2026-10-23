using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 법보·기령 탭(05 문서 56절)의 서비스를 플레이어에게 명령으로 부른다. 검사 treasure-check.ps1이 쓴다.
    //   tr                          법보·기령 상태 한 줄: treasures=n [sole:번호:이름:품질:경지:내구/최대:장착:든 기령] spirits=m [sole:번호:이름:LvL/최대:경험치/다음:재능 켬/보임:든 법보]
    //   trgive <아이템 번호>         법보 하나 지급
    //   trequip <sole> / trunequip [sole]   장착 / 해제(sole이 없으면 장착 중인 것)
    //   trmake <기령 번호>           기령 만들기
    //   trput <법보 sole> <기령 sole> / trpull <법보 sole>   넣기 / 빼기
    //   trlight <기령 sole> / trlevel <기령 sole> / trdel <기령 sole>   재능 모두 켜기 / 레벨 올리기 / 지우기
    //   trdur <법보 sole> <값>        내구를 그 값으로(수리 검사 준비. 05 문서 60절)
    //   trrepair <법보 sole>|all      수리 / 모두 수리
    //   (트레이너 3차) trnpc <unitID|any|me>   다음 tr* 명령의 대상(기본 플레이어). trart <sole> 경지·단계·비율·옵션,
    //   trgrade <sole> [목표 경지] 경지 올리기, trstage <sole> <1~6> 단계, trsp 기령 친밀도, trclose <기령 sole> 친밀도 최대
    internal static class DevCommandsTreasure
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "tr":
                    return State(Me());
                case "trgive":
                    return TreasureService.Give(Me(), int.Parse(p[1]));
                case "trequip":
                    return TreasureService.Equip(Me(), p[1]);
                case "trunequip":
                    return TreasureService.Unequip(Me(), p.Length > 1 ? p[1] : EquippedSole(Me()));
                case "trmake":
                    return TreasureService.Create(Me(), int.Parse(p[1]));
                case "trput":
                    return TreasureService.PutSpirit(Me(), p[1], int.Parse(p[2]));
                case "trpull":
                    return TreasureService.PullSpirit(Me(), p[1]);
                case "trlight":
                    return TreasureService.LightTalents(Me(), int.Parse(p[1]));
                case "trlevel":
                    return TreasureService.LevelUp(Me(), int.Parse(p[1]));
                case "trdel":
                    return TreasureService.Delete(Me(), int.Parse(p[1]));
                case "trdur":
                    return SetDurable(Me(), p[1], int.Parse(p[2]));
                case "trnpc":
                    return SetTarget(p[1]);
                case "trart":
                    return Art(Me(), p[1]);
                case "trgrade":
                    return p.Length > 2 ? TreasureService.GradeUpTo(Me(), p[1], int.Parse(p[2])) : TreasureService.GradeUp(Me(), p[1]);
                case "trstage":
                    return TreasureService.SetStage(Me(), p[1], int.Parse(p[2]));
                case "trsp":
                    return Spirits(Me());
                case "trclose":
                    return TreasureService.CloseMax(Me(), int.Parse(p[1]));
                case "trrepair":
                    return p[1] == "all" ? TreasureService.RepairAll(Me()) : TreasureService.Repair(Me(), p[1]);
                default:
                    return null;
            }
        }

        private static string targetId;   // null이면 플레이어

        private static WorldUnitBase Me()
        {
            if (g.world == null)
            {
                return null;
            }
            if (targetId != null)
            {
                WorldUnitBase unit = g.world.unit.GetUnit(targetId, true);
                if (unit != null)
                {
                    return unit;
                }
            }
            return g.world.playerUnit;
        }

        private static string SetTarget(string who)
        {
            if (who == "me")
            {
                targetId = null;
                return "target=player";
            }
            if (who == "any")
            {
                string me = g.world.playerUnit.data.unitData.unitID;
                var units = g.world.unit.GetUnits(false);
                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i] != null && units[i].data.unitData.unitID != me && TreasureService.Ready(units[i]))
                    {
                        targetId = units[i].data.unitData.unitID;
                        return "target=" + targetId + " " + EditService.NameOf(units[i]);
                    }
                }
                return "no npc";
            }
            targetId = who;
            return "target=" + who;
        }

        private static string Art(WorldUnitBase unit, string soleId)
        {
            TreasureService.Treasure t = TreasureService.Find(unit, soleId);
            if (t == null)
            {
                return "no treasure " + soleId;
            }
            return t.SoleId + ":" + t.ItemId + " grade=" + t.Grade + " level=" + t.Level + " rate=" + TreasureService.RatesText(t.Rates) + " pre=" + t.Prefixes + "/" + t.PrefixMax;
        }

        private static string Spirits(WorldUnitBase unit)
        {
            var sb = new StringBuilder();
            foreach (TreasureService.Spirit s in TreasureService.Spirits(unit))
            {
                sb.Append(s.SoleId).Append(':').Append(s.SpriteId).Append(" intim=").Append(s.Intimacy).Append('/').Append(s.IntimacyMax).Append(' ');
            }
            return sb.Length == 0 ? "no spirits" : sb.ToString();
        }

        private static string SetDurable(WorldUnitBase unit, string soleId, int value)
        {
            foreach (TreasureService.Treasure t in TreasureService.Treasures(unit))
            {
                if (t.SoleId == soleId && t.Data != null)
                {
                    t.Data.durable = value;
                    return "durable " + t.Name + " -> " + t.Data.durable + "/" + t.DurableMax;
                }
            }
            return "no treasure " + soleId;
        }

        private static string EquippedSole(WorldUnitBase unit)
        {
            foreach (TreasureService.Treasure t in TreasureService.Treasures(unit))
            {
                if (t.Equipped)
                {
                    return t.SoleId;
                }
            }
            return "";
        }

        private static string State(WorldUnitBase unit)
        {
            if (!TreasureService.Ready(unit))
            {
                return "not ready";
            }
            var sb = new StringBuilder();
            var treasures = TreasureService.Treasures(unit);
            sb.Append("treasures=").Append(treasures.Count);
            foreach (TreasureService.Treasure t in treasures)
            {
                sb.Append(" [").Append(t.SoleId).Append(':').Append(t.ItemId).Append(':').Append(t.Name).Append(":q").Append(t.Quality).Append(":g").Append(t.Grade)
                    .Append(':').Append(t.Durable).Append('/').Append(t.DurableMax).Append(':').Append(t.Equipped ? "eq" : "-").Append(':').Append(t.SpiritSole).Append(']');
            }
            var spirits = TreasureService.Spirits(unit);
            sb.Append(" spirits=").Append(spirits.Count);
            foreach (TreasureService.Spirit s in spirits)
            {
                sb.Append(" [").Append(s.SoleId).Append(':').Append(s.SpriteId).Append(':').Append(s.Name).Append(":Lv").Append(s.Level).Append('/').Append(s.MaxLevel)
                    .Append(':').Append(s.Exp).Append('/').Append(s.NextExp).Append(':').Append(s.Lit).Append('/').Append(s.Shown).Append(':').Append(s.InSole ?? "-").Append(']');
            }
            return sb.ToString();
        }
    }
}
