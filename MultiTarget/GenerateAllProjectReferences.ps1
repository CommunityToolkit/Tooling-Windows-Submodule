Param (
  [Parameter(HelpMessage = "The directory where props files for discovered projects should be saved.")]
  [string]$projectPropsOutputDir = "$PSScriptRoot/Generated",

  [Parameter(HelpMessage = "Only projects that support these targets will have references generated for use by deployable heads.")]
  [string[]] $MultiTarget = @("uwp", "wasdk", "wpf", "wasm", "linuxgtk", "macos", "ios", "android", "netstandard")
)

$preWorkingDir = $pwd;
Set-Location $PSScriptRoot;

# Delete and recreate output folder.
Remove-Item -Path $projectPropsOutputDir -Recurse -Force -ErrorAction SilentlyContinue | Out-Null;
New-Item -ItemType Directory -Force -Path $projectPropsOutputDir -ErrorAction SilentlyContinue | Out-Null;

# Discover projects in provided paths
foreach ($projectPath in Get-ChildItem -Directory -Path "$PSScriptRoot/../../components/*") {
  $srcPath = Resolve-Path "$($projectPath.FullName)\src";
  $srcProjectPath = Get-ChildItem -File "$srcPath\*.csproj";
  $sampleProjectPath = Get-ChildItem -File "$($projectPath.FullName)\samples\*.csproj" -ErrorAction SilentlyContinue;

  # Generate <ProjectReference>s for sample project
  # Use source project MultiTarget as first fallback.
  if ($null -ne $sampleProjectPath -and (Test-Path $sampleProjectPath)) {
    & $PSScriptRoot\GenerateMultiTargetAwareProjectReferenceProps.ps1 -projectPath $sampleProjectPath -outputPath "$projectPropsOutputDir/$($sampleProjectPath.BaseName).props" -MultiTarget $MultiTarget
  }

  # Generate <ProjectReference>s for src project
  & $PSScriptRoot\GenerateMultiTargetAwareProjectReferenceProps.ps1 -projectPath $srcProjectPath -outputPath "$projectPropsOutputDir/$($srcProjectPath.BaseName).props" -MultiTarget $MultiTarget
}

Set-Location $preWorkingDir;