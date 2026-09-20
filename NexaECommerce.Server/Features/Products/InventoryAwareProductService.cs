using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Pagination;
using NexaECommerce.Server.Platform.MultiTenancy;

namespace NexaECommerce.Server.Features.Products;

/// <summary>
/// Catalog remains the single place where Product/ProductVariant are created.
/// Inventory quantity is read from WarehouseStock and never written to Catalog
/// by the product API.
/// </summary>
public sealed class InventoryAwareProductService(
    ProductService inner,
    IStockReader stockReader,
    ICurrentTenant currentTenant)
    : IProductService
{
    public async Task<PagedResult<ProductDto>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        Guid? categoryId = null,
        Guid? brandId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isFeatured = null,
        bool? isInStock = null,
        bool? isActive = null,
        bool? isPublished = null,
        bool includeInactive = false,
        bool includeUnpublished = false,
        string? sortBy = null,
        bool desc = false,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetPagedAsync(
            page,
            pageSize,
            search,
            categoryId,
            brandId,
            minPrice,
            maxPrice,
            isFeatured,
            isInStock,
            isActive,
            isPublished,
            includeInactive,
            includeUnpublished,
            sortBy,
            desc,
            cancellationToken);

        await ApplyInventoryStockAsync(result.Items, cancellationToken);
        return result;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products = (await inner.GetAllAsync(cancellationToken)).ToList();
        await ApplyInventoryStockAsync(products, cancellationToken);
        return products;
    }

    public async Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await inner.GetByIdAsync(id, cancellationToken);
        if (product is null) return null;

        await ApplyInventoryStockAsync([product], cancellationToken);
        return product;
    }

    public async Task<ProductDto?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var product = await inner.GetBySlugAsync(slug, cancellationToken);
        if (product is null) return null;

        await ApplyInventoryStockAsync([product], cancellationToken);
        return product;
    }

    public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var products = (await inner.GetByCategoryAsync(categoryId, cancellationToken)).ToList();
        await ApplyInventoryStockAsync(products, cancellationToken);
        return products;
    }

    public async Task<IEnumerable<ProductDto>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var products = (await inner.SearchAsync(searchTerm, cancellationToken)).ToList();
        await ApplyInventoryStockAsync(products, cancellationToken);
        return products;
    }

    public async Task<IEnumerable<ProductDto>> GetFeaturedAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        var products = (await inner.GetFeaturedAsync(count, cancellationToken)).ToList();
        await ApplyInventoryStockAsync(products, cancellationToken);
        return products;
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductDto createDto,
        CancellationToken cancellationToken = default)
    {
        var product = await inner.CreateAsync(
            SanitizeCreate(createDto),
            cancellationToken);

        await ApplyInventoryStockAsync([product], cancellationToken);
        return product;
    }

    public Task UpdateAsync(
        Guid id,
        UpdateProductDto updateDto,
        CancellationToken cancellationToken = default)
    {
        return inner.UpdateAsync(
            id,
            SanitizeUpdate(updateDto),
            cancellationToken);
    }

    public Task UpdateStockAsync(
        Guid id,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Product stock is managed through Inventory warehouses and locations. Select the existing product variant in Inventory instead of changing product stock here.");
    }

    public Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default) =>
        inner.SetActiveAsync(id, isActive, cancellationToken);

    public Task SetFeaturedAsync(Guid id, bool isFeatured, CancellationToken cancellationToken = default) =>
        inner.SetFeaturedAsync(id, isFeatured, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        inner.DeleteAsync(id, cancellationToken);

    private async Task ApplyInventoryStockAsync(
        IEnumerable<ProductDto> products,
        CancellationToken cancellationToken)
    {
        var list = products.ToList();
        var variants = list
            .SelectMany(x => x.Variants)
            .Where(x => x.IsActive)
            .ToList();

        var quantities = await stockReader.GetAvailableQuantitiesAsync(
            currentTenant.Id,
            variants.Select(x => x.Id),
            cancellationToken);

        foreach (var variant in variants)
        {
            variant.StockQuantity = quantities.TryGetValue(
                variant.Id,
                out var quantity)
                ? Math.Max(0, quantity)
                : 0;
        }

        foreach (var product in list)
        {
            var active = product.Variants.Where(x => x.IsActive).ToList();
            product.StockQuantity = active.Sum(x => x.StockQuantity);
            product.IsInStock = active.Any(x => x.StockQuantity > 0);
        }
    }

    private static CreateProductDto SanitizeCreate(CreateProductDto source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new CreateProductDto
        {
            Name = source.Name,
            Price = source.Price,
            Currency = source.Currency,
            Sku = source.Sku,
            Description = source.Description,
            ShortDescription = source.ShortDescription,
            BrandId = source.BrandId,
            ManufacturerId = source.ManufacturerId,
            CategoryIds = source.CategoryIds?.ToList() ?? [],
            Images = source.Images?.ToList() ?? [],
            Variants = (source.Variants ?? [])
                .Select(x => new CreateProductVariantDto
                {
                    Sku = x.Sku,
                    Color = x.Color,
                    Size = x.Size,
                    PriceOverride = x.PriceOverride,
                    StockQuantity = 0,
                    AttributeValueIds = x.AttributeValueIds?.ToList() ?? []
                })
                .ToList()
        };
    }

    private static UpdateProductDto SanitizeUpdate(UpdateProductDto source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new UpdateProductDto
        {
            Name = source.Name,
            Price = source.Price,
            Currency = source.Currency,
            Description = source.Description,
            ShortDescription = source.ShortDescription,
            ComparePrice = source.ComparePrice,
            DiscountPercentage = source.DiscountPercentage,
            BrandId = source.BrandId,
            ManufacturerId = source.ManufacturerId,
            CategoryIds = source.CategoryIds?.ToList() ?? [],
            IsActive = source.IsActive,
            IsFeatured = source.IsFeatured,
            IsPublished = source.IsPublished,
            Variants = (source.Variants ?? [])
                .Select(x => new UpdateProductVariantDto
                {
                    Id = x.Id,
                    Sku = x.Sku,
                    Color = x.Color,
                    Size = x.Size,
                    PriceOverride = x.PriceOverride,
                    ComparePrice = x.ComparePrice,
                    StockQuantity = null,
                    IsActive = x.IsActive,
                    AttributeValueIds = x.AttributeValueIds?.ToList() ?? []
                })
                .ToList()
        };
    }
}
