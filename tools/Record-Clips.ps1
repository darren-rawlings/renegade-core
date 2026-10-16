<#
.SYNOPSIS
Records a game's tutorial clips: one square Ogg Theora video per clip name, into tutorial/.

.EXAMPLE
powershell -File addons/renegade_core/tools/Record-Clips.ps1 place wave end

.DESCRIPTION
Each game plays a scripted scene for `-- --clip=<name>` with its HUD hidden, then quits. The
project's portrait window size would override --resolution, so a temporary override.cfg makes
the window square while recording.
#>
param([Parameter(Mandatory, ValueFromRemainingArguments)] [string[]] $Clips)
$ErrorActionPreference = 'Stop'
$project = Get-Location
$godot = Join-Path $project '..\Godot.exe' | Resolve-Path
New-Item -ItemType Directory -Force (Join-Path $project 'tutorial') | Out-Null
$override = Join-Path $project 'override.cfg'
@'
[display]
window/size/window_width_override=600
window/size/window_height_override=600
window/size/viewport_width=720
window/size/viewport_height=720
'@ | Set-Content -Encoding ascii $override
try {
	foreach ($clip in $Clips) {
		$out = Join-Path $project "tutorial\$clip.ogv"
		$run = Start-Process -FilePath $godot -ArgumentList @("--path", "`"$project`"", "--write-movie", "`"$out`"", "--fixed-fps", "30", "--", "--clip=$clip") -NoNewWindow -PassThru
		if (-not $run.WaitForExit(120000)) { $run.Kill(); Write-Warning "$clip took over 2 minutes and was stopped" }
		Write-Host "Recorded $out ($([math]::Round((Get-Item $out).Length / 1KB)) KB)"
	}
} finally {
	Remove-Item $override
}
