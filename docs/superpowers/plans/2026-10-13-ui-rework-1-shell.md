# 화면 구조 정리 1차 — 검증 도구, 창의 틀, 탭 분리 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 편집기 창을 "창의 틀 + 탭별 클래스"로 나누고, 창에 앞뒤 순서(게임 창이 위에 뜸)·끌어서 이동·접기·세 줄 결과 영역·켜 둔 기능 표시를 넣는다. 탭 안의 동작은 지금과 같게 둔다.

**Architecture:** `UI/EditorWindow.cs`(1,371줄)를 창의 틀로 다시 쓰고, 탭의 그리는 코드는 `UI/Tabs/`의 클래스 아홉 개로 **옮기기만** 한다(버튼 동작 뒤에 탭 내용을 지우고 다시 그리는 방식 유지). 편집기 캔버스는 게임과 같은 카메라 방식으로 바꿔 게임 화면 사이에 끼운다. 검증 스크립트는 버튼을 좌표가 아니라 이름으로 찾도록 먼저 바꾼다.

**Tech Stack:** C# 7.3 / .NET Framework 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4(Unhollower), uGUI. 검증은 PowerShell 7 스크립트와 `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-13-ui-rework-design.md`

**이 계획이 다루는 스펙의 범위:** 7절 작업 순서의 1~2번(5.2, 5.3, 5.4의 `Push`/`Pop`을 뺀 부분, 5.5의 AmountBar 가운데 "용도별로 따로 기억", 5.9)과 6절의 1~6, 10, 15, 16, 19~23번. 나머지(공통 부품 다섯 개와 AmountBar의 Enter 적용, 탭의 하위 화면 쌓기, 확인 대기의 통일, 탭별 개선, 가방 칸 한 번에 지정, 변경 이력 전체 보기)는 이 계획을 구현한 뒤 2차 계획에서 다룬다. 한 문서에 담기에는 코드가 너무 많고, 2차의 코드는 여기서 만든 틀에 맞춰 써야 하기 때문이다.

## Global Constraints

- 대상: 귀곡팔황 v1.2.113.259, 게임 폴더 `E:\Games\Tale of Immortal`, 모드 ID `sWP6UL`, 네임스페이스 `MOD_sWP6UL`.
- `src/ModCode/ModMain/Services/`는 고치지 않는다.
- 참조를 추가하지 않는다. `bin\Release`에는 `MOD_sWP6UL.dll` 하나만 있어야 한다.
- 개발 구성과 배포 구성(`-p:DevTools=false`) 모두 경고 0, 오류 0으로 빌드돼야 한다. `Dev\`의 코드를 배포 코드가 부르면 배포 구성이 깨진다(`#if DEVTOOLS`로 감싼 것은 된다).
- 창 크기 1500x1000, 탭 9개의 이름과 순서(능력치·자원·경지·공법·운명·기예·NPC·세계·안전)는 그대로다.
- 버튼과 입력 칸의 이름을 유지한다. 특히 `Day1`·`Day5`·`Day10`, `SkipMonth`, `Freeze`, `DateApply`, `DateYear`·`DateMonth`·`DateDay`, `BagMinus`·`BagPlus`, `God`, `Multiplier`, `Close`, `Tab0`~`Tab8`.
- 브랜치: `develop`에서 `feat/ui-rework-1`을 만들어 작업하고 `git merge --no-ff`로 병합한다. `main`·`develop`에 직접 커밋하지 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 게임 검증은 `tools/DevRemote/session.ps1`의 회차 안에서만 한다. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다. `-Begin`은 "WORLD READY", `-End`는 "SAVE RESTORED AND IDENTICAL"이 나와야 한다.
- 코드 모양은 주변과 같게: 중괄호는 항상 쓰고 줄을 바꾼다, 들여쓰기 4칸, 주석은 한국어로 "왜"를 적는다.
- PowerShell: 스크립트는 `pwsh`(7)로 실행한다. 배열 매개변수가 있는 스크립트(`mod.ps1`, `rc.ps1`)는 `pwsh -File`이 아니라 `&`로 부른다. 변수 이름은 대소문자를 구분하지 않는다. 새 함수 이름은 `Get-Alias`로 겹치지 않는지 확인한다.
- 문서에는 어셈블리나 게임에서 실제로 확인한 이름만 적고, 확인하지 못한 것은 "미확인"으로 적는다.

## Review Focus

스펙이 말하지 않았지만 쓰다 보면 만나는 상황이다. 줄마다 그 상황을 확인하는 검사를 맡은 Task에 넣었다.

1. `config.txt`의 `window=` 값이 깨져 있거나 화면 밖이다(손으로 고침, 다른 해상도에서 저장) → 범위 안의 가장 가까운 자리에 열리고 경고가 없다. (Task 4, `move-corrupt`)
2. 편집 대상으로 잡은 NPC가 없어졌다(다른 세이브에 들어감, 사망) → 플레이어로 돌아가고 제목 줄이 맞게 바뀌며 예외가 없다. (Task 2, `tabs`)
3. 접힌 상태에서 창을 끌거나, 접은 채 닫았다가 다시 연다 → 창이 움직이고, 다시 열면 펼쳐져 있으며 탭이 옮긴 자리에서 눌린다. (Task 4, `move`)
4. `Insert`를 빠르게 여러 번 누르거나, 게임 화면이 가린 상태와 섞어 누른다 → 열림·닫힘이 어긋나지 않고 예외가 없다. (Task 3, `order`)
5. 전투로 장면이 바뀐다 → 편집기가 열리고 눌리며, 켜 둔 기능의 표시가 전투 중에도 보인다. (Task 3 `battle`, Task 5 `status-battle`)

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `src/ModCode/ModMain/Dev/DevCommandsUi.cs` | 새 파일 | 화면 검증용 개발 명령 |
| `src/ModCode/ModMain/Dev/DevCommands.cs` | 수정 | 위 명령으로 넘기는 줄 추가 |
| `tools/research/check-lib.ps1` | 새 파일 | 검증 스크립트가 함께 쓰는 함수(명령 보내기, 이름으로 누르기, PASS/FAIL) |
| `tools/research/ui-check.ps1` | 새 파일 | 화면의 게임 내 검증. Task마다 부분이 늘어난다 |
| `tools/research/datebag-check.ps1` | 수정 | 고정 좌표를 이름 찾기로, 공통 함수를 `check-lib.ps1`로 |
| `src/ModCode/ModMain/UI/EditorWindow.cs` | 다시 씀 | 창의 틀 |
| `src/ModCode/ModMain/UI/Tabs/EditorTab.cs` | 새 파일 | 탭의 기반 클래스 |
| `src/ModCode/ModMain/UI/Tabs/{Attributes,Resources,Grade,Martial,Luck,Art,Npc,World,Safety}Tab.cs` | 새 파일 | 탭마다 한 파일(기존 코드를 옮김) |
| `src/ModCode/ModMain/UI/Widgets/AmountBar.cs` | 새 파일 | 수량 줄. 쓰는 곳마다 값을 따로 기억 |
| `src/ModCode/ModMain/UI/StatusOverlay.cs` | 새 파일 | 켜 둔 기능 표시 |
| `src/ModCode/ModMain/UI/EditorHost.cs` | 수정 | 매 프레임 `EditorWindow.Tick`, `StatusOverlay.Tick` 호출 |
| `docs/05-editor-v0.1.md`, `docs/06-handoff.md` | 수정 | 26절 추가, 인계 문서 갱신 |

프로젝트 파일(`ModMain.csproj`)은 폴더 아래의 `.cs`를 모두 컴파일하므로 고치지 않는다.

**기존 코드의 줄 번호**는 모두 `develop`(커밋 `f2d5092`)의 `src/ModCode/ModMain/UI/EditorWindow.cs`(1,371줄) 기준이다. 옮기기 전에 `git show f2d5092:src/ModCode/ModMain/UI/EditorWindow.cs > <임시 파일>`로 옛 파일을 따로 떠 두고 그 파일의 줄 번호로 본다.

## 검증 회차의 공통 절차

