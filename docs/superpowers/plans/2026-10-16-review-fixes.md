# 리뷰 수정 묶음(X) 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 리뷰 보고서의 묶음 "작은 수정 아홉(1·3·4·5·7·13·14·16·20) + 탭 줄 재배치(2·24)"와 결과 칸(8), 46절의 빈 이름 줄을 고친다.

**Architecture:** 탭마다 고정 번호를 두어 검사의 `Tab<번호>`를 지키면서 화폐 탭을 자원 탭의 그룹으로, 안전 탭을 제목 줄 단추로 옮긴다. 작은 수정은 각각 한 파일 안의 변경이고, 검사는 `tools/research/ui-check.ps1`의 부분(part)으로 회차 안에서 돈다. 검사 스크립트를 먼저 고쳐 RED 회차에서 실패를 보고, 코드를 두 묶음(창·자원 탭 / 작은 수정)으로 고쳐 GREEN 회차 둘에서 본다.

**Tech Stack:** C# 7.3(MelonLoader 0.5.4, IL2CPP uGUI), PowerShell 7(`tools/research/ui-check.ps1`, `tools/DevRemote/session.ps1`), 빌드 `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release`와 `-c Debug`(DEVTOOLS).

**Spec:** `docs/superpowers/specs/2026-10-16-review-fixes-design.md`

## Global Constraints

- 탭 번호는 스펙 3.2절의 표 그대로(능력치 0 … 도호 15, 2는 비움, 안전 14는 제목 줄). 검증 스크립트의 `Press "Tab<번호>"`와 `uistate`의 `tab=<번호>`가 그 번호다.
- 탭 줄: 간격 46, 단추 150x40, 탭 줄 높이 18·46. 자원 탭 `TopH` 48, 그룹 간격 42 그대로.
- 요소 이름(검사가 찾는다): `Tab<번호>`, `ResGroupCurrency`, `ItemName<id>`, `CurrencyOwned<id>`, `CurrencyAdd<id>`, `CurrencyOwnedFame`, `CurrencyAddFame`, `MessageMore`, `MessageEllipsis`, `MessageFull`, `MessageFullText`, `DrawError`, `DevThrow`, `MartialBroken<순번>`, `MartialBrokenNote`.
- 확인 키: `luck-replace:<id>`, `fate-pick:<id>`, `skill-learn-all`, `art-learn-all`. 확인 글 "정말 <동작>", 안내 끝 "한 번 더 누르면 실행합니다."
- 개발 명령(DEVTOOLS에만): `uifail`, `uibuttons <접두>`, `mbreak`, `unload`, `bkreset`, `upopen`, `fateclash`, `fateamong <대경지> <번호> <pending…>`, `cimpsec <이름> <구역>`.
- 두 구성 모두 경고 0으로 빌드된다. 게임 폴더에는 쓰지 않는다(설치는 사용자 확인 뒤 별도).
- 회차 이름 `ws-1017e`(RED), `ws-1017f`(GREEN 1), `ws-1017g`(GREEN 2). 다시 쓰지 않는다.
- 커밋 메시지 끝에 `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.

## Review Focus

1. 안전을 보던 채 창을 닫았다 열면 `Tab14`가 돌아가기이고 눌렀을 때 `tabBefore`로 간다(한 번도 연 적이 없으면 0). → Task 2의 `tabs` 검사에 "닫았다 연 뒤"를 넣지 않았다. 리뷰어가 코드로 본다.
2. 결과 칸을 펼친 채 다른 단추를 누르면: 덮인 단추는 눌리지 않고, 보이는 단추(탭 줄)를 누르면 접힌다. → Task 1 `tabs`의 'a tab change folds it'.
3. 화폐 그룹에서 검색 칸이 없으므로 다른 그룹에서 남긴 검색어(`filter.Query`)가 화폐 줄을 거르지 않아야 한다. → Task 3의 `Rows()`는 화폐 그룹에서 `filter`를 보지 않는다. Task 1 `items`에서 화폐 그룹 전에 `ResClear`를 누르지 않고 들어간다.
4. `mbreak`로 만든 깨진 공법이 있을 때 다른 탭(보관함의 캐릭터 내보내기, 프리셋)은 이미 try/catch가 있다(리뷰 C1). 회차 끝의 `-End`가 세이브를 되돌린다.
5. `ResetState`가 `Destroy`에서 예외를 내면 뒤의 `OnDestroy`들이 돌지 않는다. → Task 8에서 try/catch로 감싼다.

---

### Task 1: 검사 스크립트 (RED)

**Files:**
- Modify: `tools/research/ui-check.ps1` (`$tabProbe`, `base`, `tabs`, `items`, `wrap`, `actions`, `modbooks`, `status`; 새 부분 `confirm2`·`errors`·`backup`·`import-settle`)
- Modify: `tools/research/preset-check.ps1:417`

**Interfaces:**
- Consumes: 없음(검사가 기대하는 이름은 Global Constraints).
- Produces: 회차에서 돌릴 부분 이름 `confirm2`, `errors`, `backup`, `import-settle`.

- [ ] **Step 1: 스크래치에 치환 스크립트를 쓴다(heredoc 금지 — 백슬래시가 깨진다. Write 도구로 `check-x.py`)**

```python
import io
p = r'E:\TlToybox\tools\research\ui-check.ps1'
s = io.open(p, encoding='utf-8-sig').read()
def rep(old, new):
    global s
    assert s.count(old) == 1, old[:70]
    s = s.replace(old, new)

# $tabProbe: 번호 → 단추 표
rep("""# 탭마다 그 탭에만 있는 버튼 하나. 탭이 제대로 그려졌는지 보는 데 쓴다.
$tabProbe = @('Restore', 'MoneyAdd', 'CurrencyAdd10001', 'VaultModeBag', 'FillExp', 'SkillAddOpen', 'ImmortalAddOpen', 'ArtifactPoint', 'LuckAddOpen', 'ArtLearnAll', 'NpcRefresh', 'Freeze', 'God', 'PresetRestore', 'BackupNow', 'AplClassAll')""",
"""# 탭 번호마다 그 탭에만 있는 버튼 하나(05 문서 47절: 번호는 고정이고 2는 비었다. 14는 제목 줄의 안전 단추). 탭이 제대로 그려졌는지 보는 데 쓴다.
$tabProbe = @(
  @(0, 'Restore'), @(1, 'BagApply'), @(3, 'VaultModeBag'), @(4, 'FillExp'), @(5, 'SkillAddOpen'), @(6, 'ImmortalAddOpen'), @(7, 'ArtifactPoint'),
  @(8, 'LuckAddOpen'), @(9, 'ArtLearnAll'), @(10, 'NpcRefresh'), @(11, 'Freeze'), @(12, 'God'), @(13, 'PresetRestore'), @(14, 'BackupNow'), @(15, 'AplClassAll'))""")

# base
rep("""    for ($i = 0; $i -lt $tabProbe.Count; $i++) {
      Press "Tab$i" | Out-Null
      Check "tab $i shows $($tabProbe[$i])" (IsAt $tabProbe[$i])
    }
    Press 'Tab0' | Out-Null
    Check 'uitext reads a label'""",
"""    foreach ($t in $tabProbe) {
      Press "Tab$($t[0])" | Out-Null
      Check "tab $($t[0]) shows $($t[1])" (IsAt $t[1])
    }
    Press 'Tab0' | Out-Null
    Check 'uitext reads a label'""")

# tabs: 탭 줄 재배치
rep("""    for ($i = 0; $i -lt $tabProbe.Count; $i++) {
      Press "Tab$i" | Out-Null
      Check "tab $i is selected" ((Cmd 'uistate') -match "tab=$i ")
      Check "tab $i shows $($tabProbe[$i])" (IsAt $tabProbe[$i])
    }
    Press 'Tab11' | Out-Null
    Check 'world tab no longer has the battle helpers' (-not (IsAt 'God'))""",
"""    foreach ($t in $tabProbe) {
      Press "Tab$($t[0])" | Out-Null
      Check "tab $($t[0]) is selected" ((Cmd 'uistate') -match "tab=$($t[0]) ")
      Check "tab $($t[0]) shows $($t[1])" (IsAt $t[1])
    }
    # 05 문서 47절: 화폐 탭은 없고(자원 탭의 그룹), 탭 간격 46, 안전은 제목 줄의 단추 Tab14(열면 돌아가기, 다시 누르면 앞 탭).
    Check 'the currency tab is gone' ((Cmd 'uipos Tab2') -match 'no element named')
    $a = Pos 'Tab0'; $b = Pos 'Tab1'; $c = Pos 'Tab3'
    Check 'tab buttons are 46 apart' (($b.y - $a.y) -eq 46 -and ($c.y - $b.y) -eq 46) "y=$($a.y),$($b.y),$($c.y)"
    Check 'the safety button is in the title bar' ((Pos 'Tab14').y -lt $a.y) "safety y=$((Pos 'Tab14').y) tab0 y=$($a.y)"
    Press 'Tab5' | Out-Null; Press 'Tab14' | Out-Null
    Check 'the safety view opens from the title bar' ((Cmd 'uistate') -match 'tab=14 ' -and (IsAt 'BackupNow') -and (Cmd 'uitext Tab14') -match "text='돌아가기'")
    Press 'Tab14' | Out-Null
    Check 'pressing it again returns to the tab before' ((Cmd 'uistate') -match 'tab=5 ' -and (IsAt 'SkillAddOpen') -and (Cmd 'uitext Tab14') -match "text='안전'")
    Press 'Tab14' | Out-Null; Press 'Tab0' | Out-Null
    Check 'a tab button leaves the safety view' ((Cmd 'uistate') -match 'tab=0 ' -and (Cmd 'uitext Tab14') -match "text='안전'")
    Press 'Tab11' | Out-Null
    Check 'world tab no longer has the battle helpers' (-not (IsAt 'God'))""")

# tabs: 결과 칸 더 보기
rep("""    Check 'a long result is accepted' ((Cmd 'uisay 30') -match 'said 360 chars')
    Shot 'long-message'
    Press 'Tab1' | Out-Null""",
"""    Check 'a long result is accepted' ((Cmd 'uisay 30') -match 'said 360 chars')
    # 05 문서 48절(U3): 세 줄을 넘는 결과는 "…"과 더 보기. 펼치면 스크롤 칸에 전체, 접기·탭 전환으로 접힌다.
    Check 'a long result offers more' ((IsAt 'MessageMore') -and (Cmd 'uipos MessageEllipsis') -match ' at ' -and (Cmd 'uitext MessageMore') -match "text='더 보기'")
    Press 'MessageMore' | Out-Null
    Check 'more shows the whole text in a scroll box' ((Cmd 'uipos MessageFull') -match ' at ' -and (Cmd 'uitext MessageFullText') -match "(?s)text='.{360}'" -and (Cmd 'uitext MessageMore') -match "text='접기'")
    Shot 'long-message'
    Press 'MessageMore' | Out-Null
    Check 'fold hides the scroll box' ((Cmd 'uipos MessageFull') -match 'no element named' -and (IsAt 'MessageMore'))
    Press 'MessageMore' | Out-Null; Press 'Tab0' | Out-Null
    Check 'a tab change folds it' ((Cmd 'uipos MessageFull') -match 'no element named' -and (Cmd 'uipos MessageMore') -match 'no element named')
    Cmd 'uisay 1' | Out-Null
    Check 'a short result has no more button' ((Cmd 'uipos MessageMore') -match 'no element named' -and (Cmd 'uipos MessageEllipsis') -match 'no element named')
    Press 'Tab1' | Out-Null""")

