#Install-Module -Name psake
#Requires -Modules @{ModuleName='psake'; RequiredVersion='4.9.0'}

$workingDir = Join-Path $PSScriptRoot "..\"
$workingDir = [System.IO.Path]::GetFullPath($workingDir)

$buildOutDir = Join-Path $workingDir "build.out"
$sourceDir = Join-Path $workingDir "Sources"
$repositoryDir = Join-Path $workingDir "ThirdPartyLibraries"
$psakeMain = Join-Path $PSScriptRoot "build-tasks.ps1"

Invoke-psake $psakeMain -parameters @{"buildOutDir"="$buildOutDir"; "sourceDir"="$sourceDir"; "repositoryDir"="$repositoryDir"}