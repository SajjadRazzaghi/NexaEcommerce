using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;
using NexaEcommerce.Modules.Catalog.Domain.Models;
using NexaEcommerce.Modules.Catalog.Infrastructure;


namespace NexaEcommerce.Modules.Catalog.Application.Services;

public sealed class CatalogDataIntegrityService
    : ICatalogDataIntegrityService
{
    private readonly CatalogDbContext _context;

    public CatalogDataIntegrityService(
        CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<CatalogDataIntegrityReport> AuditAsync(
        CancellationToken cancellationToken = default)
    {
        var products =
            await _context.Products
                .AsNoTracking()
                .Include(x => x.ProductCategories)
                .ThenInclude(x => x.Category)
                .Include(x => x.Attributes)
                .ThenInclude(x => x.CatalogAttribute)
                .Include(x => x.Attributes)
                .ThenInclude(x => x.Values)
                .ThenInclude(x => x.CatalogAttributeValue)
                .Include(x => x.Variants)
                .ThenInclude(x => x.AttributeValues)
                .ThenInclude(x => x.AttributeValue)
                .ThenInclude(x => x.ProductAttribute)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

        var productAttributes =
            products
                .SelectMany(x => x.Attributes)
                .Where(x => !x.IsDeleted)
                .ToList();

        var attributeValues =
            productAttributes
                .SelectMany(x => x.Values)
                .Where(x => !x.IsDeleted)
                .ToList();

        var variants =
            products
                .SelectMany(x => x.Variants)
                .Where(x => !x.IsDeleted)
                .ToList();

        var productsWithoutCatalogAttribute =
            productAttributes.Count(
                x => !x.CatalogAttributeId.HasValue);

        var valuesWithoutCatalogValue =
            attributeValues.Count(
                x => !x.CatalogAttributeValueId.HasValue);

        var variantsWithoutCombinationKey =
            variants.Count(
                x => string.IsNullOrWhiteSpace(
                    x.CombinationKey));

        var invalidVariantMappings = 0;

        var variantsWithoutRequiredValues = 0;

        foreach (var product in products)
        {
            var variantDefiningAttributes =
                product.Attributes
                    .Where(
                        x =>
                            !x.IsDeleted &&
                            x.Role.HasFlag(
                                AttributeRole.VariantDefining))
                    .ToList();

            var requiredAttributes =
                product.Attributes
                    .Where(
                        x =>
                            !x.IsDeleted &&
                            x.IsRequired)
                    .ToList();

            foreach (var attribute in requiredAttributes)
            {
                if (!attribute.Values.Any(
                        x => !x.IsDeleted))
                {
                    continue;
                }
            }

            foreach (
                var variant in product.Variants
                    .Where(x =>
                        !x.IsDeleted &&
                        x.IsActive))
            {
                var values =
                    variant.AttributeValues
                        .Where(x =>
                            !x.IsDeleted &&
                            x.AttributeValue is not null)
                        .Select(x => x.AttributeValue!)
                        .ToList();

                var usedAttributeIds =
                    new HashSet<Guid>();

                foreach (var value in values)
                {
                    var attribute =
                        value.ProductAttribute;

                    if (attribute is null)
                    {
                        invalidVariantMappings++;
                        continue;
                    }

                    if (!attribute.Role.HasFlag(
                            AttributeRole.VariantDefining))
                    {
                        invalidVariantMappings++;
                    }

                    if (!usedAttributeIds.Add(
                            attribute.Id))
                    {
                        invalidVariantMappings++;
                    }
                }

                foreach (
                    var variantAttribute in
                    variantDefiningAttributes)
                {
                    var count =
                        values.Count(
                            x =>
                                x.ProductAttributeId ==
                                variantAttribute.Id);

                    if (count != 1)
                    {
                        invalidVariantMappings++;
                    }
                }

                foreach (
                    var requiredAttribute in
                    variantDefiningAttributes
                        .Where(x => x.IsRequired))
                {
                    var hasValue =
                        values.Any(
                            x =>
                                x.ProductAttributeId ==
                                requiredAttribute.Id);

                    if (!hasValue)
                    {
                        variantsWithoutRequiredValues++;
                    }
                }
            }
        }

        var duplicateVariantSkus =
            variants
                .Where(x => !string.IsNullOrWhiteSpace(x.Sku))
                .GroupBy(
                    x => x.Sku,
                    StringComparer.OrdinalIgnoreCase)
                .Sum(
                    x => Math.Max(
                        0,
                        x.Count() - 1));

        var duplicateVariantCombinations = 0;

        foreach (var product in products)
        {
            var duplicates =
                product.Variants
                    .Where(
                        x =>
                            !x.IsDeleted &&
                            x.IsActive &&
                            !string.IsNullOrWhiteSpace(
                                x.CombinationKey))
                    .GroupBy(
                        x => x.CombinationKey!,
                        StringComparer.OrdinalIgnoreCase)
                    .Sum(
                        x => Math.Max(
                            0,
                            x.Count() - 1));

            duplicateVariantCombinations +=
                duplicates;
        }

        var requiredAttributesWithoutValues =
            productAttributes
                .Count(
                    x =>
                        x.IsRequired &&
                        !x.Values.Any(
                            v => !v.IsDeleted));

        return new CatalogDataIntegrityReport
        {
            ProductCount =
                products.Count,

            ProductAttributeCount =
                productAttributes.Count,

            ProductAttributesWithoutCatalogAttribute =
                productsWithoutCatalogAttribute,

            AttributeValueCount =
                attributeValues.Count,

            AttributeValuesWithoutCatalogValue =
                valuesWithoutCatalogValue,

            VariantCount =
                variants.Count,

            VariantsWithoutCombinationKey =
                variantsWithoutCombinationKey,

            InvalidVariantAttributeMappings =
                invalidVariantMappings,

            DuplicateVariantSkus =
                duplicateVariantSkus,

            DuplicateVariantCombinations =
                duplicateVariantCombinations,

            RequiredAttributesWithoutValues =
                requiredAttributesWithoutValues,

            VariantsWithoutRequiredAttributeValues =
                variantsWithoutRequiredValues
        };
    }

    public async Task<CatalogDataIntegrityReport> BackfillAsync(
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var catalogAttributes =
                await _context.CatalogAttributes
                    .AsTracking()
                    .ToListAsync(cancellationToken);

            var catalogAttributeValues =
                await _context.CatalogAttributeValues
                    .AsTracking()
                    .ToListAsync(cancellationToken);

            var products =
                await _context.Products
                    .AsTracking()
                    .Include(x => x.ProductCategories)
                    .ThenInclude(x => x.Category)
                    .ThenInclude(x => x.Attributes)
                    .Include(x => x.Attributes)
                    .ThenInclude(x => x.CatalogAttribute)
                    .Include(x => x.Attributes)
                    .ThenInclude(x => x.Values)
                    .Include(x => x.Variants)
                    .ThenInclude(x => x.AttributeValues)
                    .ThenInclude(x => x.AttributeValue)
                    .ThenInclude(x => x.ProductAttribute)
                    .AsSplitQuery()
                    .ToListAsync(cancellationToken);

            // -------------------------------------------------
            // ProductAttribute -> CatalogAttribute
            // -------------------------------------------------

            foreach (var product in products)
            {
                foreach (var attribute in product.Attributes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (attribute.IsDeleted)
                    {
                        continue;
                    }

                    CatalogAttribute? catalogAttribute = null;

                    if (attribute.CatalogAttributeId.HasValue)
                    {
                        catalogAttribute =
                            catalogAttributes.FirstOrDefault(
                                x =>
                                    x.Id ==
                                    attribute.CatalogAttributeId.Value);
                    }

                    if (catalogAttribute is null)
                    {
                        var matches =
                            catalogAttributes
                                .Where(
                                    x =>
                                        !string.IsNullOrWhiteSpace(
                                            attribute.Code) &&
                                        string.Equals(
                                            x.Code,
                                            attribute.Code,
                                            StringComparison.OrdinalIgnoreCase))
                                .ToList();

                        if (matches.Count == 1)
                        {
                            catalogAttribute =
                                matches[0];

                            attribute.SetCatalogAttribute(
                                catalogAttribute.Id);
                        }
                    }

                    if (catalogAttribute is null)
                    {
                        continue;
                    }

                    // -------------------------------------------------
                    // Role from CategoryAttribute
                    // -------------------------------------------------

                    var categoryAttributeConfigurations =
    product.ProductCategories
        .Where(
            x =>
                x.Category is not null &&
                !x.Category.IsDeleted)
        .SelectMany(
            x =>
                x.Category!.Attributes)
        .Where(
            x =>
                !x.IsDeleted &&
                x.CatalogAttributeId ==
                catalogAttribute.Id)
        .ToList();

                    if (categoryAttributeConfigurations.Count > 0)
                    {
                        var role =
                            AttributeRole.None;

                        foreach (
                            var configuration in
                            categoryAttributeConfigurations)
                        {
                            role |=
                                configuration.Role;
                        }

                        if (role == AttributeRole.None)
                        {
                            role =
                                AttributeRole.Descriptive;
                        }

                        attribute.SetRole(role);

                        attribute.SetRequired(
                            categoryAttributeConfigurations.Any(
                                x => x.IsRequired));
                    }
                    else
                    {
                        var role =
                            AttributeRole.Descriptive;

                        if (catalogAttribute.IsFilterable)
                        {
                            role |=
                                AttributeRole.Filterable;
                        }

                        if (catalogAttribute.IsVariantAttribute)
                        {
                            role |=
                                AttributeRole.VariantDefining;
                        }

                        attribute.SetRole(role);

                        if (catalogAttribute.IsRequired)
                        {
                            attribute.SetRequired(
                                true);
                        }
                    }

                    // -------------------------------------------------
                    // AttributeValue -> CatalogAttributeValue
                    // -------------------------------------------------

                    foreach (var value in attribute.Values)
                    {
                        if (value.IsDeleted)
                        {
                            continue;
                        }

                        if (value.CatalogAttributeValueId.HasValue)
                        {
                            continue;
                        }

                        var valueMatches =
                            catalogAttributeValues
                                .Where(
                                    x =>
                                        x.CatalogAttributeId ==
                                        catalogAttribute.Id &&
                                        string.Equals(
                                            x.Value?.Trim(),
                                            value.Value?.Trim(),
                                            StringComparison.OrdinalIgnoreCase))
                                .ToList();

                        if (valueMatches.Count == 1)
                        {
                            value.SetCatalogValue(
                                valueMatches[0].Id);
                        }
                    }
                }
            }

            // -------------------------------------------------
            // Variant CombinationKey
            // -------------------------------------------------

            foreach (var product in products)
            {
                foreach (
                    var variant in
                    product.Variants.Where(
                        x => !x.IsDeleted))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var values =
                        variant.AttributeValues
                            .Where(
                                x =>
                                    !x.IsDeleted &&
                                    x.AttributeValue is not null)
                            .Select(
                                x => x.AttributeValue!)
                            .ToList();

                    var variantDefiningAttributes =
                        product.Attributes
                            .Where(
                                x =>
                                    !x.IsDeleted &&
                                    x.Role.HasFlag(
                                        AttributeRole.VariantDefining))
                            .ToList();

                    // Simple product.
                    if (variantDefiningAttributes.Count == 0)
                    {
                        if (values.Count == 0)
                        {
                            variant.SetCombinationKey(
                                string.Empty);
                        }

                        continue;
                    }

                    var valid =
                        true;

                    foreach (
                        var definingAttribute in
                        variantDefiningAttributes)
                    {
                        var count =
                            values.Count(
                                x =>
                                    x.ProductAttributeId ==
                                    definingAttribute.Id);

                        if (count != 1)
                        {
                            valid = false;
                            break;
                        }
                    }

                    foreach (var value in values)
                    {
                        var attribute =
                            value.ProductAttribute;

                        if (attribute is null ||
                            !attribute.Role.HasFlag(
                                AttributeRole.VariantDefining))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (!valid)
                    {
                        continue;
                    }

                    var combinationKey =
                        BuildCombinationKey(values);

                    variant.SetCombinationKey(
                        combinationKey);
                }
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return await AuditAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private static string BuildCombinationKey(
        IEnumerable<AttributeValue> values)
    {
        return string.Join(
            "|",
            values
                .Select(
                    value =>
                    {
                        var attribute =
                            value.ProductAttribute;

                        return new
                        {
                            AttributeCode =
                                attribute?.Code?.Trim()
                                    .ToLowerInvariant()
                                    ?? string.Empty,

                            AttributeId =
                                value.ProductAttributeId,

                            ValueId =
                                value.Id
                        };
                    })
                .OrderBy(x => x.AttributeCode)
                .ThenBy(x => x.AttributeId)
                .ThenBy(x => x.ValueId)
                .Select(
                    x =>
                        $"{x.AttributeCode}:{x.ValueId}"));
    }
}