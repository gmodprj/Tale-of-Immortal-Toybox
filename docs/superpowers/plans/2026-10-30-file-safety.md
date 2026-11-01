# 파일 안전과 예외(묶음 4) 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 사용자가 손으로 고치는 파일(번역표 셋·`config.txt`)을 쓰는 도중 게임이 죽어도 파일이 사라지지 않게 하고, 리뷰가 짚은 예외 경로 여섯을 닫는다. 동작은 바뀌지 않는다.

**Architecture:** 원자적 쓰기 도우미 `AtomicFile.WriteAllText(path, text, encoding)` 하나(임시 파일 → `File.Replace`/`Move`. `FavoriteStore.Change`의 꼴). 쓰기 지점은 그것을 부르고, 예외 경로는 각 자리에서 try·범위 넓히기.

**Spec:** `docs/13-review-2026-10-29.md` 3절 A7·A8·A10·A11·A12·A15, 4절 B22. 사용자 지시 "묶음 4 시작".

## Global Constraints

- 브랜치 `fix/file-safety`. 파일의 내용·인코딩(UTF-8 BOM 없음)·줄 끝은 그대로.
- 회차 이름 `fs-a-1030`부터.

## Review Focus

1. 번역 파일에 덧붙이는 도중 예외가 나도 원래 파일이 그대로 남아야 한다 — Task 1의 `AtomicFile` 시험(임시 파일 남김 없음, 대상 없을 때 Move, 있을 때 Replace).
2. 다른 프로세스가 대상 파일을 잠근 경우 `File.Replace`가 던지면 호출부의 기존 catch가 받아야 한다(번역: `TranslationService.Ensure`의 호출부. 확인만).
3. 깨진 공법 하나가 있어도 전승 화면·배우기 수 세기가 통째로 실패하지 않아야 한다 — A11(게임에서는 `mbreak`로 깨진 공법을 만들어 `suit-ui`·`tabs`로 확인).
4. 프리셋 폴더에 접근할 수 없을 때 프리셋 탭이 "이 탭을 그리지 못했습니다"가 아니라 빈 목록이어야 한다 — A8(코드로만. 폴더 접근 거부를 회차에서 만들기 어렵다).
5. `TextPrefix`가 던지면 게임의 글이 원문 그대로 남고 로그 한 줄만 — B22(코드로만).

---

### Task 1: `AtomicFile`(순수 입출력) + 시험

**Files:** Create `src/ModCode/ModMain/Services/AtomicFile.cs`; Modify `tests/ModMain.Tests/ModMain.Tests.csproj`, `Program.cs`(`AtomicFileTests`).

**Interfaces (Produces):** `internal static class AtomicFile { public static void WriteAllText(string path, string text, Encoding encoding); }` — 폴더가 없으면 만들고, `path + ".tmp"`에 쓴 뒤 대상이 있으면 `File.Replace(tmp, path, null)`, 없으면 `File.Move(tmp, path)`. 쓰기 도중 예외는 그대로 던지되 임시 파일은 지운다.
- [ ] Step 1 시험(RED): 임시 폴더에서 ① 새 파일 쓰기 → 내용 일치·`.tmp` 없음 ② 덮어쓰기 → 새 내용·`.tmp` 없음 ③ BOM 없는 UTF-8(첫 바이트가 0xEF가 아님) ④ 없는 하위 폴더에 쓰기 → 폴더 생김.
- [ ] Step 2 RED(형식 없음) → Step 3 구현 → Step 4 `FAILS=0` → Step 5 커밋 `feat: AtomicFile — 임시 파일 뒤 바꿔치기 쓰기(시험 4줄)`.

### Task 2: 쓰기 지점 교체(A7)와 예외 범위(A15)

**Files:** `Services/TranslationService.cs:54,77,162`, `Services/TextTranslationService.cs:86,94`, `Services/ConfigFile.Io.cs`, `Services/MartialGridService.cs:186`, `Services/FavoriteStore.cs:342-351`(도우미로 바꿈. `.bak` 복사는 그대로).
- `ConfigFile.Write`: `File.WriteAllLines` → `AtomicFile.WriteAllText(FilePath, string.Join(Environment.NewLine, lines) + Environment.NewLine, new UTF8Encoding(false))`? **아니다** — 기존 `WriteAllLines`는 UTF-8 BOM 없이 CRLF를 쓴다. 같은 결과가 되게 `string.Join(Environment.NewLine, …) + Environment.NewLine`으로 하고, 인코딩은 `new UTF8Encoding(false)`. `EnsureFile`의 `File.WriteAllText(FilePath, text)`도 같은 인코딩(기본 `WriteAllText`는 BOM 없는 UTF-8).
- `ConfigFile.Read`·`Write`·`EnsureFile`: `catch (IOException)`+`catch (UnauthorizedAccessException)` → `catch (Exception e)` 하나(A15·R9 겸).
- 프리셋 파일 쓰기(`PresetService.cs:144,187`)와 `ExportFiles.Write`(새 파일만, 덮어쓰지 않음)는 그대로 둔다(덮어쓰는 사용자 파일이 아님).
- [ ] Step 1 구현 → Step 2 빌드 0 오류, 단위 시험 FAILS=0 → Step 3 커밋 `fix: 번역표 셋·config.txt·martial-grid.txt·즐겨찾기를 AtomicFile로 쓰고, config 입출력은 모든 예외를 로그로(13 문서 A7·A15)`.

