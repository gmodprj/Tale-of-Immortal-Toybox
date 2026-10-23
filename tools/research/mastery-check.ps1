# 수련 단계 대도까지 채우기(스펙 docs/superpowers/specs/2026-10-23-mastery-fill-design.md, 05 문서 83절)의 게임 내 검증.
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base -Exclude 2814696167로 게임이 떠서 세계에 들어가 있어야 한다.
# 고정본 플레이어: 현화술 tIjlr1(옵션 0), 역 궁 청풍검 fY5GR9(장착, 옵션 7, 숙련 3570), 한공결 Grf9Gt(심법 장착, 4), 건강약 cR6Nyn(신법 3), 강 궁 노혈참 4Dc8Cu(절기 6).
#   -Part service   서비스(mf*): 끼워 넣기·다시 만들기·후보·거부·자리 밖 옛 옵션·만들기 실패·NPC.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

function LogHas([string]$word) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 40 -Encoding UTF8 | Where-Object { $_ -match $word }).Count -gt 0 }
function St([string]$sole, [string]$who = 'me') { Cmd "mfstate $sole $who" 300 }
function Vals([string]$sole, [string]$who = 'me') { Cmd "mfvals $sole $who" 300 }
function Count { if ((Cmd 'pstate' 300) -match 'martials=(\d+)') { [int]$Matches[1] } else { -1 } }

