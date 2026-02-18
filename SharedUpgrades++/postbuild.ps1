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

# Copy DLL
Copy-Item -LiteralPath $DllPath -Destination $buildZipDir -Force

# Update manifests in-place (preserves formatting)
Update-ManifestVersion (Join-Path $buildZipDir "manifest.json") $Version
Update-ManifestVersion (Join-Path $RepoRoot "manifest.json") $Version

# Create zip (exclude any existing zip)
$zipPath = Join-Path $buildZipDir "SharedUpgradesPlus.zip"
$files = Get-ChildItem -LiteralPath $buildZipDir -File | Where-Object { $_.Extension -ne ".zip" }
Compress-Archive -Path $files.FullName -DestinationPath $zipPath -Force

Write-Host "Packaged v$Version -> $zipPath"
