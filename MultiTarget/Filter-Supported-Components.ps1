<#
.SYNOPSIS
    Given a list of components, filters them based on their support for the specified MultiTarget TFM(s) and WinUI major version.

.DESCRIPTION
    This script checks each component to determine if it supports the specified MultiTarget TFM(s) and WinUI major version.
    It returns a list of components that are supported for the given parameters.

.PARAMETER MultiTargets
    Specifies the MultiTarget TFM(s) to include for building the components.

.PARAMETER WinUIMajorVersion
    Specifies the WinUI major version to use when building for Uno. Also decides the package id and dependency variant.

.NOTES
    Author: Arlo Godfrey
    Date:   6/6/2025
#>
Param (
    [ValidateSet('all', 'wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
    [Alias("mt")]
    [Parameter(Mandatory=$true)]
    [string[]]$MultiTargets,

    [Alias("c")]
    [Parameter(Mandatory=$true)]
    [string[]]$Components,

    [Alias("winui")]
    [Parameter(Mandatory=$true)]
    [int]$WinUIMajorVersion
)

if ($MultiTargets -eq 'all') {
    $MultiTargets = @('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')
}

$supportedComponents = @();

if ($Components -eq @('all')) {
    $Components = @('**')
}

foreach ($ComponentName in $Components) {
    # Find all components source csproj (when wildcard), or find specific component csproj by name.
    $path = "$PSScriptRoot/../../components/$ComponentName/src/*.csproj"
    
    foreach ($componentCsproj in Get-ChildItem -Path $path) {
        # Get component name from csproj path
        $componentPath = Get-Item "$componentCsproj/../../"
        $componentName = $($componentPath.BaseName);

        # Get supported MultiTarget for this component
        $supportedMultiTargets = & $PSScriptRoot\Get-MultiTargets.ps1 -component $componentName
        
        $componentSupportResult = & $PSScriptRoot\Test-Component-Support.ps1 `
            -RequestedMultiTargets $MultiTargets `
            -SupportedMultiTargets $supportedMultiTargets `
            -Component $componentName `
            -WinUIMajorVersion $WinUIMajorVersion

        if ($componentSupportResult.IsSupported -eq $true) {
            $supportedComponents += $componentName
        }
    }
}


return $supportedComponents;