switch ($Part) {
  'service' {
    Cmd 'mfstate tIjlr1' 300 | Out-Null   # 예열: 회차의 첫 명령은 낡은 명령 파일을 치우느라 빈 답일 수 있다
    $traps = Traps
    # 1) 옵션 0개 기술에 끼워 넣기
    Check 'a no-option skill starts at stage 1' ((St 'tIjlr1') -match 'opts=0 stages=1 ') (St 'tIjlr1')
    $r = Cmd 'mfinsert tIjlr1 roll' 1500
    Check 'insert fills a no-option skill to 11 and the dao stage' ((St 'tIjlr1') -match 'opts=11 stages=12 ') "$r | $(St 'tIjlr1')"
    $v = Vals 'tIjlr1'
    $lowNew = @([regex]::Matches($v, '(\d+):\d+:(\d+)/(\d+)') | Where-Object { $_.Groups[2].Value -ne $_.Groups[3].Value })
    Check 'new options are at max quality' ($lowNew.Count -eq 0) $v
    # 2) 장착한 7개 무예: 같은 sole·장착·숙련, 옛 수치 그대로
    $before = Vals 'fY5GR9'
    $oldRaws = ([regex]::Matches($before, '(\d):(\d+):\d+/\d+') | Select-Object -First 7 | ForEach-Object { $_.Value -replace ':\d+/\d+$','' }) -join ' '
    $r = Cmd 'mfinsert fY5GR9 roll' 1500
    $after = Vals 'fY5GR9'
    Check 'insert keeps the sole, the slot and the exp' ((St 'fY5GR9') -match 'opts=11 stages=12 ' -and $after -match 'equip=1 exp=3570 ') "$r | $after"
    $newRaws = ([regex]::Matches($after, '(\d):(\d+):\d+/\d+') | Select-Object -First 7 | ForEach-Object { $_.Value -replace ':\d+/\d+$','' }) -join ' '
    Check 'insert keeps the old option values' ($oldRaws -eq $newRaws -and $oldRaws.Length -gt 0) "$oldRaws | $newRaws"
    Check 'a full skill is refused' ((Cmd 'mfinsert fY5GR9 1' 500) -match '11개' -and (St 'fY5GR9') -match 'block=이미') (St 'fY5GR9')
    # 3) 후보는 그 자리 가중치 > 0인 것만
    $c = Cmd 'mfcand cR6Nyn -' 500
    $zero = @([regex]::Matches($c, '(\d+):(-?\d+)') | Where-Object { [int]$_.Groups[2].Value -le 0 })
    Check 'candidates all have a weight at the slot' ($c -match 'slot=3 \d' -and $zero.Count -eq 0) $c
    # 4) 장착한 심법을 전승과 함께 다시 만들기
    Cmd 'msuit Grf9Gt 10025' 800 | Out-Null
    $n0 = Count
    $before = Vals 'Grf9Gt'
    $r = Cmd 'mfremake Grf9Gt roll' 3000
    $new = if ($r -match 'new=(\w{6})') { $Matches[1] } else { '' }
    Check 'remake makes a new sole and removes the old one' ($new -ne '' -and (Vals 'Grf9Gt') -match 'no martial' -and (Count) -eq $n0) "$r | n0=$n0 now=$(Count)"
    $nv = Vals $new
    Check 'remake keeps the slot' ($nv -match 'equip=1 ') $nv
    Check 'remake keeps the exp and the suit' ($nv -match 'exp=50 suit=10025 ' -and (Cmd "msuitstate $new" 300) -match 'isSuit=true') "$nv | $(Cmd "msuitstate $new" 300)"
    $oldRaws = ([regex]::Matches($before, '(\d):(\d+):\d+/\d+') | Select-Object -First 4 | ForEach-Object { $_.Value -replace ':\d+/\d+$','' }) -join ' '
    $newRaws = ([regex]::Matches($nv, '(\d):(\d+):\d+/\d+') | Select-Object -First 4 | ForEach-Object { $_.Value -replace ':\d+/\d+$','' }) -join ' '
    Check 'remake keeps the old option values' ($oldRaws -eq $newRaws) "$oldRaws | $newRaws"
    Check 'remake reaches the dao stage' ((St $new) -match 'opts=11 stages=12 ') (St $new)
    # 5) 만들기 실패 흉내: 옛 것 그대로
    $n0 = Count
    Cmd 'mffailnext' 200 | Out-Null
    $r = Cmd 'mfremake cR6Nyn roll' 2000
    Check 'remake keeps the old one when make fails' ($r -match '그대로' -and (St 'cR6Nyn') -match 'opts=3 ' -and (Count) -eq $n0) "$r | $(St 'cR6Nyn')"
    # 6) 자리 밖 옛 옵션(옵션 교체 강제)도 인정
    $slots = Cmd 'tb2slot 4Dc8Cu 0' 500
    $have = if ((Cmd 'tb2mlist' 1000) -match '(?m)4Dc8Cu \d+ \w+ (.*)$') { @([regex]::Matches($Matches[1], '\d+:(\d+):\d+') | ForEach-Object { $_.Groups[1].Value }) } else { @() }
    # 자리 0의 가중치가 0이고, 지금 옵션에 없고, 선행·배타 조건이 없는 번호
    $out = @([regex]::Matches($slots, '(\d+):w0:m(\d+):r(\S*)') | Where-Object { $have -notcontains $_.Groups[1].Value -and ($_.Groups[3].Value -eq '' -or $_.Groups[3].Value -eq '0') })[0]
    if ($out) {
      $r = Cmd "tb2force 4Dc8Cu 0 $($out.Groups[1].Value)" 800
      $s = St '4Dc8Cu'
      Check 'forced old option is accepted' ($s -match 'block=-') "$r | $s"
      $r = Cmd 'mfinsert 4Dc8Cu roll' 1500
      Check 'insert works after a forced option' ((St '4Dc8Cu') -match 'opts=(\d+) ' -and [int]$Matches[1] -gt 6) "$r | $(St '4Dc8Cu')"
    } else {
      "INFO no out-of-slot option at slot 0 for 4Dc8Cu: $slots"
    }
    # 7) NPC 대상
    $npc = if ((Cmd 'trnpc any' 300) -match 'target=(\S+)') { $Matches[1] } else { '' }
    Cmd 'trnpc me' 200 | Out-Null
    $mine = Cmd 'pstate' 500
    $nsole = ''
    foreach ($m in [regex]::Matches((Cmd "msole 0 $npc" 300), '(\w{6}):\d+')) { if ((St $m.Groups[1].Value $npc) -match 'block=-') { $nsole = $m.Groups[1].Value; break } }
    if ($nsole) {
      $r = Cmd "mfinsert $nsole roll $npc" 1500
      Check 'npc target: the npc ability is filled' ((St $nsole $npc) -match 'stages=12 ' -or (St $nsole $npc) -match 'block=') "$r | $(St $nsole $npc)"
      Check 'npc target: the player is unchanged' ((Cmd 'pstate' 500) -eq $mine)
    } else { "INFO npc $npc has no fillable ability" }
    Check 'the change log has mastery lines' (LogHas '수련 단계')
    Check 'no callback exception in the log' ((Traps) -eq $traps)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
