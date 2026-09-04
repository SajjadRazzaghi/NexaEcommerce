using NexaEcommerce.Modules.Orders.Domain.Entities;

namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record CouponDto(
    Guid Id,
    string Code,
    string Name,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinimumOrderAmount,
    decimal? MaximumDiscountAmount,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    int? UsageLimit,
    bool IsActive);

public sealed record CreateCouponRequest(
    string Code,
    string Name,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinimumOrderAmount,
    decimal? MaximumDiscountAmount,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    int? UsageLimit);

public sealed record UpdateCouponRequest(
    string Name,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinimumOrderAmount,
    decimal? MaximumDiscountAmount,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    int? UsageLimit);

public sealed record CouponValidationResultDto(
    string Code,
    bool IsValid,
    decimal DiscountAmount,
    string? Message);

public sealed record CouponRedemptionResultDto(
    string Code,
    bool Redeemed,
    decimal DiscountAmount,
    bool AlreadyRedeemed,
    string? Message);