아래 Task들이 "회차"라고 부르는 것은 다음 순서다. 저장소 뿌리(`E:\TlToybox`)에서 실행한다.

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin <이름>     # 마지막 줄이 WORLD READY
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part <부분>       # 줄마다 PASS/FAIL, 마지막 줄 FAILS=0
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart           # 필요할 때만
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End <이름>       # SAVE RESTORED AND IDENTICAL
```

- 회차 이름은 한 번만 쓸 수 있다(`.devremote\save-backup-<이름>`이 남는다). 다시 돌릴 때는 `-b`, `-c`를 붙인다.
- 검사가 FAIL이거나 스크립트가 중간에 멈춰도 반드시 `-End`로 끝낸 뒤 고친다. 게임이 켜져 있는 동안에는 DLL을 바꿀 수 없다.
- 예상하지 못한 창에 막히면 `& tools\DevRemote\win.ps1 -Do shot -Out .devremote\x.png -Scale 0.5`로 캡처해 무엇이 떠 있는지부터 본다.
- 버튼이 반응하지 않으면 `E:\Games\Tale of Immortal\MelonLoader\Latest.log`의 "trampoline" 줄을 본다.

---

### Task 1: 검증 도구 — 이름으로 찾는 개발 명령과 검증 스크립트

화면을 고치기 전에 검증 도구부터 바꾼다. 이 Task가 끝나면 **지금 화면에서** 새 방식의 검사가 모두 PASS여야 한다. 그래야 다음 Task에서 FAIL이 나왔을 때 화면 탓인지 도구 탓인지 가를 수 있다.

**Files:**
- Create: `src/ModCode/ModMain/Dev/DevCommandsUi.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs:63` (`DevCommandsTime.Execute` 호출 앞)
- Create: `tools/research/check-lib.ps1`
- Create: `tools/research/ui-check.ps1`
- Modify: `tools/research/datebag-check.ps1`

**Interfaces:**
- Consumes: 편집기의 뿌리 오브젝트 이름 `TlToyboxEditor`, 기존 개발 명령 `editor`, `ui set <이름> <값>`.
- Produces (개발 명령, 출력 모양):
  - `uipos <이름>` → `<실제 이름> at <x>,<y> size=<w>,<h>` 또는 `no element named <이름>`. 이름 끝의 `*`는 "이 글자로 시작".
  - `uitext <이름>` → `<실제 이름> text='<글>'`
  - `uifocus <이름>` → `<실제 이름> focused`
  - `uidrag <x1> <y1> <x2> <y2> [steps]` → `hit=<이름> handler=<이름> dragged <dx>,<dy> in <n> steps`
  - `uicanvas`, `uilist`, `uiopen <화면>`, `uiclose <화면>`
- Produces (`check-lib.ps1`의 함수): `Cmd`, `Rc`, `Key`, `Shot`, `Check`, `ModWarnings`, `Traps`, `Pos`, `IsAt`, `Press`, `Finish`.

- [ ] **Step 1: 작업 브랜치를 만든다**

```powershell
git -C E:\TlToybox switch -c feat/ui-rework-1 develop
```

- [ ] **Step 2: `Dev/DevCommandsUi.cs`를 만든다**

```csharp
using System;
using System.Reflection;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 개발 전용. 편집기 화면의 검증 명령. tools/research/check-lib.ps1이 이 명령들의 출력 모양에 의존한다.
    //   uipos <이름>   : 편집기 안에서 그 이름의 요소를 찾아 가운데의 게임 좌표를 돌려준다. 이름 끝의 *는 "이 글자로 시작"이다
    //   uitext <이름>  : 그 요소의 글(입력 칸이면 입력된 글, 아니면 그 아래 첫 Text)
    //   uifocus <이름> : 그 이름의 입력 칸을 고르고 입력 상태로 만든다
    //   uidrag x1 y1 x2 y2 [steps] : 끌기 이벤트를 보낸다(실제 마우스가 만드는 것과 같은 이벤트 경로)
    //   uicanvas / uilist / uiopen <화면> / uiclose <화면> : 게임 화면의 정렬 값을 읽고, 화면을 열고 닫는다
    internal static class DevCommandsUi
    {
        private const string EditorName = "TlToyboxEditor";

        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "uipos":
                    return Pos(p[1]);
                case "uitext":
                    return TextOf(p[1]);
                case "uifocus":
                    return Focus(p[1]);
                case "uidrag":
                    return Drag(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]), p.Length > 5 ? int.Parse(p[5]) : 4);
                case "uicanvas":
                    return CanvasInfo();
                case "uilist":
                    return OpenList();
                case "uiopen":
                    return Open(p[1]);
                case "uiclose":
                    g.ui.CloseUI(TypeOf(p[1]), true);
                    return "closed " + p[1];
                default:
                    return null;
            }
        }

        private static bool Matches(string actual, string wanted)
        {
            return wanted.EndsWith("*") ? actual.StartsWith(wanted.Substring(0, wanted.Length - 1)) : actual == wanted;
        }

        // 켜져 있는 요소만 깊이 우선으로 찾는다. 같은 이름이 여럿이면 먼저 만든 것이 나온다.
        private static Transform FindDeep(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }
                if (Matches(child.name, name))
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

        private static Transform Find(string name)
        {
            GameObject root = GameObject.Find(EditorName);
            return root == null ? null : FindDeep(root.transform, name);
        }

        private static string Pos(string name)
        {
            Transform found = Find(name);
            if (found == null)
            {
                return "no element named " + name;
            }
            RectTransform rect = found.GetComponent<RectTransform>();
            Canvas canvas = GameObject.Find(EditorName).GetComponent<Canvas>();
            // 덧그리기 방식에서는 세계 좌표가 곧 화면 좌표다. 카메라 방식에서는 그 카메라로 바꾼다.
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Rect r = rect.rect;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(new Vector3(r.center.x, r.center.y, 0f)));
            return found.name + " at " + Mathf.RoundToInt(screen.x) + "," + Mathf.RoundToInt(Screen.height - screen.y)
                + " size=" + Mathf.RoundToInt(r.width) + "," + Mathf.RoundToInt(r.height);
        }

        private static string TextOf(string name)
        {
            Transform found = Find(name);
            if (found == null)
            {
                return "no element named " + name;
            }
            InputField input = found.GetComponent<InputField>();
            if (input != null)
            {
                return found.name + " text='" + input.text + "'";
            }
            Text text = found.GetComponentInChildren<Text>();
            return found.name + " text='" + (text == null ? "" : text.text) + "'";
        }

        private static string Focus(string name)
        {
            Transform found = Find(name);
            if (found == null)
            {
                return "no element named " + name;
            }
            InputField input = found.GetComponent<InputField>();
            if (input == null)
            {
                return found.name + " is not an input";
            }
            EventSystem.current.SetSelectedGameObject(found.gameObject);
            input.ActivateInputField();
            return found.name + " focused";
        }

        // 게임 좌표(왼쪽 위 기준) (x1,y1)에서 (x2,y2)까지 끄는 이벤트를 그 자리의 끌기 처리기에 보낸다.
        private static string Drag(int x1, int y1, int x2, int y2, int steps)
        {
            EventSystem es = EventSystem.current;
            if (es == null)
            {
                return "no EventSystem";
            }
            var pointer = new PointerEventData(es);
            var from = new Vector2(x1, Screen.height - y1);
            var to = new Vector2(x2, Screen.height - y2);
            pointer.position = from;
            pointer.pressPosition = from;
            pointer.button = PointerEventData.InputButton.Left;
            var results = new Il2CppSystem.Collections.Generic.List<RaycastResult>();
            es.RaycastAll(pointer, results);
            if (results.Count == 0)
            {
                return "no hit at " + x1 + "," + y1;
            }
            GameObject hit = results[0].gameObject;
            GameObject handler = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
            if (handler == null)
            {
                return "hit=" + hit.name + " -> no drag handler";
            }
            pointer.pointerDrag = handler;
            pointer.dragging = true;
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.beginDragHandler);
            Vector2 step = (to - from) / steps;
            for (int i = 0; i < steps; i++)
            {
                pointer.delta = step;
                pointer.position = pointer.position + step;
                ExecuteEvents.Execute(handler, pointer, ExecuteEvents.dragHandler);
            }
            ExecuteEvents.Execute(handler, pointer, ExecuteEvents.endDragHandler);
            return "hit=" + hit.name + " handler=" + handler.name + " dragged " + (x2 - x1) + "," + (y2 - y1) + " in " + steps + " steps";
        }

        private static string Describe(Canvas c)
        {
            if (c == null)
            {
                return "null";
            }
            return c.gameObject.name + " mode=" + c.renderMode + " override=" + c.overrideSorting + " order=" + c.sortingOrder
                + " plane=" + c.planeDistance + " cam=" + (c.worldCamera == null ? "none" : c.worldCamera.name) + " goLayer=" + c.gameObject.layer;
        }

        private static string CanvasInfo()
        {
            var sb = new StringBuilder();
            sb.Append("screen=").Append(Screen.width).Append('x').Append(Screen.height);
            sb.Append("\n  game canvas: ").Append(Describe(g.ui.canvas));
            sb.Append("\n  UI layer topSort=").Append(g.ui.GetLayerTopSort(UILayer.UI)).Append(" interval=").Append(UIMgr.UIIntervalSorting);
            GameObject editor = GameObject.Find(EditorName);
            sb.Append("\n  editor: ").Append(editor == null ? "not open" : Describe(editor.GetComponent<Canvas>()));
            return sb.ToString();
        }

        private static string OpenList()
        {
            var sb = new StringBuilder("open UI:");
            foreach (var pair in g.ui.allUI)
            {
                var list = pair.Value;
                for (int i = 0; list != null && i < list.Count; i++)
                {
                    UIBase ui = list[i];
                    if (ui == null)
                    {
                        continue;
                    }
                    sb.Append("\n  ").Append(pair.Key).Append(" layer=").Append(ui.uiType == null ? "?" : ui.uiType.uiLayer.ToString())
                        .Append(" sort=").Append(ui.sortingOrder).Append(" active=").Append(ui.gameObject.activeInHierarchy);
                }
            }
            return sb.ToString();
        }

        private static UIType.UITypeBase TypeOf(string name)
        {
            PropertyInfo prop = typeof(UIType).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            if (prop == null)
            {
                throw new ArgumentException("no UIType." + name);
            }
            return (UIType.UITypeBase)prop.GetValue(null, null);
        }

        private static string Open(string name)
        {
            if (name == "CheckPopup")
            {
                g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData("시험", "편집기와의 앞뒤 순서를 보는 시험 창입니다.", 2,
                    new Action(() => MelonLogger.Msg("[TlToybox][cmd] check popup yes")), new Action(() => MelonLogger.Msg("[TlToybox][cmd] check popup no")));
                return "opened CheckPopup";
            }
            g.ui.OpenUI(TypeOf(name));
            return "opened " + name;
        }
    }
}
```

- [ ] **Step 3: `Dev/DevCommands.cs`의 `Execute`에서 새 명령으로 넘긴다**

`string time = DevCommandsTime.Execute(p);` 줄 바로 앞에 넣는다.

```csharp
            string screen = DevCommandsUi.Execute(p);
            if (screen != null)
            {
                return screen;
            }
```

- [ ] **Step 4: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 5: `tools/research/check-lib.ps1`을 만든다**

```powershell
# 게임 내 검증 스크립트가 함께 쓰는 함수. datebag-check.ps1과 ui-check.ps1이 점(.)으로 불러 쓴다.
# 줄마다 PASS/FAIL이 나오고 Finish가 마지막 줄 FAILS=<수>를 낸다. "SHOT <파일>"은 눈으로 확인할 캡처다.
$ErrorActionPreference = 'Stop'
$tools = Join-Path (Split-Path -Parent $PSScriptRoot) 'DevRemote'
$work = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.devremote'
$data = Join-Path $env:USERPROFILE 'AppData\LocalLow\guigugame\guigubahuang\TlToybox'
$log = 'E:\Games\Tale of Immortal\MelonLoader\Latest.log'
$script:fails = 0
if (-not (Test-Path (Join-Path $work 'session.name'))) { throw 'session.ps1 -Begin으로 연 회차 안에서만 실행합니다(세이브와 모드 파일을 바꾸는 시험이다).' }

function Cmd([string]$c, [int]$wait = 600) { (& "$tools\mod.ps1" -Cmd $c -WaitMs $wait) -join "`n" }
function Rc([string]$c, [int]$wait = 800) { (& "$tools\rc.ps1" -Cmd $c -NoShot -WaitMs $wait) -join "`n" }
# 게임 창에 키 하나를 보낸다. 0x2D는 Insert, 0x0D는 Enter.
function Key([int]$vk, [int]$wait = 500) {
  & "$tools\win.ps1" -Do key -Vk $vk | Out-Null
  Start-Sleep -Milliseconds $wait
}
function Shot([string]$name) {
  $file = Join-Path $work "check-$name.png"
  & "$tools\win.ps1" -Do shot -Out $file -Scale 0.6 | Out-Null
  "SHOT $file"
}
# 모드가 로그에 남긴 경고·오류 줄. 로그의 한글은 UTF-8이 아니어서(CP949) 문구로 찾을 수 없다.
# 정상 줄(Init, Destroy, state, 단축키 안내)이 아닌 "[TlToybox] " 줄을 고른다. 명령과 변경 이력은 "[TlToybox][...]"이라 걸리지 않는다.
function ModWarnings {
  Get-Content -LiteralPath $log | Where-Object { $_ -match '\[TlToybox\] ' -and $_ -notmatch '\[TlToybox\] (Init|Destroy|state: )' -and $_ -notmatch ': (Insert|F2|F3)\s*$' }
}
# 버튼 콜백 안의 예외는 화면에 나오지 않고 로그의 "trampoline" 줄에만 남는다. 그 줄의 수.
function Traps { @(Get-Content -LiteralPath $log | Where-Object { $_ -match 'trampoline' }).Count }
function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
# 편집기에서 그 이름의 요소를 찾아 가운데의 게임 좌표를 돌려준다. 이름 끝의 *는 "이 글자로 시작"이다.
function Pos([string]$name) {
  $o = Cmd "uipos $name" 100
  if ($o -notmatch '-> (\S+) at (-?\d+),(-?\d+) size=') { throw "편집기에서 요소를 찾지 못했습니다: $name ($o)" }
  [pscustomobject]@{ name = $Matches[1]; x = [int]$Matches[2]; y = [int]$Matches[3] }
}
# 그 이름의 요소가 있고, 그 자리의 클릭이 그 요소로 가는지(가려지지 않았는지).
function IsAt([string]$name) {
  $o = Cmd "uipos $name" 100
  if ($o -notmatch '-> (\S+) at (-?\d+),(-?\d+) size=') { return $false }
  $real = $Matches[1]; $x = $Matches[2]; $y = $Matches[3]
  (Rc "hit $x $y" 150) -match "/$([regex]::Escape($real))\s*$"
}
# 그 이름의 버튼을 찾아 그 자리를 누른다. 다른 것이 눌렸거나 버튼이 꺼져 있으면 멈춘다.
function Press([string]$name, [int]$wait = 800) {
  $p = Pos $name
  $o = Rc "click $($p.x) $($p.y)" $wait
  if ($o -notmatch "Button\.onClick: $([regex]::Escape($p.name))\s*$") { throw "버튼이 눌리지 않았습니다: $name ($o)" }
  $o
}
function Finish {
  "FAILS=$script:fails"
  if ($script:fails -gt 0) { exit 1 }
  exit 0
}
```

- [ ] **Step 6: 새 함수 이름이 별칭과 겹치지 않는지 확인한다**

```powershell
Get-Alias Key, Traps, Pos, IsAt, Press, Finish -ErrorAction SilentlyContinue
```

Expected: 아무것도 나오지 않는다. 나오면 그 함수의 이름을 바꾸고 이 계획의 뒤쪽에서도 같은 이름으로 바꾼다.

- [ ] **Step 7: `tools/research/ui-check.ps1`을 만든다(`base` 부분)**

```powershell
# 편집기 화면의 게임 내 검증(docs/superpowers/specs/2026-10-13-ui-rework-design.md 6절).
# tools\DevRemote\session.ps1 -Begin으로 게임이 떠서 세계에 들어가 있어야 한다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

# 탭마다 그 탭에만 있는 버튼 하나. 탭이 제대로 그려졌는지 보는 데 쓴다.
$tabProbe = @('Restore', 'MoneyAdd', 'FillExp', 'SkillAddOpen', 'LuckAddOpen', 'ArtLearnAll', 'NpcRefresh', 'Freeze', 'BackupNow')

