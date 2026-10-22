using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaEcommerce.Modules.Orders.Application.Pos;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.PointOfSale;

public sealed class PosCatalogReader(
    CatalogDbContext catalog,
    InventoryDbContext inventory,
    ICurrentTenant currentTenant) : IPosCatalogReader
{
    public async Task<IReadOnlyList<PosProductDto>> SearchProductsAsync(
        string search, int take, CancellationToken cancellationToken)
    {
        var normalized = search.Trim();
        take = Math.Clamp(take, 1, 100);

        var products = await catalog.Products
            .AsNoTrackingWithIdentityResolution()
            .Where(p => p.IsActive &&
                (p.Name.Contains(normalized) ||
                 p.Sku.Contains(normalized) ||
                 p.Variants.Any(v => v.IsActive &&
                     (v.Sku.Contains(normalized) ||
                      (v.Barcode != null && v.Barcode.Contains(normalized)))))
            )
            .OrderBy(p => p.Name)
            .Take(take)
            .Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.Images)
            .Include(p => p.Variants).ThenInclude(v => v.AttributeValues)
                .ThenInclude(x => x.AttributeValue)
                .ThenInclude(x => x.ProductAttribute)
            .ToListAsync(cancellationToken);

        var variants = products.SelectMany(p => p.Variants.Where(v => v.IsActive)).ToArray();
        var quantities = await GetAvailableQuantitiesAsync(
            variants.Select(v => v.Id).Distinct().ToArray(), cancellationToken);

        return products.Select(product =>
        {
            var mappedVariants = product.Variants
                .Where(v => v.IsActive)
                .OrderBy(v => v.Sku)
                .Select(v => MapVariant(v, product, quantities))
                .ToArray();

            return new PosProductDto(
                product.Id,
                product.Name,
                ProductImageUrl(product),
                mappedVariants);
        })
        .Where(p => p.Variants.Count > 0)
        .ToArray();
    }

    public async Task<PosVariantDto?> FindSellableVariantByBarcodeAsync(
        string barcode, CancellationToken cancellationToken)
    {
        var normalized = barcode.Trim();
        var variant = await catalog.ProductVariants
            .AsNoTrackingWithIdentityResolution()
            .Where(v => v.Barcode == normalized && v.IsActive && v.Product.IsActive)
            .Include(v => v.Product).ThenInclude(p => p.Images)
            .Include(v => v.Images)
            .Include(v => v.AttributeValues).ThenInclude(x => x.AttributeValue)
                .ThenInclude(x => x.ProductAttribute)
            .SingleOrDefaultAsync(cancellationToken);

        if (variant is null)
            return null;

        var quantities = await GetAvailableQuantitiesAsync(
            new[] { variant.Id }, cancellationToken);
        return MapVariant(variant, variant.Product, quantities);
    }

    public async Task<IReadOnlyList<PosVariantDto>> GetSellableVariantsAsync(
        IReadOnlyCollection<Guid> variantIds, CancellationToken cancellationToken)
    {
        var ids = variantIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
            return Array.Empty<PosVariantDto>();

        var variants = await catalog.ProductVariants
            .AsNoTrackingWithIdentityResolution()
            .Where(v => ids.Contains(v.Id) && v.IsActive && v.Product.IsActive)
            .Include(v => v.Product).ThenInclude(p => p.Images)
            .Include(v => v.Images)
            .Include(v => v.AttributeValues).ThenInclude(x => x.AttributeValue)
                .ThenInclude(x => x.ProductAttribute)
            .ToListAsync(cancellationToken);

        var quantities = await GetAvailableQuantitiesAsync(
            variants.Select(v => v.Id).ToArray(), cancellationToken);

        return variants.Select(v => MapVariant(v, v.Product, quantities)).ToArray();
    }

    private async Task<IReadOnlyDictionary<Guid, int>> GetAvailableQuantitiesAsync(
        IReadOnlyCollection<Guid> variantIds, CancellationToken ct)
    {
        if (variantIds.Count == 0 || string.IsNullOrWhiteSpace(currentTenant.Id))
            return new Dictionary<Guid, int>();

        var tenantId = currentTenant.Id;
        var warehouseIds = await inventory.Warehouses.AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.IsActive)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Code)
            .Select(w => w.Id)
            .ToArrayAsync(ct);

        if (warehouseIds.Length == 0)
            return new Dictionary<Guid, int>();

        var locationIds = await inventory.WarehouseLocations.AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.IsActive && warehouseIds.Contains(l.WarehouseId))
            .Select(l => l.Id)
            .ToArrayAsync(ct);

        if (locationIds.Length == 0)
            return new Dictionary<Guid, int>();

        var rows = await inventory.WarehouseStocks.AsNoTracking()
            .Where(s => s.TenantId == tenantId &&
                        variantIds.Contains(s.ProductVariantId) &&
                        warehouseIds.Contains(s.WarehouseId) &&
                        locationIds.Contains(s.LocationId))
            .Select(s => new
            {
                s.ProductVariantId,
                Available = s.OnHandQuantity - s.ReservedQuantity
            })
            .ToListAsync(ct);

        return rows.GroupBy(x => x.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0, x.Available)));
    }

    private static PosVariantDto MapVariant(
        ProductVariant variant,
        Product product,
        IReadOnlyDictionary<Guid, int> quantities)
    {
        var labels = variant.AttributeValues
            .Where(x => x.AttributeValue != null && x.AttributeValue.ProductAttribute != null)
            .OrderBy(x => x.AttributeValue.ProductAttribute.DisplayOrder)
            .Select(x => $"{x.AttributeValue.ProductAttribute.Name}: " +
                         (x.AttributeValue.DisplayValue ?? x.AttributeValue.Value))
            .ToArray();

        var image = variant.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => x.ImageUrl)
            .FirstOrDefault() ?? ProductImageUrl(product);

        return new PosVariantDto(
            variant.Id,
            product.Id,
            product.Name,
            image,
            variant.Sku,
            variant.Barcode,
            variant.PriceOverride,
            product.Currency,
            quantities.GetValueOrDefault(variant.Id),
            labels);
    }

    private static string? ProductImageUrl(Product product) => product.Images
        .OrderByDescending(x => x.IsPrimary)
        .ThenBy(x => x.DisplayOrder)
        .Select(x => x.ImageUrl)
        .FirstOrDefault();
}