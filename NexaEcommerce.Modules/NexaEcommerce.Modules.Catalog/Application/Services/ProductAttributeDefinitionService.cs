
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Application.DTOs;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.Modules.Catalog.Infrastructure;

namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class ProductAttributeDefinitionService
    : IProductAttributeDefinitionService
{
    private readonly CatalogDbContext _context;

    public ProductAttributeDefinitionService(
        CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProductAttributeDefinitionDto>>
        GetForCategoriesAsync(
            IReadOnlyCollection<Guid> categoryIds,
            Guid? productId = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(categoryIds);

        var normalizedCategoryIds = categoryIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var linkedAttributes = normalizedCategoryIds.Length == 0
            ? []
            : await _context.CategoryAttributes
                .AsNoTracking()
                .Where(x =>
                    normalizedCategoryIds.Contains(x.CategoryId) &&
                    x.IsActive &&
                    x.CatalogAttribute.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CatalogAttribute.Name)
                .ToListAsync(cancellationToken);

        var linksByAttributeId = linkedAttributes
            .GroupBy(x => x.CatalogAttributeId)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray());

        var requiredCatalogAttributeIds = new HashSet<Guid>(
            linksByAttributeId.Keys);

        // Preserve the definitions already used by the product.
        // This is important when editing older products whose
        // categories have not yet been configured.
        if (productId.HasValue && productId.Value != Guid.Empty)
        {
            var existingIds = await _context.ProductAttributes
                .AsNoTracking()
                .Where(x =>
                    x.ProductId == productId.Value &&
                    x.CatalogAttributeId.HasValue)
                .Select(x => x.CatalogAttributeId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            requiredCatalogAttributeIds.UnionWith(existingIds);
        }

        var useLegacyFallback =
            linkedAttributes.Count == 0 &&
            normalizedCategoryIds.Length > 0;

        List<CatalogAttribute> catalogAttributes;

        if (useLegacyFallback)
        {
            // Backward compatibility:
            // a category with no configured attributes uses the
            // previous global attribute list.
            catalogAttributes = await _context.CatalogAttributes
                .AsNoTracking()
                .Include(x => x.Values)
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }
        else if (requiredCatalogAttributeIds.Count == 0)
        {
            return Array.Empty<ProductAttributeDefinitionDto>();
        }
        else
        {
            catalogAttributes = await _context.CatalogAttributes
                .AsNoTracking()
                .Include(x => x.Values)
                .Where(x =>
                    requiredCatalogAttributeIds.Contains(x.Id) &&
                    (
                        x.IsActive ||
                        _context.ProductAttributes.Any(productAttribute =>
                            productAttribute.ProductId == productId &&
                            productAttribute.CatalogAttributeId == x.Id)
                    ))
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        var result = new List<ProductAttributeDefinitionDto>(
            catalogAttributes.Count);

        foreach (var attribute in catalogAttributes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AttributeRole role;
            bool isRequired;
            int displayOrder;

            if (linksByAttributeId.TryGetValue(
                    attribute.Id,
                    out var categoryLinks))
            {
                role = categoryLinks.Aggregate(
                    AttributeRole.None,
                    (current, link) => current | link.Role);

                isRequired = categoryLinks.Any(x => x.IsRequired);

                displayOrder = categoryLinks.Min(x => x.DisplayOrder);
            }
            else
            {
                // Legacy default when no category assignment exists.
                role = attribute.IsVariantAttribute
                    ? AttributeRole.VariantDefining
                    : AttributeRole.Descriptive;

                if (attribute.IsFilterable)
                {
                    role |= AttributeRole.Filterable;
                }

                isRequired = attribute.IsRequired;
                displayOrder = attribute.DisplayOrder;
            }

            result.Add(new ProductAttributeDefinitionDto
            {
                CatalogAttributeId = attribute.Id,
                Name = attribute.Name,
                Code = attribute.Code,
                DataType = attribute.DataType,
                DefaultRole = role,
                IsRequired = isRequired,
                IsFilterable = role.HasFlag(AttributeRole.Filterable),
                DisplayType = attribute.DisplayType,
                DisplayOrder = displayOrder,
                Values = attribute.Values
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.Value)
                    .Select(value => new ProductAttributeDefinitionValueDto
                    {
                        CatalogAttributeValueId = value.Id,
                        Value = value.Value,
                        DisplayValue = value.DisplayValue,
                        ColorHex = value.ColorHex,
                        DisplayOrder = value.DisplayOrder,
                        IsActive = value.IsActive
                    })
                    .ToList()
            });
        }

        return result
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToArray();
    }
}
