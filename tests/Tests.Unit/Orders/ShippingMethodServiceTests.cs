using NSubstitute;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class ShippingMethodServiceTests
{
    [Fact]
    public async Task Quote_returns_server_side_price_for_active_method()
    {
        var method =
            ShippingMethod.Create(
                "tenant-1",
                "standard",
                "Standard",
                "Carrier",
                65000);

        var repository =
            Substitute.For<
                IShippingMethodRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        repository
            .GetByIdAsync(
                "tenant-1",
                method.Id,
                Arg.Any<CancellationToken>())
            .Returns(method);

        var service =
            new ShippingMethodService(
                repository,
                unitOfWork);

        var quote =
            await service.QuoteAsync(
                "tenant-1",
                method.Id);

        quote.Price
            .ShouldBe(65000);

        quote.Code
            .ShouldBe("standard");
    }

    [Fact]
    public async Task Quote_rejects_inactive_method()
    {
        var method =
            ShippingMethod.Create(
                "tenant-1",
                "standard",
                "Standard",
                "Carrier",
                65000);

        method.Deactivate();

        var repository =
            Substitute.For<
                IShippingMethodRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        repository
            .GetByIdAsync(
                "tenant-1",
                method.Id,
                Arg.Any<CancellationToken>())
            .Returns(method);

        var service =
            new ShippingMethodService(
                repository,
                unitOfWork);

        await Should.ThrowAsync<
            InvalidOperationException>(
            () =>
                service.QuoteAsync(
                    "tenant-1",
                    method.Id));
    }

    [Fact]
    public async Task Create_rejects_duplicate_code()
    {
        var existing =
            ShippingMethod.Create(
                "tenant-1",
                "standard",
                "Standard",
                "Carrier",
                50000);

        var repository =
            Substitute.For<
                IShippingMethodRepository>();

        var unitOfWork =
            Substitute.For<IOrderUnitOfWork>();

        repository
            .GetByCodeAsync(
                "tenant-1",
                "STANDARD",
                Arg.Any<CancellationToken>())
            .Returns(existing);

        var service =
            new ShippingMethodService(
                repository,
                unitOfWork);

        await Should.ThrowAsync<
            InvalidOperationException>(
            () =>
                service.CreateAsync(
                    "tenant-1",
                    new CreateShippingMethodRequest(
                        "standard",
                        "Another",
                        "Carrier",
                        50000)));
    }
}
