[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solutionPath = Join-Path $repositoryRoot "Bzs.Blazor.slnx"
$unitTestProject = Join-Path $repositoryRoot "tests/Bzs.Blazor.Tests/Bzs.Blazor.Tests.csproj"

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    Write-Host "dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

Invoke-DotNet @("restore", $solutionPath)
Invoke-DotNet @("build", $solutionPath, "--configuration", $Configuration, "--no-restore")
Invoke-DotNet @(
    "test", $unitTestProject,
    "--configuration", $Configuration,
    "--no-build", "--no-restore")

Write-Host "Fast verification passed."
