using Microsoft.EntityFrameworkCore;
using NexaEcommerce.Modules.Catalog.Domain.Entities;

namespace NexaEcommerce.Modules.Catalog.Infrastructure.Repositories;

public sealed class ManufacturerRepository
{
    private readonly CatalogDbContext _db;

    public ManufacturerRepository(CatalogDbContext db)
    {
        _db = db;
    }

    public IQueryable<Manufacturer> Query()
    {
        return _db.Manufacturers;
    }

    public Task<Manufacturer?> GetByIdAsync(
        Guid id,
        CancellationToken ct)
    {
        return _db.Manufacturers
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Manufacturer?> GetBySlugAsync(
        string slug,
        CancellationToken ct)
    {
        return _db.Manufacturers
            .FirstOrDefaultAsync(
                x => x.Slug == slug,
                ct);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId,
        CancellationToken ct)
    {
        return _db.Manufacturers
            .AnyAsync(
                x =>
                    x.Name == name &&
                    (!excludeId.HasValue ||
                     x.Id != excludeId.Value),
                ct);
    }

    public Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludeId,
        CancellationToken ct)
    {
        return _db.Manufacturers
            .AnyAsync(
                x =>
                    x.Slug == slug &&
                    (!excludeId.HasValue ||
                     x.Id != excludeId.Value),
                ct);
    }

    public Task AddAsync(
        Manufacturer manufacturer,
        CancellationToken ct)
    {
        return _db.Manufacturers
            .AddAsync(manufacturer, ct)
            .AsTask();
    }

    public void Remove(Manufacturer manufacturer)
    {
        manufacturer.IsDeleted = true;
    }

    public async Task<List<Manufacturer>> GetLookupAsync(
        CancellationToken ct)
    {
        return await _db.Manufacturers
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.IsPublished)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(
        CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}