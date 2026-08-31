using Microsoft.AspNetCore.Mvc;
using NexaEcommerce.Modules.Customers.Application.DTOs;
using NexaEcommerce.Modules.Customers.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.MultiTenancy;
using System.Security.Claims;

namespace NexaECommerce.Server.Features.CustomerAddresses;

public sealed class CustomerAddressEndpoints
    : IFeatureEndpoints
{
    public void Map(
        IEndpointRouteBuilder app)
    {
        var group =
            app.MapGroup(
                    "/api/customer/addresses")
                .WithTags(
                    "Customer Addresses");

        group.MapGet(
            "/",
            GetAll);

        group.MapGet(
            "/{id:guid}",
            Get);

        group.MapPost(
            "/",
            Create);

        group.MapPut(
            "/{id:guid}",
            Update);

        group.MapDelete(
            "/{id:guid}",
            Delete);

        group.MapPost(
            "/{id:guid}/default",
            SetDefault);
    }

    private static async Task<IResult> GetAll(
        ICustomerAddressService service,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
            return Results.Unauthorized();

        var result =
            await service.GetAllAsync(
                tenant.Id,
                userId,
                ct);

        return Results.Ok(result);
    }

    private static async Task<IResult> Get(
        Guid id,
        ICustomerAddressService service,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
            return Results.Unauthorized();

        var result =
            await service.GetAsync(
                tenant.Id,
                userId,
                id,
                ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> Create(
        [FromBody] CreateAddressRequest request,
        ICustomerAddressService service,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
            return Results.Unauthorized();

        try
        {
            var result =
                await service.CreateAsync(
                    tenant.Id,
                    userId,
                    request,
                    ct);

            return Results.Created(
                $"/api/customer/addresses/{result.Id}",
                result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Update(
        Guid id,
        [FromBody] UpdateAddressRequest request,
        ICustomerAddressService service,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
            return Results.Unauthorized();

        try
        {
            var result =
                await service.UpdateAsync(
                    tenant.Id,
                    userId,
                    id,
                    request,
                    ct);

            return result is null
                ? Results.NotFound()
                : Results.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(
                new
                {
                    error = ex.Message
                });
        }
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICustomerAddressService service,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
            return Results.Unauthorized();

        var result =
            await service.DeleteAsync(
                tenant.Id,
                userId,
                id,
                ct);

        return result
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> SetDefault(
        Guid id,
        ICustomerAddressService service,
        ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId =
            GetUserId(http);

        if (userId is null)
            return Results.Unauthorized();

        var result =
            await service.SetDefaultAsync(
                tenant.Id,
                userId,
                id,
                ct);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static string? GetUserId(
        HttpContext http)
    {
        return http.User
            .FindFirstValue(
                ClaimTypes.NameIdentifier);
    }
}