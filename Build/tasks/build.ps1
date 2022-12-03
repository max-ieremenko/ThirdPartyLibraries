[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $SourcesPath
)

task Default DotnetRestore, DotnetBuild, NpmInstall

task DotnetRestore {
    exec { dotnet restore $SourcesPath }
}

task DotnetBuild {
    $sln = Join-Path $SourcesPath "ThirdPartyLibraries.sln"
    exec {
        dotnet build $sln `
            -t:Rebuild `
            -p:Configuration=Release `
            -p:ContinuousIntegrationBuild=true `
            -p:EmbedUntrackedSources=true
    }
}

task NpmInstall {
    $currentLocation = Get-Location
    Set-Location  (Join-Path $SourcesPath "ThirdPartyLibraries.Npm.Demo")
    exec { npm install }
    Set-Location $currentLocation
}