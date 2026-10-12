using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 편집기 창. 화면만 그리고, 게임 값의 읽기·쓰기는 EditService에 맡긴다.
    internal static class EditorWindow
    {
        private const float Width = 1120;
        private const float Height = 810;
        private const float ContentX = 180;
        private const float ContentY = 104;
        private const float ContentW = 924;
        private const int AttributeRows = 12;
        private const int NpcRows = 8;
        private const int HistoryRows = 14;

        private static readonly string[] Tabs = { "능력치", "자원", "경지", "공법", "NPC", "안전" };
        private static readonly int[] Steps = { 1, 10, 100, 1000, 10000 };

        private static GameObject root;
        private static RectTransform window;
        private static RectTransform content;
        private static Text targetLabel;
        private static Text messageLabel;
        private static RectTransform tabBar;

        private static int tab;
        private static int stepIndex = 1;
        private static int attributePage;
        private static int npcPage;
        private static string targetId;    // 능력치 편집 대상. null이면 플레이어
        private static string selectedNpcId;
        private static string itemIdText = "";
        private static int selectedAttribute = -1;
        private static string attributeInput = "";
        private static string npcQuery = "";
        private static string martialId;      // 공법 탭에서 선택한 공법
        private static int martialPage;
        private static int replaceIndex = -1; // 옵션 교체 중인 자리. -1이면 교체 화면이 아니다
        private static int replacePage;
        private static string appliedNpcQuery = "";
        private static string message = "";
        private static List<WorldUnitBase> npcCache;

        public static bool IsOpen
        {
            get { return root != null && root.activeSelf; }
        }

        public static void Toggle()
        {
            if (IsOpen)
            {
                root.SetActive(false);
                return;
            }
            if (root == null)
            {
                Build();
            }
            root.SetActive(true);
            npcCache = null;
            Render();
        }

        public static void Close()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private static int Step
        {
            get { return Steps[stepIndex]; }
        }

        private static void Build()
        {
            root = new GameObject("TlToyboxEditor");
            UnityEngine.Object.DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();

            window = Ui.Panel("Window", root.transform, 60, 60, Width, Height, Ui.Background);
            Ui.Label(window, "TlToybox 편집기 v0.1", 20, 10, 400, 40, 26);
            Ui.Button("Close", window, "닫기", Width - 100, 12, 84, 36, new Action(Close));
            targetLabel = Ui.Label(window, "", 20, 54, 700, 30, 20);
            messageLabel = Ui.Label(window, "", 20, Height - 44, Width - 40, 32, 19);
            messageLabel.color = Ui.Warn;
            tabBar = Ui.Rect("Tabs", window, 16, ContentY, 150, 400);
            content = Ui.Rect("Content", window, ContentX, ContentY, ContentW, Height - ContentY - 56);
        }

        private static WorldUnitBase Player
        {
            get { return g.world.playerUnit; }
        }

        private static WorldUnitBase Target
        {
            get
            {
                if (targetId != null)
                {
                    WorldUnitBase unit = g.world.unit.GetUnit(targetId, true);
                    if (unit != null)
                    {
                        return unit;
                    }
                    targetId = null;
                }
                return Player;
            }
        }

        private static void Say(string text)
        {
            message = text;
            Render();
        }

        private static void Render()
        {
            Ui.Clear(tabBar);
            Ui.Clear(content);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int index = i;
                Ui.Button("Tab" + i, tabBar, Tabs[i], 0, i * 52, 150, 44, new Action(() => { tab = index; message = ""; Render(); }), true, tab == i);
            }
            messageLabel.text = message;
            if (!EditService.WorldReady)
            {
                targetLabel.text = "";
                Ui.Label(content, "세이브에 진입한 뒤에 사용할 수 있습니다.", 0, 0, 800, 40, 22);
                return;
            }
            WorldUnitBase target = Target;
            targetLabel.text = "편집 대상: " + EditService.NameOf(target) + (targetId == null ? " (플레이어)" : " (NPC)")
                + "    " + EditService.GradeName(target.data.unitData.propertyData.gradeID);
            switch (tab)
            {
                case 0: RenderAttributes(target); break;
                case 1: RenderResources(); break;
                case 2: RenderGrade(); break;
                case 3: RenderMartial(target); break;
                case 4: RenderNpcs(); break;
                default: RenderSafety(); break;
            }
        }

        private static void StepButton(float x, float y)
        {
            Ui.Button("Step", content, "단위: " + Step, x, y, 150, 40, new Action(() => { stepIndex = (stepIndex + 1) % Steps.Length; Render(); }));
        }

        private static void RenderAttributes(WorldUnitBase target)
        {
            var saved = target.data.unitData.propertyData;
            var dyn = target.data.dynUnitData;
            ConfRoleAttributeLimit limit = g.conf.roleAttributeLimit;
            List<AttributeDef> all = AttributeCatalog.All;
            int pages = (all.Count + AttributeRows - 1) / AttributeRows;
            attributePage = Mathf.Clamp(attributePage, 0, pages - 1);

            Text head = Ui.Label(content, "항목", 0, 0, 150, 30, 18);
            head.color = Ui.Dim;
            Ui.Label(content, "저장값", 170, 0, 110, 30, 18).color = Ui.Dim;
            Ui.Label(content, "적용값(보정 포함)", 290, 0, 170, 30, 18).color = Ui.Dim;
            Ui.Label(content, "허용 범위", 470, 0, 220, 30, 18).color = Ui.Dim;

            for (int row = 0; row < AttributeRows; row++)
            {
                int index = attributePage * AttributeRows + row;
                if (index >= all.Count)
                {
                    break;
                }
                AttributeDef def = all[index];
                float y = 34 + row * 40;
                int value = def.Get(saved);
                int min = def.Min(limit);
                int max = def.Max(limit);
                Ui.Label(content, def.Label, 0, y, 160, 36);
                Ui.Button("Select" + index, content, value.ToString(), 166, y, 104, 34, new Action(() => { selectedAttribute = index; attributeInput = ""; message = ""; Render(); }), true, index == selectedAttribute);
                Ui.Label(content, def.Dyn(dyn).value.ToString(), 290, y, 170, 36);
                Ui.Label(content, min + " ~ " + max + (def.OwnLimit ? " (자체 상한)" : ""), 470, y, 230, 36, 18).color = Ui.Dim;
                Ui.Button("Minus" + index, content, "-", 700, y, 56, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Get(Target.data.unitData.propertyData) - Step))));
                Ui.Button("Plus" + index, content, "+", 762, y, 56, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Get(Target.data.unitData.propertyData) + Step))));
                Ui.Button("Max" + index, content, "최대", 824, y, 84, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Max(g.conf.roleAttributeLimit)))));
            }

            float footer = 34 + AttributeRows * 40 + 12;
            Ui.Button("Prev", content, "이전", 0, footer, 90, 40, new Action(() => { attributePage--; Render(); }), attributePage > 0);
            Ui.Label(content, (attributePage + 1) + " / " + pages, 100, footer, 80, 40, 20, TextAnchor.MiddleCenter);
            Ui.Button("Next", content, "다음", 190, footer, 90, 40, new Action(() => { attributePage++; Render(); }), attributePage < pages - 1);
            StepButton(300, footer);
            Ui.Button("Restore", content, "자원 회복", 470, footer, 140, 40, new Action(() => Say(EditService.RestoreResources(Target))));
            Ui.Button("ToPlayer", content, "대상: 플레이어로", 630, footer, 200, 40, new Action(() => { targetId = null; martialId = null; replaceIndex = -1; Say("편집 대상을 플레이어로 바꿨습니다."); }), targetId != null);
            RenderAttributeInput(footer + 52);
        }

        // 능력치 탭 아래쪽의 직접 입력 줄. 저장값 칸을 눌러 항목을 고른 뒤 숫자를 입력해 적용한다.
        private static void RenderAttributeInput(float y)
        {
            List<AttributeDef> all = AttributeCatalog.All;
            if (selectedAttribute < 0 || selectedAttribute >= all.Count)
            {
                Ui.Label(content, "값을 직접 입력하려면 위에서 저장값 칸을 누르세요.", 0, y, 800, 40, 18).color = Ui.Dim;
                return;
            }
            AttributeDef def = all[selectedAttribute];
            Ui.Label(content, "직접 입력 — " + def.Label, 0, y, 250, 40);
            Ui.Input("AttributeInput", content, attributeInput, "숫자 입력", 260, y, 220, 40, new Action<string>(v => attributeInput = v), true);
            Ui.Button("AttributeApply", content, "적용", 492, y, 100, 40, new Action(() =>
            {
                int value;
                if (!int.TryParse(attributeInput, out value))
                {
                    Say(def.Label + ": 숫자를 입력하세요.");
                    return;
                }
                Say(EditService.SetAttribute(Target, def, value));
            }));
        }

        private static void RenderNpcSearch()
        {
            Ui.Label(content, "이름 검색", 0, 110, 110, 40);
            Ui.Input("NpcQuery", content, npcQuery, "이름의 일부", 110, 110, 280, 40, new Action<string>(v => npcQuery = v));
            Ui.Button("NpcSearch", content, "검색", 400, 110, 90, 40, new Action(() => { appliedNpcQuery = npcQuery.Trim(); npcCache = null; npcPage = 0; message = ""; Render(); }));
            Ui.Button("NpcSearchClear", content, "지움", 500, 110, 90, 40, new Action(() => { npcQuery = ""; appliedNpcQuery = ""; npcCache = null; npcPage = 0; message = ""; Render(); }), appliedNpcQuery.Length > 0 || npcQuery.Length > 0);
            if (appliedNpcQuery.Length > 0)
            {
                Ui.Label(content, "'" + appliedNpcQuery + "' 포함", 604, 110, 300, 40, 18).color = Ui.Warn;
            }
        }

