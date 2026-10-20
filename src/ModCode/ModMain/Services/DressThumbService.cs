using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 캐릭터 만들기 의복 목록의 빈 썸네일 채우기(05 문서 74절).
    // 목록 칸(G:srItems/Viewport/Content/<dressID>)의 그림(G:imgPart)은 원작 정적 몸 그림을 동기로 읽어 붙여서, 모드가 Fatury로만 주는 몸(예: [剑来] 2818763591의
    // 여성 244~322)은 칸이 빈다. 큰 입화는 g.res.LoadAsync(Fatury가 패치하는 길)로 읽혀 나온다(52절). 그래서 창이 열린 동안 빈 칸만 같은 길로 읽어 그림을 붙인다.
    // 원작 그림이 있는 칸은 건드리지 않는다. 읽은 그림은 (성별, 번호)마다 한 번만 요청하고 다시 쓴다.
    internal static class DressThumbService
    {
        private const int PollFrames = 30;

        private static TimerCoroutine timer;
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> requested = new HashSet<string>();
        private static readonly List<Il2CppSystem.Action<UnityEngine.Object>> handlers = new List<Il2CppSystem.Action<UnityEngine.Object>>();
        private const int RequestsPerPoll = 24;
        private static readonly HashSet<string> filledCells = new HashSet<string>();
        private static HashSet<string> emptyBefore = new HashSet<string>();
        private static string lastError;
        private static bool open;
        private static int generation;
        private static int pending;

        public static int Filled { get; private set; }
        public static int Missing { get; private set; }

        public static void Ensure()
        {
            if (timer != null)
            {
                return;
            }
            timer = g.timer.Frame(new Action(Poll), PollFrames, true);
        }

        public static void Stop()
        {
            if (timer != null)
            {
                g.timer.Stop(timer);
                timer = null;
            }
        }

        public static string Stats()
        {
            // distinct: 읽은 그림 가운데 서로 다른 텍스처 수(같은 틀을 쓰는 모드 그림이 캐시에서 하나로 겹치는지 보는 값).
            var textures = new HashSet<int>();
            foreach (Sprite s in sprites.Values)
            {
                if (s != null && s.texture != null)
                {
                    textures.Add(s.texture.GetInstanceID());
                }
            }
            return "requested=" + requested.Count + " loaded=" + sprites.Count + " distinct=" + textures.Count + " filled=" + Filled + " missing=" + Missing;
        }

        // 개발 확인용: 목록 칸 하나의 그림(G:imgPart) 상태(켜짐·색·크기·스프라이트). 칸이 없으면 그 사실을 적는다.
        public static string Describe(string cellName)
        {
            UICreatePlayer ui = g.ui.GetUI<UICreatePlayer>(UIType.CreatePlayer);
            Transform content = ui == null ? null : FindContent(ui.transform);
            Transform cell = content == null ? null : content.Find(cellName);
            Transform part = cell == null ? null : cell.Find("G:imgPart");
            UnityEngine.UI.Image image = part == null ? null : part.GetComponent<UnityEngine.UI.Image>();
            if (image == null)
            {
                return "no cell image " + cellName;
            }
            var rt = part.GetComponent<RectTransform>();
            Sprite s = image.sprite;
            return cellName + " active=" + part.gameObject.activeInHierarchy + " enabled=" + image.enabled + " alpha=" + image.color.a
                + " size=" + rt.rect.width + "x" + rt.rect.height + " pos=" + rt.anchoredPosition.x + "," + rt.anchoredPosition.y
                + " scale=" + rt.localScale.x + " parentScale=" + rt.lossyScale.x
                + " preserve=" + image.preserveAspect + " type=" + image.type + " mat=" + (image.material == null ? "null" : image.material.name)
                + " sprite=" + (s == null ? "null" : s.name + "/" + s.rect.width + "x" + s.rect.height + " pivot=" + s.pivot.x + "," + s.pivot.y + " tex=" + (s.texture == null ? "null" : s.texture.name + "/" + s.texture.width + "x" + s.texture.height));
        }

        private static void Poll()
        {
            try
            {
                UICreatePlayer ui = g.ui.GetUI<UICreatePlayer>(UIType.CreatePlayer);
                if (ui == null)
                {
                    if (open)
                    {
                        Clear();
                    }
                    return;
                }
                open = true;
                Transform content = FindContent(ui.transform);
                if (content == null || !content.gameObject.activeInHierarchy)
                {
                    return;
                }
                string sex = IsWoman(ui.transform) ? "Woman" : "Man";
                float box = BoxSize(content);
                var emptyNow = new HashSet<string>();
                int sent = 0;
                for (int i = 0; i < content.childCount; i++)
                {
                    Transform cell = content.GetChild(i);
                    int id;
                    if (!int.TryParse(cell.name, out id))
                    {
                        continue;
                    }
                    Transform part = cell.Find("G:imgPart");
                    UnityEngine.UI.Image image = part == null ? null : part.GetComponent<UnityEngine.UI.Image>();
                    if (image == null || image.sprite != null)
                    {
                        continue;
                    }
                    string key = sex + "/" + id;
                    emptyNow.Add(key);
                    // 두 번 연달아(0.5초 넘게) 빈 칸만 다룬다. 게임이 칸을 만든 직후 원작 그림을 늦게 붙이는 사이에 끼어들지 않게(리뷰 I3).
                    if (!emptyBefore.Contains(key))
                    {
                        continue;
                    }
                    Sprite sprite;
                    if (sprites.TryGetValue(key, out sprite))
                    {
                        if (sprite != null)
                        {
                            // 원작 칸은 그림 크기로 칸 그림의 크기를 맞춘다(222번: 785×794). 빈 칸은 36×65로 남아 있어 그대로 붙이면 점처럼 작다(회차 wg-1020r).
                            // 원작 칸은 그림을 상자에 맞추는 배율도 준다(222번: 0.0756 × 794 ≈ 60). 그림이 없던 칸은 배율이 0이라 같은 상자 크기로 다시 맞춘다.
                            image.sprite = sprite;
                            image.preserveAspect = true;
                            image.SetNativeSize();
                            Rect r = part.GetComponent<RectTransform>().rect;
                            float side = Mathf.Max(r.width, r.height);
                            if (side > 0f)
                            {
                                float s = box / side;
                                part.localScale = new Vector3(s, s, 1f);
                            }
                            filledCells.Add(cell.name);
                            Filled++;
                        }
                        continue;
                    }
                    // 한 번에 많이 보내면 창을 열 때 적재가 몰린다. 폴마다 몇 개씩만(리뷰 I2).
                    if (sent < RequestsPerPoll && !requested.Contains(key))
                    {
                        Request(key, "Game/Portrait/" + key.Replace("/", "/Body/"));
                        sent++;
                    }
                }
                emptyBefore = emptyNow;
            }
            catch (Exception e)
            {
                string note = e.GetType().Name + ": " + e.Message;
                if (note != lastError)
                {
                    lastError = note;
                    MelonLogger.Msg("[TlToybox] dress thumbs: " + note);
                }
            }
        }

        // 창이 닫히면 읽어 둔 그림을 놓는다(큰 입화 텍스처를 세계에서까지 붙잡지 않게. 리뷰 I2). 아직 안 온 콜백은 세대 번호로 버린다.
        private static void Clear()
        {
            open = false;
            generation++;
            sprites.Clear();
            requested.Clear();
            emptyBefore = new HashSet<string>();
            filledCells.Clear();
            lastError = null;
            if (pending == 0)
            {
                handlers.Clear();
            }
        }

        private static void Request(string key, string path)
        {
            requested.Add(key);
            int gen = generation;
            pending++;
            var handler = UnhollowerRuntimeLib.DelegateSupport.ConvertDelegate<Il2CppSystem.Action<UnityEngine.Object>>(
                new Action<UnityEngine.Object>(o =>
                {
                    pending--;
                    if (gen == generation)
                    {
                        Sprite sprite = SpriteOf(o);
                        sprites[key] = sprite;
                        if (sprite == null)
                        {
                            Missing++;
                        }
                    }
                    // 콜백이 끝나기 전에는 대리자를 놓지 않는다(게임이 부를 대상이 사라지지 않게).
                    if (pending == 0 && !open)
                    {
                        handlers.Clear();
                    }
                }));
            handlers.Add(handler);
            g.res.LoadAsync(path, handler);
        }

        // 몸 프리팹 안의 첫 그림(SpriteRenderer 또는 Image). 없거나 8×8 이하(빈 자리 그림)면 null.
        private static Sprite SpriteOf(UnityEngine.Object o)
        {
            GameObject go = o == null ? null : o.TryCast<GameObject>();
            if (go == null)
            {
                return null;
            }
            foreach (SpriteRenderer r in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (Usable(r.sprite))
                {
                    return r.sprite;
                }
            }
            foreach (UnityEngine.UI.Image img in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (Usable(img.sprite))
                {
                    return img.sprite;
                }
            }
            return null;
        }

        private static bool Usable(Sprite sprite)
        {
            return sprite != null && sprite.texture != null && sprite.texture.width > 8;
        }

        // 원작 그림이 붙은 칸에서 읽은 상자 크기(그림의 긴 변 × 배율). 못 읽으면 60(회차 wg-1020s의 222번 값).
        private static float BoxSize(Transform content)
        {
            for (int i = 0; i < content.childCount; i++)
            {
                Transform part = content.GetChild(i).Find("G:imgPart");
                UnityEngine.UI.Image image = part == null ? null : part.GetComponent<UnityEngine.UI.Image>();
                if (image == null || image.sprite == null || part.localScale.x <= 0f || filledCells.Contains(content.GetChild(i).name))
                {
                    continue;
                }
                Rect r = part.GetComponent<RectTransform>().rect;
                float side = Mathf.Max(r.width, r.height);
                if (side > 0f)
                {
                    return side * part.localScale.x;
                }
            }
            return 60f;
        }

        private static Transform FindContent(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Content" && t.parent != null && t.parent.name == "Viewport" && t.parent.parent != null && t.parent.parent.name.Contains("srItems"))
                {
                    return t;
                }
            }
            return null;
        }

        // 성별 토글(G:tglWoman_<언어>)이 켜져 있으면 여성. 화면에 보이는 것만 본다.
        private static bool IsWoman(Transform root)
        {
            foreach (UnityEngine.UI.Toggle toggle in root.GetComponentsInChildren<UnityEngine.UI.Toggle>(false))
            {
                if (toggle.name.StartsWith("G:tglWoman", StringComparison.Ordinal))
                {
                    return toggle.isOn;
                }
            }
            return false;
        }
    }
}
