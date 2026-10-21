namespace NexaECommerce.Server.Features.PointOfSale;

public sealed record PosCreateSaleRequest(
    string PaymentMethod,
    List<PosSaleLineRequest> Items);

public sealed record PosSaleLineRequest(
    string Barcode,
    int Quantity);

public sealed record PosProductLookupResponse(
    Guid ProductVariantId,
    string Barcode,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    string Currency);

public sealed record PosSaleItemResponse(
    Guid ProductVariantId,
    string Barcode,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record PosSaleResponse(
    Guid Id,
    string SaleNumber,
    string Status,
    string PaymentMethod,
    string Currency,
    decimal Subtotal,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    string? PaymentReference,
    IReadOnlyList<PosSaleItemResponse> Items);