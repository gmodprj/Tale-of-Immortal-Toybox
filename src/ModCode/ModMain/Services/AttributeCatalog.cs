using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 편집할 수 있는 능력치 하나. 저장값(PropertyData), 파생값(WorldUnitDynData), 허용 범위를 묶는다.
    // 값은 모두 "화면에 보이는 단위"다. 게임이 다른 단위로 저장하는 항목은 여기서 환산한다.
    internal class AttributeDef
    {
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

        // 이름은 게임의 한국어 표기를 따른다.
        public static readonly List<AttributeDef> All = new List<AttributeDef>
        {
            Def("공격력", p => p.attack, (p, v) => p.attack = v, d => d.attack, l => l.attackMin, l => l.attackMax),
            Def("방어력", p => p.defense, (p, v) => p.defense = v, d => d.defense, l => l.defenseMin, l => l.defenseMax),
            Def("체력 상한", p => p.hpMax, (p, v) => p.hpMax = v, d => d.hpMax, l => l.hpMin, l => l.hpMax),
            Def("영력 상한", p => p.mpMax, (p, v) => p.mpMax = v, d => d.mpMax, l => l.mpMin, l => l.mpMax),
            Def("집중력 상한", p => p.spMax, (p, v) => p.spMax = v, d => d.spMax, l => l.spMin, l => l.spMax),
            new AttributeDef
            {
                Label = "수명(년)",
                Get = p => p.life / MonthsPerYear,
                Set = (p, v) => p.life = v * MonthsPerYear,
                DynBase = d => d.life.baseValue / MonthsPerYear,
                DynValue = d => d.life.value / MonthsPerYear,
                Min = l => (l.lifeMin + MonthsPerYear - 1) / MonthsPerYear,
                Max = l => OwnLifeMaxYears,
                OwnLimit = true,
            },
            Def("행운", p => p.luck, (p, v) => p.luck = v, d => d.luck, l => l.luckMin, l => l.luckMax),
            Def("오성", p => p.talent, (p, v) => p.talent = v, d => d.talent, l => l.talentMin, l => l.talentMax),
            Def("기분 상한", p => p.moodMax, (p, v) => p.moodMax = v, d => d.moodMax, l => l.moodMin, l => l.moodMax),
            Def("건강 상한", p => p.healthMax, (p, v) => p.healthMax = v, d => d.healthMax, l => l.healthMin, l => l.healthMax),
            Def("정력 상한", p => p.energyMax, (p, v) => p.energyMax = v, d => d.energyMax, l => l.energyMin, l => l.energyMax),
            Def("각력", p => p.footSpeed, (p, v) => p.footSpeed = v, d => d.footSpeed, l => l.footSpeedMin, l => l.footSpeedMax),
            Def("이동속도", p => p.moveSpeed, (p, v) => p.moveSpeed = v, d => d.moveSpeed, l => l.moveSpeedMin, l => l.moveSpeedMax),
            Def("회심", p => p.crit, (p, v) => p.crit = v, d => d.crit, l => l.critMin, l => l.critMax),
            Def("회심 배율", p => p.critValue, (p, v) => p.critValue = v, d => d.critValue, l => l.critValueMin, l => l.critValueMax),
            Def("호심", p => p.guard, (p, v) => p.guard = v, d => d.guard, l => l.guardMin, l => l.guardMax),
            Def("호심 배율", p => p.guardValue, (p, v) => p.guardValue = v, d => d.guardValue, l => l.guardValueMin, l => l.guardValueMax),
            Def("물리 저항", p => p.phycicalFree, (p, v) => p.phycicalFree = v, d => d.phycicalFree, l => l.phycicalFreeMin, l => l.phycicalFreeMax),
            Def("마법 저항", p => p.magicFree, (p, v) => p.magicFree = v, d => d.magicFree, l => l.magicFreeMin, l => l.magicFreeMax),
            Def("매력", p => p.beauty, (p, v) => p.beauty = v, d => d.beauty, l => l.beautyMin, l => l.beautyMax),
            Def("명성", p => p.reputation, (p, v) => p.reputation = v, d => d.reputation, l => l.reputationMin, l => l.reputationMax),
            Def("수도 점수", p => p.abilityPoint, (p, v) => p.abilityPoint = v, d => d.abilityPoint, l => l.abilityPointMin, l => l.abilityPointMax),
            Def("화영근", p => p.basisFire, (p, v) => p.basisFire = v, d => d.basisFire, l => l.basisFireMin, l => l.basisFireMax),
            Def("수영근", p => p.basisFroze, (p, v) => p.basisFroze = v, d => d.basisFroze, l => l.basisFrozeMin, l => l.basisFrozeMax),
            Def("뇌영근", p => p.basisThunder, (p, v) => p.basisThunder = v, d => d.basisThunder, l => l.basisThunderMin, l => l.basisThunderMax),
            Def("풍영근", p => p.basisWind, (p, v) => p.basisWind = v, d => d.basisWind, l => l.basisWindMin, l => l.basisWindMax),
            Def("토영근", p => p.basisEarth, (p, v) => p.basisEarth = v, d => d.basisEarth, l => l.basisEarthMin, l => l.basisEarthMax),
            Def("목영근", p => p.basisWood, (p, v) => p.basisWood = v, d => d.basisWood, l => l.basisWoodMin, l => l.basisWoodMax),
            Def("검법", p => p.basisSword, (p, v) => p.basisSword = v, d => d.basisSword, l => l.basisSwordMin, l => l.basisSwordMax),
            Def("창법", p => p.basisSpear, (p, v) => p.basisSpear = v, d => d.basisSpear, l => l.basisSpearMin, l => l.basisSpearMax),
            Def("도법", p => p.basisBlade, (p, v) => p.basisBlade = v, d => d.basisBlade, l => l.basisBladeMin, l => l.basisBladeMax),
            Def("권법", p => p.basisFist, (p, v) => p.basisFist = v, d => d.basisFist, l => l.basisFistMin, l => l.basisFistMax),
            Def("장법", p => p.basisPalm, (p, v) => p.basisPalm = v, d => d.basisPalm, l => l.basisPalmMin, l => l.basisPalmMax),
            Def("지법", p => p.basisFinger, (p, v) => p.basisFinger = v, d => d.basisFinger, l => l.basisFingerMin, l => l.basisFingerMax),
            Def("연단", p => p.refineElixir, (p, v) => p.refineElixir = v, d => d.refineElixir, l => l.refineElixirMin, l => l.refineElixirMax),
            Def("연기", p => p.refineWeapon, (p, v) => p.refineWeapon = v, d => d.refineWeapon, l => l.refineWeaponMin, l => l.refineWeaponMax),
            Def("풍수", p => p.geomancy, (p, v) => p.geomancy = v, d => d.geomancy, l => l.geomancyMin, l => l.geomancyMax),
            Def("약재", p => p.herbal, (p, v) => p.herbal = v, d => d.herbal, l => l.herbalMin, l => l.herbalMax),
            Def("광재", p => p.mine, (p, v) => p.mine = v, d => d.mine, l => l.mineMin, l => l.mineMax),
        };

        private static AttributeDef Def(string label, Func<DataUnit.PropertyData, int> get, Action<DataUnit.PropertyData, int> set,
            Func<WorldUnitDynData, DynInt> dyn, Func<ConfRoleAttributeLimit, int> min, Func<ConfRoleAttributeLimit, int> max)
        {
            return new AttributeDef
            {
                Label = label,
                Get = get,
                Set = set,
                DynBase = d => dyn(d).baseValue,
                DynValue = d => dyn(d).value,
                Min = min,
                Max = max,
            };
        }
    }
}
