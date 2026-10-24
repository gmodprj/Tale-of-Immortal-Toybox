using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. 트레이너 비교 1묶음 설계(docs/superpowers/specs/2026-10-20-trainer-batch1-design.md 4절)의 설계 전 실험 E1~E4.
    //   tbtraits [n]                     플레이어와 NPC n명(기본 3)의 inTrait·outTrait1·outTrait2·GetCharacter, roleCreateCharacter의 모든 줄
    //   tbtraitset <in> <o1> <o2> [unitID]   성격 칸을 쓰고 전후의 칸·GetCharacter
    //   tbids <className> [n]            그 세부 분류의 아이템 번호와 이름(앞 n개, 기본 10)
    //   tbbag <id>                       가방에서 그 번호의 줄: sole·수·종류·겹침·장착·법보(level·grade·옵션 수), GetPropsNum
    //   tbequips                         장착 중인 soleID
    //   tbbagset <id> <n>                수를 n으로(늘림 RewardPropItem, 줄임 DelProps(id, 차이))
    //   tbbagdel <sole> [count]          DelProps(soleID[, count])
    //   tbuse <id>                       그 번호의 첫 줄로 UnitActionPropUse를 만들어 IsCreateAction·CreateAction, 수 전후
    //   tbbprops / tbbuse <i>            전투: 플레이어 전투 아이템 목록 / i번을 IsCreate·Create
    //   tbforge <연기보> <재료 번호>     가방의 그 재료로 연기 행동을 만들어 CreateAction(재료가 줄어드는지)
    //   tbtrace on [묶음,…]|off|read|clear   아이템 소모·영력 깎기 후보 메서드에 기록용 접두(되돌리지 않음. off는 기록만 멈춤)
    //                                    묶음: use(UnitActionPropUse) wud(WorldUnitData.Cost*) del(DataProps.Del*) count(PropsData.set_propsCount)
    //                                    act(학습·돌파·버리기·연기 OnCreate) mp(AddMP·AddSP). 기본은 count를 뺀 전부.
    //                                    전투 아이템의 OnCreate(Vector2 인자)는 접두를 거는 순간 게임이 꺼졌다(tb1-1020a·b). 걸지 않는다
    //   tbblock usecost|sole|addmp on|off   후보 막기: UnitActionPropUse.CostPropItem 건너뛰기 /
    //                                    쓰기 행동(PropUse·비급 학습·돌파) 안에서 그 행동의 sole만 WorldUnitData.CostPropItem 건너뛰기 /
    //                                    전투 중 플레이어의 AddMP·AddSP 음수 건너뛰기
    //   tbmp                             전투 중 플레이어의 mp/sp와 상한
    //   tbartlevel <sole> <단계>         가방의 법보 하나의 단계를 PropsArtifact.SetLevel로 바꾼다(검사 bagcount의 "약한 것부터")
    internal static class DevCommandsTrainerBatch1
    {
        private static HarmonyLib.Harmony harmony;
        private static bool tracing;
        private static bool blockUseCost, blockMp, blockSole;
        private static readonly HashSet<string> protectedSoles = new HashSet<string>();   // 지금 도는 쓰기 행동이 쓰는 아이템
        private static readonly List<string> log = new List<string>();
        private static readonly List<string> patchErrors = new List<string>();

        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "tbtraits":
                    return Traits(p.Length > 1 ? int.Parse(p[1]) : 3);
                case "tbtraitset":
                    return TraitSet(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), p.Length > 4 ? p[4] : null);
                case "tbids":
                    return Ids(int.Parse(p[1]), p.Length > 2 ? int.Parse(p[2]) : 10);
                case "tbbag":
                    return Bag(int.Parse(p[1]));
                case "tbequips":
                    return string.Join(",", Equips().ToArray());
                case "tbbagset":
                    return BagSet(int.Parse(p[1]), int.Parse(p[2]));
                case "tbbagdel":
                    return BagDel(p[1], p.Length > 2 ? int.Parse(p[2]) : -1);
                case "tbuse":
                    return Use(int.Parse(p[1]));
                case "tbbprops":
                    return BattleProps();
                case "tbbuse":
                    return BattleUse(int.Parse(p[1]));
                case "tbforge":
                    return Forge(int.Parse(p[1]), int.Parse(p[2]));
                case "tbtrace":
                    return Trace(p[1], p.Length > 2 ? p[2] : "use,wud,del,act,mp");
                case "tbblock":
                    return Block(p[1], p[2] == "on");
                case "tbartlevel":
                    return ArtLevel(p[1], int.Parse(p[2]));
                case "tbmp":
                    return Mp();
                default:
                    return null;
            }
        }

        private static WorldUnitBase Me()
        {
            return g.world.playerUnit;
        }

        // ---- E1 성격 ----

        private static string TraitLine(WorldUnitBase u)
        {
            var pd = u.data.unitData.propertyData;
            var chars = u.data.GetCharacter();
            var list = new List<string>();
            for (int i = 0; chars != null && i < chars.Count; i++)
            {
                list.Add(chars[i].ToString());
            }
            return u.data.unitData.unitID + " in=" + pd.inTrait + " o1=" + pd.outTrait1 + " o2=" + pd.outTrait2
                + " char=[" + string.Join(",", list.ToArray()) + "]";
        }

        private static string Traits(int n)
        {
            var sb = new StringBuilder();
            sb.Append("player ").Append(TraitLine(Me())).Append('\n');
            var units = g.world.unit.GetUnits(false);
            int shown = 0;
            for (int i = 0; i < units.Count && shown < n; i++)
            {
                if (units[i].data.unitData.unitID == Me().data.unitData.unitID)
                {
                    continue;
                }
                sb.Append("npc ").Append(TraitLine(units[i])).Append('\n');
                shown++;
            }
            var rows = g.conf.roleCreateCharacter._allConfList;
            sb.Append("rows=").Append(rows.Count).Append('\n');
            for (int i = 0; i < rows.Count; i++)
            {
                ConfRoleCreateCharacterItem r = rows[i];
                sb.Append("row ").Append(r.id).Append(" type=").Append(r.type)
                  .Append(" s1=").Append(r.sc5asd_sd34).Append('/').Append(SafeLs(r.sc5asd_sd34))
                  .Append(" s2=").Append(r.xdash_54sd).Append('/').Append(SafeLs(r.xdash_54sd))
                  .Append(" ints=").Append(r.xssdf434).Append(',').Append(r.xxcvbgd).Append(',').Append(r.asdxzx).Append(',').Append(r.kjhgg6778)
                  .Append(',').Append(r.xcssdsd997).Append(',').Append(r.zzxcas113).Append(',').Append(r.hfasd445).Append(',').Append(r.xxcsasd856)
                  .Append(',').Append(r.xxcasd123).Append(',').Append(r.jgas654).Append(',').Append(r.xxcsadg654).Append(',').Append(r.xccsadiu)
                  .Append(',').Append(r.ppodxcsd1).Append(',').Append(r.hjk765).Append(',').Append(r.asdasdqwe123).Append(" doctrine=").Append(r.doctrine)
                  .Append(',').Append(r.bgsadgg543).Append(',').Append(r.xcasdzxcasd).Append(',').Append(r.zxcasdasdfsad).Append(',').Append(r.asdsdsd123).Append('\n');
            }
            return sb.ToString();
        }

        private static string SafeLs(string key)
        {
            try
            {
                return string.IsNullOrEmpty(key) ? "" : GameTool.LS(key);
            }
            catch (Exception e)
            {
                return "!" + e.GetType().Name;
            }
        }

        private static string TraitSet(int inT, int o1, int o2, string unitId)
        {
            WorldUnitBase u = unitId == null ? Me() : g.world.unit.GetUnit(unitId, true);
            if (u == null)
            {
                return "no unit " + unitId;
            }
            string before = TraitLine(u);
            var pd = u.data.unitData.propertyData;
            pd.inTrait = inT;
            pd.outTrait1 = o1;
            pd.outTrait2 = o2;
            return "before " + before + "\nafter  " + TraitLine(u);
        }

        // ---- E3 가방 ----

        private static string Ids(int className, int n)
        {
            var sb = new StringBuilder();
            var all = g.conf.itemProps._allConfList;
            int shown = 0;
            for (int i = 0; i < all.Count && shown < n; i++)
            {
                if (all[i].className == className)
                {
                    sb.Append(all[i].id).Append(':').Append(SafeLs(all[i].name)).Append(" lv").Append(all[i].level).Append(" ov").Append(all[i].isOverlay).Append(' ');
                    shown++;
                }
            }
            return sb.ToString();
        }

        private static List<string> Equips()
        {
            var result = new List<string>();
            var all = Me().data.unitData.GetAllEquipPropsSoleID();
            for (int i = 0; all != null && i < all.Count; i++)
            {
                result.Add(all[i]);
            }
            return result;
        }

        private static string Bag(int id)
        {
            var data = Me().data.unitData;
            var sb = new StringBuilder();
            ConfItemPropsItem conf = g.conf.itemProps.GetItem(id);
            sb.Append("id=").Append(id).Append(" overlay=").Append(conf == null ? -1 : conf.isOverlay).Append(" num=").Append(data.propData.GetPropsNum(id))
              .Append(" slots=").Append(data.propData.allProps.Count).Append('\n');
            var all = data.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                DataProps.PropsData pd = all[i];
                if (pd.propsID != id)
                {
                    continue;
                }
                sb.Append(" [").Append(i).Append("] sole=").Append(pd.soleID).Append(" count=").Append(pd.propsCount).Append(" type=").Append(pd.propsType)
                  .Append(" equip=").Append(data.IsEquipProps(pd.soleID));
                if (SpriteService.IsArtifact(pd.propsID))
                {
                    try
                    {
                        DataProps.PropsArtifact a = pd.To<DataProps.PropsArtifact>();
                        sb.Append(" level=").Append(a.level).Append(" grade=").Append(a.grade).Append(" prefix=").Append(a.GetPrefixCount()).Append(" durable=").Append(a.durable);
                    }
                    catch (Exception e)
                    {
                        sb.Append(" artifact!").Append(e.GetType().Name);
                    }
                }
                sb.Append('\n');
            }
            sb.Append("equips=").Append(string.Join(",", Equips().ToArray()));
            return sb.ToString();
        }

        private static string BagSet(int id, int n)
        {
            var props = Me().data.unitData.propData;
            int c = props.GetPropsNum(id);
            if (n > c)
            {
                Me().data.RewardPropItem(id, n - c, false);
            }
            else if (n < c)
            {
                props.DelProps(id, c - n);
            }
            return "num " + c + " -> " + props.GetPropsNum(id) + "\n" + Bag(id);
        }

        private static string BagDel(string sole, int count)
        {
            var props = Me().data.unitData.propData;
            int slots = props.allProps.Count;
            DataProps.PropsData r = count < 0 ? props.DelProps(sole) : props.DelProps(sole, count);
            return "returned=" + (r == null ? "null" : r.propsID + "x" + r.propsCount) + " slots " + slots + " -> " + props.allProps.Count
                + " equips=" + string.Join(",", Equips().ToArray());
        }

        private static string ArtLevel(string sole, int level)
        {
            var all = Me().data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].soleID == sole)
                {
                    DataProps.PropsArtifact a = all[i].To<DataProps.PropsArtifact>();
                    int before = a.level;
                    a.SetLevel(level);
                    return "level " + before + " -> " + a.level;
                }
            }
            return "no prop " + sole;
        }

        // ---- E2 아이템 사용 ----

        private static DataProps.PropsData First(int id)
        {
            var all = Me().data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].propsID == id)
                {
                    return all[i];
                }
            }
            return null;
        }

        private static string Use(int id)
        {
            DataProps.PropsData pd = First(id);
            if (pd == null)
            {
                return "no prop " + id;
            }
            var props = Me().data.unitData.propData;
            int before = props.GetPropsNum(id);
            var action = new UnitActionPropUse(pd);
            int can = Me().IsCreateAction(action, false);
            int code = can == 0 ? Me().CreateAction(action, false) : -1;
            return "IsCreateAction=" + can + " CreateAction=" + code + " num " + before + " -> " + props.GetPropsNum(id);
        }

        private static string BattleProps()
        {
            if (!RateService.InBattle)
            {
                return "not in battle";
            }
            UnitCtrlPlayer unit = SceneType.battle.battleMap.playerUnitCtrl;
            var sb = new StringBuilder();
            for (int i = 0; unit.props != null && i < unit.props.Count; i++)
            {
                PropItemBase pi = unit.props[i];
                DataProps.PropsData pd = pi == null || pi.data == null ? null : pi.data.propsData;
                sb.Append('[').Append(i).Append("] ").Append(pi == null ? "null" : pi.GetIl2CppType().Name)
                  .Append(" props=").Append(pd == null ? "-" : pd.propsID + "x" + pd.propsCount + " num=" + Me().data.unitData.propData.GetPropsNum(pd.propsID)).Append('\n');
            }
            return sb.Length == 0 ? "no props" : sb.ToString();
        }

        private static string BattleUse(int index)
        {
            if (!RateService.InBattle)
            {
                return "not in battle";
            }
            UnitCtrlPlayer unit = SceneType.battle.battleMap.playerUnitCtrl;
            PropItemBase pi = unit.props[index];
            DataProps.PropsData pd = pi.data.propsData;
            int id = pd.propsID;
            int before = Me().data.unitData.propData.GetPropsNum(id);
            Vector2 posi = unit.posiCenter.position;
            Vector2 dire = Vector2.right;
            bool can = pi.IsCreate(posi, dire, false);
            if (can)
            {
                pi.Create(posi, dire);
            }
            return "IsCreate=" + can + " num " + before + " -> " + Me().data.unitData.propData.GetPropsNum(id) + " (propsData.count=" + pd.propsCount + ")";
        }

        private static string Forge(int shapeMaterialId, int materialId)
        {
            ConfArtifactShapeMaterialItem m = g.conf.artifactShapeMaterial.GetItem(shapeMaterialId);
            if (m == null)
            {
                return "no shape material " + shapeMaterialId;
            }
            var types = new List<int>();
            for (int i = 0; i < m.materialType.Length; i++)
            {
                types.Add(m.materialType[i]);
            }
            var all = Me().data.unitData.propData.allProps;
            DataProps.PropsData main = null, fire = null;
            var il = new Il2CppSystem.Collections.Generic.List<DataProps.PropsData>();
            int matCount = 0;
            for (int i = 0; i < all.Count; i++)
            {
                DataProps.PropsData pd = all[i];
                ConfItemPropsItem c = g.conf.itemProps.GetItem(pd.propsID);
                int cls = c == null ? -1 : c.className;
                if (pd.propsID == m.coreMaterialID && main == null)
                {
                    main = pd;
                }
                else if (cls == 522 && fire == null)
                {
                    fire = pd;
                }
                else if (pd.propsID == materialId && matCount < m.materialNum)
                {
                    il.Add(pd);
                    matCount += pd.propsCount;
                }
            }
            int slots = all.Count;
            string counts = (main == null ? "-" : main.propsID + ":" + Me().data.unitData.propData.GetPropsNum(main.propsID)) + " fire=" + (fire == null ? "-" : fire.propsID + ":" + Me().data.unitData.propData.GetPropsNum(fire.propsID));
            var action = new UnitActionPropArtifactShapeMade(main, null, il, fire, m, true);
            action.unit = Me();
            int can = Me().IsCreateAction(action, false);
            int code = can == 0 ? Me().CreateAction(action, false) : -1;
            string after = (main == null ? "-" : main.propsID + ":" + Me().data.unitData.propData.GetPropsNum(main.propsID)) + " fire=" + (fire == null ? "-" : fire.propsID + ":" + Me().data.unitData.propData.GetPropsNum(fire.propsID));
            return "IsCreate=" + can + " Create=" + code + " core " + counts + " -> " + after + " slots " + slots + " -> " + Me().data.unitData.propData.allProps.Count;
        }

        // ---- 추적 ----

        private static string Trace(string what, string groups)
        {
            switch (what)
            {
                case "on":
                    EnsurePatched(groups);
                    tracing = true;
                    return "tracing on, patch errors=" + patchErrors.Count + (patchErrors.Count > 0 ? " " + string.Join(" | ", patchErrors.ToArray()) : "");
                case "off":
                    tracing = false;
                    return "tracing off";
                case "clear":
                    log.Clear();
                    return "cleared";
                default:
                    return "lines=" + log.Count + "\n" + string.Join("\n", log.ToArray());
            }
        }

        private static string Block(string what, bool on)
        {
            EnsurePatched(what == "usecost" ? "use" : "mp");
            if (what == "usecost")
            {
                blockUseCost = on;
            }
            else if (what == "sole")
            {
                EnsurePatched("use,wud,act");
                blockSole = on;
            }
            else if (what == "addmp")
            {
                blockMp = on;
            }
            else
            {
                return "unknown " + what;
            }
            return "block usecost=" + blockUseCost + " sole=" + blockSole + " addmp=" + blockMp + " patch errors=" + patchErrors.Count;
        }

        private static string Mp()
        {
            if (!RateService.InBattle)
            {
                return "not in battle";
            }
            UnitDataBase d = SceneType.battle.battleMap.playerUnitCtrl.data;
            return "mp=" + d.mp + "/" + d.maxMP.value + " sp=" + d.sp + "/" + d.maxSP.value;
        }

        private static void Add(string line)
        {
            if (tracing && log.Count < 500)
            {
                log.Add("#" + Time.frameCount + " " + line);
            }
        }

        private static void Patch(Type type, string name, Type[] args, string prefix, string postfix = null)
        {
            try
            {
                var target = args == null ? HarmonyLib.AccessTools.Method(type, name) : HarmonyLib.AccessTools.Method(type, name, args);
                if (target == null)
                {
                    patchErrors.Add(type.Name + "." + name + " not found");
                    return;
                }
                var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
                harmony.Patch(target, new HarmonyLib.HarmonyMethod(typeof(DevCommandsTrainerBatch1).GetMethod(prefix, flags)),
                    postfix == null ? null : new HarmonyLib.HarmonyMethod(typeof(DevCommandsTrainerBatch1).GetMethod(postfix, flags)));
            }
            catch (Exception e)
            {
                patchErrors.Add(type.Name + "." + name + " " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static readonly HashSet<string> patchedGroups = new HashSet<string>();

        private static void EnsurePatched(string groups)
        {
            if (harmony == null)
            {
                harmony = new HarmonyLib.Harmony("TlToybox.dev.tb1");
            }
            foreach (string group in groups.Split(','))
            {
                if (patchedGroups.Add(group))
                {
                    PatchGroup(group);
                }
            }
        }

        private static void PatchGroup(string group)
        {
            Type pd = typeof(DataProps.PropsData);
            Type pdList = typeof(Il2CppSystem.Collections.Generic.List<DataProps.PropsData>);
            Type intList = typeof(Il2CppSystem.Collections.Generic.List<int>);
            switch (group)
            {
                case "use":
            Patch(typeof(UnitActionPropUse), "OnCreate", Type.EmptyTypes, "UseOnCreate", "ProtectEnd");
            Patch(typeof(UnitActionPropUse), "CostPropItem", Type.EmptyTypes, "UseCost");
                    break;
                case "wud":
            Patch(typeof(WorldUnitData), "CostPropItem", new[] { typeof(int), typeof(int), typeof(bool) }, "WudCostId");
            Patch(typeof(WorldUnitData), "CostPropItem", new[] { pdList, typeof(bool) }, "WudCostList");
            Patch(typeof(WorldUnitData), "CostPropItem", new[] { pd, typeof(bool) }, "WudCostPd");
            Patch(typeof(WorldUnitData), "CostPropItem", new[] { typeof(string), typeof(int), typeof(bool) }, "WudCostSole");
            Patch(typeof(WorldUnitData), "CostItem", new[] { typeof(int), typeof(bool) }, "WudCostItem");
            Patch(typeof(WorldUnitData), "CostItem", new[] { intList, typeof(bool) }, "WudCostItems");
                    break;
                case "del":
            Patch(typeof(DataProps), "DelProps", new[] { typeof(string) }, "DelSole");
            Patch(typeof(DataProps), "DelProps", new[] { typeof(string), typeof(int) }, "DelSoleCount");
            Patch(typeof(DataProps), "DelProps", new[] { typeof(int), typeof(int) }, "DelId");
            Patch(typeof(DataProps), "DelPropsItem", new[] { pd }, "DelItem");
                    break;
                case "count":
            Patch(pd, "set_propsCount", new[] { typeof(int) }, "SetCount");
                    break;
                case "act":
            Patch(typeof(UnitActionPropMartialStudy), "OnCreate", Type.EmptyTypes, "StudyOnCreate", "ProtectEnd");
            Patch(typeof(UnitActionRoleUpGrade), "OnCreate", Type.EmptyTypes, "UpGradeOnCreate", "ProtectEnd");
            Patch(typeof(UnitActionPropDel), "OnCreate", Type.EmptyTypes, "ActionOnCreate");
            Patch(typeof(UnitActionPropArtifactShapeMade), "OnCreate", Type.EmptyTypes, "ActionOnCreate");
                    break;
                case "mp":
            Patch(typeof(UnitDataBase), "AddMP", new[] { typeof(int), typeof(bool) }, "AddMp");
            Patch(typeof(UnitDataBase), "AddSP", new[] { typeof(int), typeof(bool) }, "AddSp");
                    break;
                default:
                    patchErrors.Add("unknown group " + group);
                    break;
            }
        }

        private static string Who(WorldUnitBase u)
        {
            return u == null ? "?" : u.Pointer == Me().Pointer ? "P" : "N";
        }

        private static string PdText(DataProps.PropsData pd)
        {
            return pd == null ? "null" : pd.propsID + "x" + pd.propsCount;
        }

        private static bool IsPlayerProps(DataProps d)
        {
            return d != null && d.Pointer == Me().data.unitData.propData.Pointer;
        }

        private static bool IsPlayerBattle(UnitDataBase d)
        {
            return d != null && RateService.InBattle && d.Pointer == SceneType.battle.battleMap.playerUnitCtrl.data.Pointer;
        }

        private static void UseOnCreate(UnitActionPropUse __instance)
        {
            Add("PropUse.OnCreate " + Who(__instance.unit) + " " + PdText(__instance.propsData));
            protectedSoles.Clear();
            if (IsMe(__instance.unit) && __instance.propsData != null)
            {
                protectedSoles.Add(__instance.propsData.soleID);
            }
        }

        private static void StudyOnCreate(UnitActionPropMartialStudy __instance)
        {
            ActionOnCreate(__instance);
            protectedSoles.Clear();
            if (IsMe(__instance.unit) && __instance.martialData != null && __instance.martialData.data != null)
            {
                protectedSoles.Add(__instance.martialData.data.soleID);
            }
            Add("  protect " + string.Join(",", new List<string>(protectedSoles).ToArray()));
        }

        private static void UpGradeOnCreate(UnitActionRoleUpGrade __instance)
        {
            ActionOnCreate(__instance);
            protectedSoles.Clear();
            if (IsMe(__instance.unit))
            {
                AddAll(__instance.requirePropsSoleID);
                AddAll(__instance.rarityPropsSoleID);
            }
            Add("  protect " + string.Join(",", new List<string>(protectedSoles).ToArray()));
        }

        private static void AddAll(UnhollowerBaseLib.Il2CppStringArray soles)
        {
            for (int i = 0; soles != null && i < soles.Length; i++)
            {
                protectedSoles.Add(soles[i]);
            }
        }

        private static void ProtectEnd()
        {
            protectedSoles.Clear();
        }

        private static bool IsMe(WorldUnitBase u)
        {
            return u != null && u.Pointer == Me().Pointer;
        }

        private static bool UseCost(UnitActionPropUse __instance)
        {
            Add("PropUse.CostPropItem " + Who(__instance.unit) + " " + PdText(__instance.propsData) + (blockUseCost ? " BLOCKED" : ""));
            return !(blockUseCost && __instance.unit != null && __instance.unit.Pointer == Me().Pointer);
        }

        private static void WudCostId(WorldUnitData __instance, int __0, int __1)
        {
            Add("WUD.CostPropItem(id) " + Who(__instance.unit) + " " + __0 + "x" + __1);
        }

        private static void WudCostList(WorldUnitData __instance, Il2CppSystem.Collections.Generic.List<DataProps.PropsData> __0)
        {
            var parts = new List<string>();
            for (int i = 0; __0 != null && i < __0.Count; i++)
            {
                parts.Add(PdText(__0[i]));
            }
            Add("WUD.CostPropItem(list) " + Who(__instance.unit) + " " + string.Join(",", parts.ToArray()));
        }

        private static bool WudCostPd(WorldUnitData __instance, DataProps.PropsData __0)
        {
            bool skip = blockSole && __0 != null && protectedSoles.Contains(__0.soleID) && IsMe(__instance.unit);
            Add("WUD.CostPropItem(pd) " + Who(__instance.unit) + " " + PdText(__0) + (skip ? " BLOCKED" : ""));
            return !skip;
        }

        private static bool WudCostSole(WorldUnitData __instance, string __0, int __1)
        {
            bool skip = blockSole && protectedSoles.Contains(__0) && IsMe(__instance.unit);
            Add("WUD.CostPropItem(sole) " + Who(__instance.unit) + " " + __0 + "x" + __1 + (skip ? " BLOCKED" : ""));
            return !skip;
        }

        private static void WudCostItem(WorldUnitData __instance, int __0)
        {
            Add("WUD.CostItem(id) " + Who(__instance.unit) + " " + __0);
        }

        private static void WudCostItems(WorldUnitData __instance)
        {
            Add("WUD.CostItem(list) " + Who(__instance.unit));
        }

        private static void DelSole(DataProps __instance, string __0)
        {
            Add("DataProps.DelProps(sole) " + (IsPlayerProps(__instance) ? "P" : "N") + " " + __0);
        }

        private static void DelSoleCount(DataProps __instance, string __0, int __1)
        {
            Add("DataProps.DelProps(sole,n) " + (IsPlayerProps(__instance) ? "P" : "N") + " " + __0 + "x" + __1);
        }

        private static void DelId(DataProps __instance, int __0, int __1)
        {
            Add("DataProps.DelProps(id,n) " + (IsPlayerProps(__instance) ? "P" : "N") + " " + __0 + "x" + __1);
        }

        private static void DelItem(DataProps __instance, DataProps.PropsData __0)
        {
            Add("DataProps.DelPropsItem " + (IsPlayerProps(__instance) ? "P" : "N") + " " + PdText(__0));
        }

        private static void SetCount(DataProps.PropsData __instance, int __0)
        {
            if (tracing && __0 < __instance.propsCount)
            {
                Add("PropsData.set_propsCount " + __instance.propsID + " " + __instance.propsCount + "->" + __0);
            }
        }

        private static void ActionOnCreate(UnitActionBase __instance)
        {
            Add(__instance.GetIl2CppType().Name + ".OnCreate " + Who(__instance.unit));
        }

        private static bool AddMp(UnitDataBase __instance, int __0)
        {
            if (!IsPlayerBattle(__instance) || __0 >= 0)
            {
                return true;
            }
            Add("AddMP " + __0 + " mp=" + __instance.mp + (blockMp ? " BLOCKED" : ""));
            return !blockMp;
        }

        private static bool AddSp(UnitDataBase __instance, int __0)
        {
            if (!IsPlayerBattle(__instance) || __0 >= 0)
            {
                return true;
            }
            Add("AddSP " + __0 + " sp=" + __instance.sp + (blockMp ? " BLOCKED" : ""));
            return !blockMp;
        }

        private static void SetMp(UnitDataBase __instance, int __0)
        {
            if (tracing && IsPlayerBattle(__instance) && __0 < __instance.mp)
            {
                Add("set_mp " + __instance.mp + "->" + __0);
            }
        }

        private static void SetSp(UnitDataBase __instance, int __0)
        {
            if (tracing && IsPlayerBattle(__instance) && __0 < __instance.sp)
            {
                Add("set_sp " + __instance.sp + "->" + __0);
            }
        }
    }
}
