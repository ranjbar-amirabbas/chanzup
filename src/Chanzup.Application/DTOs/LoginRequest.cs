using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace Chanzup.Application.DTOs;

/// <summary>
/// Login request model for user authentication
/// </summary>
/// <remarks>
/// Used for authenticating business staff, players, and administrators.
/// All user types use the same login format with email and password.
/// </remarks>
[SwaggerSchema(Description = "Login credentials for user authentication")]
public class LoginRequest
{
    /// <summary>
    /// User's email address (used as username)
    /// </summary>
    /// <example>owner@coffeeshop.com</example>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [SwaggerSchema(Description = "Valid email address used for authentication")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's password (minimum 6 characters)
    /// </summary>
    /// <example>DemoPassword123!</example>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    [SwaggerSchema(Description = "Password with minimum 6 characters")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response containing JWT tokens and user information
/// </summary>
/// <remarks>
/// Returned after successful authentication for any user type.
/// Contains both access token (short-lived) and refresh token (long-lived).
/// </remarks>
[SwaggerSchema(Description = "Authentication response with JWT tokens and user details")]
public class LoginResponse
{
    /// <summary>
    /// JWT access token for API authentication
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    [SwaggerSchema(Description = "JWT access token (expires in 1 hour)")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    /// <example>def502004a08b8be09c4b6fd85bb02c09004aeccd...</example>
    [SwaggerSchema(Description = "Refresh token for token renewal (expires in 30 days)")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    /// <example>3600</example>
    [SwaggerSchema(Description = "Access token expiration time in seconds")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type (always "Bearer")
    /// </summary>
    /// <example>Bearer</example>
    [SwaggerSchema(Description = "Token type for Authorization header")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Authenticated user information
    /// </summary>
    [SwaggerSchema(Description = "User details and role information")]
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// User information included in login response
/// </summary>
/// <remarks>
/// Contains essential user details and role information for client-side use.
/// TenantId is only present for business staff members.
/// </remarks>
[SwaggerSchema(Description = "User profile information and role details")]
public class UserInfo
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    /// <example>123e4567-e89b-12d3-a456-426614174000</example>
    [SwaggerSchema(Description = "Unique user ID (GUID)")]
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    /// <example>owner@coffeeshop.com</example>
    [SwaggerSchema(Description = "User's email address")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the system
    /// </summary>
    /// <example>BusinessOwner</example>
    [SwaggerSchema(Description = "User role: Admin, BusinessOwner, Staff, or Player")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    /// <example>John</example>
    [SwaggerSchema(Description = "User's first name (optional)")]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    /// <example>Doe</example>
    [SwaggerSchema(Description = "User's last name (optional)")]
    public string? LastName { get; set; }

    /// <summary>
    /// Business ID for staff members (multi-tenant isolation)
    /// </summary>
    /// <example>456e7890-e89b-12d3-a456-426614174001</example>
    [SwaggerSchema(Description = "Business/tenant ID (only for business staff)")]
    public Guid? TenantId { get; set; }
}

/// <summary>
/// Refresh token request for obtaining new access tokens
/// </summary>
/// <remarks>
/// Used to exchange a valid refresh token for a new access token and refresh token pair.
/// The old refresh token is automatically revoked for security.
/// </remarks>
[SwaggerSchema(Description = "Request to refresh an expired access token")]
public class RefreshTokenRequest
{
    /// <summary>
    /// Valid refresh token obtained from login or previous refresh
    /// </summary>
    /// <example>def502004a08b8be09c4b6fd85bb02c09004aeccd...</example>
    [Required(ErrorMessage = "Refresh token is required")]
    [SwaggerSchema(Description = "Valid refresh token for obtaining new access token")]
    public string RefreshToken { get; set; } = string.Empty;
}