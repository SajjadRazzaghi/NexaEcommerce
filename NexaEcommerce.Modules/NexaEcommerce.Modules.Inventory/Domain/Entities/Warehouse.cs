using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public sealed class Warehouse : AggregateRoot
{
    private Warehouse()
    {
    }

    private Warehouse(
        string tenantId,
        string code,
        string name,
        string? addressLine,
        string? city,
        string? postalCode,
        string? phone,
        bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        TenantId = tenantId.Trim();

        SetCode(code);
        SetName(name);

        AddressLine = NormalizeOptional(addressLine);
        City = NormalizeOptional(city);
        PostalCode = NormalizeOptional(postalCode);
        Phone = NormalizeOptional(phone);

        IsDefault = isDefault;
        IsActive = true;
    }

    public string TenantId { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? AddressLine { get; private set; }

    public string? City { get; private set; }

    public string? PostalCode { get; private set; }

    public string? Phone { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public static Warehouse Create(
        string tenantId,
        string code,
        string name,
        string? addressLine = null,
        string? city = null,
        string? postalCode = null,
        string? phone = null,
        bool isDefault = false)
    {
        return new Warehouse(
            tenantId,
            code,
            name,
            addressLine,
            city,
            postalCode,
            phone,
            isDefault);
    }

    public void Update(
        string code,
        string name,
        string? addressLine,
        string? city,
        string? postalCode,
        string? phone)
    {
        SetCode(code);
        SetName(name);

        AddressLine = NormalizeOptional(addressLine);
        City = NormalizeOptional(city);
        PostalCode = NormalizeOptional(postalCode);
        Phone = NormalizeOptional(phone);

        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDefault(bool value)
    {
        IsDefault = value;
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
                "Warehouse code is required.",
                nameof(code));
        }

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length > 32)
        {
            throw new ArgumentException(
                "Warehouse code cannot exceed 32 characters.",
                nameof(code));
        }

        Code = normalized;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Warehouse name is required.",
                nameof(name));
        }

        var normalized = name.Trim();

        if (normalized.Length > 128)
        {
            throw new ArgumentException(
                "Warehouse name cannot exceed 128 characters.",
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
