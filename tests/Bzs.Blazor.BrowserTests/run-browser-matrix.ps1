[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$DemoBaseUrl,
    [string]$ArtifactsDirectory,
    [ValidateNotNullOrEmpty()]
    [string[]]$Targets = @("chromium", "mobile-chrome", "chrome", "msedge", "firefox", "webkit", "mobile-safari"),
    [switch]$RequireAllTargets
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Confirm-MatrixTarget {
    param(
        [ValidateSet("chromium", "mobile-chrome", "chrome", "msedge", "firefox", "webkit", "mobile-safari")]
        [string]$Target
    )

    return $Target
}

$Targets = @($Targets | ForEach-Object {
    foreach ($target in $_ -split ",") {
        Confirm-MatrixTarget $target.Trim()
    }
})

$duplicateTargets = @($Targets | Group-Object | Where-Object { $_.Count -gt 1 })
if ($duplicateTargets.Count -gt 0) {
    throw "Targets must not contain duplicates: $($duplicateTargets.Name -join ', ')."
}

$repositoryRoot = (Resolve-Path (Join-Path (Join-Path $PSScriptRoot "..") "..")).Path
$solutionPath = Join-Path $repositoryRoot "Bzs.Blazor.slnx"
$testProjectPath = Join-Path $PSScriptRoot "Bzs.Blazor.BrowserTests.csproj"

if ([string]::IsNullOrWhiteSpace($ArtifactsDirectory)) {
    $ArtifactsDirectory = Join-Path (Join-Path $repositoryRoot "TestResults") "browser-matrix"
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

    if ($IsWindows) {
        $localApplicationData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
        return Join-Path $localApplicationData "ms-playwright"
    }

    if ($IsLinux) {
        if (-not [string]::IsNullOrWhiteSpace($env:XDG_CACHE_HOME)) {
            return Join-Path $env:XDG_CACHE_HOME "ms-playwright"
        }

        return Join-Path (Join-Path $HOME ".cache") "ms-playwright"
    }

    throw "The browser matrix runner supports Windows and Linux only."
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
    param(
        [Parameter(Mandatory = $true)][string]$WindowsRelativePath,
        [Parameter(Mandatory = $true)][string[]]$LinuxCommands
    )

    if ($IsWindows) {
        $roots = @(
            [Environment]::GetEnvironmentVariable("ProgramFiles"),
            [Environment]::GetEnvironmentVariable("ProgramFiles(x86)"),
            [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

        foreach ($root in $roots) {
            if (Test-Path -LiteralPath (Join-Path $root $WindowsRelativePath)) {
                return $true
            }
        }
    }
    elseif ($IsLinux) {
        foreach ($command in $LinuxCommands) {
            if ($null -ne (Get-Command $command -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1)) {
                return $true
            }
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

foreach ($target in $Targets) {
    switch ($target) {
        "chromium" {
            if (Test-PlaywrightBrowserInstalled "chromium") {
                Invoke-MatrixTarget "chromium"
            }
            else {
                $failedTargets.Add("chromium")
                Write-Host "FAIL [chromium] Playwright Chromium is required. Run playwright.ps1 install chromium."
            }
        }
        "mobile-chrome" {
            if (Test-PlaywrightBrowserInstalled "chromium") {
                Invoke-MatrixTarget "mobile-chrome"
            }
            else {
                if ($RequireAllTargets) {
                    $failedTargets.Add("mobile-chrome")
                    Write-Host "FAIL [mobile-chrome] Playwright Chromium executable is required for Pixel 5 emulation."
                }
                else {
                    Write-Host "SKIP [mobile-chrome] Playwright Chromium executable is not installed; Pixel 5 emulation requires Chromium."
                }
            }
        }
        "chrome" {
            if (Test-SystemBrowserInstalled `
                    -WindowsRelativePath "Google\Chrome\Application\chrome.exe" `
                    -LinuxCommands @("google-chrome", "google-chrome-stable")) {
                Invoke-MatrixTarget "chrome"
            }
            else {
                if ($RequireAllTargets) {
                    $failedTargets.Add("chrome")
                    Write-Host "FAIL [chrome] Google Chrome channel is required but was not found on this machine."
                }
                else {
                    Write-Host "SKIP [chrome] Google Chrome channel was not found on this machine."
                }
            }
        }
        "msedge" {
            if (Test-SystemBrowserInstalled `
                    -WindowsRelativePath "Microsoft\Edge\Application\msedge.exe" `
                    -LinuxCommands @("microsoft-edge")) {
                Invoke-MatrixTarget "msedge"
            }
            else {
                if ($RequireAllTargets) {
                    $failedTargets.Add("msedge")
                    Write-Host "FAIL [msedge] Microsoft Edge channel is required but was not found on this machine."
                }
                else {
                    Write-Host "SKIP [msedge] Microsoft Edge channel was not found on this machine."
                }
            }
        }
        "firefox" {
            if (Test-PlaywrightBrowserInstalled "firefox") {
                Invoke-MatrixTarget "firefox"
            }
            else {
                if ($RequireAllTargets) {
                    $failedTargets.Add("firefox")
                    Write-Host "FAIL [firefox] Playwright Firefox is required. Run playwright.ps1 install firefox."
                }
                else {
                    Write-Host "SKIP [firefox] Playwright Firefox executable is not installed. Run playwright.ps1 install firefox."
                }
            }
        }
        "webkit" {
            if (Test-PlaywrightBrowserInstalled "webkit") {
                Invoke-MatrixTarget "webkit"
            }
            else {
                if ($RequireAllTargets) {
                    $failedTargets.Add("webkit")
                    Write-Host "FAIL [webkit] Playwright WebKit is required. Run playwright.ps1 install webkit."
                }
                else {
                    Write-Host "SKIP [webkit] Playwright WebKit executable is not installed. Run playwright.ps1 install webkit."
                }
            }
        }
        "mobile-safari" {
            if (Test-PlaywrightBrowserInstalled "webkit") {
                Invoke-MatrixTarget "mobile-safari"
            }
            else {
                if ($RequireAllTargets) {
                    $failedTargets.Add("mobile-safari")
                    Write-Host "FAIL [mobile-safari] Playwright WebKit is required for iPhone 13 emulation."
                }
                else {
                    Write-Host "SKIP [mobile-safari] Playwright WebKit executable is not installed; iPhone 13 emulation requires WebKit."
                }
            }
        }
    }
}

Write-Host "Artifacts: $ArtifactsDirectory"
if ($failedTargets.Count -gt 0) {
    Write-Host "Failed matrix targets: $($failedTargets -join ', ')"
    exit 1
}
