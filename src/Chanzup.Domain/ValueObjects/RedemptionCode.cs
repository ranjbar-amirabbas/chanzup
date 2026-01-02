namespace Chanzup.Domain.ValueObjects;

public record RedemptionCode
{
    public string Value { get; init; }

    public RedemptionCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Redemption code cannot be null or empty", nameof(value));

        if (value.Length < 6 || value.Length > 20)
            throw new ArgumentException("Redemption code must be between 6 and 20 characters", nameof(value));

        Value = value.ToUpperInvariant();
    }

    public static RedemptionCode Generate(string prefix = "")
    {
        var random = new Random();
        var code = prefix + random.Next(100000, 999999).ToString();
        return new RedemptionCode(code);
    }

    public static implicit operator string(RedemptionCode code) => code.Value;
    public static implicit operator RedemptionCode(string code) => new(code);

    public override string ToString() => Value;
}