switch ($Part) {
  'base' {
    $traps = Traps
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    for ($i = 0; $i -lt 9; $i++) {
      Press "Tab$i" | Out-Null
      Check "tab $i shows $($tabProbe[$i])" (IsAt $tabProbe[$i])
    }
    Press 'Tab0' | Out-Null
    Check 'uitext reads a label' ((Cmd 'uitext Restore') -match "text='자원 회복'")
    Check 'a name prefix finds an element' ((Cmd 'uipos Plus*') -match '-> Plus\d+ at ')
    Check 'unknown name is reported' ((Cmd 'uipos NoSuchThing') -match 'no element named')
    Press 'Close' | Out-Null
    Check 'editor closed' ((Cmd 'uipos Tab0') -match 'no element named')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
```

- [ ] **Step 8: `tools/research/datebag-check.ps1`을 이름 찾기로 바꾼다**

(1) 5~27행(`$ErrorActionPreference`부터 `Check` 함수의 닫는 중괄호까지)을 다음 한 줄로 바꾼다. `State`부터 `DateInput`까지의 함수는 그대로 둔다.

```powershell
. (Join-Path $PSScriptRoot 'check-lib.ps1')
```

(2) 맨 끝의 세 줄(`"FAILS=$script:fails"`, `if ($script:fails -gt 0) { exit 1 }`, `exit 0`)을 `Finish` 한 줄로 바꾼다.

(3) 편집기의 좌표를 쓰는 줄을 바꾼다. 게임 화면의 좌표(`400 1000`, `884 531`, `1037 531`, `1042 47`, `959 629`, `1170 679`, `120 860`, `1722 160`)는 그대로 둔다.

| 옛 줄 | 새 줄 |
|---|---|
| `Rc 'click 151 550' \| Out-Null` (2곳) | `Press 'Tab7' \| Out-Null` |
| `Check 'Day1 button is where expected' ((Rc 'hit 415 542' 300) -match 'Day1')` | `Check 'Day1 button is where expected' (IsAt 'Day1')` |
| `Rc 'click 415 542' \| Out-Null` (2곳) | `Press 'Day1' \| Out-Null` |
| `Rc 'click 799 590' \| Out-Null` (7곳) | `Press 'DateApply' \| Out-Null` |
| `Rc 'click 1502 90' \| Out-Null` (3곳) | `Press 'Close' \| Out-Null` |
| `Rc 'click 754 542' \| Out-Null` | `Press 'SkipMonth' \| Out-Null` |
| `Check 'Freeze button is where expected' ((Rc 'hit 366 494' 300) -match 'Freeze')` | `Check 'Freeze button is where expected' (IsAt 'Freeze')` |
| `Rc 'click 366 494' \| Out-Null` (2곳) | `Press 'Freeze' \| Out-Null` |
| `Rc 'click 151 238' \| Out-Null` | `Press 'Tab1' \| Out-Null` |
| `Check 'BagPlus button is where expected' ((Rc 'hit 1501 234' 300) -match 'BagPlus')` | `Check 'BagPlus button is where expected' (IsAt 'BagPlus')` |
| `Rc 'click 1501 234' \| Out-Null` | `Press 'BagPlus' \| Out-Null` |
| `Rc 'click 1425 234' \| Out-Null` | `Press 'BagMinus' \| Out-Null` |

바꾼 뒤 편집기 좌표가 남지 않았는지 확인한다.

```powershell
Select-String -Path E:\TlToybox\tools\research\datebag-check.ps1 -Pattern "'(click|hit) (151|415|799|1502|754|366|1501|1425) "
```

Expected: 아무것도 나오지 않는다.

(4) 사용자의 세이브에 편집기 추가 칸이 이미 있다(`bag-slots.txt`에 +90). 추가 칸이 없다고 가정한 검사 두 곳을 고친다. 회차의 `-End`가 `bag-slots.txt`를 되돌리므로 시험 안에서 0으로 만들어도 된다.

`'base'` 부분:

```powershell
    Check 'grid is readable' ($g.max -gt 0) $g.add
```

`'bag'` 부분의 첫 `Check` 다음, `Cmd 'gridfill 600 5' 4000 | Out-Null` 앞에 넣는다:

```powershell
    # 사용자의 세이브에 편집기 추가 칸이 이미 있을 수 있다. 0에서 시작하도록 걷어 낸다(회차의 -End가 파일을 되돌린다).
    if ((Cmd 'bagstat') -notmatch 'extra=(\d+)') { throw 'bagstat 출력을 읽지 못했습니다.' }
    if ([int]$Matches[1] -gt 0) { Cmd "bag -$($Matches[1])" | Out-Null }
```

- [ ] **Step 9: 회차 1 — `base`와 `time`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t1a
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part base
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part time
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t1a
```

Expected: 두 검사 모두 마지막 줄 `FAILS=0`. `-End`는 `SAVE RESTORED AND IDENTICAL`.

- [ ] **Step 10: 회차 2 — `freeze`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t1b
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part freeze
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t1b
```

Expected: `FAILS=0`. 수련할 칸이 가까이 없으면 `SKIP frozen training` 한 줄이 나올 수 있다(실패가 아니다).

- [ ] **Step 11: 회차 3 — `bag`, `bag-restart`, `bag-corrupt`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t1c
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part bag
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part bag-restart
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part bag-corrupt
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t1c
Get-Content "$env:USERPROFILE\AppData\LocalLow\guigugame\guigubahuang\TlToybox\bag-slots.txt"
```

Expected: 세 부분 모두 `FAILS=0`. `-End`가 `BAG SLOTS RESTORED`를 내고, 마지막 명령이 `kh3o73=90` 줄을 보여 준다(사용자의 값이 돌아왔다).

- [ ] **Step 12: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/Dev/DevCommandsUi.cs src/ModCode/ModMain/Dev/DevCommands.cs tools/research/check-lib.ps1 tools/research/ui-check.ps1 tools/research/datebag-check.ps1
git -C E:\TlToybox commit -m @'
chore: 화면 검증을 좌표가 아니라 이름으로 — 개발 명령, 공통 함수, 검증 스크립트

편집기의 요소를 이름으로 찾아 자리를 돌려주는 개발 명령(uipos 등)을 넣고,
datebag-check.ps1의 고정 좌표를 이름 찾기로 바꿨다. 화면 배치를 바꿔도
검증 스크립트를 고칠 필요가 없다. 사용자의 세이브에 가방 추가 칸이 이미
있어도 가방 검사가 0에서 시작하도록 고쳤다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 2: 창의 틀과 탭 분리

`EditorWindow.cs`를 창의 틀로 다시 쓰고, 탭의 그리는 코드를 `UI/Tabs/`의 클래스 아홉 개로 옮긴다. 탭 안의 동작은 바꾸지 않는다. 눈에 보이는 변화는 넷이다: 제목과 편집 대상이 한 줄이 되고, `플레이어로` 버튼이 제목 줄로 가고, 결과 영역이 세 줄이 되고, 수량이 쓰는 곳마다 따로 기억된다.

**Files:**
- Create: `src/ModCode/ModMain/UI/Tabs/EditorTab.cs`
- Create: `src/ModCode/ModMain/UI/Widgets/AmountBar.cs`
- Create: `src/ModCode/ModMain/UI/Tabs/AttributesTab.cs`, `ResourcesTab.cs`, `GradeTab.cs`, `MartialTab.cs`, `LuckTab.cs`, `ArtTab.cs`, `NpcTab.cs`, `WorldTab.cs`, `SafetyTab.cs`
- Rewrite: `src/ModCode/ModMain/UI/EditorWindow.cs`
- Modify: `src/ModCode/ModMain/UI/EditorHost.cs:72` (`BagService.Tick();` 다음)
- Modify: `src/ModCode/ModMain/Dev/DevCommandsUi.cs` (`uistate`, `uitarget`)
- Modify: `tools/research/ui-check.ps1` (`tabs` 부분)

**Interfaces:**
- Consumes: Task 1의 개발 명령과 `check-lib.ps1`.
- Produces:
  - `EditorWindow`: `const float Width, Height, TitleH, ContentX, ContentY, ContentW, MessageH, ContentH`; `bool IsOpen`; `WorldUnitBase Player`, `Target`; `bool IsTargetNpc`; `string SelectedNpcId`(필드); `void SetTarget(string unitId)`; `void SwitchTab(int index)`; `void Toggle()`, `Close()`, `Say(string text)`, `ClearMessage()`, `Refresh()`, `Tick()`. 개발 전용: `string DevSetInput(string name, string value)`, `string DevMessage`, `string DevDateState`, `string DevState`.
  - `EditorTab`(추상): `string Title`, `bool UsesTarget`, `void Render(RectTransform area)`, `protected abstract void Draw()`, `virtual void OnOpen()`, `OnShow()`, `OnHide()`, `OnTargetChanged()`; `protected RectTransform content`; `protected const float ContentW, ContentH`; `protected static` `Player`, `Target`, `Say(string)`, `Refresh()`, `Redraw()`, `Shorten(string, int)`.
  - `AmountBar`: `const int Max`; `int Get(string key)`; `float Build(Transform parent, string key, float x, float y)`. 열쇠는 `attr`, `martialExp`, `move`, `intim`, `money`, `item`. 요소 이름은 `<열쇠><수>`(예: `attr100`), `<열쇠>Input`, `<열쇠>Apply`.
  - 개발 명령 `uistate` → `open=<bool> tab=<n> target=<player|npc> mode=<RenderMode> order=<n> message='<글>'`, `uitarget <unitId|player>`, `uisay <횟수>` → `said <글자 수> chars`.

- [ ] **Step 1: 빨간 불 — `uistate`, `uitarget` 명령을 먼저 넣는다**

`Dev/DevCommandsUi.cs`의 `switch`에서 `case "uicanvas":` 앞에 넣는다.

```csharp
                case "uistate":
                    return EditorWindow.DevState;
                case "uitarget":
                    // 편집 대상을 단위 ID로 바꾼다. 없는 ID를 주면 편집기가 플레이어로 돌아가는지 볼 수 있다.
                    EditorWindow.SetTarget(p[1] == "player" ? null : p[1]);
                    EditorWindow.Refresh();
                    return EditorWindow.DevState;
                case "uisay":
                    {
                        // 긴 결과 문구가 결과 영역 안에서 줄바꿈되는지 캡처로 보기 위한 것. 12글자를 주어진 횟수만큼 잇는다.
                        var sb = new StringBuilder();
                        for (int i = 0; i < int.Parse(p[1]); i++)
                        {
                            sb.Append("가나다라마 바사아자차 ");
                        }
                        EditorWindow.Say(sb.ToString());
                        return "said " + sb.Length + " chars";
                    }
```

파일 머리의 주석에 한 줄을 보탠다: `//   uistate / uitarget <단위 ID|player> / uisay <횟수> : 편집기의 상태를 읽고, 편집 대상을 바꾸고, 긴 결과 문구를 띄운다`

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
```

Expected: 실패. `'EditorWindow'에 'DevState'에 대한 정의가 포함되어 있지 않습니다`(CS0117)와 `SetTarget`, `Refresh`에 대한 같은 오류.

- [ ] **Step 2: 옛 파일을 따로 떠 둔다**

```powershell
git -C E:\TlToybox show f2d5092:src/ModCode/ModMain/UI/EditorWindow.cs | Set-Content -Encoding utf8 "$env:TEMP\EditorWindow.old.cs"
(Get-Content "$env:TEMP\EditorWindow.old.cs").Count
```

Expected: `1371`. 아래의 줄 번호는 이 파일 기준이다. (git이 줄바꿈을 CRLF로 바꾼다. 여러 줄을 한 번에 치환하려면 먼저 `\r`을 지운다.)

- [ ] **Step 3: `UI/Tabs/EditorTab.cs`를 만든다**

```csharp
using UnityEngine;

namespace MOD_sWP6UL
{
    // 편집기 탭의 기반. 탭은 실행 중 한 번 만들어지고 자기 상태(선택, 검색어, 쪽)를 필드로 갖는다.
    // 버튼 동작 뒤에는 Say나 Refresh를 불러 내용을 다시 그린다(틀이 내용 영역을 지우고 Draw를 부른다).
    internal abstract class EditorTab
    {
        protected const float ContentW = EditorWindow.ContentW;
        protected const float ContentH = EditorWindow.ContentH;

        // 내용을 그릴 영역. 좌표는 이 영역의 왼쪽 위가 원점이다.
        protected RectTransform content;

        public abstract string Title { get; }

        // 편집 대상(플레이어 또는 NPC)에 적용되는 탭인지. 탭 줄에 표시가 붙는다.
        public virtual bool UsesTarget
        {
            get { return false; }
        }

        public void Render(RectTransform area)
        {
            content = area;
            Draw();
        }

        protected abstract void Draw();

        // 편집기를 열 때마다(모든 탭)
        public virtual void OnOpen()
        {
        }

        // 이 탭으로 들어올 때, 이 탭을 떠날 때
        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {
        }

        // 편집 대상이 바뀌었을 때(모든 탭). 대상에 딸린 선택을 지운다.
        public virtual void OnTargetChanged()
        {
        }

        protected static WorldUnitBase Player
        {
            get { return EditorWindow.Player; }
        }

        protected static WorldUnitBase Target
        {
            get { return EditorWindow.Target; }
        }

        // 결과 문구를 보이고 다시 그린다.
        protected static void Say(string text)
        {
            EditorWindow.Say(text);
        }

        // 결과 문구는 그대로 두고 다시 그린다.
        protected static void Refresh()
        {
            EditorWindow.Refresh();
        }

        // 결과 문구를 지우고 다시 그린다.
        protected static void Redraw()
        {
            EditorWindow.ClearMessage();
            EditorWindow.Refresh();
        }

        protected static string Shorten(string text, int max)
        {
            return text.Length <= max ? text : text.Substring(0, max) + "…";
        }
    }
}
```

- [ ] **Step 4: `UI/Widgets/AmountBar.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 수량을 고르는 줄: 1/10/100/1000 버튼과 직접 입력 칸.
    // 값은 쓰는 곳(열쇠)마다 따로 기억한다. 저장하지 않으므로 게임을 다시 켜면 기본값이다.
    //   열쇠: attr(능력치 증감), martialExp(숙련 경험치), move(순간 이동 칸 수), intim(호감도 증감), money(영석), item(아이템 지급 수량)
    internal static class AmountBar
    {
        public const int Max = 999999;

        private static readonly int[] Amounts = { 1, 10, 100, 1000 };
        private static readonly Dictionary<string, int> values = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> typed = new Dictionary<string, string>();

        public static int Get(string key)
        {
            int value;
            return values.TryGetValue(key, out value) ? value : (key == "item" ? 1 : 10);
        }

        // 수량 줄을 그리고 오른쪽 끝의 x를 돌려준다. 요소의 이름은 열쇠로 시작한다(attr10, attrInput, attrApply).
        public static float Build(Transform parent, string key, float x, float y)
        {
            int current = Get(key);
            for (int i = 0; i < Amounts.Length; i++)
            {
                int amount = Amounts[i];
                Ui.Button(key + amount, parent, amount.ToString(), x + i * 66, y, 62, 38,
                    new Action(() => { typed.Remove(key); values[key] = amount; EditorWindow.ClearMessage(); EditorWindow.Refresh(); }), true, current == amount);
            }
            float inputX = x + Amounts.Length * 66 + 6;
            string text;
            if (!typed.TryGetValue(key, out text))
            {
                text = Array.IndexOf(Amounts, current) < 0 ? current.ToString() : "";
            }
            Ui.Input(key + "Input", parent, text, "직접 입력", inputX, y, 130, 38, new Action<string>(v => typed[key] = v), true);
            Ui.Button(key + "Apply", parent, "적용", inputX + 136, y, 70, 38, new Action(() =>
            {
                string entered;
                int value;
                typed.TryGetValue(key, out entered);
                if (!int.TryParse(entered, out value) || value < 1 || value > Max)
                {
                    EditorWindow.Say("수량은 1~" + Max + " 사이의 숫자로 입력하세요.");
                    return;
                }
                values[key] = value;
                EditorWindow.ClearMessage();
                EditorWindow.Refresh();
            }));
            return inputX + 136 + 70;
        }
    }
}
```

- [ ] **Step 5: `UI/EditorWindow.cs`를 통째로 바꾼다**

```csharp
using System;
using MelonLoader;
using UnityEngine;
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
                tabs[i].OnTargetChanged();
            }
        }

        public static void SwitchTab(int index)
        {
            tabs[tab].OnHide();
            tab = index;
            message = "";
            tabs[tab].OnShow();
            Render();
        }

        public static void Toggle()
        {
            if (IsOpen)
            {
                Close();
                return;
            }
            Open();
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
                if (!IsOpen || flash <= 0f)
                {
                    return;
                }
                flash = Mathf.Max(0f, flash - Time.unscaledDeltaTime);
                messageBox.color = Color.Lerp(Ui.Section, FlashColor, flash / FlashSeconds);
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

            window = Ui.Panel("Window", root.transform, 60, 60, Width, Height, Ui.Background);
            // 제목 줄의 바탕. 버튼보다 먼저 만들어 버튼 아래에 깔리게 한다.
            Ui.Panel("TitleBar", window, 0, 0, Width, TitleH, Ui.Section);
            Ui.Label(window, "TlToybox 편집기", 20, 8, 230, 36, 24);
            titleArea = Ui.Rect("Title", window, 260, 8, 790, 36);
            Ui.Button("Close", window, "닫기", Width - 100, 8, 84, 36, new Action(Close));
            tabBar = Ui.Rect("Tabs", window, 16, ContentY, 150, 9 * 52);
            content = Ui.Rect("Content", window, ContentX, ContentY, ContentW, ContentH);
            RectTransform box = Ui.Panel("MessageBox", window, 16, Height - MessageH - 8, Width - 32, MessageH, Ui.Section);
            messageBox = box.GetComponent<Image>();
            messageLabel = Ui.Label(box, "", 12, 4, Width - 56, MessageH - 8, 18, TextAnchor.UpperLeft);
            messageLabel.color = Ui.Warn;
            messageLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageLabel.verticalOverflow = VerticalWrapMode.Truncate;

            tabs = new EditorTab[]
            {
                new AttributesTab(), new ResourcesTab(), new GradeTab(), new MartialTab(), new LuckTab(),
                new ArtTab(), new NpcTab(), new WorldTab(), new SafetyTab(),
            };
        }

        private static void Render()
        {
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
            get { return tabs == null ? "window not built" : ((WorldTab)tabs[7]).DevDateState; }
        }

        // 개발 전용: 창의 상태. 결과 문구에는 공백이 들어가므로 맨 끝에 둔다.
        public static string DevState
        {
            get
            {
                return "open=" + IsOpen + " tab=" + tab + " target=" + (targetId == null ? "player" : "npc")
                    + " mode=" + (canvas == null ? "none" : canvas.renderMode.ToString()) + " order=" + (canvas == null ? 0 : canvas.sortingOrder)
                    + " message='" + message + "'";
            }
        }
