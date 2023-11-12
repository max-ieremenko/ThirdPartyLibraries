#Requires -Version "7.0"

[CmdletBinding()]
param (
    [Parameter()]
    [ValidateSet(".net", "InvokeBuild")] 
    [string[]]
    $List
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "scripts" "Get-ModuleVersion.ps1")

if (-not $List -or (".net" -in $List)) {
    $install = Join-Path $PSScriptRoot "scripts/Install-DotNet.ps1"
    & $install "6.0.403"
    & $install "7.0.100"
    & $install (Get-Content -Raw (Join-Path $PSScriptRoot "../Sources/global.json") | ConvertFrom-Json).sdk.version
}

if (-not $List -or ("InvokeBuild" -in $List)) {
    $install = Join-Path $PSScriptRoot "scripts/Install-Module.ps1"
    $version = Get-ModuleVersion "InvokeBuild"
    & $install "InvokeBuild" $version
}
