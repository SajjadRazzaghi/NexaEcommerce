
using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class ShippingMethod : BaseEntity
{
    private ShippingMethod()
    {
    }

    private ShippingMethod(
        string tenantId,
        string code,
        string name,
        string carrier,
        decimal price,
        int sortOrder)
    {
        TenantId = tenantId.Trim();
        Code = code.Trim();
        Name = name.Trim();
        Carrier = carrier.Trim();
        Price = price;
        SortOrder = sortOrder;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public string TenantId { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Carrier { get; private set; } = null!;

    public decimal Price { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static ShippingMethod Create(
        string tenantId,
        string code,
        string name,
        string carrier,
        decimal price,
        int sortOrder = 0)
    {
        ValidateText(
            tenantId,
            nameof(tenantId),
            64);

        ValidateText(
            code,
            nameof(code),
            64);

        ValidateText(
            name,
            nameof(name),
            150);

        ValidateText(
            carrier,
            nameof(carrier),
            100);

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder));
        }

        return new ShippingMethod(
            tenantId,
            code,
            name,
            carrier,
            price,
            sortOrder);
    }

    public void Update(
        string name,
        string carrier,
        decimal price,
        int sortOrder)
    {
        ValidateText(
            name,
            nameof(name),
            150);

        ValidateText(
            carrier,
            nameof(carrier),
            100);

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder));
        }

        Name = name.Trim();
        Carrier = carrier.Trim();
        Price = price;
        SortOrder = sortOrder;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;

        UpdatedAt =
            DateTime.UtcNow;
    }

    private static void ValidateText(
        string value,
        string parameterName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maxLength} characters.",
                parameterName);
        }
    }
}