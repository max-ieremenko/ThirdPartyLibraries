$sourceDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources"))
$solutionFile = Join-Path $sourceDir "ThirdPartyLibraries.sln"

Exec { dotnet restore $solutionFile }
Exec { dotnet build $solutionFile -t:Rebuild -p:Configuration=Release }

$currentLocation = Get-Location
Set-Location  (Join-Path $sourceDir "ThirdPartyLibraries.Npm.Demo")
Exec { npm install }
Set-Location $currentLocation
