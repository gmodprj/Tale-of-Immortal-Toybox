# 09. 창작마당 모드 등록부 (생성 2026-10-16 13:21)

이 문서는 pwsh tools/workshop.ps1 -Doc 이 만든다. 손으로 고치지 않는다. 고치려면 tools/workshop/mods.txt 와 history.txt 를 고친 뒤 다시 돌린다. 설계: docs/superpowers/specs/2026-10-16-workshop-expand-design.md 3.4절.
게임 폴더 E:\Games\Tale of Immortal, SteamCMD E:\SteamCmd\steamcmd.exe, 받은 자리 E:\SteamCmd\steamapps\workshop\content\1468810.

## 1. 요약

- 상태: 설치 23, 보류 1, 제외 1 (줄 25)
- 설치 줄 23: 받음 23/23, 설치 23/23
- 설치본 크기: 660.8MB (설치된 설치 줄의 폴더 합)
- DLL이 든 설치 모드: 7개

## 2. 모드 표

| ID | 상태 | 종류 | 원문 이름 | 한국어 이름 | 모드ID | 받음 | 설치 | 설치본 | DLL | 비고 | 링크 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 2814703549 | 설치 | 틀 | Fatury | Fatury 틀 | q9CYCo | 1.9MB 2024-12-28 | 예 | 같음 | Fatury.dll,UnityEngine.InputLegacyModule.dll,YamlDotNet.dll | 2815562168이 필요로 함. DLL 3(Fatury.dll, UnityEngine.InputLegacyModule.dll, YamlDotNet.dll) | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2814703549) |
| 2815562168 | 설치 | 외형 | 原版17个女性人物动态立绘替换 | 원작 여성 인물 17명 동적 입화 교체 | ymWzeD | 111.3MB 2023-02-26 | 예 | 같음 | - | Fatury 필요. 111MB | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2815562168) |
| 3200319699 | 설치 | 외형 | 【木头】AI动态立绘 | [木头] AI 동적 입화 | 13mJF0 | 206.4MB 2025-01-06 | 예 | 같음 | - | 206MB | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=3200319699) |
| 3001496635 | 설치 | 외형 | 【软甜废独家】《五官头饰动态立绘》-原创本人手画 | [软甜废] 이목구비·머리 장식 동적 입화 | IoRGjg | 32MB 2025-06-13 | 예 | 같음 | MOD_IoRGjg.dll | DLL 1(MOD_IoRGjg.dll) | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=3001496635) |
| 2993500314 | 설치 | 외형 | 动态头发美化 | 동적 머리카락 미화 | FsilSZ | 15.1MB 2023-06-26 | 예 | 같음 | - |  | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2993500314) |
| 2819712935 | 설치 | 외형 | 女npc颜值优化 | 여성 NPC 외모 개선 | dVhgEv | 0.3MB 2023-07-08 | 예 | 같음 | - | debug 모양(Mod_dVhgEv) | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2819712935) |
| 2823930683 | 설치 | 외형 | 男性NPC颜值优化 | 남성 NPC 외모 개선 | 5AfmFP | 0.7MB 2022-06-23 | 예 | 같음 | - |  | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2823930683) |
| 2945768367 | 설치 | 외형 | 【执余】大量生成美女 | [执余] 미녀 대량 생성 | Z08Zhd | 0.1MB 2023-03-29 | 예 | 같음 | - |  | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2945768367) |
| 2874128983 | 설치 | 외형 | 人物美化 .服装解锁 .魅力与权重修改 | 인물 미화·복장 해금·매력과 가중치 수정 | L25r1F | 1.3MB 2022-10-14 | 예 | 같음 | - |  | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2874128983) |
| 2876769227 | 설치 | 외형 | 【只有梦长】特殊NPC服装解锁 | [只有梦长] 특수 NPC 복장 해금 | FASRM7 | 0.9MB 2025-07-07 | 예 | 같음 | - |  | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2876769227) |
| 2929805068 | 설치 | 외형 | 【大姨夫】仙衣阁、美化 | [大姨夫] 선의각·미화 | cPugE8 | 0.7MB 2023-02-09 | 예 | 같음 | - | debug 모양(Mod_cPugE8) | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2929805068) |
| 2832167498 | 설치 | 외형 | 【含羞草】捏脸 | [含羞草] 얼굴 만들기 | 0lkK4v | 0.6MB 2022-08-20 | 예 | 같음 | MOD_0lkK4v.dll | DLL 1(MOD_0lkK4v.dll) | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2832167498) |
| 2814806198 | 설치 | 외형 | 过场图替换示例mod | 전환 화면 그림 교체 예제 모드 | JprJWu | 15.6MB 2022-06-01 | 예 | 같음 | - |  | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2814806198) |
| 3390738990 | 설치 | 공법 | 【只有梦长】五本功法开源 | [只有梦长] 공법 다섯 권(오픈소스) | 7kyQkw | 1.7MB 2024-12-24 | 예 | 같음 | - | debug 모양(Mod_7kyQkw). 새 공법 다섯 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=3390738990) |
| 2839261049 | 설치 | 공법 | 真.镇宗之宝：传承心法 | 진·진종지보: 전승 심법 | uCKcKw | 0.8MB 2022-08-05 | 예 | 같음 | MOD_uCKcKw.dll | DLL 1(MOD_uCKcKw.dll) | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2839261049) |
| 2948618136 | 설치 | 틀 | [无名]图集框架(修复) | [无名] 도감 틀(수정판) | chaQxF | 0.3MB 2023-05-27 | 예 | 같음 | MOD_chaQxF.dll | 2832340752가 필요로 함. 0.3MB. DLL 1(MOD_chaQxF.dll). 2026-10-16 추가 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2948618136) |
| 2832340752 | 설치 | 외형 | [无名]动态器灵(妖刀姬) | [无名] 동적 기령 요도희 | HrAonB | 9.8MB 2023-03-22 | 예 | 같음 | - | 2948618136 필요. 9.8MB. 영어 지원. 2026-10-16 추가 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2832340752) |
| 2956998877 | 설치 | 외형 | 【执余】剧情NPC实体化 | [执余] 극 NPC 실체화 | Qlrjnh | 67.1MB 2023-04-10 | 예 | 같음 | - | 67MB. 극 NPC 40여 명을 일반 인물로. 2026-10-16 추가 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2956998877) |
| 3018337316 | 설치 | 틀 | 种族框架 | 종족 틀 | vEGKqj | 5.2MB 2025-02-12 | 예 | 같음 | MOD_vEGKqj.dll | 3018994236·3019455501이 필요로 함. DLL 1(MOD_vEGKqj.dll). 설명: 빼기 전에 게임 설정의 "종족 모드 불러오기"를 끈다. 2026-10-16 추가 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=3018337316) |
| 3018994236 | 설치 | 세계관 | 「只有梦长」种族拓展Added different races | [只有梦长] 종족 확장 | LBBKkB | 1.3MB 2025-01-16 | 예 | 같음 | - | 종족 틀 필요. 종족 10종. 2026-10-16 추가 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=3018994236) |
| 3019455501 | 설치 | 세계관 | 龙族（真龙、青龙和应龙） | 용족(진룡·청룡·응룡) | EFuAci | 1.3MB 2024-08-09 | 예 | 같음 | - | 종족 틀 필요. 2026-10-16 추가 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=3019455501) |
| 2859071194 | 설치 | 세계관 | 八荒绝色榜(Eight Desolate Fairies Ranking) | 팔황 절색방 | BHJSB | 4.6MB 2025-04-12 | 예 | 같음 | MOD_BHJSB.dll | 주성 진입으로 극 시작. 4.6MB. DLL 1(MOD_BHJSB.dll). 영어 지원. 2026-10-16 추가 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2859071194) |
| 2834581477 | 설치 | 음악 | BGM背景音乐替换（无需框架） | 배경음악 교체(선검 OST, 틀 불필요) | xOR4lp | 183.3MB 2022-07-13 | 예 | 같음 | - | 183MB. 2026-10-16 추가 | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2834581477) |
| 2945768219 | 제외 | 외형 | 【执余】女NPC颜值评定优化 | [执余] 여성 NPC 외모 평가 개선 | - | 49.2MB 2024-08-05 | - | - | MOD_ZUQmIr.dll,MySql.Data.dll,System.Buffers.dll,System.Memory.dll,System.Threading.Tasks.Extensions.dll,MOD_ZUQmIr.dll,MySql.Data.dll,System.Buffers.dll,System.Memory.dll,System.Threading.Tasks.Extensions.dll,Assembly-CSharp.dll,ModMain.dll,MOD_ZUQmIr.dll,ModMain.dll,Properties.Resources.Designer.cs.dll | DLL 5에 MySql.Data.dll이 들어 있어 제외(2026-10-15) | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=2945768219) |
| 3681025074 | 보류 | - | - | - | - | 15.2MB 2026-03-13 | - | - | - | 사용자 시험용. 목록에 넣지 않기로 함(2026-10-15) | [창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=3681025074) |

