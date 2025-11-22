[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $Source,

    [Parameter(Mandatory)]
    [string]
    $Version
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Set-StrictMode -Version Latest

dotnet tool install `
    --local `
    --create-manifest-if-needed `
    --version $Version `
    --add-source $Source `
    --verbosity quiet `
    ThirdPartyLibraries.GlobalTool
if ($LASTEXITCODE) {
    throw 'dotnet tool install failed.'
}

dotnet ThirdPartyLibraries
if ($LASTEXITCODE) {
    throw 'dotnet ThirdPartyLibraries failed.'
}
