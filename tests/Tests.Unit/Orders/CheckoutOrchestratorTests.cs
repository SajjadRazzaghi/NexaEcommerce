using NexaECommerce.Server.Features.Orders;
using Shouldly;

namespace NexaECommerce.Tests.Unit.Orders;

public sealed class CheckoutOrchestratorTests
{
    [Fact]
    public void BuildReservationKey_is_stable_for_same_input()
    {
        var first =
        CheckoutOrchestrator.BuildReservationKey(
        "tenant-1",
        "user-1",
        "checkout-123",
        Guid.Parse(
        "11111111-1111-1111-1111-111111111111"));


    var second =
        CheckoutOrchestrator.BuildReservationKey(
            "tenant-1",
            "user-1",
            "checkout-123",
            Guid.Parse(
                "11111111-1111-1111-1111-111111111111"));

        first.ShouldBe(second);
    }

    [Fact]
    public void BuildReservationKey_changes_when_variant_changes()
    {
        var first =
            CheckoutOrchestrator.BuildReservationKey(
                "tenant-1",
                "user-1",
                "checkout-123",
                Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"));

        var second =
            CheckoutOrchestrator.BuildReservationKey(
                "tenant-1",
                "user-1",
                "checkout-123",
                Guid.Parse(
                    "22222222-2222-2222-2222-222222222222"));

        first.ShouldNotBe(second);
    }

    [Fact]
    public void BuildReservationKey_is_bounded_to_inventory_key_limit()
    {
        var longIdempotencyKey =
            new string(
                'x',
                128);

        var result =
            CheckoutOrchestrator.BuildReservationKey(
                "tenant-1",
                "user-1",
                longIdempotencyKey,
                Guid.Parse(
                    "11111111-1111-1111-1111-111111111111"));

        result.Length.ShouldBe(
            "checkout:".Length + 64);

        result.Length.ShouldBeLessThanOrEqualTo(
            128);
    }

    [Fact]
    public void BuildReservationKey_rejects_empty_variant()
    {
        Should.Throw<ArgumentException>(
            () =>
                CheckoutOrchestrator.BuildReservationKey(
                    "tenant-1",
                    "user-1",
                    "checkout-123",
                    Guid.Empty));
    }

    [Fact]
    public void BuildReservationKey_rejects_empty_tenant()
    {
        Should.Throw<ArgumentException>(
            () =>
                CheckoutOrchestrator.BuildReservationKey(
                    "",
                    "user-1",
                    "checkout-123",
                    Guid.NewGuid()));
    }

    [Fact]
    public void BuildReservationKey_rejects_empty_user()
    {
        Should.Throw<ArgumentException>(
            () =>
                CheckoutOrchestrator.BuildReservationKey(
                    "tenant-1",
                    "",
                    "checkout-123",
                    Guid.NewGuid()));
    }

    [Fact]
    public void BuildReservationKey_rejects_empty_idempotency_key()
    {
        Should.Throw<ArgumentException>(
            () =>
                CheckoutOrchestrator.BuildReservationKey(
                    "tenant-1",
                    "user-1",
                    "",
                    Guid.NewGuid()));
    }


}
