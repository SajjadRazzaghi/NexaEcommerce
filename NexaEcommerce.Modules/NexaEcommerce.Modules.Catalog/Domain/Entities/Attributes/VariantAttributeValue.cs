using NexaEcommerce.SharedKernel.Domain;

namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

public class VariantAttributeValue : BaseEntity
{
    public Guid ProductVariantId { get; private set; }

    public Guid AttributeValueId { get; private set; }

    public ProductVariant ProductVariant { get; private set; } = null!;

    public AttributeValue AttributeValue { get; private set; } = null!;

    private VariantAttributeValue()
    {
    }

    public VariantAttributeValue(
        Guid productVariantId,
        Guid attributeValueId)
    {
        ProductVariantId = productVariantId;
        AttributeValueId = attributeValueId;
    }
}