# 프리셋 탭 (2) 역천개명 계열 보호와 프리셋 10개 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ] `) syntax for tracking.

**Goal:** 역천개명의 계열 규칙을 운명 탭·캐릭터 가져오기·프리셋 전환에서 모두 지키게 하고, 기본 프리셋 10개의 내용을 채워 문서화한 뒤 `develop`에 병합하고 게임에 설치한다.

**Architecture:** 계열 규칙(같은 계열의 역천개명은 하나)은 `FateService` 한곳에 둔다: 함께 둘 수 없는 것을 가진 대경지를 찾는 함수와, 곧 바꿀 칸을 비우는 함수. `FateService.Set`과 `LuckService.Add`는 그 함수로 거부하고, 프리셋 적용은 이번에 바꿀 칸이면 먼저 비운 뒤 정한다(그래야 프리셋에서 프리셋으로 갈아탈 수 있다). 프리셋 10개는 공략을 다시 읽어 이름을 게임의 번호로 대조하고, 개발 명령 `pcheck`와 검사 `preset-all`이 통과해야 끝난 것이다.

**Tech Stack:** C# 7.3 / .NET Framework 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4, PowerShell 7 검증 스크립트, `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-14-preset-tab-design.md`(6절이 프리셋 10개, 9절이 문서). 첫째 계획: `docs/superpowers/plans/2026-10-14-preset-tab-1-engine.md`.

## Global Constraints

- 첫째 계획의 Global Constraints를 그대로 따른다(대상 게임과 폴더, DLL 하나, 두 구성의 경고·오류 0, 회차 규칙, 코드 모양, 커밋 메시지의 `Co-Authored-By`).
- 브랜치는 `feat/preset-tab`. 이 계획의 끝에서 `git merge --no-ff`로 `develop`에 병합하고 `tools/install.ps1`로 설치한다. `main`에는 병합하지 않는다. 병합한 브랜치의 삭제는 사용자에게 확인받는다.
- 회차 이름은 `pbuild1`부터(이미 쓴 이름: `preset1`·`preset2`, `preset-1014a`~`d`, `ptab1`~`ptab25`와 `ptab8b`~`ptab8e`·`ptab21b`).
- 역천개명은 계열(`FateFeature.group`, 0이 아님)마다 하나다. 같은 계열을 다른 대경지에 붙이면 앞의 운명이 사라지고 기록만 남는다(실측, 스펙 3절). 판정은 기록이 아니라 운명의 보유(`LuckService.Has`)로 한다.
- 프리셋의 번호는 게임에서 확인한 것만 쓴다. 공략의 이름을 번호로 바꿀 때는 개발 명령으로 대조하고, 찾지 못한 것은 넣지 않고 05 문서에 "찾지 못함"으로 적는다. 추측으로 번호를 넣지 않는다.
- 공략에 없어 채운 줄에는 `"filled": true`를 붙인다. 공략에 없는 기령·법보·명우는 넣지 않는다(스펙 6절).
- 기본 프리셋의 파일 이름은 `01-…json`~`10-…json`(스펙 6절의 순서). 기본 10개는 `pcheck`의 `problems=0`이어야 한다.
- 문서에는 어셈블리나 게임에서 실제로 확인한 이름만 적고, 확인하지 못한 것은 "미확인"과 사유를 적는다.

## Review Focus

1. 운명 탭에서 같은 계열의 역천개명을 다른 대경지에 정함 → 거부하고 앞의 운명이 남는다. (Task 1 `fate-set`의 "is refused")
2. 프리셋 A를 최종 단계까지 적용한 캐릭터에 프리셋 B를 적용 → 못 넣음 없이 B의 역천개명이 모두 실제로 붙는다. (Task 1 `apply-switch`)
3. 두 대경지의 역천개명이 서로 맞바뀐 프리셋 → 수렴한다. (Task 1 `apply-switch`의 맞바꾼 프리셋)
4. 기본 프리셋 10개를 차례로 적용 → 프리셋마다 못 넣음이 없고 장착 칸이 그 프리셋의 것이다. (Task 4 `preset-all`)
5. 기본 프리셋의 옵션이 자리 규칙에 맞음, 심법 유형이 겹치지 않음 → `pcheck`가 0. (Task 4)

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `src/ModCode/ModMain/Services/FateService.cs` | 수정 | `HeldElsewhere`, `Release`, `Set`의 계열 거부 |
| `src/ModCode/ModMain/Services/LuckService.cs` | 수정 | `Add`의 계열 거부(역천개명 분류) |
| `src/ModCode/ModMain/Services/PresetService.Apply.cs` | 수정 | 역천개명: 이번에 바꿀 칸이면 비우고 정한다 |
| `src/ModCode/ModMain/Services/PresetService.cs` | 수정 | 심법의 표시 이름 |
| `src/ModCode/ModMain/Dev/DevCommandsChar.cs` | 수정 | `cfatealt`·`cluck3`가 함께 둘 수 있는 후보만 고른다 |
| `src/ModCode/ModMain/Dev/DevCommandsPreset.cs` | 수정 | `pfateset`, `pluckadd`, `popts`, 이름 찾기(`pfind`·`pfate`·`pluck`·`psuit`·`pconf`) |
| `tools/research/preset-check.ps1` | 수정 | 부분 `fate-set`, `apply-switch`, `preset-all` |
| `src/ModCode/ModMain/Presets/01-…json`~`10-…json` | 새로·수정 | 기본 프리셋 10개(`07`은 초안을 완성) |
| `docs/05-editor-v0.1.md` | 수정 | 34절, 3절의 예외 |
| `docs/06-handoff.md` | 수정 | 현재 상태로 다시 정리 |
| `docs/superpowers/specs/2026-10-14-preset-tab-design.md` | 수정 | 구현하며 달라진 것 |

