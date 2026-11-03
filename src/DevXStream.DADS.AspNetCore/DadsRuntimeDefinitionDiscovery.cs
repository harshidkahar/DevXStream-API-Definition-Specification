using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using DevXStream.DADS.Models;
using DevXStream.DADS.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace DevXStream.DADS.AspNetCore;

/// <summary>Provides an optional application supplied DADS definition.</summary>
public interface IDadsDefinitionProvider
{
    /// <summary>Builds the definition used by the DADS portal.</summary>
    DadsDefinition GetDefinition();
}

/// <summary>Describes how the definition used by the DADS portal was obtained.</summary>
public enum DadsDefinitionSource { Auto, File, Runtime, CustomProvider }

/// <summary>Describes the definition currently available to DADS endpoints.</summary>
public sealed record DadsDefinitionResolution(
    DadsDefinition? Definition,
    DadsDefinitionSource Source,
    IReadOnlyList<DadsDiagnostic> Diagnostics)
{
    /// <summary>Whether a valid definition is available.</summary>
    public bool IsAvailable => Definition is not null;
}

/// <summary>Resolves the DADS document used by runtime endpoints.</summary>
public interface IDadsDefinitionResolver
{
    /// <summary>Returns the validated definition and its source state.</summary>
    DadsDefinitionResolution Resolve(IEndpointRouteBuilder endpoints);
}

