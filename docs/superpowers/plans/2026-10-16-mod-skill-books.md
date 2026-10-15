# 모드 공법의 비급(G2b) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 창작마당 공법 모드가 더한 공법(`isModExtend` 행)을 자원 탭 기술 서적 그룹에 `[모드]`로 보이고, 절기·무예는 "같은 종류의 기본 공법을 뽑아 번호만 바꾸고 읽어 본 뒤 받아들이기"로 비급을 지급하며, 모드 신통은 이유와 함께 막는다.

**Architecture:** `SkillCatalog`가 모드 행을 `IsMod` 항목으로 모으고 이름은 `GameTool.LS` → LocalText `ch` → 키 순으로 푼다. 비급 값은 `SkillCatalog.RollValuesForMod`가 원본 기본 기술을 골라 뽑고 0번을 치환한 뒤 가방에 넣기 전에 `To<MartialData>()`로 읽어 검증한다(최대 50회). `MartialBookService.Make`는 `IsMod`면 그 경로를 쓴다. 화면은 `ResourcesTab.Books.cs`에 `모드만` 칩(`ResKind6`)과 꼬리표·안내만 더한다. 검증은 게임 안 검사 `ui-check.ps1 -Part modbooks`와 개발 명령 `rmods`·`rlearn`이다.

**Tech Stack:** C# 7.3 / .NET 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4(Unhollower), PowerShell 7 검증 스크립트, `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-16-mod-skill-books-design.md`

## Global Constraints

- 브랜치 `feat/mod-skill-books`(develop에서. 스펙 커밋 `29365b4`·`b5af8d1`). 끝에서 `git merge --no-ff`로 `develop`에 병합한다. `main`에는 병합하지 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>`.
- 실험 브랜치 `feat/modskill-research`의 명령(`wsconf`, `wsbookas` 등)은 가져오지 않는다.
- 두 빌드 구성(개발 `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo`, 배포 `-p:DevTools=false -o <임시 폴더>`) 모두 오류 0·경고 0, `bin\Release`에 `MOD_sWP6UL.dll` 하나.
- 게임 검증은 `pwsh tools/DevRemote/session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서. 회차 이름은 `ws-1016h`부터 차례로(`ws-1016a`~`g`는 썼다), 다시 쓰지 않는다. `-End`가 `SAVE RESTORED AND IDENTICAL`과 `WORKSHOP FOLDERS REMOVED: 0`(설치된 모드 15개는 회차 전부터 있던 것이라 남는다). `-Restart`는 DLL을 다시 설치하지 않으므로 코드를 고치면 `-End` 뒤 새 이름으로 `-Begin`. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다.
- 고정본 인물은 결정 초기(대경지 3). 창작마당 모드 15개가 사용자의 게임에 설치돼 있다(`3390738990`의 공법 다섯: 59898952 신통 千杀神枪, 984555698·353535·844854565 절기 烈炎掌·藏气狂刀·风旋龙卷, 3535351 무예 赤炎剑 basSword). 이 다섯 번호가 검사의 상수다.
- 모드 항목: `IsMod`, `GradeMin = 1`, `GradeMax = 10`. 신통 모드 항목의 `BlockReason`은 "이 모드의 신통은 줄 수 없습니다. 게임이 값을 읽지 못합니다(실측)". 모드 항목의 경지 밖 문장은 "경지는 1~10입니다".
- 치환 뽑기: 최대 `SkillCatalog.ModRollMax = 50`회. 실패 문장 "모드 공법의 값을 게임이 읽지 못했습니다(50회 시도)". 원본이 없으면 "원본으로 쓸 기본 공법이 없습니다".
- 요소 이름: `ResKind6`(모드만). 그대로 쓰는 것: `ResKind0`~`5`, `ResGrade1`~`10`, `ResMinLevel1`~`6`, `ResBookFit`, `ResClear`, `BookStar|BookName|BookGive|BookBag<번호>`, `FavBook*`, `ItemQuery`, `ItemTableRange`, `ItemDetail`, `ResAllGive`, `item1`.
- 서비스는 화면을 모른다. 쓰기 전 `SaveBackup.EnsureOnce()`, 넣은 뒤 다시 읽어 판정, 넣은 비급마다 `ChangeLog.Add`(이름에 ` [모드]`).
- 문서에는 확인한 이름만, 확인하지 못한 것은 "미확인"과 사유. 사용자에게 보이는 글은 한국어.

## Review Focus

1. 모드를 뺀 뒤 즐겨찾기에 남은 모드 비급 줄 → 기존 "게임에 없는 기술" 처리로 보이고 지급이 꺼지며 ★로 뺄 수 있다. (`books-ui`의 기존 검사 "a missing skill book favorite…"가 번호 99999로 같은 길을 본다. Task 4에서 `books-ui` 통과 확인)
2. 공법 탭에서 모드 공법에 `배우기`(또는 `강제 추가`)를 누른다 → 만들지 않고 "모드 공법은 자원 탭의 기술 서적에서 비급으로 받습니다"를 보인다. (Task 1 검사 "learning a mod skill directly is refused…", 개발 명령 `rlearn`)
3. 최소 품질 6의 모드 비급 → 50회 안에 품질 6이 나오면 만든다. (Task 1 검사 "a mod book with minimum quality 6")
4. 검색 칸에 중국어 이름을 친다 → 모드 공법이 찾아진다. (Task 1 검사 "the Chinese name is searchable")
5. `이 경지만`을 켜고 끄고 `초기화` → 모드 줄은 그대로 보이고 `모드만` 칩은 풀린다. (Task 1 검사 "this-grade-only keeps mod rows", "clear releases the mod-only chip")

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `tools/research/ui-check.ps1` | 수정 | 부분 `modbooks`(카탈로그·비급·화면), `books-ui`의 칩 수 검사를 일곱으로 |
| `src/ModCode/ModMain/Services/SkillCatalog.cs` | 수정 | `Entry.IsMod/NameFromCh/BasType/KindIndex`, `Add`의 모드 규칙, `ResolveText`·`ChOf`·`Reset`, `BlockReason`, `SourceFor`, `RollValuesForMod`, `Learn`·`LearnAll`의 모드 막기 |
| `src/ModCode/ModMain/Dev/DevCommandsResources.cs` | 수정 | `rmods`, `rlearn` |
| `src/ModCode/ModMain/Services/MartialBookService.cs` | 수정 | `Make`가 `IsMod`면 `RollValuesForMod`, 이름에 ` [모드]` |
| `src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs` | 수정 | `모드만` 칩, 모드 필터, 꼬리표·경지 열, `BookShown`, 아래 칸 안내 |
| `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs` | 수정 | 칩 개수 갱신이 여섯 번째(모드만) 원소를 다루게 |
| `docs/05-editor-v0.1.md` | 수정 | 42절 |
| `docs/06-handoff.md` | 수정 | 1절, 5절, 8.3b절, 8.5절, 8.7절 |

