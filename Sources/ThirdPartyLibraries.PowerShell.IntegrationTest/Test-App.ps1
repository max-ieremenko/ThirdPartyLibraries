$ErrorActionPreference = "Stop"

Expand-Archive -Path "/pwsh.zip" -DestinationPath "/root/.local/share/powershell/Modules/ThirdPartyLibraries"
Import-Module ThirdPartyLibraries

Show-ThirdPartyLibrariesInfo

Update-ThirdPartyLibrariesRepository -AppName "Test" -Source "/sources" -Repository "/repository" -InformationAction Continue

$mit = Get-Content /repository/licenses/mit/index.json | ConvertFrom-Json
if ($mit.Code -ne "MIT" -or !$mit.HRef -or !$mit.FileName) {
    throw "MIT index.json is invalid."
}

if (!(Get-Content /repository/licenses/mit/license.txt)) {
    throw "MIT license content is empty."
}

try {
    Test-ThirdPartyLibrariesRepository -AppName "Test" -Source "/sources" -Repository "/repository" -InformationAction Continue
}
catch {
    $validationError = $_.Exception.Message
}

if (-not $validationError) {
    throw "The repository is assumed to be invalid."
}

if ($validationError -notlike "*not approved*Microsoft.SourceLink.GitHub*") {
    throw "Unexpected error $validationError."
}

$mit = Get-Content /repository/licenses/mit/index.json | ConvertFrom-Json
$mit.RequiresApproval = $false
$mit | ConvertTo-Json | Set-Content /repository/licenses/mit/index.json

Update-ThirdPartyLibrariesRepository -AppName "Test" -Source "/sources" -Repository "/repository" -InformationAction Continue
Test-ThirdPartyLibrariesRepository -AppName "Test" -Source "/sources" -Repository "/repository" -InformationAction Continue
Publish-ThirdPartyNotices -AppName "Test" -Repository "/repository" -To "/notices" -InformationAction Continue

if (-not (Test-Path "/notices/ThirdPartyNotices.txt")) {
    throw "ThirdPartyNotices.txt not found."
}

if (-not (Test-Path "/notices/Licenses")) {
    throw "Licenses not found."
}

$packages = Get-ChildItem "/repository/packages/nuget.org"
if (-not $packages.Count) {
    throw "packages/nuget.org is empty."
}

Remove-AppFromThirdPartyLibrariesRepository -AppName "Test" -Repository "/repository" -InformationAction Continue

$packages = Get-ChildItem "/repository/packages/nuget.org"
if ($packages.Count) {
    throw "packages/nuget.org must be empty."
}

