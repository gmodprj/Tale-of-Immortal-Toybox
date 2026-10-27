# 계획: 미룬 사소한 것 묶음 (2026-10-27)

스펙 `docs/superpowers/specs/2026-10-27-deferred-fixes-design.md`. 브랜치 `fix/deferred-1027`. 이 세션에서 직접 실행한다. 진행 기록 `.superpowers/sdd/2026-10-27-deferred-fixes/progress.md`(끝나면 지움).

## 과제

1. **KoreanText(단위 시험 먼저)**: `Services/KoreanText.cs`에 `Object(string word)`(을/를, 한글 아니면 `을(를)`). 시험 프로젝트에 링크, `KoreanTextTests` 6줄(RED → GREEN). `ConfigFile.Io.cs`의 네 줄이 쓴다.
2. **RateGuards**: `IsPlayerBattleData`를 하나씩 null 검사로.
3. **자원 탭**: `SkillDetail`의 모드 경지를 `BookGradeMin~BookGradeMax`로, `BookShown`의 모드 줄은 `ModBookFits && BlockReason == null`.
4. **프리셋 표지·대경지 이름 캐시**: `FolderStamp`에 `.defaults`, `AppellationService.ForgetNames()`를 `TranslationService.Apply` 끝과 `ModMain.Destroy`에서.
5. **법보·전승**: 스펙 2.6의 여섯.
6. **대도까지 채우기**: 스펙 2.7의 일곱. `RaisePoints(unit, extra, out int raisedFrom)`.
7. **검사 줄 더하기**: `books-ui`(상세 글 `경지 2~10`), `trainer3-check suit`(`{"suitID":0}` 떼기), `mastery-check ui`(`대도까지 있음`). 경지 1 원본 없는 모드 줄은 `rmods 1`로 찾아 있으면 줄 수 확인.
8. **빌드·단위 시험** → 회차 셋:
   - `dfx-a-1027`(`-Exclude`): `ui-check base tabs books-ui modbooks`, `preset-check files ui`, `mastery-check ui`
   - `dfx-b-1027`(`-Exclude`): `treasure-check forge give`, `trainer3-check suit`, `mastery-check service`, `ui-check mpnone`
   - `dfx-c-1027`(만고신화 포함): `ui-check translation`, 측정 8·9(`rfav addbook` 뒤 `rperf tabs`, `rbook` 품질 6 시간)
   - 고치기 전 측정(3의 그리기 시간)은 `dfx-0-1027`에서 develop 빌드로 `rperf query`.
9. **opus 검토자 한 번** → Critical·Important만 시험부터 고침.
10. **문서**: 05 문서 85절, 06 문서 8.0절·1절, 각 원래 절 끝의 "미룬 것"에 "85절에서 고침" 표시는 하지 않고 85절에 원래 절 번호를 적는다.
11. 사용자 확인 뒤 `--no-ff` 병합.
