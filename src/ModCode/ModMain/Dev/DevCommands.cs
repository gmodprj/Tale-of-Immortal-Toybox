using System;
using System.IO;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 개발 전용 명령 채널. docs/04-ingame-state-analysis.md 15절의 쓰기 검증을 게임 재시작 없이 하나씩 실행한다.
    //   명령 파일: <Dir>\mod-cmd.txt (한 줄에 하나)
    //   결과 파일: <Dir>\mod-out.txt
    // 명령 파일이 없으면 아무것도 하지 않는다.
    internal static class DevCommands
    {
        // .devremote 폴더(명령 채널·덤프). 환경 변수 TLTOYBOX_DEVREMOTE가 있으면 그 폴더, 없으면 기본. tools/DevRemote/devremote-dir.ps1과 같은 규칙(05 문서 49절).
        private const string DefaultDir = @"E:\TlToybox\.devremote";

        public static string Dir
        {
            get { return DirFromEnv ? Clean(Environment.GetEnvironmentVariable("TLTOYBOX_DEVREMOTE")) : DefaultDir; }
        }

        public static bool DirFromEnv
        {
            get { return Clean(Environment.GetEnvironmentVariable("TLTOYBOX_DEVREMOTE")).Length > 0; }
        }

        // 값의 앞뒤 공백·따옴표를 지우고, 드라이브 뿌리(E:\)가 아니면 끝 역슬래시를 뗀다. 공백뿐이면 "". devremote-dir.ps1과 같은 규칙.
        private static string Clean(string value)
        {
            string v = (value ?? "").Trim().Trim('"', '\'').Trim();
            if (v.Length == 0)
            {
                return "";
            }
            if (v.Length <= 3 && v.Length >= 2 && v[1] == ':')
            {
                return v.Substring(0, 2) + "\\";
            }
            return v.TrimEnd('\\', '/');
        }

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
            string input = DevCommandsInput.Execute(p);
            if (input != null)
            {
                return input;
            }
            string scan = DevCodeScan.Execute(p);
            if (scan != null)
            {
                return scan;
            }
            string zh = DevCommandsZh.Execute(p);
            if (zh != null)
            {
                return zh;
            }
            string grid = DevCommandsGrid.Execute(p);
            if (grid != null)
            {
                return grid;
            }
            string treasure = DevCommandsTreasure.Execute(p);
            if (treasure != null)
            {
                return treasure;
            }
            string translation = DevCommandsTranslation.Execute(p);
            if (translation != null)
            {
                return translation;
            }
            string workshop = DevCommandsWorkshop.Execute(p);
            if (workshop != null)
            {
                return workshop;
            }
            string dress = DevCommandsDress.Execute(p);
            if (dress != null)
            {
                return dress;
            }
            string titles = DevCommandsAppellation.Execute(p);
            if (titles != null)
            {
                return titles;
            }
            string resources = DevCommandsResources.Execute(p);
            if (resources != null)
            {
                return resources;
            }
            string artifact = DevCommandsArtifact.Execute(p);
            if (artifact != null)
            {
                return artifact;
            }
            string immortal = DevCommandsImmortal.Execute(p);
            if (immortal != null)
            {
                return immortal;
            }
            string snap = g.world == null || g.world.playerUnit == null ? null : DevCommandsSnap.Handle(g.world.playerUnit, p);
            if (snap != null)
            {
                return snap;
            }
            string preset = DevCommandsPreset.Execute(p);
            if (preset != null)
            {
                return preset;
            }
            string rate = DevCommandsRate.Execute(p);
            if (rate != null)
            {
                return rate;
            }
            string character = DevCommandsChar.Execute(p);
            if (character != null)
            {
                return character;
            }
            string ui = UiSpike.Execute(p);
            if (ui != null)
            {
                return ui;
            }
            string screen = DevCommandsUi.Execute(p);
            if (screen != null)
            {
                return screen;
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
                case "fame":
                    return "fame saved=" + saved.reputation + " value=" + dyn.reputation.value;
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
