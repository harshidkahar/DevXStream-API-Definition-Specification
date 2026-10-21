using System.Collections;
using System.Text.Json;
using DevXStream.DADS.Models;
using YamlDotNet.Serialization;

namespace DevXStream.DADS.Serialization;

public static class DadsDefinitionLoader
{
    public const int MaximumDocumentBytes = 5 * 1024 * 1024;
    public const int MaximumJsonDepth = 64;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static async Task<DadsDefinition> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        var file = new FileInfo(path);
        if (file.Length > MaximumDocumentBytes)
            throw new DadsSerializationException($"The definition exceeds the {MaximumDocumentBytes / 1024 / 1024} MB size limit.");
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return Load(text, Path.GetExtension(path));
    }

    public static DadsDefinition Load(string text, string? extension = ".json")
    {
        EnsureDocumentSize(text);
        var json = extension is ".yaml" or ".yml" ? ConvertYamlToJson(text) : text;
        var parsed = JsonSerializer.Deserialize<DadsDefinition>(json, Options)
            ?? throw new DadsSerializationException("The definition contains no JSON object.");
        return parsed with
        {
            Servers = parsed.Servers ?? [],
            Modules = parsed.Modules ?? [],
            Schemas = parsed.Schemas ?? []
        };
    }

    public static JsonDocument ParseDocument(string text, string? extension = ".json")
    {
        EnsureDocumentSize(text);
        return JsonDocument.Parse(
            extension is ".yaml" or ".yml" ? ConvertYamlToJson(text) : text,
            new JsonDocumentOptions { MaxDepth = MaximumJsonDepth });
    }

    private static string ConvertYamlToJson(string yaml)
    {
        EnsureDocumentSize(yaml);
        var source = new DeserializerBuilder().Build().Deserialize<object>(yaml);
        var json = JsonSerializer.Serialize(NormalizeYaml(source));
        EnsureDocumentSize(json);
        return json;
    }

    private static void EnsureDocumentSize(string text)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(text) > MaximumDocumentBytes)
            throw new DadsSerializationException($"The definition exceeds the {MaximumDocumentBytes / 1024 / 1024} MB size limit.");
    }

    private static object? NormalizeYaml(object? value) => value switch
    {
        null => null,
        IDictionary<object, object> dictionary => dictionary.ToDictionary(entry => Convert.ToString(entry.Key) ?? string.Empty, entry => NormalizeYaml(entry.Value)),
        IDictionary<string, object> dictionary => dictionary.ToDictionary(entry => entry.Key, entry => NormalizeYaml(entry.Value)),
        IDictionary dictionary => dictionary.Cast<DictionaryEntry>().ToDictionary(entry => Convert.ToString(entry.Key) ?? string.Empty, entry => NormalizeYaml(entry.Value)),
        IEnumerable sequence when value is not string => sequence.Cast<object?>().Select(NormalizeYaml).ToList(),
        _ => value
    };
}

public sealed class DadsSerializationException(string message) : Exception(message);
