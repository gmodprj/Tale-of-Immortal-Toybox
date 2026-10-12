using System;
using System.IO;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 개발 전용 명령 채널. docs/04-ingame-state-analysis.md 15절의 쓰기 검증을 게임 재시작 없이 하나씩 실행한다.
    //   명령 파일: E:\TlToybox\.devremote\mod-cmd.txt (한 줄에 하나)
    //   결과 파일: E:\TlToybox\.devremote\mod-out.txt
    // 명령 파일이 없으면 아무것도 하지 않는다.
    internal static class DevCommands
    {
        private const string Dir = @"E:\TlToybox\.devremote";

        public static void Poll()
        {
            string cmdPath = Path.Combine(Dir, "mod-cmd.txt");
            if (!File.Exists(cmdPath))
            {
                return;
            }
            string[] lines;
            try
            {
                lines = File.ReadAllLines(cmdPath);
                File.Delete(cmdPath);
            }
            catch (IOException)
            {
                return;
            }
            var output = new StringBuilder();
            foreach (string line in lines)
            {
                string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }
                string result;
                try
                {
                    result = Execute(parts);
                }
                catch (Exception e)
                {
                    result = "ERROR " + e.GetType().Name + ": " + e.Message;
                }
                MelonLogger.Msg("[TlToybox][cmd] " + line.Trim() + " -> " + result);
                output.AppendLine(line.Trim() + " -> " + result);
            }
            File.WriteAllText(Path.Combine(Dir, "mod-out.txt"), output.ToString(), Encoding.UTF8);
        }

        private static string Execute(string[] p)
        {
            string ui = UiSpike.Execute(p);
            if (ui != null)
            {
                return ui;
            }
            string time = DevCommandsTime.Execute(p);
            if (time != null)
            {
                return time;
            }
            string skill = DevCommandsSkill.Execute(p);
            if (skill != null)
            {
                return skill;
            }
            string fate = DevCommandsFate.Execute(p);
            if (fate != null)
            {
                return fate;
            }
            string stage4 = DevCommandsStage4.Execute(p);
            if (stage4 != null)
            {
                return stage4;
            }
            string stage3 = DevCommandsStage3.Execute(p);
            if (stage3 != null)
            {
                return stage3;
            }

            var unit = g.world.playerUnit;
            var saved = unit.data.unitData.propertyData;
            var dyn = unit.data.dynUnitData;
            var props = unit.data.unitData.propData;

            switch (p[0])
            {
                case "read":
                    return "money=" + props.GetPropsNum(PropsIDType.Money)
                        + " attack saved=" + saved.attack + " base=" + dyn.attack.baseValue + " value=" + dyn.attack.value
                        + " luck saved=" + saved.luck + " value=" + dyn.luck.value
                        + " props=" + props.allProps.Count + " grid=" + props.GetGridCurNum() + "/" + props.GetGridMaxNum()
                        + " worldDay=" + g.data.world.worldDay;
                case "count":
                    return "propsID=" + p[1] + " count=" + props.GetPropsNum(int.Parse(p[1]));
                case "money":
                    unit.data.RewardPropMoney(int.Parse(p[1]));
                    return "RewardPropMoney called; money=" + props.GetPropsNum(PropsIDType.Money);
                case "attack":
                    saved.attack = int.Parse(p[1]);
                    return "saved.attack set; saved=" + saved.attack + " base=" + dyn.attack.baseValue + " value=" + dyn.attack.value;
                case "updateconf":
                    unit.UpdateConf();
                    return "UpdateConf called; attack value=" + dyn.attack.value;
                case "item":
                    unit.data.RewardPropItem(int.Parse(p[1]), int.Parse(p[2]), false);
                    return "RewardPropItem called; count=" + props.GetPropsNum(int.Parse(p[1])) + " props=" + props.allProps.Count;
                case "limits":
                    var limit = g.conf.roleAttributeLimit;
                    return "attack " + limit.attackMin + ".." + limit.attackMax
                        + " defense " + limit.defenseMin + ".." + limit.defenseMax
                        + " hpMax " + limit.hpMin + ".." + limit.hpMax
                        + " luck " + limit.luckMin + ".." + limit.luckMax
                        + " talent " + limit.talentMin + ".." + limit.talentMax
                        + " basisFire " + limit.basisFireMin + ".." + limit.basisFireMax
                        + " lifeMin " + limit.lifeMin;
                case "save":
                    g.data.SaveData(new Action<bool>(ok => MelonLogger.Msg("[TlToybox][cmd] SaveData callback ok=" + ok)));
                    return "SaveData called; isSaveData=" + DataMgr.isSaveData;
                default:
                    return "UNKNOWN";
            }
        }
    }
}
