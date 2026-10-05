namespace NexaEcommerce.Modules.Catalog.Domain.Models;

public sealed class CatalogDataIntegrityReport
{
    public int ProductCount { get; init; }

    public int ProductAttributeCount { get; init; }

    public int ProductAttributesWithoutCatalogAttribute { get; init; }

    public int AttributeValueCount { get; init; }

    public int AttributeValuesWithoutCatalogValue { get; init; }

    public int VariantCount { get; init; }

    public int VariantsWithoutCombinationKey { get; init; }

    public int InvalidVariantAttributeMappings { get; init; }

    public int DuplicateVariantSkus { get; init; }

    public int DuplicateVariantCombinations { get; init; }

    public int RequiredAttributesWithoutValues { get; init; }

    public int VariantsWithoutRequiredAttributeValues { get; init; }

    public bool IsHealthy =>
        ProductAttributesWithoutCatalogAttribute == 0 &&
        AttributeValuesWithoutCatalogValue == 0 &&
        VariantsWithoutCombinationKey == 0 &&
        InvalidVariantAttributeMappings == 0 &&
        DuplicateVariantSkus == 0 &&
        DuplicateVariantCombinations == 0 &&
        RequiredAttributesWithoutValues == 0 &&
        VariantsWithoutRequiredAttributeValues == 0;
}