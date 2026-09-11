using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevHub.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the adapters behind the Application layer's ports: persistence (DEVHUB-006),
    /// identity (EPIC 2), storage and AWS clients (EPIC 15). This is the only place where a
    /// concrete implementation is bound to an interface, and the only reason DevHub.Api
    /// references DevHub.Infrastructure at all.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return services;
    }
}
