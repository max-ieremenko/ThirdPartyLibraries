$app = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources\bin\app\net6.0\ThirdPartyLibraries.dll"))
$repositoryDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\ThirdPartyLibraries"))
$examplesDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Examples\third-party-notices-template"))

$originalTemplateFileName = Join-Path $repositoryDir "configuration\third-party-notices-template.txt"
$originalTemplateBackup = [System.IO.Path]::GetTempFileName()
Copy-Item -Path $originalTemplateFileName -Destination $originalTemplateBackup

$examples = Get-ChildItem -Path $examplesDir -Directory
foreach ($example in $examples) {
    Write-Host "Update $($example.Name)"

    try {
        Copy-Item -Path (Join-Path $example "third-party-notices-template.txt") -Destination $originalTemplateFileName -Force
        Exec { dotnet $app generate -appName ThirdPartyLibraries -repository "$repositoryDir" -to "$example" -title "Third party libraries" }
    }
    finally {
        Copy-Item -Path $originalTemplateBackup -Destination $originalTemplateFileName -Force
    }
}

Remove-Item -Path $originalTemplateBackup
