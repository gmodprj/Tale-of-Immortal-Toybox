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
                default:
                    return null;
            }
        }

        private static WorldUnitBase Me()
        {
            return g.world == null ? null : g.world.playerUnit;
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
