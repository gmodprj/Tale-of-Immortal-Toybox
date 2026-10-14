using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 편집기 창의 틀: 제목 줄, 탭 줄, 결과 영역, 탭 전환, 탭들이 함께 쓰는 상태.
    // 탭의 내용은 UI/Tabs의 클래스가 그리고, 게임 값의 읽기·쓰기는 서비스에 맡긴다.
    internal static class EditorWindow
    {
        public const float Width = 1500;
        public const float Height = 1000;
        public const float TitleH = 52;
        public const float ContentX = 180;
        public const float ContentY = 64;
        public const float ContentW = 1304;
        public const float MessageH = 84;
        public const float ContentH = Height - ContentY - MessageH - 8;

        private const int OverlayOrder = 30000;
        // 게임 화면의 정렬은 20 간격이다(UIMgr.UIIntervalSorting). 그 사이에 끼운다.
        private const int OrderAboveTop = 10;
        // 창의 위치는 1920x1080 기준 좌표다(왼쪽 위가 원점, y는 아래로 증가). 제목 줄의 세로 전체와 가로 200 이상이 화면 안에 남아야 한다.
        private const float ScreenW = 1920;
        private const float ScreenH = 1080;
        private const float MinVisible = 200;
        private const float DefaultX = 60;
        private const float DefaultY = 60;
        private const float FlashSeconds = 0.6f;
        private static readonly Color FlashColor = new Color(0.36f, 0.31f, 0.14f, 1f);

        private static GameObject root;
        private static Canvas canvas;
        private static RectTransform window;
        private static RectTransform titleArea;
        private static RectTransform tabBar;
        private static RectTransform content;
        private static Image messageBox;
        private static Text messageLabel;

        private static EditorTab[] tabs;
        private static int tab;
        private static string targetId;   // 편집 대상. null이면 플레이어
        private static string message = "";
        private static float flash;       // 결과 영역을 밝혀 둘 남은 시간(초)
        private static bool tickWarned;
        private static bool cameraMode;   // 게임과 같은 카메라 방식으로 그리는 중인지
        private static int raisedTop;     // 맨 앞으로 가져올 때의 UI 층 맨 위 정렬 값
        private static Vector2 position = new Vector2(DefaultX, DefaultY);
        private static Vector2 dragStart;   // 끌기를 시작할 때의 창 위치
        private static Vector2 dragMoved;   // 끌기를 시작한 뒤의 누적 이동량(화면 픽셀, y는 위로 증가)
        private static bool collapsed;
        private static Text collapseLabel;

        // NPC 탭에서 고른 NPC. 세계 탭의 "선택한 NPC에게 이동"이 쓴다.
        public static string SelectedNpcId;

        public static bool IsOpen
        {
            get { return root != null && root.activeSelf; }
        }

        public static WorldUnitBase Player
        {
            get { return g.world.playerUnit; }
        }

        // 편집 대상. 대상으로 잡은 NPC를 찾지 못하면 플레이어로 돌아간다.
        public static WorldUnitBase Target
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

        public static bool IsTargetNpc
        {
            get { return targetId != null; }
        }

        // 편집 대상을 바꾼다. null이면 플레이어. 탭들이 대상에 딸린 선택을 지우도록 알린다.
        public static void SetTarget(string unitId)
        {
            targetId = unitId;
            for (int i = 0; tabs != null && i < tabs.Length; i++)
            {
                tabs[i].TargetChanged();
            }
        }

        // 게임의 UI 카메라로 그릴 수 있는지. 주 화면이나 장면이 바뀌는 중에는 없을 수 있다.
        private static bool CameraUsable(out Canvas game, out Camera cam)
        {
            game = null;
            cam = null;
            try
            {
                if (g.ui == null)
                {
                    return false;
                }
                game = g.ui.canvas;
                cam = g.ui.uiCamera;
                return game != null && cam != null && cam.isActiveAndEnabled && game.renderMode == RenderMode.ScreenSpaceCamera;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ConfigPath
        {
            get { return Path.Combine(ChangeLog.DataDir, "config.txt"); }
        }

        private static Vector2 Clamp(Vector2 p)
        {
            return new Vector2(
                Mathf.Round(Mathf.Clamp(p.x, MinVisible - Width, ScreenW - MinVisible)),
                Mathf.Round(Mathf.Clamp(p.y, 0f, ScreenH - TitleH)));
        }

        private static void MoveTo(float x, float y)
        {
            position = Clamp(new Vector2(x, y));
            window.anchoredPosition = new Vector2(position.x, -position.y);
        }

        // config.txt의 "window=<x>,<y>"를 읽는다. 없거나 읽을 수 없으면 기본 위치, 화면 밖이면 범위 안의 가장 가까운 자리.
        private static void LoadPosition()
        {
            position = new Vector2(DefaultX, DefaultY);
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return;
                }
                foreach (string line in File.ReadAllLines(ConfigPath))
                {
                    string trimmed = line.Trim();
                    if (!trimmed.StartsWith("window=", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    string[] parts = trimmed.Substring(7).Split(',');
                    int x, y;
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out x) && int.TryParse(parts[1].Trim(), out y))
                    {
                        position = Clamp(new Vector2(x, y));
                    }
                }
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] 창 위치를 읽지 못했습니다: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                MelonLogger.Msg("[TlToybox] 창 위치를 읽지 못했습니다: " + e.Message);
            }
        }

        // 위치를 config.txt에 쓴다. 다른 줄(hotkey= 등)은 그대로 두고 window= 줄만 바꾼다. 쓰지 못해도 동작에는 영향이 없다.
        private static void SavePosition()
        {
            try
            {
                Directory.CreateDirectory(ChangeLog.DataDir);
                var lines = new List<string>();
                if (File.Exists(ConfigPath))
                {
                    foreach (string line in File.ReadAllLines(ConfigPath))
                    {
                        if (!line.Trim().StartsWith("window=", StringComparison.OrdinalIgnoreCase))
                        {
                            lines.Add(line);
                        }
                    }
                }
                lines.Add("window=" + (int)position.x + "," + (int)position.y);
                File.WriteAllLines(ConfigPath, lines.ToArray());
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] 창 위치를 저장하지 못했습니다: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                MelonLogger.Msg("[TlToybox] 창 위치를 저장하지 못했습니다: " + e.Message);
            }
        }

        // 버튼 콜백 안의 예외는 화면에 나오지 않는다. 끌기는 초당 수십 번 불리므로 한 번만 로그에 남긴다.
        private static void AddEntry(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> call)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = type;
            if (entry.callback == null)
            {
                entry.callback = new EventTrigger.TriggerEvent();
            }
            entry.callback.AddListener(new Action<BaseEventData>(data =>
            {
                try
                {
                    call(data);
                }
                catch (Exception e)
                {
                    if (!tickWarned)
                    {
                        tickWarned = true;
                        MelonLogger.Msg("[TlToybox] 창 끌기 오류: " + e.Message);
                    }
                }
            }));
            trigger.triggers.Add(entry);
        }

        // 제목 줄의 바탕을 끌기 손잡이로 만든다.
        // 위치는 끌기 시작 위치 + 누적 이동량으로 계산한다. 매번 더해 가면 소수 오차가 쌓인다(실측: 10번에 59.99997).
        private static void AddDrag(GameObject handle)
        {
            var trigger = handle.AddComponent<EventTrigger>();
            AddEntry(trigger, EventTriggerType.BeginDrag, data =>
            {
                dragStart = position;
                dragMoved = Vector2.zero;
            });
            AddEntry(trigger, EventTriggerType.Drag, data =>
            {
                dragMoved = dragMoved + data.Cast<PointerEventData>().delta;
                float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
                MoveTo(dragStart.x + dragMoved.x / scale, dragStart.y - dragMoved.y / scale);
            });
            AddEntry(trigger, EventTriggerType.EndDrag, data => SavePosition());
        }

        // 접으면 제목 줄만 남는다. 접힌 상태는 기억하지 않는다(다음에 열면 펼친 상태).
        public static void SetCollapsed(bool value)
        {
            if (window == null)
            {
                return;
            }
            collapsed = value;
            tabBar.gameObject.SetActive(!value);
            content.gameObject.SetActive(!value);
            messageBox.gameObject.SetActive(!value);
            window.sizeDelta = new Vector2(Width, value ? TitleH : Height);
            collapseLabel.text = value ? "펼침" : "접기";
        }

        // 편집기를 맨 앞으로 가져온다. 지금 열려 있는 게임 화면들 바로 위에 놓으므로, 이 뒤에 열리는 게임 화면은 편집기 위에 뜬다.
        // 확인 창과 로딩 화면은 더 높은 층이라 항상 위다. UI 카메라를 쓸 수 없으면 예전처럼 무엇보다도 위에 덧그린다.
        public static void Raise()
        {
            if (canvas == null)
            {
                return;
            }
            Canvas game;
            Camera cam;
            if (!CameraUsable(out game, out cam))
            {
                cameraMode = false;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = OverlayOrder;
                return;
            }
            cameraMode = true;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = game.planeDistance;
            canvas.sortingLayerID = game.sortingLayerID;
            root.layer = game.gameObject.layer;   // 카메라가 이 층만 그린다. 뿌리의 층만 맞추면 자식도 그려진다(실측)
            raisedTop = g.ui.GetLayerTopSort(UILayer.UI);
            canvas.sortingOrder = raisedTop + OrderAboveTop;
        }

        // 나중에 열린 게임 화면이 편집기를 가리고 있는지.
        public static bool IsCovered
        {
            get { return IsOpen && cameraMode && g.ui.GetLayerTopSort(UILayer.UI) > canvas.sortingOrder; }
        }

        public static void SwitchTab(int index)
        {
            tabs[tab].Leave();
            tab = index;
            message = "";
            tabs[tab].OnShow();
            Render();
        }

        // 단축키: 닫혀 있으면 열고, 게임 화면에 가려져 있으면 맨 앞으로 가져오고, 맨 앞이면 닫는다.
        public static void Toggle()
        {
            if (!IsOpen)
            {
                Open();
                return;
            }
            if (IsCovered)
            {
                Raise();
                return;
            }
            Close();
        }

        public static void Close()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        // 결과 문구를 보이고 현재 탭을 다시 그린다. 새 문구가 눈에 띄도록 결과 영역을 잠깐 밝힌다.
        public static void Say(string text)
        {
            message = text;
            flash = FlashSeconds;
            Refresh();
        }

        public static void ClearMessage()
        {
            message = "";
        }

        public static void Refresh()
        {
            if (root != null)
            {
                Render();
            }
        }

        // 매 프레임 불린다. 예외를 밖으로 던지지 않는다.
        public static void Tick()
        {
            try
            {
                if (!IsOpen)
                {
                    return;
                }
                if (flash > 0f)
                {
                    flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime);
                    messageBox.color = Color.Lerp(Ui.Section, FlashColor, flash / FlashSeconds);
                }
                if (!cameraMode)
                {
                    return;
                }
                if (canvas.worldCamera == null)
                {
                    // 장면이 바뀌어 카메라가 사라졌다. 방식을 다시 정한다.
                    Raise();
                    return;
                }
                int top = g.ui.GetLayerTopSort(UILayer.UI);
                if (top < raisedTop)
                {
                    // 맨 앞으로 가져올 때 있던 게임 화면이 닫혔다. 내려 두지 않으면 다음에 열리는 화면이 편집기 뒤에 뜬다.
                    raisedTop = top;
                    canvas.sortingOrder = top + OrderAboveTop;
                }
            }
            catch (Exception e)
            {
                if (!tickWarned)
                {
                    tickWarned = true;
                    MelonLogger.Msg("[TlToybox] 편집기 창의 주기 처리 오류: " + e.Message);
                }
            }
        }

        private static void Open()
        {
            if (root == null)
            {
                Build();
            }
            root.SetActive(true);
            Raise();
            SetCollapsed(false);
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].OnOpen();
            }
            Render();
        }

        private static void Build()
        {
            root = new GameObject("TlToyboxEditor");
            UnityEngine.Object.DontDestroyOnLoad(root);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlayOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();

            LoadPosition();
            window = Ui.Panel("Window", root.transform, position.x, position.y, Width, Height, Ui.Background);
            // 제목 줄의 바탕이자 끌기 손잡이. 버튼보다 먼저 만들어 버튼 아래에 깔리게 한다.
            AddDrag(Ui.Panel("TitleBar", window, 0, 0, Width, TitleH, Ui.Section).gameObject);
            Ui.Label(window, "TlToybox 편집기", 20, 8, 230, 36, 24);
            titleArea = Ui.Rect("Title", window, 260, 8, 790, 36);
            Button collapse = Ui.Button("Collapse", window, "접기", Width - 196, 8, 90, 36, new Action(() => SetCollapsed(!collapsed)));
            collapseLabel = collapse.GetComponentInChildren<Text>();
            Ui.Button("Close", window, "닫기", Width - 100, 8, 84, 36, new Action(Close));
            tabBar = Ui.Rect("Tabs", window, 16, ContentY, 150, 14 * 52);
            content = Ui.Rect("Content", window, ContentX, ContentY, ContentW, ContentH);
            RectTransform box = Ui.Panel("MessageBox", window, 16, Height - MessageH - 8, Width - 32, MessageH, Ui.Section);
            messageBox = box.GetComponent<Image>();
            messageLabel = Ui.Label(box, "", 12, 4, Width - 56, MessageH - 8, 18, TextAnchor.UpperLeft);
            messageLabel.color = Ui.Warn;
            messageLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageLabel.verticalOverflow = VerticalWrapMode.Truncate;

            tabs = new EditorTab[]
            {
                new AttributesTab(), new ResourcesTab(), new CurrencyTab(), new VaultTab(), new GradeTab(), new MartialTab(), new ImmortalTab(), new LuckTab(),
                new ArtTab(), new NpcTab(), new WorldTab(), new RateTab(), new PresetTab(), new SafetyTab(),
            };
        }

        private static void Render()
        {
            Confirm.OnRender();
            Ui.Clear(titleArea);
            Ui.Clear(tabBar);
            Ui.Clear(content);
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                Ui.Button("Tab" + i, tabBar, tabs[i].Title, 0, i * 52, 150, 44, new Action(() => SwitchTab(index)), true, tab == i);
                if (tabs[i].UsesTarget)
                {
                    // 편집 대상(플레이어/NPC)이 적용되는 탭의 표시. 클릭은 아래의 버튼으로 통과시킨다.
                    Ui.Panel("TargetMark", tabBar, 0, i * 52, 5, 44, Ui.Warn).GetComponent<Image>().raycastTarget = false;
                }
            }
            messageLabel.text = message;
            if (!EditService.WorldReady)
            {
                Ui.Label(content, "세이브에 진입한 뒤에 사용할 수 있습니다.", 0, 0, 800, 40, 22);
                return;
            }
            WorldUnitBase target = Target;
            Text label = Ui.Label(titleArea, "편집 대상: " + EditService.NameOf(target) + (IsTargetNpc ? " (NPC)" : " (플레이어)")
                + "    " + EditService.GradeName(target.data.unitData.propertyData.gradeID), 0, 0, 640, 36, 19);
            if (IsTargetNpc)
            {
                label.color = Ui.Warn;
            }
            Ui.Button("ToPlayer", titleArea, "플레이어로", 650, 0, 130, 36,
                new Action(() => { SetTarget(null); Say("편집 대상을 플레이어로 바꿨습니다."); }), IsTargetNpc);
            tabs[tab].Render(content);
        }

