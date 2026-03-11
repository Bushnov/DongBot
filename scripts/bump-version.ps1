param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$csprojPath = Join-Path $repoRoot 'DongBot\DongBot.csproj'
$internalChangelogPath = Join-Path $repoRoot 'docs\CHANGELOG_INTERNAL.md'
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

if (Test-Path $internalChangelogPath) {
    $internal = Get-Content -Raw -Path $internalChangelogPath
    $header = "## $Version - $dateStamp"
    if ($internal -notmatch [regex]::Escape($header)) {
        $internal = $internal -replace "# DongBot Internal Changelog\r?\n\r?\n", "# DongBot Internal Changelog`r`n`r`n## $Version - $dateStamp`r`n`r`n### Added`r`n- `r`n`r`n### Changed`r`n- `r`n`r`n### Fixed`r`n- `r`n`r`n"
        Set-Content -Path $internalChangelogPath -Value $internal -NoNewline
        Write-Host "Prepended internal changelog template for $Version"
    }
}

if (Test-Path $discordNotesPath) {
    $discord = Get-Content -Raw -Path $discordNotesPath
    $header = "## v$Version ($dateStamp)"
    if ($discord -notmatch [regex]::Escape($header)) {
        $discord = $discord -replace "# DongBot Discord Release Notes\r?\n\r?\n", "# DongBot Discord Release Notes`r`n`r`n## v$Version ($dateStamp)`r`n`r`n### What's New`r`n- `r`n`r`n### Improvements`r`n- `r`n`r`n### Fixes`r`n- `r`n`r`n"
        Set-Content -Path $discordNotesPath -Value $discord -NoNewline
        Write-Host "Prepended Discord release notes template for v$Version"
    }
}

Write-Host "Done. Next: review notes, run dotnet test, then commit."
