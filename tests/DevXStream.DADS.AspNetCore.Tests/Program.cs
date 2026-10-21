using DevXStream.DADS.AspNetCore;
using DevXStream.DADS.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var definition = new DadsDefinition(
    "DADS 1.0",
    new ApplicationDefinition("Test", "1.0", null, null, null),
    [],
    [new ModuleDefinition("Catalog", "Catalog operations", [
        new OperationDefinition("getProduct", "GET", "/products/{id}", "Get product", "Returns one product", null, null, null, [new ResponseDefinition("200", "OK", null, null, null)], null)
    ])],
    []);

var facadeWorkflowPasses = DadsDefinitionDocument.LoadValidated("""{ "definitionVersion": "DADS 1.0", "application": { "name": "Test", "version": "1" }, "modules": [] }""").Application.Name == "Test"
    && DadsDefinitionDocument.ExportOpenApi(definition)["openapi"]?.GetValue<string>() == "3.1.1";

Console.WriteLine($"{(facadeWorkflowPasses ? "PASS" : "FAIL")} exposes DADS workflow through ASP.NET Core package");
if (!facadeWorkflowPasses) return 1;

var generatedSource = DadsAspNetCodeGenerator.Generate(definition);
var generationPasses = generatedSource.Contains("public interface IGetProductHandler", StringComparison.Ordinal)
    && generatedSource.Contains("MapGeneratedDadsEndpoints", StringComparison.Ordinal)
    && generatedSource.Contains("/products/{id}", StringComparison.Ordinal);
Console.WriteLine($"{(generationPasses ? "PASS" : "FAIL")} generates ASP.NET Core endpoint contracts");
if (!generationPasses) return 1;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();
var invoked = false;
app.MapDadsEndpoints(definition, operation => async (httpContext, metadata) =>
{
    invoked = metadata.Operation.Name == "getProduct";
    await Task.CompletedTask;
    return Results.Ok();
});

var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Single();
var route = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
var dadsMetadata = endpoint.Metadata.GetMetadata<DadsOperationMetadata>();
var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
var passes = route == "/products/{id}"
    && methods?.SequenceEqual(["GET"]) == true
    && endpointName == "getProduct"
    && dadsMetadata?.ModuleName == "Catalog"
    && dadsMetadata.Operation == definition.Modules.Single().Endpoints.Single();

Console.WriteLine($"{(passes ? "PASS" : "FAIL")} maps DADS operation metadata");
if (!passes) return 1;

var missingHandlerThrows = false;
try
{
    app.MapDadsEndpoints(definition, _ => null);
}
catch (InvalidOperationException)
{
    missingHandlerThrows = true;
}

Console.WriteLine($"{(missingHandlerThrows ? "PASS" : "FAIL")} rejects missing handlers");

var convenienceBuilder = WebApplication.CreateBuilder();
convenienceBuilder.Services.AddSingleton(definition);
var convenienceApp = convenienceBuilder.Build();
convenienceApp.MapDadsEndpoints(handlers => handlers.Map("getProduct", GetProduct));
convenienceApp.MapDadsOpenApi();
var convenienceEndpoints = ((IEndpointRouteBuilder)convenienceApp).DataSources.SelectMany(source => source.Endpoints).ToArray();
var conveniencePasses = convenienceEndpoints.Length == 2
    && convenienceEndpoints.Any(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "getProduct")
    && convenienceEndpoints.Any(endpoint => endpoint is RouteEndpoint routeEndpoint && routeEndpoint.RoutePattern.RawText == "/openapi.json");

Console.WriteLine($"{(conveniencePasses ? "PASS" : "FAIL")} maps fluent handlers and OpenAPI endpoint");

var automaticDefinition = new DadsDefinition(
    "DADS 1.0",
    new ApplicationDefinition("Test", "1.0", null, null, null),
    [],
    [new ModuleDefinition("Catalog", null, [
        new OperationDefinition("getProduct", "GET", "/products/{id}", "Get product", null, null, null, null, [new ResponseDefinition("200", "OK", null, null, null)], new EngineeringMetadata(null, "AutoGetProductHandler", null, null, null, null, null))
    ])],
    []);
var automaticBuilder = WebApplication.CreateBuilder();
automaticBuilder.Services.AddSingleton(automaticDefinition);
automaticBuilder.Services.AddDadsOperationHandlersFromAssemblyOf<AutoGetProductHandler>();
var automaticApp = automaticBuilder.Build();
automaticApp.MapDadsEndpoints();
var automaticPasses = ((IEndpointRouteBuilder)automaticApp).DataSources.SelectMany(source => source.Endpoints)
    .Any(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "getProduct");

Console.WriteLine($"{(automaticPasses ? "PASS" : "FAIL")} discovers handlers from engineering metadata");

var runtimeDefinitionPath = Path.Combine(Path.GetTempPath(), $"dads-runtime-{Guid.NewGuid():N}.json");
await File.WriteAllTextAsync(runtimeDefinitionPath, """{ "definitionVersion": "DADS 1.0", "application": { "name": "Runtime Test", "version": "1" }, "modules": [{ "name": "Runtime", "endpoints": [{ "name": "runtimeOperation", "method": "GET", "route": "/runtime", "summary": "Runtime", "responses": [{ "statusCode": "200", "description": "OK" }], "engineering": { "handler": "AutoRuntimeHandler" } }] }] }""");
var runtimeBuilder = WebApplication.CreateBuilder();
runtimeBuilder.Services.AddDevXStreamDads(options =>
{
    options.DefinitionPath = runtimeDefinitionPath;
    options.HandlerAssembly = typeof(AutoRuntimeHandler).Assembly;
});
var runtimeApp = runtimeBuilder.Build();
runtimeApp.MapDevXStreamDads();
var runtimeEndpoints = ((IEndpointRouteBuilder)runtimeApp).DataSources.SelectMany(source => source.Endpoints).ToArray();
var runtimePasses = runtimeEndpoints.Any(endpoint => endpoint is RouteEndpoint routeEndpoint && routeEndpoint.RoutePattern.RawText == "/runtime")
    && runtimeEndpoints.Any(endpoint => endpoint is RouteEndpoint routeEndpoint && routeEndpoint.RoutePattern.RawText == "/dads")
    && runtimeEndpoints.Any(endpoint => endpoint is RouteEndpoint routeEndpoint && routeEndpoint.RoutePattern.RawText == "/dads/definition.json");
File.Delete(runtimeDefinitionPath);

Console.WriteLine($"{(runtimePasses ? "PASS" : "FAIL")} maps DADS runtime and native UI");
return missingHandlerThrows && !invoked && conveniencePasses && automaticPasses && runtimePasses ? 0 : 1;

static IResult GetProduct(string id) => Results.Ok(id);

public sealed class AutoGetProductHandler : IDadsOperationHandler
{
    public Task<IResult> HandleAsync(HttpContext httpContext, DadsOperationContext operation) => Task.FromResult<IResult>(Results.Ok());
}

public sealed class AutoRuntimeHandler : IDadsOperationHandler
{
    public Task<IResult> HandleAsync(HttpContext httpContext, DadsOperationContext operation) => Task.FromResult<IResult>(Results.Ok());
}
