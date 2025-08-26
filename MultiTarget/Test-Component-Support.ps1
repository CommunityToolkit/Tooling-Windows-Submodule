<#
.SYNOPSIS
    Tests WinUI support for a specific component and its requested MultiTargets.

.DESCRIPTION
    This script tests WinUI support for a specific component and its requested MultiTargets. It checks if the requested MultiTargets are supported by the component and if the WinUI version is compatible.

.PARAMETER SupportedMultiTargets
    Specifies known supported MultiTargets for the given component. Is retrieved if not specified.

.PARAMETER RequestedMultiTargets
    Specifies the MultiTarget TFM(s) to include for building the components. The default value is 'all'. If not specified, it will use the supported MultiTargets.

.PARAMETER Component
    Specifies the names of the components to build. Defaults to all components.

.PARAMETER WinUIMajorVersion
    Specifies the WinUI major version to use when building for Uno. Also decides the package id and dependency variant.

.EXAMPLE
    Test-Component-Support -RequestedMultiTargets 'uwp', 'wasm' -Component 'MarkdownTextBlock' -WinUIMajorVersion 2

    Tests if the 'MarkdownTextBlock' component supports the 'uwp' and 'wasm' target frameworks with WinUI 2. Returns an object indicating if the component is supported and the reason if not.

.EXAMPLE
    Test-Component-Support -SupportedMultiTargets 'uwp', 'wasm', 'wasdk' -RequestedMultiTargets 'all' -Component 'DataTable' -WinUIMajorVersion 3

    Tests if the 'DataTable' component supports all target frameworks with WinUI 3, using the explicitly provided supported MultiTargets instead of retrieving them.

.NOTES
    Author: Arlo Godfrey
    Date:   6/6/2025
#>
Param (
    [ValidateSet('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
    [Alias("smt")]
    [string[]]$SupportedMultiTargets,

    [ValidateSet('all', 'wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
    [Alias("rmt")]
    [Parameter(Mandatory=$true)]
    [string[]]$RequestedMultiTargets,

    [Alias("c")]
    [Parameter(Mandatory=$true)]
    [string]$Component,

    [Alias("winui")]
    [Parameter(Mandatory=$true)]
    [int]$WinUIMajorVersion
)

if ($RequestedMultiTargets -eq 'all') {
    $RequestedMultiTargets = @('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')
}

# List of WinUI-0 (non-WinUI) compatible multitargets
$WinUI0MultiTargets = @('netstandard')

# List of WinUI-2 compatible multitargets
$WinUI2MultiTargets = @('uwp', 'wasm', 'wpf', 'linuxgtk', 'macos', 'ios', 'android')

# List of WinUI-3 compatible multitargets
$WinUI3MultiTargets = @('wasdk', 'wasm', 'wpf', 'linuxgtk', 'macos', 'ios', 'android')

# If WinUI 0 is requested, the component must not support WinUI 2 or WinUI 3 to be built.
# If WinUI 2 or 3 is requested, the component must have a target that supports WinUI 2 or 3 to be built.
$isWinUI0Supported = $false
$isWinUI2Supported = $false
$isWinUI3Supported = $false

if ($null -ne $SupportedMultiTargets -and $SupportedMultiTargets.Count -gt 0) {
    # If supported MultiTargets are provided, use them directly
    $supportedMultiTargets = $SupportedMultiTargets
} else {
    # If not provided, retrieve the supported MultiTargets for the component
    if ($null -eq $Component) {
        Write-Error "Component name must be specified to retrieve supported MultiTargets."
        exit 1
    }
    $supportedMultiTargets = & $PSScriptRoot\Get-MultiTargets.ps1 -component $Component
}

        
# Flag to check if any of the requested targets are supported by the component
$isRequestedTargetSupported = $false

foreach ($requestedTarget in $RequestedMultiTargets) {
    if ($false -eq $isRequestedTargetSupported) {
        $isRequestedTargetSupported = $requestedTarget -in $supportedMultiTargets
    }
}

foreach ($supportedMultiTarget in $supportedMultiTargets) {
    # Only build components that support WinUI 2
    if ($false -eq $isWinUI2Supported) {
        $isWinUI2Supported = $supportedMultiTarget -in $WinUI2MultiTargets;
    }

    # Only build components that support WinUI 3
    if ($false -eq $isWinUI3Supported) {
        $isWinUI3Supported = $supportedMultiTarget -in $WinUI3MultiTargets;
    }

    # Build components that support neither WinUI 2 nor WinUI 3 (e.g. netstandard only)
    if ($false -eq $isWinUI0Supported) {
        $isWinUI0Supported = $supportedMultiTarget -in $WinUI0MultiTargets -and -not ($isWinUI2Supported -or $isWinUI3Supported);
    }
}

# If none of the requested targets are supported by the component, we can skip build to save time and avoid errors.
if (-not $isRequestedTargetSupported) {
    $IsSupported = $false
    $Reason = "None of the requested MultiTargets '$RequestedMultiTargets' are enabled for this component."
}

if (-not $isWinUI0Supported -and $WinUIMajorVersion -eq 0) {
    $IsSupported = $false
    $Reason = "WinUI is disabled and one of the supported MultiTargets '$supportedMultiTargets' supports WinUI."
}

if ((-not $isWinUI2Supported -and $WinUIMajorVersion -eq 2) -or (-not $isWinUI3Supported -and $WinUIMajorVersion -eq 3)) {
    $IsSupported = $false
    $Reason = "WinUI $WinUIMajorVersion is enabled and not supported by any of the MultiTargets '$supportedMultiTargets'"
}

if ($null -eq $IsSupported) {
    # Default to true if no conditions were met
    $IsSupported = $true
    $Reason = $null
}

return [PSCustomObject]@{
    IsSupported = $IsSupported
    Reason  = $Reason
}