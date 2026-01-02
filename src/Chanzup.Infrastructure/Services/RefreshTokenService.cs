using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chanzup.Infrastructure.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string userType, string ipAddress);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token, string? ipAddress = null, string? replacedByToken = null);
    Task RevokeAllUserRefreshTokensAsync(Guid userId, string userType);
    Task CleanupExpiredTokensAsync();
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public RefreshTokenService(IApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, string userType, string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            Token = _jwtService.GenerateRefreshToken(),
            UserId = userId,
            UserType = userType,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 days expiration
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task RevokeRefreshTokenAsync(string token, string? ipAddress = null, string? replacedByToken = null)
    {
        var refreshToken = await GetRefreshTokenAsync(token);
        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.Revoke(ipAddress, replacedByToken);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserRefreshTokensAsync(Guid userId, string userType)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.UserType == userType && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revoke();
        }

        await _context.SaveChangesAsync();
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();
    }
}