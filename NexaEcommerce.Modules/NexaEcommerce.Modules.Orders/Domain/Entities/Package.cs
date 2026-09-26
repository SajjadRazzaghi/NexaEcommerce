using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public enum PackageStatus
{
    Draft = 0,
    Packing = 1,
    Packed = 2,
    LabelPrinted = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}

public sealed class Package : AggregateRoot
{
    private Package()
    {
    }

    public string TenantId { get; private set; } = null!;

    public Guid OrderId { get; private set; }

    public Guid FulfillmentId { get; private set; }

    public int PackageNumber { get; private set; }

    public PackageStatus Status { get; private set; }

    public string? TrackingNumber { get; private set; }

    public decimal? WeightKg { get; private set; }

    public decimal? LengthCm { get; private set; }

    public decimal? WidthCm { get; private set; }

    public decimal? HeightCm { get; private set; }

    public DateTime? PackingStartedAt { get; private set; }

    public DateTime? PackedAt { get; private set; }

    public DateTime? LabelPrintedAt { get; private set; }

    public DateTime? ShippedAt { get; private set; }

    public DateTime? DeliveredAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public static Package Create(
        string tenantId,
        Guid orderId,
        Guid fulfillmentId,
        int packageNumber,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId is required.", nameof(orderId));

        if (fulfillmentId == Guid.Empty)
            throw new ArgumentException("FulfillmentId is required.", nameof(fulfillmentId));

        if (packageNumber <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(packageNumber),
                "Package number must be greater than zero.");

        return new Package
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = orderId,
            FulfillmentId = fulfillmentId,
            PackageNumber = packageNumber,
            Status = PackageStatus.Draft,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void StartPacking(DateTime utcNow)
    {
        EnsureStatus(
            PackageStatus.Draft,
            PackageStatus.Packing);

        Status = PackageStatus.Packing;
        PackingStartedAt ??= utcNow;
        UpdatedAt = utcNow;
    }

    public void SetDimensions(
        decimal? weightKg,
        decimal? lengthCm,
        decimal? widthCm,
        decimal? heightCm,
        DateTime utcNow)
    {
        if (weightKg.HasValue && weightKg.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(weightKg));

        if (lengthCm.HasValue && lengthCm.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(lengthCm));

        if (widthCm.HasValue && widthCm.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(widthCm));

        if (heightCm.HasValue && heightCm.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(heightCm));

        WeightKg = weightKg;
        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
        UpdatedAt = utcNow;
    }

    public void MarkPacked(DateTime utcNow)
    {
        EnsureStatus(PackageStatus.Packing);

        Status = PackageStatus.Packed;
        PackedAt = utcNow;
        UpdatedAt = utcNow;
    }
    public void RollbackToPacked(
    DateTime utcNow)
    {
        EnsureStatus(
            PackageStatus.Packed,
            PackageStatus.LabelPrinted);

        Status =
            PackageStatus.Packed;

        LabelPrintedAt = null;
        TrackingNumber = null;

        UpdatedAt =
            utcNow;
    }

    public void RollbackToPacking(
        DateTime utcNow)
    {
        EnsureStatus(
            PackageStatus.Packed,
            PackageStatus.LabelPrinted);

        Status =
            PackageStatus.Packing;

        PackedAt = null;
        LabelPrintedAt = null;
        TrackingNumber = null;

        UpdatedAt =
            utcNow;
    }

    public void RollbackToDraft(
        DateTime utcNow)
    {
        EnsureStatus(
            PackageStatus.Packing);

        Status =
            PackageStatus.Draft;

        PackingStartedAt = null;
        PackedAt = null;
        LabelPrintedAt = null;
        TrackingNumber = null;

        UpdatedAt =
            utcNow;
    }
    public void MarkLabelPrinted(DateTime utcNow)
    {
        EnsureStatus(
            PackageStatus.Packed,
            PackageStatus.LabelPrinted);

        Status = PackageStatus.LabelPrinted;
        LabelPrintedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void SetTrackingNumber(
        string trackingNumber,
        DateTime utcNow)
    {
        if (Status == PackageStatus.Cancelled)
            throw new InvalidOperationException(
                "A cancelled package cannot receive a tracking number.");

        if (Status == PackageStatus.Delivered)
            throw new InvalidOperationException(
                "A delivered package cannot receive a tracking number.");

        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException(
                "Tracking number is required.",
                nameof(trackingNumber));

        TrackingNumber = trackingNumber.Trim();
        UpdatedAt = utcNow;
    }

    public void MarkShipped(DateTime utcNow)
    {
        EnsureStatus(
            PackageStatus.Packed,
            PackageStatus.LabelPrinted);

        if (string.IsNullOrWhiteSpace(TrackingNumber))
            throw new InvalidOperationException(
                "Tracking number must be set before shipping.");

        Status = PackageStatus.Shipped;
        ShippedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void MarkDelivered(DateTime utcNow)
    {
        EnsureStatus(PackageStatus.Shipped);

        Status = PackageStatus.Delivered;
        DeliveredAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void Cancel(DateTime utcNow)
    {
        if (Status == PackageStatus.Shipped ||
            Status == PackageStatus.Delivered)
        {
            throw new InvalidOperationException(
                "A shipped or delivered package cannot be cancelled.");
        }

        Status = PackageStatus.Cancelled;
        UpdatedAt = utcNow;
    }

    private void EnsureStatus(params PackageStatus[] allowed)
    {
        if (allowed.Contains(Status))
            return;

        throw new InvalidOperationException(
            $"Package transition from '{Status}' is not allowed.");
    }
}
