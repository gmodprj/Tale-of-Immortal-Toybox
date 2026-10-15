using System;
using System.Collections.Generic;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 기술 서적(비급) 지급. 공법 탭의 기술 추가와 같은 게임 뽑기로 값을 얻어, 배우게 하는 대신 가방에 비급으로 넣는다.
    // 근거: docs/superpowers/specs/2026-10-15-martial-books-design.md 2·3.1절(만든 비급은 게임의 비급과 같은 모양이고 게임의 학습이 받아들인다).
    internal static class MartialBookService
    {
        public const int MaxPerGive = 20;
        public const int RollMax = 20000;

        // 가방의 기술별 비급 수(기술 번호 → 권수).
        public static Dictionary<int, int> CountAllInBag(WorldUnitBase unit)
        {
            var counts = new Dictionary<int, int>();
            var all = unit.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].propsType != DataProps.PropsDataType.Martial)
                {
                    continue;
                }
                try
                {
                    int baseId = all[i].To<DataProps.MartialData>().baseID;
                    int n;
                    counts.TryGetValue(baseId, out n);
                    counts[baseId] = n + 1;
                }
                catch (Exception)
                {
                    // 게임이 읽지 못하는 비급은 세지 않는다.
                }
            }
            return counts;
        }

        public static int CountInBag(WorldUnitBase unit, int baseId)
        {
            int n;
            return CountAllInBag(unit).TryGetValue(baseId, out n) ? n : 0;
        }

        // 아래 칸의 "지급하면 …" 또는 만들 수 없는 이유.
        public static string Describe(SkillCatalog.Entry entry, int grade, int minLevel, int count)
        {
            string blocked = SkillCatalog.BlockReason(entry, grade);
            if (blocked != null)
            {
                return "지급할 수 없습니다: " + blocked + ".";
            }
            int n = Math.Max(1, Math.Min(count, MaxPerGive));
            if (entry.IsMod)
            {
                return "지급하면: 경지 " + grade + "의 비급 " + n + "권(한 권마다 옵션을 따로 뽑습니다. 모드 공법의 품질은 게임이 정하므로 최소 품질을 적용하지 않습니다)"
                    + (count > MaxPerGive ? ". 한 번에 " + MaxPerGive + "권까지 줍니다" : "") + ".";
            }
            return "지급하면: 경지 " + grade + ", 품질 " + minLevel + " 이상의 비급 " + n + "권(한 권마다 옵션을 따로 뽑습니다)"
                + (count > MaxPerGive ? ". 한 번에 " + MaxPerGive + "권까지 줍니다" : "") + ".";
        }

        // 비급을 count권(최대 20) 만들어 가방에 넣고 넣은 권수를 돌려준다. 한 권마다 따로 뽑으므로 옵션이 서로 다르다.
        // 게임이 던지는 예외는 여기서 잡는다(버튼 콜백 밖으로 나가면 결과 문장 없이 사라진다).
        public static int Make(WorldUnitBase unit, int kindIndex, SkillCatalog.Entry entry, int grade, int minLevel, int count, out string text)
        {
            string label = entry.Name + (entry.IsMod ? " [모드]" : "") + " 비급";
            string blocked = SkillCatalog.BlockReason(entry, grade);
            if (blocked != null)
            {
                text = label + ": " + blocked + ".";
                return 0;
            }
            if (count < 1)
            {
                text = label + ": 권수는 1 이상이어야 합니다.";
                return 0;
            }
            if (minLevel < 1 || minLevel > 6)
            {
                text = label + ": 최소 품질은 1~6입니다.";
                return 0;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return 0;
            }
            int wanted = Math.Min(count, MaxPerGive);
            SkillCatalog.Kind kind = SkillCatalog.Kinds[kindIndex];
            DataProps bag = unit.data.unitData.propData;
            var made = new List<string>();
            string stop = null;
            for (int i = 0; i < wanted && stop == null; i++)
            {
                try
                {
                    Il2CppStructArray<int> values;
                    if (entry.IsMod)
                    {
                        string reason;
                        // 모드 공법의 품질은 뽑은 옵션과 무관하게 게임이 정한다(실측: 절기는 늘 3, 무예는 늘 5). 최소 품질을 걸면 끝내 나오지 않으므로 걸지 않는다.
                        values = SkillCatalog.RollValuesForMod(kindIndex, entry, grade, out reason);
                        if (values == null)
                        {
                            stop = reason;
                            break;
                        }
                    }
                    else
                    {
                        values = SkillCatalog.RollValues(kind, entry.BaseId, grade, RollMax, m => m.martialInfo.level >= minLevel);
                        if (values == null)
                        {
                            stop = "경지 " + grade + "에서 품질 " + minLevel + " 이상이 나오지 않았습니다";
                            break;
                        }
                    }
                    DataProps.PropsData props = DataProps.PropsData.NewMartial(kind.Type, values);
                    int before = bag.allProps.Count;
                    bag.AddProps(props);
                    bool ok = bag.allProps.Count == before + 1;
                    string name = entry.Name;
                    int level = 0;
                    try
                    {
                        DataProps.MartialData martial = props.To<DataProps.MartialData>();
                        name = MartialService.Name(martial);
                        level = martial.martialInfo.level;
                    }
                    catch (Exception)
                    {
                        // 이름과 품질을 읽지 못해도 넣은 것은 센다.
                    }
                    ChangeLog.Add(EditService.NameOf(unit), "비급 " + name + (entry.IsMod ? " [모드]" : "") + "(" + entry.BaseId + ")", "-", "경지 " + grade + " 품질 " + level, ok);
                    if (!ok)
                    {
                        stop = "가방에 들어가지 않았습니다(빈 칸을 확인하세요)";
                        break;
                    }
                    made.Add(name + "(품질 " + level + ")");
                }
                catch (Exception e)
                {
                    stop = "게임이 받지 않았습니다(" + e.GetType().Name + ")";
                }
            }
            text = label + " " + made.Count + "권" + (made.Count > 0 ? ": " + string.Join(", ", made.ToArray()) : "")
                + (count > MaxPerGive ? " (한 번에 " + MaxPerGive + "권까지 줍니다)" : "")
                + (entry.IsMod && minLevel > 1 && made.Count > 0 ? " (모드 공법의 품질은 게임이 정합니다. 최소 품질을 적용하지 않았습니다)" : "")
                + (stop != null ? ". 멈춤: " + stop : "") + ".";
            return made.Count;
        }
    }
}
