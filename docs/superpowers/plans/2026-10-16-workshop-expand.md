# 창작마당 모드 확장(W: 검색·등록부·컬렉션) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `tools/workshop.ps1`에 창작마당 검색(`-Search`)·등록부 문서 생성(`-Doc`)·이력 기록을 더하고, 목록 파일을 상태 열이 있는 단일 원본으로 바꾼 뒤, 후보표에서 사용자가 고른 모드를 받아 검증·설치하고 Steam 컬렉션을 만들어 문서와 맞춘다.

**Architecture:** 목록 `tools/workshop/mods.txt`(일곱 칸, 상태 설치·보류·제외)가 모든 동작의 원본이고, 도구는 성공한 받음·설치·갱신·지움을 `history.txt`에 덧붙인다. `-Search`는 창작마당 검색 페이지(구독자순)와 공개 상세 API, 항목 페이지의 필수 아이템 블록을 읽어 후보표 `candidates-<날짜>.md`를 쓴다. `-Doc`은 목록·폴더·이력·컬렉션 API로 `docs/09-workshop-mods.md`와 컬렉션 설명문을 만든다. 네트워크는 읽기만 하고, 시험은 `-WebRoot`로 저장된 응답 파일을 읽어 네트워크 없이 돈다. 컬렉션 만들기·항목 넣기는 사용자가 로그인한 Chrome에서 컨트롤러가 한다.

**Tech Stack:** PowerShell 7(`pwsh`), `Invoke-WebRequest`, robocopy, SteamCMD(`E:\SteamCmd\steamcmd.exe`, 캐시 로그인 `deepestlight`), Steam 공개 Web API(`GetPublishedFileDetails`, `GetCollectionDetails`), 회차 스크립트 `tools/DevRemote/session.ps1`, 개발 명령 `wsmods`·`rloc`, Claude in Chrome(컬렉션).

**Spec:** `docs/superpowers/specs/2026-10-16-workshop-expand-design.md`

## Global Constraints

- 브랜치 `feat/workshop-expand`(`develop`에서. 스펙 커밋 `a19583c`). 끝에서 `git merge --no-ff`로 `develop`에 병합한다. `main`에는 병합하지 않는다. 커밋 메시지는 한국어, 끝에 `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.
- 모든 git·파일 명령은 저장소 `E:\TlToybox`의 절대경로로 한다(`cd` 뒤 상대경로 금지. 호출 사이에 cwd가 리셋된다).
- 게임 폴더 `E:\Games\Tale of Immortal`에 쓰는 것은 `ModExportData\<창작마당ID>\`뿐이고 `tools/workshop.ps1 -Install -Yes`(또는 `-Uninstall -Yes`)로만 쓴다. 회차 밖에서 `-Yes`를 붙이는 것은 사용자 확인 뒤, 게임이 꺼진 상태에서만.
- 비밀번호·Steam Guard는 어디에도 받지 않는다. Chrome의 Steam 로그인은 사용자가 한다. 네트워크는 읽기만 한다(검색 페이지·상세 API·항목 페이지·컬렉션 API). 요청 사이 `-Delay`(기본 1.5초).
- 목록 한 줄은 `창작마당ID | 상태 | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 비고`. 상태는 `설치`·`보류`·`제외`, 종류는 `틀`·`외형`·`공법`·`세계관`·`음악`(보류 줄만 `-`). 줄 순서가 설치 순서(Fatury 첫 줄).
- 이력 한 줄은 `yyyy-MM-dd HH:mm | 동작 | 창작마당ID | 한국어 이름`, 동작은 `받음`·`설치`·`갱신`·`지움`. `-NoHistory`면 남기지 않는다(회차 안·시험).
- 받은 자리 `E:\SteamCmd\steamapps\workshop\content\1468810\<ID>\`, 설치 자리 `E:\Games\Tale of Immortal\ModExportData\<ID>\`.
- 생성 파일: 후보표 `tools/workshop/candidates-<yyyyMMdd>.md`(git에 넣음)와 `candidates-<yyyyMMdd>-desc.txt`(git 제외), 등록부 `docs/09-workshop-mods.md`(git에 넣음, 손으로 고치지 않음), 설명문 `tools/workshop/collection-desc.txt`(git에 넣음), 컬렉션 ID `tools/workshop/collection.txt`.
- 도구가 만드는 마크다운·설명문에는 백틱을 쓰지 않는다(PowerShell 큰따옴표 문자열에서 백틱이 이스케이프 문자라 빠진다).
- 시험 `pwsh tools/workshop.tests.ps1`은 실제 게임 폴더·SteamCMD·세이브·네트워크를 건드리지 않는다(임시 상자와 `-GameDir`·`-ContentDir`·`-SteamCmd`·`-List`·`-History`·`-WebRoot`·`-ProcessName`). 줄마다 PASS/FAIL, 마지막 줄 `FAILS=<수>`. `pwsh tools/DevRemote/session.tests.ps1`은 바뀌지 않으므로 그대로 통과해야 한다.
- 게임 검증은 `pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서. 회차 이름은 `ws-1017a`부터 차례로(`ws-1016a`~`w`는 썼다). `-End`가 `SAVE RESTORED AND IDENTICAL`·`WORKSHOP FOLDERS REMOVED: <수>`. 사용자의 게임이 켜져 있으면(`Get-Process guigubahuang`) 끄도록 알리고 기다린다. 임의로 끄지 않는다.
- 편집기 C# 코드와 회차 스크립트는 바꾸지 않는다.
- 문서에는 확인한 이름만, 확인하지 못한 것은 "미확인"과 사유. 사용자에게 보이는 글은 한국어.
- PowerShell 7.6 함정: `@(List[object])`는 `ToArray()`로, 변수 뒤에 한글이 붙는 문자열은 `${name}개`로.

## Review Focus

1. 목록 줄의 칸이 일곱이 아니거나 상태·종류가 정한 값이 아니거나, 설치·제외 줄의 이름이 비었다 → 어느 동작이든 시작 전에 그 줄을 보이며 멈춘다. (Task 1 시험 "a five-cell line is refused", "an unknown state is refused", "an unknown kind is refused", "a dash name on an install row is refused")
2. `-Id`로 `제외` ID를 받거나 `보류`·`제외` ID를 설치하려 한다 → 거부하고 아무것도 받거나 쓰지 않는다. (Task 1 시험 "download with -Id refuses an excluded row", "install refuses -Id on a held or excluded row")
3. 검색 중 Steam이 요청 제한을 건다(HTTP 429 또는 "请求太多") → 그 자리에서 멈추고 그때까지 모은 것으로 파일을 쓰며 멈춘 자리를 적는다. 필수 항목은 "조회 안 함(요청 제한)". (Task 2 시험 "search stops at the rate limit…")
4. 상세 API가 어떤 ID를 돌려주지 않거나 차단·비공개다 → 그 줄만 표에서 빠지고 도구는 이어 간다. (Task 2 시험 "search drops a banned item and an item the details api did not return")
5. 컬렉션 ID 파일이 없거나 조회가 실패한다 → 문서는 그래도 만들어지고 4절에 "컬렉션 없음" 또는 "조회 실패: 사유"가 적힌다. (Task 3 시험 "doc reports the collection as missing…", "doc reports a failed collection lookup")

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `tools/workshop/mods.txt` | 수정 | 일곱 칸. 15개 설치 줄 + 제외 1 + 보류 1. 모드ID는 05 문서 41절 표 |
| `tools/workshop/history.txt` | 새로 | 이력. 앞 세션의 받음 16·설치 15를 소급 |
| `tools/workshop.ps1` | 수정 | 목록 읽기(`ReadList`, 상태·종류 검사), 줄 고르기(`Chosen`, `ForDownload`, `ForInstall`), 이력(`WriteHistory`), 검색(`Fetch`, `BrowsePage`, `Details`, `RequiredItems`, `Flags`, `DoSearch`, `WriteCandidates`), 문서(`ReadHistory`, `ReadCollectionId`, `TranslationRows`, `CollectionChildren`, `WriteCollectionDesc`, `DoDoc`) |
| `tools/workshop.tests.ps1` | 수정 | 상자에 보류·제외 줄, 이력 경로, `web` 폴더. 시험 1~11 갱신, 12(`-Search`)·13(`-Doc`) 추가 |
| `tools/workshop/candidates-20261015.md` | 새로 | 앞 세션의 자동 목록(`D:\Download\output\mod_list.md`)을 옮긴 기록 |
| `tools/workshop/candidates-20261016.md` | 새로(생성) | 실제 `-Search` 결과 + 사람이 채운 요약·추천 |
| `tools/workshop/collection.txt`, `collection-desc.txt` | 새로 | 컬렉션 ID, 설명문(생성) |
| `docs/09-workshop-mods.md` | 새로(생성) | 등록부 |
| `.gitignore` | 수정 | `tools/workshop/candidates-*-desc.txt` |
| `docs/05-editor-v0.1.md` | 수정 | 45절 |
| `docs/06-handoff.md`, `docs/02a-workshop-survey.md` | 수정 | 상태·도구·다음 일, 필수 아이템 각주 |

---

### Task 1: 목록 일곱 칸·상태별 동작·이력 파일

**Files:**
- Modify: `tools/workshop.ps1` (머리 주석~`Chosen`, `ShowStatus`, `DoDownload`·`DoInstall`·`DoUninstall`의 결과 줄, 끝의 동작 고르기)
- Modify: `tools/workshop/mods.txt`
- Create: `tools/workshop/history.txt`
- Modify: `tools/workshop.tests.ps1` (전체를 다시 쓴다)

**Interfaces:**
- Produces: 매개변수 `-Search -Doc -History <경로> -NoHistory -Kind -Top -Delay -Out -DocOut -CollectionFile -TranslationFile -WebRoot`(이 Task에서는 선언만. Task 2·3이 쓴다). 줄 객체 `Id, State, Kind, Title, Korean, ModId, Note`. 함수 `ReadList()`, `Chosen($rows)`, `ForDownload($rows)`, `ForInstall($rows)`, `WriteHistory([string]$action, $row)`. 스크립트 변수 `$listDir`, `$utf8`(BOM 없는 UTF-8), `$States`, `$Kinds`.
- Produces: 시험의 상자 `NewBox`(`root, content, steamcmd, game, list, acf, history, web`), `Run($box, $process, $more)`(`-History`·`-WebRoot`를 늘 넘긴다), `HistoryLines($box)`.

- [ ] **Step 1: 목록 파일을 일곱 칸으로 다시 쓴다**

`tools/workshop/mods.txt` 전체:

```text
# 창작마당 모드 목록(docs/superpowers/specs/2026-10-16-workshop-expand-design.md 3.1절. 처음은 2026-10-15-workshop-install-design.md).
# 한 줄에 하나: 창작마당ID | 상태 | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 비고. '#' 줄은 주석. 줄 순서가 설치 순서다(Fatury 먼저).
# 상태: 설치(받고 설치한다) · 보류(받아 두되 설치하지 않는다) · 제외(받지도 설치하지도 않는다). 종류: 틀·외형·공법·세계관·음악(보류 줄만 -).
# 모드ID는 게임이 읽은 soleID(05 문서 41절 표, wsmods). 모르면 -. tools/workshop.ps1은 이 목록에 있는 ID만 다룬다.
2814703549 | 설치 | 틀 | Fatury | Fatury 틀 | q9CYCo | 2815562168이 필요로 함. DLL 3(Fatury.dll, UnityEngine.InputLegacyModule.dll, YamlDotNet.dll)
2815562168 | 설치 | 외형 | 原版17个女性人物动态立绘替换 | 원작 여성 인물 17명 동적 입화 교체 | ymWzeD | Fatury 필요. 111MB
3200319699 | 설치 | 외형 | 【木头】AI动态立绘 | [木头] AI 동적 입화 | 13mJF0 | 206MB
3001496635 | 설치 | 외형 | 【软甜废独家】《五官头饰动态立绘》-原创本人手画 | [软甜废] 이목구비·머리 장식 동적 입화 | IoRGjg | DLL 1(MOD_IoRGjg.dll)
2993500314 | 설치 | 외형 | 动态头发美化 | 동적 머리카락 미화 | FsilSZ |
2819712935 | 설치 | 외형 | 女npc颜值优化 | 여성 NPC 외모 개선 | dVhgEv | debug 모양(Mod_dVhgEv)
2823930683 | 설치 | 외형 | 男性NPC颜值优化 | 남성 NPC 외모 개선 | 5AfmFP |
2945768367 | 설치 | 외형 | 【执余】大量生成美女 | [执余] 미녀 대량 생성 | Z08Zhd |
2874128983 | 설치 | 외형 | 人物美化 .服装解锁 .魅力与权重修改 | 인물 미화·복장 해금·매력과 가중치 수정 | L25r1F |
2876769227 | 설치 | 외형 | 【只有梦长】特殊NPC服装解锁 | [只有梦长] 특수 NPC 복장 해금 | FASRM7 |
2929805068 | 설치 | 외형 | 【大姨夫】仙衣阁、美化 | [大姨夫] 선의각·미화 | cPugE8 | debug 모양(Mod_cPugE8)
2832167498 | 설치 | 외형 | 【含羞草】捏脸 | [含羞草] 얼굴 만들기 | 0lkK4v | DLL 1(MOD_0lkK4v.dll)
2814806198 | 설치 | 외형 | 过场图替换示例mod | 전환 화면 그림 교체 예제 모드 | JprJWu |
3390738990 | 설치 | 공법 | 【只有梦长】五本功法开源 | [只有梦长] 공법 다섯 권(오픈소스) | 7kyQkw | debug 모양(Mod_7kyQkw). 새 공법 다섯
2839261049 | 설치 | 공법 | 真.镇宗之宝：传承心法 | 진·진종지보: 전승 심법 | uCKcKw | DLL 1(MOD_uCKcKw.dll)
2945768219 | 제외 | 외형 | 【执余】女NPC颜值评定优化 | [执余] 여성 NPC 외모 평가 개선 | - | DLL 5에 MySql.Data.dll이 들어 있어 제외(2026-10-15)
3681025074 | 보류 | - | - | - | - | 사용자 시험용. 목록에 넣지 않기로 함(2026-10-15)
```

- [ ] **Step 2: 이력 파일을 소급해 쓴다**

`tools/workshop/history.txt` 전체(시각을 모르는 줄은 `--:--`):

