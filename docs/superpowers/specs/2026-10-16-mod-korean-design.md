# 창작마당 모드 한글화(G2c) 설계 (2026-10-16)

## 1. 목적

사용자 요청(2026-10-15, 요청 4의 "모드 한글화")의 마지막 묶음. 창작마당 모드가 더한 글(LocalText 행)이 게임 화면과 편집기에 "0"으로 보이는 것을 한국어로 보이게 한다.

사용자가 고른 것(2026-10-16):

- 지금 있는 모드 글은 모두 번역한다(제가 쓴다). 파일에 아직 없는 키(나중에 설치한 모드)는 중국어(`ch`)로 덮어 "0" 대신 읽히게 한다.
- 번역 파일은 DLL에 내장하고 처음 실행 때 모드 데이터 폴더에 풀어 둔다(기본 프리셋과 같은 방식). 사용자가 고칠 수 있다.

## 2. 실측 (회차 `ws-1016o`, 실험 브랜치 `feat/korean-research`의 `Dev/DevCommandsKorean.cs`. 인계 8.3e절의 `wssetkr`도)

| 항목 | 실측 |
|---|---|
| 모드 글의 양 | LocalText 55,267행 가운데 모드가 더한 행(`ConfBaseItem.isModExtend`)은 62개. 모두 `kr`이 "0"이거나 빈 글. `ch`가 빈 것 2개(키 `ZYT2klWX`, `3Inmec6G`), 영어 글 1개(`0lkK4v` → `exists`) |
| 내용 | 공법 모드 `3390738990`의 기술 이름 5·설명 5(`skill_name_middle<번호>`, `skill_attack_desc<번호>`), 옵션·효과 글 46(`s_<번호>_<꼬리>`, `s_<이름>`, `effect_desc…`, `skillCondition…`, `BattleEffectDesc…`), 외형 모드의 안내문 1(`utg7MubS` → "是否进行易容？") |
| 자리 표시 | 45행에 `$s_…$`(용어 키. 대부분 게임 기본 키라 이미 한국어로 풀린다: `$s_liuxue$` 유혈, `$s_zhuoshao$` 작열, `$s_jian$` 검법 등), `&번호_꼬리&`·`&a\|x100\|/300\|f0&`(수치), `{icon_…}`(아이콘), `<y>…</y>`·`<r>…</r>`(색), `\n`이 섞여 있다. 번역할 때 그대로 둔다 |
| 덮는 자리 | 실행 중 `g.conf.localText.allConfList`의 그 행 `kr`을 바꾸면 `GameTool.LS(키)`가 바로 바뀌고, 그 뒤 만든 비급 이름도 한글이 된다(`wssetkr`, 회차 `ws-1016f`) |
| 적용 시점 | `ModMain.Init`은 세이브 진입 때마다 불리고 그때 모드 표는 이미 로드돼 있다(`wsmods`가 `Init` 뒤에 16개를 본다) |
| 모드 자체의 이름 | `g.mod.modDatas[].name`은 LocalText 키가 아니라 글 그대로(서식 태그 포함). 게임 모드 화면에만 쓰인다 |
| 기본 프리셋의 방식 | `PresetService.Defaults()`가 어셈블리 리소스(`Presets/*.json`)를 읽고 `WriteDefaults()`가 `TlToybox\presets\`에 쓴다(폴더가 없을 때). 같은 틀을 쓴다 |

## 3. 결정

### 3.1 번역 파일

- 저장소 `src/ModCode/ModMain/Translations/mods.txt`. csproj에 `EmbeddedResource`(`LogicalName="Translations/mods.txt"`)로 넣는다.
- 모양: UTF-8(BOM 없음), 한 줄에 `키<TAB>한국어`. `#`으로 시작하는 줄과 빈 줄은 무시. 한국어 안의 줄바꿈은 `\n` 두 글자로 적는다(게임 글의 `\n`과 같다). 자리 표시는 원문 그대로. 같은 키가 두 번이면 뒤의 것.
- 실행 중 읽는 자리: 모드 데이터 폴더 `TlToybox\translations\mods.txt`(`ChangeLog.DataDir` 아래). 없으면 내장본을 풀어 쓴다(있으면 건드리지 않는다. 사용자가 고친 것을 지키려고). 내장본이 새로워져도 덮지 않는다 — 새 판을 받으려면 파일을 지운다(05 문서에 적는다).
- 내용은 부록 A의 59행(중국어가 빈 2개와 영어 `exists` 1개는 넣지 않는다).

