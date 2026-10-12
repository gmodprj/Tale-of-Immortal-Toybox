# 02. 모드 개발 구성 리서치

> 조사일: 2026-10-12 · 출처: 로컬 공식 자료, 웹, 창작마당 조사(`02a-workshop-survey.md`)
> 방법: 공식 교안(docx 4종)과 템플릿·예제를 읽고, `Mods\GGBH_MOD.dll`과 프록시 어셈블리의 메타데이터를 정적으로 확인했다. 게임은 실행하지 않았다.

## 1. 요약

- 귀곡팔황의 공식 모드는 **설정 테이블(`ModExcel`) + 리소스(`ModAssets`/`ModRes`) + 코드(`ModCode`)** 세 요소로 이뤄지고, 게임 내 **모드 에디터**가 프로젝트 생성·내보내기·디버그 실행·창작마당 업로드를 맡는다.
- 코드 모드는 .NET Framework 4.7.2 DLL이다. 네임스페이스와 어셈블리 이름이 **`MOD_<모드ID>`** 여야 하고, 그 안의 `ModMain` 클래스의 `Init()`·`Destroy()`가 리플렉션으로 호출된다. 모드 ID는 에디터가 부여하는 여섯 글자다.
- 로드는 게임이 보내는 `GameCMDMelonLoader` 이벤트를 `Mods\GGBH_MOD.dll`(MelonMod)이 받아 처리한다. 즉 공식 코드 모드는 MelonLoader 위에서 도는 일반 관리 코드이며, **Harmony 패치와 MelonLoader API를 그대로 쓸 수 있다.**
- 게임 API는 전역 클래스 `g`를 통해 접근한다(`g.world`, `g.data`, `g.conf`, `g.ui`, `g.events`, `g.timer`, `g.res`, `g.mod` 등 관리자 14개).
- **판정: 공식 ModMain 경로 적합**(9절). 전제 조건 중 하나가 모드 ID를 에디터에서 발급받는 것이어서, 이번 단계의 빈 프로젝트는 임시 ID로 만들고 v0.1에서 실제 ID로 바꾼다.

## 2. 모드의 세 가지 구성 요소 (ModCode / ModExcel / ModAssets)

| 요소 | 폴더 | 하는 일 | 필요 도구 |
|---|---|---|---|
| 설정 | `ModExcel\` | 게임 설정 테이블의 행을 고치거나 새 행을 추가. xlsx 또는 JSON | 엑셀 |
| 자산 | `ModAssets\` | 폴더의 모든 파일이 그대로 함께 내보내진다. 코드에서 `g.mod.GetModPathRoot("<모드ID>")`로 경로를 얻는다 | — |
| 리소스 | `ModRes\ResBuildABProject\` | 프리팹·특효·모델·UI를 AssetBundle로 빌드. 같은 경로·이름이면 게임 원본 대신 로드된다 | Unity 2020.3.9 |
| 코드 | `ModCode\ModMain\` | C# DLL. 설정으로 안 되는 로직 | Visual Studio 2022, .NET Framework 4.7.2 |

프로젝트 폴더 배치(공식 교안과 공개 저장소 `gdjyhjs/DaguiBahuangMod`에서 확인):

```text
ModProject_<모드ID>\
├── ModProject\
│   ├── ModAssets\
│   ├── ModCode\ModMain\      # ModMain.sln, ModMain.csproj, ModMain.cs
│   ├── ModExcel\
│   ├── ModRes\ResBuildABProject\
│   ├── ModData.cache          # 에디터 UI로 만든 데이터와 excelMID, modNamespace
│   └── ModProject.cache       # soleID, 이름, 작성자, 버전, publishedFileID 등
└── debug\
    └── Mod_<모드ID>\          # 내보내기 결과: ModAssets, ModExcel(JSON), ModExportData.cache
