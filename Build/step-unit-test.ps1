[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [ValidateSet("netcoreapp3.1", "net5.0")] 
    [string]
    $Framework
)

$sourceDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources\bin\test\$Framework"))

$testList = Get-ChildItem -Path $sourceDir -Filter *.Test.dll ` | ForEach-Object {$_.FullName}

if ((-not $testList) -or (-not $testList.Length)) {
    throw ($Framework + " test list is empty.")
}

$testList
dotnet vstest $testList
