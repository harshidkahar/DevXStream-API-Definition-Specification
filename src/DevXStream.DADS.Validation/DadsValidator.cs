using System.Text.Json;
using System.Text.RegularExpressions;
using DevXStream.DADS.Models;
using DevXStream.DADS.Serialization;

namespace DevXStream.DADS.Validation;

public sealed class DadsValidator
{
    private const int MaximumModules = 250;
    private const int MaximumEndpointsPerModule = 1_000;
    private const int MaximumSchemas = 2_000;
    private static readonly HashSet<string> HttpMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];
    private static readonly Regex StatusCode = new("^(?:[1-5][0-9]{2}|default)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex RouteParameter = new("{(?<name>[A-Za-z][A-Za-z0-9_]*)}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SupportedDefinitionVersion = new("^DADS 1\\.[0-9]+(?:\\.[0-9]+)?(?:-[A-Za-z0-9.-]+)?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public ValidationResult Validate(string text, string? extension = ".json")
    {
        var diagnostics = new List<DadsDiagnostic>();
        JsonDocument document;
        try { document = DadsDefinitionLoader.ParseDocument(text, extension); }
        catch (Exception exception) { return new ValidationResult([new("DADS000", DiagnosticSeverity.Error, "/", $"Invalid document syntax: {exception.Message}")]); }
        using (document) { ValidateStructure(document.RootElement, diagnostics); }
        if (diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)) return new ValidationResult(diagnostics);
        try { ValidateSemantic(DadsDefinitionLoader.Load(text, extension), diagnostics); }
        catch (Exception exception) { diagnostics.Add(new("DADS001", DiagnosticSeverity.Error, "/", $"Definition cannot be read: {exception.Message}")); }
        return new ValidationResult(diagnostics);
    }

    private static void ValidateStructure(JsonElement root, List<DadsDiagnostic> diagnostics)
    {
        if (root.ValueKind != JsonValueKind.Object) { diagnostics.Add(new("DADS100", DiagnosticSeverity.Error, "/", "A DADS definition must be a JSON object.")); return; }
        Require(root, "definitionVersion", "/definitionVersion", diagnostics, JsonValueKind.String);
        Require(root, "application", "/application", diagnostics, JsonValueKind.Object);
        Require(root, "modules", "/modules", diagnostics, JsonValueKind.Array);
        if (root.TryGetProperty("modules", out var rootModules) && rootModules.ValueKind == JsonValueKind.Array && rootModules.GetArrayLength() > MaximumModules)
            diagnostics.Add(new("DADS106", DiagnosticSeverity.Error, "/modules", $"A definition may contain at most {MaximumModules} modules."));
        if (root.TryGetProperty("schemas", out var schemas) && schemas.ValueKind == JsonValueKind.Array && schemas.GetArrayLength() > MaximumSchemas)
            diagnostics.Add(new("DADS107", DiagnosticSeverity.Error, "/schemas", $"A definition may contain at most {MaximumSchemas} schemas."));
        if (root.TryGetProperty("application", out var application) && application.ValueKind == JsonValueKind.Object) { Require(application, "name", "/application/name", diagnostics, JsonValueKind.String); Require(application, "version", "/application/version", diagnostics, JsonValueKind.String); }
        if (root.TryGetProperty("definitionVersion", out var definitionVersion) && definitionVersion.ValueKind == JsonValueKind.String && !SupportedDefinitionVersion.IsMatch(definitionVersion.GetString() ?? string.Empty))
            diagnostics.Add(new("DADS110", DiagnosticSeverity.Error, "/definitionVersion", "This tool supports DADS major version 1 only."));
        if (root.TryGetProperty("modules", out var modules) && modules.ValueKind == JsonValueKind.Array)
            for (var i = 0; i < modules.GetArrayLength(); i++) ValidateModule(modules[i], $"/modules/{i}", diagnostics);
    }

    private static void ValidateModule(JsonElement module, string pointer, List<DadsDiagnostic> diagnostics)
    {
        if (module.ValueKind != JsonValueKind.Object) { diagnostics.Add(new("DADS101", DiagnosticSeverity.Error, pointer, "A module must be an object.")); return; }
        Require(module, "name", $"{pointer}/name", diagnostics, JsonValueKind.String);
        Require(module, "endpoints", $"{pointer}/endpoints", diagnostics, JsonValueKind.Array);
        if (module.TryGetProperty("endpoints", out var endpointsForLimit) && endpointsForLimit.ValueKind == JsonValueKind.Array && endpointsForLimit.GetArrayLength() > MaximumEndpointsPerModule)
            diagnostics.Add(new("DADS108", DiagnosticSeverity.Error, $"{pointer}/endpoints", $"A module may contain at most {MaximumEndpointsPerModule} endpoints."));
        if (module.TryGetProperty("endpoints", out var endpoints) && endpoints.ValueKind == JsonValueKind.Array)
            for (var i = 0; i < endpoints.GetArrayLength(); i++) ValidateEndpoint(endpoints[i], $"{pointer}/endpoints/{i}", diagnostics);
    }

    private static void ValidateEndpoint(JsonElement endpoint, string pointer, List<DadsDiagnostic> diagnostics)
    {
        if (endpoint.ValueKind != JsonValueKind.Object) { diagnostics.Add(new("DADS102", DiagnosticSeverity.Error, pointer, "An endpoint must be an object.")); return; }
        foreach (var property in new[] { "name", "method", "route", "summary" }) Require(endpoint, property, $"{pointer}/{property}", diagnostics, JsonValueKind.String);
        Require(endpoint, "responses", $"{pointer}/responses", diagnostics, JsonValueKind.Array);
        if (endpoint.TryGetProperty("route", out var route) && route.ValueKind == JsonValueKind.String && !route.GetString()!.StartsWith("/", StringComparison.Ordinal)) diagnostics.Add(new("DADS103", DiagnosticSeverity.Error, $"{pointer}/route", "A route must begin with '/'."));
        foreach (var textProperty in new[] { "name", "route", "summary", "description" })
            if (endpoint.TryGetProperty(textProperty, out var text) && text.ValueKind == JsonValueKind.String && ContainsUnsafeControlCharacter(text.GetString()))
                diagnostics.Add(new("DADS109", DiagnosticSeverity.Error, $"{pointer}/{textProperty}", "Text fields must not contain control characters."));
    }

    private static void Require(JsonElement parent, string name, string pointer, List<DadsDiagnostic> diagnostics, JsonValueKind kind)
    {
        if (!parent.TryGetProperty(name, out var property)) diagnostics.Add(new("DADS104", DiagnosticSeverity.Error, pointer, $"Required property '{name}' is missing."));
        else if (property.ValueKind != kind) diagnostics.Add(new("DADS105", DiagnosticSeverity.Error, pointer, $"Property '{name}' must be a {kind.ToString().ToLowerInvariant()}."));
    }

    private static void ValidateSemantic(DadsDefinition definition, List<DadsDiagnostic> diagnostics)
    {
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var operations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var schemas = definition.Schemas.Select(schema => schema.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var moduleIndex = 0; moduleIndex < definition.Modules.Count; moduleIndex++)
        {
            var module = definition.Modules[moduleIndex];
            var modulePointer = $"/modules/{moduleIndex}";
            if (!modules.Add(module.Name)) diagnostics.Add(new("DADS200", DiagnosticSeverity.Error, $"{modulePointer}/name", $"Module '{module.Name}' is duplicated."));
            for (var endpointIndex = 0; endpointIndex < module.Endpoints.Count; endpointIndex++)
            {
                var endpoint = module.Endpoints[endpointIndex];
                var pointer = $"{modulePointer}/endpoints/{endpointIndex}";
                if (!HttpMethods.Contains(endpoint.Method)) diagnostics.Add(new("DADS201", DiagnosticSeverity.Error, $"{pointer}/method", $"HTTP method '{endpoint.Method}' is not supported."));
                if (!operations.Add($"{endpoint.Method}:{endpoint.Route}")) diagnostics.Add(new("DADS202", DiagnosticSeverity.Error, pointer, $"Operation '{endpoint.Method} {endpoint.Route}' is duplicated."));
                ValidateRouteParameters(endpoint, pointer, diagnostics);
                foreach (var response in endpoint.Responses)
                {
                    if (!StatusCode.IsMatch(response.StatusCode)) diagnostics.Add(new("DADS203", DiagnosticSeverity.Error, $"{pointer}/responses", $"Response status '{response.StatusCode}' is invalid."));
                    ValidateSchemaReference(response.Schema, schemas, $"{pointer}/responses", diagnostics);
                }
                ValidateSchemaReference(endpoint.Request?.Schema, schemas, $"{pointer}/request/schema", diagnostics);
            }
        }
        foreach (var duplicate in definition.Schemas.GroupBy(schema => schema.Name, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1)) diagnostics.Add(new("DADS204", DiagnosticSeverity.Error, "/schemas", $"Schema '{duplicate.Key}' is duplicated."));
    }

    private static void ValidateRouteParameters(OperationDefinition endpoint, string pointer, List<DadsDiagnostic> diagnostics)
    {
        var routeNames = RouteParameter.Matches(endpoint.Route).Select(match => match.Groups["name"].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pathParameters = (endpoint.Parameters ?? []).Where(parameter => string.Equals(parameter.In, "path", StringComparison.OrdinalIgnoreCase)).ToDictionary(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var routeName in routeNames)
        {
            if (!pathParameters.TryGetValue(routeName, out var parameter)) diagnostics.Add(new("DADS205", DiagnosticSeverity.Error, $"{pointer}/parameters", $"Route parameter '{routeName}' has no matching path parameter."));
            else if (parameter.Required is not true) diagnostics.Add(new("DADS206", DiagnosticSeverity.Error, $"{pointer}/parameters", $"Path parameter '{routeName}' must be required."));
        }
        foreach (var name in pathParameters.Keys.Where(name => !routeNames.Contains(name))) diagnostics.Add(new("DADS207", DiagnosticSeverity.Error, $"{pointer}/parameters", $"Path parameter '{name}' does not occur in the route."));
    }

    private static void ValidateSchemaReference(string? name, HashSet<string> schemas, string pointer, List<DadsDiagnostic> diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(name) && !schemas.Contains(name)) diagnostics.Add(new("DADS208", DiagnosticSeverity.Error, pointer, $"Schema '{name}' is not defined."));
    }

    private static bool ContainsUnsafeControlCharacter(string? value) => value?.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t') == true;
}
