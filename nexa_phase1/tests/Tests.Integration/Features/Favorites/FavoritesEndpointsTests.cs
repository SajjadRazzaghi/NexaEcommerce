using System.Net;
using System.Text.Json;
using NexaECommerce.Server.Features.Favorites;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Favorites;

[Collection(IntegrationCollection.Name)]
public sealed class FavoritesEndpointsTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Anonymous_customer_can_read_favorites_and_receives_guest_cookie()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/favorites/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        root.GetProperty("ids").GetArrayLength().ShouldBe(0);
        root.GetProperty("items").GetArrayLength().ShouldBe(0);
        root.GetProperty("total").GetInt32().ShouldBe(0);

        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            .ShouldBeTrue();

        cookies!.ShouldContain(cookie =>
            cookie.StartsWith(
                "nexa_favorites_guest=",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task Anonymous_customer_cannot_read_popularity_report()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/favorites/admin/popular?periodDays=30");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_customer_without_report_permission_is_forbidden()
    {
        using var client = factory.CreateAuthenticatedClient(
            userId: "favorites-test-user");

        using var response = await client.GetAsync(
            "/api/favorites/admin/popular?periodDays=30");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_with_favorites_report_permission_can_read_popularity_report()
    {
        using var client = factory.CreateAuthenticatedClient(
            userId: "favorites-report-user",
            FavoritePermissions.ReadReport);

        using var response = await client.GetAsync(
            "/api/favorites/admin/popular?periodDays=30");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        root.GetProperty("periodDays").GetInt32().ShouldBe(30);
        root.GetProperty("items").ValueKind.ShouldBe(JsonValueKind.Array);
    }
}