# items: 화폐 그룹
rep("""    # 화폐: 영석 말고도 화폐로 쓰이는 재화(사용자 지시로 추가)
    Press 'Tab2' | Out-Null
    Check 'the currency tab lists the other currencies' ((IsAt 'CurrencyAdd10011')""",
"""    # 화폐 그룹(05 문서 47절): 영석 말고도 화폐로 쓰이는 재화. 아이템 표의 줄로 보인다(☆ 없음, 보유 CurrencyOwned, 지급 CurrencyAdd).
    Press 'Tab1' | Out-Null; Press 'ResGroupCurrency' | Out-Null
    Check 'the currency group has no star column' ((Cmd 'uipos ResStar10001') -match 'no element named' -and (Cmd 'uitext CurrencyOwned10001') -match "text='\\d+'")
    Check 'the currency group lists the other currencies' ((IsAt 'CurrencyAdd10011')""")
rep("""    foreach ($cid in 10041, 10011) {
      $had = if ((Cmd "count $cid") -match 'count=(\\d+)') { [int]$Matches[1] } else { -1 }
      Press "CurrencyAdd$cid" | Out-Null
      $has = if ((Cmd "count $cid") -match 'count=(\\d+)') { [int]$Matches[1] } else { -1 }
      Check "currency $cid goes up by the chosen amount" ($has -eq $had + 10) "$had -> $has"
    }""",
"""    $step = if ((Cmd 'uitext CurrencyAdd10041') -match "text='\\+ (\\d+)'") { [int]$Matches[1] } else { -1 }   # 지급 수량(AmountBar item)
    foreach ($cid in 10041, 10011) {
      $had = if ((Cmd "count $cid") -match 'count=(\\d+)') { [int]$Matches[1] } else { -1 }
      Press "CurrencyAdd$cid" | Out-Null
      $has = if ((Cmd "count $cid") -match 'count=(\\d+)') { [int]$Matches[1] } else { -1 }
      Check "currency $cid goes up by the chosen amount" ($step -gt 0 -and $has -eq $had + $step) "$had -> $has step=$step"
    }""")
rep("""    Press 'CurrencyAdd10001' | Out-Null
    Check 'spirit stones can be added here too' ((Cmd 'read') -match "money=$($money + 10) ")""",
"""    Press 'CurrencyAdd10001' | Out-Null
    Check 'spirit stones can be added here too' ((Cmd 'read') -match "money=$($money + $step) ")
    Check 'the owned column follows' ((Cmd 'uitext CurrencyOwned10001') -match "text='$($money + $step)'")
    Press 'ResGroupAll' | Out-Null""")

# wrap: 화폐 설명은 설명 칸에서
rep("""    # 화폐 탭: 설명이 잘리지 않는다(정도의 기 10031의 설명은 44자를 넘는다)
    Press 'Tab2' | Out-Null
    $d = TextIn 'CurrencyDesc10031'
    Check 'a currency description is whole' ($d.Length -gt 46 -and $d -notmatch '…') "len=$($d.Length) $d"
    $a = Box 'CurrencyRow10031'; $b = Box 'CurrencyRow10032'
    Check 'currency rows do not overlap' ($a -and $b -and $b.top -ge $a.top + $a.h - 1) "a=$($a.top)+$($a.h) b=$($b.top)"
""",
"""    # 화폐 그룹(05 문서 47절): 설명은 설명 칸에서 전부 보인다(정도의 기 10031의 설명은 44자를 넘는다)
    Press 'Tab1' | Out-Null; Press 'ResGroupCurrency' | Out-Null
    Press 'ItemName10031' | Out-Null
    $d = TextIn 'ItemDetailText'
    Check 'a currency description is whole' ($d.Length -gt 46 -and $d -notmatch '…') "len=$($d.Length) $d"
""")
rep("""    Press 'Tab2' | Out-Null; Shot 'wrap-currency'""",
    """    Press 'Tab1' | Out-Null; Press 'ResGroupCurrency' | Out-Null; Shot 'wrap-currency'""")

# actions: 깨진 공법 줄(C1)
rep("""    "INFO art 2: $($s -replace '.*message=', '')"
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'vault' {""",
"""    "INFO art 2: $($s -replace '.*message=', '')"

    # 05 문서 48절(C1): 깨진 공법 줄은 "(읽을 수 없음)"으로 그려지고 망각으로 지울 수 있다. mbreak가 기술 번호가 없는 공법을 넣는다.
    Press 'Tab5' | Out-Null
    $had = if ((Cmd 'uitext MartialListRange') -match '전체 (\\d+)개') { [int]$Matches[1] } else { 0 }
    $o = Cmd 'mbreak' 1500
    Press 'Tab0' | Out-Null; Press 'Tab5' | Out-Null
    if ((Cmd 'uipos MartialListLast') -match ' at ') { Press 'MartialListLast' | Out-Null }
    $has = if ((Cmd 'uitext MartialListRange') -match '전체 (\\d+)개') { [int]$Matches[1] } else { 0 }
    Check 'the broken martial is in the list' ($has -eq $had + 1 -and (Cmd 'uipos MartialBroken*') -match ' at ') "$had -> $has; $o"
    Check 'the other rows are still drawn' (IsAt 'SkillAddOpen')
    if ((Cmd 'uipos MartialBroken*') -match '-> MartialBroken(\\d+) at ') {
      Press "PickMartial$($Matches[1])" | Out-Null
      Check 'picking it shows the notice and the forget button' ((Cmd 'uipos MartialBrokenNote') -match ' at ' -and (IsAt 'MartialForget'))
      $s = PressThrough 'MartialForget'
      Check 'forgetting removes it' ($s -match "message='공법 망각" -and (Cmd 'uipos MartialBroken*') -match 'no element named') $s
    }
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'confirm2' {
    # 05 문서 48절(U2): 선천운명 교체·역천개명 정하기·기술 모두 배우기·기예 모두 배우기는 두 번 눌러야 한다.
    $traps = Traps
    function Range([string]$name) { if ((Cmd "uitext $name") -match '전체 (\\d+)개') { [int]$Matches[1] } else { 0 } }
    # 켜진 첫 단추(uibuttons: 이름 on|off text='…')
    function FirstOn([string]$prefix) { foreach ($line in ((Cmd "uibuttons $prefix" 300) -split "`n")) { if ($line -match "^(\\S+) on ") { return $Matches[1] } }; $null }
    Cmd 'editor' | Out-Null; Cmd 'uitarget player' | Out-Null

    # 기술 모두 배우기(심법 분류)
    Press 'Tab5' | Out-Null
    $had = Range 'MartialListRange'
    Press 'SkillAddOpen' | Out-Null; Press 'SkillKind3' | Out-Null
    Press 'SkillLearnAll' | Out-Null
    Check 'learn-all asks first' ((Cmd 'uitext SkillLearnAll') -match "text='정말 모두 배우기'" -and (Cmd 'uistate') -match '한 번 더 누르면')
    Press 'Back' | Out-Null
    Check 'the first press learns nothing' ((Range 'MartialListRange') -eq $had) "$had -> $(Range 'MartialListRange')"
    Press 'SkillAddOpen' | Out-Null; Press 'SkillKind3' | Out-Null
    Press 'SkillLearnAll' | Out-Null; Press 'SkillLearnAll' 4000 | Out-Null
    $s = Cmd 'uistate'
    Press 'Back' | Out-Null
    Check 'the second press learns them' ((Range 'MartialListRange') -gt $had) "$had -> $(Range 'MartialListRange'); $s"

    # 기예 모두 배우기
    Press 'Tab9' | Out-Null
    $label0 = Cmd 'uitext ArtKind0'
    Press 'ArtLearnAll' | Out-Null
    Check 'art learn-all asks first' ((Cmd 'uitext ArtLearnAll') -match "text='정말 모두 배우기'" -and (Cmd 'uitext ArtKind0') -eq $label0)
    Press 'ArtLearnAll' 4000 | Out-Null
    Check 'the second press learns the arts' ((Cmd 'uitext ArtKind0') -ne $label0 -and (Cmd 'uistate') -match "message='[^']+'") (Cmd 'uistate')

    # 선천운명 교체
    Press 'Tab8' | Out-Null
    Press (Pos 'LuckReplace*').name | Out-Null
    $pick = FirstOn 'LuckAdd'
    Check 'the replace picker has a candidate' ($null -ne $pick)
    if ($pick) {
      Press $pick | Out-Null
      Check 'replacing asks first' ((Cmd "uitext $pick") -match "text='정말 교체'" -and (Cmd 'uistate') -match '한 번 더 누르면' -and (IsAt 'Back'))
      Press $pick | Out-Null
      Check 'the second press replaces and closes the picker' ((IsAt 'LuckAddOpen') -and (Cmd 'uistate') -match "message='[^']+'") (Cmd 'uistate')
    }

    # 역천개명 정하기
    Press 'FateOpen' | Out-Null
    Press (Pos 'FateChange*').name | Out-Null
    $pick = FirstOn 'FatePick'
    Check 'the fate picker has a candidate' ($null -ne $pick)
    if ($pick) {
      Press $pick | Out-Null
      Check 'picking a fate asks first' ((Cmd "uitext $pick") -match "text='정말 정하기'" -and (Cmd 'uistate') -match '한 번 더 누르면')
      Press $pick | Out-Null
      Check 'the second press sets it and goes back to the slots' ((Cmd 'uipos FateChange*') -match ' at ' -and (Cmd 'uistate') -match "message='[^']+'") (Cmd 'uistate')
    }
    Press 'Close' | Out-Null
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'errors' {
    # 05 문서 48절(C2): 단추 콜백과 탭 그리기의 예외가 결과 칸·내용 영역에 보인다. DevThrow·uifail은 개발 빌드의 시험 장치.
    $traps = Traps
    $w0 = @(ModWarnings).Count
    Cmd 'editor' | Out-Null; Press 'Tab0' | Out-Null
    Press 'DevThrow' | Out-Null
    Check 'a button exception shows in the result line' ((Cmd 'uistate') -match "message='오류: InvalidOperationException: 시험 예외'") (Cmd 'uistate')
    Press 'Tab1' | Out-Null; Press 'Tab0' | Out-Null
    Check 'the window still works' (IsAt 'Restore')
    Cmd 'uifail' | Out-Null
    Press 'Tab1' | Out-Null
    Check 'a draw exception shows a notice' ((Cmd 'uitext DrawError') -match "text='이 탭을 그리지 못했습니다: InvalidOperationException: 시험 예외'") (Cmd 'uitext DrawError')
    Press 'Tab0' | Out-Null
    Check 'the next draw is normal' ((IsAt 'Restore') -and (Cmd 'uipos DrawError') -match 'no element named')
    Press 'Close' | Out-Null
    Check 'no trampoline line in the log' ((Traps) -eq $traps)
    $w = @(ModWarnings) | Select-Object -Skip $w0
    Check 'exactly one button error and one draw error were logged' ($w.Count -eq 2) ($w -join ' | ')
  }
  'backup' {
    # 05 문서 48절(C5): 백업 없이 쓰던 세 곳이 쓰기 전에 백업한다. EnsureOnce는 한 실행에 한 번이라 항목마다 bkreset으로 기록을 지운다.
    # 백업 폴더 이름이 초 단위라 항목 사이에 1초 넘게 쉰다.
    $traps = Traps
    function Backups { $d = Join-Path $data 'backups'; if (Test-Path -LiteralPath $d) { @(Get-ChildItem -LiteralPath $d -Directory).Count } else { 0 } }
    # (1) 게임의 돌파 화면 열기: 난관 기운을 지우기 전에 백업
    Cmd 'bkreset' | Out-Null; $n = Backups
    Cmd 'addexp 100000' 500 | Out-Null
    $r = Cmd 'upopen' 2000
    Check 'opening the breakthrough screen backs up first' ((Backups) -eq $n + 1 -and $r -match 'opened') "$r backups $n -> $(Backups)"
    Start-Sleep -Milliseconds 1200
    # (2) 경지 올리기(이미 그 경지 이상 → 보충만): 칭호 재판정 전에 백업
    Cmd 'bkreset' | Out-Null; $n = Backups
    $grade = if ((Cmd 'cstate' 200) -match 'grade=(\\d+)') { [int]$Matches[1] } else { 0 }
    $r = Cmd "pgraise $grade" 3000
    Check 'settling the grade backs up first' ((Backups) -eq $n + 1 -and $r -match '이미') "$r backups $n -> $(Backups)"
    Start-Sleep -Milliseconds 1200
    # (3) 역천개명 여러 칸 정하기: 다른 칸을 비우기 전에 백업(비운 뒤 도달하지 않은 경지라 거절되는 경우)
    $c = Cmd 'fateclash' 500
    if ($c -notmatch 'held=(\\d+) heldId=(\\d+) target=(\\d+)') { "INFO no fate to clash with in this save; not exercised: $c" }
    else {
      $held = $Matches[1]; $heldId = $Matches[2]; $target = $Matches[3]
      Cmd 'bkreset' | Out-Null; $n = Backups
      $r = Cmd "fateamong $target $heldId $held" 2000
      Check 'setting a fate among pending slots backs up before emptying another slot' ((Backups) -eq $n + 1 -and $r -match '아직 도달하지') "$r backups $n -> $(Backups)"
    }
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'import-settle' {
    # 05 문서 48절(C4): 캐릭터 가져오기의 경지 구역 뒤에 경지에 따른 보충(선력·등선 진입 선법)이 따라온다.
    $traps = Traps
    $dir = Join-Path $data 'exports\\character'
    $file = Join-Path $dir 'uicheck-S.json'
    if (Test-Path -LiteralPath $file) { [IO.File]::Delete($file) }
    Cmd 'cexport uicheck-S' 800 | Out-Null
    $pack = Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json
    $pack.grade.id = 44; $pack.grade.exp = 0
    [IO.File]::WriteAllText($file, ($pack | ConvertTo-Json -Depth 8), (New-Object Text.UTF8Encoding $false))
    Check 'the character starts without immortal power' ((Cmd 'imm' 200) -match 'immortalPoint=0 ') (Cmd 'imm' 200)
    $r = Cmd 'cimpsec uicheck-S grade' 4000
    Check 'the grade section reports the settle' ($r -match '경지에 따라 빠져 있던 것을 채웠습니다') $r
    $i = Cmd 'imm' 200
    Check 'the immortal power and the first art follow the imported grade' ($i -match 'immortalPoint=6 cards=5 ') $i
    Cmd 'uiclose GetImmortalSkill' 500 | Out-Null
    [IO.File]::Delete($file)
    Check 'no exception in button callbacks' ((Traps) -eq $traps)
    Check 'no warning from the mod' (-not (ModWarnings)) ((ModWarnings) -join ' | ')
  }
  'vault' {""")

