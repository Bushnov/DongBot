param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$csprojPath = Join-Path $repoRoot 'DongBot\DongBot.csproj'
$internalChangelogPath = Join-Path $repoRoot 'docs\CHANGELOG_INTERNAL.md'
$userNotesPath = Join-Path $repoRoot 'docs\RELEASE_NOTES_USER.md'
$adminNotesPath = Join-Path $repoRoot 'docs\RELEASE_NOTES_ADMIN.md'
$discordNotesPath = Join-Path $repoRoot 'docs\RELEASE_NOTES_DISCORD.md'

if (-not (Test-Path $csprojPath)) {
    throw "Could not find csproj at $csprojPath"
}

$assemblyVersion = "$Version.0"

[xml]$xml = Get-Content -Path $csprojPath
$propertyGroup = $xml.Project.PropertyGroup | Select-Object -First 1

if (-not $propertyGroup.Version) { $null = $propertyGroup.AppendChild($xml.CreateElement('Version')) }
if (-not $propertyGroup.AssemblyVersion) { $null = $propertyGroup.AppendChild($xml.CreateElement('AssemblyVersion')) }
if (-not $propertyGroup.FileVersion) { $null = $propertyGroup.AppendChild($xml.CreateElement('FileVersion')) }
if (-not $propertyGroup.InformationalVersion) { $null = $propertyGroup.AppendChild($xml.CreateElement('InformationalVersion')) }

$propertyGroup.Version = $Version
$propertyGroup.AssemblyVersion = $assemblyVersion
$propertyGroup.FileVersion = $assemblyVersion
$propertyGroup.InformationalVersion = $Version

$xml.Save($csprojPath)
Write-Host "Updated version metadata in $csprojPath to $Version"

$dateStamp = Get-Date -Format 'yyyy-MM-dd'

function Prepend-ReleaseSection {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Header,

        [Parameter(Mandatory = $true)]
        [string]$Template,

        [Parameter(Mandatory = $true)]
        [string]$RootHeader
    )

    if (-not (Test-Path $Path)) {
        return
    }

    $content = Get-Content -Raw -Path $Path
    if ($content -match [regex]::Escape($Header)) {
        return
    }

    $updated = $content -replace [regex]::Escape($RootHeader), ($RootHeader + "`r`n`r`n" + $Template)
    Set-Content -Path $Path -Value $updated -NoNewline
    Write-Host "Prepended template to $Path"
}

Prepend-ReleaseSection -Path $internalChangelogPath -Header "## $Version - $dateStamp" -RootHeader "# DongBot Internal Changelog" -Template "## $Version - $dateStamp`r`n`r`n### Added`r`n- `r`n`r`n### Changed`r`n- `r`n`r`n### Fixed`r`n- `r`n`r`n"
Prepend-ReleaseSection -Path $userNotesPath -Header "## v$Version ($dateStamp)" -RootHeader "# DongBot User Release Notes" -Template "## v$Version ($dateStamp)`r`n`r`n### Highlights`r`n- `r`n`r`n### Improvements`r`n- `r`n`r`n### Fixes`r`n- `r`n`r`n---`r`n`r`n"
Prepend-ReleaseSection -Path $adminNotesPath -Header "## v$Version ($dateStamp)" -RootHeader "# DongBot Admin Release Notes" -Template "## v$Version ($dateStamp)`r`n`r`n### Operational Changes`r`n- `r`n`r`n### Admin Actions`r`n- `r`n`r`n### Notes`r`n- `r`n`r`n---`r`n`r`n"
Prepend-ReleaseSection -Path $discordNotesPath -Header "## v$Version ($dateStamp)" -RootHeader "# DongBot Discord Release Notes" -Template "## v$Version ($dateStamp)`r`n`r`n### What's New`r`n- `r`n`r`n### Improvements`r`n- `r`n`r`n### Fixes`r`n- `r`n`r`n"

Write-Host "Done. Next: review notes, run dotnet test, then commit."
