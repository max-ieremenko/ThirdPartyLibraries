#Requires -Version "7.0"
#Requires -Modules @{ ModuleName="InvokeBuild"; ModuleVersion="5.10.3" }

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$file = Join-Path $PSScriptRoot "build-tasks.ps1"
Invoke-Build -File $file -Task LocalBuild