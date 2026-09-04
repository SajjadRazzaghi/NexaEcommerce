using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Orders.Domain.Entities;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    private readonly List<OrderInventoryReservation>
        _inventoryReservations = [];

    private Order()
    {
    }

    private Order(
        string tenantId,
        string userId,
        string orderNumber,
        string idempotencyKey,
        string currency,
        decimal subtotal,
        decimal shippingAmount,
        decimal discountAmount,
        string shippingFullName,
        string shippingPhone,
        string shippingAddress,
        string shippingCity,
        string? shippingPostalCode)
    {
        TenantId = tenantId;
        UserId = userId;
        OrderNumber = orderNumber;
        IdempotencyKey = idempotencyKey;
        Currency = currency;

        Subtotal = subtotal;
        ShippingAmount = shippingAmount;
        DiscountAmount = discountAmount;

        ShippingFullName = shippingFullName;
        ShippingPhone = shippingPhone;
        ShippingAddress = shippingAddress;
        ShippingCity = shippingCity;
        ShippingPostalCode = shippingPostalCode;

        TotalAmount =
            Subtotal +
            ShippingAmount -
            DiscountAmount;

        Status =
            OrderStatus.PendingPayment;
    }

    public string TenantId { get; private set; } = null!;

    public string UserId { get; private set; } = null!;

    public string OrderNumber { get; private set; } = null!;

    public string IdempotencyKey { get; private set; } = null!;

    public OrderStatus Status { get; private set; }

    public string Currency { get; private set; } = "IRR";

    public decimal Subtotal { get; private set; }

    public decimal ShippingAmount { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal TotalAmount { get; private set; }

    public string ShippingFullName { get; private set; } = null!;

    public string ShippingPhone { get; private set; } = null!;

    public string ShippingAddress { get; private set; } = null!;

    public string ShippingCity { get; private set; } = null!;

    public string? ShippingPostalCode { get; private set; }
    public decimal TaxableAmount { get; private set; }

    public decimal TaxRatePercent { get; private set; }

    public decimal TaxAmount { get; private set; }

    public string? CouponCode { get; private set; }
    public IReadOnlyCollection<OrderItem> Items =>
        _items.AsReadOnly();

    public IReadOnlyCollection<OrderInventoryReservation>
        InventoryReservations =>
        _inventoryReservations.AsReadOnly();

    public static Order Create(
        string tenantId,
        string userId,
        string orderNumber,
        string idempotencyKey,
        string currency,
        decimal subtotal,
        decimal shippingAmount,
        decimal discountAmount,
        string shippingFullName,
        string shippingPhone,
        string shippingAddress,
        string shippingCity,
        string? shippingPostalCode)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new ArgumentException(
                nameof(orderNumber));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                nameof(idempotencyKey));
        }

        if (idempotencyKey.Trim().Length > 128)
        {
            throw new ArgumentException(
                "Idempotency key cannot exceed 128 characters.",
                nameof(idempotencyKey));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                nameof(currency));
        }

        if (subtotal < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subtotal));
        }

        if (shippingAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shippingAmount));
        }

        if (discountAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountAmount));
        }

        if (string.IsNullOrWhiteSpace(shippingFullName))
        {
            throw new ArgumentException(
                nameof(shippingFullName));
        }

        if (string.IsNullOrWhiteSpace(shippingPhone))
        {
            throw new ArgumentException(
                nameof(shippingPhone));
        }

        if (string.IsNullOrWhiteSpace(shippingAddress))
        {
            throw new ArgumentException(
                nameof(shippingAddress));
        }

        if (string.IsNullOrWhiteSpace(shippingCity))
        {
            throw new ArgumentException(
                nameof(shippingCity));
        }

        return new Order(
            tenantId.Trim(),
            userId.Trim(),
            orderNumber.Trim(),
            idempotencyKey.Trim(),
            currency.Trim(),
            subtotal,
            shippingAmount,
            discountAmount,
            shippingFullName.Trim(),
            shippingPhone.Trim(),
            shippingAddress.Trim(),
            shippingCity.Trim(),
            string.IsNullOrWhiteSpace(
                shippingPostalCode)
                ? null
                : shippingPostalCode.Trim());
    }

    public OrderItem AddItem(
        Guid productVariantId,
        string sku,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                nameof(productVariantId));
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException(
                nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException(
                nameof(productName));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity));
        }

        var existingItem =
            _items.FirstOrDefault(
                x =>
                    x.ProductVariantId ==
                    productVariantId);

        if (existingItem is not null)
        {
            throw new InvalidOperationException(
                $"Product variant '{productVariantId}' already exists in this order.");
        }

        var item =
            new OrderItem(
                Id,
                productVariantId,
                sku.Trim(),
                productName.Trim(),
                unitPrice,
                quantity);

        _items.Add(item);

        RecalculateTotals();

        return item;
    }

    public void ApplyPricing(
        decimal subtotal,
        decimal shippingAmount,
        decimal discountAmount,
        decimal totalAmount)
    {
        if (subtotal < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subtotal));
        }

        if (shippingAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shippingAmount));
        }

        if (discountAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountAmount));
        }

        if (totalAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalAmount));
        }

        Subtotal = subtotal;
        ShippingAmount = shippingAmount;
        DiscountAmount = discountAmount;
        TotalAmount = totalAmount;

        UpdatedAt =
            DateTime.UtcNow;
    }
    public void ApplyPricing(
    decimal subtotal,
    decimal shippingAmount,
    decimal discountAmount,
    decimal taxableAmount,
    decimal taxRatePercent,
    decimal taxAmount,
    decimal totalAmount,
    string? couponCode)
    {
        if (subtotal < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subtotal));
        }

        if (shippingAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(shippingAmount));
        }

        if (discountAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountAmount));
        }

        if (taxableAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxableAmount));
        }

        if (taxRatePercent < 0m ||
            taxRatePercent > 100m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxRatePercent));
        }

        if (taxAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxAmount));
        }

        if (totalAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalAmount));
        }

        Subtotal = subtotal;
        ShippingAmount = shippingAmount;
        DiscountAmount = discountAmount;
        TaxableAmount = taxableAmount;
        TaxRatePercent = taxRatePercent;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
        CouponCode =
            string.IsNullOrWhiteSpace(couponCode)
                ? null
                : couponCode.Trim();

        UpdatedAt =
            DateTime.UtcNow;
    }
    public OrderInventoryReservation
        AddInventoryReservation(
            string reservationKey,
            Guid productVariantId,
            int quantity,
            DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(
                reservationKey))
        {
            throw new ArgumentException(
                nameof(reservationKey));
        }

        var normalizedKey =
            reservationKey.Trim();

        var existing =
            _inventoryReservations
                .FirstOrDefault(
                    x =>
                        string.Equals(
                            x.ReservationKey,
                            normalizedKey,
                            StringComparison.Ordinal));

        if (existing is not null)
        {
            if (existing.ProductVariantId !=
                productVariantId)
            {
                throw new InvalidOperationException(
                    "Reservation key is already associated with another product variant.");
            }

            if (existing.Quantity != quantity)
            {
                throw new InvalidOperationException(
                    "Reservation key is already associated with another quantity.");
            }

            return existing;
        }

        var reservation =
            OrderInventoryReservation.Create(
                Id,
                TenantId,
                normalizedKey,
                productVariantId,
                quantity,
                expiresAt);

        _inventoryReservations.Add(
            reservation);

        UpdatedAt =
            DateTime.UtcNow;

        return reservation;
    }

    public bool HasActiveInventoryReservations =>
        _inventoryReservations.Any(
            x =>
                x.Status ==
                InventoryReservationStatus.Reserved);

    public bool HasCommittedInventoryReservations =>
        _inventoryReservations.Any(
            x =>
                x.Status ==
                InventoryReservationStatus.Committed);

    public bool AreAllInventoryReservationsCommitted =>
        _inventoryReservations.Count > 0 &&
        _inventoryReservations.All(
            x =>
                x.Status ==
                InventoryReservationStatus.Committed);

    public void MarkInventoryReservationsCommitted()
    {
        foreach (var reservation in
                 _inventoryReservations.Where(
                     x =>
                         x.Status ==
                         InventoryReservationStatus.Reserved))
        {
            reservation.MarkCommitted();
        }

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void MarkInventoryReservationsReleased()
    {
        foreach (var reservation in
                 _inventoryReservations.Where(
                     x =>
                         x.Status ==
                         InventoryReservationStatus.Reserved))
        {
            reservation.MarkReleased();
        }

        UpdatedAt =
            DateTime.UtcNow;
    }

    public bool MarkInventoryReservationExpired(
        string reservationKey)
    {
        if (string.IsNullOrWhiteSpace(
                reservationKey))
        {
            return false;
        }

        var reservation =

            _inventoryReservations.FirstOrDefault(
                x =>
                    string.Equals(
                        x.ReservationKey,
                        reservationKey.Trim(),
                        StringComparison.Ordinal));

        if (reservation is null)
        {
            return false;
        }

        reservation.MarkExpired();

        UpdatedAt =
            DateTime.UtcNow;

        return true;
    }

    public int MarkExpiredInventoryReservations(
        DateTimeOffset now)
    {
        var count = 0;

        foreach (var reservation in
                 _inventoryReservations.Where(
                     x =>
                         x.Status ==
                         InventoryReservationStatus.Reserved &&
                         x.ExpiresAt <= now))
        {
            reservation.MarkExpired();
            count++;
        }

        if (count > 0)
        {
            UpdatedAt =
                DateTime.UtcNow;
        }

        return count;
    }

    public void MarkPaid()
    {
        if (Status !=
            OrderStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                "Only pending orders can be marked as paid.");
        }

        Status =
            OrderStatus.Paid;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void StartProcessing()
    {
        if (Status !=
            OrderStatus.Paid)
        {
            throw new InvalidOperationException(
                "Only paid orders can start processing.");
        }

        Status =
            OrderStatus.Processing;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void MarkShipped()
    {
        if (Status !=
            OrderStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only processing orders can be shipped.");
        }

        Status =
            OrderStatus.Shipped;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        if (Status !=
            OrderStatus.Shipped)
        {
            throw new InvalidOperationException(
                "Only shipped orders can be delivered.");
        }

        Status =
            OrderStatus.Delivered;

        UpdatedAt =
            DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is
            OrderStatus.Shipped or
            OrderStatus.Delivered)
        {
            throw new InvalidOperationException(
                "Shipped or delivered orders cannot be cancelled.");
        }

        MarkInventoryReservationsReleased();

        Status =
            OrderStatus.Cancelled;

        UpdatedAt =
            DateTime.UtcNow;
    }

    private void RecalculateTotals()
    {
        Subtotal =
            _items.Sum(
                x => x.LineTotal);

        UpdatedAt =
            DateTime.UtcNow;
    }
}

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}