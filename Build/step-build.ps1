$solutionFile = Join-Path $settings.sources "ThirdPartyLibraries.sln"

Exec { dotnet restore $solutionFile }
Exec { dotnet build $solutionFile -t:Rebuild -p:Configuration=Release }

$currentLocation = Get-Location
Set-Location  (Join-Path $settings.sources "ThirdPartyLibraries.Npm.Demo")
Exec { npm install }
Set-Location $currentLocation
