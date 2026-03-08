param(
    [string]$Version = "",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$InnoSetupCompiler = "$Env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectRoot "NetworkMonitor.csproj"
$publishDir = Join-Path $projectRoot "out\publish\$Runtime"
$issFile = Join-Path $projectRoot "installer\NetworkMonitor.iss"
$installerOutDir = Join-Path $projectRoot "out\installer"

if (!(Test-Path $projectFile)) {
    throw "Project file not found: $projectFile"
}

if (!(Test-Path $issFile)) {
    throw "Installer script not found: $issFile"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$projectXml = Get-Content -Path $projectFile -Encoding UTF8
    $Version = $projectXml.Project.PropertyGroup.Version | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($Version)) {
        throw "Version was not provided and no <Version> was found in $projectFile"
    }
}

Write-Host "[1/3] Publishing application..." -ForegroundColor Cyan
dotnet publish $projectFile -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -o $publishDir

if (!(Test-Path $InnoSetupCompiler)) {
    throw "Inno Setup compiler not found: $InnoSetupCompiler`nInstall Inno Setup 6 or pass -InnoSetupCompiler with a valid ISCC.exe path."
}

Write-Host "[2/3] Building installer..." -ForegroundColor Cyan
& $InnoSetupCompiler $issFile "/DMyAppVersion=$Version" "/DPublishDir=$publishDir"

Write-Host "[3/3] Done" -ForegroundColor Green
Write-Host "Installer output directory: $installerOutDir"
