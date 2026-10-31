using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 극 창의 배경 그림이 없을 때 흰 판 대신 검은 바탕으로 둔다(05 문서 95절).
    // 모드 패키지에 배경 그림이 빠지면(예: [蛋]음양쌍수 62가지, 94절) 배경 Image의 sprite가 비고 Image의 흰 색만 그려진다.
    // 대상 창: 기연 창(UIDramaFortuitous. 위에 큰 그림, 아래 글과 가로 선택지)과 큰 그림 대화 창(UIDramaDialogueBigTexture). 둘 다 배경이 imgBG1·imgBG2다.
    // 창의 Update 뒤마다 두 Image를 본다:
    //   - 그림이 비었고 보이는(알파 > 0) 밝은 색이면 그 색을 기억하고 RGB만 검게(알파는 게임의 옅어지기 연출을 따르게 둔다).
    //   - 우리가 검게 칠한 Image에 그림이 생기면, 색이 아직 우리가 칠한 검정일 때만 기억한 색으로 되돌린다(게임이 색을 바꿨으면 손대지 않는다).
    //   - 이미 어둡거나 투명한 Image(큰 그림 창의 평소 imgBG2 = 0,0,0,0)는 건드리지 않는다.
    internal static class DramaBackgroundService
    {
        private static HarmonyLib.Harmony harmony;
        private static readonly Dictionary<IntPtr, Color> saved = new Dictionary<IntPtr, Color>();

        // 개발 확인용: 꺼 두면 고치지 않는다(rdrama fix off). 배포에서는 늘 켜져 있다.
        public static bool Enabled = true;
        public static int Darkened;
        public static string LastNote = "drama bg: not hooked";

        public static void Ensure()
        {
            if (harmony != null)
            {
                return;
            }
            var instance = new HarmonyLib.Harmony("TlToybox.dramabg");
            try
            {
                Hook(instance, typeof(UIDramaFortuitous), "FortuitousPostfix");
                Hook(instance, typeof(UIDramaDialogueBigTexture), "BigTexturePostfix");
            }
            catch (Exception e)
            {
                instance.UnpatchSelf();
                LastNote = "drama bg: hook failed " + e.GetType().Name;
                MelonLogger.Msg("[TlToybox] drama background hook failed: " + e.GetType().Name + ": " + e.Message);
                return;
            }
            harmony = instance;
            LastNote = "drama bg: hooked";
        }

        private static void Hook(HarmonyLib.Harmony instance, Type window, string postfixName)
        {
            MethodInfo target = window.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            MethodInfo postfix = typeof(DramaBackgroundService).GetMethod(postfixName, BindingFlags.Static | BindingFlags.Public);
            instance.Patch(target, postfix: new HarmonyLib.HarmonyMethod(postfix));
        }

        public static void FortuitousPostfix(UIDramaFortuitous __instance)
        {
            try
            {
                if (__instance != null)
                {
                    Fix(__instance.imgBG1);
                    Fix(__instance.imgBG2);
                }
            }
            catch (Exception)
            {
                // 창의 갱신을 막지 않는다.
            }
        }

        public static void BigTexturePostfix(UIDramaDialogueBigTexture __instance)
        {
            try
            {
                if (__instance != null)
                {
                    Fix(__instance.imgBG1);
                    Fix(__instance.imgBG2);
                }
            }
            catch (Exception)
            {
            }
        }

        private static void Fix(Image img)
        {
            if (img == null)
            {
                return;
            }
            IntPtr key = img.Pointer;
            Color before;
            bool ours = saved.TryGetValue(key, out before);
            Color c = img.color;
            bool black = c.r == 0f && c.g == 0f && c.b == 0f;
            if (Enabled && img.sprite == null)
            {
                if (!black && c.a > 0f)
                {
                    // 처음이면 원래 색을 기억한다. 그 뒤 게임이 색을 다시 밝게 바꿔도 그림이 없는 동안은 다시 검게.
                    if (!ours)
                    {
                        saved[key] = c;
                        Darkened++;
                        LastNote = "drama bg: darkened " + Darkened;
                    }
                    img.color = new Color(0f, 0f, 0f, c.a);
                }
                return;
            }
            if (ours)
            {
                // 그림이 생겼거나 고치기를 껐다. 색이 아직 우리가 칠한 검정이면 기억한 색(알파는 지금 값)으로 되돌린다.
                if (black)
                {
                    img.color = new Color(before.r, before.g, before.b, c.a);
                }
                saved.Remove(key);
            }
        }

        // 세이브를 떠날 때: 기억한 Image는 창과 함께 사라지므로 비운다.
        public static void Forget()
        {
            saved.Clear();
        }
    }
}
