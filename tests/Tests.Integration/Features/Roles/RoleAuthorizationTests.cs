using System.Net;
using NexaECommerce.Server.Features.Roles;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Roles;

[Collection(IntegrationCollection.Name)]
public sealed class RoleAuthorizationTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Anonymous_user_cannot_list_roles()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/roles/");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_user_without_roles_read_permission_gets_forbidden()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "test-user");

        var response = await client.GetAsync("/api/roles/");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_with_roles_read_permission_can_list_roles()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "test-user",
            RolePermissions.Read);

        var response = await client.GetAsync("/api/roles/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task User_with_global_wildcard_can_list_roles()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "test-user",
            "*");

        var response = await client.GetAsync("/api/roles/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task User_with_roles_read_permission_can_read_permission_catalog()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "test-user",
            RolePermissions.Read);

        var response = await client.GetAsync("/api/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}