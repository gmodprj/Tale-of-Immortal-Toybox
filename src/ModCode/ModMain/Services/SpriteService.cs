using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 법보와 기령. 게임의 획득 경로가 아니라 데이터에 직접 넣는다. 화면을 모르고 결과를 문장으로 돌려준다.
    // 실측(프리셋 스펙 3절): AddSprite로 기령을 만들고, 법보 아이템을 지급해 UnitActionPropEquip으로 장착하고,
    // UnitActionEquipSprite로 법보에 넣는다. 재능은 ActiveTanlet으로 켠다.
    internal static class SpriteService
    {
        public const int FirstGrade = 2;        // 프리셋이 법보와 기령을 넣기 시작하는 대경지(법보 3031101의 최저 경지가 축기다)
        private const int ArtifactClass = 401;  // ItemProps.className: 법보
        internal const int ArtifactSlot = 2;    // UnitActionPropEquip·UnitActionPropUnequip의 칸 번호. 실측에서 쓴 값이다(TreasureService도 쓴다)

        private static Dictionary<int, bool> shown;   // 기령 번호 * 1000 + 재능 번호 → 게임 화면에 보이는 재능인가

        public static bool Known(int spriteId)
        {
            return g.conf.artifactSprite.GetItem(spriteId) != null;
        }

        public static bool IsArtifact(int itemId)
        {
            ConfItemPropsItem conf = g.conf.itemProps.GetItem(itemId);
            return conf != null && conf.className == ArtifactClass;
        }

        public static string SpriteName(int spriteId)
        {
            ConfArtifactSpriteItem conf = g.conf.artifactSprite.GetItem(spriteId);
            return conf == null ? spriteId.ToString() : EditService.Compact(GameTool.LS(conf.name));
        }

        public static string ArtifactName(int itemId)
        {
            return EditService.ItemName(itemId) ?? itemId.ToString();
        }

        // 설정 표의 isShow가 1인 재능만 게임 화면에 보인다(기령 1002는 53개 가운데 49개). 보이지 않는 것까지 켜면 화면에 "53/49"로 넘친다(실측).
        internal static bool Shown(int spriteId, int number)
        {
            if (shown == null)
            {
                shown = new Dictionary<int, bool>();
                var all = g.conf.artifactSpriteTalent._allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    shown[all[i].spriteID * 1000 + all[i].number] = all[i].isShow == 1;
                }
            }
            bool value;
            return shown.TryGetValue(spriteId * 1000 + number, out value) && value;
        }

        // 가진 법보들. itemId가 0이면 전부, 아니면 그 번호의 것만.
        private static List<DataProps.PropsData> Artifacts(WorldUnitBase unit, int itemId)
        {
            var result = new List<DataProps.PropsData>();
            var all = unit.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (itemId == 0 ? IsArtifact(all[i].propsID) : all[i].propsID == itemId)
                {
                    result.Add(all[i]);
                }
            }
            return result;
        }

        private static int SpriteSoleIn(DataProps.PropsData artifact)
        {
            return artifact.To<DataProps.PropsArtifact>().spriteSoleID;
        }

        private static DataUnit.ArtifactSpriteData.Sprite SpriteIn(WorldUnitBase unit, DataProps.PropsData artifact)
        {
            int sole = SpriteSoleIn(artifact);
            var sprites = unit.data.unitData.artifactSpriteData;
            return sole == 0 || sprites == null ? null : sprites.GetSpriteInSoleID(sole);
        }

        // 꺼져 있는 보이는 재능의 번호들.
        private static List<int> DarkTalents(DataUnit.ArtifactSpriteData.Sprite sprite)
        {
            var numbers = new List<int>();
            for (int i = 0; sprite.talents != null && i < sprite.talents.Count; i++)
            {
                if (sprite.talents[i].state == 0 && Shown(sprite.spriteID, sprite.talents[i].number))
                {
                    numbers.Add(sprite.talents[i].number);
                }
            }
            return numbers;
        }

        // 장착한 법보가 그 번호이고, 거기에 그 번호의 기령이 들어 있고, 보이는 재능이 모두 켜져 있는가.
        public static bool Has(WorldUnitBase unit, int spriteId, int artifactId)
        {
            foreach (DataProps.PropsData artifact in Artifacts(unit, artifactId))
            {
                if (!unit.data.unitData.IsEquipProps(artifact.soleID))
                {
                    continue;
                }
                DataUnit.ArtifactSpriteData.Sprite sprite = SpriteIn(unit, artifact);
                return sprite != null && sprite.spriteID == spriteId && DarkTalents(sprite).Count == 0;
            }
            return false;
        }

        // 그 번호의 법보에 그 번호의 기령을 넣어 장착하고 보이는 재능을 켠다. changed는 무엇이든 바꿨는지다.
        public static string Apply(WorldUnitBase unit, int spriteId, int artifactId, out bool changed)
        {
            changed = false;
            if (!Known(spriteId))
            {
                return "기령 " + spriteId + ": 게임에 없는 번호입니다.";
            }
            if (!IsArtifact(artifactId))
            {
                return "법보 " + artifactId + ": 게임의 법보가 아닙니다.";
            }
            string label = ArtifactName(artifactId) + " + " + SpriteName(spriteId);
            var data = unit.data.unitData;
            var sprites = data.artifactSpriteData;
            if (sprites == null)
            {
                return "법보와 기령: 이 인물에게 기령 자료가 없습니다.";
            }
            if (Has(unit, spriteId, artifactId))
            {
                return "법보와 기령: 이미 " + label + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string name = EditService.NameOf(unit);

            // 1) 그 번호의 기령이 이미 들어 있는 그 번호의 법보가 있으면 그것을 쓴다.
            DataProps.PropsData target = null;
            DataUnit.ArtifactSpriteData.Sprite sprite = null;
            foreach (DataProps.PropsData each in Artifacts(unit, artifactId))
            {
                DataUnit.ArtifactSpriteData.Sprite inside = SpriteIn(unit, each);
                if (inside != null && inside.spriteID == spriteId)
                {
                    target = each;
                    sprite = inside;
                    break;
                }
            }
            if (target == null)
            {
                // 그 번호의 기령이 다른 법보에 들어 있으면 멈춘다. 게임은 같은 번호의 기령을 하나 더 만들지 않고(실측: AddSprite가 null),
                // 든 기령을 다른 법보에 넣으면 두 법보가 같은 기령을 가리킨다(실측). 옮기지 않는다.
                foreach (DataProps.PropsData each in Artifacts(unit, 0))
                {
                    DataUnit.ArtifactSpriteData.Sprite inside = SpriteIn(unit, each);
                    if (inside != null && inside.spriteID == spriteId)
                    {
                        return "법보와 기령 " + label + ": " + SpriteName(spriteId) + "은(는) 이미 다른 법보(" + ArtifactName(each.propsID)
                            + ")에 들어 있습니다. 기령을 옮기지 않으므로 적용하지 않았습니다.";
                    }
                }
                // 2) 기령이 비어 있는 그 번호의 법보. 없으면 지급한다.
                var had = new List<string>();
                foreach (DataProps.PropsData each in Artifacts(unit, artifactId))
                {
                    had.Add(each.soleID);
                    if (target == null && SpriteSoleIn(each) == 0)
                    {
                        target = each;
                    }
                }
                if (target == null)
                {
                    unit.data.RewardPropItem(artifactId, 1, false);
                    foreach (DataProps.PropsData each in Artifacts(unit, artifactId))
                    {
                        if (!had.Contains(each.soleID))
                        {
                            target = each;
                        }
                    }
                    if (target == null)
                    {
                        ChangeLog.Add(name, "법보와 기령", "-", label + " 지급 실패", false);
                        return "법보와 기령 " + label + ": 게임이 법보를 주지 않았습니다(가방이 가득 찼을 수 있습니다). 기령은 만들지 않았습니다.";
                    }
                    changed = true;
                }
            }
            // 법보를 먼저 장착하고 기령을 넣는다(실측한 순서다).
            if (!data.IsEquipProps(target.soleID))
            {
                // 다른 법보가 장착돼 있으면 게임이 바꿔 준다(실측). IsCreateAction은 3을 주지만 CreateAction은 장착한다.
                // 실측에서는 가능 여부를 먼저 묻고(IsCreateAction, 3을 준다) 행동을 만들었다. 묻지 않고 만들면 장착되지 않았다.
                var equip = new UnitActionPropEquip(target.soleID, ArtifactSlot);
                unit.IsCreateAction(equip, false);
                unit.CreateAction(equip, false);
                changed = true;
                if (!data.IsEquipProps(target.soleID))
                {
                    ChangeLog.Add(name, "법보와 기령", "-", label + " 장착 실패", false);
                    return "법보와 기령 " + label + ": 게임이 법보를 장착하지 않았습니다.";
                }
            }
            if (sprite == null)
            {
                // 3) 어느 법보에도 들어 있지 않은 그 번호의 기령. 없으면 만든다.
                var used = new List<int>();
                foreach (DataProps.PropsData each in Artifacts(unit, 0))
                {
                    int sole = SpriteSoleIn(each);
                    if (sole != 0)
                    {
                        used.Add(sole);
                    }
                }
                for (int i = 0; sprites.sprites != null && i < sprites.sprites.Count; i++)
                {
                    if (sprites.sprites[i].spriteID == spriteId && !used.Contains(sprites.sprites[i].soleID))
                    {
                        sprite = sprites.sprites[i];
                        break;
                    }
                }
                if (sprite == null)
                {
                    sprite = sprites.AddSprite(spriteId);
                }
                if (sprite == null)
                {
                    ChangeLog.Add(name, "법보와 기령", "-", label + " 기령 만들기 실패", false);
                    return "법보와 기령 " + label + ": 게임이 기령을 만들지 않았습니다.";
                }
                changed = true;
                unit.CreateAction(new UnitActionEquipSprite(target.To<DataProps.PropsArtifact>(), sprite.soleID), false);
                if (SpriteSoleIn(target) != sprite.soleID)
                {
                    ChangeLog.Add(name, "법보와 기령", "-", label + " 넣기 실패", false);
                    return "법보와 기령 " + label + ": 게임이 기령을 법보에 넣지 않았습니다. 저장하지 말고 확인하세요.";
                }
            }
            int lit = 0;
            foreach (int number in DarkTalents(sprite))
            {
                sprites.ActiveTanlet(sprite.soleID, number);
                lit++;
                changed = true;
            }
            try
            {
                unit.UpdateConf();
            }
            catch (Exception)
            {
            }
            bool ok = Has(unit, spriteId, artifactId);
            ChangeLog.Add(name, "법보와 기령", "-", label + (lit > 0 ? ", 재능 " + lit + "개 켬" : ""), ok);
            return ok ? "법보와 기령: " + label + "(재능 " + lit + "개 켬)" : "법보와 기령 " + label + ": 썼지만 상태가 예상과 다릅니다. 저장하지 말고 확인하세요.";
        }
    }
}