```text
2026-10-15 --:-- | 받음 | 2814703549 | Fatury 틀
2026-10-15 --:-- | 받음 | 2815562168 | 원작 여성 인물 17명 동적 입화 교체
2026-10-15 --:-- | 받음 | 3200319699 | [木头] AI 동적 입화
2026-10-15 --:-- | 받음 | 3001496635 | [软甜废] 이목구비·머리 장식 동적 입화
2026-10-15 --:-- | 받음 | 2993500314 | 동적 머리카락 미화
2026-10-15 --:-- | 받음 | 2819712935 | 여성 NPC 외모 개선
2026-10-15 --:-- | 받음 | 2823930683 | 남성 NPC 외모 개선
2026-10-15 --:-- | 받음 | 2945768367 | [执余] 미녀 대량 생성
2026-10-15 --:-- | 받음 | 2874128983 | 인물 미화·복장 해금·매력과 가중치 수정
2026-10-15 --:-- | 받음 | 2876769227 | [只有梦长] 특수 NPC 복장 해금
2026-10-15 --:-- | 받음 | 2929805068 | [大姨夫] 선의각·미화
2026-10-15 --:-- | 받음 | 2832167498 | [含羞草] 얼굴 만들기
2026-10-15 --:-- | 받음 | 2814806198 | 전환 화면 그림 교체 예제 모드
2026-10-15 --:-- | 받음 | 3390738990 | [只有梦长] 공법 다섯 권(오픈소스)
2026-10-15 --:-- | 받음 | 2839261049 | 진·진종지보: 전승 심법
2026-10-15 --:-- | 받음 | 2945768219 | [执余] 여성 NPC 외모 평가 개선
2026-10-16 --:-- | 설치 | 2814703549 | Fatury 틀
2026-10-16 --:-- | 설치 | 2815562168 | 원작 여성 인물 17명 동적 입화 교체
2026-10-16 --:-- | 설치 | 3200319699 | [木头] AI 동적 입화
2026-10-16 --:-- | 설치 | 3001496635 | [软甜废] 이목구비·머리 장식 동적 입화
2026-10-16 --:-- | 설치 | 2993500314 | 동적 머리카락 미화
2026-10-16 --:-- | 설치 | 2819712935 | 여성 NPC 외모 개선
2026-10-16 --:-- | 설치 | 2823930683 | 남성 NPC 외모 개선
2026-10-16 --:-- | 설치 | 2945768367 | [执余] 미녀 대량 생성
2026-10-16 --:-- | 설치 | 2874128983 | 인물 미화·복장 해금·매력과 가중치 수정
2026-10-16 --:-- | 설치 | 2876769227 | [只有梦长] 특수 NPC 복장 해금
2026-10-16 --:-- | 설치 | 2929805068 | [大姨夫] 선의각·미화
2026-10-16 --:-- | 설치 | 2832167498 | [含羞草] 얼굴 만들기
2026-10-16 --:-- | 설치 | 2814806198 | 전환 화면 그림 교체 예제 모드
2026-10-16 --:-- | 설치 | 3390738990 | [只有梦长] 공법 다섯 권(오픈소스)
2026-10-16 --:-- | 설치 | 2839261049 | 진·진종지보: 전승 심법
```

- [ ] **Step 3: 시험 파일 전체를 다시 쓴다**

`tools/workshop.tests.ps1` 전체:

```powershell
# tools/workshop.ps1의 시험. 게임·SteamCMD·실제 세이브·네트워크를 건드리지 않고 임시 폴더에서 한다.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다.
$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot 'workshop.ps1'
$script:fails = 0
$none = 'no-such-process-tltoybox'            # 실행 중인 게임이 없는 상황
$busy = (Get-Process -Id $PID).ProcessName    # 실행 중인 게임이 있는 상황(이 시험을 돌리는 프로세스의 이름을 빌린다)
$boxes = New-Object System.Collections.Generic.List[string]

function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
function WriteFile([string]$path, [string]$text) {
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
  [IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false)))
}
# 받은 폴더·게임 폴더·목록·가짜 SteamCMD·저장된 웹 응답(web)을 흉내 낸 임시 폴더 한 벌.
#   111: 평평한 것(DLL 하나, 설치)  222: debug 모양(설치)  333: debug 안에 Mod_*가 둘(설치)  444: 받지 않음(설치)  555: 폴더만 있고 cache 없음(설치)
#   666: 보류(받지 않음)  777: 제외(받지 않았지만 게임 폴더에는 설치돼 있음)
function NewBox {
  $root = Join-Path $env:TEMP ('tltoybox-workshop-tests-' + [guid]::NewGuid().ToString('N'))
  $box = [pscustomobject]@{
    root = $root; content = "$root\steam\steamapps\workshop\content\1468810"; steamcmd = "$root\steam\steamcmd.ps1"
    game = "$root\game"; list = "$root\mods.txt"; acf = "$root\steam\steamapps\workshop\appworkshop_1468810.acf"
    history = "$root\history.txt"; web = "$root\web" }
  WriteFile "$($box.game)\ModExportData\Mod_sWP6UL\ModExportData.cache" 'ours'
  WriteFile "$($box.game)\ModExportData\unknown\ModExportData.cache" 'not in the list'
  WriteFile "$($box.game)\ModExportData\777\ModExportData.cache" 'c777'
  WriteFile "$($box.content)\111\ModExportData.cache" 'c111'
  WriteFile "$($box.content)\111\ModAssets\a.png" 'png'
  WriteFile "$($box.content)\111\ModCode\dll\MOD_x.dll" 'dll'
  WriteFile "$($box.content)\222\debug\Mod_abc\ModExportData.cache" 'c222'
  WriteFile "$($box.content)\222\debug\Mod_abc\ModAssets\b.png" 'png'
  WriteFile "$($box.content)\222\ModProject\ModData.cache" 'project'
  WriteFile "$($box.content)\333\debug\Mod_a\ModExportData.cache" 'c333a'
  WriteFile "$($box.content)\333\debug\Mod_b\ModExportData.cache" 'c333b'
  [void][IO.Directory]::CreateDirectory("$($box.content)\555")
  [void][IO.Directory]::CreateDirectory($box.web)
  WriteFile $box.list "# 시험용`n111 | 설치 | 외형 | one | 하나 | - | DLL 1`n222 | 설치 | 외형 | two | 둘 | - | debug`n333 | 설치 | 외형 | three | 셋 | - |`n444 | 설치 | 공법 | four | 넷 | - |`n555 | 설치 | 공법 | five | 다섯 | - |`n666 | 보류 | - | - | - | - | 시험용 보류`n777 | 제외 | 외형 | seven | 일곱 | - | 제외 사유`n"
  WriteFile $box.acf "`"AppWorkshop`"`n{`n`t`"WorkshopItemDetails`"`n`t{`n`t`t`"111`"`n`t`t{`n`t`t`t`"manifest`"`t`t`"1`"`n`t`t`t`"timeupdated`"`t`t`"1735353779`"`n`t`t}`n`t}`n}`n"
  # 가짜 SteamCMD. +runscript 파일의 workshop_download_item 줄마다 폴더를 만들고 Success 줄을 찍는다.
  # 첫 호출에서는 마지막 항목을 failed로 찍는다(재시도 확인). expired.flag가 있으면 로그인이 풀린 출력만 찍는다.
  WriteFile $box.steamcmd @'
$content = Join-Path $PSScriptRoot 'steamapps\workshop\content\1468810'
$marker = Join-Path $PSScriptRoot 'called.txt'
'Steam Console Client (fake)'
if (Test-Path (Join-Path $PSScriptRoot 'expired.flag')) { 'Cached credentials not found'; 'FAILED (Invalid Password)'; exit 0 }
$i = [array]::IndexOf($args, '+runscript')
$lines = @(Get-Content -LiteralPath $args[$i + 1] | Where-Object { $_ -match '^workshop_download_item' })
$first = -not (Test-Path $marker)
[IO.File]::WriteAllText($marker, '')
for ($k = 0; $k -lt $lines.Count; $k++) {
  $id = ($lines[$k] -split ' ')[2]
  if ($first -and $k -eq $lines.Count - 1) { "ERROR! Download item $id failed (Failure)."; continue }
  [void][IO.Directory]::CreateDirectory("$content\$id")
  [IO.File]::WriteAllText("$content\$id\ModExportData.cache", "c$id")
  "Success. Downloaded item $id to `"$content\$id`" (10 bytes)"
}
'@
  $boxes.Add($root)
  $box
}
# workshop.ps1을 상자 안에서 돌린다. 던진 오류는 "THROWN: ..." 문자열로 돌려준다.
function Run($box, [string]$process, [hashtable]$more) {
  try {
    $out = & $tool -GameDir $box.game -SteamCmd $box.steamcmd -ContentDir $box.content -List $box.list -History $box.history -WebRoot $box.web -ProcessName $process -TimeoutMinutes 1 @more
    ($out -join "`n")
  } catch { 'THROWN: ' + $_.Exception.Message }
}
function Tree([string]$root) {
  (Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName |
    ForEach-Object { $_.FullName.Substring($root.Length) + '=' + [IO.File]::ReadAllText($_.FullName) }) -join ';'
}
function HistoryLines($box) { if (Test-Path -LiteralPath $box.history) { @([IO.File]::ReadAllLines($box.history)) } else { @() } }
$when = '\d{4}-\d\d-\d\d \d\d:\d\d'

# 1. -Status: 줄마다 상태·받음·설치·갱신·DLL, 마지막 줄의 요약
$b = NewBox
$o = Run $b $none @{ Status = $true }
Check 'Status shows a downloaded flat item with state, size, date and dll count' ($o -match '111 \| 설치 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| - \| - \| 1') $o
Check 'Status shows a debug item without a date' ($o -match '222 \| 설치 \| 외형 \| 둘 \| 0(\.0)?MB - \| - \| - \| 0') $o
Check 'Status shows a missing item with dashes' ($o -match '444 \| 설치 \| 공법 \| 넷 \| - \| - \| - \| -') $o
Check 'Status counts a folder without a cache as not downloaded' ($o -match '555 \| 설치 \| 공법 \| 다섯 \| - \| - \| - \| -') $o
Check 'Status shows a held row with dashes for kind and name' ($o -match '666 \| 보류 \| - \| - \| - \| - \| - \| -') $o
Check 'Status shows an excluded row that is installed' ($o -match '777 \| 제외 \| 외형 \| 일곱 \| - \| 예 \| - \| -') $o
Check 'Status ends with the summary over install rows and counts held and excluded rows' ($o -match '받음 3/5, 설치 0/5 \| 보류 1 \| 제외 1') $o

# 2. -Id는 목록의 ID만 받는다
$o = Run $b $none @{ Status = $true; Id = '111,222' }
Check 'Id narrows the rows' ($o -match '받음 2/2, 설치 0/2' -and -not ($o -match '(?m)^444')) $o
$o = Run $b $none @{ Status = $true; Id = '999' }
Check 'an unknown Id is refused' ($o -match '^THROWN.*목록에 없는 ID') $o

# 3. 깨진 목록 줄은 시작 전에 멈춘다
$b = NewBox
[IO.File]::AppendAllText($b.list, "abc | 설치 | 외형 | bad | 나쁨 | - |`n")
$o = Run $b $none @{ Status = $true }
Check 'a broken list line stops the tool before it does anything' ($o -match '^THROWN.*목록의 줄을 읽지 못했습니다') $o
$b = NewBox
[IO.File]::AppendAllText($b.list, "888 | 외형 | old | 옛 | 다섯 칸`n")
$o = Run $b $none @{ Status = $true }
Check 'a five-cell line is refused' ($o -match '^THROWN.*목록의 줄을 읽지 못했습니다') $o
$b = NewBox
[IO.File]::AppendAllText($b.list, "888 | 대기 | 외형 | x | 엑스 | - |`n")
$o = Run $b $none @{ Status = $true }
Check 'an unknown state is refused' ($o -match '^THROWN.*모르는 상태') $o
$b = NewBox
[IO.File]::AppendAllText($b.list, "888 | 설치 | 기타 | x | 엑스 | - |`n")
$o = Run $b $none @{ Status = $true }
Check 'an unknown kind is refused' ($o -match '^THROWN.*모르는 종류') $o
$b = NewBox
[IO.File]::AppendAllText($b.list, "888 | 설치 | 외형 | - | - | - |`n")
$o = Run $b $none @{ Status = $true }
Check 'a dash name on an install row is refused' ($o -match '^THROWN.*이름을 비울 수 없습니다') $o

# 4. 동작은 하나만 고른다
$b = NewBox
$o = Run $b $none @{ }
Check 'no action is refused' ($o -match '^THROWN.*하나를 고릅니다') $o
$o = Run $b $none @{ Status = $true; Install = $true }
Check 'two actions are refused' ($o -match '^THROWN.*하나를 고릅니다') $o

# 5. -Install(계획만): 설치 줄만, 원본을 고르고 이유를 보이되 아무것도 쓰지 않는다
$b = NewBox
$before = Tree $b.game
$o = Run $b $none @{ Install = $true }
Check 'install plan lists the flat item with its dll' ($o -match '111 \| 하나 \| 평평함 \| 0(\.0)?MB \| DLL MOD_x\.dll \| 새로 설치') $o
Check 'install plan lists the debug item' ($o -match '222 \| 둘 \| debug \| 0(\.0)?MB \| DLL - \| 새로 설치') $o
Check 'install plan skips an item with two Mod_ folders' ($o -match '333 \| 셋 \| 건너뜀: debug 안에 Mod_\* 폴더가 2개') $o
Check 'install plan skips an item that is not downloaded' ($o -match '444 \| 넷 \| 건너뜀: 받지 않음') $o
Check 'a folder without a cache is skipped with the reason' ($o -match '555 \| 다섯 \| 건너뜀: ModExportData.cache가 없음') $o
Check 'install plan leaves out held and excluded rows' (-not ($o -match '(?m)^666') -and -not ($o -match '(?m)^777')) $o
Check 'install without -Yes writes nothing, not even history' ($o -match 'PLAN ONLY' -and (Tree $b.game) -eq $before -and (HistoryLines $b).Count -eq 0) $o
$o = Run $b $none @{ Install = $true; Id = '777' }
Check 'install refuses -Id on a held or excluded row' ($o -match '^THROWN.*제외 상태의 ID는 설치하지 않습니다') $o
$o = Run $b $none @{ Install = $true; Id = '666' }
Check 'install refuses -Id on a held row' ($o -match '^THROWN.*보류 상태의 ID는 설치하지 않습니다') $o

# 6. -Install -Yes: 평평한 것은 그대로, debug 모양은 Mod_* 안의 내용만. 이력에 설치 줄
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'install -Yes copies the flat item as it is' ($o -match 'INSTALLED 111 하나' -and [IO.File]::ReadAllText("$($b.game)\ModExportData\111\ModExportData.cache") -eq 'c111' -and (Test-Path "$($b.game)\ModExportData\111\ModCode\dll\MOD_x.dll")) $o
Check 'install -Yes flattens the debug item' ($o -match 'INSTALLED 222 둘' -and [IO.File]::ReadAllText("$($b.game)\ModExportData\222\ModExportData.cache") -eq 'c222' -and (Test-Path "$($b.game)\ModExportData\222\ModAssets\b.png") -and -not (Test-Path "$($b.game)\ModExportData\222\debug") -and -not (Test-Path "$($b.game)\ModExportData\222\ModProject")) $o
Check 'install -Yes ends with the count' ($o -match '설치 2개') $o
$h = HistoryLines $b
Check 'install -Yes appends a history line per installed item' ($h.Count -eq 2 -and $h[0] -match "^$when \| 설치 \| 111 \| 하나$" -and $h[1] -match "^$when \| 설치 \| 222 \| 둘$") ($h -join ';')
$o = Run $b $none @{ Status = $true }
Check 'Status shows installed items as identical' ($o -match '111 \| 설치 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| 예 \| 같음 \| 1' -and $o -match '받음 3/5, 설치 2/5') $o

