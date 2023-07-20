<#
.SYNOPSIS
    Changes the target frameworks to build for each package created within the repository.
.DESCRIPTION
    By default, only the UWP, Windows App SDK, and WASM heads are built to simplify dependencies 
    needed to build projects within the repository. The CI will enable all targets to build a package
    that works on all supported platforms.
    
    Note: Projects which rely on target platforms that are excluded will be unable to build.
.PARAMETER targets
    List of targets to set as TFM platforms to build for. This can also be 'all', 'all-uwp', or blank.
    When run as blank, teh defaults (uwp, winappsdk, wasm) will be used.
    'all' and 'all-uwp' shouldn't be used with other targets or each other.
.PARAMETER allowGitChanges
    Enabling this flag will allow changes to the props file to be checked into source control.
    By default the file is ignored so local changes to building don't accidently get checked in.
.EXAMPLE
    C:\PS> .\UseTargetFrameworks winappsdk
    Build targets for just the WindowsAppSDK.
.NOTES
    Author: Windows Community Toolkit Labs Team
    Date:   April 8, 2022
#>
Param (
    [Parameter(HelpMessage = "The target frameworks to enable.")]
    [ValidateSet('all', 'all-uwp', 'wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
    [string[]]$targets = @('uwp', 'winappsdk', 'wasm') # default settings
)

$UwpTfm = "UwpTargetFramework";
$WinAppSdkTfm = "WinAppSdkTargetFramework";
$WasmTfm = "NetCoreCommonTargetFramework";
$WpfTfm = "NetCoreCommonTargetFramework";
$GtkTfm = "NetCoreCommonTargetFramework";
$macOSTfm = "MacOSLibTargetFramework";
$iOSTfm = "iOSLibTargetFramework";
$DroidTfm = "AndroidLibTargetFramework";
$NetstandardTfm = "NetStandardCommonTargetFramework";

$fileContents = Get-Content -Path $PSScriptRoot/AvailableTargetFrameworks.props

$allTargetFrameworks = @(
    $WasmTfm,
    $UwpTfm,
    $WinAppSdkTfm,
    $WpfTfm,
    $GtkTfm,
    $macOSTfm,
    $iOSTfm,
    $DroidTfm,
    $NetstandardTfm
);

$desiredTfmValues = @();

if ($targets.Contains("all")) {
    $desiredTfmValues = $allTargetFrameworks;
}

if ($targets.Contains("all-uwp")) {
    $desiredTfmValues = $allTargetFrameworks.Replace($UwpTfm, "");
}

if ($targets.Contains("wasm")) {
    $desiredTfmValues += $WasmTfm;
}

if ($targets.Contains("uwp")) {
    $desiredTfmValues += $UwpTfm;
}

if ($targets.Contains("wasdk")) {
    $desiredTfmValues += $WinAppSdkTfm;
}

if ($targets.Contains("wpf")) {
    $desiredTfmValues += $WpfTfm;
}

if ($targets.Contains("linuxgtk")) {
    $desiredTfmValues += $GtkTfm;
}

if ($targets.Contains("macos")) {
    $desiredTfmValues += $macOSTfm;
}

if ($targets.Contains("ios")) {
    $desiredTfmValues += $iOSTfm;
}

if ($targets.Contains("android")) {
    $desiredTfmValues += $DroidTfm;
}

if ($targets.Contains("netstandard")) {
    $desiredTfmValues += $NetstandardTfm;
}

Write-Output "Setting enabled TargetFrameworks: $targets"

$targetFrameworksToRemove = $allTargetFrameworks.Where({ -not $desiredTfmValues.Contains($_) })

$targetFrameworksToRemoveRegexPartial = $targetFrameworksToRemove -join "|";

$newFileContents = $fileContents -replace "<(?:$targetFrameworksToRemoveRegexPartial)>.+?>", '';

Set-Content -Force -Path $PSScriptRoot/EnabledTargetFrameworks.props -Value $newFileContents;

Write-Output "Done. Please close and regenerate your solution. Do not commit these changes to the tooling repository."
