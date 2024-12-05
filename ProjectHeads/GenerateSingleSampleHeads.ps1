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

    [string[]]$ExcludeMultiTargets,

    [Alias("winui")]
    [int]$WinUIMajorVersion = 2,
    
    [Parameter(HelpMessage = "The path to the containing folder for a component where sample heads should be generated.")] 
    [string]$componentPath,

    [Parameter(HelpMessage = "Add extra diagnostic output to slngen generator.")]
    [switch]$UseDiagnostics = $false
)

if ($null -ne $Env:Path -and $Env:Path.ToLower().Contains("msbuild") -eq $false) {
    Write-Host
    Write-Host -ForegroundColor Red "Please run from a command window that has MSBuild.exe on the PATH"
    Write-Host
    Write-Host "Press any key to continue"
    [void]$host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

    Exit
}

# If componentPath is not provided or is an empty string, use the $PSScriptRoot
if ($null -eq $componentPath -or $componentPath -eq '')
{
    $componentPath = $pwd
}

# Multitarget handling
# -----------------

if ($MultiTargets.Contains('all')) {
    $MultiTargets = @('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android')
}

if ($null -eq $ExcludeMultiTargets)
{
    $ExcludeMultiTargets = @()
}

# Both uwp and wasdk share a targetframework. Both cannot be enabled at once.
# If both are supplied, remove one based on WinUIMajorVersion.
if ($MultiTargets.Contains('uwp') -and $MultiTargets.Contains('wasdk'))
{
    if ($WinUIMajorVersion -eq 2)
    {
        $ExcludeMultiTargets = $ExcludeMultiTargets + 'wasdk'
    }
    else
    {
        $ExcludeMultiTargets = $ExcludeMultiTargets + 'uwp'
    }
}

$MultiTargets = $MultiTargets | Where-Object { $_ -notin $ExcludeMultiTargets }

# Generate required props for preferences
& $PSScriptRoot/../MultiTarget/UseTargetFrameworks.ps1 -MultiTargets $MultiTargets
& $PSScriptRoot/../MultiTarget/UseUnoWinUI.ps1 $WinUIMajorVersion

# Head generation
# -----------------

$headsFolderName = "heads"
$componentName = (Get-Item $componentPath -ErrorAction Stop).Name

$outputHeadsDir = "$componentPath/$headsFolderName";

# Remove existing heads directory to refresh
Remove-Item -Recurse -Force $outputHeadsDir -ErrorAction SilentlyContinue;

# Intall our heads as a temporary template
dotnet new --install "$PSScriptRoot/SingleComponent" --force

# We need to copy files and run slngen from the target directory path
Push-Location $componentPath

# Copy and rename projects
# Set output folder to 'heads' instead of default
dotnet new ct-tooling-heads -n $componentName -o $headsFolderName

# Remove template, as just for script
dotnet new --uninstall "$PSScriptRoot/SingleComponent"

# Generate Solution
# ------------------

# Projects to include
$projects = [System.Collections.ArrayList]::new()

# Include all projects in component folder
[void]$projects.Add(".\*\*.*proj")

# Install slgnen
dotnet tool restore

$generatedSolutionFilePath = "$componentPath/$componentName.sln"
$platforms = '"Any CPU;x64;x86;ARM64"'
$slngenConfig = "--folders true --collapsefolders true --ignoreMainProject"

# Remove previous file if it exists
if (Test-Path -Path $generatedSolutionFilePath)
{
    Remove-Item $generatedSolutionFilePath
    Write-Host "Removed previous solution file"
}

# Deployable sample gallery heads
# All heads are included by default since they reside in the same folder as the component.
# Remove any heads that are not required for the solution.
# TODO: this handles separate project heads, but won't directly handle the unified Skia head from Uno.
# Once we have that, just do a transform on the csproj filename inside this loop to decide the same csproj for those separate MultiTargets.
foreach ($multitarget in $MultiTargets) {
    # capitalize first letter, avoid case sensitivity issues on linux
    $csprojFileNamePartForMultiTarget = $multitarget.substring(0,1).ToUpper() + $multitarget.Substring(1).ToLower()

    $path = "$outputHeadsDir\**\*$csprojFileNamePartForMultiTarget.csproj";

    if (Test-Path $path) {
        # iterate the wildcards caught by $path
        foreach ($foundItem in Get-ChildItem $path)
        {
            $projects = $projects + $foundItem.FullName
        }
    }
    else {
        Write-Warning "No project head could be found at $path for MultiTarget $multitarget. Skipping."
    }
}

# Include common dependencies required for solution to build
$projects = $projects + "..\..\tooling\CommunityToolkit.App.Shared\**\*.*proj"
$projects = $projects + "..\..\tooling\CommunityToolkit.Tests.Shared\**\*.*proj"
$projects = $projects + "..\..\tooling\CommunityToolkit.Tooling.SampleGen\*.csproj"
$projects = $projects + "..\..\tooling\CommunityToolkit.Tooling.TestGen\*.csproj"
$projects = $projects + "..\..\tooling\CommunityToolkit.Tooling.XamlNamedPropertyRelay\*.csproj"

if ($UseDiagnostics.IsPresent)
{
    $sdkoptions = " -d"
    $diagnostics = '-bl:slngen.binlog --consolelogger:"ShowEventId;Summary;Verbosity=Detailed" --filelogger:"LogFile=slngen.log;Append;Verbosity=Diagnostic;Encoding=UTF-8" '
}
else
{
    $sdkoptions = ""
    $diagnostics = ""
}

$cmd = "dotnet$sdkoptions tool run slngen -o $generatedSolutionFilePath $slngenConfig $diagnostics--platform $platforms $($projects -Join ' ')"

Write-Output "Running Command: $cmd"

Invoke-Expression $cmd

# go back to main working directory
Pop-Location
