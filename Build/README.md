# Build

The repository is configured to run [CI build](https://github.com/max-ieremenko/ThirdPartyLibraries/actions) on any push or pull request into the master branch.

## To run local build

build-locally.ps1 is designed to run on windows. To run CI build locally

- install dependencies

[net8.0 sdk](https://dotnet.microsoft.com/download/dotnet/8.0), 
[InvokeBuild](https://www.powershellgallery.com/packages/InvokeBuild)

    ``` powershell
    PS> ./Build/install-dependencies.ps1
    ```

- install [Node.js](https://nodejs.org/en/download/)

- switch docker to linux containers

- run build

    ``` powershell
    PS> ./Build/build-locally.ps1
    ```