### Task 3: 예외 경로(A8·A10·A11·A12·B22)

**Files:** `Services/PresetService.cs:243-246`, `Services/AppellationService.cs`(`ForgetNames` 옆에 `ForgetQuiet()` 추가 → `ModMain.Destroy`에서 호출), `Services/MartialService.cs:782-800`(`SuitsFor` 루프), `Services/SkillCatalog.cs:291-300`(`LearnedCounts` 루프), `Services/PresetService.Apply.cs:155-161`, `UI/TextPatch.cs:58-83`, `ModMain.cs`(Destroy).
- A8: `FolderStamp`를 try로 감싸 실패하면 `"ERR:" + DateTime.UtcNow.Ticks`(매번 다른 표지 → 매번 다시 읽음. `List()`가 삼킨다).
- A10: `public static int ForgetQuiet()`: `quiet`·`quietLoss`·`lossOf`를 비우고 비운 수를 돌려준다. `ModMain.Destroy`의 `AppellationService.ForgetNames();` 옆에 `AppellationService.ForgetQuiet();`.
- A11: 두 루프의 `To<MartialData>()`와 그 줄의 읽기를 `try { … } catch (Exception) { continue; }`로(`CharacterExportService.Import.cs:29-45`의 꼴). 첫 예외 한 번만 로그(`MelonLogger.Msg("[TlToybox] 깨진 공법 건너뜀(전승/배운 수): " + e.GetType().Name)` — 정적 `bool brokenWarned`).
- A12: `finally`의 되돌리기를 `try { MartialService.Equip(me, other); } catch (Exception e) { MelonLogger.Msg("[TlToybox] 프리셋 되돌리기 실패: " + e.GetType().Name + ": " + e.Message); }`. 심법의 칸 번호 보존은 `Equip`의 칸 선택 규칙을 바꿔야 해 하지 않는다(13 문서에 남김).
- B22: `TextPrefix` 본문을 try로 감싸고 `catch (Exception e)`에서 `value`는 건드리지 않고 `Failure`와 다른 정적 `string prefixError`에 첫 예외만 로그(`[TlToybox] 화면 글 치환 오류(원문 그대로 둠): …`).
- [ ] Step 1 구현 → Step 2 빌드 0 오류, 단위 시험 FAILS=0 → Step 3 커밋 `fix: 프리셋 표지·전승/배운 수의 깨진 공법·프리셋 되돌리기·화면 글 치환의 예외 경로, 도호 조용 표시를 세이브 떠날 때 비움(13 문서 A8·A10·A11·A12·B22)`.

### Task 4: 회차 검증

- [ ] `fs-a-1030`(`-Exclude` 없이 — `translation`은 만고신화가 필요): `ui-check translation`(파일 수정·BOM·삭제·주석만·미번역 내보내기) → `favstore` → `-Restart` → `favstore-restart` → `move` → `-Restart` → `move-restart`(config.txt 쓰기). 끝에 데이터 폴더에 `*.tmp`가 없는지(`translations\*.tmp`, `config.txt.tmp`, `favorites.txt.tmp`).
- [ ] `fs-b-1030`(`-Exclude 2814696167`): `mbreak`로 깨진 공법 하나 → `trainer3-check suit-ui`, `ui-check tabs`(공법 탭·기술 추가 화면이 그려짐), `ui-check grid`(학습 상한 파일 쓰기), `preset-check ui`(프리셋 탭 표지), `appellation-ui`(도호). 모두 FAIL 0.
- [ ] 단위 시험 FAILS=0.

### Task 5: 문서·병합

- [ ] 05 문서 92절, 06 1절 줄·8.0절 9번(묶음 4 끝남)·3절 파일 표(`AtomicFile.cs`), 13 문서 A7·A8·A10·A11·A12·A15·B22 행 `고침(92절)`. 커밋, `develop` 병합.
