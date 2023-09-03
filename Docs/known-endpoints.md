Known endpoints
===============

List of known endpoints to which the tool can send a request:

- [opensource.org](https://opensource.org)
- [api.opensource.org](https://api.opensource.org)
- [spdx.org](https://spdx.org)
- [www.codeproject.com](https://www.codeproject.com)
- [github.com](https://github.com)
- [api.github.com](https://api.github.com)
- [raw.github.com](https://raw.github.com)
- [raw.githubusercontent.com](https://raw.githubusercontent.com)
- [nuget.pkg.github.com](https://nuget.pkg.github.com)
- [www.npmjs.com](https://www.npmjs.com)
- [registry.npmjs.org](https://registry.npmjs.org)
- [www.nuget.org](https://www.nuget.org)
- [api.nuget.org](https://api.nuget.org)
- [licenses.nuget.org](https://licenses.nuget.org)
- [www.microsoft.com](https://www.microsoft.com)
- [go.microsoft.com](https://go.microsoft.com)
- [www.apache.org](https://www.apache.org)

opensource.org
--------------

Depending on the license code the tool can send a request to a URL defined in the corresponding `text` section. Media type preferences: `text/plain`, `text/html`, `any other`.

For example, for the [MIT](https://api.opensource.org/license/MIT) license defined in the [index](https://api.opensource.org/licenses), the content will be downloaded from https://opensource.org/license/mit/, for [GPL-3.0](https://api.opensource.org/license/GPL-3.0) from https://www.gnu.org/licenses/gpl-3.0.txt.

codeproject
-----------

The endpoint is used only for downloading [The Code Project Open License (CPOL) 1.02](https://www.codeproject.com/info/cpol10.aspx) content, see [CPOL.zip](https://www.codeproject.com/info/CPOL.zip).

microsoft.com
-------------

By default configuration section `staticLicenseUrls` in the [appsettings.json](ThirdPartyLibraries/configuration/appsettings.json) contains

`MICROSOFT .NET LIBRARY` with following hardcoded URLS:
- https://www.microsoft.com/web/webpi/eula/net_library_eula_enu.htm
- https://www.microsoft.com/en-us/web/webpi/eula/net_library_eula_ENU.htm
- http://go.microsoft.com/fwlink/?LinkId=329770
- http://go.microsoft.com/fwlink/?LinkId=529443

`MICROSOFT SYSTEM CLR TYPES FOR MICROSOFT SQL SERVER 2012`
- https://www.microsoft.com/web/webpi/eula/SysClrTypes_SQLServer.htm
- https://www.microsoft.com/web/webpi/eula/Microsoft_SQL_Server_2012_Microsoft_CLR_ENG.htm
- http://go.microsoft.com/fwlink/?LinkId=331280
  
www.apache.org
--------------

By default configuration section `staticLicenseUrls` in the [appsettings.json](ThirdPartyLibraries/configuration/appsettings.json) contains

`Apache-2.0` with following hardcoded URLS:
- http://www.apache.org/licenses/LICENSE-2.0
- http://www.apache.org/licenses/LICENSE-2.0.html
- http://www.apache.org/licenses/LICENSE-2.0.txt