param(
  [ValidateSet('info','shot','click','key')] [string]$Do = 'info',
  [int]$X = 0, [int]$Y = 0, [int]$Vk = 0,
  [string]$Out = "$PSScriptRoot\shot.png", [double]$Scale = 0.5
)
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class W {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
'@
[void][W]::SetProcessDPIAware()
$p = Get-Process guigubahuang -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { 'NO GAME PROCESS'; exit 1 }
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { 'NO MAIN WINDOW YET'; exit 1 }
$c = New-Object W+RECT; [void][W]::GetClientRect($h, [ref]$c)
$w = New-Object W+RECT; [void][W]::GetWindowRect($h, [ref]$w)
$fg = ([W]::GetForegroundWindow() -eq $h)
switch ($Do) {
  'info' { "hwnd=$h client=$($c.R)x$($c.B) window=($($w.L),$($w.T))-($($w.R),$($w.B)) foreground=$fg minimized=$([W]::IsIconic($h)) responding=$($p.Responding)" }
  'shot' {
    $bw = $w.R - $w.L; $bh = $w.B - $w.T
    $bmp = New-Object System.Drawing.Bitmap $bw, $bh
    $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    $ok = [W]::PrintWindow($h, $hdc, 2)
    $g.ReleaseHdc($hdc); $g.Dispose()
    $sw = [int]($bw * $Scale); $sh = [int]($bh * $Scale)
    $small = New-Object System.Drawing.Bitmap $bmp, $sw, $sh
    $small.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose(); $small.Dispose()
    "printwindow=$ok saved=$Out size=${sw}x${sh} scale=$Scale foreground=$fg"
  }
  'click' {
    $l = [IntPtr](($Y -shl 16) -bor ($X -band 0xFFFF))
    [void][W]::PostMessage($h, 0x0200, [IntPtr]0, $l)   # WM_MOUSEMOVE
    Start-Sleep -Milliseconds 120
    [void][W]::PostMessage($h, 0x0201, [IntPtr]1, $l)   # WM_LBUTTONDOWN
    Start-Sleep -Milliseconds 90
    [void][W]::PostMessage($h, 0x0202, [IntPtr]0, $l)   # WM_LBUTTONUP
    "posted click at client ($X,$Y) foreground=$fg"
  }
  'key' {
    [void][W]::PostMessage($h, 0x0100, [IntPtr]$Vk, [IntPtr]1)            # WM_KEYDOWN
    Start-Sleep -Milliseconds 80
    [void][W]::PostMessage($h, 0x0101, [IntPtr]$Vk, [IntPtr]0xC0000001)   # WM_KEYUP
    "posted key vk=$Vk foreground=$fg"
  }
}
