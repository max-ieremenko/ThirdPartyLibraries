using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ThirdPartyLibraries.PowerShell.Internal;

internal sealed class DependencyResolver : IDisposable
{
    private readonly DependencyResolverContext _context;

    public DependencyResolver()
    {
        _context = new DependencyResolverContext(Path.GetDirectoryName(GetType().Assembly.Location));
        AssemblyLoadContext.Default.Resolving += AssemblyResolving;
    }

    public void Dispose()
    {
        AssemblyLoadContext.Default.Resolving -= AssemblyResolving;
        _context.InvokeUnload();
    }

    private Assembly AssemblyResolving(AssemblyLoadContext context, AssemblyName name)
    {
        return _context.TryLoadLocal(name);
    }
}