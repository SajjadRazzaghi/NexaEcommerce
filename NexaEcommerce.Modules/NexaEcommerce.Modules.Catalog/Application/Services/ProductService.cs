using AutoMapper;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Pagination;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

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
        var result = await _productRepository.GetPagedAsync(
            page, pageSize, search, categoryId, brandId,
            minPrice, maxPrice, isFeatured, isInStock, isActive, isPublished,
            includeInactive, includeUnpublished, sortBy, desc, cancellationToken);

        var items = _mapper.Map<IReadOnlyList<ProductDto>>(result.Items);

        return PagedResult<ProductDto>.Create(
            items,
            page,
            pageSize,
            result.TotalItems);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetByCategoryAsync(categoryId, cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.SearchAsync(searchTerm, cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetFeaturedAsync(count, cancellationToken);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductDto createDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        if (string.IsNullOrWhiteSpace(createDto.Name))
            throw new ArgumentException("Product name is required.", nameof(createDto.Name));

        if (createDto.Price < 0)
            throw new ArgumentOutOfRangeException(nameof(createDto.Price));

        var slug = GenerateSlug(createDto.Name);
        var sku = string.IsNullOrWhiteSpace(createDto.Sku)
            ? GenerateSku()
            : createDto.Sku.Trim();

        var product = new Product(
            createDto.Name.Trim(),
            sku,
            slug,
            createDto.Price,
            createDto.Currency ?? "IRR",
            createDto.Description);

        product.SetShortDescription(createDto.ShortDescription);
        product.SetBrand(createDto.BrandId);

        foreach (var imageUrl in createDto.Images ?? new List<string>())
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                continue;

            var displayOrder = product.Images.Count;
            product.AddImage(imageUrl.Trim(), displayOrder, displayOrder == 0);
        }

        foreach (var variantDto in createDto.Variants ?? new List<CreateProductVariantDto>())
        {
            if (string.IsNullOrWhiteSpace(variantDto.Sku))
                throw new ArgumentException("Variant SKU is required.", nameof(createDto));

            var variant = product.AddVariant(
                variantDto.Sku.Trim(),
                variantDto.PriceOverride ?? createDto.Price);

            variant.ChangeStock(Math.Max(0, variantDto.StockQuantity));

            AddVariantAttribute(product, variant, variantDto.Color, "Color", "color");
            AddVariantAttribute(product, variant, variantDto.Size, "Size", "size");
        }

        // A product always has a stock-bearing variant. This keeps stock
        // calculations consistent until the Inventory module is introduced.
        if (product.Variants.Count == 0)
        {
            var defaultVariant = product.AddVariant(
                $"{product.Sku}-DEFAULT",
                product.Price);
            defaultVariant.ChangeStock(0);
        }

        await ReplaceCategoriesAsync(product, createDto.CategoryIds, cancellationToken);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return created is null
            ? _mapper.Map<ProductDto>(product)
            : _mapper.Map<ProductDto>(created);
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateProductDto updateDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        if (string.IsNullOrWhiteSpace(updateDto.Name))
            throw new ArgumentException("Product name is required.", nameof(updateDto.Name));

        if (updateDto.Price < 0)
            throw new ArgumentOutOfRangeException(nameof(updateDto.Price));

        var product = await GetExistingProductAsync(id, cancellationToken);

        product.Update(
            updateDto.Name.Trim(),
            product.Slug,
            updateDto.Description,
            updateDto.Price);

        product.SetShortDescription(updateDto.ShortDescription);
        product.SetCurrency(updateDto.Currency);
        product.SetComparePrice(updateDto.ComparePrice);

        if (updateDto.DiscountPercentage.HasValue)
            product.ApplyDiscount(updateDto.DiscountPercentage.Value);
        else
            product.RemoveDiscount();

        product.SetActive(updateDto.IsActive);
        product.SetFeatured(updateDto.IsFeatured);

        if (updateDto.IsPublished)
            product.Publish();
        else
            product.Unpublish();

        product.SetBrand(updateDto.BrandId);
        await ReplaceCategoriesAsync(product, updateDto.CategoryIds, cancellationToken);

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStockAsync(Guid id, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var product = await GetExistingProductAsync(id, cancellationToken);

        if (product.Variants.Count == 0)
        {
            var variant = product.AddVariant($"{product.Sku}-DEFAULT", product.Price);
            variant.ChangeStock(quantity);
        }
        else
        {
            product.Variants.First().ChangeStock(quantity);
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var product = await GetExistingProductAsync(id, cancellationToken);
        product.SetActive(isActive);
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetFeaturedAsync(Guid id, bool isFeatured, CancellationToken cancellationToken = default)
    {
        var product = await GetExistingProductAsync(id, cancellationToken);
        product.SetFeatured(isFeatured);
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await GetExistingProductAsync(id, cancellationToken);
        _productRepository.Delete(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplaceCategoriesAsync(
        Product product,
        IEnumerable<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        var requestedIds = (categoryIds ?? Enumerable.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToHashSet();

        var validIds = new HashSet<Guid>();

        foreach (var categoryId in requestedIds)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category is not null)
                validIds.Add(category.Id);
        }

        foreach (var existing in product.ProductCategories.ToList())
        {
            if (!validIds.Contains(existing.CategoryId))
                product.ProductCategories.Remove(existing);
        }

        var existingIds = product.ProductCategories
            .Select(x => x.CategoryId)
            .ToHashSet();

        foreach (var categoryId in validIds)
        {
            if (!existingIds.Contains(categoryId))
                product.ProductCategories.Add(new ProductCategory(product.Id, categoryId));
        }
    }

    private async Task<Product> GetExistingProductAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product ?? throw new KeyNotFoundException($"Product with id {id} was not found.");
    }

    private static void AddVariantAttribute(Product product, ProductVariant variant, string? value, string name, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var attribute = product.AddAttribute(name, code);
        var attributeValue = attribute.Values.FirstOrDefault(x =>
            string.Equals(x.Value, value.Trim(), StringComparison.OrdinalIgnoreCase));

        attributeValue ??= attribute.AddValue(value.Trim(), value.Trim());
        variant.AddAttributeValue(attributeValue);
    }

    private static string GenerateSku() => $"NX-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

    private static string GenerateSlug(string name) =>
        name.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("?", "")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(".", "-");
}
