using UnhollowerBaseLib;
using System;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 캐릭터 내보내기·가져오기의 검증(tools/research/ui-check.ps1의 charvault)이 상태를 읽고 일부러 바꾸는 데 쓴다.
    internal static class DevCommandsChar
    {
        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            switch (p[0])
            {
                case "cstate": return State(me);
                case "cmartial": return Martials(me, int.Parse(p[1]));
                case "mbreak":
                    {
                        // 배운 공법 하나의 값을 복사해 기술 번호(머리 세 칸 뒤의 두 번째)를 없는 수로 바꾼 공법을 넣는다.
                        // 읽는 것만으로 예외가 나는 "깨진 공법"을 만들어 공법 탭의 보호(05 문서 48절)를 검사한다.
                        var data = me.data.unitData;
                        DataUnit.ActionMartialData source = null;
                        foreach (var pair in data.allActionMartial)
                        {
                            source = pair.Value;
                            break;
                        }
                        if (source == null)
                        {
                            return "no martial";
                        }
                        Il2CppStructArray<int> from = source.data.values;
                        var tail = new Il2CppStructArray<int>(from.Length - 3);
                        for (int i = 3; i < from.Length; i++)
                        {
                            tail[i - 3] = from[i];
                        }
                        tail[1] = 999999999;
                        DataProps.PropsData props = DataProps.PropsData.New(0, 1, DataProps.PropsDataType.Martial, tail);
                        data.StudyActionMartialProp(props);
                        return "sole=" + props.soleID + " martials=" + data.allActionMartial.Count;
                    }
                case "mdel":
                    me.data.unitData.DelActionMartial(p[1]);
                    return "DelActionMartial called; martials=" + me.data.unitData.allActionMartial.Count;
                case "cattr":
                    {
                        AttributeDef def = AttributeCatalog.All.Find(d => d.Key == p[1]);
                        return def == null ? "no attribute " + p[1] : EditService.SetAttribute(me, def, int.Parse(p[2]));
                    }
                case "cartdel":
                    {
                        int id = int.Parse(p[1]);
                        foreach (ArtService.Kind kind in ArtService.Kinds)
                        {
                            ItemCatalog.Entry book = ArtService.Books(kind, "").Find(b => b.Id == id);
                            if (book != null && ArtService.Learned(kind, id))
                            {
                                return ArtService.Forget(kind, book);
                            }
                        }
                        return "no learned art " + id;
                    }
                case "cborndel":
                    {
                        int id = int.Parse(p[1]);
                        var born = me.data.unitData.propertyData.bornLuck;
                        for (int i = 0; i < born.Length; i++)
                        {
                            if (born[i].id == id)
                            {
                                return LuckService.RemoveBorn(me, i);
                            }
                        }
                        return "no born luck " + id;
                    }
                case "cfatelog":
                    {
                        // 경지별 역천개명 기록의 항목마다: 대경지, 객체의 주소, 운명 번호, 품질. 두 경지가 같은 객체를 가리키는지 본다.
                        var log = g.data.world.playerLog.upGrade;
                        var sb = new StringBuilder("entries=" + log.Count);
                        foreach (var pair in log)
                        {
                            sb.Append(" [").Append(pair.Key).Append(" @").Append(pair.Value.Pointer.ToInt64().ToString("X"))
                                .Append(" luck=").Append(pair.Value.luck).Append(" q=").Append(pair.Value.quality).Append("]");
                        }
                        return sb.ToString();
                    }
                case "cluck3":
                    {
                        // 지금 대경지에서 뽑을 수 있는 역천개명 분류의 운명 가운데 가지지 않은 첫 번째를, 운명 탭의 "운명 추가"와 같은 길로 붙인다.
                        foreach (LuckService.LuckView view in FateService.Candidates(FateService.CurrentGrade(me), ""))
                        {
                            if (!LuckService.Has(me, view.Id) && FateService.HeldElsewhere(me, 0, view.Id) == 0)
                            {
                                return "id=" + view.Id + " has=" + LuckService.Has(me, view.Id) + " " + LuckService.Add(me, view.Id) + " has=" + LuckService.Has(me, view.Id);
                            }
                        }
                        return "no candidate";
                    }
                case "cfatealt":
                    {
                        // 그 경지의 역천개명을 지금 것과 다른 첫 후보로 바꾼다.
                        int grade = int.Parse(p[1]);
                        foreach (LuckService.LuckView view in FateService.Candidates(grade, ""))
                        {
                            if (!LuckService.Has(me, view.Id) && FateService.HeldElsewhere(me, grade, view.Id) == 0)
                            {
                                return "alt=" + view.Id + " " + FateService.Set(me, grade, view.Id);
                            }
                        }
                        return "no other candidate";
                    }
                default:
                    return null;
            }
        }

        // 검사가 비교하는 값들을 이름=값으로 한 줄에.
        private static string State(WorldUnitBase me)
        {
            var saved = me.data.unitData.propertyData;
            var world = g.data.world;
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(saved.gradeID);
            int arts = 0;
            foreach (ArtService.Kind kind in ArtService.Kinds)
            {
                arts += ArtService.LearnedCount(kind);
            }
            var fates = new StringBuilder();
            foreach (FateService.Slot slot in FateService.Slots(me))
            {
                fates.Append(fates.Length == 0 ? "" : ",").Append(slot.Grade).Append(':').Append(slot.LuckId);
            }
            return "grade=" + saved.gradeID + " exp=" + saved.exp + " nextexp=" + (next == null ? -1 : next.exp)
                + " attack=" + saved.attack + " defense=" + saved.defense + " attrs=" + AttributeCatalog.All.Count
                + " martials=" + me.data.unitData.allActionMartial.Count + " arts=" + arts
                + " born=" + saved.bornLuck.Length + " add=" + saved.addLuck.Count + " fates=" + (fates.Length == 0 ? "none" : fates.ToString());
        }

        // 그 기술 번호의 익힌 공법들: 수, 그리고 하나마다 고유 번호·숙련 경험치·값 배열(머리 세 칸 뒤).
        private static string Martials(WorldUnitBase me, int baseId)
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                try
                {
                    DataProps.PropsData props = pair.Value.data;
                    if (props.To<DataProps.MartialData>().baseID != baseId)
                    {
                        continue;
                    }
                    count++;
                    sb.Append(" sole=").Append(pair.Key).Append(" mexp=").Append(pair.Value.exp).Append(" values=[");
                    for (int i = 3; i < props.values.Length; i++)
                    {
                        sb.Append(i == 3 ? "" : ",").Append(props.values[i]);
                    }
                    sb.Append("]");
                }
                catch (Exception)
                {
                }
            }
            return "n=" + count + sb;
        }
    }
}