# modbooks: 빈 이름 줄
rep("""    Press 'ResKind6' | Out-Null
    Check 'the mod-only chip shows every mod row' ((Range) -eq $count) (Cmd 'uitext ItemTableRange')""",
"""    Press 'ResKind6' | Out-Null
    Check 'the mod-only chip shows every mod row' ((Range) -eq $count) (Cmd 'uitext ItemTableRange')
    # 05 문서 48절: 이름이 비거나 "0"인 모드 기술은 "이름 없음(번호)"로 보이고 목록 끝에 온다
    $first = if ((Cmd 'uipos BookName*') -match '-> (BookName\\d+) at ') { $Matches[1] } else { '' }
    $t = if ($first) { Cmd "uitext $first" } else { '' }
    Check 'the first mod row has a name' ($t -match "text='([^']*)'" -and $Matches[1] -ne '' -and $Matches[1] -ne '0' -and -not $Matches[1].StartsWith('이름 없음')) $t
    Cmd 'ui set ItemQuery 126954816' | Out-Null
    Check 'a nameless mod skill is shown by number' ((Cmd 'uitext BookName126954816') -match "text='이름 없음\\(126954816\\)'") (Cmd 'uitext BookName126954816')
    Press 'ItemQueryClear' | Out-Null""")

# status: unload(C3)
rep("""    Check 'hidden again when everything is off' ((Cmd 'uistate') -match 'status=\\[hidden\\] ')
    Check 'no exception in button callbacks' ((Traps) -eq $traps)""",
"""    Check 'hidden again when everything is off' ((Cmd 'uistate') -match 'status=\\[hidden\\] ')
    # 05 문서 48절(C3): 세이브를 떠날 때(Destroy) 켜 둔 것을 끈다. unload가 같은 함수를 부른다.
    Cmd 'tfreeze on' | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab12' | Out-Null; Press 'God' | Out-Null; Press 'Multiplier' | Out-Null; Press 'Close' | Out-Null
    Start-Sleep -Milliseconds 900
    Check 'all three are on again' ((Cmd 'uistate') -match 'status=\\[무적 · 피해 x2 · 날짜 고정\\] ')
    $r = Cmd 'unload' 500
    Start-Sleep -Milliseconds 900
    Check 'leaving the save turns everything off' ((Cmd 'uistate') -match 'status=\\[hidden\\] ' -and (Cmd 'tdesc') -match 'frozen=False') $r
    Check 'no exception in button callbacks' ((Traps) -eq $traps)""")
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)

p2 = r'E:\TlToybox\tools\research\preset-check.ps1'
t = io.open(p2, encoding='utf-8-sig').read()
old = "    Check 'the safety tab is the fifteenth' (IsAt 'BackupNow')"
assert t.count(old) == 1
t = t.replace(old, "    Check 'the safety view opens from the title bar' (IsAt 'BackupNow')")
io.open(p2, 'w', encoding='utf-8', newline='\n').write(t)
print('checks updated')
```

주의: ui-check.ps1의 첫 줄에 BOM이 있는지 `utf-8-sig`로 읽고 `utf-8`로 쓴다(BOM이 있었으면 사라진다. 기존 스크립트(K)도 같은 방식으로 썼고 문제가 없었다). 파일의 `\d`는 Python 문자열 안에서 `\\d`로 적었다.

- [ ] **Step 2: 실행하고 문법을 본다**

Run: `py -3 -I -X utf8 <scratch>\check-x.py` 뒤 `pwsh -NoProfile -Command "[scriptblock]::Create((Get-Content -Raw tools/research/ui-check.ps1)) | Out-Null; 'syntax ok'"`
Expected: `checks updated`, `syntax ok`.

- [ ] **Step 3: RED 회차**

Run(각각 따로, 백그라운드로 두고 출력 파일을 읽는다):
```
pwsh tools/DevRemote/session.ps1 -Begin ws-1017e -Fixture base
foreach part in base tabs items wrap actions confirm2 errors backup import-settle modbooks status: pwsh tools/research/ui-check.ps1 -Part <part> > <scratch>/red-<part>.txt 2>&1
pwsh tools/DevRemote/session.ps1 -End ws-1017e
```
Expected(RED): `base`는 `Tab1`의 `BagApply`는 있지만 `Tab14`가 없어 `Press`에서 멈춘다(exit 1). `tabs`는 'the currency tab is gone' FAIL 등. `items`·`wrap`는 `ResGroupCurrency`가 없어 멈춘다. `actions`는 `mbreak`가 UNKNOWN이라 'the broken martial is in the list' FAIL. `confirm2`는 'learn-all asks first' FAIL. `errors`는 `DevThrow`가 없어 멈춘다. `backup`은 `upopen`이 UNKNOWN이라 세 항목 FAIL(백업 폴더 수 그대로). `import-settle`은 `cimpsec` UNKNOWN으로 FAIL. `modbooks`는 'the first mod row has a name' FAIL(빈 이름). `status`는 `unload` UNKNOWN으로 'leaving the save turns everything off' FAIL. `-End`가 `SAVE RESTORED AND IDENTICAL`. 실패하지 않는 검사가 있으면 그 검사를 고친다(스펙 4.6절: RED에서 통과하는 백업 항목은 고정본 상태를 적고 검사를 바꾼다).

- [ ] **Step 4: 커밋**

```bash
git add tools/research/ui-check.ps1 tools/research/preset-check.ps1
git commit -m "test: 리뷰 수정 묶음(X)의 검사 — 탭 번호 표, 안전 단추, 화폐 그룹, 결과 칸 더 보기, 깨진 공법, 두 번 누르기, 예외 표시, 백업 세 곳, 가져오기 보충, 빈 이름 줄, 상태 되돌리기(RED ws-1017e)"
```

---

### Task 2: 창 — 탭 번호·탭 줄 46·안전 단추·결과 칸 더 보기·그리기 보호

**Files:**
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs`
- Modify: `src/ModCode/ModMain/UI/Ui.cs:150-170`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsUi.cs` (`uifail`, `uibuttons`)

**Interfaces:**
- Consumes: `EditorTab.Title/Render/Leave/OnShow/UsesTarget`(그대로).
- Produces: `EditorWindow.DevFailDraw`(DEVTOOLS), `tabs` 배열에서 `CurrencyTab`이 빠진다(Task 3이 지운다. 이 Task에서는 배열을 먼저 고치므로 Task 3 전까지 `CurrencyTab`은 쓰이지 않는 클래스로 남는다).

- [ ] **Step 1: EditorWindow 상수·필드**

`TitleH` 아래에 더한다:
```csharp
        // 탭 줄: 간격 46·단추 높이 40이면 18개가 결과 칸(908) 위에 들어간다(18번째의 아래 끝 64 + 17·46 + 40 = 886).
        public const float TabW = 150;
        private const float TabStep = 46;
        private const float TabH = 40;
        private const int MaxTabs = 18;
        // 안전 탭의 번호. 탭 줄이 아니라 제목 줄의 단추(Tab14)로 연다.
        private const int SafetyNumber = 14;
        // 더 보기로 펼쳤을 때 결과 칸이 더 커지는 높이
        private const float MessageMoreH = 300;
