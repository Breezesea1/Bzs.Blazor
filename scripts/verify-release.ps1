[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidatePattern('^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$')]
    [string]$Version,
    [switch]$SkipBrowserMatrix,
    [switch]$SkipAot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($env:BZS_UPDATE_VISUAL_BASELINES -eq "1") {
    throw "Release verification refuses to run when BZS_UPDATE_VISUAL_BASELINES is exactly '1'. Unset it before running this script because release verification must not update visual baselines."
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Join-NativePath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string[]]$Segments
    )

    $path = $BasePath
    foreach ($segment in $Segments) {
        $path = Join-Path $path $segment
    }

    return $path
}

$solutionPath = Join-Path $repositoryRoot "Bzs.Blazor.slnx"
$packageProject = Join-NativePath $repositoryRoot @("src", "Bzs.Blazor", "Bzs.Blazor.csproj")
$consumerTemplate = Join-NativePath $repositoryRoot @("scripts", "package-consumer")
$consumerTestProject = Join-NativePath $repositoryRoot @("tests", "Bzs.Blazor.PackageConsumerTests", "Bzs.Blazor.PackageConsumerTests.csproj")
$releaseRoot = Join-NativePath $repositoryRoot @("artifacts", "release")
$packageDirectory = Join-Path $releaseRoot "packages"
$consumerRoot = Join-Path $releaseRoot "consumer"
$consumerPackagesDirectory = Join-Path $releaseRoot "consumer-packages"
$publishDirectory = Join-Path $releaseRoot "publish"
$aotDirectory = Join-Path $releaseRoot "aot-client"
$summaryPath = Join-Path $releaseRoot "verification-summary.md"
$versionPattern = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$'

[xml]$packageProjectXml = Get-Content -Raw -LiteralPath $packageProject
$versionNodes = @($packageProjectXml.SelectNodes("/Project/PropertyGroup/Version"))
if ($versionNodes.Count -ne 1 -or [string]::IsNullOrWhiteSpace($versionNodes[0].InnerText)) {
    throw "The package project must define exactly one non-empty Version property."
}

$projectVersion = $versionNodes[0].InnerText.Trim()
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $projectVersion
}
elseif ($Version -cne $projectVersion) {
    throw "Requested version '$Version' must match the package project's Version '$projectVersion'."
}

if ($Version -notmatch $versionPattern) {
    throw "Version '$Version' must be a normalized semantic version such as '1.2.3' or '1.2.3-preview.1'."
}

function Assert-RepositoryPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    $rootWithSeparator = $repositoryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($rootWithSeparator, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the repository: $fullPath"
    }

    return $fullPath
}

function Reset-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = Assert-RepositoryPath $Path
    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $fullPath | Out-Null
    return $fullPath
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$RejectIlWarnings
    )

    Write-Host "dotnet $($Arguments -join ' ')"
    $output = & dotnet @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    $text = $output | Out-String

    if ($exitCode -ne 0) {
        throw "dotnet command failed with exit code $exitCode."
    }

    if ($RejectIlWarnings -and $text -match '\b(?:IL2\d{3}|IL3050)\b') {
        throw "The publish emitted a prohibited trim or AOT warning."
    }

}

function Get-ZipEntries {
    param([Parameter(Mandatory = $true)][string]$Path)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        return @($archive.Entries | ForEach-Object FullName)
    }
    finally {
        $archive.Dispose()
    }
}

