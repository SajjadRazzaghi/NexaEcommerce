using NexaEcommerce.Modules.Catalog.Domain.Entities;

namespace NexaEcommerce.Modules.Catalog.Domain.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default);
  
    Task<bool> UpdateVariantAsync(
        Guid variantId,
        string sku,
        decimal priceOverride,
        decimal? comparePrice,
        bool isActive,
        CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugAsync(
    string slug,
    CancellationToken cancellationToken = default);

    Task<bool> ExistsBySkuAsync(
        string sku,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByVariantSkuAsync(
        string sku,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> GetFeaturedAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
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
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);
    Task DeleteNonVariantProductAttributesAsync(
    Guid productId,
    IReadOnlyCollection<string> variantAttributeCodes,
    CancellationToken cancellationToken = default);
    Task DeleteProductSpecificationAttributesAsync(
    Guid productId,
    IReadOnlyCollection<Guid> protectedAttributeIds,
    CancellationToken cancellationToken = default);
    void Update(
        Product product);
    void ClearTracking();
    void Delete(
        Product product);
    void DetachTrackedVariantAttributeMappings();
    void ResetModifiedAttributeValues();
    Task DeleteVariantAttributeMappingsAsync(
        Guid variantId,
        IReadOnlyCollection<Guid> attributeValueIds,
        CancellationToken cancellationToken = default);
   
    Task<bool> ExistsByVariantCombinationAsync(
     Guid productId,
     string combinationKey,
     Guid? excludeVariantId = null,
     CancellationToken cancellationToken = default);

    Task ReplaceVariantImagesAsync(
        Guid variantId,
        IReadOnlyCollection<string>? imageUrls,
        CancellationToken cancellationToken = default);
    void NormalizeTrackedProductAttributeStates();
    void NormalizeTrackedProductVariantStates();
    Task RepairTrackedProductAttributeStatesAsync(
    CancellationToken cancellationToken = default);

    Task AddVariantAttributeMappingsAsync(
        Guid variantId,
        IReadOnlyCollection<Guid> attributeValueIds,
        CancellationToken cancellationToken = default);

    Task ReplaceProductImagesAsync(
        Guid productId,
        IReadOnlyCollection<string>? imageUrls,
        CancellationToken cancellationToken = default);


}
