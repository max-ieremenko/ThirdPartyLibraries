using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace ThirdPartyLibraries.PowerShell.Internal;

internal sealed class DependencyResolverContext : AssemblyLoadContext
{
    private static readonly Action<AssemblyLoadContext> UnloadRef;
    private static readonly ConstructorInfo BaseCtorRef;

    private readonly string _path;
    private readonly ConcurrentDictionary<string, Assembly> _cache;
    private readonly Func<string, Assembly> _getOrAdd;

    static DependencyResolverContext()
    {
        UnloadRef = BindUnload();
        BaseCtorRef = BindBaseCtor();
    }

    public DependencyResolverContext(string path)
    {
        // base(isCollectible: true)
        BaseCtorRef.Invoke(this, new object[] { true });

        _path = path;
        _cache = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        _getOrAdd = GetOrAdd;
    }

    public Assembly TryLoadLocal(AssemblyName assemblyName)
    {
        return _cache.GetOrAdd(assemblyName.Name, _getOrAdd);
    }

    public void InvokeUnload()
    {
        _cache.Clear();
        UnloadRef(this);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        return TryLoadLocal(assemblyName);
    }

    private static Action<AssemblyLoadContext> BindUnload()
    {
        var methodInfo = typeof(AssemblyLoadContext)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(i => "Unload".Equals(i.Name, StringComparison.Ordinal))
            .Where(i => i.ReturnType == typeof(void))
            .FirstOrDefault(i => i.GetParameters().Length == 0);

        if (methodInfo == null)
        {
            throw new InvalidOperationException("Method AssemblyLoadContext.Unload not found.");
        }

        return (Action<AssemblyLoadContext>)methodInfo.CreateDelegate(typeof(Action<AssemblyLoadContext>));
    }

    private static ConstructorInfo BindBaseCtor()
    {
        var ctr = typeof(AssemblyLoadContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .FirstOrDefault(i =>
            {
                var parameters = i.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
            });

        if (ctr == null)
        {
            throw new InvalidOperationException("Method AssemblyLoadContext.ctr(bool) not found.");
        }

        return ctr;
    }

    private Assembly GetOrAdd(string assemblyName)
    {
        var fileName = Path.Combine(_path, assemblyName + ".dll");
        if (File.Exists(fileName))
        {
            return LoadFromAssemblyPath(fileName);
        }

        return null;
    }
}