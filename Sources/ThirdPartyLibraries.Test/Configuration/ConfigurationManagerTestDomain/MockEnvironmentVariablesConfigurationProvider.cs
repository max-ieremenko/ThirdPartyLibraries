using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using ThirdPartyLibraries.Shared;

namespace ThirdPartyLibraries.Configuration.ConfigurationManagerTestDomain
{
    internal sealed class MockEnvironmentVariablesConfigurationProvider : EnvironmentVariablesConfigurationProvider
    {
        public MockEnvironmentVariablesConfigurationProvider(Dictionary<string, string> variables)
            : base(CommandFactory.EnvironmentVariablePrefix)
        {
            Variables = variables;
        }

        public Dictionary<string, string> Variables { get; }

        public override void Load()
        {
            // internal void Load(IDictionary envVariables)
            var methodInfo = typeof(EnvironmentVariablesConfigurationProvider)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(i => "Load".EqualsIgnoreCase(i.Name))
                .Where(i => i.GetParameters().Length == 1)
                .Single(i => i.GetParameters()[0].ParameterType == typeof(IDictionary));

            var load = (Action<IDictionary>)methodInfo.CreateDelegate(typeof(Action<IDictionary>), this);
            load(Variables);
        }
    }
}
