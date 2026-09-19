
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Infrastructure.Persistence;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace NexaECommerce.Tests.Integration.Features.Orders;

[Collection(IntegrationCollection.Name)]
public sealed class WarehouseReadyToShipTests(
    CustomWebApplicationFactory factory)
{
    private const string TenantId = "default";

    [Fact]
    public async Task Ready_to_ship_requires_active_package()
    {
        var scenario =
            await CreateScenarioAsync(
                createPackage: false);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/ready-to-ship",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Ready_to_ship_requires_all_active_packages_to_be_packed()
    {
        var scenario =
            await CreateScenarioAsync(
                createPackage: true,
                packageStatus: PackageStatus.Packing);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/ready-to-ship",
                null,
                TestContext.Current.CancellationToken);

        response.StatusCode
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Ready_to_ship_marks_fulfillment_ready_to_ship()
    {
        var scenario =
            await CreateScenarioAsync(
                createPackage: true,
                packageStatus: PackageStatus.Packed);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var response =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/ready-to-ship",
                null,
                TestContext.Current.CancellationToken);

        if (response.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await response.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Ready-to-ship failed. " +
                $"Status: {(int)response.StatusCode} " +
                $"{response.StatusCode}. " +
                $"Body: {body}");
        }

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("status")
            .GetString()
            .ShouldBe("ReadyToShip");

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var fulfillment =
            await db.Fulfillments
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.FulfillmentId,
                    TestContext.Current.CancellationToken);

        fulfillment.Status
            .ShouldBe(
                FulfillmentStatus.ReadyToShip);

        fulfillment.ReadyToShipAt
            .ShouldNotBeNull();

        var package =
            await db.Packages
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.PackageId,
                    TestContext.Current.CancellationToken);

        package.Status
            .ShouldBe(
                PackageStatus.Packed);
    }

    [Fact]
    public async Task Ready_to_ship_is_idempotent()
    {
        var scenario =
            await CreateScenarioAsync(
                createPackage: true,
                packageStatus: PackageStatus.Packed);

        var client =
            factory.CreateAuthenticatedClient(
                scenario.UserId,
                PermissionClaims.All);

        var first =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/ready-to-ship",
                null,
                TestContext.Current.CancellationToken);

        first.StatusCode
            .ShouldBe(HttpStatusCode.OK);

        var second =
            await client.PostAsync(
                $"/api/fulfillment/orders/{scenario.OrderId}/ready-to-ship",
                null,
                TestContext.Current.CancellationToken);

        if (second.StatusCode !=
            HttpStatusCode.OK)
        {
            var body =
                await second.Content.ReadAsStringAsync(
                    TestContext.Current.CancellationToken);

            throw new Xunit.Sdk.XunitException(
                $"Second ready-to-ship request failed. " +
                $"Status: {(int)second.StatusCode} " +
                $"{second.StatusCode}. " +
                $"Body: {body}");
        }

        var json =
            await second.Content.ReadFromJsonAsync<JsonElement>(
                TestContext.Current.CancellationToken);

        json.GetProperty("status")
            .GetString()
            .ShouldBe("ReadyToShip");

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        var fulfillment =
            await db.Fulfillments
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.Id ==
                        scenario.FulfillmentId,
                    TestContext.Current.CancellationToken);

        fulfillment.Status
            .ShouldBe(
                FulfillmentStatus.ReadyToShip);

        var packageCount =
            await db.Packages
                .CountAsync(
                    x =>
                        x.OrderId ==
                        scenario.OrderId,
                    TestContext.Current.CancellationToken);

        packageCount
            .ShouldBe(1);
    }

    private async Task<ReadyToShipScenario> CreateScenarioAsync(
        bool createPackage,
        PackageStatus packageStatus = PackageStatus.Packed)
    {
        var userId =
            $"ready-{Guid.NewGuid():N}";

        var order =
            Order.Create(
                TenantId,
                userId,
                $"READY-{Guid.NewGuid():N}",
                $"ready-{Guid.NewGuid():N}",
                "IRR",
                100_000m,
                20_000m,
                0m,
                "Ready Test User",
                "09000000000",
                "Test Address",
                "Tehran",
                "1234567890");

        order.AddItem(
            Guid.NewGuid(),
            "READY-SKU-001",
            "Ready Test Product",
            100_000m,
            2);

        order.MarkPaid();
        order.StartProcessing();

        var fulfillment =
            Fulfillment.Create(
                order.Id,
                TenantId);

        fulfillment.AssignWarehouse(
            Guid.NewGuid());

        fulfillment.StartPicking();
        fulfillment.MarkPicked();
        fulfillment.StartPacking();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<OrdersDbContext>();

        await db.Database.EnsureCreatedAsync(
            TestContext.Current.CancellationToken);

        await db.Orders.AddAsync(
            order,
            TestContext.Current.CancellationToken);

        await db.Fulfillments.AddAsync(
            fulfillment,
            TestContext.Current.CancellationToken);

        await db.SaveChangesAsync(
            TestContext.Current.CancellationToken);

        var packageId =
            Guid.Empty;

        if (createPackage)
        {
            var package =
                Package.Create(
                    TenantId,
                    order.Id,
                    fulfillment.Id,
                    1,
                    DateTime.UtcNow);

            package.StartPacking(
                DateTime.UtcNow);

            if (packageStatus is
                PackageStatus.Packed or
                PackageStatus.LabelPrinted)
            {
                package.MarkPacked(
                    DateTime.UtcNow);
            }

            db.Packages.Add(
                package);

            await db.SaveChangesAsync(
                TestContext.Current.CancellationToken);

            if (package.Status != packageStatus)
            {
                throw new InvalidOperationException(
                    $"Test package could not reach expected status '{packageStatus}'. " +
                    $"Actual status: '{package.Status}'.");
            }

            if (packageStatus == PackageStatus.Packed)
            {
                fulfillment.MarkPacked();

                await db.SaveChangesAsync(
                    TestContext.Current.CancellationToken);
            }

            packageId =
                package.Id;
        }
        else
        {
            // For the no-package test the fulfillment must already
            // be Packed, otherwise the validation would fail earlier.
            fulfillment.MarkPacked();

            await db.SaveChangesAsync(
                TestContext.Current.CancellationToken);
        }

        return new ReadyToShipScenario(
            userId,
            order.Id,
            fulfillment.Id,
            packageId);
    }

    private sealed record ReadyToShipScenario(
        string UserId,
        Guid OrderId,
        Guid FulfillmentId,
        Guid PackageId);
}

