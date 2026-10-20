using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 트레이너 비교 2묶음 설계(docs/superpowers/specs/2026-10-20-trainer-batch2-design.md 4절)의 설계 전 실험 E1~E6.
    //   tb2mlist                        플레이어 공법: sole·기술·분류·옵션(자리:번호:품질)
    //   tb2slot <sole> <자리>           그 공법의 옵션 후보와 그 자리의 가중치(0이면 자리 밖)
    //   tb2force <sole> <자리> <번호>   자리 판정 없이 SetPrefixID 뒤 옵션 줄을 다시 읽는다
    //   tb2trace on|read|clear          성공률 후보 판정에 기록용 접미(이름마다 처음 30번까지 줄, 그 뒤는 수만 센다)
    //   tb2set <이름> <값|off>          그 판정의 반환값을 덮어쓴다(이름은 trace의 이름)
    //   tb2npc [n]                      가까운 NPC n명: 번호·unitID·이름·가방 첫 아이템 sole
    //   tb2escape <npc 번호> / tb2steal <npc 번호> / tb2rel <npc 번호> <관계 표 id>   행동을 만들어 IsCreateAction·CreateAction, 결과 칸
    //   tb2relitems                     관계 표(roleRelation): id·type·group·이름
    //   tb2relof <npc 번호>             플레이어가 본 그 NPC와의 관계 종류(relationData.GetRelation)
    //   sstate                          성공·드롭 탭의 상태와 드롭 개발 계수(GetDropID 호출 수·돌려준 보상 번호 수)
    //   tb2pick <sole>                  공법 탭에서 그 공법을 고른다(검사용)
    //   tb2escape <npc 번호> sure       행동의 isSureComplete를 켜고 만든다
    //   tb2drop [값]                     지금 난이도의 드롭률(g.conf.gameDifficultyValue.curItem.dropRate)을 읽거나 쓴다
    //   관계 점수 추적: FormulaTool.Relation.Score(관계 종류|어울림 줄, unit, toUnit), ScoreForceMin, UnitAllRelationScore
    //   tb2invite <npc 번호> [force]    UnitActionRoleInvite(초빙)를 만든다. force면 forceAgree를 켠다
    //   tb2npcset <npc 번호> <능력치 키> <값>   NPC의 저장값(AttributeCatalog의 키, 예: footSpeed)
    //   추적에 더함: 연요호 흡수(DevilDemonAbsorb.GetAbsorbRate·CheckMonstCanAbsorb·AbsorbCheck·AbsorbComplete),
    //               드롭(UIBattleInfoDrop.OnUnitDie, ConfBattleUnitAttr.GetDropID), 초빙(UnitActionRoleInvite.PlayerToNPCAction)
    //   덮어쓰기 이름 더함: DropTimes(GetDropID 결과를 n번 이어 붙임), CanCollect(ConfPotmonBase.IsMonstCanCollect를 참·호요 1001로),
    //               ForceConsent(WorldUnitEffectSpecialNpcRelationCommand.ForceConsent 결과). 추적: RewardTool.GetReward
    //               RelRequire·RelRep(플레이어가 요청한 관계 응답 UnitActionFeedback104 안에서만 필요 점수·필요 명성),
    //               InviteRequire(플레이어의 종문 초빙 응답 UnitActionFeedback115 안에서만 requireScore·eliteRequireScore 배열 전부)
    internal static class DevCommandsTrainerBatch2
    {
        private static HarmonyLib.Harmony harmony;
        private static bool tracing;
        private static readonly List<string> log = new List<string>();
        private static readonly Dictionary<string, int> calls = new Dictionary<string, int>();
        private static readonly Dictionary<string, double> overrides = new Dictionary<string, double>();
        private static readonly List<string> patchErrors = new List<string>();

        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "tb2relof":
                    {
                        WorldUnitBase to = Near(int.Parse(p[1]) + 1)[int.Parse(p[1])];
                        var rel = Me().data.unitData.relationData;
                        string id = to.data.unitData.unitID;
                        return id + " relation=" + rel.GetRelation(to) + " brother=" + rel.brother.Contains(id) + " brotherBack=" + rel.brotherBack.Contains(id)
                            + " | npc: relation=" + to.data.unitData.relationData.GetRelation(Me()) + " brother=" + to.data.unitData.relationData.brother.Contains(Me().data.unitData.unitID)
                            + " brotherBack=" + to.data.unitData.relationData.brotherBack.Contains(Me().data.unitData.unitID)
                            + " myBrothers=" + rel.brother.Count + "/" + rel.brotherBack.Count;
                    }
                case "sstick":
                    SuccessService.DevStick();
                    return "stuck";
                case "sstate":
                    return "steal=" + SuccessService.Steal + " relation=" + SuccessService.Relation + " drop=" + SuccessService.DropTimes
                        + " brotherRequire=" + g.conf.npcFeedback104.requireScoreBrother + " rep1=" + g.conf.npcFeedback104.requireReputationGrade1
                        + " dropCalls=" + SuccessService.DropCalls + " dropIds=" + SuccessService.DropIds + " dropRaw=" + SuccessService.DropIdsRaw;
                case "tb2pick":
                    MartialTab.DevPick = p[1];
                    EditorWindow.Refresh();
                    return "pick " + p[1];
                case "tb2mlist":
                    return MList();
                case "tb2slot":
                    return Slot(p[1], int.Parse(p[2]));
                case "tb2force":
                    return Force(p[1], int.Parse(p[2]), int.Parse(p[3]));
                case "tb2trace":
                    return Trace(p[1]);
                case "tb2set":
                    if (p[2] == "off")
                    {
                        overrides.Remove(p[1]);
                    }
                    else
                    {
                        overrides[p[1]] = double.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture);
                    }
                    EnsurePatched();
                    return "overrides: " + string.Join(", ", new List<string>(Pairs()).ToArray());
                case "tb2npc":
                    return Npcs(p.Length > 1 ? int.Parse(p[1]) : 5);
                case "tb2escape":
                    return Escape(int.Parse(p[1]), p.Length > 2 && p[2] == "sure");
                case "tb2drop":
                    {
                        ConfGameDifficultyValueItem cur = g.conf.gameDifficultyValue.curItem;
                        int before = cur.dropRate;
                        if (p.Length > 1)
                        {
                            cur.dropRate = int.Parse(p[1]);
                        }
                        return "difficulty=" + cur.id + " dropRate " + before + " -> " + cur.dropRate;
                    }
                case "tb2steal":
                    return Steal(int.Parse(p[1]));
                case "tb2rel":
                    return Relation(int.Parse(p[1]), int.Parse(p[2]));
                case "tb2relitems":
                    return RelItems();
                case "tb2invite":
                    return Invite(int.Parse(p[1]), p.Length > 2 && p[2] == "force");
                case "tb2npcset":
                    return NpcSet(int.Parse(p[1]), p[2], int.Parse(p[3]));
                default:
                    return null;
            }
        }

        private static IEnumerable<string> Pairs()
        {
            foreach (var pair in overrides)
            {
                yield return pair.Key + "=" + pair.Value;
            }
        }

        private static WorldUnitBase Me()
        {
            return g.world.playerUnit;
        }

        // ---- E1 공법 옵션 ----

        private static string MList()
        {
            var sb = new StringBuilder();
            foreach (var pair in Me().data.unitData.allActionMartial)
            {
                DataProps.MartialData m = pair.Value.data.To<DataProps.MartialData>();
                sb.Append(pair.Key).Append(' ').Append(m.baseID).Append(' ').Append(m.martialType).Append(' ');
                foreach (MartialService.PrefixView v in MartialService.Prefixes(m))
                {
                    sb.Append(v.Index).Append(':').Append(v.Number).Append(':').Append(v.Level).Append(' ');
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static string Slot(string sole, int index)
        {
            DataProps.MartialData m = MartialService.Martial(Me(), sole);
            if (m == null)
            {
                return "no martial " + sole;
            }
            var sb = new StringBuilder();
            foreach (ConfBattleSkillPrefixValueItem item in MartialService.ValidItems(m))
            {
                sb.Append(item.number).Append(":w").Append(MartialService.Weight(item, index)).Append(":m").Append(item.multi).Append(":r").Append(item.require).Append(' ');
            }
            return sb.ToString();
        }

        private static string Force(string sole, int index, int number)
        {
            DataProps.MartialData m = MartialService.Martial(Me(), sole);
            if (m == null)
            {
                return "no martial " + sole;
            }
            var sb = new StringBuilder("before ");
            foreach (MartialService.PrefixView v in MartialService.Prefixes(m))
            {
                sb.Append(v.Index).Append(':').Append(v.Number).Append(':').Append(v.Level).Append(' ');
            }
            try
            {
                m.SetPrefixID(index, number);
            }
            catch (Exception e)
            {
                return sb + " SetPrefixID ERROR " + e.GetType().Name + ": " + e.Message;
            }
            sb.Append("\nafter  ");
            try
            {
                foreach (MartialService.PrefixView v in MartialService.Prefixes(m))
                {
                    sb.Append(v.Index).Append(':').Append(v.Number).Append(':').Append(v.Level).Append('/').Append(v.MaxLevel).Append(' ');
                }
            }
            catch (Exception e)
            {
                sb.Append(" read ERROR ").Append(e.GetType().Name).Append(": ").Append(e.Message);
            }
            return sb.ToString();
        }

        // ---- 성공률 추적 ----

        private static string Trace(string what)
        {
            if (what == "on")
            {
                EnsurePatched();
                tracing = true;
                return "tracing on, patch errors=" + patchErrors.Count + (patchErrors.Count > 0 ? " " + string.Join(" | ", patchErrors.ToArray()) : "");
            }
            if (what == "clear")
            {
                log.Clear();
                calls.Clear();
                return "cleared";
            }
            var sb = new StringBuilder();
            foreach (var pair in calls)
            {
                sb.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
            }
            return "calls: " + sb + "\n" + string.Join("\n", log.ToArray());
        }

        private static void Add(string name, string line)
        {
            if (!tracing)
            {
                return;
            }
            int n;
            calls.TryGetValue(name, out n);
            calls[name] = n + 1;
            if (n < 30 && log.Count < 600)
            {
                log.Add("#" + UnityEngine.Time.frameCount + " " + name + " " + line);
            }
        }

        private static void Patch(Type type, string name, Type[] args, string prefix, string postfix)
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
                harmony.Patch(target,
                    prefix == null ? null : new HarmonyLib.HarmonyMethod(typeof(DevCommandsTrainerBatch2).GetMethod(prefix, flags)),
                    postfix == null ? null : new HarmonyLib.HarmonyMethod(typeof(DevCommandsTrainerBatch2).GetMethod(postfix, flags)));
            }
            catch (Exception e)
            {
                patchErrors.Add(type.Name + "." + name + " " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static void EnsurePatched()
        {
            if (harmony != null)
            {
                return;
            }
            harmony = new HarmonyLib.Harmony("TlToybox.dev.tb2");
            Type unit = typeof(WorldUnitBase);
            Patch(typeof(UnitActionRoleEscape), "CompleteRate", new[] { typeof(int), typeof(int), typeof(int) }, null, "EscapeRate");
            Patch(typeof(UnitActionRoleEscape), "get_isComplete", Type.EmptyTypes, null, "EscapeComplete");
            Patch(typeof(UnitActionRoleEscape), "OnCreate", Type.EmptyTypes, "ActBegin", "ActEnd");
            Patch(typeof(UnitActionRoleStealItem), "CompleteRate", new[] { unit, unit }, null, "StealRate");
            Patch(typeof(UnitActionRoleStealItem), "DiscoverRate", new[] { unit }, null, "StealDiscover");
            Patch(typeof(UnitActionRoleStealItem), "OnCreate", Type.EmptyTypes, "ActBegin", "ActEnd");
            Patch(typeof(ConfDevilDemonParamerBase), "get_collectMonstRate", Type.EmptyTypes, null, "CollectRate");
            Patch(typeof(ConfPotmonSkillStudyItem), "get_studyRate", Type.EmptyTypes, null, "PotStudy");
            Patch(typeof(ConfPotmonSkillStudyItem), "get_battleStudyRate", Type.EmptyTypes, null, "PotBattleStudy");
            Patch(typeof(ConfGameDifficultyValueItem), "get_dropRate", Type.EmptyTypes, null, "DropRate");
            Patch(typeof(PlayerInfoExtendPropsEx), "CanDrop", new[] { typeof(DataProps.PropsData), unit }, null, "CanDrop");
            Patch(typeof(FormulaTool.Relation), "UnitSchooldScore", new[] { unit, unit, typeof(int) }, null, "SchoolScore");
            Patch(typeof(UnitActionFeedback115), "OnCreate", Type.EmptyTypes, "ActBegin", "ActEnd");
            Patch(typeof(UnitActionRoleRelation), "get_isComplete", Type.EmptyTypes, null, "RelComplete");
            Patch(typeof(UnitActionRoleRelation), "PlayerToNPCAction", Type.EmptyTypes, "RelBegin", null);
            Patch(typeof(FormulaTool.Relation), "Score", new[] { typeof(UnitBothRelationType), unit, unit }, null, "RelScoreType");
            Patch(typeof(FormulaTool.Relation), "Score", new[] { typeof(ConfNpcPartFittingItem), unit, unit }, null, "RelScoreFit");
            Patch(typeof(FormulaTool.Relation), "ScoreForceMin", new[] { typeof(ConfNpcPartFittingItem), unit, unit }, null, "RelScoreMin");
            Patch(typeof(FormulaTool.Relation), "UnitAllRelationScore", new[] { unit, unit }, null, "RelAllScore");
            Patch(typeof(DevilDemonAbsorb), "GetAbsorbRate", new[] { typeof(UnitCtrlMonst) }, null, "AbsorbRate");
            Patch(typeof(DevilDemonAbsorb), "CheckMonstCanAbsorb", new[] { typeof(UnitCtrlMonst) }, null, "AbsorbCan");
            Patch(typeof(DevilDemonAbsorb), "AbsorbCheck", new[] { typeof(UnitCtrlBase) }, "AbsorbCheck", null);
            Patch(typeof(DevilDemonAbsorb), "AbsorbComplete", Type.EmptyTypes, "AbsorbDone", null);
            Patch(typeof(UIBattleInfoDrop), "OnUnitDie", new[] { typeof(ETypeData) }, "DropDieBegin", "DropDieEnd");
            Patch(typeof(ConfBattleUnitAttr), "GetDropID", new[] { typeof(int), typeof(bool) }, null, "DropId");
            Patch(typeof(ConfPotmonBase), "IsMonstCanCollect", new[] { typeof(int), typeof(int), typeof(int).MakeByRefType() }, null, "CanCollect");
            Patch(typeof(WorldUnitEffectSpecialNpcRelationCommand), "ForceConsent", new[] { unit }, null, "ForceConsent");
            Patch(typeof(UnitActionFeedback104), "OnCreate", Type.EmptyTypes, "Fb104Begin", "Fb104End");
            foreach (string g104 in new[] { "Parents", "Children", "Brother", "Master", "Student", "Marry", "Lover" })
            {
                Patch(typeof(ConfNpcFeedback104Base), "get_requireScore" + g104, Type.EmptyTypes, null, "Fb104Require");
            }
            Patch(typeof(ConfNpcFeedback104Base), "get_schoolRequireScoreMaster", Type.EmptyTypes, null, "Fb104Require");
            Patch(typeof(ConfNpcFeedback104Base), "get_schoolRequireScoreStudent", Type.EmptyTypes, null, "Fb104Require");
            Patch(typeof(ConfNpcFeedback104Base), "get_closeRequire", Type.EmptyTypes, null, "Fb104Close");
            Patch(typeof(ConfNpcFeedback104Base), "get_schoolCloseRequire", Type.EmptyTypes, null, "Fb104Close");
            for (int r = 1; r <= 10; r++)
            {
                Patch(typeof(ConfNpcFeedback104Base), "get_requireReputationGrade" + r, Type.EmptyTypes, null, "Fb104Rep");
            }
            Patch(typeof(UnitActionFeedback115), "OnCreate", Type.EmptyTypes, "Fb115Begin", "Fb115End");
            Patch(typeof(ConfNpcFeedback115Base), "get_requireScore", Type.EmptyTypes, null, "Fb115Require");
            Patch(typeof(ConfNpcFeedback115Base), "get_eliteRequireScore", Type.EmptyTypes, null, "Fb115Require");
            Patch(typeof(RewardTool), "GetReward", new[] { unit, typeof(int), typeof(int), typeof(UnhollowerBaseLib.Il2CppReferenceArray<RewardMartialGradeBase>), typeof(RewardCustomData), typeof(RandomSeedData) }, null, "GetReward");
            Patch(typeof(UnitActionRoleInvite), "PlayerToNPCAction", Type.EmptyTypes, "InviteBegin", null);
        }

        private static string Who(WorldUnitBase u)
        {
            return u == null ? "?" : u.Pointer == Me().Pointer ? "P" : "N:" + u.data.unitData.unitID;
        }

        private static bool Over(string name, out double v)
        {
            return overrides.TryGetValue(name, out v);
        }

        private static void EscapeRate(int __0, int __1, int __2, ref float __result)
        {
            double v;
            Add("EscapeRate", "luck=" + __0 + " foot=" + __1 + " toFoot=" + __2 + " -> " + __result);
            if (Over("EscapeRate", out v))
            {
                __result = (float)v;
            }
        }

        private static void EscapeComplete(UnitActionRoleEscape __instance, ref bool __result)
        {
            double v;
            Add("EscapeComplete", Who(__instance.unit) + " sure=" + __instance.isSureComplete + " -> " + __result);
            if (Over("EscapeComplete", out v))
            {
                __result = v != 0;
            }
        }

        private static void RelComplete(UnitActionRoleRelation __instance, ref bool __result)
        {
            double v;
            Add("RelComplete", Who(__instance.unit) + " type=" + __instance.relationType + " -> " + __result);
            if (Over("RelComplete", out v))
            {
                __result = v != 0;
            }
        }

        private static void RelBegin(UnitActionRoleRelation __instance)
        {
            Add("Act", "PlayerToNPCAction " + Who(__instance.unit));
        }

        private static void StealRate(WorldUnitBase __0, WorldUnitBase __1, ref float __result)
        {
            double v;
            Add("StealRate", Who(__0) + " from " + Who(__1) + " -> " + __result);
            if (Over("StealRate", out v))
            {
                __result = (float)v;
            }
        }

        private static void StealDiscover(WorldUnitBase __0, ref float __result)
        {
            double v;
            Add("StealDiscover", Who(__0) + " -> " + __result);
            if (Over("StealDiscover", out v))
            {
                __result = (float)v;
            }
        }

        private static void CollectRate(ref float __result)
        {
            double v;
            Add("CollectRate", "-> " + __result);
            if (Over("CollectRate", out v))
            {
                __result = (float)v;
            }
        }

        private static void PotStudy(ref int __result)
        {
            double v;
            Add("PotStudy", "-> " + __result);
            if (Over("PotStudy", out v))
            {
                __result = (int)v;
            }
        }

        private static void PotBattleStudy(ref int __result)
        {
            double v;
            Add("PotBattleStudy", "-> " + __result);
            if (Over("PotBattleStudy", out v))
            {
                __result = (int)v;
            }
        }

        private static void DropRate(ref int __result)
        {
            double v;
            Add("DropRate", "-> " + __result + " battle=" + RateService.InBattle);
            if (Over("DropRate", out v))
            {
                __result = (int)v;
            }
        }

        private static void CanDrop(DataProps.PropsData __0, WorldUnitBase __1, ref bool __result)
        {
            double v;
            Add("CanDrop", (__0 == null ? "null" : __0.propsID + "x" + __0.propsCount) + " " + Who(__1) + " -> " + __result);
            if (Over("CanDrop", out v))
            {
                __result = v != 0;
            }
        }

        private static void SchoolScore(WorldUnitBase __0, WorldUnitBase __1, int __2, ref int __result)
        {
            double v;
            Add("SchoolScore", Who(__0) + " to " + Who(__1) + " a=" + __2 + " -> " + __result);
            if (Over("SchoolScore", out v))
            {
                __result = (int)v;
            }
        }

        private static void ActBegin(UnitActionBase __instance)
        {
            Add("Act", __instance.GetIl2CppType().Name + " begin " + Who(__instance.unit));
        }

        private static void ActEnd(UnitActionBase __instance)
        {
            Add("Act", __instance.GetIl2CppType().Name + " end");
        }

        private static void RelScoreType(UnitBothRelationType __0, WorldUnitBase __1, WorldUnitBase __2, ref int __result)
        {
            double v;
            Add("RelScoreType", __0 + " " + Who(__1) + " to " + Who(__2) + " -> " + __result);
            if (Over("RelScoreType", out v))
            {
                __result = (int)v;
            }
        }

        private static void RelScoreFit(ConfNpcPartFittingItem __0, WorldUnitBase __1, WorldUnitBase __2, ref int __result)
        {
            double v;
            Add("RelScoreFit", (__0 == null ? "-" : __0.id.ToString()) + " " + Who(__1) + " to " + Who(__2) + " -> " + __result);
            if (Over("RelScoreFit", out v))
            {
                __result = (int)v;
            }
        }

        private static void RelScoreMin(ConfNpcPartFittingItem __0, WorldUnitBase __1, WorldUnitBase __2, ref int __result)
        {
            double v;
            Add("RelScoreMin", (__0 == null ? "-" : __0.id.ToString()) + " " + Who(__1) + " to " + Who(__2) + " -> " + __result);
            if (Over("RelScoreMin", out v))
            {
                __result = (int)v;
            }
        }

        private static void RelAllScore(WorldUnitBase __0, WorldUnitBase __1, ref int __result)
        {
            double v;
            Add("RelAllScore", Who(__0) + " to " + Who(__1) + " -> " + __result);
            if (Over("RelAllScore", out v))
            {
                __result = (int)v;
            }
        }

        private static void AbsorbRate(UnitCtrlMonst __0, ref int __result)
        {
            double v;
            Add("AbsorbRate", "-> " + __result);
            if (Over("AbsorbRate", out v))
            {
                __result = (int)v;
            }
        }

        private static void AbsorbCan(UnitCtrlMonst __0, ref bool __result)
        {
            double v;
            Add("AbsorbCan", "-> " + __result);
            if (Over("AbsorbCan", out v))
            {
                __result = v != 0;
            }
        }

        private static void AbsorbCheck()
        {
            Add("AbsorbCheck", "");
        }

        private static void AbsorbDone()
        {
            Add("AbsorbDone", "");
        }

        private static void DropDieBegin()
        {
            Add("DropDie", "begin");
        }

        private static void DropDieEnd(UIBattleInfoDrop __instance)
        {
            Add("DropDie", "end curRoomAllDrop=" + (__instance.curRoomAllDrop == null ? -1 : __instance.curRoomAllDrop.Count));
        }

        private static void DropId(int __0, bool __1, ref string __result)
        {
            double v;
            Add("DropId", __0 + " die=" + __1 + " -> " + __result);
            if (Over("DropTimes", out v) && !string.IsNullOrEmpty(__result))
            {
                var parts = new List<string>();
                for (int i = 0; i < (int)v; i++)
                {
                    parts.Add(__result);
                }
                __result = string.Join("|", parts.ToArray());
            }
        }

        private static void CanCollect(int __0, int __1, ref int __2, ref bool __result)
        {
            double v;
            Add("CanCollect", "attr=" + __0 + " base=" + __1 + " -> " + __result + " potmon=" + __2);
            if (Over("CanCollect", out v) && v != 0)
            {
                __result = true;
                if (__2 <= 0)
                {
                    __2 = 1001;
                }
            }
        }

        private static void ForceConsent(WorldUnitBase __0, ref bool __result)
        {
            double v;
            Add("ForceConsent", Who(__0) + " -> " + __result);
            if (Over("ForceConsent", out v))
            {
                __result = v != 0;
            }
        }

        private static void GetReward(WorldUnitBase __0, int __1, int __2)
        {
            Add("GetReward", Who(__0) + " reward=" + __1 + " weight=" + __2);
        }

        private static bool fb104Player, fb115Player;

        private static void Fb104Begin(UnitActionFeedback104 __instance)
        {
            fb104Player = __instance.requestUnit != null && __instance.requestUnit.Pointer == Me().Pointer;
            Add("Fb104", "begin request=" + Who(__instance.requestUnit) + " npc=" + Who(__instance.unit) + " type=" + __instance.relationType);
        }

        private static void Fb104End()
        {
            fb104Player = false;
            Add("Fb104", "end");
        }

        private static void Fb104Require(ref int __result)
        {
            double v;
            Add("Fb104Require", "player=" + fb104Player + " -> " + __result);
            if (fb104Player && Over("RelRequire", out v))
            {
                __result = (int)v;
            }
        }

        private static void Fb104Close(ref int __result)
        {
            double v;
            Add("Fb104Close", "player=" + fb104Player + " -> " + __result);
            if (fb104Player && Over("RelClose", out v))
            {
                __result = (int)v;
            }
        }

        private static void Fb104Rep(ref int __result)
        {
            double v;
            Add("Fb104Rep", "player=" + fb104Player + " -> " + __result);
            if (fb104Player && Over("RelRep", out v))
            {
                __result = (int)v;
            }
        }

        private static void Fb115Begin(UnitActionFeedback115 __instance)
        {
            fb115Player = __instance.inviteUnit != null && __instance.inviteUnit.Pointer == Me().Pointer;
            Add("Fb115", "begin invite=" + Who(__instance.inviteUnit) + " npc=" + Who(__instance.unit));
        }

        private static void Fb115End()
        {
            fb115Player = false;
        }

        private static void Fb115Require(ref UnhollowerBaseLib.Il2CppStructArray<int> __result)
        {
            double v;
            Add("Fb115Require", "player=" + fb115Player + " n=" + (__result == null ? -1 : __result.Length));
            if (fb115Player && __result != null && Over("InviteRequire", out v))
            {
                var copy = new UnhollowerBaseLib.Il2CppStructArray<int>(__result.Length);
                for (int i = 0; i < copy.Length; i++)
                {
                    copy[i] = (int)v;
                }
                __result = copy;
            }
        }

        private static void InviteBegin(UnitActionRoleInvite __instance)
        {
            Add("Act", "Invite.PlayerToNPCAction " + Who(__instance.unit) + " force=" + __instance.forceAgree);
        }

        // ---- 행동 만들기 ----

        private static List<WorldUnitBase> Near(int n)
        {
            var result = new List<WorldUnitBase>();
            var units = g.world.unit.GetUnits(false);
            for (int i = 0; i < units.Count && result.Count < n; i++)
            {
                if (units[i].Pointer != Me().Pointer)
                {
                    result.Add(units[i]);
                }
            }
            return result;
        }

        private static string FirstSole(WorldUnitBase u)
        {
            var all = u.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].propsID != PropsIDType.Money)
                {
                    return all[i].soleID;
                }
            }
            return null;
        }

        private static string Npcs(int n)
        {
            var sb = new StringBuilder();
            List<WorldUnitBase> near = Near(n);
            for (int i = 0; i < near.Count; i++)
            {
                sb.Append(i).Append(' ').Append(near[i].data.unitData.unitID).Append(' ').Append(EditService.NameOf(near[i]))
                  .Append(" first=").Append(FirstSole(near[i])).Append(" props=").Append(near[i].data.unitData.propData.allProps.Count).Append('\n');
            }
            return sb.ToString();
        }

        private static string Run(UnitActionBase action, string tail)
        {
            action.unit = Me();
            int can = Me().IsCreateAction(action, false);
            int code = can == 0 ? Me().CreateAction(action, false) : -1;
            return "IsCreateAction=" + can + " CreateAction=" + code + " " + tail;
        }

        private static string Escape(int index, bool sure)
        {
            WorldUnitBase to = Near(index + 1)[index];
            var action = new UnitActionRoleEscape(to, "");
            if (sure)
            {
                action.isSureComplete = true;
            }
            return Run(action, "") + " sure=" + action.isSureComplete;
        }

        private static string Steal(int index)
        {
            WorldUnitBase to = Near(index + 1)[index];
            string sole = FirstSole(to);
            int mine = 0;
            var before = Me().data.unitData.propData.allProps.Count;
            var action = new UnitActionRoleStealItem(to, sole);
            string r = Run(action, "");
            mine = Me().data.unitData.propData.allProps.Count;
            return r + " target sole=" + sole + " my slots " + before + " -> " + mine + " theirs has=" + (to.data.unitData.propData.GetPropsNum(sole) > 0);
        }

        private static string Relation(int index, int relationId)
        {
            WorldUnitBase to = Near(index + 1)[index];
            ConfRoleRelationItem item = g.conf.roleRelation.GetItem(relationId);
            if (item == null)
            {
                return "no relation item " + relationId;
            }
            var action = new UnitActionRoleRelation(to, (UnitRelationType)item.type, item);
            return Run(action, "") + " complete=" + action.isComplete + " setComplete=" + action.isSetComplete + " addIntim=" + action.addIntim;
        }

        private static string Invite(int index, bool force)
        {
            WorldUnitBase to = Near(index + 1)[index];
            var action = new UnitActionRoleInvite(to);
            if (force)
            {
                action.forceAgree = true;
            }
            return Run(action, "") + " complete=" + action.isComplete + " inviteComplete=" + action.isInviteComplete;
        }

        private static string NpcSet(int index, string key, int value)
        {
            WorldUnitBase to = Near(index + 1)[index];
            AttributeDef def = AttributeCatalog.All.Find(d => d.Key == key);
            if (def == null)
            {
                return "no attribute " + key;
            }
            var p = to.data.unitData.propertyData;
            int before = def.Get(p);
            def.Set(p, value);
            return to.data.unitData.unitID + " " + key + " " + before + " -> " + def.Get(p) + " (dyn " + def.DynValue(to.data.dynUnitData) + ")";
        }

        private static string RelItems()
        {
            var sb = new StringBuilder();
            var all = g.conf.roleRelation._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                sb.Append(all[i].id).Append(":t").Append(all[i].type).Append(":g").Append(all[i].group).Append(':').Append(SafeLs(all[i].relation)).Append(' ');
            }
            return sb.ToString();
        }

        private static string SafeLs(string key)
        {
            try
            {
                return string.IsNullOrEmpty(key) ? "" : GameTool.LS(key);
            }
            catch (Exception)
            {
                return key;
            }
        }
    }
}
