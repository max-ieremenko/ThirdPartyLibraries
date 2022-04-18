Set-StrictMode -Version Latest

if (($PSVersionTable.PSEdition -ne "Core") -or ([version]$PSVersionTable.PSVersion -lt "7.0.0")) {
    Write-Error "This module requires PowerShell 7.0.0+. Please, upgrade your PowerShell version."
    Exit 1
}

$psModule = $ExecutionContext.SessionState.Module
$root = $psModule.ModuleBase
$dllPath = Join-Path -Path $root "ThirdPartyLibraries.PowerShell.dll"

$importedModule = Import-Module -Name $dllPath -PassThru

# When the module is unloaded, remove the nested binary module that was loaded with it
$psModule.OnRemove = {
    Remove-Module -ModuleInfo $importedModule
}
