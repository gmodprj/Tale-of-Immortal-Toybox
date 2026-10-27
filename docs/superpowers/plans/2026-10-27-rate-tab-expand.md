# 계획: 배율 탭 확장 (2026-10-27)

스펙 `docs/superpowers/specs/2026-10-27-rate-tab-expand-design.md`. 브랜치 `feat/rate-tab-expand`. 이 세션에서 직접 실행한다.

## 과제

1. **순수 논리(단위 시험 먼저)** `Services/RateGroups.cs`: 칩 다섯의 이름과 줄 키 목록(`Groups`), `OnKeys(Func<string,bool> isOn)`로 "켜 둔 것" 거르기, `EnergyRefund(int before, int now, float rate)`(줄었으면 줄어든 양×(1−rate)를 반올림해 돌려줌, 늘었으면 0). 줄 키는 `RateService`의 `Key`(`exp` …)와 `multiplier`·`god`. 시험: 18줄이 정확히 한 칩씩, 새 줄 넷이 맞는 칩, 켜 둔 것 거르기, 돌려주기(0.5에서 10 → 5, 없음 10 → 10, x1 → 0, 늘어남 → 0, 홀수 반올림).
2. **새 배율 넷** `RateService`:
   - `MapSpeed`(`mapSpeed`, "지도 속도", "지도 속도", 1·1.5·2·3), `BattleMove`(`battleMove`, "전투 이동 속도", "이동 속도", 1·1.5·2·3), `IntimGain`(`intimGain`, "호감 변화", "호감", 1·2·5·10, Hook `IntimPrefix`, Target `DataUnit.RelationData.AddIntim`), `EnergyCost`(`energyCost`, "정력 소모", "정력 소모", 1·0.5·0.25·0, ZeroText "없음").
   - `All`에 넣는다(위쪽 표시·이력·`Reset`이 따라온다).
   - `Tick`: 지도 속도 — 세계(`g.world`)가 있고 `!InBattle`이면 timeScale을 맞추고 `mapApplied`로 기억, 끄거나 전투면 되돌림. 전투 속도의 `speedApplied`와 따로 둔다. 전투 이동 — `TickBattleMove`(플레이어 `data.Pointer`가 바뀌면 새로 걸기, 배수가 바뀌면 add 값만 갱신, 끄면 `DelValue`). 정력 — `TickEnergy`(지난 프레임 값보다 줄었으면 `RateGroups.EnergyRefund`만큼 더함, `energyMax`를 넘지 않게).
   - 호감 접두 `IntimPrefix(RelationData __instance, string __0, ref float __1)`: 플레이어가 주체/대상이고 `__1 > 0`이면 `__1 *= IntimGain.Value`.
   - 편집기의 정력 쓰기: `RateService.ForgetEnergy()`를 `EditService.SetAttribute`(key `energy`)와 캐릭터 가져오기 쓰기 뒤에 부른다(기준 값만 다시 잡음).
   - `OnDestroy`: 지도 속도 되돌림, 전투 이동 단위·정력 기준 비움.
3. **배율 탭 칩** `RateTab`: 칩 줄(`RateGroup0`~`4`, 글 `성장 1` 꼴), 고른 칩 필드(정적, 세션 동안), 줄 그리기를 키→줄 함수 표로. "켜 둔 것"이 비면 안내 글 `RateEmpty`. 개발 빌드 전용 `ShowAll`(모든 줄, 줄 높이를 맞춰 줄임) — 기존 검사용.
4. **개발 명령** `Dev/DevCommandsRate.cs`에: `ratechip <0~4|all|off>`, `rtrack start|read`, `rchat <unitID>`(대화 행동), `rintim <unitID> <값>`, `renergy`, `rmove`. 개발 빌드에서는 `ShowAll`이 처음부터 켜져 있다(기존 검사는 줄 이름을 그대로 누른다. 설치본은 `-p:DevTools=false`라 칩만). 새 검사 `rates2`가 `ratechip 0`으로 끄고 끝에 `ratechip all`로 되돌린다.
5. **검사** `ui-check.ps1`: `rates2`(칩·수·줄·새 줄 단추와 설명·켜 둔 것·`모두 1배로`), `rates2-map`, `rates2-battle`, `rates2-life`(스펙 5절).
6. **빌드·단위 시험 → 회차**: `rx-a-1027`(`-Exclude`): `rates2`·`rates2-map`·`rates2-life`, `rx-b-1027`: `rates`(첫 검사로)·`rates2-battle`·`rates-battle`, `rx-c-1027`: `itemuse`·`mpnone`·`base`·`tabs`, `combat`은 시간 나면.
7. opus 검토자 → 문서(05 문서 86절, 06 문서) → 사용자 확인 뒤 병합.
