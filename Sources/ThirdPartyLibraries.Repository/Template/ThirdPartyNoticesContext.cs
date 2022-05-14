﻿using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template;

public sealed class ThirdPartyNoticesContext
{
    // Application Title, comes from command line
    public string Title { get; set; }

    // list of repository licenses referenced by packages
    public List<ThirdPartyNoticesLicenseContext> Licenses { get; } = new();

    // list of packages
    public List<ThirdPartyNoticesPackageContext> Packages { get; } = new();
}