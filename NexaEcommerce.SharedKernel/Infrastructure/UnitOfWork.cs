using Microsoft.EntityFrameworkCore;
using NexaEcommerce.SharedKernel.Abstractions;
using System.Diagnostics;

namespace NexaEcommerce.SharedKernel.Infrastructure;

public class UnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{
    private readonly TContext _context;

    public UnitOfWork(TContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        _context.ChangeTracker.DetectChanges();

        Debug.WriteLine(
            "==================== EF SAVE CHANGES ====================");

        foreach (var entry in _context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Unchanged ||
                entry.State == EntityState.Detached)
            {
                continue;
            }

            Debug.WriteLine(
                $"ENTITY: {entry.Metadata.ClrType.Name}");

            Debug.WriteLine(
                $"STATE : {entry.State}");

            foreach (var property in entry.Properties)
            {
                if (entry.State == EntityState.Modified)
                {
                    if (!property.IsModified)
                    {
                        continue;
                    }
                }

                Debug.WriteLine(
                    $"  FIELD: {property.Metadata.Name} | " +
                    $"ORIGINAL: {property.OriginalValue} | " +
                    $"CURRENT: {property.CurrentValue}");
            }

            Debug.WriteLine(
                "----------------------------------------------------------");
        }

        Debug.WriteLine(
            "==========================================================");

        return await _context.SaveChangesAsync(
            cancellationToken);
    }
}