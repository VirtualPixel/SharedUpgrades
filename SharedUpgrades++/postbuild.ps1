param(
    [string]$Version,
    [string]$DllPath,
    [string]$RepoRoot
)

$RepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
$buildZipDir = Join-Path $RepoRoot "BuildZip\SharedUpgradesPlus"
$enc = New-Object System.Text.UTF8Encoding $false

function Update-ManifestVersion([string]$path, [string]$version) {
    $content = [System.IO.File]::ReadAllText($path, $enc)
    $content = $content -replace '"version_number":\s*"[^"]*"', "`"version_number`": `"$version`""
    [System.IO.File]::WriteAllText($path, $content, $enc)
}

# Update root manifest version (single source of truth)
Update-ManifestVersion (Join-Path $RepoRoot "manifest.json") $Version

# Copy DLL, repo-root assets, and manifest into the build zip folder
Copy-Item -LiteralPath $DllPath -Destination $buildZipDir -Force
Copy-Item -LiteralPath (Join-Path $RepoRoot "README.md") -Destination $buildZipDir -Force
Copy-Item -LiteralPath (Join-Path $RepoRoot "CHANGELOG.md") -Destination $buildZipDir -Force
Copy-Item -LiteralPath (Join-Path $RepoRoot "manifest.json") -Destination $buildZipDir -Force
Copy-Item -LiteralPath (Join-Path $RepoRoot "BuildZip\icon256.png") -Destination (Join-Path $buildZipDir "icon.png") -Force

# Create zip (exclude any existing zip)
$zipPath = Join-Path $buildZipDir "SharedUpgradesPlus.zip"
$files = Get-ChildItem -LiteralPath $buildZipDir -File | Where-Object { $_.Extension -ne ".zip" }
Compress-Archive -Path $files.FullName -DestinationPath $zipPath -Force

Write-Host "Packaged v$Version -> $zipPath"

# Clear BepInEx log for a clean test run (silently skip if locked)
try { [System.IO.File]::WriteAllText("$env:APPDATA\Thunderstore Mod Manager\DataFolder\REPO\profiles\Development\BepInEx\LogOutput.log", "", $enc) }
catch { }
