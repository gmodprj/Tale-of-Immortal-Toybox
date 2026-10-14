# 신기 탭의 게임 내 검증(docs/superpowers/specs/2026-10-14-artifact-tab-design.md 6절).
# tools\DevRemote\session.ps1 -Begin <이름> -Fixture base로 게임이 떠서 세계에 들어가 있어야 한다.
# 고정본: 대경지 3(결정), 신기는 쌍어패(1)만, 쌍어옥패 점수 9, 호천안 요수·기술 없음, 호요 0.
param([Parameter(Mandatory)][string]$Part)
. (Join-Path $PSScriptRoot 'check-lib.ps1')

function Fields([string]$text) {
  $body = ($text -replace '^[^>]*-> ', '') -replace ' \| .*$', ''
  $map = @{}
  foreach ($pair in $body -split ' ') { if ($pair -match '^([^=]+)=(.*)$') { $map[$Matches[1]] = $Matches[2] } }
  $map
}
# 그 이름의 버튼이 꺼져 있는지(누르면 "not interactable").
function IsOff([string]$name) { $p = Pos $name; (Rc "click $($p.x) $($p.y)" 300) -match 'not interactable' }
function LogHas([string]$word) { @(Get-Content -LiteralPath (Join-Path $data 'changes.log') -Tail 20 -Encoding UTF8 | Where-Object { $_ -match $word }).Count -gt 0 }

