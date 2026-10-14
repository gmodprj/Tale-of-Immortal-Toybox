using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 프리셋 탭: 빌드 프리셋의 목록과 상세보기(단계별 적용). 적용은 항상 플레이어에 한다(편집 대상과 무관).
    // 근거: docs/superpowers/specs/2026-10-14-preset-tab-design.md 5.5절.
    internal sealed class PresetTab : EditorTab
    {
        private const float NameW = 230;
        private const float SummaryW = 420;
        private const float SourceW = 170;
        private const float StageTextX = 125;

        private readonly ListView<PresetService.Preset> list = new ListView<PresetService.Preset>("PresetList", 44);
        private readonly ListView<PresetService.Stage> stages = new ListView<PresetService.Stage>("PresetStages", 44);
        private List<PresetService.Preset> presets;         // null이면 폴더를 다시 읽는다
        private List<PresetService.Learned> learned;        // 한 번 그리는 동안 쓰는 익힌 공법의 사본
        private PresetService.Preset opened;

        public override string Title
        {
            get { return "프리셋"; }
        }

        // 탭에 들어올 때와 편집기를 열 때 폴더를 다시 읽는다(밖에서 파일을 고치거나 넣은 것이 반영된다).
        public override void OnShow()
        {
            presets = null;
        }

        public override void OnOpen()
        {
            presets = null;
        }

        private static string Unreadable(PresetService.Preset preset)
        {
            return "(읽을 수 없음) " + System.IO.Path.GetFileName(preset.File) + " — " + preset.Error;
        }

        protected override void Draw()
        {
            if (presets == null)
            {
                presets = PresetService.List();
                list.Reset();
            }
            learned = PresetService.Snapshot();
            Confirm.Button("PresetRestore", content, "기본 프리셋 되돌리기", "정말 되돌리기", 0, 0, 230, 38, "preset-restore",
                "기본 프리셋 " + PresetService.DefaultCount + "개를 다시 씁니다. 고친 기본 파일은 덮어쓰고, 직접 넣은 다른 파일은 그대로 둡니다. \"정말 되돌리기\"를 한 번 더 누르세요.",
                new Action(() =>
                {
                    string result = PresetService.RestoreDefaults();
                    presets = null;
                    Say(result);
                }));
            int changed = PresetService.DefaultsChanged();
            Text note = Ui.Label(content, changed > 0 ? "기본 프리셋 " + changed + "개가 없거나 고쳐졌습니다." : "", 244, 0, 480, 38, 16);
            note.color = Ui.Warn;
            note.gameObject.name = "PresetNote";
            Ui.Label(content, "지금 경지: " + EditService.GradeName(Player.data.unitData.propertyData.gradeID), ContentW - 330, 0, 330, 38, 18, TextAnchor.MiddleRight);
            Ui.Wrapped(content, "파일: " + PresetService.Dir + " — JSON 파일을 고치거나 넣으면 이 탭에 다시 들어올 때 반영됩니다. 적용은 항상 플레이어에 하고, 익힌 것을 지우지 않고 장착만 바꿉니다. "
                + "빌드는 2022년의 공략을 바탕으로 했고 지금 버전에서의 순위는 확인하지 못했습니다.", 0, 44, ContentW, 44, 14).color = Ui.Dim;

            Ui.Panel("PresetHead", content, 0, 94, ContentW, 30, Ui.Section);
            Ui.Label(content, "이름", 8, 94, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", 245, 94, SummaryW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "출처", 672, 94, SourceW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", 848, 94, 84, 30, 16).color = Ui.Dim;
            list.EmptyText = "프리셋 파일이 없습니다. 위의 \"기본 프리셋 되돌리기\"를 누르세요.";
            list.HeightOf = preset => preset.Error != null
                ? RowFor(44, Ui.TextHeight(Unreadable(preset), ContentW - Ui.ScrollBarWidth - 16, 16))
                : RowFor(44, Ui.TextHeight(preset.Name, NameW, 19), Ui.TextHeight(preset.Summary, SummaryW, 16), Ui.TextHeight(preset.Source, SourceW, 14));
            list.Build(content, 0, 126, ContentW, ContentH - 126, presets, DrawRow);
        }

        private void DrawRow(RectTransform row, PresetService.Preset preset, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            float h = row.sizeDelta.y;
            if (preset.Error != null)
            {
                Ui.Wrapped(row, Unreadable(preset), 8, 0, right - 16, h, 16).color = Ui.Warn;
                return;
            }
            Ui.Wrapped(row, preset.Name, 8, 0, NameW, h, 19);
            Ui.Wrapped(row, preset.Summary, 245, 0, SummaryW, h, 16);
            Ui.Wrapped(row, preset.Source, 672, 0, SourceW, h, 14).color = Ui.Dim;
            Text state = Ui.Label(row, PresetService.AppliedCount(preset, learned) + "/" + PresetService.StageCount + " 단계", 848, 0, 84, h, 16);
            state.gameObject.name = "PresetState" + index;
            Ui.Button("PresetDetail" + index, row, "상세보기", right - 356, 3, 100, 34, new Action(() =>
            {
                opened = preset;
                stages.Reset();
                Push(() => opened.Name + (opened.Summary.Length > 0 ? " — " + opened.Summary : "") + (opened.Source.Length > 0 ? " (출처: " + opened.Source + ")" : ""), DrawDetail);
            }));
            // 지금의 역천개명·신혼·명우를 바꾸게 될 때만 두 번 눌러 확인한다(스펙 4절).
            string replaces = PresetService.ReplacesAll(preset);
            if (replaces == null)
            {
                Ui.Button("PresetApply" + index, row, "적용", right - 250, 3, 90, 34, new Action(() => Say(PresetService.ApplyAll(preset))));
            }
            else
            {
                Confirm.Button("PresetApply" + index, row, "적용", "정말 적용", right - 250, 3, 90, 34, "preset-apply:" + preset.File,
                    preset.Name + ": 지금의 " + replaces + "을(를) 프리셋의 것으로 바꿉니다. \"정말 적용\"을 한 번 더 누르세요.",
                    new Action(() => Say(PresetService.ApplyAll(preset))));
            }
            string replacesFull = PresetService.ReplacesFull(preset);
            Confirm.Button("PresetFull" + index, row, "최종 단계까지", "정말 적용", right - 154, 3, 150, 34, "preset-full:" + preset.File,
                preset.Name + ": 경지를 등선 후기까지 올린 뒤 모든 단계를 적용합니다. 게임의 돌파 절차를 거치지 않으며 능력치는 돌파 증가분만큼 오릅니다."
                + (replacesFull == null ? "" : " 지금의 " + replacesFull + "을(를) 프리셋의 것으로 바꿉니다.") + " \"정말 적용\"을 한 번 더 누르세요.",
                new Action(() => Say(PresetService.ApplyFull(preset))));
        }

        private void DrawDetail()
        {
            learned = PresetService.Snapshot();
            var all = new List<PresetService.Stage>();
            for (int grade = 1; grade <= PresetService.StageCount; grade++)
            {
                all.Add(PresetService.StageOf(opened, grade));
            }
            float right = ContentW - Ui.ScrollBarWidth;
            float textW = right - StageTextX - 400;
            // 쓸 수 없는 줄을 위에 모아 알린다. 대경지가 틀린 역천개명처럼 어느 단계에도 들지 않는 줄은 여기에만 나온다.
            float top = SubTop;
            List<string> problems = PresetService.Problems(opened);
            if (problems.Count > 0)
            {
                string warning = "쓸 수 없는 줄 " + problems.Count + "개(적용에서 빠집니다): " + string.Join(" / ", problems.ToArray());
                float warningH = Math.Max(30, (float)Math.Ceiling(Ui.TextHeight(warning, ContentW, 15)) + 6);
                Text shown = Ui.Wrapped(content, warning, 0, top, ContentW, warningH, 15);
                shown.color = Ui.Warn;
                shown.gameObject.name = "PresetProblems";
                top += warningH + 4;
            }
            Ui.Panel("StageHead", content, 0, top, ContentW, 30, Ui.Section);
            Ui.Label(content, "단계", 8, top, 110, 30, 16).color = Ui.Dim;
            Ui.Label(content, "구성", StageTextX, top, textW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", right - 390, top, 120, 30, 16).color = Ui.Dim;
            stages.HeightOf = stage => RowFor(44, Ui.TextHeight(PresetService.StageText(stage), textW, 16));
            stages.Build(content, 0, top + 32, ContentW, ContentH - top - 32, all, (row, stage, index) => DrawStage(row, stage, textW));
        }

        private void DrawStage(RectTransform row, PresetService.Stage stage, float textW)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            float h = row.sizeDelta.y;
            int grade = stage.Grade;
            Ui.Label(row, grade + " " + PresetService.StageName(grade), 8, 0, 110, h, 18);
            Ui.Wrapped(row, PresetService.StageText(stage), StageTextX, 0, textW, h, 16);
            int have, total;
            PresetService.StageState state = PresetService.StateOf(stage, learned, out have, out total);
            string label = state == PresetService.StageState.Locked ? "경지 미달"
                : state == PresetService.StageState.Applied ? "적용됨"
                : state == PresetService.StageState.Partial ? "일부(" + have + "/" + total + ")" : "적용 전";
            Text text = Ui.Label(row, label, right - 390, 0, 120, h, 16);
            text.gameObject.name = "StageState" + grade;
            if (state == PresetService.StageState.Locked)
            {
                text.color = Ui.Dim;
                Confirm.Button("StageRaise" + grade, row, "경지를 이 단계까지 올리기", "정말 올리기", right - 264, 3, 260, 34, "stage-raise:" + grade,
                    PresetService.StageName(grade) + " 초기까지 경지를 올립니다. 게임의 돌파 절차를 거치지 않으며 능력치는 돌파 증가분만큼 오릅니다. \"정말 올리기\"를 한 번 더 누르세요.",
                    new Action(() => Say(PresetService.RaiseToStage(grade))));
                return;
            }
            string replaces = PresetService.ReplacesStage(opened, grade);
            if (replaces == null)
            {
                Ui.Button("StageApply" + grade, row, "적용", right - 104, 3, 100, 34, new Action(() => Say(PresetService.ApplyStage(opened, grade))));
            }
            else
            {
                Confirm.Button("StageApply" + grade, row, "적용", "정말 적용", right - 104, 3, 100, 34, "stage-apply:" + opened.File + ":" + grade,
                    grade + " " + PresetService.StageName(grade) + ": 지금의 " + replaces + "을(를) 프리셋의 것으로 바꿉니다. \"정말 적용\"을 한 번 더 누르세요.",
                    new Action(() => Say(PresetService.ApplyStage(opened, grade))));
            }
        }
    }
}
