# GGBH Cheat Sandbox 사전 조사 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 귀곡팔황 통합 치트·샌드박스 모드의 v0.1 이전 단계로, 게임 구조·모드 개발 구성·인게임 상태를 실측 문서로 남기고 빌드 가능한 빈 모드 프로젝트를 만든다.

**Architecture:** 조사 문서 4종은 `docs/`에, 빈 프로젝트는 `src/`에 둔다. 게임 설치 폴더는 읽기만 하고, 분석에 쓰는 일회성 스크립트와 추출물은 스크래치패드에만 둔다. 작업 순서는 게임 구조 → 창작마당 조사 → 개발 구성 리서치와 경로 판정 → 빈 프로젝트 → 상태 분석으로 고정한다.

**Tech Stack:** PowerShell 7, git(로컬 전용), .NET SDK 8.0.425(`net472` 대상 빌드), Mono.Cecil(게임에 포함된 `MelonLoader\Mono.Cecil.dll`을 읽기 전용으로 로드), `hh.exe`(chm 추출), 웹 브라우저 도구

**Spec:** `docs/superpowers/specs/2026-10-12-pre-research-design.md`

## Global Constraints

- `E:\Games\Tale of Immortal`은 전 과정에서 읽기 전용이다. 파일을 만들거나 고치거나 지우지 않는다.
- 게임을 실행하지 않는다. 세이브 파일을 수정하지 않는다.
- 문서의 모든 사실 기재는 실측 또는 출처가 있어야 한다. 확인하지 못한 항목은 "미확인"과 사유를 적는다.
- 클래스명·필드명·메서드명은 어셈블리 또는 API 문서에서 실제로 확인한 것만 적는다.
- 다른 모드의 소스 코드는 구조와 접근 방식만 참고하고 복사하지 않는다.
- 문서는 한국어 마크다운이다. 코드 식별자, 중국어 원문 명칭, 파일 경로는 원문 그대로 둔다.
- git은 로컬 전용이다. 원격을 추가하지 않는다. `main`·`develop`에 직접 커밋하지 않는다. 작업 브랜치는 merge commit(`--no-ff`)으로 `develop`에 병합한다. `main`에는 병합하지 않는다.
- 게임 DLL을 저장소에 복사하지 않는다.
- 일회성 스크립트와 추출물은 스크래치패드(`$env:TEMP\claude\E--TlToybox\...\scratchpad` — 실행 세션이 알려 주는 경로)에만 둔다. 아래에서는 `$S`로 표기한다.
- 기능 구현, 런타임 검증, 게임 폴더 배포, 창작마당 모드 구독은 범위 밖이다.
- 모든 git/파일 명령은 절대경로 또는 `git -C E:\TlToybox`를 쓴다.
- 서브에이전트에 위임할 때는 해당 Task의 본문을 지시문에 직접 넣고, "이 작업(Task N)만 수행하라. 끝나면 보고하고 정지하라. 다른 Task로 진행하지 말라. 명세에 없는 코드를 추측·즉흥 구현하지 말라. 명세가 부족하면 멈추고 `NEEDS_CONTEXT`로 보고하라."를 포함한다.

## Review Focus

스펙이 암시하지만 놓치기 쉬운 조건들이다. 각 항목은 담당 Task의 검증 단계에 반영되어 있다.

1. **설치본이 Steam 라이브러리 경로가 아니다**(`E:\Games\...`). `appmanifest_1468810.acf`와 창작마당 구독 폴더가 없을 수 있다. → 없으면 빌드 ID·DLC·구독 모드를 "미확인"으로 적고 찾아본 경로를 사유로 남긴다. (Task 1)
2. **창작마당 페이지가 로그인·지역 제한·삭제로 열리지 않는다.** → 해당 행의 검증 상태를 "접근 불가"로 적고, 검색 결과 스니펫만으로 기능을 확정하지 않는다. (Task 2)
3. **기준 문서의 Workshop ID가 다른 모드를 가리킨다.** → "불일치"로 적고 페이지의 실제 제목을 함께 기록한다. (Task 2)
4. **`net472` 참조 어셈블리가 없어 `dotnet build`가 실패한다.** → `Microsoft.NETFramework.ReferenceAssemblies` 패키지로 해결한다. 패키지 복원이 막히면 멈추고 보고한다. (Task 4)
5. **프록시 어셈블리의 이름과 `GGBH_API.chm`의 이름이 서로 다르다**(게임 버전 차이). → 두 출처를 따로 적고 어셈블리 쪽을 실측값으로 삼는다. (Task 5)

## File Structure

| 경로 | 책임 | Task |
|---|---|---|
| `docs/00-design-brief.md` | 사용자 제공 기획서 원본 복사 | 1 |
| `docs/01-game-structure.md` | 설치본의 실측 구조 | 1 |
| `docs/02a-workshop-survey.md` | 창작마당·모드 사이트 조사표 | 2 |
| `docs/02-mod-dev-research.md` | 공식 ModMain 경로의 절차·한계와 경로 판정 | 3 |
| `CLAUDE.md`, `README.md` | 저장소 규칙과 안내 | 4 |
| `src/ModCode/ModMain/ModMain.csproj` | 빈 모드 프로젝트 정의 | 4 |
| `src/ModCode/ModMain/ModMain.cs` | 진입점 클래스(로그 한 줄) | 4 |
| `src/ModCode/ModMain/{Core,Adapters,Resolvers,Services,Features,UI,Safety}/.gitkeep` | 기준 문서 11장의 빈 폴더 | 4 |
| `src/ModExcel/.gitkeep`, `src/ModAssets/.gitkeep`, `tests/.gitkeep` | 빈 폴더 | 4 |
| `docs/04-ingame-state-analysis.md` | 영역별 상태 소유 클래스·필드 조사표 | 5 |

공통 변수(각 Task의 명령에서 사용):

```powershell
$R = 'E:\TlToybox'
$G = 'E:\Games\Tale of Immortal'
$S = '<실행 세션의 스크래치패드 절대경로>'
```

---

### Task 1: 게임 구조 확인

**Files:**
- Create: `docs/00-design-brief.md`
- Create: `docs/01-game-structure.md`

**Interfaces:**
- Consumes: 없음
- Produces: `docs/01-game-structure.md`의 "모드 관련 경로" 절 — Task 3·5가 설정 테이블과 공식 자료의 위치를 여기서 읽는다.

브랜치: `docs/pre-research`