# 7. 같은 것을 다시 넣으면 UNCHANGED(이력 없음), 원본이 바뀌면 UPDATED(이력 갱신. 게임이 켜져 있으면 그 줄만 거부). -NoHistory
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'a second install -Yes changes nothing and writes no history' ($o -match 'UNCHANGED 111 하나' -and $o -match 'UNCHANGED 222 둘' -and $o -match '설치 0개' -and (HistoryLines $b).Count -eq 2) $o
WriteFile "$($b.content)\111\ModAssets\new.png" 'new'
$o = Run $b $none @{ Status = $true }
Check 'Status notices a changed source' ($o -match '111 \| 설치 \| 외형 \| 하나 \| 0(\.0)?MB 2024-12-28 \| 예 \| 다름 \d+ \| 1') $o
WriteFile "$($b.content)\444\ModExportData.cache" 'c444'
$o = Run $b $busy @{ Install = $true; Yes = $true }
Check 'a running game blocks updating an installed folder' ($o -match '111 \| 하나 .*거부: 게임이 실행 중' -and -not (Test-Path "$($b.game)\ModExportData\111\ModAssets\new.png")) $o
Check 'a running game still lets a new folder in' ($o -match 'INSTALLED 444 넷' -and (Test-Path "$($b.game)\ModExportData\444\ModExportData.cache") -and $o -match '다음 게임 시작부터') $o
$o = Run $b $none @{ Install = $true; Yes = $true; Id = '111' }
Check 'with the game closed the changed item is updated and the history says so' ($o -match 'UPDATED 111 하나' -and (Test-Path "$($b.game)\ModExportData\111\ModAssets\new.png") -and (HistoryLines $b)[-1] -match "^$when \| 갱신 \| 111 \| 하나$") $o
WriteFile "$($b.content)\111\ModAssets\new2.png" 'new2'
$count = (HistoryLines $b).Count
$o = Run $b $none @{ Install = $true; Yes = $true; Id = '111'; NoHistory = $true }
Check '-NoHistory updates without a history line' ($o -match 'UPDATED 111 하나' -and (HistoryLines $b).Count -eq $count) $o

# 8. -Uninstall: 계획만, 게임이 켜져 있으면 거부, -Yes는 목록 ID 폴더만(제외 줄도) 지우고 이력에 지움 줄
$before = Tree $b.game
$o = Run $b $none @{ Uninstall = $true }
Check 'uninstall plan lists installed items of any state and writes nothing' ($o -match '111 \| 하나 \| 지움 0(\.0)?MB' -and $o -match '777 \| 일곱 \| 지움 0(\.0)?MB' -and $o -match '333 \| 셋 \| 설치돼 있지 않음' -and $o -match 'PLAN ONLY' -and (Tree $b.game) -eq $before) $o
$o = Run $b $busy @{ Uninstall = $true; Yes = $true }
Check 'uninstall is refused while the game runs' ($o -match '^THROWN.*게임이 실행 중' -and (Tree $b.game) -eq $before) $o
$o = Run $b $none @{ Uninstall = $true; Yes = $true }
Check 'uninstall -Yes removes the listed folders' ($o -match 'REMOVED 111 하나' -and $o -match 'REMOVED 222 둘' -and $o -match 'REMOVED 444 넷' -and $o -match 'REMOVED 777 일곱' -and $o -match '지움 4개' -and -not (Test-Path "$($b.game)\ModExportData\111") -and -not (Test-Path "$($b.game)\ModExportData\777")) $o
Check 'uninstall leaves the editor folder and a folder that is not in the list' ((Test-Path "$($b.game)\ModExportData\Mod_sWP6UL\ModExportData.cache") -and (Test-Path "$($b.game)\ModExportData\unknown\ModExportData.cache")) $o
Check 'uninstall appends a history line per removed folder' ((HistoryLines $b)[-1] -match "^$when \| 지움 \| 777 \| 일곱$") ((HistoryLines $b) -join ';')

# 9. 게임 폴더가 아니면 설치하지 않는다
$b = NewBox
[IO.Directory]::Delete("$($b.game)\ModExportData", $true)
$o = Run $b $none @{ Install = $true; Yes = $true }
Check 'install refuses a folder without ModExportData' ($o -match '^THROWN.*ModExportData') $o

# 10. -Download: 설치 줄 가운데 받지 않은 것만 받고, 실패한 것은 다시 시도한다(가짜 SteamCMD는 첫 호출에서 마지막 항목을 실패시킨다). 이력에 받음 줄
$b = NewBox
$o = Run $b $none @{ Download = $true }
Check 'download asks only for the missing install rows' ($o -match 'pass 1: 2개' -and -not (Test-Path "$($b.content)\666") -and -not (Test-Path "$($b.content)\777")) $o
Check 'download reports the failed item and retries' ($o -match 'FAIL 555 다섯' -and $o -match 'pass 2: 1개' -and $o -match 'OK   555 다섯') $o
Check 'download picks up a folder without a cache' ((Test-Path "$($b.content)\555\ModExportData.cache") -and [IO.File]::ReadAllText("$($b.content)\555\ModExportData.cache") -eq 'c555') $o
Check 'download creates the missing folder' ((Test-Path "$($b.content)\444\ModExportData.cache") -and $o -match 'OK   444 넷') $o
Check 'download ends with the count' ($o -match '받음 5/5') $o
$h = HistoryLines $b
Check 'download appends a history line per downloaded item' ($h.Count -eq 2 -and $h[0] -match "^$when \| 받음 \| 444 \| 넷$" -and $h[1] -match "^$when \| 받음 \| 555 \| 다섯$") ($h -join ';')
$o = Run $b $none @{ Download = $true }
Check 'download with nothing missing does not run steamcmd' ($o -match '받음 5/5 \(받을 것이 없습니다\)') $o
$o = Run $b $none @{ Download = $true; Id = '111' }
Check 'download with -Id re-downloads that item' ($o -match 'pass 1: 1개' -and $o -match 'OK   111 하나') $o
$o = Run $b $none @{ Download = $true; Id = '666' }
Check 'download with -Id downloads a held row' ($o -match 'OK   666 -' -and (Test-Path "$($b.content)\666\ModExportData.cache")) $o
$o = Run $b $none @{ Download = $true; Id = '777' }
Check 'download with -Id refuses an excluded row' ($o -match '^THROWN.*제외한 ID입니다' -and -not (Test-Path "$($b.content)\777")) $o

# 11. 로그인이 풀려 있으면 멈추고 사용자가 칠 명령을 보인다
$b = NewBox
WriteFile "$($b.root)\steam\expired.flag" ''
$o = Run $b $none @{ Download = $true }
Check 'an expired login stops the download and shows the login command' ($o -match '로그인이 풀려 있습니다' -and $o -match '\+login deepestlight \+quit' -and -not (Test-Path "$($b.content)\444")) $o

foreach ($root in $boxes) {
  if ($root.StartsWith((Join-Path $env:TEMP 'tltoybox-workshop-tests-')) -and (Test-Path $root)) { [IO.Directory]::Delete($root, $true) }
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
exit 0
```

- [ ] **Step 4: 시험을 돌려 실패를 본다**

Run: `pwsh E:\TlToybox\tools\workshop.tests.ps1`
Expected: 시험 1의 첫 줄부터 FAIL(지금 도구는 다섯 칸 목록을 기대해 "목록의 줄을 읽지 못했습니다"를 던진다). 마지막 줄 `FAILS=`가 0보다 크다.

- [ ] **Step 5: `workshop.ps1`의 머리·목록 읽기·줄 고르기·이력을 쓴다**

`tools/workshop.ps1`의 첫 줄부터 `function Chosen` 블록의 끝(`@($rows | Where-Object { $wanted -contains $_.Id })` 다음 `}`)까지를 다음으로 바꾼다. `Downloaded`부터 `IsInstalled`까지는 그대로 둔다.

```powershell
# 창작마당 모드를 찾고 받아 게임의 로컬 모드 폴더에 설치하는 도구
# (docs/superpowers/specs/2026-10-15-workshop-install-design.md, 2026-10-16-workshop-expand-design.md).
#   -Status                       목록의 줄마다 상태·받음·설치·갱신·DLL을 보인다
#   -Download  [-Id a,b]          설치 줄 가운데 받지 않은 것(또는 -Id로 고른 것. 보류는 되고 제외는 거부)을 SteamCMD의 캐시 로그인으로 받는다(최대 세 번)
#   -Install   [-Id a,b] [-Yes]   설치 줄의 받은 것을 <GameDir>\ModExportData\<ID>\에 복사한다. -Yes 없이는 계획만 보인다
#   -Uninstall [-Id a,b] [-Yes]   목록 ID의 설치 폴더를 지운다(상태와 무관). -Yes 없이는 계획만 보인다
#   -Search    [-Kind …] [-Top n] 창작마당에서 종류마다 구독자 상위 후보를 모아 후보표를 쓴다(2026-10-16 설계 3.3절)
#   -Doc                          등록부 문서 docs/09-workshop-mods.md와 컬렉션 설명문을 쓴다(3.4절)
# 목록 tools/workshop/mods.txt: 창작마당ID | 상태(설치·보류·제외) | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 비고. 줄 순서가 설치 순서.
# 받음·설치·갱신·지움은 tools/workshop/history.txt에 한 줄씩 남는다(-NoHistory면 남기지 않는다. 회차 안에서 쓴다).
# 게임 폴더에 쓰는 것은 ModExportData\<창작마당ID>\뿐이다(CLAUDE.md). 비밀번호는 받지 않는다. 네트워크는 읽기만 한다.
# 설치 원본: <ID>\ModExportData.cache가 있으면 <ID>\ 전체, 없으면 <ID>\debug\Mod_*\ 가운데 cache가 있는 폴더가 하나일 때 그것.
param([switch]$Status, [switch]$Download, [switch]$Install, [switch]$Uninstall, [switch]$Search, [switch]$Doc, [switch]$Yes,
  [string]$Id = '',
  [string]$GameDir = 'E:\Games\Tale of Immortal', [string]$SteamCmd = 'E:\SteamCmd\steamcmd.exe', [string]$Account = 'deepestlight',
  [string]$List = '', [int]$AppId = 1468810,
  [string]$History = '', [switch]$NoHistory,
  # -Search (3.3절)
  [string]$Kind = '외형,공법,세계관,음악', [int]$Top = 20, [double]$Delay = 1.5, [string]$Out = '',
  # -Doc (3.4절)
  [string]$DocOut = '', [string]$CollectionFile = '', [string]$TranslationFile = '',
  # 아래는 workshop.tests.ps1이 쓰는 시험용 매개변수다. 평소에는 주지 않는다. -WebRoot는 네트워크 대신 그 폴더의 파일을 읽는다.
  [string]$ContentDir = '', [string]$ProcessName = 'guigubahuang', [int]$TimeoutMinutes = 30, [string]$WebRoot = '')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $List) { $List = Join-Path $repo 'tools\workshop\mods.txt' }
$listDir = Split-Path -Parent $List
if (-not $History) { $History = Join-Path $listDir 'history.txt' }
if (-not $CollectionFile) { $CollectionFile = Join-Path $listDir 'collection.txt' }
if (-not $TranslationFile) { $TranslationFile = Join-Path $repo 'src\ModCode\ModMain\Translations\mods.txt' }
if (-not $Out) { $Out = Join-Path $listDir ('candidates-' + (Get-Date -Format 'yyyyMMdd') + '.md') }
if (-not $DocOut) { $DocOut = Join-Path $repo 'docs\09-workshop-mods.md' }
$steamRoot = Split-Path -Parent $SteamCmd
if (-not $ContentDir) { $ContentDir = Join-Path $steamRoot "steamapps\workshop\content\$AppId" }
$acf = Join-Path $steamRoot "steamapps\workshop\appworkshop_$AppId.acf"
$modsDir = Join-Path $GameDir 'ModExportData'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$States = @('설치', '보류', '제외')
$Kinds = @('틀', '외형', '공법', '세계관', '음악')

function Running { [bool](Get-Process $ProcessName -ErrorAction SilentlyContinue) }

