using Core.Interfaces;

namespace Infrastructure.Tenancy;

public interface ITenantContextInitializer
{
    void SetTenant(Guid tenantId);
}

public sealed class TenantContext : ITenantContext, ITenantContextInitializer
{
    public Guid? TenantId { get; private set;}

    public bool HasTenant => TenantId.HasValue;

    public Guid RequiredTenantId =>
        TenantId ?? throw new InvalidOperationException(
            "No tenant context is established for this operation.");

    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}