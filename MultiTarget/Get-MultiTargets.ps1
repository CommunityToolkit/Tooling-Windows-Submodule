<#
.SYNOPSIS
    Retrieves the MultiTarget properties for a component.

.DESCRIPTION
    This script retrieves the MultiTarget properties for a component by parsing the contents of the MultiTarget.props file.
    It replaces placeholders in a template with the component-specific information, such as the csproj file name and component directory.

.PARAMETER component
    The name of the component to retrieve MultiTarget properties from.

.OUTPUTS
    An array of MultiTarget properties.

.EXAMPLE
    Get-MultiTarget -component "MyComponent"
    Retrieves the MultiTarget properties for the specified component.
#>
param (
    [Parameter(Mandatory=$true)]
    [string]$component
)

# Load multitarget preferences for component
# Folder layout is expected to match the Community Toolkit.
$componentPath = "$PSScriptRoot/../../components/$component";

$srcPath = Resolve-Path "$componentPath\src";
$samplePath = "$componentPath\samples";

# Uses the <MultiTarget> values from the source library component as the fallback for the sample component.
# This behavior also implemented in TargetFramework evaluation. 
$multiTargetFallbackPropsPaths += @("$samplePath/MultiTarget.props", "$srcPath/MultiTarget.props", "$PSScriptRoot/Defaults.props")

# Load first available default
$fileContents = "";
foreach ($fallbackPath in $multiTargetFallbackPropsPaths) {
    if (Test-Path $fallbackPath) {
        $fileContents = Get-Content $fallbackPath -ErrorAction Stop;
        break;
    }
}

# Parse file contents
$regex = Select-String -Pattern '<MultiTarget>(.+?)<\/MultiTarget>' -InputObject $fileContents;

if ($null -eq $regex -or $null -eq $regex.Matches -or $null -eq $regex.Matches.Groups -or $regex.Matches.Groups.Length -lt 2) {
    Write-Error "Couldn't get MultiTarget property from $path";
    exit(-1);
}

$multiTargets = $regex.Matches.Groups[1].Value;
return $multiTargets.Split(';');