using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DevXStream.DADS.AspNetCore;

/// <summary>Implements one DADS operation using the handler name declared in <c>engineering.handler</c>.</summary>
public interface IDadsOperationHandler
{
    /// <summary>Handles the current HTTP request for its DADS operation.</summary>
    Task<IResult> HandleAsync(HttpContext httpContext, DadsOperationContext operation);
}

/// <summary>Overrides the DADS <c>engineering.handler</c> name used to discover a handler class.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DadsOperationHandlerAttribute(string handlerName) : Attribute
{
    /// <summary>The DADS handler name represented by the decorated class.</summary>
    public string HandlerName { get; } = string.IsNullOrWhiteSpace(handlerName)
        ? throw new ArgumentException("A DADS handler name is required.", nameof(handlerName))
        : handlerName;
}

/// <summary>Registers DADS operation handler classes by scanning an application assembly.</summary>
public static class DadsOperationHandlerServiceCollectionExtensions
{
    /// <summary>Registers generated DADS handler implementations from the assembly containing <typeparamref name="TMarker"/>.</summary>
    public static IServiceCollection AddDadsGeneratedOperationHandlersFromAssemblyOf<TMarker>(this IServiceCollection services) =>
        services.AddDadsGeneratedOperationHandlersFromAssembly(typeof(TMarker).Assembly);

    /// <summary>Registers generated DADS handler implementations from an assembly.</summary>
    public static IServiceCollection AddDadsGeneratedOperationHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.DefinedTypes.Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IDadsGeneratedOperationHandler).IsAssignableFrom(type)))
        {
            foreach (var contract in type.ImplementedInterfaces.Where(contract => contract != typeof(IDadsGeneratedOperationHandler) && typeof(IDadsGeneratedOperationHandler).IsAssignableFrom(contract)))
                services.AddTransient(contract, type.AsType());
        }

        return services;
    }

    /// <summary>Discovers handlers in the assembly containing <typeparamref name="TMarker"/>.</summary>
    public static IServiceCollection AddDadsOperationHandlersFromAssemblyOf<TMarker>(this IServiceCollection services) =>
        services.AddDadsOperationHandlersFromAssembly(typeof(TMarker).Assembly);

    /// <summary>Discovers and registers all concrete <see cref="IDadsOperationHandler"/> implementations in an assembly.</summary>
    public static IServiceCollection AddDadsOperationHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.DefinedTypes.Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IDadsOperationHandler).IsAssignableFrom(type)))
        {
            var handlerName = type.GetCustomAttribute<DadsOperationHandlerAttribute>()?.HandlerName ?? type.Name;
            services.AddTransient(type.AsType());
            services.AddSingleton(new DadsHandlerRegistration(handlerName, type.AsType()));
        }

        return services;
    }
}

internal sealed record DadsHandlerRegistration(string HandlerName, Type HandlerType);
