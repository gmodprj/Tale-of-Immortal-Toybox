using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 세계 진입이 FormulaTool.UnitPower.GetMartialLevel의 null 예외로 멈추는 까닭 조사(05 문서 97절). 읽기만 한다.
    //   rpower   플레이어와 세계의 인물마다 TotalPower를 불러 보고, 실패한 인물의 기술마다 GetMartialLevel을 불러
    //            실패한 기술(종류·baseID·경지·옵션 수)을 센다.
    internal static class DevCommandsPower
    {
        public static string Execute(string[] p)
        {
            if (p[0] != "rpower")
            {
                return null;
            }
            if (p.Length > 1 && p[1] == "dlc")
            {
                return Dlc();
            }
            var units = new List<WorldUnitBase> { g.world.playerUnit };
            var list = g.world.unit.GetUnits(false);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i] != g.world.playerUnit)
                {
                    units.Add(list[i]);
                }
            }
            int failed = 0;
            var bad = new Dictionary<string, int>();
            var failedUnits = new StringBuilder();
            foreach (WorldUnitBase unit in units)
            {
                try
                {
                    FormulaTool.UnitPower.TotalPower(unit.data);
                    continue;
                }
                catch (Exception)
                {
                    failed++;
                }
                if (failed <= 5)
                {
                    failedUnits.Append(' ').Append(unit.data.unitData.unitID);
                }
                foreach (var pair in unit.data.unitData.allActionMartial)
                {
                    DataProps.MartialData martial;
                    try
                    {
                        martial = pair.Value.data.To<DataProps.MartialData>();
                    }
                    catch (Exception e)
                    {
                        Count(bad, "read " + e.GetType().Name);
                        continue;
                    }
                    int options = -1;
                    try
                    {
                        options = MartialService.OptionNumbers(martial).Length;
                    }
                    catch (Exception)
                    {
                    }
                    try
                    {
                        FormulaTool.UnitPower.GetMartialLevel(martial.martialType, martial.grade, martial.baseID, Math.Max(options, 0));
                    }
                    catch (Exception)
                    {
                        Count(bad, martial.martialType + " base=" + martial.baseID + " grade=" + martial.grade + " options=" + options);
                    }
                }
            }
            var sb = new StringBuilder();
            sb.Append(MissingSkillGuard.LastNote).Append(" guarded=").Append(MissingSkillGuard.Guarded).Append(" powerZeroed=").Append(MissingSkillGuard.PowerZeroed).Append(' ');
            sb.Append("units=").Append(units.Count).Append(" failed=").Append(failed).Append(" firstFailed=").Append(failedUnits);
            foreach (var pair in bad.OrderByDescending(x => x.Value).Take(20))
            {
                sb.Append(" | ").Append(pair.Key).Append(" x").Append(pair.Value);
            }
            return sb.ToString();
        }

        // rpower dlc: Steam이 아는 DLC(소유·설치)와, 실패한 기술과 그 이웃 번호의 옵션 행 수.
        private static string Dlc()
        {
            var sb = new StringBuilder();
            try
            {
                System.Reflection.Assembly first = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp-firstpass");
                Type apps = first.GetType("Steamworks.SteamApps");
                Type appId = first.GetType("Steamworks.AppId_t");
                sb.Append("steamInit=").Append(SteamManager.Initialized);
                foreach (uint id in new uint[] { 2408730, 3294480, 3326620, 1536810 })
                {
                    object arg = Activator.CreateInstance(appId, id);
                    object sub = apps.GetMethod("BIsSubscribedApp").Invoke(null, new[] { arg });
                    object inst = apps.GetMethod("BIsDlcInstalled").Invoke(null, new[] { arg });
                    sb.Append(" | ").Append(id).Append(" subscribed=").Append(sub).Append(" installed=").Append(inst);
                }
                int count = (int)apps.GetMethod("GetDLCCount").Invoke(null, null);
                sb.Append(" | dlcCount=").Append(count);
                System.Reflection.MethodInfo byIndex = apps.GetMethod("BGetDLCDataByIndex");
                for (int i = 0; i < count; i++)
                {
                    object[] a = { i, Activator.CreateInstance(appId, 0u), false, "", 128 };
                    byIndex.Invoke(null, a);
                    sb.Append(" [").Append(a[1].GetType().GetField("m_AppId") != null ? a[1].GetType().GetField("m_AppId").GetValue(a[1]) : a[1]).Append(" avail=").Append(a[2]).Append(' ').Append(a[3]).Append(']');
                }
            }
            catch (Exception e)
            {
                sb.Append(" steam error ").Append(e.GetType().Name).Append(": ").Append((e.InnerException ?? e).Message);
            }
            var probes = new (MartialType type, int id)[]
            {
                (MartialType.Step, 510151), (MartialType.Step, 510161), (MartialType.Step, 510162),
                (MartialType.SkillRight, 24116), (MartialType.SkillRight, 24117), (MartialType.SkillRight, 24118),
                (MartialType.Ultimate, 34113), (MartialType.Ultimate, 34114),
            };
            foreach (var probe in probes)
            {
                int rows;
                try
                {
                    var items = g.conf.battleSkillPrefixValue.GetItems((int)probe.type, probe.id);
                    rows = items == null ? -2 : items.Count;
                }
                catch (Exception)
                {
                    rows = -1;
                }
                sb.Append(" | ").Append(probe.type).Append(' ').Append(probe.id).Append(" rows=").Append(rows).Append(" base=").Append(BaseRow(probe.type, probe.id));
            }
            return sb.ToString();
        }

        // 기술 본표(절기·신통은 battleSkillAttack, 신법은 battleStepBase)의 행: 없으면 "none", 있으면 경지 범위와 모드 확장 여부.
        private static string BaseRow(MartialType type, int id)
        {
            if (type == MartialType.Step)
            {
                var step = g.conf.battleStepBase._allConfList;
                for (int i = 0; i < step.Count; i++)
                {
                    if (step[i].id == id)
                    {
                        return "grade" + step[i].gradeMin + "-" + step[i].gradeMax + " mod=" + step[i].isModExtend;
                    }
                }
                return "none";
            }
            var attack = g.conf.battleSkillAttack._allConfList;
            for (int i = 0; i < attack.Count; i++)
            {
                if (attack[i].id == id)
                {
                    return "type" + attack[i].type + " grade" + attack[i].gradeMin + "-" + attack[i].gradeMax + " mod=" + attack[i].isModExtend;
                }
            }
            return "none";
        }

        // 세계 진입이 EventsMgr.Init의 다음 단계와 극 함수에서 null 예외로 끊기는 자리 찾기(97.8절).
        // EventsMgr.Init은 넘겨받은 Action을 부르기만 하므로 그 Action의 대상 메서드 이름을, OptionsFunction은 함수 문자열을 남긴다.
        private static HarmonyLib.Harmony probes;

        public static void HookProbes()
        {
            if (probes != null)
            {
                return;
            }
            probes = new HarmonyLib.Harmony("TlToybox.powerprobe");
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
                probes.Patch(typeof(EventsMgr).GetMethod("Init", flags), prefix: new HarmonyLib.HarmonyMethod(typeof(DevCommandsPower).GetMethod(nameof(EventsInitPrefix))));
                probes.Patch(typeof(DramaFunction).GetMethod("OptionsFunction", flags), prefix: new HarmonyLib.HarmonyMethod(typeof(DevCommandsPower).GetMethod(nameof(OptionsPrefix))),
                    finalizer: new HarmonyLib.HarmonyMethod(typeof(DevCommandsPower).GetMethod(nameof(OptionsFinalizer))));
                MelonLoader.MelonLogger.Msg("[TlToybox][probe97] hooked");
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Msg("[TlToybox][probe97] hook failed " + e.GetType().Name + ": " + e.Message);
            }
        }

        public static void EventsInitPrefix(Il2CppSystem.Action call)
        {
            try
            {
                var m = call == null ? null : call.Method;
                MelonLoader.MelonLogger.Msg("[TlToybox][probe97] EventsMgr.Init call=" + (m == null ? "null" : m.DeclaringType.FullName + "." + m.Name));
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Msg("[TlToybox][probe97] EventsMgr.Init call=? " + e.GetType().Name);
            }
        }

        private static string lastFunction = "";

        public static void OptionsPrefix(string function)
        {
            lastFunction = function ?? "null";
        }

        public static Exception OptionsFinalizer(Exception __exception)
        {
            if (__exception != null)
            {
                MelonLoader.MelonLogger.Msg("[TlToybox][probe97] OptionsFunction failed function=" + lastFunction);
            }
            return __exception;
        }

        private static void Count(Dictionary<string, int> map, string key)
        {
            map.TryGetValue(key, out int n);
            map[key] = n + 1;
        }
    }
}