internal sealed class DadsDefinitionResolver(DevXStreamDadsOptions options, IServiceProvider services) : IDadsDefinitionResolver
{
    private static readonly Regex RouteToken = new("{(?<name>[A-Za-z][A-Za-z0-9_]*)(?:[^}]*)}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private DadsDefinitionResolution? _resolution;

    public DadsDefinitionResolution Resolve(IEndpointRouteBuilder endpoints)
    {
        if (_resolution is not null) return _resolution;
        var diagnostics = new List<DadsDiagnostic>();

        if (options.DefinitionSource is DadsDefinitionSource.Auto or DadsDefinitionSource.CustomProvider)
        {
            var provider = services.GetService(typeof(IDadsDefinitionProvider)) as IDadsDefinitionProvider;
            if (provider is not null)
            {
                try { return _resolution = Validate(provider.GetDefinition(), DadsDefinitionSource.CustomProvider, diagnostics); }
                catch (DadsDefinitionValidationException exception) { diagnostics.AddRange(exception.Diagnostics); }
                catch (Exception) { diagnostics.Add(new("DADS002", DiagnosticSeverity.Error, "/", "Custom DADS definition provider failed.")); }
            }
            else if (options.DefinitionSource == DadsDefinitionSource.CustomProvider)
                diagnostics.Add(new("DADS003", DiagnosticSeverity.Error, "/", "A custom DADS definition provider was requested but none is registered."));
        }

        if (options.DefinitionSource is DadsDefinitionSource.Auto or DadsDefinitionSource.File)
        {
            if (!string.IsNullOrWhiteSpace(options.DefinitionPath))
            {
                try
                {
                    if (!File.Exists(options.DefinitionPath))
                        diagnostics.Add(new("DADS004", DiagnosticSeverity.Error, "/", "The configured DADS definition file was not found."));
                    else
                        return _resolution = Validate(DadsDefinitionDocument.LoadValidatedAsync(options.DefinitionPath).GetAwaiter().GetResult(), DadsDefinitionSource.File, diagnostics);
                }
                catch (DadsDefinitionValidationException exception) { diagnostics.AddRange(exception.Diagnostics); }
                catch (Exception) { diagnostics.Add(new("DADS005", DiagnosticSeverity.Error, "/", "The configured DADS definition file could not be loaded.")); }
            }
            else if (options.DefinitionSource == DadsDefinitionSource.File)
                diagnostics.Add(new("DADS004", DiagnosticSeverity.Error, "/", "DefinitionSource is File but DefinitionPath was not configured."));
        }

        if (options.DefinitionSource == DadsDefinitionSource.Runtime || options.EnableRuntimeFallback)
        {
            try { return _resolution = Validate(Discover(endpoints), DadsDefinitionSource.Runtime, diagnostics); }
            catch (DadsDefinitionValidationException exception) { diagnostics.AddRange(exception.Diagnostics); }
            catch (Exception) { diagnostics.Add(new("DADS006", DiagnosticSeverity.Error, "/", "Runtime DADS discovery failed.")); }
        }

        return _resolution = new DadsDefinitionResolution(null, options.DefinitionSource, diagnostics);
    }

    private static DadsDefinitionResolution Validate(DadsDefinition definition, DadsDefinitionSource source, List<DadsDiagnostic> diagnostics)
    {
        var text = JsonSerializer.Serialize(definition, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var validated = DadsDefinitionDocument.LoadValidated(text);
        return new DadsDefinitionResolution(validated, source, diagnostics);
    }

    private static DadsDefinition Discover(IEndpointRouteBuilder endpoints)
    {
        var operations = new List<(string Module, OperationDefinition Operation)>();
        foreach (var endpoint in endpoints.DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>())
        {
            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
            var route = NormalizeRoute(endpoint.RoutePattern.RawText);
            if (methods.Count == 0 || route.StartsWith("/dads", StringComparison.OrdinalIgnoreCase) || route.Equals("/openapi.json", StringComparison.OrdinalIgnoreCase)) continue;
            var module = ModuleName(endpoint);
            var displayName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? endpoint.DisplayName;
            foreach (var method in methods.Where(IsSupportedMethod))
            {
                var name = string.IsNullOrWhiteSpace(displayName) ? $"{method.ToUpperInvariant()} {route}" : displayName!;
                var secured = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null && endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any();
                var parameters = RouteToken.Matches(route).Select(match => new ParameterDefinition(match.Groups["name"].Value, "path", "string", true, null, null)).ToArray();
                operations.Add((module, new OperationDefinition(name, method.ToUpperInvariant(), route, name, null, null, parameters, null,
                    [new ResponseDefinition("200", "Successful response", null, null, null)], null, secured ? ["bearerAuth"] : [])));
            }
        }

        var modules = operations.GroupBy(item => item.Module, StringComparer.OrdinalIgnoreCase).Select((group, index) =>
            new ModuleDefinition(group.Key, null, group.Select(item => item.Operation).ToArray(), index)).ToArray();
        var entry = Assembly.GetEntryAssembly()?.GetName();
        return new DadsDefinition("DADS 1.0", new ApplicationDefinition(entry?.Name ?? "ASP.NET Core API", entry?.Version?.ToString() ?? "1.0.0", null, null, null), [], modules, [],
            operations.Any(item => item.Operation.Security?.Count > 0)
                ? new Dictionary<string, SecuritySchemeDefinition> { ["bearerAuth"] = new("http", "bearer", "JWT", null, null, "Discovered bearer authentication") }
                : null);
    }

    private static string ModuleName(RouteEndpoint endpoint)
    {
        var group = endpoint.Metadata.GetMetadata<IEndpointGroupNameMetadata>()?.EndpointGroupName;
        if (!string.IsNullOrWhiteSpace(group)) return group;
        var tags = endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags;
        if (tags?.FirstOrDefault() is { Length: > 0 } tag) return tag;
        if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is { } action) return action.ControllerName;
        return "General";
    }

    private static bool IsSupportedMethod(string method) => method.Equals("GET", StringComparison.OrdinalIgnoreCase) || method.Equals("POST", StringComparison.OrdinalIgnoreCase) || method.Equals("PUT", StringComparison.OrdinalIgnoreCase) || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase) || method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) || method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) || method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRoute(string? route)
    {
        var value = string.IsNullOrWhiteSpace(route) ? "/" : route;
        value = value.StartsWith("/", StringComparison.Ordinal) ? value : "/" + value;
        return RouteToken.Replace(value, match => "{" + match.Groups["name"].Value + "}");
    }
}
