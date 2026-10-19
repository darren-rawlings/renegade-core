<#
.SYNOPSIS
  Makes a Play Store feature graphic (1024 × 500 PNG): a screenshot as the backdrop, the icon,
  the title and a tagline.

.EXAMPLE
  .\New-FeatureGraphic.ps1 -Icon icon_512.png -Backdrop shot2.png -Title "Mancala" -Tagline "Sow, capture, win" -Out feature.png
#>
param(
	[Parameter(Mandatory)][string]$Icon,
	[Parameter(Mandatory)][string]$Backdrop,
	[Parameter(Mandatory)][string]$Title,
	[string]$Tagline = '',
	[Parameter(Mandatory)][string]$Out,
	# Where the backdrop's crop is centred, as a share of its height (the board is usually mid-screen).
	[double]$Focus = 0.5
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$W = 1024; $H = 500
# 24-bit: Play refuses a feature graphic with an alpha channel.
$canvas = New-Object System.Drawing.Bitmap $W, $H, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.SmoothingMode = 'AntiAlias'; $g.InterpolationMode = 'HighQualityBicubic'; $g.TextRenderingHint = 'AntiAliasGridFit'

# Backdrop: the screenshot scaled to the full width, cropped round the focus.
$shot = [System.Drawing.Image]::FromFile((Resolve-Path $Backdrop).Path)
$scale = $W / $shot.Width
$cropH = $H / $scale
$top = [Math]::Max(0, [Math]::Min($shot.Height - $cropH, $shot.Height * $Focus - $cropH / 2))
$g.DrawImage($shot, (New-Object System.Drawing.Rectangle 0, 0, $W, $H), (New-Object System.Drawing.RectangleF 0, $top, $shot.Width, $cropH), 'Pixel')
$shot.Dispose()

# Darken the left side so the title reads.
$shade = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0), (New-Object System.Drawing.Point $W, 0), ([System.Drawing.Color]::FromArgb(235, 12, 10, 8)), ([System.Drawing.Color]::FromArgb(40, 12, 10, 8))
$g.FillRectangle($shade, 0, 0, $W, $H)

# The icon, with rounded corners.
$size = 220; $x = 56; $y = ($H - $size) / 2; $r = 44
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$path.AddArc($x, $y, $r * 2, $r * 2, 180, 90); $path.AddArc($x + $size - $r * 2, $y, $r * 2, $r * 2, 270, 90)
$path.AddArc($x + $size - $r * 2, $y + $size - $r * 2, $r * 2, $r * 2, 0, 90); $path.AddArc($x, $y + $size - $r * 2, $r * 2, $r * 2, 90, 90)
$path.CloseFigure()
$icon = [System.Drawing.Image]::FromFile((Resolve-Path $Icon).Path)
$g.SetClip($path); $g.DrawImage($icon, $x, $y, $size, $size); $g.ResetClip()
$icon.Dispose()

# Title (shrunk to fit) and tagline.
$left = $x + $size + 44; $room = $W - $left - 40
$fontSize = 68
do {
	$font = New-Object System.Drawing.Font 'Segoe UI Semibold', $fontSize, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
	$measure = $g.MeasureString($Title, $font, $room)
	$fontSize -= 4
} while (($measure.Height -gt 200) -and $fontSize -gt 30)
$textTop = ($H - $measure.Height - $(if ($Tagline) { 56 } else { 0 })) / 2
$g.DrawString($Title, $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF $left, $textTop, $room, $measure.Height))
if ($Tagline) {
	$small = New-Object System.Drawing.Font 'Segoe UI', 30, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
	$gold = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 245, 197, 66))
	$g.DrawString($Tagline, $small, $gold, (New-Object System.Drawing.RectangleF $left, ($textTop + $measure.Height + 8), $room, 48))
}
$g.Dispose()
$canvas.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
Write-Host "Feature graphic: $Out"
