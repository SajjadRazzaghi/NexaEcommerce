using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities;
using NexaEcommerce.Modules.Catalog.Infrastructure;
using NexaEcommerce.Modules.Inventory.Application.Services;
using NexaEcommerce.SharedKernel.Abstractions;
using NexaECommerce.Server.Platform.Authorization;
using NexaECommerce.Server.Platform.Features;
using NexaECommerce.Server.Platform.Filters;

namespace NexaECommerce.Server.Features.Favorites;

/// <summary>
/// Persistent customer favorites for signed-in users and anonymous visitors, plus
/// tenant-scoped product popularity reporting for permitted administrators.
/// </summary>
public sealed class FavoritesEndpoints : IFeatureEndpoints
{
    private const string GuestCookieName = "nexa_favorites_guest";
    private const string GuestCookiePath = "/api/favorites";

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/favorites")
            .WithTags("Favorites");

        group.MapGet("/", GetMine)
            .AllowAnonymous();

        group.MapPut("/{productId:guid}", Add)
            .AllowAnonymous()
            .AddEndpointFilter<TransactionFilter>();

        group.MapDelete("/{productId:guid}", Remove)
            .AllowAnonymous()
            .AddEndpointFilter<TransactionFilter>();

        group.MapGet("/admin/popular", Popular)
            .RequirePermission(FavoritePermissions.ReadReport);
    }

    private static async Task<IResult> GetMine(
        [FromServices] CatalogDbContext db,
        [FromServices] IStockReader stockReader,
        [FromServices] ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var userId = GetUserId(http);
        string? guestKey = null;

        if (userId is null)
        {
            guestKey = GetOrCreateGuestKey(http);
        }
        else if (TryGetGuestKey(http, out guestKey))
        {
            await MergeGuestFavoritesAsync(db, tenant.Id, userId, guestKey, ct);
            DeleteGuestCookie(http);
        }

        var favoritesQuery = db.ProductFavorites
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenant.Id &&
                x.IsActive &&
                x.Product.IsActive &&
                x.Product.IsPublished);

        favoritesQuery = userId is not null
            ? favoritesQuery.Where(x => x.UserId == userId)
            : favoritesQuery.Where(x => x.GuestKey == guestKey);

        var rows = await favoritesQuery
            .Include(x => x.Product).ThenInclude(x => x.Images)
            .Include(x => x.Product).ThenInclude(x => x.Brand)
            .Include(x => x.Product).ThenInclude(x => x.ProductCategories).ThenInclude(x => x.Category)
            .Include(x => x.Product).ThenInclude(x => x.Variants)
            .AsSplitQuery()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var variantIds = rows
            .SelectMany(x => x.Product.Variants)
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .Distinct()
            .ToArray();

        var stockQuantities = variantIds.Length == 0
            ? new Dictionary<Guid, int>()
            : (await stockReader.GetAvailableQuantitiesAsync(tenant.Id, variantIds, ct))
                .ToDictionary(x => x.Key, x => Math.Max(0, x.Value));

        var items = rows.Select(row =>
        {
            var product = row.Product;
            var stockQuantity = product.Variants
                .Where(x => x.IsActive)
                .Sum(x => stockQuantities.TryGetValue(x.Id, out var quantity) ? quantity : 0);

            return new FavoriteItemDto(
                product.Id,
                product.Name,
                product.Sku,
                product.Slug,
                product.Price,
                product.ComparePrice,
                product.GetFinalPrice(),
                product.DiscountPercentage,
                product.Currency,
                product.BrandId,
                product.Brand?.Name,
                product.IsActive,
                product.IsFeatured,
                product.IsPublished,
                stockQuantity > 0,
                stockQuantity,
                product.Images
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.DisplayOrder)
                    .Select(x => x.ImageUrl)
                    .FirstOrDefault(),
                product.ProductCategories.Select(x => x.Category.Name).ToArray(),
                product.ProductCategories.Select(x => x.CategoryId).ToArray(),
                product.CreatedAt,
                row.CreatedAt);
        }).ToList();

        return Results.Ok(new
        {
            ids = items.Select(x => x.Id).ToArray(),
            items,
            total = items.Count
        });
    }

    private static async Task<IResult> Add(
        Guid productId,
        [FromServices] CatalogDbContext db,
        [FromServices] ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId = GetUserId(http);
        string? guestKey = null;

        if (userId is null)
        {
            guestKey = GetOrCreateGuestKey(http);
        }
        else if (TryGetGuestKey(http, out guestKey))
        {
            await MergeGuestFavoritesAsync(db, tenant.Id, userId, guestKey, ct);
            DeleteGuestCookie(http);
        }

        var productExists = await db.Products.AnyAsync(
            x => x.Id == productId && x.IsActive && x.IsPublished,
            ct);

        if (!productExists)
            return Results.NotFound(new { error = "Product was not found or is not available." });

        var existingQuery = db.ProductFavorites.Where(x =>
            x.TenantId == tenant.Id &&
            x.ProductId == productId &&
            x.IsActive);

        var alreadyFavorite = userId is not null
            ? await existingQuery.AnyAsync(x => x.UserId == userId, ct)
            : await existingQuery.AnyAsync(x => x.GuestKey == guestKey, ct);

        if (!alreadyFavorite)
        {
            var favorite = new ProductFavorite(tenant.Id, productId, userId, guestKey);
            db.ProductFavorites.Add(favorite);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // A simultaneous second click may hit the filtered unique index.
                db.Entry(favorite).State = EntityState.Detached;
                var nowExists = userId is not null
                    ? await existingQuery.AnyAsync(x => x.UserId == userId, ct)
                    : await existingQuery.AnyAsync(x => x.GuestKey == guestKey, ct);

                if (!nowExists)
                    throw;
            }
        }

        return Results.Ok(new { productId, isFavorite = true });
    }

    private static async Task<IResult> Remove(
        Guid productId,
        [FromServices] CatalogDbContext db,
        [FromServices] ICurrentTenant tenant,
        HttpContext http,
        CancellationToken ct)
    {
        var userId = GetUserId(http);
        string? guestKey = null;

        if (userId is null)
        {
            if (!TryGetGuestKey(http, out guestKey))
                return Results.NoContent();
        }
        else if (TryGetGuestKey(http, out guestKey))
        {
            await MergeGuestFavoritesAsync(db, tenant.Id, userId, guestKey, ct);
            DeleteGuestCookie(http);
        }

        var favoriteQuery = db.ProductFavorites.Where(x =>
            x.TenantId == tenant.Id &&
            x.ProductId == productId &&
            x.IsActive);

        var favorite = userId is not null
            ? await favoriteQuery.FirstOrDefaultAsync(x => x.UserId == userId, ct)
            : await favoriteQuery.FirstOrDefaultAsync(x => x.GuestKey == guestKey, ct);

        if (favorite is null)
            return Results.NoContent();

        favorite.Remove();
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Popular(
        [FromQuery] int periodDays,
        [FromServices] CatalogDbContext db,
        [FromServices] ICurrentTenant tenant,
        CancellationToken ct)
    {
        if (periodDays is not (0 or 7 or 30 or 90))
            periodDays = 30;

        var now = DateTime.UtcNow;
        DateTime? since = periodDays == 0 ? null : now.AddDays(-periodDays);

        var baseQuery = db.ProductFavorites
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.Id);

        var activeCounts = await baseQuery
            .Where(x => x.IsActive)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var addedQuery = since.HasValue
            ? baseQuery.Where(x => x.CreatedAt >= since.Value)
            : baseQuery;

        var addedCounts = await addedQuery
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var removedQuery = baseQuery.Where(x => x.RemovedAt != null);
        if (since.HasValue)
            removedQuery = removedQuery.Where(x => x.RemovedAt >= since.Value);

        var removedCounts = await removedQuery
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var activeMap = activeCounts.ToDictionary(x => x.ProductId, x => x.Count);
        var addedMap = addedCounts.ToDictionary(x => x.ProductId, x => x.Count);
        var removedMap = removedCounts.ToDictionary(x => x.ProductId, x => x.Count);
        var productIds = activeMap.Keys.ToArray();

        var products = await db.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id) && x.IsActive && x.IsPublished)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Sku,
                x.Slug,
                x.Price,
                x.Currency,
                MainImage = x.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = products
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.Sku,
                product.Slug,
                product.Price,
                product.Currency,
                product.MainImage,
                ActiveFavoriteCount = activeMap.GetValueOrDefault(product.Id),
                AddedDuringPeriod = addedMap.GetValueOrDefault(product.Id),
                RemovedDuringPeriod = removedMap.GetValueOrDefault(product.Id)
            })
            .OrderByDescending(x => x.ActiveFavoriteCount)
            .ThenByDescending(x => x.AddedDuringPeriod)
            .ThenBy(x => x.Name)
            .ToList();

        return Results.Ok(new
        {
            periodDays,
            since,
            generatedAt = now,
            totalActiveFavorites = activeCounts.Sum(x => x.Count),
            totalAddedDuringPeriod = addedCounts.Sum(x => x.Count),
            totalRemovedDuringPeriod = removedCounts.Sum(x => x.Count),
            items
        });
    }

    private static async Task MergeGuestFavoritesAsync(
        CatalogDbContext db,
        string tenantId,
        string userId,
        string guestKey,
        CancellationToken ct)
    {
        var guestFavorites = await db.ProductFavorites
            .Where(x =>
                x.TenantId == tenantId &&
                x.GuestKey == guestKey &&
                x.IsActive)
            .ToListAsync(ct);

        if (guestFavorites.Count == 0)
            return;

        var guestProductIds = guestFavorites.Select(x => x.ProductId).Distinct().ToArray();
        var existingUserProductIds = await db.ProductFavorites
            .Where(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.IsActive &&
                guestProductIds.Contains(x.ProductId))
            .Select(x => x.ProductId)
            .ToHashSetAsync(ct);

        foreach (var favorite in guestFavorites)
        {
            if (existingUserProductIds.Add(favorite.ProductId))
                favorite.AssignToUser(userId);
            else
                favorite.Remove();
        }

        await db.SaveChangesAsync(ct);
    }

    private static string? GetUserId(HttpContext http) =>
        http.User.Identity?.IsAuthenticated == true
            ? http.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    private static string GetOrCreateGuestKey(HttpContext http)
    {
        if (TryGetGuestKey(http, out var existing))
            return existing;

        var guestKey = Guid.NewGuid().ToString("N");
        http.Response.Cookies.Append(
            GuestCookieName,
            guestKey,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = http.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = GuestCookiePath,
                MaxAge = TimeSpan.FromDays(365)
            });

        return guestKey;
    }

    private static bool TryGetGuestKey(HttpContext http, out string guestKey)
    {
        guestKey = string.Empty;
        if (!http.Request.Cookies.TryGetValue(GuestCookieName, out var raw) ||
            !Guid.TryParse(raw, out var parsed))
            return false;

        guestKey = parsed.ToString("N");
        return true;
    }

    private static void DeleteGuestCookie(HttpContext http) =>
        http.Response.Cookies.Delete(
            GuestCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = http.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = GuestCookiePath
            });

    private sealed record FavoriteItemDto(
        Guid Id,
        string Name,
        string Sku,
        string Slug,
        decimal Price,
        decimal? ComparePrice,
        decimal FinalPrice,
        decimal DiscountPercentage,
        string Currency,
        Guid? BrandId,
        string? BrandName,
        bool IsActive,
        bool IsFeatured,
        bool IsPublished,
        bool IsInStock,
        int StockQuantity,
        string? MainImage,
        string[] CategoryNames,
        Guid[] CategoryIds,
        DateTime CreatedAt,
        DateTime FavoritedAt);
}
