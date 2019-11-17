function Get-AssemblyVersion($assemblyInfoCsPath) {
    $Anchor = "AssemblyVersion(""";
    $text = [System.IO.File]::ReadAllText($assemblyInfoCsPath);
    
    $index = $text.IndexOf($Anchor);
    $text = $text.Substring($index + $Anchor.Length);

    $index = $text.IndexOf('"');
    $text = $text.Substring(0, $index);

    $version = (New-Object -TypeName System.Version -ArgumentList $text).ToString();
    if ($version.EndsWith(".0")) {
        $version = $version.Substring(0, $version.Length - 2);
    }

    return $version;
}

function Get-RepositoryCommitId {
    $response = (Invoke-RestMethod -Uri "https://api.github.com/repos/max-ieremenko/ThirdPartyLibraries/commits/master")
    return $response.sha
}
