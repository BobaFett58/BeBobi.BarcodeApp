using BarcodeApp.Models;
using BarcodeApp.Services;

namespace BarcodeApp.ViewModels;

public sealed class ProductRowViewModel : ViewModelBase
{
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
            QuantityText = input.QuantityText
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
            Name = Name.Trim(),
            Quantity = int.Parse(QuantityText.Trim())
        };

        return true;
    }

    public void Validate()
    {
        var errors = new List<string>();
        var normalizedEan = Ean.Trim();

        if (!Ean13Validator.IsValid(normalizedEan)) errors.Add("EAN must contain 13 digits with valid checksum.");

        if (string.IsNullOrWhiteSpace(Name)) errors.Add("Product name is required.");

        if (!int.TryParse(QuantityText.Trim(), out var qty) || qty <= 0)
            errors.Add("Quantity must be a positive integer.");

        IsValid = errors.Count == 0;
        ValidationMessage = IsValid ? "OK" : string.Join(" | ", errors);
        RowChanged?.Invoke(this, EventArgs.Empty);
    }
}