---

### Task 1: 검사 `modbooks`를 쓰고 실패를 본다

**Files:**
- Modify: `tools/research/ui-check.ps1` (`'books-ui' {` 블록 앞에 새 블록)

**Interfaces:**
- Consumes: `check-lib.ps1`의 `Cmd`, `Rc`, `Press`, `Pos`, `Check`, `Shot`, `Traps`; 기존 개발 명령 `rbook`, `rbooks`, `rbookstudy`, `rskill`, `rfav`, `editor`, `uitext`, `uipos`, `ui set`; 고정본의 모드 공법 다섯 번호.
- Produces: 검사 부분 `modbooks`. Task 2·3·4가 각각 자기 몫의 줄을 PASS로 만든다. 개발 명령 `rmods`(형식 `count=<수> items=<분류>:<번호>:<이름>:<basType>:<NameFromCh>:<막힘 이유 또는 ->:<원본 번호 또는 ->,…`)와 `rlearn <분류> <번호> <경지>`(`SkillCatalog.Learn`의 문장)를 Task 2가 만든다.

- [ ] **Step 1: `modbooks` 블록을 쓴다**

`tools/research/ui-check.ps1`의 `  'books-ui' {` 줄 바로 앞에:

```powershell
  'modbooks' {
    # 모드 공법의 비급 스펙 5절. 고정본 인물은 결정(대경지 3). 창작마당 모드 3390738990(공법 다섯)이 설치돼 있어야 한다.
    # 회차의 -End가 세이브와 favorites.txt를 되돌린다.
    $traps = Traps
    function Field([string]$text, [string]$key) { if ($text -match "$key=([^\s|]+)") { $Matches[1] } else { '' } }
    function BookCount([int]$id) { [int](Field (Cmd "rbooks $id" 300) 'count') }
    function Range { if ((Cmd 'uitext ItemTableRange') -match '전체 (\d+)개') { [int]$Matches[1] } elseif ((Cmd 'uitext ItemTableRange') -match "text='0개'") { 0 } else { -1 } }
    # 1. 카탈로그(Task 2)
    $mods = Cmd 'rmods' 3000
    Check 'the catalog lists the five mod skills' ($mods -match 'count=5' -and $mods -match '1:984555698:' -and $mods -match '1:353535:' -and $mods -match '1:844854565:' -and $mods -match '0:3535351:' -and $mods -match '4:59898952:') $mods
    Check 'mod skill names come from the Chinese column' ($mods -match '984555698:烈炎掌:' -and $mods -match '59898952:千杀神枪:' -and $mods -match '3535351:赤炎剑:basSword:True:') $mods
    Check 'only the mod ultimate is blocked' ($mods -match '4:59898952:[^,]*:이 모드의 신통은 줄 수 없습니다[^,]*:' -and $mods -match '1:984555698:[^:]*:[^:]*:True:-:\d+' -and $mods -match '0:3535351:[^:]*:basSword:True:-:\d+') $mods
    $s = Cmd 'rskill 1 984555698' 300
    Check 'a mod skill has grade range 1~10' ($s -match 'min=1 max=10 name=烈炎掌') $s
    $l = Cmd 'rlearn 1 984555698 3' 3000
    Check 'learning a mod skill directly is refused with a hint' ($l -match '모드 공법은 자원 탭의 기술 서적에서 비급으로 받습니다' -and (Cmd 'cmartial 984555698' 300) -match 'n=0') $l
    # 2. 비급(Task 3)
    foreach ($id in 984555698, 353535, 844854565) {
      $before = BookCount $id
      $made = Cmd "rbook 1 $id 3 1 1" 20000
      $books = Cmd "rbooks $id" 300
      $sole = if ($books -match "([A-Za-z0-9]{6}):${id}:3:") { $Matches[1] } else { '' }
      $study = if ($sole) { Cmd "rbookstudy $sole" 300 } else { 'no book' }
      Check "a mod skill book is made and accepted ($id)" ($made -match 'made=1' -and (BookCount $id) -eq $before + 1 -and $study -match 'can=0') "$made | $study"
    }
    $made = Cmd 'rbook 1 984555698 1 1 1' 20000
    Check 'a mod book at grade 1' ($made -match 'made=1' -and (Cmd 'rbooks 984555698' 300) -match ':984555698:1:') $made
    $made = Cmd 'rbook 1 984555698 10 1 1' 20000
    Check 'a mod book at grade 10' ($made -match 'made=1' -and (Cmd 'rbooks 984555698' 300) -match ':984555698:10:') $made
    $made = Cmd 'rbook 1 984555698 3 6 1' 60000
    Check 'a mod book with minimum quality 6' ($made -match 'made=1' -and (Cmd 'rbooks 984555698' 300) -match ':984555698:3:6:') $made
    $ok = 0
    for ($i = 0; $i -lt 3; $i++) { if ((Cmd 'rbook 0 3535351 3 1 1' 60000) -match 'made=1') { $ok++ } }
    Check 'the mod martial art is made three times out of three (retries)' ($ok -eq 3) "ok=$ok"
    $sole = if ((Cmd 'rbooks 3535351' 300) -match '([A-Za-z0-9]{6}):3535351:') { $Matches[1] } else { '' }
    Check 'the mod martial art book reads like a base one' ($sole -ne '' -and (Cmd "rbookstudy $sole" 300) -match 'can=5') "sole=$sole"
    $made = Cmd 'rbook 4 59898952 3 1 1' 3000
    Check 'the mod ultimate is refused with the reason' ($made -match 'made=0' -and $made -match '이 모드의 신통은 줄 수 없습니다') $made
    # 3. 화면(Task 4)
    Cmd 'rfav reset' 300 | Out-Null
    Cmd 'editor' | Out-Null; Press 'Tab1' | Out-Null
    Press 'ResGroupManual' | Out-Null; Press 'ResClear' | Out-Null
    Check 'seven kind chips with the mod-only chip' ((Cmd 'uitext ResKind6') -match "text='모드만 \(5\)'" -and (Cmd 'uipos ResKind7') -match 'no element named') (Cmd 'uitext ResKind6')
    Press 'ResKind6' | Out-Null
    Check 'the mod-only chip shows five rows' ((Range) -eq 5) (Cmd 'uitext ItemTableRange')
    Press 'BookName984555698' | Out-Null
    $d = Cmd 'uitext ItemDetail'
    Check 'the box below explains a mod skill' ($d -match '\[모드\], 경지 1~10\(모드 공법\)' -and $d -match '한글 이름이 없어 중국어 이름을' -and $d -match '지급하면: 경지 3, 품질 1 이상' -and $d -match '모드 공법: 같은 종류의 기본 공법을 뽑아') $d
    $p = Pos 'BookGive59898952'
    Check 'the mod ultimate cannot be given from the row' ((Rc "click $($p.x) $($p.y)" 400) -notmatch 'Button\.onClick: BookGive59898952')
    Press 'BookName59898952' | Out-Null
    Check 'the box below gives the reason for the ultimate' ((Cmd 'uitext ItemDetail') -match '지급할 수 없습니다: 이 모드의 신통은 줄 수 없습니다') (Cmd 'uitext ItemDetail')
    $before = BookCount 984555698
    Press 'item1' | Out-Null
    Press 'BookGive984555698' 20000 | Out-Null
    Check 'giving from the row makes one mod book' ((BookCount 984555698) -eq $before + 1 -and (Cmd 'uitext BookBag984555698') -match "text='$($before + 1)'") (Cmd 'uitext BookBag984555698')
    Cmd 'ui set ItemQuery 烈炎' | Out-Null
    Check 'the Chinese name is searchable' ((Range) -eq 1) (Cmd 'uitext ItemTableRange')
    Press 'ItemQueryClear' | Out-Null
    Press 'ResBookFit' | Out-Null; Press 'ResBookFit' | Out-Null
    Check 'this-grade-only keeps mod rows' ((Range) -eq 5) (Cmd 'uitext ItemTableRange')
    Press 'ResClear' | Out-Null
    Check 'clear releases the mod-only chip' ((Range) -gt 5 -and (Cmd 'uitext ResKind0') -match "text='전체 \(") (Cmd 'uitext ItemTableRange')
    Press 'ResKind6' | Out-Null
    Press 'BookStar984555698' | Out-Null
    Check 'a mod book favorite is saved' ((Cmd 'rfav' 200) -match 'book:1:984555698:3:1:1') (Cmd 'rfav' 200)
    Press 'ResGroupFav' | Out-Null
    $before = BookCount 984555698
    Press 'ResAllGive' | Out-Null; Press 'ResAllGive' 20000 | Out-Null
    Check 'give all gives the mod book' ((BookCount 984555698) -eq $before + 1) "$before -> $(BookCount 984555698)"
    Shot 'modbooks'
    Cmd 'rfav reset' 300 | Out-Null
    Check 'no new traps' ((Traps) -eq $traps)
  }
```

