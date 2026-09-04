using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class TaxRate : BaseEntity
{
    private TaxRate()
    {
    }

    private TaxRate(
        string tenantId,
        string code,
        string name,
        decimal ratePercent,
        bool isDefault)
    {
        TenantId =
            tenantId.Trim();

        Code =
            code.Trim()
                .ToUpperInvariant();

        Name =
            name.Trim();

        RatePercent =
            ratePercent;

        IsDefault =
            isDefault;

        IsActive =
            true;

        CreatedAt =
            DateTime.UtcNow;
    }

    public string TenantId { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public decimal RatePercent { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public static TaxRate Create(
        string tenantId,
        string code,
        string name,
        decimal ratePercent,
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(
                tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(
                code))
        {
            throw new ArgumentException(
                "Tax rate code is required.",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Tax rate name is required.",
                nameof(name));
        }

        if (ratePercent < 0 ||
            ratePercent > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ratePercent));
        }

        return new TaxRate(
            tenantId,
            code,
            name,
            ratePercent,
            isDefault);
    }

    public decimal Calculate(
        decimal taxableAmount)
    {
        if (taxableAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxableAmount));
        }

        if (!IsActive)
        {
            return 0;
        }

        return decimal.Round(
            taxableAmount *
            RatePercent /
            100m,
            2,
            MidpointRounding.AwayFromZero);
    }

    public void Update(
        string name,
        decimal ratePercent)
    {
        if (string.IsNullOrWhiteSpace(
                name))
        {
            throw new ArgumentException(
                "Tax rate name is required.",
                nameof(name));
        }

        if (ratePercent < 0 ||
            ratePercent > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ratePercent));
        }

        Name =
            name.Trim();

        RatePercent =
            ratePercent;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void SetDefault(
        bool value)
    {
        IsDefault =
            value;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive =
            true;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive =
            false;

        IsDefault =
            false;

        UpdatedAt =
            DateTime.UtcNow;
    }
}
