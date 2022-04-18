$sourceDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources"))
$repositoryDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\ThirdPartyLibraries"))
$outDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\build-out\ThirdNotices"))

$app = Join-Path $sourceDir "bin\app\net5.0\ThirdPartyLibraries.dll"
    
Write-Host "Update repository"
Exec { dotnet $app update -appName ThirdPartyLibraries -source "$sourceDir" -repository "$repositoryDir" }

Write-Host "Validate repository"
Exec { dotnet $app validate -appName ThirdPartyLibraries -source "$sourceDir" -repository "$repositoryDir" }

Write-Host "Generate ThirdPartyNotices"
Exec { dotnet $app generate -appName ThirdPartyLibraries -repository "$repositoryDir" -to "$outDir" -title "Third party libraries" }

Copy-Item -Path (Join-Path $sourceDir "..\LICENSE") -Destination "$outDir"
