using System.Text.Json;
using DevXStream.DADS.Docs;
using DevXStream.DADS.OpenApi;
using DevXStream.DADS.Serialization;
using DevXStream.DADS.Validation;

var fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "fixtures");
var validator = new DadsValidator();
var tests = new[]
{
    ("valid definition", "valid.dads.json", true, ""),
    ("duplicate route", "duplicate-route.dads.json", false, "DADS202"),
    ("missing route parameter", "missing-route-parameter.dads.json", false, "DADS205"),
    ("unknown schema", "unknown-schema.dads.json", false, "DADS208"),
    ("YAML definition", "valid.dads.yaml", true, "")
};
var failures = 0;
foreach (var test in tests)
{
    var result = validator.Validate(await File.ReadAllTextAsync(Path.Combine(fixtureDirectory, test.Item2)), Path.GetExtension(test.Item2));
    var passed = result.IsValid == test.Item3 && (string.IsNullOrEmpty(test.Item4) || result.Diagnostics.Any(diagnostic => diagnostic.RuleId == test.Item4));
    Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {test.Item1}");
    if (!passed) failures++;
}

var snapshotDefinition = await DadsDefinitionLoader.LoadAsync(Path.Combine(fixtureDirectory, "valid.dads.json"));
var openApiJson = OpenApiExporter.Export(snapshotDefinition).ToJsonString(new JsonSerializerOptions { WriteIndented = true });
var expectedOpenApi = await File.ReadAllTextAsync(Path.Combine(fixtureDirectory, "openapi.snapshot.json"));
var snapshotPasses = string.Equals(openApiJson.Trim(), expectedOpenApi.Trim(), StringComparison.Ordinal);
Console.WriteLine($"{(snapshotPasses ? "PASS" : "FAIL")} OpenAPI snapshot");
if (!snapshotPasses) failures++;

var docsSite = DocumentationSiteProjector.Project(snapshotDefinition);
var projectionPasses = docsSite.Navigation.Single().Operations.Single().RequestSnippets.ContainsKey("curl") && docsSite.Schemas.Single().Name == "Product";
Console.WriteLine($"{(projectionPasses ? "PASS" : "FAIL")} Documentation projection");
if (!projectionPasses) failures++;

var overSizedDefinition = new string(' ', DadsDefinitionLoader.MaximumDocumentBytes + 1);
var oversizedResult = validator.Validate(overSizedDefinition);
var oversizedPasses = !oversizedResult.IsValid && oversizedResult.Diagnostics.Any(diagnostic => diagnostic.RuleId == "DADS000");
Console.WriteLine($"{(oversizedPasses ? "PASS" : "FAIL")} oversized definition limit");
if (!oversizedPasses) failures++;

var deeplyNestedDefinition = new string('[', DadsDefinitionLoader.MaximumJsonDepth + 1) + new string(']', DadsDefinitionLoader.MaximumJsonDepth + 1);
var depthResult = validator.Validate(deeplyNestedDefinition);
var depthPasses = !depthResult.IsValid && depthResult.Diagnostics.Any(diagnostic => diagnostic.RuleId == "DADS000");
Console.WriteLine($"{(depthPasses ? "PASS" : "FAIL")} nesting depth limit");
if (!depthPasses) failures++;

var unsupportedVersion = validator.Validate("""{ "definitionVersion": "DADS 2.0", "application": { "name": "Test", "version": "1" }, "modules": [] }""");
var versionPasses = !unsupportedVersion.IsValid && unsupportedVersion.Diagnostics.Any(diagnostic => diagnostic.RuleId == "DADS110");
Console.WriteLine($"{(versionPasses ? "PASS" : "FAIL")} unsupported major version");
if (!versionPasses) failures++;
return failures == 0 ? 0 : 1;
