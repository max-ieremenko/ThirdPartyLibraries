ThirdPartyLibraries
===================

[![NuGet](https://img.shields.io/nuget/v/ThirdPartyLibraries.GlobalTool.svg?style=flat-square&label=nuget%20dotnet%20tool)](https://www.nuget.org/packages/ThirdPartyLibraries.GlobalTool/)
[![GitHub release](https://img.shields.io/github/release/max-ieremenko/ThirdPartyLibraries.svg?style=flat-square&label=manual%20download)](https://github.com/max-ieremenko/ThirdPartyLibraries/releases)

This command line tool helps to manage third party libraries and their licenses in .net applications.

This is done by creating and managing a repository. [Here](ThirdPartyLibraries) is the library repository of ThirdPartyLibraries.

The tool makes a source code analyze to collect third party references in your application.

Table of Contents
-----------------

<!-- toc -->

- [Requirements](#requirements)
- [Installation](#installation)
- [Create or update a libraries repository](#update)
- [Refresh or update .md files in the libraries repository](#refresh)
- [Validate repository](#validate)
- [Generate ThirdPartyNotices.txt out of the libraries repository](#generate)
- [Configuration](#configuration)
- [GitHub personal access token](#personalAccessToken)
- [Manage licenses](#licenses)
- [Manage NuGet packages](#nuget.org")
- [Manage custom packages](#custom)
- [License](#license)

<!-- tocstop -->

Requirements <a name="requirements"></a>
----------------------------------------

- to run the tool .NET Core 3.0
- supported project format: [SDK-style](https://docs.microsoft.com/en-us/nuget/resources/check-project-format)
- supported project references: NuGet packages
- non-nuget references can me managed via [custom packages](#custom)


[Back to ToC](#table-of-contents)

Installation
------------

Dotnet tool

```bash
$ dotnet tool install --global ThirdPartyLibraries.GlobalTool
```

or download from [latest release](https://github.com/max-ieremenko/ThirdPartyLibraries/releases)

[![GitHub release](https://img.shields.io/github/release/max-ieremenko/ThirdPartyLibraries.svg?style=flat-square&label=manual%20download)](https://github.com/max-ieremenko/ThirdPartyLibraries/releases)

[Back to ToC](#table-of-contents)

Create or update a libraries repository <a name="update"></a>
--------------------------------------------------------------------

For the demo purpose you can use ThirdPartyLibraries sources.

```bash
$ git clone https://github.com/max-ieremenko/ThirdPartyLibraries.git c:\ThirdPartyLibraries
```

> **Important:**
> restore NuGet packages or build the solution. It is a precondition.

```bash
$ dotnet restore c:\ThirdPartyLibraries\Sources
```

Run the the tool

```bash
$ ThirdPartyLibraries update -appName ThirdPartyLibraries -source c:\ThirdPartyLibraries\Sources -repository c:\RepositoryDemo
```

**Important:** if the tool stops with error

```text
Forbidden: Forbidden
----------------
{"message":"API rate limit exceeded for [ip address]. (But here's the good news: Authenticated requests get a higher rate limit. Check out the documentation for more details.)","documentation_url":"https://developer.github.com/v3/#rate-limiting"}
```

Most of the NuGet packages are referenced to GitHub. In order to resolve license information the tools makes a number of requests to GitHub web api, wich has [the rate limit to 60 requests per hour](https://developer.github.com/v3/#rate-limiting) for unauthenticated requests.

To authenticated requests please follow instruction in the section [GitHub personal access token](#personalAccessToken) and re-start *ThirdPartyLibraries update*.

Commit and push c:\RepositoryDemo into GitHub or BitBucket, it helps you to easy read generated .md files and navigate, you should see a picture like [this](ThirdPartyLibraries).

[Back to ToC](#table-of-contents)

Refresh or update .md files in the libraries repository <a name="refresh"></a>
-------------------------------------------------

- file [configuration/readme-template.txt](ThirdPartyLibraries/configuration/readme-template.txt) contains [DotLiquid template](https://shopify.github.io/liquid/) to generate the main [readme.md](ThirdPartyLibraries/readme.md), context is [RootReadMePackageContext.cs](Sources/ThirdPartyLibraries.Repository/Template/RootReadMePackageContext.cs)
- file [configuration/nuget.org-readme-template.txt](ThirdPartyLibraries/configuration/nuget.org-readme-template.txt) contains [DotLiquid template](https://shopify.github.io/liquid/)  to generate [readme.md](ThirdPartyLibraries/readme.md) for NuGet packages, for instance [newtonsoft.json/12.0.2](ThirdPartyLibraries/packages/nuget.org/newtonsoft.json/readme.md), context is [NuGetReadMeContext.cs](Sources/ThirdPartyLibraries.Repository/Template/NuGetReadMeContext.cs)

You can change templates and test your changes by runing the the tool

```bash
$ ThirdPartyLibraries refresh -appName ThirdPartyLibraries -repository c:\RepositoryDemo
```

[Back to ToC](#table-of-contents)

Validate repository <a name="validate"></a>
-------------------------------------------------

To validate sources against a library repository run the tool

```bash
$ ThirdPartyLibraries validate -appName ThirdPartyLibraries -source c:\ThirdPartyLibraries\Sources -repository c:\RepositoryDemo
```

The tool reports to the current output about any inconsistency between sources and repository or if TODO list in the repository is not empty, for example

```text
Error: Following libraries are not approved:
   Newtonsoft.Json 12.0.2 from nuget.org
   NUnit 3.12.0 from nuget.org
```

[Back to ToC](#table-of-contents)

Generate ThirdPartyNotices.txt out of the libraries repository <a name="generate"></a>
-------------------------------------------------

```bash
$ ThirdPartyLibraries generate -appName ThirdPartyLibraries -repository c:\RepositoryDemo -to c:\notices
```

[DotLiquid template](https://shopify.github.io/liquid/) for ThirdPartyNotices.txt is [configuration/third-party-notices-template.txt](ThirdPartyLibraries/configuration/third-party-notices-template.txt), context is [ThirdPartyNoticesContext.cs](Sources/ThirdPartyLibraries.Repository/Template/ThirdPartyNoticesContext.cs)

*third-party-notices-template.txt* will be created after the first run *ThirdPartyLibraries generate*.

[Back to ToC](#table-of-contents)

Configuration <a name="configuration"></a>
---------------------------------------------

The configuration file [appsettings.json](ThirdPartyLibraries/configuration/appsettings.json) is located in the repository configuration folder

```json
{
  "nuget.org": {
    "allowToUseLocalCache": true,
    "ignorePackages": {
      "byName": [],
      "byProjectName":  [] 
    },
    "internalPackages": {
      "byName": [ "StyleCop\\.Analyzers" ],
      "byProjectName": [ "\\.Test$" ]
    }
  },
  "github.com": {
    "personalAccessToken": ""
  }
}
```

|Attribute|Description
|:--|:----------|
|nuget.org/allowToUseLocalCache|*true* or *false* flag to allow get package metadata from NuGet local [disk cache](https://docs.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders)|
|nuget.org/ignorePackages/byName|regex expressions array. Ignore all packages from source code by name|
|nuget.org/ignorePackages/byProjectName|regex expressions array. Ignore all packages from source code by project name|
|nuget.org/internalPackages/byName|regex expressions array. Mark all packages from source code by name as InternalOnly=true|
|nuget.org/internalPackages/byProjectName|regex expressions array. Mark all packages from source code by project name as InternalOnly=true|
|github.com/personalAccessToken|see [GitHub personal access token](#personalAccessToken) for more details|

[Back to ToC](#table-of-contents)

GitHub personal access token <a name="personalAccessToken"></a>
---------------------------------------------

Most of the NuGet packages are referenced to GitHub. In order to resolve license information the tools makes a number of requests to GitHub web api.

Api has [the rate limit to 60 requests per hour](https://developer.github.com/v3/#rate-limiting) for unauthenticated requests.

To authentificate requests
1. Create personal access token, details are [here](https://help.github.com/en/github/authenticating-to-github/creating-a-personal-access-token-for-the-command-line)
> for the token leave all scopes and permissions blank
2. Run the tool with the token

Options how to pass token to the tool
- set the value in the [configuration file](ThirdPartyLibraries/configuration/appsettings.json), github.com<span></span>/personalAccessToken
- set the value via [secret manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.0&tabs=windows)
```bash
$ dotnet user-secrets set --id c903410c-3d05-49fe-bc8b-b95a2f4dfc69 "github.com:personalAccessToken" "token"
```
- pass the value via environment variable *ThirdPartyLibraries:github.com:personalAccessToken*
```bash
$ SET ThirdPartyLibraries:github.com:personalAccessToken=token
$ ThirdPartyLibraries update ...
```
- pass the value via command line
```bash
$ ThirdPartyLibraries update ... -github.com:personalAccessToken token
```

[Back to ToC](#table-of-contents)

Manage licenses <a name="licenses"></a>
---------------------------------------------

Each license is located in the sub-folder *licenses/[code]* where [code] is license code in lowercase.
For instance [license/mit](ThirdPartyLibraries/licenses/mit).

File *index.json* contains a metadata for the tool:

```json
{
  "Code": "MIT",
  "FullName": "MIT License",
  "RequiresApproval": true,
  "HRef": "https://spdx.org/licenses/MIT",
  "FileName": "license.txt"
}
```

|Attribute|Description|
|:--|:----------|
|Code|license code, for some web api is case sensitive|
|FullName|full license name, is used to generate third party notices|
|RequiresApproval|*true* or *false* (default) flag to indicate the package can be marked by the tool as AutomaticallyApproved|
|HRef|public link to the license information, is used to generate third party notices|
|FileName|name of the file in this folder with a copy of the license text, is used to generate third party notices|

Such license information can be created either automatically by the tool or manually.
Once created, the folder becomes read-only for the tool.

Please feel free to tailor the content according to your needs.
Only 2 restrictions:

- *index.json* structure cannot be changed
- the folder name *licenses/[code]* in lowercase and *index.json/Code*

[Back to ToC](#table-of-contents)

Manage NuGet packages <a name="nuget.org"></a>
---------------------------------------------

Each package from [nuget.org](https://www.nuget.org/) is located in the sub-folder *packages/nuget.org/[id]/[version]* where [id] is a package id and [version] is a package version in lowercase.
For instance [newtonsoft.json/12.0.2](ThirdPartyLibraries/packages/nuget.org/newtonsoft.json/12.0.2).

File *index.json* contains a metadata for the tool:

```json
{
  "License": {
    "Code": "MIT",
    "Status": "HasToBeApproved | Approved | AutomaticallyApproved"
  },
  "UsedBy": [
    {
      "Name": "ThirdPartyLibraries",
      "InternalOnly": false,
      // ...
    }
  ],
  "Licenses": [
      // ...
  ]
}
```

|Attribute|Description|Is read-only for the tool|
|:--|:----------|:--|
|License/Code|license code is one of the licenses from folder *license* or *null* if license cannot be resolved|yes if value is not *null*|
|License/Status|*acceptance status* of this package. HasToBeApproved (TODO), Approved (can be set only manually), AutomaticallyApproved (assigned by the tool according to the license code and license/RequiresApproval)|if value is Approved|
|UsedBy/Name|a name of application references this package, see [*$ ThirdPartyLibraries update -appName ThirdPartyLibraries*](#update)|no, is always updated according to a configuration|
|UsedBy/InternalOnly|*true* or *false* (default) flag to indicate is this package is a part of third party notices, see [*$ ThirdPartyLibraries generate -appName ThirdPartyLibraries*](#generate)|no, is always updated according to a configuration|
|Licenses/...|section with a list of license from [package.nuspec](ThirdPartyLibraries/packages/nuget.org/newtonsoft.json/12.0.2/package.nuspec)|yes if License/Code is not *null*|

- file *package.nuspec* is a NuGet package specification. Once created, is read-only for the tool.
- file *readme.md* is always generated by the tool.
- file *remarks.md* is read-only for the tool and contains a content of *Remarks* section for *readme.md*.

[Back to ToC](#table-of-contents)

Manage custom packages <a name="custom"></a>
---------------------------------------------

Each custom package is located in the sub-folder *packages/custom/[name]/[version]* where [name] is a package name and [version] is a package version in lowercase.
For instance [FAMFAMFAM/1.3](ThirdPartyLibraries/packages/custom/famfamfam/1.3).

Any custom package has to be created manually. The folder is read-only for the tool.

File *index.json* contains a metadata for the tool:

```json
{
  "Name": "FAMFAMFAM",
  "Version": "1.3",
  "LicenseCode": "CC-BY-2.5 OR CC-BY-3.0",
  "HRef": "http://www.famfamfam.com/",
  "Author": "Mark James",
  "Copyright": "Copyright (c) Mark James",
  "UsedBy": [
    {
      "Name": "ThirdPartyLibraries",
      "InternalOnly": false
    }
  ]
}
```

|Attribute|Description
|:--|:----------|
|Name|package name|
|Version|package version|
|LicenseCode|license code is one of the licenses from folder *license* or *null* if license cannot be resolved|
|HRef|any public link to the package information, is used to generate third party notices|
|Author|package author(s), is used to generate third party notices|
|Copyright|copyright(s), is used to generate third party notices|
|UsedBy/Name|a name of application references this package|
|UsedBy/InternalOnly|*true* or *false* flag to indicate is this package is a part of third party notices, see [*$ ThirdPartyLibraries generate -appName ThirdPartyLibraries*](#generate)|

[Back to ToC](#table-of-contents)

License
-------

This tool is distributed under the [MIT](LICENSE) license.

[Back to ToC](#table-of-contents)