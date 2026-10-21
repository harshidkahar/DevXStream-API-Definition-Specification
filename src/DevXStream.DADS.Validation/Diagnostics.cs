namespace DevXStream.DADS.Validation;

public enum DiagnosticSeverity { Error, Warning }

public sealed record DadsDiagnostic(string RuleId, DiagnosticSeverity Severity, string JsonPointer, string Message);

public sealed class ValidationResult(IReadOnlyList<DadsDiagnostic> diagnostics)
{
    public IReadOnlyList<DadsDiagnostic> Diagnostics { get; } = diagnostics;
    public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
}
