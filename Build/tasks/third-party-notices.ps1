[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $AppPath,

    [Parameter(Mandatory)]
    [string]
    $Version,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $SourcesPath,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $RepositoryPath,

    [Parameter(Mandatory)]
    [string]
    $OutPath
)

task Default Update, Validate, Generate, CopyLicense

task Update {
    exec { dotnet $AppPath update -appName ThirdPartyLibraries -source $SourcesPath -repository $RepositoryPath }
}

task Validate {
    exec { dotnet $AppPath validate -appName ThirdPartyLibraries -source $SourcesPath -repository $RepositoryPath }
}

task Generate {
    exec { dotnet $AppPath generate -appName ThirdPartyLibraries -repository $RepositoryPath -to $OutPath -title "Third party libraries $Version" }   
}

task CopyLicense {
    Copy-Item -Path (Join-Path $SourcesPath "../LICENSE") -Destination $OutPath
}