```
필드(`private static EditorTab[] tabs;` 근처)를 이렇게 바꾼다:
```csharp
        private static RectTransform titleRight;   // 제목 줄 오른쪽: 안전 단추(와 개발용 오류 단추)
        private static RectTransform messageArea;  // 결과 칸. 안의 글은 그릴 때마다 다시 놓는다
        private static Image messageBox;
        private static Text messageLabel;

        private static EditorTab[] tabs;
        // tabs[i]의 번호. 검증 스크립트의 Tab<번호>와 uistate의 tab=이 이 번호다(05 문서 47절). 2는 비었다(화폐 탭을 자원 탭의 그룹으로 합쳤다).
        private static int[] tabNumbers;
        private static int tab;             // 보이는 탭(배열 순번)
        private static int tabBefore;       // 안전을 열기 전의 탭(배열 순번)
        private static int safetyIndex;     // 안전 탭의 배열 순번
        private static bool messageExpanded;
        private static string lastDrawError;
#if DEVTOOLS
        public static bool DevFailDraw;     // 개발 전용: 다음 그리기에서 탭 그리기 앞에 시험 예외를 한 번 던진다
#endif
```
(기존 `private static Image messageBox; private static Text messageLabel;` 줄은 지운다.)

- [ ] **Step 2: Build**

`tabBar`·결과 칸·탭 배열을 바꾼다:
```csharp
            tabBar = Ui.Rect("Tabs", window, 16, ContentY, TabW, MaxTabs * TabStep);
            titleRight = Ui.Rect("TitleRight", window, Width - 360, 8, 160, 36);
            content = Ui.Rect("Content", window, ContentX, ContentY, ContentW, ContentH);
            // 결과 칸은 내용 영역보다 뒤에 만들어 펼쳤을 때 내용 위에 그려진다. 바탕은 창과 같은 색(불투명)이고 클릭을 받는다.
            messageArea = Ui.Panel("MessageBox", window, 16, Height - MessageH - 8, Width - 32, MessageH, Ui.Section);
            messageBox = messageArea.GetComponent<Image>();

            tabs = new EditorTab[]
            {
                new AttributesTab(), new ResourcesTab(), new VaultTab(), new GradeTab(), new MartialTab(), new ImmortalTab(), new ArtifactTab(), new LuckTab(),
                new ArtTab(), new NpcTab(), new WorldTab(), new RateTab(), new PresetTab(), new SafetyTab(), new AppellationTab(),
            };
            tabNumbers = new[] { 0, 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, SafetyNumber, 15 };
            safetyIndex = Array.IndexOf(tabNumbers, SafetyNumber);
```
(기존 `RectTransform box = Ui.Panel("MessageBox" …)`부터 `messageLabel.verticalOverflow …`까지 네 줄과 `new CurrencyTab(),`을 지운다.)

- [ ] **Step 3: Render·SwitchTab·안전 단추·DevState**

`Render`를 이렇게 바꾼다(탭 줄·제목 줄 오른쪽·결과 칸·보호):
```csharp
        private static void Render()
        {
            Confirm.OnRender();
            Ui.Clear(titleArea);
            Ui.Clear(titleRight);
            Ui.Clear(tabBar);
            Ui.Clear(content);
            int slot = 0;
            for (int i = 0; i < tabs.Length; i++)
            {
                if (i == safetyIndex)
                {
                    continue;   // 안전은 제목 줄의 단추
                }
                int index = i;
                float y = slot * TabStep;
                Ui.Button("Tab" + tabNumbers[i], tabBar, tabs[i].Title, 0, y, TabW, TabH, new Action(() => SwitchTab(index)), true, tab == i);
                if (tabs[i].UsesTarget)
                {
                    // 편집 대상(플레이어/NPC)이 적용되는 탭의 표시. 클릭은 아래의 버튼으로 통과시킨다.
                    Ui.Panel("TargetMark", tabBar, 0, y, 5, TabH, Ui.Warn).GetComponent<Image>().raycastTarget = false;
                }
                slot++;
            }
            bool safety = tab == safetyIndex;
            Ui.Button("Tab" + SafetyNumber, titleRight, safety ? "돌아가기" : "안전", 64, 0, 96, 36, new Action(ToggleSafety), true, safety);
#if DEVTOOLS
            // 개발 전용: 단추 콜백의 예외가 결과 칸에 보이는지 검사한다(ui-check.ps1의 errors).
            Ui.Button("DevThrow", titleRight, "오류", 0, 0, 56, 36, new Action(() => { throw new InvalidOperationException("시험 예외"); }));
#endif
            DrawMessage();
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
            try
            {
#if DEVTOOLS
                if (DevFailDraw)
                {
                    DevFailDraw = false;
                    throw new InvalidOperationException("시험 예외");
                }
#endif
                tabs[tab].Render(content);
            }
            catch (Exception e)
            {
                // 탭 그리기의 예외는 화면에 아무 반응이 없고 로그에만 남았다(리뷰 C2). 내용 영역에 알리고, 같은 글이 이어지면 한 번만 로그에 남긴다.
                Ui.Clear(content);
                string text = "이 탭을 그리지 못했습니다: " + e.GetType().Name + ": " + e.Message;
                Text error = Ui.Label(content, text, 0, 0, ContentW, 40, 19);
                error.color = Ui.Warn;
                error.gameObject.name = "DrawError";
                if (lastDrawError != text)
                {
                    lastDrawError = text;
                    MelonLogger.Msg("[TlToybox] 탭 오류 " + tabs[tab].Title + ": " + e.GetType().Name + ": " + e.Message);
                }
            }
        }

        // 제목 줄의 안전 단추: 안전 탭을 보이거나, 보고 있으면 열기 전의 탭으로 돌아간다.
        private static void ToggleSafety()
        {
            if (tab == safetyIndex)
            {
                SwitchTab(tabBefore);
                return;
            }
            tabBefore = tab;
            SwitchTab(safetyIndex);
        }

        // 결과 칸. 글이 칸(세 줄)을 넘으면 "…"과 더 보기를 붙인다. 펼치면 칸이 위로 커져 내용 위에 덮이고 안에서 스크롤한다(리뷰 U3).
        private static void DrawMessage()
        {
            Ui.Clear(messageArea);
            float boxW = Width - 32;
            float textW = boxW - 24 - 110;   // 오른쪽의 더 보기 단추 자리
            float innerH = MessageH - 8;
            bool overflow = message.Length > 0 && Ui.TextHeight(message, textW, 18) > innerH;
            if (!overflow)
            {
                messageExpanded = false;
            }
            float boxH = messageExpanded ? MessageH + MessageMoreH : MessageH;
            messageArea.anchoredPosition = new Vector2(16, -(Height - boxH - 8));
            messageArea.sizeDelta = new Vector2(boxW, boxH);
            if (messageExpanded)
            {
                float needed = Ui.TextHeight(message, textW, 18) + 12;
                Transform area = Ui.Scroll("MessageFull", messageArea, 0, 0, boxW - 110, boxH, needed);
                Text full = Ui.Wrapped(area, message, 12, 4, textW, needed, 18, TextAnchor.UpperLeft);
                full.gameObject.name = "MessageFullText";
                full.color = Ui.Warn;
            }
            else
            {
                messageLabel = Ui.Label(messageArea, message, 12, 4, overflow ? textW : boxW - 24, innerH, 18, TextAnchor.UpperLeft);
                messageLabel.color = Ui.Warn;
                messageLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                messageLabel.verticalOverflow = VerticalWrapMode.Truncate;
                if (overflow)
                {
                    Ui.Label(messageArea, "…", boxW - 110 - 24, innerH - 24, 24, 24, 18).gameObject.name = "MessageEllipsis";
                }
            }
            if (overflow)
            {
                Ui.Button("MessageMore", messageArea, messageExpanded ? "접기" : "더 보기", boxW - 102, 6, 90, 30,
                    new Action(() => { messageExpanded = !messageExpanded; Render(); }));
            }
        }
```
`Say`에 `messageExpanded = false;`를, `Close`에 `messageExpanded = false;`를 더한다. `DevState`의 `" tab=" + tab`을 `" tab=" + tabNumbers[tab]`로 바꾼다. `SetCollapsed`는 그대로(`messageBox.gameObject`).

- [ ] **Step 4: Ui.Button의 콜백 보호**

`src/ModCode/ModMain/UI/Ui.cs`에 `using MelonLoader;`를 더하고 `Button`의 `AddListener`를 바꾼다:
```csharp
            if (enabled && onClick != null)
            {
                // 콜백의 예외는 화면에 아무 반응이 없고 로그의 trampoline 줄에만 남았다(리뷰 C2). 결과 칸에 보이고 로그에 한 줄 남긴다.
                button.onClick.AddListener(new Action(() =>
                {
                    try
                    {
                        onClick();
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Msg("[TlToybox] 버튼 오류 " + name + ": " + e.GetType().Name + ": " + e.Message);
                        EditorWindow.Say("오류: " + e.GetType().Name + ": " + e.Message);
                    }
                }));
            }
```

- [ ] **Step 5: 개발 명령 `uifail`·`uibuttons`**

`DevCommandsUi.Execute`의 `case "uiclose":` 앞에:
```csharp
                case "uifail":
                    EditorWindow.DevFailDraw = true;
                    return "next render throws";
                case "uibuttons":
                    return Buttons(p[1]);
```
`TextsOf` 아래에:
```csharp
        // 그 접두사로 시작하는 단추마다 켜짐 여부와 글. 검사가 "켜진 첫 후보"를 고를 때 쓴다.
        private static string Buttons(string prefix)
        {
            GameObject root = GameObject.Find(EditorName);
            if (root == null)
            {
                return "editor is not open";
            }
            var sb = new StringBuilder();
            foreach (Button button in root.GetComponentsInChildren<Button>(false))
            {
                if (button.gameObject.name.StartsWith(prefix))
                {
                    Text label = button.GetComponentInChildren<Text>();
                    sb.Append(button.gameObject.name).Append(button.interactable ? " on" : " off")
                        .Append(" text='").Append(label == null ? "" : label.text.Replace("\n", " ")).Append("'\n");
                }
            }
            return sb.Length == 0 ? "no button starting with " + prefix : sb.ToString();
        }
