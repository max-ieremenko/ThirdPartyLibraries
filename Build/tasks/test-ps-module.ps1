[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $PowerShellPath,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $TestPath,

    [Parameter(Mandatory)]
    [string]
    $ImageName
)

task Default {
    $nugetLocal = Join-Path $Env:USERPROFILE ".nuget\packages"
    $userSecrets = Join-Path $Env:APPDATA "Microsoft\UserSecrets"

    $runArgs = $(
        "run"
        , "--rm"
        , "-v", "$($nugetLocal):/root/.nuget/packages:ro"
        , "-v", "$($PowerShellPath):/pwsh.zip:ro"
        , "-v", "$($TestPath):/sources:ro"
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
