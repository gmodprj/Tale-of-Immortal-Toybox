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
    }
}
