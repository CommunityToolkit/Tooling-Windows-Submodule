Param (
    [Parameter(HelpMessage = "The WinUI version to use when building an Uno head.", Mandatory = $true)]
    [ValidateSet('2', '3')]
    [string]$winUIMajorVersion
)

function ApplyWinUISwap([string] $filePath) {
    $fileContents = Get-Content -Path $filePath;
    
    if ($winUIMajorVersion -eq "3") {
        $fileContents = $fileContents -replace '<WinUIMajorVersion>2</WinUIMajorVersion>', '<WinUIMajorVersion>3</WinUIMajorVersion>';
        $fileContents = $fileContents -replace '<PackageIdVariant>Uwp</PackageIdVariant>', '<PackageIdVariant>WinUI</PackageIdVariant>';
        $fileContents = $fileContents -replace '<DependencyVariant>Uwp</DependencyVariant>', '<DependencyVariant>WinUI</DependencyVariant>';
        $fileContents = $fileContents -replace 'Uno.UI', 'Uno.WinUI';
    }

    if ($winUIMajorVersion -eq "2") {
        $fileContents = $fileContents -replace '<WinUIMajorVersion>3</WinUIMajorVersion>', '<WinUIMajorVersion>2</WinUIMajorVersion>';
        $fileContents = $fileContents -replace '<PackageIdVariant>WinUI</PackageIdVariant>', '<PackageIdVariant>Uwp</PackageIdVariant>';
        $fileContents = $fileContents -replace '<DependencyVariant>WinUI</DependencyVariant>', '<DependencyVariant>Uwp</DependencyVariant>';
        $fileContents = $fileContents -replace 'Uno.WinUI', 'Uno.UI';
    }

    Set-Content -Force -Path $filePath -Value $fileContents;
    Write-Output "Updated $(Resolve-Path -Relative $filePath)"
}

Write-Output "Switching Uno to WinUI $winUIMajorVersion";

ApplyWinUISwap $PSScriptRoot/../ProjectHeads/App.Head.Uno.props
ApplyWinUISwap $PSScriptRoot/PackageReferences/Uno.props
ApplyWinUISwap $PSScriptRoot/WinUI.TargetVersion.props

Write-Output "Done. Please close and regenerate your solution. Do not commit these changes to the tooling repository."