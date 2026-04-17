using Qivisoft.BarcodeApp.Models;

namespace Qivisoft.BarcodeApp.Services;

public static class BarcodeValueRules
{
    public static bool IsValid(string? value, BarcodeSymbology type)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        return type switch
        {
            BarcodeSymbology.Ean13 => Ean13Validator.IsValid(trimmed),
            BarcodeSymbology.Ean8 => IsDigitsOfLength(trimmed, 8),
            BarcodeSymbology.UpcA => IsDigitsOfLength(trimmed, 12),
            BarcodeSymbology.Code128 => trimmed.Length is >= 1 and <= 48,
            _ => false
        };
    }

    public static string BuildZplFieldData(string value, BarcodeSymbology type)
    {
        var trimmed = value.Trim();

        return type switch
        {
            BarcodeSymbology.Ean13 => trimmed.Length >= 12 ? trimmed[..12] : trimmed,
            BarcodeSymbology.Ean8 => trimmed.Length >= 7 ? trimmed[..7] : trimmed,
            BarcodeSymbology.UpcA => trimmed.Length >= 11 ? trimmed[..11] : trimmed,
            BarcodeSymbology.Code128 => trimmed,
            _ => trimmed
        };
    }

    public static string ValidationMessage(BarcodeSymbology type)
    {
        return type switch
        {
            BarcodeSymbology.Ean13 => "EAN-13 musi zawierać 13 cyfr z poprawną sumą kontrolną.",
            BarcodeSymbology.Ean8 => "EAN-8 musi zawierać dokładnie 8 cyfr.",
            BarcodeSymbology.UpcA => "UPC-A musi zawierać dokładnie 12 cyfr.",
            BarcodeSymbology.Code128 => "Code 128 musi mieć od 1 do 48 znaków.",
            _ => "Nieprawidłowa wartość kodu kreskowego."
        };
    }

    private static bool IsDigitsOfLength(string value, int length)
    {
        return value.Length == length && value.All(char.IsDigit);
    }
}
