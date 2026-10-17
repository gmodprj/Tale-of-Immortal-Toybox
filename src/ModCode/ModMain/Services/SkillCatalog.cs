using System;
using System.Collections.Generic;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 배울 수 있는 기술의 목록과 추가. 분류는 게임 인물 화면의 기술 탭을 따른다(docs/05 22·23절).
    internal static class SkillCatalog
    {
        public class Kind
        {
            public string Tab;          // 게임 기술 화면의 탭 이름
            public MartialType Type;
            public int Combine;         // 이 분류 전체에서 뽑는 묶음 번호
        }

        public class Entry
        {
            public int BaseId;
            public string Name;
            public string Desc;
            public int GradeMin;
            public int GradeMax;
            public string Search;
            public int KindIndex;        // Kinds의 순번
            public bool IsMod;           // 모드가 더한 행(ConfBaseItem.isModExtend). 경지 범위는 1~10으로 둔다
            public bool NameFromCh;      // 한국어 이름이 없어 LocalText의 ch(중국어)로 채웠다
            public string BasType;       // 무예·영술의 무기 종류(battleSkillAttack.basType). 그 밖은 ""
            public bool Nameless;        // 모드 행인데 이름이 비거나 "0"이거나 키 그대로라 "이름 없음(번호)"로 둔 것. 분류 목록의 끝에 온다(05 문서 48절)
        }

        public static readonly Kind[] Kinds =
        {
            new Kind { Tab = "무예/영술", Type = MartialType.SkillLeft, Combine = 101 },
            new Kind { Tab = "절기", Type = MartialType.SkillRight, Combine = 102 },
            new Kind { Tab = "신법", Type = MartialType.Step, Combine = 101 },
            new Kind { Tab = "심법", Type = MartialType.Ability, Combine = 101 },
            new Kind { Tab = "신통", Type = MartialType.Ultimate, Combine = 103 },
        };

        private const int RollMax = 3000;
        private static readonly Dictionary<int, List<Entry>> cache = new Dictionary<int, List<Entry>>();

        private static Dictionary<string, string> chByKey;   // LocalText 키 → ch(중국어). 처음 한 번 만든다

        // 한국어 글이 없으면("0"·빈 글·키 그대로) LocalText 행의 ch(중국어)를, 그것도 없으면 키를 돌려준다.
        // 모드의 LocalText는 ch만 있고 kr이 "0"이다(실측, 인계 8.3e절). G2c가 kr을 덮으면 LS가 한글을 돌려주므로 그대로 따라온다.
        internal static string ResolveText(string key, out bool fromCh)
        {
            fromCh = false;
            string text = GameTool.LS(key);
            if (!string.IsNullOrEmpty(text) && text != "0" && text != key)
            {
                return text;
            }
            string ch = ChOf(key);
            if (!string.IsNullOrEmpty(ch))
            {
                fromCh = true;
                return ch;
            }
            return key;
        }

        // LocalText 행의 ch. 없으면 "". (allText.TryGetValue는 Il2Cpp 제네릭 호출에서 MissingMethodException이 났다. 목록을 한 번 훑어 사전을 만든다.)
        internal static string ChOf(string key)
        {
            if (chByKey == null)
            {
                chByKey = new Dictionary<string, string>();
                var all = g.conf.localText.allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    ConfLocalTextItem item = all[i];
                    if (item != null && item.key != null && !chByKey.ContainsKey(item.key))
                    {
                        chByKey[item.key] = item.ch ?? "";
                    }
                }
            }
            string found;
            return key != null && chByKey.TryGetValue(key, out found) ? found : "";
        }

        // 목록과 LocalText 사전을 비운다. LocalText를 덮어쓴 뒤(G2c) 부른다.
        public static void Reset()
        {
            cache.Clear();
            chByKey = null;
        }

        // 모드 항목의 비급을 뽑을 원본: 같은 분류의 기본 항목 가운데 고른 경지를 범위에 포함하는 것.
        // 무예·영술은 같은 basType을 먼저, 자기 묶음(OwnCombine)이 있는 것을 먼저 쓴다(뽑기가 한 번에 끝난다). 없으면 null.
        internal static Entry SourceFor(int kindIndex, Entry mod, int grade)
        {
            Kind kind = Kinds[kindIndex];
            Entry best = null;
            int bestScore = -1;
            foreach (Entry e in All(kindIndex))
            {
                if (e.IsMod || grade < e.GradeMin || grade > e.GradeMax)
                {
                    continue;
                }
                int score = (OwnCombine(kind, e.BaseId) != 0 ? 1 : 0) + (kind.Type == MartialType.SkillLeft && e.BasType == mod.BasType ? 2 : 0);
                if (score > bestScore)
                {
                    best = e;
                    bestScore = score;
                }
            }
            return best;
        }

        // 기본 행: 경지 범위가 0~0인 항목은 플레이어가 배우는 기술이 아니다(괴물용·부품). 이름 키가 풀리지 않는 것도 뺀다.
        // 모드 행(isModExtend): 경지 범위가 0~0이고 한국어 이름이 "0"이어도 넣는다(실측, 인계 8.3e절). 경지는 1~10.
        // 무예·영술·절기·신통의 className이 "0"인 기본 행은 분류 전체의 묶음(className 1)에서 뽑히지 않는다. 그 가운데 자기 묶음(입문 무예·영술)도 없는
        // 셋(멸신참 321111, 마인참 321112, 극풍인 641121)은 NPC 전용이라 옵션 없이 만들어도 게임이 받지 않는다(회차 fu-1017a. 사용자 보고 2026-10-17, 05 문서 55절). 목록에서 뺀다.
        private static void Add(List<Entry> list, int kindIndex, bool isMod, int id, string nameKey, string descKey, int gradeMin, int gradeMax, string basType, string className)
        {
            bool nameFromCh, descFromCh;
            string name = EditService.Compact(ResolveText(nameKey, out nameFromCh));
            if (!isMod && className == "0" && OwnCombine(Kinds[kindIndex], id) == 0)
            {
                return;
            }
            // 모드가 이름 키를 LocalText에 넣지 않으면 이름이 비거나 "0"이다(46절). 번호로 찾을 수 있게 이름을 두고 목록 끝으로 보낸다.
            bool nameless = isMod && (name.Length == 0 || name == "0" || name == EditService.Compact(nameKey));
            if (nameless)
            {
                name = "이름 없음(" + id + ")";
            }
            // 기본 행은 한국어 이름이 풀린 것만(전과 같다. ch로 대신 채운 것도 뺀다).
            if (!isMod && (gradeMax < 1 || name.Length == 0 || nameFromCh || name == EditService.Compact(nameKey)))
            {
                return;
            }
            list.Add(new Entry
            {
                BaseId = id,
                Name = name,
                Desc = MartialService.CleanDesc(ResolveText(descKey, out descFromCh)).Replace("\r", "").Replace('\n', ' '),
                GradeMin = isMod ? 1 : gradeMin,
                GradeMax = isMod ? 10 : gradeMax,
                Search = name + EditService.Compact(ChOf(nameKey)),
                KindIndex = kindIndex,
                IsMod = isMod,
                NameFromCh = nameFromCh,
                Nameless = nameless,
                BasType = basType ?? "",
            });
        }

        private static List<Entry> All(int kindIndex)
        {
            List<Entry> list;
            if (cache.TryGetValue(kindIndex, out list))
            {
                return list;
            }
            list = new List<Entry>();
            Kind kind = Kinds[kindIndex];
            if (kind.Type == MartialType.Step)
            {
                var all = g.conf.battleStepBase._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    Add(list, kindIndex, all[i].isModExtend, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, "", "");
                }
            }
            else if (kind.Type == MartialType.Ability)
            {
                var all = g.conf.battleAbilityBase._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    Add(list, kindIndex, all[i].isModExtend, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, "", "");
                }
            }
            else
            {
                // 설정의 type: 1·2는 무예·영술, 3은 절기, 4는 신통.
                var all = g.conf.battleSkillAttack._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    int type = all[i].type;
                    bool match = kind.Type == MartialType.SkillLeft ? (type == 1 || type == 2)
                        : kind.Type == MartialType.SkillRight ? type == 3 : type == 4;
                    if (match)
                    {
                        Add(list, kindIndex, all[i].isModExtend, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, all[i].basType, all[i].className);
                    }
                }
            }
            // 이름 없는 행을 끝으로(그 밖의 순서는 표 순서 그대로).
            var ordered = new List<Entry>(list.Count);
            foreach (Entry entry in list) { if (!entry.Nameless) { ordered.Add(entry); } }
            foreach (Entry entry in list) { if (entry.Nameless) { ordered.Add(entry); } }
            list = ordered;
            cache[kindIndex] = list;
            return list;
        }

        public static List<Entry> Find(int kindIndex, string query)
        {
            string needle = EditService.Compact(query ?? "");
            int id;
            bool isId = int.TryParse(needle, out id);
            var result = new List<Entry>();
            foreach (Entry entry in All(kindIndex))
            {
                if (needle.Length == 0 || entry.Search.Contains(needle) || (isId && entry.BaseId == id))
                {
                    result.Add(entry);
                }
            }
            return result;
        }

        // 대상이 이 기술을 몇 개 익혔는지(같은 기술을 옵션만 달리해 여러 개 가질 수 있다).
        public static Dictionary<int, int> LearnedCounts(WorldUnitBase unit, int kindIndex)
        {
            var counts = new Dictionary<int, int>();
            MartialType type = Kinds[kindIndex].Type;
            foreach (var pair in unit.data.unitData.allActionMartial)
            {
                DataProps.MartialData martial = pair.Value.data.To<DataProps.MartialData>();
                if (martial == null || martial.martialType != type)
                {
                    continue;
                }
                int n;
                counts.TryGetValue(martial.baseID, out n);
                counts[martial.baseID] = n + 1;
            }
            return counts;
        }

        // 지금 배울(만들) 수 없는 이유. 할 수 있으면 null.
        public static string BlockReason(Entry entry, int grade)
        {
            if (entry.IsMod)
            {
                if (grade < 1 || grade > 10)
                {
                    return "경지는 1~10입니다";
                }
                // 신통도 준다(옵션 번호를 모드 것으로 바꾸면 게임이 읽는다. 05 문서 44절). 그 경지에 원본으로 쓸 기본 공법이 없으면 미리 알린다(줄의 버튼과 결과가 어긋나지 않게).
                return SourceFor(entry.KindIndex, entry, grade) == null ? "경지 " + grade + "에 원본으로 쓸 기본 공법이 없습니다" : null;
            }
            return grade < entry.GradeMin || grade > entry.GradeMax
                ? "경지 " + entry.GradeMin + "~" + entry.GradeMax + "의 기술이라 경지 " + grade + "로는 만들 수 없습니다"
                : null;
        }

        private static bool Contains(Il2CppStructArray<int> values, int id)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == id)
                {
                    return true;
                }
            }
            return false;
        }

        private static Il2CppStructArray<int> Roll(Kind kind, int grade)
        {
            switch (kind.Type)
            {
                case MartialType.Step: return RewardTool.GetStepReward(kind.Combine, grade, null);
                case MartialType.Ability: return RewardTool.GetAbilityReward(kind.Combine, grade, null);
                default: return RewardTool.GetSkillReward(kind.Combine, grade, null);
            }
        }

        private static Dictionary<int, int> ownCombines;

        // 무예·영술·절기·신통 가운데 "이 기술 하나만" 나오는 묶음의 번호. 없으면 0.
        internal static int OwnCombine(Kind kind, int baseId)
        {
            if (kind.Type == MartialType.Step || kind.Type == MartialType.Ability)
            {
                return 0;
            }
            if (ownCombines == null)
            {
                ownCombines = new Dictionary<int, int>();
                var all = g.conf.battleSkillCombine._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    int id;
                    if (int.TryParse(all[i].baseID, out id) && id > 0 && !ownCombines.ContainsKey(id))
                    {
                        ownCombines[id] = all[i].id;
                    }
                }
            }
            int combine;
            return ownCombines.TryGetValue(baseId, out combine) ? combine : 0;
        }

        // 그 기술이 나오고 accept를 통과하는 뽑기 값. 그 기술 하나만 나오는 묶음이 있으면(입문 무예·영술 등) 먼저 쓰고,
        // 그다음 분류 전체의 묶음에서 maxTries번까지 다시 뽑는다. 없으면 null. 기술 추가(accept null)와 비급 지급이 함께 쓴다.
        // 그 기술만의 묶음은 한 번만 뽑는다. 분류 전체의 묶음에서 나오지 않는 입문 무예·영술(경지 1 전용, 05 문서 24절)은
        // 그 묶음에서 품질 1로만 나온다(2만 번 다시 뽑아도 품질 2 이상 0번, 회차 book-1015l). 다시 뽑아도 소용없어 한 번으로 둔다.
        internal static Il2CppStructArray<int> RollValues(Kind kind, int baseId, int grade, int maxTries, Func<DataProps.MartialData, bool> accept)
        {
            int own = OwnCombine(kind, baseId);
            if (own != 0)
            {
                Il2CppStructArray<int> fixedRoll = RewardTool.GetSkillReward(own, grade, null);
                if (fixedRoll != null && Contains(fixedRoll, baseId) && (accept == null || Accepts(kind, fixedRoll, baseId, accept)))
                {
                    return fixedRoll;
                }
            }
            for (int tries = 0; tries < maxTries; tries++)
            {
                Il2CppStructArray<int> rolled = Roll(kind, grade);
                if (rolled != null && Contains(rolled, baseId) && Accepts(kind, rolled, baseId, accept))
                {
                    return rolled;
                }
            }
            return null;
        }

        public const int ModRollMax = 50;

        private static readonly Random rng = new Random();

        // 비급 값의 2번부터 첫 -1 앞까지는 옵션 번호(number)다. 게임은 옵션을 (skillType, 기술 번호, number)로 BattleSkillPrefixValue 행에서 찾으므로
        // (실측, 05 문서 44절), 원본에서 뽑은 번호 가운데 모드 공법에 없는 것을 모드 공법에 있는(아직 안 쓴) 번호로 바꾼다. 있는 번호는 그대로 둔다
        // (모드 공법 자신의 행을 가리킨다). 대체 번호는 그 자리의 가중치가 0보다 큰 안 쓴 번호 가운데 무작위로 고른다. 바꿀 번호가 없으면 그대로 두고 RollValuesForMod의 읽기 시험에 맡긴다.
        internal static void RemapPrefixNumbers(Kind kind, Il2CppStructArray<int> rolled, Il2CppStructArray<int> values, int modId)
        {
            DataProps.MartialData source = DataProps.PropsData.NewMartial(kind.Type, rolled).To<DataProps.MartialData>();
            var prefixs = source.GetPrefixs();
            int skillType = -1;
            for (int i = 0; i < prefixs.Count && skillType < 0; i++)
            {
                if (prefixs[i].prefixValueItem != null)
                {
                    skillType = prefixs[i].prefixValueItem.skillType;
                }
            }
            if (skillType < 0)
            {
                return;
            }
            var modItems = g.conf.battleSkillPrefixValue.GetItems(skillType, modId);
            if (modItems == null)
            {
                return;
            }
            var modNumbers = new List<int>();
            for (int i = 0; i < modItems.Count; i++)
            {
                modNumbers.Add(modItems[i].number);
            }
            int end = values.Length;
            for (int i = 2; i < values.Length; i++)
            {
                if (values[i] == -1)
                {
                    end = i;
                    break;
                }
            }
            var used = new HashSet<int>();
            for (int i = 2; i < end; i++)
            {
                if (modNumbers.Contains(values[i]))
                {
                    used.Add(values[i]);
                }
            }
            for (int i = 2; i < end; i++)
            {
                if (values[i] <= 0 || modNumbers.Contains(values[i]))
                {
                    continue;
                }
                // 그 자리(i-2)에 나올 수 있는(자리 가중치 > 0) 안 쓴 번호 가운데 하나를 무작위로. 없으면 안 쓴 첫 번호(읽기 시험에 맡긴다).
                var fit = new List<int>();
                int first = -1;
                for (int k = 0; k < modItems.Count; k++)
                {
                    int n = modItems[k].number;
                    if (used.Contains(n))
                    {
                        continue;
                    }
                    if (first < 0)
                    {
                        first = n;
                    }
                    if (MartialService.Weight(modItems[k], i - 2) > 0)
                    {
                        fit.Add(n);
                    }
                }
                int pick = fit.Count > 0 ? fit[rng.Next(fit.Count)] : first;
                if (pick >= 0)
                {
                    values[i] = pick;
                    used.Add(pick);
                }
            }
        }

        // 모드 공법의 비급 값(인계 8.3e절·05 문서 44절의 실측): 같은 분류의 기본 공법(원본)을 뽑아 0번(기술 번호)을 모드 번호로 바꾸고,
        // 옵션 번호 가운데 모드에 없는 것을 모드의 번호로 바꾼 뒤(RemapPrefixNumbers),
        // 가방에 넣기 전에 읽어 본다. 게임이 읽지 못하는 조합(무예는 넷에 하나꼴로 읽혔다)이면 버리고 다시 뽑는다. 최대 ModRollMax번.
        // 품질은 보지 않는다: 모드 공법의 품질은 뽑은 옵션과 무관하게 게임이 정한다(실측, 05 문서 42절).
        internal static Il2CppStructArray<int> RollValuesForMod(int kindIndex, Entry mod, int grade, out string reason)
        {
            reason = null;
            Kind kind = Kinds[kindIndex];
            Entry source = SourceFor(kindIndex, mod, grade);
            if (source == null)
            {
                reason = "원본으로 쓸 기본 공법이 없습니다";
                return null;
            }
            for (int tries = 1; tries <= ModRollMax; tries++)
            {
                Il2CppStructArray<int> rolled = RollValues(kind, source.BaseId, grade, RollMax, null);
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
                    RemapPrefixNumbers(kind, rolled, values, mod.BaseId);
                    DataProps.MartialData martial = DataProps.PropsData.NewMartial(kind.Type, values).To<DataProps.MartialData>();
                    if (martial.baseID == mod.BaseId && !string.IsNullOrEmpty(martial.martialInfo.name))
                    {
                        return values;
                    }
                }
                catch (Exception)
                {
                    // 게임이 읽지 못하는 조합. 다시 뽑는다.
                }
            }
            reason = "모드 공법의 값을 게임이 읽지 못했습니다(" + ModRollMax + "회 시도)";
            return null;
        }

        private static bool Accepts(Kind kind, Il2CppStructArray<int> values, int baseId, Func<DataProps.MartialData, bool> accept)
        {
            DataProps.MartialData martial = DataProps.PropsData.NewMartial(kind.Type, values).To<DataProps.MartialData>();
            return martial.baseID == baseId && (accept == null || accept(martial));
        }

        // 그 분류의 그 번호 기술. 없으면 null(즐겨찾기의 비급 줄이 쓴다).
        public static Entry Get(int kindIndex, int baseId)
        {
            if (kindIndex < 0 || kindIndex >= Kinds.Length)
            {
                return null;
            }
            return All(kindIndex).Find(e => e.BaseId == baseId);
        }

        private enum Outcome { Rolled, Plain, Refused }

        // 기술 하나를 배운 상태로 넣는다. 옵션은 게임의 뽑기가 정한다: 원하는 기술이 나올 때까지 묶음에서 다시 뽑고,
        // 끝내 나오지 않거나(강제 추가처럼) 뽑기 대상이 아니면 옵션 없는 기술로 만든다.
        private static Outcome LearnNow(WorldUnitBase unit, Kind kind, Entry entry, int grade, bool roll)
        {
            Il2CppStructArray<int> values = roll ? RollValues(kind, entry.BaseId, grade, RollMax, null) : null;
            bool plain = values == null;
            if (plain)
            {
                // 값의 자리: 3번째가 종류, 4번째가 기술 번호, 5번째가 경지(실측한 옵션 없는 공법의 모양).
                int[] raw = { 2, 0, 1, (int)kind.Type, entry.BaseId, grade };
                values = new Il2CppStructArray<int>(raw.Length);
                for (int i = 0; i < raw.Length; i++)
                {
                    values[i] = raw[i];
                }
            }
            var data = unit.data.unitData;
            int before = data.allActionMartial.Count;
            DataProps.PropsData props = DataProps.PropsData.NewMartial(kind.Type, values);
            // 게임이 알아보지 못하는 조합이면 이름 없는 깨진 기술이 들어간다. 배우게 한 뒤 정보를 읽어 보고,
            // 읽히지 않으면(게임 쪽에서 예외가 난다) 바로 지운다. 배우기 전에는 정상 기술도 정보가 읽히지 않는다.
            data.StudyActionMartialProp(props);
            if (data.allActionMartial.Count != before + 1)
            {
                return Outcome.Refused;
            }
            bool valid;
            try
            {
                DataProps.MartialData made = data.GetActionMartial(props.soleID).data.To<DataProps.MartialData>();
                valid = made.baseID == entry.BaseId && !string.IsNullOrEmpty(made.martialInfo.name);
            }
            catch (System.Exception)
            {
                valid = false;
            }
            if (!valid)
            {
                data.DelActionMartial(props.soleID);
                return Outcome.Refused;
            }            return plain ? Outcome.Plain : Outcome.Rolled;
        }

        // force가 참이면 경지 범위 밖이어도, 그 기술의 범위 안에서 가장 가까운 경지로 만들어 넣는다.
        public static string Learn(WorldUnitBase unit, int kindIndex, Entry entry, int grade, bool force)
        {
            Kind kind = Kinds[kindIndex];
            if (entry.IsMod)
            {
                return entry.Name + ": 모드 공법은 자원 탭의 기술 서적에서 비급으로 받습니다.";
            }
            string blocked = BlockReason(entry, grade);
            if (blocked != null && !force)
            {
                return entry.Name + ": " + blocked + ".";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            // 강제 추가: 게임은 경지 범위 밖의 기술을 만들지 못한다(실측). 범위 안에서 가장 가까운 경지로 만든다.
            if (blocked != null)
            {
                grade = grade < entry.GradeMin ? entry.GradeMin : entry.GradeMax;
            }
            Outcome outcome = LearnNow(unit, kind, entry, grade, true);
            bool ok = outcome != Outcome.Refused;
            ChangeLog.Add(EditService.NameOf(unit), kind.Tab + " " + entry.Name + "(" + entry.BaseId + ", 경지 " + grade + ")" + (blocked != null ? " 강제" : ""), "없음", ok ? "익힘" : "없음", ok);
            if (!ok)
            {
                return entry.Name + ": 게임이 이 기술을 만들지 못했습니다. 추가하지 않았습니다.";
            }
            return kind.Tab + (blocked != null ? " 강제 추가: " : " 추가: ") + entry.Name + " (경지 " + grade + ")"
                + (outcome == Outcome.Plain ? " — 옵션 없이 만들었습니다." : "");
        }

        // 이 분류에서 지금 배울 수 있고 아직 익히지 않은 기술을 모두 넣는다.
        public static string LearnAll(WorldUnitBase unit, int kindIndex, int grade)
        {
            Kind kind = Kinds[kindIndex];
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            Dictionary<int, int> have = LearnedCounts(unit, kindIndex);
            int learned = 0, plain = 0, refused = 0, skipped = 0;
            foreach (Entry entry in All(kindIndex))
            {
                if (entry.IsMod)
                {
                    skipped++;
                    continue;
                }
                if (have.ContainsKey(entry.BaseId))
                {
                    continue;
                }
                if (BlockReason(entry, grade) != null)
                {
                    skipped++;
                    continue;
                }
                Outcome outcome = LearnNow(unit, kind, entry, grade, true);
                if (outcome == Outcome.Refused)
                {
                    refused++;
                    continue;
                }
                learned++;
                if (outcome == Outcome.Plain)
                {
                    plain++;
                }
            }
            ChangeLog.Add(EditService.NameOf(unit), kind.Tab + " 모두 배우기(경지 " + grade + ")", "", learned + "개 익힘", learned > 0 || refused == 0);
            return kind.Tab + ": " + learned + "개를 익혔습니다(경지 " + grade + ")."
                + (plain > 0 ? " 그중 " + plain + "개는 옵션 없이 만들었습니다." : "")
                + (skipped > 0 ? " 경지 범위 밖 " + skipped + "개는 건너뛰었습니다." : "")
                + (refused > 0 ? " " + refused + "개는 게임이 만들지 못했습니다." : "");
        }
    }
}