---

### Task 1: 역천개명의 계열 보호 — 운명 탭, 캐릭터 가져오기, 프리셋 전환

**Files:**
- Modify: `src/ModCode/ModMain/Services/FateService.cs`(`Set` 안과 `Set` 위)
- Modify: `src/ModCode/ModMain/Services/LuckService.cs`(`Add`)
- Modify: `src/ModCode/ModMain/Services/PresetService.Apply.cs`(`PutFate`, `Apply`)
- Modify: `src/ModCode/ModMain/Dev/DevCommandsChar.cs`(`cfatealt`, `cluck3`), `Dev/DevCommandsPreset.cs`(`pfateset`, `pluckadd`)
- Modify: `tools/research/preset-check.ps1`(부분 `fate-set`, `apply-switch`)

**Interfaces:**
- Consumes: `FateService.Records`, `Log`(private), `GradeName`(private), `LuckService.Has`, 첫째 계획의 `PresetService`·검사 함수(`NewTestPreset`, `LoadPicked`, `SavePicked`, `WritePreset`, `Fields`, `State`).
- Produces:
  - `FateService.HeldElsewhere(WorldUnitBase unit, int exceptGrade, int newId) : int` — 그 운명과 함께 둘 수 없는 역천개명(같은 번호, 또는 같은 계열)을 실제로 가진 다른 대경지. 없으면 0.
  - `FateService.Release(WorldUnitBase unit, int grade) : bool` — 그 대경지의 역천개명의 효과를 떼고 기록의 번호를 0으로 한다.
  - 개발 명령 `pfateset <대경지> <번호>` → `FateService.Set`의 문장, `pluckadd <번호>` → `LuckService.Add`의 문장.

- [ ] **Step 1: 검사를 먼저 쓴다**

`preset-check.ps1`의 `'fate-groups' { … }` 아래에 넣는다. 700001·700002는 계열 1이고 700001은 대경지 2부터, 700002는 3부터다(설정 표 `FateFeature`). 고정본의 기록은 `2:700016`(계열 없음), `3:700040`(계열 9).

```powershell
  'fate-set' {
    # 운명 탭과 캐릭터 가져오기가 쓰는 FateService.Set, 운명 추가가 쓰는 LuckService.Add의 계열 보호.
    $r = Cmd 'pfateset 3 700002' 800
    Check 'a fate of group 1 is set at major grade 3' ((State).fates -eq '2:700016,3:700002' -and (Cmd 'phas 700002' 200) -match 'held=1 ') $r
    $r = Cmd 'pfateset 2 700001' 800
    Check 'a fate of the same group at another grade is refused' ((State).fates -eq '2:700016,3:700002' -and (Cmd 'phas 700002,700016' 200) -match 'held=2 of=2') $r
    $r = Cmd 'pluckadd 700001' 800
    Check 'adding a fate luck of the same group is refused' ((Cmd 'phas 700001' 200) -match 'held=0 ' -and (Cmd 'phas 700002' 200) -match 'held=1 ') $r
    $r = Cmd 'pfateset 3 700001' 800
    Check 'the same group at the same grade replaces' ((State).fates -eq '2:700016,3:700001' -and (Cmd 'phas 700001' 200) -match 'held=1 ' -and (Cmd 'phas 700002' 200) -match 'held=0 ') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'apply-switch' {
    # 프리셋에서 프리셋으로: A를 최종 단계까지 적용한 뒤, 역천개명 둘이 서로 맞바뀐 B를 적용한다. 못 넣음 없이 B의 것이 모두 실제로 붙어야 한다.
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    $picked = NewTestPreset
    $file = 'uicheck-preset.json'
    $r = Cmd "papply $file full" 9000
    Check 'preset A is applied' ((Fields $r).failed -eq '0') $r
    $a = [IO.File]::ReadAllText((Join-Path $presetDir $file))
    $nine = $picked.fates['9']; $ten = $picked.fates['10']
    $swapped = $a.Replace('"uicheck preset"', '"uicheck swapped"').Replace("{ `"grade`": 9, `"id`": $nine }", "{ `"grade`": 9, `"id`": $ten }").Replace("{ `"grade`": 10, `"id`": $ten }", "{ `"grade`": 10, `"id`": $nine }")
    WritePreset 'uicheck-swapped.json' $swapped
    Check 'the swapped preset reads without unusable lines' ((Fields (Cmd 'pcheck uicheck-swapped.json' 600)).problems -eq '0') (Cmd 'pcheck uicheck-swapped.json' 600)
    $r = Cmd 'papply uicheck-swapped.json all' 6000
    Check 'preset B is applied without a failed line' ((Fields $r).failed -eq '0') $r
    $want = ((2..8 | ForEach-Object { "$($_):$($picked.fates["$_"])" }) + "9:$ten", "10:$nine") -join ','
    Check 'the two fates are swapped in the records' ((State).fates -eq $want) (State).fates
    $ids = (2..10 | ForEach-Object { $picked.fates["$_"] }) -join ','
    Check 'every fate luck is actually held' ((Cmd "phas $ids" 300) -match 'held=9 of=9') (Cmd "phas $ids" 300)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
```

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild1 -Fixture base
pwsh tools/research/preset-check.ps1 -Part apply-switch
pwsh tools/DevRemote/session.ps1 -End pbuild1
```

Expected: "preset B is applied without a failed line"과 "every fate luck is actually held"가 FAIL(지금은 다른 대경지에 가진 운명을 거부한다). `fate-set`은 명령이 아직 없어 이 단계에서는 돌리지 않는다.

- [ ] **Step 3: `FateService`에 함수 둘을 더하고 `Set`이 거부하게 한다**

`FateService.cs`의 `Set` 메서드 바로 위에 넣는다.

