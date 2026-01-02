using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Chanzup.Application.DTOs;
using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Chanzup.Domain.ValueObjects;
using Chanzup.Infrastructure.Services;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace Chanzup.API.Controllers;

/// <summary>
/// Authentication and authorization endpoints for the Chanzup platform
/// </summary>
/// <remarks>
/// This controller handles authentication for different user types:
/// - Business staff (owners, managers, employees)
/// - Players (end customers)
/// - System administrators
/// 
/// All endpoints return JWT tokens for authenticated sessions with appropriate role-based claims.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Authentication and user management endpoints")]
public class AuthController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPermissionService _permissionService;

    public AuthController(
        IApplicationDbContext context, 
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        IPermissionService permissionService)
    {
        _context = context;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Registers a new business and creates an owner account
    /// </summary>
    /// <param name="request">Business registration details including owner credentials</param>
    /// <returns>JWT token and user information for the newly created business owner</returns>
    /// <response code="200">Business registered successfully</response>
    /// <response code="400">Invalid request data or business already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("register/business")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Register a new business",
        Description = "Creates a new business account with an owner user. The owner will have full access to manage the business, campaigns, and staff.",
        OperationId = "RegisterBusiness",
        Tags = new[] { "🔐 Authentication" }
    )]
    [SwaggerResponse(200, "Business registered successfully", typeof(LoginResponse))]
    [SwaggerResponse(400, "Invalid request data", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> RegisterBusiness([FromBody] RegisterBusinessRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if business already exists
            var existingBusiness = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Email.Value == request.Email);
            if (existingBusiness != null)
            {
                return BadRequest(new { error = "Business with this email already exists" });
            }

            // Check if staff with this email already exists
            var existingStaff = await _context.Staff
                .FirstOrDefaultAsync(s => s.Email.Value == request.Email);
            if (existingStaff != null)
            {
                return BadRequest(new { error = "User with this email already exists" });
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create business
            var business = new Business
            {
                Name = request.BusinessName,
                Email = new Email(request.Email),
                Phone = request.Phone,
                Address = request.Address,
                SubscriptionTier = (SubscriptionTier)request.SubscriptionTier
            };

            // Set location if provided
            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                business.UpdateLocation(request.Latitude.Value, request.Longitude.Value);
            }

            _context.Businesses.Add(business);
            await _context.SaveChangesAsync();

            // Create staff record for business owner
            var staff = new Staff
            {
                BusinessId = business.Id,
                Email = new Email(request.Email),
                PasswordHash = passwordHash,
                FirstName = "Business",
                LastName = "Owner",
                Role = StaffRole.Owner
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            // Get permissions for business owner
            var permissions = _permissionService.GetPermissionsForRole("BusinessOwner", StaffRole.Owner);

            // Generate JWT token
            var accessToken = _jwtService.GenerateToken(staff.Id, staff.Email.Value, "BusinessOwner", business.Id, permissions);
            
            // Generate refresh token
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(staff.Id, "Staff", GetIpAddress());

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = 3600,
                User = new UserInfo
                {
                    Id = staff.Id,
                    Email = staff.Email.Value,
                    Role = "BusinessOwner",
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    TenantId = business.Id
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Registers a new player account
    /// </summary>
    /// <param name="request">Player registration details</param>
    /// <returns>JWT token and user information for the newly created player</returns>
    /// <response code="200">Player registered successfully</response>
    /// <response code="400">Invalid request data or player already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("register/player")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Register a new player",
        Description = "Creates a new player account for end customers who will participate in games and redeem prizes.",
        OperationId = "RegisterPlayer",
        Tags = new[] { "🔐 Authentication" }
    )]
    [SwaggerResponse(200, "Player registered successfully", typeof(LoginResponse))]
    [SwaggerResponse(400, "Invalid request data", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> RegisterPlayer([FromBody] RegisterPlayerRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if player already exists
            var existingPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.Email.Value == request.Email);
            if (existingPlayer != null)
            {
                return BadRequest(new { error = "Player with this email already exists" });
            }

            // Check if staff with this email already exists
            var existingStaff = await _context.Staff
                .FirstOrDefaultAsync(s => s.Email.Value == request.Email);
            if (existingStaff != null)
            {
                return BadRequest(new { error = "User with this email already exists" });
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create player
            var player = new Player
            {
                Email = new Email(request.Email),
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            // Get permissions for player
            var permissions = _permissionService.GetPermissionsForRole("Player");

            // Generate JWT token
            var accessToken = _jwtService.GenerateToken(player.Id, player.Email.Value, "Player", null, permissions);
            
            // Generate refresh token
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(player.Id, "Player", GetIpAddress());

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = 3600,
                User = new UserInfo
                {
                    Id = player.Id,
                    Email = player.Email.Value,
                    Role = "Player",
                    FirstName = player.FirstName,
                    LastName = player.LastName
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates business staff members (owners, managers, employees)
    /// </summary>
    /// <param name="request">Login credentials (email and password)</param>
    /// <returns>JWT token with business context and user information</returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid credentials or inactive account</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// This endpoint authenticates business staff and returns a JWT token with:
    /// - User role (BusinessOwner, Staff)
    /// - Business tenant ID for multi-tenant isolation
    /// - Permissions based on staff role and access level
    /// 
    /// **Demo Credentials:**
    /// - Email: owner@coffeeshop.com
    /// - Password: DemoPassword123!
    /// </remarks>
    [HttpPost("login/business")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Business staff login",
        Description = "Authenticates business staff members and returns JWT token with business context and role-based permissions.",
        OperationId = "LoginBusiness",
        Tags = new[] { "🔐 Authentication" }
    )]
    [SwaggerResponse(200, "Login successful", typeof(LoginResponse))]
    [SwaggerResponse(401, "Invalid credentials", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> LoginBusiness([FromBody] LoginRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find staff member
            var staff = await _context.Staff
                .Include(s => s.Business)
                .FirstOrDefaultAsync(s => s.Email.Value == request.Email && s.IsActive);

            if (staff == null || !BCrypt.Net.BCrypt.Verify(request.Password, staff.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            // Check if business is active
            if (!staff.Business.IsActive)
            {
                return Unauthorized(new { error = "Business account is suspended" });
            }

            // Determine role based on staff role
            var role = staff.Role == StaffRole.Owner ? "BusinessOwner" : "Staff";
            
            // Get permissions
            var permissions = _permissionService.GetPermissionsForRole(role, staff.Role);

            // Generate JWT token
            var accessToken = _jwtService.GenerateToken(staff.Id, staff.Email.Value, role, staff.BusinessId, permissions);
            
            // Generate refresh token
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(staff.Id, "Staff", GetIpAddress());

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = 3600,
                User = new UserInfo
                {
                    Id = staff.Id,
                    Email = staff.Email.Value,
                    Role = role,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    TenantId = staff.BusinessId
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates system administrators
    /// </summary>
    /// <param name="request">Admin login credentials</param>
    /// <returns>JWT token with admin privileges</returns>
    /// <response code="200">Admin login successful</response>
    /// <response code="401">Invalid admin credentials</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login/admin")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Admin login",
        Description = "Authenticates system administrators with elevated privileges for platform management.",
        OperationId = "LoginAdmin",
        Tags = new[] { "🔐 Authentication" }
    )]
    [SwaggerResponse(200, "Admin login successful", typeof(LoginResponse))]
    [SwaggerResponse(401, "Invalid credentials", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> LoginAdmin([FromBody] LoginRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find admin
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Email.Value == request.Email && a.IsActive);

            if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            // Get permissions
            var permissions = _permissionService.GetPermissionsForRole("Admin");

            // Generate JWT token
            var accessToken = _jwtService.GenerateToken(admin.Id, admin.Email.Value, "Admin", null, permissions);
            
            // Generate refresh token
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(admin.Id, "Admin", GetIpAddress());

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = 3600,
                User = new UserInfo
                {
                    Id = admin.Id,
                    Email = admin.Email.Value,
                    Role = "Admin",
                    FirstName = admin.FirstName,
                    LastName = admin.LastName
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates players (end customers)
    /// </summary>
    /// <param name="request">Player login credentials</param>
    /// <returns>JWT token for player access</returns>
    /// <response code="200">Player login successful</response>
    /// <response code="401">Invalid player credentials</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// **Demo Player Credentials:**
    /// - Email: demo@player.com
    /// - Password: PlayerPassword123!
    /// </remarks>
    [HttpPost("login/player")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Player login",
        Description = "Authenticates players (end customers) for game participation and prize redemption.",
        OperationId = "LoginPlayer",
        Tags = new[] { "🔐 Authentication" }
    )]
    [SwaggerResponse(200, "Player login successful", typeof(LoginResponse))]
    [SwaggerResponse(401, "Invalid credentials", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> LoginPlayer([FromBody] LoginRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find player
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Email.Value == request.Email && p.IsActive);

            if (player == null || !BCrypt.Net.BCrypt.Verify(request.Password, player.PasswordHash))
            {
                return Unauthorized(new { error = "Invalid email or password" });
            }

            // Get permissions
            var permissions = _permissionService.GetPermissionsForRole("Player");

            // Generate JWT token
            var accessToken = _jwtService.GenerateToken(player.Id, player.Email.Value, "Player", null, permissions);
            
            // Generate refresh token
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(player.Id, "Player", GetIpAddress());

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = 3600,
                User = new UserInfo
                {
                    Id = player.Id,
                    Email = player.Email.Value,
                    Role = "Player",
                    FirstName = player.FirstName,
                    LastName = player.LastName
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Refreshes an expired JWT token using a valid refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New JWT token and refresh token</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Invalid or expired refresh token</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// Use this endpoint when your access token expires. The refresh token has a longer lifespan
    /// and can be used to obtain a new access token without requiring the user to log in again.
    /// 
    /// **Security Note:** The old refresh token is automatically revoked and a new one is issued.
    /// </remarks>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Refresh JWT token",
        Description = "Exchanges a valid refresh token for a new access token and refresh token pair.",
        OperationId = "RefreshToken",
        Tags = new[] { "🔐 Authentication" }
    )]
    [SwaggerResponse(200, "Token refreshed successfully", typeof(LoginResponse))]
    [SwaggerResponse(401, "Invalid refresh token", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var refreshToken = await _refreshTokenService.GetRefreshTokenAsync(request.RefreshToken);
            
            if (refreshToken == null || !refreshToken.IsActive)
            {
                return Unauthorized(new { error = "Invalid refresh token" });
            }

            // Generate new tokens
            string accessToken;
            string role;
            Guid? tenantId = null;
            UserInfo userInfo;

            if (refreshToken.UserType == "Player")
            {
                var player = await _context.Players.FindAsync(refreshToken.UserId);
                if (player == null || !player.IsActive)
                {
                    return Unauthorized(new { error = "User not found or inactive" });
                }

                role = "Player";
                var permissions = _permissionService.GetPermissionsForRole(role);
                accessToken = _jwtService.GenerateToken(player.Id, player.Email.Value, role, null, permissions);
                
                userInfo = new UserInfo
                {
                    Id = player.Id,
                    Email = player.Email.Value,
                    Role = role,
                    FirstName = player.FirstName,
                    LastName = player.LastName
                };
            }
            else if (refreshToken.UserType == "Admin")
            {
                var admin = await _context.Admins.FindAsync(refreshToken.UserId);
                if (admin == null || !admin.IsActive)
                {
                    return Unauthorized(new { error = "User not found or inactive" });
                }

                role = "Admin";
                var permissions = _permissionService.GetPermissionsForRole(role);
                accessToken = _jwtService.GenerateToken(admin.Id, admin.Email.Value, role, null, permissions);
                
                userInfo = new UserInfo
                {
                    Id = admin.Id,
                    Email = admin.Email.Value,
                    Role = role,
                    FirstName = admin.FirstName,
                    LastName = admin.LastName
                };
            }
            else // Staff
            {
                var staff = await _context.Staff
                    .Include(s => s.Business)
                    .FirstOrDefaultAsync(s => s.Id == refreshToken.UserId);
                    
                if (staff == null || !staff.IsActive || !staff.Business.IsActive)
                {
                    return Unauthorized(new { error = "User not found or inactive" });
                }

                role = staff.Role == StaffRole.Owner ? "BusinessOwner" : "Staff";
                tenantId = staff.BusinessId;
                var permissions = _permissionService.GetPermissionsForRole(role, staff.Role);
                accessToken = _jwtService.GenerateToken(staff.Id, staff.Email.Value, role, tenantId, permissions);
                
                userInfo = new UserInfo
                {
                    Id = staff.Id,
                    Email = staff.Email.Value,
                    Role = role,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    TenantId = tenantId
                };
            }

            // Revoke old refresh token and create new one
            await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetIpAddress());
            var newRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(refreshToken.UserId, refreshToken.UserType, GetIpAddress());

            return Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresIn = 3600,
                User = userInfo
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Logs out a user by revoking their refresh token
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <returns>Logout confirmation</returns>
    /// <response code="200">Logout successful</response>
    /// <response code="500">Internal server error</response>
    /// <remarks>
    /// This endpoint revokes the provided refresh token, effectively logging out the user.
    /// The access token will continue to work until it expires naturally (typically 1 hour).
    /// 
    /// **Best Practice:** Always call this endpoint when users explicitly log out.
    /// </remarks>
    [HttpPost("logout")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "User logout",
        Description = "Revokes the refresh token to log out the user securely.",
        OperationId = "Logout",
        Tags = new[] { "🔐 Authentication" }
    )]
    [SwaggerResponse(200, "Logout successful", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            if (!string.IsNullOrEmpty(request.RefreshToken))
            {
                await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken, GetIpAddress());
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    private string GetIpAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"].ToString();
        else
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
    }
}