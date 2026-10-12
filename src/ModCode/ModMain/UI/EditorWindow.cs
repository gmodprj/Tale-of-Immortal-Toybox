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

        private static readonly string[] Tabs = { "능력치", "자원", "경지", "공법", "기운", "NPC", "세계", "안전" };
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
        private static string itemQuery = "";
        private static string appliedItemQuery = "";
        private static int itemPage;
        private static int selectedAttribute = -1;
        private static string attributeInput = "";
        private static string npcQuery = "";
        private static string martialId;      // 공법 탭에서 선택한 공법
        private static int martialPage;
        private static int replaceIndex = -1; // 옵션 교체 중인 자리. -1이면 교체 화면이 아니다
        private static int replacePage;
        private static int luckPage;
        private static bool luckPicking;
        private static int luckType = 1;
        private static int luckPickPage;
        private static string luckQuery = "";
        private static string appliedLuckQuery = "";
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
                case 4: RenderLuck(target); break;
                case 5: RenderNpcs(); break;
                case 6: RenderWorld(); break;
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
                Ui.Label(content, def.DynValue(dyn).ToString(), 290, y, 170, 36);
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
            const int rows = 8;
            WorldUnitBase player = Player;
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, 60, Ui.Section);
            Ui.Label(content, "영석: " + EditService.Money(player), 16, 10, 300, 40, 22);
            Ui.Button("MoneyAdd", content, "+ " + Step, 330, 10, 140, 40, new Action(() => Say(EditService.AddMoney(Player, Step))));
            StepButton(490, 10);
            Ui.Label(content, "'단위'는 영석 추가량과 아이템 지급 수량에 함께 쓰입니다.", 656, 12, 270, 36, 14).color = Ui.Dim;

            // 아이템 지급: 이름으로 찾아서 고른다.
            Ui.Label(content, "아이템 지급", 0, 70, 160, 40, 21);
            Ui.Input("ItemQuery", content, itemQuery, "아이템 이름의 일부", 160, 70, 300, 40, new Action<string>(v => itemQuery = v));
            Ui.Button("ItemSearch", content, "검색", 468, 70, 90, 40, new Action(() => { appliedItemQuery = itemQuery.Trim(); itemPage = 0; message = ""; Render(); }));
            Ui.Button("ItemSearchClear", content, "지움", 564, 70, 90, 40, new Action(() => { itemQuery = ""; appliedItemQuery = ""; itemPage = 0; message = ""; Render(); }), itemQuery.Length > 0 || appliedItemQuery.Length > 0);

            List<ItemCatalog.Entry> found = ItemCatalog.Find(appliedItemQuery);
            int pages = Math.Max(1, (found.Count + rows - 1) / rows);
            itemPage = Mathf.Clamp(itemPage, 0, pages - 1);
            Ui.Label(content, appliedItemQuery.Length > 0 ? "'" + appliedItemQuery + "' 검색 결과 " + found.Count + "개" : "전체 " + found.Count + "개", 664, 70, 260, 40, 16).color = Ui.Dim;
            if (found.Count == 0)
            {
                Ui.Label(content, "그런 이름의 아이템이 없습니다. 이름의 일부만 넣어 보세요.", 0, 122, 880, 36, 18).color = Ui.Warn;
            }
            var props = player.data.unitData.propData;
            for (int row = 0; row < rows; row++)
            {
                int index = itemPage * rows + row;
                if (index >= found.Count)
                {
                    break;
                }
                ItemCatalog.Entry entry = found[index];
                float y = 118 + row * 52;
                Ui.Label(content, entry.Name, 0, y, 330, 28, 19);
                Ui.Label(content, "보유 " + props.GetPropsNum(entry.Id) + "  ·  품질 " + entry.Level, 340, y, 240, 28, 16).color = Ui.Dim;
                Ui.Label(content, Shorten(entry.Desc, 50), 12, y + 26, 760, 22, 14).color = Ui.Dim;
                Ui.Button("ItemGive" + index, content, "지급 x" + Step, 780, y + 6, 130, 38, new Action(() => Say(EditService.GiveItem(Player, entry.Id, Step))));
            }
            float footer = 118 + rows * 52 + 4;
            Ui.Button("ItemPrev", content, "이전", 0, footer, 90, 38, new Action(() => { itemPage--; Render(); }), itemPage > 0);
            Ui.Label(content, (itemPage + 1) + " / " + pages, 96, footer, 110, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("ItemNext", content, "다음", 212, footer, 90, 38, new Action(() => { itemPage++; Render(); }), itemPage < pages - 1);
            Ui.Button("ItemNext10", content, "10쪽 뒤로", 308, footer, 130, 38, new Action(() => { itemPage += 10; Render(); }), itemPage < pages - 1);
            Ui.Label(content, "지급 대상은 플레이어입니다.", 456, footer, 400, 38, 15).color = Ui.Dim;
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
            List<ConfRoleGradeItem> major = EditService.MajorBreakthroughRows(player);
            if (major.Count == 0)
            {
                Ui.Label(content, "돌파는 게임의 정식 돌파 행동을 실행합니다. 능력치 보정이 함께 적용됩니다. 대상은 플레이어입니다.", 0, 264, 900, 32, 18).color = Ui.Dim;
                return;
            }
            // 대경지 돌파: 성공 확률 판정과 실패 시의 천겁은 게임의 절차이므로, 재료만 지급하고 게임의 돌파 화면을 연다.
            Ui.Panel("MajorBox", content, 0, 262, ContentW, 70 + major.Count * 44 + 60, Ui.Section);
            Ui.Label(content, "대경지 돌파 — 게임의 돌파 화면에서 진행합니다", 16, 268, 880, 34, 21);
            Ui.Label(content, "방식마다 필요한 재료와 역천개명 선택 수가 다릅니다. 성공 확률은 넣는 재료에 따라 정해지고, 실패하면 천겁을 치릅니다.", 16, 300, 890, 28, 15).color = Ui.Dim;
            for (int i = 0; i < major.Count; i++)
            {
                ConfRoleGradeItem row = major[i];
                float y = 334 + i * 44;
                string need = row.growItemRequire == null || row.growItemRequire.Length == 0 ? "재료 없음"
                    : (EditService.ItemName(row.growItemRequire[row.growItemRequire.Length - 1]) ?? "?") + " 등 " + row.growItemNum + "개";
                Ui.Label(content, GameTool.LS(row.qualityName) + "  ·  필요 재료 " + need + "  ·  역천개명 " + row.fateNum + "개", 16, y, 660, 38, 18);
                Ui.Button("MajorGive" + i, content, "재료 지급", 700, y + 2, 200, 36, new Action(() => Say(EditService.GiveBreakthroughMaterials(Player, row))),
                    row.growItemRequire != null && row.growItemRequire.Length > 0 && row.growItemNum > 0);
            }
            float open = 334 + major.Count * 44 + 6;
            Ui.Button("MajorOpen", content, "게임의 돌파 화면 열기", 16, open, 300, 44, new Action(() =>
            {
                string error = EditService.OpenGameBreakthroughScreen(Player);
                if (error != null)
                {
                    Say(error);
                    return;
                }
                Close();
            }));
            Ui.Label(content, "누르면 편집기를 닫고 게임의 돌파 화면을 엽니다.", 330, open + 4, 560, 36, 16).color = Ui.Dim;
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

        private static void RenderLuck(WorldUnitBase target)
        {
            const int rows = 10;
            if (luckPicking)
            {
                RenderLuckPicker(target, 8);
                return;
            }
            List<LuckService.LuckView> owned = LuckService.Owned(target).FindAll(v => !v.Hidden);
            int pages = Math.Max(1, (owned.Count + rows - 1) / rows);
            luckPage = Mathf.Clamp(luckPage, 0, pages - 1);
            Ui.Label(content, "기운", 0, 0, 200, 30, 18).color = Ui.Dim;
            Ui.Label(content, "종류", 210, 0, 100, 30, 18).color = Ui.Dim;
            Ui.Label(content, "효과", 320, 0, 400, 30, 18).color = Ui.Dim;
            for (int row = 0; row < rows; row++)
            {
                int index = luckPage * rows + row;
                if (index >= owned.Count)
                {
                    break;
                }
                LuckService.LuckView view = owned[index];
                float y = 32 + row * 40;
                Ui.Label(content, Shorten(view.Name, 9), 0, y, 205, 36);
                Ui.Label(content, view.Born ? "선천" : LuckService.TypeLabel(view.Type), 210, y, 100, 36, 18);
                Ui.Label(content, Shorten(view.Tips, 34), 320, y, 490, 36, 16);
                Ui.Button("LuckRemove" + index, content, "삭제", 822, y + 1, 84, 34, new Action(() => Say(LuckService.Remove(Target, view.Id))), LuckService.AddedByEditor(target, view.Id));
            }
            float footer = 32 + rows * 40 + 8;
            Ui.Button("LuckPrev", content, "이전", 0, footer, 90, 40, new Action(() => { luckPage--; Render(); }), luckPage > 0);
            Ui.Label(content, (luckPage + 1) + " / " + pages, 100, footer, 80, 40, 18, TextAnchor.MiddleCenter);
            Ui.Button("LuckNext", content, "다음", 190, footer, 90, 40, new Action(() => { luckPage++; Render(); }), luckPage < pages - 1);
            Ui.Button("LuckAddOpen", content, "기운 추가", 300, footer, 160, 40, new Action(() => { luckPicking = true; luckPickPage = 0; message = ""; Render(); }));
            Ui.Label(content, "편집기로 추가한 기운만 지울 수 있습니다. 숨김 기운은 표시하지 않습니다.", 480, footer, 440, 40, 15).color = Ui.Dim;
        }

        private static void RenderLuckPicker(WorldUnitBase target, int rows)
        {
            Ui.Label(content, "기운 추가 — " + EditService.NameOf(target), 0, 0, 400, 36, 21);
            Ui.Button("LuckBack", content, "돌아가기", 780, 0, 130, 36, new Action(() => { luckPicking = false; message = ""; Render(); }));
            // 후천 분류에는 게임의 상태 표식 기운이 섞여 있어 후보에서 뺀다.
            int[] types = { 1, 3 };
            for (int i = 0; i < types.Length; i++)
            {
                int type = types[i];
                Ui.Button("LuckType" + type, content, LuckService.TypeLabel(type), i * 126, 44, 120, 38, new Action(() => { luckType = type; luckPickPage = 0; Render(); }), true, luckType == type);
            }
            Ui.Input("LuckQuery", content, luckQuery, "이름·효과의 일부", 396, 44, 250, 38, new Action<string>(v => luckQuery = v));
            Ui.Button("LuckSearch", content, "검색", 654, 44, 80, 38, new Action(() => { appliedLuckQuery = luckQuery.Trim(); luckPickPage = 0; Render(); }));
            Ui.Button("LuckSearchClear", content, "지움", 740, 44, 80, 38, new Action(() => { luckQuery = ""; appliedLuckQuery = ""; luckPickPage = 0; Render(); }), luckQuery.Length > 0 || appliedLuckQuery.Length > 0);

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
                Ui.Label(content, Shorten(view.Name, 12) + "  (품질 " + view.Level + ")", 0, y, 780, 26, 18);
                Ui.Label(content, Shorten(view.Tips, 52), 16, y + 24, 770, 22, 15).color = Ui.Dim;
                Ui.Button("LuckAdd" + index, content, has ? "보유 중" : "추가", 800, y + 6, 110, 36, new Action(() => Say(LuckService.Add(Target, view.Id))), !has);
            }
            float footer = 92 + rows * 50 + 4;
            Ui.Button("LuckPickPrev", content, "이전", 0, footer, 90, 38, new Action(() => { luckPickPage--; Render(); }), luckPickPage > 0);
            Ui.Label(content, (luckPickPage + 1) + " / " + pages, 100, footer, 90, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("LuckPickNext", content, "다음", 200, footer, 90, 38, new Action(() => { luckPickPage++; Render(); }), luckPickPage < pages - 1);
            Ui.Label(content, "후보 " + candidates.Count + "개" + (appliedLuckQuery.Length > 0 ? "  ('" + appliedLuckQuery + "' 포함)" : ""), 310, footer, 500, 38, 16).color = Ui.Dim;
        }

        private static void RenderWorld()
        {
            Vector2Int here = WorldService.PlayerPoint;
            Ui.Panel("MoveBox", content, 0, 0, ContentW, 250, Ui.Section);
            Ui.Label(content, "순간 이동", 16, 6, 300, 36, 21);
            Ui.Label(content, "현재 위치: (" + here.x + ", " + here.y + ")", 16, 44, 400, 34);
            StepButton(700, 8);
            // 방향 버튼: 단위 값만큼의 칸을 이동한다. 대지도는 y가 커질수록 아래쪽이다(실측 전: 방향은 버튼 이름 기준).
            Ui.Button("MoveUp", content, "y - " + Step, 150, 88, 120, 40, new Action(() => Say(WorldService.TeleportBy(0, -Step))));
            Ui.Button("MoveLeft", content, "x - " + Step, 16, 136, 120, 40, new Action(() => Say(WorldService.TeleportBy(-Step, 0))));
            Ui.Button("MoveRight", content, "x + " + Step, 284, 136, 120, 40, new Action(() => Say(WorldService.TeleportBy(Step, 0))));
            Ui.Button("MoveDown", content, "y + " + Step, 150, 184, 120, 40, new Action(() => Say(WorldService.TeleportBy(0, Step))));
            WorldUnitBase npc = selectedNpcId == null ? null : g.world.unit.GetUnit(selectedNpcId, true);
            Ui.Button("MoveToNpc", content, npc == null ? "선택한 NPC에게 이동" : EditService.NameOf(npc) + "에게 이동", 450, 136, 300, 40,
                new Action(() => Say(WorldService.TeleportToUnit(g.world.unit.GetUnit(selectedNpcId, true)))), npc != null);
            Ui.Label(content, npc == null ? "NPC 탭에서 NPC를 선택하면 그 위치로 갈 수 있습니다." : "", 450, 184, 460, 34, 15).color = Ui.Dim;

            Ui.Panel("TimeBox", content, 0, 262, ContentW, 110, Ui.Section);
            Ui.Label(content, "날짜", 16, 268, 200, 36, 21);
            Ui.Label(content, "누적 일수: " + WorldService.WorldDay, 16, 306, 300, 34);
            Ui.Button("Day1", content, "+ 1일", 330, 306, 110, 40, new Action(() => Say(WorldService.AddDays(1))));
            Ui.Button("Day5", content, "+ 5일", 450, 306, 110, 40, new Action(() => Say(WorldService.AddDays(5))));
            Ui.Label(content, "달이 넘어가면 게임의 월말 처리가 실행됩니다.", 580, 308, 340, 36, 15).color = Ui.Dim;

            Ui.Panel("BattleBox", content, 0, 384, ContentW, 150, Ui.Section);
            Ui.Label(content, "전투 보조", 16, 390, 200, 36, 21);
            Ui.Button("God", content, "무적: " + (BattleService.God ? "켜짐" : "꺼짐"), 16, 432, 200, 44, new Action(() => Say(BattleService.SetGod(!BattleService.God))), true, BattleService.God);
            Ui.Button("Multiplier", content, "피해 배율: x" + BattleService.Multiplier, 232, 432, 230, 44, new Action(() => Say(BattleService.NextMultiplier())), true, BattleService.Multiplier > 1);
            Ui.Label(content, "무적은 플레이어가 받는 피해를 0으로, 배율은 플레이어가 주는 피해를 곱합니다. 동료와 적끼리의 피해는 그대로입니다.", 16, 482, 890, 24, 15).color = Ui.Dim;
            Ui.Label(content, "게임을 다시 켜면 둘 다 꺼진 상태로 시작합니다.", 16, 504, 890, 24, 15).color = Ui.Dim;
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
                Ui.Button("EditNpc", content, "능력치 편집", 716, 54, 190, 38, new Action(() => { targetId = selectedNpcId; tab = 0; attributePage = 0; martialId = null; replaceIndex = -1; luckPicking = false; luckPage = 0; Say("편집 대상을 NPC로 바꿨습니다."); }));
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
