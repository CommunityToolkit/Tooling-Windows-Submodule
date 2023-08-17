Param (
    [Parameter(HelpMessage = "Extra properties to pass to the msbuild pack command")]
    [string]$extraBuildProperties
)

foreach ($experimentProjPath in Get-ChildItem -Recurse -Path '../../components/*/src/*.csproj') {
  & msbuild.exe -t:pack /p:Configuration=Release /p:DebugType=Portable $experimentProjPath $extraBuildProperties
}
