[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $GithubToken
)

task LocalBuild Initialize, Clean, CiBuild, PsCoreTest, UpdateExamples
task CiBuild Build, ThirdPartyNotices, UnitTest, Pack

task Pack PackGlobalTool, PackPowerShellModule, PackManualDownload, PackTest

Enter-Build {
    $settings = @{
        sources    = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Sources'))
        output     = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../build-out'))
        bin        = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Sources/bin'))
        repository = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ThirdPartyLibraries'))
        examples   = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Examples'))
        frameworks = 'net6.0', 'net8.0', 'net9.0'
        version    = $(
            $buildProps = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Sources/Directory.Build.props'))
            $packageVersion = (Select-Xml -Path $buildProps -XPath 'Project/PropertyGroup/DefaultPackageVersion').Node.InnerText
            assert $packageVersion 'Package version not found'
            $packageVersion
        )
    }
}

task Initialize {
    $env:GITHUB_SHA = Exec { git rev-parse HEAD }
}

task Clean {
    if (Test-Path $settings.output) {
        Remove-Item -Path $settings.output -Recurse -Force
    }

    Get-ChildItem -Path $settings.sources -Filter bin -Directory -Recurse | Remove-Item -Recurse -Force
    Get-ChildItem -Path $settings.sources -Filter obj -Directory -Recurse | Remove-Item -Recurse -Force

    $nodeModules = Join-Path $settings.sources 'ThirdPartyLibraries.Npm.Demo\node_modules'
    if (Test-Path $nodeModules) {
        Remove-Item -Path $nodeModules -Recurse -Force
    }
}

task Build {
    Invoke-Build -File 'tasks/build.ps1' -SourcesPath $settings.sources
}

task ThirdPartyNotices {
    Invoke-Build `
        -File 'tasks/third-party-notices.ps1' `
        -AppPath (Join-Path $settings.bin 'app/net6.0/ThirdPartyLibraries.dll') `
        -Version $settings.version `
        -SourcesPath $settings.sources `
        -RepositoryPath $settings.repository `
        -OutPath (Join-Path $settings.output 'ThirdNotices') `
        -GithubToken $GithubToken
}

task UnitTest {
    $builds = @()
    foreach ($framework in $settings.frameworks) {
        $builds += @{ File = 'tasks/unit-test.ps1'; BinPath = $settings.bin; Framework = $framework }
    }
    
    Build-Parallel $builds -ShowParameter Framework -MaximumBuilds 4
}

task PackGlobalTool {
    Invoke-Build -File 'tasks/pack-global-tool.ps1' -SourcesPath $settings.sources -OutPath $settings.output
}

task PackPowerShellModule {
    Invoke-Build -File 'tasks/pack-ps-module.ps1' `
        -BinPath $settings.bin `
        -Version $settings.version `
        -OutPath $settings.output
}

task PackManualDownload {
    $builds = @()
    foreach ($framework in $settings.frameworks) {
        $builds += @{ 
            File      = 'tasks/pack-manual-download.ps1'
            BinPath   = $settings.bin
            Version   = $settings.version
            Framework = $framework
            OutPath   = $settings.output
        }
    }
    
    Build-Parallel $builds -ShowParameter Framework -MaximumBuilds 4
}

task PackTest {
    $packageList = Get-ChildItem -Path $settings.output -Recurse -File -Include '*.nupkg', '*.zip' | ForEach-Object { $_.FullName }
    assert ($packageList -and $packageList.Length) 'no packages found.'

    $builds = @()
    foreach ($package in $packageList) {
        $builds += @{ 
            File = 'tasks/pack-test.ps1'
            Path = $package
        }
    }

    Build-Parallel $builds -ShowParameter Path -MaximumBuilds 4
}

task PsCoreTest {
    # show-powershell-images.ps1
    $images = $(
        'mcr.microsoft.com/powershell:7.0.0-ubuntu-18.04'
        , 'mcr.microsoft.com/powershell:7.0.1-ubuntu-18.04'
        , 'mcr.microsoft.com/powershell:7.0.2-ubuntu-18.04'
        , 'mcr.microsoft.com/powershell:7.0.3-ubuntu-18.04'
        , 'mcr.microsoft.com/powershell:7.1.0-ubuntu-18.04'
        , 'mcr.microsoft.com/powershell:7.1.1-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.1.2-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.1.3-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.1.4-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.1.5-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.2.0-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.2.1-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.2.2-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.3-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:7.4-ubuntu-20.04'
        , 'mcr.microsoft.com/powershell:preview-7.5-ubuntu-20.04')

    $builds = @()
    foreach ($image in $images) {
        exec { docker pull --quiet $image }
        
        $builds += @{
            File           = 'tasks/test-ps-module.ps1'
            PowerShellPath = (Join-Path $settings.output 'pwsh.zip')
            TestPath       = (Join-Path $settings.sources 'ThirdPartyLibraries.PowerShell.IntegrationTest')
            ImageName      = $image
        }
    }

    Build-Parallel $builds -ShowParameter ImageName -MaximumBuilds 4
}

task UpdateExamples {
    $appPath = Join-Path $settings.bin 'app/net6.0/ThirdPartyLibraries.dll'
    $builds = @()

    $examples = Get-ChildItem -Path (Join-Path $settings.examples 'third-party-notices-template') -Directory
    foreach ($example in $examples) {
        $builds += @{ 
            File           = 'tasks/update-example.ps1'
            AppPath        = $appPath
            ExamplePath    = $example.FullName
            RepositoryPath = $settings.repository
            TemplatePath   = (Join-Path $example.FullName 'third-party-notices-template.txt')
            ToFileName     = 'ThirdPartyNotices.txt'
        }
    }
    
    $example = Join-Path $settings.examples 'export-to-csv'
    $builds += @{ 
        File           = 'tasks/update-example.ps1'
        AppPath        = $appPath
        ExamplePath    = $example
        RepositoryPath = $settings.repository
        TemplatePath   = (Join-Path $example 'export-template.txt')
        ToFileName     = 'packages.csv'
    }

    Build-Parallel $builds -ShowParameter ExamplePath -MaximumBuilds 1
}