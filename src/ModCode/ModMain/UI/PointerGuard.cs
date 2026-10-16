using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MOD_sWP6UL
{
    // 편집기 창 위의 마우스 입력(휠, 포인터)이 뒤의 게임(지도)에 새지 않게 한다(사용자 보고 2026-10-17, 05 문서 51절).
    // 실측(회차 fix-1017b·c, 네이티브 코드 해독):
    //   - 지도의 주기 처리 MapWorldMgr.OnUpdate가 Input.mouseScrollDelta를 UI 조건 없이 직접 읽어 카메라 배율(curCameraSize)에 더한다.
    //     편집기 창은 게임 UI 관리자(g.ui)에 없고, 게임도 휠 앞에서 UI를 보지 않으므로 창 위의 휠이 지도를 확대·축소한다.
    //   - 지도 클릭(Mouse0)은 EventSystem.IsPointerOverGameObject()가 참이면 건너뛴다. 편집기 캔버스에는 GraphicRaycaster가 있어 이 판정에 들어간다.
    //   - 같은 OnUpdate가 오른쪽·가운데 단추(Input.GetKey → GetKeyInt)를 누른 채 움직인 마우스 위치로 카메라를 끌어 옮긴다(UI 조건 없음).
    //   - 물리 충돌체의 OnMouseEnter/Exit/UpAsButton(ColliderHandler)은 EventSystem을 거치지 않고 카메라의 eventMask를 따른다.
    // 그래서: 지도의 OnUpdate 안에서만(InMapUpdate) 포인터가 창 위이면 휠을 0으로 돌려준다(편집기 목록의 휠 스크롤은 EventSystem이 따로 읽으므로 그대로다),
    // IsPointerOverGameObject는 창 위이면 참으로, 창 위인 동안 카메라의 eventMask를 0으로 두고 벗어나면 되돌린다.
    // 패치는 편집기를 처음 열 때 한 번 건다. 걸지 못하면 로그에 한 줄 남기고 편집기는 그대로 동작한다.
    internal static class PointerGuard
    {
        private static HarmonyLib.Harmony harmony;
        private static string failure;
        private static Camera[] masked = new Camera[0];
        private static int[] savedMasks = new int[0];
        private static bool tickWarned;

        public static string Ensure()
        {
            if (harmony != null || failure != null)
            {
                return failure;
            }
            var instance = new HarmonyLib.Harmony("TlToybox.pointer");
            try
            {
                const BindingFlags mine = BindingFlags.Static | BindingFlags.Public;
                instance.Patch(typeof(MapWorldMgr).GetMethod("OnUpdate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyLib.HarmonyMethod(typeof(PointerGuard).GetMethod("MapUpdatePrefix", mine)),
                    new HarmonyLib.HarmonyMethod(typeof(PointerGuard).GetMethod("MapUpdatePostfix", mine)));
                instance.Patch(typeof(Input).GetProperty("mouseScrollDelta").GetGetMethod(),
                    new HarmonyLib.HarmonyMethod(typeof(PointerGuard).GetMethod("ScrollPrefix", mine)));
#if DEVTOOLS
                // 개발 전용: 가상 포인터(uipointer). 배포 빌드는 마우스 위치를 건드리지 않는다.
                instance.Patch(typeof(Input).GetProperty("mousePosition").GetGetMethod(),
                    new HarmonyLib.HarmonyMethod(typeof(PointerGuard).GetMethod("MousePrefix", mine)));
#endif
                instance.Patch(typeof(EventSystem).GetMethod("IsPointerOverGameObject", Type.EmptyTypes),
                    new HarmonyLib.HarmonyMethod(typeof(PointerGuard).GetMethod("OverPrefix", mine)));
                // Input.GetKey(KeyCode)의 실제 구현. 지도의 OnUpdate는 오른쪽·가운데 단추(Mouse1·Mouse2)를 이것으로 읽어 끌기 이동을 한다.
                instance.Patch(typeof(Input).GetMethod("GetKeyInt", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    new HarmonyLib.HarmonyMethod(typeof(PointerGuard).GetMethod("KeyPrefix", mine)));
            }
            catch (Exception e)
            {
                instance.UnpatchSelf();
                failure = "창 위 입력 막기를 게임에 연결하지 못했습니다(" + e.GetType().Name + ": " + e.Message + ")";
                MelonLogger.Msg("[TlToybox] " + failure);
                return failure;
            }
            harmony = instance;
            return null;
        }

        // 지도의 주기 처리(MapWorldMgr.OnUpdate) 안인지. 그 안에서만 게임의 휠 읽기를 가로챈다. 편집기의 EventSystem 처리는 그대로 둔다.
        public static bool InMapUpdate { get; private set; }

        // 포인터가 창 위인지. 매 프레임 Tick이 갱신한다(카메라 eventMask의 기준). 패치 안에서는 그때그때 다시 본다.
        public static bool Over { get; private set; }

        public static bool MapUpdatePrefix()
        {
            InMapUpdate = true;
            return true;
        }

        public static void MapUpdatePostfix()
        {
            InMapUpdate = false;
        }

        public static bool ScrollPrefix(ref Vector2 __result)
        {
            try
            {
#if DEVTOOLS
                bool virtualWheel = DevVirtualInput.Wheel(ref __result);
#endif
                if (InMapUpdate && EditorWindow.IsPointerOver)
                {
                    __result = Vector2.zero;
                    return false;
                }
#if DEVTOOLS
                if (virtualWheel)
                {
                    return false;
                }
#endif
            }
            catch (Exception)
            {
            }
            return true;
        }

#if DEVTOOLS
        public static bool MousePrefix(ref Vector3 __result)
        {
            try
            {
                if (DevVirtualInput.Pointer(ref __result))
                {
                    return false;
                }
            }
            catch (Exception)
            {
            }
            return true;
        }
#endif

        // 창 위에서 오른쪽·가운데 단추를 누른 채 끌어도 지도가 움직이지 않게. 매개변수 이름이 다를 수 있어 자리(__0)로 받는다.
        public static bool KeyPrefix(KeyCode __0, ref bool __result)
        {
            try
            {
#if DEVTOOLS
                bool virtualKey = DevVirtualInput.Key(__0, ref __result);
#endif
                if (InMapUpdate && (__0 == KeyCode.Mouse1 || __0 == KeyCode.Mouse2) && EditorWindow.IsPointerOver)
                {
                    __result = false;
                    return false;
                }
#if DEVTOOLS
                if (virtualKey)
                {
                    return false;
                }
#endif
            }
            catch (Exception)
            {
            }
            return true;
        }

        public static bool OverPrefix(ref bool __result)
        {
            try
            {
                if (EditorWindow.IsPointerOver)
                {
                    __result = true;
                    return false;
                }
            }
            catch (Exception)
            {
            }
            return true;
        }

        // 매 프레임. 창 위인 동안 모든 카메라의 eventMask를 0으로 두어 충돌체의 OnMouse* 이벤트를 막고, 벗어나면 되돌린다. 예외를 밖으로 던지지 않는다.
        public static void Tick()
        {
            try
            {
                bool over = EditorWindow.IsPointerOver;
                Over = over;
                if (!over)
                {
                    Restore();
                    return;
                }
                if (masked.Length > 0)
                {
                    return;
                }
                Camera[] cameras = Camera.allCameras;
                var keep = new System.Collections.Generic.List<Camera>();
                var masks = new System.Collections.Generic.List<int>();
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera cam = cameras[i];
                    if (cam == null || cam.eventMask == 0)
                    {
                        continue;
                    }
                    keep.Add(cam);
                    masks.Add(cam.eventMask);
                    cam.eventMask = 0;
                }
                masked = keep.ToArray();
                savedMasks = masks.ToArray();
            }
            catch (Exception e)
            {
                if (!tickWarned)
                {
                    tickWarned = true;
                    MelonLogger.Msg("[TlToybox] 창 위 입력 막기 오류: " + e.GetType().Name + ": " + e.Message);
                }
            }
        }

        // 바꿔 둔 카메라의 eventMask를 되돌린다. 편집기를 닫거나 모드가 내려갈 때도 불린다.
        public static void Release()
        {
            Restore();
            Over = false;
        }

        private static void Restore()
        {
            for (int i = 0; i < masked.Length; i++)
            {
                try
                {
                    if (masked[i] != null && masked[i].eventMask == 0)
                    {
                        masked[i].eventMask = savedMasks[i];
                    }
                }
                catch (Exception)
                {
                }
            }
            masked = new Camera[0];
            savedMasks = new int[0];
        }

        // 개발 명령과 검사가 읽는 상태.
        public static string DevState
        {
            get { return "over=" + Over + " inMap=" + InMapUpdate + " masked=" + masked.Length + " patched=" + (harmony != null) + (failure == null ? "" : " failure=" + failure); }
        }
    }
}
