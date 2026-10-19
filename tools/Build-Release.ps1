<#
.SYNOPSIS
  Builds a game's signed Google Play bundle (.aab), ready to upload.

.DESCRIPTION
  1. Installs the Android build template if the game doesn't have one yet.
  2. Removes the advertising ID permissions the ads SDK adds (Families policy: children's games
     must not send it). The template is regenerated, so this runs on every build.
  3. Signs with the game's upload key from C:\GameDev\Android\keystores (<key>-release.keystore,
     password in <key>-release-README.txt), passed to Godot in environment variables so the
     password never lands in a project file.
  4. Exports build\android\<Name>.aab (or a release .apk with -Apk, to try on a phone).

.EXAMPLE
  .\Build-Release.ps1 -Project "C:\GameDev\Godot and VS Code\Mancala"
#>
param(
	[Parameter(Mandatory)][string]$Project,
	# The keystore's name; the project folder in lower case unless given.
	[string]$Key,
	[switch]$Apk
)
$ErrorActionPreference = 'Stop'
$godot = 'C:\GameDev\Godot and VS Code\Godot.exe'
$keystores = 'C:\GameDev\Android\keystores'

$Project = (Resolve-Path $Project).Path
$folder = Split-Path $Project -Leaf
if (-not $Key) { $Key = $folder.ToLower() }
$keystore = Join-Path $keystores "$Key-release.keystore"
$readme = Join-Path $keystores "$Key-release-README.txt"
if (-not (Test-Path $keystore)) { throw "No upload key: $keystore" }
$password = (Get-Content $readme | Where-Object { $_ -match '^Password:' }) -replace '^Password:\s*(\S+).*', '$1'
if (-not $password) { throw "No password in $readme" }

# 1. The build template (a debug export installs it; it isn't kept in git).
if (-not (Test-Path (Join-Path $Project 'android\build\src\main\AndroidManifest.xml'))) {
	Write-Host 'Installing the Android build template...'
	$ErrorActionPreference = 'Continue'
	& $godot --headless --path $Project --install-android-build-template --export-debug 'Android' "$env:TEMP\template-install.apk" 2>&1 | Out-Null
	$ErrorActionPreference = 'Stop'
}

# 2. No advertising ID.
$manifest = Join-Path $Project 'android\build\src\main\AndroidManifest.xml'
$xml = [IO.File]::ReadAllText($manifest)
if ($xml -notmatch 'AD_ID') {
	$remove = "`n    <uses-permission android:name=`"com.google.android.gms.permission.AD_ID`" tools:node=`"remove`" />" +
		"`n    <uses-permission android:name=`"android.permission.AD_ID`" tools:node=`"remove`" />"
	$xml = [regex]::Replace($xml, '(<manifest\b[^>]*>)', { param($m) $m.Value + $remove }, 1)
	[IO.File]::WriteAllText($manifest, $xml)
}

# 3 and 4. Sign and export. The preset builds APKs for the phone, so switch it to AAB for this export.
$name = ((Select-String -Path (Join-Path $Project 'project.godot') -Pattern '^config/name="(.*)"').Matches[0].Groups[1].Value) -replace '[^A-Za-z0-9]', ''
$presets = Join-Path $Project 'export_presets.cfg'
$original = [IO.File]::ReadAllText($presets)
$out = Join-Path $Project ("build\android\$name." + $(if ($Apk) { 'apk' } else { 'aab' }))
New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
if (Test-Path $out) { Remove-Item $out }
try {
	if (-not $Apk) {
		$aab = if ($original -match 'gradle_build/export_format=') { $original -replace 'gradle_build/export_format=\d', 'gradle_build/export_format=1' }
			else { $original -replace '(gradle_build/use_gradle_build=true)', "`$1`ngradle_build/export_format=1" }
		[IO.File]::WriteAllText($presets, $aab)
	}
	$env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH = $keystore
	$env:GODOT_ANDROID_KEYSTORE_RELEASE_USER = $Key
	$env:GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD = $password
	Write-Host "Building $out..."
	# Godot writes warnings to stderr; Windows PowerShell would treat them as fatal under 'Stop'.
	$ErrorActionPreference = 'Continue'
	& $godot --headless --path $Project --export-release 'Android' $out 2>&1 | ForEach-Object { "$_" } |
		Where-Object { $_ -match 'ERROR' -and $_ -notmatch 'EditorSettings not instantiated' } | ForEach-Object { Write-Host $_ }
	$ErrorActionPreference = 'Stop'
}
finally {
	[IO.File]::WriteAllText($presets, $original)
	Remove-Item Env:GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD -ErrorAction SilentlyContinue
}
if (-not (Test-Path $out)) { throw "The build failed: no $out" }

# Check the advertising ID really is gone from what was built.
$merged = Get-ChildItem (Join-Path $Project 'android\build\build\intermediates') -Recurse -Filter AndroidManifest.xml |
	Where-Object { $_.FullName -match 'merged_manifests\\monoRelease' } | Select-Object -First 1
if ($merged -and (Select-String -Path $merged.FullName -Pattern 'AD_ID" />' -Quiet)) { throw "The advertising ID is still in $($merged.FullName)" }
Write-Host ("Done: {0} ({1:N1} MB)" -f $out, ((Get-Item $out).Length / 1MB))
