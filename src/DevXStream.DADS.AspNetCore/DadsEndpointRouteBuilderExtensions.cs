using DevXStream.DADS.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DevXStream.DADS.AspNetCore;

/// <summary>Maps DADS operations to ASP.NET Core minimal API endpoints.</summary>
public static class DadsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps every operation in a DADS definition. The resolver is called once for each
    /// operation and must return the application handler for that operation.
    /// </summary>
    public static IEndpointRouteBuilder MapDadsEndpoints(
        this IEndpointRouteBuilder endpoints,
        DadsDefinition definition,
        Func<DadsOperationContext, DadsEndpointHandler?> handlerResolver)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(handlerResolver);

        foreach (var module in definition.Modules)
        {
            foreach (var operation in module.Endpoints)
            {
                var context = new DadsOperationContext(definition, module, operation);
                var handler = handlerResolver(context)
                    ?? throw new InvalidOperationException($"No ASP.NET Core handler was registered for DADS operation '{operation.Name}'.");

                Func<HttpContext, Task<IResult>> routeHandler = httpContext => handler(httpContext, context);
                MapOperation(endpoints, module, operation, routeHandler);
            }
        }

        return endpoints;
    }

    /// <summary>Maps registered minimal API handlers without requiring a switch resolver or null fallback.</summary>
    public static IEndpointRouteBuilder MapDadsEndpoints(
        this IEndpointRouteBuilder endpoints,
        DadsDefinition definition,
        Action<DadsEndpointHandlers> configure)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(configure);

        var handlers = new DadsEndpointHandlers();
        configure(handlers);
        var operationNames = definition.Modules.SelectMany(module => module.Endpoints).Select(operation => operation.Name).ToHashSet(StringComparer.Ordinal);
        var unknownHandlers = handlers.OperationNames.Where(name => !operationNames.Contains(name)).ToArray();
        if (unknownHandlers.Length > 0)
            throw new InvalidOperationException($"DADS handlers were registered for undefined operations: {string.Join(", ", unknownHandlers)}.");

        foreach (var module in definition.Modules)
        foreach (var operation in module.Endpoints)
            MapOperation(endpoints, module, operation, handlers.GetRequired(operation.Name));

        return endpoints;
    }

    /// <summary>Maps handlers for the DADS definition registered with <c>AddDadsDefinition</c>.</summary>
    public static IEndpointRouteBuilder MapDadsEndpoints(this IEndpointRouteBuilder endpoints, Action<DadsEndpointHandlers> configure) =>
        endpoints.MapDadsEndpoints(endpoints.ServiceProvider.GetRequiredService<DadsDefinition>(), configure);

    /// <summary>
    /// Maps all operations using discovered <see cref="IDadsOperationHandler"/> classes.
    /// Handler classes are matched to each operation's <c>engineering.handler</c> value,
    /// or its operation name when engineering metadata does not specify a handler.
    /// </summary>
    public static IEndpointRouteBuilder MapDadsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var registrations = endpoints.ServiceProvider.GetServices<DadsHandlerRegistration>().ToArray();
        var duplicateNames = registrations.GroupBy(registration => registration.HandlerName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        if (duplicateNames.Length > 0)
            throw new InvalidOperationException($"Multiple DADS handlers were registered with the same name: {string.Join(", ", duplicateNames)}.");

        var handlersByName = registrations.ToDictionary(registration => registration.HandlerName, StringComparer.Ordinal);
        return endpoints.MapDadsEndpoints(endpoints.ServiceProvider.GetRequiredService<DadsDefinition>(), operation =>
        {
            var handlerName = operation.Operation.Engineering?.Handler ?? operation.Operation.Name;
            if (!handlersByName.TryGetValue(handlerName, out var registration))
                return null;

            return (httpContext, dadsOperation) =>
                ((IDadsOperationHandler)httpContext.RequestServices.GetRequiredService(registration.HandlerType))
                    .HandleAsync(httpContext, dadsOperation);
        });
    }

    /// <summary>Exposes the registered DADS definition as an OpenAPI 3.1.1 document.</summary>
    public static IEndpointConventionBuilder MapDadsOpenApi(this IEndpointRouteBuilder endpoints, string pattern = "/openapi.json")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var definition = endpoints.ServiceProvider.GetRequiredService<DadsDefinition>();
        return endpoints.MapGet(pattern, () => Results.Text(DadsDefinitionDocument.ExportOpenApi(definition).ToJsonString(), "application/json"));
    }

    private static void MapOperation(IEndpointRouteBuilder endpoints, ModuleDefinition module, OperationDefinition operation, Delegate handler) =>
        endpoints.MapMethods(operation.Route, [operation.Method], handler)
            .WithName(operation.Name)
            .WithDisplayName($"DADS: {operation.Name}")
            .WithMetadata(new DadsOperationMetadata(module.Name, operation));
}

/// <summary>Registers minimal API handlers by DADS operation name.</summary>
public sealed class DadsEndpointHandlers
{
    private readonly Dictionary<string, Delegate> handlers = new(StringComparer.Ordinal);

    /// <summary>Associates a DADS operation name with a standard minimal API handler.</summary>
    public DadsEndpointHandlers Map<TDelegate>(string operationName, TDelegate handler) where TDelegate : Delegate
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(handler);
        if (!handlers.TryAdd(operationName, handler)) throw new InvalidOperationException($"A handler is already registered for DADS operation '{operationName}'.");
        return this;
    }

    internal IEnumerable<string> OperationNames => handlers.Keys;
    internal Delegate GetRequired(string operationName) => handlers.TryGetValue(operationName, out var handler)
        ? handler
        : throw new InvalidOperationException($"No ASP.NET Core handler was registered for DADS operation '{operationName}'.");
}

/// <summary>Supplies the DADS definition and operation metadata to an endpoint handler.</summary>
public sealed record DadsOperationContext(
    DadsDefinition Definition,
    ModuleDefinition Module,
    OperationDefinition Operation);

/// <summary>Handles a DADS-backed HTTP operation.</summary>
public delegate Task<IResult> DadsEndpointHandler(HttpContext httpContext, DadsOperationContext operation);

/// <summary>Endpoint metadata that preserves the source DADS module and operation.</summary>
public sealed record DadsOperationMetadata(string ModuleName, OperationDefinition Operation);
