$outDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\build-out"))
$binDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources\bin\app"))
$lic = Join-Path $outDir "ThirdNotices\*"

$buildProps = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources\Directory.Build.props"))
$packageVersion = (Select-Xml -Path $buildProps -XPath "Project/PropertyGroup/DefaultPackageVersion").Node.InnerText
if (-not $packageVersion) {
    throw "Package version not found"
}

$destination = Join-Path $outDir "ThirdPartyLibraries.$packageVersion-netcore31.zip"
$source = Join-Path $binDir "netcoreapp3.1\publish\*"
Compress-Archive -Path $source, $lic -DestinationPath $destination

$destination = Join-Path $outDir "ThirdPartyLibraries.$packageVersion-net50.zip"
$source = Join-Path $binDir "net5.0\publish\*"
Compress-Archive -Path $source, $lic -DestinationPath $destination
