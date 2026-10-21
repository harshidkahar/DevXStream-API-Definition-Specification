using DevXStream.DADS.Models;
using DevXStream.DADS.OpenApi;
using DevXStream.DADS.Serialization;
using DevXStream.DADS.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(serviceProvider =>
{
    var definitionPath = Path.Combine(AppContext.BaseDirectory, "Definitions", "ecommerce.dads.json");
    var definitionText = File.ReadAllText(definitionPath);
    var result = new DadsValidator().Validate(definitionText, ".json");
    if (!result.IsValid)
    {
        var failures = string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => $"{diagnostic.RuleId}: {diagnostic.Message}"));
        throw new InvalidOperationException($"The bundled DADS definition is invalid.{Environment.NewLine}{failures}");
    }

    return DadsDefinitionLoader.Load(definitionText, ".json");
});

var app = builder.Build();

var products = new[]
{
    new Product("prd_100", "Arc Lamp", 129.99m, "lighting"),
    new Product("prd_101", "Horizon Desk", 349.00m, "furniture")
};

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/products", (string? category, int? page) =>
{
    var filtered = string.IsNullOrWhiteSpace(category)
        ? products
        : products.Where(product => string.Equals(product.Category, category, StringComparison.OrdinalIgnoreCase)).ToArray();
    return Results.Ok(new { items = filtered.Select(ProductResponse.From), page = page ?? 1, total = filtered.Length });
});

app.MapGet("/products/{id}", (string id) =>
{
    var product = products.SingleOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase));
    return product is null
        ? Results.NotFound(new { title = "Product was not found." })
        : Results.Ok(ProductResponse.From(product));
});

app.MapPost("/orders", (CreateOrderRequest request) =>
{
    if (request.Items.Count == 0 || string.IsNullOrWhiteSpace(request.ShippingAddressId))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["At least one item and a shipping address are required."] });

    return Results.Created("/orders/ord_300", new { id = "ord_300", status = "Pending" });
});

app.MapGet("/dads/definition", () =>
    Results.File(Path.Combine(AppContext.BaseDirectory, "Definitions", "ecommerce.dads.json"), "application/json", "ecommerce.dads.json"));

app.MapGet("/openapi.json", (DadsDefinition definition) =>
    Results.Text(OpenApiExporter.Export(definition).ToJsonString(), "application/json"));

app.Run();

public sealed record Product(string Id, string Name, decimal Price, string Category);
public sealed record ProductResponse(string Id, string Name, decimal Price)
{
    public static ProductResponse From(Product product) => new(product.Id, product.Name, product.Price);
}

public sealed record CreateOrderRequest(IReadOnlyList<OrderItem> Items, string ShippingAddressId);
public sealed record OrderItem(string ProductId, int Quantity);
