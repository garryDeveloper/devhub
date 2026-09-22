using System.Reflection;
using DevHub.Application.Auth;
using DevHub.Application.Common;
using DevHub.Application.Common.Behaviors;
using FluentValidation;
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
    /// behaviours — logging, validation, authorization, transaction — are not free: they are
    /// added as decorators over these interfaces as tickets need them. DEVHUB-014 added the
    /// first one, <see cref="ValidationDecorator{TCommand, TResponse}"/>.
    /// </para>
    /// <para>Revisit this only in a ticket; do not mix the two styles.</para>
    /// </remarks>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Handlers are stateless and depend on scoped services (DbContext, ICurrentUser),
        // so they are scoped too.
        services.AddCommandHandlers(assembly);
        services.Scan(assembly, typeof(IQueryHandler<,>));

        // Scoped for the same reason as handlers: a validator may one day need the database
        // (an async "does this project exist" rule), and a singleton cannot hold a scoped service.
        services.Scan(assembly, typeof(IValidator<>));

        // Shared by register and login (DEVHUB-014/015), so both return exactly the same shape.
        services.AddScoped<AuthResponseFactory>();

        return services;
    }

    /// <summary>
    /// Registers each command handler behind a <see cref="ValidationDecorator{TCommand, TResponse}"/>:
    /// resolving <c>ICommandHandler&lt;RegisterCommand, Result&lt;AuthResponse&gt;&gt;</c> hands the
    /// controller the decorator, which holds the real handler as <c>inner</c>.
    /// </summary>
    /// <remarks>
    /// Microsoft.Extensions.DependencyInjection has no built-in decoration, so this does by hand
    /// what Scrutor's <c>Decorate()</c> would: register the concrete handler under its own type,
    /// then register the interface as a factory that wraps it. Only handlers returning a
    /// <see cref="Result"/> are wrapped — the decorator has to be able to return a failure.
    /// </remarks>
    private static void AddCommandHandlers(this IServiceCollection services, Assembly assembly)
    {
        foreach (var (serviceType, implementation) in FindClosedImplementations(assembly, typeof(ICommandHandler<,>)))
        {
            var responseType = serviceType.GetGenericArguments()[1];

            if (!typeof(Result).IsAssignableFrom(responseType))
            {
                services.AddScoped(serviceType, implementation);
                continue;
            }

            var decoratorType = typeof(ValidationDecorator<,>).MakeGenericType(serviceType.GetGenericArguments());

            services.AddScoped(implementation);
            services.AddScoped(serviceType, provider => ActivatorUtilities.CreateInstance(
                provider, decoratorType, provider.GetRequiredService(implementation)));
        }
    }

    /// <summary>
    /// Registers every non-abstract class in <paramref name="assembly"/> against each of the
    /// given open generic interfaces it closes.
    /// </summary>
    private static void Scan(this IServiceCollection services, Assembly assembly, params Type[] openGenericInterfaces)
    {
        foreach (var openGenericInterface in openGenericInterfaces)
        {
            foreach (var (serviceType, implementation) in FindClosedImplementations(assembly, openGenericInterface))
            {
                services.AddScoped(serviceType, implementation);
            }
        }
    }

    private static IEnumerable<(Type ServiceType, Type Implementation)> FindClosedImplementations(
        Assembly assembly, Type openGenericInterface)
    {
        return assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface)
                .Select(i => (i, type)));
    }
}
