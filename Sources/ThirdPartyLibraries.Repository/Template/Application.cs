using System;
using System.Collections.Generic;

namespace ThirdPartyLibraries.Repository.Template
{
    public sealed class Application
    {
        public string Name { get; set; }

        public bool InternalOnly { get; set; }

        public string[] TargetFrameworks { get; set; } = Array.Empty<string>();

        public IList<LibraryDependency> Dependencies { get; } = new List<LibraryDependency>();
    }
}