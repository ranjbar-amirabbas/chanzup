namespace Chanzup.Domain.Entities;

public class SystemParameter
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ParameterType Type { get; set; } = ParameterType.String;
    public string? ValidationRule { get; set; }
    public bool IsReadOnly { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Admin? UpdatedByAdmin { get; set; }

    // Domain methods
    public bool CanBeModified()
    {
        return !IsReadOnly;
    }

    public void UpdateValue(string newValue, Guid updatedBy)
    {
        if (!CanBeModified())
            throw new InvalidOperationException("Parameter is read-only");

        if (!IsValidValue(newValue))
            throw new ArgumentException("Invalid parameter value");

        Value = newValue;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public bool IsValidValue(string value)
    {
        if (string.IsNullOrEmpty(ValidationRule))
            return true;

        return Type switch
        {
            ParameterType.Integer => int.TryParse(value, out _),
            ParameterType.Decimal => decimal.TryParse(value, out _),
            ParameterType.Boolean => bool.TryParse(value, out _),
            ParameterType.Email => IsValidEmail(value),
            ParameterType.Url => Uri.TryCreate(value, UriKind.Absolute, out _),
            _ => true
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public T GetTypedValue<T>()
    {
        return Type switch
        {
            ParameterType.Integer => (T)(object)int.Parse(Value),
            ParameterType.Decimal => (T)(object)decimal.Parse(Value),
            ParameterType.Boolean => (T)(object)bool.Parse(Value),
            _ => (T)(object)Value
        };
    }
}

public enum ParameterType
{
    String = 0,
    Integer = 1,
    Decimal = 2,
    Boolean = 3,
    Email = 4,
    Url = 5
}