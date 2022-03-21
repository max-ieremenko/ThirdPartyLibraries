# Build

The repository is configured to run [CI build](https://github.com/max-ieremenko/ThirdPartyLibraries/actions) on any push or pull request into the master branch.

## Local build

To run CI build locally

- install [InvokeBuild](https://www.powershellgallery.com/packages/InvokeBuild)

    ``` powershell
    PS> Install-Module -Name InvokeBuild -RequiredVersion 5.9.9.0
    ```

- install [Node.js](https://nodejs.org/en/download/)

- install net6.0 sdk: manual [download](https://dotnet.microsoft.com/download/dotnet/6.0) or

    ``` powershell
    PS> .\Build\step-install-dotnet.ps1
    ```

- run build

    ``` powershell
    PS> .\Build\build-locally.ps1
    ```
