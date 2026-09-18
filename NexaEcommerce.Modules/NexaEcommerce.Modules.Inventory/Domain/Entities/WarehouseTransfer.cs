using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public enum WarehouseTransferStatus
{
    Pending = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed class WarehouseTransfer : AggregateRoot
{
    private WarehouseTransfer()
    {
    }

    private WarehouseTransfer(
        string tenantId,
        Guid sourceWarehouseId,
        Guid sourceLocationId,
        Guid destinationWarehouseId,
        Guid destinationLocationId,
        Guid productVariantId,
        int quantity,
        string? reason)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        ValidateGuid(
            sourceWarehouseId,
            nameof(sourceWarehouseId));

        ValidateGuid(
            sourceLocationId,
            nameof(sourceLocationId));

        ValidateGuid(
            destinationWarehouseId,
            nameof(destinationWarehouseId));

        ValidateGuid(
            destinationLocationId,
            nameof(destinationLocationId));

        ValidateGuid(
            productVariantId,
            nameof(productVariantId));

        if (sourceWarehouseId == destinationWarehouseId &&
            sourceLocationId == destinationLocationId)
        {
            throw new ArgumentException(
                "Source and destination cannot be the same location.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        TenantId = tenantId.Trim();

        SourceWarehouseId = sourceWarehouseId;
        SourceLocationId = sourceLocationId;

        DestinationWarehouseId = destinationWarehouseId;
        DestinationLocationId = destinationLocationId;

        ProductVariantId = productVariantId;

        Quantity = quantity;

        Reason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim();

        Status = WarehouseTransferStatus.Pending;
    }

    public string TenantId { get; private set; } = null!;

    public Guid SourceWarehouseId { get; private set; }

    public Guid SourceLocationId { get; private set; }

    public Guid DestinationWarehouseId { get; private set; }

    public Guid DestinationLocationId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    public WarehouseTransferStatus Status { get; private set; }

    public string? Reason { get; private set; }

    public DateTime RequestedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; private set; }

    public static WarehouseTransfer Create(
        string tenantId,
        Guid sourceWarehouseId,
        Guid sourceLocationId,
        Guid destinationWarehouseId,
        Guid destinationLocationId,
        Guid productVariantId,
        int quantity,
        string? reason = null)
    {
        return new WarehouseTransfer(
            tenantId,
            sourceWarehouseId,
            sourceLocationId,
            destinationWarehouseId,
            destinationLocationId,
            productVariantId,
            quantity,
            reason);
    }

    public void Complete()
    {
        if (Status != WarehouseTransferStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending transfers can be completed.");
        }

        Status = WarehouseTransferStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != WarehouseTransferStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending transfers can be cancelled.");
        }

        Status = WarehouseTransferStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateGuid(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid identifier is required.",
                parameterName);
        }
    }
}