Param (
    [Parameter(HelpMessage = "Date of Build/Package")]
    [string]$date,

    [Parameter(HelpMessage = "Any postfix after build number")]
    [string]$postfix
)

foreach ($experimentProjPath in Get-ChildItem -Recurse -Path '../../components/*/src/*.csproj') {
  & msbuild.exe -t:pack /p:Configuration=Release /p:DebugType=Portable /p:DateForVersion=$date /p:PreviewVersion=$postfix $experimentProjPath
}
