using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.Modules.Inventory.Infrastructure.Persistence;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Inventory;

[Collection(IntegrationCollection.Name)]
public sealed class InventoryServiceTests(
    CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Reserve_reduces_available_stock_and_increases_reserved_stock()
    {
        using var scope =
            factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IInventoryService>();

        var variantId =
            await GetExistingVariantIdAsync(scope);

        await EnsureStockAsync(
            scope,
            variantId,
            10);

        var reservationKey =
            $"test-{Guid.NewGuid():N}";

        var result =
            await service.ReserveAsync(
                "default",
                variantId,
                3,
                reservationKey,
                TimeSpan.FromMinutes(10));

        result.Quantity
            .ShouldBe(3);

        result.Status
            .ShouldBe("Active");

        var stock =
            await service.GetStockAsync(
                "default",
                variantId);

        stock.ShouldNotBeNull();

        stock!.AvailableQuantity
            .ShouldBe(7);

        stock.ReservedQuantity
            .ShouldBe(3);
    }

    [Fact]
    public async Task Same_reservation_key_is_idempotent()
    {
        using var scope =
            factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IInventoryService>();

        var variantId =
            await GetExistingVariantIdAsync(scope);

        await EnsureStockAsync(
            scope,
            variantId,
            10);

        var reservationKey =
            $"test-{Guid.NewGuid():N}";

        var first =
            await service.ReserveAsync(
                "default",
                variantId,
                2,
                reservationKey,
                TimeSpan.FromMinutes(10));

        var second =
            await service.ReserveAsync(
                "default",
                variantId,
                2,
                reservationKey,
                TimeSpan.FromMinutes(10));

        second.ReservationKey
            .ShouldBe(first.ReservationKey);

        var stock =
            await service.GetStockAsync(
                "default",
                variantId);

        stock.ShouldNotBeNull();

        stock!.ReservedQuantity
            .ShouldBe(2);
    }

    [Fact]
    public async Task Same_reservation_key_cannot_be_reused_for_different_quantity()
    {
        using var scope =
            factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IInventoryService>();

        var variantId =
            await GetExistingVariantIdAsync(scope);

        await EnsureStockAsync(
            scope,
            variantId,
            10);

        var reservationKey =
            $"test-{Guid.NewGuid():N}";

        await service.ReserveAsync(
            "default",
            variantId,
            2,
            reservationKey,
            TimeSpan.FromMinutes(10));

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    service.ReserveAsync(
                        "default",
                        variantId,
                        3,
                        reservationKey,
                        TimeSpan.FromMinutes(10)));

        exception.Message
            .ShouldContain(
                "already used");
    }

    [Fact]
    public async Task Release_returns_stock_to_available_quantity()
    {
        using var scope =
            factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IInventoryService>();

        var variantId =
            await GetExistingVariantIdAsync(scope);

        await EnsureStockAsync(
            scope,
            variantId,
            10);

        var reservationKey =
            $"test-{Guid.NewGuid():N}";

        await service.ReserveAsync(
            "default",
            variantId,
            4,
            reservationKey,
            TimeSpan.FromMinutes(10));

        var released =
            await service.ReleaseAsync(
                "default",
                reservationKey);

        released.Status
            .ShouldBe("Released");

        var stock =
            await service.GetStockAsync(
                "default",
                variantId);

        stock.ShouldNotBeNull();

        stock!.AvailableQuantity
            .ShouldBe(10);

        stock.ReservedQuantity
            .ShouldBe(0);
    }

    [Fact]
    public async Task Commit_removes_stock_from_reserved_quantity()
    {
        using var scope =
            factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IInventoryService>();

        var variantId =
            await GetExistingVariantIdAsync(scope);

        await EnsureStockAsync(
            scope,
            variantId,
            10);

        var reservationKey =
            $"test-{Guid.NewGuid():N}";

        await service.ReserveAsync(
            "default",
            variantId,
            4,
            reservationKey,
            TimeSpan.FromMinutes(10));

        var committed =
            await service.CommitAsync(
                "default",
                reservationKey);

        committed.Status
            .ShouldBe("Committed");

        var stock =
            await service.GetStockAsync(
                "default",
                variantId);

        stock.ShouldNotBeNull();

        stock!.AvailableQuantity
            .ShouldBe(6);

        stock.ReservedQuantity
            .ShouldBe(0);
    }

    [Fact]
    public async Task Committed_reservation_cannot_be_released()
    {
        using var scope =
            factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IInventoryService>();

        var variantId =
            await GetExistingVariantIdAsync(scope);

        await EnsureStockAsync(
            scope,
            variantId,
            10);

        var reservationKey =
            $"test-{Guid.NewGuid():N}";

        await service.ReserveAsync(
            "default",
            variantId,
            2,
            reservationKey,
            TimeSpan.FromMinutes(10));

        await service.CommitAsync(
            "default",
            reservationKey);

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    service.ReleaseAsync(
                        "default",
                        reservationKey));

        exception.Message
            .ShouldContain(
                "committed");
    }

    [Fact]
    public async Task Reserve_more_than_available_stock_fails()
    {
        using var scope =
            factory.Services.CreateScope();

        var service =
            scope.ServiceProvider
                .GetRequiredService<IInventoryService>();

        var variantId =
            await GetExistingVariantIdAsync(scope);

        await EnsureStockAsync(
            scope,
            variantId,
            3);

        var exception =
            await Should.ThrowAsync<InvalidOperationException>(
                () =>
                    service.ReserveAsync(
                        "default",
                        variantId,
                        4,
                        $"test-{Guid.NewGuid():N}",
                        TimeSpan.FromMinutes(10)));

        exception.Message
            .ShouldContain(
                "Insufficient available stock");
    }

    private async Task<Guid>
       GetExistingVariantIdAsync(
           IServiceScope scope)
    {
        var catalogDb =
            scope.ServiceProvider
                .GetRequiredService<
                    NexaEcommerce.Modules.Catalog.Infrastructure
                        .CatalogDbContext>();

        var variant =
            await catalogDb.ProductVariants
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.StockQuantity > 0 &&
                    x.Product.IsActive &&
                    x.Product.IsPublished &&
                    !x.Product.IsDeleted)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(
                    TestContext.Current.CancellationToken);

        variant.ShouldNotBe(Guid.Empty);

        var inventoryDb =
            scope.ServiceProvider
                .GetRequiredService<
                    NexaEcommerce.Modules.Inventory.Infrastructure.Persistence
                        .InventoryDbContext>();

        const string tenantId = "default";
        const int baselineQuantity = 10;

        var stock =
            await inventoryDb.StockItems
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.ProductVariantId == variant,
                    TestContext.Current.CancellationToken);

        if (stock is null)
        {
            stock =
                NexaEcommerce.Modules.Inventory.Domain.Entities.StockItem.Create(
                    tenantId,
                    variant,
                    baselineQuantity);

            await inventoryDb.StockItems.AddAsync(
                stock,
                TestContext.Current.CancellationToken);
        }
        else
        {
            /*
             * Every Inventory integration test must start from a clean
             * baseline. Previous tests may have reserved or consumed stock.
             */

            if (stock.ReservedQuantity > 0)
            {
                stock.Release(
                    stock.ReservedQuantity);
            }

            if (stock.AvailableQuantity < baselineQuantity)
            {
                stock.Add(
                    baselineQuantity -
                    stock.AvailableQuantity);
            }
            else if (stock.AvailableQuantity > baselineQuantity)
            {
                stock.Remove(
                    stock.AvailableQuantity -
                    baselineQuantity);
            }
        }

        /*
         * Remove reservations left by earlier tests for this variant.
         * At this point the StockItem has already been returned to the
         * baseline state.
         */
        var reservations =
            await inventoryDb.StockReservations
                .Where(
                    x =>
                        x.TenantId == tenantId &&
                        x.ProductVariantId == variant)
                .ToListAsync(
                    TestContext.Current.CancellationToken);

        if (reservations.Count > 0)
        {
            inventoryDb.StockReservations.RemoveRange(
                reservations);
        }

        await inventoryDb.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        return variant;
    }
    private static async Task EnsureStockAsync(
        IServiceScope scope,
        Guid productVariantId,
        int quantity)
    {
        var db =
            scope.ServiceProvider
                .GetRequiredService<InventoryDbContext>();

        var stock =
            await db.StockItems
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == "default" &&
                        x.ProductVariantId ==
                        productVariantId);

        if (stock is null)
        {
            await db.StockItems.AddAsync(
                NexaEcommerce.Modules.Inventory.Domain.Entities.StockItem
                    .Create(
                        "default",
                        productVariantId,
                        quantity));
        }
        else
        {
            var delta =
                quantity - stock.TotalQuantity;

            if (delta > 0)
                stock.Add(delta);
            else if (delta < 0 &&
                     -delta <= stock.AvailableQuantity)
                stock.Remove(-delta);
        }

        await db.SaveChangesAsync();
    }
}