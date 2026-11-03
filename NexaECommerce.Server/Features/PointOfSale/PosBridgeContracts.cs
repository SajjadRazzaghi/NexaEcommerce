namespace NexaECommerce.Server.Features.PointOfSale;

public sealed record PosBridgeCreateDeviceRequest(string Name);
public sealed record PosBridgeCreateDeviceResponse(Guid DeviceId, string Name, string Token);
public sealed record PosBridgeHealthDto(bool Ok, string DeviceName);
public sealed record PosBridgePaymentWorkItemDto(Guid SaleId, string SaleNumber, long Amount, string Currency);
public sealed record PosBridgePaymentCompletionRequest(
    string Outcome,
    long Amount,
    string? PaymentReference,
    string? Message);
public sealed record PosBridgeReconciliationRequest(string Outcome, string? PaymentReference, string? Reason);
public sealed record PosBridgeCompletionResponse(Guid SaleId, string Status);
public sealed record PosSalePaymentStatusDto(Guid SaleId, string Status, string? PaymentReference);

public interface IPosBridgeService
{
    Task<PosBridgeCreateDeviceResponse> CreateDeviceAsync(string tenantId, string name, CancellationToken ct);
    Task DisableDeviceAsync(string tenantId, Guid deviceId, CancellationToken ct);
    Task<PosBridgeHealthDto> GetHealthAsync(string token, CancellationToken ct);
    Task<PosBridgePaymentWorkItemDto?> ClaimNextAsync(string token, CancellationToken ct);
    Task<PosSalePaymentStatusDto?> GetSellerSaleStatusAsync(string tenantId, string sellerUserId, Guid saleId, CancellationToken ct);
    Task<PosSalePaymentStatusDto?> GetSellerUnresolvedSaleStatusAsync(string tenantId, string sellerUserId, CancellationToken ct);
    Task<PosBridgeCompletionResponse> CompleteAsync(
        string token, Guid saleId, PosBridgePaymentCompletionRequest request, CancellationToken ct);
    Task<PosBridgeCompletionResponse> ReconcileAsync(
        string tenantId, Guid saleId, PosBridgeReconciliationRequest request, CancellationToken ct);
}
