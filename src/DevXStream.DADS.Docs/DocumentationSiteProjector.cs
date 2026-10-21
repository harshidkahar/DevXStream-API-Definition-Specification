using System.Text.Json;
using DevXStream.DADS.Models;

namespace DevXStream.DADS.Docs;

public sealed record DocumentationSiteModel(
    DocumentationApplication Application,
    IReadOnlyList<DocumentationNavigationGroup> Navigation,
    IReadOnlyList<DocumentationSchema> Schemas);

public sealed record DocumentationApplication(string Name, string Version, string? Description, string? Framework, string? Architecture, IReadOnlyList<ServerDefinition> Servers);
public sealed record DocumentationNavigationGroup(string Name, string? Description, int? Order, IReadOnlyList<DocumentationOperation> Operations);
public sealed record DocumentationOperation(string Id, string Name, string Method, string Route, string Summary, string? Description, bool Deprecated, IReadOnlyList<ParameterDefinition> Parameters, RequestBodyDefinition? Request, IReadOnlyList<ResponseDefinition> Responses, EngineeringMetadata? Engineering, IReadOnlyDictionary<string, string> RequestSnippets);
public sealed record DocumentationSchema(string Name, string? Description, string? Type, IReadOnlyDictionary<string, SchemaProperty>? Properties, IReadOnlyList<string>? Required, IReadOnlyList<JsonElement>? Enum, JsonElement? Example);

public static class DocumentationSiteProjector
{
    public static DocumentationSiteModel Project(DadsDefinition definition)
    {
        var server = definition.Servers.FirstOrDefault()?.Url;
        var navigation = definition.Modules
            .OrderBy(module => module.Order ?? int.MaxValue)
            .ThenBy(module => module.Name, StringComparer.OrdinalIgnoreCase)
            .Select(module => new DocumentationNavigationGroup(
                module.Name,
                module.Description,
                module.Order,
                module.Endpoints.Select(endpoint => new DocumentationOperation(
                    $"{Slug(module.Name)}-{Slug(endpoint.Name)}",
                    endpoint.Name,
                    endpoint.Method,
                    endpoint.Route,
                    endpoint.Summary,
                    endpoint.Description,
                    endpoint.Deprecated ?? false,
                    endpoint.Parameters ?? [],
                    endpoint.Request,
                    endpoint.Responses,
                    endpoint.Engineering,
                    RequestSnippetGenerator.Generate(endpoint, server))).ToList())).ToList();
        return new DocumentationSiteModel(
            new DocumentationApplication(definition.Application.Name, definition.Application.Version, definition.Application.Description, definition.Application.Framework, definition.Application.Architecture, definition.Servers),
            navigation,
            definition.Schemas.Select(schema => new DocumentationSchema(schema.Name, schema.Description, schema.Type, schema.Properties, schema.Required, schema.Enum, schema.Example)).ToList());
    }

    private static string Slug(string value) => string.Concat(value.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-')).Trim('-');
}
