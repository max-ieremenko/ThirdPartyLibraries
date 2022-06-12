. (Join-Path $PSScriptRoot ".\step-pack-test-scripts.ps1")

$packageList = Get-ChildItem -Path $settings.output -Recurse -File -Include "*.nupkg", "*.zip" | ForEach-Object {$_.FullName}
assert ($packageList -and $packageList.Length) "no packages found."

$packageList

$tempPath = Join-Path ([System.IO.Path]::GetTempPath()) "step-pack-test"
try {
    foreach ($package in $packageList) {
        Test-Package -PackageFileName $package -TempDirectory $tempPath
    }
}
finally {
    if (Test-Path $tempPath) {
        Remove-Item -Path $tempPath -Force -Recurse
    }
}
