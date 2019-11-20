function Get-AssemblyVersion($assemblyInfoCsPath) {
    $Anchor = "AssemblyVersion(""";
    $lines = Get-Content -Path $assemblyInfoCsPath

    foreach ($line in $lines) {
        $index = $line.IndexOf($Anchor);
        if ($index -lt 0) {
            continue;
        }
        
        $text = $line.Substring($index + $Anchor.Length);

        $index = $text.IndexOf('"');
        $text = $text.Substring(0, $index);
    
        $version = New-Object -TypeName System.Version -ArgumentList $text
        $build = $version.Build
        if ($build -le 0) {
            $build = 0
        }
    
        $text = (New-Object -TypeName System.Version -ArgumentList $version.Major,$version.Minor,$build).ToString();
        return $text;
    }
}

function Get-RepositoryCommitId {
    $response = (Invoke-RestMethod -Uri "https://api.github.com/repos/max-ieremenko/ThirdPartyLibraries/commits/master")
    return $response.sha
}
