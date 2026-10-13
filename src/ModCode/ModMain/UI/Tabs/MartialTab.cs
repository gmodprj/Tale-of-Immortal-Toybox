using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 공법 탭: 익힌 공법의 옵션·숙련 경험치·장착·망각. 하위 화면: 기술 추가, 옵션 교체.
    internal sealed class MartialTab : EditorTab
    {
        private sealed class Learned
        {
            public string Id;
            public DataUnit.ActionMartialData Data;
        }

        private const float BoxH = 330;

        private readonly ListView<Learned> martialList = new ListView<Learned>("MartialList", 38);
        private readonly ListView<SkillCatalog.Entry> skillList = new ListView<SkillCatalog.Entry>("SkillList", 50);
        private readonly ListView<ConfBattleSkillPrefixValueItem> prefixList = new ListView<ConfBattleSkillPrefixValueItem>("PrefixList", 50);
        private readonly DetailPane optionDetail = new DetailPane("OptionDetail", "옵션의 글을 누르면 여기에 전체가 나옵니다.");
        private readonly DetailPane skillDetail = new DetailPane("SkillDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");
        private readonly DetailPane prefixDetail = new DetailPane("PrefixDetail", "줄을 누르면 여기에 전체 설명이 나옵니다.");

        private string martialId;       // 선택한 공법
        private int replaceIndex = -1;  // 옵션 교체 화면에서 바꾸는 자리
        private int skillKind;
        private int skillGrade;
        private string skillQuery = "";

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

        public MartialTab()
        {
            martialList.EmptyText = "익힌 공법이 없습니다.";
            skillList.EmptyText = "조건에 맞는 기술이 없습니다.";
            skillList.OnSelect = entry => skillDetail.Show(entry.Name + "  [" + entry.BaseId + "]  경지 " + entry.GradeMin + "~" + entry.GradeMax + "\n" + entry.Desc);
            prefixList.OnSelect = item => prefixDetail.Show(item.number + "번  " + MartialService.CleanDesc(GameTool.LS(item.desc)));
        }

        public override void OnTargetChanged()
        {
            martialId = null;
            replaceIndex = -1;
            skillGrade = 0;
            martialList.Reset();
            optionDetail.Clear();
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            var all = new List<Learned>();
            foreach (var pair in target.data.unitData.allActionMartial)
            {
                all.Add(new Learned { Id = pair.Key, Data = pair.Value });
            }
            DataProps.MartialData selected = martialId == null ? null : MartialService.Martial(target, martialId);
            if (selected == null)
            {
                martialId = null;
            }

            Ui.Label(content, "공법", 8, 0, 300, 30, 18).color = Ui.Dim;
            Ui.Label(content, "종류", 338, 0, 120, 30, 18).color = Ui.Dim;
            Ui.Label(content, "경지", 468, 0, 80, 30, 18).color = Ui.Dim;
            Ui.Label(content, "숙련 경험치", 558, 0, 150, 30, 18).color = Ui.Dim;
            Ui.Button("SkillAddOpen", content, "기술 추가", ContentW - 160, 0, 160, 30, new Action(() =>
            {
                skillGrade = 0;
                skillList.Reset();
                skillDetail.Clear();
                Push(() => "기술 추가 — " + EditService.NameOf(Target), DrawSkillAdd);
            }));
            float listH = ContentH - 32 - BoxH - 10;
            martialList.Build(content, 0, 32, ContentW, listH, all, DrawMartialRow);

            float box = 32 + listH + 10;
            Ui.Panel("MartialBox", content, 0, box, ContentW, BoxH, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "위에서 공법을 선택하면 옵션을 편집할 수 있습니다.", 16, box + 10, 880, 36);
                return;
            }
            Ui.Label(content, MartialService.Name(selected) + "  (" + MartialService.TypeLabel(selected.martialType) + ")", 16, box + 6, 420, 36, 21);
            Ui.Button("MartialExp", content, "숙련 경험치 + " + Step, 450, box + 8, 230, 34, new Action(() => Say(MartialService.AddExp(Target, martialId, Step))));
            AmountBar.Build(content, "martialExp", 700, box + 6);

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
                Ui.Button("OptionText" + slot, content, (slot + 1) + ". " + Shorten(view.Desc, 28), 16, y, 520, 32, new Action(() => optionDetail.Show((slot + 1) + ". " + view.Desc)));
                Ui.Label(content, "품질 " + view.Level + "/" + view.MaxLevel, 545, y, 110, 34, 18);
                Ui.Button("QualityMinus" + slot, content, "-", 660, y, 44, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.Level - 1))), view.Level > 1);
                Ui.Button("QualityPlus" + slot, content, "+", 708, y, 44, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.Level + 1))), view.Level < view.MaxLevel);
                Ui.Button("QualityMax" + slot, content, "최고", 756, y, 66, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.MaxLevel))), view.Level != view.MaxLevel);
                Ui.Button("Replace" + slot, content, "교체", 826, y, 80, 32, new Action(() =>
                {
                    replaceIndex = slot;
                    prefixList.Reset();
                    prefixDetail.Clear();
                    Push(() =>
                    {
                        DataProps.MartialData martial = MartialService.Martial(Target, martialId);
                        return (martial == null ? "공법" : MartialService.Name(martial)) + " — 옵션 " + (replaceIndex + 1) + " 교체";
                    }, DrawPrefixPicker);
                }));
            }
            optionDetail.Build(content, 920, box + 50, ContentW - 936, 228);

            // 장착·해제와 망각. 망각은 한 번 더 확인한다.
            bool equipped = MartialService.IsEquipped(target, martialId);
            Ui.Button("MartialEquip", content, equipped ? "장착 해제" : "장착", 16, box + 286, 150, 34,
                new Action(() => Say(equipped ? MartialService.Unequip(Target, martialId) : MartialService.Equip(Target, martialId))));
            Confirm.Button("MartialForget", content, "기술 망각", "정말 망각", 176, box + 286, 150, 34, "forget:" + martialId,
                "이 공법을 익힌 목록에서 지웁니다. 계속하려면 \"정말 망각\"을 한 번 더 누르세요.", new Action(() =>
                {
                    string result = MartialService.Forget(Target, martialId);
                    martialId = null;
                    optionDetail.Clear();
                    Say(result);
                }));
            Ui.Label(content, "수련은 위의 숙련 경험치로, 참오는 위 옵션의 품질·교체로 대신합니다.", 340, box + 286, 570, 34, 15).color = Ui.Dim;
        }

        private void DrawMartialRow(RectTransform row, Learned learned, int index)
        {
            DataProps.MartialData martial = learned.Data.data.To<DataProps.MartialData>();
            bool picked = learned.Id == martialId;
            Ui.Label(row, MartialService.Name(martial), 8, 0, 320, 38);
            Ui.Label(row, MartialService.TypeLabel(martial.martialType), 338, 0, 120, 38);
            Ui.Label(row, martial.grade.ToString(), 468, 0, 80, 38);
            Ui.Label(row, learned.Data.exp.ToString(), 558, 0, 150, 38);
            Ui.Button("PickMartial" + index, row, picked ? "선택됨" : "선택", 728, 3, 110, 32, new Action(() =>
            {
                martialId = learned.Id;
                optionDetail.Clear();
                Redraw();
            }), true, picked);
            Ui.Label(row, MartialService.IsEquipped(Target, learned.Id) ? "장착 중" : "", 848, 0, 120, 38, 17).color = Ui.Warn;
        }

        // 하위 화면: 게임 기술 화면의 다섯 분류에서 이름으로 골라 배운 상태로 넣는다.
        private void DrawSkillAdd()
        {
            WorldUnitBase target = Target;
            float y = SubTop;
            for (int i = 0; i < SkillCatalog.Kinds.Length; i++)
            {
                int index = i;
                Ui.Button("SkillKind" + i, content, SkillCatalog.Kinds[i].Tab, i * 156, y, 150, 38, new Action(() =>
                {
                    skillKind = index;
                    skillList.Reset();
                    skillDetail.Clear();
                    Redraw();
                }), true, skillKind == i);
            }
            SearchBox.Build(content, "SkillQuery", 800, y, 330, skillQuery, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                skillQuery = v;
                skillDetail.Clear();
                skillList.SetItems(SkillCatalog.Find(skillKind, skillQuery));
            }));

            // 만들 기술의 경지. 처음에는 편집 대상의 현재 대경지.
            if (skillGrade <= 0)
            {
                skillGrade = Math.Max(1, FateService.CurrentGrade(target));
            }
            Ui.Label(content, "경지", 0, y + 46, 60, 36, 18).color = Ui.Dim;
            for (int grade = 1; grade <= 10; grade++)
            {
                int value = grade;
                Ui.Button("SkillGrade" + grade, content, grade.ToString(), 60 + (grade - 1) * 50, y + 46, 46, 36, new Action(() => { skillGrade = value; Redraw(); }), true, skillGrade == grade);
            }
            Ui.Button("SkillLearnAll", content, SkillCatalog.Kinds[skillKind].Tab + " 모두 배우기", 580, y + 46, 240, 36, new Action(() => Say(SkillCatalog.LearnAll(Target, skillKind, skillGrade))));
            Ui.Label(content, "선택한 경지에서 배울 수 있고 아직 익히지 않은 것만 넣습니다. 옵션은 게임의 뽑기로 정해집니다.", 834, y + 46, 470, 36, 13).color = Ui.Dim;

            learnedCounts = SkillCatalog.LearnedCounts(target, skillKind);
            float top = y + 92;
            skillList.Build(content, 0, top, ContentW, ContentH - top - 78, SkillCatalog.Find(skillKind, skillQuery), DrawSkillRow);
            skillDetail.Build(content, 0, ContentH - 70, ContentW, 70);
        }

        private Dictionary<int, int> learnedCounts = new Dictionary<int, int>();

        private void DrawSkillRow(RectTransform row, SkillCatalog.Entry entry, int index)
        {
            string blocked = SkillCatalog.BlockReason(entry, skillGrade);
            int have;
            learnedCounts.TryGetValue(entry.BaseId, out have);
            Ui.Label(row, Shorten(entry.Name, 16) + "  [" + entry.BaseId + "]  경지 " + entry.GradeMin + "~" + entry.GradeMax, 8, 0, 800, 26, 18);
            Text state = Ui.Label(row, have > 0 ? "배움" + (have > 1 ? " x" + have : "") : blocked != null ? "배울 수 없음" : "-", 820, 0, 200, 26, 16);
            state.color = have > 0 ? Ui.Warn : Ui.Dim;
            Ui.Label(row, Shorten(entry.Desc, 76), 24, 24, 1080, 22, 15).color = Ui.Dim;
            float x = ContentW - Ui.ScrollBarWidth - 178;
            if (blocked == null)
            {
                Ui.Button("SkillAdd" + entry.BaseId, row, have > 0 ? "한 개 더 추가" : "배우기", x, 7, 170, 36,
                    new Action(() => Say(SkillCatalog.Learn(Target, skillKind, entry, skillGrade, false))));
                return;
            }
            // 지금 배울 수 없는 것: 이유를 보여 주고, 한 번 더 누르면 조건을 건너뛰고 넣는다.
            Confirm.Button("SkillAdd" + entry.BaseId, row, "강제 추가", "정말 강제", x, 7, 170, 36, "skill:" + entry.BaseId,
                entry.Name + ": " + blocked + ". \"정말 강제\"를 한 번 더 누르면 이 기술의 경지 범위 안에서 가장 가까운 경지로 만들어 넣습니다.",
                new Action(() => Say(SkillCatalog.Learn(Target, skillKind, entry, skillGrade, true))));
        }

        // 하위 화면: 이 공법에 붙을 수 있는 옵션을 모두 보여 주고, 규칙에 걸리는 것은 사유와 함께 막는다.
        private void DrawPrefixPicker()
        {
            DataProps.MartialData martial = martialId == null ? null : MartialService.Martial(Target, martialId);
            if (martial == null || replaceIndex < 0)
            {
                Ui.Label(content, "공법을 찾지 못했습니다. 돌아가서 다시 선택하세요.", 0, SubTop, 800, 36).color = Ui.Warn;
                return;
            }
            pickerMartial = martial;
            List<ConfBattleSkillPrefixValueItem> items = MartialService.ValidItems(martial);
            Ui.Label(content, "붙을 수 있는 옵션 " + items.Count + "개. 교체하면 품질은 이전 값을 유지합니다(새 옵션의 한도 안에서).", 0, SubTop, 900, 30, 16).color = Ui.Dim;
            float top = SubTop + 36;
            prefixList.Build(content, 0, top, ContentW, ContentH - top - 78, items, DrawPrefixRow);
            prefixDetail.Build(content, 0, ContentH - 70, ContentW, 70);
        }

        private DataProps.MartialData pickerMartial;

        private void DrawPrefixRow(RectTransform row, ConfBattleSkillPrefixValueItem item, int index)
        {
            string blocked = MartialService.ReplaceBlockReason(pickerMartial, replaceIndex, item);
            Ui.Label(row, item.number + "번  " + Shorten(MartialService.CleanDesc(GameTool.LS(item.desc)), 60), 8, 0, 1080, 28, 17);
            if (blocked != null)
            {
                Ui.Label(row, "불가: " + blocked, 38, 24, 1050, 22, 15).color = Ui.Warn;
            }
            int slot = replaceIndex;
            Ui.Button("ReplacePick" + item.number, row, "이걸로", ContentW - Ui.ScrollBarWidth - 118, 7, 110, 36, new Action(() =>
            {
                string result = MartialService.ReplacePrefix(Target, martialId, slot, item);
                CloseViews();
                Say(result);
            }), blocked == null);
        }
    }
}
