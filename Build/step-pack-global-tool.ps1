$projectFile = Join-Path $settings.sources "ThirdPartyLibraries/ThirdPartyLibraries.csproj"

Exec {
    dotnet pack `
        -c Release `
        -p:PackAsTool=true `
        -p:GlobalTool=true `
        -o $settings.output `
        $projectFile
}