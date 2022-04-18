[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]
    $ImageName
)

task Test {
    $nugetLocal = Join-Path $Env:USERPROFILE ".nuget\packages"
    $pwsh = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\build-out\pwsh.zip"))
    $source = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources\ThirdPartyLibraries.PowerShell.IntegrationTest"))
    $userSecrets = Join-Path $Env:APPDATA "Microsoft\UserSecrets"

    $runArgs = $(
        "run"
        , "--rm"
        , "-v", "$($nugetLocal):/root/.nuget/packages:ro"
        , "-v", "$($pwsh):/pwsh.zip:ro"
        , "-v", "$($source):/sources:ro"
    )

    if (Test-Path $userSecrets) {
        $runArgs += $("-v", "$($userSecrets):/root/.microsoft/usersecrets:ro")
    }

    $runArgs += $(
        $ImageName
        , "pwsh", "-Command", "/sources/Test-App.ps1"
    )

    exec { docker $runArgs }
}
