# .devremote 폴더(회차 표식·세이브 사본·캡처·명령 채널)의 기준 하나. 환경 변수 TLTOYBOX_DEVREMOTE가 있으면 그 폴더, 없으면 E:\TlToybox\.devremote.
# 저장소 위치에서 계산하지 않는다: 워크트리에서 돌려도 한 곳에 모이고, 두 체크아웃이 동시에 회차를 열면 표식이 막는다(리뷰 T1, 05 문서 49절).
# C# 쪽(DevCommands.Dir, DevRemote.cs)도 같은 규칙이다. 쓰는 법: $dir = & (Join-Path $PSScriptRoot 'devremote-dir.ps1')
$value = $env:TLTOYBOX_DEVREMOTE
if ($value) { $value.Trim().TrimEnd('\', '/') } else { 'E:\TlToybox\.devremote' }
