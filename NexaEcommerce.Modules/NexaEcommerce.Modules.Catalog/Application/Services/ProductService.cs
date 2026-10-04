using AutoMapper;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Pagination;
using System.Text.RegularExpressions;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICatalogAttributeRepository _catalogAttributeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ICatalogAttributeRepository catalogAttributeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _catalogAttributeRepository = catalogAttributeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // ============================================================
    // Queries
    // ============================================================

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
        var result =
            await _productRepository.GetPagedAsync(
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

        var items =
            _mapper.Map<IReadOnlyList<ProductDto>>(
                result.Items);

        return PagedResult<ProductDto>.Create(
            items,
            page,
            pageSize,
            result.TotalItems);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products =
            await _productRepository.GetAllAsync(
                cancellationToken);

        return _mapper.Map<IEnumerable<ProductDto>>(
            products);
    }

    public async Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product =
            await _productRepository.GetByIdAsync(
                id,
                cancellationToken);

        return product is null
            ? null
            : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var product =
            await _productRepository.GetBySlugAsync(
                slug,
                cancellationToken);

        return product is null
            ? null
            : _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var products =
            await _productRepository.GetByCategoryAsync(
                categoryId,
                cancellationToken);

        return _mapper.Map<IEnumerable<ProductDto>>(
            products);
    }

    public async Task<IEnumerable<ProductDto>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var products =
            await _productRepository.SearchAsync(
                searchTerm,
                cancellationToken);

        return _mapper.Map<IEnumerable<ProductDto>>(
            products);
    }

    public async Task<IEnumerable<ProductDto>> GetFeaturedAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        var products =
            await _productRepository.GetFeaturedAsync(
                count,
                cancellationToken);

        return _mapper.Map<IEnumerable<ProductDto>>(
            products);
    }

    // ============================================================
    // Create
    // ============================================================

    public async Task<ProductDto> CreateAsync(
        CreateProductDto createDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        if (string.IsNullOrWhiteSpace(createDto.Name))
        {
            throw new ArgumentException(
                "Product name is required.",
                nameof(createDto.Name));
        }

        if (createDto.Price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(createDto.Price),
                "Product price cannot be negative.");
        }

        var slug =
            await CreateUniqueSlugAsync(
                createDto.Name,
                cancellationToken);

        var sku =
            string.IsNullOrWhiteSpace(createDto.Sku)
                ? GenerateSku()
                : createDto.Sku.Trim();

        if (await _productRepository.ExistsBySkuAsync(
                sku,
                cancellationToken: cancellationToken))
        {
            throw new ArgumentException(
                $"Product SKU '{sku}' already exists.",
                nameof(createDto.Sku));
        }

        var product = new Product(
            createDto.Name.Trim(),
            sku,
            slug,
            createDto.Price,
            createDto.Currency ?? "IRR",
            createDto.Description);

        product.SetShortDescription(
            createDto.ShortDescription);

        product.SetBrand(
            createDto.BrandId);

        product.SetManufacturer(
            createDto.ManufacturerId);

        // --------------------------------------------------------
        // Images
        // --------------------------------------------------------

        foreach (var imageUrl in
                 createDto.Images ?? new List<string>())
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                continue;

            var displayOrder =
                product.Images.Count;

            product.AddImage(
                imageUrl.Trim(),
                displayOrder,
                displayOrder == 0);
        }

        // --------------------------------------------------------
        // Variants
        // --------------------------------------------------------

        foreach (var variantDto in
                 createDto.Variants ??
                 new List<CreateProductVariantDto>())
        {
            if (string.IsNullOrWhiteSpace(
                    variantDto.Sku))
            {
                throw new ArgumentException(
                    "Variant SKU is required.",
                    nameof(createDto));
            }

            var variant =
                product.AddVariant(
                    variantDto.Sku.Trim(),
                    variantDto.PriceOverride ??
                    createDto.Price);

            variant.ChangeStock(
                Math.Max(
                    0,
                    variantDto.StockQuantity));

            // Legacy Color support
            AddVariantAttribute(
                product,
                variant,
                variantDto.Color,
                "Color",
                "color");

            // Legacy Size support
            AddVariantAttribute(
                product,
                variant,
                variantDto.Size,
                "Size",
                "size");

            // Generic catalog attributes
            await AddCatalogVariantAttributesAsync(
                product,
                variant,
                variantDto.AttributeValueIds,
                cancellationToken);
        }

        // A product always has a stock-bearing variant.
        if (product.Variants.Count == 0)
        {
            var defaultVariant =
                product.AddVariant(
                    $"{product.Sku}-DEFAULT",
                    product.Price);

            defaultVariant.ChangeStock(0);
        }

        await SynchronizeProductAttributesAsync(
            product,
            createDto.Attributes,
            cancellationToken);

        await ReplaceCategoriesAsync(
            product,
            createDto.CategoryIds,
            cancellationToken);

        // --------------------------------------------------------
        // Persist
        // --------------------------------------------------------

        await _productRepository.AddAsync(
            product,
            cancellationToken);

     

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        var created =
            await _productRepository.GetByIdAsync(
                product.Id,
                cancellationToken);

     

        return created is null
            ? _mapper.Map<ProductDto>(product)
            : _mapper.Map<ProductDto>(created);
    }
    // ============================================================
    // Update
    // ============================================================

    public async Task UpdateAsync(
     Guid id,
     UpdateProductDto updateDto,
     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        if (string.IsNullOrWhiteSpace(updateDto.Name))
        {
            throw new ArgumentException(
                "Product name is required.",
                nameof(updateDto.Name));
        }

        if (updateDto.Price < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updateDto.Price),
                "Product price cannot be negative.");
        }

        // ============================================================
        // Load current tracked aggregate
        // ============================================================

        var product =
            await GetExistingProductAsync(
                id,
                cancellationToken);

        var normalizedSlug =
            await CreateUniqueSlugAsync(
                updateDto.Name,
                cancellationToken,
                id);

        // ============================================================
        // Basic information
        // ============================================================

        product.Update(
            updateDto.Name.Trim(),
            normalizedSlug,
            updateDto.Description,
            updateDto.Price);

        product.SetShortDescription(
            updateDto.ShortDescription);

        product.SetCurrency(
            updateDto.Currency);

        // ============================================================
        // Pricing
        // ============================================================

        product.SetComparePrice(
            updateDto.ComparePrice);

        if (updateDto.DiscountPercentage.HasValue)
        {
            product.ApplyDiscount(
                updateDto.DiscountPercentage.Value);
        }
        else
        {
            product.RemoveDiscount();
        }

        // ============================================================
        // Status
        // ============================================================

        product.SetActive(
            updateDto.IsActive);

        product.SetFeatured(
            updateDto.IsFeatured);

        if (updateDto.IsPublished)
        {
            product.Publish();
        }
        else
        {
            product.Unpublish();
        }

        // ============================================================
        // Brand / Manufacturer
        // ============================================================

        product.SetBrand(
            updateDto.BrandId);

        product.SetManufacturer(
            updateDto.ManufacturerId);

        // ============================================================
        // Product Specifications
        // ============================================================

        await SynchronizeProductAttributesAsync(
            product,
            updateDto.Attributes,
            cancellationToken);

        // ============================================================
        // Categories
        // ============================================================

        await ReplaceCategoriesAsync(
            product,
            updateDto.CategoryIds,
            cancellationToken);

        // ============================================================
        // Variants
        // ============================================================

        await SynchronizeVariantsAsync(
            product,
            updateDto.Variants,
            cancellationToken);

        // ============================================================
        // Images
        // ============================================================

        await _productRepository.ReplaceProductImagesAsync(
            product.Id,
            updateDto.Images,
            cancellationToken);

        // ============================================================
        // Existing VariantAttributeValue rows are persisted directly
        // through repository SQL operations.
        // ============================================================

        _productRepository.DetachTrackedVariantAttributeMappings();

        // ============================================================
        // IMPORTANT
        //
        // ProductAttribute / AttributeValue can be newly-created
        // during this request. If some other EF graph operation
        // accidentally promoted them to Modified, repair their state
        // before SaveChanges().
        //
        // Existing rows:
        //     Modified with no actual change -> Unchanged
        //
        // New rows:
        //     Modified but not present in DB -> Added
        //
        // This prevents UPDATE ... WHERE Id = new-guid
        // and therefore prevents false concurrency exceptions.
        // ============================================================

        await _productRepository
            .RepairTrackedProductAttributeStatesAsync(
                cancellationToken);

        // ============================================================
        // Existing ProductVariant scalar changes are already persisted
        // with ExecuteUpdateAsync().
        //
        // New ProductVariants must remain Added.
        // ============================================================

        _productRepository.NormalizeTrackedProductVariantStates();

        // ============================================================
        // Final save
        // ============================================================

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    // ============================================================
    // Stock
    // ============================================================
    public async Task UpdateStockAsync(
        Guid id,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Stock quantity cannot be negative.");
        }

        var product =
            await GetExistingProductAsync(
                id,
                cancellationToken);

        if (product.Variants.Count == 0)
        {
            var variant =
                product.AddVariant(
                    $"{product.Sku}-DEFAULT",
                    product.Price);

            variant.ChangeStock(quantity);
        }
        else
        {
            product.Variants
                .First()
                .ChangeStock(quantity);
        }

        _productRepository.Update(
            product);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    // ============================================================
    // Active
    // ============================================================

    public async Task SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var product =
            await GetExistingProductAsync(
                id,
                cancellationToken);

        product.SetActive(isActive);

        _productRepository.Update(
            product);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    // ============================================================
    // Featured
    // ============================================================

    public async Task SetFeaturedAsync(
        Guid id,
        bool isFeatured,
        CancellationToken cancellationToken = default)
    {
        var product =
            await GetExistingProductAsync(
                id,
                cancellationToken);

        product.SetFeatured(
            isFeatured);

        _productRepository.Update(
            product);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    // ============================================================
    // Delete
    // ============================================================

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product =
            await GetExistingProductAsync(
                id,
                cancellationToken);

        _productRepository.Delete(
            product);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    // ============================================================
    // Categories
    // ============================================================
    private async Task<Product> SynchronizeProductAttributesAsync(
        Product product,
        IEnumerable<ProductAttributeInputDto>? requestedAttributes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        requestedAttributes ??=
            Enumerable.Empty<ProductAttributeInputDto>();

        var requests =
            requestedAttributes.ToList();

        var catalogAttributes =
            await _catalogAttributeRepository.GetAllAsync(
                cancellationToken);

        // ============================================================
        // Identify ProductAttributes currently used by variants.
        //
        // These attributes must not be removed while variants still
        // reference one of their values.
        // ============================================================

        var protectedVariantAttributeIds =
            product.Variants
                .SelectMany(
                    variant =>
                        variant.AttributeValues)
                .Where(
                    mapping =>
                        mapping.AttributeValue != null &&
                        mapping.AttributeValue.ProductAttribute != null &&
                        mapping.AttributeValue.ProductAttribute.Id != Guid.Empty)
                .Select(
                    mapping =>
                        mapping.AttributeValue!
                            .ProductAttribute
                            .Id)
                .ToHashSet();

        // ============================================================
        // Validate requested catalog attributes
        // ============================================================

        var resolvedRequests =
            new List<(
                CatalogAttribute Attribute,
                List<ProductAttributeValueInputDto> Values)>();

        var requestedCodes =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.CatalogAttributeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Catalog attribute is required.");
            }

            var catalogAttribute =
                catalogAttributes.FirstOrDefault(
                    attribute =>
                        attribute.Id ==
                        request.CatalogAttributeId);

            if (catalogAttribute is null)
            {
                throw new ArgumentException(
                    $"Catalog attribute '{request.CatalogAttributeId}' was not found.");
            }

            if (!catalogAttribute.IsActive)
            {
                throw new ArgumentException(
                    $"Catalog attribute '{catalogAttribute.Name}' is inactive.");
            }

            var attributeCode =
                catalogAttribute.Code.Trim();

            if (string.IsNullOrWhiteSpace(attributeCode))
            {
                throw new ArgumentException(
                    $"Catalog attribute '{catalogAttribute.Id}' has an empty code.");
            }

            var normalizedCode =
                attributeCode.ToLowerInvariant();

            if (!requestedCodes.Add(
                    normalizedCode))
            {
                throw new ArgumentException(
                    $"Catalog attribute '{catalogAttribute.Name}' was supplied more than once.");
            }

            resolvedRequests.Add(
                (
                    catalogAttribute,
                    request.Values ??
                        new List<ProductAttributeValueInputDto>()
                ));
        }

        // ============================================================
        // Existing ProductAttributes by Code
        //
        // This preserves ProductAttribute.Id.
        // ============================================================

        var existingAttributesByCode =
            product.Attributes
                .Where(
                    attribute =>
                        !string.IsNullOrWhiteSpace(
                            attribute.Code))
                .ToDictionary(
                    attribute =>
                        attribute.Code.Trim(),
                    attribute =>
                        attribute,
                    StringComparer.OrdinalIgnoreCase);

        // ============================================================
        // Synchronize requested attributes
        // ============================================================

        foreach (var request in resolvedRequests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var catalogAttribute =
                request.Attribute;

            var attributeName =
                catalogAttribute.Name.Trim();

            var attributeCode =
                catalogAttribute.Code.Trim();

            var normalizedCode =
                attributeCode.ToLowerInvariant();

            existingAttributesByCode.TryGetValue(
                normalizedCode,
                out var productAttribute);

            // --------------------------------------------------------
            // CREATE only when missing.
            // --------------------------------------------------------

            if (productAttribute is null)
            {
                productAttribute =
                    product.AddAttribute(
                        attributeName,
                        attributeCode);

                existingAttributesByCode[
                    normalizedCode] =
                    productAttribute;
            }
            else
            {
                // ----------------------------------------------------
                // Existing Id remains untouched.
                // ----------------------------------------------------

                if (!string.Equals(
                        productAttribute.Name,
                        attributeName,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        productAttribute.Code,
                        attributeCode,
                        StringComparison.Ordinal))
                {
                    productAttribute.Update(
                        attributeName,
                        attributeCode);
                }
            }

            // --------------------------------------------------------
            // Variant-owned attributes are managed by variant logic.
            // --------------------------------------------------------

            if (protectedVariantAttributeIds.Contains(
                    productAttribute.Id))
            {
                continue;
            }

            // ========================================================
            // Requested values
            // ========================================================

            var requestedValues =
                new Dictionary<
                    string,
                    (
                        string Value,
                        string? DisplayValue,
                        string? ColorHex,
                        int DisplayOrder)>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var requestedValue in request.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CatalogAttributeValue? catalogValue = null;

                if (requestedValue.CatalogAttributeValueId.HasValue &&
                    requestedValue.CatalogAttributeValueId.Value != Guid.Empty)
                {
                    catalogValue =
                        catalogAttribute.Values.FirstOrDefault(
                            value =>
                                value.Id ==
                                requestedValue.CatalogAttributeValueId.Value);

                    if (catalogValue is null)
                    {
                        throw new ArgumentException(
                            $"Catalog value '{requestedValue.CatalogAttributeValueId}' does not belong to '{catalogAttribute.Name}'.");
                    }

                    if (!catalogValue.IsActive)
                    {
                        throw new ArgumentException(
                            $"Catalog value '{catalogValue.Value}' is inactive.");
                    }
                }

                // ----------------------------------------------------
                // Value always comes from database catalog when an
                // id was supplied.
                // ----------------------------------------------------

                var value =
                    (
                        catalogValue?.Value ??
                        requestedValue.Value
                    )?.Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var displayValue =
                    (
                        catalogValue?.DisplayValue ??
                        requestedValue.DisplayValue
                    )?.Trim();

                var colorHex =
                    (
                        catalogValue?.ColorHex ??
                        requestedValue.ColorHex
                    )?.Trim();

                if (!requestedValues.ContainsKey(
                        value))
                {
                    requestedValues[value] =
                        (
                            value,
                            string.IsNullOrWhiteSpace(
                                displayValue)
                                ? null
                                : displayValue,
                            string.IsNullOrWhiteSpace(
                                colorHex)
                                ? null
                                : colorHex,
                            requestedValue.DisplayOrder
                        );
                }
            }

            // ========================================================
            // Existing AttributeValues by Value
            //
            // This preserves AttributeValue.Id.
            // ========================================================

            var existingValuesByValue =
                productAttribute.Values
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value.Value))
                    .ToDictionary(
                        value =>
                            value.Value.Trim(),
                        value =>
                            value,
                        StringComparer.OrdinalIgnoreCase);

            // ========================================================
            // Add / Update requested values
            // ========================================================

            foreach (var requestedValue in requestedValues.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedValue =
                    requestedValue.Value.Trim();

                existingValuesByValue.TryGetValue(
                    normalizedValue,
                    out var existingValue);

                if (existingValue is not null)
                {
                    // ------------------------------------------------
                    // Preserve AttributeValue.Id.
                    // ------------------------------------------------

                    if (!string.Equals(
                            existingValue.DisplayValue,
                            requestedValue.DisplayValue,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            existingValue.ColorHex,
                            requestedValue.ColorHex,
                            StringComparison.Ordinal))
                    {
                        existingValue.Update(
                            existingValue.Value,
                            requestedValue.DisplayValue,
                            requestedValue.ColorHex);
                    }

                    continue;
                }

                // ----------------------------------------------------
                // New AttributeValue
                // ----------------------------------------------------

                productAttribute.AddValue(
                    requestedValue.Value,
                    requestedValue.DisplayValue,
                    requestedValue.ColorHex);
            }

            // ========================================================
            // Remove values no longer requested
            // ========================================================

            foreach (var existingValue in
                     productAttribute.Values.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedExistingValue =
                    existingValue.Value.Trim();

                if (requestedValues.ContainsKey(
                        normalizedExistingValue))
                {
                    continue;
                }

                // ----------------------------------------------------
                // Do not remove a value used by any variant.
                // ----------------------------------------------------

                var isUsedByVariant =
                    product.Variants
                        .SelectMany(
                            variant =>
                                variant.AttributeValues)
                        .Any(
                            mapping =>
                                mapping.AttributeValueId ==
                                existingValue.Id);

                if (isUsedByVariant)
                {
                    continue;
                }

                productAttribute.Values.Remove(
                    existingValue);
            }
        }

        // ============================================================
        // Remove ProductAttributes no longer requested
        // ============================================================

        foreach (var existingAttribute in
                 product.Attributes.ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Variant-owned attribute must stay.
            if (protectedVariantAttributeIds.Contains(
                    existingAttribute.Id))
            {
                continue;
            }

            var existingCode =
                existingAttribute.Code
                    .Trim()
                    .ToLowerInvariant();

            if (requestedCodes.Contains(
                    existingCode))
            {
                continue;
            }

            product.Attributes.Remove(
                existingAttribute);
        }

        return product;
    }

    private async Task ReplaceCategoriesAsync(
        Product product,
        IEnumerable<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        var requestedIds =
            (categoryIds ??
             Enumerable.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToHashSet();

        var validIds =
            new HashSet<Guid>();

        foreach (var categoryId in requestedIds)
        {
            var category =
                await _categoryRepository.GetByIdAsync(
                    categoryId,
                    cancellationToken);

            if (category is not null)
                validIds.Add(category.Id);
        }

        foreach (var existing in
                 product.ProductCategories.ToList())
        {
            if (!validIds.Contains(
                    existing.CategoryId))
            {
                product.ProductCategories.Remove(
                    existing);
            }
        }

        var existingIds =
            product.ProductCategories
                .Select(x => x.CategoryId)
                .ToHashSet();

        foreach (var categoryId in validIds)
        {
            if (!existingIds.Contains(
                    categoryId))
            {
                product.ProductCategories.Add(
                    new ProductCategory(
                        product.Id,
                        categoryId));
            }
        }
    }

    // ============================================================
    // Existing Product
    // ============================================================

    private async Task<Product> GetExistingProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product =
            await _productRepository.GetByIdAsync(
                id,
                cancellationToken);

        return product ??
               throw new KeyNotFoundException(
                   $"Product with id {id} was not found.");
    }

    // ============================================================
    // Legacy Variant Attributes
    // ============================================================

    private static void AddVariantAttribute(
        Product product,
        ProductVariant variant,
        string? value,
        string name,
        string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var normalizedValue =
            value.Trim();

        var attribute =
            product.Attributes.FirstOrDefault(
                x => string.Equals(
                    x.Code,
                    code,
                    StringComparison.OrdinalIgnoreCase));

        attribute ??=
            product.AddAttribute(
                name,
                code);

        var attributeValue =
            attribute.Values.FirstOrDefault(
                x => string.Equals(
                    x.Value,
                    normalizedValue,
                    StringComparison.OrdinalIgnoreCase));

        attributeValue ??=
            attribute.AddValue(
                normalizedValue,
                normalizedValue);

        variant.AddAttributeValue(
            attributeValue);
    }

    // ============================================================
    // Generic Catalog Variant Attributes
    // ============================================================

    private async Task AddCatalogVariantAttributesAsync(
        Product product,
        ProductVariant variant,
        IEnumerable<Guid>? attributeValueIds,
        CancellationToken cancellationToken)
    {
        var requestedIds =
            (attributeValueIds ??
             Enumerable.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToHashSet();

        if (requestedIds.Count == 0)
            return;

        // Load the catalog attributes and their values once.
        var catalogAttributes =
            await _catalogAttributeRepository.GetAllAsync(
                cancellationToken);

        foreach (var attributeValueId in requestedIds)
        {
            var catalogAttribute =
                catalogAttributes.FirstOrDefault(
                    attribute => attribute.Values.Any(
                        value => value.Id == attributeValueId));

            if (catalogAttribute is null)
            {
                throw new ArgumentException(
                    $"Catalog attribute for value '{attributeValueId}' was not found.",
                    nameof(attributeValueIds));
            }

            if (!catalogAttribute.IsActive)
            {
                throw new ArgumentException(
                    $"Catalog attribute '{catalogAttribute.Name}' is inactive.",
                    nameof(attributeValueIds));
            }

            if (!catalogAttribute.IsVariantAttribute)
            {
                throw new ArgumentException(
                    $"Catalog attribute '{catalogAttribute.Name}' is not configured as a variant attribute.",
                    nameof(attributeValueIds));
            }

            var catalogValue =
                catalogAttribute.Values.FirstOrDefault(
                    value => value.Id == attributeValueId);

            if (catalogValue is null)
            {
                throw new ArgumentException(
                    $"Catalog attribute value '{attributeValueId}' was not found.",
                    nameof(attributeValueIds));
            }

            if (!catalogValue.IsActive)
            {
                throw new ArgumentException(
                    $"Catalog attribute value '{catalogValue.Value}' is inactive.",
                    nameof(attributeValueIds));
            }

            // Reuse the product attribute if it already exists.
            var productAttribute =
                product.Attributes.FirstOrDefault(
                    attribute => string.Equals(
                        attribute.Code,
                        catalogAttribute.Code,
                        StringComparison.OrdinalIgnoreCase));

            productAttribute ??=
                product.AddAttribute(
                    catalogAttribute.Name,
                    catalogAttribute.Code);

            // Reuse the product attribute value if it already exists.
            var productAttributeValue =
                productAttribute.Values.FirstOrDefault(
                    value => string.Equals(
                        value.Value,
                        catalogValue.Value,
                        StringComparison.OrdinalIgnoreCase));

            productAttributeValue ??=
                productAttribute.AddValue(
                    catalogValue.Value,
                    catalogValue.DisplayValue,
                    catalogValue.ColorHex);

            variant.AddAttributeValue(
                productAttributeValue);
        }
    }

    // ============================================================
    // Unique Slug
    // ============================================================

    private async Task<string> CreateUniqueSlugAsync(
        string name,
        CancellationToken cancellationToken,
        Guid? excludeId = null)
    {
        var baseSlug =
            GenerateSlug(name);

        if (!await _productRepository.ExistsBySlugAsync(
                baseSlug,
                excludeId,
                cancellationToken))
        {
            return baseSlug;
        }

        for (var suffix = 2;
             suffix <= 1000;
             suffix++)
        {
            var candidate =
                $"{baseSlug}-{suffix}";

            if (!await _productRepository.ExistsBySlugAsync(
                    candidate,
                    excludeId,
                    cancellationToken))
            {
                return candidate;
            }
        }

        return
            $"{baseSlug}-{Guid.NewGuid():N}";
    }

    // ============================================================
    // SKU
    // ============================================================

    private static string GenerateSku()
    {
        return $"NX-{Guid.NewGuid():N}";
    }

    // ============================================================
    // Slug
    // ============================================================

    private static string GenerateSlug(
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var slug =
            Regex.Replace(
                name.Trim().ToLowerInvariant(),
                @"[^\p{L}\p{Nd}]+",
                "-");

        slug =
            Regex.Replace(
                slug,
                "-+",
                "-");

        return slug.Trim('-');
    }
private async Task SynchronizeVariantsAsync(
    Product product,
    IEnumerable<UpdateProductVariantDto>? variantDtos,
    CancellationToken cancellationToken)
    {
        var requested =
            (variantDtos ??
             Enumerable.Empty<UpdateProductVariantDto>())
            .ToList();

        // ------------------------------------------------------------
        // No variant list:
        //
        // Keep backward-compatible behavior and do not touch the
        // existing variants.
        // ------------------------------------------------------------

        if (requested.Count == 0)
        {
            return;
        }

        // ------------------------------------------------------------
        // IMPORTANT:
        //
        // Snapshot ONLY variants that existed before this operation.
        //
        // New ProductVariant objects receive a Guid immediately,
        // but they do not exist in the database until SaveChanges().
        //
        // We must never treat those new objects as removed variants.
        // ------------------------------------------------------------

        var existingVariantIds =
            product.Variants
                .Select(x => x.Id)
                .Where(x => x != Guid.Empty)
                .ToHashSet();

        // ============================================================
        // 1. Validate duplicate Variant IDs
        // ============================================================

        var duplicateIds =
            requested
                .Where(
                    x =>
                        x.Id.HasValue &&
                        x.Id.Value != Guid.Empty)
                .GroupBy(
                    x =>
                        x.Id!.Value)
                .Where(
                    x =>
                        x.Count() > 1)
                .Select(
                    x =>
                        x.Key)
                .ToList();

        if (duplicateIds.Count > 0)
        {
            throw new ArgumentException(
                "The same variant cannot appear more than once.");
        }

        // ============================================================
        // 2. Normalize and validate SKUs
        // ============================================================

        var normalizedSkus =
            requested
                .Select(
                    x =>
                        x.Sku?.Trim() ??
                        string.Empty)
                .ToList();

        if (normalizedSkus.Any(
                string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Every variant SKU is required.");
        }

        var duplicateSkus =
            normalizedSkus
                .GroupBy(
                    x =>
                        x,
                    StringComparer.OrdinalIgnoreCase)
                .Where(
                    x =>
                        x.Count() > 1)
                .Select(
                    x =>
                        x.Key)
                .ToList();

        if (duplicateSkus.Count > 0)
        {
            throw new ArgumentException(
                $"Duplicate variant SKU detected: {string.Join(", ", duplicateSkus)}.");
        }

        // ============================================================
        // 3. Load Catalog Attributes once
        // ============================================================

        var catalogAttributes =
            await _catalogAttributeRepository.GetAllAsync(
                cancellationToken);

        // ============================================================
        // 4. Validate requested Variant combinations BEFORE
        //    converting Catalog Values into Product Attribute Values.
        //
        //    The frontend sends CatalogAttributeValue IDs.
        //    Therefore this is the most reliable place to detect
        //    duplicate combinations.
        // ============================================================

        var requestedCombinationSignatures =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var dto in requested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var requestedValueIds =
                (dto.AttributeValueIds ??
                 Enumerable.Empty<Guid>())
                .Where(
                    x =>
                        x != Guid.Empty)
                .Distinct()
                .ToList();

            // A variant without generic attribute values is allowed
            // here. Required-attribute validation is handled by the
            // existing frontend/backend combination validation later.
            if (requestedValueIds.Count == 0)
            {
                continue;
            }

            var usedCatalogAttributeIds =
                new HashSet<Guid>();

            var signatureParts =
                new List<string>();

            foreach (var valueId in requestedValueIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var catalogAttribute =
                    catalogAttributes.FirstOrDefault(
                        attribute =>
                            attribute.Values.Any(
                                value =>
                                    value.Id ==
                                    valueId));

                if (catalogAttribute is null)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{valueId}' was not found.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogAttribute.IsActive)
                {
                    throw new ArgumentException(
                        $"Catalog attribute '{catalogAttribute.Name}' is inactive.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogAttribute.IsVariantAttribute)
                {
                    throw new ArgumentException(
                        $"Catalog attribute '{catalogAttribute.Name}' is not configured as a variant attribute.",
                        nameof(dto.AttributeValueIds));
                }

                // One Variant cannot contain two values belonging to
                // the same Catalog Attribute.
                if (!usedCatalogAttributeIds.Add(
                        catalogAttribute.Id))
                {
                    throw new ArgumentException(
                        $"Variant '{dto.Sku}' contains more than one value for attribute '{catalogAttribute.Name}'.");
                }

                var catalogValue =
                    catalogAttribute.Values.FirstOrDefault(
                        value =>
                            value.Id ==
                            valueId);

                if (catalogValue is null)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{valueId}' was not found.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogValue.IsActive)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{catalogValue.Value}' is inactive.",
                        nameof(dto.AttributeValueIds));
                }

                var attributeCode =
                    catalogAttribute.Code?
                        .Trim()
                        .ToLowerInvariant() ??
                    string.Empty;

                signatureParts.Add(
                    $"{attributeCode}:{catalogValue.Id}");
            }

            var signature =
                string.Join(
                    "|",
                    signatureParts
                        .OrderBy(
                            x =>
                                x,
                            StringComparer.OrdinalIgnoreCase));

            if (!requestedCombinationSignatures.Add(
                    signature))
            {
                throw new ArgumentException(
                    $"Duplicate variant attribute combination detected for SKU '{dto.Sku}'.");
            }
        }

        // ============================================================
        // 5. Track existing variants explicitly matched by request
        // ============================================================

        var matchedExisting =
            new HashSet<Guid>();

        // ============================================================
        // 6. Synchronize requested variants
        // ============================================================

        foreach (var dto in requested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sku =
                dto.Sku?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(sku))
            {
                throw new ArgumentException(
                    "Every variant SKU is required.");
            }

            // ========================================================
            // EXISTING VARIANT
            // ========================================================

            if (dto.Id.HasValue &&
                dto.Id.Value != Guid.Empty)
            {
                var variantId =
                    dto.Id.Value;

                var variant =
                    product.Variants.FirstOrDefault(
                        x =>
                            x.Id ==
                            variantId);

                if (variant is null)
                {
                    throw new KeyNotFoundException(
                        $"Product variant '{variantId}' was not found.");
                }

                if (variant.ProductId != product.Id)
                {
                    throw new InvalidOperationException(
                        "The specified variant does not belong to this product.");
                }

                // ----------------------------------------------------
                // SKU uniqueness
                // ----------------------------------------------------

                if (await _productRepository.ExistsByVariantSkuAsync(
                        sku,
                        variant.Id,
                        cancellationToken))
                {
                    throw new ArgumentException(
                        $"Variant SKU '{sku}' already exists.");
                }

                // ----------------------------------------------------
                // Update scalar values in domain
                // ----------------------------------------------------

                variant.ChangeSku(
                    sku);

                variant.ChangePrice(
                    dto.PriceOverride ??
                    product.Price);

                variant.SetComparePrice(
                    dto.ComparePrice);

                variant.SetActive(
                    dto.IsActive);

                // ----------------------------------------------------
                // Persist scalar values directly
                // ----------------------------------------------------

                var variantUpdated =
                    await _productRepository.UpdateVariantAsync(
                        variant.Id,
                        variant.Sku,
                        variant.PriceOverride,
                        variant.ComparePrice,
                        variant.IsActive,
                        cancellationToken);

                if (!variantUpdated)
                {
                    throw new KeyNotFoundException(
                        $"Product variant '{variant.Id}' no longer exists.");
                }

                // ----------------------------------------------------
                // Replace attribute mappings
                // ----------------------------------------------------

                await ReplaceVariantAttributesAsync(
                    product,
                    variant,
                    dto,
                    persistMappingsExplicitly: true,
                    cancellationToken);

                matchedExisting.Add(
                    variant.Id);

                // Existing inventory is managed elsewhere.
                continue;
            }

            // ========================================================
            // NEW VARIANT
            // ========================================================

            if (await _productRepository.ExistsByVariantSkuAsync(
                    sku,
                    cancellationToken: cancellationToken))
            {
                throw new ArgumentException(
                    $"Variant SKU '{sku}' already exists.");
            }

            var newVariant =
                product.AddVariant(
                    sku,
                    dto.PriceOverride ??
                    product.Price,
                    dto.ComparePrice);

            // New variants start with no catalog-managed stock.
            // Inventory remains the source of truth.
            newVariant.SetActive(
                dto.IsActive);

            // --------------------------------------------------------
            // Create generic attribute mappings
            // --------------------------------------------------------

            await ReplaceVariantAttributesAsync(
                product,
                newVariant,
                dto,
                persistMappingsExplicitly: false,
                cancellationToken);
        }

        // ============================================================
        // 7. Deactivate omitted EXISTING variants
        // ============================================================
        //
        // IMPORTANT:
        //
        // We iterate only over IDs captured BEFORE new variants were
        // added to product.Variants.
        //
        // Therefore a new in-memory variant can never be sent to
        // UpdateVariantAsync() before it has been inserted into DB.
        // ============================================================

        foreach (var existingVariant in
                 product.Variants
                     .Where(
                         x =>
                             existingVariantIds.Contains(
                                 x.Id))
                     .ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // This variant is still part of the requested set.
            if (matchedExisting.Contains(
                    existingVariant.Id))
            {
                continue;
            }

            // Variant disappeared from the request:
            // deactivate instead of hard-delete.
            existingVariant.Deactivate();

            var variantUpdated =
                await _productRepository.UpdateVariantAsync(
                    existingVariant.Id,
                    existingVariant.Sku,
                    existingVariant.PriceOverride,
                    existingVariant.ComparePrice,
                    false,
                    cancellationToken);

            if (!variantUpdated)
            {
                throw new KeyNotFoundException(
                    $"Product variant '{existingVariant.Id}' no longer exists.");
            }
        }

        // ============================================================
        // 8. Final validation
        // ============================================================

        var activeVariants =
      product.Variants
          .Where(x => x.IsActive)
          .ToList();

        if (activeVariants.Count == 0)
        {
            throw new ArgumentException(
                "A product must have at least one active variant.");
        }

        ValidateVariantAttributeCombinations(
            product,
            activeVariants);
    }


    private async Task ReplaceVariantAttributesAsync(
  Product product,
  ProductVariant variant,
  UpdateProductVariantDto dto,
  bool persistMappingsExplicitly,
  CancellationToken cancellationToken)
    {
// ------------------------------------------------------------
// New variants belong to the current Product aggregate and
// have not been persisted yet.
// ------------------------------------------------------------


if (!persistMappingsExplicitly)
        {
            variant.AttributeValues.Clear();
        }

        var selectedValues =
            new List<
                NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes.AttributeValue>();

        // ============================================================
        // Legacy Color
        // ============================================================

        AddVariantAttribute(
            product,
            variant,
            dto.Color,
            "Color",
            "color");

        if (!string.IsNullOrWhiteSpace(dto.Color))
        {
            var colorAttribute =
                product.Attributes.FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Code,
                            "color",
                            StringComparison.OrdinalIgnoreCase));

            var colorValue =
                colorAttribute?.Values.FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Value,
                            dto.Color.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (colorValue is not null)
            {
                selectedValues.Add(
                    colorValue);
            }
        }

        // ============================================================
        // Legacy Size
        // ============================================================

        AddVariantAttribute(
            product,
            variant,
            dto.Size,
            "Size",
            "size");

        if (!string.IsNullOrWhiteSpace(dto.Size))
        {
            var sizeAttribute =
                product.Attributes.FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Code,
                            "size",
                            StringComparison.OrdinalIgnoreCase));

            var sizeValue =
                sizeAttribute?.Values.FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Value,
                            dto.Size.Trim(),
                            StringComparison.OrdinalIgnoreCase));

            if (sizeValue is not null)
            {
                selectedValues.Add(
                    sizeValue);
            }
        }

        // ============================================================
        // Generic Catalog Attributes
        // ============================================================

        var requestedIds =
            (dto.AttributeValueIds ??
             Enumerable.Empty<Guid>())
            .Where(
                x =>
                    x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (requestedIds.Length > 0)
        {
            var catalogAttributes =
                await _catalogAttributeRepository.GetAllAsync(
                    cancellationToken);

            var selectedAttributeIds =
                new HashSet<Guid>();

            foreach (var valueId in requestedIds)
            {
                var catalogAttribute =
                    catalogAttributes.FirstOrDefault(
                        attribute =>
                            attribute.Values.Any(
                                value =>
                                    value.Id ==
                                    valueId));

                if (catalogAttribute is null)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{valueId}' was not found.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogAttribute.IsActive)
                {
                    throw new ArgumentException(
                        $"Catalog attribute '{catalogAttribute.Name}' is inactive.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogAttribute.IsVariantAttribute)
                {
                    throw new ArgumentException(
                        $"Catalog attribute '{catalogAttribute.Name}' is not configured as a variant attribute.",
                        nameof(dto.AttributeValueIds));
                }

                if (!selectedAttributeIds.Add(
                        catalogAttribute.Id))
                {
                    throw new ArgumentException(
                        $"Variant '{variant.Sku}' contains more than one value for attribute '{catalogAttribute.Name}'.");
                }

                var catalogValue =
                    catalogAttribute.Values.FirstOrDefault(
                        value =>
                            value.Id ==
                            valueId);

                if (catalogValue is null)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{valueId}' was not found.",
                        nameof(dto.AttributeValueIds));
                }

                if (!catalogValue.IsActive)
                {
                    throw new ArgumentException(
                        $"Catalog attribute value '{catalogValue.Value}' is inactive.",
                        nameof(dto.AttributeValueIds));
                }

                // ----------------------------------------------------
                // Product attribute
                // ----------------------------------------------------

                var productAttribute =
                    product.Attributes.FirstOrDefault(
                        attribute =>
                            string.Equals(
                                attribute.Code,
                                catalogAttribute.Code,
                                StringComparison.OrdinalIgnoreCase));

                productAttribute ??=
                    product.AddAttribute(
                        catalogAttribute.Name,
                        catalogAttribute.Code);

                // ----------------------------------------------------
                // Product attribute value
                // ----------------------------------------------------

                var productAttributeValue =
                    productAttribute.Values.FirstOrDefault(
                        value =>
                            string.Equals(
                                value.Value,
                                catalogValue.Value,
                                StringComparison.OrdinalIgnoreCase));

                productAttributeValue ??=
                    productAttribute.AddValue(
                        catalogValue.Value,
                        catalogValue.DisplayValue,
                        catalogValue.ColorHex);

                selectedValues.Add(
                    productAttributeValue);

                // ----------------------------------------------------
                // For new variants EF tracks the relationship directly.
                // ----------------------------------------------------

                if (!persistMappingsExplicitly)
                {
                    variant.AddAttributeValue(
                        productAttributeValue);
                }
            }
        }

        // ============================================================
        // Persist mappings for an existing variant
        // ============================================================

        if (persistMappingsExplicitly)
        {
            var desiredIds =
                selectedValues
                    .Select(
                        value =>
                            value.Id)
                    .Distinct()
                    .ToHashSet();

            var currentIds =
                variant.AttributeValues
                    .Select(
                        mapping =>
                            mapping.AttributeValueId)
                    .Distinct()
                    .ToHashSet();

            // --------------------------------------------------------
            // Delete obsolete mappings directly from the database.
            // --------------------------------------------------------

            var idsToDelete =
                currentIds
                    .Except(desiredIds)
                    .ToArray();

            if (idsToDelete.Length > 0)
            {
                await _productRepository
                    .DeleteVariantAttributeMappingsAsync(
                        variant.Id,
                        idsToDelete,
                        cancellationToken);

                foreach (var attributeValueId in idsToDelete)
                {
                    variant.RemoveAttributeValue(
                        attributeValueId);
                }
            }

            // --------------------------------------------------------
            // Add new mappings through the aggregate.
            //
            // This keeps ProductVariant.AttributeValues synchronized
            // with what EF is going to insert.
            // --------------------------------------------------------

            foreach (var attributeValueId in
                     desiredIds.Except(currentIds))
            {
                var attributeValue =
                    selectedValues.First(
                        value =>
                            value.Id ==
                            attributeValueId);

                variant.AddAttributeValue(
                    attributeValue);
            }
        }


    }


