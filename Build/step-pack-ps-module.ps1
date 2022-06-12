$outDir = $settings.output
$temp = Join-Path $outDir "pwsh"

Copy-Item -Path (Join-Path $settings.bin "pwsh") -Destination $temp -Recurse

# .psd1 set module version
$psdFile = Join-Path $temp "ThirdPartyLibraries.psd1"
((Get-Content -Path $psdFile -Raw) -replace "3.2.1", $settings.version) | Set-Content -Path $psdFile

Get-ChildItem $temp -Include *.pdb -Recurse | Remove-Item

$lic = Join-Path $outDir "ThirdNotices/*"
Compress-Archive -Path "$temp/*", $lic -DestinationPath (Join-Path $outDir "pwsh.zip")

Remove-Item -Path $temp -Recurse