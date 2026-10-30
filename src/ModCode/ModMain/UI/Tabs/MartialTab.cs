using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 공법 탭: 익힌 공법의 옵션·숙련 경험치·장착·망각. 하위 화면: 기술 추가, 옵션 교체.
    internal sealed partial class MartialTab : EditorTab
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
        private readonly ListView<MartialService.SuitView> suitList = new ListView<MartialService.SuitView>("SuitList", 92);
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

#if DEVTOOLS
        // 개발 전용: 검사가 고유 번호로 공법을 고른다(목록 순번은 실행마다 다를 수 있다).
        internal static string DevPick;
#endif

        protected override void Draw()
        {
            WorldUnitBase target = Target;
#if DEVTOOLS
            if (DevPick != null)
            {
                martialId = DevPick;
                DevPick = null;
            }
#endif
            var all = new List<Learned>();
            foreach (var pair in target.data.unitData.allActionMartial)
            {
                all.Add(new Learned { Id = pair.Key, Data = pair.Value });
            }
            // 선택한 공법과 그 이름·옵션. 깨진 공법이면(읽기나 To<>에서 예외) 읽지 못한 이유만 남긴다(리뷰 C1).
            DataProps.MartialData selected = null;
            string selectedTitle = null;
            string selectedBroken = null;
            var prefixes = new List<MartialService.PrefixView>();
            if (martialId != null)
            {
                try
                {
                    selected = MartialService.Martial(target, martialId);
                    if (selected != null)
                    {
                        selectedTitle = MartialService.Name(selected) + "  (" + MartialService.TypeLabel(selected.martialType) + ")";
                        prefixes = MartialService.Prefixes(selected);
                    }
                }
                catch (Exception e)
                {
                    selectedBroken = e.GetType().Name;
                }
                if (selected == null && selectedBroken == null)
                {
                    martialId = null;
                }
            }

            Ui.Label(content, "공법", 8, 0, 300, 30, 18).color = Ui.Dim;
            Ui.Label(content, "종류", 338, 0, 120, 30, 18).color = Ui.Dim;
            Ui.Label(content, "경지", 468, 0, 80, 30, 18).color = Ui.Dim;
            Ui.Label(content, "숙련 경험치", 558, 0, 150, 30, 18).color = Ui.Dim;
            // 기술 학습 상한(05 문서 60절): 게임의 기술 칸 수에 더한다. NPC에게도 적용된다(게임 함수가 인물을 받지 않는다).
            float gx = ContentW - 160 - 12 - MartialGridService.Steps.Length * 70;
            Ui.Label(content, "학습 상한", gx - 96, 0, 92, 30, 16).color = Ui.Dim;
            for (int i = 0; i < MartialGridService.Steps.Length; i++)
            {
                int step = MartialGridService.Steps[i];
                Ui.Button("MartialGrid" + step, content, MartialGridService.StepName(step), gx + i * 70, 0, 66, 30,
                    new Action(() => Say(MartialGridService.Set(step))), true, MartialGridService.Bonus == step);
            }
            Ui.Button("SkillAddOpen", content, "기술 추가", ContentW - 160, 0, 160, 30, new Action(() =>
            {
                skillGrade = 0;
                skillList.Reset();
                skillDetail.Clear();
                Push(() => "기술 추가 — " + EditService.NameOf(Target), DrawSkillAdd);
            }));
            // 옵션의 글은 줄바꿈해 전부 보인다. 두 줄 넘는 옵션이 있으면 그만큼 아래 상자를 키우고 위의 목록을 줄인다.
            float grown = 0;
            // 대도까지 채우기(83절)로 옵션이 11개까지 늘 수 있다. 6줄을 넘는 만큼(줄마다 38) 상자를 키우고 위의 목록을 줄인다.
            for (int i = 0; i < prefixes.Count; i++)
            {
                grown += OptionHeight(prefixes[i]) - 32;
            }
            grown += Math.Max(0, prefixes.Count - 6) * 38;
            float boxH = BoxH + grown;
            float listH = ContentH - 32 - boxH - 10;
            martialList.Build(content, 0, 32, ContentW, listH, all, DrawMartialRow);

            float box = 32 + listH + 10;
            Ui.Panel("MartialBox", content, 0, box, ContentW, boxH, Ui.Section);
            if (selectedBroken != null)
            {
                Text note = Ui.Label(content, "읽을 수 없는 공법입니다(" + selectedBroken + "). 기술 망각으로 지울 수 있습니다.", 16, box + 10, 880, 36);
                note.color = Ui.Warn;
                note.gameObject.name = "MartialBrokenNote";
                ForgetButton(box + 52);
                return;
            }
            if (selected == null)
            {
                Ui.Label(content, "위에서 공법을 선택하면 옵션을 편집할 수 있습니다.", 16, box + 10, 880, 36);
                return;
            }
            Ui.Label(content, selectedTitle, 16, box + 6, 420, 36, 21);
            Ui.Button("MartialExp", content, "숙련 경험치 + " + Step, 450, box + 8, 230, 34, new Action(() => Say(MartialService.AddExp(Target, martialId, Step))));
            AmountBar.Build(content, "martialExp", 700, box + 6);

            if (prefixes.Count == 0)
            {
                Ui.Label(content, "이 공법에는 옵션이 없습니다.", 16, box + 52, 880, 34, 18).color = Ui.Dim;
            }
            float y = box + 50;
            float optionH = 0;
            for (int i = 0; i < prefixes.Count; i++)
            {
                MartialService.PrefixView view = prefixes[i];
                int slot = view.Index;
                y += optionH;
                string optionText = OptionText(view);
                float buttonH = OptionHeight(view);
                optionH = buttonH + 6;
                WrapLabel(Ui.Button("OptionText" + slot, content, optionText, 16, y, 520, buttonH, new Action(() => optionDetail.Show(optionText))), 15);
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
            optionDetail.Build(content, 920, box + 50, ContentW - 936, 228 + grown);

            // 장착·해제와 망각. 망각은 한 번 더 확인한다.
            bool equipped = MartialService.IsEquipped(target, martialId);
            Ui.Button("MartialEquip", content, equipped ? "장착 해제" : "장착", 16, box + 286 + grown, 150, 34,
                new Action(() => Say(equipped ? MartialService.Unequip(Target, martialId) : MartialService.Equip(Target, martialId))));
            ForgetButton(box + 286 + grown);
            // 수련 단계 대도까지 채우기(05 문서 83절).
            float rowY = box + 286 + grown;
            MasteryService.State mastery = MasteryService.Read(Target, martialId);
            bool blocked = mastery.Block != null && mastery.Options < MasteryRules.MaxOptions;
            bool full = mastery.Options >= MasteryRules.MaxOptions;
            Text stageText = Ui.Wrapped(content, blocked ? "수련 단계 " + mastery.Stages + "/12 · " + mastery.Block
                : full ? "수련 단계 " + mastery.Stages + "/12 · 대도까지 있음" : "수련 단계 " + mastery.Stages + "/12",
                340, box + 280 + grown, 160, 44, blocked || full ? 12 : 16);
            stageText.gameObject.name = "MasteryText";
            // 긴 까닭 글이 두 줄 칸에서 잘리지 않게 글자를 줄여 맞춘다(05 문서 85절).
            // Ui.Label은 세로 넘침을 Overflow로 두는데, 그러면 BestFit이 줄이지 않는다(최종 리뷰). 칸 안으로 자른다.
            stageText.verticalOverflow = VerticalWrapMode.Truncate;
            stageText.resizeTextForBestFit = true;
            stageText.resizeTextMinSize = 9;
            stageText.resizeTextMaxSize = blocked || full ? 12 : 16;
            if (blocked)
            {
                stageText.color = Ui.Warn;
            }
            Ui.Button("MasteryOpen", content, "대도까지…", 504, rowY, 116, 34, new Action(OpenMastery), mastery.Block == null);
            float hintX = 632;
            DataProps.MartialData chosen = selected;   // 위에서 한 번 읽은 것(13 문서 P4)
            if (chosen != null && chosen.martialType == MartialType.Ability)
            {
                // 전승(심법 세트). 트레이너 3차 5.4.
                DataUnit.ActionMartialData action = Target.data.unitData.GetActionMartial(martialId);
                int suit = action == null ? 0 : MartialService.SuitOf(action.data);
                Ui.Label(content, "전승: " + (suit == 0 ? "없음" : PresetService.SuitName(suit)), 632, rowY, 190, 34, 16).gameObject.name = "MartialSuitText";
                Ui.Button("MartialSuitOpen", content, "전승…", 826, rowY, 90, 34, new Action(OpenSuits));
                hintX = 928;
            }
            Text hint = Ui.Wrapped(content, "참오 결과 최대는 성공·드롭 탭에 있습니다.",
                hintX, box + 280 + grown, ContentW - hintX, 44, 14);
            hint.color = Ui.Dim;
            hint.gameObject.name = "MartialHint";
        }

        // 망각은 한 번 더 확인한다. 깨진 공법도 고유 번호만으로 지운다.
        private void ForgetButton(float y)
        {
            Confirm.Button("MartialForget", content, "기술 망각", "정말 망각", 176, y, 150, 34, "forget:" + martialId,
                "이 공법을 익힌 목록에서 지웁니다. 계속하려면 \"정말 망각\"을 한 번 더 누르세요.", new Action(() =>
                {
                    string result = MartialService.Forget(Target, martialId);
                    martialId = null;
                    optionDetail.Clear();
                    Say(result);
                }));
        }

        private void DrawMartialRow(RectTransform row, Learned learned, int index)
        {
            bool picked = learned.Id == martialId;
            string name, type, grade;
            bool broken = false;
            try
            {
                DataProps.MartialData martial = learned.Data.data.To<DataProps.MartialData>();
                name = MartialService.Name(martial);
                type = MartialService.TypeLabel(martial.martialType);
                grade = martial.grade.ToString();
            }
            catch (Exception e)
            {
                // 게임이 알아보지 못하는 값으로 만든 공법은 읽는 것만으로 예외가 난다(인계 6절). 줄은 남겨 선택·망각으로 치울 수 있게 한다(리뷰 C1).
                broken = true;
                name = "(읽을 수 없음) " + e.GetType().Name;
                type = "-";
                grade = "-";
            }
            Text nameLabel = Ui.Label(row, name, 8, 0, 320, 38);
            if (broken)
            {
                nameLabel.color = Ui.Warn;
                nameLabel.gameObject.name = "MartialBroken" + index;
            }
            Ui.Label(row, type, 338, 0, 120, 38);
            Ui.Label(row, grade, 468, 0, 80, 38);
            Ui.Label(row, learned.Data.exp.ToString(), 558, 0, 150, 38);
            Ui.Button("PickMartial" + index, row, picked ? "선택됨" : "선택", 728, 3, 110, 32, new Action(() =>
            {
                martialId = learned.Id;
                optionDetail.Clear();
                Redraw();
            }), true, picked);
            Ui.Label(row, MartialService.IsEquipped(Target, learned.Id) ? "장착 중" : "", 848, 0, 120, 38, 17).color = Ui.Warn;
        }
    }
}
