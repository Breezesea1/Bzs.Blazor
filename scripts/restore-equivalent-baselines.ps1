[CmdletBinding()]
param(
    [ValidateRange(0, 255)]
    [int]$ChannelTolerance = 8,
    [ValidateRange(0, 1)]
    [double]$MaximumDifferentPixelRatio = 0.005
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Keeps refreshed baselines only when the visual gate would actually reject the
# committed image. PNG encoding is not byte-reproducible across runs, so an
# unchanged page still yields different bytes; comparing with the gate's own
# tolerance keeps a refresh dispatch quiet unless rendering really moved.
# ChannelTolerance and MaximumDifferentPixelRatio mirror VisualRegressionTests.

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$baselineDirectory = Join-Path $repositoryRoot "tests/Bzs.Blazor.BrowserTests/VisualBaselines"
if (-not (Test-Path -LiteralPath $baselineDirectory -PathType Container)) {
    throw "Baseline directory '$baselineDirectory' does not exist."
}

Push-Location $repositoryRoot
try {
    $changed = @(& git diff --name-only -- "tests/Bzs.Blazor.BrowserTests/VisualBaselines")
    if ($LASTEXITCODE -ne 0) {
        throw "Could not list changed baselines."
    }

    $changed = @($changed | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($changed.Count -eq 0) {
        Write-Host "No captured baselines differ from the committed images."
        return
    }

    Add-Type -AssemblyName System.Drawing
    $keptPaths = New-Object System.Collections.Generic.List[string]
    $restoredPaths = New-Object System.Collections.Generic.List[string]

    foreach ($relativePath in $changed) {
        $capturedPath = Join-Path $repositoryRoot $relativePath
        $committedPath = [IO.Path]::Combine(
            [IO.Path]::GetTempPath(),
            "bzs-baseline-$([Guid]::NewGuid().ToString('N')).png")
        # git show through PowerShell redirection would re-encode the stream, so
        # write the blob with a byte-exact process redirect instead.
        $showInfo = [Diagnostics.ProcessStartInfo]::new("git", "show HEAD:$relativePath")
        $showInfo.WorkingDirectory = $repositoryRoot
        $showInfo.RedirectStandardOutput = $true
        $showInfo.UseShellExecute = $false
        $showProcess = [Diagnostics.Process]::Start($showInfo)
        $committedStream = [IO.File]::Create($committedPath)
        try {
            $showProcess.StandardOutput.BaseStream.CopyTo($committedStream)
        }
        finally {
            $committedStream.Dispose()
            $showProcess.WaitForExit()
        }

        if ($showProcess.ExitCode -ne 0) {
            throw "Could not read the committed baseline '$relativePath'."
        }

        $captured = $null
        $committed = $null
        try {
            $captured = [System.Drawing.Bitmap]::FromFile($capturedPath)
            $committed = [System.Drawing.Bitmap]::FromFile($committedPath)
            $withinTolerance =
                $captured.Width -eq $committed.Width -and
                $captured.Height -eq $committed.Height
            if ($withinTolerance) {
                $differentPixels = 0
                $totalPixels = $captured.Width * $captured.Height
                $allowedPixels = [Math]::Floor($totalPixels * $MaximumDifferentPixelRatio)
                for ($y = 0; $y -lt $captured.Height -and $withinTolerance; $y++) {
                    for ($x = 0; $x -lt $captured.Width; $x++) {
                        $left = $committed.GetPixel($x, $y)
                        $right = $captured.GetPixel($x, $y)
                        $delta = [Math]::Max(
                            [Math]::Abs($left.R - $right.R),
                            [Math]::Max(
                                [Math]::Abs($left.G - $right.G),
                                [Math]::Max(
                                    [Math]::Abs($left.B - $right.B),
                                    [Math]::Abs($left.A - $right.A))))
                        if ($delta -gt $ChannelTolerance) {
                            $differentPixels++
                            if ($differentPixels -gt $allowedPixels) {
                                $withinTolerance = $false
                                break
                            }
                        }
                    }
                }
            }
        }
        finally {
            if ($null -ne $captured) { $captured.Dispose() }
            if ($null -ne $committed) { $committed.Dispose() }
            Remove-Item -LiteralPath $committedPath -Force -ErrorAction SilentlyContinue
        }

        if ($withinTolerance) {
            & git checkout -- $relativePath
            if ($LASTEXITCODE -ne 0) {
                throw "Could not restore the equivalent baseline '$relativePath'."
            }

            $restoredPaths.Add($relativePath)
        }
        else {
            $keptPaths.Add($relativePath)
        }
    }

    Write-Host "Restored $($restoredPaths.Count) equivalent baseline(s); kept $($keptPaths.Count) changed baseline(s)."
    foreach ($path in $keptPaths) {
        Write-Host "  changed: $path"
    }
}
finally {
    Pop-Location
}
