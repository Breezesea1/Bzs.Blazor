[CmdletBinding()]
param(
    [ValidateRange(0, 255)]
    [int]$ChannelTolerance = 8,
    [ValidateRange(0, 1)]
    [double]$MaximumDifferentPixelRatio = 0.005,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Keeps refreshed baselines only when the visual gate would actually reject the
# committed image. PNG encoding is not byte-reproducible across runs, so an
# unchanged page still yields different bytes; comparing with the gate's own
# tolerance keeps a refresh dispatch quiet unless rendering really moved.
# ChannelTolerance and MaximumDifferentPixelRatio mirror VisualRegressionTests,
# and ImageSharp is used because System.Drawing needs GDI+ and fails on Linux.

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$baselineDirectory = Join-Path $repositoryRoot "tests/Bzs.Blazor.BrowserTests/VisualBaselines"
if (-not (Test-Path -LiteralPath $baselineDirectory -PathType Container)) {
    throw "Baseline directory '$baselineDirectory' does not exist."
}

$imageSharpPath = Join-Path `
    $repositoryRoot `
    "tests/Bzs.Blazor.BrowserTests/bin/$Configuration/net10.0/SixLabors.ImageSharp.dll"
if (-not (Test-Path -LiteralPath $imageSharpPath -PathType Leaf)) {
    throw "ImageSharp assembly '$imageSharpPath' does not exist. Build the browser test project first."
}

Add-Type -LiteralPath $imageSharpPath
$rgba32 = [SixLabors.ImageSharp.PixelFormats.Rgba32]
# ImageSharp exposes ReadOnlySpan<byte> and Stream overloads but no byte[] one,
# and PowerShell cannot build a span, so load through a MemoryStream.
$loader = [SixLabors.ImageSharp.Image].GetMethods() |
    Where-Object {
        $_.Name -eq "Load" -and
        $_.IsGenericMethod -and
        $_.GetParameters().Count -eq 1 -and
        $_.GetParameters()[0].ParameterType -eq [IO.Stream]
    } |
    Select-Object -First 1
if ($null -eq $loader) {
    throw "Could not resolve the ImageSharp stream Load overload."
}
$loadRgba32 = $loader.MakeGenericMethod($rgba32)

function Read-GitBlob {
    param(
        [Parameter(Mandatory = $true)][string]$Revision,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot
    )

    # git show through PowerShell redirection would re-encode the stream, so read
    # the blob as raw bytes instead.
    $startInfo = [Diagnostics.ProcessStartInfo]::new("git", "show $Revision")
    $startInfo.WorkingDirectory = $RepositoryRoot
    $startInfo.RedirectStandardOutput = $true
    $startInfo.UseShellExecute = $false
    $process = [Diagnostics.Process]::Start($startInfo)
    $buffer = [IO.MemoryStream]::new()
    try {
        $process.StandardOutput.BaseStream.CopyTo($buffer)
    }
    finally {
        $process.WaitForExit()
    }

    if ($process.ExitCode -ne 0) {
        throw "Could not read '$Revision'."
    }

    return $buffer.ToArray()
}

function Test-WithinGateTolerance {
    param(
        [Parameter(Mandatory = $true)][byte[]]$Expected,
        [Parameter(Mandatory = $true)][byte[]]$Actual
    )

    $expectedImage = $null
    $actualImage = $null
    $expectedStream = $null
    $actualStream = $null
    try {
        $expectedStream = [IO.MemoryStream]::new($Expected, $false)
        $actualStream = [IO.MemoryStream]::new($Actual, $false)
        $expectedImage = $loadRgba32.Invoke($null, @($expectedStream))
        $actualImage = $loadRgba32.Invoke($null, @($actualStream))
        if ($expectedImage.Width -ne $actualImage.Width -or
            $expectedImage.Height -ne $actualImage.Height) {
            return $false
        }

        $totalPixels = $expectedImage.Width * $expectedImage.Height
        $allowedPixels = [Math]::Floor($totalPixels * $MaximumDifferentPixelRatio)
        $differentPixels = 0
        for ($y = 0; $y -lt $expectedImage.Height; $y++) {
            for ($x = 0; $x -lt $expectedImage.Width; $x++) {
                $left = $expectedImage[$x, $y]
                $right = $actualImage[$x, $y]
                $delta = [Math]::Max(
                    [Math]::Max(
                        [Math]::Abs($left.R - $right.R),
                        [Math]::Abs($left.G - $right.G)),
                    [Math]::Max(
                        [Math]::Abs($left.B - $right.B),
                        [Math]::Abs($left.A - $right.A)))
                if ($delta -gt $ChannelTolerance) {
                    $differentPixels++
                    if ($differentPixels -gt $allowedPixels) {
                        return $false
                    }
                }
            }
        }

        return $true
    }
    finally {
        if ($null -ne $expectedImage) { $expectedImage.Dispose() }
        if ($null -ne $actualImage) { $actualImage.Dispose() }
        if ($null -ne $expectedStream) { $expectedStream.Dispose() }
        if ($null -ne $actualStream) { $actualStream.Dispose() }
    }
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

    $keptPaths = New-Object System.Collections.Generic.List[string]
    $restoredCount = 0
    foreach ($relativePath in $changed) {
        $committedBytes = Read-GitBlob -Revision "HEAD:$relativePath" -RepositoryRoot $repositoryRoot
        $capturedBytes = [IO.File]::ReadAllBytes((Join-Path $repositoryRoot $relativePath))
        if (Test-WithinGateTolerance -Expected $committedBytes -Actual $capturedBytes) {
            & git checkout -- $relativePath
            if ($LASTEXITCODE -ne 0) {
                throw "Could not restore the equivalent baseline '$relativePath'."
            }

            $restoredCount++
        }
        else {
            $keptPaths.Add($relativePath)
        }
    }

    Write-Host "Restored $restoredCount equivalent baseline(s); kept $($keptPaths.Count) changed baseline(s)."
    foreach ($path in $keptPaths) {
        Write-Host "  changed: $path"
    }
}
finally {
    Pop-Location
}