- [ ] **Step 1: 브랜치 확인과 기획서 복사**

```powershell
git -C $R switch docs/pre-research
Copy-Item -LiteralPath 'D:\Download\귀곡팔황_통합치트_모드설계_및_창작마당조사.md' -Destination "$R\docs\00-design-brief.md"
```

원본 파일명이 URL 인코딩된 형태(`%EA%B7%80...md`)로 저장되어 있으면 그 이름을 그대로 `-LiteralPath`에 넣는다. 복사 후 `Get-FileHash`로 원본과 해시가 같은지 확인한다.

- [ ] **Step 2: 최상위 구조와 크기 수집**

```powershell
Get-ChildItem -Force $G | Select-Object Mode,Length,LastWriteTime,Name
Get-ChildItem $G -Directory | ForEach-Object {
  $s = (Get-ChildItem $_.FullName -Recurse -File -ErrorAction SilentlyContinue | Measure-Object Length -Sum)
  '{0,-40} {1,8} files {2,10:N1} MB' -f $_.Name, $s.Count, ($s.Sum/1MB)
}
```

- [ ] **Step 3: 엔진·로더 정보 수집**

```powershell
Get-Content "$G\MelonLoader\Latest.log"
Get-Content "$G\guigubahuang_Data\boot.config"
Get-Content "$G\guigubahuang_Data\app.info"
Get-Content "$G\guigubahuang_Data\ScriptingAssemblies.json"
(Get-Item "$G\guigubahuang.exe","$G\UnityPlayer.dll","$G\GameAssembly.dll","$G\MelonLoader\MelonLoader.dll","$G\Mods\GGBH_MOD.dll","$G\version.dll").VersionInfo |
  Select-Object FileName,FileVersion,ProductVersion,ProductName
Get-ChildItem "$G\MelonLoader\Logs" | Select-Object Name,Length,LastWriteTime
```

로그에서 로드된 Mod·Plugin 목록과 `GGBH_MOD` 관련 줄을 찾아 기록한다.

- [ ] **Step 4: Steam 빌드 ID·DLC·창작마당 구독 폴더 확인**

```powershell
$steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
$steam
if ($steam) {
  Get-Content "$steam\steamapps\libraryfolders.vdf"
  Get-ChildItem "$steam\steamapps" -Filter 'appmanifest_1468810.acf' -ErrorAction SilentlyContinue
}
Get-ChildItem $G -Force -Filter 'steam_*' -Recurse -Depth 2 -ErrorAction SilentlyContinue | Select-Object FullName,Length
```

`libraryfolders.vdf`에 나온 각 라이브러리 경로에 대해 `steamapps\appmanifest_1468810.acf`와 `steamapps\workshop\content\1468810`의 존재를 확인한다. 매니페스트가 있으면 `buildid`, `InstalledDepots`, `LastUpdated`를 기록한다. 어디에도 없으면 "미확인"으로 적고 찾아본 경로를 나열한다(Review Focus 1).

- [ ] **Step 5: 모드 관련 경로와 공식 자료 목록 수집**

```powershell
foreach ($d in 'Mod','Mods','Plugins','ModExportData','UserData','UserLibs') {
  "=== $d ==="
  Get-ChildItem "$G\$d" -Recurse -Depth 4 -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(obj|bin|Library|\.vs|Packages)\\' } |
    ForEach-Object { $_.FullName.Substring($G.Length) + $(if ($_.PSIsContainer) {'\'} else {"  ($($_.Length))"}) }
}
```

`Mod\modFQA` 아래의 docx·chm·txt·예제 프로젝트마다 용도를 한 줄로 적는다. `说明.txt` 같은 텍스트 파일은 읽어서 요지를 적는다.

- [ ] **Step 6: 세이브·설정 파일 위치 확인**

```powershell
$ll = "$env:USERPROFILE\AppData\LocalLow"
Get-ChildItem $ll -Directory | Where-Object Name -match 'guigu|GuiGu|鬼谷'
Get-ChildItem $ll -Directory -Recurse -Depth 1 | Where-Object Name -match 'guigubahuang|鬼谷八荒'
```

찾은 폴더의 하위 구조를 깊이 3까지 나열한다(파일 내용은 열지 않는다). `app.info`의 회사명·제품명(`guigugame` / `guigubahuang`)이 경로와 일치하는지 확인한다. 찾지 못하면 "미확인"으로 적는다.

- [ ] **Step 7: 데이터 자산 구성 수집**

```powershell
Get-ChildItem "$G\guigubahuang_Data" -Recurse -Depth 2 |
  ForEach-Object { $_.FullName.Substring($G.Length) + $(if ($_.PSIsContainer) {'\'} else {"  ($($_.Length))"}) } |
  Select-Object -First 200
Get-Content "$G\guigubahuang_Data\RuntimeInitializeOnLoads.json"
```

- [ ] **Step 8: `docs/01-game-structure.md` 작성**

아래 절 구성을 그대로 쓰고, 각 절을 Step 2~7의 실측값으로 채운다.

```markdown
# 01. 게임 구조 확인

> 조사일: 2026-10-12 · 대상: `E:\Games\Tale of Immortal` · 방법: 읽기 전용 조사

## 1. 요약
## 2. 버전 정보
| 항목 | 값 | 근거 |
## 3. 최상위 폴더·파일 역할
| 경로 | 역할 | 크기 |
## 4. 엔진과 스크립팅 백엔드
## 5. MelonLoader 구성
## 6. 설치된 모드·플러그인·DLC
## 7. 모드 관련 경로
## 8. 공식 modFQA 자료 목록
| 경로 | 종류 | 용도 |
## 9. 세이브·설정 파일 위치
## 10. 데이터 자산 구성
## 11. 미확인 항목
| 항목 | 사유 | 확인 방법 |
```

"2. 버전 정보"에는 게임 빌드, Unity, MelonLoader, Cpp2IL, Il2CppAssemblyUnhollower, `GGBH_MOD.dll`의 버전을 한 행씩 적는다.

- [ ] **Step 9: 검증**

```powershell
$f = "$R\docs\01-game-structure.md"
1..11 | ForEach-Object { if (-not (Select-String -Path $f -Pattern "^## $_\. " -Quiet)) { "MISSING section $_" } }
Select-String -Path $f -Pattern 'TBD|TODO|추정|아마|것으로 보인다'
git -C $R status --short
```

