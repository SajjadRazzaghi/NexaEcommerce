
using NexaEcommerce.Modules.Orders.Application.DTOs;
using NexaEcommerce.Modules.Orders.Domain.Entities;
using NexaEcommerce.Modules.Orders.Domain.Interfaces;

namespace NexaEcommerce.Modules.Orders.Application.Services;

public sealed class ShippingMethodService(
    IShippingMethodRepository repository,
    IOrderUnitOfWork unitOfWork)
    : IShippingMethodService
{
    public async Task<IReadOnlyList<ShippingMethodDto>>
        GetActiveAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var methods =
            await repository.GetActiveAsync(
                tenantId,
                cancellationToken);

        return methods
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<ShippingMethodDto>>
        GetAllAsync(
            string tenantId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var methods =
            await repository.GetAllAsync(
                tenantId,
                cancellationToken);

        return methods
            .Select(Map)
            .ToList();
    }

    public async Task<ShippingMethodDto?>
        GetAsync(
            string tenantId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (id == Guid.Empty)
            return null;

        var method =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        return method is null
            ? null
            : Map(method);
    }

    public async Task<ShippingMethodDto>
        CreateAsync(
            string tenantId,
            CreateShippingMethodRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var code =
            NormalizeCode(
                request.Code);

        var duplicate =
            await repository.GetByCodeAsync(
                tenantId,
                code,
                cancellationToken);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Shipping method code '{code}' already exists.");
        }

        var method =
            ShippingMethod.Create(
                tenantId,
                code,
                request.Name,
                request.Carrier,
                request.Price,
                request.SortOrder);

        await repository.AddAsync(
            method,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(method);
    }

    public async Task<ShippingMethodDto?>
        UpdateAsync(
            string tenantId,
            Guid id,
            UpdateShippingMethodRequest request,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var method =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (method is null)
            return null;

        method.Update(
            request.Name,
            request.Carrier,
            request.Price,
            request.SortOrder);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(method);
    }

    public async Task<bool>
        SetActiveAsync(
            string tenantId,
            Guid id,
            bool active,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var method =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (method is null)
            return false;

        if (active)
            method.Activate();
        else
            method.Deactivate();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<bool>
        DeleteAsync(
            string tenantId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        var method =
            await repository.GetByIdAsync(
                tenantId,
                id,
                cancellationToken);

        if (method is null)
            return false;

        repository.Remove(
            method);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<ShippingQuoteDto>
        QuoteAsync(
            string tenantId,
            Guid shippingMethodId,
            CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);

        if (shippingMethodId == Guid.Empty)
        {
            throw new ArgumentException(
                "Shipping method id is required.",
                nameof(shippingMethodId));
        }

        var method =
            await repository.GetByIdAsync(
                tenantId,
                shippingMethodId,
                cancellationToken);

        if (method is null)
        {
            throw new KeyNotFoundException(
                "Shipping method was not found.");
        }

        if (!method.IsActive)
        {
            throw new InvalidOperationException(
                "The selected shipping method is no longer active.");
        }

        return new ShippingQuoteDto(
            method.Id,
            method.Code,
            method.Name,
            method.Carrier,
            method.Price);
    }

    private static ShippingMethodDto Map(
        ShippingMethod method)
    {
        return new ShippingMethodDto(
            method.Id,
            method.Code,
            method.Name,
            method.Carrier,
            method.Price,
            method.SortOrder,
            method.IsActive);
    }

    private static string NormalizeCode(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Shipping method code is required.",
                nameof(value));
        }

        var normalized =
            value.Trim()
                .ToUpperInvariant();

        if (normalized.Length > 64)
        {
            throw new ArgumentException(
                "Shipping method code cannot exceed 64 characters.",
                nameof(value));
        }

        return normalized;
    }

    private static void ValidateTenant(
        string tenantId)
    {
        if (string.IsNullOrWhiteSpace(
                tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }
    }
}