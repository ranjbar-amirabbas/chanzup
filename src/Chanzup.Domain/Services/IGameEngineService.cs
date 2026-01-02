using Chanzup.Domain.Entities;

namespace Chanzup.Domain.Services;

public interface IGameEngineService
{
    Task<Prize?> SpinWheel(Campaign campaign, Player player);
    bool CanPlayerSpin(Campaign campaign, Player player);
    int CalculateTokensToAward(Campaign campaign);
    bool ValidateSpinResult(WheelSpin spin, Campaign campaign);
}