[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $BinPath,

    [Parameter(Mandatory)]
    [ValidateSet('net8.0', 'net9.0', 'net10.0')] 
    [string]
    $Framework,

    [Parameter(Mandatory)]
    [ValidateSet('sdk', 'cmd')] 
    [string]
    $ToolType,

    [Parameter(Mandatory)]
    [string]
    $ToolVersion
)

task . Pull, TestDotNet, TestCmd

Enter-Build {
    $sdkImage = $(
        $version = $Framework.TrimStart('net')
        "mcr.microsoft.com/dotnet/sdk:$version"
    )
}

task Pull {
    exec {
        docker pull --quiet $sdkImage
    }
}

task TestDotNet -If ($ToolType -eq 'sdk') {
    $scripts = Join-Path $PSScriptRoot '../scripts'

    exec {
        docker run `
            -it `
            --rm `
            --entrypoint pwsh `
            -v "$($scripts):/app/scripts" `
            -v "$($BinPath):/app/bin" `
            --workdir '/tmp' `
            $sdkImage `
            '/app/scripts/Test-SdkTool.ps1' `
            -Version $ToolVersion `
            -Source '/app/bin'
    }
}

task TestCmd -If ($ToolType -eq 'cmd') {
    $scripts = Join-Path $PSScriptRoot '../scripts'

    exec {
        docker run `
            -it `
            --rm `
            --entrypoint pwsh `
            -v "$($scripts):/app/scripts" `
            -v "$($BinPath):/app/bin" `
            --workdir '/tmp' `
            $sdkImage `
            '/app/scripts/Test-CmdTool.ps1' `
            -Path "/app/bin/ThirdPartyLibraries.$ToolVersion-$Framework.zip"
    }
}