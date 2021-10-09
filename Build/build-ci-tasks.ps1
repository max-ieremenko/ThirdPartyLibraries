task Default Build, ThirdPartyNotices, UnitTest, Pack
task UnitTest UnitTestCore31, UnitTest50
task Pack PackGlobalTool, PackManualDownload, PackTest

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