# LocalText 밖 중국어 글 한글화 — 구현 계획 (2026-10-17)

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans(이 세션이 직접 수행). 단계는 `- [ ]` 체크박스.

**Goal:** 창작마당 모드의 LocalText 밖 중국어(표 칸·종족 JSON·DLL 라벨·버튼 글)가 게임과 편집기 화면에서 한국어로 보이게 한다.

**Architecture:** 원문 기반 번역표 `Translations/texts.txt`를 `TextTranslationService`가 읽어 (1) 시작 때 다섯 표의 글 칸을 전체 일치로 바꿔 끼우고 (2) `TMP_Text.text`·`Text.text` 설정자의 Harmony 앞붙임이 한자가 든 글을 `TextReplacer`(전체 일치 → 긴 원문부터 부분 치환, 한 글자 제외)로 바꾼다.

**Tech Stack:** C# 7.3, MelonLoader 0.5.4 IL2CPP, HarmonyLib(0Harmony), Unity.TextMeshPro·UnityEngine.UI 참조(csproj에 있음), 시험은 `tests/ModMain.Tests`(net8.0, `dotnet run -c Release`).

**Spec:** `docs/superpowers/specs/2026-10-17-zh-text-korean-design.md`

## Global Constraints

- 게임 폴더(모드 JSON 포함)는 건드리지 않는다. 번역표는 DLL 내장 + 데이터 폴더 `translations\texts.txt`.
- 번역표 줄 형식: `원문<TAB>한국어`, `\n`은 두 글자, 자리 표시(`{…}`·`<…>`·`&…&`)는 원문과 같은 수.
- 한 글자 원문은 전체 일치에만 쓰고 부분 치환에 쓰지 않는다.
- 한자가 없는 글은 설정자에서 바로 돌려보낸다(성능).
- 검증은 회차(`session.ps1 -Begin <새 이름> -Fixture base`) 안에서만.

## Review Focus

- 한자 검사: U+4E00~U+9FFF와 U+3400~U+4DBF만 한자로 본다. 한글·가나·전각 기호만 있는 글은 치환하지 않는다 → Task 1 시험.
- 전체 일치가 부분 치환보다 먼저다(`无` 같은 한 글자도 전체 일치면 바뀐다) → Task 1 시험.
- 긴 원문 우선: `变身技-`와 `变身技`가 둘 다 있을 때 `变身技-幽莹`은 `변신기-유영`이 된다 → Task 1 시험.
- 번역표가 비었거나 없을 때 패치를 걸지 않고 게임이 그대로 돈다 → Task 2(회차에서 파일을 비워 확인하지는 않는다. 코드 검토).
- 치환 결과가 다시 설정자로 들어와도(재귀) 한자가 없으므로 끝난다 → Task 2 코드 검토 + 회차 `rzhstat`의 호출 수.

---

### Task 1: TextReplacer(순수 규칙)와 단위 시험

**Files:**
- Create: `src/ModCode/ModMain/Services/TextReplacer.cs`
- Modify: `tests/ModMain.Tests/ModMain.Tests.csproj`(링크 한 줄), `tests/ModMain.Tests/Program.cs`(시험 블록)

**Interfaces:**
- Produces: `internal sealed class TextReplacer { TextReplacer(Dictionary<string,string> map); int Count {get;} static bool HasChinese(string s); string Replace(string text, out bool changed); }`

- [ ] **Step 1: 시험 먼저** — `Program.cs`의 TranslationTable 블록 뒤에 다음을 넣는다.

```csharp
            // TextReplacer(원문 기반 치환. 스펙 2026-10-17-zh-text-korean-design.md 3.3절)
            var zh = new TextReplacer(new Dictionary<string, string> { { "变身技-", "변신기-" }, { "变身技", "변신기" }, { "幽莹", "유영" }, { "无", "없음" }, { "（未解锁）", "(미해금)" }, { "体力{icon_tili}*<y>100</y>%", "체력{icon_tili}*<y>100</y>%" } });
            bool changed;
            Check("zh: HasChinese sees CJK only", TextReplacer.HasChinese("变身") && !TextReplacer.HasChinese("변신 abc") && !TextReplacer.HasChinese("") && !TextReplacer.HasChinese(null) && TextReplacer.HasChinese("a㐀b"));
            Check("zh: whole match wins even for one char", zh.Replace("无", out changed) == "없음" && changed);
            Check("zh: one-char key is not used for partial replace", zh.Replace("无敌", out changed) == "无敌" && !changed);
            Check("zh: longer key first", zh.Replace("变身技-幽莹（未解锁）", out changed) == "변신기-유영(미해금)" && changed);
            Check("zh: placeholders kept", zh.Replace("体力{icon_tili}*<y>100</y>%，灵力", out changed) == "체력{icon_tili}*<y>100</y>%，灵力" && changed);
            Check("zh: text without CJK returns same instance", object.ReferenceEquals(zh.Replace("변신기", out changed), "변신기") && !changed);
            Check("zh: null and empty pass through", zh.Replace(null, out changed) == null && !changed && zh.Replace("", out changed) == "" && !changed);
            Check("zh: count excludes nothing, empty map replaces nothing", zh.Count == 6 && new TextReplacer(new Dictionary<string, string>()).Replace("变身", out changed) == "变身" && !changed);
```

