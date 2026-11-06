using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexaEcommerce.Modules.Inventory.Domain.Entities;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Application.Pos;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;

namespace NexaECommerce.Server.Features.PointOfSale;

public sealed class PosSaleCommitter(
    OrdersDbContext orders,
    InventoryDbContext inventory) : IPosSaleCommitter
{
    public async Task<PosSaleResultDto?> FindByIdempotencyKeyAsync(
        string tenantId, string sellerUserId, string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var sale = await orders.PosSales.AsNoTracking()
            .Include(x => x.Items)
            .SingleOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.SellerUserId == sellerUserId &&
                x.IdempotencyKey == idempotencyKey,
                cancellationToken);
        return sale is null ? null : Map(sale);
    }

    public async Task<IReadOnlyList<PosSaleResultDto>> ListSalesAsync(
        string tenantId, int skip, int take, CancellationToken cancellationToken)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);

        var sales = await orders.PosSales.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.SaleNumber)
            .Skip(skip)
            .Take(take)
            .Include(x => x.Items)
            .ToListAsync(cancellationToken);

        return sales.Select(Map).ToArray();
    }

    public async Task<PosSaleResultDto?> FindSaleByIdAsync(
        string tenantId, Guid saleId, CancellationToken cancellationToken)
    {
        var sale = await orders.PosSales.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == saleId)
            .Include(x => x.Items)
            .SingleOrDefaultAsync(cancellationToken);
        return sale is null ? null : Map(sale);
    }
    public async Task<PosSaleResultDto?> FindSaleByIdForSellerAsync(
    string tenantId,
    string sellerUserId,
    Guid saleId,
    CancellationToken cancellationToken)
    {
        var sale = await orders.PosSales
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.SellerUserId == sellerUserId &&
                x.Id == saleId)
            .Include(x => x.Items)
            .SingleOrDefaultAsync(cancellationToken);

        return sale is null ? null : Map(sale);
    }
    // Existing cash-sale path. Card payments must use CreatePendingCardSaleAndReserveStockAsync.
    public Task<PosSaleResultDto> CommitSaleAndDecrementStockAtomicallyAsync(
        PosSaleDraft draft, CancellationToken cancellationToken)
        => CommitSaleInternalAsync(draft, reserveForCardPayment: false, cancellationToken);

    // Card-sale path: reserve inventory and persist PendingPayment; do not mark as paid here.
    public Task<PosSaleResultDto> CreatePendingCardSaleAndReserveStockAsync(
        PosSaleDraft draft, CancellationToken cancellationToken)
        => CommitSaleInternalAsync(draft, reserveForCardPayment: true, cancellationToken);

    private async Task<PosSaleResultDto> CommitSaleInternalAsync(
        PosSaleDraft draft,
        bool reserveForCardPayment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        BindSharedConnection();
        var strategy = orders.Database.CreateExecutionStrategy();

        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                orders.ChangeTracker.Clear();
                inventory.ChangeTracker.Clear();

                await using var transaction = await orders.Database
                    .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                await inventory.Database.UseTransactionAsync(
                    transaction.GetDbTransaction(), cancellationToken);

                try
                {
                    // A repeated HTTP request with the same idempotency key returns the original sale.
                    var prior = await orders.PosSales
                        .Include(x => x.Items)
                        .SingleOrDefaultAsync(x =>
                            x.TenantId == draft.TenantId &&
                            x.SellerUserId == draft.SellerUserId &&
                            x.IdempotencyKey == draft.IdempotencyKey,
                            cancellationToken);

                    if (prior is not null)
                    {
                        await transaction.CommitAsync(cancellationToken);
                        return Map(prior);
                    }

                    var paymentMethod =
       draft.PaymentMethod.Equals(
           "Cash",
           StringComparison.OrdinalIgnoreCase)
           ? PosPaymentMethod.Cash
           : draft.PaymentMethod.Equals(
               "Card",
               StringComparison.OrdinalIgnoreCase)
               ? PosPaymentMethod.BankMellat
               : draft.PaymentMethod.Equals(
                   "ManualCard",
                   StringComparison.OrdinalIgnoreCase)
                   ? PosPaymentMethod.ManualCard
                   : throw new PosValidationException(
                       "روش پرداخت پشتیبانی نمی‌شود.");

                    if (reserveForCardPayment && paymentMethod != PosPaymentMethod.BankMellat)
                        throw new PosValidationException("رزرو انتظار پرداخت فقط برای کارتخوان مجاز است.");

                    if (!reserveForCardPayment && paymentMethod == PosPaymentMethod.BankMellat)
                        throw new PosValidationException("فروش کارتخوان باید از مسیر تأیید Bridge انجام شود.");

                    if (reserveForCardPayment)
                    {
                        // DB unique filtered index also protects against concurrent requests.
                        var unresolvedCardPayment = await orders.PosSales.AnyAsync(x =>
                            x.TenantId == draft.TenantId &&
                            x.SellerUserId == draft.SellerUserId &&
                            x.PaymentMethod == PosPaymentMethod.BankMellat &&
                            (x.Status == PosSaleStatus.PendingPayment ||
                             x.Status == PosSaleStatus.PaymentInProgress ||
                             x.Status == PosSaleStatus.ReconciliationRequired),
                            cancellationToken);

                        if (unresolvedCardPayment)
                            throw new PosConflictException(
                                "برای این فروشنده یک پرداخت کارتخوان باز یا نیازمند تطبیق وجود دارد. ابتدا وضعیت آن را مشخص کنید.");
                    }

                    var sale = new PosSale(
                        draft.TenantId,
                        draft.SellerUserId,
                        draft.IdempotencyKey,
                        paymentMethod,
                        draft.Currency);

                    foreach (var line in draft.Items)
                    {
                        var saleItem = new PosSaleItem(
                            sale.Id,
                            line.ProductVariantId,
                            line.Barcode,
                            line.Sku,
                            line.ProductName,
                            line.Quantity,
                            line.UnitPrice,
                            $"POS-{sale.Id:N}-{line.ProductVariantId:N}",
                            line.AttributeLabels);
                        sale.AddItem(saleItem);
                    }

                    if (reserveForCardPayment)
                    {
                        await ReserveStockForCardPaymentAsync(draft, sale.Id, cancellationToken);
                        // Status intentionally remains PendingPayment. The Bridge claims it later.
                    }
                    else
                    {
                        await DecrementStockAsync(draft, sale.Id, cancellationToken);
                        sale.MarkPaid(draft.PaymentReference);
                    }

                    orders.PosSales.Add(sale);

                    await inventory.SaveChangesAsync(cancellationToken);
                    await orders.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Map(sale);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
                finally
                {
                    await inventory.Database.UseTransactionAsync(null, CancellationToken.None);
                }
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            orders.ChangeTracker.Clear();
            inventory.ChangeTracker.Clear();
            throw new PosConflictException(
                "موجودی هم‌زمان توسط فروش دیگری تغییر کرده است؛ سبد را تازه‌سازی و دوباره ثبت کنید.");
        }
        catch (DbUpdateException)
        {
            orders.ChangeTracker.Clear();
            inventory.ChangeTracker.Clear();

            var existing = await FindByIdempotencyKeyAsync(
                draft.TenantId, draft.SellerUserId, draft.IdempotencyKey,
                CancellationToken.None);
            if (existing is not null)
                return existing;

            throw new PosConflictException(
                "ثبت فاکتور یا رزرو موجودی با تعارض دیتابیس مواجه شد؛ وضعیت انبار و پرداخت‌های باز را بررسی کنید.");
        }
    }

    private void BindSharedConnection()
    {
        var ordersConnection = orders.Database.GetDbConnection();
        var inventoryConnection = inventory.Database.GetDbConnection();

        if (ReferenceEquals(ordersConnection, inventoryConnection))
            return;

        if (inventoryConnection.State != ConnectionState.Closed)
            throw new InvalidOperationException(
                "Inventory connection must be closed before sharing the POS transaction.");

        inventory.Database.SetDbConnection(ordersConnection, contextOwnsConnection: false);
    }

    private async Task DecrementStockAsync(
        PosSaleDraft draft, Guid saleId, CancellationToken ct)
    {
        var variantIds = draft.Items.Select(x => x.ProductVariantId).Distinct().ToArray();
        var warehouses = await inventory.Warehouses
            .Where(w => w.TenantId == draft.TenantId && w.IsActive)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Code)
            .Select(w => new { w.Id, w.Code, w.IsDefault })
            .ToListAsync(ct);

        if (warehouses.Count == 0)
            throw new PosInsufficientStockException("هیچ انبار فعالی برای فروش وجود ندارد.");

        var warehouseIds = warehouses.Select(w => w.Id).ToArray();
        var warehouseRank = warehouses
            .Select((w, index) => new { w.Id, Index = index })
            .ToDictionary(x => x.Id, x => x.Index);

        var locations = await inventory.WarehouseLocations
            .Where(l => l.TenantId == draft.TenantId &&
                        l.IsActive && warehouseIds.Contains(l.WarehouseId))
            .Select(l => new { l.Id, l.WarehouseId })
            .ToListAsync(ct);
        var locationIds = locations.Select(l => l.Id).ToArray();

        var stocks = await inventory.WarehouseStocks
            .Where(s => s.TenantId == draft.TenantId &&
                        variantIds.Contains(s.ProductVariantId) &&
                        warehouseIds.Contains(s.WarehouseId) &&
                        locationIds.Contains(s.LocationId))
            .ToListAsync(ct);

        foreach (var line in draft.Items)
        {
            var remaining = line.Quantity;
            var candidates = stocks
                .Where(s => s.ProductVariantId == line.ProductVariantId)
                .OrderBy(s => warehouseRank.GetValueOrDefault(s.WarehouseId, int.MaxValue))
                .ThenBy(s => s.LocationId)
                .ToArray();

            foreach (var stock in candidates)
            {
                var available = stock.AvailableQuantity;
                if (available <= 0)
                    continue;

                var allocated = Math.Min(available, remaining);
                stock.RemoveOnHand(allocated);

                inventory.WarehouseStockMovements.Add(
                    WarehouseStockMovement.Create(
                        draft.TenantId,
                        stock.WarehouseId,
                        stock.LocationId,
                        stock.ProductVariantId,
                        WarehouseStockMovementType.Sale,
                        -allocated,
                        stock.OnHandQuantity,
                        "PosSale",
                        saleId.ToString("D"),
                        $"فروش حضوری {saleId:D}"));

                remaining -= allocated;
                if (remaining == 0)
                    break;
            }

            if (remaining > 0)
                throw new PosInsufficientStockException(
                    $"موجودی قابل فروش برای «{line.ProductName}» کافی نیست. سبد را به‌روزرسانی کنید.");
        }
    }

    private async Task ReserveStockForCardPaymentAsync(
        PosSaleDraft draft, Guid saleId, CancellationToken ct)
    {
        var variantIds = draft.Items.Select(x => x.ProductVariantId).Distinct().ToArray();

        var warehouses = await inventory.Warehouses
            .Where(w => w.TenantId == draft.TenantId && w.IsActive)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Code)
            .Select(w => new { w.Id, w.Code, w.IsDefault })
            .ToListAsync(ct);

        if (warehouses.Count == 0)
            throw new PosInsufficientStockException("هیچ انبار فعالی برای فروش وجود ندارد.");

        var warehouseIds = warehouses.Select(w => w.Id).ToArray();
        var warehouseRank = warehouses
            .Select((w, index) => new { w.Id, Index = index })
            .ToDictionary(x => x.Id, x => x.Index);

        var locations = await inventory.WarehouseLocations
            .Where(l => l.TenantId == draft.TenantId &&
                        l.IsActive && warehouseIds.Contains(l.WarehouseId))
            .Select(l => new { l.Id, l.WarehouseId })
            .ToListAsync(ct);
        var locationIds = locations.Select(l => l.Id).ToArray();

        var stocks = await inventory.WarehouseStocks
            .Where(s => s.TenantId == draft.TenantId &&
                        variantIds.Contains(s.ProductVariantId) &&
                        warehouseIds.Contains(s.WarehouseId) &&
                        locationIds.Contains(s.LocationId))
            .ToListAsync(ct);

        foreach (var line in draft.Items)
        {
            var remaining = line.Quantity;
            var candidates = stocks
                .Where(s => s.ProductVariantId == line.ProductVariantId)
                .OrderBy(s => warehouseRank.GetValueOrDefault(s.WarehouseId, int.MaxValue))
                .ThenBy(s => s.LocationId)
                .ToArray();

            foreach (var stock in candidates)
            {
                var available = stock.AvailableQuantity;
                if (available <= 0)
                    continue;

                var allocated = Math.Min(available, remaining);
                stock.Reserve(allocated);

                inventory.WarehouseStockMovements.Add(
                    WarehouseStockMovement.Create(
                        draft.TenantId,
                        stock.WarehouseId,
                        stock.LocationId,
                        stock.ProductVariantId,
                        WarehouseStockMovementType.Reservation,
                        allocated,
                        stock.OnHandQuantity,
                        "PosSalePayment",
                        saleId.ToString("D"),
                        $"رزرو موجودی برای پرداخت فاکتور POS {saleId:D}"));

                remaining -= allocated;
                if (remaining == 0)
                    break;
            }

            if (remaining > 0)
                throw new PosInsufficientStockException(
                    $"موجودی قابل فروش برای «{line.ProductName}» کافی نیست. سبد را به‌روزرسانی کنید.");
        }
    }

    private static PosSaleResultDto Map(PosSale sale)
    {
        var createdAtUtc = DateTime.SpecifyKind(sale.CreatedAtUtc, DateTimeKind.Utc);
        var items = sale.Items.Select(item => new PosSaleLineResultDto(
            item.ProductVariantId,
            item.ProductName,
            item.Sku,
            item.Barcode,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal,
            ReadAttributes(item.AttributesJson))).ToArray();

        return new PosSaleResultDto(
            sale.Id,
            sale.SaleNumber,
            new DateTimeOffset(createdAtUtc),
            sale.Currency,
            sale.Status.ToString(),
            sale.PaymentMethod.ToString(),
            sale.PaymentReference,
            sale.Subtotal,
            sale.TotalAmount,
            items);
    }

    private static IReadOnlyList<string> ReadAttributes(string? json)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json ?? "[]") ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
