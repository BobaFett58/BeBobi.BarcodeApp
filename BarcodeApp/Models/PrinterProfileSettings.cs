namespace BarcodeApp.Models;

public sealed class PrinterProfileSettings
{
    public string Name { get; set; } = "Domyslny";
    public BarcodeSymbology BarcodeType { get; set; } = BarcodeSymbology.Ean13;
    public int PrinterDpi { get; set; } = 203;
    public string LabelWidthDotsText { get; set; } = "600";
    public string BarcodeHeightDotsText { get; set; } = "110";
    public string LabelHeightDotsText { get; set; } = "0";
}
