$outDir = $settings.output
$binDir = Join-Path $settings.bin "app"
$lic = Join-Path $outDir "ThirdNotices/*"
$packageVersion = $settings.version

$destination = Join-Path $outDir "ThirdPartyLibraries.$packageVersion-netcore31.zip"
$source = Join-Path $binDir "netcoreapp3.1/publish/*"
Compress-Archive -Path $source, $lic -DestinationPath $destination

$destination = Join-Path $outDir "ThirdPartyLibraries.$packageVersion-net50.zip"
$source = Join-Path $binDir "net5.0/publish/*"
Compress-Archive -Path $source, $lic -DestinationPath $destination

$destination = Join-Path $outDir "ThirdPartyLibraries.$packageVersion-net60.zip"
$source = Join-Path $binDir "net6.0/publish/*"
Compress-Archive -Path $source, $lic -DestinationPath $destination