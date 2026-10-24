using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NexaEcommerce.Modules.Customers.Application.Services;
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Application.Services;
using NexaEcommerce.SharedKernel.Pagination;

using NexaECommerce.Server.Data;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using NexaECommerce.Server.Platform.Pagination;

namespace NexaECommerce.Server.Features.Customers;

public sealed class CustomerAdminEndpoints : IFeatureEndpoints
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers");

        group.MapGet("/", List)
            .RequirePermission(CustomerPermissions.Read);

        group.MapGet("/{id}", Get)
            .RequirePermission(CustomerPermissions.Read);

        group.MapGet("/{id}/orders", GetOrders)
            .RequirePermission(CustomerPermissions.Read);

        group.MapGet("/{id}/addresses", GetAddresses)
            .RequirePermission(CustomerPermissions.Read);
    }

    private static QuerySpec<AppUser> CustomerQuerySpec() =>
        new QuerySpec<AppUser>()
            .Allow("displayName", user => user.DisplayName)
            .Allow("email", user => user.Email)
            .Allow("phoneNumber", user => user.PhoneNumber)
            .Allow("createdAt", user => user.CreatedAt)
            .Searchable(user => user.DisplayName)
            .Searchable(user => user.Email)
            .Searchable(user => user.PhoneNumber)
            .DefaultSort("createdAt", descending: true);

    private static async Task<IResult> List(
        PagedRequest request,
        [FromServices] UserManager<AppUser> users,
        [FromServices] AppDbContext db,
        [FromServices] ITenantContext tenant,
        [FromServices] IOrderService orderService,
        CancellationToken ct)
    {
        var memberships = db.Set<TenantUserRole>()
            .AsNoTracking()
            .Where(role => role.TenantId == tenant.TenantId);

        var memberIds = memberships
            .Select(role => role.UserId)
            .Distinct();

        var adminRoleIds = await db.Roles
            .AsNoTracking()
            .Where(role => role.Name == SystemRoles.Admin)
            .Select(role => role.Id)
            .ToListAsync(ct);

        var tenantAdminIds = await memberships
            .Where(role => adminRoleIds.Contains(role.RoleId))
            .Select(role => role.UserId)
            .Distinct()
            .ToListAsync(ct);

        var query = users.Users
            .AsNoTracking()
            .Where(user =>
                memberIds.Contains(user.Id) &&
                !tenantAdminIds.Contains(user.Id));

        var paged = await query.ToPagedResultAsync(
            request,
            CustomerQuerySpec(),
            user => user,
            ct);

        var summaries =
            await orderService.GetCustomerOrderSummariesAsync(
                tenant.TenantId,
                paged.Items.Select(user => user.Id).ToArray(),
                ct);

        var items = paged.Items
            .Select(user =>
                ToDto(
                    user,
                    summaries.TryGetValue(
                        user.Id,
                        out var summary)
                            ? summary
                            : null))
            .ToList();

        return Results.Ok(
            PagedResult<CustomerAdminDto>.Create(
                items,
                paged.Page,
                paged.PageSize,
                paged.TotalItems));
    }

    private static async Task<IResult> Get(
        string id,
        [FromServices] UserManager<AppUser> users,
        [FromServices] ITenantRoleService tenantRoles,
        [FromServices] ITenantContext tenant,
        [FromServices] IOrderService orderService,
        CancellationToken ct)
    {
        var user = await FindCustomerAsync(
            id,
            users,
            tenantRoles,
            tenant.TenantId,
            ct);

        if (user is null)
            return Results.NotFound();

        var summaries =
            await orderService.GetCustomerOrderSummariesAsync(
                tenant.TenantId,
                [user.Id],
                ct);

        summaries.TryGetValue(user.Id, out var summary);

        return Results.Ok(ToDto(user, summary));
    }

    private static async Task<IResult> GetOrders(
        string id,
        [FromServices] UserManager<AppUser> users,
        [FromServices] ITenantRoleService tenantRoles,
        [FromServices] ITenantContext tenant,
        [FromServices] IOrderService orderService,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        string? status = null)
    {
        var user = await FindCustomerAsync(
            id,
            users,
            tenantRoles,
            tenant.TenantId,
            ct);

        if (user is null)
            return Results.NotFound();

        var orders = await orderService.GetUserOrdersAsync(
            tenant.TenantId,
            user.Id,
            page,
            pageSize,
            status,
            ct);

        return Results.Ok(orders);
    }

    private static async Task<IResult> GetAddresses(
        string id,
        [FromServices] UserManager<AppUser> users,
        [FromServices] ITenantRoleService tenantRoles,
        [FromServices] ITenantContext tenant,
        [FromServices] ICustomerAddressService addressService,
        CancellationToken ct)
    {
        var user = await FindCustomerAsync(
            id,
            users,
            tenantRoles,
            tenant.TenantId,
            ct);

        if (user is null)
            return Results.NotFound();

        var addresses = await addressService.GetAllAsync(
            tenant.TenantId,
            user.Id,
            ct);

        return Results.Ok(addresses);
    }

    private static async Task<AppUser?> FindCustomerAsync(
        string id,
        UserManager<AppUser> users,
        ITenantRoleService tenantRoles,
        string tenantId,
        CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id);

        if (user is null)
            return null;

        if (!await tenantRoles.IsMemberAsync(
                user.Id,
                tenantId,
                ct))
        {
            return null;
        }

        var roles = await tenantRoles.RoleNamesAsync(
            user.Id,
            tenantId,
            ct);

        if (roles.Any(role =>
                string.Equals(
                    role,
                    SystemRoles.Admin,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return user;
    }

    private static CustomerAdminDto ToDto(
        AppUser user,
        CustomerOrderSummaryDto? summary) =>
        new(
            user.Id,
            user.DisplayName,
            user.Email,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.PhoneNumberConfirmed,
            user.CreatedAt,
            summary?.TotalOrders ?? 0,
            summary?.PaidOrders ?? 0,
            summary?.TotalSpent ?? 0m,
            summary?.LastOrderAt);
}