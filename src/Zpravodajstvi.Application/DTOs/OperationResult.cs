namespace Zpravodajstvi.Application.DTOs;

// Výsledek operace služby (registrace, úprava účtu, správa rolí): úspěch, nebo seznam chyb pro formulář
public class OperationResult
{
    public bool Succeeded { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    public static OperationResult Success() => new() { Succeeded = true };

    public static OperationResult Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };

    public static OperationResult Failure(IEnumerable<string> errors) => Failure(errors.ToArray());
}
