using System.Globalization;
using BarcodeApp.Models;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;

namespace BarcodeApp.Services;

public sealed class ImportService
{
    private static readonly HashSet<string> EanAliases =
    [
        "ean", "gtin", "kod", "kodkreskowy", "barcode", "kodproduktu"
    ];

    private static readonly HashSet<string> NameAliases =
    [
        "nazwa", "nazwaproduktu", "pelnaunnazwyanazwaproduktu", "product", "productname"
    ];

    private static readonly HashSet<string> QuantityAliases =
    [
        "ilosc", "iloscszt", "qty", "quantity", "szt", "sztuki"
    ];

    public ImportResult Import(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Input file was not found.", path);
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        var rows = extension switch
        {
            ".csv" => ReadCsv(path),
            ".xls" or ".xlsx" => ReadExcel(path),
            _ => throw new NotSupportedException($"Unsupported file extension: {extension}")
        };

        return ParseRows(rows);
    }

    private static List<string[]> ReadCsv(string path)
    {
        using var sr = new StreamReader(path);
        var firstLine = sr.ReadLine() ?? string.Empty;
        var delimiter = DetectDelimiter(firstLine);
        sr.BaseStream.Position = 0;
        sr.DiscardBufferedData();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            BadDataFound = null,
            MissingFieldFound = null,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim
        };

        var rows = new List<string[]>();
        using var csv = new CsvReader(sr, config);

        while (csv.Read())
        {
            var raw = csv.Parser.Record;
            if (raw is null)
            {
                continue;
            }

            rows.Add(raw.Select(cell => cell?.Trim() ?? string.Empty).ToArray());
        }

        return rows;
    }

    private static List<string[]> ReadExcel(string path)
    {
        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<string[]>();
        foreach (var row in worksheet.RowsUsed())
        {
            var maxColumn = Math.Max(3, row.LastCellUsed()?.Address.ColumnNumber ?? 3);
            var values = row.Cells(1, maxColumn)
                .Select(cell => cell.GetFormattedString().Trim())
                .ToArray();

            if (values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(values);
        }

        return rows;
    }

    private static ImportResult ParseRows(List<string[]> rows)
    {
        if (rows.Count == 0)
        {
            return new ImportResult
            {
                Rows = [],
                Warnings = ["Input file has no data rows."]
            };
        }

        var warnings = new List<string>();
        var first = rows[0];
        var mapping = ResolveColumnMapping(first, out var hasHeader);

        if (mapping is null)
        {
            mapping = (0, 1, 2);
            warnings.Add("Could not detect headers. Falling back to first three columns: EAN, Name, Quantity.");
        }

        var startIndex = hasHeader ? 1 : 0;
        var importedRows = new List<ProductInputRow>();

        for (var i = startIndex; i < rows.Count; i++)
        {
            var row = rows[i];
            var ean = GetCell(row, mapping.Value.EanIndex);
            var name = GetCell(row, mapping.Value.NameIndex);
            var quantity = GetCell(row, mapping.Value.QuantityIndex);

            if (string.IsNullOrWhiteSpace(ean) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(quantity))
            {
                continue;
            }

            importedRows.Add(new ProductInputRow
            {
                Ean = ean,
                Name = name,
                QuantityText = quantity,
                SourceRowNumber = i + 1
            });
        }

        if (importedRows.Count == 0)
        {
            warnings.Add("No rows with product data were found after parsing.");
        }

        return new ImportResult
        {
            Rows = importedRows,
            Warnings = warnings
        };
    }

    private static (int EanIndex, int NameIndex, int QuantityIndex)? ResolveColumnMapping(
        IReadOnlyList<string> firstRow,
        out bool hasHeader)
    {
        hasHeader = false;

        if (firstRow.Count == 0)
        {
            return null;
        }

        int? ean = null;
        int? name = null;
        int? quantity = null;
        for (var i = 0; i < firstRow.Count; i++)
        {
            var normalized = NormalizeHeader(firstRow[i]);

            if (EanAliases.Contains(normalized))
            {
                ean = i;
            }
            else if (NameAliases.Contains(normalized))
            {
                name = i;
            }
            else if (QuantityAliases.Contains(normalized))
            {
                quantity = i;
            }
        }

        if (ean.HasValue && name.HasValue && quantity.HasValue)
        {
            hasHeader = true;
            return (ean.Value, name.Value, quantity.Value);
        }

        var firstCell = firstRow[0].Trim();
        if (!LooksLikeEan(firstCell) && firstRow.Any(cell => cell.Any(char.IsLetter)))
        {
            hasHeader = true;
            return (ean ?? 0, name ?? 1, quantity ?? 2);
        }

        return (0, 1, 2);
    }

    private static string NormalizeHeader(string header)
    {
        var lowered = header.Trim().ToLowerInvariant()
            .Replace('ą', 'a')
            .Replace('ć', 'c')
            .Replace('ę', 'e')
            .Replace('ł', 'l')
            .Replace('ń', 'n')
            .Replace('ó', 'o')
            .Replace('ś', 's')
            .Replace('ź', 'z')
            .Replace('ż', 'z');

        return new string(lowered.Where(char.IsLetterOrDigit).ToArray());
    }

    private static bool LooksLikeEan(string value)
    {
        return value.Length == 13 && value.All(char.IsDigit);
    }

    private static string GetCell(IReadOnlyList<string> row, int index)
    {
        if (index < 0 || index >= row.Count)
        {
            return string.Empty;
        }

        return row[index].Trim();
    }

    private static char DetectDelimiter(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return ',';
        }

        var candidates = new[] { ';', ',', '\t' };
        var best = candidates
            .Select(candidate => (candidate, count: line.Count(ch => ch == candidate)))
            .OrderByDescending(item => item.count)
            .First();

        return best.count == 0 ? ',' : best.candidate;
    }
}
