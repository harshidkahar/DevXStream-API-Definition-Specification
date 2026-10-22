using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevXStream.DADS.Models;

public sealed record DadsDefinition(
    string DefinitionVersion,
    ApplicationDefinition Application,
    IReadOnlyList<ServerDefinition> Servers,
    IReadOnlyList<ModuleDefinition> Modules,
    IReadOnlyList<SchemaDefinition> Schemas,
    IReadOnlyDictionary<string, SecuritySchemeDefinition>? SecuritySchemes = null)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extensions { get; init; } = [];
}

public sealed record ApplicationDefinition(string Name, string Version, string? Description, string? Framework, string? Architecture);
public sealed record ServerDefinition(string Url, string? Description);
public sealed record ModuleDefinition(string Name, string? Description, IReadOnlyList<OperationDefinition> Endpoints, int? Order = null);
public sealed record OperationDefinition(
    string Name,
    string Method,
    string Route,
    string Summary,
    string? Description,
    bool? Deprecated,
    IReadOnlyList<ParameterDefinition>? Parameters,
    RequestBodyDefinition? Request,
    IReadOnlyList<ResponseDefinition> Responses,
    EngineeringMetadata? Engineering,
    IReadOnlyList<string>? Security = null);
public sealed record ParameterDefinition(string Name, string In, string Type, bool? Required, string? Description, JsonElement? Example);
public sealed record RequestBodyDefinition(string? Schema, string? Description, string? ContentType, JsonElement? Example);
public sealed record ResponseDefinition(string StatusCode, string Description, string? Schema, string? ContentType, JsonElement? Example);
public sealed record SecuritySchemeDefinition(string Type, string? Scheme, string? BearerFormat, string? In, string? Name, string? Description);
public sealed record SchemaDefinition(string Name, string? Description, string? Type, IReadOnlyDictionary<string, SchemaProperty>? Properties, IReadOnlyList<string>? Required, IReadOnlyList<JsonElement>? Enum, JsonElement? Example);
public sealed record SchemaProperty(string? Type, string? Format, string? Description, bool? Nullable, JsonElement? Example, SchemaProperty? Items);
public sealed record EngineeringMetadata(string? Cqrs, string? Handler, string? Authorization, string? Cache, string? Transaction, string? Persistence, string? Validation);
