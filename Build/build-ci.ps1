#Requires -Version "7.0"
#Requires -Modules @{ ModuleName="InvokeBuild"; ModuleVersion="5.11.0" }

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $GithubToken
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$file = Join-Path $PSScriptRoot "build-tasks.ps1"
Invoke-Build -File $file -Task CiBuild -GithubToken $GithubToken