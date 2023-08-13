using System;
using System.IO;
using System.Reflection;
using RunAsync = System.Func<string, System.Collections.Generic.IList<(string Name, string Value)>, System.Action<string>, System.Action<string>, System.Threading.CancellationToken, System.Threading.Tasks.Task>;

namespace ThirdPartyLibraries.PowerShell.Internal;

internal sealed class DependencyResolver : IDisposable
{
    private readonly DependencyResolverContext _context;

    public DependencyResolver()
    {
        _context = new DependencyResolverContext(Path.GetDirectoryName(GetType().Assembly.Location));
    }

    public RunAsync BindRunAsync()
    {
        var assembly = _context.TryLoadLocal(new AssemblyName("ThirdPartyLibraries"));
        var method = assembly!
            .EntryPoint!
            .DeclaringType!
            .GetMethod(nameof(Program.RunAsync), BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

        var result = method!.CreateDelegate(typeof(RunAsync));
        return (RunAsync)result;
    }

    public void Dispose()
    {
        _context.InvokeUnload();
    }
}