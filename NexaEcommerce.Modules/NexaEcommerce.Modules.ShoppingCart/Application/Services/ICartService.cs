using NexaEcommerce.Modules.ShoppingCart.Application.DTOs;

namespace NexaEcommerce.Modules.ShoppingCart.Application.Services;

public interface ICartService
{
    Task<CartDto> GetAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken = default);

    Task<CartDto> AddItemAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        AddCartItemDto request,
        CancellationToken cancellationToken = default);

    Task<CartDto> SetQuantityAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        SetCartItemQuantityDto request,
        CancellationToken cancellationToken = default);

    Task<CartDto> RemoveItemAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        Guid productVariantId,
        CancellationToken cancellationToken = default);

    Task<CartDto> ClearAsync(
        string tenantId,
        string? userId,
        string? guestToken,
        CancellationToken cancellationToken = default);

Task<CartDto> MergeGuestCartAsync(
    string tenantId,
    string userId,
    string guestToken,
    CancellationToken cancellationToken = default);


}