switch ($Part) {
  'list' {
    Check 'the fixture has only the pisces pendant' ((Fields (Cmd 'aw' 200)).weapons -eq '1') (Cmd 'aw' 200)
    $r = Cmd 'aoff 1' 400
    Check 'the last artifact is not removed' ((Fields (Cmd 'aw' 200)).weapons -eq '1' -and $r -match '하나 이상') $r
    $r = Cmd 'aon 2' 400
    Check 'turning on the god eye adds 2' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2' -and $r -match '켰습니다') $r
    $r = Cmd 'aon 2' 400
    Check 'turning it on again adds nothing' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2' -and $r -match '이미') $r
    Cmd 'aon 3' 400 | Out-Null
    Check 'turning on the gourd adds 3' ((Fields (Cmd 'aw' 200)).weapons -eq '1,2,3') (Cmd 'aw' 200)
    $r = Cmd 'aoff 1' 400
    Check 'one of several can be turned off' ((Fields (Cmd 'aw' 200)).weapons -eq '2,3') $r
    $r = Cmd 'aoff 1' 400
    Check 'turning off a missing one says so' ($r -match '가지고 있지 않') $r
    Check 'the change log has the artifact line' (LogHas '신기 목록')
    $r = Cmd 'apoint 25' 400
    Check 'the point is set' ((Fields (Cmd 'aw' 200)).point -eq '25') $r
    $r = Cmd 'apoint -5' 400
    Check 'a negative point becomes 0' ((Fields (Cmd 'aw' 200)).point -eq '0') $r
    $r = Cmd 'apoint 123456' 400
    Check 'a point over the limit becomes 9999' ((Fields (Cmd 'aw' 200)).point -eq '9999') $r
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'godeye' {
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'geunlock 7010' 400
    Check 'without the god eye nothing is unlocked' ($r -match '호천안이 없습니다' -and (Fields (Cmd 'gestate' 200)).bosses -eq '0') $r
    Cmd 'awset 1,2' 300 | Out-Null
    $cat = Fields (Cmd 'gecat' 300)
    Check 'the catalog groups 95 skills under 42 bosses' ($cat.bosses -eq '42' -and $cat.skills -eq '95') (Cmd 'gecat' 300)
    Check 'the first boss has a translated name' ($cat.first -match '^\D' -and $cat.first -notmatch '^요수') (Cmd 'gecat' 300)
    Cmd 'gedmg 1' 300 | Out-Null
    Check 'damage makes it unusable' ((Fields (Cmd 'gestate' 200)).canUse -eq 'False') (Cmd 'gestate' 200)
    $r = Cmd 'gerepair' 400
    $s = Fields (Cmd 'gestate' 200)
    Check 'repair makes it usable at once' ($s.canUse -eq 'True' -and $s.damaged -eq 'False' -and $s.repairMonth -eq '0') "$r | $(Cmd 'gestate' 200)"
    $r = Cmd 'geequip 100721' 400
    Check 'an unlearned skill is not equipped' ((Fields (Cmd 'gestate' 200)).equip -eq '0') $r
    $r = Cmd 'geunlock 7010' 400
    $b = Fields (Cmd 'geboss 7010' 200)
    Check 'unlock writes the player major grade and learns the boss skills' ($b.grade -eq '3' -and $b.learned -eq '3/3') "$r | $(Cmd 'geboss 7010' 200)"
    $r = Cmd 'geunlock 7010' 400
    Check 'unlocking it again adds nothing' ((Fields (Cmd 'gestate' 200)).skills -eq '3' -and $r -match '이미') $r
    $r = Cmd 'geequip 100721' 400
    Check 'a learned skill is equipped' ((Fields (Cmd 'gestate' 200)).equip -eq '100721') $r
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'geunlockall' 400
    Check 'unlock all needs the god eye too' ((Fields (Cmd 'gestate' 200)).skills -eq '3') $r
    Cmd 'awset 1,2' 300 | Out-Null
    $r = Cmd 'geunlockall' 3000
    $s = Fields (Cmd 'gestate' 200)
    Check 'unlock all opens every boss and learns every skill' ($s.bosses -eq '42' -and $s.skills -eq '95' -and $r -match '요수 해금 41, 기술 92개 익힘, 건너뜀 1') "$r | $(Cmd 'gestate' 200)"
    $r = Cmd 'geunlockall' 3000
    Check 'unlock all again skips every boss' ($r -match '요수 해금 0, 기술 0개 익힘, 건너뜀 42' -and (Fields (Cmd 'gestate' 200)).skills -eq '95') $r
    Check 'the change log has the god eye line' (LogHas '호천안')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  'potmon' {
    Cmd 'awset 1' 300 | Out-Null
    $r = Cmd 'potadd 1001 3' 400
    Check 'without the gourd no potmon is added' ($r -match '연요호가 없습니다' -and (Fields (Cmd 'potlist' 200)).count -eq '0') $r
    Cmd 'awset 1,3' 300 | Out-Null
    $r = Cmd 'potadd 1001 3' 600
    $p = Fields (Cmd 'potlist' 200)
    Check 'a potmon is added at the chosen grade' ($p.count -eq '1' -and $p.grade -eq '3' -and $p.exp -eq '18000' -and $p.next -eq '30000') "$r | $(Cmd 'potlist' 200)"
    $r = Cmd "potfill $($p.first)" 600
    $q = Fields (Cmd 'potlist' 200)
    Check 'fill exp stops at the next grade' ($q.exp -eq '30000' -and $q.grade -eq '3') "$r | $(Cmd 'potlist' 200)"
    $r = Cmd "potfill $($p.first)" 600
    Check 'fill exp at the boundary is refused' ((Fields (Cmd 'potlist' 200)).exp -eq '30000' -and $r -match '승급') $r
    $r = Cmd 'potadd 1001 11' 400
    Check 'a grade out of range is refused' ((Fields (Cmd 'potlist' 200)).count -eq '1') $r
    for ($i = 0; $i -lt 27; $i++) { Cmd 'potadd 1002 1' 150 | Out-Null }
    Check 'potmons can be added up to 28' ((Fields (Cmd 'potlist' 200)).count -eq '28') (Cmd 'potlist' 200)
    $r = Cmd 'potadd 1002 1' 400
    Check 'the 29th potmon is refused' ((Fields (Cmd 'potlist' 200)).count -eq '28' -and $r -match '28') $r
    Cmd 'ddmg 1' 300 | Out-Null
    Check 'damage makes the gourd unusable' ((Fields (Cmd 'ddstate' 200)).canUse -eq 'False') (Cmd 'ddstate' 200)
    $r = Cmd 'ddrepair' 400
    Check 'repair makes the gourd usable' ((Fields (Cmd 'ddstate' 200)).canUse -eq 'True' -and (Fields (Cmd 'ddstate' 200)).damaged -eq 'False') "$r | $(Cmd 'ddstate' 200)"
    Check 'the change log has the gourd line' (LogHas '연요호')
    Check 'no callback exception in the log' ((Traps) -eq 0)
  }
  default { throw "알 수 없는 Part: $Part" }
}
Finish
