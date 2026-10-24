# 01. 게임 구조 확인

> 조사일: 2026-10-12 · 대상: `E:\Games\Tale of Immortal` · 방법: 읽기 전용 조사

## 1. 요약

- 게임은 Unity 2020.3.9(중국판 `f1c1`) IL2CPP x64 빌드이고, 게임 버전은 **v1.2.113.259**다.
- 모드 로더는 게임에 포함된 **MelonLoader v0.5.4**다. 공식 모드 시스템은 `Mods\GGBH_MOD.dll`을 통해 MelonLoader 위에서 동작한다.
- IL2CPP 프록시 어셈블리(`MelonLoader\Managed\Assembly-CSharp.dll`, 45.6MB)가 이미 생성되어 있어 정적 분석에 쓸 수 있다.
- 공식 모드 제작 자료가 `Mod\modFQA`에 들어 있다. 코드 템플릿, API 문서(chm), 교안 4종, 예제 프로젝트 4종, **설정 테이블 761종의 머리글(xlsx)과 읽기 전용 JSON 사본**이 포함된다.
- 현재 설치본(`E:\Games\Tale of Immortal`)은 Steam 라이브러리 밖에 있고, `steam_api64.dll` 옆에 Steam 에뮬레이터 설정 파일 `tenoke.ini`가 있다. Steam 매니페스트와 창작마당 구독 폴더가 연결되어 있지 않아 **Steam 빌드 ID는 확인할 수 없고, 이 설치본으로는 창작마당 구독·업로드를 할 수 없다.**
- 같은 PC에 **Steam 설치본을 쓰던 흔적**이 있다. 2026-10-04의 `Player-prev.log`에 따르면 게임이 `E:\SteamLibrary\steamapps\common\鬼谷八荒`에서 같은 버전(v1.2.113.259)으로 실행됐고 창작마당 모드 204개를 로드했다. 지금 그 폴더에는 게임 파일이 없고 모드가 만든 파일만 남아 있다(12절).
- 설치된 외부 모드는 없다(`Plugins`, `UserLibs`, `ModExportData` 비어 있음).

## 2. 버전 정보

| 항목 | 값 | 근거 |
|---|---|---|
| 게임 버전 | v1.2.113.259 | `%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\Player.log`의 `游戏版本：v1.2.113.259` |
| 게임이 인식한 플랫폼 | Steam | 같은 줄의 `平台：Steam` |
| Steam appid | 1468810 | `tenoke.ini`의 `id = 1468810` |
| Steam 빌드 ID | 미확인 | 11절 참고 |
| Unity | 2020.3.9f1c1 (ef65345766d1) | `Player.log`의 `Initialize engine version`. 실행 파일 버전은 `2020.3.9.15689012` |
| 스크립팅 백엔드 | IL2CPP, x64 | `MelonLoader\Latest.log`의 `Game Type: Il2Cpp`, `Game Arch: x64` |
| MelonLoader | v0.5.4 Open-Beta | `Latest.log`, `MelonLoader.dll` 파일 버전 0.5.4 |
| MelonLoader 프록시(`version.dll`) | 0.4.4.0 | 파일 버전 정보 |
| Cpp2IL | 2022.1.0-pre-release.3 | `Latest.log`의 `Using Dumper Version` |
| Il2CppAssemblyUnhollower | 0.4.18.0 | `Latest.log` |
| Unity Dependencies | 2020.3.9 | `Latest.log` |
| `GGBH_MOD.dll` | 1.0.0.0 (8,192바이트) | 파일 버전 정보 |
| Steamworks.NET | 2024.8.0 (Steamworks SDK 1.60) | `guigubahuang_Data\Plugins\Steamworks.NET.txt` |

MelonLoader 로그는 게임 이름과 버전을 읽지 못했다(`Game Name: UNKNOWN`, `Game Version: 0`, `Failed to Initialize Assets Manager!`). 게임 버전은 Unity 플레이어 로그에서 확인했다.

