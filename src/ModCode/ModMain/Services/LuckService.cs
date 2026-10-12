using System;
using System.Collections.Generic;
using System.IO;

namespace MOD_sWP6UL
{
    // 운명(선천·후천·역천개명)의 조회와 추가·삭제.
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
            public int BornIndex = -1; // 선천운명 배열에서의 자리
            public bool Hidden;
            public bool IsSystem; // 경지의 난관·돌파 실패 표식
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
                IsSystem = SystemIds.Contains(id),
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
                LuckView view = View(saved.bornLuck[i].id, saved.bornLuck[i].duration, true);
                view.BornIndex = i;
                result.Add(view);
            }
            for (int i = 0; i < saved.addLuck.Count; i++)
            {
                result.Add(View(saved.addLuck[i].id, saved.addLuck[i].duration, false));
            }
            return result;
        }

        // 편집기로 붙이면 안 되는 운명: 경지의 난관 운명과 돌파 실패 운명. 게임의 진행 상태를 나타내는 표식이다.
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
            // 숨김 운명만 뺀다. 후천(2) 분류에는 게임의 상태 표식이 섞여 있으므로 화면에서 경고한다.
            return conf.type >= 1 && conf.type <= 3 && conf.conceal == 0;
        }

        // 추가할 수 있는 운명 후보. 숨김 운명만 뺀다.
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

        // 편집기로 추가한 운명의 기록. 게임이 붙인 운명(도심 상태, 난관 표식 등)을 실수로 지우지 않도록, 여기 있는 것만 삭제를 허용한다.
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
                return "운명 " + id + ": 게임에 없는 운명이라 추가하지 않았습니다.";
            }
            if (!Addable(conf))
            {
                return "운명 " + GameTool.LS(conf.name) + ": 편집기로 붙일 수 없는 종류입니다.";
            }
            if (Has(unit, id))
            {
                return "운명 " + GameTool.LS(conf.name) + ": 이미 가지고 있습니다.";
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
            ChangeLog.Add(EditService.NameOf(unit), "운명 " + GameTool.LS(conf.name) + "(" + id + ")", "없음", added ? "있음" : "없음", added);
            return added ? "운명 추가: " + GameTool.LS(conf.name) : "운명 " + GameTool.LS(conf.name) + ": 게임이 추가하지 않았습니다.";
        }

        private static DataUnit.LuckData FindAdd(DataUnit.PropertyData saved, int id)
        {
            for (int i = 0; i < saved.addLuck.Count; i++)
            {
                if (saved.addLuck[i].id == id)
                {
                    return saved.addLuck[i];
                }
            }
            return null;
        }

        // 게임의 인물 화면은 선천운명 칸에 bornLuck를, 후천운명 칸에 addLuck 가운데 후천 분류만 보여 준다.
        // 그래서 선천 분류의 운명은 bornLuck에 넣어야 화면에 나온다. 게임에는 이 기능이 없어 배열을 직접 늘린다.
        // 이미 후천 목록에 붙어 있는 운명이면 그 항목을 선천으로 옮긴다.
        public static string AddBorn(WorldUnitBase unit, int id)
        {
            var saved = unit.data.unitData.propertyData;
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(id);
            if (conf == null)
            {
                return "운명 " + id + ": 게임에 없는 운명이라 추가하지 않았습니다.";
            }
            string name = GameTool.LS(conf.name);
            if (!Addable(conf))
            {
                return "운명 " + name + ": 편집기로 붙일 수 없는 종류입니다.";
            }
            for (int i = 0; i < saved.bornLuck.Length; i++)
            {
                if (saved.bornLuck[i].id == id)
                {
                    return "운명 " + name + ": 이미 선천운명입니다.";
                }
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            // 게임이 만든 항목을 그대로 선천 배열로 옮겨 쓴다(효과는 추가 행동이 이미 켰다).
            if (FindAdd(saved, id) == null)
            {
                unit.CreateAction(new UnitActionLuckAdd(id), false);
            }
            DataUnit.LuckData data = FindAdd(saved, id);
            if (data == null)
            {
                ChangeLog.Add(EditService.NameOf(unit), "선천운명 추가 " + name + "(" + id + ")", "없음", "없음", false);
                return "운명 " + name + ": 게임이 추가하지 않았습니다.";
            }
            var old = saved.bornLuck;
            var grown = new UnhollowerBaseLib.Il2CppReferenceArray<DataUnit.LuckData>(old.Length + 1);
            for (int i = 0; i < old.Length; i++)
            {
                grown[i] = old[i];
            }
            grown[old.Length] = data;
            saved.bornLuck = grown;
            saved.DelAddLuck(id);
            SetTracked(unit, id, true);
            bool ok = saved.bornLuck.Length == old.Length + 1 && FindAdd(saved, id) == null && Has(unit, id);
            ChangeLog.Add(EditService.NameOf(unit), "선천운명 추가 " + name + "(" + id + ")", "없음", ok ? "있음" : "확인 필요", ok);
            return ok ? "선천운명 추가: " + name : "선천운명 " + name + ": 썼지만 상태가 예상과 다릅니다. 저장하지 말고 확인하세요.";
        }

        // 선천운명 한 자리를 없앤다. 효과는 게임의 삭제 행동으로 끄고, 저장 배열에서 그 자리를 뺀다.
        public static string RemoveBorn(WorldUnitBase unit, int index)
        {
            var saved = unit.data.unitData.propertyData;
            if (index < 0 || index >= saved.bornLuck.Length)
            {
                return "선천운명 삭제: 대상을 찾지 못했습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int id = saved.bornLuck[index].id;
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(id);
            string name = conf == null ? id.ToString() : GameTool.LS(conf.name);
            unit.CreateAction(new UnitActionLuckDel(id), false);
            var old = saved.bornLuck;
            var shrunk = new UnhollowerBaseLib.Il2CppReferenceArray<DataUnit.LuckData>(old.Length - 1);
            for (int i = 0, k = 0; i < old.Length; i++)
            {
                if (i != index)
                {
                    shrunk[k++] = old[i];
                }
            }
            saved.bornLuck = shrunk;
            SetTracked(unit, id, false);
            bool ok = !Has(unit, id);
            ChangeLog.Add(EditService.NameOf(unit), "선천운명 삭제 " + name + "(" + id + ")", "있음", ok ? "없음" : "확인 필요", ok);
            return ok ? "선천운명 삭제: " + name : "선천운명 " + name + ": 목록에서는 뺐지만 효과가 남아 있습니다. 저장 후 다시 불러오면 사라집니다.";
        }

        // 선천운명 한 자리를 다른 운명으로 바꾼다. 게임에는 이 기능이 없어 저장 데이터(bornLuck)를 직접 고친다.
        // 적용 중인 효과는 게임의 삭제·추가 행동으로 맞추고, 추가 행동이 후천 목록에 넣은 항목은 다시 뺀다.
        public static string ReplaceBorn(WorldUnitBase unit, int index, int newId)
        {
            var saved = unit.data.unitData.propertyData;
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(newId);
            if (conf == null || index < 0 || index >= saved.bornLuck.Length)
            {
                return "선천운명 교체: 대상을 찾지 못했습니다.";
            }
            string newName = GameTool.LS(conf.name);
            if (!Addable(conf))
            {
                return "운명 " + newName + ": 편집기로 붙일 수 없는 종류입니다.";
            }
            if (Has(unit, newId))
            {
                return "운명 " + newName + ": 이미 가지고 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int oldId = saved.bornLuck[index].id;
            ConfRoleCreateFeatureItem oldConf = g.conf.roleCreateFeature.GetItem(oldId);
            string oldName = oldConf == null ? oldId.ToString() : GameTool.LS(oldConf.name);
            unit.CreateAction(new UnitActionLuckDel(oldId), false);
            saved.bornLuck[index].id = newId;
            unit.CreateAction(new UnitActionLuckAdd(newId), false);
            saved.DelAddLuck(newId);
            bool written = saved.bornLuck[index].id == newId;
            bool live = Has(unit, newId) && !Has(unit, oldId);
            bool inAdd = false;
            for (int i = 0; i < saved.addLuck.Count; i++)
            {
                inAdd |= saved.addLuck[i].id == newId;
            }
            ChangeLog.Add(EditService.NameOf(unit), "선천운명 " + (index + 1) + "번", oldName + "(" + oldId + ")", newName + "(" + newId + ")", written);
            if (!written)
            {
                return "선천운명 교체: 값이 쓰이지 않았습니다.";
            }
            return "선천운명 교체: " + oldName + " -> " + newName
                + (live ? "" : " (효과는 저장 후 다시 불러와야 적용됩니다)")
                + (inAdd ? " (후천 목록에도 남았습니다. 운명 탭에서 지우세요)" : "");
        }

        // 후천운명을 지운다. 게임이 붙인 것을 지울 때의 확인은 화면이 맡는다.
        public static string Remove(WorldUnitBase unit, int id)
        {
            LuckView owned = Owned(unit).Find(v => v.Id == id);
            if (owned == null)
            {
                return "운명을 찾지 못했습니다.";
            }
            if (owned.Born)
            {
                return RemoveBorn(unit, owned.BornIndex);
            }
            if (owned.Hidden)
            {
                return "운명 " + owned.Name + ": 선천운명과 숨김 운명은 지울 수 없습니다.";
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
            ChangeLog.Add(EditService.NameOf(unit), "운명 " + owned.Name + "(" + id + ")", "있음", removed ? "없음" : "있음", removed);
            return removed ? "운명 삭제: " + owned.Name : "운명 " + owned.Name + ": 게임이 지우지 않았습니다.";
        }
    }
}