- [ ] **Step 2: 링크 추가** — csproj의 `TranslationTable.cs` 링크 줄 뒤에 `<Compile Include="..\..\src\ModCode\ModMain\Services\TextReplacer.cs" Link="Linked\TextReplacer.cs" />`.
- [ ] **Step 3: RED** — `dotnet run -c Release --project tests/ModMain.Tests` → 컴파일 오류(`TextReplacer` 없음)로 실패함을 확인.
- [ ] **Step 4: 구현** — `TextReplacer.cs`: 생성자에서 전체 일치 사전(원문→한국어)과 부분 치환 목록(길이≥2, 길이 내림차순·같은 길이는 서수 오름차순)을 만든다. `Replace`: null/빈/한자 없음 → 같은 인스턴스, `changed=false`; 전체 일치 → 그 값; 아니면 `StringBuilder` 없이 `string.Replace`를 목록 순서로 적용(`Ordinal`), 바뀌면 `changed=true`.
- [ ] **Step 5: GREEN** — 시험 실행, `FAILS=0`.
- [ ] **Step 6: 커밋** — `feat: TextReplacer — 원문 기반 치환 규칙과 시험`.

### Task 2: TextTranslationService + TextPatch + 개발 명령

**Files:**
- Create: `src/ModCode/ModMain/Services/TextTranslationService.cs`, `src/ModCode/ModMain/UI/TextPatch.cs`
- Modify: `src/ModCode/ModMain/ModMain.cs`(Init에서 호출), `src/ModCode/ModMain/Dev/DevCommandsZh.cs`(`rzhtext`·`rzhstat`), `src/ModCode/ModMain/Translations/texts.txt`(임시 두 줄: `变身技-\t변신기-`, `幽莹\t유영` — Task 3이 채운다)

**Interfaces:**
- Consumes: `TextReplacer`, `TranslationTable.Parse/MissingLines`, `ChangeLog.DataDir`.
- Produces: `TextTranslationService.Apply()`(요약 글 돌려줌: `file=… rows=… tables=… cells=…`), `TextTranslationService.Replacer`(null 가능), `TextTranslationService.LastSummary`; `TextPatch.Ensure()`(null 또는 실패 글), `TextPatch.Replaced`(치환 수), `TextPatch.Calls`(한자 검사 수), `TextPatch.Ticks`(누적 Stopwatch 틱).

- [ ] **Step 1: 서비스** — `TranslationService`를 본떠 `ResourceName = "Translations/texts.txt"`, `FilePath = <translations>\texts.txt`, `EnsureFile`(풀어 둠/덧붙임/있음/없음), `Load`, `Apply()`: 사전을 읽어 `Replacer`를 만들고 `ApplyTables()`로 다섯 표의 칸을 바꾼다. `ApplyTables`는 `typeof(ConfMgr).GetProperty(표)`→`allConfList` 또는 `_allConfList`→줄마다 대상 속성(`GetProperty(이름)`)의 값에 한자가 있고 사전에 전체 일치가 있으면 `SetValue`. 대상: `battleSkillAttack`(`name`,`desc`), `battleEffect`(`desc`), `roleCreateFeature`(`name`,`tips`), `modNpcAddWorld`(`name1`,`name2`,`joinSchoolName`), `dramaOptions`(`text`), `dramaDialogue`(`dialogue`). 예외는 표 단위로 잡아 요약에 `오류 n`.
- [ ] **Step 2: 패치** — `TextPatch.Ensure()`: `Replacer`가 null이거나 `Count==0`이면 걸지 않음. `new HarmonyLib.Harmony("TlToybox.text")`로 `typeof(TMPro.TMP_Text).GetProperty("text").GetSetMethod()`와 `typeof(UnityEngine.UI.Text).GetProperty("text").GetSetMethod()`에 앞붙임 `static void Prefix(ref string value)`: `Calls++`; `HasChinese(value)`가 아니면 return; `Stopwatch` 재어 `value = Replacer.Replace(value, out changed)`; 바뀌면 `Replaced++`. 실패 시 `UnpatchSelf`·로그(`PointerGuard` 꼴).
- [ ] **Step 3: Init 연결** — `ModMain.Init`의 `TranslationService.Apply();` 뒤에 `TextTranslationService.Apply(); TextPatch.Ensure();`(각각 try/catch, 실패 로그 `[TlToybox] texts failed: …`). `TextTranslationService.Apply`는 `[TlToybox] texts: <요약>` 로그.
- [ ] **Step 4: 개발 명령** — `DevCommandsZh`에 `rzhtext <원문…>`(인자들을 공백으로 이어 `Replacer.Replace` 결과와 changed), `rzhstat`(`TextTranslationService.LastSummary` + `TextPatch`의 `Replaced`·`Calls`·누적 ms + 패치 실패 글).
- [ ] **Step 5: 빌드** — `dotnet build src/ModCode/ModMain/ModMain.csproj -c Release` 오류 0. 배포 구성(`-p:DevTools=false`)도 빌드해 Dev 폴더 밖 코드가 DEVTOOLS에 기대지 않음을 확인.
- [ ] **Step 6: 커밋** — `feat: 원문 기반 번역표 texts.txt — 표 칸 바꿔 끼움과 화면 글 치환(Harmony)`.