설치 폴더의 파일 대부분은 수정 시각이 2020-02-02로 일괄 설정되어 있어, 파일 시각으로는 빌드 시점을 알 수 없다.

## 3. 최상위 폴더·파일 역할

| 경로 | 역할 | 크기 |
|---|---|---|
| `guigubahuang.exe` | Unity 플레이어 실행 파일 | 0.6 MB |
| `UnityPlayer.dll` | Unity 엔진 런타임 | 28.0 MB |
| `GameAssembly.dll` | IL2CPP로 컴파일된 게임 코드 전체 | 120.6 MB |
| `baselib.dll` | Unity 기반 라이브러리 | 0.5 MB |
| `version.dll` | MelonLoader 부트스트랩 프록시. 게임 시작 시 로더를 주입한다 | 0.05 MB |
| `NOTICE.txt` | MelonLoader 라이선스 고지(Apache-2.0, Lava Gang) | — |
| `guigubahuang_Data\` | 게임 데이터 자산(66개 파일) | 27,001 MB |
| `MelonLoader\` | 모드 로더 본체와 생성된 프록시 어셈블리(430개 파일) | 315 MB |
| `Mod\` | 공식 모드 제작 자료 `modFQA`(20,564개 파일) | 517 MB |
| `Mods\` | MelonLoader가 로드하는 Mod DLL. `GGBH_MOD.dll` 하나 | — |
| `Plugins\` | MelonLoader Plugin DLL. 비어 있음 | — |
| `UserLibs\` | MelonLoader 사용자 라이브러리. 비어 있음 | — |
| `UserData\` | MelonLoader 설정(`MelonPreferences.cfg`는 0바이트, 시작 화면 테마 설정) | — |
| `ModExportData\` | 비어 있음. 용도는 02 문서에서 다룬다 | 0 |
| `guigubahuang - FreeSoundtrack\` | 사운드트랙(91개 파일) | 290 MB |
| `_CommonRedist\` | .NET Framework 4.7.2, VC++ 2019 재배포 설치 파일 | 108 MB |

## 4. 엔진과 스크립팅 백엔드

- 게임 코드는 IL2CPP로 네이티브 컴파일되어 `GameAssembly.dll`에 들어 있고, 메타데이터는 `guigubahuang_Data\il2cpp_data\Metadata\global-metadata.dat`(40.5 MB)에 있다. 원본 C# 어셈블리는 배포되지 않는다.
- `guigubahuang_Data\ScriptingAssemblies.json`에 게임이 포함한 어셈블리 이름 목록이 있다.
- `guigubahuang_Data\RuntimeInitializeOnLoads.json`에 따르면 시작 시 호출되는 게임 코드는 `SteamManager.InitOnPlayMode`와 `FairyChickenSystem.Init`이다(둘 다 `Assembly-CSharp`, 네임스페이스 없음).
- `Player.log`의 스택에는 `g:Start()`, `game:Start()`가 나온다. 전역 클래스 이름이 소문자 `g`, `game`이다.
- `_CommonRedist`에 .NET Framework 4.7.2가 포함되어 있다. 공식 코드 템플릿의 대상 프레임워크(`v4.7.2`)와 같다.

## 5. MelonLoader 구성

| 경로 | 내용 |
|---|---|
| `MelonLoader\MelonLoader.dll` | 로더 본체(0.5.4) |
| `MelonLoader\0Harmony.dll` | Harmony 패치 라이브러리 |
| `MelonLoader\Mono.Cecil*.dll`, `MonoMod.*.dll` | 어셈블리 조작·런타임 디투어 |
| `MelonLoader\AssetsTools.NET.dll`, `AssetRipper.VersionUtilities.dll`, `Tomlet.dll` | 자산·버전·설정 유틸리티 |
| `MelonLoader\Dependencies\Il2CppAssemblyGenerator\` | Cpp2IL, Il2CppAssemblyUnhollower, Unity 의존성 |
| `MelonLoader\Dependencies\MonoBleedingEdge.x64\` | 모드 코드를 실행하는 Mono 런타임 |
| `MelonLoader\Dependencies\SupportModules\Il2Cpp.dll` | IL2CPP 지원 모듈 |
| `MelonLoader\Managed\` | 생성된 프록시 어셈블리와 Mono 기본 라이브러리 |
| `MelonLoader\Latest.log`, `MelonLoader\Logs\` | 실행 로그(10개 보관) |
| `MelonLoader\GGBH_MOD_LOAD_COMPLETE.txt` | 0바이트 표식 파일 |

`MelonLoader\Managed`의 주요 프록시 어셈블리:

| 파일 | 크기 | 내용 |
|---|---|---|
| `Assembly-CSharp.dll` | 45.6 MB | 게임 코드 프록시 |
| `Assembly-CSharp-firstpass.dll` | 1.7 MB | 서드파티·플러그인 코드 프록시 |
| `GameConfPack2.dll`, `GameConfPack3.dll` | 28 KB, 22 KB | 게임 전용 어셈블리 |
| `Il2Cppmscorlib.dll`, `Il2CppSystem*.dll` | — | IL2CPP 쪽 기본 라이브러리 프록시 |
| `UnhollowerBaseLib.dll`, `UnhollowerRuntimeLib.dll` | — | 프록시 런타임 |
| `UnityEngine.*.dll`, `Unity.*.dll` | — | Unity 모듈 프록시 |
| `DOTween*.dll`, `Il2CppNewtonsoft.Json.dll` | — | 게임이 쓰는 서드파티 라이브러리 프록시 |

마지막 실행(2026-10-11 21:15)에서 `Assembly Generation Needed!`가 떠서 프록시를 다시 생성했다. 생성 통계: 필드 727개 복원·33개 실패, 메서드 17,442개 복원·1,311개 실패, IL 본문 7,936개 성공·3,311개 실패.

`MelonLoader\Logs`에는 2023-02-08 날짜의 로그 9개와 2026-10-11 로그 1개가 있다.

## 6. 설치된 모드·플러그인·DLC

**MelonLoader Mod / Plugin**

| 위치 | 내용 |
|---|---|
| `Mods\` | `GGBH_MOD.dll` 하나(공식 모드 브리지) |
| `Plugins\` | 없음 (`Latest.log`: `No Plugins Loaded!`) |
| `UserLibs\` | 없음 |

**게임 공식 모드**: 설치 폴더 안에는 `Mod\modFQA`(제작 자료) 외에 설치된 모드가 없다. 세이브 폴더에 `Mod_LoadData_backup_20261004_claude.bin`이 있어 모드 로드 정보가 세이브 쪽에 저장된다는 점을 알 수 있다. 구독·로컬 모드의 저장 위치는 02 문서에서 다룬다.

**DLC**: `tenoke.ini`의 `[DLC]` 절에 다음이 등록되어 있다.

| appid | 이름 |
|---|---|
| 2408730 | 鬼谷八荒 - 不归玄境 |
| 3294480 | Tale of Immortal - BaHuang Chronicles Digital Art Book |
| 3326620 | Tale of Immortal - Five Blooms |
| 1536810 | 鬼谷八荒 Soundtrack |

## 7. 모드 관련 경로

| 경로 | 용도 | 상태 |
|---|---|---|
| `E:\Games\Tale of Immortal\Mods\` | MelonLoader Mod DLL | `GGBH_MOD.dll` |
| `E:\Games\Tale of Immortal\Plugins\` | MelonLoader Plugin DLL | 비어 있음 |
| `E:\Games\Tale of Immortal\UserLibs\` | Mod가 공유하는 라이브러리 | 비어 있음 |
| `E:\Games\Tale of Immortal\UserData\` | MelonLoader 설정 | 기본값 |
| `E:\Games\Tale of Immortal\Mod\modFQA\` | 공식 모드 제작 자료 | 8절 참고 |
| `E:\Games\Tale of Immortal\ModExportData\` | — | 비어 있음 |
| `E:\Games\Tale of Immortal\MelonLoader\Managed\` | 모드 프로젝트가 참조할 프록시 어셈블리 | 생성되어 있음 |
| `E:\Games\Tale of Immortal\MelonLoader\0Harmony.dll` | 모드 프로젝트가 참조할 Harmony | 있음 |
| `E:\Games\Tale of Immortal\Mod\modFQA\配置修改教程\配置（只读）Json格式\` | 설정 테이블의 읽기 전용 JSON 사본 761개 | 있음 |
| `E:\Games\Tale of Immortal\Mod\modFQA\配置修改教程\配置表头\` | 설정 테이블 머리글 xlsx 761개 | 있음 |
| Steam 창작마당 구독 폴더(`steamapps\workshop\content\1468810`) | 구독한 모드 | 없음 |

공식 예제의 `ModAssets` 안내문에 따르면 모드 코드에서 모드 루트 경로는 `g.mod.GetModPathRoot("六位数的随机模组ID")`로 얻는다. 모드마다 여섯 자리 ID가 부여되며, 예제의 `debug` 폴더에 `Mod_BpOnHa` 형태의 폴더가 있다.

## 8. 공식 modFQA 자료 목록

| 경로 (`Mod\modFQA\` 기준) | 종류 | 용도 |
|---|---|---|
| `鬼谷八荒MOD使用指南基础版 1.0\鬼谷八荒MOD使用指南基础版 1.0.docx` | docx (219.5 MB) | 모드 에디터 기본 사용 안내서 |
| `代码编写教程\代码编写教程.docx` | docx (0.36 MB) | 코드 모드 작성 교안 |
| `代码编写教程\GGBH_API.chm` | chm (93.8 MB) | 게임 API 문서 |
| `代码编写教程\ModMain\ModMain.sln`, `ModMain.csproj` | VS 프로젝트 | 코드 모드 템플릿(.NET Framework 4.7.2, 참조는 `dll\` 상대 경로) |
| `代码编写教程\ModMain\ModMain.cs` | C# | 진입점 클래스 `ModNamespace.ModMain`(`Init`, `Destroy`) |
| `代码编写教程\ModMain\Example\Example.cs` | C# (10 KB) | API 사용 예제 |
| `代码编写教程\ModMain\dll\`, `bin\` | 폴더 | 비어 있음. 참조 DLL이 들어 있지 않다 |
| `配置修改教程\配置修改教程.docx` | docx | 설정 테이블 수정 교안 |
| `配置修改教程\配置表头\*.xlsx` | xlsx 761개 | 테이블별 머리글(필드 구조) |
| `配置修改教程\配置（只读）Json格式\*.json` | json 761개 | 테이블 내용의 읽기 전용 사본 |
| `配置修改教程\配置（只读）\*.xlsx` | xlsx 128개 | 중국어 이름의 테이블 묶음(예: `角色配置表.xlsx`) |
| `配置修改教程\配置对照\配置对照表.xlsx` | xlsx (6.1 MB) | 테이블 대조표 |
| `配置修改教程\配置对照\效果配置对照表.xlsx` | xlsx | 효과 설정 대조표 |
| `资源修改教程\资源修改教程.docx` | docx | 리소스 수정 교안 |
| `资源修改教程\ResBuildABProject\`, `ResBuildABProject_Example\` | Unity 프로젝트 | AssetBundle 빌드용 프로젝트와 예제 |
| `资源修改教程\Resources（资源对照）\` | 폴더 | 리소스 대조 자료. 하위: `Battle`, `Effect`, `Game`, `Map`, `Material`, `Sounds`, `SpriteAtlas`, `Texture`, `UI`(게임 UI 프리팹) |
| `MOD模板例子\技能配置例子\ModProject\` | 예제 | 기술 설정(`ModExcel`의 `Battle*.xlsx`) |
| `MOD模板例子\DLC修改配置例子\ModProject\`, `debug\` | 예제 | DLC 설정 수정. DLC 관련 표는 `DLC` 접두 머리글을 우선 쓴다는 안내 포함 |
| `MOD模板例子\替换游戏UI、插图例子\ModProject\`, `debug\` | 예제 | UI·삽화 교체 |
| `MOD模板例子\制作“静态和动态”立绘例子\ModProject\` | 예제 | 정적·동적 입화 제작(Unity 필요, `RoleDress` 표 수정) |

예제 `ModProject`의 구성은 `ModAssets\`, `ModExcel\`, `ModData.cache`, `ModProject.cache`, `说明.txt`다.

## 9. 세이브·설정 파일 위치

루트: `C:\Users\deepe\AppData\LocalLow\guigugame\guigubahuang\` (`guigubahuang_Data\app.info`의 회사명 `guigugame`, 제품명 `guigubahuang`과 일치)

| 경로 | 내용 |
|---|---|
| `Player.log`, `Player-prev.log` | Unity 플레이어 로그 |
| `Steam\CacheData\` | 현재 세이브. `DataGloble.cache`(전역 데이터), `steam_autocloud.vdf`, 슬롯 폴더 |
| `Steam\CacheData\1_fjSwpw\` | 세이브 슬롯 하나. Base64 형태 이름의 `.cache` 파일 13개와 `CacheData`, `CacheFixVer` |
| `Steam\CacheData\cacheSimple\` | 기본 세이브 틀(게임의 `StreamingAssets\cacheSimple`과 같은 파일 구성) |
| `Steam\CacheData备份的存档\`, `Steam\CacheData新的备份存档\`, `Steam\CacheData_SchoolVerBackup\`, `Steam\CacheDataTempguigubahuang\` | 게임이 만드는 백업·임시 세이브 |
| `Steam\CacheData_backup_20261004_claude\`, `Steam\Mod_LoadData_backup_20261004_claude.bin` | 2026-10-04에 만들어진 수동 백업 |
| `Steam\DaguiToolCmdData.cache` | 용도 미확인 |
| `Steam\DebugLog\` | 게임 디버그 로그 |
| `Steam\手动备份的存档请不要放在CacheData文件夹里面！！！\` | 안내용 폴더(수동 백업을 `CacheData` 안에 두지 말라는 뜻) |
| `data\` | 무작위 이름의 하위 폴더와 `.cache`/`.bak`/`.bin`/`.log` 파일. 용도 미확인 |

세이브 파일의 내용은 열어 보지 않았다. 파일 이름이 Base64 형태이고 확장자가 `.cache`라는 점만 확인했다.

## 10. 데이터 자산 구성

| 경로 (`guigubahuang_Data\` 기준) | 크기 | 내용 |
|---|---|---|
| `resources.assets.resS` | 25,537 MB | 텍스처·오디오 등 스트리밍 리소스 |
| `resources.assets` | 1,764 MB | `Resources` 폴더 자산(프리팹, 설정 등) |
| `resources.resource` | 893 MB | 오디오·비디오 리소스 |
| `globalgamemanagers` | 60 MB | 엔진 전역 설정 |
| `globalgamemanagers.assets`(+`.resS`) | 1.5 MB | 전역 자산 |
| `level0` ~ `level4` | 각 4~13 KB | 씬 5개 |
| `sharedassets0` ~ `sharedassets4.assets` | — | 씬 공유 자산 |
| `il2cpp_data\Metadata\global-metadata.dat` | 40 MB | IL2CPP 메타데이터 |
| `Plugins\x86_64\steam_api64.dll` | 2.0 MB | Steam API DLL(`Steamworks.NET.txt`에 적힌 정품 크기 300,392바이트와 다르다) |
| `Plugins\x86_64\tenoke.ini` | 82 KB | Steam 에뮬레이터 설정(appid, DLC, 업적 목록) |
| `Plugins\x86_64\lib_burst_generated.dll` | 57 KB | Unity Burst 컴파일 결과 |
| `StreamingAssets\cacheSimple\` | — | 기본 세이브 틀 |
| `StreamingAssets\assets\game_icon` | — | 아이콘 |

게임의 자산은 AssetBundle로 나뉘어 있지 않고 `resources.assets` 계열 파일에 통합되어 있다. 설정 테이블 원본이 이 파일들 안에 있는지, `GameConfPack*.dll`과 어떤 관계인지는 02 문서에서 다룬다.

## 11. 미확인 항목

| 항목 | 사유 | 확인 방법 |
|---|---|---|
| Steam 빌드 ID | `appmanifest_1468810.acf`가 없다. 확인한 경로: `C:\Program Files (x86)\Steam\steamapps`, `E:\SteamLibrary\steamapps`. 설치본이 Steam 클라이언트를 거치지 않았다 | Steam에서 설치한 정식 설치본의 매니페스트 |
| 창작마당 구독 모드 | 위 두 라이브러리에 `workshop\content\1468810`이 없다. 에뮬레이터 구성에서는 창작마당 구독이 동작하지 않는다 | 정식 설치본 |
| 최신 게임 버전과의 차이 | 설치본 버전 v1.2.113.259가 현재 Steam 배포 버전과 같은지 확인하지 못했다 | 02a 문서에서 창작마당 모드의 지원 버전과 대조 |
| `ModExportData\`의 용도 | 비어 있고 이름만으로는 확정할 수 없다 | 02 문서(공식 교안) |
| `Steam\DaguiToolCmdData.cache`, `data\`의 용도 | 내용을 열지 않았다 | 필요 시 v0.1 단계에서 확인 |
| 2023-02-08 로그 9개의 출처 | 배포본에 포함되어 온 것인지 이 PC에서 실행한 기록인지 알 수 없다(파일 시각이 2020-02-02로 통일됨) | 확인 불필요 |

## 12. 이전 Steam 설치의 흔적

2026-10-12 후속 조사에서 확인했다. 1~11절을 처음 쓸 때는 표준 경로만 확인해 놓쳤던 내용이다.

| 항목 | 내용 | 근거 |
|---|---|---|
| 이전 실행 경로 | `E:/SteamLibrary/steamapps/common/鬼谷八荒/` | `Player-prev.log`(2026-10-04 17:45)의 `Discovering subsystems at path` |
| 이전 실행 버전 | v1.2.113.259 (현재 설치본과 같음) | 같은 로그의 `游戏版本` |
| 이전에 로드한 모드 | 창작마당 모드 204개(`E:\SteamLibrary\steamapps\workshop\content\1468810\<ID>`) | 같은 로그의 `加载模组：` 줄 204개 |
| 현재 `E:\SteamLibrary\steamapps\common\鬼谷八荒\` | 게임 실행 파일과 데이터가 없다. `MelonLoader\`, `Mods\MOD_HvBArn`, `XSMSX\`, `AIPromptFile.json`, `GiftPropSort_Config.json`, `warhistory_*.json` 등 모드가 만든 파일만 남아 있다 | 폴더 목록 |
| 현재 창작마당 폴더 | `workshop\content\1468810_disabled_claude`(항목 3개). `appworkshop_1468810.acf`와 `appmanifest_1468810.acf`는 없다 | 폴더 목록 |
| 현재 실행 로그 | `Player.log`(2026-10-11)에는 `加载模组` 줄이 없다. 현재 설치본은 모드 없이 실행됐다 | `Player.log` |
| 세이브 폴더의 백업 | `Steam\CacheData_backup_20261004_claude\`, `Steam\Mod_LoadData_backup_20261004_claude.bin` — 2026-10-04에 만들어졌다 | 9절 |

Steam 설치본이 언제 왜 제거됐는지는 이 조사로 알 수 없다. 창작마당 구독·업로드와 Steam 빌드 ID 확인에는 Steam 설치본이 필요하다는 결론은 그대로다.

이 로그는 공식 경로의 로드 순서도 보여 준다. 창작마당 모드는 `<ID>\ModExportData.cache` 또는 `<ID>\debug\Mod_<모드ID>\ModExportData.cache`에서 로드된다. `ModExportData.cache`는 암호화된 파일이어서 모드 에디터의 내보내기 없이 손으로 만들 수 없다.
