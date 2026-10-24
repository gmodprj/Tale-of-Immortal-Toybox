# .devremote 폴더(회차 표식·세이브 사본·캡처·명령 채널)의 기준 하나. 환경 변수 TLTOYBOX_DEVREMOTE가 있으면 그 폴더, 없으면 E:\TlToybox\.devremote.
# 저장소 위치에서 계산하지 않는다: 워크트리에서 돌려도 한 곳에 모이고, 두 체크아웃이 동시에 회차를 열면 표식이 막는다(리뷰 T1, 05 문서 49절).
# C# 쪽(DevCommands.Dir, DevRemote.cs)도 같은 규칙이다. 쓰는 법: $dir = & (Join-Path $PSScriptRoot 'devremote-dir.ps1')
# 값의 앞뒤 공백·따옴표를 지우고, 드라이브 뿌리(E:\)가 아니면 끝 역슬래시를 뗀다. 공백뿐이면 기본값.
$value = $env:TLTOYBOX_DEVREMOTE
$value = if ($value) { $value.Trim().Trim('"', "'").Trim() } else { '' }
if (-not $value) { 'E:\TlToybox\.devremote' }
elseif ($value -match '^[A-Za-z]:[\\/]?$') { $value.TrimEnd('\', '/') + '\' }
else { $value.TrimEnd('\', '/') }
