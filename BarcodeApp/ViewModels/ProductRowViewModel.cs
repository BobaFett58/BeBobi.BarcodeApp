using Qivisoft.BarcodeApp.Models;
using Qivisoft.BarcodeApp.Services;

namespace Qivisoft.BarcodeApp.ViewModels;

public sealed class ProductRowViewModel : ViewModelBase
{
    public BarcodeSymbology BarcodeType
    {
        get;
        set
        {
            if (SetProperty(ref field, value)) Validate();
        }
    } = BarcodeSymbology.Ean13;

    public string Ean
    {
        get;
        set
        {
            if (SetProperty(ref field, value)) Validate();
        }
    } = string.Empty;

    public string Name
    {
        get;
        set
        {
            if (SetProperty(ref field, value)) Validate();
        }
    } = string.Empty;

    public string QuantityText
    {
        get;
        set
        {
            if (SetProperty(ref field, value)) Validate();
        }
    } = "1";

    public string Sku
    {
        get;
        set
        {
            if (SetProperty(ref field, value)) RowChanged?.Invoke(this, EventArgs.Empty);
        }
    } = string.Empty;

    public string Price
    {
        get;
        set
        {
            if (SetProperty(ref field, value)) RowChanged?.Invoke(this, EventArgs.Empty);
        }
    } = string.Empty;

    public bool IsValid
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string ValidationMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    public event EventHandler? RowChanged;

    public static ProductRowViewModel CreateEmpty()
    {
        var row = new ProductRowViewModel();
        row.Validate();
        return row;
    }

    public static ProductRowViewModel FromInput(ProductInputRow input)
    {
        return new ProductRowViewModel
        {
            Ean = input.Ean,
            Name = input.Name,
            QuantityText = input.QuantityText,
            Sku = input.Sku,
            Price = input.Price,
            BarcodeType = BarcodeSymbology.Ean13
        };
    }

    public bool TryBuildValidData(out ValidProductData data)
    {
        Validate();

        if (!IsValid)
        {
            data = null!;
            return false;
        }

        data = new ValidProductData
        {
            Ean = Ean.Trim(),
            Name = BuildDescription(Sku, Name, Price),
            Quantity = int.Parse(QuantityText.Trim())
        };

        return true;
    }

    public void Validate()
    {
        var errors = new List<string>();
        var normalizedEan = Ean.Trim();

        if (!BarcodeValueRules.IsValid(normalizedEan, BarcodeType))
            errors.Add(BarcodeValueRules.ValidationMessage(BarcodeType));

        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Nazwa produktu jest wymagana.");

        if (!int.TryParse(QuantityText.Trim(), out var qty) || qty <= 0)
            errors.Add("Ilość musi być dodatnią liczbą całkowitą.");

        IsValid = errors.Count == 0;
        ValidationMessage = IsValid ? "OK" : string.Join(" | ", errors);
        RowChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string BuildDescription(string sku, string name, string price)
    {
        var parts = new[] { sku, name, price }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim());

        return string.Join(" ", parts);
    }
}