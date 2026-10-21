using System.Text.Json;
using System.Text.Json.Nodes;
using DevXStream.DADS.Models;

namespace DevXStream.DADS.OpenApi;

public static class OpenApiExporter
{
    public static JsonObject Export(DadsDefinition definition)
    {
        var tags = new JsonArray(definition.Modules.Select(module => (JsonNode)new JsonObject { ["name"] = module.Name, ["description"] = module.Description }).ToArray());
        var paths = new JsonObject();
        foreach (var module in definition.Modules)
        foreach (var endpoint in module.Endpoints)
        {
            if (paths[endpoint.Route] is not JsonObject path) { path = []; paths[endpoint.Route] = path; }
            path[endpoint.Method.ToLowerInvariant()] = Operation(endpoint, module.Name);
        }
        return new JsonObject
        {
            ["openapi"] = "3.1.1",
            ["info"] = new JsonObject { ["title"] = definition.Application.Name, ["version"] = definition.Application.Version, ["description"] = definition.Application.Description },
            ["servers"] = new JsonArray(definition.Servers.Select(server => (JsonNode)new JsonObject { ["url"] = server.Url, ["description"] = server.Description }).ToArray()),
            ["tags"] = tags,
            ["paths"] = paths,
            ["components"] = new JsonObject { ["schemas"] = new JsonObject(definition.Schemas.Select(schema => KeyValuePair.Create<string, JsonNode?>(schema.Name, Schema(schema)))) }
        };
    }

    private static JsonObject Operation(OperationDefinition endpoint, string tag)
    {
        var operation = new JsonObject { ["operationId"] = endpoint.Name, ["tags"] = new JsonArray(tag), ["summary"] = endpoint.Summary, ["description"] = endpoint.Description, ["deprecated"] = endpoint.Deprecated ?? false };
        if (endpoint.Parameters?.Count > 0) operation["parameters"] = new JsonArray(endpoint.Parameters.Select(Parameter).ToArray());
        if (endpoint.Request is not null) operation["requestBody"] = Request(endpoint.Request);
        operation["responses"] = new JsonObject(endpoint.Responses.Select(response => KeyValuePair.Create<string, JsonNode?>(response.StatusCode, Response(response))));
        if (endpoint.Engineering is not null) operation["x-devxstream-engineering"] = JsonSerializer.SerializeToNode(endpoint.Engineering);
        return operation;
    }

    private static JsonObject Parameter(ParameterDefinition parameter) => new()
    {
        ["name"] = parameter.Name, ["in"] = parameter.In, ["required"] = parameter.In == "path" || parameter.Required == true, ["description"] = parameter.Description,
        ["schema"] = TypeSchema(parameter.Type), ["example"] = parameter.Example is JsonElement example ? JsonNode.Parse(example.GetRawText()) : null
    };

    private static JsonObject Request(RequestBodyDefinition request) => new()
    {
        ["description"] = request.Description, ["content"] = new JsonObject { [request.ContentType ?? "application/json"] = new JsonObject { ["schema"] = ReferenceOrEmpty(request.Schema), ["example"] = request.Example is JsonElement example ? JsonNode.Parse(example.GetRawText()) : null } }
    };

    private static JsonObject Response(ResponseDefinition response) => new()
    {
        ["description"] = response.Description,
        ["content"] = response.Schema is null && response.Example is null ? null : new JsonObject { [response.ContentType ?? "application/json"] = new JsonObject { ["schema"] = ReferenceOrEmpty(response.Schema), ["example"] = response.Example is JsonElement example ? JsonNode.Parse(example.GetRawText()) : null } }
    };

    private static JsonObject Schema(SchemaDefinition schema)
    {
        var result = TypeSchema(schema.Type ?? "object");
        result["description"] = schema.Description;
        if (schema.Properties is not null) result["properties"] = new JsonObject(schema.Properties.Select(property => KeyValuePair.Create<string, JsonNode?>(property.Key, PropertySchema(property.Value))));
        if (schema.Required?.Count > 0) result["required"] = new JsonArray(schema.Required.Select(value => (JsonNode)value).ToArray());
        if (schema.Enum?.Count > 0) result["enum"] = new JsonArray(schema.Enum.Select(value => JsonNode.Parse(value.GetRawText())).ToArray());
        if (schema.Example is JsonElement example) result["example"] = JsonNode.Parse(example.GetRawText());
        return result;
    }

    private static JsonObject PropertySchema(SchemaProperty property)
    {
        var result = TypeSchema(property.Type ?? "string"); result["format"] = property.Format; result["description"] = property.Description; result["nullable"] = property.Nullable;
        if (property.Example is JsonElement example) result["example"] = JsonNode.Parse(example.GetRawText());
        if (property.Items is not null) result["items"] = PropertySchema(property.Items);
        return result;
    }

    private static JsonObject ReferenceOrEmpty(string? name) => string.IsNullOrWhiteSpace(name) ? new JsonObject() : new JsonObject { ["$ref"] = $"#/components/schemas/{name}" };
    private static JsonObject TypeSchema(string type) => type.ToLowerInvariant() switch { "guid" or "uuid" => new JsonObject { ["type"] = "string", ["format"] = "uuid" }, "int" or "integer" => new JsonObject { ["type"] = "integer" }, "decimal" or "double" or "number" => new JsonObject { ["type"] = "number" }, "bool" or "boolean" => new JsonObject { ["type"] = "boolean" }, _ => new JsonObject { ["type"] = type } };
}
