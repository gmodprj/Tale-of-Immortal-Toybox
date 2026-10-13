using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 켜 둔 기능(무적, 피해 배율, 날짜 고정, 배율)을 화면 위쪽 가운데에 보인다.
    // 편집기와 별개의 캔버스에 덧그린다. 편집기를 닫았거나 게임 창이 편집기를 가렸을 때도 보여야 하기 때문이다.
    // 클릭은 받지 않는다(캔버스에 GraphicRaycaster를 붙이지 않는다).
    internal static class StatusOverlay
    {
        private const float BoxH = 34;
        private const int PollFrames = 30;

        private static GameObject root;
        private static RectTransform box;
        private static Text label;
        private static string shown = "";
        private static int frames;
        private static bool warned;

        // 켜진 기능을 이은 문구. 켜진 것이 없거나 세이브에 들어가지 않았으면 빈 글.
        public static string Describe()
        {
            if (!EditService.WorldReady)
            {
                return "";
            }
            var parts = new List<string>();
            if (BattleService.God)
            {
                parts.Add("무적");
            }
            if (BattleService.Multiplier > 1)
            {
                parts.Add("피해 x" + BattleService.Multiplier);
            }
            if (TimeService.Frozen)
            {
                parts.Add("날짜 고정");
            }
            foreach (RateService.Rate rate in RateService.All)
            {
                if (rate.On)
                {
                    parts.Add(rate.Short + " " + rate.Text);
                }
            }
            return string.Join(" · ", parts.ToArray());
        }

        // 매 프레임 불린다. 30프레임마다 한 번 상태를 읽어 표시를 맞춘다. 예외를 던지지 않는다.
        public static void Tick()
        {
            frames++;
            if (frames < PollFrames)
            {
                return;
            }
            frames = 0;
            try
            {
                Apply(Describe());
            }
            catch (Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    MelonLogger.Msg("[TlToybox] 켜 둔 기능의 표시 오류: " + e.Message);
                }
            }
        }

        private static void Apply(string text)
        {
            if (text == shown)
            {
                return;
            }
            shown = text;
            if (text.Length == 0)
            {
                if (root != null)
                {
                    root.SetActive(false);
                }
                return;
            }
            // 켜진 것이 처음 생겼을 때 만든다. 한 번도 켜지 않으면 게임에 아무것도 올리지 않는다.
            if (root == null)
            {
                Build();
            }
            root.SetActive(true);
            float width = 36 + text.Length * 19;
            box.anchoredPosition = new Vector2((1920 - width) / 2, -6);
            box.sizeDelta = new Vector2(width, BoxH);
            label.rectTransform.sizeDelta = new Vector2(width, BoxH);
            label.text = text;
        }

        private static void Build()
        {
            root = new GameObject("TlToyboxStatus");
            UnityEngine.Object.DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            box = Ui.Panel("Box", root.transform, 0, 6, 100, BoxH, new Color(0.10f, 0.09f, 0.05f, 0.85f));
            box.GetComponent<Image>().raycastTarget = false;
            label = Ui.Label(box, "", 0, 0, 100, BoxH, 18, TextAnchor.MiddleCenter);
            label.color = Ui.Warn;
        }

#if DEVTOOLS
        // 개발 전용: 지금 보이는 문구. 숨겨져 있으면 hidden.
        public static string DevState
        {
            get { return root != null && root.activeSelf ? shown : "hidden"; }
        }
#endif
    }
}
