using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Application.Pos;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;
using NexaEcommerce.SharedKernel.Domain.ValueObjects;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace NexaECommerce.Server.Features.PointOfSale;

public sealed class PosBridgeService(
    OrdersDbContext orders,
    InventoryDbContext inventory) : IPosBridgeService
{
    private const string ReservationReferenceType = "PosSalePayment";

    public async Task<PosBridgeCreateDeviceResponse> CreateDeviceAsync(
        string tenantId, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new PosValidationException("شناسه فروشگاه معتبر نیست.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new PosValidationException("نام دستگاه الزامی است و نباید بیشتر از ۱۰۰ نویسه باشد.");

        var existingDevice = await orders.Set<PosBridgeDevice>()
            .AnyAsync(x => x.TenantId == tenantId && x.IsActive, ct);
        if (existingDevice)
            throw new PosConflictException("برای این فروشگاه یک Bridge فعال وجود دارد. ابتدا Bridge قبلی را غیرفعال کنید.");

        var token = CreateToken();
        var device = PosBridgeDevice.Create(tenantId, name, HashToken(token));
        orders.Set<PosBridgeDevice>().Add(device);
        await orders.SaveChangesAsync(ct);

        // Plaintext token is returned only in this response. Only SHA-256 is persisted.
        return new PosBridgeCreateDeviceResponse(device.Id, device.Name, token);
    }

    public async Task DisableDeviceAsync(string tenantId, Guid deviceId, CancellationToken ct)
    {
        var device = await orders.Set<PosBridgeDevice>().SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == deviceId, ct);
        if (device is null) throw new PosNotFoundException("دستگاه Bridge پیدا نشد.");

        var hasPaymentInProgress = await orders.PosSales.AnyAsync(x =>
            x.TenantId == tenantId &&
            x.BridgeDeviceId == deviceId &&
            x.PaymentMethod == PosPaymentMethod.BankMellat &&
            x.Status == PosSaleStatus.PaymentInProgress, ct);

        if (hasPaymentInProgress)
            throw new PosConflictException(
                "این Bridge در حال پردازش پرداخت است. ابتدا نتیجه کارتخوان را دریافت یا تراکنش را تطبیق دهید.");

        device.Disable();
        await orders.SaveChangesAsync(ct);
    }

    public async Task<PosBridgeHealthDto> GetHealthAsync(string token, CancellationToken ct)
    {
        var device = await AuthenticateAsync(token, ct);
        device.Touch();
        await orders.SaveChangesAsync(ct);
        return new PosBridgeHealthDto(true, device.Name);
    }
    public async Task<PosBridgeAvailabilityDto> GetSellerAvailabilityAsync(
    string tenantId,
    CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return new PosBridgeAvailabilityDto(
                false,
                false,
                null,
                null,
                "شناسه فروشگاه معتبر نیست.");
        }

        var device = await orders.Set<PosBridgeDevice>()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Name,
                x.LastSeenAtUtc
            })
            .FirstOrDefaultAsync(ct);

        if (device is null)
        {
            return new PosBridgeAvailabilityDto(
                false,
                false,
                null,
                null,
                "برای این فروشگاه Bridge فعالی ثبت نشده است.");
        }

        // Bridge باید حداقل هر ۶۰ ثانیه Heartbeat داشته باشد.
        // ۹۰ ثانیه برای تأخیرهای کوتاه شبکه در نظر گرفته شده است.
        var onlineSince = DateTime.UtcNow.AddSeconds(-90);

        var online =
            device.LastSeenAtUtc.HasValue &&
            device.LastSeenAtUtc.Value >= onlineSince;

        return new PosBridgeAvailabilityDto(
            true,
            online,
            device.Name,
            device.LastSeenAtUtc,
            online
                ? null
                : "ارتباط با نرم‌افزار Bridge برقرار نیست.");
    }
    public async Task<PosSalePaymentStatusDto?> GetSellerSaleStatusAsync(
        string tenantId, string sellerUserId, Guid saleId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(sellerUserId) || saleId == Guid.Empty)
            return null;

        var sale = await orders.PosSales.AsNoTracking().SingleOrDefaultAsync(x =>
            x.TenantId == tenantId && x.SellerUserId == sellerUserId && x.Id == saleId, ct);

        return sale is null
            ? null
            : new PosSalePaymentStatusDto(sale.Id, sale.Status.ToString(), sale.PaymentReference);
    }

    public async Task<PosSalePaymentStatusDto?> GetSellerUnresolvedSaleStatusAsync(
        string tenantId, string sellerUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(sellerUserId))
            return null;

        var sale = await orders.PosSales.AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.SellerUserId == sellerUserId &&
                        x.PaymentMethod == PosPaymentMethod.BankMellat &&
                        (x.Status == PosSaleStatus.PendingPayment ||
                         x.Status == PosSaleStatus.PaymentInProgress ||
                         x.Status == PosSaleStatus.ReconciliationRequired))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        return sale is null
            ? null
            : new PosSalePaymentStatusDto(sale.Id, sale.Status.ToString(), sale.PaymentReference);
    }

    public async Task<PosBridgePaymentWorkItemDto?> ClaimNextAsync(
        string token, CancellationToken ct)
    {
        var device = await AuthenticateAsync(token, ct);

        // Serializable transaction + queue index prevents two installed Bridges from
        // claiming the same pending card sale concurrently.
        await using var transaction = await orders.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);

        // Re-check after opening the transaction so a just-disabled Bridge cannot claim new work.
        var stillActive = await orders.Set<PosBridgeDevice>().AnyAsync(
            x => x.Id == device.Id && x.TenantId == device.TenantId && x.IsActive, ct);
        if (!stillActive)
            throw new UnauthorizedAccessException();

        // A claimed payment that is still unresolved after the SDK's maximum timeout
        // becomes reconciliation work. It is never re-queued to the terminal.
        var heartbeatDue = !device.LastSeenAtUtc.HasValue ||
                           device.LastSeenAtUtc.Value < DateTime.UtcNow.AddSeconds(-60);
        var staleBefore = DateTime.UtcNow.AddMinutes(-12);
        var staleClaims = await orders.PosSales
            .Where(x => x.TenantId == device.TenantId &&
                        x.PaymentMethod == PosPaymentMethod.BankMellat &&
                        x.Status == PosSaleStatus.PaymentInProgress &&
                        x.PaymentStartedAtUtc.HasValue &&
                        x.PaymentStartedAtUtc.Value < staleBefore)
            .Take(20)
            .ToListAsync(ct);
        foreach (var stale in staleClaims)
            stale.MarkReconciliationRequired("Bridge پس از Claim پاسخ نهایی ثبت نکرد؛ وضعیت باید از پنل پذیرنده تطبیق داده شود.");

        var sale = await orders.PosSales
            .Include(x => x.Items)
            .Where(x => x.TenantId == device.TenantId &&
                        x.PaymentMethod == PosPaymentMethod.BankMellat &&
                        x.Status == PosSaleStatus.PendingPayment)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.SaleNumber)
            .FirstOrDefaultAsync(ct);

        if (heartbeatDue)
            device.Touch();

        if (sale is null)
        {
            if (heartbeatDue || staleClaims.Count > 0)
                await orders.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return null;
        }

        long amountInRial;

        try
        {
            amountInRial = IranianCurrency.ToRial(
                sale.TotalAmount,
                sale.Currency);
        }
        catch (Exception ex) when (
            ex is NotSupportedException ||
            ex is OverflowException ||
            ex is ArgumentOutOfRangeException)
        {
            sale.MarkReconciliationRequired(
                "مبلغ یا واحد پول فاکتور برای کارتخوان معتبر نیست.");

            await orders.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return null;
        }
        sale.MarkPaymentInProgress(device.Id);
        await orders.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new PosBridgePaymentWorkItemDto(
      sale.Id,
      sale.SaleNumber,
      amountInRial,
      IranianCurrency.Rial);
    }

    public async Task<PosBridgeCompletionResponse> CompleteAsync(
     string token,
     Guid saleId,
     PosBridgePaymentCompletionRequest request,
     CancellationToken ct)
    {
        var device = await AuthenticateAsync(token, ct);

        if (saleId == Guid.Empty)
            throw new PosValidationException("شناسه فاکتور معتبر نیست.");

        if (request is null)
            throw new PosValidationException("نتیجه پرداخت ارسال نشده است.");

        if (request.Amount <= 0)
            throw new PosValidationException("مبلغ برگشتی معتبر نیست.");

        var outcome = request.Outcome?.Trim();

        if (outcome is not ("Succeeded" or "Declined" or "Unknown"))
            throw new PosValidationException(
                "Outcome فقط می‌تواند Succeeded، Declined یا Unknown باشد.");

        BindSharedConnection();

        var strategy = orders.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            orders.ChangeTracker.Clear();
            inventory.ChangeTracker.Clear();

            await using var transaction =
                await orders.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);

            await inventory.Database.UseTransactionAsync(
                transaction.GetDbTransaction(), ct);

            try
            {
                var sale = await orders.PosSales
                    .Include(x => x.Items)
                    .SingleOrDefaultAsync(
                        x => x.TenantId == device.TenantId &&
                             x.Id == saleId, ct);

                if (sale is null)
                    throw new PosNotFoundException(
                        "فاکتور متعلق به این فروشگاه پیدا نشد.");

                // Idempotent callback
                if (sale.Status is PosSaleStatus.Paid
                    or PosSaleStatus.Cancelled
                    or PosSaleStatus.ReconciliationRequired)
                {
                    await transaction.CommitAsync(ct);

                    return new PosBridgeCompletionResponse(
                        sale.Id,
                        sale.Status.ToString());
                }

                if (sale.Status != PosSaleStatus.PaymentInProgress ||
                    sale.BridgeDeviceId != device.Id)
                {
                    throw new PosConflictException(
                        "این فاکتور توسط این Bridge در حال پردازش نیست.");
                }

                var expectedRialAmount = IranianCurrency.ToRial(
                    sale.TotalAmount,
                    sale.Currency);

                if (expectedRialAmount != request.Amount)
                {
                    sale.MarkReconciliationRequired(
                        "مبلغ پاسخ کارتخوان با مبلغ فاکتور مطابقت ندارد.");

                    await orders.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    return new PosBridgeCompletionResponse(
                        sale.Id,
                        sale.Status.ToString());
                }

                if (outcome == "Unknown")
                {
                    // وضعیت نامشخص؛ رزرو موجودی حفظ می‌شود.
                    sale.MarkReconciliationRequired(
                        SafeMessage(request.Message));
                }
                else
                {
                    var allocations = await inventory.WarehouseStockMovements
                        .Where(x =>
                            x.TenantId == device.TenantId &&
                            x.ReferenceType == ReservationReferenceType &&
                            x.ReferenceId == sale.Id.ToString("D") &&
                            x.Type == WarehouseStockMovementType.Reservation &&
                            x.QuantityDelta > 0)
                        .OrderBy(x => x.WarehouseId)
                        .ThenBy(x => x.LocationId)
                        .ThenBy(x => x.ProductVariantId)
                        .ToListAsync(ct);

                    if (allocations.Count == 0)
                    {
                        sale.MarkReconciliationRequired(
                            "رزرو موجودی فاکتور یافت نشد؛ بررسی دستی لازم است.");
                    }
                    else if (outcome == "Succeeded")
                    {
                        foreach (var allocation in allocations)
                        {
                            var stock = await inventory.WarehouseStocks
                                .SingleOrDefaultAsync(x =>
                                    x.TenantId == device.TenantId &&
                                    x.WarehouseId == allocation.WarehouseId &&
                                    x.LocationId == allocation.LocationId &&
                                    x.ProductVariantId == allocation.ProductVariantId,
                                    ct);

                            if (stock is null)
                            {
                                throw new PosConflictException(
                                    "ردیف موجودی رزروشده یافت نشد؛ فاکتور به بررسی دستی نیاز دارد.");
                            }

                            stock.ConsumeReserved(allocation.QuantityDelta);

                            inventory.WarehouseStockMovements.Add(
                                WarehouseStockMovement.Create(
                                    device.TenantId,
                                    allocation.WarehouseId,
                                    allocation.LocationId,
                                    allocation.ProductVariantId,
                                    WarehouseStockMovementType.Sale,
                                    -allocation.QuantityDelta,
                                    stock.OnHandQuantity,
                                    "PosSale",
                                    sale.Id.ToString("D"),
                                    $"فروش حضوری پرداخت‌شده {sale.SaleNumber}"));
                        }

                        sale.MarkGatewayPaid(
                            NormalizeReference(request.PaymentReference),
                            SafeMessage(request.Message));
                    }
                    else // Declined
                    {
                        foreach (var allocation in allocations)
                        {
                            var stock = await inventory.WarehouseStocks
                                .SingleOrDefaultAsync(x =>
                                    x.TenantId == device.TenantId &&
                                    x.WarehouseId == allocation.WarehouseId &&
                                    x.LocationId == allocation.LocationId &&
                                    x.ProductVariantId == allocation.ProductVariantId,
                                    ct);

                            if (stock is null)
                            {
                                throw new PosConflictException(
                                    "ردیف موجودی رزروشده یافت نشد؛ فاکتور به بررسی دستی نیاز دارد.");
                            }

                            stock.Release(allocation.QuantityDelta);

                            inventory.WarehouseStockMovements.Add(
                                WarehouseStockMovement.Create(
                                    device.TenantId,
                                    allocation.WarehouseId,
                                    allocation.LocationId,
                                    allocation.ProductVariantId,
                                    WarehouseStockMovementType.ReservationRelease,
                                    -allocation.QuantityDelta,
                                    stock.OnHandQuantity,
                                    "PosSalePayment",
                                    sale.Id.ToString("D"),
                                    $"آزادسازی رزرو پرداخت ناموفق {sale.SaleNumber}"));
                        }

                        sale.MarkGatewayDeclined(
                            SafeMessage(request.Message));
                    }
                }

                await inventory.SaveChangesAsync(ct);
                await orders.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                return new PosBridgeCompletionResponse(
                    sale.Id,
                    sale.Status.ToString());
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
            finally
            {
                await inventory.Database.UseTransactionAsync(
                    null, CancellationToken.None);
            }
        });
    }
    public async Task<PosBridgeCompletionResponse> CancelUnclaimedAsync(
      string tenantId,
      string sellerUserId,
      Guid saleId,
      CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(sellerUserId) ||
            saleId == Guid.Empty)
        {
            throw new PosValidationException(
                "اطلاعات فروشنده یا فاکتور معتبر نیست.");
        }

        BindSharedConnection();

        // SQL Server retry strategy must execute the entire transaction.
        var strategy = orders.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            orders.ChangeTracker.Clear();
            inventory.ChangeTracker.Clear();

            await using var transaction =
                await orders.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    ct);

            await inventory.Database.UseTransactionAsync(
                transaction.GetDbTransaction(),
                ct);

            try
            {
                var sale = await orders.PosSales
                    .SingleOrDefaultAsync(x =>
                        x.TenantId == tenantId &&
                        x.SellerUserId == sellerUserId &&
                        x.Id == saleId,
                        ct);

                if (sale is null)
                {
                    throw new PosNotFoundException(
                        "فاکتور متعلق به این فروشنده پیدا نشد.");
                }

                // Idempotent retry: an already-cancelled sale
                // must never release its reservation again.
                if (sale.Status == PosSaleStatus.Cancelled)
                {
                    await transaction.CommitAsync(ct);

                    return new PosBridgeCompletionResponse(
                        sale.Id,
                        sale.Status.ToString());
                }

                // Only an unclaimed payment may be cancelled.
                if (sale.PaymentMethod != PosPaymentMethod.BankMellat ||
                    sale.Status != PosSaleStatus.PendingPayment ||
                    sale.BridgeDeviceId.HasValue ||
                    sale.PaymentStartedAtUtc.HasValue)
                {
                    throw new PosConflictException(
                        "این فاکتور قبلاً توسط Bridge دریافت شده یا وضعیت آن نامشخص است. لغو خودکار مجاز نیست.");
                }

                var allocations = await inventory.WarehouseStockMovements
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.ReferenceType == ReservationReferenceType &&
                        x.ReferenceId == sale.Id.ToString("D") &&
                        x.Type == WarehouseStockMovementType.Reservation &&
                        x.QuantityDelta > 0)
                    .OrderBy(x => x.WarehouseId)
                    .ThenBy(x => x.LocationId)
                    .ThenBy(x => x.ProductVariantId)
                    .ToListAsync(ct);

                if (allocations.Count == 0)
                {
                    throw new PosConflictException(
                        "رزرو موجودی این فاکتور پیدا نشد؛ آزادسازی خودکار امن نیست.");
                }

                foreach (var allocation in allocations)
                {
                    var stock = await inventory.WarehouseStocks
                        .SingleOrDefaultAsync(x =>
                            x.TenantId == tenantId &&
                            x.WarehouseId == allocation.WarehouseId &&
                            x.LocationId == allocation.LocationId &&
                            x.ProductVariantId == allocation.ProductVariantId,
                            ct);

                    if (stock is null)
                    {
                        throw new PosConflictException(
                            "ردیف موجودی رزروشده پیدا نشد.");
                    }

                    stock.Release(allocation.QuantityDelta);

                    inventory.WarehouseStockMovements.Add(
                        WarehouseStockMovement.Create(
                            tenantId,
                            allocation.WarehouseId,
                            allocation.LocationId,
                            allocation.ProductVariantId,
                            WarehouseStockMovementType.ReservationRelease,
                            -allocation.QuantityDelta,
                            stock.OnHandQuantity,
                            "PosSalePayment",
                            sale.Id.ToString("D"),
                            $"آزادسازی رزرو درخواست ارسال‌نشده {sale.SaleNumber}"));
                }

                sale.MarkCancelled();

                await inventory.SaveChangesAsync(ct);
                await orders.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                return new PosBridgeCompletionResponse(
                    sale.Id,
                    sale.Status.ToString());
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
            finally
            {
                await inventory.Database.UseTransactionAsync(
                    null,
                    CancellationToken.None);
            }
        });
    }
    public async Task<PosBridgeCompletionResponse> ReconcileAsync(
    string tenantId,
    Guid saleId,
    PosBridgeReconciliationRequest request,
    CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId) ||
            saleId == Guid.Empty)
        {
            throw new PosValidationException(
                "شناسه فروشگاه یا فاکتور معتبر نیست.");
        }

        if (request is null)
            throw new PosValidationException(
                "اطلاعات تطبیق ارسال نشده است.");

        var outcome = request.Outcome?.Trim();

        if (outcome is not ("Succeeded" or "Declined"))
        {
            throw new PosValidationException(
                "نتیجه تطبیق باید Succeeded یا Declined باشد.");
        }

        BindSharedConnection();

        var strategy = orders.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            orders.ChangeTracker.Clear();
            inventory.ChangeTracker.Clear();

            await using var transaction =
                await orders.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, ct);

            await inventory.Database.UseTransactionAsync(
                transaction.GetDbTransaction(), ct);

            try
            {
                var sale = await orders.PosSales
                    .SingleOrDefaultAsync(
                        x => x.TenantId == tenantId &&
                             x.Id == saleId, ct);

                if (sale is null)
                    throw new PosNotFoundException("فاکتور پیدا نشد.");

                if (sale.Status is PosSaleStatus.Paid
                    or PosSaleStatus.Cancelled)
                {
                    await transaction.CommitAsync(ct);

                    return new PosBridgeCompletionResponse(
                        sale.Id,
                        sale.Status.ToString());
                }

                if (sale.Status != PosSaleStatus.ReconciliationRequired ||
                    sale.PaymentMethod != PosPaymentMethod.BankMellat)
                {
                    throw new PosConflictException(
                        "فقط فاکتور کارتخوان در وضعیت ReconciliationRequired قابل تطبیق است.");
                }

                var allocations = await inventory.WarehouseStockMovements
                    .Where(x =>
                        x.TenantId == tenantId &&
                        x.ReferenceType == ReservationReferenceType &&
                        x.ReferenceId == sale.Id.ToString("D") &&
                        x.Type == WarehouseStockMovementType.Reservation &&
                        x.QuantityDelta > 0)
                    .OrderBy(x => x.WarehouseId)
                    .ThenBy(x => x.LocationId)
                    .ThenBy(x => x.ProductVariantId)
                    .ToListAsync(ct);

                if (allocations.Count == 0)
                {
                    throw new PosConflictException(
                        "رزرو موجودی فاکتور پیدا نشد؛ تطبیق خودکار ایمن نیست.");
                }

                foreach (var allocation in allocations)
                {
                    var stock = await inventory.WarehouseStocks
                        .SingleOrDefaultAsync(x =>
                            x.TenantId == tenantId &&
                            x.WarehouseId == allocation.WarehouseId &&
                            x.LocationId == allocation.LocationId &&
                            x.ProductVariantId == allocation.ProductVariantId,
                            ct);

                    if (stock is null)
                    {
                        throw new PosConflictException(
                            "ردیف موجودی رزروشده پیدا نشد.");
                    }

                    if (outcome == "Succeeded")
                    {
                        stock.ConsumeReserved(allocation.QuantityDelta);

                        inventory.WarehouseStockMovements.Add(
                            WarehouseStockMovement.Create(
                                tenantId,
                                allocation.WarehouseId,
                                allocation.LocationId,
                                allocation.ProductVariantId,
                                WarehouseStockMovementType.Sale,
                                -allocation.QuantityDelta,
                                stock.OnHandQuantity,
                                "PosSale",
                                sale.Id.ToString("D"),
                                $"فروش حضوری تطبیق‌شده {sale.SaleNumber}"));
                    }
                    else
                    {
                        stock.Release(allocation.QuantityDelta);

                        inventory.WarehouseStockMovements.Add(
                            WarehouseStockMovement.Create(
                                tenantId,
                                allocation.WarehouseId,
                                allocation.LocationId,
                                allocation.ProductVariantId,
                                WarehouseStockMovementType.ReservationRelease,
                                -allocation.QuantityDelta,
                                stock.OnHandQuantity,
                                "PosSalePayment",
                                sale.Id.ToString("D"),
                                $"آزادسازی رزرو پس از تطبیق {sale.SaleNumber}"));
                    }
                }

                if (outcome == "Succeeded")
                {
                    sale.MarkReconciledPaid(
                        NormalizeReference(request.PaymentReference),
                        SafeMessage(request.Reason));
                }
                else
                {
                    sale.MarkReconciledDeclined(
                        SafeMessage(request.Reason));
                }

                await inventory.SaveChangesAsync(ct);
                await orders.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                return new PosBridgeCompletionResponse(
                    sale.Id,
                    sale.Status.ToString());
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
            finally
            {
                await inventory.Database.UseTransactionAsync(
                    null, CancellationToken.None);
            }
        });
    }
    private async Task<PosBridgeDevice> AuthenticateAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200)
            throw new UnauthorizedAccessException();

        var hash = HashToken(token.Trim());
        var device = await orders.Set<PosBridgeDevice>()
            .SingleOrDefaultAsync(x => x.TokenHash == hash && x.IsActive, ct);
        return device ?? throw new UnauthorizedAccessException();
    }

    private void BindSharedConnection()
    {
        var ordersConnection = orders.Database.GetDbConnection();
        var inventoryConnection = inventory.Database.GetDbConnection();
        if (ReferenceEquals(ordersConnection, inventoryConnection)) return;
        if (inventoryConnection.State != ConnectionState.Closed)
            throw new InvalidOperationException("Inventory connection must be closed before sharing the POS transaction.");
        inventory.Database.SetDbConnection(ordersConnection, contextOwnsConnection: false);
    }

    private static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    private static string? NormalizeReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var clean = new string(value.Trim().Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        if (clean.Length == 0 || clean.Length > 64 || clean.Length >= 13 && clean.All(char.IsDigit)) return null;
        return clean;
    }

    private static string SafeMessage(string? value)
    {
        // The Bridge sends an authored, sanitized status only. Do not persist raw SDK output.
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var safe = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return safe.Length <= 1000 ? safe : safe.Substring(0, 1000);
    }
}
