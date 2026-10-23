# 수련 단계 대도까지 채우기 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 공법 탭에서 고른 공법에 옵션을 11개까지 더해(무작위 뽑기 또는 직접 고르기) 수련 단계를 대도까지 만든다. 넣는 길은 제자리 끼워 넣기와 다시 만들기.

**Architecture:** 값 배열 끼워 넣기와 가중치 뽑기는 게임 형식 없는 `Services/MasteryRules.cs`(단위 시험). 게임 쓰기는 새 `Services/MasteryService.cs`(후보·뽑기·끼워 넣기·다시 만들기), 규칙은 `MartialService.OptionProblem`에 `slotFrom` 인자를 더해 재사용. 화면은 `MartialTab`의 단추와 하위 화면.

**Tech Stack:** C# net472, MelonLoader 0.5.4, Unhollower IL2CPP. PowerShell 7 검사 스크립트.

**Spec:** `docs/superpowers/specs/2026-10-23-mastery-fill-design.md`

## Global Constraints

- 브랜치 `feat/mastery-fill`. 커밋 끝 `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- 빌드 오류 0, 단위 시험 `FAILS=0`. 게임 검증은 `session.ps1 -Begin mf-1023a… -Fixture base -Exclude 2814696167`, 끝 `SAVE RESTORED AND IDENTICAL`, 회차 이름 다시 쓰지 않음.
- 값 배열: `[2, 0, 1, 종류, 기본 번호, 경지, 옵션…, -1, 수치…]`, 옵션 0개면 `-1`·수치 없음. 옵션 최대 11.
- 자리 판정은 새 자리에만, 중복·선행·배타는 전체 조합(스펙 6절 "규칙의 범위").
- 새 옵션은 최고 품질(`TopRawOfLevel(…, MaxLevel)`), 옛 옵션 수치는 그대로.
- 사용자 글 한국어, 쓰기마다 `SaveBackup.EnsureOnce()`와 `ChangeLog.Add`.

## Review Focus

1. 자리 밖 옛 옵션(옵션 교체 "강제"로 넣은 것)이 있는 공법 → 후보가 나오고 끼워 넣기·다시 만들기가 된다(Task 2 검사 `forced old option is accepted`).
2. 다시 만들기 중간 실패 → 옛 공법 그대로, 새 공법 남지 않음(Task 2 검사 `remake keeps the old one when make fails` — 개발 명령으로 실패를 흉내).
3. 장착 중인 공법 다시 만들기 → 같은 칸에 새 것이 장착(Task 2 검사 `remake keeps the slot`).
4. 대상이 NPC → NPC 공법만 바뀜(Task 2 검사 `npc target`).
5. 옵션 행 없는 기술(신법 어술 700117) → 단추 꺼짐과 까닭(Task 3 검사 `no option rows`).

---

### Task 1: `MasteryRules`

**Files:** Create `src/ModCode/ModMain/Services/MasteryRules.cs`; Modify `tests/ModMain.Tests/ModMain.Tests.csproj`(링크), `tests/ModMain.Tests/Program.cs`(`MasteryRulesTests()`).

**Interfaces (Produces):**
- `const int MaxOptions = 11;`
- `int[] MasteryRules.InsertOptions(int[] values, int[] add)` — 옵션 끝(첫 -1, 없으면 끝)에 `add`, 없던 -1 추가, 수치 칸 끝에 1을 `add.Length`개. `add`가 비면 같은 배열 복사.
- `int[] MasteryRules.OptionsOf(int[] values)` — 6번째(인덱스 6)부터 -1 앞까지.
- `int MasteryRules.PickWeighted(IList<int> weights, double r)` — `r`∈[0,1) 누적 비례, 가중치 ≤0 제외, 모두 0이면 -1.

- [ ] Step 1 시험: 4절 배열 셋 — `[2,0,1,1,14111,1,1,1,7,1,14,5,11,-1,83,22,85,549,92,66,63]` + `{1,1,1,1}` → 실험 결과 배열; `[2,0,1,1,11423,1]` + `{1,1,1,1,1,1,1,2,3,4,5}` → 실험 결과; 빈 추가 → 같음; `OptionsOf` 두 경우; `PickWeighted({0,3,1}, 0.0)`=1, `(…,0.74)`=1, `(…,0.76)`=2, `({0,0},0.5)`=-1.
- [ ] Step 2 실패 확인(형식 없음). Step 3 구현. Step 4 `FAILS=0`. Step 5 커밋 `feat: 수련 단계 값 배열·가중치 규칙(MasteryRules)과 단위 시험`.

### Task 2: `MasteryService`, 규칙 인자, 개발 명령, 서비스 검사

**Files:** Modify `Services/MartialService.cs`(`OptionProblem(type, baseId, options, anySlot=false, slotFrom=0)` — 자리 판정을 `i >= slotFrom`에서만); Create `Services/MasteryService.cs`, `Dev/DevCommandsMastery.cs`(배포 빌드 제외 폴더), `tools/research/mastery-check.ps1`(부분 `service`); Modify `Dev/DevCommands.cs`(등록).

**Interfaces (Produces):**
- `sealed class MasteryService.State { int Options; int Stages; string LastStage; string Block; }` — `Block`: 더할 수 없는 까닭(11개, 옵션 행 없음, 옛 옵션 규칙 위반, 없는 공법) 또는 null.
- `State MasteryService.Read(WorldUnitBase unit, string sole)`
- `List<ConfBattleSkillPrefixValueItem> MasteryService.Candidates(WorldUnitBase unit, string sole, List<int> pending)` — 다음 자리(`옛 수 + pending 수`)의 후보.
- `List<int> MasteryService.Roll(WorldUnitBase unit, string sole, System.Random rng)` — 자리마다 `Candidates`와 `PickWeighted`, 후보 없으면 멈춤.
- `string MasteryService.Insert(WorldUnitBase unit, string sole, List<int> add)`, `string MasteryService.Remake(WorldUnitBase unit, string sole, List<int> add, out string newSole)` — 스펙 6절 규칙. `crazyID`가 0이 아니면 문구에 "주화입마 기록(crazyID)은 옮기지 않았습니다".
- 개발 명령: `mfstate <sole> [unit]` → `opts=n stages=n last=<이름> block=<까닭|->`, `mfroll <sole> [seed] [unit]` → 번호 목록, `mfcand <sole> <pending,…|-> [unit]` → 후보 번호, `mfinsert <sole> <n,…|roll> [unit]`, `mfremake <sole> <n,…|roll> [unit]` → 문구와 `new=<sole>`, `mfvals <sole> [unit]` → 값 배열·장착·경험치·suit, `mffailnext`(다음 `Remake`의 `Make`를 실패로 흉내. 개발 전용 훅).
- [ ] Step 1 검사 `service`(고정본 플레이어 공법): 현화술(0개) 끼워 넣기 roll → opts 11·stages 12·같은 sole; 청풍검(장착 7, 경험치 3570) 끼워 넣기 → 장착·경험치 같음·옛 옵션 수치 그대로·새 옵션 품질 = 최고(`mopts`); 한공결 전승 10025 뒤 다시 만들기 → 새 sole·장착 칸·경험치·suit 옮김·옛 sole 없음(`remake keeps the slot`); 11개 공법에 다시 → block; `mfcand`의 후보가 모두 그 자리 가중치 > 0(옵션 교체 화면의 판정과 같음, `tb2mlist` 등 기존 명령으로); 옵션 교체 "강제"로 자리 밖 옵션을 넣은 공법(`tb2force` 기존 개발 명령 또는 `MartialService.ReplacePrefix(force)` — 실행 때 확인)에 roll·끼워 넣기 성공(`forced old option is accepted`); `mffailnext` 뒤 다시 만들기 → 실패 문구, 옛 sole 그대로, 공법 수 같음; `trnpc any`로 얻은 NPC id로 `mfinsert … roll <npc>` → NPC만(`npc target`); 이력 줄, Traps 0.
- [ ] Step 2 실패 확인. Step 3 구현. Step 4 회차에서 `service`, `ui-check prefixforce`, `preset-check martial` `FAILS=0`. Step 5 커밋 `feat: 수련 단계 대도까지 채우기 서비스(끼워 넣기·다시 만들기·뽑기)`.

### Task 3: 공법 탭 화면

**Files:** Modify `UI/Tabs/MartialTab.cs`; `tools/research/mastery-check.ps1`(부분 `ui`).

**Interfaces (Produces):** 요소 `MasteryText`(`수련 단계 n/12`), `MasteryOpen`, 하위 화면 `MasteryInfo`, `MasteryRoll`·`MasteryPickMode`(칩), `MasteryReroll`, `MasteryRow<자리>`, `MasteryPick<번호>`, `MasteryUndo`, `MasteryInsert`·`MasteryRemake`(두 번 누르기), `MasteryBlock`(까닭 글).
- [ ] Step 1 검사 `ui`: 현화술 고름 → `MasteryText` "1/12" → `MasteryOpen` → 행 `MasteryRow0`~`10` 있음 → `MasteryReroll` 뒤 행 글이 바뀌거나 같은 길이 → `MasteryInsert` 두 번 → `mfstate` opts 11 → 뒤로 가면 `MasteryText` "12/12"·`MasteryOpen` 꺼짐; 직접 고르기: 다른 공법(건강약 3개)에서 `MasteryPickMode` → 첫 후보 `MasteryPick<n>` 두 번 → 행 둘 → `MasteryUndo` → 하나 → `MasteryRemake` 두 번 → `mfstate` opts 4·새 sole; 신법 어술 없으면 `msnew`처럼 기존 명령으로 700117을 배우게 해 `MasteryOpen` 꺼짐과 까닭 글(`no option rows`). 캡처 `check-mastery.png`.
- [ ] Step 2 실패 확인. Step 3 구현. Step 4 `ui`·`ui-check tabs` `FAILS=0`, 캡처 확인. Step 5 커밋 `feat: 공법 탭 — 대도까지 채우기 화면`.

### Task 4: 유지·전투·정보 창·회귀

**Files:** `tools/research/mastery-check.ps1`(부분 `persist` → `-Restart` → `persist-restart`, `battle`).
- [ ] `persist`: 끼워 넣기 하나·다시 만들기 하나 → `save`. `persist-restart`: 둘 다 opts 11, 장착·경험치 그대로. `battle`: `fight 1011 20` → 전투 진입 단추 이름으로 누르기(`btnBattle`) → `bcast 1·2` → `rmart` 숙련 증가 → `bend`. 정보 창: 청풍검 경험치를 대도까지(`mexp`) → `msui`와 같은 창 열기 개발 명령(이 브랜치에 `mfui <sole>`) → 캡처 "(11/11)".
- [ ] 회귀: `ui-check base`·`tabs`·`actions`, `trainer3-check suit`, `treasure-check service`, 단위 시험, 배포 빌드(`bin\Release` DLL 하나). 커밋 `test: 대도까지 채우기 유지·전투 검사`.

### Task 5: 문서

05 문서 83절(실험·기능·검사·함정), 06 인계(1절 날짜 줄, 2절 공법 줄, 5절 검사 표, 8.0절 1번 끝남과 다음 일, 8.7절 실험 브랜치 `feat/mastery2-research`), 커밋 `docs: 대도까지 채우기 — 05 문서 83절, 06 인계`.
