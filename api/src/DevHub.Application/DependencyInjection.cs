using System.Reflection;
using DevHub.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace DevHub.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers everything the Application layer owns.
    /// </summary>
    /// <remarks>
    /// DECISION (DEVHUB-002) — dispatch mechanism: <b>plain handler classes, not MediatR</b>.
    /// <para>
    /// Controllers inject the concrete handler interface they need
    /// (<c>ICommandHandler&lt;ChangeIssueStatusCommand, IssueDto&gt;</c>) and call
    /// <c>HandleAsync</c>. There is no mediator, no runtime type resolution, no marker
    /// assembly scanning beyond the registration below.
    /// </para>
    /// <para>
    /// Why: the dependency is visible in the constructor, "go to definition" lands on the
    /// handler instead of on <c>IMediator.Send</c>, and the project takes no third-party
    /// dependency that no ticket requires (CLAUDE.md §7). The cost is that MediatR's pipeline
    /// behaviours — logging, validation, authorization, transaction — are not free: they will
    /// be added as decorators over these interfaces when EPIC 2 first needs them.
    /// </para>
    /// <para>Revisit this only in a ticket; do not mix the two styles.</para>
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Handlers are stateless and depend on scoped services (DbContext, ICurrentUser),
        // so they are scoped too.
        services.Scan(
            Assembly.GetExecutingAssembly(),
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>));

        return services;
    }

    /// <summary>
    /// Registers every non-abstract class in <paramref name="assembly"/> against each of the
    /// given open generic interfaces it closes.
    /// </summary>
    private static void Scan(this IServiceCollection services, Assembly assembly, params Type[] openGenericInterfaces)
    {
        foreach (var implementation in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            var serviceTypes = implementation.GetInterfaces()
                .Where(i => i.IsGenericType && openGenericInterfaces.Contains(i.GetGenericTypeDefinition()));

            foreach (var serviceType in serviceTypes)
            {
                services.AddScoped(serviceType, implementation);
            }
        }
    }
}
