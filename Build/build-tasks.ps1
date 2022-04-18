task LocalBuild Initialize, Clean, CiBuild, PsCoreTest
task CiBuild Build, ThirdPartyNotices, UnitTest, Pack

task UnitTest UnitTestCore31, UnitTest50, UnitTest60
task Pack PackGlobalTool, PackPowerShellModule, PackManualDownload, PackTest

task Initialize {
    $env:GITHUB_SHA = Exec { git rev-parse HEAD }
}

task Clean {
    $buildOutDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\build-out"))
    if (Test-Path $buildOutDir) {
        Remove-Item -Path $buildOutDir -Recurse -Force
    }

    $sourcesDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources"))
    Get-ChildItem -Path $sourcesDir -Filter bin -Directory -Recurse | Remove-Item -Recurse -Force
    Get-ChildItem -Path $sourcesDir -Filter obj -Directory -Recurse | Remove-Item -Recurse -Force

    $nodeModules = Join-Path $sourcesDir "ThirdPartyLibraries.Npm.Demo\node_modules"
    if (Test-Path $nodeModules) {
        Remove-Item -Path $nodeModules -Recurse -Force
    }
}

task Build {
    Exec { .\step-build.ps1 }
}

task ThirdPartyNotices {
    Exec { .\step-third-party-notices.ps1 }
}

task UnitTestCore31 {
    Exec { .\step-unit-test.ps1 "netcoreapp3.1" }
}

task UnitTest50 {
    Exec { .\step-unit-test.ps1 "net5.0" }
}

task UnitTest60 {
    Exec { .\step-unit-test.ps1 "net6.0" }
}

task PackGlobalTool {
    Exec { .\step-pack-global-tool.ps1 }
}

task PackPowerShellModule {
    .\step-pack-ps-module.ps1
}

task PackManualDownload {
    Exec { .\step-pack-manual-download.ps1 }
}

task PackTest {
    Exec { .\step-pack-test.ps1 }
}

task PsCoreTest {
    # show-powershell-images.ps1
    $images = $(
        "mcr.microsoft.com/powershell:7.0.0-ubuntu-18.04"
        , "mcr.microsoft.com/powershell:7.0.1-ubuntu-18.04"
        , "mcr.microsoft.com/powershell:7.0.2-ubuntu-18.04"
        , "mcr.microsoft.com/powershell:7.0.3-ubuntu-18.04"
        , "mcr.microsoft.com/powershell:7.1.0-ubuntu-18.04"
        , "mcr.microsoft.com/powershell:7.1.1-ubuntu-20.04"
        , "mcr.microsoft.com/powershell:7.1.2-ubuntu-20.04"
        , "mcr.microsoft.com/powershell:7.1.3-ubuntu-20.04"
        , "mcr.microsoft.com/powershell:7.1.4-ubuntu-20.04"
        , "mcr.microsoft.com/powershell:7.1.5-ubuntu-20.04"
        , "mcr.microsoft.com/powershell:7.2.0-ubuntu-20.04"
        , "mcr.microsoft.com/powershell:7.2.1-ubuntu-20.04"
        , "mcr.microsoft.com/powershell:7.2.2-ubuntu-20.04"
        , "mcr.microsoft.com/powershell:7.3.0-preview.3-ubuntu-20.04")

    $builds = @()
    foreach ($image in $images) {
        $builds += @{
            File      = "step-test-ps-module.ps1";
            Task      = "Test";
            ImageName = $image;
        }
    }

    Build-Parallel $builds -ShowParameter ImageName -MaximumBuilds 4
}
