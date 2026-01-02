using Chanzup.Application.Interfaces;
using Chanzup.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Chanzup.Application.Services;

public class ReferralService : IReferralService
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenTransactionService _tokenTransactionService;
    
    // Configuration constants
    private const int ReferralTokenReward = 50;
    private const int ReferredPlayerTokenReward = 25;
    private const int SocialShareTokenReward = 10;
    private const int MaxDailySocialShares = 3;

    public ReferralService(IApplicationDbContext context, ITokenTransactionService tokenTransactionService)
    {
        _context = context;
        _tokenTransactionService = tokenTransactionService;
    }

    public async Task<string> GenerateReferralCodeAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new InvalidOperationException($"Player with ID {playerId} not found");

        // Check if player already has an active referral code
        var existingReferral = await _context.Referrals
            .Where(r => r.ReferrerId == playerId && r.ReferredPlayerId == Guid.Empty)
            .FirstOrDefaultAsync();

        if (existingReferral != null)
            return existingReferral.ReferralCode;

        // Generate a unique referral code
        string referralCode;
        bool isUnique;
        do
        {
            referralCode = GenerateUniqueCode(8);
            isUnique = !await _context.Referrals.AnyAsync(r => r.ReferralCode == referralCode);
        } while (!isUnique);

        // Create referral record
        var referral = Referral.Create(playerId, referralCode);
        _context.Referrals.Add(referral);
        await _context.SaveChangesAsync();

        return referralCode;
    }

    public async Task<Referral> UseReferralCodeAsync(string referralCode, Guid newPlayerId)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            throw new ArgumentException("Referral code is required", nameof(referralCode));

        var referral = await _context.Referrals
            .Include(r => r.Referrer)
            .Where(r => r.ReferralCode == referralCode && r.ReferredPlayerId == Guid.Empty)
            .FirstOrDefaultAsync();

        if (referral == null)
            throw new InvalidOperationException("Invalid or already used referral code");

        if (referral.ReferrerId == newPlayerId)
            throw new InvalidOperationException("Cannot use your own referral code");

        // Update referral with new player
        referral.ReferredPlayerId = newPlayerId;
        await _context.SaveChangesAsync();

        return referral;
    }

    public async Task<bool> CompleteReferralAsync(Guid referralId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var referral = await _context.Referrals
                .Include(r => r.Referrer)
                .Include(r => r.ReferredPlayer)
                .FirstOrDefaultAsync(r => r.Id == referralId);

            if (referral == null || !referral.IsEligibleForCompletion())
                return false;

            // Award tokens to referrer
            await _tokenTransactionService.AwardBonusTokensAsync(
                referral.ReferrerId,
                ReferralTokenReward,
                $"Referral bonus for referring {referral.ReferredPlayer.GetDisplayName()}",
                referral.Id);

            // Award tokens to referred player
            await _tokenTransactionService.AwardBonusTokensAsync(
                referral.ReferredPlayerId,
                ReferredPlayerTokenReward,
                $"Welcome bonus for using referral code {referral.ReferralCode}",
                referral.Id);

            // Complete the referral
            referral.CompleteReferral(ReferralTokenReward);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<IEnumerable<Referral>> GetPlayerReferralsAsync(Guid playerId)
    {
        return await _context.Referrals
            .Include(r => r.ReferredPlayer)
            .Where(r => r.ReferrerId == playerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ReferralStats> GetReferralStatsAsync(Guid playerId)
    {
        var referrals = await _context.Referrals
            .Where(r => r.ReferrerId == playerId)
            .ToListAsync();

        return new ReferralStats
        {
            TotalReferrals = referrals.Count,
            CompletedReferrals = referrals.Count(r => r.IsCompleted),
            PendingReferrals = referrals.Count(r => !r.IsCompleted && r.ReferredPlayerId != Guid.Empty),
            TotalTokensEarned = referrals.Where(r => r.IsCompleted).Sum(r => r.TokensAwarded),
            LastReferralDate = referrals.Where(r => r.ReferredPlayerId != Guid.Empty)
                                      .OrderByDescending(r => r.CreatedAt)
                                      .FirstOrDefault()?.CreatedAt
        };
    }

    public async Task<bool> IsReferralCodeValidAsync(string referralCode)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return false;

        return await _context.Referrals
            .AnyAsync(r => r.ReferralCode == referralCode && r.ReferredPlayerId == Guid.Empty);
    }

    public async Task<SocialShare> RecordSocialShareAsync(Guid playerId, SocialPlatform platform, string content, string? externalShareId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
            throw new InvalidOperationException($"Player with ID {playerId} not found");

        // Check daily limit
        var today = DateTime.UtcNow.Date;
        var dailyShareCount = await GetDailySocialShareCountAsync(playerId, today);
        
        if (dailyShareCount >= MaxDailySocialShares)
            throw new InvalidOperationException($"Daily social share limit of {MaxDailySocialShares} reached");

        // Create social share record
        var socialShare = SocialShare.Create(playerId, platform, content, externalShareId);
        _context.SocialShares.Add(socialShare);

        // Auto-verify and award tokens (in a real system, this might require external verification)
        socialShare.VerifyShare(SocialShareTokenReward);

        // Award tokens
        await _tokenTransactionService.AwardBonusTokensAsync(
            playerId,
            SocialShareTokenReward,
            $"Social sharing bonus on {platform}",
            socialShare.Id);

        await _context.SaveChangesAsync();
        return socialShare;
    }

    public async Task<bool> VerifySocialShareAsync(Guid socialShareId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var socialShare = await _context.SocialShares
                .FirstOrDefaultAsync(s => s.Id == socialShareId);

            if (socialShare == null || !socialShare.IsEligibleForVerification())
                return false;

            // Verify the share
            socialShare.VerifyShare(SocialShareTokenReward);

            // Award tokens
            await _tokenTransactionService.AwardBonusTokensAsync(
                socialShare.PlayerId,
                SocialShareTokenReward,
                $"Social sharing bonus verification on {socialShare.Platform}",
                socialShare.Id);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<IEnumerable<SocialShare>> GetPlayerSocialSharesAsync(Guid playerId)
    {
        return await _context.SocialShares
            .Where(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetDailySocialShareCountAsync(Guid playerId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.SocialShares
            .Where(s => s.PlayerId == playerId &&
                       s.CreatedAt >= startOfDay &&
                       s.CreatedAt < endOfDay)
            .CountAsync();
    }

    private static string GenerateUniqueCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new StringBuilder();
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[bytes[i] % chars.Length]);
        }
        
        return result.ToString();
    }
}