function Read-ZipEntry {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$EntryName
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.GetEntry($EntryName)
        if ($null -eq $entry) {
            throw "Package entry '$EntryName' was not found."
        }

        $reader = [IO.StreamReader]::new($entry.Open())
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-PackageContents {
    param(
        [Parameter(Mandatory = $true)][string]$PackagePath,
        [Parameter(Mandatory = $true)][string]$SymbolPackagePath,
        [Parameter(Mandatory = $true)][string]$ExpectedVersion
    )

    $entries = Get-ZipEntries $PackagePath
    $requiredEntries = @(
        "README.md",
        "LICENSE",
        "lib/net10.0/Bzs.Blazor.dll",
        "lib/net10.0/Bzs.Blazor.xml",
        "lib/net10.0/zh-Hans/Bzs.Blazor.resources.dll",
        "staticwebassets/bzs.blazor.css",
        "staticwebassets/Components/Dialog/BzsDialog.razor.js",
        "staticwebassets/Components/Tabs/BzsTabs.razor.js",
        "staticwebassets/Components/Theme/BzsThemeProvider.razor.js",
        "build/Bzs.Blazor.props",
        "buildTransitive/Bzs.Blazor.props"
    )
    foreach ($entry in $requiredEntries) {
        if ($entries -notcontains $entry) {
            throw "Package is missing required entry '$entry'."
        }
    }

    $licenseText = Read-ZipEntry $PackagePath "LICENSE"
    $repositoryLicenseText = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot "LICENSE")
    if (-not [string]::Equals($licenseText, $repositoryLicenseText, [StringComparison]::Ordinal)) {
        throw "Package LICENSE does not match the repository LICENSE."
    }

    $requiredLicenseText = [ordered]@{
        "Copyright (c) 2026 tongyu" = 1
        "Permission is hereby granted, free of charge, to any person obtaining a copy" = 2
        "The above copyright notice and this permission notice shall be included in all" = 2
        'THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR' = 2
        "ISC License" = 1
        "Copyright (c) 2026 Lucide Icons and Contributors" = 1
        "Permission to use, copy, modify, and/or distribute this software for any" = 1
        "copyright notice and this permission notice appear in all copies." = 1
        'THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH' = 1
        "The MIT License (MIT) (for the icons listed above)" = 1
        "Copyright (c) 2013-present Cole Bemis" = 1
        "Source: https://github.com/lucide-icons/lucide" = 1
    }
    foreach ($notice in $requiredLicenseText.GetEnumerator()) {
        $occurrenceCount = [regex]::Matches(
            $licenseText,
            [regex]::Escape($notice.Key)).Count
        if ($occurrenceCount -ne $notice.Value) {
            throw "Package LICENSE must contain '$($notice.Key)' exactly $($notice.Value) time(s); found $occurrenceCount."
        }
    }

    if (-not ($entries | Where-Object { $_ -match '^staticwebassets/Bzs\.Blazor\..+\.bundle\.scp\.css$' })) {
        throw "Package is missing the scoped CSS bundle."
    }

    $symbolEntries = Get-ZipEntries $SymbolPackagePath
    if ($symbolEntries -notcontains "lib/net10.0/Bzs.Blazor.pdb") {
        throw "Symbol package is missing lib/net10.0/Bzs.Blazor.pdb."
    }

    [xml]$nuspec = Read-ZipEntry $PackagePath "Bzs.Blazor.nuspec"
    $metadata = $nuspec.package.metadata
    if ($metadata.id -ne "Bzs.Blazor" -or $metadata.version -ne $ExpectedVersion) {
        throw "Package identity or version is incorrect; expected Bzs.Blazor $ExpectedVersion."
    }
    if ($metadata.license.type -ne "expression" -or $metadata.license.'#text' -ne "MIT") {
        throw "Package license metadata is incorrect."
    }
    if ($metadata.readme -ne "README.md") {
        throw "Package README metadata is missing."
    }

    $dependencyIds = @($metadata.dependencies.group.dependency | ForEach-Object id)
    $unexpectedDependencies = $dependencyIds | Where-Object {
        $_ -notin @("Microsoft.AspNetCore.Components.Web", "Microsoft.Extensions.Localization")
    }
    if ($unexpectedDependencies) {
        throw "Package has unexpected runtime dependencies: $($unexpectedDependencies -join ', ')"
    }
}

