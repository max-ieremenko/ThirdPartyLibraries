. (Join-Path $PSScriptRoot ".\step-pack-test-scripts.ps1")

$binDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\build-out"))

$packageList = Get-ChildItem -Path $binDir -Recurse -File -Include "*.nupkg", "*.zip" | ForEach-Object {$_.FullName}

if ((-not $packageList) -or (-not $packageList.Length)) {
    throw "no packages found."
}

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
