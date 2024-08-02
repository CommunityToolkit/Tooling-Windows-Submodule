<#
.SYNOPSIS
    Generates the solution file comprising of platform heads for samples, individual component projects, and tests.

.DESCRIPTION
    Used mostly for CI building of everything and testing end-to-end scenarios involving the full
    sample app experience.

    Otherwise it is recommended to focus on an individual component's solution instead.

.PARAMETER MultiTargets
    Specifies the MultiTarget TFM(s) to include for building the components. The default value is 'all'.

.PARAMETER ExcludeMultiTargets
    Specifies the MultiTarget TFM(s) to exclude for building the components. The default value excludes targets that require additional tooling or workloads to build. Run uno-check to install the required workloads.

.PARAMETER Components
    The names of the components to generate project and solution references for. Defaults to all components.

.PARAMETER ExcludeComponents
    The names of the components to exclude when generating solution and project references. Defaults to none.

.PARAMETER WinUIMajorVersion
  Specifies the WinUI major version to use when building an Uno head. Also decides the package id and dependency variant. The default value is '2'.

.PARAMETER UseDiagnostics
    Add extra diagnostic output to running slngen, such as a binlog, etc...

.EXAMPLE
    C:\PS> .\GenerateAllSolution -MultiTargets wasdk
    Build a solution that doesn't contain UWP projects.

.NOTES
    Author: Windows Community Toolkit Labs Team
    Date:   April 27, 2022
#>
Param (
    [ValidateSet('all', 'wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android')]
    [Alias("mt")]
    [string[]]$MultiTargets = @('uwp', 'wasm', 'wasdk'),

    [ValidateSet('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
    [string[]]$ExcludeMultiTargets = @(), # default settings

    [Alias("c")]
    [string[]]$Components = @('all'),

    [Alias("winui")]
    [int]$WinUIMajorVersion = 2,

    [string[]]$ExcludeComponents,

    [switch]$UseDiagnostics = $false
)

if ($MultiTargets.Contains('all')) {
    $MultiTargets = @('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android')
}

if ($null -eq $ExcludeMultiTargets)
{
    $ExcludeMultiTargets = @()
}

$MultiTargets = $MultiTargets | Where-Object { $_ -notin $ExcludeMultiTargets }

# Generate required props for "All" solution.
& ./tooling/MultiTarget/GenerateAllProjectReferences.ps1 -MultiTargets $MultiTargets -Components $Components -ExcludeComponents $ExcludeComponents
& ./tooling/MultiTarget/UseTargetFrameworks.ps1 -MultiTargets $MultiTargets
& ./tooling/MultiTarget/UseUnoWinUI.ps1 $WinUIMajorVersion

# Set up constant values
$generatedSolutionFilePath = 'CommunityToolkit.AllComponents.sln'
$platforms = 'Any CPU;x64;x86;ARM64'
$slngenConfig = @(
    '--folders'
    'true'
    '--collapsefolders'
    'true'
    '--ignoreMainProject'
)

# Remove previous file if it exists
if (Test-Path -Path $generatedSolutionFilePath)
{
    Remove-Item $generatedSolutionFilePath
    Write-Host "Removed previous solution file"
}

# Projects to include
$projects = [System.Collections.ArrayList]::new()

# Common/Dependencies for shared infrastructure
[void]$projects.Add(".\tooling\CommunityToolkit*\*.*proj")

# Deployable sample gallery heads 
# TODO: this handles separate project heads, but won't directly handle the unified Skia head from Uno.
# Once we have that, just do a transform on the csproj filename inside this loop to decide the same csproj for those separate MultiTargets.
foreach ($multitarget in $MultiTargets) {
    # capitalize first letter, avoid case sensitivity issues on linux
    $csprojFileNamePartForMultiTarget = $multitarget.substring(0,1).ToUpper() + $multitarget.Substring(1).ToLower()

    $path = ".\tooling\ProjectHeads\AllComponents\**\*.$csprojFileNamePartForMultiTarget.csproj";

    if (Test-Path $path) {
        [void]$projects.Add($path)
    }
    else {
        Write-Warning "No project head could be found at $path for MultiTarget $multitarget. Skipping."
    }
}

# Individual projects
if ($Components -eq @('all')) {
    $Components = @('**')
}

foreach ($componentName in $Components) {
    if ($ExcludeComponents -contains $componentName) {
        continue;
    }
    
    foreach ($componentPath in Get-Item "$PSScriptRoot/../components/$componentName/") {
        $multiTargetPrefs = & $PSScriptRoot\MultiTarget\Get-MultiTargets.ps1 -component $($componentPath.BaseName)

        $shouldReferenceInSolution = $multiTargetPrefs.Where({ $MultiTargets.Contains($_) }).Count -gt 0

        if ($shouldReferenceInSolution) {
            Write-Output "Add component $componentPath to solution";

            [void]$projects.Add(".\components\$($componentPath.BaseName)\src\*.csproj")
            [void]$projects.Add(".\components\$($componentPath.BaseName)\samples\*.Samples.csproj")
            [void]$projects.Add(".\components\$($componentPath.BaseName)\tests\*.Tests\*.shproj")
        } else {
            Write-Warning "Component $($componentPath.BaseName) doesn't MultiTarget any of $MultiTargets and won't be added to the solution.";
        }
    }
}

if ($UseDiagnostics.IsPresent)
{
    $sdkoptions = "-d"
    $diagnostics = @(
        '-bl:slngen.binlog'
        '--consolelogger:ShowEventId;Summary;Verbosity=Detailed'
    )
}
else
{
    $sdkoptions = ""
    $diagnostics = ""
}

# See https://learn.microsoft.com/en-us/powershell/scripting/learn/experimental-features?view=powershell-7.4#psnativecommandargumentpassing
$PSNativeCommandArgumentPassing = 'Legacy'

$cmd = 'dotnet'
$arguments = @(
    $sdkoptions
    'tool'
    'run'
    'slngen'
    '-o'
    $generatedSolutionFilePath
    $slngenConfig
    $diagnostics
    '--platform'
    $platforms
    $projects
)

&$cmd @arguments
