using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NexaECommerce.Server.Data;
using NexaECommerce.Server.Features.Roles;
using NexaECommerce.Server.Features.Users;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.MultiTenancy;
using NexaECommerce.Tests.Integration.Fixtures;
using Shouldly;

namespace NexaECommerce.Tests.Integration.Features.Users;

[Collection(IntegrationCollection.Name)]
public sealed class UserPermissionRefreshTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Changing_user_roles_updates_the_tenant_role_assignment()
    {
        var user = await CreateUserAsync();

        var client = factory.CreateAuthenticatedClient(
            userId: "admin-user",
            UserPermissions.Update);

        var response = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new UpdateUserRolesRequest(
                [SystemRoles.Member]));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();

        var tenantRoles =
            scope.ServiceProvider
                .GetRequiredService<ITenantRoleService>();

        var roles = await tenantRoles.RoleNamesAsync(
            user.Id,
            TenancyOptions.DefaultTenant);

        roles.ShouldBe(
            [SystemRoles.Member]);
    }

    [Fact]
    public async Task Removing_all_roles_removes_tenant_membership()
    {
        var user = await CreateUserAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var roles =
                scope.ServiceProvider
                    .GetRequiredService<RoleManager<IdentityRole>>();

            var member =
                await roles.FindByNameAsync(SystemRoles.Member);

            member.ShouldNotBeNull();

            var tenantRoles =
                scope.ServiceProvider
                    .GetRequiredService<ITenantRoleService>();

            await tenantRoles.SetRoleIdsAsync(
                user.Id,
                TenancyOptions.DefaultTenant,
                [member!.Id]);
        }

        var client = factory.CreateAuthenticatedClient(
            userId: "admin-user",
            UserPermissions.Update);

        var response = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new UpdateUserRolesRequest([]));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var verifyScope = factory.Services.CreateScope();

        var tenantRolesAfter =
            verifyScope.ServiceProvider
                .GetRequiredService<ITenantRoleService>();

        var isMember =
            await tenantRolesAfter.IsMemberAsync(
                user.Id,
                TenancyOptions.DefaultTenant);

        isMember.ShouldBeFalse();
    }

    [Fact]
    public async Task Role_change_rotates_the_security_stamp()
    {
        var user = await CreateUserAsync();

        string? before;

        using (var scope = factory.Services.CreateScope())
        {
            var users =
                scope.ServiceProvider
                    .GetRequiredService<UserManager<AppUser>>();

            var loaded =
                await users.FindByIdAsync(user.Id);

            loaded.ShouldNotBeNull();

            before = loaded!.SecurityStamp;
        }

        var client = factory.CreateAuthenticatedClient(
            userId: "admin-user",
            UserPermissions.Update);

        var response = await client.PutAsJsonAsync(
            $"/api/users/{user.Id}/roles",
            new UpdateUserRolesRequest(
                [SystemRoles.Member]));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var verifyScope = factory.Services.CreateScope();

        var usersAfter =
            verifyScope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

        var updated =
            await usersAfter.FindByIdAsync(user.Id);

        updated.ShouldNotBeNull();
        updated!.SecurityStamp.ShouldNotBeNullOrWhiteSpace();

        updated.SecurityStamp
            .ShouldNotBe(before);
    }

    private async Task<AppUser> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();

        var users =
            scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

        var email =
            $"permission-test-{Guid.NewGuid():N}@nexaecommerce.test";

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Permission Test User"
        };

        var result =
            await users.CreateAsync(user);

        result.Succeeded.ShouldBeTrue();

        return user;
    }
}