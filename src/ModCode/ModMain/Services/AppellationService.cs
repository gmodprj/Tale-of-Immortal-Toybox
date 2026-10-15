using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 도호(칭호) 관리: 게임 표 appellationTitle을 읽고 플레이어에게 주기·빼기·장착·해제를 한다. 화면을 모른다.
    // 근거: docs/superpowers/specs/2026-10-15-appellation-design.md 2·3.2절(인계 문서 8.3d절의 실측).
    internal static class AppellationService
    {
        public sealed class Entry
        {
            public int Id;
            public string Name;
            public string ClassKey;     // ClassGroup.Key
            public string Effect;       // 장착 효과(tips)
            public string Gain;         // 얻는 법(gainDesc)
            public string Desc;
            public int DropGrade;       // 이 대경지에 이르면 돌파 때 잃는다(dropType 1). 0이면 잃지 않는다
            public bool Unique;         // 한 사람만 가진다(lossType 1, '제일' 도호)
            public bool Story;          // 이야기와 묶인 효과(결말 다섯, 단금쉬옥)
            public string Search;
        }

        public sealed class ClassGroup
        {
            public string Key;
            public string Name;
            public int[] Classes;       // className의 첫 값
        }

        public const string AllKey = "All";
        public const string OwnedKey = "Owned";
        public const string OtherKey = "Other";
        public const string StoryNotice = "이야기와 묶인 효과라 확인하지 못했습니다";
        private const int StoryOnlyId = 2500;   // 단금쉬옥(분류 110)

        public static readonly ClassGroup[] Groups =
        {
            new ClassGroup { Key = "First", Name = "제일", Classes = new[] { 101 } },
            new ClassGroup { Key = "Romance", Name = "풍류", Classes = new[] { 102 } },
            new ClassGroup { Key = "Champion", Name = "괴수", Classes = new[] { 103 } },
            new ClassGroup { Key = "Spender", Name = "산재", Classes = new[] { 104 } },
            new ClassGroup { Key = "Dress", Name = "예상", Classes = new[] { 105 } },
            new ClassGroup { Key = "Free", Name = "소요", Classes = new[] { 106 } },
            new ClassGroup { Key = "Alchemy", Name = "영단", Classes = new[] { 107 } },
            new ClassGroup { Key = "Forge", Name = "영보", Classes = new[] { 108 } },
            new ClassGroup { Key = "Supreme", Name = "천존", Classes = new[] { 109, 110, 112 } },
            new ClassGroup { Key = "Ending", Name = "결말", Classes = new[] { 2601, 2602, 2603, 2604, 2605 } },
            new ClassGroup { Key = OtherKey, Name = "기타", Classes = new int[0] },
        };

        private static List<Entry> entries;
        private static readonly HashSet<int> quiet = new HashSet<int>();   // 편집기가 준 번호: 그 획득 창은 편집기가 닫는다

        // 편집기가 대신 확인을 누른 "도호 획득" 창의 수(검사가 본다).
        public static int Silenced { get; private set; }

        public static List<Entry> All()
        {
            if (entries != null)
            {
                return entries;
            }
            var list = new List<Entry>();
            var all = g.conf.appellationTitle._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                ConfAppellationTitleItem c = all[i];
                int cls = c.className != null && c.className.Length > 0 ? c.className[0] : 0;
                string key = KeyOf(cls);
                string name = Text(c.name);
                int drop = c.dropType == 1 && c.dropValue != null && c.dropValue.Length > 0 ? c.dropValue[0] : 0;
                list.Add(new Entry
                {
                    Id = c.id,
                    Name = name.Length > 0 ? name : "도호 " + c.id,
                    ClassKey = key,
                    Effect = Clean(c.tips),
                    Gain = Clean(c.gainDesc),
                    Desc = Clean(c.desc),
                    DropGrade = drop >= 1 && drop <= 10 ? drop : 0,
                    Unique = c.lossType == 1,
                    Story = key == "Ending" || c.id == StoryOnlyId,
                    Search = EditService.Compact(name),
                });
            }
            entries = list;
            return entries;
        }

        private static string KeyOf(int cls)
        {
            foreach (ClassGroup group in Groups)
            {
                if (Array.IndexOf(group.Classes, cls) >= 0)
                {
                    return group.Key;
                }
            }
            return OtherKey;
        }

        private static string Text(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }
            try
            {
                return GameTool.LS(key) ?? "";
            }
            catch (Exception)
            {
                return key;
            }
        }

        // 자리 표시($s_quan$ → 권법)를 풀고 색 태그를 지운다(공법 탭과 같은 함수).
        private static string Clean(string key)
        {
            return MartialService.CleanDesc(Text(key)).Replace("\r", "").Replace('\n', ' ');
        }

        public static Entry Get(int id)
        {
            return All().Find(e => e.Id == id);
        }

        public static string GroupName(string key)
        {
            foreach (ClassGroup group in Groups)
            {
                if (group.Key == key)
                {
                    return group.Name;
                }
            }
            return key;
        }

        // 게임 표에 묶음 밖의 분류가 있는지(있을 때만 기타 칩을 보인다).
        public static bool HasOther
        {
            get { return All().Exists(e => e.ClassKey == OtherKey); }
        }

        private static string TitleName(int id)
        {
            Entry e = Get(id);
            return e == null ? "도호 " + id : e.Name;
        }

        public static HashSet<int> Owned(WorldUnitBase unit)
        {
            var ids = new HashSet<int>();
            var title = unit.data.unitData.appellationTitle;
            if (title == null)
            {
                return ids;
            }
            for (int i = 0; i < title.appellationItems.Count; i++)
            {
                ids.Add(title.appellationItems[i].id);
            }
            return ids;
        }

        public static int Equipped(WorldUnitBase unit)
        {
            var title = unit.data.unitData.appellationTitle;
            return title == null || title.equipAppellationID.Count == 0 ? 0 : title.equipAppellationID[0];
        }

        // 그 도호를 가진 다른 인물(게임의 보유자 목록 WorldUnitAppellationMgr.allAppellationUnits).
        public static List<WorldUnitBase> OtherHolders(WorldUnitBase unit, int id)
        {
            var result = new List<WorldUnitBase>();
            var map = WorldUnitAppellationMgr.allAppellationUnits;
            if (map == null || !map.ContainsKey(id))
            {
                return result;
            }
            var list = map[id];
            string me = unit.data.unitData.unitID;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].data.unitData.unitID != me)
                {
                    result.Add(list[i]);
                }
            }
            return result;
        }

        // 표의 "다른 보유자" 열: '제일' 도호는 이름, 그 밖은 "NPC n명", 없으면 "-".
        public static string HolderText(Entry e, List<WorldUnitBase> holders)
        {
            if (holders.Count == 0)
            {
                return "-";
            }
            if (!e.Unique)
            {
                return "NPC " + holders.Count + "명";
            }
            var names = new List<string>();
            foreach (WorldUnitBase unit in holders)
            {
                names.Add(EditService.NameOf(unit));
            }
            return string.Join(", ", names.ToArray());
        }

        public static int CurrentGrade(WorldUnitBase unit)
        {
            return Math.Max(1, FateService.CurrentGrade(unit));
        }

        public static bool DropsSoon(Entry e, int grade)
        {
            return e.DropGrade > 0 && grade >= e.DropGrade;
        }

        // 표의 "잃는 경지" 열.
        public static string LossText(Entry e, int grade)
        {
            if (e.DropGrade == 0)
            {
                return "-";
            }
            string name = ArtifactService.MajorName(e.DropGrade);
            return DropsSoon(e, grade) ? name + " · 다음 돌파" : name;
        }

        public static string DropNotice(Entry e)
        {
            return ArtifactService.MajorName(e.DropGrade) + " 이상이라 다음 돌파 때 잃습니다";
        }

        public static bool Give(WorldUnitBase unit, int id, out string text)
        {
            Entry e = Get(id);
            if (e == null)
            {
                text = "도호: 게임에 없는 번호입니다(" + id + ").";
                return false;
            }
            if (Owned(unit).Contains(id))
            {
                text = "도호: " + e.Name + "은(는) 이미 가졌습니다.";
                return false;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            try
            {
                string field = "도호 " + e.Name + "(" + id + ")";
                var moved = new List<string>();
                if (e.Unique)
                {
                    // 한 사람만 가지는 도호: 가진 인물들에게서 먼저 뺀다(게임은 정리하지 않는다).
                    foreach (WorldUnitBase other in OtherHolders(unit, id))
                    {
                        string who = EditService.NameOf(other);
                        other.appellation.DelAppellationID(id, false);
                        ChangeLog.Add(who, field, "가짐", "없음", !Owned(other).Contains(id));
                        moved.Add(who);
                    }
                }
                bool emptyBefore = Equipped(unit) == 0;
                quiet.Add(id);
                unit.appellation.AddAppellationID(id);
                bool ok = Owned(unit).Contains(id);
                if (!ok)
                {
                    quiet.Remove(id);
                }
                bool equippedNow = emptyBefore && Equipped(unit) == id;
                ChangeLog.Add(EditService.NameOf(unit), field, "없음", equippedNow ? "장착" : "가짐", ok);
                text = "도호 주기: " + e.Name
                    + (moved.Count > 0 ? " (" + string.Join(", ", moved.ToArray()) + "에게서 옮김)" : "")
                    + (equippedNow ? ", 장착됨" : "")
                    + (DropsSoon(e, CurrentGrade(unit)) ? ". " + DropNotice(e) : "")
                    + (e.Story ? ". " + StoryNotice : "")
                    + (ok ? "." : ". 게임이 넣지 않았습니다.");
                return ok;
            }
            catch (Exception ex)
            {
                text = "도호: 게임이 받지 않았습니다(" + ex.GetType().Name + ").";
                return false;
            }
        }

        public static bool Remove(WorldUnitBase unit, int id, out string text)
        {
            string name = TitleName(id);
            if (!Owned(unit).Contains(id))
            {
                text = "도호: " + name + "은(는) 갖고 있지 않습니다.";
                return false;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            try
            {
                bool wasEquipped = Equipped(unit) == id;
                unit.appellation.DelAppellationID(id, false);
                bool ok = !Owned(unit).Contains(id);
                ChangeLog.Add(EditService.NameOf(unit), "도호 " + name + "(" + id + ")", wasEquipped ? "장착" : "가짐", "없음", ok);
                text = "도호 빼기: " + name + (wasEquipped && ok ? ", 장착이 비었습니다" : "") + (ok ? "." : ". 게임이 빼지 않았습니다.");
                return ok;
            }
            catch (Exception ex)
            {
                text = "도호: 게임이 받지 않았습니다(" + ex.GetType().Name + ").";
                return false;
            }
        }

        public static bool Equip(WorldUnitBase unit, int id, out string text)
        {
            string name = TitleName(id);
            if (!Owned(unit).Contains(id))
            {
                text = "도호: " + name + "은(는) 갖고 있지 않습니다.";
                return false;
            }
            int before = Equipped(unit);
            if (before == id)
            {
                text = "도호: " + name + "은(는) 이미 장착했습니다.";
                return false;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            try
            {
                unit.appellation.EquipAppellationID(id);
                bool ok = Equipped(unit) == id;
                ChangeLog.Add(EditService.NameOf(unit), "도호 장착", before == 0 ? "없음" : TitleName(before), name, ok);
                text = "도호 장착: " + name + (ok ? "." : ". 게임이 장착하지 않았습니다.");
                return ok;
            }
            catch (Exception ex)
            {
                text = "도호: 게임이 받지 않았습니다(" + ex.GetType().Name + ").";
                return false;
            }
        }

        public static bool Unequip(WorldUnitBase unit, out string text)
        {
            int current = Equipped(unit);
            if (current == 0)
            {
                text = "도호: 장착한 도호가 없습니다.";
                return false;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                text = backupError + " 변경을 중단했습니다.";
                return false;
            }
            try
            {
                unit.appellation.UnequipAppellationID(current);
                bool ok = Equipped(unit) == 0;
                ChangeLog.Add(EditService.NameOf(unit), "도호 장착", TitleName(current), "없음", ok);
                text = "도호 장착 해제: " + TitleName(current) + (ok ? "." : ". 게임이 해제하지 않았습니다.");
                return ok;
            }
            catch (Exception ex)
            {
                text = "도호: 게임이 받지 않았습니다(" + ex.GetType().Name + ").";
                return false;
            }
        }

        // 매 프레임. 편집기가 준 번호의 "도호 획득" 창이 열려 있으면 그 창의 확인 버튼을 누른다.
        // CloseUI로 닫으면 창 대기열이 멈춰 그 뒤의 창이 뜨지 않는다(회차 apl-1015d). 확인을 누르면 대기열이 이어진다(apl-1015e).
        public static void Tick()
        {
            if (quiet.Count == 0)
            {
                return;
            }
            try
            {
                UIGetTaoist ui = g.ui.GetUI<UIGetTaoist>(UIType.GetTaoist);
                if (ui == null || ui.conf == null || !quiet.Remove(ui.conf.id))
                {
                    return;
                }
                Silenced++;
                ui.btnOK.onClick.Invoke();
            }
            catch (Exception)
            {
                // 매 프레임 불리므로 예외를 던지지 않는다.
            }
        }
    }
}