```
머리말 주석에 `uifail / uibuttons <접두>` 한 줄을 더한다.

- [ ] **Step 6: 빌드(두 구성)**

Run: `dotnet build src/ModCode/ModMain/ModMain.csproj -c Debug 2>&1 | tail -3` 와 `-c Release`
Expected: 둘 다 `경고 0`·`오류 0`. (`CurrencyTab`은 아직 있으나 쓰이지 않는다. 경고가 나면 Task 3에서 지우므로 그때 본다.)

- [ ] **Step 7: 커밋**

```bash
git add src/ModCode/ModMain/UI/EditorWindow.cs src/ModCode/ModMain/UI/Ui.cs src/ModCode/ModMain/Dev/DevCommandsUi.cs
git commit -m "feat: 탭 번호 고정·탭 줄 46 간격·안전 단추 Tab14·결과 칸 더 보기·단추와 탭 그리기의 예외 표시(리뷰 2·4·8)"
```

---

### Task 3: 자원 탭의 화폐 그룹, 영석 줄 제거, 화폐 탭 삭제

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs`
- Delete: `src/ModCode/ModMain/UI/Tabs/CurrencyTab.cs`

**Interfaces:**
- Consumes: `ItemCatalog.Currencies`(`List<ItemCatalog.Entry>`, `Group`·`Kind`는 null), `EditService.Money/AddMoney/GiveItem/ItemName/SetAttribute`, `AttributeCatalog.All`, `AmountBar.Get("item")`.
- Produces: 그룹 키 `CurrencyKey = "Currency"`, 요소 `ResGroupCurrency`·`CurrencyOwned<id>`·`CurrencyAdd<id>`·`CurrencyOwnedFame`·`CurrencyAddFame`.

- [ ] **Step 1: 상수와 그룹**

`TopH` 주석과 값: `private const float TopH = 48;                  // 지급 수량과 가방 칸 한 줄(영석 줄은 화폐 그룹으로 갔다. 05 문서 47절)`. `GroupStep` 주석을 "열다섯 개가 아래 설명 칸 위에 들어간다(56 + 14·42 + 38 = 682 < 686)"로. `ManualKey` 옆(Books.cs가 아니라 ResourcesTab.cs의 `RowH` 위)에:
```csharp
        private const string CurrencyKey = "Currency";   // 화폐 그룹: 영석과 재화(ItemCatalog.Currencies), 명성 줄. 05 문서 47절
```
`IsFav` 아래에:
```csharp
        private bool IsCurrency
        {
            get { return groupKey == CurrencyKey; }
        }
```
`DrawGroups`의 `foreach` 뒤에:
```csharp
            GroupButton(CurrencyKey, "화폐", ItemCatalog.Currencies.Count, y);
```
`OnQuery`의 `else if (label.Key == ManualKey)` 뒤에:
```csharp
                else if (label.Key == CurrencyKey)
                {
                    label.Set(ItemCatalog.Currencies.Count);
                }
```

- [ ] **Step 2: 윗줄(영석 줄 제거)**

`DrawTop`을 이렇게 바꾼다(2줄이 1줄로 올라온다. x 배치는 그대로):
```csharp
        private void DrawTop(WorldUnitBase player)
        {
            Ui.Panel("MoneyBox", content, 0, 0, ContentW, TopH, Ui.Section);
            // 아이템 지급 수량과 가방 칸. 가방 칸은 편집기가 늘린 만큼만 되돌릴 수 있다. 영석은 화폐 그룹의 첫 줄이다.
            Ui.Label(content, "지급 수량", 12, 4, 110, 40, 18);
            float itemRight = AmountBar.Build(content, "item", 122, 5);
            int bagExtra = BagService.Extra;
            bagInput = bagExtra.ToString();
            float bagX = itemRight + 16;
            Ui.Label(content, BagService.Describe(), bagX, 4, 296, 40, 17);
            float bx = bagX + 300;
            NumberField.Build(content, "BagExtra", bx, 5, 76, 38, bagInput, new Action<string>(v => bagInput = v), new Action<int>(SetBagExtra));
            Ui.Button("BagApply", content, "적용", bx + 80, 5, 60, 38, new Action(() =>
            {
                int value;
                if (!int.TryParse(bagInput, out value))
                {
                    Say("가방: 추가 칸을 숫자로 입력하세요.");
                    return;
                }
                SetBagExtra(value);
            }));
            Ui.Button("BagMinus", content, "-" + BagService.Step, bx + 144, 5, 60, 38, new Action(() => Say(BagService.Change(-BagService.Step))), bagExtra > 0);
            Ui.Button("BagPlus", content, "+" + BagService.Step, bx + 208, 5, 60, 38, new Action(() => Say(BagService.Change(BagService.Step))), bagExtra < BagService.MaxExtra);
        }
```
파일 머리 주석의 "영석, 지급 수량과 가방 칸"을 "지급 수량과 가방 칸, 화폐 그룹(영석·재화·명성)"으로.

- [ ] **Step 3: 줄·칩·거르기·머리·설명**

`Rows`의 `if (groupKey == ManualKey)` 뒤에:
```csharp
            if (IsCurrency)
            {
                // 화폐 그룹은 거르기(검색·품질·보유 중만)를 쓰지 않는다. 다른 그룹에서 남긴 검색어가 여기 줄을 숨기지 않게.
                var currencies = new List<Row>();
                foreach (ItemCatalog.Entry entry in ItemCatalog.Currencies)
                {
                    currencies.Add(new Row { Item = entry });
                }
                return currencies;
            }
```
`Draw`의 `y = groupKey == ManualKey ? DrawBookFilters(y) : DrawFilters(y);`를 `y = groupKey == ManualKey ? DrawBookFilters(y) : IsCurrency ? y : DrawFilters(y);`로. `list.EmptyText`는 그대로.

`DrawKinds`의 맨 앞(`if (groupKey == ManualKey)` 앞)에:
```csharp
            if (IsCurrency)
            {
                return DrawFameLine(y);
            }
```
`DrawKinds` 뒤에:
```csharp
        // 화폐 그룹의 명성 줄. 명성은 아이템이 아니라 인물의 속성(저장값 reputation)이다. 낭야각 같은 상점의 "명성 N 도달 필요"에 쓰이는 값과 같아
        // (실측: 화면의 80 = 저장값 80) 재화와 함께 올릴 수 있게 여기 둔다. 쓰기는 능력치 탭과 같은 경로다.
        private float DrawFameLine(float y)
        {
            AttributeDef fame = AttributeCatalog.All.Find(d => d.Label == "명성");
            if (fame == null)
            {
                return y;
            }
            int amount = AmountBar.Get("item");
            int have = fame.Get(Player.data.unitData.propertyData);
            Ui.Label(content, "속성 명성", RightX, y, 110, 38, 18);
            Ui.Label(content, "보유", RightX + 110, y, 44, 38, 16).color = Ui.Dim;
            Ui.Label(content, have.ToString(), RightX + 154, y, 100, 38, 18).gameObject.name = "CurrencyOwnedFame";
            Ui.Button("CurrencyAddFame", content, "+ " + amount, RightX + 260, y + 2, 120, 34, new Action(() =>
                Say(EditService.SetAttribute(Player, fame, fame.Get(Player.data.unitData.propertyData) + amount))));
            Ui.Label(content, "인물의 속성입니다(능력치 탭 > 개인 속성의 명성과 같은 값). 상점의 \"명성 N 도달 필요\" 조건에 쓰입니다. 아래 재화는 가방 칸을 차지하지 않습니다.",
                RightX + 392, y, RightW - 392, 38, 14).color = Ui.Dim;
            return y + 44;
        }
```
`DrawHead`를 화폐 그룹에 맞춘다(☆·품질 열 이름을 비운다):
```csharp
        private void DrawHead(float y)
        {
            Ui.Panel("ResHead", content, RightX, y, RightW, 30, Ui.Section);
            if (!IsCurrency)
            {
                Ui.Label(content, "☆", RightX + ColStar, y, 40, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            }
            Ui.Label(content, IsCurrency ? "화폐" : "아이템명", RightX + ColName, y, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "종류", RightX + ColKind, y, KindW, 30, 16).color = Ui.Dim;
            if (!IsCurrency)
            {
                Ui.Label(content, "품질", RightX + ColLevel, y, 44, 30, 16).color = Ui.Dim;
            }
            Ui.Label(content, "보유", RightX + ColOwned, y, 60, 30, 16).color = Ui.Dim;
            if (IsFav)
            {
                Ui.Label(content, "수량", RightX + ColAmount, y, AmountW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            }
            Ui.Label(content, "지급", RightX + ColGive, y, GiveW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
        }
```
`DrawRow`의 `if (r.Item != null)` 가지를 `if (r.Item != null) { if (IsCurrency) { DrawCurrencyRow(row, r.Item, h); } else { DrawItemRow(row, r.Item, h); } }`로. `DrawItemRow` 뒤에:
```csharp
        // 화폐 줄: ☆ 없음(즐겨찾기는 가방 아이템의 것), 종류 "화폐", 품질 없음, 보유는 영석이면 EditService.Money. 지급은 CurrencyTab(지운 탭)의 Add와 같다.
        private void DrawCurrencyRow(RectTransform row, ItemCatalog.Entry entry, float h)
        {
            int id = entry.Id;
            WrapLabel(Ui.Button("ItemName" + id, row, entry.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(Detail(entry)))));
            Ui.Wrapped(row, "화폐", ColKind, 0, KindW, h, 16);
            Ui.Label(row, "-", ColLevel, 0, 44, h, 16).color = Ui.Dim;
            int have = id == PropsIDType.Money ? EditService.Money(Player) : Player.data.unitData.propData.GetPropsNum(id);
            Ui.Label(row, have.ToString(), ColOwned, 0, 60, h, 16).gameObject.name = "CurrencyOwned" + id;
            int amount = AmountBar.Get("item");
            Ui.Button("CurrencyAdd" + id, row, "+ " + amount, ColGive, 2, GiveW, RowH - 4, new Action(() => Say(AddCurrency(entry, amount))));
        }

        private static string AddCurrency(ItemCatalog.Entry entry, int amount)
        {
            if (entry.Id == PropsIDType.Money)
            {
                return EditService.AddMoney(Player, amount);
            }
            string result = EditService.GiveItem(Player, entry.Id, amount);
            // 이름의 번역이 없는 재화는 결과 문장에 번역 열쇠가 나온다. 목록에 보인 이름으로 바꾼다.
            string raw = EditService.ItemName(entry.Id);
            return string.IsNullOrEmpty(raw) || raw == entry.Name ? result : result.Replace(raw, entry.Name);
        }
```
`Detail(entry)`: `if (entry.Missing)` 뒤에 화폐(그룹이 없는 항목)를 더한다:
```csharp
            if (entry.Group == null)
            {
                return "[" + entry.Id + "] " + entry.Name + "  (화폐, 품질 " + entry.Level + ")\n" + entry.Desc;
            }
```
`KindOf(Row)`의 `row.Item.Kind == null ? "-"`를 `row.Item.Kind == null ? (row.Item.Group == null ? "화폐" : "-")`로(줄 높이 계산용).

