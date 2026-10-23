using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 개발 전용. 묶음 2(docs/superpowers/plans/2026-10-23-mod-skill-list.md)의 모드 비급 살리기 실험.
    //   rmodtry <분류> <번호> <경지> <길>        길: now(지금 RollValuesForMod) | repeat(남는 칸에 모드 번호 되풀이) | own(모드 자신의 묶음) | trim(옵션 칸을 모드 번호만 남기고 줄임)
    //                                            비급을 가방에 넣고 학습 수용(can=)을 같은 경지의 바닐라 고성검(14112)과 견준다.
    //   rmodsweep <경지> <길> [반복=10]          이름 있는 모드 공법마다 길을 반복번 따로 시도. 첫 성공 하나만 가방에 넣어 can=을 재고 바로 뺀다.
    //                                            결과 .devremote\modsweep-<경지>-<길>.txt, 돌려주는 줄 ok(반복 모두)·partial·fail 수
    internal static class DevCommandsModBook
    {
        private const int VanillaProbe = 14112;   // 고성검(무예/영술). modbooks 검사와 같은 기준

        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "rmodtry": return Try(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), p[4]);
                case "rmodsweep": return Sweep(int.Parse(p[1]), p[2], p.Length > 3 ? int.Parse(p[3]) : 10);
                default: return null;
            }
        }

        // 길마다 비급 값. 못 만들면 null과 이유.
        internal static Il2CppStructArray<int> MakeValues(int kindIndex, SkillCatalog.Entry mod, int grade, string path, out string reason)
        {
            reason = null;
            if (path == "now")
            {
                return SkillCatalog.RollValuesForMod(kindIndex, mod, grade, out reason);
            }
            SkillCatalog.Kind kind = SkillCatalog.Kinds[kindIndex];
            if (path == "own")
            {
                int own = SkillCatalog.OwnCombine(kind, mod.BaseId);
                if (own == 0)
                {
                    reason = "자기 묶음 없음";
                    return null;
                }
                for (int tries = 0; tries < SkillCatalog.ModRollMax; tries++)
                {
                    Il2CppStructArray<int> rolled = RewardTool.GetSkillReward(own, grade, null);
                    if (rolled != null && Readable(kind, rolled, mod.BaseId))
                    {
                        return rolled;
                    }
                }
                reason = "자기 묶음에서 읽히는 값이 나오지 않음";
                return null;
            }
            SkillCatalog.Entry source = SkillCatalog.SourceFor(kindIndex, mod, grade);
            if (source == null)
            {
                reason = "원본 없음";
                return null;
            }
            for (int tries = 0; tries < SkillCatalog.ModRollMax; tries++)
            {
                Il2CppStructArray<int> rolled = SkillCatalog.RollValues(kind, source.BaseId, grade, 3000, null);
                if (rolled == null)
                {
                    continue;
                }
                var values = new Il2CppStructArray<int>(rolled.Length);
                for (int i = 0; i < rolled.Length; i++)
                {
                    values[i] = i == 0 ? mod.BaseId : rolled[i];
                }
                try
                {
                    SkillCatalog.RemapPrefixNumbers(kind, rolled, values, mod.BaseId);
                    Il2CppStructArray<int> final = path == "repeat" ? Repeat(kind, rolled, values, mod.BaseId) : path == "trim" ? Trim(kind, rolled, values, mod.BaseId) : values;
                    if (final != null && Readable(kind, final, mod.BaseId))
                    {
                        return final;
                    }
                }
                catch (Exception)
                {
                }
            }
            reason = "읽히는 값이 나오지 않음(" + SkillCatalog.ModRollMax + "회)";
            return null;
        }

        private static bool Readable(SkillCatalog.Kind kind, Il2CppStructArray<int> values, int baseId)
        {
            try
            {
                DataProps.MartialData martial = DataProps.PropsData.NewMartial(kind.Type, values).To<DataProps.MartialData>();
                return martial.baseID == baseId && !string.IsNullOrEmpty(martial.martialInfo.name);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int SkillTypeOf(SkillCatalog.Kind kind, Il2CppStructArray<int> rolled)
        {
            var prefixs = DataProps.PropsData.NewMartial(kind.Type, rolled).To<DataProps.MartialData>().GetPrefixs();
            for (int i = 0; i < prefixs.Count; i++)
            {
                if (prefixs[i].prefixValueItem != null)
                {
                    return prefixs[i].prefixValueItem.skillType;
                }
            }
            return -1;
        }

        private static int End(Il2CppStructArray<int> values)
        {
            for (int i = 2; i < values.Length; i++)
            {
                if (values[i] == -1)
                {
                    return i;
                }
            }
            return values.Length;
        }

        // 남는 자리(모드에 없는 번호)를 그 자리 가중치가 0보다 크고 중복 한도(multi) 안인 모드 번호로 채운다. 같은 번호 되풀이 허용.
        private static Il2CppStructArray<int> Repeat(SkillCatalog.Kind kind, Il2CppStructArray<int> rolled, Il2CppStructArray<int> values, int modId)
        {
            int skillType = SkillTypeOf(kind, rolled);
            var items = skillType < 0 ? null : g.conf.battleSkillPrefixValue.GetItems(skillType, modId);
            if (items == null || items.Count == 0)
            {
                return null;
            }
            var byNumber = new Dictionary<int, ConfBattleSkillPrefixValueItem>();
            for (int k = 0; k < items.Count; k++)
            {
                byNumber[items[k].number] = items[k];
            }
            int end = End(values);
            var used = new Dictionary<int, int>();
            for (int i = 2; i < end; i++)
            {
                if (byNumber.ContainsKey(values[i]))
                {
                    int c;
                    used.TryGetValue(values[i], out c);
                    used[values[i]] = c + 1;
                }
            }
            for (int i = 2; i < end; i++)
            {
                if (values[i] <= 0 || byNumber.ContainsKey(values[i]))
                {
                    continue;
                }
                int pick = -1;
                for (int pass = 0; pass < 2 && pick < 0; pass++)
                {
                    for (int k = 0; k < items.Count; k++)
                    {
                        int n = items[k].number;
                        int c;
                        used.TryGetValue(n, out c);
                        if (c < items[k].multi && (pass == 1 || MartialService.Weight(items[k], i - 2) > 0))
                        {
                            pick = n;
                            break;
                        }
                    }
                }
                if (pick < 0)
                {
                    return null;
                }
                values[i] = pick;
                int cc;
                used.TryGetValue(pick, out cc);
                used[pick] = cc + 1;
            }
            return values;
        }

        // 옵션 칸을 모드 번호인 자리만 남기고 줄인다. -1 뒤의 수준 칸도 같은 자리만 남긴다.
        private static Il2CppStructArray<int> Trim(SkillCatalog.Kind kind, Il2CppStructArray<int> rolled, Il2CppStructArray<int> values, int modId)
        {
            int skillType = SkillTypeOf(kind, rolled);
            var items = skillType < 0 ? null : g.conf.battleSkillPrefixValue.GetItems(skillType, modId);
            if (items == null || items.Count == 0)
            {
                return null;
            }
            var numbers = new HashSet<int>();
            for (int k = 0; k < items.Count; k++)
            {
                numbers.Add(items[k].number);
            }
            int end = End(values);
            int count = end - 2;
            var keep = new List<int>();
            for (int i = 2; i < end; i++)
            {
                if (numbers.Contains(values[i]))
                {
                    keep.Add(i - 2);
                }
            }
            var result = new List<int> { values[0], values[1] };
            foreach (int j in keep) { result.Add(values[2 + j]); }
            result.Add(-1);
            foreach (int j in keep) { int at = end + 1 + j; result.Add(at < values.Length ? values[at] : 1); }
            for (int i = end + 1 + count; i < values.Length; i++) { result.Add(values[i]); }
            var trimmed = new Il2CppStructArray<int>(result.Count);
            for (int i = 0; i < result.Count; i++) { trimmed[i] = result[i]; }
            return trimmed;
        }

        private static string Join(Il2CppStructArray<int> values)
        {
            var sb = new StringBuilder();
            for (int i = 0; values != null && i < values.Length; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(values[i]);
            }
            return sb.ToString();
        }

        // 가방에 넣고 학습 수용 값을 잰다. keep이 거짓이면 바로 뺀다.
        private static int Can(SkillCatalog.Kind kind, Il2CppStructArray<int> values, bool keep, out string sole)
        {
            WorldUnitBase me = g.world.playerUnit;
            DataProps bag = me.data.unitData.propData;
            DataProps.PropsData props = DataProps.PropsData.NewMartial(kind.Type, values);
            bag.AddProps(props);
            sole = props.soleID;
            int can = me.IsCreateAction(new UnitActionPropMartialStudy(props), false);
            if (!keep)
            {
                bag.DelProps(props.soleID);
            }
            return can;
        }

        private static readonly Dictionary<int, int> baseCan = new Dictionary<int, int>();

        private static int BaseCan(int grade)
        {
            int can;
            if (baseCan.TryGetValue(grade, out can))
            {
                return can;
            }
            Il2CppStructArray<int> values = SkillCatalog.RollValues(SkillCatalog.Kinds[0], VanillaProbe, grade, 3000, null);
            string sole;
            can = values == null ? int.MinValue : Can(SkillCatalog.Kinds[0], values, false, out sole);
            baseCan[grade] = can;
            return can;
        }

        private static string Try(int kindIndex, int baseId, int grade, string path)
        {
            SkillCatalog.Entry mod = SkillCatalog.Get(kindIndex, baseId);
            if (mod == null)
            {
                return "no skill " + baseId;
            }
            string reason;
            Il2CppStructArray<int> values = MakeValues(kindIndex, mod, grade, path, out reason);
            if (values == null)
            {
                return "path=" + path + " made=0 reason=" + reason;
            }
            string sole;
            int can = Can(SkillCatalog.Kinds[kindIndex], values, true, out sole);
            string name = DataProps.PropsData.NewMartial(SkillCatalog.Kinds[kindIndex].Type, values).To<DataProps.MartialData>().martialInfo.name;
            return "path=" + path + " made=1 name=" + name + " sole=" + sole + " can=" + can + " base=" + BaseCan(grade) + " values=" + Join(values);
        }

        private static string Sweep(int grade, string path, int repeat)
        {
            var file = new StringBuilder("kind\tid\tname\trows\tmultiSum\tok/rep\tcanMatch\treason\n");
            int ok = 0, partial = 0, fail = 0;
            int baseline = BaseCan(grade);
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                SkillCatalog.Kind kind = SkillCatalog.Kinds[k];
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    if (!e.IsMod || e.Nameless)
                    {
                        continue;
                    }
                    int rows = 0, multiSum = 0;
                    for (int t = 1; t <= 5; t++)
                    {
                        var items = g.conf.battleSkillPrefixValue.GetItems(t, e.BaseId);
                        if (items != null && items.Count > 0)
                        {
                            rows = items.Count;
                            multiSum = 0;
                            for (int i = 0; i < items.Count; i++) { multiSum += Math.Min(items[i].multi, 11); }
                            break;
                        }
                    }
                    int success = 0;
                    string canMatch = "-";
                    string lastReason = "";
                    for (int r = 0; r < repeat; r++)
                    {
                        string reason;
                        Il2CppStructArray<int> values = null;
                        try { values = MakeValues(k, e, grade, path, out reason); }
                        catch (Exception ex) { reason = "EX " + ex.GetType().Name; }
                        if (values == null)
                        {
                            lastReason = reason;
                            continue;
                        }
                        success++;
                        if (canMatch == "-")
                        {
                            string sole;
                            int can = Can(kind, values, false, out sole);
                            canMatch = can == baseline ? "same" : "can=" + can + "/base=" + baseline;
                        }
                    }
                    if (success == repeat && canMatch == "same") { ok++; } else if (success > 0) { partial++; } else { fail++; }
                    file.Append(kind.Tab).Append('\t').Append(e.BaseId).Append('\t').Append(e.Name).Append('\t').Append(rows).Append('\t').Append(multiSum).Append('\t')
                        .Append(success).Append('/').Append(repeat).Append('\t').Append(canMatch).Append('\t').Append(lastReason).Append('\n');
                }
            }
            File.WriteAllText(Path.Combine(DevCommands.Dir, "modsweep-" + grade + "-" + path + ".txt"), file.ToString(), new UTF8Encoding(false));
            return "grade=" + grade + " path=" + path + " repeat=" + repeat + " ok=" + ok + " partial=" + partial + " fail=" + fail + " base=" + baseline;
        }
    }
}