private static void ValidateVariantAttributeCombinations(
    Product product,
    IEnumerable<ProductVariant> variants)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(variants);

        var activeVariants =
            variants
                .Where(
                    variant =>
                        variant.IsActive)
                .ToList();

        if (activeVariants.Count == 0)
        {
            throw new ArgumentException(
                "A product must have at least one active variant.");
        }

        // ============================================================
        // Resolve Product AttributeValue information by ID.
        //
        // We intentionally do NOT depend on
        // VariantAttributeValue.AttributeValue navigation because new
        // mappings created during the current request may not have
        // that navigation populated yet.
        // ============================================================

        var attributeValueLookup =
            product.Attributes
                .SelectMany(
                    attribute =>
                        (
                            attribute.Values ??
                            Enumerable.Empty<
                                NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes.AttributeValue>()
                        )
                        .Select(
                            value =>
                                new
                                {
                                    ValueId =
                                        value.Id,

                                    AttributeCode =
                                        attribute.Code?
                                            .Trim()
                                            .ToLowerInvariant()
                                        ?? string.Empty,

                                    AttributeName =
                                        attribute.Name?
                                            .Trim()
                                        ?? string.Empty,
                                }))
                .Where(
                    item =>
                        item.ValueId != Guid.Empty &&
                        !string.IsNullOrWhiteSpace(
                            item.AttributeCode))
                .ToDictionary(
                    item =>
                        item.ValueId);

        // ============================================================
        // Every active Variant must use the same set of attributes.
        // ============================================================

        HashSet<string>? expectedAttributeCodes =
            null;

        var signatures =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var variant in activeVariants)
        {
            // --------------------------------------------------------
            // Resolve mappings using AttributeValueId.
            // --------------------------------------------------------

            var resolvedMappings =
                variant.AttributeValues
                    .Select(
                        mapping =>
                        {
                            if (!attributeValueLookup.TryGetValue(
                                    mapping.AttributeValueId,
                                    out var attributeInfo))
                            {
                                return null;
                            }

                            return new
                            {
                                AttributeValueId =
                                    mapping.AttributeValueId,

                                AttributeCode =
                                    attributeInfo.AttributeCode,

                                AttributeName =
                                    attributeInfo.AttributeName,
                            };
                        })
                    .Where(
                        item =>
                            item != null)
                    .ToList();

            // --------------------------------------------------------
            // Do not silently turn an invalid mapping into an empty
            // combination.
            // --------------------------------------------------------

            if (
                resolvedMappings.Count !=
                variant.AttributeValues.Count)
            {
                throw new InvalidOperationException(
                    $"Variant '{variant.Sku}' contains an invalid attribute value mapping.");
            }

            // --------------------------------------------------------
            // One Variant cannot contain two values from the same
            // attribute dimension.
            // --------------------------------------------------------

            var duplicateAttributeCodes =
                resolvedMappings
                    .GroupBy(
                        mapping =>
                            mapping.AttributeCode,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(
                        group =>
                            group.Count() > 1)
                    .Select(
                        group =>
                            group.Key)
                    .ToList();

            if (duplicateAttributeCodes.Count > 0)
            {
                throw new ArgumentException(
                    $"Variant '{variant.Sku}' contains more than one value for the same variant attribute.");
            }

            // --------------------------------------------------------
            // Determine this Variant's attribute dimensions.
            // --------------------------------------------------------

            var attributeCodes =
                resolvedMappings
                    .Select(
                        mapping =>
                            mapping.AttributeCode)
                    .ToHashSet(
                        StringComparer.OrdinalIgnoreCase);

            if (expectedAttributeCodes is null)
            {
                expectedAttributeCodes =
                    attributeCodes;
            }
            else if (
                !expectedAttributeCodes.SetEquals(
                    attributeCodes))
            {
                throw new ArgumentException(
                    $"All active variants of a product must use the same variant attributes. Variant '{variant.Sku}' has a different attribute set.");
            }

            // --------------------------------------------------------
            // Build a deterministic combination signature.
            //
            // Example:
            //
            // color:GUID1|size:GUID2
            //
            // Two Variants with different values therefore receive
            // different signatures.
            // --------------------------------------------------------

            var signature =
                string.Join(
                    "|",
                    resolvedMappings
                        .Select(
                            mapping =>
                                $"{mapping.AttributeCode}:{mapping.AttributeValueId}")
                        .OrderBy(
                            value =>
                                value,
                            StringComparer.OrdinalIgnoreCase));

            if (!signatures.Add(
                    signature))
            {
                throw new ArgumentException(
                    $"Duplicate variant attribute combination detected for SKU '{variant.Sku}'.");
            }
        }
    }






}