```csharp
        // 그 운명과 함께 둘 수 없는 역천개명을 실제로 가진 다른 대경지. 없으면 0.
        // 게임은 같은 계열(fateFeature.group, 0이 아님)의 역천개명을 하나만 둔다. 같은 계열을 또 붙이면 앞의 운명이 사라지고
        // 경지별 기록만 남아 겉으로는 정상으로 보인다(실측, 프리셋 스펙 3절). 같은 번호를 두 대경지에 둘 수도 없다.
        public static int HeldElsewhere(WorldUnitBase unit, int exceptGrade, int newId)
        {
            ConfFateFeatureItem fate = g.conf.fateFeature.GetItem(newId);
            foreach (var pair in Records(unit))
            {
                if (pair.Key == exceptGrade || pair.Value == 0 || !LuckService.Has(unit, pair.Value))
                {
                    continue;
                }
                ConfFateFeatureItem other = g.conf.fateFeature.GetItem(pair.Value);
                if (pair.Value == newId || (fate != null && other != null && fate.group != 0 && other.group == fate.group))
                {
                    return pair.Key;
                }
            }
            return 0;
        }

        // 그 대경지의 역천개명의 효과를 떼고 기록의 번호를 0으로 한다. 곧 다른 것으로 정할 칸을 먼저 비울 때 쓴다.
        // 번호를 남겨 두면, 그 번호를 다른 대경지에 다시 붙인 뒤 이 칸을 정할 때 Set이 "이전 것"으로 보고 지운다.
        public static bool Release(WorldUnitBase unit, int grade)
        {
            var log = Log(unit);
            if (log == null || !log.ContainsKey(grade))
            {
                return false;
            }
            int id = log[grade].luck;
            if (id != 0 && LuckService.Has(unit, id))
            {
                unit.CreateAction(new UnitActionLuckDel(id), false);
            }
            if (id != 0 && LuckService.Has(unit, id))
            {
                return false;
            }
            log[grade].luck = 0;
            return true;
        }
```

`Set` 안에서 `if (LuckService.Has(unit, newId)) { return name + ": 이미 가지고 있습니다."; }` 블록 바로 아래에 넣는다.

```csharp
            int clash = HeldElsewhere(unit, grade, newId);
            if (clash != 0)
            {
                ConfRoleCreateFeatureItem held = g.conf.roleCreateFeature.GetItem(Records(unit)[clash]);
                return name + ": 같은 계열의 역천개명(" + (held == null ? "?" : GameTool.LS(held.name)) + ")을 " + GradeName(clash)
                    + "에 가지고 있어 함께 둘 수 없습니다. 그 경지의 역천개명을 먼저 바꾸세요.";
            }
```

- [ ] **Step 4: `LuckService.Add`가 역천개명 분류의 계열을 보게 한다**

`LuckService.cs`의 `Add`에서 `if (Has(unit, id)) { … }` 블록 바로 아래에 넣는다.

```csharp
            // 역천개명 분류(3)는 같은 계열을 하나만 둘 수 있다. 붙이면 게임이 같은 계열의 역천개명을 없애고 그 경지의 기록만 남긴다(실측).
            if (conf.type == 3 && FateService.HeldElsewhere(unit, 0, id) != 0)
            {
                return "운명 " + GameTool.LS(conf.name) + ": 같은 계열의 역천개명을 이미 가지고 있어 함께 둘 수 없습니다. 운명 탭의 역천개명에서 그 경지의 것을 바꾸세요.";
            }
```

- [ ] **Step 5: 프리셋 적용이 이번에 바꿀 칸을 비우고 정하게 한다**

`PresetService.Apply.cs`의 `PutFate`를 통째로 바꾼다.

```csharp
        // replacing: 이번 적용이 정할 대경지들. 함께 둘 수 없는 것을 가진 대경지가 여기 있으면 그 칸을 먼저 비운다(곧 다른 것으로 정해진다).
        private static Outcome PutFate(WorldUnitBase me, FateLine line, List<int> replacing, out string text)
        {
            string label = Named(line.Name, line.Id);
            if (line.Problem != null)
            {
                text = label + ": " + line.Problem;
                return Outcome.Failed;
            }
            if (FateNow(me, line.Grade) == line.Id && LuckService.Has(me, line.Id))
            {
                text = label;
                return Outcome.Skipped;
            }
            // 같은 번호나 같은 계열을 다른 대경지에 가지고 있으면 함께 둘 수 없다(게임이 앞의 운명을 없애고 기록만 남긴다).
            for (int tries = 0; tries < StageCount; tries++)
            {
                int clash = FateService.HeldElsewhere(me, line.Grade, line.Id);
                if (clash == 0)
                {
                    break;
                }
                if (!replacing.Contains(clash) || !FateService.Release(me, clash))
                {
                    text = label + ": " + StageName(clash) + "의 역천개명과 같은 계열이라 함께 둘 수 없습니다. 그 경지의 역천개명을 먼저 바꾸세요";
                    return Outcome.Failed;
                }
            }
            text = FateService.Set(me, line.Grade, line.Id);
            return FateNow(me, line.Grade) == line.Id && LuckService.Has(me, line.Id) ? Outcome.Done : Outcome.Failed;
        }
```

`Apply`의 역천개명 반복을 바꾼다. 앞의 것:

```csharp
            foreach (FateLine each in preset.Fates)
            {
                FateLine line = each;
                if (line.Grade >= fateFrom && line.Grade <= fateTo)
                {
                    fates.Count(Named(line.Name, line.Id), (out string text) => PutFate(me, line, out text));
                }
            }
```

뒤의 것:

