using DevXStream.DADS.AspNetCore;
using DevXStream.DADS.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
return missingHandlerThrows && !invoked ? 0 : 1;
