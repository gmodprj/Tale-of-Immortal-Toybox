using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 운명 탭: 선천·후천운명의 추가·교체·삭제, 역천개명(경지별) 지정.
    internal sealed class LuckTab : EditorTab
    {
        private int luckPage;
        private int pendingLuckRemove;
        private int luckReplaceIndex = -1;  // 교체할 선천운명 자리. -1이면 후천운명 추가
        private bool luckPicking;
        private int luckType = 1;
        private int luckPickPage;
        private string luckQuery = "";
        private string appliedLuckQuery = "";
        private bool fateOpen;              // 역천개명 화면
        private int fatePickGrade;          // 후보를 고르는 중인 대경지. 0이면 목록 화면
        private int fatePickPage;
        private string fateQuery = "";
        private string appliedFateQuery = "";

        public override string Title
        {
            get { return "운명"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public override void OnTargetChanged()
        {
            luckPicking = false;
            luckPage = 0;
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            const int rows = 14;
            if (fateOpen)
            {
                RenderFate(target);
                return;
            }
            if (luckPicking)
            {
                RenderLuckPicker(target, 12);
                return;
            }
            List<LuckService.LuckView> owned = LuckService.Owned(target).FindAll(v => !v.Hidden);
            int pages = Math.Max(1, (owned.Count + rows - 1) / rows);
            luckPage = Mathf.Clamp(luckPage, 0, pages - 1);
            Ui.Label(content, "운명", 0, 0, 240, 30, 18).color = Ui.Dim;
            Ui.Label(content, "게임 화면의 위치", 250, 0, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "효과", 450, 0, 500, 30, 18).color = Ui.Dim;
            for (int row = 0; row < rows; row++)
            {
                int index = luckPage * rows + row;
                if (index >= owned.Count)
                {
                    break;
                }
                LuckService.LuckView view = owned[index];
                float y = 32 + row * 40;
                bool mine = LuckService.AddedByEditor(target, view.Id);
                bool confirming = pendingLuckRemove == view.Id;
                // 게임의 인물 화면은 후천 목록 가운데 후천 분류만 후천운명 칸에 보여 준다.
                bool shown = view.Born || view.Type == 2;
                Ui.Label(content, Shorten(view.Name, 11), 0, y, 245, 36);
                Text place = Ui.Label(content, view.Born ? "선천운명" : shown ? "후천운명" : "표시 안 됨(효과만)", 250, y, 200, 36, 17);
                if (!shown)
                {
                    place.color = Ui.Warn;
                }
                Ui.Label(content, Shorten(view.Tips, 36), 450, y, 590, 36, 16);
                if (view.Born)
                {
                    int bornIndex = view.BornIndex;
                    Ui.Button("LuckReplace" + index, content, "교체", 1054, y + 1, 120, 34, new Action(() => { luckReplaceIndex = bornIndex; luckPicking = true; luckPickPage = 0; pendingLuckRemove = 0; Redraw(); }));
                }
                else if (view.Type == 1)
                {
                    Ui.Button("LuckToBorn" + index, content, "선천으로 옮김", 1014, y + 1, 160, 34, new Action(() => { pendingLuckRemove = 0; Say(LuckService.AddBorn(Target, view.Id)); }));
                }
                Ui.Button("LuckRemove" + index, content, confirming ? "정말 삭제" : "삭제", 1180, y + 1, 120, 34, new Action(() =>
                {
                    // 편집기가 붙인 후천 목록의 운명만 바로 지운다. 선천운명과 게임이 붙인 운명은 한 번 더 확인한다.
                    if ((view.Born || !mine) && !confirming)
                    {
                        pendingLuckRemove = view.Id;
                        Say(view.Name + ": " + (view.Born ? "선천운명입니다" : "게임이 붙인 운명입니다") + (view.IsSystem ? "(경지 진행 표식)" : "") + ". 지우려면 \"정말 삭제\"를 한 번 더 누르세요.");
                        return;
                    }
                    pendingLuckRemove = 0;
                    Say(view.Born ? LuckService.RemoveBorn(Target, view.BornIndex) : LuckService.Remove(Target, view.Id));
                }), true, confirming);
            }
            float footer = 32 + rows * 40 + 8;
            Ui.Button("LuckPrev", content, "이전", 0, footer, 90, 40, new Action(() => { luckPage--; pendingLuckRemove = 0; Refresh(); }), luckPage > 0);
            Ui.Label(content, (luckPage + 1) + " / " + pages, 100, footer, 80, 40, 18, TextAnchor.MiddleCenter);
            Ui.Button("LuckNext", content, "다음", 190, footer, 90, 40, new Action(() => { luckPage++; pendingLuckRemove = 0; Refresh(); }), luckPage < pages - 1);
            Ui.Button("FateOpen", content, "역천개명 (경지별)", 470, footer, 220, 40, new Action(() => { fateOpen = true; fatePickGrade = 0; pendingLuckRemove = 0; Redraw(); }));
            Ui.Button("LuckAddOpen", content, "운명 추가", 300, footer, 160, 40, new Action(() => { luckReplaceIndex = -1; luckPicking = true; luckPickPage = 0; pendingLuckRemove = 0; Redraw(); }));
            Ui.Label(content, "게임의 인물 화면은 선천운명 칸과, 후천 분류의 후천운명만 보여 줍니다. \"표시 안 됨\"은 효과는 적용되지만 그 화면에 나오지 않는 운명입니다.", 0, footer + 48, ContentW, 30, 15).color = Ui.Dim;
        }

        // 운명 탭의 역천개명 화면: 도달한 경지마다 한 줄. 변경을 누르면 그 경지의 후보 목록이 열린다.
        private void RenderFate(WorldUnitBase target)
        {
            if (fatePickGrade > 0)
            {
                RenderFatePicker(target, 12);
                return;
            }
            Ui.Label(content, "역천개명 — " + EditService.NameOf(target), 0, 0, 600, 36, 21);
            Ui.Button("FateBack", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(() => { fateOpen = false; Redraw(); }));
            Ui.Label(content, "경지", 0, 48, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "역천개명", 210, 48, 240, 30, 18).color = Ui.Dim;
            Ui.Label(content, "효과", 460, 48, 600, 30, 18).color = Ui.Dim;
            List<FateService.Slot> slots = FateService.Slots(target);
            if (slots.Count == 0)
            {
                Ui.Label(content, "역천개명은 축기경부터 경지마다 하나씩 있습니다. 이 인물은 아직 축기경에 이르지 않았습니다.", 0, 84, ContentW, 36, 18).color = Ui.Warn;
            }
            for (int i = 0; i < slots.Count; i++)
            {
                FateService.Slot slot = slots[i];
                float y = 82 + i * 40;
                Ui.Label(content, slot.GradeName, 0, y, 200, 36);
                Text name = Ui.Label(content, slot.LuckId == 0 ? "없음" : Shorten(slot.LuckName, 11), 210, y, 245, 36);
                if (slot.LuckId == 0)
                {
                    name.color = Ui.Dim;
                }
                Ui.Label(content, Shorten(slot.Tips, 44), 460, y, 700, 36, 16);
                Ui.Button("FateChange" + slot.Grade, content, slot.LuckId == 0 ? "정하기" : "변경", 1180, y + 1, 120, 34,
                    new Action(() => { fatePickGrade = slot.Grade; fatePickPage = 0; Redraw(); }));
            }
            Ui.Label(content, "게임의 경지 화면은 경지마다 역천개명 하나를 보여 줍니다. 여기서 정하면 효과와 그 화면의 표시가 함께 바뀝니다. 도달한 경지만 정할 수 있습니다.", 0, 82 + Math.Max(1, slots.Count) * 40 + 12, ContentW, 30, 15).color = Ui.Dim;
        }

        private void RenderFatePicker(WorldUnitBase target, int rows)
        {
            int grade = fatePickGrade;
            Ui.Label(content, "역천개명 고르기 — " + EditService.NameOf(target) + ", 대경지 " + grade, 0, 0, 700, 36, 21);
            Ui.Button("FatePickBack", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(() => { fatePickGrade = 0; Redraw(); }));
            Ui.Input("FateQuery", content, fateQuery, "이름·효과의 일부", 0, 44, 250, 38, new Action<string>(v => fateQuery = v));
            Ui.Button("FateSearch", content, "검색", 258, 44, 80, 38, new Action(() => { appliedFateQuery = fateQuery.Trim(); fatePickPage = 0; Refresh(); }));
            Ui.Button("FateSearchClear", content, "지움", 344, 44, 80, 38, new Action(() => { fateQuery = ""; appliedFateQuery = ""; fatePickPage = 0; Refresh(); }), fateQuery.Length > 0 || appliedFateQuery.Length > 0);
            Ui.Label(content, "이 경지에서 게임이 뽑을 수 있는 역천개명만 나옵니다.", 440, 44, 700, 38, 15).color = Ui.Dim;

            List<LuckService.LuckView> candidates = FateService.Candidates(grade, appliedFateQuery);
            int pages = Math.Max(1, (candidates.Count + rows - 1) / rows);
            fatePickPage = Mathf.Clamp(fatePickPage, 0, pages - 1);
            for (int row = 0; row < rows; row++)
            {
                int index = fatePickPage * rows + row;
                if (index >= candidates.Count)
                {
                    break;
                }
                LuckService.LuckView view = candidates[index];
                float y = 92 + row * 50;
                bool has = LuckService.Has(target, view.Id);
                Ui.Label(content, Shorten(view.Name, 14) + "  (품질 " + view.Level + ")", 0, y, 1000, 26, 18);
                Ui.Label(content, Shorten(view.Tips, 76), 16, y + 24, 1080, 22, 15).color = Ui.Dim;
                Ui.Button("FatePick" + index, content, has ? "보유 중" : "이것으로 정하기", ContentW - 190, y + 6, 190, 36, new Action(() =>
                {
                    string result = FateService.Set(Target, grade, view.Id);
                    fatePickGrade = 0;
                    Say(result);
                }), !has);
            }
            float footer = 92 + rows * 50 + 4;
            Ui.Button("FatePickPrev", content, "이전", 0, footer, 90, 38, new Action(() => { fatePickPage--; Refresh(); }), fatePickPage > 0);
            Ui.Label(content, (fatePickPage + 1) + " / " + pages, 100, footer, 90, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("FatePickNext", content, "다음", 200, footer, 90, 38, new Action(() => { fatePickPage++; Refresh(); }), fatePickPage < pages - 1);
            Ui.Label(content, "후보 " + candidates.Count + "개", 310, footer, 600, 38, 16).color = Ui.Dim;
        }

        private void RenderLuckPicker(WorldUnitBase target, int rows)
        {
            bool replacing = luckReplaceIndex >= 0;
            Ui.Label(content, (replacing ? "선천운명 " + (luckReplaceIndex + 1) + "번 교체 — " : "운명 추가 — ") + EditService.NameOf(target), 0, 0, 600, 36, 21);
            Ui.Button("LuckBack", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(() => { luckPicking = false; Redraw(); }));
            int[] types = { 1, 2, 3 };
            for (int i = 0; i < types.Length; i++)
            {
                int type = types[i];
                Ui.Button("LuckType" + type, content, LuckService.TypeLabel(type) + " 분류", i * 146, 44, 140, 38, new Action(() => { luckType = type; luckPickPage = 0; Refresh(); }), true, luckType == type);
            }
            Ui.Input("LuckQuery", content, luckQuery, "이름·효과의 일부", 460, 44, 250, 38, new Action<string>(v => luckQuery = v));
            Ui.Button("LuckSearch", content, "검색", 718, 44, 80, 38, new Action(() => { appliedLuckQuery = luckQuery.Trim(); luckPickPage = 0; Refresh(); }));
            Ui.Button("LuckSearchClear", content, "지움", 804, 44, 80, 38, new Action(() => { luckQuery = ""; appliedLuckQuery = ""; luckPickPage = 0; Refresh(); }), luckQuery.Length > 0 || appliedLuckQuery.Length > 0);
            string note = replacing ? "" : luckType == 1 ? "선천 분류는 선천운명 칸에 추가됩니다."
                : luckType == 2 ? "후천 분류에는 게임이 진행 상태를 나타내려고 쓰는 운명이 섞여 있습니다."
                : "역천개명 분류는 여기서 붙이면 효과만 적용됩니다. 경지 화면에 나오게 하려면 역천개명 (경지별)을 쓰세요.";
            Ui.Label(content, note, 900, 44, 400, 38, 14).color = Ui.Warn;

            List<LuckService.LuckView> candidates = LuckService.Candidates(luckType, appliedLuckQuery);
            int pages = Math.Max(1, (candidates.Count + rows - 1) / rows);
            luckPickPage = Mathf.Clamp(luckPickPage, 0, pages - 1);
            for (int row = 0; row < rows; row++)
            {
                int index = luckPickPage * rows + row;
                if (index >= candidates.Count)
                {
                    break;
                }
                LuckService.LuckView view = candidates[index];
                float y = 92 + row * 50;
                bool has = LuckService.Has(target, view.Id);
                Text name = Ui.Label(content, Shorten(view.Name, 14) + "  (품질 " + view.Level + ")" + (view.IsSystem ? "  — 경지 진행 표식" : ""), 0, y, 1000, 26, 18);
                if (view.IsSystem)
                {
                    name.color = Ui.Warn;
                }
                Ui.Label(content, Shorten(view.Tips, 76), 16, y + 24, 1080, 22, 15).color = Ui.Dim;
                string label = has ? "보유 중" : replacing ? "이것으로 교체" : view.Type == 1 ? "선천운명에 추가" : view.Type == 2 ? "후천운명에 추가" : "추가";
                Ui.Button("LuckAdd" + index, content, label, ContentW - 190, y + 6, 190, 36, new Action(() =>
                {
                    if (replacing)
                    {
                        string result = LuckService.ReplaceBorn(Target, luckReplaceIndex, view.Id);
                        luckPicking = false;
                        luckReplaceIndex = -1;
                        Say(result);
                        return;
                    }
                    Say(view.Type == 1 ? LuckService.AddBorn(Target, view.Id) : LuckService.Add(Target, view.Id));
                }), !has);
            }
            float footer = 92 + rows * 50 + 4;
            Ui.Button("LuckPickPrev", content, "이전", 0, footer, 90, 38, new Action(() => { luckPickPage--; Refresh(); }), luckPickPage > 0);
            Ui.Label(content, (luckPickPage + 1) + " / " + pages, 100, footer, 90, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("LuckPickNext", content, "다음", 200, footer, 90, 38, new Action(() => { luckPickPage++; Refresh(); }), luckPickPage < pages - 1);
            Ui.Label(content, "후보 " + candidates.Count + "개" + (appliedLuckQuery.Length > 0 ? "  ('" + appliedLuckQuery + "' 포함)" : ""), 310, footer, 900, 38, 16).color = Ui.Dim;
        }
    }
}