## 3. 이력 (최근 것부터)

| 때 | 동작 | ID | 한국어 이름 |
|---|---|---|---|
| 2026-10-16 13:20 | 설치 | 2834581477 | 배경음악 교체(선검 OST, 틀 불필요) |
| 2026-10-16 13:20 | 설치 | 2859071194 | 팔황 절색방 |
| 2026-10-16 13:20 | 설치 | 3019455501 | 용족(진룡·청룡·응룡) |
| 2026-10-16 13:20 | 설치 | 3018994236 | [只有梦长] 종족 확장 |
| 2026-10-16 13:20 | 설치 | 3018337316 | 종족 틀 |
| 2026-10-16 13:20 | 설치 | 2956998877 | [执余] 극 NPC 실체화 |
| 2026-10-16 13:20 | 설치 | 2832340752 | [无名] 동적 기령 요도희 |
| 2026-10-16 13:20 | 설치 | 2948618136 | [无名] 도감 틀(수정판) |
| 2026-10-16 13:09 | 받음 | 2834581477 | 배경음악 교체(선검 OST, 틀 불필요) |
| 2026-10-16 13:09 | 받음 | 2859071194 | 팔황 절색방 |
| 2026-10-16 13:09 | 받음 | 3019455501 | 용족(진룡·청룡·응룡) |
| 2026-10-16 13:09 | 받음 | 3018994236 | [只有梦长] 종족 확장 |
| 2026-10-16 13:09 | 받음 | 3018337316 | 종족 틀 |
| 2026-10-16 13:09 | 받음 | 2956998877 | [执余] 극 NPC 실체화 |
| 2026-10-16 13:09 | 받음 | 2832340752 | [无名] 동적 기령 요도희 |
| 2026-10-16 13:09 | 받음 | 2948618136 | [无名] 도감 틀(수정판) |
| 2026-10-16 --:-- | 설치 | 2839261049 | 진·진종지보: 전승 심법 |
| 2026-10-16 --:-- | 설치 | 3390738990 | [只有梦长] 공법 다섯 권(오픈소스) |
| 2026-10-16 --:-- | 설치 | 2814806198 | 전환 화면 그림 교체 예제 모드 |
| 2026-10-16 --:-- | 설치 | 2832167498 | [含羞草] 얼굴 만들기 |
| 2026-10-16 --:-- | 설치 | 2929805068 | [大姨夫] 선의각·미화 |
| 2026-10-16 --:-- | 설치 | 2876769227 | [只有梦长] 특수 NPC 복장 해금 |
| 2026-10-16 --:-- | 설치 | 2874128983 | 인물 미화·복장 해금·매력과 가중치 수정 |
| 2026-10-16 --:-- | 설치 | 2945768367 | [执余] 미녀 대량 생성 |
| 2026-10-16 --:-- | 설치 | 2823930683 | 남성 NPC 외모 개선 |
| 2026-10-16 --:-- | 설치 | 2819712935 | 여성 NPC 외모 개선 |
| 2026-10-16 --:-- | 설치 | 2993500314 | 동적 머리카락 미화 |
| 2026-10-16 --:-- | 설치 | 3001496635 | [软甜废] 이목구비·머리 장식 동적 입화 |
| 2026-10-16 --:-- | 설치 | 3200319699 | [木头] AI 동적 입화 |
| 2026-10-16 --:-- | 설치 | 2815562168 | 원작 여성 인물 17명 동적 입화 교체 |
| 2026-10-16 --:-- | 설치 | 2814703549 | Fatury 틀 |
| 2026-10-15 --:-- | 받음 | 2945768219 | [执余] 여성 NPC 외모 평가 개선 |
| 2026-10-15 --:-- | 받음 | 2839261049 | 진·진종지보: 전승 심법 |
| 2026-10-15 --:-- | 받음 | 3390738990 | [只有梦长] 공법 다섯 권(오픈소스) |
| 2026-10-15 --:-- | 받음 | 2814806198 | 전환 화면 그림 교체 예제 모드 |
| 2026-10-15 --:-- | 받음 | 2832167498 | [含羞草] 얼굴 만들기 |
| 2026-10-15 --:-- | 받음 | 2929805068 | [大姨夫] 선의각·미화 |
| 2026-10-15 --:-- | 받음 | 2876769227 | [只有梦长] 특수 NPC 복장 해금 |
| 2026-10-15 --:-- | 받음 | 2874128983 | 인물 미화·복장 해금·매력과 가중치 수정 |
| 2026-10-15 --:-- | 받음 | 2945768367 | [执余] 미녀 대량 생성 |
| 2026-10-15 --:-- | 받음 | 2823930683 | 남성 NPC 외모 개선 |
| 2026-10-15 --:-- | 받음 | 2819712935 | 여성 NPC 외모 개선 |
| 2026-10-15 --:-- | 받음 | 2993500314 | 동적 머리카락 미화 |
| 2026-10-15 --:-- | 받음 | 3001496635 | [软甜废] 이목구비·머리 장식 동적 입화 |
| 2026-10-15 --:-- | 받음 | 3200319699 | [木头] AI 동적 입화 |
| 2026-10-15 --:-- | 받음 | 2815562168 | 원작 여성 인물 17명 동적 입화 교체 |
| 2026-10-15 --:-- | 받음 | 2814703549 | Fatury 틀 |

## 4. 컬렉션 비교

컬렉션 없음 (tools/workshop/collection.txt 에 컬렉션 ID를 한 줄 적으면 비교한다)

## 5. 후보표

- [candidates-20261015.md](../tools/workshop/candidates-20261015.md)
- [candidates-20261016.md](../tools/workshop/candidates-20261016.md)

## 6. 한글화

- 번역 파일 src/ModCode/ModMain/Translations/mods.txt: 59행. 모드별 상태는 한글화(K)에서 정한다.
