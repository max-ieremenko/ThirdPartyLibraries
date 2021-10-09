task LocalBuild Initialize, Clean, Build, ThirdPartyNotices, UnitTest, Pack
task CiBuild Build, ThirdPartyNotices, UnitTest, Pack
task UnitTest UnitTestCore31, UnitTest50
task Pack PackGlobalTool, PackManualDownload, PackTest

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

task PackGlobalTool {
    Exec { .\step-pack-global-tool.ps1 }
}

task PackManualDownload {
    Exec { .\step-pack-manual-download.ps1 }
}

task PackTest {
    Exec { .\step-pack-test.ps1 }
}