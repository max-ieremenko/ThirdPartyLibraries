Include ".\build-scripts.ps1"

Properties  {
    $buildOutDir,
    $sourceDir,
    $repositoryDir
}

Task default -Depends Initialize, Clean, Build, Test, CreateThirdPartyNotices, PackGlobalTool, PackApp

Task Initialize {
    Assert ($buildOutDir -ne $null) "Build output is missing"
    Assert (Test-Path $sourceDir) "Sources not found"
    Assert (Test-Path $repositoryDir) "Repository not found"

    $script:buildOutAppDir = Join-Path $buildOutDir "app"
    $script:buildOutTestDir = Join-Path $buildOutDir "test"
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
}

Task Build {
    $solutionFile = Join-Path $sourceDir "ThirdPartyLibraries.sln"
    Exec { & dotnet restore $solutionFile }

    $appProjFile = Join-Path $sourceDir "ThirdPartyLibraries\ThirdPartyLibraries.csproj"
    Exec { & dotnet msbuild "/t:build" "/p:Configuration=Release" "/p:OutDir=$buildOutAppDir" $appProjFile }
}

Task Test {
    # https://github.com/nunit/docs/wiki/.NET-Core-and-.NET-Standard
    $projects = (Get-ChildItem $sourceDir -Recurse -Include *.Test.csproj) | Sort-Object
    foreach ($project in $projects) {
        Exec { & dotnet test $project }
    }
}

Task CreateThirdPartyNotices {
    $app = Join-Path $buildOutAppDir ThirdPartyLibraries.exe
    
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
    Exec { & dotnet pack "$appProjFile" -c Release -p:PackAsTool=true -p:PackageVersion=$packageVersion -p:RepositoryCommit=$repositoryCommitId -o "$buildOutDir" }
}

Task PackApp {
    $destination = Join-Path $buildOutDir "ThirdPartyLibraries-$packageVersion.zip"
    Compress-Archive -Path "$buildOutAppDir\*" -DestinationPath $destination
}