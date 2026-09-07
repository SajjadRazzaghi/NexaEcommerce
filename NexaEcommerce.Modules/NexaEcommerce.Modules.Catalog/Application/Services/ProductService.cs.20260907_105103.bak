using AutoMapper;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Pagination;
using System.Text.RegularExpressions;

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

        // --------------------------------------------------------
        // Categories
        // --------------------------------------------------------

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

        var product =
            await GetExistingProductAsync(
                id,
                cancellationToken);

        var normalizedSlug =
            await CreateUniqueSlugAsync(
                updateDto.Name,
                cancellationToken,
                id);

        product.Update(
            updateDto.Name.Trim(),
            normalizedSlug,
            updateDto.Description,
            updateDto.Price);

        product.SetShortDescription(
            updateDto.ShortDescription);

        product.SetCurrency(
            updateDto.Currency);

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

        product.SetBrand(
            updateDto.BrandId);

        product.SetManufacturer(
            updateDto.ManufacturerId);

        await ReplaceCategoriesAsync(
            product,
            updateDto.CategoryIds,
            cancellationToken);
        await SynchronizeVariantsAsync(
    product,
    updateDto.Variants,
    cancellationToken);
        _productRepository.Update(
            product);

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

        // No variant list means:
        // preserve the existing lifecycle for backwards compatibility.
        if (requested.Count == 0)
            return;

        var duplicateIds =
            requested
                .Where(x => x.Id.HasValue &&
                            x.Id.Value != Guid.Empty)
                .GroupBy(x => x.Id!.Value)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

        if (duplicateIds.Count > 0)
        {
            throw new ArgumentException(
                "The same variant cannot appear more than once.");
        }

        var normalizedSkus =
            requested
                .Select(
                    x => x.Sku.Trim())
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
                    x => x,
                    StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

        if (duplicateSkus.Count > 0)
        {
            throw new ArgumentException(
                $"Duplicate variant SKU detected: {string.Join(", ", duplicateSkus)}.");
        }

        var hasExistingIds =
            requested.Any(
                x => x.Id.HasValue &&
                     x.Id.Value != Guid.Empty);

        var matchedExisting =
            new HashSet<Guid>();

        foreach (var dto in requested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sku = dto.Sku.Trim();

            if (dto.Id.HasValue &&
                dto.Id.Value != Guid.Empty)
            {
                var variant =
                    product.Variants.FirstOrDefault(
                        x => x.Id == dto.Id.Value);

                if (variant is null)
                {
                    throw new KeyNotFoundException(
                        $"Product variant '{dto.Id}' was not found.");
                }

                if (variant.ProductId != product.Id)
                {
                    throw new InvalidOperationException(
                        "The specified variant does not belong to this product.");
                }

                if (await _productRepository.ExistsByVariantSkuAsync(
                        sku,
                        variant.Id,
                        cancellationToken))
                {
                    throw new ArgumentException(
                        $"Variant SKU '{sku}' already exists.");
                }

                variant.ChangeSku(sku);

                variant.ChangePrice(
                    dto.PriceOverride ??
                    product.Price);

                variant.SetComparePrice(
                    dto.ComparePrice);

                variant.SetActive(
                    dto.IsActive);

                await ReplaceVariantAttributesAsync(
                    product,
                    variant,
                    dto,
                    cancellationToken);

                matchedExisting.Add(
                    variant.Id);

                // IMPORTANT:
                // StockQuantity is deliberately NOT changed here
                // for an existing variant.
                continue;
            }

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

            if (dto.StockQuantity.HasValue)
            {
                if (dto.StockQuantity.Value < 0)
                {
                    throw new ArgumentException(
                        $"Stock quantity for variant '{sku}' cannot be negative.");
                }

                newVariant.ChangeStock(
                    dto.StockQuantity.Value);
            }

            newVariant.SetActive(
                dto.IsActive);

            await ReplaceVariantAttributesAsync(
                product,
                newVariant,
                dto,
                cancellationToken);
        }

        // Only synchronize removals when the request contains
        // explicit existing IDs. This prevents legacy callers
        // that send SKU-only variants from accidentally disabling
        // every current variant.
        if (hasExistingIds)
        {
            foreach (var existingVariant in
                     product.Variants.ToList())
            {
                if (!matchedExisting.Contains(
                        existingVariant.Id) &&
                    !requested.Any(
                        x => x.Id == existingVariant.Id))
                {
                    existingVariant.Deactivate();
                }
            }
        }

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
            activeVariants);
    }
    private async Task ReplaceVariantAttributesAsync(
    Product product,
    ProductVariant variant,
    UpdateProductVariantDto dto,
    CancellationToken cancellationToken)
    {
        variant.AttributeValues.Clear();

        AddVariantAttribute(
            product,
            variant,
            dto.Color,
            "Color",
            "color");

        AddVariantAttribute(
            product,
            variant,
            dto.Size,
            "Size",
            "size");

        var requestedIds =
            (dto.AttributeValueIds ??
             Enumerable.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (requestedIds.Length == 0)
            return;

        var catalogAttributes =
            await _catalogAttributeRepository.GetAllAsync(
                cancellationToken);

        var selectedValues =
            new List<
                NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes.AttributeValue>();

        var selectedAttributeIds =
            new HashSet<Guid>();

        foreach (var valueId in requestedIds)
        {
            var catalogAttribute =
                catalogAttributes.FirstOrDefault(
                    attribute =>
                        attribute.Values.Any(
                            value => value.Id == valueId));

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
                    value => value.Id == valueId);

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

            var productValue =
                productAttribute.Values.FirstOrDefault(
                    value =>
                        string.Equals(
                            value.Value,
                            catalogValue.Value,
                            StringComparison.OrdinalIgnoreCase));

            productValue ??=
                productAttribute.AddValue(
                    catalogValue.Value,
                    catalogValue.DisplayValue ??
                    catalogValue.Value,
                    catalogValue.ColorHex);

            selectedValues.Add(
                productValue);
        }

        foreach (var value in selectedValues)
        {
            variant.AddAttributeValue(
                value);
        }
    }
    private static void ValidateVariantAttributeCombinations(
    IEnumerable<ProductVariant> variants)
    {
        var signatures =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var variant in variants)
        {
            var signature =
                string.Join(
                    "|",
                    variant.AttributeValues
                        .Where(
                            x => x.AttributeValue != null &&
                                 x.AttributeValue.ProductAttribute != null)
                        .Select(
                            x =>
                                $"{x.AttributeValue!.ProductAttribute!.Code}:{x.AttributeValueId}")
                        .OrderBy(
                            x => x,
                            StringComparer.OrdinalIgnoreCase));

            // Variants without attributes are allowed for simple products.
            if (string.IsNullOrWhiteSpace(signature))
                continue;

            if (!signatures.Add(signature))
            {
                throw new ArgumentException(
                    $"Duplicate variant attribute combination detected for SKU '{variant.Sku}'.");
            }
        }
    }


}
