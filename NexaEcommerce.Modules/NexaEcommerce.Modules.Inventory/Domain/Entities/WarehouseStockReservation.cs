using NexaEcommerce.SharedKernel.Domain;
namespace NexaEcommerce.Modules.Inventory.Domain.Entities;

public enum WarehouseStockReservationStatus
{
    Reserved = 1,
    Released = 2,
    Consumed = 3
}

public sealed class WarehouseStockReservation : BaseEntity
{
    private WarehouseStockReservation()
    {
    }

    private WarehouseStockReservation(
        string tenantId,
        Guid orderId,
        Guid fulfillmentId,
        Guid orderInventoryReservationId,
        string reservationKey,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        int quantity)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order id is required.",
                nameof(orderId));
        }

        if (fulfillmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Fulfillment id is required.",
                nameof(fulfillmentId));
        }

        if (orderInventoryReservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Order inventory reservation id is required.",
                nameof(orderInventoryReservationId));
        }

        if (string.IsNullOrWhiteSpace(reservationKey))
        {
            throw new ArgumentException(
                "Reservation key is required.",
                nameof(reservationKey));
        }

        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Warehouse id is required.",
                nameof(warehouseId));
        }

        if (locationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Location id is required.",
                nameof(locationId));
        }

        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        TenantId = tenantId.Trim();
        OrderId = orderId;
        FulfillmentId = fulfillmentId;
        OrderInventoryReservationId = orderInventoryReservationId;
        ReservationKey = reservationKey.Trim();
        WarehouseId = warehouseId;
        LocationId = locationId;
        ProductVariantId = productVariantId;
        Quantity = quantity;

        Status =
            WarehouseStockReservationStatus.Reserved;

        ReservedAt =
            DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = null!;

    public Guid OrderId { get; private set; }

    public Guid FulfillmentId { get; private set; }

    public Guid OrderInventoryReservationId { get; private set; }

    public string ReservationKey { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }

    public Guid LocationId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public int Quantity { get; private set; }

    public WarehouseStockReservationStatus Status { get; private set; }

    public DateTimeOffset ReservedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static WarehouseStockReservation Create(
        string tenantId,
        Guid orderId,
        Guid fulfillmentId,
        Guid orderInventoryReservationId,
        string reservationKey,
        Guid warehouseId,
        Guid locationId,
        Guid productVariantId,
        int quantity)
    {
        return new WarehouseStockReservation(
            tenantId,
            orderId,
            fulfillmentId,
            orderInventoryReservationId,
            reservationKey,
            warehouseId,
            locationId,
            productVariantId,
            quantity);
    }

    public bool IsReserved =>
        Status ==
        WarehouseStockReservationStatus.Reserved;

    public void Release()
    {
        if (Status ==
            WarehouseStockReservationStatus.Released)
        {
            return;
        }

        if (Status ==
            WarehouseStockReservationStatus.Consumed)
        {
            throw new InvalidOperationException(
                "Consumed warehouse stock reservation cannot be released.");
        }

        Status =
            WarehouseStockReservationStatus.Released;

        CompletedAt =
            DateTimeOffset.UtcNow;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Consume()
    {
        if (Status ==
            WarehouseStockReservationStatus.Consumed)
        {
            return;
        }

        if (Status !=
            WarehouseStockReservationStatus.Reserved)
        {
            throw new InvalidOperationException(
                "Only reserved warehouse stock can be consumed.");
        }

        Status =
            WarehouseStockReservationStatus.Consumed;

        CompletedAt =
            DateTimeOffset.UtcNow;

        UpdatedAt =
            DateTime.UtcNow;
    }
    public void RestoreToReserved()
    {
        if (Status ==
            WarehouseStockReservationStatus.Reserved)
        {
            return;
        }

        if (Status !=
            WarehouseStockReservationStatus.Consumed)
        {
            throw new InvalidOperationException(
                "Only consumed warehouse stock reservations can be restored.");
        }

        Status =
            WarehouseStockReservationStatus.Reserved;

        CompletedAt = null;

        UpdatedAt =
            DateTime.UtcNow;
    }
}