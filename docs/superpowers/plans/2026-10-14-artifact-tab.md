# 신기 탭 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 선법 다음에 "신기" 탭을 두어 시작 신기 셋(쌍어패·호천안·연요호)을 켜고 끄고, 쌍어옥패 점수를 바꾸고, 호천안의 손상·요수·기술·장착과 연요호의 손상·호요·경험치를 다룬다.

**Architecture:** 세계 자료를 다루는 서비스 셋을 새로 만든다: `ArtifactService`(신기 목록, 점수, 손상, 대경지 이름), `GodEyeService`(요수·기술 목록과 해금·장착), `PotmonService`(호요 종류와 가진 호요, 추가·경험치). 화면은 새 탭 `ArtifactTab`(첫 화면 + `Push`한 호천안·연요호 화면). 검증은 새 스크립트 `artifact-check.ps1`과 개발 명령 `Dev/DevCommandsArtifact.cs`.

**Tech Stack:** C# 7.3 / .NET 4.7.2, Unity 2020.3 IL2CPP + MelonLoader 0.5.4, PowerShell 7 검증 스크립트, `tools/DevRemote`.

**Spec:** `docs/superpowers/specs/2026-10-14-artifact-tab-design.md`

## Global Constraints

- 브랜치 `feat/artifact-tab`(이미 있음, 스펙 커밋 `4b5b909`). 끝에서 `git merge --no-ff`로 `develop`에 병합하고 `tools/install.ps1`로 설치. `main`에는 병합하지 않는다. 커밋 메시지 끝에 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 두 구성(개발, `-p:DevTools=false`) 모두 경고·오류 0, `bin\Release`에 DLL 하나. `Dev\`의 코드는 배포 구성에서 빠지므로 배포 코드가 `Dev\`의 것을 참조하지 않는다.
- 게임 검증은 `session.ps1 -Begin <이름> -Fixture base` … `-End <이름>` 회차 안에서. 회차 이름은 `atab1`부터 차례로, 다시 쓰지 않는다. `-End`가 "SAVE RESTORED AND IDENTICAL".
- 회차와 검사는 한 번에 하나. 도구의 한 번 실행은 10분이 한계이므로 회차를 `-Begin` + 검사 서너 부분 + `-End`로 나눈다.
- 서비스는 화면을 모르고 결과를 문장으로 돌려준다. 쓰기 전 `SaveBackup.EnsureOnce()`, 쓴 뒤 다시 읽어 판정, `ChangeLog.Add(대상, 항목, 전, 후, 확인)`.
- 신기 목록에서 마지막 하나는 빼지 않는다(스펙 3절). 호요는 28 이상이면 넣지 않는다. 쌍어옥패 점수는 0~9999.
- 쓰지 않는 것: `GodEyeData.Repair()`, `GivePisces`/`RecallPisces`, `PotmonUpgrade`, `godEyeGrade` 쓰기, 지우기(호요 방생, 기술 잊기).
- 고정본 인물: 결정 초기(경지 9, 대경지 3), 신기는 쌍어패(1)만, 쌍어옥패 점수 9, 호천안 요수·기술 없음, 호요 0.
- 문서에는 확인한 이름만 쓰고 확인하지 못한 것은 "미확인"과 사유.

## Review Focus

1. 신기를 하나만 가진 세이브에서 그 하나를 끄려 함 → 서비스가 거부하고 목록이 그대로, 화면의 끄기 버튼이 꺼지고 이유가 보인다. (Task 1 `list`, Task 2 `ui`)
2. 신기를 가지지 않은 채 호천안 해금·장착이나 호요 추가를 부름(개발 명령이나 낡은 화면) → "호천안이 없습니다"/"연요호가 없습니다"로 거부하고 자료가 바뀌지 않는다. (Task 1 `godeye`·`potmon`의 첫 줄)
3. 이미 해금하고 기술을 모두 익힌 요수를 다시 해금, 모두 해금을 두 번 → 건너뛰고 기술 수가 늘지 않는다(같은 기술 번호가 두 번 들어가지 않는다). (Task 1 `godeye`)
4. 경험치가 이미 다음 단계 경계인 호요에 경험치 채우기 → 거부하고 경험치가 그대로. (Task 1 `potmon`)
5. 점수 칸에 범위 밖 값(−5, 99999) → 0과 9999로 맞춘다. (Task 2 `ui`)

## File Structure

| 파일 | 변경 | 맡는 일 |
|---|---|---|
| `src/ModCode/ModMain/Services/ArtifactService.cs` | 새로 | 신기 목록(가짐, 켜기, 끄기), 이름·설명, 쌍어옥패 점수, 호천안·연요호 손상, 대경지와 그 이름 |
| `src/ModCode/ModMain/Services/GodEyeService.cs` | 새로 | 요수·기술 목록(`godEyeSkills`를 `bossID`로 묶음), 해금·익힘·장착 상태, 해금, 모두 해금, 장착 |
| `src/ModCode/ModMain/Services/PotmonService.cs` | 새로 | 호요 종류 목록, 가진 호요, 다음 단계 경험치, 추가, 경험치 채우기 |
| `src/ModCode/ModMain/UI/Tabs/ArtifactTab.cs` | 새로 | 첫 화면, 호천안 화면, 연요호 화면 |
| `src/ModCode/ModMain/UI/EditorWindow.cs` | 수정 | 탭 등록(선법 다음), 탭 줄 높이 `15 * 52` |
| `src/ModCode/ModMain/Dev/DevCommandsArtifact.cs` | 새로 | 검증용 명령 |
| `src/ModCode/ModMain/Dev/DevCommands.cs` | 수정 | 위 명령 연결 |
| `tools/research/artifact-check.ps1` | 새로 | 검사 부분 `list`, `godeye`, `potmon`, `ui`, `game`, `persist`, `restart` |
| `tools/research/ui-check.ps1`, `datebag-check.ps1`, `preset-check.ps1`, `immortal-check.ps1` | 수정 | 탭 이름 `Tab7` 이상을 하나씩 민다, 탭 수 |

---

### Task 1: 신기 서비스 셋과 검사

**Files:**
- Create: `src/ModCode/ModMain/Services/ArtifactService.cs`, `src/ModCode/ModMain/Services/GodEyeService.cs`, `src/ModCode/ModMain/Services/PotmonService.cs`, `src/ModCode/ModMain/Dev/DevCommandsArtifact.cs`, `tools/research/artifact-check.ps1`
- Modify: `src/ModCode/ModMain/Dev/DevCommands.cs`(`Execute`의 맨 앞)

**Interfaces:**
- Produces:
  - `ArtifactService.Ids : int[]`(1, 2, 3), `PointMax = 9999`, `ButtonNote`, `LastReason`(문장 상수)
  - `ArtifactService.Ready : bool`, `Has(int id) : bool`, `Count : int`, `Listed() : string`("1,2"), `Name(int id) : string`, `Desc(int id) : string`
  - `ArtifactService.Enable(int id) : string`, `Disable(int id) : string`
  - `ArtifactService.Point : int`, `SetPoint(int value) : string`
  - `ArtifactService.GodEyeDamaged : bool`, `GodEyeState() : string`, `RepairGodEye() : string`, `DevilDamaged : bool`, `DevilState() : string`, `RepairDevil() : string`
  - `ArtifactService.PlayerMajor() : int`, `MajorName(int major) : string`
  - `GodEyeService.Skill { int Id; int BossId; string Name; string Desc; }`, `GodEyeService.Boss { int Id; string Name; List<Skill> Skills; string Search; }`
  - `GodEyeService.Bosses() : List<Boss>`(표 순서, 캐시), `Find(string query) : List<Boss>`, `FindBoss(int id) : Boss`, `FindSkill(int id) : Skill`, `SkillCount() : int`(서로 다른 기술 수)
  - `GodEyeService.Unlocked(Boss) : bool`, `Grade(Boss) : int`, `Learned(int skillId) : bool`, `LearnedCount(Boss) : int`, `Equipped : int`
  - `GodEyeService.Unlock(Boss) : string`, `UnlockAll(List<Boss>) : string`("호천안: 요수 해금 n, 기술 m개 익힘, 건너뜀 k(이미 모두 익힘)."), `Equip(Skill) : string`
  - `PotmonService.Kind { int Id; string Name; string Race; string Search; }`, `PotmonService.Pet { string SoleId; int Id; string Name; int Grade; int Exp; int Next; }`(Next는 다음 단계 경험치, 가장 높은 단계면 −1)
  - `PotmonService.Cap = 28`, `ExpPerItem = 1000`, `MaxGrade = 10`, `Kinds() : List<Kind>`, `Find(string query) : List<Kind>`, `FindKind(int id) : Kind`, `Count : int`, `NextExp(int grade) : int`, `Owned() : List<Pet>`
  - `PotmonService.Add(Kind, int grade) : string`, `FillExp(string soleId) : string`
  - 개발 명령(아래 Step 3의 코드가 출력 형식의 기준)

- [ ] **Step 1: 검사를 먼저 쓴다**

`tools/research/artifact-check.ps1`을 만든다(이 Task에서는 `list`, `godeye`, `potmon` 부분. `ui`, `game`, `persist`, `restart`는 Task 2에서 더한다):

```powershell
# 신기 탭의 게임 내 검증(docs/superpowers/specs/2026-10-14-artifact-tab-design.md 6절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다.
# 고정본: 대경지 3(결정), 신기는 쌍어패(1)만, 쌍어옥패 점수 9, 호천안 요수·기술 없음, 호요 0.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

function Fields([string]$text) {
  $body = ($text -replace '^[^>]*-> ', '') -replace ' \| .*$', ''
  $map = @{}
  foreach ($pair in $body -split ' ') { if ($pair -match '^([^=]+)=(.*)$') { $map[$Matches[1]] = $Matches[2] } }
  $map
}
# 그 이름의 버튼이 꺼져 있는지(누르면 "not interactable").
function IsOff([string]$name) { $p = Pos $name; (Rc "click $($p.x) $($p.y)" 300) -match 'not interactable' }
function LogHas([string]$word) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 20 -Encoding UTF8 | Where-Object { $_ -match $word }).Count -gt 0 }

