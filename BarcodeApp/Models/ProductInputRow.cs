namespace BarcodeApp.Models;

public sealed class ProductInputRow
{
    public required string Ean { get; init; }

    public required string Name { get; init; }

    public required string QuantityText { get; init; }

    public required int SourceRowNumber { get; init; }
}