```csharp
            var replacing = new List<int>();
            foreach (FateLine each in preset.Fates)
            {
                if (each.Problem == null && each.Grade >= fateFrom && each.Grade <= fateTo)
                {
                    replacing.Add(each.Grade);
                }
            }
            foreach (FateLine each in preset.Fates)
            {
                FateLine line = each;
                if (line.Grade >= fateFrom && line.Grade <= fateTo)
                {
                    fates.Count(Named(line.Name, line.Id), (out string text) => PutFate(me, line, replacing, out text));
                }
            }
```

- [ ] **Step 6: 개발 명령**

`Dev/DevCommandsPreset.cs`의 `switch`에 넣는다.

```csharp
                case "pfateset": return FateService.Set(me, int.Parse(p[1]), int.Parse(p[2]));
                case "pluckadd": return LuckService.Add(me, int.Parse(p[1]));
```

`Dev/DevCommandsChar.cs`의 `cluck3`와 `cfatealt`에서 후보를 고르는 조건 `if (!LuckService.Has(me, view.Id))`를 둘 다 다음으로 바꾼다(`cfatealt`는 `grade`, `cluck3`는 `0`을 준다). 함께 둘 수 없는 후보를 고르면 이제 거부되어 기존 검사(`charvault`, `fateguard`)가 엉뚱하게 실패한다.

```csharp
                            if (!LuckService.Has(me, view.Id) && FateService.HeldElsewhere(me, grade, view.Id) == 0)
```

```csharp
                            if (!LuckService.Has(me, view.Id) && FateService.HeldElsewhere(me, 0, view.Id) == 0)
```

- [ ] **Step 7: 빌드하고 검사가 통과하는 것을 본다**

두 구성으로 빌드한다(첫째 계획의 공통 절차). Expected: 오류 0, 경고 0.

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild2 -Fixture base
pwsh tools/research/preset-check.ps1 -Part fate-set
pwsh tools/research/preset-check.ps1 -Part fate-groups
pwsh tools/research/ui-check.ps1 -Part fateguard
pwsh tools/DevRemote/session.ps1 -End pbuild2
```

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild3 -Fixture base
pwsh tools/research/preset-check.ps1 -Part apply-switch
pwsh tools/DevRemote/session.ps1 -End pbuild3
```

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild4 -Fixture base
pwsh tools/research/preset-check.ps1 -Part apply
pwsh tools/research/preset-check.ps1 -Part apply-full
pwsh tools/research/ui-check.ps1 -Part charvault
pwsh tools/DevRemote/session.ps1 -End pbuild4
```

Expected: 모든 부분 `FAILS=0`. `fate-set`은 Step 2에서 보지 못했으므로, 통과한 뒤 `Set`의 `clash` 블록을 잠깐 주석으로 막고 한 번 더 돌려 "is refused"가 FAIL하는 것을 확인하고 되돌린다(회차 `pbuild2b`).

- [ ] **Step 8: 커밋**

```bash
git add src/ModCode/ModMain/Services/FateService.cs src/ModCode/ModMain/Services/LuckService.cs src/ModCode/ModMain/Services/PresetService.Apply.cs src/ModCode/ModMain/Dev tools/research/preset-check.ps1
git commit -m "fix: 역천개명의 계열 규칙을 운명 탭·캐릭터 가져오기·프리셋 전환에서 지킨다

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 프리셋을 쓰는 도구 — 이름 찾기, 옵션 목록, 심법의 표시 이름

**Files:**
- Modify: `src/ModCode/ModMain/Dev/DevCommandsPreset.cs`(명령 `pfind`, `pfate`, `pluck`, `psuit`, `pconf`, `popts`)
- Modify: `src/ModCode/ModMain/Services/PresetService.cs`(`Finish`)
- Modify: `tools/research/preset-check.ps1`(부분 `files`에 한 줄)

**Interfaces:**
- Consumes: 실험 브랜치 `feat/preset-research`의 `Dev/DevCommandsPreset.cs`에 있는 `Each`, `Name`, `FindSkill`, `FindFate`, `FindLuck`, `FindSuit`, `Conf`. `DevCommandsPreset.EntryOf`(첫째 계획).
- Produces: 개발 명령
  - `pfind <이름…>` — 기술 이름(화면 표기의 일부)으로 `[분류 번호 이름 g최소-최대]`.
  - `pfate <이름…>` — 역천개명 `[번호 이름 g최소-최대 group계열 lv등급]`.
  - `pluck <이름…>` — 운명 `[번호 이름 type분류 lv등급]`.
  - `psuit [이름…]` — 심법 세트 `[번호 이름 type member=… cond=…]`.
  - `pconf <elder|sprite|wing|heart>` — 설정 표의 번호와 이름.
  - `popts <기술 번호>` — 그 기술에 넣을 수 있는 옵션마다 `#번호 multi=한도 require=조건 slots=나올 수 있는 자리 설명`.
  - 프리셋 화면과 `pstage`의 심법 이름이 `이름(번호)` 모양이 된다.

- [ ] **Step 1: 검사를 먼저 쓴다**

`preset-check.ps1`의 `'files'` 부분에서 `Check 'the defaults have no unusable line' …` 줄 아래에 넣는다.

```powershell
    Check 'the name search finds a skill by its shown name' ((Cmd 'pfind 13111' 300) -match '\[0 13111 \S+ g1-10\]') (Cmd 'pfind 13111' 300)
    Check 'the option list of a skill is printed' ((Cmd 'popts 13111' 300) -match '#1 multi=\d+ require=\S* slots=[\d,]+ ') (Cmd 'popts 13111' 300)
    Check 'an ability is shown with its number' ((Cmd "pstage $(($onDisk | Sort-Object Name | Select-Object -First 1).Name) 3" 400) -match '\(81001\)') (Cmd "pstage $(($onDisk | Sort-Object Name | Select-Object -First 1).Name) 3" 400)
```

