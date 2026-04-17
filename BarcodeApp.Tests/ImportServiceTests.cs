using BarcodeApp.Services;
using BarcodeApp.Tests.TestHelpers;
using ClosedXML.Excel;

namespace BarcodeApp.Tests;

public sealed class ImportServiceTests
{
    private readonly ImportService _service = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Import_Throws_ForNullOrWhitespacePath(string? path)
    {
        Assert.ThrowsAny<ArgumentException>(() => _service.Import(path!));
    }

    [Fact]
    public void Import_Throws_ForMissingFile()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");

        Assert.Throws<FileNotFoundException>(() => _service.Import(missing));
    }

    [Fact]
    public void Import_Csv_WithSemicolonHeaders_ParsesRows()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("input.csv");

        File.WriteAllText(path,
            "GTIN;Pełna, ustandaryzowana nazwa produktu.;Ilość szt.\n" +
            "5903949788051;Zestaw A;10\n" +
            "5903949788068;Zestaw B;5\n");

        var result = _service.Import(path);

        Assert.Equal(2, result.Rows.Count);
        Assert.Empty(result.Warnings);
        Assert.Equal("5903949788051", result.Rows[0].Ean);
        Assert.Equal("Zestaw A", result.Rows[0].Name);
        Assert.Equal("10", result.Rows[0].QuantityText);
    }

    [Fact]
    public void Import_Csv_WithoutHeader_UsesFirstThreeColumns()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("no-header.csv");

        File.WriteAllText(path,
            "5903949788051,Produkt A,3\n" +
            "5903949788068,Produkt B,4\n");

        var result = _service.Import(path);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("5903949788068", result.Rows[1].Ean);
        Assert.Equal("Produkt B", result.Rows[1].Name);
        Assert.Equal("4", result.Rows[1].QuantityText);
    }

    [Fact]
    public void Import_Csv_HeaderDetectedByText_ParsesWithoutWarnings()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("unknown-header.csv");

        File.WriteAllText(path,
            "ColA,ColB,ColC\n" +
            "5903949788051,Produkt A,2\n");

        var result = _service.Import(path);

        Assert.Single(result.Rows);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Import_EmptyCsv_ReturnsNoDataWarning()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("empty.csv");
        File.WriteAllText(path, string.Empty);

        var result = _service.Import(path);

        Assert.Empty(result.Rows);
        Assert.Single(result.Warnings);
        Assert.Contains("Input file has no data rows", result.Warnings[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Import_Excel_WithKnownHeaders_ParsesRows()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("input.xlsx");

        using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("Data");
            ws.Cell(1, 1).Value = "EAN";
            ws.Cell(1, 2).Value = "Nazwa produktu";
            ws.Cell(1, 3).Value = "Ilość";

            ws.Cell(2, 1).Value = "5903949788051";
            ws.Cell(2, 2).Value = "Produkt A";
            ws.Cell(2, 3).Value = 6;

            ws.Cell(3, 1).Value = "5903949788068";
            ws.Cell(3, 2).Value = "Produkt B";
            ws.Cell(3, 3).Value = 7;

            workbook.SaveAs(path);
        }

        var result = _service.Import(path);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("Produkt B", result.Rows[1].Name);
        Assert.Equal("7", result.Rows[1].QuantityText);
    }

    [Fact]
    public void Import_UnsupportedExtension_Throws()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("input.txt");
        File.WriteAllText(path, "irrelevant");

        Assert.Throws<NotSupportedException>(() => _service.Import(path));
    }

    [Fact]
    public void Import_Csv_WithTabDelimiter_ParsesRows()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("tab.csv");

        File.WriteAllText(path,
            "EAN\tNazwa produktu\tIlość\n" +
            "5903949788051\tProdukt A\t2\n");

        var result = _service.Import(path);

        Assert.Single(result.Rows);
        Assert.Equal("Produkt A", result.Rows[0].Name);
        Assert.Equal("2", result.Rows[0].QuantityText);
    }

    [Fact]
    public void Import_Csv_HeaderWithoutData_ReturnsNoRowsWarning()
    {
        using var temp = new TempPath();
        var path = temp.GetFilePath("header-only.csv");

        File.WriteAllText(path, "AAA,BBB,CCC\n");

        var result = _service.Import(path);

        Assert.Empty(result.Rows);
        Assert.Single(result.Warnings);
        Assert.Contains("No rows with product data", result.Warnings[0], StringComparison.Ordinal);
    }
}