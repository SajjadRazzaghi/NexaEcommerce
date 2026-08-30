using NexaEcommerce.Modules.ShoppingCart.Application.DTOs;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;
using NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;
using NexaEcommerce.SharedKernel.Abstractions;

namespace NexaEcommerce.Modules.ShoppingCart.Application.Services;

public sealed class CartService(
    ICartRepository repository,
    IProductVariantReader productVariantReader,
    IStockReader stockReader,
    ICartUnitOfWork unitOfWork)
    : ICartService
{
    public async Task<CartDto> GetAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken = default)
    {
        var cart =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        return cart is null
            ? CartDto.Empty(tenantId)
            : Map(cart);
    }

    public async Task<CartDto> AddItemAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        AddCartItemDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity));

        var variant =
            await productVariantReader
                .GetSellableVariantAsync(
                    request.ProductVariantId,
                    cancellationToken);

        if (variant is null ||
            !variant.IsActive ||
            !variant.IsPublished)
        {
            throw new KeyNotFoundException(
                "Product variant is not available.");
        }

        var availableQuantity =
            await stockReader.GetAvailableQuantityAsync(
                tenantId,
                request.ProductVariantId,
                cancellationToken);

        if (availableQuantity is null)
        {
            throw new KeyNotFoundException(
                "Stock record is not available.");
        }

        var cart =
            await GetOrCreateAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        var currentQuantity =
            cart.Items
                .Where(x =>
                    x.ProductVariantId ==
                    request.ProductVariantId)
                .Select(x => x.Quantity)
                .FirstOrDefault();

        var requestedTotal =
            currentQuantity +
            request.Quantity;

        if (requestedTotal >
            availableQuantity.Value)
        {
            throw new InvalidOperationException(
                "Requested quantity exceeds available stock.");
        }

        cart.AddItem(
            variant.Id,
            request.Quantity,
            variant.Price,
            variant.ProductName,
            variant.ImageUrl);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(cart);
    }

    public async Task<CartDto> SetQuantityAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        SetCartItemQuantityDto request,
        CancellationToken cancellationToken = default)
    {
        var cart =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (cart is null)
            return CartDto.Empty(tenantId);

        if (request.Quantity > 0)
        {
            var variant =
                await productVariantReader
                    .GetSellableVariantAsync(
                        request.ProductVariantId,
                        cancellationToken);

            if (variant is null ||
                !variant.IsActive ||
                !variant.IsPublished)
            {
                throw new KeyNotFoundException(
                    "Product variant is not available.");
            }

            var availableQuantity =
                await stockReader.GetAvailableQuantityAsync(
                    tenantId,
                    request.ProductVariantId,
                    cancellationToken);

            if (availableQuantity is null)
            {
                throw new KeyNotFoundException(
                    "Stock record is not available.");
            }

            if (request.Quantity >
                availableQuantity.Value)
            {
                throw new InvalidOperationException(
                    "Requested quantity exceeds available stock.");
            }

            cart.SetQuantity(
                request.ProductVariantId,
                request.Quantity,
                variant.Price,
                variant.ProductName,
                variant.ImageUrl);
        }
        else
        {
            cart.RemoveItem(
                request.ProductVariantId);
        }

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(cart);
    }

    public async Task<CartDto> RemoveItemAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var cart =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (cart is null)
            return CartDto.Empty(tenantId);

        cart.RemoveItem(
            productVariantId);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(cart);
    }

    public async Task<CartDto> ClearAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken = default)
    {
        var cart =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (cart is null)
            return CartDto.Empty(tenantId);

        cart.Clear();

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(cart);
    }
public async Task<CartDto> MergeGuestCartAsync(
    string tenantId,
    string userId,
    string guestToken,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));

        if (string.IsNullOrWhiteSpace(guestToken))
            throw new ArgumentException(
                "Guest cart token is required.",
                nameof(guestToken));

        var guestCart =
            await repository.GetByGuestTokenAsync(
                tenantId,
                guestToken,
                cancellationToken);

        var userCart =
            await repository.GetByUserAsync(
                tenantId,
                userId,
                cancellationToken);

        if (guestCart is null)
        {
            return userCart is null
                ? CartDto.Empty(tenantId)
                : Map(userCart);
        }

        if (userCart is null)
        {
            userCart =
                Cart.ForUser(
                    tenantId,
                    userId);

            await repository.AddAsync(
                userCart,
                cancellationToken);
        }

        var variantIds =
            guestCart.Items
                .Select(x => x.ProductVariantId)
                .Concat(
                    userCart.Items
                        .Select(x => x.ProductVariantId))
                .Distinct()
                .ToArray();

        var availableQuantities =
            await stockReader.GetAvailableQuantitiesAsync(
                tenantId,
                variantIds,
                cancellationToken);

        userCart.MergeFrom(
            guestCart,
            availableQuantities);

        repository.Update(userCart);

        repository.Remove(
            guestCart);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(userCart);
    }



    private async Task<Cart?> FindAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return await repository.GetByUserAsync(
                tenantId,
                userId,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(guestToken))
        {
            return await repository.GetByGuestTokenAsync(
                tenantId,
                guestToken,
                cancellationToken);
        }

        return null;
    }

    private async Task<Cart> GetOrCreateAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken)
    {
        var existing =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (existing is not null)
            return existing;

        Cart cart;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            cart =
                Cart.ForUser(
                    tenantId,
                    userId);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(
                    guestToken))
            {
                throw new InvalidOperationException(
                    "Guest cart token is required.");
            }

            cart =
                Cart.ForGuest(
                    tenantId,
                    guestToken);
        }

        await repository.AddAsync(
            cart,
            cancellationToken);

        return cart;
    }

    private static CartDto Map(
        Cart cart)
    {
        var items =
            cart.Items
                .Select(
                    item =>
                        new CartItemDto(
                            item.ProductVariantId,
                            item.ProductName,
                            item.ImageUrl,
                            item.Quantity,
                            item.UnitPrice,
                            item.LineTotal))
                .ToList();

        return new CartDto(
            cart.Id,
            cart.TenantId,
            items,
            items.Sum(
                x => x.Quantity),
            items.Sum(
                x => x.LineTotal));
    }
}