변경 이력의 `[모드]` 줄은 검사가 아니라 Task 3의 Step 4에서 파일을 직접 `grep`한다. `Range`는 `books-ui`의 것과 같은 함수다(블록 안에서 다시 정의한다. 블록마다 함수가 따로 있다).

- [ ] **Step 2: 회차를 열고 실패를 본다**

사용자의 게임이 꺼져 있는지 본 뒤:

```bash
pwsh tools/DevRemote/session.ps1 -Begin ws-1016h -Fixture base
pwsh tools/research/ui-check.ps1 -Part modbooks > .superpowers/modbooks-red.txt 2>&1; grep -c "^PASS" .superpowers/modbooks-red.txt; grep "^FAIL" .superpowers/modbooks-red.txt | cut -c1-100; tail -1 .superpowers/modbooks-red.txt
```

Expected: `WORLD READY`. 카탈로그 줄 5개가 FAIL(`rmods`·`rlearn`이 없어 `unknown command`류, `rskill`이 `no skill`), 비급 줄이 FAIL(`rbook`이 `no skill`), 화면 줄이 FAIL(`ResKind6` 없음, `BookName984555698` 없음). `FAILS=`가 20 안팎. 회차는 열어 둔 채 Task 2로 간다(코드를 고친 뒤 `-End`하고 새 이름으로 연다).

- [ ] **Step 3: 커밋**

```bash
git add tools/research/ui-check.ps1
git commit -m "test: ui-check modbooks — 모드 공법의 카탈로그·비급·화면 검사(아직 실패)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 2: 카탈로그가 모드 공법을 모은다

**Files:**
- Modify: `src/ModCode/ModMain/Services/SkillCatalog.cs` (`Entry`, `Add`, `All`, `BlockReason`, `Learn`, `LearnAll`; 새 `ResolveText`·`ChOf`·`Reset`·`SourceFor`)
- Modify: `src/ModCode/ModMain/Dev/DevCommandsResources.cs` (`rmods`, `rlearn`)

**Interfaces:**
- Consumes: `g.conf.battleSkillAttack/battleStepBase/battleAbilityBase._allConfList`의 `isModExtend`(`ConfBaseItem`), `battleSkillAttack` 행의 `basType`(string), `g.conf.localText.allConfList`(`ConfLocalTextItem.key/ch`), `GameTool.LS`, `OwnCombine`.
- Produces: `Entry.IsMod`(bool), `Entry.NameFromCh`(bool), `Entry.BasType`(string), `Entry.KindIndex`(int); `SkillCatalog.ResolveText(string key, out bool fromCh)`, `SkillCatalog.ChOf(string key)`, `SkillCatalog.Reset()`, `SkillCatalog.SourceFor(int kindIndex, Entry mod, int grade)`(원본 `Entry` 또는 null); `BlockReason`의 모드 규칙; `Learn`이 모드 항목을 거부하는 문장; 개발 명령 `rmods`·`rlearn`. Task 3·4가 쓴다.

- [ ] **Step 1: `Entry`와 `Add`, `All`을 고친다**

`SkillCatalog.cs`의 `Entry`를:

```csharp
        public class Entry
        {
            public int BaseId;
            public string Name;
            public string Desc;
            public int GradeMin;
            public int GradeMax;
            public string Search;
            public int KindIndex;        // Kinds의 순번
            public bool IsMod;           // 모드가 더한 행(ConfBaseItem.isModExtend). 경지 범위는 1~10으로 둔다
            public bool NameFromCh;      // 한국어 이름이 없어 LocalText의 ch(중국어)로 채웠다
            public string BasType;       // 무예·영술의 무기 종류(battleSkillAttack.basType). 그 밖은 ""
        }
