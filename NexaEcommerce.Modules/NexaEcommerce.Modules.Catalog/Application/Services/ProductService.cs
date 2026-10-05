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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IProductStockReader _productStockReader;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICatalogAttributeRepository _catalogAttributeRepository;


    public ProductService(
     IProductRepository productRepository,
     IUnitOfWork unitOfWork,
     IMapper mapper,
     IProductStockReader productStockReader)
    {
        _productRepository =
            productRepository;

        _unitOfWork =
            unitOfWork;

        _mapper =
            mapper;

        _productStockReader =
            productStockReader;
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
    private async Task SynchronizeNewProductVariantsAsync(
      Product product,
      IEnumerable<CreateProductVariantDto>? variantDtos,
      CancellationToken cancellationToken)
    {
        var requestedVariants =
            (variantDtos ??
             Enumerable.Empty<CreateProductVariantDto>())
            .ToList();

        // --------------------------------------------------------
        // Simple product:
        // create one default sellable variant automatically.
        // --------------------------------------------------------

        if (requestedVariants.Count == 0)
        {
            var hasVariantDefiningAttributes =
                product.Attributes.Any(
                    x =>
                        !x.IsDeleted &&
                        x.Role.HasFlag(
                            AttributeRole.VariantDefining));

            if (!hasVariantDefiningAttributes)
            {
                var defaultSku =
                    product.Sku;

                if (await _productRepository.ExistsByVariantSkuAsync(
                        defaultSku,
                        cancellationToken: cancellationToken))
                {
                    throw new ArgumentException(
                        $"Variant SKU '{defaultSku}' already exists.");
                }

                product.AddVariant(
      product.Sku,
      product.Price,
      0,
      product.ComparePrice);

                return;
            }

            // Variant product without variants is invalid.
            // ValidateVariantDefinitions will also protect this,
            // but fail earlier with a clearer message.
            throw new ArgumentException(
                "A product with variant-defining attributes must contain at least one variant.");
        }

        var usedSkus =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var variantDto in requestedVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sku =
                variantDto.Sku?.Trim();

            if (string.IsNullOrWhiteSpace(sku))
            {
                throw new ArgumentException(
                    "Variant SKU is required.");
            }

            if (!usedSkus.Add(sku))
            {
                throw new ArgumentException(
                    $"Duplicate variant SKU '{sku}' detected.");
            }

            if (await _productRepository.ExistsByVariantSkuAsync(
                    sku,
                    cancellationToken: cancellationToken))
            {
                throw new ArgumentException(
                    $"Variant SKU '{sku}' already exists.");
            }

            var attributeValues =
                await ResolveVariantAttributeValuesAsync(
                    product,
                    variantDto.AttributeValueIds,
                    cancellationToken);

            var combinationKey =
                BuildCombinationKey(
                    attributeValues);

            var duplicateCombination =
                product.Variants.Any(
                    existing =>
                        string.Equals(
                            existing.CombinationKey,
                            combinationKey,
                            StringComparison.OrdinalIgnoreCase));

            if (duplicateCombination)
            {
                throw new ArgumentException(
                    $"Duplicate variant combination detected for SKU '{sku}'.");
            }

            var variant =
     product.AddVariant(
         sku,
         variantDto.PriceOverride ??
         product.Price,
         0,
         variantDto.ComparePrice);

            variant.SetBarcode(
                variantDto.Barcode);

            variant.SetCombinationKey(
                combinationKey);

            foreach (var attributeValue in attributeValues)
            {
                variant.AddAttributeValue(
                    attributeValue);
            }

            foreach (var imageUrl in
                     variantDto.Images ??
                     new List<string>())
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var displayOrder =
                    variant.Images.Count;

                var image =
                    new ProductVariantImage(
                        variant.Id,
                        imageUrl.Trim(),
                        null,
                        displayOrder,
                        displayOrder == 0);

                variant.Images.Add(image);
            }
        }
    }
    private static void ValidateVariantDefinitions(
     Product product)
    {
        var variantAttributes =
            product.Attributes
                .Where(
                    attribute =>
                        !attribute.IsDeleted &&
                        attribute.Role.HasFlag(
                            AttributeRole.VariantDefining))
                .ToList();

        var activeVariants =
            product.Variants
                .Where(
                    variant =>
                        !variant.IsDeleted &&
                        variant.IsActive)
                .ToList();

        /*
         * Simple product:
         * no VariantDefining attributes means that only one active
         * variant is allowed.
         *
         * The default variant may have no attribute mappings.
         */
        if (variantAttributes.Count == 0)
        {
            if (activeVariants.Count > 1)
            {
                throw new ArgumentException(
                    "A product without variant-defining attributes can have only one active variant.");
            }

            foreach (var variant in activeVariants)
            {
                var mappingsCount =
                    variant.AttributeValues
                        .Count(
                            mapping =>
                                !mapping.IsDeleted &&
                                mapping.AttributeValue != null);

                if (mappingsCount > 0)
                {
                    throw new ArgumentException(
                        $"Simple product variant '{variant.Sku}' cannot contain variant attribute mappings.");
                }

                if (!string.IsNullOrWhiteSpace(
                        variant.CombinationKey))
                {
                    variant.SetCombinationKey(
                        string.Empty);
                }
            }

            return;
        }

        /*
         * A product with VariantDefining attributes must have
         * at least one active variant.
         */
        if (activeVariants.Count == 0)
        {
            throw new ArgumentException(
                "A product with variant-defining attributes must have at least one active variant.");
        }

        var expectedAttributeIds =
            variantAttributes
                .Select(
                    attribute =>
                        attribute.Id)
                .ToHashSet();

        var signatures =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var variant in activeVariants)
        {
            var mappings =
                variant.AttributeValues
                    .Where(
                        mapping =>
                            !mapping.IsDeleted &&
                            mapping.AttributeValue != null)
                    .ToList();

            /*
             * Every active variant must contain exactly one value
             * for every VariantDefining attribute.
             */
            var attributeGroups =
                mappings
                    .GroupBy(
                        mapping =>
                            mapping.AttributeValue!
                                .ProductAttributeId)
                    .ToList();

            /*
             * Missing or unexpected attributes.
             */
            var resolvedAttributeIds =
                attributeGroups
                    .Select(
                        group =>
                            group.Key)
                    .ToHashSet();

            if (!expectedAttributeIds.SetEquals(
                    resolvedAttributeIds))
            {
                throw new ArgumentException(
                    $"Variant '{variant.Sku}' does not provide exactly one value for every VariantDefining attribute.");
            }

            /*
             * More than one value for the same attribute
             * is invalid.
             *
             * Example:
             * Color = Red
             * Color = Blue
             */
            foreach (var group in attributeGroups)
            {
                if (group.Count() != 1)
                {
                    var attribute =
                        variantAttributes
                            .FirstOrDefault(
                                x =>
                                    x.Id == group.Key);

                    throw new ArgumentException(
                        $"Variant '{variant.Sku}' contains more than one value for attribute '{attribute?.Name ?? group.Key.ToString()}'.");
                }
            }

            /*
             * Ensure every mapping points to a ProductAttribute
             * that is actually VariantDefining.
             */
            foreach (var mapping in mappings)
            {
                var attribute =
                    mapping.AttributeValue!
                        .ProductAttribute;

                if (attribute is null)
                {
                    throw new ArgumentException(
                        $"Variant '{variant.Sku}' contains an attribute value without a ProductAttribute.");
                }

                if (!attribute.Role.HasFlag(
                        AttributeRole.VariantDefining))
                {
                    throw new ArgumentException(
                        $"Variant '{variant.Sku}' references an attribute '{attribute.Code}' that is not VariantDefining.");
                }

                if (!expectedAttributeIds.Contains(
                        attribute.Id))
                {
                    throw new ArgumentException(
                        $"Variant '{variant.Sku}' references an attribute that does not belong to the product's VariantDefining attributes.");
                }
            }

            /*
             * Build deterministic combination key.
             */
            var key =
                BuildCombinationKey(
                    mappings.Select(
                        mapping =>
                            mapping.AttributeValue!));

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    $"Variant '{variant.Sku}' has an empty combination key.");
            }

            /*
             * Two active variants cannot have the same combination.
             */
            if (!signatures.Add(key))
            {
                throw new ArgumentException(
                    $"Duplicate variant combination detected for SKU '{variant.Sku}'.");
            }

            /*
             * Keep the persisted CombinationKey synchronized
             * with the actual attribute mappings.
             */
            if (!string.Equals(
                    variant.CombinationKey,
                    key,
                    StringComparison.OrdinalIgnoreCase))
            {
                variant.SetCombinationKey(
                    key);
            }
        }
    }
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
            {
                continue;
            }

            var displayOrder =
                product.Images.Count;

            product.AddImage(
                imageUrl.Trim(),
                displayOrder,
                displayOrder == 0);
        }

        // --------------------------------------------------------
        // Product Attributes
        // IMPORTANT:
        // Attributes MUST be created before Variants because
        // Variants reference Product Attribute Values.
        // --------------------------------------------------------

        await SynchronizeProductAttributesAsync(
            product,
            createDto.Attributes,
            cancellationToken);

        // --------------------------------------------------------
        // Variants
        // --------------------------------------------------------

        await SynchronizeNewProductVariantsAsync(
            product,
            createDto.Variants,
            cancellationToken);

        // --------------------------------------------------------
        // Categories
        // --------------------------------------------------------

        await ReplaceCategoriesAsync(
            product,
            createDto.CategoryIds,
            cancellationToken);

        // --------------------------------------------------------
        // Validation
        // --------------------------------------------------------

        ValidateRequiredProductAttributes(
            product);

        ValidateVariantDefinitions(
            product);

        // --------------------------------------------------------
        // Persist
        // --------------------------------------------------------

        await _productRepository.AddAsync(
            product,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        // Re-load complete aggregate for DTO mapping.
        var created =
            await _productRepository.GetByIdAsync(
                product.Id,
                cancellationToken);
        var result =
            created is null
                ? _mapper.Map<ProductDto>(product)
                : _mapper.Map<ProductDto>(created);

        await EnrichStockQuantitiesAsync(
            result,
            cancellationToken);

        return result;
    }
    // ============================================================
    // Update
    // ============================================================
    private static string BuildCombinationKey(
        IEnumerable<AttributeValue> attributeValues)
    {
        var parts =
            attributeValues
                .Select(
                    value =>
                    {
                        var attribute =
                            value.ProductAttribute;

                        var code =
                            attribute?.Code?.Trim()
                            ?? string.Empty;

                        return new
                        {
                            AttributeId =
                                value.ProductAttributeId,

                            AttributeCode =
                                code.ToLowerInvariant(),

                            ValueId =
                                value.Id
                        };
                    })
                .OrderBy(x => x.AttributeCode)
                .ThenBy(x => x.AttributeId)
                .ThenBy(x => x.ValueId)
                .Select(
                    x =>
                        $"{x.AttributeCode}:{x.ValueId}")
                .ToList();

        return string.Join(
            "|",
            parts);
    }
    private static void ValidateRequiredProductAttributes(
       Product product)
    {
        var requiredAttributes =
            product.Attributes
                .Where(
                    x =>
                        !x.IsDeleted &&
                        x.IsRequired)
                .ToList();

        foreach (var attribute in requiredAttributes)
        {
            var hasValue =
                attribute.Values.Any(
                    x => !x.IsDeleted);

            if (!hasValue)
            {
                throw new ArgumentException(
                    $"Required product attribute '{attribute.Name}' must have at least one value.");
            }
        }
    }
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

        if (!string.IsNullOrWhiteSpace(updateDto.Currency))
        {
            product.SetCurrency(
                updateDto.Currency);
        }

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
        // Categories
        //
        // Categories MUST be synchronized before Product Attributes.
        // This allows category-level attribute configuration to be
        // available when ProductAttribute definitions are synchronized.
        // ============================================================

        await ReplaceCategoriesAsync(
            product,
            updateDto.CategoryIds,
            cancellationToken);

        // ============================================================
        // Product Specifications
        // ============================================================

        await SynchronizeProductAttributesAsync(
            product,
            updateDto.Attributes,
            cancellationToken);

        // ============================================================
        // Variants
        //
        // Product Attributes must exist before Variant AttributeValue
        // mappings are resolved.
        // ============================================================

        await SynchronizeVariantsAsync(
            product,
            updateDto.Variants,
            cancellationToken);

        // ============================================================
        // Final validation
        // ============================================================

        ValidateRequiredProductAttributes(
            product);

        ValidateVariantDefinitions(
            product);

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
        // Repair tracked ProductAttribute / AttributeValue states
        // ============================================================

        await _productRepository
            .RepairTrackedProductAttributeStatesAsync(
                cancellationToken);

        // ============================================================
        // Existing ProductVariant scalar changes are persisted through
        // repository update operations.
        //
        // New ProductVariants remain Added and must stay attached.
        // ============================================================

        _productRepository
            .NormalizeTrackedProductVariantStates();

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

    private async Task ReplaceVariantAttributeMappingsAsync(
    Product product,
    ProductVariant variant,
    IReadOnlyCollection<AttributeValue> desiredValues,
    string combinationKey,
    CancellationToken cancellationToken)
    {
        var desiredIds =
            desiredValues
                .Select(x => x.Id)
                .ToHashSet();

        var currentIds =
            variant.AttributeValues
                .Select(x => x.AttributeValueId)
                .ToHashSet();

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

            foreach (var id in idsToDelete)
            {
                variant.RemoveAttributeValue(id);
            }
        }

        foreach (var value in desiredValues)
        {
            if (!currentIds.Contains(
                    value.Id))
            {
                variant.AddAttributeValue(
                    value);
            }
        }

        variant.SetCombinationKey(
            combinationKey);
    }
    private async Task SynchronizeVariantsAsync(
        Product product,
        IEnumerable<UpdateProductVariantDto>? variantDtos,
        CancellationToken cancellationToken)
    {
        var requestedVariants =
            (variantDtos ??
             Enumerable.Empty<UpdateProductVariantDto>())
            .ToList();

        var requestedVariantIds =
            new HashSet<Guid>();

        var requestedSkus =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        var requestedCombinationKeys =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var variantDto in requestedVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sku =
                variantDto.Sku?.Trim();

            if (string.IsNullOrWhiteSpace(sku))
            {
                throw new ArgumentException(
                    "Variant SKU is required.");
            }

            if (!requestedSkus.Add(sku))
            {
                throw new ArgumentException(
                    $"Duplicate variant SKU '{sku}' detected.");
            }

            if (await _productRepository.ExistsByVariantSkuAsync(
                    sku,
                    variantDto.Id,
                    cancellationToken))
            {
                throw new ArgumentException(
                    $"Variant SKU '{sku}' already exists.");
            }

            var attributeValues =
                await ResolveVariantAttributeValuesAsync(
                    product,
                    variantDto.AttributeValueIds,
                    cancellationToken);

            var combinationKey =
                BuildCombinationKey(
                    attributeValues);

            if (!requestedCombinationKeys.Add(
                    combinationKey))
            {
                throw new ArgumentException(
                    $"Duplicate variant combination detected for SKU '{sku}'.");
            }

            var variant =
      product.Variants
          .FirstOrDefault(
              x => x.Id == variantDto.Id);

            if (variant is null)
            {
                var newVariant =
                    product.AddVariant(
                        sku,
                        variantDto.PriceOverride ??
                        product.Price,
                        variantDto.StockQuantity ?? 0,
                        variantDto.ComparePrice);

                newVariant.SetBarcode(
                    variantDto.Barcode);

                newVariant.SetCombinationKey(
                    combinationKey);

                newVariant.SetActive(
                    variantDto.IsActive);

                foreach (var attributeValue in attributeValues)
                {
                    newVariant.AddAttributeValue(
                        attributeValue);
                }

                requestedVariantIds.Add(
                    newVariant.Id);

                continue;
            }

            variant.ChangeSku(
      sku);

            variant.ChangePrice(
                variantDto.PriceOverride ??
                product.Price);

            variant.SetComparePrice(
                variantDto.ComparePrice);

            variant.SetBarcode(
                variantDto.Barcode);

            variant.SetCombinationKey(
                combinationKey);

            variant.SetActive(
                variantDto.IsActive);

            await _productRepository.UpdateVariantAsync(
                variant.Id,
                variant.Sku,
                variant.PriceOverride,
                variant.ComparePrice,
                variant.IsActive,
                cancellationToken);

            await ReplaceVariantAttributeMappingsAsync(
                product,
                variant,
                attributeValues,
                combinationKey,
                cancellationToken);

            await _productRepository.ReplaceVariantImagesAsync(
                variant.Id,
                variantDto.Images,
                cancellationToken);
            requestedVariantIds.Add(
                variant.Id);
        }

        // Deactivate variants omitted from the request.
        foreach (var existingVariant in product.Variants)
        {
            if (!requestedVariantIds.Contains(
                    existingVariant.Id))
            {
                existingVariant.Deactivate();
            }
        }

        ValidateVariantDefinitions(
            product);
    }
    private async Task EnrichStockQuantitiesAsync(
      ProductDto productDto,
      CancellationToken cancellationToken)
    {
        if (productDto.Variants is null ||
            productDto.Variants.Count == 0)
        {
            productDto.StockQuantity = 0;
            return;
        }

        var variantIds =
            productDto.Variants
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

        if (variantIds.Length == 0)
        {
            productDto.StockQuantity = 0;
            return;
        }

        var stock =
            await _productStockReader
                .GetAvailableQuantitiesAsync(
                    variantIds,
                    cancellationToken);

        foreach (var variant in productDto.Variants)
        {
            variant.StockQuantity =
                stock.TryGetValue(
                    variant.Id,
                    out var quantity)
                    ? quantity
                    : 0;
        }

        productDto.StockQuantity =
            productDto.Variants
                .Where(x => x.IsActive)
                .Sum(x => x.StockQuantity);
    }
    private async Task<List<AttributeValue>> ResolveVariantAttributeValuesAsync(
        Product product,
        IEnumerable<Guid>? attributeValueIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        var ids =
            (attributeValueIds ?? Enumerable.Empty<Guid>())
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToArray();

        if (ids.Length == 0)
        {
            return new List<AttributeValue>();
        }

        var lookup =
            product.Attributes
                .SelectMany(
                    attribute =>
                        attribute.Values.Select(
                            value =>
                                new
                                {
                                    Attribute = attribute,
                                    Value = value
                                }))
                .ToDictionary(
                    x => x.Value.Id);

        var result =
            new List<AttributeValue>();

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!lookup.TryGetValue(
                    id,
                    out var resolved))
            {
                throw new ArgumentException(
                    $"Attribute value '{id}' does not belong to this product.");
            }

            if (!resolved.Attribute.Role.HasFlag(
                    AttributeRole.VariantDefining))
            {
                throw new ArgumentException(
                    $"Attribute '{resolved.Attribute.Name}' is not configured as VariantDefining.");
            }

            result.Add(
                resolved.Value);
        }

        return result;
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