# 목록 파일. '#' 줄은 주석. ID | 상태 | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 비고
function ReadList {
  if (-not (Test-Path -LiteralPath $List)) { throw "목록 파일이 없습니다: $List" }
  $rows = New-Object System.Collections.Generic.List[object]
  foreach ($line in [IO.File]::ReadAllLines($List)) {
    $t = $line.Trim()
    if (-not $t -or $t.StartsWith('#')) { continue }
    $cells = @($t.Split('|') | ForEach-Object { $_.Trim() })
    if ($cells.Count -lt 7 -or $cells[0] -notmatch '^\d+$') { throw "목록의 줄을 읽지 못했습니다(일곱 칸이어야 합니다): $line" }
    if ($States -notcontains $cells[1]) { throw "모르는 상태입니다(설치·보류·제외): $line" }
    $held = $cells[1] -eq '보류'
    if (-not ($Kinds -contains $cells[2] -or ($held -and $cells[2] -eq '-'))) { throw "모르는 종류입니다(틀·외형·공법·세계관·음악): $line" }
    if (-not $held -and ($cells[3] -in @('', '-') -or $cells[4] -in @('', '-'))) { throw "설치·제외 줄은 이름을 비울 수 없습니다: $line" }
    $rows.Add([pscustomobject]@{ Id = $cells[0]; State = $cells[1]; Kind = $cells[2]; Title = $cells[3]; Korean = $cells[4]; ModId = $cells[5]; Note = $cells[6] })
  }
  $rows.ToArray()
}
# -Id로 고른 것만 남긴다. 목록에 없는 ID는 거부한다.
function Chosen($rows) {
  if (-not $Id) { return @($rows) }
  $wanted = @($Id.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
  foreach ($w in $wanted) { if (-not @($rows | Where-Object { $_.Id -eq $w })) { throw "목록에 없는 ID입니다: $w" } }
  @($rows | Where-Object { $wanted -contains $_.Id })
}
# 받을 줄: 설치 줄. -Id로 지정하면 보류도 받되 제외는 거부한다.
function ForDownload($rows) {
  if ($Id) { foreach ($r in $rows) { if ($r.State -eq '제외') { throw "제외한 ID입니다: $($r.Id) ($($r.Note))" } }; return @($rows) }
  @($rows | Where-Object { $_.State -eq '설치' })
}
# 설치할 줄: 설치 줄만. -Id로 보류·제외를 지정하면 거부한다.
function ForInstall($rows) {
  if ($Id) { foreach ($r in $rows) { if ($r.State -ne '설치') { throw "$($r.State) 상태의 ID는 설치하지 않습니다: $($r.Id)" } } }
  @($rows | Where-Object { $_.State -eq '설치' })
}
# 이력 한 줄을 덧붙인다. 동작은 받음·설치·갱신·지움. -NoHistory면 쓰지 않는다.
function WriteHistory([string]$action, $row) {
  if ($NoHistory) { return }
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $History))
  $line = '{0} | {1} | {2} | {3}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm'), $action, $row.Id, $row.Korean
  [IO.File]::AppendAllText($History, $line + "`n", $utf8)
}
```

- [ ] **Step 6: `ShowStatus`를 바꾼다**

`function ShowStatus($rows) { … }` 전체를 다음으로 바꾼다.

```powershell
function ShowStatus($rows) {
  $got = 0; $inst = 0
  $target = @($rows | Where-Object { $_.State -eq '설치' })
  'ID | 상태 | 종류 | 이름 | 받음 | 설치 | 갱신 | DLL'
  foreach ($r in $rows) {
    $root = Downloaded $r.Id
    $s = Source $r.Id
    $isInst = IsInstalled $r.Id
    if ($r.State -eq '설치') { if ($root) { $got++ }; if ($isInst) { $inst++ } }
    $have = if ($root) { "$(SizeMb $root)MB $(Updated $r.Id)" } else { '-' }
    $fresh = if ($s.Path -and $isInst) { $n = DiffCount $s.Path (Installed $r.Id); if ($n -eq 0) { '같음' } else { "다름 $n" } } else { '-' }
    $dll = if ($root) { (Dlls $root).Count } else { '-' }
    "$($r.Id) | $($r.State) | $($r.Kind) | $($r.Korean) | $have | $(if ($isInst) { '예' } else { '-' }) | $fresh | $dll"
  }
  $held = @($rows | Where-Object { $_.State -eq '보류' }).Count
  $excl = @($rows | Where-Object { $_.State -eq '제외' }).Count
  "받음 $got/$($target.Count), 설치 $inst/$($target.Count) | 보류 $held | 제외 $excl"
}
```

- [ ] **Step 7: 세 동작에 이력 줄을 더하고 동작 고르기를 바꾼다**

`DoInstall` 안의 `"$(if ($p.What -eq '새로 설치') { 'INSTALLED' } else { 'UPDATED' }) $($p.Row.Id) $($p.Row.Korean)"` 줄 바로 아래에 한 줄을 더한다:

```powershell
    WriteHistory $(if ($p.What -eq '새로 설치') { '설치' } else { '갱신' }) $p.Row
```

`DoUninstall` 안의 `"REMOVED $($t.Row.Id) $($t.Row.Korean)"` 줄 바로 아래에:

```powershell
    WriteHistory '지움' $t.Row
```

`DoDownload` 안의 `foreach ($r in $pending) { if ($joined -match "Success\. Downloaded item $($r.Id)\b") { "OK   $($r.Id) $($r.Korean)" } else { "FAIL $($r.Id) $($r.Korean)" } }`를 다음으로 바꾼다:

```powershell
    foreach ($r in $pending) {
      if ($joined -match "Success\. Downloaded item $($r.Id)\b") { "OK   $($r.Id) $($r.Korean)"; WriteHistory '받음' $r } else { "FAIL $($r.Id) $($r.Korean)" }
    }
```

파일 끝의 `$chosen = Chosen (ReadList)`부터 `exit 0`까지를 다음으로 바꾼다:

```powershell
$all = ReadList
$chosen = Chosen $all
$modes = @(@($Status, $Download, $Install, $Uninstall, $Search, $Doc) | Where-Object { $_ }).Count
if ($modes -ne 1) { throw '-Status, -Download, -Install, -Uninstall, -Search, -Doc 가운데 하나를 고릅니다.' }
if ($Status) { ShowStatus $chosen }
elseif ($Download) { DoDownload (ForDownload $chosen) }
elseif ($Install) { DoInstall (ForInstall $chosen) }
elseif ($Uninstall) { DoUninstall $chosen }
elseif ($Search) { throw '-Search는 아직 없습니다(Task 2).' }
elseif ($Doc) { throw '-Doc은 아직 없습니다(Task 3).' }
$global:LASTEXITCODE = 0
exit 0
```

- [ ] **Step 8: 시험을 돌려 통과를 본다**

Run: `pwsh E:\TlToybox\tools\workshop.tests.ps1`
Expected: 모든 줄 PASS, 마지막 줄 `FAILS=0`(53개).

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Status`
Expected: 실제 목록 17줄이 상태 열과 함께 나오고 마지막 줄 `받음 15/15, 설치 15/15 | 보류 1 | 제외 1`. 아무것도 쓰지 않는다(`tools/workshop/history.txt`가 Step 2의 31줄 그대로).

- [ ] **Step 9: 커밋**

```bash
git -C E:\TlToybox add tools/workshop.ps1 tools/workshop.tests.ps1 tools/workshop/mods.txt tools/workshop/history.txt
git -C E:\TlToybox commit -m "feat: 창작마당 목록을 일곱 칸(상태·모드ID)으로, 상태별 받기·설치·지움, 이력 파일과 -NoHistory, 시험 53개

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 2: `-Search` 후보표

**Files:**
- Modify: `tools/workshop.ps1` (동작 고르기 바로 위에 검색 블록을 넣고, `-Search` 분기를 바꾼다)
- Modify: `tools/workshop.tests.ps1` (시험 12를 정리 루프 앞에 넣는다)
- Modify: `.gitignore`
- Create(생성): `tools/workshop/candidates-20261016.md`

**Interfaces:**
- Consumes: Task 1의 `ReadList`, 줄 객체의 `Id`·`State`, `$listDir`, `$utf8`, 매개변수 `-Kind -Top -Delay -Out -WebRoot`.
- Produces: `Fetch([string]$url, [string]$local, [hashtable]$form)` → `@{ Ok; Status; Body; Limited }`(Task 3의 컬렉션 조회가 쓴다), `Rest`, `StripTags`, `Mb`, `DateOf`, `$ItemUrl`(항목 페이지 주소 앞부분. Task 3의 문서 링크가 쓴다), `DoSearch($rows)`.
- 저장 응답 파일 이름(`-WebRoot`): 검색 페이지 `browse-<태그 또는 검색어>-p<쪽>.html`, 상세 `details.json`, 항목 `item-<ID>.html`, 컬렉션 `collection.json`.

- [ ] **Step 1: 시험 12를 쓴다**

`tools/workshop.tests.ps1`의 `foreach ($root in $boxes) {` 줄 바로 앞에 넣는다.

```powershell
# 12. -Search: 저장해 둔 응답(-WebRoot)으로 후보표를 만든다. 네트워크는 쓰지 않는다
$b = NewBox
function Links([string[]]$ids) { '<html><body>' + (($ids | ForEach-Object { '<a href="https://steamcommunity.com/sharedfiles/filedetails/?id=' + $_ + '&searchtext=">x</a>' }) -join "`n") + '</body></html>' }
function Det([string]$id, [string]$title, [long]$updated, [string]$size, [string[]]$tags, [bool]$banned = $false, [string]$desc = '설명') {
  @{ publishedfileid = $id; result = 1; title = $title; description = $desc; time_updated = $updated; file_size = $size; lifetime_subscriptions = 1000; favorited = 10; visibility = 0; banned = $banned; tags = @($tags | ForEach-Object { @{ tag = $_ } }) }
}
WriteFile "$($b.web)\browse-Beautify-p1.html" (Links @('111', '777', '1001', '1002', '1003', '1004', '1005'))
WriteFile "$($b.web)\browse-Beautify-p2.html" '<html><body>no items</body></html>'
WriteFile "$($b.web)\browse-Battle-p1.html" (Links @('2001', '2002', '1001'))
WriteFile "$($b.web)\browse-Battle-p2.html" '<html></html>'
WriteFile "$($b.web)\browse-功法-p1.html" (Links @('2003'))
WriteFile "$($b.web)\browse-功法-p2.html" '<html></html>'
WriteFile "$($b.web)\browse-Drama-p1.html" (Links @('3001'))
WriteFile "$($b.web)\browse-Drama-p2.html" '<html></html>'
WriteFile "$($b.web)\browse-NPC-p1.html" '<html></html>'
WriteFile "$($b.web)\browse-LargeIntegrated-p1.html" '<html></html>'
$dets = @(
  (Det '111' 'one' 1735000000 '1000' @('Beautify')), (Det '777' 'seven' 1735000000 '1000' @('Beautify')),
  (Det '1001' '双修立绘' 1735000000 '1000' @('Beautify') $false ('가나다' * 100)),
  (Det '1002' 'old one' 1600000000 '1000' @('Beautify')),
  (Det '1003' 'big one' 1735000000 '300000000' @('Beautify')),
  (Det '1004' 'banned one' 1735000000 '1000' @('Beautify') $true),
  (Det '2001' '功法升级' 1735000000 '1000' @('Battle')),
  (Det '2002' 'big world' 1735000000 '1000' @('Battle', 'LargeIntegrated')),
  (Det '2003' '【x】<color=#FF0000>功法</color>' 1735000000 '1000' @('Battle')),
  (Det '3001' 'drama' 1735000000 '1000' @('Drama')))
WriteFile "$($b.web)\details.json" (@{ response = @{ result = 1; resultcount = $dets.Count; publishedfiledetails = $dets } } | ConvertTo-Json -Depth 6)
WriteFile "$($b.web)\item-1001.html" '<div class="requiredItemsContainer" id="RequiredItems"><a href="https://steamcommunity.com/workshop/filedetails/?id=2814703549" target="_blank"><div class="requiredItem">Fatury   </div></a></div>'
WriteFile "$($b.web)\item-1002.html" '<html><body>no required block</body></html>'
foreach ($id in @('2001', '2002', '2003', '3001')) { WriteFile "$($b.web)\item-$id.html" '<html></html>' }
$out = "$($b.root)\cand.md"
$o = Run $b $none @{ Search = $true; Out = $out }
$t = if (Test-Path $out) { [IO.File]::ReadAllText($out) } else { '' }
Check 'search writes a table per kind with the candidate count' ($t -match '(?m)^## 외형 \(3개 후보\)' -and $t -match '(?m)^## 공법 \(3개 후보\)' -and $t -match '(?m)^## 세계관 \(1개 후보\)' -and $t -match '(?m)^## 음악 \(0개 후보\)') $t
Check 'search marks listed ids with their state and gives them no rank' ($t -match '\| - \| 111 \|.*목록에 있음\(설치\)' -and $t -match '\| - \| 777 \|.*목록에 있음\(제외\)') $t
Check 'search drops a banned item and an item the details api did not return' (-not ($t -match '\| 1004 \|') -and -not ($t -match '\| 1005 \|')) $t
Check 'search flags adult, old, big, system and large items' ($t -match '\| 1001 \|.*\| 성인향\? \|' -and $t -match '\| 1002 \|.*\| 오래됨 \|' -and $t -match '\| 1003 \|.*\| 큼 \|' -and $t -match '\| 2001 \|.*\| 시스템\? \|' -and $t -match '\| 2002 \|.*\| 대형 \|') $t
Check 'search reads required items, a page without the block and a missing page' ($t -match '\| 1001 \|(?: [^|]* \|){6} 2814703549 Fatury \|' -and $t -match '\| 1002 \|(?: [^|]* \|){6} - \|' -and $t -match '\| 1003 \|(?: [^|]* \|){6} 조회 실패 \|') $t
$gf = $t.Substring($t.IndexOf('## 공법'), $t.IndexOf('## 세계관') - $t.IndexOf('## 공법'))
Check 'search does not repeat an id an earlier kind took and joins the queries of a kind in order' (-not ($gf -match '\| 1001 \|') -and $gf -match '\| 1 \| 2001 \|' -and $gf -match '\| 3 \| 2003 \|') $gf
Check 'search strips formatting tags from the title' ($t -match '\| 2003 \| 【x】功法 \|') $t
Check 'search puts the first 200 characters of the description under the table' ($t -match '(?m)^- 1001: (?:가나다){66}가나…$') $t
Check 'search writes the description file next to the table' ((Test-Path "$($b.root)\cand-desc.txt") -and ([IO.File]::ReadAllText("$($b.root)\cand-desc.txt")) -match "(?m)^1001`t") $t
Check 'search prints a line per kind and the file name' ($o -match '외형: 후보 3, 목록에 있음 2' -and $o -match '후보표: ') $o
$o = Run $b $none @{ Search = $true; Out = $out; Top = 2 }
$t = [IO.File]::ReadAllText($out)
Check 'search stops a kind at -Top' ($t -match '(?m)^## 외형 \(2개 후보\)' -and -not ($t -match '\| 1003 \|')) $t
$o = Run $b $none @{ Search = $true; Out = $out; Kind = '외형,기타' }
Check 'an unknown kind is refused' ($o -match '^THROWN.*모르는 종류') $o
WriteFile "$($b.web)\browse-NPC-p1.html" '<html><body>您最近作出的请求太多了</body></html>'
$o = Run $b $none @{ Search = $true; Out = $out }
$t = [IO.File]::ReadAllText($out)
Check 'search stops at the rate limit, keeps what it has, names the place and does not fetch required items' ($t -match '요청 제한으로 멈춤: NPC 1쪽' -and $t -match '\| 3001 \|' -and $t -match '(?m)^## 음악 \(요청 제한으로 읽지 못함\)' -and $t -match '\| 1001 \|(?: [^|]* \|){6} 조회 안 함\(요청 제한\) \|') $t
```

- [ ] **Step 2: 시험을 돌려 실패를 본다**

Run: `pwsh E:\TlToybox\tools\workshop.tests.ps1`
Expected: 시험 1~11은 PASS, 시험 12의 첫 줄부터 FAIL(`-Search는 아직 없습니다`로 던져 파일이 없다). `FAILS=13`.

- [ ] **Step 3: 검색 블록을 쓴다**

`tools/workshop.ps1`에서 `$all = ReadList` 줄 바로 앞에 넣는다.

```powershell
# ---- -Search: 창작마당 후보표 (2026-10-16 설계 3.3절) ----
$UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) TlToybox-workshop/1.0'
$BrowseUrl = 'https://steamcommunity.com/workshop/browse/'
$DetailsUrl = 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/'
$ItemUrl = 'https://steamcommunity.com/sharedfiles/filedetails/?id='
$KindQueries = [ordered]@{
  '외형'   = @(@{ tag = 'Beautify' })
  '공법'   = @(@{ tag = 'Battle' }, @{ search = '功法' })
  '세계관' = @(@{ tag = 'Drama' }, @{ tag = 'NPC' }, @{ tag = 'LargeIntegrated' })
  '음악'   = @(@{ search = '音乐' }, @{ search = 'BGM' }, @{ search = '特效' })
}
$NsfwRe = '双修|雙修|采补|採補|裸|R18|NSFW|涩|澀|绅士|紳士|魅魔|合欢|合歡|处子|處子'
$SystemRe = '升级|升級|合成|分解|融合|经验|經驗|速度|修改器|工具|框架|UI|自动|自動|参悟|參悟|秘籍|秘笈'
$LimitRe = '请求太多'

