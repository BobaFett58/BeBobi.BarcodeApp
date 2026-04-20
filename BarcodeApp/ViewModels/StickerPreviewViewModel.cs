using Qivisoft.BarcodeApp.Models;

namespace Qivisoft.BarcodeApp.ViewModels;

public sealed class StickerPreviewViewModel : ViewModelBase
{
    public StickerPreviewViewModel(ProductRowViewModel row, bool includeProductName, BarcodeSymbology barcodeType)
    {
        ArgumentNullException.ThrowIfNull(row);

        IncludeProductName = includeProductName;
        BarcodeType = barcodeType;
        DescriptionLine1 = includeProductName ? row.PreviewDescriptionLine1 : string.Empty;
        DescriptionLine2 = includeProductName ? row.PreviewDescriptionLine2 : string.Empty;
        BarcodeBars = row.PreviewBarcodeBars;
        BarcodeText = row.PreviewBarcodeText;
        ValidationMessage = row.ValidationMessage;
        IsValid = row.IsValid;
    }

    public bool IncludeProductName { get; }

    public BarcodeSymbology BarcodeType { get; }

    public string DescriptionLine1 { get; }

    public string DescriptionLine2 { get; }

    public string BarcodeBars { get; }

    public string BarcodeText { get; }

    public string ValidationMessage { get; }

    public bool IsValid { get; }

    public string BarcodeCaption => BarcodeType switch
    {
        BarcodeSymbology.Ean13 => "EAN-13",
        BarcodeSymbology.Ean8 => "EAN-8",
        BarcodeSymbology.UpcA => "UPC-A",
        BarcodeSymbology.Code128 => "Code 128",
        _ => "Kod kreskowy"
    };
}
