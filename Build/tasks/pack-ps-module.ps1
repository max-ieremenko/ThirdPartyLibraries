[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $BinPath,

    [Parameter(Mandatory)]
    [string]
    $Version,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $OutPath
)

task . Copy, Clean, SetVersion, Zip

Enter-Build {
    $temp = Join-Path $OutPath 'pwsh'
}

Exit-Build {
    remove $temp
}

task Copy {
    Copy-Item -Path (Join-Path $BinPath 'pwsh') -Destination $temp -Recurse    
}

task Clean {
    Get-ChildItem $temp -Include *.pdb -Recurse | Remove-Item   
}

task SetVersion {
    # .psd1 set module version
    $psdFile = Join-Path $temp 'ThirdPartyLibraries.psd1'
    $content = Get-Content -Path $psdFile -Raw
    $content = $content -replace '3.2.1', $Version
    Set-Content -Path $psdFile -Value $content
}

task Zip {
    $lic = Join-Path $OutPath 'ThirdNotices/*'
    Compress-Archive -Path "$temp/*", $lic -DestinationPath (Join-Path $OutPath 'pwsh.zip')
}