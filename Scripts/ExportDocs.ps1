Param (
    [Parameter(HelpMessage = "Where to output docs")]
    [string]$OutputDir,

    [Parameter(HelpMessage = "What to name folder for components")]
    [string]$folderName
)

$preWorkingDir = $pwd;
Set-Location $PSScriptRoot;

$OutputDir = Join-Path $preWorkingDir $OutputDir

$lastComponent = ""
$tocContents = "items:`n"

# Find all Markdown documents
foreach ($markdownFile in Get-ChildItem -Recurse -Path '../../components/*/samples/**/*.md' |
          Where-Object {$_.FullName -notlike "*\bin\*" -and $_FullName -notlike "*\obj\*"}) {
  $contents = Get-Content $markdownFile -Raw

  $filePath = $markdownFile.FullName.Substring($preWorkingDir.Path.Length).Replace('\', '/').Trim('/')

  # Get Component Name
  $componentsRoot = $filePath.Replace('components/','')
  $componentName = $componentsRoot.Substring(0, $componentsRoot.IndexOf('/'))
  if ($componentName -ne $lastComponent)
  {
    $tocContents = $tocContents + "- name: $componentName`n  items:`n"
    $lastComponent = $componentName
  }

  # Find title
  $contents -match 'title:\s*(?<title>.*)' | Out-Null

  $header = $Matches.title

  # Find end of YAML
  $endIndex = $contents | Select-String -Pattern "---" -AllMatches | ForEach-Object { $_.Matches[1].Index }

  # Insert Header
  $contents = $contents.Substring(0, $endIndex+5) + "`n# $header`n" + $contents.Substring($endIndex+5)

  # Find Sample Placeholders
  foreach ($sample in ($contents | Select-String -Pattern '>\s*\[!SAMPLE\s*(?<sampleid>.*)\s*\]\s*' -AllMatches).Matches)
  {
    $sampleid = $sample.Groups[1].Value
    $sampleString = $sample.Groups[0].Value

    # Find matching filename for CS
    foreach ($csFile in Get-ChildItem -Recurse -Path ($markdownFile.DirectoryName + '\**\*.xaml.cs').Replace('\', '/') |
              Where-Object {$_.FullName -notlike "*\bin\*" -and $_FullName -notlike "*\obj\*"})
    {
      $csSample = Get-Content $csFile -Raw
      
      if ($csSample -match '\[ToolkitSample\s?\(\s*id:\s*(?:"|nameof\()\s?' + $sampleid + '\s?(?:"|\))')
      {
        # Get Relative Path
        $docPath = $csfile.FullName.Substring($preWorkingDir.Path.Length).Replace('\', '/').Trim('/')

        # See https://learn.microsoft.com/en-us/contribute/content/code-in-docs#out-of-repo-snippet-references
        $snippet = ':::code language="xaml" source="~/../code-windows/' + $docPath.Substring(0, $docPath.Length - 3) + '":::' + "`n`n"

        $snippet = $snippet + ':::code language="csharp" source="~/../code-windows/' + $docPath + '":::' + "`n`n"

        # Replace our Sample Placeholder with references for docs
        $contents = $contents.Replace($sampleString, $snippet)
      }
    }
  }

  # Make any learn links relative
  $contents = $contents.Replace('https://learn.microsoft.com', '')

  # create output directory if it doesn't exist
  $targetFile = $filePath.Replace('components','').Replace('samples','').Replace('//', '/')
  $outputFile = (Join-Path $OutputDir $targetFile)
  [System.IO.Directory]::CreateDirectory((Split-Path $outputFile)) | Out-Null

  # Write file contents
  Write-Host 'Writing File:', $outputFile
  $contents | Set-Content $outputFile

  # Add to TOC
  $targetFile = $targetFile.Trim('/') # need to remove initial / from path
  $tocContents = $tocContents + "  - name: $header`n    href: $targetFile`n"
}

Write-Host 'Writing TOC'
$tocContents | Set-Content (Join-Path $OutputDir "TOC.yml")

Set-Location $preWorkingDir;