```

`Add`를(기존 함수를 통째로 바꾼다):

```csharp
        // 기본 행: 경지 범위가 0~0인 항목은 플레이어가 배우는 기술이 아니다(괴물용·부품). 이름 키가 풀리지 않는 것도 뺀다.
        // 모드 행(isModExtend): 경지 범위가 0~0이고 한국어 이름이 "0"이어도 넣는다(실측, 인계 8.3e절). 경지는 1~10.
        private static void Add(List<Entry> list, int kindIndex, bool isMod, int id, string nameKey, string descKey, int gradeMin, int gradeMax, string basType)
        {
            bool nameFromCh, descFromCh;
            string name = EditService.Compact(ResolveText(nameKey, out nameFromCh));
            if (!isMod && (gradeMax < 1 || name.Length == 0 || name == EditService.Compact(nameKey)))
            {
                return;
            }
            list.Add(new Entry
            {
                BaseId = id,
                Name = name,
                Desc = MartialService.CleanDesc(ResolveText(descKey, out descFromCh)).Replace("\r", "").Replace('\n', ' '),
                GradeMin = isMod ? 1 : gradeMin,
                GradeMax = isMod ? 10 : gradeMax,
                Search = name + EditService.Compact(ChOf(nameKey)),
                KindIndex = kindIndex,
                IsMod = isMod,
                NameFromCh = nameFromCh,
                BasType = basType ?? "",
            });
        }
```

`All`의 `Add(...)` 호출 셋을:

```csharp
                    Add(list, kindIndex, all[i].isModExtend, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, "");          // battleStepBase
                    Add(list, kindIndex, all[i].isModExtend, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, "");          // battleAbilityBase
                        Add(list, kindIndex, all[i].isModExtend, all[i].id, all[i].name, all[i].desc, all[i].gradeMin, all[i].gradeMax, all[i].basType);   // battleSkillAttack
```

- [ ] **Step 2: `ResolveText`·`ChOf`·`Reset`·`SourceFor`와 `BlockReason`을 넣는다**

`cache` 선언 아래에:

```csharp
        private static Dictionary<string, string> chByKey;   // LocalText 키 → ch(중국어). 처음 한 번 만든다

        // 한국어 글이 없으면("0"·빈 글·키 그대로) LocalText 행의 ch(중국어)를, 그것도 없으면 키를 돌려준다.
        // 모드의 LocalText는 ch만 있고 kr이 "0"이다(실측). G2c가 kr을 덮으면 LS가 한글을 돌려주므로 그대로 따라온다.
        internal static string ResolveText(string key, out bool fromCh)
        {
            fromCh = false;
            string text = GameTool.LS(key);
            if (!string.IsNullOrEmpty(text) && text != "0" && text != key)
            {
                return text;
            }
            string ch = ChOf(key);
            if (!string.IsNullOrEmpty(ch))
            {
                fromCh = true;
                return ch;
            }
            return key;
        }

        // LocalText 행의 ch. 없으면 "". (allText.TryGetValue는 Il2Cpp 제네릭 호출에서 MissingMethodException이 났다. 목록을 한 번 훑어 사전을 만든다.)
        internal static string ChOf(string key)
        {
            if (chByKey == null)
            {
                chByKey = new Dictionary<string, string>();
                var all = g.conf.localText.allConfList;
                for (int i = 0; i < all.Count; i++)
                {
                    ConfLocalTextItem item = all[i];
                    if (item != null && item.key != null && !chByKey.ContainsKey(item.key))
                    {
                        chByKey[item.key] = item.ch ?? "";
                    }
                }
            }
            string found;
            return key != null && chByKey.TryGetValue(key, out found) ? found : "";
        }

        // 목록과 LocalText 사전을 비운다. LocalText를 덮어쓴 뒤(G2c) 부른다.
        public static void Reset()
        {
            cache.Clear();
            chByKey = null;
        }

        // 모드 항목의 비급을 뽑을 원본: 같은 분류의 기본 항목 가운데 고른 경지를 범위에 포함하는 것.
        // 무예·영술은 같은 basType을 먼저, 자기 묶음(OwnCombine)이 있는 것을 먼저 쓴다(뽑기가 한 번에 끝난다). 없으면 null.
        internal static Entry SourceFor(int kindIndex, Entry mod, int grade)
        {
            Kind kind = Kinds[kindIndex];
            Entry best = null;
            int bestScore = -1;
            foreach (Entry e in All(kindIndex))
            {
                if (e.IsMod || grade < e.GradeMin || grade > e.GradeMax)
                {
                    continue;
                }
                int score = (OwnCombine(kind, e.BaseId) != 0 ? 1 : 0) + (kind.Type == MartialType.SkillLeft && e.BasType == mod.BasType ? 2 : 0);
                if (score > bestScore)
                {
                    best = e;
                    bestScore = score;
                }
            }
            return best;
        }
```

`BlockReason`을:

```csharp
        // 지금 배울(만들) 수 없는 이유. 할 수 있으면 null.
        public static string BlockReason(Entry entry, int grade)
        {
            if (entry.IsMod)
            {
                if (Kinds[entry.KindIndex].Type == MartialType.Ultimate)
                {
                    return "이 모드의 신통은 줄 수 없습니다. 게임이 값을 읽지 못합니다(실측)";
                }
                return grade < 1 || grade > 10 ? "경지는 1~10입니다" : null;
            }
            return grade < entry.GradeMin || grade > entry.GradeMax
                ? "경지 " + entry.GradeMin + "~" + entry.GradeMax + "의 기술이라 경지 " + grade + "로는 만들 수 없습니다"
                : null;
        }
```

- [ ] **Step 3: `Learn`·`LearnAll`이 모드 항목을 거른다**

`Learn`의 첫 줄(`Kind kind = Kinds[kindIndex];` 다음)에:

```csharp
            if (entry.IsMod)
            {
                return entry.Name + ": 모드 공법은 자원 탭의 기술 서적에서 비급으로 받습니다.";
            }
```

`LearnAll`의 `foreach` 안, `if (have.ContainsKey(entry.BaseId))` 앞에:

```csharp
                if (entry.IsMod)
                {
                    skipped++;
                    continue;
                }
```

- [ ] **Step 4: 개발 명령 `rmods`·`rlearn`을 넣는다**

`DevCommandsResources.cs`의 머리 주석에 두 줄:

```csharp
    //   rmods : 카탈로그의 모드 항목 count=<수> items=<분류>:<번호>:<이름>:<basType>:<NameFromCh>:<막힘 이유(경지 3) 또는 ->:<원본 번호(경지 3) 또는 ->,…
    //   rlearn <분류> <번호> <경지> : SkillCatalog.Learn의 문장(모드 항목은 거부 문장)
```

`switch`에:

```csharp
                case "rmods":
                    return Mods();
                case "rlearn":
                    {
                        SkillCatalog.Entry entry = SkillCatalog.Get(int.Parse(p[1]), int.Parse(p[2]));
                        return entry == null ? "no skill " + p[2] : SkillCatalog.Learn(g.world.playerUnit, int.Parse(p[1]), entry, int.Parse(p[3]), false);
                    }
