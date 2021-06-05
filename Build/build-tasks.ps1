Include ".\build-scripts.ps1"

Properties  {
    $buildOutDir,
    $sourceDir,
    $repositoryDir
}

Task default -Depends Initialize, Clean, Build, Test, CreateThirdPartyNotices, PackGlobalTool, PackApp, PostClean

Task Initialize {
    Assert ($buildOutDir -ne $null) "Build output is missing"
    Assert (Test-Path $sourceDir) "Sources not found"
    Assert (Test-Path $repositoryDir) "Repository not found"

    $script:buildOutApp50Dir = Join-Path $sourceDir "bin\app\net5.0"
    $script:buildOutApp31Dir = Join-Path $sourceDir "bin\app\netcoreapp3.1"
    $script:buildOutTestDir = Join-Path $sourceDir "bin\test"
    $script:buildOutThirdNoticesDir = Join-Path $buildOutDir "ThirdNotices"
    $script:packageVersion = Get-AssemblyVersion (Join-Path $sourceDir "GlobalAssemblyInfo.cs")
    $script:repositoryCommitId = Get-RepositoryCommitId

    Write-Host "PackageVersion: $packageVersion"
    Write-Host "CommitId: $repositoryCommitId"
}

Task Clean {
    if (Test-Path $buildOutDir) {
        Remove-Item -Path $buildOutDir -Recurse -Force
    }

    if (Test-Path $buildOutApp50Dir) {
        Remove-Item -Path $buildOutApp50Dir -Recurse -Force
    }

    if (Test-Path $buildOutApp31Dir) {
        Remove-Item -Path $buildOutApp31Dir -Recurse -Force
    }

    if (Test-Path $buildOutTestDir) {
        Remove-Item -Path $buildOutTestDir -Recurse -Force
    }
}

Task Build {
    $solutionFile = Join-Path $sourceDir "ThirdPartyLibraries.sln"
    Exec { & dotnet restore $solutionFile }

    Exec { & dotnet publish -c Release -f net5.0 $solutionFile }
    Exec { & dotnet publish -c Release -f netcoreapp3.1 $solutionFile }
}

Task Test {
    # https://github.com/nunit/docs/wiki/.NET-Core-and-.NET-Standard
    $projects = (Get-ChildItem $sourceDir -Recurse -Include *.Test.csproj) | Sort-Object
    foreach ($project in $projects) {
        Exec { & dotnet test -c Release $project }
    }
}

Task CreateThirdPartyNotices {
    $app = Join-Path $buildOutApp50Dir ThirdPartyLibraries.exe
    
    Write-Host "Update repository"
    Exec { & $app update -appName ThirdPartyLibraries -source "$sourceDir" -repository "$repositoryDir" }

    Write-Host "Validate repository"
    Exec { & $app validate -appName ThirdPartyLibraries -source "$sourceDir" -repository "$repositoryDir" }

    Write-Host "Generate ThirdPartyNotices"
    Exec { & $app generate -appName ThirdPartyLibraries -repository "$repositoryDir" -to "$buildOutThirdNoticesDir" }
    Copy-Item -Path (Join-Path $sourceDir "..\LICENSE") -Destination "$buildOutThirdNoticesDir"
}

Task PackGlobalTool {
    $appProjFile = Join-Path $sourceDir "ThirdPartyLibraries\ThirdPartyLibraries.csproj"

    # workaround for he DateTimeOffset specified cannot be converted into a Zip file timestamp, https://github.com/NuGet/Home/issues/7001
    Get-ChildItem -Path $buildOutApp50Dir -File -Recurse | % {$_.LastWriteTime = (Get-Date)}
    Get-ChildItem -Path $buildOutApp31Dir -File -Recurse | % {$_.LastWriteTime = (Get-Date)}
    
    Exec { & dotnet pack "$appProjFile" -c Release --no-build -p:PackAsTool=true -p:PackageVersion=$packageVersion -p:RepositoryCommit=$repositoryCommitId -o "$buildOutDir" }
}

Task PackApp {
    $destination = Join-Path $buildOutDir "ThirdPartyLibraries-net5.0-$packageVersion.zip"
    Compress-Archive -Path "$buildOutApp50Dir\publish\*","$buildOutThirdNoticesDir\*" -DestinationPath $destination

    $destination = Join-Path $buildOutDir "ThirdPartyLibraries-net3.1-$packageVersion.zip"
    Compress-Archive -Path "$buildOutApp31Dir\publish\*","$buildOutThirdNoticesDir\*" -DestinationPath $destination
}

Task PostClean {
    Remove-Item -Path $buildOutApp50Dir -Recurse -Force
    Remove-Item -Path $buildOutApp31Dir -Recurse -Force
    Remove-Item -Path $buildOutTestDir -Recurse -Force
}
