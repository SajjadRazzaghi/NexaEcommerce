using System.Net;
using NexaECommerce.Server.Features.Products;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Products;

[Collection(IntegrationCollection.Name)]
public sealed class ProductAuthorizationTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Anonymous_request_to_admin_products_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/products/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_user_without_permission_returns_403()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "test-user");

        var response = await client.GetAsync(
            "/api/products/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_with_products_read_permission_can_access_admin_products()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "test-user",
            ProductPermissions.Read);

        var response = await client.GetAsync(
            "/api/products/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task User_with_catalog_wildcard_can_access_admin_products()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "test-user",
            "catalog.*");

        var response = await client.GetAsync(
            "/api/products/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Superadmin_wildcard_can_access_admin_products()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "test-user",
            "*");

        var response = await client.GetAsync(
            "/api/products/admin");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}