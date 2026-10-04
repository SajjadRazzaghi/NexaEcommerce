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
                $"ENTITY: {entry.Metadata.ClrType.FullName}");

            Debug.WriteLine(
                $"STATE : {entry.State}");

            foreach (var property in entry.Properties)
            {
                if (entry.State == EntityState.Modified &&
                    !property.IsModified)
                {
                    continue;
                }

                Debug.WriteLine(
       $"  FIELD: {property.Metadata.Name} | " +
       $"ORIGINAL: {property.OriginalValue} | " +
       $"CURRENT: {property.CurrentValue} | " +
       $"MODIFIED: {property.IsModified} | " +
       $"PK: {property.Metadata.IsPrimaryKey()} | " +
       $"CONCURRENCY: {property.Metadata.IsConcurrencyToken}");
            }

            Debug.WriteLine(
                "----------------------------------------------------------");
        }

        Debug.WriteLine(
            "==========================================================");
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                var modifiedProperties =
                    entry.Properties
                        .Where(property => property.IsModified)
                        .Select(property => property.Metadata.Name)
                        .ToList();

                Debug.WriteLine(
                    $"ENTITY MODIFIED: {entry.Metadata.ClrType.FullName}");

                Debug.WriteLine(
                    $"ACTUAL MODIFIED PROPERTIES: " +
                    $"{string.Join(", ", modifiedProperties)}");
            }
        }
        try
        {
            return await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Debug.WriteLine(
                "================ EF CONCURRENCY EXCEPTION ===============");

            foreach (var entry in ex.Entries)
            {
                Debug.WriteLine(
                    $"FAILED ENTITY: {entry.Metadata.ClrType.FullName}");

                Debug.WriteLine(
                    $"FAILED STATE : {entry.State}");

                foreach (var property in entry.Properties)
                {
                    Debug.WriteLine(
                        $"  FIELD: {property.Metadata.Name} | " +
                        $"CURRENT: {property.CurrentValue} | " +
                        $"ORIGINAL: {property.OriginalValue} | " +
                        $"PK: {property.Metadata.IsPrimaryKey()} | " +
                        $"CONCURRENCY: {property.Metadata.IsConcurrencyToken}");
                }

                Debug.WriteLine(
                    "----------------------------------------------------------");
            }

            Debug.WriteLine(
                "==========================================================");

            throw;
        }
    }

}