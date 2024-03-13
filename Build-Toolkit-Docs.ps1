Param (
    [Parameter(HelpMessage = "Where to output docs")]
    [string]$OutputDir = "docs"
)

$tocContents = "items:`n"

$componentsRoot = Resolve-Path $PSScriptRoot/../components/

# For each component
foreach ($componentFolder in Get-ChildItem -Path $componentsRoot -Directory) {
  $componentName = $componentFolder.Name

  # Add component to TOC
  $tocContents = $tocContents + "- name: $componentName`n  items:`n"
  
  # Find each markdown file in component's samples folder
  foreach ($markdownFile in Get-ChildItem -Recurse -Path "$componentFolder/samples/**/*.md" | Where-Object {$_.FullName -notlike "*\bin\*" -and $_FullName -notlike "*\obj\*"}) {
    $contents = Get-Content $markdownFile -Raw

    # Find title
    $contents -match 'title:\s*(?<title>.*)' | Out-Null

    $header = $Matches.title
    
    # Find end of YAML
    $endIndex = $contents | Select-String -Pattern "---" -AllMatches | ForEach-Object { $_.Matches[1].Index }

    # Insert Header
    $contents = $contents.Substring(0, $endIndex+5) + "`n# $header`n" + $contents.Substring($endIndex+5)

    # Find Sample Placeholders, replace with code content
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
          $docPath = $(Join-Path "components" $($csfile.FullName.Replace($componentsRoot.Path, ''))).Replace('\', '/').Trim('/')

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
    $mdOutputPath = Join-Path $OutputDir $componentName

    if (-not (Test-Path $mdOutputPath)) {
      New-Item -ItemType Directory -Path $mdOutputPath | Out-Null
    }

    $mdOutputFile = Join-Path $mdOutputPath $markdownFile.Name

    # Write file contents
    Write-Host 'Writing File:', $mdOutputFile
    $contents | Set-Content $mdOutputFile
  
    # Add to TOC
    $mdOutputFile = $mdOutputFile.Trim('/') # need to remove initial / from path

    # TOC is placed within output directory, hrefs are relative to TOC
    $tocHref = $mdOutputFile.Replace($OutputDir, '').Trim('\')
    $tocContents = $tocContents + "  - name: $header`n    href: $tocHref`n"
  }
}

Write-Host 'Writing TOC'
$tocContents | Set-Content (Join-Path $OutputDir "TOC.yml")