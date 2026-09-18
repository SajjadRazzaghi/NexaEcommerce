using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public sealed class WarehouseLocation : BaseEntity
{
    private WarehouseLocation()
    {
    }

    private WarehouseLocation(
        string tenantId,
        Guid warehouseId,
        string code,
        string name,
        string? zone,
        string? rack,
        string? shelf,
        string? bin)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse id is required.",
                nameof(warehouseId));
        }

        TenantId = tenantId.Trim();
        WarehouseId = warehouseId;

        SetCode(code);
        SetName(name);

        Zone = NormalizeOptional(zone);
        Rack = NormalizeOptional(rack);
        Shelf = NormalizeOptional(shelf);
        Bin = NormalizeOptional(bin);

        IsActive = true;
    }

    public string TenantId { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? Zone { get; private set; }

    public string? Rack { get; private set; }

    public string? Shelf { get; private set; }

    public string? Bin { get; private set; }

    public bool IsActive { get; private set; }

    public static WarehouseLocation Create(
        string tenantId,
        Guid warehouseId,
        string code,
        string name,
        string? zone = null,
        string? rack = null,
        string? shelf = null,
        string? bin = null)
    {
        return new WarehouseLocation(
            tenantId,
            warehouseId,
            code,
            name,
            zone,
            rack,
            shelf,
            bin);
    }

    public void Update(
        string code,
        string name,
        string? zone,
        string? rack,
        string? shelf,
        string? bin)
    {
        SetCode(code);
        SetName(name);

        Zone = NormalizeOptional(zone);
        Rack = NormalizeOptional(rack);
        Shelf = NormalizeOptional(shelf);
        Bin = NormalizeOptional(bin);

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool value)
    {
        IsActive = value;
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Location code is required.",
                nameof(code));
        }

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException(
                "Location code cannot exceed 64 characters.",
                nameof(code));
        }

        Code = normalized;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Location name is required.",
                nameof(name));
        }

        var normalized = name.Trim();

        if (normalized.Length > 128)
        {
            throw new ArgumentException(
                "Location name cannot exceed 128 characters.",
                nameof(name));
        }

        Name = normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