function Rest { if (-not $WebRoot) { Start-Sleep -Milliseconds ([int]($Delay * 1000)) } }
# HTTP 한 번. $form이 있으면 폼 POST. -WebRoot가 있으면 그 폴더의 $local 파일을 읽는다(시험용).
# 돌려주는 것: Ok, Status, Body, Limited(요청 제한: HTTP 429 또는 본문의 "请求太多")
function Fetch([string]$url, [string]$local, [hashtable]$form = $null) {
  if ($WebRoot) {
    $path = Join-Path $WebRoot $local
    if (-not (Test-Path -LiteralPath $path)) { return @{ Ok = $false; Status = 404; Body = ''; Limited = $false } }
    $body = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
    return @{ Ok = $true; Status = 200; Body = $body; Limited = [bool]($body -match $LimitRe) }
  }
  $headers = @{ 'User-Agent' = $UA; 'Accept-Language' = 'zh-CN,ko;q=0.8' }
  try {
    if ($form) {
      $pairs = @($form.GetEnumerator() | ForEach-Object { [Uri]::EscapeDataString([string]$_.Key) + '=' + [Uri]::EscapeDataString([string]$_.Value) })
      $res = Invoke-WebRequest -Uri $url -Method Post -Body ($pairs -join '&') -ContentType 'application/x-www-form-urlencoded' -Headers $headers -TimeoutSec 60
    } else {
      $res = Invoke-WebRequest -Uri $url -Headers $headers -TimeoutSec 60
    }
    $body = [string]$res.Content
    @{ Ok = $true; Status = [int]$res.StatusCode; Body = $body; Limited = [bool]($body -match $LimitRe) }
  } catch {
    $status = 0
    if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
    @{ Ok = $false; Status = $status; Body = ''; Limited = ($status -eq 429) }
  }
}
function QueryName([hashtable]$q) { if ($q.tag) { $q.tag } else { $q.search } }
# 검색 페이지 한 쪽의 ID(나온 순서, 중복 없이). 구독자순, 바로 사용 가능한 항목, 한 쪽 30개
function BrowsePage([hashtable]$q, [int]$page) {
  $qs = "appid=$AppId&browsesort=totaluniquesubscribers&actualsort=totaluniquesubscribers&section=readytouseitems&numperpage=30&p=$page"
  if ($q.tag) { $qs += '&requiredtags[]=' + [Uri]::EscapeDataString($q.tag) } else { $qs += '&searchtext=' + [Uri]::EscapeDataString($q.search) }
  $res = Fetch "$BrowseUrl?$qs" "browse-$(QueryName $q)-p$page.html"
  $ids = New-Object System.Collections.Generic.List[string]
  if ($res.Ok) { foreach ($m in [regex]::Matches($res.Body, 'sharedfiles/filedetails/\?id=(\d+)')) { if (-not $ids.Contains($m.Groups[1].Value)) { $ids.Add($m.Groups[1].Value) } } }
  @{ Ok = $res.Ok; Ids = $ids.ToArray(); Limited = $res.Limited }
}
# 상세 API. 30개씩. ID → 상세(JSON 객체). 돌려주지 않은 ID, result가 1이 아닌 것, 차단된 것, 비공개는 뺀다
function Details([string[]]$ids) {
  $map = @{}
  for ($i = 0; $i -lt $ids.Count; $i += 30) {
    $batch = @($ids[$i..([math]::Min($i + 29, $ids.Count - 1))])
    $form = @{ itemcount = $batch.Count }
    for ($k = 0; $k -lt $batch.Count; $k++) { $form["publishedfileids[$k]"] = $batch[$k] }
    $res = Fetch $DetailsUrl 'details.json' $form
    if ($res.Ok) {
      $json = $null
      try { $json = $res.Body | ConvertFrom-Json } catch { $json = $null }
      if ($json) {
        foreach ($d in @($json.response.publishedfiledetails)) {
          $fid = [string]$d.publishedfileid
          if ($batch -notcontains $fid) { continue }
          if ([int]$d.result -ne 1 -or [bool]$d.banned -or [int]$d.visibility -ne 0) { continue }
          $map[$fid] = $d
        }
      }
    }
    Rest
  }
  $map
}
# 항목 페이지의 "필수 아이템". Text: '-'(없음), '조회 실패', 또는 'ID 이름, ID 이름'
function RequiredItems([string]$id) {
  $res = Fetch "$ItemUrl$id" "item-$id.html"
  if (-not $res.Ok) { return @{ Text = '조회 실패'; Limited = $res.Limited } }
  $i = $res.Body.IndexOf('id="RequiredItems"')
  if ($i -lt 0) { return @{ Text = '-'; Limited = $res.Limited } }
  $seg = $res.Body.Substring($i, [math]::Min(6000, $res.Body.Length - $i))
  $items = @(foreach ($m in [regex]::Matches($seg, 'filedetails/\?id=(\d+)[^>]*>\s*<div class="requiredItem">\s*([^<]*)')) { $m.Groups[1].Value + ' ' + $m.Groups[2].Value.Trim() })
  @{ Text = $(if ($items.Count -gt 0) { $items -join ', ' } else { '-' }); Limited = $res.Limited }
}
# 서식 태그를 지우고 한 줄로. 표의 칸 구분자 |는 전각으로
function StripTags([string]$s) {
  if (-not $s) { return '' }
  [regex]::Replace($s, '<[^>]*>', '').Replace("`r", ' ').Replace("`n", ' ').Replace('|', '｜').Trim()
}
# 표시(경고): 목록에 있음(상태), 성인향?, 오래됨(2023-01-01 이전 갱신), 큼(200MB 초과), 시스템?(공법 종류만), 대형
function Flags([string]$kind, $d, [string]$listState) {
  $f = New-Object System.Collections.Generic.List[string]
  if ($listState) { $f.Add("목록에 있음($listState)") }
  if ([string]$d.title -match $NsfwRe) { $f.Add('성인향?') }
  if ([long]$d.time_updated -lt 1672531200) { $f.Add('오래됨') }
  if ([long]$d.file_size -gt 200MB) { $f.Add('큼') }
  if ($kind -eq '공법' -and (([string]$d.title + ' ' + [string]$d.description) -match $SystemRe)) { $f.Add('시스템?') }
  if (@(@($d.tags) | ForEach-Object { [string]$_.tag }) -contains 'LargeIntegrated') { $f.Add('대형') }
  if ($f.Count -eq 0) { '' } else { $f -join ' ' }
}
function Mb([long]$bytes) { [math]::Round([double]$bytes / 1MB, 1) }
function DateOf([long]$unix) { [DateTimeOffset]::FromUnixTimeSeconds($unix).LocalDateTime.ToString('yyyy-MM-dd') }

# 후보표와 설명 전문 파일을 쓴다. $found: 종류 → ID 목록(목록에 있는 것 포함). 종류마다 요약 한 줄을 출력한다
function WriteCandidates([string[]]$kinds, $found, $listed, $details, $req, [string]$stopped) {
  $sb = New-Object System.Text.StringBuilder
  $desc = New-Object System.Text.StringBuilder
  [void]$sb.AppendLine("# 창작마당 후보표 ($(Get-Date -Format 'yyyy-MM-dd HH:mm'))")
  [void]$sb.AppendLine('')
  [void]$sb.AppendLine("도구 pwsh tools/workshop.ps1 -Search -Kind $($kinds -join ',') -Top $Top 이 만들었다(창작마당 검색 페이지의 구독자순). 표시는 경고일 뿐 자동으로 빼지 않는다. 호환성은 댓글을 읽지 않으므로 갱신일로만 가늠한다. 한국어 요약·추천 열은 사람이 채운다. DLL 유무는 받은 뒤 -Status로 본다. 설명 전문은 같은 이름의 -desc.txt(git 제외).")
  foreach ($k in $kinds) {
    [void]$sb.AppendLine('')
    if (-not $found.Contains($k)) { [void]$sb.AppendLine("## $k (요청 제한으로 읽지 못함)"); "${k}: 읽지 못함"; continue }
    $ids = @($found[$k])
    $cands = @($ids | Where-Object { -not $listed.ContainsKey($_) -and $details.ContainsKey($_) })
    $inList = @($ids | Where-Object { $listed.ContainsKey($_) -and $details.ContainsKey($_) })
    "${k}: 후보 $($cands.Count), 목록에 있음 $($inList.Count)"
    [void]$sb.AppendLine("## $k ($($cands.Count)개 후보)")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('| 순위 | ID | 제목 | 구독(누적) | 즐겨찾기 | 갱신일 | 크기 | 태그 | 필수 항목 | 표시 | 한국어 요약 | 추천 |')
    [void]$sb.AppendLine('|---|---|---|---|---|---|---|---|---|---|---|---|')
    $rank = 0
    foreach ($id in $ids) {
      if (-not $details.ContainsKey($id)) { continue }
      $d = $details[$id]
      $isListed = $listed.ContainsKey($id)
      $no = if ($isListed) { '-' } else { $rank++; "$rank" }
      $need = if ($isListed) { '-' } elseif ($req.ContainsKey($id)) { $req[$id] } elseif ($stopped) { '조회 안 함(요청 제한)' } else { '조회 실패' }
      $tags = (@(@($d.tags) | ForEach-Object { [string]$_.tag }) -join ',')
      $flag = Flags $k $d $(if ($isListed) { $listed[$id] } else { '' })
      [void]$sb.AppendLine("| $no | $id | $(StripTags $d.title) | $($d.lifetime_subscriptions) | $($d.favorited) | $(DateOf $d.time_updated) | $(Mb $d.file_size)MB | $tags | $need | $flag |  |  |")
      [void]$desc.AppendLine($id + "`t" + ([string]$d.description).Replace("`r", '').Replace("`n", '\n'))
    }
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('설명(앞 200자):')
    foreach ($id in $cands) {
      $text = StripTags $details[$id].description
      if ($text.Length -gt 200) { $text = $text.Substring(0, 200) + '…' }
      [void]$sb.AppendLine("- ${id}: $text")
    }
  }
  if ($stopped) { [void]$sb.AppendLine(''); [void]$sb.AppendLine("요청 제한으로 멈춤: $stopped") }
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $Out))
  [IO.File]::WriteAllText($Out, $sb.ToString(), $utf8)
  $descPath = Join-Path (Split-Path -Parent $Out) ([IO.Path]::GetFileNameWithoutExtension($Out) + '-desc.txt')
  [IO.File]::WriteAllText($descPath, $desc.ToString(), $utf8)
}

function DoSearch($rows) {
  $kinds = @($Kind.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })
  foreach ($k in $kinds) { if (-not $KindQueries.Contains($k)) { throw "모르는 종류입니다(외형·공법·세계관·음악): $k" } }
  $listed = @{}; foreach ($r in $rows) { $listed[$r.Id] = $r.State }
  $taken = @{}                 # 앞 종류에서 후보가 된 ID
  $found = [ordered]@{}        # 종류 → 표에 넣을 ID(나온 순서. 목록에 있는 것은 표시용으로 함께)
  $stopped = ''                # 요청 제한으로 멈춘 자리
  foreach ($k in $kinds) {
    $ids = New-Object System.Collections.Generic.List[string]
    $count = 0
    :queries foreach ($q in $KindQueries[$k]) {
      for ($p = 1; $p -le 5; $p++) {
        $page = BrowsePage $q $p
        if ($page.Limited) { $stopped = "$(QueryName $q) ${p}쪽"; break queries }
        if (-not $page.Ok -or $page.Ids.Count -eq 0) { break }
        foreach ($id in $page.Ids) {
          if ($ids.Contains($id) -or $taken.ContainsKey($id)) { continue }
          $ids.Add($id)
          if (-not $listed.ContainsKey($id)) { $count++; if ($count -ge $Top) { break queries } }
        }
        Rest
      }
    }
    foreach ($id in $ids) { if (-not $listed.ContainsKey($id)) { $taken[$id] = $true } }
    $found[$k] = $ids.ToArray()
    if ($stopped) { break }
  }
  $all = @(@(foreach ($k in $found.Keys) { $found[$k] }) | Select-Object -Unique)
  $details = if ($all.Count -gt 0) { Details ([string[]]$all) } else { @{} }
  $req = @{}
  if (-not $stopped) {
    foreach ($id in $all) {
      if ($listed.ContainsKey($id) -or -not $details.ContainsKey($id)) { continue }
      $r = RequiredItems $id
      if ($r.Limited) { $stopped = "항목 $id"; break }
      $req[$id] = $r.Text
      Rest
    }
  }
  WriteCandidates $kinds $found $listed $details $req $stopped
  if ($stopped) { "요청 제한으로 멈춤: $stopped (모은 것까지 썼다. 나중에 다시 돌린다)" }
  "후보표: $Out"
}
```

동작 고르기의 `elseif ($Search) { throw '-Search는 아직 없습니다(Task 2).' }`를 `elseif ($Search) { DoSearch $all }`로 바꾼다.

- [ ] **Step 4: 시험을 돌려 통과를 본다**

Run: `pwsh E:\TlToybox\tools\workshop.tests.ps1`
Expected: `FAILS=0`(66개).

- [ ] **Step 5: `.gitignore`에 설명 전문 파일을 더한다**

`.gitignore` 끝에:

```text

# 창작마당 후보표의 설명 전문(크기와 저작권. tools/workshop.ps1 -Search가 다시 만든다)
tools/workshop/candidates-*-desc.txt
```

- [ ] **Step 6: 실제로 한 번 돌려 후보표를 만든다**

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Search`
Expected: 종류마다 `외형: 후보 20, 목록에 있음 n` 같은 줄 넷, 마지막 `후보표: E:\TlToybox\tools\workshop\candidates-20261016.md`. 3분 안팎. 요청 제한에 걸리면 `요청 제한으로 멈춤: …` 줄이 나오고 파일에도 적힌다. 그러면 10분 뒤 `-Delay 3`으로 다시 돌린다(같은 날이라 같은 파일을 덮는다). 파일을 열어 종류 넷의 표와 설명 소절이 있는지, `tools/workshop/candidates-20261016-desc.txt`가 `git status`에 나오지 않는지(무시됨) 본다.

- [ ] **Step 7: 커밋**

