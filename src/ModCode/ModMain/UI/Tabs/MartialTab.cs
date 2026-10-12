using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 공법 탭: 익힌 공법의 옵션·숙련 경험치·장착·망각, 기술 추가.
    internal sealed class MartialTab : EditorTab
    {
        private string martialId;       // 선택한 공법
        private int martialPage;
        private int replaceIndex = -1;  // 옵션 교체 중인 자리. -1이면 교체 화면이 아니다
        private int replacePage;
        private string martialPending;  // 망각 확인 중인 공법
        private bool skillAdding;       // 기술 추가 화면
        private int skillKind;
        private int skillPage;
        private int skillGrade;
        private int skillPending;       // 강제 추가 확인 중인 기술 번호
        private string skillQuery = "";
        private string appliedSkillQuery = "";

        private static int Step
        {
            get { return AmountBar.Get("martialExp"); }
        }

        public override string Title
        {
            get { return "공법"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public override void OnTargetChanged()
        {
            martialId = null;
            replaceIndex = -1;
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            if (skillAdding)
            {
                RenderSkillAdd(target);
                return;
            }
            const int listRows = 8;
            const int pickRows = 12;
            var all = new List<KeyValuePair<string, DataUnit.ActionMartialData>>();
            foreach (var pair in target.data.unitData.allActionMartial)
            {
                all.Add(new KeyValuePair<string, DataUnit.ActionMartialData>(pair.Key, pair.Value));
            }
            DataProps.MartialData selected = martialId == null ? null : MartialService.Martial(target, martialId);
            if (selected == null)
            {
                martialId = null;
                replaceIndex = -1;
            }

            if (selected != null && replaceIndex >= 0)
            {
                RenderPrefixPicker(target, selected, pickRows);
                return;
            }

            int pages = Math.Max(1, (all.Count + listRows - 1) / listRows);
            martialPage = Mathf.Clamp(martialPage, 0, pages - 1);
            Ui.Label(content, "공법", 0, 0, 300, 30, 18).color = Ui.Dim;
            Ui.Label(content, "종류", 330, 0, 120, 30, 18).color = Ui.Dim;
            Ui.Label(content, "경지", 460, 0, 80, 30, 18).color = Ui.Dim;
            Ui.Label(content, "숙련 경험치", 550, 0, 150, 30, 18).color = Ui.Dim;
            for (int row = 0; row < listRows; row++)
            {
                int index = martialPage * listRows + row;
                if (index >= all.Count)
                {
                    break;
                }
                string id = all[index].Key;
                DataProps.MartialData martial = all[index].Value.data.To<DataProps.MartialData>();
                float y = 32 + row * 38;
                Ui.Label(content, MartialService.Name(martial), 0, y, 320, 34);
                Ui.Label(content, MartialService.TypeLabel(martial.martialType), 330, y, 120, 34);
                Ui.Label(content, martial.grade.ToString(), 460, y, 80, 34);
                Ui.Label(content, all[index].Value.exp.ToString(), 550, y, 150, 34);
                Ui.Label(content, MartialService.IsEquipped(target, id) ? "장착 중" : "", 840, y, 120, 34, 17).color = Ui.Warn;
                Ui.Button("PickMartial" + index, content, id == martialId ? "선택됨" : "선택", 720, y, 110, 32, new Action(() => { martialId = id; replaceIndex = -1; martialPending = null; Redraw(); }), true, id == martialId);
            }
            float listEnd = 32 + listRows * 38;
            Ui.Button("MartialPrev", content, "이전", 0, listEnd, 80, 34, new Action(() => { martialPage--; Refresh(); }), martialPage > 0);
            Ui.Label(content, (martialPage + 1) + " / " + pages, 86, listEnd, 80, 34, 18, TextAnchor.MiddleCenter);
            Ui.Button("MartialNext", content, "다음", 172, listEnd, 80, 34, new Action(() => { martialPage++; Refresh(); }), martialPage < pages - 1);
            Ui.Label(content, all.Count == 0 ? "익힌 공법이 없습니다." : "대상이 익힌 공법 " + all.Count + "개", 270, listEnd, 400, 34, 18).color = Ui.Dim;
            Ui.Button("SkillAddOpen", content, "기술 추가", 700, listEnd, 160, 34, new Action(() => { skillAdding = true; skillPage = 0; skillGrade = 0; martialPending = null; Redraw(); }));

            float box = listEnd + 46;
            Ui.Panel("MartialBox", content, 0, box, ContentW, 330, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "위에서 공법을 선택하면 옵션을 편집할 수 있습니다.", 16, box + 10, 880, 36);
                return;
            }
            Ui.Label(content, MartialService.Name(selected) + "  (" + MartialService.TypeLabel(selected.martialType) + ")", 16, box + 6, 420, 36, 21);
            Ui.Button("MartialExp", content, "숙련 경험치 + " + Step, 450, box + 8, 230, 34, new Action(() => Say(MartialService.AddExp(Target, martialId, Step))));
            AmountBar.Build(content, "martialExp", 700, box + 6);
            // 장착·해제와 망각. 망각은 한 번 더 확인한다.
            bool equipped = MartialService.IsEquipped(target, martialId);
            bool forgetting = martialPending == martialId;
            Ui.Button("MartialEquip", content, equipped ? "장착 해제" : "장착", 16, box + 286, 150, 34, new Action(() =>
            {
                martialPending = null;
                Say(equipped ? MartialService.Unequip(Target, martialId) : MartialService.Equip(Target, martialId));
            }));
            Ui.Button("MartialForget", content, forgetting ? "정말 망각" : "기술 망각", 176, box + 286, 150, 34, new Action(() =>
            {
                if (!forgetting)
                {
                    martialPending = martialId;
                    Say("이 공법을 익힌 목록에서 지웁니다. 계속하려면 \"정말 망각\"을 한 번 더 누르세요.");
                    return;
                }
                martialPending = null;
                string result = MartialService.Forget(Target, martialId);
                martialId = null;
                Say(result);
            }), true, forgetting);
            Ui.Label(content, "수련은 위의 숙련 경험치로, 참오는 위 옵션의 품질·교체로 대신합니다.", 340, box + 286, 800, 34, 15).color = Ui.Dim;
            List<MartialService.PrefixView> prefixes = MartialService.Prefixes(selected);
            if (prefixes.Count == 0)
            {
                Ui.Label(content, "이 공법에는 옵션이 없습니다.", 16, box + 52, 880, 34, 18).color = Ui.Dim;
            }
            for (int i = 0; i < prefixes.Count && i < 6; i++)
            {
                MartialService.PrefixView view = prefixes[i];
                int slot = view.Index;
                float y = box + 50 + i * 38;
                Ui.Label(content, (slot + 1) + ". " + Shorten(view.Desc, 34), 16, y, 520, 34, 17);
                Ui.Label(content, "품질 " + view.Level + "/" + view.MaxLevel, 545, y, 110, 34, 18);
                Ui.Button("QualityMinus" + slot, content, "-", 660, y, 44, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.Level - 1))), view.Level > 1);
                Ui.Button("QualityPlus" + slot, content, "+", 708, y, 44, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.Level + 1))), view.Level < view.MaxLevel);
                Ui.Button("QualityMax" + slot, content, "최고", 756, y, 66, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.MaxLevel))), view.Level != view.MaxLevel);
                Ui.Button("Replace" + slot, content, "교체", 826, y, 80, 32, new Action(() => { replaceIndex = slot; replacePage = 0; Redraw(); }));
            }
        }

        // 공법 탭의 기술 추가 화면: 게임 기술 화면의 다섯 분류에서 이름으로 골라 배운 상태로 넣는다.
        private void RenderSkillAdd(WorldUnitBase target)
        {
            const int rows = 12;
            Ui.Label(content, "기술 추가 — " + EditService.NameOf(target), 0, 0, 500, 36, 21);
            Ui.Button("SkillBack", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(() => { skillAdding = false; Redraw(); }));
            for (int i = 0; i < SkillCatalog.Kinds.Length; i++)
            {
                int index = i;
                Ui.Button("SkillKind" + i, content, SkillCatalog.Kinds[i].Tab, i * 156, 44, 150, 38, new Action(() => { skillKind = index; skillPage = 0; skillPending = 0; Redraw(); }), true, skillKind == i);
            }
            Ui.Input("SkillQuery", content, skillQuery, "이름의 일부 또는 번호", 800, 44, 250, 38, new Action<string>(v => skillQuery = v));
            Ui.Button("SkillSearch", content, "검색", 1058, 44, 80, 38, new Action(() => { appliedSkillQuery = skillQuery.Trim(); skillPage = 0; Refresh(); }));
            Ui.Button("SkillSearchClear", content, "지움", 1144, 44, 80, 38, new Action(() => { skillQuery = ""; appliedSkillQuery = ""; skillPage = 0; Refresh(); }), skillQuery.Length > 0 || appliedSkillQuery.Length > 0);

            // 만들 기술의 경지. 처음에는 편집 대상의 현재 대경지.
            if (skillGrade <= 0)
            {
                skillGrade = Math.Max(1, FateService.CurrentGrade(target));
            }
            Ui.Label(content, "경지", 0, 90, 60, 36, 18).color = Ui.Dim;
            for (int grade = 1; grade <= 10; grade++)
            {
                int value = grade;
                Ui.Button("SkillGrade" + grade, content, grade.ToString(), 60 + (grade - 1) * 50, 90, 46, 36, new Action(() => { skillGrade = value; Redraw(); }), true, skillGrade == grade);
            }
            Ui.Button("SkillLearnAll", content, SkillCatalog.Kinds[skillKind].Tab + " 모두 배우기", 580, 90, 240, 36, new Action(() => { skillPending = 0; Say(SkillCatalog.LearnAll(Target, skillKind, skillGrade)); }));
            Ui.Label(content, "선택한 경지에서 배울 수 있고 아직 익히지 않은 것만 넣습니다. 옵션은 게임의 뽑기로 정해집니다.", 834, 90, 470, 36, 13).color = Ui.Dim;

            List<SkillCatalog.Entry> found = SkillCatalog.Find(skillKind, appliedSkillQuery);
            Dictionary<int, int> learnedCounts = SkillCatalog.LearnedCounts(target, skillKind);
            int pages = Math.Max(1, (found.Count + rows - 1) / rows);
            skillPage = Mathf.Clamp(skillPage, 0, pages - 1);
            for (int row = 0; row < rows; row++)
            {
                int index = skillPage * rows + row;
                if (index >= found.Count)
                {
                    break;
                }
                SkillCatalog.Entry entry = found[index];
                float y = 136 + row * 50;
                string blocked = SkillCatalog.BlockReason(entry, skillGrade);
                int have;
                learnedCounts.TryGetValue(entry.BaseId, out have);
                bool confirming = skillPending == entry.BaseId;
                Ui.Label(content, Shorten(entry.Name, 16) + "  [" + entry.BaseId + "]  경지 " + entry.GradeMin + "~" + entry.GradeMax, 0, y, 800, 26, 18);
                Text state = Ui.Label(content, have > 0 ? "배움" + (have > 1 ? " x" + have : "") : blocked != null ? "배울 수 없음" : "-", 820, y, 200, 26, 16);
                state.color = have > 0 ? Ui.Warn : Ui.Dim;
                Ui.Label(content, Shorten(entry.Desc, 76), 16, y + 24, 1080, 22, 15).color = Ui.Dim;
                Ui.Button("SkillAdd" + index, content, blocked == null ? (have > 0 ? "한 개 더 추가" : "배우기") : confirming ? "정말 강제" : "강제 추가", ContentW - 170, y + 6, 170, 36, new Action(() =>
                {
                    if (blocked == null)
                    {
                        skillPending = 0;
                        Say(SkillCatalog.Learn(Target, skillKind, entry, skillGrade, false));
                        return;
                    }
                    // 지금 배울 수 없는 것: 이유를 보여 주고, 한 번 더 누르면 조건을 건너뛰고 넣는다.
                    if (!confirming)
                    {
                        skillPending = entry.BaseId;
                        Say(entry.Name + ": " + blocked + ". \"정말 강제\"를 한 번 더 누르면 이 기술의 경지 범위 안에서 가장 가까운 경지로 만들어 넣습니다.");
                        return;
                    }
                    skillPending = 0;
                    Say(SkillCatalog.Learn(Target, skillKind, entry, skillGrade, true));
                }), true, confirming);
            }
            float footer = 136 + rows * 50 + 4;
            Ui.Button("SkillPrev", content, "이전", 0, footer, 90, 38, new Action(() => { skillPage--; Refresh(); }), skillPage > 0);
            Ui.Label(content, (skillPage + 1) + " / " + pages, 100, footer, 110, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("SkillNext", content, "다음", 220, footer, 90, 38, new Action(() => { skillPage++; Refresh(); }), skillPage < pages - 1);
            Ui.Label(content, SkillCatalog.Kinds[skillKind].Tab + " " + found.Count + "개" + (appliedSkillQuery.Length > 0 ? "  ('" + appliedSkillQuery + "' 포함)" : ""), 330, footer, 700, 38, 16).color = Ui.Dim;
        }

        // 옵션 교체 화면. 이 공법에 붙을 수 있는 옵션을 모두 보여 주고, 규칙에 걸리는 것은 사유와 함께 막는다.
        private void RenderPrefixPicker(WorldUnitBase target, DataProps.MartialData martial, int rows)
        {
            List<ConfBattleSkillPrefixValueItem> items = MartialService.ValidItems(martial);
            int pages = Math.Max(1, (items.Count + rows - 1) / rows);
            replacePage = Mathf.Clamp(replacePage, 0, pages - 1);
            Ui.Label(content, MartialService.Name(martial) + " — 옵션 " + (replaceIndex + 1) + " 교체", 0, 0, 640, 36, 21);
            Ui.Button("ReplaceCancel", content, "돌아가기", 780, 0, 130, 36, new Action(() => { replaceIndex = -1; Redraw(); }));
            for (int row = 0; row < rows; row++)
            {
                int index = replacePage * rows + row;
                if (index >= items.Count)
                {
                    break;
                }
                ConfBattleSkillPrefixValueItem item = items[index];
                string blocked = MartialService.ReplaceBlockReason(martial, replaceIndex, item);
                float y = 44 + row * 50;
                Ui.Label(content, item.number + "번  " + Shorten(MartialService.CleanDesc(GameTool.LS(item.desc)), 40), 0, y, 760, 28, 17);
                if (blocked != null)
                {
                    Ui.Label(content, "불가: " + blocked, 30, y + 24, 730, 22, 15).color = Ui.Warn;
                }
                int slot = replaceIndex;
                Ui.Button("ReplacePick" + index, content, "이걸로", 800, y + 4, 110, 36, new Action(() =>
                {
                    string result = MartialService.ReplacePrefix(Target, martialId, slot, item);
                    replaceIndex = -1;
                    Say(result);
                }), blocked == null);
            }
            float footer = 44 + rows * 50 + 4;
            Ui.Button("ReplacePrev", content, "이전", 0, footer, 90, 38, new Action(() => { replacePage--; Refresh(); }), replacePage > 0);
            Ui.Label(content, (replacePage + 1) + " / " + pages, 100, footer, 80, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("ReplaceNext", content, "다음", 190, footer, 90, 38, new Action(() => { replacePage++; Refresh(); }), replacePage < pages - 1);
            Ui.Label(content, "붙을 수 있는 옵션 " + items.Count + "개. 교체하면 품질은 이전 값을 유지합니다(새 옵션의 한도 안에서).", 300, footer, 620, 38, 16).color = Ui.Dim;
        }
    }
}
