using DevXStream.DADS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DevXStream.DADS.AspNetCore;

/// <summary>Registers DADS definitions for ASP.NET Core applications.</summary>
public static class DadsServiceCollectionExtensions
{
    /// <summary>Validates, loads, and registers one DADS definition from a file path.</summary>
    public static IServiceCollection AddDadsDefinition(this IServiceCollection services, string path)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return services.AddSingleton<DadsDefinition>(_ => DadsDefinitionDocument.LoadValidatedAsync(path).GetAwaiter().GetResult());
    }
}
