namespace BarcodeApp.Services;

public static class Ean13Validator
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = value.Trim();
        if (digits.Length != 13 || digits.Any(ch => !char.IsDigit(ch)))
        {
            return false;
        }

        var expectedChecksum = CalculateChecksum(digits[..12]);
        return expectedChecksum == (digits[12] - '0');
    }

    public static int CalculateChecksum(string first12Digits)
    {
        if (first12Digits.Length != 12 || first12Digits.Any(ch => !char.IsDigit(ch)))
        {
            throw new ArgumentException("EAN checksum requires exactly 12 digits.", nameof(first12Digits));
        }

        var sum = 0;
        for (var i = 0; i < first12Digits.Length; i++)
        {
            var digit = first12Digits[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        var modulo = sum % 10;
        return modulo == 0 ? 0 : 10 - modulo;
    }
}