Expected: `MISSING` 출력 없음. 두 번째 명령에 걸린 줄이 있으면 실측값으로 고치거나 "11. 미확인 항목"으로 옮긴다. `git status`에는 `docs/` 아래 새 파일 두 개만 나온다.

- [ ] **Step 10: Commit**

```powershell
git -C $R add docs/00-design-brief.md docs/01-game-structure.md
git -C $R commit -m "docs: 게임 구조 확인 문서 추가" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 창작마당·모드 사이트 조사

**Files:**
- Create: `docs/02a-workshop-survey.md`

**Interfaces:**
- Consumes: `docs/00-design-brief.md`의 10장 후보 표와 10.1 검색 키워드
- Produces: `docs/02a-workshop-survey.md` — Task 3이 "창작마당 모드들이 실제로 쓰는 방식"의 근거로 인용한다.

브랜치: `docs/pre-research`

- [ ] **Step 1: 기준 문서 후보 10종 재검증**

다음 페이지를 브라우저 도구로 하나씩 열어 제목, 작성자, 최종 갱신일, 설명 요지, "필수 아이템"(의존성), 최근 댓글의 오류 보고를 기록한다.

```text
https://steamcommunity.com/sharedfiles/filedetails/?id=2845764367   (기준 문서: 成长型功法体系)
https://steamcommunity.com/sharedfiles/filedetails/?id=2827641660   (기준 문서: 功法升级与合成)
https://steamcommunity.com/sharedfiles/filedetails/?id=3341173894   (기준 문서: 功法升级与合成无限制补丁)
https://steamcommunity.com/sharedfiles/filedetails/?id=3489005072   (기준 문서: 功法参悟不回溯)
https://steamcommunity.com/sharedfiles/filedetails/?id=3513802055   (기준 문서: 功法修炼度提高 관련 컬렉션)
https://steamcommunity.com/sharedfiles/filedetails/?id=2936146595   (기준 문서: 综合商店、宗门。Npc--Mod)
```

ID가 없는 나머지 4종은 창작마당 검색으로 찾는다.

```text
https://steamcommunity.com/workshop/browse/?appid=1468810&searchtext=神通+心法+经验
https://steamcommunity.com/workshop/browse/?appid=1468810&searchtext=功法效果优化
https://steamcommunity.com/workshop/browse/?appid=1468810&searchtext=万物生灵
https://steamcommunity.com/workshop/browse/?appid=1468810&searchtext=九阴真经
```

쿠키 동의 배너가 나오면 비필수 항목을 거부한다. 로그인이나 구독은 하지 않는다.

판정 규칙: 페이지 제목이 기준 문서의 이름과 같으면 "확인", 다른 모드면 "불일치"(실제 제목 기록), 열리지 않으면 "접근 불가"(Review Focus 2·3).

- [ ] **Step 2: 키워드 검색으로 추가 모드 수집**

`https://steamcommunity.com/workshop/browse/?appid=1468810&searchtext=<키워드>&browsesort=trend` 형식으로 다음 키워드를 검색하고, 키워드마다 상위 결과 중 이 프로젝트의 기능과 관련된 모드를 최대 5개씩 기록한다.

```text
新境界  境界扩展  突破  登仙
新功法  自创功法  功法合成
新神通  新心法  新绝技
修改器  作弊  控制台  NPC编辑  物品添加
```

치트·편집기 계열(`修改器`, `作弊`, `控制台`)은 이 프로젝트와 기능이 겹치므로 설명과 스크린샷에서 UI 형태와 제공 기능 목록을 자세히 적는다.

- [ ] **Step 3: 모드 사이트와 공개 소스 조사**

웹 검색으로 다음을 찾고, 찾은 페이지를 열어 확인한다.

```text
Nexus Mods "Tale of Immortal"
3DM 鬼谷八荒 MOD
github 鬼谷八荒 mod ModMain
github guigubahuang mod harmony
鬼谷八荒 修改器 源码
```

모드 배포 사이트는 사이트별로 귀곡팔황 모드 목록 URL, 대략의 모드 수, 치트·편집 계열 대표 모드를 기록한다. GitHub 저장소는 URL, 라이선스, 최종 커밋일, 프로젝트 구조(공식 ModMain 경로인지 독립 MelonMod인지), Harmony 패치와 UI를 어떤 방식으로 쓰는지를 기록한다. 파일을 내려받지 않는다.

- [ ] **Step 4: `docs/02a-workshop-survey.md` 작성**

```markdown
# 02a. 창작마당·모드 사이트 조사표

> 조사일: 2026-10-12 · 범위: Steam 창작마당(appid 1468810), 모드 배포 사이트, 공개 소스 저장소

## 1. 요약
## 2. 기준 문서 후보 10종 재검증
| # | 기준 문서의 이름 | 페이지의 실제 제목 | ID / 링크 | 최종 갱신일 | 기능 요약 | 의존성 | 검증 상태 | 참고 가치 |
## 3. 추가 조사 모드 — 경지
## 4. 추가 조사 모드 — 공법·무공·신통
## 5. 추가 조사 모드 — 치트·편집기
## 6. 모드 배포 사이트
| 사이트 | URL | 모드 수 | 대표 모드 | 비고 |
## 7. 공개 소스 저장소
| 저장소 | 라이선스 | 최종 커밋 | 로딩 경로 | Harmony 사용 | UI 방식 |
## 8. 이 프로젝트에 주는 시사점
## 9. 접근하지 못한 항목
```

3~5절의 표는 스펙 6.2의 열(제목, ID/링크, 출처, 최종 갱신일, 기능 요약, 의존성, 검증 상태, 참고 가치)을 쓴다.

- [ ] **Step 5: 검증**

```powershell
$f = "$R\docs\02a-workshop-survey.md"
1..9 | ForEach-Object { if (-not (Select-String -Path $f -Pattern "^## $_\. " -Quiet)) { "MISSING section $_" } }
(Select-String -Path $f -Pattern '^\| *(10|[1-9]) *\|').Count
Select-String -Path $f -Pattern '확인|불일치|접근 불가' | Measure-Object | Select-Object Count
```

Expected: `MISSING` 없음. 2절에 1~10번 행이 모두 있다. 모든 모드 행에 검증 상태 값이 있다. 표의 링크 중 무작위 5개를 다시 열어 제목과 갱신일이 문서와 같은지 확인한다.

- [ ] **Step 6: Commit**