- [ ] **Step 4: 화폐 탭 삭제**

Run: `git rm src/ModCode/ModMain/UI/Tabs/CurrencyTab.cs` 뒤 `grep -rn "CurrencyTab\|AmountBar.Get(\"money\")\|\"money\"" src/ModCode/ModMain --include=*.cs`
Expected: 매치 없음(`EditorWindow`는 Task 2에서 뺐다).

- [ ] **Step 5: 빌드(두 구성)**

Run: `dotnet build … -c Debug`, `-c Release`
Expected: 경고 0, 오류 0.

- [ ] **Step 6: 커밋**

```bash
git add -A src/ModCode/ModMain/UI/Tabs/
git commit -m "feat: 화폐 탭을 자원 탭의 화폐 그룹으로(아이템 표의 줄, 명성 줄), 윗줄의 영석 줄을 빼고 가방 줄이 그 자리로(리뷰 2·24)"
```

---

### Task 4: GREEN 회차 1 — 창과 자원 탭

**Files:** 없음(검증만). 실패하면 Task 2·3의 코드를 고치고 다시 빌드한다(검사를 고치지 않는다. 검사가 틀렸으면 그 이유를 적고 고친다).

- [ ] **Step 1: 회차 `ws-1017f`(고정본)**

Run: `-Begin ws-1017f -Fixture base` → `ui-check.ps1 -Part base|tabs|items|wrap|errors|history|confirm` → `preset-check.ps1 -Part ui` → `-End ws-1017f`
Expected: 모두 `FAILS=0`, exit 0. `tabs`의 캡처 `check-tabs.png`·`check-long-message.png`, `wrap`의 `check-wrap-currency.png`를 Read로 보고 겹침·잘림이 없는지 본다. `-End`가 `SAVE RESTORED AND IDENTICAL`.
실측해 적어 둘 것(05 문서 47절·인계 5절용): `Pos Tab0`·`Tab1`의 y, `Pos Tab14`의 x·y.

- [ ] **Step 2: 실패가 있으면 고치고 `ws-1017f2`로 다시 돈다. 결과를 05 문서 47절 초안(스크래치 메모)에 적는다.**

---

### Task 5: 공법 탭의 줄 보호(C1)와 `mbreak`

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/MartialTab.cs:64-174`
- Modify: `src/ModCode/ModMain/Services/MartialService.cs:634-652` (`Forget`)
- Modify: `src/ModCode/ModMain/Dev/DevCommandsChar.cs` (`mbreak`)

**Interfaces:**
- Consumes: `MartialService.Martial/Name/TypeLabel/Prefixes/Forget`.
- Produces: 요소 `MartialBroken<순번>`, `MartialBrokenNote`; 개발 명령 `mbreak`(`sole=<고유 번호> martials=<수>`).

- [ ] **Step 1: 줄 그리기**

`DrawMartialRow`를 바꾼다:
```csharp
        private void DrawMartialRow(RectTransform row, Learned learned, int index)
        {
            bool picked = learned.Id == martialId;
            string name, type, grade;
            bool broken = false;
            try
            {
                DataProps.MartialData martial = learned.Data.data.To<DataProps.MartialData>();
                name = MartialService.Name(martial);
                type = MartialService.TypeLabel(martial.martialType);
                grade = martial.grade.ToString();
            }
            catch (Exception e)
            {
                // 게임이 알아보지 못하는 값으로 만든 공법은 읽는 것만으로 예외가 난다(인계 6절). 줄은 남겨 선택·망각으로 치울 수 있게 한다(리뷰 C1).
                broken = true;
                name = "(읽을 수 없음) " + e.GetType().Name;
                type = "-";
                grade = "-";
            }
            Text nameLabel = Ui.Label(row, name, 8, 0, 320, 38);
            if (broken)
            {
                nameLabel.color = Ui.Warn;
                nameLabel.gameObject.name = "MartialBroken" + index;
            }
            Ui.Label(row, type, 338, 0, 120, 38);
            Ui.Label(row, grade, 468, 0, 80, 38);
            Ui.Label(row, learned.Data.exp.ToString(), 558, 0, 150, 38);
            Ui.Button("PickMartial" + index, row, picked ? "선택됨" : "선택", 728, 3, 110, 32, new Action(() =>
            {
                martialId = learned.Id;
                optionDetail.Clear();
                Redraw();
            }), true, picked);
            Ui.Label(row, MartialService.IsEquipped(Target, learned.Id) ? "장착 중" : "", 848, 0, 120, 38, 17).color = Ui.Warn;
        }
```

- [ ] **Step 2: 선택한 공법의 상자**

`Draw`에서 `DataProps.MartialData selected = …` 뒤를 이렇게 바꾼다:
```csharp
            DataProps.MartialData selected = martialId == null ? null : MartialService.Martial(target, martialId);
            if (selected == null)
            {
                martialId = null;
            }
            // 선택한 공법의 이름과 옵션. 깨진 공법이면 읽지 못한 이유만 남긴다.
            string selectedTitle = null;
            string selectedBroken = null;
            var prefixes = new List<MartialService.PrefixView>();
            if (selected != null)
            {
                try
                {
                    selectedTitle = MartialService.Name(selected) + "  (" + MartialService.TypeLabel(selected.martialType) + ")";
                    prefixes = MartialService.Prefixes(selected);
                }
                catch (Exception e)
                {
                    selectedBroken = e.GetType().Name;
                }
            }
```
(기존 `List<MartialService.PrefixView> prefixes = selected == null ? … : MartialService.Prefixes(selected);` 줄은 지운다.) `Ui.Panel("MartialBox", …)` 다음의 `if (selected == null)` 블록 뒤에:
```csharp
            if (selectedBroken != null)
            {
                Text note = Ui.Label(content, "읽을 수 없는 공법입니다(" + selectedBroken + "). 기술 망각으로 지울 수 있습니다.", 16, box + 10, 880, 36);
                note.color = Ui.Warn;
                note.gameObject.name = "MartialBrokenNote";
                ForgetButton(box + 52);
                return;
            }
            Ui.Label(content, selectedTitle, 16, box + 6, 420, 36, 21);
```
(기존 `Ui.Label(content, MartialService.Name(selected) + "  (" + … , 21);` 줄은 지운다.) 맨 끝의 `Confirm.Button("MartialForget", …)`를 `ForgetButton(box + 286 + grown);`로 바꾸고 메서드를 더한다:
```csharp
        // 망각은 한 번 더 확인한다. 깨진 공법도 고유 번호만으로 지운다.
        private void ForgetButton(float y)
        {
            Confirm.Button("MartialForget", content, "기술 망각", "정말 망각", 176, y, 150, 34, "forget:" + martialId,
                "이 공법을 익힌 목록에서 지웁니다. 계속하려면 \"정말 망각\"을 한 번 더 누르세요.", new Action(() =>
                {
                    string result = MartialService.Forget(Target, martialId);
                    martialId = null;
                    optionDetail.Clear();
                    Say(result);
                }));
        }
