namespace NexaEcommerce.Modules.Orders.Domain.Entities;

/// <summary>A locally installed POS Bridge paired with exactly one tenant.</summary>
public sealed class PosBridgeDevice
{
    private PosBridgeDevice() { }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }

    public static PosBridgeDevice Create(string tenantId, string name, string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) throw new ArgumentException("Device name is invalid.", nameof(name));
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != 64) throw new ArgumentException("Token hash is invalid.", nameof(tokenHash));

        return new PosBridgeDevice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Trim(),
            Name = name.Trim(),
            TokenHash = tokenHash,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Touch()
    {
        if (!IsActive) throw new InvalidOperationException("Bridge device is disabled.");
        LastSeenAtUtc = DateTime.UtcNow;
    }

    public void Disable() => IsActive = false;
}
