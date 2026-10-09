<#
.SYNOPSIS
  Builds the Toolkit Gallery with specified parameters. Primarily used by maintainers for local testing.

.DESCRIPTION
  The Build-Toolkit-Gallery function is used to build the Community Toolkit Gallery app with customizable parameters. It allows you to specify the MultiTarget TFM, include heads, enable binlogs, additional msbuild properties, pick the components to build, and exclude specific components.

.PARAMETER MultiTargets
    Specifies the MultiTarget TFM(s) to include for building the components. The default value is 'all'.

.PARAMETER ExcludeMultiTargets
    Specifies the MultiTarget TFM(s) to exclude for building the components. The default value excludes targets that require additional tooling or workloads to build: 'wpf', 'linux', 'macos', 'ios', and 'android'. Run uno-check to install the required workloads.

.PARAMETER Heads
  The heads to include in the build. Default is 'Uwp', 'Wasdk', 'Wasm'.

.PARAMETER ExcludeHeads
  The heads to exclude from the build. Default is none.

.PARAMETER Components
  The names of the components to build. Defaults to all components.

.PARAMETER ExcludeComponents
  The names of the components to exclude from the build. Defaults to none.

.PARAMETER BinlogOutput
  Specifies the output directory for binlogs. This parameter is optional, default is the current directory.

.PARAMETER EnableBinLogs
  Enables the generation of binlogs by appending '/bl' to the msbuild command. Generated binlogs will match the csproj name. This parameter is optional. Use BinlogOutput to specify the output directory.

.PARAMETER WinUIMajorVersion
  Specifies the WinUI major version to use when building an Uno head. Also decides the package id and dependency variant. The default value is '2'.

.PARAMETER AdditionalProperties
  Additional msbuild properties to pass.

.PARAMETER Release
  Specifies whether to build in Release configuration. Default is false.

.PARAMETER Verbose
  Specifies whether to enable detailed msbuild verbosity. Default is false.

.NOTES
  Author: Arlo Godfrey
  Date:   2/19/2024
#>
Param (
  [ValidateSet('all', 'wasm', 'uwp', 'wasdk', 'wpf', 'win32', 'linux', 'macos', 'ios', 'android', 'netstandard')]
  [Alias("mt")]
  [string[]]$MultiTargets = @('uwp', 'wasdk', 'wasm'), # default settings

  [ValidateSet('wasm', 'uwp', 'wasdk', 'wpf', 'win32', 'linux', 'macos', 'ios', 'android', 'netstandard')]
  [string[]]$ExcludeMultiTargets = @(), # default settings

  [ValidateSet('all', 'Uwp', 'Wasdk', 'Wasm', 'Uno', 'Tests.Uwp', 'Tests.Wasdk')]
  [string[]]$Heads = @('Uwp', 'Wasdk', 'Wasm'),

  [ValidateSet('Uwp', 'Wasdk', 'Wasm', 'Uno', 'Tests.Uwp', 'Tests.Wasdk')]
  [string[]]$ExcludeHeads,

  [Alias("bl")]
  [switch]$EnableBinLogs,

  [Alias("blo")]
  [string]$BinlogOutput,

  [Alias("p")]
  [hashtable]$AdditionalProperties,

  [Alias("winui")]
  [int]$WinUIMajorVersion = 3,

  [Alias("c")]
  [string[]]$Components = @("all"),

  [string[]]$ExcludeComponents,

  [switch]$Release,

  [Alias("v")]
  [switch]$Verbose
)

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

if ($MultiTargets -eq 'all') {
  $MultiTargets = @('wasm', 'uwp', 'wasdk', 'wpf', 'win32', 'linux', 'macos', 'ios', 'android', 'netstandard')
}

if ($ExcludeMultiTargets) {
  $MultiTargets = $MultiTargets | Where-Object { $_ -notin $ExcludeMultiTargets }
}

if ($ExcludeComponents) {
    $Components = $Components | Where-Object { $_ -notin $ExcludeComponents }
}

# Certain ProjectReferences should always be generated (are required to build gallery) if csproj is available.
if ($Components -notcontains 'SettingsControls') {
  $Components += 'SettingsControls'
}

if ($Components -notcontains 'Converters') {
  $Components += 'Converters'
}

# Use the specified MultiTarget TFM and WinUI version
& $PSScriptRoot\MultiTarget\UseTargetFrameworks.ps1 $MultiTargets
& $PSScriptRoot\MultiTarget\UseUnoWinUI.ps1 $WinUIMajorVersion

