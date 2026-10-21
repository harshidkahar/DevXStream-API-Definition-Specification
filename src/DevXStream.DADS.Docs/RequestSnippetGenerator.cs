using System.Text.Json;
using DevXStream.DADS.Models;

namespace DevXStream.DADS.Docs;

public static class RequestSnippetGenerator
{
    public static IReadOnlyDictionary<string, string> Generate(OperationDefinition operation, string? serverUrl)
    {
        var url = $"{(serverUrl ?? "https://api.example.com").TrimEnd('/')}{operation.Route}";
        var payload = operation.Request?.Example is JsonElement example ? example.GetRawText() : null;
        var contentType = operation.Request?.ContentType ?? "application/json";
        var curl = "curl -X " + operation.Method + " '" + url + "'" + (payload is null ? string.Empty : " -H 'Content-Type: " + contentType + "' --data '" + payload.Replace("'", "\\'", StringComparison.Ordinal) + "'");
        var http = $"{operation.Method} {url} HTTP/1.1\nAccept: application/json" + (payload is null ? string.Empty : $"\nContent-Type: {contentType}\n\n{payload}");
        var csharp = $"using var client = new HttpClient();\nusing var request = new HttpRequestMessage(HttpMethod.{ToCSharpMethod(operation.Method)}, \"{url}\");" + (payload is null ? string.Empty : $"\nrequest.Content = JsonContent.Create({payload});") + "\nusing var response = await client.SendAsync(request);";
        var javascript = "const response = await fetch(\"" + url + "\", { method: \"" + operation.Method + "\"" + (payload is null ? string.Empty : ", headers: { \"Content-Type\": \"" + contentType + "\" }, body: JSON.stringify(" + payload + ")") + " });";
        var python = "response = requests.request(\"" + operation.Method + "\", \"" + url + "\"" + (payload is null ? string.Empty : ", json=" + payload) + ")";
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["curl"] = curl,
            ["http"] = http,
            ["csharp"] = csharp,
            ["javascript"] = javascript,
            ["python"] = python
        };
    }

    private static string ToCSharpMethod(string method) => method.ToUpperInvariant() switch
    {
        "GET" => "Get",
        "POST" => "Post",
        "PUT" => "Put",
        "PATCH" => "Patch",
        "DELETE" => "Delete",
        "HEAD" => "Head",
        "OPTIONS" => "Options",
        _ => "Get"
    };
}
