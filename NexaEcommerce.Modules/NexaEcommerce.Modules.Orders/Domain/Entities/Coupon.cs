using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class Coupon : BaseEntity
{
    private Coupon()
    {
    }

    private Coupon(
        string tenantId,
        string code,
        string name,
        CouponDiscountType discountType,
        decimal discountValue,
        decimal? minimumOrderAmount,
        decimal? maximumDiscountAmount,
        DateTime? startsAt,
        DateTime? expiresAt,
        int? usageLimit)
    {
        TenantId = tenantId.Trim();
        Code = NormalizeCode(code);
        Name = name.Trim();

        DiscountType = discountType;
        DiscountValue = discountValue;

        MinimumOrderAmount =
            minimumOrderAmount;

        MaximumDiscountAmount =
            maximumDiscountAmount;

        StartsAt = startsAt;
        ExpiresAt = expiresAt;

        UsageLimit = usageLimit;

        IsActive = true;

        CreatedAt =
            DateTime.UtcNow;
    }

    public string TenantId { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public CouponDiscountType DiscountType { get; private set; }

    public decimal DiscountValue { get; private set; }

    public decimal? MinimumOrderAmount { get; private set; }

    public decimal? MaximumDiscountAmount { get; private set; }

    public DateTime? StartsAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public int? UsageLimit { get; private set; }

    public bool IsActive { get; private set; }

    public static Coupon Create(
        string tenantId,
        string code,
        string name,
        CouponDiscountType discountType,
        decimal discountValue,
        decimal? minimumOrderAmount = null,
        decimal? maximumDiscountAmount = null,
        DateTime? startsAt = null,
        DateTime? expiresAt = null,
        int? usageLimit = null)
    {
        ValidateRequired(
            tenantId,
            nameof(tenantId),
            64);

        ValidateRequired(
            code,
            nameof(code),
            64);

        ValidateRequired(
            name,
            nameof(name),
            150);

        if (!Enum.IsDefined(
                discountType))
        {
            throw new ArgumentException(
                "Invalid coupon discount type.",
                nameof(discountType));
        }

        if (discountValue <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountValue));
        }

        if (discountType ==
            CouponDiscountType.Percentage &&
            discountValue > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountValue),
                "Percentage discount cannot exceed 100.");
        }

        ValidateAmounts(
            minimumOrderAmount,
            maximumDiscountAmount);

        ValidateDates(
            startsAt,
            expiresAt);

        if (usageLimit is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usageLimit));
        }

        return new Coupon(
            tenantId,
            code,
            name,
            discountType,
            discountValue,
            minimumOrderAmount,
            maximumDiscountAmount,
            startsAt,
            expiresAt,
            usageLimit);
    }

    public bool IsCurrentlyValid(
        DateTime utcNow)
    {
        if (!IsActive)
            return false;

        if (StartsAt.HasValue &&
            utcNow < StartsAt.Value)
        {
            return false;
        }

        if (ExpiresAt.HasValue &&
            utcNow >= ExpiresAt.Value)
        {
            return false;
        }

        return true;
    }

    public decimal CalculateDiscount(
        decimal orderAmount)
    {
        if (orderAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(orderAmount));
        }

        if (MinimumOrderAmount.HasValue &&
            orderAmount <
            MinimumOrderAmount.Value)
        {
            return 0;
        }

        decimal discount;

        switch (DiscountType)
        {
            case CouponDiscountType.Percentage:
                discount =
                    orderAmount *
                    DiscountValue /
                    100m;
                break;

            case CouponDiscountType.FixedAmount:
                discount =
                    DiscountValue;
                break;

            default:
                throw new InvalidOperationException(
                    "Unsupported coupon discount type.");
        }

        if (MaximumDiscountAmount.HasValue)
        {
            discount =
                Math.Min(
                    discount,
                    MaximumDiscountAmount.Value);
        }

        return Math.Clamp(
            discount,
            0,
            orderAmount);
    }

    public void Update(
        string name,
        CouponDiscountType discountType,
        decimal discountValue,
        decimal? minimumOrderAmount,
        decimal? maximumDiscountAmount,
        DateTime? startsAt,
        DateTime? expiresAt,
        int? usageLimit)
    {
        ValidateRequired(
            name,
            nameof(name),
            150);

        if (!Enum.IsDefined(
                discountType))
        {
            throw new ArgumentException(
                "Invalid coupon discount type.",
                nameof(discountType));
        }

        if (discountValue <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountValue));
        }

        if (discountType ==
            CouponDiscountType.Percentage &&
            discountValue > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountValue));
        }

        ValidateAmounts(
            minimumOrderAmount,
            maximumDiscountAmount);

        ValidateDates(
            startsAt,
            expiresAt);

        if (usageLimit is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(usageLimit));
        }

        Name =
            name.Trim();

        DiscountType =
            discountType;

        DiscountValue =
            discountValue;

        MinimumOrderAmount =
            minimumOrderAmount;

        MaximumDiscountAmount =
            maximumDiscountAmount;

        StartsAt =
            startsAt;

        ExpiresAt =
            expiresAt;

        UsageLimit =
            usageLimit;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;

        UpdatedAt =
            DateTime.UtcNow;
    }

    private static string NormalizeCode(
        string value)
    {
        return value
            .Trim()
            .ToUpperInvariant();
    }

    private static void ValidateRequired(
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

    private static void ValidateAmounts(
        decimal? minimumOrderAmount,
        decimal? maximumDiscountAmount)
    {
        if (minimumOrderAmount is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumOrderAmount));
        }

        if (maximumDiscountAmount is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumDiscountAmount));
        }
    }

    private static void ValidateDates(
        DateTime? startsAt,
        DateTime? expiresAt)
    {
        if (startsAt.HasValue &&
            expiresAt.HasValue &&
            expiresAt <= startsAt)
        {
            throw new ArgumentException(
                "Coupon expiration must be after the start date.");
        }
    }
}

public enum CouponDiscountType
{
    Percentage = 1,
    FixedAmount = 2
}
