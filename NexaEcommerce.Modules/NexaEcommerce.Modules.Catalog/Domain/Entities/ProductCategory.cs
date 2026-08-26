using NexaEcommerce.Modules.Catalog.Domain.Entities;

public class ProductCategory
{
    public Guid ProductId { get; private set; }
    public Guid CategoryId { get; private set; }

    public Product Product { get; private set; } = null!;
    public Category Category { get; private set; } = null!;

    private ProductCategory()
    {
    }

    public ProductCategory(
        Guid productId,
        Guid categoryId)
    {
        ProductId = productId;
        CategoryId = categoryId;
    }
}