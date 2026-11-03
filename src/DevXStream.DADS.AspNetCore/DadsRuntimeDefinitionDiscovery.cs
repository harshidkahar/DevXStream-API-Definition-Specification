using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using DevXStream.DADS.Models;
using DevXStream.DADS.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using System.Text.Json.Serialization;

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
        var schemas = new RuntimeSchemaBuilder();
        foreach (var endpoint in endpoints.DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>())
        {
            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
            var route = NormalizeRoute(endpoint.RoutePattern.RawText);
            if (methods.Count == 0
                || endpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>() is not null
                || route.StartsWith("/dads", StringComparison.OrdinalIgnoreCase)
                || route.Equals("/openapi.json", StringComparison.OrdinalIgnoreCase)) continue;
            var module = ModuleName(endpoint);
            foreach (var method in methods.Where(IsSupportedMethod))
            {
                var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                var name = OperationName(endpoint, action, method, route);
                var secured = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null && endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any();
                var parameters = DiscoverParameters(route, method, action, schemas, out var request);
                operations.Add((module, new OperationDefinition(name, method.ToUpperInvariant(), route, name, null, null, parameters, request,
                    [new ResponseDefinition("200", "Successful response", null, null, null)], null, secured ? ["bearerAuth"] : [])));
            }
        }

        var modules = operations.GroupBy(item => item.Module, StringComparer.OrdinalIgnoreCase).Select((group, index) =>
            new ModuleDefinition(group.Key, null, group.Select(item => item.Operation).ToArray(), index)).ToArray();
        var entry = Assembly.GetEntryAssembly()?.GetName();
        return new DadsDefinition("DADS 1.0", new ApplicationDefinition(entry?.Name ?? "ASP.NET Core API", entry?.Version?.ToString() ?? "1.0.0", null, null, null), [], modules, schemas.Schemas,
            operations.Any(item => item.Operation.Security?.Count > 0)
                ? new Dictionary<string, SecuritySchemeDefinition> { ["bearerAuth"] = new("http", "bearer", "JWT", null, null, "Discovered bearer authentication") }
                : null);
    }

    private static string OperationName(RouteEndpoint endpoint, ControllerActionDescriptor? action, string method, string route)
    {
        var explicitName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
        if (!string.IsNullOrWhiteSpace(explicitName)) return explicitName;
        if (action is not null)
        {
            var routeName = route.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault(segment => !segment.Contains('{', StringComparison.Ordinal));
            if (!string.IsNullOrWhiteSpace(routeName)) return routeName;
            if (!string.IsNullOrWhiteSpace(action.ActionName)) return action.ActionName;
        }
        return $"{method.ToUpperInvariant()} {route}";
    }

    private static IReadOnlyList<ParameterDefinition> DiscoverParameters(string route, string method, ControllerActionDescriptor? action, RuntimeSchemaBuilder schemas, out RequestBodyDefinition? request)
    {
        var parameters = RouteToken.Matches(route).Select(match => match.Groups["name"].Value)
            .ToDictionary(name => name, name => new ParameterDefinition(name, "path", "string", true, null, null), StringComparer.OrdinalIgnoreCase);
        request = null;
        if (action is null) return parameters.Values.ToArray();

        foreach (var parameter in action.Parameters)
        {
            var name = parameter.BindingInfo?.BinderModelName ?? parameter.Name;
            if (string.IsNullOrWhiteSpace(name)) continue;
            var source = parameter.BindingInfo?.BindingSource;
            var location = parameters.ContainsKey(name) || source == BindingSource.Path ? "path" : source == BindingSource.Header ? "header" : source == BindingSource.Query ? "query" : null;
            var inferBody = source is null && IsBodyMethod(method) && RuntimeSchemaBuilder.IsComplex(parameter.ParameterType);
            if (source == BindingSource.Body || inferBody)
            {
                var schema = schemas.Add(parameter.ParameterType);
                request = new RequestBodyDefinition(schema, $"{name} request body", "application/json", schemas.Example(parameter.ParameterType));
                continue;
            }
            if (location is null) continue;
            var isPath = string.Equals(location, "path", StringComparison.Ordinal);
            parameters[name] = new ParameterDefinition(name, location, RuntimeSchemaBuilder.TypeName(parameter.ParameterType), isPath, null, null);
        }
        return parameters.Values.ToArray();
    }

    private static bool IsBodyMethod(string method) => method.Equals("POST", StringComparison.OrdinalIgnoreCase) || method.Equals("PUT", StringComparison.OrdinalIgnoreCase) || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);

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

    private sealed class RuntimeSchemaBuilder
    {
        private readonly Dictionary<Type, string> _names = [];
        private readonly Dictionary<string, SchemaDefinition> _schemas = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<SchemaDefinition> Schemas => _schemas.Values.ToArray();

        public string? Add(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (IsSimple(type)) return null;
            if (TryGetElementType(type, out var element)) return Add(element);
            if (_names.TryGetValue(type, out var existing)) return existing;

            var name = type.Name.Split('`')[0];
            var suffix = 2;
            while (_schemas.ContainsKey(name)) name = type.Name.Split('`')[0] + suffix++;
            _names[type] = name;
            _schemas[name] = new SchemaDefinition(name, null, "object", new Dictionary<string, SchemaProperty>(), [], null, null);

            var properties = new Dictionary<string, SchemaProperty>(StringComparer.Ordinal);
            var required = new List<string>();
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && property.GetCustomAttribute<JsonIgnoreAttribute>() is null))
            {
                var propertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
                properties[propertyName] = Property(property.PropertyType);
                if (Nullable.GetUnderlyingType(property.PropertyType) is null && property.PropertyType.IsValueType) required.Add(propertyName);
            }
            _schemas[name] = new SchemaDefinition(name, null, "object", properties, required, null, Example(type));
            return name;
        }

        public static string TypeName(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum) return "string";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) || type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong)) return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
            if (type == typeof(Guid)) return "guid";
            if (type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "string";
            if (TryGetElementType(type, out _)) return "array";
            return IsSimple(type) ? "string" : "object";
        }

        public JsonElement Example(Type type) => JsonSerializer.SerializeToElement(Value(type, new HashSet<Type>(), 0), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        private SchemaProperty Property(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (TryGetElementType(type, out var element)) return new SchemaProperty("array", null, null, true, null, Property(element));
            return new SchemaProperty(TypeName(type), type == typeof(Guid) ? "uuid" : null, null, !type.IsValueType || Nullable.GetUnderlyingType(type) is not null, IsSimple(type) ? Example(type) : null, null);
        }

        private static object? Value(Type type, HashSet<Type> visited, int depth)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return false;
            if (type == typeof(Guid)) return "00000000-0000-0000-0000-000000000000";
            if (type == typeof(DateOnly)) return "2026-01-01";
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "2026-01-01T00:00:00Z";
            if (type.IsEnum) return Enum.GetNames(type).FirstOrDefault() ?? "value";
            if (type.IsPrimitive || type == typeof(decimal)) return 0;
            if (TryGetElementType(type, out var element)) return new[] { Value(element, visited, depth + 1) };
            if (depth >= 3 || !visited.Add(type)) return new Dictionary<string, object?>();
            var value = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
                .ToDictionary(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name), property => Value(property.PropertyType, visited, depth + 1));
            visited.Remove(type);
            return value;
        }

        internal static bool IsComplex(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return !IsSimple(type) && !TryGetElementType(type, out _);
        }

        private static bool IsSimple(Type type) => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(DateTime) || type == typeof(DateTimeOffset);

        private static bool TryGetElementType(Type type, out Type element)
        {
            if (type.IsArray) { element = type.GetElementType()!; return true; }
            if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type.GetGenericArguments().Length == 1) { element = type.GetGenericArguments()[0]; return true; }
            element = null!;
            return false;
        }
    }
}