#endif
    }
}
```

- [ ] **Step 6: 탭 아홉 개를 만든다 — 옛 코드를 옮긴다**

탭마다 아래의 뼈대를 쓰고, "본문" 표가 가리키는 옛 줄을 그대로 옮긴 뒤 **공통 치환**과 그 탭의 **개별 수정**을 적용한다. 옛 코드를 여기에 다시 적지 않는 이유는 1,200줄이 글자 그대로 옮겨지기 때문이다. 옮긴 뒤의 동작이 같아야 한다.

모든 파일의 머리:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
```

**공통 치환** (옮긴 본문 전체에):

| 옛 코드 | 새 코드 |
|---|---|
| 메서드·필드 앞의 `private static` | `private` (아래에서 `static`으로 남긴다고 적은 것은 제외) |
| `message = ""; Render();` (한 줄에 붙어 있는 것) | `Redraw();` |
| 그 밖의 `Render();` | `Refresh();` |
| `Height - ContentY - 56` | `ContentH` |
| `selectedNpcId` | `EditorWindow.SelectedNpcId` |
| 혼자 있는 `Close();` | `EditorWindow.Close();` |

치환 뒤 `message`라는 낱말은 `ResourcesTab.ResetItemList`의 한 곳만 남는다(개별 수정에서 바꾼다).

**(1) `AttributesTab.cs`**

```csharp
    // 능력치 탭: 게임 인물 화면의 다섯 분류로 저장값을 편집한다.
    internal sealed class AttributesTab : EditorTab
    {
        private int attributeCategory;
        private int selectedAttribute = -1;
        private string attributeInput = "";

        public override string Title
        {
            get { return "능력치"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public override void OnTargetChanged()
        {
            attributeCategory = 0;
            selectedAttribute = -1;
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            int step = AmountBar.Get("attr");
            // 본문: 옛 252~301행
        }

        // 본문: 옛 304~326행 (RenderAttributeInput)
    }
}
```

개별 수정:

- 옛 297행 `float right = AmountBar("Step", 110, footer, step, v => step = v);` → `float right = AmountBar.Build(content, "attr", 110, footer);`
- 옛 299행(`Ui.Button("ToPlayer", …)`)은 지운다. 이 버튼은 제목 줄로 갔다.

**(2) `ResourcesTab.cs`**

```csharp
    // 자원 탭: 영석, 아이템 지급 표, 가방 칸.
    internal sealed class ResourcesTab : EditorTab
    {
        private static readonly int[] ItemPageSizes = { 10, 20, 50 };

        private string itemQuery = "";
        private string appliedItemQuery = "";
        private int itemPage;
        private int itemPageSizeIndex;
        private float itemScrollY;
        private float itemSubScrollY;
        private string itemDetail = "";
        private Text itemDetailLabel;
        private int itemMainType = -1;
        private int itemSubType = -1;

        public override string Title
        {
            get { return "자원"; }
        }

        // 본문: 옛 370~376행 (ResetItemList)

        protected override void Draw()
        {
            // 본문: 옛 380~527행
        }

        // 본문: 옛 530~533행 (SubWidth), 535~538행 (Detail). 둘은 private static으로 둔다.
    }
}
```

개별 수정:

- `ResetItemList` 안의 `message = "";` → `EditorWindow.ClearMessage();`
- 옛 382행 `float contentH = Height - ContentY - 56;` → `float contentH = ContentH;` 그 다음 줄에 `int moneyAmount = AmountBar.Get("money");`와 `int itemAmount = AmountBar.Get("item");`을 넣는다.
- 옛 388행 → `float moneyRight = AmountBar.Build(content, "money", 340, 7);`
- 옛 391행 → `AmountBar.Build(content, "item", 340, 51);`

**(3) `GradeTab.cs`**

```csharp
    // 경지 탭: 수련치 채우기, 소경지 돌파, 대경지 돌파 재료와 게임의 돌파 화면.
    internal sealed class GradeTab : EditorTab
    {
        public override string Title
        {
            get { return "경지"; }
        }

        protected override void Draw()
        {
            // 본문: 옛 542~591행
        }
    }
}
```

개별 수정: 없음(공통 치환의 `Close();` 한 곳).

**(4) `MartialTab.cs`**

```csharp
    // 공법 탭: 익힌 공법의 옵션·숙련 경험치·장착·망각, 기술 추가.
    internal sealed class MartialTab : EditorTab
    {
        private string martialId;       // 선택한 공법
        private int martialPage;
        private int replaceIndex = -1;  // 옵션 교체 중인 자리. -1이면 교체 화면이 아니다
        private int replacePage;
        private string martialPending;  // 망각 확인 중인 공법
        private bool skillAdding;       // 기술 추가 화면
        private int skillKind;
        private int skillPage;
        private int skillGrade;
        private int skillPending;       // 강제 추가 확인 중인 기술 번호
        private string skillQuery = "";
        private string appliedSkillQuery = "";

        private static int Step
        {
            get { return AmountBar.Get("martialExp"); }
        }

        public override string Title
        {
            get { return "공법"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public override void OnTargetChanged()
        {
            martialId = null;
            replaceIndex = -1;
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            // 본문: 옛 672~774행
        }

        // 본문: 옛 594~668행 (RenderSkillAdd), 777~813행 (RenderPrefixPicker)
    }
}
```

개별 수정:

- 옛 736행 `AmountBar("Step", 700, box + 6, step, v => step = v);` → `AmountBar.Build(content, "martialExp", 700, box + 6);`
- 옛 758행은 두 문장이 한 줄에 붙어 있다(`…color = Ui.Dim;            List<MartialService.PrefixView> prefixes = …`). 두 줄로 나눈다.

**(5) `LuckTab.cs`**

```csharp
    // 운명 탭: 선천·후천운명의 추가·교체·삭제, 역천개명(경지별) 지정.
    internal sealed class LuckTab : EditorTab
    {
        private int luckPage;
        private int pendingLuckRemove;
        private int luckReplaceIndex = -1;  // 교체할 선천운명 자리. -1이면 후천운명 추가
        private bool luckPicking;
        private int luckType = 1;
        private int luckPickPage;
        private string luckQuery = "";
        private string appliedLuckQuery = "";
        private bool fateOpen;              // 역천개명 화면
        private int fatePickGrade;          // 후보를 고르는 중인 대경지. 0이면 목록 화면
        private int fatePickPage;
        private string fateQuery = "";
        private string appliedFateQuery = "";

        public override string Title
        {
            get { return "운명"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public override void OnTargetChanged()
        {
            luckPicking = false;
            luckPage = 0;
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            // 본문: 옛 896~961행
        }

        // 본문: 옛 820~853행 (RenderFate), 855~892행 (RenderFatePicker), 964~1021행 (RenderLuckPicker)
    }
}
```

개별 수정: 없음.

**(6) `ArtTab.cs`**