### 3.2 적용 (`TranslationService`)

- `ModMain.Init`에서 `EditorHost.Ensure()` 뒤에 `TranslationService.Apply()`를 부른다. 매번 불려도 된다(같은 값을 다시 쓴다).
- `Apply()`: 파일을 읽어 사전(키 → 한국어)을 만든다. `g.conf.localText.allConfList`를 한 번 돌며 `isModExtend` 행마다: 사전에 키가 있으면 `kr`을 그 글로(앞뒤 공백을 지우고 `\n`을 줄바꿈으로); 없고 `kr`이 "0"이거나 빈 글이고 `ch`가 비어 있지 않으면 `kr = ch`. 기본 행(`isModExtend`가 거짓)은 건드리지 않는다(사전에 그 키가 있어도).
- 끝나면 `SkillCatalog.Reset()`(편집기 목록이 새 이름을 쓰게). `Latest.log`에 한 줄: `[TlToybox] translations: file=<있음/풀어 둠/없음> rows=<사전 수> applied=<번역으로 덮은 수> fallback=<ch로 덮은 수> unused=<사전에 있으나 표에 없는 키 수> broken=<탭 없는 줄 수>`.
- 파일을 읽지 못하면(예외) 번역 없이 `ch` 대체만 하고 로그에 이유를 적는다. 세이브는 건드리지 않는다(설정 표만 바꾼다).

### 3.3 새 키 찾기 (개발 명령, `DevCommandsResources`가 아니라 새 `DevCommandsTranslation`)

- `rloc` : `Apply()`의 마지막 결과 문장(`file=… rows=… applied=… fallback=… unused=… broken=…`).
- `rloc dump` : 사전에 없는 모드 행(`ch`가 비어 있지 않은 것)을 `TlToybox\translations\untranslated.txt`에 `키<TAB><TAB>중국어`로 쓴다(한국어 칸 비움). `written=<수>`. 다음 세션이 이 파일을 번역해 `mods.txt`(저장소와 데이터 폴더 둘 다)에 합친다. 절차를 인계 문서에 적는다.
- `rloc key <키>` : `ls=<GameTool.LS(키)> kr=<행의 kr> ch=<행의 ch>`(검증용).

### 3.4 다른 곳에 주는 영향

- `SkillCatalog.ResolveText`는 `LS`가 한국어를 돌려주므로 모드 항목의 `NameFromCh`가 거짓이 되고 아래 칸의 "한글 이름이 없어…" 안내가 사라진다. 코드 변경 없음.
- `modbooks` 검사의 "the Chinese name is searchable"(검색 `烈炎`)은 한글 이름이 되면 `Search`에 `ch`도 들어 있어 그대로 통과한다. 한글 검색(`열염`) 검사를 하나 더한다.
- 모드 자체의 이름(모드 화면)은 바꾸지 않는다(범위 밖).

### 3.5 요소 이름

화면 변경이 없다. 새 요소 없음.

## 4. 코드

| 파일 | 일 |
|---|---|
| `src/ModCode/ModMain/Translations/mods.txt` | 새 파일. 부록 A |
| `src/ModCode/ModMain/ModMain.csproj` | `EmbeddedResource Include="Translations\*.txt" LogicalName="Translations/%(Filename)%(Extension)"` |
| `src/ModCode/ModMain/Services/TranslationService.cs` | 새 파일. `Dir`(`TlToybox\translations`), `FilePath`, `EnsureFile()`(없으면 내장본 풀기), `Load()`(사전, 깨진 줄 수), `Apply()`(3.2), `LastSummary`, `DumpUntranslated()`(3.3) |
| `src/ModCode/ModMain/ModMain.cs` | `Init`에서 `TranslationService.Apply()` |
| `src/ModCode/ModMain/Dev/DevCommandsTranslation.cs`, `Dev/DevCommands.cs` | `rloc`, `rloc dump`, `rloc key` |
| `tools/DevRemote/session.ps1`, `session.tests.ps1` | `-Begin`이 `translations` 폴더를 보관하고 `-End`가 되돌린다(프리셋 폴더와 같은 방식. 검사가 파일을 지우거나 바꾸므로). 시험 둘 |
| `tools/research/ui-check.ps1` | 부분 `translation`; `modbooks`에 한글 검색 한 줄 |
| `docs/05-editor-v0.1.md` 43절, `docs/06-handoff.md` | 6절 |