```

`Books` 함수 앞에:

```csharp
        private static string Mods()
        {
            var items = new List<string>();
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry e in SkillCatalog.Find(k, ""))
                {
                    if (!e.IsMod)
                    {
                        continue;
                    }
                    string block = SkillCatalog.BlockReason(e, 3) ?? "-";
                    SkillCatalog.Entry source = SkillCatalog.SourceFor(k, e, 3);
                    items.Add(k + ":" + e.BaseId + ":" + e.Name + ":" + e.BasType + ":" + e.NameFromCh + ":" + block + ":" + (source == null ? "-" : source.BaseId.ToString()));
                }
            }
            return "count=" + items.Count + " items=" + string.Join(",", items.ToArray());
        }
```

- [ ] **Step 5: 빌드하고 회차를 바꿔 카탈로그 검사가 통과하는지 본다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
pwsh tools/DevRemote/session.ps1 -End ws-1016h
pwsh tools/DevRemote/session.ps1 -Begin ws-1016i -Fixture base
pwsh tools/research/ui-check.ps1 -Part modbooks > .superpowers/modbooks-t2.txt 2>&1; grep "^PASS\|^FAIL" .superpowers/modbooks-t2.txt | cut -c1-100; tail -1 .superpowers/modbooks-t2.txt
```

Expected: 빌드 경고 0·오류 0. `-End`가 `SAVE RESTORED AND IDENTICAL`·`WORKSHOP FOLDERS REMOVED: 0`. 카탈로그 다섯 줄 PASS(`the catalog lists the five mod skills`, `mod skill names come from the Chinese column`, `only the mod ultimate is blocked`, `a mod skill has grade range 1~10`, `learning a mod skill directly is refused with a hint`). 비급·화면 줄은 아직 FAIL(`rbook`이 모드 항목을 뽑지 못해 `made=0`, `ResKind6` 없음). `rmods`의 `items`에서 무예 3535351의 원본이 `basSword`인 기본 무예 번호인지 `rskill 0 <원본>`으로 본다.

- [ ] **Step 6: 커밋**

```bash
git add src/ModCode/ModMain/Services/SkillCatalog.cs src/ModCode/ModMain/Dev/DevCommandsResources.cs
git commit -m "feat: SkillCatalog — isModExtend 행을 모드 항목으로(경지 1~10, 이름 ch 대체), 원본 고르기, 모드 신통 막기, 공법 탭 배우기 거부, rmods·rlearn

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 3: 치환 뽑기로 모드 비급을 만든다

**Files:**
- Modify: `src/ModCode/ModMain/Services/SkillCatalog.cs` (`RollValues` 뒤에 `RollValuesForMod`)
- Modify: `src/ModCode/ModMain/Services/MartialBookService.cs` (`Make`)

**Interfaces:**
- Consumes: Task 2의 `Entry.IsMod`, `SourceFor`, `RollValues(kind, baseId, grade, maxTries, accept)`, `Kinds`, `RollMax`; `DataProps.PropsData.NewMartial`, `To<DataProps.MartialData>()`.
- Produces: `SkillCatalog.ModRollMax = 50`; `SkillCatalog.RollValuesForMod(int kindIndex, Entry mod, int grade, Func<DataProps.MartialData, bool> accept, out int tries, out string reason)` → 값 배열 또는 null; `Make`의 멈춤 문장 "모드 공법의 값을 게임이 읽지 못했습니다(50회 시도)" / "원본으로 쓸 기본 공법이 없습니다"; 변경 이력 이름 `비급 <이름> [모드](<번호>)`.

- [ ] **Step 1: `RollValuesForMod`를 넣는다**

`SkillCatalog.cs`의 `RollValues` 함수 뒤에:

```csharp
        public const int ModRollMax = 50;

        // 모드 공법의 비급 값(인계 8.3e절의 실측): 같은 분류의 기본 기술(원본)을 뽑아 0번(기술 번호)만 모드 번호로 바꾸고,
        // 가방에 넣기 전에 읽어 본다. 게임이 읽지 못하는 조합(무예는 넷에 하나꼴로 읽혔다)이면 버리고 다시 뽑는다. 최대 ModRollMax번.
        internal static Il2CppStructArray<int> RollValuesForMod(int kindIndex, Entry mod, int grade, Func<DataProps.MartialData, bool> accept, out int tries, out string reason)
        {
            tries = 0;
            reason = null;
            Kind kind = Kinds[kindIndex];
            Entry source = SourceFor(kindIndex, mod, grade);
            if (source == null)
            {
                reason = "원본으로 쓸 기본 공법이 없습니다";
                return null;
            }
            for (tries = 1; tries <= ModRollMax; tries++)
            {
                Il2CppStructArray<int> rolled = RollValues(kind, source.BaseId, grade, RollMax, null);
                if (rolled == null)
                {
                    continue;
                }
                var values = new Il2CppStructArray<int>(rolled.Length);
                for (int i = 0; i < rolled.Length; i++)
                {
                    values[i] = i == 0 ? mod.BaseId : rolled[i];
                }
                try
                {
                    DataProps.MartialData martial = DataProps.PropsData.NewMartial(kind.Type, values).To<DataProps.MartialData>();
                    if (martial.baseID == mod.BaseId && !string.IsNullOrEmpty(martial.martialInfo.name) && (accept == null || accept(martial)))
                    {
                        return values;
                    }
                }
                catch (Exception)
                {
                    // 게임이 읽지 못하는 조합. 다시 뽑는다.
                }
            }
            tries = ModRollMax;
            reason = "모드 공법의 값을 게임이 읽지 못했습니다(" + ModRollMax + "회 시도)";
            return null;
        }
```

- [ ] **Step 2: `Make`가 모드 항목에 그것을 쓴다**

`MartialBookService.Make`에서 `string label = entry.Name + " 비급";`을:

```csharp
            string label = entry.Name + (entry.IsMod ? " [모드]" : "") + " 비급";
```

루프 안의

```csharp
                    Il2CppStructArray<int> values = SkillCatalog.RollValues(kind, entry.BaseId, grade, RollMax, m => m.martialInfo.level >= minLevel);
                    if (values == null)
                    {
                        stop = "경지 " + grade + "에서 품질 " + minLevel + " 이상이 나오지 않았습니다";
                        break;
                    }