```csharp
    // 기예 탭: 게임 기예 화면의 여섯 분류. 서적을 골라 배우게 하거나 배운 기록을 지운다.
    internal sealed class ArtTab : EditorTab
    {
        private int artKind;
        private int artPage;
        private float artScrollY;
        private int artPending;   // 잊기·강제 추가 확인 중인 서적 번호
        private string artQuery = "";
        private string appliedArtQuery = "";

        public override string Title
        {
            get { return "기예"; }
        }

        protected override void Draw()
        {
            // 본문: 옛 1026~1121행
        }
    }
}
```

개별 수정: 없음(공통 치환의 `Height - ContentY - 56` 한 곳).

**(7) `NpcTab.cs`**

```csharp
    // NPC 탭: 가까운 NPC 목록, 이름 검색, 호감도, 편집 대상 지정.
    internal sealed class NpcTab : EditorTab
    {
        private const int NpcRows = 8;

        private int npcPage;
        private string npcQuery = "";
        private string appliedNpcQuery = "";
        private List<WorldUnitBase> npcCache;

        private static int Step
        {
            get { return AmountBar.Get("intim"); }
        }

        public override string Title
        {
            get { return "NPC"; }
        }

        // 편집기를 열 때마다 목록을 새로 만든다(그 사이에 NPC가 움직였을 수 있다).
        public override void OnOpen()
        {
            npcCache = null;
        }

        protected override void Draw()
        {
            // 본문: 옛 1275~1327행
        }

        // 본문: 옛 328~338행 (RenderNpcSearch), 1249~1266행 (BuildNpcCache),
        //       1268~1271행 (Distance, private static으로 둔다), 1330~1339행 (ChangeIntim)
    }
}
```

개별 수정:

- 옛 1296행(`Ui.Button("EditNpc", …)`)의 콜백을 바꾼다. 다른 탭의 상태를 직접 지우던 것을 `SetTarget`이 알리게 한다.

```csharp
                Ui.Button("EditNpc", content, "능력치 편집", 716, 54, 190, 38, new Action(() =>
                {
                    EditorWindow.SetTarget(EditorWindow.SelectedNpcId);
                    EditorWindow.SwitchTab(0);
                    Say("편집 대상을 NPC로 바꿨습니다.");
                }));
```

- 옛 1297행 `AmountBar("Step", 716, 8, step, v => step = v);` → `AmountBar.Build(content, "intim", 716, 8);`

**(8) `WorldTab.cs`**

```csharp
    // 세계 탭: 순간 이동, 날짜(고정·실제 진행·직접 지정), 전투 보조.
    internal sealed class WorldTab : EditorTab
    {
        private readonly string[] dateInput = { "", "", "" };  // 직접 지정: 연, 월, 일
        private bool datePending;   // 직접 지정을 한 번 더 눌러야 하는 상태
        private bool dateEdited;    // 입력 칸을 사용자가 고쳤는지. 고치지 않았으면 현재 날짜를 보여 준다

        private static int Step
        {
            get { return AmountBar.Get("move"); }
        }

        public override string Title
        {
            get { return "세계"; }
        }

        public override void OnOpen()
        {
            ResetDateInput();
        }

        public override void OnShow()
        {
            ResetDateInput();
        }

        // 직접 지정의 입력 칸을 현재 날짜로 되돌리고 확인 상태를 푼다. 편집기를 열 때, 이 탭으로 들어올 때, 날짜가 바뀌는 동작 뒤에 부른다.
        // 그러지 않으면 예전 날짜가 입력 칸에 남아, 일부만 고쳐 적용했을 때 엉뚱한 날짜로 간다.
        private void ResetDateInput()
        {
            dateEdited = false;
            datePending = false;
        }

        protected override void Draw()
        {
            // 본문: 옛 1126~1147행
        }

        // 본문: 옛 1150~1247행 (RenderTimeBox)

#if DEVTOOLS
        // 개발 전용: 직접 지정 입력 칸과 확인 상태.
        public string DevDateState
        {
            get { return "input=" + dateInput[0] + "-" + dateInput[1] + "-" + dateInput[2] + " pending=" + datePending; }
        }
#endif
    }
}
```

개별 수정:

- 옛 1130행 `AmountBar("Step", 700, 8, step, v => step = v);` → `AmountBar.Build(content, "move", 700, 8);`

**(9) `SafetyTab.cs`**

```csharp
    // 안전 탭: 세이브 백업, 변경 이력.
    internal sealed class SafetyTab : EditorTab
    {
        private const int HistoryRows = 14;

        public override string Title
        {
            get { return "안전"; }
        }

        protected override void Draw()
        {
            // 본문: 옛 1343~1367행
        }
    }
}
```

개별 수정: 없음.

- [ ] **Step 7: `EditorHost.cs`의 `Update`에서 창의 주기 처리를 부른다**

`BagService.Tick();` 다음 줄에 넣는다.

```csharp
            EditorWindow.Tick();
```

