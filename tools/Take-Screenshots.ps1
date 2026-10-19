<#
.SYNOPSIS
  Takes Play Store screenshots (1080 × 1920) of a game playing its own demo.

.DESCRIPTION
  Runs the game with -- --demo and Godot's movie maker, writing every frame, then keeps the frames
  at the given seconds as shot1.png, shot2.png, … in -Out and deletes the rest. A temporary
  override.cfg sets the window to 1080 × 1920 (the project's own window size would win over
  --resolution).

.EXAMPLE
  .\Take-Screenshots.ps1 -Project "C:\GameDev\Godot and VS Code\Mancala" -Out C:\GameDev\PlayStore\Mancala -At 6,14,30
#>
param(
	[Parameter(Mandatory)][string]$Project,
	[Parameter(Mandatory)][string]$Out,
	[double[]]$At = @(6, 15, 28),
	[string[]]$GameArgs = @('--demo'),
	[int]$Fps = 15
)
$ErrorActionPreference = 'Stop'
$godot = 'C:\GameDev\Godot and VS Code\Godot.exe'
$Project = (Resolve-Path $Project).Path
New-Item -ItemType Directory -Force $Out | Out-Null
$frames = Join-Path $env:TEMP ("shots-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $frames | Out-Null
Add-Type -AssemblyName System.Drawing

# Play wants screenshots without transparency; the window is scaled up to 1080 × 1920.
function Save-Shot([string]$From, [string]$To) {
	$src = [System.Drawing.Image]::FromFile($From)
	$dst = New-Object System.Drawing.Bitmap 1080, 1920, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
	$g = [System.Drawing.Graphics]::FromImage($dst)
	$g.InterpolationMode = 'HighQualityBicubic'; $g.PixelOffsetMode = 'HighQuality'
	$g.DrawImage($src, 0, 0, 1080, 1920)
	$g.Dispose(); $src.Dispose()
	$dst.Save($To, [System.Drawing.Imaging.ImageFormat]::Png); $dst.Dispose()
}

# The window must fit on the screen (a 1080-pixel-tall desktop clips a 1920 window and the 3D view
# comes out zoomed in), so record at 594 × 1056, borderless, and scale up.
$override = Join-Path $Project 'override.cfg'
@'
[display]
window/size/window_width_override=594
window/size/window_height_override=1056
window/size/borderless=true
window/size/initial_position_type=0
'@ | Set-Content -Encoding ascii $override
try {
	$last = [int](($At | Measure-Object -Maximum).Maximum * $Fps) + 2
	$arguments = @('--path', "`"$Project`"", '--write-movie', "`"$frames\f.png`"", '--fixed-fps', "$Fps", '--quit-after', "$last", '--') + $GameArgs
	$run = Start-Process -FilePath $godot -ArgumentList $arguments -NoNewWindow -PassThru
	if (-not $run.WaitForExit(600000)) { $run.Kill(); throw 'The demo took over 10 minutes and was stopped' }
	$i = 0
	foreach ($second in $At) {
		$i++
		$frame = Join-Path $frames ('f{0:D8}.png' -f [int]($second * $Fps))
		if (Test-Path $frame) { Save-Shot $frame (Join-Path $Out "shot$i.png"); Write-Host "shot$i.png: ${second}s" }
		else { Write-Warning "No frame at ${second}s" }
	}
}
finally {
	Remove-Item $override -ErrorAction SilentlyContinue
	Remove-Item $frames -Recurse -Force -ErrorAction SilentlyContinue
}