# Generate gallery references to components
# Components built are selected via references from gallery head.
& $PSScriptRoot\MultiTarget\GenerateAllProjectReferences.ps1 -MultiTarget $MultiTargets -Components $Components

if ($Heads -eq 'all') {
  $Heads = @('Uwp', 'Wasdk', 'Wasm', 'Uno', 'Tests.Uwp', 'Tests.Wasdk')
}

function Invoke-MSBuildWithBinlog {
  param (
    [string]$TargetHeadPath,
    [string]$TargetFramework
  )

  # Reset build args to default
  $msbuildArgs = @("-r", "-m", "-t:Clean,Build")

  if ($TargetFramework) {
    # dotnet build's "-f" does a scoped inner-build restore for just that TFM; plain
    # "/p:TargetFramework=" on a multi-targeted project restores the outer project's full TFM
    # closure first, which fails for components that don't support every TFM the head declares.
    # msbuild.exe has no "-f" equivalent, so this only reliably supports one TFM at a time there.
    if ($($PSVersionTable.Platform) -eq "Unix") {
      $msbuildArgs += "-f:$TargetFramework"
    }
    else {
      $msbuildArgs += "/p:TargetFramework=$TargetFramework"
    }
  }

  # Add additional properties to the msbuild arguments
  if ($AdditionalProperties) {
    foreach ($property in $AdditionalProperties.GetEnumerator()) {
      $msbuildArgs += "/p:$($property.Name)=$($property.Value)"
    }
  }

  # Handle binlog options
  if ($EnableBinLogs) {
    # Get binlog filename and output path
    $csprojFileName = [System.IO.Path]::GetFileNameWithoutExtension($TargetHeadPath)
    $binlogSuffix = $TargetFramework ? ".$TargetFramework" : ""
    $defaultBinlogFilename = "$csprojFileName$binlogSuffix.msbuild.binlog"
    $finalBinlogPath = $defaultBinlogFilename;

    # Set default binlog output location if not provided
    if ($BinlogOutput) {
      $finalBinlogPath = "$BinlogOutput/$defaultBinlogFilename"
    }

    $msbuildArgs += "/bl:$finalBinlogPath"
  }

  if ($Release) {
    $msbuildArgs += "/p:Configuration=Release"
  }

  if ($Verbose) {
    $msbuildArgs += "/verbosity:detailed"
  }

  # On Linux there's no msbuild.exe on PATH; dotnet build is the only option there anyway.
  if ($($PSVersionTable.Platform) -eq "Unix") {
    dotnet build $msbuildArgs $TargetHeadPath
  }
  else {
    msbuild $msbuildArgs $TargetHeadPath
  }
}

# The Uno.Sdk head covers several MultiTargets from one multi-TFM csproj (see
# ProjectHeads/AllComponents/Uno/CommunityToolkit.App.Uno.csproj). Building all of its enabled TFMs
# in one invocation isn't reliable across every combination, so - matching what CI already does -
# build it one TFM at a time via `-f`, only for the TFMs the requested $MultiTargets actually enable.
function Get-UnoSdkHeadTargetFrameworks {
  param (
    [string[]]$MultiTargets
  )

  $targetFrameworks = [System.Collections.ArrayList]::new()

  if (($MultiTargets | Where-Object { @('win32', 'linux', 'macos') -contains $_ }).Count -gt 0) {
    [void]$targetFrameworks.Add('net9.0-desktop')
  }
  if ($MultiTargets -contains 'wasm') {
    [void]$targetFrameworks.Add('net9.0-browserwasm')
  }
  if ($MultiTargets -contains 'android') {
    [void]$targetFrameworks.Add('net9.0-android')
  }
  if ($MultiTargets -contains 'ios') {
    [void]$targetFrameworks.Add('net9.0-ios')
  }

  return $targetFrameworks
}

foreach ($head in $Heads) {
  if ($ExcludeHeads -and $head -in $ExcludeHeads) {
    continue
  }

  $targetHeadPath = Get-ChildItem "$PSScriptRoot/ProjectHeads/AllComponents/$head/*.csproj"

  if ($head -eq 'Uno') {
    $unoTargetFrameworks = Get-UnoSdkHeadTargetFrameworks -MultiTargets $MultiTargets

    if ($unoTargetFrameworks.Count -eq 0) {
      Write-Warning "None of the requested MultiTargets ($MultiTargets) are served by the Uno.Sdk head. Skipping."
      continue
    }

    foreach ($targetFramework in $unoTargetFrameworks) {
      Invoke-MSBuildWithBinlog $targetHeadPath $targetFramework
    }
  }
  else {
    Invoke-MSBuildWithBinlog $targetHeadPath
  }
}
