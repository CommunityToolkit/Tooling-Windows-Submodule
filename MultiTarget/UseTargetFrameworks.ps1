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

$UwpTfm = "UwpTargetFramework";
$WinAppSdkTfm = "WinAppSdkTargetFramework";
$WasmTfm = "DotnetCommonTargetFramework";
$WpfTfm = "DotnetCommonTargetFramework";
$GtkTfm = "DotnetCommonTargetFramework";
$macOSTfm = "MacOSLibTargetFramework";
$iOSTfm = "iOSLibTargetFramework";
$DroidTfm = "AndroidLibTargetFramework";
$NetstandardTfm = "DotnetStandardCommonTargetFramework";

$fileContents = Get-Content -Path $PSScriptRoot/AvailableTargetFrameworks.props

# 'all' represents many '$MultiTargets' values
if ($MultiTargets.Contains("all")) {
    $MultiTargets = @('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')
}

# Exclude as needed
foreach ($excluded in $ExcludeMultiTargets) {
    $MultiTargets = $MultiTargets.Where({ $_ -ne $excluded });
}

Write-Output "Setting enabled TargetFrameworks: $MultiTargets"

$desiredTfmValues = @();

if ($MultiTargets.Contains("wasm")) {
    $desiredTfmValues += $WasmTfm;
}

if ($MultiTargets.Contains("uwp")) {
    $desiredTfmValues += $UwpTfm;
}

if ($MultiTargets.Contains("wasdk")) {
    $desiredTfmValues += $WinAppSdkTfm;
}

if ($MultiTargets.Contains("wpf")) {
    $desiredTfmValues += $WpfTfm;
}

if ($MultiTargets.Contains("linuxgtk")) {
    $desiredTfmValues += $GtkTfm;
}

if ($MultiTargets.Contains("macos")) {
    $desiredTfmValues += $macOSTfm;
}

if ($MultiTargets.Contains("ios")) {
    $desiredTfmValues += $iOSTfm;
}

if ($MultiTargets.Contains("android")) {
    $desiredTfmValues += $DroidTfm;
}

if ($MultiTargets.Contains("netstandard")) {
    $desiredTfmValues += $NetstandardTfm;
}

$targetFrameworksToRemove = @(
    $WasmTfm,
    $UwpTfm,
    $WinAppSdkTfm,
    $WpfTfm,
    $GtkTfm,
    $macOSTfm,
    $iOSTfm,
    $DroidTfm,
    $NetstandardTfm
).Where({ -not $desiredTfmValues.Contains($_) })

$targetFrameworksToRemoveRegexPartial = $targetFrameworksToRemove -join "|";

$newFileContents = $fileContents -replace "<(?:$targetFrameworksToRemoveRegexPartial)>.+?>", '';

Set-Content -Force -Path $PSScriptRoot/EnabledTargetFrameworks.props -Value $newFileContents;

Write-Output "Done. Please close and regenerate your solution. Do not commit these changes to the tooling repository."
