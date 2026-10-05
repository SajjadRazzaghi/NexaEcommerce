namespace NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;

/// <summary>
/// Defines how an attribute is used inside a product.
/// A single attribute can have multiple roles simultaneously.
/// </summary>
[Flags]
public enum AttributeRole
{
    None = 0,

    /// <summary>
    /// Appears in catalog filtering/faceting.
    /// </summary>
    Filterable = 1,

    /// <summary>
    /// Appears as descriptive product specification.
    /// </summary>
    Descriptive = 2,

    /// <summary>
    /// Participates in the product variant/SKU combination.
    /// </summary>
    VariantDefining = 4
}