`pfind`는 이름의 일부로 찾지만 번호를 줘도 그 번호의 기술을 돌려준다(`SkillCatalog.Find`가 번호도 받는다).

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild5 -Fixture base
pwsh tools/research/preset-check.ps1 -Part files
pwsh tools/DevRemote/session.ps1 -End pbuild5
```

Expected: 새 세 줄이 FAIL(`pfind`·`popts`는 `UNKNOWN`, 심법 이름에 번호가 없다).

- [ ] **Step 3: 이름 찾기 명령을 옮긴다**

실험 브랜치의 파일에서 메서드 일곱을 `Dev/DevCommandsPreset.cs`의 클래스 끝에 옮긴다. 옮길 메서드: `Each`, `Name`, `FindSkill`, `FindFate`, `FindLuck`, `FindSuit`, `Conf`. 내용은 그대로 둔다.

```bash
git show feat/preset-research:src/ModCode/ModMain/Dev/DevCommandsPreset.cs > "$TEMP/preset-research.cs"
grep -n "private static string \(Each\|Name\|FindSkill\|FindFate\|FindLuck\|FindSuit\|Conf\|State\)" "$TEMP/preset-research.cs"
```

출력의 줄 번호로 `Each`의 첫 줄부터 `State`의 바로 앞 줄까지가 옮길 범위다(일곱 메서드가 이어져 있다). 그 범위를 클래스의 닫는 중괄호 앞에 붙인다. `switch`에 넣는다.

```csharp
                case "pfind": return Each(p, FindSkill);
                case "pfate": return Each(p, FindFate);
                case "pluck": return Each(p, FindLuck);
                case "psuit": return p.Length > 1 ? Each(p, FindSuit) : FindSuit("");
                case "pconf": return Conf(p[1]);
                case "popts": return OptionList(int.Parse(p[1]));
```

`OptionList`를 더한다.

```csharp
        // 그 기술에 넣을 수 있는 옵션마다: 번호, 중복 한도, 선행(양수)·배타(음수) 조건, 나올 수 있는 자리(1부터), 설명.
        // 프리셋의 options를 쓸 때 본다. 자리는 설정 표의 weight1~weight11이 0보다 큰 칸이다.
        private static string OptionList(int baseId)
        {
            MartialType type;
            SkillCatalog.Entry entry = EntryOf(baseId, out type);
            if (entry == null)
            {
                return "no skill " + baseId;
            }
            var sb = new StringBuilder(entry.Name + " type=" + (int)type);
            var items = g.conf.battleSkillPrefixValue.GetItems((int)type, baseId);
            for (int i = 0; items != null && i < items.Count; i++)
            {
                ConfBattleSkillPrefixValueItem item = items[i];
                int[] weights = { item.weight1, item.weight2, item.weight3, item.weight4, item.weight5, item.weight6,
                    item.weight7, item.weight8, item.weight9, item.weight10, item.weight11 };
                var slots = new StringBuilder();
                for (int slot = 0; slot < weights.Length; slot++)
                {
                    if (weights[slot] > 0)
                    {
                        slots.Append(slots.Length == 0 ? "" : ",").Append(slot + 1);
                    }
                }
                sb.Append("\n#").Append(item.number).Append(" multi=").Append(item.multi).Append(" require=").Append(item.require)
                    .Append(" slots=").Append(slots).Append(' ').Append(MartialService.CleanDesc(GameTool.LS(item.desc)).Replace('\n', ' '));
            }
            return sb.ToString();
        }
```

- [ ] **Step 4: 심법의 표시 이름에 번호를 붙인다**

게임의 심법 목록 이름은 "경", "식"처럼 유형의 끝 글자뿐이라 구별되지 않는다(첫째 계획의 화면 확인). `PresetService.cs`의 `Finish`에서 `line.Name = entry.Name;`을 바꾼다.

```csharp
            // 심법의 목록 이름은 유형의 끝 글자("경", "식")뿐이라 번호를 붙여 구별한다.
            line.Name = line.Type == MartialType.Ability ? entry.Name + "(" + line.BaseId + ")" : entry.Name;
```

- [ ] **Step 5: 빌드하고 검사가 통과하는 것을 본다**

두 구성으로 빌드한다. Expected: 오류 0, 경고 0.

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild6 -Fixture base
pwsh tools/research/preset-check.ps1 -Part files
pwsh tools/research/preset-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End pbuild6
```

Expected: 둘 다 `FAILS=0`.

- [ ] **Step 6: 커밋**

