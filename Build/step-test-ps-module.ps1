[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $Output,

    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $Sources,

    [Parameter(Mandatory = $true)]
    [string]
    $ImageName
)

task Test {
    $nugetLocal = Join-Path $Env:USERPROFILE ".nuget\packages"
    $pwsh = Join-Path $Output "pwsh.zip"
    $source = Join-Path $Sources "ThirdPartyLibraries.PowerShell.IntegrationTest"
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
