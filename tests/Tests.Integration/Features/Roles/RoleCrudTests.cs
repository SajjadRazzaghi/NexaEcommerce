using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NexaECommerce.Server.Features.Roles;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Roles;

[Collection(IntegrationCollection.Name)]
public sealed class RoleCrudTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Can_create_read_update_and_delete_custom_role()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "role-admin",
            RolePermissions.Create,
            RolePermissions.Read,
            RolePermissions.Update,
            RolePermissions.Delete);

        var roleName = $"Catalog Manager {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync(
            "/api/roles/",
            new SaveRoleRequest(
                roleName,
                ["catalog.products.read"]));

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<RoleDto>();
        created.ShouldNotBeNull();

        created!.Name.ShouldBe(roleName);
        created.Permissions.ShouldContain("catalog.products.read");

        var getResponse = await client.GetAsync(
            $"/api/roles/{created.Id}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<RoleDto>();
        fetched.ShouldNotBeNull();
        fetched!.Id.ShouldBe(created.Id);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/roles/{created.Id}",
            new SaveRoleRequest(
                roleName,
                [
                    "catalog.products.read",
                    "catalog.products.update"
                ]));

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<RoleDto>();
        updated.ShouldNotBeNull();

        updated!.Permissions.ShouldContain("catalog.products.read");
        updated.Permissions.ShouldContain("catalog.products.update");

        var deleteResponse = await client.DeleteAsync(
            $"/api/roles/{created.Id}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var afterDelete = await client.GetAsync(
            $"/api/roles/{created.Id}");

        afterDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Unknown_permission_is_rejected()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "role-admin",
            RolePermissions.Create);

        var response = await client.PostAsJsonAsync(
            "/api/roles/",
            new SaveRoleRequest(
                $"Invalid Role {Guid.NewGuid():N}",
                ["this.permission.does.not.exist"]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Built_in_admin_role_cannot_be_modified()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "role-admin",
            RolePermissions.Read,
            RolePermissions.Update);

        var listResponse = await client.GetAsync("/api/roles/");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var roles = await listResponse.Content
            .ReadFromJsonAsync<List<RoleDto>>();

        roles.ShouldNotBeNull();

        var admin = roles!
            .Single(r => r.Name == "Admin");

        var response = await client.PutAsJsonAsync(
            $"/api/roles/{admin.Id}",
            new SaveRoleRequest(
                "Something Else",
                []));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Built_in_admin_role_cannot_be_deleted()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "role-admin",
            RolePermissions.Read,
            RolePermissions.Delete);

        var listResponse = await client.GetAsync("/api/roles/");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var roles = await listResponse.Content
            .ReadFromJsonAsync<List<RoleDto>>();

        roles.ShouldNotBeNull();

        var admin = roles!
            .Single(r => r.Name == "Admin");

        var response = await client.DeleteAsync(
            $"/api/roles/{admin.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}