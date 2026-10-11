
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.Modules.Catalog.Infrastructure;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public interface ICategoryAttributeManagementService
{
    Task<IReadOnlyList<CategoryAttributeConfigurationDto>>
        GetForCategoryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryAttributeConfigurationDto>>
        ReplaceForCategoryAsync(
            Guid categoryId,
            ReplaceCategoryAttributesDto request,
            CancellationToken cancellationToken = default);
}

public sealed class CategoryAttributeManagementService
    : ICategoryAttributeManagementService
{
    private readonly CatalogDbContext _context;

    public CategoryAttributeManagementService(
        CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryAttributeConfigurationDto>>
        GetForCategoryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
    {
        await EnsureCategoryExistsAsync(
            categoryId,
            cancellationToken);

        var links = await _context.CategoryAttributes
            .AsNoTracking()
            .Include(x => x.CatalogAttribute)
                .ThenInclude(x => x.Values)
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.CatalogAttribute.Name)
            .ToListAsync(cancellationToken);

        return links.Select(Map).ToArray();
    }

    public async Task<IReadOnlyList<CategoryAttributeConfigurationDto>>
        ReplaceForCategoryAsync(
            Guid categoryId,
            ReplaceCategoryAttributesDto request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureCategoryExistsAsync(
            categoryId,
            cancellationToken);

        var requested = request.Attributes ??
            throw new ArgumentException(
                "Attributes cannot be null.",
                nameof(request.Attributes));

        var duplicateIds = requested
            .GroupBy(x => x.CatalogAttributeId)
            .Where(x => x.Key == Guid.Empty || x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();

        if (duplicateIds.Length > 0)
        {
            throw new ArgumentException(
                "Attribute IDs must be valid and unique.");
        }

        foreach (var item in requested)
        {
            if (item.DisplayOrder < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(item.DisplayOrder),
                    "Display order cannot be negative.");
            }

            ValidateRole(item.Role);
        }

        var requestedIds = requested
            .Select(x => x.CatalogAttributeId)
            .ToArray();

        var catalogAttributes = requestedIds.Length == 0
            ? new Dictionary<Guid, CatalogAttribute>()
            : await _context.CatalogAttributes
                .Where(x =>
                    requestedIds.Contains(x.Id) &&
                    x.IsActive)
                .ToDictionaryAsync(
                    x => x.Id,
                    cancellationToken);

        if (catalogAttributes.Count != requestedIds.Length)
        {
            throw new ArgumentException(
                "One or more selected catalog attributes do not exist or are inactive.");
        }

        var existing = await _context.CategoryAttributes
            .Where(x => x.CategoryId == categoryId)
            .ToListAsync(cancellationToken);

        var existingByAttributeId = existing
            .ToDictionary(x => x.CatalogAttributeId);

        foreach (var item in requested)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (existingByAttributeId.TryGetValue(
                    item.CatalogAttributeId,
                    out var link))
            {
                link.Update(
                    item.Role,
                    item.IsRequired,
                    item.DisplayOrder,
                    item.IsActive);
            }
            else
            {
                _context.CategoryAttributes.Add(
                    new CategoryAttribute(
                        categoryId,
                        item.CatalogAttributeId,
                        item.Role,
                        item.IsRequired,
                        item.DisplayOrder,
                        item.IsActive));
            }
        }

        var requestedIdSet = requestedIds.ToHashSet();

        // Preserve old links instead of physically deleting them.
        // Their unique database keys remain stable, and reactivating
        // a previously configured attribute will not create a duplicate.
        foreach (var link in existing)
        {
            if (!requestedIdSet.Contains(link.CatalogAttributeId) &&
                link.IsActive)
            {
                link.Update(
                    link.Role,
                    link.IsRequired,
                    link.DisplayOrder,
                    false);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetForCategoryAsync(
            categoryId,
            cancellationToken);
    }

    private async Task EnsureCategoryExistsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty ||
            !await _context.Categories.AnyAsync(
                x => x.Id == categoryId,
                cancellationToken))
        {
            throw new KeyNotFoundException(
                $"Category '{categoryId}' was not found.");
        }
    }

    private static void ValidateRole(AttributeRole role)
    {
        const AttributeRole allowed =
            AttributeRole.Filterable |
            AttributeRole.Descriptive |
            AttributeRole.VariantDefining;

        if (role == AttributeRole.None ||
            ((int)role & ~(int)allowed) != 0)
        {
            throw new ArgumentException(
                $"Invalid attribute role: {(int)role}.");
        }
    }

    private static CategoryAttributeConfigurationDto Map(
        CategoryAttribute link)
    {
        var attribute = link.CatalogAttribute;

        return new CategoryAttributeConfigurationDto(
            link.Id,
            link.CategoryId,
            link.CatalogAttributeId,
            attribute.Name,
            attribute.Code,
            attribute.DataType,
            attribute.DisplayType,
            link.Role,
            link.IsRequired,
            link.DisplayOrder,
            link.IsActive,
            attribute.Values
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Value)
                .Select(x => new CategoryAttributeValueOptionDto(
                    x.Id,
                    x.Value,
                    x.DisplayValue,
                    x.ColorHex,
                    x.DisplayOrder,
                    x.IsActive))
                .ToArray());
    }
}
