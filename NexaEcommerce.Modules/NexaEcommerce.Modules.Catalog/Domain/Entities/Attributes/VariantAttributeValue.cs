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
        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        if (attributeValueId == Guid.Empty)
        {
            throw new ArgumentException(
                "Attribute value id is required.",
                nameof(attributeValueId));
        }

        ProductVariantId = productVariantId;
        AttributeValueId = attributeValueId;
    }

    public VariantAttributeValue(
        Guid productVariantId,
        AttributeValue attributeValue)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        ArgumentNullException.ThrowIfNull(
            attributeValue);

        if (attributeValue.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "Attribute value id is required.",
                nameof(attributeValue));
        }

        ProductVariantId = productVariantId;
        AttributeValueId = attributeValue.Id;

        /*
         * Keep the navigation populated when the relationship is
         * created through the aggregate.
         *
         * This is important during Product update because the
         * product attribute synchronization logic uses the
         * AttributeValue -> ProductAttribute navigation to know
         * whether an attribute is still used by a Variant.
         */
        AttributeValue = attributeValue;
    }
}