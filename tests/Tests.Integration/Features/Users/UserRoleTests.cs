using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NexaECommerce.Server.Data;
using NexaECommerce.Server.Features.Roles;
using NexaECommerce.Server.Features.Users;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.MultiTenancy;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace NexaECommerce.Tests.Integration.Features.Users;

[Collection(IntegrationCollection.Name)]
public sealed class UserRoleTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task User_with_update_permission_can_assign_existing_role()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "admin-user",
            UserPermissions.Update,
            RolePermissions.Read);

        var rolesResponse = await client.GetAsync("/api/roles/");
        rolesResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var roles = await rolesResponse.Content
            .ReadFromJsonAsync<List<RoleDto>>();

        roles.ShouldNotBeNull();

        var member = roles!
            .Single(r => r.Name == SystemRoles.Member);

        var user = await CreateTestUserAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new UpdateUserRolesRequest(
                [member.Name]));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dto = await response.Content
            .ReadFromJsonAsync<UserDto>();

        dto.ShouldNotBeNull();
        dto!.Roles.ShouldContain(SystemRoles.Member);
    }

    [Fact]
    public async Task Assigning_roles_replaces_previous_roles()
    {
        var client = factory.CreateAuthenticatedClient(
            userId: "admin-user",
            UserPermissions.Update,
            RolePermissions.Read);

        var rolesResponse = await client.GetAsync("/api/roles/");
        rolesResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var roles = await rolesResponse.Content
            .ReadFromJsonAsync<List<RoleDto>>();

        roles.ShouldNotBeNull();

        var member = roles!
            .Single(r => r.Name == SystemRoles.Member);

        IdentityRole temporaryRole;

        var user = await CreateTestUserAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var tenantRoles =
                scope.ServiceProvider
                    .GetRequiredService<ITenantRoleService>();

            var roleManager =
                scope.ServiceProvider
                    .GetRequiredService<RoleManager<IdentityRole>>();

            temporaryRole = new IdentityRole(
                $"Temporary-{Guid.NewGuid():N}");

            var created =
                await roleManager.CreateAsync(temporaryRole);

            created.Succeeded.ShouldBeTrue();

            await tenantRoles.SetRoleIdsAsync(
                user.Id,
                TenancyOptions.DefaultTenant,
                [temporaryRole.Id]);
        }

        var response = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new UpdateUserRolesRequest(
                [member.Name]));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<UserDto>();

        result.ShouldNotBeNull();

        result!.Roles.Count.ShouldBe(1);
        result.Roles.ShouldContain(SystemRoles.Member);
        result.Roles.ShouldNotContain(temporaryRole.Name);
    }

    [Fact]
    public async Task Admin_cannot_change_own_roles()
    {
        var self = await CreateTestUserAsync();

        var client = factory.CreateAuthenticatedClient(
            userId: self.Id,
            UserPermissions.Update);

        var response = await client.PutAsJsonAsync(
            $"/api/users/{self.Id}/roles",
            new UpdateUserRolesRequest(
                [SystemRoles.Member]));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unknown_role_is_rejected()
    {
        var user = await CreateTestUserAsync();

        var client = factory.CreateAuthenticatedClient(
            userId: "admin-user",
            UserPermissions.Update);

        var response = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new UpdateUserRolesRequest(
                ["Role-Does-Not-Exist"]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Anonymous_user_cannot_change_roles()
    {
        var user = await CreateTestUserAsync();

        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new UpdateUserRolesRequest(
                [SystemRoles.Member]));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<AppUser> CreateTestUserAsync()
    {
        using var scope = factory.Services.CreateScope();

        var users =
            scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

        var email =
            $"role-test-{Guid.NewGuid():N}@nexaecommerce.test";

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Role Test User"
        };

        var result = await users.CreateAsync(user);

        result.Succeeded.ShouldBeTrue();

        return user;
    }
}