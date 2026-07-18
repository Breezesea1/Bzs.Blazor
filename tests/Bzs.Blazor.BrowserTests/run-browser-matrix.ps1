[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$DemoBaseUrl,
    [string]$ArtifactsDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$solutionPath = Join-Path $repositoryRoot "Bzs.Blazor.slnx"
$testProjectPath = Join-Path $PSScriptRoot "Bzs.Blazor.BrowserTests.csproj"

if ([string]::IsNullOrWhiteSpace($ArtifactsDirectory)) {
    $ArtifactsDirectory = Join-Path $repositoryRoot "TestResults\browser-matrix"
}

New-Item -ItemType Directory -Force -Path $ArtifactsDirectory | Out-Null

if (-not [string]::IsNullOrWhiteSpace($DemoBaseUrl)) {
    $env:BZS_DEMO_BASE_URL = $DemoBaseUrl.TrimEnd("/")
}

& dotnet restore $solutionPath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& dotnet build $solutionPath --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

function Get-PlaywrightBrowserCache {
    if ((-not [string]::IsNullOrWhiteSpace($env:PLAYWRIGHT_BROWSERS_PATH)) -and $env:PLAYWRIGHT_BROWSERS_PATH -ne "0") {
        return $env:PLAYWRIGHT_BROWSERS_PATH
    }

    return Join-Path $env:LOCALAPPDATA "ms-playwright"
}

function Test-PlaywrightBrowserInstalled {
    param([Parameter(Mandatory = $true)][string]$Prefix)

    $cachePath = Get-PlaywrightBrowserCache
    if (-not (Test-Path -LiteralPath $cachePath)) {
        return $false
    }

    return $null -ne (Get-ChildItem -LiteralPath $cachePath -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -like "$Prefix-*" } |
        Select-Object -First 1)
}

function Test-SystemBrowserInstalled {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $roots = @(
        [Environment]::GetEnvironmentVariable("ProgramFiles"),
        [Environment]::GetEnvironmentVariable("ProgramFiles(x86)"),
        $env:LOCALAPPDATA
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($root in $roots) {
        if (Test-Path -LiteralPath (Join-Path $root $RelativePath)) {
            return $true
        }
    }

    return $false
}

$failedTargets = [System.Collections.Generic.List[string]]::new()

function Invoke-MatrixTarget {
    param([Parameter(Mandatory = $true)][string]$Target)

    $targetArtifacts = Join-Path $ArtifactsDirectory $Target
    New-Item -ItemType Directory -Force -Path $targetArtifacts | Out-Null

    $previousTarget = $env:BZS_BROWSER_MATRIX_TARGET
    $previousArtifacts = $env:BZS_BROWSER_ARTIFACTS
    try {
        $env:BZS_BROWSER_MATRIX_TARGET = $Target
        $env:BZS_BROWSER_ARTIFACTS = $ArtifactsDirectory
        Write-Host "RUN  [$Target]"
        & dotnet test $testProjectPath --configuration $Configuration --no-build --no-restore `
            --filter "FullyQualifiedName~BrowserMatrixTests" `
            --results-directory $targetArtifacts `
            --logger "trx;LogFileName=browser-matrix-$Target.trx"

        if ($LASTEXITCODE -ne 0) {
            $failedTargets.Add($Target)
            Write-Host "FAIL [$Target] See $targetArtifacts"
        }
    }
    finally {
        if ($null -eq $previousTarget) {
            Remove-Item Env:BZS_BROWSER_MATRIX_TARGET -ErrorAction SilentlyContinue
        }
        else {
            $env:BZS_BROWSER_MATRIX_TARGET = $previousTarget
        }

        if ($null -eq $previousArtifacts) {
            Remove-Item Env:BZS_BROWSER_ARTIFACTS -ErrorAction SilentlyContinue
        }
        else {
            $env:BZS_BROWSER_ARTIFACTS = $previousArtifacts
        }
    }
}

if (Test-PlaywrightBrowserInstalled "chromium") {
    Invoke-MatrixTarget "chromium"
}
else {
    $failedTargets.Add("chromium")
    Write-Host "FAIL [chromium] Playwright Chromium is required. Run playwright.ps1 install chromium."
}

if (Test-PlaywrightBrowserInstalled "chromium") {
    Invoke-MatrixTarget "mobile-chrome"
}
else {
    Write-Host "SKIP [mobile-chrome] Playwright Chromium executable is not installed; Pixel 5 emulation requires Chromium."
}

if (Test-SystemBrowserInstalled "Google\Chrome\Application\chrome.exe") {
    Invoke-MatrixTarget "chrome"
}
else {
    Write-Host "SKIP [chrome] Google Chrome channel is not installed on this machine."
}

if (Test-SystemBrowserInstalled "Microsoft\Edge\Application\msedge.exe") {
    Invoke-MatrixTarget "msedge"
}
else {
    Write-Host "SKIP [msedge] Microsoft Edge channel is not installed on this machine."
}

if (Test-PlaywrightBrowserInstalled "firefox") {
    Invoke-MatrixTarget "firefox"
}
else {
    Write-Host "SKIP [firefox] Playwright Firefox executable is not installed. Run playwright.ps1 install firefox."
}

if (Test-PlaywrightBrowserInstalled "webkit") {
    Invoke-MatrixTarget "webkit"
}
else {
    Write-Host "SKIP [webkit] Playwright WebKit executable is not installed. Run playwright.ps1 install webkit."
}

if (Test-PlaywrightBrowserInstalled "webkit") {
    Invoke-MatrixTarget "mobile-safari"
}
else {
    Write-Host "SKIP [mobile-safari] Playwright WebKit executable is not installed; iPhone 13 emulation requires WebKit."
}

Write-Host "Artifacts: $ArtifactsDirectory"
if ($failedTargets.Count -gt 0) {
    Write-Host "Failed matrix targets: $($failedTargets -join ', ')"
    exit 1
}
