[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $BinPath,

    [Parameter(Mandatory = $true)]
    [ValidateSet("net6.0", "net7.0", "net8.0")] 
    [string]
    $Framework
)

task Default {
    $path = Join-Path $BinPath "test" $Framework
    $testList = Get-ChildItem -Path $path -Filter *.Test.dll ` | ForEach-Object { $_.FullName }
    
    assert $testList.Count "$Framework test list is empty"
    
    $testList
    exec { dotnet vstest $testList }
}