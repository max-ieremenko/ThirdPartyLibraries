#Install-Module -Name psake
$workingDir = $PSScriptRoot

$buildOutDir = Join-Path $workingDir "..\build.out"
$sourceDir = Join-Path $workingDir "..\Sources"
$repositoryDir = Join-Path $workingDir "..\ThirdPartyLibraries"
$psakeMain = Join-Path $workingDir "build-tasks.ps1"

Invoke-psake $psakeMain -parameters @{"buildOutDir"="$buildOutDir"; "sourceDir"="$sourceDir"; "repositoryDir"="$repositoryDir"}