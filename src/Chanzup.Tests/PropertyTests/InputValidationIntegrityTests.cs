using FsCheck;
using FsCheck.Xunit;
using Chanzup.Domain.ValueObjects;
using Chanzup.Domain.Entities;
using Xunit;
using FluentAssertions;

namespace Chanzup.Tests.PropertyTests;

/// <summary>
/// Feature: vancouver-rewards-platform, Property 3: Input Validation Integrity
/// Validates: Requirements 1.3
/// </summary>
public class InputValidationIntegrityTests
{
    [Property(MaxTest = 25)]
    public Property EmailValueObjectValidation()
    {
        return Prop.ForAll(
            GenerateEmailTestData(),
            (emailData) =>
            {
                try
                {
                    var email = new Email(emailData.Input);
                    
                    // If we get here, the email was valid
                    return emailData.ShouldBeValid &&
                           email.Value.Equals(emailData.Input.ToLowerInvariant()) &&
                           !string.IsNullOrWhiteSpace(email.Value);
                }
                catch (ArgumentException)
                {
                    // If we get here, the email was invalid
                    return !emailData.ShouldBeValid;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property LocationValueObjectValidation()
    {
        return Prop.ForAll(
            GenerateLocationTestData(),
            (locationData) =>
            {
                try
                {
                    var location = new Location(locationData.Latitude, locationData.Longitude);
                    
                    // If we get here, the location was valid
                    return locationData.ShouldBeValid &&
                           location.Latitude == locationData.Latitude &&
                           location.Longitude == locationData.Longitude &&
                           location.Latitude >= -90 && location.Latitude <= 90 &&
                           location.Longitude >= -180 && location.Longitude <= 180;
                }
                catch (ArgumentException)
                {
                    // If we get here, the location was invalid
                    return !locationData.ShouldBeValid;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property MoneyValueObjectValidation()
    {
        return Prop.ForAll(
            GenerateMoneyTestData(),
            (moneyData) =>
            {
                try
                {
                    var money = new Money(moneyData.Amount, moneyData.Currency);
                    
                    // If we get here, the money was valid
                    return moneyData.ShouldBeValid &&
                           money.Amount >= 0 &&
                           money.Amount == Math.Round(moneyData.Amount, 2) &&
                           !string.IsNullOrWhiteSpace(money.Currency) &&
                           money.Currency == moneyData.Currency.ToUpperInvariant();
                }
                catch (ArgumentException)
                {
                    // If we get here, the money was invalid
                    return !moneyData.ShouldBeValid;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property RedemptionCodeValidation()
    {
        return Prop.ForAll(
            GenerateRedemptionCodeTestData(),
            (codeData) =>
            {
                try
                {
                    var code = new RedemptionCode(codeData.Input);
                    
                    // If we get here, the code was valid
                    return codeData.ShouldBeValid &&
                           code.Value == codeData.Input.ToUpperInvariant() &&
                           code.Value.Length >= 6 &&
                           code.Value.Length <= 20 &&
                           !string.IsNullOrWhiteSpace(code.Value);
                }
                catch (ArgumentException)
                {
                    // If we get here, the code was invalid
                    return !codeData.ShouldBeValid;
                }
            });
    }

    [Property(MaxTest = 25)]
    public Property BusinessDomainMethodValidation()
    {
        return Prop.ForAll(
            GenerateBusinessTestData(),
            (businessData) =>
            {
                var business = new Business
                {
                    Name = businessData.Name,
                    Email = new Email("test@example.com"),
                    SubscriptionTier = businessData.SubscriptionTier,
                    IsActive = businessData.IsActive
                };

                var canCreateCampaign = business.CanCreateCampaign();
                var maxCampaigns = business.GetMaxCampaigns();

                // Validate business logic
                var expectedCanCreate = businessData.IsActive && businessData.SubscriptionTier != SubscriptionTier.Suspended;
                var expectedMaxCampaigns = businessData.SubscriptionTier switch
                {
                    SubscriptionTier.Basic => 1,
                    SubscriptionTier.Premium => 5,
                    SubscriptionTier.Enterprise => int.MaxValue,
                    _ => 0
                };

                return canCreateCampaign == expectedCanCreate &&
                       maxCampaigns == expectedMaxCampaigns;
            });
    }

    [Property(MaxTest = 25)]
    public Property PlayerTokenOperationValidation()
    {
        return Prop.ForAll(
            GeneratePlayerTokenTestData(),
            (tokenData) =>
            {
                var player = new Player
                {
                    Email = new Email("test@example.com"),
                    TokenBalance = tokenData.InitialBalance,
                    IsActive = tokenData.IsActive
                };

                try
                {
                    if (tokenData.Operation == "add")
                    {
                        player.AddTokens(tokenData.Amount, "Test");
                        return tokenData.Amount > 0 &&
                               player.TokenBalance == tokenData.InitialBalance + tokenData.Amount;
                    }
                    else if (tokenData.Operation == "spend")
                    {
                        player.SpendTokens(tokenData.Amount, "Test");
                        return tokenData.Amount > 0 &&
                               tokenData.InitialBalance >= tokenData.Amount &&
                               player.TokenBalance == tokenData.InitialBalance - tokenData.Amount;
                    }
                    else if (tokenData.Operation == "canAfford")
                    {
                        var canAfford = player.CanAffordSpin(tokenData.Amount);
                        return canAfford == (tokenData.IsActive && tokenData.InitialBalance >= tokenData.Amount);
                    }

                    return false;
                }
                catch (ArgumentException)
                {
                    return tokenData.Amount <= 0;
                }
                catch (InvalidOperationException)
                {
                    return tokenData.Operation == "spend" && tokenData.InitialBalance < tokenData.Amount;
                }
            });
    }

    // Test data generators
    private static Arbitrary<EmailTestData> GenerateEmailTestData()
    {
        return Arb.From(
            Gen.OneOf(
                // Valid emails
                Gen.Elements("test@example.com", "user@domain.org", "admin@site.net")
                   .Select(email => new EmailTestData { Input = email, ShouldBeValid = true }),
                
                // Invalid emails
                Gen.Elements("", "invalid", "@domain.com", "user@", "user@domain", "user.domain.com")
                   .Select(email => new EmailTestData { Input = email, ShouldBeValid = false })
            ));
    }

    private static Arbitrary<LocationTestData> GenerateLocationTestData()
    {
        return Arb.From(
            Gen.OneOf(
                // Valid locations
                from lat in Gen.Choose(-90, 90).Select(x => (decimal)x)
                from lng in Gen.Choose(-180, 180).Select(x => (decimal)x)
                select new LocationTestData { Latitude = lat, Longitude = lng, ShouldBeValid = true },
                
                // Invalid locations
                Gen.OneOf(
                    Gen.Choose(-200, -91).Select(lat => new LocationTestData { Latitude = lat, Longitude = 0, ShouldBeValid = false }),
                    Gen.Choose(91, 200).Select(lat => new LocationTestData { Latitude = lat, Longitude = 0, ShouldBeValid = false }),
                    Gen.Choose(-300, -181).Select(lng => new LocationTestData { Latitude = 0, Longitude = lng, ShouldBeValid = false }),
                    Gen.Choose(181, 300).Select(lng => new LocationTestData { Latitude = 0, Longitude = lng, ShouldBeValid = false })
                )
            ));
    }

    private static Arbitrary<MoneyTestData> GenerateMoneyTestData()
    {
        return Arb.From(
            Gen.OneOf(
                // Valid money
                from amount in Gen.Choose(0, 10000).Select(x => (decimal)x / 100)
                from currency in Gen.Elements("CAD", "USD", "EUR")
                select new MoneyTestData { Amount = amount, Currency = currency, ShouldBeValid = true },
                
                // Invalid money (negative amounts)
                from amount in Gen.Choose(-10000, -1).Select(x => (decimal)x / 100)
                from currency in Gen.Elements("CAD", "USD", "EUR")
                select new MoneyTestData { Amount = amount, Currency = currency, ShouldBeValid = false },
                
                // Invalid currency
                from amount in Gen.Choose(0, 10000).Select(x => (decimal)x / 100)
                select new MoneyTestData { Amount = amount, Currency = "", ShouldBeValid = false }
            ));
    }

    private static Arbitrary<RedemptionCodeTestData> GenerateRedemptionCodeTestData()
    {
        return Arb.From(
            Gen.OneOf(
                // Valid codes
                Gen.Elements("ABC123", "COFFEE-456", "DISCOUNT789", "PRIZE-XYZ-123")
                   .Select(code => new RedemptionCodeTestData { Input = code, ShouldBeValid = true }),
                
                // Invalid codes
                Gen.Elements("", "12345", "A", "THIS-IS-A-VERY-LONG-REDEMPTION-CODE-THAT-EXCEEDS-LIMIT")
                   .Select(code => new RedemptionCodeTestData { Input = code, ShouldBeValid = false })
            ));
    }

    private static Arbitrary<BusinessTestData> GenerateBusinessTestData()
    {
        return Arb.From(
            from name in Gen.Elements("Coffee Shop", "Restaurant", "Retail Store")
            from tier in Gen.Elements(SubscriptionTier.Basic, SubscriptionTier.Premium, SubscriptionTier.Enterprise, SubscriptionTier.Suspended)
            from isActive in Arb.Generate<bool>()
            select new BusinessTestData { Name = name, SubscriptionTier = tier, IsActive = isActive });
    }

    private static Arbitrary<PlayerTokenTestData> GeneratePlayerTokenTestData()
    {
        return Arb.From(
            from initialBalance in Gen.Choose(0, 1000)
            from amount in Gen.Choose(-100, 100)
            from operation in Gen.Elements("add", "spend", "canAfford")
            from isActive in Arb.Generate<bool>()
            select new PlayerTokenTestData 
            { 
                InitialBalance = initialBalance, 
                Amount = amount, 
                Operation = operation,
                IsActive = isActive
            });
    }

    // Test data classes
    private class EmailTestData
    {
        public string Input { get; set; } = "";
        public bool ShouldBeValid { get; set; }
    }

    private class LocationTestData
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool ShouldBeValid { get; set; }
    }

    private class MoneyTestData
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "";
        public bool ShouldBeValid { get; set; }
    }

    private class RedemptionCodeTestData
    {
        public string Input { get; set; } = "";
        public bool ShouldBeValid { get; set; }
    }

    private class BusinessTestData
    {
        public string Name { get; set; } = "";
        public SubscriptionTier SubscriptionTier { get; set; }
        public bool IsActive { get; set; }
    }

    private class PlayerTokenTestData
    {
        public int InitialBalance { get; set; }
        public int Amount { get; set; }
        public string Operation { get; set; } = "";
        public bool IsActive { get; set; }
    }
}