#if DEVTOOLS
        // 개발 전용: 켜져 있는 요소를 이름으로 찾는다(깊이 우선).
        private static Transform FindDeep(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }
                if (child.name == name)
                {
                    return child;
                }
                Transform found = FindDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        // 개발 전용: 자동 시험에서 입력 칸에 글을 넣는다.
        public static string DevSetInput(string name, string value)
        {
            if (window == null)
            {
                return "window not built";
            }
            Transform field = FindDeep(window, name);
            if (field == null)
            {
                return "no input named " + name;
            }
            InputField input = field.gameObject.GetComponent<InputField>();
            if (input == null)
            {
                return name + " is not an input";
            }
            input.text = value;
            return name + " = '" + value + "'";
        }

        // 개발 전용: 자동 시험에서 결과 문구를 읽는다.
        public static string DevMessage
        {
            get { return message; }
        }

        // 개발 전용: 세계 탭의 직접 지정 입력 칸과 확인 상태.
        public static string DevDateState
        {
            get
            {
                for (int i = 0; tabs != null && i < tabs.Length; i++)
                {
                    WorldTab world = tabs[i] as WorldTab;
                    if (world != null)
                    {
                        return world.DevDateState;
                    }
                }
                return "window not built";
            }
        }

        // 개발 전용: 창의 상태. 결과 문구에는 공백이 들어가므로 맨 끝에 둔다.
        public static string DevState
        {
            get
            {
                return "open=" + IsOpen + " tab=" + tab + " target=" + (targetId == null ? "player" : "npc")
                    + " mode=" + (canvas == null ? "none" : canvas.renderMode.ToString()) + " order=" + (canvas == null ? 0 : canvas.sortingOrder)
                    + " covered=" + IsCovered + " top=" + g.ui.GetLayerTopSort(UILayer.UI)
                    + " pos=" + (int)position.x + "," + (int)position.y + " collapsed=" + collapsed
                    + " message='" + message + "'";
            }
        }
#endif
    }
}