function Assert-RestoredPackageMatches {
    param(
        [Parameter(Mandatory = $true)][string]$ExpectedPackagePath,
        [Parameter(Mandatory = $true)][string]$ExpectedVersion
    )

    $restoredPackagePath = Join-NativePath $consumerPackagesDirectory @(
        "bzs.blazor",
        $ExpectedVersion,
        "bzs.blazor.$ExpectedVersion.nupkg")
    if (-not (Test-Path -LiteralPath $restoredPackagePath)) {
        throw "The isolated consumer cache does not contain Bzs.Blazor $ExpectedVersion."
    }

    $expectedHash = (Get-FileHash -LiteralPath $ExpectedPackagePath -Algorithm SHA256).Hash
    $restoredHash = (Get-FileHash -LiteralPath $restoredPackagePath -Algorithm SHA256).Hash
    if ($expectedHash -ne $restoredHash) {
        throw "The temporary consumer restored a different Bzs.Blazor $ExpectedVersion package."
    }
}

function Set-ConsumerPackageVersion {
    param(
        [Parameter(Mandatory = $true)][string[]]$ProjectPaths,
        [Parameter(Mandatory = $true)][string]$PackageVersion
    )

    foreach ($projectPath in $ProjectPaths) {
        [xml]$projectXml = Get-Content -Raw -LiteralPath $projectPath
        $packageReferences = @($projectXml.SelectNodes(
            "/Project/ItemGroup/PackageReference[@Include='Bzs.Blazor']"))
        if ($packageReferences.Count -ne 1) {
            throw "Consumer project '$projectPath' must contain exactly one Bzs.Blazor package reference."
        }

        $packageReferences[0].SetAttribute("Version", $PackageVersion)
        $projectXml.Save($projectPath)
    }
}

