using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 수련 단계 대도까지 채우기(05 문서 83절, MasteryService). 검사 mastery-check.ps1이 쓴다. [unit]은 unitID(없거나 me면 플레이어).
    //   mfstate <sole> [unit]               opts=n stages=n last=<단계> block=<까닭|->
    //   mfroll <sole> [seed] [unit]         무작위 뽑기 결과 번호
    //   mfcand <sole> <n,…|-> [unit]        그 목록 다음 자리의 후보 번호와 자리 가중치(번호:가중치)
    //   mfinsert <sole> <n,…|roll> [unit]   끼워 넣기
    //   mfremake <sole> <n,…|roll> [unit]   다시 만들기. 끝에 new=<sole>
    //   mfvals <sole> [unit]                값 배열·장착·숙련·전승·옵션 수치(index:raw:lv/max)
    //   mffailnext                          다음 다시 만들기의 만들기를 실패로 흉내
    //   mffailequip                         다음 다시 만들기의 새 공법 장착을 실패로 흉내(만든 뒤 되돌리기)
    //   mfui <sole>|off                     게임 기술 정보 창(UIMartialInfo)을 플레이어의 그 공법으로
    internal static class DevCommandsMastery
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "mfstate":
                    {
                        var s = MasteryService.Read(Unit(p, 2), p[1]);
                        return "opts=" + s.Options + " stages=" + s.Stages + " last=" + s.LastStage + " block=" + (s.Block ?? "-");
                    }
                case "mfroll":
                    {
                        int seed = p.Length > 2 && p[2] != "-" ? int.Parse(p[2]) : Environment.TickCount;
                        return Join(MasteryService.Roll(Unit(p, 3), p[1], new Random(seed)));
                    }
                case "mfcand":
                    {
                        WorldUnitBase unit = Unit(p, 3);
                        var pending = Parse(p[2]);
                        var state = MasteryService.Read(unit, p[1]);
                        int slot = state.Options + pending.Count;
                        var sb = new StringBuilder("slot=" + slot + " ");
                        foreach (var item in MasteryService.Candidates(unit, p[1], pending))
                        {
                            sb.Append(item.number).Append(':').Append(MartialService.Weight(item, slot)).Append(' ');
                        }
                        return sb.ToString();
                    }
                case "mfinsert":
                    {
                        WorldUnitBase unit = Unit(p, 3);
                        return MasteryService.Insert(unit, p[1], Pick(unit, p[1], p[2]));
                    }
                case "mfremake":
                    {
                        WorldUnitBase unit = Unit(p, 3);
                        string made;
                        string text = MasteryService.Remake(unit, p[1], Pick(unit, p[1], p[2]), out made);
                        return text + " new=" + (made ?? "-");
                    }
                case "mfvals":
                    return Vals(Unit(p, 2), p[1]);
                case "mfmake":
                    {
                        // mfmake <분류 0~4> <기술 번호> <경지>: 옵션 없이 MartialService.Make로 만들어 배운다(모드 공법 이름 검사 준비)
                        var type = SkillCatalog.Kinds[int.Parse(p[1])].Type;
                        string made;
                        string error = MartialService.Make(g.world.playerUnit, type, int.Parse(p[2]), int.Parse(p[3]), new int[0], out made, true);
                        return error ?? "made " + made;
                    }
                case "mfname":
                    {
                        // 익힌 공법마다 sole:기본 번호:다듬은 이름|게임 원문(모드 이름의 태그 확인, 83.7절)
                        var sb = new StringBuilder();
                        foreach (var pair in g.world.playerUnit.data.unitData.allActionMartial)
                        {
                            var m = pair.Value.data.To<DataProps.MartialData>();
                            sb.Append(pair.Key).Append(':').Append(m.baseID).Append(':').Append(MartialService.Name(m)).Append('|').Append(m.martialInfo.name).Append('\n');
                        }
                        return sb.ToString();
                    }
                case "mffailnext":
                    MasteryService.FailNextMake = true;
                    return "fail next";
                case "mffailequip":
                    MasteryService.FailNextEquip = true;
                    return "fail next equip";
                case "mfui":
                    if (p[1] == "off")
                    {
                        g.ui.CloseUI(UIType.MartialInfo, true);
                        return "closed";
                    }
                    g.ui.OpenUI<UIMartialInfo>(UIType.MartialInfo).InitData(g.world.playerUnit, g.world.playerUnit.data.unitData.GetActionMartial(p[1]), new UnityEngine.Vector2(0, 0));
                    return "opened";
                default:
                    return null;
            }
        }

        private static WorldUnitBase Unit(string[] p, int index)
        {
            if (p.Length > index && p[index] != "me")
            {
                WorldUnitBase unit = g.world.unit.GetUnit(p[index], true);
                if (unit != null)
                {
                    return unit;
                }
            }
            return g.world.playerUnit;
        }

        private static List<int> Parse(string text)
        {
            return text == "-" ? new List<int>() : new List<int>(Array.ConvertAll(text.Split(','), int.Parse));
        }

        private static List<int> Pick(WorldUnitBase unit, string sole, string text)
        {
            return text == "roll" ? MasteryService.Roll(unit, sole, new Random(Environment.TickCount)) : Parse(text);
        }

        private static string Join(List<int> list)
        {
            return list.Count == 0 ? "none" : string.Join(",", list.ConvertAll(x => x.ToString()).ToArray());
        }

        private static string Vals(WorldUnitBase unit, string sole)
        {
            var action = unit.data.unitData.GetActionMartial(sole);
            if (action == null)
            {
                return "no martial " + sole;
            }
            var v = action.data.values;
            var sb = new StringBuilder("values=");
            for (int i = 0; v != null && i < v.Length; i++)
            {
                sb.Append(v[i]).Append(i + 1 < v.Length ? "," : "");
            }
            var m = action.data.To<DataProps.MartialData>();
            sb.Append(" equip=").Append(MartialService.IsEquipped(unit, sole) ? 1 : 0).Append(" exp=").Append(action.exp)
              .Append(" suit=").Append(MartialService.SuitOf(action.data)).Append(" opts=");
            foreach (MartialService.PrefixView view in MartialService.Prefixes(m))
            {
                sb.Append(view.Index).Append(':').Append(view.Raw).Append(':').Append(view.Level).Append('/').Append(view.MaxLevel).Append(' ');
            }
            return sb.ToString();
        }
    }
}
