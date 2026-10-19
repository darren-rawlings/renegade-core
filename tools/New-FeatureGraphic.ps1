<#
.SYNOPSIS
  Makes a Play Store feature graphic (1024 × 500, 24-bit PNG): the game's icon, title and
  tagline on a blurred, darkened backdrop from a screenshot, with the screenshot itself on the
  right like a phone screen.

.EXAMPLE
  .\New-FeatureGraphic.ps1 -Icon icon_512.png -Shot shot1.png -Title "Mancala" -Tagline "Sow, capture, win" -Out feature.png
#>
param(
	[Parameter(Mandatory)][string]$Icon,
	[Parameter(Mandatory)][string]$Shot,
	[Parameter(Mandatory)][string]$Title,
	[string]$Tagline = '',
	[Parameter(Mandatory)][string]$Out
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$W = 1024; $H = 500

function Get-RoundedPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
	$p = New-Object System.Drawing.Drawing2D.GraphicsPath
	$p.AddArc($x, $y, $r * 2, $r * 2, 180, 90); $p.AddArc($x + $w - $r * 2, $y, $r * 2, $r * 2, 270, 90)
	$p.AddArc($x + $w - $r * 2, $y + $h - $r * 2, $r * 2, $r * 2, 0, 90); $p.AddArc($x, $y + $h - $r * 2, $r * 2, $r * 2, 90, 90)
	$p.CloseFigure()
	return $p
}

# 24-bit: Play refuses a feature graphic with an alpha channel.
$canvas = New-Object System.Drawing.Bitmap $W, $H, ([System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.SmoothingMode = 'AntiAlias'; $g.InterpolationMode = 'HighQualityBicubic'; $g.TextRenderingHint = 'AntiAliasGridFit'
$screen = [System.Drawing.Image]::FromFile((Resolve-Path $Shot).Path)

# Backdrop: the screenshot shrunk to a few pixels and stretched back up (a cheap, smooth blur),
# then darkened so the title reads.
$tiny = New-Object System.Drawing.Bitmap 24, 12
$tg = [System.Drawing.Graphics]::FromImage($tiny); $tg.InterpolationMode = 'HighQualityBilinear'
$tg.DrawImage($screen, 0, 0, 24, 12); $tg.Dispose()
$g.DrawImage($tiny, -40, -40, $W + 80, $H + 80); $tiny.Dispose()
$dark = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(150, 10, 8, 6))
$g.FillRectangle($dark, 0, 0, $W, $H)

# The screenshot as a phone screen on the right, with a soft shadow.
$ph = 440; $pw = [int]($ph * $screen.Width / $screen.Height); $px = $W - $pw - 56; $py = ($H - $ph) / 2
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$g.FillPath($shadow, (Get-RoundedPath ($px + 6) ($py + 10) $pw $ph 22))
$frame = Get-RoundedPath $px $py $pw $ph 22
$g.SetClip($frame); $g.DrawImage($screen, $px, $py, $pw, $ph); $g.ResetClip()
$g.DrawPath((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(160, 255, 255, 255)), 2), $frame)
$screen.Dispose()

# The icon, with rounded corners.
$size = 168; $ix = 52; $iy = ($H - $size) / 2
$iconImage = [System.Drawing.Image]::FromFile((Resolve-Path $Icon).Path)
$g.SetClip((Get-RoundedPath $ix $iy $size $size 36)); $g.DrawImage($iconImage, $ix, $iy, $size, $size); $g.ResetClip()
$iconImage.Dispose()

# Title (shrunk until it fits in two lines) and tagline, between the icon and the phone.
$left = $ix + $size + 34; $room = $px - $left - 30
$fontSize = 60
do {
	$font = New-Object System.Drawing.Font 'Segoe UI Semibold', $fontSize, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
	$measure = $g.MeasureString($Title, $font, $room)
	$fontSize -= 4
} while ((($measure.Height -gt 2.6 * $font.Height) -or ($measure.Width -gt $room)) -and $fontSize -gt 28)
$small = New-Object System.Drawing.Font 'Segoe UI', 26, ([System.Drawing.FontStyle]::Regular), ([System.Drawing.GraphicsUnit]::Pixel)
$tagSize = if ($Tagline) { $g.MeasureString($Tagline, $small, $room) } else { New-Object System.Drawing.SizeF 0, 0 }
$textTop = ($H - $measure.Height - $tagSize.Height - 6) / 2
$g.DrawString($Title, $font, [System.Drawing.Brushes]::White, (New-Object System.Drawing.RectangleF $left, $textTop, $room, ($measure.Height + 4)))
if ($Tagline) {
	$gold = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 245, 197, 66))
	$g.DrawString($Tagline, $small, $gold, (New-Object System.Drawing.RectangleF $left, ($textTop + $measure.Height + 6), $room, ($tagSize.Height + 4)))
}
$g.Dispose()
$canvas.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
Write-Host "Feature graphic: $Out"