function Get-OpenPort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try {
        return ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Invoke-PackageConsumerSmoke {
    param(
        [Parameter(Mandatory = $true)][string[]]$ProcessArguments,
        [Parameter(Mandatory = $true)][string]$WorkingDirectory,
        [Parameter(Mandatory = $true)][string]$Runtimes,
        [Parameter(Mandatory = $true)][string]$LogName,
        [Parameter(Mandatory = $true)][string]$AspNetCoreEnvironment
    )

    $port = Get-OpenPort
    $baseUrl = "http://127.0.0.1:$port"
    $stdoutPath = Join-Path $releaseRoot "$LogName.stdout.log"
    $stderrPath = Join-Path $releaseRoot "$LogName.stderr.log"
    $arguments = @($ProcessArguments) + @("--urls", $baseUrl)

    $previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
    try {
        $env:ASPNETCORE_ENVIRONMENT = $AspNetCoreEnvironment
        $startProcessParameters = @{
            FilePath = "dotnet"
            ArgumentList = $arguments
            WorkingDirectory = $WorkingDirectory
            RedirectStandardOutput = $stdoutPath
            RedirectStandardError = $stderrPath
            PassThru = $true
        }
        if ($IsWindows) {
            $startProcessParameters["WindowStyle"] = "Hidden"
        }

        $consumerProcess = Start-Process @startProcessParameters
    }
    finally {
        if ($null -eq $previousAspNetCoreEnvironment) {
            Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
        }
        else {
            $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment
        }
    }

    try {
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds(90)
        do {
            if ($consumerProcess.HasExited) {
                throw "The package consumer exited with code $($consumerProcess.ExitCode). See $stderrPath."
            }

            try {
                $response = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing -TimeoutSec 2
                if ($response.StatusCode -eq 200) {
                    break
                }
            }
            catch {
            }

            Start-Sleep -Milliseconds 250
        } while ([DateTimeOffset]::UtcNow -lt $deadline)

        if ([DateTimeOffset]::UtcNow -ge $deadline) {
            throw "The package consumer did not become ready at $baseUrl."
        }

        $previousBaseUrl = $env:BZS_PACKAGE_CONSUMER_BASE_URL
        $previousRuntimes = $env:BZS_PACKAGE_CONSUMER_RUNTIMES
        try {
            $env:BZS_PACKAGE_CONSUMER_BASE_URL = $baseUrl
            $env:BZS_PACKAGE_CONSUMER_RUNTIMES = $Runtimes
            Invoke-DotNet @(
                "test", $consumerTestProject,
                "--configuration", $Configuration,
                "--no-build", "--no-restore")
        }
        finally {
            if ($null -eq $previousBaseUrl) {
                Remove-Item Env:BZS_PACKAGE_CONSUMER_BASE_URL -ErrorAction SilentlyContinue
            }
            else {
                $env:BZS_PACKAGE_CONSUMER_BASE_URL = $previousBaseUrl
            }

            if ($null -eq $previousRuntimes) {
                Remove-Item Env:BZS_PACKAGE_CONSUMER_RUNTIMES -ErrorAction SilentlyContinue
            }
            else {
                $env:BZS_PACKAGE_CONSUMER_RUNTIMES = $previousRuntimes
            }
        }
    }
    finally {
        if (-not $consumerProcess.HasExited) {
            Stop-Process -Id $consumerProcess.Id -Force
            $consumerProcess.WaitForExit()
        }

        $consumerProcess.Dispose()
    }
}

Reset-Directory $releaseRoot | Out-Null
New-Item -ItemType Directory -Path $packageDirectory | Out-Null

Invoke-DotNet @("clean", $solutionPath, "--configuration", $Configuration)
Invoke-DotNet @("restore", $solutionPath)
Invoke-DotNet @("build", $solutionPath, "--configuration", $Configuration, "--no-restore")
Invoke-DotNet @("test", $solutionPath, "--configuration", $Configuration, "--no-build", "--no-restore")

if (-not $SkipBrowserMatrix) {
    $browserMatrixScript = Join-NativePath $repositoryRoot @(
        "tests", "Bzs.Blazor.BrowserTests", "run-browser-matrix.ps1")
    & pwsh $browserMatrixScript `
        -Configuration $Configuration `
        -ArtifactsDirectory (Join-Path $releaseRoot "browser-matrix")
    if ($LASTEXITCODE -ne 0) {
        throw "The browser matrix failed."
    }
}

Invoke-DotNet @(
    "pack", $packageProject,
    "--configuration", $Configuration,
    "--no-build", "--no-restore",
    "--output", $packageDirectory,
    "-p:Version=$Version")

$packagePath = Join-Path $packageDirectory "Bzs.Blazor.$Version.nupkg"
$symbolPackagePath = Join-Path $packageDirectory "Bzs.Blazor.$Version.snupkg"
Assert-PackageContents $packagePath $symbolPackagePath $Version

Reset-Directory $consumerRoot | Out-Null
Reset-Directory $consumerPackagesDirectory | Out-Null
Get-ChildItem -LiteralPath $consumerTemplate | Copy-Item -Destination $consumerRoot -Recurse -Force

$hostProject = Join-NativePath $consumerRoot @("Bzs.Blazor.Consumer", "Bzs.Blazor.Consumer.csproj")
$clientProject = Join-NativePath $consumerRoot @("Bzs.Blazor.Consumer.Client", "Bzs.Blazor.Consumer.Client.csproj")
$aotProject = Join-NativePath $consumerRoot @("Bzs.Blazor.Consumer.Aot", "Bzs.Blazor.Consumer.Aot.csproj")
Set-ConsumerPackageVersion @($hostProject, $clientProject, $aotProject) $Version
$projectText = Get-Content -Raw $hostProject, $clientProject, $aotProject
if ($projectText -match 'src[\\/]Bzs\.Blazor|Bzs\.Blazor\.csproj') {
    throw "The temporary consumer contains a forbidden runtime project reference."
}

$restoreSources = @(
    "-p:RestoreAdditionalProjectSources=$packageDirectory",
    "-p:RestorePackagesPath=$consumerPackagesDirectory")
Invoke-DotNet (@("restore", $hostProject) + $restoreSources)
Assert-RestoredPackageMatches $packagePath $Version
Invoke-DotNet @("build", $hostProject, "--configuration", $Configuration, "--no-restore")
Invoke-DotNet @("restore", $consumerTestProject)
Invoke-DotNet @("build", $consumerTestProject, "--configuration", $Configuration, "--no-restore")

Invoke-PackageConsumerSmoke `
    -ProcessArguments @(
        "run", "--project", $hostProject,
        "--configuration", $Configuration,
        "--no-build", "--no-restore", "--") `
    -WorkingDirectory $consumerRoot `
    -Runtimes "server,wasm,auto" `
    -LogName "consumer-source" `
    -AspNetCoreEnvironment "Development"

if (-not $SkipAot) {
    $workloadList = dotnet workload list | Out-String
    if ($workloadList -notmatch '(?m)^wasm-tools\s') {
        throw "The wasm-tools workload is required. Run 'dotnet workload install wasm-tools'."
    }

    Reset-Directory $aotDirectory | Out-Null
    Invoke-DotNet (@(
        "restore", $aotProject,
        "-p:RunAOTCompilation=true",
        "-p:PublishTrimmed=true") + $restoreSources)
    Invoke-DotNet @(
        "publish", $aotProject,
        "--configuration", $Configuration,
        "--no-restore",
        "--output", $aotDirectory,
        "-p:RunAOTCompilation=true",
        "-p:PublishTrimmed=true") -RejectIlWarnings

    Reset-Directory $publishDirectory | Out-Null
    Invoke-DotNet @(
        "publish", $hostProject,
        "--configuration", $Configuration,
        "--no-restore",
        "--output", $publishDirectory)

    $aotWebRoot = Join-Path $aotDirectory "wwwroot"
    $publishedAotRoot = Join-NativePath $publishDirectory @("wwwroot", "aot")
    New-Item -ItemType Directory -Path $publishedAotRoot -Force | Out-Null
    Get-ChildItem -LiteralPath $aotWebRoot | Copy-Item -Destination $publishedAotRoot -Recurse -Force

    $aotFrameworkRoot = Join-Path $publishedAotRoot "_framework"
    if (-not (Get-ChildItem -LiteralPath $aotFrameworkRoot -Filter "*.dat")) {
        throw "The published AOT consumer is missing ICU globalization data."
    }
    $aotZhHansRoot = Join-Path $aotFrameworkRoot "zh-Hans"
    if (-not (Get-ChildItem -LiteralPath $aotZhHansRoot -Filter "Bzs.Blazor.resources*.wasm")) {
        throw "The published AOT consumer is missing the zh-Hans package satellite resource."
    }

    Invoke-PackageConsumerSmoke `
        -ProcessArguments @("Bzs.Blazor.Consumer.dll") `
        -WorkingDirectory $publishDirectory `
        -Runtimes "server,aot" `
        -LogName "consumer-published" `
        -AspNetCoreEnvironment "Production"
}

$remoteUrl = git -C $repositoryRoot remote get-url origin 2>$null
$sourceLinkStatus = if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($remoteUrl)) {
    "Repository URL available: $remoteUrl"
}
else {
    "No Git remote URL is configured; resolvable Source Link URLs cannot be produced."
}

@"
# Bzs.Blazor $Version local verification

- Configuration: $Configuration
- Package: $packagePath
- Symbols: $symbolPackagePath
- Package-only consumer: $consumerRoot
- Isolated consumer packages: $consumerPackagesDirectory
- Published consumer: $publishDirectory
- Published AOT client: $aotDirectory
- Source Link: $sourceLinkStatus
- Browser matrix skipped: $SkipBrowserMatrix
- WASM AOT publish skipped: $SkipAot
"@ | Set-Content -Path $summaryPath -Encoding utf8

Write-Host "Release verification passed."
Write-Host "Package: $packagePath"
Write-Host "Summary: $summaryPath"
