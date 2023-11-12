[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $BinPath,

    [Parameter(Mandatory)]
    [string]
    $Version,

    [Parameter(Mandatory = $true)]
    [ValidateSet("net6.0", "net7.0", "net8.0")] 
    [string]
    $Framework,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $OutPath
)

task Default {
    $lic = Join-Path $OutPath "ThirdNotices/*"
    $destination = Join-Path $OutPath "ThirdPartyLibraries.$Version-$Framework.zip"
    $source = Join-Path $BinPath "app/$Framework/publish/*"

    Compress-Archive -Path $source, $lic -DestinationPath $destination
}