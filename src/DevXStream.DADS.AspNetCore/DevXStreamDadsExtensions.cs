using System.Reflection;
using System.Text.Json;
using DevXStream.DADS.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DevXStream.DADS.AspNetCore;

/// <summary>Configures a complete DADS API runtime and its native documentation UI.</summary>
public sealed class DevXStreamDadsOptions
{
    /// <summary>Optional path to a curated DADS JSON or YAML definition.</summary>
    public string DefinitionPath { get; set; } = string.Empty;

    /// <summary>Chooses the definition source. Auto first uses a registered provider, then a valid file, then runtime discovery.</summary>
    public DadsDefinitionSource DefinitionSource { get; set; } = DadsDefinitionSource.Auto;

    /// <summary>Allows runtime endpoint discovery when an optional custom provider or definition file is unavailable or invalid.</summary>
    public bool EnableRuntimeFallback { get; set; } = true;

    /// <summary>Route where the native DADS UI is served.</summary>
    public string UiPath { get; set; } = "/dads";

    /// <summary>Route where the UI reads the loaded DADS definition.</summary>
    public string DefinitionEndpointPath { get; set; } = "/dads/definition.json";

    /// <summary>Assembly containing <see cref="IDadsOperationHandler"/> implementations. Defaults to the entry assembly.</summary>
    public Assembly? HandlerAssembly { get; set; }

    /// <summary>Determines whether DADS maps handlers or validates endpoints already mapped by the application.</summary>
    public DadsEndpointMode EndpointMode { get; set; } = DadsEndpointMode.AutoDiscoverHandlers;
}

/// <summary>Determines how an ASP.NET Core application implements DADS operations.</summary>
public enum DadsEndpointMode
{
    /// <summary>Maps DADS operations to discovered <see cref="IDadsOperationHandler"/> classes.</summary>
    AutoDiscoverHandlers,
    /// <summary>Uses existing controller or Minimal API endpoints and verifies them against the DADS contract.</summary>
    ExistingApplicationEndpoints
}

/// <summary>Provides the one-call DADS ASP.NET Core integration.</summary>
public static class DevXStreamDadsExtensions
{
    /// <summary>Registers DADS using runtime endpoint discovery and the default portal routes.</summary>
    public static IServiceCollection AddDevXStreamDads(this IServiceCollection services) =>
        services.AddDevXStreamDads(_ => { });

    /// <summary>Registers the DADS definition and discovers its operation handlers.</summary>
    public static IServiceCollection AddDevXStreamDads(this IServiceCollection services, Action<DevXStreamDadsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new DevXStreamDadsOptions();
        configure(options);
        ValidateRoute(options.UiPath, nameof(options.UiPath));
        ValidateRoute(options.DefinitionEndpointPath, nameof(options.DefinitionEndpointPath));

        services.AddSingleton(options);
        services.AddSingleton<DadsDefinitionResolver>();
        services.AddSingleton<IDadsDefinitionResolver>(provider => provider.GetRequiredService<DadsDefinitionResolver>());
        if (options.EndpointMode == DadsEndpointMode.AutoDiscoverHandlers)
        {
            var handlerAssembly = options.HandlerAssembly ?? Assembly.GetEntryAssembly()
                ?? throw new InvalidOperationException("Unable to discover a DADS handler assembly. Set DevXStreamDadsOptions.HandlerAssembly explicitly.");
            services.AddDadsOperationHandlersFromAssembly(handlerAssembly);
        }
        return services;
    }

