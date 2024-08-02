Param (    
    [Parameter(HelpMessage = "The full path of the csproj to generated references to.", Mandatory = $true)] 
    [string]$projectPath,

    [Parameter(HelpMessage = "A path to a .props file where generated content should be saved to.", Mandatory = $true)] 
    [string]$outputPath,

    [Parameter(HelpMessage = "The path to the template used to generate the props file.")]
    [string]$templatePath = "$PSScriptRoot/MultiTargetAwareProjectReference.props.template",

    [Parameter(HelpMessage = "The placeholder text to replace when inserting the project file name into the template.")] 
    [string]$projectFileNamePlaceholder = "[ProjectFileName]",

    [Parameter(HelpMessage = "The placeholder text to replace when inserting the project path into the template.")] 
    [string]$projectRootPlaceholder = "[ProjectRoot]",

    [Parameter(HelpMessage = "Only projects that support these targets will have references generated for use by deployable heads.")]
    [ValidateSet("uwp", "wasdk", "wpf", "wasm", "linuxgtk", "macos", "ios", "android", "netstandard")]
    [Alias("mt")]
    [string[]] $MultiTargets = @("uwp", "wasdk", "wpf", "wasm", "linuxgtk", "macos", "ios", "android", "netstandard")
)

$templateContents = Get-Content -Path $templatePath;

$preWorkingDir = $pwd;
Set-Location "$PSScriptRoot/../../"

$relativeProjectPath = Resolve-Path -Relative -Path $projectPath

Set-Location $preWorkingDir;

# Insert csproj file name.
$csprojFileName = [System.IO.Path]::GetFileName($projectPath);
$templateContents = $templateContents -replace [regex]::escape($projectFileNamePlaceholder), $csprojFileName;

# Insert component directory
$componentDirectoryRelativeToRoot = [System.IO.Path]::GetDirectoryName($relativeProjectPath).TrimStart('.').TrimStart('\');
$templateContents = $templateContents -replace [regex]::escape($projectRootPlaceholder), "$componentDirectoryRelativeToRoot";

# Get component name from project path
$componentPath = Get-Item "$projectPath/../../"

# Load multitarget preferences for component
$multiTargetPrefs = & $PSScriptRoot\Get-MultiTargets.ps1 -component $($componentPath.BaseName)

if ($null -eq $multiTargetPrefs) {
    Write-Error "Couldn't get MultiTarget property for $componentPath";
    exit(-1);
}

# Ensure multiTargetPrefs is not empty
if ($multiTargetPrefs.Length -eq 0) {
    Write-Error "MultiTarget property is empty for $projectPath";
    exit(-1);
}

$templateContents = $templateContents -replace [regex]::escape("[IntendedTargets]"), $multiTargetPrefs;

function ShouldMultiTarget([string] $target) {
    return ($multiTargetPrefs.Contains($target) -and $MultiTargets.Contains($target))
}

function ShouldMultiTargetMsBuildValue([string] $target) {
    return $(ShouldMultiTarget $target).ToString().ToLower()
}

$targeted = @("uwp", "wasdk", "wpf", "wasm", "linuxgtk", "macos", "ios", "android", "netstandard").Where({ ShouldMultiTarget $_ })

if ($targeted.Count -gt 0) {
    Write-Host "Generating project references for $([System.IO.Path]::GetFileNameWithoutExtension($csprojFileName)): $($targeted -Join ', ')"
}

$templateContents = $templateContents -replace [regex]::escape("[CanTargetWasm]"), "'$(ShouldMultiTargetMsBuildValue "wasm")'";
$templateContents = $templateContents -replace [regex]::escape("[CanTargetUwp]"), "'$(ShouldMultiTargetMsBuildValue "uwp")'";
$templateContents = $templateContents -replace [regex]::escape("[CanTargetWasdk]"), "'$(ShouldMultiTargetMsBuildValue "wasdk")'";
$templateContents = $templateContents -replace [regex]::escape("[CanTargetWpf]"), "'$(ShouldMultiTargetMsBuildValue "wpf")'";
$templateContents = $templateContents -replace [regex]::escape("[CanTargetLinuxGtk]"), "'$(ShouldMultiTargetMsBuildValue "linuxgtk")'";
$templateContents = $templateContents -replace [regex]::escape("[CanTargetMacOS]"), "'$(ShouldMultiTargetMsBuildValue "macos")'";
$templateContents = $templateContents -replace [regex]::escape("[CanTargetiOS]"), "'$(ShouldMultiTargetMsBuildValue "ios")'";
$templateContents = $templateContents -replace [regex]::escape("[CanTargetDroid]"), "'$(ShouldMultiTargetMsBuildValue "android")'";
$templateContents = $templateContents -replace [regex]::escape("[CanTargetNetstandard]"), "'$(ShouldMultiTargetMsBuildValue "netstandard")'";

# Save to disk
Set-Content -Path $outputPath -Value $templateContents;
