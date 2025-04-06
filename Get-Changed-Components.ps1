<#
.PARAMETER FromSha
    The SHA of the commit to start the diff from. This is typically the SHA of the main branch.
    The default value is 'origin/main'.

.PARAMETER ToSha
    The SHA of the commit to end the diff at. This is typically the SHA of the current commit.
    If not provided, the script will use the current commit SHA.

.EXAMPLE
    Get-Changed-Components.ps1 -FromSha 'origin/main' -ToSha $(git rev-parse HEAD)

    Get-Changed-Components.ps1 -t $(git rev-parse HEAD)

    Gets the components that have been changed in a specific patch range, defined by -FromSha and -ToSha parameters.

.NOTES
    Author: Arlo Godfrey
    Date:   2/19/2024
#>
Param (
    [Alias("f")]
    [string]$FromSha = 'origin/main',

    [Alias("t")]
    [string]$ToSha
)

if (-not $ToSha) {
    $ToSha = $(git rev-parse HEAD)
}

git fetch origin main

$changedComponentFiles = Invoke-Expression "git diff --name-only $($FromSha)...$($ToSha) -- components/"
$otherChanges = Invoke-Expression "git diff --name-only $($FromSha)...$($ToSha) | Select-String -NotMatch '^components/'"

if (-not $otherChanges) {
  $names = $changedComponentFiles | ForEach-Object { ($_ -replace '^components/', '') -replace '/.*$', '' }
  $uniqueNames = $names | Sort-Object -Unique
  $quotedNames = $uniqueNames | ForEach-Object { "'$_'" }
  $changedComponentsList = $quotedNames -join ','
  return $changedComponentsList
}
else {
    return 'all';
}