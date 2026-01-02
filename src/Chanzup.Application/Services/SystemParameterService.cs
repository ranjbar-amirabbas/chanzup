using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Services;

public class SystemParameterService : ISystemParameterService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<SystemParameterService> _logger;

    public SystemParameterService(IApplicationDbContext context, ILogger<SystemParameterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Parameter Management
    public async Task<SystemParameterResponse> GetParameterAsync(string key)
    {
        var parameter = await _context.SystemParameters
            .Include(sp => sp.UpdatedByAdmin)
            .FirstOrDefaultAsync(sp => sp.Key == key);

        if (parameter == null)
            throw new ArgumentException($"Parameter '{key}' not found");

        return MapToSystemParameterResponse(parameter);
    }

    public async Task<IEnumerable<SystemParameterResponse>> GetParametersByCategoryAsync(string category)
    {
        var parameters = await _context.SystemParameters
            .Include(sp => sp.UpdatedByAdmin)
            .Where(sp => sp.Category == category)
            .OrderBy(sp => sp.Key)
            .ToListAsync();

        return parameters.Select(MapToSystemParameterResponse);
    }

    public async Task<IEnumerable<SystemParameterResponse>> GetAllParametersAsync()
    {
        var parameters = await _context.SystemParameters
            .Include(sp => sp.UpdatedByAdmin)
            .OrderBy(sp => sp.Category)
            .ThenBy(sp => sp.Key)
            .ToListAsync();

        return parameters.Select(MapToSystemParameterResponse);
    }

    public async Task<SystemParameterResponse> UpdateParameterAsync(string key, UpdateSystemParameterRequest request, Guid updatedBy)
    {
        var parameter = await _context.SystemParameters
            .FirstOrDefaultAsync(sp => sp.Key == key);

        if (parameter == null)
            throw new ArgumentException($"Parameter '{key}' not found");

        parameter.UpdateValue(request.Value, updatedBy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("System parameter {Key} updated to {Value} by {UpdatedBy}", key, request.Value, updatedBy);

        return await GetParameterAsync(key);
    }

    public async Task<SystemParameterResponse> CreateParameterAsync(string key, string value, string description, string category, ParameterType type, Guid createdBy)
    {
        var existingParameter = await _context.SystemParameters
            .FirstOrDefaultAsync(sp => sp.Key == key);

        if (existingParameter != null)
            throw new InvalidOperationException($"Parameter '{key}' already exists");

        var parameter = new SystemParameter
        {
            Key = key,
            Value = value,
            Description = description,
            Category = category,
            Type = type,
            UpdatedBy = createdBy
        };

        _context.SystemParameters.Add(parameter);
        await _context.SaveChangesAsync();

        _logger.LogInformation("System parameter {Key} created with value {Value} by {CreatedBy}", key, value, createdBy);

        return MapToSystemParameterResponse(parameter);
    }

    public async Task DeleteParameterAsync(string key)
    {
        var parameter = await _context.SystemParameters
            .FirstOrDefaultAsync(sp => sp.Key == key);

        if (parameter == null)
            throw new ArgumentException($"Parameter '{key}' not found");

        if (parameter.IsReadOnly)
            throw new InvalidOperationException($"Parameter '{key}' is read-only and cannot be deleted");

        _context.SystemParameters.Remove(parameter);
        await _context.SaveChangesAsync();

        _logger.LogInformation("System parameter {Key} deleted", key);
    }

    // Typed Parameter Access
    public async Task<string> GetStringParameterAsync(string key, string defaultValue = "")
    {
        try
        {
            var parameter = await _context.SystemParameters
                .FirstOrDefaultAsync(sp => sp.Key == key);

            return parameter?.Value ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task<int> GetIntParameterAsync(string key, int defaultValue = 0)
    {
        try
        {
            var parameter = await _context.SystemParameters
                .FirstOrDefaultAsync(sp => sp.Key == key);

            if (parameter != null && int.TryParse(parameter.Value, out var value))
                return value;

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task<decimal> GetDecimalParameterAsync(string key, decimal defaultValue = 0m)
    {
        try
        {
            var parameter = await _context.SystemParameters
                .FirstOrDefaultAsync(sp => sp.Key == key);

            if (parameter != null && decimal.TryParse(parameter.Value, out var value))
                return value;

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task<bool> GetBoolParameterAsync(string key, bool defaultValue = false)
    {
        try
        {
            var parameter = await _context.SystemParameters
                .FirstOrDefaultAsync(sp => sp.Key == key);

            if (parameter != null && bool.TryParse(parameter.Value, out var value))
                return value;

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    // Parameter Categories
    public async Task<IEnumerable<string>> GetParameterCategoriesAsync()
    {
        return await _context.SystemParameters
            .Select(sp => sp.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    // System Health
    public async Task InitializeDefaultParametersAsync()
    {
        var defaultParameters = new[]
        {
            // Fraud Detection Parameters
            new { Key = "FRAUD_MAX_SCANS_PER_HOUR", Value = "20", Description = "Maximum QR scans per hour per player", Category = "Fraud Detection", Type = ParameterType.Integer },
            new { Key = "FRAUD_MAX_SPINS_PER_HOUR", Value = "50", Description = "Maximum wheel spins per hour per player", Category = "Fraud Detection", Type = ParameterType.Integer },
            new { Key = "FRAUD_MAX_TRANSACTIONS_PER_HOUR", Value = "100", Description = "Maximum token transactions per hour per player", Category = "Fraud Detection", Type = ParameterType.Integer },
            new { Key = "FRAUD_MAX_LOGINS_PER_HOUR", Value = "10", Description = "Maximum login attempts per hour per user", Category = "Fraud Detection", Type = ParameterType.Integer },
            new { Key = "FRAUD_MAX_ACCOUNTS_PER_IP", Value = "3", Description = "Maximum accounts per IP address per week", Category = "Fraud Detection", Type = ParameterType.Integer },
            new { Key = "FRAUD_QR_RISK_THRESHOLD", Value = "50", Description = "Risk threshold for QR scan fraud detection", Category = "Fraud Detection", Type = ParameterType.Decimal },
            new { Key = "FRAUD_SPIN_RISK_THRESHOLD", Value = "60", Description = "Risk threshold for wheel spin fraud detection", Category = "Fraud Detection", Type = ParameterType.Decimal },
            new { Key = "FRAUD_TRANSACTION_RISK_THRESHOLD", Value = "40", Description = "Risk threshold for transaction fraud detection", Category = "Fraud Detection", Type = ParameterType.Decimal },
            new { Key = "FRAUD_LOGIN_RISK_THRESHOLD", Value = "45", Description = "Risk threshold for login fraud detection", Category = "Fraud Detection", Type = ParameterType.Decimal },
            new { Key = "FRAUD_ACCOUNT_CREATION_RISK_THRESHOLD", Value = "50", Description = "Risk threshold for account creation fraud detection", Category = "Fraud Detection", Type = ParameterType.Decimal },
            new { Key = "FRAUD_EXPECTED_WIN_RATE", Value = "0.3", Description = "Expected win rate for wheel spins", Category = "Fraud Detection", Type = ParameterType.Decimal },
            new { Key = "FRAUD_WIN_RATE_THRESHOLD", Value = "0.8", Description = "Suspicious win rate threshold", Category = "Fraud Detection", Type = ParameterType.Decimal },
            new { Key = "FRAUD_AUTO_BLOCK_THRESHOLD", Value = "3", Description = "Number of critical activities before auto-block", Category = "Fraud Detection", Type = ParameterType.Integer },
            new { Key = "FRAUD_VERIFICATION_THRESHOLD", Value = "70", Description = "Risk score threshold for additional verification", Category = "Fraud Detection", Type = ParameterType.Decimal },

            // Rate Limiting Parameters
            new { Key = "RATE_LIMIT_QR_SCAN", Value = "1", Description = "QR scan rate limit per minute", Category = "Rate Limiting", Type = ParameterType.Integer },
            new { Key = "RATE_LIMIT_WHEEL_SPIN", Value = "10", Description = "Wheel spin rate limit per minute", Category = "Rate Limiting", Type = ParameterType.Integer },
            new { Key = "RATE_LIMIT_API_GENERAL", Value = "100", Description = "General API rate limit per minute", Category = "Rate Limiting", Type = ParameterType.Integer },
            new { Key = "RATE_LIMIT_LOGIN", Value = "5", Description = "Login attempt rate limit per minute", Category = "Rate Limiting", Type = ParameterType.Integer },

            // Security Parameters
            new { Key = "SECURITY_JWT_EXPIRY_MINUTES", Value = "60", Description = "JWT token expiry time in minutes", Category = "Security", Type = ParameterType.Integer },
            new { Key = "SECURITY_REFRESH_TOKEN_EXPIRY_DAYS", Value = "30", Description = "Refresh token expiry time in days", Category = "Security", Type = ParameterType.Integer },
            new { Key = "SECURITY_PASSWORD_MIN_LENGTH", Value = "8", Description = "Minimum password length", Category = "Security", Type = ParameterType.Integer },
            new { Key = "SECURITY_REQUIRE_MFA", Value = "false", Description = "Require multi-factor authentication", Category = "Security", Type = ParameterType.Boolean },
            new { Key = "SECURITY_SESSION_TIMEOUT_MINUTES", Value = "30", Description = "Session timeout in minutes", Category = "Security", Type = ParameterType.Integer },

            // Business Rules Parameters
            new { Key = "BUSINESS_MAX_CAMPAIGNS_BASIC", Value = "1", Description = "Maximum campaigns for basic subscription", Category = "Business Rules", Type = ParameterType.Integer },
            new { Key = "BUSINESS_MAX_CAMPAIGNS_PREMIUM", Value = "5", Description = "Maximum campaigns for premium subscription", Category = "Business Rules", Type = ParameterType.Integer },
            new { Key = "BUSINESS_MAX_CAMPAIGNS_ENTERPRISE", Value = "999", Description = "Maximum campaigns for enterprise subscription", Category = "Business Rules", Type = ParameterType.Integer },
            new { Key = "PLAYER_MAX_TOKENS_PER_DAY", Value = "100", Description = "Maximum tokens a player can earn per day", Category = "Business Rules", Type = ParameterType.Integer },
            new { Key = "PLAYER_MAX_SPINS_PER_DAY", Value = "20", Description = "Maximum spins per player per day", Category = "Business Rules", Type = ParameterType.Integer },
            new { Key = "PRIZE_EXPIRY_DAYS", Value = "30", Description = "Prize expiry time in days", Category = "Business Rules", Type = ParameterType.Integer },

            // System Maintenance Parameters
            new { Key = "SYSTEM_MAINTENANCE_MODE", Value = "false", Description = "Enable maintenance mode", Category = "System", Type = ParameterType.Boolean },
            new { Key = "SYSTEM_LOG_RETENTION_DAYS", Value = "90", Description = "Log retention period in days", Category = "System", Type = ParameterType.Integer },
            new { Key = "SYSTEM_AUDIT_LOG_ENABLED", Value = "true", Description = "Enable audit logging", Category = "System", Type = ParameterType.Boolean },
            new { Key = "SYSTEM_ANALYTICS_ENABLED", Value = "true", Description = "Enable analytics collection", Category = "System", Type = ParameterType.Boolean }
        };

        foreach (var param in defaultParameters)
        {
            var existingParameter = await _context.SystemParameters
                .FirstOrDefaultAsync(sp => sp.Key == param.Key);

            if (existingParameter == null)
            {
                var systemParameter = new SystemParameter
                {
                    Key = param.Key,
                    Value = param.Value,
                    Description = param.Description,
                    Category = param.Category,
                    Type = param.Type,
                    IsReadOnly = false
                };

                _context.SystemParameters.Add(systemParameter);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Default system parameters initialized");
    }

    public async Task<bool> ValidateParametersAsync()
    {
        var parameters = await _context.SystemParameters.ToListAsync();
        var isValid = true;

        foreach (var parameter in parameters)
        {
            if (!parameter.IsValidValue(parameter.Value))
            {
                _logger.LogError("Invalid parameter value: {Key} = {Value}", parameter.Key, parameter.Value);
                isValid = false;
            }
        }

        return isValid;
    }

    private static SystemParameterResponse MapToSystemParameterResponse(SystemParameter parameter)
    {
        return new SystemParameterResponse
        {
            Key = parameter.Key,
            Value = parameter.Value,
            Description = parameter.Description,
            Category = parameter.Category,
            UpdatedAt = parameter.UpdatedAt,
            UpdatedByName = parameter.UpdatedByAdmin?.GetFullName()
        };
    }
}