    /// <summary>Maps all DADS operations, the native DADS UI, and its definition endpoint.</summary>
    public static IEndpointRouteBuilder MapDevXStreamDads(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var options = endpoints.ServiceProvider.GetRequiredService<DevXStreamDadsOptions>();
        var resolution = endpoints.ServiceProvider.GetRequiredService<DadsDefinitionResolver>().Resolve(endpoints);

        if (resolution.Definition is not null && resolution.Source != DadsDefinitionSource.Runtime)
        {
            if (options.EndpointMode == DadsEndpointMode.AutoDiscoverHandlers) endpoints.MapDadsEndpoints(resolution.Definition, operation =>
            {
                var registrations = endpoints.ServiceProvider.GetServices<DadsHandlerRegistration>().ToDictionary(item => item.HandlerName, StringComparer.Ordinal);
                var name = operation.Operation.Engineering?.Handler ?? operation.Operation.Name;
                return registrations.TryGetValue(name, out var registration) ? (httpContext, context) => ((IDadsOperationHandler)httpContext.RequestServices.GetRequiredService(registration.HandlerType)).HandleAsync(httpContext, context) : null;
            });
            else endpoints.ValidateDadsEndpoints(resolution.Definition);
        }
        endpoints.MapGet(options.DefinitionEndpointPath, () => resolution.Definition is null
            ? Results.Problem("DADS definition is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable, extensions: new Dictionary<string, object?> { ["diagnostics"] = resolution.Diagnostics.Select(d => new { d.RuleId, severity = d.Severity.ToString(), d.JsonPointer, d.Message }) })
            : Results.Json(resolution.Definition, DadsJsonOptions));
        endpoints.MapGet(options.UiPath, () => Results.Content(DadsUi.GetHtml(options.UiPath), "text/html; charset=utf-8"));
        endpoints.MapGet($"{options.UiPath}/styles.css", () => Results.Content(DadsUi.Styles, "text/css; charset=utf-8"));
        endpoints.MapGet($"{options.UiPath}/branding.css", () => Results.Content(DadsUi.Branding, "text/css; charset=utf-8"));
        endpoints.MapGet($"{options.UiPath}/client.css", () => Results.Content(DadsUi.Client, "text/css; charset=utf-8"));
        endpoints.MapGet($"{options.UiPath}/app.js", () => Results.Content(DadsUi.GetScript(options.DefinitionEndpointPath), "text/javascript; charset=utf-8"));
        endpoints.MapGet($"{options.UiPath}/assets/devxstream-logo.png", () => Results.File(DadsUi.Logo, "image/png"));
        endpoints.MapGet($"{options.UiPath}/assets/dads-logo.png", () => Results.File(DadsUi.DadsLogo, "image/png"));
        endpoints.MapGet($"{options.UiPath}/assets/dads-wordmark.png", () => Results.File(DadsUi.DadsWordmark, "image/png"));
        endpoints.MapGet($"{options.UiPath}/assets/dads-wordmark-black.png", () => Results.File(DadsUi.DadsWordmarkBlack, "image/png"));
        endpoints.MapGet($"{options.UiPath}/assets/dads-wordmark-black-full.png", () => Results.File(DadsUi.DadsWordmarkBlackFull, "image/png"));
        return endpoints;
    }

    /// <summary>Verifies that currently mapped Minimal API or controller routes implement every DADS operation.</summary>
    public static IEndpointRouteBuilder ValidateDadsEndpoints(this IEndpointRouteBuilder endpoints, DadsDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(definition);
        var applicationOperations = endpoints.DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                .Select(method => (Method: method, Route: NormalizeRoute(endpoint.RoutePattern.RawText))) ?? [])
            .ToHashSet();
        var missing = definition.Modules.SelectMany(module => module.Endpoints)
            .Where(operation => !applicationOperations.Contains((operation.Method.ToUpperInvariant(), NormalizeRoute(operation.Route))))
            .Select(operation => $"{operation.Method.ToUpperInvariant()} {operation.Route}").ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException($"The application does not map these DADS operations: {string.Join(", ", missing)}.");
        return endpoints;
    }

    private static readonly JsonSerializerOptions DadsJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static void ValidateRoute(string route, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(route) || !route.StartsWith("/", StringComparison.Ordinal))
            throw new ArgumentException("DADS UI routes must start with '/'.", parameterName);
    }

    private static string NormalizeRoute(string? route)
    {
        var value = string.IsNullOrWhiteSpace(route) ? "/" : route;
        return value.StartsWith("/", StringComparison.Ordinal) ? value : "/" + value;
    }
}