```

을:

```csharp
                    Il2CppStructArray<int> values;
                    if (entry.IsMod)
                    {
                        int tries;
                        string reason;
                        values = SkillCatalog.RollValuesForMod(kindIndex, entry, grade, m => m.martialInfo.level >= minLevel, out tries, out reason);
                        if (values == null)
                        {
                            stop = reason;
                            break;
                        }
                    }
                    else
                    {
                        values = SkillCatalog.RollValues(kind, entry.BaseId, grade, RollMax, m => m.martialInfo.level >= minLevel);
                        if (values == null)
                        {
                            stop = "경지 " + grade + "에서 품질 " + minLevel + " 이상이 나오지 않았습니다";
                            break;
                        }
                    }
```

변경 이력 줄의 `"비급 " + name + "(" + entry.BaseId + ")"`을 `"비급 " + name + (entry.IsMod ? " [모드]" : "") + "(" + entry.BaseId + ")"`로.

- [ ] **Step 3: 빌드하고 회차를 바꿔 비급 검사가 통과하는지 본다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
pwsh tools/DevRemote/session.ps1 -End ws-1016i
pwsh tools/DevRemote/session.ps1 -Begin ws-1016j -Fixture base
pwsh tools/research/ui-check.ps1 -Part modbooks > .superpowers/modbooks-t3.txt 2>&1; grep "^PASS\|^FAIL" .superpowers/modbooks-t3.txt | cut -c1-100; tail -1 .superpowers/modbooks-t3.txt
```

Expected: 카탈로그 줄과 비급 줄 모두 PASS(절기 셋 `made and accepted`, 경지 1·10, 최소 품질 6, 무예 3/3, 무예 `can=5`, 신통 `made=0`과 이유). 화면 줄은 아직 FAIL.

- [ ] **Step 4: 변경 이력에 `[모드]`가 적혔는지 본다**

```bash
grep -a "비급 .*\[모드\](984555698)" "$USERPROFILE/AppData/LocalLow/guigugame/guigubahuang/TlToybox/changes.log" | tail -2
```

Expected: `비급 묵운0 [모드](984555698)`처럼 이름(옵션 접두 + "0") 뒤에 ` [모드]`가 있는 줄이 있다(게임이 보이는 비급 이름은 G2c 전까지 "…0"이다).

- [ ] **Step 5: 커밋**

```bash
git add src/ModCode/ModMain/Services/SkillCatalog.cs src/ModCode/ModMain/Services/MartialBookService.cs
git commit -m "feat: 모드 공법의 비급 — 원본 뽑기·0번 치환·읽기 확인(최대 50회), Make의 모드 경로와 이력 표시

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 4: 기술 서적 그룹 화면에 모드 공법을 보인다

**Files:**
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs` (`bookKind` 값, `BookShown`, `BookRows`, `BookKindCounts`, `DrawBookKinds`, `DrawSkillRow`, `SkillDetail`)
- Modify: `src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs` (칩 개수 갱신 분기)
- Modify: `tools/research/ui-check.ps1` (`books-ui`의 칩 수 검사)

**Interfaces:**
- Consumes: Task 2의 `Entry.IsMod`, `NameFromCh`, `BlockReason`; 기존 `Ui.Button/Label/Wrapped`, `CountLabel`, `AmountBar`, `MartialBookService.Describe`.
- Produces: 칩 `ResKind6`(모드만), `bookKind == ModOnly(-2)`; `BookKindCounts()`가 여섯 원소(다섯 분류 + 모드만)를 돌려준다.

- [ ] **Step 1: `ResourcesTab.Books.cs`를 고친다**

`private int bookKind = -1;` 줄 위에:

```csharp
        private const int ModOnly = -2;     // bookKind가 이 값이면 모든 분류의 모드 공법만
```

`BookShown`·`BookRows`·`BookKindCounts`·`BookCount`를:

```csharp
        private bool BookShown(SkillCatalog.Entry entry)
        {
            // 모드 공법은 경지 범위가 없고(1~10) 막힌 신통도 이유를 읽을 수 있게 늘 보인다.
            return entry.IsMod || !bookFit || SkillCatalog.BlockReason(entry, BookGrade) == null;
        }

        private bool BookInKind(SkillCatalog.Entry entry, int k)
        {
            return bookKind == ModOnly ? entry.IsMod : (bookKind < 0 || bookKind == k);
        }

        private List<Row> BookRows()
        {
            var rows = new List<Row>();
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(k, filter.Query))
                {
                    if (BookInKind(entry, k) && BookShown(entry))
                    {
                        rows.Add(new Row { Skill = entry, SkillKind = k });
                    }
                }
            }
            return rows;
        }

        // 분류마다 검색어와 "이 경지만"을 통과한 기술의 수(SkillCatalog.Kinds 순서) + 마지막에 모드 공법의 수.
        private int[] BookKindCounts()
        {
            var counts = new int[SkillCatalog.Kinds.Length + 1];
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                foreach (SkillCatalog.Entry entry in SkillCatalog.Find(k, filter.Query))
                {
                    if (BookShown(entry))
                    {
                        counts[k]++;
                        if (entry.IsMod)
                        {
                            counts[SkillCatalog.Kinds.Length]++;
                        }
                    }
                }
            }
            return counts;
        }

        private int BookCount()
        {
            int total = 0;
            int[] counts = BookKindCounts();
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                total += counts[k];
            }
            return total;
        }
```

`DrawBookKinds`를:

```csharp
        // 종류 칩 일곱(전체, 다섯 분류, 모드만). 한 줄에 들어간다.
        private float DrawBookKinds(float y)
        {
            int[] counts = BookKindCounts();
            int total = 0;
            for (int k = 0; k < SkillCatalog.Kinds.Length; k++)
            {
                total += counts[k];
            }
            float x = 0;
            for (int i = 0; i <= SkillCatalog.Kinds.Length + 1; i++)
            {
                bool modChip = i == SkillCatalog.Kinds.Length + 1;
                string name = i == 0 ? "전체" : modChip ? "모드만" : SkillCatalog.Kinds[i - 1].Tab;
                int count = i == 0 ? total : counts[i - 1];
                float w = Math.Max(70f, Ui.TextWidth(name + " (00000)", 19) + 28f);
                int kindValue = modChip ? ModOnly : i - 1;
                Button chip = Ui.Button("ResKind" + i, content, name + " (" + count + ")", RightX + x, y, w, ChipH,
                    new Action(() => { bookKind = kindValue; Changed(); }), true, bookKind == kindValue);
                labels.Add(new CountLabel { Text = chip.GetComponentInChildren<Text>(), Name = name, Index = i });
                x += w + ChipGap;
            }
            return y + ChipH + 6;
        }
```

(`i == Kinds.Length + 1`이면 `counts[i - 1]`이 여섯 번째 원소, 곧 모드 공법의 수다.)

`DrawSkillRow`의 분류·경지 두 줄을:

