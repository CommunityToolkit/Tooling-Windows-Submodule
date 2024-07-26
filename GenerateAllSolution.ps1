<#
.SYNOPSIS
    Generates the solution file comprising of platform heads for samples, individual component projects, and tests.
.DESCRIPTION
    Used mostly for CI building of everything and testing end-to-end scenarios involving the full
    sample app experience.

    Otherwise it is recommended to focus on an individual component's solution instead.
.PARAMETER IncludeHeads
    List of TFM based projects to include. This can be 'all', 'uwp', or 'wasdk'.

    Defaults to 'all'.
.PARAMETER UseDiagnostics
    Add extra diagnostic output to running slngen, such as a binlog, etc...
.EXAMPLE
    C:\PS> .\GenerateAllSolution -IncludeHeads wasdk
    Build a solution that doesn't contain UWP projects.
.NOTES
    Author: Windows Community Toolkit Labs Team
    Date:   April 27, 2022
#>
Param (
    [Parameter(HelpMessage = "The heads to include for building the sample gallery and tests.", ParameterSetName = "IncludeHeads")]
    [ValidateSet('all', 'wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android')]
    [string[]]$IncludeHeads = @('all'),

    [Parameter(HelpMessage = "Add extra diagnostic output to slngen generator.")]
    [switch]$UseDiagnostics = $false
)

if ($IncludeHeads.Contains('all')) {
    $IncludeHeads = @('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android')
}

# Generate required props for "All" solution.
& ./tooling/MultiTarget/GenerateAllProjectReferences.ps1

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

# TODO: this handles separate project heads, but won't directly handle the unified Skia head from Uno.
# Once we have that, just do a transform on the csproj filename inside this loop to decide the same csproj for those separate MultiTargets.
foreach ($multitarget in $IncludeHeads) {
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
[void]$projects.Add(".\components\**\src\*.csproj")
[void]$projects.Add(".\components\**\samples\*.Samples.csproj")
[void]$projects.Add(".\components\**\tests\*.Tests\*.shproj")

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
