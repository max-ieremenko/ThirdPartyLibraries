$projectFile = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Sources\ThirdPartyLibraries\ThirdPartyLibraries.csproj"))
$outDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\build-out"))

Exec {
    dotnet pack `
        -c Release `
        -p:PackAsTool=true `
        -p:GlobalTool=true `
        -o $outDir `
        $projectFile
}