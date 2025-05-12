Param (
  [Parameter(HelpMessage = "The directory where props files for discovered projects should be saved.")]
  [string]$projectPropsOutputDir = "$PSScriptRoot/Generated",

  [Parameter(HelpMessage = "Only projects that support these targets will have references generated for use by deployable heads.")]
  [Alias("mt")]
  [ValidateSet('wasm', 'uwp', 'wasdk', 'wpf', 'linuxgtk', 'macos', 'ios', 'android', 'netstandard')]
  [string[]]$MultiTargets = @("uwp", "wasdk", "wpf", "wasm", "linuxgtk", "macos", "ios", "android", "netstandard"),

  [Parameter(HelpMessage = "The names of the components to generate references for. Defaults to all components.")]
  [string[]]$Components = @("all"),

  [Parameter(HelpMessage = "The names of the components to exclude when generating project references.")]
  [string[]]$ExcludeComponents
)

if ($Components -eq @('all')) {
    $Components = @('**')
}

$preWorkingDir = $pwd;
Set-Location $PSScriptRoot;

# Delete and recreate output folder.
Remove-Item -Path $projectPropsOutputDir -Recurse -Force -ErrorAction SilentlyContinue | Out-Null;
New-Item -ItemType Directory -Force -Path $projectPropsOutputDir -ErrorAction SilentlyContinue | Out-Null;

foreach ($componentName in $Components) {
  if ($ExcludeComponents -contains $componentName) {
    continue;
  }

  # Don't generate project reference if component isn't available
  if (!(Test-Path "$PSScriptRoot/../../components/$componentName/")) {
    continue;
  }

  # Find all components source/sample/test projects (when wildcard), or find specific component projects by component name.
  foreach ($componentPath in Get-Item "$PSScriptRoot/../../components/$componentName/") {
    $componentName = $componentPath.BaseName;
    Write-Output "Generating project references for component $componentName at $componentPath";

    # Find source, sample and test project files
    $componentSourceCsproj = Get-ChildItem $componentPath/src/*.csproj -ErrorAction SilentlyContinue;
    $componentSampleCsproj = Get-ChildItem $componentPath/samples/*.csproj -ErrorAction SilentlyContinue;
    $componentTestProj = Get-ChildItem $componentPath/tests/*.projitems -ErrorAction SilentlyContinue;
    
    # Generate <ProjectReference>s for sample project
    # Use source project MultiTarget as first fallback.
    if ($null -ne $componentSampleCsproj -and (Test-Path $componentSampleCsproj)) {
      & $PSScriptRoot\GenerateMultiTargetAwareProjectReferenceProps.ps1 -projectPath $componentSampleCsproj -outputPath "$projectPropsOutputDir/$($componentName).Samples.props" -MultiTargets $MultiTargets
    }

    if ($null -ne $componentTestProj -and (Test-Path $componentTestProj)) {
      # Generate references for test project
      & $PSScriptRoot\GenerateMultiTargetAwareProjectReferenceProps.ps1 -projectPath $componentTestProj -outputPath "$projectPropsOutputDir/$($componentName).Tests.props" -MultiTargets $MultiTargets
    }

    # Generate references for src project
    & $PSScriptRoot\GenerateMultiTargetAwareProjectReferenceProps.ps1 -projectPath $componentSourceCsproj -outputPath "$projectPropsOutputDir/$($componentName).Source.props" -MultiTargets $MultiTargets
  }
}

Set-Location $preWorkingDir;