```bash
git add src/ModCode/ModMain/Dev/DevCommandsPreset.cs src/ModCode/ModMain/Services/PresetService.cs tools/research/preset-check.ps1
git commit -m "feat: 프리셋을 쓰는 개발 명령(이름 찾기, 옵션 목록), 심법 이름에 번호

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 공략을 다시 읽고 빌드 10개의 내용을 정한다

**Files:**
- Create: `docs/superpowers/specs/2026-10-14-preset-builds.md`(빌드마다의 내용과 근거. 05 문서 34절의 바탕이 된다)

**Interfaces:**
- Consumes: 스펙 6절의 표(빌드 10개와 출처), Task 2의 이름 찾기 명령.
- Produces: 빌드마다 한 절인 문서. 절마다 다음 표가 있다: 칸(무예·절기·신법·신통, 심법 8유형, 선천운명, 대경지 2~10의 역천개명, 법보·기령, 신혼, 명우) / 공략의 표기 / 게임의 번호와 이름 / 공략에 있음·채움 / 옵션 번호. Task 4가 이 표를 JSON으로 옮긴다.

- [ ] **Step 1: 출처를 다시 읽는다**

스펙 6절의 출처 열 개를 웹에서 다시 연다. 빌드마다 공략이 **이름을 대어 말한 것**만 적는다: 기술, 옵션(이름이나 효과), 심법 세트, 역천개명과 그 대경지, 기령, 명우, 신혼. 공략이 방향만 준 것(예: "검법 + 풍신법")은 방향으로 적는다. 열리지 않는 출처는 "열리지 않음"으로 적고 다른 출처로 바꾸지 않는다(2026-10-13의 조사 범위를 지킨다).

- [ ] **Step 2: 이름을 게임의 번호로 대조한다**

회차 하나를 열어 이름마다 명령으로 찾는다. 공략의 한국어·중국어 표기가 게임의 한국어 표기와 다를 수 있으므로 이름의 일부로 찾고, 후보가 여럿이면 설명을 읽어 고른다. 찾지 못한 것은 "찾지 못함"으로 적는다.

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild7 -Fixture base
pwsh -Command "& tools/DevRemote/mod.ps1 -Cmd 'pfind 천심모 질룡파 패왕순'"
pwsh -Command "& tools/DevRemote/mod.ps1 -Cmd 'pfate 혈마 살심 무신'"
pwsh -Command "& tools/DevRemote/mod.ps1 -Cmd 'psuit'"
pwsh -Command "& tools/DevRemote/mod.ps1 -Cmd 'pconf elder' , 'pconf sprite', 'pconf wing'"
pwsh -Command "& tools/DevRemote/mod.ps1 -Cmd 'popts 13111'"
pwsh tools/DevRemote/session.ps1 -End pbuild7
```

기록할 것: 기술마다 번호·분류·경지 범위, 역천개명마다 번호·경지 범위·계열, 세트마다 번호와 구성원, 옵션마다 번호와 나올 수 있는 자리. 한 회차가 10분을 넘지 않게 빌드 서넛씩 나눠 연다(`pbuild7`, `pbuild8`, `pbuild9`).

- [ ] **Step 3: 빈 칸을 채운다**

스펙 6절의 "채우는 규칙"을 그대로 적용하고 채운 줄에 "채움"을 적는다. 지킬 것:

- 역천개명은 대경지마다 하나, **계열이 서로 달라야** 한다(`pfate`의 `group`. 0은 계열 없음). 같은 계열의 높은 등급이 있으면 그 계열에서는 가장 높은 등급 하나만 쓴다.
- 역천개명의 경지 범위(`g최소-최대`)가 그 대경지를 품어야 한다.
- 심법은 유형(번호의 앞 두 자리 81~88)마다 하나. 세트 번호는 그 심법이 세트의 구성원일 때만 적는다.
- 옵션은 `popts`의 `slots`에 그 자리가 있는 번호만, `multi`를 넘지 않게, `require`의 선행(양수)을 함께 넣고 배타(음수)를 피한다. 11개까지.
- 공략에 없는 기령·법보·명우는 넣지 않는다. 신혼은 주 능력치에 맞춘다(`pconf elder`의 성장 능력치).

- [ ] **Step 4: 문서를 쓰고 커밋한다**

`docs/superpowers/specs/2026-10-14-preset-builds.md`에 빌드 10개의 절을 쓴다. 맨 위에 조사한 날짜, 열리지 않은 출처, 찾지 못한 이름의 목록을 둔다.

