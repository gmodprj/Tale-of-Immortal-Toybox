using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 전투에서 신기를 하나씩 쓰는 단축키와 전투 칸 클릭(05 문서 37절).
    // 게임의 "신기 기능 방출"(DataGloble.Key.battleGodEye, 기본 F)은 누르면 가진 신기 셋을 한꺼번에 발동한다.
    // 모드는 게임의 입력 처리(UnitPlayerInputCtrl)의 UseGodEye·UseDevilDemon·UsePiscesPendant를 따로 부른다(회차 key-1014a~d에서 각각 발동 확인).
    // "게임의 신기 키 끄기"를 켜면 전투 동안만 게임의 키를 None으로 바꾸고, 전투가 끝나거나 게임 설정 창이 열리면 되돌린다
    // (게임은 이 값을 사용자의 키 설정에 저장하므로 None이 저장되지 않게 한다).
    // 설정은 config.txt의 artifactkey1~3=<KeyCode>, artifactgamekey=off|on. 키 1 쌍어패, 2 호천안, 3 연요호.
    internal static class ArtifactKeyService
    {
        public static readonly KeyCode[] Keys = { KeyCode.None, KeyCode.None, KeyCode.None };
        public static bool GameKeyOff;

        private static bool loaded;
        private static bool suppressed;
        private static KeyCode savedGameKey = KeyCode.None;
        private static IntPtr clickScreen = IntPtr.Zero;

        // 키 바꾸기 화면: 다음에 누르는 키를 그 신기의 키로 잡는다. 0이면 잡지 않는다.
        public static int Capturing;

        public static string Name(int id)
        {
            return id == 1 ? "쌍어패" : id == 2 ? "호천안" : "연요호";
        }

        public static KeyCode Get(int id)
        {
            Load();
            return Keys[id - 1];
        }

        // 바꿀 수 없는 키: 마우스, 편집기 단축키, 게임의 전투 키들과 겹치는 키는 거부한다.
        public static string Set(int id, KeyCode key)
        {
            Load();
            if (key != KeyCode.None)
            {
                if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
                {
                    return "마우스 단추는 쓸 수 없습니다.";
                }
                if (key == EditorHost.Hotkey || key == KeyCode.Escape)
                {
                    return key + "는 편집기나 게임이 쓰는 키라 쓸 수 없습니다.";
                }
                string used = GameKeyUser(key);
                if (used != null)
                {
                    return key + "는 게임의 \"" + used + "\" 키라 쓸 수 없습니다.";
                }
                for (int i = 0; i < Keys.Length; i++)
                {
                    if (i != id - 1 && Keys[i] == key)
                    {
                        return key + "는 이미 " + Name(i + 1) + "의 키입니다.";
                    }
                }
            }
            KeyCode before = Keys[id - 1];
            Keys[id - 1] = key;
            Save();
            ChangeLog.Add("신기", Name(id) + " 단축키", before.ToString(), key.ToString(), Keys[id - 1] == key);
            return Name(id) + " 단축키: " + (key == KeyCode.None ? "없음" : key.ToString()) + ".";
        }

        public static string SetGameKeyOff(bool on)
        {
            Load();
            GameKeyOff = on;
            Save();
            ChangeLog.Add("신기", "게임의 신기 키 끄기", (!on).ToString(), on.ToString(), true);
            return on ? "전투 동안 게임의 신기 키(" + GameKey() + ")를 끕니다. 모드의 단축키나 칸 클릭으로 신기를 하나씩 쓰세요."
                : "게임의 신기 키를 그대로 씁니다(누르면 가진 신기가 한꺼번에 발동합니다).";
        }

        // 게임이 지금 쓰는 신기 키(끄기 중이면 되돌릴 원래 키).
        public static KeyCode GameKey()
        {
            if (suppressed)
            {
                return savedGameKey;
            }
            var key = GameKeys();
            return key == null ? KeyCode.None : key.battleGodEye;
        }

        // 개발 명령과 검사용: 게임 키의 지금 값(끄기 중이면 None).
        public static KeyCode GameKeyNow()
        {
            var key = GameKeys();
            return key == null ? KeyCode.None : key.battleGodEye;
        }

        private static DataGloble.Key GameKeys()
        {
            try
            {
                return g.data == null || g.data.dataGloble == null || g.data.dataGloble.data == null ? null : g.data.dataGloble.data.key;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GameKeyUser(KeyCode k)
        {
            var key = GameKeys();
            if (key == null)
            {
                return null;
            }
            var pairs = new List<KeyValuePair<KeyCode, string>>
            {
                new KeyValuePair<KeyCode, string>(key.battleMoveUp, "상"), new KeyValuePair<KeyCode, string>(key.battleMoveDown, "하"),
                new KeyValuePair<KeyCode, string>(key.battleMoveLeft, "좌"), new KeyValuePair<KeyCode, string>(key.battleMoveRight, "우"),
                new KeyValuePair<KeyCode, string>(key.battleSkill1, "무예/영술"), new KeyValuePair<KeyCode, string>(key.battleSkill2, "절기"),
                new KeyValuePair<KeyCode, string>(key.battleStep, "신법"), new KeyValuePair<KeyCode, string>(key.battleUltimate, "신통"),
                new KeyValuePair<KeyCode, string>(key.battleRule, "영역"), new KeyValuePair<KeyCode, string>(key.battleImmortal, "선법 방출"),
                new KeyValuePair<KeyCode, string>(key.battleProp1, "도구 1"), new KeyValuePair<KeyCode, string>(key.battleProp2, "도구 2"),
                new KeyValuePair<KeyCode, string>(key.battleProp3, "도구 3"), new KeyValuePair<KeyCode, string>(key.battleProp4, "도구 4"),
                new KeyValuePair<KeyCode, string>(key.battleProp5, "도구 5"), new KeyValuePair<KeyCode, string>(key.battleDialogue, "대화"),
            };
            foreach (var pair in pairs)
            {
                if (pair.Key == k)
                {
                    return pair.Value;
                }
            }
            return null;
        }

        // 전투 중 그 신기를 하나만 발동한다. 발동 여부(집중력, 재사용 대기)는 게임이 판단한다.
        public static string Fire(int id)
        {
            UnitCtrlPlayer pl = RateService.InBattle ? SceneType.battle.battleMap.playerUnitCtrl : null;
            if (pl == null || pl.inputCtrl == null)
            {
                return "전투 중이 아닙니다.";
            }
            if (!ArtifactService.Has(id))
            {
                return Name(id) + "을(를) 가지고 있지 않습니다.";
            }
            if (id == 1)
            {
                pl.inputCtrl.UsePiscesPendant();
            }
            else if (id == 2)
            {
                pl.inputCtrl.UseGodEye();
            }
            else
            {
                pl.inputCtrl.UseDevilDemon();
            }
            return Name(id) + " 발동을 요청했습니다.";
        }

        public static void Tick()
        {
            try
            {
                Load();
                bool inBattle = RateService.InBattle;
                UpdateGameKey(inBattle);
                if (Capturing != 0)
                {
                    Capture();
                    return;
                }
                if (!inBattle)
                {
                    return;
                }
                for (int i = 0; i < Keys.Length; i++)
                {
                    if (Keys[i] != KeyCode.None && Input.GetKeyDown(Keys[i]))
                    {
                        Fire(i + 1);
                    }
                }
                AttachClicks();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 신기 단축키 처리 오류: " + e.Message);
            }
        }

        // 모드를 내릴 때 게임의 키를 되돌린다.
        public static void OnDestroy()
        {
            Restore();
        }

        private static void UpdateGameKey(bool inBattle)
        {
            bool settingOpen = false;
            try
            {
                settingOpen = g.ui != null && g.ui.GetUI<UIGameSetting>(UIType.GameSetting) != null;
            }
            catch (Exception)
            {
            }
            bool want = GameKeyOff && inBattle && !settingOpen;
            var key = GameKeys();
            if (key == null)
            {
                return;
            }
            if (want && !suppressed)
            {
                savedGameKey = key.battleGodEye;
                key.battleGodEye = KeyCode.None;
                suppressed = true;
            }
            else if (!want && suppressed)
            {
                Restore();
            }
        }

        private static void Restore()
        {
            if (!suppressed)
            {
                return;
            }
            var key = GameKeys();
            if (key != null && key.battleGodEye == KeyCode.None)
            {
                key.battleGodEye = savedGameKey;
            }
            suppressed = false;
        }

        private static void Capture()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Capturing = 0;
                EditorWindow.Say("단축키 바꾸기를 그만뒀습니다.");
                return;
            }
            if (!Input.anyKeyDown)
            {
                return;
            }
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                if (k == KeyCode.None || (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) || k >= KeyCode.JoystickButton0)
                {
                    continue;
                }
                if (Input.GetKeyDown(k))
                {
                    int id = Capturing;
                    Capturing = 0;
                    EditorWindow.Say(Set(id, k));
                    return;
                }
            }
        }

        // 전투 화면의 쌍어패·연요호 칸에 클릭을 단다(호천안 칸은 게임의 기술 단추가 있다). 회차 click-1014a~h.
        // 쌍어패 칸(goPiscesPendant)에는 레이캐스트 대상이 없어 투명한 그림을 더해 클릭을 받는다.
        // 연요호 칸의 그림(imgPotmonHead)에는 게임의 마우스 처리기(UIEventListener, 설명 표시)가 있어 실제 클릭이 거기서 멈춘다.
        // 그래서 부모가 아니라 클릭이 도착하는 그 그림에 EventTrigger를 더한다(한 요소의 클릭 처리기는 모두 불린다).
        private static void AttachClicks()
        {
            UIBattleInfo ui = g.ui.GetUI<UIBattleInfo>(UIType.BattleInfo);
            if (ui == null || ui.info == null || ui.Pointer == clickScreen)
            {
                return;
            }
            clickScreen = ui.Pointer;
            Attach(ui.info.goPiscesPendant, 1);
            Attach(ui.info.imgPotmonHead == null ? null : ui.info.imgPotmonHead.gameObject, 3);
        }

        private static void Attach(GameObject go, int id)
        {
            if (go == null || go.GetComponent<UnityEngine.EventSystems.EventTrigger>() != null)
            {
                return;
            }
            Graphic graphic = go.GetComponent<Graphic>();
            if (graphic == null)
            {
                Image image = go.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                graphic = image;
            }
            graphic.raycastTarget = true;
            var trigger = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
            entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
            entry.callback = new UnityEngine.EventSystems.EventTrigger.TriggerEvent();
            entry.callback.AddListener(new Action<UnityEngine.EventSystems.BaseEventData>(e => Fire(id)));
            trigger.triggers.Add(entry);
        }

        private static string ConfigPath
        {
            get { return Path.Combine(ChangeLog.DataDir, "config.txt"); }
        }

        private static void Load()
        {
            if (loaded)
            {
                return;
            }
            loaded = true;
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    return;
                }
                foreach (string line in File.ReadAllLines(ConfigPath))
                {
                    string t = line.Trim();
                    for (int i = 0; i < Keys.Length; i++)
                    {
                        string head = "artifactkey" + (i + 1) + "=";
                        if (t.StartsWith(head, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                Keys[i] = (KeyCode)Enum.Parse(typeof(KeyCode), t.Substring(head.Length).Trim(), true);
                            }
                            catch (ArgumentException)
                            {
                                Keys[i] = KeyCode.None;
                            }
                        }
                    }
                    if (t.StartsWith("artifactgamekey=", StringComparison.OrdinalIgnoreCase))
                    {
                        GameKeyOff = t.Substring(16).Trim().Equals("off", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] 신기 단축키 설정을 읽지 못했습니다: " + e.Message);
            }
        }

        // 다른 줄(hotkey=, window= 등)은 그대로 두고 신기 줄만 바꾼다.
        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(ChangeLog.DataDir);
                var lines = new List<string>();
                if (File.Exists(ConfigPath))
                {
                    foreach (string line in File.ReadAllLines(ConfigPath))
                    {
                        string t = line.Trim();
                        if (!t.StartsWith("artifactkey", StringComparison.OrdinalIgnoreCase) && !t.StartsWith("artifactgamekey=", StringComparison.OrdinalIgnoreCase))
                        {
                            lines.Add(line);
                        }
                    }
                }
                for (int i = 0; i < Keys.Length; i++)
                {
                    lines.Add("artifactkey" + (i + 1) + "=" + Keys[i]);
                }
                lines.Add("artifactgamekey=" + (GameKeyOff ? "off" : "on"));
                File.WriteAllLines(ConfigPath, lines.ToArray());
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] 신기 단축키 설정을 저장하지 못했습니다: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                MelonLogger.Msg("[TlToybox] 신기 단축키 설정을 저장하지 못했습니다: " + e.Message);
            }
        }
    }
}
