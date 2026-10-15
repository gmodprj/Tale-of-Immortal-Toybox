using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 도호 탭의 검증용 명령(tools/research/ui-check.ps1의 appellation 부분).
    //   ttl                            : me=<unitID> owned=<번호,…> equipped=<번호|0> talent=<오성> popup=<획득 창이 열렸는지> silenced=<편집기가 닫은 창 수> grade=<대경지>
    //   ttlgive|ttldel|ttlequip <번호> : ok=<성공> | <문장> | <ttl>
    //   ttlunequip                     : ok=<성공> | <문장> | <ttl>
    //   ttlholders <번호>              : holders=<수> names=<이름(unitID)>,…(플레이어 포함)
    //   ttlnpcgive <unitID|any> <번호> : 검사 준비. 그 NPC(any면 그 도호가 없는 첫 NPC)에게 게임 함수로 바로 넣는다. unit=<unitID> ok=<결과>
    //   ttlrewardopen                  : 검사 준비. 도호가 아닌 일반 보상 창(아이템 1011011 하나)을 연다
    //   ttlrewardclose                 : 열린 보상 창의 확인을 누른다. closed=<눌렀는지>
    //   (ttl의 reward=<도호 칸이 보이는 보상 창, 곧 "도호 상실" 창이 열렸는지> rewardAny=<보상 창이 열렸는지>)
    internal static class DevCommandsAppellation
    {
        public static string Execute(string[] p)
        {
            string text;
            bool ok;
            switch (p[0])
            {
                case "ttl":
                    return State();
                case "ttlgive":
                    ok = AppellationService.Give(g.world.playerUnit, int.Parse(p[1]), out text);
                    return "ok=" + ok + " | " + text + " | " + State();
                case "ttldel":
                    ok = AppellationService.Remove(g.world.playerUnit, int.Parse(p[1]), out text);
                    return "ok=" + ok + " | " + text + " | " + State();
                case "ttlequip":
                    ok = AppellationService.Equip(g.world.playerUnit, int.Parse(p[1]), out text);
                    return "ok=" + ok + " | " + text + " | " + State();
                case "ttlunequip":
                    ok = AppellationService.Unequip(g.world.playerUnit, out text);
                    return "ok=" + ok + " | " + text + " | " + State();
                case "ttlholders":
                    return Holders(int.Parse(p[1]));
                case "ttlnpcgive":
                    return NpcGive(p[1], int.Parse(p[2]));
                case "ttlrewardopen":
                    g.ui.OpenUI<UIGetReward>(UIType.GetReward).UpdateProp(1011011, 1);
                    return "opened a prop reward | " + State();
                case "ttlrewardclose":
                    {
                        UIGetReward reward = g.ui.GetUI<UIGetReward>(UIType.GetReward);
                        if (reward != null)
                        {
                            reward.btnOK.onClick.Invoke();
                        }
                        return "closed=" + (reward != null);
                    }
                default:
                    return null;
            }
        }

        private static string State()
        {
            WorldUnitBase me = g.world.playerUnit;
            var owned = new List<int>(AppellationService.Owned(me));
            owned.Sort();
            var ids = new List<string>();
            foreach (int id in owned)
            {
                ids.Add(id.ToString());
            }
            return "me=" + me.data.unitData.unitID + " owned=" + string.Join(",", ids.ToArray())
                + " equipped=" + AppellationService.Equipped(me) + " talent=" + me.data.dynUnitData.talent.value
                + " popup=" + (g.ui.GetUI<UIGetTaoist>(UIType.GetTaoist) != null) + " silenced=" + AppellationService.Silenced
                + " grade=" + AppellationService.CurrentGrade(me)
                + " reward=" + LossWindowOpen() + " rewardAny=" + (g.ui.GetUI<UIGetReward>(UIType.GetReward) != null);
        }

        // 도호 칸이 보이는 보상 창("도호 상실" 창)이 열려 있는지. 창의 제목 글은 일반 보상에서도 "도호 상실"로 남아 쓸 수 없다(회차 apl-1015g).
        private static bool LossWindowOpen()
        {
            UIGetReward reward = g.ui.GetUI<UIGetReward>(UIType.GetReward);
            return reward != null && reward.goTaoist != null && reward.goTaoist.activeSelf;
        }

        private static string Holders(int id)
        {
            var names = new List<string>();
            var map = WorldUnitAppellationMgr.allAppellationUnits;
            if (map != null && map.ContainsKey(id))
            {
                var list = map[id];
                for (int i = 0; i < list.Count; i++)
                {
                    names.Add(EditService.NameOf(list[i]) + "(" + list[i].data.unitData.unitID + ")");
                }
            }
            return "holders=" + names.Count + " names=" + string.Join(",", names.ToArray());
        }

        private static string NpcGive(string who, int id)
        {
            string me = g.world.playerUnit.data.unitData.unitID;
            WorldUnitBase unit = null;
            if (who != "any")
            {
                unit = g.world.unit.GetUnit(who, true);
            }
            else
            {
                var units = g.world.unit.GetUnits(false);
                for (int i = 0; i < units.Count && unit == null; i++)
                {
                    if (units[i].data.unitData.unitID != me && !AppellationService.Owned(units[i]).Contains(id))
                    {
                        unit = units[i];
                    }
                }
            }
            if (unit == null)
            {
                return "no unit " + who;
            }
            bool ok = unit.appellation.AddAppellationID(id);
            return "unit=" + unit.data.unitData.unitID + " ok=" + ok;
        }
    }
}
