[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $SourcesPath
)

task . DotnetRestore, DotnetBuild

task DotnetRestore {
    exec { dotnet restore $SourcesPath }
}

task DotnetBuild {
    $sln = Join-Path $SourcesPath 'ThirdPartyLibraries.sln'
    exec {
        dotnet build $sln `
            -t:Rebuild `
            -p:Configuration=Release `
            -p:ContinuousIntegrationBuild=true `
            -p:EmbedUntrackedSources=true
    }
}