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
    $RepositoryPath
)

task Default BackupTemplate, UpdateTemplate, Generate

Enter-Build {
    $originalTemplateFileName = Join-Path $RepositoryPath "configuration/third-party-notices-template.txt"
    $originalTemplateBackup = [System.IO.Path]::GetTempFileName()
    remove $originalTemplateBackup
}

Exit-Build {
    if (Test-Path $originalTemplateBackup) {
        Copy-Item -Path $originalTemplateBackup -Destination $originalTemplateFileName -Force
    }

    remove $originalTemplateBackup
}

task BackupTemplate {
    Copy-Item -Path $originalTemplateFileName -Destination $originalTemplateBackup
}

task UpdateTemplate {
    Copy-Item -Path (Join-Path $ExamplePath "third-party-notices-template.txt") -Destination $originalTemplateFileName -Force
}

task Generate {
    exec { dotnet $AppPath generate -appName ThirdPartyLibraries -repository $RepositoryPath -to $ExamplePath -title "Third party libraries" }
}