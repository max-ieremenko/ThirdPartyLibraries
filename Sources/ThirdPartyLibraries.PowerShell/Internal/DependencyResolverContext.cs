using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ThirdPartyLibraries.PowerShell.Internal;

internal sealed class DependencyResolverContext : AssemblyLoadContext
{
    private static readonly Action<AssemblyLoadContext> UnloadRef;
    private static readonly ConstructorInfo BaseCtorRef;

    private readonly string _path;
    private readonly ConcurrentDictionary<string, Assembly?> _cache;
    private readonly Func<string, Assembly?> _getOrAdd;
    private readonly IDictionary<string, string> _localFileByAssemblyName;

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
        _cache = new ConcurrentDictionary<string, Assembly?>(StringComparer.OrdinalIgnoreCase);
        _getOrAdd = GetOrAdd;
        _localFileByAssemblyName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        LoadLocalFiles();
    }

    public Assembly? TryLoadLocal(AssemblyName assemblyName)
    {
        return _cache.GetOrAdd(assemblyName.Name, _getOrAdd);
    }

    public void InvokeUnload()
    {
        _cache.Clear();
        UnloadRef(this);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        return TryLoadLocal(assemblyName);
    }

    private static Action<AssemblyLoadContext> BindUnload()
    {
        var methods = typeof(AssemblyLoadContext)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        MethodInfo? unload = null;
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if ("Unload".Equals(method.Name, StringComparison.Ordinal)
                && method.ReturnType == typeof(void)
                && method.GetParameters().Length == 0)
            {
                unload = method;
                break;
            }
        }

        if (unload == null)
        {
            throw new InvalidOperationException("Method AssemblyLoadContext.Unload not found.");
        }

        return (Action<AssemblyLoadContext>)unload.CreateDelegate(typeof(Action<AssemblyLoadContext>));
    }

    private static ConstructorInfo BindBaseCtor()
    {
        var ctors = typeof(AssemblyLoadContext)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        ConstructorInfo? isCollectible = null;
        for (var i = 0; i < ctors.Length; i++)
        {
            var ctor = ctors[i];
            var parameters = ctor.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(bool))
            {
                isCollectible = ctor;
                break;
            }
        }

        if (isCollectible == null)
        {
            throw new InvalidOperationException("Method AssemblyLoadContext.ctr(bool) not found.");
        }

        return isCollectible;
    }

    private Assembly? GetOrAdd(string assemblyName)
    {
        if (_localFileByAssemblyName.TryGetValue(assemblyName, out var fileName)
            && File.Exists(fileName))
        {
            return LoadFromAssemblyPath(fileName);
        }

        return null;
    }

    private void LoadLocalFiles()
    {
        var files = Directory.GetFiles(_path);
        for (var i = 0; i < files.Length; i++)
        {
            var fullName = files[i];
            if (!".dll".Equals(Path.GetExtension(fullName), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(fullName);
            _localFileByAssemblyName.Add(name, fullName);
        }
    }
}