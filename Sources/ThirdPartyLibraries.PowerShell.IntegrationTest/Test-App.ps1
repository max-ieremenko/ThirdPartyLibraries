$ErrorActionPreference = "Stop"

Expand-Archive -Path "/pwsh.zip" -DestinationPath "/root/.local/share/powershell/Modules/ThirdPartyLibraries"
Import-Module ThirdPartyLibraries

Show-ThirdPartyLibrariesInfo

Update-ThirdPartyLibrariesRepository -AppName "Test" -Source "/sources" -Repository "/repository" -InformationAction Continue

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