[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ })]
    [string]
    $Bin,

    [Parameter(Mandatory = $true)]
    [ValidateSet("netcoreapp3.1", "net5.0", "net6.0")] 
    [string]
    $Framework
)

$sourceDir = Join-Path $Bin "test/$Framework"
$testList = Get-ChildItem -Path $sourceDir -Filter *.Test.dll ` | ForEach-Object { $_.FullName }

assert ($testList -and $testList.Length) "$Framework test list is empty"

$testList
Exec { dotnet vstest $testList }
