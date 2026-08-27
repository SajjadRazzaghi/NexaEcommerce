namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class CreateProductDto
{
    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? Currency { get; set; } = "IRR";

    public string? Sku { get; set; }

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public Guid? BrandId { get; set; }

    public Guid? ManufacturerId { get; set; }

    public List<Guid> CategoryIds { get; set; } = new();

    public List<string> Images { get; set; } = new();

    public List<CreateProductVariantDto> Variants { get; set; } = new();
}