실험 브랜치 `feat/korean-research`의 `wsloc`·`wslocdump`는 가져오지 않는다(`rloc dump`가 대신한다).

## 5. 검증

게임 없이: `session.tests.ps1` 둘(translations 폴더 보관·복원, 없었으면 없앰).

회차 `ws-1016p`부터(고정본. 모드 15개가 로드된다):

- `rloc` → `file=풀어 둠`(첫 회차. 회차 스크립트가 폴더를 치웠으므로) 또는 `있음`, `rows=59 applied=59 fallback=0 unused=0 broken=0`. `fallback`은 중국어가 빈 2개와 `exists`를 세지 않는다(`ch`가 비었거나 사전에 없고 kr이 "0"인 것 — `exists`는 `fallback=1`이 된다. 기대값 `fallback=1`).
- `rloc key skill_name_middle984555698` → `ls=열염장`. `rloc key s_shenqiang` → `ls=신창`.
- `rmods` → 이름이 한글(열염장·장기광도·풍선용권·적염검·천살신창), `NameFromCh=False`.
- `rbook 1 984555698 3 1 1` → 결과 문장의 이름이 `<옵션 접두>열염장`. `rbooks 984555698`의 이름도.
- 기술 서적 그룹: `ResKind6` → `BookName984555698`의 글이 `열염장`, 아래 칸에 "한글 이름이 없어" 안내가 없다, 검색 `열염` 1줄, 검색 `烈炎` 1줄.
- `-Restart` 뒤 `rloc`가 `file=있음`이고 이름이 그대로 한글.
- 파일을 지우고 `-Restart` → `file=풀어 둠`, 다시 60행. 파일에 깨진 줄과 모르는 키를 넣고 `-Restart` → `broken=1 unused=1`, 나머지는 적용. 파일의 한 줄을 바꿔(`skill_name_middle984555698	열염장2`) `-Restart` → `ls=열염장2`(사용자 수정이 먹는다).
- `rloc dump` → 59행이 모두 사전에 있으면 `written=1`(`exists`만. 또는 사전에 넣고 0). 파일 `untranslated.txt`의 모양.
- 기존 `modbooks`·`books-ui`·`base`·`tabs` 통과. 두 빌드 구성 경고 0·오류 0.

## 6. 문서

- 05 문서 43절 "창작마당 모드 한글화(G2c)": 규칙, 파일 모양, 새 키 절차, 검증, 확인하지 못한 것.
- 인계: 1절 상태(G2 끝남), 5절 도구에 `rloc`과 `translation` 검사, `-End`가 되돌리는 것에 `translations`, 8.3b절 "G2 모두 끝남", 8.5절 확인 대기(게임 화면의 기술 이름·설명이 한글로 보이는지, 외형 모드의 안내문), 8.7절 회차 이름.
- CLAUDE.md: 변경 없음.

## 7. 범위 밖

- 모드 자체의 이름·설명(모드 화면), 게임 본체 문자열, 자동 번역.
- 모드마다 파일을 나누는 것(키가 전역으로 유일하므로 한 파일).
- 번역 파일의 편집기 화면.

## 8. 확인하지 못한 것, 위험

- 번역은 제가 중국어를 읽고 썼다. 게임 안 용어(유혈·작열·내상 등)는 게임 표기를 따랐지만 문장은 사용자가 보고 고칠 수 있다(데이터 폴더의 파일).
- 게임 화면(기술 창·비급 설명)에서 자리 표시가 한국어 문장 안에서 제대로 풀리는지는 회차에서 비급 이름까지만 본다. 설명 전체는 사용자 확인 항목.
- `Apply()`가 `Init`마다 55,267행을 한 번 도는 비용(한 번 훑기. `rmods`의 LocalText 훑기가 3초 안쪽이었다).
- 내장본이 새로워져도 데이터 폴더의 파일을 덮지 않으므로, 번역을 고친 새 판을 배포하면 사용자가 파일을 지워야 한다.
- `exists`(`0lkK4v`)는 어떤 모드의 플래그로 보인다. 번역하지 않는다.

## 부록 A. 번역표 (59행. `mods.txt`의 내용)

`\n`은 그대로 두 글자다. `$…$`·`&…&`·`{…}`·`<…>`는 원문 그대로.

