using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 편집기 창. 화면만 그리고, 게임 값의 읽기·쓰기는 EditService에 맡긴다.
    internal static class EditorWindow
    {
        private const float Width = 1500;
        private const float Height = 1000;
        private const float ContentX = 180;
        private const float ContentY = 104;
        private const float ContentW = 1304;
        private const int NpcRows = 8;
        private const int HistoryRows = 14;

        private static readonly string[] Tabs = { "능력치", "자원", "경지", "공법", "운명", "기예", "NPC", "세계", "안전" };
        private static readonly int[] Amounts = { 1, 10, 100, 1000 };
        private const int MaxAmount = 999999;

        private static GameObject root;
        private static RectTransform window;
        private static RectTransform content;
        private static Text targetLabel;
        private static Text messageLabel;
        private static RectTransform tabBar;

        private static int tab;
        private static int step = 10;        // 능력치 증감 등에 쓰는 단위
        private static int moneyAmount = 10; // 영석 추가량
        private static int itemAmount = 1;   // 아이템 지급 수량
        private static readonly Dictionary<string, string> amountInputs = new Dictionary<string, string>();
        private static int attributeCategory;
        private static int npcPage;
        private static string targetId;    // 능력치 편집 대상. null이면 플레이어
        private static string selectedNpcId;
        private static string itemQuery = "";
        private static string appliedItemQuery = "";
        private static int itemPage;
        private static int itemPageSizeIndex;
        private static float itemScrollY;
        private static float itemSubScrollY;
        private static string itemDetail = "";
        private static Text itemDetailLabel;
        private static int itemMainType = -1;
        private static int itemSubType = -1;
        private static readonly int[] ItemPageSizes = { 10, 20, 50 };
        private static int selectedAttribute = -1;
        private static string attributeInput = "";
        private static string npcQuery = "";
        private static string martialId;      // 공법 탭에서 선택한 공법
        private static int martialPage;
        private static int replaceIndex = -1; // 옵션 교체 중인 자리. -1이면 교체 화면이 아니다
        private static int replacePage;
        private static int luckPage;
        private static int pendingLuckRemove;
        private static int luckReplaceIndex = -1; // 교체할 선천운명 자리. -1이면 후천운명 추가
        private static bool luckPicking;
        private static int artKind;
        private static int artPage;
        private static float artScrollY;
        private static int artPending;      // 잊기 확인 중인 서적 번호
        private static string artQuery = "";
        private static string appliedArtQuery = "";
        private static string martialPending;
        private static bool skillAdding;      // 공법 탭의 기술 추가 화면
        private static int skillKind;
        private static int skillPage;
        private static int skillGrade;
        private static string skillQuery = "";
        private static string appliedSkillQuery = "";  // 망각 확인 중인 공법
        private static bool fateOpen;       // 운명 탭의 역천개명 화면
        private static int fatePickGrade;   // 후보를 고르는 중인 대경지. 0이면 목록 화면
        private static int fatePickPage;
        private static string fateQuery = "";
        private static string appliedFateQuery = "";
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
            get { return step; }
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
                case 5: RenderArt(); break;
                case 6: RenderNpcs(); break;
                case 7: RenderWorld(); break;
                default: RenderSafety(); break;
            }
        }

        // 수량을 고르는 줄: 1/10/100/1000 버튼과 직접 입력 칸. 오른쪽 끝의 x를 돌려준다.
        private static float AmountBar(string name, float x, float y, int current, Action<int> set)
        {
            for (int i = 0; i < Amounts.Length; i++)
            {
                int amount = Amounts[i];
                Ui.Button(name + amount, content, amount.ToString(), x + i * 66, y, 62, 38,
                    new Action(() => { amountInputs.Remove(name); set(amount); message = ""; Render(); }), true, current == amount);
            }
            float inputX = x + Amounts.Length * 66 + 6;
            string typed;
            if (!amountInputs.TryGetValue(name, out typed))
            {
                typed = Array.IndexOf(Amounts, current) < 0 ? current.ToString() : "";
            }
            Ui.Input(name + "Input", content, typed, "직접 입력", inputX, y, 130, 38, new Action<string>(v => amountInputs[name] = v), true);
            Ui.Button(name + "Apply", content, "적용", inputX + 136, y, 70, 38, new Action(() =>
            {
                string text;
                int value;
                amountInputs.TryGetValue(name, out text);
                if (!int.TryParse(text, out value) || value < 1 || value > MaxAmount)
                {
                    Say("수량은 1~" + MaxAmount + " 사이의 숫자로 입력하세요.");
                    return;
                }
                set(value);
                message = "";
                Render();
            }));
            return inputX + 136 + 70;
        }

        private static void RenderAttributes(WorldUnitBase target)
        {
            var saved = target.data.unitData.propertyData;
            var dyn = target.data.dynUnitData;
            ConfRoleAttributeLimit limit = g.conf.roleAttributeLimit;
            List<AttributeDef> all = AttributeCatalog.All;

            // 게임의 인물 화면과 같은 분류
            for (int i = 0; i < AttributeCatalog.Categories.Length; i++)
            {
                int category = i;
                Ui.Button("AttrCat" + i, content, AttributeCatalog.Categories[i], i * 156, 0, 150, 40,
                    new Action(() => { attributeCategory = category; selectedAttribute = -1; message = ""; Render(); }), true, attributeCategory == i);
            }

            const float head = 52;
            Ui.Label(content, "항목", 0, head, 190, 30, 18).color = Ui.Dim;
            Ui.Label(content, "저장값", 206, head, 110, 30, 18).color = Ui.Dim;
            Ui.Label(content, "게임 화면 값(보정 포함)", 330, head, 220, 30, 18).color = Ui.Dim;
            Ui.Label(content, "허용 범위", 560, head, 220, 30, 18).color = Ui.Dim;

            int row = 0;
            for (int i = 0; i < all.Count; i++)
            {
                AttributeDef def = all[i];
                if (def.Category != attributeCategory)
                {
                    continue;
                }
                int index = i;
                float y = head + 34 + row * 40;
                row++;
                int value = def.Get(saved);
                int min = def.Min(limit);
                int max = def.Max(limit);
                Ui.Label(content, def.Label, 0, y, 200, 36);
                Ui.Button("Select" + index, content, value.ToString(), 206, y, 104, 34, new Action(() => { selectedAttribute = index; attributeInput = ""; message = ""; Render(); }), true, index == selectedAttribute);
                Ui.Label(content, def.DynValue(dyn).ToString(), 330, y, 220, 36);
                Ui.Label(content, min + " ~ " + max + (def.OwnLimit ? " (자체 상한)" : ""), 560, y, 260, 36, 18).color = Ui.Dim;
                Ui.Button("Minus" + index, content, "- " + step, 830, y, 100, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Get(Target.data.unitData.propertyData) - step))));
                Ui.Button("Plus" + index, content, "+ " + step, 936, y, 100, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Get(Target.data.unitData.propertyData) + step))));
                Ui.Button("Max" + index, content, "최대", 1042, y, 84, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Max(g.conf.roleAttributeLimit)))));
            }

            // 분류마다 줄 수가 달라도 아래쪽 줄의 위치는 고정한다(가장 긴 분류가 12줄).
            float footer = head + 34 + 12 * 40 + 12;
            Ui.Label(content, "증감 단위", 0, footer, 110, 38, 18).color = Ui.Dim;
            float right = AmountBar("Step", 110, footer, step, v => step = v);
            Ui.Button("Restore", content, "자원 회복", right + 30, footer, 140, 38, new Action(() => Say(EditService.RestoreResources(Target))));
            Ui.Button("ToPlayer", content, "대상: 플레이어로", right + 180, footer, 200, 38, new Action(() => { targetId = null; martialId = null; replaceIndex = -1; Say("편집 대상을 플레이어로 바꿨습니다."); }), targetId != null);
            RenderAttributeInput(footer + 52);
            Ui.Label(content, "저장값은 인물 고유의 값이고, 게임 화면에는 운명·장비 등의 보정이 더해진 값이 나옵니다. 편집기는 저장값을 바꿉니다.", 0, footer + 104, ContentW, 30, 15).color = Ui.Dim;
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

        private static void ResetItemList()
        {
            itemPage = 0;
            itemDetail = "";
            itemScrollY = 0;
            message = "";
        }

        private static void RenderResources()
        {
            WorldUnitBase player = Player;
            var props = player.data.unitData.propData;
            float contentH = Height - ContentY - 56;

            // 영석 추가량과 아이템 지급 수량은 따로 고른다.
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, 96, Ui.Section);
            Ui.Label(content, "영석: " + EditService.Money(player), 16, 6, 250, 40, 22);
            Ui.Label(content, "추가량", 270, 6, 70, 40, 18).color = Ui.Dim;
            float moneyRight = AmountBar("MoneyAmount", 340, 7, moneyAmount, v => moneyAmount = v);
            Ui.Button("MoneyAdd", content, "영석 + " + moneyAmount, moneyRight + 20, 7, 200, 38, new Action(() => Say(EditService.AddMoney(Player, moneyAmount))));
            Ui.Label(content, "아이템 지급 수량", 16, 50, 250, 40, 20);
            AmountBar("ItemAmount", 340, 51, itemAmount, v => itemAmount = v);
            Ui.Label(content, "아래 표의 지급 버튼 한 번에 이 수량만큼 지급합니다.", 840, 51, 460, 38, 15).color = Ui.Dim;

            // 필터: 아이템명
            Ui.Label(content, "아이템명", 0, 106, 90, 38);
            Ui.Input("ItemQuery", content, itemQuery, "이름의 일부 또는 번호", 90, 106, 280, 38, new Action<string>(v => itemQuery = v));
            Ui.Button("ItemSearch", content, "검색", 378, 106, 84, 38, new Action(() => { appliedItemQuery = itemQuery.Trim(); ResetItemList(); Render(); }));
            Ui.Button("ItemSearchClear", content, "초기화", 468, 106, 96, 38, new Action(() => { itemQuery = ""; appliedItemQuery = ""; itemMainType = -1; itemSubType = -1; ResetItemList(); Render(); }),
                itemQuery.Length > 0 || appliedItemQuery.Length > 0 || itemMainType >= 0);

            // 페이지당 개수
            Ui.Label(content, "페이지당", 1010, 106, 90, 38, 18).color = Ui.Dim;
            for (int i = 0; i < ItemPageSizes.Length; i++)
            {
                int index = i;
                Ui.Button("ItemPageSize" + ItemPageSizes[i], content, ItemPageSizes[i] + "개", 1100 + i * 70, 106, 64, 38,
                    new Action(() => { itemPageSizeIndex = index; ResetItemList(); Render(); }), true, itemPageSizeIndex == i);
            }
            int pageSize = ItemPageSizes[itemPageSizeIndex];

            // 필터: 카테고리(게임의 분류표). 첫 줄은 대분류, 대분류를 고르면 아래에 세부 분류가 나온다.
            Ui.Label(content, "카테고리", 0, 150, 90, 38);
            float cx = 90;
            Ui.Button("ItemCatAll", content, "전체", cx, 150, 80, 38, new Action(() => { itemMainType = -1; itemSubType = -1; ResetItemList(); Render(); }), true, itemMainType < 0);
            cx += 86;
            foreach (ItemCatalog.Category category in ItemCatalog.MainCategories)
            {
                int type = category.Id;
                string label = category.Name + " (" + category.Count + ")";
                float width = 24 + label.Length * 15;
                Ui.Button("ItemCat" + type, content, label, cx, 150, width, 38, new Action(() => { itemMainType = type; itemSubType = -1; itemSubScrollY = 0; ResetItemList(); Render(); }), true, itemMainType == type);
                cx += width + 4;
            }
            float top = 194;
            if (itemMainType >= 0)
            {
                // 세부 분류가 많은 대분류(도구는 50종 이상)는 두 줄만 보이게 하고 그 안에서 스크롤한다.
                const float subRow = 38;
                float subW = ContentW - 90;
                List<ItemCatalog.Category> subs = ItemCatalog.SubCategories(itemMainType);
                var places = new List<Vector2>();
                float sx = 76, sy = 0;
                foreach (ItemCatalog.Category category in subs)
                {
                    float width = SubWidth(category);
                    if (sx + width > subW - Ui.ScrollBarWidth)
                    {
                        sx = 0;
                        sy += subRow;
                    }
                    places.Add(new Vector2(sx, sy));
                    sx += width + 6;
                }
                float subsH = sy + subRow;
                float shownH = Math.Min(subsH, subRow * 2);
                Ui.Label(content, "세부", 0, top, 90, 34, 18).color = Ui.Dim;
                Transform subRoot = subsH > shownH
                    ? Ui.Scroll("ItemSubs", content, 90, top, subW, shownH, subsH, itemSubScrollY, new Action<float>(v => itemSubScrollY = v))
                    : Ui.Rect("ItemSubs", content, 90, top, subW, shownH);
                Ui.Button("ItemSubAll", subRoot, "전체", 0, 0, 70, 34, new Action(() => { itemSubType = -1; ResetItemList(); Render(); }), true, itemSubType < 0);
                for (int i = 0; i < subs.Count; i++)
                {
                    int sub = subs[i].Id;
                    Ui.Button("ItemSub" + sub, subRoot, subs[i].Name + " (" + subs[i].Count + ")", places[i].x, places[i].y, SubWidth(subs[i]), 34,
                        new Action(() => { itemSubType = sub; ResetItemList(); Render(); }), true, itemSubType == sub);
                }
                top += shownH + 4;
            }

            List<ItemCatalog.Entry> found = ItemCatalog.Find(appliedItemQuery, itemMainType, itemSubType);
            int pages = Math.Max(1, (found.Count + pageSize - 1) / pageSize);
            itemPage = Mathf.Clamp(itemPage, 0, pages - 1);

            // 표 머리
            const float colId = 8, colCat = 110, colName = 290, colLevel = 560, colOwned = 630, colDesc = 710, colGive = 1166;
            Ui.Panel("ItemHead", content, 0, top, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", colId, top, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "카테고리", colCat, top, 170, 30, 16).color = Ui.Dim;
            Ui.Label(content, "아이템명", colName, top, 260, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", colLevel, top, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "보유", colOwned, top, 70, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", colDesc, top, 440, 30, 16).color = Ui.Dim;
            Ui.Label(content, "지급", colGive, top, 116, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            top += 32;

            // 아래에서부터: 선택한 아이템의 전체 설명, 그 위에 페이징 줄, 남은 높이가 표 본문이다.
            const float detailH = 92;
            float footer = contentH - detailH - 46;
            const float rowHeight = 32;
            int first = itemPage * pageSize;
            int shown = Math.Max(0, Math.Min(pageSize, found.Count - first));
            RectTransform rowsRoot = Ui.Scroll("ItemTable", content, 0, top, ContentW, footer - 8 - top, shown * rowHeight, itemScrollY,
                new Action<float>(v => itemScrollY = v));
            if (found.Count == 0)
            {
                Ui.Label(rowsRoot, "조건에 맞는 아이템이 없습니다.", 16, 8, 800, 34, 18).color = Ui.Warn;
            }
            for (int row = 0; row < shown; row++)
            {
                ItemCatalog.Entry entry = found[first + row];
                float y = row * rowHeight;
                if (row % 2 == 1)
                {
                    Ui.Panel("Stripe", rowsRoot, 0, y, ContentW - Ui.ScrollBarWidth, rowHeight, new Color(1f, 1f, 1f, 0.04f)).GetComponent<Image>().raycastTarget = false;
                }
                Ui.Label(rowsRoot, entry.Id.ToString(), colId, y, 100, rowHeight, 16);
                Ui.Label(rowsRoot, Shorten(entry.SubTypeName, 8), colCat, y, 175, rowHeight, 16);
                Ui.Button("ItemName" + row, rowsRoot, Shorten(entry.Name, 12), colName, y + 2, 262, rowHeight - 4,
                    new Action(() => { itemDetail = Detail(entry); if (itemDetailLabel != null) { itemDetailLabel.text = itemDetail; } }));
                Ui.Label(rowsRoot, entry.Level.ToString(), colLevel, y, 60, rowHeight, 16);
                Ui.Label(rowsRoot, props.GetPropsNum(entry.Id).ToString(), colOwned, y, 70, rowHeight, 16);
                Ui.Label(rowsRoot, Shorten(entry.Desc, 27), colDesc, y, 440, rowHeight, 15).color = Ui.Dim;
                Ui.Button("ItemGive" + row, rowsRoot, "지급 x" + itemAmount, colGive, y + 2, 116, rowHeight - 4, new Action(() => Say(EditService.GiveItem(Player, entry.Id, itemAmount))));
            }

            // 페이징
            Ui.Button("ItemFirst", content, "처음", 0, footer, 80, 38, new Action(() => { itemPage = 0; itemScrollY = 0; Render(); }), itemPage > 0);
            Ui.Button("ItemPrev", content, "이전", 86, footer, 80, 38, new Action(() => { itemPage--; itemScrollY = 0; Render(); }), itemPage > 0);
            Ui.Label(content, (itemPage + 1) + " / " + pages, 172, footer, 130, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("ItemNext", content, "다음", 308, footer, 80, 38, new Action(() => { itemPage++; itemScrollY = 0; Render(); }), itemPage < pages - 1);
            Ui.Button("ItemLast", content, "끝", 394, footer, 80, 38, new Action(() => { itemPage = pages - 1; itemScrollY = 0; Render(); }), itemPage < pages - 1);
            string range = found.Count == 0 ? "0개" : (first + 1) + "~" + (first + shown) + "번째 / 조건에 맞는 아이템 " + found.Count + "개";
            Ui.Label(content, range + "  ·  목록은 마우스 휠이나 오른쪽 막대로 스크롤", 490, footer, 800, 38, 15).color = Ui.Dim;

            // 선택한 아이템의 전체 설명
            Ui.Panel("ItemDetail", content, 0, contentH - detailH, ContentW, detailH, Ui.Section);
            itemDetailLabel = Ui.Label(content, itemDetail.Length > 0 ? itemDetail : "아이템명을 누르면 여기에 전체 설명이 나옵니다.", 12, contentH - detailH + 4, ContentW - 24, detailH - 8, 16, TextAnchor.UpperLeft);
            itemDetailLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemDetailLabel.verticalOverflow = VerticalWrapMode.Truncate;
            if (itemDetail.Length == 0)
            {
                itemDetailLabel.color = Ui.Dim;
            }
        }

        private static float SubWidth(ItemCatalog.Category category)
        {
            return 46 + (category.Name.Length + category.Count.ToString().Length) * 16;
        }

        private static string Detail(ItemCatalog.Entry entry)
        {
            return "[" + entry.Id + "] " + entry.Name + "  (" + entry.TypeName + " / " + entry.SubTypeName + ", 품질 " + entry.Level + ")\n" + entry.Desc;
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
                List<int> needItems = EditService.BreakthroughNeedItems(row);
                List<int> treasures = EditService.BreakthroughTreasures(row);
                string need = needItems.Count == 0 ? "없음"
                    : (EditService.ItemName(needItems[0]) ?? "?") + (needItems.Count > 1 ? " 등 " : " ") + needItems.Count + "개";
                string treasure = treasures.Count == 0 ? "없음"
                    : (EditService.ItemName(treasures[0]) ?? "?") + (treasures.Count > 1 ? " 등 " : " ") + treasures.Count + "개";
                Ui.Label(content, GameTool.LS(row.qualityName) + "  ·  필요 재료 " + need + "  ·  천재지보 " + treasure + "  ·  역천개명 " + row.fateNum + "개", 16, y, 980, 38, 18);
                Ui.Button("MajorGive" + i, content, "재료·천재지보 지급", 1010, y + 2, 280, 36, new Action(() => Say(EditService.GiveBreakthroughMaterials(Player, row))),
                    needItems.Count + treasures.Count > 0);
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

        // 공법 탭의 기술 추가 화면: 게임 기술 화면의 다섯 분류에서 이름으로 골라 배운 상태로 넣는다.
        private static void RenderSkillAdd(WorldUnitBase target)
        {
            const int rows = 12;
            Ui.Label(content, "기술 추가 — " + EditService.NameOf(target), 0, 0, 500, 36, 21);
            Ui.Button("SkillBack", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(() => { skillAdding = false; message = ""; Render(); }));
            for (int i = 0; i < SkillCatalog.Kinds.Length; i++)
            {
                int index = i;
                Ui.Button("SkillKind" + i, content, SkillCatalog.Kinds[i].Tab, i * 156, 44, 150, 38, new Action(() => { skillKind = index; skillPage = 0; message = ""; Render(); }), true, skillKind == i);
            }
            Ui.Input("SkillQuery", content, skillQuery, "이름의 일부 또는 번호", 800, 44, 250, 38, new Action<string>(v => skillQuery = v));
            Ui.Button("SkillSearch", content, "검색", 1058, 44, 80, 38, new Action(() => { appliedSkillQuery = skillQuery.Trim(); skillPage = 0; Render(); }));
            Ui.Button("SkillSearchClear", content, "지움", 1144, 44, 80, 38, new Action(() => { skillQuery = ""; appliedSkillQuery = ""; skillPage = 0; Render(); }), skillQuery.Length > 0 || appliedSkillQuery.Length > 0);

            // 만들 기술의 경지. 처음에는 편집 대상의 현재 대경지.
            if (skillGrade <= 0)
            {
                skillGrade = Math.Max(1, FateService.CurrentGrade(target));
            }
            Ui.Label(content, "경지", 0, 90, 60, 36, 18).color = Ui.Dim;
            for (int grade = 1; grade <= 10; grade++)
            {
                int value = grade;
                Ui.Button("SkillGrade" + grade, content, grade.ToString(), 60 + (grade - 1) * 50, 90, 46, 36, new Action(() => { skillGrade = value; message = ""; Render(); }), true, skillGrade == grade);
            }
            Ui.Label(content, "기술의 경지(1 연기 ~ 10)입니다. 옵션은 게임의 뽑기로 정해지고, 추가한 뒤 이 탭에서 품질·교체로 고칠 수 있습니다.", 580, 90, 720, 36, 15).color = Ui.Dim;

            List<SkillCatalog.Entry> found = SkillCatalog.Find(skillKind, appliedSkillQuery);
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
                bool inRange = skillGrade >= entry.GradeMin && skillGrade <= entry.GradeMax;
                Ui.Label(content, Shorten(entry.Name, 16) + "  [" + entry.BaseId + "]  경지 " + entry.GradeMin + "~" + entry.GradeMax, 0, y, 1000, 26, 18);
                Ui.Label(content, Shorten(entry.Desc, 80), 16, y + 24, 1100, 22, 15).color = Ui.Dim;
                Ui.Button("SkillAdd" + index, content, inRange ? "추가" : "경지 범위 밖", ContentW - 170, y + 6, 170, 36,
                    new Action(() => Say(SkillCatalog.Learn(Target, skillKind, entry, skillGrade))), inRange);
            }
            float footer = 136 + rows * 50 + 4;
            Ui.Button("SkillPrev", content, "이전", 0, footer, 90, 38, new Action(() => { skillPage--; Render(); }), skillPage > 0);
            Ui.Label(content, (skillPage + 1) + " / " + pages, 100, footer, 110, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("SkillNext", content, "다음", 220, footer, 90, 38, new Action(() => { skillPage++; Render(); }), skillPage < pages - 1);
            Ui.Label(content, SkillCatalog.Kinds[skillKind].Tab + " " + found.Count + "개" + (appliedSkillQuery.Length > 0 ? "  ('" + appliedSkillQuery + "' 포함)" : ""), 330, footer, 700, 38, 16).color = Ui.Dim;
        }

        private static void RenderMartial(WorldUnitBase target)
        {
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
                Ui.Button("PickMartial" + index, content, id == martialId ? "선택됨" : "선택", 720, y, 110, 32, new Action(() => { martialId = id; replaceIndex = -1; martialPending = null; message = ""; Render(); }), true, id == martialId);
            }
            float listEnd = 32 + listRows * 38;
            Ui.Button("MartialPrev", content, "이전", 0, listEnd, 80, 34, new Action(() => { martialPage--; Render(); }), martialPage > 0);
            Ui.Label(content, (martialPage + 1) + " / " + pages, 86, listEnd, 80, 34, 18, TextAnchor.MiddleCenter);
            Ui.Button("MartialNext", content, "다음", 172, listEnd, 80, 34, new Action(() => { martialPage++; Render(); }), martialPage < pages - 1);
            Ui.Label(content, all.Count == 0 ? "익힌 공법이 없습니다." : "대상이 익힌 공법 " + all.Count + "개", 270, listEnd, 400, 34, 18).color = Ui.Dim;
            Ui.Button("SkillAddOpen", content, "기술 추가", 700, listEnd, 160, 34, new Action(() => { skillAdding = true; skillPage = 0; skillGrade = 0; martialPending = null; message = ""; Render(); }));

            float box = listEnd + 46;
            Ui.Panel("MartialBox", content, 0, box, ContentW, 330, Ui.Section);
            if (selected == null)
            {
                Ui.Label(content, "위에서 공법을 선택하면 옵션을 편집할 수 있습니다.", 16, box + 10, 880, 36);
                return;
            }
            Ui.Label(content, MartialService.Name(selected) + "  (" + MartialService.TypeLabel(selected.martialType) + ")", 16, box + 6, 420, 36, 21);
            Ui.Button("MartialExp", content, "숙련 경험치 + " + Step, 450, box + 8, 230, 34, new Action(() => Say(MartialService.AddExp(Target, martialId, Step))));
            AmountBar("Step", 700, box + 6, step, v => step = v);
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
            Ui.Label(content, "수련은 위의 숙련 경험치로, 참오는 위 옵션의 품질·교체로 대신합니다.", 340, box + 286, 800, 34, 15).color = Ui.Dim;            List<MartialService.PrefixView> prefixes = MartialService.Prefixes(selected);
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

        // 운명 탭의 역천개명 화면: 도달한 경지마다 한 줄. 변경을 누르면 그 경지의 후보 목록이 열린다.
        private static void RenderFate(WorldUnitBase target)
        {
            if (fatePickGrade > 0)
            {
                RenderFatePicker(target, 12);
                return;
            }
            Ui.Label(content, "역천개명 — " + EditService.NameOf(target), 0, 0, 600, 36, 21);
            Ui.Button("FateBack", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(() => { fateOpen = false; message = ""; Render(); }));
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
                    new Action(() => { fatePickGrade = slot.Grade; fatePickPage = 0; message = ""; Render(); }));
            }
            Ui.Label(content, "게임의 경지 화면은 경지마다 역천개명 하나를 보여 줍니다. 여기서 정하면 효과와 그 화면의 표시가 함께 바뀝니다. 도달한 경지만 정할 수 있습니다.", 0, 82 + Math.Max(1, slots.Count) * 40 + 12, ContentW, 30, 15).color = Ui.Dim;
        }

        private static void RenderFatePicker(WorldUnitBase target, int rows)
        {
            int grade = fatePickGrade;
            Ui.Label(content, "역천개명 고르기 — " + EditService.NameOf(target) + ", 대경지 " + grade, 0, 0, 700, 36, 21);
            Ui.Button("FatePickBack", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(() => { fatePickGrade = 0; message = ""; Render(); }));
            Ui.Input("FateQuery", content, fateQuery, "이름·효과의 일부", 0, 44, 250, 38, new Action<string>(v => fateQuery = v));
            Ui.Button("FateSearch", content, "검색", 258, 44, 80, 38, new Action(() => { appliedFateQuery = fateQuery.Trim(); fatePickPage = 0; Render(); }));
            Ui.Button("FateSearchClear", content, "지움", 344, 44, 80, 38, new Action(() => { fateQuery = ""; appliedFateQuery = ""; fatePickPage = 0; Render(); }), fateQuery.Length > 0 || appliedFateQuery.Length > 0);
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
            Ui.Button("FatePickPrev", content, "이전", 0, footer, 90, 38, new Action(() => { fatePickPage--; Render(); }), fatePickPage > 0);
            Ui.Label(content, (fatePickPage + 1) + " / " + pages, 100, footer, 90, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("FatePickNext", content, "다음", 200, footer, 90, 38, new Action(() => { fatePickPage++; Render(); }), fatePickPage < pages - 1);
            Ui.Label(content, "후보 " + candidates.Count + "개", 310, footer, 600, 38, 16).color = Ui.Dim;
        }

        private static void RenderLuck(WorldUnitBase target)
        {
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
                    Ui.Button("LuckReplace" + index, content, "교체", 1054, y + 1, 120, 34, new Action(() => { luckReplaceIndex = bornIndex; luckPicking = true; luckPickPage = 0; pendingLuckRemove = 0; message = ""; Render(); }));
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
            Ui.Button("LuckPrev", content, "이전", 0, footer, 90, 40, new Action(() => { luckPage--; pendingLuckRemove = 0; Render(); }), luckPage > 0);
            Ui.Label(content, (luckPage + 1) + " / " + pages, 100, footer, 80, 40, 18, TextAnchor.MiddleCenter);
            Ui.Button("LuckNext", content, "다음", 190, footer, 90, 40, new Action(() => { luckPage++; pendingLuckRemove = 0; Render(); }), luckPage < pages - 1);
            Ui.Button("FateOpen", content, "역천개명 (경지별)", 470, footer, 220, 40, new Action(() => { fateOpen = true; fatePickGrade = 0; pendingLuckRemove = 0; message = ""; Render(); }));
            Ui.Button("LuckAddOpen", content, "운명 추가", 300, footer, 160, 40, new Action(() => { luckReplaceIndex = -1; luckPicking = true; luckPickPage = 0; pendingLuckRemove = 0; message = ""; Render(); }));
            Ui.Label(content, "게임의 인물 화면은 선천운명 칸과, 후천 분류의 후천운명만 보여 줍니다. \"표시 안 됨\"은 효과는 적용되지만 그 화면에 나오지 않는 운명입니다.", 0, footer + 48, ContentW, 30, 15).color = Ui.Dim;
        }

        private static void RenderLuckPicker(WorldUnitBase target, int rows)
        {
            bool replacing = luckReplaceIndex >= 0;
            Ui.Label(content, (replacing ? "선천운명 " + (luckReplaceIndex + 1) + "번 교체 — " : "운명 추가 — ") + EditService.NameOf(target), 0, 0, 600, 36, 21);
            Ui.Button("LuckBack", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(() => { luckPicking = false; message = ""; Render(); }));
            int[] types = { 1, 2, 3 };
            for (int i = 0; i < types.Length; i++)
            {
                int type = types[i];
                Ui.Button("LuckType" + type, content, LuckService.TypeLabel(type) + " 분류", i * 146, 44, 140, 38, new Action(() => { luckType = type; luckPickPage = 0; Render(); }), true, luckType == type);
            }
            Ui.Input("LuckQuery", content, luckQuery, "이름·효과의 일부", 460, 44, 250, 38, new Action<string>(v => luckQuery = v));
            Ui.Button("LuckSearch", content, "검색", 718, 44, 80, 38, new Action(() => { appliedLuckQuery = luckQuery.Trim(); luckPickPage = 0; Render(); }));
            Ui.Button("LuckSearchClear", content, "지움", 804, 44, 80, 38, new Action(() => { luckQuery = ""; appliedLuckQuery = ""; luckPickPage = 0; Render(); }), luckQuery.Length > 0 || appliedLuckQuery.Length > 0);
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
            Ui.Button("LuckPickPrev", content, "이전", 0, footer, 90, 38, new Action(() => { luckPickPage--; Render(); }), luckPickPage > 0);
            Ui.Label(content, (luckPickPage + 1) + " / " + pages, 100, footer, 90, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("LuckPickNext", content, "다음", 200, footer, 90, 38, new Action(() => { luckPickPage++; Render(); }), luckPickPage < pages - 1);
            Ui.Label(content, "후보 " + candidates.Count + "개" + (appliedLuckQuery.Length > 0 ? "  ('" + appliedLuckQuery + "' 포함)" : ""), 310, footer, 900, 38, 16).color = Ui.Dim;
        }

        // 기예 탭: 게임 기예 화면의 여섯 탭과 같은 분류. 서적을 골라 배우게 하거나 배운 기록을 지운다.
        private static void RenderArt()
        {
            float contentH = Height - ContentY - 56;
            for (int i = 0; i < ArtService.Kinds.Length; i++)
            {
                int index = i;
                ArtService.Kind each = ArtService.Kinds[i];
                Ui.Button("ArtKind" + i, content, each.Tab + " (" + ArtService.LearnedCount(each) + "/" + ArtService.Books(each, "").Count + ")", i * 176, 0, 170, 40,
                    new Action(() => { artKind = index; artPage = 0; artScrollY = 0; artPending = 0; message = ""; Render(); }), true, artKind == i);
            }
            ArtService.Kind kind = ArtService.Kinds[artKind];

            Ui.Label(content, kind.Book, 0, 50, 90, 38);
            Ui.Input("ArtQuery", content, artQuery, "이름의 일부 또는 번호", 90, 50, 280, 38, new Action<string>(v => artQuery = v));
            Ui.Button("ArtSearch", content, "검색", 378, 50, 84, 38, new Action(() => { appliedArtQuery = artQuery.Trim(); artPage = 0; artScrollY = 0; message = ""; Render(); }));
            Ui.Button("ArtSearchClear", content, "초기화", 468, 50, 96, 38, new Action(() => { artQuery = ""; appliedArtQuery = ""; artPage = 0; artScrollY = 0; message = ""; Render(); }),
                artQuery.Length > 0 || appliedArtQuery.Length > 0);
            Ui.Button("ArtLearnAll", content, kind.Tab + " 모두 배우기", 600, 50, 220, 38, new Action(() => { artPending = 0; Say(ArtService.LearnAll(ArtService.Kinds[artKind])); }));
            Ui.Label(content, "기예는 플레이어의 기록입니다(편집 대상과 무관).", 840, 50, 460, 38, 15).color = Ui.Dim;

            List<ItemCatalog.Entry> books = ArtService.Books(kind, appliedArtQuery);
            const int pageSize = 50;
            int pages = Math.Max(1, (books.Count + pageSize - 1) / pageSize);
            artPage = Mathf.Clamp(artPage, 0, pages - 1);

            const float colId = 8, colName = 110, colLevel = 420, colState = 490, colDesc = 600, colAct = 1166;
            Ui.Panel("ArtHead", content, 0, 98, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", colId, 98, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, kind.Book, colName, 98, 300, 30, 16).color = Ui.Dim;
            Ui.Label(content, "품질", colLevel, 98, 60, 30, 16).color = Ui.Dim;
            Ui.Label(content, "상태", colState, 98, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", colDesc, 98, 500, 30, 16).color = Ui.Dim;

            const float rowHeight = 32;
            float footer = contentH - 46;
            int first = artPage * pageSize;
            int shown = Math.Max(0, Math.Min(pageSize, books.Count - first));
            RectTransform rowsRoot = Ui.Scroll("ArtTable", content, 0, 130, ContentW, footer - 8 - 130, shown * rowHeight, artScrollY, new Action<float>(v => artScrollY = v));
            if (books.Count == 0)
            {
                Ui.Label(rowsRoot, "조건에 맞는 " + kind.Book + "이(가) 없습니다.", 16, 8, 800, 34, 18).color = Ui.Warn;
            }
            for (int row = 0; row < shown; row++)
            {
                ItemCatalog.Entry book = books[first + row];
                float y = row * rowHeight;
                bool learned = ArtService.Learned(kind, book.Id);
                bool confirming = artPending == book.Id;
                if (row % 2 == 1)
                {
                    Ui.Panel("Stripe", rowsRoot, 0, y, ContentW - Ui.ScrollBarWidth, rowHeight, new Color(1f, 1f, 1f, 0.04f)).GetComponent<Image>().raycastTarget = false;
                }
                Ui.Label(rowsRoot, book.Id.ToString(), colId, y, 100, rowHeight, 16);
                Ui.Label(rowsRoot, Shorten(book.Name, 15), colName, y, 305, rowHeight, 17);
                Ui.Label(rowsRoot, book.Level.ToString(), colLevel, y, 60, rowHeight, 16);
                Text state = Ui.Label(rowsRoot, learned ? "배움" : "-", colState, y, 100, rowHeight, 16);
                state.color = learned ? Ui.Warn : Ui.Dim;
                Ui.Label(rowsRoot, Shorten(book.Desc, 32), colDesc, y, 560, rowHeight, 15).color = Ui.Dim;
                Ui.Button("ArtAct" + row, rowsRoot, !learned ? "배우기" : confirming ? "정말 잊기" : "잊기", colAct, y + 2, 116, rowHeight - 4, new Action(() =>
                {
                    ArtService.Kind current = ArtService.Kinds[artKind];
                    if (!learned)
                    {
                        artPending = 0;
                        Say(ArtService.Learn(current, book));
                        return;
                    }
                    if (!confirming)
                    {
                        artPending = book.Id;
                        Say(book.Name + ": 배운 기록을 지웁니다. 계속하려면 \"정말 잊기\"를 한 번 더 누르세요.");
                        return;
                    }
                    artPending = 0;
                    Say(ArtService.Forget(current, book));
                }), true, confirming);
            }

            Ui.Button("ArtPrev", content, "이전", 0, footer, 80, 38, new Action(() => { artPage--; artScrollY = 0; Render(); }), artPage > 0);
            Ui.Label(content, (artPage + 1) + " / " + pages, 86, footer, 110, 38, 18, TextAnchor.MiddleCenter);
            Ui.Button("ArtNext", content, "다음", 202, footer, 80, 38, new Action(() => { artPage++; artScrollY = 0; Render(); }), artPage < pages - 1);
            Ui.Label(content, kind.Book + " " + books.Count + "개  ·  목록은 마우스 휠이나 오른쪽 막대로 스크롤", 300, footer, 900, 38, 15).color = Ui.Dim;
        }

        private static void RenderWorld()
        {
            Vector2Int here = WorldService.PlayerPoint;
            Ui.Panel("MoveBox", content, 0, 0, ContentW, 250, Ui.Section);
            Ui.Label(content, "순간 이동", 16, 6, 300, 36, 21);
            Ui.Label(content, "현재 위치: (" + here.x + ", " + here.y + ")", 16, 44, 400, 34);
            AmountBar("Step", 700, 8, step, v => step = v);
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
                Ui.Button("EditNpc", content, "능력치 편집", 716, 54, 190, 38, new Action(() => { targetId = selectedNpcId; tab = 0; attributeCategory = 0; selectedAttribute = -1; martialId = null; replaceIndex = -1; luckPicking = false; luckPage = 0; Say("편집 대상을 NPC로 바꿨습니다."); }));
                AmountBar("Step", 716, 8, step, v => step = v);
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
