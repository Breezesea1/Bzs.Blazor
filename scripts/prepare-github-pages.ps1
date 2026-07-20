[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $PublishedWwwroot,

    [ValidateNotNullOrEmpty()]
    [string] $BasePath = '/Bzs.Blazor/'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if ($BasePath -notmatch '^/[A-Za-z0-9._-]+/$') {
    throw "BasePath must contain exactly one repository path segment and have leading and trailing slashes. Received '$BasePath'."
}

$publishedRoot = [System.IO.Path]::GetFullPath($PublishedWwwroot)
$indexPath = Join-Path $publishedRoot 'index.html'

if (-not (Test-Path -LiteralPath $publishedRoot -PathType Container)) {
    throw "Published wwwroot does not exist: $publishedRoot"
}

if (-not (Test-Path -LiteralPath $indexPath -PathType Leaf)) {
    throw "Published index.html does not exist: $indexPath"
}

$index = [System.IO.File]::ReadAllText($indexPath)
$allBaseTags = [regex]::Matches($index, '<base\b[^>]*>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$expectedBaseTags = [regex]::Matches($index, '<base\s+href="/"\s*/>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

if ($allBaseTags.Count -ne 1 -or $expectedBaseTags.Count -ne 1) {
    throw 'index.html must contain exactly one <base href="/" /> tag before deployment preparation.'
}

$preparedBaseTag = '<base href="{0}" />' -f $BasePath
$preparedIndex = $index.Replace('<base href="/" />', $preparedBaseTag)
if ($preparedIndex -eq $index) {
    throw 'The expected base tag was validated but could not be rewritten with exact casing.'
}

$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($indexPath, $preparedIndex, $utf8WithoutBom)
[System.IO.File]::WriteAllText((Join-Path $publishedRoot '404.html'), $preparedIndex, $utf8WithoutBom)
[System.IO.File]::WriteAllText((Join-Path $publishedRoot '.nojekyll'), '', $utf8WithoutBom)