- [ ] **Step 8: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
Get-ChildItem E:\TlToybox\src\ModCode\ModMain\bin\Release -Filter *.dll | Select-Object Name
```

Expected: 둘 다 "경고 0개 / 오류 0개", `bin\Release`에 `MOD_sWP6UL.dll` 하나. 오류가 나면 대개 옮기다 빠뜨린 치환이다(`Render`, `message`, `step`, `selectedNpcId`가 남았는지 본다).

- [ ] **Step 9: 옮기다 빠뜨린 것이 없는지 낱말로 확인한다**

```powershell
Select-String -Path E:\TlToybox\src\ModCode\ModMain\UI\Tabs\*.cs -Pattern '\bRender\(\);|\bmessage\b|private static void Render|Height - ContentY' | Select-Object Filename, LineNumber, Line
(Select-String -Path E:\TlToybox\src\ModCode\ModMain\UI\Tabs\*.cs -Pattern 'Ui\.(Button|Input)\("').Count
(Select-String -Path "$env:TEMP\EditorWindow.old.cs" -Pattern 'Ui\.(Button|Input)\("').Count
```

Expected: 첫 명령은 아무것도 내지 않는다. 둘째는 **110**, 셋째는 **113**이다. 옛 파일의 113개 가운데 탭 밖에 있던 것은 148행(`Close`), 191행(`Tab`), 299행(`ToPlayer`, 지웠다)의 셋뿐이다(`AmountBar` 함수 안의 셋과 날짜 입력 칸은 이름이 따옴표로 시작하지 않아 이 수에 들지 않는다). 둘째가 110이 아니면 버튼이나 입력 칸을 빠뜨렸거나 두 번 옮긴 것이다.

- [ ] **Step 10: `ui-check.ps1`에 `tabs` 부분을 넣는다**

`'base' { … }` 블록 다음에 넣는다.

```powershell
  'tabs' {
    $traps = Traps
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    $s = Cmd 'uistate'
    Check 'state is readable' ($s -match 'open=True tab=\d target=player ') $s
    Check 'title shows the target' ((Cmd 'uitext Title') -match '편집 대상: .+\(플레이어\)')
    $p = Pos 'ToPlayer'
    Check 'ToPlayer is off while the target is the player' ((Rc "click $($p.x) $($p.y)" 300) -match 'not interactable')
    for ($i = 0; $i -lt 9; $i++) {
      Press "Tab$i" | Out-Null
      Check "tab $i is selected" ((Cmd 'uistate') -match "tab=$i ")
      Check "tab $i shows $($tabProbe[$i])" (IsAt $tabProbe[$i])
    }

    # 탭 안의 하위 화면
    Press 'Tab3' | Out-Null; Press 'SkillAddOpen' | Out-Null
    Check 'skill add screen opens' (IsAt 'SkillBack')
    Press 'SkillBack' | Out-Null
    Check 'skill add screen closes' (IsAt 'SkillAddOpen')
    Press 'Tab4' | Out-Null; Press 'LuckAddOpen' | Out-Null
    Check 'luck picker opens' (IsAt 'LuckBack')
    Press 'LuckBack' | Out-Null; Press 'FateOpen' | Out-Null
    Check 'fate screen opens' (IsAt 'FateBack')
    Press 'FateBack' | Out-Null
    Check 'luck list is back' (IsAt 'LuckAddOpen')

    # 수량은 쓰는 곳마다 따로 기억한다 (스펙 6절 10번)
    Press 'Tab0' | Out-Null; Press 'attr100' | Out-Null
    Check 'attribute step is 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'Tab7' | Out-Null
    Check 'move step is still 10' ((Cmd 'uitext MoveUp') -match "text='y - 10'")
    Press 'move1000' | Out-Null
    Check 'move step is 1000' ((Cmd 'uitext MoveUp') -match "text='y - 1000'")
    Press 'Tab6' | Out-Null; Press 'Pick0' | Out-Null
    Check 'intim step is still 10' ((Cmd 'uitext IntimPlus') -match "text='\+ 10'")
    Press 'Tab0' | Out-Null
    Check 'attribute step is still 100' ((Cmd 'uitext Plus*') -match "text='\+ 100'")
    Press 'attr10' | Out-Null; Press 'Tab7' | Out-Null; Press 'move10' | Out-Null

    # 결과 영역 (스펙 6절 20번은 캡처로 본다)
    Press 'Tab0' | Out-Null; Press 'Restore' | Out-Null
    Check 'result line shows the outcome' ((Cmd 'uistate') -match "message='자원 회복: ")
    Press 'Tab1' | Out-Null
    Check 'tab change clears the result line' ((Cmd 'uistate') -match "message=''")
    Check 'a long result is accepted' ((Cmd 'uisay 30') -match 'said 360 chars')
    Shot 'long-message'
    Press 'Tab1' | Out-Null

    # 편집 대상: NPC로 바꿨다가 제목 줄의 버튼으로 되돌린다 (스펙 6절 15번)
    Press 'Tab6' | Out-Null; Press 'EditNpc' | Out-Null
    $s = Cmd 'uistate'
    Check 'target is the NPC and the first tab is shown' ($s -match 'tab=0 target=npc ') $s
    Check 'title shows the NPC' ((Cmd 'uitext Title') -match '\(NPC\)')
    Press 'ToPlayer' | Out-Null
    Check 'back to the player' ((Cmd 'uistate') -match 'target=player ' -and (Cmd 'uitext Title') -match '\(플레이어\)')

    # Review Focus 2: 대상으로 잡은 NPC가 없어지면 플레이어로 돌아간다
    $s = Cmd 'uitarget no-such-unit'
    Check 'a missing target falls back to the player' ($s -match 'target=player ' -and (Cmd 'uitext Title') -match '\(플레이어\)') $s
    Press 'Tab3' | Out-Null
    Check 'target tabs still draw after the fallback' (IsAt 'SkillAddOpen')

    Shot 'tabs'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
```

- [ ] **Step 11: 회차 1 — `time`과 `tabs`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t2a
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part tabs
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part time
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t2a
```

Expected: 둘 다 `FAILS=0`. `.devremote\check-long-message.png`에서 결과 문구가 결과 영역 안에서 줄바꿈되고 창 밖으로 나가지 않는지, `.devremote\check-tabs.png`에서 제목 줄이 한 줄이고 능력치·공법·운명 탭의 왼쪽에 띠가 있는지 눈으로 확인한다.

- [ ] **Step 12: 회차 2, 3 — `freeze`, `bag`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t2b
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part freeze
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t2b
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t2c
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part bag
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t2c
```

Expected: 둘 다 `FAILS=0`.

- [ ] **Step 13: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI src/ModCode/ModMain/Dev/DevCommandsUi.cs tools/research/ui-check.ps1
git -C E:\TlToybox commit -m @'
refactor: 편집기 창을 틀과 탭별 클래스로 나눔

EditorWindow.cs 한 파일에 있던 탭 아홉 개의 그리는 코드를 UI/Tabs의 클래스로
옮겼다. 탭 안의 동작은 그대로다. 제목과 편집 대상을 한 줄로 합치고 "플레이어로"
버튼을 제목 줄로 옮겼으며, 결과 영역을 세 줄로 넓히고 새 결과가 오면 잠깐
밝힌다. 수량 단위는 쓰는 곳마다 따로 기억한다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 3: 앞뒤 순서 — 게임 창이 편집기 위에 뜨게

편집기 캔버스를 게임과 같은 카메라 방식으로 바꿔 게임 화면 사이에 끼운다(스펙 3.2, 4, 5.2절).

**Files:**
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsUi.cs` (`uiraise`)
- Modify: `tools/research/ui-check.ps1` (`order`, `battle` 부분)

**Interfaces:**
- Consumes: 게임의 `g.ui.canvas`, `g.ui.uiCamera`, `g.ui.GetLayerTopSort(UILayer.UI)`(스펙 3.1절에서 실측).
- Produces: `EditorWindow.Raise()`, `EditorWindow.IsCovered`. `uistate`의 출력에 `covered=<bool> top=<n>`이 `order=` 다음에 들어간다. 개발 명령 `uiraise`.

- [ ] **Step 1: 빨간 불 — `uiraise` 명령을 먼저 넣는다**

`Dev/DevCommandsUi.cs`의 `switch`에서 `case "uistate":` 앞에 넣는다.

```csharp
                case "uiraise":
                    EditorWindow.Raise();
                    return EditorWindow.DevState;
```

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
```

Expected: 실패. `'EditorWindow'에 'Raise'에 대한 정의가 포함되어 있지 않습니다`.

- [ ] **Step 2: `EditorWindow.cs`에 필드를 보탠다**

`private const int OverlayOrder = 30000;` 다음에:

```csharp
        // 게임 화면의 정렬은 20 간격이다(UIMgr.UIIntervalSorting). 그 사이에 끼운다.
        private const int OrderAboveTop = 10;
```

`private static bool tickWarned;` 다음에:

```csharp
        private static bool cameraMode;   // 게임과 같은 카메라 방식으로 그리는 중인지
        private static int raisedTop;     // 맨 앞으로 가져올 때의 UI 층 맨 위 정렬 값
```

- [ ] **Step 3: `Raise`, `IsCovered`, `CameraUsable`을 넣는다**

`SwitchTab` 메서드 앞에 넣는다.

```csharp
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
```

- [ ] **Step 4: `Toggle`, `Tick`, `Open`을 바꾼다**

`Toggle` 전체:

```csharp
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
```

`Tick` 전체:

```csharp
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
```

`Open`에서 `root.SetActive(true);` 다음 줄에 `Raise();`를 넣는다.

- [ ] **Step 5: `DevState`에 가려짐과 맨 위 값을 보탠다**

`DevState`의 `get` 전체:

```csharp
            get
            {
                return "open=" + IsOpen + " tab=" + tab + " target=" + (targetId == null ? "player" : "npc")
                    + " mode=" + (canvas == null ? "none" : canvas.renderMode.ToString()) + " order=" + (canvas == null ? 0 : canvas.sortingOrder)
                    + " covered=" + IsCovered + " top=" + g.ui.GetLayerTopSort(UILayer.UI)
                    + " message='" + message + "'";
            }
```

- [ ] **Step 6: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 7: `ui-check.ps1`에 `order`와 `battle` 부분을 넣는다**

`'tabs' { … }` 블록 다음에 넣는다.

```powershell
  'order' {
    $traps = Traps
    Cmd 'editor' | Out-Null
    $s = Cmd 'uistate'
    Check 'opens in camera mode just above the map' ($s -match 'mode=ScreenSpaceCamera order=10 covered=False top=0 ') $s

    # 스펙 6절 2번: 나중에 연 게임 화면이 위에 뜨고, Insert가 편집기를 맨 앞으로 가져온다
    Cmd 'uiopen PlayerInfo' 1500 | Out-Null
    $s = Cmd 'uistate'
    Check 'a game screen opened later covers the editor' ($s -match 'covered=True top=20 ' -and -not (IsAt 'Tab0')) $s
    Key 0x2D
    $s = Cmd 'uistate'
    Check 'Insert brings a covered editor to the front' ($s -match 'open=True .*order=30 covered=False ' -and (IsAt 'Tab0')) $s

    # 스펙 6절 3번: 아래의 게임 화면이 닫히면 정렬을 낮춘다
    Cmd 'uiclose PlayerInfo' 1500 | Out-Null
    $s = Cmd 'uistate'
    Check 'order drops when the screen below closes' ($s -match 'order=10 covered=False top=0 ') $s
    Cmd 'uiopen PlayerInfo' 1500 | Out-Null
    Check 'the reopened screen is above the editor again' ((Cmd 'uistate') -match 'covered=True ' -and -not (IsAt 'Tab0'))
    Cmd 'uiclose PlayerInfo' 1500 | Out-Null
    Key 0x2D
    Check 'Insert closes an editor that is in front' ((Cmd 'uistate') -match 'open=False ')

    # 스펙 6절 4번: 확인 창은 더 높은 층이라 항상 위다. 가려진 것으로 치지 않는다
    Cmd 'editor' | Out-Null
    Cmd 'uiopen CheckPopup' 1200 | Out-Null
    Check 'confirm popup is above the editor' ((Rc 'hit 1037 531' 300) -match 'CheckPopup' -and -not (IsAt 'Tab0') -and (Cmd 'uistate') -match 'covered=False ')
    Cmd 'uiclose CheckPopup' 1000 | Out-Null
    Check 'editor is usable again' (IsAt 'Tab0')

    # Review Focus 4: Insert를 빠르게 여러 번, 가려진 상태와 섞어서
    1..6 | ForEach-Object { Key 0x2D 150 }
    Check 'six quick toggles end where they started' ((Cmd 'uistate') -match 'open=True ')
    Cmd 'uiopen PlayerInfo' 1500 | Out-Null
    1..3 | ForEach-Object { Key 0x2D 150 }   # 맨 앞으로 → 닫기 → 열기
    $s = Cmd 'uistate'
    Check 'toggles mixed with a covering screen stay consistent' ($s -match 'open=True .*order=30 covered=False ') $s
    Cmd 'uiclose PlayerInfo' 1500 | Out-Null
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'battle' {
    # 스펙 6절 21번, Review Focus 5. 전투에서 나오는 방법을 자동화하지 않았으므로 회차의 마지막에 돌리고 바로 -End 한다.
    $traps = Traps
    Cmd 'fight 1011 1' 8000 | Out-Null
    Check 'editor opens in battle' ((Cmd 'editor') -match 'open=True')
    "INFO battle state: $(Cmd 'uistate')"
    Check 'tabs are clickable in battle' (IsAt 'Tab7')
    Press 'Tab7' | Out-Null
    Check 'world tab is drawn in battle' (IsAt 'God')
    Shot 'battle'
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
```

- [ ] **Step 8: 회차 — `order`, `battle`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t3
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part order
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part tabs
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part battle
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t3
```

Expected: 세 부분 모두 `FAILS=0`. `battle`의 `INFO` 줄에 나온 방식(`mode=`)을 적어 둔다(Task 6의 문서에 쓴다). `.devremote\check-battle.png`에서 편집기가 전투 화면 위에 보이는지 눈으로 확인한다.

`order`의 첫 검사가 `order=10`이 아니라 다른 값으로 실패하면 지도 화면이 맨 위가 아닌 것이다(다른 게임 화면이 떠 있다). 캡처해 무엇이 떠 있는지 보고, 게임 메뉴(`UI/GameMemu`)면 `돌아가기`(959,391)를 눌러 닫은 뒤 다시 돌린다.

- [ ] **Step 9: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI/EditorWindow.cs src/ModCode/ModMain/Dev/DevCommandsUi.cs tools/research/ui-check.ps1
git -C E:\TlToybox commit -m @'
feat: 게임의 창이 편집기 위에 뜨게 — 편집기를 게임 화면 사이에 끼움

편집기 캔버스를 게임과 같은 카메라 방식으로 그리고, 열 때의 게임 화면들 바로
위에 놓는다. 그 뒤에 뜨는 돌파 결과·NPC 대화·월말 기록은 편집기 위에 나온다.
Insert는 가려진 편집기를 맨 앞으로 가져오고, 맨 앞이면 닫는다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 4: 이동과 접기

제목 줄을 끌어 창을 옮기고, 위치를 `config.txt`에 기억하고, `접기`로 제목 줄만 남긴다(스펙 3.3, 5.2절).

**Files:**
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsUi.cs` (`uicollapse`)
- Modify: `tools/research/ui-check.ps1` (`move`, `move-restart`, `move-corrupt` 부분과 함수 둘)

**Interfaces:**
- Consumes: `ChangeLog.DataDir`(`<세이브 뿌리>\TlToybox`), 제목 줄의 바탕 `TitleBar`(Task 2), 개발 명령 `uidrag`(Task 1).
- Produces: `EditorWindow.SetCollapsed(bool)`. 버튼 `Collapse`. `config.txt`의 `window=<x>,<y>` 줄. `uistate`의 출력에 `pos=<x>,<y> collapsed=<bool>`이 `top=` 다음에 들어간다. 개발 명령 `uicollapse on|off`.

- [ ] **Step 1: 빨간 불 — `uicollapse` 명령을 먼저 넣는다**

`Dev/DevCommandsUi.cs`의 `switch`에서 `case "uiraise":` 앞에 넣는다.

```csharp
                case "uicollapse":
                    EditorWindow.SetCollapsed(p[1] == "on");
                    return EditorWindow.DevState;
```

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
```

Expected: 실패. `'EditorWindow'에 'SetCollapsed'에 대한 정의가 포함되어 있지 않습니다`.

- [ ] **Step 2: `EditorWindow.cs`의 `using`과 필드를 보탠다**

파일 머리의 `using`을 다음으로 바꾼다.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
```

`private const int OrderAboveTop = 10;` 다음에:

```csharp
        // 창의 위치는 1920x1080 기준 좌표다(왼쪽 위가 원점, y는 아래로 증가). 제목 줄의 세로 전체와 가로 200 이상이 화면 안에 남아야 한다.
        private const float ScreenW = 1920;
        private const float ScreenH = 1080;
        private const float MinVisible = 200;
        private const float DefaultX = 60;
        private const float DefaultY = 60;
```

`private static int raisedTop;` 다음에:

```csharp
        private static Vector2 position = new Vector2(DefaultX, DefaultY);
        private static Vector2 dragStart;   // 끌기를 시작할 때의 창 위치
        private static Vector2 dragMoved;   // 끌기를 시작한 뒤의 누적 이동량(화면 픽셀, y는 위로 증가)
        private static bool collapsed;
        private static Text collapseLabel;
```

- [ ] **Step 3: 위치, 끌기, 접기의 메서드를 넣는다**

`Raise` 메서드 앞에 넣는다.

```csharp
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
```

- [ ] **Step 4: `Build`와 `Open`을 고친다**

`Build`에서 `window = Ui.Panel("Window", root.transform, 60, 60, Width, Height, Ui.Background);`부터 `Ui.Button("Close", …);`까지를 다음으로 바꾼다.

```csharp
            LoadPosition();
            window = Ui.Panel("Window", root.transform, position.x, position.y, Width, Height, Ui.Background);
            // 제목 줄의 바탕이자 끌기 손잡이. 버튼보다 먼저 만들어 버튼 아래에 깔리게 한다.
            AddDrag(Ui.Panel("TitleBar", window, 0, 0, Width, TitleH, Ui.Section).gameObject);
            Ui.Label(window, "TlToybox 편집기", 20, 8, 230, 36, 24);
            titleArea = Ui.Rect("Title", window, 260, 8, 790, 36);
            Button collapse = Ui.Button("Collapse", window, "접기", Width - 196, 8, 90, 36, new Action(() => SetCollapsed(!collapsed)));
            collapseLabel = collapse.GetComponentInChildren<Text>();
            Ui.Button("Close", window, "닫기", Width - 100, 8, 84, 36, new Action(Close));
```

`Open`에서 `Raise();` 다음 줄에 `SetCollapsed(false);`를 넣는다.

- [ ] **Step 5: `DevState`에 위치와 접힘을 보탠다**

`DevState`의 `get` 전체:

```csharp
            get
            {
                return "open=" + IsOpen + " tab=" + tab + " target=" + (targetId == null ? "player" : "npc")
                    + " mode=" + (canvas == null ? "none" : canvas.renderMode.ToString()) + " order=" + (canvas == null ? 0 : canvas.sortingOrder)
                    + " covered=" + IsCovered + " top=" + g.ui.GetLayerTopSort(UILayer.UI)
                    + " pos=" + (int)position.x + "," + (int)position.y + " collapsed=" + collapsed
                    + " message='" + message + "'";
            }
```

- [ ] **Step 6: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 7: `ui-check.ps1`에 창 위치를 다루는 함수 둘을 넣는다**

`$tabProbe = …` 줄 다음에 넣는다.

```powershell
function WinState {
  $o = Cmd 'uistate' 100
  if ($o -notmatch ' pos=(-?\d+),(-?\d+) collapsed=(\w+) ') { throw "uistate 출력을 읽지 못했습니다: $o" }
  [pscustomobject]@{ x = [int]$Matches[1]; y = [int]$Matches[2]; collapsed = ($Matches[3] -eq 'True'); raw = $o }
}
# 제목 줄의 보이는 빈 곳을 잡아 창의 왼쪽 위가 (x,y)에 오도록 끈다.
# 잡는 곳은 닫기 버튼 오른쪽의 틈(창 안 x=1492)이고, 그곳이 화면 밖이면 제목 글자 자리(x=150)다. 글자는 클릭을 받지 않아 바탕이 잡힌다.
function DragWindowTo([int]$x, [int]$y) {
  $w = WinState
  $gx = $w.x + 1492
  if ($gx -gt 1917) { $gx = $w.x + 150 }
  $gy = $w.y + 26
  Cmd "uidrag $gx $gy $($gx + $x - $w.x) $($gy + $y - $w.y) 5" 300
}
```

- [ ] **Step 8: `ui-check.ps1`에 `move`, `move-restart`, `move-corrupt` 부분을 넣는다**

`'battle' { … }` 블록 **앞**에 넣는다.

```powershell
  'move' {
    # 창 위치는 세이브 폴더 밖의 config.txt에 남는다(회차를 되돌려도 남는다). 시험 전의 파일을 떠 두고 move-corrupt에서 되돌린다.
    $traps = Traps
    Copy-Item -LiteralPath (Join-Path $data 'config.txt') -Destination (Join-Path $work 'check-config.txt') -Force
    Cmd 'editor' | Out-Null
    $o = DragWindowTo 60 60
    Check 'the title bar takes the drag' ($o -match 'handler=TitleBar ') $o
    $w = WinState
    Check 'window is at the base position' ($w.x -eq 60 -and $w.y -eq 60) $w.raw
    $t = Pos 'Tab0'

    # 스펙 6절 5번: 끌면 따라 움직이고, 옮긴 자리에서 버튼이 눌린다
    DragWindowTo 260 160 | Out-Null
    $w = WinState; $t2 = Pos 'Tab0'
    Check 'window follows the drag' ($w.x -eq 260 -and $w.y -eq 160) $w.raw
    Check 'everything inside moved with it' ($t2.x -eq $t.x + 200 -and $t2.y -eq $t.y + 100 -and (IsAt 'Tab0')) "tab0 $($t.x),$($t.y) -> $($t2.x),$($t2.y)"
    Press 'Tab1' | Out-Null
    Check 'buttons work at the new place' ((Cmd 'uistate') -match 'tab=1 ')
    Check 'position is written to config.txt' ([bool]((Get-Content -LiteralPath (Join-Path $data 'config.txt') -Encoding UTF8) -match '^window=260,160$'))
    Check 'other config lines are kept' ([bool]((Get-Content -LiteralPath (Join-Path $data 'config.txt') -Encoding UTF8) -match '^hotkey='))

    # 화면 밖으로는 나가지 않는다
    DragWindowTo 5000 5000 | Out-Null
    $w = WinState
    Check 'stops at the right and bottom limits' ($w.x -eq 1720 -and $w.y -eq 1028) $w.raw
    DragWindowTo -5000 -5000 | Out-Null
    $w = WinState
    Check 'stops at the left and top limits' ($w.x -eq -1300 -and $w.y -eq 0) $w.raw
    DragWindowTo 260 160 | Out-Null

    # 스펙 6절 6번: 접기와 펼침
    Press 'Tab0' | Out-Null
    $t = Pos 'Tab0'
    Press 'Collapse' | Out-Null
    $w = WinState
    Check 'collapsed' ($w.collapsed -and (Cmd 'uitext Collapse') -match "text='펼침'") $w.raw
    Check 'the folded area lets clicks through to the game' ((Rc "hit $($t.x) $($t.y)" 200) -notmatch 'TlToyboxEditor')
    Check 'the title bar is still there' (IsAt 'Close')

    # Review Focus 3: 접힌 상태에서 끌기, 접은 채 닫았다가 다시 열기
    DragWindowTo 300 200 | Out-Null
    $w = WinState
    Check 'a collapsed window can be dragged' ($w.x -eq 300 -and $w.y -eq 200 -and $w.collapsed) $w.raw
    Press 'Collapse' | Out-Null
    $w = WinState; $t2 = Pos 'Tab0'
    Check 'expands again where it was dragged, on the same tab' (-not $w.collapsed -and $w.raw -match ' tab=0 ' -and $t2.x -eq $t.x + 40 -and $t2.y -eq $t.y + 40 -and (IsAt 'Tab0')) $w.raw
    Press 'Collapse' | Out-Null
    Press 'Close' | Out-Null
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'reopening shows it expanded at the same place' (-not $w.collapsed -and $w.x -eq 300 -and $w.y -eq 200 -and (IsAt 'Tab0')) $w.raw

    DragWindowTo 260 160 | Out-Null   # move-restart가 확인할 위치
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'move-restart' {
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'position survives a restart' ($w.x -eq 260 -and $w.y -eq 160) $w.raw
    Press 'Close' | Out-Null
    # Review Focus 1: 깨진 값과 화면 밖의 값으로 다시 시작할 준비
    $lines = @(Get-Content -LiteralPath (Join-Path $work 'check-config.txt') -Encoding UTF8 | Where-Object { $_ -notmatch '^window=' })
    [IO.File]::WriteAllLines((Join-Path $data 'config.txt'), [string[]]($lines + 'window=abc' + 'window=99999,-99999' + 'window=7'))
    'corrupt config written'
  }
  'move-corrupt' {
    Cmd 'editor' | Out-Null
    $w = WinState
    Check 'unreadable lines are skipped and an off-screen value is pulled inside' ($w.x -eq 1720 -and $w.y -eq 0) $w.raw
    # 이 자리에서는 제목 줄의 왼쪽 200만 화면 안에 있다. 그 부분이 잡히는지 본다.
    Check 'the visible part of the title bar can be grabbed' ((Rc 'hit 1870 26' 200) -match '/TitleBar\s*$')
    DragWindowTo 60 60 | Out-Null
    Check 'and dragged back' ((WinState).x -eq 60 -and (IsAt 'Close'))
    Press 'Close' | Out-Null
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
    Copy-Item -LiteralPath (Join-Path $work 'check-config.txt') -Destination (Join-Path $data 'config.txt') -Force
    [IO.File]::Delete((Join-Path $work 'check-config.txt'))
    'config.txt restored'
  }
```

- [ ] **Step 9: 회차 — `move`, `move-restart`, `move-corrupt`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t4
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part order
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part move
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part move-restart
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Restart
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part move-corrupt
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t4
Get-Content "$env:USERPROFILE\AppData\LocalLow\guigugame\guigubahuang\TlToybox\config.txt"
```

Expected: 네 부분 모두 `FAILS=0`. 마지막 명령이 보여 주는 `config.txt`에 `window=` 줄이 없다(시험 전과 같다).

검사가 중간에 멈춰 `config.txt`를 되돌리지 못했으면 손으로 되돌린다:

```powershell
Copy-Item -LiteralPath E:\TlToybox\.devremote\check-config.txt -Destination "$env:USERPROFILE\AppData\LocalLow\guigugame\guigubahuang\TlToybox\config.txt" -Force
```

- [ ] **Step 10: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI/EditorWindow.cs src/ModCode/ModMain/Dev/DevCommandsUi.cs tools/research/ui-check.ps1
git -C E:\TlToybox commit -m @'
feat: 편집기 창을 끌어 옮기고 접기

제목 줄을 끌면 창이 따라 움직이고, 제목 줄이 화면 밖으로 나가지 않게 막는다.
위치는 config.txt의 window= 줄에 기억한다. "접기"를 누르면 제목 줄만 남아
게임 화면이 그대로 보인다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 5: 켜 둔 기능 표시

무적, 피해 배율, 날짜 고정 가운데 켜진 것을 화면 위쪽 가운데에 보인다. 편집기를 닫아도 남는다(스펙 5.3절).

**Files:**
- Create: `src/ModCode/ModMain/UI/StatusOverlay.cs`
- Modify: `src/ModCode/ModMain/UI/EditorHost.cs` (`EditorWindow.Tick();` 다음)
- Modify: `src/ModCode/ModMain/Dev/DevCommandsUi.cs` (`uistate`)
- Modify: `tools/research/ui-check.ps1` (`status`, `status-battle` 부분)

**Interfaces:**
- Consumes: `BattleService.God`(bool), `BattleService.Multiplier`(int), `TimeService.Frozen`(bool), `EditService.WorldReady`.
- Produces: `StatusOverlay.Tick()`, `StatusOverlay.Describe()`, 개발 전용 `StatusOverlay.DevState`. `uistate`의 출력 맨 앞에 `status=[<문구|hidden>] `이 붙는다.

- [ ] **Step 1: 빨간 불 — `uistate`가 표시의 상태를 함께 내게 한다**

`Dev/DevCommandsUi.cs`의 `case "uistate":`를 바꾼다.

```csharp
                case "uistate":
                    return "status=[" + StatusOverlay.DevState + "] " + EditorWindow.DevState;
```

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
```

Expected: 실패. `'StatusOverlay' 이름이 현재 컨텍스트에 없습니다`(CS0103).

- [ ] **Step 2: `UI/StatusOverlay.cs`를 만든다**

```csharp
using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 켜 둔 기능(무적, 피해 배율, 날짜 고정)을 화면 위쪽 가운데에 보인다.
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
```

- [ ] **Step 3: `EditorHost.cs`의 `Update`에서 부른다**

`EditorWindow.Tick();` 다음 줄에 넣는다.

```csharp
            StatusOverlay.Tick();
```

- [ ] **Step 4: 두 구성으로 빌드한다**

```powershell
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo | Select-Object -Last 6
dotnet build E:\TlToybox\src\ModCode\ModMain\ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check" | Select-Object -Last 4
```

Expected: 둘 다 "경고 0개 / 오류 0개".

- [ ] **Step 5: `ui-check.ps1`에 `status`와 `status-battle` 부분을 넣는다**

`'battle' { … }` 블록 **앞**에 넣는다.

```powershell
  'status' {
    # 스펙 6절 19번. 표시는 30프레임마다 맞춰지므로 켜고 끈 뒤 조금 기다린다.
    $traps = Traps
    Check 'hidden when nothing is on' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    Cmd 'tfreeze on' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'shown without ever opening the editor' ((Cmd 'uistate') -match 'status=\[날짜 고정\] ')
    Cmd 'editor' | Out-Null
    Press 'Tab7' | Out-Null; Press 'God' | Out-Null; Press 'Multiplier' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'all three are listed' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] ')
    Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'stays when the editor is closed' ((Cmd 'uistate') -match 'status=\[무적 · 피해 x2 · 날짜 고정\] open=False ')
    Check 'the overlay does not take clicks' ((Rc 'hit 960 23' 200) -notmatch 'TlToybox')
    Shot 'status'
    # 끈다. 배율은 x2 → x5 → x10 → x100 → x1로 돈다.
    Cmd 'editor' | Out-Null
    Press 'Tab7' | Out-Null; Press 'God' | Out-Null; Press 'Freeze' | Out-Null
    1..4 | ForEach-Object { Press 'Multiplier' | Out-Null }
    Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'hidden again when everything is off' ((Cmd 'uistate') -match 'status=\[hidden\] ')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'status-battle' {
    # Review Focus 5. 회차의 마지막에 돌리고 바로 -End 한다(전투에서 나오지 않는다).
    Cmd 'editor' | Out-Null
    Press 'Tab7' | Out-Null; Press 'God' | Out-Null
    Press 'Close' | Out-Null
    Cmd 'fight 1011 1' 8000 | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'shown in battle' ((Cmd 'uistate') -match 'status=\[무적\] ')
    Shot 'status-battle'
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
```

- [ ] **Step 6: 회차 — `status`, `tabs`, `status-battle`**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-t5
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part status
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part tabs
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part status-battle
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-t5
```

Expected: 세 부분 모두 `FAILS=0`. `.devremote\check-status.png`에서 표시가 화면 위쪽 가운데에 있고 게임의 HUD를 가리지 않는지, `check-status-battle.png`에서 전투 화면에 표시가 보이는지 눈으로 확인한다.

`status-battle`의 `shown in battle`이 `status=[hidden]`으로 실패하면 전투 중에 `EditService.WorldReady`가 `false`인 것이다. 그때는 멈추고 사용자에게 알린다(전투 중의 판정을 어떻게 할지는 `Services/`를 건드리는 결정이라 이 계획의 범위 밖이다).

- [ ] **Step 7: 커밋한다**

```powershell
git -C E:\TlToybox add src/ModCode/ModMain/UI/StatusOverlay.cs src/ModCode/ModMain/UI/EditorHost.cs src/ModCode/ModMain/Dev/DevCommandsUi.cs tools/research/ui-check.ps1
git -C E:\TlToybox commit -m @'
feat: 켜 둔 기능(무적·피해 배율·날짜 고정)을 화면 위쪽에 표시

편집기를 닫으면 무엇이 켜져 있는지 알 수 없었다. 켜진 것이 있으면 화면 위쪽
가운데에 작은 표시를 띄운다. 클릭은 받지 않는다.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
```

---

### Task 6: 전체 검증, 문서, 병합, 설치

**Files:**
- Modify: `docs/05-editor-v0.1.md` (맨 끝에 26절)
- Modify: `docs/06-handoff.md`

**Interfaces:**
- Consumes: Task 1~5의 결과와 검증 출력.
- Produces: `develop`에 병합된 1차 결과, 게임에 설치된 배포 빌드.

- [ ] **Step 1: 배포 구성을 게임에서 확인한다 (스펙 6절 22번)**

배포 구성에는 개발 명령이 없다. 조작 도구(`rc.ps1`)와 키 입력으로만 본다. 창은 기본 위치 (60,60)이므로 첫 탭 버튼의 가운데는 (151,146)이다.

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-rel -ReleaseBuild
& tools\DevRemote\win.ps1 -Do key -Vk 0x2D
Start-Sleep -Seconds 1
& tools\DevRemote\rc.ps1 -Cmd 'hit 151 146' -NoShot
& tools\DevRemote\rc.ps1 -Cmd 'click 151 562' -NoShot
& tools\DevRemote\win.ps1 -Do shot -Out .devremote\check-release.png -Scale 0.6
& tools\DevRemote\win.ps1 -Do key -Vk 0x2D
Start-Sleep -Seconds 1
& tools\DevRemote\rc.ps1 -Cmd 'hit 151 146' -NoShot
Select-String -LiteralPath 'E:\Games\Tale of Immortal\MelonLoader\Latest.log' -Pattern 'trampoline'
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-rel
```

Expected: 첫 `hit`이 `TlToyboxEditor/Window/Tabs/Tab0`, `click`이 `Button.onClick: Tab8`(안전 탭), 둘째 `hit`이 편집기가 아닌 것(닫혔다), `trampoline` 줄 없음. 캡처에서 안전 탭이 보인다.

- [ ] **Step 2: 전체 검사를 깨끗한 회차에서 한 번 더 돌린다**

```powershell
pwsh -NoProfile -File tools\DevRemote\session.ps1 -Begin ui1-final
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part base
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part tabs
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part order
pwsh -NoProfile -File tools\research\ui-check.ps1 -Part status
pwsh -NoProfile -File tools\research\datebag-check.ps1 -Part time
pwsh -NoProfile -File tools\DevRemote\session.ps1 -End ui1-final
pwsh -NoProfile -File tools\DevRemote\session.tests.ps1
```

Expected: 다섯 부분 모두 `FAILS=0`. `session.tests.ps1`은 14개 모두 통과(회차 스크립트를 고치지 않았으므로 그대로여야 한다). 부분별 PASS 수를 적어 둔다(문서에 쓴다).

- [ ] **Step 3: `docs/05-editor-v0.1.md`의 맨 끝에 26절을 넣는다**

`<…>`로 적은 곳은 Task 3·5·6의 실제 출력으로 채운다. 채울 수 없으면 "미확인"과 사유를 적는다.

```markdown
## 26. 창의 틀과 탭 분리 (2026-10-13 추가)

묶음 B의 1차분이다. 설계와 사전 실험은 `docs/superpowers/specs/2026-10-13-ui-rework-design.md`(3절이 실측), 구현 계획은 `docs/superpowers/plans/2026-10-13-ui-rework-1-shell.md`에 있다. 탭 안의 동작은 바꾸지 않았다. 공통 부품과 탭별 개선은 2차에서 한다.

### 넣은 것

| 기능 | 동작 |
|---|---|
| 앞뒤 순서 | 편집기를 게임과 같은 카메라 방식(`g.ui.uiCamera`)으로 그리고, 열 때의 `UI` 층 맨 위 정렬 값(`g.ui.GetLayerTopSort(UILayer.UI)`) + 10에 놓는다. 그 뒤에 열리는 게임 화면은 편집기 위에 뜬다. 확인 창(`Window` 층)과 로딩은 항상 위다 |
| 단축키 | 닫혀 있으면 열고, 게임 화면에 가려져 있으면 맨 앞으로 가져오고, 맨 앞이면 닫는다 |
| 이동 | 제목 줄을 끌어 옮긴다. 제목 줄의 세로 전체와 가로 200 이상이 화면 안에 남는다. 위치는 `config.txt`의 `window=<x>,<y>` |
| 접기 | 제목 줄만 남긴다. 접힌 상태는 기억하지 않는다 |
| 제목 줄 | 제목과 편집 대상이 한 줄. 대상이 NPC이면 이름이 강조색이고 `플레이어로` 버튼이 켜진다 |
| 결과 영역 | 세 줄까지 줄바꿈. 새 결과가 오면 0.6초 동안 밝아진다 |
| 켜 둔 기능 표시 | 무적, 피해 배율, 날짜 고정 가운데 켜진 것을 화면 위쪽 가운데에 보인다. 편집기를 닫아도 남고 클릭은 받지 않는다 |
| 수량 단위 | 능력치·숙련 경험치·순간 이동·호감도·영석·아이템이 따로 기억한다(전에는 앞의 넷이 하나를 함께 썼다) |

### 구조

- `UI/EditorWindow.cs`가 창의 틀이고, 탭은 `UI/Tabs/`의 클래스 아홉 개다(`EditorTab`을 상속). 탭은 자기 상태를 필드로 갖고, 버튼 동작 뒤에 `Say`나 `Refresh`로 내용을 다시 그린다.
- 편집 대상이 바뀌면 `EditorWindow.SetTarget`이 모든 탭의 `OnTargetChanged`를 부른다. 탭이 다른 탭의 상태를 직접 지우지 않는다.
- 매 프레임 처리(`EditorHostBehaviour.Update`)는 `BagService.Tick`, `EditorWindow.Tick`(결과 영역의 색, 정렬 낮추기), `StatusOverlay.Tick`(30프레임마다)이다. 셋 다 예외를 밖으로 던지지 않는다.

### 검증 (게임에서 실행, `tools/research/ui-check.ps1`)

| 부분 | 보는 것 | 결과 |
|---|---|---|
| `base` | 탭 아홉 개가 열리고 이름으로 요소를 찾는다 | <PASS 수> |
| `tabs` | 탭 전환, 하위 화면, 수량의 따로 기억, 결과 문구, 편집 대상 바꾸기와 되돌리기, 없는 대상 | <PASS 수> |
| `order` | 카메라 방식과 정렬, 가려짐, `Insert`의 세 동작, 확인 창, 빠른 연속 입력 | <PASS 수> |
| `move`, `move-restart`, `move-corrupt` | 끌기, 범위, 위치 기억, 접기, 접힌 채 끌기, 깨진 설정 | <PASS 수> |
| `status`, `status-battle` | 표시의 켜짐·꺼짐, 편집기를 닫은 뒤, 전투 중 | <PASS 수> |
| `battle` | 전투에서 편집기를 연다. 방식은 <카메라 방식 / 덧그리기> | <PASS 수> |
| `datebag-check.ps1`의 `time`·`freeze`·`bag` | 날짜와 가방이 그대로인지 | <PASS 수> |
| 배포 구성 | 경고·오류 0, DLL 하나, 편집기가 열린다 | <결과> |

검증 스크립트는 버튼을 좌표가 아니라 이름으로 찾는다(`uipos <이름>`으로 자리를 얻어 그 자리를 누른다). 화면 배치를 바꿔도 스크립트를 고칠 필요가 없다.

### 구현하며 고친 것

<구현 중에 계획과 달라진 것을 적는다. 없으면 "없음">

### 확인하지 못한 것, 한계

- 실제 마우스로 제목 줄을 끄는 동작(도구가 보낸 끌기 이벤트로 시험했다). <사용자가 확인했으면 그 결과>
- 주 화면(세이브 진입 전)과 로딩 중의 편집기 표시.
- 1920x1080이 아닌 화면에서의 끌기 범위(시험은 1920x1080 창에서만 했다).
- 지도 화면 안에 정렬 값을 따로 가진 자식 캔버스가 있는지(시험에서는 편집기가 지도 화면 전체를 덮었다).
```

- [ ] **Step 4: `docs/06-handoff.md`를 갱신한다**

- 1절: "직전 작업"을 "묶음 B의 1차(창의 틀과 탭 분리)를 끝냈다(05 문서 26절). 다음은 2차 계획이다(8.3절)"로, 설치된 것의 날짜를 이번 설치 시각으로 바꾼다.
- 2절의 "공통 규칙"에 한 줄: "편집기는 게임 화면 사이에 끼어 그려진다. 편집기를 연 뒤에 뜨는 게임 창은 편집기 위에 나온다. `Insert`는 가려진 편집기를 맨 앞으로 가져온다."
- 3절의 표: `UI/` 줄을 "`Ui.cs`(uGUI 도우미), `EditorHost.cs`(단축키, 매 프레임 처리), `EditorWindow.cs`(창의 틀), `StatusOverlay.cs`(켜 둔 기능 표시), `Tabs/`(탭마다 한 파일, `EditorTab` 상속), `Widgets/`(공통 부품. 지금은 `AmountBar`)"로 바꾼다. `tools/research/` 줄에 `check-lib.ps1`, `ui-check.ps1`을 보탠다.
- 5절: "편집기 좌표 계산" 문단을 다음으로 바꾼다. "편집기의 버튼은 좌표가 아니라 이름으로 찾는다. `mod.ps1 -Cmd 'uipos <이름>'`이 가운데의 게임 좌표를 준다(이름 끝의 `*`는 '이 글자로 시작'). 검증 스크립트에서는 `check-lib.ps1`의 `Press <이름>`, `IsAt <이름>`을 쓴다. `uistate`가 창의 상태(열림, 탭, 대상, 방식, 정렬, 가려짐, 위치, 접힘, 결과 문구, 켜 둔 기능 표시)를 준다." "회차 안에서 쓰는 도구"에 `ui-check.ps1 -Part <이름>`과 부분 목록(`base`, `tabs`, `order`, `move`, `move-restart`, `move-corrupt`, `status`, `status-battle`, `battle`. `battle`과 `status-battle`은 회차의 마지막에)을 보탠다.
- 6절(함정)에 두 줄: "`mod.ps1`·`rc.ps1`은 배열 매개변수가 있어 `pwsh -File`로 부르면 인수가 어긋난다. `&`로 부른다." "창 위치는 세이브 폴더 밖의 `config.txt`에 남는다. 위치를 바꾸는 시험은 시험 전의 파일을 떠 두고 되돌린다(`ui-check.ps1`의 `move` 계열이 그렇게 한다)."
- 8.1절의 표: B의 상태를 "1차(창의 틀과 탭 분리) 끝남. 2차(공통 부품, 탭별 개선, 가방 칸 한 번에 지정, 변경 이력 전체 보기) 계획 작성 전"으로.
- 8.3절: 첫 문단을 "1차는 끝났다. 2차는 스펙의 작업 순서 3~7번이며, `/superpowers:writing-plans`로 계획을 쓰는 것부터 시작한다. 탭의 하위 화면 쌓기(`Push`/`Pop`)는 2차에서 `EditorTab`에 넣는다"로 바꾼다.
- 8.7절: `feat/ui-rework-research` 줄은 그대로 둔다.

- [ ] **Step 5: 문서를 커밋하고 `develop`에 병합한다**

```powershell
git -C E:\TlToybox add docs/05-editor-v0.1.md docs/06-handoff.md
git -C E:\TlToybox commit -m @'
docs: 창의 틀과 탭 분리의 기록(05 문서 26절), 인계 문서 갱신

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
git -C E:\TlToybox switch develop
git -C E:\TlToybox merge --no-ff feat/ui-rework-1 -m @'
Merge branch 'feat/ui-rework-1' into develop

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>
'@
git -C E:\TlToybox status --short
```

Expected: 병합이 충돌 없이 끝나고 작업 트리가 깨끗하다.

- [ ] **Step 6: 배포 빌드를 게임에 설치한다**

게임이 꺼져 있어야 한다. 켜져 있으면 사용자에게 알리고 기다린다.

```powershell
pwsh -NoProfile -File E:\TlToybox\tools\install.ps1
```

Expected: 마지막에 `…\ModExportData\Mod_sWP6UL\ModCode\dll\MOD_sWP6UL.dll`과 오늘 날짜가 나온다.

- [ ] **Step 7: 사용자에게 알리고 직접 확인을 부탁한다**

알릴 것: 바뀐 조작 네 가지(게임 창이 편집기 위에 뜸과 `Insert`의 세 동작, 제목 줄 끌기, `접기`, 켜 둔 기능 표시), 탭 안은 그대로라는 것, 검증 결과.

부탁할 것(도구로 시험할 수 없었다): **실제 마우스로 제목 줄을 끌어 창이 따라오는지**, 놓은 자리가 다음에 열 때 유지되는지. 결과를 받으면 05 문서 26절의 "확인하지 못한 것"에 적는다.
