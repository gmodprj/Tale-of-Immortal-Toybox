# 명령 채널(파일 교환)의 공용 함수. mod.ps1(개발 명령, mod-cmd/mod-out)과 rc.ps1·enter.ps1(조작 도구, cmd/out)이 점(.)으로 불러 쓴다.
# 게임은 프레임마다 명령 파일을 읽어 지우고 결과 파일을 쓴다. 그 사이의 경합 셋을 여기서 막는다(05 문서 59절):
#   1) 앞 명령 파일을 게임이 아직 가져가지 않았는데 새 명령을 옮기면 Move가 "이미 있음"으로 실패한다 → 가져갈 때까지 기다리고, 그래도 남으면 덮는다.
#   2) 게임이 결과 파일을 쓰는 중이면 지우기가 막힌다 → 잠깐씩 다시 시도한다.
#   3) 게임이 결과 파일을 쓰는 중이면 읽기가 막힌다 → 잠깐씩 다시 시도한다.

function Send-ChannelCommand([string]$dir, [string]$text, [string]$cmdName) {
  $tmp = Join-Path $dir ($cmdName + '.tmp')
  $dst = Join-Path $dir ($cmdName + '.txt')
  [IO.File]::WriteAllText($tmp, $text)
  for ($n = 0; $n -lt 50 -and (Test-Path $dst); $n++) { [Threading.Thread]::Sleep(100) }
  if (Test-Path $dst) {
    # 5초 동안 게임이 가져가지 않았다(앞 명령이 응답 없이 끝났거나 게임이 멈췄다). 남은 명령은 버리고 새 명령으로 덮는다.
    [IO.File]::Move($tmp, $dst, $true)
    return 'replaced a stale command file'
  }
  [IO.File]::Move($tmp, $dst)
  return $null
}

function Remove-ChannelFile([string]$path) {
  for ($r = 0; $r -lt 20 -and (Test-Path $path); $r++) {
    try { [IO.File]::Delete($path) } catch [IO.IOException] { [Threading.Thread]::Sleep(50) } catch [UnauthorizedAccessException] { [Threading.Thread]::Sleep(50) }
  }
}

function Read-ChannelFile([string]$path) {
  for ($r = 0; $r -lt 20; $r++) {
    try { return [IO.File]::ReadAllText($path) } catch [IO.IOException] { [Threading.Thread]::Sleep(50) }
  }
  return $null
}
