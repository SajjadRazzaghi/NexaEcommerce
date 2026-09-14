using NexaEcommerce.Modules.ShoppingCart.Application.DTOs;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using NexaEcommerce.Modules.ShoppingCart.Domain.Interfaces;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaEcommerce.SharedKernel.Infrastructure;

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
        repository.ClearTracking();

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
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Quantity),
                "Quantity must be greater than zero.");
        }

        if (request.ProductVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(request.ProductVariantId));
        }

        var variant =
            await productVariantReader
                .GetSellableVariantAsync(
                    request.ProductVariantId,
                    cancellationToken);

        if (
            variant is null ||
            !variant.IsActive ||
            !variant.IsPublished)
        {
            throw new KeyNotFoundException(
                "Product variant is not available.");
        }

        var availableQuantity =
            await stockReader
                .GetAvailableQuantityAsync(
                    tenantId,
                    request.ProductVariantId,
                    cancellationToken);

        if (availableQuantity is null)
        {
            throw new KeyNotFoundException(
                "Stock record is not available.");
        }

        if (request.Quantity > availableQuantity.Value)
        {
            throw new InvalidOperationException(
                "Requested quantity exceeds available stock.");
        }

        /*
         * این مسیر فقط Cart را بدون Items می‌خواند.
         *
         * دلیل:
         * برای INSERT جدید اصلاً نمی‌خواهیم Navigation Graph مربوط
         * به CartItem وارد ChangeTracker شود.
         */
        repository.ClearTracking();

        var cart =
            await GetOrCreateWithoutItemsAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        /*
         * Existing Item را مستقیماً از جدول CartItems پیدا می‌کنیم.
         */
        var existingItem =
            await repository.GetItemAsync(
                cart.Id,
                request.ProductVariantId,
                cancellationToken);

        if (existingItem is not null)
        {
            var requestedTotal =
                existingItem.Quantity +
                request.Quantity;

            if (requestedTotal > availableQuantity.Value)
            {
                throw new InvalidOperationException(
                    "Requested quantity exceeds available stock.");
            }

            existingItem.SetQuantity(
                requestedTotal,
                variant.Price,
                variant.ProductName,
                variant.ImageUrl);

            /*
             * Cart را نیز تغییر می‌دهیم تا UpdatedAt به‌روزرسانی شود.
             */
            repository.ClearTracking();

            var trackedCart =
                await GetOrCreateWithoutItemsAsync(
                    tenantId,
                    userId,
                    guestToken,
                    cancellationToken);

            if (trackedCart is null)
            {
                throw new InvalidOperationException(
                    "Cart could not be loaded.");
            }

            /*
             * چون بعد از ClearTracking، existingItem دیگر tracked نیست،
             * آن را به‌صورت صریح دوباره به Context معرفی می‌کنیم.
             */
            var freshExistingItem =
                await repository.GetItemAsync(
                    trackedCart.Id,
                    request.ProductVariantId,
                    cancellationToken);

            if (freshExistingItem is null)
            {
                throw new InvalidOperationException(
                    "Cart item disappeared while it was being updated.");
            }

            var finalQuantity =
                freshExistingItem.Quantity +
                request.Quantity;

            if (finalQuantity > availableQuantity.Value)
            {
                throw new InvalidOperationException(
                    "Requested quantity exceeds available stock.");
            }

            freshExistingItem.SetQuantity(
                finalQuantity,
                variant.Price,
                variant.ProductName,
                variant.ImageUrl);

            trackedCart.SetQuantity(
                request.ProductVariantId,
                finalQuantity,
                variant.Price,
                variant.ProductName,
                variant.ImageUrl);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            repository.ClearTracking();

            var updatedCart =
                await FindAsync(
                    tenantId,
                    userId,
                    guestToken,
                    cancellationToken);

            return updatedCart is null
                ? CartDto.Empty(tenantId)
                : Map(updatedCart);
        }

        /*
         * INSERT جدید:
         *
         * دیگر Cart.AddItem() را صدا نمی‌زنیم.
         * مستقیماً DbSet<CartItem>.AddAsync() انجام می‌شود.
         */
        repository.ClearTracking();

        cart =
            await GetOrCreateWithoutItemsAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (cart is null)
        {
            throw new InvalidOperationException(
                "Cart could not be loaded.");
        }

        /*
         * یک بررسی نهایی برای race condition.
         */
        var raceExistingItem =
            await repository.GetItemAsync(
                cart.Id,
                request.ProductVariantId,
                cancellationToken);

        if (raceExistingItem is not null)
        {
            var raceQuantity =
                raceExistingItem.Quantity +
                request.Quantity;

            if (raceQuantity > availableQuantity.Value)
            {
                throw new InvalidOperationException(
                    "Requested quantity exceeds available stock.");
            }

            raceExistingItem.SetQuantity(
                raceQuantity,
                variant.Price,
                variant.ProductName,
                variant.ImageUrl);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            repository.ClearTracking();

            var raceCart =
                await FindAsync(
                    tenantId,
                    userId,
                    guestToken,
                    cancellationToken);

            return raceCart is null
                ? CartDto.Empty(tenantId)
                : Map(raceCart);
        }

        /*
         * این متد فقط یک CartItem جدید می‌سازد و مستقیماً آن را
         * در DbSet به State=Added می‌برد.
         */
        await repository.AddItemAsync(
            cart.Id,
            variant.Id,
            request.Quantity,
            variant.Price,
            variant.ProductName,
            variant.ImageUrl,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        repository.ClearTracking();

        var freshCartAfterInsert =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        return freshCartAfterInsert is null
            ? CartDto.Empty(tenantId)
            : Map(freshCartAfterInsert);
    }

    public async Task<CartDto> SetQuantityAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        SetCartItemQuantityDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(request.ProductVariantId));
        }

        repository.ClearTracking();

        var cart =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (cart is null)
        {
            return CartDto.Empty(
                tenantId);
        }

        if (request.Quantity <= 0)
        {
            cart.RemoveItem(
                request.ProductVariantId);

            await unitOfWork.SaveChangesAsync(
                cancellationToken);

            return Map(cart);
        }

        var variant =
            await productVariantReader
                .GetSellableVariantAsync(
                    request.ProductVariantId,
                    cancellationToken);

        if (
            variant is null ||
            !variant.IsActive ||
            !variant.IsPublished)
        {
            throw new KeyNotFoundException(
                "Product variant is not available.");
        }

        var availableQuantity =
            await stockReader
                .GetAvailableQuantityAsync(
                    tenantId,
                    request.ProductVariantId,
                    cancellationToken);

        if (availableQuantity is null)
        {
            throw new KeyNotFoundException(
                "Stock record is not available.");
        }

        if (
            request.Quantity >
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
        if (productVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Product variant id is required.",
                nameof(productVariantId));
        }

        repository.ClearTracking();

        var cart =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (cart is null)
        {
            return CartDto.Empty(
                tenantId);
        }

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
        repository.ClearTracking();

        var cart =
            await FindAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (cart is null)
        {
            return CartDto.Empty(
                tenantId);
        }

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
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User id is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(guestToken))
        {
            throw new ArgumentException(
                "Guest cart token is required.",
                nameof(guestToken));
        }

        var normalizedTenantId =
            tenantId.Trim();

        var normalizedUserId =
            userId.Trim();

        var normalizedGuestToken =
            guestToken.Trim();

        repository.ClearTracking();

        var guestCart =
            await repository.GetByGuestTokenAsync(
                normalizedTenantId,
                normalizedGuestToken,
                cancellationToken);

        var userCart =
            await repository.GetByUserAsync(
                normalizedTenantId,
                normalizedUserId,
                cancellationToken);

        if (guestCart is null)
        {
            return userCart is null
                ? CartDto.Empty(
                    normalizedTenantId)
                : Map(userCart);
        }

        if (userCart is null)
        {
            userCart =
                Cart.ForUser(
                    normalizedTenantId,
                    normalizedUserId);

            await repository.AddAsync(
                userCart,
                cancellationToken);
        }

        var variantIds =
            guestCart.Items
                .Where(item => !item.IsDeleted)
                .Select(
                    item =>
                        item.ProductVariantId)
                .Concat(
                    userCart.Items
                        .Where(item => !item.IsDeleted)
                        .Select(
                            item =>
                                item.ProductVariantId))
                .Distinct()
                .ToArray();

        var availableQuantities =
            variantIds.Length == 0
                ? new Dictionary<Guid, int>()
                : await stockReader
                    .GetAvailableQuantitiesAsync(
                        normalizedTenantId,
                        variantIds,
                        cancellationToken);

        userCart.MergeFrom(
            guestCart,
            availableQuantities);

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
        if (string.IsNullOrWhiteSpace(
            tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        var normalizedTenantId =
            tenantId.Trim();

        if (!string.IsNullOrWhiteSpace(
            userId))
        {
            return await repository.GetByUserAsync(
                normalizedTenantId,
                userId.Trim(),
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(
            guestToken))
        {
            return await repository.GetByGuestTokenAsync(
                normalizedTenantId,
                guestToken.Trim(),
                cancellationToken);
        }

        return null;
    }

    private async Task<Cart?> FindWithoutItemsAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
            tenantId))
        {
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        var normalizedTenantId =
            tenantId.Trim();

        if (!string.IsNullOrWhiteSpace(
            userId))
        {
            return await repository.GetByUserWithoutItemsAsync(
                normalizedTenantId,
                userId.Trim(),
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(
            guestToken))
        {
            return await repository.GetByGuestTokenWithoutItemsAsync(
                normalizedTenantId,
                guestToken.Trim(),
                cancellationToken);
        }

        return null;
    }

    private async Task<Cart?> GetOrCreateWithoutItemsAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken)
    {
        var existing =
            await FindWithoutItemsAsync(
                tenantId,
                userId,
                guestToken,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        if (!string.IsNullOrWhiteSpace(
            userId))
        {
            var cart =
                Cart.ForUser(
                    tenantId,
                    userId.Trim());

            await repository.AddAsync(
                cart,
                cancellationToken);

            return cart;
        }

        if (!string.IsNullOrWhiteSpace(
            guestToken))
        {
            var cart =
                Cart.ForGuest(
                    tenantId,
                    guestToken.Trim());

            await repository.AddAsync(
                cart,
                cancellationToken);

            return cart;
        }

        return null;
    }

    private static CartDto Map(
        Cart cart)
    {
        var items =
            cart.Items
                .Where(item => !item.IsDeleted)
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
                item =>
                    item.Quantity),
            items.Sum(
                item =>
                    item.LineTotal));
    }
}