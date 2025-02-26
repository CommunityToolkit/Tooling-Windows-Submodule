<#
.SYNOPSIS
    Uses the dotnet template tool to copy and rename project heads to run sample code for different platforms.

.DESCRIPTION
    This is used to centralize configuration and reduce duplication of copying these heads for every project.
    This script also generates a solution for the project and will open Visual Studio.

.PARAMETER componentPath
    Folder for the project to copy the project heads to.

.PARAMETER MultiTargets
    Specifies the MultiTarget TFM(s) to include for building the components. The default value is 'uwp', 'wasm', 'wasdk'.

.PARAMETER ExcludeMultiTargets
    Specifies the MultiTarget TFM(s) to exclude for building the components. The default value excludes targets that require additional tooling or workloads to build. Run uno-check to install the required workloads.

.PARAMETER WinUIMajorVersion
  Specifies the WinUI major version to use when building an Uno head. Also decides the package id and dependency variant. The default value is '2'.

.PARAMETER UseDiagnostics
    Add extra diagnostic output to running slngen, such as a binlog, etc...

.EXAMPLE
    C:\PS> .\GenerateSingleSampleHeads -componentPath components\testproj
    Builds project heads for component in testproj directory.

.NOTES
    Author: Windows Community Toolkit Labs Team
    Date:   Feb 9, 2023
#>
Param (
    [ValidateSet('all', 'wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android')]
    [Alias("mt")]
    [string[]]$MultiTargets = @('uwp', 'wasm', 'wasdk'),

    [string[]]$ExcludeMultiTargets = @(),

    [Alias("winui")]
    [int]$WinUIMajorVersion = 2,
    
    [Parameter(HelpMessage = "The path to the containing folder for a component where sample heads should be generated.")] 
    [string]$componentPath,

    [Parameter(HelpMessage = "Add extra diagnostic output to slngen generator.")]
    [switch]$UseDiagnostics = $false
)

# Use & and a separate script path variable to avoid issues with parameter passing
$scriptPath = "$PSScriptRoot/../GenerateSingleSolution.ps1"
& $scriptPath -MultiTargets $MultiTargets -ExcludeMultiTargets $ExcludeMultiTargets -WinUIMajorVersion $WinUIMajorVersion -UseDiagnostics:$UseDiagnostics -componentPath $componentPath