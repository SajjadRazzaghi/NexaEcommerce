namespace NexaEcommerce.Modules.Orders.Application.DTOs;

public sealed record PackageDto(
    Guid Id,
    Guid OrderId,
    Guid FulfillmentId,
    int PackageNumber,
    string Status,
    string? TrackingNumber,
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm,
    DateTime? PackingStartedAt,
    DateTime? PackedAt,
    DateTime? LabelPrintedAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreatePackageRequest;

public sealed record UpdatePackageDimensionsRequest(
    decimal? WeightKg,
    decimal? LengthCm,
    decimal? WidthCm,
    decimal? HeightCm);

public sealed record SetPackageTrackingRequest(
    string TrackingNumber);

public sealed record PackageLabelDto(
    Guid PackageId,
    Guid OrderId,
    Guid FulfillmentId,
    int PackageNumber,
    string OrderNumber,
    string RecipientFullName,
    string RecipientPhone,
    string RecipientAddress,
    string RecipientCity,
    string? RecipientPostalCode,
    string? TrackingNumber,
    decimal? WeightKg,
    string Status,
    DateTime GeneratedAtUtc);