#if DEVTOOLS
        // 개발 전용: 자동 시험에서 입력 칸에 글자를 넣는다. 게임 창이 뒤에 있으면 실제 키 입력이 입력 칸에 전달되지 않기 때문이다.
        public static string DevSetInput(string name, string value)
        {
            if (content == null)
            {
                return "window not built";
            }
            Transform field = content.Find(name);
            if (field == null)
            {
                return "no input named " + name;
            }
            field.gameObject.GetComponent<InputField>().text = value;
            return name + " = '" + value + "' (attributeInput='" + attributeInput + "' npcQuery='" + npcQuery + "')";
        }
#endif

        private static void RenderResources()
        {
            WorldUnitBase player = Player;
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, 70, Ui.Section);
            Ui.Label(content, "영석: " + EditService.Money(player), 16, 15, 300, 40, 22);
            Ui.Button("MoneyAdd", content, "+ " + Step, 330, 15, 140, 40, new Action(() => Say(EditService.AddMoney(Player, Step))));
            StepButton(490, 15);

            Ui.Panel("ItemBox", content, 0, 90, ContentW, 330, Ui.Section);
            int itemId;
            bool hasId = int.TryParse(itemIdText, out itemId);
            string name = hasId ? EditService.ItemName(itemId) : null;
            Ui.Label(content, "아이템 지급", 16, 100, 300, 36, 22);
            Ui.Label(content, "아이템 ID: " + (itemIdText.Length == 0 ? "(아래 숫자판으로 입력)" : itemIdText), 16, 144, 500, 36);
            Text nameLabel = Ui.Label(content, !hasId ? "" : name == null ? "게임에 없는 아이템 ID입니다." : "이름: " + name, 16, 184, 600, 36);
            nameLabel.color = name == null ? Ui.Warn : Color.white;
            for (int digit = 0; digit <= 9; digit++)
            {
                string d = digit.ToString();
                Ui.Button("Digit" + d, content, d, 16 + digit * 62, 232, 56, 48, new Action(() => { if (itemIdText.Length < 9) { itemIdText += d; } Render(); }));
            }
            Ui.Button("Back", content, "한 글자 지움", 16, 292, 160, 44, new Action(() => { if (itemIdText.Length > 0) { itemIdText = itemIdText.Substring(0, itemIdText.Length - 1); } Render(); }));
            Ui.Button("ClearId", content, "모두 지움", 190, 292, 140, 44, new Action(() => { itemIdText = ""; Render(); }));
            Ui.Button("Give", content, "지급 x" + Step, 16, 352, 180, 48, new Action(() => Say(EditService.GiveItem(Player, int.Parse(itemIdText), Step))), name != null);
            Ui.Label(content, "수량은 위의 '단위' 값을 씁니다. 지급 대상은 플레이어입니다.", 210, 356, 680, 40, 18).color = Ui.Dim;
        }

        private static void RenderGrade()
        {
            WorldUnitBase player = Player;
            var saved = player.data.unitData.propertyData;
            ConfRoleGradeItem next = g.conf.roleGrade.GetNextGradeItem(saved.gradeID);
            Ui.Panel("GradeBox", content, 0, 0, ContentW, 250, Ui.Section);
            Ui.Label(content, "현재 경지: " + EditService.GradeName(saved.gradeID), 16, 12, 800, 36, 22);
            Ui.Label(content, "다음 단계: " + (next == null ? "없음" : EditService.GradeName(next.id)), 16, 52, 800, 36);
            Ui.Label(content, "수련치: " + saved.exp + (next == null ? "" : " / " + next.exp), 16, 92, 800, 36);
            string blocked = EditService.BreakthroughBlockReason(player);
            Ui.Button("FillExp", content, "수련치 채우기", 16, 144, 200, 48, new Action(() => Say(EditService.FillExp(Player))), next != null && saved.exp < next.exp);
            Ui.Button("Breakthrough", content, "다음 단계로 돌파", 232, 144, 230, 48, new Action(() => Say(EditService.Breakthrough(Player))), blocked == null);
            if (blocked != null)
            {
                Ui.Label(content, blocked, 16, 200, 880, 36, 18).color = Ui.Warn;
            }
            Ui.Label(content, "돌파는 게임의 정식 돌파 행동을 실행합니다. 능력치 보정이 함께 적용됩니다.", 0, 264, 900, 32, 18).color = Ui.Dim;
            Ui.Label(content, "대상은 플레이어입니다. 대경지 돌파는 이 버전에서 지원하지 않습니다.", 0, 296, 900, 32, 18).color = Ui.Dim;
        }

        private static void RenderMartial(WorldUnitBase target)
        {
            const int listRows = 5;
            const int pickRows = 9;
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
                Ui.Button("PickMartial" + index, content, id == martialId ? "선택됨" : "선택", 720, y, 110, 32, new Action(() => { martialId = id; replaceIndex = -1; message = ""; Render(); }), true, id == martialId);
            }
            float listEnd = 32 + listRows * 38;
            Ui.Button("MartialPrev", content, "이전", 0, listEnd, 80, 34, new Action(() => { martialPage--; Render(); }), martialPage > 0);
            Ui.Label(content, (martialPage + 1) + " / " + pages, 86, listEnd, 80, 34, 18, TextAnchor.MiddleCenter);
            Ui.Button("MartialNext", content, "다음", 172, listEnd, 80, 34, new Action(() => { martialPage++; Render(); }), martialPage < pages - 1);
            Ui.Label(content, all.Count == 0 ? "익힌 공법이 없습니다." : "대상이 익힌 공법 " + all.Count + "개", 270, listEnd, 400, 34, 18).color = Ui.Dim;

            float box = listEnd + 46;
            Ui.Panel("MartialBox", content, 0, box, ContentW, 330, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "위에서 공법을 선택하면 옵션을 편집할 수 있습니다.", 16, box + 10, 880, 36);
                return;
            }
            Ui.Label(content, MartialService.Name(selected) + "  (" + MartialService.TypeLabel(selected.martialType) + ")", 16, box + 6, 420, 36, 21);
            Ui.Button("MartialExp", content, "숙련 경험치 + " + Step, 450, box + 8, 230, 34, new Action(() => Say(MartialService.AddExp(Target, martialId, Step))));
            StepButton(700, box + 6);
            List<MartialService.PrefixView> prefixes = MartialService.Prefixes(selected);
            if (prefixes.Count == 0)
            {
                Ui.Label(content, "이 공법에는 옵션이 없습니다.", 16, box + 52, 880, 34, 18).color = Ui.Dim;
            }
            for (int i = 0; i < prefixes.Count && i < 7; i++)
            {
                MartialService.PrefixView view = prefixes[i];
                int slot = view.Index;
                float y = box + 50 + i * 38;
                Ui.Label(content, (slot + 1) + ". " + Shorten(view.Desc, 34), 16, y, 520, 34, 17);
                Ui.Label(content, "품질 " + view.Level + "/" + view.MaxLevel, 545, y, 110, 34, 18);
                Ui.Button("QualityMinus" + slot, content, "-", 660, y, 44, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.Level - 1))), view.Level > 1);
                Ui.Button("QualityPlus" + slot, content, "+", 708, y, 44, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.Level + 1))), view.Level < view.MaxLevel);
                Ui.Button("QualityMax" + slot, content, "최고", 756, y, 66, 32, new Action(() => Say(MartialService.SetQuality(Target, martialId, slot, view.MaxLevel))), view.Level != view.MaxLevel);
                Ui.Button("Replace" + slot, content, "교체", 826, y, 80, 32, new Action(() => { replaceIndex = slot; replacePage = 0; message = ""; Render(); }));
            }
        }

        // 옵션 교체 화면. 이 공법에 붙을 수 있는 옵션을 모두 보여 주고, 규칙에 걸리는 것은 사유와 함께 막는다.
        private static void RenderPrefixPicker(WorldUnitBase target, DataProps.MartialData martial, int rows)
        {
            List<ConfBattleSkillPrefixValueItem> items = MartialService.ValidItems(martial);
            int pages = Math.Max(1, (items.Count + rows - 1) / rows);
            replacePage = Mathf.Clamp(replacePage, 0, pages - 1);
            Ui.Label(content, MartialService.Name(martial) + " — 옵션 " + (replaceIndex + 1) + " 교체", 0, 0, 640, 36, 21);
            Ui.Button("ReplaceCancel", content, "돌아가기", 780, 0, 130, 36, new Action(() => { replaceIndex = -1; message = ""; Render(); }));
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
            Ui.Button("ReplacePrev", content, "이전", 0, footer, 90, 38, new Action(() => { replacePage--; Render(); }), replacePage > 0);
            Ui.Label(content, (replacePage + 1) + " / " + pages, 100, footer, 80, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("ReplaceNext", content, "다음", 190, footer, 90, 38, new Action(() => { replacePage++; Render(); }), replacePage < pages - 1);
            Ui.Label(content, "붙을 수 있는 옵션 " + items.Count + "개. 교체하면 품질은 이전 값을 유지합니다(새 옵션의 한도 안에서).", 300, footer, 620, 38, 16).color = Ui.Dim;
        }

        private static string Shorten(string text, int max)
        {
            return text.Length <= max ? text : text.Substring(0, max) + "…";
        }

        private static void BuildNpcCache()
        {
            npcCache = new List<WorldUnitBase>();
            WorldUnitBase player = Player;
            string playerId = player.data.unitData.unitID;
            var units = g.world.unit.GetUnits(false);
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].data.unitData.unitID != playerId
                    && (appliedNpcQuery.Length == 0 || EditService.NameOf(units[i]).Contains(EditService.Compact(appliedNpcQuery))))
                {
                    npcCache.Add(units[i]);
                }
            }
            int px = player.data.unitData.pointX;
            int py = player.data.unitData.pointY;
            npcCache.Sort((a, b) => Distance(a, px, py).CompareTo(Distance(b, px, py)));
        }

        private static int Distance(WorldUnitBase unit, int px, int py)
        {
            return Math.Abs(unit.data.unitData.pointX - px) + Math.Abs(unit.data.unitData.pointY - py);
        }

        private static void RenderNpcs()
        {
            if (npcCache == null)
            {
                BuildNpcCache();
            }
            WorldUnitBase player = Player;
            int px = player.data.unitData.pointX;
            int py = player.data.unitData.pointY;
            WorldUnitBase selected = selectedNpcId == null ? null : g.world.unit.GetUnit(selectedNpcId, true);

            Ui.Panel("SelectedBox", content, 0, 0, ContentW, 104, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "아래 목록에서 NPC를 선택하세요. 가까운 순서입니다.", 16, 12, 880, 36);
            }
            else
            {
                Ui.Label(content, "선택: " + EditService.NameOf(selected) + "   " + EditService.GradeName(selected.data.unitData.propertyData.gradeID), 16, 8, 600, 36, 21);
                Ui.Label(content, "호감도  나→상대 " + EditService.IntimTo(player, selected) + "   상대→나 " + EditService.IntimTo(selected, player), 16, 52, 380, 40);
                Ui.Button("IntimMinus", content, "- " + Step, 400, 54, 100, 38, new Action(() => ChangeIntim(-Step)));
                Ui.Button("IntimPlus", content, "+ " + Step, 506, 54, 100, 38, new Action(() => ChangeIntim(Step)));
                Ui.Button("IntimZero", content, "0으로", 612, 54, 90, 38, new Action(() => Say(EditService.SetIntimBoth(Player, g.world.unit.GetUnit(selectedNpcId, true), 0))));
                Ui.Button("EditNpc", content, "능력치 편집", 716, 54, 190, 38, new Action(() => { targetId = selectedNpcId; tab = 0; attributePage = 0; martialId = null; replaceIndex = -1; Say("편집 대상을 NPC로 바꿨습니다."); }));
                StepButton(716, 8);
            }

            int pages = Math.Max(1, (npcCache.Count + NpcRows - 1) / NpcRows);
            npcPage = Mathf.Clamp(npcPage, 0, pages - 1);
            RenderNpcSearch();
            Ui.Label(content, "이름", 0, 156, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "경지", 260, 156, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "거리(칸)", 560, 156, 120, 30, 18).color = Ui.Dim;
            for (int row = 0; row < NpcRows; row++)
            {
                int index = npcPage * NpcRows + row;
                if (index >= npcCache.Count)
                {
                    break;
                }
                WorldUnitBase npc = npcCache[index];
                string id = npc.data.unitData.unitID;
                float y = 188 + row * 40;
                Ui.Label(content, EditService.NameOf(npc), 0, y, 250, 36);
                Ui.Label(content, EditService.GradeName(npc.data.unitData.propertyData.gradeID), 260, y, 290, 36);
                Ui.Label(content, Distance(npc, px, py).ToString(), 560, y, 120, 36);
                Ui.Button("Pick" + index, content, id == selectedNpcId ? "선택됨" : "선택", 700, y, 110, 34, new Action(() => { selectedNpcId = id; message = ""; Render(); }), true, id == selectedNpcId);
            }
            float footer = 188 + NpcRows * 40 + 8;
            Ui.Button("NpcPrev", content, "이전", 0, footer, 90, 40, new Action(() => { npcPage--; Render(); }), npcPage > 0);
            Ui.Label(content, (npcPage + 1) + " / " + pages, 100, footer, 110, 40, 20, TextAnchor.MiddleCenter);
            Ui.Button("NpcNext", content, "다음", 220, footer, 90, 40, new Action(() => { npcPage++; Render(); }), npcPage < pages - 1);
            Ui.Button("NpcNext10", content, "10쪽 뒤로", 320, footer, 130, 40, new Action(() => { npcPage += 10; Render(); }), npcPage < pages - 1);
            Ui.Button("NpcRefresh", content, "목록 새로 고침", 470, footer, 180, 40, new Action(() => { npcCache = null; npcPage = 0; Render(); }));
            Ui.Label(content, "전체 " + npcCache.Count + "명", 670, footer, 200, 40, 18).color = Ui.Dim;
        }

        private static void ChangeIntim(int delta)
        {
            WorldUnitBase npc = g.world.unit.GetUnit(selectedNpcId, true);
            if (npc == null)
            {
                Say("선택한 NPC를 찾지 못했습니다.");
                return;
            }
            Say(EditService.SetIntimBoth(Player, npc, EditService.IntimTo(npc, Player) + delta));
        }

        private static void RenderSafety()
        {
            Ui.Panel("BackupBox", content, 0, 0, ContentW, 104, Ui.Section);
            Ui.Label(content, "세이브 백업: " + (SaveBackup.LastBackupDir == null ? "이번 실행에서 아직 백업하지 않았습니다." : "완료"), 16, 8, 880, 34);
            if (SaveBackup.LastBackupDir != null)
            {
                Ui.Label(content, SaveBackup.LastBackupDir, 16, 40, 880, 28, 15).color = Ui.Dim;
            }
            Ui.Button("BackupNow", content, "지금 백업", 16, 62, 160, 36, new Action(() =>
            {
                string error = SaveBackup.BackupNow();
                Say(error ?? "세이브를 백업했습니다.");
            }));
            Ui.Label(content, "값을 처음 바꾸기 직전에 자동으로 한 번 백업합니다. 백업은 디스크에 저장된 세이브의 사본입니다.", 190, 64, 720, 32, 16).color = Ui.Dim;

            IList<ChangeLog.Entry> entries = ChangeLog.Entries;
            Ui.Label(content, "변경 이력 (이번 실행, 최근 " + HistoryRows + "건 / 전체 " + entries.Count + "건)", 0, 116, 880, 32, 20);
            int shown = Math.Min(HistoryRows, entries.Count);
            for (int i = 0; i < shown; i++)
            {
                ChangeLog.Entry entry = entries[entries.Count - 1 - i];
                Ui.Label(content, entry.ToString(), 0, 152 + i * 30, 900, 28, 17).color = entry.Verified ? Color.white : Ui.Warn;
            }
            if (entries.Count == 0)
            {
                Ui.Label(content, "아직 변경한 것이 없습니다.", 0, 152, 880, 30, 18).color = Ui.Dim;
            }
        }
    }
}
