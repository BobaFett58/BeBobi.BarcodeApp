using BarcodeApp.Models;
using BarcodeApp.Services;

namespace BarcodeApp.ViewModels;

public sealed class ProductRowViewModel : ViewModelBase
{
    private string _ean = string.Empty;
    private string _name = string.Empty;
    private string _quantityText = "1";
    private bool _isValid;
    private string _validationMessage = string.Empty;

    public event EventHandler? RowChanged;

    public string Ean
    {
        get => _ean;
        set
        {
            if (SetProperty(ref _ean, value))
            {
                Validate();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                Validate();
            }
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (SetProperty(ref _quantityText, value))
            {
                Validate();
            }
        }
    }

    public bool IsValid
    {
        get => _isValid;
        private set => SetProperty(ref _isValid, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

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

        if (!Ean13Validator.IsValid(normalizedEan))
        {
            errors.Add("EAN must contain 13 digits with valid checksum.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Product name is required.");
        }

        if (!int.TryParse(QuantityText.Trim(), out var qty) || qty <= 0)
        {
            errors.Add("Quantity must be a positive integer.");
        }

        IsValid = errors.Count == 0;
        ValidationMessage = IsValid ? "OK" : string.Join(" | ", errors);
        RowChanged?.Invoke(this, EventArgs.Empty);
    }
}
