$outDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../build-out"))
$temp = Join-Path $outDir "pwsh"

Copy-Item -Path (Join-Path $PSScriptRoot "../Sources/bin/pwsh") -Destination $temp -Recurse

# .psd1 set module version
$version = Select-Xml -Path (Join-Path $PSScriptRoot "../Sources/Directory.Build.props") -XPath "Project/PropertyGroup/DefaultPackageVersion"
$psdFile = Join-Path $temp "ThirdPartyLibraries.psd1"
((Get-Content -Path $psdFile -Raw) -replace "3.2.1", $version.Node.InnerText) | Set-Content -Path $psdFile

Get-ChildItem $temp -Include *.pdb -Recurse | Remove-Item

$lic = Join-Path $outDir "ThirdNotices/*"
Compress-Archive -Path "$temp/*", $lic -DestinationPath (Join-Path $outDir "pwsh.zip")

Remove-Item -Path $temp -Recurse