| 키 | 중국어 | 한국어 |
|---|---|---|
| utg7MubS | 是否进行易容？ | 용모를 바꾸시겠습니까? |
| skill_name_middle59898952 | 千杀神枪 | 천살신창 |
| skill_attack_desc59898952 | 进入$s_shenqiang$状态后即可施展此神通。持续召唤大量神枪布下千杀枪阵，神枪将追踪攻击场上所有敌人，每道神枪对碰撞到的敌人造成威力&59898952_dmg&的$s_qiang$伤害，并刻下$s_qiansha$标记。 | $s_shenqiang$ 상태에 들어간 뒤에 이 신통을 펼칠 수 있다. 대량의 신창을 계속 소환해 천살창진을 펼치며, 신창은 전장의 모든 적을 추적해 공격하고 부딪힌 적에게 위력 &59898952_dmg&의 $s_qiang$ 피해를 주며 $s_qiansha$ 표식을 새긴다. |
| s_shenqiang | 神枪 | 신창 |
| effect_descshenqiang | 场上只有一个敌人，或战斗开始&59898952_qzsj&秒后\n进入神枪状态,必定化解受到的武法伤害 | 전장에 적이 하나뿐이거나 전투 시작 &59898952_qzsj&초 뒤\n신창 상태에 들어가며, 받는 무법 피해를 반드시 상쇄한다 |
| s_59898952_mpCost | 此技能的灵力消耗&59898952_mpCost&点。 | 이 기술의 영력 소모 &59898952_mpCost&점. |
| s_59898952_cd | 此技能的冷却时间&59898952_cd&秒。 | 이 기술의 쿨타임 &59898952_cd&초. |
| s_664997436_tssh | 施展此神通时，若场上只有一个敌人，则此技能造成的伤害+&664997436_tssh&%。 | 이 신통을 펼칠 때 전장에 적이 하나뿐이면 이 기술의 피해 +&664997436_tssh&%. |
| s_68675244_qsj | 此技能的持续时间&68675244_qsj&秒。 | 이 기술의 지속 시간 &68675244_qsj&초. |
| s_265874779_ysjd | 受到此技能伤害的敌人，其移动速度&265874779_ysjd&%，持续&265874779_jssj&秒。 | 이 기술의 피해를 받은 적은 이동속도 &265874779_ysjd&%, &265874779_jssj&초 지속. |
| s_457824468_msbl | 在此神通的持续时间内，自身受到的伤害&457824468_msbl&%。 | 이 신통의 지속 시간 동안 자신이 받는 피해 &457824468_msbl&%. |
| skillCondition445657899 | 需求神枪层数： | 필요 신창 중첩 수: |
| effect_desc665988763qztj | 封 | 봉 |
| s_qiansha | 千杀 | 천살 |
| effect_descqiansha | 标记叠加{icon_diejiacengshu}<y>10</y>层将触发千杀效果，对被标记者造成其最大生命值{icon_zhijieshanghai}<y>1</y>%的真实伤害，并清除标记层数 | 표식이 {icon_diejiacengshu}<y>10</y>층 쌓이면 천살 효과가 발동해, 표식된 대상에게 최대 생명의 {icon_zhijieshanghai}<y>1</y>% 고정 피해를 주고 표식 중첩을 지운다 |
| skill_name_middle984555698 | 烈炎掌 | 열염장 |
| skill_attack_desc984555698 | $s_xueli984555698$ {icon_chixushijian}<y>1.5</y>秒，向前方发射&984555698_ddsl&道射程&984555698_range&的烈炎掌，对碰撞到的敌人造成威力&984555698_dmg&的$s_zhang$伤害。在蓄力达到最大时间后施放，可达成完美蓄力，将额外释放{icon_dandaoshuliang}<y>2</y>次$s_huo$的烈炎掌。 | $s_xueli984555698$ {icon_chixushijian}<y>1.5</y>초, 전방에 사거리 &984555698_range&의 열염장을 &984555698_ddsl&갈래 발사해 부딪힌 적에게 위력 &984555698_dmg&의 $s_zhang$ 피해를 준다. 충전이 최대 시간에 이른 뒤 펼치면 완전 충전이 되어 $s_huo$의 열염장을 {icon_dandaoshuliang}<y>2</y>번 더 발사한다. |
| s_984555698_mpCost | 此技能的灵力消耗&984555698_mpCost&点。 | 이 기술의 영력 소모 &984555698_mpCost&점. |
| s_984555698_cd | 此技能的冷却时间&984555698_cd&秒。 | 이 기술의 쿨타임 &984555698_cd&초. |
| s_984555698_neishang | 若目标带有$s_neishang$，则引爆目标身上的所有$s_neishang$，每引爆一层$s_neishang$有&984555698_zfgl&%的概率在目标的脚下生成一道$s_zhangfeng$。 | 대상에게 $s_neishang$이 있으면 대상의 모든 $s_neishang$을 터뜨리며, $s_neishang$ 한 층을 터뜨릴 때마다 &984555698_zfgl&%의 확률로 대상의 발밑에 $s_zhangfeng$을 하나 만든다. |
| s_984555698_zhengfeng2 | 若自身带有$s_neijing$，则消耗自身的所有$s_neijing$，每消耗一层$s_neijing$有&984555698_zfgl2&%的概率在自身脚下生成一道$s_zhangfeng$。 | 자신에게 $s_neijing$이 있으면 자신의 모든 $s_neijing$을 소모하며, $s_neijing$ 한 층을 소모할 때마다 &984555698_zfgl2&%의 확률로 자신의 발밑에 $s_zhangfeng$을 하나 만든다. |
| s_984555698_zsyb | 若目标带有$s_zhuoshao$，则引爆目标身上的所有$s_zhuoshao$，每引爆一层$s_zhuoshao$有&355557521_hygl&%的概率在目标的脚下生成一道$s_lieyan$。 | 대상에게 $s_zhuoshao$이 있으면 대상의 모든 $s_zhuoshao$을 터뜨리며, $s_zhuoshao$ 한 층을 터뜨릴 때마다 &355557521_hygl&%의 확률로 대상의 발밑에 $s_lieyan$을 하나 만든다. |
| s_984555698_xhrh | 若自身带有$s_ranhun$，则消耗自身的所有$s_ranhun$，每消耗一层$s_ranhun$有&355557521_hygl2&%的概率在自身脚下生成一道$s_lieyan$。 | 자신에게 $s_ranhun$이 있으면 자신의 모든 $s_ranhun$을 소모하며, $s_ranhun$ 한 층을 소모할 때마다 &355557521_hygl2&%의 확률로 자신의 발밑에 $s_lieyan$을 하나 만든다. |
| s_984555698_ddzj | 蓄力期间持续增加烈炎掌弹道数量，至多增加&984555698_zjdd&次。 | 충전하는 동안 열염장의 탄도 수가 계속 늘며, 최대 &984555698_zjdd&번 는다. |
| s_984555698_ctcs | 烈炎掌的穿透次数&984555698_ctcs&。 | 열염장의 관통 횟수 &984555698_ctcs&. |
| s_xueli984555698 | 蓄力施法 | 충전 시전 |
| s_xueli984555698_desc | 按住鼠标即可开始蓄力，松开鼠标即可停止蓄力并施放技能，蓄力时间越长，技能的威力越高、射程（范围）越大。 | 마우스를 누르고 있으면 충전을 시작하고, 떼면 충전을 멈추고 기술을 펼친다. 충전 시간이 길수록 기술의 위력이 높아지고 사거리(범위)가 커진다. |
| skill_name_middle353535 | 藏气狂刀 | 장기광도 |
| skill_attack_desc353535 | 向前突进&353535_tjjl&距离，同时快速斩出三道可瞬间造成&353535_ddsl\|x100\|/300\|f0&次伤害的刀光，对范围&353535_range&内的敌人造成威力&353535_dmg&的$s_dao$伤害。\n此技能暗藏$s_daoqi35226635$，将造成额外伤害，斩击结束后提升自身{icon_yisu}<y>45</y>%移速，持续{icon_chixushijian}<y>3</y>秒。 | 전방으로 &353535_tjjl& 거리를 돌진하며 동시에 순간적으로 &353535_ddsl\|x100\|/300\|f0&번 피해를 줄 수 있는 도광 세 줄기를 빠르게 베어 내, 범위 &353535_range& 안의 적에게 위력 &353535_dmg&의 $s_dao$ 피해를 준다.\n이 기술에는 $s_daoqi35226635$가 숨어 있어 추가 피해를 주며, 베기가 끝나면 자신의 이동속도가 {icon_yisu}<y>45</y>% 올라 {icon_chixushijian}<y>3</y>초 지속된다. |
| s_353535_ybxr | 引爆目标身上的$s_xueren$，每引爆一层，在目标位置落下一道血斩，对范围&22111_xzfw&内的敌人造成&22111_xzsh\|x22111_dmg\|/100\|f0&$s_dao$伤害。血斩造成的伤害，可吸血&22111_xzxx&%。 | 대상의 $s_xueren$을 터뜨리며, 한 층을 터뜨릴 때마다 대상의 자리에 혈참을 한 줄기 떨어뜨려 범위 &22111_xzfw& 안의 적에게 &22111_xzsh\|x22111_dmg\|/100\|f0& $s_dao$ 피해를 준다. 혈참이 준 피해의 &22111_xzxx&%를 흡혈한다. |
| s_353535_xhjq | 使用藏气狂刀消耗自身所有$s_jueqing$，每消耗一层$s_jueqing$，可增加藏气狂刀攻击距离&353535_mxbl&%，持续1秒 | 장기광도를 쓰면 자신의 모든 $s_jueqing$을 소모하며, $s_jueqing$ 한 층을 소모할 때마다 장기광도의 공격 거리가 &353535_mxbl&% 늘어 1초 지속된다 |
| s_353535_mpCost | 此技能的灵力消耗&353535_mpCost&点。 | 이 기술의 영력 소모 &353535_mpCost&점. |
| s_353535_cd | 此技能的冷却时间&353535_cd&秒。 | 이 기술의 쿨타임 &353535_cd&초. |
| s_353535_range | 刀光的范围&353535_range&。 | 도광의 범위 &353535_range&. |
| s_353535_zjcd | 若使用此技能引爆了&353535_xqcs&层$s_xueren$，则武技的冷却时间&353535_zjcd&%，持续&22113_zjsj&秒。 | 이 기술로 $s_xueren$을 &353535_xqcs&층 터뜨리면 무예의 쿨타임 &353535_zjcd&%, &22113_zjsj&초 지속. |
| BattleEffectDesc35353584 | 移速{icon_yisu}提升<y>25%</y> | 이동속도{icon_yisu} <y>25%</y> 상승 |
| s_daoqi35226635 | 刀气 | 도기 |
| s_daoqi35226635_desc | 暗藏的刀气造成威力&353535_dmg&的$s_dao$伤害 | 숨은 도기가 위력 &353535_dmg&의 $s_dao$ 피해를 준다 |
| skill_name_middle3535351 | 赤炎剑 | 적염검 |
| skill_attack_desc3535351 | 向前发射一道射程&3535351_range&的玄灵剑，对敌人造成威力&3535351_dmg&的$s_jian$伤害。同时催动$s_huolingli353535$，使玄灵剑转化为赤炎剑追踪敌人，命中敌人造成威力&3535352_dmg&的$s_huo$伤害,并且有概率碎裂剑体形成<r>赤炎爆</r>，额外造成一次伤害。 | 전방에 사거리 &3535351_range&의 현령검을 한 줄기 발사해 적에게 위력 &3535351_dmg&의 $s_jian$ 피해를 준다. 동시에 $s_huolingli353535$을 일으켜 현령검을 적염검으로 바꿔 적을 추적하게 하며, 적에게 맞으면 위력 &3535352_dmg&의 $s_huo$ 피해를 주고 확률적으로 검이 부서져 <r>적염폭</r>이 되어 피해를 한 번 더 준다. |
| s_huolingli353535 | 火灵力 | 화령력 |
| s_3535352_buffProb%1 | 玄灵剑命中敌人，有&14111_buffProb%1&%的概率为其叠加一层$s_liuxue$。\n赤炎剑命中敌人，有&41111_buffProb%1&%的概率为其叠加一层$s_zhuoshao$。 | 현령검이 적에게 맞으면 &14111_buffProb%1&%의 확률로 적에게 $s_liuxue$을 한 층 쌓는다.\n적염검이 적에게 맞으면 &41111_buffProb%1&%의 확률로 적에게 $s_zhuoshao$을 한 층 쌓는다. |
| s_3535352_buffProb%2 | 玄灵剑命中，有&14112_buffProb%1&%的概率为自身叠加一层$s_jianzhan$。\n赤炎剑命中，有&41112_buffProb%1&%的概率为自身叠加一层$s_ranhun$。 | 현령검이 맞으면 &14112_buffProb%1&%의 확률로 자신에게 $s_jianzhan$을 한 층 쌓는다.\n적염검이 맞으면 &41112_buffProb%1&%의 확률로 자신에게 $s_ranhun$을 한 층 쌓는다. |
| s_3535351_cd | 此技能的冷却时间&3535351_cd&秒。 | 이 기술의 쿨타임 &3535351_cd&초. |
| s_3535351_layCount | $s_liuxue$与$s_zhuoshao$的最多叠加层数&14111_layCount&层。 | $s_liuxue$과 $s_zhuoshao$의 최대 중첩 수 &14111_layCount&층. |
| s_3535351_layCount2 | $s_jianzhan$与$s_ranhun$的最多叠加层数&14112_layCount&层。 | $s_jianzhan$과 $s_ranhun$의 최대 중첩 수 &14112_layCount&층. |
| s_3535351_bszj | 此技能造成的暴击伤害，增加&3535351_bszj&% | 이 기술의 치명타 피해가 &3535351_bszj&% 는다 |
| s_3535351_range | 玄灵剑的射程&3535351_range&。 | 현령검의 사거리 &3535351_range&. |
| s_3535351_duration | $s_liuxue$与$s_zhuoshao$的持续时间&14111_duration&秒。 | $s_liuxue$과 $s_zhuoshao$의 지속 시간 &14111_duration&초. |
| s_3535351_duration2 | $s_jianzhan$与$s_ranhun$的持续时间&14112_duration&秒。 | $s_jianzhan$과 $s_ranhun$의 지속 시간 &14112_duration&초. |
| skill_name_middle844854565 | 风旋龙卷 | 풍선용권 |
| skill_attack_desc844854565 | $s_chixushifa$ &844854565_sfsj&秒，连续向目标方向召唤风旋龙卷，对敌人造成威力&844854565_dmg&的$s_feng$伤害。 | $s_chixushifa$ &844854565_sfsj&초, 대상 방향으로 풍선용권을 잇달아 소환해 적에게 위력 &844854565_dmg&의 $s_feng$ 피해를 준다. |
| s_844854565_mpCost | 此技能的灵力消耗&844854565_mpCost&点。 | 이 기술의 영력 소모 &844854565_mpCost&점. |
| s_844854565_cd | 此技能的冷却时间&844854565_cd&秒。 | 이 기술의 쿨타임 &844854565_cd&초. |
| s_844854565_sfsj | 此技能施法时长增加&844854565_sfsj&秒。 | 이 기술의 시전 시간이 &844854565_sfsj&초 는다. |
| s_844854565_yszj | 若自身带有$s_lingfeng$，则消耗自身所有$s_lingfeng$，每消耗一层$s_lingfeng$，增加&844854565_yszj&点移速，之后每移动&844854565_ydjl&，即产生一道向前移动的旋风。施法结束将移除提升移速与产生旋风的效果。 | 자신에게 $s_lingfeng$이 있으면 자신의 모든 $s_lingfeng$을 소모하며, $s_lingfeng$ 한 층을 소모할 때마다 이동속도가 &844854565_yszj&점 늘고, 그 뒤 &844854565_ydjl&만큼 움직일 때마다 앞으로 나아가는 회오리를 하나 만든다. 시전이 끝나면 이동속도 상승과 회오리 생성 효과가 사라진다. |
| s_844854565_ysjd | 若目标带有$s_nifeng$，则引爆其所有$s_nifeng$，每引爆一层$s_nifeng$可对其额外造成一次威力&844854565_ewsh&的$s_feng$伤害。\n引爆$s_nifeng$时可使目标的移动速度&844854565_ysjd&%，持续&844854565_jssj&秒。 | 대상에게 $s_nifeng$이 있으면 그 모든 $s_nifeng$을 터뜨리며, $s_nifeng$ 한 층을 터뜨릴 때마다 대상에게 위력 &844854565_ewsh&의 $s_feng$ 피해를 한 번 더 준다.\n$s_nifeng$을 터뜨릴 때 대상의 이동속도 &844854565_ysjd&%, &844854565_jssj&초 지속. |
| s_844854565_sbcs | 此技能施法期间，闪避前&844854565_sbcs&次受到的伤害。 | 이 기술을 시전하는 동안 처음 &844854565_sbcs&번 받는 피해를 회피한다. |
| s_844854565_jdsh | 此技能命中敌人时，可使目标在接下来的&844854565_jdsj&秒内，造成的伤害&844854565_jdsh&%。 | 이 기술이 적에게 맞으면 대상이 이어지는 &844854565_jdsj&초 동안 주는 피해 &844854565_jdsh&%. |
