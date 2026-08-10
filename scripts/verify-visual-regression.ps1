[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [ValidateRange(1, [int]::MaxValue)]
    [int]$ExpectedTestCount = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($env:BZS_UPDATE_VISUAL_BASELINES -eq "1") {
    throw "Visual verification refuses to update baselines. Unset BZS_UPDATE_VISUAL_BASELINES before running this script."
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$testProject = Join-Path $repositoryRoot "tests/Bzs.Blazor.BrowserTests/Bzs.Blazor.BrowserTests.csproj"
$resultsDirectory = Join-Path $repositoryRoot "TestResults/visual-regression"
$trxPath = Join-Path $resultsDirectory "visual-regression.trx"

if (Test-Path -LiteralPath $resultsDirectory) {
    Remove-Item -LiteralPath $resultsDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $resultsDirectory | Out-Null

& dotnet test $testProject `
    --configuration $Configuration `
    --no-build `
    --no-restore `
    --filter "FullyQualifiedName~VisualRegressionTests" `
    --results-directory $resultsDirectory `
    --logger "trx;LogFileName=visual-regression.trx"
$testExitCode = $LASTEXITCODE

if (-not (Test-Path -LiteralPath $trxPath -PathType Leaf)) {
    throw "Visual regression test run did not produce '$trxPath'."
}

[xml]$testRun = Get-Content -Raw -LiteralPath $trxPath
$counters = $testRun.SelectSingleNode(
    "/*[local-name()='TestRun']/*[local-name()='ResultSummary']/*[local-name()='Counters']")
if ($null -eq $counters) {
    throw "Visual regression TRX does not contain result counters."
}

$total = [int]$counters.GetAttribute("total")
$executed = [int]$counters.GetAttribute("executed")
$passed = [int]$counters.GetAttribute("passed")
$failed = [int]$counters.GetAttribute("failed")
$notExecuted = [int]$counters.GetAttribute("notExecuted")

if ($testExitCode -ne 0) {
    throw "Visual regression tests failed with exit code $testExitCode. See '$trxPath'."
}

if ($total -ne $ExpectedTestCount -or
    $executed -ne $ExpectedTestCount -or
    $passed -ne $ExpectedTestCount -or
    $failed -ne 0 -or
    $notExecuted -ne 0) {
    throw "Visual regression gate expected exactly $ExpectedTestCount executed and passed tests; total=$total, executed=$executed, passed=$passed, failed=$failed, notExecuted=$notExecuted."
}

Write-Host "Visual regression gate passed: $passed/$ExpectedTestCount tests."
