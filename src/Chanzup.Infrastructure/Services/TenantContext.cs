namespace Chanzup.Infrastructure.Services;

public interface ITenantContext
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
}

public class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    public Guid? TenantId => _tenantId;

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}