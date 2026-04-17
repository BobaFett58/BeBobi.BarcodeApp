namespace Qivisoft.BarcodeApp.Models;

public sealed class ValidProductData
{
    public required string Ean { get; init; }

    public required string Name { get; init; }

    public required int Quantity { get; init; }
}