using Chanzup.Domain.ValueObjects;

namespace Chanzup.Domain.Services;

public interface ILocationVerificationService
{
    bool VerifyPlayerLocation(Location playerLocation, Location businessLocation, double allowedRadiusMeters = 100);
    double CalculateDistance(Location from, Location to);
}