```bash
git add docs/superpowers/specs/2026-10-14-preset-builds.md
git commit -m "docs: 프리셋 10개의 내용 — 공략의 표기와 게임의 번호 대조

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: 기본 프리셋 10개를 쓰고 검증한다

**Files:**
- Create: `src/ModCode/ModMain/Presets/01-…json`~`06-…json`, `08-…json`~`10-…json`
- Modify: `src/ModCode/ModMain/Presets/07-창-과룡문.json`(초안을 완성)
- Modify: `tools/research/preset-check.ps1`(부분 `preset-all`)

**Interfaces:**
- Consumes: Task 3의 문서, 첫째 계획의 파일 형식(스펙 5.1절)과 `pcheck`·`papply`·`pstage`.
- Produces: 기본 프리셋 10개. 파일 이름은 `01-풍검.json`, `02-수검.json`, `03-화검.json`, `04-화권.json`, `05-도.json`, `06-수영술.json`, `07-창-과룡문.json`, `08-화영술.json`, `09-목화.json`, `10-뇌영술.json`.

- [ ] **Step 1: 검사를 먼저 쓴다**

`preset-check.ps1`의 `'apply-switch' { … }` 아래에 넣는다.

```powershell
  'preset-all' {
    # 기본 프리셋 10개를 차례로 최종 단계까지 적용한다. 프리셋마다 못 넣음이 없고, 마지막 단계가 "적용됨"이어야 한다.
    # 앞 프리셋의 역천개명·신혼·명우를 다음 프리셋이 바꾼다(프리셋에서 프리셋으로 갈아타기).
    if (Test-Path -LiteralPath $presetDir) { [IO.Directory]::Delete($presetDir, $true) }
    $c = Fields (Cmd 'pcheck' 900)
    Check 'there are ten default presets' ((Fields (Cmd 'pdefaults' 200)).embedded -eq '10' -and $c.presets -eq '10' -and $c.unreadable -eq '0') "embedded=$((Fields (Cmd 'pdefaults' 200)).embedded) presets=$($c.presets)"
    Check 'no default preset has an unusable line' ($c.problems -eq '0') (Cmd 'pcheck' 900)
    foreach ($file in Get-ChildItem -LiteralPath $presetDir -Filter *.json | Sort-Object Name) {
      $r = Cmd "papply $($file.Name) full" 9000
      $run = Fields $r
      Check "$($file.BaseName.Substring(0, 2)): nothing failed" ($run.failed -eq '0') $r
      $st = Fields (Cmd "pstage $($file.Name) 10" 500)
      Check "$($file.BaseName.Substring(0, 2)): the last stage reads as applied" ($st.state -eq 'Applied') (Cmd "pstage $($file.Name) 10" 500)
      Check "$($file.BaseName.Substring(0, 2)): ability points cover what is in use" ([int](State).used -le [int](State).ap) "used=$((State).used) ap=$((State).ap)"
    }
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
```

파일 이름에 한글이 들어가므로 `papply`의 인자는 공백이 없어야 한다(파일 이름에 공백을 쓰지 않는다).

- [ ] **Step 2: 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild10 -Fixture base
pwsh tools/research/preset-check.ps1 -Part preset-all
pwsh tools/DevRemote/session.ps1 -End pbuild10
```

Expected: "there are ten default presets"가 FAIL(`embedded=1`).

- [ ] **Step 3: 파일 열 개를 쓴다**

Task 3의 문서를 줄 단위로 옮긴다. 형식은 스펙 5.1절이다. `07`의 초안을 본보기로 삼고, 줄마다:

- `name`에는 게임의 한국어 이름을 적는다(사람이 읽기 위한 것).
- 공략에 없어 채운 줄에는 `"filled": true`.
- `summary`는 한 줄(핵심 기술과 운용), `source`는 스펙 6절의 출처 그대로.
- `07`의 `summary`에서 "(초안)"을 뺀다.

- [ ] **Step 4: `pcheck`가 0이 될 때까지 고친다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release
pwsh tools/DevRemote/session.ps1 -Begin pbuild11 -Fixture base
pwsh -Command "& tools/DevRemote/mod.ps1 -Cmd 'pcheck' -WaitMs 900"
pwsh tools/DevRemote/session.ps1 -End pbuild11
```

Expected: `presets=10 unreadable=0 problems=0`. 문제가 있으면 ` | ` 뒤에 파일과 줄과 이유가 나온다. 이유에 맞춰 **파일을 고친다**(검사를 느슨하게 하지 않는다). 옵션의 자리 문제는 `popts`의 `slots`를 다시 보고 순서를 바꾸거나 다른 옵션으로 바꾼다. DLL에 든 파일을 고쳤으므로 다시 빌드해 새 회차에서 확인한다.

- [ ] **Step 5: 열 개를 적용해 본다**

`preset-all`은 프리셋마다 경지 올리기(첫 번째만)와 공법 열한 개쯤을 만든다. 10분 안에 끝나지 않으면 검사의 `foreach`를 파일 이름의 앞 두 글자로 나눠 두 회차로 돌린다(`01`~`05`, `06`~`10`).

```bash
pwsh tools/DevRemote/session.ps1 -Begin pbuild12 -Fixture base
pwsh tools/research/preset-check.ps1 -Part preset-all
pwsh tools/DevRemote/session.ps1 -End pbuild12
```

Expected: `FAILS=0`. "nothing failed"가 실패하면 `papply`의 문장에서 못 넣은 줄의 이유를 읽는다. 장착 거부(코드)나 게임이 만들지 못한 공법이면 그 줄의 번호·경지·옵션을 다시 대조한다.

전투 한 번으로 마지막 프리셋의 세트와 명우가 켜지는지 본다(같은 회차의 끝에).

```bash
pwsh tools/research/preset-check.ps1 -Part extras-battle
```

Expected: 명우가 든 프리셋이 마지막이면 "the wingman is in the battle"이 PASS. 마지막 프리셋에 명우가 없으면 그 줄은 FAIL이 맞다 — 그때는 `pbattle`의 `suits`에 그 프리셋의 세트 번호와 구성원 수가 있는지만 본다.

- [ ] **Step 6: 커밋**

```bash
git add src/ModCode/ModMain/Presets tools/research/preset-check.ps1
git commit -m "feat: 기본 프리셋 10개

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: 전체 검증, 문서, 병합, 설치

**Files:**
- Modify: `docs/05-editor-v0.1.md`(34절 추가, 3절의 "경지 ID를 직접 쓰지 않는다"에 예외)
- Modify: `docs/06-handoff.md`
- Modify: `docs/superpowers/specs/2026-10-14-preset-tab-design.md`(구현하며 달라진 것)

**Interfaces:**
- Consumes: 두 계획의 모든 것, 첫째 계획에서 미뤄 둔 사소한 것 열한 가지와 판단 보류 셋.
- Produces: `develop`에 병합된 프리셋 탭, 게임에 설치된 배포 빌드, 현재 상태의 인계 문서.

- [ ] **Step 1: 두 구성의 빌드와 회차 스크립트의 시험**

```bash
pwsh tools/DevRemote/session.tests.ps1
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release -p:DevTools=false -o "$TEMP/tltoybox-release-check"
ls "$TEMP/tltoybox-release-check" | grep -c -i "\.dll$"
```

Expected: `FAILS=0`(23개), 두 빌드 오류 0·경고 0, DLL 수 `1`.

- [ ] **Step 2: 모든 검사를 깨끗한 회차에서 다시 돌린다**

프리셋: `state`·`martial`·`files`·`grade` / `extras`·`extras-battle` / `fate-set`·`fate-groups`·`apply-switch` / `apply`·`apply-full`·`apply-battle`·`-Restart`·`apply-restart` / `ui` / `preset-all`. 기존: `base`·`tabs`·`confirm`·`wrap`·`fateguard` / `rates`·`vault`·`charvault`. 회차마다 새 이름(`pbuild13`~)을 쓰고 한 회차는 10분 안에 끝낸다. `ui`의 기본 프리셋 수는 이제 10이다(검사는 `pdefaults`의 수를 읽으므로 고칠 것이 없다).

Expected: 모든 부분 `FAILS=0`, 모든 `-End`가 "SAVE RESTORED AND IDENTICAL".

- [ ] **Step 3: 배포 구성으로 게임에 띄운다**

첫째 계획 Task 8 Step 4와 같다(게임 메뉴가 떠 있으면 `click 959 391`로 닫고 누른다). 캡처에 프리셋 열 줄이 보이고 `Latest.log`에 "trampoline"이 없다.

- [ ] **Step 4: 05 문서에 34절을 쓴다**

`docs/05-editor-v0.1.md`의 끝에 `## 34. 프리셋 탭 (2026-10-14 추가)`를 더한다. 다른 절(30·32절)과 같은 차례로:

- 쓰는 법: 목록의 세 버튼, 상세보기, 두 번 눌러 확인하는 경우, 파일 위치와 기본 프리셋 되돌리기.
- 적용 규칙의 표: 공법·심법(만들기, 장착 칸 비우기, 수도 점수), 역천개명(계열), 선천운명, 법보와 기령, 신혼, 명우, 경지 올리기.
- 파일 형식과 줄이 쓸 수 없는 경우.
- 프리셋 10개의 표(이름, 핵심, 출처, 공략에 있던 것과 채운 것의 수). 자세한 대조는 `docs/superpowers/specs/2026-10-14-preset-builds.md`를 가리킨다.
- 실측: 스펙 3절의 표를 옮긴다(기령, 신혼, 명우, 심법 세트, 경지, 공법, 장착 조건, 구현하며 실측한 것).
- 구조: 서비스 여섯과 탭, 개발 명령, 검사 스크립트의 부분.
- 검증 결과의 표(부분, 보는 것, PASS 수).
- 구현하며 고친 것: 법보 장착의 호출 순서, 같은 번호의 기령, 역천개명의 계열(검토에서 드러남, 기록만 보는 검사의 한계), 프리셋 전환.
- 확인하지 못한 것, 한계: 스펙 3절의 "확인하지 못한 것", 검토의 판단 보류 셋(신혼 교체 때의 경험치, 명우 효과·옵션의 개수와 중복, 재능 캐시의 키), 첫째 계획에서 미룬 사소한 것 가운데 남은 것, 공략이 2022년 것이고 채운 부분은 작성자의 판단이라는 것.

같은 문서 3절의 "경지 ID를 직접 쓰지 않는다" 줄에 예외를 더한다: 캐릭터 가져오기의 경지(30절)와 프리셋의 경지 올리기(34절).

- [ ] **Step 5: 스펙과 인계 문서를 고친다**

스펙: 4절에 "프리셋 적용은 이번에 바꿀 대경지의 역천개명을 먼저 비우고 정한다(프리셋 전환)", 5.4절의 개발 명령·검사 스크립트 이름, 7절에 두 계획의 파일 이름.

`docs/06-handoff.md`를 현재 상태로 다시 쓴다(부분 수정이 아니라 절마다 지금의 사실로):

- 1절: 직전 작업은 프리셋 탭(34절), 게임에 설치된 것의 시각.
- 2절: 탭 순서(13개, 프리셋 `Tab11`, 안전 `Tab12`)와 기능 지도의 프리셋 줄, 공통 규칙의 모드 파일에 `presets\`.
- 3절: 새 서비스·`Presets/`·`preset-check.ps1`, 05 문서의 절 수.
- 5절: `-End`가 되돌리는 것에 프리셋 폴더, 자동 검증 표에 `preset-check.ps1`의 부분과 돌리는 조건·순서, 개발 명령 목록에 프리셋 명령.
- 6절: 이번에 겪은 함정 — 게임 행동은 `IsCreateAction`을 먼저 불러야 동작하는 것이 있다, 기록만 보는 검사는 운명의 보유를 놓친다, 검사 스크립트에서 `check-lib.ps1`의 변수(`$log`, `$data`, `$work`)를 덮어쓰지 않는다, Bash의 here-doc에 작은따옴표가 든 PowerShell을 넣으면 깨진다(Write 도구로 파일을 만든다).
- 7절: 프리셋의 미확인.
- 8절: 묶음 F2를 "지금까지" 표로 옮기고, 다음 후보와 사용자에게 확인을 부탁할 것(프리셋을 실제로 써 본 이야기, 한글 파일 이름의 프리셋), 고치지 않은 사소한 것, 쓴 회차 이름.

- [ ] **Step 6: 문서를 커밋하고 `develop`에 병합한다**

```bash
git add docs
git commit -m "docs: 프리셋 탭의 기록(05 문서 34절), 스펙과 인계 문서 갱신

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git checkout develop
git merge --no-ff feat/preset-tab -m "Merge branch 'feat/preset-tab' into develop"
git log --oneline -3
```

Expected: 병합 커밋이 `develop`의 맨 위에 있다. 충돌이 없다(`develop`은 이 작업 동안 바뀌지 않았다).

- [ ] **Step 7: 게임에 설치한다**

게임이 꺼져 있어야 한다(켜져 있으면 사용자에게 끄도록 알리고 기다린다).

```bash
pwsh tools/install.ps1
```

Expected: 배포 빌드가 `E:\Games\Tale of Immortal\ModExportData\Mod_sWP6UL\ModCode\dll\MOD_sWP6UL.dll`에 설치됐다는 출력. 설치한 시각을 인계 문서 1절에 적어 `docs/handoff-…` 브랜치로 `develop`에 병합한다(직접 커밋하지 않는다).

- [ ] **Step 8: 사용자에게 보고하고 확인받을 것**

- 병합한 `feat/preset-tab` 브랜치를 지워도 되는지(삭제는 사용자 확인 뒤에).
- 도구로 확인할 수 없어 사용자가 써 봐야 하는 것: 프리셋을 실제 세이브에 적용해 플레이했을 때의 이상 여부, 신혼이 게임 화면의 어디에 나오는지, 기령 재능과 법보를 게임 화면에서 다뤘을 때.
- 고치지 않고 남긴 사소한 것의 목록.
