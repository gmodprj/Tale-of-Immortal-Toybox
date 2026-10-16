# devremote-dir.ps1(.devremote 경로 한 곳)의 시험. 게임을 띄우지 않고 임시 폴더에서 한다. 05 문서 49절.
# 줄마다 PASS/FAIL이 나오고 마지막 줄이 FAILS=<수>다.
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$resolver = Join-Path $here 'devremote-dir.ps1'
$script:fails = 0
function Check([string]$name, [bool]$ok, [string]$detail = '') {
  if ($ok) { "PASS $name" } else { $script:fails++; "FAIL $name  $detail" }
}
# 환경 변수를 바꿔 자식 pwsh에서 돌린다(이 셸의 값은 건드리지 않는다).
function InEnv($value, [string]$command) {
  $prefix = if ($null -eq $value) { 'Remove-Item Env:TLTOYBOX_DEVREMOTE -ErrorAction SilentlyContinue; ' } else { "`$env:TLTOYBOX_DEVREMOTE = '$value'; " }
  try { (& pwsh -NoProfile -Command ($prefix + $command) 2>&1) -join "`n" } catch { 'THROWN: ' + $_.Exception.Message }
}
$box = Join-Path $env:TEMP ('tltoybox-devremote-tests-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($box)
# 스크립트가 환경 변수를 무시하면 실제 폴더(기본값)에 표식 t1·명령 파일이 남아 다음 회차를 막는다. 시험 전후를 비교해 시험이 만든 것만 치우고 FAIL을 낸다.
$real = 'E:\TlToybox\.devremote'
$realBefore = @('session.name', 'mod-cmd.txt', 'cmd.txt') | Where-Object { Test-Path (Join-Path $real $_) }
try {
  Check 'resolver exists' (Test-Path $resolver)
  Check 'default is the fixed folder' ((InEnv $null "& '$resolver'") -eq 'E:\TlToybox\.devremote')
  Check 'the environment variable wins' ((InEnv $box "& '$resolver'") -eq $box)
  Check 'a trailing backslash is trimmed' ((InEnv ($box + '\') "& '$resolver'") -eq $box)
  # mod.ps1·rc.ps1은 명령 파일을 그 폴더에 쓴다(게임이 없으니 결과는 오지 않는다. 짧게 기다린다)
  InEnv $box "& '$here\rc.ps1' -Cmd 'hit 1 1' -NoShot -WaitMs 50" | Out-Null
  Check 'rc.ps1 writes its command into the folder' (Test-Path (Join-Path $box 'cmd.txt'))
  InEnv $box "& '$here\mod.ps1' -Cmd 'uistate' -WaitMs 50" | Out-Null
  Check 'mod.ps1 writes its command into the folder' (Test-Path (Join-Path $box 'mod-cmd.txt'))
  # session.ps1은 -WorkDir가 없으면 그 폴더에 회차 표식을 둔다(-DryRun, 세이브·게임은 임시 폴더)
  $save = Join-Path $box 'save\CacheData'; $data = Join-Path $box 'data'; $game = Join-Path $box 'game'
  foreach ($d in "$save\slot1", $data, "$game\ModExportData\Mod_sWP6UL") { [void][IO.Directory]::CreateDirectory($d) }
  [IO.File]::WriteAllText("$save\DataGloble.cache", 'g'); [IO.File]::WriteAllText("$save\slot1\a.cache", 'a')
  [IO.File]::WriteAllText("$game\ModExportData\Mod_sWP6UL\ModExportData.cache", 'ours')
  $o = InEnv $box "& '$here\session.ps1' -DryRun -Begin t1 -SaveDir '$save' -DataDir '$data' -GameDir '$game' -ProcessName no-such-process-tltoybox"
  Check 'session.ps1 keeps its marker in the folder' (Test-Path (Join-Path $box 'session.name')) $o
  # check-lib.ps1은 $work를 그 폴더로 둔다(표식이 있어야 읽힌다)
  $lib = Join-Path (Split-Path -Parent $here) 'research\check-lib.ps1'
  $o = InEnv $box ". '$lib'; `$work"
  Check 'check-lib.ps1 works in the folder' ($o.Trim() -eq $box) $o
} finally {
  if (Test-Path $box) { Remove-Item -LiteralPath $box -Recurse -Force }
  $leaked = @()
  foreach ($name in 'session.name', 'mod-cmd.txt', 'cmd.txt') {
    $path = Join-Path $real $name
    if ((Test-Path $path) -and ($realBefore -notcontains $name)) {
      if ($name -ne 'session.name' -or [IO.File]::ReadAllText($path).Trim() -eq 't1') { $leaked += $name; Remove-Item -LiteralPath $path -Force }
    }
  }
  if ($leaked -contains 'session.name') { Remove-Item -LiteralPath (Join-Path $real 'save-backup-t1') -Recurse -Force -ErrorAction SilentlyContinue }
  Check 'the real folder was left untouched' ($leaked.Count -eq 0) ("removed: " + ($leaked -join ', '))
}
"FAILS=$script:fails"
if ($script:fails -gt 0) { exit 1 }
