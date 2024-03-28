Param (
  [Parameter(HelpMessage = "Where to output docs")]
  [string]$OutputDir = "docs"
)

$tocContents = "items:`n"

function AppendTocItem([string] $name, [int] $level, [hashtable] $properties) {
  $indent = "  " * $level
  $firstLineIndent = "  " * ($level - 1)

  $lines = "$firstLineIndent- name: $name`n"

  foreach($key in $properties.Keys) {
    $lines += "$indent$($key): $($properties[$key])`n"
  }

  return $lines
}

function GetTitleFrontMatterFromMarkdownFile($markdownFile) {
  $contents = Get-Content $markdownFile -Raw
  return GetTitleFrontMatterFromMarkdownContents $contents
}

function GetTitleFrontMatterFromMarkdownContents($contents) {
  $contents -match 'title:\s*(?<title>.*)' | Out-Null
  return $Matches.title
}

function ProcessMarkdownFile($markdownFile) {
  $contents = Get-Content $markdownFile -Raw
  $header = GetTitleFrontMatterFromMarkdownContents $contents
      
  # Find end of YAML
  $endIndex = $contents | Select-String -Pattern "---" -AllMatches | ForEach-Object { $_.Matches[1].Index }
  
  # Insert Header
  $contents = $contents.Substring(0, $endIndex + 5) + "`n# $header`n" + $contents.Substring($endIndex + 5)
  
  # Find Sample Placeholders, replace with code content
  foreach ($sample in ($contents | Select-String -Pattern '>\s*\[!SAMPLE\s*(?<sampleid>.*)\s*\]\s*' -AllMatches).Matches) {
    $sampleid = $sample.Groups[1].Value
    $sampleString = $sample.Groups[0].Value
  
    # Find matching filename for CS
    foreach ($csFile in Get-ChildItem -Recurse -Path ($markdownFile.DirectoryName + '\**\*.xaml.cs').Replace('\', '/') |
      Where-Object { $_.FullName -notlike "*\bin\*" -and $_FullName -notlike "*\obj\*" }) {
      $csSample = Get-Content $csFile -Raw
          
      if ($csSample -match '\[ToolkitSample\s?\(\s*id:\s*(?:"|nameof\()\s?' + $sampleid + '\s?(?:"|\))') {
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
  
  $markdownFileName = $markdownFile.Name
  
  # If the file is named the same as the component, rename it to index.md
  # This is so that the URL for the component is /componentName instead of /componentName/componentName
  if ($markdownFileName -eq "$componentName.md") {
    $markdownFileName = "index.md"
  }
  
  $mdOutputPath = Join-Path $OutputDir $componentName
  $mdOutputFile = Join-Path $mdOutputPath $markdownFileName
  
  # Create output directory if it doesn't exist
  if (-not (Test-Path $mdOutputPath)) {
    New-Item -ItemType Directory -Path $mdOutputPath | Out-Null
  }
    
  # Write file contents
  Write-Host 'Writing File:', $mdOutputFile
  $contents | Set-Content $mdOutputFile
    
  return $mdOutputFile
}

$componentsRoot = Resolve-Path $PSScriptRoot/../components/

# For each component
foreach ($componentFolder in Get-ChildItem -Path $componentsRoot -Directory) {
  $componentName = $componentFolder.Name

  # Add component to TOC
  $markdownFiles = Get-ChildItem -Recurse -Path "$componentFolder/samples/**/*.md" | Where-Object { $_.FullName -notlike "*\bin\*" -and $_FullName -notlike "*\obj\*" }
  
  # If there's only one markdown file, append it to the root of the TOC
  if ($markdownFiles.Count -eq 1) {    
    $header = GetTitleFrontMatterFromMarkdownFile $markdownFiles[0]
    $mdOutputFile = ProcessMarkdownFile $markdownFiles[0]
    
    $tocHref = $mdOutputFile.Trim('/').Replace($OutputDir, '').Trim('\')
    $tocContents += AppendTocItem $header 1 @{ "href" = $tocHref }
  }
  else {
    $tocContents += AppendTocItem $componentName 1 @{ "items" = "" }
  
    # For each markdown file
    foreach ($markdownFile in $markdownFiles) {
      $header = GetTitleFrontMatterFromMarkdownFile $markdownFile
      $mdOutputFile = ProcessMarkdownFile $markdownFile
    
      $tocHref = $mdOutputFile.Trim('/').Replace($OutputDir, '').Trim('\')
      $tocContents += AppendTocItem $header 2 @{ "href" = $tocHref }
    }
  }
}

Write-Host 'Writing TOC'
$tocContents | Set-Content (Join-Path $OutputDir "TOC.yml")