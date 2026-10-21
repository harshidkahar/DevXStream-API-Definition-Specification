using System.Text.Json;
using DevXStream.DADS.Docs;
using DevXStream.DADS.AspNetCore;
using DevXStream.DADS.OpenApi;
using DevXStream.DADS.Serialization;
using DevXStream.DADS.Validation;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dads <validate|export-openapi|build-docs-data|generate-aspnet> <definition> [--output <file>]");
    return 2;
}

var command = args[0].ToLowerInvariant();
var inputPath = args[1];
if (command is not ("validate" or "export-openapi" or "build-docs-data" or "generate-aspnet"))
{
    Console.Error.WriteLine($"Unknown command: {command}");
    return 2;
}
if (!File.Exists(inputPath)) { Console.Error.WriteLine($"File not found: {inputPath}"); return 2; }

var validation = new DadsValidator().Validate(await File.ReadAllTextAsync(inputPath), Path.GetExtension(inputPath));
if (!validation.IsValid)
{
    foreach (var diagnostic in validation.Diagnostics) Console.Error.WriteLine($"{diagnostic.Severity} {diagnostic.RuleId} {diagnostic.JsonPointer}: {diagnostic.Message}");
    return 1;
}

if (command == "validate") { Console.WriteLine("DADS validation passed."); return 0; }
if (args.Length != 4 || args[2] != "--output") { Console.Error.WriteLine("Export commands require --output <file>."); return 2; }

var definition = await DadsDefinitionLoader.LoadAsync(inputPath);
if (command == "generate-aspnet")
{
    await File.WriteAllTextAsync(args[3], DadsAspNetCodeGenerator.Generate(definition));
    Console.WriteLine($"Wrote generated ASP.NET Core endpoints to {args[3]}.");
    return 0;
}
object export = command switch
{
    "export-openapi" => OpenApiExporter.Export(definition),
    "build-docs-data" => DocumentationSiteProjector.Project(definition),
    _ => throw new InvalidOperationException("Command was validated before export.")
};
await File.WriteAllTextAsync(args[3], JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
Console.WriteLine($"Wrote {command} output to {args[3]}.");
return 0;