```csharp
            Ui.Wrapped(row, SkillCatalog.Kinds[kindIndex].Tab + (skill.IsMod ? " [모드]" : ""), ColKind, 0, KindW, h, 16);
            Ui.Label(row, skill.IsMod ? "모드" : skill.GradeMin + "~" + skill.GradeMax, ColLevel, 0, 50, h, 16);
```

`SkillDetail`을:

```csharp
        private string SkillDetail(Row r)
        {
            SkillCatalog.Entry skill = r.Skill;
            string head = skill.Name + "  [" + skill.BaseId + "]  " + SkillCatalog.Kinds[r.SkillKind].Tab
                + (skill.IsMod ? " [모드], 경지 1~10(모드 공법)" : ", 경지 " + skill.GradeMin + "~" + skill.GradeMax);
            string note = "";
            if (skill.IsMod)
            {
                if (skill.NameFromCh)
                {
                    note += "\n한글 이름이 없어 중국어 이름을 보입니다(게임 화면과 비급 이름에는 0으로 보일 수 있습니다. 한글화는 G2c).";
                }
                note += "\n모드 공법: 같은 종류의 기본 공법을 뽑아 번호만 바꿔 만듭니다. 옵션은 게임이 뽑은 것입니다.";
            }
            return head + "\n" + MartialBookService.Describe(skill, BookGrade, bookMinLevel, AmountBar.Get("item")) + note + "\n" + skill.Desc;
        }
```

- [ ] **Step 2: 칩 개수 갱신이 여섯 번째 원소를 다루게 한다**

`ResourcesTab.cs`의 `else if (kinds != null)` 분기를:

```csharp
                else if (kinds != null)
                {
                    // 기술 서적 그룹의 kinds는 다섯 분류 뒤에 "모드만"이 하나 더 있다. 전체 수에는 넣지 않는다(분류 수에 이미 들어 있다).
                    int kindCount = groupKey == ManualKey ? SkillCatalog.Kinds.Length : kinds.Length;
                    int sum = 0;
                    for (int i = 0; i < kindCount; i++)
                    {
                        sum += kinds[i];
                    }
                    label.Set(label.Index == 0 ? sum : kinds[label.Index - 1]);
                }
```

- [ ] **Step 3: 빌드하고 회차를 바꿔 화면 검사를 본다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
pwsh tools/DevRemote/session.ps1 -End ws-1016j
pwsh tools/DevRemote/session.ps1 -Begin ws-1016k -Fixture base
pwsh tools/research/ui-check.ps1 -Part modbooks > .superpowers/modbooks-t4.txt 2>&1; grep "^FAIL" .superpowers/modbooks-t4.txt | cut -c1-120; tail -1 .superpowers/modbooks-t4.txt
pwsh tools/research/ui-check.ps1 -Part books-ui > .superpowers/booksui-t4.txt 2>&1; grep "^FAIL" .superpowers/booksui-t4.txt | cut -c1-120; tail -1 .superpowers/booksui-t4.txt
```

Expected: `modbooks`가 `FAILS=0`. `books-ui`는 `six kind chips` 한 줄만 FAIL(이제 `ResKind6`이 있다) — 이것이 다음 단계의 RED다.

- [ ] **Step 4: `books-ui`의 칩 수 검사를 일곱으로 고친다**

`ui-check.ps1`의 줄

```powershell
    Check 'six kind chips' ((Cmd 'uitext ResKind0') -match "text='전체 \(" -and (Cmd 'uitext ResKind5') -match "text='신통 \(" -and (Cmd 'uipos ResKind6') -match 'no element named')
```

을

```powershell
    Check 'seven kind chips' ((Cmd 'uitext ResKind0') -match "text='전체 \(" -and (Cmd 'uitext ResKind5') -match "text='신통 \(" -and (Cmd 'uitext ResKind6') -match "text='모드만 \(" -and (Cmd 'uipos ResKind7') -match 'no element named')
```

로. 그리고 같은 회차에서:

```bash
pwsh tools/research/ui-check.ps1 -Part books-ui > .superpowers/booksui-t4b.txt 2>&1; tail -1 .superpowers/booksui-t4b.txt
pwsh tools/research/ui-check.ps1 -Part books > .superpowers/books-t4.txt 2>&1; tail -1 .superpowers/books-t4.txt
pwsh tools/research/ui-check.ps1 -Part favorites > .superpowers/fav-t4.txt 2>&1; tail -1 .superpowers/fav-t4.txt
pwsh tools/research/ui-check.ps1 -Part groups > .superpowers/groups-t4.txt 2>&1; tail -1 .superpowers/groups-t4.txt
pwsh tools/DevRemote/session.ps1 -End ws-1016k
```

Expected: 넷 모두 `FAILS=0`(`books`는 가방을 채우므로 마지막에. `favorites`가 경고를 남기면 그 뒤 `-End`). `-End`가 `SAVE RESTORED AND IDENTICAL`, `WORKSHOP FOLDERS REMOVED: 0`. 실패하면 원인을 적고 고친 뒤 새 회차에서 다시 돌린다.

- [ ] **Step 5: 배포 구성도 컴파일되는지 보고 커밋**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check"
git add src/ModCode/ModMain/UI/Tabs/ResourcesTab.Books.cs src/ModCode/ModMain/UI/Tabs/ResourcesTab.cs tools/research/ui-check.ps1
git commit -m "feat: 기술 서적 그룹 — 모드만 칩, [모드] 꼬리표와 경지 '모드', 모드 공법 안내, 막힌 신통 줄 보이기; books-ui 칩 일곱

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

---

### Task 5: 문서, 검토, 병합

**Files:**
- Modify: `docs/05-editor-v0.1.md` (41절 뒤에 42절)
- Modify: `docs/06-handoff.md` (1절, 5절, 8.3b절, 8.5절, 8.7절)

- [ ] **Step 1: 05 문서 42절을 쓴다**

41절 끝에 이어 붙인다(값은 Task 2~4의 회차 결과로 채운다):

```markdown
## 42. 모드 공법의 비급(G2b) (2026-10-16 추가)

스펙 `docs/superpowers/specs/2026-10-16-mod-skill-books-design.md`, 계획 `plans/2026-10-16-mod-skill-books.md`. 설계 전 실험은 인계 8.3e절(회차 `ws-1016a`~`g`).

### 규칙

