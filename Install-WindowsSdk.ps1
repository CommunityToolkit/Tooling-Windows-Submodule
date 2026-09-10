$ErrorActionPreference = "Stop"

$WinSdkTempDir = "C:\WinSdkTemp\"
$WinSdkSetupExe = "C:\WinSdkTemp\" + "WinSdkSetup.exe"

New-Item -ItemType Directory -Path $WinSdkTempDir -Force | Out-Null

$client = [System.Net.WebClient]::new()
$client.DownloadFile("https://go.microsoft.com/fwlink/?linkid=2311805", $WinSdkSetupExe)

$installer = Start-Process -Wait -PassThru $WinSdkSetupExe "/features OptionId.UWPManaged OptionId.UWPCpp /q /norestart"
if ($installer.ExitCode -notin @(0, 3010)) {
    throw "Windows SDK 19041 installation failed with exit code $($installer.ExitCode)."
}