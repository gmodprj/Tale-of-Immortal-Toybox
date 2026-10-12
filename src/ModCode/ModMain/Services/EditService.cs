using System;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 게임 값을 바꾸는 유일한 통로. 모든 쓰기는 백업 -> 범위 검사 -> 쓰기 -> 재조회 -> 이력 기록 순서를 따른다.
    // 여기 있는 쓰기 방법은 docs/04-ingame-state-analysis.md 15절에서 실측한 것만이다.
    internal static class EditService
    {
        // 호감도의 유효 범위는 게임에서 확인하지 못했다. 다른 공개 모드가 쓰는 값(±300)을 자체 제한으로 둔다.
        public const int IntimMin = -300;
        public const int IntimMax = 300;

        public static bool WorldReady
        {
            get { return g.world != null && g.world.isIntoWorld && g.world.playerUnit != null; }
        }

        public static string NameOf(WorldUnitBase unit)
        {
            return Compact(unit.data.unitData.propertyData.GetName());
        }

        // 이름에서 글자와 숫자만 남긴다. 게임의 이름에는 성과 이름 사이에 공백류 문자가 들어 있다.
        public static string Compact(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string GradeName(int gradeId)
        {
            ConfRoleGradeItem item = g.conf.roleGrade.GetItem(gradeId);
            return item == null ? "?" : GameTool.LS(item.gradeName) + " " + GameTool.LS(item.phaseName);
        }

        // 능력치 저장값을 newValue로 바꾼다. 범위 밖이면 거부한다.
        public static string SetAttribute(WorldUnitBase unit, AttributeDef def, int newValue)
        {
            ConfRoleAttributeLimit limit = g.conf.roleAttributeLimit;
            int min = def.Min(limit);
            int max = def.Max(limit);
            if (newValue < min || newValue > max)
            {
                return def.Label + ": " + newValue + "은(는) 허용 범위 " + min + "~" + max + " 밖이라 적용하지 않았습니다.";
            }
            var saved = unit.data.unitData.propertyData;
            int before = def.Get(saved);
            if (before == newValue)
            {
                return def.Label + ": 이미 " + newValue + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            def.Set(saved, newValue);
            int after = def.Get(saved);
            int baseValue = def.Dyn(unit.data.dynUnitData).baseValue;
            bool verified = after == newValue && baseValue == newValue;
            ChangeLog.Add(NameOf(unit), def.Label, before.ToString(), after.ToString(), verified);
            return verified
                ? def.Label + ": " + before + " -> " + after
                : def.Label + ": 썼지만 재조회 값이 다릅니다(저장 " + after + ", 기준 " + baseValue + ").";
        }

        // 체력·영력·염력·건강·정력·심정의 현재값을 각 상한(보정 포함)까지 채운다.
        public static string RestoreResources(WorldUnitBase unit)
        {
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            var saved = unit.data.unitData.propertyData;
            var dyn = unit.data.dynUnitData;
            string before = saved.hp + "/" + saved.mp + "/" + saved.sp + "/" + saved.health + "/" + saved.energy + "/" + saved.mood;
            saved.hp = dyn.hpMax.value;
            saved.mp = dyn.mpMax.value;
            saved.sp = dyn.spMax.value;
            saved.health = dyn.healthMax.value;
            saved.energy = dyn.energyMax.value;
            saved.mood = dyn.moodMax.value;
            string after = saved.hp + "/" + saved.mp + "/" + saved.sp + "/" + saved.health + "/" + saved.energy + "/" + saved.mood;
            ChangeLog.Add(NameOf(unit), "자원 회복(체력/영력/염력/건강/정력/심정)", before, after, true);
            return "자원 회복: " + after;
        }

        public static int Money(WorldUnitBase unit)
        {
            return unit.data.unitData.propData.GetPropsNum(PropsIDType.Money);
        }

        public static string AddMoney(WorldUnitBase unit, int amount)
        {
            if (amount <= 0)
            {
                return "영석: 0보다 큰 수만 추가할 수 있습니다.";
            }
            int before = Money(unit);
            if ((long)before + amount > int.MaxValue)
            {
                return "영석: 합계가 표현 범위를 넘어 적용하지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            unit.data.RewardPropMoney(amount);
            int after = Money(unit);
            ChangeLog.Add(NameOf(unit), "영석", before.ToString(), after.ToString(), after == before + amount);
            return "영석: " + before + " -> " + after;
        }

        public static string ItemName(int propsId)
        {
            ConfItemPropsItem item = g.conf.itemProps.GetItem(propsId);
            return item == null ? null : GameTool.LS(item.name);
        }

        public static string GiveItem(WorldUnitBase unit, int propsId, int count)
        {
            string name = ItemName(propsId);
            if (name == null)
            {
                return "아이템 " + propsId + ": 게임에 없는 아이템 ID라 지급하지 않았습니다.";
            }
            if (count <= 0)
            {
                return "아이템: 수량은 1 이상이어야 합니다.";
            }
            var props = unit.data.unitData.propData;
            int before = props.GetPropsNum(propsId);
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            unit.data.RewardPropItem(propsId, count, false);
            int after = props.GetPropsNum(propsId);
            ChangeLog.Add(NameOf(unit), "아이템 " + name + "(" + propsId + ")", before.ToString(), after.ToString(), after == before + count);
            return after == before + count
                ? name + ": " + before + "개 -> " + after + "개"
                : name + ": 지급을 요청했지만 수량이 " + before + " -> " + after + "입니다(가방 공간을 확인하세요).";
        }

        // 수련치를 다음 단계 요구량까지 채운다.
        public static string FillExp(WorldUnitBase unit)
        {
            var saved = unit.data.unitData.propertyData;
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(saved.gradeID);
            if (next == null)
            {
                return "수련치: 다음 단계가 없습니다.";
            }
            int before = saved.exp;
            if (before >= next.exp)
            {
                return "수련치: 이미 다음 단계 요구량(" + next.exp + ")을 채웠습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            saved.AddExp(next.exp - before);
            int after = saved.exp;
            ChangeLog.Add(NameOf(unit), "수련치", before.ToString(), after.ToString(), after == next.exp);
            return "수련치: " + before + " -> " + after;
        }

        // 다음 단계가 같은 대경지 안의 소경지일 때만 정식 돌파 행동을 실행한다.
        // 대경지 돌파(재료·역천개명이 걸리는 단계)는 검증하지 못해 막아 둔다.
        public static string BreakthroughBlockReason(WorldUnitBase unit)
        {
            var saved = unit.data.unitData.propertyData;
            ConfRoleGradeItem current = g.conf.roleGrade.GetItem(saved.gradeID);
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(saved.gradeID);
            if (next == null)
            {
                return "다음 단계가 없습니다.";
            }
            if (next.grade != current.grade)
            {
                return "다음은 대경지 돌파입니다. 이 버전에서는 지원하지 않습니다.";
            }
            if (saved.exp < next.exp)
            {
                return "수련치가 부족합니다(" + saved.exp + "/" + next.exp + ").";
            }
            return null;
        }

        public static string Breakthrough(WorldUnitBase unit)
        {
            string blocked = BreakthroughBlockReason(unit);
            if (blocked != null)
            {
                return "돌파: " + blocked;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            var saved = unit.data.unitData.propertyData;
            int beforeId = saved.gradeID;
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(beforeId);
            unit.CreateAction(new UnitActionRoleUpGrade(next.id, new Il2CppStringArray(0), new Il2CppStringArray(0)), false);
            int afterId = saved.gradeID;
            ChangeLog.Add(NameOf(unit), "경지", GradeName(beforeId), GradeName(afterId), afterId == next.id);
            return afterId == next.id
                ? "돌파: " + GradeName(beforeId) + " -> " + GradeName(afterId)
                : "돌파: 행동을 실행했지만 경지가 바뀌지 않았습니다.";
        }

        public static int IntimTo(WorldUnitBase from, WorldUnitBase to)
        {
            return from.data.unitData.relationData.GetIntim(to.data.unitData.unitID);
        }

        // 호감도는 단위마다 따로 저장되므로 양쪽을 함께 쓴다.
        public static string SetIntimBoth(WorldUnitBase player, WorldUnitBase npc, int value)
        {
            if (value < IntimMin || value > IntimMax)
            {
                return "호감도: " + value + "은(는) 편집기 제한 " + IntimMin + "~" + IntimMax + " 밖이라 적용하지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = IntimTo(player, npc) + "/" + IntimTo(npc, player);
            player.data.unitData.relationData.SetIntim(npc.data.unitData.unitID, value);
            npc.data.unitData.relationData.SetIntim(player.data.unitData.unitID, value);
            bool verified = IntimTo(player, npc) == value && IntimTo(npc, player) == value;
            string after = IntimTo(player, npc) + "/" + IntimTo(npc, player);
            ChangeLog.Add(NameOf(player) + " <-> " + NameOf(npc), "호감도(나→상대/상대→나)", before, after, verified);
            return "호감도 " + NameOf(npc) + ": " + before + " -> " + after;
        }
    }
}
