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

        Guid warehouseId;
        Guid locationId;
        int originalOnHand;
        int originalReserved;
        int originalIncoming;
        int originalDamaged;
        int originalReorderPoint;
        bool created = false;

        using (var scope =
               factory.Services.CreateScope())
        {
            var inventoryDb =
                scope.ServiceProvider
                    .GetRequiredService<InventoryDbContext>();

            var warehouse =
                await inventoryDb.Warehouses
                    .Where(
                        x =>
                            x.TenantId == tenantId &&
                            x.IsActive)
                    .OrderByDescending(x => x.IsDefault)
                    .ThenBy(x => x.Code)
                    .FirstOrDefaultAsync();

            warehouse.ShouldNotBeNull();

            var location =
                await inventoryDb.WarehouseLocations
                    .Where(
                        x =>
                            x.TenantId == tenantId &&
                            x.WarehouseId == warehouse!.Id &&
                            x.IsActive)
                    .OrderBy(x => x.Code)
                    .FirstOrDefaultAsync();

            location.ShouldNotBeNull();

            warehouseId = warehouse!.Id;
            locationId = location!.Id;

            var stock =
                await inventoryDb.WarehouseStocks
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.WarehouseId == warehouseId &&
                            x.LocationId == locationId &&
                            x.ProductVariantId == info.VariantId);

            if (stock is null)
            {
                stock =
                    NexaEcommerce.Modules.Inventory.Domain.Entities
                        .WarehouseStock.Create(
                            tenantId,
                            warehouseId,
                            locationId,
                            info.VariantId,
                            onHandQuantity: testQuantity);

                await inventoryDb.WarehouseStocks.AddAsync(stock);
                created = true;

                originalOnHand = 0;
                originalReserved = 0;
                originalIncoming = 0;
                originalDamaged = 0;
                originalReorderPoint = 0;
            }
            else
            {
                originalOnHand = stock.OnHandQuantity;
                originalReserved = stock.ReservedQuantity;
                originalIncoming = stock.IncomingQuantity;
                originalDamaged = stock.DamagedQuantity;
                originalReorderPoint = stock.ReorderPoint;

                if (stock.ReservedQuantity > 0)
                {
                    stock.Release(stock.ReservedQuantity);
                }

                if (stock.OnHandQuantity > testQuantity)
                {
                    stock.RemoveOnHand(
                        stock.OnHandQuantity - testQuantity);
                }
                else if (stock.OnHandQuantity < testQuantity)
                {
                    stock.AddOnHand(
                        testQuantity - stock.OnHandQuantity);
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

            using var json =
                JsonDocument.Parse(body);

            var product =
                json.RootElement
                    .GetProperty("items")
                    .EnumerateArray()
                    .FirstOrDefault(
                        x =>
                            x.GetProperty("id")
                                .GetGuid() ==
                            info.ProductId);

            product.ValueKind
                .ShouldNotBe(JsonValueKind.Undefined);

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
                    .GetRequiredService<InventoryDbContext>();

            var stock =
                await inventoryDb.WarehouseStocks
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.WarehouseId == warehouseId &&
                            x.LocationId == locationId &&
                            x.ProductVariantId == info.VariantId);

            if (stock is not null)
            {
                if (created)
                {
                    inventoryDb.WarehouseStocks.Remove(stock);
                }
                else
                {
                    stock.SetQuantities(
                        originalOnHand,
                        originalReserved,
                        originalIncoming,
                        originalDamaged);

                    stock.SetReorderPoint(
                        originalReorderPoint);
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

