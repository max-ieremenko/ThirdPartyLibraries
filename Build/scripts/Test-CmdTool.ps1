[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $Path
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Set-StrictMode -Version Latest

$tmp = 'ThirdPartyLibraries'
Expand-Archive -Path $Path -DestinationPath $tmp

dotnet "$tmp/ThirdPartyLibraries.dll"
if ($LASTEXITCODE) {
    throw 'dotnet ThirdPartyLibraries.dll failed.'
}
