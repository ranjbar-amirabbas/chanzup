using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Chanzup.Application.DTOs;

/// <summary>
/// Business registration request model
/// </summary>
/// <remarks>
/// Used to register a new business and create the initial owner account.
/// The owner will have full administrative privileges for the business.
/// </remarks>
[SwaggerSchema(Description = "Business registration details including owner account information")]
public class RegisterBusinessRequest
{
    /// <summary>
    /// Name of the business
    /// </summary>
    /// <example>My Coffee Shop</example>
    [Required(ErrorMessage = "Business name is required")]
    [StringLength(100, ErrorMessage = "Business name cannot exceed 100 characters")]
    [SwaggerSchema(Description = "Official business name")]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>
    /// Business owner's email address (will be used for login)
    /// </summary>
    /// <example>owner@mycoffeeshop.com</example>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [SwaggerSchema(Description = "Owner's email address for account creation")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password for the owner account
    /// </summary>
    /// <example>SecurePassword123!</example>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    [SwaggerSchema(Description = "Strong password with mixed case, numbers, and special characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Business phone number
    /// </summary>
    /// <example>+1-555-123-4567</example>
    [Phone(ErrorMessage = "Please provide a valid phone number")]
    [SwaggerSchema(Description = "Business contact phone number (optional)")]
    public string? Phone { get; set; }

    /// <summary>
    /// Business physical address
    /// </summary>
    /// <example>123 Main Street, City, State 12345</example>
    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    [SwaggerSchema(Description = "Business physical address (optional)")]
    public string? Address { get; set; }

    /// <summary>
    /// Business location latitude for mapping
    /// </summary>
    /// <example>40.7128</example>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    [SwaggerSchema(Description = "GPS latitude coordinate (optional)")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Business location longitude for mapping
    /// </summary>
    /// <example>-74.0060</example>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    [SwaggerSchema(Description = "GPS longitude coordinate (optional)")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Initial subscription tier for the business
    /// </summary>
    /// <example>1</example>
    [Range(0, 2, ErrorMessage = "Subscription tier must be 0 (Basic), 1 (Premium), or 2 (Enterprise)")]
    [SwaggerSchema(Description = "Subscription tier: 0=Basic, 1=Premium, 2=Enterprise")]
    public int SubscriptionTier { get; set; } = 0;
}