### Task 3: 번역표 내용과 검사 스크립트

**Files:**
- Modify: `src/ModCode/ModMain/Translations/texts.txt`
- Create: `tools/research/check-texts.py`

- [ ] **Step 1: 검사 스크립트** — `check-texts.py <texts.txt>`: 줄 형식(탭), 중복 원문, 한 글자 원문(경고), 자리 표시 집합(`\{[^{}]*\}`·`<[^<>]*>`·`&[^&\s]*&`·`\\n`)이 원문과 번역에서 같은 수인지, 번역에 한자가 남았는지(괄호 병기 허용 표시 `#병기` 주석 줄 다음은 제외) → 문제 0이면 `OK n줄`.
- [ ] **Step 2: 원문 모으기** — `.devremote\zh-display-20261017.txt`의 다섯 표 원문(원작 표 셋 제외) + 53절의 JSON 30개(`race-json.py`로 다시 뽑기) + DLL 라벨 25개 + 버튼 8개 + 성·이름 붙인 꼴 11개.
- [ ] **Step 3: 번역** — 스펙 3.4의 규칙으로 한국어를 적는다(음독: 기술·변신·NPC 이름, 뜻: 설명·라벨). 절 머리 주석(`# ---- 표: battleSkillAttack.name ----` 등).
- [ ] **Step 4: 검사** — `py -3 -I -X utf8 tools/research/check-texts.py src/ModCode/ModMain/Translations/texts.txt` → `OK`.
- [ ] **Step 5: 커밋** — `feat: texts.txt — LocalText 밖 중국어 글 n줄 한글화`.

### Task 4: 회차 검증

- [ ] **Step 1: 고정본 회차 `zh-1017b`** — `-Begin zh-1017b -Fixture base`; `Latest.log`에 `[TlToybox] texts: file=풀어 둠 rows=… tables=5 cells=…`; `rzhstat`; `rzh all` → `battleSkillAttack`·`battleEffect`·`roleCreateFeature`·`modNpcAddWorld`·`dramaOptions`·`dramaDialogue`가 요약에서 사라짐(또는 `joinSchoolName`만 남음); `rzhtext 变身技-幽莹（未解锁）` → `변신기-유영(미해금)`; `rzhtext 种族主动技能:` 등 라벨 셋.
- [ ] **Step 2: 화면** — `-End zh-1017b` 뒤 `-Begin zh-1017c -Fixture base -Login`으로 캐릭터 만들기 → 여성 → 종족 탭(종족 기술 설명이 보이는 곳)을 찍어 한국어로 보이는지(`.devremote\zh-1017c-*.png`). `rzhstat`로 치환 수·누적 ms.
- [ ] **Step 3: 종료** — `-End zh-1017c` `SAVE RESTORED AND IDENTICAL`.
- [ ] **Step 4: 시험** — `dotnet run -c Release --project tests/ModMain.Tests` `FAILS=0`, `pwsh tools/DevRemote/session.tests.ps1` `FAILS=0`.

### Task 5: 문서·병합

- [ ] 05 문서 54절(설계 요약·실측·한계), 06 인계 1절(설치본에 없음을 표시)·8절, 커밋, `develop`에 `--no-ff` 병합, 브랜치 삭제.
