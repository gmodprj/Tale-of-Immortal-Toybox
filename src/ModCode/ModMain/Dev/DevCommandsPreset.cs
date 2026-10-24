using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 프리셋의 검증(tools/research/preset-check.ps1)이 상태를 읽고 서비스를 직접 부르는 데 쓴다.
    // 상태를 읽는 명령은 서비스를 거치지 않고 게임 값을 바로 읽는다(서비스의 판정을 서비스로 확인하지 않기 위해서다).
    internal static class DevCommandsPreset
    {
        private const int ArtifactClass = 401;
        private static Dictionary<int, bool> shownTalents;

        public static string Execute(string[] p)
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            switch (p[0])
            {
                case "pstate": return State(me);
                case "psoles": return Soles(me);
                case "pmartial": return Martials(me, int.Parse(p[1]));
                case "pbattle": return Battle();
                case "pgpath": return GradePath(me, int.Parse(p[1]));
                case "pgsum": return GradeSum(me, int.Parse(p[1]));
                case "pgraise": return GradeService.RaiseTo(me, int.Parse(p[1]));
                case "poptpick": return OptionPick(int.Parse(p[1]), int.Parse(p[2]));
                case "pmake": return MakeSkill(me, int.Parse(p[1]), int.Parse(p[2]), p[3]);
                case "psuit": return WriteSuit(me, int.Parse(p[1]), int.Parse(p[2]));
                case "psprite":
                    {
                        bool changed;
                        string result = SpriteService.Apply(me, int.Parse(p[1]), int.Parse(p[2]), out changed);
                        return "changed=" + changed + " | " + result;
                    }
                case "pelder": return ElderService.Set(me, int.Parse(p[1]));
                case "pwing": return WingmanService.Set(me, int.Parse(p[1]), Ints(p[2]), Ints(p[3]));
                case "pdefaults":
                    return "embedded=" + PresetService.DefaultCount + " changed=" + PresetService.DefaultsChanged()
                        + " dir=" + System.IO.Directory.Exists(PresetService.Dir);
                case "plist": return ListPresets();
                case "pcheck": return CheckPresets(p.Length > 1 ? p[1] : null);
                case "pstrict": return StrictCheck(p.Length > 1 ? p[1] : null);
                case "prestore": return PresetService.RestoreDefaults();
                case "pstage": return StageInfo(p[1], int.Parse(p[2]));
                case "papply": return ApplyPreset(p[1], p[2]);
                case "preplaces": return ReplacesInfo(p[1], p[2]);
                case "pfatepick": return FatePick(me, int.Parse(p[1]), p.Length > 2 ? p[2] : "");
                case "pbornpick": return BornPick(me);
                case "pfind": return Each(p, FindSkill);
                case "pfindch": return FindByChinese(p[1], p.Length > 2 ? int.Parse(p[2]) : 12);
                case "pfate": return Each(p, FindFate);
                case "pluck": return Each(p, FindLuck);
                case "psuits": return p.Length > 1 ? Each(p, FindSuit) : FindSuit("");
                case "pconf": return Conf(p[1]);
                case "popts": return OptionList(int.Parse(p[1]));
                case "pdump": return Dump(p[1]);
                case "pfateset": return FateService.Set(me, int.Parse(p[1]), int.Parse(p[2]));
                case "pluckadd": return LuckService.Add(me, int.Parse(p[1]));
                case "pborn": return LuckService.AddBorn(me, int.Parse(p[1]));
                case "cexport": return CharacterExportService.Export(p[1]);
                case "cimpsec":
                    {
                        // 그 이름의 캐릭터 내보내기 파일에서 한 구역만 가져온다(grade·attributes·lucks·fates·martials·arts). 검사 import-settle이 쓴다.
                        var section = (CharacterExportService.Section)Enum.Parse(typeof(CharacterExportService.Section), p[2], true);
                        foreach (CharacterExportService.Pack pack in CharacterExportService.List())
                        {
                            if (pack.Name == p[1] && pack.Error == null)
                            {
                                return CharacterExportService.ImportSection(pack, section);
                            }
                        }
                        return "no pack " + p[1];
                    }
                case "cimpfates":
                    {
                        // 이름이 그것인 캐릭터 내보내기 파일의 역천개명 구역을 가져온다(화면의 "이 항목 모두 가져오기").
                        foreach (CharacterExportService.Pack pack in CharacterExportService.List())
                        {
                            if (pack.Name == p[1] && pack.Error == null)
                            {
                                return CharacterExportService.ImportSection(pack, CharacterExportService.Section.Fates);
                            }
                        }
                        return "no pack " + p[1];
                    }
                case "phas":
                    {
                        // 그 번호의 운명들 가운데 실제로 가지고 있는 수(효과가 붙어 있는가). 기록만 보는 검사를 보완한다.
                        int held = 0;
                        int[] ids = Ints(p[1]);
                        foreach (int id in ids)
                        {
                            held += LuckService.Has(me, id) ? 1 : 0;
                        }
                        return "held=" + held + " of=" + ids.Length;
                    }
                default: return null;
            }
        }

        // 장착 칸의 고유 번호를 기술 번호로. 비었으면 0.
        private static int BaseOf(WorldUnitBase me, string sole)
        {
            if (string.IsNullOrEmpty(sole))
            {
                return 0;
            }
            try
            {
                return me.data.unitData.GetActionMartial(sole).data.To<DataProps.MartialData>().baseID;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private static int SuitOf(DataProps.PropsData props)
        {
            try
            {
                DataProps.PropsAbilityData ability = props.To<DataProps.PropsAbilityData>();
                return ability == null ? 0 : ability.suitID;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // 설정 표의 isShow가 1인 재능인가(게임 화면에 보이는 재능).
        private static bool TalentShown(int spriteId, int number)
        {
            if (shownTalents == null)
            {
                shownTalents = new Dictionary<int, bool>();
                var all = g.conf.artifactSpriteTalent._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    shownTalents[all[i].spriteID * 1000 + all[i].number] = all[i].isShow == 1;
                }
            }
            bool shown;
            return shownTalents.TryGetValue(spriteId * 1000 + number, out shown) && shown;
        }

        private static string Join(Il2CppSystem.Collections.Generic.List<int> list)
        {
            var sb = new StringBuilder();
            for (int i = 0; list != null && i < list.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(list[i]);
            }
            return sb.ToString();
        }

        private static string State(WorldUnitBase me)
        {
            var data = me.data.unitData;
            var saved = data.propertyData;
            var dyn = me.data.dynUnitData;
            ConfRoleGradeItem row = g.conf.roleGrade.GetItem(saved.gradeID);
            var sb = new StringBuilder();
            sb.Append("grade=").Append(saved.gradeID).Append(" major=").Append(row == null ? 0 : row.grade)
                .Append(" ap=").Append(saved.abilityPoint).Append(" used=").Append(dyn.GetCurUseAbilityPoint())
                .Append(" attack=").Append(saved.attack).Append(" attackv=").Append(dyn.attack.value)
                .Append(" defense=").Append(saved.defense).Append(" hpMax=").Append(saved.hpMax).Append(" life=").Append(saved.life)
                .Append(" martials=").Append(data.allActionMartial.Count)
                .Append(" left=").Append(BaseOf(me, data.skillLeft)).Append(" right=").Append(BaseOf(me, data.skillRight))
                .Append(" step=").Append(BaseOf(me, data.step)).Append(" ult=").Append(BaseOf(me, data.ultimate)).Append(" abilities=");
            bool first = true;
            for (int i = 0; data.abilitys != null && i < data.abilitys.Length; i++)
            {
                if (string.IsNullOrEmpty(data.abilitys[i]))
                {
                    continue;
                }
                DataUnit.ActionMartialData action = data.GetActionMartial(data.abilitys[i]);
                sb.Append(first ? "" : ",").Append(BaseOf(me, data.abilitys[i])).Append(':').Append(action == null ? 0 : SuitOf(action.data));
                first = false;
            }

            // 법보와 기령: 장착한 법보, 거기 든 기령, 켠 재능. dupes는 두 법보가 같은 기령을 가리키는 수(0이어야 한다).
            int artifact = 0, sprite = 0, on = 0, shown = 0, allOn = 0, allTalents = 0, dupes = 0;
            var spriteData = data.artifactSpriteData;
            int sprites = spriteData == null || spriteData.sprites == null ? 0 : spriteData.sprites.Count;
            var holders = new List<int>();
            var props = data.propData.allProps;
            for (int i = 0; i < props.Count; i++)
            {
                ConfItemPropsItem conf = g.conf.itemProps.GetItem(props[i].propsID);
                if (conf == null || conf.className != ArtifactClass)
                {
                    continue;
                }
                int inside = props[i].To<DataProps.PropsArtifact>().spriteSoleID;
                if (inside != 0)
                {
                    if (holders.Contains(inside))
                    {
                        dupes++;
                    }
                    holders.Add(inside);
                }
                if (!data.IsEquipProps(props[i].soleID))
                {
                    continue;
                }
                artifact = props[i].propsID;
                var held = inside == 0 || spriteData == null ? null : spriteData.GetSpriteInSoleID(inside);
                if (held == null)
                {
                    continue;
                }
                sprite = held.spriteID;
                for (int t = 0; held.talents != null && t < held.talents.Count; t++)
                {
                    bool lit = held.talents[t].state != 0;
                    allTalents++;
                    allOn += lit ? 1 : 0;
                    if (TalentShown(held.spriteID, held.talents[t].number))
                    {
                        shown++;
                        on += lit ? 1 : 0;
                    }
                }
            }
            sb.Append(" artifact=").Append(artifact).Append(" sprite=").Append(sprite).Append(" sprites=").Append(sprites).Append(" dupes=").Append(dupes)
                .Append(" talents=").Append(on).Append('/').Append(shown).Append(" talentsall=").Append(allOn).Append('/').Append(allTalents);

            // 신혼: 번호와 지금 단계. 대경지 7 아래에서는 단계 표를 읽는 것이 예외를 낼 수 있다.
            var elder = data.elderData;
            int elderId = elder == null ? 0 : elder.id;
            int elderLevel = 0;
            if (elderId != 0)
            {
                try
                {
                    ConfElderLevelItem item = g.conf.elderLevel.GetItem(me);
                    elderLevel = item == null ? 0 : item.level;
                }
                catch (Exception)
                {
                    elderLevel = -1;
                }
            }
            sb.Append(" elder=").Append(elderId).Append(" elderlv=").Append(elderLevel);

            var reborn = data.reborn;
            sb.Append(" wing=").Append(reborn == null ? 0 : reborn.wingManID)
                .Append(" effects=").Append(reborn == null ? "" : Join(reborn.wingmanEffectList))
                .Append(" prefixes=").Append(reborn == null ? "" : Join(reborn.wingmanPrefixList));

            var records = new List<KeyValuePair<int, int>>(FateService.Records(me));
            records.Sort((a, b) => a.Key.CompareTo(b.Key));
            sb.Append(" fates=");
            for (int i = 0; i < records.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(records[i].Key).Append(':').Append(records[i].Value);
            }
            sb.Append(" born=").Append(saved.bornLuck.Length);
            return sb.ToString();
        }

        private static string Soles(WorldUnitBase me)
        {
            var soles = new List<string>();
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                soles.Add(pair.Key);
            }
            soles.Sort(StringComparer.Ordinal);
            return "soles=" + string.Join(",", soles.ToArray());
        }

        // 그 기술 번호의 익힌 공법마다: 고유 번호, 경지, 장착, 세트 번호, 희귀도, 수도 점수 비용, 옵션 번호, 모든 옵션이 품질 상한인지.
        private static string Martials(WorldUnitBase me, int baseId)
        {
            var sb = new StringBuilder();
            int count = 0;
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                try
                {
                    DataProps.PropsData props = pair.Value.data;
                    DataProps.MartialData martial = props.To<DataProps.MartialData>();
                    if (martial.baseID != baseId)
                    {
                        continue;
                    }
                    count++;
                    List<MartialService.PrefixView> views = MartialService.Prefixes(martial);
                    views.Sort((a, b) => a.Index.CompareTo(b.Index));
                    bool atMax = true;
                    var numbers = new StringBuilder();
                    for (int i = 0; i < views.Count; i++)
                    {
                        numbers.Append(i == 0 ? "" : ",").Append(views[i].Number);
                        atMax = atMax && views[i].Level >= views[i].MaxLevel;
                    }
                    sb.Append(" | sole=").Append(pair.Key).Append(" g=").Append(martial.grade).Append(" eq=").Append(MartialService.IsEquipped(me, pair.Key))
                        .Append(" suit=").Append(martial.martialType == MartialType.Ability ? SuitOf(props) : 0)
                        .Append(" lv=").Append(martial.martialInfo.level).Append(" cost=").Append(martial.martialInfo.abilityPoint)
                        .Append(" nums=").Append(numbers).Append(" atmax=").Append(atMax);
                }
                catch (Exception)
                {
                }
            }
            return "n=" + count + sb;
        }

        // 전투 중의 플레이어 단위: 켜진 세트(번호:구성원 수), 명우, 도구(법보가 여기 든다), 기술의 수.
        private static string Battle()
        {
            if (SceneType.battle == null || SceneType.battle.battleMap == null || SceneType.battle.battleMap.playerUnitCtrl == null)
            {
                return "not in battle";
            }
            UnitCtrlPlayer ctrl = SceneType.battle.battleMap.playerUnitCtrl;
            var suits = new StringBuilder();
            foreach (var pair in ctrl.activeSuits)
            {
                suits.Append(suits.Length == 0 ? "" : ",").Append(pair.Key).Append(':').Append(pair.Value);
            }
            UnitCtrlHuman human = ctrl.TryCast<UnitCtrlHuman>();
            WingManBase wing = human == null ? null : human.wingman;
            return "suits=" + suits + " wing=" + (wing == null ? 0 : wing.wingManID)
                + " wingobjs=" + (wing == null || wing.wingManList == null ? 0 : wing.wingManList.Count)
                + " props=" + (ctrl.props == null ? 0 : ctrl.props.Count) + " skills=" + (ctrl.skills == null ? 0 : ctrl.skills.Count);
        }

        private static string GradePath(WorldUnitBase me, int toId)
        {
            var sb = new StringBuilder("path=");
            List<ConfRoleGradeItem> path = GradeService.Path(me.data.unitData.propertyData.gradeID, toId);
            for (int i = 0; i < path.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(path[i].id);
            }
            return sb.ToString();
        }

        // 그 줄까지 올린 뒤 기대하는 저장값. 서비스의 더하기를 거치지 않고 줄마다의 증가분을 따로 더한다(상한은 보지 않는다).
        private static string GradeSum(WorldUnitBase me, int toId)
        {
            var saved = me.data.unitData.propertyData;
            int attack = saved.attack, defense = saved.defense, hpMax = saved.hpMax, life = saved.life, points = saved.abilityPoint;
            foreach (ConfRoleGradeItem row in GradeService.Path(saved.gradeID, toId))
            {
                foreach (var pair in UnitActionRoleUpGrade.GetAddAttr(row))
                {
                    attack += pair.Key == "atk" ? pair.Value : 0;
                    defense += pair.Key == "def" ? pair.Value : 0;
                    hpMax += pair.Key == "hpMax" ? pair.Value : 0;
                    life += pair.Key == "life" ? pair.Value : 0;
                }
                life += row.growAddLife;
                points += row.abilityPoint;
            }
            return "attack=" + attack + " defense=" + defense + " hpMax=" + hpMax + " life=" + life + " ap=" + points;
        }

        // 쉼표로 이은 번호들. "-"나 빈 글은 빈 배열.
        private static int[] Ints(string csv)
        {
            var list = new List<int>();
            foreach (string part in (csv ?? "").Split(','))
            {
                int value;
                if (int.TryParse(part, out value))
                {
                    list.Add(value);
                }
            }
            return list.ToArray();
        }

        // 그 기술 번호의 분류와 목록의 항목. 없으면 null.
        private static SkillCatalog.Entry EntryOf(int baseId, out MartialType type)
        {
            type = MartialType.SkillLeft;
            for (int kind = 0; kind < SkillCatalog.Kinds.Length; kind++)
            {
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(kind, baseId.ToString()))
                {
                    if (entry.BaseId == baseId)
                    {
                        type = SkillCatalog.Kinds[kind].Type;
                        return entry;
                    }
                }
            }
            return null;
        }

        // 그 기술에 쓸 수 있는 옵션을 자리마다 하나씩 골라 count개. 검사가 프리셋 파일을 만들 때 쓴다.
        private static string OptionPick(int baseId, int count)
        {
            MartialType type;
            SkillCatalog.Entry entry = EntryOf(baseId, out type);
            if (entry == null)
            {
                return "no skill " + baseId;
            }
            var picked = new List<int>();
            var items = g.conf.battleSkillPrefixValue.GetItems((int)type, baseId);
            for (int slot = 0; slot < count; slot++)
            {
                for (int i = 0; items != null && i < items.Count; i++)
                {
                    var candidate = new List<int>(picked);
                    candidate.Add(items[i].number);
                    if (MartialService.OptionProblem(type, baseId, candidate.ToArray()) == null)
                    {
                        picked = candidate;
                        break;
                    }
                }
            }
            var sb = new StringBuilder();
            for (int i = 0; i < picked.Count; i++)
            {
                sb.Append(i == 0 ? "" : ",").Append(picked[i]);
            }
            return "type=" + (int)type + " gmin=" + entry.GradeMin + " gmax=" + entry.GradeMax + " opts=" + sb;
        }

        private static string MakeSkill(WorldUnitBase me, int baseId, int grade, string options)
        {
            MartialType type;
            SkillCatalog.Entry entry = EntryOf(baseId, out type);
            if (entry == null)
            {
                return "result=fail | no skill " + baseId;
            }
            if (grade < entry.GradeMin || grade > entry.GradeMax)
            {
                return "result=fail | grade out of range " + entry.GradeMin + "-" + entry.GradeMax;
            }
            string sole;
            string error = MartialService.Make(me, type, baseId, grade, Ints(options), out sole);
            return error == null ? "result=ok sole=" + sole : "result=fail | " + error;
        }

        private static string WriteSuit(WorldUnitBase me, int baseId, int suitId)
        {
            foreach (var pair in me.data.unitData.allActionMartial)
            {
                try
                {
                    if (pair.Value.data.To<DataProps.MartialData>().baseID == baseId)
                    {
                        bool ok = MartialService.SetSuit(pair.Value.data, suitId);
                        return "ok=" + ok + " suit=" + MartialService.SuitOf(pair.Value.data);
                    }
                }
                catch (Exception)
                {
                }
            }
            return "not learned " + baseId;
        }

        private static PresetService.Preset PresetNamed(string fileName)
        {
            return PresetService.List().Find(x => System.IO.Path.GetFileName(x.File) == fileName);
        }

        private static string ListPresets()
        {
            List<PresetService.Preset> presets = PresetService.List();
            var names = new StringBuilder();
            int unreadable = 0;
            for (int i = 0; i < presets.Count; i++)
            {
                unreadable += presets[i].Error == null ? 0 : 1;
                names.Append(i == 0 ? "" : ";").Append(i).Append(':').Append(System.IO.Path.GetFileName(presets[i].File));
            }
            return "count=" + presets.Count + " unreadable=" + unreadable + " | " + names;
        }

        // 폴더의 프리셋(파일 이름을 주면 그것만)의 쓸 수 없는 줄을 센다. 기본 프리셋은 0이어야 한다.
        private static string CheckPresets(string fileName)
        {
            int count = 0, unreadable = 0, usable = 0;
            var problems = new List<string>();
            foreach (PresetService.Preset preset in PresetService.List())
            {
                if (fileName != null && System.IO.Path.GetFileName(preset.File) != fileName)
                {
                    continue;
                }
                count++;
                if (preset.Error != null)
                {
                    unreadable++;
                    continue;
                }
                usable += PresetService.Usable(preset);
                foreach (string problem in PresetService.Problems(preset))
                {
                    problems.Add(System.IO.Path.GetFileName(preset.File) + " " + problem);
                }
            }
            return "presets=" + count + " unreadable=" + unreadable + " problems=" + problems.Count + " usable=" + usable
                + " | " + string.Join(" / ", problems.ToArray());
        }

        // 프리셋의 옵션을 자리 판정까지 엄격하게 본다(프리셋 적용은 자리 판정을 건너뛴다, 05 문서 68절). 운명은 파일의 이름과 게임 이름, 경지 범위.
        private static string StrictCheck(string fileName)
        {
            var sb = new StringBuilder();
            foreach (PresetService.Preset preset in PresetService.List())
            {
                string file = System.IO.Path.GetFileName(preset.File);
                if ((fileName != null && file != fileName) || preset.Error != null)
                {
                    continue;
                }
                var lines = new List<PresetService.SkillLine>(preset.Skills);
                lines.AddRange(preset.Abilities);
                foreach (PresetService.SkillLine line in lines)
                {
                    MartialType gameType;
                    SkillCatalog.Entry entry = EntryOf(line.BaseId, out gameType);
                    sb.Append(file).Append(" skill ").Append(line.BaseId).Append(" file=").Append(line.Name).Append(" game=").Append(entry == null ? "(none)" : entry.Name).Append('\n');
                    string strict = MartialService.OptionProblem(line.Type, line.BaseId, line.Options, false);
                    if (strict != null)
                    {
                        sb.Append(file).Append(' ').Append(line.BaseId).Append(' ').Append(strict).Append('\n');
                    }
                }
                foreach (PresetService.FateLine line in preset.Fates)
                {
                    ConfFateFeatureItem fate = g.conf.fateFeature.GetItem(line.Id);
                    sb.Append(file).Append(" fate ").Append(line.Grade).Append(':').Append(line.Id).Append(' ').Append(line.Name)
                      .Append(fate == null ? " (none)" : " g" + fate.gradeMin + "-" + fate.gradeMax + " group" + fate.group)
                      .Append(line.Problem == null ? "" : " PROBLEM " + line.Problem).Append('\n');
                }
            }
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        private static string StageInfo(string fileName, int grade)
        {
            PresetService.Preset preset = PresetNamed(fileName);
            if (preset == null || preset.Error != null)
            {
                return "no readable preset " + fileName;
            }
            PresetService.Stage stage = PresetService.StageOf(preset, grade);
            int have, total;
            PresetService.StageState state = PresetService.StateOf(stage, PresetService.Snapshot(), out have, out total);
            return "state=" + state + " have=" + have + " total=" + total + " | " + PresetService.StageText(stage).Replace("\n", " / ");
        }

        private static string ApplyPreset(string fileName, string what)
        {
            PresetService.Preset preset = PresetNamed(fileName);
            if (preset == null)
            {
                return "refused=True | no preset " + fileName;
            }
            int grade;
            string result = what == "all" ? PresetService.ApplyAll(preset)
                : what == "full" ? PresetService.ApplyFull(preset)
                : int.TryParse(what, out grade) ? PresetService.ApplyStage(preset, grade) : "unknown " + what;
            return (PresetService.LastRun.Length == 0 ? "refused=True" : PresetService.LastRun) + " | " + result;
        }

        private static string ReplacesInfo(string fileName, string what)
        {
            PresetService.Preset preset = PresetNamed(fileName);
            if (preset == null)
            {
                return "no preset " + fileName;
            }
            string replaces = what == "all" ? PresetService.ReplacesAll(preset)
                : what == "full" ? PresetService.ReplacesFull(preset) : PresetService.ReplacesStage(preset, int.Parse(what));
            return "replaces=" + (replaces == null ? "no" : "yes") + " | " + replaces;
        }

        // 그 대경지에서 고를 수 있는 역천개명 가운데 가지고 있지 않고 빼낼 목록에도 없는 첫 번째. 검사가 프리셋 파일을 만들 때 쓴다.
        private static string FatePick(WorldUnitBase me, int grade, string exclude)
        {
            // 게임은 같은 계열(fateFeature.group)의 역천개명을 하나만 둔다. 이미 고른 것, 지금 기록에 있는 것과 계열이 겹치지 않게 고른다.
            var skip = new List<int>(Ints(exclude));
            var taken = new List<int>(skip);
            taken.AddRange(FateService.Records(me).Values);
            var groups = new List<int>();
            foreach (int id in taken)
            {
                ConfFateFeatureItem conf = g.conf.fateFeature.GetItem(id);
                if (conf != null && conf.group != 0)
                {
                    groups.Add(conf.group);
                }
            }
            foreach (LuckService.LuckView view in FateService.Candidates(grade, ""))
            {
                ConfFateFeatureItem conf = g.conf.fateFeature.GetItem(view.Id);
                if (!LuckService.Has(me, view.Id) && !skip.Contains(view.Id) && (conf.group == 0 || !groups.Contains(conf.group)))
                {
                    return "id=" + view.Id;
                }
            }
            return "id=0";
        }

        // 가지고 있지 않은 선천 분류(1)의 운명 가운데 첫 번째.
        private static string BornPick(WorldUnitBase me)
        {
            foreach (LuckService.LuckView view in LuckService.Candidates(1, ""))
            {
                if (!LuckService.Has(me, view.Id) && !LuckService.IsSystem(view.Id))
                {
                    return "id=" + view.Id;
                }
            }
            return "id=0";
        }

        private static string Each(string[] p, Func<string, string> find)
        {
            var sb = new StringBuilder();
            for (int i = 1; i < p.Length; i++)
            {
                sb.Append("\n").Append(p[i]).Append(" => ").Append(find(p[i]));
            }
            return sb.ToString();
        }

        private static string Name(string key)
        {
            return EditService.Compact(GameTool.LS(key));
        }

        private static string FindSkill(string name)
        {
            var sb = new StringBuilder();
            for (int kind = 0; kind < SkillCatalog.Kinds.Length; kind++)
            {
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(kind, name))
                {
                    sb.Append('[').Append(kind).Append(' ').Append(entry.BaseId).Append(' ').Append(entry.Name)
                        .Append(" g").Append(entry.GradeMin).Append('-').Append(entry.GradeMax).Append("] ");
                }
            }
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        // 중국어 원문 이름으로 찾기(docs/superpowers/specs/2026-10-20-preset-drafts-design.md 4절): LocalText의 ch에 조각이 든 줄의
        // 한국어 이름(LS)으로 기술·역천개명·세트를 찾는다. 공략 원문의 이름을 게임 번호로 옮길 때 쓴다.
        private static string FindByChinese(string fragment, int max)
        {
            var sb = new StringBuilder();
            var seen = new HashSet<string>();
            var all = g.conf.localText.allConfList;
            int shown = 0;
            for (int i = 0; i < all.Count && shown < max; i++)
            {
                ConfLocalTextItem item = all[i];
                if (item == null || string.IsNullOrEmpty(item.ch) || !item.ch.Contains(fragment) || item.ch.Length > 24)
                {
                    continue;
                }
                string ko = GameTool.LS(item.key);
                if (string.IsNullOrEmpty(ko) || !seen.Add(ko))
                {
                    continue;
                }
                shown++;
                sb.Append("ch=").Append(item.ch).Append(" ko=").Append(ko).Append(" | skill ").Append(FindSkill(ko))
                  .Append(" | fate ").Append(FindFate(ko)).Append(" | suit ").Append(FindSuit(ko)).Append("\n");
            }
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        private static string FindFate(string name)
        {
            var sb = new StringBuilder();
            var all = g.conf.fateFeature._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(all[i].id);
                if (conf == null || !Name(conf.name).Contains(name))
                {
                    continue;
                }
                sb.Append('[').Append(conf.id).Append(' ').Append(Name(conf.name)).Append(" g").Append(all[i].gradeMin).Append('-')
                    .Append(all[i].gradeMax).Append(" group").Append(all[i].group).Append(" lv").Append(conf.level).Append("] ");
            }
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        private static string FindLuck(string name)
        {
            var sb = new StringBuilder();
            var all = g.conf.roleCreateFeature._allConfList;
            int shown = 0;
            for (int i = 0; i < all.Count && shown < 12; i++)
            {
                if (!Name(all[i].name).Contains(name))
                {
                    continue;
                }
                sb.Append('[').Append(all[i].id).Append(' ').Append(Name(all[i].name)).Append(" type").Append(all[i].type)
                    .Append(" lv").Append(all[i].level).Append("] ");
                shown++;
            }
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        private static string FindSuit(string name)
        {
            var sb = new StringBuilder();
            var all = g.conf.battleAbilitySuitBase._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                string suit = Name(all[i].suitName);
                if (name.Length > 0 && !suit.Contains(name))
                {
                    continue;
                }
                sb.Append("\n[").Append(all[i].id).Append(' ').Append(suit).Append(" type").Append(all[i].suitType)
                    .Append(" member=").Append(all[i].suitMember).Append(" cond=").Append(all[i].suitCondition).Append(']');
            }
            return sb.Length == 0 ? "(none)" : sb.ToString();
        }

        private static string Conf(string what)
        {
            var sb = new StringBuilder();
            if (what == "elder")
            {
                var all = g.conf.elderBase._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    sb.Append('[').Append(all[i].id).Append(' ').Append(Name(all[i].name)).Append(" item").Append(all[i].item)
                        .Append(' ').Append(all[i].growAttr).Append("] ");
                }
            }
            else if (what == "sprite")
            {
                var all = g.conf.artifactSprite._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    sb.Append('[').Append(all[i].id).Append(' ').Append(Name(all[i].name)).Append("] ");
                }
            }
            else if (what == "wing")
            {
                var all = g.conf.wingmanBase._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    sb.Append('[').Append(all[i].id).Append(' ').Append(Name(all[i].name)).Append("] ");
                }
            }
            else if (what == "heart")
            {
                var all = g.conf.taoistHeart._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    sb.Append('[').Append(all[i].id).Append(' ').Append(Name(all[i].heartName)).Append("] ");
                }
            }
            return sb.Length == 0 ? "unknown table" : sb.ToString();
        }

        // 그 기술에 넣을 수 있는 옵션마다: 번호, 중복 한도, 선행(양수)·배타(음수) 조건, 나올 수 있는 자리(1부터), 설명.
        // 프리셋의 options를 쓸 때 본다. 자리는 설정 표의 weight1~weight11이 0보다 큰 칸이다.
        private static string OptionList(int baseId)
        {
            MartialType type;
            SkillCatalog.Entry entry = EntryOf(baseId, out type);
            if (entry == null)
            {
                return "no skill " + baseId;
            }
            var sb = new StringBuilder(entry.Name + " type=" + (int)type);
            var items = g.conf.battleSkillPrefixValue.GetItems((int)type, baseId);
            for (int i = 0; items != null && i < items.Count; i++)
            {
                ConfBattleSkillPrefixValueItem item = items[i];
                int[] weights = { item.weight1, item.weight2, item.weight3, item.weight4, item.weight5, item.weight6,
                    item.weight7, item.weight8, item.weight9, item.weight10, item.weight11 };
                var slots = new StringBuilder();
                for (int slot = 0; slot < weights.Length; slot++)
                {
                    if (weights[slot] > 0)
                    {
                        slots.Append(slots.Length == 0 ? "" : ",").Append(slot + 1);
                    }
                }
                sb.Append("\n#").Append(item.number).Append(" multi=").Append(item.multi).Append(" require=").Append(item.require)
                    .Append(" slots=").Append(slots).Append(' ').Append(MartialService.CleanDesc(GameTool.LS(item.desc)).Replace('\n', ' '));
            }
            return sb.ToString();
        }

        // 프리셋을 쓸 때 대조할 표를 파일로 내보낸다(.devremote\dump-<이름>.txt). 이름은 fates, skills, opts이거나 g.conf의 표 이름이다.
        private static string Dump(string what)
        {
            var sb = new StringBuilder();
            if (what == "fates")
            {
                var all = g.conf.fateFeature._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(all[i].id);
                    if (conf != null)
                    {
                        sb.Append(conf.id).Append('\t').Append(Name(conf.name)).Append("\tg").Append(all[i].gradeMin).Append('-').Append(all[i].gradeMax)
                            .Append("\tgroup").Append(all[i].group).Append("\tlv").Append(conf.level).Append('\t')
                            .Append(MartialService.CleanDesc(GameTool.LS(conf.tips)).Replace('\n', ' ')).Append('\n');
                    }
                }
            }
            else if (what == "skills" || what == "opts")
            {
                for (int kind = 0; kind < SkillCatalog.Kinds.Length; kind++)
                {
                    foreach (SkillCatalog.Entry entry in SkillCatalog.Find(kind, ""))
                    {
                        if (what == "skills")
                        {
                            sb.Append(kind).Append('\t').Append(entry.BaseId).Append('\t').Append(entry.Name).Append("\tg").Append(entry.GradeMin).Append('-')
                                .Append(entry.GradeMax).Append('\t').Append((entry.Desc ?? "").Replace('\n', ' ')).Append('\n');
                        }
                        else
                        {
                            sb.Append("== ").Append(entry.BaseId).Append(' ').Append(OptionList(entry.BaseId)).Append('\n');
                        }
                    }
                }
            }
            else
            {
                // 표의 줄마다 정수·실수·글자 속성을 모두 적는다. 글자는 게임의 번역을 거친다.
                System.Reflection.PropertyInfo table = typeof(ConfMgr).GetProperty(what);
                object conf = table == null ? null : table.GetValue(g.conf, null);
                System.Reflection.PropertyInfo listProperty = conf == null ? null : conf.GetType().GetProperty("_allConfList");
                object list = listProperty == null ? null : listProperty.GetValue(conf, null);
                if (list == null)
                {
                    return "unknown table " + what;
                }
                int count = (int)list.GetType().GetProperty("Count").GetValue(list, null);
                System.Reflection.PropertyInfo item = list.GetType().GetProperty("Item");
                for (int i = 0; i < count; i++)
                {
                    object row = item.GetValue(list, new object[] { i });
                    foreach (System.Reflection.PropertyInfo property in row.GetType().GetProperties(System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.Instance))
                    {
                        Type type = property.PropertyType;
                        if (type != typeof(int) && type != typeof(float) && type != typeof(string))
                        {
                            continue;
                        }
                        object value = property.GetValue(row, null);
                        string text = value == null ? "" : value.ToString();
                        if (type == typeof(string) && text.Length > 0)
                        {
                            // 번역이 없는 글자(번호를 이은 값 등)는 원문 그대로 적는다.
                            string shown = MartialService.CleanDesc(GameTool.LS(text)).Replace('\n', ' ');
                            text = shown.Length == 0 ? text : shown;
                        }
                        sb.Append(property.Name).Append('=').Append(text).Append('\t');
                    }
                    sb.Append('\n');
                }
            }
            string path = System.IO.Path.Combine(DevCommands.Dir, "dump-" + what + ".txt");
            System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return "dumped=" + what + " chars=" + sb.Length;
        }
    }
}
