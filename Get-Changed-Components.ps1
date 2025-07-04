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

# Ensure that orphaned commits are available for diffing (e.g. when force-pushed).
git fetch origin $FromSha
git fetch origin main

$changedComponentFiles = Invoke-Expression "git diff --name-only $($FromSha)...$($ToSha) -- components/" -ErrorAction Stop
$otherChanges = Invoke-Expression "git diff --name-only $($FromSha)...$($ToSha) | Select-String -NotMatch '^components/'" -ErrorAction Stop

# If one or more components is changed, return them in a list. 
$retChangedComponents = -not [string]::IsNullOrWhiteSpace($changedComponentFiles);

# Return 'all' components when either:
# - One or more files *outside* of a component folder is changed
# - All files *inside* of component folders are unchanged
# Both of these:
#   - Can happen when any non-component file is changed
#   - May indicate (but not guarantee) a build configuration change.
#   - Are a fallback to ensure that the script doesn't return an empty list of components.
$retAllComponents = [string]::IsNullOrWhiteSpace($changedComponentFiles) -or -not [string]::IsNullOrWhiteSpace($otherChanges);

if ($retAllComponents) {
    return 'all';
}

if ($retChangedComponents) {
  $names = $changedComponentFiles | ForEach-Object { ($_ -replace '^components/', '') -replace '/.*$', '' }
  $uniqueNames = $names | Sort-Object -Unique
  $changedComponentsList = $uniqueNames -join ','
  return $changedComponentsList
}

Write-Error "Unhandled code path."
Write-Error "Please report this error to the author of this script."