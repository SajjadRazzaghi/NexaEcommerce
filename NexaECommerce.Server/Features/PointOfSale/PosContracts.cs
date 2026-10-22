using System.ComponentModel.DataAnnotations;

namespace NexaEcommerce.Modules.Orders.Application.Pos;

public sealed record PosVariantDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    string Sku,
    string? Barcode,
    decimal UnitPrice,
    string Currency,
    int AvailableQuantity,
    IReadOnlyList<string> AttributeLabels);

public sealed record PosProductDto(
    Guid Id,
    string Name,
    string? ImageUrl,
    IReadOnlyList<PosVariantDto> Variants);

public sealed record PosSaleLineRequestDto(Guid ProductVariantId, int Quantity);

public sealed record PosCreateSaleRequestDto
{
    [Required, MinLength(16), MaxLength(128)]
    public string IdempotencyKey { get; init; } = string.Empty;

    // Cash یا Card. Credit عمداً پشتیبانی نمی‌شود.
    [Required, MaxLength(20)]
    public string PaymentMethod { get; init; } = string.Empty;

    [MaxLength(256)]
    public string? PaymentReference { get; init; }

    [Required, MinLength(1), MaxLength(100)]
    public IReadOnlyList<PosSaleLineRequestDto> Items { get; init; }
        = Array.Empty<PosSaleLineRequestDto>();
}

public sealed record PosSaleLineResultDto(
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    string? Barcode,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    IReadOnlyList<string> AttributeLabels);

public sealed record PosSaleResultDto(
    Guid Id,
    string SaleNumber,
    DateTimeOffset CreatedAt,
    string Currency,
    string Status,
    string PaymentMethod,
    string? PaymentReference,
    decimal Subtotal,
    decimal TotalAmount,
    IReadOnlyList<PosSaleLineResultDto> Items);

public sealed record PosSaleDraftLine(
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    string? Barcode,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    IReadOnlyList<string> AttributeLabels);

public sealed record PosSaleDraft(
    string TenantId,
    string SellerUserId,
    string IdempotencyKey,
    string Currency,
    string PaymentMethod,
    string? PaymentReference,
    decimal Subtotal,
    decimal TotalAmount,
    IReadOnlyList<PosSaleDraftLine> Items);

public sealed class PosValidationException(string message) : Exception(message);
public sealed class PosNotFoundException(string message) : Exception(message);
public sealed class PosInsufficientStockException(string message) : Exception(message);
public sealed class PosConflictException(string message) : Exception(message);

public interface IPosCatalogReader
{
    Task<IReadOnlyList<PosProductDto>> SearchProductsAsync(
        string search, int take, CancellationToken cancellationToken);

    Task<PosVariantDto?> FindSellableVariantByBarcodeAsync(
        string barcode, CancellationToken cancellationToken);

    Task<IReadOnlyList<PosVariantDto>> GetSellableVariantsAsync(
        IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken);
}

public interface IPosExecutionContext
{
    string TenantId { get; }
    string UserId { get; }
}

public interface IPosSaleCommitter
{
    Task<PosSaleResultDto?> FindByIdempotencyKeyAsync(
        string tenantId, string sellerUserId, string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PosSaleResultDto>> ListSalesAsync(
        string tenantId, int skip, int take, CancellationToken cancellationToken);

    Task<PosSaleResultDto?> FindSaleByIdAsync(
        string tenantId, Guid saleId, CancellationToken cancellationToken);

    Task<PosSaleResultDto> CommitSaleAndDecrementStockAtomicallyAsync(
        PosSaleDraft draft, CancellationToken cancellationToken);
}

public interface IPosOrchestrator
{
    Task<IReadOnlyList<PosProductDto>> SearchProductsAsync(string? search, CancellationToken cancellationToken);
    Task<PosVariantDto> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    Task<PosSaleResultDto> CreateSaleAsync(PosCreateSaleRequestDto request, CancellationToken cancellationToken);
}