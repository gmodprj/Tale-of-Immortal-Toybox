using System;

namespace MOD_sWP6UL
{
    // 명우. 게임은 특수 전투에서 고른 명우·효과·옵션을 unitData.reborn에 옮긴다. 편집기는 그 값을 직접 쓴다(프리셋 스펙 3절).
    internal static class WingmanService
    {
        // 게임에서 명우는 우화(대경지 9)에서 얻는다. 낮은 경지에서 써도 전투에는 나오지만(실측) 게임의 진행을 따른다.
        public const int FirstGrade = 9;

        public static bool Known(int id)
        {
            return g.conf.wingmanBase.GetItem(id) != null;
        }

        public static string Name(int id)
        {
            ConfWingmanBaseItem conf = g.conf.wingmanBase.GetItem(id);
            return conf == null ? id.ToString() : EditService.Compact(GameTool.LS(conf.name));
        }

        public static bool EffectKnown(int effectId)
        {
            return g.conf.wingmanEffect.GetItem(effectId) != null;
        }

        // 그 옵션이 그 명우의 것인가.
        public static bool PrefixOf(int prefixId, int wingmanId)
        {
            ConfWingmanFixValueItem conf = g.conf.wingmanFixValue.GetItem(prefixId);
            return conf != null && conf.wingManID == wingmanId;
        }

        public static int Current(WorldUnitBase unit)
        {
            var reborn = unit.data.unitData.reborn;
            return reborn == null ? 0 : reborn.wingManID;
        }

        private static bool Same(Il2CppSystem.Collections.Generic.List<int> have, int[] want)
        {
            int count = have == null ? 0 : have.Count;
            if (count != want.Length)
            {
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                if (have[i] != want[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static Il2CppSystem.Collections.Generic.List<int> ToList(int[] values)
        {
            var list = new Il2CppSystem.Collections.Generic.List<int>();
            foreach (int value in values)
            {
                list.Add(value);
            }
            return list;
        }

        public static bool Has(WorldUnitBase unit, int id, int[] effects, int[] prefixes)
        {
            var reborn = unit.data.unitData.reborn;
            return reborn != null && reborn.wingManID == id && Same(reborn.wingmanEffectList, effects) && Same(reborn.wingmanPrefixList, prefixes);
        }

        // 명우와 그 효과·옵션 목록을 쓴다. 지금 명우가 다른 것이면 바꾼다.
        public static string Set(WorldUnitBase unit, int id, int[] effects, int[] prefixes)
        {
            if (!Known(id))
            {
                return "명우 " + id + ": 게임에 없는 번호입니다.";
            }
            string label = Name(id);
            foreach (int effect in effects)
            {
                if (!EffectKnown(effect))
                {
                    return "명우 " + label + ": 게임에 없는 효과 번호입니다(" + effect + ").";
                }
            }
            foreach (int prefix in prefixes)
            {
                if (!PrefixOf(prefix, id))
                {
                    return "명우 " + label + ": 이 명우의 옵션이 아닙니다(" + prefix + ").";
                }
            }
            if (FateService.CurrentGrade(unit) < FirstGrade)
            {
                return "명우 " + label + ": 우화(대경지 9)부터 쓸 수 있습니다. 쓰지 않았습니다.";
            }
            var reborn = unit.data.unitData.reborn;
            if (reborn == null)
            {
                return "명우: 이 인물에게 명우 자료가 없습니다.";
            }
            if (Has(unit, id, effects, prefixes))
            {
                return "명우: 이미 " + label + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = reborn.wingManID == 0 ? "없음" : Name(reborn.wingManID);
            reborn.wingManID = id;
            reborn.isPassRebornBattle = true;
            // 명우를 얻은 적이 없는 인물은 두 목록이 null이다(실측). 새 목록을 넣는다.
            reborn.wingmanEffectList = ToList(effects);
            reborn.wingmanPrefixList = ToList(prefixes);
            if (reborn.wingmanPrefixUp == null)
            {
                reborn.wingmanPrefixUp = new Il2CppSystem.Collections.Generic.List<int>();
            }
            try
            {
                unit.UpdateConf();
            }
            catch (Exception)
            {
            }
            bool ok = Has(unit, id, effects, prefixes);
            ChangeLog.Add(EditService.NameOf(unit), "명우", before, label + "(효과 " + effects.Length + ", 옵션 " + prefixes.Length + ")", ok);
            return ok ? "명우: " + before + " -> " + label + "(효과 " + effects.Length + ", 옵션 " + prefixes.Length + ")"
                : "명우 " + label + ": 썼지만 상태가 예상과 다릅니다. 저장하지 말고 확인하세요.";
        }
    }
}
