using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 신기 탭: 신기 셋 켜고 끄기, 쌍어옥패 점수. 하위 화면: 호천안(손상, 요수 해금, 기술 장착), 연요호(손상, 가진 호요의 경험치, 호요 추가).
    // 세계 자료에 쓰므로 편집 대상과 무관하다(스펙 docs/superpowers/specs/2026-10-14-artifact-tab-design.md).
    internal sealed class ArtifactTab : EditorTab
    {
        private sealed class Line
        {
            public GodEyeService.Boss Boss;
            public GodEyeService.Skill Skill;   // null이면 요수 줄
        }

        private readonly ListView<Line> eyeList = new ListView<Line>("GodEyeList", 44);
        private readonly ListView<PotmonService.Pet> petList = new ListView<PotmonService.Pet>("PotmonOwnedList", 44);
        private readonly ListView<PotmonService.Kind> kindList = new ListView<PotmonService.Kind>("PotmonKindList", 44);
        private string eyeQuery = "";
        private string petQuery = "";
        private int petGrade = 1;

        public override string Title
        {
            get { return "신기"; }
        }

        public ArtifactTab()
        {
            eyeList.EmptyText = "조건에 맞는 요수가 없습니다.";
            petList.EmptyText = "가진 호요가 없습니다.";
            kindList.EmptyText = "조건에 맞는 호요가 없습니다.";
        }

        protected override void Draw()
        {
            if (!ArtifactService.Ready)
            {
                Ui.Label(content, "세계 자료가 없습니다(세계에 들어간 뒤에 씁니다).", 0, 0, 800, 36).gameObject.name = "ArtifactNoData";
                return;
            }
            Ui.Label(content, "신기", 0, 0, 200, 36, 21);
            Ui.Wrapped(content, ArtifactService.ButtonNote, 220, 0, ContentW - 220, 36, 15).color = Ui.Dim;
            float y = 46;
            float descW = ContentW - 640;
            foreach (int id in ArtifactService.Ids)
            {
                int n = id;
                bool has = ArtifactService.Has(n);
                string desc = ArtifactService.Desc(n);
                float h = RowFor(50, Ui.TextHeight(desc, descW, 15));
                float by = y + (h - 36) / 2;
                Ui.Panel("ArtifactRow" + n, content, 0, y, ContentW, h, Ui.Section);
                Ui.Label(content, ArtifactService.Name(n), 12, y, 120, h, 19);
                Text state = Ui.Label(content, has ? "가짐" : "없음", 140, y, 70, h, 17);
                state.color = has ? Ui.Warn : Ui.Dim;
                state.gameObject.name = "ArtifactState" + n;
                Ui.Wrapped(content, desc, 220, y, descW, h, 15).color = Ui.Dim;
                float bx = ContentW - 410;
                if (n != 1 && !has)
                {
                    Text need = Ui.Wrapped(content, "신기를 먼저 켜세요", bx, y, 120, h, 15);
                    need.color = Ui.Warn;
                    need.gameObject.name = "ArtifactNeed" + n;
                }
                else
                {
                    Ui.Label(content, n == 2 ? ArtifactService.GodEyeState() : n == 3 ? ArtifactService.DevilState() : "", bx, y, 120, h, 15).color = Ui.Dim;
                }
                if (has)
                {
                    Confirm.Button("ArtifactOff" + n, content, "끄기", "정말 끄기", bx + 126, by, 130, 36, "artifact-off-" + n,
                        ArtifactService.Name(n) + "을(를) 끕니다. 그 신기의 자료는 남고, 지도 화면의 버튼은 다시 불러온 뒤에 사라집니다. \"정말 끄기\"를 한 번 더 누르세요.",
                        new Action(() => Say(ArtifactService.Disable(n))), ArtifactService.Count > 1);
                }
                else
                {
                    Ui.Button("ArtifactOn" + n, content, "켜기", bx + 126, by, 130, 36, new Action(() => Say(ArtifactService.Enable(n))));
                }
                if (n != 1)
                {
                    Ui.Button("ArtifactOpen" + n, content, "자세히", bx + 262, by, 130, 36, new Action(() => Open(n)), has);
                }
                y += h + 6;
            }
            if (ArtifactService.Count <= 1)
            {
                Text last = Ui.Wrapped(content, ArtifactService.LastReason, 0, y, ContentW, 30, 15);
                last.color = Ui.Warn;
                last.gameObject.name = "ArtifactLast";
                y += 34;
            }
            y += 14;
            Ui.Label(content, "쌍어옥패 점수", 0, y, 160, 36, 19);
            NumberField.Build(content, "ArtifactPoint", 170, y, 140, 36, ArtifactService.Point.ToString(), null, new Action<int>(v => Say(ArtifactService.SetPoint(v))));
            string[] labels = { "-10", "-1", "+1", "+10" };
            string[] names = { "ArtifactPointMinus10", "ArtifactPointMinus1", "ArtifactPointPlus1", "ArtifactPointPlus10" };
            int[] steps = { -10, -1, 1, 10 };
            for (int i = 0; i < steps.Length; i++)
            {
                int step = steps[i];
                Ui.Button(names[i], content, labels[i], 320 + i * 76, y, 70, 36, new Action(() => Say(ArtifactService.SetPoint(ArtifactService.Point + step))));
            }
            Ui.Wrapped(content, "전투에서 쓰러지면 점수를 써서 되살아납니다(0~" + ArtifactService.PointMax + "). 게임 화면: 부활 정보의 \"쌍어옥패 N점\".", 640, y, ContentW - 640, 40, 15).color = Ui.Dim;
        }

        private void Open(int n)
        {
            if (n == 2)
            {
                eyeList.Reset();
                Push(() => "호천안", DrawGodEye);
                return;
            }
            petList.Reset();
            kindList.Reset();
            petGrade = Math.Min(PotmonService.MaxGrade, ArtifactService.PlayerMajor());
            Push(() => "연요호", DrawDevil);
        }

        private static List<Line> Lines(List<GodEyeService.Boss> bosses)
        {
            var lines = new List<Line>();
            foreach (GodEyeService.Boss b in bosses)
            {
                lines.Add(new Line { Boss = b });
                foreach (GodEyeService.Skill s in b.Skills)
                {
                    lines.Add(new Line { Boss = b, Skill = s });
                }
            }
            return lines;
        }

        // 기술 줄: 이름 40~260, 설명 270~(270+DescW), 상태, 버튼.
        private static float DescW
        {
            get { return ContentW - Ui.ScrollBarWidth - 540; }
        }

        private void DrawGodEye()
        {
            float y = SubTop;
            Ui.Label(content, "상태: " + ArtifactService.GodEyeState(), 0, y, 290, 36, 18).gameObject.name = "GodEyeStateText";
            Ui.Button("GodEyeRepair", content, "손상 해제", 300, y, 130, 36, new Action(() => Say(ArtifactService.RepairGodEye())), ArtifactService.GodEyeDamaged);
            GodEyeService.Skill equipped = GodEyeService.FindSkill(GodEyeService.Equipped);
            Ui.Label(content, "장착: " + (equipped == null ? "없음" : equipped.Name), 450, y, 400, 36, 18).gameObject.name = "GodEyeEquipText";
            Confirm.Button("GodEyeUnlockAll", content, "모든 요수 해금", "정말 모두", ContentW - 220, y, 220, 36, "godeye-all",
                "모든 요수를 지금 대경지(" + ArtifactService.MajorName(ArtifactService.PlayerMajor()) + ")로 해금하고 기술을 모두 익힙니다(이미 해금한 요수는 단계를 바꾸지 않습니다). 게임의 호천안 돌파 진행(요수 격파 수)도 함께 오릅니다. \"정말 모두\"를 한 번 더 누르세요.",
                new Action(() => Say(GodEyeService.UnlockAll(GodEyeService.Bosses()))));
            y += 46;
            SearchBox.Build(content, "GodEyeSearch", 0, y, 330, eyeQuery, "요수 또는 기술 이름의 일부", new Action<string>(v =>
            {
                eyeQuery = v;
                eyeList.SetItems(Lines(GodEyeService.Find(eyeQuery)));
            }));
            Ui.Wrapped(content, "해금은 지금 대경지로 하고 그 요수의 기술을 모두 익힙니다. 호천안 돌파 진행(요수 격파 수)에도 들어갑니다. 장착은 하나만 됩니다. 전투에서의 사용은 확인하지 못했습니다.", 350, y, ContentW - 350, 40, 15).color = Ui.Dim;
            float top = y + 46;
            eyeList.HeightOf = l => l.Skill == null ? 44 : RowFor(40, Ui.TextHeight(l.Skill.Desc, DescW, 15));
            eyeList.Build(content, 0, top, ContentW, ContentH - top, Lines(GodEyeService.Find(eyeQuery)), DrawEyeLine);
        }

        private void DrawEyeLine(RectTransform row, Line l, int index)
        {
            float bx = ContentW - Ui.ScrollBarWidth - 150;
            float h = row.sizeDelta.y;
            if (l.Skill == null)
            {
                GodEyeService.Boss b = l.Boss;
                bool open = GodEyeService.Unlocked(b);
                int learned = GodEyeService.LearnedCount(b);
                Ui.Label(row, b.Name + "  [" + b.Id + "]", 8, 0, 360, h, 19);
                Ui.Label(row, open ? ArtifactService.MajorName(GodEyeService.Grade(b)) : "-", 380, 0, 120, h, 17);
                Ui.Label(row, "기술 " + learned + "/" + b.Skills.Count, 510, 0, 160, h, 17);
                Text state = Ui.Label(row, open ? "해금" : "잠김", 680, 0, 100, h, 17);
                state.color = open ? Ui.Warn : Ui.Dim;
                state.gameObject.name = "GodEyeState" + b.Id;
                Ui.Button("GodEyeBoss" + b.Id, row, "해금", bx, (h - 36) / 2, 140, 36, new Action(() => Say(GodEyeService.Unlock(b))), !open || learned < b.Skills.Count);
                return;
            }
            GodEyeService.Skill s = l.Skill;
            bool has = GodEyeService.Learned(s.Id);
            bool on = GodEyeService.Equipped == s.Id;
            Ui.Label(row, "└ " + s.Name, 40, 0, 220, h, 17);
            Text desc = Ui.Wrapped(row, s.Desc, 270, 0, DescW, h, 15);
            desc.color = Ui.Dim;
            desc.gameObject.name = "GodEyeDesc" + s.Id;
            Ui.Label(row, on ? "장착 중" : has ? "익힘" : "미익힘", bx - 100, 0, 96, h, 16).color = on ? Ui.Warn : Ui.Dim;
            Ui.Button("GodEyeEquip" + s.Id, row, "장착", bx, (h - 36) / 2, 140, 36, new Action(() => Say(GodEyeService.Equip(s))), has && !on);
        }

        private void DrawDevil()
        {
            float y = SubTop;
            Ui.Label(content, "상태: " + ArtifactService.DevilState(), 0, y, 290, 36, 18).gameObject.name = "DevilStateText";
            Ui.Button("DevilRepair", content, "손상 해제", 300, y, 130, 36, new Action(() => Say(ArtifactService.RepairDevil())), ArtifactService.DevilDamaged);
            Ui.Label(content, "호요 " + PotmonService.Count + " / " + PotmonService.Cap, 450, y, 200, 36, 18).gameObject.name = "DevilCount";
            Ui.Wrapped(content, "승급은 게임 화면에서 합니다(승급 아이템 필요). 호요는 게임 화면의 상한 " + PotmonService.Cap + "까지 넣습니다.", 660, y, ContentW - 660, 40, 15).color = Ui.Dim;
            y += 46;
            Ui.Label(content, "가진 호요", 0, y, 200, 30, 18).color = Ui.Dim;
            y += 32;
            List<PotmonService.Pet> pets = PotmonService.Owned();
            // ListView의 높이에는 아래 이동 줄(38 + 간격 6)이 들어 있다.
            float petH = Math.Min(230, Math.Max(48, pets.Count * 44 + 4)) + 44;
            petList.Build(content, 0, y, ContentW, petH, pets, DrawPet);
            y += petH + 10;
            Ui.Label(content, "호요 추가  단계:", 0, y, 160, 36, 18);
            for (int gr = 1; gr <= PotmonService.MaxGrade; gr++)
            {
                int value = gr;
                WrapLabel(Ui.Button("PotmonGrade" + gr, content, gr + " " + ArtifactService.MajorName(gr), 160 + (gr - 1) * 92, y, 88, 36,
                    new Action(() => { petGrade = value; Redraw(); }), true, petGrade == gr), 15);
            }
            y += 46;
            SearchBox.Build(content, "PotmonSearch", 0, y, 330, petQuery, "호요 이름 또는 종족", new Action<string>(v =>
            {
                petQuery = v;
                kindList.SetItems(PotmonService.Find(petQuery));
            }));
            float top = y + 46;
            kindList.Build(content, 0, top, ContentW, ContentH - top, PotmonService.Find(petQuery), DrawKind);
        }

        private void DrawPet(RectTransform row, PotmonService.Pet p, int index)
        {
            Ui.Label(row, p.Name, 8, 0, 300, 44, 18);
            Ui.Label(row, ArtifactService.MajorName(p.Grade), 320, 0, 120, 44, 17);
            Ui.Label(row, "경험치 " + p.Exp + " / " + (p.Next < 0 ? "최고 단계" : p.Next.ToString()), 450, 0, 320, 44, 17).gameObject.name = "PotmonExpText" + p.SoleId;
            Ui.Button("PotmonExp" + p.SoleId, row, "경험치 채우기", ContentW - Ui.ScrollBarWidth - 180, 4, 170, 36,
                new Action(() => Say(PotmonService.FillExp(p.SoleId))), p.Next >= 0 && p.Exp < p.Next);
        }

        private void DrawKind(RectTransform row, PotmonService.Kind k, int index)
        {
            Ui.Label(row, k.Name + "  [" + k.Id + "]", 8, 0, 400, 44, 18);
            Ui.Label(row, k.Race, 420, 0, 200, 44, 17).color = Ui.Dim;
            Ui.Button("PotmonAdd" + k.Id, row, "추가", ContentW - Ui.ScrollBarWidth - 150, 4, 140, 36,
                new Action(() => Say(PotmonService.Add(k, petGrade))), PotmonService.Count < PotmonService.Cap);
        }
    }
}
