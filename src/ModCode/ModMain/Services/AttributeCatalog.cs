using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 편집할 수 있는 능력치 하나. 저장값(PropertyData), 파생값(WorldUnitDynData), 허용 범위를 묶는다.
    // 값은 모두 "화면에 보이는 단위"다. 게임이 다른 단위로 저장하는 항목은 여기서 환산한다.
    internal class AttributeDef
    {
        public int Category;  // AttributeCatalog.Categories의 순번
        public string Key;    // 파일에 적는 고정 이름. 화면의 이름(Label)이 바뀌어도 같은 줄을 가리킨다
        public string Label;
        public Func<DataUnit.PropertyData, int> Get;
        public Action<DataUnit.PropertyData, int> Set;
        public Func<WorldUnitDynData, int> DynBase;   // 보정 전 기준값
        public Func<WorldUnitDynData, int> DynValue;  // 보정 포함 적용값
        public Func<ConfRoleAttributeLimit, int> Min;
        public Func<ConfRoleAttributeLimit, int> Max;
        public bool OwnLimit; // 게임의 상한표에 범위가 없어 편집기가 자체로 정한 범위
    }

    internal static class AttributeCatalog
    {
        // 게임은 수명과 나이를 개월로 저장하고 화면에는 년으로 보여 준다(실측: 저장 5240 -> 화면 436).
        private const int MonthsPerYear = 12;
        private const int OwnLifeMaxYears = 8000;

        // 분류와 이름, 순서는 게임의 인물 화면(속성)을 따른다.
        public static readonly string[] Categories = { "개인 속성", "전투 속성", "공법 자질", "영근 자질", "기예" };

        public static readonly List<AttributeDef> All = new List<AttributeDef>
        {
            // 개인 속성
            new AttributeDef
            {
                Category = 0,
                Key = "life",
                Label = "수명(년)",
                Get = p => p.life / MonthsPerYear,
                Set = (p, v) => p.life = v * MonthsPerYear,
                DynBase = d => d.life.baseValue / MonthsPerYear,
                DynValue = d => d.life.value / MonthsPerYear,
                Min = l => (l.lifeMin + MonthsPerYear - 1) / MonthsPerYear,
                Max = l => OwnLifeMaxYears,
                OwnLimit = true,
            },
            Def(0, "moodMax", "기분 상한", p => p.moodMax, (p, v) => p.moodMax = v, d => d.moodMax, l => l.moodMin, l => l.moodMax),
            Def(0, "healthMax", "건강 상한", p => p.healthMax, (p, v) => p.healthMax = v, d => d.healthMax, l => l.healthMin, l => l.healthMax),
            Def(0, "energyMax", "정력 상한", p => p.energyMax, (p, v) => p.energyMax = v, d => d.energyMax, l => l.energyMin, l => l.energyMax),
            Def(0, "hpMax", "체력 상한", p => p.hpMax, (p, v) => p.hpMax = v, d => d.hpMax, l => l.hpMin, l => l.hpMax),
            Def(0, "mpMax", "영력 상한", p => p.mpMax, (p, v) => p.mpMax = v, d => d.mpMax, l => l.mpMin, l => l.mpMax),
            Def(0, "spMax", "집중력 상한", p => p.spMax, (p, v) => p.spMax = v, d => d.spMax, l => l.spMin, l => l.spMax),
            Def(0, "luck", "행운", p => p.luck, (p, v) => p.luck = v, d => d.luck, l => l.luckMin, l => l.luckMax),
            Def(0, "talent", "오성", p => p.talent, (p, v) => p.talent = v, d => d.talent, l => l.talentMin, l => l.talentMax),
            // 게임 화면에서는 인물 정보 쪽에 있는 값들. 다섯 분류에 없어 개인 속성에 둔다.
            Def(0, "beauty", "매력", p => p.beauty, (p, v) => p.beauty = v, d => d.beauty, l => l.beautyMin, l => l.beautyMax),
            Def(0, "reputation", "명성", p => p.reputation, (p, v) => p.reputation = v, d => d.reputation, l => l.reputationMin, l => l.reputationMax),
            Def(0, "abilityPoint", "수도 점수", p => p.abilityPoint, (p, v) => p.abilityPoint = v, d => d.abilityPoint, l => l.abilityPointMin, l => l.abilityPointMax),

            // 전투 속성
            Def(1, "attack", "공격력", p => p.attack, (p, v) => p.attack = v, d => d.attack, l => l.attackMin, l => l.attackMax),
            Def(1, "defense", "방어력", p => p.defense, (p, v) => p.defense = v, d => d.defense, l => l.defenseMin, l => l.defenseMax),
            Def(1, "footSpeed", "각력", p => p.footSpeed, (p, v) => p.footSpeed = v, d => d.footSpeed, l => l.footSpeedMin, l => l.footSpeedMax),
            Def(1, "phycicalFree", "공법 내성", p => p.phycicalFree, (p, v) => p.phycicalFree = v, d => d.phycicalFree, l => l.phycicalFreeMin, l => l.phycicalFreeMax),
            Def(1, "magicFree", "영근 내성", p => p.magicFree, (p, v) => p.magicFree = v, d => d.magicFree, l => l.magicFreeMin, l => l.magicFreeMax),
            Def(1, "crit", "회심", p => p.crit, (p, v) => p.crit = v, d => d.crit, l => l.critMin, l => l.critMax),
            Def(1, "guard", "호심", p => p.guard, (p, v) => p.guard = v, d => d.guard, l => l.guardMin, l => l.guardMax),
            Def(1, "moveSpeed", "이동속도", p => p.moveSpeed, (p, v) => p.moveSpeed = v, d => d.moveSpeed, l => l.moveSpeedMin, l => l.moveSpeedMax),
            Def(1, "critValue", "치명타 배수(%)", p => p.critValue, (p, v) => p.critValue = v, d => d.critValue, l => l.critValueMin, l => l.critValueMax),
            Def(1, "guardValue", "치명타 저항 배수(%)", p => p.guardValue, (p, v) => p.guardValue = v, d => d.guardValue, l => l.guardValueMin, l => l.guardValueMax),

            // 공법 자질
            Def(2, "basisBlade", "도법", p => p.basisBlade, (p, v) => p.basisBlade = v, d => d.basisBlade, l => l.basisBladeMin, l => l.basisBladeMax),
            Def(2, "basisSpear", "창법", p => p.basisSpear, (p, v) => p.basisSpear = v, d => d.basisSpear, l => l.basisSpearMin, l => l.basisSpearMax),
            Def(2, "basisSword", "검법", p => p.basisSword, (p, v) => p.basisSword = v, d => d.basisSword, l => l.basisSwordMin, l => l.basisSwordMax),
            Def(2, "basisFist", "권법", p => p.basisFist, (p, v) => p.basisFist = v, d => d.basisFist, l => l.basisFistMin, l => l.basisFistMax),
            Def(2, "basisPalm", "장법", p => p.basisPalm, (p, v) => p.basisPalm = v, d => d.basisPalm, l => l.basisPalmMin, l => l.basisPalmMax),
            Def(2, "basisFinger", "지법", p => p.basisFinger, (p, v) => p.basisFinger = v, d => d.basisFinger, l => l.basisFingerMin, l => l.basisFingerMax),

            // 영근 자질
            Def(3, "basisFire", "화영근", p => p.basisFire, (p, v) => p.basisFire = v, d => d.basisFire, l => l.basisFireMin, l => l.basisFireMax),
            Def(3, "basisFroze", "수영근", p => p.basisFroze, (p, v) => p.basisFroze = v, d => d.basisFroze, l => l.basisFrozeMin, l => l.basisFrozeMax),
            Def(3, "basisThunder", "뇌영근", p => p.basisThunder, (p, v) => p.basisThunder = v, d => d.basisThunder, l => l.basisThunderMin, l => l.basisThunderMax),
            Def(3, "basisWind", "풍영근", p => p.basisWind, (p, v) => p.basisWind = v, d => d.basisWind, l => l.basisWindMin, l => l.basisWindMax),
            Def(3, "basisEarth", "토영근", p => p.basisEarth, (p, v) => p.basisEarth = v, d => d.basisEarth, l => l.basisEarthMin, l => l.basisEarthMax),
            Def(3, "basisWood", "목영근", p => p.basisWood, (p, v) => p.basisWood = v, d => d.basisWood, l => l.basisWoodMin, l => l.basisWoodMax),

            // 기예
            Def(4, "refineElixir", "연단", p => p.refineElixir, (p, v) => p.refineElixir = v, d => d.refineElixir, l => l.refineElixirMin, l => l.refineElixirMax),
            Def(4, "refineWeapon", "연기", p => p.refineWeapon, (p, v) => p.refineWeapon = v, d => d.refineWeapon, l => l.refineWeaponMin, l => l.refineWeaponMax),
            Def(4, "geomancy", "풍수", p => p.geomancy, (p, v) => p.geomancy = v, d => d.geomancy, l => l.geomancyMin, l => l.geomancyMax),
            // 상한표(RoleAttributeLimit)에 부적 항목이 없어 풍수의 범위를 빌려 쓴다.
            Def(4, "symbol", "부적", p => p.symbol, (p, v) => p.symbol = v, d => d.symbol, l => l.geomancyMin, l => l.geomancyMax, true),
            Def(4, "herbal", "약재", p => p.herbal, (p, v) => p.herbal = v, d => d.herbal, l => l.herbalMin, l => l.herbalMax),
            Def(4, "mine", "광재", p => p.mine, (p, v) => p.mine = v, d => d.mine, l => l.mineMin, l => l.mineMax),
        };

        private static AttributeDef Def(int category, string key, string label, Func<DataUnit.PropertyData, int> get, Action<DataUnit.PropertyData, int> set,
            Func<WorldUnitDynData, DynInt> dyn, Func<ConfRoleAttributeLimit, int> min, Func<ConfRoleAttributeLimit, int> max, bool ownLimit = false)
        {
            return new AttributeDef
            {
                Category = category,
                Key = key,
                Label = label,
                Get = get,
                Set = set,
                DynBase = d => dyn(d).baseValue,
                DynValue = d => dyn(d).value,
                Min = min,
                Max = max,
                OwnLimit = ownLimit,
            };
        }
    }
}
