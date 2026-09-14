using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.ShoppingCart.Domain.Entities;
using NexaEcommerce.Modules.ShoppingCart.Application.Services;
using NexaEcommerce.SharedKernel.Infrastructure;

namespace NexaEcommerce.Modules.ShoppingCart.Infrastructure.Persistence;

public sealed class CartUnitOfWork(
    ShoppingCartDbContext context)
    : ICartUnitOfWork
{
    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var entries =
            context.ChangeTracker
                .Entries<CartItem>()
                .Select(
                    entry => new
                    {
                        entry.Entity.Id,
                        entry.Entity.CartId,
                        entry.Entity.ProductVariantId,
                        entry.Entity.Quantity,
                        State = entry.State.ToString(),
                        entry.Entity.IsDeleted
                    })
                .ToList();

        Console.WriteLine(
            "================ CART SAVE DIAGNOSTICS ================");

        Console.WriteLine(
            $"CartItem tracked count: {entries.Count}");

        foreach (var item in entries)
        {
            Console.WriteLine(
                $"CartItem Id={item.Id} " +
                $"CartId={item.CartId} " +
                $"VariantId={item.ProductVariantId} " +
                $"Quantity={item.Quantity} " +
                $"State={item.State} " +
                $"IsDeleted={item.IsDeleted}");
        }

        Console.WriteLine(
            "========================================================");

        foreach (var entry in context.ChangeTracker.Entries<CartItem>())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var exists =
                await context.CartItems
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.Id == entry.Entity.Id,
                        cancellationToken);

            Console.WriteLine(
                $"DATABASE CHECK: CartItem Id={entry.Entity.Id}, Exists={exists}");

            if (!exists)
            {
                Console.WriteLine(
                    $"WARNING: EF is trying to UPDATE CartItem " +
                    $"{entry.Entity.Id} but that row does NOT exist " +
                    $"in the database visible to this DbContext.");
            }
        }

        return await context.SaveChangesAsync(
            cancellationToken);
    }
}
