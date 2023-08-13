using Microsoft.Extensions.DependencyInjection;
using ThirdPartyLibraries.Suite.Validate.Internal;

namespace ThirdPartyLibraries.Suite.Validate;

internal static class ValidateCommandModule
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IPackageValidator, PackageValidator>();
        services.AddTransient<IValidationState, ValidationState>();
    }
}