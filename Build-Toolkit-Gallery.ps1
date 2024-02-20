<#
.SYNOPSIS
  Builds the Toolkit Gallery with specified parameters. Primarily used by maintainers for local testing.

.DESCRIPTION
  The Build-Toolkit-Gallery function is used to build the Community Toolkit Gallery app with customizable parameters. It allows you to specify the MultiTarget TFM, include heads, enable binlogs, additional msbuild properties, pick the components to build, and exclude specific components.

.PARAMETER MultiTargets
    Specifies the MultiTarget TFM(s) to include for building the components. The default value is 'all'.

.PARAMETER ExcludeMultiTargets
    Specifies the MultiTarget TFM(s) to exclude for building the components. The default value excludes targets that require additional tooling or workloads to build: 'wpf', 'linuxgtk', 'macos', 'ios', and 'android'. Run uno-check to install the required workloads.

.PARAMETER Heads
  The heads to include in the build. Default is 'Uwp', 'WinAppSdk', 'Wasm'.

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
  [ValidateSet('all', 'wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
  [string[]]$MultiTargets = @('all'),
  
  [ValidateSet('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
  [string[]]$ExcludeMultiTargets = @('wpf', 'linuxgtk', 'macos', 'ios', 'android'),

  [ValidateSet('all', 'Uwp', 'WinAppSdk', 'Wasm', 'Tests.Uwp', 'Tests.WinAppSdk')]
  [string[]]$Heads = @('Uwp', 'WinAppSdk', 'Wasm'),

  [ValidateSet('Uwp', 'WinAppSdk', 'Wasm', 'Tests.Uwp', 'Tests.WinAppSdk')]
  [string[]]$ExcludeHeads,

  [Alias("bl")]
  [switch]$EnableBinLogs,

  [string]$BinlogOutput,

  [hashtable]$AdditionalProperties,

  [int]$WinUIMajorVersion = 2,

  [string[]]$Components = @("all"),

  [string[]]$ExcludeComponents,

  [switch]$Release,

  [Alias("v")]
  [switch]$Verbose
)

if ($ExcludeMultiTargets -eq $null)
{
  $ExcludeMultiTargets = @()
}

if ($MultiTargets -eq 'all') {
  $MultiTargets = @('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')
}

if ($ExcludeMultiTargets) {
  $MultiTargets = $MultiTargets | Where-Object { $_ -notin $ExcludeMultiTargets }
}

if ($ExcludeComponents) {
    $Components = $Components | Where-Object { $_ -notin $ExcludeComponents }
}

# Certain Components are required to build the gallery app.
# Add them if not already included.
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
  $Heads = @('Uwp', 'WinAppSdk', 'Wasm', 'Tests.Uwp', 'Tests.WinAppSdk')
}

function Invoke-MSBuildWithBinlog {
  param (
    [string]$TargetHeadPath,
    [switch]$EnableBinLogs,
    [string]$BinlogOutput
  )

  # Reset build args to default
  $msbuildArgs = @("-r", "-m", "-t:Clean,Build")

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
    $defaultBinlogFilename = "$csprojFileName.msbuild.binlog"
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

  msbuild $msbuildArgs $TargetHeadPath
}

foreach ($head in $Heads) {
  if ($ExcludeHeads -and $head -in $ExcludeHeads) {
    continue
  }

  $targetHeadPath = Get-ChildItem "$PSScriptRoot/ProjectHeads/AllComponents/$head/*.csproj"

  Invoke-MSBuildWithBinlog $targetHeadPath $EnableBinLogs $BinlogOutput
}
