# 배율 서비스 다듬기(묶음 2) 설계 (2026-10-30)

13 문서 7절 묶음 2. 근거: 13 문서 A2·A4·A5, T1·T3, R1·R7, 2.6절(전투 단위를 포인터로만 구분). 사용자 지시 "묶음 2 시작"(질문 없이 진행).

## 1. 목표와 범위

- **동작은 바뀌지 않는다**(배율의 수치·반올림·하한은 지금 그대로). 바뀌는 것은 전투 단위 상태의 전환 규칙(겹침·누락을 막음)과 코드의 자리뿐이다.
- 범위 밖: 피해 배율을 동료에게 걸지 않는 것(기존 동작 유지), 새 배율 줄, UI.

## 2. 전투 단위 상태의 전환 규칙(`RateUnitRules`, 순수)

`RateService`의 세 틱(쿨타임 비율 `TickCooldown`, 기술 값 `TickSkillValues`, 전투 이동 속도 `TickBattleMove`)은 "전투 플레이어 단위 하나에 떠 둔 원래 값"을 가진다. 지금은 포인터(`IntPtr`)로만 단위를 구분하고, 전투 밖에서 줄이 모두 꺼지면 떠 둔 것을 잊는다(13 문서 A4: 쓴 값이 남은 채 잊어 그 전투 끝까지 곱해진 값이 남을 수 있다).

규칙(프레임마다 한 번 판정):

| 상황 | 판정 | 뜻 |
|---|---|---|
| 전투 밖, 쓴 값이 남아 있음(`applied`) | `Skip` | 잊지 않는다. 같은 단위가 다시 보이면 되돌린다 |
| 전투 밖, 쓴 값 없음 | `Forget` | 떠 둔 것을 지운다(줄이 켜져 있어도. 다음 프레임에 다시 뜬다) |
| 전투 안, 떠 둔 단위 없음 | 켜진 줄이 있으면 `Capture`, 없으면 `Skip` | 원래 값을 뜬다 |
| 전투 안, 다른 포인터 | 켜진 줄이 있으면 `Capture`, 없으면 `Forget` | 앞 단위는 사라졌다. 쓴 값도 함께 사라졌다 |
| 전투 안, 같은 포인터, 쓴 값이 **마지막으로 쓴 것과 다름**(`!intact`) | 켜진 줄이 있으면 `Capture`, 없으면 `Forget` | 게임이 값을 바꿨거나 같은 주소에 새 단위가 잡혔다(2.6절). 옛 원래 값을 쓰지 않는다 |
| 전투 안, 같은 포인터, 그대로 | `Apply` | 줄마다 `WritesRate`로 쓰고, 모두 꺼졌으면 되돌린 뒤 잊는다(지금과 같음) |

"마지막으로 쓴 것과 다름"의 판정은 값마다 `Written`(마지막으로 쓴 baseValue)을 두고 지금 `baseValue`와 비교한다. 아직 쓰지 않았으면(`applied` 거짓) 비교하지 않는다.

- 쿨타임 비율: `cooldownUnit`에 `skillWritten`·`stepWritten`·`itemWritten`을 더한다. 단위 수준의 `intact`는 늘 참으로 두고, **줄마다** 원래 값을 정한다(`RowOriginal`: 안 쓴 줄은 지금 값, 쓴 줄은 마지막으로 쓴 값 그대로면 떠 둔 원래 값, 다르면 지금 값). 한 줄이 바뀌었다고 다른 줄을 다시 뜨지 않는다(최종 리뷰 뒤 고침. 처음 안 "하나라도 다르면 셋을 다시 뜬다"는 그대로였던 줄의 배율을 겹치게 했다).
- 기술 값(`skillValues`): 항목마다 `Written`. 같은 단위에서 항목의 `baseValue != Written`이면 그 항목만 버린다(다른 항목은 그대로). `applied`는 `skillValues.Count > 0`(되돌린 항목은 지워진다).
- 전투 이동 속도는 처리기(`AddValue`)라 값 비교 대신 **붙어 있는지**를 본다(A2): `data.moveSpeed.allAddCall`에 태그 `TlToybox.battleMove`가 있으면 붙어 있다. 켜져 있고 안 붙어 있으면 붙이고, 꺼져 있고 붙어 있으면 뗀다. 포인터는 "붙인 단위의 DynInt를 기억"하는 데만 쓴다. 전투 밖에서는 켜져 있으면 `Skip`, 꺼져 있으면 잊는다(처리기는 단위와 함께 사라진다).

## 3. 순수 함수로 떼는 것(`RateGroups`, 단위 시험)

동작 불변. 반올림 방식이 함수마다 다른 것은 **그대로 둔다**(바꾸면 검사의 기대값이 바뀐다). 시험이 그 차이를 적는다.

| 지금 | 새 이름 | 규칙 |
|---|---|---|
| `RateService.Scale(rate, value)` | `RateGroups.ScaleValue(int value, float rate)` | 0 이하 그대로, `Math.Round`(짝수 반올림), int 최댓값으로 자름 |
| `RateService.ScaleDay(day)` | `RateGroups.ScaleDay(int day, float rate)` | 0 이하 그대로, 0.5 올림(AwayFromZero), 자르지 않음 |
| `RateService.Scaled(original, factor)` | `RateGroups.ScaleSkillValue(int original, float rate)` | 0 이하 그대로, 0.5 올림, 자르지 않음 |
| `RateService.CooldownTarget(original, factor)` | `RateGroups.CooldownRateValue(int original, float factor, long max)` | 0 이하 그대로, 짝수 반올림, `max`로 자름 |
| `BattleService.OnHitCore`의 받는 피해 | `RateGroups.TakenDamage(int before, float rate)` | 0 이하 그대로, 짝수 반올림, 최소 1 |
| `BattleService.OnHitCore`의 주는 피해 배수 | `RateGroups.DealtDamage(int before, int multiplier)` | **0 이하 그대로**(A5: 음수가 뒤집히지 않게), 곱해서 int 최댓값으로 자름 |

## 4. 리팩토링

- R1: `MartialService`의 `^\{"suitID":\d+\}$` 세 곳 → `MartialService.IsPlainSuitRecord(string)`.
- R7: `RateService.cs`(1116줄)를 `RateService.cs`(Rate 정의·단계·패치·접두)와 `RateService.Tick.cs`(매 프레임 틱 다섯과 전투 단위 상태)로 나눈다. 코드 이동만.

## 5. 검증

- 단위 시험: `RateUnitRules.Decide`의 표 6행(각 갈래), `MoveAttachNeeded`/`MoveDetachNeeded`, 3절의 함수 여섯(반올림 차이 포함).
- 회차(고정본, `-Exclude 2814696167`): `ui-check` `rates`(깨끗한 회차 첫 검사) → `rates-battle` → `combat`; `rates2-battle` → `mpnone`; `trainer4-check` `items` → `skills`; `durable`. 모두 FAIL 0이면 동작 불변.
- 새 규칙이 실제로 다르게 행동하는 상황(한 전투 안에서 `playerUnitCtrl`이 잠깐 사라짐, 같은 주소 재사용)은 도구로 만들 수 없다. 단위 시험과 코드로만.
