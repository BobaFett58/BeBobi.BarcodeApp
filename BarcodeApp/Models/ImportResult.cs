namespace Qivisoft.BarcodeApp.Models;

public sealed class ImportResult
{
    public required IReadOnlyList<ProductInputRow> Rows { get; init; }

    public required IReadOnlyList<string> Warnings { get; init; }
}