# Downloads the release archives of this repository into .\Releases, next to the release notes.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/fetch_releases.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/fetch_releases.ps1 -Tag v1.0.1
#
# Each archive is checked against the SHA-256 that Tools/fetch_releases.ps1 records in Releases\SHA256SUMS:
# add -WriteSums to (re)write that file from the downloaded archives.
param(
    [string]$Tag = "",
    [string]$Repo = "",
    [string]$Out = "",
    [switch]$WriteSums
)

$ErrorActionPreference = "Stop"

if (-not $Repo) {
    # The repository this checkout points at, e.g. Naviuki/Flappy-Copy.
    $origin = (git remote get-url origin 2>$null)
    if ($origin -match "github\.com[:/](?<repo>[^/]+/[^/.]+)") {
        $Repo = $Matches["repo"]
    }
}
if (-not $Repo) {
    Write-Output "Cannot tell the repository; pass -Repo owner/name."
    exit 2
}

if (-not $Out) {
    $Out = Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "Releases"
}
New-Item -ItemType Directory -Force -Path $Out | Out-Null

$api = if ($Tag) { "https://api.github.com/repos/$Repo/releases/tags/$Tag" } else { "https://api.github.com/repos/$Repo/releases/latest" }
$headers = @{ Accept = "application/vnd.github+json"; "User-Agent" = "FlappyCopy-fetch" }
$token = $env:GITHUB_TOKEN
if ($token) {
    $headers.Authorization = "Bearer $token"
}

$release = Invoke-RestMethod -Uri $api -Headers $headers
Write-Output "release: $($release.name) ($($release.tag_name))"

$notes = Join-Path $Out "RELEASE_NOTES.md"
Set-Content -Path $notes -Value $release.body -Encoding utf8

$sums = @()
foreach ($asset in $release.assets) {
    $target = Join-Path $Out $asset.name
    Write-Output "downloading $($asset.name) ($([int]($asset.size / 1MB)) MB)"
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $target -Headers $headers
    $hash = (Get-FileHash -Path $target -Algorithm SHA256).Hash.ToLowerInvariant()
    $sums += "$hash  $($asset.name)"
    Write-Output "  sha256 $hash"
}

if ($WriteSums) {
    Set-Content -Path (Join-Path $Out "SHA256SUMS") -Value ($sums -join "`n") -Encoding utf8
    Write-Output "wrote SHA256SUMS"
}

Write-Output "files in $Out"