```bash
git -C E:\TlToybox add tools/workshop.ps1 tools/workshop.tests.ps1 .gitignore tools/workshop/candidates-20261016.md
git -C E:\TlToybox commit -m "feat: 창작마당 검색 -Search — 종류별 질의(태그·검색어)·상세 API·필수 항목·표시·요청 제한 멈춤, -WebRoot 시험 13개, 첫 후보표

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 3: `-Doc` 등록부 문서·컬렉션 비교·설명문, 앞 세션 후보표

**Files:**
- Modify: `tools/workshop.ps1` (검색 블록 뒤에 문서 블록, `-Doc` 분기)
- Modify: `tools/workshop.tests.ps1` (시험 13을 정리 루프 앞에 넣는다)
- Create: `tools/workshop/candidates-20261015.md`
- Create(생성): `docs/09-workshop-mods.md`, `tools/workshop/collection-desc.txt`

**Interfaces:**
- Consumes: Task 1의 줄 객체·`$listDir`·`$utf8`·`Downloaded`·`Source`·`SizeMb`·`Updated`·`DiffCount`·`Dlls`·`Installed`·`IsInstalled`, Task 2의 `Fetch`·`$ItemUrl`, 매개변수 `-DocOut -CollectionFile -TranslationFile -History -WebRoot`.
- Produces: `ReadHistory()`(객체 `When, Action, Id, Korean`), `ReadCollectionId()`, `TranslationRows()`(-1이면 파일 없음), `CollectionChildren([string]$cid)` → `@{ Ok; Ids; Reason }`, `WriteCollectionDesc($target)`, `DoDoc($rows)`.

- [ ] **Step 1: 시험 13을 쓴다**

`tools/workshop.tests.ps1`의 `foreach ($root in $boxes) {` 줄 바로 앞(시험 12 뒤)에 넣는다.

```powershell
# 13. -Doc: 목록·폴더·이력·컬렉션으로 등록부 문서와 컬렉션 설명문을 만든다
$b = NewBox
[void](Run $b $none @{ Install = $true; Yes = $true })      # 111·222 설치 → 이력 두 줄
[void](Run $b $none @{ Download = $true })                  # 444·555 받음 → 이력 두 줄
WriteFile "$($b.root)\collection.txt" "# 컬렉션 ID`n5000`n"
WriteFile "$($b.web)\collection.json" '{"response":{"result":1,"resultcount":1,"collectiondetails":[{"publishedfileid":"5000","result":1,"children":[{"publishedfileid":"111","sortorder":0,"filetype":0},{"publishedfileid":"222","sortorder":1,"filetype":0},{"publishedfileid":"9999","sortorder":2,"filetype":0}]}]}}'
WriteFile "$($b.root)\tr.txt" "# 주석`nkey1`t하나`nkey2`t둘`n깨진 줄`n"
WriteFile "$($b.root)\candidates-20260101.md" '# 후보표'
$doc = "$($b.root)\doc.md"
$docArgs = @{ Doc = $true; DocOut = $doc; CollectionFile = "$($b.root)\collection.txt"; TranslationFile = "$($b.root)\tr.txt" }
$o = Run $b $none $docArgs
$t = if (Test-Path $doc) { [IO.File]::ReadAllText($doc) } else { '' }
Check 'doc has the six sections' ($t -match '(?m)^## 1\. 요약' -and $t -match '(?m)^## 2\. 모드 표' -and $t -match '(?m)^## 3\. 이력' -and $t -match '(?m)^## 4\. 컬렉션 비교' -and $t -match '(?m)^## 5\. 후보표' -and $t -match '(?m)^## 6\. 한글화') $t
Check 'doc summary counts states, install rows and dll mods' ($t -match '- 상태: 설치 5, 보류 1, 제외 1 \(줄 7\)' -and $t -match '- 설치 줄 5: 받음 5/5, 설치 2/5' -and $t -match '- DLL이 든 설치 모드: 1개') $t
Check 'doc table has a row per list line with the live state and a link' (@([regex]::Matches($t, '(?m)^\| \d+ \|')).Count -eq 7 -and $t -match '\| 111 \| 설치 \| 외형 \| one \| 하나 \| - \| 0(\.0)?MB 2024-12-28 \| 예 \| 같음 \| MOD_x\.dll \| DLL 1 \| \[창작마당\]\(https://steamcommunity\.com/sharedfiles/filedetails/\?id=111\) \|' -and $t -match '\| 777 \| 제외 \| 외형 \| seven \| 일곱 \| - \| - \| 예 \| - \| - \| 제외 사유 \|') $t
Check 'doc lists the history newest first' (($t.IndexOf('| 받음 | 555 |') -lt $t.IndexOf('| 설치 | 111 |')) -and $t -match "\| $when \| 설치 \| 111 \| 하나 \|") $t
Check 'doc compares the install rows with the collection' ($t -match '- 컬렉션 5000: https://steamcommunity\.com/sharedfiles/filedetails/\?id=5000' -and $t -match '- 일치 2' -and $t -match '- 컬렉션에만: 9999' -and $t -match '- 설치 줄에만: 333, 444, 555') $t
Check 'doc lists the candidate files and counts translation rows' ($t -match '- \[candidates-20260101\.md\]' -and $t -match 'mods\.txt: 2행') $t
$d = if (Test-Path "$($b.root)\collection-desc.txt") { [IO.File]::ReadAllText("$($b.root)\collection-desc.txt") } else { '' }
Check 'doc writes the collection description with the install rows by kind' ($d -match '(?m)^\[외형\]' -and $d -match '(?m)^- one\(하나\)' -and $d -match '(?m)^\[공법\]' -and $d -match '(?m)^- four\(넷\)' -and -not ($d -match 'seven') -and $d -match '설치 5개') $d
Check 'doc says where it wrote' ($o -match '문서: ' -and $o -match '설명문: ') $o
[IO.File]::Delete("$($b.root)\collection.txt")
$o = Run $b $none $docArgs
$t = [IO.File]::ReadAllText($doc)
Check 'doc reports the collection as missing without the id file' ($t -match '컬렉션 없음') $t
WriteFile "$($b.root)\collection.txt" "5000`n"
[IO.File]::Delete("$($b.web)\collection.json")
$o = Run $b $none $docArgs
$t = [IO.File]::ReadAllText($doc)
Check 'doc reports a failed collection lookup and still writes the rest' ($t -match '- 조회 실패: HTTP 404' -and $t -match '(?m)^## 5\. 후보표') $t
$o = Run $b $none @{ Doc = $true; DocOut = $doc; CollectionFile = "$($b.root)\collection.txt"; TranslationFile = "$($b.root)\no-such.txt" }
$t = [IO.File]::ReadAllText($doc)
Check 'doc reports a missing translation file' ($t -match '번역 파일 없음') $t
```

- [ ] **Step 2: 시험을 돌려 실패를 본다**

Run: `pwsh E:\TlToybox\tools\workshop.tests.ps1`
Expected: 시험 1~12 PASS, 시험 13의 첫 줄부터 FAIL(`-Doc은 아직 없습니다`). `FAILS=11`.

- [ ] **Step 3: 문서 블록을 쓴다**

`tools/workshop.ps1`에서 `$all = ReadList` 줄 바로 앞(검색 블록 뒤)에 넣는다.

```powershell
# ---- -Doc: 등록부 문서·컬렉션 비교·설명문 (2026-10-16 설계 3.4절) ----
$CollectionUrl = 'https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/'
# 이력 파일. 줄: yyyy-MM-dd HH:mm | 동작 | ID | 한국어 이름 (시각 자리에 --:--도 온다)
function ReadHistory {
  if (-not (Test-Path -LiteralPath $History)) { return @() }
  $items = New-Object System.Collections.Generic.List[object]
  foreach ($line in [IO.File]::ReadAllLines($History)) {
    $m = [regex]::Match($line, '^\s*(\S+ \S+) \| (\S+) \| (\d+) \| (.*)$')
    if ($m.Success) { $items.Add([pscustomobject]@{ When = $m.Groups[1].Value; Action = $m.Groups[2].Value; Id = $m.Groups[3].Value; Korean = $m.Groups[4].Value.Trim() }) }
  }
  $items.ToArray()
}
# 컬렉션 ID 파일의 첫 줄(# 줄과 빈 줄 제외). 없으면 ''
function ReadCollectionId {
  if (-not (Test-Path -LiteralPath $CollectionFile)) { return '' }
  foreach ($line in [IO.File]::ReadAllLines($CollectionFile)) { $t = $line.Trim(); if ($t -and -not $t.StartsWith('#')) { return $t } }
  ''
}
# 번역 파일의 행 수(탭이 있는 줄). 파일이 없으면 -1
function TranslationRows {
  if (-not (Test-Path -LiteralPath $TranslationFile)) { return -1 }
  @([IO.File]::ReadAllLines($TranslationFile) | Where-Object { $_.Trim() -and -not $_.TrimStart().StartsWith('#') -and $_.Contains("`t") }).Count
}
# 컬렉션의 항목 ID(공개 API). Ok, Ids, Reason
function CollectionChildren([string]$cid) {
  $res = Fetch $CollectionUrl 'collection.json' @{ collectioncount = 1; 'publishedfileids[0]' = $cid }
  if (-not $res.Ok) { return @{ Ok = $false; Ids = @(); Reason = "HTTP $($res.Status)" } }
  $json = $null
  try { $json = $res.Body | ConvertFrom-Json } catch { return @{ Ok = $false; Ids = @(); Reason = 'JSON이 아님' } }
  $c = @($json.response.collectiondetails) | Select-Object -First 1
  if (-not $c -or [int]$c.result -ne 1) { return @{ Ok = $false; Ids = @(); Reason = "result=$(if ($c) { $c.result } else { '없음' })" } }
  @{ Ok = $true; Ids = @(@($c.children) | ForEach-Object { [string]$_.publishedfileid }); Reason = '' }
}
# 컬렉션 설명문(사람이 Steam의 설명 칸에 붙여 넣는다). 설치 줄만, 종류별로
function WriteCollectionDesc($target) {
  $lines = New-Object System.Collections.Generic.List[string]
  $lines.Add("TlToybox 편집기와 함께 쓰는 창작마당 모드 목록($(Get-Date -Format 'yyyy-MM-dd') 기준, 설치 $($target.Count)개)")
  foreach ($k in $Kinds) {
    $ofKind = @($target | Where-Object { $_.Kind -eq $k })
    if ($ofKind.Count -eq 0) { continue }
    $lines.Add(''); $lines.Add("[$k]")
    foreach ($r in $ofKind) { $lines.Add("- $($r.Title)($($r.Korean))") }
  }
  $lines.Add('')
  $lines.Add('이 설치본은 Steam을 거치지 않아 SteamCMD로 받아 ModExportData\<창작마당ID>\에 넣는다. 도구: 저장소 tools/workshop.ps1')
  $path = Join-Path $listDir 'collection-desc.txt'
  [IO.File]::WriteAllText($path, ($lines -join "`n") + "`n", $utf8)
  $path
}
function DoDoc($rows) {
  $target = @($rows | Where-Object { $_.State -eq '설치' })
  $held = @($rows | Where-Object { $_.State -eq '보류' }).Count
  $excl = @($rows | Where-Object { $_.State -eq '제외' }).Count
  # 2절의 줄을 먼저 계산한다(1절의 합계가 필요하다)
  $tableRows = New-Object System.Collections.Generic.List[string]
  $got = 0; $inst = 0; $sizeSum = 0.0; $dllMods = 0
  foreach ($r in $rows) {
    $root = Downloaded $r.Id
    $s = Source $r.Id
    $isInst = IsInstalled $r.Id
    $have = if ($root) { "$(SizeMb $root)MB $(Updated $r.Id)" } else { '-' }
    $fresh = if ($s.Path -and $isInst) { $n = DiffCount $s.Path (Installed $r.Id); if ($n -eq 0) { '같음' } else { "다름 $n" } } else { '-' }
    $dlls = if ($root) { @(Dlls $root) } else { @() }
    if ($r.State -eq '설치') {
      if ($root) { $got++ }
      if ($isInst) { $inst++; $sizeSum += SizeMb (Installed $r.Id); if ($dlls.Count -gt 0) { $dllMods++ } }
    }
    $tableRows.Add("| $($r.Id) | $($r.State) | $($r.Kind) | $($r.Title) | $($r.Korean) | $($r.ModId) | $have | $(if ($isInst) { '예' } else { '-' }) | $fresh | $(if ($dlls.Count -gt 0) { $dlls -join ',' } else { '-' }) | $($r.Note) | [창작마당]($ItemUrl$($r.Id)) |")
  }
  $lines = New-Object System.Collections.Generic.List[string]
  $lines.Add("# 09. 창작마당 모드 등록부 (생성 $(Get-Date -Format 'yyyy-MM-dd HH:mm'))")
  $lines.Add('')
  $lines.Add('이 문서는 pwsh tools/workshop.ps1 -Doc 이 만든다. 손으로 고치지 않는다. 고치려면 tools/workshop/mods.txt 와 history.txt 를 고친 뒤 다시 돌린다. 설계: docs/superpowers/specs/2026-10-16-workshop-expand-design.md 3.4절.')
  $lines.Add("게임 폴더 $GameDir, SteamCMD $SteamCmd, 받은 자리 $ContentDir.")
  $lines.Add(''); $lines.Add('## 1. 요약'); $lines.Add('')
  $lines.Add("- 상태: 설치 $($target.Count), 보류 $held, 제외 $excl (줄 $($rows.Count))")
  $lines.Add("- 설치 줄 $($target.Count): 받음 $got/$($target.Count), 설치 $inst/$($target.Count)")
  $lines.Add("- 설치본 크기: $([math]::Round($sizeSum, 1))MB (설치된 설치 줄의 폴더 합)")
  $lines.Add("- DLL이 든 설치 모드: ${dllMods}개")
  $lines.Add(''); $lines.Add('## 2. 모드 표'); $lines.Add('')
  $lines.Add('| ID | 상태 | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 받음 | 설치 | 설치본 | DLL | 비고 | 링크 |')
  $lines.Add('|---|---|---|---|---|---|---|---|---|---|---|---|')
  foreach ($t in $tableRows) { $lines.Add($t) }
  $lines.Add(''); $lines.Add('## 3. 이력 (최근 것부터)'); $lines.Add('')
  $hist = @(ReadHistory)
  if ($hist.Count -eq 0) { $lines.Add('이력 없음') }
  else {
    $lines.Add('| 때 | 동작 | ID | 한국어 이름 |'); $lines.Add('|---|---|---|---|')
    [array]::Reverse($hist)
    foreach ($h in $hist) { $lines.Add("| $($h.When) | $($h.Action) | $($h.Id) | $($h.Korean) |") }
  }
  $lines.Add(''); $lines.Add('## 4. 컬렉션 비교'); $lines.Add('')
  $cid = ReadCollectionId
  if (-not $cid) { $lines.Add('컬렉션 없음 (tools/workshop/collection.txt 에 컬렉션 ID를 한 줄 적으면 비교한다)') }
  else {
    $c = CollectionChildren $cid
    $lines.Add("- 컬렉션 ${cid}: $ItemUrl$cid")
    if (-not $c.Ok) { $lines.Add("- 조회 실패: $($c.Reason)") }
    else {
      $want = @($target | ForEach-Object { $_.Id })
      $onlyColl = @($c.Ids | Where-Object { $want -notcontains $_ })
      $onlyList = @($want | Where-Object { $c.Ids -notcontains $_ })
      $lines.Add("- 일치 $(@($want | Where-Object { $c.Ids -contains $_ }).Count)")
      $lines.Add("- 컬렉션에만: $(if ($onlyColl.Count -gt 0) { $onlyColl -join ', ' } else { '없음' })")
      $lines.Add("- 설치 줄에만: $(if ($onlyList.Count -gt 0) { $onlyList -join ', ' } else { '없음' })")
    }
  }
  $lines.Add(''); $lines.Add('## 5. 후보표'); $lines.Add('')
  $candFiles = @(Get-ChildItem -LiteralPath $listDir -File -Filter 'candidates-*.md' -ErrorAction SilentlyContinue | Sort-Object Name | ForEach-Object Name)
  if ($candFiles.Count -eq 0) { $lines.Add('후보표 없음') } else { foreach ($cf in $candFiles) { $lines.Add("- [$cf](../tools/workshop/$cf)") } }
  $lines.Add(''); $lines.Add('## 6. 한글화'); $lines.Add('')
  $tr = TranslationRows
  if ($tr -lt 0) { $lines.Add("- 번역 파일 없음: $TranslationFile") } else { $lines.Add("- 번역 파일 src/ModCode/ModMain/Translations/mods.txt: ${tr}행. 모드별 상태는 한글화(K)에서 정한다.") }
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $DocOut))
  [IO.File]::WriteAllText($DocOut, ($lines -join "`n") + "`n", $utf8)
  $descPath = WriteCollectionDesc $target
  "문서: $DocOut (모드 $($rows.Count)줄, 이력 $($hist.Count)줄)"
  "설명문: $descPath"
}
```

동작 고르기의 `elseif ($Doc) { throw '-Doc은 아직 없습니다(Task 3).' }`를 `elseif ($Doc) { DoDoc $all }`로 바꾼다.

- [ ] **Step 4: 시험을 돌려 통과를 본다**

Run: `pwsh E:\TlToybox\tools\workshop.tests.ps1`
Expected: `FAILS=0`(77개).

- [ ] **Step 5: 앞 세션의 자동 목록을 옮긴다**

`D:\Download\output\mod_list.md`(2026-10-15)를 읽어 `tools/workshop/candidates-20261015.md`를 만든다. 머리에 다음을 두고 그 아래에 원본 표(외형 15·공법 14)를 그대로 붙인다.

```markdown
# 창작마당 후보표 (2026-10-15, 앞 세션의 자동 목록)

