using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Products;

[Collection(IntegrationCollection.Name)]
public sealed class ProductEndpointsTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Product_list_uses_inventory_available_stock()
    {
        using var client =
            factory.CreateClient();

        var info =
            await GetExistingProductAndVariantAsync();

        const string tenantId = "default";
        const int testQuantity = 1;

        int? originalAvailableQuantity = null;

        using (var scope =
               factory.Services.CreateScope())
        {
            var inventoryDb =
                scope.ServiceProvider
                    .GetRequiredService<
                        InventoryDbContext>();

            var stock =
                await inventoryDb.StockItems
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.ProductVariantId ==
                            info.VariantId);

            if (stock is null)
            {
                stock =
                    NexaEcommerce.Modules.Inventory.Domain.Entities
                        .StockItem.Create(
                            tenantId,
                            info.VariantId,
                            testQuantity);

                await inventoryDb.StockItems
                    .AddAsync(stock);
            }
            else
            {
                originalAvailableQuantity =
                    stock.AvailableQuantity;

                if (stock.ReservedQuantity > 0)
                {
                    stock.Release(
                        stock.ReservedQuantity);
                }

                if (stock.AvailableQuantity >
                    testQuantity)
                {
                    stock.Remove(
                        stock.AvailableQuantity -
                        testQuantity);
                }
                else if (
                    stock.AvailableQuantity <
                    testQuantity)
                {
                    stock.Add(
                        testQuantity -
                        stock.AvailableQuantity);
                }
            }

            await inventoryDb.SaveChangesAsync();
        }

        try
        {
            var response =
                await client.GetAsync(
                    "/api/products/?page=1&pageSize=100");

            response.StatusCode
                .ShouldBe(HttpStatusCode.OK);

            var body =
                await response.Content
                    .ReadAsStringAsync();

            body
                .ShouldNotBeNullOrWhiteSpace();

            using var json =
                JsonDocument.Parse(body);

            var items =
                json.RootElement
                    .GetProperty("items");

            var product =
                items
                    .EnumerateArray()
                    .FirstOrDefault(
                        x =>
                            x.GetProperty("id")
                                .GetGuid() ==
                            info.ProductId);

            product.ValueKind
                .ShouldNotBe(
                    JsonValueKind.Undefined);

            product
                .GetProperty("stockQuantity")
                .GetInt32()
                .ShouldBe(testQuantity);

            product
                .GetProperty("isInStock")
                .GetBoolean()
                .ShouldBeTrue();
        }
        finally
        {
            using var scope =
                factory.Services.CreateScope();

            var inventoryDb =
                scope.ServiceProvider
                    .GetRequiredService<
                        InventoryDbContext>();

            var stock =
                await inventoryDb.StockItems
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.ProductVariantId ==
                            info.VariantId);

            if (stock is not null)
            {
                if (stock.ReservedQuantity > 0)
                {
                    stock.Release(
                        stock.ReservedQuantity);
                }

                if (originalAvailableQuantity is null)
                {
                    inventoryDb.StockItems
                        .Remove(stock);
                }
                else
                {
                    if (stock.AvailableQuantity >
                        originalAvailableQuantity.Value)
                    {
                        stock.Remove(
                            stock.AvailableQuantity -
                            originalAvailableQuantity.Value);
                    }
                    else if (
                        stock.AvailableQuantity <
                        originalAvailableQuantity.Value)
                    {
                        stock.Add(
                            originalAvailableQuantity.Value -
                            stock.AvailableQuantity);
                    }
                }

                await inventoryDb.SaveChangesAsync();
            }
        }
    }

private async Task<ProductInfo>
    GetExistingProductAndVariantAsync()
    {
        using var scope =
            factory.Services.CreateScope();

        var catalogDb =
            scope.ServiceProvider
                .GetRequiredService<CatalogDbContext>();

        var result =
            await catalogDb.ProductVariants
                .AsNoTracking()
                .Where(
                    x =>
                        x.IsActive &&
                        !x.IsDeleted &&
                        x.Product.IsActive &&
                        x.Product.IsPublished &&
                        !x.Product.IsDeleted &&
                        x.Product.Variants.Count(
                            v =>
                                v.IsActive &&
                                !v.IsDeleted) == 1)
                .OrderBy(
                    x => x.ProductId)
                .Select(
                    x => new ProductInfo(
                        x.ProductId,
                        x.Id))
                .FirstOrDefaultAsync();

        result
            .ShouldNotBeNull();

        return result!;
    }

    private sealed record ProductInfo(
        Guid ProductId,
        Guid VariantId);
}

