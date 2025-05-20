# Meant to be run from the root folder, adjust if needed

param(
  [string]$BuildDir = ".",
  [string]$ModName   = "NotADrill",
  [string]$OutDir = "package\$ModName"
)

$ErrorActionPreference = "Stop"

Remove-Item -LiteralPath "$OutDir\.." -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $OutDir | Out-Null

Copy-Item -Path "$BuildDir\ModCover.jpg" -Destination $OutDir
Copy-Item -Path "$BuildDir\ModInfo.json" -Destination $OutDir -ErrorAction SilentlyContinue

Copy-Item -Path "Translations" -Destination "$OutDir/Translations" -Recurse
Copy-Item -Path "Libraries" -Destination "$OutDir/Libraries" -Recurse
Copy-Item -Path "README.md" -Destination $OutDir

$datePart = Get-Date -Format yyyyMMdd
$baseName = "$ModName-$datePart"
$zipName  = "$baseName.zip"

# If it exists, append -v1, -v2, ... until unique
if (Test-Path $zipName) {
    $version = 1
    do {
        $zipName = "$baseName-v$version.zip"
        $version++
    } while (Test-Path $zipName)
}

# --- Create the zip ------------------------------------------
Compress-Archive -Path "$OutDir\..\*" -DestinationPath $zipName -Force

Write-Host "Packaged into $zipName"