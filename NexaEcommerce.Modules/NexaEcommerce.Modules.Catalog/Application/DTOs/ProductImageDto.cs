using System.Text.Json.Serialization;

namespace NexaEcommerce.Modules.Catalog.Application.DTOs;

public sealed class ProductImageDto
{
    public Guid Id { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }

    [JsonPropertyName("isMain")]
    public bool IsPrimary { get; set; }
}