```

`ModProject.cache` 예(공식 예제 `技能配置例子`): `{"soleID":"Ze4vmx", ..., "name":"武技配置例子", "ver":"v 1.0.0", "publishedFileID":0, "excelEncrypt":true}`. `ModData.cache`에는 `"excelMID":-1991658079`와 `"modNamespace":"MOD_Ze4vmx"`가 들어 있다.

## 3. 프로젝트 구조와 로드 규칙

**요구되는 네 값**

| 항목 | 값 | 근거 |
|---|---|---|
| 어셈블리 이름 | `MOD_<모드ID>` (예: `MOD_Ze4vmx.dll`) | 교안: "确保bin/Release目录下成功生成了MOD_XXXXXX.dll". 네임스페이스를 바꾸면 "程序集名称"도 함께 바꾸라는 안내 |
| 네임스페이스 | `MOD_<모드ID>`. 에디터 UI의 "代码命名空间"과 일치해야 함 | 교안: "命名空间默认为MOD_XXXXXX（模组ID）". `GGBH_MOD.dll`이 `<키>.ModMain` 타입을 찾는다 |
| 진입점 | 클래스 `ModMain`, 공개 인스턴스 메서드 `void Init()`와 `void Destroy()`. 매개변수 없음 | 교안: "主类名必须是ModMain". `GGBH_MOD.dll`이 `GetMethod("Init")`·`GetMethod("Destroy")`를 호출 |
| 대상 프레임워크 | .NET Framework 4.7.2 | 교안: "必须是这个版本". 템플릿 `ModMain.csproj`의 `v4.7.2`. `GGBH_MOD.dll` 자신도 4.7.2 |

배포 템플릿의 `ModMain.csproj`는 `AssemblyName`이 `ModMain`, 네임스페이스가 `ModNamespace`인 원형이다. 에디터가 "编写代码" 버튼을 처음 누를 때 이 원형으로 VS 프로젝트를 만들면서 모드 ID를 채운다.

**로드 절차** (`Mods\GGBH_MOD.dll`의 IL에서 확인)

1. `GGBH_MOD.ModMain`은 `MelonLoader.MelonMod`를 상속한다. 시작 시 `MelonLoader\GGBH_MOD_LOAD_COMPLETE.txt`를 쓴다.
2. Harmony 패치 `Patch_EventsMgr_Init.Postfix`가 `EventsMgr.Init` 뒤에 `g.events.On("GameCMDMelonLoader", ...)`로 이벤트를 구독한다.
3. 게임이 `cmd == "LoadDll"` 이벤트를 보내면 `values[0]`을 키(네임스페이스), `values[1]`을 DLL 경로로 삼아 `MelonHandler.LoadFromFile`과 `Assembly.LoadFrom`으로 로드하고 키별 목록에 보관한다.
4. `cmd == "InitModMain"`이면 먼저 이전 인스턴스들의 `Destroy`를 호출한 뒤, 이번에 로드 대상인 키마다 `assembly.GetType(키 + ".ModMain")` → `CreateInstance` → `Init` 호출 순으로 초기화한다. 타입을 못 찾으면 콘솔에 `找不到Type“<키>.ModMain”`을 출력한다.
5. `cmd == "DestroyModMain"`이면 `Destroy`를 호출한다.

템플릿 주석에 따르면 `Init`은 게임(세이브)에 진입할 때, `Destroy`는 주 화면으로 돌아갈 때 호출되고 그 뒤 `Init`이 다시 호출된다. **`Init`은 한 실행 중 여러 번 불린다.** 그래서 템플릿은 `Init`에서 기존 Harmony 인스턴스를 `UnpatchSelf()`한 뒤 다시 `PatchAll`한다. 한 번 로드된 DLL은 프로세스가 끝날 때까지 내려가지 않는다.

콘솔 메시지에 `没有继承ModMainBase`라는 문구가 있으나, 템플릿의 `ModMain`은 아무것도 상속하지 않고 호출은 리플렉션으로 이뤄진다.

**게임 쪽 관리자**: `ModMgr`(`g.mod`). 프록시에서 확인한 멤버: `pathModExportData`, `allModPaths`, `loadModNamespace`, `modDatas`, `debugModExportData`, `LoadAllMod()`, `LoadMod(String modExportDataFile)`, `GetModPathRoot(String soleID)`, `IsLoadMod(String soleID)`, `GetLoadOrderData()`, `SaveLoadOrderData(ModLoadData)`, `DeleteDLLFile()`.

## 4. 빌드·로컬 테스트·배포 절차

공식 절차(교안 `代码编写教程`, `配置修改教程`):

1. 게임 주 화면의 "模组" → "模组开发" → "新建项目". 에디터가 `ModProject_<모드ID>` 폴더와 ID를 만든다. "更改目录"로 프로젝트 위치를 바꿀 수 있다.
2. "编写代码"를 누르면 VS 프로젝트와 `ModMain` 클래스가 생긴다. 코드를 쓰고 **Release**로 다시 빌드한다. `bin\Release`에는 `MOD_<모드ID>.dll`만 있어야 한다. 참조 DLL의 "복사 로컬"은 반드시 `False`로 둔다. 여분의 DLL이 Release 폴더에 섞이면 모드가 동작하지 않는다.
3. 추가로 참조할 DLL은 게임 폴더의 `MelonLoader`에서 찾는다.
4. 에디터 주 화면 오른쪽 아래 "进入游戏调试"를 누르면 에디터가 Release 폴더의 DLL을 전부 게임으로 복사해 로드하고, 설정 테이블을 JSON으로 내보낸다(`debug\Mod_<모드ID>\`).
5. 디버그가 끝나면 에디터에서 창작마당에 업로드한다.

보조 사항:

- MelonLoader 콘솔은 기본으로 꺼져 있다. 게임 실행 인자 `--melonloader.showconsole`로 켠다.
- 창작마당 밖에서 배포되는 모드는 내보낸 폴더를 게임의 `ModExportData\`에 넣어 설치한다(18183의 설치 안내. `02a` 6절).
- 여러 모드의 로드 순서는 로컬 모드 목록의 위에서 아래이고, 아래 모드의 설정이 위 모드의 설정을 덮어쓴다.

**이 프로젝트의 빌드 방식**: 템플릿은 구형 csproj에 참조를 `dll\` 상대 경로로 두지만 배포본의 `dll\` 폴더는 비어 있다. 결과물은 평범한 4.7.2 DLL이므로 SDK 형식 csproj(`net472`)와 `dotnet build`로 만들어도 같다. 참조는 게임의 `MelonLoader\Managed`와 `MelonLoader\0Harmony.dll`을 직접 가리킨다. 공개 저장소 `4azuo/GuiGuBaHuang-ModLib`도 csproj의 MelonLoader 경로를 자기 게임 경로로 맞추는 방식을 쓴다.

## 5. Harmony 패치 가능 범위

확인된 사실:

- 공식 템플릿의 `Init`이 `new HarmonyLib.Harmony("...")`와 `PatchAll(Assembly.GetExecutingAssembly())`를 호출한다. 교안도 "使用了Harmony补丁功能的，需要手动启用补丁"라고 안내한다. 즉 **공식 경로가 Harmony 사용을 전제로 한다.**
- `GGBH_MOD.dll` 자체가 게임 클래스 `EventsMgr.Init`에 Postfix를 건다. IL2CPP 프록시 메서드에 대한 Postfix가 이 설치본의 MelonLoader 0.5.4에서 동작한다는 근거다.
- 패치 대상은 프록시 어셈블리(`Assembly-CSharp.dll`)에 드러난 타입·메서드다. 이 설치본의 프록시에는 타입 15,559개가 있다.

패치 없이 쓸 수 있는 확장 지점(공식 예제 `Example.cs`):

- **이벤트**: `g.events.On(EBattleType.UnitHitDynIntHandler, callback)`. 범주는 `EGameType`(공통), `EMapType`(대지도), `EBattleType`(전투). 콜백 인자 `ETypeData`를 `e.Cast<EBattleTypeData.UnitHitDynIntHandler>()`로 변환해 쓴다. 예제는 이 이벤트에서 `edata.dynV.baseValue = 0`으로 피해를 0으로 만든다.
- **매 프레임 호출**: `g.timer.Frame(new Action(...), 1, true)`와 `g.timer.Stop(...)`.
- **명령·조건 문자열**: `DramaFunctionTool.OptionsFunction("openUI_...")`, `BattleFunctionTool.OptionsFunction("createMonst_7210_0_0_3")`, `UnitConditionTool.Condition("grade_0_1_1", ...)`. 명령과 조건의 전체 목록은 `配置对照表.xlsx`의 `剧情条件及影响`, `条件说明表`, `副本作用功能` 시트에 있다.
- **단위 행동**: `unit.CreateAction(new UnitActionLuckAdd(120))`, `unit.CreateAction(new UnitActionRoleTrains(g.world.playerUnit))`.

IL2CPP 환경의 제약(MelonLoader 위키 "Il2Cpp differences"와 공식 예제의 주석):

- 게임 객체는 C#의 `as`나 직접 캐스트 대신 `Cast<T>()`/`TryCast<T>()`로 변환한다.
- 게임 메서드에 콜백을 넘길 때 `System.Action`을 `Il2CppSystem.Action`으로 변환한다(암시적 변환 또는 명시적 캐스트).
- 커스텀 `MonoBehaviour`는 `ClassInjector.RegisterTypeInIl2Cpp<T>()`로 먼저 등록하고, `IntPtr`을 받는 생성자를 둬야 `AddComponent`할 수 있다. 주입한 타입은 가상 메서드를 재정의할 수 없고, IL2CPP 쪽에 노출되는 멤버가 제한된다.
- 예제 주석: 게임의 클래스 대부분은 `MonoBehaviour`를 상속하지 않으므로 지연·반복 호출에는 `g.timer`를 쓴다.
- 이 설치본은 MelonLoader 0.5.4여서 네임스페이스가 `UnhollowerRuntimeLib`/`UnhollowerBaseLib`이다. 최신 MelonLoader 문서의 `Il2CppInterop.*` 네임스페이스는 해당하지 않는다.

## 6. 설정 테이블

**위치와 형식**

| 자료 | 경로 (`Mod\modFQA\配置修改教程\` 기준) | 내용 |
|---|---|---|
| 머리글 | `配置表头\<표이름>.xlsx` (761개) | 1행 중국어 설명, 2행 필드명, 3행 타입(`int`, `string`, `int[]`, `int[][]`). 모드 작성 시 이 파일을 복사해 4행부터 채운다 |
| 내용 사본 | `配置（只读）Json格式\<표이름>.json` (761개, 합계 99 MB) | 게임 원본 설정의 읽기 전용 JSON. 행 객체의 배열이고 모든 값이 문자열이다 |
| 묶음 사본 | `配置（只读）\<중국어 이름>.xlsx` (128개) | 주제별로 여러 표를 묶은 엑셀(예: `角色配置表.xlsx`, `道具配置表.xlsx`, `技能境界配置表.xlsx`) |
| 대조표 | `配置对照\配置对照表.xlsx` | 시트 10개: `剧情条件及影响`(명령), `条件说明表`(조건), `副本作用功能`, `效果说明表`(`RoleEffect`), `引导触发对照表`, `字体颜色`, `单位行为触发监听对照表`, `宗门门规配置`, `装饰物ID对照表`, `道号单位行为触发监听` |
| 효과 대조표 | `配置对照\效果配置对照表.xlsx` | 전투 효과(`BattleEffect`) 매개변수 설명 |

예: `RoleGrade.json`(경지 표)의 필드는 `id`, `grade`, `gradeName`, `phase`, `phaseName`, `quality`, `exp`, `condition`, `growItemRequire`, `growAddLife`, `abilityPoint`, `atkUp`, `defUp`, `hpUp`, `mpUp`, `spUp`, `maxLimitCount`, `failFeatureProb`, `dungeonID` 등이다.

게임 본체의 설정 원본은 게임 데이터 자산 안에 있고(예제 `ModProject.cache`의 `"excelEncrypt":true`가 내보내기 시 암호화 옵션을 가리킨다), 런타임에는 `g.conf`(`ConfMgr`)로 읽는다. 예: `g.conf.roleGrade.GetGradeItem(1, 1)`이 `ConfRoleGradeItem`을 돌려준다. 프록시의 `GameConfPack2.dll`, `GameConfPack3.dll`과 원본의 관계는 확인하지 못했다.

**수정 규칙** (교안 `配置修改教程`)

- 기존 행 수정: 게임과 같은 ID를 쓰고 바꿀 필드만 채운다. 나머지는 비워 둔다.
- 새 행 추가: ID를 `MID&<번호>` 형식으로 쓴다. 에디터가 프로젝트마다 무작위 int(`excelMID`, ±21억 범위에서 게임이 쓰는 -100만~1억 구간 제외)를 정하고, 내보낼 때 `excelMID + 번호`를 실제 ID로 만든다. 다른 표를 참조하는 필드도 `MID&101|MID&102`처럼 쓴다. `MID&` 표기는 xlsx에서만 쓸 수 있고 JSON에서는 쓸 수 없다.
- 표의 ID는 문자열이 아니라 int여서 모드 간 충돌이 날 수 있다. 번호는 1만 이하의 연속 값이 권장된다.
- DLC 관련 표는 `DLC` 접두가 붙은 머리글을 우선 쓴다(전투 관련 표가 해당).
- 문자열 필드는 국제화 표(`LocalText`) 키를 넣는 것이 원칙이나, 다국어가 필요 없으면 문장을 직접 넣어도 된다.

**설정만으로 되는 일과 코드가 필요한 일**

| 설정(`ModExcel`)만으로 가능 | 코드(`ModCode`)가 필요 |
|---|---|
| 기운(선천·후천·역천개명) 추가, 효과 수치 조정 | 세이브에 저장된 개체의 값을 임의 시점에 읽고 쓰기 |
| 아이템·좌기·NPC·문파·기연·극·임무·부본 추가 | 검색·목록·편집 UI |
| 기술 수치, 경험치 배율, 드랍, 상점 품목 조정 | 단축키 입력 처리 |
| 기연·극 선택지에 명령을 걸어 아이템 지급, 속성 증가 등 실행 | 전투 중 피해·쿨타임 가로채기(이벤트 구독 또는 Harmony) |
| 경지 이름·수치 변경(`RoleGrade`) | 게임 규칙 자체의 변경, 백업·변경 이력 |

창작마당에는 코드 없이 기연·극 설정만으로 치트 메뉴를 만든 모드가 있다(`02a` 5절, 3400202000). 이 프로젝트의 핵심인 조회·편집기는 코드가 필요하다.

## 7. UI 주입 방법과 한계

확인된 방법:

1. **게임의 기존 UI 열기**: `g.ui.OpenUI(UIType.PlayerInfo)`, `UITipItem.AddTip("...")`(토스트), `g.ui.OpenUI<UICheckPopup>(UIType.CheckPopup).InitData(제목, 내용, 버튼 수, 콜백)`(확인 창), `UITextInfo`, `UITextInfoLong`(텍스트 창). 코드만으로 쓸 수 있다.
2. **자체 UI 프리팹**: Unity 2020.3.9로 프리팹을 만들어 AssetBundle로 빌드하고 `g.ui.OpenUI(new UIType.UITypeBase("프리팹 이름", UILayer.UI))`로 연다. UI 스크립트는 `UIBase`를 상속해야 하고, 모드 DLL은 동적으로 로드되므로 프리팹에 스크립트를 미리 붙일 수 없어 코드에서 `AddComponent`한 뒤 `Find`로 자식 컴포넌트를 찾는다(교안 `资源修改教程` 예제 10).
3. **게임 UI 프리팹 재사용**: 게임의 UI 프리팹 목록이 `资源修改教程\Resources（资源对照）\UI（游戏UI预制体）`에 있다. 공개 모드 `修改器`(`qqty012/ggbh_mod`)는 README에서 "UI 프리팹으로 화면을 만든다"고 밝힌다.
4. **극(剧情) UI**: `DramaTool.OpenDrama(id, new DramaData{...})` 또는 `UICustomDramaDyn`으로 선택지 창을 띄우고 `SetOptionCall`로 버튼 콜백을 건다. `dramaData.dialogueOptionsAddText[...]`로 버튼을 동적으로 추가할 수 있다. 프리팹 없이 메뉴형 UI를 만드는 가장 가벼운 방법이다.
5. **기존 화면에 버튼 추가**: 창작마당 모드들이 기술 화면, 문파 영각 화면에 버튼을 추가한다(`02a` 8절). 구현 방식은 소스를 열지 않아 확인하지 못했다.

한계와 미확인:

- 자체 UI 프리팹을 만들려면 Unity 에디터 2020.3.9와 AssetBundle 빌드가 필요하다.
- IMGUI(`OnGUI`)는 프록시에 `UnityEngine.IMGUIModule.dll`이 있다는 점만 확인했다. 프록시 생성 로그에 "메서드 1,311개 복원 실패"가 있어, IMGUI가 이 빌드에서 동작하는지는 런타임에서 확인해야 한다.
- 키 입력을 받는 방법(`g.input`의 `InputMgr`, Unity `Input`)은 확인하지 못했다.
- 여러 모드가 쓰는 공용 UI 프레임워크(`【大鬼】UI框架`)가 있으나 저장소에 라이선스가 없어 코드 재사용은 할 수 없다. 의존 모드로 요구하는 것은 가능하지만 이 프로젝트에서는 택하지 않는다.

## 8. 독립 MelonMod 경로와의 비교

| 기준 | 공식 ModMain | 독립 MelonMod |
|---|---|---|
| 로드 방식 | 게임의 모드 시스템이 `GGBH_MOD.dll`을 통해 로드. 세이브 진입 시 `Init` | `Mods\`에 넣은 DLL을 MelonLoader가 게임 시작 시 로드 |
| 개입 시점 | 세이브 진입 이후(`Init`). 주 화면 단계에는 개입하지 못한다 | 게임 시작부터 |
| Harmony | 사용 가능(템플릿이 전제) | 사용 가능 |
| 설정 테이블·리소스 | 한 프로젝트에서 `ModExcel`·`ModRes`와 함께 관리, 에디터가 내보내기 | 직접 처리해야 함 |
| 켜고 끄기, 순서 | 게임의 모드 목록에서 관리 | 파일을 넣고 빼야 함 |
| 배포 | 창작마당 업로드, 또는 내보낸 폴더를 `ModExportData`에 복사 | 파일 배포만 가능 |
| 선행 사례 | 창작마당 모드 대부분과 공개 저장소 3곳 | Nexus의 ChatAi 한 건만 확인 |
| 제약 | 모드 ID·네임스페이스 규칙, 에디터 절차, `Init`의 반복 호출 | 게임 모드 시스템과의 통합을 직접 구현 |

이 프로젝트의 기능(세이브에 진입한 뒤의 조회·편집)은 주 화면 단계의 개입이 필요 없다.

## 9. 판정

판정: 공식 ModMain 경로 적합

근거:

1. 로드 메커니즘이 정적으로 확인됐다. `GGBH_MOD.dll`이 `<네임스페이스>.ModMain`을 찾아 `Init`/`Destroy`를 호출하며, 필요한 규약 네 값이 모두 명확하다(3절).
2. Harmony 패치가 공식 템플릿의 전제이고, `GGBH_MOD.dll` 자체가 게임 클래스에 Postfix를 거는 실례다(5절).
3. 조회·편집에 필요한 진입점이 공식 예제에 나와 있다. 플레이어(`g.world.playerUnit`), NPC(`g.world.unit.GetUnit(id)`), 세이브 데이터(`g.data`), 설정(`g.conf`), 이벤트(`g.events`), UI(`g.ui`)다.
4. 같은 종류의 모드(통합 수정기)가 이 경로로 여러 개 만들어져 있고, 2026년 8월에 나온 것도 있다(`02a` 5절).
5. 독립 MelonMod의 유일한 이점인 이른 개입 시점이 이 프로젝트에는 필요 없다(8절).

적합 판정의 전제 조건(정적 자료로 확정하지 못한 것):

- **모드 ID 발급**: 실제 모드 ID와 `ModProject.cache`·`ModData.cache`는 게임 내 모드 에디터가 만든다. 게임 실행이 필요하므로 이번 범위 밖이다. Task 4의 빈 프로젝트는 임시 ID를 쓰고, v0.1 시작 시 에디터에서 프로젝트를 만들어 그 ID로 바꾼다.
- **수작업 프로젝트의 인식**: 에디터가 만들지 않은 csproj(SDK 형식)의 산출물을 "进入游戏调试"가 그대로 복사·로드하는지는 런타임 확인이 필요하다. 산출물이 `bin\Release\MOD_<모드ID>.dll` 하나라는 조건은 맞춘다.
- **UI 방식**: 편집기 UI를 극 UI·게임 UI 프리팹 재사용·자체 프리팹·IMGUI 중 무엇으로 만들지는 v0.1에서 런타임 확인 후 정한다(7절).

## 10. 미확인 항목과 v0.1에서 검증할 것

| 항목 | 현재 상태 | 검증 방법 |
|---|---|---|
| 모드 에디터가 만드는 프로젝트의 실제 위치와 ID | 미확인 | 에디터에서 프로젝트 생성 후 폴더 확인 |
| SDK 형식 csproj 산출물의 디버그 로드 | 미확인 | "进入游戏调试" 후 콘솔의 `InitModMain：MOD_<ID>` 출력 확인 |
| `Init`·`Destroy`의 호출 시점과 횟수 | 템플릿 주석으로만 확인 | 로그 출력으로 확인 |
| 키 입력 처리 방법 | 미확인 | `g.input`(`InputMgr`) 멤버 조사 후 시험 |
| IMGUI 동작 여부 | 미확인 | `OnGUI`를 가진 주입 컴포넌트로 시험 |
| 게임 UI 프리팹 재사용 방법 | 선행 사례의 README로만 확인 | `g.ui.OpenUI`와 프리팹 구조 조사 |
| `ModExportData\`에 넣은 모드의 인식 | 서드파티 설치 안내로만 확인 | `ModMgr.pathModExportData` 값 확인 |
| 인라인된 메서드 등 Harmony가 걸리지 않는 대상 | 미확인 | 패치 대상마다 개별 확인 |
| `GameConfPack2/3.dll`의 역할 | 미확인 | 04 문서의 메타데이터 분석 |
| 이 설치본에서의 창작마당 업로드 | 불가(01 문서 11절) | 정식 설치본 필요 |

### 런타임 검증 결과 (2026-10-12)

위 표의 일부를 게임에서 확인했다.

| 항목 | 결과 |
|---|---|
| 모드 에디터가 만드는 프로젝트의 위치와 ID | 에디터에서 지정한 작업 폴더 아래 `ModProject_<ID>\`에 생긴다. 이 프로젝트는 `E:\TlToybox\ModProject_sWP6UL\`, ID는 `sWP6UL`. 작업 폴더는 레지스트리 `HKCU\Software\guigugame\guigubahuang`의 `ModTool_modProjectRootPath`에 저장된다 |
| 에디터가 생성하는 코드 프로젝트 | `ModProject\ModCode\ModMain\`에 `ModMain.csproj`(`AssemblyName`·`RootNamespace`가 `MOD_sWP6UL`), `ModMain.cs`, `Example\`, 그리고 `dll\Assembly-CSharp.dll`(프록시 사본 45 MB)이 생긴다 |
| SDK 형식 csproj 산출물의 디버그 로드 | **동작한다.** `bin\Release\MOD_sWP6UL.dll`을 "게임 디버깅 시작"이 `ModProject_sWP6UL\debug\Mod_sWP6UL\ModCode\dll\`로 복사해 로드했다. 로그에 `No Compatibility Layer for ...MOD_sWP6UL.dll` 경고가 한 줄 나오지만 로드와 실행에는 지장이 없었다 |
| 게임 폴더로의 복사 | 디버그 로드는 게임 폴더에 아무것도 쓰지 않는다. 모드는 프로젝트의 `debug\` 폴더에서 직접 로드된다 |
| `Init` 호출 시점 | "게임 디버깅 시작"을 누른 직후, **세이브를 불러오기 전**에 한 번 호출된다. 이때 `g.world`는 있지만 `g.world.isIntoWorld`가 `false`다. 세이브를 불러와 세계에 들어갈 때 `Init`이 다시 호출되지는 않았다 |
| `g.timer.Frame` | `Init`에서 건 타이머가 세이브 로드를 거쳐 세계 진입 후까지 유지됐다 |
| DLL 교체 | 한 번 로드된 DLL은 게임을 끄기 전에는 교체되지 않는다. 코드를 고치면 게임을 재시작해야 한다 |
| 디버그 시작 후 안내 | "모드 가져오기 성공. '새 저장' 혹은 '예전 저장으로 진입'로 디버그를 진행해 주십시오." 알림이 뜨고 주 화면으로 돌아간다 |

아직 확인하지 못한 것: `Destroy`의 호출 시점, 주 화면으로 돌아갔다가 다시 들어갈 때의 `Init` 재호출, 키 입력, IMGUI, UI 프리팹 재사용, `ModExportData\` 설치 경로.

### 개발용 조작 도구

게임 UI는 실제 마우스 커서 위치를 읽기 때문에 창에 클릭 메시지(`WM_LBUTTONDOWN`)를 보내는 방식으로는 조작되지 않았다. 대신 게임 프로세스 안에서 UI 클릭을 수행하는 개발 전용 MelonMod(`tools/DevRemote`)를 만들었다. 명령 파일(`.devremote\cmd.txt`)의 `click x y`를 읽어 `EventSystem.RaycastAll`로 대상을 찾고 `Button.onClick`을 호출한다. 창 내용은 `PrintWindow`로 캡처한다. 게임 창이 다른 창 뒤에 있어도 동작하고 실제 마우스·키보드를 쓰지 않는다. 이 도구는 게임의 `Mods\TlToyboxDevRemote.dll`로 설치되어 있으며 배포 대상이 아니다.

## 11. 출처

로컬 자료(`E:\Games\Tale of Immortal\` 기준):

- `Mod\modFQA\代码编写教程\代码编写教程.docx`, `ModMain\ModMain.cs`, `ModMain\ModMain.csproj`, `ModMain\Example\Example.cs`
- `Mod\modFQA\配置修改教程\配置修改教程.docx`, `配置表头\RoleGrade.xlsx`, `配置（只读）Json格式\RoleGrade.json`, `配置对照\配置对照表.xlsx`
- `Mod\modFQA\资源修改教程\资源修改教程.docx`
- `Mod\modFQA\鬼谷八荒MOD使用指南基础版 1.0\鬼谷八荒MOD使用指南基础版 1.0.docx`
- `Mod\modFQA\MOD模板例子\` 아래 `ModProject.cache`, `ModData.cache`, `说明.txt`, `debug\Mod_BpOnHa\`
- `Mods\GGBH_MOD.dll`(IL 확인), `MelonLoader\Managed\Assembly-CSharp.dll`(메타데이터 확인)

웹:

- MelonLoader 위키 "Il2Cpp differences": https://melonwiki.xyz/#/modders/il2cppdifferences (원문: https://github.com/lavagang/melonwiki/blob/master/docs/modders/il2cppdifferences.md)
- MelonLoader 저장소: https://github.com/LavaGang/MelonLoader
- 공개 모드 저장소: https://github.com/4azuo/GuiGuBaHuang-ModLib , https://github.com/gdjyhjs/DaguiBahuangMod , https://github.com/qqty012/ggbh_mod
- 서드파티 모드 설치 안내: https://www.18183.com/xiazai/bd_22808.html
- 창작마당 조사 결과: `02a-workshop-survey.md`
