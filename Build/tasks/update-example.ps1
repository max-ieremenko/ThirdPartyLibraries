[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $AppPath,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $ExamplePath,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $RepositoryPath,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $TemplatePath,

    [Parameter(Mandatory)]
    [string]
    $ToFileName
)

task . {
    exec { 
        dotnet $AppPath `
            generate `
            -appName ThirdPartyLibraries `
            -repository $RepositoryPath `
            -to $ExamplePath `
            -title 'Third party libraries' `
            -template $TemplatePath `
            -toFileName $ToFileName
    }
}