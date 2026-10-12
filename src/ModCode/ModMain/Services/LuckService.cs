using System;
using System.Collections.Generic;
using System.IO;

namespace MOD_sWP6UL
{
    // 기운(선천·후천·역천개명)의 조회와 추가·삭제.
    internal static class LuckService
    {
        public class LuckView
        {
            public int Id;
            public string Name;
            public string Tips;
            public int Type;
            public int Level;
            public int Duration;
            public bool Born;
            public bool Hidden;
        }

        public static string TypeLabel(int type)
        {
            switch (type)
            {
                case 1: return "선천";
                case 2: return "후천";
                case 3: return "역천개명";
                default: return "기타";
            }
        }

        private static LuckView View(int id, int duration, bool born)
        {
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(id);
            return new LuckView
            {
                Id = id,
                Name = conf == null ? "?" : GameTool.LS(conf.name),
                Tips = conf == null ? "" : Clean(GameTool.LS(conf.tips)),
                Type = conf == null ? 0 : conf.type,
                Level = conf == null ? 0 : conf.level,
                Duration = duration,
                Born = born,
                Hidden = conf != null && conf.conceal != 0,
            };
        }

        private static string Clean(string text)
        {
            return System.Text.RegularExpressions.Regex.Replace(text ?? "", @"<[^>]+>|\\n|\n", " ").Trim();
        }

        public static List<LuckView> Owned(WorldUnitBase unit)
        {
            var result = new List<LuckView>();
            var saved = unit.data.unitData.propertyData;
            for (int i = 0; i < saved.bornLuck.Length; i++)
            {
                result.Add(View(saved.bornLuck[i].id, saved.bornLuck[i].duration, true));
            }
            for (int i = 0; i < saved.addLuck.Count; i++)
            {
                result.Add(View(saved.addLuck[i].id, saved.addLuck[i].duration, false));
            }
            return result;
        }

        // 편집기로 붙이면 안 되는 기운: 경지의 난관 기운과 돌파 실패 기운. 게임의 진행 상태를 나타내는 표식이다.
        private static HashSet<int> systemIds;

        private static HashSet<int> SystemIds
        {
            get
            {
                if (systemIds == null)
                {
                    systemIds = new HashSet<int>();
                    var grades = g.conf.roleGrade._allConfList;
                    for (int i = 0; i < grades.Count; i++)
                    {
                        if (grades[i].lockFeature != 0) { systemIds.Add(grades[i].lockFeature); }
                        if (grades[i].failFeatureID != 0) { systemIds.Add(grades[i].failFeatureID); }
                    }
                }
                return systemIds;
            }
        }

        private static bool Addable(ConfRoleCreateFeatureItem conf)
        {
            // 후천(2) 분류에는 게임의 상태 표식 기운이 섞여 있어 선천(1)과 역천개명(3)만 허용한다.
            return (conf.type == 1 || conf.type == 3) && conf.conceal == 0 && conf.level >= 1 && !SystemIds.Contains(conf.id);
        }

        // 추가할 수 있는 기운 후보. 숨김 기운, 품질이 0 이하인 상태·불이익 기운, 진행 표식 기운은 뺀다.
        public static List<LuckView> Candidates(int type, string query)
        {
            var result = new List<LuckView>();
            string needle = EditService.Compact(query ?? "");
            var all = g.conf.roleCreateFeature._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfRoleCreateFeatureItem conf = all[i];
                if (conf.type != type || !Addable(conf))
                {
                    continue;
                }
                LuckView view = View(conf.id, 0, false);
                if (view.Name.Length == 0)
                {
                    continue;
                }
                if (needle.Length > 0 && !EditService.Compact(view.Name).Contains(needle) && !EditService.Compact(view.Tips).Contains(needle))
                {
                    continue;
                }
                result.Add(view);
            }
            return result;
        }

        public static bool Has(WorldUnitBase unit, int id)
        {
            return unit.GetLuck(id) != null;
        }

        // 편집기로 추가한 기운의 기록. 게임이 붙인 기운(도심 상태, 난관 표식 등)을 실수로 지우지 않도록, 여기 있는 것만 삭제를 허용한다.
        private static string TrackFile
        {
            get { return Path.Combine(ChangeLog.DataDir, "editor-lucks.txt"); }
        }

        private static HashSet<string> Tracked()
        {
            var result = new HashSet<string>();
            try
            {
                if (File.Exists(TrackFile))
                {
                    foreach (string line in File.ReadAllLines(TrackFile))
                    {
                        if (line.Trim().Length > 0)
                        {
                            result.Add(line.Trim());
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            return result;
        }

        private static void SetTracked(WorldUnitBase unit, int id, bool tracked)
        {
            HashSet<string> all = Tracked();
            string key = unit.data.unitData.unitID + ":" + id;
            if (tracked)
            {
                all.Add(key);
            }
            else
            {
                all.Remove(key);
            }
            try
            {
                Directory.CreateDirectory(ChangeLog.DataDir);
                File.WriteAllLines(TrackFile, new List<string>(all).ToArray());
            }
            catch (IOException)
            {
            }
        }

        public static bool AddedByEditor(WorldUnitBase unit, int id)
        {
            return Tracked().Contains(unit.data.unitData.unitID + ":" + id);
        }

        public static string Add(WorldUnitBase unit, int id)
        {
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(id);
            if (conf == null)
            {
                return "기운 " + id + ": 게임에 없는 기운이라 추가하지 않았습니다.";
            }
            if (!Addable(conf))
            {
                return "기운 " + GameTool.LS(conf.name) + ": 편집기로 붙일 수 없는 종류입니다.";
            }
            if (Has(unit, id))
            {
                return "기운 " + GameTool.LS(conf.name) + ": 이미 가지고 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            unit.CreateAction(new UnitActionLuckAdd(id), false);
            bool added = Has(unit, id);
            if (added)
            {
                SetTracked(unit, id, true);
            }
            ChangeLog.Add(EditService.NameOf(unit), "기운 " + GameTool.LS(conf.name) + "(" + id + ")", "없음", added ? "있음" : "없음", added);
            return added ? "기운 추가: " + GameTool.LS(conf.name) : "기운 " + GameTool.LS(conf.name) + ": 게임이 추가하지 않았습니다.";
        }

        // 편집기로 추가한 기운만 지운다.
        public static string Remove(WorldUnitBase unit, int id)
        {
            LuckView owned = Owned(unit).Find(v => v.Id == id);
            if (owned == null)
            {
                return "기운을 찾지 못했습니다.";
            }
            if (owned.Born || owned.Hidden || !AddedByEditor(unit, id))
            {
                return "기운 " + owned.Name + ": 편집기로 추가한 기운만 지울 수 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            unit.CreateAction(new UnitActionLuckDel(id), false);
            bool removed = !Has(unit, id);
            if (removed)
            {
                SetTracked(unit, id, false);
            }
            ChangeLog.Add(EditService.NameOf(unit), "기운 " + owned.Name + "(" + id + ")", "있음", removed ? "없음" : "있음", removed);
            return removed ? "기운 삭제: " + owned.Name : "기운 " + owned.Name + ": 게임이 지우지 않았습니다.";
        }
    }
}
