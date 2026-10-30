using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 공법 탭의 하위 화면 넷(13 문서 S2로 나눔): 수련 단계 대도까지 채우기, 전승, 기술 추가, 옵션 교체.
    internal sealed partial class MartialTab
    {
        // ---- 수련 단계 대도까지 채우기(05 문서 83절, 스펙 2026-10-23-mastery-fill-design.md 5절) ----

        private readonly List<int> masteryAdd = new List<int>();
        private bool masteryPick;   // 직접 고르기
        private readonly ListView<ConfBattleSkillPrefixValueItem> masteryCands = new ListView<ConfBattleSkillPrefixValueItem>("MasteryPickList", 40);
        private readonly System.Random masteryRng = new System.Random();

        private void OpenMastery()
        {
            masteryPick = false;
            masteryAdd.Clear();
            masteryAdd.AddRange(MasteryService.Roll(Target, martialId, masteryRng));
            masteryCands.Reset();
            Push(() =>
            {
                DataProps.MartialData martial = MartialService.Martial(Target, martialId);
                return (martial == null ? "공법" : MartialService.Name(martial)) + " — 대도까지 채우기";
            }, DrawMastery);
        }

        // 넣은 뒤 아직 덜 찼으면 무작위 모드는 남은 자리를 다시 뽑아 보여 준다(빈 목록 안내가 틀리지 않게).
        private void RefillAfterApply()
        {
            if (!masteryPick && MasteryService.Read(Target, martialId).Block == null)
            {
                masteryAdd.AddRange(MasteryService.Roll(Target, martialId, masteryRng));
            }
            Redraw();
        }

        private static string StageName(int stage)
        {
            ConfBattleSkillMasteryItem item = g.conf.battleSkillMastery.GetItem(stage);
            return item == null ? stage.ToString() : GameTool.LS(item.name);
        }

        private void DrawMastery()
        {
            float y = SubTop;
            DataProps.MartialData martial = MartialService.Martial(Target, martialId);
            if (martial == null)
            {
                MissingNote("공법을 찾지 못했습니다. 돌아가서 다시 선택하세요.", y);
                return;
            }
            MasteryService.State state = MasteryService.Read(Target, martialId);
            if (state.Block != null)
            {
                masteryAdd.Clear();   // 다 찼거나 더할 수 없으면 남은 목록을 보이지 않는다
            }
            List<ConfBattleSkillPrefixValueItem> items = MartialService.ValidItems(martial);
            Ui.Label(content, "옵션 " + state.Options + "/" + MasteryRules.MaxOptions + "   수련 단계 " + state.Stages + "/12 " + state.LastStage
                + (MartialService.IsEquipped(Target, martialId) ? "   장착 중" : ""), 0, y, ContentW, 32, 18).gameObject.name = "MasteryInfo";
            y += 38;
            Ui.Button("MasteryRoll", content, "무작위 뽑기", 0, y, 130, 34, new Action(() =>
            {
                masteryPick = false;
                masteryAdd.Clear();
                masteryAdd.AddRange(MasteryService.Roll(Target, martialId, masteryRng));
                Redraw();
            }), true, !masteryPick);
            Ui.Button("MasteryPickMode", content, "직접 고르기", 136, y, 130, 34, new Action(() =>
            {
                masteryPick = true;
                masteryAdd.Clear();
                masteryCands.Reset();
                Redraw();
            }), true, masteryPick);
            if (masteryPick)
            {
                Ui.Button("MasteryUndo", content, "마지막 하나 빼기", 290, y, 160, 34, new Action(() =>
                {
                    if (masteryAdd.Count > 0)
                    {
                        masteryAdd.RemoveAt(masteryAdd.Count - 1);
                    }
                    masteryCands.Reset();
                    Redraw();
                }), masteryAdd.Count > 0);
            }
            else
            {
                Ui.Button("MasteryReroll", content, "다시 뽑기", 290, y, 130, 34, new Action(() =>
                {
                    masteryAdd.Clear();
                    masteryAdd.AddRange(MasteryService.Roll(Target, martialId, masteryRng));
                    Redraw();
                }), state.Block == null);
            }
            if (state.Block != null)
            {
                Ui.Label(content, state.Block, 470, y, ContentW - 470, 34, 15).color = Ui.Warn;
            }
            y += 42;
            // 더할 목록: 자리 k의 옵션 = 단계 k+2(1 초학, 2 입문 … 12 대도)
            for (int i = 0; i < masteryAdd.Count; i++)
            {
                int slot = state.Options + i;
                int number = masteryAdd[i];
                ConfBattleSkillPrefixValueItem item = items.Find(it => it.number == number);
                string desc = item == null ? "?" : MartialService.CleanLine(item.desc);
                Text row = Ui.Label(content, "자리 " + (slot + 1) + " · " + StageName(slot + 2) + " · " + number + "번  " + desc, 0, y, ContentW, 26, 14);
                row.gameObject.name = "MasteryRow" + slot;
                y += 27;
            }
            if (masteryAdd.Count == 0)
            {
                Ui.Label(content, masteryPick ? "아래 목록에서 다음 자리의 옵션을 고르세요." : "더할 옵션이 없습니다.", 0, y, ContentW, 26, 14).color = Ui.Dim;
                y += 27;
            }
            float bottom = ContentH - 44;
            if (masteryPick && state.Options + masteryAdd.Count < MasteryRules.MaxOptions)
            {
                int slot = state.Options + masteryAdd.Count;
                Ui.Label(content, "자리 " + (slot + 1) + "(" + StageName(slot + 2) + ")에 넣을 수 있는 옵션", 0, y + 4, 600, 26, 15).color = Ui.Dim;
                float top = y + 32;
                masteryCands.Build(content, 0, top, ContentW, Math.Max(90, bottom - top - 8), MasteryService.Candidates(Target, martialId, masteryAdd), (row, item, index) =>
                {
                    float h = row.sizeDelta.y;
                    int number = item.number;
                    Ui.Label(row, number + "번  " + MartialService.CleanLine(item.desc), 8, 0, ContentW - 170, h, 14);
                    Ui.Button("MasteryPick" + number, row, "이걸로", ContentW - Ui.ScrollBarWidth - 6 - 110, (h - 32) / 2, 110, 32, new Action(() =>
                    {
                        masteryAdd.Add(number);
                        masteryCands.Reset();
                        Redraw();
                    }));
                });
            }
            bool can = state.Block == null && masteryAdd.Count > 0;
            string sole = martialId;
            var add = new List<int>(masteryAdd);
            Confirm.Button("MasteryInsert", content, "끼워 넣기", "정말 끼워 넣기", 0, bottom, 150, 36, "mastery-insert-" + sole,
                "옵션 " + add.Count + "개를 이 공법에 더합니다(장착·숙련·전승 그대로). 한 번 더 누르세요.", new Action(() =>
                {
                    masteryAdd.Clear();
                    Say(MasteryService.Insert(Target, sole, add));
                    RefillAfterApply();
                }), can);
            Confirm.Button("MasteryRemake", content, "다시 만들기", "정말 다시 만들기", 160, bottom, 150, 36, "mastery-remake-" + sole,
                "같은 기술을 옵션 " + add.Count + "개를 더해 새로 만들고 장착·숙련·전승을 옮긴 뒤 옛 것을 지웁니다. 한 번 더 누르세요.", new Action(() =>
                {
                    masteryAdd.Clear();
                    string made;
                    string text = MasteryService.Remake(Target, sole, add, out made);
                    if (made != null)
                    {
                        martialId = made;
                    }
                    Say(text);
                    RefillAfterApply();
                }), can);
            Ui.Wrapped(content, "새 옵션은 최고 품질로 넣습니다. 끼워 넣기는 같은 공법에, 다시 만들기는 새 공법으로(고유 번호가 바뀜). 기술 이름 앞머리가 바뀔 수 있습니다.",
                320, bottom - 4, ContentW - 320, 44, 13).color = Ui.Dim;
        }

        private void OpenSuits()
        {
            suitList.Reset();
            Push(() =>
            {
                DataProps.MartialData martial = MartialService.Martial(Target, martialId);
                return (martial == null ? "심법" : MartialService.Name(martial)) + " — 전승";
            }, DrawSuits);
        }

        // 하위 화면: 이 심법이 구성원인 세트. 붙이기·떼기(MartialService.ApplySuit·RemoveSuit).
        private void DrawSuits()
        {
            float y = SubTop;
            DataUnit.ActionMartialData action = Target.data.unitData.GetActionMartial(martialId);
            if (action == null)
            {
                MissingNote("공법을 찾지 못했습니다. 돌아가서 다시 선택하세요.", y);
                return;
            }
            int suit = MartialService.SuitOf(action.data);
            string sole = martialId;
            Ui.Label(content, "지금 전승: " + (suit == 0 ? "없음" : PresetService.SuitName(suit)), 0, y, 420, 36, 18);
            Ui.Button("SuitRemove", content, "전승 떼기", 430, y, 140, 36, new Action(() => Say(MartialService.RemoveSuit(Target, sole))), suit != 0);
            Ui.Wrapped(content, "세트 효과는 같은 세트 번호를 가진 심법을 정해진 수만큼 장착하면 켜집니다. 조건이 붙은 세트는 게임이 따로 판정합니다(미확인).",
                590, y, ContentW - 590, 40, 14).color = Ui.Dim;
            float top = y + 46;
            suitList.Build(content, 0, top, ContentW, ContentH - top, MartialService.SuitsFor(Target, sole), DrawSuitRow);
        }

        private void DrawSuitRow(RectTransform row, MartialService.SuitView v, int index)
        {
            float h = row.sizeDelta.y;
            float bx = ContentW - Ui.ScrollBarWidth - 6 - 120;
            var text = new System.Text.StringBuilder();
            text.Append(v.Name).Append("  [").Append(v.Id).Append("]").Append(v.Current ? "  (지금)" : "").Append(v.HasCondition ? "  조건 있음(게임 판정, 미확인)" : "")
                .Append("\n구성원: ").Append(string.Join(", ", v.Members.ToArray()));
            if (v.MembersHeld.Count > 0)
            {
                text.Append("   가진 것: ").Append(string.Join(", ", v.MembersHeld.ToArray()));
            }
            string effects = (v.Desc1.Length > 0 ? (v.Claim1 > 0 ? v.Claim1 + "개: " : "") + v.Desc1 : "")
                + (v.Desc2.Length > 0 ? "   " + (v.Claim2 > 0 ? v.Claim2 + "개: " : "") + v.Desc2 : "");
            if (effects.Length > 0)
            {
                text.Append('\n').Append(effects);
            }
            Text label = Ui.Wrapped(row, text.ToString(), 8, 2, bx - 16, h - 4, 14);
            label.gameObject.name = "SuitRow" + v.Id;
            label.color = v.Current ? Ui.Warn : Color.white;
            int id = v.Id;
            string sole = martialId;
            Ui.Button("SuitPick" + id, row, "이 세트로", bx, (h - 36) / 2, 120, 36, new Action(() => Say(MartialService.ApplySuit(Target, sole, id))), !v.Current);
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
                skillList.SetItems(SkillCatalog.Listed(skillKind, skillQuery));
            }));

            // 만들 기술의 경지. 처음에는 편집 대상의 현재 대경지.
            if (skillGrade <= 0)
            {
                skillGrade = Math.Max(1, FateService.CurrentGrade(target));
            }
            Ui.Label(content, "경지", 0, y + 46, 60, 36, 18).color = Ui.Dim;
            NumberChips.Draw(content, "SkillGrade", 1, 10, 60, y + 46, 46, 50, 36, skillGrade, g => { skillGrade = g; Redraw(); });
            // 수십 개를 한 번에 넣고 하나씩 망각해야 되돌린다. 두 번 눌러 확인한다(리뷰 U2).
            Confirm.Button("SkillLearnAll", content, SkillCatalog.Kinds[skillKind].Tab + " 모두 배우기", "정말 모두 배우기", 580, y + 46, 240, 36, "skill-learn-all",
                SkillCatalog.Kinds[skillKind].Tab + "의 배울 수 있는 기술을 모두 넣습니다(하나씩 망각해야 되돌립니다). 한 번 더 누르면 실행합니다.",
                new Action(() => Say(SkillCatalog.LearnAll(Target, skillKind, skillGrade))));
            Ui.Wrapped(content, "선택한 경지에서 배울 수 있고 아직 익히지 않은 것만 넣습니다. 옵션은 게임의 뽑기로 정해집니다.", 834, y + 46, 470, 36, 13).color = Ui.Dim;

            learnedCounts = SkillCatalog.LearnedCounts(target, skillKind);
            float top = y + 92;
            skillList.HeightOf = entry => RowWithSecondLine(50, Ui.TextHeight(entry.Desc, 1050, 15));
            skillList.Build(content, 0, top, ContentW, ContentH - top - 78, SkillCatalog.Listed(skillKind, skillQuery), DrawSkillRow);
            skillDetail.Build(content, 0, ContentH - 70, ContentW, 70);
        }

        private static string OptionText(MartialService.PrefixView view)
        {
            return (view.Index + 1) + ". " + view.Desc;
        }

        // 옵션 버튼의 높이: 글이 버튼 안에서 줄바꿈된 높이. 한 줄이면 32.
        private static float OptionHeight(MartialService.PrefixView view)
        {
            return Math.Max(32, (float)Math.Ceiling(Ui.TextHeight(OptionText(view), 520, 15)) + 6);
        }

        private Dictionary<int, int> learnedCounts = new Dictionary<int, int>();

        private void DrawSkillRow(RectTransform row, SkillCatalog.Entry entry, int index)
        {
            string blocked = SkillCatalog.BlockReason(entry, skillGrade);
            int have;
            learnedCounts.TryGetValue(entry.BaseId, out have);
            Ui.Label(row, (entry.IsMod ? "[모드] " : "") + entry.Name + "  [" + entry.BaseId + "]  경지 " + entry.GradeMin + "~" + entry.GradeMax, 8, 0, 800, 26, 18);
            Text state = Ui.Label(row, have > 0 ? "배움" + (have > 1 ? " x" + have : "") : entry.IsMod ? "비급으로" : blocked != null ? "배울 수 없음" : "-", 820, 0, 200, 26, 16);
            state.color = have > 0 ? Ui.Warn : Ui.Dim;
            Text desc = Ui.Wrapped(row, entry.Desc, 24, SecondLineY, 1050, row.sizeDelta.y - SecondLineY - 4, 15, TextAnchor.UpperLeft);
            desc.color = Ui.Dim;
            desc.gameObject.name = "SkillDesc" + entry.BaseId;
            float x = ContentW - Ui.ScrollBarWidth - 178;
            // 모드 공법은 여기서 배울 수 없다(Learn이 늘 거부한다. 05 문서 42절). 버튼을 끄고 길을 알린다(리뷰 08의 22번).
            if (entry.IsMod)
            {
                Ui.Button("SkillAdd" + entry.BaseId, row, "자원 탭 기술 서적", x, 7, 170, 36, null, false);
                return;
            }
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
                MissingNote("공법을 찾지 못했습니다. 돌아가서 다시 선택하세요.", SubTop);
                return;
            }
            pickerMartial = martial;
            List<ConfBattleSkillPrefixValueItem> items = MartialService.ValidItems(martial);
            Ui.Label(content, "붙을 수 있는 옵션 " + items.Count + "개. 교체하면 품질은 이전 값을 유지합니다(새 옵션의 한도 안에서).", 0, SubTop, 900, 30, 16).color = Ui.Dim;
            float top = SubTop + 36;
            // 글 아래에 "불가" 사유 한 줄의 자리를 늘 남긴다(사유를 줄마다 미리 묻지 않으려고).
            prefixList.HeightOf = item => Math.Max(50, (float)Math.Ceiling(Ui.TextHeight(PrefixText(item), 1060, 17)) + 30);
            prefixList.Build(content, 0, top, ContentW, ContentH - top - 78, items, DrawPrefixRow);
            prefixDetail.Build(content, 0, ContentH - 70, ContentW, 70);
        }

        private DataProps.MartialData pickerMartial;

        private static string PrefixText(ConfBattleSkillPrefixValueItem item)
        {
            return item.number + "번  " + MartialService.CleanDesc(GameTool.LS(item.desc));
        }

        private void DrawPrefixRow(RectTransform row, ConfBattleSkillPrefixValueItem item, int index)
        {
            string blocked = MartialService.ReplaceBlockReason(pickerMartial, replaceIndex, item);
            string text = PrefixText(item);
            float textH = (float)Math.Ceiling(Ui.TextHeight(text, 1060, 17));
            Ui.Wrapped(row, text, 8, 3, 1060, textH, 17, TextAnchor.UpperLeft).gameObject.name = "PrefixDesc" + item.number;
            int slot = replaceIndex;
            float bx = ContentW - Ui.ScrollBarWidth - 118;
            // 자리만 막힌 옵션은 두 번 누르기로 강제로 넣는다(스펙 2026-10-20-trainer-batch2-design.md 5.1절). 중복·조건 위반은 그대로 막는다.
            if (MartialService.SlotBlocked(item, slot) && MartialService.RuleBlockReason(pickerMartial, slot, item) == null)
            {
                Ui.Label(row, "자리 밖: 이 자리에는 나오지 않는 옵션(강제로 넣을 수 있음)", 38, textH + 3, 1050, 22, 15).color = Ui.Warn;
                Confirm.Button("ReplaceForce" + item.number, row, "강제", "정말 강제", bx, 7, 110, 36, "prefix-force-" + item.number,
                    "옵션 " + item.number + "번은 이 자리에 나오지 않는 옵션입니다. 게임이 원래 만들지 않는 조합이라 효과가 나지 않을 수 있습니다. 한 번 더 누르면 넣습니다.",
                    new Action(() =>
                    {
                        string forced = MartialService.ReplacePrefix(Target, martialId, slot, item, true);
                        CloseViews();
                        Say(forced);
                    }));
                return;
            }
            if (blocked != null)
            {
                Ui.Label(row, "불가: " + blocked, 38, textH + 3, 1050, 22, 15).color = Ui.Warn;
            }
            Ui.Button("ReplacePick" + item.number, row, "이걸로", bx, 7, 110, 36, new Action(() =>
            {
                string result = MartialService.ReplacePrefix(Target, martialId, slot, item);
                CloseViews();
                Say(result);
            }), blocked == null);
        }
    }
}
