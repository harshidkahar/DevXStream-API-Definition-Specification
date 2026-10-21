using DevXStream.DADS.Models;
using DevXStream.DADS.OpenApi;
using DevXStream.DADS.Serialization;
using DevXStream.DADS.Validation;
using System.Text.Json.Nodes;

namespace DevXStream.DADS.AspNetCore;

/// <summary>Provides the complete DADS document workflow through the ASP.NET Core package.</summary>
public static class DadsDefinitionDocument
{
    /// <summary>Validates and loads a DADS definition from its text.</summary>
    public static DadsDefinition LoadValidated(string content, string? extension = ".json")
    {
        var result = new DadsValidator().Validate(content, extension);
        if (!result.IsValid) throw new DadsDefinitionValidationException(result.Diagnostics);
        return DadsDefinitionLoader.Load(content, extension);
    }

    /// <summary>Validates and loads a DADS definition from a file.</summary>
    public static async Task<DadsDefinition> LoadValidatedAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return LoadValidated(content, Path.GetExtension(path));
    }

    /// <summary>Exports a loaded DADS definition as OpenAPI 3.1.1 JSON.</summary>
    public static JsonObject ExportOpenApi(DadsDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return OpenApiExporter.Export(definition);
    }
}

/// <summary>Indicates that a DADS document could not be validated.</summary>
public sealed class DadsDefinitionValidationException(IReadOnlyList<DadsDiagnostic> diagnostics)
    : InvalidOperationException("The DADS definition is invalid.")
{
    /// <summary>The validation diagnostics that prevented loading.</summary>
    public IReadOnlyList<DadsDiagnostic> Diagnostics { get; } = diagnostics;
}
