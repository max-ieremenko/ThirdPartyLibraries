$sourceDir = $settings.sources
$repositoryDir = $settings.repository
$outDir = Join-Path $settings.output "ThirdNotices"
$version = $settings.version

$app = Join-Path $settings.bin "app/net5.0/ThirdPartyLibraries.dll"
    
Write-Host "Update repository"
Exec { dotnet $app update -appName ThirdPartyLibraries -source "$sourceDir" -repository "$repositoryDir" }

Write-Host "Validate repository"
Exec { dotnet $app validate -appName ThirdPartyLibraries -source "$sourceDir" -repository "$repositoryDir" }

Write-Host "Generate ThirdPartyNotices"
Exec { dotnet $app generate -appName ThirdPartyLibraries -repository "$repositoryDir" -to "$outDir" -title "Third party libraries $version" }

Copy-Item -Path (Join-Path $sourceDir "..\LICENSE") -Destination "$outDir"
