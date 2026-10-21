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
    /// <summary>Path to the DADS JSON or YAML definition.</summary>
    public string DefinitionPath { get; set; } = string.Empty;

    /// <summary>Route where the native DADS UI is served.</summary>
    public string UiPath { get; set; } = "/dads";

    /// <summary>Route where the UI reads the loaded DADS definition.</summary>
    public string DefinitionEndpointPath { get; set; } = "/dads/definition.json";

    /// <summary>Assembly containing <see cref="IDadsOperationHandler"/> implementations. Defaults to the entry assembly.</summary>
    public Assembly? HandlerAssembly { get; set; }
}

/// <summary>Provides the one-call DADS ASP.NET Core integration.</summary>
public static class DevXStreamDadsExtensions
{
    /// <summary>Registers the DADS definition and discovers its operation handlers.</summary>
    public static IServiceCollection AddDevXStreamDads(this IServiceCollection services, Action<DevXStreamDadsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new DevXStreamDadsOptions();
        configure(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DefinitionPath);
        ValidateRoute(options.UiPath, nameof(options.UiPath));
        ValidateRoute(options.DefinitionEndpointPath, nameof(options.DefinitionEndpointPath));

        var handlerAssembly = options.HandlerAssembly ?? Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("Unable to discover a DADS handler assembly. Set DevXStreamDadsOptions.HandlerAssembly explicitly.");
        services.AddSingleton(options);
        services.AddDadsDefinition(options.DefinitionPath);
        services.AddDadsOperationHandlersFromAssembly(handlerAssembly);
        return services;
    }

    /// <summary>Maps all DADS operations, the native DADS UI, and its definition endpoint.</summary>
    public static IEndpointRouteBuilder MapDevXStreamDads(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var options = endpoints.ServiceProvider.GetRequiredService<DevXStreamDadsOptions>();
        var definition = endpoints.ServiceProvider.GetRequiredService<DadsDefinition>();

        endpoints.MapDadsEndpoints();
        endpoints.MapGet(options.DefinitionEndpointPath, () => Results.Json(definition, DadsJsonOptions));
        endpoints.MapGet(options.UiPath, () => Results.Content(DadsUi.Html.Replace("__DADS_DEFINITION_URL__", options.DefinitionEndpointPath, StringComparison.Ordinal), "text/html; charset=utf-8"));
        return endpoints;
    }

    private static readonly JsonSerializerOptions DadsJsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static void ValidateRoute(string route, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(route) || !route.StartsWith("/", StringComparison.Ordinal))
            throw new ArgumentException("DADS UI routes must start with '/'.", parameterName);
    }
}
