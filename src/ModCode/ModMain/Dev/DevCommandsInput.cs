using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 개발 전용. 편집기 창이 게임의 마우스 입력(휠 확대, 지도 호버·클릭)을 삼키는지 조사하는 명령(05 문서 51절).
    //   (호출 추적은 DevCodeScan의 xrefl. MelonLoader의 XrefScanner.XrefScan은 캐시 DB 초기화에 실패하고 UsedBy는 게임을 종료시켰다 — 회차 fix-1017a)
    //   mapcam                 : 지도 카메라 배율, 지도 입력 상태, 격자 호버 UI, 마우스 위치, EventSystem의 UI 판정을 한 줄에
    //   inwatch start|read     : 매 프레임 마우스 휠·위치·UI 판정을 모아 두었다가 읽는다(창 밖의 게임에 보낸 메시지가 닿는지 본다)
    //   uipointer <x> <y>|off  : 가상 포인터. Input.mousePosition을 이 게임 좌표로 바꿔 게임과 EventSystem이 그 자리로 보게 한다(PointerGuard의 패치)
    //   uiwheel <delta> [프레임] : 가상 휠. Input.mouseScrollDelta를 그 프레임 수 동안 (0, delta)로 바꾼다
    //   uikey <KeyCode 번호> [프레임] : 가상 키. Input.GetKey(그 키)를 그 프레임 수 동안 참으로(324가 Mouse1). 0을 주면 푼다
    //   colliders              : 장면의 카메라(eventMask)와 충돌체 기반 마우스 처리기(ColliderHandler)
    // 실측(회차 fix-1017a): 게임 창에 WM_MOUSEMOVE·WM_MOUSEWHEEL을 보내도 Unity의 Input에는 닿지 않는다(위치는 실제 커서, 휠 0). 그래서 가상 입력을 쓴다.
    internal static class DevCommandsInput
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "mapcam":
                    return MapCam();
                case "inwatch":
                    return DevInputWatch.Command(p.Length > 1 ? p[1] : "read");
                case "uipointer":
                case "uiwheel":
                case "uikey":
                    return DevVirtualInput.Command(p);
                case "colliders":
                    return Colliders();
                default:
                    return null;
            }
        }

        // 장면의 카메라(eventMask)와 충돌체 기반 마우스 처리기(ColliderHandler: 지도의 호버·클릭이 물리 이벤트로 오는지 본다).
        private static string Colliders()
        {
            var sb = new StringBuilder();
            Camera[] cameras = Camera.allCameras;
            sb.Append("cameras=").Append(cameras.Length);
            for (int i = 0; i < cameras.Length; i++)
            {
                sb.Append(' ').Append(cameras[i].name).Append("/mask=").Append(cameras[i].eventMask).Append("/depth=").Append(cameras[i].depth);
            }
            int total = 0, enter = 0, click = 0, active = 0;
            var names = new StringBuilder();
            foreach (ColliderHandler handler in Resources.FindObjectsOfTypeAll<ColliderHandler>())
            {
                total++;
                if (!handler.gameObject.activeInHierarchy)
                {
                    continue;
                }
                active++;
                bool hasEnter = handler.onMouseEnter != null;
                bool hasClick = handler.onMouseUpAsButton != null;
                if (hasEnter) { enter++; }
                if (hasClick) { click++; }
                if ((hasEnter || hasClick) && names.Length < 600)
                {
                    Transform t = handler.transform;
                    names.Append(' ').Append(t.parent == null ? "" : t.parent.name + "/").Append(t.name).Append(":L").Append(handler.gameObject.layer)
                        .Append(hasEnter ? "E" : "").Append(hasClick ? "C" : "");
                }
            }
            int col2d = 0, col3d = 0;
            var colNames = new StringBuilder();
            foreach (Collider2D c in Resources.FindObjectsOfTypeAll<Collider2D>())
            {
                if (c.gameObject.activeInHierarchy && c.enabled)
                {
                    col2d++;
                    if (colNames.Length < 400) { colNames.Append(' ').Append(c.gameObject.name).Append(":L").Append(c.gameObject.layer); }
                }
            }
            foreach (Collider c in Resources.FindObjectsOfTypeAll<Collider>())
            {
                if (c.gameObject.activeInHierarchy && c.enabled)
                {
                    col3d++;
                    if (colNames.Length < 400) { colNames.Append(' ').Append(c.gameObject.name).Append(":L").Append(c.gameObject.layer).Append("(3d)"); }
                }
            }
            sb.Append(" handlers=").Append(total).Append(" active=").Append(active).Append(" withEnter=").Append(enter).Append(" withClick=").Append(click)
                .Append(" colliders2d=").Append(col2d).Append(" colliders3d=").Append(col3d)
                .Append(" guard=[").Append(PointerGuard.DevState).Append(']').Append(names).Append(" |").Append(colNames);
            return sb.ToString();
        }

        private static string MapCam()
        {
            var sb = new StringBuilder();
            SceneMap scene = SceneType.map;
            MapWorldMgr map = scene == null ? null : scene.world;
            if (map == null)
            {
                sb.Append("map=none");
            }
            else
            {
                // curCameraSize·isEnableMap은 정적 멤버다(실측: CS0176).
                sb.Append("scale=").Append(map.curCameraScale).Append(" size=").Append(MapWorldMgr.curCameraSize).Append(" ortho=").Append(map.cameraOrthoSize)
                    .Append(" max=").Append(map.maxCameraScale).Append(" enable=").Append(MapWorldMgr.isEnableMap).Append(" stopMove=").Append(map.isStopMove)
                    .Append(" stopKey=").Append(map.stopKeyMove).Append(" select=").Append(map.curSelectPoint.x).Append(',').Append(map.curSelectPoint.y)
                    .Append(" lastMouse=").Append((int)map.lastMousePosi.x).Append(',').Append((int)map.lastMousePosi.y);
            }
            CameraCtrl ctrl = scene == null ? null : scene.cameraCtrl;
            Camera cam = ctrl == null ? null : ctrl.ca;
            sb.Append(" cam=").Append(cam == null ? "none" : cam.orthographicSize + "/mask=" + cam.eventMask + "/" + cam.name);
            sb.Append(" camPos=").Append(ctrl == null ? "?" : ctrl.transform.position.x.ToString("F2") + "," + ctrl.transform.position.y.ToString("F2"));
            Vector3 mouse = Input.mousePosition;
            sb.Append(" mouse=").Append((int)mouse.x).Append(',').Append((int)(Screen.height - mouse.y)).Append(" wheel=").Append(Input.mouseScrollDelta.y)
                .Append(" focused=").Append(Application.isFocused).Append(" screen=").Append(Screen.width).Append('x').Append(Screen.height);
            try
            {
                UIMapMain main = g.ui == null ? null : g.ui.GetUI<UIMapMain>(UIType.MapMain);
                if (main != null)
                {
                    bool info = main.uiGridInfo != null && main.uiGridInfo.goGroupRoot != null && main.uiGridInfo.goGroupRoot.activeSelf;
                    bool unit = main.uiGridUnit != null && main.uiGridUnit.goGroupRoot != null && main.uiGridUnit.goGroupRoot.activeSelf;
                    sb.Append(" gridInfo=").Append(info).Append(main.uiGridInfo == null ? "" : "/" + main.uiGridInfo.point.x + "," + main.uiGridInfo.point.y)
                        .Append(" gridUnit=").Append(unit);
                }
            }
            catch (Exception e)
            {
                sb.Append(" grid=err:").Append(e.GetType().Name);
            }
            EventSystem es = EventSystem.current;
            sb.Append(" es=").Append(es == null ? "none" : es.gameObject.name + "/" + (es.currentInputModule == null ? "no-module" : es.currentInputModule.GetIl2CppType().Name)
                + "/over=" + es.IsPointerOverGameObject());
            sb.Append(" editorOver=").Append(EditorWindow.IsPointerOver).Append(" editorOpen=").Append(EditorWindow.IsOpen).Append(" guard=[").Append(PointerGuard.DevState).Append(']');
            int raycasters = 0;
            foreach (GraphicRaycaster r in Resources.FindObjectsOfTypeAll<GraphicRaycaster>())
            {
                if (r.isActiveAndEnabled)
                {
                    raycasters++;
                }
            }
            sb.Append(" raycasters=").Append(raycasters);
            return sb.ToString();
        }
    }

    // 가상 입력. PointerGuard의 패치가 Input.mousePosition·mouseScrollDelta를 돌려주기 전에 묻는다. 검사 input이 쓴다(05 문서 51절).
    internal static class DevVirtualInput
    {
        private static bool pointerOn;
        private static Vector3 pointer;
        private static float wheel;
        private static int wheelUntil = -1;
        private static KeyCode key;
        private static int keyUntil = -1;

        public static string Command(string[] p)
        {
            string error = PointerGuard.Ensure();
            if (error != null)
            {
                return error;
            }
            if (p[0] == "uipointer")
            {
                if (p[1] == "off")
                {
                    pointerOn = false;
                    return "pointer off";
                }
                pointer = new Vector3(int.Parse(p[1]), Screen.height - int.Parse(p[2]), 0f);
                pointerOn = true;
                return "pointer at " + p[1] + "," + p[2];
            }
            if (p[0] == "uikey")
            {
                key = (KeyCode)int.Parse(p[1]);
                keyUntil = key == KeyCode.None ? -1 : Time.frameCount + (p.Length > 2 ? int.Parse(p[2]) : 1);
                return "key " + key + " until frame " + keyUntil + " (now " + Time.frameCount + ")";
            }
            wheel = float.Parse(p[1]);
            wheelUntil = Time.frameCount + (p.Length > 2 ? int.Parse(p[2]) : 1);
            return "wheel " + wheel + " until frame " + wheelUntil + " (now " + Time.frameCount + ")";
        }

        public static bool Key(KeyCode asked, ref bool result)
        {
            if (asked != key || Time.frameCount > keyUntil)
            {
                return false;
            }
            result = true;
            return true;
        }

        public static bool Pointer(ref Vector3 result)
        {
            if (!pointerOn)
            {
                return false;
            }
            result = pointer;
            return true;
        }

        public static bool Wheel(ref Vector2 result)
        {
            if (Time.frameCount > wheelUntil)
            {
                return false;
            }
            result = new Vector2(0f, wheel);
            return true;
        }
    }

    // 매 프레임 마우스 입력을 모은다(EditorHostBehaviour.Update가 부른다). 창 밖에 세워 둔 게임에 보낸 휠·이동 메시지가 Input에 닿는지 보는 용도.
    internal static class DevInputWatch
    {
        private static bool on;
        private static int frames;
        private static float wheelSum;
        private static int wheelFrames;
        private static Vector3 lastPos;
        private static int posChanges;
        private static int overUiFrames;
        private static int overEditorFrames;
        private static float scaleMin = float.MaxValue, scaleMax = float.MinValue;

        public static string Command(string what)
        {
            if (what == "start")
            {
                on = true;
                frames = 0; wheelSum = 0f; wheelFrames = 0; posChanges = 0; overUiFrames = 0; overEditorFrames = 0;
                scaleMin = float.MaxValue; scaleMax = float.MinValue;
                lastPos = Input.mousePosition;
                return "watching";
            }
            string result = "frames=" + frames + " wheelSum=" + wheelSum + " wheelFrames=" + wheelFrames + " posChanges=" + posChanges
                + " lastMouse=" + (int)lastPos.x + "," + (int)(Screen.height - lastPos.y) + " overUi=" + overUiFrames + " overEditor=" + overEditorFrames
                + " scale=" + (scaleMin == float.MaxValue ? "-" : scaleMin + ".." + scaleMax);
            if (what == "stop")
            {
                on = false;
            }
            return result;
        }

        public static void Tick()
        {
            if (!on)
            {
                return;
            }
            try
            {
                frames++;
                float wheel = Input.mouseScrollDelta.y;
                if (wheel != 0f)
                {
                    wheelSum += wheel;
                    wheelFrames++;
                }
                Vector3 pos = Input.mousePosition;
                if (pos != lastPos)
                {
                    posChanges++;
                    lastPos = pos;
                }
                EventSystem es = EventSystem.current;
                if (es != null && es.IsPointerOverGameObject())
                {
                    overUiFrames++;
                }
                if (EditorWindow.IsPointerOver)
                {
                    overEditorFrames++;
                }
                SceneMap scene = SceneType.map;
                if (scene != null && scene.world != null)
                {
                    float scale = scene.world.curCameraScale;
                    scaleMin = Mathf.Min(scaleMin, scale);
                    scaleMax = Mathf.Max(scaleMax, scale);
                }
            }
            catch (Exception)
            {
                on = false;
            }
        }
    }
}
