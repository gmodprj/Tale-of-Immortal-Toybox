using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 법보·기령 탭의 자료와 조작(스펙 docs/superpowers/specs/2026-10-17-treasure-tab-design.md, 05 문서 56절).
    // 게임 자료를 읽어 줄로 만들고, 게임의 행동(UnitAction*)과 기령 자료의 메서드로 바꾼 뒤 다시 읽어 확인한다. 문장을 돌려주고 변경 이력에 남긴다.
    internal static class TreasureService
    {
        public sealed class Treasure
        {
            public DataProps.PropsData Props;
            public DataProps.PropsArtifact Data;
            public string SoleId;
            public int ItemId;
            public string Name;
            public int Quality;
            public int Grade;
            public int Durable;
            public int DurableMax;
            public bool Equipped;
            public int SpiritSole;      // 든 기령의 고유 번호. 0이면 없음
            public string SpiritText;   // "이름 Lv n" 또는 "없음"
            public int Level;           // 단계 1~6(PropsArtifact.level)
            public int[] Rates;         // 비율 다섯: 체력·공격·방어·혼력·집중력
            public int Prefixes;        // 붙은 옵션 수
            public int PrefixMax;       // 모양 표의 최대 옵션 수
        }

        public sealed class Spirit
        {
            public DataUnit.ArtifactSpriteData.Sprite Data;
            public int SoleId;
            public int SpriteId;
            public string Name;
            public int Level;
            public int MaxLevel;
            public int Exp;
            public int NextExp;         // 다음 레벨의 경험치. 최고 레벨이면 -1
            public int Lit;             // 켜진 보이는 재능
            public int Shown;           // 보이는 재능의 수
            public string InName;       // 든 법보의 이름. 없으면 null
            public string InSole;       // 든 법보의 soleID. 없으면 null
            public int Intimacy;
            public int IntimacyMax;
        }

        public sealed class Kind
        {
            public int SpriteId;
            public string Name;
            public int Quality;
            public bool Owned;
        }

        public sealed class ItemKind
        {
            public int ItemId;
            public string Name;
            public int Quality;
            public int InitGrade;
            public int Owned;
        }

        private const string Field = "법보";
        private static Dictionary<int, List<KeyValuePair<int, int>>> levels;   // 기령 번호 → (레벨, 경험치) 오름차순

        public static bool Ready(WorldUnitBase unit)
        {
            return unit != null && unit.data != null && unit.data.unitData != null && unit.data.unitData.artifactSpriteData != null;
        }

        private static string Name(WorldUnitBase unit)
        {
            return EditService.NameOf(unit);
        }

        // ---- 읽기 ----

        public static List<Treasure> Treasures(WorldUnitBase unit)
        {
            var result = new List<Treasure>();
            if (!Ready(unit))
            {
                return result;
            }
            var data = unit.data.unitData;
            var all = data.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                DataProps.PropsData props = all[i];
                if (props == null || !SpriteService.IsArtifact(props.propsID))
                {
                    continue;
                }
                DataProps.PropsArtifact art = props.To<DataProps.PropsArtifact>();
                ConfItemPropsItem conf = g.conf.itemProps.GetItem(props.propsID);
                ConfArtifactShapeItem shape = g.conf.artifactShape.GetItem(props.propsID);
                var t = new Treasure
                {
                    Props = props,
                    Data = art,
                    SoleId = props.soleID,
                    ItemId = props.propsID,
                    Name = SpriteService.ArtifactName(props.propsID),
                    Quality = conf == null ? 0 : conf.level,
                    Grade = art == null ? 0 : art.grade,
                    Durable = art == null ? 0 : art.durable,
                    DurableMax = shape == null ? 0 : shape.durable,
                    Equipped = data.IsEquipProps(props.soleID),
                    SpiritSole = art == null ? 0 : art.spriteSoleID,
                    Level = art == null ? 0 : art.level,
                    Rates = art == null ? new int[5] : new[] { art.hpRate, art.atkRate, art.defRate, art.soulCapcityRate, art.equipSpRate },
                    PrefixMax = shape == null ? 0 : shape.perfixMax,
                };
                try
                {
                    var prefixes = art == null ? null : art.GetPrefixs();
                    t.Prefixes = prefixes == null ? 0 : prefixes.Count;
                }
                catch (Exception)
                {
                }
                DataUnit.ArtifactSpriteData.Sprite inside = t.SpiritSole == 0 ? null : data.artifactSpriteData.GetSpriteInSoleID(t.SpiritSole);
                t.SpiritText = inside == null ? (t.SpiritSole == 0 ? "없음" : "? (" + t.SpiritSole + ")") : SpriteService.SpriteName(inside.spriteID) + " Lv" + inside.level;
                result.Add(t);
            }
            return result;
        }

        public static List<Spirit> Spirits(WorldUnitBase unit)
        {
            var result = new List<Spirit>();
            if (!Ready(unit))
            {
                return result;
            }
            var sprites = unit.data.unitData.artifactSpriteData;
            var where = new Dictionary<int, Treasure>();
            foreach (Treasure t in Treasures(unit))
            {
                if (t.SpiritSole != 0 && !where.ContainsKey(t.SpiritSole))
                {
                    where[t.SpiritSole] = t;
                }
            }
            for (int i = 0; sprites.sprites != null && i < sprites.sprites.Count; i++)
            {
                DataUnit.ArtifactSpriteData.Sprite sp = sprites.sprites[i];
                if (sp == null)
                {
                    continue;
                }
                var s = new Spirit
                {
                    Data = sp,
                    SoleId = sp.soleID,
                    SpriteId = sp.spriteID,
                    Name = SpriteService.SpriteName(sp.spriteID),
                    Level = sp.level,
                    Exp = sp.exp,
                    Intimacy = sp.intimacy,
                    IntimacyMax = CloseMaxValue(),
                };
                s.MaxLevel = MaxLevel(sp.spriteID, sp.level);
                s.NextExp = NextExp(sp.spriteID, sp.level);
                for (int k = 0; sp.talents != null && k < sp.talents.Count; k++)
                {
                    if (SpriteService.Shown(sp.spriteID, sp.talents[k].number))
                    {
                        s.Shown++;
                        if (sp.talents[k].state != 0)
                        {
                            s.Lit++;
                        }
                    }
                }
                Treasure holder;
                if (where.TryGetValue(sp.soleID, out holder))
                {
                    s.InName = holder.Name;
                    s.InSole = holder.SoleId;
                }
                result.Add(s);
            }
            return result;
        }

        // 32종. query는 이름·번호의 일부(비면 전부).
        public static List<Kind> Kinds(WorldUnitBase unit, string query)
        {
            var owned = new HashSet<int>();
            foreach (Spirit s in Spirits(unit))
            {
                owned.Add(s.SpriteId);
            }
            string needle = EditService.Compact(query ?? "");
            var result = new List<Kind>();
            var all = g.conf.artifactSprite._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfArtifactSpriteItem conf = all[i];
                if (conf == null)
                {
                    continue;
                }
                string name = SpriteService.SpriteName(conf.id);
                if (needle.Length > 0 && !EditService.Compact(name).Contains(needle) && !conf.id.ToString().Contains(needle))
                {
                    continue;
                }
                result.Add(new Kind { SpriteId = conf.id, Name = name, Quality = conf.level, Owned = owned.Contains(conf.id) });
            }
            return result;
        }

        // 열 종의 법보 아이템(분류 401)과 가진 수.
        public static List<ItemKind> ItemKinds(WorldUnitBase unit)
        {
            var result = new List<ItemKind>();
            var all = g.conf.artifactShape._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfArtifactShapeItem shape = all[i];
                if (shape == null || !SpriteService.IsArtifact(shape.id))
                {
                    continue;
                }
                ConfItemPropsItem conf = g.conf.itemProps.GetItem(shape.id);
                int owned = 0;
                if (Ready(unit))
                {
                    owned = unit.data.unitData.propData.GetPropsNum(shape.id);
                }
                result.Add(new ItemKind { ItemId = shape.id, Name = SpriteService.ArtifactName(shape.id), Quality = conf == null ? 0 : conf.level, InitGrade = shape.initGrade, Owned = owned });
            }
            return result;
        }

        private static List<KeyValuePair<int, int>> Levels(int spriteId)
        {
            if (levels == null)
            {
                levels = new Dictionary<int, List<KeyValuePair<int, int>>>();
                var all = g.conf.artifactSpriteLevel._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    ConfArtifactSpriteLevelItem row = all[i];
                    List<KeyValuePair<int, int>> list;
                    if (!levels.TryGetValue(row.spriteID, out list))
                    {
                        list = new List<KeyValuePair<int, int>>();
                        levels[row.spriteID] = list;
                    }
                    list.Add(new KeyValuePair<int, int>(row.level, row.exp));
                }
                foreach (var list in levels.Values)
                {
                    list.Sort((a, b) => a.Key.CompareTo(b.Key));
                }
            }
            List<KeyValuePair<int, int>> found;
            return levels.TryGetValue(spriteId, out found) ? found : new List<KeyValuePair<int, int>>();
        }

        private static int MaxLevel(int spriteId, int level)
        {
            int max = level;
            foreach (var pair in Levels(spriteId))
            {
                if (pair.Key > max)
                {
                    max = pair.Key;
                }
            }
            return max;
        }

        private static int NextExp(int spriteId, int level)
        {
            foreach (var pair in Levels(spriteId))
            {
                if (pair.Key == level + 1)
                {
                    return pair.Value;
                }
            }
            return -1;
        }

        public static Treasure Find(WorldUnitBase unit, string soleId)
        {
            foreach (Treasure t in Treasures(unit))
            {
                if (t.SoleId == soleId)
                {
                    return t;
                }
            }
            return null;
        }

        public static Spirit FindSpirit(WorldUnitBase unit, int soleId)
        {
            foreach (Spirit s in Spirits(unit))
            {
                if (s.SoleId == soleId)
                {
                    return s;
                }
            }
            return null;
        }

        private static void Update(WorldUnitBase unit)
        {
            try
            {
                unit.UpdateConf();
            }
            catch (Exception)
            {
            }
        }

        // ---- 조작 ----

        public static string Equip(WorldUnitBase unit, string soleId)
        {
            Treasure t = Find(unit, soleId);
            if (t == null)
            {
                return "법보: 그 법보가 없습니다.";
            }
            if (t.Equipped)
            {
                return "법보 " + t.Name + ": 이미 장착 중입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            // 34절 실측: 가능 여부를 먼저 물은 뒤 행동을 만들어야 장착된다. 다른 법보가 장착돼 있으면 게임이 바꿔 준다.
            var action = new UnitActionPropEquip(t.SoleId, SpriteService.ArtifactSlot);
            unit.IsCreateAction(action, false);
            unit.CreateAction(action, false);
            Update(unit);
            bool ok = unit.data.unitData.IsEquipProps(t.SoleId);
            ChangeLog.Add(Name(unit), Field, "-", t.Name + " 장착", ok);
            return ok ? "법보: " + t.Name + "을(를) 장착했습니다." : "법보 " + t.Name + ": 게임이 장착하지 않았습니다.";
        }

        // 수리: 지금 내구를 법보 모양 설정의 내구(ConfArtifactShapeItem.durable)까지 채운다. 게임의 수리 화면(UIArtifactShapeRepair)이 재료로 올리는 값과 같은 칸이다(05 문서 60절).
        public static string Repair(WorldUnitBase unit, string soleId)
        {
            Treasure t = Find(unit, soleId);
            if (t == null)
            {
                return "법보: 그 법보가 없습니다.";
            }
            string why = RepairBlock(t);
            if (why != null)
            {
                return "법보 " + t.Name + ": " + why;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            return RepairOne(unit, t);
        }

        // 내구가 모자란 법보를 모두 수리한다.
        public static string RepairAll(WorldUnitBase unit)
        {
            List<Treasure> worn = Treasures(unit).FindAll(t => RepairBlock(t) == null);
            if (worn.Count == 0)
            {
                return "법보: 수리할 법보가 없습니다(모두 내구가 가득합니다).";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int ok = 0;
            var failed = new List<string>();
            foreach (Treasure t in worn)
            {
                string r = RepairOne(unit, t);
                if (r.StartsWith("법보: "))
                {
                    ok++;
                }
                else
                {
                    failed.Add(t.Name);
                }
            }
            return "법보: " + ok + "개를 수리했습니다." + (failed.Count > 0 ? " 고치지 못함 " + failed.Count + "(" + string.Join(", ", failed) + ")." : "");
        }

        // 수리할 수 없는 까닭. 할 수 있으면 null(탭의 단추를 켜고 끄는 데도 쓴다).
        public static string RepairBlock(Treasure t)
        {
            if (t.Data == null)
            {
                return "법보 자료를 읽지 못했습니다.";
            }
            if (t.DurableMax <= 0)
            {
                return "최대 내구를 알 수 없습니다.";
            }
            return t.Durable >= t.DurableMax ? "내구가 이미 가득합니다(" + t.Durable + "/" + t.DurableMax + ")." : null;
        }

        private static string RepairOne(WorldUnitBase unit, Treasure t)
        {
            int before = t.Durable;
            t.Data.durable = t.DurableMax;
            Update(unit);
            Treasure after = Find(unit, t.SoleId);
            bool ok = after != null && after.Durable == t.DurableMax;
            ChangeLog.Add(Name(unit), Field, t.Name + " 내구 " + before, (after == null ? "?" : after.Durable.ToString()) + "/" + t.DurableMax, ok);
            return ok ? "법보: " + t.Name + "을(를) 수리했습니다(내구 " + before + " -> " + t.DurableMax + ")."
                : "법보 " + t.Name + ": 내구를 채우지 못했습니다(" + (after == null ? "?" : after.Durable.ToString()) + "/" + t.DurableMax + ").";
        }

        public static string Unequip(WorldUnitBase unit, string soleId)
        {
            Treasure t = Find(unit, soleId);
            if (t == null)
            {
                return "법보: 그 법보가 없습니다.";
            }
            if (!t.Equipped)
            {
                return "법보 " + t.Name + ": 장착 중이 아닙니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            var action = new UnitActionPropUnequip(SpriteService.ArtifactSlot);
            unit.IsCreateAction(action, false);
            unit.CreateAction(action, false);
            Update(unit);
            bool ok = !unit.data.unitData.IsEquipProps(t.SoleId);
            ChangeLog.Add(Name(unit), Field, "-", t.Name + " 해제", ok);
            return ok ? "법보: " + t.Name + "을(를) 해제했습니다." : "법보 " + t.Name + ": 게임이 해제하지 않았습니다.";
        }

        // 든 기령을 뺀다. 게임의 행동(기령 번호 0으로 넣기)을 먼저 쓰고, 안 되면 자료를 직접 지운다(스펙 2절. 어느 쪽이 됐는지 문장에 남긴다).
        public static string PullSpirit(WorldUnitBase unit, string soleId)
        {
            Treasure t = Find(unit, soleId);
            if (t == null)
            {
                return "법보: 그 법보가 없습니다.";
            }
            if (t.SpiritSole == 0)
            {
                return "법보 " + t.Name + ": 든 기령이 없습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string spirit = t.SpiritText;
            string how = "행동";
            try
            {
                unit.CreateAction(new UnitActionEquipSprite(t.Data, 0), false);
            }
            catch (Exception)
            {
            }
            if (t.Data.spriteSoleID != 0)
            {
                t.Data.spriteSoleID = 0;
                how = "직접";
            }
            Update(unit);
            bool ok = t.Data.spriteSoleID == 0;
            ChangeLog.Add(Name(unit), Field, spirit, t.Name + "에서 기령 뺌(" + how + ")", ok);
            return ok ? "법보: " + t.Name + "에서 " + spirit + "을(를) 뺐습니다(" + how + "). 기령은 남아 있습니다." : "법보 " + t.Name + ": 기령을 빼지 못했습니다.";
        }

        // 장착한 빈 법보에, 어디에도 들어 있지 않은 기령을 넣는다(34절에서 실측한 순서: 장착 → 넣기).
        public static string PutSpirit(WorldUnitBase unit, string soleId, int spiritSole)
        {
            Treasure t = Find(unit, soleId);
            Spirit s = FindSpirit(unit, spiritSole);
            if (t == null || s == null)
            {
                return "법보: 그 법보나 기령이 없습니다.";
            }
            if (!t.Equipped)
            {
                return "법보 " + t.Name + ": 장착한 법보에만 기령을 넣습니다.";
            }
            if (t.SpiritSole != 0)
            {
                return "법보 " + t.Name + ": 이미 기령(" + t.SpiritText + ")이 들어 있습니다. 먼저 빼세요.";
            }
            if (s.InSole != null)
            {
                return "기령 " + s.Name + ": 이미 " + s.InName + "에 들어 있습니다. 먼저 빼세요.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            unit.CreateAction(new UnitActionEquipSprite(t.Data, s.SoleId), false);
            Update(unit);
            bool ok = t.Data.spriteSoleID == s.SoleId;
            ChangeLog.Add(Name(unit), Field, "-", t.Name + "에 " + s.Name + " 넣음", ok);
            return ok ? "법보: " + t.Name + "에 " + s.Name + "을(를) 넣었습니다." : "법보 " + t.Name + ": 게임이 기령을 넣지 않았습니다.";
        }

        public static string LightTalents(WorldUnitBase unit, int spiritSole)
        {
            Spirit s = FindSpirit(unit, spiritSole);
            if (s == null)
            {
                return "기령: 그 기령이 없습니다.";
            }
            if (s.Lit >= s.Shown)
            {
                return "기령 " + s.Name + ": 보이는 재능 " + s.Shown + "개가 모두 켜져 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            var sprites = unit.data.unitData.artifactSpriteData;
            int lit = 0;
            for (int k = 0; s.Data.talents != null && k < s.Data.talents.Count; k++)
            {
                var talent = s.Data.talents[k];
                if (talent.state == 0 && SpriteService.Shown(s.SpriteId, talent.number))
                {
                    sprites.ActiveTanlet(s.SoleId, talent.number);
                    lit++;
                }
            }
            Update(unit);
            Spirit after = FindSpirit(unit, spiritSole);
            bool ok = after != null && after.Lit >= after.Shown;
            ChangeLog.Add(Name(unit), Field, s.Lit + "/" + s.Shown, s.Name + " 재능 " + lit + "개 켬", ok);
            return ok ? "기령: " + s.Name + "의 재능 " + lit + "개를 켰습니다(" + after.Lit + "/" + after.Shown + ")."
                : "기령 " + s.Name + ": 재능을 켰지만 " + (after == null ? "?" : after.Lit + "/" + after.Shown) + "입니다. 저장하지 말고 확인하세요.";
        }

        // 다음 레벨의 경험치까지 채운다(AddExp). 레벨이 오르지 않으면 1을 더 넣어 본다. 결과 문장에 레벨과 경험치를 적는다.
        public static string LevelUp(WorldUnitBase unit, int spiritSole)
        {
            Spirit s = FindSpirit(unit, spiritSole);
            if (s == null)
            {
                return "기령: 그 기령이 없습니다.";
            }
            if (s.NextExp < 0)
            {
                return "기령 " + s.Name + ": 최고 레벨(" + s.Level + ")입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int add = Math.Max(0, s.NextExp - s.Exp);
            s.Data.AddExp(add);
            if (s.Data.level <= s.Level)
            {
                s.Data.AddExp(1);
                add++;
            }
            Update(unit);
            Spirit after = FindSpirit(unit, spiritSole);
            bool ok = after != null && after.Level > s.Level;
            ChangeLog.Add(Name(unit), Field, s.Name + " Lv" + s.Level + " " + s.Exp, "Lv" + (after == null ? "?" : after.Level.ToString()) + " " + (after == null ? "?" : after.Exp.ToString()) + " (+" + add + ")", ok);
            return ok ? "기령: " + s.Name + " Lv" + s.Level + " → Lv" + after.Level + "(경험치 " + after.Exp + ")."
                : "기령 " + s.Name + ": 경험치 " + add + "을(를) 넣었지만 레벨이 오르지 않았습니다(" + (after == null ? "?" : "Lv" + after.Level + " " + after.Exp) + ").";
        }

        public static string Delete(WorldUnitBase unit, int spiritSole)
        {
            Spirit s = FindSpirit(unit, spiritSole);
            if (s == null)
            {
                return "기령: 그 기령이 없습니다.";
            }
            if (s.InSole != null)
            {
                return "기령 " + s.Name + ": " + s.InName + "에 들어 있어 지우지 않습니다. 먼저 빼세요.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            unit.data.unitData.artifactSpriteData.DelSpriteInSoleID(s.SoleId);
            Update(unit);
            bool ok = FindSpirit(unit, spiritSole) == null;
            ChangeLog.Add(Name(unit), Field, s.Name + " Lv" + s.Level, "지움", ok);
            return ok ? "기령: " + s.Name + "을(를) 지웠습니다." : "기령 " + s.Name + ": 게임이 지우지 않았습니다.";
        }

        public static string Create(WorldUnitBase unit, int spriteId)
        {
            if (!Ready(unit))
            {
                return "기령: 이 인물에게 기령 자료가 없습니다.";
            }
            if (!SpriteService.Known(spriteId))
            {
                return "기령 " + spriteId + ": 게임에 없는 번호입니다.";
            }
            string name = SpriteService.SpriteName(spriteId);
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            DataUnit.ArtifactSpriteData.Sprite made = unit.data.unitData.artifactSpriteData.AddSprite(spriteId);
            Update(unit);
            bool ok = made != null;
            ChangeLog.Add(Name(unit), Field, "-", name + " 만듦", ok);
            return ok ? "기령: " + name + "을(를) 만들었습니다(Lv" + made.level + "). 장착한 빈 법보에 넣을 수 있습니다."
                : "기령 " + name + ": 게임이 만들지 않았습니다(같은 기령은 하나만 가질 수 있습니다).";
        }

        // ---- 트레이너 3차: 경지·단계·친밀도(스펙 2026-10-23-trainer3-design.md 6절) ----

        // 친밀도 상한(표 artifactSpriteClose.closeMax, 실측 900). 읽지 못하면 0.
        public static int CloseMaxValue()
        {
            try
            {
                return g.conf.artifactSpriteClose.closeMax;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static string GradeLabel(int grade)
        {
            return grade >= 1 && grade <= 10 ? ArtifactService.MajorName(grade) : grade.ToString();
        }

        public static string RatesText(int[] rates)
        {
            return string.Join("/", Array.ConvertAll(rates, r => r.ToString()));
        }

        // 경지 한 칸. 게임 행동(UnitActionPropArtifactShapeUpgrade)을 쓰고, 게임이 깎는 비율은 되돌린다. 10에서는 부르지 않는다(E3: 숫자가 오른 뒤 예외).
        public static string GradeUp(WorldUnitBase unit, string soleId)
        {
            Treasure t = Find(unit, soleId);
            if (t == null || t.Data == null)
            {
                return "법보: 그 법보가 없습니다.";
            }
            if (t.Grade >= TreasureRules.MaxGrade)
            {
                return "법보 " + t.Name + ": 이미 경지 " + TreasureRules.MaxGrade + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            return GradeUpOne(unit, t);
        }

        private static string GradeUpOne(WorldUnitBase unit, Treasure t)
        {
            int[] rates = (int[])t.Rates.Clone();
            string error = null;
            try
            {
                unit.CreateAction(new UnitActionPropArtifactShapeUpgrade(t.Data), false);
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            Treasure after = Find(unit, t.SoleId);
            if (after != null && after.Data != null && after.Grade > t.Grade)
            {
                after.Data.SetAttrrRate(rates[0], rates[1], rates[2], rates[3], rates[4], false);
            }
            Update(unit);
            after = Find(unit, t.SoleId);
            bool ok = error == null && after != null && after.Grade == t.Grade + 1;
            ChangeLog.Add(Name(unit), Field, t.Name + " 경지 " + t.Grade + " 옵션 " + t.Prefixes,
                "경지 " + (after == null ? "?" : after.Grade.ToString()) + " 옵션 " + (after == null ? "?" : after.Prefixes.ToString()), ok);
            return ok ? "법보: " + t.Name + " 경지 " + GradeLabel(t.Grade) + " → " + GradeLabel(after.Grade) + ", 옵션 " + t.Prefixes + " → " + after.Prefixes + "칸."
                : "법보 " + t.Name + ": 경지를 올리지 못했습니다(" + (error ?? ("경지 " + (after == null ? "?" : after.Grade.ToString()))) + ").";
        }

        // target(10을 넘으면 10)까지 되풀이한다. 한 번이라도 실패하면 멈춘다.
        public static string GradeUpTo(WorldUnitBase unit, string soleId, int target)
        {
            Treasure t = Find(unit, soleId);
            if (t == null || t.Data == null)
            {
                return "법보: 그 법보가 없습니다.";
            }
            int steps = TreasureRules.GradeSteps(t.Grade, target);
            if (steps == 0)
            {
                return t.Grade >= TreasureRules.MaxGrade ? "법보 " + t.Name + ": 이미 경지 " + TreasureRules.MaxGrade + "입니다."
                    : "법보 " + t.Name + ": 이미 경지 " + GradeLabel(t.Grade) + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int start = t.Grade;
            for (int i = 0; i < steps; i++)
            {
                Treasure now = Find(unit, soleId);
                string r = GradeUpOne(unit, now);
                if (!r.StartsWith("법보: "))
                {
                    Treasure stop = Find(unit, soleId);
                    return r + " " + GradeLabel(start) + "에서 " + GradeLabel(stop == null ? start : stop.Grade) + "까지 올리고 멈췄습니다.";
                }
            }
            Treasure end = Find(unit, soleId);
            return "법보: " + t.Name + " 경지 " + GradeLabel(start) + " → " + GradeLabel(end.Grade) + ", 옵션 " + t.Prefixes + " → " + end.Prefixes + "칸.";
        }

        // 비율 다섯을 그 단계의 대표 값으로 맞춘다(6 = 한계돌파 120).
        public static string SetStage(WorldUnitBase unit, string soleId, int stage)
        {
            Treasure t = Find(unit, soleId);
            if (t == null || t.Data == null)
            {
                return "법보: 그 법보가 없습니다.";
            }
            if (stage < TreasureRules.MinStage || stage > TreasureRules.MaxStage)
            {
                return "법보: 단계는 1~6입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            return SetStageNoBackup(unit, t, stage);
        }

        private static string SetStageNoBackup(WorldUnitBase unit, Treasure t, int stage)
        {
            int r = TreasureRules.StageRate(stage);
            t.Data.SetAttrrRate(r, r, r, r, r, true);
            Update(unit);
            Treasure after = Find(unit, t.SoleId);
            bool ok = after != null && after.Level == stage;
            ChangeLog.Add(Name(unit), Field, t.Name + " 단계 " + t.Level + " 비율 " + RatesText(t.Rates),
                "단계 " + (after == null ? "?" : after.Level.ToString()) + " 비율 " + (after == null ? "?" : RatesText(after.Rates)), ok);
            return ok ? "법보: " + t.Name + " 단계 " + t.Level + " → " + stage + "(비율 " + r + ")."
                : "법보 " + t.Name + ": 단계를 " + stage + "(으)로 맞추지 못했습니다(지금 " + (after == null ? "?" : after.Level.ToString()) + ").";
        }

        public static string CloseMax(WorldUnitBase unit, int spiritSole)
        {
            Spirit s = FindSpirit(unit, spiritSole);
            if (s == null)
            {
                return "기령: 그 기령이 없습니다.";
            }
            int max = CloseMaxValue();
            if (max <= 0)
            {
                return "기령 " + s.Name + ": 친밀도 상한을 읽지 못했습니다.";
            }
            if (s.Intimacy >= max)
            {
                return "기령 " + s.Name + ": 이미 친밀도 최대(" + max + ")입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            s.Data.intimacy = max;
            Update(unit);
            Spirit after = FindSpirit(unit, spiritSole);
            bool ok = after != null && after.Intimacy == max;
            ChangeLog.Add(Name(unit), "기령 친밀도", s.Name + " " + s.Intimacy, after == null ? "?" : after.Intimacy.ToString(), ok);
            return ok ? "기령: " + s.Name + " 친밀도 " + s.Intimacy + " → " + max + "."
                : "기령 " + s.Name + ": 친밀도를 쓰지 못했습니다.";
        }

        public static string Give(WorldUnitBase unit, int itemId)
        {
            if (!SpriteService.IsArtifact(itemId))
            {
                return "법보 " + itemId + ": 게임의 법보가 아닙니다.";
            }
            return EditService.GiveItem(unit, itemId, 1);
        }
    }
}
