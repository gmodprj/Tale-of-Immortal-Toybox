using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 가방 아이템의 수량 지정과 버리기(docs/superpowers/specs/2026-10-20-trainer-batch1-design.md 5.3·6절, 05 문서 65절).
    // 수는 장착품을 포함한 수(GetPropsNum, 게임 가방과 같다). 줄일 때는 BagPlan이 정한 순서로 줄(sole)마다 지운다.
    // 게임의 DelProps(번호, 수)는 장착한 법보부터 지우고 장착 칸에서도 빼므로 쓰지 않는다.
    internal static partial class BagService
    {
        public const int MaxCount = 99999;

        public static string SetCount(WorldUnitBase unit, int propsId, int n)
        {
            string name = EditService.ItemName(propsId);
            if (name == null)
            {
                return "아이템 " + propsId + ": 게임에 없는 아이템 ID입니다.";
            }
            if (n < 0 || n > MaxCount)
            {
                return name + ": 수량은 0~" + MaxCount + "만 쓸 수 있습니다.";
            }
            var props = unit.data.unitData.propData;
            int before = props.GetPropsNum(propsId);
            if (n == before)
            {
                return name + ": 이미 " + n + "개입니다.";
            }
            if (n > before)
            {
                return EditService.GiveItem(unit, propsId, n - before);
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            ConfItemPropsItem conf = g.conf.itemProps.GetItem(propsId);
            bool overlay = conf != null && conf.isOverlay == 1;
            int kept;
            foreach (BagPlan.Cut cut in BagPlan.Reduce(Stacks(unit, propsId), overlay, before - n, out kept))
            {
                if (cut.All)
                {
                    props.DelProps(cut.Sole);
                }
                else
                {
                    props.DelProps(cut.Sole, cut.Count);
                }
            }
            int after = props.GetPropsNum(propsId);
            bool ok = after == n + kept;
            ChangeLog.Add(EditService.NameOf(unit), "아이템 " + name + "(" + propsId + ")", before.ToString(), after.ToString(), ok);
            string text = ok ? name + ": " + before + "개 -> " + after + "개"
                : name + ": 줄이기를 요청했지만 수량이 " + before + " -> " + after + "입니다.";
            if (kept > 0)
            {
                text += " 장착 중인 " + kept + "개는 그대로 두었습니다.";
            }
            return text;
        }

        public static string Drop(WorldUnitBase unit, int propsId)
        {
            return SetCount(unit, propsId, 0);
        }

        // 버리기 단추의 첫 누름에 보이는 확인 문구.
        public static string DropQuestion(WorldUnitBase unit, int propsId)
        {
            string name = EditService.ItemName(propsId) ?? propsId.ToString();
            int held = unit.data.unitData.propData.GetPropsNum(propsId);
            int strong = BagPlan.StrongCount(Stacks(unit, propsId));
            return name + " " + held + "개를 모두 버립니다" + (strong > 0 ? "(강화·옵션이 붙은 것 " + strong + "개 포함)" : "") + ". 한 번 더 누르면 버립니다.";
        }

        // 보유 칸에 더 작은 수를 쓰고 Enter를 눌렀을 때 보이는 확인 문구(05 문서 89절. 같은 값에서 Enter를 한 번 더 누르면 줄인다).
        public static string LowerQuestion(WorldUnitBase unit, int propsId, int n)
        {
            string name = EditService.ItemName(propsId) ?? propsId.ToString();
            int held = unit.data.unitData.propData.GetPropsNum(propsId);
            return name + ": " + held + "개에서 " + n + "개로 줄입니다(" + (held - n) + "개 버림). 같은 값에서 Enter를 한 번 더 누르세요.";
        }

        // 그 번호의 가방 줄들. 장착 목록에는 장비와 전투 아이템 칸의 묶음이 함께 들어 있다(05 문서 65절).
        private static List<BagPlan.Stack> Stacks(WorldUnitBase unit, int propsId)
        {
            var data = unit.data.unitData;
            var equipped = new HashSet<string>();
            var soles = data.GetAllEquipPropsSoleID();
            for (int i = 0; soles != null && i < soles.Count; i++)
            {
                equipped.Add(soles[i]);
            }
            bool artifact = SpriteService.IsArtifact(propsId);
            var result = new List<BagPlan.Stack>();
            var all = data.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                DataProps.PropsData pd = all[i];
                if (pd == null || pd.propsID != propsId)
                {
                    continue;
                }
                result.Add(new BagPlan.Stack { Sole = pd.soleID, Count = pd.propsCount, Equipped = equipped.Contains(pd.soleID), Strength = artifact ? Strength(pd) : 0 });
            }
            return result;
        }

        // 법보의 강함: 단계 + 한계돌파 + 옵션 수. 읽지 못하면 0(가장 약한 것으로 본다).
        private static int Strength(DataProps.PropsData pd)
        {
            try
            {
                DataProps.PropsArtifact a = pd.To<DataProps.PropsArtifact>();
                return a.level + a.grade + a.GetPrefixCount();
            }
            catch (Exception e)
            {
                // 첫 예외 하나만 로그에 남긴다(12 문서 A4).
                if (!strengthWarned)
                {
                    strengthWarned = true;
                    MelonLoader.MelonLogger.Msg("[TlToybox] 법보 강도를 읽지 못함: " + e.GetType().Name + ": " + e.Message);
                }
                return 0;
            }
        }

        private static bool strengthWarned;
    }
}
