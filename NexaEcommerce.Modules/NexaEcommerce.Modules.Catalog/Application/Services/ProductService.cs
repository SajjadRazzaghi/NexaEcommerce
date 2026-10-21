using AutoMapper;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Pagination;
using System.Text.RegularExpressions;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using System.Globalization;
namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IProductStockReader _productStockReader;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICatalogAttributeRepository _catalogAttributeRepository;
    private readonly ICategoryAttributeManagementService?
    _categoryAttributeManagementService;

    public ProductService(
     IProductRepository productRepository,
     IUnitOfWork unitOfWork,
     IMapper mapper,
     IProductStockReader productStockReader,
     ICategoryRepository categoryRepository,
     ICatalogAttributeRepository catalogAttributeRepository,
      ICategoryAttributeManagementService categoryAttributeManagementService)
    {
        _categoryAttributeManagementService =
    categoryAttributeManagementService;
        _productRepository =
            productRepository;

        _unitOfWork =
            unitOfWork;

        _mapper =
            mapper;

        _productStockReader =
            productStockReader;
        _categoryRepository =
    categoryRepository;

        _catalogAttributeRepository =
            catalogAttributeRepository;
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
    private static void ValidateRequestedAttributeValues(
        CatalogAttribute catalogAttribute,
        IEnumerable<ProductAttributeValueInputDto> requestedValues,
        AttributeRole role)
    {
        ArgumentNullException.ThrowIfNull(catalogAttribute);
        ArgumentNullException.ThrowIfNull(requestedValues);

        var values = requestedValues
            .Where(value =>
                (value.CatalogAttributeValueId.HasValue &&
                 value.CatalogAttributeValueId.Value != Guid.Empty) ||
                !string.IsNullOrWhiteSpace(value.Value))
            .ToList();

        if (catalogAttribute.DataType ==
                CatalogAttributeDataType.SingleSelect &&
            values.Count > 1 &&
            !role.HasFlag(AttributeRole.VariantDefining))
        {
            throw new ArgumentException(
                $"Attribute '{catalogAttribute.Name}' accepts only one value.");
        }

        if (catalogAttribute.DataType is
            CatalogAttributeDataType.SingleSelect or
            CatalogAttributeDataType.MultiSelect)
        {
            if (values.Any(value =>
                !value.CatalogAttributeValueId.HasValue ||
                value.CatalogAttributeValueId.Value == Guid.Empty))
            {
                throw new ArgumentException(
                    $"Attribute '{catalogAttribute.Name}' requires a valid catalog value selection.");
            }
        }
    }

    private static void ValidateAttributeValue(
        CatalogAttributeDataType dataType,
        string value,
        string attributeName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"Value for attribute '{attributeName}' cannot be empty.");
        }

        var isValid = dataType switch
        {
            CatalogAttributeDataType.Text =>
                true,

            CatalogAttributeDataType.Integer =>
                long.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out _),

            CatalogAttributeDataType.Decimal =>
                decimal.TryParse(
                    value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out _),

            CatalogAttributeDataType.Boolean =>
                bool.TryParse(value, out _),

            CatalogAttributeDataType.Date =>
                DateOnly.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _),

            CatalogAttributeDataType.DateTime =>
                DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _),

            CatalogAttributeDataType.SingleSelect
                or CatalogAttributeDataType.MultiSelect =>
                true,

            _ => false
        };

        if (!isValid)
        {
            throw new ArgumentException(
                $"Value '{value}' is invalid for attribute " +
                $"'{attributeName}' with data type '{dataType}'.");
        }
    }
    private static void PopulateVariantAttributesFromCombinationKey(
    Product sourceProduct,
    ProductDto targetProduct)
    {
        var variantAttributes = sourceProduct.Attributes
            .Where(attribute =>
                !attribute.IsDeleted &&
                attribute.Role.HasFlag(
                    AttributeRole.VariantDefining))
            .ToList();

        var sourceVariants = sourceProduct.Variants
            .ToDictionary(variant => variant.Id);

        foreach (var targetVariant in targetProduct.Variants)
        {
            if (!sourceVariants.TryGetValue(
                    targetVariant.Id,
                    out var sourceVariant))
            {
                continue;
            }

            targetVariant.Attributes ??=
                new List<ProductVariantAttributeDto>();

            // نگاشت‌های موجود را فقط در صورتی نگه می‌داریم
            // که هنوز به ویژگی و مقدار فعلی و معتبر محصول تعلق داشته باشند.
            targetVariant.Attributes =
                targetVariant.Attributes
                    .Where(mapped =>
                        variantAttributes.Any(attribute =>
                            attribute.Id == mapped.ProductAttributeId &&
                            attribute.Values.Any(value =>
                                !value.IsDeleted &&
                                value.Id == mapped.AttributeValueId)))
                    .ToList();

            var resolvedAttributeIds = targetVariant.Attributes
                .Select(attribute => attribute.ProductAttributeId)
                .ToHashSet();

            if (string.IsNullOrWhiteSpace(
                    sourceVariant.CombinationKey))
            {
                continue;
            }

            var parts = sourceVariant.CombinationKey.Split(
                '|',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var separatorIndex = part.IndexOf(':');

                if (separatorIndex <= 0 ||
                    separatorIndex == part.Length - 1)
                {
                    continue;
                }

                var attributeCode = part[..separatorIndex].Trim();
                var valueIdText = part[(separatorIndex + 1)..].Trim();

                if (!Guid.TryParse(valueIdText, out var valueId))
                {
                    continue;
                }

                // فقط ویژگی‌هایی قابل استفاده‌اند که در حال حاضر
                // VariantDefining باشند؛ مثل سایز.
                // ویژگی رنگ که اکنون Descriptive است، نادیده گرفته می‌شود.
                var productAttribute = variantAttributes
                    .FirstOrDefault(attribute =>
                        string.Equals(
                            attribute.Code?.Trim(),
                            attributeCode,
                            StringComparison.OrdinalIgnoreCase));

                if (productAttribute is null ||
                    resolvedAttributeIds.Contains(productAttribute.Id))
                {
                    continue;
                }

                var attributeValue = productAttribute.Values
                    .FirstOrDefault(value =>
                        !value.IsDeleted &&
                        value.Id == valueId);

                if (attributeValue is null)
                {
                    continue;
                }

                targetVariant.Attributes.Add(
                    new ProductVariantAttributeDto
                    {
                        AttributeValueId = attributeValue.Id,
                        ProductAttributeId = productAttribute.Id,
                        CatalogAttributeId =
                            productAttribute.CatalogAttributeId,
                        CatalogAttributeValueId =
                            attributeValue.CatalogAttributeValueId,
                        AttributeCode = productAttribute.Code,
                        AttributeName = productAttribute.Name,
                        Role = productAttribute.Role.ToString(),
                        RoleValue = (int)productAttribute.Role,
                        Value = attributeValue.Value,
                        DisplayValue = attributeValue.DisplayValue,
                        ColorHex = attributeValue.ColorHex
                    });

                resolvedAttributeIds.Add(productAttribute.Id);
            }
        }
    }
    public async Task<ProductDto?> GetByIdAsync(
     Guid id,
     CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        var dto = _mapper.Map<ProductDto>(product);

        PopulateVariantAttributesFromCombinationKey(
            product,
            dto);

        return dto;
    }
    public async Task<ProductDto?> GetBySlugAsync(
     string slug,
     CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var product = await _productRepository.GetBySlugAsync(
            slug,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        var dto = _mapper.Map<ProductDto>(product);

        PopulateVariantAttributesFromCombinationKey(
            product,
            dto);

        return dto;
    }
    private static decimal ResolveVariantPrice(
    decimal? requestedPrice,
    decimal productPrice)
    {
        if (requestedPrice.HasValue &&
            requestedPrice.Value < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedPrice),
                "Variant price cannot be negative.");
        }

        // صفر یا null یعنی استفاده از قیمت اصلی محصول.
        return requestedPrice.HasValue &&
               requestedPrice.Value > 0m
            ? requestedPrice.Value
            : productPrice;
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

            var variant = product.AddVariant(
      sku,
      ResolveVariantPrice(
          variantDto.PriceOverride,
          product.Price),
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

        var createVariants = createDto.Variants ?? new List<CreateProductVariantDto>();

        var createBarcodes = createVariants
            .Where(v => !string.IsNullOrWhiteSpace(v.Barcode))
            .Select(v => v.Barcode!.Trim())
            .ToList();

        if (createBarcodes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != createBarcodes.Count)
        {
            throw new ArgumentException(
                "Each product variant must have a unique barcode.");
        }

        foreach (var barcode in createBarcodes)
        {
            if (await _productRepository.ExistsByVariantBarcodeAsync(
                    barcode,
                    cancellationToken: cancellationToken))
            {
                throw new ArgumentException(
                    $"Barcode '{barcode}' is already assigned to another product variant.");
            }
        }

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
        product.SetRecommendedProductIds(
    await ValidateRecommendedProductIdsAsync(
        createDto.RecommendedProductIds,
        product.Id,
        cancellationToken));

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
        await ValidateRequiredCategoryAttributesAsync(
    product,
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
        if (updateDto.RecommendedProductIds is not null)
        {
            product.SetRecommendedProductIds(
                await ValidateRecommendedProductIdsAsync(
                    updateDto.RecommendedProductIds,
                    product.Id,
                    cancellationToken));
        }
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

        await SynchronizeVariantsAsync(
            product,
            updateDto.Variants,
            cancellationToken);

        // Second pass:
        // Variant mappings are now final, so previously protected
        // VariantDefining attributes can safely be removed when they
        // are no longer requested.
        await SynchronizeProductAttributesAsync(
      product,
      updateDto.Attributes,
      cancellationToken);

        ValidateRequiredProductAttributes(product);

        await ValidateRequiredCategoryAttributesAsync(
            product,
            cancellationToken);

        ValidateVariantDefinitions(product);

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
    private async Task<List<Guid>> ValidateRecommendedProductIdsAsync(
    IEnumerable<Guid>? requestedIds,
    Guid currentProductId,
    CancellationToken cancellationToken)
    {
        var requested = (
            requestedIds ?? Enumerable.Empty<Guid>()
        ).ToList();

        if (requested.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException(
                "Recommended product IDs cannot contain an empty ID.");
        }

        var ids = requested
            .Distinct()
            .ToList();

        if (ids.Count > 12)
        {
            throw new ArgumentException(
                "A product can have up to 12 recommended products.");
        }

        if (ids.Contains(currentProductId))
        {
            throw new ArgumentException(
                "A product cannot recommend itself.");
        }

        if (ids.Count == 0)
        {
            return ids;
        }

        var existingIds =
            await _productRepository.GetExistingIdsAsync(
                ids,
                cancellationToken);

        var existingSet = existingIds.ToHashSet();

        var missingIds = ids
            .Where(id => !existingSet.Contains(id))
            .ToArray();

        if (missingIds.Length > 0)
        {
            throw new ArgumentException(
                "One or more recommended products were not found.");
        }

        return ids;
    }
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

        var requests = requestedAttributes.ToList();

        var catalogAttributes =
            await _catalogAttributeRepository.GetAllAsync(
                cancellationToken);

        // نگهداری مشخصه‌هایی که هنوز توسط تنوع‌های فعلی استفاده می‌شوند.
        var protectedVariantAttributeIds =
            product.Variants
                .SelectMany(variant => variant.AttributeValues)
                .Where(mapping =>
                    mapping.AttributeValue?.ProductAttribute != null &&
                    mapping.AttributeValue.ProductAttribute.Id != Guid.Empty)
                .Select(mapping =>
                    mapping.AttributeValue!.ProductAttribute.Id)
                .ToHashSet();

        // Resolve catalog attributes and reject duplicate requests.
        var resolvedRequests =
            new List<(
                CatalogAttribute Attribute,
                ProductAttributeInputDto Request,
                List<ProductAttributeValueInputDto> Values)>();

        var requestedCodes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request.CatalogAttributeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Catalog attribute is required.");
            }

            var catalogAttribute =
                catalogAttributes.FirstOrDefault(attribute =>
                    attribute.Id == request.CatalogAttributeId);

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

            var attributeCode = catalogAttribute.Code?.Trim();

            if (string.IsNullOrWhiteSpace(attributeCode))
            {
                throw new ArgumentException(
                    $"Catalog attribute '{catalogAttribute.Id}' has an empty code.");
            }

            if (!requestedCodes.Add(attributeCode))
            {
                throw new ArgumentException(
                    $"Catalog attribute '{catalogAttribute.Name}' was supplied more than once.");
            }

            resolvedRequests.Add((
                catalogAttribute,
                request,
                request.Values?.ToList()
                    ?? new List<ProductAttributeValueInputDto>()));
        }

        // Keep existing ProductAttribute IDs stable.
        var existingAttributesByCode =
            product.Attributes
                .Where(attribute =>
                    !string.IsNullOrWhiteSpace(attribute.Code))
                .GroupBy(
                    attribute => attribute.Code.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);

        foreach (var resolvedRequest in resolvedRequests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var catalogAttribute = resolvedRequest.Attribute;
            var inputRequest = resolvedRequest.Request;
            var attributeName = catalogAttribute.Name.Trim();
            var attributeCode = catalogAttribute.Code.Trim();

            // Determine the role before validating the requested values.
            var role = ResolveProductAttributeRole(
                catalogAttribute,
                inputRequest);

            ValidateRequestedAttributeValues(
                catalogAttribute,
                resolvedRequest.Values,
                role);

            var isRequired =
                inputRequest.IsRequired ??
                (role.HasFlag(AttributeRole.VariantDefining)
                    ? true
                    : catalogAttribute.IsRequired);

            var displayOrder = inputRequest.DisplayOrder;

            if (displayOrder < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inputRequest.DisplayOrder),
                    $"Display order for attribute '{attributeName}' cannot be negative.");
            }

            existingAttributesByCode.TryGetValue(
                attributeCode,
                out var productAttribute);

            // Create or update the ProductAttribute.
            if (productAttribute is null)
            {
                productAttribute = product.AddAttribute(
                    attributeName,
                    attributeCode,
                    catalogAttribute.Id,
                    role,
                    isRequired,
                    displayOrder);

                existingAttributesByCode[attributeCode] =
                    productAttribute;
            }
            else
            {
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

                productAttribute.SetCatalogAttribute(catalogAttribute.Id);
                productAttribute.SetRole(role);
                productAttribute.SetRequired(isRequired);
                productAttribute.SetDisplayOrder(displayOrder);
            }

            // Normalize requested values by value text.
            var requestedValues =
                new Dictionary<
                    string,
                    (
                        string Value,
                        string? DisplayValue,
                        string? ColorHex,
                        Guid? CatalogAttributeValueId,
                        int DisplayOrder)>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var requestedValue in resolvedRequest.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CatalogAttributeValue? catalogValue = null;

                if (requestedValue.CatalogAttributeValueId.HasValue &&
                    requestedValue.CatalogAttributeValueId.Value != Guid.Empty)
                {
                    catalogValue =
                        catalogAttribute.Values.FirstOrDefault(value =>
                            value.Id ==
                            requestedValue.CatalogAttributeValueId.Value);

                    if (catalogValue is null)
                    {
                        throw new ArgumentException(
                            $"Catalog value '{requestedValue.CatalogAttributeValueId}' does not belong to '{attributeName}'.");
                    }

                    if (!catalogValue.IsActive)
                    {
                        var alreadyUsed =
                            productAttribute.Values.Any(value =>
                                value.CatalogAttributeValueId == catalogValue.Id);

                        if (!alreadyUsed)
                        {
                            throw new ArgumentException(
                                $"Catalog value '{catalogValue.Value}' is inactive.");
                        }
                    }
                }

                var value = (
                    catalogValue?.Value ?? requestedValue.Value
                )?.Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (catalogAttribute.DataType is
                    CatalogAttributeDataType.SingleSelect or
                    CatalogAttributeDataType.MultiSelect)
                {
                    if (!requestedValue.CatalogAttributeValueId.HasValue ||
                        requestedValue.CatalogAttributeValueId.Value == Guid.Empty ||
                        catalogValue is null)
                    {
                        throw new ArgumentException(
                            $"Attribute '{attributeName}' requires a valid catalog value selection.");
                    }
                }
                else
                {
                    ValidateAttributeValue(
                        catalogAttribute.DataType,
                        value,
                        attributeName);
                }

                var displayValue = (
                    catalogValue?.DisplayValue ?? requestedValue.DisplayValue
                )?.Trim();

                var colorHex = (
                    catalogValue?.ColorHex ?? requestedValue.ColorHex
                )?.Trim();

                var catalogValueId =
                    catalogValue?.Id ??
                    requestedValue.CatalogAttributeValueId;

                if (!requestedValues.ContainsKey(value))
                {
                    requestedValues[value] = (
                        value,
                        string.IsNullOrWhiteSpace(displayValue)
                            ? null
                            : displayValue,
                        string.IsNullOrWhiteSpace(colorHex)
                            ? null
                            : colorHex,
                        catalogValueId,
                        requestedValue.DisplayOrder);
                }
            }

            // Existing ProductAttributeValues, indexed by their value.
            var existingValuesByValue =
                productAttribute.Values
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value.Value))
                    .GroupBy(
                        value => value.Value.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First(),
                        StringComparer.OrdinalIgnoreCase);

            // Add or update requested values.
            foreach (var requestedValue in requestedValues.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedValue = requestedValue.Value.Trim();

                existingValuesByValue.TryGetValue(
                    normalizedValue,
                    out var existingValue);

                if (existingValue is not null)
                {
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

                    if (catalogAttribute.DataType is
                            CatalogAttributeDataType.SingleSelect or
                            CatalogAttributeDataType.MultiSelect &&
                        !requestedValue.CatalogAttributeValueId.HasValue)
                    {
                        throw new ArgumentException(
                            $"Attribute '{attributeName}' only accepts predefined catalog values.");
                    }

                    // Keep the catalog value reference synchronized.
                    var requestedCatalogValueId =
                        requestedValue.CatalogAttributeValueId;

                    if (requestedCatalogValueId.HasValue &&
                        requestedCatalogValueId.Value != Guid.Empty)
                    {
                        if (existingValue.CatalogAttributeValueId !=
                            requestedCatalogValueId.Value)
                        {
                            existingValue.SetCatalogValue(
                                requestedCatalogValueId.Value);
                        }
                    }
                    else if (existingValue.CatalogAttributeValueId.HasValue)
                    {
                        existingValue.ClearCatalogValue();
                    }

                    continue;
                }

                productAttribute.AddValue(
                    requestedValue.Value,
                    requestedValue.DisplayValue,
                    requestedValue.ColorHex,
                    requestedValue.CatalogAttributeValueId);
            }

            // Remove values no longer requested, except values still used
            // by any existing variant mapping.
            foreach (var existingValue in productAttribute.Values.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (requestedValues.ContainsKey(existingValue.Value.Trim()))
                {
                    continue;
                }

                var isUsedByVariant =
                    product.Variants
                        .SelectMany(variant => variant.AttributeValues)
                        .Any(mapping =>
                            mapping.AttributeValueId == existingValue.Id);

                if (isUsedByVariant)
                {
                    continue;
                }

                productAttribute.Values.Remove(existingValue);
            }
        }

        // Remove attributes omitted from the request, but preserve those
        // still referenced by existing variants during this sync pass.
        foreach (var existingAttribute in product.Attributes.ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (protectedVariantAttributeIds.Contains(existingAttribute.Id))
            {
                continue;
            }

            var existingCode = existingAttribute.Code?.Trim();

            if (!string.IsNullOrWhiteSpace(existingCode) &&
                requestedCodes.Contains(existingCode))
            {
                continue;
            }

            product.Attributes.Remove(existingAttribute);
        }

        return product;
    }
    private async Task ValidateRequiredCategoryAttributesAsync(
     Product product,
     CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(product);

        // برای سازگاری با تست‌هایی که هنوز این سرویس را تزریق نکرده‌اند.
        // در برنامه اصلی باید سرویس ثبت و تزریق شده باشد.
        if (_categoryAttributeManagementService is null)
        {
            return;
        }

        var categoryIds = product.ProductCategories
            .Select(category => category.CategoryId)
            .Where(categoryId => categoryId != Guid.Empty)
            .Distinct()
            .ToArray();

        foreach (var categoryId in categoryIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var definitions =
                await _categoryAttributeManagementService.GetForCategoryAsync(
                    categoryId,
                    cancellationToken);

            var requiredDefinitions = definitions
                .Where(definition =>
                    definition.IsActive &&
                    definition.IsRequired)
                .ToList();

            foreach (var definition in requiredDefinitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var productAttribute = product.Attributes
                    .FirstOrDefault(attribute =>
                        !attribute.IsDeleted &&
                        attribute.CatalogAttributeId ==
                            definition.CatalogAttributeId);

                var hasValue = productAttribute is not null &&
                    productAttribute.Values.Any(value =>
                        !value.IsDeleted &&
                        !string.IsNullOrWhiteSpace(value.Value));

                if (!hasValue)
                {
                    throw new ArgumentException(
                        $"Required category attribute " +
                        $"'{definition.Name}' must have a value for category " +
                        $"'{categoryId}'.");
                }
            }
        }
    }
    
    private static AttributeRole ResolveProductAttributeRole(
      CatalogAttribute catalogAttribute,
      ProductAttributeInputDto request)
    {
        ArgumentNullException.ThrowIfNull(
            catalogAttribute);

        ArgumentNullException.ThrowIfNull(
            request);

        AttributeRole role;

        // ------------------------------------------------------------
        // 1. Explicit numeric role sent by the frontend.
        // ------------------------------------------------------------

        if (request.RoleValue.HasValue)
        {
            role =
                (AttributeRole)request.RoleValue.Value;
        }
        // ------------------------------------------------------------
        // 2. Explicit enum name sent by the frontend.
        // ------------------------------------------------------------

        else if (
            !string.IsNullOrWhiteSpace(
                request.Role) &&
            Enum.TryParse<AttributeRole>(
                request.Role,
                true,
                out var parsedRole))
        {
            role =
                parsedRole;
        }
        // ------------------------------------------------------------
        // 3. Backward-compatible fallback.
        //
        // Existing catalog data still uses IsVariantAttribute.
        // ------------------------------------------------------------

        else
        {
            role =
                catalogAttribute.IsVariantAttribute
                    ? AttributeRole.VariantDefining
                    : AttributeRole.Descriptive;

            if (catalogAttribute.IsFilterable)
            {
                role |=
                    AttributeRole.Filterable;
            }
        }

        // ------------------------------------------------------------
        // Only the known AttributeRole flags are allowed.
        // ------------------------------------------------------------

        const AttributeRole allowedRoles =
            AttributeRole.Filterable |
            AttributeRole.Descriptive |
            AttributeRole.VariantDefining;

        if (
            role == AttributeRole.None ||
            ((int)role & ~(int)allowedRoles) != 0)
        {
            throw new ArgumentException(
                $"Invalid attribute role '{(int)role}' for catalog attribute '{catalogAttribute.Name}'.");
        }

        return role;
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
        ArgumentNullException.ThrowIfNull(
            product);

        ArgumentNullException.ThrowIfNull(
            variant);

        ArgumentNullException.ThrowIfNull(
            desiredValues);

        var existingMappings =
            variant.AttributeValues
                .ToList();

        var existingAttributeValueIds =
            existingMappings
                .Select(
                    mapping =>
                        mapping.AttributeValueId)
                .Where(
                    id =>
                        id != Guid.Empty)
                .Distinct()
                .ToArray();

        /*
         * Delete all persisted mappings for this Variant.
         */
        if (existingAttributeValueIds.Length > 0)
        {
            await _productRepository
                .DeleteVariantAttributeMappingsAsync(
                    variant.Id,
                    existingAttributeValueIds,
                    cancellationToken);
        }

        /*
         * IMPORTANT:
         *
         * DeleteVariantAttributeMappingsAsync also detaches the
         * tracked mapping entities from EF Core.
         *
         * Therefore the old mapping objects must also be removed
         * from the Variant navigation collection.
         *
         * Otherwise a detached old mapping and a new Added mapping
         * can coexist in variant.AttributeValues.
         */
        foreach (var mapping in existingMappings)
        {
            variant.AttributeValues.Remove(
                mapping);
        }

        /*
         * Rebuild the navigation collection from the desired values.
         *
         * ReplaceAttributeValues also removes duplicate AttributeValue
         * IDs from desiredValues.
         */
        variant.ReplaceAttributeValues(
            desiredValues);

        variant.SetCombinationKey(
            combinationKey);
    }
    private async Task SynchronizeVariantsAsync(
       Product product,
       IEnumerable<UpdateProductVariantDto>? variantDtos,
       CancellationToken cancellationToken)
    {
        static string? NormalizeBarcode(string? barcode)
        {
            return string.IsNullOrWhiteSpace(barcode)
                ? null
                : barcode.Trim();
        }

        cancellationToken.ThrowIfCancellationRequested();

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

        /*
         * Validate requested variant IDs.
         *
         * A non-empty ID must belong to this product.
         * A new variant must use Guid.Empty.
         */
        var duplicateVariantId =
            requestedVariants
                .Where(x => x.Id != Guid.Empty)
                .GroupBy(x => x.Id)
                .FirstOrDefault(x => x.Count() > 1);

        if (duplicateVariantId is not null)
        {
            throw new ArgumentException(
                $"Duplicate variant ID '{duplicateVariantId.Key}' detected.");
        }

        foreach (var variantDto in requestedVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (variantDto.Id == Guid.Empty)
            {
                continue;
            }

            var existingVariant =
                product.Variants.FirstOrDefault(
                    x => x.Id == variantDto.Id);

            if (existingVariant is null)
            {
                throw new ArgumentException(
                    $"Variant '{variantDto.Id}' does not belong to product '{product.Id}'.");
            }
        }

        /*
         * Validate SKU uniqueness within the request.
         */
        foreach (var variantDto in requestedVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sku = variantDto.Sku?.Trim();

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
        }

        /*
         * Determine the effective barcode for each requested variant.
         *
         * null:
         *     Preserve the existing barcode of an existing variant.
         *
         * Empty or whitespace:
         *     Clear the barcode.
         *
         * Non-empty:
         *     Trim and validate the new barcode.
         */
        var requestedBarcodes =
            requestedVariants
                .Select(variantDto =>
                {
                    var existingVariant =
                        variantDto.Id == Guid.Empty
                            ? null
                            : product.Variants.First(
                                x => x.Id == variantDto.Id);

                    var effectiveBarcode =
                        variantDto.Barcode ??
                        existingVariant?.Barcode;

                    return NormalizeBarcode(
                        effectiveBarcode);
                })
                .Where(barcode => barcode is not null)
                .Select(barcode => barcode!)
                .ToList();

        /*
         * Validate barcode uniqueness within the request.
         */
        if (requestedBarcodes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != requestedBarcodes.Count)
        {
            throw new ArgumentException(
                "Each product variant must have a unique barcode.");
        }

        /*
         * Validate barcode uniqueness across persisted variants.
         *
         * Existing variants exclude their own ID.
         * New variants use Guid.Empty.
         */
        foreach (var variantDto in requestedVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingVariant =
                variantDto.Id == Guid.Empty
                    ? null
                    : product.Variants.First(
                        x => x.Id == variantDto.Id);

            var effectiveBarcode =
                NormalizeBarcode(
                    variantDto.Barcode ??
                    existingVariant?.Barcode);

            if (effectiveBarcode is null)
            {
                continue;
            }

            if (await _productRepository.ExistsByVariantBarcodeAsync(
                    effectiveBarcode,
                    variantDto.Id,
                    cancellationToken))
            {
                throw new ArgumentException(
                    $"Barcode '{effectiveBarcode}' is already assigned to another product variant.");
            }
        }

        /*
         * Create and update variants.
         */
        foreach (var variantDto in requestedVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sku = variantDto.Sku!.Trim();

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
                variantDto.Id == Guid.Empty
                    ? null
                    : product.Variants.First(
                        x => x.Id == variantDto.Id);

            var effectivePrice =
                ResolveVariantPrice(
                    variantDto.PriceOverride,
                    product.Price);

            var effectiveBarcode =
                NormalizeBarcode(
                    variantDto.Barcode ??
                    variant?.Barcode);

            /*
             * Create a new variant.
             */
            if (variant is null)
            {
                var newVariant =
                    product.AddVariant(
                        sku,
                        effectivePrice,
                        0,
                        variantDto.ComparePrice);

                newVariant.SetBarcode(
                    effectiveBarcode);

                newVariant.SetCombinationKey(
                    combinationKey);

                newVariant.SetActive(
                    variantDto.IsActive);

                foreach (var attributeValue in attributeValues)
                {
                    newVariant.AddAttributeValue(
                        attributeValue);
                }

                /*
                 * Explicitly add the new variant to the repository.
                 */
                await _productRepository.AddVariantAsync(
                    newVariant,
                    cancellationToken);

                /*
                 * Ensure that images are synchronized for new variants too.
                 */
                await _productRepository.ReplaceVariantImagesAsync(
                    newVariant.Id,
                    variantDto.Images,
                    cancellationToken);

                requestedVariantIds.Add(
                    newVariant.Id);

                continue;
            }

            /*
             * Update an existing variant.
             */
            variant.ChangeSku(
                sku);

            variant.ChangePrice(
                effectivePrice);

            variant.SetComparePrice(
                variantDto.ComparePrice);

            /*
             * Do not clear an existing barcode just because an older
             * client omitted the Barcode property.
             *
             * An explicitly supplied empty value clears the barcode.
             */
            if (variantDto.Barcode is not null)
            {
                variant.SetBarcode(
                    NormalizeBarcode(
                        variantDto.Barcode));
            }

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

            /*
             * Synchronize attribute mappings for the existing variant.
             */
            await ReplaceVariantAttributeMappingsAsync(
                product,
                variant,
                attributeValues,
                combinationKey,
                cancellationToken);

            /*
             * Replace images for the existing variant.
             */
            await _productRepository.ReplaceVariantImagesAsync(
                variant.Id,
                variantDto.Images,
                cancellationToken);

            requestedVariantIds.Add(
                variant.Id);
        }

        /*
         * Deactivate variants omitted from the request.
         *
         * Explicitly passing an empty collection therefore deactivates
         * all existing variants.
         */
        foreach (var existingVariant in product.Variants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (requestedVariantIds.Contains(
                    existingVariant.Id))
            {
                continue;
            }

            var existingAttributeValueIds =
                existingVariant.AttributeValues
                    .Select(mapping => mapping.AttributeValueId)
                    .Distinct()
                    .ToArray();

            if (existingAttributeValueIds.Length > 0)
            {
                await _productRepository
                    .DeleteVariantAttributeMappingsAsync(
                        existingVariant.Id,
                        existingAttributeValueIds,
                        cancellationToken);

                foreach (var attributeValueId
                         in existingAttributeValueIds)
                {
                    existingVariant.RemoveAttributeValue(
                        attributeValueId);
                }
            }

            existingVariant.Deactivate();
        }
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

        /*
         * Frontend sends CatalogAttributeValue IDs.
         *
         * The VariantAttributeValue table, however, points to the
         * Product-side AttributeValue ID.
         *
         * Therefore both identities are accepted:
         *
         *   Product AttributeValue.Id
         *   CatalogAttributeValue.Id
         *
         * Existing callers which already send Product AttributeValue
         * IDs remain compatible.
         */

        var values =
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
                .ToList();

        var byProductValueId =
            values
                .Where(
                    x =>
                        x.Value.Id != Guid.Empty)
                .ToDictionary(
                    x =>
                        x.Value.Id);

        var byCatalogValueId =
            values
                .Where(
                    x =>
                        x.Value.CatalogAttributeValueId.HasValue &&
                        x.Value.CatalogAttributeValueId.Value != Guid.Empty)
                .GroupBy(
                    x =>
                        x.Value.CatalogAttributeValueId!.Value)
                .ToDictionary(
                    group =>
                        group.Key,
                    group =>
                        group.First());

        var result =
            new List<AttributeValue>();

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!byProductValueId.TryGetValue(
                    id,
                    out var resolved) &&
                !byCatalogValueId.TryGetValue(
                    id,
                    out resolved))
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

