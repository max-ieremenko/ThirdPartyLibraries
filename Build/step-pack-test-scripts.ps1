function Test-Package {
    param (
        [Parameter(Mandatory = $true)]
        [string]
        $PackageFileName,

        [Parameter(Mandatory = $true)]
        [string]
        $TempDirectory
    )

    if (Test-Path $TempDirectory) {
        Remove-Item -Path $TempDirectory -Force -Recurse
    }

    New-Item -Path $TempDirectory -ItemType Directory | Out-Null

    # un-zip package into TempDirectory
    $tempPackageFileName = Join-Path $TempDirectory "test.zip"
    Copy-Item -Path $PackageFileName -Destination $tempPackageFileName
    Expand-Archive -Path $tempPackageFileName -DestinationPath $TempDirectory
    Remove-Item $tempPackageFileName

    $name = Split-Path $PackageFileName -Leaf

    assert (Test-Path (Join-Path $TempDirectory "LICENSE")) "File LICENSE not found in $name"
    assert (Test-Path (Join-Path $TempDirectory "ThirdPartyNotices.txt")) "File ThirdPartyNotices.txt not found in $name"
    assert (Test-Path (Join-Path $TempDirectory "Licenses/MIT-license.txt")) "File Licenses/MIT-license.txt not found in $name"

    $name = [System.IO.Path]::GetExtension($PackageFileName)
    if ($name -eq ".nupkg") {
        # test .nuspec
        $nuspecFile = Get-ChildItem -Path $TempDirectory -Filter *.nuspec
        assert ($nuspecFile) ".nuspec not found in $PackageFileName"

        Test-NuGetSpec $nuspecFile.FullName
    }
}

function Test-NuGetSpec {
    param (
        [string]$fileName
    )
    
    [xml]$nuspec = Get-Content $fileName
    $ns = New-Object -TypeName "Xml.XmlNamespaceManager" -ArgumentList $nuspec.NameTable
    $ns.AddNamespace("n", $nuspec.DocumentElement.NamespaceURI)

    $name = $nuspec.SelectNodes("n:package/n:metadata/n:id", $ns).InnerText

    $repository = $nuspec.SelectNodes("n:package/n:metadata/n:repository", $ns)
    if (-not $repository) {
        throw ("Repository element not found in " + $name)
    }

    if ([string]::IsNullOrWhiteSpace($nuspec.SelectNodes("n:package/n:metadata/n:repository/@commit", $ns).Value)) {
        throw ("Repository commit attribute not found in " + $name)
    }
}