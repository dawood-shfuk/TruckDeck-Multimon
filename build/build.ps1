param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sln = Join-Path $root "TruckDeck.Multimon.sln"

$msbuild = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $msbuild) {
    throw "MSBuild not found. Install Visual Studio 2022 Build Tools."
}

Write-Host "Generating app icon..."
& (Join-Path $PSScriptRoot "generate-app-icon.ps1")

Write-Host "Building TruckDeck Multimon ($Configuration)..."
& $msbuild $sln /p:Configuration=$Configuration /v:m
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$outDir = Join-Path $root "dist\$Configuration"
Write-Host "Done: $(Join-Path $outDir 'TruckDeckMultimon.exe')"
