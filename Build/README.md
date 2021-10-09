# Build

The repository is configured to run [CI build](https://github.com/max-ieremenko/ThirdPartyLibraries/actions) on any push or pull request into the master branch.

## Local build

To run CI build locally

- install [InvokeBuild](https://www.powershellgallery.com/packages/InvokeBuild)

    ``` powershell
    PS> Install-Module -Name InvokeBuild
    ```

- install [Node.js](https://nodejs.org/en/download/)

- run build

    ``` powershell
    PS> .\Build\build-locally.ps1
    ```