```

- [ ] **Step 3: `Forget`의 이름 읽기와 삭제 보호**

`MartialService.Forget`:
```csharp
            string name;
            try
            {
                name = Name(action.data.To<DataProps.MartialData>());
            }
            catch (Exception e)
            {
                name = "(읽을 수 없음 " + e.GetType().Name + ")";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            try
            {
                unit.CreateAction(new UnitActionMartialDel(action), false);
            }
            catch (Exception)
            {
                // 깨진 공법은 게임의 삭제 행동이 읽다가 예외를 낼 수 있다. 목록에서 직접 뺀다.
                data.DelActionMartial(soleId);
            }
```

- [ ] **Step 4: `mbreak`**

`DevCommandsChar.Execute`의 `case "mdel":` 앞에(파일에 `using UnhollowerBaseLib;`를 더한다):
```csharp
                case "mbreak":
                    {
                        // 배운 공법 하나의 값을 복사해 기술 번호(머리 세 칸 뒤의 두 번째)를 없는 수로 바꾼 공법을 넣는다.
                        // 읽는 것만으로 예외가 나는 "깨진 공법"을 만들어 공법 탭의 보호(05 문서 48절)를 검사한다.
                        var data = me.data.unitData;
                        DataUnit.ActionMartialData source = null;
                        foreach (var pair in data.allActionMartial)
                        {
                            source = pair.Value;
                            break;
                        }
                        if (source == null)
                        {
                            return "no martial";
                        }
                        Il2CppStructArray<int> from = source.data.values;
                        var tail = new Il2CppStructArray<int>(from.Length - 3);
                        for (int i = 3; i < from.Length; i++)
                        {
                            tail[i - 3] = from[i];
                        }
                        tail[1] = 999999999;
                        DataProps.PropsData props = DataProps.PropsData.New(0, 1, DataProps.PropsDataType.Martial, tail);
                        data.StudyActionMartialProp(props);
                        return "sole=" + props.soleID + " martials=" + data.allActionMartial.Count;
                    }
```

- [ ] **Step 5: 빌드(두 구성), 커밋**

Expected: 경고 0.
```bash
git add src/ModCode/ModMain/UI/Tabs/MartialTab.cs src/ModCode/ModMain/Services/MartialService.cs src/ModCode/ModMain/Dev/DevCommandsChar.cs
git commit -m "fix: 공법 탭이 깨진 공법 줄을 \"(읽을 수 없음)\"으로 그리고 망각으로 지운다, 개발 명령 mbreak(리뷰 1)"
```

---

### Task 6: 두 번 누르기(U2)

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/LuckTab.cs` (`OpenPicker`, `DrawPickRow`, `DrawFateRow`)
- Modify: `src/ModCode/ModMain/UI/Tabs/MartialTab.cs:210`
- Modify: `src/ModCode/ModMain/UI/Tabs/ArtTab.cs:49`
- Modify: `src/ModCode/ModMain/Services/FateService.cs:32` (`GradeName`을 internal로)

- [ ] **Step 1: 운명 탭**

필드에 `private string replaceName = "";   // 교체할 선천운명의 이름(확인 안내에 쓴다)`를 더하고 `OpenPicker`를:
```csharp
        private void OpenPicker(int bornIndex)
        {
            replaceIndex = bornIndex;
            LuckService.LuckView old = bornIndex < 0 ? null : LuckService.Owned(Target).Find(v => v.Born && v.BornIndex == bornIndex);
            replaceName = old == null ? "선천운명 " + (bornIndex + 1) + "번" : old.Name;
            pickList.Reset();
            pickDetail.Clear();
            Push(() => (replaceIndex >= 0 ? "선천운명 " + (replaceIndex + 1) + "번 교체 — " : "운명 추가 — ") + EditService.NameOf(Target), DrawPicker);
        }
```
`DrawPickRow`의 `string label = …`부터 끝까지를:
```csharp
            float x = ContentW - Ui.ScrollBarWidth - 198;
            if (replacing)
            {
                // 선천운명 교체는 게임이 붙인 것을 뗀다. 두 번 눌러 확인한다(리뷰 U2).
                Confirm.Button("LuckAdd" + view.Id, row, has ? "보유 중" : "이것으로 교체", "정말 교체", x, 7, 190, 36, "luck-replace:" + view.Id,
                    "선천운명 " + replaceName + "을(를) 떼고 " + view.Name + "(으)로 바꿉니다. 한 번 더 누르면 실행합니다.", new Action(() =>
                    {
                        string result = LuckService.ReplaceBorn(Target, replaceIndex, view.Id);
                        CloseViews();
                        Say(result);
                    }), !has);
                return;
            }
            string label = has ? "보유 중" : view.Type == 1 ? "선천운명에 추가" : view.Type == 2 ? "후천운명에 추가" : "추가";
            Ui.Button("LuckAdd" + view.Id, row, label, x, 7, 190, 36, new Action(() =>
                Say(view.Type == 1 ? LuckService.AddBorn(Target, view.Id) : LuckService.Add(Target, view.Id))), !has);
```
`DrawFateRow`의 `Ui.Button("FatePick" …)`를:
```csharp
            // 정하면 이전 역천개명의 효과가 꺼진다. 두 번 눌러 확인한다(리뷰 U2).
            Confirm.Button("FatePick" + view.Id, row, has ? "보유 중" : "이것으로 정하기", "정말 정하기", ContentW - Ui.ScrollBarWidth - 198, 7, 190, 36, "fate-pick:" + view.Id,
                FateService.GradeName(grade) + "의 역천개명을 " + view.Name + "(으)로 정합니다. 이전 것의 효과는 꺼집니다. 한 번 더 누르면 실행합니다.", new Action(() =>
                {
                    string result = FateService.Set(Target, grade, view.Id);
                    Pop();
                    Say(result);
                }), !has);
```
`FateService.cs:32`의 `private static string GradeName(int grade)`를 `internal static string GradeName(int grade)`로.

- [ ] **Step 2: 모두 배우기 둘**

`MartialTab.cs:210`:
```csharp
            Confirm.Button("SkillLearnAll", content, SkillCatalog.Kinds[skillKind].Tab + " 모두 배우기", "정말 모두 배우기", 580, y + 46, 240, 36, "skill-learn-all",
                SkillCatalog.Kinds[skillKind].Tab + "의 배울 수 있는 기술을 모두 넣습니다(하나씩 망각해야 되돌립니다). 한 번 더 누르면 실행합니다.",
                new Action(() => Say(SkillCatalog.LearnAll(Target, skillKind, skillGrade))));
```
`ArtTab.cs:49`:
```csharp
            Confirm.Button("ArtLearnAll", content, kind.Tab + " 모두 배우기", "정말 모두 배우기", 600, 50, 220, 38, "art-learn-all",
                kind.Tab + "의 기예를 모두 배웁니다(하나씩 잊어야 되돌립니다). 한 번 더 누르면 실행합니다.",
                new Action(() => Say(ArtService.LearnAll(ArtService.Kinds[artKind]))));
```

- [ ] **Step 3: 빌드(두 구성), 커밋**

```bash
git add src/ModCode/ModMain/UI/Tabs/LuckTab.cs src/ModCode/ModMain/UI/Tabs/MartialTab.cs src/ModCode/ModMain/UI/Tabs/ArtTab.cs src/ModCode/ModMain/Services/FateService.cs
git commit -m "fix: 선천운명 교체·역천개명 정하기·기술 모두 배우기·기예 모두 배우기를 두 번 눌러 확인(리뷰 3)"
```

---

### Task 7: 세이브를 떠날 때 상태 되돌리기(C3)

**Files:**
- Modify: `src/ModCode/ModMain/ModMain.cs:30-41`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsTime.cs` (`unload`)

- [ ] **Step 1: `ResetState`**

```csharp
        // 주 화면으로 돌아갈 때 호출되며, 이후 Init이 다시 호출된다.
        public void Destroy()
        {
            MelonLogger.Msg("[TlToybox] Destroy");
            EditorWindow.Close();
            ResetState();
            BattleService.OnDestroy();
            RateService.OnDestroy();
            ArtifactKeyService.OnDestroy();
            InputGuard.Release();
#if DEVTOOLS
            StopDevPoll();
#endif
        }

        // 세이브를 떠날 때 켜 둔 것(날짜 고정·배율·무적)을 끈다. 다음 세이브가 그 상태로 시작하지 않게(리뷰 C3). 이력에 남는다.
        // 예외가 나도 뒤의 정리(OnDestroy)는 돈다.
        public static void ResetState()
        {
            try
            {
                if (TimeService.Frozen)
                {
                    TimeService.SetFrozen(false);
                }
                RateService.Reset();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 상태 되돌리기 오류: " + e.GetType().Name + ": " + e.Message);
            }
        }
```
`DevCommandsTime.Execute`의 `case "tfreeze":` 앞에:
```csharp
                case "unload":
                    // Destroy가 부르는 상태 되돌리기만 부른다(창은 두고). 검사 status가 쓴다.
                    ModMain.ResetState();
                    return "reset frozen=" + TimeService.Frozen + " god=" + BattleService.God + " mul=" + BattleService.Multiplier;
```

- [ ] **Step 2: 빌드(두 구성), 커밋**

```bash
git add src/ModCode/ModMain/ModMain.cs src/ModCode/ModMain/Dev/DevCommandsTime.cs
git commit -m "fix: Destroy에서 날짜 고정·배율·무적을 끈다, 개발 명령 unload(리뷰 5·20)"
```

---

### Task 8: 가져오기의 경지 구역 뒤 보충(C4)

**Files:**
- Modify: `src/ModCode/ModMain/Services/CharacterExportService.Import.cs:141-200`
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPreset.cs` (`cimpsec`)

- [ ] **Step 1: `PutGrade`**

`bool gradeOk = …; bool expOk = …;` 뒤, `string after = …` 앞에:
```csharp
            // 경지에 따라 빠져 있는 것(선력·등선 진입 선법·칭호 재판정)을 채운다. 경지 올리기(35절)와 같다(리뷰 C4).
            string settled = gradeOk && expOk ? GradeService.Settle(me) : null;
```
마지막 `text = "경지: " + … + " 능력치는 따라오지 않으니 능력치도 가져오세요";`를 `… + "(으)로 바꿨습니다. 능력치는 따라오지 않으니 능력치도 가져오세요" + (settled == null ? "" : " " + settled);`로(앞부분은 그대로).

- [ ] **Step 2: `cimpsec`**

`DevCommandsPreset.Execute`의 `case "cimpfates":` 앞에(파일에 `using System;`이 없으면 더한다):
```csharp
                case "cimpsec":
                    {
                        // 그 이름의 캐릭터 내보내기 파일에서 한 구역만 가져온다(grade·attributes·lucks·fates·martials·arts). 검사 import-settle이 쓴다.
                        var section = (CharacterExportService.Section)Enum.Parse(typeof(CharacterExportService.Section), p[2], true);
                        foreach (CharacterExportService.Pack pack in CharacterExportService.List())
                        {
                            if (pack.Name == p[1] && pack.Error == null)
                            {
                                return CharacterExportService.ImportSection(pack, section);
                            }
                        }
                        return "no pack " + p[1];
                    }
```

- [ ] **Step 3: 빌드(두 구성), 커밋**

```bash
git add src/ModCode/ModMain/Services/CharacterExportService.Import.cs src/ModCode/ModMain/Dev/DevCommandsPreset.cs
git commit -m "fix: 캐릭터 가져오기의 경지 구역 뒤에 경지에 따른 보충(선력·선법·칭호), 개발 명령 cimpsec(리뷰 6)"
```

---

### Task 9: 백업 빠진 세 곳(C5)과 `verified`

**Files:**
- Modify: `src/ModCode/ModMain/Services/EditService.cs:76-95,331-346`
- Modify: `src/ModCode/ModMain/Services/GradeService.cs:248-256`
- Modify: `src/ModCode/ModMain/Services/FateService.cs:186-210`
- Modify: `src/ModCode/ModMain/Safety/SaveBackup.cs` (`DevReset`)
- Modify: `src/ModCode/ModMain/Dev/DevCommandsChar.cs` (`bkreset`, `upopen`, `fateclash`, `fateamong`)

- [ ] **Step 1: 세 곳**

`EditService.OpenGameBreakthroughScreen`의 `RemoveStaleBottleneck(unit);` 앞에:
```csharp
            // 난관 기운 지우기는 운명 삭제(쓰기)다. 다른 경로(FillExp·Breakthrough)처럼 먼저 백업한다(리뷰 C5).
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 화면을 열지 않았습니다.";
            }
```
`GradeService.Settle`의 `DropTitles(unit);`부터 끝까지를:
```csharp
            // 칭호 재판정은 칭호를 지울 수 있다. 채울 것이 없어도 그 앞에 백업한다(리뷰 C5).
            string titleBackup = SaveBackup.EnsureOnce();
            if (titleBackup == null)
            {
                DropTitles(unit);
            }
            string skipped = titleBackup == null ? "" : " " + titleBackup + " 칭호를 재판정하지 않았습니다.";
            if (done.Count == 0)
            {
                return skipped.Length == 0 ? null : skipped.Trim();
            }
            string text = "경지에 따라 빠져 있던 것을 채웠습니다: " + string.Join(", ", done.ToArray()) + "." + skipped;
            ChangeLog.Add(EditService.NameOf(unit), "경지에 따른 보충", "-", string.Join(", ", done.ToArray()), true);
            return text;
```
`FateService.SetAmong`의 `var emptied = …` 앞에:
```csharp
            // 다른 칸 비우기(Release)는 운명 삭제(쓰기)다. Set의 백업보다 앞서므로 여기서 먼저 백업한다(리뷰 C5).
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
```
`EditService.RestoreResources`: `ChangeLog.Add(…, true);` 앞에 재조회를 넣고 `true`를 `verified`로:
```csharp
            bool verified = saved.hp == dyn.hpMax.value && saved.mp == dyn.mpMax.value && saved.sp == dyn.spMax.value
                && saved.health == dyn.healthMax.value && saved.energy == dyn.energyMax.value && saved.mood == dyn.moodMax.value;
            ChangeLog.Add(NameOf(unit), "자원 회복(체력/영력/염력/건강/정력/심정)", before, after, verified);
```

- [ ] **Step 2: `DevReset`과 개발 명령**

`SaveBackup`의 `EnsureOnce` 뒤에:
```csharp
#if DEVTOOLS
        // 개발 전용: 한 실행 안에서 "첫 쓰기 전 백업"을 여러 번 보려고 백업 기록을 지운다(검사 backup).
        public static void DevReset()
        {
            lastBackupDir = null;
        }
#endif
```
`DevCommandsChar.Execute`의 `case "mbreak":` 앞에(파일에 `using System.Collections.Generic;`을 더한다):
```csharp
                case "bkreset":
                    SaveBackup.DevReset();
                    return "backup record cleared";
                case "upopen":
                    {
                        // 경지 탭의 "게임의 돌파 화면 열기"와 같은 함수를 부르고, 열렸으면 바로 닫는다(검사 backup).
                        string error = EditService.OpenGameBreakthroughScreen(me);
                        if (error != null)
                        {
                            return "refused: " + error;
                        }
                        g.ui.CloseUI(UIType.UpGrade, true);
                        return "opened and closed";
                    }
                case "fateclash":
                    {
                        // 도달한 대경지 H의 역천개명 X를 돌려준다. 도달하지 않은 대경지(지금+1)에 X를 정하려 하면 SetAmong이 H를 비운 뒤 거절한다(검사 backup).
                        int current = FateService.CurrentGrade(me);
                        foreach (var pair in FateService.Records(me))
                        {
                            if (pair.Key <= current && pair.Value != 0 && LuckService.Has(me, pair.Value))
                            {
                                return "held=" + pair.Key + " heldId=" + pair.Value + " target=" + (current + 1);
                            }
                        }
                        return "no held fate";
                    }
                case "fateamong":
                    {
                        // FateService.SetAmong을 직접 부른다: 대경지 p[1]에 p[2]를, pending은 p[3]부터.
                        var pending = new List<int>();
                        for (int i = 3; i < p.Length; i++)
                        {
                            pending.Add(int.Parse(p[i]));
                        }
                        return FateService.SetAmong(me, int.Parse(p[1]), int.Parse(p[2]), pending);
                    }
```

- [ ] **Step 3: 빌드(두 구성), 커밋**

```bash
git add src/ModCode/ModMain/Services/EditService.cs src/ModCode/ModMain/Services/GradeService.cs src/ModCode/ModMain/Services/FateService.cs src/ModCode/ModMain/Safety/SaveBackup.cs src/ModCode/ModMain/Dev/DevCommandsChar.cs
git commit -m "fix: 돌파 화면 열기·칭호 재판정·역천개명 칸 비우기 앞에 백업, 자원 회복의 재조회, 개발 명령 bkreset·upopen·fateclash·fateamong(리뷰 7)"
```

---

### Task 10: 이름 없는 모드 기술(46절)

**Files:**
- Modify: `src/ModCode/ModMain/Services/SkillCatalog.cs:18-30,116-145,145-182`
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs:63-77`

- [ ] **Step 1: 카탈로그**

`Entry`에 `public bool Nameless;        // 모드 행인데 이름이 비거나 "0"이거나 키 그대로라 "이름 없음(번호)"로 둔 것. 분류 목록의 끝에 온다`를 더한다. `Add`의 `string name = …;` 뒤:
```csharp
            // 모드가 이름 키를 LocalText에 넣지 않으면 이름이 비거나 "0"이다(46절). 번호로 찾을 수 있게 이름을 두고 목록 끝으로 보낸다.
            bool nameless = isMod && (name.Length == 0 || name == "0" || name == EditService.Compact(nameKey));
            if (nameless)
            {
                name = "이름 없음(" + id + ")";
            }
```
`new Entry { … NameFromCh = nameFromCh,` 뒤에 `Nameless = nameless,`. `All(kindIndex)`의 `cache[kindIndex] = list;` 앞에:
```csharp
            // 이름 없는 행을 끝으로(그 밖의 순서는 표 순서 그대로).
            var ordered = new List<Entry>(list.Count);
            foreach (Entry entry in list) { if (!entry.Nameless) { ordered.Add(entry); } }
            foreach (Entry entry in list) { if (entry.Nameless) { ordered.Add(entry); } }
            list = ordered;
```

- [ ] **Step 2: 기술 서적 그룹의 줄 순서(분류를 넘어 끝으로)**

`BookRows`:
```csharp
        private List<Row> BookRows()
        {
            var rows = new List<Row>();
            var nameless = new List<Row>();   // 이름 없는 모드 기술은 분류를 넘어 맨 끝에
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(k, filter.Query))
                {
                    if (BookInKind(entry, k) && BookShown(entry))
                    {
                        (entry.Nameless ? nameless : rows).Add(new Row { Skill = entry, SkillKind = k });
                    }
                }
            }
            rows.AddRange(nameless);
            return rows;
        }
```

- [ ] **Step 3: 빌드(두 구성), 커밋**

```bash
git add src/ModCode/ModMain/Services/SkillCatalog.cs src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs
git commit -m "fix: 이름이 비거나 0인 모드 기술을 \"이름 없음(번호)\"로 보이고 목록 끝에 둔다(46절)"
```

---

### Task 11: GREEN 회차 2 — 작은 수정

- [ ] **Step 1: 회차 `ws-1017g`(고정본)**

Run: `-Begin ws-1017g -Fixture base` → `ui-check.ps1 -Part actions|confirm2|backup|import-settle|modbooks|status|fateguard|charvault|translation` → `-End ws-1017g`
Expected: 모두 `FAILS=0`. `backup`의 세 항목이 모두 `+1`(RED에서 셋 다 그대로였어야 한다. 아니었다면 Task 1 Step 3에서 적은 대로). `-End`가 `SAVE RESTORED AND IDENTICAL`.
`backup` (3)에서 `fateclash`가 `no held fate`면 INFO로 넘어가고 05 문서에 "고정본에 역천개명이 없어 (3)은 코드로만 확인"을 적는다.

- [ ] **Step 2: 실패를 고치고 `ws-1017g2`로 다시 돈다.**

---

### Task 12: 문서

**Files:**
- Modify: `docs/05-editor-v0.1.md` (3절 39줄, 30절 1106줄, 34절 1575줄, 끝에 47·48절)
- Modify: `docs/06-handoff.md` (1절 설치·직전 작업·다음 할 일, 2절 탭 순서·공통 규칙 D5, 3절 D4, 5절 탭 좌표, 8.1 표, 8.7 회차 이름)

- [ ] **Step 1: 05 문서의 세 줄**

- 39줄 `| 수명 | 게임의 상한표에 상한이 없어 편집기 자체 상한 100000을 둔다 |` → `| 수명 | 게임의 상한표에 상한이 없어 편집기 자체 상한 8000년을 둔다(\`AttributeCatalog.OwnLifeMaxYears\`. 리뷰 D1에서 100000을 고침) |`
- 1106줄 경지 행 끝에 ` 쓴 뒤 경지에 따른 보충(35절의 \`Settle\`: 선력·등선 진입 선법·칭호 재판정)이 따라오고 결과 문장에 붙는다(48절)` 를 더한다.
- 1575줄 끝에 `(35절에서 고침: 선력 총량으로 채운다)`.

- [ ] **Step 2: 05 문서 47절·48절**

46절 뒤에 두 절을 쓴다. 47절(탭 줄 재배치): 번호 표(스펙 3.2절), 좌표 실측(Task 4), 안전 단추의 동작, 화폐 그룹(줄·명성 줄·요소 이름), 윗줄, 검사(`base`·`tabs`·`items`·`wrap`)와 회차(`ws-1017e` RED, `ws-1017f` GREEN), 확인하지 못한 것(Review Focus 1). 48절(작은 수정 열 가지): 항목마다 규칙·검사 이름·회차 결과 한두 줄, 개발 명령 목록, 미확인(`Destroy` 경로, C5 (3)의 고정본 상태, `mbreak`가 실제로 예외를 냈는지), 회차 이름. 형식은 46절을 따른다(`## 47. …(2026-10-16 추가)`, `### 규칙`, `### 실측·검증`, `### 확인하지 못한 것, 한계`).

- [ ] **Step 3: 인계 문서**

- 1절: 설치 줄은 그대로(설치는 사용자 확인 뒤). "직전 작업"에 ` → 같은 날 X 리뷰 수정 묶음(47·48절. 스펙 \`2026-10-16-review-fixes-design.md\`, 회차 \`ws-1017e\`~\`g\`)`. "다음에 할 일"을 `(1) 배포 DLL 설치(사용자 확인·게임 꺼짐) (2) 사용자 확인(8.5절) (3) 리뷰에서 남긴 것: 12(내보내기 format 2)·9(.devremote 경로)·17·19·10·11·15·낮음 21~49, 8.3a, 8.6절`로.
- 2절 탭 순서 문장: `탭 줄(14개, 간격 46): 능력치 · 자원 · 보관함 · 경지 · 공법 · 선법 · 신기 · 운명 · 기예 · NPC · 세계 · 배율 · 프리셋 · 도호. 안전은 제목 줄의 단추. 검증 스크립트의 이름은 고정 번호 \`Tab<번호>\`(능력치 0, 자원 1, 보관함 3, 경지 4, 공법 5, 선법 6, 신기 7, 운명 8, 기예 9, NPC 10, 세계 11, 배율 12, 프리셋 13, 안전 14, 도호 15. 2는 비었다. 새 탭은 16부터. 05 문서 47절). 화폐는 자원 탭의 그룹이다.` 표의 화폐 행을 자원 행에 합친다(화폐 행 삭제, 자원 행 기능에 "화폐 그룹(영석·재화·명성)" 추가). 공통 규칙의 "게임을 켤 때마다 꺼진 상태로 시작한다"에 ` 세이브를 바꿀 때(\`Destroy\`)도 모두 꺼진다(48절).`
- 3절: `(1~37절)` → `(1~48절)`; 스펙 목록 줄 끝에 빠진 아홉(`2026-10-15-appellation*`, `-martial-books*`, `-resources-tab*`, `-workshop-install*`, `2026-10-16-mod-korean*`, `-mod-skill-books*`, `-workshop-expand*`, `-mod-korean-2*`, `-review-fixes*`)을 더한다(있는 것은 빼고).
- 5절: `편집기가 기본 위치(60,60)일 때 탭 줄은 x=151, \`Tab0\`이 y=146이고 탭마다 52씩 내려간다.` → Task 4의 실측(`Tab0` y=144, 46씩, 안전 단추 `Tab14`는 제목 줄 x=…,y=…).
- 8.1 표에 `| X | 리뷰 수정 묶음(2026-10-16 요청 6): 탭 줄 재배치, 작은 수정 열 가지 | 47·48절. 스펙 \`2026-10-16-review-fixes-design.md\` |`. 8.7 회차 이름에 `ws-1017e`~`g`.

- [ ] **Step 4: 커밋**

```bash
git add docs/05-editor-v0.1.md docs/06-handoff.md
git commit -m "docs: 05 문서 47절(탭 줄 재배치)·48절(작은 수정), 3·30·34절 고침, 인계(탭 번호·좌표·D4·D5·8.1)"
```

---

### Task 13: develop 병합

- [ ] **Step 1:** `git checkout develop && git merge --no-ff feat/review-fixes -m "Merge branch 'feat/review-fixes' into develop"` → `git log --oneline -3`. 브랜치는 지우지 않는다(사용자에게 묻는다). DLL 설치는 사용자 확인·게임 꺼짐 뒤 `pwsh tools/install.ps1`.
