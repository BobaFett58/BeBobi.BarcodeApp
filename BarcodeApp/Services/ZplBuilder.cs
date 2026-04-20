using System.Text;
using Qivisoft.BarcodeApp.Models;

namespace Qivisoft.BarcodeApp.Services;

public static class ZplBuilder
{
    public static string Build(IEnumerable<ValidProductData> rows, ZplBuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(options);

        var builder = new StringBuilder();

        foreach (var row in rows)
        {
            var safeName = EscapeField(row.Name);
            var barcodeFieldData = BarcodeValueRules.BuildZplFieldData(row.Ean, options.BarcodeType);
            var barcodeCommand = BuildBarcodeCommand(options.BarcodeType, options.BarcodeModuleWidthDots, options.BarcodeHeightDots, barcodeFieldData);
            var barcodeX = CalculateCenteredBarcodeX(options.LabelWidthDots, options.BarcodeType, options.BarcodeModuleWidthDots);

            for (var i = 0; i < row.Quantity; i++)
            {
                builder.AppendLine("^XA");
                builder.AppendLine($"^PW{options.LabelWidthDots}");
                if (options.LabelHeightDots > 0)
                    builder.AppendLine($"^LL{options.LabelHeightDots}");
                builder.AppendLine("^LH0,0");
                // Use UTF-8 code page for Polish diacritics in product names.
                builder.AppendLine("^CI28");

                if (options.IncludeProductName && !string.IsNullOrWhiteSpace(safeName))
                {
                    var nameFieldData = EnsureFieldBlockLineSeparator(safeName);
                    // 2-line centered description using Field Block
                    builder.AppendLine($"^FO0,12^FB{options.LabelWidthDots},2,2,C^A0N,28,28^FD{nameFieldData}^FS");
                    builder.AppendLine($"^FO{barcodeX},82{barcodeCommand}");
                }
                else
                {
                    builder.AppendLine($"^FO{barcodeX},30{barcodeCommand}");
                }

                builder.AppendLine("^XZ");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Calculates the left X origin (dots) to horizontally center a barcode
    /// within the label. Widths are estimated from module width in <c>^BY</c>.
    /// </summary>
    private static int CalculateCenteredBarcodeX(int labelWidthDots, BarcodeSymbology type, int moduleWidthDots)
    {
        var normalizedModuleWidth = Math.Clamp(moduleWidthDots, 1, 10);
        var baseWidthForModuleOne = type switch
        {
            BarcodeSymbology.Ean13   => 114,
            BarcodeSymbology.Ean8    => 82,
            BarcodeSymbology.UpcA    => 114,
            BarcodeSymbology.Code128 => 100,
            _                        => 114
        };
        var barcodeWidth = baseWidthForModuleOne * normalizedModuleWidth;
        return Math.Max((labelWidthDots - barcodeWidth) / 2, 10);
    }

    private static string EscapeField(string value)
    {
        return value
            .Replace("^", string.Empty, StringComparison.Ordinal)
            .Replace("~", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string EnsureFieldBlockLineSeparator(string value)
    {
        return value.EndsWith("\\&", StringComparison.Ordinal)
            ? value
            : $"{value}\\&";
    }

    private static string BuildBarcodeCommand(BarcodeSymbology type, int moduleWidthDots, int height, string fieldData)
    {
        var normalizedModuleWidth = Math.Clamp(moduleWidthDots, 1, 10);

        return type switch
        {
            BarcodeSymbology.Ean13 => $"^BY{normalizedModuleWidth},2,{height}^BEN,{height},Y,N^FD{fieldData}^FS",
            BarcodeSymbology.Ean8 => $"^BY{normalizedModuleWidth},2,{height}^B8N,{height},Y,N^FD{fieldData}^FS",
            BarcodeSymbology.UpcA => $"^BY{normalizedModuleWidth},2,{height}^BUN,{height},Y,N^FD{fieldData}^FS",
            BarcodeSymbology.Code128 => $"^BY{normalizedModuleWidth},2,{height}^BCN,{height},Y,N,N^FD{fieldData}^FS",
            _ => $"^BY{normalizedModuleWidth},2,{height}^BEN,{height},Y,N^FD{fieldData}^FS"
        };
    }
}