switch ($Part) {
  'list' {
    Check 'the fixture has only the pisces pendant' ((Fields (Cmd 'aw' 200)).weapons -eq '1') (Cmd 'aw' 200)
    $r = Cmd 'aoff 1' 400
    Check 'the last artifact is not removed' ((Fields (Cmd 'aw' 200)).weapons -eq '1' -and $r -match '하나 이상') $r
    $r = Cmd 'aon 2' 400
    Check 'turning on the god eye adds 2' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2' -and $r -match '켰습니다') $r
    $r = Cmd 'aon 2' 400
    Check 'turning it on again adds nothing' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2' -and $r -match '이미') $r
    Cmd 'aon 3' 400 | Out-Null
    Check 'turning on the gourd adds 3' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2,3') (Cmd 'aw' 200)
    $r = Cmd 'aoff 1' 400
    Check 'one of several can be turned off' ((Fields (Cmd 'aw' 200)).weapons -eq '2,3') $r
    $r = Cmd 'aoff 1' 400
    Check 'turning off a missing one says so' ($r -match '가지고 있지 않') $r
    Check 'the change log has the artifact line' (LogHas '신기 목록')
    $r = Cmd 'apoint 25' 400
    Check 'the point is set' ((Fields (Cmd 'aw' 200)).point -eq '25') $r
    $r = Cmd 'apoint -5' 400
    Check 'a negative point becomes 0' ((Fields (Cmd 'aw' 200)).point -eq '0') $r
    $r = Cmd 'apoint 123456' 400
    Check 'a point over the limit becomes 9999' ((Fields (Cmd 'aw' 200)).point -eq '9999') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'godeye' {
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'geunlock 7010' 400
    Check 'without the god eye nothing is unlocked' ($r -match '호천안이 없습니다' -and (Fields (Cmd 'gestate' 200)).bosses -eq '0') $r
    Cmd 'awset 1,2' 300 | Out-Null
    $cat = Fields (Cmd 'gecat' 300)
    Check 'the catalog groups 95 skills under 42 bosses' ($cat.bosses -eq '42' -and $cat.skills -eq '95') (Cmd 'gecat' 300)
    Check 'the first boss has a translated name' ($cat.first -match '^\D' -and $cat.first -notmatch '^요수') (Cmd 'gecat' 300)
    Cmd 'gedmg 1' 300 | Out-Null
    Check 'damage makes it unusable' ((Fields (Cmd 'gestate' 200)).canUse -eq 'False') (Cmd 'gestate' 200)
    $r = Cmd 'gerepair' 400
    $s = Fields (Cmd 'gestate' 200)
    Check 'repair makes it usable at once' ($s.canUse -eq 'True' -and $s.damaged -eq 'False' -and $s.repairMonth -eq '0') "$r | $(Cmd 'gestate' 200)"
    $r = Cmd 'geequip 100721' 400
    Check 'an unlearned skill is not equipped' ((Fields (Cmd 'gestate' 200)).equip -eq '0') $r
    $r = Cmd 'geunlock 7010' 400
    $b = Fields (Cmd 'geboss 7010' 200)
    Check 'unlock writes the player major grade and learns the boss skills' ($b.grade -eq '3' -and $b.learned -eq '3/3') "$r | $(Cmd 'geboss 7010' 200)"
    $r = Cmd 'geunlock 7010' 400
    Check 'unlocking it again adds nothing' ((Fields (Cmd 'gestate' 200)).skills -eq '3' -and $r -match '이미') $r
    $r = Cmd 'geequip 100721' 400
    Check 'a learned skill is equipped' ((Fields (Cmd 'gestate' 200)).equip -eq '100721') $r
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'geunlockall' 400
    Check 'unlock all needs the god eye too' ((Fields (Cmd 'gestate' 200)).skills -eq '3') $r
    Cmd 'awset 1,2' 300 | Out-Null
    $r = Cmd 'geunlockall' 3000
    $s = Fields (Cmd 'gestate' 200)
    Check 'unlock all opens every boss and learns every skill' ($s.bosses -eq '42' -and $s.skills -eq '95' -and $r -match '요수 해금 41, 기술 92개 익힘, 건너뜀 1') "$r | $(Cmd 'gestate' 200)"
    $r = Cmd 'geunlockall' 3000
    Check 'unlock all again skips every boss' ($r -match '요수 해금 0, 기술 0개 익힘, 건너뜀 42' -and (Fields (Cmd 'gestate' 200)).skills -eq '95') $r
    Check 'the change log has the god eye line' (LogHas '호천안')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'potmon' {
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'potadd 1001 3' 400
    Check 'without the gourd no potmon is added' ($r -match '연요호가 없습니다' -and (Fields (Cmd 'potlist' 200)).count -eq '0') $r
    Cmd 'awset 1,3' 300 | Out-Null
    $r = Cmd 'potadd 1001 3' 600
    $p = Fields (Cmd 'potlist' 200)
    Check 'a potmon is added at the chosen grade' ($p.count -eq '1' -and $p.grade -eq '3' -and $p.exp -eq '18000' -and $p.next -eq '30000') "$r | $(Cmd 'potlist' 200)"
    $r = Cmd "potfill $($p.first)" 600
    $q = Fields (Cmd 'potlist' 200)
    Check 'fill exp stops at the next grade' ($q.exp -eq '30000' -and $q.grade -eq '3') "$r | $(Cmd 'potlist' 200)"
    $r = Cmd "potfill $($p.first)" 600
    Check 'fill exp at the boundary is refused' ((Fields (Cmd 'potlist' 200)).exp -eq '30000' -and $r -match '승급') $r
    $r = Cmd 'potadd 1001 11' 400
    Check 'a grade out of range is refused' ((Fields (Cmd 'potlist' 200)).count -eq '1') $r
    for ($i = 0; $i -lt 27; $i++) { Cmd 'potadd 1002 1' 150 | Out-Null }
    Check 'potmons can be added up to 28' ((Fields (Cmd 'potlist' 200)).count -eq '28') (Cmd 'potlist' 200)
    $r = Cmd 'potadd 1002 1' 400
    Check 'the 29th potmon is refused' ((Fields (Cmd 'potlist' 200)).count -eq '28' -and $r -match '28') $r
    Cmd 'ddmg 1' 300 | Out-Null
    Check 'damage makes the gourd unusable' ((Fields (Cmd 'ddstate' 200)).canUse -eq 'False') (Cmd 'ddstate' 200)
    $r = Cmd 'ddrepair' 400
    Check 'repair makes the gourd usable' ((Fields (Cmd 'ddstate' 200)).canUse -eq 'True' -and (Fields (Cmd 'ddstate' 200)).damaged -eq 'False') "$r | $(Cmd 'ddstate' 200)"
    Check 'the change log has the gourd line' (LogHas '연요호')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
```

`godeye`의 "요수 해금 41, 기술 92개 익힘, 건너뜀 1"은 앞에서 7010(기술 3)을 해금해 두었기 때문이다(42 − 1, 95 − 3).

- [ ] **Step 2: 회차를 열어 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin atab1 -Fixture base
pwsh tools/research/artifact-check.ps1 -Part list
pwsh tools/DevRemote/session.ps1 -End atab1
```

Expected: `aw`가 모르는 명령이라 "UNKNOWN" → `FAIL ...`, 마지막 줄 `FAILS=` 0보다 큼. `-End`는 "SAVE RESTORED AND IDENTICAL".

- [ ] **Step 3: 서비스와 개발 명령을 만든다**

`src/ModCode/ModMain/Services/ArtifactService.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 시작 신기 셋(1 쌍어패, 2 호천안, 3 연요호)의 목록, 쌍어옥패 점수, 호천안·연요호의 손상. 세계 자료(g.data.world)에 쓴다.
    // 목록을 비우면 게임이 시작 연출과 신기 선택을 다시 열므로 마지막 하나는 빼지 않는다(docs/07 7.2절, 스펙 3절).
    // 지도 화면의 신기 버튼은 다시 불러와야 바뀐다(UpdateUI로는 켜지지 않았다).
    internal static class ArtifactService
    {
        public static readonly int[] Ids = { 1, 2, 3 };
        public const int PointMax = 9999;
        public const string ButtonNote = "지도 화면의 신기 버튼은 저장 후 다시 불러오면 바뀝니다.";
        public const string LastReason = "신기는 하나 이상 있어야 합니다. 비우면 게임이 시작 연출과 신기 선택을 다시 엽니다.";
        private const string Who = "신기";

        public static bool Ready
        {
            get { return g.data != null && g.data.world != null && g.data.world.animaWeapons != null; }
        }

        public static bool Has(int id)
        {
            return Ready && g.data.world.animaWeapons.Contains((GameAnimaWeapon)id);
        }

        public static int Count
        {
            get { return Ready ? g.data.world.animaWeapons.Count : 0; }
        }

        public static string Listed()
        {
            var parts = new List<string>();
            for (int i = 0; i < Count; i++)
            {
                parts.Add(((int)g.data.world.animaWeapons[i]).ToString());
            }
            return string.Join(",", parts.ToArray());
        }

        internal static string Text(string key)
        {
            return key == null || key == "0" ? "" : MartialService.CleanDesc(GameTool.LS(key)).Replace('\n', ' ');
        }

        public static string Name(int id)
        {
            ConfAnimaWeaponItem item = g.conf.animaWeapon.GetItem(id);
            string name = item == null ? "" : Text(item.name);
            return name.Length > 0 ? name : "신기 " + id;
        }

        public static string Desc(int id)
        {
            ConfAnimaWeaponItem item = g.conf.animaWeapon.GetItem(id);
            return item == null ? "" : Text(item.desc);
        }

        public static string Enable(int id)
        {
            string label = Name(id);
            if (!Ready)
            {
                return label + ": 세계 자료가 없습니다.";
            }
            if (Has(id))
            {
                return label + ": 이미 가지고 있습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = Listed();
            g.data.world.animaWeapons.Add((GameAnimaWeapon)id);
            bool ok = Has(id);
            ChangeLog.Add(Who, "신기 목록", before, Listed(), ok);
            return ok ? label + ": 켰습니다. " + ButtonNote : label + ": 목록에 들어가지 않았습니다.";
        }

        public static string Disable(int id)
        {
            string label = Name(id);
            if (!Has(id))
            {
                return label + ": 가지고 있지 않습니다.";
            }
            if (Count <= 1)
            {
                return label + ": " + LastReason;
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = Listed();
            g.data.world.animaWeapons.Remove((GameAnimaWeapon)id);
            bool ok = !Has(id);
            ChangeLog.Add(Who, "신기 목록", before, Listed(), ok);
            return ok ? label + ": 껐습니다(그 신기의 자료는 남습니다). " + ButtonNote : label + ": 목록에서 빠지지 않았습니다.";
        }

        public static int Point
        {
            get { return Ready ? g.data.world.resurgeCount : 0; }
        }

        public static string SetPoint(int value)
        {
            if (!Ready)
            {
                return "쌍어옥패 점수: 세계 자료가 없습니다.";
            }
            int v = Math.Max(0, Math.Min(PointMax, value));
            int before = Point;
            if (before == v)
            {
                return "쌍어옥패 점수: 이미 " + v + "입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            g.data.world.resurgeCount = v;
            bool ok = Point == v;
            ChangeLog.Add(Who, "쌍어옥패 점수", before.ToString(), Point.ToString(), ok);
            return ok ? "쌍어옥패 점수: " + before + " → " + v + (v != value ? "(0~" + PointMax + "로 맞춤)" : "") + "." : "쌍어옥패 점수: 바뀌지 않았습니다.";
        }

        private static DataWorld.World.GodEyeData GodEye
        {
            get { return Ready ? g.data.world.godEyeData : null; }
        }

        public static bool GodEyeDamaged
        {
            get
            {
                var e = GodEye;
                return e != null && (e.isDamage || e.IsRepair());
            }
        }

        public static string GodEyeState()
        {
            var e = GodEye;
            if (e == null)
            {
                return "자료 없음";
            }
            return e.isDamage ? "손상" : e.IsRepair() ? "수리 중" : "사용 가능";
        }

        // Repair()는 수리를 맡기는 함수라(3개월 뒤에 끝난다) 쓰지 않고 값을 직접 지운다(스펙 2절).
        public static string RepairGodEye()
        {
            var e = GodEye;
            if (e == null)
            {
                return "호천안: 자료가 없습니다.";
            }
            if (!GodEyeDamaged)
            {
                return "호천안: 손상되지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            string before = GodEyeState();
            e.isDamage = false;
            e.repairMonth = 0;
            bool ok = e.IsCanUse();
            ChangeLog.Add("호천안", "손상", before, GodEyeState(), ok);
            return ok ? "호천안: 손상을 해제했습니다(바로 쓸 수 있습니다)." : "호천안: 값은 지웠지만 게임이 아직 쓸 수 없다고 합니다.";
        }

        private static DevilDemonMgr Devil
        {
            get { return g.world == null ? null : g.world.devilDemonMgr; }
        }

        // 연요호의 손상은 관리자(실행 중의 값)와 세계 자료(저장되는 값) 두 곳에 있다. 둘 다 본다.
        public static bool DevilDamaged
        {
            get
            {
                DevilDemonMgr mgr = Devil;
                bool runtime = mgr != null && mgr.devilDemonData != null && mgr.devilDemonData.isDamage;
                bool saved = Ready && g.data.world.devilDemonData != null && g.data.world.devilDemonData.isDamage;
                return runtime || saved;
            }
        }

        public static string DevilState()
        {
            return Devil == null ? "자료 없음" : DevilDamaged ? "손상" : "사용 가능";
        }

        public static string RepairDevil()
        {
            DevilDemonMgr mgr = Devil;
            if (mgr == null)
            {
                return "연요호: 자료가 없습니다.";
            }
            if (!DevilDamaged)
            {
                return "연요호: 손상되지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            if (mgr.devilDemonData != null)
            {
                mgr.devilDemonData.isDamage = false;
                mgr.devilDemonData.repairMonth = 0;
            }
            if (g.data.world.devilDemonData != null)
            {
                g.data.world.devilDemonData.isDamage = false;
                g.data.world.devilDemonData.repairMonth = 0;
            }
            bool ok = !DevilDamaged;
            ChangeLog.Add("연요호", "손상", "손상", DevilState(), ok);
            return ok ? "연요호: 손상을 해제했습니다." : "연요호: 손상이 남아 있습니다.";
        }

        // 플레이어의 대경지(1 연기, 2 축기, 3 결정 …). 호천안 요수의 단계와 호요의 단계가 이 번호를 쓴다(스펙 2절).
        public static int PlayerMajor()
        {
            WorldUnitBase me = g.world == null ? null : g.world.playerUnit;
            ConfRoleGradeItem item = me == null ? null : g.conf.roleGrade.GetItem(me.data.unitData.propertyData.gradeID);
            return item == null ? 1 : Math.Max(1, item.grade);
        }

        public static string MajorName(int major)
        {
            ConfRoleGradeItem row = GradeService.FirstRowOf(major);
            string name = row == null ? "" : Text(row.gradeName);
            return name.Length > 0 ? name : major + "단계";
        }
    }
}
```

`src/ModCode/ModMain/Services/GodEyeService.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 호천안의 요수와 기술. 목록은 설정 표 godEyeSkills(기술 줄마다 bossID)를 요수로 묶어 만든다. 요수 이름은 battleUnitAttr의 그 번호 줄.
    // 해금은 godEyeData.bossGrade[요수]=단계(그림 위의 경지 글이 된다), 익힘은 skillsID, 장착은 equipSkillID(스펙 2절).
    internal static class GodEyeService
    {
        public sealed class Skill
        {
            public int Id;
            public int BossId;
            public string Name = "";
            public string Desc = "";
        }

        public sealed class Boss
        {
            public int Id;
            public string Name = "";
            public readonly List<Skill> Skills = new List<Skill>();
            public string Search = "";
        }

        private static List<Boss> bosses;

        private static string Normalize(string text)
        {
            return (text ?? "").Replace(" ", "").ToLowerInvariant();
        }

        public static List<Boss> Bosses()
        {
            if (bosses != null)
            {
                return bosses;
            }
            var result = new List<Boss>();
            var byId = new Dictionary<int, Boss>();
            var rows = g.conf.godEyeSkills._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                ConfGodEyeSkillsItem row = rows[i];
                Boss boss;
                if (!byId.TryGetValue(row.bossID, out boss))
                {
                    ConfBattleUnitAttrItem attr = g.conf.battleUnitAttr.GetItem(row.bossID);
                    string name = attr == null ? "" : ArtifactService.Text(attr.name);
                    boss = new Boss { Id = row.bossID, Name = name.Length > 0 ? name : "요수 " + row.bossID };
                    byId[row.bossID] = boss;
                    result.Add(boss);
                }
                int id = row.id;
                if (boss.Skills.Exists(s => s.Id == id))
                {
                    continue;
                }
                string skillName = ArtifactService.Text(row.name);
                boss.Skills.Add(new Skill { Id = id, BossId = row.bossID, Name = skillName.Length > 0 ? skillName : id.ToString(), Desc = ArtifactService.Text(row.desc) });
            }
            foreach (Boss b in result)
            {
                b.Search = Normalize(b.Name + string.Join("", b.Skills.ConvertAll(s => s.Name).ToArray()));
            }
            bosses = result;
            return bosses;
        }

        public static List<Boss> Find(string query)
        {
            string q = Normalize(query);
            return q.Length == 0 ? new List<Boss>(Bosses()) : Bosses().FindAll(b => b.Search.Contains(q));
        }

        public static Boss FindBoss(int id)
        {
            return Bosses().Find(b => b.Id == id);
        }

        public static Skill FindSkill(int id)
        {
            foreach (Boss b in Bosses())
            {
                Skill s = b.Skills.Find(x => x.Id == id);
                if (s != null)
                {
                    return s;
                }
            }
            return null;
        }

        // 서로 다른 기술 번호의 수.
        public static int SkillCount()
        {
            var ids = new HashSet<int>();
            foreach (Boss b in Bosses())
            {
                foreach (Skill s in b.Skills)
                {
                    ids.Add(s.Id);
                }
            }
            return ids.Count;
        }

        private static DataWorld.World.GodEyeData Data
        {
            get { return ArtifactService.Ready ? g.data.world.godEyeData : null; }
        }

        public static bool Unlocked(Boss b)
        {
            var d = Data;
            return b != null && d != null && d.bossGrade != null && d.bossGrade.ContainsKey(b.Id);
        }

        public static int Grade(Boss b)
        {
            return Unlocked(b) ? Data.bossGrade[b.Id] : 0;
        }

        public static bool Learned(int skillId)
        {
            var d = Data;
            return d != null && d.skillsID != null && d.skillsID.Contains(skillId);
        }

        public static int LearnedCount(Boss b)
        {
            int n = 0;
            foreach (Skill s in b.Skills)
            {
                n += Learned(s.Id) ? 1 : 0;
            }
            return n;
        }

        public static int Equipped
        {
            get
            {
                var d = Data;
                return d == null ? 0 : d.equipSkillID;
            }
        }

        private static bool Complete(Boss b)
        {
            return Unlocked(b) && LearnedCount(b) == b.Skills.Count;
        }

        public static string Unlock(Boss b)
        {
            if (b == null)
            {
                return "호천안: 그런 요수가 없습니다.";
            }
            if (!ArtifactService.Has(2))
            {
                return b.Name + ": 호천안이 없습니다.";
            }
            var d = Data;
            if (d == null || d.bossGrade == null || d.skillsID == null)
            {
                return b.Name + ": 호천안 자료가 없습니다.";
            }
            if (Complete(b))
            {
                return b.Name + ": 이미 해금했고 기술을 모두 익혔습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            bool was = Unlocked(b);
            int before = LearnedCount(b);
            int grade = was ? Grade(b) : ArtifactService.PlayerMajor();
            if (!was)
            {
                d.bossGrade[b.Id] = grade;
            }
            foreach (Skill s in b.Skills)
            {
                if (!Learned(s.Id))
                {
                    d.skillsID.Add(s.Id);
                }
            }
            bool ok = Complete(b);
            int added = LearnedCount(b) - before;
            ChangeLog.Add("호천안", "요수 " + b.Name + "(" + b.Id + ")", was ? "해금, 기술 " + before + "/" + b.Skills.Count : "잠김",
                "해금(" + ArtifactService.MajorName(grade) + "), 기술 " + LearnedCount(b) + "/" + b.Skills.Count, ok);
            return ok ? b.Name + ": " + (was ? "이미 해금" : "해금(" + ArtifactService.MajorName(grade) + ")") + ", 기술 " + added + "개 익힘."
                : b.Name + ": 게임 자료에 들어가지 않았습니다.";
        }

        public static string UnlockAll(List<Boss> list)
        {
            if (!ArtifactService.Has(2))
            {
                return "호천안: 호천안이 없습니다.";
            }
            int unlocked = 0, learned = 0, skipped = 0;
            var failed = new List<string>();
            foreach (Boss b in list)
            {
                if (Complete(b))
                {
                    skipped++;
                    continue;
                }
                bool was = Unlocked(b);
                int before = LearnedCount(b);
                string text = Unlock(b);
                if (Complete(b))
                {
                    unlocked += was ? 0 : 1;
                    learned += LearnedCount(b) - before;
                }
                else
                {
                    failed.Add(text);
                }
            }
            return "호천안: 요수 해금 " + unlocked + ", 기술 " + learned + "개 익힘, 건너뜀 " + skipped + "(이미 모두 익힘)"
                + (failed.Count > 0 ? ", 못 함 " + failed.Count + "(" + string.Join(" / ", failed.ToArray()) + ")" : "") + ".";
        }

        public static string Equip(Skill s)
        {
            if (s == null)
            {
                return "호천안: 그런 기술이 없습니다.";
            }
            if (!ArtifactService.Has(2))
            {
                return s.Name + ": 호천안이 없습니다.";
            }
            if (!Learned(s.Id))
            {
                return s.Name + ": 익히지 않은 기술은 장착할 수 없습니다.";
            }
            if (Equipped == s.Id)
            {
                return s.Name + ": 이미 장착 중입니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            Skill old = FindSkill(Equipped);
            string before = Equipped == 0 ? "없음" : old == null ? Equipped.ToString() : old.Name;
            Data.equipSkillID = s.Id;
            bool ok = Equipped == s.Id;
            ChangeLog.Add("호천안", "장착 기술", before, s.Name + "(" + s.Id + ")", ok);
            return ok ? s.Name + ": 장착했습니다." : s.Name + ": 장착되지 않았습니다.";
        }
    }
}
```

`src/ModCode/ModMain/Services/PotmonService.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 연요호의 호요. 종류는 설정 표 potmonBase, 가진 호요는 실행 중의 관리자(g.world.devilDemonMgr.potMonMgr.potMonDatas)에서 읽는다.
    // 추가는 PotMonMgr.AddPotMon(종류, 단계, true), 경험치는 PotmonAddExp(개수당 1000, 다음 단계 경험치에서 멈춘다). 승급은 하지 않는다(스펙 2·3절).
    internal static class PotmonService
    {
        public sealed class Kind
        {
            public int Id;
            public string Name = "";
            public string Race = "";
            public string Search = "";
        }

        public sealed class Pet
        {
            public string SoleId = "";
            public int Id;
            public string Name = "";
            public int Grade;
            public int Exp;
            public int Next;    // 다음 단계 경험치. 가장 높은 단계면 -1
        }

        public const int Cap = 28;          // 게임 화면 "호요 수량: N/28". 출처는 찾지 못했다(스펙 2절)
        public const int ExpPerItem = 1000; // PotmonParamer.potmonPropAddExp
        public const int MaxGrade = 10;

        private static List<Kind> kinds;

        private static PotMonMgr Mgr
        {
            get { return g.world == null || g.world.devilDemonMgr == null ? null : g.world.devilDemonMgr.potMonMgr; }
        }

        private static string Normalize(string text)
        {
            return (text ?? "").Replace(" ", "").ToLowerInvariant();
        }

        public static List<Kind> Kinds()
        {
            if (kinds != null)
            {
                return kinds;
            }
            var result = new List<Kind>();
            var rows = g.conf.potmonBase._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                string name = ArtifactService.Text(rows[i].name);
                var k = new Kind { Id = rows[i].id, Name = name.Length > 0 ? name : "호요 " + rows[i].id, Race = ArtifactService.Text(rows[i].race) };
                k.Search = Normalize(k.Name + k.Race);
                result.Add(k);
            }
            kinds = result;
            return kinds;
        }

        public static List<Kind> Find(string query)
        {
            string q = Normalize(query);
            return q.Length == 0 ? new List<Kind>(Kinds()) : Kinds().FindAll(k => k.Search.Contains(q));
        }

        public static Kind FindKind(int id)
        {
            return Kinds().Find(k => k.Id == id);
        }

        public static int Count
        {
            get
            {
                PotMonMgr m = Mgr;
                return m == null || m.potMonDatas == null ? 0 : m.potMonDatas.Count;
            }
        }

        public static int NextExp(int grade)
        {
            var rows = g.conf.potmonGradeLevel._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].grade == grade + 1)
                {
                    return rows[i].exp;
                }
            }
            return -1;
        }

        public static List<Pet> Owned()
        {
            var result = new List<Pet>();
            PotMonMgr m = Mgr;
            for (int i = 0; m != null && m.potMonDatas != null && i < m.potMonDatas.Count; i++)
            {
                PotMonUnitData u = m.potMonDatas[i].potMonUnitData;
                if (u == null)
                {
                    continue;
                }
                Kind k = FindKind(u.id);
                result.Add(new Pet { SoleId = u.soleId, Id = u.id, Name = k == null ? "호요 " + u.id : k.Name, Grade = u.grade, Exp = u.exp, Next = NextExp(u.grade) });
            }
            return result;
        }

        public static string Add(Kind k, int grade)
        {
            if (k == null)
            {
                return "호요: 그런 종류가 없습니다.";
            }
            string label = "호요 " + k.Name;
            if (!ArtifactService.Has(3))
            {
                return label + ": 연요호가 없습니다.";
            }
            PotMonMgr m = Mgr;
            if (m == null || m.potMonDatas == null)
            {
                return label + ": 연요호 자료가 없습니다.";
            }
            if (grade < 1 || grade > MaxGrade)
            {
                return label + ": 단계는 1~" + MaxGrade + "입니다.";
            }
            int before = Count;
            if (before >= Cap)
            {
                return label + ": 호요는 게임 화면의 상한 " + Cap + "까지 넣습니다(지금 " + before + ").";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            m.AddPotMon(k.Id, grade, true);
            bool ok = Count == before + 1;
            ChangeLog.Add("연요호", "호요 추가", before.ToString(), Count + "(" + k.Name + ", " + ArtifactService.MajorName(grade) + ")", ok);
            return ok ? label + ": 넣었습니다(" + ArtifactService.MajorName(grade) + ", 호요 " + Count + "/" + Cap + ")." : label + ": 게임이 넣지 않았습니다.";
        }

        public static string FillExp(string soleId)
        {
            PotMonMgr m = Mgr;
            PotMonMgr.PotMonData data = m == null ? null : m.GetPotmonFromSoleID(soleId);
            if (data == null || data.potMonUnitData == null)
            {
                return "호요: 찾지 못했습니다.";
            }
            PotMonUnitData u = data.potMonUnitData;
            Kind k = FindKind(u.id);
            string label = "호요 " + (k == null ? u.id.ToString() : k.Name);
            int next = NextExp(u.grade);
            if (next < 0)
            {
                return label + ": 가장 높은 단계입니다.";
            }
            if (u.exp >= next)
            {
                return label + ": 이미 다음 단계 경험치입니다. 승급은 게임 화면에서 합니다(승급 아이템 필요).";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            int before = u.exp;
            int count = (next - before + ExpPerItem - 1) / ExpPerItem;
            m.PotmonAddExp(data, count, false);
            int after = data.potMonUnitData.exp;
            bool ok = after >= next;
            ChangeLog.Add("연요호", "호요 " + (k == null ? u.id.ToString() : k.Name) + " 경험치", before.ToString(), after.ToString(), ok);
            return ok ? label + ": 경험치 " + before + " → " + after + "(다음 단계 경계). 승급은 게임 화면에서 합니다."
                : label + ": 경험치가 " + before + " → " + after + "에 그쳤습니다.";
        }
    }
}
```

`src/ModCode/ModMain/Dev/DevCommandsArtifact.cs`:

```csharp
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 신기 탭의 검증용 명령(tools/research/artifact-check.ps1).
    internal static class DevCommandsArtifact
    {
        public static string Execute(string[] p)
        {
            if (g.world == null || g.world.playerUnit == null || !ArtifactService.Ready)
            {
                return null;
            }
            switch (p[0])
            {
                case "aw":
                    return "weapons=" + ArtifactService.Listed() + " point=" + ArtifactService.Point;
                case "awset":
                    {
                        // 검사 준비: 목록을 그대로 바꾼다(서비스의 보호를 거치지 않는다).
                        var list = g.data.world.animaWeapons;
                        list.Clear();
                        foreach (string s in p[1].Split(','))
                        {
                            list.Add((GameAnimaWeapon)int.Parse(s));
                        }
                        return "weapons=" + ArtifactService.Listed();
                    }
                case "awbtn":
                    {
                        // 지도 화면의 신기 버튼이 켜져 있는지.
                        UIMapMain map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
                        if (map == null || map.playerInfo == null)
                        {
                            return "map=null";
                        }
                        return "pisces=" + On(map.playerInfo.btnPiscesPendant) + " godEye=" + On(map.playerInfo.btnGodEye) + " devil=" + On(map.playerInfo.btnDevilDemon);
                    }
                case "aon": return ArtifactService.Enable(int.Parse(p[1]));
                case "aoff": return ArtifactService.Disable(int.Parse(p[1]));
                case "apoint": return ArtifactService.SetPoint(int.Parse(p[1]));
                case "gedmg":
                    {
                        var e = g.data.world.godEyeData;
                        e.isDamage = p[1] == "1";
                        e.repairMonth = 0;
                        return GeState();
                    }
                case "gestate": return GeState();
                case "gerepair": return ArtifactService.RepairGodEye();
                case "gecat":
                    {
                        var bosses = GodEyeService.Bosses();
                        return "bosses=" + bosses.Count + " skills=" + GodEyeService.SkillCount() + " first=" + (bosses.Count == 0 ? "" : bosses[0].Name.Replace(' ', '_'));
                    }
                case "geunlock": return GodEyeService.Unlock(GodEyeService.FindBoss(int.Parse(p[1])));
                case "geunlockall": return GodEyeService.UnlockAll(GodEyeService.Bosses());
                case "geboss":
                    {
                        GodEyeService.Boss b = GodEyeService.FindBoss(int.Parse(p[1]));
                        return b == null ? "no boss" : "grade=" + GodEyeService.Grade(b) + " learned=" + GodEyeService.LearnedCount(b) + "/" + b.Skills.Count;
                    }
                case "geequip": return GodEyeService.Equip(GodEyeService.FindSkill(int.Parse(p[1])));
                case "ddmg":
                    {
                        bool on = p[1] == "1";
                        if (g.world.devilDemonMgr != null && g.world.devilDemonMgr.devilDemonData != null)
                        {
                            g.world.devilDemonMgr.devilDemonData.isDamage = on;
                        }
                        g.data.world.devilDemonData.isDamage = on;
                        return DdState();
                    }
                case "ddstate": return DdState();
                case "ddrepair": return ArtifactService.RepairDevil();
                case "potlist":
                    {
                        List<PotmonService.Pet> pets = PotmonService.Owned();
                        var sb = new StringBuilder("count=" + pets.Count);
                        if (pets.Count > 0)
                        {
                            sb.Append(" first=").Append(pets[0].SoleId).Append(" grade=").Append(pets[0].Grade).Append(" exp=").Append(pets[0].Exp).Append(" next=").Append(pets[0].Next);
                        }
                        return sb.ToString();
                    }
                case "potadd": return PotmonService.Add(PotmonService.FindKind(int.Parse(p[1])), int.Parse(p[2]));
                case "potfill": return PotmonService.FillExp(p[1]);
                default: return null;
            }
        }

        private static string GeState()
        {
            var e = g.data.world.godEyeData;
            return "damaged=" + ArtifactService.GodEyeDamaged + " canUse=" + e.IsCanUse() + " repairMonth=" + e.repairMonth
                + " bosses=" + (e.bossGrade == null ? -1 : e.bossGrade.Count) + " skills=" + (e.skillsID == null ? -1 : e.skillsID.Count) + " equip=" + e.equipSkillID;
        }

        private static string DdState()
        {
            DevilDemonMgr mgr = g.world.devilDemonMgr;
            return "damaged=" + ArtifactService.DevilDamaged + " canUse=" + (mgr != null && mgr.IsCanUse());
        }

        private static string On(UnityEngine.UI.Button b)
        {
            return b == null ? "null" : b.gameObject.activeSelf ? "on" : "off";
        }
    }
}
```

`src/ModCode/ModMain/Dev/DevCommands.cs`의 `Execute` 맨 앞(`DevCommandsImmortal.Execute` 앞)에 넣는다:

```csharp
            string artifact = DevCommandsArtifact.Execute(p);
            if (artifact != null)
            {
                return artifact;
            }
```

`gecat`의 `first`는 표의 첫 요수 이름이다(고정본 표에서는 7091 무라). 검사는 "번호로 된 대체 이름(`요수 …`)이 아님"만 본다.

- [ ] **Step 4: 두 구성으로 빌드한다**

```bash
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release
dotnet build src/ModCode/ModMain/ModMain.csproj -c Release -p:DevTools=false -o "$TEMP/artifact-release"
ls src/ModCode/ModMain/bin/Release
```

Expected: 두 빌드 모두 "경고 0개", "오류 0개". `bin/Release`에 `MOD_sWP6UL.dll` 하나.

- [ ] **Step 5: 검사가 통과하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin atab2 -Fixture base
pwsh tools/research/artifact-check.ps1 -Part list
pwsh tools/research/artifact-check.ps1 -Part godeye
pwsh tools/research/artifact-check.ps1 -Part potmon
pwsh tools/DevRemote/session.ps1 -End atab2
```

Expected: 세 부분 모두 마지막 줄 `FAILS=0`, `-End`가 "SAVE RESTORED AND IDENTICAL". 실패하면 실패한 줄의 상세(명령 출력)로 원인을 찾고, 검사를 고치기 전에 서비스가 스펙 3절과 다른지부터 본다. `godeye`의 수(42, 95)가 다르면 `gecat`의 출력과 스펙 2절을 대조해 보고한다(수를 맞춰 고치지 않는다).

- [ ] **Step 6: 커밋**

```bash
git add src/ModCode/ModMain/Services/ArtifactService.cs src/ModCode/ModMain/Services/GodEyeService.cs src/ModCode/ModMain/Services/PotmonService.cs src/ModCode/ModMain/Dev/DevCommandsArtifact.cs src/ModCode/ModMain/Dev/DevCommands.cs tools/research/artifact-check.ps1
git commit -m "feat: 신기 서비스(목록·점수·손상, 호천안 요수·기술, 연요호 호요)와 검사

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: 신기 탭 화면

**Files:**
- Create: `src/ModCode/ModMain/UI/Tabs/ArtifactTab.cs`
- Modify: `src/ModCode/ModMain/UI/EditorWindow.cs`(탭 목록, 탭 줄 높이), `tools/research/artifact-check.ps1`(부분 `ui`, `game`, `persist`, `restart`), `tools/research/ui-check.ps1`, `datebag-check.ps1`, `preset-check.ps1`, `immortal-check.ps1`(탭 이름)

**Interfaces:**
- Consumes: Task 1의 `ArtifactService`, `GodEyeService`, `PotmonService` 전부(위 Interfaces의 이름 그대로), 개발 명령 `aw`, `awset`, `awbtn`, `apoint`, `gestate`, `geboss`, `geunlock`, `geequip`, `potlist`, `potadd`.
- Produces: 탭 `ArtifactTab`(`Title` "신기", `UsesTarget` false), 요소 이름 `ArtifactOn<n>`, `ArtifactOff<n>`, `ArtifactState<n>`, `ArtifactOpen<2|3>`, `ArtifactLast`, `ArtifactPoint`, `ArtifactPointMinus10`, `ArtifactPointMinus1`, `ArtifactPointPlus1`, `ArtifactPointPlus10`, `GodEyeStateText`, `GodEyeRepair`, `GodEyeEquipText`, `GodEyeUnlockAll`, `GodEyeSearch`, `GodEyeList`, `GodEyeBoss<요수>`, `GodEyeState<요수>`, `GodEyeEquip<기술>`, `GodEyeDesc<기술>`, `DevilStateText`, `DevilRepair`, `DevilCount`, `PotmonOwnedList`, `PotmonExp<soleId>`, `PotmonExpText<soleId>`, `PotmonGrade<1~10>`, `PotmonSearch`, `PotmonKindList`, `PotmonAdd<종류>`.

- [ ] **Step 1: 화면 검사를 먼저 쓴다**

`tools/research/artifact-check.ps1`의 `switch`에서 `default` 앞에 넣는다:

```powershell
  'ui' {
    Cmd 'awset 1' 300 | Out-Null
    Check 'editor opens' ((Cmd 'editor') -match 'open=True')
    Press 'Tab7' | Out-Null
    Check 'the artifact tab is the eighth' (IsAt 'ArtifactPoint')
    Check 'the last artifact cannot be turned off' ((IsOff 'ArtifactOff1') -and (Cmd 'uitext ArtifactLast' 150) -match '하나 이상')
    Check 'details of a missing artifact are off' (IsOff 'ArtifactOpen2')
    Press 'ArtifactOn2' 1200 | Out-Null
    Check 'turning on adds it' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2') (Cmd 'aw' 200)
    Press 'ArtifactOn3' 1200 | Out-Null
    Press 'ArtifactOff3' | Out-Null
    Check 'turning off asks first' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2,3')
    Press 'ArtifactOff3' 1200 | Out-Null
    Check 'the second press turns it off' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2') (Cmd 'aw' 200)
    Press 'ArtifactOn3' 1200 | Out-Null
    Cmd 'uifocus ArtifactPoint' | Out-Null; Cmd 'ui set ArtifactPoint 25' | Out-Null; Key 0x0D
    Check 'the point field sets the point on Enter' ((Fields (Cmd 'aw' 200)).point -eq '25') (Cmd 'aw' 200)
    Press 'ArtifactPointPlus10' | Out-Null
    Press 'ArtifactPointMinus1' | Out-Null
    Check 'plus ten and minus one' ((Fields (Cmd 'aw' 200)).point -eq '34') (Cmd 'aw' 200)
    Cmd 'uifocus ArtifactPoint' | Out-Null; Cmd 'ui set ArtifactPoint -5' | Out-Null; Key 0x0D
    Check 'a negative point becomes 0' ((Fields (Cmd 'aw' 200)).point -eq '0') (Cmd 'aw' 200)
    Cmd 'uifocus ArtifactPoint' | Out-Null; Cmd 'ui set ArtifactPoint 99999' | Out-Null; Key 0x0D
    Check 'a point over the limit becomes 9999' ((Fields (Cmd 'aw' 200)).point -eq '9999') (Cmd 'aw' 200)
    Press 'ArtifactOpen2' | Out-Null
    Check 'the god eye page has its controls' ((IsAt 'GodEyeUnlockAll') -and (IsAt 'GodEyeSearch') -and (IsAt 'GodEyeBoss7010'))
    Check 'repair is off without damage' (IsOff 'GodEyeRepair')
    Check 'an unlearned skill cannot be equipped' (IsOff 'GodEyeEquip100721')
    Press 'GodEyeBoss7010' 1200 | Out-Null
    Check 'unlock from the page' ((Fields (Cmd 'geboss 7010' 200)).learned -eq '3/3' -and (Cmd 'uitext GodEyeState7010' 150) -match '해금') (Cmd 'geboss 7010' 200)
    Press 'GodEyeEquip100721' 1200 | Out-Null
    Check 'equip from the page' ((Fields (Cmd 'gestate' 200)).equip -eq '100721' -and (Cmd 'uitext GodEyeEquipText' 150) -match '유서불풍') (Cmd 'gestate' 200)
    $fit = Fields (Cmd 'ifit GodEyeDesc' 300)
    Check 'every skill description is whole' ([int]$fit.texts -ge 3 -and $fit.overflow -eq '0') (Cmd 'ifit GodEyeDesc' 300)
    Shot 'artifact-godeye-page'
    Press 'Back' | Out-Null
    Press 'ArtifactOpen3' | Out-Null
    Check 'the gourd page has its controls' ((IsAt 'PotmonGrade10') -and (IsAt 'PotmonSearch') -and (IsAt 'PotmonAdd1001'))
    Check 'the count starts at zero' ((Cmd 'uitext DevilCount' 150) -match '0 / 28')
    Press 'PotmonGrade2' | Out-Null
    Press 'PotmonAdd1001' 1200 | Out-Null
    $p = Fields (Cmd 'potlist' 200)
    Check 'add from the page uses the chosen grade' ($p.count -eq '1' -and $p.grade -eq '2') (Cmd 'potlist' 200)
    Press "PotmonExp$($p.first)" 1200 | Out-Null
    Check 'fill exp from the page' ((Fields (Cmd 'potlist' 200)).exp -eq '18000') (Cmd 'potlist' 200)
    Check 'the fill button is off at the boundary' (IsOff "PotmonExp$($p.first)")
    Check 'the count is shown' ((Cmd 'uitext DevilCount' 150) -match '1 / 28')
    Shot 'artifact-gourd-page'
    Press 'Back' | Out-Null
    Shot 'artifact-tab'
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'game' {
    # 게임 화면에 나오는지(캡처로 본다): 호천안 요수 기능, 연요호 모든 호요, 부활 정보의 점수.
    Cmd 'awset 1,2,3' 300 | Out-Null
    Cmd 'geunlock 7010' 400 | Out-Null; Cmd 'geequip 100721' 400 | Out-Null
    Cmd 'potadd 1001 3' 400 | Out-Null
    Cmd 'apoint 12' 300 | Out-Null
    Cmd 'uiopen GodEye' 1500 | Out-Null
    Rc 'click 1379 851' 1500 | Out-Null
    Check 'the game god eye skill page opened' ((Cmd 'uilist' 200) -match 'GodEyeBossSkill')
    Shot 'artifact-game-godeye'
    Cmd 'uiclose GodEyeBossSkill' 500 | Out-Null; Cmd 'uiclose GodEye' 800 | Out-Null
    Cmd 'uiopen DevilDemon' 1500 | Out-Null
    Rc 'click 1362 897' 1500 | Out-Null
    Check 'the game potmon list opened' ((Cmd 'uilist' 200) -match 'PotmonList')
    Shot 'artifact-game-gourd'
    Cmd 'uiclose PotmonList' 500 | Out-Null; Cmd 'uiclose DevilDemon' 800 | Out-Null
    Cmd 'uiopen ResurgeInfo' 1500 | Out-Null
    Shot 'artifact-game-point'
    Cmd 'uiclose ResurgeInfo' 800 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'persist' {
    # 이 뒤에 session.ps1 -Restart, 그 뒤에 restart 부분을 돌린다.
    Cmd 'awset 1' 300 | Out-Null
    Cmd 'aon 2' 400 | Out-Null; Cmd 'aon 3' 400 | Out-Null
    Cmd 'apoint 12' 300 | Out-Null
    Cmd 'geunlock 7010' 400 | Out-Null; Cmd 'geequip 100721' 400 | Out-Null
    Cmd 'potadd 1001 3' 400 | Out-Null
    Check 'the map buttons do not change before reloading' ((Cmd 'awbtn' 200) -match 'godEye=off' -and (Cmd 'awbtn' 200) -match 'devil=off') (Cmd 'awbtn' 200)
    Cmd 'save' 2500 | Out-Null
    Start-Sleep -Seconds 6   # 저장이 끝나기 전에 재시작하면 남지 않았다(07 문서 7.2절)
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'restart' {
    Check 'the artifacts are kept' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2,3' -and (Fields (Cmd 'aw' 200)).point -eq '12') (Cmd 'aw' 200)
    Check 'the map shows all three buttons after reloading' ((Cmd 'awbtn' 200) -match 'pisces=on godEye=on devil=on') (Cmd 'awbtn' 200)
    Check 'the god eye boss and the equipped skill are kept' ((Fields (Cmd 'geboss 7010' 200)).learned -eq '3/3' -and (Fields (Cmd 'gestate' 200)).equip -eq '100721') (Cmd 'gestate' 200)
    $p = Fields (Cmd 'potlist' 200)
    Check 'the potmon is kept' ($p.count -eq '1' -and $p.grade -eq '3') (Cmd 'potlist' 200)
    # 호천안을 가진 세이브는 불러올 때 호천안 화면이 저절로 열린다(원인 미확인, 07 문서 7.2절). 다음 검사를 위해 닫는다.
    if ((Cmd 'uilist' 200) -match 'GodEye') { Cmd 'uiclose GodEye' 800 | Out-Null }
  }
```

- [ ] **Step 2: 회차를 열어 화면 검사가 실패하는 것을 본다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin atab3 -Fixture base
pwsh tools/research/artifact-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End atab3
```

Expected: `Tab7`은 지금 운명 탭이라 `ArtifactPoint`를 찾지 못해 FAIL, 이어 `IsOff`의 `Pos`가 "편집기에서 요소를 찾지 못했습니다"로 멈춘다(예외로 끝나도 실패를 본 것으로 한다). `-End`는 "SAVE RESTORED AND IDENTICAL".

- [ ] **Step 3: 탭을 만든다**

`src/ModCode/ModMain/UI/Tabs/ArtifactTab.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 신기 탭: 신기 셋 켜고 끄기, 쌍어옥패 점수. 하위 화면: 호천안(손상, 요수 해금, 기술 장착), 연요호(손상, 가진 호요의 경험치, 호요 추가).
    // 세계 자료에 쓰므로 편집 대상과 무관하다(스펙 docs/superpowers/specs/2026-10-14-artifact-tab-design.md).
    internal sealed class ArtifactTab : EditorTab
    {
        private sealed class Line
        {
            public GodEyeService.Boss Boss;
            public GodEyeService.Skill Skill;   // null이면 요수 줄
        }

        private readonly ListView<Line> eyeList = new ListView<Line>("GodEyeList", 44);
        private readonly ListView<PotmonService.Pet> petList = new ListView<PotmonService.Pet>("PotmonOwnedList", 44);
        private readonly ListView<PotmonService.Kind> kindList = new ListView<PotmonService.Kind>("PotmonKindList", 44);
        private string eyeQuery = "";
        private string petQuery = "";
        private int petGrade = 1;

        public override string Title
        {
            get { return "신기"; }
        }

        public ArtifactTab()
        {
            eyeList.EmptyText = "조건에 맞는 요수가 없습니다.";
            petList.EmptyText = "가진 호요가 없습니다.";
            kindList.EmptyText = "조건에 맞는 호요가 없습니다.";
        }

        protected override void Draw()
        {
            if (!ArtifactService.Ready)
            {
                Ui.Label(content, "세계 자료가 없습니다(세계에 들어간 뒤에 씁니다).", 0, 0, 800, 36).gameObject.name = "ArtifactNoData";
                return;
            }
            Ui.Label(content, "신기", 0, 0, 200, 36, 21);
            Ui.Wrapped(content, ArtifactService.ButtonNote, 220, 0, ContentW - 220, 36, 15).color = Ui.Dim;
            float y = 46;
            float descW = ContentW - 640;
            foreach (int id in ArtifactService.Ids)
            {
                int n = id;
                bool has = ArtifactService.Has(n);
                string desc = ArtifactService.Desc(n);
                float h = RowFor(50, Ui.TextHeight(desc, descW, 15));
                float by = y + (h - 36) / 2;
                Ui.Panel("ArtifactRow" + n, content, 0, y, ContentW, h, Ui.Section);
                Ui.Label(content, ArtifactService.Name(n), 12, y, 120, h, 19);
                Text state = Ui.Label(content, has ? "가짐" : "없음", 140, y, 70, h, 17);
                state.color = has ? Ui.Warn : Ui.Dim;
                state.gameObject.name = "ArtifactState" + n;
                Ui.Wrapped(content, desc, 220, y, descW, h, 15).color = Ui.Dim;
                float bx = ContentW - 410;
                Ui.Label(content, n == 2 && has ? ArtifactService.GodEyeState() : n == 3 && has ? ArtifactService.DevilState() : "", bx, y, 120, h, 15).color = Ui.Dim;
                if (has)
                {
                    Confirm.Button("ArtifactOff" + n, content, "끄기", "정말 끄기", bx + 126, by, 130, 36, "artifact-off-" + n,
                        ArtifactService.Name(n) + "을(를) 끕니다. 그 신기의 자료는 남고, 지도 화면의 버튼은 다시 불러온 뒤에 사라집니다. \"정말 끄기\"를 한 번 더 누르세요.",
                        new Action(() => Say(ArtifactService.Disable(n))), ArtifactService.Count > 1);
                }
                else
                {
                    Ui.Button("ArtifactOn" + n, content, "켜기", bx + 126, by, 130, 36, new Action(() => Say(ArtifactService.Enable(n))));
                }
                if (n != 1)
                {
                    Ui.Button("ArtifactOpen" + n, content, "자세히", bx + 262, by, 130, 36, new Action(() => Open(n)), has);
                }
                y += h + 6;
            }
            if (ArtifactService.Count <= 1)
            {
                Text last = Ui.Wrapped(content, ArtifactService.LastReason, 0, y, ContentW, 30, 15);
                last.color = Ui.Warn;
                last.gameObject.name = "ArtifactLast";
                y += 34;
            }
            y += 14;
            Ui.Label(content, "쌍어옥패 점수", 0, y, 160, 36, 19);
            NumberField.Build(content, "ArtifactPoint", 170, y, 140, 36, ArtifactService.Point.ToString(), null, new Action<int>(v => Say(ArtifactService.SetPoint(v))));
            string[] labels = { "-10", "-1", "+1", "+10" };
            string[] names = { "ArtifactPointMinus10", "ArtifactPointMinus1", "ArtifactPointPlus1", "ArtifactPointPlus10" };
            int[] steps = { -10, -1, 1, 10 };
            for (int i = 0; i < steps.Length; i++)
            {
                int step = steps[i];
                Ui.Button(names[i], content, labels[i], 320 + i * 76, y, 70, 36, new Action(() => Say(ArtifactService.SetPoint(ArtifactService.Point + step))));
            }
            Ui.Wrapped(content, "전투에서 쓰러지면 점수를 써서 되살아납니다(0~" + ArtifactService.PointMax + "). 게임 화면: 부활 정보의 \"쌍어옥패 N점\".", 640, y, ContentW - 640, 40, 15).color = Ui.Dim;
        }

        private void Open(int n)
        {
            if (n == 2)
            {
                eyeList.Reset();
                Push(() => "호천안", DrawGodEye);
                return;
            }
            petList.Reset();
            kindList.Reset();
            petGrade = Math.Min(PotmonService.MaxGrade, ArtifactService.PlayerMajor());
            Push(() => "연요호", DrawDevil);
        }

        private static List<Line> Lines(List<GodEyeService.Boss> bosses)
        {
            var lines = new List<Line>();
            foreach (GodEyeService.Boss b in bosses)
            {
                lines.Add(new Line { Boss = b });
                foreach (GodEyeService.Skill s in b.Skills)
                {
                    lines.Add(new Line { Boss = b, Skill = s });
                }
            }
            return lines;
        }

        // 기술 줄: 이름 40~260, 설명 270~(270+DescW), 상태, 버튼.
        private static float DescW
        {
            get { return ContentW - Ui.ScrollBarWidth - 540; }
        }

        private void DrawGodEye()
        {
            float y = SubTop;
            Ui.Label(content, "상태: " + ArtifactService.GodEyeState(), 0, y, 290, 36, 18).gameObject.name = "GodEyeStateText";
            Ui.Button("GodEyeRepair", content, "손상 해제", 300, y, 130, 36, new Action(() => Say(ArtifactService.RepairGodEye())), ArtifactService.GodEyeDamaged);
            GodEyeService.Skill equipped = GodEyeService.FindSkill(GodEyeService.Equipped);
            Ui.Label(content, "장착: " + (equipped == null ? "없음" : equipped.Name), 450, y, 400, 36, 18).gameObject.name = "GodEyeEquipText";
            Confirm.Button("GodEyeUnlockAll", content, "모든 요수 해금", "정말 모두", ContentW - 220, y, 220, 36, "godeye-all",
                "모든 요수를 지금 대경지(" + ArtifactService.MajorName(ArtifactService.PlayerMajor()) + ")로 해금하고 기술을 모두 익힙니다(이미 해금한 요수는 단계를 바꾸지 않습니다). \"정말 모두\"를 한 번 더 누르세요.",
                new Action(() => Say(GodEyeService.UnlockAll(GodEyeService.Bosses()))));
            y += 46;
            SearchBox.Build(content, "GodEyeSearch", 0, y, 330, eyeQuery, "요수 또는 기술 이름의 일부", new Action<string>(v =>
            {
                eyeQuery = v;
                eyeList.SetItems(Lines(GodEyeService.Find(eyeQuery)));
            }));
            Ui.Wrapped(content, "해금은 지금 대경지로 하고 그 요수의 기술을 모두 익힙니다. 장착은 하나만 됩니다. 전투에서의 사용은 확인하지 못했습니다.", 350, y, ContentW - 350, 40, 15).color = Ui.Dim;
            float top = y + 46;
            eyeList.HeightOf = l => l.Skill == null ? 44 : RowFor(40, Ui.TextHeight(l.Skill.Desc, DescW, 15));
            eyeList.Build(content, 0, top, ContentW, ContentH - top, Lines(GodEyeService.Find(eyeQuery)), DrawEyeLine);
        }

        private void DrawEyeLine(RectTransform row, Line l, int index)
        {
            float bx = ContentW - Ui.ScrollBarWidth - 150;
            float h = row.sizeDelta.y;
            if (l.Skill == null)
            {
                GodEyeService.Boss b = l.Boss;
                bool open = GodEyeService.Unlocked(b);
                int learned = GodEyeService.LearnedCount(b);
                Ui.Label(row, b.Name + "  [" + b.Id + "]", 8, 0, 360, h, 19);
                Ui.Label(row, open ? ArtifactService.MajorName(GodEyeService.Grade(b)) : "-", 380, 0, 120, h, 17);
                Ui.Label(row, "기술 " + learned + "/" + b.Skills.Count, 510, 0, 160, h, 17);
                Text state = Ui.Label(row, open ? "해금" : "잠김", 680, 0, 100, h, 17);
                state.color = open ? Ui.Warn : Ui.Dim;
                state.gameObject.name = "GodEyeState" + b.Id;
                Ui.Button("GodEyeBoss" + b.Id, row, "해금", bx, (h - 36) / 2, 140, 36, new Action(() => Say(GodEyeService.Unlock(b))), !open || learned < b.Skills.Count);
                return;
            }
            GodEyeService.Skill s = l.Skill;
            bool has = GodEyeService.Learned(s.Id);
            bool on = GodEyeService.Equipped == s.Id;
            Ui.Label(row, "└ " + s.Name, 40, 0, 220, h, 17);
            Text desc = Ui.Wrapped(row, s.Desc, 270, 0, DescW, h, 15);
            desc.color = Ui.Dim;
            desc.gameObject.name = "GodEyeDesc" + s.Id;
            Ui.Label(row, on ? "장착 중" : has ? "익힘" : "미익힘", bx - 100, 0, 96, h, 16).color = on ? Ui.Warn : Ui.Dim;
            Ui.Button("GodEyeEquip" + s.Id, row, "장착", bx, (h - 36) / 2, 140, 36, new Action(() => Say(GodEyeService.Equip(s))), has && !on);
        }

        private void DrawDevil()
        {
            float y = SubTop;
            Ui.Label(content, "상태: " + ArtifactService.DevilState(), 0, y, 290, 36, 18).gameObject.name = "DevilStateText";
            Ui.Button("DevilRepair", content, "손상 해제", 300, y, 130, 36, new Action(() => Say(ArtifactService.RepairDevil())), ArtifactService.DevilDamaged);
            Ui.Label(content, "호요 " + PotmonService.Count + " / " + PotmonService.Cap, 450, y, 200, 36, 18).gameObject.name = "DevilCount";
            Ui.Wrapped(content, "승급은 게임 화면에서 합니다(승급 아이템 필요). 호요는 게임 화면의 상한 " + PotmonService.Cap + "까지 넣습니다.", 660, y, ContentW - 660, 40, 15).color = Ui.Dim;
            y += 46;
            Ui.Label(content, "가진 호요", 0, y, 200, 30, 18).color = Ui.Dim;
            y += 32;
            List<PotmonService.Pet> pets = PotmonService.Owned();
            float petH = Math.Min(230, Math.Max(48, pets.Count * 44 + 4));
            petList.Build(content, 0, y, ContentW, petH, pets, DrawPet);
            y += petH + 10;
            Ui.Label(content, "호요 추가  단계:", 0, y, 160, 36, 18);
            for (int gr = 1; gr <= PotmonService.MaxGrade; gr++)
            {
                int value = gr;
                WrapLabel(Ui.Button("PotmonGrade" + gr, content, gr + " " + ArtifactService.MajorName(gr), 160 + (gr - 1) * 92, y, 88, 36,
                    new Action(() => { petGrade = value; Redraw(); }), true, petGrade == gr), 15);
            }
            y += 46;
            SearchBox.Build(content, "PotmonSearch", 0, y, 330, petQuery, "호요 이름 또는 종족", new Action<string>(v =>
            {
                petQuery = v;
                kindList.SetItems(PotmonService.Find(petQuery));
            }));
            float top = y + 46;
            kindList.Build(content, 0, top, ContentW, ContentH - top, PotmonService.Find(petQuery), DrawKind);
        }

        private void DrawPet(RectTransform row, PotmonService.Pet p, int index)
        {
            Ui.Label(row, p.Name, 8, 0, 300, 44, 18);
            Ui.Label(row, ArtifactService.MajorName(p.Grade), 320, 0, 120, 44, 17);
            Ui.Label(row, "경험치 " + p.Exp + " / " + (p.Next < 0 ? "최고 단계" : p.Next.ToString()), 450, 0, 320, 44, 17).gameObject.name = "PotmonExpText" + p.SoleId;
            Ui.Button("PotmonExp" + p.SoleId, row, "경험치 채우기", ContentW - Ui.ScrollBarWidth - 180, 4, 170, 36,
                new Action(() => Say(PotmonService.FillExp(p.SoleId))), p.Next >= 0 && p.Exp < p.Next);
        }

        private void DrawKind(RectTransform row, PotmonService.Kind k, int index)
        {
            Ui.Label(row, k.Name + "  [" + k.Id + "]", 8, 0, 400, 44, 18);
            Ui.Label(row, k.Race, 420, 0, 200, 44, 17).color = Ui.Dim;
            Ui.Button("PotmonAdd" + k.Id, row, "추가", ContentW - Ui.ScrollBarWidth - 150, 4, 140, 36,
                new Action(() => Say(PotmonService.Add(k, petGrade))), PotmonService.Count < PotmonService.Cap);
        }
    }
}
```

`src/ModCode/ModMain/UI/EditorWindow.cs`: 탭 목록(445~448줄)에서 `new ImmortalTab(),` 뒤에 `new ArtifactTab(),`를 넣고, 436줄의 `14 * 52`를 `15 * 52`로 바꾼다(탭 줄 64 + 780 = 844, 결과 영역 908 위).

- [ ] **Step 4: 기존 검사의 탭 이름을 민다**

신기가 선법(`Tab6`) 뒤에 들어가므로 `Tab7`~`Tab13`이 `Tab8`~`Tab14`가 된다. 네 스크립트에서 큰 번호부터 바꾼다(겹쳐 바뀌지 않게). 치환 스크립트를 Write 도구로 `$TEMP/renumber.ps1`에 만들어 `pwsh -File`로 돌린다(인계 문서 6절: 한 줄 명령에 넣지 않는다):

```powershell
foreach ($f in 'ui-check.ps1', 'datebag-check.ps1', 'preset-check.ps1', 'immortal-check.ps1') {
  $p = Join-Path 'E:\TlToybox\tools\research' $f
  $t = [IO.File]::ReadAllText($p)
  foreach ($n in 13..7) { $t = $t.Replace("'Tab$n'", "'Tab$($n + 1)'").Replace("""Tab$n""", """Tab$($n + 1)""") }
  [IO.File]::WriteAllText($p, $t, (New-Object Text.UTF8Encoding $false))
}
```

그 뒤 손으로 고친다:
- `ui-check.ps1` 7줄 `$tabProbe`: `'ImmortalAddOpen'` 다음에 `'ArtifactPoint'`를 넣는다(15개).
- `preset-check.ps1`의 `'the safety tab is the fourteenth'`를 `'the safety tab is the fifteenth'`로.
- `artifact-check.ps1`은 이미 새 번호(`Tab7`)를 쓰므로 바꾸지 않는다(치환 목록에 넣지 않았다).

확인: `grep -n "Tab1[0-4]\|Tab[7-9]" tools/research/*.ps1`로 바뀐 줄을 보고, `git diff --stat tools/research`에서 네 파일만 바뀐 것을 본다.

- [ ] **Step 5: 빌드하고 화면 검사가 통과하는 것을 본다**

두 구성으로 빌드한다(Task 1 Step 4와 같은 명령). Expected: 오류 0, 경고 0, DLL 1.

```bash
pwsh tools/DevRemote/session.ps1 -Begin atab4 -Fixture base
pwsh tools/research/artifact-check.ps1 -Part ui
pwsh tools/research/artifact-check.ps1 -Part game
pwsh tools/DevRemote/session.ps1 -End atab4
pwsh tools/DevRemote/session.ps1 -Begin atab5 -Fixture base
pwsh tools/research/artifact-check.ps1 -Part persist
pwsh tools/DevRemote/session.ps1 -Restart
pwsh tools/research/artifact-check.ps1 -Part restart
pwsh tools/DevRemote/session.ps1 -End atab5
```

Expected: 네 부분 모두 `FAILS=0`. 캡처를 열어 본다: `check-artifact-tab.png`(신기 셋 줄과 점수), `check-artifact-godeye-page.png`(수요 해금, 유서불풍 "장착 중", 설명이 잘리지 않음), `check-artifact-gourd-page.png`(가진 호요 한 줄, 단계 버튼 열 개가 한 줄에 들어감), `check-artifact-game-godeye.png`(게임 "요수 기능"에서 수요가 열리고 "장비 중"), `check-artifact-game-gourd.png`("호요 수량: 1/28"), `check-artifact-game-point.png`("쌍어옥패 12점"). 단계 버튼의 글이 넘치면 버튼 글을 번호만으로 줄이고 다시 돌린다.

- [ ] **Step 6: 기존 검사**

```bash
pwsh tools/DevRemote/session.ps1 -Begin atab6 -Fixture base
pwsh tools/research/ui-check.ps1 -Part base
pwsh tools/research/ui-check.ps1 -Part tabs
pwsh tools/research/ui-check.ps1 -Part confirm
pwsh tools/research/ui-check.ps1 -Part wrap
pwsh tools/research/ui-check.ps1 -Part fateguard
pwsh tools/DevRemote/session.ps1 -End atab6
pwsh tools/DevRemote/session.ps1 -Begin atab7 -Fixture base
pwsh tools/research/ui-check.ps1 -Part rates
pwsh tools/research/ui-check.ps1 -Part people
pwsh tools/research/immortal-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End atab7
pwsh tools/DevRemote/session.ps1 -Begin atab8 -Fixture base
pwsh tools/research/preset-check.ps1 -Part files
pwsh tools/research/preset-check.ps1 -Part ui
pwsh tools/DevRemote/session.ps1 -End atab8
```

Expected: 모든 부분 `FAILS=0`. 탭 이름 때문에 실패하면 Step 4를 다시 본다. 다른 이유로 실패하면 고정본에서 한 번 더 돌려 보고(인계 문서 6절), 이번 변경과 관계없는 실패면 보고에 적는다.

- [ ] **Step 7: 커밋**

```bash
git add src/ModCode/ModMain/UI tools/research
git commit -m "feat: 신기 탭 — 신기 셋 켜고 끄기, 쌍어옥패 점수, 호천안 요수·장착, 연요호 호요

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: 검토, 문서, 병합, 설치

- [ ] **Step 1: 검토자 한 명이 브랜치 전체를 본다**

가장 능력 있는 모델의 읽기 전용 검토자에게 `git diff develop...feat/artifact-tab`과 스펙·이 계획을 주고, 범위 고정 문구("이 검토만 하라. 끝나면 보고하고 정지하라. 코드를 고치지 말라. 모든 git·파일 명령에 절대경로 또는 `-C E:\TlToybox`를 쓰라")를 넣는다. Critical·Important는 재현 검사를 먼저 `artifact-check.ps1`에 넣고 고친다. "판단 보류"는 게임에서 실험해 가린다.

- [ ] **Step 2: 배포 구성으로 띄운다**

```bash
pwsh tools/DevRemote/session.ps1 -Begin atab9 -Fixture base -ReleaseBuild
pwsh -Command "& tools/DevRemote/win.ps1 -Do key -Vk 0x2D"
pwsh -Command "& tools/DevRemote/rc.ps1 -Cmd 'hit 151 510' -NoShot"
pwsh -Command "& tools/DevRemote/rc.ps1 -Cmd 'click 151 510' -NoShot"
pwsh -Command "& tools/DevRemote/win.ps1 -Do shot -Out .devremote/shot-artifact-release.png -Scale 0.6"
pwsh tools/DevRemote/session.ps1 -End atab9
```

Expected: `hit`이 `Tab7`(146 + 7 × 52 = 510), 캡처에 신기 탭, `MelonLoader\Latest.log`에 "trampoline" 없음.

- [ ] **Step 3: 문서**

- `docs/05-editor-v0.1.md`에 `## 37. 신기 탭 (2026-10-14 추가)`: 쓰는 법, 규칙(마지막 하나, 28, 0~9999, 지도 버튼은 다시 불러온 뒤), 실측(스펙 2절), 구조, 검증 표, 확인하지 못한 것(스펙 7절과 검토에서 남긴 것).
- `docs/06-handoff.md`: 탭 순서(15개, `Tab0`~`Tab14`: 선법 `Tab6`, 신기 `Tab7`, 운명 `Tab8`, NPC `Tab10`, 세계 `Tab11`, 배율 `Tab12`, 프리셋 `Tab13`, 안전 `Tab14`), 기능 지도에 신기 줄, 3절에 서비스 셋·검사 스크립트, 5절 표에 `artifact-check.ps1`의 부분과 개발 명령, 159줄의 탭 좌표 설명, 8.1절에 묶음, 8.3절을 "끝남"으로, 8.7절 회차 이름(`atab1`~), 1절 직전 작업.
- `docs/07-divine-artifacts.md` 8절 끝에 "편집기에 넣었다(05 문서 37절)".

- [ ] **Step 4: 커밋, 병합, 설치**

```bash
git add docs
git commit -m "docs: 신기 탭(05 문서 37절), 인계 문서

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git checkout develop
git merge --no-ff feat/artifact-tab -m "Merge branch 'feat/artifact-tab' into develop"
pwsh tools/install.ps1
```

Expected: 병합 커밋, 설치 출력에 `MOD_sWP6UL.dll`. 사용자의 게임이 켜져 있으면 끄도록 알리고 기다린다(임의로 끄지 않는다).