- 모드가 더한 기술 행(`ConfBaseItem.isModExtend`)은 경지 범위가 0~0이고 이름 키의 한국어가 "0"이어도 카탈로그에 들어간다(`IsMod`, 경지 1~10). 이름·설명은 `GameTool.LS` → LocalText `ch` → 키 순으로 푼다(`SkillCatalog.ResolveText`). 검색은 푼 이름과 `ch` 모두로.
- 비급 값은 같은 분류의 기본 기술(무예·영술은 같은 `basType`, 자기 묶음이 있는 것 먼저)을 뽑아 0번(기술 번호)만 모드 번호로 바꾸고, 가방에 넣기 전에 `To<MartialData>()`로 읽어 본 뒤 받아들인다. 최대 50회(`SkillCatalog.ModRollMax`). 실패하면 "모드 공법의 값을 게임이 읽지 못했습니다(50회 시도)".
- 모드 신통은 막는다(`BlockReason`: "이 모드의 신통은 줄 수 없습니다. 게임이 값을 읽지 못합니다(실측)"). 공법 탭의 배우기는 모드 항목을 거부한다("모드 공법은 자원 탭의 기술 서적에서 비급으로 받습니다"), 모두 배우기는 건너뛴다.
- 화면: 종류 칩 `모드만`(`ResKind6`), 분류 열 `[모드]`, 경지 열 `모드`, `이 경지만`은 모드 줄을 거르지 않는다. 아래 칸에 중국어 이름 안내와 만드는 방식 안내. 즐겨찾기 비급 줄은 모드 번호도 같은 형식.
- 개발 명령 `rmods`(모드 항목과 원본), `rlearn <분류> <번호> <경지>`(공법 탭 배우기의 문장). 검사 `ui-check.ps1 -Part modbooks`.
- G2c가 LocalText의 `kr`을 덮은 뒤 `SkillCatalog.Reset()`을 부르면 이름이 한글로 바뀐다(이 묶음은 함수만 둔다).

### 검증 (회차 `ws-1016h`~`k`)

(회차 결과를 적는다: `modbooks`의 PASS 줄 수와 FAIL 0, `rmods`의 다섯 항목과 무예의 원본 번호·이름, 무예 3/3의 시도 수(결과 문장에 없으면 "미기록"), 경지 1·10과 최소 품질 6의 결과, `books-ui`·`books`·`favorites`·`groups` 통과, 변경 이력의 `[모드]` 줄, 캡처 `modbooks`.)

### 확인하지 못한 것, 한계

- 무예의 치환 뽑기 성공률(실험 1/4)과 50회 한도. 최소 품질 6은 품질 조건까지 겹쳐 실패할 수 있다(그때 멈춤 문장).
- 만든 모드 비급을 게임에서 학습해 전투에서 쓸 때(사용자 확인 항목). 옵션이 원본 기술의 것이라 어색할 수 있다.
- 게임이 보이는 비급 이름은 G2c 전까지 "<옵션 접두>0"이다.
- 다른 공법 모드(신통이 되는 모드가 있을 수 있다). `isModExtend`는 이 모드의 행에서만 봤다.
- 모드를 뺀 세이브에 남은 모드 비급은 게임이 깨진 아이템으로 다룰 수 있다.
```

- [ ] **Step 2: 인계 문서를 고친다**

- 1절 "직전 작업": "G2b 모드 공법의 비급(05 문서 42절): 모드 행을 카탈로그에 넣고 치환 뽑기로 절기·무예 비급을 지급, 모드 신통은 막음, 기술 서적 그룹에 `모드만` 칩. 회차 `ws-1016h`~`k`."
- 1절 "다음에 할 일": (2)를 "G2b 끝남"으로 지우고 G2c 한글화를 첫째로: "G2c 한글화: 실험은 끝났다(8.3e절). 번역 파일(키 → 한글) → 실행 중 `kr` 덮기 → `SkillCatalog.Reset()`. 설계부터." 그 뒤는 그대로.
- 5절 "회차 안에서 쓰는 도구" 자원 탭 줄에 `rmods`, `rlearn`. 자동 검증 표에 `modbooks`(기술 서적 그룹의 모드 공법: 카탈로그·비급·화면. 가방에 비급을 넣으므로 회차 뒤쪽에).
- 8.3b절 제목을 "G2 창작마당 모드 연동 — G2a·G2b 끝남(2026-10-16), G2c 남음"으로 바꾸고 끝에 "G2b는 05 문서 42절. 남은 것은 G2c(한글화)뿐이다." 문단.
- 8.5절에: "모드 공법 비급(42절): 게임에서 학습해 전투에서 썼을 때, 자원 탭 `모드만` 칩과 `[모드]` 줄을 실제로 써 봤을 때".
- 8.7절 회차 이름 줄에 `ws-1016h`~`k`.

- [ ] **Step 3: 커밋**

```bash
git add docs/05-editor-v0.1.md docs/06-handoff.md
git commit -m "docs: 05 문서 42절(모드 공법의 비급)과 인계(G2b 끝남, 다음은 G2c, rmods·modbooks)

Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>"
```

- [ ] **Step 4: 검토자 한 명이 브랜치 전체를 본다**

읽기 전용 서브에이전트(가장 높은 모델)에게 `git -C E:\TlToybox diff develop...feat/mod-skill-books`와 스펙·계획·장부의 판정을 주고 본다. 지시에 "이 검토만 하고 보고 뒤 정지. 파일을 고치지 말 것. 모든 git 명령에 `-C E:\TlToybox`"를 넣는다. 보는 것: `RollValuesForMod`가 가방을 건드리기 전에만 읽는지(실패한 시도가 흔적을 남기지 않는지), `Add`의 모드 규칙이 기본 행의 기존 필터를 바꾸지 않았는지(`books-ui`·`books` 통과로도 보지만 코드로), `BlockReason`이 `KindIndex`를 쓰므로 `Entry`를 만드는 모든 자리가 `KindIndex`를 채우는지, `BookKindCounts`의 여섯 번째 원소가 `ResourcesTab.cs`의 갱신 분기와 `BookCount`에서 두 번 세이지 않는지, `Learn`의 모드 거부가 `강제 추가`(force)에도 걸리는지, `ChOf`의 사전이 모드 추가·제거 뒤(게임 재시작 전에는 바뀌지 않음)에 맞는지. 실제 결함은 시험(검사)을 먼저 더한 뒤 고친다. "판단 보류"는 05 문서 42절의 "확인하지 못한 것"에 적는다.

- [ ] **Step 5: 병합**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release --nologo -p:DevTools=false -o "$env:TEMP\tltoybox-release-check"
git checkout develop
git merge --no-ff feat/mod-skill-books -m "Merge branch 'feat/mod-skill-books' into develop"
git log --oneline -3
```

Expected: 두 빌드 경고 0·오류 0, 병합 커밋 하나. 설치(`tools/install.ps1`)는 사용자 확인을 받고 게임이 꺼진 상태에서 한다(배포 DLL이 바뀌었다). 작업 브랜치 삭제는 사용자에게 묻는다.
