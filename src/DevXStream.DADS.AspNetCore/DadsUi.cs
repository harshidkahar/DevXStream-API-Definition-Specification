using System.Reflection;
using System.Text;

namespace DevXStream.DADS.AspNetCore;

internal static class DadsUi
{
    private const string ResourcePrefix = "DevXStream.DADS.AspNetCore.Portal.";
    private static readonly Assembly Assembly = typeof(DadsUi).Assembly;

    internal static string Styles { get; } = ReadText("styles.css");
    internal static string Branding { get; } = ReadText("branding.css");
    internal static string Client { get; } = ReadText("client.css");
    internal static byte[] Logo { get; } = ReadBytes("devxstream-logo.png");
    internal static byte[] DadsLogo { get; } = ReadBytes("dads-logo.png");

    internal static string GetHtml(string uiPath) =>
        ReadText("index.html")
            .Replace("./styles.css", $"{uiPath}/styles.css", StringComparison.Ordinal)
            .Replace("./branding.css", $"{uiPath}/branding.css", StringComparison.Ordinal)
            .Replace("./client.css", $"{uiPath}/client.css", StringComparison.Ordinal)
            .Replace("./assets/devxstream-logo.png", $"{uiPath}/assets/devxstream-logo.png", StringComparison.Ordinal)
            .Replace("./assets/dads-logo.png", $"{uiPath}/assets/dads-logo.png", StringComparison.Ordinal)
            .Replace("./app.js", $"{uiPath}/app.js", StringComparison.Ordinal);

    internal static string GetScript(string definitionEndpointPath) =>
        ReadText("app.js").Replace("../samples/ecommerce.dads.json", definitionEndpointPath, StringComparison.Ordinal);

    private static string ReadText(string name) => Encoding.UTF8.GetString(ReadBytes(name));

    private static byte[] ReadBytes(string name)
    {
        using var stream = Assembly.GetManifestResourceStream(ResourcePrefix + name)
            ?? throw new InvalidOperationException($"The native DADS portal resource '{name}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