출처: 사용자가 첨부한 `D:\Download\build_lists.py`의 결과 `output\mod_list.md`(점수 = 누적 구독 + 5×즐겨찾기, 제목 낱말로 외형·공법 분류). 도구 `tools/workshop.ps1 -Search`가 만든 것이 아니라 열 모양이 다르다. 2026-10-15에 사용자는 이 목록 대신 설명을 읽어 고르기로 했고, 고른 16개와 제외 1개는 05 문서 41절과 `tools/workshop/mods.txt`에 있다. 자동 목록의 공법 14개는 대부분 새 공법을 더하지 않는 편의·시스템 모드였고, 외형 15개에도 세계관·종문·BGM 모드가 섞여 있었다(인계 8.3b절).

```

Run: `py -3 -I -X utf8 -c "import sys; print(open(r'D:\Download\output\mod_list.md', encoding='utf-8').read())"`로 원본을 보고 그대로 옮긴다(`python`은 이 PC에서 돌지 않는다).

- [ ] **Step 6: 실제로 등록부를 만든다**

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Doc`
Expected: `문서: E:\TlToybox\docs\09-workshop-mods.md (모드 17줄, 이력 31줄)`, `설명문: E:\TlToybox\tools\workshop\collection-desc.txt`. 문서를 열어 1절 `- 상태: 설치 15, 보류 1, 제외 1 (줄 17)`, `- 설치 줄 15: 받음 15/15, 설치 15/15`, 2절 17줄, 3절 31줄(최근 것부터), 4절 `컬렉션 없음`, 5절에 `candidates-20261015.md`·`candidates-20261016.md`, 6절 `59행`을 본다. 게임 폴더에는 아무것도 쓰지 않았다(`git -C E:\TlToybox status`에 docs/09와 tools/workshop의 두 파일만).

- [ ] **Step 7: 커밋**

```bash
git -C E:\TlToybox add tools/workshop.ps1 tools/workshop.tests.ps1 tools/workshop/candidates-20261015.md tools/workshop/collection-desc.txt docs/09-workshop-mods.md
git -C E:\TlToybox commit -m "feat: 등록부 -Doc — docs/09 생성(요약·모드 표·이력·컬렉션 비교·후보표·한글화), 컬렉션 설명문, 시험 11개, 앞 세션 자동 목록 보관

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 4: 요약·추천 채우기와 사용자 선정 (컨트롤러가 직접. 서브에이전트에 맡기지 않는다)

**Files:**
- Modify: `tools/workshop/candidates-20261016.md` (`한국어 요약`·`추천` 열)
- Modify: `tools/workshop/mods.txt` (고른 모드를 `설치` 줄로)

**Interfaces:**
- Consumes: Task 2의 후보표와 `candidates-20261016-desc.txt`(ID<TAB>설명 전문).
- Produces: `mods.txt`의 새 `설치` 줄(모드ID `-`, 비고에 필수 항목). Task 5가 받는다.

- [ ] **Step 1: 설명 전문을 읽고 요약·추천을 채운다**

`tools/workshop/candidates-20261016-desc.txt`를 ID 순서대로 읽는다(중국어). 후보표의 후보 줄(순위가 있는 줄)마다 `한국어 요약`(한 줄, 무엇을 더하는지)과 `추천`(`추천`·`보통`·`비추천` + 이유 한 줄)을 채운다. 기준(스펙 3.5절): 새 내용을 더하는가(공법은 새 공법. 시스템·편의는 비추천), 갱신일(2024년 이후면 좋음), 필수 항목을 함께 넣을 수 있는가, 성인향 아님, 같은 영역의 설치 모드와 겹치지 않는가. 표 머리에 "요약·추천은 2026-10-16에 사람이 채웠다. 호환성은 갱신일로만 가늠했다" 한 줄을 더한다.

- [ ] **Step 2: 커밋**

```bash
git -C E:\TlToybox add tools/workshop/candidates-20261016.md
git -C E:\TlToybox commit -m "docs: 후보표에 한국어 요약·추천 채움(2026-10-16)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

- [ ] **Step 3: 사용자에게 보이고 고르게 한다**

종류마다 추천 줄을 먼저, 나머지를 뒤에 두어 `ID | 제목 | 한국어 요약 | 갱신일 | 크기 | 필수 항목 | 표시 | 추천` 표로 보인다(한국어). 사용자가 고를 ID를 말할 때까지 기다린다(이 단계는 사용자의 결정이다). 사용자가 뺀 후보는 후보표에 그대로 남는다.

- [ ] **Step 4: 고른 모드를 목록에 더한다**

`tools/workshop/mods.txt`의 `2839261049` 줄(마지막 설치 줄) 뒤, `2945768219` 줄 앞에 고른 모드를 `설치` 줄로 더한다. 원문 이름은 서식 태그를 뺀 제목, 한국어 이름은 짓는다, 모드ID `-`, 비고에 필수 항목(`<ID> 필요`)·크기(100MB 넘으면)·표시. 필수 항목이 목록에 없으면 그것도 `설치` 줄로 더하고 그 앞 줄에 둔다. 종류는 후보표의 종류.

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Status`
Expected: 새 줄이 `<ID> | 설치 | <종류> | <한국어 이름> | - | - | - | -`로 나오고 마지막 줄 `받음 15/<15+n>, 설치 15/<15+n> | 보류 1 | 제외 1`.

- [ ] **Step 5: 커밋**

```bash
git -C E:\TlToybox add tools/workshop/mods.txt
git -C E:\TlToybox commit -m "feat: 창작마당 목록에 사용자가 고른 모드 n개를 설치 줄로 더함

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 5: 받기·DLL 거르기·고정본 회차 검증·모드ID 채우기

**Files:**
- Modify: `tools/workshop/mods.txt` (비고의 DLL, 모드ID, 필요하면 상태를 `제외`로)
- Modify(도구가 씀): `tools/workshop/history.txt`
- Create(git 제외): `.devremote/untranslated-20261016.txt`

**Interfaces:**
- Consumes: Task 4의 새 `설치` 줄. 도구 `-Download`·`-Status`·`-Install`, 회차 스크립트, 개발 명령 `wsmods`·`rloc dump`, 검사 `ui-check.ps1 -Part base`·`tabs`.
- Produces: 받은 폴더, 모드ID가 채워진 목록, 한글화(K)의 입력 파일.

- [ ] **Step 1: 받는다**

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Download`
Expected: `pass 1: n개`, 줄마다 `OK   <ID> <한국어 이름>`, 끝에 `받음 <15+n>/<15+n>`. `FAIL`이 남으면 세 번까지 스스로 다시 시도한다. `로그인이 풀려 있습니다`가 나오면 사용자에게 `E:\SteamCmd\steamcmd.exe +login deepestlight +quit`을 한 번 쳐 달라고 한 뒤 다시 돌린다. `history.txt`에 `받음` 줄이 n개 붙는다.

- [ ] **Step 2: DLL을 거른다**

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Status`
Expected: 새 줄의 DLL 열이 수로 나온다. DLL이 1 이상인 ID마다:

```powershell
Get-ChildItem -LiteralPath "E:\SteamCmd\steamapps\workshop\content\1468810\<ID>" -Recurse -File -Filter *.dll | ForEach-Object Name
```

이름을 그 줄의 비고에 `DLL <수>(<이름들>)`로 적는다. 이름에 `MySql`·`Socket`·`Http`·`Web`·`Net`이 들어 있으면 상태를 `제외`로 바꾸고 비고에 사유(`<이름>이 들어 있어 제외(2026-10-16)`)를 적고 사용자에게 알린다(사용자가 그래도 넣겠다고 하면 `설치`로 되돌린다).

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Install`
Expected: `PLAN ONLY`. 새 줄이 `<ID> | <이름> | 평평함 또는 debug | <MB> | DLL … | 새로 설치`. `건너뜀: debug 안에 Mod_* 폴더가 2개`가 나오면 비고에 적고 사용자에게 어느 것을 넣을지 묻는다(둘 다면 그 ID는 보류).

- [ ] **Step 3: 사용자의 게임이 꺼져 있는지 보고 회차를 연다**

Run: `Get-Process guigubahuang -ErrorAction SilentlyContinue`
Expected: 아무것도 나오지 않는다. 나오면 사용자에게 게임을 꺼 달라고 알리고 기다린다(임의로 끄지 않는다).

Run: `pwsh E:\TlToybox\tools\DevRemote\session.ps1 -Begin ws-1017a -Fixture base`
Expected: 끝에 `WORLD READY`.

- [ ] **Step 4: 회차 안에서 새 모드를 넣고 재시작한다**

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Install -Yes -NoHistory`
Expected: 기존 15개는 `UNCHANGED`, 새 모드는 `INSTALLED <ID> <이름>`, `게임이 실행 중입니다. 새로 넣은 모드는 다음 게임 시작부터 읽힙니다.`, `설치 n개`. `history.txt`는 그대로다(`git -C E:\TlToybox diff --stat tools/workshop/history.txt`가 비어 있다).

Run: `pwsh E:\TlToybox\tools\DevRemote\session.ps1 -Restart`
Expected: `WORLD READY`.

- [ ] **Step 5: 로드를 확인한다**

Run: `pwsh E:\TlToybox\tools\DevRemote\mod.ps1 -Cmd "wsmods"`
Expected: `loaded=<16+n>`, `mods`에 새 모드의 `모드ID:이름:판:창작마당 번호`가 있다. 새 모드마다 모드ID를 적어 둔다. 없는 것은 `Player.log`(`%USERPROFILE%\AppData\LocalLow\guigugame\guigubahuang\Player.log`)의 `加载模组` 줄에서 그 폴더가 읽혔는지 본다.

Run: `pwsh E:\TlToybox\tools\research\ui-check.ps1 -Part base` 그리고 `-Part tabs`
Expected: 둘 다 `FAILS=0`. 실패하면 `MelonLoader\Latest.log`의 예외가 편집기 것인지 새 모드 것인지 가려 적는다(편집기 것이면 고치고, 모드 것이면 05 문서 45절에 적는다).

- [ ] **Step 6: 새 글을 뽑아 둔다(한글화의 입력)**

Run: `pwsh E:\TlToybox\tools\DevRemote\mod.ps1 -Cmd "rloc"` 그리고 `-Cmd "rloc dump"`
Expected: `rloc`가 `file=… rows=59 applied=59 fallback=<새 모드 글 수> …`, `rloc dump`가 `written=<n>`.

Run:
```powershell
Copy-Item -LiteralPath "$env:USERPROFILE\AppData\LocalLow\guigugame\guigubahuang\TlToybox\translations\untranslated.txt" -Destination "E:\TlToybox\.devremote\untranslated-20261016.txt"
```
Expected: 파일이 생기고 줄 수가 `written`과 같다(`(Get-Content E:\TlToybox\.devremote\untranslated-20261016.txt | Measure-Object -Line).Lines`). git 제외 폴더다.

Run: `Select-String -Path "E:\Games\Tale of Immortal\MelonLoader\Latest.log" -Pattern 'Exception' | Select-Object -Last 20`
Expected: 새 예외가 있으면 어느 모드 것인지 적어 둔다(편집기 `MOD_sWP6UL`의 것은 없어야 한다).

- [ ] **Step 7: 회차를 닫는다**

Run: `pwsh E:\TlToybox\tools\DevRemote\session.ps1 -End ws-1017a`
Expected: `SAVE RESTORED AND IDENTICAL`, `WORKSHOP FOLDERS REMOVED: <n>`, `TRANSLATIONS RESTORED`. `E:\Games\Tale of Immortal\ModExportData\`에 `Mod_sWP6UL`과 기존 15개만 남는다.

- [ ] **Step 8: 모드ID를 채우고 시험을 다시 돌린다**

`tools/workshop/mods.txt`의 새 줄마다 모드ID 칸에 Step 5의 값을 적는다.

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Status` → 새 줄이 `받음`에 크기와 날짜, `설치 -`.
Run: `pwsh E:\TlToybox\tools\workshop.tests.ps1` → `FAILS=0`.
Run: `pwsh E:\TlToybox\tools\DevRemote\session.tests.ps1` → 마지막 줄 `FAILS=0`.

- [ ] **Step 9: 커밋**

