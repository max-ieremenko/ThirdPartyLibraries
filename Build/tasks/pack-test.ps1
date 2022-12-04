[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $Path
)

task Default Expand, TestLicenses

Enter-Build {
    $name = Split-Path $Path -Leaf
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) $name
    remove $temp
}

Exit-Build {
    remove $temp
}

task Expand {
    New-Item -Path $temp -ItemType Directory | Out-Null
    Expand-Archive -Path $Path -DestinationPath $temp
}

task TestLicenses {
    assert (Test-Path (Join-Path $temp "LICENSE")) "File LICENSE not found in $name"
    assert (Test-Path (Join-Path $temp "ThirdPartyNotices.txt")) "File ThirdPartyNotices.txt not found in $name"
    assert (Test-Path (Join-Path $temp "Licenses/MIT-license.txt")) "File Licenses/MIT-license.txt not found in $name"
}

task TestNuSpec {
    $ext = [System.IO.Path]::GetExtension($name)
    if ($ext -eq ".nupkg") {
        $nuspecFile = Get-ChildItem -Path $temp -Filter *.nuspec
        assert ($nuspecFile) ".nuspec not found in $name"

        [xml]$nuspec = Get-Content $nuspecFile
        $ns = New-Object -TypeName "Xml.XmlNamespaceManager" -ArgumentList $nuspec.NameTable
        $ns.AddNamespace("n", $nuspec.DocumentElement.NamespaceURI)
    
        $id = $nuspec.SelectNodes("n:package/n:metadata/n:id", $ns)
        assert ($id) "id not found in $name"
    
        $repository = $nuspec.SelectNodes("n:package/n:metadata/n:repository", $ns)
        assert ($repository) "Repository element not found in $name"
    
        if ([string]::IsNullOrWhiteSpace($nuspec.SelectNodes("n:package/n:metadata/n:repository/@commit", $ns).Value)) {
            throw ("Repository commit attribute not found in $name")
        }
    }
}