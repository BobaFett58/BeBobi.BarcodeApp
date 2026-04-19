namespace Qivisoft.BarcodeApp.Models;

public sealed class PrinterProfileSettings
{
    public string Name { get; set; } = "Domyslny";
    public BarcodeSymbology BarcodeType { get; set; } = BarcodeSymbology.Ean13;
    public string PrinterQueueName { get; set; } = string.Empty;
    public string PrinterHost { get; set; } = string.Empty;
    public int PrinterPort { get; set; } = 9100;
    public int PrinterDpi { get; set; } = 203;
    public string LabelWidthDotsText { get; set; } = "600";
    public string BarcodeHeightDotsText { get; set; } = "110";
    public string LabelHeightDotsText { get; set; } = "0";
}
