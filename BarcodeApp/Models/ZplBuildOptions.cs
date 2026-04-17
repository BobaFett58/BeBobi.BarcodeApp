namespace BarcodeApp.Models;

public sealed class ZplBuildOptions
{
    public bool IncludeProductName { get; init; } = true;

    public int MaxProductNameLength { get; init; } = 42;

    public int LabelWidthDots { get; init; } = 600;

    public int BarcodeHeightDots { get; init; } = 110;
}