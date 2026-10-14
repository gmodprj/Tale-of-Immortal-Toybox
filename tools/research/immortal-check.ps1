# 선법 탭의 게임 내 검증(docs/superpowers/specs/2026-10-14-immortal-tab-design.md 6절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

function Fields([string]$text) {
  $body = ($text -replace '^[^>]*-> ', '') -replace ' \| .*$', ''
  $map = @{}
  foreach ($pair in $body -split ' ') { if ($pair -match '^([^=]+)=(.*)$') { $map[$Matches[1]] = $Matches[2] } }
  $map
}
# 설정 표(게임 폴더의 읽기 전용 JSON)에서 센 카드 수: 선인마다 주동(0이 아니면)과 피동, 같은 번호는 한 번, 표에 있는 것만.
function ExpectedCount {
  $dir = 'E:\Games\Tale of Immortal\Mod\modFQA\配置修改教程\配置（只读）Json格式'
  $active = @{}; foreach ($r in (Get-Content -LiteralPath (Join-Path $dir 'ImmortalMethod.json') -Raw | ConvertFrom-Json)) { $active[[string]$r.id] = 1 }
  $passive = @{}; foreach ($r in (Get-Content -LiteralPath (Join-Path $dir 'PassiveMethod.json') -Raw | ConvertFrom-Json)) { $passive[[string]$r.passiveId] = 1 }
  $seen = @{}
  foreach ($h in (Get-Content -LiteralPath (Join-Path $dir 'ImmortalHuman.json') -Raw | ConvertFrom-Json)) {
    if ([string]$h.activeSkills -ne '0' -and $active.ContainsKey([string]$h.activeSkills)) { $seen["a$($h.activeSkills)"] = 1 }
    foreach ($p in ([string]$h.passiveSkills -split '\|')) { if ($p -ne '0' -and $passive.ContainsKey($p)) { $seen["p$p"] = 1 } }
  }
  $seen.Count
}

switch ($Part) {
  'catalog' {
    $all = Fields (Cmd 'icount 0 0' 300)
    Check 'the list has every card of the immortal table' ([int]$all.count -eq (ExpectedCount)) "count=$($all.count) want=$(ExpectedCount)"
    Check 'one immortal (1004) has six cards' ((Fields (Cmd 'icount 1004 0' 300)).count -eq '6') (Cmd 'icount 1004 0' 300)
    Check 'active only' ([int](Fields (Cmd 'icount 0 1' 300)).count -ge 7 -and [int](Fields (Cmd 'icount 0 1' 300)).count -le 10) (Cmd 'icount 0 1' 300)
    Check 'a name search finds the card' ((Fields (Cmd 'icount 0 0 천무절향' 300)).first -eq '5000') (Cmd 'icount 0 0 천무절향' 300)
    Check 'every entry has a name' ((Cmd 'icount 0 0 ?noname' 300) -match 'count=0 ')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'learn' {
    Check 'the fixture has no immortal card' ((Fields (Cmd 'ilist' 200)).owned -eq '0') (Cmd 'ilist' 200)
    $r = Cmd 'ilearn 5000' 800
    Check 'one card is learned' ((Cmd 'ihas 5000' 200) -match 'has=True' -and (Fields (Cmd 'ilist' 200)).owned -eq '1') $r
    $r = Cmd 'ilearn 5000' 800
    Check 'learning it again adds nothing' ((Fields (Cmd 'ilist' 200)).owned -eq '1') $r
    $r = Cmd 'ilearn 10010' 800
    Check 'a passive card is learned' ((Cmd 'ihas 10010' 200) -match 'has=True') $r
    Check 'the change log has the line' (@(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 5 -Encoding UTF8 | Where-Object { $_ -match '선법' }).Count -gt 0)
    Cmd 'save' 2500 | Out-Null
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'restart' {
    # learn(끝에서 저장한다) 뒤에 session.ps1 -Restart를 하고 돌린다.
    Check 'the learned cards are kept after a restart' ((Cmd 'ihas 5000' 200) -match 'has=True' -and (Cmd 'ihas 10010' 200) -match 'has=True') (Cmd 'ilist' 200)
  }
  'learn-all' {
    $before = [int](Fields (Cmd 'ilist' 200)).owned
    $r = Cmd 'ilearnall 1004 0' 2000
    # 앞 부분이 익혀 둔 카드는 건너뛴다. 익힘과 건너뜀의 합이 그 선인의 카드 수(6)이고, 익힌 만큼 늘어야 한다.
    $done = if ($r -match '익힘 (\d+), 건너뜀 (\d+)') { [int]$Matches[1] } else { -1 }
    $skip = if ($r -match '익힘 (\d+), 건너뜀 (\d+)') { [int]$Matches[2] } else { -1 }
    Check 'every card of the filtered list is learned' ($done + $skip -eq 6 -and [int](Fields (Cmd 'ilist' 200)).owned -eq $before + $done) $r
    Check 'cards of other immortals are not learned' ((Cmd 'ihas 6000' 200) -match 'has=False')
    $r = Cmd 'ilearnall 1004 0' 2000
    Check 'doing it again skips all' ($r -match '익힘 0, 건너뜀 6' -and [int](Fields (Cmd 'ilist' 200)).owned -eq $before + $done) $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
