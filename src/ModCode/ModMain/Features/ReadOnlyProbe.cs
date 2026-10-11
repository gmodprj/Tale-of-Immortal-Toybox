using System;
using System.Diagnostics;
using MelonLoader;

namespace MOD_TlTbx0
{
    // docs/04-ingame-state-analysis.md 15절 1단계의 읽기 전용 탐침.
    // 게임 상태를 읽어 로그로만 내보낸다. 어떤 값도 쓰지 않는다.
    internal static class ReadOnlyProbe
    {
        private const string Tag = "[TlToybox][probe]";

        public static void Run()
        {
            Log("==== begin ====");
            Step("1 player name", ProbePlayerName);
            Step("2 saved vs dyn", ProbeSavedVsDyn);
            Step("3 unit count", ProbeUnitCount);
            Step("4a props", ProbeProps);
            Step("4b martial", ProbeMartial);
            Step("5 world", ProbeWorld);
            Log("==== end ====");
        }

        private static void Step(string name, Action probe)
        {
            try
            {
                probe();
            }
            catch (Exception e)
            {
                Log(name + " FAILED: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static void Log(string message)
        {
            MelonLogger.Msg(Tag + " " + message);
        }

        private static void ProbePlayerName()
        {
            var unitData = g.world.playerUnit.data.unitData;
            Log("1 name=" + unitData.propertyData.GetName() + " unitID=" + unitData.unitID);
        }

        private static void ProbeSavedVsDyn()
        {
            var unit = g.world.playerUnit;
            var saved = unit.data.unitData.propertyData;
            var dyn = unit.data.dynUnitData;
            Log("2 attack saved=" + saved.attack + " dyn.base=" + dyn.attack.baseValue + " dyn.value=" + dyn.attack.value);
            Log("2 defense saved=" + saved.defense + " dyn.base=" + dyn.defense.baseValue + " dyn.value=" + dyn.defense.value);
            Log("2 hpMax saved=" + saved.hpMax + " dyn.base=" + dyn.hpMax.baseValue + " dyn.value=" + dyn.hpMax.value);
            Log("2 hp saved=" + saved.hp + " dyn.value=" + dyn.hp.value);
            Log("2 life saved=" + saved.life + " dyn.value=" + dyn.life.value + " age=" + saved.age);
            Log("2 luck saved=" + saved.luck + " dyn.value=" + dyn.luck.value);
            Log("2 gradeID saved=" + saved.gradeID + " dyn.value=" + dyn.gradeID.value + " exp=" + saved.exp);
        }

        private static void ProbeUnitCount()
        {
            var watch = Stopwatch.StartNew();
            var units = g.world.unit.GetUnits(false);
            int count = units.Count;
            int named = 0;
            for (int i = 0; i < count; i++)
            {
                if (units[i].data.unitData.propertyData.GetName().Length > 0)
                {
                    named++;
                }
            }
            watch.Stop();
            Log("3 units=" + count + " named=" + named + " elapsedMs=" + watch.ElapsedMilliseconds);
        }

        private static void ProbeProps()
        {
            var propData = g.world.playerUnit.data.unitData.propData;
            var all = propData.allProps;
            Log("4a props entries=" + all.Count + " grid=" + propData.GetGridCurNum() + "/" + propData.GetGridMaxNum()
                + " money=" + propData.GetPropsNum(PropsIDType.Money));
            int shown = Math.Min(all.Count, 10);
            for (int i = 0; i < shown; i++)
            {
                var p = all[i];
                Log("4a   [" + i + "] propsID=" + p.propsID + " count=" + p.propsCount + " type=" + p.propsType + " soleID=" + p.soleID);
            }
        }

        private static void ProbeMartial()
        {
            var martials = g.world.playerUnit.data.unitData.allActionMartial;
            Log("4b martial entries=" + martials.Count);
            int shown = 0;
            foreach (var pair in martials)
            {
                if (shown >= 5)
                {
                    break;
                }
                shown++;
                var action = pair.Value;
                var martial = action.data.To<DataProps.MartialData>();
                var prefixes = martial.GetPrefixs();
                Log("4b   soleID=" + pair.Key + " type=" + martial.martialType + " baseID=" + martial.baseID
                    + " grade=" + martial.grade + " exp=" + action.exp + " prefixes=" + prefixes.Count);
                for (int i = 0; i < prefixes.Count; i++)
                {
                    Log("4b     prefix index=" + prefixes[i].index + " level=" + prefixes[i].prefixLevel);
                }
            }
        }

        private static void ProbeWorld()
        {
            var world = g.data.world;
            Log("5 worldDay=" + world.worldDay + " playerUnitID=" + world.playerUnitID + " gameLevel=" + world.gameLevel
                + " roundMonth=" + g.world.run.roundMonth);
        }
    }
}
