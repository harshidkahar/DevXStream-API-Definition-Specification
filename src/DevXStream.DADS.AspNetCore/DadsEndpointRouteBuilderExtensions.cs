using DevXStream.DADS.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
                endpoints.MapMethods(operation.Route, [operation.Method], (Delegate)routeHandler)
                    .WithName(operation.Name)
                    .WithDisplayName($"DADS: {operation.Name}")
                    .WithMetadata(new DadsOperationMetadata(module.Name, operation));
            }
        }

        return endpoints;
    }
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
