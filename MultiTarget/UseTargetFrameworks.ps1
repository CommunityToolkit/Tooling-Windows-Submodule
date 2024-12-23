<#
.SYNOPSIS
    Changes the target frameworks to build for each package created within the repository.
.DESCRIPTION
    By default, only the UWP, Windows App SDK, and WASM heads are built to simplify dependencies 
    needed to build projects within the repository. The CI will enable all targets to build a package
    that works on all supported platforms.
    
    Note: Projects which rely on target platforms that are excluded will be unable to build.
.PARAMETER MultiTargets
    List of targets to set as TFM platforms to build for. Possible values match those provided to the <MultiTarget> MSBuild property.
    By default, uwp, wasdk, and wasm will included.
.PARAMETER ExcludeMultiTargets
    List to exclude from build. Possible values match those provided to the <MultiTarget> MSBuild property.
    By default, none will excluded.
.EXAMPLE
    C:\PS> .\UseTargetFrameworks wasdk
    Build TFMs for only the Windows App SDK.
.NOTES
    Author: Windows Community Toolkit Labs Team
    Date:   April 8, 2022
#>
Param (
    [Parameter(HelpMessage = "The target frameworks to enable.")]
    [ValidateSet('all', 'wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
    [Alias("mt")]
    [string[]]$MultiTargets = @('uwp', 'wasdk', 'wasm'), # default settings

    [Parameter(HelpMessage = "The target frameworks to disable.")]
    [ValidateSet('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
    [string[]]$ExcludeMultiTargets = @() # default settings
)

$fileContents = Get-Content -Path $PSScriptRoot/EnabledMultiTargets.props
$newFileContents = $fileContents;

$AllMultiTargets = @('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')

# Exclude as needed
foreach ($excluded in $ExcludeMultiTargets) {
    $MultiTargets = $MultiTargets.Where({ $_ -ne $excluded });
    $AllMultiTargets = $AllMultiTargets.Where({ $_ -ne $excluded });
}

Write-Output "Setting enabled MultiTargets: $MultiTargets"

# 'all' represents all available '$MultiTargets' values
if ($MultiTargets.Contains("all")) {
    $enabledMultiTargets = $AllMultiTargets -join ";"
}
else {
    $enabledMultiTargets = $AllMultiTargets.Where({ $MultiTargets.Contains($_) }) -join ";"
}

# When enabledMultiTargetsRegexPartial is empty, the regex will match everything.
# To work around this, check if there's anything to remove before doing it.
if ($enabledMultiTargets.Length -gt 0) {
    $newFileContents = $fileContents -replace "\<EnabledMultiTargets\>(.+?)\<\/EnabledMultiTargets\>", "<EnabledMultiTargets>$enabledMultiTargets</EnabledMultiTargets>";
}

Set-Content -Force -Path $PSScriptRoot/EnabledMultiTargets.props -Value $newFileContents;
Write-Output "Done. Please close and regenerate your solution. Do not commit these changes to the tooling repository."