```powershell
git -C $R add docs/02a-workshop-survey.md
git -C $R commit -m "docs: 창작마당·모드 사이트 조사표 추가" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 모드 개발 구성 리서치와 경로 판정

**Files:**
- Create: `docs/02-mod-dev-research.md`

**Interfaces:**
- Consumes: `docs/01-game-structure.md`의 7·8절, `docs/02a-workshop-survey.md`의 7·8절
- Produces: `docs/02-mod-dev-research.md`의 "9. 판정"과 "3. 프로젝트 구조와 로드 규칙" — Task 4가 착수 여부와 네임스페이스·클래스·어셈블리 이름을 여기서 읽는다. "6. 설정 테이블" — Task 5가 테이블 위치와 형식을 여기서 읽는다.

브랜치: `docs/pre-research`

- [ ] **Step 1: 공식 교안 docx 3종을 스크래치패드에서 텍스트로 추출**

```powershell
$docs = @(
  "$G\Mod\modFQA\代码编写教程\代码编写教程.docx",
  "$G\Mod\modFQA\配置修改教程",
  "$G\Mod\modFQA\资源修改教程\资源修改教程.docx",
  "$G\Mod\modFQA\鬼谷八荒MOD使用指南基础版 1.0"
)
New-Item -ItemType Directory -Force "$S\fqa" | Out-Null
Get-ChildItem $docs -Recurse -Include *.docx,*.txt,*.pdf -File | ForEach-Object {
  $dst = Join-Path "$S\fqa" ($_.BaseName + $_.Extension)
  Copy-Item -LiteralPath $_.FullName -Destination $dst
  if ($_.Extension -eq '.docx') {
    $x = "$S\fqa\$($_.BaseName)"
    Expand-Archive -LiteralPath $dst -DestinationPath $x -Force
    $xml = [xml](Get-Content "$x\word\document.xml" -Raw -Encoding UTF8)
    $ns = New-Object Xml.XmlNamespaceManager $xml.NameTable
    $ns.AddNamespace('w','http://schemas.openxmlformats.org/wordprocessingml/2006/main')
    ($xml.SelectNodes('//w:p',$ns) | ForEach-Object { ($_.SelectNodes('.//w:t',$ns) | ForEach-Object InnerText) -join '' }) |
      Set-Content "$x.txt" -Encoding UTF8
  }
}
Get-ChildItem "$S\fqa" -Filter *.txt | Select-Object Name,Length
```

`Expand-Archive`가 `.docx` 확장자를 거부하면 복사본의 확장자를 `.zip`으로 바꿔서 푼다. 추출한 txt를 전부 읽는다. 교안의 스크린샷이 절차 이해에 필요하면 `word\media`의 이미지를 본다.

- [ ] **Step 2: `GGBH_API.chm` 추출**

```powershell
New-Item -ItemType Directory -Force "$S\chm" | Out-Null
Copy-Item -LiteralPath "$G\Mod\modFQA\代码编写教程\GGBH_API.chm" -Destination "$S\chm\GGBH_API.chm"
Start-Process hh.exe -ArgumentList '-decompile', "$S\chm\out", "$S\chm\GGBH_API.chm" -Wait
Get-ChildItem "$S\chm\out" -Recurse -File | Group-Object Extension | Select-Object Name,Count
```

목차 파일(`.hhc`)과 색인(`.hhk`)을 읽어 문서화된 네임스페이스·클래스 목록을 파악한다. `hh.exe`가 결과를 내지 않으면 `7z x`로 시도하고, 둘 다 안 되면 "미확인"으로 적는다.

- [ ] **Step 3: 공식 템플릿과 예제 프로젝트 분석**

```powershell
$m = "$G\Mod\modFQA\代码编写教程\ModMain"
Get-Content "$m\ModMain.cs","$m\Example\Example.cs","$m\Properties\AssemblyInfo.cs","$m\ModMain.sln","$m\ModMain.csproj.user" -Encoding UTF8
Get-ChildItem "$m\dll","$m\bin" -Recurse | Select-Object FullName,Length
Get-ChildItem "$G\Mod\modFQA\MOD模板例子" -Recurse -Depth 4 |
  ForEach-Object { $_.FullName.Substring($G.Length) + $(if ($_.PSIsContainer) {'\'} else {"  ($($_.Length))"}) }
Get-ChildItem "$G\Mod\modFQA\MOD模板例子" -Recurse -Include *.txt,*.cache,*.json -File |
  ForEach-Object { "=== $($_.FullName) ==="; Get-Content -LiteralPath $_.FullName -Encoding UTF8 }
```

확인할 것: `ModMain.csproj`의 대상 프레임워크(`v4.7.2`)와 참조 방식(`dll\` 상대 경로), `ModMain.cs`의 진입점 규약(`ModNamespace.ModMain`의 `Init()`/`Destroy()`), 네임스페이스를 모드 에디터 설정과 맞춰야 한다는 주석, `Example.cs`가 보여 주는 이벤트 구독·Harmony 패치·UI 사용 방식, `ModProject`의 폴더 구성(`ModAssets`, `ModExcel`, `ModCode` 유무), `ModData.cache`/`ModProject.cache`의 형식.

- [ ] **Step 4: 설정 테이블 형식 확인**

```powershell
$ex = "$G\Mod\modFQA\MOD模板例子\技能配置例子\ModProject\ModExcel"
Get-ChildItem $ex -Recurse | Select-Object FullName,Length
New-Item -ItemType Directory -Force "$S\xlsx" | Out-Null
$one = Get-ChildItem $ex -Filter 'BattleSkillBase.xlsx' | Select-Object -First 1
Copy-Item -LiteralPath $one.FullName -Destination "$S\xlsx\BattleSkillBase.zip"
Expand-Archive "$S\xlsx\BattleSkillBase.zip" "$S\xlsx\BattleSkillBase" -Force
Get-Content "$S\xlsx\BattleSkillBase\xl\sharedStrings.xml" -Raw -Encoding UTF8 | Select-Object -First 1
```

머리글 행의 구성(필드명 행, 타입 행, 설명 행)을 기록한다. 게임 본체의 설정 테이블이 어디에 있는지(`GameConfPack*.dll`, `StreamingAssets`, `resources.assets` 내부 등)는 교안의 설명과 `docs/01-game-structure.md` 10절을 대조해 적는다.

- [ ] **Step 5: 웹 리서치**

다음을 검색하고 찾은 페이지를 열어 확인한다. 인용할 내용마다 URL을 남긴다.

```text
鬼谷八荒 MOD 代码编写教程 ModMain Harmony
鬼谷八荒 模组编辑器 导出 创意工坊 上传
鬼谷八荒 mod UI 界面 创建 g.ui
Tale of Immortal modding guide ModMain MelonLoader
Tale of Immortal Il2Cpp unhollower Harmony patch
MelonLoader 0.5.4 Il2CppAssemblyUnhollower Harmony limitations
```

MelonLoader와 Harmony의 API·제약은 Context7로 현재 문서를 조회한다(`resolve-library-id` → `query-docs`). 확인할 질문:

- 공식 경로에서 코드 DLL이 언제, 어떤 규약으로 로드되는가
- IL2CPP 프록시 메서드에 Harmony Prefix/Postfix를 거는 방법과 알려진 제약(인라인된 메서드, 제네릭, 구조체 인자)
- UI를 주입하는 방법(게임 자체 UI 프리팹 재사용, IMGUI, 직접 생성한 uGUI)과 IL2CPP에서 커스텀 `MonoBehaviour`를 쓰기 위한 등록 절차
- 모드 에디터에서 내보내기와 창작마당 업로드 절차
- 로컬에서 개발 중인 모드를 게임에 인식시키는 방법

- [ ] **Step 6: `docs/02-mod-dev-research.md` 작성**

```markdown
# 02. 모드 개발 구성 리서치

> 조사일: 2026-10-12 · 출처: 로컬 공식 자료, 웹, 창작마당 조사(`02a-workshop-survey.md`)

## 1. 요약
## 2. 모드의 세 가지 구성 요소 (ModCode / ModExcel / ModAssets)
## 3. 프로젝트 구조와 로드 규칙
## 4. 빌드·로컬 테스트·배포 절차
## 5. Harmony 패치 가능 범위
## 6. 설정 테이블
## 7. UI 주입 방법과 한계
## 8. 독립 MelonMod 경로와의 비교
| 기준 | 공식 ModMain | 독립 MelonMod |
## 9. 판정
## 10. 미확인 항목과 v0.1에서 검증할 것
## 11. 출처
```

"3. 프로젝트 구조와 로드 규칙"에는 다음 네 값을 표로 명시한다: 요구되는 어셈블리 이름, 네임스페이스 규칙, 진입점 클래스와 메서드 시그니처, 대상 프레임워크.

"9. 판정"의 첫 줄은 다음 둘 중 하나로 쓴다.

```text
판정: 공식 ModMain 경로 적합
판정: 공식 ModMain 경로 부적합
```

그 아래에 근거를 적는다. 근거는 5·7절에서 확인된 사실만 쓴다. 정적 자료로 확정하지 못한 부분은 "적합"의 전제 조건으로 따로 나열한다.

- [ ] **Step 7: 검증**

```powershell
$f = "$R\docs\02-mod-dev-research.md"
1..11 | ForEach-Object { if (-not (Select-String -Path $f -Pattern "^## $_\. " -Quiet)) { "MISSING section $_" } }
Select-String -Path $f -Pattern '^판정: 공식 ModMain 경로 (적합|부적합)$'
(Select-String -Path $f -Pattern 'https?://').Count
git -C $R status --short
```

Expected: `MISSING` 없음, 판정 줄이 정확히 한 번 나옴, 출처 URL이 1개 이상, 스크래치패드 추출물이 저장소에 들어오지 않음(`docs/02-mod-dev-research.md`만 표시).

- [ ] **Step 8: Commit과 develop 병합**

```powershell
git -C $R add docs/02-mod-dev-research.md
git -C $R commit -m "docs: 모드 개발 구성 리서치와 경로 판정 추가" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git -C $R switch develop
git -C $R merge --no-ff docs/pre-research -m "Merge branch 'docs/pre-research' into develop" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 9: 판정 게이트**

판정이 "부적합"이면 여기서 멈추고 사용자에게 보고한다. Task 4로 진행하지 않는다. "적합"이면 판정과 전제 조건을 사용자에게 요약해 알리고 Task 4로 진행한다.

---

### Task 4: 모드 폴더 설정 (빈 프로젝트)

**Files:**
- Create: `CLAUDE.md`
- Create: `README.md`
- Create: `src/ModCode/ModMain/ModMain.csproj`
- Create: `src/ModCode/ModMain/ModMain.cs`
- Create: `src/ModCode/ModMain/{Core,Adapters,Resolvers,Services,Features,UI,Safety}/.gitkeep`
- Create: `src/ModExcel/.gitkeep`, `src/ModAssets/.gitkeep`, `tests/.gitkeep`

**Interfaces:**
- Consumes: `docs/02-mod-dev-research.md`의 "9. 판정"(적합이어야 함)과 "3. 프로젝트 구조와 로드 규칙"의 네 값
- Produces: `src/ModCode/ModMain/bin/Debug/net472/ModMain.dll` — 타입 `TlToybox.ModMain`, 공개 메서드 `void Init()`와 `void Destroy()`

브랜치: `chore/mod-scaffold` (`develop`에서 분기)

**선행 확인:** `docs/02-mod-dev-research.md` 3절의 네 값이 아래 명세와 다르면(어셈블리 이름이 `ModMain`이 아니다, 진입점이 `Init()`/`Destroy()`가 아니다, 폴더 배치가 `ModCode`/`ModExcel`/`ModAssets`가 아니다 등) 즉흥으로 고치지 말고 멈춰 `NEEDS_CONTEXT`로 보고한다. 네임스페이스는 공식 템플릿의 주석대로 모드 에디터 설정과 일치해야 하며, 이 프로젝트는 `TlToybox`를 쓴다.

- [ ] **Step 1: 브랜치 생성**

```powershell
git -C $R switch develop
git -C $R switch -c chore/mod-scaffold
```

- [ ] **Step 2: 검증 명령을 먼저 실행해 실패 확인**

```powershell
dotnet build "$R\src\ModCode\ModMain\ModMain.csproj" -c Debug
```

Expected: FAIL — `MSB1009: Project file does not exist.`

- [ ] **Step 3: 프로젝트 파일 작성**

`src/ModCode/ModMain/ModMain.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <AssemblyName>ModMain</AssemblyName>
    <RootNamespace>TlToybox</RootNamespace>
    <LangVersion>7.3</LangVersion>
    <Deterministic>true</Deterministic>
    <GameDir Condition="'$(GameDir)' == ''">E:\Games\Tale of Immortal</GameDir>
    <ManagedDir>$(GameDir)\MelonLoader\Managed</ManagedDir>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="MelonLoader">
      <HintPath>$(GameDir)\MelonLoader\MelonLoader.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="0Harmony">
      <HintPath>$(GameDir)\MelonLoader\0Harmony.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="Assembly-CSharp">
      <HintPath>$(ManagedDir)\Assembly-CSharp.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="Il2Cppmscorlib">
      <HintPath>$(ManagedDir)\Il2Cppmscorlib.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="UnhollowerBaseLib">
      <HintPath>$(ManagedDir)\UnhollowerBaseLib.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(ManagedDir)\UnityEngine.CoreModule.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>

</Project>
```

`<Private>False</Private>`는 게임 DLL이 출력 폴더로 복사되지 않게 한다. 게임 경로가 다른 환경에서는 `dotnet build -p:GameDir="<경로>"`로 덮어쓴다.

- [ ] **Step 4: 진입점 클래스 작성**

`src/ModCode/ModMain/ModMain.cs`:

```csharp
using MelonLoader;

// 이 네임스페이스는 모드 에디터에 설정한 네임스페이스와 같아야 한다. 다르면 DLL 로드가 실패한다.
namespace TlToybox
{
    public class ModMain
    {
        // 게임에 진입할 때 호출된다.
        public void Init()
        {
            MelonLogger.Msg("[TlToybox] Init");
        }

        // 메인 화면으로 돌아갈 때 호출되며, 이후 Init이 다시 호출된다.
        public void Destroy()
        {
        }
    }
}
```

- [ ] **Step 5: 빈 폴더 생성**

```powershell
$dirs = 'Core','Adapters','Resolvers','Services','Features','UI','Safety' | ForEach-Object { "$R\src\ModCode\ModMain\$_" }
$dirs += "$R\src\ModExcel", "$R\src\ModAssets", "$R\tests"
foreach ($d in $dirs) { New-Item -ItemType Directory -Force $d | Out-Null; New-Item -ItemType File -Force "$d\.gitkeep" | Out-Null }
```

- [ ] **Step 6: 빌드해서 통과 확인**

```powershell
dotnet build "$R\src\ModCode\ModMain\ModMain.csproj" -c Debug
```

Expected: `Build succeeded.` 경고 0, 오류 0.

`MSB3644`(참조 어셈블리 없음)가 나오면 패키지 복원이 실패한 것이다. `dotnet restore`의 출력을 확인하고, 네트워크 차단이 원인이면 멈추고 보고한다(Review Focus 4). 참조 DLL을 찾지 못한다는 경고(`MSB3245`)가 나오면 `$(GameDir)` 경로를 확인한다.

- [ ] **Step 7: 산출물 검증**

```powershell
$out = "$R\src\ModCode\ModMain\bin\Debug\net472"
Get-ChildItem $out | Select-Object Name,Length
Add-Type -Path "$G\MelonLoader\Mono.Cecil.dll"
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$out\ModMain.dll")
$t = $a.MainModule.GetType('TlToybox.ModMain')
$t.FullName
$t.Methods | Where-Object { $_.IsPublic -and -not $_.IsConstructor } | ForEach-Object { "$($_.ReturnType.Name) $($_.Name)($($_.Parameters.Count))" }
$a.MainModule.GetTypes() | Where-Object { $_.Name -ne '<Module>' } | Measure-Object | Select-Object Count
$a.Dispose()
git -C 'E:\Games\Tale of Immortal' status 2>&1 | Select-Object -First 1
Get-ChildItem "$G\Mods","$G\ModExportData" | Select-Object FullName,LastWriteTime
```

Expected:
- 출력 폴더에 `ModMain.dll`과 `ModMain.pdb`만 있다(게임 DLL 없음).
- `TlToybox.ModMain`이 출력되고 공개 메서드는 `Void Init(0)`, `Void Destroy(0)` 두 개다.
- 타입 수는 1이다(기능 코드 없음).
- 게임의 `Mods`와 `ModExportData`의 내용과 수정 시각이 작업 전과 같다.

- [ ] **Step 8: `CLAUDE.md`와 `README.md` 작성**

`CLAUDE.md`:

```markdown
# TlToybox

귀곡팔황(Tale of Immortal) 통합 치트·샌드박스 모드 `GGBH Cheat Sandbox`의 작업 저장소.

## 브랜치 전략

- `main`은 보호 브랜치다. 직접 커밋하지 않는다.
- `develop`이 통합 브랜치다. 직접 커밋하지 않는다.
- 작업은 `develop`에서 분기한 `feat/*`, `fix/*`, `chore/*`, `docs/*` 브랜치에서 한다.
- 현재는 원격 저장소가 없다. 작업 브랜치는 로컬에서 `git merge --no-ff`로 `develop`에 병합한다.
- `main`에는 릴리스 시점에 `develop`만 병합한다.

## 게임 폴더 규칙

- 게임 설치 경로: `E:\Games\Tale of Immortal`
- 게임 폴더는 읽기 전용으로 다룬다. 빌드 산출물을 게임 폴더로 복사하는 일은 사용자 확인을 받은 뒤에만 한다.
- 게임 DLL을 저장소에 복사하지 않는다. 참조는 `$(GameDir)\MelonLoader`를 가리킨다.
- 세이브 파일을 수정하는 실험은 별도 테스트 세이브에서만 한다.

## 문서 규칙

- 클래스명·필드명·메서드명은 어셈블리 또는 API 문서에서 실제로 확인한 것만 적는다.
- 확인하지 못한 항목은 "미확인"과 사유를 적는다.

## 빌드

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Debug

게임 경로가 다르면 `-p:GameDir="<경로>"`를 붙인다.
```

`README.md`:

```markdown
# TlToybox

귀곡팔황(Tale of Immortal) 통합 치트·샌드박스 모드 `GGBH Cheat Sandbox`.

현재 단계: 사전 조사 완료, 기능 구현 전.

## 구조

| 경로 | 내용 |
|---|---|
| `docs/00-design-brief.md` | 기획서 |
| `docs/01-game-structure.md` | 설치본 구조 |
| `docs/02-mod-dev-research.md` | 모드 개발 구성과 경로 판정 |
| `docs/02a-workshop-survey.md` | 창작마당·모드 사이트 조사표 |
| `docs/04-ingame-state-analysis.md` | 인게임 상태 구성 분석 |
| `src/ModCode/ModMain/` | 코드 모드 프로젝트(현재 빈 진입점만 있음) |
| `src/ModExcel/` | 설정 테이블 |
| `src/ModAssets/` | 리소스 |

## 빌드

요구 사항: .NET SDK 8, MelonLoader가 설치된 게임.

    dotnet build src/ModCode/ModMain/ModMain.csproj -c Debug

산출물은 `src/ModCode/ModMain/bin/Debug/net472/ModMain.dll`이다. 게임 폴더로의 배포는 아직 자동화하지 않았다.
```

- [ ] **Step 9: Commit과 develop 병합**

```powershell
git -C $R add CLAUDE.md README.md src tests
git -C $R status --short
git -C $R commit -m "chore: 빈 모드 프로젝트와 저장소 안내 문서 추가" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git -C $R switch develop
git -C $R merge --no-ff chore/mod-scaffold -m "Merge branch 'chore/mod-scaffold' into develop" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

커밋 전 `git status --short`에 `bin/`이나 `obj/` 아래 파일이 없어야 한다.

- [ ] **Step 10: 중단 지점**

빌드 산출물을 게임 폴더로 복사하지 않는다. 인게임 로드 확인도 하지 않는다. 빌드 성공과 산출물 경로를 사용자에게 알리고 Task 5로 진행한다.

---

### Task 5: 인게임 상태 구성 분석

**Files:**
- Create: `docs/04-ingame-state-analysis.md`

**Interfaces:**
- Consumes: `docs/02-mod-dev-research.md`의 6절(설정 테이블 위치·형식), Task 3에서 추출한 `GGBH_API.chm` 내용, `docs/00-design-brief.md`의 3~9장과 11.1 표
- Produces: `docs/04-ingame-state-analysis.md` — 이후 v0.1 구현 계획의 입력

브랜치: `docs/pre-research` (`develop`의 최신 내용을 먼저 병합)

- [ ] **Step 1: 브랜치 준비**

```powershell
git -C $R switch docs/pre-research
git -C $R merge --no-ff develop -m "Merge branch 'develop' into docs/pre-research" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 2: 프록시 어셈블리 메타데이터 덤프**

스크래치패드에 `$S\dump-types.ps1`을 만든다(저장소에 넣지 않는다).

```powershell
param([string]$Game, [string]$Out)
Add-Type -Path "$Game\MelonLoader\Mono.Cecil.dll"
$rp = New-Object Mono.Cecil.ReaderParameters
$rp.ReadWrite = $false
$sw = [IO.StreamWriter]::new($Out, $false, [Text.Encoding]::UTF8)
foreach ($name in 'Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','GameConfPack2.dll','GameConfPack3.dll') {
  $asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$Game\MelonLoader\Managed\$name", $rp)
  $sw.WriteLine("ASSEMBLY $name")
  foreach ($t in $asm.MainModule.GetTypes()) {
    $base = if ($t.BaseType) { $t.BaseType.FullName } else { '' }
    $kind = if ($t.IsEnum) { 'ENUM' } else { 'TYPE' }
    $sw.WriteLine("$kind $($t.FullName) : $base")
    if ($t.IsEnum) {
      foreach ($f in $t.Fields) { if ($f.HasConstant) { $sw.WriteLine("  E $($f.Name) = $($f.Constant)") } }
      continue
    }
    foreach ($p in $t.Properties) { $sw.WriteLine("  P $($p.PropertyType.FullName) $($p.Name)") }
    foreach ($m in $t.Methods) {
      if ($m.IsGetter -or $m.IsSetter -or $m.IsConstructor) { continue }
      $ps = ($m.Parameters | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ', '
      $st = if ($m.IsStatic) { 'static ' } else { '' }
      $sw.WriteLine("  M $st$($m.ReturnType.FullName) $($m.Name)($ps)")
    }
  }
  $asm.Dispose()
}
$sw.Dispose()
```

실행:

```powershell
pwsh -NoProfile -File "$S\dump-types.ps1" -Game $G -Out "$S\types.txt"
(Get-Item "$S\types.txt").Length
(Select-String -Path "$S\types.txt" -Pattern '^(TYPE|ENUM) ').Count
```

Expected: `types.txt`가 생성되고 타입 수가 수천 개 이상이다. Unhollower 프록시에서는 원본 필드가 프로퍼티(`P` 줄)로 나타난다. 실행이 10분 넘게 걸리면 백그라운드로 돌린다.

- [ ] **Step 3: 전역 진입점 파악**

공식 템플릿이 `g.timer`를 쓰므로 전역 접근 클래스 `g`부터 본다.

```powershell
$t = "$S\types.txt"
$lines = Get-Content $t -Encoding UTF8
$i = [Array]::IndexOf($lines, ($lines | Where-Object { $_ -match '^TYPE g : ' } | Select-Object -First 1))
$lines[$i..($i+200)] | Where-Object { $_ -match '^(TYPE|  P|  M static)' } | Select-Object -First 200
```

`g`의 정적 프로퍼티 각각의 타입을 기록하고, 그 타입들의 정의를 같은 방식으로 따라간다. 이름이 `g`가 아니면 `GGBH_API.chm`의 목차에서 전역 접근 클래스를 찾는다.

- [ ] **Step 4: 영역별 타입 탐색**

영역마다 아래 패턴으로 후보 타입을 찾고, 후보의 정의 블록을 읽어 상태를 소유하는 클래스와 필드를 가려낸다.

```powershell
function Find-Types($pattern) { Select-String -Path "$S\types.txt" -Pattern "^(TYPE|ENUM) .*($pattern)" | Select-Object -ExpandProperty Line }
Find-Types 'WorldUnit|UnitData|PlayerUnit|Hero'          # 플레이어·NPC
Find-Types 'Prop|Item|Bag|Equip'                         # 아이템
Find-Types 'Grade|Realm|Breakthrough|Upgrade'            # 경지·돌파
Find-Types 'Skill|Martial|Ability|Step|Immortal'         # 공법·무공·신통
Find-Types 'Relation|Intim|Hate|Marry|Master'            # 관계
Find-Types 'School|Sect'                                 # 문파
Find-Types 'World|Map|Area|Time|Month|Event|Drama|Task'  # 세계·이벤트
Find-Types 'Battle|Fight|Damage|Hit|Buff|Missile'        # 전투
Find-Types '^Conf|ConfBase|ConfMgr'                      # 설정 테이블 클래스
```

이 패턴은 탐색의 출발점이다. 결과가 없거나 지나치게 많으면 Step 3에서 `g`로부터 따라간 타입 이름과 `GGBH_API.chm`의 목차를 근거로 패턴을 바꾼다. 문서에는 패턴이 아니라 실제로 확인한 타입만 적는다.

각 후보 타입에 대해 확인할 것:

- 어느 객체가 이 상태를 소유하는가(`g`에서 어떤 경로로 도달하는가)
- 값이 직접 저장된 필드인가, 다른 값에서 계산되는 파생값인가(이름이 `Get*`/`Calc*`/`Update*`/`Refresh*`인 메서드의 존재로 단서를 얻는다)
- 값을 바꾸는 공식 메서드가 있는가(`Add*`/`Set*`/`Create*`/`Del*` 메서드)
- 템플릿(설정 테이블의 행)과 인스턴스(세이브에 저장되는 개체)가 어떻게 나뉘는가

- [ ] **Step 5: API 문서와 대조**

Task 3에서 추출한 `$S\chm\out`의 문서에서 Step 4의 타입들을 찾아, 문서의 설명과 어셈블리의 멤버가 일치하는지 확인한다.

```powershell
Select-String -Path "$S\chm\out\*.htm*" -Pattern 'WorldUnitBase|UnitData' -List | Select-Object Path
```

문서에는 있는데 어셈블리에 없거나 그 반대인 멤버는 따로 표로 적는다. 실측값은 어셈블리 쪽이다(Review Focus 5).

- [ ] **Step 6: 설정 테이블 대조**

`docs/02-mod-dev-research.md` 6절에 적힌 테이블 위치·형식을 근거로, 영역별로 관련 설정 테이블 이름과 그것을 읽는 `Conf*` 클래스를 짝짓는다. 예제 `ModExcel`의 xlsx 머리글에서 필드명을 확인한다.

- [ ] **Step 7: `docs/04-ingame-state-analysis.md` 작성**

```markdown
# 04. 인게임 상태 구성 분석

> 분석일: 2026-10-12 · 방법: 정적 분석(프록시 어셈블리 메타데이터, GGBH_API.chm, 설정 테이블)
> 한계: 런타임 검증을 하지 않았다. 쓰기·저장 반영·연쇄 처리는 확정하지 않는다.

## 1. 요약
## 2. 분석 대상과 방법
## 3. 전역 접근 구조
## 4. 플레이어
## 5. NPC
## 6. 아이템
## 7. 경지·돌파
## 8. 공법·무공·신통
## 9. 관계
## 10. 문파
## 11. 세계·이벤트
## 12. 전투
## 13. 템플릿과 인스턴스의 구분
## 14. API 문서와 어셈블리의 불일치
## 15. v0.1 런타임 검증 목록
```

4~12절은 각각 다음 세 부분으로 쓴다.

1. 상태 소유 구조 — `g`에서 해당 데이터까지의 접근 경로와 관련 타입 목록
2. 주요 필드 표 — `| 기획서 항목 | 타입 | 멤버 | 멤버 타입 | 저장값/파생값 | 관련 변경 메서드 | 관련 설정 테이블 |`
3. 기준 문서 11.1의 조사 기록 표 — 기능 후보별로 `| 기능 | 후보 클래스/메서드 | 데이터 소유자 | 읽기 | 쓰기 | 저장 반영 | 연쇄 처리 | 충돌 |`

기입 규칙:

- "읽기" 열은 접근 경로가 메타데이터로 확인되면 `후보 확인(정적)`, 아니면 `미확인`.
- "쓰기", "저장 반영", "연쇄 처리", "충돌" 열은 전부 `미확인(런타임 검증 필요)`. 변경 메서드가 존재하면 "후보 클래스/메서드" 열에 이름을 적는다.
- 기획서의 항목에 대응하는 멤버를 찾지 못하면 행을 지우지 말고 `미확인`으로 남긴다.

"15. v0.1 런타임 검증 목록"은 4~12절에서 `미확인(런타임 검증 필요)`로 남은 항목을 영역별로 모아, 기준 문서 11.2의 검증 순서(읽기 → 단순 값 한 항목 쓰기 → 재시작 후 지속성)에 맞춰 우선순위를 붙인다.

- [ ] **Step 8: 검증**

```powershell
$f = "$R\docs\04-ingame-state-analysis.md"
1..15 | ForEach-Object { if (-not (Select-String -Path $f -Pattern "^## $_\. " -Quiet)) { "MISSING section $_" } }
$dump = Get-Content "$S\types.txt" -Encoding UTF8 -Raw
[regex]::Matches((Get-Content $f -Raw -Encoding UTF8), '`([A-Za-z_][A-Za-z0-9_.]*)`') |
  ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique |
  Where-Object { $dump -notmatch [regex]::Escape(($_ -split '\.')[-1]) } 
Select-String -Path $f -Pattern '쓰기.*확인됨|저장 반영.*확인됨'
git -C $R status --short
```

Expected: `MISSING` 없음. 두 번째 명령은 문서에서 백틱으로 적은 식별자 중 덤프에 없는 것을 출력한다 — 파일 경로나 테이블 이름이 아닌 식별자가 나오면 문서에서 고친다(추측 이름 금지). 세 번째 명령은 아무것도 출력하지 않는다(정적 분석으로 쓰기를 확정하지 않는다). `git status`에는 `docs/04-ingame-state-analysis.md`만 나온다.

- [ ] **Step 9: Commit과 develop 병합**

```powershell
git -C $R add docs/04-ingame-state-analysis.md
git -C $R commit -m "docs: 인게임 상태 구성 분석 추가" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git -C $R switch develop
git -C $R merge --no-ff docs/pre-research -m "Merge branch 'docs/pre-research' into develop" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git -C $R log --oneline --graph --all
```

- [ ] **Step 10: 최종 확인**

```powershell
git -C $R branch -a
git -C $R remote -v
git -C $R log main --oneline
Get-ChildItem "$G\Mods","$G\ModExportData","$G\Plugins","$G\UserLibs" | Select-Object FullName,LastWriteTime
```

Expected: 원격 없음. `main`에는 초기 커밋 하나뿐이다. 게임의 모드 관련 폴더가 작업 전과 같다(`Mods\GGBH_MOD.dll` 하나, 나머지는 비어 있음).