```bash
git -C E:\TlToybox add tools/workshop/mods.txt tools/workshop/history.txt
git -C E:\TlToybox commit -m "feat: 새 모드 n개 받음(이력), DLL 이름과 모드ID 기록, 고정본 회차 ws-1017a 로드 확인

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 6: 실제 설치와 Steam 컬렉션 (컨트롤러가 직접. 브라우저와 사용자 확인이 든다)

**Files:**
- Create: `tools/workshop/collection.txt`
- Modify(도구가 씀): `tools/workshop/history.txt`, `docs/09-workshop-mods.md`, `tools/workshop/collection-desc.txt`

**Interfaces:**
- Consumes: Task 5의 받은 폴더와 목록, Task 3의 `-Doc`·`collection-desc.txt`, Claude in Chrome 도구(`mcp__claude-in-chrome__*`).
- Produces: 게임에 설치된 새 모드, 컬렉션 ID, 등록부 4절 `일치 n`.

- [ ] **Step 1: 사용자 확인을 받고 설치한다**

사용자에게 설치할 목록(새 줄의 ID·이름·DLL)을 보이고 "게임을 끈 상태에서 설치해도 되는지"를 묻는다. 예를 받은 뒤:

Run: `Get-Process guigubahuang -ErrorAction SilentlyContinue` → 아무것도 없어야 한다.
Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Install -Yes`
Expected: 기존 15개 `UNCHANGED`, 새 모드 `INSTALLED`, `설치 n개`. `history.txt`에 `설치` 줄 n개.
Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Status` → `받음 <15+n>/<15+n>, 설치 <15+n>/<15+n> | 보류 1 | 제외 <x>`.
Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Doc` → 1절이 새 수를 보인다.

- [ ] **Step 2: 커밋**

```bash
git -C E:\TlToybox add tools/workshop/history.txt docs/09-workshop-mods.md tools/workshop/collection-desc.txt
git -C E:\TlToybox commit -m "feat: 새 모드 n개를 게임에 설치(사용자 확인 2026-10-16), 등록부 갱신

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

- [ ] **Step 3: 사용자가 Chrome에서 Steam에 로그인한다**

사용자에게 "Chrome에서 https://steamcommunity.com 에 `deepestlight`로 로그인해 두세요(비밀번호·Steam Guard는 제가 받지 않습니다)"라고 말하고 기다린다. 로그인됐다고 하면 Claude in Chrome 도구를 한 번에 올린다:

ToolSearch: `select:mcp__claude-in-chrome__tabs_context_mcp,mcp__claude-in-chrome__tabs_create_mcp,mcp__claude-in-chrome__navigate,mcp__claude-in-chrome__read_page,mcp__claude-in-chrome__find,mcp__claude-in-chrome__computer,mcp__claude-in-chrome__form_input,mcp__claude-in-chrome__get_page_text`

새 탭을 만들어(`tabs_create_mcp`) `https://steamcommunity.com/sharedfiles/managecollection/?appid=1468810`으로 간다. `read_page`로 제목·설명 칸과 가시성 선택이 보이면 로그인된 것이다. 로그인 페이지가 보이면 사용자에게 다시 알리고 기다린다.

- [ ] **Step 4: 컬렉션을 만든다**

`form_input`으로 제목 `귀곡팔황 TlToybox 설치 모드`, 설명은 `tools/workshop/collection-desc.txt`의 내용, 가시성은 "비공개 링크(Unlisted)"에 해당하는 선택지(페이지의 표기대로. 중국어 UI면 `仅限链接` 또는 비슷한 것. 없으면 `read_page`로 선택지를 적어 사용자에게 묻는다). 저장 단추를 누르기 전에 사용자에게 "이 내용으로 컬렉션을 만들까요?"라고 묻고, 예를 받은 뒤 누른다. 만들어진 페이지의 주소 `…/filedetails/?id=<컬렉션ID>`에서 ID를 읽는다.

`tools/workshop/collection.txt`:

```text
# Steam 창작마당 컬렉션 ID(2026-10-16, 비공개 링크). 도구 -Doc이 설치 줄과 비교한다.
<컬렉션ID>
```

- [ ] **Step 5: 항목을 넣는다**

`mods.txt`의 `설치` 줄 ID를 순서대로, 항목 페이지 `https://steamcommunity.com/sharedfiles/filedetails/?id=<ID>`에서 "컬렉션에 추가"(`添加到收藏集` 또는 비슷한 표기) 단추 → 컬렉션 선택 → 확인. 확인은 제출이므로 사용자에게 묻는다. 사용자가 "설치 줄 전부를 넣어도 된다"고 한 번에 허락하면 그 묶음은 이어서 하고, 끝나면 넣은 수를 보고한다. 컬렉션 편집 페이지에 ID로 여러 개를 한 번에 넣는 화면이 있으면 그것을 쓴다(저장 한 번).

Run: `pwsh E:\TlToybox\tools\workshop.ps1 -Doc`
Expected: 4절 `- 일치 <15+n>`, `- 컬렉션에만: 없음`, `- 설치 줄에만: 없음`. `조회 실패: result=9`나 `HTTP …`가 나오면 비공개 링크 컬렉션을 API가 돌려주지 않는 것이다(스펙 8절). 사용자에게 공개로 바꿀지 묻고, 바꾸지 않으면 4절은 그대로 두고 05 문서 45절에 "비공개 링크는 API로 읽히지 않음(실측)"으로 적는다.

- [ ] **Step 6: 커밋**

```bash
git -C E:\TlToybox add tools/workshop/collection.txt docs/09-workshop-mods.md
git -C E:\TlToybox commit -m "feat: Steam 컬렉션 <컬렉션ID>(비공개 링크) 만들고 설치 줄 <수>개를 넣음, 등록부 4절 일치

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 7: 문서·검토·병합

**Files:**
- Modify: `docs/05-editor-v0.1.md` (45절 추가)
- Modify: `docs/06-handoff.md` (1·3·4·5·8절)
- Modify: `docs/02a-workshop-survey.md` (9절 각주)

- [ ] **Step 1: 05 문서 45절을 쓴다**

`docs/05-editor-v0.1.md` 끝에 다음을 더한다. `<…>`는 Task 2·5·6의 실제 출력으로 채운다(수치를 모르면 "미확인"과 사유).

```markdown
## 45. 창작마당 모드 확장(W): 검색·등록부·컬렉션 (2026-10-16 추가)

스펙 `docs/superpowers/specs/2026-10-16-workshop-expand-design.md`, 계획 `plans/2026-10-16-workshop-expand.md`. 사용자 요청(2026-10-16) 2·3·5. 리뷰(1)는 `docs/08-review-2026-10-16.md`, 한글화(4)는 K로 따로.

### 규칙

- 목록 `tools/workshop/mods.txt`는 일곱 칸 `창작마당ID | 상태 | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 비고`. 상태 `설치`·`보류`·`제외`(다뤄 본 ID는 모두 줄로 남긴다), 종류 `틀`·`외형`·`공법`·`세계관`·`음악`. `-Download`·`-Install`은 `설치` 줄만(`-Id`로 `보류`는 받되 `제외`는 거부), `-Uninstall`은 모든 줄. 모르는 상태·종류·빈 이름은 시작 전에 거부.
- 이력 `tools/workshop/history.txt`: `yyyy-MM-dd HH:mm | 받음·설치·갱신·지움 | ID | 한국어 이름`. 도구가 성공한 동작마다 덧붙인다. `-NoHistory`(회차 안·시험)면 쓰지 않는다. 2026-10-15·16의 받음 16·설치 15는 소급(시각 `--:--`).
- `-Search [-Kind 외형,공법,세계관,음악] [-Top 20] [-Delay 1.5]`: 검색 페이지(구독자순, 태그 `Beautify`/`Battle`+`功法`/`Drama`·`NPC`·`LargeIntegrated`/검색어 `音乐`·`BGM`·`特效`) → 상세 API(30개씩. 차단·비공개 제외) → 항목 페이지의 `RequiredItems` 블록. 표시 `성인향?`·`오래됨`(2023년 이전)·`큼`(200MB)·`시스템?`(공법)·`대형`·`목록에 있음(상태)`. HTTP 429나 "请求太多"면 멈추고 모은 것을 쓴다. 결과 `tools/workshop/candidates-<날짜>.md`(git)와 `-desc.txt`(git 제외). `-WebRoot`로 저장 응답을 읽는 시험.
- `-Doc`: `docs/09-workshop-mods.md`(요약·모드 표·이력·컬렉션 비교·후보표·한글화)와 `tools/workshop/collection-desc.txt`를 통째로 다시 쓴다. 손으로 고치지 않는다. 컬렉션 ID는 `tools/workshop/collection.txt`.
- 선정: 도구의 후보표에 사람이 한국어 요약·추천을 채우고 사용자가 고른다. 뺀 후보는 후보표에 남는다.
- 컬렉션은 사용자가 로그인한 Chrome에서 컨트롤러가 만들고 항목을 넣는다(쓰기 API 없음). 가시성 비공개 링크.

### 실측

- Steam 접근(2026-10-16, 읽기만): 검색 페이지 한 쪽 30개, 게임 고유 태그 열 개(`Luck`·`Drama`·`NPC`·`Battle`·`Function`·`LargeIntegrated`·`Item`·`Beautify`·`OpenSource`·`English`), 상세 API 키 없이, 항목 페이지의 `id="RequiredItems"` 블록(02a 조사표 9절의 미확인을 풂), 컬렉션 조회 API 키 없이, 컬렉션 만들기 페이지는 로그인 없이는 오류.
- 실제 `-Search`(<날짜 시각>): 종류마다 후보 <수>, 요청 <수>회, <분>분, 요청 제한 <걸림/안 걸림>.
- 사용자 선정: <n>개(<ID와 이름>). 필수 항목으로 함께 넣은 것 <있음/없음>. DLL이 든 것 <ID와 DLL 이름>. 제외로 돌린 것 <없음/ID와 사유>.
- 회차 `ws-1017a`(고정본): `-Install -Yes -NoHistory` → `-Restart` → `wsmods loaded=<수>`, 새 모드ID <표>, `base`·`tabs` <FAILS>, `rloc dump written=<수>`, `Latest.log`의 새 예외 <없음/모드 이름>, `-End`가 `SAVE RESTORED AND IDENTICAL`·`WORKSHOP FOLDERS REMOVED: <n>`.
- 실제 설치(<날짜 시각>, 사용자 확인, 게임 꺼짐): `INSTALLED` <n>, `-Status` 받음 <a>/<a>·설치 <a>/<a>.
- 컬렉션 <ID>(비공개 링크): 항목 <수>. `-Doc` 4절 <일치 n / 조회 실패와 사유>.

### 확인하지 못한 것, 한계

- 새 모드의 실제 효과(새 종문·NPC·음악)는 로드까지만 봤다. 실제 플레이의 이상은 사용자 확인 항목(인계 8.5절).
- 호환성은 갱신일로만 가늠했다(댓글을 읽지 않았다).
- <비공개 링크 컬렉션의 API 조회 결과>.
- `robocopy /MIR` 갱신이 모드가 쓴 파일을 지울 가능성(41절부터의 미확인).
- 회차 이름: `ws-1017a`.
```

- [ ] **Step 2: 인계 문서를 고친다**

`docs/06-handoff.md`:
- 1절 "지금 게임에 설치된 것"의 창작마당 모드 줄: 수를 `15 + n`으로, "목록 `tools/workshop/mods.txt`(일곱 칸, 상태), 등록부 `docs/09-workshop-mods.md`, 컬렉션 <ID>"를 더한다. "직전 작업"에 W(45절)를 더하고, "다음에 할 일"을 "(1) 한글화 K: `.devremote\untranslated-20261016.txt`(<n>행)를 번역해 43절 절차로 합친다 (2) 기타 X: `docs/08-review-2026-10-16.md`(리뷰 세션)와 8.3a·8.5·8.6절에서 고를 것을 정한다 (3) 사용자 확인(8.5절)"로 바꾼다.
- 3절 표에 `tools/workshop/`(`mods.txt`·`history.txt`·`candidates-*.md`·`collection.txt`·`collection-desc.txt`)와 `docs/09-workshop-mods.md`(생성본) 줄을 더한다.
- 4절의 `workshop.ps1` 줄을 `-Status | -Download | -Install [-Yes] | -Uninstall [-Yes] | -Search [-Kind …] | -Doc`으로 바꾸고 "회차 안에서 설치할 때는 `-NoHistory`", "목록·이력·설치가 바뀌면 `-Doc`을 돌려 같이 커밋한다"를 더한다.
- 5절 "회차 안에서 쓰는 도구"의 창작마당 줄에 `-NoHistory` 규칙을 더한다.
- 8.1절 표에 `W | 창작마당 확장: 검색·등록부·컬렉션 (2026-10-16 요청 2·3·5) | 45절. 스펙·계획 2026-10-16-workshop-expand*`를 더한다. 8.5절에 "새 모드 <n>개를 설치한 뒤 플레이했을 때의 이상 여부, 새 종문·NPC·음악이 실제로 보이는지"를 더한다. 8.7절의 회차 이름에 `ws-1017a`를 더한다.

- [ ] **Step 3: 02a 조사표에 각주를 단다**

`docs/02a-workshop-survey.md` 9절의 "창작마당 페이지의 "필수 아이템"(의존성) 표시" 행 끝에 ` (2026-10-16 정정: 항목 페이지의 `<div … id="RequiredItems">` 블록 안의 `filedetails/?id=` 링크와 `requiredItem` div로 읽힌다. 05 문서 45절, `tools/workshop.ps1 -Search`)`를 더한다.

- [ ] **Step 4: 커밋**

```bash
git -C E:\TlToybox add docs/05-editor-v0.1.md docs/06-handoff.md docs/02a-workshop-survey.md
git -C E:\TlToybox commit -m "docs: 05 문서 45절(창작마당 확장 W), 인계 1·3·4·5·8절, 02a 필수 아이템 정정

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

- [ ] **Step 5: 검토자 한 명이 브랜치 전체를 본다**

`superpowers:requesting-code-review`로 `git -C E:\TlToybox diff develop...feat/workshop-expand`를 검토시킨다(읽기 전용. 범위: 도구·시험·목록·문서. 지적마다 파일:줄). 고칠 것은 고치고 `pwsh E:\TlToybox\tools\workshop.tests.ps1`·`pwsh E:\TlToybox\tools\DevRemote\session.tests.ps1`을 다시 돌려 `FAILS=0`을 본 뒤 커밋한다(`fix: 검토 반영 — …`). 사소하다고 본 것은 인계 8.6절에 적는다.

- [ ] **Step 6: 병합한다**

```bash
git -C E:\TlToybox checkout develop
git -C E:\TlToybox merge --no-ff feat/workshop-expand -m "Merge branch 'feat/workshop-expand' into develop"
git -C E:\TlToybox log --oneline -3
```

Expected: 병합 커밋이 맨 위. 작업 트리 깨끗. 브랜치를 지울지는 사용자에게 묻는다(지우지 않는다. 사용자 지시 뒤에만).
