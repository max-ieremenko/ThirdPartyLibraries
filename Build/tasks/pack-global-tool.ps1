[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $SourcesPath,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $OutPath
)

task Default {
    $projectFile = Join-Path $SourcesPath "ThirdPartyLibraries/ThirdPartyLibraries.csproj"

    exec {
        dotnet pack `
            -c Release `
            -p:PackAsTool=true `
            -p:GlobalTool=true `
            -o $OutPath `
            $projectFile
    }
}
