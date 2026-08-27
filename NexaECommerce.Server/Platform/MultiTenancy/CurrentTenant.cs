using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaECommerce.Server.Platform.MultiTenancy;

public sealed class CurrentTenant(
    ITenantContext tenantContext)
    : ICurrentTenant
{
    public string Id =>
        tenantContext.TenantId;

    public bool IsMultiTenant =>
        tenantContext.IsMultiTenant;
}