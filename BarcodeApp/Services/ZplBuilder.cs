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
            if (safeName.Length > options.MaxProductNameLength) safeName = safeName[..options.MaxProductNameLength];
            var barcodeFieldData = BarcodeValueRules.BuildZplFieldData(row.Ean, options.BarcodeType);
            var barcodeCommand = BuildBarcodeCommand(options.BarcodeType, options.BarcodeHeightDots, barcodeFieldData);

            for (var i = 0; i < row.Quantity; i++)
            {
                builder.AppendLine("^XA");
                builder.AppendLine($"^PW{options.LabelWidthDots}");
                if (options.LabelHeightDots > 0)
                    builder.AppendLine($"^LL{options.LabelHeightDots}");
                builder.AppendLine("^LH0,0");

                if (options.IncludeProductName && !string.IsNullOrWhiteSpace(safeName))
                {
                    builder.AppendLine($"^FO30,20^A0N,32,32^FD{safeName}^FS");
                    builder.AppendLine($"^FO30,70{barcodeCommand}");
                }
                else
                {
                    builder.AppendLine($"^FO30,25{barcodeCommand}");
                }

                builder.AppendLine("^XZ");
            }
        }

        return builder.ToString();
    }

    private static string EscapeField(string value)
    {
        return value
            .Replace("^", string.Empty, StringComparison.Ordinal)
            .Replace("~", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string BuildBarcodeCommand(BarcodeSymbology type, int height, string fieldData)
    {
        return type switch
        {
            BarcodeSymbology.Ean13 => $"^BY2,2,{height}^BEN,{height},Y,N^FD{fieldData}^FS",
            BarcodeSymbology.Ean8 => $"^BY2,2,{height}^B8N,{height},Y,N^FD{fieldData}^FS",
            BarcodeSymbology.UpcA => $"^BY2,2,{height}^BUN,{height},Y,N^FD{fieldData}^FS",
            BarcodeSymbology.Code128 => $"^BY2,2,{height}^BCN,{height},Y,N,N^FD{fieldData}^FS",
            _ => $"^BY2,2,{height}^BEN,{height},Y,N^FD{fieldData}^FS"
        };
    }
}