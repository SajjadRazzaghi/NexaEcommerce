using Microsoft.EntityFrameworkCore;

using NexaEcommerce.Modules.Catalog.Domain.Entities.Attributes;
using NexaEcommerce.Modules.Catalog.Domain.Interfaces;

namespace NexaEcommerce.Modules.Catalog.Infrastructure.Repositories;

public class CatalogAttributeRepository : ICatalogAttributeRepository
{
    private readonly CatalogDbContext _context;

    public CatalogAttributeRepository(
        CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<CatalogAttribute?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.CatalogAttributes
            .Include(x => x.Values)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<CatalogAttribute?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.CatalogAttributes
            .Include(x => x.Values)
            .FirstOrDefaultAsync(
                x => x.Code == code,
                cancellationToken);
    }

    public async Task<List<CatalogAttribute>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.CatalogAttributes
            .Include(x => x.Values)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CatalogAttribute>> SearchAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CatalogAttributes
            .Include(x => x.Values)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.Code.Contains(search));
        }

        return await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        CatalogAttribute attribute,
        CancellationToken cancellationToken = default)
    {
        await _context.CatalogAttributes.AddAsync(
            attribute,
            cancellationToken);
    }

    public void Update(CatalogAttribute attribute)
    {
        _context.CatalogAttributes.Update(attribute);
    }

    public void Remove(CatalogAttribute attribute)
    {
        _context.CatalogAttributes.Remove(attribute);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}