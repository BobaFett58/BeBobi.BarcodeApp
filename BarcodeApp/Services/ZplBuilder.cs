using System.Text;
using BarcodeApp.Models;

namespace BarcodeApp.Services;

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
            if (safeName.Length > options.MaxProductNameLength)
            {
                safeName = safeName[..options.MaxProductNameLength];
            }

            for (var i = 0; i < row.Quantity; i++)
            {
                builder.AppendLine("^XA");
                builder.AppendLine($"^PW{options.LabelWidthDots}");
                builder.AppendLine("^LH0,0");

                if (options.IncludeProductName && !string.IsNullOrWhiteSpace(safeName))
                {
                    builder.AppendLine($"^FO30,20^A0N,32,32^FD{safeName}^FS");
                    builder.AppendLine($"^FO30,70^BY2,2,{options.BarcodeHeightDots}^BEN,{options.BarcodeHeightDots},Y,N^FD{row.Ean}^FS");
                }
                else
                {
                    builder.AppendLine($"^FO30,25^BY2,2,{options.BarcodeHeightDots}^BEN,{options.BarcodeHeightDots},Y,N^FD{row.Ean}^FS");
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
}
